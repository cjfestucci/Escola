import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from './auth.service';
import { SegmentoService } from './segmento.service';

/** Pra onde mandar quem tentou entrar numa área que não é dele. O Suporte (equipe do produto) nunca vai pras telas do
 * cliente — sua casa é a Plataforma; o Responsável vai pro portal; o resto da equipe, pra rotina. */
function destinoSemAcesso(auth: AuthService): string {
  if (!auth.estaLogado()) return '/entrar';
  if (auth.ehSuporte()) return '/plataforma';
  if (auth.ehResponsavel()) return '/portal/filhos';
  return '/alunos';
}

/** Landing page depois do login — no clube o "Turma" (rotina diária de bebês/crianças) não existe,
 * então manda pro Dashboard em vez do /alunos padrão da escola. */
export const redirecionamentoInicialGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const segmentoService = inject(SegmentoService);
  const router = inject(Router);

  if (!auth.estaLogado()) return router.parseUrl('/entrar');
  if (auth.ehSuporte()) return router.parseUrl('/plataforma');
  if (auth.ehResponsavel()) return router.parseUrl('/portal/filhos');
  return router.parseUrl(segmentoService.ehClube() ? '/dashboard' : '/alunos');
};

export const equipeGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.ehEquipe()) return true;
  return router.parseUrl(destinoSemAcesso(auth));
};

export const gestaoGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.ehGestao()) return true;
  return router.parseUrl(auth.ehEquipe() ? '/alunos' : destinoSemAcesso(auth));
};

/** Telas de Configurações: Gestão do cliente e Suporte (a única área de cliente que o Suporte enxerga). */
export const configuracaoGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.podeConfigurar()) return true;
  return router.parseUrl(auth.ehEquipe() ? '/alunos' : destinoSemAcesso(auth));
};

export const suporteGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.ehSuporte()) return true;
  return router.parseUrl(auth.ehEquipe() ? '/alunos' : destinoSemAcesso(auth));
};

export const financeiroGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.ehFinanceiro()) return true;
  return router.parseUrl(auth.ehEquipe() ? '/alunos' : destinoSemAcesso(auth));
};

export const portalGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.ehResponsavel()) return true;
  return router.parseUrl(auth.estaLogado() ? destinoSemAcesso(auth) : '/entrar');
};
