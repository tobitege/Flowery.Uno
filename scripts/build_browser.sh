#!/bin/bash
# build_browser.sh - Wrapper to call build_browser.ps1 from Git Bash
# Usage: ./scripts/build_browser.sh [args]
# Example: ./scripts/build_browser.sh -NoRun -Rebuild

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
pwsh -ExecutionPolicy Bypass -File "$SCRIPT_DIR/build_browser.ps1" "$@"
