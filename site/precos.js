// Tabela de preços do site — um lugar só.
// Iguais à configuração Assinatura:* da API (a tela de cadastro do app lê de lá). Mudou um, mude o outro.
// Desde 2026-10-14: R$ 60 por mês cobre até 20 atletas; cada atleta acima disso custa R$ 4.
const PRECO_FIXO = 60;
const ATLETAS_INCLUIDOS = 20;
const PRECO_POR_ATLETA = 4;
const DIAS_TESTE = 7;
// Dias de atraso até suspender o acesso (Assinatura:DiasToleranciaAtraso).
const DIAS_TOLERANCIA = 7;

// Endereço do app (tela de cadastro e de entrar): o de produção, ou o de desenvolvimento quando o site roda na máquina.
const URL_APP = ['localhost', '127.0.0.1'].includes(location.hostname)
  ? 'http://localhost:4200'
  : 'https://app.gestortatico.com.br';

function calcularMensalidade(atletas) {
  return PRECO_FIXO + PRECO_POR_ATLETA * Math.max(0, atletas - ATLETAS_INCLUIDOS);
}

function formatarReais(valor) {
  return valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}
