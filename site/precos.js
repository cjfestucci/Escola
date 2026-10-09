// Tabela de preços do site — um lugar só (usado pela página inicial e pela assinatura).
// VALORES PROVISÓRIOS: o fixo ainda não foi definido.
const PRECO_FIXO = 149;
const PRECO_POR_ATLETA = 3;

function calcularMensalidade(atletas) {
  return PRECO_FIXO + PRECO_POR_ATLETA * Math.max(0, atletas);
}

function formatarReais(valor) {
  return valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}
