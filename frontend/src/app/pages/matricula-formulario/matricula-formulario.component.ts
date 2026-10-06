import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { ConviteMatricula, POSICOES_ATLETA, PosicaoAtleta, ResponsavelResumo, SenhaGeradaResponsavel, Turma } from '../../models/aluno.model';
import { DIAS_AVISO_ATESTADO, rotuloAtestado, situacaoAtestado } from '../../models/atestado.model';
import { FichaSaude } from '../../models/ficha-saude.model';
import { TermoAceite } from '../../models/termo.model';
import { AlunoService } from '../../services/aluno.service';
import { FichaSaudeService } from '../../services/ficha-saude.service';
import { NotificacaoService } from '../../services/notificacao.service';
import { ResponsavelService } from '../../services/responsavel.service';
import { SegmentoService } from '../../services/segmento.service';
import { TermoService } from '../../services/termo.service';
import { TurmaService } from '../../services/turma.service';
import { UploadService } from '../../services/upload.service';
import { CalendarioComponent } from '../../shared/calendario/calendario.component';
import { formatarDataAbsoluta, formatarDataHoraAbsoluta, hojeIso } from '../../shared/data-utils';
import { DocumentosSaudeComponent } from '../../shared/documentos-saude/documentos-saude.component';
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
  imports: [FormsModule, SeletorArquivoComponent, CalendarioComponent, LogsModalComponent, DocumentosSaudeComponent],
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
  private readonly termoService = inject(TermoService);
  private readonly notificacao = inject(NotificacaoService);
  protected readonly segmentoService = inject(SegmentoService);

  protected alunoId: string | null = null;

  readonly turmas = signal<Turma[]>([]);
  readonly carregando = signal(true);
  readonly salvando = signal(false);
  readonly enviandoFoto = signal(false);
  readonly fotoUrl = signal<string | null>(null);
  readonly responsaveis = signal<ResponsavelResumo[]>([novoResponsavelVazio()]);
  // Posições dos cartões de responsável abertos (só vale com 2+): o primeiro nasce aberto, os demais minimizados.
  readonly responsaveisAbertos = signal<Set<number>>(new Set([0]));
  readonly calendarioNascimentoAberto = signal(false);
  /** Resultado do e-mail da matrícula (mostrado uma vez, depois de salvar). */
  readonly convites = signal<ConviteMatricula[]>([]);
  readonly matriculaPendente = signal(false);
  readonly matriculaConfirmadaEm = signal<string | null>(null);
  readonly aceites = signal<TermoAceite[]>([]);
  readonly aceiteAbertoId = signal<string | null>(null);
  readonly reenviandoId = signal<string | null>(null);
  /** Convite reenviado que não saiu por e-mail: o link aparece (uma vez) junto do responsável. */
  readonly linkReenviado = signal<ConviteMatricula | null>(null);
  readonly exportando = signal(false);
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
  posicao: PosicaoAtleta | '' = '';
  descontoMensalidade: number | null = null;
  motivoDesconto = '';

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
  atestadoValidoAte = '';
  readonly calendarioAtestadoAberto = signal(false);

  protected readonly resolverFotoUrl = resolverFotoUrl;
  protected readonly formatarDataAbsoluta = formatarDataAbsoluta;
  protected readonly formatarDataHoraAbsoluta = formatarDataHoraAbsoluta;
  protected readonly hojeIso = hojeIso;
  protected readonly tiposSanguineos = TIPOS_SANGUINEOS;
  protected readonly posicoes = POSICOES_ATLETA;
  protected readonly rotuloAtestado = rotuloAtestado;
  protected readonly situacaoAtestado = situacaoAtestado;
  protected readonly diasAvisoAtestado = DIAS_AVISO_ATESTADO;

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
          this.posicao = aluno.posicao ?? '';
          this.descontoMensalidade = aluno.descontoMensalidadePercentual || null;
          this.motivoDesconto = aluno.motivoDesconto ?? '';
          this.fotoUrl.set(aluno.fotoUrl);
          this.responsaveis.set(aluno.responsaveis.length > 0 ? aluno.responsaveis : [novoResponsavelVazio()]);
          this.matriculaPendente.set(!!aluno.matriculaPendente);
          this.matriculaConfirmadaEm.set(aluno.matriculaConfirmadaEm ?? null);
          this.carregando.set(false);
        },
        error: () => {
          this.notificacao.erro('Não foi possível carregar o aluno.');
          this.carregando.set(false);
        }
      });
      this.carregarFicha(this.alunoId);
      this.termoService.aceitesDoAluno(this.alunoId).subscribe({ next: (a) => this.aceites.set(a), error: () => undefined });
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
        this.atestadoValidoAte = ficha.atestadoValidoAte ?? '';
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
      atestadoValidoAte: this.atestadoValidoAte || null,
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

  selecionarAtestado(dataIso: string): void {
    this.atestadoValidoAte = dataIso;
    this.calendarioAtestadoAberto.set(false);
  }

  limparAtestado(): void {
    this.atestadoValidoAte = '';
    this.calendarioAtestadoAberto.set(false);
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

  /** Com um responsável só não há o que recolher; com mais de um, cada cartão tem seu toggle. */
  responsavelAberto(indice: number): boolean {
    return this.responsaveis().length <= 1 || this.responsaveisAbertos().has(indice);
  }

  alternarResponsavel(indice: number): void {
    this.responsaveisAbertos.update((atual) => {
      const proximo = new Set(atual);
      if (proximo.has(indice)) proximo.delete(indice);
      else proximo.add(indice);
      return proximo;
    });
  }

  adicionarResponsavel(): void {
    const novoIndice = this.responsaveis().length;
    this.responsaveis.update((atual) => [...atual, novoResponsavelVazio()]);
    this.responsaveisAbertos.update((atual) => new Set(atual).add(novoIndice));
  }

  removerResponsavel(indice: number): void {
    if (this.responsaveis().length <= 1) return;

    this.responsaveis.update((atual) => atual.filter((_, i) => i !== indice));
    // O estado aberto/fechado é por posição: quem estava depois do removido desce uma posição.
    this.responsaveisAbertos.update(
      (atual) => new Set([...atual].filter((i) => i !== indice).map((i) => (i > indice ? i - 1 : i)))
    );
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

  reenviarConvite(responsavel: ResponsavelResumo): void {
    if (!responsavel.id || !this.alunoId) return;

    this.linkReenviado.set(null);
    this.reenviandoId.set(responsavel.id);
    this.alunoService.reenviarConvite(this.alunoId, responsavel.id).subscribe({
      next: (resultado) => {
        this.reenviandoId.set(null);
        if (resultado.entregue) {
          this.notificacao.sucesso(`E-mail da matrícula enviado para ${resultado.email}.`);
        } else {
          this.linkReenviado.set(resultado);
          this.notificacao.erro(resultado.aviso ?? 'O e-mail não foi entregue.');
        }
      },
      error: (resposta) => {
        this.reenviandoId.set(null);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível reenviar o e-mail.');
      }
    });
  }

  alternarAceite(id: string): void {
    this.aceiteAbertoId.set(this.aceiteAbertoId() === id ? null : id);
  }

  exportarDados(): void {
    if (!this.alunoId) return;

    this.exportando.set(true);
    this.alunoService.exportarDados(this.alunoId).subscribe({
      next: (blob) => {
        this.exportando.set(false);
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `dados-${this.nome.trim().replace(/[^\p{L}\p{N}]+/gu, '-')}-${hojeIso()}.zip`;
        link.click();
        URL.revokeObjectURL(url);
        this.notificacao.sucesso('Arquivo gerado. A exportação ficou registrada no histórico.');
      },
      error: () => {
        this.exportando.set(false);
        this.notificacao.erro('Não foi possível exportar os dados.');
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
      // Leva o usuário até o problema: aba dos responsáveis com os cartões incompletos abertos.
      this.abaAtiva.set('responsaveis');
      this.responsaveisAbertos.set(
        new Set(this.responsaveis().flatMap((r, i) => (r.nome.trim() && r.email.trim() ? [] : [i])))
      );
      this.notificacao.erro('Informe ao menos um responsável, com nome e e-mail.');
      return;
    }

    if (this.descontoMensalidade !== null && (this.descontoMensalidade < 0 || this.descontoMensalidade > 100)) {
      this.notificacao.erro('O desconto na mensalidade deve estar entre 0% e 100%.');
      return;
    }

    const payload = {
      nome: this.nome.trim(),
      dataNascimento: this.dataNascimento,
      turmaId: this.turmaId,
      fotoUrl: this.fotoUrl(),
      responsaveis: responsaveisValidos,
      posicao: this.posicao || null,
      descontoMensalidadePercentual: this.descontoMensalidade ?? 0,
      motivoDesconto: this.motivoDesconto.trim() || null
    };

    this.salvando.set(true);
    const requisicao$ = this.alunoId
      ? this.alunoService.editar(this.alunoId, payload)
      : this.alunoService.criar(payload);

    requisicao$.subscribe({
      next: (resultado) => {
        if (resultado.convites?.length > 0) {
          this.convites.set(resultado.convites);
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
