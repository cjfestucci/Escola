export type TipoMovimentacaoEstoque = 'Entrada' | 'Saida';

export interface MovimentacaoEstoque {
  id: string;
  produtoId: string;
  produtoNome: string;
  unidadeMedida: string;
  tipo: TipoMovimentacaoEstoque;
  quantidade: number;
  data: string;
  fornecedorId: string | null;
  fornecedorNome: string | null;
  observacao: string | null;
  registradoPorNome: string;
  registradoEm: string;
}

export interface CriarMovimentacaoEstoque {
  produtoId: string;
  tipo: TipoMovimentacaoEstoque;
  quantidade: number;
  data: string;
  fornecedorId: string | null;
  observacao: string | null;
}
