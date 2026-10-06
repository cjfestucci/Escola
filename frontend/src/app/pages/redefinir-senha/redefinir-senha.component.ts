import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { AuthService } from '../../services/auth.service';
import { NotificacaoService } from '../../services/notificacao.service';
import { SegmentoService } from '../../services/segmento.service';

/** Mesmo mínimo exigido pelo backend (a regra de verdade é a de lá). */
const TAMANHO_MINIMO_SENHA = 8;

/** Define a nova senha a partir do link recebido por e-mail (`/redefinir-senha?token=…`). */
@Component({
  selector: 'app-redefinir-senha',
  imports: [FormsModule],
  templateUrl: './redefinir-senha.component.html',
  styleUrl: './redefinir-senha.component.scss'
})
export class RedefinirSenhaComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly notificacao = inject(NotificacaoService);
  protected readonly segmentoService = inject(SegmentoService);

  protected readonly minimo = TAMANHO_MINIMO_SENHA;

  private token = '';
  novaSenha = '';
  confirmacao = '';

  readonly semToken = signal(false);
  /** Link de convite (conta nova): muda o texto — é "confirmar e-mail e criar senha", não "nova senha". */
  readonly convite = signal(false);
  readonly salvando = signal(false);
  readonly mostrarSenha = signal(false);

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';
    this.semToken.set(!this.token);
    this.convite.set(this.route.snapshot.queryParamMap.get('convite') === '1');
  }

  salvar(): void {
    if (this.novaSenha.length < TAMANHO_MINIMO_SENHA) {
      this.notificacao.erro(`A nova senha deve ter pelo menos ${TAMANHO_MINIMO_SENHA} caracteres.`);
      return;
    }
    if (this.novaSenha !== this.confirmacao) {
      this.notificacao.erro('A confirmação não é igual à nova senha.');
      return;
    }

    this.salvando.set(true);
    this.auth.redefinirSenha(this.token, this.novaSenha).subscribe({
      next: () => {
        this.salvando.set(false);
        this.notificacao.sucesso(this.convite() ? 'Pronto! Senha cadastrada. Entre com seu e-mail e senha.' : 'Senha redefinida! Entre com a nova senha.');
        this.router.navigateByUrl('/entrar');
      },
      error: (resposta) => {
        this.salvando.set(false);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível redefinir a senha.');
      }
    });
  }

  pedirNovoLink(): void {
    this.router.navigateByUrl('/esqueci-senha');
  }
}
