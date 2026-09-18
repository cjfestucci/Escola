import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { Periodo, Turma } from '../../models/aluno.model';
import { TurmaService } from '../../services/turma.service';

const ROTULO_PERIODO: Record<string, string> = {
  Manha: 'Manhã',
  Tarde: 'Tarde',
  Integral: 'Integral',
  Noite: 'Noite'
};

const SEM_PROFESSOR = 'sem-professor';

interface OpcaoProfessor {
  id: string;
  nome: string;
}

@Component({
  selector: 'app-turmas-lista',
  imports: [FormsModule],
  templateUrl: './turmas-lista.component.html',
  styleUrl: './turmas-lista.component.scss'
})
export class TurmasListaComponent implements OnInit {
  private readonly turmaService = inject(TurmaService);
  private readonly router = inject(Router);

  readonly turmas = signal<Turma[]>([]);
  readonly carregando = signal(true);
  readonly erro = signal<string | null>(null);
  readonly confirmandoExclusaoId = signal<string | null>(null);
  readonly excluindoId = signal<string | null>(null);

  readonly filtroNome = signal('');
  readonly filtroPeriodo = signal<Periodo | ''>('');
  readonly filtroProfessorId = signal('');

  protected readonly periodos: Periodo[] = ['Manha', 'Tarde', 'Integral', 'Noite'];
  protected readonly semProfessor = SEM_PROFESSOR;

  readonly professoresDisponiveis = computed<OpcaoProfessor[]>(() => {
    const porId = new Map<string, string>();
    for (const turma of this.turmas()) {
      if (turma.professorId) porId.set(turma.professorId, turma.professorNome ?? '');
    }
    return [...porId.entries()]
      .map(([id, nome]) => ({ id, nome }))
      .sort((a, b) => a.nome.localeCompare(b.nome, 'pt-BR'));
  });

  readonly turmasFiltradas = computed(() => {
    const nome = this.filtroNome().trim().toLowerCase();
    const periodo = this.filtroPeriodo();
    const professorId = this.filtroProfessorId();

    return this.turmas().filter((turma) => {
      const bateNome = !nome || turma.nome.toLowerCase().includes(nome);
      const batePeriodo = !periodo || turma.periodo === periodo;
      const bateProfessor =
        !professorId ||
        (professorId === SEM_PROFESSOR ? !turma.professorId : turma.professorId === professorId);
      return bateNome && batePeriodo && bateProfessor;
    });
  });

  ngOnInit(): void {
    this.carregar();
  }

  limparFiltros(): void {
    this.filtroNome.set('');
    this.filtroPeriodo.set('');
    this.filtroProfessorId.set('');
  }

  private carregar(): void {
    this.carregando.set(true);
    this.turmaService.listar().subscribe({
      next: (turmas) => {
        this.turmas.set(turmas);
        this.carregando.set(false);
      },
      error: () => this.carregando.set(false)
    });
  }

  rotuloPeriodo(periodo: string): string {
    return ROTULO_PERIODO[periodo] ?? periodo;
  }

  nova(): void {
    this.router.navigateByUrl('/turmas/nova');
  }

  editar(turma: Turma): void {
    this.router.navigate(['/turmas', turma.id, 'editar']);
  }

  pedirConfirmacaoExclusao(turmaId: string): void {
    this.erro.set(null);
    this.confirmandoExclusaoId.set(turmaId);
  }

  cancelarExclusao(): void {
    this.confirmandoExclusaoId.set(null);
  }

  confirmarExclusao(turma: Turma): void {
    this.excluindoId.set(turma.id);
    this.turmaService.excluir(turma.id).subscribe({
      next: () => {
        this.turmas.update((atual) => atual.filter((t) => t.id !== turma.id));
        this.excluindoId.set(null);
        this.confirmandoExclusaoId.set(null);
      },
      error: (resposta) => {
        this.excluindoId.set(null);
        this.confirmandoExclusaoId.set(null);
        this.erro.set(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível excluir a turma.');
      }
    });
  }
}
