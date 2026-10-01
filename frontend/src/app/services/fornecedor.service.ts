import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { CriarOuEditarFornecedor, Fornecedor } from '../models/fornecedor.model';

@Injectable({ providedIn: 'root' })
export class FornecedorService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  listar(): Observable<Fornecedor[]> {
    return this.http.get<Fornecedor[]>(`${this.baseUrl}/fornecedores`);
  }

  obterPorId(id: string): Observable<Fornecedor> {
    return this.http.get<Fornecedor>(`${this.baseUrl}/fornecedores/${id}`);
  }

  criar(payload: CriarOuEditarFornecedor): Observable<Fornecedor> {
    return this.http.post<Fornecedor>(`${this.baseUrl}/fornecedores`, payload);
  }

  editar(id: string, payload: CriarOuEditarFornecedor): Observable<Fornecedor> {
    return this.http.put<Fornecedor>(`${this.baseUrl}/fornecedores/${id}`, payload);
  }

  desativar(id: string): Observable<Fornecedor> {
    return this.http.post<Fornecedor>(`${this.baseUrl}/fornecedores/${id}/desativar`, {});
  }

  ativar(id: string): Observable<Fornecedor> {
    return this.http.post<Fornecedor>(`${this.baseUrl}/fornecedores/${id}/ativar`, {});
  }
}
