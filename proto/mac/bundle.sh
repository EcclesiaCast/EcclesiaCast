#!/bin/bash
# Builds "EcclesiaCast Proto.app" from a self-contained osx-arm64 publish,
# signs it locally (no Apple account: ad-hoc signature) and zips it with
# ditto, which keeps the execute permissions and the signatures.
#
#   proto/mac/bundle.sh <publish folder> <output folder>
set -euo pipefail

PUBLISH="$1"
OUT="$2"
HERE="$(cd "$(dirname "$0")" && pwd)"
APP="$OUT/EcclesiaCast Proto.app"
ZIP="$OUT/EcclesiaCast-Proto-mac.zip"

rm -rf "$APP" "$ZIP"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"

# The program and everything .NET publishes go next to the executable.
cp -R "$PUBLISH/." "$APP/Contents/MacOS/"

# The Bible is data, not code: it goes to Resources (ProtoData looks there).
mv "$APP/Contents/MacOS/bibles" "$APP/Contents/Resources/bibles"

cp "$HERE/Info.plist" "$APP/Contents/Info.plist"
chmod +x "$APP/Contents/MacOS/EcclesiaCast.Proto"

echo "== Arquitectura de las piezas nativas"
file "$APP/Contents/MacOS/EcclesiaCast.Proto"
file "$APP/Contents/MacOS/libvlc/osx-arm64/lib/libvlc.dylib"
file "$APP/Contents/MacOS/libvlc/osx-arm64/lib/libvlccore.dylib"
if [ -d "$APP/Contents/MacOS/libvlc/osx-x64" ]; then
  echo "ERROR: se coló la versión Intel de LibVLC" >&2
  exit 1
fi

# Inside out: every Mach-O file first (dylibs, VLC plugins, createdump),
# then the whole bundle. On an Apple chip nothing runs without a signature.
echo "== Firmando las piezas nativas"
count=0
while IFS= read -r -d '' f; do
  if file -b "$f" | grep -q 'Mach-O'; then
    codesign --force --sign - "$f"
    count=$((count + 1))
  fi
done < <(find "$APP/Contents/MacOS" -type f ! -name 'EcclesiaCast.Proto' -print0)
echo "Firmadas: $count"

echo "== Firmando el paquete"
codesign --force --deep --sign - "$APP"

echo "== Comprimiendo"
ditto -c -k --keepParent "$APP" "$ZIP"

du -sh "$APP"
ls -lh "$ZIP"
