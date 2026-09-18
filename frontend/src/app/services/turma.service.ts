import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { CriarOuEditarTurma, Turma } from '../models/aluno.model';

@Injectable({ providedIn: 'root' })
export class TurmaService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  listar(): Observable<Turma[]> {
    return this.http.get<Turma[]>(`${this.baseUrl}/turmas`);
  }

  obterPorId(id: string): Observable<Turma> {
    return this.http.get<Turma>(`${this.baseUrl}/turmas/${id}`);
  }

  criar(payload: CriarOuEditarTurma): Observable<Turma> {
    return this.http.post<Turma>(`${this.baseUrl}/turmas`, payload);
  }

  editar(id: string, payload: CriarOuEditarTurma): Observable<Turma> {
    return this.http.put<Turma>(`${this.baseUrl}/turmas/${id}`, payload);
  }

  excluir(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/turmas/${id}`);
  }
}
