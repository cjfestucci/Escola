import { Component, computed, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { filter } from 'rxjs';

import { SessaoService } from '../../services/sessao.service';

type Contexto = 'educador' | 'portal' | null;

@Component({
  selector: 'app-header',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss'
})
export class HeaderComponent {
  private readonly router = inject(Router);
  protected readonly sessao = inject(SessaoService);

  private readonly url = signal(this.router.url);

  protected readonly contexto = computed<Contexto>(() => {
    const u = this.url();
    if (u.startsWith('/portal')) return 'portal';
    if (u.startsWith('/alunos') || u.startsWith('/entrar')) return 'educador';
    return null;
  });

  constructor() {
    this.router.events.pipe(filter((evento) => evento instanceof NavigationEnd)).subscribe(() => {
      this.url.set(this.router.url);
    });
  }

  trocar(): void {
    if (this.contexto() === 'portal') {
      this.sessao.sairResponsavel();
      this.router.navigateByUrl('/portal');
    } else {
      this.sessao.sairEducador();
      this.router.navigateByUrl('/entrar');
    }
  }
}
