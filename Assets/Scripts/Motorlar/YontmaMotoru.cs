using System.Collections.Generic;
using UnityEngine;

// SÖZLEŞME: her spin 0 < ödeme < bahis. Sıfır yasak, bahis üstü yasak.
// SCATTER/BONUS YOK (kasıtlı): kesintisiz ufak-kayıp pedagojisi — bonus patlaması
// 'sessiz erime' mesajını bozar. Bug değil, tasarım.

/// <summary>
/// FAZ36: Yontma motoru (35.153 mantığı, izole). 03-only. HER spin kazanç, 0 &lt; ödeme &lt; bahis ("ufak ufak yont").
/// - Band [minKat,maksKat]×bahis (panel preset 0.2-0.7) içinde tek küme seçer → force-win, asla 0.
/// - Cascade KAPALI: tek tumble adımı, refill KAZANÇSIZ (ReceteAdimKurucu) → ham = beklenenTl ≤ band max &lt; bahis.
/// - Bonus çarpan: beklenenTl × carpan &lt; bahis kısıtı (çarpan GERÇEK ödemeye biniyor: nihaiOdeme = ham × carpan).
///
/// Hiçbir instance state'e/flag'e dokunmaz; yalnız MotorGirdi okur, SpinSimulasyonKaydi üretir (ANAYASA rule 2/4).
/// Mevcut motor (SimuleEtVeKaydetImpl) ve diğer modlar BU DOSYADAN ETKİLENMEZ.
/// </summary>
public sealed class YontmaMotoru : ISpinMotoru
{
    private const int CARPAN_SEMBOL = -2;

    // Anti-streak: motorun KENDİ iç durumu (başka motor/legacy göremez — ANAYASA rule 2).
    private static int _sonSecilenSembol = -1;

    public bool Uygulanabilir(MotorGirdi girdi) => girdi != null && girdi.aktifSenaryo == "yontma";

    public SpinSimulasyonKaydi ReceteUret(MotorGirdi g)
    {
        if (g == null || g.paytable == null || g.paytable.PayTable_8_9 == null) return null;
        int sembolSayisi = g.paytable.PayTable_8_9.Length;
        if (sembolSayisi <= 0 || g.bahis <= 0 || g.sutun <= 0 || g.satir <= 0) return null;

        int minTl = Mathf.RoundToInt(g.bahis * g.minKat);
        int maxTl = Mathf.RoundToInt(g.bahis * g.maksKat);
        if (maxTl < minTl) { int t = minTl; minTl = maxTl; maxTl = t; }
        if (minTl <= 0) minTl = 1;   // yontma: taban asla 0

        // 1. Küme seç (band içi, eşit şans + anti-streak). HedefOdemeMotorBase static = read-only kütüphane.
        if (!HedefOdemeMotorBase.TryPaytableUyumluTekKumeRastgeleSec(
                g.paytable, g.bahis, minTl, maxTl, g.scatterIdx, g.sutun, g.satir,
                _sonSecilenSembol, out int kSym, out int kCnt, out int beklenenTl))
        {
            // Fallback (35.148 A2 emsali): bantta küme yok → ≥minTl en küçük (yoksa en büyük) küme. Yontma asla 0.
            if (!EnYakinKumeSec(g, sembolSayisi, minTl, out kSym, out kCnt, out beklenenTl))
                return null;   // imkansız (paytable tamamen boş) → legacy çalışsın
        }

        // 2. Grid kur (tek küme, kalanı başka sembol — max 5/sembol). Static = read-only.
        if (!HedefOdemeMotorBase.TryTekKumeliIlkGridOlustur(
                g.sutun, g.satir, kSym, kCnt, g.scatterIdx, sembolSayisi, out int[,] ilkGrid))
            return null;

        _sonSecilenSembol = kSym;

        // 3. Çarpan (FAZ36 İŞ A): CarpanServisi panel slider'ına göre. SADECE küme×Σçarpan < bahis ise uygula
        //    (yontma sözleşmesi: ödeme < bahis korunur). beklenenTl ∈ band zaten < bahis; çarpan ancak fit ederse biner.
        var ck = MotorCarpanServisi.Hesapla(g);
        int[,] ilkCarpanGrid = new int[g.sutun, g.satir];
        var ilkCarpanDegerleri = new List<int>();
        int finalCarpan = 1;
        if (ck.dussun && ck.degerler != null && (long)beklenenTl * ck.toplam < g.bahis)
        {
            int yerlesen = 0;
            foreach (int deger in ck.degerler)
            {
                // Küme DIŞI hücreye CARPAN_SEMBOL bomba (cluster bozulmaz → küme TL'si sabit).
                if (BombaHucresiBul(ilkGrid, kSym, g.scatterIdx, g.sutun, g.satir, out Vector2Int bomba))
                {
                    ilkGrid[bomba.x, bomba.y] = CARPAN_SEMBOL;
                    ilkCarpanGrid[bomba.x, bomba.y] = deger;
                    ilkCarpanDegerleri.Add(deger);
                    yerlesen += deger;
                }
            }
            if (yerlesen > 0) finalCarpan = yerlesen;   // yerlesen ≤ toplam → küme×yerlesen < bahis korunur
        }

        // 4. Reçete kur. Cascade KAPALI → tek adım. patlayan = griddeki TÜM kSym hücreleri.
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
        kayit.ZorlaCarpanKullanildi = finalCarpan > 1;   // bomba iniş efekti + çarpan text kilidi tetikler
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

    /// <summary>Bantta küme yoksa: ≥minTl en küçük (yoksa genel en büyük) küme — yontma asla 0.</summary>
    private static bool EnYakinKumeSec(MotorGirdi g, int sembolSayisi, int minTl,
        out int kSym, out int kCnt, out int beklenenTl)
    {
        kSym = -1; kCnt = 0; beklenenTl = -1;
        int minCluster = Mathf.Max(2, g.paytable.MinClusterSize);
        int maxHucre = Mathf.Min(12, Mathf.Max(minCluster, g.sutun * g.satir));
        int enYakinSym = -1, enYakinCnt = 0, enYakinTl = -1;
        int enBuyukSym = -1, enBuyukCnt = 0, enBuyukTl = -1;
        for (int sym = 0; sym < sembolSayisi; sym++)
        {
            if (sym == g.scatterIdx) continue;
            for (int cnt = minCluster; cnt <= maxHucre; cnt++)
            {
                float pay = g.paytable.GetPayForCount(sym, cnt);
                if (pay <= 0f) continue;
                int tl = Mathf.RoundToInt(pay * g.bahis);
                if (tl <= 0) continue;
                if (tl >= minTl && (enYakinTl < 0 || tl < enYakinTl)) { enYakinTl = tl; enYakinSym = sym; enYakinCnt = cnt; }
                if (tl > enBuyukTl) { enBuyukTl = tl; enBuyukSym = sym; enBuyukCnt = cnt; }
            }
        }
        if (enYakinSym >= 0) { kSym = enYakinSym; kCnt = enYakinCnt; beklenenTl = enYakinTl; return true; }
        if (enBuyukSym >= 0) { kSym = enBuyukSym; kCnt = enBuyukCnt; beklenenTl = enBuyukTl; return true; }
        return false;
    }
}
