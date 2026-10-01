import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from './auth.service';
import { SegmentoService } from './segmento.service';

/** Landing page depois do login — no clube o "Turma" (rotina diária de bebês/crianças) não existe,
 * então manda pro Dashboard em vez do /alunos padrão da escola. */
export const redirecionamentoInicialGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const segmentoService = inject(SegmentoService);
  const router = inject(Router);

  if (!auth.estaLogado()) return router.parseUrl('/entrar');
  if (auth.ehResponsavel()) return router.parseUrl('/portal/filhos');
  return router.parseUrl(segmentoService.ehClube() ? '/dashboard' : '/alunos');
};

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
