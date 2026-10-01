import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { FinanceiroService } from '../../services/financeiro.service';
import { NotificacaoService } from '../../services/notificacao.service';

@Component({
  selector: 'app-configuracao-pix',
  imports: [FormsModule],
  templateUrl: './configuracao-pix.component.html',
  styleUrl: './configuracao-pix.component.scss'
})
export class ConfiguracaoPixComponent implements OnInit {
  private readonly financeiroService = inject(FinanceiroService);
  private readonly notificacao = inject(NotificacaoService);

  readonly carregando = signal(true);
  readonly salvando = signal(false);

  pixChave = '';
  pixNomeRecebedor = '';
  pixCidade = '';

  ngOnInit(): void {
    this.financeiroService.obterConfiguracao().subscribe({
      next: (config) => {
        this.pixChave = config.pixChave ?? '';
        this.pixNomeRecebedor = config.pixNomeRecebedor ?? '';
        this.pixCidade = config.pixCidade ?? '';
        this.carregando.set(false);
      },
      error: () => {
        this.carregando.set(false);
        this.notificacao.erro('Não foi possível carregar a configuração Pix.');
      }
    });
  }

  salvar(): void {
    this.salvando.set(true);
    this.financeiroService
      .editarConfiguracao({
        pixChave: this.pixChave.trim() || null,
        pixNomeRecebedor: this.pixNomeRecebedor.trim() || null,
        pixCidade: this.pixCidade.trim() || null
      })
      .subscribe({
        next: () => {
          this.salvando.set(false);
          this.notificacao.sucesso('Configuração Pix salva com sucesso.');
        },
        error: (resposta) => {
          this.salvando.set(false);
          this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível salvar a configuração.');
        }
      });
  }
}
