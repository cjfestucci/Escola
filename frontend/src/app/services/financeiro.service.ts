import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import {
  Cobranca,
  ConfiguracaoFinanceira,
  CriarCobranca,
  EditarCobranca,
  EditarConfiguracaoFinanceira,
  PixCobranca,
} from '../models/cobranca.model';

@Injectable({ providedIn: 'root' })
export class FinanceiroService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  listar(filtros: { turmaId?: string; paga?: boolean } = {}): Observable<Cobranca[]> {
    const params: Record<string, string> = {};
    if (filtros.turmaId) params['turmaId'] = filtros.turmaId;
    if (filtros.paga !== undefined) params['paga'] = String(filtros.paga);
    return this.http.get<Cobranca[]>(`${this.baseUrl}/financeiro/cobrancas`, { params });
  }

  listarDoAluno(alunoId: string): Observable<Cobranca[]> {
    return this.http.get<Cobranca[]>(`${this.baseUrl}/alunos/${alunoId}/cobrancas`);
  }

  criar(payload: CriarCobranca): Observable<Cobranca> {
    return this.http.post<Cobranca>(`${this.baseUrl}/financeiro/cobrancas`, payload);
  }

  editar(id: string, payload: EditarCobranca): Observable<Cobranca> {
    return this.http.put<Cobranca>(`${this.baseUrl}/financeiro/cobrancas/${id}`, payload);
  }

  marcarPaga(id: string): Observable<Cobranca> {
    return this.http.post<Cobranca>(`${this.baseUrl}/financeiro/cobrancas/${id}/marcar-paga`, {});
  }

  desmarcarPaga(id: string): Observable<Cobranca> {
    return this.http.post<Cobranca>(`${this.baseUrl}/financeiro/cobrancas/${id}/desmarcar-paga`, {});
  }

  excluir(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/financeiro/cobrancas/${id}`);
  }

  obterPix(id: string): Observable<PixCobranca> {
    return this.http.get<PixCobranca>(`${this.baseUrl}/financeiro/cobrancas/${id}/pix`);
  }

  enviarEmail(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/financeiro/cobrancas/${id}/enviar-email`, {});
  }

  obterConfiguracao(): Observable<ConfiguracaoFinanceira> {
    return this.http.get<ConfiguracaoFinanceira>(`${this.baseUrl}/financeiro/configuracao`);
  }

  editarConfiguracao(payload: EditarConfiguracaoFinanceira): Observable<ConfiguracaoFinanceira> {
    return this.http.put<ConfiguracaoFinanceira>(`${this.baseUrl}/financeiro/configuracao`, payload);
  }
}
