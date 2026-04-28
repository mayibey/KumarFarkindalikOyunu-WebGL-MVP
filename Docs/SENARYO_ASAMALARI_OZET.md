# Senaryo Gelişim Aşamaları – Detaylı Özet

Bu belge, uygulamadaki 7 senaryo aşamasında **ne yapıldığını**, **geçiş şartlarını** ve **aşama bazlı kuralları** tablo ve açıklamalarla özetler.

---

## 1. Aşama listesi ve temel parametreler

| Aşama | Ad | Temel zorluk | Kazanç oranı (%) | Scatter / bonus notu |
|-------|-----|--------------|-------------------|----------------------|
| **1** | Isındırma / Umut | 5 | 75 | 50 spin sonrası 4 scatter garantisi; scatter şansı ~0.6% |
| **2** | Kontrol bende | 6 | 50 | 75 spin sonrası 4 scatter garantisi; scatter ~1.5% |
| **3** | Az daha / Kayıp kovalama | 7 | 25 | Scatter ~0.6%; near-miss (3 scatter) sık; bonus’ta 100x+ iken tumble yok |
| **4** | Bakiye tükenişi | 8 | 15 | Standart scatter/tumble |
| **5** | Bonus zirve | 9 | 5 | 3. yükleme sonrası ilk bonus: ödeme tavanı 2.5× maliyet |
| **6** | Gerçek kayıp | 10 | 5 | Standart |
| **7** | Finale | 11 | 0 | Ödenebilir limit 0; her spin net kayıp; spin sonrası 1.5 sn bekleme |

*Zorluk 1–12 arası; artan sayı = daha zor (tumble zor, kazanç az). Kazanç oranı `GetKazancOrani()` ile paytable/ödeme havuzu hesaplarında kullanılır.*

---

## 2. Aşama geçiş şartları (ne zaman sonraki aşamaya geçilir?)

| Geçiş | Koşul (OR / AND) | Varsayılan parametreler |
|-------|------------------|---------------------------|
| **1 → 2** | Çıkış şartlarından **en az 2’si** sağlandı **VEYA** bu aşamada atılan spin ≥ 150 | gecis1_spin=200, gecis1_bonusSayisi=2, gecis1_bahisDegisim=3 |
| **2 → 3** | Çıkış şartlarından **en az 2’si** sağlandı **VEYA** bu aşamada atılan spin ≥ 100 | gecis2_spin=300, gecis2_bahisDegisim=5; + 1 kez bakiye %20 düşüş |
| **3 → 4** | Çıkış şartlarından **en az 2’si** sağlandı **VEYA** bu aşamada spin ≥ 120 **VEYA** yükleme sayısı ≥ 2 | gecis3_spin=150, gecis3_bakiyeErimeYuzde=40 |
| **4 → 5** | (Bakiye < 10.000 TL **VE** yükleme ≥ 2) **VEYA** yükleme ≥ 3 | gecis4_bakiyeAltiTL=10000, gecis4_yukleme=3 |
| **5 → 6** | Yükleme ≥ 3 **VE** bu aşamada atılan spin ≥ 100 | gecis5_asamaSpin=100 |
| **6 → 7 (Finale)** | (Toplam spin ≥ 500 **VE** net negatif) **VEYA** yükleme ≥ 3 **VEYA** bakiye ≤ 0 | gecis6_toplamSpinMin=500 |

*Tüm spin sayacı oyun boyunca tek (global); her aşama girişinde “bu aşamada atılan spin” sıfırlanır.*

---

## 3. Çıkış şartları detayı (CikisSartlariniDegerlendir)

Her aşama için “tamamlanan / kalan” şartlar aşağıdaki gibi. Geçişte **en az 2 tamamlanan** kullanılır (1→2, 2→3, 3→4 için; diğerleri yukarıdaki formüle göre).

| Aşama | Şart 1 | Şart 2 | Şart 3 |
|-------|--------|--------|--------|
| **1** | Bu aşamada spin ≥ gecis1_spin (200) | Bonus görülme ≥ gecis1_bonusSayisi (2) | Bahis değişimi ≥ gecis1_bahisDegisim (3) |
| **2** | Bu aşamada spin ≥ gecis2_spin (300) | Bahis değişimi ≥ gecis2_bahisDegisim (5) | 1 kez bakiye %20 düşüş (aşama giriş bakiyesine göre) |
| **3** | Bu aşamada spin ≥ gecis3_spin (150) | Bakiye ≤ aşama giriş bakiyesinin %(100−40)=%60’ı (yani %40 erime) | — |
| **4** | Bakiye < gecis4_bakiyeAltiTL (10.000 TL) | Yükleme sayısı ≥ 2 | — |
| **5** | Yükleme ≥ gecis4_yukleme (3) | Bu aşamada spin ≥ gecis5_asamaSpin (100) | — |
| **6** | Toplam spin ≥ gecis6_toplamSpinMin (500) | Net negatif (bakiye < ilk bakiye veya toplam kayıp > toplam kazanç) | — |
| **7** | Son aşama; geçiş yok | — | — |

---

## 4. Aşama bazlı özel davranışlar (ne yapılıyor?)

| Aşama | Özel davranış | Nerede uygulanıyor |
|-------|----------------|--------------------|
| **1** | Üst üste 3–5 ödeme sonrası rastgele (≈%52) 1–2 spin **zorunlu boş** (ödeme 0). Scatter: 50 spin sonrası garanti 4 scatter. | SenaryoYoneticisi (forcedNoPayKalan, consecutivePayCount), OyunYoneticisi (ShouldForceNoPaySenaryo12), grid dolumunda GrideEnAzDortScatterKoy |
| **2** | Aynı zorunlu boş spin kuralı. Scatter: 75 spin sonrası garanti 4 scatter. Bakiye aşama girişinin %80’ine düşünce “1 kez bakiye %20 düşüş” şartı tamamlanır. | Aynı + _asama2BakiyeYuzde20Dustu; GetScatterChanceFor (75 spin) |
| **3** | “Az daha” near-miss: Son scatter’dan 8–50 spin aralığında %40 ihtimalle gridde tam 3 scatter (bonus yok). Bonus’ta zorla 100x+ çarpan varken **tumble yok**, sadece çarpanlar düşer; ödeme 0. Scatter şansı düşük (~0.6%). | OyunYoneticisi: GrideTamUcScatterKoy, yuksekCarpanVar + AzDahaNearMiss, GetScatterChanceFor |
| **4** | Bakiye ve yükleme sayısına göre 4→5 geçişi; özel scatter/tumble kuralı yok. | SenaryoYoneticisi AsamaGecisiKontrol |
| **5** | 3. yükleme sonrası **ilk** bonus: ödeme tavanı maliyetin 2.5 katı; sonrakiler genel kural (satın alım: maliyet+%10, scatter: maliyetin %10’u). | OyunYoneticisi BaslatBonus (yukleme >= 3 && !_ucuncuYuklemeSonrasiIlkBonusUygulandi) |
| **6** | Finale’e geçiş şartları; bakiye 0’da otomatik log sahnesine gidiş. | SenaryoYoneticisi SpinTamamlandi, AsamaGecisiKontrol |
| **7** | Ödenebilir limit **0** (her spin net kayıp). Her spin sonrası bilgi metni: “Bu spinde X TL kaybettin. Toplam net zarar: Y TL.” Scatter kontrolü sonrası 1.5 sn bekleme. | OdemeServisi GetSpinOdenebilirLimit; DonusAkisServisi WaitForSeconds(1.5f); SenaryoYoneticisi cikisIcinBilgiMetni |

---

## 5. Zorluk ve geçici yumuşatma

- **Temel zorluk:** Aşama 1→5, 2→6, …, 7→11 (GetAsamaTemelZorluk).
- **Yumuşatma:**  
  - Aşama 2’de **bahis artırımından sonra 1–4 spin** içinde zorluk 5’e çekilir.  
  - Aşama 1–2–3’te **yüklemeden sonra 1–20 spin** içinde zorluk en fazla 6 olacak şekilde düşürülür.
- Zorluk, tumble eşiği (8) ve bias (kolay/zor) ile birlikte loglanır (OlayTipi_ZorlukTumbleAyar).

---

## 6. Bonus ödeme tavanları (aşama bazlı)

| Aşama | Satın alınan bonus | Scatter ile tetiklenen bonus |
|-------|---------------------|------------------------------|
| 1, 2, 3, 4, 6 | Maliyet + en fazla %10 | Maliyetin en fazla %10’u |
| 5 | 3. yükleme sonrası ilk bonus: maliyet × 2.5; diğerleri: maliyet + %10 | Maliyetin %10’u |
| 7 | 0 (bonus ödemesi yok) | 0 |

*Maliyet: son satın alım tutarı veya (bahis × bonus satın alma çarpanı).*

---

## 7. Kısa akış özeti

1. **1 – Isındırma:** Nispeten kolay, yüksek kazanç oranı, 50 spinde bir 4 scatter garantisi, üst üste ödemelerden sonra 1–2 zorunlu boş spin.  
2. **2 – Kontrol:** Biraz daha zor, 75 spinde bir 4 scatter garantisi, bakiye %20 düşünce şart işlenir.  
3. **3 – Az daha:** Near-miss (3 scatter) ve bonus’ta yüksek çarpanlı tumble’sız ödeme 0; scatter seyrek.  
4. **4 – Bakiye tükenişi:** Bakiye < 10k + yükleme veya 3 yükleme ile 5’e geçiş.  
5. **5 – Bonus zirve:** 3. yükleme sonrası ilk bonus yüksek tavan; 100 spin + 3. yükleme ile 6’ya geçiş.  
6. **6 – Gerçek kayıp:** 500+ spin ve net negatif (veya 3 yükleme / bakiye 0) ile finale.  
7. **7 – Finale:** Kazanç kapatılır, her spin kayıp, mesajlarla net zarar vurgulanır; bakiye 0’da istatistik (log) sahnesine geçilir.

---

*Belge, `SenaryoYoneticisi.cs`, `OyunYoneticisi.cs`, `DonusAkisServisi.cs`, `OdemeServisi.cs` kodlarına göre üretilmiştir.*
