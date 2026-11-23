# Rascunhos da semana de 30/11/2026 (@gestortaticooficial)

Preparado em 23/11/2026 pelo agente semanal. **Nada foi publicado.** Revise, ajuste e publique pelo Meta Business Suite.
Obs.: a semana de 23/11 já tinha rascunhos e artes prontos (`semanas/2026-11-23/`), então este rascunho é da semana seguinte. Temas novos: perfis de acesso da equipe, "Fale com o clube" no Portal da Família e uma dica rápida de primeiros passos no teste.

---

## Seg 30/11: carrossel "Cada um da equipe vê o que precisa" (8 slides)

- **Formato:** carrossel
- **Arquivos:** `png/20261130-perfis-1.png` a `png/20261130-perfis-8.png` (o 8 é o CTA)
- **Horário sugerido:** 12h (ou 19h)

**Legenda:**
> Login dividido pela equipe inteira? Aí ninguém sabe quem mexeu na mensalidade ou na chamada 🔐
>
> No Gestor Tático cada pessoa tem o próprio acesso, criado por convite no e-mail. O treinador faz a chamada e a convocação só das categorias dele. A secretaria cadastra atletas, marca a mensalidade paga no balcão e cuida do estoque, mas desconto e cancelamento ficam com o financeiro. E tudo o que muda fica no histórico, com o nome de quem fez.
>
> Marque quem coordena a equipe da sua escolinha.
>
> Teste grátis por 7 dias: link na bio.

**Hashtags:** #escolinhadefutebol #futebolbase #gestaoesportiva #categoriasdebase #treinadordefutebol #escolinha

**O que este post afirma → onde está a prova**
- Perfis administrador, coordenação, treinador, financeiro e secretaria com acessos diferentes; treinador só nas turmas dele; secretaria cadastra e recebe no balcão sem dar desconto → `site/sistema-para-escolinha-de-futebol.html` ("Equipe com acesso por perfil"); `CLAUDE.md` "Perfil Secretaria" e "Professor só lança nas próprias turmas".
- Treinador "faz a chamada, agenda jogos e monta a convocação… o resto só consulta" → guia "Convidar treinadores e a equipe" (Ajuda → Guias de uso); `CLAUDE.md` "Professor só lança nas próprias turmas".
- Secretaria: cancelar cobrança e desconto só com o financeiro; cuida do estoque → `CLAUDE.md` "Perfil Secretaria".
- Convite por e-mail, cada um cria a própria senha → `CLAUDE.md` "Convite por e-mail na tela Usuários".
- Histórico com quem mexeu; desativar conta → `CLAUDE.md` "Auditoria" e "Desativar/Cancelar em vez de excluir" (desativar derruba as sessões abertas: "Revogação de sessões").
- Não afirma "usuários ilimitados" (ainda pendente de validação, segundo o `CLAUDE.md`).

---

## Qua 02/12: carrossel "Fale com o clube, no Portal da Família" (8 slides)

- **Formato:** carrossel
- **Arquivos:** `png/20261202-contato-1.png` a `png/20261202-contato-8.png` (o 8 é o CTA)
- **Horário sugerido:** 19h (ou 12h)

**Legenda:**
> "Qual é o número da secretaria mesmo?" Se essa pergunta aparece toda semana, o contato do clube está no lugar errado 📲
>
> No Gestor Tático você cadastra endereço, telefone, WhatsApp e e-mail de cada unidade, e os pais encontram tudo no Portal da Família, em "Fale com o clube". O WhatsApp abre a conversa já com o nome do responsável e do atleta, e o e-mail vira um formulário que chega para o clube com a categoria do atleta.
>
> Mande para quem atende os pais na sua escolinha.
>
> Teste grátis por 7 dias: link na bio.

**Hashtags:** #escolinhadefutebol #futebolbase #gestaoesportiva #categoriasdebase #escolinha

**O que este post afirma → onde está a prova**
- Contatos por unidade (endereço, telefone, WhatsApp, e-mail, todos opcionais) mostrados no portal em "Fale com o clube" → `CLAUDE.md` "Ajuda: Fale conosco e 'Fale com o clube'"; `site/portal-da-familia-escolinha.html` ("Fale com o clube"); guia "Primeiros passos".
- WhatsApp vira botão que abre a conversa com mensagem inicial com o nome do responsável e do atleta (categoria) → `CLAUDE.md` (`mensagemInicialWhatsApp`). **É um link para o WhatsApp do clube, não envio automático**: a legenda e os slides não falam em mensagem automática.
- Formulário de e-mail com assunto; chega na unidade com o nome do pai e os atletas com a categoria; o pai recebe cópia → `CLAUDE.md` (mesma seção).
- O slide 4 diz "unidades" (o sistema mostra só as ativas; não precisa detalhar).
- O botão flutuante de WhatsApp no portal **não** foi citado de propósito (detalhe de interface; pode entrar nos stories).

---

## Sex 04/12: post único "Primeiro dia de teste: por onde começar" (formato leve)

- **Formato:** post único (imagem); pode ir também nos stories com o link
- **Arquivo:** `png/20261204-primeiros-passos-1.png`
- **Horário sugerido:** 12h

**Legenda:**
> Começou o teste e não sabe por onde ir? Três passos para o sistema já ter cara de clube no primeiro dia ✅
>
> 1. Nome, logo e contatos: em Configurações e Unidades.
> 2. Convide a equipe por e-mail e crie as categorias (Sub-9, Sub-11…).
> 3. Cadastre os atletas e faça a primeira chamada.
>
> Dentro do sistema, o menu Ajuda tem guias passo a passo de cada tarefa.
>
> Salve para o dia em que começar. Teste grátis por 7 dias: link na bio.

**Hashtags:** #escolinhadefutebol #futebolbase #gestaoesportiva #categoriasdebase #treinadordefutebol

**O que este post afirma → onde está a prova**
- Ordem dos passos (dados do clube e contatos → equipe → categorias → atletas → chamada) → guias de uso em `frontend/src/app/pages/ajuda-guias` (índice "Primeiros passos" a "Cadastrar atletas" e "Fazer a chamada"); `CLAUDE.md` "Guias de uso".
- Nome e logo pelo Admin em Configurações → Geral; contatos em Unidades → `CLAUDE.md` "Identidade da escola" e "Fale com o clube".
- Menu Ajuda com guias, só para quem já entrou → `CLAUDE.md` ("Guias de uso… só dentro do app").
- Nenhuma menção a Pix na arte nem na legenda (o guia fala em "baixa automática", que não pode ir para o Instagram).

---

## Pendências para o Cleber confirmar
1. **Perfis de acesso (seg)**: conferir se a tabela de papéis do guia continua valendo em produção (treinador só consulta fora das turmas dele; secretaria sem desconto nem cancelamento).
2. **Fale com o clube (qua)**: o recurso depende de o clube preencher WhatsApp/e-mail da unidade, e o formulário de e-mail usa o SMTP do sistema. Vale conferir uma vez em produção (ex.: no clube Demonstração, sem número real) antes de postar.
3. **Guia "Primeiros passos"** dentro do app ainda cita "Configurar o recebimento por Pix (e a baixa automática)" no índice. O post não fala disso, mas quem abrir o guia vai ver: confirmar se está ok, já que a baixa automática só vale com a conta Asaas do clube conectada.
4. Nenhum post fala de baixa automática de Pix, boleto, cartão, WhatsApp automático, preço ou cupom.
5. Sem novidade do produto visível ao cliente nos últimos 7 dias além do conserto do captcha do `/assinar` (interno), então nada de "novidade" nesta semana.
6. O agente não altera o `calendario.md`: acrescentar as linhas de 23/11 a 04/12 quando publicar.
