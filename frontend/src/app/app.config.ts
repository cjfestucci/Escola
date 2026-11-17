import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { ApplicationConfig, LOCALE_ID, inject, isDevMode, provideAppInitializer, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors, withXhr } from '@angular/common/http';

import { routes } from './app.routes';
import { authInterceptor } from './services/auth.interceptor';
import { ConfiguracaoEscolaService } from './services/configuracao-escola.service';
import { provideServiceWorker } from '@angular/service-worker';

// Pipes do Angular (number, date, currency) no formato brasileiro: "1.234,50", não "1,234.50".
registerLocaleData(localePt, 'pt-BR');

export const appConfig: ApplicationConfig = {
  providers: [
    { provide: LOCALE_ID, useValue: 'pt-BR' },
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withXhr(), withInterceptors([authInterceptor])),
    provideAppInitializer(() => inject(ConfiguracaoEscolaService).inicializar()),
    // O "?v=" força os navegadores a reinstalar o service worker: ele guarda a CSP do momento da instalação e, como o arquivo não
    // muda entre versões, nunca seria reinstalado sozinho. Mudou o connect-src do app no Caddyfile → incremente o número.
    // (v=2 em 17/11/2026: liberou o Turnstile do /assinar.)
    provideServiceWorker('ngsw-worker.js?v=2', {
      enabled: !isDevMode(),
      registrationStrategy: 'registerWhenStable:30000'
    })
  ]
};
