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

## Backup (desde 2026-10-15)

| Arquivo | Onde fica no servidor | Uso |
|---|---|---|
| `gt-backup` | `/usr/local/bin/` | backup do banco (`pg_dump -Fc`) + volumes `fotos` e `documentos`, **criptografados com age** (só a chave pública fica no servidor), enviados pro Cloudflare R2 (`r2:gestortatico-backup/AAAA-MM-DD_HHMM/`) + os 3 últimos em `/root/backups`. Log em `/var/log/gt-backup.log`. Com `HC_BACKUP_URL` no `.env` (`gt-definir HC_BACKUP_URL`), avisa o healthchecks.io no início, no sucesso e na falha — e trata disco acima de 85% como falha |
| `gt-backup.service` / `gt-backup.timer` | `/etc/systemd/system/` | roda o `gt-backup` todo dia às 03:30 (`systemctl list-timers gt-backup.timer`) |
| `gt-backup-configurar` | `/usr/local/bin/` | grava as chaves do R2 no rclone (ocultas). O R2 precisa de `no_head = true` (não implementa o HEAD com `versionId`) e não aceita filtro de IP no token |
| `gt-backup-chave` | `/usr/local/bin/` | cria o par de chaves age: a **privada aparece uma vez** (vai pro Bitwarden: "Gestor Tático – chave dos backups") e é apagada |
| `gt-backup-testar` | `/usr/local/bin/` | **teste de restauração**: baixa o último backup do R2, confere SHA256, abre com a chave privada (digitada, só em memória), restaura num Postgres temporário e compara contagens com a produção. Fazer **a cada trimestre** |

No bucket do R2: **bucket lock de 30 dias** (nem a chave do servidor apaga) e **regra de ciclo de vida** que exclui depois de 35 dias.

**Restaurar de verdade** (servidor novo): instalar `age` e `rclone`, `gt-backup-configurar`, baixar a pasta do dia, `age -d -i chave.txt banco.dump.age > banco.dump`, subir o `db` vazio e `pg_restore -U gestortatico -d gestortatico --no-owner < banco.dump`; os `.tar.gz` vão pra `/var/lib/docker/volumes/gestortatico_{fotos,documentos}/_data`.
