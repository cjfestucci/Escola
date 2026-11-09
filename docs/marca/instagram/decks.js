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
{t:'claro',h:'Uma ficha <em>por atleta</em>',list:['Alergias e restrições alimentares','Medicamentos e condições de saúde','Plano de saúde, pediatra e contato de emergência']},
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
],
'20261116-atencao':[
{t:'capa',tag:'Painel da coordenação',h:'O que pede <em>atenção</em> hoje, numa tela só',p:'Faltas, atestados, estoque e contas: o painel junta o que a coordenação não pode esquecer.'},
{h:'Problema não avisa <em>quando chega</em>',p:'O atleta some, o atestado vence, a bola acaba, a conta atrasa. Cada coisa mora num caderno, e você descobre tarde.'},
{t:'claro',h:'Só aparece o que <em>tem ocorrência</em>',p:'O painel lista os alertas do dia. Sem nenhum, mostra que está tudo em dia. Nada de tela cheia de zero.'},
{h:'No <em>cuidado com os atletas</em>',list:['Atletas com 3 ou mais faltas seguidas','Atestado médico vencido ou perto de vencer','Matrículas aguardando o aceite da família']},
{t:'claro',h:'No <em>dinheiro e no material</em>',list:['Contas a pagar atrasadas','Produtos com estoque baixo ou zerado','Atletas bloqueados por atraso']},
{h:'Cada alerta é um <em>atalho</em>',p:'Tocou no aviso, abre a lista já filtrada: os atletas faltosos, os atestados vencidos, os produtos sem estoque.'},
{t:'claro',h:'Mais de uma <em>unidade</em>?',p:'Veja a visão geral ou escolha uma unidade para recortar o painel.',n:'O estoque e as contas a pagar seguem consolidados, não por unidade.'},
CTA],
'20261118-campeonato':[
{t:'capa',tag:'Campeonatos',h:'Como foi a <em>campanha</em> da categoria?',p:'Vitórias, gols e artilheiros do campeonato, a partir das súmulas.'},
{h:'Resultado perdido em <em>grupo de WhatsApp</em>',p:'No fim do campeonato ninguém lembra o placar da terceira rodada nem quem foi o artilheiro.'},
{t:'claro',h:'Placar e súmula, <em>jogo a jogo</em>',p:'Depois da partida, lance o placar e, na convocação, os gols e os cartões de cada atleta.',n:'A soma dos gols dos atletas não pode passar do placar.'},
{h:'A campanha de cada <em>categoria</em>',list:['Jogos, vitórias, empates e derrotas','Gols pró, gols contra e saldo','Pontos: vitória vale 3, empate vale 1']},
{t:'claro',h:'Artilharia <em>sem planilha</em>',p:'Os gols lançados nas súmulas somam sozinhos numa lista de artilheiros do campeonato.'},
{h:'Disciplina <em>à vista</em>',p:'A página do campeonato mostra quem está suspenso e quem está pendurado, pelo limite de amarelos que você definiu.'},
{t:'claro',h:'O que ele <em>ainda não faz</em>',p:'O sistema registra o lado do seu clube. Não monta a tabela de classificação com os outros times.'},
CTA],
'20261120-estoque':[
{t:'capa',tag:'Estoque',h:'Quantas bolas <em>sobraram</em> no armário?',p:'Entradas, saídas e saldo sempre certo, com aviso quando o estoque baixa.'}
]};
