import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { Campeonato, CriarOuEditarCampeonato } from '../models/competicao.model';

@Injectable({ providedIn: 'root' })
export class CampeonatoService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/campeonatos`;

  listar(): Observable<Campeonato[]> {
    return this.http.get<Campeonato[]>(this.baseUrl);
  }

  obterPorId(id: string): Observable<Campeonato> {
    return this.http.get<Campeonato>(`${this.baseUrl}/${id}`);
  }

  criar(payload: CriarOuEditarCampeonato): Observable<Campeonato> {
    return this.http.post<Campeonato>(this.baseUrl, payload);
  }

  editar(id: string, payload: CriarOuEditarCampeonato): Observable<Campeonato> {
    return this.http.put<Campeonato>(`${this.baseUrl}/${id}`, payload);
  }

  desativar(id: string): Observable<Campeonato> {
    return this.http.post<Campeonato>(`${this.baseUrl}/${id}/desativar`, {});
  }

  ativar(id: string): Observable<Campeonato> {
    return this.http.post<Campeonato>(`${this.baseUrl}/${id}/ativar`, {});
  }
}
