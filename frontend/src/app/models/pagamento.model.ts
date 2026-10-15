/** Conta de pagamento da escola no gateway (subconta Asaas aberta pela plataforma). */
export interface ContaPagamento {
  /** O gateway está configurado neste ambiente (sem isso não dá pra conectar). */
  disponivel: boolean;
  ambiente: string;
  conectada: boolean;
  titularNome: string | null;
  titularCpfCnpj: string | null;
  titularEmail: string | null;
  chavePixCriada: boolean;
  webhookConfigurado: boolean;
  /** Situação da conta no gateway (APPROVED, PENDING, AWAITING_APPROVAL, REJECTED…). */
  situacaoGateway: string | null;
  criadaEm: string | null;
  /** Já dá pra cobrar com baixa automática por esta conta. */
  pagamentoAutomaticoAtivo: boolean;
  /** Documentos que faltam pra aprovação (só enquanto a conta não está aprovada; nulo se não consultou). */
  documentos?: DocumentoPendente[] | null;
  /** Motivo de recusa informado pelo gateway, se houver. */
  motivoRecusa?: string | null;
}

export interface DocumentoPendente {
  titulo: string;
  descricao: string | null;
  /** Não enviado, Em análise ou Recusado. */
  situacao: string;
  /** Página do gateway pra enviar o documento; nula = não é pela página (fale com o suporte). */
  urlEnvio: string | null;
}

export type TipoEmpresa = 'MEI' | 'LIMITED' | 'INDIVIDUAL' | 'ASSOCIATION';

export const TIPOS_EMPRESA: { valor: TipoEmpresa; rotulo: string }[] = [
  { valor: 'MEI', rotulo: 'MEI' },
  { valor: 'LIMITED', rotulo: 'Sociedade limitada (LTDA)' },
  { valor: 'INDIVIDUAL', rotulo: 'Empresário individual / EIRELI / SLU' },
  { valor: 'ASSOCIATION', rotulo: 'Associação / entidade sem fins lucrativos' }
];

export interface ConectarContaPagamento {
  nome: string;
  email: string;
  cpfCnpj: string;
  tipoEmpresa: TipoEmpresa | null;
  dataNascimento: string | null;
  celular: string;
  cep: string;
  endereco: string;
  numero: string;
  complemento: string | null;
  bairro: string;
  faturamentoMensal: number;
}

/** Rótulo em pt-BR da situação que o gateway devolve. */
export function rotuloSituacaoGateway(situacao: string | null): string {
  switch ((situacao ?? '').toUpperCase()) {
    case 'APPROVED': return 'Aprovada';
    case 'PENDING': return 'Pendente — faltam dados ou documentos';
    case 'AWAITING_APPROVAL': return 'Em análise pelo gateway';
    case 'REJECTED': return 'Recusada';
    case '': return 'Ainda não consultada';
    default: return situacao!;
  }
}
