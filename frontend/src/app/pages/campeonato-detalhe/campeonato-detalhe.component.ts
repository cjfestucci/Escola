import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';

import { Campeonato, Jogo, ROTULOS_RESULTADO, resultadoDoJogo } from '../../models/competicao.model';
import { CampeonatoService } from '../../services/campeonato.service';
import { JogoService } from '../../services/jogo.service';
import { NotificacaoService } from '../../services/notificacao.service';
import { formatarDataAbsoluta } from '../../shared/data-utils';

@Component({
  selector: 'app-campeonato-detalhe',
  imports: [RouterLink],
  templateUrl: './campeonato-detalhe.component.html',
  styleUrl: './campeonato-detalhe.component.scss'
})
export class CampeonatoDetalheComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly campeonatoService = inject(CampeonatoService);
  private readonly jogoService = inject(JogoService);
  private readonly notificacao = inject(NotificacaoService);

  readonly campeonato = signal<Campeonato | null>(null);
  readonly jogos = signal<Jogo[]>([]);
  readonly carregando = signal(true);

  protected readonly formatarDataAbsoluta = formatarDataAbsoluta;
  protected readonly resultadoDoJogo = resultadoDoJogo;
  protected readonly rotulosResultado = ROTULOS_RESULTADO;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.router.navigateByUrl('/campeonatos');
      return;
    }

    forkJoin({
      campeonato: this.campeonatoService.obterPorId(id),
      jogos: this.jogoService.listar({ campeonatoId: id })
    }).subscribe({
      next: ({ campeonato, jogos }) => {
        this.campeonato.set(campeonato);
        this.jogos.set(jogos);
        this.carregando.set(false);
      },
      error: () => {
        this.carregando.set(false);
        this.notificacao.erro('Não foi possível carregar o campeonato.');
      }
    });
  }

  saldo(golsPro: number, golsContra: number): string {
    const saldo = golsPro - golsContra;
    return saldo > 0 ? `+${saldo}` : String(saldo);
  }
}
