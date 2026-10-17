#!/usr/bin/env bash
# Renders the Gallery app_icon.svg to a square PNG.
# Uses rsvg-convert when available (Linux/CI: apt install librsvg2-bin; macOS: brew install librsvg),
# otherwise qlmanage + sips (macOS only).
#
#   packaging/render-icon.sh <size> <output.png>
set -euo pipefail

SIZE="${1:?uso: $0 <size> <output.png>}"
OUT="${2:?uso: $0 <size> <output.png>}"
PROJECT_ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"
SVG="$PROJECT_ROOT/Flowery.Uno.Gallery.Core/Assets/app_icon.svg"

TMP="$(mktemp -d "${TMPDIR:-/tmp}/flowery-icon.XXXXXX")"
trap 'rm -r "$TMP"' EXIT

mkdir -p "$(dirname "$OUT")"
if command -v rsvg-convert >/dev/null; then
    rsvg-convert -w "$SIZE" -h "$SIZE" -o "$OUT" "$SVG"
elif command -v qlmanage >/dev/null && command -v sips >/dev/null; then
    qlmanage -t -s 1024 -o "$TMP" "$SVG" >/dev/null 2>&1
    sips -z "$SIZE" "$SIZE" "$TMP/app_icon.svg.png" --out "$OUT" >/dev/null
else
    echo "[ERRORE] serve rsvg-convert (librsvg) oppure macOS (qlmanage + sips)" >&2
    exit 1
fi
