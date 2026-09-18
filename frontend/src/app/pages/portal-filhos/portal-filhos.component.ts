import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';

import { Aluno } from '../../models/aluno.model';
import { ResponsavelService } from '../../services/responsavel.service';
import { SessaoService } from '../../services/sessao.service';

@Component({
  selector: 'app-portal-filhos',
  imports: [],
  templateUrl: './portal-filhos.component.html',
  styleUrl: './portal-filhos.component.scss'
})
export class PortalFilhosComponent implements OnInit {
  private readonly responsavelService = inject(ResponsavelService);
  private readonly sessao = inject(SessaoService);
  private readonly router = inject(Router);

  readonly filhos = signal<Aluno[]>([]);
  readonly carregando = signal(true);

  ngOnInit(): void {
    const responsavelId = this.sessao.responsavelId();
    if (!responsavelId) {
      this.router.navigateByUrl('/portal');
      return;
    }

    this.responsavelService.listarFilhos(responsavelId).subscribe({
      next: (filhos) => {
        this.filhos.set(filhos);
        this.carregando.set(false);

        // só um filho: pula direto pra linha do tempo dele
        if (filhos.length === 1) {
          this.router.navigate(['/portal/alunos', filhos[0].id]);
        }
      },
      error: () => this.carregando.set(false)
    });
  }

  abrir(aluno: Aluno): void {
    this.router.navigate(['/portal/alunos', aluno.id]);
  }
}
