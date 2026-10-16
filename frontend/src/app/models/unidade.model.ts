export interface Unidade {
  id: string;
  nome: string;
  endereco: string | null;
  telefone: string | null;
  /** WhatsApp pros pais (o "Fale com o clube" do portal abre a conversa). */
  whatsApp: string | null;
  /** E-mail de contato pros pais. */
  email: string | null;
  ativa: boolean;
}

export interface CriarOuEditarUnidade {
  nome: string;
  endereco: string | null;
  telefone: string | null;
  /** WhatsApp pros pais (o "Fale com o clube" do portal abre a conversa). */
  whatsApp: string | null;
  /** E-mail de contato pros pais. */
  email: string | null;
  ativa: boolean;
}
