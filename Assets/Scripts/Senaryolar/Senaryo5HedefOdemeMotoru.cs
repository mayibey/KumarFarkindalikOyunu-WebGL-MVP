using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Senaryo 5 (Sonunu Düşünen Kahraman Olamaz): K→KY→500x döngüsü.
/// Kazanç/kayıp spinleri paytable bandı; 3. spin mümkün en ucuz sembol kümesi × 500x.
/// Ortak paytable seçim ve grid kurulum mantığı HedefOdemeMotorBase'den gelir.
/// </summary>
public class Senaryo5HedefOdemeMotoru : HedefOdemeMotorBase
{
    public static bool TryMinimalKayipGridOlustur(
        TumbleAyarlari ta, int bahis, int scatterIdx, int sutun, int satir,
        out int kayipSembol, out int kayipCnt, out int[,] ilkGrid)
    {
        kayipSembol = -1; kayipCnt = 0; ilkGrid = null;
        if (ta == null || ta.PayTable_8_9 == null || ta.PayTable_8_9.Length <= 0 || bahis <= 0) return false;
        int sembolSayisi = ta.PayTable_8_9.Length;
        int minCluster = Mathf.Max(2, ta.MinClusterSize);
        int maxKayipCnt = Mathf.Min(minCluster + 2, sutun * satir);
        var adaylar = new List<(int sym, int cnt)>();
        for (int sym = 0; sym < sembolSayisi; sym++)
        {
            if (sym == scatterIdx) continue;
            for (int cnt = minCluster; cnt <= maxKayipCnt; cnt++)
            {
                float pay = ta.GetPayForCount(sym, cnt);
                if (pay <= 0f) continue;
                int payout = Mathf.RoundToInt(pay * bahis);
                if (payout > 0 && payout < bahis) adaylar.Add((sym, cnt));
            }
        }
        if (adaylar.Count == 0) { Debug.LogWarning("[SEN5][KAYIP] Aday bulunamadı"); return false; }
        var sec = adaylar[Random.Range(0, adaylar.Count)];
        kayipSembol = sec.sym; kayipCnt = sec.cnt;
        return TryTekKumeliIlkGridOlustur(sutun, satir, kayipSembol, kayipCnt, scatterIdx, sembolSayisi, out ilkGrid);
    }

    /// <summary>Payout'u [minPayout..maxPayout] aralığında olan sembol+cnt seçer; aday yoksa TryMinimalKayipGridOlustur'a düşer.</summary>
    public static bool TryRangeliKayipGridOlustur(
        TumbleAyarlari ta, int bahis, int minPayout, int maxPayout,
        int scatterIdx, int sutun, int satir,
        out int kayipSembol, out int kayipCnt, out int[,] ilkGrid)
    {
        kayipSembol = -1; kayipCnt = 0; ilkGrid = null;
        if (ta == null || ta.PayTable_8_9 == null || ta.PayTable_8_9.Length <= 0 || bahis <= 0) return false;
        int sembolSayisi = ta.PayTable_8_9.Length;
        int minCluster = Mathf.Max(2, ta.MinClusterSize);
        int maxCnt = Mathf.Min(minCluster + 2, sutun * satir);
        var adaylar = new List<(int sym, int cnt)>();
        for (int sym = 0; sym < sembolSayisi; sym++)
        {
            if (sym == scatterIdx) continue;
            for (int cnt = minCluster; cnt <= maxCnt; cnt++)
            {
                float pay = ta.GetPayForCount(sym, cnt);
                if (pay <= 0f) continue;
                int payout = Mathf.RoundToInt(pay * bahis);
                if (payout >= minPayout && payout <= maxPayout) adaylar.Add((sym, cnt));
            }
        }
        if (adaylar.Count == 0)
        {
            Debug.LogWarning($"[SEN5][KAYIP_RANGED] {minPayout}..{maxPayout} TL aday yok, fallback");
            return TryMinimalKayipGridOlustur(ta, bahis, scatterIdx, sutun, satir, out kayipSembol, out kayipCnt, out ilkGrid);
        }
        var sec = adaylar[Random.Range(0, adaylar.Count)];
        kayipSembol = sec.sym; kayipCnt = sec.cnt;
        return TryTekKumeliIlkGridOlustur(sutun, satir, kayipSembol, kayipCnt, scatterIdx, sembolSayisi, out ilkGrid);
    }

    /// <summary>Mümkün olan en küçük ödemeyi veren sembol+minCluster kombinasyonuyla grid oluşturur (500x bomb spini için).</summary>
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
