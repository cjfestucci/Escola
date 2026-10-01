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
}

export interface CriarOuEditarTurma {
  nome: string;
  periodo: Periodo;
  horarioEntrada: string;
  horarioSaida: string;
  professorId: string | null;
  unidadeId: string;
}

export interface Aluno {
  id: string;
  nome: string;
  dataNascimento: string;
  fotoUrl: string | null;
  turmaId: string;
  turmaNome: string;
  ativo: boolean;
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
}

export interface CriarOuEditarAluno {
  nome: string;
  dataNascimento: string;
  turmaId: string;
  fotoUrl: string | null;
  responsaveis: ResponsavelResumo[];
}
