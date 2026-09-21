export interface FichaSaude {
  tipoSanguineo: string | null;
  alergias: string | null;
  restricoesAlimentares: string | null;
  medicamentosEmUso: string | null;
  condicoesSaude: string | null;
  planoSaude: string | null;
  pediatraNome: string | null;
  pediatraTelefone: string | null;
  contatoEmergenciaNome: string | null;
  contatoEmergenciaTelefone: string | null;
  vacinacaoEmDia: boolean;
  autorizaUsoImagem: boolean;
  atualizadoEm: string | null;
}
