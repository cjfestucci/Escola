const FORMATO_QUANTIDADE = new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 3 });

const FORMATO_TAMANHO = new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 1 });

/** Tamanho de arquivo legível em pt-BR (ex.: "1,2 MB"). */
export function formatarTamanho(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${FORMATO_TAMANHO.format(bytes / 1024)} KB`;
  return `${FORMATO_TAMANHO.format(bytes / (1024 * 1024))} MB`;
}

/** Quantidade de estoque em pt-BR (vírgula decimal, até 3 casas, sem zeros à direita). */
export function formatarQuantidade(valor: number): string {
  return FORMATO_QUANTIDADE.format(valor);
}
