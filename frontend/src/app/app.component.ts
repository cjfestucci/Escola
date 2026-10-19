import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRouteSnapshot, NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';

import { AuthService } from './services/auth.service';
import { AvisoAssinaturaComponent } from './shared/aviso-assinatura/aviso-assinatura.component';
import { NotificacoesComponent } from './shared/notificacoes/notificacoes.component';
import { SidebarComponent } from './shared/sidebar/sidebar.component';
import { TopbarComponent } from './shared/topbar/topbar.component';
import { WhatsappFlutuanteComponent } from './shared/whatsapp-flutuante/whatsapp-flutuante.component';
import { AvisoConexaoComponent } from './shared/aviso-conexao/aviso-conexao.component';
import { ContextoService } from './services/contexto.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, SidebarComponent, TopbarComponent, NotificacoesComponent, AvisoAssinaturaComponent, WhatsappFlutuanteComponent, AvisoConexaoComponent],
  templateUrl: './app.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './app.component.scss'
})
export class AppComponent {
  protected readonly auth = inject(AuthService);
  protected readonly contexto = inject(ContextoService);
  protected readonly menuAberto = signal(false);
  private readonly router = inject(Router);

  /** Telas públicas (entrar, assinar, esqueci/criar senha) nunca mostram o menu de quem está logado: um link de convite aberto
   * no navegador de outra pessoa (ex.: o Suporte abrindo o convite do Admin) não pode aparecer dentro da sessão dela. */
  protected readonly paginaPublica = signal(false);

  constructor() {
    this.router.events.pipe(filter((e) => e instanceof NavigationEnd)).subscribe(() => {
      let rota: ActivatedRouteSnapshot = this.router.routerState.snapshot.root;
      while (rota.firstChild) rota = rota.firstChild;
      this.paginaPublica.set(rota.data['publica'] === true);
    });
  }
}
