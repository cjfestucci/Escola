import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, tap } from 'rxjs';

import { environment } from '../../environments/environment';
import { TermoAceite, TermoPendente } from '../models/termo.model';

@Injectable({ providedIn: 'root' })
export class TermoService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  /** Conta (usuarioId) para a qual já se sabe, nesta sessão, que não há termo a aceitar — o guard do portal não precisa ir ao
   * servidor a cada navegação. Guardado por conta porque outra pessoa pode entrar no mesmo navegador. */
  private semPendenciasPara: string | null = null;

  pendente(usuarioId: string): Observable<TermoPendente> {
    return this.http
      .get<TermoPendente>(`${this.baseUrl}/portal/termo`)
      .pipe(tap((t) => (this.semPendenciasPara = t.alunos.length === 0 ? usuarioId : null)));
  }

  jaSemPendencias(usuarioId: string): boolean {
    return this.semPendenciasPara === usuarioId;
  }

  aceitar(usuarioId: string, versao: string, alunoIds: string[]): Observable<void> {
    return this.http
      .post<void>(`${this.baseUrl}/portal/termo/aceitar`, { versao, alunoIds })
      .pipe(tap(() => (this.semPendenciasPara = usuarioId)));
  }

  aceitesDoAluno(alunoId: string): Observable<TermoAceite[]> {
    return this.http.get<TermoAceite[]>(`${this.baseUrl}/alunos/${alunoId}/termos`);
  }
}
