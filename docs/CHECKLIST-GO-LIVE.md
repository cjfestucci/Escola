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
| B5 | **Advogado** revisa Termos, Privacidade e termo de matrícula; definir o **DPO** | 👤 | ⬜ | |
| B6 | Aplicar textos revisados + dados da empresa | 🤖 | ⬜ | depende de B2 e B5 |

## Domínio e e-mail

| # | Tarefa | Quem | Situação | Observação |
|---|---|---|---|---|
| C1 | **Comprar o domínio** | 👤 | ⬜ | depende de A3 |
| C2 | **DNS na Cloudflare** | 👤 | ⬜ | |
| C3 | **E-mail profissional** (contato@, suporte@, financeiro@, privacidade@) | 👤 | ⬜ | |
| C4 | **E-mail transacional** (Brevo/Resend/SES) + SPF/DKIM/DMARC | 👤 | ⬜ | |
| C5 | Configurar SMTP no sistema e testar entrega | 🤖 | ⬜ | depende de C4 |

## Asaas produção

| # | Tarefa | Quem | Situação | Observação |
|---|---|---|---|---|
| D1 | Abrir conta **PJ** no Asaas e enviar documentos | 👤 | ⬜ | depende de B2/B3 |
| D2 | Chave Pix da conta + conversar sobre **subcontas** (perguntar: como a escola envia os documentos — link de onboarding?) | 👤 | ⬜ | o sistema ainda não tem tela de envio de documentos (KYC) da escola — 🤖 implementar conforme a resposta |
| D3 | Chave de API de **produção** (direto no servidor, não precisa me mandar) | 👤 | ⬜ | |
| D4 | **Webhook** da assinatura no painel | 👤🤖 | ⬜ | eu gero o token |
| D5 | **NFS-e** no Asaas (com o contador) | 👤 | ⬜ | |

## Servidor

| # | Tarefa | Quem | Situação | Observação |
|---|---|---|---|---|
| E1 | Contratar **VPS** no Brasil (2 vCPU / 4 GB) | 👤 | ⬜ | |
| E2 | Instalar Docker, subir banco + API + app + site, **HTTPS** | 🤖 | ⬜ | depende de E1 e C2 |
| E3 | **Backup** diário fora do servidor + teste de restauração | 🤖 | ⬜ | |
| E4 | **Monitoramento** (UptimeRobot no `/healthz`) | 👤🤖 | ⬜ | |

## Configuração e validação

| # | Tarefa | Quem | Situação | Observação |
|---|---|---|---|---|
| F1 | Gerar segredos e guardar no **cofre de senhas** | 🤖👤 | ⬜ | |
| F2 | `.env` de produção, `URL_APP`, `urlSite` | 🤖 | ⬜ | |
| F3 | Validação completa (e-mails, celular, backup, segurança, CI) | 🤖 | ⬜ | |

## Lançamento

| # | Tarefa | Quem | Situação | Observação |
|---|---|---|---|---|
| G1 | Virar o Asaas para **produção** | 🤖 | ⬜ | |
| G2 | **Assinatura real** de teste (sua) | 👤 | ⬜ | |
| G3 | Primeiro **clube piloto** com cupom de lançamento | 👤🤖 | ⬜ | |
| G4 | Após ~1 mês: ligar a **exclusão automática** de dados | 🤖 | ⬜ | |
