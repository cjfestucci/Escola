import { DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { Aluno, Turma } from '../../models/aluno.model';
import { Cobranca } from '../../models/cobranca.model';
import { AlunoService } from '../../services/aluno.service';
import { FinanceiroService } from '../../services/financeiro.service';
import { CalendarioComponent } from '../../shared/calendario/calendario.component';
import { formatarDataAbsoluta, hojeIso } from '../../shared/data-utils';

type StatusFiltro = 'todos' | 'pendente' | 'pago' | 'atrasado';

@Component({
  selector: 'app-financeiro-lista',
  imports: [FormsModule, CalendarioComponent, DecimalPipe],
  templateUrl: './financeiro-lista.component.html',
  styleUrl: './financeiro-lista.component.scss'
})
export class FinanceiroListaComponent implements OnInit {
  private readonly financeiroService = inject(FinanceiroService);
  private readonly alunoService = inject(AlunoService);

  readonly cobrancas = signal<Cobranca[]>([]);
  readonly turmas = signal<Turma[]>([]);
  readonly alunos = signal<Aluno[]>([]);
  readonly carregando = signal(true);
  readonly erro = signal<string | null>(null);

  readonly filtroNome = signal('');
  readonly filtroTurmaId = signal('');
  readonly filtroStatus = signal<StatusFiltro>('todos');

  readonly formularioAberto = signal(false);
  readonly cobrancaEmEdicaoId = signal<string | null>(null);
  readonly salvando = signal(false);
  readonly calendarioAberto = signal(false);
  readonly processandoId = signal<string | null>(null);
  readonly confirmandoExclusaoId = signal<string | null>(null);

  alunoId = '';
  descricao = '';
  valor: number | null = null;
  vencimento = '';

  protected readonly hojeIso = hojeIso;
  protected readonly formatarDataAbsoluta = formatarDataAbsoluta;

  readonly cobrancasFiltradas = computed(() => {
    const nome = this.filtroNome().trim().toLowerCase();
    const status = this.filtroStatus();
    const hoje = hojeIso();

    return this.cobrancas().filter((c) => {
      const bateNome = !nome || c.alunoNome.toLowerCase().includes(nome);
      const statusAtual = this.statusDe(c, hoje);
      const bateStatus = status === 'todos' || statusAtual === status;
      return bateNome && bateStatus;
    });
  });

  readonly resumo = computed(() => {
    const hoje = hojeIso();
    const lista = this.cobrancas();
    return {
      pendentes: lista.filter((c) => this.statusDe(c, hoje) === 'pendente').length,
      atrasadas: lista.filter((c) => this.statusDe(c, hoje) === 'atrasado').length,
      totalPendente: lista.filter((c) => !c.paga).reduce((soma, c) => soma + c.valor, 0)
    };
  });

  ngOnInit(): void {
    this.alunoService.listarTurmas().subscribe((turmas) => this.turmas.set(turmas));
    this.alunoService.listarAlunos().subscribe((alunos) => this.alunos.set(alunos));
    this.carregar();
  }

  private carregar(): void {
    this.carregando.set(true);
    const turmaId = this.filtroTurmaId() || undefined;
    this.financeiroService.listar({ turmaId }).subscribe({
      next: (cobrancas) => {
        this.cobrancas.set(cobrancas);
        this.carregando.set(false);
      },
      error: () => this.carregando.set(false)
    });
  }

  aoMudarTurma(turmaId: string): void {
    this.filtroTurmaId.set(turmaId);
    this.carregar();
  }

  statusDe(c: Cobranca, hoje: string): 'pago' | 'atrasado' | 'pendente' {
    if (c.paga) return 'pago';
    return c.vencimento < hoje ? 'atrasado' : 'pendente';
  }

  limparFiltros(): void {
    this.filtroNome.set('');
    this.filtroStatus.set('todos');
    this.aoMudarTurma('');
  }

  abrirNova(): void {
    this.cobrancaEmEdicaoId.set(null);
    this.alunoId = '';
    this.descricao = '';
    this.valor = null;
    this.vencimento = '';
    this.erro.set(null);
    this.formularioAberto.set(true);
  }

  editar(cobranca: Cobranca): void {
    this.cobrancaEmEdicaoId.set(cobranca.id);
    this.alunoId = cobranca.alunoId;
    this.descricao = cobranca.descricao;
    this.valor = cobranca.valor;
    this.vencimento = cobranca.vencimento;
    this.erro.set(null);
    this.formularioAberto.set(true);
  }

  selecionarVencimento(dataIso: string): void {
    this.vencimento = dataIso;
    this.calendarioAberto.set(false);
  }

  cancelar(): void {
    this.formularioAberto.set(false);
  }

  salvar(): void {
    this.erro.set(null);

    if (!this.cobrancaEmEdicaoId() && !this.alunoId) {
      this.erro.set('Selecione o aluno.');
      return;
    }
    if (!this.descricao.trim()) {
      this.erro.set('Informe uma descrição.');
      return;
    }
    if (!this.valor || this.valor <= 0) {
      this.erro.set('Informe um valor maior que zero.');
      return;
    }
    if (!this.vencimento) {
      this.erro.set('Informe o vencimento.');
      return;
    }

    this.salvando.set(true);
    const id = this.cobrancaEmEdicaoId();
    const requisicao$ = id
      ? this.financeiroService.editar(id, { descricao: this.descricao.trim(), valor: this.valor, vencimento: this.vencimento })
      : this.financeiroService.criar({
          alunoId: this.alunoId,
          descricao: this.descricao.trim(),
          valor: this.valor,
          vencimento: this.vencimento
        });

    requisicao$.subscribe({
      next: () => {
        this.salvando.set(false);
        this.formularioAberto.set(false);
        this.carregar();
      },
      error: (resposta) => {
        this.salvando.set(false);
        this.erro.set(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível salvar a cobrança.');
      }
    });
  }

  alternarPaga(cobranca: Cobranca): void {
    this.processandoId.set(cobranca.id);
    const requisicao$ = cobranca.paga
      ? this.financeiroService.desmarcarPaga(cobranca.id)
      : this.financeiroService.marcarPaga(cobranca.id);

    requisicao$.subscribe({
      next: (atualizada) => {
        this.cobrancas.update((atual) => atual.map((c) => (c.id === atualizada.id ? atualizada : c)));
        this.processandoId.set(null);
      },
      error: () => this.processandoId.set(null)
    });
  }

  pedirConfirmacaoExclusao(id: string): void {
    this.confirmandoExclusaoId.set(id);
  }

  cancelarExclusao(): void {
    this.confirmandoExclusaoId.set(null);
  }

  confirmarExclusao(cobranca: Cobranca): void {
    this.processandoId.set(cobranca.id);
    this.financeiroService.excluir(cobranca.id).subscribe({
      next: () => {
        this.cobrancas.update((atual) => atual.filter((c) => c.id !== cobranca.id));
        this.processandoId.set(null);
        this.confirmandoExclusaoId.set(null);
      },
      error: () => this.processandoId.set(null)
    });
  }
}
