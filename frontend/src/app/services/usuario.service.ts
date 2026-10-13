import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { Turma } from '../models/aluno.model';
import { CriarOuEditarUsuario, EnvioLinkConta, Usuario, UsuarioConta } from '../models/usuario.model';

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

  criarConta(payload: CriarOuEditarUsuario): Observable<EnvioLinkConta> {
    return this.http.post<EnvioLinkConta>(`${this.baseUrl}/usuarios/contas`, payload);
  }

  editarConta(id: string, payload: CriarOuEditarUsuario): Observable<UsuarioConta> {
    return this.http.put<UsuarioConta>(`${this.baseUrl}/usuarios/contas/${id}`, payload);
  }

  /** Pra quem perdeu o celular e os códigos de recuperação. */
  removerDoisFatores(id: string): Observable<UsuarioConta> {
    return this.http.post<UsuarioConta>(`${this.baseUrl}/usuarios/contas/${id}/remover-dois-fatores`, {});
  }

  /** Convite pendente: reenvia o convite. Conta ativa: link de nova senha (1 hora). */
  enviarLink(id: string): Observable<EnvioLinkConta> {
    return this.http.post<EnvioLinkConta>(`${this.baseUrl}/usuarios/contas/${id}/enviar-link`, {});
  }

  desativarConta(id: string): Observable<UsuarioConta> {
    return this.http.post<UsuarioConta>(`${this.baseUrl}/usuarios/contas/${id}/desativar`, {});
  }

  /** Derruba as sessões abertas da conta (a senha e o cadastro continuam); a pessoa só precisa entrar de novo. */
  encerrarSessoes(id: string): Observable<UsuarioConta> {
    return this.http.post<UsuarioConta>(`${this.baseUrl}/usuarios/contas/${id}/encerrar-sessoes`, {});
  }

  ativarConta(id: string): Observable<UsuarioConta> {
    return this.http.post<UsuarioConta>(`${this.baseUrl}/usuarios/contas/${id}/ativar`, {});
  }
}
