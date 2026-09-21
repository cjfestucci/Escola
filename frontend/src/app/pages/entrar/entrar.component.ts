import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { environment } from '../../../environments/environment';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-entrar',
  imports: [FormsModule],
  templateUrl: './entrar.component.html',
  styleUrl: './entrar.component.scss'
})
export class EntrarComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  // Só em dev, pra agilizar teste manual — nunca preenche sozinho em produção.
  email = environment.production ? '' : 'admin@escola.dev';
  senha = environment.production ? '' : 'escola123';

  readonly entrando = signal(false);
  readonly erro = signal<string | null>(null);
  readonly mostrarSenha = signal(false);
  readonly mostrarAjudaSenha = signal(false);

  entrar(): void {
    this.erro.set(null);

    if (!this.email.trim() || !this.senha) {
      this.erro.set('Informe e-mail e senha.');
      return;
    }

    this.entrando.set(true);
    this.auth.entrar(this.email.trim(), this.senha).subscribe({
      next: (resposta) => {
        this.entrando.set(false);
        this.router.navigateByUrl(resposta.papel === 'Responsavel' ? '/portal/filhos' : '/alunos');
      },
      error: (resposta) => {
        this.entrando.set(false);
        this.erro.set(resposta.status === 401 ? 'E-mail ou senha inválidos.' : 'Não foi possível entrar. Tente novamente.');
      }
    });
  }
}
