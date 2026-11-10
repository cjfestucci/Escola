import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/** Identificação do prestador dos serviços financeiros, exigida pelo Asaas no modelo BaaS (Playbook de BaaS, Resolução Conjunta
 * nº 16/2025 do Banco Central): o selo do Asaas e a menção textual aparecem em toda tela em que a plataforma abre conta de pagamento
 * ou movimenta valores. Os endereços dos selos (com o nosso `id`) vieram do próprio Asaas e são públicos.
 *
 * Não usar `referrerpolicy="no-referrer"` na imagem: o Asaas confere pelo referrer que o selo foi carregado. */
const BASE_SELOS = 'https://baas.asaas.com/selos';
const ID_SELO = '99abe421-e0b4-4a10-a6cb-5254a527daba';

export type VarianteSeloAsaas = 'positivo' | 'preto' | 'branco';

const ARQUIVOS: Record<VarianteSeloAsaas, string> = {
  positivo: 'Servicos_financeiros_Asaas-Reduzida-Positivo.svg',
  preto: 'Servicos_financeiros_Asaas-Reduzida-Negativo-Preto.svg',
  branco: 'Servicos_financeiros_Asaas-Reduzida-Negativo-Branco.svg'
};

@Component({
  selector: 'app-aviso-asaas',
  templateUrl: './aviso-asaas.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './aviso-asaas.component.scss'
})
export class AvisoAsaasComponent {
  /** `positivo` (colorido) em fundo claro; `branco` em fundo escuro. */
  readonly variante = input<VarianteSeloAsaas>('positivo');

  /** Versão curta, pra telas de pagamento (Pix): selo + uma linha. */
  readonly compacto = input(false);

  /** Mostra a imagem do selo (o endereço é do BaaS do Asaas). Telas da conta própria do clube não usam: só o texto. */
  readonly selo = input(true);

  /** Versão longa para a conta Asaas que o próprio clube contrata (sem o texto de "abertura da conta pela plataforma"). */
  readonly contaPropria = input(false);

  /** Como o pagamento aparece na frase da versão curta ("Pagamento {{ meio }} processado…"). */
  readonly meio = input('por Pix');

  protected readonly urlSelo = computed(() => `${BASE_SELOS}/${ARQUIVOS[this.variante()]}?id=${ID_SELO}`);
}
