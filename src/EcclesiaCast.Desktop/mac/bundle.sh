#!/bin/bash
# Builds "EcclesiaCast.app" (the test program for the Mac) from a
# self-contained osx-arm64 publish, signs it locally (no Apple account:
# ad-hoc signature) and zips it with ditto, which keeps the execute
# permissions and the signatures. From the prototype of batch 1.
#
#   src/EcclesiaCast.Desktop/mac/bundle.sh <publish folder> <output folder> <version>
set -euo pipefail

PUBLISH="$1"
OUT="$2"
VERSION="${3:-0.0.0}"
HERE="$(cd "$(dirname "$0")" && pwd)"
APP="$OUT/EcclesiaCast.app"
ZIP="$OUT/EcclesiaCast-mac-prueba.zip"
EXE="EcclesiaCast.Desktop"

rm -rf "$APP" "$ZIP"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"

# The program and everything .NET publishes go next to the executable.
cp -R "$PUBLISH/." "$APP/Contents/MacOS/"

# The Bibles and the sample backgrounds are data, not code: they go to
# Resources (MacPlatformPaths.BundledFolder looks there).
mv "$APP/Contents/MacOS/bibles" "$APP/Contents/Resources/bibles"
mv "$APP/Contents/MacOS/fondos" "$APP/Contents/Resources/fondos"

# The version of the program, for Finder's «Obtener información». macOS
# wants only numbers and dots there: «1.4.0-mac.12» goes as «1.4.0».
PLAIN="${VERSION%%-*}"
sed -e "s|<string>0.0</string>|<string>${PLAIN}</string>|" \
    -e "s|<string>0.0.0</string>|<string>${PLAIN}</string>|" \
    "$HERE/Info.plist" > "$APP/Contents/Info.plist"
chmod +x "$APP/Contents/MacOS/$EXE"

echo "== Arquitectura de las piezas nativas"
file "$APP/Contents/MacOS/$EXE"
file "$APP/Contents/MacOS/libvlc/osx-arm64/lib/libvlc.dylib"
file "$APP/Contents/MacOS/libvlc/osx-arm64/lib/libvlccore.dylib"
if [ -d "$APP/Contents/MacOS/libvlc/osx-x64" ]; then
  echo "ERROR: se coló la versión Intel de LibVLC" >&2
  exit 1
fi

echo "== Lo que viene con el programa"
ls "$APP/Contents/Resources/bibles" "$APP/Contents/Resources/fondos"
ls "$APP/Contents/MacOS/Assets/Fonts" 2>/dev/null || true

# Inside out: every Mach-O file first (dylibs, VLC plugins, createdump),
# then the whole bundle. On an Apple chip nothing runs without a signature.
echo "== Firmando las piezas nativas"
count=0
while IFS= read -r -d '' f; do
  if file -b "$f" | grep -q 'Mach-O'; then
    codesign --force --sign - "$f"
    count=$((count + 1))
  fi
done < <(find "$APP/Contents/MacOS" -type f ! -name "$EXE" -print0)
echo "Firmadas: $count"

echo "== Firmando el paquete"
codesign --force --deep --sign - "$APP"

echo "== Comprimiendo"
ditto -c -k --keepParent "$APP" "$ZIP"

du -sh "$APP"
ls -lh "$ZIP"
