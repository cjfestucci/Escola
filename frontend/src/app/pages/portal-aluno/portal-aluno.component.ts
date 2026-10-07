import { NgTemplateOutlet } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { Aluno } from '../../models/aluno.model';
import { Cobranca, FormasPagamento } from '../../models/cobranca.model';
import { RegistroDiarioClasse } from '../../models/diario-classe.model';
import { CampeonatoFamilia, JogosCampeonatoDoAluno, LocalJogo, JogosDoAluno, ROTULOS_MANDO, ROTULOS_RESULTADO, resultadoDoJogo } from '../../models/competicao.model';
import { rotuloAtestado, situacaoAtestado } from '../../models/atestado.model';
import { FichaSaude } from '../../models/ficha-saude.model';
import { FrequenciaDoAluno, ROTULOS_PRESENCA } from '../../models/presenca.model';
import { CategoriaRegistro, RegistroRotina } from '../../models/registro-rotina.model';
import { AlunoService } from '../../services/aluno.service';
import { DiarioClasseService } from '../../services/diario-classe.service';
import { FichaSaudeService } from '../../services/ficha-saude.service';
import { FinanceiroService } from '../../services/financeiro.service';
import { JogoService } from '../../services/jogo.service';
import { NotificacaoService } from '../../services/notificacao.service';
import { SegmentoService } from '../../services/segmento.service';
import { PresencaService } from '../../services/presenca.service';
import { RotinaService } from '../../services/rotina.service';
import { SessaoService } from '../../services/sessao.service';
import { CalendarioComponent } from '../../shared/calendario/calendario.component';
import { FiltroPeriodoComponent, PeriodoEscolhido, periodoPadrao } from '../../shared/filtro-periodo/filtro-periodo.component';
import { DocumentosSaudeComponent } from '../../shared/documentos-saude/documentos-saude.component';
import { formatarDataAbsoluta, hojeIso, rotuloData, somarDias } from '../../shared/data-utils';
import { gerarQrCodePix } from '../../shared/pix-qrcode';
import {
  horaRegistro,
  iconeCategoria,
  resolverFotoUrls,
  rotuloCategoria,
  rotuloCategoriaCurto
} from '../../shared/registro-rotina-display';

@Component({
  selector: 'app-portal-aluno',
  imports: [CalendarioComponent, DocumentosSaudeComponent, NgTemplateOutlet, FiltroPeriodoComponent],
  templateUrl: './portal-aluno.component.html',
  styleUrl: './portal-aluno.component.scss'
})
export class PortalAlunoComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly alunoService = inject(AlunoService);
  private readonly fichaSaudeService = inject(FichaSaudeService);
  private readonly rotinaService = inject(RotinaService);
  private readonly diarioClasseService = inject(DiarioClasseService);
  private readonly financeiroService = inject(FinanceiroService);
  private readonly presencaService = inject(PresencaService);
  private readonly jogoService = inject(JogoService);
  protected readonly segmentoService = inject(SegmentoService);
  private readonly notificacao = inject(NotificacaoService);
  private readonly sessao = inject(SessaoService);

  protected alunoId = '';

  readonly aluno = signal<Aluno | null>(null);
  readonly registros = signal<RegistroRotina[]>([]);
  readonly carregando = signal(true);

  readonly fichaSaude = signal<FichaSaude | null>(null);
  readonly mostrarFicha = signal(false);
  protected readonly rotuloAtestado = rotuloAtestado;
  /** Aviso no topo só quando o atestado informado venceu ou vence em breve — sem data informada não incomoda a família. */
  readonly avisoAtestado = computed(() => {
    const validoAte = this.fichaSaude()?.atestadoValidoAte;
    const situacao = situacaoAtestado(validoAte);
    if (situacao !== 'vencido' && situacao !== 'vencendo') return null;
    return { vencido: situacao === 'vencido', texto: rotuloAtestado(validoAte) };
  });

  readonly fichaPreenchida = computed(() => {
    const f = this.fichaSaude();
    if (!f) return false;
    return !!(
      f.tipoSanguineo ||
      f.alergias ||
      f.restricoesAlimentares ||
      f.medicamentosEmUso ||
      f.condicoesSaude ||
      f.planoSaude ||
      f.pediatraNome ||
      f.contatoEmergenciaNome ||
      f.atestadoValidoAte
    );
  });

  readonly filtroCategoria = signal<CategoriaRegistro | null>(null);
  readonly registrosFiltrados = computed(() => {
    const categoria = this.filtroCategoria();
    return categoria ? this.registros().filter((r) => r.categoria === categoria) : this.registros();
  });

  readonly dataVisualizada = signal(hojeIso());
  readonly ehHoje = computed(() => this.dataVisualizada() === hojeIso());
  readonly calendarioAberto = signal(false);

  readonly registrosDiario = signal<RegistroDiarioClasse[]>([]);

  readonly jogos = signal<JogosDoAluno | null>(null);
  readonly mostrarJogos = signal(false);
  readonly campeonatos = signal<CampeonatoFamilia[]>([]);
  readonly campeonatoEscolhidoId = signal('');
  readonly jogosDoCampeonato = signal<JogosCampeonatoDoAluno | null>(null);
  readonly carregandoCampeonato = signal(false);
  protected rotuloMando(mando: LocalJogo): string {
    return ROTULOS_MANDO[mando];
  }
  protected readonly rotulosResultado = ROTULOS_RESULTADO;
  protected readonly resultadoDoJogo = resultadoDoJogo;

  aoMudarPeriodoFrequencia(periodo: PeriodoEscolhido): void {
    this.periodoFrequencia.set(periodo);
    this.presencaService.frequenciaDoAluno(this.alunoId, periodo).subscribe({
      next: (f) => this.frequencia.set(f),
      error: () => this.notificacao.erro('Não foi possível carregar a frequência desse período.')
    });
  }

  /** Escolhe um campeonato no seletor e carrega o calendário completo do time do filho nele ('' = fechar). */
  escolherCampeonato(campeonatoId: string): void {
    this.campeonatoEscolhidoId.set(campeonatoId);
    this.jogosDoCampeonato.set(null);
    if (!campeonatoId) return;

    this.carregandoCampeonato.set(true);
    this.jogoService.jogosDoCampeonatoDoAluno(this.alunoId, campeonatoId).subscribe({
      next: (dados) => {
        this.jogosDoCampeonato.set(dados);
        this.carregandoCampeonato.set(false);
      },
      error: () => {
        this.carregandoCampeonato.set(false);
        this.notificacao.erro('Não foi possível carregar os jogos do campeonato.');
      }
    });
  }

  readonly frequencia = signal<FrequenciaDoAluno | null>(null);
  readonly mostrarFrequencia = signal(false);
  /** O painel só existe pra quem já tem alguma chamada (decidido na 1ª carga, no período padrão): filtrar um período
   * sem chamadas não pode fazer o painel — e o filtro — sumirem. */
  readonly temFrequencia = signal(false);
  readonly periodoFrequencia = signal<PeriodoEscolhido>(periodoPadrao());
  protected readonly rotulosPresenca = ROTULOS_PRESENCA;

  readonly cobrancas = signal<Cobranca[]>([]);
  readonly mostrarFinanceiro = signal(false);
  readonly cobrancasPendentes = computed(() => this.cobrancas().filter((c) => !c.paga).length);

  readonly pixAbertoId = signal<string | null>(null);
  readonly pixCarregando = signal(false);
  readonly pixCodigo = signal<string | null>(null);
  readonly pixAutomatico = signal(false);
  readonly pixQrCode = signal<string | null>(null);
  readonly pixCopiado = signal(false);

  protected readonly rotuloCategoria = rotuloCategoria;
  protected readonly rotuloCategoriaCurto = rotuloCategoriaCurto;
  protected readonly iconeCategoria = iconeCategoria;
  protected readonly horaRegistro = horaRegistro;
  protected readonly resolverFotoUrls = resolverFotoUrls;
  protected readonly categoriasFiltro: CategoriaRegistro[] = ['Alimentacao', 'Sono', 'Higiene', 'Humor', 'Momento'];
  protected readonly rotuloData = rotuloData;
  protected readonly hojeIso = hojeIso;
  protected readonly formatarDataAbsoluta = formatarDataAbsoluta;

  /** Formas de pagamento ativas na escola: só elas são oferecidas (nulo até carregar, pra não piscar uma opção desligada). */
  readonly formas = signal<FormasPagamento | null>(null);

  ngOnInit(): void {
    this.financeiroService.formasPagamento().subscribe({ next: (f) => this.formas.set(f), error: () => undefined });
    if (!this.sessao.responsavelId()) {
      this.router.navigateByUrl('/portal');
      return;
    }

    this.alunoId = this.route.snapshot.paramMap.get('id') ?? '';
    if (!this.alunoId) {
      this.router.navigateByUrl('/portal/filhos');
      return;
    }

    this.alunoService.obterAluno(this.alunoId).subscribe((aluno) => {
      this.aluno.set(aluno);
      this.carregarDiario();
    });
    this.fichaSaudeService.obter(this.alunoId).subscribe((ficha) => this.fichaSaude.set(ficha));
    this.financeiroService.listarDoAluno(this.alunoId).subscribe((cobrancas) => this.cobrancas.set(cobrancas));
    if (this.segmentoService.mostrarCompeticoes()) {
      this.jogoService.listarDoAluno(this.alunoId).subscribe({ next: (j) => this.jogos.set(j), error: () => undefined });
      this.jogoService.listarCampeonatosDoAluno(this.alunoId).subscribe({ next: (c) => this.campeonatos.set(c), error: () => undefined });
    }
    // Complemento: se a frequência não carregar, o resto da tela segue normal.
    this.presencaService.frequenciaDoAluno(this.alunoId, this.periodoFrequencia()).subscribe({
      next: (f) => {
        this.frequencia.set(f);
        this.temFrequencia.set(f.recentes.length > 0);
      },
      error: () => undefined
    });
    this.carregarTimeline();
  }

  private carregarTimeline(): void {
    if (!this.segmentoService.mostrarRotinaDiaria()) return;
    this.carregando.set(true);
    this.rotinaService.listarDoDia(this.alunoId, this.dataVisualizada()).subscribe({
      next: (registros) => {
        this.registros.set(registros);
        this.carregando.set(false);
      },
      error: () => this.carregando.set(false)
    });
  }

  private carregarDiario(): void {
    if (!this.segmentoService.mostrarDiarioClasse()) return;
    const turmaId = this.aluno()?.turmaId;
    if (!turmaId) return;

    this.diarioClasseService.listarDoDia(turmaId, this.dataVisualizada()).subscribe({
      next: (registros) => this.registrosDiario.set(registros),
      error: () => this.registrosDiario.set([])
    });
  }

  irParaDia(dataIso: string): void {
    this.dataVisualizada.set(dataIso);
    this.calendarioAberto.set(false);
    this.carregarTimeline();
    this.carregarDiario();
  }

  diaAnterior(): void {
    this.irParaDia(somarDias(this.dataVisualizada(), -1));
  }

  diaSeguinte(): void {
    if (this.ehHoje()) return;
    this.irParaDia(somarDias(this.dataVisualizada(), 1));
  }

  voltar(): void {
    this.router.navigateByUrl('/portal/filhos');
  }

  abrirPix(cobranca: Cobranca): void {
    this.pixAbertoId.set(cobranca.id);
    this.pixCodigo.set(null);
    this.pixAutomatico.set(false);
    this.pixQrCode.set(null);
    this.pixCopiado.set(false);
    this.pixCarregando.set(true);
    this.financeiroService.obterPix(cobranca.id).subscribe({
      next: (resposta) => {
        this.pixCodigo.set(resposta.codigoCopiaECola);
        this.pixAutomatico.set(resposta.automatico);
        this.pixCarregando.set(false);
        gerarQrCodePix(resposta.codigoCopiaECola).then((url) => this.pixQrCode.set(url));
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
}
