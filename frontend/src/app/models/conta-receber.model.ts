export interface ContaReceber {
  id: string;
  descricao: string;
  origem: string | null;
  valor: number;
  vencimento: string;
  recebida: boolean;
  recebidoEm: string | null;
  cancelada: boolean;
  canceladaEm: string | null;
}

export interface CriarOuEditarContaReceber {
  descricao: string;
  origem: string | null;
  valor: number;
  vencimento: string;
}
