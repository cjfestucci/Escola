import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { environment } from '../../../environments/environment';
import { AuthService } from '../../services/auth.service';
import { NotificacaoService } from '../../services/notificacao.service';
import { SegmentoService } from '../../services/segmento.service';

@Component({
  selector: 'app-entrar',
  imports: [FormsModule],
  templateUrl: './entrar.component.html',
  styleUrl: './entrar.component.scss'
})
export class EntrarComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly notificacao = inject(NotificacaoService);
  protected readonly segmentoService = inject(SegmentoService);

  // Só em dev, pra agilizar teste manual — nunca preenche sozinho em produção.
  email = environment.production ? '' : 'admin@escola.dev';
  senha = environment.production ? '' : 'escola123';

  readonly entrando = signal(false);
  readonly mostrarSenha = signal(false);
  readonly mostrarAjudaSenha = signal(false);

  entrar(): void {
    if (!this.email.trim() || !this.senha) {
      this.notificacao.erro('Informe e-mail e senha.');
      return;
    }

    this.entrando.set(true);
    this.auth.entrar(this.email.trim(), this.senha).subscribe({
      next: (resposta) => {
        this.entrando.set(false);
        // '' passa pelo redirecionamentoInicialGuard, que decide a landing certa por papel/segmento
        // (Responsavel -> portal, Equipe-escola -> /alunos, Equipe-clube -> /dashboard).
        this.router.navigateByUrl(resposta.papel === 'Responsavel' ? '/portal/filhos' : '/');
      },
      error: (resposta) => {
        this.entrando.set(false);
        this.notificacao.erro(resposta.status === 401 ? 'E-mail ou senha inválidos.' : 'Não foi possível entrar. Tente novamente.');
      }
    });
  }
}
