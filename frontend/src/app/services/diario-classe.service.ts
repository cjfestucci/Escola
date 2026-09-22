import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { CriarOuEditarRegistroDiarioClasse, RegistroDiarioClasse } from '../models/diario-classe.model';

@Injectable({ providedIn: 'root' })
export class DiarioClasseService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  listarDoDia(turmaId: string, data?: string): Observable<RegistroDiarioClasse[]> {
    const params: Record<string, string> = {};
    if (data) params['data'] = data;
    return this.http.get<RegistroDiarioClasse[]>(`${this.baseUrl}/turmas/${turmaId}/diario`, { params });
  }

  criar(turmaId: string, payload: CriarOuEditarRegistroDiarioClasse): Observable<RegistroDiarioClasse> {
    return this.http.post<RegistroDiarioClasse>(`${this.baseUrl}/turmas/${turmaId}/diario`, payload);
  }

  editar(turmaId: string, registroId: string, payload: CriarOuEditarRegistroDiarioClasse): Observable<RegistroDiarioClasse> {
    return this.http.put<RegistroDiarioClasse>(`${this.baseUrl}/turmas/${turmaId}/diario/${registroId}`, payload);
  }

  excluir(turmaId: string, registroId: string, usuarioId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/turmas/${turmaId}/diario/${registroId}`, { params: { usuarioId } });
  }
}
