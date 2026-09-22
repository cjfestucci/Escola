import { Component, inject, input, output } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';

import { AuthService } from '../../services/auth.service';
import { ContextoService } from '../../services/contexto.service';
import { SessaoService } from '../../services/sessao.service';

interface ItemNav {
  rotulo: string;
  rota: string;
  icone: 'home' | 'users' | 'book' | 'wallet' | 'turmas' | 'matricula' | 'usuarios';
}

const ITENS_EDUCADOR: ItemNav[] = [
  { rotulo: 'Turma', rota: '/alunos', icone: 'users' },
  { rotulo: 'Diário de Classe', rota: '/diario', icone: 'book' },
  { rotulo: 'Matrícula', rota: '/matricula', icone: 'matricula' },
  { rotulo: 'Turmas (cadastro)', rota: '/turmas', icone: 'turmas' }
];
const ITEM_USUARIOS: ItemNav = { rotulo: 'Usuários', rota: '/usuarios', icone: 'usuarios' };
const ITENS_PORTAL: ItemNav[] = [{ rotulo: 'Meus Filhos', rota: '/portal/filhos', icone: 'home' }];
const ITENS_EM_BREVE: ItemNav[] = [{ rotulo: 'Financeiro', rota: '', icone: 'wallet' }];

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

  readonly aberto = input(false);
  readonly fechar = output<void>();

  protected readonly itensEmBreve = ITENS_EM_BREVE;

  protected itensPrincipais(): ItemNav[] {
    if (this.contextoService.contexto() === 'portal') return ITENS_PORTAL;
    return this.auth.ehGestao() ? [...ITENS_EDUCADOR, ITEM_USUARIOS] : ITENS_EDUCADOR;
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
