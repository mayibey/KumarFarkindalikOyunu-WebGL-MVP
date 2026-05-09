# CS Dosyası Birleştirme Analizi

EkonomiAyarlari gibi birleştirilebilecek / silinebilecek dosyalar analiz edildi.

---

## 1. Hemen yapılabilecekler (düşük risk)

| Öneri | Dosya | Ne yapılır | Kazanç |
|-------|--------|------------|--------|
| **StatsEntry → PlayerProfile.cs** | StatsEntry.cs silinir | `StatsEntry` sınıfı `PlayerProfile.cs` dosyasına taşınır (GameLogEntry zaten orada). GameManager ve PlayerProfile aynı tipi kullanmaya devam eder. | **-1 .cs** |
| **SesAyarlari sil** | SesAyarlari.cs silinir | Kodda hiçbir yerde `SesAyarlari` referansı yok; sadece 02_SenaryoluOyun sahnesinde bileşen var. Ses kaynakları muhtemelen OY veya başka objelerde atanıyor. Script silinirse sahnedeki objede Missing Script kalır; bileşeni kaldırman yeterli. | **-1 .cs** |

---

## 2. İsteğe bağlı (arayüzleri servis dosyasına al)

| Öneri | Dosyalar | Ne yapılır | Kazanç |
|-------|----------|------------|--------|
| **Arayüz + servis aynı dosyada** | IDonusAkisBaglami, IIzgaraBaslatmaBaglami, ICokmeAkisBaglami, ITumbleAkisBaglami, IScatterEfektBaglami, IOyunUIGuncellemeBaglami, IOyunBootstrapBaglami, ICarpanYerlestirmeBaglami, IZorlukBaglami, IOyunKorumaBaglami | Her arayüz ilgili servis .cs dosyasının **üstünde** (aynı dosyada) tanımlanır; ayrı I*.cs silinir. Örn. `IDonusAkisBaglami` → `DonusAkisServisi.cs` içinde. | **-10 .cs** |

Not: OyunYoneticisi `using` veya tam adla zaten arayüze referans veriyor; sınıf adı ve namespace değişmez, sadece dosya birleşir.

---

## 3. Orta risk / daha büyük iş

| Öneri | Dosya | Zorluk | Not |
|-------|--------|--------|-----|
| **SpinIconRotate → OyunYoneticisi** | SpinIconRotate.cs silinir | Orta | Dönüş ikonu mantığı OY içinde bir `Update` + `SerializeField Transform spinIconTransform` ile yapılır; `SetSpinIconRotate(bool)` kalır. Sahnede artık SpinIconRotate bileşeni yerine OY’ye transform ref verilir. **-1 .cs** ama sahne bağlantısı değişir. |
| **CarpanAyarlari kaldır** | CarpanAyarlari.cs silinir | Yüksek | UygulaCarpanAyarlari çok alan kopyalıyor; hepsi OY’de default + (isteğe bağlı) AdminPanel slider’dan gelir. Force x2/x5 butonları AdminPanel veya OY’de toplanır. EkonomiAyarlari’ya benzer ama alan sayısı çok. |

---

## 4. Birleştirilmesi mantıklı olmayanlar

| Dosya | Neden |
|-------|--------|
| **TumbleAyarlari** | PayTable ve ScatterIndex merkezi; birçok yer kullanıyor. Servise taşımak büyük refaktör. |
| **BonusAyarlari / OdulHavuzuAyarlari** | SyncFromAyarClasses’ta onlarca alan kopyalanıyor; default’lara çekmek büyük iş. İleride tek “OyunAyarlari” config’e toplanabilir. |
| **HosgeldinizText** | Çok küçük; sahnede bileşen. Birleştirmek dosya sayısında anlamlı kazanç sağlamaz. |
| **UIReferanslari** | Sahne referans konteyneri; SahneBaglamaServisi ile sıkı bağlı. Ayrı kalmalı. |

---

## Özet (dosya azaltma potansiyeli)

| Aksiyon | Dosya sayısı değişimi |
|---------|------------------------|
| StatsEntry → PlayerProfile.cs | -1 |
| SesAyarlari sil | -1 |
| 10 arayüzü ilgili servis dosyasına taşı | -10 |
| SpinIconRotate → OY (isteğe bağlı) | -1 |
| **Toplam (1+2 uygulanırsa)** | **-12 .cs** |

Öncelik: Önce **StatsEntry → PlayerProfile.cs** ve **SesAyarlari sil** (2 dosya azalır, risk düşük). Sonra istersen arayüz birleştirmeleri.
