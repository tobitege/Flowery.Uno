#!/usr/bin/env bash
set -e

# ============================================================
# Flowery.Uno Gallery — Android APK (arm64 + x64), signed Release
#
# Extra arguments (e.g. in CI: -p:InstallAndroidDependencies=true) are passed to dotnet publish.
#
# Signing: by default Uno/.NET Android uses the debug keystore (fine for a demo).
# To use your own keystore, export these first:
#   FLOWERY_ANDROID_KEYSTORE=/path/file.jks
#   FLOWERY_ANDROID_KEY_ALIAS=alias
#   FLOWERY_ANDROID_KEYSTORE_PASS=password
# ============================================================

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
OUT_DIR="$SCRIPT_DIR/output/installer"
PROJECT="$PROJECT_ROOT/Flowery.Uno.Gallery/Flowery.Uno.Gallery.csproj"
# Outside any cloud-synced folder (e.g. OneDrive): avoids syncing hundreds of intermediate files
PUBLISH_DIR="/tmp/FloweryGallery-android-build"

SIGN_ARGS=()
if [ -n "${FLOWERY_ANDROID_KEYSTORE:-}" ]; then
    SIGN_ARGS=(
        -p:AndroidKeyStore=true
        -p:AndroidSigningKeyStore="$FLOWERY_ANDROID_KEYSTORE"
        -p:AndroidSigningKeyAlias="$FLOWERY_ANDROID_KEY_ALIAS"
        -p:AndroidSigningKeyPass="env:FLOWERY_ANDROID_KEYSTORE_PASS"
        -p:AndroidSigningStorePass="env:FLOWERY_ANDROID_KEYSTORE_PASS"
    )
fi

echo "Pulizia output precedente..."
mkdir -p "$OUT_DIR"
rm -f "$OUT_DIR/FloweryUnoGallery.apk"
rm -r "$PUBLISH_DIR" 2>/dev/null || true

dotnet publish "$PROJECT" -f net10.0-android36.0 -c Release \
    -p:AndroidPackageFormat=apk ${SIGN_ARGS[@]+"${SIGN_ARGS[@]}"} -o "$PUBLISH_DIR" "$@"

APK="$(find "$PUBLISH_DIR" -maxdepth 1 -name '*-Signed.apk' | head -n 1)"
[ -n "$APK" ] || { echo "[ERRORE] APK firmato non trovato in $PUBLISH_DIR"; exit 1; }
cp "$APK" "$OUT_DIR/FloweryUnoGallery.apk"
ls -lh "$OUT_DIR/FloweryUnoGallery.apk"
