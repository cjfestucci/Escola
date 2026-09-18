import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { AuthService } from '../../services/auth.service';
import { UsuarioService } from '../../services/usuario.service';
import { CriarOuEditarUsuario, PapelEquipe, SenhaGerada, UsuarioConta } from '../../models/usuario.model';

const ROTULO_PAPEL: Record<PapelEquipe, string> = {
  Admin: 'Admin',
  Coordenador: 'Coordenador',
  Educador: 'Professor',
  Financeiro: 'Financeiro'
};

@Component({
  selector: 'app-usuarios-lista',
  imports: [FormsModule],
  templateUrl: './usuarios-lista.component.html',
  styleUrl: './usuarios-lista.component.scss'
})
export class UsuariosListaComponent implements OnInit {
  private readonly usuarioService = inject(UsuarioService);
  private readonly auth = inject(AuthService);

  readonly contas = signal<UsuarioConta[]>([]);
  readonly carregando = signal(true);
  readonly erro = signal<string | null>(null);
  readonly salvando = signal(false);

  readonly formularioAberto = signal(false);
  readonly contaEmEdicaoId = signal<string | null>(null);
  readonly senhaGerada = signal<SenhaGerada | null>(null);

  readonly confirmandoExclusaoId = signal<string | null>(null);
  readonly excluindoId = signal<string | null>(null);
  readonly redefinindoId = signal<string | null>(null);

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
      error: () => this.carregando.set(false)
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
    this.erro.set(null);

    if (!this.nome.trim() || !this.email.trim()) {
      this.erro.set('Nome e e-mail são obrigatórios.');
      return;
    }

    const payload: CriarOuEditarUsuario = { nome: this.nome.trim(), email: this.email.trim(), papel: this.papel };
    const id = this.contaEmEdicaoId();

    this.salvando.set(true);
    const aoErro = (resposta: { error?: unknown }) => {
      this.salvando.set(false);
      this.erro.set(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível salvar a conta.');
    };

    if (id) {
      this.usuarioService.editarConta(id, payload).subscribe({
        next: () => {
          this.salvando.set(false);
          this.formularioAberto.set(false);
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
    this.erro.set(null);
    this.redefinindoId.set(conta.id);
    this.usuarioService.redefinirSenha(conta.id).subscribe({
      next: (resultado) => {
        this.redefinindoId.set(null);
        this.senhaGerada.set(resultado);
      },
      error: () => {
        this.redefinindoId.set(null);
        this.erro.set('Não foi possível redefinir a senha.');
      }
    });
  }

  pedirConfirmacaoExclusao(contaId: string): void {
    this.erro.set(null);
    this.confirmandoExclusaoId.set(contaId);
  }

  cancelarExclusao(): void {
    this.confirmandoExclusaoId.set(null);
  }

  confirmarExclusao(conta: UsuarioConta): void {
    this.excluindoId.set(conta.id);
    this.usuarioService.excluirConta(conta.id).subscribe({
      next: () => {
        this.contas.update((atual) => atual.filter((c) => c.id !== conta.id));
        this.excluindoId.set(null);
        this.confirmandoExclusaoId.set(null);
      },
      error: (resposta) => {
        this.excluindoId.set(null);
        this.confirmandoExclusaoId.set(null);
        this.erro.set(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível excluir a conta.');
      }
    });
  }
}
