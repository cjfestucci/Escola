import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { CampeonatoService } from '../../services/campeonato.service';
import { NotificacaoService } from '../../services/notificacao.service';
import { CalendarioComponent } from '../../shared/calendario/calendario.component';
import { formatarDataAbsoluta, hojeIso } from '../../shared/data-utils';

@Component({
  selector: 'app-campeonato-formulario',
  imports: [FormsModule, CalendarioComponent],
  templateUrl: './campeonato-formulario.component.html',
  styleUrl: './campeonato-formulario.component.scss'
})
export class CampeonatoFormularioComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly campeonatoService = inject(CampeonatoService);
  private readonly notificacao = inject(NotificacaoService);

  private campeonatoId: string | null = null;

  readonly carregando = signal(true);
  readonly salvando = signal(false);
  readonly calendarioAberto = signal<'inicio' | 'fim' | null>(null);

  nome = '';
  dataInicio = '';
  dataFim = '';
  observacao = '';
  amarelosParaSuspensao: number | null = 3;

  protected readonly formatarDataAbsoluta = formatarDataAbsoluta;
  protected readonly hojeIso = hojeIso;

  get titulo(): string {
    return this.campeonatoId ? 'Editar campeonato' : 'Novo campeonato';
  }

  ngOnInit(): void {
    this.campeonatoId = this.route.snapshot.paramMap.get('id');

    if (!this.campeonatoId) {
      this.carregando.set(false);
      return;
    }

    this.campeonatoService.obterPorId(this.campeonatoId).subscribe({
      next: (campeonato) => {
        this.nome = campeonato.nome;
        this.dataInicio = campeonato.dataInicio.slice(0, 10);
        this.dataFim = campeonato.dataFim?.slice(0, 10) ?? '';
        this.observacao = campeonato.observacao ?? '';
        this.amarelosParaSuspensao = campeonato.amarelosParaSuspensao;
        this.carregando.set(false);
      },
      error: () => {
        this.notificacao.erro('Não foi possível carregar o campeonato.');
        this.carregando.set(false);
      }
    });
  }

  alternarCalendario(qual: 'inicio' | 'fim'): void {
    this.calendarioAberto.set(this.calendarioAberto() === qual ? null : qual);
  }

  escolherData(qual: 'inicio' | 'fim', dataIso: string): void {
    if (qual === 'inicio') this.dataInicio = dataIso;
    else this.dataFim = dataIso;
    this.calendarioAberto.set(null);
  }

  limparDataFim(): void {
    this.dataFim = '';
  }

  salvar(): void {
    if (!this.nome.trim()) {
      this.notificacao.erro('Informe o nome do campeonato.');
      return;
    }
    if (!this.dataInicio) {
      this.notificacao.erro('Informe a data de início.');
      return;
    }
    if (this.dataFim && this.dataFim < this.dataInicio) {
      this.notificacao.erro('A data de término não pode ser anterior à data de início.');
      return;
    }

    // Campo numérico vazio vira null (ou string vazia, conforme o navegador): vazio = sem controle disciplinar.
    const amarelos =
      this.amarelosParaSuspensao === null || (this.amarelosParaSuspensao as unknown) === '' ? null : Number(this.amarelosParaSuspensao);
    if (amarelos !== null && (!Number.isInteger(amarelos) || amarelos < 1 || amarelos > 10)) {
      this.notificacao.erro('Informe os amarelos para suspensão como um número inteiro de 1 a 10, ou deixe em branco para não controlar.');
      return;
    }

    const payload = {
      nome: this.nome.trim(),
      dataInicio: this.dataInicio,
      dataFim: this.dataFim || null,
      observacao: this.observacao.trim() || null,
      amarelosParaSuspensao: amarelos
    };

    this.salvando.set(true);
    const requisicao$ = this.campeonatoId
      ? this.campeonatoService.editar(this.campeonatoId, payload)
      : this.campeonatoService.criar(payload);

    requisicao$.subscribe({
      next: () => {
        this.notificacao.sucesso('Campeonato salvo com sucesso.');
        this.router.navigateByUrl('/campeonatos');
      },
      error: (resposta) => {
        this.salvando.set(false);
        this.notificacao.erro(typeof resposta.error === 'string' ? resposta.error : 'Não foi possível salvar o campeonato.');
      }
    });
  }

  cancelar(): void {
    this.router.navigateByUrl('/campeonatos');
  }
}
