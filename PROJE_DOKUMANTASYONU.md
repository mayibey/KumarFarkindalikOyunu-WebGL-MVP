# KumarFarkindalikOyunu — Teknik Dokümantasyon

Bu belge, projeyi **hiç görmemiş bir geliştirici** için hazırlanmıştır. Kod tabanı: Unity (C#), hedef platform özellikle **WebGL MVP** (`README.md`).

---

## 1. Uygulamanın amacı

### Ne yapıyor?

- **Slot benzeri** bir oyun yüzeyi sunar: sembol grid’i, **spin**, **tumble** (patlayan sembollerin kalkması ve yukarıdan yenilerinin dolması), **paytable** ile ham kazanç, **çarpan (multiplier)** ile nihai ödeme, **scatter** ve **bonus** turu.
- Paralelde **senaryolu (pedagojik) bir akış** çalışır: oyuncu belirli **aşamalarda** farklı duygusal/ekonomik baskılarla karşılaşır; sistem **üst üste kazanç**, **zorunlu boş spin**, **ödeme bantları**, **log ve istatistik** ile deneyimi **izlenebilir** kılar.

### Kime yönelik?

- **Eğitim / farkındalık** senaryoları düşünülerek tasarlanmıştır (ticari kumar operatörü modeli değildir).
- **Araştırmacı, eğitmen veya geliştirici** test için **Admin** sahnesinden parametreleri oynayabilir; oyuncu tarafı **profil + senaryolu oyun** veya **sadece admin test sahnesi** ile girer.

### Hangi problemi çözüyor?

- “Şans oyunu” arayüzünün arkasındaki **bakiye, bahis, seri kazanç, ödeme modeli, zorluk** gibi mekanikleri **tek bir uygulamada** birleştirir.
- **Oturum logları** ve **profil istatistikleri** ile davranışın **ölçülebilir** olmasını hedefler (`LogYoneticisi`, `GameManager` istatistik alanları, `SenaryoYoneticisi` takip değişkenleri).

---

## 2. Sahne akışı

### Build’e kayıtlı sahneler

`ProjectSettings/EditorBuildSettings.asset` içinde (satır 7–25 civarı) şu sıra ile kayıtlıdır:

| Sıra | Dosya |
|------|--------|
| 1 | `Assets/Scenes/01_GirisScene.unity` |
| 2 | `Assets/Scenes/02_SenaryoluOyun.unity` |
| 3 | `Assets/Scenes/03_AdminOyunScene.unity` |
| 4 | `Assets/Scenes/06_AdminOyunKopya.unity` |
| 5 | `Assets/Scenes/04_LogScane.unity` |
| 6 | `Assets/Scenes/05_AdminTutorialScene.unity` |

### Kullanıcı uygulamayı açınca (tipik)

1. **`01_GirisScene`**  
   - `GameManager` sahne yüklüdür (`01_GirisScene.unity` içinde bileşen ataması).  
   - `GirisUI`: Oyuncu adı girilir veya listeden seçilir; `GameManager.SelectOrCreatePlayer` çağrılır.  
   - Oyun tipi: **Senaryolu oyun**, **Admin oyun**, **Admin tutorial**, **Admin kopya** butonları (`GirisUI.cs` başlıklar ve `hedefSahne` mantığı, yaklaşık satır 665–766).

2. **Senaryolu yol** → `GameManager.LoadScene("02_SenaryoluOyun")`  
   - Ana oyun: `OyunYoneticisi`, `SenaryoYoneticisi`, grid, spin, tumble.

3. **Admin yolu** → `AdminGirisDogrulama` ile şifre sonrası `03_AdminOyunScene` veya `06_AdminOyunKopya` (`GirisUI.cs` satır 675–682, 755–762).  
   - Not: `GirisUI` içinde `adminTutorialSceneAdi` ve `adminOyunKopyaSceneAdi` **ikisi de** varsayılan olarak `"06_AdminOyunKopya"` (`GirisUI.cs` satır 31–33). Yani giriş akışındaki “tutorial” ve “kopya” aynı sahneye gidebilir; `05_AdminTutorialScene` build’de varken giriş UI varsayılanında farklı sahneye bağlı olmayabilir — bkz. bölüm 8.

4. **Log sahnesi** → `04_LogScane`  
   - `OyunYoneticisi` veya `SenaryoYoneticisi` / `LogYoneticisi` üzerinden geçiş (`OyunYoneticisi.cs` ~4584; `SenaryoYoneticisi.cs` ~870; `LogYoneticisi.cs` ~1778 girişe dönüş).

5. **Geri dönüş**  
   - `GirisDonButonu`, `LogYoneticisi` vb. `girisSahneAdi` / `01_GirisScene` benzeri isimle girişe döner.

### `Assets/_Recovery/` klasörü

Unity’nin ürettiği **yedek sahne** dosyalarıdır; normal kullanıcı akışının parçası değildir.

---

## 3. Oyun mekaniği

### 3.1 Spin (özet)

**Ana orkestrasyon:** `OyunYoneticisi.cs` (çok büyük dosya; spin başı, simülasyon, animasyon, sonuç).

Tipik akış:

1. Bahis düşülür (`EkonomiServisi` + `GameManager` senkronu).
2. **Simülasyon** (çoğunlukla bir döngüde çoklu “reroll”): rastgele veya senaryoya özel grid üretimi, tumble adımları, toplam ham kazanç ve çarpan.
3. **Doğrulama:** `SpinKaydiHamPaytableIleUyumluMu` (`OyunYoneticisi.cs` ~2931–2969) — her adımın `TurKazanci` değerinin, o adımdaki grid ve patlayan hücrelerle paytable’dan hesaplanan tutarla eşleşmesi; `GridRefillSonrasi` ile grid ilerletilir.
4. Onaylanan sonuç **oynatılır** (animasyon servisleri).
5. Bakiye / istatistik güncellenir (`LogServisi`, `GameManager`).

**Senaryo 1 özel yolu:** Hedef ödeme bandı zorunluysa önce `Senaryo1HedefOdemeAkisi` ile hedef TL, sonra `Senaryo1HedefOdemeMotoru` ile paytable uyumlu grid; mümkünse **iki aşamalı tumble** (`Senaryo1PaytableKonstrukteHedefSpinDene` içinde). Bant dışı kalırsa `Senaryo1FallbackKazanciniBandIcineZorla` devreye girebilir.

### 3.2 Tumble nedir?

- Grid’de **ödeme eşiğini geçen sembol kütlesi** (bu projede kural: **aynı sembolün grid üzerindeki toplam adedi ≥ eşik**, genelde **8**) bulunur.
- Bu hücreler **patlar** (kaldırılır), sütunlarda **çökme** ve **yukarıdan refill** uygulanır.
- Bir spin içinde birden fazla **tumble turu** olabilir: yeni grid yine eşik üstü küme oluşturuyorsa süreç devam eder (üst sınır `OyunKorumaServisi.MAX_TUMBLE_TUR`).

**Önemli:** Küme tespiti **komşuluk (flood-fill) ile bağlı bileşen** değil; **tüm gridde sembol başına toplam sayım** (`TumbleServisi.FindClustersToRemove`, `TumbleAyarlari.FindClustersToRemove`).

### 3.3 Çarpan sistemi

- **Ayarlar:** `CarpanAyarlari.cs` (üretim olasılığı, max adet, zorla çarpan değeri).
- **Çalışma:** `CarpanServisi.cs` — spin içi çarpan birikimi, `MulClampInt` ile ham kazanç × toplam çarpan → **nihai TL** (taşma koruması).
- **Admin:** Panelden çarpan olasılığı / max adet; ayrıca **zorla çarpan** butonları (`AdminPanel.cs` başlıklar ~28–54).

### 3.4 Bonus nasıl tetikleniyor?

- **Scatter:** `TumbleAyarlari` içinde scatter indeksi ve düşme şansları; grid doldururken / refill sırasında scatter üretimi politikaya bağlı (`RandomSymbolWithScatterChance` vb.).
- **Bonus modu (`bonusAktif`):** `OyunYoneticisi` içinde geniş mantık: bonus başlangıcında havuzdan pay ayırma (`_bonusPendingOdemeTL` vb. yorumlar ~403–414), bonus içi spin limitleri, zorla çarpan birikimi, zirve bonus bayrakları (`_buBonusZirveBonusuMu`).
- **UI:** `BonusUIServisi.cs`, `CiftSansKutusu.cs`, `BonusAyarlari.cs`.

Detaylı tetik koşulları tek paragrafta özetlenemez; değişiklik yapacak geliştirici `OyunYoneticisi` içinde `bonusAktif`, `scatter`, `BonusSatinAl` benzeri aramalarla ilerlemelidir.

---

## 4. Senaryo sistemi

### Ne işe yarar?

- Oyuncuyu **7 aşamalı** bir yolculukta tutar; her aşamada **farklı zorluk, log, üst üste ödeme soğutması, bakiye baskısı** gibi parametreler devreye girebilir.
- **PlayerPrefs** ile `playerId` bazlı kalıcılık (ör. `PP_SENARYO_MEVCUT_ASAMA_` + id, `GameManager.TumKullanicilariVeVerileriSil` içinde silinen anahtarlar ~88–92).

### Kaç “senaryo” / aşama?

**Dosya:** `SenaryoYoneticisi.cs` — `SenaryoAsama` enum (satır ~13–21):

1. `Asama1_IsindirmaUmut`  
2. `Asama2_KontrolBende`  
3. `Asama3_AzDahaKayipKovalama`  
4. `Asama4_BakiyeTukenis`  
5. `Asama5_BonusZirve`  
6. `Asama6_GercekKayip`  
7. `Asama7_Finale`

### Nasıl çalışır?

- `mevcutAsama`, `toplamSpin`, `consecutivePayCount` / `forcedNoPayKalan` (üst üste ödeme sonrası zorunlu boş spin), net kayıp eşik logları, uzun oyun uyarıları (`SenaryoYoneticisi.cs` başı ~24–78 ve devamı).
- Aşama geçiş tablosu yorumda özetlenir (~80+): “Isındırma → Kontrol → …”.
- **Spin politikası:** `Services/Senaryolar/SenaryoSpinPolitikasi.cs` arayüzü, `VarsayilanSpinPolitikasi.cs` ve `AdminSenaryoSpinPolitikalari.cs` uygulamaları — simülasyonda “yeniden dene”, “bant kontrolü”, admin özel kuralları.

### Admin “senaryo preset” (karışmaması için)

`AdminPanel.cs` ve `OyunYoneticisi.cs` içinde **1–5 numaralı ön ayarlar** (bahis, scatter %, çarpan %, max çarpan, zorla çarpan, max scatter) **oyun dengesini hızlı kurmak** içindir; bunlar **enum’daki 7 psikolojik aşama** ile aynı şey değildir — isim benzerliğine dikkat.

---

## 5. Admin paneli

**Dosya:** `AdminPanel.cs`

### Admin ne yapabilir?

- **Şifre ile panel açma** (`adminSifre`, satır ~9–16).
- **Zorluk slider** → `OyunYoneticisi` / zorluk bağlamına yansır (başlık ~19–21).
- **Scatter slider** ve metinler (~23–26).
- **Çarpan ayarları:** olasılık ve max adet slider’ları (~28–32).
- **Senaryo preset dropdown** (1–5) ve toggle (~34–44); preset tablosu ~78–87.
- **Zorla çarpan butonları** (x2, x5, x10, x50, x100, sıfırla) (~46–54).
- **Kullanıcıları silme** gibi işlemler `GameManager` ile bağlanır (`AdminPanel.cs` içinde ilgili metotlar; özet grep ile ~859+).

**Admin oyun sahnesi:** `03_AdminOyunScene` — tam `OyunYoneticisi` ile test.  
**Admin kopya / tutorial girişi:** `06_AdminOyunKopya` — `OyunYoneticisiAdminOyunKopya` türev sınıfı (`Assets/Scripts/AdminOyunKopya/`).

---

## 6. Veri akışı

### Kullanıcı profili

- **Model:** `PlayerProfile.cs` (isim, id, bakiye, toplam spin, kazan/kayıp/net, yatır/çek vb.).
- **Yönetim:** `GameManager.cs`  
  - **Editor / standalone:** `Application.persistentDataPath` altında `profiles.json` (sabit `ProfilDosyaYolu`, satır ~9).  
  - **WebGL:** `PlayerPrefs` anahtarı `PP_WEBGL_PROFILES_JSON_V1` (satır ~10, yükleme ~23–35, kaydetme ~61–65).
- **Seçim:** `SelectOrCreatePlayer` (`GameManager.cs`); giriş `GirisUI.cs`.

### Bakiye ve bahis

- **Oyun içi kaynak:** `EkonomiServisi.cs` — `EkonomiYukleGameManagerVeyaPrefs`; güncellemeler `GameManager.ActivePlayer.balance` ve `SaveProfiles` ile diske/PlayerPrefs’e gider.
- **PlayerPrefs yedek anahtarları:** `GameManager.TumKullanicilariVeVerileriSil` içinde özetlenir (~94–100: bakiye, bahis, kasa vb.).

### Log / istatistik

- **Oyun içi ekonomi log çağrıları:** `LogServisi.cs` → `GameManager.RecordEconomyAction` / `Log`.
- **Log sahnesi UI:** `LogYoneticisi.cs` — tablolar, oturum özeti, dışa aktarma (clipboard); **TODO** satır ~1662: bonus replay UI.
- **Senaryo oturum logu:** `SenaryoYoneticisi` PlayerPrefs anahtarı `PP_SENARYO_OTURUM_LOGU_` + `playerId` (silme `GameManager` ~91).

---

## 7. Kritik sınıflar (en önemli 10)

| # | Sınıf | Dosya | Görevi |
|---|--------|--------|--------|
| 1 | `OyunYoneticisi` | `Assets/Scripts/OyunYoneticisi.cs` | Spin, simülasyon, tumble oynatma, bonus, admin köprüleri, senaryo 1 konstrükte yolu — **merkez orkestratör** (dosya çok büyük). |
| 2 | `SenaryoYoneticisi` | `Assets/Scripts/SenaryoYoneticisi.cs` | 7 aşama, üst üste ödeme soğutması, takip, sahne geçişleri (ör. log). |
| 3 | `GameManager` | `Assets/Scripts/GameManager.cs` | Profil listesi, aktif oyuncu, sahne yükleme, kalıcı kayıt, toplu silme. |
| 4 | `TumbleAyarlari` | `Assets/Scripts/TumbleAyarlari.cs` | Paytable, scatter ayarı, grid üzerinde küme bulma ve ham kazanç hesabı. |
| 5 | `TumbleServisi` | `Assets/Scripts/Services/TumbleServisi.cs` | Tumble döngüsü için küme bulma / kazanç delegasyonu (`OyunYoneticisi`’ne bağlı). |
| 6 | `CokmeAkisServisi` | `Assets/Scripts/Services/CokmeAkisServisi.cs` | Çökme + refill; `SpinSimulasyonKaydi.GridRefillSonrasi`. |
| 7 | `CarpanServisi` | `Assets/Scripts/Services/CarpanServisi.cs` | Çarpan çarpımı, `MulClampInt`. |
| 8 | `EkonomiServisi` | `Assets/Scripts/Services/EkonomiServisi.cs` | Bakiye/bahis iş kuralları; `GameManager` senkronu. |
| 9 | `Senaryo1HedefOdemeMotoru` | `Assets/Scripts/Senaryolar/Senaryo1HedefOdemeMotoru.cs` | Senaryo 1 hedef ödemeye uygun küme seçimi, ilk grid, iki aşamalı tumble için ikinci küme enjeksiyonu. |
| 10 | `SpinSimulasyonKaydi` | `Assets/Scripts/Services/SpinSimulasyonKaydi.cs` | Spin simülasyon kaydı; adımlar ve refill sonrası grid kopyası (paytable doğrulaması için). |

**Yakın ikinciller:** `DonusAkisServisi`, `IzgaraServisi`, `AnimasyonServisi`, `BonusUIServisi`, `VarsayilanSpinPolitikasi`, `AdminPanel`.

---

## 8. Bilinen sorunlar ve teknik borç

1. **`OyunYoneticisi.cs` monolitik** — binlerce satır; okuma/yeni özellik riski yüksek. Proje kurallarında (`.cursor/rules`) uzun vadede servislere bölünmesi hedeflenir.
2. **Giriş sahnesi vs tutorial sahnesi:** `GirisUI.cs` satır 31–33’te `adminTutorialSceneAdi` ve `adminOyunKopyaSceneAdi` ikisi de `"06_AdminOyunKopya"`. Build’de `05_AdminTutorialScene` varken giriş akışı varsayılanında bu sahneye **gitmeyebilir** — tutarsızlık / kullanılmayan sahne riski.
3. **`TumTmpSariStilZorlayici.cs`** — projede başka referans bulunamadıysa **ölü kod** olabilir (sahne/prefab elle atanmış olabilir; doğrulama Unity Editor ile yapılmalı).
4. **`LogYoneticisi.cs` ~1662** — `TODO`: bonus replay UI eksik.
5. **`Assets/_Recovery/`** — yedek sahneler; repo temizliği ve yanlışlıkla referans verilmemesi önerilir.
6. **Ödeme hissi karmaşıklığı:** Senaryo 1’de hedef jitter, çok adımlı konstrükte, fallback birlikte **tutarsız hissedilen** ödeme aralıkları üretebilir; ayar ve log satırlarıyla ayıklama gerekir.

---

## Ek: Hızlı dosya haritası

| Klasör | İçerik |
|--------|--------|
| `Assets/Scripts/` | MonoBehaviour’lar, `OyunYoneticisi`, UI, admin |
| `Assets/Scripts/Services/` | Ekonomi, ızgara, tumble, animasyon, log, bonus UI… |
| `Assets/Scripts/Services/Senaryolar/` | Spin politikası arayüzü + admin/varsayılan uygulamalar |
| `Assets/Scripts/Senaryolar/` | Senaryo 1 hedef ödeme akışı ve motoru |
| `Assets/Scripts/AdminOyunKopya/` | Admin kopya sahnesi için ince uzatılmış sınıflar |
| `Assets/Editor/` | Build ve editör yardımcıları |
| `Assets/Scenes/` | Oyun ve admin sahneleri |

---

*Belge oluşturulma: repo kökü `PROJE_DOKUMANTASYONU.md`. Kod satır numaraları yaklaşık referanslıdır; IDE’de dosya değiştikçe kayabilir.*
