import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { Aluno } from '../../models/aluno.model';
import { Cobranca } from '../../models/cobranca.model';
import { RegistroDiarioClasse } from '../../models/diario-classe.model';
import { FichaSaude } from '../../models/ficha-saude.model';
import { CategoriaRegistro, RegistroRotina } from '../../models/registro-rotina.model';
import { AlunoService } from '../../services/aluno.service';
import { DiarioClasseService } from '../../services/diario-classe.service';
import { FichaSaudeService } from '../../services/ficha-saude.service';
import { FinanceiroService } from '../../services/financeiro.service';
import { NotificacaoService } from '../../services/notificacao.service';
import { RotinaService } from '../../services/rotina.service';
import { SessaoService } from '../../services/sessao.service';
import { CalendarioComponent } from '../../shared/calendario/calendario.component';
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
  imports: [CalendarioComponent, DocumentosSaudeComponent],
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
  private readonly notificacao = inject(NotificacaoService);
  private readonly sessao = inject(SessaoService);

  protected alunoId = '';

  readonly aluno = signal<Aluno | null>(null);
  readonly registros = signal<RegistroRotina[]>([]);
  readonly carregando = signal(true);

  readonly fichaSaude = signal<FichaSaude | null>(null);
  readonly mostrarFicha = signal(false);
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
      f.contatoEmergenciaNome
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

  readonly cobrancas = signal<Cobranca[]>([]);
  readonly mostrarFinanceiro = signal(false);
  readonly cobrancasPendentes = computed(() => this.cobrancas().filter((c) => !c.paga).length);

  readonly pixAbertoId = signal<string | null>(null);
  readonly pixCarregando = signal(false);
  readonly pixCodigo = signal<string | null>(null);
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

  ngOnInit(): void {
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
    this.carregarTimeline();
  }

  private carregarTimeline(): void {
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
    this.pixQrCode.set(null);
    this.pixCopiado.set(false);
    this.pixCarregando.set(true);
    this.financeiroService.obterPix(cobranca.id).subscribe({
      next: (resposta) => {
        this.pixCodigo.set(resposta.codigoCopiaECola);
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
