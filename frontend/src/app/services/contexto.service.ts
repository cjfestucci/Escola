import { Injectable, computed, inject } from '@angular/core';

import { AuthService } from './auth.service';

export type Contexto = 'educador' | 'portal' | null;

/** Deriva se a identidade logada é da equipe ou do Portal dos Pais, pro shell (sidebar/topbar) se adaptar. */
@Injectable({ providedIn: 'root' })
export class ContextoService {
  private readonly auth = inject(AuthService);

  readonly contexto = computed<Contexto>(() => {
    if (this.auth.ehResponsavel()) return 'portal';
    if (this.auth.ehEquipe()) return 'educador';
    return null;
  });
}
