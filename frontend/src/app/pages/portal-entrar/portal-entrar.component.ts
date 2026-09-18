import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';

import { Responsavel } from '../../models/responsavel.model';
import { ResponsavelService } from '../../services/responsavel.service';
import { SessaoService } from '../../services/sessao.service';

@Component({
  selector: 'app-portal-entrar',
  imports: [],
  templateUrl: './portal-entrar.component.html',
  styleUrl: './portal-entrar.component.scss'
})
export class PortalEntrarComponent implements OnInit {
  private readonly responsavelService = inject(ResponsavelService);
  private readonly sessao = inject(SessaoService);
  private readonly router = inject(Router);

  readonly responsaveis = signal<Responsavel[]>([]);
  readonly carregando = signal(true);
  readonly erro = signal(false);

  ngOnInit(): void {
    this.responsavelService.listar().subscribe({
      next: (responsaveis) => {
        this.responsaveis.set(responsaveis);
        this.carregando.set(false);
      },
      error: () => {
        this.erro.set(true);
        this.carregando.set(false);
      }
    });
  }

  selecionar(responsavel: Responsavel): void {
    this.sessao.definirResponsavel(responsavel.id);
    this.router.navigateByUrl('/portal/filhos');
  }
}
