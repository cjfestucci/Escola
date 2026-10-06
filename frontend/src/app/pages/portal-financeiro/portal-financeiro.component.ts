import { DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { forkJoin } from 'rxjs';

import { Aluno } from '../../models/aluno.model';
import { Cobranca } from '../../models/cobranca.model';
import { FinanceiroService } from '../../services/financeiro.service';
import { NotificacaoService } from '../../services/notificacao.service';
import { ResponsavelService } from '../../services/responsavel.service';
import { SessaoService } from '../../services/sessao.service';
import { MESES_PT_BR, formatarDataAbsoluta, hojeIso } from '../../shared/data-utils';
import { gerarQrCodePix } from '../../shared/pix-qrcode';

type StatusFiltro = 'todos' | 'pendente' | 'pago' | 'atrasado';

@Component({
  selector: 'app-portal-financeiro',
  imports: [DecimalPipe, FormsModule],
  templateUrl: './portal-financeiro.component.html',
  styleUrl: './portal-financeiro.component.scss'
})
export class PortalFinanceiroComponent implements OnInit {
  private readonly responsavelService = inject(ResponsavelService);
  private readonly financeiroService = inject(FinanceiroService);
  private readonly notificacao = inject(NotificacaoService);
  private readonly sessao = inject(SessaoService);
  private readonly router = inject(Router);

  readonly filhos = signal<Aluno[]>([]);
  readonly cobrancas = signal<Cobranca[]>([]);
  readonly carregando = signal(true);

  readonly filtroFilhoId = signal('');
  readonly filtroStatus = signal<StatusFiltro>('todos');
  readonly filtroAno = signal('');
  readonly filtroMes = signal('');

  protected readonly meses = MESES_PT_BR;

  readonly pixAbertoId = signal<string | null>(null);
  readonly pixCarregando = signal(false);
  readonly pixCodigo = signal<string | null>(null);
  readonly pixAutomatico = signal(false);
  readonly pixQrCode = signal<string | null>(null);
  readonly pixCopiado = signal(false);

  protected readonly hojeIso = hojeIso;
  protected readonly formatarDataAbsoluta = formatarDataAbsoluta;

  readonly anosDisponiveis = computed(() => {
    const anos = new Set(this.cobrancas().map((c) => c.vencimento.slice(0, 4)));
    anos.add(hojeIso().slice(0, 4));
    return [...anos].sort((a, b) => b.localeCompare(a));
  });

  readonly cobrancasFiltradas = computed(() => {
    const filhoId = this.filtroFilhoId();
    const status = this.filtroStatus();
    const ano = this.filtroAno();
    const mes = this.filtroMes();
    const hoje = hojeIso();

    return this.cobrancas().filter((c) => {
      const bateFilho = !filhoId || c.alunoId === filhoId;
      const bateStatus = status === 'todos' || this.statusDe(c, hoje) === status;
      const bateAno = !ano || c.vencimento.slice(0, 4) === ano;
      const bateMes = !mes || c.vencimento.slice(5, 7) === mes;
      return bateFilho && bateStatus && bateAno && bateMes;
    });
  });

  readonly resumo = computed(() => {
    const hoje = hojeIso();
    const lista = this.cobrancas();
    const aVencerLista = lista.filter((c) => this.statusDe(c, hoje) === 'pendente');
    const atrasadasLista = lista.filter((c) => this.statusDe(c, hoje) === 'atrasado');
    const valorAVencer = aVencerLista.reduce((soma, c) => soma + c.valor, 0);
    const valorAtrasado = atrasadasLista.reduce((soma, c) => soma + c.valor, 0);
    return {
      aVencer: aVencerLista.length,
      valorAVencer,
      atrasadas: atrasadasLista.length,
      valorAtrasado,
      totalAberto: aVencerLista.length + atrasadasLista.length,
      totalEmAberto: valorAVencer + valorAtrasado
    };
  });

  ngOnInit(): void {
    const responsavelId = this.sessao.responsavelId();
    if (!responsavelId) {
      this.router.navigateByUrl('/portal');
      return;
    }

    this.responsavelService.listarFilhos(responsavelId).subscribe({
      next: (filhos) => {
        this.filhos.set(filhos);
        if (filhos.length === 0) {
          this.carregando.set(false);
          return;
        }

        forkJoin(filhos.map((f) => this.financeiroService.listarDoAluno(f.id))).subscribe({
          next: (listas) => {
            this.cobrancas.set(listas.flat().sort((a, b) => a.vencimento.localeCompare(b.vencimento)));
            this.carregando.set(false);
          },
          error: () => {
            this.carregando.set(false);
            this.notificacao.erro('Não foi possível carregar as cobranças.');
          }
        });
      },
      error: () => {
        this.carregando.set(false);
        this.notificacao.erro('Não foi possível carregar os filhos vinculados.');
      }
    });
  }

  statusDe(c: Cobranca, hoje: string): 'pago' | 'atrasado' | 'pendente' {
    if (c.paga) return 'pago';
    return c.vencimento < hoje ? 'atrasado' : 'pendente';
  }

  abrirPix(cobranca: Cobranca): void {
    this.pixAbertoId.set(cobranca.id);
    this.pixCodigo.set(null);
    this.pixAutomatico.set(false);
    this.pixQrCode.set(null);
    this.pixCopiado.set(false);
    this.pixCarregando.set(true);
    this.financeiroService.obterPix(cobranca.id).subscribe({
      next: (resposta) => {
        this.pixCodigo.set(resposta.codigoCopiaECola);
        this.pixAutomatico.set(resposta.automatico);
        this.pixCarregando.set(false);
        gerarQrCodePix(resposta.codigoCopiaECola).then((url) => this.pixQrCode.set(url));
      },
      error: (resposta) => {
        this.pixAbertoId.set(null);
        this.pixCarregando.set(false);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível gerar o código Pix.');
      }
    });
  }

  fecharPix(): void {
    this.pixAbertoId.set(null);
  }

  copiarPix(): void {
    const codigo = this.pixCodigo();
    if (!codigo) return;
    navigator.clipboard.writeText(codigo).then(() => {
      this.pixCopiado.set(true);
      setTimeout(() => this.pixCopiado.set(false), 2000);
    });
  }
}
