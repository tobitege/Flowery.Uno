#!/usr/bin/env bash
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

bash "$SCRIPT_DIR/build-android.sh"
bash "$SCRIPT_DIR/build-dmg-macos.sh"
bash "$SCRIPT_DIR/build-windows.sh"
bash "$SCRIPT_DIR/build-deb-linux.sh"
