using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// S1–S5 hedef ödeme motorları için ortak paytable seçim ve grid kurulum mantığı.
/// TryTekKumeliIlkGridOlustur shuffle-tabanlıdır (S1/S2/S4/S5); S3 kendi BFS versiyonuyla gizler.
/// </summary>
public abstract class HedefOdemeMotorBase
{
    protected static int KumeBoyuAgirligi(int cnt)
    {
        if (cnt <= 9)  return 100;
        if (cnt <= 11) return 25;
        return 5;
    }

    private static bool AgirlikliSec(
        List<(int sym, int cnt, int tl, int agirlik)> liste,
        out int kazanSembol, out int kumeBuyuklugu, out int nihaiTl)
    {
        kazanSembol = -1; kumeBuyuklugu = 0; nihaiTl = 0;
        if (liste.Count == 0) return false;
        float toplamW = 0f;
        foreach (var a in liste) toplamW += a.agirlik;
        if (toplamW <= 0f) return false;
        float r = Random.value * toplamW;
        foreach (var a in liste)
        {
            r -= a.agirlik;
            if (r <= 0f) { kazanSembol = a.sym; kumeBuyuklugu = a.cnt; nihaiTl = a.tl; return true; }
        }
        var son = liste[liste.Count - 1];
        kazanSembol = son.sym; kumeBuyuklugu = son.cnt; nihaiTl = son.tl;
        return true;
    }

    public static bool TryPaytableUyumluTekKumeSec(
        TumbleAyarlari ta, int bahis, int minTl, int maxTl, int hedefTercihNihai,
        int scatterIdx, int sutun, int satir,
        out int kazanSembol, out int kumeBuyuklugu, out int nihaiTl)
    {
        kazanSembol = -1; kumeBuyuklugu = 0; nihaiTl = 0;
        if (ta == null || ta.PayTable_8_9 == null || ta.PayTable_8_9.Length <= 0 || bahis <= 0) return false;
        int sembolSayisi = ta.PayTable_8_9.Length;
        int minCluster = Mathf.Max(2, ta.MinClusterSize);
        int maxHucre = Mathf.Min(12, Mathf.Max(minCluster, sutun * satir));
        var adaylar = new List<(int sym, int cnt, int tl, int agirlik)>();
        for (int sym = 0; sym < sembolSayisi; sym++)
        {
            if (sym == scatterIdx) continue;
            for (int cnt = minCluster; cnt <= maxHucre; cnt++)
            {
                float payKatsayi = ta.GetPayForCount(sym, cnt);
                if (payKatsayi <= 0f) continue;
                int tl = Mathf.RoundToInt(payKatsayi * bahis);
                if (tl < minTl || tl > maxTl) continue;
                adaylar.Add((sym, cnt, tl, KumeBoyuAgirligi(cnt)));
            }
        }
        if (adaylar.Count == 0) return false;
        int tol = Mathf.Max(60, (maxTl - minTl) / 4);
        var yakin = new List<(int sym, int cnt, int tl, int agirlik)>();
        foreach (var a in adaylar)
            if (Mathf.Abs(a.tl - hedefTercihNihai) <= tol) yakin.Add(a);
        var liste = yakin.Count > 0 ? yakin : adaylar;
        return AgirlikliSec(liste, out kazanSembol, out kumeBuyuklugu, out nihaiTl);
    }

    public static bool TryPaytableUyumluIkiTumbleKumesiSec(
        TumbleAyarlari ta, int bahis, int minTl, int maxTl, int hedefTercihNihai,
        int scatterIdx, int sutun, int satir,
        out int sym1, out int c1, out int tl1, out int sym2, out int c2, out int tl2)
    {
        sym1 = sym2 = -1; c1 = c2 = tl1 = tl2 = 0;
        if (ta == null || ta.PayTable_8_9 == null || ta.PayTable_8_9.Length <= 0 || bahis <= 0) return false;
        int sembolSayisi = ta.PayTable_8_9.Length;
        int minCluster = Mathf.Max(2, ta.MinClusterSize);
        int hucre = Mathf.Max(1, sutun * satir);
        int c1Max = Mathf.Min(15, hucre - minCluster);
        int c2Max = Mathf.Min(15, hucre - minCluster);
        int hedefYakinlikTol = Mathf.Max(60, (maxTl - minTl) / 4);
        var adaylar = new List<(int s1, int n1, int t1, int s2, int n2, int t2, int agirlik)>();
        for (int s1 = 0; s1 < sembolSayisi; s1++)
        {
            if (s1 == scatterIdx) continue;
            for (int n1 = minCluster; n1 <= c1Max; n1++)
            {
                float p1 = ta.GetPayForCount(s1, n1);
                if (p1 <= 0f) continue;
                int t1v = Mathf.RoundToInt(p1 * bahis);
                if (t1v <= 0) continue;
                for (int s2 = 0; s2 < sembolSayisi; s2++)
                {
                    if (s2 == scatterIdx || s2 == s1) continue;
                    int n2Hi = Mathf.Min(c2Max, Mathf.Min(n1, hucre - n1));
                    if (minCluster > n2Hi) continue;
                    for (int n2 = minCluster; n2 <= n2Hi; n2++)
                    {
                        float p2 = ta.GetPayForCount(s2, n2);
                        if (p2 <= 0f) continue;
                        int t2v = Mathf.RoundToInt(p2 * bahis);
                        if (t2v <= 0) continue;
                        int toplam = t1v + t2v;
                        if (toplam < minTl || toplam > maxTl) continue;
                        if (Mathf.Abs(toplam - hedefTercihNihai) > hedefYakinlikTol) continue;
                        adaylar.Add((s1, n1, t1v, s2, n2, t2v, KumeBoyuAgirligi(n1) + KumeBoyuAgirligi(n2)));
                    }
                }
            }
        }
        if (adaylar.Count == 0) return false;
        float toplamW = 0f;
        foreach (var a in adaylar) toplamW += a.agirlik;
        if (toplamW <= 0f) return false;
        float r = Random.value * toplamW;
        foreach (var a in adaylar)
        {
            r -= a.agirlik;
            if (r <= 0f) { sym1 = a.s1; c1 = a.n1; tl1 = a.t1; sym2 = a.s2; c2 = a.n2; tl2 = a.t2; return true; }
        }
        var son = adaylar[adaylar.Count - 1];
        sym1 = son.s1; c1 = son.n1; tl1 = son.t1; sym2 = son.s2; c2 = son.n2; tl2 = son.t2;
        return true;
    }

    public static bool TryIkinciTumbleKumesiRefillSonrasiEnjekteEt(
        int[,] grid, int sutun, int satir, int sym2, int count2, int scatterIdx, int sembolSayisi,
        IReadOnlyList<Vector2Int> yeniSpawnHucreleri = null)
    {
        if (grid == null || sutun <= 0 || satir <= 0 || sembolSayisi <= 0) return false;
        if (sym2 < 0 || sym2 >= sembolSayisi || sym2 == scatterIdx || count2 < 8) return false;
        int[] adet = new int[sembolSayisi];
        for (int x = 0; x < sutun; x++)
            for (int y = 0; y < satir; y++) { int v = grid[x, y]; if (v >= 0 && v < sembolSayisi) adet[v]++; }
        for (int s = 0; s < sembolSayisi; s++) { if (s == scatterIdx || s == sym2) continue; if (adet[s] >= 8) return false; }
        int ihtiyac = count2 - adet[sym2];
        if (ihtiyac == 0) return true;
        if (ihtiyac < 0) return false;
        var adaylar = new List<Vector2Int>();
        if (yeniSpawnHucreleri != null && yeniSpawnHucreleri.Count > 0)
        {
            foreach (var p in yeniSpawnHucreleri) { int v = grid[p.x, p.y]; if (v != scatterIdx && v != sym2 && v >= 0 && v < sembolSayisi) adaylar.Add(p); }
        }
        else
        {
            for (int x = 0; x < sutun; x++)
                for (int y = 0; y < satir; y++) { int v = grid[x, y]; if (v != scatterIdx && v != sym2 && v >= 0 && v < sembolSayisi) adaylar.Add(new Vector2Int(x, y)); }
        }
        adaylar.Sort((a, b) => { int ca = adet[grid[a.x, a.y]], cb = adet[grid[b.x, b.y]]; int c = cb.CompareTo(ca); return c != 0 ? c : (a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y)); });
        if (adaylar.Count < ihtiyac) return false;
        for (int i = 0; i < ihtiyac; i++) { var p = adaylar[i]; adet[grid[p.x, p.y]]--; grid[p.x, p.y] = sym2; adet[sym2]++; }
        for (int s = 0; s < sembolSayisi; s++) { if (s == scatterIdx) continue; int c = adet[s]; if (s == sym2) { if (c != count2) return false; } else if (c >= 8) return false; }
        return true;
    }

    /// <summary>Rastgele shuffle ile kümeyi grid'e yerleştirir (S1/S2/S4/S5). S3 bu metodu BFS versiyonuyla gizler.</summary>
    public static bool TryTekKumeliIlkGridOlustur(
        int sutun, int satir, int kazanSembol, int kumeBuyuklugu, int scatterIdx, int sembolSayisi, out int[,] ilkGrid)
    {
        ilkGrid = null;
        if (sutun <= 0 || satir <= 0 || sembolSayisi <= 0) return false;
        if (kumeBuyuklugu < 8 || kazanSembol < 0 || kazanSembol >= sembolSayisi) return false;
        if (kazanSembol == scatterIdx) return false;
        int toplamHucre = sutun * satir;
        if (kumeBuyuklugu > toplamHucre) return false;
        var pozlar = new List<Vector2Int>(toplamHucre);
        for (int y = 0; y < satir; y++)
            for (int x = 0; x < sutun; x++)
                pozlar.Add(new Vector2Int(x, y));
        for (int i = pozlar.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            var tmp = pozlar[i]; pozlar[i] = pozlar[j]; pozlar[j] = tmp;
        }
        var dolguAdaylari = new List<int>();
        for (int s = 0; s < sembolSayisi; s++)
        {
            if (s == scatterIdx || s == kazanSembol) continue;
            dolguAdaylari.Add(s);
        }
        if (dolguAdaylari.Count == 0) return false;
        int fillHucre = toplamHucre - kumeBuyuklugu;
        int fillSembolSayisi = dolguAdaylari.Count;
        int fillMaxPerSembol = fillSembolSayisi > 0
            ? Mathf.Max(3, Mathf.CeilToInt((float)fillHucre / fillSembolSayisi) + 1)
            : 5;
        fillMaxPerSembol = Mathf.Min(fillMaxPerSembol, 5);
        ilkGrid = new int[sutun, satir];
        var sembolAdet = new int[sembolSayisi];
        for (int i = 0; i < kumeBuyuklugu; i++) { var p = pozlar[i]; ilkGrid[p.x, p.y] = kazanSembol; sembolAdet[kazanSembol]++; }
        int rot = 0;
        for (int i = kumeBuyuklugu; i < pozlar.Count; i++)
        {
            var p = pozlar[i]; int secilen = -1;
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
            ilkGrid[p.x, p.y] = secilen;
            sembolAdet[secilen]++;
        }
        return true;
    }
}
