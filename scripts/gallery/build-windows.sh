#!/usr/bin/env bash
set -e

# ============================================================
# Flowery.Uno Gallery — Windows x64 + arm64 (portable zips)
#
# Cross-published from macOS with the net10.0-desktop TFM (Skia/Win32),
# self-contained: unzip and run Flowery.Uno.Gallery.exe.
# ============================================================

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
OUT_DIR="$SCRIPT_DIR/output/installer"
PROJECT="$PROJECT_ROOT/Flowery.Uno.Gallery/Flowery.Uno.Gallery.csproj"
TFM=net10.0-desktop

echo "Pulizia output precedente..."
mkdir -p "$OUT_DIR"
rm -f "$OUT_DIR"/FloweryUnoGallery_win-*.zip

dotnet restore "$PROJECT" -p:TargetFrameworks=$TFM -nologo -v quiet
VERSION="$(dotnet msbuild "$PROJECT" -nologo -getProperty:Version -p:TargetFramework=$TFM | tr -d '[:space:]')"
echo "Versione rilevata: $VERSION"

for pair in "x64:win-x64" "arm64:win-arm64"; do
    arch="${pair%%:*}"
    rid="${pair##*:}"
    PUBLISH_DIR="$SCRIPT_DIR/output/publish/$arch"

    echo ""
    echo "[$arch] dotnet publish ($rid)..."
    rm -r "$PUBLISH_DIR" 2>/dev/null || true
    dotnet publish "$PROJECT" -f $TFM -p:TargetFrameworks=$TFM -c Release -r "$rid" --self-contained \
        -p:UseMonoRuntime=false -p:DebugType=None -p:DebugSymbols=false -o "$PUBLISH_DIR" -nologo

    echo "[$arch] zip..."
    ZIP="$OUT_DIR/FloweryUnoGallery_${rid}.zip"
    (cd "$PUBLISH_DIR" && zip -qr -X "$ZIP" .)
    ls -lh "$ZIP"
done
