import { Component, inject } from '@angular/core';

import { NotificacaoService } from '../../services/notificacao.service';

@Component({
  selector: 'app-notificacoes',
  imports: [],
  templateUrl: './notificacoes.component.html',
  styleUrl: './notificacoes.component.scss'
})
export class NotificacoesComponent {
  protected readonly servico = inject(NotificacaoService);

  fechar(id: number): void {
    this.servico.fechar(id);
  }
}
