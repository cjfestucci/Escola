export interface RegistroDiarioClasse {
  id: string;
  turmaId: string;
  titulo: string;
  descricao: string | null;
  fotoUrls: string[];
  registradoEm: string;
  criadoPorNome: string;
}

export interface CriarOuEditarRegistroDiarioClasse {
  usuarioId: string;
  titulo: string;
  descricao: string | null;
  fotoUrls: string[];
}
