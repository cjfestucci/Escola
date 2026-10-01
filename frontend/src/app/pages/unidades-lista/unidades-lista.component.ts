import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { Unidade } from '../../models/unidade.model';
import { AuthService } from '../../services/auth.service';
import { NotificacaoService } from '../../services/notificacao.service';
import { UnidadeService } from '../../services/unidade.service';
import { LogsModalComponent } from '../../shared/logs-modal/logs-modal.component';

type StatusFiltro = 'todas' | 'ativas' | 'inativas';

@Component({
  selector: 'app-unidades-lista',
  imports: [FormsModule, LogsModalComponent],
  templateUrl: './unidades-lista.component.html',
  styleUrl: './unidades-lista.component.scss'
})
export class UnidadesListaComponent implements OnInit {
  private readonly unidadeService = inject(UnidadeService);
  private readonly router = inject(Router);
  private readonly notificacao = inject(NotificacaoService);
  protected readonly auth = inject(AuthService);

  readonly unidades = signal<Unidade[]>([]);
  readonly carregando = signal(true);
  readonly processandoId = signal<string | null>(null);
  readonly historicoAbertoId = signal<string | null>(null);

  readonly filtroNome = signal('');
  readonly filtroStatus = signal<StatusFiltro>('todas');

  readonly unidadesFiltradas = computed(() => {
    const nome = this.filtroNome().trim().toLowerCase();
    const status = this.filtroStatus();

    return this.unidades().filter((unidade) => {
      const bateNome = !nome || unidade.nome.toLowerCase().includes(nome);
      const bateStatus = status === 'todas' || (status === 'ativas' ? unidade.ativa : !unidade.ativa);
      return bateNome && bateStatus;
    });
  });

  ngOnInit(): void {
    this.carregar();
  }

  private carregar(): void {
    this.carregando.set(true);
    this.unidadeService.listar().subscribe({
      next: (unidades) => {
        this.unidades.set(unidades);
        this.carregando.set(false);
      },
      error: () => {
        this.carregando.set(false);
        this.notificacao.erro('Não foi possível carregar as unidades.');
      }
    });
  }

  limparFiltros(): void {
    this.filtroNome.set('');
    this.filtroStatus.set('todas');
  }

  nova(): void {
    this.router.navigateByUrl('/unidades/nova');
  }

  editar(unidade: Unidade): void {
    this.router.navigate(['/unidades', unidade.id, 'editar']);
  }

  abrirHistorico(unidadeId: string): void {
    this.historicoAbertoId.set(unidadeId);
  }

  fecharHistorico(): void {
    this.historicoAbertoId.set(null);
  }

  alternarStatus(unidade: Unidade): void {
    this.processandoId.set(unidade.id);
    const requisicao$ = unidade.ativa ? this.unidadeService.desativar(unidade.id) : this.unidadeService.ativar(unidade.id);

    requisicao$.subscribe({
      next: (atualizada) => {
        this.unidades.update((atual) => atual.map((u) => (u.id === atualizada.id ? atualizada : u)));
        this.processandoId.set(null);
        this.notificacao.sucesso(atualizada.ativa ? 'Unidade reativada.' : 'Unidade desativada.');
      },
      error: (resposta) => {
        this.processandoId.set(null);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível atualizar o status da unidade.');
      }
    });
  }
}
