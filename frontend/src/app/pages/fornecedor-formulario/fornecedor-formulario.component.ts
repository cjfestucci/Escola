import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { NotificacaoService } from '../../services/notificacao.service';
import { FornecedorService } from '../../services/fornecedor.service';

@Component({
  selector: 'app-fornecedor-formulario',
  imports: [FormsModule],
  templateUrl: './fornecedor-formulario.component.html',
  styleUrl: './fornecedor-formulario.component.scss'
})
export class FornecedorFormularioComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fornecedorService = inject(FornecedorService);
  private readonly notificacao = inject(NotificacaoService);

  private fornecedorId: string | null = null;

  readonly carregando = signal(true);
  readonly salvando = signal(false);

  nome = '';
  documento = '';
  telefone = '';
  email = '';

  get titulo(): string {
    return this.fornecedorId ? 'Editar fornecedor' : 'Novo fornecedor';
  }

  ngOnInit(): void {
    this.fornecedorId = this.route.snapshot.paramMap.get('id');

    if (this.fornecedorId) {
      this.fornecedorService.obterPorId(this.fornecedorId).subscribe({
        next: (fornecedor) => {
          this.nome = fornecedor.nome;
          this.documento = fornecedor.documento ?? '';
          this.telefone = fornecedor.telefone ?? '';
          this.email = fornecedor.email ?? '';
          this.carregando.set(false);
        },
        error: () => {
          this.notificacao.erro('Não foi possível carregar o fornecedor.');
          this.carregando.set(false);
        }
      });
    } else {
      this.carregando.set(false);
    }
  }

  salvar(): void {
    if (!this.nome.trim()) {
      this.notificacao.erro('Informe o nome do fornecedor.');
      return;
    }

    const payload = {
      nome: this.nome.trim(),
      documento: this.documento.trim() || null,
      telefone: this.telefone.trim() || null,
      email: this.email.trim() || null
    };

    this.salvando.set(true);
    const requisicao$ = this.fornecedorId
      ? this.fornecedorService.editar(this.fornecedorId, payload)
      : this.fornecedorService.criar(payload);

    requisicao$.subscribe({
      next: () => {
        this.notificacao.sucesso('Fornecedor salvo com sucesso.');
        this.router.navigateByUrl('/fornecedores');
      },
      error: (resposta) => {
        this.salvando.set(false);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível salvar o fornecedor.');
      }
    });
  }

  cancelar(): void {
    this.router.navigateByUrl('/fornecedores');
  }
}
