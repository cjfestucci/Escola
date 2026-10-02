import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';

import { Fornecedor } from '../../models/fornecedor.model';
import { MovimentacaoEstoque, TipoMovimentacaoEstoque } from '../../models/movimentacao-estoque.model';
import { Produto } from '../../models/produto.model';
import { FornecedorService } from '../../services/fornecedor.service';
import { MovimentacaoEstoqueService } from '../../services/movimentacao-estoque.service';
import { NotificacaoService } from '../../services/notificacao.service';
import { ProdutoService } from '../../services/produto.service';
import { CalendarioComponent } from '../../shared/calendario/calendario.component';
import { MESES_PT_BR, formatarDataAbsoluta, hojeIso } from '../../shared/data-utils';
import { formatarQuantidade } from '../../shared/numero-utils';

type TipoFiltro = 'todos' | TipoMovimentacaoEstoque;

@Component({
  selector: 'app-movimentacoes-estoque',
  imports: [FormsModule, CalendarioComponent, RouterLink],
  templateUrl: './movimentacoes-estoque.component.html',
  styleUrl: './movimentacoes-estoque.component.scss'
})
export class MovimentacoesEstoqueComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly movimentacaoService = inject(MovimentacaoEstoqueService);
  private readonly produtoService = inject(ProdutoService);
  private readonly fornecedorService = inject(FornecedorService);
  private readonly notificacao = inject(NotificacaoService);

  readonly movimentacoes = signal<MovimentacaoEstoque[]>([]);
  readonly produtos = signal<Produto[]>([]);
  readonly fornecedores = signal<Fornecedor[]>([]);
  readonly carregando = signal(true);

  readonly filtroProdutoId = signal('');
  readonly filtroTipo = signal<TipoFiltro>('todos');
  readonly filtroAno = signal('');
  readonly filtroMes = signal('');

  protected readonly meses = MESES_PT_BR;

  readonly formularioAberto = signal(false);
  readonly salvando = signal(false);
  readonly calendarioAberto = signal(false);

  produtoId = '';
  tipo: TipoMovimentacaoEstoque = 'Entrada';
  quantidade: number | null = null;
  data = '';
  fornecedorId = '';
  observacao = '';

  protected readonly hojeIso = hojeIso;
  protected readonly formatarDataAbsoluta = formatarDataAbsoluta;
  protected readonly formatarQuantidade = formatarQuantidade;

  readonly produtosAtivos = computed(() => this.produtos().filter((p) => p.ativo));
  readonly fornecedoresAtivos = computed(() => this.fornecedores().filter((f) => f.ativo));

  private readonly produtoIdSignal = signal('');
  readonly produtoSelecionado = computed(() => this.produtos().find((p) => p.id === this.produtoIdSignal()));

  readonly anosDisponiveis = computed(() => {
    const anos = new Set(this.movimentacoes().map((m) => m.data.slice(0, 4)));
    anos.add(hojeIso().slice(0, 4));
    return [...anos].sort((a, b) => b.localeCompare(a));
  });

  readonly movimentacoesFiltradas = computed(() => {
    const produtoId = this.filtroProdutoId();
    const tipo = this.filtroTipo();
    const ano = this.filtroAno();
    const mes = this.filtroMes();

    return this.movimentacoes().filter((m) => {
      const bateProduto = !produtoId || m.produtoId === produtoId;
      const bateTipo = tipo === 'todos' || m.tipo === tipo;
      const bateAno = !ano || m.data.slice(0, 4) === ano;
      const bateMes = !mes || m.data.slice(5, 7) === mes;
      return bateProduto && bateTipo && bateAno && bateMes;
    });
  });

  ngOnInit(): void {
    this.fornecedorService.listar().subscribe((fornecedores) => this.fornecedores.set(fornecedores));
    this.carregar(() => {
      const produtoIdInicial = this.route.snapshot.queryParamMap.get('produtoId');
      if (produtoIdInicial && this.produtosAtivos().some((p) => p.id === produtoIdInicial)) {
        this.filtroProdutoId.set(produtoIdInicial);
        this.abrirNova(produtoIdInicial);
      }
    });
  }

  private carregar(aoConcluir?: () => void): void {
    this.carregando.set(true);
    forkJoin({
      movimentacoes: this.movimentacaoService.listar(),
      produtos: this.produtoService.listar()
    }).subscribe({
      next: ({ movimentacoes, produtos }) => {
        this.movimentacoes.set(movimentacoes);
        this.produtos.set(produtos);
        this.carregando.set(false);
        aoConcluir?.();
      },
      error: () => {
        this.carregando.set(false);
        this.notificacao.erro('Não foi possível carregar as movimentações.');
      }
    });
  }

  limparFiltros(): void {
    this.filtroProdutoId.set('');
    this.filtroTipo.set('todos');
    this.filtroAno.set('');
    this.filtroMes.set('');
  }

  abrirNova(produtoIdInicial?: string): void {
    if (this.produtosAtivos().length === 0) {
      this.notificacao.erro('Cadastre um produto antes de lançar uma movimentação.');
      return;
    }
    this.selecionarProduto(produtoIdInicial ?? this.produtosAtivos()[0].id);
    this.tipo = 'Entrada';
    this.quantidade = null;
    this.data = hojeIso();
    this.fornecedorId = '';
    this.observacao = '';
    this.formularioAberto.set(true);
  }

  selecionarProduto(id: string): void {
    this.produtoId = id;
    this.produtoIdSignal.set(id);
  }

  aoTrocarTipo(): void {
    if (this.tipo === 'Saida') this.fornecedorId = '';
  }

  selecionarData(dataIso: string): void {
    this.data = dataIso;
    this.calendarioAberto.set(false);
  }

  cancelar(): void {
    this.formularioAberto.set(false);
    this.calendarioAberto.set(false);
  }

  salvar(): void {
    if (!this.produtoId) {
      this.notificacao.erro('Selecione o produto.');
      return;
    }
    if (!this.quantidade || this.quantidade <= 0) {
      this.notificacao.erro('Informe uma quantidade maior que zero.');
      return;
    }
    if (!this.data) {
      this.notificacao.erro('Informe a data.');
      return;
    }

    this.salvando.set(true);
    this.movimentacaoService
      .criar({
        produtoId: this.produtoId,
        tipo: this.tipo,
        quantidade: this.quantidade,
        data: this.data,
        fornecedorId: this.tipo === 'Entrada' ? this.fornecedorId || null : null,
        observacao: this.observacao.trim() || null
      })
      .subscribe({
        next: () => {
          this.salvando.set(false);
          this.formularioAberto.set(false);
          this.notificacao.sucesso(this.tipo === 'Entrada' ? 'Entrada registrada.' : 'Saída registrada.');
          this.carregar();
        },
        error: (resposta) => {
          this.salvando.set(false);
          this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível registrar a movimentação.');
        }
      });
  }
}
