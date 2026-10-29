# Gravação de Reels pelo app (celular simulado)

`semear-demo.js` cria no banco de **dev** o clube "Demonstração FC" (12 atletas fictícios e 4 treinos de histórico).
`gravar-reel.js` abre o app local numa tela 9:16, executa o roteiro da Chamada e gera o MP4 (1080x1920, H.264).

Precisa de: Docker + API (5080) + frontend (4200) rodando, Chrome, Node, FFmpeg (`winget install Gyan.FFmpeg`) e `npm i playwright-core`
numa pasta temporária. Os dois scripts leem a senha de teste de `../senha-admin-baas.txt` (fora do repositório) e foram feitos pra rodar
de uma pasta de trabalho (scratchpad); ajuste os caminhos no topo se mover. Antes de cada nova gravação o script apaga a chamada de hoje
do clube Demonstração.
