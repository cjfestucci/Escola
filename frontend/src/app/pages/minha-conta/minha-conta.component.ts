import { Component, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { FormsModule } from '@angular/forms';
import QRCode from 'qrcode';

import { AuthService } from '../../services/auth.service';
import { DoisFatores, EnvioCodigo, InicioDoisFatores, MinhaContaService } from '../../services/minha-conta.service';
import { NotificacaoService } from '../../services/notificacao.service';
import { formatarDataHoraAbsoluta } from '../../shared/data-utils';

type Etapa = 'status' | 'ativando' | 'ativandoEmail' | 'codigos' | 'desativando' | 'novosCodigos';

/** Minha conta: a verificação em duas etapas da própria pessoa, por código no e-mail ou app autenticador. O QR Code é gerado no
 * navegador — o segredo não sai pra nenhum serviço. */
@Component({
  selector: 'app-minha-conta',
  imports: [FormsModule, NgTemplateOutlet],
  templateUrl: './minha-conta.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './minha-conta.component.scss'
})
export class MinhaContaComponent implements OnInit {
  private readonly servico = inject(MinhaContaService);
  private readonly notificacao = inject(NotificacaoService);
  protected readonly auth = inject(AuthService);
  protected readonly formatarDataHora = formatarDataHoraAbsoluta;

  readonly carregando = signal(true);
  readonly estado = signal<DoisFatores | null>(null);
  readonly etapa = signal<Etapa>('status');
  readonly inicio = signal<InicioDoisFatores | null>(null);
  readonly qrCode = signal<string | null>(null);
  readonly codigosRecuperacao = signal<string[]>([]);
  readonly enviando = signal(false);
  /** Pra onde foi o último código por e-mail ("c***@escola.com"). */
  readonly emailMascarado = signal<string | null>(null);

  codigo = '';
  senha = '';

  ngOnInit(): void {
    this.carregar();
  }

  private carregar(): void {
    this.servico.obter().subscribe({
      next: (e) => {
        this.estado.set(e);
        this.carregando.set(false);
      },
      error: () => {
        this.carregando.set(false);
        this.notificacao.erro('Não foi possível carregar a sua conta.');
      }
    });
  }

  comecarAtivacao(): void {
    this.enviando.set(true);
    this.servico.iniciar().subscribe({
      next: async (inicio) => {
        this.inicio.set(inicio);
        this.qrCode.set(await QRCode.toDataURL(inicio.uriOtpAuth, { width: 200, margin: 1 }));
        this.codigo = '';
        this.etapa.set('ativando');
        this.enviando.set(false);
      },
      error: (erro) => this.falhou(erro, 'Não foi possível começar a ativação.')
    });
  }

  confirmarAtivacao(): void {
    if (!/^\d{6}$/.test(this.codigo.trim())) return this.notificacao.erro('Digite o código de 6 dígitos que aparece no app.');
    this.enviando.set(true);
    this.servico.confirmar(this.codigo.trim()).subscribe({
      next: (codigos) => {
        this.codigosRecuperacao.set(codigos);
        this.etapa.set('codigos');
        this.enviando.set(false);
        this.notificacao.sucesso('Verificação em duas etapas ativada.');
        this.carregar();
      },
      error: (erro) => this.falhou(erro, 'Código inválido.')
    });
  }

  /** Por e-mail: manda um código pro e-mail da conta; confirmar com ele ativa (prova que a pessoa recebe nesse endereço). */
  comecarAtivacaoEmail(): void {
    this.enviando.set(true);
    this.servico.iniciarEmail().subscribe({
      next: (envio) => {
        this.codigo = '';
        this.etapa.set('ativandoEmail');
        this.tratarEnvio(envio);
      },
      error: (erro) => this.falhou(erro, 'Não foi possível enviar o código.')
    });
  }

  confirmarAtivacaoEmail(): void {
    if (!/^\d{6}$/.test(this.codigo.trim())) return this.notificacao.erro('Digite o código de 6 dígitos que enviamos por e-mail.');
    this.enviando.set(true);
    this.servico.confirmarEmail(this.codigo.trim()).subscribe({
      next: (codigos) => {
        this.codigosRecuperacao.set(codigos);
        this.etapa.set('codigos');
        this.enviando.set(false);
        this.notificacao.sucesso('Verificação em duas etapas ativada.');
        this.carregar();
      },
      error: (erro) => this.falhou(erro, 'Código inválido.')
    });
  }

  /** Com o código por e-mail já ativo: pede um código pra desativar ou gerar novos códigos de recuperação. */
  enviarCodigo(): void {
    this.enviando.set(true);
    this.servico.enviarCodigo().subscribe({
      next: (envio) => this.tratarEnvio(envio),
      error: (erro) => this.falhou(erro, 'Não foi possível enviar o código.')
    });
  }

  private tratarEnvio(envio: EnvioCodigo): void {
    this.enviando.set(false);
    this.emailMascarado.set(envio.emailMascarado);
    if (envio.aviso) this.notificacao.info(envio.aviso);
    else this.notificacao.sucesso(`Código enviado para ${envio.emailMascarado}.`);
  }

  esquecerAparelhos(): void {
    this.enviando.set(true);
    this.servico.esquecerAparelhos().subscribe({
      next: (e) => {
        this.estado.set(e);
        this.enviando.set(false);
        this.notificacao.sucesso('Pronto: o código volta a ser pedido em todos os aparelhos.');
      },
      error: (erro) => this.falhou(erro, 'Não foi possível esquecer os aparelhos.')
    });
  }

  desativar(): void {
    if (!this.senha || !this.codigo.trim()) return this.notificacao.erro('Informe a senha e um código.');
    this.enviando.set(true);
    this.servico.desativar(this.senha, this.codigo.trim()).subscribe({
      next: (e) => {
        this.estado.set(e);
        this.voltar();
        this.enviando.set(false);
        this.notificacao.sucesso('Verificação em duas etapas desativada.');
      },
      error: (erro) => this.falhou(erro, 'Não foi possível desativar.')
    });
  }

  gerarNovosCodigos(): void {
    if (!/^\d{6}$/.test(this.codigo.trim()))
      return this.notificacao.erro(this.estado()?.metodo === 'Email' ? 'Digite o código de 6 dígitos que enviamos por e-mail.' : 'Digite o código de 6 dígitos que aparece no app.');
    this.enviando.set(true);
    this.servico.novosCodigos(this.codigo.trim()).subscribe({
      next: (codigos) => {
        this.codigosRecuperacao.set(codigos);
        this.etapa.set('codigos');
        this.enviando.set(false);
        this.carregar();
      },
      error: (erro) => this.falhou(erro, 'Código inválido.')
    });
  }

  abrir(etapa: Etapa): void {
    this.codigo = '';
    this.senha = '';
    this.emailMascarado.set(null);
    this.etapa.set(etapa);
  }

  voltar(): void {
    this.codigo = '';
    this.senha = '';
    this.inicio.set(null);
    this.qrCode.set(null);
    this.codigosRecuperacao.set([]);
    this.etapa.set('status');
  }

  async copiarCodigos(): Promise<void> {
    try {
      await navigator.clipboard.writeText(this.codigosRecuperacao().join('\n'));
      this.notificacao.sucesso('Códigos copiados.');
    } catch {
      this.notificacao.erro('Não foi possível copiar. Anote os códigos à mão.');
    }
  }

  private falhou(erro: { error?: unknown }, padrao: string): void {
    this.enviando.set(false);
    this.notificacao.erro(typeof erro.error === 'string' ? erro.error : padrao);
  }
}
