import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { Aluno } from '../models/aluno.model';
import { Responsavel } from '../models/responsavel.model';

@Injectable({ providedIn: 'root' })
export class ResponsavelService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  listar(): Observable<Responsavel[]> {
    return this.http.get<Responsavel[]>(`${this.baseUrl}/responsaveis`);
  }

  listarFilhos(responsavelId: string): Observable<Aluno[]> {
    return this.http.get<Aluno[]>(`${this.baseUrl}/responsaveis/${responsavelId}/alunos`);
  }
}
