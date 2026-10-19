import { Component, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { Cupom } from '../../../models/plataforma.model';
import { NotificacaoService } from '../../../services/notificacao.service';
import { PlataformaService } from '../../../services/plataforma.service';
import { CalendarioComponent } from '../../../shared/calendario/calendario.component';
import { formatarDataAbsoluta, hojeIso } from '../../../shared/data-utils';

/** Cupons de desconto da assinatura (Suporte): criar, ativar e desativar. Mudar um cupom não afeta quem já assinou com ele. */
@Component({
  selector: 'app-cupons',
  imports: [FormsModule, CalendarioComponent],
  templateUrl: './cupons.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './cupons.component.scss'
})
export class CuponsComponent implements OnInit {
  private readonly plataforma = inject(PlataformaService);
  private readonly notificacao = inject(NotificacaoService);
  protected readonly formatarData = formatarDataAbsoluta;
  protected readonly hojeIso = hojeIso;

  readonly cupons = signal<Cupom[]>([]);
  readonly formularioAberto = signal(false);
  readonly salvando = signal(false);
  readonly processandoId = signal<string | null>(null);
  readonly calendarioAberto = signal(false);

  codigo = '';
  percentual: number | null = 20;
  meses: number | null = 3;
  mesesGratis: number | null = null;
  validoAte: string | null = null;
  usosMaximos: number | null = null;
  observacao = '';

  ngOnInit(): void {
    this.plataforma.listarCupons().subscribe({
      next: (lista) => this.cupons.set(lista),
      error: () => this.notificacao.erro('Não foi possível carregar os cupons.')
    });
  }

  abrir(): void {
    this.codigo = '';
    this.percentual = 20;
    this.meses = 3;
    this.mesesGratis = null;
    this.validoAte = null;
    this.usosMaximos = null;
    this.observacao = '';
    this.formularioAberto.set(true);
  }

  escolherValidade(data: string): void {
    this.validoAte = data;
    this.calendarioAberto.set(false);
  }

  salvar(): void {
    if (this.codigo.trim().length < 3) return this.notificacao.erro('Informe o código do cupom (pelo menos 3 letras ou números).');
    const percentual = this.percentual || 0;
    const mesesGratis = this.mesesGratis || null;
    if (percentual === 100) return this.notificacao.erro('Para dar meses grátis, use o campo "Meses grátis" (desconto de 100% não gera fatura).');
    if (percentual < 0 || percentual > 99) return this.notificacao.erro('O desconto precisa ser de 0% a 99%.');
    if (mesesGratis !== null && (mesesGratis < 1 || mesesGratis > 12)) return this.notificacao.erro('Os meses grátis precisam ser de 1 a 12.');
    if (percentual === 0 && mesesGratis === null) return this.notificacao.erro('Informe meses grátis ou um desconto.');
    this.salvando.set(true);
    this.plataforma
      .criarCupom({
        codigo: this.codigo,
        percentualDesconto: percentual,
        meses: percentual > 0 ? this.meses || null : null,
        mesesGratis,
        validoAte: this.validoAte,
        usosMaximos: this.usosMaximos || null,
        observacao: this.observacao.trim() || null
      })
      .subscribe({
        next: (cupom) => {
          this.salvando.set(false);
          this.formularioAberto.set(false);
          this.cupons.update((lista) => [cupom, ...lista]);
          this.notificacao.sucesso(`Cupom ${cupom.codigo} criado.`);
        },
        error: (erro) => {
          this.salvando.set(false);
          this.notificacao.erro(typeof erro.error === 'string' ? erro.error : 'Não foi possível criar o cupom.');
        }
      });
  }

  alternar(cupom: Cupom): void {
    this.processandoId.set(cupom.id);
    this.plataforma.definirCupomAtivo(cupom.id, !cupom.ativo).subscribe({
      next: (atualizado) => {
        this.processandoId.set(null);
        this.cupons.update((lista) => lista.map((c) => (c.id === atualizado.id ? atualizado : c)));
      },
      error: () => {
        this.processandoId.set(null);
        this.notificacao.erro('Não foi possível alterar o cupom.');
      }
    });
  }
}
