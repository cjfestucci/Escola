import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { CriarMovimentacaoEstoque, MovimentacaoEstoque } from '../models/movimentacao-estoque.model';

@Injectable({ providedIn: 'root' })
export class MovimentacaoEstoqueService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/estoque/movimentacoes`;

  listar(): Observable<MovimentacaoEstoque[]> {
    return this.http.get<MovimentacaoEstoque[]>(this.baseUrl);
  }

  criar(payload: CriarMovimentacaoEstoque): Observable<MovimentacaoEstoque> {
    return this.http.post<MovimentacaoEstoque>(this.baseUrl, payload);
  }
}
