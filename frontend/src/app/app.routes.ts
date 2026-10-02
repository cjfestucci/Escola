import { Routes } from '@angular/router';

import { equipeGuard, financeiroGuard, gestaoGuard, portalGuard, redirecionamentoInicialGuard } from './services/auth.guards';

export const routes: Routes = [
  { path: '', pathMatch: 'full', canActivate: [redirecionamentoInicialGuard], children: [] },
  { path: 'portal', pathMatch: 'full', redirectTo: 'entrar' },
  {
    path: 'entrar',
    data: { titulo: 'Rotina Escola' },
    loadComponent: () => import('./pages/entrar/entrar.component').then((m) => m.EntrarComponent)
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
    path: 'configuracoes',
    pathMatch: 'full',
    redirectTo: 'configuracoes/geral'
  },
  {
    path: 'configuracoes/geral',
    pathMatch: 'full',
    canActivate: [gestaoGuard],
    data: { titulo: 'Configurações gerais' },
    loadComponent: () =>
      import('./pages/configuracao-geral/configuracao-geral.component').then((m) => m.ConfiguracaoGeralComponent)
  },
  {
    path: 'configuracoes/pix',
    pathMatch: 'full',
    canActivate: [gestaoGuard],
    data: { titulo: 'Configuração Pix' },
    loadComponent: () =>
      import('./pages/configuracao-pix/configuracao-pix.component').then((m) => m.ConfiguracaoPixComponent)
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
    path: 'portal/filhos',
    canActivate: [portalGuard],
    data: { titulo: 'Meus filhos' },
    loadComponent: () => import('./pages/portal-filhos/portal-filhos.component').then((m) => m.PortalFilhosComponent)
  },
  {
    path: 'portal/alunos/:id',
    canActivate: [portalGuard],
    data: { titulo: 'Rotina do dia' },
    loadComponent: () => import('./pages/portal-aluno/portal-aluno.component').then((m) => m.PortalAlunoComponent)
  },
  {
    path: 'portal/financeiro',
    canActivate: [portalGuard],
    data: { titulo: 'Financeiro' },
    loadComponent: () =>
      import('./pages/portal-financeiro/portal-financeiro.component').then((m) => m.PortalFinanceiroComponent)
  }
];
