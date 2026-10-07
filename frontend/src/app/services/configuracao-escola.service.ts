import { HttpClient } from '@angular/common/http';
import { Injectable, effect, inject, untracked } from '@angular/core';
import { Observable, firstValueFrom, tap } from 'rxjs';

import { environment } from '../../environments/environment';
import { ConfiguracaoEscola } from '../models/configuracao-escola.model';
import { definirFusoEscola } from '../shared/data-utils';
import { aplicarTema } from '../shared/tema';
import { AuthService } from './auth.service';
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
  private readonly auth = inject(AuthService);
  private readonly baseUrl = environment.apiUrl;

  /** Escola cuja configuração está aplicada (undefined = ainda não carregou nenhuma). */
  private clienteAplicado: string | null | undefined = undefined;

  constructor() {
    // Todas as escolas usam a mesma tela de login: ao entrar, sair ou trocar de conta, a configuração (fuso, cor, segmento, logo)
    // passa a ser a da escola da sessão — ou o padrão do produto, sem login. A primeira carga fica com inicializar().
    effect(() => {
      const cliente = this.auth.identidade()?.clienteId ?? null;
      untracked(() => {
        if (this.clienteAplicado === undefined || cliente === this.clienteAplicado) return;
        this.clienteAplicado = cliente;
        this.obter().subscribe({ error: () => undefined });
      });
    });
  }

  async inicializar(): Promise<void> {
    this.clienteAplicado = this.auth.identidade()?.clienteId ?? null;
    const fusoEmCache = this.ler(CHAVE_FUSO);
    if (fusoEmCache) definirFusoEscola(fusoEmCache);
    aplicarTema(this.ler(CHAVE_COR));
    try {
      await firstValueFrom(this.obter());
    } catch {
      // API fora do ar: segue com o fuso e a cor em cache (ou os padrões)
    }
  }

  /** Carrega a configuração da escola da sessão atual (logo depois do login, antes de navegar: o redirecionamento inicial
   * depende do segmento da escola). Marca a escola como aplicada pro efeito de troca de sessão não buscar de novo. */
  aplicarDaSessao(): Observable<ConfiguracaoEscola> {
    this.clienteAplicado = this.auth.identidade()?.clienteId ?? null;
    return this.obter();
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

  /** Configurações → Geral (Admin ou Suporte): nome e fuso da escola; já aplica no app. */
  editarDados(nomeEscola: string, fusoHorario: string): Observable<ConfiguracaoEscola> {
    return this.http
      .put<ConfiguracaoEscola>(`${this.baseUrl}/configuracao/escola/dados`, { nomeEscola, fusoHorario })
      .pipe(tap((config) => this.aplicar(config)));
  }

  /** Logo da escola (Admin ou Suporte). Depois de enviar/remover, recarregue com {@link obter} pro menu e o cache pegarem a nova. */
  enviarLogo(arquivo: File): Observable<{ logoUrl: string | null }> {
    const formulario = new FormData();
    formulario.append('arquivo', arquivo);
    return this.http.put<{ logoUrl: string | null }>(`${this.baseUrl}/configuracao/escola/logo`, formulario);
  }

  removerLogo(): Observable<{ logoUrl: string | null }> {
    return this.http.delete<{ logoUrl: string | null }>(`${this.baseUrl}/configuracao/escola/logo`);
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
    this.segmentoService.aplicarNomeEscola(config.nomeEscola);
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
