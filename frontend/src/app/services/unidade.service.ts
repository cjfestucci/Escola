import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { CriarOuEditarUnidade, Unidade } from '../models/unidade.model';

@Injectable({ providedIn: 'root' })
export class UnidadeService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  listar(): Observable<Unidade[]> {
    return this.http.get<Unidade[]>(`${this.baseUrl}/unidades`);
  }

  obterPorId(id: string): Observable<Unidade> {
    return this.http.get<Unidade>(`${this.baseUrl}/unidades/${id}`);
  }

  criar(payload: CriarOuEditarUnidade): Observable<Unidade> {
    return this.http.post<Unidade>(`${this.baseUrl}/unidades`, payload);
  }

  editar(id: string, payload: CriarOuEditarUnidade): Observable<Unidade> {
    return this.http.put<Unidade>(`${this.baseUrl}/unidades/${id}`, payload);
  }

  desativar(id: string): Observable<Unidade> {
    return this.http.post<Unidade>(`${this.baseUrl}/unidades/${id}/desativar`, {});
  }

  ativar(id: string): Observable<Unidade> {
    return this.http.post<Unidade>(`${this.baseUrl}/unidades/${id}/ativar`, {});
  }
}
