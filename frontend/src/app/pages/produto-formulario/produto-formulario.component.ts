import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { NotificacaoService } from '../../services/notificacao.service';
import { ProdutoService } from '../../services/produto.service';

export const UNIDADES_MEDIDA = [
  { valor: 'un', rotulo: 'Unidade (un)' },
  { valor: 'par', rotulo: 'Par' },
  { valor: 'cx', rotulo: 'Caixa (cx)' },
  { valor: 'pct', rotulo: 'Pacote (pct)' },
  { valor: 'kg', rotulo: 'Quilo (kg)' },
  { valor: 'g', rotulo: 'Grama (g)' },
  { valor: 'L', rotulo: 'Litro (L)' },
  { valor: 'mL', rotulo: 'Mililitro (mL)' },
  { valor: 'm', rotulo: 'Metro (m)' }
];

@Component({
  selector: 'app-produto-formulario',
  imports: [FormsModule],
  templateUrl: './produto-formulario.component.html',
  styleUrl: './produto-formulario.component.scss'
})
export class ProdutoFormularioComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly produtoService = inject(ProdutoService);
  private readonly notificacao = inject(NotificacaoService);

  private produtoId: string | null = null;

  readonly carregando = signal(true);
  readonly salvando = signal(false);
  protected readonly unidadesMedida = UNIDADES_MEDIDA;

  nome = '';
  codigo = '';
  unidadeMedida = 'un';
  estoqueMinimo: number | null = 0;

  get titulo(): string {
    return this.produtoId ? 'Editar produto' : 'Novo produto';
  }

  ngOnInit(): void {
    this.produtoId = this.route.snapshot.paramMap.get('id');

    if (this.produtoId) {
      this.produtoService.obterPorId(this.produtoId).subscribe({
        next: (produto) => {
          this.nome = produto.nome;
          this.codigo = produto.codigo ?? '';
          this.unidadeMedida = produto.unidadeMedida;
          this.estoqueMinimo = produto.estoqueMinimo;
          this.carregando.set(false);
        },
        error: () => {
          this.notificacao.erro('Não foi possível carregar o produto.');
          this.carregando.set(false);
        }
      });
    } else {
      this.carregando.set(false);
    }
  }

  salvar(): void {
    if (!this.nome.trim()) {
      this.notificacao.erro('Informe o nome do produto.');
      return;
    }
    if (!this.unidadeMedida) {
      this.notificacao.erro('Selecione a unidade de medida.');
      return;
    }
    if (this.estoqueMinimo !== null && this.estoqueMinimo < 0) {
      this.notificacao.erro('O estoque mínimo não pode ser negativo.');
      return;
    }

    const payload = {
      nome: this.nome.trim(),
      codigo: this.codigo.trim() || null,
      unidadeMedida: this.unidadeMedida,
      estoqueMinimo: this.estoqueMinimo ?? 0
    };

    this.salvando.set(true);
    const requisicao$ = this.produtoId
      ? this.produtoService.editar(this.produtoId, payload)
      : this.produtoService.criar(payload);

    requisicao$.subscribe({
      next: () => {
        this.notificacao.sucesso('Produto salvo com sucesso.');
        this.router.navigateByUrl('/estoque/produtos');
      },
      error: (resposta) => {
        this.salvando.set(false);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível salvar o produto.');
      }
    });
  }

  cancelar(): void {
    this.router.navigateByUrl('/estoque/produtos');
  }
}
