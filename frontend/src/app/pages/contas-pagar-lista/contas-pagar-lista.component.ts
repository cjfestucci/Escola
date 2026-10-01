import { DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { ContaPagar } from '../../models/conta-pagar.model';
import { Fornecedor } from '../../models/fornecedor.model';
import { ContaPagarService } from '../../services/conta-pagar.service';
import { FornecedorService } from '../../services/fornecedor.service';
import { NotificacaoService } from '../../services/notificacao.service';
import { CalendarioComponent } from '../../shared/calendario/calendario.component';
import { MESES_PT_BR, formatarDataAbsoluta, hojeIso } from '../../shared/data-utils';
import { LogsModalComponent } from '../../shared/logs-modal/logs-modal.component';

type StatusFiltro = 'todos' | 'pendente' | 'pago' | 'atrasado' | 'cancelada';

@Component({
  selector: 'app-contas-pagar-lista',
  imports: [FormsModule, CalendarioComponent, DecimalPipe, RouterLink, LogsModalComponent],
  templateUrl: './contas-pagar-lista.component.html',
  styleUrl: './contas-pagar-lista.component.scss'
})
export class ContasPagarListaComponent implements OnInit {
  private readonly contaPagarService = inject(ContaPagarService);
  private readonly fornecedorService = inject(FornecedorService);
  private readonly notificacao = inject(NotificacaoService);

  readonly contas = signal<ContaPagar[]>([]);
  readonly fornecedores = signal<Fornecedor[]>([]);
  readonly carregando = signal(true);

  readonly filtroDescricao = signal('');
  readonly filtroFornecedorId = signal('');
  readonly filtroStatus = signal<StatusFiltro>('todos');
  readonly filtroAno = signal('');
  readonly filtroMes = signal('');

  protected readonly meses = MESES_PT_BR;

  readonly formularioAberto = signal(false);
  readonly contaEmEdicaoId = signal<string | null>(null);
  readonly salvando = signal(false);
  readonly calendarioAberto = signal(false);
  readonly processandoId = signal<string | null>(null);
  readonly confirmandoCancelamentoId = signal<string | null>(null);
  readonly historicoAbertoId = signal<string | null>(null);

  fornecedorId = '';
  descricao = '';
  valor: number | null = null;
  vencimento = '';

  protected readonly hojeIso = hojeIso;
  protected readonly formatarDataAbsoluta = formatarDataAbsoluta;

  readonly anosDisponiveis = computed(() => {
    const anos = new Set(this.contas().map((c) => c.vencimento.slice(0, 4)));
    anos.add(hojeIso().slice(0, 4));
    return [...anos].sort((a, b) => b.localeCompare(a));
  });

  readonly contasFiltradas = computed(() => {
    const descricao = this.filtroDescricao().trim().toLowerCase();
    const fornecedorId = this.filtroFornecedorId();
    const status = this.filtroStatus();
    const ano = this.filtroAno();
    const mes = this.filtroMes();
    const hoje = hojeIso();

    return this.contas().filter((c) => {
      const bateDescricao = !descricao || c.descricao.toLowerCase().includes(descricao) || c.fornecedorNome.toLowerCase().includes(descricao);
      const bateFornecedor = !fornecedorId || c.fornecedorId === fornecedorId;
      const statusAtual = this.statusDe(c, hoje);
      const bateStatus = status === 'todos' || statusAtual === status;
      const bateAno = !ano || c.vencimento.slice(0, 4) === ano;
      const bateMes = !mes || c.vencimento.slice(5, 7) === mes;
      return bateDescricao && bateFornecedor && bateStatus && bateAno && bateMes;
    });
  });

  readonly resumo = computed(() => {
    const hoje = hojeIso();
    const lista = this.contas();
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

  ngOnInit(): void {
    this.fornecedorService.listar().subscribe((fornecedores) => this.fornecedores.set(fornecedores));
    this.carregar();
  }

  private carregar(): void {
    this.carregando.set(true);
    this.contaPagarService.listar().subscribe({
      next: (contas) => {
        this.contas.set(contas);
        this.carregando.set(false);
      },
      error: () => {
        this.carregando.set(false);
        this.notificacao.erro('Não foi possível carregar as contas a pagar.');
      }
    });
  }

  statusDe(c: ContaPagar, hoje: string): 'pago' | 'atrasado' | 'pendente' | 'cancelada' {
    if (c.cancelada) return 'cancelada';
    if (c.paga) return 'pago';
    return c.vencimento < hoje ? 'atrasado' : 'pendente';
  }

  limparFiltros(): void {
    this.filtroDescricao.set('');
    this.filtroFornecedorId.set('');
    this.filtroStatus.set('todos');
    this.filtroAno.set('');
    this.filtroMes.set('');
  }

  abrirNova(): void {
    if (this.fornecedores().length === 0) {
      this.notificacao.erro('Cadastre um fornecedor antes de lançar uma conta a pagar.');
      return;
    }
    this.contaEmEdicaoId.set(null);
    this.fornecedorId = this.fornecedores()[0].id;
    this.descricao = '';
    this.valor = null;
    this.vencimento = '';
    this.formularioAberto.set(true);
  }

  editar(conta: ContaPagar): void {
    this.contaEmEdicaoId.set(conta.id);
    this.fornecedorId = conta.fornecedorId;
    this.descricao = conta.descricao;
    this.valor = conta.valor;
    this.vencimento = conta.vencimento;
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
    if (!this.fornecedorId) {
      this.notificacao.erro('Selecione o fornecedor.');
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
    const id = this.contaEmEdicaoId();
    const payload = {
      fornecedorId: this.fornecedorId,
      descricao: this.descricao.trim(),
      valor: this.valor,
      vencimento: this.vencimento
    };
    const requisicao$ = id ? this.contaPagarService.editar(id, payload) : this.contaPagarService.criar(payload);

    requisicao$.subscribe({
      next: () => {
        this.salvando.set(false);
        this.formularioAberto.set(false);
        this.notificacao.sucesso('Conta a pagar salva com sucesso.');
        this.carregar();
      },
      error: (resposta) => {
        this.salvando.set(false);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível salvar a conta a pagar.');
      }
    });
  }

  alternarPaga(conta: ContaPagar): void {
    this.processandoId.set(conta.id);
    const requisicao$ = conta.paga ? this.contaPagarService.desmarcarPaga(conta.id) : this.contaPagarService.marcarPaga(conta.id);

    requisicao$.subscribe({
      next: (atualizada) => {
        this.contas.update((atual) => atual.map((c) => (c.id === atualizada.id ? atualizada : c)));
        this.processandoId.set(null);
      },
      error: () => {
        this.processandoId.set(null);
        this.notificacao.erro('Não foi possível atualizar o status da conta.');
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

  confirmarCancelamento(conta: ContaPagar): void {
    this.processandoId.set(conta.id);
    this.contaPagarService.cancelar(conta.id).subscribe({
      next: (atualizada) => {
        this.contas.update((atual) => atual.map((c) => (c.id === atualizada.id ? atualizada : c)));
        this.processandoId.set(null);
        this.confirmandoCancelamentoId.set(null);
        this.notificacao.sucesso('Conta a pagar cancelada.');
      },
      error: (resposta) => {
        this.processandoId.set(null);
        this.confirmandoCancelamentoId.set(null);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível cancelar a conta a pagar.');
      }
    });
  }

  reabrir(conta: ContaPagar): void {
    this.processandoId.set(conta.id);
    this.contaPagarService.reabrir(conta.id).subscribe({
      next: (atualizada) => {
        this.contas.update((atual) => atual.map((c) => (c.id === atualizada.id ? atualizada : c)));
        this.processandoId.set(null);
        this.notificacao.sucesso('Conta a pagar reaberta.');
      },
      error: () => {
        this.processandoId.set(null);
        this.notificacao.erro('Não foi possível reabrir a conta a pagar.');
      }
    });
  }
}
