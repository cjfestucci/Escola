import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { Campeonato } from '../../models/competicao.model';
import { AuthService } from '../../services/auth.service';
import { CampeonatoService } from '../../services/campeonato.service';
import { NotificacaoService } from '../../services/notificacao.service';
import { formatarDataAbsoluta } from '../../shared/data-utils';
import { LogsModalComponent } from '../../shared/logs-modal/logs-modal.component';

type StatusFiltro = 'todos' | 'ativos' | 'inativos';

@Component({
  selector: 'app-campeonatos-lista',
  imports: [FormsModule, RouterLink, LogsModalComponent],
  templateUrl: './campeonatos-lista.component.html',
  styleUrl: './campeonatos-lista.component.scss'
})
export class CampeonatosListaComponent implements OnInit {
  private readonly campeonatoService = inject(CampeonatoService);
  private readonly router = inject(Router);
  private readonly notificacao = inject(NotificacaoService);
  protected readonly auth = inject(AuthService);

  readonly campeonatos = signal<Campeonato[]>([]);
  readonly carregando = signal(true);
  readonly processandoId = signal<string | null>(null);
  readonly historicoAbertoId = signal<string | null>(null);

  readonly filtroNome = signal('');
  readonly filtroStatus = signal<StatusFiltro>('ativos');

  protected readonly formatarDataAbsoluta = formatarDataAbsoluta;

  readonly campeonatosFiltrados = computed(() => {
    const nome = this.filtroNome().trim().toLowerCase();
    const status = this.filtroStatus();
    return this.campeonatos().filter((c) => {
      const bateNome = !nome || c.nome.toLowerCase().includes(nome);
      const bateStatus = status === 'todos' || (status === 'ativos' ? c.ativo : !c.ativo);
      return bateNome && bateStatus;
    });
  });

  ngOnInit(): void {
    this.campeonatoService.listar().subscribe({
      next: (campeonatos) => {
        this.campeonatos.set(campeonatos);
        this.carregando.set(false);
      },
      error: () => {
        this.carregando.set(false);
        this.notificacao.erro('Não foi possível carregar os campeonatos.');
      }
    });
  }

  limparFiltros(): void {
    this.filtroNome.set('');
    this.filtroStatus.set('ativos');
  }

  novo(): void {
    this.router.navigateByUrl('/campeonatos/novo');
  }

  editar(campeonato: Campeonato): void {
    this.router.navigate(['/campeonatos', campeonato.id, 'editar']);
  }

  abrirHistorico(id: string): void {
    this.historicoAbertoId.set(id);
  }

  fecharHistorico(): void {
    this.historicoAbertoId.set(null);
  }

  alternarStatus(campeonato: Campeonato): void {
    this.processandoId.set(campeonato.id);
    const requisicao$ = campeonato.ativo ? this.campeonatoService.desativar(campeonato.id) : this.campeonatoService.ativar(campeonato.id);

    requisicao$.subscribe({
      next: (atualizado) => {
        this.campeonatos.update((atual) => atual.map((c) => (c.id === atualizado.id ? atualizado : c)));
        this.processandoId.set(null);
        this.notificacao.sucesso(atualizado.ativo ? 'Campeonato reativado.' : 'Campeonato desativado.');
      },
      error: (resposta) => {
        this.processandoId.set(null);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível atualizar o status do campeonato.');
      }
    });
  }
}
