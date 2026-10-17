#!/usr/bin/env bash
# Builds the Linux .deb of the Flowery.Uno Gallery (net10.0-desktop).
#
#   packaging/linux/build-deb.sh <linux-x64|linux-arm64> <output-dir>
#
# Runs on macOS (brew install dpkg) and on Linux. Output:
# <output-dir>/flowery-uno-gallery_<version>_<amd64|arm64>.deb
# Installs the self-contained app in /opt/flowery-uno-gallery and links /usr/bin/flowery-uno-gallery.
set -euo pipefail

RID="${1:-}"
OUT_DIR="${2:-}"
case "$RID" in
    linux-x64)   ARCH=amd64 ;;
    linux-arm64) ARCH=arm64 ;;
    *) echo "usage: $0 <linux-x64|linux-arm64> <output-dir>" >&2; exit 2 ;;
esac
[ -n "$OUT_DIR" ] || { echo "usage: $0 <linux-x64|linux-arm64> <output-dir>" >&2; exit 2; }

command -v dpkg-deb >/dev/null || { echo "dpkg-deb not found (macOS: brew install dpkg)" >&2; exit 1; }

PACKAGING_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "$PACKAGING_DIR/../../../.." && pwd)"
PROJECT="$PROJECT_ROOT/Flowery.Uno.Gallery/Flowery.Uno.Gallery.csproj"
TFM=net10.0-desktop
NAME=flowery-uno-gallery
EXE=Flowery.Uno.Gallery

dotnet restore "$PROJECT" -p:TargetFrameworks=$TFM -nologo -v quiet
VERSION="$(dotnet msbuild "$PROJECT" -nologo -getProperty:Version -p:TargetFramework=$TFM | tr -d '[:space:]')"
[[ "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || { echo "unexpected version '$VERSION'" >&2; exit 1; }

WORK_DIR="$(mktemp -d "${TMPDIR:-/tmp}/flowery-deb.XXXXXX")"
trap 'rm -r "$WORK_DIR"' EXIT
PUBLISH_DIR="$WORK_DIR/publish"
ROOT="$WORK_DIR/${NAME}_${VERSION}_${ARCH}"
DEB="${NAME}_${VERSION}_${ARCH}.deb"

echo "==> Flowery.Uno Gallery $VERSION, $RID ($ARCH)"

echo "==> dotnet publish"
dotnet publish "$PROJECT" -f $TFM -p:TargetFrameworks=$TFM -c Release -r "$RID" --self-contained \
    -p:UseMonoRuntime=false -p:DebugType=None -p:DebugSymbols=false -o "$PUBLISH_DIR" -nologo -v quiet

echo "==> package tree"
mkdir -p "$ROOT/DEBIAN" "$ROOT/opt" "$ROOT/usr/bin" "$ROOT/usr/share/applications"
cp -R "$PUBLISH_DIR" "$ROOT/opt/$NAME"
find "$ROOT/opt/$NAME" -name '*.a' -delete
# Launcher instead of a symlink: libSkiaSharp.so on linux-arm64 calls uuid_generate_random and
# FT_Get_BDF_Property without linking libuuid and libfreetype, so both are preloaded.
cat > "$ROOT/usr/bin/$NAME" <<LAUNCHER
#!/bin/sh
export LD_PRELOAD="libuuid.so.1 libfreetype.so.6\${LD_PRELOAD:+ \$LD_PRELOAD}"
exec /opt/$NAME/$EXE "\$@"
LAUNCHER

cp "$PACKAGING_DIR/com.uno.flowery.gallery.desktop" "$ROOT/usr/share/applications/"
for size in 48 64 128 256 512; do
    mkdir -p "$ROOT/usr/share/icons/hicolor/${size}x${size}/apps"
    "$PACKAGING_DIR/../render-icon.sh" "$size" \
        "$ROOT/usr/share/icons/hicolor/${size}x${size}/apps/$NAME.png"
done

# Normalized permissions: the files come from a macOS checkout with any umask.
find "$ROOT" -type d -exec chmod 755 {} +
find "$ROOT" -type f -exec chmod 644 {} +
chmod 755 "$ROOT/opt/$NAME/$EXE" "$ROOT/usr/bin/$NAME"

INSTALLED_SIZE="$(du -sk "$ROOT" | cut -f1)"
sed -e "s/@VERSION@/$VERSION/" -e "s/@ARCH@/$ARCH/" -e "s/@INSTALLED_SIZE@/$INSTALLED_SIZE/" \
    "$PACKAGING_DIR/control.in" > "$ROOT/DEBIAN/control"

echo "==> dpkg-deb"
mkdir -p "$OUT_DIR"
dpkg-deb --build --root-owner-group -Zxz "$ROOT" "$OUT_DIR/$DEB"
dpkg-deb --info "$OUT_DIR/$DEB" | sed -n '/Package:/,/Description:/p'
ls -lh "$OUT_DIR/$DEB"
