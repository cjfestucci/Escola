import { Component, computed, inject, output, signal } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';

import { AuthService } from '../../services/auth.service';
import { ContextoService } from '../../services/contexto.service';
import { SegmentoService } from '../../services/segmento.service';
import { SessaoService } from '../../services/sessao.service';

const TITULOS_CLUBE: Record<string, string> = {
  'Matrícula': 'Atletas',
  'Novo aluno': 'Novo atleta',
  'Editar aluno': 'Editar atleta',
  // Página do filho no portal: "Rotina do dia" é da escola infantil.
  'Rotina do dia': 'Portal da Família'
};

@Component({
  selector: 'app-topbar',
  imports: [],
  templateUrl: './topbar.component.html',
  styleUrl: './topbar.component.scss'
})
export class TopbarComponent {
  private readonly router = inject(Router);
  protected readonly auth = inject(AuthService);
  protected readonly contextoService = inject(ContextoService);
  protected readonly sessao = inject(SessaoService);
  private readonly segmentoService = inject(SegmentoService);

  readonly abrirMenu = output<void>();

  /** Título fixo da rota (data.titulo), lido a cada navegação. */
  private readonly tituloDaRota = signal<string | null>(this.lerTituloDaRota());

  /** Reage também ao segmento: ele muda sem navegação quando a escola da sessão muda (login/saída na mesma tela). */
  protected readonly titulo = computed(() => {
    const titulo = this.tituloDaRota() ?? this.segmentoService.nomeApp();
    return this.segmentoService.ehClube() ? (TITULOS_CLUBE[titulo] ?? titulo) : titulo;
  });

  constructor() {
    this.router.events.pipe(filter((evento) => evento instanceof NavigationEnd)).subscribe(() => {
      this.tituloDaRota.set(this.lerTituloDaRota());
    });
  }

  sair(): void {
    this.auth.sair();
    this.sessao.limpar();
    this.router.navigateByUrl('/entrar');
  }

  private lerTituloDaRota(): string | null {
    let rota = this.router.routerState.snapshot.root;
    while (rota.firstChild) rota = rota.firstChild;
    return (rota.data['titulo'] as string | undefined) ?? null;
  }
}
