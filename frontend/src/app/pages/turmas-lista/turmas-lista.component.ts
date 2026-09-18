import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';

import { Turma } from '../../models/aluno.model';
import { TurmaService } from '../../services/turma.service';

const ROTULO_PERIODO: Record<string, string> = {
  Manha: 'Manhã',
  Tarde: 'Tarde',
  Integral: 'Integral',
  Noite: 'Noite'
};

@Component({
  selector: 'app-turmas-lista',
  imports: [],
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

  ngOnInit(): void {
    this.carregar();
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
