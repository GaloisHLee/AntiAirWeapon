#!/bin/bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
PROJECT_FILE="$ROOT_DIR/AntiAirWeapon.csproj"
CONFIGURATION="${1:-Debug}"

find_rimworld_managed() {
  if [[ -n "${RIMWORLD_MANAGED_DIR:-}" && -d "${RIMWORLD_MANAGED_DIR}" ]]; then
    printf '%s\n' "${RIMWORLD_MANAGED_DIR}"
    return 0
  fi

  local candidates=(
    "F:/SteamLibrary/steamapps/common/RimWorld/RimWorldWin64_Data/Managed"
    "$HOME/Library/Application Support/Steam/steamapps/common/RimWorld/RimWorldMac.app/Contents/Resources/Data/Managed"
    "$ROOT_DIR/References/RimWorld/Managed"
  )

  local dir
  for dir in "${candidates[@]}"; do
    if [[ -d "$dir" && -f "$dir/Assembly-CSharp.dll" ]]; then
      printf '%s\n' "$dir"
      return 0
    fi
  done

  return 1
}

find_hugslib_dir() {
  if [[ -n "${HUGSLIB_DIR:-}" && -d "${HUGSLIB_DIR}" ]]; then
    printf '%s\n' "${HUGSLIB_DIR}"
    return 0
  fi

  local candidates=(
    "F:/SteamLibrary/steamapps/workshop/content/294100/818773962/v1.6/Assemblies"
    "$HOME/Library/Application Support/Steam/steamapps/workshop/content/294100/818773962/v1.6/Assemblies"
    "$HOME/Library/Application Support/Steam/steamapps/workshop/content/294100/818773962/Assemblies"
    "$ROOT_DIR/References/HugsLib"
  )

  local dir
  for dir in "${candidates[@]}"; do
    if [[ -d "$dir" && -f "$dir/HugsLib.dll" ]]; then
      printf '%s\n' "$dir"
      return 0
    fi
  done

  return 1
}

find_harmony_dir() {
  if [[ -n "${HARMONY_DIR:-}" && -d "${HARMONY_DIR}" ]]; then
    printf '%s\n' "${HARMONY_DIR}"
    return 0
  fi

  local candidates=(
    "F:/SteamLibrary/steamapps/workshop/content/294100/2009463077/Current/Assemblies"
    "$HOME/Library/Application Support/Steam/steamapps/workshop/content/294100/2009463077/Current/Assemblies"
    "$HOME/Library/Application Support/Steam/steamapps/workshop/content/294100/2009463077/1.6/Assemblies"
    "$ROOT_DIR/References/Harmony"
  )

  local dir
  for dir in "${candidates[@]}"; do
    if [[ -d "$dir" && -f "$dir/0Harmony.dll" ]]; then
      printf '%s\n' "$dir"
      return 0
    fi
  done

  return 1
}

find_build_tool() {
  local candidates=(
    "${MSBUILD_PATH:-}"
    "$(command -v msbuild || true)"
    "$(command -v xbuild || true)"
    "$(command -v mono-msbuild || true)"
  )

  local tool
  for tool in "${candidates[@]}"; do
    if [[ -n "$tool" && -x "$tool" ]]; then
      printf '%s\n' "$tool"
      return 0
    fi
  done

  return 1
}

find_netstandard_facade() {
  if [[ -n "${NETSTANDARD_FACADE:-}" && -f "${NETSTANDARD_FACADE}" ]]; then
    printf '%s\n' "${NETSTANDARD_FACADE}"
    return 0
  fi

  local candidates=(
    "/opt/homebrew/Cellar/mono"
    "/usr/local/Cellar/mono"
    "/Library/Frameworks/Mono.framework"
  )

  local root
  for root in "${candidates[@]}"; do
    if [[ -d "$root" ]]; then
      local facade
      facade="$(find "$root" -path '*4.7.2-api/Facades/netstandard.dll' 2>/dev/null | head -n 1)"
      if [[ -n "$facade" && -f "$facade" ]]; then
        printf '%s\n' "$facade"
        return 0
      fi
    fi
  done

  return 1
}

RIMWORLD_MANAGED="$(find_rimworld_managed || true)"
HUGSLIB_DIR="$(find_hugslib_dir || true)"
HARMONY_DIR="$(find_harmony_dir || true)"
BUILD_TOOL="$(find_build_tool || true)"
NETSTANDARD_FACADE="$(find_netstandard_facade || true)"

if [[ -z "$BUILD_TOOL" ]]; then
  echo "Missing build tool. Install Mono/MSBuild, or set MSBUILD_PATH."
  exit 1
fi

if [[ -z "$RIMWORLD_MANAGED" ]]; then
  echo "Missing RimWorld managed assemblies. Set RIMWORLD_MANAGED_DIR or copy files into References/RimWorld/Managed."
  exit 1
fi

if [[ -z "$HUGSLIB_DIR" ]]; then
  echo "Missing HugsLib assemblies. Set HUGSLIB_DIR or copy HugsLib.dll into References/HugsLib."
  exit 1
fi

if [[ -z "$HARMONY_DIR" ]]; then
  echo "Missing Harmony assemblies. Set HARMONY_DIR or copy 0Harmony.dll from the Harmony mod into References/Harmony."
  exit 1
fi

if [[ -z "$NETSTANDARD_FACADE" ]]; then
  echo "Missing netstandard facade. Set NETSTANDARD_FACADE to your mono facade path."
  exit 1
fi

echo "Using build tool: $BUILD_TOOL"
echo "Using RimWorld assemblies: $RIMWORLD_MANAGED"
echo "Using HugsLib assemblies: $HUGSLIB_DIR"
echo "Using Harmony assemblies: $HARMONY_DIR"
echo "Using netstandard facade: $NETSTANDARD_FACADE"

"$BUILD_TOOL" "$PROJECT_FILE" \
  /p:Configuration="$CONFIGURATION" \
  /p:RimWorldManagedDir="$RIMWORLD_MANAGED" \
  /p:HugsLibDir="$HUGSLIB_DIR" \
  /p:HarmonyDir="$HARMONY_DIR" \
  /p:NetStandardFacade="$NETSTANDARD_FACADE"
