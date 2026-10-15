gt_gravar() {
  local F=/opt/gestortatico/deploy/producao/.env tmp
  [[ "$2" != *"'"* ]] || { echo "Valor com aspas simples não é aceito."; return 1; }
  tmp=$(mktemp); grep -v "^$1=" "$F" > "$tmp" || true
  printf "%s='%s'\n" "$1" "$2" >> "$tmp"; install -m 600 "$tmp" "$F"; rm -f "$tmp"
}
