// Tabela de preços do site — um lugar só.
// Iguais à configuração Assinatura:* da API (a tela de cadastro do app lê de lá). Mudou um, mude o outro.
const PRECO_FIXO = 99;
const PRECO_POR_ATLETA = 3;
const DIAS_TESTE = 7;

// Endereço do app (tela de cadastro e de entrar). Em produção, trocar pelo domínio do app.
const URL_APP = 'http://localhost:4200';

function calcularMensalidade(atletas) {
  return PRECO_FIXO + PRECO_POR_ATLETA * Math.max(0, atletas);
}

function formatarReais(valor) {
  return valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}
