// Preenche as páginas do site com os valores de precos.js (um lugar só) e aponta os botões pro app.

// Versão dos Termos de Uso e da Política de Privacidade. Mudou o texto → troque aqui E em
// AssinaturaService.VersaoTermosUso (backend), que grava qual versão cada clube aceitou no cadastro.
const VERSAO_DOCUMENTOS = '2026-10-14.2';
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
document.querySelectorAll('[data-app]').forEach((a) => (a.href = URL_APP + '/' + a.dataset.app));
