// Simulador de preço da página inicial. Fica fora do HTML porque a política de segurança do site (Caddyfile) não aceita script embutido.

const faixa = document.getElementById('atletas');
function atualizarSimulador() {
  const atletas = Number(faixa.value);
  const total = calcularMensalidade(atletas);
  document.getElementById('atletas-qtd').textContent = atletas;
  document.getElementById('total').textContent = formatarReais(total);
  document.getElementById('por-atleta').textContent = formatarReais(total / atletas);
  document.getElementById('assinar-simulado').href = urlAssinar(atletas);
}
faixa.addEventListener('input', atualizarSimulador);
// O cupom da campanha chega depois (consulta ao servidor): o link do simulador passa a levá-lo.
document.addEventListener('cupom-site', atualizarSimulador);
atualizarSimulador();
