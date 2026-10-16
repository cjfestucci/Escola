import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, tap, throwError } from 'rxjs';

import { environment } from '../../environments/environment';
import { AuthService } from './auth.service';
import { ConexaoService } from './conexao.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const conexao = inject(ConexaoService);

  const token = authService.obterToken();
  const paraApi = request.url.startsWith(environment.apiUrl);
  const requisicao = token && paraApi ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request;

  return next(requisicao).pipe(
    // Qualquer resposta da API prova que o servidor está no ar (some a faixa "sem conexão").
    tap(() => paraApi && conexao.registrarSucesso()),
    catchError((erro) => {
      // Sem resposta (rede/servidor fora) ou o proxy sem o app atrás: faixa "sem conexão". 503 não — a API o usa em respostas normais.
      if (paraApi && [0, 502, 504].includes(erro.status)) conexao.registrarFalha();
      else if (paraApi && erro.status) conexao.registrarSucesso();
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
