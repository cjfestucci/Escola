import { Injectable, NgZone, inject, signal } from '@angular/core';

import { environment } from '../../environments/environment';

/** Intervalo entre as tentativas de reconexão enquanto o servidor está fora. */
const INTERVALO_TENTATIVA_MS = 15_000;

/**
 * Sabe quando o app não consegue falar com o servidor (servidor/rede fora — ex.: manutenção do provedor), pra mostrar a faixa
 * "sem conexão" em vez de telas quebradas. Quem marca é o interceptor: falha de rede (status 0) ou o proxy sem o app atrás (502/504).
 * O 503 fica de fora de propósito: a própria API o usa em respostas normais (ex.: "envio de e-mail indisponível").
 * Enquanto estiver fora, testa o /healthz a cada 15 s e some sozinha quando volta.
 */
@Injectable({ providedIn: 'root' })
export class ConexaoService {
  private readonly zona = inject(NgZone);
  private readonly urlSaude = `${environment.apiUrl.replace(/\/api\/?$/, '')}/healthz`;
  private temporizador: ReturnType<typeof setInterval> | null = null;

  readonly servidorFora = signal(false);
  /** Acabou de voltar depois de uma queda: a tela pode ter ficado sem dados (oferece recarregar; não recarrega sozinho pra não
   * perder o que a pessoa estava digitando). */
  readonly voltou = signal(false);
  /** O próprio aparelho está sem internet (aí a mensagem é outra). */
  readonly semInternet = signal(typeof navigator !== 'undefined' && navigator.onLine === false);

  constructor() {
    if (typeof window === 'undefined') return;
    window.addEventListener('online', () => this.zona.run(() => {
      this.semInternet.set(false);
      void this.tentarAgora();
    }));
    window.addEventListener('offline', () => this.zona.run(() => this.semInternet.set(true)));
  }

  registrarFalha(): void {
    if (this.servidorFora()) return;
    this.servidorFora.set(true);
    // Fora do Angular: o setInterval não deve segurar a estabilidade da aplicação (service worker, testes).
    this.zona.runOutsideAngular(() => {
      this.temporizador = setInterval(() => void this.tentarAgora(), INTERVALO_TENTATIVA_MS);
    });
  }

  registrarSucesso(): void {
    if (!this.servidorFora()) return;
    this.servidorFora.set(false);
    this.voltou.set(true);
    if (this.temporizador) clearInterval(this.temporizador);
    this.temporizador = null;
  }

  /** Testa o servidor agora (botão "Tentar agora" e as tentativas automáticas). */
  async tentarAgora(): Promise<boolean> {
    try {
      const resposta = await fetch(this.urlSaude, { cache: 'no-store' });
      const ok = resposta.ok;
      if (ok) this.zona.run(() => this.registrarSucesso());
      return ok;
    } catch {
      return false;
    }
  }
}
