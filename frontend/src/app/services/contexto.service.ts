import { Injectable, inject, signal } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';

export type Contexto = 'educador' | 'portal' | null;

/** Deriva se a rota atual é da área do educador ou do Portal dos Pais, pro shell (sidebar/topbar) se adaptar. */
@Injectable({ providedIn: 'root' })
export class ContextoService {
  private readonly router = inject(Router);

  private readonly url = signal(this.router.url);

  readonly contexto = signal<Contexto>(this.calcular(this.router.url));

  constructor() {
    this.router.events.pipe(filter((evento) => evento instanceof NavigationEnd)).subscribe(() => {
      this.url.set(this.router.url);
      this.contexto.set(this.calcular(this.router.url));
    });
  }

  private calcular(url: string): Contexto {
    if (url.startsWith('/portal')) return 'portal';
    if (url.startsWith('/alunos') || url.startsWith('/entrar') || url.startsWith('/turmas')) return 'educador';
    return null;
  }
}
