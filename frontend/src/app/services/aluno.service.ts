import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { Aluno, Turma } from '../models/aluno.model';

@Injectable({ providedIn: 'root' })
export class AlunoService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  listarTurmas(): Observable<Turma[]> {
    return this.http.get<Turma[]>(`${this.baseUrl}/turmas`);
  }

  listarAlunos(turmaId?: string): Observable<Aluno[]> {
    const params: Record<string, string> = {};
    if (turmaId) params['turmaId'] = turmaId;
    return this.http.get<Aluno[]>(`${this.baseUrl}/alunos`, { params });
  }

  obterAluno(id: string): Observable<Aluno> {
    return this.http.get<Aluno>(`${this.baseUrl}/alunos/${id}`);
  }
}
