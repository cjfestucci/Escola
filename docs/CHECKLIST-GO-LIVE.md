# Checklist de go-live — acompanhamento

Lista viva do que falta pra colocar o Gestor Tático no ar. O "como" de cada item está em [`GO-LIVE.md`](GO-LIVE.md).
Marcamos aqui conforme concluímos. **👤** = você · **🤖** = eu.

Legenda de situação: ⬜ a fazer · 🟨 em andamento · ✅ concluído · ⏸️ esperando terceiros

## Agora (rápidos, destravam testes)

| # | Tarefa | Quem | Situação | Observação |
|---|---|---|---|---|
| A1 | Criar conta no **sandbox do Asaas** (com o CNPJ) e gerar a chave de API (`$aact_hmlg_…`) | 👤 | ✅ | chave no `appsettings.Development.json` (fora do git) |
| A2 | Validar assinatura, cupom e Pix das escolas no sandbox | 🤖👤 | ✅ | 14/10/2026: cadastro → assinatura R$ 140; pagamento → "Ativa" + valor R$ 60 sincronizado; cupom R$ 112; subconta aprovada (autoaprovação do sandbox ligada) → Pix pelo Asaas → baixa automática. Achados corrigidos: cobrança órfã quando o QR falha e cliente duplicado no Asaas |
| A3 | Escolher e conferir se o **domínio** está livre | 👤🤖 | ✅ | gestortatico.com.br, .com e .app livres no RDAP (14/10/2026) |
| A4 | Definir o **número de WhatsApp** de suporte (o site promete) | 👤 | ⬜ | |

## Empresa e jurídico (os mais lentos — começar em paralelo)

| # | Tarefa | Quem | Situação | Observação |
|---|---|---|---|---|
| B1 | Contratar **contador** (que atenda SaaS) | 👤 | ✅ | já tem; confirmar com ele: código de serviço da NFS-e (item 1.05?), anexo do Simples/fator R, incluir CNAE 63.11-9-00? |
| B2 | **CNPJ** (ME, CNAE 62.03-1-00 de licenciamento, inscrição municipal) | 👤 | ✅ | conferido em 14/10/2026 |
| B3 | **Conta bancária PJ** | 👤 | ✅ | já tem |
| B4 | Pedido da **marca no INPI** (opcional, recomendado) | 👤 | ⬜ | |
| B4a | **Endereço da sede** desatualizado na Receita (Rua Blumenau, 278 — não mora mais lá) | 👤 | ⬜ | atualizar com o contador; depois Asaas, Termos/Privacidade e Google Workspace (este não deixa mudar). Nome fantasia fica "CJF Technology" (decidido em 14/10/2026) |
| B5 | **Advogado** revisa Termos, Privacidade e termo de matrícula; definir o **DPO** | 👤 | ⬜ | |
| B6 | Aplicar textos revisados + dados da empresa | 🤖 | 🟨 | dados da empresa, DPO (Cleber Festucci), hospedagem (Hostinger, Campinas/SP) e backup fora do país (R2) aplicados em 15/10/2026 (versão 2026-10-15.2); falta só a revisão do advogado (B5), opcional antes do 1º clube |

## Domínio e e-mail

| # | Tarefa | Quem | Situação | Observação |
|---|---|---|---|---|
| C1 | **Comprar o domínio** | 👤 | ✅ | gestortatico.com.br no CNPJ da empresa (14/10/2026); .com ainda livre (opcional) |
| C2 | **DNS na Cloudflare** | 👤 | ✅ | delegação para armfazh/molly.ns.cloudflare.com concluída em 14/10/2026; registros importados (MX nulo, SPF -all, DMARC reject) mudam no C3/C4 |
| C3 | **E-mail profissional** (contato@, suporte@, financeiro@, privacidade@) | 👤 | ✅ | Google Workspace Business Starter (mensal, R$ 39,20 por 12 meses, depois R$ 49; teste até 28/10/2026), no CNPJ; domínio verificado e Gmail ativo em 14/10/2026 (MX smtp.google.com, SPF include:_spf.google.com -all); DKIM do Google (google._domainkey, 2048 bits); caixa cleber@ com 2FA + chave de acesso + recuperação; apelidos contato@, suporte@, financeiro@, privacidade@ e "Enviar como"; teste de ida e volta com SPF/DKIM/DMARC = PASS |
| C4 | **E-mail transacional** + SPF/DKIM/DMARC | 👤 | ✅ | **produção desde 15/10/2026: SMTP relay do Google Workspace** (`smtp-relay.gmail.com:587`, só do IP do servidor, com senha de app; sem rastreio nem "cancelar inscrição", que a Brevo confirmou serem obrigatórios). Antes: conta Brevo (300/dia grátis); domínio autenticado em 14/10/2026 (TXT brevo-code + 2 CNAME DKIM, DNS only; DMARC p=reject mantido); remetente Gestor Tático <nao-responda@gestortatico.com.br>; chave SMTP de dev no Bitwarden |
| C5 | Configurar SMTP no sistema e testar entrega | 🤖 | ✅ | dev: smtp-relay.brevo.com:587; convite real chegou na **caixa de entrada** do Gmail em 14/10/2026 (convite de teste cancelado). Brevo não deixa desligar rastreio nem o "Cancelar inscrição" (rastreio ficou anônimo; pedido ao suporte do Brevo em aberto). Se alguém não recebe e-mail: Configurações → E-mail transacional → **Contatos bloqueados ou cancelados** → desbloquear. Produção usa outra chave (F2) |

## Asaas produção

| # | Tarefa | Quem | Situação | Observação |
|---|---|---|---|---|
| D1 | Abrir conta **PJ** no Asaas e enviar documentos | 👤 | ✅ | conta de produção (www.asaas.com) do CNPJ já existia e está **aprovada** (conferido em 14/10/2026); login no e-mail pessoal com Token SMS, e-mail da empresa financeiro@; nome fantasia CJF Technology, endereço da Receita, cor e logo na fatura; conta duplicada encerrada. Taxas: cartão 1,99%+0,49 (promo até 07/01/2027), boleto R$ 0,99, notificações R$ 0,99/cobrança (desligadas pelo sistema), Pix dinâmico grátis, **subconta R$ 12,90** (confirmar se é mensal). Conta bancária PJ cadastrada (14/10/2026) |
| D2 | Chave Pix da conta + conversar sobre **subcontas** | 👤🤖 | 🟨 | resposta do Asaas (14/10/2026): R$ 12,90 por subconta, debitado do saldo na criação; período de avaliação (60 dias, até 10 subcontas, R$ 2.000 cada); documentos pelo `onboardingUrl`. 🤖 tela de documentos pendentes com o link feita. **Falta**: pedir ao gerente do Asaas a habilitação de subcontas **BaaS/white label com onboardingUrl** (no sandbox o link veio nulo) e criar a chave Pix da conta |
| D3 | Chave de API de **produção** (direto no servidor, não precisa me mandar) | 👤 | ✅ | 15/10/2026: `servidor-gestortatico`, sem saque e sem expiração, gravada com `gt-definir ASAAS_API_KEY`; conta APPROVED. A `AssinaturasWorker` consulta o Asaas 1x/dia pra chave não ser desabilitada por falta de uso (3 meses) |
| D4 | **Webhook** da assinatura no painel | 👤🤖 | ✅ | 15/10/2026: "Gestor Tático – assinaturas" → `/api/assinaturas/asaas/webhook`, eventos de Cobranças, token = `ASSINATURA_WEBHOOK_TOKEN`; sem token o endpoint responde 401. Entrega real só será vista no G2 |
| D5 | **NFS-e** no Asaas (com o contador) | 👤 | ⬜ | |

## Servidor

| # | Tarefa | Quem | Situação | Observação |
|---|---|---|---|---|
| E1 | Contratar **VPS** no Brasil (2 vCPU / 4 GB) | 👤 | ✅ | Hostinger KVM 2 (2 vCPU, 8 GB, 100 GB NVMe), Brasil, Ubuntu 24.04, 12 meses (R$ 557,89, contratado 14/10/2026 — anotar a renovação); IP 187.127.44.70; acesso só por chave SSH `~/.ssh/gestortatico_ed25519` (cópia no Bitwarden), senha root no Bitwarden |
| E2 | Instalar Docker, subir banco + API + app + site, **HTTPS** | 🤖 | 🟨 | 14/10/2026: servidor endurecido (SSH só por chave, ufw 22/80/443, fail2ban, atualizações automáticas, fuso SP), Docker, código por deploy key, `deploy/producao` (Postgres + API + app + Caddy) no ar com Let's Encrypt: app.gestortatico.com.br, gestortatico.com.br, www → apex. `.env` com segredos gerados no servidor. `.env` guardado no Bitwarden; SMTP de produção (chave própria do Brevo) testado do servidor em 14/10/2026; `gt-definir NOME` grava segredo no `.env` sem mostrar na tela. Falta: Suporte (hash + TOTP), Asaas |
| E3 | **Backup** diário fora do servidor + teste de restauração | 🤖 | ✅ | 15/10/2026: banco + fotos + documentos criptografados (age; chave privada só no Bitwarden) pro Cloudflare R2 todo dia às 03:30 (systemd), 3 últimos também no servidor; teste de restauração OK (`gt-backup-testar`, repetir a cada trimestre). Ver `deploy/producao/servidor/LEIA-ME.md` |
| E4 | **Monitoramento** (UptimeRobot no `/healthz`) | 👤🤖 | ✅ | 15/10/2026: **UptimeRobot** (grátis, 5 min, e-mail cleber@) com "Gestor Tático – App" (`/healthz`, que testa o banco — a raiz do app é estática e fica 200 com a API fora) e "Gestor Tático – Site"; queda simulada (API parada) alertou em 4 min. **Healthchecks.io** check "Backup diário" (cron `30 3 * * *`, fuso São Paulo, 1 h de tolerância): o `gt-backup` avisa início/sucesso/falha e disco ≥ 85% (`HC_BACKUP_URL` no `.env`); falha simulada chegou por e-mail |

## Configuração e validação

| # | Tarefa | Quem | Situação | Observação |
|---|---|---|---|---|
| F1 | Gerar segredos e guardar no **cofre de senhas** | 🤖👤 | ⬜ | |
| F2 | `.env` de produção, `URL_APP`, `urlSite` | 🤖 | ⬜ | Brevo: chave SMTP só de produção + ligar "bloquear IPs não autorizados" com o IP da VPS |
| F3 | Validação completa (e-mails, celular, backup, segurança, CI) | 🤖 | ⬜ | |

## Lançamento

| # | Tarefa | Quem | Situação | Observação |
|---|---|---|---|---|
| G1 | Virar o Asaas para **produção** | 🤖 | ✅ | 15/10/2026: `ASAAS_AMBIENTE=Producao` só pra **assinatura dos clubes**; subcontas das escolas desligadas (`ASAAS_SUBCONTAS=false`) até o D2 |
| G2 | **Assinatura real** de teste (sua) | 👤 | ⬜ | |
| G3 | Primeiro **clube piloto** com cupom de lançamento | 👤🤖 | ⬜ | |
| G4 | Após ~1 mês: ligar a **exclusão automática** de dados | 🤖 | ⬜ | |
