#!/bin/bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")" && pwd)"
PACKAGE_ROOT="$ROOT_DIR/artifacts/release/AntiAirWeapon"
ASSEMBLY_SOURCE="$ROOT_DIR/dist/Assemblies/AntiAirWeapon.dll"
PDB_SOURCE="$ROOT_DIR/dist/Assemblies/AntiAirWeapon.pdb"
VERSIONED_ROOT="$PACKAGE_ROOT/1.6"
ZIP_PATH="$ROOT_DIR/artifacts/release/AntiAirWeapon.zip"
STEAM_TEST_ROOT="$ROOT_DIR/2802147499"

"$ROOT_DIR/build-local.sh" Release

rm -rf "$PACKAGE_ROOT"
mkdir -p "$PACKAGE_ROOT" "$VERSIONED_ROOT/Assemblies"

cp -R "$ROOT_DIR/About" "$PACKAGE_ROOT/About"
cp -R "$ROOT_DIR/Languages" "$PACKAGE_ROOT/Languages"
cp -R "$ROOT_DIR/Sounds" "$PACKAGE_ROOT/Sounds"
cp -R "$ROOT_DIR/Textures" "$PACKAGE_ROOT/Textures"
cp -R "$ROOT_DIR/1.6/Defs" "$VERSIONED_ROOT/Defs"
cp "$ASSEMBLY_SOURCE" "$VERSIONED_ROOT/Assemblies/AntiAirWeapon.dll"

if [[ -f "$PDB_SOURCE" ]]; then
  cp "$PDB_SOURCE" "$VERSIONED_ROOT/Assemblies/AntiAirWeapon.pdb"
fi

cp "$ROOT_DIR/README.md" "$PACKAGE_ROOT/README.md"
cp "$ROOT_DIR/LICENSE" "$PACKAGE_ROOT/LICENSE"
cp "$ROOT_DIR/LICENSE.zh-CN.md" "$PACKAGE_ROOT/LICENSE.zh-CN.md"

mkdir -p "$STEAM_TEST_ROOT"
rm -rf \
  "$STEAM_TEST_ROOT/1.6" \
  "$STEAM_TEST_ROOT/About" \
  "$STEAM_TEST_ROOT/Languages" \
  "$STEAM_TEST_ROOT/Sounds" \
  "$STEAM_TEST_ROOT/Textures" \
  "$STEAM_TEST_ROOT/LICENSE" \
  "$STEAM_TEST_ROOT/LICENSE.zh-CN.md"
cp -R "$PACKAGE_ROOT/1.6" "$STEAM_TEST_ROOT/1.6"
cp -R "$PACKAGE_ROOT/About" "$STEAM_TEST_ROOT/About"
cp -R "$PACKAGE_ROOT/Languages" "$STEAM_TEST_ROOT/Languages"
cp -R "$PACKAGE_ROOT/Sounds" "$STEAM_TEST_ROOT/Sounds"
cp -R "$PACKAGE_ROOT/Textures" "$STEAM_TEST_ROOT/Textures"
cp "$PACKAGE_ROOT/LICENSE" "$STEAM_TEST_ROOT/LICENSE"
cp "$PACKAGE_ROOT/LICENSE.zh-CN.md" "$STEAM_TEST_ROOT/LICENSE.zh-CN.md"

rm -f "$ZIP_PATH"
if command -v zip >/dev/null 2>&1; then
  (
    cd "$ROOT_DIR/artifacts/release"
    zip -rq "AntiAirWeapon.zip" "AntiAirWeapon"
  )
else
  echo "zip command not found. Release folder prepared at $PACKAGE_ROOT"
  exit 0
fi

echo "Release prepared:"
echo "  Folder: $PACKAGE_ROOT"
echo "  Archive: $ZIP_PATH"
echo "  Steam test folder: $STEAM_TEST_ROOT"
