#!/bin/bash
# Gera os PNGs 1080x1350 de cada slide em ./png (precisa do Chrome e do Node).
# Uso: bash exportar.sh            -> todos os carrosséis de decks.js
#      bash exportar.sh nome ...   -> só os carrosséis indicados
cd "$(dirname "$0")"; mkdir -p png
CH="/c/Program Files/Google/Chrome/Application/chrome.exe"
if [ $# -gt 0 ]; then FILTRO=" $* "; else FILTRO=""; fi
node -e "
const fs=require('fs');
const src=fs.readFileSync('decks.js','utf8')+'\n;decks';
const decks=eval(src);
for(const [k,v] of Object.entries(decks)) console.log(k, v.length);
" | while read d n; do
  if [ -n "$FILTRO" ] && [[ "$FILTRO" != *" $d "* ]]; then continue; fi
  for i in $(seq 1 $n); do
    "$CH" --headless --disable-gpu --hide-scrollbars --window-size=1080,1350 \
      --screenshot="$(cygpath -w "$PWD/png/$d-$i.png")" \
      "file:///$(cygpath -m "$PWD/carrosseis.html")#$d-$i" >/dev/null 2>&1
  done
  echo "$d: $n slides"
done
