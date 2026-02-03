#!/bin/bash

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
pwsh -ExecutionPolicy Bypass -File "$SCRIPT_DIR/build_kiosk_browser.ps1" "$@"
