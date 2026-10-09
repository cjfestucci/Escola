import { HttpClient } from '@angular/common/http';
import { Injectable, computed, effect, inject, signal, untracked } from '@angular/core';
import { Observable, tap } from 'rxjs';

import { environment } from '../../environments/environment';
import {
  CadastroAssinaturaRequest,
  CadastroAssinaturaResposta,
  MinhaAssinatura,
  PlanoAssinatura
} from '../models/assinatura.model';
import { AuthService } from './auth.service';

/** Assinatura do clube com a plataforma: o cadastro pelo site (anônimo) e a situação do clube logado (Admin/Suporte), que alimenta a
 * faixa de aviso no topo e a tela Configurações → Assinatura. */
@Injectable({ providedIn: 'root' })
export class AssinaturaService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);
  private readonly baseUrl = `${environment.apiUrl}/assinaturas`;

  private readonly _minha = signal<MinhaAssinatura | null>(null);
  /** Assinatura do clube logado; nula sem login, pra quem não é Admin, ou pra clube sem assinatura pelo site. */
  readonly minha = this._minha.asReadonly();
  readonly bloqueada = computed(() => this._minha()?.bloqueada ?? false);

  constructor() {
    // Entrar/sair/trocar de conta: o Admin carrega a assinatura do clube dele; os demais não veem a faixa.
    effect(() => {
      const papel = this.auth.papel();
      const cliente = this.auth.identidade()?.clienteId ?? null;
      untracked(() => {
        this._minha.set(null);
        if (papel === 'Admin' && cliente) this.carregar().subscribe({ error: () => undefined });
      });
    });
  }

  plano(): Observable<PlanoAssinatura> {
    return this.http.get<PlanoAssinatura>(`${this.baseUrl}/plano`);
  }

  cadastrar(dados: CadastroAssinaturaRequest): Observable<CadastroAssinaturaResposta> {
    return this.http.post<CadastroAssinaturaResposta>(this.baseUrl, dados);
  }

  carregar(): Observable<MinhaAssinatura> {
    return this.http.get<MinhaAssinatura>(`${this.baseUrl}/minha`).pipe(tap((a) => this._minha.set(a)));
  }

  atualizar(): Observable<MinhaAssinatura> {
    return this.http.post<MinhaAssinatura>(`${this.baseUrl}/minha/atualizar`, null).pipe(tap((a) => this._minha.set(a)));
  }
}
