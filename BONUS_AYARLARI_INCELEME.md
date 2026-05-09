# BonusAyarlari İnceleme

## 1. Dosyanın yapısı

**BonusAyarlari.cs** iki bölümden oluşuyor:

| Bölüm | İçerik | Satır (yaklaşık) |
|--------|--------|-------------------|
| **Config (Inspector)** | [Header] ile public alanlar: BonusHakBaslangic, BonusSpinBekleme, BonusSatinAlCarpani, BonusBudgetAktif, BonusBudgetHavuzOran, BonusBudgetMinTL/MaxTL, BonusOtoZorlukAktif, BonusMinCluster_Easy/Hard, ScatterChanceNormal/Bonus, ScatterEsik, ScatterScaleUp, ScatterAnimDuration. | 1–31 |
| **Runtime state + mantık** | Private: _bonusHakKalan, _bonusKazanc, _bonusPendingOdemeTL, _bonusBudgetKalanTL, _bonusOturumOdenenToplamTL, _bonusOdenenTL, _bonusBaslangicHavuzTL, _bonusMaxOdemeTL, _bonusMaxOdemeHavuzOrani. Metodlar: BonusBaslat, SpinHakAzalt, GetBonusRemainingPayableTL, BonusOdemeEkle, BonusPendingTemizle, BonusBittiMi, BonusSatinAlMaliyet. | 33–136 |

---

## 2. Kodda nasıl kullanılıyor?

**BonusAyarlari’ya tek referans:** `OyunYoneticisi.SyncFromAyarClassesIfPresent()`.

- `var bonus = FindFirstObjectByType<BonusAyarlari>(...);` ile sahnedeki bileşen bulunuyor.
- Sadece **config alanları** okunup OY’nin kendi alanlarına kopyalanıyor:
  - bonusHakBaslangic, bonusSpinBekleme, bonusSatinAlCarpani, bonusBudgetAktif, bonusBudgetHavuzOran, bonusBudgetMinTL, bonusBudgetMaxTL, bonusOtoZorlukAktif, bonusMinCluster_Easy, bonusMinCluster_Hard, scatterChanceNormal, scatterChanceBonus, scatterEsik, scatterScaleUp, scatterAnimDuration.

**BonusAyarlari’daki state ve metodlar hiç çağrılmıyor:**

- `BonusBaslat`, `SpinHakAzalt`, `GetBonusRemainingPayableTL`, `BonusOdemeEkle`, `BonusPendingTemizle`, `BonusBittiMi`, `BonusSatinAlMaliyet` — projede hiçbir yerde kullanılmıyor.
- Bonus **state** (hak kalan, kazanç, bütçe kalan, ödenen vb.) ve **limit hesabı** zaten **OyunYoneticisi** içinde: `bonusHakKalan`, `bonusKazanc`, `_bonusBudgetKalanTL`, `GetBonusRemainingPayableTL()`, `RecordBonusPayment()` vb.
- SenaryoServisi / TumbleServisi / CarpanServisi hep OY’nin (ve SenaryoServisi delegasyonunun) `GetBonusRemainingPayableTL`’ini kullanıyor; BonusAyarlari’nınki kullanılmıyor.

Yani BonusAyarlari fiilen **sadece config kaynağı**; runtime mantığı ve state **ölü kod**.

---

## 3. Özet tablo

| Ne | Kullanılıyor mu? | Nerede? |
|----|-------------------|--------|
| Config alanları (BonusHakBaslangic, BonusSpinBekleme, scatter*, bonusBudget*, vb.) | Evet | OY.SyncFromAyarClassesIfPresent → OY alanlarına kopyalanıyor. |
| _bonusHakKalan, _bonusKazanc, _bonusBudgetKalanTL vb. | Hayır | Hiçbir yerde okunmuyor/yazılmıyor. |
| BonusBaslat, GetBonusRemainingPayableTL, BonusOdemeEkle, SpinHakAzalt, BonusPendingTemizle, BonusBittiMi, BonusSatinAlMaliyet | Hayır | Hiçbir yerde çağrılmıyor. |

---

## 4. Sonuç ve seçenekler

- **Durum:** BonusAyarlari’nın sadece **Inspector config** kısmı kullanılıyor; tüm **runtime state ve metodlar** kullanılmıyor (ölü kod).
- **EkonomiAyarlari benzeri:** EkonomiAyarlari’da da sadece 3 alan (BahisMin/Max/Adim) okunuyordu; sonra script kaldırılıp default’a çekildi. BonusAyarlari’da okunan alan sayısı çok (15 civarı), bu yüzden “tamamen silip hepsini OY default’una çekmek” daha büyük bir refaktör.

**Seçenekler:**

| Seçenek | Açıklama |
|---------|----------|
| **A) Sadece ölü kodu temizle** | BonusAyarlari’dan runtime state ve tüm metodları (BonusBaslat, GetBonusRemainingPayableTL, BonusOdemeEkle, SpinHakAzalt, BonusPendingTemizle, BonusBittiMi, BonusSatinAlMaliyet) kaldır. Dosya sadece **config alanları** (Inspector) ile kalır; davranış değişmez, sadece dead code azalır. |
| **B) EkonomiAyarlari gibi kaldır** | SyncFromAyarClasses’taki BonusAyarlari bloğunu sil; bu 15 alanın default değerlerini OY’de (veya tek bir yerde) tanımla. BonusAyarlari.cs’i sil. Sahnede varsa Missing Script kalır; bileşen kaldırılır. |
| **C) Olduğu gibi bırak** | Config kaynağı olarak kalsın; ölü kod ileride temizlenebilir. |

**Öneri:** Önce **A** uygulanabilir (risk düşük, dosya sadeleşir). İstersen sonra **B** ile tamamen kaldırılıp EkonomiAyarlari gibi default’a çekilebilir.
