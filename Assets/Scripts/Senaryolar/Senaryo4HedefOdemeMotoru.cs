using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Senaryo 4 (Şansın Döndü): KY→K→100x döngüsü.
/// Kazanç/kayıp spinleri paytable bandı; 3. spin mümkün en ucuz sembol kümesi × 100x.
/// Ortak paytable seçim ve grid kurulum mantığı HedefOdemeMotorBase'den gelir.
/// </summary>
public class Senaryo4HedefOdemeMotoru : HedefOdemeMotorBase
{
    public static bool TryMinimalKayipGridOlustur(
        TumbleAyarlari ta, int bahis, int scatterIdx, int sutun, int satir,
        out int kayipSembol, out int kayipCnt, out int[,] ilkGrid)
    {
        kayipSembol = -1; kayipCnt = 0; ilkGrid = null;
        if (ta == null || ta.PayTable_8_9 == null || ta.PayTable_8_9.Length <= 0 || bahis <= 0) return false;
        int sembolSayisi = ta.PayTable_8_9.Length;
        int minCluster = Mathf.Max(2, ta.MinClusterSize);
        var adaylar = new List<(int sym, int cnt, int payout)>();
        for (int sym = 0; sym < sembolSayisi; sym++)
        {
            if (sym == scatterIdx) continue;
            float pay = ta.GetPayForCount(sym, minCluster);
            if (pay <= 0f) continue;
            int payout = Mathf.RoundToInt(pay * bahis);
            if (payout > 0 && payout < bahis) adaylar.Add((sym, minCluster, payout));
        }
        if (adaylar.Count == 0) { Debug.LogWarning("[SEN4][KAYIP] Aday bulunamadı"); return false; }
        var sec = adaylar[0];
        foreach (var a in adaylar) if (a.payout < sec.payout) sec = a;
        kayipSembol = sec.sym; kayipCnt = sec.cnt;
        Debug.Log($"[SEN4][KAYIP] Seçilen sym={kayipSembol} cnt={kayipCnt} payout={sec.payout} TL (bahis={bahis})");
        return TryTekKumeliIlkGridOlustur(sutun, satir, kayipSembol, kayipCnt, scatterIdx, sembolSayisi, out ilkGrid);
    }

    /// <summary>
    /// S4 bomb spini: ham × bombDegeri ∈ [minNihai, maxNihai] olan en düşük ham ödemeli kümeyi seçer.
    /// </summary>
    public static bool TryBombNihaiHedefliGridOlustur(
        TumbleAyarlari ta, int bahis, int bombDegeri, int minNihai, int maxNihai,
        int scatterIdx, int sutun, int satir,
        out int bombSembol, out int bombCnt, out int[,] ilkGrid)
    {
        bombSembol = -1; bombCnt = 0; ilkGrid = null;
        if (ta == null || ta.PayTable_8_9 == null || ta.PayTable_8_9.Length <= 0 || bahis <= 0 || bombDegeri <= 0) return false;
        int sembolSayisi = ta.PayTable_8_9.Length;
        int minCluster = Mathf.Max(2, ta.MinClusterSize);
        var adaylar = new List<(int sym, int cnt, int hamTl)>();
        for (int sym = 0; sym < sembolSayisi; sym++)
        {
            if (sym == scatterIdx) continue;
            for (int cnt = minCluster; cnt <= minCluster + 1; cnt++)
            {
                float pay = ta.GetPayForCount(sym, cnt);
                if (pay <= 0f) continue;
                int hamTl = Mathf.RoundToInt(pay * bahis);
                if (hamTl <= 0) continue;
                int nihaiTl = hamTl * bombDegeri;
                if (nihaiTl >= minNihai && nihaiTl <= maxNihai) adaylar.Add((sym, cnt, hamTl));
            }
        }
        if (adaylar.Count == 0)
        {
            Debug.LogWarning($"[SEN4][BOMB] {minNihai}-{maxNihai} TL aralığında aday yok (bahis={bahis} bomb={bombDegeri}x)");
            return false;
        }
        var sec = adaylar[0];
        foreach (var a in adaylar) if (a.hamTl < sec.hamTl) sec = a;
        bombSembol = sec.sym; bombCnt = sec.cnt;
        Debug.Log($"[SEN4][BOMB] Seçilen sym={bombSembol} cnt={bombCnt} ham={sec.hamTl} TL × {bombDegeri}x = {sec.hamTl * bombDegeri} TL");
        return TryTekKumeliIlkGridOlustur(sutun, satir, bombSembol, bombCnt, scatterIdx, sembolSayisi, out ilkGrid);
    }

    /// <summary>Mümkün olan en küçük ödemeyi veren sembol+minCluster kombinasyonuyla grid oluşturur (100x/500x bomb spini için).</summary>
    public static bool TryCheapestBombGridOlustur(
        TumbleAyarlari ta, int scatterIdx, int sutun, int satir,
        out int cheapestSym, out int cheapestCnt, out int[,] ilkGrid)
    {
        cheapestSym = -1; cheapestCnt = 0; ilkGrid = null;
        if (ta == null || ta.PayTable_8_9 == null || ta.PayTable_8_9.Length <= 0) return false;
        int sembolSayisi = ta.PayTable_8_9.Length;
        int minCluster = Mathf.Max(2, ta.MinClusterSize);
        float minPay = float.MaxValue;
        for (int sym = 0; sym < sembolSayisi; sym++)
        {
            if (sym == scatterIdx) continue;
            float pay = ta.GetPayForCount(sym, minCluster);
            if (pay <= 0f) continue;
            if (pay < minPay) { minPay = pay; cheapestSym = sym; }
        }
        if (cheapestSym < 0) return false;
        cheapestCnt = minCluster;
        return TryTekKumeliIlkGridOlustur(sutun, satir, cheapestSym, cheapestCnt, scatterIdx, sembolSayisi, out ilkGrid);
    }
}
