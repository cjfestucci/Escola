import { Component, computed, effect, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NgTemplateOutlet } from '@angular/common';
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { filter, map } from 'rxjs';

import { AjudaService, ContatoUnidade, mensagemInicialWhatsApp } from '../../services/ajuda.service';
import { AuthService } from '../../services/auth.service';
import { ContextoService } from '../../services/contexto.service';
import { linkWhatsApp } from '../mascara/mascara';

/** Atalho do WhatsApp no canto inferior direito, só no portal dos pais e só se o clube cadastrou um WhatsApp numa unidade ativa.
 * Uma unidade com WhatsApp → abre a conversa direto (nova aba); várias → leva pro "Fale com o clube", onde o pai escolhe a unidade.
 * Fica fora da tela do termo (o pai ainda não entrou de fato no portal). */
@Component({
  selector: 'app-whatsapp-flutuante',
  imports: [RouterLink, NgTemplateOutlet],
  templateUrl: './whatsapp-flutuante.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './whatsapp-flutuante.component.scss'
})
export class WhatsappFlutuanteComponent {
  private readonly ajuda = inject(AjudaService);
  private readonly auth = inject(AuthService);
  private readonly contexto = inject(ContextoService);
  private readonly router = inject(Router);

  private readonly unidadesComWhatsApp = signal<ContatoUnidade[]>([]);
  private readonly atletas = signal<string[]>([]);
  /** Cliente cujo contato já foi buscado (entrar em outra conta busca de novo). */
  private carregadoPara: string | null = null;

  private readonly url = toSignal(
    this.router.events.pipe(
      filter((e) => e instanceof NavigationEnd),
      map(() => this.router.url)
    ),
    { initialValue: this.router.url }
  );

  /** Link direto do WhatsApp (uma unidade só) ou nulo (várias: vai pro "Fale com o clube"). */
  protected readonly linkDireto = computed(() => {
    const unidades = this.unidadesComWhatsApp();
    return unidades.length === 1
      ? linkWhatsApp(unidades[0].whatsApp, mensagemInicialWhatsApp(this.auth.identidade()?.nome, this.atletas()))
      : null;
  });

  protected readonly visivel = computed(
    () => this.contexto.contexto() === 'portal' && this.unidadesComWhatsApp().length > 0 && !this.url().startsWith('/portal/termo')
  );

  constructor() {
    effect(() => {
      const clienteId = this.contexto.contexto() === 'portal' ? this.auth.identidade()?.clienteId ?? null : null;
      if (!clienteId) {
        this.carregadoPara = null;
        this.unidadesComWhatsApp.set([]);
        return;
      }
      if (this.carregadoPara === clienteId) return;
      this.carregadoPara = clienteId;
      this.ajuda.contatoClube().subscribe({
        next: (contato) => {
          this.atletas.set(contato.atletas);
          this.unidadesComWhatsApp.set(contato.unidades.filter((u) => linkWhatsApp(u.whatsApp)));
        },
        // Atalho é conveniência: se falhar, simplesmente não aparece (o "Fale com o clube" continua no menu).
        error: () => this.unidadesComWhatsApp.set([])
      });
    });
  }
}
