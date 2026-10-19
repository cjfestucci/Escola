import { Component, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';

import {
  ASSUNTOS_CLUBE,
  AjudaService,
  AssuntoClube,
  ContatoClube,
  ContatoUnidade,
  MENSAGEM_MAXIMA,
  MENSAGEM_MINIMA,
  mensagemInicialWhatsApp
} from '../../services/ajuda.service';
import { AuthService } from '../../services/auth.service';
import { NotificacaoService } from '../../services/notificacao.service';
import { SegmentoService } from '../../services/segmento.service';
import { linkWhatsApp } from '../../shared/mascara/mascara';

/** "Fale com o clube" do portal: os pais falam com o clube (mensalidade, treino, jogo), não com o produto. Mostra o contato das unidades
 * que o próprio clube cadastrou e, nas que têm e-mail, um formulário que manda a mensagem pro clube já dizendo de quem é e de qual
 * atleta (a resposta volta no e-mail do pai). */
@Component({
  selector: 'app-portal-contato-clube',
  imports: [FormsModule],
  templateUrl: './portal-contato-clube.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './portal-contato-clube.component.scss'
})
export class PortalContatoClubeComponent implements OnInit {
  private readonly ajuda = inject(AjudaService);
  private readonly notificacao = inject(NotificacaoService);
  private readonly auth = inject(AuthService);
  protected readonly segmentoService = inject(SegmentoService);
  protected readonly linkWhatsApp = linkWhatsApp;
  protected readonly assuntos = ASSUNTOS_CLUBE;
  protected readonly maximo = MENSAGEM_MAXIMA;

  readonly contato = signal<ContatoClube | null>(null);
  readonly carregando = signal(true);

  /** Unidade com o formulário de mensagem aberto (um por vez). */
  readonly formularioAbertoId = signal<string | null>(null);
  readonly enviadaParaId = signal<string | null>(null);
  readonly enviando = signal(false);
  assunto: AssuntoClube | '' = '';
  mensagem = '';

  ngOnInit(): void {
    this.ajuda.contatoClube().subscribe({
      next: (contato) => {
        this.contato.set(contato);
        this.carregando.set(false);
      },
      error: () => {
        this.carregando.set(false);
        this.notificacao.erro('Não foi possível carregar o contato.');
      }
    });
  }

  /** Só dígitos, pro link de ligar (tel:). */
  protected telefoneParaLink(telefone: string): string {
    return telefone.replace(/[^\d+]/g, '');
  }

  /** Link do WhatsApp da unidade com a mensagem inicial (quem fala e de qual atleta). */
  protected linkWhatsAppDe(unidade: ContatoUnidade): string | null {
    return linkWhatsApp(unidade.whatsApp, mensagemInicialWhatsApp(this.auth.identidade()?.nome, this.contato()?.atletas ?? []));
  }

  protected temContato(contato: ContatoClube): boolean {
    return contato.unidades.some((u) => u.telefone || u.endereco || u.email || this.linkWhatsApp(u.whatsApp));
  }

  abrirFormulario(unidade: ContatoUnidade): void {
    this.assunto = '';
    this.mensagem = '';
    this.enviadaParaId.set(null);
    this.formularioAbertoId.set(this.formularioAbertoId() === unidade.id ? null : unidade.id);
  }

  enviar(unidade: ContatoUnidade): void {
    if (!this.assunto) return this.notificacao.erro('Escolha o assunto da mensagem.');
    const mensagem = this.mensagem.trim();
    if (mensagem.length < MENSAGEM_MINIMA) return this.notificacao.erro('Escreva a mensagem com um pouco mais de detalhe.');

    this.enviando.set(true);
    this.ajuda.enviarMensagemClube(unidade.id, this.assunto, mensagem).subscribe({
      next: () => {
        this.enviando.set(false);
        this.formularioAbertoId.set(null);
        this.enviadaParaId.set(unidade.id);
        this.notificacao.sucesso('Mensagem enviada. A resposta chega no seu e-mail.');
      },
      error: (erro) => {
        this.enviando.set(false);
        this.notificacao.erro(typeof erro.error === 'string' ? erro.error : 'Não foi possível enviar a mensagem. Tente de novo.');
      }
    });
  }
}
