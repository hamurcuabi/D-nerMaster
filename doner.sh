#!/bin/bash
# Doner Master — tek komutla Android release.
#
#   bash doner.sh            → sürümü otomatik artırır (v0.1.0 → v0.1.1 ...)
#   bash doner.sh 0.2.0      → belirli sürüm
#
# Yaptıkları: GitHub girişi → repo indir/güncelle → Unity + Android modülü kontrolü
# → imza anahtarı (yoksa oluşturur, şifreyi macOS Anahtar Zinciri'ne kaydeder)
# → APK build → GitHub release → telefon bağlıysa kurar.
set -euo pipefail

REPO="hamurcuabi/D-nerMaster"
KEYSTORE="${DONER_KEYSTORE:-$HOME/keys/doner-release.keystore}"
KEY_ALIAS="doner"
KEYCHAIN_SERVICE="doner-master-keystore"

say()  { printf "\n\033[1;33m▶ %s\033[0m\n" "$*"; }
ok()   { printf "  \033[1;32m✓\033[0m %s\n" "$*"; }
die()  { printf "\n\033[1;31m✗ %s\033[0m\n" "$*"; exit 1; }

# ------------------------------------------------------------------ 1. GitHub
say "GitHub"
if ! command -v gh >/dev/null; then
  command -v brew >/dev/null || die "GitHub CLI yok ve Homebrew da yok. https://brew.sh kur, sonra tekrar çalıştır."
  brew install gh
fi
if ! gh auth status >/dev/null 2>&1; then
  gh auth login
fi
ok "giriş yapıldı ($(gh api user -q .login))"

# ------------------------------------------------------------------ 2. Repo
say "Proje"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
if git -C "$SCRIPT_DIR" remote get-url origin 2>/dev/null | grep -qi "d-nermaster"; then
  DIR="$(git -C "$SCRIPT_DIR" rev-parse --show-toplevel)"
else
  DIR="${DONER_DIR:-$HOME/Projects/DonerMaster}"
fi
if [ -d "$DIR/.git" ]; then
  git -C "$DIR" pull --rebase --autostash --quiet || die "git pull başarısız ($DIR). Çakışmayı çözüp tekrar dene."
  ok "güncellendi: $DIR"
else
  mkdir -p "$(dirname "$DIR")"
  gh repo clone "$REPO" "$DIR" -- --quiet
  ok "indirildi: $DIR"
fi
cd "$DIR"

# ------------------------------------------------------------------ 3. Unity + Android
say "Unity"
HUB="/Applications/Unity Hub.app/Contents/MacOS/Unity Hub"
hub() { "$HUB" -- --headless "$@" 2>/dev/null; }

# Prints the folder that contains Unity.app for the newest installed Unity 6, or nothing.
find_unity() {
  local d
  d="$(ls -d /Applications/Unity/Hub/Editor/6000.* 2>/dev/null | sort -V | tail -1 || true)"
  if [ -z "$d" ] && [ -x "$HUB" ]; then
    # Hub may install editors to a custom folder — ask it
    d="$(hub editors -i | grep -Eo '/[^,]*/6000\.[^/,]*/Unity\.app' | sed 's|/Unity\.app$||' | sort -V | tail -1 || true)"
  fi
  echo "$d"
}

if [ -n "${UNITY_PATH:-}" ]; then
  EDITOR_DIR="$(cd "$(dirname "$UNITY_PATH")/../../.." && pwd)"
else
  EDITOR_DIR="$(find_unity)"
fi

FRESH_INSTALL=0
if [ -z "$EDITOR_DIR" ]; then
  if [ ! -x "$HUB" ]; then
    say "Unity Hub kuruluyor"
    command -v brew >/dev/null || die "Homebrew yok. https://brew.sh kur ya da Unity Hub'ı unity.com'dan indir."
    brew install --cask unity-hub
  fi
  say "Unity 6 + Android modülü kuruluyor (~10 GB indirme, 15-40 dk sürebilir)"
  RELEASES="$(hub editors -r || true)"
  VER="$(echo "$RELEASES" | grep -Eo '6000\.0\.[0-9]+f[0-9]+' | sort -V | tail -1 || true)"   # Unity 6 LTS
  [ -n "$VER" ] || VER="$(echo "$RELEASES" | grep -Eo '6000\.[0-9]+\.[0-9]+f[0-9]+' | sort -V | tail -1 || true)"
  [ -n "$VER" ] || die "Hub'dan Unity 6 sürüm listesi alınamadı. Unity Hub'ı bir kere açıp giriş yap, sonra tekrar çalıştır."
  echo "  Sürüm: $VER"
  ARCH="x86_64"
  [ "$(uname -m)" = "arm64" ] && ARCH="arm64"
  "$HUB" -- --headless install --version "$VER" --architecture "$ARCH" -m android android-sdk-ndk-tools android-open-jdk --childModules \
    || die "Unity kurulumu başarısız. Unity Hub'ı açıp Installs → Install Editor → $VER (+ Android Build Support) ile kur."
  EDITOR_DIR="$(find_unity)"
  [ -n "$EDITOR_DIR" ] || die "Kurulum bitti ama Unity bulunamadı. Unity Hub → Installs listesini kontrol et."
  FRESH_INSTALL=1
fi

UNITY="${UNITY_PATH:-$EDITOR_DIR/Unity.app/Contents/MacOS/Unity}"
[ -x "$UNITY" ] || die "Unity çalıştırılabilir dosyası yok: $UNITY"
UNITY_VERSION="$(basename "$EDITOR_DIR")"
ok "Unity $UNITY_VERSION"

ANDROID_DIR="$EDITOR_DIR/PlaybackEngines/AndroidPlayer"
if [ ! -d "$ANDROID_DIR/OpenJDK" ] || [ ! -d "$ANDROID_DIR/SDK" ]; then
  say "Android modülü eksik — kuruluyor (birkaç GB)"
  [ -x "$HUB" ] || die "Unity Hub bulunamadı. Hub → Installs → $UNITY_VERSION → Add modules → Android Build Support (+ OpenJDK, SDK & NDK)."
  "$HUB" -- --headless install-modules --version "$UNITY_VERSION" -m android android-sdk-ndk-tools android-open-jdk --childModules \
    || die "Modül kurulamadı. Hub'dan elle ekle: Installs → $UNITY_VERSION → Add modules → Android Build Support."
  [ -d "$ANDROID_DIR/OpenJDK" ] || die "Android modülü hâlâ görünmüyor. Hub'dan elle ekleyip tekrar çalıştır."
fi
ok "Android Build Support hazır"

# Google Play requires targetSdk 36 — make sure that SDK platform is installed
if [ ! -d "$ANDROID_DIR/SDK/platforms/android-36" ]; then
  SDKM="$(ls "$ANDROID_DIR"/SDK/cmdline-tools/*/bin/sdkmanager 2>/dev/null | sort -V | tail -1 || true)"
  if [ -n "$SDKM" ]; then
    say "Android SDK 36 (Play zorunluluğu) kuruluyor"
    export JAVA_HOME="$ANDROID_DIR/OpenJDK"
    yes | "$SDKM" --licenses >/dev/null 2>&1 || true
    "$SDKM" "platforms;android-36" "build-tools;36.0.0" >/dev/null || die "Android SDK 36 kurulamadı ($SDKM)."
  fi
fi
ok "Android SDK hazır"

# Unity needs a (free) Personal license before it can build, even from the terminal.
if [ "$FRESH_INSTALL" = "1" ]; then
  say "Unity lisansı"
  echo "  Unity Hub açılıyor: giriş yap → Preferences (⚙) → Licenses → Add → 'Get a free personal license'."
  open -a "Unity Hub" 2>/dev/null || true
  read -r -p "  Lisansı aldıktan sonra Enter'a bas... " _
fi

# Project must not be open in the editor
if pgrep -f "Unity.app/Contents/MacOS/Unity.*-projectpath.*$(basename "$DIR")" >/dev/null 2>&1 \
   || pgrep -f "Unity.app/Contents/MacOS/Unity.*-projectPath.*$(basename "$DIR")" >/dev/null 2>&1; then
  die "Proje Unity editöründe açık. Kapatıp tekrar çalıştır."
fi

# ------------------------------------------------------------------ 4. Signing key
say "İmza anahtarı"
KEYTOOL="$ANDROID_DIR/OpenJDK/bin/keytool"
PASS="$(security find-generic-password -a "$USER" -s "$KEYCHAIN_SERVICE" -w 2>/dev/null || true)"
if [ ! -f "$KEYSTORE" ]; then
  echo "  Release anahtarı yok, oluşturuluyor: $KEYSTORE"
  echo "  (Bu dosya ve şifresi Play'deki tüm güncellemeler için gerekli — yedekle!)"
  while true; do
    read -r -s -p "  Yeni anahtar şifresi (en az 6 karakter): " P1; echo
    read -r -s -p "  Tekrar: " P2; echo
    [ "$P1" = "$P2" ] && [ ${#P1} -ge 6 ] && break
    echo "  Şifreler uyuşmuyor ya da çok kısa, tekrar dene."
  done
  PASS="$P1"
  mkdir -p "$(dirname "$KEYSTORE")"
  "$KEYTOOL" -genkeypair -keystore "$KEYSTORE" -alias "$KEY_ALIAS" -keyalg RSA -keysize 2048 -validity 10000 \
    -storepass "$PASS" -keypass "$PASS" -dname "CN=Doner Master, O=Hamurcu Games, C=TR" >/dev/null
  chmod 600 "$KEYSTORE"
  security add-generic-password -U -a "$USER" -s "$KEYCHAIN_SERVICE" -w "$PASS"
  ok "anahtar oluşturuldu, şifre Anahtar Zinciri'ne kaydedildi"
else
  if [ -z "$PASS" ]; then
    read -r -s -p "  Keystore şifresi: " PASS; echo
    "$KEYTOOL" -list -keystore "$KEYSTORE" -storepass "$PASS" >/dev/null 2>&1 || die "Şifre yanlış."
    security add-generic-password -U -a "$USER" -s "$KEYCHAIN_SERVICE" -w "$PASS"
    ok "şifre Anahtar Zinciri'ne kaydedildi (bir daha sormayacak)"
  fi
  ok "$KEYSTORE"
fi

# ------------------------------------------------------------------ 5. Version
say "Sürüm"
if [ $# -ge 1 ]; then
  VERSION="${1#v}"
else
  LAST="$(gh release list -R "$REPO" --limit 1 --json tagName -q '.[0].tagName' 2>/dev/null || true)"
  if [ -z "$LAST" ]; then
    VERSION="0.1.0"
  else
    IFS=. read -r MA MI PA <<< "${LAST#v}"
    VERSION="$MA.$MI.$((PA + 1))"
  fi
fi
[[ "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || die "Sürüm formatı x.y.z olmalı (verilen: $VERSION)"
IFS=. read -r MA MI PA <<< "$VERSION"
VERSION_CODE=$((MA * 10000 + MI * 100 + PA))
ok "v$VERSION (versionCode $VERSION_CODE)"

# ------------------------------------------------------------------ 6. Build + release
say "Build + GitHub release"
export UNITY_PATH="$UNITY"
export DONER_KEYSTORE="$KEYSTORE"
export DONER_KEYSTORE_PASS="$PASS"
export DONER_KEY_ALIAS="$KEY_ALIAS"
export DONER_VERSION="$VERSION"
export DONER_VERSION_CODE="$VERSION_CODE"
export DONER_RELEASE=1
bash ./Tools/build-android.sh

APK="Builds/Android/DonerMaster-$VERSION.apk"

# ------------------------------------------------------------------ 7. Install on a connected phone
ADB="$ANDROID_DIR/SDK/platform-tools/adb"
if [ -x "$ADB" ] && [ -f "$APK" ] && "$ADB" devices | grep -q "device$"; then
  say "Telefona kuruluyor"
  "$ADB" install -r "$APK" && ok "kuruldu"
fi

URL="$(gh release view "v$VERSION" -R "$REPO" --json url -q .url 2>/dev/null || true)"
say "Bitti 🎉"
echo "  APK:     $DIR/$APK"
[ -n "$URL" ] && echo "  Release: $URL" && open "$URL" 2>/dev/null || true
