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
}

export interface CriarOuEditarTurma {
  nome: string;
  periodo: Periodo;
  horarioEntrada: string;
  horarioSaida: string;
  professorId: string | null;
}

export interface Aluno {
  id: string;
  nome: string;
  dataNascimento: string;
  fotoUrl: string | null;
  turmaId: string;
  turmaNome: string;
}

export interface ResponsavelResumo {
  id: string | null;
  nome: string;
  email: string;
  telefone: string | null;
  responsavelFinanceiro: boolean;
}

export interface AlunoDetalhe extends Aluno {
  responsaveis: ResponsavelResumo[];
}

export interface CriarOuEditarAluno {
  nome: string;
  dataNascimento: string;
  turmaId: string;
  fotoUrl: string | null;
  responsaveis: ResponsavelResumo[];
}
