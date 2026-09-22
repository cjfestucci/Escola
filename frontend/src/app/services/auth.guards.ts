import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from './auth.service';

export const equipeGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.ehEquipe()) return true;
  return router.parseUrl(auth.estaLogado() ? '/portal/filhos' : '/entrar');
};

export const gestaoGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.ehGestao()) return true;
  return router.parseUrl(auth.ehEquipe() ? '/alunos' : '/entrar');
};

export const financeiroGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.ehFinanceiro()) return true;
  return router.parseUrl(auth.ehEquipe() ? '/alunos' : '/entrar');
};

export const portalGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.ehResponsavel()) return true;
  return router.parseUrl(auth.estaLogado() ? '/alunos' : '/entrar');
};
