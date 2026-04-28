# Kumar Farkındalık Oyunu – Proje Analizi (A’dan Z’ye)

Bu belge projenin mimarisini, akışları, potansiyel hataları ve geliştirme önerilerini özetler. İstek: titiz inceleme, bug riskleri ve unutulmuş noktaların değerlendirilmesi.

---

## 1. Yapılan Düzeltme: Aşama 4’te Zorluk 6 Görünmesi

**Sorun:** Aşama 4’e gelindiğinde panelde zorluk 6 yazıyordu; spec’e göre Aşama 4 (Bakiye tükenişi) temel zorluk **8** olmalı.

**Sebep:** `AsamaAyariniUygula()` içinde “yükleme sonrası ilk 20 spin” kuralı **tüm aşamalarda** uygulanıyordu: `spinFarkiYukleme <= 20` iken zorluk `Min(zorluk, 6)` yapılıyordu. Aşama 4’te de yükleme sonrası 20 spin içindeyseniz ekranda 6 görünüyordu.

**Çözüm:** Bu kural yalnızca **aşama 1–3** için geçerli olacak şekilde sınırlandı:  
`(int)mevcutAsama <= 3` iken yükleme sonrası 20 spinde zorluk en fazla 6; aşama 4–7’de temel zorluk (8–11) korunuyor.

---

## 2. Mimari Özet

| Bileşen | Sorumluluk |
|--------|------------|
| **OyunYoneticisi** | Tek merkez orkestratör; Inspector referansları, servislerin oluşturulması ve bağlanması, spin/bahis/bonus girişi, ödenebilir limit (normal + senaryo bütçesi). Birden fazla arayüz implement eder (IDonusAkisBaglami, IOyunUIGuncellemeBaglami vb.). Sahne başına bir instance. |
| **SenaryoYoneticisi** | Statik singleton `I`; DontDestroyOnLoad (senaryo sahnesinde veya singleton kazanınca). 7 aşama, geçiş şartları, sayaçlar (toplamSpin, bahisArtirimSayisi, yuklemeSayisi vb.). Aşama zorluğu, UI (aşama/spin/bakiye, MevcutAyarlarMetni), aşama geçiş kontrolü. |
| **GameManager** | Singleton `I`, DontDestroyOnLoad. Profil listesi (profiles.json), ActivePlayer, bahis; SelectOrCreatePlayer, Deposit, Withdraw, AddSessionResult, Log, LoadScene. |
| **KasaYoneticisi** | Ana kasa / ödül havuzu; para girişi, havuzdan ödeme; PlayerPrefs ile kalıcılık. |
| **OdemeServisi** | Havuz/limit delegasyonu; GetSpinOdenebilirLimit = dinamik (senaryo bütçesi) veya override veya havuz %10. |
| **DonusAkisServisi** | Normal ve bonus spin akışı (simülasyon → oynat → öde → SenaryoOdenebilirGuncelle, SpinTamamlandi, scatter/bonus). |
| **EkonomiServisi** | Bakiye/bahis; GameManager.ActivePlayer ve PlayerPrefs ile senkron. |
| **LogYoneticisi** | Log sahnesi; profil istatistikleri + SenaryoYoneticisi.GetOturumLogu() ile senaryo olayları. |

---

## 3. Kritik Akışlar

### 3.1 Spin akışı
1. Spin butonu → `OyunYoneticisi.SpinButon` → `BirSpinHazirlaVeAt`: ödenebilir limit alınır, bahis düşülür, `NormalSpinAkisi` başlatılır.
2. `NormalSpinAkisi`: `SimuleEtVeKaydet(odenebilirLimit, false)` → `SimulasyonKaydiniOynat(kayit)` (animasyon).
3. Ödeme: `PayFromHavuz` veya `AddWinnings`; `SenaryoOdenebilirGuncelle(odenen, bahis)`; `SenaryoYoneticisi.I?.SpinTamamlandi(odenen, bahis)`.
4. `SpinTamamlandi`: toplamSpin++, kazanç/kayıp, AsamaAyariniUygula, UI_Guncelle, AsamaGecisiKontrol.

### 3.2 Bahis butonları
- Artık **OyunYoneticisi.BahisArttir / BahisAzalt**’a bağlı (ekonomi + SenaryoYoneticisi.BahisArtirimiYapildi + UI_Guncelle). Doğrudan EkonomiServisi’ne bağlı değil.

### 3.3 Senaryo aşaması kalıcılığı
- Kayıt: `OnApplicationQuit` / `OnDestroy` → `SenaryoAsamaKaydet()` → PlayerPrefs (mevcutAsama, asamaGirisSpinIndex).
- Yükleme: `Start()` içinde GameManager + PlayerPrefs; aşama ve asama giriş spin indeksi geri yüklenir.

---

## 4. Kalıcılık (PlayerPrefs + Dosya)

| Key / Kaynak | İçerik |
|--------------|--------|
| PP_BAKIYE, PP_BAHIS | Bakiye, bahis (EkonomiServisi). |
| PP_SENARYO_MEVCUT_ASAMA, PP_SENARYO_ASAMA_GIRIS_SPIN | Mevcut aşama (1–7), aşama giriş spin indeksi. |
| PP_SENARYO_ODENEBILIR_KALAN_TL | Senaryo ödenebilir bütçe (100k’dan başlayıp ödeme/ödeme yok ile değişen). |
| PP_ANA_KASA_TL, PP_ODUL_HAVUZU_TL, PP_*_KAP | Ana kasa, ödül havuzu, görsel kapasiteler (KasaYoneticisi). |
| profiles.json | Oyuncu profilleri (balance, totalSpins, totalWon, totalLost, logs, vb.). |

**Kalıcı olmayan (sadece oturum):**
- `bahisArtirimSayisi`, `sonBahisArtirimSpinIndex` – oyun kapatılıp açılınca sıfırlanır; “Bahis değişikliği” sayacı ve aşama 2’deki “bahis artırımı sonrası 3–4 spin kolaylaştırma” kaybolur.
- Senaryo olay logu (`oturumLogu`) – sadece SenaryoYoneticisi bellekte; log sahnesine geçince (DontDestroyOnLoad ile) hâlâ görünür, uygulama kapanınca silinir.

---

## 5. Potansiyel Bug ve Riskler

### 5.1 Sahne / singleton
- **SenaryoYoneticisi.I**: Senaryo sahnesinden çıkıp Admin veya başka sahneye geçilince `I` eski sahnenin instance’ına işaret etmeye devam eder. Eski sahnedeki UI referansları (asamaText, mevcutAyarlarMetni vb.) artık geçersiz/destroy olabilir. FindObjectOfType<OyunYoneticisi> yeni sahnedeki OyunYoneticisi’ni döner; senaryo mantığı yanlış bağlamda çalışabilir.
- **Çözüm önerisi:** Sahne adına göre “şu an senaryo sahnesindeyiz” kontrolü; senaryo sahnesinde değilken SenaryoYoneticisi çağrılarını atlamak veya I’ı null’a çekmek.

### 5.2 FindObjectOfType / timing
- SenaryoYoneticisi birçok yerde `FindObjectOfType<OyunYoneticisi>()` kullanıyor (AsamaAyariniUygula, UI_Guncelle, GecikmeliMevcutDurumYenile). Sahne değişince veya OyunYoneticisi henüz hazır değilse null dönebilir; çağrılar null kontrolü ile güvende ama UI “Mevcut ayarlar yükleniyor...” kalabilir.
- **OyunYoneticisi** sahne yüklendikten sonra Start/InitRoutine ile hazır olur; SenaryoYoneticisi’nin 0.4 sn gecikmeli yenilemesi bu yüzden var.

### 5.3 Ödenebilir limit / gösterim
- Normal spin: `GetSpinOdenebilirLimit()` (senaryoda dinamik bütçe) kullanılıyor; panel de aynı kaynaktan besleniyor.
- Bonus: Limit `GetBonusRemainingPayableTL()` (bonus tavanı + havuz %10 + senaryo bütçesi). Panel hâlâ `GetSpinOdenebilirLimit()` (senaryo bütçesi) gösteriyor; bonus tavanı daha düşükse gerçek üst limit panelden farklı olabilir (kullanıcı kafası karışabilir).
- Ödenebilir bütçe 0.4 sn gecikmeyle yükleniyor; ilk frame’lerde yanlış değer görünebilir.

### 5.4 Bahis değişikliği sayacı
- `bahisArtirimSayisi` ve `sonBahisArtirimSpinIndex` kaydedilmiyor. Oyun kapatılıp açılınca 0 / -1 olur. Sonuçlar:
  - “Bahis değişikliği (N)” panelde sıfırdan başlar.
  - Aşama 2’de “bahis artırımı sonrası 3–4 spin zorluk 5” kuralı (spinFarkiBahis) fiilen devre dışı kalır (sonBahisArtirimSpinIndex = -1).
  - Geçiş şartı “Bahis değişikliği ≥ 1 veya 2” tekrar sağlanmalı.

### 5.5 Coroutine / destroy
- Spin ve bonus akışı coroutine zinciri; sahne değişir veya obje destroy edilirken devam ederse callback’ler (SenaryoYoneticisi.I?.SpinTamamlandi vb.) eski/destroy instance’a gidebilir veya atlanabilir. Unity’de genelde objeyle birlikte durur ama servis/context üzerinden çağrı varsa dikkat gerekir.

### 5.6 Çift sayaç (totalSpins / toplamSpin)
- GameManager.ActivePlayer.totalSpins ve SenaryoYoneticisi.toplamSpin var. Spin sonrası hem OyunYoneticisi (totalSpins += 1) hem SpinTamamlandi (toplamSpin++) güncelleniyor; senaryo sahnesinde senkron. Yüklemede SenaryoYoneticisi toplamSpin = profile.totalSpins yapıyor; tutarlı.

---

## 6. Spec ile Kod Uyumu (SENARYO_ASAMA_ZORLUK_VE_AYARLAR_SPEC.md)

| Spec maddesi | Kod durumu |
|--------------|------------|
| Aşama 1–7 temel zorluk 5–11 | GetAsamaTemelZorluk ile uygulanıyor. |
| Aşama 2: bahis artırımı sonrası 3–4 spin zorluk 5 | Var; sonBahisArtirimSpinIndex ile. (Yüklemede index sıfırlanıyor.) |
| Aşama 4: yükleme sonrası ilk 20 spin zorluk 6 | Artık sadece aşama 1–3’te uygulanıyor; aşama 4’te temel 8 korunuyor. |
| Bonus satın al Aşama 1: max maliyet + %30 | BaslatBonus içinde _sonBonusSatinAlindiMaliyet ile tavan uygulanıyor. |
| Scatter Aşama 1: ~50 spinde bir | GetScatterChanceFor ile Aşama 1’de düşük oran (0.01f). |
| Aşama 3–4–5–6 bonus tavanları (scatter/satın al ±%) | Sadece Aşama 1 satın al tavanı kodda; diğer aşamalar için henüz yok. |
| Near-miss (3 scatter), bonus içi yüksek çarpan + tumble yok | Ayrı mekanizma olarak tam uygulanmıyor. |
| 3. yükleme sonrası ilk bonus: yüksek ödeme | Özel “ilk bonus” ve 2.5x maliyet mantığı yok. |
| Finale (7): kazanç tamamen kapalı | GetAsamaTemelZorluk 11 veriyor; “tumble 0” için ek mantık yok. |

---

## 7. Geliştirme Önerileri (Öncelik Sırasıyla)

1. **Senaryo aşaması + sahne tutarlılığı**  
   Sahne adına göre “senaryo sahnesi aktif mi” kontrolü; sadece bu sahnedeyken SenaryoYoneticisi.I ile senaryo mantığı ve UI güncellemesi yapılsın; diğer sahnelerde I’ı kullanmamak veya null saymak.

2. **bahisArtirimSayisi / sonBahisArtirimSpinIndex kalıcılığı**  
   PlayerPrefs (veya profile) ile kaydet/yükle; böylece “Bahis değişikliği” ve aşama 2 kolaylaştırması kapatıp açınca da doğru kalır.

3. **Bonus tavanları (aşama 2–3–4–5–6)**  
   Spec’teki scatter/satın al ±% kurallarını aşama bazlı uygula (maliyet +%20, -%20, -%50 vb.).

4. **Ödenebilir gösterimi bonus sırasında**  
   Bonus oyundayken panelde “min(ödenebilir bütçe, bonus kalan tavan)” veya “Bonus kalan: X / Ödenebilir bütçe: Y” gibi net bir metin; kullanıcı neden ödeme kesildiğini anlasın.

5. **Senaryo olay logunun kalıcılığı**  
   Oturum logunu (oturumLogu) uygulama kapanırken dosyaya veya profile’a yaz; log sahnesi açıldığında (özellikle yeniden başlatma sonrası) son oturum logu da listelensin.

6. **Aşama 3–5–6–7 özel kurallar**  
   Near-miss (3 scatter), “bonus içi çarpan göster tumble yok”, “3. yükleme sonrası ilk bonus 2.5x”, Finale’de tumble/ödeme 0 gibi spec maddeleri için net görevler çıkarılıp adım adım kodlanabilir.

7. **OyunYoneticisi büyüklüğü**  
   Proje kurallarına uygun olarak orkestrasyonu koruyup, tumble/simülasyon/ödeme mantığını Unity’den bağımsız servis sınıflarına taşıma hedefi sürdürülebilir.

8. **Unit / entegrasyon testi**  
   Kritik yol: spin → simülasyon (limit aşımı yok) → ödeme → senaryo bütçe güncellemesi → SpinTamamlandi → aşama geçişi; senaryo bütçesi 0 iken davranış; bonus tavanı aşımı. Bunlar için test senaryoları yazılabilir.

---

## 8. Kısa Özet

- **Aşama 4 zorluk:** Yükleme sonrası 20 spin kuralı artık sadece aşama 1–3’te; aşama 4’te zorluk 8 doğru şekilde görünecek.
- **Mimari:** OyunYoneticisi orkestratör, SenaryoYoneticisi aşama/UI/senaryo mantığı, servisler delegasyon ile bağlı.
- **Riskler:** SenaryoYoneticisi.I sahne değişiminde eski referanslar, bahis sayacı kalıcı değil, bonus tavanları sadece kısmen, ödenebilir gösterimi bonusta belirsiz olabilir.
- **Öneriler:** Sahne/singleton güvenliği, bahis sayacı kalıcılığı, spec’teki bonus tavanları ve aşama özel kurallarının tamamlanması, senaryo logunun kalıcılığı, test ve refaktör.

Bu belge, ileride yapılacak değişikliklerde referans olarak kullanılabilir; yeni bulunan risk veya karar eklenerek güncellenebilir.
