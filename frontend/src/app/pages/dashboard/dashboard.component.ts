import { DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { Aluno, Turma } from '../../models/aluno.model';
import { Cobranca } from '../../models/cobranca.model';
import { Unidade } from '../../models/unidade.model';
import { AlunoService } from '../../services/aluno.service';
import { AuthService } from '../../services/auth.service';
import { FinanceiroService } from '../../services/financeiro.service';
import { SegmentoService } from '../../services/segmento.service';
import { UnidadeService } from '../../services/unidade.service';
import { hojeIso } from '../../shared/data-utils';

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
  protected readonly auth = inject(AuthService);
  protected readonly segmentoService = inject(SegmentoService);

  readonly alunos = signal<Aluno[]>([]);
  readonly turmas = signal<Turma[]>([]);
  readonly unidades = signal<Unidade[]>([]);
  readonly cobrancas = signal<Cobranca[]>([]);
  readonly carregando = signal(true);

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
    const emAberto = this.cobrancasFiltradas().filter((c) => !c.paga);
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
      mapa.set(chave, (mapa.get(chave) ?? 0) + c.valor);
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
    } else {
      this.carregando.set(false);
    }
  }
}
