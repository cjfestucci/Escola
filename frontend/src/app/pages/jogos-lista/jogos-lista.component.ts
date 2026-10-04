import { NgTemplateOutlet } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { Jogo, LocalJogo, ROTULOS_MANDO, ROTULOS_RESULTADO, StatusJogo, resultadoDoJogo } from '../../models/competicao.model';
import { JogoService } from '../../services/jogo.service';
import { NotificacaoService } from '../../services/notificacao.service';
import { formatarDataAbsoluta, hojeIso } from '../../shared/data-utils';
import { LogsModalComponent } from '../../shared/logs-modal/logs-modal.component';

type SituacaoFiltro = 'todas' | StatusJogo;

@Component({
  selector: 'app-jogos-lista',
  imports: [FormsModule, LogsModalComponent, NgTemplateOutlet],
  templateUrl: './jogos-lista.component.html',
  styleUrl: './jogos-lista.component.scss'
})
export class JogosListaComponent implements OnInit {
  private readonly jogoService = inject(JogoService);
  private readonly router = inject(Router);
  private readonly notificacao = inject(NotificacaoService);

  readonly jogos = signal<Jogo[]>([]);
  readonly carregando = signal(true);
  readonly processandoId = signal<string | null>(null);
  readonly confirmandoCancelamentoId = signal<string | null>(null);
  readonly historicoAbertoId = signal<string | null>(null);

  readonly filtroAdversario = signal('');
  readonly filtroTurmaId = signal('');
  readonly filtroCampeonatoId = signal('');
  readonly filtroSituacao = signal<SituacaoFiltro>('todas');

  protected readonly formatarDataAbsoluta = formatarDataAbsoluta;
  protected readonly resultadoDoJogo = resultadoDoJogo;
  protected readonly rotulosResultado = ROTULOS_RESULTADO;
  protected rotuloMando(mando: LocalJogo): string {
    return ROTULOS_MANDO[mando];
  }

  readonly turmasDisponiveis = computed(() => {
    const porId = new Map<string, string>();
    for (const jogo of this.jogos()) porId.set(jogo.turmaId, jogo.turmaNome);
    return [...porId.entries()].map(([id, nome]) => ({ id, nome })).sort((a, b) => a.nome.localeCompare(b.nome, 'pt-BR'));
  });

  readonly campeonatosDisponiveis = computed(() => {
    const porId = new Map<string, string>();
    for (const jogo of this.jogos()) if (jogo.campeonatoId && jogo.campeonatoNome) porId.set(jogo.campeonatoId, jogo.campeonatoNome);
    return [...porId.entries()].map(([id, nome]) => ({ id, nome })).sort((a, b) => a.nome.localeCompare(b.nome, 'pt-BR'));
  });

  readonly jogosFiltrados = computed(() => {
    const adversario = this.filtroAdversario().trim().toLowerCase();
    const turmaId = this.filtroTurmaId();
    const campeonatoId = this.filtroCampeonatoId();
    const situacao = this.filtroSituacao();

    return this.jogos().filter((j) => {
      const bateAdversario = !adversario || j.adversario.toLowerCase().includes(adversario);
      const bateTurma = !turmaId || j.turmaId === turmaId;
      const bateCampeonato = !campeonatoId || (campeonatoId === 'amistoso' ? !j.campeonatoId : j.campeonatoId === campeonatoId);
      const bateSituacao = situacao === 'todas' || j.status === situacao;
      return bateAdversario && bateTurma && bateCampeonato && bateSituacao;
    });
  });

  /** Jogos agendados de hoje em diante, do mais próximo pro mais distante. */
  readonly proximos = computed(() => {
    const hoje = hojeIso();
    return this.jogosFiltrados()
      .filter((j) => j.status === 'Agendado' && j.data.slice(0, 10) >= hoje)
      .sort((a, b) => (a.data + a.hora).localeCompare(b.data + b.hora));
  });

  /** O resto (realizados, cancelados e agendados que já passaram), do mais recente pro mais antigo. */
  readonly anteriores = computed(() => {
    const proximosIds = new Set(this.proximos().map((j) => j.id));
    return this.jogosFiltrados().filter((j) => !proximosIds.has(j.id));
  });

  ngOnInit(): void {
    this.carregar();
  }

  private carregar(): void {
    this.carregando.set(true);
    this.jogoService.listar().subscribe({
      next: (jogos) => {
        this.jogos.set(jogos);
        this.carregando.set(false);
      },
      error: () => {
        this.carregando.set(false);
        this.notificacao.erro('Não foi possível carregar os jogos.');
      }
    });
  }

  limparFiltros(): void {
    this.filtroAdversario.set('');
    this.filtroTurmaId.set('');
    this.filtroCampeonatoId.set('');
    this.filtroSituacao.set('todas');
  }

  temFiltro(): boolean {
    return !!(this.filtroAdversario() || this.filtroTurmaId() || this.filtroCampeonatoId() || this.filtroSituacao() !== 'todas');
  }

  novo(): void {
    this.router.navigateByUrl('/jogos/novo');
  }

  abrir(jogo: Jogo): void {
    this.router.navigate(['/jogos', jogo.id, 'editar']);
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

  confirmarCancelamento(jogo: Jogo): void {
    this.alterar(jogo, this.jogoService.cancelar(jogo.id), 'Jogo cancelado.');
  }

  reabrir(jogo: Jogo): void {
    this.alterar(jogo, this.jogoService.reabrir(jogo.id), 'Jogo reaberto.');
  }

  private alterar(jogo: Jogo, requisicao$: ReturnType<JogoService['cancelar']>, sucesso: string): void {
    this.processandoId.set(jogo.id);
    requisicao$.subscribe({
      next: (atualizado) => {
        // O detalhe devolve os convocados; na lista só importa a quantidade.
        this.jogos.update((atual) => atual.map((j) => (j.id === atualizado.id ? { ...atualizado, convocados: undefined } : j)));
        this.processandoId.set(null);
        this.confirmandoCancelamentoId.set(null);
        this.notificacao.sucesso(sucesso);
      },
      error: (resposta) => {
        this.processandoId.set(null);
        this.confirmandoCancelamentoId.set(null);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível atualizar o jogo.');
      }
    });
  }
}
