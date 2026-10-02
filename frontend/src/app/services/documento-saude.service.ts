import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { DocumentoSaude } from '../models/documento-saude.model';

@Injectable({ providedIn: 'root' })
export class DocumentoSaudeService {
  private readonly http = inject(HttpClient);

  private url(alunoId: string): string {
    return `${environment.apiUrl}/alunos/${alunoId}/documentos-saude`;
  }

  listar(alunoId: string): Observable<DocumentoSaude[]> {
    return this.http.get<DocumentoSaude[]>(this.url(alunoId));
  }

  enviar(alunoId: string, arquivo: File): Observable<DocumentoSaude> {
    const formData = new FormData();
    formData.append('arquivo', arquivo);
    return this.http.post<DocumentoSaude>(this.url(alunoId), formData);
  }

  /** O arquivo só sai por endpoint autenticado (não existe URL pública), então vem como Blob. */
  baixar(alunoId: string, documentoId: string): Observable<Blob> {
    return this.http.get(`${this.url(alunoId)}/${documentoId}/arquivo`, { responseType: 'blob' });
  }

  remover(alunoId: string, documentoId: string): Observable<void> {
    return this.http.delete<void>(`${this.url(alunoId)}/${documentoId}`);
  }
}
