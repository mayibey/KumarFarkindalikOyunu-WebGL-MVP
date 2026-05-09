using UnityEngine;
using System.Collections.Generic;

// TumbleAyarlari - Tumble sistemi ayarları ve mantığı

[DisallowMultipleComponent]
public class TumbleAyarlari : MonoBehaviour
{
    [Header("Tumble / Eslestirme")]
    [Tooltip("Eslesme icin minimum ayni sembol sayisi.")]
    [Min(2)] public int MinClusterSize = 8;

    [Header("Pay Table - 8-9 Meyve")]
    [Tooltip("8-9 eslesme pay table (x bahis). Sembol index'e gore degerler.")]
    public float[] PayTable_8_9 = new float[9] { 0.5f, 1f, 1.5f, 2f, 3f, 5f, 8f, 10f, 20f };

    [Tooltip("10-11 eslesme pay table (x bahis)")]
    public float[] PayTable_10_11 = new float[9] { 1f, 2f, 3f, 5f, 8f, 10f, 15f, 25f, 50f };

    [Tooltip("12+ eslesme pay table (x bahis)")]
    public float[] PayTable_12Plus = new float[9] { 2f, 5f, 8f, 10f, 15f, 25f, 40f, 60f, 100f };
    
    [Tooltip("Scatter sembolu indexi (sembolSpriteListesi içinde silahın indexi; pay table'da 0 olmali)")]
    public int ScatterIndex = 7;
    
    // PayTable'i initialize eden metot
    public void EnsurePayTablesInitialized(int sembolSayisi)
    {
        if (sembolSayisi <= 0) return;
        
        // 8-9 table
        if (PayTable_8_9 == null || PayTable_8_9.Length != sembolSayisi)
        {
            float[] old = PayTable_8_9;
            PayTable_8_9 = new float[sembolSayisi];
            for (int i = 0; i < sembolSayisi; i++) PayTable_8_9[i] = 1f;
            if (old != null)
            {
                for (int i = 0; i < Mathf.Min(old.Length, sembolSayisi); i++)
                    PayTable_8_9[i] = old[i];
            }
        }
        
        // 10-11 table
        if (PayTable_10_11 == null || PayTable_10_11.Length != sembolSayisi)
        {
            float[] old = PayTable_10_11;
            PayTable_10_11 = new float[sembolSayisi];
            for (int i = 0; i < sembolSayisi; i++) PayTable_10_11[i] = (PayTable_8_9 != null && i < PayTable_8_9.Length) ? PayTable_8_9[i] * 2 : 1f;
            if (old != null)
            {
                for (int i = 0; i < Mathf.Min(old.Length, sembolSayisi); i++)
                    PayTable_10_11[i] = old[i];
            }
        }
        
        // 12+ table
        if (PayTable_12Plus == null || PayTable_12Plus.Length != sembolSayisi)
        {
            float[] old = PayTable_12Plus;
            PayTable_12Plus = new float[sembolSayisi];
            for (int i = 0; i < sembolSayisi; i++) PayTable_12Plus[i] = (PayTable_8_9 != null && i < PayTable_8_9.Length) ? PayTable_8_9[i] * 3 : 1f;
            if (old != null)
            {
                for (int i = 0; i < Mathf.Min(old.Length, sembolSayisi); i++)
                    PayTable_12Plus[i] = old[i];
            }
        }
        
        // Scatter'i 0 yap
        if (ScatterIndex >= 0 && ScatterIndex < sembolSayisi)
        {
            PayTable_8_9[ScatterIndex] = 0f;
            PayTable_10_11[ScatterIndex] = 0f;
            PayTable_12Plus[ScatterIndex] = 0f;
        }
    }
    
    // PayTable degerlerini getiren metotlar
    public float GetPayForCount(int sembolIndex, int eslesmeSayisi)
    {
        if (eslesmeSayisi < MinClusterSize) return 0f;
        
        float[] tablo = null;
        if (eslesmeSayisi <= 9)
            tablo = PayTable_8_9;
        else if (eslesmeSayisi <= 11)
            tablo = PayTable_10_11;
        else
            tablo = PayTable_12Plus;
            
        if (tablo == null || sembolIndex < 0 || sembolIndex >= tablo.Length)
            return 0f;
            
        return tablo[sembolIndex];
    }
    
    // TumbleAyarlari'nin kendi PayTable'ini kullan
    public int CalculateWinWithOwnPayTable(List<Vector2Int> removed, int[,] grid, int satir, int sutun, int bahis)
    {
        if (removed == null || grid == null) return 0;

        // TumbleAyarlari'nin PayTable'ini kullan
        float[] payTable = PayTable_8_9;
        float[] payTable10_11 = PayTable_10_11;
        float[] payTable12Plus = PayTable_12Plus;

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

            if (count < MinClusterSize) continue;

            float pay = 0f;
            if (count <= 9)
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

    [Header("Animasyon Hizlari (Normal)")]
    [Tooltip("Patlama animasyon suresi")]
    [Min(0f)] public float PopDuration = 0.15f;

    [Tooltip("Dusme animasyon suresi")]
    [Min(0f)] public float FallDuration = 0.18f;

    [Tooltip("Adimlar arasi bekleme")]
    [Min(0f)] public float BetweenStepsDelay = 0.05f;

    [Tooltip("Ustten dogma offset")]
    public float SpawnFromTopOffset = 120f;

    [Header("Bonus Hizlari (Override)")]
    [Tooltip("Bonus oyununda hizlari override et")]
    public bool BonusYavasMod = true;

    [Min(0f)] public float BonusPopDuration = 0.7f;
    [Min(0f)] public float BonusFallDuration = 0.8f;
    [Min(0f)] public float BonusBetweenStepsDelay = 0.35f;

    [Tooltip("Bonus spin bekleme override")]
    [Min(0f)] public float BonusSpinBeklemeOverride = 1.1f;

    [Header("Efekt (Opsiyonel)")]
    public ParticleSystem PopParticlePrefab;

    // ========== TUMBLE MANTIĞI ==========
    
    // Zorluk bias
    private float _easyBias01 = 0f;
    private float _hardBias01 = 0f;
    private int _zorlukSliderDegeri = 8;
    private const int TUMBLE_SABIT_ESIK = 8;

    // Scatter (tek kaynak: ScatterIndex)
    private float _scatterChanceNormal = 0.005f;
    private float _scatterChanceBonus = 0.001f;
    private int _scatterEsik = 4;
    private int _maxScatterPerSpin = 5;

    // Referans
    public OyunYoneticisi oyunYoneticisi;
    public List<Sprite> sembolSpriteListesi;

    // OyunYoneticisi'nden sprite listesini al
    public List<Sprite> GetSembolSpriteListesi()
    {
        if (sembolSpriteListesi != null && sembolSpriteListesi.Count > 0)
            return sembolSpriteListesi;
        
        if (oyunYoneticisi != null && oyunYoneticisi.sembolSpriteListesi != null)
            return oyunYoneticisi.sembolSpriteListesi;
            
        return null;
    }

    // ========== ZORLUK METOTLARI ==========

    public void SetZorluk(int v)
    {
        v = Mathf.Clamp(v, 4, 12);
        _zorlukSliderDegeri = v;
        
        _easyBias01 = (v < 8) ? Mathf.InverseLerp(8f, 4f, v) : 0f;
        _hardBias01 = (v > 8) ? Mathf.InverseLerp(8f, 12f, v) : 0f;

        // Scatter'ı zorluğa göre ayarla
        if (v <= 8)
        {
            float tEasy = Mathf.InverseLerp(8f, 4f, v);
            _scatterChanceNormal = Mathf.Lerp(0.010f, 0.020f, tEasy);
        }
        else
        {
            float tHard = Mathf.InverseLerp(8f, 12f, v);
            _scatterChanceNormal = Mathf.Lerp(0.010f, 0.003f, tHard);
        }
    }

    public void SetScatterAyarlari(int spriteIndex, float normalChance, float bonusChance, int esik, int maxScatter)
    {
        ScatterIndex = spriteIndex;
        _scatterChanceNormal = normalChance;
        _scatterChanceBonus = bonusChance;
        _scatterEsik = esik;
        _maxScatterPerSpin = maxScatter;
    }

    // ========== TUMBLE METOTLARI ==========

    public List<Vector2Int> FindClustersToRemove(int[,] grid, int satir, int sutun, int minSize)
    {
        if (grid == null) return new List<Vector2Int>();

        Dictionary<int, List<Vector2Int>> bySymbol = new Dictionary<int, List<Vector2Int>>();

        for (int x = 0; x < sutun; x++)
        {
            for (int y = 0; y < satir; y++)
            {
                int sym = grid[x, y];
                if (sym < 0) continue;
                if (sym == ScatterIndex) continue;

                if (!bySymbol.ContainsKey(sym))
                    bySymbol[sym] = new List<Vector2Int>();

                bySymbol[sym].Add(new Vector2Int(x, y));
            }
        }

        List<Vector2Int> toRemove = new List<Vector2Int>();
        foreach (var kv in bySymbol)
        {
            if (kv.Value.Count >= minSize)
                toRemove.AddRange(kv.Value);
        }

        return toRemove;
    }

    // Scatter sayısı - doğru parametre isimleri
    public int ScatterSayMethod(int[,] gridData, int satir, int sutun)
    {
        if (gridData == null) return 0;
        int c = 0;
        for (int y = 0; y < satir; y++)
            for (int x = 0; x < sutun; x++)
                if (gridData[x, y] == ScatterIndex) c++;
        return c;
    }

    public float CurrentScatterChance(bool bonusAktif) => bonusAktif ? _scatterChanceBonus : _scatterChanceNormal;

    // OyunYoneticisi'den çağrılan versiyon
    public int RandomSymbolWithScatterChance(int[,] grid, bool bonusAktif, int satir, int sutun)
    {
        return RandomSymbolWithScatterChanceForGrid(grid, bonusAktif, satir, sutun);
    }

    // Asıl implementasyon - countGrid yerine doğru parametre adı
    public int RandomSymbolWithScatterChanceForGrid(int[,] gridData, bool bonusAktif, int satir, int sutun)
    {
        if (gridData == null) return 0;
        
        // Scatter kontrolü - doğru metodu çağır
        int mevcutScatter = ScatterSayMethod(gridData, satir, sutun);
        
        if (mevcutScatter >= _maxScatterPerSpin)
            return RandomNonScatterSymbol(gridData, satir, sutun);
        
        if (Random.value < CurrentScatterChance(bonusAktif))
            return ScatterIndex;

        return RandomNonScatterSymbol(gridData, satir, sutun);
    }

    public float BiasMultiplier(float easyMult, float hardMult)
    {
        float m = 1f;
        if (_easyBias01 > 0f) m *= Mathf.Lerp(1f, easyMult, _easyBias01);
        if (_hardBias01 > 0f) m *= Mathf.Lerp(1f, hardMult, _hardBias01);
        return m;
    }

    public int RandomNonScatterSymbol(int[,] gridData, int satir, int sutun)
    {
        // Doğrudan OyunYoneticisi'nden al
        var spriteList = (oyunYoneticisi != null) ? oyunYoneticisi.sembolSpriteListesi : null;
        
        // Fallback: liste boşsa rastgele döndür
        if (spriteList == null || spriteList.Count <= 1)
        {
            // Rastgele 0-8 arası bir değer döndür
            return Random.Range(0, 9);
        }

        int n = spriteList.Count;
        int[] counts = new int[n];

        if (gridData != null)
        {
            for (int x = 0; x < sutun; x++)
            {
                for (int y = 0; y < satir; y++)
                {
                    int s = gridData[x, y];
                    if (s < 0) continue;
                    if (s == ScatterIndex) continue;
                    if (s >= 0 && s < n) counts[s]++;
                }
            }
        }

        // En çok görünen sembol
        int dominantIndex = -1;
        int dominantCount = -1;
        for (int i = 0; i < n; i++)
        {
            if (i == ScatterIndex) continue;
            if (counts[i] > dominantCount)
            {
                dominantCount = counts[i];
                dominantIndex = i;
            }
        }

        float totalW = 0f;
        float[] w = new float[n];
        
        for (int i = 0; i < n; i++)
        {
            if (i == ScatterIndex) { w[i] = 0f; continue; }

            float wi = 1f;
            int c = counts[i];

            if (i == dominantIndex && dominantCount >= 3)
                wi *= BiasMultiplier(1.35f, 1.00f);

            if (c == MinClusterSize - 4)
                wi *= BiasMultiplier(1.20f, 1.00f);
            else if (c == MinClusterSize - 3)
                wi *= BiasMultiplier(1.60f, 1.00f);
            else if (c == MinClusterSize - 2)
                wi *= BiasMultiplier(2.20f, 0.70f);
            else if (c >= MinClusterSize - 1)
                wi *= BiasMultiplier(5.00f, 0.25f);

            wi = Mathf.Max(wi, 0.08f);
            w[i] = wi;
            totalW += wi;
        }

        if (totalW <= 0f)
        {
            int fallback = Random.Range(0, n);
            if (fallback == ScatterIndex) fallback = (fallback + 1) % n;
            return fallback;
        }

        float r = Random.value * totalW;
        int picked = 0;
        for (int i = 0; i < n; i++)
        {
            r -= w[i];
            if (r <= 0f) { picked = i; break; }
        }

        // Fren mekanizması
        int pickedCount = counts[picked];
        bool completesTumble = (pickedCount >= MinClusterSize - 1);
        if (completesTumble)
        {
            float rerollChance = Mathf.Lerp(0.00f, 0.65f, _hardBias01);
            if (Random.value < rerollChance)
            {
                float r2 = Random.value * totalW;
                for (int i = 0; i < n; i++)
                {
                    r2 -= w[i];
                    if (r2 <= 0f) { picked = i; break; }
                }
            }
        }

        if (picked == ScatterIndex)
            picked = (picked + 1) % n;

        return picked;
    }

    public void FillRandomAll(int[,] grid, int[,] carpanDegerGrid, int[] carpanDegerByCellIndex, int satir, int sutun, bool bonusAktif, int maxTry = 30)
    {
        if (grid == null) return;

        int kalanLimitTL = int.MaxValue;

        for (int t = 0; t < maxTry; t++)
        {
            for (int y = 0; y < satir; y++)
            {
                for (int x = 0; x < sutun; x++)
                {
                    grid[x, y] = RandomSymbolWithScatterChance(grid, bonusAktif, satir, sutun);
                    if (carpanDegerGrid != null) carpanDegerGrid[x, y] = 0;
                    
                    int idx = (y * sutun) + x;
                    if (carpanDegerByCellIndex != null && idx >= 0 && idx < carpanDegerByCellIndex.Length)
                        carpanDegerByCellIndex[idx] = 0;
                }
            }

            if (!bonusAktif) break;

            List<Vector2Int> toRemove = FindClustersToRemove(grid, satir, sutun, MinClusterSize);
            if (toRemove == null || toRemove.Count == 0) break;
        }
    }
}
