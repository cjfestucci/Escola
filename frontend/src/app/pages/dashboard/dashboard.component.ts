import { DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { Aluno, Turma } from '../../models/aluno.model';
import { Cobranca } from '../../models/cobranca.model';
import { Jogo, ROTULOS_MANDO } from '../../models/competicao.model';
import { ContaPagar } from '../../models/conta-pagar.model';
import { ContaReceber } from '../../models/conta-receber.model';
import { FALTAS_SEGUIDAS_PARA_ALERTA, Faltoso } from '../../models/presenca.model';
import { Produto } from '../../models/produto.model';
import { Unidade } from '../../models/unidade.model';
import { AlunoService } from '../../services/aluno.service';
import { AuthService } from '../../services/auth.service';
import { ContaPagarService } from '../../services/conta-pagar.service';
import { ContaReceberService } from '../../services/conta-receber.service';
import { FinanceiroService } from '../../services/financeiro.service';
import { JogoService } from '../../services/jogo.service';
import { PresencaService } from '../../services/presenca.service';
import { ProdutoService } from '../../services/produto.service';
import { SegmentoService } from '../../services/segmento.service';
import { UnidadeService } from '../../services/unidade.service';
import { formatarDataAbsoluta, hojeIso, somarDias } from '../../shared/data-utils';

interface BarraMes {
  chave: string;
  rotulo: string;
  valor: number;
  atual: boolean;
}

interface Aniversariante {
  nome: string;
  dia: number;
  idadeCompleta: number;
}

interface Atencao {
  texto: string;
  rota: string;
  consulta: Record<string, string>;
  tom: 'erro' | 'aviso';
}

/** Quantos dias à frente entram em "A pagar nos próximos dias". */
const DIAS_A_PAGAR = 7;

const NOMES_MES_CURTO = ['Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun', 'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez'];

@Component({
  selector: 'app-dashboard',
  imports: [DecimalPipe, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  private readonly alunoService = inject(AlunoService);
  private readonly financeiroService = inject(FinanceiroService);
  private readonly unidadeService = inject(UnidadeService);
  private readonly contaPagarService = inject(ContaPagarService);
  private readonly contaReceberService = inject(ContaReceberService);
  private readonly produtoService = inject(ProdutoService);
  private readonly jogoService = inject(JogoService);
  private readonly presencaService = inject(PresencaService);
  protected readonly auth = inject(AuthService);
  protected readonly segmentoService = inject(SegmentoService);

  readonly alunos = signal<Aluno[]>([]);
  readonly turmas = signal<Turma[]>([]);
  readonly unidades = signal<Unidade[]>([]);
  readonly cobrancas = signal<Cobranca[]>([]);
  readonly contasPagar = signal<ContaPagar[]>([]);
  readonly contasReceber = signal<ContaReceber[]>([]);
  readonly produtos = signal<Produto[]>([]);
  readonly jogos = signal<Jogo[]>([]);
  readonly faltosos = signal<Faltoso[]>([]);
  readonly carregando = signal(true);

  protected readonly formatarDataAbsoluta = formatarDataAbsoluta;
  protected readonly diasAPagar = DIAS_A_PAGAR;
  protected rotuloMando(jogo: Jogo): string {
    return ROTULOS_MANDO[jogo.mando];
  }

  readonly unidadeSelecionadaId = signal<string | null>(null);

  readonly nomeUnidadeSelecionada = computed(() => {
    const id = this.unidadeSelecionadaId();
    return id ? (this.unidades().find((u) => u.id === id)?.nome ?? '') : '';
  });

  private readonly mapaTurmaParaUnidade = computed(() => {
    const mapa = new Map<string, string>();
    for (const t of this.turmas()) mapa.set(t.id, t.unidadeId);
    return mapa;
  });

  private readonly mapaAlunoParaUnidade = computed(() => {
    const porTurma = this.mapaTurmaParaUnidade();
    const mapa = new Map<string, string>();
    for (const a of this.alunos()) {
      const unidadeId = porTurma.get(a.turmaId);
      if (unidadeId) mapa.set(a.id, unidadeId);
    }
    return mapa;
  });

  readonly turmasFiltradas = computed(() => {
    const unidadeId = this.unidadeSelecionadaId();
    return unidadeId ? this.turmas().filter((t) => t.unidadeId === unidadeId) : this.turmas();
  });

  readonly alunosFiltrados = computed(() => {
    const unidadeId = this.unidadeSelecionadaId();
    if (!unidadeId) return this.alunos();
    const porTurma = this.mapaTurmaParaUnidade();
    return this.alunos().filter((a) => porTurma.get(a.turmaId) === unidadeId);
  });

  readonly cobrancasFiltradas = computed(() => {
    const unidadeId = this.unidadeSelecionadaId();
    if (!unidadeId) return this.cobrancas();
    const porAluno = this.mapaAlunoParaUnidade();
    return this.cobrancas().filter((c) => porAluno.get(c.alunoId) === unidadeId);
  });

  readonly totalPessoas = computed(() => this.alunosFiltrados().length);
  readonly totalTurmas = computed(() => this.turmasFiltradas().length);

  readonly resumoFinanceiro = computed(() => {
    const hoje = hojeIso();
    // Cancelada não é cobrança em aberto: some de "a vencer", "atrasadas" e do total em aberto.
    const emAberto = this.cobrancasFiltradas().filter((c) => !c.paga && !c.cancelada);
    const pendentes = emAberto.filter((c) => c.vencimento >= hoje);
    const atrasadas = emAberto.filter((c) => c.vencimento < hoje);
    return {
      pendentes: pendentes.length,
      atrasadas: atrasadas.length,
      totalAberto: emAberto.reduce((soma, c) => soma + c.valor, 0)
    };
  });

  readonly recebidoPorMes = computed(() => {
    const mapa = new Map<string, number>();
    for (const c of this.cobrancasFiltradas()) {
      if (!c.paga || !c.pagoEm) continue;
      const chave = c.pagoEm.slice(0, 7);
      mapa.set(chave, (mapa.get(chave) ?? 0) + (c.valorPago ?? c.valor));
    }
    return mapa;
  });

  readonly barrasControle = computed<BarraMes[]>(() => {
    const mapa = this.recebidoPorMes();
    const chaveAtual = hojeIso().slice(0, 7);
    const [anoAtual, mesAtual] = chaveAtual.split('-').map(Number);
    const barras: BarraMes[] = [];
    for (let i = 5; i >= 0; i--) {
      const data = new Date(Date.UTC(anoAtual, mesAtual - 1 - i, 1));
      const chave = `${data.getUTCFullYear()}-${String(data.getUTCMonth() + 1).padStart(2, '0')}`;
      barras.push({
        chave,
        rotulo: NOMES_MES_CURTO[data.getUTCMonth()],
        valor: mapa.get(chave) ?? 0,
        atual: chave === chaveAtual
      });
    }
    return barras;
  });

  readonly maiorBarra = computed(() => Math.max(1, ...this.barrasControle().map((b) => b.valor)));

  readonly recebidoEsteMes = computed(() => {
    const chaveAtual = hojeIso().slice(0, 7);
    return this.recebidoPorMes().get(chaveAtual) ?? 0;
  });

  readonly variacaoRecebido = computed(() => {
    const barras = this.barrasControle();
    const atual = barras[barras.length - 1]?.valor ?? 0;
    const anterior = barras[barras.length - 2]?.valor ?? 0;
    if (anterior <= 0) return null;
    return Math.round(((atual - anterior) / anterior) * 100);
  });

  readonly aniversariantes = computed<Aniversariante[]>(() => {
    const mesAtual = hojeIso().slice(5, 7);
    const anoAtual = Number(hojeIso().slice(0, 4));
    return this.alunosFiltrados()
      .filter((a) => a.dataNascimento.slice(5, 7) === mesAtual)
      .map((a) => ({
        nome: a.nome,
        dia: Number(a.dataNascimento.slice(8, 10)),
        idadeCompleta: anoAtual - Number(a.dataNascimento.slice(0, 4))
      }))
      .sort((a, b) => a.dia - b.dia);
  });

  // Contas a pagar/receber e estoque são da instalação inteira (não carregam Unidade), então o balanço
  // e os alertas dessas áreas ficam consolidados mesmo com uma Unidade selecionada.
  readonly balancoMes = computed(() => {
    const chave = hojeIso().slice(0, 7);
    const mensalidades = this.cobrancas()
      .filter((c) => c.paga && !c.cancelada && c.pagoEm?.slice(0, 7) === chave)
      .reduce((soma, c) => soma + (c.valorPago ?? c.valor), 0);
    const outras = this.contasReceber()
      .filter((c) => c.recebida && !c.cancelada && c.recebidoEm?.slice(0, 7) === chave)
      .reduce((soma, c) => soma + c.valor, 0);
    const despesas = this.contasPagar()
      .filter((c) => c.paga && !c.cancelada && c.pagoEm?.slice(0, 7) === chave)
      .reduce((soma, c) => soma + c.valor, 0);
    const receitas = mensalidades + outras;
    return { receitas, despesas, saldo: receitas - despesas };
  });

  private readonly contasPagarEmAberto = computed(() => this.contasPagar().filter((c) => !c.paga && !c.cancelada));

  readonly contasPagarAtrasadas = computed(() => {
    const hoje = hojeIso();
    return this.contasPagarEmAberto().filter((c) => c.vencimento < hoje);
  });

  readonly contasAPagarProximas = computed(() => {
    const hoje = hojeIso();
    const limite = somarDias(hoje, DIAS_A_PAGAR);
    return this.contasPagarEmAberto()
      .filter((c) => c.vencimento >= hoje && c.vencimento <= limite)
      .sort((a, b) => a.vencimento.localeCompare(b.vencimento));
  });

  readonly totalAPagarProximas = computed(() => this.contasAPagarProximas().reduce((soma, c) => soma + c.valor, 0));

  readonly atletasBloqueados = computed(() => this.alunosFiltrados().filter((a) => a.ativo && a.bloqueado).length);
  readonly produtosEstoqueBaixo = computed(() => this.produtos().filter((p) => p.ativo && p.estoqueBaixo).length);
  readonly produtosSemEstoque = computed(() => this.produtos().filter((p) => p.ativo && p.saldoAtual <= 0).length);

  /** Faltosos (3+ faltas seguidas) das turmas da Unidade selecionada. */
  readonly faltososFiltrados = computed(() => {
    const unidadeId = this.unidadeSelecionadaId();
    if (!unidadeId) return this.faltosos();
    const porTurma = this.mapaTurmaParaUnidade();
    return this.faltosos().filter((f) => porTurma.get(f.turmaId) === unidadeId);
  });

  readonly proximosJogos = computed(() => {
    const hoje = hojeIso();
    const unidadeId = this.unidadeSelecionadaId();
    const porTurma = this.mapaTurmaParaUnidade();
    return this.jogos()
      .filter((j) => j.status === 'Agendado' && j.data.slice(0, 10) >= hoje)
      .filter((j) => !unidadeId || porTurma.get(j.turmaId) === unidadeId)
      .sort((a, b) => (a.data + a.hora).localeCompare(b.data + b.hora))
      .slice(0, 3);
  });

  /** Pendências que pedem uma ação, cada uma levando pra tela já filtrada. Só entra o que tem ocorrência. */
  readonly atencoes = computed<Atencao[]>(() => {
    const itens: Atencao[] = [];
    const bloqueados = this.atletasBloqueados();
    if (bloqueados > 0) {
      const pessoa = this.segmentoService.rotuloPessoa().toLowerCase();
      itens.push({
        texto: `${bloqueados} ${bloqueados === 1 ? `${pessoa} bloqueado` : `${this.segmentoService.rotuloPessoaPlural().toLowerCase()} bloqueados`} por atraso`,
        rota: '/matricula',
        consulta: { status: 'bloqueados' },
        tom: 'erro'
      });
    }
    const faltosos = this.faltososFiltrados();
    if (faltosos.length > 0) {
      // Leva pra chamada da turma com mais faltosos (o caso comum é um só time com o problema).
      const porTurma = new Map<string, number>();
      for (const f of faltosos) porTurma.set(f.turmaId, (porTurma.get(f.turmaId) ?? 0) + 1);
      const turmaId = [...porTurma.entries()].sort((a, b) => b[1] - a[1])[0][0];
      const pessoas = this.segmentoService.rotuloPessoaPlural().toLowerCase();
      itens.push({
        texto: faltosos.length + ' ' + (faltosos.length === 1 ? this.segmentoService.rotuloPessoa().toLowerCase() : pessoas) + ' com ' + FALTAS_SEGUIDAS_PARA_ALERTA + '+ faltas seguidas',
        rota: '/chamada',
        consulta: { turmaId },
        tom: 'aviso'
      });
    }
    const atrasadasPagar = this.contasPagarAtrasadas().length;
    if (atrasadasPagar > 0) {
      itens.push({
        texto: `${atrasadasPagar} ${atrasadasPagar === 1 ? 'conta a pagar atrasada' : 'contas a pagar atrasadas'}`,
        rota: '/financeiro/contas-pagar',
        consulta: { status: 'atrasado' },
        tom: 'erro'
      });
    }
    const semEstoque = this.produtosSemEstoque();
    if (semEstoque > 0) {
      itens.push({
        texto: `${semEstoque} ${semEstoque === 1 ? 'produto sem estoque' : 'produtos sem estoque'}`,
        rota: '/estoque/produtos',
        consulta: { estoque: 'zerado' },
        tom: 'erro'
      });
    }
    const baixo = this.produtosEstoqueBaixo();
    if (baixo > 0) {
      itens.push({
        texto: `${baixo} ${baixo === 1 ? 'produto com estoque baixo' : 'produtos com estoque baixo'}`,
        rota: '/estoque/produtos',
        consulta: { estoque: 'baixo' },
        tom: 'aviso'
      });
    }
    return itens;
  });

  selecionarUnidade(unidadeId: string | null): void {
    this.unidadeSelecionadaId.set(unidadeId);
  }

  ngOnInit(): void {
    this.alunoService.listarAlunos().subscribe((alunos) => this.alunos.set(alunos));
    this.alunoService.listarTurmas().subscribe((turmas) => this.turmas.set(turmas));
    this.unidadeService.listar().subscribe((unidades) => this.unidades.set(unidades));

    if (this.auth.ehFinanceiro()) {
      this.financeiroService.listar({}).subscribe({
        next: (cobrancas) => {
          this.cobrancas.set(cobrancas);
          this.carregando.set(false);
        },
        error: () => this.carregando.set(false)
      });
      // Carregamentos secundários: se algum falhar, só o painel correspondente fica vazio.
      this.contaPagarService.listar().subscribe({ next: (contas) => this.contasPagar.set(contas), error: () => undefined });
      this.contaReceberService.listar().subscribe({ next: (contas) => this.contasReceber.set(contas), error: () => undefined });
      this.produtoService.listar().subscribe({ next: (produtos) => this.produtos.set(produtos), error: () => undefined });
    } else {
      this.carregando.set(false);
    }

    this.presencaService.faltosos().subscribe({ next: (faltosos) => this.faltosos.set(faltosos), error: () => undefined });

    if (this.segmentoService.mostrarCompeticoes()) {
      this.jogoService.listar().subscribe({ next: (jogos) => this.jogos.set(jogos), error: () => undefined });
    }
  }
}
