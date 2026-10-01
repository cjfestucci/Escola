import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { Aluno, AlunoDetalhe, CriarOuEditarAluno, Turma } from '../models/aluno.model';

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

  obterAluno(id: string): Observable<AlunoDetalhe> {
    return this.http.get<AlunoDetalhe>(`${this.baseUrl}/alunos/${id}`);
  }

  criar(payload: CriarOuEditarAluno): Observable<AlunoDetalhe> {
    return this.http.post<AlunoDetalhe>(`${this.baseUrl}/alunos`, payload);
  }

  editar(id: string, payload: CriarOuEditarAluno): Observable<AlunoDetalhe> {
    return this.http.put<AlunoDetalhe>(`${this.baseUrl}/alunos/${id}`, payload);
  }

  desativar(id: string): Observable<AlunoDetalhe> {
    return this.http.post<AlunoDetalhe>(`${this.baseUrl}/alunos/${id}/desativar`, {});
  }

  ativar(id: string): Observable<AlunoDetalhe> {
    return this.http.post<AlunoDetalhe>(`${this.baseUrl}/alunos/${id}/ativar`, {});
  }
}
