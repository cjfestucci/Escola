export interface Usuario {
  id: string;
  nome: string;
  papel: string;
}

export type PapelEquipe = 'Admin' | 'Coordenador' | 'Educador' | 'Financeiro' | 'Secretaria';

export interface UsuarioConta {
  id: string;
  nome: string;
  email: string;
  papel: PapelEquipe;
  ativo: boolean;
  /** Ainda não cadastrou a senha pelo convite do e-mail. */
  convitePendente: boolean;
  /** Usa verificação em duas etapas (código do app autenticador). */
  doisFatores: boolean;
}

export interface CriarOuEditarUsuario {
  nome: string;
  email: string;
  papel: PapelEquipe;
}

/** Conta criada ou link de senha mandado. O link só vem quando o e-mail não saiu (sem SMTP): a Gestão repassa. */
export interface EnvioLinkConta {
  conta: UsuarioConta;
  emailEnviado: boolean;
  link: string | null;
  aviso: string | null;
}

export interface SenhaGerada {
  nome: string;
  email: string;
  senha: string;
}
