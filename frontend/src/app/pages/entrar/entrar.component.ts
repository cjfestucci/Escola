import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { environment } from '../../../environments/environment';
import { OpcaoClienteLogin } from '../../models/auth.model';
import { AuthService } from '../../services/auth.service';
import { ConfiguracaoEscolaService } from '../../services/configuracao-escola.service';
import { NotificacaoService } from '../../services/notificacao.service';
import { SegmentoService } from '../../services/segmento.service';

@Component({
  selector: 'app-entrar',
  imports: [FormsModule],
  templateUrl: './entrar.component.html',
  styleUrl: './entrar.component.scss'
})
export class EntrarComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly configuracaoEscola = inject(ConfiguracaoEscolaService);
  private readonly notificacao = inject(NotificacaoService);
  protected readonly segmentoService = inject(SegmentoService);

  // Só em dev, pra agilizar teste manual — nunca preenche sozinho em produção.
  email = environment.production ? '' : 'admin@escola.dev';
  senha = environment.production ? '' : 'escola123';

  readonly entrando = signal(false);
  /** Senha certa numa conta com segundo fator: a tela passa a pedir o código do app autenticador. */
  readonly etapaCodigo = signal(false);
  codigo = '';
  /** A senha confere em mais de uma escola (todas usam esta mesma tela): a pessoa escolhe em qual entrar. */
  readonly escolas = signal<OpcaoClienteLogin[]>([]);
  /** Escola escolhida — vai junto nas próximas chamadas (inclusive na do código do autenticador). */
  private clienteEscolhido: string | undefined;
  readonly mostrarSenha = signal(false);

  voltarDaEtapaCodigo(): void {
    this.etapaCodigo.set(false);
    this.codigo = '';
    this.clienteEscolhido = undefined;
  }

  escolherEscola(escola: OpcaoClienteLogin): void {
    this.clienteEscolhido = escola.id;
    this.escolas.set([]);
    this.entrar();
  }

  voltarDaEscolha(): void {
    this.escolas.set([]);
    this.clienteEscolhido = undefined;
  }

  /** Leva pra tela que pede o link de redefinição por e-mail (o e-mail digitado aqui não é repassado). */
  esqueceuSenha(): void {
    this.router.navigateByUrl('/esqueci-senha');
  }

  entrar(): void {
    if (!this.email.trim() || !this.senha) {
      this.notificacao.erro('Informe e-mail e senha.');
      return;
    }
    if (this.etapaCodigo() && !/^\d{6}$/.test(this.codigo.replace(/\s|-/g, ''))) {
      this.notificacao.erro('Informe o código de 6 dígitos do seu app autenticador.');
      return;
    }

    this.entrando.set(true);
    this.auth.entrar(this.email.trim(), this.senha, this.etapaCodigo() ? this.codigo.replace(/\s|-/g, '') : undefined, this.clienteEscolhido).subscribe({
      next: (resposta) => {
        this.entrando.set(false);
        if (resposta.escolherCliente?.length) {
          this.escolas.set(resposta.escolherCliente);
          return;
        }
        if (resposta.requerSegundoFator) {
          this.etapaCodigo.set(true);
          return;
        }

        // '' passa pelo redirecionamentoInicialGuard, que decide a landing certa por papel/segmento
        // (Responsavel -> portal, Equipe-escola -> /alunos, Equipe-clube -> /dashboard).
        // A configuração da escola (segmento, tema, logo, fuso) vem antes da navegação: o guard da rota '' escolhe a tela inicial
        // pelo segmento — sem esperar, um clube cairia na rotina da escola infantil.
        const destino = resposta.papel === 'Responsavel' ? '/portal/filhos' : '/';
        this.configuracaoEscola.aplicarDaSessao().subscribe({
          next: () => this.router.navigateByUrl(destino),
          error: () => this.router.navigateByUrl(destino)
        });
      },
      error: (resposta) => {
        this.entrando.set(false);
        // O backend já manda a mensagem certa (senha inválida, conta desativada, acesso suspenso, código inválido, muitas tentativas).
        const mensagemDoServidor = typeof resposta.error === 'string' && resposta.error ? resposta.error : null;
        if (resposta.status === 401 || resposta.status === 429) this.notificacao.erro(mensagemDoServidor ?? 'E-mail ou senha inválidos.');
        else this.notificacao.erro('Não foi possível entrar. Tente novamente.');
        if (this.etapaCodigo()) this.codigo = '';
      }
    });
  }
}
