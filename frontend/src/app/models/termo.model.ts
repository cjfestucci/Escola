export interface TermoAlunoPendente {
  alunoId: string;
  alunoNome: string;
  matriculaPendente: boolean;
}

/** Termo de matrícula/LGPD da versão atual e os filhos que ainda não têm aceite dela (lista vazia = nada a aceitar). */
export interface TermoPendente {
  versao: string;
  titulo: string;
  paragrafos: string[];
  alunos: TermoAlunoPendente[];
}

export interface TermoAceite {
  id: string;
  responsavelNome: string;
  versao: string;
  aceitoEm: string;
  ip: string | null;
  textoAceito: string;
}
