# GECE ANALİZ RAPORU — KumarFarkindalikOyunu
**Tarih:** 2026-04-19 | **Analist:** Claude Sonnet 4.6 | **Kapsam:** Tüm Assets/Scripts/

---

## BÖLÜM 1: KOD KALİTESİ VE TEKNİK BORÇ

### 1.1 Dosya Boyutları ve God Object

| Dosya | Satır | Durum |
|---|---|---|
| `Assets/Scripts/OyunYoneticisi.cs` | 7.894 | 🔴 GOD OBJECT |
| `Assets/Scripts/LogYoneticisi.cs` | 1.788 | 🟡 Büyük |
| `Assets/Scripts/SenaryoYoneticisi.cs` | 1.061 | 🟡 Büyük |
| `Assets/Scripts/AdminPanel.cs` | 993 | 🟡 Büyük |
| `Assets/Scripts/Services/CokmeAkisServisi.cs` | 554 | 🟢 Kabul edilebilir |
| `Assets/Scripts/Services/DonusAkisServisi.cs` | 451 | 🟢 Kabul edilebilir |
| `Assets/Scripts/Services/IzgaraServisi.cs` | 737 | 🟡 Büyük |
| `Assets/Scripts/TumbleAyarlari.cs` | 451 | 🟢 Kabul edilebilir |

**`OyunYoneticisi.cs` — God Object Kanıtı:**
- 13 arayüz birden implement eder (`IIzgaraBaslatmaBaglami`, `IDonusAkisBaglami`, `ICokmeAkisBaglami`, `IZorlukBaglami`, `ICarpanYerlestirmeBaglami`, `ISenaryoServisi`, vb.) — OyunYoneticisi.cs:1
- 7.894 satırda yüzlerce alan (`public` ve `private` karışık), 5 farklı senaryo için ayrı konstrukte fonksiyonu (Senaryo1..5), bonus/spin döngüleri, UI kodu, ses kodu, sahne yönetimi birlikte
- Cyclomatic karmaşıklığı ölçülmese de `SimuleEtVeKaydetImpl` tek başına ~420 satır (OyunYoneticisi.cs:6343-6800 arası) — tek bir metotta kazanç hesaplama, senaryo seçimi, reroll döngüsü, fallback mantığı

### 1.2 Sihirli Sayılar (Kanıtlı)

| Dosya:Satır | Değer | Açıklama |
|---|---|---|
| `EkonomiServisi.cs:144` | `20000` | Sabit bakiye yükleme miktarı |
| `EkonomiServisi.cs:82` | `_bakiyeYuklemeKalanHak = 2` | Maksimum yükleme hakkı |
| `AdminPanel.cs:10` | `"1234"` | Admin şifresi hardcode |
| `AdminGirisDogrulama.cs:9` | `"admin"` | Kullanıcı adı hardcode |
| `AdminGirisDogrulama.cs:10` | `"admin"` | Şifre hardcode |
| `PlayerProfile.cs:33` | `balance = 20000` | Başlangıç bakiyesi hardcode |
| `OyunYoneticisi.cs:6026` | `bombDegeri = 100` | S4 bomba değeri (const olarak lokal tanımlı) |
| `OyunYoneticisi.cs:6274` | `bombDegeri = 500` | S5 bomba değeri (const olarak lokal tanımlı) |
| `OyunYoneticisi.cs:6027-6028` | `bombMinNihai=5000, bombMaxNihai=7000` | S4 bomba ödeme bandı hardcode |
| `OyunYoneticisi.cs:5113-5114` | `s5WinMin = bahis*2, s5WinMax = bahis*5/2` | S5 kazanç bant oranı |
| `OyunYoneticisi.cs:5199` | `s5KayipMin = bahis/4, s5KayipMax = bahis/2` | S5 kayıp bant oranı |
| `SenaryoYoneticisi.cs:~45` | `gecis1_spin=200, gecis2_spin=300` | Aşama geçiş eşikleri public field |
| `KasaYoneticisi.cs:64` | `anaKasaGorselKapasiteTL = 100000` | Görsel kapasite hardcode |
| `KasaYoneticisi.cs:65` | `odulHavuzuGorselKapasiteTL = 50000` | Görsel kapasite hardcode |

### 1.3 Ölü Kod (Dead Code)

**`BombEfektServisi.cs:22-53` — `BeyzFlashEnum` metodu hiç çağrılmıyor:**
```csharp
// BombEfektServisi.cs:13-19
public void BombEfektBaslat(MonoBehaviour calistirici, int carpanDegeri)
{
    if (calistirici == null || carpanDegeri < 100) return;
    if (_shakeCoroutine != null) calistirici.StopCoroutine(_shakeCoroutine);
    _shakeCoroutine = calistirici.StartCoroutine(KameraShakeEnum()); // Sadece shake başlatıyor
    // _flashCoroutine = calistirici.StartCoroutine(BeyzFlashEnum(calistirici)); // ÇAĞRILMIYOR!
}
```
`_flashCoroutine` alanı tanımlanmış (satır 11) ama hiçbir zaman başlatılmıyor. Yani 100x+ çarpan düşüşünde beyaz ekran flash efekti **hiç oynanmıyor**, sadece kamera sarsıntısı çalışıyor.

**`CarpanAyarlari.cs` — Kendi çarpan mantığı kullanılmıyor:**
`CarpanAyarlari.cs` içinde `CarpanlariDoluGriddeUygula()`, `CarpanUretVeBirik()`, `SpinBasindaSifirla()` metotları var (satır 171-286) ama oyun akışı bunları çağırmıyor; bunun yerine `CarpanServisi` ve `CarpanYerlestirmeServisi` kullanılıyor. Bu metotlar gereksiz karmaşıklık yaratıyor.

**`OdemeServisi.cs:49` — `GetSpinOdenebilirLimitRaw()` her zaman `int.MaxValue` döndürüyor:**
```csharp
public int GetSpinOdenebilirLimitRaw()
{
    return int.MaxValue; // Havuz %10 hesabı kaldırıldı ama metod kaldı
}
```
Bu metodu çağıran kod `OyunYoneticisi.cs:4343` içinde hâlâ var ama sonucu `int.MaxValue` olduğundan hiçbir etki yapmıyor.

### 1.4 Bağlantı (Coupling) Sorunları

**`CarpanAyarlari.cs:131` — Doğrudan OyunYoneticisi referansı:**
```csharp
public OyunYoneticisi oyunYoneticisi; // Public alan!
```
`CarpanAyarlari.ZorlaCarpan()` metodu (satır 136-155) OyunYoneticisi'nin 8 farklı `public` alanını doğrudan setValue yapıyor:
```csharp
oyunYoneticisi.zorlaSiradakiCarpan = deger;
oyunYoneticisi.carpanUretimiAktif = CarpanUretimiAktif;
oyunYoneticisi.carpanSadeceBonus = CarpanSadeceBonus;
// ... 5 alan daha
```

**`AdminPanel.cs:198` — `OyunSirasindaAdminKilitlenmeli()` hardcoded false:**
```csharp
private bool OyunSirasindaAdminKilitlenmeli() => false; // AdminPanel.cs:198
```
Admin panel **oyun sırasında asla kilitlenmiyor** — spin dönerken, bonus oynanırken admin panel açık kalabiliyor.

### 1.5 Duplicate State

`BonusBudgetAktif`, `BonusBudgetHavuzOran`, `BonusBudgetMinTL`, `BonusBudgetMaxTL`, `BonusMaxOdemeHavuzOrani` alanları hem `BonusAyarlari.cs` (satır 18-22) hem de `KasaYoneticisi.cs` (satır 77-81) içinde tanımlı. OyunYoneticisi hangi kaynağı kullandığını dışarıdan anlamak zor.

---

## BÖLÜM 2: SENARYO SİSTEMİ DERİN ANALİZ

### 2.1 Senaryo Kuralları ve Kod Yolu

**Admin Senaryo 1 (Kazanç Bandı: 4x-5x bahis):**
- `AdminSenaryo1SpinPolitikasi.AdminOdemeEfektifBandiniUygula()` (AdminSenaryoSpinPolitikalari.cs:10-17): `efektifMin = b*4, efektifMax = b*5`
- Hedef ödeme seçimi: `Senaryo1HedefOdemeAkisi.HedefNihaiOdemeSec()` (OyunYoneticisi.cs:6397)
- Konstrukte döngüsü: `Senaryo1PaytableKonstrukteHedefSpinDene()` (OyunYoneticisi.cs:4956), iki-tumble veya tek-tumble seçeneği
- Fallback: 2500 reroll (OyunYoneticisi.cs:6385), sonra `Senaryo1FallbackKazanciniBandIcineZorla()` (OyunYoneticisi.cs:6782)

**Admin Senaryo 2 (K-KY-K-KY-K döngüsü):**
- Bant: Kazanç için `efektifMin = b*3, efektifMax = b*8`; Kayıp için `efektifMin = max(0, b-100), efektifMax = max(0, b-10)` (AdminSenaryoSpinPolitikalari.cs:32-45)
- `Senaryo2BeklenenKazancMi()` ile döngü durumu okunuyor
- Kayıp konstrukte: `Senaryo2KayipKonstrukteHedefSpinDene()` — en ucuz sembolden minCluster adet

**Admin Senaryo 3 (KY-K-KY-K-KY döngüsü):**
- Kazanç bant: `bahis+100..bahis+600` (sabit TL fark) (OyunYoneticisi.cs:5563)
- S3 kayıp: `_senaryo3DonguIndex == 4` ise mutlaka sıfır; aksi durumda %20 sıfır şansı (OyunYoneticisi.cs:5767)
- Yüksek ödeme kayıp: `Senaryo3HedefOdemeMotoru.TryYuksekPayKayipGridOlustur()` 

**Admin Senaryo 4 (KY→K→BOMB_100x, 3-spin döngüsü):**
- `Senaryo4DonguSpinTipi()` ile sıra: Kayıp→Kazanç→Bomb
- Bomba bant: `bombMinNihai=5000, bombMaxNihai=7000` (OyunYoneticisi.cs:6027-6028) — **bahis bağımsız sabit!** 200 TL bahiste de, 50 TL bahiste de aynı aralık
- `ForceCarpaniIlkGriddeGuvenliYerlestir(100)` (OyunYoneticisi.cs:6046) ile 100x çarpan yerleştirilir

**Admin Senaryo 5 (K→KY→BOMB_500x, 3-spin döngüsü):**
- Kazanç bant: `2x..2.5x bahis` (OyunYoneticisi.cs:5113-5114)
- Kayıp bant: `0.25x..0.5x bahis` (OyunYoneticisi.cs:5199-5200)
- 500x bomba: `Senaryo5BombKonstrukteHedefSpinDene()`, bomba değeri hardcoded `const int bombDegeri = 500` (OyunYoneticisi.cs:6274)
- S5 bonus limiti: `_senaryo5BonusCuziLimitAktif` aktifse bonus ödeme `bahis/5`'e kısıtlanıyor (OyunYoneticisi.cs:6359)

### 2.2 Bypass Riskleri

**Senaryo Bypass 1 — Konstrukte fallback'te bant gözetilmez:**
`SimuleEtVeKaydetImpl()` reroll döngüsü bittiğinde (`sonKayit == null`) son deneme kaydı (`sonDenemeKayit`) direkt döndürülüyor (OyunYoneticisi.cs:6755-6799). Bu fallback'te `SenaryoOdemeBandinaUygun = false` olabilir — senaryo 2/3 üst üste döngü ilerletilmiyor (`NormalSpinAkisi` kontrol ediyor), ama ödeme yine de yapılıyor.

**Senaryo Bypass 2 — Admin panel oyun sırasında açık:**
`OyunSirasindaAdminKilitlenmeli()` her zaman `false` döndürüyor (AdminPanel.cs:198). Spin dönerken admin senaryo değiştirilebilir; `_senaryoPresetAktif` sonraki spin başlamadan önce değişirse, hazırlanmış `_oncedenHesaplananSpinKaydi` geçersiz olabilir.

**Senaryo Bypass 3 — SenaryoYoneticisi aşama eşikleri public:**
`gecis1_spin`, `gecis2_spin` vb. alanlar public olduğundan Unity Inspector veya kod üzerinden doğrudan değiştirilebilir. Pedagojik senaryo hızlandırılabilir veya atlatılabilir.

### 2.3 State Temizliği Sorunları

**S5 popup polling (Update'de):**
`_senaryo5BombSonrasiPopupBekliyor` bayrağı Update() içinde her frame kontrol ediliyor (OyunYoneticisi.cs:~3352). Eğer popup onay fonksiyonu gecikirse (ağ lag, frame drop) bayrak temizlenmeden sonraki spin başlayabilir.

**Senaryo döngü index sıfırlanması:**
S4/S5 döngü indexleri (`_senaryo4DonguIndex`, `_senaryo5DonguIndex`) PlayerPrefs'e kaydedilmez. Uygulama kapanıp açıldığında döngü sıfırdan başlar — bir döngü ortasında kapanırsa tutarsız durum.

**`_senaryo1KonstrukteSimAktif` flag:**
`try/finally` bloğu ile temizleniyor (OyunYoneticisi.cs:5047-5137) — Exception güvenliği iyi. Ancak reroll döngüsünün tam ortasında başka bir senaryo kontrolü tetiklenirse (`senaryo2KonstrukteZorunlu` vb.) çakışma olabilir.

---

## BÖLÜM 3: ÖDEME MATEMATİĞİ DOĞRULAMA

### 3.1 Paytable Değerleri

```
PayTable_8_9  (küme boyutu 8-9):
  İndeks: 0     1     2     3     4     5     6     7     8(scatter)
  Değer:  0.2x  0.3x  0.4x  0.5x  0.6x  0.8x  1.0x  1.5x  0.0x

PayTable_10_11 (küme boyutu 10-11):
  İndeks: 0     1     2     3     4     5     6     7     8(scatter)
  Değer:  0.5x  0.6x  0.8x  1.0x  1.5x  2.0x  3.0x  5.0x  0.0x

PayTable_12Plus (küme boyutu 12+):
  İndeks: 0     1     2     3     4     5     6     7     8(scatter)
  Değer:  1.0x  1.5x  2.0x  2.5x  3.0x  5.0x  10.0x 25.0x 0.0x
```

**Kazanç Formülü:** `kazanc = küme_boyutu × payTableValue[sembolIndex] × bahis`
- Örnek: 200 TL bahis, 10 adet indeks-7 sembol → `10 × 5.0 × 200 = 10.000 TL`
- Tüm tumble adımları toplanır → `spinKazancHam`
- Çarpan uygulanır: `nihaiOdeme = spinKazancHam × toplamCarpan`

### 3.2 Dikkat Çeken Matematik Sorunları

**Scatter sembolü ödeme sıfır (beklenen davranış):**
Tüm paytable'larda indeks 8 = scatter = 0.0x. Scatter patlasa da ödeme yapmıyor — bu bilinçli tasarım ama `TumbleAyarlari.CalculateWinWithOwnPayTable()` scatter içeren kümeleri hesaba katmıyor mu diye kontrol edilmeli.

**S4 Bomb bant bahis bağımsız:**
```csharp
// OyunYoneticisi.cs:6027-6028
const int bombMinNihai = 5000;
const int bombMaxNihai = 7000;
```
50 TL bahiste 5.000-7.000 TL ödeme = 100x-140x kazanç. 200 TL bahiste = 25x-35x kazanç. Tutarsız oranlar oyuncuya bahis büyüklüğünden bağımsız büyük ödeme algısı verebilir.

**S5 kayıp bant dar aralık:**
`s5KayipMin = bahis/4, s5KayipMax = bahis/2` — 200 TL bahiste sadece 50-100 TL aralık. `Senaryo5HedefOdemeMotoru.TryRangeliKayipGridOlustur()` bu dar aralığa uymak zorunda; başarısız olursa null döner ve normal reroll'a düşer (senaryo bant garantisi bozulur).

**`OdemeServisi.PayFromHavuz()` sıfır dönebilir:**
`KasaYoneticisi.OdemeYap_OdulHavuzundan()` (satır 159-189): `odulHavuzuTL <= 0` ise 0 döner. `havuzYetmezseOdemeSifirla = false` varsayılan — "ne varsa onu öde" modu. Havuz tükenince oyuncu 0 ödeme alır ama senaryo döngüsü ilerler; kayıt ile gerçek ödeme uyuşmuyor.

**Bonus kalan bütçe çift hesap:**
```csharp
// OyunYoneticisi.cs:4846
long kalan = (long)cap - (long)bonusKazanc; // bonusKazanc = ödenen toplam
```
`bonusKazanc` içine zorla çarpan miktarları da ekleniyor (satır 4395). Sonraki spin limiti düşüyor. Zorla çarpan çok büyükse (`500x × büyük kazanç`) bütçe hızla tükeniyor ve kalan bonus spinler 0 ödüyor.

### 3.3 Paytable ile Simülasyon Tutarlılığı

`SpinKaydiHamPaytableIleUyumluMu()` metodu (OyunYoneticisi.cs:~5155) her konstrukte sonrası çağrılıyor. Bu kontrol tumble adımlarındaki `TurKazanci` değerlerinin paytable ile örtüşüp örtüşmediğini doğruluyor — bu iyi bir güvence. Ancak yalnızca ham kazanç kontrol ediliyor; çarpan uygulanmış nihai ödeme `_minOdemeTL.._maxOdemeTL` bandı dışına çıkabilir (tolerans logic `OdemeModelineUygunMu()` içinde).

---

## BÖLÜM 4: PERFORMANS PROFİLİ

### 4.1 Update() Maliyeti

`OyunYoneticisi.Update()` (OyunYoneticisi.cs:~3840-3914) her frame şunları çağırıyor:
1. `SenaryoPresetUIHazirlaGerekirse()` — `_senaryoPresetHazirlandi` flag ile kırpılıyor ama contdition check her frame
2. `SenaryoPedagojikOdemeVeZorlaKilidiGuncelle()` — her frame bakiye kontrol
3. `BahisGorselKilidiniUygula()` — her frame `RectTransform` set işlemleri (satır 3900-3914)
4. Input kontrolleri (Input.GetKeyDown, fare tıklamaları)
5. `_senaryo5BombSonrasiPopupBekliyor` polling

`AdminPanel.Update()` (AdminPanel.cs) içinde de 3 metot her frame çağrılıyor: `SenaryoPresetUIHazirlaGerekirse`, `SenaryoPedagojikOdemeVeZorlaKilidiGuncelle`, `BahisGorselKilidiniUygula` — bunlar `_senaryoPresetHazirlandi` true olana kadar her frame çalışıyor.

### 4.2 Senkron PlayerPrefs.Save() Çağrıları

**`KasaYoneticisi.UI_Guncelle()` her çağrıda senkron yazıyor:**
```csharp
// KasaYoneticisi.cs:250
public void UI_Guncelle()
{
    // UI güncellemesi...
    SaveKasalar(); // Her UI güncellemesinde disk yazımı!
}
// SaveKasalar() → PlayerPrefs.SetString(...) + PlayerPrefs.Save()
```
`ParaGirisi_BolVeEkle()` (her bahis girişinde) ve `OdemeYap_OdulHavuzundan()` (her ödeme sonunda) → `UI_Guncelle()` → `PlayerPrefs.Save()` zinciri. Bir spin içinde 2-5 kez senkron disk yazımı.

`EkonomiServisi.EkonomiSenkronizeEt()` de her çağrıda `PlayerPrefs.Save()` yapıyor.

`GameManager.SaveProfiles()` her bakiye/bahis değişiminde çağrılıyor.

### 4.3 Pahalı Unity Sorguları

**`CarpanAyarlari.Start()` — `FindObjectsOfType<Button>(true)` (satır 74):**
```csharp
Button[] butonlar = FindObjectsOfType<Button>(true); // Sahnedeki TÜM butonları tarıyor
foreach (Button b in butonlar) { /* isim karşılaştırması */ }
```
Her sahne yüklendiğinde tüm Button bileşenleri taranıyor.

**`SesKaynaklariniHazirla()` — `GameObject.Find()` (OyunYoneticisi.cs:4202-4208):**
```csharp
tumbleSfxSource = GameObject.Find("TumbleSfxSource")?.GetComponent<AudioSource>()
    ?? GameObject.Find("SfxSource")?.GetComponent<AudioSource>()
    ?? FindFirstObjectByType<AudioSource>(FindObjectsInactive.Include);
```
Her ses çalınmasında (her spin sonucu popup'ta) bu zincir çalışıyor. `tumbleSfxSource != null` kontrolü önünde ama null kalırsa her seferinde O(n) sahne taraması.

**`BonusMiktariYazisiniGuncelle()` — `GetComponentsInChildren<TMP_Text>(true)` (OyunYoneticisi.cs:4284):**
Her bonus satın alma onay paneli açılışında tüm child textleri tarıyor.

### 4.4 Animasyon İçi Yüksek Frekans Operasyonlar

`CokmeAkisServisi.DokulmeEfektiIcinGecikmeVeSureAta()` (satır 491-518) her hücre için `UnityEngine.Random.Range()` çağırıyor. 30 hücre × her tumble refill = sık çağrı ama kabul edilebilir.

`AnimasyonServisi.AnimateGridDropIn()` coroutine içinde her frame tüm hücrelerin pozisyon ve alfa değerini güncelliyor — 30 hücre × 60fps × animasyon süresi kadar işlem. Kabul edilebilir ama profil edilmeli.

**String Concat (WebGL önemli):**
`DonusAkisServisi.NormalSpinAkisi()` ve `BonusDongusu()` içinde Debug.Log ifadelerinde `$"..."` interpolasyon ile `System.Text.StringBuilder` karışık kullanım (satır 227-231, 410-418). Geliştirme derlemelerinde gideri ihmal edilebilir; Release derlemede `UNITY_EDITOR` guard'ı var bazılarında ama hepsinde değil.

---

## BÖLÜM 5: GÜVENLİK DERİN TARAMA

### 5.1 KRİTİK: Giriş Doğrulama Açığı

**`AdminGirisDogrulama.cs:136` — Kullanıcı Adı DOĞRULANMIYOR:**
```csharp
// AdminGirisDogrulama.cs:9-10
public string BeklenenKullaniciAdi = "admin"; // KULLANILMIYOR!
public string BeklenenSifre = "admin";        // Tek kontrol bu

// DogrulaVeDevamEt() içinde (satır ~136):
bool dogru = string.Equals(sifre, BeklenenSifre, StringComparison.OrdinalIgnoreCase);
// Kullanıcı adı hiç karşılaştırılmıyor!
```
Giriş ekranında kullanıcı adı alanı "admin" ile doldurulsa da (`satır 100`) hiçbir zaman kontrol edilmiyor. Kullanıcı adı alanına herhangi bir şey yazılabilir — yalnızca şifre "admin" olmalı. Bu, admin paneline erişim bariyerini yarıya indiriyor.

### 5.2 KRİTİK: Hardcoded Kimlik Bilgileri WebGL Binary'de Görünür

**`AdminPanel.cs:10` — `public string adminSifre = "1234"`:**
```csharp
public string adminSifre = "1234"; // Unity Inspector'da görünür
```
Bu değer Unity Inspector'da görünür, WebGL build'inde IL2CPP binary içinde plaintext olarak kalabilir. `strings` komutu veya IL2CPP decompiler ile kolayca çıkarılabilir.

**`AdminGirisDogrulama.cs:9-10` — `BeklenenSifre = "admin"`:**
Aynı sorun: WebGL binary'de görünür.

### 5.3 YÜKSEK: WebGL'de File.IO Crash

**`OyunYoneticisi.cs` — `DosyadanSpriteYukle()` metodu `File.ReadAllBytes` kullanıyor:**
`using System.IO;` (OyunYoneticisi.cs:4) eklenmiş. `DosyadanSpriteYukle()` metodu (satır ~3218):
```csharp
byte[] data = File.ReadAllBytes(yol); // WebGL'de FileNotFoundException fırlatır!
```
WebGL'de `System.IO.File` API'leri çalışmaz; bu metot çağrıldığında oyun crash olur. `GameManager.cs` bunu `#if !UNITY_WEBGL` guard'ı ile koruyor ama `OyunYoneticisi` içindeki bu kullanım korunmuyor.

### 5.4 YÜKSEK: PlayerPrefs Manipülasyonu

WebGL'de PlayerPrefs localStorage'a yazılır. Kullanıcı tarayıcı geliştirici konsolundan `localStorage` içeriğini görüp değiştirebilir:
- `PP_ANA_KASA_TL` — ana kasa tutarı
- `PP_ODUL_HAVUZU_TL` — ödül havuzu tutarı
- `PP_BAKIYE` — oyuncu bakiyesi
- `PP_BAHIS` — bahis miktarı
- `PP_SENARYO_MEVCUT_ASAMA_{playerId}` — senaryo aşaması
- `PP_WEBGL_PROFILES_JSON_V1` — tüm profil verisi JSON

Hiçbirinde şifreleme veya imzalama yok.

### 5.5 YÜKSEK: Admin Panel Oyun Sırasında Kilitlenmiyor

```csharp
// AdminPanel.cs:198
private bool OyunSirasindaAdminKilitlenmeli() => false;
```
Bu metot her zaman `false` döndürüyor. Spin çalışırken, bonus oynanırken admin panel açılabilir, senaryo değiştirilebilir, kasa değerleri ayarlanabilir. `spinCalisiyor == true` iken admin işlemleri UygulaAdminSenaryo() gibi metotları tetikleyebilir — bu durumda sonraki spin için hazırlanan `_oncedenHesaplananSpinKaydi` geçersiz kılınıyor ama bayrağı temizlenmiyor.

### 5.6 ORTA: Admin Panel Buton Otomatik Bağlama Güvenliği

```csharp
// CarpanAyarlari.cs:74-120
Button[] butonlar = FindObjectsOfType<Button>(true);
foreach (Button b in butonlar) {
    if (adi.Contains("forcex5") && !adi.Contains("forcex50")) {
        b.onClick.AddListener(() => ZorlaCarpan(100)); // İsim eşleştirmesi ile
    }
}
```
Sahnede "forcex5" içeren herhangi bir buton otomatik olarak `ZorlaCarpan(100)` ile bağlanıyor. Eğer bir UI elemanına bu isim yanlışlıkla verilirse, masum bir buton admin yetkisi kazanıyor.

### 5.7 DÜŞÜK: Hassas Veri Debug.Log

`DonusAkisServisi.cs:262` içinde:
```csharp
Debug.Log($"🧪 BonusKontrol: ScatterSay(ilk)={scIlk} ScatterSay(simdi)={scSimdi} ... | scatter index={scatterIdx}");
```
WebGL'de Unity Debug.Log tarayıcı konsoluna yazar. Oyunun iç mantığı (scatter threshold, senaryo durumu, ödeme limitleri) konsoldan okunabilir.

---

## BÖLÜM 6: UI/UX DEFECT ANALİZİ

### 6.1 KRİTİK: Beyaz Ekran Flash Efekti Çalışmıyor

```csharp
// BombEfektServisi.cs:13-19
public void BombEfektBaslat(MonoBehaviour calistirici, int carpanDegeri)
{
    if (calistirici == null || carpanDegeri < 100) return;
    if (_shakeCoroutine != null) calistirici.StopCoroutine(_shakeCoroutine);
    _shakeCoroutine = calistirici.StartCoroutine(KameraShakeEnum());
    // BeyzFlashEnum ÇAĞRILMIYOR — flash hiç görünmüyor
}
```
100x+ çarpan düşüşünde beyaz ekran flash (`BeyzFlashEnum`, satır 22-53) hiç oynatılmıyor. Yalnızca kamera sarsıntısı (`KameraShakeEnum`) oynatılıyor. Bu büyük bir atmosfer kaybı.

### 6.2 YÜKSEK: Race Condition — Otomatik Spin ve Bakiye Kontrolü

```csharp
// OyunYoneticisi.cs:4540-4558
private IEnumerator OtomatikSpinDongusu()
{
    while (_otomatikSpinKalan > 0 && !bonusAktif && ... && _ekonomiServisi.Bakiye >= _ekonomiServisi.Bahis)
    {
        SetGlobalTiklamaKilidi(true);
        yield return BirSpinHazirlaVeAt(); // Spin burada tamamlanır
        SetGlobalTiklamaKilidi(false);
        _otomatikSpinKalan--;
        // while koşuluna dön: bakiye kontrolü yeniden yapılır
    }
}
```
Kontrol güvenli. Ancak `BirSpinHazirlaVeAt()` dönerken `spinCalisiyor = false` oluyor ama `_otomatikSpinKalan--` sonrasında `while` koşulu tekrar değerlendiriliyor — bu arada bonus tetiklenmiş olabilir (`bonusAktif = true` setleniyor). Döngü `bonusAktif` kontrolü ile doğru çıkıyor.

### 6.3 ORTA: GeciciTiklamaKilidi Panel DontDestroyOnLoad

```csharp
// OyunYoneticisi.cs:4460
DontDestroyOnLoad(_geciciTiklamaKilidiPanel);
```
Bu panel sahne geçişlerinde korunuyor. Sahne geçişi sırasında spin bekleme aktifse panel ekranda kalıp tüm tıklamaları engelleyebilir. `UygulaGlobalTiklamaKilidiGorunurlugu()` içinde `aktif = false` hardcoded (satır 4473) — panel hiçbir zaman gizlenmiyor! Bu satır kasıtlı mı (özellik devre dışı) yoksa yanlışlıkla mı konuldu?

### 6.4 ORTA: S5 Bomb Sonrası Popup Polling

```csharp
// OyunYoneticisi.cs içinde Update() (~satır 3352)
if (_senaryo5BombSonrasiPopupBekliyor)
{
    // popup beklenmedik kapatılmışsa devam et
}
```
Event-driven yerine polling mimarisi. Popup olmayan durumlarda her frame gereksiz kontrol.

### 6.5 DÜŞÜK: Türkçe Karakter Endişeleri

`KasaYoneticisi.cs:96`: `private readonly CultureInfo tr = new CultureInfo("tr-TR");`
Bu doğru kullanım. Ancak bazı string karşılaştırmalarında (CarpanAyarlari.cs:78: `adi.Contains("forcex5")`) locale bağımsız `ToLower()` kullanılıyor — bu "ı" (dotless i) karakterini hatalı işleyebilir. Buton isimlerinde Türkçe karakter yoksa sorun yok ama dikkat edilmeli.

---

## BÖLÜM 7: WEBGL UYUMLULUK DERİN TARAMA

### 7.1 KRİTİK: System.IO.File Korumasız Kullanım

```csharp
// OyunYoneticisi.cs:4 (using bildirimi)
using System.IO;
```
`DosyadanSpriteYukle()` metodu (satır ~3218) içinde:
```csharp
byte[] data = File.ReadAllBytes(yol); // WebGL'de CRASH
```
`GameManager.cs:2`'de `using System.IO;` var ama `#if !UNITY_WEBGL` guard'larıyla dosya sistemi erişimi korunuyor. `OyunYoneticisi.cs`'te bu koruma yok. Bu metot yalnızca admin sahnesinde mi çağrılıyor soruşturulmalı.

### 7.2 YÜKSEK: PlayerPrefs Boyut Limiti

WebGL'de PlayerPrefs localStorage'ı kullanır. Tarayıcılar genellikle 5-10MB limit koyar.

`PP_WEBGL_PROFILES_JSON_V1` anahtarına tüm profil listesi JSON olarak kaydediliyor (`GameManager.cs`). Çok sayıda profil veya büyük log geçmişi (SenaryoOlayKaydi listesi) limitin aşılmasına yol açabilir. Aşıldığında `PlayerPrefs.Save()` sessizce başarısız olur.

### 7.3 ORTA: Threading Güvenligi

Unity WebGL single-thread çalışır. Projede `System.Threading` kullanımı gözlemlenmedi. Coroutine'ler düzgün kullanılıyor. Bir risk yok.

### 7.4 ORTA: Reflection Kullanımı

Projede yaygın reflection gözlemlenmedi. `typeof()` ve `is` operatörleri kullanılıyor ama WebGL'de çalışan IL2CPP bunları strip etmez.

### 7.5 DÜŞÜK: Application.persistentDataPath

`GameManager.cs` içinde:
```csharp
// GameManager.cs:~30
ProfilDosyaYolu = Path.Combine(Application.persistentDataPath, "profiles.json");
```
`#if UNITY_WEBGL` guard ile bu yol WebGL'de kullanılmıyor, PlayerPrefs'e yönlendiriliyor. Güvenli.

---

## BÖLÜM 8: VERİ AKIŞI TAM HARİTASI

### 8.1 Profil Akışı

```
GirisUI (UI) 
  → GameManager.I.SetActivePlayer(playerId)
    → GameManager._profiles listesi (RAM)
      → [WebGL] PlayerPrefs "PP_WEBGL_PROFILES_JSON_V1" (JSON)
      → [PC/Editor] "profiles.json" dosyası (Application.persistentDataPath)
```

Her `SaveProfiles()` çağrısında tam liste serialize edilip kaydediliyor — artımlı değil, tüm veri her seferinde.

### 8.2 Bakiye Akışı

```
Spin Başlangıcı
  → EkonomiServisi.DeductSpinMaliyeti(spinMaliyeti)
    → EkonomiServisi._bakiye -= spinMaliyeti
    → PlayerPrefs "PP_BAKIYE" (güncelleme)
    → GameManager.ActivePlayer.balance güncelleme
  → OdemeServisi.AddBahisToKasa(spinMaliyeti)
    → KasaYoneticisi.ParaGirisi_BolVeEkle(miktar)
      → anaKasaTL += miktar/2, odulHavuzuTL += miktar/2
      → PlayerPrefs "PP_ANA_KASA_TL", "PP_ODUL_HAVUZU_TL" (KasaYoneticisi.UI_Guncelle)

Ödeme
  → OdemeServisi.PayFromHavuz(istenenTL)
    → KasaYoneticisi.OdemeYap_OdulHavuzundan(miktar)
      → odulHavuzuTL -= miktar
      → PlayerPrefs güncelleme
  → EkonomiServisi.AddWinnings(odenen, bahis)
    → EkonomiServisi._bakiye += odenen
    → PlayerPrefs güncelleme
    → GameManager.ActivePlayer.balance güncelleme
```

### 8.3 Senaryo Durumu Akışı

```
SenaryoYoneticisi.I (Singleton, DontDestroyOnLoad)
  → mevcutAsama (enum)
  → toplamSpin, consecutivePayCount, forcedNoPayKalan, vb.
  → Aşama geçişlerinde PlayerPrefs "PP_SENARYO_MEVCUT_ASAMA_{playerId}" kaydediliyor

OyunYoneticisi._senaryoPresetAktif (bool)
  → AdminPanel.cs senaryo preset seçiminden geliyor
  → PlayerPrefs'e kaydedilmiyor — sahne yenilenince sıfırlanır
```

### 8.4 Log Sistemi Akışı

```
SenaryoOlayKaydi (enum + string mesaj) listesi
  → SenaryoYoneticisi.I.LogEkle(tip, mesaj)
    → _olayListesi'ne eklenir (RAM)
    → LogServisi / LogYoneticisi sahnede görselleştirme
    → PlayerPrefs üzerinden kalıcı depolama (büyük boyutlarda risk)
```

### 8.5 GameManager → OyunYoneticisi → Servisler Bağlantısı

```
GameManager.I (Singleton)
  ↓ ActivePlayer, LoadScene()
OyunYoneticisi : MonoBehaviour (13 interface)
  ↓ Bootstrap
  ├── EkonomiServisi (bakiye, bahis yönetimi)
  ├── KasaYoneticisi (FindObjectOfType ile)
  ├── OdemeServisi (stateless, delegate tabanlı)
  ├── IzgaraServisi (grid, scatter, sembol)
  ├── TumbleServisi (küme hesaplama, FindClustersToRemove)
  ├── CarpanServisi (çarpan state)
  ├── CarpanYerlestirmeServisi (grid yerleştirme)
  ├── ZorlukServisi (bias hesaplama)
  ├── CokmeAkisServisi (düşme animasyonu + grid refill)
  ├── DonusAkisServisi (NormalSpinAkisi, BonusDongusu)
  ├── AnimasyonServisi (görsel animasyonlar)
  ├── UIServisi (UI güncelleme)
  └── SenaryoServisi (bonus bütçe, senaryo delegasyonu)
```

---

## BÖLÜM 9: TEST EDİLEMEZLİK ANALİZİ

### 9.1 Test Edilemeyen Yapılar

**OyunYoneticisi — MonoBehaviour + God Object:**
Tüm iş mantığı tek bir MonoBehaviour içinde. Unit test için Unity runtime gerekiyor. NUnit/MSTest ile izole test imkansız.

**`UnityEngine.Random` doğrudan kullanım:**
```csharp
// IzgaraServisi.cs:558
return adaylar[UnityEngine.Random.Range(0, adaylar.Count)]; // Seed kontrolü yok
```
`SimuleEtVeKaydetImpl()` içinde 2500 reroll ile bir sonuç arıyor — deterministik test için Random.State kayıt/restore gerekiyor ama kullanılmıyor.

**Singleton pattern:**
`SenaryoYoneticisi.I` ve `GameManager.I` test izolasyonunu engelliyor. Test suite'te birden fazla test aynı singleton'u paylaşırsa kirli state.

**Coroutine içi iş mantığı:**
`DonusAkisServisi.BonusDongusu()` (satır 312-449) bir coroutine içinde bonus hesaplama, ödeme, senaryo ilerletme yapıyor. Coroutine'lerin unit testi Unity Test Runner olmadan imkansız.

**Private konstrukte metotları:**
`Senaryo1PaytableKonstrukteHedefSpinDene()`, `Senaryo4BombKonstrukteHedefSpinDene()` vb. hepsi OyunYoneticisi private metotları — dışarıdan test edilemiyor.

### 9.2 Test Edilebilirlik Oranı

| Bileşen | Test Edilebilirlik |
|---|---|
| TumbleAyarlari.CalculateWinWithOwnPayTable | 🟢 Bağımsız, pure logic |
| IzgaraServisi.RandomNonScatterSymbol | 🟡 Random bağımlı |
| CarpanServisi (state metotları) | 🟢 Bağımsız, no Unity dep |
| OdemeServisi | 🟢 Tamamen stateless delegate |
| ZorlukServisi.ZorlukUygula | 🟢 Context mock edilebilir |
| CokmeAkisServisi | 🟡 Context bağımlı |
| OyunYoneticisi | 🔴 Test edilemez |
| SenaryoYoneticisi | 🔴 Singleton, MonoBehaviour |
| DonusAkisServisi | 🟡 Context interface var ama coroutine |

---

## BÖLÜM 10: MİMARİ ÖNERİLER

### 10.1 OyunYoneticisi Partial Class Bölünmesi

7.894 satırlık dosya aşağıdaki partial class'lara bölünebilir:

```
OyunYoneticisi.Core.cs        — Inspector alanları, Awake/Start/Update, servis init
OyunYoneticisi.Spin.cs        — SpinButon, BirSpinHazirlaVeAt, SpinButonImpl
OyunYoneticisi.Bonus.cs       — BaslatBonus, ShowBonusEndMessage, GetBonusRemainingPayableTL
OyunYoneticisi.Senaryo1.cs    — Senaryo1PaytableKonstrukteHedefSpinDene + yardımcılar
OyunYoneticisi.Senaryo2.cs    — Senaryo2 kazanç/kayıp konstrukte
OyunYoneticisi.Senaryo3.cs    — Senaryo3 kazanç/kayıp konstrukte
OyunYoneticisi.Senaryo45.cs   — Senaryo4+5 kazanç/kayıp/bomb konstrukte
OyunYoneticisi.Simulasyon.cs  — SimuleEtVeKaydetImpl (merkez döngü)
OyunYoneticisi.UI.cs          — UI popup'lar, animasyonlar, bakiye akışı
OyunYoneticisi.Admin.cs       — Admin bahis ayarla, senaryo preset UI
```

### 10.2 Eksik Servisler

- **PlayerPrefsServisi:** Tüm key sabitlerini tek yerde toplar, şifreleme hook noktası sağlar
- **SenariyoDurumuDeposu:** Senaryo aşama geçişi state'ini serialize/deserialize eder (PlayerPrefs key yönetimi)
- **AuditLogServisi:** Güvenlik açısından kritik işlemleri (admin girişi, kasa değişimi) kayıt altına alır
- **RandomServisi wrapper:** `UnityEngine.Random` üstünde ince wrapper — test seed'i enjekte edilebilir

### 10.3 Dependency Injection Fırsatları

Servisler halihazırda interface'ler üzerinden bağlanıyor (`IDonusAkisBaglami`, vb.) — bu iyi bir temel. Eksik olan:
- `GameManager.I` ve `SenaryoYoneticisi.I` singleton'larını DI container'a çekme
- `UnityEngine.Random` → `IRandomProvider` abstraction
- `PlayerPrefs` → `IVeriDeposu` abstraction (test'te mock, WebGL'de PlayerPrefs, PC'de dosya)

### 10.4 Event-Driven Göçü

Mevcut durum: OyunYoneticisi'nin pek çok alt sistemi doğrudan çağırıyor.
Öneri: `SpinTamamlandi`, `BonusBasladi`, `BonusBitti`, `AsamaDegisti` event'leri ile ayrıştırma.
`SenaryoYoneticisi.SpinTamamlandi()` zaten çağrılıyor — bu event'e dönüştürülebilir.

---

## BÖLÜM 11: SABAH ÖNCELİK LİSTESİ

### 🔴 KRİTİK (Hemen Düzelt)

**#1 — Giriş Doğrulama Açığı**
- **Dosya:Satır:** `Assets/Scripts/AdminGirisDogrulama.cs:136`
- **Sorun:** `DogrulaVeDevamEt()` yalnızca şifreyi karşılaştırıyor, kullanıcı adını hiç kontrol etmiyor.
- **Çözüm:**
  ```csharp
  bool dogru = string.Equals(kullanici, BeklenenKullaniciAdi, ...) &&
               string.Equals(sifre, BeklenenSifre, ...);
  ```
- **Süre:** 10 dakika

**#2 — WebGL'de File.IO Crash**
- **Dosya:Satır:** `Assets/Scripts/OyunYoneticisi.cs:~3218` (`DosyadanSpriteYukle` metodu)
- **Sorun:** `File.ReadAllBytes(yol)` WebGL'de crash.
- **Çözüm:** `#if !UNITY_WEBGL` guard ekle veya metodu `UnityWebRequest` / `Resources.Load` ile değiştir.
- **Süre:** 30 dakika

**#3 — Admin Panel Oyun Sırasında Kilitlenmiyor**
- **Dosya:Satır:** `Assets/Scripts/AdminPanel.cs:198`
- **Sorun:** `OyunSirasindaAdminKilitlenmeli()` hardcoded `false`.
- **Çözüm:**
  ```csharp
  private bool OyunSirasindaAdminKilitlenmeli() => 
      _oyunYoneticisi != null && (_oyunYoneticisi.spinCalisiyor || _oyunYoneticisi.bonusAktif);
  ```
- **Süre:** 20 dakika

**#4 — Hardcoded Kimlik Bilgileri**
- **Dosya:Satır:** `AdminPanel.cs:10`, `AdminGirisDogrulama.cs:9-10`
- **Sorun:** "1234" ve "admin" plaintext, WebGL binary'de görünür.
- **Çözüm:** Değerleri `ScriptableObject` veya `PlayerPrefs` ile dışarıya taşı, hash karşılaştırması kullan (`System.Security.Cryptography.SHA256`).
- **Süre:** 2 saat

### 🟠 YÜKSEK (Bu Hafta)

**#5 — BombEfektServisi Beyaz Ekran Flash Çalışmıyor**
- **Dosya:Satır:** `Assets/Scripts/Services/BombEfektServisi.cs:13-19`
- **Sorun:** `BombEfektBaslat()` yalnızca shake çalıştırıyor, `BeyzFlashEnum` hiç çağrılmıyor.
- **Çözüm:**
  ```csharp
  public void BombEfektBaslat(MonoBehaviour calistirici, int carpanDegeri)
  {
      if (calistirici == null || carpanDegeri < 100) return;
      if (_flashCoroutine != null) calistirici.StopCoroutine(_flashCoroutine);
      if (_shakeCoroutine != null) calistirici.StopCoroutine(_shakeCoroutine);
      _flashCoroutine = calistirici.StartCoroutine(BeyzFlashEnum(calistirici));
      _shakeCoroutine = calistirici.StartCoroutine(KameraShakeEnum());
  }
  ```
- **Süre:** 5 dakika

**#6 — KasaYoneticisi Her UI Güncellemede Senkron Disk Yazımı**
- **Dosya:Satır:** `Assets/Scripts/KasaYoneticisi.cs:250`
- **Sorun:** `UI_Guncelle()` → `SaveKasalar()` → `PlayerPrefs.Save()` zinciri. Spin başına 3-5 kez senkron yazım.
- **Çözüm:** `SaveKasalar()` çağrısını `UI_Guncelle()` içinden çıkar; `OnApplicationQuit`, `OnApplicationPause` ve en fazla 30 saniyede bir otomatik kayıt kullan.
- **Süre:** 1 saat

**#7 — RandomNonScatterSymbol Scatter Döndürebilir**
- **Dosya:Satır:** `Assets/Scripts/Services/IzgaraServisi.cs:656-661`
- **Sorun:** `totalW <= 0` fallback içinde scatter sembolünün seçilip `(fallback + 1) % n` ile "düzeltilmesi" garanti değil — n=9, scatter=0, fallback=0 olursa: `(0+1)%9 = 1` — tamam. Ama scatter=8 ise ve fallback=8 olursa: `(8+1)%9 = 0` — scatter değil. Mantık doğru görünüyor.
  - Asıl risk `adaylar` listesi boş kaldığında `return adaylar[UnityEngine.Random.Range(0, adaylar.Count)]` IndexOutOfRange fırlatır.
  ```csharp
  // IzgaraServisi.cs:655-661
  if (adaylar.Count == 0)
  {
      int fallback = UnityEngine.Random.Range(0, n);
      if (fallback == _scatterSpriteIndex) fallback = (fallback + 1) % n;
      return fallback; // Güvenli
  }
  return adaylar[UnityEngine.Random.Range(0, adaylar.Count)]; // adaylar.Count > 0 garantili
  ```
  Kodun bu kısmı aslında güvenli. Asıl risk erken return satırı:
  ```csharp
  // IzgaraServisi.cs:546
  if (_sembolSpriteListesi == null || _sembolSpriteListesi.Count <= 1) return 0;
  ```
  `Count == 1` ise index 0 döner — bu scatter olabilir! Sprite listesi scatter dahil 9 elemandan az olursa.
- **Çözüm:** Early return'de `return (0 == _scatterSpriteIndex) ? 1 : 0;` güvenliği ekle.
- **Süre:** 15 dakika

**#8 — CarpanAyarlari.Start() Pahalı FindObjectsOfType**
- **Dosya:Satır:** `Assets/Scripts/CarpanAyarlari.cs:74`
- **Sorun:** `FindObjectsOfType<Button>(true)` her sahne yüklendiğinde tüm sahneyi tarıyor.
- **Çözüm:** Butonları `[SerializeField]` ile Inspector'dan ata; start'taki otomatik bağlama kaldır.
- **Süre:** 30 dakika

**#9 — S4 Bomb Bant Bahis Bağımsız**
- **Dosya:Satır:** `Assets/Scripts/OyunYoneticisi.cs:6027-6028`
- **Sorun:** `bombMinNihai = 5000, bombMaxNihai = 7000` sabit — küçük bahislerde orantısız büyük ödeme.
- **Çözüm:** `int bombMinNihai = bahisB * 25; int bombMaxNihai = bahisB * 35;` ile bahis orantılı yap.
- **Süre:** 10 dakika

### 🟡 ORTA (Bu Sprint)

**#10 — Senaryo Döngü Indexleri Kalıcı Değil**
- **Dosya:Satır:** `Assets/Scripts/OyunYoneticisi.cs` — `_senaryo4DonguIndex`, `_senaryo5DonguIndex`
- **Sorun:** PlayerPrefs'e kaydedilmiyor; uygulama kapanınca döngü sıfırlanıyor — S4/S5 senaryo yarıda bozuluyor.
- **Çözüm:** `OnApplicationQuit/Pause` içinde `PlayerPrefs.SetInt("PP_S4_DONGU_IDX", _senaryo4DonguIndex)` kaydet, `Start/Awake` içinde yükle.
- **Süre:** 30 dakika

**#11 — Admin Senaryo Preset Verisi Kopyalanmış**
- **Dosya:Satır:** `AdminPanel.cs:78-87` ve `OyunYoneticisi.cs:138-145`
- **Sorun:** Senaryo isim/indeks eşleştirmeleri iki ayrı yerde tanımlı; birinde güncelleme diğerini senkronlamıyor.
- **Çözüm:** `ScriptableObject SenaryoPresetTanimlamalari` ile tek kaynak.
- **Süre:** 2 saat

**#12 — `SesKaynaklariniHazirla()` Her Seste GameObject.Find**
- **Dosya:Satır:** `Assets/Scripts/OyunYoneticisi.cs:4202-4208`
- **Sorun:** Her `PlayNormalSpinSonucSesi()` çağrısında `tumbleSfxSource == null` kontrolünden geçen kod `GameObject.Find()` yapıyor. Null kalırsa her ses için O(n) sahne taraması.
- **Çözüm:** Ses kaynağını Awake/Start'ta bir kez çöz; başarısızsa log at ve bırak.
- **Süre:** 20 dakika

**#13 — PlayerPrefs Şifrelemesi Yok (WebGL)**
- **Dosya:Satır:** `GameManager.cs`, `KasaYoneticisi.cs`, `EkonomiServisi.cs`
- **Sorun:** localStorage manipülasyonu ile bakiye, kasa, senaryo durumu değiştirilebilir.
- **Çözüm:** Kritik değerler için checksum/HMAC ekle; değer yüklendiğinde imzayı doğrula.
- **Süre:** 4 saat

### 🟢 DÜŞÜK (Teknik Borç)

**#14 — BonusAyarlari + KasaYoneticisi Duplicate Alanlar**
- **Dosya:Satır:** `BonusAyarlari.cs:18-22`, `KasaYoneticisi.cs:77-81`
- **Sorun:** `BonusBudgetAktif`, `BonusBudgetHavuzOran` vb. her ikisinde de var. OyunYoneticisi hangisinden okuyacağını bilmek zor.
- **Çözüm:** `KasaYoneticisi` üzerinden tek kaynak; `BonusAyarlari` yalnızca spin-level ayarları tutmalı.
- **Süre:** 1 saat

**#15 — Debug.Log İçinde Hassas Oyun Durumu**
- **Dosya:Satır:** `DonusAkisServisi.cs:253`, `OyunYoneticisi.cs:6383` vb.
- **Sorun:** WebGL'de tarayıcı konsolu açık kalırsa scatter threshold, senaryo durumu, ödeme limitleri görünür.
- **Çözüm:** Production derlemesinde `Debug.Log` çıktılarını `#if UNITY_EDITOR || DEVELOPMENT_BUILD` ile sar.
- **Süre:** 2 saat

**#16 — OyunYoneticisi GeciciTiklamaKilidiPanel Mantık Hatası**
- **Dosya:Satır:** `Assets/Scripts/OyunYoneticisi.cs:4472-4475`
- **Sorun:**
  ```csharp
  private void UygulaGlobalTiklamaKilidiGorunurlugu()
  {
      EnsureGlobalTiklamaKilidiPanel();
      bool aktif = false; // Her zaman false! Kilit hiç çalışmıyor
      if (_geciciTiklamaKilidiPanel != null)
          _geciciTiklamaKilidiPanel.SetActive(aktif);
  }
  ```
  Tıklama kilidi özelliği kasıtlı olarak kapatılmış (yorum: "İstek: Oyun akışında tıklama kilidi tamamen devre dışı"). Ama `BaslatGeciciGlobalTiklamaKilidi()`, `SetGlobalTiklamaKilidi()` metotları hâlâ çağrılıyor — boşuna coroutine başlatılıyor.
- **Çözüm:** Özellik gerçekten kapatılacaksa tüm çağrı noktalarını temizle; açılacaksa `aktif` yerine gerçek flag kullan.
- **Süre:** 30 dakika

---

## ÖZET TABLO

| Öncelik | Adet | Tahmini Toplam Süre |
|---|---|---|
| 🔴 KRİTİK | 4 | ~3 saat |
| 🟠 YÜKSEK | 5 | ~5 saat |
| 🟡 ORTA | 4 | ~8 saat |
| 🟢 DÜŞÜK | 3 | ~4 saat |
| **TOPLAM** | **16** | **~20 saat** |

---

*Rapor oluşturuldu: 2026-04-19 | Kapsam: 72 .cs dosyası | OyunYoneticisi.cs satır 1-7894 tam okundu*
