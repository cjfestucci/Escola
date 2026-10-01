import { Injectable, signal } from '@angular/core';

export type TipoNotificacao = 'sucesso' | 'erro' | 'info';

export interface Notificacao {
  id: number;
  tipo: TipoNotificacao;
  mensagem: string;
}

/** Serviço central de notificações (balõezinhos) — substitui mensagens de erro/sucesso inline espalhadas pelas telas. */
@Injectable({ providedIn: 'root' })
export class NotificacaoService {
  private readonly _notificacoes = signal<Notificacao[]>([]);
  readonly notificacoes = this._notificacoes.asReadonly();

  private proximoId = 1;

  sucesso(mensagem: string): void {
    this.mostrar('sucesso', mensagem, 4000);
  }

  erro(mensagem: string): void {
    this.mostrar('erro', mensagem, 6000);
  }

  info(mensagem: string): void {
    this.mostrar('info', mensagem, 4000);
  }

  fechar(id: number): void {
    this._notificacoes.update((atual) => atual.filter((n) => n.id !== id));
  }

  private mostrar(tipo: TipoNotificacao, mensagem: string, duracaoMs: number): void {
    const id = this.proximoId++;
    this._notificacoes.update((atual) => [...atual, { id, tipo, mensagem }]);
    setTimeout(() => this.fechar(id), duracaoMs);
  }
}
