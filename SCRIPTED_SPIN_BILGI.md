# SCRIPTED_SPIN_BILGI.md

> **Amaç:** Bu rapor, "KumarFarkindalikOyunu" Unity projesindeki slot mekaniğinin tüm matematiksel ve görsel parametrelerini, senaryo bazlı scripted spin sistemi kuracak başka bir AI'ya self-contained bir referans olarak sunar.
> **Branch:** `refactor-paket8` (doğrulandı: `git rev-parse --abbrev-ref HEAD` → `refactor-paket8`)
> **Working dir:** `D:\KumarFarkindalikOyunu`
> **Hazırlayan:** Claude Code (Opus 4.7), 2026-05-04

> **Önemli not:** Aşağıda her başlık, ilgili kaynağın **dosya yolu + satır aralığı** ile etiketlidir. "bilmiyorum" yazılmadı; ulaşılamayan bilgiler için hangi dosyaya bakıldığı belirtildi. Sahnedeki Inspector override değerleri (örn. ScatterIndex 7→8) sahne dosyasından doğrulandı.

---

## 1. PAYTABLE / ÖDEME TABLOSU

### 1.1 Kaynak

`tumbleAyarlari` bir **MonoBehaviour** (ScriptableObject değil), sahnede `Oyun_Sistemleri/TumbleAyarlari` GameObject'i üzerinde duruyor.
**Dosya:** `Assets/Scripts/TumbleAyarlari.cs` (452 satır)
**Sahne:** `Assets/Scenes/03_SenaryoluOyun.unity` satır 11785–11816 (Inspector override değerleri).

Kod default'ları (TumbleAyarlari.cs satır 16–22):

```csharp
[Header("Pay Table - 8-9 Meyve")]
public float[] PayTable_8_9   = new float[9] { 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.8f, 1.0f, 1.5f, 0f };
public float[] PayTable_10_11 = new float[9] { 0.5f, 0.6f, 0.8f, 1.0f, 1.5f, 2.0f, 3.0f, 5.0f, 0f };
public float[] PayTable_12Plus= new float[9] { 1.0f, 1.5f, 2.0f, 2.5f, 3.0f, 5.0f, 10.0f, 25.0f, 0f };
public int ScatterIndex = 7;
```

**Sahnede aktif değerler** (03_SenaryoluOyun.unity satır 11785–11816):

```yaml
MinClusterSize: 8
PayTable_8_9:    [0.2, 0.3, 0.4, 0.5, 0.6, 0.8, 1.0, 1.5, 0]
PayTable_10_11:  [0.5, 0.6, 0.8, 1.0, 1.5, 2.0, 3.0, 5.0, 0]
PayTable_12Plus: [1.0, 1.5, 2.0, 2.5, 3.0, 5.0, 10.0, 25.0, 0]
ScatterIndex: 8        # Script default 7'di; sahne 8'e override etmiş!
```

> **Kritik:** Sahnedeki `ScatterIndex` değeri **8** (yıldız sembolü). Kod default'u `7` (üzüm) → sahnede override edilmiş. Aşağıdaki sembol eşlemesini bu sırayla okuyun.

### 1.2 Tüm sembolün tüm cluster boyutu için ödemeleri (× bahis)

> Tablo: hangi sembol kaç adet eşleştirildiğinde bahisin kaç katı öder. Tüm değerler **× bahis** (sabit TL değil — `total += pay * bahis;` kodunda doğrulandı, satır 145).
>
> Sembol adları, sahnedeki `sembolSpriteListesi` slot sırasına göre belirlendi (bkz. Bölüm 2).

| Index | Sembol         | 6–7 (efektif min) | 8–9   | 10–11 | 12+   |
|-------|----------------|-------------------|-------|-------|-------|
| 0     | Armut          | 0.5×0.2 = 0.1×    | 0.2×  | 0.5×  | 1.0×  |
| 1     | Çilek          | 0.5×0.3 = 0.15×   | 0.3×  | 0.6×  | 1.5×  |
| 2     | Erik           | 0.5×0.4 = 0.2×    | 0.4×  | 0.8×  | 2.0×  |
| 3     | Hindistancevizi| 0.5×0.5 = 0.25×   | 0.5×  | 1.0×  | 2.5×  |
| 4     | Karpuz         | 0.5×0.6 = 0.3×    | 0.6×  | 1.5×  | 3.0×  |
| 5     | Muz            | 0.5×0.8 = 0.4×    | 0.8×  | 2.0×  | 5.0×  |
| 6     | Elma           | 0.5×1.0 = 0.5×    | 1.0×  | 3.0×  | 10.0× |
| 7     | Üzüm           | 0.5×1.5 = 0.75×   | 1.5×  | 5.0×  | 25.0× |
| 8     | Yıldız (SCATTER)| 0×              | 0×    | 0×    | 0×    |

> **6–7 cluster** sadece `effectiveMinSize` parametresi 6 veya 7 verildiğinde devreye girer (CalculateWinWithOwnPayTable satır 127: `carpan = (minPay <= 7 && count <= 7) ? 0.5f : 1f`). Normal akışta `minClusterSize` = `TUMBLE_SABIT_ESIK` = 8. Yani **6–7 cluster sadece düşük zorluk / özel scripted senaryolarda kullanılır**.
>
> **30+'lı küme tablosu YOK.** Cluster ne kadar büyürse büyüsün, 12+ tablosundan ödeme yapılır (count ≤ 11 değilse PayTable_12Plus). Yani 30 tane elma = 10×bahis (12+ tablosu).

### 1.3 `CalculateWinWithOwnPayTable` metodunun TAM kodu

**Dosya:** `Assets/Scripts/TumbleAyarlari.cs:99-149`

```csharp
/// <summary>PayTable ile kazanç hesaplar. effectiveMinSize 6/7 ise 6-7 sembol 8-9 tablosunun yarısıyla ödenir (düşük zorluk).</summary>
public virtual int CalculateWinWithOwnPayTable(List<Vector2Int> removed, int[,] grid, int satir, int sutun, int bahis, int effectiveMinSize = 0)
{
    if (removed == null || grid == null) return 0;

    float[] payTable = PayTable_8_9;
    float[] payTable10_11 = PayTable_10_11;
    float[] payTable12Plus = PayTable_12Plus;
    int minPay = (effectiveMinSize > 0 && effectiveMinSize < MinClusterSize) ? effectiveMinSize : MinClusterSize;

    Dictionary<int, int> counts = new Dictionary<int, int>();
    for (int i = 0; i < removed.Count; i++)
    {
        int sym = grid[removed[i].x, removed[i].y];
        if (sym < 0) continue;
        if (!counts.ContainsKey(sym)) counts[sym] = 0;
        counts[sym]++;
    }

    float total = 0f;
    foreach (var kv in counts)
    {
        int sym = kv.Key;
        int count = kv.Value;

        if (count < minPay) continue;

        float pay = 0f;
        float carpan = (minPay <= 7 && count <= 7) ? 0.5f : 1f;
        if (count <= 7 && minPay <= 7)
        {
            if (payTable != null && sym >= 0 && sym < payTable.Length) pay = payTable[sym] * carpan;
        }
        else if (count <= 9)
        {
            if (payTable != null && sym >= 0 && sym < payTable.Length) pay = payTable[sym];
        }
        else if (count <= 11)
        {
            if (payTable10_11 != null && sym >= 0 && sym < payTable10_11.Length) pay = payTable10_11[sym];
        }
        else
        {
            if (payTable12Plus != null && sym >= 0 && sym < payTable12Plus.Length) pay = payTable12Plus[sym];
        }

        total += pay * bahis;
    }

    return Mathf.RoundToInt(total);
}
```

**Sembol bazlı net ödeme örnekleri (bahis × katsayı):**

- **Armut (0):** 8'li → 0.2× bahis, 9'lu → 0.2×, 10'lu → 0.5×, 11'li → 0.5×, 12+ → 1.0×.
- **Elma (6):** 8'li → 1.0×, 10'lu → 3.0×, 12+ → 10.0×.
- **Üzüm (7):** 8'li → 1.5×, 10'lu → 5.0×, 12+ → 25.0× (en yüksek ödeyen normal sembol).
- **Yıldız (8 = scatter):** Hangi cluster büyüklüğünde olursa olsun **0 TL** öder; sadece bonus tetikler.

### 1.4 Ödeme bahis çarpanı mı, sabit TL mi?

**Bahis çarpanı.** Kanıt: satır 145 → `total += pay * bahis;`. Sonuç `Mathf.RoundToInt` ile int TL'ye yuvarlanır.

> Sabit TL ödeme YOK. Tüm ödemeler `bahis` × katsayı formülünden geçer.

### 1.5 Min cluster eşiği

`MinClusterSize` (TumbleAyarlari) Inspector'da 8.
`OyunKorumaServisi.TUMBLE_SABIT_ESIK` = 8 (Assets/Scripts/Services/OyunKorumaServisi.cs:19).
SimuleEtVeKaydetImpl içinde sabit olarak `minClusterSize = TUMBLE_SABIT_ESIK = 8` set edilir (OyunYoneticisi.Spin.cs:590).

---

## 2. SEMBOL LİSTESİ

### 2.1 Toplam sembol sayısı: **9**

(Sahnede `sembolSpriteListesi` 9 sprite içerir, OyunYoneticisi'ne bağlı; PayTable_*_* dizileri de 9 elemanlı.)

### 2.2 Sembol ID → Görsel adı eşlemesi

GUID → dosya eşleştirmesi sahnede `03_SenaryoluOyun.unity` satır 7832–7841 ve `.meta` dosyalarından çıkarıldı.

| Index | GUID                                | Sprite dosyası                | Açıklama                                |
|-------|-------------------------------------|-------------------------------|-----------------------------------------|
| 0     | `980b72952abbb4e4fa3a8ca142a52851` | `Gorseller/armut.png`         | Armut                                   |
| 1     | `7814d2438e3ca2e4da28db4998d57a0a` | `Gorseller/çilleekkk.png`     | Çilek                                   |
| 2     | `50a2a48537ef48f4cbf951b50ee9bbc0` | `Gorseller/errriklerrrr.png`  | Erik                                    |
| 3     | `8fa64fccffe996149befc91c18a3482d` | `Gorseller/hindistancevizi.png`| Hindistan cevizi                       |
| 4     | `fd6609b5a3031f9479792c1533717c70` | `Gorseller/karpuz.png`        | Karpuz                                  |
| 5     | `05158594f0a268f449a02aa9a9ef32da` | `Gorseller/muz.png`           | Muz                                     |
| 6     | `9e582de2f0636e343ad728d084cfc66a` | `Gorseller/elmalarrrr.png`    | Elma                                    |
| 7     | `1f6e029491bd415469f0401122a859ee` | `Gorseller/üzzzümmmm.png`     | Üzüm                                    |
| 8     | `6b94dfd011d7cea4c9d244ee26e75076` | `Gorseller/yıldız.png`        | **SCATTER** (sahnede ScatterIndex=8)   |

### 2.3 `CARPAN_SEMBOL` sabiti

**Değer:** `-2`
**Konum:** `Assets/Scripts/OyunYoneticisi.Fields.cs:601`

```csharp
// grid hücresinde -1 = boş, -2 = çarpan sembolü, 0..N-1 = normal semboller
private const int CARPAN_SEMBOL = -2;
```

**Bu hangi sembol?** Çarpan/bomba ayrı bir sprite (sahnedeki sprite listesinde değil); `CarpanAyarlari.CarpanSembolSprite` veya `OyunYoneticisi.carpanSembolSprite` field'ında atanan ayrı sprite. Grid'de `-2` ile temsil edilir, sembol indeks alanı 0..8 dışındadır.

### 2.4 Scatter sembolü

- **Var.** Sembol ID: **8** (sahnede ScatterIndex=8 → yıldız.png).
- TumbleAyarlari.cs default'u 7 (üzüm) ama sahne override 8 (yıldız).
- ScatterEsik = 4 (4 scatter cluster'ı bonus oyunu tetikler).
- Scatter cluster'ları **patlamaz** (FindClustersToRemove satır 254: `if (sym == ScatterIndex) continue;`); sadece sayılır ve sayı eşiği aşarsa bonus başlar.
- Scatter'ın paytable değeri tüm tablolarda `0` (EnsurePayTablesInitialized satır 72–77 ile zorlanır).

### 2.5 Bonus / Wild sembolü

- **Wild sembolü YOK** (kodda hiç "wild" geçmiyor; FindClustersToRemove sembole tam eşleşme arar, joker mantığı yok).
- **Bonus tetikleyici = scatter** (yıldız, ID=8). 4 veya daha fazla yıldız = 10 spinlik bonus.
- **Çarpan/bomba (CARPAN_SEMBOL = -2)** ayrı bir sembol gibi davranır (grid'e enjekte edilir, patlamaz, bonus tetiklemez, sadece spin sonu kazancını çarpar).

---

## 3. ÇARPAN MEKANİĞİ

### 3.1 Doğal havuzdaki çarpan değerleri

**Dosya:** `Assets/Scripts/CarpanAyarlari.cs:191-199`

```csharp
public virtual int RastgeleCarpan()
{
    // Doğal havuz yalnızca {2,3,5,8,10}; 100x/250x/500x force path üzerinden gider.
    int[] havuz = new int[] { 2, 3, 5, 8, 10 };
    int secilen = havuz[Random.Range(0, havuz.Length)];
    Debug.Log($"[CARPAN] kaynak=DOGAL havuz=FALLBACK secilen={secilen}x");
    return secilen;
}
```

**Doğal (RNG) havuz:** `{2x, 3x, 5x, 8x, 10x}` — eşit olasılıkla.

**Force (zorlanan) değerler:** `2x, 5x, 10x, 50x, 100x, 250x, 500x` — admin/senaryo path üzerinden çağrılır:
- `CarpanAyarlari.ZorlaCarpan(int deger)` ile butondan tetiklenir (CarpanAyarlari.cs:152).
- Aşağıdaki force butonları otomatik bağlanır (CarpanAyarlari.cs:93-138): ForceX2, ForceX5, ForceX10, ForceX50, ForceX100.
- Senaryo 4 → 100x bomb path; Senaryo 5 → 500x bomb path (OyunYoneticisi.Senaryolar.cs içinden).

`yuksekCarpanOrani` field'ı **DEPRECATED** (TumbleAyarlari.cs:23): "Doğal havuzda artık kullanılmıyor. 100x/250x/500x yalnızca force path (admin/senaryo 4-5) üzerinden düşer."

### 3.2 Aynı anda gridde birden fazla çarpan

**Evet, mümkün.** Üst sınır `MaxCarpanAdedi` = **2** (default, CarpanAyarlari.cs:18). OyunYoneticisi'nde slider ile 1–10 aralığında ayarlanabilir (OyunYoneticisi.Fields.cs:540: `[Range(1,10)] public int maxCarpanAdedi = 3;`).

CarpanServisi.cs:127:
```csharp
int adet = UnityEngine.Random.Range(1, _carpanKalanBuSpin + 1);
for (int i = 0; i < adet; i++)
{
    int carpan = _rollCarpanDegeri != null ? _rollCarpanDegeri() : 0;
    if (carpan <= 0) continue;
    _pendingCarpanDusurecek.Add(carpan);
}
```

### 3.3 Çarpanlar TOPLANIYOR mu, ÇARPILIYOR mu?

**TOPLANIR (toplam = sum).** Kanıt: CarpanServisi.cs:147–151 (`RecordPlacedCarpanlar`):

```csharp
for (int i = 0; i < placed.Count; i++)
{
    int c = placed[i];
    _spinCarpanDegerleri.Add(c);
    _spinCarpanCarpim += c;   // ← ADD, çarpma değil!
    if (_spinCarpanCarpim > long.MaxValue) _spinCarpanCarpim = long.MaxValue;
}
```

> Field adı `_spinCarpanCarpim` ("çarpım") yanıltıcı — gerçekte **toplama** yapıyor. Sweet Bonanza mantığı: 5x + 10x + 3x = 18x toplam.

### 3.4 Çarpanın ham kazanca uygulanması

**Dosya:** `Assets/Scripts/Services/CarpanServisi.cs:78-89`

```csharp
public virtual int MulClampInt(int value, long multiplier)
{
    long v = (long)value * multiplier;
    if (v > int.MaxValue) return int.MaxValue;
    if (v < int.MinValue) return int.MinValue;
    return (int)v;
}

public virtual int ApplyMultiplierToWin(int hamWin)
{
    return MulClampInt(hamWin, GetCurrentMultiplier());
}
```

`GetCurrentMultiplier()` `_spinCarpanCarpim` (toplam) değerini döner; <1 ise 0 döner. `GetTotalMultiplierForSpin()` ise <1 ise **1** döner (kazanca clamp uygulanmasın diye, satır 72-76).

### 3.5 Çarpan dağılım olasılıkları

- **Spin başına çarpan üretim olasılığı:** `carpanUretimOlasiligi` = 0.15 (default, range 0..1). OyunYoneticisi.Fields.cs:539. CarpanServisi.cs:119: `if (olasilik <= 0f || UnityEngine.Random.value > olasilik) return false;`
- **Doğal değer dağılımı:** {2, 3, 5, 8, 10} arasında **uniform random** (CarpanAyarlari.cs:195).
- **Sadece bonusta düş seçeneği:** `carpanSadeceBonus` toggle'ı (OyunYoneticisi.Fields.cs:538). Açıkken normal spinde çarpan üretilmez.
- **Force/admin path:** `zorlaSiradakiCarpan > 0` ise olasılık ve havuz bypass edilir (CarpanServisi.cs:102-112).

### 3.6 Çarpan grid yerleşimi

**Dosya:** `Assets/Scripts/Services/CarpanServisi.cs:163-234` (`CarpanYerlestirmeServisi.CarpanlariDoluGriddeUygula`)

Kural: çarpan/bomba meyve hücrelerinin (sembol >= 0, scatter ve CARPAN_SEMBOL hariç) üzerine yerleşir; o hücredeki meyve değiştirilir, `carpanDegerGrid[x,y]` set edilir.

Tumble adımı içinde refill sonrası **yeni spawn edilen** hücrelere yerleşim için: `CokmeAkisServisi.CokmeDoldurSadeceMantik` (CokmeAkisServisi.cs:268-292). `_bombaPatlamaSonrasiIlkRefillCarpanEngeli` flag'i bombadan hemen sonra yeni bomba düşmesini engeller.

---

## 4. GRID & TUMBLE PARAMETRELERİ

### 4.1 Grid boyutu

**6 sütun × 5 satır = 30 hücre.**
- `OyunYoneticisi.sutun` = 6 (Fields.cs:391, sahnede 7827)
- `OyunYoneticisi.satir` = 5 (Fields.cs:392, sahnede 7828)

### 4.2 `TUMBLE_SABIT_ESIK` (cluster minimum boyutu)

**Değer: 8.**
**Dosya:** `Assets/Scripts/Services/OyunKorumaServisi.cs:19`
```csharp
public const int TUMBLE_SABIT_ESIK = 8;
```

### 4.3 `MAX_TUMBLE_TUR`

**Değer: 20.**
**Dosya:** `Assets/Scripts/Services/OyunKorumaServisi.cs:22`
```csharp
public const int MAX_TUMBLE_TUR = 20;
```

### 4.4 Cluster algılama mantığı

**Komşuluk YOK — "scatter pays" / "anyhere wins" mantığı.** Aynı sembolden 8+ adet **gridin herhangi bir yerinde** olunca patlar (bağlantılı küme aranmaz, yalın count).

Kanıt: `TumbleAyarlari.FindClustersToRemove` (TumbleAyarlari.cs:242-271):

```csharp
public virtual List<Vector2Int> FindClustersToRemove(int[,] grid, int satir, int sutun, int minSize)
{
    Dictionary<int, List<Vector2Int>> bySymbol = new Dictionary<int, List<Vector2Int>>();
    for (int x = 0; x < sutun; x++)
        for (int y = 0; y < satir; y++)
        {
            int sym = grid[x, y];
            if (sym < 0) continue;
            if (sym == ScatterIndex) continue;
            if (!bySymbol.ContainsKey(sym)) bySymbol[sym] = new List<Vector2Int>();
            bySymbol[sym].Add(new Vector2Int(x, y));
        }
    List<Vector2Int> toRemove = new List<Vector2Int>();
    foreach (var kv in bySymbol)
        if (kv.Value.Count >= minSize)
            toRemove.AddRange(kv.Value);
    return toRemove;
}
```

`TumbleServisi.FindClustersToRemove` aynı mantığı tekrar eder (TumbleServisi.cs:36-77). Sadece sembol bazlı sayım; flood-fill yok, 4-yön/8-yön komşuluk yok. Dolayısıyla tüm 8+ aynı sembol grid'de nerede olursa olsun aynı cluster sayılır.

> Yorum satırı `OyunYoneticisi.Fields.cs:418` ("4-yön komşuluk") **eskimiş** — gerçek implementasyon konum-bağımsız count.

### 4.5 Tumble sonrası boş hücre dolumu

Yukarıdan **rastgele sembol düşer** (reel/şerit sistemi YOK).

- Cluster patlatıldıktan sonra hücreler boşalır → üstteki hücreler aşağı kayar (yerçekimi simülasyonu) → boş üst hücreler `RandomSymbolWithScatterChanceForGrid` ile doldurulur.
- `TumbleAyarlari.FillRandomAll` (TumbleAyarlari.cs:424-450): tüm grid'i baştan rastgele doldurur (ilk doldurma).
- `RandomSymbolWithScatterChanceForGrid` (TumbleAyarlari.cs:293-307):
  1. Mevcut scatter sayısı `_maxScatterPerSpin` (default 5) eşiğine ulaştıysa scatter YOK,
  2. Aksi halde `CurrentScatterChance(bonusAktif)` (default normal=0.005..0.020, bonus=0.001) olasılıkla scatter,
  3. Aksi halde `RandomNonScatterSymbol` ile zorluk-bias'lı rastgele meyve.
- `RandomNonScatterSymbol` (satır 317-422): zorluk slider'a (4–12) göre dominant sembolün ağırlığı artırılır/azaltılır; `MinClusterSize - 1` (= 7) yakınında patlamaya yaklaşan sembol için "fren" mekanizması (zor zorlukta %65 reroll, satır 406).

> **Reel/şerit yok** — her hücre bağımsız RNG. Sweet Bonanza tarzı tumble.

### 4.6 Tumble loop özeti

Simülasyon tarafı (`SimuleEtVeKaydetImpl`, OyunYoneticisi.Spin.cs:593-642):

```
while (turSayaci < MAX_TUMBLE_TUR = 20):
    toRemove = FindClustersToRemove(grid, MIN=8)
    if toRemove == null || count==0: break

    pendingCarpan = TryScheduleCarpanDrop(...)   // çarpan üretim olasılığı kontrolü
    turHam = CalculateWinWithOwnPayTable(toRemove, grid, ..., bahis, MIN=8)
    spinKazancHam += turHam

    adim = TumbleAdimKaydi { TurKazanci = turHam, PatlayanHucreler = toRemove }
    GridHucreleriniTemizle(toRemove)               // patlayan hücreler -1
    CokmeDoldurSadeceMantik(adim)                  // çökme + refill + (varsa) çarpan yerleştir
    kayit.Adimlar.Add(adim)
    turSayaci++
```

Sonuç: `kayit.ToplamHamKazanc = spinKazancHam`, `kayit.NihaiCarpanToplam = CarpanServisi.GetTotalMultiplierForSpin()`. Nihai ödeme: `MulClampInt(ham, toplamX)`.

---

## 5. SPINSIMULASYONKAYDI YAPISI

### 5.1 `SpinSimulasyonKaydi` TAM kodu

**Dosya:** `Assets/Scripts/Services/SpinSimulasyonKaydi.cs:1-29`

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bir spin'in simülasyon sonucu: ilk grid ve her tumble adımı.
/// Simülasyon arkada çalışır; kayıt ekranda oynatılır (RNG tekrar çalışmaz).
/// </summary>
[Serializable]
public class SpinSimulasyonKaydi
{
    public int Sutun { get; set; }
    public int Satir { get; set; }
    /// <summary>İlk doldurma sonrası grid (drop-in öncesi).</summary>
    public int[,] IlkGrid { get; set; }
    /// <summary>İlk grid için çarpan değerleri (genelde 0).</summary>
    public int[,] IlkCarpanGrid { get; set; }
    /// <summary>İlk gridde yerleştirilen çarpan değerleri (oynatmada state için).</summary>
    public List<int> IlkCarpanDegerleri { get; set; } = new List<int>();
    public List<TumbleAdimKaydi> Adimlar { get; set; } = new List<TumbleAdimKaydi>();
    /// <summary>Nihai ham kazanç (tüm tumble'lar toplamı).</summary>
    public int ToplamHamKazanc { get; set; }
    /// <summary>Nihai çarpan (tüm adımlardaki çarpanların çarpımı/toplamı - servis mantığına göre).</summary>
    public int NihaiCarpanToplam { get; set; }
    /// <summary>Zorla çarpan (5x/10x/50x/100x) ile üretildiyse true; kazanç/oturum gösterimi tavanlanmadan yapılır.</summary>
    public bool ZorlaCarpanKullanildi { get; set; }
    /// <summary>Admin senaryo 2/3: Simülasyon bu spin için ödeme modeli bandına uygun üretildiyse true. Fallback ile band dışı sonuç oynatıldıysa false (K-KY sırası ilerletilmez).</summary>
    public bool SenaryoOdemeBandinaUygun { get; set; } = true;
}
```

### 5.2 `TumbleAdimKaydi` TAM kodu

**Dosya:** `Assets/Scripts/Services/SpinSimulasyonKaydi.cs:31-53`

```csharp
/// <summary>
/// Tek bir tumble adımı: patlayan hücreler, bu tur kazancı, bu tur yerleştirilen çarpanlar, refill sonrası grid.
/// </summary>
[Serializable]
public class TumbleAdimKaydi
{
    public List<Vector2Int> PatlayanHucreler { get; set; } = new List<Vector2Int>();
    public int TurKazanci { get; set; }
    /// <summary>Bu turda yerleştirilen çarpan değerleri (sırayla).</summary>
    public List<int> CarpanDegerleriBuTur { get; set; } = new List<int>();
    /// <summary>Refill + çökme sonrası grid.</summary>
    public int[,] GridRefillSonrasi { get; set; }
    /// <summary>Refill + çökme sonrası çarpan değer grid'i.</summary>
    public int[,] CarpanGridRefillSonrasi { get; set; }
    /// <summary>Bu turda yeni spawn edilen hücreler (animasyon için).</summary>
    public List<Vector2Int> YeniSpawnEdilenHucreler { get; set; } = new List<Vector2Int>();
    /// <summary>Yerçekimi ile düşen hücrelerin eski konumu (animasyon: from -> to).</summary>
    public List<Vector2Int> DusenHucreFrom { get; set; } = new List<Vector2Int>();
    /// <summary>Yerçekimi ile düşen hücrelerin yeni konumu.</summary>
    public List<Vector2Int> DusenHucreTo { get; set; } = new List<Vector2Int>();
    /// <summary>Senaryo konstrukte enjeksiyonuyla sembolü değiştirilen hücreler (sprite güncelleme için).</summary>
    public List<Vector2Int> InjekteEdilenHucreler { get; set; } = new List<Vector2Int>();
}
```

### 5.3 Field'ların tam anlamı ve formatı

| Field                       | Format                       | Anlam                                                                                       |
|-----------------------------|------------------------------|---------------------------------------------------------------------------------------------|
| `Sutun`                     | `int`                        | Grid sütun sayısı (= 6).                                                                    |
| `Satir`                     | `int`                        | Grid satır sayısı (= 5).                                                                    |
| `IlkGrid`                   | `int[Sutun, Satir]`          | İlk doldurma sonrası grid sembolleri. Hücre değeri 0..N-1 sembol, -1 boş, -2 = CARPAN.      |
| `IlkCarpanGrid`             | `int[Sutun, Satir]`          | İlk grid çarpan değerleri (genelde 0; sadece CARPAN_SEMBOL hücrelerinde çarpan değeri).      |
| `IlkCarpanDegerleri`        | `List<int>`                  | İlk gridde yerleşen çarpan değerleri (sıralı). Spin başında force/random ile yerleştirilen. |
| `Adimlar`                   | `List<TumbleAdimKaydi>`      | Sırayla tüm tumble adımları. Ekranda bu sırayla oynatılır.                                  |
| `ToplamHamKazanc`           | `int`                        | Tüm `Adimlar[].TurKazanci` toplamı (TL, çarpan uygulanmamış).                              |
| `NihaiCarpanToplam`         | `int`                        | Tüm yerleşen çarpanların **TOPLAMI** (≥ 1; sıfırsa 1 sayılır). Field adı yanıltıcı.        |
| `ZorlaCarpanKullanildi`     | `bool`                       | Force path ile üretildiyse true; admin/senaryo 4-5 bombaları için.                          |
| `SenaryoOdemeBandinaUygun`  | `bool`                       | Admin senaryo 2/3 K-KY döngüsünde band içinde mi? Fallback false ise sıra ilerletilmez.      |

`TumbleAdimKaydi` field'ları:

| Field                        | Format                | Anlam                                                                          |
|------------------------------|-----------------------|--------------------------------------------------------------------------------|
| `PatlayanHucreler`           | `List<Vector2Int>`    | Bu turda patlayan hücre koordinatları (cluster).                              |
| `TurKazanci`                 | `int`                 | Bu turun ham kazancı (TL, çarpan uygulanmamış).                               |
| `CarpanDegerleriBuTur`       | `List<int>`           | Refill sonrası bu turda yerleştirilen yeni çarpanlar.                         |
| `GridRefillSonrasi`          | `int[Sutun, Satir]`   | Refill + çökme sonrası grid; bir sonraki turun başlangıç grid'i.              |
| `CarpanGridRefillSonrasi`    | `int[Sutun, Satir]`   | Refill sonrası çarpan değer grid'i.                                           |
| `YeniSpawnEdilenHucreler`    | `List<Vector2Int>`    | Üstten düşerek yeni gelen hücreler (animasyon).                               |
| `DusenHucreFrom`             | `List<Vector2Int>`    | Yerçekimi ile kayan eski konumlar.                                             |
| `DusenHucreTo`               | `List<Vector2Int>`    | Yerçekimi ile kayan yeni konumlar (animasyon: from→to).                        |
| `InjekteEdilenHucreler`      | `List<Vector2Int>`    | Senaryo konstrukte enjeksiyonu (Senaryo 1-5) ile zorla değiştirilen hücreler.  |

### 5.4 Nesnelerin oluşum/tüketim akışı (`NormalSpinAkisi` özeti)

**Dosya:** `Assets/Scripts/Services/DonusAkisServisi.cs:130-383`

```
1) SpinButon → SpinButonImpl → BirSpinHazirlaVeAt
   ├─ EkonomiServisi.DeductSpinMaliyeti(bahis)
   ├─ LogServisi.RecordSpinStart(prevBakiye, bakiye, bahis, odenebilirLimit)
   └─ yield DonusServisi.NormalSpinAkisi()

2) NormalSpinAkisi:
   ├─ Stat reset (kazanc, ham, carpan, vs.)
   ├─ TryConsumeOncedenHesaplanan(false, out kayit)   // önbellek varsa al
   │     └─ yoksa: StartPrecomputeNextSpin(); 5 frame bekle; hâlâ yoksa SimuleEtVeKaydet(...)
   │           SimuleEtVeKaydet → SimuleEtVeKaydetImpl (OyunYoneticisi.Spin.cs:287-823)
   │             ├─ Senaryo 1-5 konstrukte yolu (band hedefli, deterministik)
   │             ├─ Genel reroll döngüsü (max=AsamaIcinMaxReroll, ödeme bandı uygunluk kontrolü)
   │             └─ SpinSimulasyonKaydi döner
   ├─ yield SimulasyonKaydiniOynat(kayit)            // ekranda oynatma (RNG yok, her adım kayıttan)
   ├─ Kazanç hesabı: hamKazanc=kayit.ToplamHamKazanc, toplamX=kayit.NihaiCarpanToplam
   │     teorikToplam = MulClampInt(hamKazanc, toplamX)
   ├─ EkonomiServisi.AddWinnings(odenen, bahis)      // bakiyeye ekle
   ├─ LogServisi.RecordSpinResult(prevBakiye, bakiye, bahis, odenen)
   ├─ ArdisikKayipSayac güncelle → eşik aşılırsa SonrakiSpinKacisFrenlemeAktifEt() (sonraki spin'de cluster zorlanır)
   ├─ Senaryo 2/3 K-KY döngü ilerletme (yalnızca band uygunsa)
   ├─ Scatter sayımı → eşik aşılırsa BaslatBonus()
   ├─ AnlaticiSeritKopru.Ornek?.SpinAtildi() (BirSpinHazirlaVeAt sonu, OyunYoneticisi.Spin.cs:204-205)
   └─ Bir sonraki spin için StartPrecomputeNextSpin(...) tetikle (önbellek doldur)
```

**Önemli:** `SimulasyonKaydiniOynat` sadece kayda bakar; RNG yeniden çağrılmaz. Yani scripted spin için yapılması gereken: **`SpinSimulasyonKaydi` nesnesini elle inşa edip `TryConsumeOncedenHesaplanan` veya doğrudan `_oncedenHesaplananKayit` field'ına enjekte etmek**, ya da `SimuleEtVeKaydet` çağrısını intercept etmek.

`_oncedenHesaplananKayit`, `_oncedenHesaplananHazir`, `_oncedenHesaplananBonusMu` field'ları OyunYoneticisi.Fields.cs:628-630'da private; intercept için partial class genişletmesi veya yeni bir ScriptedSpinYoneticisi (henüz YOK — bkz. Bölüm 7) gerekecek.

---

## 6. ANLATICI ŞERİT SİSTEMİ

### 6.1 `AnlaticiSeritKopru.cs` (TAM, Assets/Scripts/AnlaticiSeritKopru.cs:1-325)

```csharp
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Sol-sabit "Anlatıcı Şerit" HTML iframe paneli için Unity tarafı köprüsü.
/// 7 aşamalı manipülasyon hikayesini yönetir: aşama → 10 spin → bir sonraki aşama.
/// Her aşama OdemeEgilimi + MaxOdeme profilini OyunYoneticisi'ye uygular.
/// HTML panel ⟷ Unity arası postMessage / SendMessage protokolü.
/// </summary>
public class AnlaticiSeritKopru : MonoBehaviour
{
    [DllImport("__Internal")] private static extern void AnlaticiPaneliAc(string url);
    [DllImport("__Internal")] private static extern void AnlaticiPaneliKapat();
    [DllImport("__Internal")] private static extern void AnlaticiPaneliGuncelle(string json);

    private OyunYoneticisi _oy;
    private int _aktifAsama = 0;
    private int _aktifSpin = 0;
    private int _toplamSpin = 0;
    private long _baslangicBakiye = 0;
    private int _sonUygulananAsama = -1; // YENI: aşama değişimi tespiti için
    private long _sonBakiye = 50000; // bir önceki spin sonu bakiye — spin başına net delta için
    private readonly List<int> _asamaSpinNet = new List<int>(); // mevcut aşamadaki spin başına net (+/-) TL
    private const int BASLANGIC_BAKIYE = 50000;
    private const int ASAMA7_GORSEL_MAX_CUBUK = 10; // Asama 7 dinamik (999 spin) — HTML max 10 çubuk göster
    private static AnlaticiSeritKopru _ornek;

    /// <summary>Aşama bazlı önerilen bahis (yeniAsama geçişinde set edilir, kullanıcı sonra manuel değiştirebilir).
    /// Pedagojik eğri: 50K → 60K → 75K → 70K → 55K → 30K → 10K → 0 (~61 spin).</summary>
    private static readonly int[] _onerilenBahisler = new int[] { 500, 1000, 1500, 2500, 4000, 2500, 1500 };

    /// <summary>Aşama başına spin eşiği. Aşama 7 = 999 (dinamik, bakiye yetince Tukenis guard'i tetikler).</summary>
    private static readonly int[] _asamaSpinSayisi = new int[] { 10, 10, 8, 8, 10, 10, 999 };

    [System.Serializable]
    public class AsamaAyari
    {
        public int egilim;
        public float maxCarpani;
        public bool nearMiss;
    }

    // Egilim 0-100 arası clamp'lenir (motor üst sınırı). Kazandırma maxCarpan ile yapılır.
    private static readonly AsamaAyari[] _asamalar = new AsamaAyari[]
    {
        new AsamaAyari { egilim = 95, maxCarpani = 5.0f, nearMiss = false }, // 1 Isındırma ve Umut — çarpıcı kazanç
        new AsamaAyari { egilim = 90, maxCarpani = 3.5f, nearMiss = false }, // 2 Kontrol Bende Hissi — bol kazanç
        new AsamaAyari { egilim = 50, maxCarpani = 1.0f, nearMiss = true  }, // 3 Geri Kazanabilirim
        new AsamaAyari { egilim = 30, maxCarpani = 0.6f, nearMiss = true  }, // 4 Şansın Döndü
        new AsamaAyari { egilim = 20, maxCarpani = 0.4f, nearMiss = true  }, // 5 Sonu Düşünmeyen Kahraman
        new AsamaAyari { egilim = 15, maxCarpani = 0.3f, nearMiss = true  }, // 6 Başka Yerden Para Bulmalıyım
        new AsamaAyari { egilim = 5,  maxCarpani = 0.1f, nearMiss = true  }  // 7 Tükeniş
    };

    public static AnlaticiSeritKopru Ornek => _ornek;

    /// <summary>0-6 (Aşama 1-7). OyunYoneticisi.Admin/Spin tarafından reroll/bant override için okunur.</summary>
    public int AktifAsama => _aktifAsama;

    void Awake()
    {
        string aktifSahne = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (aktifSahne != "03_SenaryoluOyun")
        {
            Debug.Log("[AnlaticiSeritKopru] Aktif sahne " + aktifSahne + ", anlatici devre disi.");
            gameObject.SetActive(false);
            return;
        }
        _ornek = this;
    }
    void OnDestroy() { if (_ornek == this) _ornek = null; }

    void Start()
    {
        _oy = FindObjectOfType<OyunYoneticisi>();
        if (_oy == null) { Debug.LogError("[AnlaticiSeritKopru] OyunYoneticisi bulunamadi"); return; }

        // KRİTİK: Eski admin senaryo preset'leri (Senaryo 1-5) Anlatıcı manipülasyonunu BYPASS ediyor.
        try { _oy.AdminNormalOyunUygula(); } catch { }
        try { _oy.AnlaticiKazancFaziniSifirla(); } catch { }
        if (SenaryoYoneticisi.I != null)
        {
            try
            {
                SenaryoYoneticisi.I.mevcutAsama = SenaryoYoneticisi.SenaryoAsama.Asama7_Finale;
                SenaryoYoneticisi.I.forcedNoPayKalan = 0;
            } catch { }
        }

        // Eğitim aracı: her sahne girişinde sıfırdan başla
        _aktifAsama = 0; _aktifSpin = 0; _toplamSpin = 0; _sonUygulananAsama = -1;
        _oy.AnlaticiBakiyeyiSifirla(BASLANGIC_BAKIYE);
        _baslangicBakiye = BASLANGIC_BAKIYE;
        _sonBakiye = BASLANGIC_BAKIYE;
        _asamaSpinNet.Clear();
        AsamayiUygula(0);

#if UNITY_WEBGL && !UNITY_EDITOR
        AnlaticiPaneliAc("StreamingAssets/anlatici.html");
#endif
        StartCoroutine(IlkGuncelleme());
    }

    private IEnumerator IlkGuncelleme() { yield return new WaitForSeconds(0.5f); Guncelle(); }

    /// <summary>OyunYoneticisi.Spin spin tamamlandıktan sonra çağırır.</summary>
    public void SpinAtildi()
    {
        long simdikiBakiye = _oy != null ? _oy.BahisPanelMevcutBakiye() : _sonBakiye;
        int spinNet = (int)(simdikiBakiye - _sonBakiye);
        _sonBakiye = simdikiBakiye;
        _asamaSpinNet.Add(spinNet);

        _aktifSpin++;
        _toplamSpin++;

        int hedefSpin = _asamaSpinSayisi[Mathf.Clamp(_aktifAsama, 0, _asamaSpinSayisi.Length - 1)];

        if (_aktifSpin >= hedefSpin)
        {
            if (_aktifAsama < 6)
            {
                Guncelle();
                _aktifAsama++;
                _aktifSpin = 0;
                _asamaSpinNet.Clear();
                AsamayiUygula(_aktifAsama);
            }
            else { Guncelle(); Tukenis(); return; }
        }
        else
            AsamayiUygula(_aktifAsama);

        // Bakiye yetersizse Aşama 7 zorla atlanır veya Tukenis tetiklenir.
        if (_oy != null)
        {
            int simdiBakiye = (int)_oy.BahisPanelMevcutBakiye();
            int sonrakiBahis = _onerilenBahisler[Mathf.Clamp(_aktifAsama, 0, _onerilenBahisler.Length - 1)];
            if (simdiBakiye < sonrakiBahis)
            {
                if (_aktifAsama < 6)
                { _aktifAsama = 6; _aktifSpin = 0; _sonUygulananAsama = -1; _asamaSpinNet.Clear();
                  AsamayiUygula(6); Tukenis(); return; }
                else { Tukenis(); return; }
            }
        }
        Guncelle();
    }

    private void AsamayiUygula(int idx)
    {
        if (idx < 0 || idx >= _asamalar.Length || _oy == null) return;
        var a = _asamalar[idx];
        bool yeniAsama = (idx != _sonUygulananAsama);
        _sonUygulananAsama = idx;
        if (yeniAsama && idx >= 0 && idx < _onerilenBahisler.Length)
        {
            int onerilen = _onerilenBahisler[idx];
            try { _oy.AnlaticiSetBahis(onerilen); } catch { }
        }
        int bahis = _oy.AnlaticiMevcutBahis();
        if (bahis <= 0) bahis = 100;
        int maxOdeme = Mathf.CeilToInt(bahis * a.maxCarpani);
        try { _oy.AdminSetOdemeEgilimi(a.egilim); _oy.AdminSetMaxOdeme(maxOdeme); } catch { }
    }

    private void Guncelle()
    {
        if (_oy == null) return;
        long bakiye = _oy.BahisPanelMevcutBakiye();
        long net = bakiye - _baslangicBakiye;
        int hedefSpin = _asamaSpinSayisi[Mathf.Clamp(_aktifAsama, 0, _asamaSpinSayisi.Length - 1)];
        string spinNetJson = "[" + string.Join(",", _asamaSpinNet.ConvertAll(n => n.ToString())) + "]";
        string json = "{\"asama\":" + _aktifAsama + ",\"spin\":" + _aktifSpin +
                      ",\"hedefSpin\":" + hedefSpin + ",\"bakiyeNet\":" + net +
                      ",\"toplamSpin\":" + _toplamSpin + ",\"spinNetleri\":" + spinNetJson + "}";
#if UNITY_WEBGL && !UNITY_EDITOR
        AnlaticiPaneliGuncelle(json);
#endif
    }

    public void Tukenis()
    {
        if (_oy == null) return;
        long bakiye = _oy.BahisPanelMevcutBakiye();
        long net = bakiye - _baslangicBakiye;
        string json = "{\"tukenis\":true,\"bakiyeNet\":" + net + ",\"toplamSpin\":" + _toplamSpin + "}";
#if UNITY_WEBGL && !UNITY_EDITOR
        AnlaticiPaneliGuncelle(json);
#endif
    }

    /// <summary>HTML panelde ◀ ▶ veya nokta tıklamasıyla manuel aşama değişimi.</summary>
    public void HtmlAsamaDegisti(int yeniAsama)
    {
        if (yeniAsama < 0 || yeniAsama > 6) return;
        _aktifAsama = yeniAsama; _aktifSpin = 0; _asamaSpinNet.Clear();
        AsamayiUygula(yeniAsama); Guncelle();
    }

    public void YenidenBaslat()
    {
        if (_oy != null) _oy.AnlaticiBakiyeyiSifirla(BASLANGIC_BAKIYE);
        _baslangicBakiye = BASLANGIC_BAKIYE;
        _aktifAsama = 0; _aktifSpin = 0; _toplamSpin = 0;
        _sonUygulananAsama = -1; _sonBakiye = BASLANGIC_BAKIYE; _asamaSpinNet.Clear();
        AsamayiUygula(0);
        string json = "{\"asama\":0,\"spin\":0,\"bakiyeNet\":0,\"toplamSpin\":0,\"spinNetleri\":[],\"tukenisKapat\":true}";
#if UNITY_WEBGL && !UNITY_EDITOR
        AnlaticiPaneliGuncelle(json);
#endif
    }
}
```

> Yukarıdaki kod, gerçek dosyanın esas akış mantığını birebir yansıtır. Inline yorumların bir kısmı (Debug.Log'lar) yer kazanmak için kısaltıldı; orijinal dosya 325 satır, ek dökümün gerekirse `Assets/Scripts/AnlaticiSeritKopru.cs` doğrudan açılabilir.

### 6.2 7 Aşama profil tanımı

**Konum:** `AnlaticiSeritKopru.cs:32-55` (statik diziler).

| Aşama (idx) | Ad                        | Önerilen Bahis | Spin Sayısı | Eğilim (%) | Max Carpan | Near Miss |
|-------------|---------------------------|----------------|-------------|------------|------------|-----------|
| 0 (Aşama 1) | Isındırma ve Umut         | 500            | 10          | 95         | 5.0×       | false     |
| 1 (Aşama 2) | Kontrol Bende Hissi       | 1000           | 10          | 90         | 3.5×       | false     |
| 2 (Aşama 3) | Geri Kazanabilirim        | 1500           | 8           | 50         | 1.0×       | true      |
| 3 (Aşama 4) | Şansın Döndü              | 2500           | 8           | 30         | 0.6×       | true      |
| 4 (Aşama 5) | Sonu Düşünmeyen Kahraman  | 4000           | 10          | 20         | 0.4×       | true      |
| 5 (Aşama 6) | Başka Yerden Para Bulmalıyım | 2500        | 10          | 15         | 0.3×       | true      |
| 6 (Aşama 7) | Tükeniş                   | 1500           | 999 (dinamik) | 5        | 0.1×       | true      |

- **Bahisler:** `_onerilenBahisler[] = {500, 1000, 1500, 2500, 4000, 2500, 1500}`. Yeni aşama geçişinde otomatik set edilir; kullanıcı sonra manuel değiştirebilir.
- **Spin sayıları:** `_asamaSpinSayisi[] = {10, 10, 8, 8, 10, 10, 999}`. Aşama 7 dinamik (999 = bakiye tükenene kadar).
- **MaxOdeme** = `bahis × maxCarpan` (Mathf.CeilToInt). Her spin sonrası `AsamayiUygula` ile yeniden hesaplanır (kullanıcı bahis değiştirirse senkronlanır).
- **Eğilim 0-100:** Motor üst sınırı (`AdminSetOdemeEgilimi`); kazandırma asıl `maxCarpan` ile yapılır.
- **NearMiss:** Aşama 3-7'de `true`; YakınKaçırma görsel enjeksiyonu (DonusAkisServisi.cs içinde `GrideNearMissEnjekteEt` çağrısı).

> RTP hedefi açıkça yazılmamış; aksine **bahis × maxCarpan** = aşamanın TL bazlı tavanı. Pedagojik eğri (yorum satırından): "50K → 60K → 75K → 70K → 55K → 30K → 10K → 0 (~61 spin)".
>
> Anlatıcı **satırları/metinleri** Unity tarafında YOK — HTML panel (`StreamingAssets/anlatici.html`) içinde tanımlı (Unity sadece JSON state push eder, metin orada).

### 6.3 `AktifAsama` indeksleme

**0-indexed.** Kanıt: AnlaticiSeritKopru.cs:60 yorumu — `0-6 (Aşama 1-7)`.

- `_aktifAsama = 0` → Aşama 1 (Isındırma)
- `_aktifAsama = 6` → Aşama 7 (Tükeniş)

`OyunYoneticisi.Spin.cs:259-267` `AsamaIcinMaxReroll()` da bu indekslemeyi kullanır:
```csharp
if (asama == 0) return 2000; // 1 Isındırma — cömertlik zirvesi
if (asama == 1) return 1500; // 2 Kontrol Bende
...
return 20;                   // 7 Tükeniş — minimal reroll, kayıp serbest
```

### 6.4 `SpinAtildi()` metodunun yaptıkları

**Çağrı yeri:** OyunYoneticisi.Spin.cs:204-205 (`BirSpinHazirlaVeAt` sonu, NormalSpinAkisi tamamlandıktan sonra).

**Yaptıkları (sırayla):**
1. **Spin net hesabı:** `spinNet = simdikiBakiye - _sonBakiye`; `_asamaSpinNet` listesine ekler (HTML çubuk grafik için).
2. `_aktifSpin++` ve `_toplamSpin++`.
3. **Aşama eşiği kontrolü:** `_aktifSpin >= hedefSpin` ise:
   - Aşama 1-6 ise: `_aktifAsama++`, `_aktifSpin=0`, `_asamaSpinNet.Clear()`, `AsamayiUygula(_aktifAsama)`.
   - Aşama 7'de eşik (999) görüldüyse `Tukenis()` tetiklenir.
4. **Aşama değişmediyse:** `AsamayiUygula(_aktifAsama)` çağırır → bahis manuel değişmişse `maxOdeme` yeniden hesaplanır.
5. **Bakiye yetersizlik kontrolü:** Mevcut bakiye sonraki aşamanın önerilen bahisinden düşükse:
   - Aşama 1-6 → Aşama 7'ye zorla atla + Tukenis.
   - Aşama 7 → Tukenis.
6. `Guncelle()` → HTML paneline JSON state push (asama, spin, bakiyeNet, toplamSpin, spinNetleri).

`AsamayiUygula(idx)` (satır 221-252):
- Yeni aşama geçişinde `_oy.AnlaticiSetBahis(onerilen)` ile bahis set eder (her spinde değil, sadece geçişte).
- `int maxOdeme = Mathf.CeilToInt(bahis * a.maxCarpani);`
- `_oy.AdminSetOdemeEgilimi(a.egilim)` + `_oy.AdminSetMaxOdeme(maxOdeme)`.

---

## 7. İLGİLİ DOSYALARIN LİSTESİ

### 7.1 `OyunYoneticisi` partial class dosyaları

`public partial class OyunYoneticisi` 8 dosyaya bölünmüş:

| Dosya                                    | Satır | İçerik                                                                 |
|------------------------------------------|-------|------------------------------------------------------------------------|
| `Assets/Scripts/OyunYoneticisi.cs`           | 2010  | Ana sınıf: bootstrap, Update, scene transitions, ekonomi senkronu vs. |
| `Assets/Scripts/OyunYoneticisi.Admin.cs`     | 1430  | Admin panel ayarları, ödeme eğilim/min/max, force çarpan, senaryo preset uygula |
| `Assets/Scripts/OyunYoneticisi.Bonus.cs`     | 226   | Bonus oyunu başlat/bitir, scatter eşik, bonus budget                  |
| `Assets/Scripts/OyunYoneticisi.Fields.cs`    | 642   | Tüm field'lar, sabitler (CARPAN_SEMBOL=-2), serializable struct'lar  |
| `Assets/Scripts/OyunYoneticisi.Senaryolar.cs`| 1479  | Senaryo 1-5 konstrukte motorları, paytable enjeksiyon, K-KY-K döngü |
| `Assets/Scripts/OyunYoneticisi.Simulasyon.cs`| 938   | Simülasyon yardımcıları, grid manipülasyon, near-miss, kacis frenleme |
| `Assets/Scripts/OyunYoneticisi.Spin.cs`      | 823   | SpinButon, BirSpinHazirlaVeAt, SimuleEtVeKaydetImpl, AsamaIcinMaxReroll |
| `Assets/Scripts/OyunYoneticisi.UI.cs`        | 1084  | Tüm UI bind, UI_Guncelle, popup'lar, tooltip, slider'lar              |

### 7.2 `Senaryo` veya `Scripted` adı geçen tüm dosyalar

`Scripted*` adıyla **HİÇBİR dosya yok** (`grep -r ScriptedSpin` 0 sonuç). Aşağıdakiler `Senaryo` öneki ile bulundu:

| Yol                                                                  | Açıklama                                                          |
|-----------------------------------------------------------------------|------------------------------------------------------------------|
| `Assets/Scripts/SenaryoYoneticisi.cs`                                | 7 aşama senaryo sistemi (Asama1_IsindirmaUmut..Asama7_Finale).   |
| `Assets/Scripts/SenaryoOlayKaydi.cs`                                 | Senaryo log entry türleri (NormalSpinBasladi, BonusBitti vs.).    |
| `Assets/Scripts/SenaryoOtomatikAkis.cs`                              | Senaryo'yu otomatik ilerleten Editor/Test akış aracı.            |
| `Assets/Scripts/SenaryoDurumPaneliKaydirici.cs`                      | UI panel scroller.                                               |
| `Assets/Scripts/OyunYoneticisi.Senaryolar.cs`                        | Senaryo 1-5 konstrukte motorları (admin senaryo preset path'leri). |
| `Assets/Scripts/Senaryolar/HedefOdemeMotorBase.cs`                   | Hedef ödeme tutar seçici base (senaryo 1-5 için).                |
| `Assets/Scripts/Senaryolar/Senaryo1HedefOdemeMotoru.cs`              | Senaryo 1 paytable konstrukte motoru.                            |
| `Assets/Scripts/Senaryolar/Senaryo2HedefOdemeMotoru.cs`              | Senaryo 2 K-KY-K döngü motoru.                                    |
| `Assets/Scripts/Senaryolar/Senaryo3HedefOdemeMotoru.cs`              | Senaryo 3 KY-K-KY döngü motoru.                                   |
| `Assets/Scripts/Senaryolar/Senaryo4HedefOdemeMotoru.cs`              | Senaryo 4 KY→K→BOMB_100x.                                         |
| `Assets/Scripts/Senaryolar/Senaryo5HedefOdemeMotoru.cs`              | Senaryo 5 K→KY→BOMB_500x + Zirve bonusu.                          |
| `Assets/Scripts/Services/SenaryoOdemeModelServisi.cs`                | Ödeme modeli ortak yardımcısı.                                    |
| `Assets/Scripts/Services/Senaryolar/AdminSenaryoSpinPolitikalari.cs` | Admin preset spin politikaları.                                   |
| `Assets/Scripts/Services/Senaryolar/SenaryoSpinPolitikasi.cs`        | `ISenaryoSpinPolitikasi` arayüzü + impl.                          |
| `Assets/Scripts/Services/Senaryolar/VarsayilanSpinPolitikasi.cs`     | Varsayılan (senaryo aktif değilken) spin politikası.              |

> **Not:** Senaryo path'leri `OyunYoneticisi.Spin.cs` içinde `IsAdminSenaryo1Aktif()` … `IsAdminSenaryo5Aktif()` flag'leriyle kontrol edilir. Senaryo aktif değilken **anlatıcı şerit profili** uygulanır (Bölüm 6).
>
> **`ScriptedSpinYoneticisi.Aktif = true` flag'i şu an PROJEDE YOK.** Kullanıcı kendi yeni sistemi inşa edecek; önerilen entegrasyon noktası: `OyunYoneticisi.Spin.cs:287` `SimuleEtVeKaydetImpl`'in başında bir `ScriptedSpinYoneticisi.Aktif` kontrolü, true ise senaryo bandını bypass edip elle hazırlanan `SpinSimulasyonKaydi` döndürmek (mevcut Senaryo 1-5 yolu güzel referans).

### 7.3 Tumble / Carpan / Odeme / Ekonomi servisleri

| Servis                       | Dosya yolu                                              | Satır | Görev                                              |
|------------------------------|---------------------------------------------------------|-------|----------------------------------------------------|
| TumbleAyarlari (MonoBehaviour)| `Assets/Scripts/TumbleAyarlari.cs`                     | 452   | Pay table, scatter ayarları, FindClustersToRemove, FillRandomAll, RandomNonScatterSymbol. |
| TumbleServisi                | `Assets/Scripts/Services/TumbleServisi.cs`              | 113   | Tumble loop wrapper (delegate'ler ile OyunYoneticisi'yi çağırır). |
| TumbleAkisServisi            | `Assets/Scripts/Services/TumbleAkisServisi.cs`          | 114   | Tumble loop animasyon akışı.                       |
| CokmeAkisServisi             | `Assets/Scripts/Services/CokmeAkisServisi.cs`           | 554   | Çökme + refill mantık ve animasyon (CokmeDoldurSadeceMantik, CokmeDoldurOynat). |
| CarpanAyarlari (MonoBehaviour)| `Assets/Scripts/CarpanAyarlari.cs`                     | 222   | Çarpan üretim olasılığı, max adet, force butonları.|
| CarpanServisi                | `Assets/Scripts/Services/CarpanServisi.cs`              | 234   | Çarpan state, MulClampInt, RecordPlacedCarpanlar, CarpanYerlestirmeServisi. |
| CarpanOverlayServisi         | `Assets/Scripts/Services/CarpanOverlayServisi.cs`       | 248   | Çarpan overlay text/visuals.                       |
| OdemeServisi                 | `Assets/Scripts/Services/OdemeServisi.cs`               | 46    | Havuz/ödenebilir limit (delegate'lerle).           |
| EkonomiServisi               | `Assets/Scripts/Services/EkonomiServisi.cs`             | 261   | Bakiye, bahis, AddWinnings, DeductSpinMaliyeti, PlayerPrefs senkron. |
| KasaYoneticisi (LEGACY)      | `Assets/Scripts/KasaYoneticisi.cs`                       | —     | Eski kasa mantığı; OyunYoneticisi.Fields.cs:307 LEGACY işaretli. |
| BonusAyarlari                | `Assets/Scripts/BonusAyarlari.cs`                       | —     | Bonus konfigürasyonu (bonusHak, scatter eşiği vs.). |
| OyunKorumaServisi (static)   | `Assets/Scripts/Services/OyunKorumaServisi.cs`          | 35    | TUMBLE_SABIT_ESIK, MAX_TUMBLE_TUR, ClampZorluk, ClampScatterYuzde. |
| ZorlukServisi                | `Assets/Scripts/Services/ZorlukServisi.cs`              | 47    | Zorluk slider 4-12 → easy/hard bias.                |

### 7.4 `LogYoneticisi.cs` ve log şeması

**Dosya:** `Assets/Scripts/LogYoneticisi.cs` (UI tarafı; ekrana yansıtma).
**Servis:** `Assets/Scripts/Services/LogServisi.cs` (kayıt motoru).
**Kalıcı kayıt:** `GameManager.RecordEconomyAction(...)` → `PlayerProfile.statsEntries` ve `PlayerProfile.logs` listelerine eklenir; `SaveProfiles` ile JSON'a yazılır (PlayerPrefs anahtar `PP_WEBGL_PROFILES_JSON_V1`, dosya `Application.persistentDataPath/profiles.json`).

**Log şemaları:**

`GameLogEntry` (PlayerProfile.cs:48-62):
```csharp
public class GameLogEntry {
    public string timeIso;   // "yyyy-MM-dd HH:mm:ss"
    public string type;      // "SPIN", "SPIN_RESULT", "BONUS_PAYOUT", "DEPOSIT", ...
    public string message;
    public int amount;
}
```

`StatsEntry` (PlayerProfile.cs:65-103):
```csharp
public class StatsEntry {
    public double oncekiBakiye, sonrakiBakiye;
    public string yapilanIslem;            // "Spin Başladı", "Spin Sonucu", "Bonus Ödemesi"
    public double kazanc;                  // odenen
    public int toplamSpinSayisi;
    public int toplamBonusGirisSayisi;
    public double bahis;
    public double netDegisim;              // sonraki - onceki
    public string kategori;                // "SPIN" | "SPIN_RESULT" | "BONUS_PAYOUT"
    public string aciklama;
    public string tarihSaat;
}
```

**LogServisi metodları (yazılan kayıtlar):**
- `RecordSpinStart(prevBakiye, bakiye, bahis, odenebilirLimit)` → "Spin Başladı" / "SPIN" / amount=bahis.
- `RecordSpinResult(prevBakiye, bakiye, bahis, odenen)` → "Spin Sonucu" / "SPIN_RESULT" / amount=odenen.
- `RecordBonusSpin(prevBakiye, bakiye, bahis, odenen)` → "Spin Sonucu" / "SPIN_RESULT" (bonus için aynı tip).
- `RecordBonusEnd(prevBakiye, bakiye, gercekOdeme)` → "Bonus Ödemesi" / "BONUS_PAYOUT".

Ek olarak `OturumKayitcisi.EkleEvent(...)` ile özel olaylar (örn. `kacis_frenleme_aktif`) loglanır (DonusAkisServisi.cs:251).

`SenaryoYoneticisi.LogEkle(SenaryoOlayKaydi.OlayTipi_*)` ile senaryo akış olayları (NormalSpinBasladi, NormalSpinBitti, BonusBitti, HizHizlandi vs.) ayrı bir kanaldan kaydedilir.

### 7.5 Resources klasörü içeriği

**Yol:** `Assets/Resources/`

| Yol                                          | Tür                | Açıklama                                                |
|----------------------------------------------|--------------------|---------------------------------------------------------|
| `Assets/Resources/Fonts/`                    | Klasör             | TextMeshPro font asset'leri.                            |
| `Assets/Resources/MeyveHucre/10 meyvehucre.png` | Sprite           | Tek bir hücre görseli (boş arka plan; inspector'da).    |
| `Assets/Resources/arkaplan.png`              | Sprite             | Sahne arkaplan.                                         |
| `Assets/Resources/kirmizi_buton.png`         | Sprite             | UI buton.                                               |
| `Assets/Resources/yesil_buton.png`           | Sprite             | UI buton.                                               |

> **ScriptableObject asset YOK** Resources altında. Tüm konfig MonoBehaviour'lar (TumbleAyarlari, CarpanAyarlari, BonusAyarlari) sahnedeki GameObject'lere bağlı. Yeni bir scripted spin sistemi eklenirken yine sahne-bazlı GameObject + MonoBehaviour pattern'i takip edilebilir veya ScriptableObject Resources/Senaryolar altına konabilir (proje şu an SO kullanmıyor).
>
> **`Assets/Editor/`, `Assets/Plugins/`, `Assets/StreamingAssets/`, `Assets/Videos/`, `Assets/WebGLTemplates/`** ayrı klasörler. `StreamingAssets/anlatici.html` Anlatıcı Şerit'in HTML panelidir.

---

## 8. MEVCUT SAHNELER VE BAĞLANTILAR

### 8.1 Build Settings sahne sırası

**Kaynak:** `ProjectSettings/EditorBuildSettings.asset`

| Build Index | Sahne                                       | GUID                                |
|-------------|---------------------------------------------|-------------------------------------|
| 0           | `Assets/Scenes/01_GirisScene.unity`         | c4c02429ea7a7574b831c73b743d1ff0    |
| 1           | `Assets/Scenes/02_TutorialScene.unity`      | 590650738761426c859a04d2a970183a    |
| 2           | `Assets/Scenes/03_SenaryoluOyun.unity`      | 4e83115628d34cefb1aa3d6aa5008ae3    |
| 3           | `Assets/Scenes/04_AdminOyunScene.unity`     | 8a2c9f1e3d704562b8e94a1c7f6d903b    |
| 4           | `Assets/Scenes/05_LogScane.unity`           | 1453fc080ca66e443ac8f2ee1a1475c7    |

Tümü `enabled: 1`.

### 8.2 `OyunYoneticisi`'nin aktif olduğu sahneler

- **03_SenaryoluOyun.unity** — Hierarchy'de `OyunYoneticisi` GameObject'i (m_Script GUID `7c2a4f9e1d8b4036a1b2c3d4e5f60708`, satır 7706, m_EditorClassIdentifier: `OyunYoneticisiAdminOyunKopya`).
- **04_AdminOyunScene.unity** — Aynı pattern, admin oyun sahnesi (kopya class).

> Sahnedeki class adı `OyunYoneticisiAdminOyunKopya` görünüyor; ancak bu **ana `OyunYoneticisi` partial class'ının özelleştirilmiş bir kopyası** (admin sahnesi için isim ayrımı). Mantık aynı.

`OyunYoneticisi` **yalnızca slot oyunu sahnelerinde** çalışır (01_Giris, 02_Tutorial, 05_Log sahnelerinde aktif değil).

### 8.3 `AnlaticiSeritKopru.Ornek` singleton

- **Statik field:** `private static AnlaticiSeritKopru _ornek;` (AnlaticiSeritKopru.cs:28).
- **Public erişim:** `public static AnlaticiSeritKopru Ornek => _ornek;` (satır 57).
- **Set:** `Awake()` içinde, **sadece `03_SenaryoluOyun` sahnesinde** (satır 64-72):
  ```csharp
  string aktifSahne = SceneManager.GetActiveScene().name;
  if (aktifSahne != "03_SenaryoluOyun") { gameObject.SetActive(false); return; }
  _ornek = this;
  ```
- **Reset:** `OnDestroy` içinde (satır 73).

> **AnlaticiSeritKopru sadece `03_SenaryoluOyun` sahnesinde aktif bir singleton.** Diğer sahnelerde `Ornek` null döner. Bu davranış hem singleton hem sahne-bağımlı; başka sahnelerden çağrılırsa null check zorunlu.

Sahnedeki GameObject'in adı: `AnlaticiSeritKopru` (m_Script GUID `bc6243c7eb0db3c4ea05c58d8b847794`, satır 18572).

### 8.4 `03_SenaryoluOyun` sahnesinin Hierarchy bileşenleri (kritik GameObject'ler)

03_SenaryoluOyun.unity dosyasında saptanan kritik GameObject'ler:

| GameObject Adı            | Bağlı script (m_Script GUID)                          | Sahnedeki rolü                                                                |
|---------------------------|--------------------------------------------------------|--------------------------------------------------------------------------------|
| `OyunYoneticisi`          | `7c2a4f9e1d8b4036a1b2c3d4e5f60708` (OyunYoneticisi)  | Ana slot motor (satır 7706).                                                   |
| `TumbleAyarlari `         | `9e4c6f1a3d0b5248c3d4e5f607091012` (TumbleAyarlari)  | Pay table + tumble parametreleri (satır 11767).                                |
| `CarpanAyarlari `         | (sahnede satır 21115)                                  | Çarpan üretim parametreleri.                                                   |
| `AnlaticiSeritKopru`      | `bc6243c7eb0db3c4ea05c58d8b847794` (AnlaticiSeritKopru)| 7 aşama anlatıcı şerit kontrolcüsü (satır 18572).                            |
| `AdminPanelUygulaButton`  | UnityEngine.UI.Button                                   | Admin paneli açma (satır 19562).                                              |
| `OyunYoneticisi`'nin sembolSpriteListesi | (referanslar)                              | 9 sembol sprite'ı (sahnede satır 7832-7841, GUID listesi Bölüm 2.2'de).      |

> **`SenaryoYoneticisi`** sahnede ayrı bir GameObject'te. Singleton (`SenaryoYoneticisi.I`), `Awake`/`Start`'ta `I = this` set eder. Ana mantığı sahne tarafından aktif tutulur.
>
> **Sahnenin tamamı 24351 satır YAML.** Yukarıdaki tablo en kritik 5-6 GameObject'i içerir; tam Hierarchy'i çıkarmak için Unity Editor açılması gerekir.

### 8.5 Senaryo bazlı Scripted Spin için önerilen entegrasyon noktası

(Raporun kullanıcısı için kısa pratik özet — kod değiştirmedim, sadece okuma yaptım.)

1. **Yeni `ScriptedSpinYoneticisi` MonoBehaviour'ı 03_SenaryoluOyun sahnesine eklenebilir.** Mevcut sahnede `Oyun_Sistemleri` GameObject'i altında konumlandırılması mantıklı (TumbleAyarlari ve CarpanAyarlari'nın komşusu).
2. **Aktivasyon flag'i:** `public static bool Aktif = false;` (ScriptedSpinYoneticisi.cs içinde) — spin başında okunur.
3. **Entegrasyon noktası:** `OyunYoneticisi.Spin.cs:287 SimuleEtVeKaydetImpl`'in başında, mevcut Senaryo 1-5 if-else bloğunun **öncesine** bir `if (ScriptedSpinYoneticisi.Aktif)` kontrolü eklenmesi.
4. **Veri:** `SpinSimulasyonKaydi` nesnesini elle inşa et (`IlkGrid`, `Adimlar` doldur). `NihaiCarpanToplam` ≥ 1 olmalı; `ToplamHamKazanc` adımların `TurKazanci` toplamına eşit olmalı (`SpinKaydiHamPaytableIleUyumluMu` doğrulamasını geçmeli — OyunYoneticisi.Spin.cs:78-117).
5. **Anlatıcı ile uyum:** `AnlaticiSeritKopru.Start()` (satır 84-119) zaten Anlatıcı sahnesi aktifken admin senaryo preset'lerini bypass ediyor. Yeni scripted sistem aynı pattern'i takip etmeli — Anlatıcı aktifken kendi flag'ini set etmek yerine devre dışı kalabilir veya Anlatıcı ile birleştirilebilir.

---

## EK: Hızlı Referans

- **Grid:** 6×5 = 30 hücre
- **Min cluster:** 8 (TUMBLE_SABIT_ESIK)
- **Max tumble tur:** 20 (MAX_TUMBLE_TUR)
- **Sembol:** 9 adet (0..8); 0-7 normal meyve, 8 = scatter (yıldız)
- **CARPAN_SEMBOL grid kodu:** -2
- **Çarpan değerleri (doğal):** {2, 3, 5, 8, 10}; (force): 50, 100, 250, 500
- **Çarpanlar TOPLANIR** (Sweet Bonanza mantığı; sum, not multiply)
- **Anlatıcı 7 aşama, 0-indexed:** 0=Isındırma…6=Tükeniş
- **Anlatıcı bahisleri:** 500/1000/1500/2500/4000/2500/1500
- **Anlatıcı spin sayıları:** 10/10/8/8/10/10/999
- **Anlatıcı sahne kısıtı:** Sadece `03_SenaryoluOyun.unity`
- **OyunYoneticisi partial:** 8 dosya; ana akış `OyunYoneticisi.Spin.cs::SimuleEtVeKaydetImpl`
- **NormalSpinAkisi:** `Assets/Scripts/Services/DonusAkisServisi.cs:130-383`
- **Önbellek:** `_oncedenHesaplananKayit` field'ı (Fields.cs:628). Buton → cache miss → simulate → 5 frame bekle → fallback simulate.
- **Scripted spin yöneticisi:** Henüz YOK. Eklenecek nokta: `SimuleEtVeKaydetImpl` başı.

---

**Rapor sonu.** Toplam 8 başlık, ~600 satır. Tüm bilgiler `D:\KumarFarkindalikOyunu\` altında, branch `refactor-paket8` üzerinde okunan dosyalardan derlendi.
