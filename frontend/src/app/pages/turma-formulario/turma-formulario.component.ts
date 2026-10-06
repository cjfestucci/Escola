import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { Periodo } from '../../models/aluno.model';
import { Unidade } from '../../models/unidade.model';
import { Usuario } from '../../models/usuario.model';
import { NotificacaoService } from '../../services/notificacao.service';
import { TurmaService } from '../../services/turma.service';
import { UnidadeService } from '../../services/unidade.service';
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
  private readonly unidadeService = inject(UnidadeService);
  private readonly notificacao = inject(NotificacaoService);

  private turmaId: string | null = null;

  readonly educadores = signal<Usuario[]>([]);
  readonly unidades = signal<Unidade[]>([]);
  readonly carregando = signal(true);
  readonly salvando = signal(false);

  nome = '';
  periodo: Periodo = 'Manha';
  horarioEntrada = '07:00';
  horarioSaida = '12:00';
  professorId = '';
  unidadeId = '';
  valorMensalidade: number | null = null;

  get titulo(): string {
    return this.turmaId ? 'Editar turma' : 'Nova turma';
  }

  ngOnInit(): void {
    this.turmaId = this.route.snapshot.paramMap.get('id');

    this.usuarioService.listarEducadores().subscribe((educadores) => this.educadores.set(educadores));
    this.unidadeService.listar().subscribe((unidades) => {
      this.unidades.set(unidades);
      if (!this.turmaId && !this.unidadeId && unidades.length > 0) this.unidadeId = unidades[0].id;
    });

    if (this.turmaId) {
      this.turmaService.obterPorId(this.turmaId).subscribe({
        next: (turma) => {
          this.nome = turma.nome;
          this.periodo = turma.periodo;
          this.horarioEntrada = turma.horarioEntrada.slice(0, 5);
          this.horarioSaida = turma.horarioSaida.slice(0, 5);
          this.professorId = turma.professorId ?? '';
          this.unidadeId = turma.unidadeId;
          this.valorMensalidade = turma.valorMensalidade ?? null;
          this.carregando.set(false);
        },
        error: () => {
          this.notificacao.erro('Não foi possível carregar a turma.');
          this.carregando.set(false);
        }
      });
    } else {
      this.carregando.set(false);
    }
  }

  salvar(): void {
    if (!this.nome.trim()) {
      this.notificacao.erro('Informe o nome da turma.');
      return;
    }
    if (this.horarioSaida <= this.horarioEntrada) {
      this.notificacao.erro('Horário de saída deve ser depois do horário de entrada.');
      return;
    }
    if (!this.unidadeId) {
      this.notificacao.erro('Selecione a unidade.');
      return;
    }

    if (this.valorMensalidade !== null && this.valorMensalidade < 0) {
      this.notificacao.erro('O valor da mensalidade não pode ser negativo.');
      return;
    }

    const payload = {
      nome: this.nome.trim(),
      periodo: this.periodo,
      horarioEntrada: this.horarioEntrada,
      horarioSaida: this.horarioSaida,
      professorId: this.professorId || null,
      unidadeId: this.unidadeId,
      valorMensalidade: this.valorMensalidade || null
    };

    this.salvando.set(true);
    const requisicao$ = this.turmaId
      ? this.turmaService.editar(this.turmaId, payload)
      : this.turmaService.criar(payload);

    requisicao$.subscribe({
      next: () => {
        this.notificacao.sucesso('Turma salva com sucesso.');
        this.router.navigateByUrl('/turmas');
      },
      error: (resposta) => {
        this.salvando.set(false);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível salvar a turma.');
      }
    });
  }

  cancelar(): void {
    this.router.navigateByUrl('/turmas');
  }
}
