import { Component, inject, output, signal } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';

import { ContextoService } from '../../services/contexto.service';
import { SessaoService } from '../../services/sessao.service';

@Component({
  selector: 'app-topbar',
  imports: [],
  templateUrl: './topbar.component.html',
  styleUrl: './topbar.component.scss'
})
export class TopbarComponent {
  private readonly router = inject(Router);
  protected readonly contextoService = inject(ContextoService);
  protected readonly sessao = inject(SessaoService);

  readonly abrirMenu = output<void>();

  protected readonly titulo = signal(this.tituloAtual());

  constructor() {
    this.router.events.pipe(filter((evento) => evento instanceof NavigationEnd)).subscribe(() => {
      this.titulo.set(this.tituloAtual());
    });
  }

  trocar(): void {
    if (this.contextoService.contexto() === 'portal') {
      this.sessao.sairResponsavel();
      this.router.navigateByUrl('/portal');
    } else {
      this.sessao.sairEducador();
      this.router.navigateByUrl('/entrar');
    }
  }

  private tituloAtual(): string {
    let rota = this.router.routerState.snapshot.root;
    while (rota.firstChild) rota = rota.firstChild;
    return (rota.data['titulo'] as string) ?? 'Rotina Escola';
  }
}
