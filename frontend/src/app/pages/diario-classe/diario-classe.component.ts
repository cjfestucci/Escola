import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { CriarOuEditarRegistroDiarioClasse, RegistroDiarioClasse } from '../../models/diario-classe.model';
import { AlunoService } from '../../services/aluno.service';
import { DiarioClasseService } from '../../services/diario-classe.service';
import { SessaoService } from '../../services/sessao.service';
import { UploadService } from '../../services/upload.service';
import { UsuarioService } from '../../services/usuario.service';
import { CalendarioComponent } from '../../shared/calendario/calendario.component';
import { hojeIso, rotuloData, somarDias } from '../../shared/data-utils';
import { horaRegistro, resolverFotoUrls } from '../../shared/registro-rotina-display';
import { SeletorArquivoComponent } from '../../shared/seletor-arquivo/seletor-arquivo.component';

const MAX_FOTOS = 4;

@Component({
  selector: 'app-diario-classe',
  imports: [FormsModule, CalendarioComponent, SeletorArquivoComponent],
  templateUrl: './diario-classe.component.html',
  styleUrl: './diario-classe.component.scss'
})
export class DiarioClasseComponent implements OnInit {
  private readonly diarioService = inject(DiarioClasseService);
  private readonly alunoService = inject(AlunoService);
  private readonly usuarioService = inject(UsuarioService);
  private readonly uploadService = inject(UploadService);
  protected readonly sessao = inject(SessaoService);
  private readonly router = inject(Router);

  readonly registros = signal<RegistroDiarioClasse[]>([]);
  readonly carregando = signal(true);
  readonly carregandoTurmas = signal(true);

  readonly dataVisualizada = signal(hojeIso());
  readonly ehHoje = computed(() => this.dataVisualizada() === hojeIso());
  readonly calendarioAberto = signal(false);

  readonly formularioAberto = signal(false);
  readonly registroEmEdicaoId = signal<string | null>(null);
  readonly enviando = signal(false);
  readonly excluindoId = signal<string | null>(null);
  readonly confirmandoExclusaoId = signal<string | null>(null);

  readonly fotoUrls = signal<string[]>([]);
  readonly enviandoFoto = signal(false);
  readonly maxFotos = MAX_FOTOS;

  titulo = '';
  descricao = '';

  protected readonly horaRegistro = horaRegistro;
  protected readonly resolverFotoUrls = resolverFotoUrls;
  protected readonly rotuloData = rotuloData;
  protected readonly hojeIso = hojeIso;

  ngOnInit(): void {
    const usuarioId = this.sessao.educadorId();
    if (!usuarioId) {
      this.router.navigateByUrl('/entrar');
      return;
    }

    if (this.sessao.turmas().length === 0) {
      this.usuarioService.listarTurmas(usuarioId).subscribe({
        next: (turmas) => {
          if (turmas.length > 0) {
            this.sessao.definirTurmas(turmas);
            this.carregandoTurmas.set(false);
            this.carregarTimeline();
          } else {
            this.carregarTodasTurmas();
          }
        },
        error: () => this.carregarTodasTurmas()
      });
    } else {
      this.carregandoTurmas.set(false);
      this.carregarTimeline();
    }
  }

  private carregarTodasTurmas(): void {
    this.alunoService.listarTurmas().subscribe({
      next: (turmas) => {
        this.sessao.definirTurmas(turmas);
        this.carregandoTurmas.set(false);
        this.carregarTimeline();
      },
      error: () => {
        this.carregandoTurmas.set(false);
        this.carregarTimeline();
      }
    });
  }

  private carregarTimeline(): void {
    const turmaId = this.sessao.turmaAtivaId();
    if (!turmaId) {
      this.carregando.set(false);
      return;
    }

    this.carregando.set(true);
    this.diarioService.listarDoDia(turmaId, this.dataVisualizada()).subscribe({
      next: (registros) => {
        this.registros.set(registros);
        this.carregando.set(false);
      },
      error: () => this.carregando.set(false)
    });
  }

  selecionarTurma(turmaId: string): void {
    this.sessao.definirTurmaAtiva(turmaId);
    this.fecharFormulario();
    this.carregarTimeline();
  }

  irParaDia(dataIso: string): void {
    this.dataVisualizada.set(dataIso);
    this.calendarioAberto.set(false);
    this.fecharFormulario();
    this.carregarTimeline();
  }

  diaAnterior(): void {
    this.irParaDia(somarDias(this.dataVisualizada(), -1));
  }

  diaSeguinte(): void {
    if (this.ehHoje()) return;
    this.irParaDia(somarDias(this.dataVisualizada(), 1));
  }

  abrirNovo(): void {
    this.limparFormulario();
    this.formularioAberto.set(true);
  }

  editarRegistro(registro: RegistroDiarioClasse): void {
    this.registroEmEdicaoId.set(registro.id);
    this.titulo = registro.titulo;
    this.descricao = registro.descricao ?? '';
    this.fotoUrls.set([...registro.fotoUrls]);
    this.formularioAberto.set(true);
  }

  fecharFormulario(): void {
    this.limparFormulario();
    this.formularioAberto.set(false);
  }

  private limparFormulario(): void {
    this.registroEmEdicaoId.set(null);
    this.titulo = '';
    this.descricao = '';
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

  confirmar(): void {
    const usuarioId = this.sessao.educadorId();
    const turmaId = this.sessao.turmaAtivaId();
    if (!usuarioId || !turmaId || !this.titulo.trim()) return;

    this.enviando.set(true);
    const registroId = this.registroEmEdicaoId();
    const payload: CriarOuEditarRegistroDiarioClasse = {
      usuarioId,
      titulo: this.titulo.trim(),
      descricao: this.descricao.trim() || null,
      fotoUrls: this.fotoUrls()
    };

    const requisicao$ = registroId
      ? this.diarioService.editar(turmaId, registroId, payload)
      : this.diarioService.criar(turmaId, payload);

    requisicao$.subscribe({
      next: (registro) => {
        this.registros.update((atual) =>
          registroId ? atual.map((r) => (r.id === registro.id ? registro : r)) : [registro, ...atual]
        );
        this.enviando.set(false);
        this.fecharFormulario();
      },
      error: () => this.enviando.set(false)
    });
  }

  pedirConfirmacaoExclusao(registroId: string): void {
    this.confirmandoExclusaoId.set(registroId);
  }

  cancelarExclusao(): void {
    this.confirmandoExclusaoId.set(null);
  }

  confirmarExclusao(registro: RegistroDiarioClasse): void {
    const usuarioId = this.sessao.educadorId();
    const turmaId = this.sessao.turmaAtivaId();
    if (!usuarioId || !turmaId) return;

    this.excluindoId.set(registro.id);
    this.diarioService.excluir(turmaId, registro.id, usuarioId).subscribe({
      next: () => {
        this.registros.update((atual) => atual.filter((r) => r.id !== registro.id));
        this.excluindoId.set(null);
        this.confirmandoExclusaoId.set(null);
      },
      error: () => this.excluindoId.set(null)
    });
  }
}
