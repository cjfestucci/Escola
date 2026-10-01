import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { ContaReceber, CriarOuEditarContaReceber } from '../models/conta-receber.model';

@Injectable({ providedIn: 'root' })
export class ContaReceberService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  listar(filtros: { recebida?: boolean } = {}): Observable<ContaReceber[]> {
    const params: Record<string, string> = {};
    if (filtros.recebida !== undefined) params['recebida'] = String(filtros.recebida);
    return this.http.get<ContaReceber[]>(`${this.baseUrl}/financeiro/contas-receber`, { params });
  }

  criar(payload: CriarOuEditarContaReceber): Observable<ContaReceber> {
    return this.http.post<ContaReceber>(`${this.baseUrl}/financeiro/contas-receber`, payload);
  }

  editar(id: string, payload: CriarOuEditarContaReceber): Observable<ContaReceber> {
    return this.http.put<ContaReceber>(`${this.baseUrl}/financeiro/contas-receber/${id}`, payload);
  }

  marcarRecebida(id: string): Observable<ContaReceber> {
    return this.http.post<ContaReceber>(`${this.baseUrl}/financeiro/contas-receber/${id}/marcar-recebida`, {});
  }

  desmarcarRecebida(id: string): Observable<ContaReceber> {
    return this.http.post<ContaReceber>(`${this.baseUrl}/financeiro/contas-receber/${id}/desmarcar-recebida`, {});
  }

  cancelar(id: string): Observable<ContaReceber> {
    return this.http.post<ContaReceber>(`${this.baseUrl}/financeiro/contas-receber/${id}/cancelar`, {});
  }

  reabrir(id: string): Observable<ContaReceber> {
    return this.http.post<ContaReceber>(`${this.baseUrl}/financeiro/contas-receber/${id}/reabrir`, {});
  }
}
