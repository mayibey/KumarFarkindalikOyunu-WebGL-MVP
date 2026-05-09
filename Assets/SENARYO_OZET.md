# Scripted Senaryo — Tam Özet Tablosu

**Kaynaklar:** `ScriptedSenaryoAssetUreteci.cs` (asset üretici), `AnlaticiSeritKopru.cs` (aşama profili), `ScriptedSpinYoneticisi.cs` (A6 dinamik), modal/popup/final UI sınıfları.

**Pedagojik eğri:** 50K → 60K → 75K → 70K → 55K → ~30K → 10K → 0 (~61 spin, anlatici plan kommentinden).

---

## 1. Aşama Profili — `AnlaticiSeritKopru.cs`

| Aşama | Index | Ad | Önerilen Bahis | Hedef Spin | Anlatici Eğilim | Max Çarpan |
|---|---|---|---:|---:|---:|---:|
| A1 | 0 | Isındırma ve Umut | 500 | 10 | 95 | 5.0× |
| A2 | 1 | Kontrol Bende Hissi | 1000 | 10 | 90 | 3.5× |
| A3 | 2 | Geri Kazanabilirim | 1500 | 8 | — | — |
| A4 | 3 | Şansım Döndü | 2500 | 8 | — | — |
| A5 | 4 | Sonunu Düşünen | 4000 | 10 | — | — |
| A6 | 5 | Borç Sonrası | 2500 | 10 | — | — |
| A7 | 6 | Final / Tükeniş | 1500 | 999 (cutscene) | — | — |

**Kritik not:** Asset'te A1-A5 sadece **kritik spinler** tanımlı (8+8+8+5+5 = 34 spin). Hedef sayısına ulaşmadan asset spinleri biterse kalan spinler RNG fallback'e düşer (mevcut Anlatici eğilim+maxCarpan ile çalışır). Bakiye yetersizliği oluşursa `BasaArayisAkisi` (A5 sonu) veya `DonguAkisi` (A6 sonu) tetiklenir.

---

## 2. A1 — Isındırma ve Umut (bahis 500)

| Spin | Tip | Brüt | Net | Bakiye Sonu | Tetik |
|---:|---|---:|---:|---:|---|
| 1 | Kazanç (3 tumble: hindistan→elma→üzüm) | 1500 | +1000 | 51.000 | — |
| 2 | Sıfır | 0 | -500 | 50.500 | — |
| 3 | Kazanç (8 elma + 8 üzüm + ×2 çarpan) | 2500 | +2000 | 52.500 | — |
| 4 | Kazanç (8 üzüm tek cluster) | 750 | +250 | 52.750 | **Modal A1_S4** |
| 5 | Kazanç (üzüm→elma 2 tumble) | 1250 | +750 | 53.500 | — |
| 6 | NearMiss (7 üzüm) | 0 | -500 | 53.000 | — |
| 7 | **MEGA WIN** (8 üzüm + 8 elma + ×5 çarpan) | 6250 | +5750 | 58.750 | **Modal A1_S7** |
| 8 | Kazanç (10 elma + 8 hindistan) | 1750 | +1250 | 60.000 | **Modal A1_S8** |
| 9-10 | RNG fallback (asset'te yok) | ? | ? | ~60K | — |

---

## 3. A2 — Kontrol Bende Hissi (bahis 1000)

| Spin | Tip | Brüt | Net | Bakiye Sonu | Tetik |
|---:|---|---:|---:|---:|---|
| 1 | Kazanç (tek cluster elma) | 1000 | 0 | 60.000 | — |
| 2 | Kazanç (tek cluster hindistan) | 500 | -500 | 59.500 | — |
| 3 | NearMiss (7 üzüm + 7 elma) | 0 | -1000 | 58.500 | **Modal A2_S3** |
| 4 | Kazanç (tek cluster üzüm) | 1500 | +500 | 59.000 | — |
| 5 | Sıfır | 0 | -1000 | 58.000 | — |
| 6 | NearMiss (3 scatter) | 0 | -1000 | 57.000 | **Modal A2_S6** |
| 7 | Kazanç (elma→hindistan 2 tumble) | 750 | -250 | 56.750 | — |
| 8 | Sıfır | 0 | -1000 | 55.750 | **Modal A2_S8** |
| 9-10 | RNG fallback | ? | ? | ~55-60K | — |

---

## 4. A3 — Geri Kazanabilirim (bahis 1500, 8 spin tam tanımlı)

| Spin | Tip | Brüt | Net | Bakiye Sonu | Tetik |
|---:|---|---:|---:|---:|---|
| 1 | Sıfır | 0 | -1500 | ~58.500 | — |
| 2 | NearMiss (7 üzüm) | 0 | -1500 | 57.000 | — |
| 3 | NearMiss (7 elma) | 0 | -1500 | 55.500 | **Modal A3_S3** |
| 4 | NearMiss (7 üzüm + 7 elma) | 0 | -1500 | 54.000 | — |
| 5 | Bahis İadesi (tek cluster hindistan) | 750 | -750 | 53.250 | — |
| 6 | NearMiss (3 scatter) | 0 | -1500 | 51.750 | **Modal A3_S6** |
| 7 | Sıfır | 0 | -1500 | 50.250 | — |
| 8 | NearMiss (7 üzüm + 7 elma + 3 scatter) | 0 | -1500 | 48.750 | **Modal A3_S8** |

---

## 5. A4 — Şansım Döndü (bahis 2500, asset'te 5 spin)

| Spin | Tip | Brüt | Net | Bakiye Sonu | Tetik |
|---:|---|---:|---:|---:|---|
| 1 | Sıfır | 0 | -2500 | ~46.250 | — |
| 2 | NearMiss (7 üzüm) | 0 | -2500 | 43.750 | **Modal A4_S2** |
| 3 | NearMiss (7 üzüm + 7 elma) | 0 | -2500 | 41.250 | — |
| 4 | Sıfır | 0 | -2500 | 38.750 | **Modal A4_S4** |
| 5 | **MEGA WIN** (8 ARMUT + ×100 çarpan) | 20.000 | +17.500 | 56.250 | **Modal A4_S5** |
| 6-8 | RNG fallback (3 spin) | ? | ? | ~50-55K | — |

---

## 6. A5 — Sonunu Düşünen (bahis 4000 anlatici, 2000 asset; 5 spin asset)

| Spin | Tip | Brüt | Net | Bakiye Sonu | Tetik |
|---:|---|---:|---:|---:|---|
| 1 | Sıfır | 0 | -4000 | ~52.250 | **Modal A5_S1** |
| 2 | Kazanç (8 üzüm + ×2 çarpan) | 6000 | +2000 | 54.250 | — |
| 3 | Sıfır + ×500 çarpan kaçtı (cluster yok) | 0 | -4000 | 50.250 | **Modal A5_S3** + carpanKactiFlag |
| 4 | **BONUS TETİK** | 0 | (bahis düşmez, popup açılır) | 50.250 | **Cazip Pop-up** + bonus oyun başlar |
| ↳ Pop-up onay | tüm bakiye 0'a düşer | — | -50.250 | 0 | Bakiye → 0 |
| ↳ Bonus oyun (10 free spin) | bahis override 1000 → motor doğal RTP | ~2-3K | +2-3K | ~2-3K | HUD overlay |
| 5 | Kazanç (cüzi ödeme) | 800 | -3200 | bakiye düşer | **Modal A5_S5** |
| 6+ | Bakiye yetersiz → **BasaArayisAkisi** | — | — | < 4000 | **Eğitmen modal + Düşünce balonu + Yükleme paneli** |

---

## 7. A6 — Borç Sonrası (dinamik, bahis 2500, hedef 10 spin)

**`ScriptedSpinYoneticisi.UretA6DinamikSpin` mantığı:**
- Her spin: brüt = 0 (Sıfır tip), 6 sembol × 5'er hücre dolgu, hiç cluster yok
- Bahis 2500, modal yok, bonus yok
- Plan: bakiye 50.800 → 0 (~20 spin, ama hedef 10 spin sayacı)

| Spin | Tip | Brüt | Net | Bakiye Sonu (Borç +50K sonrası ~50.250 başlangıç) | Tetik |
|---:|---|---:|---:|---:|---|
| 1-10 | Sıfır (dinamik) | 0 | -2500 her | 50.250 → 25.250 | — |
| 11+ (asama 5 spin>0) | Bakiye yetersiz | — | — | < 2500 | **DonguAkisi → Tukenis** |

---

## 8. A7 — Final / Tükeniş (cutscene, spin atılmaz)

`ScriptedFinalEkrani.GosterFinalEkrani` — `AnlaticiSeritKopru.AktifAsama == 6` olunca otomatik açılır.

**Final ekran içeriği:**
- Başlık: **"OYUN BİTTİ"** (kırmızı)
- İstatistikler:
  - "Yatırdığın toplam: 100.000 TL" (BorcAlindi=true ise) veya 50.000 TL
  - "Geri aldığın: {sonBakiye} TL"
  - "Net kayıp: {toplamKayip} TL" (kırmızı)
  - "Toplam spin: {AnlaticiSeritKopru.ToplamSpin}"
- Pedagojik mesaj: *"Bu yaşadığın senaryo Türkiye'de her gün binlerce kişinin başına geliyor. Online kumar bağımlılığı bir hastalıktır; yardım almak güçlü bir farkındalıktır."*
- Yeşilay Yardım Hattı: **0850 222 0 191**
- "YENİDEN BAŞLA" butonu — sahne reload (state reset)

---

## 9. Modal Mesajları — Tam Metin

### A1 — Isındırma ve Umut
- **Spin 4** (`M_A1_S4`):
  > "Oyuncu ilk kazançları yaşıyor. Beyninde dopamin salgılanıyor. Bu his, sonraki saatlerce oyun oynamanın yakıtı olacak."
- **Spin 7** (`M_A1_S7`):
  > "Sistem büyük bir kazanç yaşatmak üzere. Geçmiş kayıpları unutturacak bir an gelecek."
- **Spin 8** (`M_A1_S8`):
  > "İlk kazanç en tehlikeli başlangıçtır."

### A2 — Kontrol Bende Hissi
- **Spin 3** (`M_A2_S3`):
  > "Oyuncu artık 'oyunun mantığını çözdüğünü' düşünmeye başlıyor. Aslında kazançların ne zaman geleceğini sistem belirliyor."
- **Spin 6** (`M_A2_S6`):
  > "Kontrol yanılsaması — oyuncu kendini şanslı hissediyor, kaybetme ihtimalini küçümsüyor."
- **Spin 8** (`M_A2_S8`):
  > "Sen oyunu yönettiğini düşünürken, oyun seni adım adım içine çekiyor."

### A3 — Geri Kazanabilirim
- **Spin 3** (`M_A3_S3`):
  > "İlk ciddi kayıplar yaşanıyor. Amaç para kazanmaktan çıktı, kayıpları telafi etmeye dönüştü."
- **Spin 6** (`M_A3_S6`):
  > "Oyuncu kayıpları geri kazanmak için daha fazla risk alıyor. Mantıklı düşünme yetisini kaybediyor."
- **Spin 8** (`M_A3_S8`):
  > "Bir tur daha = bir kayıp daha."

### A4 — Şansım Döndü
- **Spin 2** (`M_A4_S2`):
  > "Üst üste kayıplar oyuncuyu yıpratıyor. Sistem şimdi büyük bir vuruş hazırlıyor."
- **Spin 4** (`M_A4_S4`):
  > "Oyuncu pes etmek üzere. Tam bu noktada büyük kazanç gelecek — bu kasıtlı bir manipülasyon."
- **Spin 5** (`M_A4_S5`, MEGA WIN sonrası):
  > "Bir büyük kazanç tüm geçmiş kayıpları gölgeliyor. Oyuncu 'şansının döndüğüne' inanıyor. Bu büyük kazancın amacı yeni bahisleri tetiklemek."

### A5 — Sonunu Düşünen
- **Spin 1** (`M_A5_S1`):
  > "Bahis arttı, beklenti arttı. Adrenalin salgılanıyor."
- **Spin 3** (`M_A5_S3`, çarpan kaçtı):
  > "x500 çarpan ekrana düştü ama eşleşme olmadı. Oyuncuda 'bir daha denemek' arzusu yaratıldı. Sistem fırsat sunmak üzere."
- **Spin 4** (`M_A5_S4_BONUS`, cazip pop-up):
  > "🎰 ŞANSLI SAATİNDESİN! Bonus oyun aktif edildi. Bakiyenin tamamını yatır, x10000 kazanma şansını kaçırma. SINIRLI TEKLİF."
- **Spin 5** (`M_A5_S5`, bonus sonrası):
  > "Oyuncu tüm bakiyesini bonus oyuna yatırdı. Geri aldığı miktar yatırdığının %1'i. Bu sömürünün adı 'değişken oranlı pekiştireç'."

---

## 10. UI Elemanları — Tam Metin

### Cazip Pop-up — `ScriptedBonusTuzagiPopup`
- **Başlık:** "🎰 ŞANSLI ANINDASIN!"
- **Açıklama:**
  > "Tüm bakiyeni bonus oyuna yatır,
  > **10.000 KATI KAZANMA**
  > şansını yakala!
  >
  > *Bu fırsat bir daha karşına çıkmayabilir!*"
- **Buton:** "BONUS AL — TÜM BAKİYE ({tutar} TL)" — onay sonrası bakiye 0'a düşer

### Yükleme Paneli — `ScriptedYuklemePaneli`
- **Başlık:** "Borç al — devam etmek istiyor musun?"
- **Açıklama:**
  > "Aileden, kredi kartından veya iş arkadaşından borç alarak oyuna devam etmek istiyorsun. Borçla kumar oynamak bağımlılığın klasik göstergelerinden biridir."
- **Buton:** "BORÇ AL — 50.000 TL"

### A5 sonu Eğitmen Modal — `AnlaticiSeritKopru.BasaArayisAkisi`
> "Oyuncu artık paranın bittiğini fark etti.
>
> Şimdi başka yerden para bulma arayışında. Yalan söylemeye başlıyor — yakınlarına, akrabalarına, arkadaşlarına...
>
> Bu, kumar bağımlılığının yıkıcı evresidir. Bir sonraki ekran o anı temsil ediyor."

### Düşünce Balonu — Sol-Üst Başlık (`ScriptedDusunceBalonu` UIYarat)
> **"BAŞKA YERDEN PARA BULMA ARAYIŞI"** (sarı, bold)

### Düşünce Balonu — 4 Kalıcı Yalan
1. **SOL ÜST** (-420, +280): "Çocuğum hasta, acil para lazım..."
2. **SAĞ ÜST** (+420, +280): "Sadece kısa süre için, hemen ödeyeceğim..."
3. **SOL** (-490, +30): "Kız kardeşim borca girdi, yardım etmem gerek..."
4. **SAĞ** (+490, +30): "Bu sefer kazanırsam hepsini öderim, söz veriyorum..."

### Düşünce Balonu — Paralel Asistan Modal (sol-altta, balonla eş zamanlı)
> "Bu aşamada oyuncu çevresindeki kişilere yalan söyleyerek veya bankalardan kredi çekerek para bulmaya çalışır.
>
> Burada amaç eski kayıpların telafisidir. Ancak bu, kumar bağımlılığının en yıkıcı evresidir — borç katlanarak büyür, ilişkiler bozulur, hayatlar mahvolur."

### A6 Sonu Döngü Modal — `AnlaticiSeritKopru.DonguAkisi`
> "Bakiye yine bitti.
>
> Bu oyuncu şimdi **A1'e geri dönecek**. *'Belki bu sefer şanslıyım'* diye düşünüyor. *'Bir kerelik daha denersem...'* diyerek kendini kandırıyor.
>
> İşte bağımlılığın özü budur: **KAYIP → BORÇ → KAYIP → BORÇ**. Sonsuz döngü.
>
> Gerçek hayatta bu döngü ailelerin yıkımıyla, evlerin satılmasıyla, hayatların mahvolmasıyla biter.
>
> Sonraki ekranda yaşanan toplam kayıp gösteriliyor."

### A7 Final Ekran — `ScriptedFinalEkrani`
- **Başlık:** "OYUN BİTTİ"
- **İstatistik:** "Yatırdığın toplam: {tutar}\nGeri aldığın: {bakiye}\nNet kayıp: {kayıp}\nToplam spin: {sayı}"
- **Pedagojik:**
  > "Bu yaşadığın senaryo Türkiye'de her gün binlerce kişinin başına geliyor.
  > Online kumar bağımlılığı bir hastalıktır; yardım almak güçlü bir farkındalıktır.
  >
  > **Yeşilay Yardım Hattı: 0850 222 0 191**"
- **Buton:** "YENİDEN BAŞLA" — sahne reload

### Modal Genel Stil — `ScriptedModalKopru`
- Başlık: "BİLGİLENDİRİCİ ASİSTAN" (altın, bold, 18px)
- Karakter: `Resources/egitmenyuz.png` (sol-altta, 220×280)
- Sağ alt köşede TAMAM butonu (100×40, koyu arka + 1.5px sarı border, typewriter sonu fade-in)

---

## 11. Akış Şeması — Senaryo Sırası

```
A1 (10 spin, bahis 500)
  ├─ Spin 4: dopamin modal
  ├─ Spin 7: MEGA WIN modal
  └─ Spin 8: tehlikeli başlangıç modal
A2 (10 spin, bahis 1000)
  ├─ Spin 3, 6, 8: kontrol yanılsaması modal
A3 (8 spin, bahis 1500)
  ├─ Spin 3, 6, 8: kayıp telafisi modal
A4 (8 spin, bahis 2500)
  ├─ Spin 2, 4: yıpranma modal
  └─ Spin 5: MEGA WIN ×100 (manipülasyon vuruşu)
A5 (10 spin, bahis 4000)
  ├─ Spin 1: adrenalin modal
  ├─ Spin 3: çarpan kaçtı modal
  ├─ Spin 4: ⚠️ CAZİP POP-UP (bonus tuzağı)
  │   ├─ Onay → bakiye 0
  │   ├─ Bonus oyun (10 free spin, bahis override 1000)
  │   └─ Motor doğal RTP → ~2-3K geri
  ├─ Spin 5: cüzi ödeme modal (sömürü)
  └─ (bakiye < bahis):
       ├─ EĞİTMEN MODAL (para arayışı)
       ├─ DÜŞÜNCE BALONU (4 yalan + paralel modal + sol-üst başlık)
       └─ YÜKLEME PANELİ (BORÇ AL — 50K)
A6 (10 spin, bahis 2500, dinamik kayıp)
  └─ Bakiye 0 → DÖNGÜ MODAL (KAYIP→BORÇ döngüsü)
A7 — FINAL EKRAN (cutscene)
  └─ İstatistik + Yeşilay 0850 222 0 191 + YENİDEN BAŞLA
```

---

## 12. Dinamik Üretim Notları

### A6 (`ScriptedSpinYoneticisi.UretA6DinamikSpin`)
- Bahis sabit: 2500
- Brüt sabit: 0 (her spin sıfır kayıp)
- Grid: 6 sembol × 5 hücre döngüsel dolgu, cluster oluşmayacak şekilde
- Tumble yok, modal yok, bonus yok
- 10 spin sonu bakiye ~25K (50K - 25K)
- 20 spin sonu bakiye ~0 (kullanıcı RNG dalga için biraz farklı)

### A6 → A7 geçişi (`AnlaticiSeritKopru.SpinTamamlandi`)
- `_aktifAsama=5 && _aktifSpin>0 && bakiye<2500` → `DonguAkisi` coroutine
- Modal kapanınca `Tukenis()` → `_aktifAsama=6` → `ScriptedFinalEkrani` Update'te algılar

---

## 13. Bilinen Uçlar / Notlar

- **Asset/Anlatici bahis çelişkisi:** Asset üreticide `BAHIS_A4=1000`, `BAHIS_A5=2000` ama anlatici plan `2500/4000`. Anlatici bahisi runtime'da geçerli; asset değerleri sadece referans.
- **A1-A2 RNG fallback:** Hedef 10, asset 8 — 9. ve 10. spinler RNG ile dolar (Anlatici eğilim+maxCarpan).
- **A4 RNG fallback:** Hedef 8, asset 5 — 6-8 RNG.
- **A5 RNG fallback:** Hedef 10, asset 5 — bonus tuzağı sonrası 6-10 RNG ama bakiye genelde tüketilmiş, BasaArayisAkisi tetiklenir.
- **A4 Spin 5 MEGA ×100:** Pedagojik manipülasyon vuruşu (`M_A4_S5` modal: "büyük kazancın amacı yeni bahisleri tetiklemek").
- **Bonus tuzağı backend bahis override:** A5 Spin 4'te bonus oyun başlarken bahis runtime'da 1000 TL'ye düşürülür → motor RTP × 1000 × 10 spin ≈ 2-3K. Cap override veya manuel düzeltme YOK; pedagojik "az ödeme" doğal akışla gelir. Bonus bittiğinde bahis 4000'e geri yüklenir.
- **A5 Spin 4 UI override:** BahisText "Bahis: TÜM BAKİYE" gösterir (backend 1000, manipülasyon hissi).
- **iframe gizleme:** Tüm modal/popup/yükleme/balon/final açıkken `AnlaticiSeritKopru.Gizle()` çağrılır (referans counter); Unity Canvas overlay'leri sol HTML iframe altında kalmaz.
