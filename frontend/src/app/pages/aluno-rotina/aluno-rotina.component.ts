import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { Aluno } from '../../models/aluno.model';
import {
  CategoriaRegistro,
  Humor,
  Refeicao,
  RegistroRotina,
  StatusAlimentacao,
  TipoHigiene
} from '../../models/registro-rotina.model';
import { AlunoService } from '../../services/aluno.service';
import { RotinaService } from '../../services/rotina.service';
import { SessaoService } from '../../services/sessao.service';

type AcaoRapida = CategoriaRegistro | null;

@Component({
  selector: 'app-aluno-rotina',
  imports: [FormsModule],
  templateUrl: './aluno-rotina.component.html',
  styleUrl: './aluno-rotina.component.scss'
})
export class AlunoRotinaComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly alunoService = inject(AlunoService);
  private readonly rotinaService = inject(RotinaService);
  private readonly sessao = inject(SessaoService);

  private alunoId = '';

  readonly aluno = signal<Aluno | null>(null);
  readonly registros = signal<RegistroRotina[]>([]);
  readonly carregando = signal(true);
  readonly enviando = signal(false);

  readonly acaoAtiva = signal<AcaoRapida>(null);

  // campos do formulário rápido — só os relevantes para a ação ativa são usados
  observacao = '';
  refeicao: Refeicao = 'Almoco';
  statusAlimentacao: StatusAlimentacao = 'ComeuTudo';
  horaInicioSono = this.horaAtual();
  tipoHigiene: TipoHigiene = 'TrocaFralda';
  humor: Humor = 'Feliz';

  ngOnInit(): void {
    if (!this.sessao.educadorId()) {
      this.router.navigateByUrl('/entrar');
      return;
    }

    this.alunoId = this.route.snapshot.paramMap.get('id') ?? '';
    if (!this.alunoId) {
      this.router.navigateByUrl('/alunos');
      return;
    }

    this.alunoService.obterAluno(this.alunoId).subscribe((aluno) => this.aluno.set(aluno));
    this.carregarTimeline();
  }

  private carregarTimeline(): void {
    this.rotinaService.listarDoDia(this.alunoId).subscribe({
      next: (registros) => {
        this.registros.set(registros);
        this.carregando.set(false);
      },
      error: () => this.carregando.set(false)
    });
  }

  abrirAcao(acao: CategoriaRegistro): void {
    this.acaoAtiva.set(this.acaoAtiva() === acao ? null : acao);
    this.observacao = '';
    this.horaInicioSono = this.horaAtual();
  }

  fecharAcao(): void {
    this.acaoAtiva.set(null);
  }

  voltar(): void {
    this.router.navigateByUrl('/alunos');
  }

  confirmar(): void {
    const usuarioId = this.sessao.educadorId();
    const acao = this.acaoAtiva();
    if (!usuarioId || !acao) return;

    this.enviando.set(true);
    const base = { usuarioId, observacao: this.observacao || null };

    const requisicao$ = (() => {
      switch (acao) {
        case 'Alimentacao':
          return this.rotinaService.registrarAlimentacao(this.alunoId, {
            ...base,
            refeicao: this.refeicao,
            status: this.statusAlimentacao
          });
        case 'Sono':
          return this.rotinaService.registrarSono(this.alunoId, {
            ...base,
            horaInicio: this.horaInicioSono
          });
        case 'Higiene':
          return this.rotinaService.registrarHigiene(this.alunoId, {
            ...base,
            tipo: this.tipoHigiene
          });
        case 'Humor':
          return this.rotinaService.registrarHumor(this.alunoId, {
            ...base,
            humor: this.humor
          });
        case 'Momento':
          return this.rotinaService.registrarMomento(this.alunoId, base);
      }
    })();

    requisicao$.subscribe({
      next: (registro) => {
        this.registros.update((atual) => [registro, ...atual]);
        this.enviando.set(false);
        this.fecharAcao();
      },
      error: () => this.enviando.set(false)
    });
  }

  rotuloCategoria(registro: RegistroRotina): string {
    switch (registro.categoria) {
      case 'Alimentacao':
        return `${this.rotuloRefeicao(registro.refeicao)} — ${this.rotuloStatusAlimentacao(registro.statusAlimentacao)}`;
      case 'Sono':
        return registro.horaFim ? `Dormiu ${registro.horaInicio}–${registro.horaFim}` : `Dormiu às ${registro.horaInicio}`;
      case 'Higiene':
        return registro.tipoHigiene === 'TrocaFralda' ? 'Troca de fralda' : 'Foi ao banheiro';
      case 'Humor':
        return this.rotuloHumor(registro.humor);
      case 'Momento':
        return 'Momento registrado';
    }
  }

  iconeCategoria(categoria: CategoriaRegistro): string {
    return { Alimentacao: '🍽️', Sono: '😴', Higiene: '🧷', Humor: '🙂', Momento: '📷' }[categoria];
  }

  horaRegistro(registradoEm: string): string {
    return new Date(registradoEm).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });
  }

  private rotuloRefeicao(v: Refeicao | null): string {
    return { Cafe: 'Café', Almoco: 'Almoço', Lanche: 'Lanche' }[v ?? 'Lanche'];
  }

  private rotuloStatusAlimentacao(v: StatusAlimentacao | null): string {
    return { ComeuTudo: 'comeu tudo', Parcial: 'comeu parte', Recusou: 'recusou' }[v ?? 'Parcial'];
  }

  private rotuloHumor(v: Humor | null): string {
    return { Feliz: 'Feliz 😊', Agitado: 'Agitado 🙃', Sonolento: 'Sonolento 😪', Choroso: 'Choroso 😢' }[v ?? 'Feliz'];
  }

  private horaAtual(): string {
    const agora = new Date();
    return `${String(agora.getHours()).padStart(2, '0')}:${String(agora.getMinutes()).padStart(2, '0')}`;
  }
}
