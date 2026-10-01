import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { ResponsavelResumo, SenhaGeradaResponsavel, Turma } from '../../models/aluno.model';
import { FichaSaude } from '../../models/ficha-saude.model';
import { AlunoService } from '../../services/aluno.service';
import { FichaSaudeService } from '../../services/ficha-saude.service';
import { NotificacaoService } from '../../services/notificacao.service';
import { ResponsavelService } from '../../services/responsavel.service';
import { SegmentoService } from '../../services/segmento.service';
import { TurmaService } from '../../services/turma.service';
import { UploadService } from '../../services/upload.service';
import { CalendarioComponent } from '../../shared/calendario/calendario.component';
import { formatarDataAbsoluta, hojeIso } from '../../shared/data-utils';
import { LogsModalComponent } from '../../shared/logs-modal/logs-modal.component';
import { resolverFotoUrl } from '../../shared/registro-rotina-display';
import { SeletorArquivoComponent } from '../../shared/seletor-arquivo/seletor-arquivo.component';

function novoResponsavelVazio(): ResponsavelResumo {
  return { id: null, nome: '', email: '', telefone: null, responsavelFinanceiro: false };
}

const TIPOS_SANGUINEOS = ['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', 'O+', 'O-'];

type Aba = 'dados' | 'responsaveis' | 'saude';

@Component({
  selector: 'app-matricula-formulario',
  imports: [FormsModule, SeletorArquivoComponent, CalendarioComponent, LogsModalComponent],
  templateUrl: './matricula-formulario.component.html',
  styleUrl: './matricula-formulario.component.scss'
})
export class MatriculaFormularioComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly alunoService = inject(AlunoService);
  private readonly turmaService = inject(TurmaService);
  private readonly uploadService = inject(UploadService);
  private readonly fichaSaudeService = inject(FichaSaudeService);
  private readonly responsavelService = inject(ResponsavelService);
  private readonly notificacao = inject(NotificacaoService);
  protected readonly segmentoService = inject(SegmentoService);

  private alunoId: string | null = null;

  readonly turmas = signal<Turma[]>([]);
  readonly carregando = signal(true);
  readonly salvando = signal(false);
  readonly enviandoFoto = signal(false);
  readonly fotoUrl = signal<string | null>(null);
  readonly responsaveis = signal<ResponsavelResumo[]>([novoResponsavelVazio()]);
  readonly calendarioNascimentoAberto = signal(false);
  readonly senhasGeradas = signal<SenhaGeradaResponsavel[]>([]);
  readonly redefinindoSenhaId = signal<string | null>(null);
  readonly senhaResponsavelGerada = signal<SenhaGeradaResponsavel | null>(null);
  readonly abaAtiva = signal<Aba>('dados');

  readonly carregandoFicha = signal(false);
  readonly salvandoFicha = signal(false);
  readonly fichaSaudeId = signal<string | null>(null);
  readonly historicoFichaAberto = signal(false);

  nome = '';
  dataNascimento = '';
  turmaId = '';

  tipoSanguineo = '';
  alergias = '';
  restricoesAlimentares = '';
  medicamentosEmUso = '';
  condicoesSaude = '';
  planoSaude = '';
  pediatraNome = '';
  pediatraTelefone = '';
  contatoEmergenciaNome = '';
  contatoEmergenciaTelefone = '';
  vacinacaoEmDia = false;
  autorizaUsoImagem = false;

  protected readonly resolverFotoUrl = resolverFotoUrl;
  protected readonly formatarDataAbsoluta = formatarDataAbsoluta;
  protected readonly hojeIso = hojeIso;
  protected readonly tiposSanguineos = TIPOS_SANGUINEOS;

  get titulo(): string {
    const pessoa = this.segmentoService.rotuloPessoa();
    return this.alunoId ? `Editar ${pessoa.toLowerCase()}` : `Novo(a) ${pessoa.toLowerCase()}`;
  }

  get ehEdicao(): boolean {
    return !!this.alunoId;
  }

  ngOnInit(): void {
    this.alunoId = this.route.snapshot.paramMap.get('id');

    this.turmaService.listar().subscribe((turmas) => this.turmas.set(turmas));

    if (this.alunoId) {
      this.alunoService.obterAluno(this.alunoId).subscribe({
        next: (aluno) => {
          this.nome = aluno.nome;
          this.dataNascimento = aluno.dataNascimento.slice(0, 10);
          this.turmaId = aluno.turmaId;
          this.fotoUrl.set(aluno.fotoUrl);
          this.responsaveis.set(aluno.responsaveis.length > 0 ? aluno.responsaveis : [novoResponsavelVazio()]);
          this.carregando.set(false);
        },
        error: () => {
          this.notificacao.erro('Não foi possível carregar o aluno.');
          this.carregando.set(false);
        }
      });
      this.carregarFicha(this.alunoId);
    } else {
      this.carregando.set(false);
    }
  }

  private carregarFicha(alunoId: string): void {
    this.carregandoFicha.set(true);
    this.fichaSaudeService.obter(alunoId).subscribe({
      next: (ficha) => {
        this.fichaSaudeId.set(ficha.id);
        this.tipoSanguineo = ficha.tipoSanguineo ?? '';
        this.alergias = ficha.alergias ?? '';
        this.restricoesAlimentares = ficha.restricoesAlimentares ?? '';
        this.medicamentosEmUso = ficha.medicamentosEmUso ?? '';
        this.condicoesSaude = ficha.condicoesSaude ?? '';
        this.planoSaude = ficha.planoSaude ?? '';
        this.pediatraNome = ficha.pediatraNome ?? '';
        this.pediatraTelefone = ficha.pediatraTelefone ?? '';
        this.contatoEmergenciaNome = ficha.contatoEmergenciaNome ?? '';
        this.contatoEmergenciaTelefone = ficha.contatoEmergenciaTelefone ?? '';
        this.vacinacaoEmDia = ficha.vacinacaoEmDia;
        this.autorizaUsoImagem = ficha.autorizaUsoImagem;
        this.carregandoFicha.set(false);
      },
      error: () => {
        this.carregandoFicha.set(false);
        this.notificacao.erro('Não foi possível carregar a ficha de saúde.');
      }
    });
  }

  abrirHistoricoFicha(): void {
    this.historicoFichaAberto.set(true);
  }

  fecharHistoricoFicha(): void {
    this.historicoFichaAberto.set(false);
  }

  salvarFicha(): void {
    if (!this.alunoId) return;

    const payload: FichaSaude = {
      id: null,
      tipoSanguineo: this.tipoSanguineo || null,
      alergias: this.alergias || null,
      restricoesAlimentares: this.restricoesAlimentares || null,
      medicamentosEmUso: this.medicamentosEmUso || null,
      condicoesSaude: this.condicoesSaude || null,
      planoSaude: this.planoSaude || null,
      pediatraNome: this.pediatraNome || null,
      pediatraTelefone: this.pediatraTelefone || null,
      contatoEmergenciaNome: this.contatoEmergenciaNome || null,
      contatoEmergenciaTelefone: this.contatoEmergenciaTelefone || null,
      vacinacaoEmDia: this.vacinacaoEmDia,
      autorizaUsoImagem: this.autorizaUsoImagem,
      atualizadoEm: null
    };

    this.salvandoFicha.set(true);
    this.fichaSaudeService.salvar(this.alunoId, payload).subscribe({
      next: (ficha) => {
        this.fichaSaudeId.set(ficha.id);
        this.salvandoFicha.set(false);
        this.notificacao.sucesso('Ficha de saúde salva com sucesso.');
      },
      error: () => {
        this.salvandoFicha.set(false);
        this.notificacao.erro('Não foi possível salvar a ficha de saúde.');
      }
    });
  }

  selecionarNascimento(dataIso: string): void {
    this.dataNascimento = dataIso;
    this.calendarioNascimentoAberto.set(false);
  }

  aoSelecionarFoto(arquivo: File): void {
    this.enviandoFoto.set(true);
    this.uploadService.enviarFoto(arquivo).subscribe({
      next: (resultado) => {
        this.fotoUrl.set(resultado.url);
        this.enviandoFoto.set(false);
      },
      error: () => {
        this.enviandoFoto.set(false);
        this.notificacao.erro('Não foi possível enviar a foto.');
      }
    });
  }

  removerFoto(): void {
    this.fotoUrl.set(null);
  }

  adicionarResponsavel(): void {
    this.responsaveis.update((atual) => [...atual, novoResponsavelVazio()]);
  }

  removerResponsavel(indice: number): void {
    this.responsaveis.update((atual) => (atual.length > 1 ? atual.filter((_, i) => i !== indice) : atual));
  }

  atualizarResponsavel(indice: number, campo: keyof ResponsavelResumo, valor: string | boolean): void {
    this.responsaveis.update((atual) =>
      atual.map((r, i) => (i === indice ? { ...r, [campo]: valor } : r))
    );
  }

  redefinirSenhaResponsavel(responsavel: ResponsavelResumo): void {
    if (!responsavel.id) return;

    this.senhaResponsavelGerada.set(null);
    this.redefinindoSenhaId.set(responsavel.id);
    this.responsavelService.redefinirSenha(responsavel.id).subscribe({
      next: (resultado) => {
        this.redefinindoSenhaId.set(null);
        this.senhaResponsavelGerada.set(resultado);
      },
      error: () => {
        this.redefinindoSenhaId.set(null);
        this.notificacao.erro('Não foi possível redefinir a senha desse responsável.');
      }
    });
  }

  salvar(): void {
    if (!this.nome.trim()) {
      this.notificacao.erro('Informe o nome do aluno.');
      return;
    }
    if (!this.dataNascimento) {
      this.notificacao.erro('Informe a data de nascimento.');
      return;
    }
    if (!this.turmaId) {
      this.notificacao.erro('Selecione a turma.');
      return;
    }
    const responsaveisValidos = this.responsaveis().filter((r) => r.nome.trim() && r.email.trim());
    if (responsaveisValidos.length === 0) {
      this.notificacao.erro('Informe ao menos um responsável, com nome e e-mail.');
      return;
    }

    const payload = {
      nome: this.nome.trim(),
      dataNascimento: this.dataNascimento,
      turmaId: this.turmaId,
      fotoUrl: this.fotoUrl(),
      responsaveis: responsaveisValidos
    };

    this.salvando.set(true);
    const requisicao$ = this.alunoId
      ? this.alunoService.editar(this.alunoId, payload)
      : this.alunoService.criar(payload);

    requisicao$.subscribe({
      next: (resultado) => {
        if (resultado.senhasGeradas?.length > 0) {
          this.senhasGeradas.set(resultado.senhasGeradas);
          this.salvando.set(false);
        } else {
          this.notificacao.sucesso(`${this.segmentoService.rotuloPessoa()} salvo(a) com sucesso.`);
          this.router.navigateByUrl('/matricula');
        }
      },
      error: (resposta) => {
        this.salvando.set(false);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível salvar o aluno.');
      }
    });
  }

  continuar(): void {
    this.router.navigateByUrl('/matricula');
  }

  cancelar(): void {
    this.router.navigateByUrl('/matricula');
  }
}
