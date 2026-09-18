# Rotina Escola — convenções do projeto

## Idioma: tudo em português (pt-BR)

Este é um app para escolas brasileiras — todo texto visível ao usuário deve estar em português, sem exceção.

### Nunca usar widgets nativos do navegador que renderizam texto

Componentes nativos do HTML seguem o idioma do **navegador**, não o `lang` da página (`<html lang="pt-BR">` não resolve isso no Chrome). Use sempre os substitutos customizados já existentes no projeto:

- **Data** (`<input type="date">`) → use `app-calendario` (`frontend/src/app/shared/calendario/`). Botão que abre um popover com calendário mensal em português, com `[dataSelecionada]`, `[dataMaxima]` opcional e `(escolher)`.
- **Arquivo** (`<input type="file">`) → use `app-seletor-arquivo` (`frontend/src/app/shared/seletor-arquivo/`). Botão estilizado que dispara um input nativo oculto, com `[rotulo]`, `[aceitar]`, `[capturarCamera]` e `(arquivoSelecionado)` emitindo o `File` diretamente.
- Antes de adicionar qualquer novo campo de data ou de arquivo em um formulário, use esses componentes — não volte a usar o `<input>` nativo.
- Ao criar novos tipos de widget nativo com texto (ex.: `<input type="month">`, `<input type="week">`), teste no Chrome antes de assumir que está em português; se não estiver, siga o mesmo padrão (componente customizado).

### Outras fontes de texto em inglês a evitar

- `confirm()` / `alert()` nativos do navegador — não usar; o padrão do projeto é confirmação inline (ex.: `confirmandoExclusaoId` signal + botões de confirmar/cancelar no próprio item da lista).
- Formatação de data/hora: sempre passar `'pt-BR'` explicitamente em `toLocaleDateString`/`toLocaleTimeString`/`Intl.DateTimeFormat`. Nunca usar `.toString()`/`.toDateString()` puro (sempre em inglês). Utilitários prontos em `frontend/src/app/shared/data-utils.ts` (`hojeIso`, `rotuloData`, `formatarDataAbsoluta`, `idadeFormatada`).
