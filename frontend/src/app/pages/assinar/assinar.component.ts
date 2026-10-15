import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import {
  CadastroAssinaturaResposta,
  CupomPublico,
  PlanoAssinatura,
  cnpjValido,
  cpfValido,
  formatarReais,
  atletasExcedentes,
  valorDoPlano
} from '../../models/assinatura.model';
import { AssinaturaService } from '../../services/assinatura.service';
import { NotificacaoService } from '../../services/notificacao.service';
import { EMPRESA_COBRANCA, NOME_PRODUTO, SegmentoService } from '../../services/segmento.service';
import { formatarDataAbsoluta } from '../../shared/data-utils';
import { environment } from '../../../environments/environment';

type Etapa = 'clube' | 'voce' | 'pronto';

function soDigitos(texto: string): string {
  return texto.replace(/\D/g, '');
}

/** Cadastro pelo site: o clube vira cliente sozinho. Pública (sem login), como a tela de entrar. */
@Component({
  selector: 'app-assinar',
  imports: [FormsModule],
  templateUrl: './assinar.component.html',
  styleUrl: './assinar.component.scss'
})
export class AssinarComponent implements OnInit {
  private readonly assinaturas = inject(AssinaturaService);
  private readonly notificacao = inject(NotificacaoService);
  private readonly rota = inject(ActivatedRoute);
  private readonly router = inject(Router);
  protected readonly segmentoService = inject(SegmentoService);

  protected readonly formatarReais = formatarReais;
  protected readonly atletasExcedentes = atletasExcedentes;
  protected readonly formatarData = formatarDataAbsoluta;
  protected readonly urlSite = environment.urlSite;
  protected readonly empresaCobranca = EMPRESA_COBRANCA;
  protected readonly nomeProduto = NOME_PRODUTO;

  readonly plano = signal<PlanoAssinatura | null>(null);
  readonly etapa = signal<Etapa>('clube');
  readonly enviando = signal(false);
  readonly resultado = signal<CadastroAssinaturaResposta | null>(null);

  nomeClube = '';
  cpfCnpj = '';
  cidade = '';
  readonly atletas = signal(100);
  nomeAdmin = '';
  email = '';
  celular = '';
  aceite = false;
  cupomDigitado = '';
  readonly cupom = signal<CupomPublico | null>(null);
  readonly aplicandoCupom = signal(false);

  /** Valor cheio do plano, antes do cupom. */
  readonly valorCheio = computed(() => {
    const plano = this.plano();
    return plano ? valorDoPlano(plano, this.atletas()) : null;
  });

  /** Com o cupom aplicado (o servidor confere de novo ao assinar). */
  readonly valorMensal = computed(() => {
    const cheio = this.valorCheio();
    const cupom = this.cupom();
    if (cheio === null) return null;
    return cupom ? Math.round(cheio * (100 - cupom.percentualDesconto)) / 100 : cheio;
  });

  aplicarCupom(): void {
    const codigo = this.cupomDigitado.trim();
    if (!codigo) return;
    this.aplicandoCupom.set(true);
    this.assinaturas.conferirCupom(codigo).subscribe({
      next: (cupom) => {
        this.aplicandoCupom.set(false);
        this.cupom.set(cupom);
        this.notificacao.sucesso(`Cupom ${cupom.codigo} aplicado: ${cupom.descricao}.`);
      },
      error: (erro) => {
        this.aplicandoCupom.set(false);
        this.cupom.set(null);
        this.notificacao.erro(typeof erro.error === 'string' ? erro.error : 'Cupom inválido.');
      }
    });
  }

  removerCupom(): void {
    this.cupom.set(null);
    this.cupomDigitado = '';
  }

  ngOnInit(): void {
    // O simulador do site institucional manda a quantidade de atletas.
    const atletas = Number(this.rota.snapshot.queryParamMap.get('atletas'));
    if (Number.isInteger(atletas) && atletas > 0 && atletas <= 5000) this.atletas.set(atletas);

    this.assinaturas.plano().subscribe({
      next: (plano) => this.plano.set(plano),
      error: () => this.notificacao.erro('Não foi possível carregar o plano. Tente de novo em instantes.')
    });
  }

  definirAtletas(valor: number | string): void {
    const numero = Math.round(Number(valor));
    this.atletas.set(Number.isFinite(numero) ? Math.min(5000, Math.max(0, numero)) : 0);
  }

  continuar(): void {
    const documento = soDigitos(this.cpfCnpj);
    if (this.nomeClube.trim().length < 3) return this.notificacao.erro('Informe o nome do clube.');
    if (documento.length === 11 ? !cpfValido(documento) : !cnpjValido(documento)) return this.notificacao.erro('Informe um CPF ou CNPJ válido.');
    if (this.cidade.trim().length < 2) return this.notificacao.erro('Informe a cidade.');
    if (this.atletas() < 1) return this.notificacao.erro('Informe quantos atletas o clube tem.');
    this.etapa.set('voce');
  }

  voltar(): void {
    this.etapa.set('clube');
  }

  assinar(): void {
    const email = this.email.trim();
    if (this.nomeAdmin.trim().length < 3) return this.notificacao.erro('Informe o seu nome.');
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) return this.notificacao.erro('Informe um e-mail válido.');
    if (![10, 11].includes(soDigitos(this.celular).length)) return this.notificacao.erro('Informe o celular com DDD.');
    if (!this.aceite) return this.notificacao.erro('É preciso aceitar os Termos de Uso e a Política de Privacidade.');

    this.enviando.set(true);
    this.assinaturas
      .cadastrar({
        nomeClube: this.nomeClube.trim(),
        cpfCnpj: soDigitos(this.cpfCnpj),
        cidade: this.cidade.trim(),
        atletas: this.atletas(),
        nomeAdmin: this.nomeAdmin.trim(),
        email,
        celular: soDigitos(this.celular),
        aceiteTermos: this.aceite,
        cupom: this.cupom()?.codigo ?? null
      })
      .subscribe({
        next: (resultado) => {
          this.enviando.set(false);
          this.resultado.set(resultado);
          this.etapa.set('pronto');
        },
        error: (erro) => {
          this.enviando.set(false);
          this.notificacao.erro(typeof erro.error === 'string' ? erro.error : 'Não foi possível concluir a assinatura. Tente de novo.');
        }
      });
  }

  irParaEntrar(): void {
    this.router.navigateByUrl('/entrar');
  }
}
