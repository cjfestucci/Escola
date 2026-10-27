// Preenche as páginas do site com os valores de precos.js (um lugar só) e aponta os botões pro app.

// Versão dos Termos de Uso e da Política de Privacidade. Mudou o texto → troque aqui E em
// AssinaturaService.VersaoTermosUso (backend), que grava qual versão cada clube aceitou no cadastro.
const VERSAO_DOCUMENTOS = '2026-10-27.1';
const VIGENCIA_DOCUMENTOS = '14/10/2026';

function preencher(seletor, texto) {
  document.querySelectorAll(seletor).forEach((el) => (el.textContent = texto));
}

preencher('.ano', new Date().getFullYear());
preencher('.dias-teste', DIAS_TESTE);
preencher('.dias-tolerancia', DIAS_TOLERANCIA);
preencher('.preco-fixo', formatarReais(PRECO_FIXO));
preencher('.preco-atleta', formatarReais(PRECO_POR_ATLETA));
preencher('.atletas-incluidos', ATLETAS_INCLUIDOS);
preencher('.versao-termos', VERSAO_DOCUMENTOS);
preencher('.vigencia-termos', VIGENCIA_DOCUMENTOS);
function apontarBotoes() {
  document.querySelectorAll('[data-app]').forEach((a) => (a.href = a.dataset.app === 'assinar' ? urlAssinar() : URL_APP + '/' + a.dataset.app));
}
apontarBotoes();

// Campanha: o site pergunta ao servidor se o cupom ainda vale e quantas vagas restam (o Caddy repassa só esse endereço pro app).
// Sem resposta (servidor fora, site rodando local) a faixa simplesmente não aparece.
const faixaOferta = document.getElementById('faixa-oferta');
if (faixaOferta) {
  fetch('/api/assinaturas/cupom/' + CUPOM_SITE, { cache: 'no-store' })
    .then((resposta) => (resposta.ok ? resposta.json() : null))
    .then((cupom) => {
      if (!cupom || cupom.restantes === 0) return;
      cupomSiteAtivo = true;
      apontarBotoes();
      const quem = cupom.restantes ? 'os próximos ' + cupom.restantes + (cupom.restantes === 1 ? ' assinante' : ' assinantes') : 'novos assinantes';
      const prazo = cupom.meses ? (cupom.meses === 1 ? 'na 1ª mensalidade' : 'nas ' + cupom.meses + ' primeiras mensalidades') : 'em todas as mensalidades';
      document.getElementById('texto-oferta').innerHTML =
        'Condição especial para ' + quem + ': <strong>' + cupom.percentualDesconto + '% de desconto ' + prazo + '</strong>. Já vem aplicado ao começar seu teste grátis.';
      faixaOferta.hidden = false;
      document.dispatchEvent(new Event('cupom-site'));
    })
    .catch(() => {});
}
