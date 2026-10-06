import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { Chamada, Faltoso, FrequenciaAluno, FrequenciaDoAluno, SalvarChamada } from '../models/presenca.model';

@Injectable({ providedIn: 'root' })
export class PresencaService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  obterChamada(turmaId: string, data: string): Observable<Chamada> {
    return this.http.get<Chamada>(`${this.baseUrl}/turmas/${turmaId}/presenca`, { params: { data } });
  }

  salvarChamada(turmaId: string, payload: SalvarChamada): Observable<Chamada> {
    return this.http.put<Chamada>(`${this.baseUrl}/turmas/${turmaId}/presenca`, payload);
  }

  /** Sem período, o backend usa os últimos 30 dias. */
  frequenciaDaTurma(turmaId: string, periodo?: { de: string; ate: string }): Observable<FrequenciaAluno[]> {
    return this.http.get<FrequenciaAluno[]>(`${this.baseUrl}/turmas/${turmaId}/frequencia`, { params: { ...periodo } });
  }

  frequenciaDoAluno(alunoId: string, periodo?: { de: string; ate: string }): Observable<FrequenciaDoAluno> {
    return this.http.get<FrequenciaDoAluno>(`${this.baseUrl}/alunos/${alunoId}/frequencia`, { params: { ...periodo } });
  }

  faltosos(minimo?: number): Observable<Faltoso[]> {
    const params: Record<string, number> = {};
    if (minimo) params['minimo'] = minimo;
    return this.http.get<Faltoso[]>(`${this.baseUrl}/presenca/faltosos`, { params });
  }
}
