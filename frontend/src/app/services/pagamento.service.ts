import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { ConectarContaPagamento, ContaPagamento } from '../models/pagamento.model';

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

  atualizarConta(): Observable<ContaPagamento> {
    return this.http.post<ContaPagamento>(`${this.baseUrl}/conta/atualizar`, {});
  }
}
