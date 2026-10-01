/**
 * Utilidades de data. Datas "de calendário" (YYYY-MM-DD: vencimento, nascimento, dia visualizado) são
 * manipuladas como texto/UTC puro, sem fuso. Já "que dia é hoje" e a exibição de horários de registros
 * usam sempre o **fuso da escola** (não o do navegador) — um pai viajando vê o mesmo dia letivo que a
 * escola, e o dia não vira às 21h como aconteceria usando UTC no Brasil.
 */

/** Fuso IANA da escola. Carregado da API na inicialização do app (`ConfiguracaoEscolaService`),
 * antes de qualquer tela renderizar; este valor é só o padrão até lá. */
let fusoEscola = 'America/Sao_Paulo';

export function definirFusoEscola(fuso: string): void {
  fusoEscola = fuso;
}

export function obterFusoEscola(): string {
  return fusoEscola;
}

function partesNoFusoEscola(data: Date): { ano: string; mes: string; dia: string; hora: string; minuto: string } {
  const partes = new Intl.DateTimeFormat('pt-BR', {
    timeZone: fusoEscola,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    hourCycle: 'h23'
  }).formatToParts(data);
  const valor = (tipo: Intl.DateTimeFormatPartTypes) => partes.find((p) => p.type === tipo)?.value ?? '';
  return { ano: valor('year'), mes: valor('month'), dia: valor('day'), hora: valor('hour'), minuto: valor('minute') };
}

/** Data de hoje (YYYY-MM-DD) no fuso da escola. */
export function hojeIso(): string {
  const p = partesNoFusoEscola(new Date());
  return `${p.ano}-${p.mes}-${p.dia}`;
}

/** Hora atual (HH:MM) no fuso da escola. */
export function horaAtualEscola(): string {
  const p = partesNoFusoEscola(new Date());
  return `${p.hora}:${p.minuto}`;
}

export function somarDias(dataIso: string, dias: number): string {
  const data = new Date(`${dataIso}T00:00:00Z`);
  data.setUTCDate(data.getUTCDate() + dias);
  return data.toISOString().slice(0, 10);
}

export function rotuloData(dataIso: string): string {
  const hoje = hojeIso();
  if (dataIso === hoje) return 'Hoje';
  if (dataIso === somarDias(hoje, -1)) return 'Ontem';

  const data = new Date(`${dataIso}T00:00:00Z`);
  const rotulo = data.toLocaleDateString('pt-BR', {
    weekday: 'short',
    day: '2-digit',
    month: 'short',
    timeZone: 'UTC'
  });
  return rotulo.charAt(0).toUpperCase() + rotulo.slice(1);
}

export function formatarDataAbsoluta(dataIso: string): string {
  const data = new Date(`${dataIso}T00:00:00Z`);
  return data.toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit', year: 'numeric', timeZone: 'UTC' });
}

export const MESES_PT_BR = [
  { valor: '01', rotulo: 'Janeiro' },
  { valor: '02', rotulo: 'Fevereiro' },
  { valor: '03', rotulo: 'Março' },
  { valor: '04', rotulo: 'Abril' },
  { valor: '05', rotulo: 'Maio' },
  { valor: '06', rotulo: 'Junho' },
  { valor: '07', rotulo: 'Julho' },
  { valor: '08', rotulo: 'Agosto' },
  { valor: '09', rotulo: 'Setembro' },
  { valor: '10', rotulo: 'Outubro' },
  { valor: '11', rotulo: 'Novembro' },
  { valor: '12', rotulo: 'Dezembro' }
];

/** Timestamp (ISO com fuso, vindo da API em UTC) → "dd/mm/aaaa hh:mm" no fuso da escola. */
export function formatarDataHoraAbsoluta(dataHoraIso: string): string {
  const data = new Date(dataHoraIso);
  return data.toLocaleString('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    timeZone: fusoEscola
  });
}

/** Timestamp (ISO com fuso, vindo da API em UTC) → "hh:mm" no fuso da escola. */
export function formatarHora(dataHoraIso: string): string {
  return new Date(dataHoraIso).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit', timeZone: fusoEscola });
}

/** Compara como datas de calendário (texto), sem `new Date('YYYY-MM-DD')` — que vira meia-noite UTC e,
 * no fuso do navegador, cai no dia anterior. */
export function idadeFormatada(dataNascimento: string): string {
  const [anoNasc, mesNasc, diaNasc] = dataNascimento.slice(0, 10).split('-').map(Number);
  const [anoHoje, mesHoje, diaHoje] = hojeIso().split('-').map(Number);
  let meses = (anoHoje - anoNasc) * 12 + (mesHoje - mesNasc);
  if (diaHoje < diaNasc) meses--;

  if (meses < 24) return `${meses} meses`;
  return `${Math.floor(meses / 12)} anos`;
}
