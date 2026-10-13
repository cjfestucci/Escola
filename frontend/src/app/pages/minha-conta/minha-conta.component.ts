import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import QRCode from 'qrcode';

import { AuthService } from '../../services/auth.service';
import { DoisFatores, InicioDoisFatores, MinhaContaService } from '../../services/minha-conta.service';
import { NotificacaoService } from '../../services/notificacao.service';
import { formatarDataHoraAbsoluta } from '../../shared/data-utils';

type Etapa = 'status' | 'ativando' | 'codigos' | 'desativando' | 'novosCodigos';

/** Minha conta: a verificação em duas etapas da própria pessoa. O QR Code é gerado no navegador — o segredo não sai pra nenhum serviço. */
@Component({
  selector: 'app-minha-conta',
  imports: [FormsModule],
  templateUrl: './minha-conta.component.html',
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
    if (!/^\d{6}$/.test(this.codigo.trim())) return this.notificacao.erro('Digite o código de 6 dígitos que aparece no app.');
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
