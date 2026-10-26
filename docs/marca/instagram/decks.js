// Textos dos carrosséis. Cada chave é um carrossel (ou post único); o nome vira o prefixo dos PNGs em png/.
// Slide: {t:'capa'|'claro'|'verde', tag, h, p, list, passo, n, b}. <em> destaca em verde. Veja carrosseis.html.
const CTA={t:'verde',h:'Teste por <em>7 dias grátis</em>',p:'Gestor Tático: gestão para escolinhas e clubes de base.',b:'Link na bio'};
const decks={
apresentacao:[
{t:'capa',tag:'Novo por aqui',h:'Gestão para escolinhas e <em>clubes de base</em>',p:'Atletas, chamada, jogos, mensalidades com Pix e Portal da Família. Tudo num sistema só.'}
],
mensalidade:[
{t:'capa',tag:'Mensalidade',h:'Fechar a mensalidade da escolinha em <em>minutos</em>',p:'Sem planilha, sem conferir Pix um por um.'},
{h:'O mês fecha e a <em>conta não bate</em>',p:'Conferir quem pagou. Lembrar quem não pagou. Calcular o atraso. Responder "qual é a chave Pix?" pela décima vez.'},
{t:'claro',h:'Valor na <em>categoria</em>, desconto no <em>atleta</em>',p:'O valor fica na categoria (Sub-9, Sub-11…). Bolsa e desconto para irmãos ficam no atleta, em percentual e com o motivo anotado.',n:'Desconto de 100% = atleta isento, sem cobrança.'},
{h:'Gere o mês <em>inteiro</em> de uma vez',p:'Antes de gravar, uma prévia mostra quem recebe cobrança, quem já tem e quem é isento.',n:'Gerar de novo não duplica: quem já tem a mensalidade não ganha outra.'},
{t:'claro',h:'Pix com <em>QR Code</em> em cada cobrança',p:'O responsável vê no Portal da Família e recebe por e-mail. É só copiar e pagar no app do banco.'},
{h:'Multa e juros? <em>O sistema calcula</em>',p:'O valor atualizado do dia já sai certo no código Pix e no e-mail da cobrança. Sem conta de cabeça.'},
{t:'claro',h:'Recebeu? <em>Um clique.</em>',p:'Marcar paga. A lista separa o que está a vencer, atrasado e pago, e o painel mostra o recebido no mês e o total em aberto.'},
CTA],
chamada:[
{t:'capa',tag:'Chamada',h:'Chamada em menos de <em>1 minuto</em>',p:'No celular, do campo. E a frequência sai pronta.'},
{h:'Lista no papel <em>não vira informação</em>',p:'Quem está faltando? Quem sumiu há três treinos? Sem esse dado, a desistência só aparece quando já é tarde.'},
{t:'claro',h:'Três toques e <em>pronto</em>',list:['Abra Chamada e escolha a turma','Toque em "Todos presentes"','Ajuste só as exceções e salve'],passo:true,n:'Treino que já passou? Escolha a data e lance.'},
{h:'Falta justificada <em>não pesa</em> contra',p:'A frequência de cada atleta aparece ao lado do nome, por período, em verde, amarelo ou vermelho. A falta justificada fica fora da conta.'},
{t:'claro',h:'3 faltas seguidas? <em>O sistema avisa</em>',p:'O atleta ganha um selo e aparece no painel da coordenação. É a hora de conversar com a família, antes da desistência.'},
{h:'A família <em>acompanha</em> pelo portal',p:'No Portal da Família, o responsável vê a frequência do filho e as últimas chamadas. Sem surpresa de "ele disse que estava indo".'},
CTA],
convocacao:[
{t:'capa',tag:'Convocação',h:'Quem está <em>suspenso</em> no próximo jogo?',p:'O sistema responde antes de você escalar.'},
{h:'Súmula no papel, cartão <em>na memória</em>',p:'Em dia de jogo, alguém sempre escala um atleta suspenso. Com tudo registrado, o aviso aparece sozinho.'},
{t:'claro',h:'As regras, <em>calculadas</em>',list:['Vermelho: suspenso do próximo jogo','Limite de amarelos: suspenso e a contagem recomeça','Pendurado: a um amarelo da suspensão'],n:'Você define o limite de amarelos de cada campeonato.'},
{h:'Na convocação, tudo <em>ao lado do nome</em>',list:['Suspenso ou pendurado','Frequência nos treinos e faltas seguidas','Atestado médico vencido na data do jogo']},
{t:'claro',h:'São <em>avisos</em>. A decisão é do treinador.',p:'O sistema informa, não impede. Pode haver um bom motivo para escalar, e quem decide é a comissão.'},
{h:'Cada família vê só o <em>próprio filho</em>',p:'No portal, os pais veem se o filho foi convocado e se é titular. Nunca a escalação completa nem dados de outros atletas.'},
CTA]};
