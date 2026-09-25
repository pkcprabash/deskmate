#!/usr/bin/env bash
# Builds, signs, notarizes and packs the macOS installer with Velopack.
#
# Usage: build/pack-macos.sh [version] [rid]     (defaults: 1.0.0, osx-arm64)
#
# Signing + notarization run only when these are set (otherwise you get an unsigned local build):
#   APPLE_APP_IDENTITY       e.g. "Developer ID Application: Your Name (TEAMID)"
#   APPLE_INSTALL_IDENTITY   e.g. "Developer ID Installer: Your Name (TEAMID)"
#   APPLE_NOTARY_PROFILE     keychain profile created with `xcrun notarytool store-credentials`
#
# Requires: dotnet tool install -g vpk
set -euo pipefail

VERSION="${1:-1.0.0}"
RID="${2:-osx-arm64}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PUBLISH_DIR="$ROOT/artifacts/publish/$RID"
RELEASE_DIR="$ROOT/artifacts/releases/$RID"

rm -rf "$PUBLISH_DIR"
dotnet publish "$ROOT/src/Deskmate.App/Deskmate.App.csproj" \
  -c Release -r "$RID" --self-contained -p:Version="$VERSION" -o "$PUBLISH_DIR"

ARGS=(
  --packId Deskmate
  --packTitle Deskmate
  --packVersion "$VERSION"
  --packDir "$PUBLISH_DIR"
  --mainExe Deskmate.App
  --outputDir "$RELEASE_DIR"
)

if [[ -f "$ROOT/build/deskmate.icns" ]]; then
  ARGS+=(--icon "$ROOT/build/deskmate.icns")
fi

if [[ -n "${APPLE_APP_IDENTITY:-}" ]]; then
  ARGS+=(--signAppIdentity "$APPLE_APP_IDENTITY")
  [[ -n "${APPLE_INSTALL_IDENTITY:-}" ]] && ARGS+=(--signInstallIdentity "$APPLE_INSTALL_IDENTITY")
  [[ -n "${APPLE_NOTARY_PROFILE:-}" ]] && ARGS+=(--notaryProfile "$APPLE_NOTARY_PROFILE")
else
  echo "APPLE_APP_IDENTITY not set: building an UNSIGNED, un-notarized package."
fi

vpk pack "${ARGS[@]}"
echo "Done. Output in $RELEASE_DIR"
