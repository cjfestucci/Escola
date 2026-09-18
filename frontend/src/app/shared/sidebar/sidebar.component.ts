import { Component, inject, input, output } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';

import { ContextoService } from '../../services/contexto.service';
import { SessaoService } from '../../services/sessao.service';

interface ItemNav {
  rotulo: string;
  rota: string;
  icone: 'home' | 'users' | 'book' | 'wallet';
}

const ITENS_EDUCADOR: ItemNav[] = [{ rotulo: 'Turma', rota: '/alunos', icone: 'users' }];
const ITENS_PORTAL: ItemNav[] = [{ rotulo: 'Meus Filhos', rota: '/portal/filhos', icone: 'home' }];
const ITENS_EM_BREVE: ItemNav[] = [
  { rotulo: 'Diário de Classe', rota: '', icone: 'book' },
  { rotulo: 'Financeiro', rota: '', icone: 'wallet' }
];

@Component({
  selector: 'app-sidebar',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss'
})
export class SidebarComponent {
  private readonly router = inject(Router);
  protected readonly contextoService = inject(ContextoService);
  protected readonly sessao = inject(SessaoService);

  readonly aberto = input(false);
  readonly fechar = output<void>();

  protected readonly itensEmBreve = ITENS_EM_BREVE;

  protected itensPrincipais(): ItemNav[] {
    return this.contextoService.contexto() === 'portal' ? ITENS_PORTAL : ITENS_EDUCADOR;
  }

  trocarArea(): void {
    const destino = this.contextoService.contexto() === 'portal' ? '/entrar' : '/portal';
    this.router.navigateByUrl(destino);
    this.fechar.emit();
  }

  aoNavegar(): void {
    this.fechar.emit();
  }
}
