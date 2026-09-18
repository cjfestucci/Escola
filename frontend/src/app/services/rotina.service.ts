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

  editarAlimentacao(alunoId: string, registroId: string, payload: CriarRegistroAlimentacao): Observable<RegistroRotina> {
    return this.http.put<RegistroRotina>(`${this.baseUrl}/alunos/${alunoId}/rotina/alimentacao/${registroId}`, payload);
  }

  editarSono(alunoId: string, registroId: string, payload: CriarRegistroSono): Observable<RegistroRotina> {
    return this.http.put<RegistroRotina>(`${this.baseUrl}/alunos/${alunoId}/rotina/sono/${registroId}`, payload);
  }

  editarHigiene(alunoId: string, registroId: string, payload: CriarRegistroHigiene): Observable<RegistroRotina> {
    return this.http.put<RegistroRotina>(`${this.baseUrl}/alunos/${alunoId}/rotina/higiene/${registroId}`, payload);
  }

  editarHumor(alunoId: string, registroId: string, payload: CriarRegistroHumor): Observable<RegistroRotina> {
    return this.http.put<RegistroRotina>(`${this.baseUrl}/alunos/${alunoId}/rotina/humor/${registroId}`, payload);
  }

  editarMomento(alunoId: string, registroId: string, payload: CriarRegistroMomento): Observable<RegistroRotina> {
    return this.http.put<RegistroRotina>(`${this.baseUrl}/alunos/${alunoId}/rotina/momento/${registroId}`, payload);
  }

  excluir(alunoId: string, registroId: string, usuarioId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/alunos/${alunoId}/rotina/${registroId}`, { params: { usuarioId } });
  }
}
