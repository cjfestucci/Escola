export type SituacaoAssinatura = 'AguardandoPagamento' | 'EmTeste' | 'Ativa' | 'EmAtraso' | 'Suspensa' | 'Cancelada';

export interface PlanoAssinatura {
  precoFixo: number;
  precoPorAtleta: number;
  diasTeste: number;
  versaoTermos: string;
}

export interface CadastroAssinaturaRequest {
  nomeClube: string;
  cpfCnpj: string;
  cidade: string;
  atletas: number;
  nomeAdmin: string;
  email: string;
  celular: string;
  aceiteTermos: boolean;
}

export interface CadastroAssinaturaResposta {
  situacao: SituacaoAssinatura;
  testeAte: string | null;
  valorMensal: number;
  /** Só sem teste grátis: a página de pagamento da 1ª fatura. */
  linkPagamento: string | null;
  emailEnviado: boolean;
}

export interface FaturaAssinatura {
  vencimento: string;
  valor: number;
  paga: boolean;
  pagaEm: string | null;
  linkPagamento: string | null;
}

export interface MinhaAssinatura {
  situacao: SituacaoAssinatura;
  rotulo: string;
  bloqueada: boolean;
  testeAte: string | null;
  diasRestantesTeste: number | null;
  valorMensal: number;
  atletasInformados: number;
  vencimentoEmAberto: string | null;
  linkPagamento: string | null;
  diasToleranciaAtraso: number;
  faturasDisponiveis: boolean;
  faturas: FaturaAssinatura[];
}

export function valorDoPlano(plano: PlanoAssinatura, atletas: number): number {
  return Math.round((plano.precoFixo + plano.precoPorAtleta * Math.max(0, atletas)) * 100) / 100;
}

export function formatarReais(valor: number): string {
  return valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

/** Só dígitos, com dígito verificador (mesma regra do backend). */
export function cpfValido(cpf: string): boolean {
  if (!/^\d{11}$/.test(cpf) || /^(\d)\1{10}$/.test(cpf)) return false;
  const digito = (n: number) => {
    let soma = 0;
    for (let i = 0; i < n; i++) soma += Number(cpf[i]) * (n + 1 - i);
    const resto = (soma * 10) % 11;
    return resto === 10 ? 0 : resto;
  };
  return digito(9) === Number(cpf[9]) && digito(10) === Number(cpf[10]);
}

export function cnpjValido(cnpj: string): boolean {
  if (!/^\d{14}$/.test(cnpj) || /^(\d)\1{13}$/.test(cnpj)) return false;
  const digito = (n: number) => {
    const pesos = n === 12 ? [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2] : [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    const soma = pesos.reduce((total, peso, i) => total + Number(cnpj[i]) * peso, 0);
    const resto = soma % 11;
    return resto < 2 ? 0 : 11 - resto;
  };
  return digito(12) === Number(cnpj[12]) && digito(13) === Number(cnpj[13]);
}
