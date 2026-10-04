import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { CriarOuEditarJogo, Jogo, JogoAtleta } from '../models/competicao.model';

@Injectable({ providedIn: 'root' })
export class JogoService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/jogos`;

  listar(filtros: { campeonatoId?: string; turmaId?: string } = {}): Observable<Jogo[]> {
    const params: Record<string, string> = {};
    if (filtros.campeonatoId) params['campeonatoId'] = filtros.campeonatoId;
    if (filtros.turmaId) params['turmaId'] = filtros.turmaId;
    return this.http.get<Jogo[]>(this.baseUrl, { params });
  }

  obterPorId(id: string): Observable<Jogo> {
    return this.http.get<Jogo>(`${this.baseUrl}/${id}`);
  }

  criar(payload: CriarOuEditarJogo): Observable<Jogo> {
    return this.http.post<Jogo>(this.baseUrl, payload);
  }

  editar(id: string, payload: CriarOuEditarJogo): Observable<Jogo> {
    return this.http.put<Jogo>(`${this.baseUrl}/${id}`, payload);
  }

  cancelar(id: string): Observable<Jogo> {
    return this.http.post<Jogo>(`${this.baseUrl}/${id}/cancelar`, {});
  }

  reabrir(id: string): Observable<Jogo> {
    return this.http.post<Jogo>(`${this.baseUrl}/${id}/reabrir`, {});
  }

  salvarConvocacao(id: string, convocados: Omit<JogoAtleta, 'alunoNome'>[]): Observable<Jogo> {
    return this.http.put<Jogo>(`${this.baseUrl}/${id}/convocacao`, { convocados });
  }
}
