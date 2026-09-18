import { Injectable, signal } from '@angular/core';

const CHAVE_EDUCADOR = 'escola.educadorId';
const CHAVE_RESPONSAVEL = 'escola.responsavelId';

/** Guarda quem está usando o app neste dispositivo, até existir login de verdade. */
@Injectable({ providedIn: 'root' })
export class SessaoService {
  readonly educadorId = signal<string | null>(this.lerStorage(CHAVE_EDUCADOR));
  readonly responsavelId = signal<string | null>(this.lerStorage(CHAVE_RESPONSAVEL));

  definirEducador(id: string): void {
    this.educadorId.set(id);
    this.gravarStorage(CHAVE_EDUCADOR, id);
  }

  definirResponsavel(id: string): void {
    this.responsavelId.set(id);
    this.gravarStorage(CHAVE_RESPONSAVEL, id);
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
}
