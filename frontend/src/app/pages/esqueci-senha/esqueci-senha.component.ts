import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { AuthService } from '../../services/auth.service';
import { NotificacaoService } from '../../services/notificacao.service';
import { SegmentoService } from '../../services/segmento.service';

/** Pede o link de redefinição de senha por e-mail. A confirmação é sempre a mesma, exista a conta ou não
 * (o backend não revela quais e-mails têm cadastro). */
@Component({
  selector: 'app-esqueci-senha',
  imports: [FormsModule],
  templateUrl: './esqueci-senha.component.html',
  styleUrl: './esqueci-senha.component.scss'
})
export class EsqueciSenhaComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly notificacao = inject(NotificacaoService);
  protected readonly segmentoService = inject(SegmentoService);

  email = '';
  readonly enviando = signal(false);
  readonly enviado = signal(false);

  enviar(): void {
    const email = this.email.trim();
    if (!email) {
      this.notificacao.erro('Informe o e-mail da sua conta.');
      return;
    }

    this.enviando.set(true);
    this.auth.esqueciSenha(email).subscribe({
      next: () => {
        this.enviando.set(false);
        this.enviado.set(true);
      },
      error: () => {
        this.enviando.set(false);
        this.notificacao.erro('Não foi possível enviar o pedido agora. Tente novamente em instantes.');
      }
    });
  }

  voltar(): void {
    this.router.navigateByUrl('/entrar');
  }
}
