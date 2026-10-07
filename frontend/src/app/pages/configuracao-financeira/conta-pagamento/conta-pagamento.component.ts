import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { ContaPagamento, TIPOS_EMPRESA, TipoEmpresa, rotuloSituacaoGateway } from '../../../models/pagamento.model';
import { AuthService } from '../../../services/auth.service';
import { NotificacaoService } from '../../../services/notificacao.service';
import { PagamentoService } from '../../../services/pagamento.service';
import { MESES_PT_BR, formatarDataHoraAbsoluta, hojeIso } from '../../../shared/data-utils';

const soDigitos = (valor: string) => valor.replace(/\D/g, '');

/** Seção "Pagamento automático" de Configurações → Financeiro: abre a subconta da escola no gateway (Asaas) e mostra a situação dela.
 * Some quando o gateway não está configurado no ambiente e a escola ainda não tem conta. */
@Component({
  selector: 'app-conta-pagamento',
  imports: [FormsModule],
  templateUrl: './conta-pagamento.component.html',
  styleUrl: './conta-pagamento.component.scss'
})
export class ContaPagamentoComponent implements OnInit {
  private readonly pagamentoService = inject(PagamentoService);
  private readonly notificacao = inject(NotificacaoService);
  protected readonly auth = inject(AuthService);

  readonly conta = signal<ContaPagamento | null>(null);
  readonly aberta = signal(false);
  readonly conectando = signal(false);
  readonly atualizando = signal(false);
  protected readonly tiposEmpresa = TIPOS_EMPRESA;
  protected readonly meses = MESES_PT_BR;
  protected readonly dias = Array.from({ length: 31 }, (_, i) => i + 1);
  protected readonly anos = (() => {
    const atual = Number(hojeIso().slice(0, 4));
    return Array.from({ length: 83 }, (_, i) => atual - 18 - i);
  })();
  protected readonly rotuloSituacao = rotuloSituacaoGateway;
  protected readonly formatarDataHora = formatarDataHoraAbsoluta;

  readonly visivel = computed(() => {
    const c = this.conta();
    return !!c && (c.disponivel || c.conectada);
  });

  nome = '';
  email = '';
  cpfCnpj = '';
  tipoEmpresa: TipoEmpresa | '' = '';
  diaNascimento: number | null = null;
  mesNascimento: number | null = null;
  anoNascimento: number | null = null;
  celular = '';
  cep = '';
  endereco = '';
  numero = '';
  complemento = '';
  bairro = '';
  faturamentoMensal: number | null = null;

  /** CPF = titular pessoa física (pede nascimento); CNPJ = empresa (pede o tipo). Decidido pela quantidade de dígitos. */
  get ehCnpj(): boolean {
    return soDigitos(this.cpfCnpj).length > 11;
  }

  ngOnInit(): void {
    // Complemento da tela: se falhar, o resto das configurações segue funcionando.
    this.pagamentoService.obterConta().subscribe({ next: (c) => this.conta.set(c), error: () => undefined });
  }

  conectar(): void {
    const documento = soDigitos(this.cpfCnpj);
    if (!this.nome.trim() || !this.email.trim()) return this.notificacao.erro('Informe o nome do titular e o e-mail.');
    if (documento.length !== 11 && documento.length !== 14) return this.notificacao.erro('Informe um CPF (11 dígitos) ou CNPJ (14 dígitos).');
    if (this.ehCnpj && !this.tipoEmpresa) return this.notificacao.erro('Selecione o tipo de empresa.');

    let dataNascimento: string | null = null;
    if (!this.ehCnpj) {
      if (!this.diaNascimento || !this.mesNascimento || !this.anoNascimento) return this.notificacao.erro('Informe a data de nascimento do titular.');
      const data = new Date(Date.UTC(this.anoNascimento, this.mesNascimento - 1, this.diaNascimento));
      if (data.getUTCDate() !== this.diaNascimento) return this.notificacao.erro('Data de nascimento inválida.');
      dataNascimento = `${this.anoNascimento}-${String(this.mesNascimento).padStart(2, '0')}-${String(this.diaNascimento).padStart(2, '0')}`;
    }
    if (!this.celular.trim()) return this.notificacao.erro('Informe o celular.');
    if (!this.cep.trim() || !this.endereco.trim() || !this.numero.trim() || !this.bairro.trim())
      return this.notificacao.erro('Informe o endereço completo (CEP, endereço, número e bairro).');
    if (!this.faturamentoMensal || this.faturamentoMensal <= 0) return this.notificacao.erro('Informe o faturamento mensal estimado.');

    this.conectando.set(true);
    this.pagamentoService
      .conectarConta({
        nome: this.nome.trim(),
        email: this.email.trim(),
        cpfCnpj: documento,
        tipoEmpresa: this.ehCnpj ? (this.tipoEmpresa as TipoEmpresa) : null,
        dataNascimento,
        celular: this.celular,
        cep: this.cep,
        endereco: this.endereco.trim(),
        numero: this.numero.trim(),
        complemento: this.complemento.trim() || null,
        bairro: this.bairro.trim(),
        faturamentoMensal: this.faturamentoMensal
      })
      .subscribe({
        next: (c) => {
          this.conta.set(c);
          this.conectando.set(false);
          this.notificacao.sucesso('Conta de pagamento aberta.');
        },
        error: (resposta) => {
          this.conectando.set(false);
          this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível abrir a conta de pagamento.');
        }
      });
  }

  atualizar(): void {
    this.atualizando.set(true);
    this.pagamentoService.atualizarConta().subscribe({
      next: (c) => {
        this.conta.set(c);
        this.atualizando.set(false);
        this.notificacao.info('Situação da conta atualizada.');
      },
      error: (resposta) => {
        this.atualizando.set(false);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível consultar a conta agora.');
      }
    });
  }
}
