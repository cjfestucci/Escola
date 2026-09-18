import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';

import { Aluno } from '../../models/aluno.model';
import { ResumoDashboard } from '../../models/resumo-dashboard.model';
import { AlunoService } from '../../services/aluno.service';
import { DashboardService } from '../../services/dashboard.service';
import { SessaoService } from '../../services/sessao.service';
import { UsuarioService } from '../../services/usuario.service';
import { idadeFormatada } from '../../shared/data-utils';

@Component({
  selector: 'app-alunos-lista',
  imports: [],
  templateUrl: './alunos-lista.component.html',
  styleUrl: './alunos-lista.component.scss'
})
export class AlunosListaComponent implements OnInit {
  private readonly alunoService = inject(AlunoService);
  private readonly dashboardService = inject(DashboardService);
  private readonly usuarioService = inject(UsuarioService);
  protected readonly sessao = inject(SessaoService);
  private readonly router = inject(Router);

  readonly alunos = signal<Aluno[]>([]);
  readonly resumo = signal<ResumoDashboard | null>(null);
  readonly carregando = signal(true);

  ngOnInit(): void {
    const usuarioId = this.sessao.educadorId();
    if (!usuarioId) {
      this.router.navigateByUrl('/entrar');
      return;
    }

    if (this.sessao.turmas().length === 0) {
      this.usuarioService.listarTurmas(usuarioId).subscribe({
        next: (turmas) => {
          this.sessao.definirTurmas(turmas);
          this.carregarLista();
        },
        error: () => this.carregarLista()
      });
    } else {
      this.carregarLista();
    }
  }

  private carregarLista(): void {
    const turmaId = this.sessao.turmaAtivaId() ?? undefined;

    this.carregando.set(true);
    this.alunoService.listarAlunos(turmaId).subscribe({
      next: (alunos) => {
        this.alunos.set(alunos);
        this.carregando.set(false);
      },
      error: () => this.carregando.set(false)
    });

    this.dashboardService.resumo(turmaId).subscribe((resumo) => this.resumo.set(resumo));
  }

  selecionarTurma(turmaId: string): void {
    this.sessao.definirTurmaAtiva(turmaId);
    this.carregarLista();
  }

  abrir(aluno: Aluno): void {
    this.router.navigate(['/alunos', aluno.id]);
  }

  protected readonly idade = idadeFormatada;
}
