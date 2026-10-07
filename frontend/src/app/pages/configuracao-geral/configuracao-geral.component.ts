import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { FUSOS_HORARIOS } from '../../models/configuracao-escola.model';
import { AuthService } from '../../services/auth.service';
import { ConfiguracaoEscolaService } from '../../services/configuracao-escola.service';
import { SegmentoService } from '../../services/segmento.service';
import { NotificacaoService } from '../../services/notificacao.service';
import { LogsModalComponent } from '../../shared/logs-modal/logs-modal.component';
import { SeletorArquivoComponent } from '../../shared/seletor-arquivo/seletor-arquivo.component';
import { COR_PADRAO, CORES_SUGERIDAS, aplicarTema, corValida, normalizarCor, problemaDaCor } from '../../shared/tema';

@Component({
  selector: 'app-configuracao-geral',
  imports: [FormsModule, LogsModalComponent, SeletorArquivoComponent],
  templateUrl: './configuracao-geral.component.html',
  styleUrl: './configuracao-geral.component.scss'
})
export class ConfiguracaoGeralComponent implements OnInit, OnDestroy {
  private readonly configuracaoService = inject(ConfiguracaoEscolaService);
  private readonly notificacao = inject(NotificacaoService);
  protected readonly auth = inject(AuthService);
  protected readonly segmentoService = inject(SegmentoService);

  // Dados da escola (nome, fuso, logo): só o Admin e o Suporte.
  nomeEscola = '';
  fusoHorario = '';
  private nomeSalvo = '';
  private fusoSalvo = '';
  fusos = FUSOS_HORARIOS;
  readonly salvandoDados = signal(false);
  readonly enviandoLogo = signal(false);
  /** Mesmo limite do backend. */
  private readonly tamanhoMaximoLogo = 2 * 1024 * 1024;

  get fusoMudou(): boolean {
    return !!this.fusoSalvo && this.fusoHorario !== this.fusoSalvo;
  }

  get dadosMudaram(): boolean {
    return this.nomeEscola.trim() !== this.nomeSalvo || this.fusoMudou;
  }

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
        this.aplicarDados(config.nomeEscola ?? '', config.fusoHorario);
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

  private aplicarDados(nome: string, fuso: string): void {
    this.nomeEscola = nome;
    this.nomeSalvo = nome;
    this.fusoHorario = fuso;
    this.fusoSalvo = fuso;
    // Fuso salvo por fora da lista (ex.: direto na API) continua aparecendo como opção
    if (fuso && !this.fusos.some((f) => f.valor === fuso)) this.fusos = [...this.fusos, { valor: fuso, rotulo: fuso }];
  }

  salvarDados(): void {
    if (!this.nomeEscola.trim()) {
      this.notificacao.erro('Informe o nome da escola.');
      return;
    }
    this.salvandoDados.set(true);
    this.configuracaoService.editarDados(this.nomeEscola.trim(), this.fusoHorario).subscribe({
      next: (config) => {
        this.configuracaoId.set(config.id);
        this.aplicarDados(config.nomeEscola ?? '', config.fusoHorario);
        this.salvandoDados.set(false);
        this.notificacao.sucesso('Dados da escola salvos.');
      },
      error: (resposta) => {
        this.salvandoDados.set(false);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível salvar os dados da escola.');
      }
    });
  }

  aoEscolherLogo(arquivo: File): void {
    if (arquivo.size > this.tamanhoMaximoLogo) {
      this.notificacao.erro('A logo pode ter no máximo 2MB.');
      return;
    }
    this.enviandoLogo.set(true);
    this.configuracaoService.enviarLogo(arquivo).subscribe({
      next: () => this.aposMudarLogo('Logo atualizada.'),
      error: (resposta) => {
        this.enviandoLogo.set(false);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível enviar a logo. Use PNG, JPEG ou WEBP de até 2MB.');
      }
    });
  }

  removerLogo(): void {
    this.enviandoLogo.set(true);
    this.configuracaoService.removerLogo().subscribe({
      next: () => this.aposMudarLogo('Logo removida.'),
      error: () => {
        this.enviandoLogo.set(false);
        this.notificacao.erro('Não foi possível remover a logo.');
      }
    });
  }

  /** Recarrega a configuração: é ela que alimenta o menu (e o cache) com a logo nova. */
  private aposMudarLogo(mensagem: string): void {
    this.configuracaoService.obter().subscribe({
      next: (config) => {
        this.configuracaoId.set(config.id);
        this.enviandoLogo.set(false);
        this.notificacao.sucesso(mensagem);
      },
      error: () => this.enviandoLogo.set(false)
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
