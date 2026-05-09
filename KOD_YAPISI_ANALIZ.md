# KOD YAPISAL ANALİZ RAPORU — KumarFarkindalikOyunu
**Tarih:** 2026-04-20 | **Analist:** Claude Sonnet 4.6 | **Kapsam:** 76 .cs dosyası, ~18.000 satır

---

## BÖLÜM 1 — BİRLEŞTİRİLEBİLİR DOSYALAR

Çok küçük, tek sorumlu veya saf delegation dosyaları. Başka bir dosyaya gömülmeleri dosya sayısını azaltır, okunabilirliği artırır.

---

### 1.1 `BombEfektServisi.cs` + `CarpanSokEfektServisi.cs` → `CarpanDarbeEfektServisi.cs`

| Alan | Değer |
|---|---|
| **Mevcut satır** | 77 + 85 = 162 |
| **Hedef satır** | ~120 |
| **Gerekçe** | İkisi de "çarpan düşünce ne olur" sorusunu yanıtlıyor. `BombEfektServisi` beyaz flash + kamera shake; `CarpanSokEfektServisi` canvas shake. Her ikisi de `BaslatXxx(MonoBehaviour, int)` imzasına sahip. Aynı çağrı noktasından (`OyunYoneticisi.cs:7369`) art arda tetikleniyorlar. Tek bir `CarpanDarbeEfektServisi` ikisini kapsayabilir. |
| **Tahmini süre** | 30 dakika |

---

### 1.2 `DonusServisi.cs` + `UIServisi.cs` + `SenaryoServisi.cs` → `OyunYoneticisiDelegasyonServisleri.cs`

| Alan | Değer |
|---|---|
| **Mevcut satır** | 32 + 49 + 46 = 127 |
| **Hedef satır** | ~100 |
| **Gerekçe** | Üçü de saf `Action`/`Func` delegation wrapper'ı. Hiçbirinde kendi state'i yok; her metod OyunYoneticisi'nin bir metodunu çağırıyor. Tek bir dosyada `region` ile bölünürse arama kolaylaşır, 3 dosya yerine 1. |
| **Tahmini süre** | 20 dakika |

---

### 1.3 `Senaryo1HedefOdemeAkisi.cs` → `Senaryo1HedefOdemeMotoru.cs` içine

| Alan | Değer |
|---|---|
| **Mevcut satır** | 26 |
| **Hedef** | `Senaryo1HedefOdemeMotoru.cs` alt sınıf veya iç sınıf olarak |
| **Gerekçe** | 26 satır, tek metod (`HedefNihaiOdemeSec()`). Yalnızca `Senaryo1HedefOdemeMotoru` tarafından kullanılıyor. Ayrı dosya olması hiçbir şey katmıyor. |
| **Tahmini süre** | 10 dakika |

---

### 1.4 `GirisDonButonu.cs` → `GirisUI.cs` içine

| Alan | Değer |
|---|---|
| **Mevcut satır** | 16 |
| **Gerekçe** | Tek yapısı `SceneManager.LoadScene("GirisEkrani")`. `GirisUI.cs` zaten o sahneyle ilgileniyor. |
| **Tahmini süre** | 10 dakika |

---

### 1.5 `SenaryoSpinPolitikasi.cs` + `VarsayilanSpinPolitikasi.cs` + `AdminSenaryoSpinPolitikalari.cs` → `SpinPolitikalari.cs`

| Alan | Değer |
|---|---|
| **Mevcut satır** | ~50 + ~75 + ~50 = ~175 |
| **Gerekçe** | Üçü de aynı konsept: "bu spinde ödeme bandı ne olacak?" Interface + varsayılan + admin override. Hepsi `ISpinPolitikasi` altında tek dosyada yaşayabilir, alt sınıflar `region` ile ayrılır. |
| **Tahmini süre** | 30 dakika |

---

### 1.6 `AdminOyunKopya/` klasörü (3 dosya) → Ana sınıflara `#if` guard ile

| Alan | Değer |
|---|---|
| **Mevcut satır** | 5 + ~15 + ~15 = ~35 |
| **Gerekçe** | `OyunYoneticisiAdminOyunKopya.cs` yalnızca `OyunYoneticisi`'nin boş bir subclass'ı (5 satır!). `CarpanAyarlariAdminOyunKopya` ve `TumbleAyarlariAdminOyunKopya` da yalnızca `CarpanAyarlari` ve `TumbleAyarlari`'yi extend ediyor. Bu pattern gereksiz; admin sahnesine özel davranış `[SerializeField] bool adminSahnesi` flag'i ile ana sınıfta yönetilebilir. |
| **Tahmini süre** | 45 dakika |

---

### 1.7 `OyunBootstrapServisi.cs` → `OyunYoneticisi.Core.cs` (partial class)

| Alan | Değer |
|---|---|
| **Mevcut satır** | 27 |
| **Gerekçe** | Sadece `IOyunBootstrapBaglami` interface tanımı + bootstrap metodunu çağıran 3-4 satır. OyunYoneticisi bootstrap zaten Start/Awake içinde; bu ayrı dosya soyutlama değil gürültü. |
| **Tahmini süre** | 15 dakika |

---

## BÖLÜM 2 — GEREKSİZ DOSYALAR (Güvenle Silinebilir)

---

### 2.1 `SonSpinListeOlaylari.cs` ⛔ SİL

| Alan | Değer |
|---|---|
| **Mevcut satır** | **0** — boş dosya |
| **Gerekçe** | İçi tamamen boş. Referans veren kod yok. Muhtemelen başlangıçta bir şeyler yazılacaktı, sonra terk edildi. |
| **Risk** | Sıfır. |
| **Tahmini süre** | 2 dakika |

---

### 2.2 `TumTmpSariStilZorlayici.cs` ⛔ SİL

| Alan | Değer |
|---|---|
| **Mevcut satır** | 108 |
| **Gerekçe** | Tüm TMP_Text bileşenlerini sarıya boyuyor. "Test/debug amaçlı" olarak açıklanmış. Production build'de zararlı — tüm metinleri bozar. Inspector'dan deactivate edilmiş olsa bile dosyanın varlığı karışıklık yaratıyor. |
| **Risk** | Düşük. Silinmeden önce sahnelerde herhangi bir GameObject'e atanıp atanmadığını kontrol et. |
| **Tahmini süre** | 5 dakika (kontrol dahil) |

---

### 2.3 `OdemeServisi.cs` içindeki `GetSpinOdenebilirLimitRaw()` — **metod** silinmeli

| Alan | Değer |
|---|---|
| **Mevcut satır** | 52 (dosya korunur, metod silinir) |
| **Gerekçe** | Her zaman `int.MaxValue` döndürüyor; havuz %10 hesabı kaldırılmış ama metod kalmış. Çağıran kod (`OyunYoneticisi.cs:4343`) sonucu kullanmıyor. Dead code. |
| **Risk** | Sıfır. |
| **Tahmini süre** | 5 dakika |

---

### 2.4 `CarpanAyarlari.cs` içindeki 3 metod — **metodlar** silinmeli

| Alan | Değer |
|---|---|
| **Mevcut satır** | 302 (dosya korunur, ~115 satır silinir) |
| **Silinen metodlar** | `CarpanlariDoluGriddeUygula()`, `CarpanUretVeBirik()`, `SpinBasindaSifirla()` (satır 171-286) |
| **Gerekçe** | Hiç çağrılmıyor — oyun akışı bunların yerine `CarpanServisi` ve `CarpanYerlestirmeServisi`'ni kullanıyor. Grep'te sıfır çağrı noktası. |
| **Risk** | Düşük. Grep ile her metodun çağrılmadığını doğrula. |
| **Tahmini süre** | 15 dakika |

---

### 2.5 `BuildUILayoutFix.cs` — kaldırılabilir veya `#if` guard altına alınmalı

| Alan | Değer |
|---|---|
| **Mevcut satır** | 95 |
| **Gerekçe** | "Build'de UI layout problemleri düzeltir" açıklaması var. Bu tür "force canvas rebuild" kodları genellikle altta yatan sorunu çözmez, maskeler. Underlying layout bug'ı düzeltilirse bu dosya gereksiz kalır. Bırakılacaksa `#if UNITY_WEBGL && !UNITY_EDITOR` ile korunmalı. |
| **Risk** | Orta. Kaldırmadan önce WebGL build'de UI bozulup bozulmadığını test et. |
| **Tahmini süre** | 30 dakika (test dahil) |

---

## BÖLÜM 3 — BÖLÜNMESI GEREKENLER

---

### 3.1 `OyunYoneticisi.cs` — 7.911 satır 🔴 KRİTİK

| Alan | Değer |
|---|---|
| **Mevcut satır** | 7.911 |
| **Uygulama** | C# `partial class` — derleme davranışı değişmez, IDE refactor risksiz |

**Önerilen bölünme:**

| Dosya | İçerik | Tahmini Satır |
|---|---|---|
| `OyunYoneticisi.Core.cs` | Inspector alanları, Awake/Start/Update, servis init, interface implementations | ~800 |
| `OyunYoneticisi.Spin.cs` | `SpinButon()`, `BirSpinHazirlaVeAt()`, `OtomatikSpinDongusu()`, precompute | ~600 |
| `OyunYoneticisi.Bonus.cs` | `BaslatBonus()`, `ShowBonusEndMessage()`, bonus bütçe hesabı | ~500 |
| `OyunYoneticisi.Simulasyon.cs` | `SimuleEtVeKaydetImpl()` + tüm reroll döngüsü | ~700 |
| `OyunYoneticisi.Senaryo1.cs` | S1 konstrukte metodları | ~350 |
| `OyunYoneticisi.Senaryo2.cs` | S2 konstrukte + döngü metodları | ~300 |
| `OyunYoneticisi.Senaryo3.cs` | S3 konstrukte metodları | ~350 |
| `OyunYoneticisi.Senaryo45.cs` | S4+S5 bomb konstrukte + döngü | ~500 |
| `OyunYoneticisi.Oynatma.cs` | `SimulasyonKaydiniOynat()` + grid replay kodu | ~600 |
| `OyunYoneticisi.UI.cs` | Popup'lar, bakiye akışı animasyonu, flash efekti | ~400 |
| `OyunYoneticisi.Admin.cs` | Admin bahis ayarla, senaryo preset UI, `_adminManuelZorlukKilidi` | ~300 |

**Tahmini süre:** 4-6 saat (sadece bölme; mantık değişikliği yok)

---

### 3.2 `LogYoneticisi.cs` — 1.788 satır 🟠

| Alan | Değer |
|---|---|
| **Mevcut satır** | 1.788 |
| **Sorun** | Log verisi yönetimi (PlayerProfile okuma, filtreleme) + Log UI render (tablo satırları, renkler, sayfalama) aynı dosyada. |

**Önerilen bölünme:**

| Dosya | İçerik | Tahmini Satır |
|---|---|---|
| `LogYoneticisi.cs` (azaltılmış) | Sadece UI koordinasyonu, panel aç/kapat | ~400 |
| `LogVeriAnalizcisi.cs` | Filtreleme, istatistik hesaplama, log sıralama | ~600 |
| `LogSatirOlusturucu.cs` | Tablo satırı prefab oluşturma, renk kodu, format | ~500 |

**Tahmini süre:** 3 saat

---

### 3.3 `SenaryoYoneticisi.cs` — 1.061 satır 🟠

| Alan | Değer |
|---|---|
| **Mevcut satır** | 1.061 |
| **Sorun** | Aşama geçiş mantığı + pedagojik uyarı UI + log sistemi + bakiye yükleme hakkı + singleton state tek dosyada. |

**Önerilen bölünme:**

| Dosya | İçerik | Tahmini Satır |
|---|---|---|
| `SenaryoYoneticisi.cs` (azaltılmış) | Singleton state, aşama geçişi, `SpinTamamlandi()` | ~400 |
| `SenaryoPedagojikUIServisi.cs` | Pedagojik uyarı metinleri, UI gösterimi, eşik kontrolü | ~350 |
| `SenaryoLogServisi.cs` | `LogEkle()`, `KaydetOturumLogu()`, oturum log yönetimi | ~250 |

**Tahmini süre:** 2.5 saat

---

### 3.4 `AdminPanel.cs` — 993 satır 🟡

| Alan | Değer |
|---|---|
| **Mevcut satır** | 993 |
| **Sorun** | Senaryo preset UI + çarpan preset butonları + zorluk/scatter slider'lar + kasa manuel kontrol + tutorial akışı hepsi aynı dosyada. |

**Önerilen bölünme:**

| Dosya | İçerik | Tahmini Satır |
|---|---|---|
| `AdminPanel.cs` (azaltılmış) | Şifre doğrulama, ana panel aç/kapat koordinasyonu | ~200 |
| `AdminSenaryoPresetUI.cs` | 5 senaryo preset seçimi, `UygulaAdminSenaryo()` | ~300 |
| `AdminOyunAyarlariUI.cs` | Zorluk slider, scatter slider, çarpan butonları | ~250 |
| `AdminKasaUI.cs` | Ana kasa / ödül havuzu manuel giriş, reset | ~150 |

**Tahmini süre:** 2 saat

---

### 3.5 `GirisUI.cs` — 768 satır 🟡

| Alan | Değer |
|---|---|
| **Mevcut satır** | 768 |
| **Sorun** | Kullanıcı listesi + yeni kullanıcı oluşturma + admin giriş doğrulama + hover efektleri + oyun modu seçimi hepsi bir arada. |

**Önerilen bölünme:**

| Dosya | İçerik | Tahmini Satır |
|---|---|---|
| `GirisUI.cs` (azaltılmış) | Ana panel koordinasyonu, sahne geçişi | ~250 |
| `KullaniciSecimUI.cs` | Oyuncu listesi, seçim, silme | ~300 |
| `GirisAnimasyonlari.cs` | Hover efektleri, panel açılış animasyonu | ~150 |

**Tahmini süre:** 2 saat

---

### 3.6 `BakiyeYuklePanelMetinStili.cs` — 616 satır 🟡

| Alan | Değer |
|---|---|
| **Mevcut satır** | 616 |
| **Sorun** | Beklenmedik büyüklükte bir "stili uygula" dosyası. Gradient metin, gölge, animasyon, köşe yumuşatma, buton hizalama — hepsi ayrı ayrı Coroutine'lerle yönetiliyor. |
| **Öneri** | `PanelStilAyarlari.cs` (ScriptableObject config) + `PanelStilUygulayici.cs` (~200 satır). Config ayrıldığında ana dosya küçülür. |
| **Tahmini süre** | 2 saat |

---

### 3.7 `BonusUIServisi.cs` — 631 satır 🟡

| Alan | Değer |
|---|---|
| **Mevcut satır** | 631 |
| **Sorun** | Bonus tur sayacı + kazanç göstergesi + animasyon + spin ikonları + final mesaj hepsi tek servis. |
| **Öneri** | `BonusUIServisi.cs` (~300 satır, koordinasyon) + `BonusAnimasyonServisi.cs` (~250 satır, animasyon coroutine'leri). |
| **Tahmini süre** | 1.5 saat |

---

### 3.8 `Senaryo1-5HedefOdemeMotoru.cs` — 5 dosya, ~1.720 satır toplam 🟡

| Alan | Değer |
|---|---|
| **Mevcut satır** | S1:378 + S2:371 + S3:434 + S4:273 + S5:265 = **1.721** |
| **Sorun** | `KumeBoyuAgirligi()`, `TryPaytableUyumluTekKumeSec()`, `AgirlikliSec()`, `TryTekKumeliIlkGridOlustur()` metodları S1/S2/S4/S5'te **neredeyse birebir kopyalanmış**. Tek gerçek fark: S3'te Fisher-Yates yerine BFS büyüme var. |
| **Öneri** | `HedefOdemeMotorBase.cs` abstract base class: ortak metodlar burada. S1-S5 sadece farklı olan metodları override eder. Toplam ~1.000 satıra düşer. |
| **Tahmini süre** | 3 saat |

---

## BÖLÜM 4 — DOĞRU YERDE OLANLAR

Okunabilir, tek sorumlu, uygun büyüklükte. Dokunmaya gerek yok.

| Dosya | Satır | Neden Doğru |
|---|---|---|
| `PlayerProfile.cs` | 103 | Temiz veri modeli. Sadece alan + hesaplanmış özellikler. |
| `SenaryoOlayKaydi.cs` | 122 | Event log veri yapısı, bağımsız. |
| `WinFeedbackUI.cs` | 183 | Tek sorumluluk: BIG/MEGA/EPIC WIN göster. |
| `KazancSayaciUI.cs` | 119 | Spin sonu sayaç animasyonu, bağımsız. |
| `EkonomiServisi.cs` | 261 | Bakiye yönetimi mantığı sınırlı ve net. |
| `KorutinServisi.cs` | 61 | Named coroutine wrapper, temiz API. |
| `SpinSimulasyonKaydi.cs` | 53 | Veri modeli, bağımsız. |
| `OdemeServisi.cs` | 52 | (ölü metod silinince) temiz delegate. |
| `CarpanOverlayServisi.cs` | 248 | Tek sorumlu: grid üstü çarpan overlay. |
| `ScatterEfektServisi.cs` | 163 | Scatter efekti, izole. |
| `KasaYoneticisi.cs` | 268 | Kasa mantığı odaklı; UI_Guncelle'den SaveKasalar çıkarıldıktan sonra daha da iyileşir. |
| `SahneBaglamaServisi.cs` | 243 | Binding yönetimi, tek sorumlu. |
| `HizVeSesServisi.cs` | 88 | Ses + hız yönetimi, küçük ve net. |
| `OyunFormatServisi.cs` | 15 | Static utility, mükemmel. |
| `TumbleServisi.cs` | 113 | Küme algoritması, izole test edilebilir. |
| `TumbleAkisServisi.cs` | 114 | Tumble akış orkestrasyonu, net. |
| `IzgaraBaslatmaServisi.cs` | 119 | Grid init, net. |
| `GameManager.cs` | 308 | Profil yönetimi odaklı; `Destroy(this)` bug'ı dışında doğru yapı. |
| `AdminAyarUIServisi.cs` | 135 | Slider bağlama servisi, odaklı. |
| `YuvarlakUICerceveSprite.cs` | 99 | Runtime sprite utility, bağımsız. |
| `ButonBasimHissi.cs` | 61 | UI efekti, izole. |
| `ButonHoverBuyut.cs` | 52 | UI efekti, izole. |
| `SpinIconRotate.cs` | 40 | Animasyon util, izole. |
| `ZorlukServisi.cs` | 47 | Zorluk bias hesabı, net. |
| `SenaryoOdemeModelServisi.cs` | 70 | Ödeme modeli hesabı, bağımsız. |
| `OyunKorumaServisi.cs` | 35 | Sabit + clamp metodu, static utility. |
| `AnimasyonServisi.cs` | 525 | Animasyon mantığı tek yerde, kabul edilebilir. |
| `CarpanServisi.cs` | 233 | Çarpan placement yönetimi, odaklı. |
| `CokmeAkisServisi.cs` | 554 | Çökme akışı, interface ile ayrılmış. |

---

## BÖLÜM 5 — YENİDEN YAZILMASI GEREKENLER

---

### 5.1 `AdminGirisDogrulama.cs` — Güvenlik Yeniden Yazımı 🔴

| Alan | Değer |
|---|---|
| **Mevcut satır** | 268 |
| **Sorun 1** | `BeklenenKullaniciAdi = "admin"` hardcode — ama hiç kullanılmıyor. |
| **Sorun 2** | `BeklenenSifre = "admin"` hardcode — WebGL binary'den IL2CPP decompiler ile çıkarılabilir. |
| **Sorun 3** | `DogrulaVeDevamEt()` içinde yalnızca şifre karşılaştırılıyor, kullanıcı adı hiç kontrol edilmiyor (`bool dogru = string.Equals(sifre, BeklenenSifre, ...)`) |
| **Sorun 4** | Kullanıcı adı otomatik "admin" doldurulmuş (satır 100). |
| **Ne yapılmalı** | Şifreleri `SHA256` hash olarak PlayerPrefs'e taşı (ilk kurulumda sor). Doğrulama: `SHA256(girilen) == kaydedilen_hash`. Username kontrolünü geri ekle. Otomatik doldurma kaldır. |
| **Tahmini süre** | 2 saat |

---

### 5.2 `SimuleEtVeKaydetImpl()` — `OyunYoneticisi.cs:~6343-6800` 🔴

| Alan | Değer |
|---|---|
| **Mevcut satır** | ~450 satır **tek metod** |
| **Sorun** | Tek metotta: senaryo tipi tespiti, reroll döngüsü (2500 iterasyon), 5 farklı senaryo için ayrı konstrukte çağrısı, fallback mantığı, paytable doğrulama. Cyclomatic karmaşıklığı 40+. Test edilemez, takip edilemez. |
| **Ne yapılmalı** | Strategy Pattern: `ISpinKonstrukteStratejisi.Uygula(SpinParametreleri)` interface'i. Her senaryo için ayrı `Senaryo1Stratejisi`, `Senaryo4BombStratejisi` vb. `SimuleEtVeKaydetImpl` sadece stratejiyi seçip çağırır (~30 satıra düşer). |
| **Tahmini süre** | 6-8 saat |

---

### 5.3 `SenaryoYoneticisi.cs` — Aşama Geçiş Bug'ı + State Kalıcılığı 🟠

| Alan | Değer |
|---|---|
| **Mevcut satır** | 1.061 |
| **Sorun 1** | `AsamaGecisiKontrol()` satır 456: `if (this != I && (gelistirmeModu || !senaryoAktif)) return;` — `this == I` iken `gelistirmeModu` kontrolü atlanıyor. Dev modunda otomatik aşama geçişleri çalışıyor. |
| **Sorun 2** | `consecutivePayCount` ve `forcedNoPayKalan` `AsamaGecir()` içinde sıfırlanmıyor — önceki aşamanın state'i bir sonrakine sızıyor. |
| **Sorun 3** | `bahisArtirimSayisi` PlayerPrefs'e kaydedilmiyor — uygulama kapanınca sıfırlanıyor. |
| **Ne yapılmalı** | 3 bug düzeltmesi + state alanlarını `AsamaGecir()` içinde açıkça sıfırla + `bahisArtirimSayisi`'ni `PP_BAHIS_ARTIR_SAYISI_{playerId}` key'i ile kaydet. |
| **Tahmini süre** | 2 saat |

---

### 5.4 `Senaryo1-5HedefOdemeMotoru.cs` — Duplicate Code Yeniden Yazımı 🟠

| Alan | Değer |
|---|---|
| **Toplam satır** | ~1.721 |
| **Sorun** | `KumeBoyuAgirligi()`, `TryPaytableUyumluTekKumeSec()`, `AgirlikliSec()`, `TryTekKumeliIlkGridOlustur()` S1/S2/S4/S5'te neredeyse birebir aynı — ~600 satır duplicate. Bir bug düzeltmesi 4 dosyada yapılmak zorunda. |
| **Ne yapılmalı** | `HedefOdemeMotorBase` static class, ortak metodlar burada. S1-S5 yalnızca `TryTekKumeliIlkGridOlustur()` gibi farklılaşan metodları override eder (S3 BFS, diğerleri Fisher-Yates). |
| **Tahmini süre** | 3 saat |

---

### 5.5 `KasaYoneticisi.cs` içi — `UI_Guncelle()` → `SaveKasalar()` zinciri 🟡

| Alan | Değer |
|---|---|
| **Mevcut satır** | 268 |
| **Sorun** | `UI_Guncelle()` her çağrıldığında `SaveKasalar()` → `PlayerPrefs.Save()` çağrılıyor. Spin başına 3-5 kez senkron disk yazımı. WebGL'de özellikle yavaşlatır. |
| **Ne yapılmalı** | `SaveKasalar()` çağrısını `UI_Guncelle()`'den ayır. Kayıt: yalnızca `OnApplicationQuit`, `OnApplicationPause`, ve en fazla 30 saniyede bir otomatik kayıt (`_sonKayitZamani` field ile). |
| **Tahmini süre** | 45 dakika |

---

### 5.6 `AdminTutorialAkisServisi.cs` — Okunaksız Durum Makinesi 🟡

| Alan | Değer |
|---|---|
| **Mevcut satır** | 605 |
| **Sorun** | Admin tutorial adımları hardcode index'lerle yönetiliyor. `adim == 3` gibi sabitler her yerde. Yeni adım eklendiğinde onlarca index güncellenmeli. |
| **Ne yapılmalı** | `TutorialAdimi` enum + `ScriptableObject TutorialTanimlamalari` ile konfigürasyon dışarı alınmalı. Servis sadece akışı yönetmeli, metinleri bilmemeli. |
| **Tahmini süre** | 3 saat |

---

## ÖZET TABLO

| Kategori | Dosya/Grup Sayısı | Tahmini Süre |
|---|---|---|
| 🔀 Birleştirilebilir | 7 grup (15 dosya) | ~3 saat |
| 🗑️ Gereksiz (sil/temizle) | 5 dosya/bölüm | ~1 saat |
| ✂️ Bölünmesi gereken | 8 dosya | ~22 saat |
| ✅ Doğru yerde | 29 dosya | — |
| 🔁 Yeniden yazılmalı | 6 dosya/bölüm | ~17 saat |
| **TOPLAM** | **76 dosya** | **~43 saat** |

---

## ÖNERİLEN İŞ SIRASI

### Faz 1 — Güvenlik & Kritik Bug'lar (4 saat)
1. `AdminGirisDogrulama.cs` güvenlik yeniden yazımı
2. `SenaryoYoneticisi.cs` 3 bug düzeltmesi
3. `KasaYoneticisi.cs` `UI_Guncelle` / `SaveKasalar` ayrımı

### Faz 2 — Temizlik (4 saat)
4. Gereksiz dosyaları sil (`SonSpinListeOlaylari.cs`, `TumTmpSariStilZorlayici.cs`)
5. Dead code metodlarını sil (`GetSpinOdenebilirLimitRaw`, `CarpanAyarlari` 3 metod)
6. Küçük dosyaları birleştir (DonusServisi+UIServisi+SenaryoServisi, vb.)

### Faz 3 — Refactor (35 saat)
7. Senaryo motorları base class birleşimi (3 saat)
8. `OyunYoneticisi.cs` partial class bölünmesi (6 saat)
9. `LogYoneticisi.cs` bölünmesi (3 saat)
10. `AdminPanel.cs` bölünmesi (2 saat)
11. `SimuleEtVeKaydetImpl` Strategy Pattern (8 saat)

---

*Rapor oluşturuldu: 2026-04-20 | Kapsam: 76 .cs dosyası | OyunYoneticisi.cs dahil tüm servis dosyaları analiz edildi*
