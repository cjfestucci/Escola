export interface Cobranca {
  id: string;
  alunoId: string;
  alunoNome: string;
  turmaNome: string;
  descricao: string;
  valor: number;
  vencimento: string;
  paga: boolean;
  pagoEm: string | null;
}

export interface CriarCobranca {
  alunoId: string;
  descricao: string;
  valor: number;
  vencimento: string;
}

export interface EditarCobranca {
  descricao: string;
  valor: number;
  vencimento: string;
}

export interface ConfiguracaoFinanceira {
  pixChave: string | null;
  pixNomeRecebedor: string | null;
  pixCidade: string | null;
  configurado: boolean;
}

export interface EditarConfiguracaoFinanceira {
  pixChave: string | null;
  pixNomeRecebedor: string | null;
  pixCidade: string | null;
}

export interface PixCobranca {
  codigoCopiaECola: string;
}
