import { ChangeDetectionStrategy, Component, ElementRef, OnDestroy, OnInit, inject, input, output } from '@angular/core';

/** O pedaço da API do Turnstile (Cloudflare) que usamos. */
interface Turnstile {
  render(elemento: HTMLElement, opcoes: Record<string, unknown>): string;
  reset(widgetId: string): void;
  remove(widgetId: string): void;
}

declare global {
  interface Window {
    turnstile?: Turnstile;
  }
}

const SCRIPT = 'https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit';
let carregando: Promise<Turnstile> | null = null;

/** Carrega o script do Turnstile uma vez só, e só na tela que usa o captcha (liberado na CSP do app, no Caddyfile). */
function carregarTurnstile(): Promise<Turnstile> {
  if (window.turnstile) return Promise.resolve(window.turnstile);
  carregando ??= new Promise((resolver, rejeitar) => {
    const script = document.createElement('script');
    script.src = SCRIPT;
    script.async = true;
    script.onload = () => (window.turnstile ? resolver(window.turnstile) : rejeitar(new Error('Turnstile indisponível')));
    script.onerror = () => {
      carregando = null;
      rejeitar(new Error('Não foi possível carregar o Turnstile'));
    };
    document.head.appendChild(script);
  });
  return carregando;
}

/**
 * Captcha do Cloudflare Turnstile (modo "Managed": na maioria das vezes passa sozinho, sem clique). Emite o token quando a verificação
 * passa e `null` quando vence ou falha. O token vale uma vez: depois de um envio recusado, chame `reiniciar()`.
 */
@Component({
  selector: 'app-captcha',
  template: '',
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: ':host { display: block; min-height: 65px; }'
})
export class CaptchaComponent implements OnInit, OnDestroy {
  readonly chave = input.required<string>();
  readonly token = output<string | null>();

  private readonly elemento = inject<ElementRef<HTMLElement>>(ElementRef);
  private turnstile?: Turnstile;
  private widgetId?: string;
  private destruido = false;

  ngOnInit(): void {
    carregarTurnstile()
      .then((turnstile) => {
        if (this.destruido) return;
        this.turnstile = turnstile;
        this.widgetId = turnstile.render(this.elemento.nativeElement, {
          sitekey: this.chave(),
          language: 'pt-br',
          callback: (token: string) => this.token.emit(token),
          'expired-callback': () => this.token.emit(null),
          'error-callback': () => this.token.emit(null)
        });
      })
      // Sem o script (bloqueado, sem internet) o envio vai sem token e o servidor responde com a mensagem do captcha.
      .catch(() => this.token.emit(null));
  }

  reiniciar(): void {
    this.token.emit(null);
    if (this.turnstile && this.widgetId) this.turnstile.reset(this.widgetId);
  }

  ngOnDestroy(): void {
    this.destruido = true;
    if (this.turnstile && this.widgetId) this.turnstile.remove(this.widgetId);
  }
}
