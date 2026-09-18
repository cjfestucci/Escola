import { Injectable, signal } from '@angular/core';

const CHAVE_EDUCADOR_ID = 'escola.educadorId';
const CHAVE_EDUCADOR_NOME = 'escola.educadorNome';
const CHAVE_RESPONSAVEL_ID = 'escola.responsavelId';
const CHAVE_RESPONSAVEL_NOME = 'escola.responsavelNome';

/** Guarda quem está usando o app neste dispositivo, até existir login de verdade. */
@Injectable({ providedIn: 'root' })
export class SessaoService {
  readonly educadorId = signal<string | null>(this.lerStorage(CHAVE_EDUCADOR_ID));
  readonly educadorNome = signal<string | null>(this.lerStorage(CHAVE_EDUCADOR_NOME));
  readonly responsavelId = signal<string | null>(this.lerStorage(CHAVE_RESPONSAVEL_ID));
  readonly responsavelNome = signal<string | null>(this.lerStorage(CHAVE_RESPONSAVEL_NOME));

  definirEducador(id: string, nome: string): void {
    this.educadorId.set(id);
    this.educadorNome.set(nome);
    this.gravarStorage(CHAVE_EDUCADOR_ID, id);
    this.gravarStorage(CHAVE_EDUCADOR_NOME, nome);
  }

  definirResponsavel(id: string, nome: string): void {
    this.responsavelId.set(id);
    this.responsavelNome.set(nome);
    this.gravarStorage(CHAVE_RESPONSAVEL_ID, id);
    this.gravarStorage(CHAVE_RESPONSAVEL_NOME, nome);
  }

  sairEducador(): void {
    this.educadorId.set(null);
    this.educadorNome.set(null);
    this.removerStorage(CHAVE_EDUCADOR_ID);
    this.removerStorage(CHAVE_EDUCADOR_NOME);
  }

  sairResponsavel(): void {
    this.responsavelId.set(null);
    this.responsavelNome.set(null);
    this.removerStorage(CHAVE_RESPONSAVEL_ID);
    this.removerStorage(CHAVE_RESPONSAVEL_NOME);
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
