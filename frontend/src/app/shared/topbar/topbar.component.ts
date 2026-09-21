import { Component, inject, output, signal } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';

import { AuthService } from '../../services/auth.service';
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
  protected readonly auth = inject(AuthService);
  protected readonly contextoService = inject(ContextoService);
  protected readonly sessao = inject(SessaoService);

  readonly abrirMenu = output<void>();

  protected readonly titulo = signal(this.tituloAtual());

  constructor() {
    this.router.events.pipe(filter((evento) => evento instanceof NavigationEnd)).subscribe(() => {
      this.titulo.set(this.tituloAtual());
    });
  }

  sair(): void {
    this.auth.sair();
    this.sessao.limpar();
    this.router.navigateByUrl('/entrar');
  }

  private tituloAtual(): string {
    let rota = this.router.routerState.snapshot.root;
    while (rota.firstChild) rota = rota.firstChild;
    return (rota.data['titulo'] as string) ?? 'Rotina Escola';
  }
}
