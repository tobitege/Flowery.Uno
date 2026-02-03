#!/bin/bash
# build_windows.sh - Wrapper to call build_windows.ps1 from Git Bash
# Usage: ./scripts/build_windows.sh [args]
# Example: ./scripts/build_windows.sh -Rebuild

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
pwsh -ExecutionPolicy Bypass -File "$SCRIPT_DIR/build_windows.ps1" "$@"
