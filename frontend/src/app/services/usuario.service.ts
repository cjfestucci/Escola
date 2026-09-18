import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { Turma } from '../models/aluno.model';
import { CriarOuEditarUsuario, SenhaGerada, Usuario, UsuarioConta } from '../models/usuario.model';

@Injectable({ providedIn: 'root' })
export class UsuarioService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  listarEducadores(): Observable<Usuario[]> {
    return this.http.get<Usuario[]>(`${this.baseUrl}/usuarios`, { params: { papel: 'Educador' } });
  }

  listarTurmas(usuarioId: string): Observable<Turma[]> {
    return this.http.get<Turma[]>(`${this.baseUrl}/usuarios/${usuarioId}/turmas`);
  }

  listarContas(): Observable<UsuarioConta[]> {
    return this.http.get<UsuarioConta[]>(`${this.baseUrl}/usuarios/contas`);
  }

  criarConta(payload: CriarOuEditarUsuario): Observable<SenhaGerada> {
    return this.http.post<SenhaGerada>(`${this.baseUrl}/usuarios/contas`, payload);
  }

  editarConta(id: string, payload: CriarOuEditarUsuario): Observable<UsuarioConta> {
    return this.http.put<UsuarioConta>(`${this.baseUrl}/usuarios/contas/${id}`, payload);
  }

  redefinirSenha(id: string): Observable<SenhaGerada> {
    return this.http.post<SenhaGerada>(`${this.baseUrl}/usuarios/contas/${id}/redefinir-senha`, {});
  }

  excluirConta(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/usuarios/contas/${id}`);
  }
}
