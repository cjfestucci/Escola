export type StatusPresenca = 'Presente' | 'Falta' | 'Justificada';

export const ROTULOS_PRESENCA: Record<StatusPresenca, string> = {
  Presente: 'Presente',
  Falta: 'Falta',
  Justificada: 'Justificada'
};

/** A partir de quantas faltas seguidas o aluno/atleta é destacado como faltoso (mesmo valor do backend). */
export const FALTAS_SEGUIDAS_PARA_ALERTA = 3;

export interface ChamadaItem {
  alunoId: string;
  alunoNome: string;
  fotoUrl: string | null;
  /** Nulo = ainda não marcado nesse dia. */
  status: StatusPresenca | null;
}

export interface Chamada {
  turmaId: string;
  data: string;
  registrada: boolean;
  itens: ChamadaItem[];
  presentes: number;
  faltas: number;
  justificadas: number;
}

export interface SalvarChamada {
  data: string;
  itens: { alunoId: string; status: StatusPresenca }[];
}

export interface FrequenciaAluno {
  alunoId: string;
  alunoNome: string;
  presencas: number;
  faltas: number;
  justificadas: number;
  /** Presenças ÷ (presenças + faltas), em %. Nulo se ainda não há chamada na janela. */
  percentual: number | null;
  faltasSeguidas: number;
}

export interface PresencaRecente {
  data: string;
  status: StatusPresenca;
}

export interface FrequenciaDoAluno {
  resumo: FrequenciaAluno;
  janelaDias: number;
  /** Chamadas do período, da mais recente pra mais antiga (até 60). */
  recentes: PresencaRecente[];
  /** Período efetivamente usado (inclusive nas duas pontas; o fim nunca passa de hoje). */
  de: string;
  ate: string;
}

export interface Faltoso {
  alunoId: string;
  alunoNome: string;
  turmaId: string;
  turmaNome: string;
  faltasSeguidas: number;
}
