# Ideia: módulo "Desempenho" com análise de vídeo do treino

Situação: **em teste**. Registrada em 24/11/2026, a partir da sessão "Ideias de sistemas para futebol". Decisão do dono (24/11/2026): **testar já** com um vídeo real e, se funcionar, implementar e vender também como **produto separado** (ver "Produto separado ou junto com a gestão"). O teste roda no Google Colab, com o notebook [`prototipos/desempenho-video/desempenho_colab.ipynb`](../../prototipos/desempenho-video/desempenho_colab.ipynb), porque o computador de desenvolvimento não tem GPU NVIDIA nem Python.

## O que é

O treinador grava o treino ou o jogo com o celular num tripé, na lateral do campo. Um serviço de visão computacional processa o vídeo e devolve, por atleta:

- distância percorrida, tempo em campo, m/min, velocidade máxima e número de sprints;
- mapa de calor;
- clipes automáticos (os sprints mais rápidos e os lances marcados pelo treinador com um botão "lance!" durante a gravação).

Como funciona:

1. **Calibração do campo:** o treinador clica uma vez nos 4 cantos do campo. A homografia converte pixel em metros e fica salva para o campo.
2. **Detecção e rastreio:** YOLO (pessoa) + ByteTrack. Quem está fora do campo (pais, banco) é descartado.
3. **Identificação:** OCR do número do colete, com votação ao longo do track, e separação dos times pela cor do tronco. O sistema **sugere** quem é quem e o treinador confirma numa grade de fotos.
4. **Métricas:** calculadas depois da revisão, em segundos e sem reprocessar o vídeo. Recalculadas a cada correção de identidade.

## Por que dentro do Gestor Tático

Sozinho, seria só mais um app de análise. Dentro da gestão, aproveita o que já existe:

- **Chamada:** os presentes do dia viram a lista de atletas esperados no vídeo.
- **Atletas:** aba "Desempenho" na ficha, com a evolução, os mapas de calor e os clipes.
- **Portal da Família:** card "o treino do seu filho", com os clipes. É o ponto de maior retenção (o pai vê valor toda semana).
- **LGPD:** o consentimento de imagem fica no mesmo cadastro do termo de matrícula. O serviço só processa atletas com consentimento ativo.
- **Venda:** add-on de plano mais caro.

## Produto separado ou junto com a gestão

O vídeo chama atenção e pode ser a **porta de entrada**: o clube contrata só a análise, cadastra o elenco para usá-la e fica a um passo de usar a gestão inteira (chamada, mensalidades, portal). Planos possíveis: "Só análise de vídeo", "Gestão" e "Gestão + vídeo".

Como encaixa no que já existe:

- **Módulos por cliente:** hoje o `Cliente.Segmento` decide quais menus aparecem. Um campo novo (por exemplo `Cliente.Modulos`, com Gestão e Desempenho) faria o mesmo com os módulos. Quem contrata só o vídeo vê **Atletas (cadastro reduzido) + Desempenho + Configurações**. Os vocabulários, o login único e a assinatura continuam os mesmos.
- **Bloqueio no servidor, não só no menu:** endpoint de um módulo não contratado responde 403, no mesmo espírito do bloqueio da assinatura (`Autenticacao.LiberadoComAssinaturaBloqueada`). Um teste como o `PermissoesDosEndpointsTests` garante que todo endpoint novo declara o módulo.
- **Assinatura:** o `/assinar` ganha a escolha do plano, e o valor (`OpcoesAssinatura.CalcularValor`, `site/precos.js`) passa a depender do plano, além do número de atletas. Mudar de plano pela tela de Assinatura é o caminho do upsell.
- **Dado novo no atleta:** número da camisa/colete (hoje não existe), usado para casar o OCR com o atleta.
- **O que o módulo isolado precisa da gestão:** cadastro de atletas e turmas, responsáveis (para o consentimento de imagem e o card dos pais) e o Portal da Família. A chamada é opcional (sem ela, o treinador marca os presentes na própria gravação).
- **Site e Instagram:** só anunciar depois do teste. Página própria no site (por exemplo `analise-de-video-escolinha-de-futebol.html`) quando estiver no ar.

## Arquitetura

Integrar no produto, **mas não no mesmo código**: a visão computacional é um serviço à parte (Python, PyTorch, GPU sob demanda) e a gestão (.NET) orquestra.

```
Gestão (.NET)                                   Serviço de visão (Python, GPU)
  1. cria a sessão + URL de upload
  2. app envia o vídeo  ──►  storage de objetos (prefixo por cliente)
  3. publica o job na fila  ──────────────────►  processa: tracks, sugestões, clipes
  5. recebe o webhook "concluído"  ◄──────────  4. grava os resultados no storage
  6. treinador revisa na tela
  7. pede o recálculo das métricas  ──────────►  endpoint leve (CPU, segundos)
```

Por que separar: stack diferente, a GPU só liga quando há vídeo na fila, uma falha no processamento não derruba financeiro e chamada, e o modelo de IA evolui sem deploy da gestão.

O serviço **não conhece o banco**. Recebe um job e devolve um resultado.

Job (gestão → serviço):

```json
{
  "sessao_id": "a1b2...",
  "video_url": "https://storage/.../treino.mp4",
  "calibracao": { "pontos_imagem": [[0, 0], [0, 0], [0, 0], [0, 0]], "comprimento": 40, "largura": 20 },
  "presentes": [ { "atleta_id": "…", "numero": 7 }, { "atleta_id": "…", "numero": 10 } ],
  "lances": [ 312.5, 1045.0 ],
  "categoria": "Sub-13",
  "callback_url": "https://app.gestortatico.com.br/api/desempenho/sessoes/a1b2/resultado"
}
```

Resultado (serviço → gestão):

```json
{
  "sessao_id": "a1b2...",
  "status": "concluido",
  "tracks": [
    { "track": 17, "numero_sugerido": 7, "confianca": 0.82, "atleta_id_sugerido": "…",
      "recorte_url": "...", "inicio_s": 0, "fim_s": 840 }
  ],
  "deteccoes_url": "https://storage/.../deteccoes.parquet",
  "clipes": [ { "tipo": "lance", "tempo_s": 312.5, "url": "..." } ]
}
```

O webhook segue a regra do projeto para webhooks: o corpo só diz qual sessão conferir, e a gestão busca o resultado no storage com credencial própria (token no cabeçalho, comparação em tempo fixo).

## Modelo de dados (rascunho)

Todas as entidades isoladas por cliente (`ClienteId` automático), com auditoria.

| Entidade | Para quê |
|---|---|
| `Campo` | Local (Unidade), dimensões e calibração salva, reaproveitada em toda gravação no mesmo campo |
| `SessaoVideo` | Turma, data (ligada à chamada do dia), status (enviando, processando, revisão, concluída, erro), duração e minutos consumidos |
| `SessaoTrack` | Track sugerido, atleta confirmado e quem confirmou |
| `MetricaAtletaSessao` | Distância, tempo, m/min, velocidade máxima e sprints, por atleta e sessão. Alimenta a evolução, o ranking da turma e o card dos pais |
| `Clipe` | Caminho no storage, tipo (sprint ou lance), atleta e se aparece para a família |
| `ConsentimentoImagem` | Autorização do responsável, data, versão do texto e escopo (uso interno, Portal da Família, divulgação pública) |

## Telas

- **Chamada / treino:** botão "Gravar treino" com os presentes já carregados.
- **Revisão:** grade de fotos com o nome sugerido. O treinador confirma com um toque. Meta: menos de 3 a 5 minutos por treino.
- **Ficha do atleta:** aba "Desempenho" (gráficos de evolução, mapas de calor e clipes).
- **Portal da Família:** card "o treino do seu filho", só com os dados e clipes do próprio filho (mesma regra de privacidade da convocação).
- **Painel do dono:** minutos consumidos no mês e turmas que mais usam.

## Como vender

| Plano | O que inclui |
|---|---|
| Gestão (atual) | Sem vídeo |
| Gestão + Desempenho | X horas de vídeo por mês, relatórios e clipes para os pais |
| Hora adicional | Avulsa, para quem gravar mais que o pacote |
| Premium para os pais (opcional) | Vídeo completo e download dos clipes, pago pelo próprio pai |

Custo estimado de processamento: cerca de R$ 2 a 3 por hora de vídeo (a validar no protótipo, medindo o tempo de GPU por minuto de vídeo).

## Roteiro

1. **Protótipo offline** num vídeo de treino real, com 2 ou 3 clubes: o notebook do Colab em `prototipos/desempenho-video/` (o mesmo pipeline dos scripts Python da sessão "Ideias de sistemas para futebol", juntado num notebook, com a calibração por clique no próprio navegador). Ainda **não foi executado**.
2. **Integração mínima:** a gestão cria a sessão, o vídeo é enviado manualmente, o serviço processa, a revisão acontece numa tela simples e as métricas aparecem na ficha do atleta. Valida se o treinador grava toda semana.
3. **Gravação pelo app:** upload, calibração e botão "lance!", com a tela de revisão.
4. **Portal da Família** com clipes e evolução. Valida se o pai vê valor.
5. **Cobrança do add-on** e medição do consumo.

O que medir no protótipo:

| Pergunta | Como medir |
|---|---|
| A detecção funciona? | Vídeo de debug: algum jogador fica sem caixa por muito tempo? |
| A identificação funciona? | % de tracks resolvidos pelo número e quantos tracks por atleta (muitos = rastreio fragmentado) |
| A revisão é viável? | Cronometrar. Meta: menos de 3 a 5 minutos por treino |
| As métricas são confiáveis? | Comparar a distância com um relógio GPS num atleta. Até ~15% de diferença está ok |
| Quanto custa? | Tempo de processamento por minuto de vídeo na GPU |

## Riscos e limitações

- **LGPD:** imagem de menor de idade. Consentimento do responsável com escopo, antes de qualquer gravação, inclusive no protótipo (vídeos guardados só localmente). Precisa de texto próprio, revisado juridicamente.
- **Retenção:** apagar o vídeo bruto em 30 dias e guardar só clipes e métricas. Escrever isso nos Termos.
- **Isolamento:** storage com prefixo por cliente e links assinados e temporários (como os de `/uploads`), nunca públicos.
- **Troca de identidade** quando jogadores se sobrepõem: aumentar o `track_buffer` do ByteTrack e, depois, usar re-identificação por aparência (BoT-SORT).
- **OCR genérico erra** em número pequeno e borrado: depois, trocar por um classificador de dígitos treinado com os recortes revisados (a revisão vira dataset).
- **Câmera parada:** se o tripé mexer, a calibração perde a validade.
- **Hospedagem:** o servidor atual (Hostinger KVM 2) não tem GPU. O processamento precisaria de GPU sob demanda num provedor à parte, com o vídeo saindo do Brasil (declarar como transferência internacional na Política de Privacidade, como o backup no R2).
- **Não prometer** nada disso no site ou no Instagram antes do protótipo provar que funciona.
