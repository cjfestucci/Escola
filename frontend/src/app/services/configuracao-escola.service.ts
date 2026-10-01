import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, firstValueFrom, tap } from 'rxjs';

import { environment } from '../../environments/environment';
import { ConfiguracaoEscola } from '../models/configuracao-escola.model';
import { definirFusoEscola } from '../shared/data-utils';

const CHAVE_FUSO = 'escola.fusoHorario';

/** Fuso horário da escola — carregado uma vez na inicialização do app (`provideAppInitializer`),
 * antes de qualquer tela calcular "hoje". Guarda uma cópia em localStorage pro app abrir no fuso
 * certo mesmo sem rede (PWA). */
@Injectable({ providedIn: 'root' })
export class ConfiguracaoEscolaService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  async inicializar(): Promise<void> {
    const emCache = this.lerCache();
    if (emCache) definirFusoEscola(emCache);
    try {
      await firstValueFrom(this.obter());
    } catch {
      // API fora do ar: segue com o fuso em cache (ou o padrão)
    }
  }

  obter(): Observable<ConfiguracaoEscola> {
    return this.http.get<ConfiguracaoEscola>(`${this.baseUrl}/configuracao/escola`).pipe(tap((config) => this.aplicar(config)));
  }

  editar(fusoHorario: string): Observable<ConfiguracaoEscola> {
    return this.http
      .put<ConfiguracaoEscola>(`${this.baseUrl}/configuracao/escola`, { fusoHorario })
      .pipe(tap((config) => this.aplicar(config)));
  }

  private aplicar(config: ConfiguracaoEscola): void {
    definirFusoEscola(config.fusoHorario);
    try {
      localStorage.setItem(CHAVE_FUSO, config.fusoHorario);
    } catch {
      // localStorage indisponível (ex.: navegação privada) — segue só em memória
    }
  }

  private lerCache(): string | null {
    try {
      return localStorage.getItem(CHAVE_FUSO);
    } catch {
      return null;
    }
  }
}
