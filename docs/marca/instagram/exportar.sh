#!/bin/bash
# Gera os PNGs 1080x1350 de cada slide em ./png (precisa do Chrome). Uso: ./exportar.sh
cd "$(dirname "$0")"; mkdir -p png
CH="/c/Program Files/Google/Chrome/Application/chrome.exe"
for d in mensalidade chamada convocacao; do
  for i in 1 2 3 4 5 6 7 8; do
    [ "$d" != mensalidade ] && [ $i -gt 7 ] && continue
    "$CH" --headless --disable-gpu --hide-scrollbars --window-size=1080,1350 \
      --screenshot="$(cygpath -w "$PWD/png/$d-$i.png")" \
      "file:///$(cygpath -m "$PWD/carrosseis.html")#$d-$i" >/dev/null 2>&1
  done
done
