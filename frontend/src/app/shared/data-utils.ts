/** Utilidades de data em UTC puro (YYYY-MM-DD), pra bater com o corte de dia que a API usa. */

export function hojeIso(): string {
  return new Date().toISOString().slice(0, 10);
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

export function idadeFormatada(dataNascimento: string): string {
  const nascimento = new Date(dataNascimento);
  const hoje = new Date();
  let meses = (hoje.getFullYear() - nascimento.getFullYear()) * 12 + (hoje.getMonth() - nascimento.getMonth());
  if (hoje.getDate() < nascimento.getDate()) meses--;

  if (meses < 24) return `${meses} meses`;
  return `${Math.floor(meses / 12)} anos`;
}
