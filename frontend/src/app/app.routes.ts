import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'alunos' },
  {
    path: 'entrar',
    loadComponent: () => import('./pages/entrar/entrar.component').then((m) => m.EntrarComponent)
  },
  {
    path: 'alunos',
    loadComponent: () => import('./pages/alunos-lista/alunos-lista.component').then((m) => m.AlunosListaComponent)
  },
  {
    path: 'alunos/:id',
    loadComponent: () => import('./pages/aluno-rotina/aluno-rotina.component').then((m) => m.AlunoRotinaComponent)
  }
];
