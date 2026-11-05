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
CTA],
'20261109-portal':[
{t:'capa',tag:'Portal da Família',h:'Os pais perguntam. O portal <em>responde antes</em>.',p:'Convocação, frequência e Pix do filho, sem depender do WhatsApp.'},
{h:'"Ele foi <em>convocado</em>?"',p:'"Qual é o Pix?" "Foi ao treino ontem?" A coordenação responde isso o dia inteiro, uma mensagem por vez.'},
{t:'claro',h:'Cada responsável tem o <em>próprio acesso</em>',p:'O clube cadastra o atleta com o e-mail do responsável. Ele recebe o convite, cria a senha e entra pelo celular, sem baixar aplicativo.',n:'Pai e mãe podem ter acesso ao mesmo atleta.'},
{h:'Jogos e <em>convocação</em>',list:['Próximos jogos do time do filho','Se a convocação saiu e se ele foi chamado','Jogo cancelado aparece como cancelado']},
{t:'claro',h:'Frequência e <em>mensalidade</em>',list:['Percentual de presença e últimas chamadas','Cobranças de todos os filhos numa lista','Pix com QR Code em cada mensalidade']},
{h:'Cada família vê <em>só o próprio filho</em>',p:'Nada de lista de atletas, escalação completa ou dados de outras crianças. A privacidade vem desenhada no sistema.'},
{t:'claro',h:'Para o clube: <em>menos mensagem repetida</em>',p:'A família encontra a resposta sozinha, e a coordenação ganha tempo para o que importa. O portal vale para todos os responsáveis do clube.'},
CTA],
'20261111-saude':[
{t:'capa',tag:'Ficha de saúde',h:'Atestado <em>vencido</em> e ninguém percebeu?',p:'Ficha de saúde e atestado médico no mesmo lugar do atleta.'},
{h:'Escolinha guarda <em>dado de criança</em>',p:'Alergias, medicamentos, laudos, atestados. Um papel perdido na mochila do treinador não é um bom arquivo.'},
{t:'claro',h:'Uma ficha <em>por atleta</em>',list:['Alergias e restrições alimentares','Medicamentos e condições de saúde','Plano de saúde, pediatra e contato de emergência'],n:'O treinador vê o alerta antes de lançar a rotina do dia.'},
{h:'Atestado com <em>data de validade</em>',p:'Registre até quando o atestado vale. O sistema mostra um selo na lista de atletas e um filtro de vencidos e de vencendo em 30 dias.'},
{t:'claro',h:'O aviso aparece <em>onde você decide</em>',list:['No painel de atenção da coordenação','Na convocação, se não vale na data do jogo','No portal, para a família providenciar o novo']},
{h:'Documentos <em>não ficam em link público</em>',p:'Exames e laudos anexados só abrem para a equipe do clube e para o responsável daquele atleta. Toda alteração fica no histórico.'},
{t:'claro',h:'Consentimento <em>registrado</em>',p:'No primeiro acesso ao portal, o responsável lê e aceita o termo de matrícula. O sistema guarda o texto aceito, a data e o IP.',n:'Não é orientação jurídica: vale revisar o termo com o advogado do clube.'},
CTA],
'pergunta-mensalidade':[
{t:'capa',tag:'Pergunta do dia',h:'Como você controla a <em>mensalidade</em> hoje?',p:'Planilha, caderno ou sistema? Conta nos comentários.'}
],
'20261113-pergunta':[
{t:'capa',tag:'Pergunta do dia',h:'Como os pais ficam sabendo da <em>convocação</em>?',p:'Grupo de WhatsApp, recado no treino ou outro jeito? Conta nos comentários.'}
]};
