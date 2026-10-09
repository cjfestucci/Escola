import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';

import { AssinaturaService } from './assinatura.service';
import { AuthService } from './auth.service';
import { SegmentoService } from './segmento.service';
import { TermoService } from './termo.service';

/** Pra onde mandar quem tentou entrar numa área que não é dele. O Suporte (equipe do produto) nunca vai pras telas do
 * cliente — sua casa é a Plataforma; o Responsável vai pro portal; o resto da equipe, pra rotina. */
/** Assinatura do clube suspensa/pendente: o Admin só usa a tela da assinatura (a API responderia 402 em tudo). */
function bloqueioDaAssinatura(): string | null {
  return inject(AssinaturaService).bloqueada() ? '/configuracoes/assinatura' : null;
}

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
  const bloqueio = bloqueioDaAssinatura();
  if (bloqueio) return router.parseUrl(bloqueio);

  if (auth.ehEquipe()) return true;
  return router.parseUrl(destinoSemAcesso(auth));
};

export const gestaoGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const bloqueio = bloqueioDaAssinatura();
  if (bloqueio) return router.parseUrl(bloqueio);

  if (auth.ehGestao()) return true;
  return router.parseUrl(auth.ehEquipe() ? '/alunos' : destinoSemAcesso(auth));
};

/** Telas de Configurações: Gestão do cliente e Suporte (a única área de cliente que o Suporte enxerga). */
export const configuracaoGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const bloqueio = bloqueioDaAssinatura();
  if (bloqueio) return router.parseUrl(bloqueio);

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
  const bloqueio = bloqueioDaAssinatura();
  if (bloqueio) return router.parseUrl(bloqueio);

  if (auth.ehFinanceiro()) return true;
  return router.parseUrl(auth.ehEquipe() ? '/alunos' : destinoSemAcesso(auth));
};

export const portalGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.ehResponsavel()) return true;
  return router.parseUrl(auth.estaLogado() ? destinoSemAcesso(auth) : '/entrar');
};

/** No portal, antes de qualquer tela: se algum filho ainda não tem o aceite do termo de matrícula (versão atual), vai pro termo.
 * Falha da API não tranca o portal (o termo volta a ser pedido na próxima navegação). */
export const termoPortalGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const termo = inject(TermoService);
  const router = inject(Router);

  const usuarioId = auth.identidade()?.usuarioId;
  if (!auth.ehResponsavel() || !usuarioId || termo.jaSemPendencias(usuarioId)) return true;

  return termo.pendente(usuarioId).pipe(
    map((t) => (t.alunos.length > 0 ? router.parseUrl('/portal/termo') : true)),
    catchError(() => of(true))
  );
};

/** Assinatura do clube: o dono da conta (Admin) e o Suporte. */
export const adminOuSuporteGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.podeEditarIdentidade()) return true;
  return router.parseUrl(auth.ehEquipe() ? '/alunos' : destinoSemAcesso(auth));
};
