import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { AdminEscola, ClientePlataforma, DiagnosticoPlataforma, EditarClientePlataforma, ResultadoEnvioAdmin } from '../models/plataforma.model';

/** Área da equipe do produto (papel Suporte) sobre o cliente deste ambiente. */
@Injectable({ providedIn: 'root' })
export class PlataformaService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/plataforma`;

  obterCliente(): Observable<ClientePlataforma> {
    return this.http.get<ClientePlataforma>(`${this.baseUrl}/cliente`);
  }

  editarCliente(payload: EditarClientePlataforma): Observable<ClientePlataforma> {
    return this.http.put<ClientePlataforma>(`${this.baseUrl}/cliente`, payload);
  }

  /** Envia a logo (multipart). O backend valida o conteúdo real: PNG, JPEG ou WEBP, até 2MB. */
  enviarLogo(arquivo: File): Observable<{ logoUrl: string | null }> {
    const formulario = new FormData();
    formulario.append('arquivo', arquivo);
    return this.http.put<{ logoUrl: string | null }>(`${this.baseUrl}/logo`, formulario);
  }

  removerLogo(): Observable<{ logoUrl: string | null }> {
    return this.http.delete<{ logoUrl: string | null }>(`${this.baseUrl}/logo`);
  }

  listarAdmins(): Observable<AdminEscola[]> {
    return this.http.get<AdminEscola[]>(`${this.baseUrl}/admins`);
  }

  /** Cria o Admin da escola sem senha e manda o convite por e-mail (a pessoa confirma o e-mail e cadastra a própria senha). */
  criarAdmin(nome: string, email: string): Observable<ResultadoEnvioAdmin> {
    return this.http.post<ResultadoEnvioAdmin>(`${this.baseUrl}/admins`, { nome, email });
  }

  /** Conta pendente: reenvia o convite. Conta ativa: manda um link de nova senha. */
  reenviarAdmin(id: string): Observable<ResultadoEnvioAdmin> {
    return this.http.post<ResultadoEnvioAdmin>(`${this.baseUrl}/admins/${id}/reenviar`, {});
  }

  cancelarConvite(id: string): Observable<AdminEscola> {
    return this.http.post<AdminEscola>(`${this.baseUrl}/admins/${id}/cancelar-convite`, {});
  }

  diagnostico(): Observable<DiagnosticoPlataforma> {
    return this.http.get<DiagnosticoPlataforma>(`${this.baseUrl}/diagnostico`);
  }
}
