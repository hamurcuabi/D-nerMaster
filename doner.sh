#!/bin/bash
# Doner Master — tek komutla Android release.
#
#   bash doner.sh            → sürümü otomatik artırır (v0.1.0 → v0.1.1 ...)
#   bash doner.sh 0.2.0      → belirli sürüm
#
# Yaptıkları: GitHub girişi → repo indir/güncelle → Mac'teki Android SDK/NDK/JDK'yı bulur, sadece
# eksiği indirir → Unity + Android Build Support (alt paketsiz)
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

# ------------------------------------------------------------------ 3. Android araçları (Mac'teki mevcut kurulum)
# Önce senin SDK/NDK/JDK'na bakar; sadece eksik olanı indirir.
say "Android araçları"
NDK_VER="${DONER_NDK_VERSION:-27.2.12479018}"     # NDK r27c — Unity 6'nın istediği sürüm

is_jdk17() { [ -x "$1/bin/java" ] && grep -q 'JAVA_VERSION="17' "$1/release" 2>/dev/null; }
find_jdk17() {
  local c
  for c in "${DONER_JDK:-}" "$(/usr/libexec/java_home -v 17 2>/dev/null || true)" \
           "/Applications/Android Studio.app/Contents/jbr/Contents/Home" \
           /Library/Java/JavaVirtualMachines/*17*/Contents/Home \
           /opt/homebrew/opt/openjdk@17/libexec/openjdk.jdk/Contents/Home \
           /usr/local/opt/openjdk@17/libexec/openjdk.jdk/Contents/Home; do
    [ -n "$c" ] && is_jdk17 "$c" && { echo "$c"; return; }
  done
}
find_sdk() {
  local c
  for c in "${DONER_ANDROID_SDK:-}" "${ANDROID_HOME:-}" "${ANDROID_SDK_ROOT:-}" "$HOME/Library/Android/sdk" \
           /opt/homebrew/share/android-commandlinetools /usr/local/share/android-commandlinetools; do
    [ -n "$c" ] && { [ -d "$c/platforms" ] || [ -d "$c/cmdline-tools" ]; } && { echo "$c"; return; }
  done
}
find_sdkmanager() {
  local m
  m="$(ls "$1"/cmdline-tools/*/bin/sdkmanager 2>/dev/null | sort -V | tail -1 || true)"
  [ -z "$m" ] && m="$(command -v sdkmanager || true)"
  echo "$m"
}

JDK="$(find_jdk17)"
if [ -z "$JDK" ]; then
  echo "  JDK 17 yok — kuruluyor (~190 MB)"
  command -v brew >/dev/null || die "JDK 17 bulunamadı ve Homebrew yok. https://adoptium.net'ten JDK 17 kur."
  brew install --cask temurin@17
  JDK="$(find_jdk17)"
  [ -n "$JDK" ] || die "JDK 17 kuruldu ama bulunamadı. DONER_JDK=/yol/jdk17 ile ver."
fi
ok "JDK 17: $JDK"
export JAVA_HOME="$JDK"

SDK="$(find_sdk)"
if [ -z "$SDK" ]; then
  echo "  Android SDK yok — sadece komut satırı araçları kuruluyor (~150 MB)"
  command -v brew >/dev/null || die "Android SDK bulunamadı. Android Studio ile kur ya da DONER_ANDROID_SDK=/yol ver."
  brew install --cask android-commandlinetools
  SDK="$(find_sdk)"
  [ -n "$SDK" ] || die "Android SDK kurulumu bulunamadı."
fi
ok "Android SDK: $SDK"

# Version of an SDK component, read the same way Unity does (source.properties → Pkg.Revision)
pkg_rev() { sed -n 's/^Pkg.Revision=//p' "$1/source.properties" 2>/dev/null | head -1; }
# version_ge A B → true when A >= B (dotted versions)
version_ge() { [ -n "$1" ] && [ "$(printf '%s\n%s\n' "$2" "$1" | sort -V | head -1)" = "$2" ]; }

MISSING=()
[ -d "$SDK/platforms/android-36" ] || MISSING+=("platforms;android-36")
BT="$(ls "$SDK/build-tools" 2>/dev/null | sort -V | tail -1 || true)"
version_ge "$BT" "36.0.0" || MISSING+=("build-tools;36.0.0")
# Unity 6 refuses to build with platform-tools older than 37 (and can't update it in batch mode)
version_ge "$(pkg_rev "$SDK/platform-tools")" "37.0.0" || MISSING+=("platform-tools")
[ -d "$SDK/ndk/$NDK_VER" ] || MISSING+=("ndk;$NDK_VER")
[ -d "$SDK/cmake/3.22.1" ] || MISSING+=("cmake;3.22.1")     # Unity 6 build step requires exactly this

if [ ${#MISSING[@]} -gt 0 ]; then
  SDKM="$(find_sdkmanager "$SDK")"
  [ -n "$SDKM" ] || die "sdkmanager bulunamadı. Android Studio → SDK Manager → SDK Tools → 'Android SDK Command-line Tools' kur."
  echo "  Eksik olanlar indiriliyor: ${MISSING[*]}"
  yes | "$SDKM" --sdk_root="$SDK" --licenses >/dev/null 2>&1 || true
  "$SDKM" --sdk_root="$SDK" "${MISSING[@]}" || die "SDK paketleri kurulamadı: ${MISSING[*]}"
fi
ok "platform 36, build-tools, adb, NDK r27c ve CMake 3.22.1 hazır (indirilen: ${#MISSING[@]} paket)"
NDK="$SDK/ndk/$NDK_VER"

# ------------------------------------------------------------------ 4. Unity
say "Unity"
HUB="/Applications/Unity Hub.app/Contents/MacOS/Unity Hub"
export UNITY_HUB_CLI_NO_DEPRECATION_WARNING=1
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
  RELEASES="$(hub editors -r || true)"
  VER="$(echo "$RELEASES" | grep -Eo '6000\.0\.[0-9]+f[0-9]+' | sort -V | tail -1 || true)"   # Unity 6 LTS
  [ -n "$VER" ] || VER="$(echo "$RELEASES" | grep -Eo '6000\.[0-9]+\.[0-9]+f[0-9]+' | sort -V | tail -1 || true)"
  [ -n "$VER" ] || die "Hub'dan Unity 6 sürüm listesi alınamadı. Unity Hub'ı bir kere açıp giriş yap, sonra tekrar çalıştır."
  say "Unity $VER + sadece Android Build Support kuruluyor (SDK/NDK/JDK Mac'tekilerden kullanılacak)"
  ARCH="x86_64"
  [ "$(uname -m)" = "arm64" ] && ARCH="arm64"
  "$HUB" -- --headless install --version "$VER" --architecture "$ARCH" -m android \
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
if [ ! -d "$ANDROID_DIR" ]; then
  say "Unity Android Build Support kuruluyor (SDK/NDK alt paketleri olmadan)"
  [ -x "$HUB" ] || die "Unity Hub bulunamadı. Hub → Installs → $UNITY_VERSION → Add modules → Android Build Support (alt kutuları işaretleme)."
  "$HUB" -- --headless install-modules --version "$UNITY_VERSION" -m android \
    || die "Modül kurulamadı. Hub'dan elle ekle: Installs → $UNITY_VERSION → Add modules → Android Build Support."
  [ -d "$ANDROID_DIR" ] || die "Android modülü hâlâ görünmüyor. Hub'dan elle ekleyip tekrar çalıştır."
fi
ok "Android Build Support hazır"

# Unity looks for its tools in AndroidPlayer/{SDK,NDK,OpenJDK}. Link the Mac's own copies there
# (only where Unity has no copy of its own) so nothing is downloaded twice.
link_tool() {   # link_tool <name> <target>
  local dst="$ANDROID_DIR/$1"
  if [ -L "$dst" ] || [ ! -e "$dst" ]; then
    ln -sfn "$2" "$dst" || die "Bağlantı oluşturulamadı: $dst → $2"
    ok "Unity $1 → $2"
  else
    ok "Unity $1: kendi kopyası kullanılıyor"
  fi
}
link_tool SDK "$SDK"
link_tool NDK "$NDK"
link_tool OpenJDK "$JDK"

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

# ------------------------------------------------------------------ 5. Signing key
say "İmza anahtarı"
KEYTOOL="$JDK/bin/keytool"
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

# ------------------------------------------------------------------ 6. Version
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

# ------------------------------------------------------------------ 7. Build + release
say "Build + GitHub release"
export UNITY_PATH="$UNITY"
export DONER_KEYSTORE="$KEYSTORE"
export DONER_KEYSTORE_PASS="$PASS"
export DONER_KEY_ALIAS="$KEY_ALIAS"
export DONER_VERSION="$VERSION"
export DONER_VERSION_CODE="$VERSION_CODE"
export DONER_RELEASE=1
export DONER_TOOLS_LINKED=1
bash ./Tools/build-android.sh

APK="Builds/Android/DonerMaster-$VERSION.apk"

# ------------------------------------------------------------------ 8. Install on a connected phone
ADB="$SDK/platform-tools/adb"
if [ -x "$ADB" ] && [ -f "$APK" ] && "$ADB" devices | grep -q "device$"; then
  say "Telefona kuruluyor"
  "$ADB" install -r "$APK" && ok "kuruldu"
fi

URL="$(gh release view "v$VERSION" -R "$REPO" --json url -q .url 2>/dev/null || true)"
say "Bitti 🎉"
echo "  APK:     $DIR/$APK"
[ -n "$URL" ] && echo "  Release: $URL" && open "$URL" 2>/dev/null || true
