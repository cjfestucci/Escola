export type Papel = 'Admin' | 'Coordenador' | 'Educador' | 'Financeiro' | 'Responsavel';

export interface LoginResposta {
  token: string;
  usuarioId: string;
  nome: string;
  papel: Papel;
  responsavelId: string | null;
}

export interface IdentidadeAtual {
  usuarioId: string;
  nome: string;
  papel: Papel;
  responsavelId: string | null;
}
