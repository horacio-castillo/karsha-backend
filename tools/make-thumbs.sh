#!/usr/bin/env bash
# Genera una miniatura JPG por plantilla en wwwroot/uploads/thumbs/{codigo}.jpg
# Requiere: API en Development corriendo en :5080 y Google Chrome instalado.
# Uso: backend/tools/make-thumbs.sh [codigo]   (sin argumento: todas)
set -euo pipefail
API="${API:-http://localhost:5080}"
CHROME="${CHROME:-/Applications/Google Chrome.app/Contents/MacOS/Google Chrome}"
OUT="$(cd "$(dirname "$0")/.." && pwd)/src/TarjetasPro.Api/wwwroot/uploads/thumbs"
mkdir -p "$OUT"
if [ $# -gt 0 ]; then codes="$1"; else
  codes=$(curl -sf "$API/api/templates/dev-render" | tr -d '[]"' | tr ',' '\n')
fi
n=0
for code in $codes; do
  "$CHROME" --headless=new --disable-gpu --hide-scrollbars --force-device-scale-factor=1 \
    --window-size=500,780 --virtual-time-budget=4000 \
    --screenshot="$OUT/$code.png" "$API/api/templates/dev-render/$code?wrap=true" >/dev/null 2>&1 \
    && sips -c 780 390 "$OUT/$code.png" >/dev/null \
    && sips -s format jpeg -s formatOptions 72 "$OUT/$code.png" --out "$OUT/$code.jpg" >/dev/null && rm -f "$OUT/$code.png" \
    && n=$((n+1)) || echo "falló: $code"
done
echo "Miniaturas generadas: $n en $OUT"
