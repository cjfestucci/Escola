import { Injectable, computed, signal } from '@angular/core';

import { environment } from '../../environments/environment';

export type Segmento = 'escola' | 'clube';

const CHAVE_STORAGE = 'rotinaEscola.segmento';

/**
 * Um mesmo código-base atende dois tipos de cliente (escola infantil e clube/academia) —
 * cada implantação é isolada (banco e deploy próprios), então isso é só uma troca de
 * vocabulário/visibilidade de tela, não multi-tenant de verdade. Padrão vem de
 * `environment.segmento`; a sobreposição via localStorage existe só pra comparar as duas
 * experiências no mesmo ambiente de dev sem precisar recompilar.
 */
@Injectable({ providedIn: 'root' })
export class SegmentoService {
  private readonly _segmento = signal<Segmento>(this.lerInicial());
  readonly segmento = this._segmento.asReadonly();

  readonly ehClube = computed(() => this._segmento() === 'clube');

  readonly rotuloPessoa = computed(() => (this.ehClube() ? 'Atleta' : 'Aluno'));
  readonly rotuloPessoaPlural = computed(() => (this.ehClube() ? 'Atletas' : 'Alunos'));
  readonly rotuloCadastro = computed(() => (this.ehClube() ? 'Atletas' : 'Matrícula'));
  readonly rotuloResponsaveis = computed(() => (this.ehClube() ? 'Contatos' : 'Responsáveis'));
  readonly rotuloPortal = computed(() => (this.ehClube() ? 'Portal da Família' : 'Portal dos Pais'));
  readonly nomeApp = computed(() => (this.ehClube() ? 'Escola de Futebol' : 'Rotina Escola'));
  readonly iconeApp = computed(() => (this.ehClube() ? '⚽' : '🏫'));

  readonly mostrarRotinaDiaria = computed(() => !this.ehClube());
  readonly mostrarDiarioClasse = computed(() => !this.ehClube());
  readonly mostrarCompeticoes = computed(() => this.ehClube());

  definir(segmento: Segmento): void {
    this._segmento.set(segmento);
    try {
      localStorage.setItem(CHAVE_STORAGE, segmento);
    } catch {
      // localStorage indisponível — segue só em memória
    }
  }

  private lerInicial(): Segmento {
    try {
      const guardado = localStorage.getItem(CHAVE_STORAGE);
      if (guardado === 'clube' || guardado === 'escola') return guardado;
    } catch {
      // localStorage indisponível — segue com o padrão do ambiente
    }
    return (environment as { segmento?: Segmento }).segmento ?? 'escola';
  }
}
