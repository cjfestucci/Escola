import { Component, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';

import { formatarReais } from '../../models/assinatura.model';
import { AssinaturaService } from '../../services/assinatura.service';
import { NotificacaoService } from '../../services/notificacao.service';
import { formatarDataAbsoluta } from '../../shared/data-utils';

/** Assinatura do clube com a plataforma: situação, plano, faturas e "Pagar agora". É a única tela que o Admin alcança com a assinatura
 * suspensa (a API responde 402 no resto e o app traz ele pra cá). */
@Component({
  selector: 'app-assinatura',
  templateUrl: './assinatura.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './assinatura.component.scss'
})
export class AssinaturaComponent implements OnInit {
  protected readonly assinaturas = inject(AssinaturaService);
  private readonly notificacao = inject(NotificacaoService);

  protected readonly formatarReais = formatarReais;
  protected readonly formatarData = formatarDataAbsoluta;

  readonly carregando = signal(true);
  readonly semAssinatura = signal(false);
  readonly atualizando = signal(false);
  readonly confirmandoCancelamento = signal(false);
  readonly cancelando = signal(false);
  readonly reativando = signal(false);

  ngOnInit(): void {
    this.assinaturas.carregar().subscribe({
      next: () => this.carregando.set(false),
      error: (erro) => {
        this.carregando.set(false);
        if (erro.status === 404) this.semAssinatura.set(true);
        else this.notificacao.erro('Não foi possível carregar a assinatura.');
      }
    });
  }

  cancelar(): void {
    this.cancelando.set(true);
    this.assinaturas.cancelar().subscribe({
      next: (a) => {
        this.cancelando.set(false);
        this.confirmandoCancelamento.set(false);
        this.notificacao.sucesso(
          a.acessoAte ? `Assinatura cancelada. O clube pode usar o sistema até ${formatarDataAbsoluta(a.acessoAte)}.` : 'Assinatura cancelada.'
        );
      },
      error: (erro) => {
        this.cancelando.set(false);
        this.notificacao.erro(typeof erro.error === 'string' ? erro.error : 'Não foi possível cancelar a assinatura agora.');
      }
    });
  }

  reativar(): void {
    this.reativando.set(true);
    this.assinaturas.reativar().subscribe({
      next: (a) => {
        this.reativando.set(false);
        this.notificacao.sucesso(a.bloqueada ? 'Assinatura reativada. O acesso volta assim que a mensalidade for paga.' : 'Assinatura reativada.');
      },
      error: (erro) => {
        this.reativando.set(false);
        this.notificacao.erro(typeof erro.error === 'string' ? erro.error : 'Não foi possível reativar a assinatura agora.');
      }
    });
  }

  /** Confere no gateway agora (ex.: acabou de pagar). */
  atualizar(): void {
    this.atualizando.set(true);
    this.assinaturas.atualizar().subscribe({
      next: (a) => {
        this.atualizando.set(false);
        this.notificacao.sucesso(a.bloqueada ? 'Situação atualizada. O pagamento ainda não foi confirmado.' : 'Situação atualizada.');
      },
      error: () => {
        this.atualizando.set(false);
        this.notificacao.erro('Não foi possível conferir a assinatura agora. Tente de novo em instantes.');
      }
    });
  }
}
