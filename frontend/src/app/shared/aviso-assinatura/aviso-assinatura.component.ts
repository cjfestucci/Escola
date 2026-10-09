import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';

import { AssinaturaService } from '../../services/assinatura.service';
import { formatarDataAbsoluta } from '../data-utils';

/** Faixa no topo do app pro Admin: dias de teste restantes, fatura em atraso ou acesso suspenso. Some com a assinatura em dia. */
@Component({
  selector: 'app-aviso-assinatura',
  imports: [RouterLink],
  template: `
    @if (aviso(); as a) {
      <div class="aviso" [class.aviso--alerta]="a.alerta" role="status">
        <span>{{ a.texto }}</span>
        @if (a.link) {
          <a [href]="a.link" target="_blank" rel="noopener">Pagar agora</a>
        } @else {
          <a routerLink="/configuracoes/assinatura">Ver assinatura</a>
        }
      </div>
    }
  `,
  styles: `
    .aviso {
      display: flex;
      align-items: center;
      justify-content: center;
      flex-wrap: wrap;
      gap: 6px 14px;
      padding: 8px 16px;
      background: var(--cor-primaria-clara);
      color: var(--cor-texto);
      font-size: 0.88rem;
      text-align: center;
    }
    .aviso--alerta {
      background: #fdecea;
      color: #8c1d18;
    }
    a {
      font-weight: 700;
      color: inherit;
    }
  `
})
export class AvisoAssinaturaComponent {
  private readonly assinaturas = inject(AssinaturaService);

  protected readonly aviso = computed(() => {
    const a = this.assinaturas.minha();
    if (!a) return null;
    switch (a.situacao) {
      case 'EmTeste': {
        const dias = a.diasRestantesTeste ?? 0;
        const quando = dias === 0 ? 'Hoje é o último dia' : `Faltam ${dias} ${dias === 1 ? 'dia' : 'dias'}`;
        return { texto: `Teste grátis: ${quando}.`, link: a.linkPagamento, alerta: false };
      }
      case 'EmAtraso':
        return {
          texto: `A mensalidade de ${a.vencimentoEmAberto ? formatarDataAbsoluta(a.vencimentoEmAberto) : ''} está em atraso.`,
          link: a.linkPagamento,
          alerta: true
        };
      case 'AguardandoPagamento':
      case 'Suspensa':
        return { texto: 'O acesso do clube está suspenso até o pagamento.', link: a.linkPagamento, alerta: true };
      default:
        return null;
    }
  });
}
