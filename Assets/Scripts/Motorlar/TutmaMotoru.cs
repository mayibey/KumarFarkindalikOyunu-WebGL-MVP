using System.Collections.Generic;
using UnityEngine;

// KAÇIŞ ENGELLEME RİTMİ: 2 spin TAM KAYIP (0) → 3. spin bahisin birazcık üstü kazanç (1650-2250) → döngü.
// Sayaç motorun İÇİNDE (izole), Uygula'da sıfırlanır (epoch).

/// <summary>
/// FAZ36 İŞ C: Tutma (Kaçış Engelleme Ritmi) motoru, izole. 03-only. Sabit 2K-1K ritmi:
/// faz = _ritimSayac%3 → 0,1 = TAM KAYIP (KazancsizGridKur), 2 = kazanç (band 1.1-1.5 = 1650-2250 @1500).
/// İçsel _ritimSayac (private static, İZOLE — başka motor/legacy göremez) Uygula epoch değişince sıfırlanır.
/// Çarpan MotorCarpanServisi (slider-driven; küme×toplam ≤ maxTl → kazanç çeşitliliği {1800,2250}). Scatter zero.
/// </summary>
public sealed class TutmaMotoru : ISpinMotoru
{
    private const int CARPAN_SEMBOL = -2;

    // İçsel ritim durumu (İZOLE). _sonEpoch != girdi.uygulamaEpoch → Uygula olmuş → sayaç sıfırla.
    private static int _ritimSayac = 0;
    private static int _sonEpoch = int.MinValue;
    private static int _sonSecilenSembol = -1;

    public bool Uygulanabilir(MotorGirdi girdi) => girdi != null && girdi.aktifSenaryo == "tutma";

    public SpinSimulasyonKaydi ReceteUret(MotorGirdi g)
    {
        if (g == null || g.paytable == null || g.paytable.PayTable_8_9 == null) return null;
        int sembolSayisi = g.paytable.PayTable_8_9.Length;
        if (sembolSayisi <= 0 || g.bahis <= 0 || g.sutun <= 0 || g.satir <= 0) return null;

        // Uygula (epoch) değişince ritmi baştan başlat → sonraki 2 spin kayıp, 3. kazanç.
        if (g.uygulamaEpoch != _sonEpoch) { _ritimSayac = 0; _sonEpoch = g.uygulamaEpoch; }

        int faz = _ritimSayac % 3;   // 0,1 = KAYIP ; 2 = KAZANÇ
        _ritimSayac++;

        if (faz < 2)
            return KayipRecete(g, sembolSayisi);   // 2 spin TAM KAYIP

        // faz==2: kazanç spini — band [minTl,maxTl] = bahis × [1.1, 1.5] = 1650-2250.
        int minTl = Mathf.RoundToInt(g.bahis * g.minKat);
        int maxTl = Mathf.RoundToInt(g.bahis * g.maksKat);
        if (maxTl < minTl) { int t = minTl; minTl = maxTl; maxTl = t; }
        if (minTl <= 0) minTl = 1;

        // Çarpan (MotorCarpanServisi, slider-driven). Düşerse küçük küme [minTl/toplam, maxTl/toplam] → küme×toplam ∈ band.
        var ck = MotorCarpanServisi.Hesapla(g);
        bool carpanli = ck.dussun && ck.toplam > 1;
        int secMinTl = carpanli ? Mathf.CeilToInt(minTl / (float)ck.toplam) : minTl;
        int secMaxTl = carpanli ? maxTl / ck.toplam : maxTl;
        if (carpanli && secMaxTl < secMinTl) { carpanli = false; secMinTl = minTl; secMaxTl = maxTl; }

        int kSym, kCnt, beklenenTl;
        if (!HedefOdemeMotorBase.TryPaytableUyumluTekKumeRastgeleSec(
                g.paytable, g.bahis, secMinTl, secMaxTl, g.scatterIdx, g.sutun, g.satir,
                _sonSecilenSembol, out kSym, out kCnt, out beklenenTl))
        {
            carpanli = false;
            if (!HedefOdemeMotorBase.TryPaytableUyumluTekKumeRastgeleSec(
                    g.paytable, g.bahis, minTl, maxTl, g.scatterIdx, g.sutun, g.satir,
                    _sonSecilenSembol, out kSym, out kCnt, out beklenenTl))
            {
                if (!EnYakinKumeSec(g, sembolSayisi, minTl, out kSym, out kCnt, out beklenenTl))
                    return KayipRecete(g, sembolSayisi);   // kazanç bulunamazsa kayıp (ritim bozulmasın, defansif)
            }
        }
        if (!HedefOdemeMotorBase.TryTekKumeliIlkGridOlustur(
                g.sutun, g.satir, kSym, kCnt, g.scatterIdx, sembolSayisi, out int[,] ilkGrid))
            return KayipRecete(g, sembolSayisi);
        _sonSecilenSembol = kSym;

        int[,] ilkCarpanGrid = new int[g.sutun, g.satir];
        var ilkCarpanDegerleri = new List<int>();
        int finalCarpan = 1;
        if (carpanli && ck.degerler != null)
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
