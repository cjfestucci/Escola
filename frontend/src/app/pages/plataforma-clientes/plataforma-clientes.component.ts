import { Component, OnInit, computed, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { CuponsComponent } from './cupons/cupons.component';

import { formatarReais } from '../../models/assinatura.model';
import { ClienteResumo, ROTULOS_SEGMENTO } from '../../models/plataforma.model';
import { NotificacaoService } from '../../services/notificacao.service';
import { PlataformaService } from '../../services/plataforma.service';
import { formatarDataAbsoluta, formatarDataHoraAbsoluta } from '../../shared/data-utils';

type Filtro = 'todos' | 'EmTeste' | 'Ativa' | 'EmAtraso' | 'bloqueados' | 'Cancelada' | 'semAssinatura' | 'excluidos';

const FILTROS: { valor: Filtro; rotulo: string }[] = [
  { valor: 'todos', rotulo: 'Todos' },
  { valor: 'EmTeste', rotulo: 'Em teste' },
  { valor: 'Ativa', rotulo: 'Ativas' },
  { valor: 'EmAtraso', rotulo: 'Em atraso' },
  { valor: 'bloqueados', rotulo: 'Suspensas / aguardando' },
  { valor: 'Cancelada', rotulo: 'Canceladas' },
  { valor: 'semAssinatura', rotulo: 'Sem assinatura' },
  { valor: 'excluidos', rotulo: 'Excluídos' }
];

/** Painel do Suporte: todos os clientes do produto, com a assinatura e o uso (contagens). Pra agir num cliente, o Suporte entra nele
 * escolhendo a escola no login. */
@Component({
  selector: 'app-plataforma-clientes',
  imports: [FormsModule, CuponsComponent],
  templateUrl: './plataforma-clientes.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './plataforma-clientes.component.scss'
})
export class PlataformaClientesComponent implements OnInit {
  private readonly plataforma = inject(PlataformaService);
  private readonly notificacao = inject(NotificacaoService);

  protected readonly formatarReais = formatarReais;
  protected readonly formatarData = formatarDataAbsoluta;
  protected readonly formatarDataHora = formatarDataHoraAbsoluta;
  protected readonly rotulosSegmento = ROTULOS_SEGMENTO;
  protected readonly filtros = FILTROS;

  readonly carregando = signal(true);
  readonly clientes = signal<ClienteResumo[]>([]);
  readonly filtro = signal<Filtro>('todos');
  readonly busca = signal('');

  /** Clientes de verdade: os excluídos (dados apagados) não contam no total nem aparecem fora do filtro "Excluídos". */
  readonly totalClientes = computed(() => this.clientes().filter((c) => !c.excluidoEm).length);

  readonly contagem = computed(() => {
    const porFiltro = new Map<Filtro, number>();
    for (const f of FILTROS) porFiltro.set(f.valor, this.clientes().filter((c) => this.passa(c, f.valor)).length);
    return porFiltro;
  });

  /** Receita mensal prevista: assinaturas que estão gerando cobrança (teste e canceladas fora). */
  readonly receitaMensal = computed(() =>
    this.clientes()
      .filter((c) => c.situacao === 'Ativa' || c.situacao === 'EmAtraso')
      .reduce((total, c) => total + (c.valorMensal ?? 0), 0)
  );

  readonly visiveis = computed(() => {
    const termo = this.busca().trim().toLowerCase();
    return this.clientes().filter(
      (c) =>
        this.passa(c, this.filtro()) &&
        (!termo || c.nome.toLowerCase().includes(termo) || (c.emailCobranca ?? '').toLowerCase().includes(termo))
    );
  });

  ngOnInit(): void {
    this.plataforma.listarClientes().subscribe({
      next: (lista) => {
        this.clientes.set(lista);
        this.carregando.set(false);
      },
      error: () => {
        this.carregando.set(false);
        this.notificacao.erro('Não foi possível carregar os clientes.');
      }
    });
  }

  private passa(c: ClienteResumo, filtro: Filtro): boolean {
    if (filtro === 'excluidos') return !!c.excluidoEm;
    if (c.excluidoEm) return false;
    switch (filtro) {
      case 'todos':
        return true;
      case 'semAssinatura':
        return c.situacao === null;
      case 'bloqueados':
        return c.situacao === 'Suspensa' || c.situacao === 'AguardandoPagamento';
      default:
        return c.situacao === filtro;
    }
  }
}
