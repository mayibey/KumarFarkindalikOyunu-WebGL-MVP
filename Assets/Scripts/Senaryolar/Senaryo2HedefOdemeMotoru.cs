using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Senaryo 2 (Kontrol Bende Hissi): kazanç spinlerinde paytable-uyumlu tek/iki-tumble grid;
/// kayıp spinlerinde en ucuz sembolden minCluster adet ile minimal geri ödeme.
/// Ortak paytable seçim ve grid kurulum mantığı HedefOdemeMotorBase'den gelir.
/// </summary>
public class Senaryo2HedefOdemeMotoru : HedefOdemeMotorBase
{
    /// <summary>
    /// Kayıp spin için: payout &lt; bahis olan sembol/adet kombinasyonları arasından rastgele birini seçer.
    /// Adaylar paytable'daki tüm ucuz seçenekleri kapsar; her kayıp spini farklı sembol ve farklı tutar verir.
    /// </summary>
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
        if (adaylar.Count == 0)
        {
            Debug.LogWarning("[SEN2][KAYIP] Geçerli kayıp aday bulunamadı");
            return false;
        }
        var sec = adaylar[Random.Range(0, adaylar.Count)];
        kayipSembol = sec.sym; kayipCnt = sec.cnt;
        return TryTekKumeliIlkGridOlustur(sutun, satir, kayipSembol, kayipCnt, scatterIdx, sembolSayisi, out ilkGrid);
    }
}
