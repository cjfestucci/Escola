import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'alunos' },
  {
    path: 'entrar',
    data: { titulo: 'Entrar' },
    loadComponent: () => import('./pages/entrar/entrar.component').then((m) => m.EntrarComponent)
  },
  {
    path: 'alunos',
    data: { titulo: 'Turma' },
    loadComponent: () => import('./pages/alunos-lista/alunos-lista.component').then((m) => m.AlunosListaComponent)
  },
  {
    path: 'alunos/:id',
    data: { titulo: 'Rotina do aluno' },
    loadComponent: () => import('./pages/aluno-rotina/aluno-rotina.component').then((m) => m.AlunoRotinaComponent)
  },
  {
    path: 'portal',
    pathMatch: 'full',
    data: { titulo: 'Portal dos Pais' },
    loadComponent: () => import('./pages/portal-entrar/portal-entrar.component').then((m) => m.PortalEntrarComponent)
  },
  {
    path: 'portal/filhos',
    data: { titulo: 'Meus filhos' },
    loadComponent: () => import('./pages/portal-filhos/portal-filhos.component').then((m) => m.PortalFilhosComponent)
  },
  {
    path: 'portal/alunos/:id',
    data: { titulo: 'Rotina do dia' },
    loadComponent: () => import('./pages/portal-aluno/portal-aluno.component').then((m) => m.PortalAlunoComponent)
  }
];
