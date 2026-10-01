import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { LogAuditoria } from '../models/log-auditoria.model';

@Injectable({ providedIn: 'root' })
export class LogAuditoriaService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  listar(entidadeTipo: string, entidadeId: string): Observable<LogAuditoria[]> {
    return this.http.get<LogAuditoria[]>(`${this.baseUrl}/logs`, { params: { entidadeTipo, entidadeId } });
  }

  listarPorTurma(turmaId: string, data: string): Observable<LogAuditoria[]> {
    return this.http.get<LogAuditoria[]>(`${this.baseUrl}/logs/turma`, { params: { turmaId, data } });
  }
}
