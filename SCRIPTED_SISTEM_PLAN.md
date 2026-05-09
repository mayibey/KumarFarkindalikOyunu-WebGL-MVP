# Scripted Spin Sistemi — Uygulama Planı

> **Branch:** `refactor-paket8`
> **Hazırlayan:** Claude Code, 2026-05-04
> **Bağlam:** Bu plan, kullanıcı tarafından verilmiş ve uygulama aşamaları halinde takip edilmektedir. Hiçbir mevcut dosya silinmez; sadece eklemeler ve hook satırları yapılır.

## 1. Mimari Özet

Mevcut proje senaryolu sahnede (`03_SenaryoluOyun`) 7 aşamalı bir kumar
manipülasyon hikayesi anlatıyor. RNG tabanlı mevcut sistem RTP hedefini
tutturamıyor. Çözüm: **anlatıcı sahnesinde RNG'yi bypass edip tamamen
deterministik scripted spinler kullanmak.**

Diğer sahneler (`02_TutorialScene`, `04_AdminOyunScene`) mevcut RNG akışında
kalır, hiç etkilenmez.

Hook noktası: `OyunYoneticisi.Spin.cs` içindeki `SimuleEtVeKaydetImpl`
metodunun başı. ScriptedSpinYoneticisi.Aktif=true ise scripted akış,
değilse mevcut RNG akışı.

## 2. Bakiye Eğrisi (Senaryo)

| Aşama | Sunum karşılığı | Bahis × Spin | Net | Bakiye sonu |
|---|---|---|---|---|
| Başlangıç | — | — | — | 50.000 |
| 1. Isındırma ve Umut | Sunum 2 | 500 × 8 | +10.000 | 60.000 |
| 2. Kontrol Bende | Sunum 3 | 1000 × 8 | -4.250 | 55.750 |
| 3. Geri Kazanabilirim | Sunum 4 | 1500 × 8 | -11.250 | 44.500 |
| 4. Şansım Döndü | Sunum 5 | 1000 × 5 | +15.000 | 59.500 |
| 5. Sonunu Düşünen | Sunum 6 | dinamik | -58.700 | 800 |
| Yükleme paneli | Sunum 7 | — | +50.000 | 50.800 |
| 6. Başka Yerden Para | Sunum 7 | 2000 × 25 | -50.800 | 0 |
| 7. Tükeniş cutscene | — | spinsiz | — | 0 |

Toplam oyuncu kaybı: 100.000 TL.

## 3. Spin Tabloları (Tam Liste)

### AŞAMA 1 — Isındırma ve Umut (bahis 500)
| # | Konfigürasyon | Brüt |
|---|---|---|
| 1 | 3 tumble: 8 hindistancevizi → 8 elma → 8 üzüm | 1500 |
| 2 | Saf rastgele sıfır | 0 |
| 3 | 8 elma + 8 üzüm + ilk grid x2 çarpan | 2500 |
| 4 | 8 üzüm tek cluster | 750 |
| 5 | 2 tumble: 8 üzüm → 8 elma | 1250 |
| 6 | 7 üzüm near-miss (sıfır, görsel olarak 7 üzüm grid'de) | 0 |
| 7 | 8 üzüm + 8 elma + ilk grid x5 çarpan (MEGA) | 6250 |
| 8 | 10 elma + 8 hindistancevizi tek tumble | 1750 |

### AŞAMA 2 — Kontrol Bende Hissi (bahis 1000)
| # | Konfigürasyon | Brüt |
|---|---|---|
| 1 | 8 elma tek cluster | 1000 |
| 2 | 8 hindistancevizi tek cluster | 500 |
| 3 | Çift 7'lik near-miss (7 üzüm + 7 elma, sıfır) | 0 |
| 4 | 8 üzüm tek cluster | 1500 |
| 5 | Saf rastgele sıfır | 0 |
| 6 | 3 scatter near-miss (3 yıldız grid'de, sıfır) | 0 |
| 7 | 2 tumble: 8 elma → 8 hindistancevizi | 750 |
| 8 | Saf rastgele sıfır | 0 |

### AŞAMA 3 — Geri Kazanabilirim (bahis 1500)
| # | Konfigürasyon | Brüt |
|---|---|---|
| 1 | Saf rastgele sıfır | 0 |
| 2 | 7 üzüm near-miss | 0 |
| 3 | 7 elma near-miss | 0 |
| 4 | Çift 7'lik near-miss | 0 |
| 5 | 8 hindistancevizi (yarım iade) | 750 |
| 6 | 3 scatter near-miss | 0 |
| 7 | Saf rastgele sıfır | 0 |
| 8 | Üçlü near-miss (7 üzüm + 7 elma + 3 scatter) | 0 |

### AŞAMA 4 — Şansım Döndü (bahis 1000, 5 spin)
| # | Konfigürasyon | Brüt |
|---|---|---|
| 1 | Saf rastgele sıfır | 0 |
| 2 | 7 üzüm near-miss | 0 |
| 3 | Çift 7'lik near-miss | 0 |
| 4 | Saf rastgele sıfır | 0 |
| 5 | 🎯 8 ARMUT + ilk grid x100 çarpan (MEGA WIN) = 20.000 | 20000 |

### AŞAMA 5 — Sonunu Düşünen Kahraman (bahis 2000 + bonus tuzağı)
| # | Olay | Bahis | Brüt |
|---|---|---|---|
| 1 | Saf rastgele sıfır | 2000 | 0 |
| 2 | 8 üzüm + ilk grid x2 çarpan (kazanç anı) | 2000 | 6000 |
| 3 | x500 çarpan grid'e düşer ama hiçbir sembolden 8'lik yok → çarpan kaçtı | 2000 | 0 |
| 4 | MODAL: "Şanslı saatindesin, bonus oyun aç" → otomatik bakiyenin tamamı yatar | bakiye=58000 | bonus tetik |
| 5 | Bonus oyun: scripted CÜZİ ödeme 800 TL | (bonus) | 800 |

### AŞAMA 6 — Başka Yerden Para (bahis 2000, 25 spin)

Yükleme paneli açılır → 50.000 yüklenir → bakiye 50.800.

25 spin tablosu:
- Spin 1-5: rastgele sıfır
- Spin 6: bahis iadesi 2000 (sahte umut)
- Spin 7-10: çeşitli near-miss
- Spin 11: bahis iadesi 2000
- Spin 12-15: rastgele sıfır
- Spin 16-20: near-miss varyasyonları
- Spin 21-25: rastgele sıfır kapanış

Hesap: Brüt 4000 (iki bahis iadesi) - Bahis 50.000 = -46.000.
Bakiye 50.800 + 4000 - 50.000 = 4.800. SIFIRA İNDİRMEK için ek
4.800 TL kayıp gerek → Spin 11'i de sıfır yap (bahis iadesi 1 tane kalsın)
ve son spinde yarım bahis kal (24 spin tam + 25. spin bakiye yetmez):
- Toplam tam spin = 24 × 2000 = 48.000 + iade 2000 = brüt 2000, net -46.000
- Bakiye 50.800 - 46.000 = 4.800 → 25. spin için yetersiz değil (4.800 > 2000)
- Spin 25-26 daha gerek. Dinamik.

**Çözüm: A6 tam dinamik olsun. Bakiye bitene kadar scripted sıfır spin
servis et. Spin sayısı _spinKalan = bakiye/bahis kadar.**

### AŞAMA 7 — Tükeniş Cutscene
- Hiç spin yok.
- Anlatıcı kapanış metni: "Oyun bitti. Yatırdığınız toplam: 100.000 TL.
  Geri aldığınız: 0 TL. Bu tablo gerçek hayatta her gün yaşanıyor."

## 4. Modal Mesajları (Üçüncü Tekil Narrator, Bloke Eden, OK Butonu)

Frekans: Her 3 spin'de bir + her aşamanın kritik anları.

### A1 (Isındırma)
- Spin 4: "Oyuncu ilk kazançları yaşıyor. Beyninde dopamin salgılanıyor. Bu his, sonraki saatlerce oyun oynamanın yakıtı olacak."
- Spin 7 öncesi: "Sistem büyük bir kazanç yaşatmak üzere. Geçmiş kayıpları unutturacak bir an gelecek."
- Spin 8: "İlk kazanç en tehlikeli başlangıçtır."

### A2 (Kontrol)
- Spin 3: "Oyuncu artık 'oyunun mantığını çözdüğünü' düşünmeye başlıyor. Aslında kazançların ne zaman geleceğini sistem belirliyor."
- Spin 6: "Kontrol yanılsaması — oyuncu kendini şanslı hissediyor, kaybetme ihtimalini küçümsüyor."
- Spin 8: "Sen oyunu yönettiğini düşünürken, oyun seni adım adım içine çekiyor."

### A3 (Geri Kazanabilirim)
- Spin 3: "İlk ciddi kayıplar yaşanıyor. Amaç para kazanmaktan çıktı, kayıpları telafi etmeye dönüştü."
- Spin 6: "Oyuncu kayıpları geri kazanmak için daha fazla risk alıyor. Mantıklı düşünme yetisini kaybediyor."
- Spin 8: "Bir tur daha = bir kayıp daha."

### A4 (Şansım Döndü)
- Spin 2: "Üst üste kayıplar oyuncuyu yıpratıyor. Sistem şimdi büyük bir vuruş hazırlıyor."
- Spin 4: "Oyuncu pes etmek üzere. Tam bu noktada büyük kazanç gelecek — bu kasıtlı bir manipülasyon."
- Spin 5 (MEGA WIN): "Bir büyük kazanç tüm geçmiş kayıpları gölgeliyor. Oyuncu 'şansının döndüğüne' inanıyor. Bu büyük kazancın amacı yeni bahisleri tetiklemek."

### A5 (Sonunu Düşünen)
- Spin 1: "Bahis arttı, beklenti arttı. Adrenalin salgılanıyor."
- Spin 3 (x500 kaçar): "x500 çarpan ekrana düştü ama eşleşme olmadı. Oyuncuda 'bir daha denemek' arzusu yaratıldı. Sistem fırsat sunmak üzere."
- Spin 4 öncesi modal popup (bonus tuzağı):
  > "🎰 ŞANSLI SAATİNDESİN! Bonus oyun aktif edildi. Bakiyenin tamamını yatır, x10000 kazanma şansını kaçırma. SINIRLI TEKLİF."
  > [Şansını Dene]
- Spin 5 (cüzi ödeme): "Oyuncu tüm bakiyesini bonus oyuna yatırdı. Geri aldığı miktar yatırdığının %1'i. Bu sömürünün adı 'değişken oranlı pekiştireç'."

### A6 (Başka Yerden Para)
- Yükleme paneli öncesi: "Oyuncu artık kontrolü kaybetti. Borç alarak oyunu devam ettirmek istiyor."
- Spin 6 (sahte iade): "Küçük bir kazanç umudu yeniden alevlendiriyor. Bu kasıtlı bir tuzak."
- Spin 12: "Borçla kurulan düzen ilk rüzgarda yıkılır."
- Spin 18: "Şans kısa, borçları ödemek uzun sürer."
- Spin 24 (kapanışa yakın): "Borcun büyüdükçe sen küçülürsün."

### A7 (Tükeniş)
- Cutscene metni (kalın, sahnenin tamamında):
  > "Oyun bitti.
  > Yatırdığınız toplam: 100.000 TL.
  > Geri aldığınız: 0 TL.
  > Bu tablo gerçek hayatta her gün yaşanıyor.
  > Yardım istemek bir zayıflık değil, güçlü bir farkındalıktır."

## 5. Yükleme Paneli (A6 Başında)

Tek buton:
> "Borç al — 50.000 TL yükle"

Açıklayıcı alt metin (oyuncu butona basmadan önce okur):
> "Aileden, kredi kartından veya iş arkadaşından borç alarak oyuna devam etmek istiyorsun. Borçla kumar oynamak bağımlılığın klasik göstergelerinden biridir."

Buton tıklanınca: bakiye += 50.000, A6 spinleri başlar.

## 6. Dosya Yapısı

```
Assets/_Project/Scripts/Senaryo/Scripted/
  ├─ ScriptedSpinKaydi.cs         (data class, JSON serializable)
  ├─ ScriptedAsamaListesi.cs      (ScriptableObject)
  ├─ ScriptedSpinYoneticisi.cs    (singleton, AnlaticiSeritKopru dinler)
  ├─ ScriptedSpinUygulayici.cs    (Kayıt → SpinSimulasyonKaydi dönüşümü)
  ├─ ScriptedModalKopru.cs        (modal mesajları HTML iframe'e yollar)
  ├─ ScriptedYuklemePaneli.cs     (Unity Canvas, tek buton)
  └─ ScriptedBonusOyunUygulayici.cs (BonusOyunYoneticisi entegrasyonu)

Assets/_Project/Resources/
  └─ ScriptedSenaryo.asset        (Inspector'dan dolacak)

DEĞIŞEN MEVCUT DOSYALAR:
  ├─ OyunYoneticisi.Spin.cs       (8 satır hook eklenir)
  └─ BonusOyunYoneticisi.cs       (5 satır hook eklenir)
```

## 7. Uygulama Aşamaları

### AŞAMA 1: Veri Modelleri + Senaryo Asset

Yarat:
- `ScriptedSpinKaydi.cs`:
  - Field'lar: `int spinSiraNo`, `int asamaIndex` (0-6), `int bahis`,
    `SpinTipi tip` (enum: Sifir, NearMiss, Kazanc, MegaWin, BonusTetik, BahisIadesi),
    `long brutOdeme`, `int[] ilkGridSemboller` (30 hücre, -1 boş),
    `int[] ilkCarpanDegerleri` (her hücre için, 0=çarpan yok),
    `List<TumbleAdimTanimi> tumbleler`,
    `string modalMesaji` (null = mesaj yok),
    `bool carpanKactiFlag` (x500 kaçtı senaryosu için).
- `TumbleAdimTanimi` nested class: `List<int> patlayanHucreler` (Vector2Int olarak),
  `int[] gridRefillSonrasi`, `int[] yeniCarpanlar`.
- `ScriptedAsamaListesi.cs`: ScriptableObject.
  - `[CreateAssetMenu(fileName = "ScriptedSenaryo", menuName = "Kumar/Scripted Senaryo")]`.
  - `List<List<ScriptedSpinKaydi>> asamaSpinleri` (7 aşama, her biri liste).
- `Assets/_Project/Resources/ScriptedSenaryo.asset` dosyasını yarat (sahne yüklenirken Resources.Load ile çekilecek).

Asset için verileri **bu plandaki Bölüm 3 tablolarına göre Inspector'da elle
doldurmama gerek yok** — tablodaki tüm 59 spini scripted olarak C# init kodu
içinde doldur, asset'i programatik olarak yarat. Editor menüsü ekle:
"Tools/Kumar/Scripted Senaryo Asset'ini Yeniden Üret" — bu menü çalıştırınca
asset'i temizleyip plandaki verilere göre yeniden doldursun.

DURMA NOKTASI: Bu aşama bittiğinde derle, hata yoksa "AŞAMA 1 hazır" diye dön.

### AŞAMA 2: Yönetici + Hook

- `ScriptedSpinYoneticisi.cs`:
  - Singleton: `public static ScriptedSpinYoneticisi Ornek`.
  - `public static bool Aktif` (sadece `03_SenaryoluOyun` sahnesinde true).
  - `Start()`: `Resources.Load<ScriptedAsamaListesi>("ScriptedSenaryo")`.
  - `public ScriptedSpinKaydi SonrakiSpiniAl(int asamaIndex, int spinSiraNo)`.
  - `AnlaticiSeritKopru.SpinAtildi` callback'inden tetiklenir.

- `OyunYoneticisi.Spin.cs` SimuleEtVeKaydetImpl başına:
```csharp
  if (!bonusSpin && ScriptedSpinYoneticisi.Aktif)
  {
      var kayit = ScriptedSpinYoneticisi.Ornek.SonrakiSpiniAl(
          AnlaticiSeritKopru.Ornek.AktifAsama,
          AnlaticiSeritKopru.Ornek.AsamadakiSpinSayaci);
      if (kayit != null)
      {
          ScriptedSpinUygulayici.UygulaKaydi(kayit, this);
          return;
      }
  }
```

- Tutorial ve admin sahnelerinde `Aktif=false` olduğunu doğrula (sahne
  build index'ine bak — sadece index 2 = 03_SenaryoluOyun olduğunda true).

DURMA NOKTASI: Hook entegre, scripted aktif olduğunda mevcut RNG akışı
bypass ediliyor mu test edilebilir. "AŞAMA 2 hazır" diye dön.

### AŞAMA 3: Uygulayıcı (Kayıt → SpinSimulasyonKaydi)

- `ScriptedSpinUygulayici.cs`:
  - `public static void UygulaKaydi(ScriptedSpinKaydi kayit, OyunYoneticisi mgr)`.
  - `ScriptedSpinKaydi`'i `SpinSimulasyonKaydi`'a çevir:
    - `IlkGrid` ← `kayit.ilkGridSemboller` (1D → 2D dönüşüm)
    - `IlkCarpanDegerleri` ← `kayit.ilkCarpanDegerleri`
    - `Adimlar` ← her tumble için `TumbleAdimKaydi` üret
    - `ToplamHamKazanc` ← `kayit.brutOdeme`
    - `NihaiCarpanToplam` ← çarpanların toplamı (Sweet Bonanza mantığı: SUM, not multiply)
  - `mgr.NormalSpinAkisi(kayit)` çağır (mevcut metod scripted kayıtla da çalışır).

- `kayit.carpanKactiFlag = true` ise: çarpan grid'e konulur ama hiçbir
  sembolden 8'lik cluster oluşmaz, ödeme 0, görsel olarak çarpan yanıp söner.
  Bu A5 Spin 3 senaryosu için.

DURMA NOKTASI: A1'in 8 spinini deterministik oynayabilen sistem.
Bahis iadesi, near-miss, çarpan, tumble zinciri çalışıyor. "AŞAMA 3 hazır."

### AŞAMA 4: Modal + Yükleme Paneli + Anlatıcı Entegrasyonu

- `ScriptedModalKopru.cs`: Mevcut anlatıcı HTML iframe'e ek olarak.
  Yeni Unity Canvas: bloke eden modal panel (ScreenSpace - Overlay),
  arka plan yarı saydam, ortada metin kutusu + "OK" butonu.
- `kayit.modalMesaji != null` ise modal aç, OK basılana kadar spin akışı
  bekle (coroutine).
- A5 Spin 4 öncesi popup için özel modal: "ŞANSLI SAATİNDESİN" başlığı,
  alt metin, "Şansını Dene" butonu (tek buton, scripted akışı zorlamak için).
- `ScriptedYuklemePaneli.cs`: A5 sonu (bakiye 800) → A6 başı geçişinde
  açılır. Tek "Borç al — 50.000 TL" butonu. Buton bakiyeye 50.000 ekler,
  A6'ya geçer.

DURMA NOKTASI: Tüm görsel akış tamam. "AŞAMA 4 hazır."

### AŞAMA 5: Bonus Oyun Entegrasyonu

- `ScriptedBonusOyunUygulayici.cs`:
  - A5 Spin 4'te `BonusOyunYoneticisi.BonusOyunBaslat()` çağrılır.
  - Bonus oyun spinleri scripted ödeme 800 TL toplam verecek şekilde
    tasarlanır (örn 5-10 bonus spin, biri 400, biri 200, biri 200 → toplam 800).
- `BonusOyunYoneticisi.cs`'e hook: scripted aktif ve scripted bonus tetiklendiyse,
  RNG yerine scripted bonus akışı kullan.

DURMA NOKTASI: Tam senaryo başından sonuna oynayabiliyor. "AŞAMA 5 hazır, sistem komple."

## 8. Test Kontrol Listesi

Her aşama tamamlanınca:
- [ ] `02_TutorialScene` hâlâ RNG ile çalışıyor mu?
- [ ] `04_AdminOyunScene` hâlâ RNG ile çalışıyor mu?
- [ ] `03_SenaryoluOyun` scripted akışta mı?
- [ ] Console error/warning yok
- [ ] AnlaticiSeritKopru aşama geçişleri scripted ile uyumlu

## 9. Önemli Teknik Notlar

- `SimuleEtVeKaydetImpl` mevcut 770 satır → dokunulmuyor, sadece üstüne
  8 satır hook ekleniyor.
- `senaryo1KazancBandiZorunlu`, `_ustUsteKazancFaziAktif`, `kacisFrenlemeUygula`
  flag'leri scripted moddayken otomatik bypass edilir (hook'tan return ediliyor).
- `Senaryo1HedefOdemeMotoru` dokunulmuyor, scripted modunda hiç çağrılmıyor.
- ScatterIndex sahnede 8 (script default 7), kod default'una güvenme,
  `tumbleAyarlari.ScatterIndex`'i runtime oku.
- Cluster algılama "scatter pays" mantığında — sembolün gridde nerede
  olduğu önemli değil, sadece sayısı önemli. Kayıtlarda sembolleri istediğin
  yere koyabilirsin.
- Çarpanlar SUM ile toplanır, MUL değil (`_spinCarpanCarpim` ismi yanıltıcı).
