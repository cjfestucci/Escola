import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { ChamadaItem, FALTAS_SEGUIDAS_PARA_ALERTA, FrequenciaAluno, StatusPresenca } from '../../models/presenca.model';
import { AlunoService } from '../../services/aluno.service';
import { NotificacaoService } from '../../services/notificacao.service';
import { PresencaService } from '../../services/presenca.service';
import { SegmentoService } from '../../services/segmento.service';
import { SessaoService } from '../../services/sessao.service';
import { UsuarioService } from '../../services/usuario.service';
import { CalendarioComponent } from '../../shared/calendario/calendario.component';
import { hojeIso, rotuloData, somarDias } from '../../shared/data-utils';
import { FiltroPeriodoComponent, PeriodoEscolhido, periodoPadrao } from '../../shared/filtro-periodo/filtro-periodo.component';
import { LogsModalComponent } from '../../shared/logs-modal/logs-modal.component';
import { resolverFotoUrl } from '../../shared/registro-rotina-display';

/** Chamada (presença) da turma, dia a dia: marca Presente/Falta/Justificada de cada aluno/atleta e mostra a frequência dos últimos 30 dias. */
@Component({
  selector: 'app-chamada',
  imports: [CalendarioComponent, LogsModalComponent, FiltroPeriodoComponent],
  templateUrl: './chamada.component.html',
  styleUrl: './chamada.component.scss'
})
export class ChamadaComponent implements OnInit {
  private readonly presencaService = inject(PresencaService);
  private readonly alunoService = inject(AlunoService);
  private readonly usuarioService = inject(UsuarioService);
  private readonly notificacao = inject(NotificacaoService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  protected readonly sessao = inject(SessaoService);
  protected readonly segmentoService = inject(SegmentoService);

  readonly carregandoTurmas = signal(true);
  readonly carregando = signal(false);
  readonly salvando = signal(false);
  readonly historicoAberto = signal(false);

  readonly dataVisualizada = signal(hojeIso());
  readonly ehHoje = computed(() => this.dataVisualizada() === hojeIso());
  readonly calendarioAberto = signal(false);

  /** Estado editável da chamada (começa igual ao que está salvo e vai mudando até clicar em Salvar). */
  readonly itens = signal<ChamadaItem[]>([]);
  readonly registrada = signal(false);
  readonly alterada = signal(false);
  private readonly frequencias = signal<Map<string, FrequenciaAluno>>(new Map());

  /** Período da frequência mostrada ao lado de cada nome (independe do dia da chamada que está sendo feita). */
  readonly periodo = signal<PeriodoEscolhido>(periodoPadrao());

  readonly contagem = computed(() => {
    const lista = this.itens();
    return {
      presentes: lista.filter((i) => i.status === 'Presente').length,
      faltas: lista.filter((i) => i.status === 'Falta').length,
      justificadas: lista.filter((i) => i.status === 'Justificada').length,
      semMarca: lista.filter((i) => i.status === null).length
    };
  });

  protected readonly rotuloData = rotuloData;
  protected readonly hojeIso = hojeIso;
  protected readonly resolverFotoUrl = resolverFotoUrl;
  protected readonly opcoes: { valor: StatusPresenca; rotulo: string; classe: string }[] = [
    { valor: 'Presente', rotulo: 'Presente', classe: 'presente' },
    { valor: 'Falta', rotulo: 'Falta', classe: 'falta' },
    { valor: 'Justificada', rotulo: 'Justif.', classe: 'justificada' }
  ];
  protected readonly alertaFaltas = FALTAS_SEGUIDAS_PARA_ALERTA;

  ngOnInit(): void {
    const usuarioId = this.sessao.educadorId();
    if (!usuarioId) {
      this.router.navigateByUrl('/entrar');
      return;
    }

    if (this.sessao.turmas().length > 0) {
      this.carregandoTurmas.set(false);
      this.carregar();
      return;
    }

    // Mesma regra do Diário/Rotina: o educador vê as turmas dele; sem nenhuma (ou outro perfil de equipe), todas.
    this.usuarioService.listarTurmas(usuarioId).subscribe({
      next: (turmas) => {
        if (turmas.length > 0) {
          this.sessao.definirTurmas(turmas);
          this.carregandoTurmas.set(false);
          this.carregar();
        } else {
          this.carregarTodasTurmas();
        }
      },
      error: () => this.carregarTodasTurmas()
    });
  }

  private carregarTodasTurmas(): void {
    this.alunoService.listarTurmas().subscribe({
      next: (turmas) => {
        this.sessao.definirTurmas(turmas);
        this.carregandoTurmas.set(false);
        this.carregar();
      },
      error: () => this.carregandoTurmas.set(false)
    });
  }

  private carregar(): void {
    this.aplicarTurmaDaUrl();
    const turmaId = this.sessao.turmaAtivaId();
    if (!turmaId) return;

    this.carregando.set(true);
    this.presencaService.obterChamada(turmaId, this.dataVisualizada()).subscribe({
      next: (chamada) => {
        this.itens.set(chamada.itens);
        this.registrada.set(chamada.registrada);
        this.alterada.set(false);
        this.carregando.set(false);
      },
      error: () => {
        this.carregando.set(false);
        this.notificacao.erro('Não foi possível carregar a chamada.');
      }
    });
    this.carregarFrequencias(turmaId);
  }

  /** Atalhos do Dashboard abrem a chamada já na turma com o problema (/chamada?turmaId=…). Vale uma vez só. */
  private aplicarTurmaDaUrl(): void {
    const turmaId = this.route.snapshot.queryParamMap.get('turmaId');
    if (turmaId && !this.turmaDaUrlAplicada && this.sessao.turmas().some((t) => t.id === turmaId)) {
      this.sessao.definirTurmaAtiva(turmaId);
    }
    this.turmaDaUrlAplicada = true;
  }

  private turmaDaUrlAplicada = false;

  private carregarFrequencias(turmaId: string): void {
    this.presencaService.frequenciaDaTurma(turmaId, this.periodo()).subscribe({
      next: (lista) => this.frequencias.set(new Map(lista.map((f) => [f.alunoId, f]))),
      // A frequência é complemento: se falhar, a chamada em si continua funcionando.
      error: () => this.frequencias.set(new Map())
    });
  }

  aoMudarPeriodo(periodo: PeriodoEscolhido): void {
    this.periodo.set(periodo);
    const turmaId = this.sessao.turmaAtivaId();
    if (turmaId) this.carregarFrequencias(turmaId);
  }

  frequenciaDe(alunoId: string): FrequenciaAluno | undefined {
    return this.frequencias().get(alunoId);
  }

  classeFrequencia(percentual: number): string {
    if (percentual >= 85) return 'boa';
    if (percentual >= 70) return 'media';
    return 'baixa';
  }

  selecionarTurma(turmaId: string): void {
    this.sessao.definirTurmaAtiva(turmaId);
    this.carregar();
  }

  irParaDia(dataIso: string): void {
    this.dataVisualizada.set(dataIso);
    this.calendarioAberto.set(false);
    this.carregar();
  }

  diaAnterior(): void {
    this.irParaDia(somarDias(this.dataVisualizada(), -1));
  }

  diaSeguinte(): void {
    if (this.ehHoje()) return;
    this.irParaDia(somarDias(this.dataVisualizada(), 1));
  }

  marcar(alunoId: string, status: StatusPresenca): void {
    this.itens.update((lista) => lista.map((i) => (i.alunoId === alunoId ? { ...i, status } : i)));
    this.alterada.set(true);
  }

  marcarTodosPresentes(): void {
    this.itens.update((lista) => lista.map((i) => ({ ...i, status: 'Presente' as StatusPresenca })));
    this.alterada.set(true);
  }

  salvar(): void {
    const turmaId = this.sessao.turmaAtivaId();
    if (!turmaId) return;

    const itens = this.itens();
    if (itens.length === 0) {
      this.notificacao.erro('Esta turma não tem alunos ativos para fazer a chamada.');
      return;
    }
    const semMarca = itens.filter((i) => i.status === null).length;
    if (semMarca > 0) {
      this.notificacao.erro(
        `Marque a situação de todos antes de salvar (faltam ${semMarca}). Dica: use "Todos presentes" e ajuste só as exceções.`
      );
      return;
    }

    this.salvando.set(true);
    this.presencaService
      .salvarChamada(turmaId, {
        data: this.dataVisualizada(),
        itens: itens.map((i) => ({ alunoId: i.alunoId, status: i.status as StatusPresenca }))
      })
      .subscribe({
        next: (chamada) => {
          this.itens.set(chamada.itens);
          this.registrada.set(chamada.registrada);
          this.alterada.set(false);
          this.salvando.set(false);
          this.notificacao.sucesso('Chamada salva.');
          this.carregarFrequencias(turmaId);
        },
        error: (resposta) => {
          this.salvando.set(false);
          this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível salvar a chamada.');
        }
      });
  }

  abrirHistorico(): void {
    this.historicoAberto.set(true);
  }

  fecharHistorico(): void {
    this.historicoAberto.set(false);
  }
}
