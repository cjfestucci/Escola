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

## Diário de Classe (desde 2026-09-22)

Registro pedagógico da **turma** (não do aluno individual) — atividades do dia, com fotos. Modela `RegistroDiarioClasse`/`FotoDiarioClasse` no mesmo padrão normalizado de `RegistroRotina`/`FotoRegistro` (o projeto não usa colunas-array do Postgres em nenhum lugar, mesmo sendo suportado pelo Npgsql — segue a convenção existente). Endpoints em `/api/turmas/{turmaId}/diario`.

- **Autorização**: segue exatamente o padrão do `RotinaController` — `GET` pra Equipe (qualquer turma) e Responsável (só turma onde tem filho matriculado, checado via `Aluno.TurmaId` + `AlunoResponsaveis`); `POST`/`PUT`/`DELETE` só `Equipe`, sem checar se o Educador está de fato vinculado àquela turma (mesma lacuna que já existe em Rotina — deliberadamente não corrigida aqui pra manter consistência, não é regressão nova).
- **Frontend**: página própria (`diario-classe`, rota `/diario`), com seletor de turma em pílulas — reaproveita a mesma lógica de "minhas turmas ou todas as turmas" de `alunos-lista` (Admin/Coordenador/Financeiro veem todas; Educador só as suas) — e o mesmo navegador de data em português (`app-calendario`) usado em `aluno-rotina`. Cadastro/edição com até 4 fotos, mesmo componente `app-seletor-arquivo`.
- Estava marcado "Em breve" na sidebar — agora é item de navegação normal (ícone `book`).
- No Portal dos Pais (`portal-aluno`), aparece como seção "Diário da turma" somente leitura, reaproveitando o navegador de data já existente ali (mesma data da rotina individual) — sem seletor de turma próprio, porque só mostra a turma do filho que está sendo visualizado.
- **Cuidado com `[Authorize(Roles=...)]` em nível de classe + de ação**: os dois se combinam com E (interseção), não substituição — um método que precisa de um grupo de papéis MAIOR que o da classe (como `ListarDoAluno` do Financeiro abaixo, que também libera Responsável) não pode ficar numa classe já restrita a um grupo menor, senão o grupo do método vira letra morta. Erro cometido e corrigido ao construir o Financeiro — o padrão certo é `[Authorize]` puro na classe quando algum método precisa de um grupo diferente do padrão, com o grupo real em cada ação.

## Financeiro (desde 2026-09-22)

Controle de cobranças (ex.: mensalidade) por aluno — **sem gateway de pagamento**, é só registro manual de quem pagou (marcar como paga depois de receber por fora, ex. PIX/transferência). `Cobranca`: `Descricao`, `Valor`, `Vencimento`, `Paga` (bool) + `PagoEm`. "Atrasado" não é um campo — é calculado (`!Paga && Vencimento < hoje`), tanto no backend (`FinanceiroController`) quanto no frontend, pra não precisar de job periódico pra manter em dia.

- **Dois grupos de endpoints**: `/api/financeiro/cobrancas` (visão administrativa — listar tudo com filtros, criar, editar, marcar/desmarcar paga, excluir) restrito a `GruposDePapeis.Financeiro` (`Admin,Coordenador,Financeiro` — **primeiro uso real do papel Financeiro além de só existir como tipo de login**); `/api/alunos/{alunoId}/cobrancas` (visão por aluno, pro Portal dos Pais) com o mesmo padrão Equipe+Responsável-dono das outras APIs por aluno.
- **Frontend**: página `/financeiro` (`financeiro-lista`), guard próprio `financeiroGuard`/`auth.ehFinanceiro()` — um Educador comum não vê o item no menu nem consegue entrar pela URL. Filtros por turma/status/nome, formulário de nova cobrança com `app-calendario` (sem `dataMaxima`, já que vencimento é no futuro).
- No Portal dos Pais, aparece como painel colapsável "💰 Financeiro" (mesmo padrão da Ficha de Saúde), com um badge de quantas cobranças estão pendentes quando fechado.

### Pix (código copia e cola) e envio por e-mail (desde 2026-09-22)

Pedido original era "boleto" — sem conta em nenhuma processadora de pagamento (confirmado com o usuário), boleto de verdade fica pra quando houver. No lugar, cada cobrança gera um código **Pix "copia e cola"** (padrão BR Code/EMV do Bacen) totalmente local, sem API externa nenhuma: `Escola.Infrastructure/Pagamentos/PixBrCode.cs`, monta o payload TLV (chave, nome, cidade, valor, txid) e calcula o CRC16 na mão.

- **Configuração da chave Pix**: linha única `ConfiguracaoFinanceira` (chave, nome do recebedor, cidade da escola) — `GET /api/financeiro/configuracao` liberado pro grupo `Financeiro` (precisa pra gerar os códigos), `PUT` restrito a `Gestao` (é identidade financeira da escola, não é operacional do dia a dia). Editável no próprio `financeiro-lista` via botão "⚙️ Configurar Pix" (só visível pra quem é Gestão).
- **Código por cobrança**: `GET /api/financeiro/cobrancas/{id}/pix`, mesmo padrão Equipe+Responsável-dono das outras rotas por cobrança/aluno. Retorna 400 com mensagem amigável ("chave Pix da escola ainda não foi configurada") em vez de gerar um código quebrado quando a config está vazia — testado explicitamente nos dois estados (configurado/não configurado).
- **E-mail**: infraestrutura pronta mas **não configurada** — não tem conta de SMTP ainda (confirmado com o usuário). `IEmailSender`/`SmtpEmailSender` (MailKit) com `Configurado` (bool, true só se Host/Usuario/Senha/RemetenteEmail estiverem todos preenchidos). Config vem de `appsettings.Development.json` seção `Smtp:*` (não fica no banco, ao contrário do Pix) — é credencial técnica única, mesmo padrão de `Jwt:Chave`/connection string, não dado de negócio editável por Gestão. `POST /api/financeiro/cobrancas/{id}/enviar-email` (grupo `Financeiro`) retorna 400 com mensagem clara quando `!Configurado`, em vez de 500 — testado e confirmado que a UI mostra esse erro de forma amigável no botão "✉️" do card da cobrança. Pra habilitar de verdade, falta só preencher a seção `Smtp` com credenciais reais (ex.: app password do Gmail).
