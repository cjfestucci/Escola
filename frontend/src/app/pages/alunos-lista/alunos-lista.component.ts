import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';

import { Aluno } from '../../models/aluno.model';
import { ResumoDashboard } from '../../models/resumo-dashboard.model';
import { AlunoService } from '../../services/aluno.service';
import { DashboardService } from '../../services/dashboard.service';
import { SessaoService } from '../../services/sessao.service';

@Component({
  selector: 'app-alunos-lista',
  imports: [],
  templateUrl: './alunos-lista.component.html',
  styleUrl: './alunos-lista.component.scss'
})
export class AlunosListaComponent implements OnInit {
  private readonly alunoService = inject(AlunoService);
  private readonly dashboardService = inject(DashboardService);
  private readonly sessao = inject(SessaoService);
  private readonly router = inject(Router);

  readonly alunos = signal<Aluno[]>([]);
  readonly resumo = signal<ResumoDashboard | null>(null);
  readonly carregando = signal(true);

  ngOnInit(): void {
    if (!this.sessao.educadorId()) {
      this.router.navigateByUrl('/entrar');
      return;
    }

    this.alunoService.listarAlunos().subscribe({
      next: (alunos) => {
        this.alunos.set(alunos);
        this.carregando.set(false);
      },
      error: () => this.carregando.set(false)
    });

    this.dashboardService.resumo().subscribe((resumo) => this.resumo.set(resumo));
  }

  abrir(aluno: Aluno): void {
    this.router.navigate(['/alunos', aluno.id]);
  }

  idade(dataNascimento: string): string {
    const nascimento = new Date(dataNascimento);
    const hoje = new Date();
    let meses = (hoje.getFullYear() - nascimento.getFullYear()) * 12 + (hoje.getMonth() - nascimento.getMonth());
    if (hoje.getDate() < nascimento.getDate()) meses--;

    if (meses < 24) return `${meses} meses`;
    return `${Math.floor(meses / 12)} anos`;
  }
}
