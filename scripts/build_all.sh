#!/bin/bash
# build_all.sh - Wrapper to call build_all.ps1 from Git Bash
# Usage: ./scripts/build_all.sh [args]
# Example: ./scripts/build_all.sh -Rebuild -RestoreWorkloads

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
pwsh -ExecutionPolicy Bypass -File "$SCRIPT_DIR/build_all.ps1" "$@"
