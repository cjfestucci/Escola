# Rotina Escola — convenções do projeto

## Idioma: tudo em português (pt-BR)

Este é um app para escolas brasileiras — todo texto visível ao usuário deve estar em português, sem exceção.

### Nunca usar widgets nativos do navegador que renderizam texto

Componentes nativos do HTML seguem o idioma do **navegador**, não o `lang` da página (`<html lang="pt-BR">` não resolve isso no Chrome). Use sempre os substitutos customizados já existentes no projeto:

- **Data** (`<input type="date">`) → use `app-calendario` (`frontend/src/app/shared/calendario/`). Botão que abre um popover com calendário mensal em português, com `[dataSelecionada]`, `[dataMaxima]` opcional e `(escolher)`.
- **Arquivo** (`<input type="file">`) → use `app-seletor-arquivo` (`frontend/src/app/shared/seletor-arquivo/`). Botão estilizado que dispara um input nativo oculto, com `[rotulo]`, `[aceitar]`, `[capturarCamera]` e `(arquivoSelecionado)` emitindo o `File` diretamente.
- **Hora** (`<input type="time">`) → use `app-seletor-horario` (`frontend/src/app/shared/seletor-horario/`). Dois `<select>` (hora 00–23, minuto 00–59) em vez do picker nativo, que mostra AM/PM em inglês no Chrome. Two-way binding via `[horario]`/`(horarioChange)` (string `HH:MM`).
- Antes de adicionar qualquer novo campo de data, hora ou de arquivo em um formulário, use esses componentes — não volte a usar o `<input>` nativo.
- Ao criar novos tipos de widget nativo com texto (ex.: `<input type="month">`, `<input type="week">`), teste no Chrome antes de assumir que está em português; se não estiver, siga o mesmo padrão (componente customizado).

### Outras fontes de texto em inglês a evitar

- `confirm()` / `alert()` nativos do navegador — não usar; o padrão do projeto é confirmação inline (ex.: `confirmandoExclusaoId` signal + botões de confirmar/cancelar no próprio item da lista).
- Formatação de data/hora: sempre passar `'pt-BR'` explicitamente em `toLocaleDateString`/`toLocaleTimeString`/`Intl.DateTimeFormat`. Nunca usar `.toString()`/`.toDateString()` puro (sempre em inglês). Utilitários prontos em `frontend/src/app/shared/data-utils.ts` (`hojeIso`, `rotuloData`, `formatarDataAbsoluta`, `idadeFormatada`).

## Login e perfis (autenticação real, desde 2026-09-18)

Todo mundo loga com e-mail + senha — equipe (Admin, Coordenador, Educador/Professor, Financeiro) e Responsável (Pais), pela mesma tela (`/entrar`). `PapelUsuario` (`backend/Escola.Domain/Enums/PapelUsuario.cs`) tem esses 5 valores; `Educador` é o nome interno pro papel que aparece como "Professor" nas telas.

- **Token**: JWT de 30 dias (sem refresh token — prioriza não pedir senha de novo, é um pilot pequeno). Configurado em `appsettings.Development.json` (`Jwt:Chave`/`Emissor`/`DiasValidade`), emitido por `AuthController.Entrar` (`POST /api/auth/entrar`).
- **Frontend**: `AuthService` (`frontend/src/app/services/auth.service.ts`) decodifica o JWT direto no cliente (sem round-trip) e guarda em `localStorage`. `authInterceptor` anexa o header em toda chamada à API e desloga em qualquer 401. Guards em `auth.guards.ts`: `equipeGuard` (qualquer papel de equipe), `gestaoGuard` (só Admin/Coordenador), `portalGuard` (só Responsável).
- **Backend**: todo controller tem `[Authorize]`; grupos de papéis reutilizáveis em `Escola.Api/Auth/GruposDePapeis.cs` (`Equipe`, `Gestao`, `Responsavel`). Criar/editar/excluir Turma e Aluno (Matrícula) é só `Gestao` — um Educador só visualiza essas listas agora (antes qualquer um podia mexer).
- **Senhas**: hash PBKDF2 próprio em `Escola.Infrastructure/Auth/SenhaHasher.cs` (sem dependência do ASP.NET Identity). Contas de equipe são criadas manualmente por Admin/Coordenador na tela **Usuários** (`/usuarios`, só aparece pra quem é Gestão) — a senha inicial é gerada (`GeradorSenhaTemporaria`) e mostrada **uma única vez** na tela, sem ficar guardada em texto puro em lugar nenhum.
- **Responsável (Pais)**: não tem tela de cadastro própria — o login (`Usuario` com `Papel = Responsavel`, ligado via `ResponsavelId`) é criado automaticamente na primeira vez que esse e-mail aparece como responsável de um aluno na Matrícula (`AlunosController.SincronizarResponsaveisAsync`). A senha gerada aparece uma vez na tela de Matrícula depois de salvar (`senhasGeradas` na resposta).
- **Senha de dev**: todas as contas seed (`DbInitializer`) usam `escola123` — nunca usar esse padrão fora de ambiente de desenvolvimento.
- Um Responsável só acessa os próprios filhos (checado via claim `responsavelId` no token contra `AlunoResponsaveis`) — nunca confiar em um `:id` da URL sem checar isso.
- **Redefinir senha de Responsável**: `POST /api/responsaveis/{id}/redefinir-senha`, `Gestao` only — mesmo padrão de senha gerada mostrada uma vez, exposto na Matrícula (`matricula-formulario`) junto de cada responsável já salvo (não aparece pra um responsável ainda não persistido, sem `id`). Existe porque a tela de login aponta "fale com a coordenação" pro reset — sem esse endpoint, um Admin não tinha como de fato ajudar um Pai/Mãe que esqueceu a senha.

## Ficha de saúde (desde 2026-09-21)

Registro de saúde do aluno — 1:1 com `Aluno` (`FichaSaude`, criada sob demanda, `PUT` faz upsert). Endpoints em `/api/alunos/{alunoId}/ficha-saude`: `GET` para Equipe (qualquer aluno) e Responsável (só o próprio filho — mesmo padrão de checagem via `responsavelId`); `PUT` só `Gestao`.

- Editada em `matricula-formulario` (aparece só em modo edição, não ao cadastrar um aluno novo) — salva com botão próprio ("Salvar ficha de saúde"), separado do salvar do aluno.
- Mostrada de forma resumida (alergias, restrições alimentares, medicamentos, condições de saúde) como um alerta visível no topo de `aluno-rotina`, pro educador ver antes de registrar a rotina do dia. Só aparece o que estiver preenchido — sem ficha ainda, sem alerta.
- No Portal dos Pais (`portal-aluno`), aparece como um painel colapsável (fechado por padrão) com todos os campos, incluindo uma mensagem própria quando ainda não há nada preenchido — sempre somente leitura pro Responsável.
