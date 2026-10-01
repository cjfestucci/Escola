export interface Fornecedor {
  id: string;
  nome: string;
  documento: string | null;
  telefone: string | null;
  email: string | null;
  ativo: boolean;
}

export interface CriarOuEditarFornecedor {
  nome: string;
  documento: string | null;
  telefone: string | null;
  email: string | null;
}
