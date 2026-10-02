import { Component, OnInit, inject, input, signal } from '@angular/core';

import { DocumentoSaude } from '../../models/documento-saude.model';
import { DocumentoSaudeService } from '../../services/documento-saude.service';
import { NotificacaoService } from '../../services/notificacao.service';
import { formatarDataHoraAbsoluta } from '../data-utils';
import { formatarTamanho } from '../numero-utils';
import { SeletorArquivoComponent } from '../seletor-arquivo/seletor-arquivo.component';

const TAMANHO_MAXIMO_BYTES = 10 * 1024 * 1024;

/** Lista (e, se `podeEditar`, anexa/remove) os documentos de saúde de um aluno. Cada ação vale na hora —
 * não depende de salvar a ficha de saúde. */
@Component({
  selector: 'app-documentos-saude',
  imports: [SeletorArquivoComponent],
  templateUrl: './documentos-saude.component.html',
  styleUrl: './documentos-saude.component.scss'
})
export class DocumentosSaudeComponent implements OnInit {
  private readonly documentoService = inject(DocumentoSaudeService);
  private readonly notificacao = inject(NotificacaoService);

  readonly alunoId = input.required<string>();
  readonly podeEditar = input(false);

  readonly documentos = signal<DocumentoSaude[]>([]);
  readonly carregando = signal(true);
  readonly enviando = signal(false);
  readonly baixandoId = signal<string | null>(null);
  readonly confirmandoRemocaoId = signal<string | null>(null);
  readonly removendoId = signal<string | null>(null);

  protected readonly formatarTamanho = formatarTamanho;
  protected readonly formatarDataHoraAbsoluta = formatarDataHoraAbsoluta;
  protected readonly aceitar = 'application/pdf,image/jpeg,image/png,image/webp';

  ngOnInit(): void {
    this.documentoService.listar(this.alunoId()).subscribe({
      next: (documentos) => {
        this.documentos.set(documentos);
        this.carregando.set(false);
      },
      error: () => {
        this.carregando.set(false);
        this.notificacao.erro('Não foi possível carregar os documentos.');
      }
    });
  }

  aoSelecionarArquivo(arquivo: File): void {
    if (arquivo.size > TAMANHO_MAXIMO_BYTES) {
      this.notificacao.erro('Arquivo maior que o limite de 10MB.');
      return;
    }

    this.enviando.set(true);
    this.documentoService.enviar(this.alunoId(), arquivo).subscribe({
      next: (documento) => {
        this.documentos.update((atual) => [documento, ...atual]);
        this.enviando.set(false);
        this.notificacao.sucesso('Documento anexado.');
      },
      error: (resposta) => {
        this.enviando.set(false);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível anexar o documento.');
      }
    });
  }

  baixar(documento: DocumentoSaude): void {
    this.baixandoId.set(documento.id);
    this.documentoService.baixar(this.alunoId(), documento.id).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = documento.nomeArquivo;
        link.click();
        URL.revokeObjectURL(url);
        this.baixandoId.set(null);
      },
      error: () => {
        this.baixandoId.set(null);
        this.notificacao.erro('Não foi possível baixar o documento.');
      }
    });
  }

  pedirConfirmacaoRemocao(id: string): void {
    this.confirmandoRemocaoId.set(id);
  }

  cancelarRemocao(): void {
    this.confirmandoRemocaoId.set(null);
  }

  confirmarRemocao(documento: DocumentoSaude): void {
    this.removendoId.set(documento.id);
    this.documentoService.remover(this.alunoId(), documento.id).subscribe({
      next: () => {
        this.documentos.update((atual) => atual.filter((d) => d.id !== documento.id));
        this.removendoId.set(null);
        this.confirmandoRemocaoId.set(null);
        this.notificacao.sucesso('Documento removido.');
      },
      error: () => {
        this.removendoId.set(null);
        this.confirmandoRemocaoId.set(null);
        this.notificacao.erro('Não foi possível remover o documento.');
      }
    });
  }
}
