# Rascunhos da semana de 23/11/2026 (@gestortaticooficial)

Preparado em 17/11/2026 pelo agente semanal. **Nada foi publicado.** Revise, ajuste e publique pelo Meta Business Suite.
Obs.: o `calendario.md` cobre até 20/11 (painel de atenção, campeonato, estoque); esta é a semana seguinte. Temas novos: financeiro (contas a pagar/receber e balanço), matrícula do atleta e uma pergunta ao público.

---

## Seg 23/11: carrossel "Contas a pagar, a receber e balanço do mês" (8 slides)

- **Formato:** carrossel
- **Arquivos:** `png/20261123-contas-1.png` a `png/20261123-contas-8.png` (o 8 é o CTA)
- **Horário sugerido:** 12h (ou 19h)

**Legenda:**
> A mensalidade entra, mas e o aluguel da quadra, o árbitro, o fornecedor do uniforme? Conta no caderno costuma vencer sem ninguém ver 💸
>
> No Gestor Tático você cadastra o fornecedor, lança a conta a pagar com valor e vencimento e marca como paga quando quitar. Receitas que não são mensalidade (patrocínio, aluguel de quadra) entram em contas a receber. No painel, o balanço do mês mostra receitas, despesas e saldo, e as contas dos próximos 7 dias.
>
> Marque quem cuida do financeiro da sua escolinha.
>
> Teste grátis por 7 dias: link na bio.

**Hashtags:** #escolinhadefutebol #futebolbase #gestaoesportiva #categoriasdebase #escolinha #gestaofinanceira

**O que este post afirma → onde está a prova**
- Contas a pagar por fornecedor (valor, vencimento, marcar paga; pendentes/atrasadas/pagas) → `CLAUDE.md` "Financeiro expandido"; `site/index.html` ("Financeiro e dashboard").
- Contas a receber para receita que não é mensalidade (patrocínio, aluguel de quadra) → `CLAUDE.md` ("ContaReceber... cobre receita que não é mensalidade").
- Balanço do mês: receitas (mensalidades pagas + contas recebidas) × despesas (contas pagas) = saldo, pela data do pagamento; "A pagar nos próximos 7 dias" → `CLAUDE.md` "Dashboard: painéis de ação".
- Conta não se apaga, é cancelada; tudo com histórico → `CLAUDE.md` ("Desativar/Cancelar em vez de excluir"; "Auditoria").
- Balanço só para o financeiro; consolidado, não por unidade → `CLAUDE.md` (slide 7 avisa só do perfil; não diz nada de unidade).
- "Inscrição em torneio" e "árbitro" no slide 2/4 são exemplos de despesa/receita genéricos, não recursos do sistema.

---

## Qua 25/11: carrossel "Matrícula completa do atleta" (8 slides)

- **Formato:** carrossel
- **Arquivos:** `png/20261125-matricula-1.png` a `png/20261125-matricula-8.png` (o 8 é o CTA)
- **Horário sugerido:** 19h (ou 12h)

**Legenda:**
> Ficha de papel, planilha e contato de mãe anotado em outro caderno: na hora de achar, nada está no lugar 😅
>
> No Gestor Tático, a matrícula do atleta junta foto, categoria, posição em campo (opcional), responsáveis e ficha de saúde. A família recebe um convite por e-mail, cria a senha e aceita o termo de matrícula no Portal da Família. Até o aceite, o atleta fica marcado como "Aguardando aceite".
>
> Marque quem faz as matrículas na sua escolinha.
>
> Teste grátis por 7 dias: link na bio.

**Hashtags:** #escolinhadefutebol #futebolbase #categoriasdebase #gestaoesportiva #escolinha #treinadordefutebol

**O que este post afirma → onde está a prova**
- Foto, categoria, posição opcional (clube), vários responsáveis com o financeiro indicado → `CLAUDE.md` ("Posição do atleta", "Responsáveis recolhíveis", matrícula).
- Convite por e-mail para confirmar e criar senha; termo aceito no primeiro acesso; selo "Aguardando aceite" e filtro → `CLAUDE.md` "LGPD: termo de matrícula".
- Desconto em % com motivo; a mensalidade em lote já aplica → `CLAUDE.md` ("Mensalidades em lote, desconto...").
- Desativar em vez de apagar; histórico de cada alteração → `CLAUDE.md` ("Desativar/Cancelar..."; "Auditoria").
- Não afirma que isso é conformidade com a LGPD nem dá orientação jurídica.

---

## Sex 27/11: post único, pergunta ao público (formato leve)

- **Formato:** post único (imagem) + enquete/caixinha nos stories, se der
- **Arquivo:** `png/20261127-pergunta-1.png`
- **Horário sugerido:** 12h

**Legenda:**
> Qual é a maior dor da sua escolinha hoje? Mensalidade, chamada, comunicação com os pais ou controle do material? Conta nos comentários 👇
>
> Respondemos todo mundo, e os temas mais pedidos viram os próximos posts.
>
> Teste grátis por 7 dias: link na bio.

**Hashtags:** #escolinhadefutebol #futebolbase #gestaoesportiva #categoriasdebase #treinadordefutebol

**O que este post afirma → onde está a prova**
- Só pergunta; nenhuma afirmação sobre o produto. "Respondemos todo mundo" é compromisso do perfil: só manter se o Cleber for responder.

---

## Pendências para o Cleber confirmar
1. **Balanço do mês** aparece só para quem é do financeiro (Admin, Coordenador, Financeiro). Dito no slide 7 do carrossel de contas.
2. **Slide 4 do carrossel de matrícula** ("convite por e-mail"): depende de o clube ter e-mail dos responsáveis; e o envio usa o SMTP do sistema (em produção já configurado, conforme o `CLAUDE.md`). Confirmar que está entregando.
3. **Posição em campo** só aparece no segmento clube (o perfil mira clubes/escolinhas de futebol, então é coerente).
4. **Convocação com botão "Divulgar convocação"** (e-mail aos responsáveis, commit 98e28db) ficou de fora: o `CLAUDE.md` diz que não foi testado no navegador. Se testar e estiver no ar, vira post na semana seguinte.
5. Nenhum post fala de Pix automático, boleto, cartão, WhatsApp, preço ou cupom.
6. O agente não altera o `calendario.md`: acrescentar as linhas de 23, 25 e 27/11 quando publicar.
