# Publicando o Rotina Escola

**Um site só atende todos os clientes** (escolas): mesma tela de login, e é a **conta que entra** que define a escola (desde 2026-10-07).
Todos usam o mesmo banco PostgreSQL (os dados são separados por `ClienteId`). Então **um** `docker-compose.yml` com **um** `.env` serve
pra todos — `CLIENTE_ID`/`CLIENTE_NOME` só dizem qual é o cliente criado na primeira subida.

## Cliente (escola) novo

Ainda não há tela de cadastro (vem com a assinatura pelo site). Por enquanto:

1. Inserir a escola: `INSERT INTO "Clientes" ("Id","Nome","Ativo","CriadoEm","Segmento") VALUES (gen_random_uuid(), 'Nome da Escola', true, now(), 1);`
   (`Segmento`: 0 = escola, 1 = clube).
2. **Reiniciar a API**: a conta de Suporte é criada em todos os clientes a cada subida.
3. Entrar com o Suporte (ele escolhe a escola no login), abrir **Plataforma → Administradores da escola** e convidar o Admin.

## O que o CI garante (a cada push/PR — `.github/workflows/ci.yml`)

| Job | O que prova |
|---|---|
| `backend` | compila e roda os ~190 testes (regras de dinheiro, frequência, disciplina, 2FA, permissões de **todos** os endpoints, isolamento entre clientes, convite do admin) |
| `migrations` | num Postgres vazio, as ~30 migrations aplicam até a última e a API responde `/healthz` |
| `frontend` | testes no Chrome sem interface + build de produção |
| `docker` | as duas imagens compilam |

Rodar local: `dotnet test backend/Escola.Tests` e `cd frontend && npm run test:ci`.
(Se a API de desenvolvimento estiver rodando, ela trava os DLLs de Debug: use `dotnet test backend/Escola.Tests -c Release`.)

## Primeira vez

1. **Banco**: um PostgreSQL 14+ com um banco e um usuário só da aplicação (não o `postgres`).
2. `cp .env.example .env` e preencha. Obrigatórios: `DB_CONNECTION`, `JWT_CHAVE` (32+ caracteres aleatórios),
   `CLIENTE_ID` (GUID do primeiro cliente), `CLIENTE_NOME`, `APP_URL_BASE`.
3. `docker compose up -d --build`. A API aplica as migrations, cria a linha do cliente em `Clientes` e passa a responder em `/healthz`.
4. Coloque um **proxy com HTTPS** na frente da porta `PORTA_SITE` (Caddy, Traefik, nginx do host…). O app não termina TLS.
5. **Criar o primeiro Admin da escola**: configure a conta de Suporte (abaixo), entre em `/entrar` com ela, abra **Plataforma →
   Administradores da escola** e envie o convite. A pessoa recebe o e-mail, confirma e cadastra a própria senha.
   (Isso exige SMTP configurado — sem ele o convite é recusado em produção, de propósito.)

## Conta de Suporte e 2FA

```bash
dotnet run --project backend/Escola.Api -- --gerar-hash-senha     # pede a senha (12+ caracteres) → SUPORTE_SENHA_HASH
dotnet run --project backend/Escola.Api -- --gerar-segredo-totp   # → SUPORTE_TOTP_SEGREDO e o link otpauth:// pro app autenticador
```

Guarde senha e segredo num cofre. Sem `SUPORTE_*` completos a conta fica desativada neste ambiente (é assim que se revoga o acesso).

## Atualizar

`git pull && docker compose up -d --build`. As migrations rodam sozinhas ao subir (`MIGRAR_AO_INICIAR=true`).
Faça **backup do banco antes** — migration não tem "desfazer" automático. Em vários clientes no mesmo banco, a primeira cópia a subir
migra; as outras só encontram o banco já atualizado.

## Backup (o que precisa ser guardado)

- **O banco PostgreSQL** (tudo de todos os clientes) — `pg_dump` diário, testado de vez em quando com um restore de verdade.
- **Os volumes `fotos` e `documentos`** de cada cliente. `documentos` guarda **exames e laudos de menores**: dado sensível de saúde
  (LGPD) — o backup precisa ser protegido/criptografado como o banco.

## Checklist de segurança antes de ir pro ar

- [ ] `JWT_CHAVE` longa e aleatória (não a de teste).
- [ ] HTTPS no proxy; `APP_URL_BASE` com `https://`.
- [ ] `PROXIES_NA_FRENTE` certo (2 com proxy HTTPS na frente do site; 1 sem) — é o que grava o IP real no aceite do termo de matrícula.
- [ ] **Texto do termo de matrícula revisado** pela parte jurídica da escola (`backend/Escola.Api/Servicos/TermoMatricula.cs`; mudou o texto → mude a `Versao`).
- [ ] Banco acessível só pela rede interna, com usuário próprio da aplicação.
- [ ] `Suporte__*` configurado só onde o suporte precisa atuar; segredo 2FA no cofre.
- [ ] SMTP funcionando (teste o "Esqueceu a senha?" com um e-mail real).
- [ ] Backup do banco e dos volumes agendado **e restaurado uma vez** como teste.
- [ ] Pix automático: validado na **homologação do Banco do Brasil** antes de `PIX_BB_AMBIENTE=Producao` (hoje só foi testado contra um simulador).

## Limitações conhecidas

- O freio de tentativas de login é **em memória**: com mais de uma instância da API por cliente, cada uma conta as suas.
- Uma sessão já aberta continua valendo até expirar mesmo se a conta for desativada (30 dias; 12 h no Suporte) — ver `CLAUDE.md`.
- Não há agendamento automático de backup nem monitoramento: `/healthz` serve pra plugar o seu.
