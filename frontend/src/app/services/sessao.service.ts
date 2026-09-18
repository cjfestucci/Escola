import { Injectable, signal } from '@angular/core';

const CHAVE_STORAGE = 'escola.educadorId';

/** Guarda quem está usando o app neste dispositivo, até existir login de verdade. */
@Injectable({ providedIn: 'root' })
export class SessaoService {
  readonly educadorId = signal<string | null>(this.lerStorage());

  definirEducador(id: string): void {
    this.educadorId.set(id);
    try {
      localStorage.setItem(CHAVE_STORAGE, id);
    } catch {
      // localStorage indisponível (ex.: navegação privada) — segue só em memória
    }
  }

  private lerStorage(): string | null {
    try {
      return localStorage.getItem(CHAVE_STORAGE);
    } catch {
      return null;
    }
  }
}
