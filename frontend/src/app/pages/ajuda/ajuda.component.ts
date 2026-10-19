import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { ASSUNTOS_FALE_CONOSCO, AjudaService, AssuntoFaleConosco, MENSAGEM_MAXIMA, MENSAGEM_MINIMA } from '../../services/ajuda.service';
import { NotificacaoService } from '../../services/notificacao.service';

/** Ajuda da equipe do clube: o Fale conosco manda a mensagem por e-mail pro suporte do produto, já com o clube, a conta, o perfil e a
 * tela de onde a pessoa veio — quem responde não precisa perguntar nada disso. */
@Component({
  selector: 'app-ajuda',
  imports: [FormsModule, RouterLink],
  templateUrl: './ajuda.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './ajuda.component.scss'
})
export class AjudaComponent {
  private readonly ajuda = inject(AjudaService);
  private readonly notificacao = inject(NotificacaoService);

  protected readonly assuntos = ASSUNTOS_FALE_CONOSCO;
  protected readonly maximo = MENSAGEM_MAXIMA;

  /** A tela em que a pessoa estava antes de abrir a Ajuda (contexto pro suporte). */
  private readonly paginaDeOrigem: string | null =
    inject(Router).lastSuccessfulNavigation()?.previousNavigation?.finalUrl?.toString() ?? null;

  assunto: AssuntoFaleConosco | '' = '';
  mensagem = '';
  readonly enviando = signal(false);
  readonly enviada = signal(false);

  enviar(): void {
    if (!this.assunto) return this.notificacao.erro('Escolha o assunto da mensagem.');
    const mensagem = this.mensagem.trim();
    if (mensagem.length < MENSAGEM_MINIMA) return this.notificacao.erro('Escreva a mensagem com um pouco mais de detalhe.');

    this.enviando.set(true);
    this.ajuda.enviarFaleConosco(this.assunto, mensagem, this.paginaDeOrigem).subscribe({
      next: () => {
        this.enviando.set(false);
        this.enviada.set(true);
        this.notificacao.sucesso('Mensagem enviada. Vamos responder no seu e-mail.');
      },
      error: (erro) => {
        this.enviando.set(false);
        this.notificacao.erro(typeof erro.error === 'string' ? erro.error : 'Não foi possível enviar a mensagem. Tente de novo.');
      }
    });
  }

  novaMensagem(): void {
    this.assunto = '';
    this.mensagem = '';
    this.enviada.set(false);
  }
}
