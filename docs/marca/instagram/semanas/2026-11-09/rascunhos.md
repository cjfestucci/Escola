# Rascunhos da semana de 09/11/2026 (@gestortaticooficial)

Preparado em 03/11/2026 pelo agente semanal. **Nada foi publicado.** Revise, ajuste e publique pelo Meta Business Suite.
Obs.: a semana de 02 a 06/11 já estava coberta pelo `calendario.md`; esta é a seguinte. Se o Reel de chamada (previsto para 30/10) ainda não saiu, ele pode entrar em qualquer dia desta semana no lugar do post de sexta.

---

## Ter 10/11 (era seg 09/11, movido por causa do Reel): carrossel "Portal da Família" (8 slides)

- **Formato:** carrossel
- **Arquivos:** `png/20261109-portal-1.png` a `png/20261109-portal-8.png` (o 8 é o CTA)
- **Horário sugerido:** 12h (ou 19h)

**Legenda:**
> "Ele foi convocado?" "Qual é o Pix?" "Foi ao treino ontem?" 📲
>
> Essas perguntas chegam no WhatsApp da coordenação o dia inteiro. No Portal da Família, cada responsável tem o próprio acesso e encontra a resposta sozinho: jogos e convocação do filho, frequência nos treinos e mensalidades com Pix e QR Code.
>
> Cada família vê só o próprio filho. Funciona no navegador do celular, sem baixar aplicativo.
>
> Salve e marque quem cuida da coordenação da sua escolinha.
>
> Teste grátis por 7 dias: link na bio.

**Hashtags:** #escolinhadefutebol #futebolbase #gestaoesportiva #categoriasdebase #escolinha #treinadordefutebol

**O que este post afirma → onde está a prova**
- Cada responsável tem acesso próprio, com convite por e-mail; pai e mãe podem acessar o mesmo atleta → `site/portal-da-familia-escolinha.html` ("Como os pais entram", FAQ).
- Vê jogos, convocação (se foi chamado), frequência, mensalidades com Pix e QR Code; jogo cancelado aparece como cancelado → mesma página; `CLAUDE.md` (Portal da Família, Competições, Menu Financeiro no Portal).
- Cada família vê só o próprio filho → página do portal ("Privacidade das crianças"); `CLAUDE.md` (privacidade do `JogoFamiliaDto`).
- Funciona no navegador, sem baixar app → FAQ do portal.
- "O portal vale para todos os responsáveis do clube" (slide 7) → FAQ: "sem limite de responsáveis".
- "Menos mensagem repetida" é benefício esperado, não número: **sem dado de redução de mensagens.**

---

## Qua 11/11: carrossel "Ficha de saúde e atestado" (8 slides)

- **Formato:** carrossel
- **Arquivos:** `png/20261111-saude-1.png` a `png/20261111-saude-8.png` (o 8 é o CTA)
- **Horário sugerido:** 19h (ou 12h)

**Legenda:**
> Escolinha guarda dado de criança: alergia, medicamento, laudo, atestado. E atestado vencido é daquelas coisas que só se descobre na hora errada 🩺
>
> No Gestor Tático, cada atleta tem uma ficha de saúde. O atestado médico tem data de validade, e o aviso aparece na lista de atletas, no painel da coordenação, na convocação (se não vale na data do jogo) e no portal da família.
>
> Exames e laudos anexados só abrem para a equipe do clube e para o responsável daquele atleta, e toda alteração fica no histórico.
>
> Marque o coordenador que vive atrás de atestado.
>
> Teste grátis por 7 dias: link na bio.

**Hashtags:** #escolinhadefutebol #futebolbase #gestaoesportiva #categoriasdebase #lgpd #treinadordefutebol

**O que este post afirma → onde está a prova**
- Ficha de saúde por atleta (alergias, medicamentos, condições, plano, pediatra, contato de emergência) → `site/lgpd-ficha-de-saude-escolinha.html`; `CLAUDE.md` "Ficha de saúde".
- Alerta da ficha para o treinador antes de lançar a rotina → `CLAUDE.md` (alerta no topo de `aluno-rotina`). **Atenção:** na tela da rotina, que existe só no segmento escola; no clube o aviso aparece em outros pontos. O slide 3 diz "O treinador vê o alerta antes de lançar a rotina do dia": se o público é clube, considere trocar essa nota (ver pendências).
- Atestado com validade, selo e filtro, aviso no painel (vencidos e vencendo em 30 dias), na convocação (data do jogo) e no portal → página LGPD ("Atestado médico com validade e aviso"); `CLAUDE.md` "Validade do atestado médico".
- Documentos só para equipe e responsável do atleta, sem link público; alterações no histórico → página LGPD ("Documentos e fotos protegidos", "Cada um vê o que precisa"); `CLAUDE.md` (Documentos anexados, Auditoria).
- Termo aceito no primeiro acesso, com texto, versão, data, IP e navegador → página LGPD ("Consentimento do responsável, com prova"); `CLAUDE.md` "LGPD: termo de matrícula".
- Slide 7 traz o aviso "Não é orientação jurídica". Não usa as palavras "conformidade" nem "garante".

---

## Sex 13/11: post único de pergunta ao público (formato leve)

- **Formato:** post único (imagem) e, se quiser, a mesma pergunta nos stories com caixinha de perguntas/enquete
- **Arquivo:** `png/20261113-pergunta-1.png`
- **Horário sugerido:** 12h

**Legenda:**
> Curiosidade de quem cuida de escolinha ou clube de base: como os pais ficam sabendo da convocação? Grupo de WhatsApp, recado no treino ou outro jeito? Conta nos comentários 👇

**Hashtags:** #escolinhadefutebol #futebolbase #categoriasdebase #treinadordefutebol

**O que este post afirma → onde está a prova**
- Nenhuma afirmação sobre o sistema; é só pergunta. Responder todos os comentários (é pesquisa de dor real e prepara o gancho do portal de segunda).

---

## Pendências para o Cleber confirmar
1. **Slide 3 do carrossel de saúde** (nota "O treinador vê o alerta antes de lançar a rotina do dia"): a rotina diária só existe no segmento escola. Para público de clube, trocar por outra nota ou apagar. Posso editar em `decks.js` e reexportar.
2. **Slide 4 do portal** fala de "convocação" e jogos: só vale para clubes (segmento clube). Está coerente com o público do perfil.
3. **Slide 4 da saúde**: "filtro de vencidos e de vencendo em 30 dias" confere com o código (`DIAS_AVISO_ATESTADO` = 30).
4. A legenda do portal cita "mensalidades com Pix e QR Code": é copia e cola, com baixa manual (nada de baixa automática, e o texto não promete isso).
5. Ordem sugerida: se o Reel da chamada ainda não foi ao ar, ele pode substituir o post de pergunta na sexta.
6. Observação técnica: `carrosseis.html` ganhou um ajuste no leitor do hash (para aceitar chaves com hífen, como `20261109-portal`); os carrosséis antigos foram reexportados sem diferença. O arquivo `reels/reel-chamada.mp4` aparece como modificado no git por causa de outro processo, não deste agente.
