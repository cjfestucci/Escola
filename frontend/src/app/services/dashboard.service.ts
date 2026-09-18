import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { ResumoDashboard } from '../models/resumo-dashboard.model';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);

  resumo(): Observable<ResumoDashboard> {
    return this.http.get<ResumoDashboard>(`${environment.apiUrl}/dashboard/resumo`);
  }
}
