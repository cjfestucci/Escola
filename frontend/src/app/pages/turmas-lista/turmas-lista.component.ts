import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { Periodo, Turma } from '../../models/aluno.model';
import { AuthService } from '../../services/auth.service';
import { NotificacaoService } from '../../services/notificacao.service';
import { TurmaService } from '../../services/turma.service';
import { LogsModalComponent } from '../../shared/logs-modal/logs-modal.component';

const ROTULO_PERIODO: Record<string, string> = {
  Manha: 'Manhã',
  Tarde: 'Tarde',
  Integral: 'Integral',
  Noite: 'Noite'
};

const SEM_PROFESSOR = 'sem-professor';

interface OpcaoProfessor {
  id: string;
  nome: string;
}

type StatusFiltro = 'todas' | 'ativas' | 'inativas';

@Component({
  selector: 'app-turmas-lista',
  imports: [FormsModule, LogsModalComponent],
  templateUrl: './turmas-lista.component.html',
  styleUrl: './turmas-lista.component.scss'
})
export class TurmasListaComponent implements OnInit {
  private readonly turmaService = inject(TurmaService);
  private readonly router = inject(Router);
  private readonly notificacao = inject(NotificacaoService);
  protected readonly auth = inject(AuthService);

  readonly turmas = signal<Turma[]>([]);
  readonly carregando = signal(true);
  readonly processandoId = signal<string | null>(null);
  readonly historicoAbertoId = signal<string | null>(null);

  readonly filtroNome = signal('');
  readonly filtroPeriodo = signal<Periodo | ''>('');
  readonly filtroProfessorId = signal('');
  readonly filtroStatus = signal<StatusFiltro>('todas');

  protected readonly periodos: Periodo[] = ['Manha', 'Tarde', 'Integral', 'Noite'];
  protected readonly semProfessor = SEM_PROFESSOR;

  readonly professoresDisponiveis = computed<OpcaoProfessor[]>(() => {
    const porId = new Map<string, string>();
    for (const turma of this.turmas()) {
      if (turma.professorId) porId.set(turma.professorId, turma.professorNome ?? '');
    }
    return [...porId.entries()]
      .map(([id, nome]) => ({ id, nome }))
      .sort((a, b) => a.nome.localeCompare(b.nome, 'pt-BR'));
  });

  readonly turmasFiltradas = computed(() => {
    const nome = this.filtroNome().trim().toLowerCase();
    const periodo = this.filtroPeriodo();
    const professorId = this.filtroProfessorId();
    const status = this.filtroStatus();

    return this.turmas().filter((turma) => {
      const bateNome = !nome || turma.nome.toLowerCase().includes(nome);
      const batePeriodo = !periodo || turma.periodo === periodo;
      const bateProfessor =
        !professorId ||
        (professorId === SEM_PROFESSOR ? !turma.professorId : turma.professorId === professorId);
      const bateStatus = status === 'todas' || (status === 'ativas' ? turma.ativa : !turma.ativa);
      return bateNome && batePeriodo && bateProfessor && bateStatus;
    });
  });

  ngOnInit(): void {
    this.carregar();
  }

  limparFiltros(): void {
    this.filtroNome.set('');
    this.filtroPeriodo.set('');
    this.filtroProfessorId.set('');
    this.filtroStatus.set('todas');
  }

  private carregar(): void {
    this.carregando.set(true);
    this.turmaService.listar().subscribe({
      next: (turmas) => {
        this.turmas.set(turmas);
        this.carregando.set(false);
      },
      error: () => {
        this.carregando.set(false);
        this.notificacao.erro('Não foi possível carregar as turmas.');
      }
    });
  }

  rotuloPeriodo(periodo: string): string {
    return ROTULO_PERIODO[periodo] ?? periodo;
  }

  nova(): void {
    this.router.navigateByUrl('/turmas/nova');
  }

  editar(turma: Turma): void {
    this.router.navigate(['/turmas', turma.id, 'editar']);
  }

  abrirHistorico(turmaId: string): void {
    this.historicoAbertoId.set(turmaId);
  }

  fecharHistorico(): void {
    this.historicoAbertoId.set(null);
  }

  alternarStatus(turma: Turma): void {
    this.processandoId.set(turma.id);
    const requisicao$ = turma.ativa ? this.turmaService.desativar(turma.id) : this.turmaService.ativar(turma.id);

    requisicao$.subscribe({
      next: (atualizada) => {
        this.turmas.update((atual) => atual.map((t) => (t.id === atualizada.id ? atualizada : t)));
        this.processandoId.set(null);
        this.notificacao.sucesso(atualizada.ativa ? 'Turma reativada.' : 'Turma desativada.');
      },
      error: (resposta) => {
        this.processandoId.set(null);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível atualizar o status da turma.');
      }
    });
  }
}
