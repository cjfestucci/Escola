import { Component, input, output } from '@angular/core';

/** Botão de escolher arquivo em português — o <input type="file"> nativo mostra
 * "Choose File / No file chosen" seguindo o idioma do navegador, não da página. */
@Component({
  selector: 'app-seletor-arquivo',
  imports: [],
  templateUrl: './seletor-arquivo.component.html',
  styleUrl: './seletor-arquivo.component.scss'
})
export class SeletorArquivoComponent {
  readonly rotulo = input('Escolher arquivo');
  readonly aceitar = input('image/*');
  readonly capturarCamera = input(false);
  readonly desabilitado = input(false);

  readonly arquivoSelecionado = output<File>();

  aoSelecionar(event: Event): void {
    const input = event.target as HTMLInputElement;
    const arquivo = input.files?.[0];
    input.value = '';
    if (arquivo) this.arquivoSelecionado.emit(arquivo);
  }
}
