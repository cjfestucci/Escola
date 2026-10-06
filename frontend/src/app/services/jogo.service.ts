import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { CampeonatoFamilia, CriarOuEditarJogo, Jogo, JogoAtleta, JogosCampeonatoDoAluno, JogosDoAluno } from '../models/competicao.model';

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

  /** Visão da família: jogos da turma do atleta e as convocações dele (Equipe e Responsável do atleta). */
  listarDoAluno(alunoId: string): Observable<JogosDoAluno> {
    return this.http.get<JogosDoAluno>(`${environment.apiUrl}/alunos/${alunoId}/jogos`);
  }

  /** Campeonatos em que o time do atleta joga (visão da família). */
  listarCampeonatosDoAluno(alunoId: string): Observable<CampeonatoFamilia[]> {
    return this.http.get<CampeonatoFamilia[]>(`${environment.apiUrl}/alunos/${alunoId}/campeonatos`);
  }

  /** Calendário completo do time do atleta num campeonato, com a campanha do time. */
  jogosDoCampeonatoDoAluno(alunoId: string, campeonatoId: string): Observable<JogosCampeonatoDoAluno> {
    return this.http.get<JogosCampeonatoDoAluno>(`${environment.apiUrl}/alunos/${alunoId}/campeonatos/${campeonatoId}/jogos`);
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
