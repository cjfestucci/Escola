import { Component, ElementRef, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

/**
 * Guias de uso (Ajuda → Guias de uso): passo a passo por tarefa, só para quem está logado. O texto fica no template — mudou um botão,
 * menu ou fluxo citado ali, atualize o guia.
 */
@Component({
  selector: 'app-ajuda-guias',
  imports: [RouterLink],
  templateUrl: './ajuda-guias.component.html',
  styleUrl: './ajuda-guias.component.scss'
})
export class AjudaGuiasComponent {
  private readonly elemento = inject<ElementRef<HTMLElement>>(ElementRef);

  readonly semResultado = signal(false);

  constructor() {
    // /ajuda/guias#chamada abre já no guia.
    const fragmento = inject(ActivatedRoute).snapshot.fragment;
    if (fragmento) setTimeout(() => this.irPara(fragmento));
  }

  /** Mostra só os guias que têm todas as palavras digitadas (sem diferenciar acento nem maiúscula). */
  buscar(texto: string): void {
    const palavras = normalizar(texto).split(/\s+/).filter(Boolean);
    const raiz = this.elemento.nativeElement;
    let visiveis = 0;
    raiz.querySelectorAll<HTMLElement>('.guia').forEach((guia) => {
      const mostra = palavras.every((p) => normalizar(guia.textContent ?? '').includes(p));
      guia.hidden = !mostra;
      if (mostra) visiveis++;
    });
    const indice = raiz.querySelector<HTMLElement>('#indice-ajuda');
    if (indice) indice.hidden = palavras.length > 0;
    this.semResultado.set(visiveis === 0);
  }

  /** Os links do índice são âncoras (#chamada…): rola até o guia em vez de deixar o navegador trocar de endereço. */
  aoClicar(evento: MouseEvent): void {
    const link = (evento.target as HTMLElement).closest('a');
    const href = link?.getAttribute('href');
    if (!href?.startsWith('#')) return;
    evento.preventDefault();
    this.irPara(href.slice(1));
  }

  private irPara(id: string): void {
    this.elemento.nativeElement.querySelector(`#${CSS.escape(id)}`)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }
}

function normalizar(texto: string): string {
  return texto.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase();
}
