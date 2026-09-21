import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { FichaSaude } from '../models/ficha-saude.model';

@Injectable({ providedIn: 'root' })
export class FichaSaudeService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  obter(alunoId: string): Observable<FichaSaude> {
    return this.http.get<FichaSaude>(`${this.baseUrl}/alunos/${alunoId}/ficha-saude`);
  }

  salvar(alunoId: string, ficha: FichaSaude): Observable<FichaSaude> {
    return this.http.put<FichaSaude>(`${this.baseUrl}/alunos/${alunoId}/ficha-saude`, ficha);
  }
}
