import { Component, ElementRef, HostListener, computed, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { Aluno } from '../../models/aluno.model';

/** Combobox com busca por nome — troca o <select> simples quando a lista de alunos pode crescer muito. */
@Component({
  selector: 'app-seletor-aluno',
  imports: [FormsModule],
  templateUrl: './seletor-aluno.component.html',
  styleUrl: './seletor-aluno.component.scss'
})
export class SeletorAlunoComponent {
  private readonly elementRef = inject(ElementRef<HTMLElement>);

  readonly alunos = input.required<Aluno[]>();
  readonly alunoSelecionadoId = input<string | null>(null);
  readonly selecionar = output<string>();

  readonly aberto = signal(false);
  readonly busca = signal('');

  readonly alunoSelecionado = computed(() => this.alunos().find((a) => a.id === this.alunoSelecionadoId()) ?? null);

  readonly alunosFiltrados = computed(() => {
    const termo = this.busca().trim().toLowerCase();
    const lista = this.alunos();
    if (!termo) return lista;
    return lista.filter((a) => a.nome.toLowerCase().includes(termo));
  });

  /** Fecha ao clicar em qualquer lugar fora do componente — mais confiável que um backdrop fixed,
   * que depende de z-index/stacking-context e pode ser "furado" por outros elementos da página. */
  @HostListener('document:click', ['$event'])
  aoClicarFora(event: MouseEvent): void {
    if (!this.aberto()) return;
    if (!this.elementRef.nativeElement.contains(event.target as Node)) {
      this.fechar();
    }
  }

  alternar(): void {
    if (this.aberto()) {
      this.fechar();
    } else {
      this.busca.set('');
      this.aberto.set(true);
    }
  }

  fechar(): void {
    this.aberto.set(false);
  }

  escolher(aluno: Aluno): void {
    this.selecionar.emit(aluno.id);
    this.fechar();
  }
}
