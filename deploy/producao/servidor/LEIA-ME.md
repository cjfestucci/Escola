# Comandos do servidor de produção

Instalados no servidor pra gravar segredos no `deploy/producao/.env` **sem que passem pela tela de ninguém** (nem do chat):

| Arquivo | Onde fica no servidor | Uso |
|---|---|---|
| `gt-definir` | `/usr/local/bin/` | `gt-definir NOME` — pede o valor (oculto) e grava `NOME='valor'` no `.env` (ex.: `SMTP_SENHA`, `ASAAS_API_KEY`) |
| `gt-suporte-senha` | `/usr/local/bin/` | pede a senha do Suporte 2x e grava só o hash (`SUPORTE_SENHA_HASH`) |
| `gt-suporte-totp` | `/usr/local/bin/` | gera o segredo de 2 etapas do Suporte, grava `SUPORTE_TOTP_SEGREDO` e mostra o QR Code (precisa do pacote `qrencode`) |
| `gt-gravar.sh` | `/usr/local/lib/` | função usada pelos dois de cima |

Do Windows: `ssh -t -i "$HOME\.ssh\gestortatico_ed25519" root@187.127.44.70 gt-definir SMTP_SENHA`.
Depois de gravar: `cd /opt/gestortatico/deploy/producao && docker compose up -d api` — e atualizar a cópia do `.env` no Bitwarden.
Os valores ficam entre aspas simples no `.env` porque o docker compose interpreta `$` fora delas (a chave do Asaas começa com `$aact_`).
