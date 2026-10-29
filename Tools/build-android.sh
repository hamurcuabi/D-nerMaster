#!/bin/bash
# Release APK (or AAB) from the macOS terminal — no need to open the Unity editor.
#
# One-time setup:
#   1. Unity Hub → Installs → your Unity 6 → Add modules → "Android Build Support"
#      (tick OpenJDK and Android SDK & NDK Tools).
#   2. Create a release keystore (keep it OUTSIDE the repo and back it up — you need
#      the same key for every future update on Google Play):
#        keytool -genkeypair -v -keystore ~/keys/doner-release.keystore \
#          -alias doner -keyalg RSA -keysize 2048 -validity 10000
#
# Build:
#   export DONER_KEYSTORE=~/keys/doner-release.keystore
#   export DONER_KEYSTORE_PASS='...'      # DONER_KEY_PASS defaults to the same
#   ./Tools/build-android.sh              # → Builds/Android/DonerMaster-<version>.apk
#
# Optional: DONER_VERSION=0.2.0 DONER_VERSION_CODE=2 DONER_AAB=1 (Play Store bundle)
#           DONER_BUNDLE_ID=com.you.app  UNITY_PATH=/path/to/Unity
#
# GitHub release: after a successful build the file is published as release v<version>
# (needs the GitHub CLI: `brew install gh && gh auth login`). Skip with DONER_RELEASE=0.
# Building the same version again replaces that release's file.
# Close the project in the Unity editor first — a project can be open in only one Unity.
set -euo pipefail
cd "$(dirname "$0")/.."

export DONER_VERSION="${DONER_VERSION:-0.1.0}"
RELEASE="${DONER_RELEASE:-1}"
EXT=$([ "${DONER_AAB:-0}" = "1" ] && echo aab || echo apk)
OUT="Builds/Android/DonerMaster-$DONER_VERSION.$EXT"
export DONER_OUTPUT="$OUT"

# Check the GitHub side before spending 10 minutes on a build
if [ "$RELEASE" = "1" ]; then
  command -v gh >/dev/null || { echo "GitHub CLI yok: brew install gh && gh auth login  (ya da DONER_RELEASE=0)"; exit 1; }
  gh auth status >/dev/null 2>&1 || { echo "GitHub'a giriş yapılmamış: gh auth login"; exit 1; }
fi

UNITY="${UNITY_PATH:-$(ls -d /Applications/Unity/Hub/Editor/6000.*/Unity.app/Contents/MacOS/Unity 2>/dev/null | sort -V | tail -1 || true)}"
if [ -z "$UNITY" ] || [ ! -x "$UNITY" ]; then
  echo "Unity 6 bulunamadı. UNITY_PATH ile yolunu ver, ör:"
  echo "  export UNITY_PATH=/Applications/Unity/Hub/Editor/6000.0.58f2/Unity.app/Contents/MacOS/Unity"
  exit 1
fi

: "${DONER_KEYSTORE:?DONER_KEYSTORE ayarlı değil (keystore dosyasının yolu)}"
: "${DONER_KEYSTORE_PASS:?DONER_KEYSTORE_PASS ayarlı değil}"
DONER_KEYSTORE="$(cd "$(dirname "$DONER_KEYSTORE")" && pwd)/$(basename "$DONER_KEYSTORE")"
export DONER_KEYSTORE

mkdir -p Builds
LOG="Builds/android-build.log"
echo "Unity: $UNITY"
echo "Building... (log: $LOG — ilk build IL2CPP yüzünden 5-15 dk sürebilir)"

set +e
"$UNITY" -batchmode -quit \
  -projectPath "$PWD" \
  -buildTarget Android \
  -executeMethod DonerMaster.EditorTools.BuildScript.BuildAndroidRelease \
  -logFile "$LOG"
CODE=$?
set -e

grep "\[DonerBuild\]" "$LOG" || true
if [ $CODE -ne 0 ]; then
  echo "Build başarısız (exit $CODE). Hatalar için: grep -i error $LOG"
  exit $CODE
fi
ls -lh "$OUT"

if [ "$RELEASE" = "1" ]; then
  TAG="v$DONER_VERSION"
  COMMIT=$(git rev-parse HEAD)
  if ! git diff --quiet HEAD -- Assets Packages 2>/dev/null; then
    echo "Uyarı: Assets/Packages altında commit edilmemiş değişiklik var; release $COMMIT commit'ine etiketlenecek."
  fi
  if gh release view "$TAG" >/dev/null 2>&1; then
    echo "Release $TAG zaten var — dosya güncelleniyor..."
    gh release upload "$TAG" "$OUT" --clobber
  else
    echo "GitHub release oluşturuluyor: $TAG"
    gh release create "$TAG" "$OUT" \
      --target "$COMMIT" \
      --title "Doner Master $DONER_VERSION" \
      --generate-notes
  fi
  echo "Release: $(gh release view "$TAG" --json url -q .url)"
fi

echo "Telefona kurmak için (USB debugging açık): adb install -r $OUT"
