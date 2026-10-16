import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';

/** Os mesmos assuntos que a API aceita (`AjudaController.Assuntos`). */
export const ASSUNTOS_FALE_CONOSCO = ['Dúvida de uso', 'Problema ou erro', 'Sugestão', 'Assinatura e pagamento', 'Outro'] as const;
export type AssuntoFaleConosco = (typeof ASSUNTOS_FALE_CONOSCO)[number];

/** Os mesmos assuntos que a API aceita na mensagem do pai pro clube (`ContatoClubeController.Assuntos`). */
export const ASSUNTOS_CLUBE = ['Mensalidade e pagamento', 'Treinos e jogos', 'Cadastro e documentos', 'Outro'] as const;
export type AssuntoClube = (typeof ASSUNTOS_CLUBE)[number];

export const MENSAGEM_MINIMA = 10;
export const MENSAGEM_MAXIMA = 4000;

export interface ContatoUnidade {
  id: string;
  nome: string;
  endereco: string | null;
  telefone: string | null;
  whatsApp: string | null;
  email: string | null;
}

export interface ContatoClube {
  nomeClube: string;
  unidades: ContatoUnidade[];
  /** Atletas ativos do responsável logado, "Nome (Categoria)". */
  atletas: string[];
}

@Injectable({ providedIn: 'root' })
export class AjudaService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  /** Fale conosco da equipe do clube: vai por e-mail pro suporte do produto, com o contexto de quem escreveu. */
  enviarFaleConosco(assunto: AssuntoFaleConosco, mensagem: string, pagina: string | null): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/ajuda/fale-conosco`, { assunto, mensagem, pagina });
  }

  /** Mensagem do pai pro e-mail de uma unidade do clube (portal). */
  enviarMensagemClube(unidadeId: string, assunto: AssuntoClube, mensagem: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/portal/contato-clube/mensagem`, { unidadeId, assunto, mensagem });
  }

  /** Contato do clube pros pais (portal). */
  contatoClube(): Observable<ContatoClube> {
    return this.http.get<ContatoClube>(`${this.baseUrl}/portal/contato-clube`);
  }
}

/** Mensagem que já vem escrita ao abrir o WhatsApp do clube: diz quem está falando e de qual atleta, e o pai só completa. */
export function mensagemInicialWhatsApp(nomeResponsavel: string | null | undefined, atletas: string[]): string {
  const nome = nomeResponsavel?.trim();
  const quem = nome ? `Olá! Sou ${nome}` : 'Olá!';
  if (atletas.length === 0) return nome ? `${quem}. ` : `${quem} `;
  const lista = atletas.length === 1 ? atletas[0] : `${atletas.slice(0, -1).join(', ')} e ${atletas.at(-1)}`;
  return `${quem}, responsável por ${lista}. `;
}
