using System.Collections.Generic;
using UnityEngine;

// ZORLA ÇARPAN (tek atımlık): ×100/×250/×500/Özel → BİR SONRAKİ spinde o çarpan düşer. Dispatch'te ÖNCELİKLİ (her modu ezer).
// Toggle "Çarpan Düşünce Ödeme Versin": AÇIK → en küçük küme × force (SOK ödeme); KAPALI → kazançsız grid + bomba (ödeme 0).
// Toggle YALNIZ force bağlamında geçerli (zorlaCarpan>0); mod motorlarının sözleşmesini EZMEZ — force yokken bu motor devreye girmez.

/// <summary>
/// FAZ36 İŞ D2: Zorla çarpan motoru, izole. 03-only. Uygulanabilir = zorlaCarpan>0 (dispatch dizisinde EN ÖNDE → öncelik).
/// Tek-atımlık: dispatch hook reçete dönünce zorlaSiradakiCarpan'ı sıfırlar (Spin.cs). Hiçbir instance state'e dokunmaz.
/// </summary>
public sealed class ZorlaCarpanMotoru : ISpinMotoru
{
    private const int CARPAN_SEMBOL = -2;

    public bool Uygulanabilir(MotorGirdi girdi) => girdi != null && girdi.zorlaCarpan > 0;

    public SpinSimulasyonKaydi ReceteUret(MotorGirdi g)
    {
        if (g == null || g.paytable == null || g.paytable.PayTable_8_9 == null) return null;
        int sembolSayisi = g.paytable.PayTable_8_9.Length;
        if (sembolSayisi <= 0 || g.bahis <= 0 || g.sutun <= 0 || g.satir <= 0 || g.zorlaCarpan <= 0) return null;
        int force = g.zorlaCarpan;

        // Toggle KAPALI → kazançsız dizilim + bomba (ham=0). "Çarpan düştü, para gelmedi" pedagojisi.
        if (!g.carpanOdemeAcik)
            return KazancsizZorlaRecete(g, sembolSayisi, force);

        // Toggle AÇIK → en küçük paytable kümesi × force = SOK ödeme (band cap YOK, kasıtlı).
        if (!EnKucukKumeSec(g, sembolSayisi, out int kSym, out int kCnt, out int beklenenTl) ||
            !HedefOdemeMotorBase.TryTekKumeliIlkGridOlustur(
                g.sutun, g.satir, kSym, kCnt, g.scatterIdx, sembolSayisi, out int[,] ilkGrid))
            return KazancsizZorlaRecete(g, sembolSayisi, force);   // küme yoksa (imkansız) → KAPALI davranışı

        int[,] ilkCarpanGrid = new int[g.sutun, g.satir];
        var ilkCarpanDegerleri = new List<int>();
        int finalCarpan = 1;
        if (BombaHucresiBul(ilkGrid, kSym, g.scatterIdx, g.sutun, g.satir, out Vector2Int b))
        {
            ilkGrid[b.x, b.y] = CARPAN_SEMBOL;
            ilkCarpanGrid[b.x, b.y] = force;
            ilkCarpanDegerleri.Add(force);
            finalCarpan = force;
        }

        var patlayan = ReceteAdimKurucu.SembolHucreleri(ilkGrid, kSym, g.sutun, g.satir);
        var adim = ReceteAdimKurucu.TekKumeAdim(ilkGrid, ilkCarpanGrid, patlayan, beklenenTl,
            g.sutun, g.satir, g.scatterIdx, sembolSayisi, kSym);

        var kayit = new SpinSimulasyonKaydi { Sutun = g.sutun, Satir = g.satir };
        kayit.IlkGrid = ilkGrid;
        kayit.IlkCarpanGrid = ilkCarpanGrid;
        kayit.IlkCarpanDegerleri = ilkCarpanDegerleri;
        kayit.Adimlar.Add(adim);
        kayit.ToplamHamKazanc = beklenenTl;
        kayit.NihaiCarpanToplam = Mathf.Max(1, finalCarpan);   // ödeme = beklenenTl × force (vitrin=ödeme)
        kayit.ZorlaCarpanKullanildi = true;
        kayit.SenaryoOdemeBandinaUygun = true;
        return kayit;
    }

    // KAPALI: kazançsız grid (İŞ B helper) + force bomba → Adimlar boş, ham=0, çarpan GÖRÜNÜR (formül "0 × force = 0").
    private static SpinSimulasyonKaydi KazancsizZorlaRecete(MotorGirdi g, int sembolSayisi, int force)
    {
        var grid = ReceteAdimKurucu.KazancsizGridKur(g.sutun, g.satir, sembolSayisi, g.scatterIdx);
        int[,] ilkCarpanGrid = new int[g.sutun, g.satir];
        var ilkCarpanDegerleri = new List<int>();
        if (BombaHucresiBul(grid, -1, g.scatterIdx, g.sutun, g.satir, out Vector2Int b))   // -1: kazançsız gridde herhangi hücre
        {
            grid[b.x, b.y] = CARPAN_SEMBOL;     // bomba paytable sembolü değil → cluster sayımına girmez, kazanç hâlâ 0
            ilkCarpanGrid[b.x, b.y] = force;
            ilkCarpanDegerleri.Add(force);
        }
        var kayit = new SpinSimulasyonKaydi { Sutun = g.sutun, Satir = g.satir };
        kayit.IlkGrid = grid;
        kayit.IlkCarpanGrid = ilkCarpanGrid;
        kayit.IlkCarpanDegerleri = ilkCarpanDegerleri;
        kayit.ToplamHamKazanc = 0;
        kayit.NihaiCarpanToplam = Mathf.Max(1, force);   // 0 × force = 0 (çarpan görünür, ödeme yok)
        kayit.ZorlaCarpanKullanildi = true;              // bomba iniş efekti + formül
        kayit.SenaryoOdemeBandinaUygun = true;
        return kayit;
    }

    private static bool EnKucukKumeSec(MotorGirdi g, int sembolSayisi, out int kSym, out int kCnt, out int beklenenTl)
    {
        kSym = -1; kCnt = 0; beklenenTl = -1;
        int minCluster = Mathf.Max(2, g.paytable.MinClusterSize);
        int maxHucre = Mathf.Min(12, Mathf.Max(minCluster, g.sutun * g.satir));
        int enKucuk = int.MaxValue;
        for (int sym = 0; sym < sembolSayisi; sym++)
        {
            if (sym == g.scatterIdx) continue;
            for (int cnt = minCluster; cnt <= maxHucre; cnt++)
            {
                float pay = g.paytable.GetPayForCount(sym, cnt);
                if (pay <= 0f) continue;
                int tl = Mathf.RoundToInt(pay * g.bahis);
                if (tl > 0 && tl < enKucuk) { enKucuk = tl; kSym = sym; kCnt = cnt; beklenenTl = tl; }
            }
        }
        return kSym >= 0;
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
