import { Component, OnInit, inject, input, output, signal } from '@angular/core';

import { LogAuditoria } from '../../models/log-auditoria.model';
import { LogAuditoriaService } from '../../services/log-auditoria.service';
import { formatarDataHoraAbsoluta } from '../data-utils';

const ROTULOS_ACAO: Record<LogAuditoria['acao'], string> = {
  Criado: 'Criado',
  Editado: 'Editado',
  Excluido: 'Excluído'
};

/** Modal genérico "Ver histórico" — toda tela com dado editável abre este componente passando
 * o tipo e o id da entidade, sem precisar de nenhuma tela nova por módulo. */
@Component({
  selector: 'app-logs-modal',
  imports: [],
  templateUrl: './logs-modal.component.html',
  styleUrl: './logs-modal.component.scss'
})
export class LogsModalComponent implements OnInit {
  private readonly logAuditoriaService = inject(LogAuditoriaService);

  /** Modo "um registro": passe entidadeTipo+entidadeId. */
  readonly entidadeTipo = input<string>();
  readonly entidadeId = input<string>();
  /** Modo "turma inteira, num dia": passe turmaId+data (YYYY-MM-DD) — pega tudo que aconteceu
   * naquele dia pra essa turma, inclusive registros já excluídos (não depende deles ainda existirem). */
  readonly turmaId = input<string>();
  readonly data = input<string>();
  readonly titulo = input('Histórico de alterações');
  readonly fechar = output<void>();

  readonly logs = signal<LogAuditoria[]>([]);
  readonly carregando = signal(true);
  readonly erro = signal(false);

  protected readonly formatarDataHoraAbsoluta = formatarDataHoraAbsoluta;

  ngOnInit(): void {
    const consulta$ =
      this.turmaId() && this.data()
        ? this.logAuditoriaService.listarPorTurma(this.turmaId()!, this.data()!)
        : this.logAuditoriaService.listar(this.entidadeTipo()!, this.entidadeId()!);

    consulta$.subscribe({
      next: (logs) => {
        this.logs.set(logs);
        this.carregando.set(false);
      },
      error: () => {
        this.erro.set(true);
        this.carregando.set(false);
      }
    });
  }

  protected rotuloAcao(acao: LogAuditoria['acao']): string {
    return ROTULOS_ACAO[acao] ?? acao;
  }

  protected linhasDetalhe(detalhe: string | null): string[] {
    return detalhe ? detalhe.split('\n') : [];
  }

  fecharModal(): void {
    this.fechar.emit();
  }
}
