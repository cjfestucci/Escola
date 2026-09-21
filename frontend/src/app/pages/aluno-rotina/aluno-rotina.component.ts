import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { Aluno } from '../../models/aluno.model';
import { FichaSaude } from '../../models/ficha-saude.model';
import {
  CategoriaRegistro,
  Humor,
  MAX_FOTOS_POR_REGISTRO,
  Refeicao,
  RegistroRotina,
  StatusAlimentacao,
  TipoHigiene
} from '../../models/registro-rotina.model';
import { AlunoService } from '../../services/aluno.service';
import { FichaSaudeService } from '../../services/ficha-saude.service';
import { RotinaService } from '../../services/rotina.service';
import { SessaoService } from '../../services/sessao.service';
import { UploadService } from '../../services/upload.service';
import { CalendarioComponent } from '../../shared/calendario/calendario.component';
import { hojeIso, rotuloData, somarDias } from '../../shared/data-utils';
import {
  horaRegistro,
  iconeCategoria,
  resolverFotoUrls,
  rotuloCategoria,
  rotuloCategoriaCurto
} from '../../shared/registro-rotina-display';
import { SeletorArquivoComponent } from '../../shared/seletor-arquivo/seletor-arquivo.component';
import { SeletorHorarioComponent } from '../../shared/seletor-horario/seletor-horario.component';

type AcaoRapida = CategoriaRegistro | null;

@Component({
  selector: 'app-aluno-rotina',
  imports: [FormsModule, CalendarioComponent, SeletorArquivoComponent, SeletorHorarioComponent],
  templateUrl: './aluno-rotina.component.html',
  styleUrl: './aluno-rotina.component.scss'
})
export class AlunoRotinaComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly alunoService = inject(AlunoService);
  private readonly fichaSaudeService = inject(FichaSaudeService);
  private readonly rotinaService = inject(RotinaService);
  private readonly sessao = inject(SessaoService);
  private readonly uploadService = inject(UploadService);

  private alunoId = '';

  readonly aluno = signal<Aluno | null>(null);
  readonly registros = signal<RegistroRotina[]>([]);
  readonly carregando = signal(true);

  readonly fichaSaude = signal<FichaSaude | null>(null);
  readonly alertasSaude = computed(() => {
    const f = this.fichaSaude();
    if (!f) return [];
    const alertas: { rotulo: string; texto: string }[] = [];
    if (f.alergias) alertas.push({ rotulo: 'Alergias', texto: f.alergias });
    if (f.restricoesAlimentares) alertas.push({ rotulo: 'Restrições alimentares', texto: f.restricoesAlimentares });
    if (f.medicamentosEmUso) alertas.push({ rotulo: 'Medicamentos em uso', texto: f.medicamentosEmUso });
    if (f.condicoesSaude) alertas.push({ rotulo: 'Condições de saúde', texto: f.condicoesSaude });
    return alertas;
  });

  readonly filtroCategoria = signal<CategoriaRegistro | null>(null);
  readonly registrosFiltrados = computed(() => {
    const categoria = this.filtroCategoria();
    return categoria ? this.registros().filter((r) => r.categoria === categoria) : this.registros();
  });

  readonly dataVisualizada = signal(hojeIso());
  readonly ehHoje = computed(() => this.dataVisualizada() === hojeIso());
  readonly calendarioAberto = signal(false);
  readonly enviando = signal(false);
  readonly excluindoId = signal<string | null>(null);
  readonly confirmandoExclusaoId = signal<string | null>(null);

  readonly acaoAtiva = signal<AcaoRapida>(null);
  readonly registroEmEdicaoId = signal<string | null>(null);
  readonly fotoUrls = signal<string[]>([]);
  readonly enviandoFoto = signal(false);

  readonly maxFotos = MAX_FOTOS_POR_REGISTRO;

  // campos do formulário rápido — só os relevantes para a ação ativa são usados
  observacao = '';
  refeicao: Refeicao = 'Almoco';
  statusAlimentacao: StatusAlimentacao = 'ComeuTudo';
  horaInicioSono = this.horaAtual();
  tipoHigiene: TipoHigiene = 'TrocaFralda';
  humor: Humor = 'Feliz';

  ngOnInit(): void {
    if (!this.sessao.educadorId()) {
      this.router.navigateByUrl('/entrar');
      return;
    }

    this.alunoId = this.route.snapshot.paramMap.get('id') ?? '';
    if (!this.alunoId) {
      this.router.navigateByUrl('/alunos');
      return;
    }

    this.alunoService.obterAluno(this.alunoId).subscribe((aluno) => this.aluno.set(aluno));
    this.fichaSaudeService.obter(this.alunoId).subscribe((ficha) => this.fichaSaude.set(ficha));
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

  irParaDia(dataIso: string): void {
    this.dataVisualizada.set(dataIso);
    this.calendarioAberto.set(false);
    this.fecharAcao();
    this.carregarTimeline();
  }

  diaAnterior(): void {
    this.irParaDia(somarDias(this.dataVisualizada(), -1));
  }

  diaSeguinte(): void {
    if (this.ehHoje()) return;
    this.irParaDia(somarDias(this.dataVisualizada(), 1));
  }

  abrirAcao(acao: CategoriaRegistro): void {
    const jaAberta = this.acaoAtiva() === acao && !this.registroEmEdicaoId();
    this.limparFormulario();
    this.acaoAtiva.set(jaAberta ? null : acao);
  }

  editarRegistro(registro: RegistroRotina): void {
    this.limparFormulario();
    this.acaoAtiva.set(registro.categoria);
    this.registroEmEdicaoId.set(registro.id);
    this.observacao = registro.observacao ?? '';
    this.fotoUrls.set([...registro.fotoUrls]);

    if (registro.refeicao) this.refeicao = registro.refeicao;
    if (registro.statusAlimentacao) this.statusAlimentacao = registro.statusAlimentacao;
    if (registro.horaInicio) this.horaInicioSono = registro.horaInicio.slice(0, 5);
    if (registro.tipoHigiene) this.tipoHigiene = registro.tipoHigiene;
    if (registro.humor) this.humor = registro.humor;
  }

  pedirConfirmacaoExclusao(registroId: string): void {
    this.confirmandoExclusaoId.set(registroId);
  }

  cancelarExclusao(): void {
    this.confirmandoExclusaoId.set(null);
  }

  confirmarExclusao(registro: RegistroRotina): void {
    const usuarioId = this.sessao.educadorId();
    if (!usuarioId) return;

    this.excluindoId.set(registro.id);
    this.rotinaService.excluir(this.alunoId, registro.id, usuarioId).subscribe({
      next: () => {
        this.registros.update((atual) => atual.filter((r) => r.id !== registro.id));
        this.excluindoId.set(null);
        this.confirmandoExclusaoId.set(null);
      },
      error: () => this.excluindoId.set(null)
    });
  }

  fecharAcao(): void {
    this.limparFormulario();
    this.acaoAtiva.set(null);
  }

  private limparFormulario(): void {
    this.registroEmEdicaoId.set(null);
    this.observacao = '';
    this.horaInicioSono = this.horaAtual();
    this.fotoUrls.set([]);
  }

  aoSelecionarFoto(arquivo: File): void {
    if (this.fotoUrls().length >= this.maxFotos) return;

    this.enviandoFoto.set(true);
    this.uploadService.enviarFoto(arquivo).subscribe({
      next: (resultado) => {
        this.fotoUrls.update((atual) => [...atual, resultado.url]);
        this.enviandoFoto.set(false);
      },
      error: () => this.enviandoFoto.set(false)
    });
  }

  removerFoto(url: string): void {
    this.fotoUrls.update((atual) => atual.filter((u) => u !== url));
  }

  voltar(): void {
    this.router.navigateByUrl('/alunos');
  }

  confirmar(): void {
    const usuarioId = this.sessao.educadorId();
    const acao = this.acaoAtiva();
    if (!usuarioId || !acao) return;

    this.enviando.set(true);
    const registroId = this.registroEmEdicaoId();
    const base = { usuarioId, observacao: this.observacao || null, fotoUrls: this.fotoUrls() };

    const requisicao$ = (() => {
      switch (acao) {
        case 'Alimentacao': {
          const payload = { ...base, refeicao: this.refeicao, status: this.statusAlimentacao };
          return registroId
            ? this.rotinaService.editarAlimentacao(this.alunoId, registroId, payload)
            : this.rotinaService.registrarAlimentacao(this.alunoId, payload);
        }
        case 'Sono': {
          const payload = { ...base, horaInicio: this.horaInicioSono };
          return registroId
            ? this.rotinaService.editarSono(this.alunoId, registroId, payload)
            : this.rotinaService.registrarSono(this.alunoId, payload);
        }
        case 'Higiene': {
          const payload = { ...base, tipo: this.tipoHigiene };
          return registroId
            ? this.rotinaService.editarHigiene(this.alunoId, registroId, payload)
            : this.rotinaService.registrarHigiene(this.alunoId, payload);
        }
        case 'Humor': {
          const payload = { ...base, humor: this.humor };
          return registroId
            ? this.rotinaService.editarHumor(this.alunoId, registroId, payload)
            : this.rotinaService.registrarHumor(this.alunoId, payload);
        }
        case 'Momento':
          return registroId
            ? this.rotinaService.editarMomento(this.alunoId, registroId, base)
            : this.rotinaService.registrarMomento(this.alunoId, base);
      }
    })();

    requisicao$.subscribe({
      next: (registro) => {
        this.registros.update((atual) =>
          registroId ? atual.map((r) => (r.id === registro.id ? registro : r)) : [registro, ...atual]
        );
        this.enviando.set(false);
        this.fecharAcao();
      },
      error: () => this.enviando.set(false)
    });
  }

  protected readonly rotuloCategoria = rotuloCategoria;
  protected readonly rotuloCategoriaCurto = rotuloCategoriaCurto;
  protected readonly iconeCategoria = iconeCategoria;
  protected readonly horaRegistro = horaRegistro;
  protected readonly resolverFotoUrls = resolverFotoUrls;
  protected readonly rotuloData = rotuloData;
  protected readonly hojeIso = hojeIso;
  protected readonly categoriasFiltro: CategoriaRegistro[] = ['Alimentacao', 'Sono', 'Higiene', 'Humor', 'Momento'];

  private horaAtual(): string {
    const agora = new Date();
    return `${String(agora.getHours()).padStart(2, '0')}:${String(agora.getMinutes()).padStart(2, '0')}`;
  }
}
