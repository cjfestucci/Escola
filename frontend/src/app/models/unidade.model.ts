export interface Unidade {
  id: string;
  nome: string;
  endereco: string | null;
  telefone: string | null;
  ativa: boolean;
}

export interface CriarOuEditarUnidade {
  nome: string;
  endereco: string | null;
  telefone: string | null;
  ativa: boolean;
}
