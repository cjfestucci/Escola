// Tabela de preços do site — um lugar só.
// Iguais à configuração Assinatura:* da API (a tela de cadastro do app lê de lá). Mudou um, mude o outro.
// Desde 2026-11-09: preço por faixa de atletas (vale a primeira faixa que comporta o clube); acima da última, o preço dela
// + PRECO_POR_ATLETA_ACIMA por atleta extra. Iguais a Assinatura:Faixas e Assinatura:PrecoPorAtletaAcima da API.
const FAIXAS = [
  { ate: 20, preco: 59 },
  { ate: 30, preco: 69 },
  { ate: 100, preco: 129 },
  { ate: 200, preco: 189 },
  { ate: 300, preco: 249 }
];
const PRECO_POR_ATLETA_ACIMA = 0.6;
const PRECO_MINIMO = FAIXAS[0].preco;
const DIAS_TESTE = 7;
// Dias de atraso até suspender o acesso (Assinatura:DiasToleranciaAtraso).
const DIAS_TOLERANCIA = 7;

// Endereço do app (tela de cadastro e de entrar): o de produção, ou o de desenvolvimento quando o site roda na máquina.
const URL_APP = ['localhost', '127.0.0.1'].includes(location.hostname)
  ? 'http://localhost:4200'
  : 'https://app.gestortatico.com.br';

function calcularMensalidade(atletas) {
  const faixa = FAIXAS.find((f) => atletas <= f.ate);
  if (faixa) return faixa.preco;
  const ultima = FAIXAS[FAIXAS.length - 1];
  return Math.round((ultima.preco + PRECO_POR_ATLETA_ACIMA * (atletas - ultima.ate)) * 100) / 100;
}

// Sem os centavos quando o valor é inteiro ("R$ 59"), pra frases e tabelas.
function formatarReaisCurto(valor) {
  return Number.isInteger(valor) ? valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL', maximumFractionDigits: 0 }) : formatarReais(valor);
}

function formatarReais(valor) {
  return valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

// Cupom da campanha do site: 25% nas 12 primeiras mensalidades, limitado aos próximos assinantes (os usos ficam no próprio cupom,
// criado pelo Suporte). Os botões "Começar teste grátis" levam o cupom já aplicado e a faixa do topo mostra as vagas; quando o cupom
// esgota ou é desativado, a faixa some e os botões voltam ao normal sozinhos (paginas.js consulta o servidor).
const CUPOM_SITE = 'BEMVINDO25';
let cupomSiteAtivo = false;

function urlAssinar(atletas) {
  const params = new URLSearchParams();
  if (atletas) params.set('atletas', atletas);
  if (cupomSiteAtivo) params.set('cupom', CUPOM_SITE);
  const consulta = params.toString();
  return URL_APP + '/assinar' + (consulta ? '?' + consulta : '');
}
