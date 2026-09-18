import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { Periodo } from '../../models/aluno.model';
import { Usuario } from '../../models/usuario.model';
import { TurmaService } from '../../services/turma.service';
import { UsuarioService } from '../../services/usuario.service';
import { SeletorHorarioComponent } from '../../shared/seletor-horario/seletor-horario.component';

@Component({
  selector: 'app-turma-formulario',
  imports: [FormsModule, SeletorHorarioComponent],
  templateUrl: './turma-formulario.component.html',
  styleUrl: './turma-formulario.component.scss'
})
export class TurmaFormularioComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly turmaService = inject(TurmaService);
  private readonly usuarioService = inject(UsuarioService);

  private turmaId: string | null = null;

  readonly educadores = signal<Usuario[]>([]);
  readonly carregando = signal(true);
  readonly salvando = signal(false);
  readonly erro = signal<string | null>(null);

  nome = '';
  periodo: Periodo = 'Manha';
  horarioEntrada = '07:00';
  horarioSaida = '12:00';
  professorId = '';

  get titulo(): string {
    return this.turmaId ? 'Editar turma' : 'Nova turma';
  }

  ngOnInit(): void {
    this.turmaId = this.route.snapshot.paramMap.get('id');

    this.usuarioService.listarEducadores().subscribe((educadores) => this.educadores.set(educadores));

    if (this.turmaId) {
      this.turmaService.obterPorId(this.turmaId).subscribe({
        next: (turma) => {
          this.nome = turma.nome;
          this.periodo = turma.periodo;
          this.horarioEntrada = turma.horarioEntrada.slice(0, 5);
          this.horarioSaida = turma.horarioSaida.slice(0, 5);
          this.professorId = turma.professorId ?? '';
          this.carregando.set(false);
        },
        error: () => {
          this.erro.set('Não foi possível carregar a turma.');
          this.carregando.set(false);
        }
      });
    } else {
      this.carregando.set(false);
    }
  }

  salvar(): void {
    this.erro.set(null);

    if (!this.nome.trim()) {
      this.erro.set('Informe o nome da turma.');
      return;
    }
    if (this.horarioSaida <= this.horarioEntrada) {
      this.erro.set('Horário de saída deve ser depois do horário de entrada.');
      return;
    }

    const payload = {
      nome: this.nome.trim(),
      periodo: this.periodo,
      horarioEntrada: this.horarioEntrada,
      horarioSaida: this.horarioSaida,
      professorId: this.professorId || null
    };

    this.salvando.set(true);
    const requisicao$ = this.turmaId
      ? this.turmaService.editar(this.turmaId, payload)
      : this.turmaService.criar(payload);

    requisicao$.subscribe({
      next: () => this.router.navigateByUrl('/turmas'),
      error: (resposta) => {
        this.salvando.set(false);
        this.erro.set(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível salvar a turma.');
      }
    });
  }

  cancelar(): void {
    this.router.navigateByUrl('/turmas');
  }
}
