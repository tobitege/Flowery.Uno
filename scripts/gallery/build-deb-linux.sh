#!/usr/bin/env bash
set -e

# ============================================================
# Flowery.Uno Gallery — Linux .deb packages (x64 and arm64), net10.0-desktop
#
# Prerequisites (one time):
#   brew install dpkg
#
# The actual work is done by packaging/linux/build-deb.sh.
# ============================================================

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
OUT_DIR="$SCRIPT_DIR/output/installer"

if ! command -v dpkg-deb &>/dev/null; then
    echo "[ERRORE] dpkg-deb non trovato. Installa con:"
    echo "  brew install dpkg"
    exit 1
fi

echo "Pulizia output precedente..."
mkdir -p "$OUT_DIR"
rm -f "$OUT_DIR"/flowery-uno-gallery_*.deb

for rid in linux-x64 linux-arm64; do
    "$SCRIPT_DIR/packaging/linux/build-deb.sh" "$rid" "$OUT_DIR"
done

ls -lh "$OUT_DIR"/flowery-uno-gallery_*.deb
