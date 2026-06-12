using System.Collections.Generic;
using UnityEngine;

// NEAR-MISS ("Neredeyse Oluyordu"): HER spin "az kaldı" görseli — 3 scatter (bonus eşik 4'ün 1 altı) +
// 7 aynı meyve (cluster eşik 8'in 1 altı) + kalan <8'lik gruplar. ÖDEME = 0 (matematiksel imkansız).
// Tetik: yakinKacirma>0 (Manipülasyon Türü = Near-miss radio), MOD-BAĞIMSIZ (legacy FAZ35.145 ile aynı).

/// <summary>
/// FAZ36.8: NearMiss motoru, izole. 03-only (dispatch buildIndex==2 + !bonusSpin garantisi). STATELESS — sayaç/epoch YOK.
/// Her spin near-miss: ReceteAdimKurucu.NearMissGridKur → 3 scatter + 7 meyve A + kalan <8 gruplar. Ödeme=0:
/// Adimlar boş + ham=0 (KorumaMotoru.KayipRecete deseni). MotorBonusServisi/MotorCarpanServisi ÇAĞRILMAZ.
/// scatterBaskila aktifken (Çarpan testi) devre dışı — tek-aktif-blok rejimi zaten ayırır, bu savunma guard'ı.
/// Dispatch sırası: ZorlaCarpan SONRASI / mod motorları ÖNCESI → force ezmez, modu ezer (near-miss mod-bağımsız).
/// Hiçbir instance state'e/legacy'ye dokunmaz; legacy GrideNearMissEnjekteEt (normal mod) DEĞİŞMEDEN durur.
/// </summary>
public sealed class NearMissMotoru : ISpinMotoru
{
    public bool Uygulanabilir(MotorGirdi girdi)
        => girdi != null && girdi.yakinKacirma > 0 && !PanelKopru.ScatterBaskilaAktif;

    public SpinSimulasyonKaydi ReceteUret(MotorGirdi g)
    {
        if (g == null || g.paytable == null || g.paytable.PayTable_8_9 == null) return null;
        int sembolSayisi = g.paytable.PayTable_8_9.Length;
        if (sembolSayisi <= 0 || g.sutun <= 0 || g.satir <= 0) return null;

        var grid = ReceteAdimKurucu.NearMissGridKur(
            g.sutun, g.satir, sembolSayisi, g.scatterIdx,
            out int scatterSay, out int meyveA, out int meyveAYerlesen);

        var kayit = new SpinSimulasyonKaydi { Sutun = g.sutun, Satir = g.satir };
        kayit.IlkGrid = grid;
        kayit.IlkCarpanGrid = new int[g.sutun, g.satir];
        kayit.IlkCarpanDegerleri = new List<int>();
        // Adimlar BOŞ → tumble yok, ham=0 → "az kaldı ama kazanamadın" görseli (legacy kayıp-spin oynatması).
        kayit.ToplamHamKazanc = 0;
        kayit.NihaiCarpanToplam = 1;
        kayit.ZorlaCarpanKullanildi = false;
        kayit.SenaryoOdemeBandinaUygun = true;

        // Kalıcı davranış göstergesi (teşhis değil): canlıda "tam 3 scatter + 7 meyve" doğrulanır.
        Debug.Log($"[NEARMISS] Reçete: {scatterSay} scatter (eşik 4 → bonus yok) + {meyveAYerlesen} meyve A=idx{meyveA} (eşik 8 → ödeme yok).");
        return kayit;
    }
}
