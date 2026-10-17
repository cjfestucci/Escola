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
  icone: 'home' | 'users' | 'book' | 'wallet' | 'turmas' | 'matricula' | 'usuarios' | 'painel' | 'unidades' | 'fornecedores' | 'configuracoes' | 'estoque' | 'competicoes' | 'chamada' | 'plataforma' | 'ajuda';
  subitens?: SubItemNav[];
  /** Destacar só na rota exata (ex.: /plataforma não acende em /plataforma/clientes, que tem item próprio). */
  exato?: boolean;
}

const ITEM_DASHBOARD: ItemNav = { rotulo: 'Dashboard', rota: '/dashboard', icone: 'painel' };
const ITEM_UNIDADES: ItemNav = { rotulo: 'Unidades', rota: '/unidades', icone: 'unidades' };
const ITEM_TURMA_ROTINA: ItemNav = { rotulo: 'Turma', rota: '/alunos', icone: 'users' };
const ITEM_DIARIO: ItemNav = { rotulo: 'Diário de Classe', rota: '/diario', icone: 'book' };
const ITEM_CHAMADA: ItemNav = { rotulo: 'Chamada', rota: '/chamada', icone: 'chamada' };
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
const ITEM_MENSALIDADES: ItemNav = { rotulo: 'Mensalidades', rota: '/financeiro/mensalidades', icone: 'wallet' };
const ITEM_COMPETICOES: ItemNav = {
  rotulo: 'Competições',
  icone: 'competicoes',
  subitens: [
    { rotulo: 'Jogos', rota: '/jogos' },
    { rotulo: 'Campeonatos', rota: '/campeonatos' }
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
const ITEM_PLATAFORMA: ItemNav = { rotulo: 'Plataforma', rota: '/plataforma', icone: 'plataforma', exato: true };
/** Painel do Suporte com todos os clientes do produto (assinaturas e uso). */
const ITEM_CLIENTES: ItemNav = { rotulo: 'Clientes', rota: '/plataforma/clientes', icone: 'unidades' };
const ITEM_USUARIOS: ItemNav = { rotulo: 'Usuários', rota: '/usuarios', icone: 'usuarios' };
const ITEM_AJUDA: ItemNav = {
  rotulo: 'Ajuda',
  icone: 'ajuda',
  subitens: [
    { rotulo: 'Guias de uso', rota: '/ajuda/guias' },
    { rotulo: 'Fale conosco', rota: '/ajuda/fale-conosco' }
  ]
};
const ITEM_CONFIGURACOES: ItemNav = {
  rotulo: 'Configurações',
  icone: 'configuracoes',
  subitens: [
    { rotulo: 'Geral', rota: '/configuracoes/geral' },
    { rotulo: 'Financeiro', rota: '/configuracoes/financeiro' }
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
    // Pais falam com o clube, não com o produto: o "Fale com…" do portal mostra o contato do próprio clube.
    if (this.contextoService.contexto() === 'portal')
      return [...ITENS_PORTAL, { rotulo: this.segmentoService.ehClube() ? 'Fale com o clube' : 'Fale com a escola', rota: '/portal/contato', icone: 'ajuda' }];
    // Suporte (equipe do produto): só a Plataforma e as Configurações — nada de dado de aluno, saúde ou financeiro do cliente.
    if (this.contextoService.contexto() === 'suporte') return [ITEM_PLATAFORMA, ITEM_CLIENTES, this.itemConfiguracoes()];

    const itens: ItemNav[] = [ITEM_DASHBOARD, ITEM_UNIDADES];
    // A Secretaria não lança nada de turma (rotina, diário, chamada): esses itens nem aparecem pra ela.
    const lancaTurma = !this.auth.ehSecretaria();
    if (lancaTurma && this.segmentoService.mostrarRotinaDiaria()) itens.push(ITEM_TURMA_ROTINA);
    if (lancaTurma && this.segmentoService.mostrarDiarioClasse()) itens.push(ITEM_DIARIO);
    if (lancaTurma) itens.push(ITEM_CHAMADA);
    itens.push(ITEM_TURMAS_CADASTRO, { rotulo: this.segmentoService.rotuloCadastro(), rota: '/matricula', icone: 'matricula' });
    if (this.segmentoService.mostrarCompeticoes()) itens.push(ITEM_COMPETICOES);
    if (this.auth.ehFinanceiro()) itens.push(ITEM_FORNECEDORES, ITEM_FINANCEIRO, ITEM_ESTOQUE);
    // Secretaria: só Mensalidades (recebimento no balcão) e Estoque — sem contas a pagar/receber nem fornecedores.
    else if (this.auth.ehSecretaria()) itens.push(ITEM_MENSALIDADES, ITEM_ESTOQUE);
    if (this.auth.ehGestao()) itens.push(ITEM_USUARIOS, this.itemConfiguracoes());
    itens.push(ITEM_AJUDA);
    return itens;
  }

  /** A assinatura do clube é do dono da conta (Admin) e do Suporte — o Coordenador não vê. */
  private itemConfiguracoes(): ItemNav {
    if (!this.auth.podeEditarIdentidade()) return ITEM_CONFIGURACOES;
    return { ...ITEM_CONFIGURACOES, subitens: [...ITEM_CONFIGURACOES.subitens!, { rotulo: 'Assinatura', rota: '/configuracoes/assinatura' }] };
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
    const ativos = [ITEM_COMPETICOES, ITEM_FINANCEIRO, ITEM_ESTOQUE, this.itemConfiguracoes(), ITEM_AJUDA].filter((item) => item.subitens?.some((sub) => url.startsWith(sub.rota)));
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
