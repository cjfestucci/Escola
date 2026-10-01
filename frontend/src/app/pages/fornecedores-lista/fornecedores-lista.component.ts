import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { Fornecedor } from '../../models/fornecedor.model';
import { NotificacaoService } from '../../services/notificacao.service';
import { FornecedorService } from '../../services/fornecedor.service';
import { LogsModalComponent } from '../../shared/logs-modal/logs-modal.component';

type StatusFiltro = 'todos' | 'ativos' | 'inativos';

@Component({
  selector: 'app-fornecedores-lista',
  imports: [FormsModule, RouterLink, LogsModalComponent],
  templateUrl: './fornecedores-lista.component.html',
  styleUrl: './fornecedores-lista.component.scss'
})
export class FornecedoresListaComponent implements OnInit {
  private readonly fornecedorService = inject(FornecedorService);
  private readonly router = inject(Router);
  private readonly notificacao = inject(NotificacaoService);

  readonly fornecedores = signal<Fornecedor[]>([]);
  readonly carregando = signal(true);
  readonly processandoId = signal<string | null>(null);
  readonly historicoAbertoId = signal<string | null>(null);

  readonly filtroNome = signal('');
  readonly filtroStatus = signal<StatusFiltro>('todos');

  readonly fornecedoresFiltrados = computed(() => {
    const nome = this.filtroNome().trim().toLowerCase();
    const status = this.filtroStatus();
    return this.fornecedores().filter((f) => {
      const bateNome = !nome || f.nome.toLowerCase().includes(nome);
      const bateStatus = status === 'todos' || (status === 'ativos' ? f.ativo : !f.ativo);
      return bateNome && bateStatus;
    });
  });

  ngOnInit(): void {
    this.carregar();
  }

  private carregar(): void {
    this.carregando.set(true);
    this.fornecedorService.listar().subscribe({
      next: (fornecedores) => {
        this.fornecedores.set(fornecedores);
        this.carregando.set(false);
      },
      error: () => {
        this.carregando.set(false);
        this.notificacao.erro('Não foi possível carregar os fornecedores.');
      }
    });
  }

  limparFiltros(): void {
    this.filtroNome.set('');
    this.filtroStatus.set('todos');
  }

  novo(): void {
    this.router.navigateByUrl('/fornecedores/novo');
  }

  editar(fornecedor: Fornecedor): void {
    this.router.navigate(['/fornecedores', fornecedor.id, 'editar']);
  }

  abrirHistorico(fornecedorId: string): void {
    this.historicoAbertoId.set(fornecedorId);
  }

  fecharHistorico(): void {
    this.historicoAbertoId.set(null);
  }

  alternarStatus(fornecedor: Fornecedor): void {
    this.processandoId.set(fornecedor.id);
    const requisicao$ = fornecedor.ativo
      ? this.fornecedorService.desativar(fornecedor.id)
      : this.fornecedorService.ativar(fornecedor.id);

    requisicao$.subscribe({
      next: (atualizado) => {
        this.fornecedores.update((atual) => atual.map((f) => (f.id === atualizado.id ? atualizado : f)));
        this.processandoId.set(null);
        this.notificacao.sucesso(atualizado.ativo ? 'Fornecedor reativado.' : 'Fornecedor desativado.');
      },
      error: (resposta) => {
        this.processandoId.set(null);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível atualizar o status do fornecedor.');
      }
    });
  }
}
