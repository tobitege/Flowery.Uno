#!/usr/bin/env bash
set -e

# ============================================================
# Flowery.Uno Gallery — macOS DMG build (arm64 + x64), direct distribution
#
# The Gallery has no maccatalyst TFM: net10.0-desktop (Skia) is published for
# osx-arm64 and osx-x64, and a single .app contains both payloads.
#
# Prerequisites:
#   brew install create-dmg
#
# Signing (optional):
#   FLOWERY_DEV_ID_CERT  name of the "Developer ID Application: ..." certificate in the Keychain
#                        (default: the maintainer's). If it is not in the Keychain, the bundle
#                        is signed ad-hoc ("-"), as in CI without secrets: the DMG opens
#                        with right-click > Open.
#   FLOWERY_NOTARIZE     "true" to notarize (requires FLOWERY_NOTARY_PROFILE)
# ============================================================

DEV_ID_CERT="${FLOWERY_DEV_ID_CERT:-Developer ID Application: Stefano Del Furia (VZWMBQL256)}"

# Notarization is disabled by default.
NOTARIZE="${FLOWERY_NOTARIZE:-false}"
NOTARY_PROFILE="${FLOWERY_NOTARY_PROFILE:-flowery-gallery-notary}"

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
PROJECT="$PROJECT_ROOT/Flowery.Uno.Gallery/Flowery.Uno.Gallery.csproj"
TFM=net10.0-desktop
APP_NAME="Flowery.Uno Gallery"
EXE=Flowery.Uno.Gallery
OUT_DIR="$SCRIPT_DIR/output/installer"
PACKAGING_DIR="$SCRIPT_DIR/packaging"
ENTITLEMENTS="$PACKAGING_DIR/macos/Entitlements.plist"
FINAL_DMG="$OUT_DIR/FloweryUnoGallery.dmg"

# /tmp instead of a cloud-synced folder: OneDrive re-adds its own xattrs after xattr -cr,
# which breaks the codesign verification
WORK_DIR="/tmp/FloweryGallery-dmg-build"
APP="$WORK_DIR/$APP_NAME.app"
DMG_STAGING="$WORK_DIR/dmg-staging"

is_macho() { file -b "$1" | grep -q "Mach-O"; }

echo "========================================"
echo " Flowery.Uno Gallery — DMG (distribuzione diretta)"
echo "========================================"

# --- Check prerequisites ---
if ! command -v create-dmg &>/dev/null; then
    echo "[ERRORE] create-dmg non trovato. Installa con:"
    echo "  brew install create-dmg"
    exit 1
fi

if security find-identity -v -p codesigning | grep -q "$DEV_ID_CERT"; then
    SIGN_ID="$DEV_ID_CERT"
    SIGN_OPTS=(--options runtime --timestamp)
    SIGNED=true
else
    echo "[AVVISO] Certificato Developer ID non trovato nel Keychain: $DEV_ID_CERT"
    echo "         Firma ad-hoc: il DMG non sarà verificato da Gatekeeper."
    SIGN_ID="-"
    SIGN_OPTS=(--options runtime --timestamp=none)
    SIGNED=false
    NOTARIZE=false
fi

# --- Cleanup ---
echo ""
echo "[1/6] Pulizia output precedente..."
rm -r "$WORK_DIR" 2>/dev/null || true
mkdir -p "$WORK_DIR" "$OUT_DIR"

dotnet restore "$PROJECT" -p:TargetFrameworks=$TFM -nologo -v quiet
VERSION="$(dotnet msbuild "$PROJECT" -nologo -getProperty:Version -p:TargetFramework=$TFM | tr -d '[:space:]')"
echo "Versione rilevata: $VERSION"

# --- Build ---
echo ""
echo "[2/6] dotnet publish (Release, $TFM, osx-arm64 + osx-x64)..."
for rid in osx-arm64 osx-x64; do
    dotnet publish "$PROJECT" -f $TFM -p:TargetFrameworks=$TFM -c Release -r "$rid" --self-contained \
        -p:UseMonoRuntime=false -p:DebugType=None -p:DebugSymbols=false -o "$WORK_DIR/$rid" -nologo
done

# --- .app bundle with two payloads (arm64 + x64) ---
echo ""
echo "[3/6] Creazione .app (payload arm64 + x64, launcher che sceglie)..."
# No lipo: the framework managed DLLs (System.Private.CoreLib, ...) are ReadyToRun and
# architecture-specific, so a universal apphost with arm64 DLLs fails under
# Rosetta ("Failed to load System.Private.CoreLib.dll ... 0x8007000B").
# Payload in Contents/Resources: satellite folders (zh-Hans, ...) inside Contents/MacOS
# make codesign fail ("code object is not signed at all").
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
ditto "$WORK_DIR/osx-arm64" "$APP/Contents/Resources/app-arm64"
ditto "$WORK_DIR/osx-x64" "$APP/Contents/Resources/app-x64"
cat > "$APP/Contents/MacOS/$EXE" <<'LAUNCHER'
#!/bin/sh
# Pick the payload from the real hardware (hw.optional.arm64 stays 1 even under Rosetta).
DIR="$(cd "$(dirname "$0")/../Resources" && pwd)"
if [ "$(sysctl -n hw.optional.arm64 2>/dev/null)" = "1" ]; then
    exec /usr/bin/arch -arm64 "$DIR/app-arm64/Flowery.Uno.Gallery" "$@"
fi
exec "$DIR/app-x64/Flowery.Uno.Gallery" "$@"
LAUNCHER
chmod +x "$APP/Contents/MacOS/$EXE"

sed "s/@VERSION@/$VERSION/g" "$PACKAGING_DIR/macos/Info.plist.in" > "$APP/Contents/Info.plist"

# Icon: PNGs rendered from the Gallery SVG -> .icns
ICONSET="$WORK_DIR/AppIcon.iconset"
mkdir -p "$ICONSET"
for size in 16 32 128 256 512; do
    "$PACKAGING_DIR/render-icon.sh" "$size" "$ICONSET/icon_${size}x${size}.png"
    "$PACKAGING_DIR/render-icon.sh" "$((size * 2))" "$ICONSET/icon_${size}x${size}@2x.png"
done
iconutil -c icns "$ICONSET" -o "$APP/Contents/Resources/AppIcon.icns"

# --- Sign the .app ---
echo ""
echo "[4/6] Firma .app..."
xattr -cr "$APP"

# First all the inner Mach-O files (dylib, so, executables), then the bundle.
find "$APP/Contents" -type f | sort | while read -r f; do
    if is_macho "$f"; then
        codesign --sign "$SIGN_ID" --force "${SIGN_OPTS[@]}" \
            --entitlements "$ENTITLEMENTS" "$f"
    fi
done

codesign \
    --sign "$SIGN_ID" \
    --force \
    "${SIGN_OPTS[@]}" \
    --entitlements "$ENTITLEMENTS" \
    "$APP"

echo "Verifica firma..."
codesign --verify --deep --strict "$APP"
spctl --assess --type execute "$APP" 2>&1 || true

# --- Create the DMG ---
echo ""
echo "[5/6] Creazione DMG con create-dmg..."
rm -f "$FINAL_DMG"

# Detach any temporary dmg volumes left over from earlier failed runs
for stale_vol in /Volumes/dmg.*; do
    [ -d "$stale_vol" ] && hdiutil detach "$stale_vol" -force 2>/dev/null || true
done
rm -f "$OUT_DIR"/rw.*.FloweryUnoGallery.dmg 2>/dev/null || true

mkdir -p "$DMG_STAGING"
# ditto instead of "cp -r": preserves symlinks/xattrs without breaking the signature.
ditto "$APP" "$DMG_STAGING/$APP_NAME.app"

create-dmg \
    --volname "$APP_NAME" \
    --window-size 600 420 \
    --icon-size 128 \
    --icon "$APP_NAME.app" 160 200 \
    --app-drop-link 440 200 \
    --no-internet-enable \
    "$FINAL_DMG" \
    "$DMG_STAGING"

if [ "$SIGNED" = "true" ]; then
    codesign --sign "$DEV_ID_CERT" --timestamp "$FINAL_DMG"
    echo "DMG firmato."
else
    echo "DMG non firmato (solo .app con firma ad-hoc)."
fi

# --- Notarization ---
if [ "$NOTARIZE" = "true" ]; then
    echo ""
    echo "[6/6] Notarizzazione DMG (può richiedere qualche minuto)..."
    xcrun notarytool submit "$FINAL_DMG" --keychain-profile "$NOTARY_PROFILE" --wait
    xcrun stapler staple "$FINAL_DMG"
    echo "Notarizzazione completata."
else
    echo "[6/6] Notarizzazione saltata (NOTARIZE=false)."
fi

# --- Final output ---
echo ""
echo "========================================"
echo " DMG pronto:"
echo " $FINAL_DMG"
echo "========================================"
