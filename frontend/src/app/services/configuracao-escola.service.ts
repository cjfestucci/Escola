import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, firstValueFrom, tap } from 'rxjs';

import { environment } from '../../environments/environment';
import { ConfiguracaoEscola } from '../models/configuracao-escola.model';
import { definirFusoEscola } from '../shared/data-utils';
import { aplicarTema } from '../shared/tema';
import { SegmentoService } from './segmento.service';

const CHAVE_FUSO = 'escola.fusoHorario';
const CHAVE_COR = 'escola.corPrincipal';

/** Configuração geral da escola (fuso horário, cor do tema e segmento do cliente) — carregada uma vez na inicialização do app
 * (`provideAppInitializer`), antes de qualquer tela calcular "hoje" ou pintar. Guarda uma cópia em
 * localStorage pro app abrir no fuso e na cor certos mesmo sem rede (PWA) e sem piscar a cor padrão. */
@Injectable({ providedIn: 'root' })
export class ConfiguracaoEscolaService {
  private readonly http = inject(HttpClient);
  private readonly segmentoService = inject(SegmentoService);
  private readonly baseUrl = environment.apiUrl;

  async inicializar(): Promise<void> {
    const fusoEmCache = this.ler(CHAVE_FUSO);
    if (fusoEmCache) definirFusoEscola(fusoEmCache);
    aplicarTema(this.ler(CHAVE_COR));
    try {
      await firstValueFrom(this.obter());
    } catch {
      // API fora do ar: segue com o fuso e a cor em cache (ou os padrões)
    }
  }

  obter(): Observable<ConfiguracaoEscola> {
    return this.http.get<ConfiguracaoEscola>(`${this.baseUrl}/configuracao/escola`).pipe(tap((config) => this.aplicar(config)));
  }

  /** Configurações → Geral (Gestão): só a cor. O fuso é do Suporte, na Plataforma ({@link editarFuso}). */
  editar(corPrincipal: string | null): Observable<ConfiguracaoEscola> {
    return this.http
      .put<ConfiguracaoEscola>(`${this.baseUrl}/configuracao/escola`, { corPrincipal })
      .pipe(tap((config) => this.aplicar(config)));
  }

  /** Tela Plataforma (só Suporte): troca o fuso horário da escola e já aplica no app. */
  editarFuso(fusoHorario: string): Observable<ConfiguracaoEscola> {
    return this.http
      .put<ConfiguracaoEscola>(`${this.baseUrl}/plataforma/fuso`, { fusoHorario })
      .pipe(tap((config) => this.aplicar(config)));
  }

  private aplicar(config: ConfiguracaoEscola): void {
    definirFusoEscola(config.fusoHorario);
    aplicarTema(config.corPrincipal);
    this.segmentoService.aplicarDoCliente(config.segmento);
    this.segmentoService.aplicarLogo(config.logoUrl);
    this.guardar(CHAVE_FUSO, config.fusoHorario);
    this.guardar(CHAVE_COR, config.corPrincipal);
  }

  private guardar(chave: string, valor: string | null): void {
    try {
      if (valor) localStorage.setItem(chave, valor);
      else localStorage.removeItem(chave);
    } catch {
      // localStorage indisponível (ex.: navegação privada) — segue só em memória
    }
  }

  private ler(chave: string): string | null {
    try {
      return localStorage.getItem(chave);
    } catch {
      return null;
    }
  }
}
