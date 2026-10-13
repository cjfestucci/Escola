import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, map, of } from 'rxjs';

import { Turma } from '../models/aluno.model';
import { AuthService } from './auth.service';
import { UsuarioService } from './usuario.service';

const CHAVE_TURMA_ATIVA_ID = 'escola.turmaAtivaId';

/** Estado de UI derivado da identidade logada (AuthService) — turma ativa do educador, etc. */
@Injectable({ providedIn: 'root' })
export class SessaoService {
  private readonly auth = inject(AuthService);

  readonly educadorId = computed(() => (this.auth.ehEquipe() ? this.auth.identidade()?.usuarioId ?? null : null));
  readonly educadorNome = computed(() => (this.auth.ehEquipe() ? this.auth.identidade()?.nome ?? null : null));
  readonly suporteNome = computed(() => (this.auth.ehSuporte() ? this.auth.identidade()?.nome ?? null : null));
  readonly responsavelId = computed(() => this.auth.identidade()?.responsavelId ?? null);
  readonly responsavelNome = computed(() => (this.auth.ehResponsavel() ? this.auth.identidade()?.nome ?? null : null));
  /** Professor/Treinador: só lança nas turmas em que está vinculado (o servidor recusa as outras com 403). */
  readonly ehEducador = computed(() => this.auth.papel() === 'Educador');

  private readonly usuarioService = inject(UsuarioService);
  /** Turmas em que o professor logado está vinculado (nulo = ainda não carregou). Usado pra esconder ações que o servidor recusaria. */
  private readonly turmasVinculadas = signal<{ usuarioId: string; ids: Set<string> } | null>(null);

  /** Carrega (uma vez por sessão) as turmas do professor logado; completa quando dá pra usar <c>podeLancarNaTurma</c>. Pra outros
   * perfis completa na hora. */
  carregarTurmasVinculadas(): Observable<void> {
    const usuarioId = this.educadorId();
    if (!this.ehEducador() || !usuarioId || this.turmasVinculadas()?.usuarioId === usuarioId) return of(undefined);
    return this.usuarioService.listarTurmas(usuarioId).pipe(
      catchError(() => of([] as Turma[])),
      map((turmas) => this.turmasVinculadas.set({ usuarioId, ids: new Set(turmas.map((t) => t.id)) }))
    );
  }

  /** Pode lançar (chamada, jogo, convocação, rotina, diário) nesta turma? Gestão/Financeiro: sempre; professor: só nas dele;
   * Secretaria: nunca (só consulta). */
  podeLancarNaTurma(turmaId: string): boolean {
    if (this.auth.ehSecretaria()) return false;
    if (!this.ehEducador()) return true;
    const cache = this.turmasVinculadas();
    return cache?.usuarioId === this.educadorId() && cache.ids.has(turmaId);
  }

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
    this.turmasVinculadas.set(null);
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
