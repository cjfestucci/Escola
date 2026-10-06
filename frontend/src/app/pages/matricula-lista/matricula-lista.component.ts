import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { Aluno, rotuloPosicao } from '../../models/aluno.model';
import { rotuloAtestado, situacaoAtestado } from '../../models/atestado.model';
import { AlunoService } from '../../services/aluno.service';
import { AuthService } from '../../services/auth.service';
import { NotificacaoService } from '../../services/notificacao.service';
import { SegmentoService } from '../../services/segmento.service';
import { hojeIso, idadeFormatada } from '../../shared/data-utils';
import { LogsModalComponent } from '../../shared/logs-modal/logs-modal.component';
import { resolverFotoUrl } from '../../shared/registro-rotina-display';

interface OpcaoTurma {
  id: string;
  nome: string;
}

type StatusFiltro = 'todos' | 'ativos' | 'inativos' | 'bloqueados' | 'pendentes';
type AtestadoFiltro = '' | 'pendente' | 'vencido' | 'vencendo' | 'sem';

@Component({
  selector: 'app-matricula-lista',
  imports: [FormsModule, LogsModalComponent],
  templateUrl: './matricula-lista.component.html',
  styleUrl: './matricula-lista.component.scss'
})
export class MatriculaListaComponent implements OnInit {
  private readonly alunoService = inject(AlunoService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly notificacao = inject(NotificacaoService);
  protected readonly auth = inject(AuthService);
  protected readonly segmentoService = inject(SegmentoService);

  readonly alunos = signal<Aluno[]>([]);
  readonly carregando = signal(true);
  readonly processandoId = signal<string | null>(null);
  readonly historicoAbertoId = signal<string | null>(null);

  readonly filtroNome = signal('');
  readonly filtroTurmaId = signal('');
  readonly filtroStatus = signal<StatusFiltro>('todos');
  readonly filtroAtestado = signal<AtestadoFiltro>('');

  readonly turmasDisponiveis = computed<OpcaoTurma[]>(() => {
    const porId = new Map<string, string>();
    for (const aluno of this.alunos()) porId.set(aluno.turmaId, aluno.turmaNome);
    return [...porId.entries()]
      .map(([id, nome]) => ({ id, nome }))
      .sort((a, b) => a.nome.localeCompare(b.nome, 'pt-BR'));
  });

  readonly alunosFiltrados = computed(() => {
    const nome = this.filtroNome().trim().toLowerCase();
    const turmaId = this.filtroTurmaId();
    const status = this.filtroStatus();
    const atestado = this.filtroAtestado();
    const hoje = hojeIso();

    return this.alunos().filter((aluno) => {
      const bateNome = !nome || aluno.nome.toLowerCase().includes(nome);
      const bateTurma = !turmaId || aluno.turmaId === turmaId;
      const bateStatus =
        status === 'todos' ||
        (status === 'ativos' && aluno.ativo) ||
        (status === 'inativos' && !aluno.ativo) ||
        (status === 'bloqueados' && !!aluno.bloqueado) ||
        (status === 'pendentes' && !!aluno.matriculaPendente);
      const situacao = situacaoAtestado(aluno.atestadoValidoAte, hoje);
      const bateAtestado =
        !atestado || (atestado === 'pendente' ? situacao === 'vencido' || situacao === 'vencendo' : situacao === atestado);
      return bateNome && bateTurma && bateStatus && bateAtestado;
    });
  });

  protected readonly idade = idadeFormatada;
  protected readonly rotuloPosicao = rotuloPosicao;
  protected readonly rotuloAtestado = rotuloAtestado;
  protected readonly situacaoAtestado = situacaoAtestado;
  protected readonly resolverFotoUrl = resolverFotoUrl;

  // Foto cadastrada mas arquivo ausente/quebrado: volta pra inicial do nome em vez de mostrar imagem quebrada.
  readonly fotosComErro = signal<Set<string>>(new Set());

  registrarErroFoto(alunoId: string): void {
    this.fotosComErro.update((atual) => new Set(atual).add(alunoId));
  }

  ngOnInit(): void {
    // Atalhos do Dashboard abrem a lista já filtrada (ex.: /matricula?status=bloqueados).
    const status = this.route.snapshot.queryParamMap.get('status');
    if (status === 'ativos' || status === 'inativos' || status === 'bloqueados' || status === 'pendentes') this.filtroStatus.set(status);
    const atestado = this.route.snapshot.queryParamMap.get('atestado');
    if (atestado === 'pendente' || atestado === 'vencido' || atestado === 'vencendo' || atestado === 'sem') this.filtroAtestado.set(atestado);
    this.carregar();
  }

  private carregar(): void {
    this.carregando.set(true);
    this.alunoService.listarAlunos().subscribe({
      next: (alunos) => {
        this.alunos.set(alunos);
        this.carregando.set(false);
      },
      error: () => {
        this.carregando.set(false);
        this.notificacao.erro('Não foi possível carregar os alunos.');
      }
    });
  }

  limparFiltros(): void {
    this.filtroNome.set('');
    this.filtroTurmaId.set('');
    this.filtroStatus.set('todos');
    this.filtroAtestado.set('');
  }

  novo(): void {
    this.router.navigateByUrl('/matricula/novo');
  }

  editar(aluno: Aluno): void {
    this.router.navigate(['/matricula', aluno.id, 'editar']);
  }

  abrirHistorico(alunoId: string): void {
    this.historicoAbertoId.set(alunoId);
  }

  fecharHistorico(): void {
    this.historicoAbertoId.set(null);
  }

  alternarStatus(aluno: Aluno): void {
    this.processandoId.set(aluno.id);
    const requisicao$ = aluno.ativo ? this.alunoService.desativar(aluno.id) : this.alunoService.ativar(aluno.id);

    requisicao$.subscribe({
      next: (atualizado) => {
        this.alunos.update((atual) => atual.map((a) => (a.id === atualizado.id ? atualizado : a)));
        this.processandoId.set(null);
        this.notificacao.sucesso(atualizado.ativo ? 'Reativado(a) com sucesso.' : 'Desativado(a) com sucesso.');
      },
      error: (resposta) => {
        this.processandoId.set(null);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível atualizar o status.');
      }
    });
  }
}
