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
}

export interface InicioDoisFatores {
  segredo: string;
  uriOtpAuth: string;
}

/** Verificação em duas etapas da própria conta (app autenticador + códigos de recuperação). */
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

  desativar(senha: string, codigo: string): Observable<DoisFatores> {
    return this.http.post<DoisFatores>(`${this.baseUrl}/desativar`, { senha, codigo });
  }

  novosCodigos(codigo: string): Observable<string[]> {
    return this.http.post<{ codigos: string[] }>(`${this.baseUrl}/novos-codigos`, { codigo }).pipe(map((r) => r.codigos));
  }
}
