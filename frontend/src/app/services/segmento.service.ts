import { Injectable, computed, signal } from '@angular/core';

import { environment } from '../../environments/environment';
import { resolverFotoUrl } from '../shared/registro-rotina-display';

export type Segmento = 'escola' | 'clube';

/** Marca do produto (a plataforma), usada onde ainda não há escola: tela de login, menu sem sessão. Espelho de MarcaProduto.Nome no backend. */
export const NOME_PRODUTO = 'Gestor Tático';
/** Símbolo da marca (public/marca), mostrado nas telas sem sessão quando não há logo de escola. */
export const SIMBOLO_PRODUTO = 'marca/simbolo.svg';

/** Segmento vindo do cliente (banco), guardado pro app abrir no vocabulário certo mesmo sem rede (PWA). */
const CHAVE_CACHE = 'escola.segmento';
/** Sobreposição manual, só pra comparar as duas experiências no mesmo ambiente de dev sem recompilar. */
const CHAVE_OVERRIDE = 'rotinaEscola.segmento';
/** Logo do cliente guardada pro app abrir já com ela (PWA offline, sem piscar o ícone padrão). */
const CHAVE_LOGO = 'escola.logoUrl';
/** Nome da escola da sessão, guardado pro menu abrir já com ele (sem piscar o nome genérico). */
const CHAVE_NOME = 'escola.nome';

function normalizar(valor: string | null | undefined): Segmento | null {
  const texto = valor?.toLowerCase();
  return texto === 'clube' || texto === 'escola' ? texto : null;
}

/**
 * Um mesmo código-base atende dois tipos de cliente (escola infantil e clube/academia). Quem define qual é
 * é o **cliente no banco** (`Clientes.Segmento`), entregue por `GET /api/configuracao/escola` e aplicado na
 * inicialização do app por `ConfiguracaoEscolaService` (antes de qualquer tela renderizar). Isso aqui só troca
 * vocabulário e visibilidade de tela. `environment.segmento` é apenas o último recurso (API fora do ar e nada
 * em cache); o override do localStorage vence tudo, e só existe pra dev.
 */
@Injectable({ providedIn: 'root' })
export class SegmentoService {
  private readonly doCliente = signal<Segmento>(this.lerCache() ?? this.padraoDoAmbiente());
  private readonly override = signal<Segmento | null>(this.lerOverride());

  readonly segmento = computed<Segmento>(() => this.override() ?? this.doCliente());

  private readonly _logoUrl = signal<string | null>(this.lerLogo());
  /** URL completa da logo do cliente (ao lado do nome do app), ou nulo — aí vale o ícone padrão do segmento. */
  readonly logoUrl = computed(() => {
    const caminho = this._logoUrl();
    return caminho ? resolverFotoUrl(caminho) : null;
  });

  readonly ehClube = computed(() => this.segmento() === 'clube');

  readonly rotuloPessoa = computed(() => (this.ehClube() ? 'Atleta' : 'Aluno'));
  readonly rotuloPessoaPlural = computed(() => (this.ehClube() ? 'Atletas' : 'Alunos'));
  readonly rotuloCadastro = computed(() => (this.ehClube() ? 'Atletas' : 'Matrícula'));
  readonly rotuloResponsaveis = computed(() => (this.ehClube() ? 'Contatos' : 'Responsáveis'));
  readonly rotuloPortal = computed(() => (this.ehClube() ? 'Portal da Família' : 'Portal dos Pais'));
  private readonly _nomeEscola = signal<string | null>(this.lerNomeEscola());
  /** Nome no topo do menu: o da escola da sessão; sem login (tela de login de todas as escolas), a marca do produto. */
  readonly nomeApp = computed(() => this._nomeEscola() ?? NOME_PRODUTO);
  /** Sem escola na sessão: as telas de login/senha mostram o símbolo da marca (SIMBOLO_PRODUTO) em vez do ícone. */
  readonly semEscola = computed(() => this._nomeEscola() === null);
  readonly simboloProduto = SIMBOLO_PRODUTO;
  /** Sem escola na sessão vale o ícone da marca (futebol); com escola, o do segmento dela. */
  readonly iconeApp = computed(() => (this._nomeEscola() === null || this.ehClube() ? '⚽' : '🏫'));

  readonly mostrarRotinaDiaria = computed(() => !this.ehClube());
  readonly mostrarDiarioClasse = computed(() => !this.ehClube());
  readonly mostrarCompeticoes = computed(() => this.ehClube());

  /** Aplica o segmento do cliente (vindo da API) e guarda em cache. Não mexe no override de dev. */
  aplicarDoCliente(valor: string | null | undefined): void {
    const segmento = normalizar(valor);
    if (!segmento) return;
    this.doCliente.set(segmento);
    try {
      localStorage.setItem(CHAVE_CACHE, segmento);
    } catch {
      // localStorage indisponível — segue só em memória
    }
  }

  /** Aplica a logo do cliente (vinda da API) e guarda em cache. */
  /** Nome da escola vindo da configuração (nulo sem login). */
  aplicarNomeEscola(nome: string | null | undefined): void {
    const valor = nome?.trim() || null;
    this._nomeEscola.set(valor);
    try {
      if (valor) localStorage.setItem(CHAVE_NOME, valor);
      else localStorage.removeItem(CHAVE_NOME);
    } catch {
      // localStorage indisponível — segue só em memória
    }
  }

  private lerNomeEscola(): string | null {
    try {
      return localStorage.getItem(CHAVE_NOME);
    } catch {
      return null;
    }
  }

  aplicarLogo(caminho: string | null | undefined): void {
    this._logoUrl.set(caminho || null);
    try {
      if (caminho) localStorage.setItem(CHAVE_LOGO, caminho);
      else localStorage.removeItem(CHAVE_LOGO);
    } catch {
      // localStorage indisponível — segue só em memória
    }
  }

  /** Override de dev (persiste no localStorage até ser limpo com `definir(null)`). */
  definir(segmento: Segmento | null): void {
    this.override.set(segmento);
    try {
      if (segmento) localStorage.setItem(CHAVE_OVERRIDE, segmento);
      else localStorage.removeItem(CHAVE_OVERRIDE);
    } catch {
      // localStorage indisponível — segue só em memória
    }
  }

  private lerCache(): Segmento | null {
    try {
      return normalizar(localStorage.getItem(CHAVE_CACHE));
    } catch {
      return null;
    }
  }

  private lerLogo(): string | null {
    try {
      return localStorage.getItem(CHAVE_LOGO);
    } catch {
      return null;
    }
  }

  private lerOverride(): Segmento | null {
    try {
      return normalizar(localStorage.getItem(CHAVE_OVERRIDE));
    } catch {
      return null;
    }
  }

  private padraoDoAmbiente(): Segmento {
    return normalizar((environment as { segmento?: string }).segmento) ?? 'escola';
  }
}
