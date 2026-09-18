import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { Aluno } from '../../models/aluno.model';
import {
  CategoriaRegistro,
  Humor,
  Refeicao,
  RegistroRotina,
  StatusAlimentacao,
  TipoHigiene
} from '../../models/registro-rotina.model';
import { AlunoService } from '../../services/aluno.service';
import { RotinaService } from '../../services/rotina.service';
import { SessaoService } from '../../services/sessao.service';
import { UploadService } from '../../services/upload.service';
import { horaRegistro, iconeCategoria, resolverFotoUrl, rotuloCategoria } from '../../shared/registro-rotina-display';

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
  readonly enviando = signal(false);

  readonly acaoAtiva = signal<AcaoRapida>(null);
  readonly fotoUrl = signal<string | null>(null);
  readonly enviandoFoto = signal(false);

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
    this.rotinaService.listarDoDia(this.alunoId).subscribe({
      next: (registros) => {
        this.registros.set(registros);
        this.carregando.set(false);
      },
      error: () => this.carregando.set(false)
    });
  }

  abrirAcao(acao: CategoriaRegistro): void {
    this.acaoAtiva.set(this.acaoAtiva() === acao ? null : acao);
    this.observacao = '';
    this.horaInicioSono = this.horaAtual();
    this.fotoUrl.set(null);
  }

  fecharAcao(): void {
    this.acaoAtiva.set(null);
    this.fotoUrl.set(null);
  }

  aoSelecionarFoto(event: Event): void {
    const arquivo = (event.target as HTMLInputElement).files?.[0];
    if (!arquivo) return;

    this.enviandoFoto.set(true);
    this.uploadService.enviarFoto(arquivo).subscribe({
      next: (resultado) => {
        this.fotoUrl.set(resultado.url);
        this.enviandoFoto.set(false);
      },
      error: () => this.enviandoFoto.set(false)
    });
  }

  voltar(): void {
    this.router.navigateByUrl('/alunos');
  }

  confirmar(): void {
    const usuarioId = this.sessao.educadorId();
    const acao = this.acaoAtiva();
    if (!usuarioId || !acao) return;

    this.enviando.set(true);
    const base = { usuarioId, observacao: this.observacao || null, fotoUrl: this.fotoUrl() };

    const requisicao$ = (() => {
      switch (acao) {
        case 'Alimentacao':
          return this.rotinaService.registrarAlimentacao(this.alunoId, {
            ...base,
            refeicao: this.refeicao,
            status: this.statusAlimentacao
          });
        case 'Sono':
          return this.rotinaService.registrarSono(this.alunoId, {
            ...base,
            horaInicio: this.horaInicioSono
          });
        case 'Higiene':
          return this.rotinaService.registrarHigiene(this.alunoId, {
            ...base,
            tipo: this.tipoHigiene
          });
        case 'Humor':
          return this.rotinaService.registrarHumor(this.alunoId, {
            ...base,
            humor: this.humor
          });
        case 'Momento':
          return this.rotinaService.registrarMomento(this.alunoId, base);
      }
    })();

    requisicao$.subscribe({
      next: (registro) => {
        this.registros.update((atual) => [registro, ...atual]);
        this.enviando.set(false);
        this.fecharAcao();
      },
      error: () => this.enviando.set(false)
    });
  }

  protected readonly rotuloCategoria = rotuloCategoria;
  protected readonly iconeCategoria = iconeCategoria;
  protected readonly horaRegistro = horaRegistro;
  protected readonly resolverFotoUrl = resolverFotoUrl;

  private horaAtual(): string {
    const agora = new Date();
    return `${String(agora.getHours()).padStart(2, '0')}:${String(agora.getMinutes()).padStart(2, '0')}`;
  }
}
