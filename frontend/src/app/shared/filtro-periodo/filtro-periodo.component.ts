import { Component, computed, output, signal } from '@angular/core';

import { CalendarioComponent } from '../calendario/calendario.component';
import { formatarDataAbsoluta, hojeIso, somarDias } from '../data-utils';

export interface PeriodoEscolhido {
  /** Datas de calendário (YYYY-MM-DD), inclusive nas duas pontas. */
  de: string;
  ate: string;
  /** Texto pronto pra frase ("últimos 30 dias", "setembro de 2026", "de 01/08/2026 a 15/08/2026"). */
  rotulo: string;
}

type Atalho = 'ultimos7' | 'ultimos30' | 'ultimos90' | 'mes-atual' | 'mes-anterior' | 'personalizado';

const NOMES_MES = ['janeiro', 'fevereiro', 'março', 'abril', 'maio', 'junho', 'julho', 'agosto', 'setembro', 'outubro', 'novembro', 'dezembro'];

/** Período padrão usado pelas telas de frequência (últimos 30 dias). */
export function periodoPadrao(): PeriodoEscolhido {
  const hoje = hojeIso();
  return { de: somarDias(hoje, -29), ate: hoje, rotulo: 'últimos 30 dias' };
}

/** Filtro de período: atalhos prontos (7/30/90 dias, mês atual/anterior) e "Personalizado" com data inicial e final.
 * Só emite períodos válidos (início ≤ fim, nada depois de hoje) — quem usa não precisa revalidar. */
@Component({
  selector: 'app-filtro-periodo',
  imports: [CalendarioComponent],
  templateUrl: './filtro-periodo.component.html',
  styleUrl: './filtro-periodo.component.scss'
})
export class FiltroPeriodoComponent {
  readonly periodoChange = output<PeriodoEscolhido>();

  protected readonly atalhos: { valor: Atalho; rotulo: string }[] = [
    { valor: 'ultimos7', rotulo: '7 dias' },
    { valor: 'ultimos30', rotulo: '30 dias' },
    { valor: 'ultimos90', rotulo: '90 dias' },
    { valor: 'mes-atual', rotulo: 'Mês atual' },
    { valor: 'mes-anterior', rotulo: 'Mês anterior' },
    { valor: 'personalizado', rotulo: 'Personalizado' }
  ];

  readonly atalhoAtivo = signal<Atalho>('ultimos30');
  readonly de = signal(periodoPadrao().de);
  readonly ate = signal(periodoPadrao().ate);
  readonly calendarioAberto = signal<'de' | 'ate' | null>(null);

  protected readonly hojeIso = hojeIso;
  protected readonly formatarDataAbsoluta = formatarDataAbsoluta;
  readonly personalizado = computed(() => this.atalhoAtivo() === 'personalizado');

  escolher(atalho: Atalho): void {
    this.atalhoAtivo.set(atalho);
    this.calendarioAberto.set(null);
    if (atalho === 'personalizado') return; // só emite quando as datas são escolhidas

    const periodo = this.periodoDoAtalho(atalho);
    this.de.set(periodo.de);
    this.ate.set(periodo.ate);
    this.periodoChange.emit(periodo);
  }

  escolherData(qual: 'de' | 'ate', data: string): void {
    this.calendarioAberto.set(null);
    let de = qual === 'de' ? data : this.de();
    let ate = qual === 'ate' ? data : this.ate();
    // Escolher um início depois do fim (ou o contrário) arrasta a outra ponta, em vez de gerar um período inválido.
    if (de > ate) {
      if (qual === 'de') ate = de;
      else de = ate;
    }
    this.de.set(de);
    this.ate.set(ate);
    this.periodoChange.emit({ de, ate, rotulo: `de ${formatarDataAbsoluta(de)} a ${formatarDataAbsoluta(ate)}` });
  }

  private periodoDoAtalho(atalho: Exclude<Atalho, 'personalizado'>): PeriodoEscolhido {
    const hoje = hojeIso();
    switch (atalho) {
      case 'ultimos7':
        return { de: somarDias(hoje, -6), ate: hoje, rotulo: 'últimos 7 dias' };
      case 'ultimos30':
        return periodoPadrao();
      case 'ultimos90':
        return { de: somarDias(hoje, -89), ate: hoje, rotulo: 'últimos 90 dias' };
      case 'mes-atual': {
        const [ano, mes] = hoje.split('-').map(Number);
        return { de: `${ano}-${String(mes).padStart(2, '0')}-01`, ate: hoje, rotulo: `${NOMES_MES[mes - 1]} de ${ano}` };
      }
      case 'mes-anterior': {
        const [ano, mes] = hoje.split('-').map(Number);
        const primeiroDoMes = new Date(Date.UTC(ano, mes - 1, 1));
        const fimAnterior = new Date(primeiroDoMes.getTime() - 86400000);
        const inicioAnterior = new Date(Date.UTC(fimAnterior.getUTCFullYear(), fimAnterior.getUTCMonth(), 1));
        const iso = (d: Date) => d.toISOString().slice(0, 10);
        return { de: iso(inicioAnterior), ate: iso(fimAnterior), rotulo: `${NOMES_MES[fimAnterior.getUTCMonth()]} de ${fimAnterior.getUTCFullYear()}` };
      }
    }
  }
}
