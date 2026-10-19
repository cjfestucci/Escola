import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { environment } from '../../../environments/environment';
import { LoginResposta, OpcaoClienteLogin } from '../../models/auth.model';
import { AuthService } from '../../services/auth.service';
import { ConfiguracaoEscolaService } from '../../services/configuracao-escola.service';
import { NotificacaoService } from '../../services/notificacao.service';
import { SegmentoService } from '../../services/segmento.service';

@Component({
  selector: 'app-entrar',
  imports: [FormsModule, RouterLink],
  templateUrl: './entrar.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './entrar.component.scss'
})
export class EntrarComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly configuracaoEscola = inject(ConfiguracaoEscolaService);
  private readonly notificacao = inject(NotificacaoService);
  protected readonly segmentoService = inject(SegmentoService);

  constructor() {
    // Já logado: a tela de entrar não faz sentido (apareceria dentro do menu). Vai pra tela inicial da conta.
    if (this.auth.estaLogado()) this.router.navigateByUrl('/');
  }

  // Só em dev, pra agilizar teste manual — nunca preenche sozinho em produção.
  email = environment.production ? '' : 'admin@escola.test';
  senha = environment.production ? '' : 'escola123';

  readonly entrando = signal(false);
  /** Senha certa numa conta com segundo fator: a tela passa a pedir o código do app autenticador. */
  readonly etapaCodigo = signal(false);
  /** De onde vem o código: app autenticador ou e-mail (aí a tela diz pra onde foi e oferece reenviar). */
  readonly metodoCodigo = signal<'App' | 'Email'>('App');
  readonly emailMascarado = signal<string | null>(null);
  /** "Lembrar este aparelho por 30 dias": desmarcado por padrão (computador da secretaria costuma ser compartilhado). */
  lembrarAparelho = false;
  readonly podeLembrarAparelho = signal(false);
  codigo = '';
  /** A senha confere em mais de uma escola (todas usam esta mesma tela): a pessoa escolhe em qual entrar. */
  readonly escolas = signal<OpcaoClienteLogin[]>([]);
  /** Escola escolhida — vai junto nas próximas chamadas (inclusive na do código do autenticador). */
  private clienteEscolhido: string | undefined;
  readonly mostrarSenha = signal(false);

  private entrarNaEtapaCodigo(resposta: LoginResposta): void {
    this.metodoCodigo.set(resposta.metodoSegundoFator === 'Email' ? 'Email' : 'App');
    this.emailMascarado.set(resposta.emailMascarado ?? null);
    this.podeLembrarAparelho.set(!!resposta.podeLembrarAparelho);
    this.etapaCodigo.set(true);
    if (resposta.aviso) this.notificacao.info(resposta.aviso);
  }

  /** Código por e-mail: pedir de novo é só repetir o login sem código (o servidor respeita um intervalo mínimo entre envios). */
  reenviarCodigo(): void {
    this.entrando.set(true);
    this.auth.entrar(this.email.trim(), this.senha, undefined, this.clienteEscolhido).subscribe({
      next: (resposta) => {
        this.entrando.set(false);
        this.codigo = '';
        this.entrarNaEtapaCodigo(resposta);
        if (!resposta.aviso) this.notificacao.sucesso('Enviamos um novo código.');
      },
      error: () => {
        this.entrando.set(false);
        this.notificacao.erro('Não foi possível reenviar o código. Tente novamente.');
      }
    });
  }

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
    // 6 dígitos do app autenticador, ou um código de recuperação (8 letras/números, "XXXX-XXXX").
    const codigoLimpo = this.codigo.replace(/\s|-/g, '');
    if (this.etapaCodigo() && !/^\d{6}$/.test(codigoLimpo) && !/^[A-Za-z0-9]{8}$/.test(codigoLimpo)) {
      this.notificacao.erro(this.metodoCodigo() === 'Email'
        ? 'Informe o código de 6 dígitos enviado por e-mail ou um código de recuperação.'
        : 'Informe o código de 6 dígitos do app autenticador ou um código de recuperação.');
      return;
    }

    this.entrando.set(true);
    const comCodigo = this.etapaCodigo();
    this.auth.entrar(this.email.trim(), this.senha, comCodigo ? codigoLimpo : undefined, this.clienteEscolhido, comCodigo && this.podeLembrarAparelho() && this.lembrarAparelho).subscribe({
      next: (resposta) => {
        this.entrando.set(false);
        if (resposta.escolherCliente?.length) {
          this.escolas.set(resposta.escolherCliente);
          return;
        }
        if (resposta.requerSegundoFator) {
          this.entrarNaEtapaCodigo(resposta);
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
