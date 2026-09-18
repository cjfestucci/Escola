import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { Aluno } from '../../models/aluno.model';
import { AlunoService } from '../../services/aluno.service';
import { idadeFormatada } from '../../shared/data-utils';

interface OpcaoTurma {
  id: string;
  nome: string;
}

@Component({
  selector: 'app-matricula-lista',
  imports: [FormsModule],
  templateUrl: './matricula-lista.component.html',
  styleUrl: './matricula-lista.component.scss'
})
export class MatriculaListaComponent implements OnInit {
  private readonly alunoService = inject(AlunoService);
  private readonly router = inject(Router);

  readonly alunos = signal<Aluno[]>([]);
  readonly carregando = signal(true);
  readonly erro = signal<string | null>(null);
  readonly confirmandoExclusaoId = signal<string | null>(null);
  readonly excluindoId = signal<string | null>(null);

  readonly filtroNome = signal('');
  readonly filtroTurmaId = signal('');

  readonly turmasDisponiveis = computed<OpcaoTurma[]>(() => {
    const porId = new Map<string, string>();
    for (const aluno of this.alunos()) porId.set(aluno.turmaId, aluno.turmaNome);
    return [...porId.entries()]
      .map(([id, nome]) => ({ id, nome }))
      .sort((a, b) => a.nome.localeCompare(b.nome, 'pt-BR'));
  });

  readonly alunosFiltrados = computed(() => {
    const nome = this.filtroNome().trim().toLowerCase();
    const turmaId = this.filtroTurmaId();

    return this.alunos().filter((aluno) => {
      const bateNome = !nome || aluno.nome.toLowerCase().includes(nome);
      const bateTurma = !turmaId || aluno.turmaId === turmaId;
      return bateNome && bateTurma;
    });
  });

  protected readonly idade = idadeFormatada;

  ngOnInit(): void {
    this.carregar();
  }

  private carregar(): void {
    this.carregando.set(true);
    this.alunoService.listarAlunos().subscribe({
      next: (alunos) => {
        this.alunos.set(alunos);
        this.carregando.set(false);
      },
      error: () => this.carregando.set(false)
    });
  }

  limparFiltros(): void {
    this.filtroNome.set('');
    this.filtroTurmaId.set('');
  }

  novo(): void {
    this.router.navigateByUrl('/matricula/novo');
  }

  editar(aluno: Aluno): void {
    this.router.navigate(['/matricula', aluno.id, 'editar']);
  }

  pedirConfirmacaoExclusao(alunoId: string): void {
    this.erro.set(null);
    this.confirmandoExclusaoId.set(alunoId);
  }

  cancelarExclusao(): void {
    this.confirmandoExclusaoId.set(null);
  }

  confirmarExclusao(aluno: Aluno): void {
    this.excluindoId.set(aluno.id);
    this.alunoService.excluir(aluno.id).subscribe({
      next: () => {
        this.alunos.update((atual) => atual.filter((a) => a.id !== aluno.id));
        this.excluindoId.set(null);
        this.confirmandoExclusaoId.set(null);
      },
      error: (resposta) => {
        this.excluindoId.set(null);
        this.confirmandoExclusaoId.set(null);
        this.erro.set(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível excluir o aluno.');
      }
    });
  }
}
