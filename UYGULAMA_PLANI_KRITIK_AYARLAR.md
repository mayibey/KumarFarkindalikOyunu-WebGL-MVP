# Kritik Ayar ve Spin Akışı — Uygulama Planı

Bu plan, `KRITIK_AYARLAR_VE_SPIN_AKISI_ANALIZI.md` raporundaki eksikleri gidermek için **sırayla, kontrollü** adımları tanımlar.  
Her adımda en fazla 2–4 dosya değiştirilir; davranış değişikliği yalnızca planlanan maddelerle sınırlı tutulur.

**Yedek:** Değişikliklere başlamadan önce alındı. Geri dönmek için: **`BACKUP_KRITIK_AYAR_20250228 geri yükle`** yaz.

---

## Faz 1: Limit ve tutarlılık (öncelikli)

### Adım 1.1 — FillRandomAll’daki “tumble bastırma” kaldır
- **Amaç:** Limit aşılınca grid’i “güvenli” diye değiştirip tumble’ı gizleme; güven kaybı riskini kaldır.
- **Dosya:** `Assets/Scripts/Services/IzgaraServisi.cs`
- **Yapılacak:** `FillRandomAll(int odenebilirLimit)` içinde, `hamKazanc > kalanLimitTL` olduğunda tüm grid’i yeniden yazan blok **tamamen kaldırılacak**. Limit aşımı bu adımda sadece “kontrol dışı” kalacak; Adım 1.2/1.3 ile çözülecek.
- **Kontrol:** Fill sonrası grid sadece rastgele; “limit aşınca tumble’sız grid” üretilmiyor.

### Adım 1.2 — Normal spin tumble döngüsüne ödenebilir limit kontrolü
- **Amaç:** Normal spinde tumble toplamı `_spinOdenebilirLimit`’i aşmasın; tek spin ödemesi %10 kuralına uysun.
- **Dosyalar:** `Assets/Scripts/Services/TumbleAkisServisi.cs`, `Assets/Scripts/OyunYoneticisi.cs` (gerekirse context)
- **Yapılacak:**  
  - `ITumbleAkisBaglami`’ye `int GetSpinOdenebilirLimit()` (veya mevcut spin limitini döndüren bir erişim) eklenir.  
  - `TumbleLoop` içinde **bonus dışında** da her turdan önce: `(spinKazancHam + bu_tur_ham) * çarpan > GetSpinOdenebilirLimit()` ise döngü **break** (yeni tumble yapılmaz, mevcut kazançla devam).  
  - Böylece normal spinde toplam ödeme limiti aşılmaz.
- **Kontrol:** Normal spin ile birkaç tumble sonrası toplam ödemenin limiti geçmediği test edilir.

### Adım 1.3 — Spin sonucunu önceden hesapla (simülasyon + re-roll) [büyük adım]
- **Amaç:** Spin butonuna basıldığında sonuç belli olsun; limit aşacaksa grid yeniden üretilsin; kullanıcı sadece geçerli sonucu görsün.
- **Dosyalar:** Yeni/mevcut: `TumbleServisi` veya ayrı bir “spin simülatör” katmanı, `IzgaraServisi`, `DonusAkisServisi`, `OyunYoneticisi`.
- **Yapılacak:**  
  1. **Simülasyon modu:** Grid doldurma + tumble (cluster bul, kazanç ekle, hücreleri temizle, refill) animasyonsuz bir döngüde çalışacak şekilde ayrılır; toplam ham kazanç + çarpan ile nihai ödeme hesaplanır.  
  2. **Re-roll:** Toplam ödeme > ödenebilir limit ise grid **baştan** üretilir, simülasyon tekrarlanır; limiti aşan sonuç kullanılmaz.  
  3. **Gösterim:** Geçerli (onaylanmış) grid ve tumble adımları ya tek seferde ya da adım adım animasyonla kullanıcıya oynatılır; kullanıcı sadece bu sonucu görür.
- **Kontrol:** Limit küçük (örn. 500 TL) verilip spin atıldığında hem “tumble varken tumble yok” hem “limit aşan ödeme” olmadığı doğrulanır.

### Adım 1.4 — Bonus tumble’da “ortada kes” yerine simülasyonla uyum
- **Amaç:** Bonus’ta da limit aşan tumble olmasın ama “ekranda cluster varken tumble yapmıyormuş” hissi olmasın.
- **Yapılacak:** Bonus spin için de (Adım 1.3’e benzer şekilde) önce simüle et; toplam > bonus kalan limit ise re-roll. Sonucu sabitledikten sonra animasyonla oynat. Böylece “break ile ortada kes” kaldırılır.
- **Kontrol:** Bonus’ta limit dolana kadar tumble’lar tutarlı; limit aşan adım yok.

---

## Faz 2: Sembol olasılıkları ve zorluk

### Adım 2.1 — Sembol ağırlığı ↔ PayTable (ters ağırlık)
- **Amaç:** Yüksek ödeme veren sembolün düşme olasılığı daha az olsun.
- **Dosyalar:** `Assets/Scripts/Services/IzgaraServisi.cs`, `TumbleAyarlari` veya pay tablosuna erişen servis.
- **Yapılacak:** `RandomNonScatterSymbol` (ve gerekirse refill’de kullanılan seçim) ağırlık hesabına **PayTable/ödeme katsayısı** eklenir: sembol `i` için yüksek ödeme → düşük ağırlık (ters oran veya 1/pay gibi). Scatter/çarpan hariç normal semboller için uygulanır.
- **Kontrol:** Yüksek ödemeli sembolün grid’de daha seyrek göründüğü gözlemlenir.

### Adım 2.2 — Zorluk 4’te limit bilgisinin tumble/simülasyona girmesi
- **Amaç:** Zorluk 4’te “limit (örn. 500 TL) altında tumble olasılığı yüksek” olsun.
- **Dosyalar:** Simülasyon/re-roll mantığı (Adım 1.3), zorluk ve limit entegrasyonu; gerekirse `ZorlukServisi` / `OyunYoneticisi`.
- **Yapılacak:** Zorluk 4 (veya kolay aralık) için re-roll/simülasyon kriterinde veya refill ağırlığında “ödenebilir limit” bilgisi kullanılır; limit düşükken tumble’lı (ama limiti aşmayan) sonuçların seçilme olasılığı artırılır. Detay, Adım 1.3 mimarisine göre netleştirilir.
- **Kontrol:** Zorluk 4, düşük limit ile sık ama limit aşmayan tumble’lar görülür.

### Adım 2.3 — Zorluk 12’de tumble’ı neredeyse sıfırlama (isteğe bağlı)
- **Amaç:** Zorluk 12’de neredeyse hiç tumble olmasın.
- **Yapılacak:** Ya `minClusterSize` zorluğa göre 10–12 gibi yükseltilir ya da mevcut hard bias yeterince güçlendirilir. Tercih mimariye göre.
- **Kontrol:** Zorluk 12’de çok seyrek tumble.

### Adım 2.4 — Zorluk 4’te cluster boyutu dağılımı (8–9 çoğunluk, 10–11 az, 12+ çok az)
- **Amaç:** Aynı meyve adedi “deli gibi” artmasın; çoğunlukla 8–9, az 10–11, çok az 12+.
- **Dosyalar:** Refill/cluster tarafı: `IzgaraServisi`, `TumbleAyarlari` veya simülasyon.
- **Yapılacak:** Refill veya cluster kabulünde cluster boyutuna göre ağırlık: 8–9 ağır, 10–11 orta, 12+ düşük. Zorluk 4 (veya kolay) için açıkça uygulanır.
- **Kontrol:** Zorluk 4’te cluster boyutları dağılıma uygun.

---

## Sıra ve bağımlılıklar

| Sıra | Adım | Bağımlılık |
|------|------|------------|
| 1 | 1.1 FillRandomAll tumble bastırma kaldır | — |
| 2 | 1.2 Normal spin tumble’da limit kontrolü | — |
| 3 | 1.3 Spin simülasyonu + re-roll (normal) | 1.1, 1.2 ile uyumlu |
| 4 | 1.4 Bonus’ta simülasyon + re-roll | 1.3 |
| 5 | 2.1 Sembol ağırlığı ↔ PayTable | — |
| 6 | 2.2 Zorluk 4 + limit | 1.3 |
| 7 | 2.3 Zorluk 12 (isteğe bağlı) | — |
| 8 | 2.4 Cluster boyutu dağılımı (8–9 / 10–11 / 12+) | Refill mantığı |

**Önerilen uygulama sırası:** 1.1 → 1.2 → 1.3 → 1.4 → 2.1 → 2.2 → (2.3) → 2.4.  
Her adımdan sonra derleme ve kısa oyun testi yapılır; bozulma yoksa bir sonraki adıma geçilir.

---

## Geri çağırma

Herhangi bir adımda geri dönmek için:

**`BACKUP_KRITIK_AYAR_20250228 geri yükle`**

Yedek konumu: `_Yedek_KritikAyar_20250228/Scripts` → `Assets/Scripts`.
