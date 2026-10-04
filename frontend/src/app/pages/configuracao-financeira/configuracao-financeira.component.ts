import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { TipoChavePix } from '../../models/cobranca.model';
import { FinanceiroService } from '../../services/financeiro.service';
import { NotificacaoService } from '../../services/notificacao.service';
import { LogsModalComponent } from '../../shared/logs-modal/logs-modal.component';

interface OpcaoTipoChave {
  valor: TipoChavePix;
  rotulo: string;
  placeholder: string;
  dica: string;
}

const TIPOS_CHAVE: OpcaoTipoChave[] = [
  {
    valor: 'Cpf',
    rotulo: 'CPF',
    placeholder: 'Ex.: 123.456.789-09',
    dica: 'Os 11 números do CPF, com ou sem pontos e traço.'
  },
  {
    valor: 'Cnpj',
    rotulo: 'CNPJ',
    placeholder: 'Ex.: 12.345.678/0001-95',
    dica: 'Os 14 números do CNPJ, com ou sem pontos, barra e traço.'
  },
  {
    valor: 'Telefone',
    rotulo: 'Telefone',
    placeholder: 'Ex.: +5511999998888',
    dica: 'Formato: +55 (código do Brasil) + DDD + número, tudo junto — ex.: +5511999998888. Se você digitar só o DDD e o número, o +55 é acrescentado automaticamente.'
  },
  {
    valor: 'Email',
    rotulo: 'E-mail',
    placeholder: 'Ex.: financeiro@suaescola.com.br',
    dica: 'O e-mail exatamente como está cadastrado como chave Pix no seu banco.'
  },
  {
    valor: 'Aleatoria',
    rotulo: 'Chave aleatória',
    placeholder: 'Ex.: 123e4567-e89b-12d3-a456-426614174000',
    dica: 'Copie a chave aleatória gerada no app do seu banco (32 letras e números, separados por traços).'
  }
];

const REGEX_UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

/** Chaves cadastradas antes do tipo existir não têm o tipo guardado — o formato quase sempre entrega. */
function inferirTipo(chave: string): TipoChavePix | '' {
  if (chave.includes('@')) return 'Email';
  if (REGEX_UUID.test(chave)) return 'Aleatoria';
  if (chave.startsWith('+')) return 'Telefone';
  if (/^\d{14}$/.test(chave)) return 'Cnpj';
  if (/^\d{11}$/.test(chave)) return 'Cpf';
  return '';
}

@Component({
  selector: 'app-configuracao-financeira',
  imports: [FormsModule, LogsModalComponent],
  templateUrl: './configuracao-financeira.component.html',
  styleUrl: './configuracao-financeira.component.scss'
})
export class ConfiguracaoFinanceiraComponent implements OnInit {
  private readonly financeiroService = inject(FinanceiroService);
  private readonly notificacao = inject(NotificacaoService);

  readonly carregando = signal(true);
  readonly salvando = signal(false);
  readonly configuracaoId = signal<string | null>(null);
  readonly historicoAberto = signal(false);
  // Seções recolhíveis: a primeira abre expandida, as demais minimizadas.
  readonly pixAberto = signal(true);
  readonly bloqueioAberto = signal(false);
  protected readonly tiposChave = TIPOS_CHAVE;

  tipoChave: TipoChavePix | '' = '';
  pixChave = '';
  pixNomeRecebedor = '';
  pixCidade = '';
  diasParaBloqueio: number | null = null;

  get tipoSelecionado(): OpcaoTipoChave | undefined {
    return TIPOS_CHAVE.find((t) => t.valor === this.tipoChave);
  }

  ngOnInit(): void {
    this.financeiroService.obterConfiguracao().subscribe({
      next: (config) => {
        this.pixChave = config.pixChave ?? '';
        this.tipoChave = config.pixTipoChave ?? inferirTipo(this.pixChave);
        this.pixNomeRecebedor = config.pixNomeRecebedor ?? '';
        this.pixCidade = config.pixCidade ?? '';
        this.diasParaBloqueio = config.diasParaBloqueio;
        this.configuracaoId.set(config.id);
        this.carregando.set(false);
      },
      error: () => {
        this.carregando.set(false);
        this.notificacao.erro('Não foi possível carregar a configuração Pix.');
      }
    });
  }

  salvar(): void {
    const chave = this.pixChave.trim();
    if (chave && !this.tipoChave) {
      this.pixAberto.set(true);
      this.notificacao.erro('Selecione o tipo da chave Pix.');
      return;
    }

    // O campo numérico vem vazio como null (ou string vazia, dependendo do navegador): vazio = sem bloqueio.
    const dias = this.diasParaBloqueio === null || (this.diasParaBloqueio as unknown) === '' ? null : Number(this.diasParaBloqueio);
    if (dias !== null && (!Number.isInteger(dias) || dias < 1 || dias > 365)) {
      this.bloqueioAberto.set(true);
      this.notificacao.erro('Informe a quantidade de dias para bloqueio como um número inteiro entre 1 e 365, ou deixe em branco.');
      return;
    }

    this.salvando.set(true);
    this.financeiroService
      .editarConfiguracao({
        pixChave: chave || null,
        pixTipoChave: chave && this.tipoChave ? this.tipoChave : null,
        pixNomeRecebedor: this.pixNomeRecebedor.trim() || null,
        pixCidade: this.pixCidade.trim() || null,
        diasParaBloqueio: dias
      })
      .subscribe({
        next: (config) => {
          this.salvando.set(false);
          // O backend devolve a chave já normalizada (ex.: telefone com +55) — mostra exatamente o que foi guardado.
          this.pixChave = config.pixChave ?? '';
          this.configuracaoId.set(config.id);
          this.notificacao.sucesso('Configuração Pix salva com sucesso.');
        },
        error: (resposta) => {
          this.salvando.set(false);
          // O erro do backend pode ser de qualquer seção (ex.: chave Pix inválida) — abre a que provavelmente o causou.
          if (typeof resposta.error === 'string' && resposta.error.toLowerCase().includes('dias')) this.bloqueioAberto.set(true);
          else this.pixAberto.set(true);
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
