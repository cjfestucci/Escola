export interface Cobranca {
  id: string;
  alunoId: string;
  alunoNome: string;
  turmaNome: string;
  descricao: string;
  valor: number;
  vencimento: string;
  paga: boolean;
  pagoEm: string | null;
  cancelada: boolean;
  canceladaEm: string | null;
  /** Multa/juros por atraso calculados agora (zero se não está atrasada ou a escola não cobra). */
  multa: number;
  juros: number;
  /** valor + multa + juros: é o que o Pix cobra e o que fica gravado como valorPago ao marcar como paga. */
  valorAtualizado: number;
  diasAtraso: number;
  /** Quanto foi recebido de fato (nulo se não paga). */
  valorPago: number | null;
  /** Primeiro dia do mês da mensalidade (só nas geradas em lote). */
  competencia: string | null;
}

export interface CriarCobranca {
  alunoId: string;
  descricao: string;
  valor: number;
  vencimento: string;
}

export interface EditarCobranca {
  descricao: string;
  valor: number;
  vencimento: string;
}

export type TipoChavePix = 'Cpf' | 'Cnpj' | 'Telefone' | 'Email' | 'Aleatoria';

export interface ConfiguracaoFinanceira {
  id: string | null;
  pixChave: string | null;
  pixTipoChave: TipoChavePix | null;
  pixNomeRecebedor: string | null;
  pixCidade: string | null;
  diasParaBloqueio: number | null;
  configurado: boolean;
  diaVencimentoMensalidade: number;
  multaAtrasoPercentual: number | null;
  jurosMensaisPercentual: number | null;
  pagamentoPixAtivo: boolean;
  pagamentoBoletoAtivo: boolean;
  pagamentoPresencialAtivo: boolean;
  instrucoesPagamentoPresencial: string | null;
}

export interface EditarConfiguracaoFinanceira {
  pixChave: string | null;
  pixTipoChave: TipoChavePix | null;
  pixNomeRecebedor: string | null;
  pixCidade: string | null;
  diasParaBloqueio: number | null;
  diaVencimentoMensalidade: number;
  multaAtrasoPercentual: number | null;
  jurosMensaisPercentual: number | null;
  pagamentoPixAtivo: boolean;
  pagamentoBoletoAtivo: boolean;
  pagamentoPresencialAtivo: boolean;
  instrucoesPagamentoPresencial: string | null;
}

export interface PixCobranca {
  codigoCopiaECola: string;
  /** O código foi criado no banco: o pagamento é confirmado e baixado sozinho. Falso = Pix estático (baixa manual). */
  automatico: boolean;
}

export type SituacaoMensalidade = 'Gerar' | 'JaExiste' | 'SemValor' | 'Isento' | 'MatriculaPendente';

export interface MensalidadeItem {
  alunoId: string;
  alunoNome: string;
  turmaNome: string;
  valorBase: number;
  descontoPercentual: number;
  valor: number;
  situacao: SituacaoMensalidade;
}

export interface PreviaMensalidades {
  ano: number;
  mes: number;
  descricao: string;
  vencimento: string;
  itens: MensalidadeItem[];
  aGerar: number;
  jaExistem: number;
  semValor: number;
  isentos: number;
  totalAGerar: number;
  /** Alunos com matrícula ainda não confirmada pelo responsável (termo não aceito): ficam de fora. */
  matriculasPendentes?: number;
}

export interface GeracaoMensalidades {
  geradas: number;
  jaExistiam: number;
  semValor: number;
  isentos: number;
  total: number;
}

/** Situação da integração com a API Pix do banco (baixa automática). Ligada por configuração do ambiente, não por esta tela. */
export interface PixAutomaticoStatus {
  configurado: boolean;
  ambiente: string;
  chavePixConfigurada: boolean;
  certificadoConfigurado: boolean;
  webhookHabilitado: boolean;
  webhookUrlBaseConfigurada: boolean;
  intervaloConciliacaoSegundos: number;
  cobrancasAguardando: number;
  pagasAutomaticamente: number;
}

/** Formas de pagamento ativas (Configurações → Financeiro): o portal e as telas só oferecem estas. */
export interface FormasPagamento {
  pix: boolean;
  boleto: boolean;
  presencial: boolean;
  instrucoesPresencial: string | null;
}
