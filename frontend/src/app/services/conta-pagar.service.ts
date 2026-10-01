import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { ContaPagar, CriarOuEditarContaPagar } from '../models/conta-pagar.model';

@Injectable({ providedIn: 'root' })
export class ContaPagarService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  listar(filtros: { fornecedorId?: string; paga?: boolean } = {}): Observable<ContaPagar[]> {
    const params: Record<string, string> = {};
    if (filtros.fornecedorId) params['fornecedorId'] = filtros.fornecedorId;
    if (filtros.paga !== undefined) params['paga'] = String(filtros.paga);
    return this.http.get<ContaPagar[]>(`${this.baseUrl}/financeiro/contas-pagar`, { params });
  }

  criar(payload: CriarOuEditarContaPagar): Observable<ContaPagar> {
    return this.http.post<ContaPagar>(`${this.baseUrl}/financeiro/contas-pagar`, payload);
  }

  editar(id: string, payload: CriarOuEditarContaPagar): Observable<ContaPagar> {
    return this.http.put<ContaPagar>(`${this.baseUrl}/financeiro/contas-pagar/${id}`, payload);
  }

  marcarPaga(id: string): Observable<ContaPagar> {
    return this.http.post<ContaPagar>(`${this.baseUrl}/financeiro/contas-pagar/${id}/marcar-paga`, {});
  }

  desmarcarPaga(id: string): Observable<ContaPagar> {
    return this.http.post<ContaPagar>(`${this.baseUrl}/financeiro/contas-pagar/${id}/desmarcar-paga`, {});
  }

  cancelar(id: string): Observable<ContaPagar> {
    return this.http.post<ContaPagar>(`${this.baseUrl}/financeiro/contas-pagar/${id}/cancelar`, {});
  }

  reabrir(id: string): Observable<ContaPagar> {
    return this.http.post<ContaPagar>(`${this.baseUrl}/financeiro/contas-pagar/${id}/reabrir`, {});
  }
}
