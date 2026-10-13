import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { environment } from '../../environments/environment';

export interface DoisFatores {
  ativo: boolean;
  ativadoEm: string | null;
  codigosRecuperacaoRestantes: number;
  /** O ambiente tem o cofre de segredos configurado. */
  disponivel: boolean;
  /** Como a pessoa recebe o código: app autenticador ou e-mail (nulo = desativada). */
  metodo: 'App' | 'Email' | null;
  /** O ambiente envia e-mail (SMTP). */
  emailDisponivel: boolean;
  /** Aparelhos em que o código não é pedido ("lembrar este aparelho por 30 dias"). */
  aparelhosLembrados: number;
}

export interface EnvioCodigo {
  emailMascarado: string;
  /** Ex.: o e-mail não pôde ser entregue. */
  aviso: string | null;
}

export interface InicioDoisFatores {
  segredo: string;
  uriOtpAuth: string;
}

/** Verificação em duas etapas da própria conta (app autenticador ou código por e-mail, + códigos de recuperação). */
@Injectable({ providedIn: 'root' })
export class MinhaContaService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/minha-conta/dois-fatores`;

  obter(): Observable<DoisFatores> {
    return this.http.get<DoisFatores>(this.baseUrl);
  }

  iniciar(): Observable<InicioDoisFatores> {
    return this.http.post<InicioDoisFatores>(`${this.baseUrl}/iniciar`, null);
  }

  /** Devolve os códigos de recuperação — só aparecem esta vez. */
  confirmar(codigo: string): Observable<string[]> {
    return this.http.post<{ codigos: string[] }>(`${this.baseUrl}/confirmar`, { codigo }).pipe(map((r) => r.codigos));
  }

  /** Ativação por e-mail: manda um código pro e-mail da conta. */
  iniciarEmail(): Observable<EnvioCodigo> {
    return this.http.post<EnvioCodigo>(`${this.baseUrl}/email/iniciar`, null);
  }

  confirmarEmail(codigo: string): Observable<string[]> {
    return this.http.post<{ codigos: string[] }>(`${this.baseUrl}/email/confirmar`, { codigo }).pipe(map((r) => r.codigos));
  }

  /** Com o código por e-mail ativo: manda um código (pra desativar ou gerar novos códigos de recuperação). */
  enviarCodigo(): Observable<EnvioCodigo> {
    return this.http.post<EnvioCodigo>(`${this.baseUrl}/email/enviar-codigo`, null);
  }

  /** O código volta a ser pedido em todos os aparelhos. */
  esquecerAparelhos(): Observable<DoisFatores> {
    return this.http.post<DoisFatores>(`${this.baseUrl}/esquecer-aparelhos`, null);
  }

  desativar(senha: string, codigo: string): Observable<DoisFatores> {
    return this.http.post<DoisFatores>(`${this.baseUrl}/desativar`, { senha, codigo });
  }

  novosCodigos(codigo: string): Observable<string[]> {
    return this.http.post<{ codigos: string[] }>(`${this.baseUrl}/novos-codigos`, { codigo }).pipe(map((r) => r.codigos));
  }
}
