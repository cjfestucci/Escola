import { Component, inject, input, output, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { filter } from 'rxjs';

import { AuthService } from '../../services/auth.service';
import { ContextoService } from '../../services/contexto.service';
import { SegmentoService } from '../../services/segmento.service';
import { SessaoService } from '../../services/sessao.service';

interface SubItemNav {
  rotulo: string;
  rota: string;
}

interface ItemNav {
  rotulo: string;
  rota?: string;
  icone: 'home' | 'users' | 'book' | 'wallet' | 'turmas' | 'matricula' | 'usuarios' | 'painel' | 'unidades' | 'fornecedores' | 'configuracoes' | 'estoque';
  subitens?: SubItemNav[];
}

const ITEM_DASHBOARD: ItemNav = { rotulo: 'Dashboard', rota: '/dashboard', icone: 'painel' };
const ITEM_UNIDADES: ItemNav = { rotulo: 'Unidades', rota: '/unidades', icone: 'unidades' };
const ITEM_TURMA_ROTINA: ItemNav = { rotulo: 'Turma', rota: '/alunos', icone: 'users' };
const ITEM_DIARIO: ItemNav = { rotulo: 'Diário de Classe', rota: '/diario', icone: 'book' };
const ITEM_TURMAS_CADASTRO: ItemNav = { rotulo: 'Turmas (cadastro)', rota: '/turmas', icone: 'turmas' };
const ITEM_FINANCEIRO: ItemNav = {
  rotulo: 'Financeiro',
  icone: 'wallet',
  subitens: [
    { rotulo: 'Mensalidades', rota: '/financeiro/mensalidades' },
    { rotulo: 'Contas a Pagar', rota: '/financeiro/contas-pagar' },
    { rotulo: 'Contas a Receber', rota: '/financeiro/contas-receber' }
  ]
};
const ITEM_FORNECEDORES: ItemNav = { rotulo: 'Fornecedores', rota: '/fornecedores', icone: 'fornecedores' };
const ITEM_ESTOQUE: ItemNav = {
  rotulo: 'Estoque',
  icone: 'estoque',
  subitens: [
    { rotulo: 'Produtos', rota: '/estoque/produtos' },
    { rotulo: 'Movimentações', rota: '/estoque/movimentacoes' }
  ]
};
const ITEM_USUARIOS: ItemNav = { rotulo: 'Usuários', rota: '/usuarios', icone: 'usuarios' };
const ITEM_CONFIGURACOES: ItemNav = {
  rotulo: 'Configurações',
  icone: 'configuracoes',
  subitens: [
    { rotulo: 'Geral', rota: '/configuracoes/geral' },
    { rotulo: 'Pix', rota: '/configuracoes/pix' }
  ]
};
const ITENS_PORTAL: ItemNav[] = [
  { rotulo: 'Meus Filhos', rota: '/portal/filhos', icone: 'home' },
  { rotulo: 'Financeiro', rota: '/portal/financeiro', icone: 'wallet' }
];

@Component({
  selector: 'app-sidebar',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss'
})
export class SidebarComponent {
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  protected readonly contextoService = inject(ContextoService);
  protected readonly sessao = inject(SessaoService);
  protected readonly segmentoService = inject(SegmentoService);

  readonly aberto = input(false);
  readonly fechar = output<void>();

  private readonly gruposExpandidos = signal<Set<string>>(new Set());

  constructor() {
    this.expandirGruposComRotaAtiva();
    this.router.events.pipe(filter((evento) => evento instanceof NavigationEnd)).subscribe(() => {
      this.expandirGruposComRotaAtiva();
    });
  }

  protected itensPrincipais(): ItemNav[] {
    if (this.contextoService.contexto() === 'portal') return ITENS_PORTAL;

    const itens: ItemNav[] = [ITEM_DASHBOARD, ITEM_UNIDADES];
    if (this.segmentoService.mostrarRotinaDiaria()) itens.push(ITEM_TURMA_ROTINA);
    if (this.segmentoService.mostrarDiarioClasse()) itens.push(ITEM_DIARIO);
    itens.push(ITEM_TURMAS_CADASTRO, { rotulo: this.segmentoService.rotuloCadastro(), rota: '/matricula', icone: 'matricula' });
    if (this.auth.ehFinanceiro()) itens.push(ITEM_FORNECEDORES, ITEM_FINANCEIRO, ITEM_ESTOQUE);
    if (this.auth.ehGestao()) itens.push(ITEM_USUARIOS, ITEM_CONFIGURACOES);
    return itens;
  }

  protected grupoExpandido(item: ItemNav): boolean {
    return this.gruposExpandidos().has(item.rotulo);
  }

  protected alternarGrupo(item: ItemNav): void {
    this.gruposExpandidos.update((atual) => {
      const proximo = new Set(atual);
      if (proximo.has(item.rotulo)) proximo.delete(item.rotulo);
      else proximo.add(item.rotulo);
      return proximo;
    });
  }

  private expandirGruposComRotaAtiva(): void {
    const url = this.router.url;
    const ativos = [ITEM_FINANCEIRO, ITEM_ESTOQUE, ITEM_CONFIGURACOES].filter((item) => item.subitens?.some((sub) => url.startsWith(sub.rota)));
    if (ativos.length === 0) return;
    this.gruposExpandidos.update((atual) => new Set([...atual, ...ativos.map((item) => item.rotulo)]));
  }

  sair(): void {
    this.auth.sair();
    this.sessao.limpar();
    this.router.navigateByUrl('/entrar');
    this.fechar.emit();
  }

  aoNavegar(): void {
    this.fechar.emit();
  }
}
