import { DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';

import { Aluno, Turma } from '../../models/aluno.model';
import { Cobranca, PreviaMensalidades, FormasPagamento } from '../../models/cobranca.model';
import { AlunoService } from '../../services/aluno.service';
import { FinanceiroService } from '../../services/financeiro.service';
import { NotificacaoService } from '../../services/notificacao.service';
import { CalendarioComponent } from '../../shared/calendario/calendario.component';
import { MESES_PT_BR, formatarDataAbsoluta, hojeIso } from '../../shared/data-utils';
import { LogsModalComponent } from '../../shared/logs-modal/logs-modal.component';
import { SeletorAlunoComponent } from '../../shared/seletor-aluno/seletor-aluno.component';

type StatusFiltro = 'todos' | 'pendente' | 'pago' | 'atrasado' | 'cancelada';

@Component({
  selector: 'app-financeiro-lista',
  imports: [FormsModule, CalendarioComponent, DecimalPipe, SeletorAlunoComponent, LogsModalComponent],
  templateUrl: './financeiro-lista.component.html',
  styleUrl: './financeiro-lista.component.scss'
})
export class FinanceiroListaComponent implements OnInit {
  private readonly financeiroService = inject(FinanceiroService);
  private readonly alunoService = inject(AlunoService);
  private readonly route = inject(ActivatedRoute);
  private readonly notificacao = inject(NotificacaoService);

  readonly cobrancas = signal<Cobranca[]>([]);
  readonly turmas = signal<Turma[]>([]);
  readonly alunos = signal<Aluno[]>([]);
  readonly carregando = signal(true);

  readonly filtroNome = signal('');
  readonly filtroTurmaId = signal('');
  readonly filtroStatus = signal<StatusFiltro>('todos');
  readonly filtroAno = signal('');
  readonly filtroMes = signal('');

  protected readonly meses = MESES_PT_BR;

  readonly formularioAberto = signal(false);
  readonly cobrancaEmEdicaoId = signal<string | null>(null);
  readonly salvando = signal(false);
  readonly calendarioAberto = signal(false);
  readonly processandoId = signal<string | null>(null);
  readonly confirmandoCancelamentoId = signal<string | null>(null);
  readonly historicoAbertoId = signal<string | null>(null);

  // Geração de mensalidades em lote: escolhe mês/turma, vê a prévia e só então confirma.
  readonly geracaoAberta = signal(false);
  readonly previa = signal<PreviaMensalidades | null>(null);
  readonly previaCarregando = signal(false);
  readonly gerando = signal(false);
  geracaoMes = "";
  geracaoAno = "";
  geracaoTurmaId = "";


  readonly pixAbertoId = signal<string | null>(null);
  readonly pixCarregando = signal(false);
  readonly pixCodigo = signal<string | null>(null);
  readonly pixAutomatico = signal(false);
  readonly pixCopiado = signal(false);

  readonly enviandoEmailId = signal<string | null>(null);

  alunoId = '';
  descricao = '';
  valor: number | null = null;
  vencimento = '';

  protected readonly hojeIso = hojeIso;
  protected readonly formatarDataAbsoluta = formatarDataAbsoluta;

  readonly anosDisponiveis = computed(() => {
    const anos = new Set(this.cobrancas().map((c) => c.vencimento.slice(0, 4)));
    anos.add(hojeIso().slice(0, 4));
    return [...anos].sort((a, b) => b.localeCompare(a));
  });

  readonly cobrancasFiltradas = computed(() => {
    const nome = this.filtroNome().trim().toLowerCase();
    const status = this.filtroStatus();
    const ano = this.filtroAno();
    const mes = this.filtroMes();
    const hoje = hojeIso();

    return this.cobrancas().filter((c) => {
      const bateNome = !nome || c.alunoNome.toLowerCase().includes(nome);
      const statusAtual = this.statusDe(c, hoje);
      const bateStatus = status === 'todos' || statusAtual === status;
      const bateAno = !ano || c.vencimento.slice(0, 4) === ano;
      const bateMes = !mes || c.vencimento.slice(5, 7) === mes;
      return bateNome && bateStatus && bateAno && bateMes;
    });
  });

  readonly resumo = computed(() => {
    const hoje = hojeIso();
    const lista = this.cobrancas();
    const pendentesLista = lista.filter((c) => this.statusDe(c, hoje) === 'pendente');
    const atrasadasLista = lista.filter((c) => this.statusDe(c, hoje) === 'atrasado');
    const valorPendente = pendentesLista.reduce((soma, c) => soma + c.valor, 0);
    const valorAtrasado = atrasadasLista.reduce((soma, c) => soma + c.valor, 0);
    return {
      pendentes: pendentesLista.length,
      valorPendente,
      atrasadas: atrasadasLista.length,
      valorAtrasado,
      totalAberto: pendentesLista.length + atrasadasLista.length,
      totalPendente: valorPendente + valorAtrasado
    };
  });

  /** Meses oferecidos na geração: do ano passado ao próximo. */
  readonly anosGeracao = (() => {
    const ano = Number(hojeIso().slice(0, 4));
    return [ano - 1, ano, ano + 1].map(String);
  })();

  abrirGeracao(): void {
    // Padrão: o mês que vem — o caso comum é gerar as mensalidades com antecedência.
    const [ano, mes] = hojeIso().split("-").map(Number);
    const proximo = mes === 12 ? { ano: ano + 1, mes: 1 } : { ano, mes: mes + 1 };
    this.geracaoAno = String(proximo.ano);
    this.geracaoMes = String(proximo.mes).padStart(2, "0");
    this.geracaoTurmaId = "";
    this.previa.set(null);
    this.formularioAberto.set(false);
    this.geracaoAberta.set(true);
  }

  fecharGeracao(): void {
    this.geracaoAberta.set(false);
    this.previa.set(null);
  }

  /** Qualquer mudança nos filtros invalida a prévia mostrada (ela não vale mais pro que está selecionado). */
  aoMudarGeracao(): void {
    this.previa.set(null);
  }

  visualizarGeracao(): void {
    this.previaCarregando.set(true);
    this.financeiroService.previaMensalidades(Number(this.geracaoAno), Number(this.geracaoMes), this.geracaoTurmaId || undefined).subscribe({
      next: (previa) => {
        this.previa.set(previa);
        this.previaCarregando.set(false);
      },
      error: (resposta) => {
        this.previaCarregando.set(false);
        this.notificacao.erro(typeof resposta.error === "string" ? resposta.error : "Não foi possível montar a prévia das mensalidades.");
      }
    });
  }

  confirmarGeracao(): void {
    this.gerando.set(true);
    this.financeiroService.gerarMensalidades(Number(this.geracaoAno), Number(this.geracaoMes), this.geracaoTurmaId || undefined).subscribe({
      next: (resultado) => {
        this.gerando.set(false);
        this.notificacao.sucesso(resultado.geradas === 1 ? "1 mensalidade gerada." : resultado.geradas + " mensalidades geradas.");
        this.fecharGeracao();
        this.carregar();
      },
      error: (resposta) => {
        this.gerando.set(false);
        this.notificacao.erro(typeof resposta.error === "string" ? resposta.error : "Não foi possível gerar as mensalidades.");
      }
    });
  }

  /** Formas de pagamento ativas na escola: só elas são oferecidas (nulo até carregar, pra não piscar uma opção desligada). */
  readonly formas = signal<FormasPagamento | null>(null);

  ngOnInit(): void {
    this.financeiroService.formasPagamento().subscribe({ next: (f) => this.formas.set(f), error: () => undefined });
    // Atalhos do Dashboard abrem a lista já filtrada (ex.: /financeiro/mensalidades?status=atrasado).
    const status = this.route.snapshot.queryParamMap.get('status');
    if (status === 'pendente' || status === 'pago' || status === 'atrasado' || status === 'cancelada') this.filtroStatus.set(status);
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
      error: () => {
        this.carregando.set(false);
        this.notificacao.erro('Não foi possível carregar as cobranças.');
      }
    });
  }

  aoMudarTurma(turmaId: string): void {
    this.filtroTurmaId.set(turmaId);
    this.carregar();
  }

  statusDe(c: Cobranca, hoje: string): 'pago' | 'atrasado' | 'pendente' | 'cancelada' {
    if (c.cancelada) return 'cancelada';
    if (c.paga) return 'pago';
    return c.vencimento < hoje ? 'atrasado' : 'pendente';
  }

  limparFiltros(): void {
    this.filtroNome.set('');
    this.filtroStatus.set('todos');
    this.filtroAno.set('');
    this.filtroMes.set('');
    this.aoMudarTurma('');
  }

  abrirNova(): void {
    this.cobrancaEmEdicaoId.set(null);
    this.alunoId = '';
    this.descricao = '';
    this.valor = null;
    this.vencimento = '';
    this.geracaoAberta.set(false);
    this.formularioAberto.set(true);
  }

  editar(cobranca: Cobranca): void {
    this.cobrancaEmEdicaoId.set(cobranca.id);
    this.alunoId = cobranca.alunoId;
    this.descricao = cobranca.descricao;
    this.valor = cobranca.valor;
    this.vencimento = cobranca.vencimento;
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
    if (!this.cobrancaEmEdicaoId() && !this.alunoId) {
      this.notificacao.erro('Selecione o aluno.');
      return;
    }
    if (!this.descricao.trim()) {
      this.notificacao.erro('Informe uma descrição.');
      return;
    }
    if (!this.valor || this.valor <= 0) {
      this.notificacao.erro('Informe um valor maior que zero.');
      return;
    }
    if (!this.vencimento) {
      this.notificacao.erro('Informe o vencimento.');
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
        this.notificacao.sucesso('Cobrança salva com sucesso.');
        this.carregar();
      },
      error: (resposta) => {
        this.salvando.set(false);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível salvar a cobrança.');
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
      error: () => {
        this.processandoId.set(null);
        this.notificacao.erro('Não foi possível atualizar o status da cobrança.');
      }
    });
  }

  abrirHistorico(id: string): void {
    this.historicoAbertoId.set(id);
  }

  fecharHistorico(): void {
    this.historicoAbertoId.set(null);
  }

  pedirConfirmacaoCancelamento(id: string): void {
    this.confirmandoCancelamentoId.set(id);
  }

  cancelarCancelamento(): void {
    this.confirmandoCancelamentoId.set(null);
  }

  confirmarCancelamento(cobranca: Cobranca): void {
    this.processandoId.set(cobranca.id);
    this.financeiroService.cancelar(cobranca.id).subscribe({
      next: (atualizada) => {
        this.cobrancas.update((atual) => atual.map((c) => (c.id === atualizada.id ? atualizada : c)));
        this.processandoId.set(null);
        this.confirmandoCancelamentoId.set(null);
        this.notificacao.sucesso('Cobrança cancelada.');
      },
      error: (resposta) => {
        this.processandoId.set(null);
        this.confirmandoCancelamentoId.set(null);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível cancelar a cobrança.');
      }
    });
  }

  reabrir(cobranca: Cobranca): void {
    this.processandoId.set(cobranca.id);
    this.financeiroService.reabrir(cobranca.id).subscribe({
      next: (atualizada) => {
        this.cobrancas.update((atual) => atual.map((c) => (c.id === atualizada.id ? atualizada : c)));
        this.processandoId.set(null);
        this.notificacao.sucesso('Cobrança reaberta.');
      },
      error: () => {
        this.processandoId.set(null);
        this.notificacao.erro('Não foi possível reabrir a cobrança.');
      }
    });
  }

  abrirPix(cobranca: Cobranca): void {
    this.pixAbertoId.set(cobranca.id);
    this.pixCodigo.set(null);
    this.pixAutomatico.set(false);
    this.pixCopiado.set(false);
    this.pixCarregando.set(true);
    this.financeiroService.obterPix(cobranca.id).subscribe({
      next: (resposta) => {
        this.pixCodigo.set(resposta.codigoCopiaECola);
        this.pixAutomatico.set(resposta.automatico);
        this.pixCarregando.set(false);
      },
      error: (resposta) => {
        this.pixAbertoId.set(null);
        this.pixCarregando.set(false);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível gerar o código Pix.');
      }
    });
  }

  fecharPix(): void {
    this.pixAbertoId.set(null);
  }

  copiarPix(): void {
    const codigo = this.pixCodigo();
    if (!codigo) return;
    navigator.clipboard.writeText(codigo).then(() => {
      this.pixCopiado.set(true);
      setTimeout(() => this.pixCopiado.set(false), 2000);
    });
  }

  enviarEmail(cobranca: Cobranca): void {
    this.enviandoEmailId.set(cobranca.id);
    this.financeiroService.enviarEmail(cobranca.id).subscribe({
      next: () => {
        this.enviandoEmailId.set(null);
        this.notificacao.sucesso('E-mail enviado com sucesso.');
      },
      error: (resposta) => {
        this.enviandoEmailId.set(null);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível enviar o e-mail.');
      }
    });
  }
}
