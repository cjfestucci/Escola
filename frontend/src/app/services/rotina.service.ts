import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import {
  CriarRegistroAlimentacao,
  CriarRegistroHigiene,
  CriarRegistroHumor,
  CriarRegistroMomento,
  CriarRegistroSono,
  RegistroRotina
} from '../models/registro-rotina.model';

@Injectable({ providedIn: 'root' })
export class RotinaService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  listarDoDia(alunoId: string, data?: string): Observable<RegistroRotina[]> {
    const params: Record<string, string> = {};
    if (data) params['data'] = data;
    return this.http.get<RegistroRotina[]>(`${this.baseUrl}/alunos/${alunoId}/rotina`, { params });
  }

  registrarAlimentacao(alunoId: string, payload: CriarRegistroAlimentacao): Observable<RegistroRotina> {
    return this.http.post<RegistroRotina>(`${this.baseUrl}/alunos/${alunoId}/rotina/alimentacao`, payload);
  }

  registrarSono(alunoId: string, payload: CriarRegistroSono): Observable<RegistroRotina> {
    return this.http.post<RegistroRotina>(`${this.baseUrl}/alunos/${alunoId}/rotina/sono`, payload);
  }

  registrarHigiene(alunoId: string, payload: CriarRegistroHigiene): Observable<RegistroRotina> {
    return this.http.post<RegistroRotina>(`${this.baseUrl}/alunos/${alunoId}/rotina/higiene`, payload);
  }

  registrarHumor(alunoId: string, payload: CriarRegistroHumor): Observable<RegistroRotina> {
    return this.http.post<RegistroRotina>(`${this.baseUrl}/alunos/${alunoId}/rotina/humor`, payload);
  }

  registrarMomento(alunoId: string, payload: CriarRegistroMomento): Observable<RegistroRotina> {
    return this.http.post<RegistroRotina>(`${this.baseUrl}/alunos/${alunoId}/rotina/momento`, payload);
  }
}
