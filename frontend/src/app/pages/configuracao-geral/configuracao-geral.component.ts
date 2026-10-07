import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { ConfiguracaoEscolaService } from '../../services/configuracao-escola.service';
import { NotificacaoService } from '../../services/notificacao.service';
import { LogsModalComponent } from '../../shared/logs-modal/logs-modal.component';
import { COR_PADRAO, CORES_SUGERIDAS, aplicarTema, corValida, normalizarCor, problemaDaCor } from '../../shared/tema';

@Component({
  selector: 'app-configuracao-geral',
  imports: [FormsModule, LogsModalComponent],
  templateUrl: './configuracao-geral.component.html',
  styleUrl: './configuracao-geral.component.scss'
})
export class ConfiguracaoGeralComponent implements OnInit, OnDestroy {
  private readonly configuracaoService = inject(ConfiguracaoEscolaService);
  private readonly notificacao = inject(NotificacaoService);

  readonly carregando = signal(true);
  readonly salvando = signal(false);
  readonly configuracaoId = signal<string | null>(null);
  readonly historicoAberto = signal(false);

  protected readonly coresSugeridas = CORES_SUGERIDAS;
  protected readonly corPadrao = COR_PADRAO;

  /** Cor escolhida na tela (null = padrão do produto). Aplicada ao vivo como prévia; só vale depois de salvar. */
  readonly corEscolhida = signal<string | null>(null);
  readonly corTexto = signal('');
  private corSalva: string | null = null;

  readonly problemaCor = computed(() => {
    const texto = this.corTexto().trim();
    return texto ? problemaDaCor(normalizarCor(texto)) : null;
  });

  ngOnInit(): void {
    this.configuracaoService.obter().subscribe({
      next: (config) => {
        this.configuracaoId.set(config.id);
        this.corSalva = config.corPrincipal;
        this.corEscolhida.set(config.corPrincipal);
        this.corTexto.set(config.corPrincipal ?? '');
        this.carregando.set(false);
      },
      error: () => {
        this.carregando.set(false);
        this.notificacao.erro('Não foi possível carregar a configuração.');
      }
    });
  }

  ngOnDestroy(): void {
    // Saiu sem salvar: desfaz a prévia e volta pra cor que está valendo de fato.
    aplicarTema(this.corSalva);
  }

  escolherSugerida(cor: string): void {
    this.definirCor(cor === COR_PADRAO ? null : cor);
    this.corTexto.set(cor === COR_PADRAO ? '' : cor);
  }

  restaurarPadrao(): void {
    this.definirCor(null);
    this.corTexto.set('');
  }

  digitarCor(texto: string): void {
    this.corTexto.set(texto);
    const cor = normalizarCor(texto);
    // Só vira prévia uma cor completa e legível; enquanto digita, a tela segue com a última cor válida.
    if (corValida(cor) && !problemaDaCor(cor)) this.definirCor(cor === COR_PADRAO ? null : cor);
  }

  private definirCor(cor: string | null): void {
    this.corEscolhida.set(cor);
    aplicarTema(cor);
  }

  salvar(): void {
    const problema = this.problemaCor();
    if (problema) {
      this.notificacao.erro(problema);
      return;
    }

    this.salvando.set(true);
    this.configuracaoService.editar(this.corEscolhida()).subscribe({
      next: (config) => {
        this.configuracaoId.set(config.id);
        this.corSalva = config.corPrincipal;
        this.corEscolhida.set(config.corPrincipal);
        this.corTexto.set(config.corPrincipal ?? '');
        this.salvando.set(false);
        this.notificacao.sucesso('Configuração salva com sucesso.');
      },
      error: (resposta) => {
        this.salvando.set(false);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível salvar a configuração.');
      }
    });
  }

  abrirHistorico(): void {
    this.historicoAberto.set(true);
  }

  fecharHistorico(): void {
    this.historicoAberto.set(false);
  }
}
