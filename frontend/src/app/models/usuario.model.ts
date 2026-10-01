export interface Usuario {
  id: string;
  nome: string;
  papel: string;
}

export type PapelEquipe = 'Admin' | 'Coordenador' | 'Educador' | 'Financeiro';

export interface UsuarioConta {
  id: string;
  nome: string;
  email: string;
  papel: PapelEquipe;
  ativo: boolean;
}

export interface CriarOuEditarUsuario {
  nome: string;
  email: string;
  papel: PapelEquipe;
}

export interface SenhaGerada {
  nome: string;
  email: string;
  senha: string;
}
