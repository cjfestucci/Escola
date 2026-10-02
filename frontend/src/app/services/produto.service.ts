import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { CriarOuEditarProduto, Produto } from '../models/produto.model';

@Injectable({ providedIn: 'root' })
export class ProdutoService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/estoque/produtos`;

  listar(): Observable<Produto[]> {
    return this.http.get<Produto[]>(this.baseUrl);
  }

  obterPorId(id: string): Observable<Produto> {
    return this.http.get<Produto>(`${this.baseUrl}/${id}`);
  }

  criar(payload: CriarOuEditarProduto): Observable<Produto> {
    return this.http.post<Produto>(this.baseUrl, payload);
  }

  editar(id: string, payload: CriarOuEditarProduto): Observable<Produto> {
    return this.http.put<Produto>(`${this.baseUrl}/${id}`, payload);
  }

  desativar(id: string): Observable<Produto> {
    return this.http.post<Produto>(`${this.baseUrl}/${id}/desativar`, {});
  }

  ativar(id: string): Observable<Produto> {
    return this.http.post<Produto>(`${this.baseUrl}/${id}/ativar`, {});
  }
}
