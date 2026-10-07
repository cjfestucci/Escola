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
  /** Equipe do produto (não do cliente): só configura o ambiente. Não conta como "equipe" nem "gestão" de propósito. */
  readonly ehSuporte = computed(() => this.papel() === 'Suporte');
  readonly ehGestao = computed(() => this.papel() === 'Admin' || this.papel() === 'Coordenador');
  /** Identidade da escola (nome, logo, fuso): só o Admin — o dono da conta — e o Suporte. O Coordenador mexe só na cor. */
  readonly podeEditarIdentidade = computed(() => this.papel() === 'Admin' || this.papel() === 'Suporte');
  readonly ehFinanceiro = computed(() => this.ehGestao() || this.papel() === 'Financeiro');
  /** Telas de Configurações: Gestão do cliente e Suporte. */
  readonly podeConfigurar = computed(() => this.ehGestao() || this.ehSuporte());

  /** Pede o link de redefinição por e-mail. A resposta é sempre a mesma, exista a conta ou não. */
  esqueciSenha(email: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/auth/esqueci-senha`, { email });
  }

  redefinirSenha(token: string, novaSenha: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/auth/redefinir-senha`, { token, novaSenha });
  }

  /** Contas com segundo fator (o Suporte) respondem primeiro com <c>requerSegundoFator</c>, sem token: a tela pede o código
   * do app autenticador e chama de novo com ele. */
  entrar(email: string, senha: string, codigo?: string, clienteId?: string): Observable<LoginResposta> {
    return this.http
      .post<LoginResposta>(`${this.baseUrl}/auth/entrar`, { email, senha, codigo: codigo || null, clienteId: clienteId || null })
      .pipe(
      tap((resposta) => {
        // Etapas intermediárias (escolher a escola, código do autenticador) ainda não trazem token.
        if (resposta.requerSegundoFator || resposta.escolherCliente?.length) return;
        this._token.set(resposta.token);
        this.gravarStorage(CHAVE_TOKEN, resposta.token);
        this._identidade.set(this.decodificar(resposta.token));
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
        responsavelId: payload['responsavelId'] ?? null,
        clienteId: payload['clienteId'] ?? null
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
