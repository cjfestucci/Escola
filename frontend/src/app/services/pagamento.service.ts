import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { ConectarContaPagamento, ConectarContaPropria, ContaPagamento } from '../models/pagamento.model';

@Injectable({ providedIn: 'root' })
export class PagamentoService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/pagamentos`;

  obterConta(): Observable<ContaPagamento> {
    return this.http.get<ContaPagamento>(`${this.baseUrl}/conta`);
  }

  conectarConta(dados: ConectarContaPagamento): Observable<ContaPagamento> {
    return this.http.post<ContaPagamento>(`${this.baseUrl}/conta`, dados);
  }

  /** Conecta a conta Asaas do próprio clube pela chave de API que ele gerou no Asaas. */
  conectarContaPropria(dados: ConectarContaPropria): Observable<ContaPagamento> {
    return this.http.post<ContaPagamento>(`${this.baseUrl}/conta/propria`, dados);
  }

  desconectarConta(): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/conta/desconectar`, {});
  }

  atualizarConta(): Observable<ContaPagamento> {
    return this.http.post<ContaPagamento>(`${this.baseUrl}/conta/atualizar`, {});
  }

  /** Envia o arquivo de um documento que não tem página de envio do gateway (PDF, JPEG ou PNG, até 10MB). */
  enviarDocumento(id: string, arquivo: File): Observable<ContaPagamento> {
    const formulario = new FormData();
    formulario.append('arquivo', arquivo);
    return this.http.post<ContaPagamento>(`${this.baseUrl}/conta/documentos/${encodeURIComponent(id)}`, formulario);
  }
}
