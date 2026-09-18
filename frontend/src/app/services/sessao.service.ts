import { Injectable, computed, inject, signal } from '@angular/core';

import { Turma } from '../models/aluno.model';
import { AuthService } from './auth.service';

const CHAVE_TURMA_ATIVA_ID = 'escola.turmaAtivaId';

/** Estado de UI derivado da identidade logada (AuthService) — turma ativa do educador, etc. */
@Injectable({ providedIn: 'root' })
export class SessaoService {
  private readonly auth = inject(AuthService);

  readonly educadorId = computed(() => (this.auth.ehEquipe() ? this.auth.identidade()?.usuarioId ?? null : null));
  readonly educadorNome = computed(() => (this.auth.ehEquipe() ? this.auth.identidade()?.nome ?? null : null));
  readonly responsavelId = computed(() => this.auth.identidade()?.responsavelId ?? null);
  readonly responsavelNome = computed(() => (this.auth.ehResponsavel() ? this.auth.identidade()?.nome ?? null : null));

  /** Turmas do educador logado — pode ter mais de uma. */
  readonly turmas = signal<Turma[]>([]);
  readonly turmaAtivaId = signal<string | null>(this.lerStorage(CHAVE_TURMA_ATIVA_ID));

  definirTurmas(turmas: Turma[]): void {
    this.turmas.set(turmas);

    const ativaAindaValida = turmas.some((t) => t.id === this.turmaAtivaId());
    if (!ativaAindaValida) {
      this.definirTurmaAtiva(turmas[0]?.id ?? null);
    }
  }

  definirTurmaAtiva(id: string | null): void {
    this.turmaAtivaId.set(id);
    if (id) {
      this.gravarStorage(CHAVE_TURMA_ATIVA_ID, id);
    } else {
      this.removerStorage(CHAVE_TURMA_ATIVA_ID);
    }
  }

  limpar(): void {
    this.turmas.set([]);
    this.turmaAtivaId.set(null);
    this.removerStorage(CHAVE_TURMA_ATIVA_ID);
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
