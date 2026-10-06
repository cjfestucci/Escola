import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { TermoPendente } from '../../models/termo.model';
import { AuthService } from '../../services/auth.service';
import { NotificacaoService } from '../../services/notificacao.service';
import { SessaoService } from '../../services/sessao.service';
import { TermoService } from '../../services/termo.service';

/** Primeira tela do portal enquanto houver filho sem aceite do termo de matrícula (versão atual). A matrícula só é efetivada
 * depois deste aceite; o servidor guarda o texto exato mostrado aqui, com data, IP e navegador. */
@Component({
  selector: 'app-portal-termo',
  imports: [FormsModule],
  templateUrl: './portal-termo.component.html',
  styleUrl: './portal-termo.component.scss'
})
export class PortalTermoComponent implements OnInit {
  private readonly termoService = inject(TermoService);
  private readonly auth = inject(AuthService);
  private readonly sessao = inject(SessaoService);
  private readonly router = inject(Router);
  private readonly notificacao = inject(NotificacaoService);

  readonly termo = signal<TermoPendente | null>(null);
  readonly carregando = signal(true);
  readonly enviando = signal(false);
  concordo = false;

  readonly matriculasPendentes = computed(() => (this.termo()?.alunos ?? []).filter((a) => a.matriculaPendente));
  readonly nomesPendentes = computed(() => juntarNomes(this.matriculasPendentes().map((a) => a.alunoNome)));

  private get usuarioId(): string {
    return this.auth.identidade()?.usuarioId ?? '';
  }

  ngOnInit(): void {
    this.termoService.pendente(this.usuarioId).subscribe({
      next: (termo) => {
        if (termo.alunos.length === 0) {
          this.router.navigateByUrl('/portal/filhos');
          return;
        }
        this.termo.set(termo);
        this.carregando.set(false);
      },
      error: () => {
        this.carregando.set(false);
        this.notificacao.erro('Não foi possível carregar o termo. Tente novamente em instantes.');
      }
    });
  }

  aceitar(): void {
    const termo = this.termo();
    if (!termo || !this.concordo) return;

    this.enviando.set(true);
    this.termoService.aceitar(this.usuarioId, termo.versao, termo.alunos.map((a) => a.alunoId)).subscribe({
      next: () => {
        this.notificacao.sucesso(
          this.matriculasPendentes().length > 0 ? 'Termo aceito. Matrícula confirmada!' : 'Termo aceito. Obrigado!'
        );
        this.router.navigateByUrl('/portal/filhos');
      },
      error: (resposta) => {
        this.enviando.set(false);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível registrar o aceite.');
        // Termo ou lista de filhos mudou no meio do caminho: recarrega pra mostrar o texto certo.
        if (resposta.status === 400) this.ngOnInit();
      }
    });
  }

  sair(): void {
    this.auth.sair();
    this.sessao.limpar();
    this.router.navigateByUrl('/entrar');
  }
}

function juntarNomes(nomes: string[]): string {
  if (nomes.length <= 1) return nomes[0] ?? '';
  return `${nomes.slice(0, -1).join(', ')} e ${nomes[nomes.length - 1]}`;
}
