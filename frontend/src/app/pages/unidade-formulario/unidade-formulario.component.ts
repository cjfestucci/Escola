import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { NotificacaoService } from '../../services/notificacao.service';
import { UnidadeService } from '../../services/unidade.service';

@Component({
  selector: 'app-unidade-formulario',
  imports: [FormsModule],
  templateUrl: './unidade-formulario.component.html',
  styleUrl: './unidade-formulario.component.scss'
})
export class UnidadeFormularioComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly unidadeService = inject(UnidadeService);
  private readonly notificacao = inject(NotificacaoService);

  private unidadeId: string | null = null;

  readonly carregando = signal(true);
  readonly salvando = signal(false);

  nome = '';
  endereco = '';
  telefone = '';
  ativa = true;

  get titulo(): string {
    return this.unidadeId ? 'Editar unidade' : 'Nova unidade';
  }

  ngOnInit(): void {
    this.unidadeId = this.route.snapshot.paramMap.get('id');

    if (this.unidadeId) {
      this.unidadeService.obterPorId(this.unidadeId).subscribe({
        next: (unidade) => {
          this.nome = unidade.nome;
          this.endereco = unidade.endereco ?? '';
          this.telefone = unidade.telefone ?? '';
          this.ativa = unidade.ativa;
          this.carregando.set(false);
        },
        error: () => {
          this.notificacao.erro('Não foi possível carregar a unidade.');
          this.carregando.set(false);
        }
      });
    } else {
      this.carregando.set(false);
    }
  }

  salvar(): void {
    if (!this.nome.trim()) {
      this.notificacao.erro('Informe o nome da unidade.');
      return;
    }

    const payload = {
      nome: this.nome.trim(),
      endereco: this.endereco.trim() || null,
      telefone: this.telefone.trim() || null,
      ativa: this.ativa
    };

    this.salvando.set(true);
    const requisicao$ = this.unidadeId
      ? this.unidadeService.editar(this.unidadeId, payload)
      : this.unidadeService.criar(payload);

    requisicao$.subscribe({
      next: () => {
        this.notificacao.sucesso('Unidade salva com sucesso.');
        this.router.navigateByUrl('/unidades');
      },
      error: (resposta) => {
        this.salvando.set(false);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível salvar a unidade.');
      }
    });
  }

  cancelar(): void {
    this.router.navigateByUrl('/unidades');
  }
}
