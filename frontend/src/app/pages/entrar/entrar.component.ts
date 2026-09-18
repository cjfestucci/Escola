import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';

import { Usuario } from '../../models/usuario.model';
import { SessaoService } from '../../services/sessao.service';
import { UsuarioService } from '../../services/usuario.service';

@Component({
  selector: 'app-entrar',
  imports: [],
  templateUrl: './entrar.component.html',
  styleUrl: './entrar.component.scss'
})
export class EntrarComponent implements OnInit {
  private readonly usuarioService = inject(UsuarioService);
  private readonly sessao = inject(SessaoService);
  private readonly router = inject(Router);

  readonly educadores = signal<Usuario[]>([]);
  readonly carregando = signal(true);
  readonly erro = signal(false);

  ngOnInit(): void {
    this.usuarioService.listarEducadores().subscribe({
      next: (educadores) => {
        this.educadores.set(educadores);
        this.carregando.set(false);
      },
      error: () => {
        this.erro.set(true);
        this.carregando.set(false);
      }
    });
  }

  selecionar(educador: Usuario): void {
    this.sessao.definirEducador(educador.id, educador.nome);
    this.router.navigateByUrl('/alunos');
  }
}
