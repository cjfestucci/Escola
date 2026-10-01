export interface ContaPagar {
  id: string;
  fornecedorId: string;
  fornecedorNome: string;
  descricao: string;
  valor: number;
  vencimento: string;
  paga: boolean;
  pagoEm: string | null;
  cancelada: boolean;
  canceladaEm: string | null;
}

export interface CriarOuEditarContaPagar {
  fornecedorId: string;
  descricao: string;
  valor: number;
  vencimento: string;
}
