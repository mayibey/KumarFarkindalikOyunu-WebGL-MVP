using System.Collections.Generic;
using UnityEngine;

// SÖZLEŞME: her spin KAZANÇ (sıfır yasak), ödeme bahisin BİRAZ üstü (band 1.1-1.7).
// SCATTER/BONUS VAR (kasıtlı): taze kan = oyuncuyu bağlama — bonus heyecanı/dopamin
// bağlamanın aracı. Yontma'nın TERSİ (orada bonus pedagojiyi bozardı, burada besler).
// ÇARPAN ŞOVU: ~%30 spin bilinçli küçük küme + 2x çarpan → küme×çarpan band içi (vitrin=ödeme korunur).

/// <summary>
/// FAZ36 ADIM 2: Hook (Taze Kan) motoru, izole. 03-only. HER spin kazanç, ödeme bahisin biraz üstü.
/// YontmaMotoru kalıbının band'i yukarı kaydırılmış + çarpan tavanı maxTl + scatter/bonus AÇIK versiyonu.
/// - Band [minKat,maksKat]×bahis (panel preset 1.1-1.7) → force-win, asla 0.
/// - Çarpan: küme×çarpan ≤ maxTl HER ZAMAN. ~%30 spin 2x çarpan (küçük küme×2 = band içi), gerisi çarpansız.
/// - Scatter: olasılıksal enjeksiyon → ara sıra eşik aşılır → legacy bonus oyunu tetiklenir.
/// Hiçbir instance state'e/flag'e dokunmaz; yalnız MotorGirdi okur, SpinSimulasyonKaydi üretir.
/// </summary>
public sealed class HookMotoru : ISpinMotoru
{
    private const int CARPAN_SEMBOL = -2;

    // Çarpan şovu olasılığı: bu oranda spin küçük küme + 2x (vitrin'de çarpan görünür, ödeme band içi).
    private const float CARPAN_OLASILIK = 0.30f;
    private const int CARPAN_DEGERI = 2;

    // KALİBRASYON NOKTASI: bonus eşiği aşma olasılığı. 0.03 ≈ 1 bonus / 33 spin (hedef 30-40).
    // Build sonrası 03'te gözle ayarla — büyütürsen daha sık bonus, küçültürsen seyrek.
    private const float BONUS_OLASILIK = 0.03f;

    // = OyunYoneticisi.scatterEsik (Fields.cs:397 default 4 / BonusAyarlari.cs:31 ScatterEsik=4).
    // BonusAyarlari.ScatterEsik Inspector'dan değiştirilirse BU SABİTİ senkronla (Spin.cs hook'a dokunmamak için sabit).
    private const int SCATTER_ESIK = 4;

    // Anti-streak: motorun KENDİ iç durumu (başka motor/legacy göremez — ANAYASA rule 2).
    private static int _sonSecilenSembol = -1;

    public bool Uygulanabilir(MotorGirdi girdi) => girdi != null && girdi.aktifSenaryo == "hook";

    public SpinSimulasyonKaydi ReceteUret(MotorGirdi g)
    {
        if (g == null || g.paytable == null || g.paytable.PayTable_8_9 == null) return null;
        int sembolSayisi = g.paytable.PayTable_8_9.Length;
        if (sembolSayisi <= 0 || g.bahis <= 0 || g.sutun <= 0 || g.satir <= 0) return null;

        int minTl = Mathf.RoundToInt(g.bahis * g.minKat);   // 1.1×bahis = 1650 @1500
        int maxTl = Mathf.RoundToInt(g.bahis * g.maksKat);  // 1.7×bahis = 2550 @1500
        if (maxTl < minTl) { int t = minTl; minTl = maxTl; maxTl = t; }
        if (minTl <= 0) minTl = 1;

        // ÇARPAN KARARI: ~%30 spin 2x çarpan → küme [minTl/2, maxTl/2] seç, ×2 = band içi.
        //                ~%70 spin çarpansız → küme [minTl, maxTl] (final = küme, band içi).
        // Kısıt küme×çarpan ≤ maxTl HER İKİ yolda da inşaen sağlanır.
        bool carpanli = Random.value < CARPAN_OLASILIK;
        int carpan = carpanli ? CARPAN_DEGERI : 1;
        int secMinTl = carpanli ? Mathf.CeilToInt(minTl / (float)carpan) : minTl;
        int secMaxTl = carpanli ? maxTl / carpan : maxTl;

        int kSym, kCnt, beklenenTl;
        if (!HedefOdemeMotorBase.TryPaytableUyumluTekKumeRastgeleSec(
                g.paytable, g.bahis, secMinTl, secMaxTl, g.scatterIdx, g.sutun, g.satir,
                _sonSecilenSembol, out kSym, out kCnt, out beklenenTl))
        {
            // Çarpan yolu başarısız → çarpansız tam band'a düş; o da yoksa A2 en yakın (full band, carpan=1).
            carpan = 1;
            if (!HedefOdemeMotorBase.TryPaytableUyumluTekKumeRastgeleSec(
                    g.paytable, g.bahis, minTl, maxTl, g.scatterIdx, g.sutun, g.satir,
                    _sonSecilenSembol, out kSym, out kCnt, out beklenenTl))
            {
                if (!EnYakinKumeSec(g, sembolSayisi, minTl, out kSym, out kCnt, out beklenenTl))
                    return null;
            }
        }

        if (!HedefOdemeMotorBase.TryTekKumeliIlkGridOlustur(
                g.sutun, g.satir, kSym, kCnt, g.scatterIdx, sembolSayisi, out int[,] ilkGrid))
            return null;

        _sonSecilenSembol = kSym;

        // Çarpan bombası (küme DIŞI hücreye → küme TL'si sabit, küme×çarpan ≤ maxTl korunur).
        int[,] ilkCarpanGrid = new int[g.sutun, g.satir];
        var ilkCarpanDegerleri = new List<int>();
        if (carpan > 1)
        {
            if (BombaHucresiBul(ilkGrid, kSym, g.scatterIdx, g.sutun, g.satir, out Vector2Int bomba))
            {
                ilkGrid[bomba.x, bomba.y] = CARPAN_SEMBOL;
                ilkCarpanGrid[bomba.x, bomba.y] = carpan;
                ilkCarpanDegerleri.Add(carpan);
            }
            else carpan = 1;   // yer yoksa çarpansız (savunmacı)
        }

        // SCATTER (h2): olasılıksal enjeksiyon. Bonus olasılığı tutarsa eşik kadar (bonus tetikler),
        // değilse eşik-altı görsel heyecan. Scatter küme dışı → tumble'da silinmez (ReceteAdimKurucu dokunmaz).
        int scatterSayisi = Random.value < BONUS_OLASILIK
            ? SCATTER_ESIK                         // sc >= esik → DonusAkisServisi bonus tetikler (bonus oyunu LEGACY'de)
            : Random.Range(0, SCATTER_ESIK - 1);   // 0..esik-2 görsel heyecan, eşik altı garanti
        ScatterEnjekte(ilkGrid, scatterSayisi, kSym, g.scatterIdx, g.sutun, g.satir);

        // Reçete (cascade kapalı tek adım).
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
        kayit.NihaiCarpanToplam = Mathf.Max(1, carpan);   // final ödeme = beklenenTl × carpan ∈ [minTl, maxTl]
        kayit.ZorlaCarpanKullanildi = carpan > 1;
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

    /// <summary>Küme/bomba/scatter dışı hücrelere `adet` kadar scatter yerleştirir (ScatterSay → bonus tetik kaynağı).</summary>
    private static void ScatterEnjekte(int[,] grid, int adet, int kazanSembol, int scatterIdx, int sutun, int satir)
    {
        if (adet <= 0 || scatterIdx < 0) return;
        var aday = new List<Vector2Int>();
        for (int x = 0; x < sutun; x++)
            for (int y = 0; y < satir; y++)
            {
                int v = grid[x, y];
                if (v == kazanSembol || v == CARPAN_SEMBOL || v == scatterIdx) continue;
                aday.Add(new Vector2Int(x, y));
            }
        int kac = Mathf.Min(adet, aday.Count);
        for (int i = 0; i < kac; i++)
        {
            int r = Random.Range(i, aday.Count);
            var t = aday[r]; aday[r] = aday[i]; aday[i] = t;   // Fisher-Yates kısmi
            grid[aday[i].x, aday[i].y] = scatterIdx;
        }
    }

    /// <summary>Bantta küme yoksa: ≥minTl en küçük (yoksa genel en büyük) küme — hook asla 0.</summary>
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
