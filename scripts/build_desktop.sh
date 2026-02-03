#!/bin/bash
# build_desktop.sh - Wrapper to call build_desktop.ps1 from Git Bash
# Usage: ./scripts/build_desktop.sh [args]
# Example: ./scripts/build_desktop.sh -NoRun -Log

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
pwsh -ExecutionPolicy Bypass -File "$SCRIPT_DIR/build_desktop.ps1" "$@"
