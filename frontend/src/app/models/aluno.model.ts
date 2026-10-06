export type Periodo = 'Manha' | 'Tarde' | 'Integral' | 'Noite';

export interface Turma {
  id: string;
  nome: string;
  periodo: Periodo;
  horarioEntrada: string;
  horarioSaida: string;
  quantidadeAlunos: number;
  professorId: string | null;
  professorNome: string | null;
  unidadeId: string;
  unidadeNome: string;
  ativa: boolean;
  /** Valor mensal de cada aluno da turma (antes do desconto do aluno); nulo = fora da geração em lote. */
  valorMensalidade?: number | null;
}

export interface CriarOuEditarTurma {
  nome: string;
  periodo: Periodo;
  horarioEntrada: string;
  horarioSaida: string;
  professorId: string | null;
  unidadeId: string;
  valorMensalidade: number | null;
}

export type PosicaoAtleta =
  | 'Goleiro'
  | 'Zagueiro'
  | 'LateralDireito'
  | 'LateralEsquerdo'
  | 'Volante'
  | 'MeioCampo'
  | 'MeiaAtacante'
  | 'PontaDireita'
  | 'PontaEsquerda'
  | 'Centroavante';

export const POSICOES_ATLETA: { valor: PosicaoAtleta; rotulo: string }[] = [
  { valor: 'Goleiro', rotulo: 'Goleiro' },
  { valor: 'Zagueiro', rotulo: 'Zagueiro' },
  { valor: 'LateralDireito', rotulo: 'Lateral direito' },
  { valor: 'LateralEsquerdo', rotulo: 'Lateral esquerdo' },
  { valor: 'Volante', rotulo: 'Volante' },
  { valor: 'MeioCampo', rotulo: 'Meio-campo' },
  { valor: 'MeiaAtacante', rotulo: 'Meia-atacante' },
  { valor: 'PontaDireita', rotulo: 'Ponta direita' },
  { valor: 'PontaEsquerda', rotulo: 'Ponta esquerda' },
  { valor: 'Centroavante', rotulo: 'Centroavante' }
];

export function rotuloPosicao(posicao: PosicaoAtleta | null | undefined): string {
  return POSICOES_ATLETA.find((p) => p.valor === posicao)?.rotulo ?? '';
}

export interface Aluno {
  id: string;
  nome: string;
  dataNascimento: string;
  fotoUrl: string | null;
  turmaId: string;
  turmaNome: string;
  ativo: boolean;
  posicao?: PosicaoAtleta | null;
  /** Derivado pelo backend das mensalidades em atraso e da configuração "dias para bloqueio" — não é editável. */
  bloqueado?: boolean;
  /** Validade do atestado médico, da Ficha de Saúde (só vem na listagem). */
  atestadoValidoAte?: string | null;
}

export interface ResponsavelResumo {
  id: string | null;
  nome: string;
  email: string;
  telefone: string | null;
  responsavelFinanceiro: boolean;
}

export interface SenhaGeradaResponsavel {
  nome: string;
  email: string;
  senha: string;
}

export interface AlunoDetalhe extends Aluno {
  responsaveis: ResponsavelResumo[];
  senhasGeradas: SenhaGeradaResponsavel[];
  descontoMensalidadePercentual: number;
  motivoDesconto: string | null;
}

export interface CriarOuEditarAluno {
  nome: string;
  dataNascimento: string;
  turmaId: string;
  fotoUrl: string | null;
  responsaveis: ResponsavelResumo[];
  posicao: PosicaoAtleta | null;
  descontoMensalidadePercentual: number;
  motivoDesconto: string | null;
}
