import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';

import { environment } from '../../environments/environment';
import { IdentidadeAtual, LoginResposta, Papel } from '../models/auth.model';

const CHAVE_TOKEN = 'escola.token';

const PAPEIS_EQUIPE: Papel[] = ['Admin', 'Coordenador', 'Educador', 'Financeiro'];

/** Login de verdade (JWT). Token de vida longa pra não pedir senha toda hora — expira em 30 dias. */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  private readonly _token = signal<string | null>(this.lerTokenValido());
  private readonly _identidade = signal<IdentidadeAtual | null>(this.decodificar(this._token()));

  readonly identidade = this._identidade.asReadonly();
  readonly estaLogado = computed(() => this._identidade() !== null);
  readonly papel = computed(() => this._identidade()?.papel ?? null);
  readonly ehEquipe = computed(() => {
    const papel = this.papel();
    return papel !== null && PAPEIS_EQUIPE.includes(papel);
  });
  readonly ehResponsavel = computed(() => this.papel() === 'Responsavel');
  readonly ehGestao = computed(() => this.papel() === 'Admin' || this.papel() === 'Coordenador');

  entrar(email: string, senha: string): Observable<LoginResposta> {
    return this.http.post<LoginResposta>(`${this.baseUrl}/auth/entrar`, { email, senha }).pipe(
      tap((resposta) => {
        this._token.set(resposta.token);
        this.gravarStorage(CHAVE_TOKEN, resposta.token);
        this._identidade.set({
          usuarioId: resposta.usuarioId,
          nome: resposta.nome,
          papel: resposta.papel,
          responsavelId: resposta.responsavelId
        });
      })
    );
  }

  sair(): void {
    this._token.set(null);
    this._identidade.set(null);
    this.removerStorage(CHAVE_TOKEN);
  }

  obterToken(): string | null {
    return this._token();
  }

  private lerTokenValido(): string | null {
    const token = this.lerStorage(CHAVE_TOKEN);
    if (!token) return null;
    return this.decodificar(token) ? token : null;
  }

  private decodificar(token: string | null): IdentidadeAtual | null {
    if (!token) return null;

    try {
      const payload = JSON.parse(this.base64UrlDecode(token.split('.')[1]));
      const expiraEm = payload.exp as number | undefined;
      if (expiraEm && Date.now() >= expiraEm * 1000) return null;

      return {
        usuarioId: payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'],
        nome: payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'],
        papel: payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'],
        responsavelId: payload['responsavelId'] ?? null
      };
    } catch {
      return null;
    }
  }

  private base64UrlDecode(segmento: string): string {
    const base64 = segmento.replace(/-/g, '+').replace(/_/g, '/');
    const preenchido = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
    return decodeURIComponent(
      atob(preenchido)
        .split('')
        .map((c) => '%' + c.charCodeAt(0).toString(16).padStart(2, '0'))
        .join('')
    );
  }

  private lerStorage(chave: string): string | null {
    try {
      return localStorage.getItem(chave);
    } catch {
      return null;
    }
  }

  private gravarStorage(chave: string, valor: string): void {
    try {
      localStorage.setItem(chave, valor);
    } catch {
      // localStorage indisponível (ex.: navegação privada) — segue só em memória
    }
  }

  private removerStorage(chave: string): void {
    try {
      localStorage.removeItem(chave);
    } catch {
      // segue só em memória
    }
  }
}
