import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { Aluno } from '../../models/aluno.model';
import { RegistroRotina } from '../../models/registro-rotina.model';
import { AlunoService } from '../../services/aluno.service';
import { RotinaService } from '../../services/rotina.service';
import { SessaoService } from '../../services/sessao.service';
import { horaRegistro, iconeCategoria, resolverFotoUrl, rotuloCategoria } from '../../shared/registro-rotina-display';

@Component({
  selector: 'app-portal-aluno',
  imports: [],
  templateUrl: './portal-aluno.component.html',
  styleUrl: './portal-aluno.component.scss'
})
export class PortalAlunoComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly alunoService = inject(AlunoService);
  private readonly rotinaService = inject(RotinaService);
  private readonly sessao = inject(SessaoService);

  private alunoId = '';

  readonly aluno = signal<Aluno | null>(null);
  readonly registros = signal<RegistroRotina[]>([]);
  readonly carregando = signal(true);

  protected readonly rotuloCategoria = rotuloCategoria;
  protected readonly iconeCategoria = iconeCategoria;
  protected readonly horaRegistro = horaRegistro;
  protected readonly resolverFotoUrl = resolverFotoUrl;

  ngOnInit(): void {
    if (!this.sessao.responsavelId()) {
      this.router.navigateByUrl('/portal');
      return;
    }

    this.alunoId = this.route.snapshot.paramMap.get('id') ?? '';
    if (!this.alunoId) {
      this.router.navigateByUrl('/portal/filhos');
      return;
    }

    this.alunoService.obterAluno(this.alunoId).subscribe((aluno) => this.aluno.set(aluno));
    this.rotinaService.listarDoDia(this.alunoId).subscribe({
      next: (registros) => {
        this.registros.set(registros);
        this.carregando.set(false);
      },
      error: () => this.carregando.set(false)
    });
  }

  voltar(): void {
    this.router.navigateByUrl('/portal/filhos');
  }
}
