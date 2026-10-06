import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { Aluno, AlunoDetalhe, ConviteMatricula, CriarOuEditarAluno, Turma } from '../models/aluno.model';

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

  /** Manda de novo o e-mail da matrícula a um responsável (convite, se a conta não foi ativada; senão, aviso pra entrar no portal). */
  reenviarConvite(alunoId: string, responsavelId: string): Observable<ConviteMatricula> {
    return this.http.post<ConviteMatricula>(`${this.baseUrl}/alunos/${alunoId}/responsaveis/${responsavelId}/reenviar-convite`, {});
  }

  /** LGPD: ZIP com todos os dados pessoais do aluno (Gestão). Fica no histórico do aluno. */
  exportarDados(alunoId: string): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/alunos/${alunoId}/dados-pessoais/exportar`, { responseType: 'blob' });
  }

  desativar(id: string): Observable<AlunoDetalhe> {
    return this.http.post<AlunoDetalhe>(`${this.baseUrl}/alunos/${id}/desativar`, {});
  }

  ativar(id: string): Observable<AlunoDetalhe> {
    return this.http.post<AlunoDetalhe>(`${this.baseUrl}/alunos/${id}/ativar`, {});
  }
}
