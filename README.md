# Doner Master: Kebab Shop (MVP)

Mobil, dükkân yönetimli döner yapma oyunu. Unity 6, C#. Şu an **gri kutu prototip**: tüm görseller primitive objelerden, tüm sesler kodla üretiliyor. Amaç önce **kesme hissini ve oyun döngüsünü** kanıtlamak; asset'ler sonra tek tek değiştirilecek.

## Çalıştırma

1. Unity Hub → **Add → Add project from disk** → bu klasörü seç. (Hub farklı bir Unity 6 sürümü önerirse kurulu olan 6000.x sürümünü seç.)
2. Proje açılınca herhangi bir sahnede **Play**'e bas. Oyun kendini kodla kuruyor; sahneye bir şey eklemene gerek yok.
3. Game penceresini dikey bir telefon çözünürlüğüne al (ör. 1080x1920).
4. Build almak için: menüden **Doner Master → Create Main Scene (for builds)**. Sahneyi oluşturur, Build Settings'e ekler, dikey ekranı ayarlar.

### Terminalden release APK (macOS)

```bash
# bir kere: Unity Hub → Unity 6 → Add modules → Android Build Support (OpenJDK + SDK/NDK)
keytool -genkeypair -v -keystore ~/keys/doner-release.keystore -alias doner -keyalg RSA -keysize 2048 -validity 10000

export DONER_KEYSTORE=~/keys/doner-release.keystore
export DONER_KEYSTORE_PASS='şifren'
./Tools/build-android.sh            # → Builds/Android/DonerMaster-0.1.0.apk
```

Build bitince dosya GitHub'da **v<sürüm>** release'i olarak yayınlanır (gerekli: `brew install gh && gh auth login`; kapatmak için `DONER_RELEASE=0`). Aynı sürümü tekrar build edersen o release'teki dosya güncellenir.

Sürüm: `DONER_VERSION=0.2.0 DONER_VERSION_CODE=2`. Play Store için AAB: `DONER_AAB=1`. Build sırasında proje Unity editöründe açık olmamalı.

Diğer menüler: **Reset Save Data**, **Give 1000 Coins (debug)** (Play modundayken).

## Nasıl oynanır

| Hareket | Ne olur |
| --- | --- |
| Dönerin üstünde parmağı **aşağı** kaydır | Et keser. Yavaş ve düzenli = ince dilim, hızlı = kalın parça |
| Dönerin üstünde **yana** kaydır | Şişi döndürür; daha iyi pişmiş tarafı öne getirir |
| LAVASH / BREAD | Siparişe başlar |
| TRAY (tepsi) | Kesilen eti tahtaya koyar |
| Malzeme kutuları | Domates, soğan, marul, turşu, biber |
| AYRAN dolabı | Siparişe ayran ekler |
| BOARD (tahta) | Dürümü sarar |
| Müşteri ya da baloncuğu | Servis eder |
| HEAT düğmesi | Ocak: LOW / MED / HIGH |
| TRASH | Tahtayı ya da tepsideki yanık etleri temizler |

Şişin üstündeki etiket öndeki yüzeyin pişme durumunu gösterir: RAW → COOKING → PERFECT → BURNT.

## Oyun sistemleri

- **Döner şişi:** 4 yüzey bölgesi, ocağa bakan taraf daha hızlı pişer. Kesince dış katman gider, altındaki daha az pişmiş et açığa çıkar. Şiş 60 dilimde biter, dokununca 4 sn'de yenisi takılır.
- **Kalite puanı (0–100):** malzeme %35, pişme %25, dilim inceliği %15, etin sıcaklığı %10, hız %15. 90+ = PERFECT, kombo çarpanı +0.1 (en fazla x2), 70 altı komboyu sıfırlar, 40 altı müşteri ödemez.
- **Müşteriler:** Öğrenci, Takım elbiseli, Taksici (müdavim, hep aynı sipariş), Turist, Teyze ("soğansız"), Gurme eleştirmen (85+ ister). Her birinin sabrı, bahşişi ve açıldığı gün farklı.
- **Gün döngüsü:** 3 dakikalık gün → kapanış → gün sonu ekranı (kazanç, yıldızlar, upgrade'ler). 1. gün adım adım eğitim var.
- **Upgrade'ler:** Keskin bıçak, güçlü ocak, ısıtmalı tepsi, dekor.
- **Juice:** prosedürel sesler (cızırtı, kesme, coin, ding, müşteri tepkileri), parçacıklar, kamera sarsıntısı/zoom, Android titreşim.
- **Kayıt:** PlayerPrefs (JSON).

Bilinçli olarak MVP dışında: reklam, IAP, login, analitik, yeni şehirler, çırak.

## Kod yapısı

```
Assets/DonerMaster/
  Scripts/Core/      GameBootstrap (giriş noktası), GameManager (gün döngüsü), GameData (tüm denge sayıları), SaveSystem, WorldBuilder
  Scripts/Gameplay/  DonerSpit, GameplayInput (kesme/döndürme/tap), MeatSlice, MeatTray, PrepBoard, Station, Customer, Order + ScoreCalculator
  Scripts/Feel/      AudioManager + ProceduralAudio, FX (parçacık), Juice (tween), CameraShake, Haptics
  Scripts/UI/        GameUI (HUD, baloncuklar, ekranlar), UIKit
  Scripts/Util/      Mats (Built-in/URP uyumlu materyal), MeshFactory, PointerInput (eski/yeni Input System)
  Editor/            DonerMenu
Tools/CompileCheck/  Unity olmadan derleme kontrolü için API stub'ları (Unity bu klasörü görmez)
```

Denge ayarları (sabır, fiyat, pişme hızı, kesme hızı aralığı, upgrade fiyatları) tek yerde: `GameData.cs`.

## Sonraki adımlar (asset entegrasyonu)

1. Stilize low-poly paket (büfe, sokak, karakterler) → `WorldBuilder` ve `Customer.BuildVisual` içindeki primitive'leri prefab'larla değiştir.
2. Gerçek ses kayıtları → `AudioManager` bileşenindeki `Overrides` listesine sürükle (kod değişmez).
3. URP + post-processing (bloom, renk) → materyaller otomatik URP Lit'e geçer.
4. Mixamo animasyonları (bekleme, sabırsızlık, yeme, sevinme, kızma).
