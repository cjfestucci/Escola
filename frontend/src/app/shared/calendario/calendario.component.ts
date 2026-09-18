import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';

import { hojeIso } from '../data-utils';

const NOMES_MES = [
  'Janeiro',
  'Fevereiro',
  'Março',
  'Abril',
  'Maio',
  'Junho',
  'Julho',
  'Agosto',
  'Setembro',
  'Outubro',
  'Novembro',
  'Dezembro'
];
const NOMES_DIA_SEMANA = ['Dom', 'Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb'];

interface CelulaDia {
  dataIso: string;
  dia: number;
  foraDoMes: boolean;
}

/** Calendário em português, independente do idioma do navegador (o date picker nativo segue o idioma do Chrome, não da página). */
@Component({
  selector: 'app-calendario',
  imports: [],
  templateUrl: './calendario.component.html',
  styleUrl: './calendario.component.scss'
})
export class CalendarioComponent implements OnChanges {
  @Input() dataSelecionada = '';
  @Input() dataMaxima: string | null = null;
  @Output() readonly escolher = new EventEmitter<string>();

  protected readonly nomesDiaSemana = NOMES_DIA_SEMANA;

  protected ano = 2026;
  protected mes = 0;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['dataSelecionada']?.firstChange) {
      const [ano, mes] = (this.dataSelecionada || hojeIso()).split('-').map(Number);
      this.ano = ano;
      this.mes = mes - 1;
    }
  }

  protected get nomeMesAno(): string {
    return `${NOMES_MES[this.mes]} de ${this.ano}`;
  }

  protected get celulas(): CelulaDia[] {
    const primeiroDia = new Date(Date.UTC(this.ano, this.mes, 1));
    const diaSemanaInicio = primeiroDia.getUTCDay();
    const inicio = new Date(Date.UTC(this.ano, this.mes, 1 - diaSemanaInicio));

    return Array.from({ length: 42 }, (_, i) => {
      const data = new Date(inicio);
      data.setUTCDate(inicio.getUTCDate() + i);
      return {
        dataIso: data.toISOString().slice(0, 10),
        dia: data.getUTCDate(),
        foraDoMes: data.getUTCMonth() !== this.mes
      };
    });
  }

  protected get mesSeguinteDesabilitado(): boolean {
    if (!this.dataMaxima) return false;
    const [anoMax, mesMax] = this.dataMaxima.split('-').map(Number);
    return this.ano > anoMax || (this.ano === anoMax && this.mes >= mesMax - 1);
  }

  mesAnterior(): void {
    if (this.mes === 0) {
      this.mes = 11;
      this.ano--;
    } else {
      this.mes--;
    }
  }

  mesSeguinte(): void {
    if (this.mesSeguinteDesabilitado) return;
    if (this.mes === 11) {
      this.mes = 0;
      this.ano++;
    } else {
      this.mes++;
    }
  }

  diaDesabilitado(dataIso: string): boolean {
    return !!this.dataMaxima && dataIso > this.dataMaxima;
  }

  ehHoje(dataIso: string): boolean {
    return dataIso === hojeIso();
  }

  ehSelecionado(dataIso: string): boolean {
    return dataIso === this.dataSelecionada;
  }

  selecionar(dataIso: string): void {
    if (this.diaDesabilitado(dataIso)) return;
    this.escolher.emit(dataIso);
  }
}
