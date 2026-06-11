using System.Collections.Generic;
using UnityEngine;

// BAKİYE TÜKETME: neredeyse hiç kazanç. ~%8 spin minik kırıntı (band 0.1-0.3 = 150-450),
// ~%92 spin TAM KAYIP (0). Çarpan YOK, scatter/bonus YOK (kasıtlı — yavaş kanama pedagojisi).
// KAZANC_OLASILIK=0.08f KALİBRASYON NOKTASI.

/// <summary>
/// FAZ36 İŞ B: Koruma (Bakiye Tüketme) motoru, izole. 03-only. Yontma kalıbı AMA force-win DEĞİL:
/// ~%8 kazanç (band [minKat,maksKat]×bahis = 0.1-0.3) + ~%92 kayıp (KazancsizGridKur, Adimlar boş, ham=0).
/// Çarpan: MotorCarpanServisi uniform çağrı — koruma preset carpanOdeme=KAPALI → dussun=false → no-op.
/// Scatter: zero (KazancsizGridKur scatter hariç; kazanç gridinde de enjeksiyon yok). Hiçbir instance state'e dokunmaz.
/// </summary>
public sealed class KorumaMotoru : ISpinMotoru
{
    private const int CARPAN_SEMBOL = -2;

    // KALİBRASYON NOKTASI: kazanç spini olasılığı. 0.08 ≈ ~%8 minik kazanç, ~%92 tam kayıp (legacy eğilim %8 hissi).
    private const float KAZANC_OLASILIK = 0.08f;

    // Anti-streak: motorun KENDİ iç durumu (İZOLE).
    private static int _sonSecilenSembol = -1;

    public bool Uygulanabilir(MotorGirdi girdi) => girdi != null && girdi.aktifSenaryo == "koruma";

    public SpinSimulasyonKaydi ReceteUret(MotorGirdi g)
    {
        if (g == null || g.paytable == null || g.paytable.PayTable_8_9 == null) return null;
        int sembolSayisi = g.paytable.PayTable_8_9.Length;
        if (sembolSayisi <= 0 || g.bahis <= 0 || g.sutun <= 0 || g.satir <= 0) return null;

        // ~%92 spin TAM KAYIP — kazançsız grid, ham=0, Adimlar boş.
        if (Random.value > KAZANC_OLASILIK)
            return KayipRecete(g, sembolSayisi);

        // ~%8 spin minik kazanç: band [minTl, maxTl] = bahis × [0.1, 0.3] = 150-450 @1500.
        int minTl = Mathf.RoundToInt(g.bahis * g.minKat);
        int maxTl = Mathf.RoundToInt(g.bahis * g.maksKat);
        if (maxTl < minTl) { int t = minTl; minTl = maxTl; maxTl = t; }
        if (minTl <= 0) minTl = 1;

        if (!HedefOdemeMotorBase.TryPaytableUyumluTekKumeRastgeleSec(
                g.paytable, g.bahis, minTl, maxTl, g.scatterIdx, g.sutun, g.satir,
                _sonSecilenSembol, out int kSym, out int kCnt, out int beklenenTl))
            return KayipRecete(g, sembolSayisi);   // bantta küme yoksa → kayıp (koruma zaten çoğu kayıp)

        if (!HedefOdemeMotorBase.TryTekKumeliIlkGridOlustur(
                g.sutun, g.satir, kSym, kCnt, g.scatterIdx, sembolSayisi, out int[,] ilkGrid))
            return KayipRecete(g, sembolSayisi);

        _sonSecilenSembol = kSym;

        // Çarpan: MotorCarpanServisi uniform çağrı. Koruma preset carpanOdeme=KAPALI → dussun=false → bomba yerleşmez.
        // (Defansif: düşse bile küme×toplam ≤ maxTl kısıtı.) Scatter YOK (kazanç gridine enjeksiyon yapılmaz).
        var ck = MotorCarpanServisi.Hesapla(g);
        int[,] ilkCarpanGrid = new int[g.sutun, g.satir];
        var ilkCarpanDegerleri = new List<int>();
        int finalCarpan = 1;
        if (ck.dussun && ck.degerler != null && (long)beklenenTl * ck.toplam <= maxTl)
        {
            int yerlesen = 0;
            foreach (int deger in ck.degerler)
                if (BombaHucresiBul(ilkGrid, kSym, g.scatterIdx, g.sutun, g.satir, out Vector2Int b))
                { ilkGrid[b.x, b.y] = CARPAN_SEMBOL; ilkCarpanGrid[b.x, b.y] = deger; ilkCarpanDegerleri.Add(deger); yerlesen += deger; }
            if (yerlesen > 0) finalCarpan = yerlesen;
        }

        var patlayan = ReceteAdimKurucu.SembolHucreleri(ilkGrid, kSym, g.sutun, g.satir);
        var adim = ReceteAdimKurucu.TekKumeAdim(
            ilkGrid, ilkCarpanGrid, patlayan, beklenenTl,
            g.sutun, g.satir, g.scatterIdx, sembolSayisi, kSym);

        var kayit = new SpinSimulasyonKaydi { Sutun = g.sutun, Satir = g.satir };
        kayit.IlkGrid = ilkGrid;
        kayit.IlkCarpanGrid = ilkCarpanGrid;
        kayit.IlkCarpanDegerleri = ilkCarpanDegerleri;
        kayit.Adimlar.Add(adim);
        kayit.ToplamHamKazanc = beklenenTl;
        kayit.NihaiCarpanToplam = Mathf.Max(1, finalCarpan);
        kayit.ZorlaCarpanKullanildi = finalCarpan > 1;
        kayit.SenaryoOdemeBandinaUygun = true;
        return kayit;
    }

    /// <summary>Kayıp spini: kazançsız grid (cluster yok, scatter yok), Adimlar boş, ham=0 → legacy kayıp görseli.</summary>
    private static SpinSimulasyonKaydi KayipRecete(MotorGirdi g, int sembolSayisi)
    {
        var grid = ReceteAdimKurucu.KazancsizGridKur(g.sutun, g.satir, sembolSayisi, g.scatterIdx);
        var kayit = new SpinSimulasyonKaydi { Sutun = g.sutun, Satir = g.satir };
        kayit.IlkGrid = grid;
        kayit.IlkCarpanGrid = new int[g.sutun, g.satir];
        kayit.IlkCarpanDegerleri = new List<int>();
        // Adimlar BOŞ → tumble yok, ham=0 → kayıp spini.
        kayit.ToplamHamKazanc = 0;
        kayit.NihaiCarpanToplam = 1;
        kayit.ZorlaCarpanKullanildi = false;
        kayit.SenaryoOdemeBandinaUygun = true;
        return kayit;
    }

    private static bool BombaHucresiBul(int[,] grid, int kazanSembol, int scatterIdx, int sutun, int satir, out Vector2Int hucre)
    {
        var aday = new List<Vector2Int>();
        for (int x = 0; x < sutun; x++)
            for (int y = 0; y < satir; y++)
            {
                int v = grid[x, y];
                if (v == kazanSembol || v == scatterIdx || v == CARPAN_SEMBOL) continue;
                aday.Add(new Vector2Int(x, y));
            }
        if (aday.Count == 0) { hucre = default; return false; }
        hucre = aday[Random.Range(0, aday.Count)];
        return true;
    }
}
