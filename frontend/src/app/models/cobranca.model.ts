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
  cancelada: boolean;
  canceladaEm: string | null;
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

export type TipoChavePix = 'Cpf' | 'Cnpj' | 'Telefone' | 'Email' | 'Aleatoria';

export interface ConfiguracaoFinanceira {
  id: string | null;
  pixChave: string | null;
  pixTipoChave: TipoChavePix | null;
  pixNomeRecebedor: string | null;
  pixCidade: string | null;
  diasParaBloqueio: number | null;
  configurado: boolean;
}

export interface EditarConfiguracaoFinanceira {
  pixChave: string | null;
  pixTipoChave: TipoChavePix | null;
  pixNomeRecebedor: string | null;
  pixCidade: string | null;
  diasParaBloqueio: number | null;
}

export interface PixCobranca {
  codigoCopiaECola: string;
}
