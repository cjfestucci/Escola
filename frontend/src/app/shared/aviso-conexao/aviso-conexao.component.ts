import { Component, inject, signal } from '@angular/core';

import { ConexaoService } from '../../services/conexao.service';

/** Faixa no topo quando o app não consegue falar com o servidor (ver ConexaoService). Aparece em qualquer tela, inclusive a de
 * entrar: com o servidor fora o app ainda abre do cache do PWA, e sem a faixa a pessoa veria só telas vazias ou erros soltos. */
@Component({
  selector: 'app-aviso-conexao',
  templateUrl: './aviso-conexao.component.html',
  styleUrl: './aviso-conexao.component.scss'
})
export class AvisoConexaoComponent {
  protected readonly conexao = inject(ConexaoService);
  readonly tentando = signal(false);

  recarregar(): void {
    location.reload();
  }

  async tentar(): Promise<void> {
    this.tentando.set(true);
    const voltou = await this.conexao.tentarAgora();
    // Voltou: recarrega pra a tela buscar de novo o que tinha falhado (dados, configuração da escola).
    if (voltou) location.reload();
    else this.tentando.set(false);
  }
}
