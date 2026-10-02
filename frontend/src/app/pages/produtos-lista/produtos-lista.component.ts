import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { Produto } from '../../models/produto.model';
import { NotificacaoService } from '../../services/notificacao.service';
import { ProdutoService } from '../../services/produto.service';
import { LogsModalComponent } from '../../shared/logs-modal/logs-modal.component';
import { formatarQuantidade } from '../../shared/numero-utils';

type StatusFiltro = 'todos' | 'ativos' | 'inativos';
type EstoqueFiltro = 'todos' | 'baixo' | 'zerado';

@Component({
  selector: 'app-produtos-lista',
  imports: [FormsModule, RouterLink, LogsModalComponent],
  templateUrl: './produtos-lista.component.html',
  styleUrl: './produtos-lista.component.scss'
})
export class ProdutosListaComponent implements OnInit {
  private readonly produtoService = inject(ProdutoService);
  private readonly router = inject(Router);
  private readonly notificacao = inject(NotificacaoService);

  readonly produtos = signal<Produto[]>([]);
  readonly carregando = signal(true);
  readonly processandoId = signal<string | null>(null);
  readonly historicoAbertoId = signal<string | null>(null);

  readonly filtroBusca = signal('');
  readonly filtroStatus = signal<StatusFiltro>('ativos');
  readonly filtroEstoque = signal<EstoqueFiltro>('todos');

  protected readonly formatarQuantidade = formatarQuantidade;

  readonly produtosFiltrados = computed(() => {
    const busca = this.filtroBusca().trim().toLowerCase();
    const status = this.filtroStatus();
    const estoque = this.filtroEstoque();
    return this.produtos().filter((p) => {
      const bateBusca = !busca || p.nome.toLowerCase().includes(busca) || (p.codigo ?? '').toLowerCase().includes(busca);
      const bateStatus = status === 'todos' || (status === 'ativos' ? p.ativo : !p.ativo);
      const bateEstoque = estoque === 'todos' || (estoque === 'baixo' ? p.estoqueBaixo : p.saldoAtual <= 0);
      return bateBusca && bateStatus && bateEstoque;
    });
  });

  readonly resumo = computed(() => {
    const ativos = this.produtos().filter((p) => p.ativo);
    return {
      ativos: ativos.length,
      estoqueBaixo: ativos.filter((p) => p.estoqueBaixo).length,
      zerados: ativos.filter((p) => p.saldoAtual <= 0).length
    };
  });

  ngOnInit(): void {
    this.carregar();
  }

  private carregar(): void {
    this.carregando.set(true);
    this.produtoService.listar().subscribe({
      next: (produtos) => {
        this.produtos.set(produtos);
        this.carregando.set(false);
      },
      error: () => {
        this.carregando.set(false);
        this.notificacao.erro('Não foi possível carregar os produtos.');
      }
    });
  }

  limparFiltros(): void {
    this.filtroBusca.set('');
    this.filtroStatus.set('ativos');
    this.filtroEstoque.set('todos');
  }

  novo(): void {
    this.router.navigateByUrl('/estoque/produtos/novo');
  }

  editar(produto: Produto): void {
    this.router.navigate(['/estoque/produtos', produto.id, 'editar']);
  }

  movimentar(produto: Produto): void {
    this.router.navigate(['/estoque/movimentacoes'], { queryParams: { produtoId: produto.id } });
  }

  abrirHistorico(produtoId: string): void {
    this.historicoAbertoId.set(produtoId);
  }

  fecharHistorico(): void {
    this.historicoAbertoId.set(null);
  }

  alternarStatus(produto: Produto): void {
    this.processandoId.set(produto.id);
    const requisicao$ = produto.ativo ? this.produtoService.desativar(produto.id) : this.produtoService.ativar(produto.id);

    requisicao$.subscribe({
      next: (atualizado) => {
        this.produtos.update((atual) => atual.map((p) => (p.id === atualizado.id ? atualizado : p)));
        this.processandoId.set(null);
        this.notificacao.sucesso(atualizado.ativo ? 'Produto reativado.' : 'Produto desativado.');
      },
      error: (resposta) => {
        this.processandoId.set(null);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível atualizar o status do produto.');
      }
    });
  }
}
