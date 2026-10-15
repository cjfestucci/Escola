import { Directive, ElementRef, HostListener, Renderer2, forwardRef, inject, input } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { TAMANHO_MAXIMO, TipoMascara, formatarPorMascara } from './mascara';

/**
 * Máscara de digitação: `<input appMascara="telefone" [(ngModel)]="telefone" />` (também `cpf`, `cnpj`, `cpfCnpj`, `cep`).
 * Substitui o value accessor padrão do ngModel: formata o que chega do modelo (dado antigo sem máscara aparece formatado) e o que é
 * digitado/colado, devolvendo ao modelo o texto já formatado. O cursor fica depois do mesmo dígito em que estava, pra editar no meio
 * do número não pular pro fim. Define `inputmode="numeric"` e o `maxlength` certo sozinha.
 */
@Directive({
  selector: 'input[appMascara]',
  standalone: true,
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => MascaraDirective), multi: true }]
})
export class MascaraDirective implements ControlValueAccessor {
  readonly appMascara = input.required<TipoMascara>();

  private readonly elemento = inject<ElementRef<HTMLInputElement>>(ElementRef);
  private readonly renderer = inject(Renderer2);
  private aoMudar: (valor: string) => void = () => {};
  private aoTocar: () => void = () => {};

  ngOnInit(): void {
    const campo = this.elemento.nativeElement;
    if (!campo.getAttribute('inputmode')) this.renderer.setAttribute(campo, 'inputmode', 'numeric');
    this.renderer.setAttribute(campo, 'maxlength', String(TAMANHO_MAXIMO[this.appMascara()]));
  }

  writeValue(valor: string | null | undefined): void {
    this.renderer.setProperty(this.elemento.nativeElement, 'value', formatarPorMascara(this.appMascara(), valor));
  }

  registerOnChange(fn: (valor: string) => void): void { this.aoMudar = fn; }
  registerOnTouched(fn: () => void): void { this.aoTocar = fn; }
  setDisabledState(desabilitado: boolean): void {
    this.renderer.setProperty(this.elemento.nativeElement, 'disabled', desabilitado);
  }

  @HostListener('input')
  aoDigitar(): void {
    const campo = this.elemento.nativeElement;
    const bruto = campo.value;
    // Quantos dígitos havia antes do cursor: depois de formatar, o cursor volta pra logo após esse mesmo dígito.
    const digitosAntes = bruto.slice(0, campo.selectionStart ?? bruto.length).replace(/\D/g, '').length;
    const formatado = formatarPorMascara(this.appMascara(), bruto);
    if (formatado !== bruto) {
      campo.value = formatado;
      let pos = 0;
      for (let vistos = 0; pos < formatado.length && vistos < digitosAntes; pos++) {
        if (/\d/.test(formatado[pos])) vistos++;
      }
      if (document.activeElement === campo) campo.setSelectionRange(pos, pos);
    }
    this.aoMudar(formatado);
  }

  @HostListener('blur')
  aoSair(): void { this.aoTocar(); }
}
