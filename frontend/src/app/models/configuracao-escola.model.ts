export interface ConfiguracaoEscola {
  id: string | null;
  fusoHorario: string;
}

/** Fusos oferecidos na tela de configuração (o backend aceita qualquer fuso IANA válido). */
export const FUSOS_HORARIOS: { valor: string; rotulo: string }[] = [
  { valor: 'America/Noronha', rotulo: 'Fernando de Noronha (UTC−2)' },
  { valor: 'America/Sao_Paulo', rotulo: 'Brasília — SP, RJ, MG, Sul, Nordeste, GO, DF, PA, TO, AP (UTC−3)' },
  { valor: 'America/Cuiaba', rotulo: 'Mato Grosso e Mato Grosso do Sul (UTC−4)' },
  { valor: 'America/Manaus', rotulo: 'Amazonas, Rondônia e Roraima (UTC−4)' },
  { valor: 'America/Rio_Branco', rotulo: 'Acre e sudoeste do Amazonas (UTC−5)' }
];
