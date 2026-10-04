export type LocalJogo = 'Casa' | 'Fora' | 'Neutro';
export type StatusJogo = 'Agendado' | 'Realizado' | 'Cancelado';

export const ROTULOS_MANDO: Record<LocalJogo, string> = { Casa: 'Casa', Fora: 'Fora', Neutro: 'Campo neutro' };
export const ROTULOS_STATUS_JOGO: Record<StatusJogo, string> = { Agendado: 'Agendado', Realizado: 'Realizado', Cancelado: 'Cancelado' };

export interface Desempenho {
  turmaId: string;
  turmaNome: string;
  jogos: number;
  vitorias: number;
  empates: number;
  derrotas: number;
  golsPro: number;
  golsContra: number;
  pontos: number;
}

export interface Artilheiro {
  alunoId: string;
  alunoNome: string;
  gols: number;
}

export type TipoAlertaDisciplinar = 'Suspenso' | 'Pendurado';

/** Suspenso ou pendurado, com a explicação pronta — derivado dos cartões das súmulas. */
export interface AlertaDisciplinar {
  alunoId: string;
  alunoNome: string;
  tipo: TipoAlertaDisciplinar;
  detalhe: string;
  amarelosAcumulados: number;
}

export interface Campeonato {
  id: string;
  nome: string;
  dataInicio: string;
  dataFim: string | null;
  observacao: string | null;
  /** Amarelos acumulados que geram suspensão; nulo = sem controle disciplinar. */
  amarelosParaSuspensao: number | null;
  ativo: boolean;
  quantidadeJogos: number;
  /** Só vêm no detalhe (`GET /api/campeonatos/{id}`). */
  desempenho?: Desempenho[] | null;
  artilheiros?: Artilheiro[] | null;
  disciplina?: AlertaDisciplinar[] | null;
}

export interface CriarOuEditarCampeonato {
  nome: string;
  dataInicio: string;
  dataFim: string | null;
  observacao: string | null;
  amarelosParaSuspensao: number | null;
}

export interface JogoAtleta {
  alunoId: string;
  alunoNome: string;
  titular: boolean;
  gols: number;
  cartoesAmarelos: number;
  cartaoVermelho: boolean;
}

export interface Jogo {
  id: string;
  campeonatoId: string | null;
  campeonatoNome: string | null;
  turmaId: string;
  turmaNome: string;
  adversario: string;
  data: string;
  hora: string;
  local: string | null;
  mando: LocalJogo;
  status: StatusJogo;
  golsPro: number | null;
  golsContra: number | null;
  observacao: string | null;
  quantidadeConvocados: number;
  /** Só vem no detalhe (`GET /api/jogos/{id}`). */
  convocados?: JogoAtleta[] | null;
  /** Só em jogo agendado de campeonato com controle disciplinar (senão 0 / ausente). */
  suspensos?: number;
  pendurados?: number;
  alertas?: AlertaDisciplinar[] | null;
}

export interface CriarOuEditarJogo {
  campeonatoId: string | null;
  turmaId: string;
  adversario: string;
  data: string;
  hora: string;
  local: string | null;
  mando: LocalJogo;
  status: StatusJogo;
  golsPro: number | null;
  golsContra: number | null;
  observacao: string | null;
}

export type ResultadoJogo = 'V' | 'E' | 'D';

/** Resultado do nosso time — derivado do placar, nunca guardado. Só existe em jogo realizado. */
export function resultadoDoJogo(jogo: Pick<Jogo, 'status' | 'golsPro' | 'golsContra'>): ResultadoJogo | null {
  if (jogo.status !== 'Realizado' || jogo.golsPro === null || jogo.golsContra === null) return null;
  if (jogo.golsPro > jogo.golsContra) return 'V';
  return jogo.golsPro === jogo.golsContra ? 'E' : 'D';
}

export const ROTULOS_RESULTADO: Record<ResultadoJogo, string> = { V: 'Vitória', E: 'Empate', D: 'Derrota' };
