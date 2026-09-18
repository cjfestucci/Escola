import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { Aluno } from '../../models/aluno.model';
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
import { RotinaService } from '../../services/rotina.service';
import { SessaoService } from '../../services/sessao.service';
import { UploadService } from '../../services/upload.service';
import { hojeIso, rotuloData, somarDias } from '../../shared/data-utils';
import { horaRegistro, iconeCategoria, resolverFotoUrls, rotuloCategoria } from '../../shared/registro-rotina-display';

type AcaoRapida = CategoriaRegistro | null;

@Component({
  selector: 'app-aluno-rotina',
  imports: [FormsModule],
  templateUrl: './aluno-rotina.component.html',
  styleUrl: './aluno-rotina.component.scss'
})
export class AlunoRotinaComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly alunoService = inject(AlunoService);
  private readonly rotinaService = inject(RotinaService);
  private readonly sessao = inject(SessaoService);
  private readonly uploadService = inject(UploadService);

  private alunoId = '';

  readonly aluno = signal<Aluno | null>(null);
  readonly registros = signal<RegistroRotina[]>([]);
  readonly carregando = signal(true);

  readonly dataVisualizada = signal(hojeIso());
  readonly ehHoje = computed(() => this.dataVisualizada() === hojeIso());
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

  aoEscolherData(event: Event): void {
    const valor = (event.target as HTMLInputElement).value;
    if (valor) this.irParaDia(valor);
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

  aoSelecionarFoto(event: Event): void {
    const input = event.target as HTMLInputElement;
    const arquivo = input.files?.[0];
    input.value = '';
    if (!arquivo || this.fotoUrls().length >= this.maxFotos) return;

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
  protected readonly iconeCategoria = iconeCategoria;
  protected readonly horaRegistro = horaRegistro;
  protected readonly resolverFotoUrls = resolverFotoUrls;
  protected readonly rotuloData = rotuloData;
  protected readonly hojeIso = hojeIso;

  private horaAtual(): string {
    const agora = new Date();
    return `${String(agora.getHours()).padStart(2, '0')}:${String(agora.getMinutes()).padStart(2, '0')}`;
  }
}
