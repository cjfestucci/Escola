import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { Aluno } from '../../models/aluno.model';
import { FichaSaude } from '../../models/ficha-saude.model';
import { CategoriaRegistro, RegistroRotina } from '../../models/registro-rotina.model';
import { AlunoService } from '../../services/aluno.service';
import { FichaSaudeService } from '../../services/ficha-saude.service';
import { RotinaService } from '../../services/rotina.service';
import { SessaoService } from '../../services/sessao.service';
import {
  horaRegistro,
  iconeCategoria,
  resolverFotoUrls,
  rotuloCategoria,
  rotuloCategoriaCurto
} from '../../shared/registro-rotina-display';

@Component({
  selector: 'app-portal-aluno',
  imports: [],
  templateUrl: './portal-aluno.component.html',
  styleUrl: './portal-aluno.component.scss'
})
export class PortalAlunoComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly alunoService = inject(AlunoService);
  private readonly fichaSaudeService = inject(FichaSaudeService);
  private readonly rotinaService = inject(RotinaService);
  private readonly sessao = inject(SessaoService);

  private alunoId = '';

  readonly aluno = signal<Aluno | null>(null);
  readonly registros = signal<RegistroRotina[]>([]);
  readonly carregando = signal(true);

  readonly fichaSaude = signal<FichaSaude | null>(null);
  readonly mostrarFicha = signal(false);
  readonly fichaPreenchida = computed(() => {
    const f = this.fichaSaude();
    if (!f) return false;
    return !!(
      f.tipoSanguineo ||
      f.alergias ||
      f.restricoesAlimentares ||
      f.medicamentosEmUso ||
      f.condicoesSaude ||
      f.planoSaude ||
      f.pediatraNome ||
      f.contatoEmergenciaNome
    );
  });

  readonly filtroCategoria = signal<CategoriaRegistro | null>(null);
  readonly registrosFiltrados = computed(() => {
    const categoria = this.filtroCategoria();
    return categoria ? this.registros().filter((r) => r.categoria === categoria) : this.registros();
  });

  protected readonly rotuloCategoria = rotuloCategoria;
  protected readonly rotuloCategoriaCurto = rotuloCategoriaCurto;
  protected readonly iconeCategoria = iconeCategoria;
  protected readonly horaRegistro = horaRegistro;
  protected readonly resolverFotoUrls = resolverFotoUrls;
  protected readonly categoriasFiltro: CategoriaRegistro[] = ['Alimentacao', 'Sono', 'Higiene', 'Humor', 'Momento'];

  ngOnInit(): void {
    if (!this.sessao.responsavelId()) {
      this.router.navigateByUrl('/portal');
      return;
    }

    this.alunoId = this.route.snapshot.paramMap.get('id') ?? '';
    if (!this.alunoId) {
      this.router.navigateByUrl('/portal/filhos');
      return;
    }

    this.alunoService.obterAluno(this.alunoId).subscribe((aluno) => this.aluno.set(aluno));
    this.fichaSaudeService.obter(this.alunoId).subscribe((ficha) => this.fichaSaude.set(ficha));
    this.rotinaService.listarDoDia(this.alunoId).subscribe({
      next: (registros) => {
        this.registros.set(registros);
        this.carregando.set(false);
      },
      error: () => this.carregando.set(false)
    });
  }

  voltar(): void {
    this.router.navigateByUrl('/portal/filhos');
  }
}
