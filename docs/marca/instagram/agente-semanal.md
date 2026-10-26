# Agente semanal de rascunhos do Instagram (@gestortaticooficial)

Instruções do agente agendado `instagram-rascunhos-semanais` (domingo, 18h). Ele **prepara** a semana seguinte; quem revisa e publica é o Cleber (Meta Business Suite). O agente **nunca publica, nunca faz commit nem push**.

## O que entregar

Rascunhos dos 3 posts da próxima semana (segunda, quarta e sexta), prontos para revisar:

1. Em `docs/marca/instagram/semanas/AAAA-MM-DD/rascunhos.md` (AAAA-MM-DD = a segunda-feira da semana), para cada post: data, formato (carrossel / post único / roteiro de Reel / enquete), arquivos de imagem, legenda completa, hashtags (4 a 6), horário sugerido e a lista "O que este post afirma → onde está a prova" (página do site ou trecho do `CLAUDE.md`).
2. As artes: acrescentar o carrossel em `docs/marca/instagram/decks.js` (chave `AAAAMMDD-tema`, ex.: `20261109-portal`) e rodar `bash docs/marca/instagram/exportar.sh <chave>`. Os PNGs saem em `docs/marca/instagram/png/`.
3. Abrir pelo menos a capa e mais um slide de cada carrossel gerado e conferir: texto sem corte, sem erro de português, sem sobreposição.
4. Fechar com um resumo curto: o que foi preparado, e dúvidas ou afirmações que o Cleber precisa confirmar.

## Como escolher o conteúdo

- Ler antes: `docs/marca/instagram/calendario.md`, `decks.js`, as pastas anteriores em `semanas/` (não repetir tema nem gancho) e as páginas do site (`site/*.html`), que são a fonte do que o sistema faz.
- **Se a semana já está coberta pelo `calendario.md` com artes prontas** (26/10 a 06/11/2026), não criar nada e dizer isso no resumo.
- Mistura sugerida por semana: 2 carrosséis + 1 de formato leve (enquete, dica rápida, roteiro de Reel de 20 a 30 s, post único).
- Temas ainda não usados: Portal da Família, LGPD e ficha de saúde, atestado médico vencido na convocação, desempenho e artilharia do campeonato, painel de atenção da coordenação, estoque, contas a pagar e balanço do mês, perguntas ao público, bastidores do produto.
- Novidade do produto: olhar `git log --since="7 days ago" --oneline`; só virar post o que for visível ao cliente e já estiver no ar. Nunca divulgar o que é interno.
- Tom: direto, em português do Brasil, falando com quem cuida de escolinha ou clube de base (dono, coordenador, treinador). Sem jargão técnico, sem exagero de emoji (no máximo 1 ou 2 por legenda).
- Todo carrossel termina com o slide `CTA` (já definido em `decks.js`), 6 a 8 slides, 1 ideia por slide, títulos curtos.

## Regras que não podem ser quebradas

- Só afirmar o que o sistema faz **hoje**, conforme o site e o `CLAUDE.md`. Em especial: **não falar em baixa automática de Pix** (hoje é Pix copia e cola com QR Code e baixa manual), nem em boleto, cartão, WhatsApp automático ou NFS-e.
- Sem número inventado (clientes, economia, percentuais), sem depoimento, sem comparar com concorrente pelo nome.
- Sem dado real de atleta, responsável ou clube; sem foto de criança. Se precisar de exemplo, usar nomes genéricos (Sub-9, "o atleta").
- Não mencionar o cupom `BEMVINDO25` nem preço nas legendas (o link da bio já leva o cupom e o site mostra o preço). Chamada para ação: "Teste grátis por 7 dias: link na bio".
- Privacidade e LGPD: nunca dar orientação jurídica; dizer o que o sistema registra, não o que "garante conformidade".
- Não alterar `site/`, `backend/`, `frontend/`, `CLAUDE.md` nem os carrosséis já existentes. Só criar arquivos em `docs/marca/instagram/` (e acrescentar chaves novas em `decks.js`).
- Não abrir nem usar o Instagram, nem navegador para postar.
