import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { ResponsavelResumo, SenhaGeradaResponsavel, Turma } from '../../models/aluno.model';
import { AlunoService } from '../../services/aluno.service';
import { TurmaService } from '../../services/turma.service';
import { UploadService } from '../../services/upload.service';
import { CalendarioComponent } from '../../shared/calendario/calendario.component';
import { formatarDataAbsoluta, hojeIso } from '../../shared/data-utils';
import { resolverFotoUrl } from '../../shared/registro-rotina-display';
import { SeletorArquivoComponent } from '../../shared/seletor-arquivo/seletor-arquivo.component';

function novoResponsavelVazio(): ResponsavelResumo {
  return { id: null, nome: '', email: '', telefone: null, responsavelFinanceiro: false };
}

@Component({
  selector: 'app-matricula-formulario',
  imports: [FormsModule, SeletorArquivoComponent, CalendarioComponent],
  templateUrl: './matricula-formulario.component.html',
  styleUrl: './matricula-formulario.component.scss'
})
export class MatriculaFormularioComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly alunoService = inject(AlunoService);
  private readonly turmaService = inject(TurmaService);
  private readonly uploadService = inject(UploadService);

  private alunoId: string | null = null;

  readonly turmas = signal<Turma[]>([]);
  readonly carregando = signal(true);
  readonly salvando = signal(false);
  readonly enviandoFoto = signal(false);
  readonly erro = signal<string | null>(null);
  readonly fotoUrl = signal<string | null>(null);
  readonly responsaveis = signal<ResponsavelResumo[]>([novoResponsavelVazio()]);
  readonly calendarioNascimentoAberto = signal(false);
  readonly senhasGeradas = signal<SenhaGeradaResponsavel[]>([]);

  nome = '';
  dataNascimento = '';
  turmaId = '';

  protected readonly resolverFotoUrl = resolverFotoUrl;
  protected readonly formatarDataAbsoluta = formatarDataAbsoluta;
  protected readonly hojeIso = hojeIso;

  get titulo(): string {
    return this.alunoId ? 'Editar aluno' : 'Novo aluno';
  }

  ngOnInit(): void {
    this.alunoId = this.route.snapshot.paramMap.get('id');

    this.turmaService.listar().subscribe((turmas) => this.turmas.set(turmas));

    if (this.alunoId) {
      this.alunoService.obterAluno(this.alunoId).subscribe({
        next: (aluno) => {
          this.nome = aluno.nome;
          this.dataNascimento = aluno.dataNascimento.slice(0, 10);
          this.turmaId = aluno.turmaId;
          this.fotoUrl.set(aluno.fotoUrl);
          this.responsaveis.set(aluno.responsaveis.length > 0 ? aluno.responsaveis : [novoResponsavelVazio()]);
          this.carregando.set(false);
        },
        error: () => {
          this.erro.set('Não foi possível carregar o aluno.');
          this.carregando.set(false);
        }
      });
    } else {
      this.carregando.set(false);
    }
  }

  selecionarNascimento(dataIso: string): void {
    this.dataNascimento = dataIso;
    this.calendarioNascimentoAberto.set(false);
  }

  aoSelecionarFoto(arquivo: File): void {
    this.enviandoFoto.set(true);
    this.uploadService.enviarFoto(arquivo).subscribe({
      next: (resultado) => {
        this.fotoUrl.set(resultado.url);
        this.enviandoFoto.set(false);
      },
      error: () => this.enviandoFoto.set(false)
    });
  }

  removerFoto(): void {
    this.fotoUrl.set(null);
  }

  adicionarResponsavel(): void {
    this.responsaveis.update((atual) => [...atual, novoResponsavelVazio()]);
  }

  removerResponsavel(indice: number): void {
    this.responsaveis.update((atual) => (atual.length > 1 ? atual.filter((_, i) => i !== indice) : atual));
  }

  atualizarResponsavel(indice: number, campo: keyof ResponsavelResumo, valor: string | boolean): void {
    this.responsaveis.update((atual) =>
      atual.map((r, i) => (i === indice ? { ...r, [campo]: valor } : r))
    );
  }

  salvar(): void {
    this.erro.set(null);

    if (!this.nome.trim()) {
      this.erro.set('Informe o nome do aluno.');
      return;
    }
    if (!this.dataNascimento) {
      this.erro.set('Informe a data de nascimento.');
      return;
    }
    if (!this.turmaId) {
      this.erro.set('Selecione a turma.');
      return;
    }
    const responsaveisValidos = this.responsaveis().filter((r) => r.nome.trim() && r.email.trim());
    if (responsaveisValidos.length === 0) {
      this.erro.set('Informe ao menos um responsável, com nome e e-mail.');
      return;
    }

    const payload = {
      nome: this.nome.trim(),
      dataNascimento: this.dataNascimento,
      turmaId: this.turmaId,
      fotoUrl: this.fotoUrl(),
      responsaveis: responsaveisValidos
    };

    this.salvando.set(true);
    const requisicao$ = this.alunoId
      ? this.alunoService.editar(this.alunoId, payload)
      : this.alunoService.criar(payload);

    requisicao$.subscribe({
      next: (resultado) => {
        if (resultado.senhasGeradas?.length > 0) {
          this.senhasGeradas.set(resultado.senhasGeradas);
          this.salvando.set(false);
        } else {
          this.router.navigateByUrl('/matricula');
        }
      },
      error: (resposta) => {
        this.salvando.set(false);
        this.erro.set(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível salvar o aluno.');
      }
    });
  }

  continuar(): void {
    this.router.navigateByUrl('/matricula');
  }

  cancelar(): void {
    this.router.navigateByUrl('/matricula');
  }
}
