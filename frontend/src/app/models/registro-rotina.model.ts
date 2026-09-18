export type CategoriaRegistro = 'Alimentacao' | 'Sono' | 'Higiene' | 'Humor' | 'Momento';

export type Refeicao = 'Cafe' | 'Almoco' | 'Lanche';
export type StatusAlimentacao = 'ComeuTudo' | 'Parcial' | 'Recusou';
export type TipoHigiene = 'TrocaFralda' | 'Banheiro';
export type Humor = 'Feliz' | 'Agitado' | 'Sonolento' | 'Choroso';

export const MAX_FOTOS_POR_REGISTRO = 4;

export interface RegistroRotina {
  id: string;
  alunoId: string;
  categoria: CategoriaRegistro;
  registradoEm: string;
  observacao: string | null;
  fotoUrls: string[];
  criadoPorNome: string;
  refeicao: Refeicao | null;
  statusAlimentacao: StatusAlimentacao | null;
  horaInicio: string | null;
  horaFim: string | null;
  tipoHigiene: TipoHigiene | null;
  humor: Humor | null;
}

interface CampoComum {
  usuarioId: string;
  observacao?: string | null;
  fotoUrls?: string[] | null;
}

export interface CriarRegistroAlimentacao extends CampoComum {
  refeicao: Refeicao;
  status: StatusAlimentacao;
}

export interface CriarRegistroSono extends CampoComum {
  horaInicio: string;
  horaFim?: string | null;
}

export interface CriarRegistroHigiene extends CampoComum {
  tipo: TipoHigiene;
}

export interface CriarRegistroHumor extends CampoComum {
  humor: Humor;
}

export type CriarRegistroMomento = CampoComum;
