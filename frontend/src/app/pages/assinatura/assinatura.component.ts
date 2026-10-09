import { Component, OnInit, inject, signal } from '@angular/core';

import { formatarReais } from '../../models/assinatura.model';
import { AssinaturaService } from '../../services/assinatura.service';
import { NotificacaoService } from '../../services/notificacao.service';
import { formatarDataAbsoluta } from '../../shared/data-utils';

/** Assinatura do clube com a plataforma: situação, plano, faturas e "Pagar agora". É a única tela que o Admin alcança com a assinatura
 * suspensa (a API responde 402 no resto e o app traz ele pra cá). */
@Component({
  selector: 'app-assinatura',
  templateUrl: './assinatura.component.html',
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
