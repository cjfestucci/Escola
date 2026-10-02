export interface Produto {
  id: string;
  nome: string;
  codigo: string | null;
  unidadeMedida: string;
  estoqueMinimo: number;
  ativo: boolean;
  saldoAtual: number;
  estoqueBaixo: boolean;
}

export interface CriarOuEditarProduto {
  nome: string;
  codigo: string | null;
  unidadeMedida: string;
  estoqueMinimo: number;
}
