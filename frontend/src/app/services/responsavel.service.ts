import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { Aluno } from '../models/aluno.model';

@Injectable({ providedIn: 'root' })
export class ResponsavelService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  listarFilhos(responsavelId: string): Observable<Aluno[]> {
    return this.http.get<Aluno[]>(`${this.baseUrl}/responsaveis/${responsavelId}/alunos`);
  }
}
