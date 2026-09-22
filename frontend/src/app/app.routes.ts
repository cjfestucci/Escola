import { Routes } from '@angular/router';

import { equipeGuard, financeiroGuard, gestaoGuard, portalGuard } from './services/auth.guards';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'alunos' },
  { path: 'portal', pathMatch: 'full', redirectTo: 'entrar' },
  {
    path: 'entrar',
    data: { titulo: 'Rotina Escola' },
    loadComponent: () => import('./pages/entrar/entrar.component').then((m) => m.EntrarComponent)
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
    path: 'diario',
    pathMatch: 'full',
    canActivate: [equipeGuard],
    data: { titulo: 'Diário de Classe' },
    loadComponent: () => import('./pages/diario-classe/diario-classe.component').then((m) => m.DiarioClasseComponent)
  },
  {
    path: 'financeiro',
    pathMatch: 'full',
    canActivate: [financeiroGuard],
    data: { titulo: 'Financeiro' },
    loadComponent: () =>
      import('./pages/financeiro-lista/financeiro-lista.component').then((m) => m.FinanceiroListaComponent)
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
  }
];
