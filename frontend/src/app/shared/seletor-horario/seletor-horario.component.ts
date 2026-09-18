import { Component, computed, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';

const HORAS = Array.from({ length: 24 }, (_, i) => String(i).padStart(2, '0'));
const MINUTOS = Array.from({ length: 60 }, (_, i) => String(i).padStart(2, '0'));

/** Seletor de horário em 24h, sem AM/PM — o <input type="time"> nativo mostra
 * AM/PM em inglês seguindo o idioma do navegador, não da página. */
@Component({
  selector: 'app-seletor-horario',
  imports: [FormsModule],
  templateUrl: './seletor-horario.component.html',
  styleUrl: './seletor-horario.component.scss'
})
export class SeletorHorarioComponent {
  readonly horario = input.required<string>();
  readonly horarioChange = output<string>();

  protected readonly horas = HORAS;
  protected readonly minutos = MINUTOS;

  protected readonly hora = computed(() => this.horario().slice(0, 2) || '00');
  protected readonly minuto = computed(() => this.horario().slice(3, 5) || '00');

  aoAlterarHora(valor: string): void {
    this.horarioChange.emit(`${valor}:${this.minuto()}`);
  }

  aoAlterarMinuto(valor: string): void {
    this.horarioChange.emit(`${this.hora()}:${valor}`);
  }
}
