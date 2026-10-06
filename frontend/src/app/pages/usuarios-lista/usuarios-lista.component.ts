import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { AuthService } from '../../services/auth.service';
import { NotificacaoService } from '../../services/notificacao.service';
import { UsuarioService } from '../../services/usuario.service';
import { CriarOuEditarUsuario, PapelEquipe, SenhaGerada, UsuarioConta } from '../../models/usuario.model';
import { LogsModalComponent } from '../../shared/logs-modal/logs-modal.component';

const ROTULO_PAPEL: Record<PapelEquipe, string> = {
  Admin: 'Admin',
  Coordenador: 'Coordenador',
  Educador: 'Professor',
  Financeiro: 'Financeiro'
};

@Component({
  selector: 'app-usuarios-lista',
  imports: [FormsModule, LogsModalComponent],
  templateUrl: './usuarios-lista.component.html',
  styleUrl: './usuarios-lista.component.scss'
})
export class UsuariosListaComponent implements OnInit {
  private readonly usuarioService = inject(UsuarioService);
  private readonly notificacao = inject(NotificacaoService);
  private readonly auth = inject(AuthService);

  readonly contas = signal<UsuarioConta[]>([]);
  readonly carregando = signal(true);
  readonly salvando = signal(false);

  readonly formularioAberto = signal(false);
  readonly contaEmEdicaoId = signal<string | null>(null);
  readonly senhaGerada = signal<SenhaGerada | null>(null);

  readonly processandoId = signal<string | null>(null);
  readonly redefinindoId = signal<string | null>(null);
  readonly historicoAbertoId = signal<string | null>(null);

  nome = '';
  email = '';
  papel: PapelEquipe = 'Educador';

  protected readonly papeis: PapelEquipe[] = ['Admin', 'Coordenador', 'Educador', 'Financeiro'];
  protected readonly rotuloPapel = (papel: string): string => ROTULO_PAPEL[papel as PapelEquipe] ?? papel;

  get meuId(): string | null {
    return this.auth.identidade()?.usuarioId ?? null;
  }

  ngOnInit(): void {
    this.carregar();
  }

  private carregar(): void {
    this.carregando.set(true);
    this.usuarioService.listarContas().subscribe({
      next: (contas) => {
        this.contas.set(contas);
        this.carregando.set(false);
      },
      error: () => {
        this.carregando.set(false);
        this.notificacao.erro('Não foi possível carregar as contas.');
      }
    });
  }

  abrirNovo(): void {
    this.senhaGerada.set(null);
    this.contaEmEdicaoId.set(null);
    this.nome = '';
    this.email = '';
    this.papel = 'Educador';
    this.formularioAberto.set(true);
  }

  editar(conta: UsuarioConta): void {
    this.senhaGerada.set(null);
    this.contaEmEdicaoId.set(conta.id);
    this.nome = conta.nome;
    this.email = conta.email;
    this.papel = conta.papel;
    this.formularioAberto.set(true);
  }

  cancelar(): void {
    this.formularioAberto.set(false);
  }

  salvar(): void {
    if (!this.nome.trim() || !this.email.trim()) {
      this.notificacao.erro('Nome e e-mail são obrigatórios.');
      return;
    }

    const payload: CriarOuEditarUsuario = { nome: this.nome.trim(), email: this.email.trim(), papel: this.papel };
    const id = this.contaEmEdicaoId();

    this.salvando.set(true);
    const aoErro = (resposta: { error?: unknown }) => {
      this.salvando.set(false);
      this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível salvar a conta.');
    };

    if (id) {
      this.usuarioService.editarConta(id, payload).subscribe({
        next: () => {
          this.salvando.set(false);
          this.formularioAberto.set(false);
          this.notificacao.sucesso('Conta salva com sucesso.');
          this.carregar();
        },
        error: aoErro
      });
    } else {
      this.usuarioService.criarConta(payload).subscribe({
        next: (resultado) => {
          this.salvando.set(false);
          this.formularioAberto.set(false);
          this.senhaGerada.set(resultado);
          this.carregar();
        },
        error: aoErro
      });
    }
  }

  redefinirSenha(conta: UsuarioConta): void {
    this.redefinindoId.set(conta.id);
    this.usuarioService.redefinirSenha(conta.id).subscribe({
      next: (resultado) => {
        this.redefinindoId.set(null);
        this.senhaGerada.set(resultado);
      },
      error: () => {
        this.redefinindoId.set(null);
        this.notificacao.erro('Não foi possível redefinir a senha.');
      }
    });
  }

  encerrarSessoes(conta: UsuarioConta): void {
    this.processandoId.set(conta.id);
    this.usuarioService.encerrarSessoes(conta.id).subscribe({
      next: () => {
        this.processandoId.set(null);
        this.notificacao.sucesso(`Sessões de ${conta.nome} encerradas. A pessoa precisa entrar de novo.`);
      },
      error: (resposta) => {
        this.processandoId.set(null);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível encerrar as sessões.');
      }
    });
  }

  abrirHistorico(contaId: string): void {
    this.historicoAbertoId.set(contaId);
  }

  fecharHistorico(): void {
    this.historicoAbertoId.set(null);
  }

  alternarStatus(conta: UsuarioConta): void {
    this.processandoId.set(conta.id);
    const requisicao$ = conta.ativo
      ? this.usuarioService.desativarConta(conta.id)
      : this.usuarioService.ativarConta(conta.id);

    requisicao$.subscribe({
      next: (atualizada) => {
        this.contas.update((atual) => atual.map((c) => (c.id === atualizada.id ? atualizada : c)));
        this.processandoId.set(null);
        this.notificacao.sucesso(atualizada.ativo ? 'Conta reativada.' : 'Conta desativada.');
      },
      error: (resposta) => {
        this.processandoId.set(null);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível atualizar o status da conta.');
      }
    });
  }
}
