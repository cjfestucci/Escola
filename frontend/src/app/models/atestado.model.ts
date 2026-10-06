import { formatarDataAbsoluta, hojeIso } from '../shared/data-utils';

/** Atestado que vence dentro desta janela (em dias, contando hoje) já gera aviso de "vence em breve". */
export const DIAS_AVISO_ATESTADO = 30;

/** Derivada da data de validade da Ficha de Saúde contra o "hoje" da escola — nunca gravada. */
export type SituacaoAtestado = 'sem' | 'vencido' | 'vencendo' | 'valido';

function diasEntre(deIso: string, ateIso: string): number {
  return Math.round((Date.parse(`${ateIso}T00:00:00Z`) - Date.parse(`${deIso}T00:00:00Z`)) / 86_400_000);
}

/** A data de validade é o último dia válido: vence no dia seguinte. */
export function situacaoAtestado(validoAte: string | null | undefined, hoje: string = hojeIso()): SituacaoAtestado {
  if (!validoAte) return 'sem';
  const dias = diasEntre(hoje, validoAte);
  if (dias < 0) return 'vencido';
  if (dias < DIAS_AVISO_ATESTADO) return 'vencendo';
  return 'valido';
}

export function rotuloAtestado(validoAte: string | null | undefined, hoje: string = hojeIso()): string {
  if (!validoAte) return 'Sem atestado médico';
  const dias = diasEntre(hoje, validoAte);
  if (dias < 0) return `Atestado vencido em ${formatarDataAbsoluta(validoAte)}`;
  if (dias === 0) return 'Atestado vence hoje';
  if (dias === 1) return 'Atestado vence amanhã';
  if (dias < DIAS_AVISO_ATESTADO) return `Atestado vence em ${dias} dias`;
  return `Atestado válido até ${formatarDataAbsoluta(validoAte)}`;
}
