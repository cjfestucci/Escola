export interface Turma {
  id: string;
  nome: string;
  quantidadeAlunos: number;
}

export interface Aluno {
  id: string;
  nome: string;
  dataNascimento: string;
  fotoUrl: string | null;
  turmaId: string;
  turmaNome: string;
}
