export type SegmentoCliente = 'Escola' | 'Clube';

export const ROTULOS_SEGMENTO: Record<SegmentoCliente, string> = {
  Escola: 'Escola infantil',
  Clube: 'Clube / escola de esportes'
};

export interface ClientePlataforma {
  id: string;
  nome: string;
  segmento: SegmentoCliente;
  /** Falso = acesso suspenso: ninguém do cliente consegue entrar (o Suporte continua entrando). */
  ativo: boolean;
  criadoEm: string;
}

export interface EditarClientePlataforma {
  nome: string;
  segmento: SegmentoCliente;
  ativo: boolean;
}

/** Só diz se cada integração do ambiente está configurada — nunca traz segredo. */
export interface DiagnosticoPlataforma {
  ambiente: string;
  smtpConfigurado: boolean;
  urlBaseConfigurada: boolean;
  pixAutomaticoConfigurado: boolean;
  pixAutomaticoAmbiente: string;
  pixCertificadoConfigurado: boolean;
  pixWebhookHabilitado: boolean;
}

/** Conta de Admin da escola (a única que o Suporte enxerga/cria). */
export interface AdminEscola {
  id: string;
  nome: string;
  email: string;
  ativo: boolean;
  /** Criada por convite e ainda sem senha: o link de convite não foi aceito. Não consegue entrar. */
  pendente: boolean;
  ultimoEnvioEm: string | null;
}

export interface ResultadoEnvioAdmin {
  admin: AdminEscola;
  /** Falso se o e-mail não foi entregue (ver aviso); a conta/link existem do mesmo jeito e dá pra reenviar. */
  emailEnviado: boolean;
  aviso: string | null;
}
