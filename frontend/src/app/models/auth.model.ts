export type Papel = 'Admin' | 'Coordenador' | 'Educador' | 'Financeiro' | 'Responsavel' | 'Suporte';

export interface LoginResposta {
  token: string;
  usuarioId: string;
  nome: string;
  papel: Papel;
  responsavelId: string | null;
  /** Senha certa, mas falta o código do app autenticador: nenhum token foi emitido (a tela pede o código). */
  requerSegundoFator?: boolean;
  /** A senha confere em mais de uma escola: a tela pede pra escolher e chama de novo com o id escolhido. Sem token ainda. */
  escolherCliente?: OpcaoClienteLogin[] | null;
  clienteId?: string | null;
}

/** Escola em que a pessoa tem conta (todas as escolas usam a mesma tela de login). */
export interface OpcaoClienteLogin {
  id: string;
  nome: string;
}

export interface IdentidadeAtual {
  usuarioId: string;
  nome: string;
  papel: Papel;
  responsavelId: string | null;
  /** Escola da sessão (claim clienteId do token): decide de quem são os dados, o tema e o segmento. */
  clienteId: string | null;
}
