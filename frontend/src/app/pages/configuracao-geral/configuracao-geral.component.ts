import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { FUSOS_HORARIOS } from '../../models/configuracao-escola.model';
import { ConfiguracaoEscolaService } from '../../services/configuracao-escola.service';
import { NotificacaoService } from '../../services/notificacao.service';
import { LogsModalComponent } from '../../shared/logs-modal/logs-modal.component';

@Component({
  selector: 'app-configuracao-geral',
  imports: [FormsModule, LogsModalComponent],
  templateUrl: './configuracao-geral.component.html',
  styleUrl: './configuracao-geral.component.scss'
})
export class ConfiguracaoGeralComponent implements OnInit {
  private readonly configuracaoService = inject(ConfiguracaoEscolaService);
  private readonly notificacao = inject(NotificacaoService);

  readonly carregando = signal(true);
  readonly salvando = signal(false);
  readonly configuracaoId = signal<string | null>(null);
  readonly historicoAberto = signal(false);

  fusoHorario = '';
  fusos = FUSOS_HORARIOS;

  ngOnInit(): void {
    this.configuracaoService.obter().subscribe({
      next: (config) => {
        this.fusoHorario = config.fusoHorario;
        this.configuracaoId.set(config.id);
        // Fuso salvo por fora da lista (ex.: direto na API) continua aparecendo como opção
        if (!this.fusos.some((f) => f.valor === config.fusoHorario)) {
          this.fusos = [...this.fusos, { valor: config.fusoHorario, rotulo: config.fusoHorario }];
        }
        this.carregando.set(false);
      },
      error: () => {
        this.carregando.set(false);
        this.notificacao.erro('Não foi possível carregar a configuração.');
      }
    });
  }

  salvar(): void {
    this.salvando.set(true);
    this.configuracaoService.editar(this.fusoHorario).subscribe({
      next: (config) => {
        this.configuracaoId.set(config.id);
        this.salvando.set(false);
        this.notificacao.sucesso('Configuração salva com sucesso.');
      },
      error: (resposta) => {
        this.salvando.set(false);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível salvar a configuração.');
      }
    });
  }

  abrirHistorico(): void {
    this.historicoAberto.set(true);
  }

  fecharHistorico(): void {
    this.historicoAberto.set(false);
  }
}
