import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin } from 'rxjs';

import { Turma } from '../../models/aluno.model';
import { AlertaDisciplinar, Campeonato, Jogo, JogoAtleta, LocalJogo, ROTULOS_MANDO, StatusJogo } from '../../models/competicao.model';
import { AlunoService } from '../../services/aluno.service';
import { PresencaService } from '../../services/presenca.service';
import { FALTAS_SEGUIDAS_PARA_ALERTA, FrequenciaAluno } from '../../models/presenca.model';
import { CampeonatoService } from '../../services/campeonato.service';
import { JogoService } from '../../services/jogo.service';
import { NotificacaoService } from '../../services/notificacao.service';
import { TurmaService } from '../../services/turma.service';
import { CalendarioComponent } from '../../shared/calendario/calendario.component';
import { formatarDataAbsoluta, hojeIso } from '../../shared/data-utils';
import { SeletorHorarioComponent } from '../../shared/seletor-horario/seletor-horario.component';

type Aba = 'dados' | 'convocacao';

interface LinhaConvocacao {
  alunoId: string;
  nome: string;
  convocado: boolean;
  titular: boolean;
  gols: number;
  amarelos: number;
  vermelho: boolean;
}

@Component({
  selector: 'app-jogo-formulario',
  imports: [FormsModule, CalendarioComponent, SeletorHorarioComponent],
  templateUrl: './jogo-formulario.component.html',
  styleUrl: './jogo-formulario.component.scss'
})
export class JogoFormularioComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly jogoService = inject(JogoService);
  private readonly turmaService = inject(TurmaService);
  private readonly campeonatoService = inject(CampeonatoService);
  private readonly alunoService = inject(AlunoService);
  private readonly presencaService = inject(PresencaService);
  private readonly notificacao = inject(NotificacaoService);

  private jogoId: string | null = null;

  readonly carregando = signal(true);
  readonly salvando = signal(false);
  readonly salvandoConvocacao = signal(false);
  readonly abaAtiva = signal<Aba>('dados');
  readonly calendarioAberto = signal(false);

  /** Jogo como está gravado — define o que a súmula aceita (gols/cartões só em jogo realizado). */
  readonly jogo = signal<Jogo | null>(null);
  readonly turmas = signal<Turma[]>([]);
  readonly campeonatos = signal<Campeonato[]>([]);

  campeonatoId = '';
  turmaId = '';
  adversario = '';
  data = '';
  hora = '09:00';
  local = '';
  mando: LocalJogo = 'Casa';
  status: StatusJogo = 'Agendado';
  golsPro: number | null = null;
  golsContra: number | null = null;
  observacao = '';

  linhas: LinhaConvocacao[] = [];

  protected readonly formatarDataAbsoluta = formatarDataAbsoluta;
  protected readonly hojeIso = hojeIso;
  protected readonly mandos = Object.entries(ROTULOS_MANDO).map(([valor, rotulo]) => ({ valor: valor as LocalJogo, rotulo }));

  get ehEdicao(): boolean {
    return !!this.jogoId;
  }

  get titulo(): string {
    return this.ehEdicao ? 'Jogo' : 'Novo jogo';
  }

  get cancelado(): boolean {
    return this.jogo()?.status === 'Cancelado';
  }

  get sumulaLiberada(): boolean {
    return this.jogo()?.status === 'Realizado';
  }

  /** Turmas e campeonatos inativos só aparecem se for o que o jogo já usa. */
  get turmasDisponiveis(): Turma[] {
    return this.turmas().filter((t) => t.ativa || t.id === this.jogo()?.turmaId);
  }

  get campeonatosDisponiveis(): Campeonato[] {
    return this.campeonatos().filter((c) => c.ativo || c.id === this.jogo()?.campeonatoId);
  }

  /** Frequência nos treinos (30 dias) dos atletas da turma — ajuda o técnico a decidir a convocação. Só avisa. */
  private frequencias = new Map<string, FrequenciaAluno>();
  protected readonly alertaFaltas = FALTAS_SEGUIDAS_PARA_ALERTA;

  frequenciaDe(alunoId: string): FrequenciaAluno | undefined {
    return this.frequencias.get(alunoId);
  }

  alertaDe(alunoId: string): AlertaDisciplinar | undefined {
    return this.jogo()?.alertas?.find((a) => a.alunoId === alunoId);
  }

  get totalConvocados(): number {
    return this.linhas.filter((l) => l.convocado).length;
  }

  get totalTitulares(): number {
    return this.linhas.filter((l) => l.convocado && l.titular).length;
  }

  get totalGolsAtletas(): number {
    return this.linhas.filter((l) => l.convocado).reduce((soma, l) => soma + (Number(l.gols) || 0), 0);
  }

  ngOnInit(): void {
    this.jogoId = this.route.snapshot.paramMap.get('id');

    forkJoin({ turmas: this.turmaService.listar(), campeonatos: this.campeonatoService.listar() }).subscribe({
      next: ({ turmas, campeonatos }) => {
        this.turmas.set(turmas);
        this.campeonatos.set(campeonatos);

        if (!this.jogoId) {
          const primeira = turmas.find((t) => t.ativa);
          if (primeira) this.turmaId = primeira.id;
          this.data = hojeIso();
          this.carregando.set(false);
          return;
        }

        this.carregarJogo(this.jogoId);
      },
      error: () => {
        this.carregando.set(false);
        this.notificacao.erro('Não foi possível carregar os dados do formulário.');
      }
    });
  }

  private carregarJogo(id: string): void {
    this.jogoService.obterPorId(id).subscribe({
      next: (jogo) => {
        this.aplicarJogo(jogo);
        this.carregarCandidatos(jogo);
        this.carregando.set(false);
      },
      error: () => {
        this.carregando.set(false);
        this.notificacao.erro('Não foi possível carregar o jogo.');
      }
    });
  }

  private aplicarJogo(jogo: Jogo): void {
    this.jogo.set(jogo);
    this.campeonatoId = jogo.campeonatoId ?? '';
    this.turmaId = jogo.turmaId;
    this.adversario = jogo.adversario;
    this.data = jogo.data.slice(0, 10);
    this.hora = jogo.hora.slice(0, 5);
    this.local = jogo.local ?? '';
    this.mando = jogo.mando;
    this.status = jogo.status === 'Cancelado' ? 'Agendado' : jogo.status;
    this.golsPro = jogo.golsPro;
    this.golsContra = jogo.golsContra;
    this.observacao = jogo.observacao ?? '';
  }

  /** Candidatos = atletas ativos da turma do jogo + quem já está convocado (mesmo de outra turma, ex.: jogando "por cima"). */
  private carregarCandidatos(jogo: Jogo): void {
    this.presencaService.frequenciaDaTurma(jogo.turmaId).subscribe({
      next: (lista) => (this.frequencias = new Map(lista.map((f) => [f.alunoId, f]))),
      error: () => undefined
    });
    this.alunoService.listarAlunos(jogo.turmaId).subscribe({
      next: (alunos) => {
        const convocados = new Map((jogo.convocados ?? []).map((c) => [c.alunoId, c]));
        const linhas: LinhaConvocacao[] = alunos
          .filter((a) => a.ativo || convocados.has(a.id))
          .map((a) => this.criarLinha(a.id, a.nome, convocados.get(a.id)));

        for (const c of convocados.values()) {
          if (!linhas.some((l) => l.alunoId === c.alunoId)) linhas.push(this.criarLinha(c.alunoId, c.alunoNome, c));
        }
        this.linhas = linhas.sort((a, b) => Number(b.convocado) - Number(a.convocado) || a.nome.localeCompare(b.nome, 'pt-BR'));
      },
      error: () => this.notificacao.erro('Não foi possível carregar os atletas da turma.')
    });
  }

  private criarLinha(alunoId: string, nome: string, convocado?: JogoAtleta): LinhaConvocacao {
    return {
      alunoId,
      nome,
      convocado: !!convocado,
      titular: convocado?.titular ?? false,
      gols: convocado?.gols ?? 0,
      amarelos: convocado?.cartoesAmarelos ?? 0,
      vermelho: convocado?.cartaoVermelho ?? false
    };
  }

  selecionarData(dataIso: string): void {
    this.data = dataIso;
    this.calendarioAberto.set(false);
  }

  salvar(): void {
    if (!this.adversario.trim()) {
      this.notificacao.erro('Informe o adversário.');
      return;
    }
    if (!this.turmaId) {
      this.notificacao.erro('Selecione a turma.');
      return;
    }
    if (!this.data) {
      this.notificacao.erro('Informe a data do jogo.');
      return;
    }
    if (this.status === 'Realizado') {
      const valido = (n: number | null) => n !== null && Number.isInteger(Number(n)) && Number(n) >= 0 && Number(n) <= 99;
      if (!valido(this.golsPro) || !valido(this.golsContra)) {
        this.notificacao.erro('Informe o placar (gols de 0 a 99 para cada lado) de um jogo realizado.');
        return;
      }
    }

    const realizado = this.status === 'Realizado';
    const payload = {
      campeonatoId: this.campeonatoId || null,
      turmaId: this.turmaId,
      adversario: this.adversario.trim(),
      data: this.data,
      hora: this.hora,
      local: this.local.trim() || null,
      mando: this.mando,
      status: this.status,
      golsPro: realizado ? Number(this.golsPro) : null,
      golsContra: realizado ? Number(this.golsContra) : null,
      observacao: this.observacao.trim() || null
    };

    this.salvando.set(true);
    const requisicao$ = this.jogoId ? this.jogoService.editar(this.jogoId, payload) : this.jogoService.criar(payload);

    requisicao$.subscribe({
      next: (salvo) => {
        this.salvando.set(false);
        if (!this.jogoId) {
          this.notificacao.sucesso('Jogo salvo. Agora você pode montar a convocação.');
          this.router.navigate(['/jogos', salvo.id, 'editar']);
          return;
        }
        this.notificacao.sucesso('Jogo salvo com sucesso.');
        this.aplicarJogo(salvo);
      },
      error: (resposta) => {
        this.salvando.set(false);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível salvar o jogo.');
      }
    });
  }

  convocarTodos(): void {
    this.linhas.forEach((l) => (l.convocado = true));
  }

  limparConvocacao(): void {
    this.linhas.forEach((l) => {
      l.convocado = false;
      l.titular = false;
      l.gols = 0;
      l.amarelos = 0;
      l.vermelho = false;
    });
  }

  salvarConvocacao(): void {
    if (!this.jogoId) return;

    const sumula = this.sumulaLiberada;
    const convocados = this.linhas
      .filter((l) => l.convocado)
      .map((l) => ({
        alunoId: l.alunoId,
        titular: l.titular,
        gols: sumula ? Number(l.gols) || 0 : 0,
        cartoesAmarelos: sumula ? Number(l.amarelos) || 0 : 0,
        cartaoVermelho: sumula ? l.vermelho : false
      }));

    this.salvandoConvocacao.set(true);
    this.jogoService.salvarConvocacao(this.jogoId, convocados).subscribe({
      next: (jogo) => {
        this.salvandoConvocacao.set(false);
        this.jogo.set(jogo);
        this.carregarCandidatos(jogo);
        this.notificacao.sucesso('Convocação salva com sucesso.');
      },
      error: (resposta) => {
        this.salvandoConvocacao.set(false);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível salvar a convocação.');
      }
    });
  }

  voltar(): void {
    this.router.navigateByUrl('/jogos');
  }
}
