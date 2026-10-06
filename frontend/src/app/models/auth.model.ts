export type Papel = 'Admin' | 'Coordenador' | 'Educador' | 'Financeiro' | 'Responsavel' | 'Suporte';

export interface LoginResposta {
  token: string;
  usuarioId: string;
  nome: string;
  papel: Papel;
  responsavelId: string | null;
  /** Senha certa, mas falta o código do app autenticador: nenhum token foi emitido (a tela pede o código). */
  requerSegundoFator?: boolean;
}

export interface IdentidadeAtual {
  usuarioId: string;
  nome: string;
  papel: Papel;
  responsavelId: string | null;
}
