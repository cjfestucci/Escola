import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';

export interface UploadResult {
  url: string;
}

@Injectable({ providedIn: 'root' })
export class UploadService {
  private readonly http = inject(HttpClient);

  enviarFoto(arquivo: File): Observable<UploadResult> {
    const formData = new FormData();
    formData.append('arquivo', arquivo);
    return this.http.post<UploadResult>(`${environment.apiUrl}/uploads`, formData);
  }
}
