using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Senaryo 3 (Kaybettiklerimi Geri Kazanabilirim): kazanç spinlerinde bahis+küçük bandında tek/iki-tumble grid;
/// kayıp spinlerinde bahise yakın (yüksek) ödeme — kayıp hissedilir ama geri kazanma umudu yaşatır.
/// TryTekKumeliIlkGridOlustur BFS-tabanlıdır (bağlantılı küme garantisi); diğer ortak metodlar HedefOdemeMotorBase'den gelir.
/// </summary>
public class Senaryo3HedefOdemeMotoru : HedefOdemeMotorBase
{
    private static readonly Vector2Int[] _komsular = {
        new Vector2Int(1,0), new Vector2Int(-1,0),
        new Vector2Int(0,1), new Vector2Int(0,-1)
    };

    /// <summary>BFS ile garantili bağlantılı küme oluşturur (S3'e özgü; base sınıfın shuffle versiyonunu gizler).</summary>
    public new static bool TryTekKumeliIlkGridOlustur(
        int sutun, int satir, int kazanSembol, int kumeBuyuklugu, int scatterIdx, int sembolSayisi, out int[,] ilkGrid)
    {
        ilkGrid = null;
        if (sutun <= 0 || satir <= 0 || sembolSayisi <= 0) return false;
        if (kumeBuyuklugu < 8 || kazanSembol < 0 || kazanSembol >= sembolSayisi) return false;
        if (kazanSembol == scatterIdx) return false;
        int toplamHucre = sutun * satir;
        if (kumeBuyuklugu > toplamHucre) return false;

        var kullanilan = new bool[sutun, satir];
        var kume = new List<Vector2Int>(kumeBuyuklugu);
        var sinir = new List<Vector2Int>();

        int baslangicX = UnityEngine.Random.Range(0, sutun);
        int baslangicY = UnityEngine.Random.Range(0, satir);
        kullanilan[baslangicX, baslangicY] = true;
        kume.Add(new Vector2Int(baslangicX, baslangicY));

        foreach (var k in _komsular)
        {
            int nx = baslangicX + k.x, ny = baslangicY + k.y;
            if (nx >= 0 && nx < sutun && ny >= 0 && ny < satir)
                sinir.Add(new Vector2Int(nx, ny));
        }

        while (kume.Count < kumeBuyuklugu && sinir.Count > 0)
        {
            int idx = UnityEngine.Random.Range(0, sinir.Count);
            var sec = sinir[idx];
            sinir.RemoveAt(idx);
            if (kullanilan[sec.x, sec.y]) continue;
            kullanilan[sec.x, sec.y] = true;
            kume.Add(sec);
            foreach (var k in _komsular)
            {
                int nx = sec.x + k.x, ny = sec.y + k.y;
                if (nx >= 0 && nx < sutun && ny >= 0 && ny < satir && !kullanilan[nx, ny])
                    sinir.Add(new Vector2Int(nx, ny));
            }
        }

        if (kume.Count < kumeBuyuklugu) return false;

        var dolguAdaylari = new List<int>();
        for (int s = 0; s < sembolSayisi; s++)
        {
            if (s == scatterIdx || s == kazanSembol) continue;
            dolguAdaylari.Add(s);
        }
        if (dolguAdaylari.Count == 0) return false;

        int fillHucre = toplamHucre - kumeBuyuklugu;
        int fillMaxPerSembol = Mathf.Min(5, Mathf.Max(3, Mathf.CeilToInt((float)fillHucre / dolguAdaylari.Count) + 1));

        ilkGrid = new int[sutun, satir];
        var sembolAdet = new int[sembolSayisi];

        foreach (var p in kume) { ilkGrid[p.x, p.y] = kazanSembol; sembolAdet[kazanSembol]++; }

        int rot = 0;
        for (int y = 0; y < satir; y++)
        {
            for (int x = 0; x < sutun; x++)
            {
                if (kullanilan[x, y]) continue;
                int secilen = -1;
                for (int den = 0; den < dolguAdaylari.Count * 3; den++)
                {
                    int s = dolguAdaylari[(rot + den) % dolguAdaylari.Count];
                    if (sembolAdet[s] < fillMaxPerSembol) { secilen = s; rot = (rot + den + 1) % dolguAdaylari.Count; break; }
                }
                if (secilen < 0)
                    for (int s = 0; s < sembolSayisi; s++)
                    {
                        if (s == scatterIdx || s == kazanSembol) continue;
                        if (sembolAdet[s] < fillMaxPerSembol + 2) { secilen = s; break; }
                    }
                if (secilen < 0) return false;
                ilkGrid[x, y] = secilen;
                sembolAdet[secilen]++;
            }
        }
        return true;
    }

    /// <summary>
    /// Kayıp spin için: ödeme 0 &lt; payout &lt; bahis olan tüm (sym,cnt) kombinasyonlarından ağırlıklı seçer.
    /// Düşük ödeme (yüksek kayıp) daha olası; BFS grid kullanır.
    /// </summary>
    public static bool TryYuksekPayKayipGridOlustur(
        TumbleAyarlari ta, int bahis, int scatterIdx, int sutun, int satir,
        out int kayipSembol, out int kayipCnt, out int[,] ilkGrid)
    {
        kayipSembol = -1; kayipCnt = 0; ilkGrid = null;
        if (ta == null || ta.PayTable_8_9 == null || ta.PayTable_8_9.Length <= 0 || bahis <= 0) return false;
        int sembolSayisi = ta.PayTable_8_9.Length;
        int minCluster = Mathf.Max(2, ta.MinClusterSize);
        int maxKayipCnt = minCluster;

        var adaylar = new List<(int sym, int cnt, int agirlik)>();
        for (int sym = 0; sym < sembolSayisi; sym++)
        {
            if (sym == scatterIdx) continue;
            for (int cnt = minCluster; cnt <= maxKayipCnt; cnt++)
            {
                float pay = ta.GetPayForCount(sym, cnt);
                if (pay <= 0f) continue;
                int payout = Mathf.RoundToInt(pay * bahis);
                if (payout > 0 && payout < bahis) adaylar.Add((sym, cnt, bahis - payout));
            }
        }

        if (adaylar.Count == 0)
        {
            int maxFallback = Mathf.Min(minCluster + 3, sutun * satir);
            for (int sym = 0; sym < sembolSayisi; sym++)
            {
                if (sym == scatterIdx) continue;
                for (int cnt = minCluster; cnt <= maxFallback; cnt++)
                {
                    float pay = ta.GetPayForCount(sym, cnt);
                    if (pay <= 0f) continue;
                    int payout = Mathf.RoundToInt(pay * bahis);
                    if (payout > 0 && payout < bahis) adaylar.Add((sym, cnt, bahis - payout));
                }
            }
        }

        if (adaylar.Count == 0)
        {
            Debug.LogWarning("[SEN3][KAYIP] Geçerli kayıp aday bulunamadı");
            return false;
        }

        int toplamAgirlik = 0;
        foreach (var a in adaylar) toplamAgirlik += a.agirlik;
        int r = Random.Range(0, toplamAgirlik);
        (int sym, int cnt, int agirlik) sec = adaylar[adaylar.Count - 1];
        foreach (var a in adaylar) { r -= a.agirlik; if (r < 0) { sec = a; break; } }
        kayipSembol = sec.sym;
        kayipCnt = sec.cnt;
        int secilenPayout = Mathf.RoundToInt(sec.agirlik > 0 ? (bahis - sec.agirlik) : 0);
        Debug.Log($"[SEN3][KAYIP] Seçilen: sym={kayipSembol} cnt={kayipCnt} beklenenOdeme={secilenPayout} TL (bahis={bahis}) | adaylar={adaylar.Count} toplamW={toplamAgirlik}");
        bool gridOk = TryTekKumeliIlkGridOlustur(sutun, satir, kayipSembol, kayipCnt, scatterIdx, sembolSayisi, out ilkGrid);
        if (!gridOk) Debug.LogWarning($"[SEN3][KAYIP] TryTekKumeliIlkGridOlustur BAŞARISIZ sym={kayipSembol}");
        return gridOk;
    }
}
