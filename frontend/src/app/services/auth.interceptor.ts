import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

import { environment } from '../../environments/environment';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const token = authService.obterToken();
  const paraApi = request.url.startsWith(environment.apiUrl);
  const requisicao = token && paraApi ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request;

  return next(requisicao).pipe(
    catchError((erro) => {
      if (erro.status === 401 && paraApi) {
        authService.sair();
        router.navigateByUrl('/entrar');
      }
      // 402: a assinatura do clube está pendente — o Admin só alcança a tela da assinatura (a API barra o resto).
      if (erro.status === 402 && paraApi && !router.url.startsWith('/configuracoes/assinatura')) {
        router.navigateByUrl('/configuracoes/assinatura');
      }
      return throwError(() => erro);
    })
  );
};
