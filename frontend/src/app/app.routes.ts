import { Routes } from '@angular/router';

import { configuracaoGuard, equipeGuard, financeiroGuard, gestaoGuard, portalGuard, redirecionamentoInicialGuard, suporteGuard, termoPortalGuard } from './services/auth.guards';

export const routes: Routes = [
  { path: '', pathMatch: 'full', canActivate: [redirecionamentoInicialGuard], children: [] },
  { path: 'portal', pathMatch: 'full', redirectTo: 'entrar' },
  {
    path: 'entrar',
    data: { titulo: 'Rotina Escola' },
    loadComponent: () => import('./pages/entrar/entrar.component').then((m) => m.EntrarComponent)
  },
  {
    path: 'esqueci-senha',
    data: { titulo: 'Rotina Escola' },
    loadComponent: () => import('./pages/esqueci-senha/esqueci-senha.component').then((m) => m.EsqueciSenhaComponent)
  },
  {
    path: 'redefinir-senha',
    data: { titulo: 'Rotina Escola' },
    loadComponent: () => import('./pages/redefinir-senha/redefinir-senha.component').then((m) => m.RedefinirSenhaComponent)
  },
  {
    path: 'dashboard',
    pathMatch: 'full',
    canActivate: [equipeGuard],
    data: { titulo: 'Dashboard' },
    loadComponent: () => import('./pages/dashboard/dashboard.component').then((m) => m.DashboardComponent)
  },
  {
    path: 'unidades',
    pathMatch: 'full',
    canActivate: [equipeGuard],
    data: { titulo: 'Unidades' },
    loadComponent: () => import('./pages/unidades-lista/unidades-lista.component').then((m) => m.UnidadesListaComponent)
  },
  {
    path: 'unidades/nova',
    canActivate: [gestaoGuard],
    data: { titulo: 'Nova unidade' },
    loadComponent: () =>
      import('./pages/unidade-formulario/unidade-formulario.component').then((m) => m.UnidadeFormularioComponent)
  },
  {
    path: 'unidades/:id/editar',
    canActivate: [gestaoGuard],
    data: { titulo: 'Editar unidade' },
    loadComponent: () =>
      import('./pages/unidade-formulario/unidade-formulario.component').then((m) => m.UnidadeFormularioComponent)
  },
  {
    path: 'alunos',
    canActivate: [equipeGuard],
    data: { titulo: 'Turma' },
    loadComponent: () => import('./pages/alunos-lista/alunos-lista.component').then((m) => m.AlunosListaComponent)
  },
  {
    path: 'alunos/:id',
    canActivate: [equipeGuard],
    data: { titulo: 'Rotina do aluno' },
    loadComponent: () => import('./pages/aluno-rotina/aluno-rotina.component').then((m) => m.AlunoRotinaComponent)
  },
  {
    path: 'turmas',
    pathMatch: 'full',
    canActivate: [equipeGuard],
    data: { titulo: 'Turmas' },
    loadComponent: () => import('./pages/turmas-lista/turmas-lista.component').then((m) => m.TurmasListaComponent)
  },
  {
    path: 'turmas/nova',
    canActivate: [gestaoGuard],
    data: { titulo: 'Nova turma' },
    loadComponent: () =>
      import('./pages/turma-formulario/turma-formulario.component').then((m) => m.TurmaFormularioComponent)
  },
  {
    path: 'turmas/:id/editar',
    canActivate: [gestaoGuard],
    data: { titulo: 'Editar turma' },
    loadComponent: () =>
      import('./pages/turma-formulario/turma-formulario.component').then((m) => m.TurmaFormularioComponent)
  },
  {
    path: 'matricula',
    pathMatch: 'full',
    canActivate: [equipeGuard],
    data: { titulo: 'Matrícula' },
    loadComponent: () => import('./pages/matricula-lista/matricula-lista.component').then((m) => m.MatriculaListaComponent)
  },
  {
    path: 'matricula/novo',
    canActivate: [gestaoGuard],
    data: { titulo: 'Novo aluno' },
    loadComponent: () =>
      import('./pages/matricula-formulario/matricula-formulario.component').then((m) => m.MatriculaFormularioComponent)
  },
  {
    path: 'matricula/:id/editar',
    canActivate: [gestaoGuard],
    data: { titulo: 'Editar aluno' },
    loadComponent: () =>
      import('./pages/matricula-formulario/matricula-formulario.component').then((m) => m.MatriculaFormularioComponent)
  },
  {
    path: 'usuarios',
    pathMatch: 'full',
    canActivate: [gestaoGuard],
    data: { titulo: 'Usuários' },
    loadComponent: () => import('./pages/usuarios-lista/usuarios-lista.component').then((m) => m.UsuariosListaComponent)
  },
  {
    path: 'plataforma',
    pathMatch: 'full',
    canActivate: [suporteGuard],
    data: { titulo: 'Plataforma' },
    loadComponent: () => import('./pages/plataforma/plataforma.component').then((m) => m.PlataformaComponent)
  },
  {
    path: 'configuracoes',
    pathMatch: 'full',
    redirectTo: 'configuracoes/geral'
  },
  {
    path: 'configuracoes/geral',
    pathMatch: 'full',
    canActivate: [configuracaoGuard],
    data: { titulo: 'Configurações gerais' },
    loadComponent: () =>
      import('./pages/configuracao-geral/configuracao-geral.component').then((m) => m.ConfiguracaoGeralComponent)
  },
  {
    path: 'configuracoes/financeiro',
    pathMatch: 'full',
    canActivate: [configuracaoGuard],
    data: { titulo: 'Configurações financeiras' },
    loadComponent: () =>
      import('./pages/configuracao-financeira/configuracao-financeira.component').then((m) => m.ConfiguracaoFinanceiraComponent)
  },
  {
    path: 'configuracoes/pix',
    pathMatch: 'full',
    redirectTo: 'configuracoes/financeiro'
  },
  {
    path: 'chamada',
    pathMatch: 'full',
    canActivate: [equipeGuard],
    data: { titulo: 'Chamada' },
    loadComponent: () => import('./pages/chamada/chamada.component').then((m) => m.ChamadaComponent)
  },
  {
    path: 'diario',
    pathMatch: 'full',
    canActivate: [equipeGuard],
    data: { titulo: 'Diário de Classe' },
    loadComponent: () => import('./pages/diario-classe/diario-classe.component').then((m) => m.DiarioClasseComponent)
  },
  {
    path: 'financeiro',
    pathMatch: 'full',
    redirectTo: 'financeiro/mensalidades'
  },
  {
    path: 'financeiro/mensalidades',
    pathMatch: 'full',
    canActivate: [financeiroGuard],
    data: { titulo: 'Mensalidades' },
    loadComponent: () =>
      import('./pages/financeiro-lista/financeiro-lista.component').then((m) => m.FinanceiroListaComponent)
  },
  {
    path: 'financeiro/contas-pagar',
    pathMatch: 'full',
    canActivate: [financeiroGuard],
    data: { titulo: 'Contas a Pagar' },
    loadComponent: () =>
      import('./pages/contas-pagar-lista/contas-pagar-lista.component').then((m) => m.ContasPagarListaComponent)
  },
  {
    path: 'financeiro/contas-receber',
    pathMatch: 'full',
    canActivate: [financeiroGuard],
    data: { titulo: 'Contas a Receber' },
    loadComponent: () =>
      import('./pages/contas-receber-lista/contas-receber-lista.component').then((m) => m.ContasReceberListaComponent)
  },
  {
    path: 'fornecedores',
    pathMatch: 'full',
    canActivate: [financeiroGuard],
    data: { titulo: 'Fornecedores' },
    loadComponent: () =>
      import('./pages/fornecedores-lista/fornecedores-lista.component').then((m) => m.FornecedoresListaComponent)
  },
  {
    path: 'fornecedores/novo',
    canActivate: [financeiroGuard],
    data: { titulo: 'Novo fornecedor' },
    loadComponent: () =>
      import('./pages/fornecedor-formulario/fornecedor-formulario.component').then((m) => m.FornecedorFormularioComponent)
  },
  {
    path: 'fornecedores/:id/editar',
    canActivate: [financeiroGuard],
    data: { titulo: 'Editar fornecedor' },
    loadComponent: () =>
      import('./pages/fornecedor-formulario/fornecedor-formulario.component').then((m) => m.FornecedorFormularioComponent)
  },
  {
    path: 'jogos',
    pathMatch: 'full',
    canActivate: [equipeGuard],
    data: { titulo: 'Jogos' },
    loadComponent: () => import('./pages/jogos-lista/jogos-lista.component').then((m) => m.JogosListaComponent)
  },
  {
    path: 'jogos/novo',
    canActivate: [equipeGuard],
    data: { titulo: 'Novo jogo' },
    loadComponent: () => import('./pages/jogo-formulario/jogo-formulario.component').then((m) => m.JogoFormularioComponent)
  },
  {
    path: 'jogos/:id/editar',
    canActivate: [equipeGuard],
    data: { titulo: 'Jogo' },
    loadComponent: () => import('./pages/jogo-formulario/jogo-formulario.component').then((m) => m.JogoFormularioComponent)
  },
  {
    path: 'campeonatos',
    pathMatch: 'full',
    canActivate: [equipeGuard],
    data: { titulo: 'Campeonatos' },
    loadComponent: () =>
      import('./pages/campeonatos-lista/campeonatos-lista.component').then((m) => m.CampeonatosListaComponent)
  },
  {
    path: 'campeonatos/novo',
    canActivate: [gestaoGuard],
    data: { titulo: 'Novo campeonato' },
    loadComponent: () =>
      import('./pages/campeonato-formulario/campeonato-formulario.component').then((m) => m.CampeonatoFormularioComponent)
  },
  {
    path: 'campeonatos/:id/editar',
    canActivate: [gestaoGuard],
    data: { titulo: 'Editar campeonato' },
    loadComponent: () =>
      import('./pages/campeonato-formulario/campeonato-formulario.component').then((m) => m.CampeonatoFormularioComponent)
  },
  {
    path: 'campeonatos/:id',
    canActivate: [equipeGuard],
    data: { titulo: 'Campeonato' },
    loadComponent: () =>
      import('./pages/campeonato-detalhe/campeonato-detalhe.component').then((m) => m.CampeonatoDetalheComponent)
  },
  {
    path: 'estoque',
    pathMatch: 'full',
    redirectTo: 'estoque/produtos'
  },
  {
    path: 'estoque/produtos',
    pathMatch: 'full',
    canActivate: [financeiroGuard],
    data: { titulo: 'Produtos' },
    loadComponent: () => import('./pages/produtos-lista/produtos-lista.component').then((m) => m.ProdutosListaComponent)
  },
  {
    path: 'estoque/produtos/novo',
    canActivate: [financeiroGuard],
    data: { titulo: 'Novo produto' },
    loadComponent: () =>
      import('./pages/produto-formulario/produto-formulario.component').then((m) => m.ProdutoFormularioComponent)
  },
  {
    path: 'estoque/produtos/:id/editar',
    canActivate: [financeiroGuard],
    data: { titulo: 'Editar produto' },
    loadComponent: () =>
      import('./pages/produto-formulario/produto-formulario.component').then((m) => m.ProdutoFormularioComponent)
  },
  {
    path: 'estoque/movimentacoes',
    pathMatch: 'full',
    canActivate: [financeiroGuard],
    data: { titulo: 'Movimentações de estoque' },
    loadComponent: () =>
      import('./pages/movimentacoes-estoque/movimentacoes-estoque.component').then((m) => m.MovimentacoesEstoqueComponent)
  },
  {
    path: 'portal/termo',
    canActivate: [portalGuard],
    data: { titulo: 'Termo de matrícula' },
    loadComponent: () => import('./pages/portal-termo/portal-termo.component').then((m) => m.PortalTermoComponent)
  },
  {
    path: 'portal/filhos',
    canActivate: [portalGuard, termoPortalGuard],
    data: { titulo: 'Meus filhos' },
    loadComponent: () => import('./pages/portal-filhos/portal-filhos.component').then((m) => m.PortalFilhosComponent)
  },
  {
    path: 'portal/alunos/:id',
    canActivate: [portalGuard, termoPortalGuard],
    data: { titulo: 'Rotina do dia' },
    loadComponent: () => import('./pages/portal-aluno/portal-aluno.component').then((m) => m.PortalAlunoComponent)
  },
  {
    path: 'portal/financeiro',
    canActivate: [portalGuard, termoPortalGuard],
    data: { titulo: 'Financeiro' },
    loadComponent: () =>
      import('./pages/portal-financeiro/portal-financeiro.component').then((m) => m.PortalFinanceiroComponent)
  }
];
