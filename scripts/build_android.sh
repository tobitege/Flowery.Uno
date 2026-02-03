#!/bin/bash
# build_android.sh - Wrapper to call build_android.ps1 from Git Bash
# Usage: ./scripts/build_android.sh [args]
# Example: ./scripts/build_android.sh -Rebuild

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
pwsh -ExecutionPolicy Bypass -File "$SCRIPT_DIR/build_android.ps1" "$@"
