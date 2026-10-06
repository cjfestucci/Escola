import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { AdminEscola, ClientePlataforma, DiagnosticoPlataforma, ROTULOS_SEGMENTO, ResultadoEnvioAdmin, SegmentoCliente } from '../../models/plataforma.model';
import { ConfiguracaoEscolaService } from '../../services/configuracao-escola.service';
import { NotificacaoService } from '../../services/notificacao.service';
import { PlataformaService } from '../../services/plataforma.service';
import { formatarDataHoraAbsoluta } from '../../shared/data-utils';
import { LogsModalComponent } from '../../shared/logs-modal/logs-modal.component';
import { SeletorArquivoComponent } from '../../shared/seletor-arquivo/seletor-arquivo.component';
import { SegmentoService } from '../../services/segmento.service';

/** Casa da equipe do produto (papel Suporte): dados do cliente deste ambiente (nome, segmento, acesso liberado/suspenso) e
 * um diagnóstico das integrações. Tudo o que for salvo aqui fica no histórico como feito pelo Suporte. */
@Component({
  selector: 'app-plataforma',
  imports: [FormsModule, RouterLink, LogsModalComponent, SeletorArquivoComponent],
  templateUrl: './plataforma.component.html',
  styleUrl: './plataforma.component.scss'
})
export class PlataformaComponent implements OnInit {
  private readonly plataformaService = inject(PlataformaService);
  private readonly configuracaoEscolaService = inject(ConfiguracaoEscolaService);
  private readonly notificacao = inject(NotificacaoService);
  protected readonly segmentoService = inject(SegmentoService);

  readonly enviandoLogo = signal(false);

  // Administradores da escola (convite por e-mail)
  readonly admins = signal<AdminEscola[]>([]);
  readonly criandoAdmin = signal(false);
  readonly processandoAdminId = signal<string | null>(null);
  readonly cancelandoConviteId = signal<string | null>(null);
  novoAdminNome = '';
  novoAdminEmail = '';
  protected readonly formatarDataHora = formatarDataHoraAbsoluta;
  /** Mesmo limite do backend. */
  private readonly tamanhoMaximoLogo = 2 * 1024 * 1024;

  readonly carregando = signal(true);
  readonly salvando = signal(false);
  readonly cliente = signal<ClientePlataforma | null>(null);
  readonly diagnostico = signal<DiagnosticoPlataforma | null>(null);
  readonly historicoAberto = signal(false);
  /** Suspender o acesso derruba todos os usuários do cliente: pede confirmação antes. */
  readonly confirmandoSuspensao = signal(false);

  nome = '';
  segmento: SegmentoCliente = 'Escola';
  acessoLiberado = true;

  protected readonly segmentos = (Object.keys(ROTULOS_SEGMENTO) as SegmentoCliente[]).map((valor) => ({ valor, rotulo: ROTULOS_SEGMENTO[valor] }));

  ngOnInit(): void {
    this.plataformaService.obterCliente().subscribe({
      next: (cliente) => {
        this.aplicar(cliente);
        this.carregando.set(false);
      },
      error: () => {
        this.carregando.set(false);
        this.notificacao.erro('Não foi possível carregar os dados do cliente.');
      }
    });
    this.carregarAdmins();
    // Complemento informativo: se falhar, o resto da tela segue funcionando.
    this.plataformaService.diagnostico().subscribe({ next: (d) => this.diagnostico.set(d), error: () => undefined });
  }

  private carregarAdmins(): void {
    this.plataformaService.listarAdmins().subscribe({
      next: (admins) => this.admins.set(admins),
      error: () => this.notificacao.erro('Não foi possível carregar os administradores.')
    });
  }

  criarAdmin(): void {
    const nome = this.novoAdminNome.trim();
    const email = this.novoAdminEmail.trim();
    if (!nome || !email) {
      this.notificacao.erro('Informe o nome e o e-mail do administrador.');
      return;
    }

    this.criandoAdmin.set(true);
    this.plataformaService.criarAdmin(nome, email).subscribe({
      next: (resultado) => {
        this.criandoAdmin.set(false);
        this.novoAdminNome = '';
        this.novoAdminEmail = '';
        this.avisarEnvio(resultado, 'Convite enviado para ' + email + '.');
        this.carregarAdmins();
      },
      error: (resposta) => {
        this.criandoAdmin.set(false);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível criar o administrador.');
      }
    });
  }

  reenviar(admin: AdminEscola): void {
    this.processandoAdminId.set(admin.id);
    this.plataformaService.reenviarAdmin(admin.id).subscribe({
      next: (resultado) => {
        this.processandoAdminId.set(null);
        this.avisarEnvio(resultado, admin.pendente ? 'Convite reenviado.' : 'Link de nova senha enviado.');
        this.carregarAdmins();
      },
      error: (resposta) => {
        this.processandoAdminId.set(null);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível reenviar.');
      }
    });
  }

  confirmarCancelamentoConvite(admin: AdminEscola): void {
    this.processandoAdminId.set(admin.id);
    this.plataformaService.cancelarConvite(admin.id).subscribe({
      next: () => {
        this.processandoAdminId.set(null);
        this.cancelandoConviteId.set(null);
        this.notificacao.sucesso('Convite cancelado.');
        this.carregarAdmins();
      },
      error: (resposta) => {
        this.processandoAdminId.set(null);
        this.cancelandoConviteId.set(null);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível cancelar o convite.');
      }
    });
  }

  /** Entregue = sucesso; criado mas não entregue (sem SMTP, falha de envio) = aviso, porque a conta/link existem e dá pra reenviar. */
  private avisarEnvio(resultado: ResultadoEnvioAdmin, sucesso: string): void {
    if (resultado.emailEnviado) this.notificacao.sucesso(sucesso);
    else this.notificacao.info('Feito, mas o e-mail não foi entregue: ' + (resultado.aviso ?? 'tente reenviar.'));
  }

  private aplicar(cliente: ClientePlataforma): void {
    this.cliente.set(cliente);
    this.nome = cliente.nome;
    this.segmento = cliente.segmento;
    this.acessoLiberado = cliente.ativo;
  }

  get segmentoMudou(): boolean {
    return !!this.cliente() && this.segmento !== this.cliente()!.segmento;
  }

  salvar(): void {
    const atual = this.cliente();
    if (!atual) return;

    if (!this.nome.trim()) {
      this.notificacao.erro('Informe o nome do cliente.');
      return;
    }

    // Suspender é a única ação que tira o app de todo mundo: confirma antes (inline, como no resto do projeto).
    if (atual.ativo && !this.acessoLiberado && !this.confirmandoSuspensao()) {
      this.confirmandoSuspensao.set(true);
      return;
    }

    this.confirmandoSuspensao.set(false);
    this.salvando.set(true);
    this.plataformaService.editarCliente({ nome: this.nome.trim(), segmento: this.segmento, ativo: this.acessoLiberado }).subscribe({
      next: (cliente) => {
        this.aplicar(cliente);
        this.salvando.set(false);
        this.notificacao.sucesso('Dados do cliente salvos.');
        // O segmento muda o vocabulário e o menu do app: reaplica a configuração pública (e o cache) agora.
        this.configuracaoEscolaService.obter().subscribe({ error: () => undefined });
      },
      error: (resposta) => {
        this.salvando.set(false);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível salvar.');
      }
    });
  }

  aoEscolherLogo(arquivo: File): void {
    if (arquivo.size > this.tamanhoMaximoLogo) {
      this.notificacao.erro('A logo pode ter no máximo 2MB.');
      return;
    }

    this.enviandoLogo.set(true);
    this.plataformaService.enviarLogo(arquivo).subscribe({
      next: () => this.aposMudarLogo('Logo atualizada.'),
      error: (resposta) => {
        this.enviandoLogo.set(false);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível enviar a logo. Use PNG, JPEG ou WEBP de até 2MB.');
      }
    });
  }

  removerLogo(): void {
    this.enviandoLogo.set(true);
    this.plataformaService.removerLogo().subscribe({
      next: () => this.aposMudarLogo('Logo removida.'),
      error: () => {
        this.enviandoLogo.set(false);
        this.notificacao.erro('Não foi possível remover a logo.');
      }
    });
  }

  /** Recarrega a configuração pública: é ela que alimenta o menu e o login (e o cache) com a logo nova. */
  private aposMudarLogo(mensagem: string): void {
    this.configuracaoEscolaService.obter().subscribe({
      next: () => {
        this.enviandoLogo.set(false);
        this.notificacao.sucesso(mensagem);
      },
      error: () => this.enviandoLogo.set(false)
    });
  }

  cancelarSuspensao(): void {
    this.confirmandoSuspensao.set(false);
    this.acessoLiberado = true;
  }

  abrirHistorico(): void {
    this.historicoAberto.set(true);
  }

  fecharHistorico(): void {
    this.historicoAberto.set(false);
  }
}
