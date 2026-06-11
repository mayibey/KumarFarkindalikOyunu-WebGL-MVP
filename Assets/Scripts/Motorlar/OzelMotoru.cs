using System.Collections.Generic;
using UnityEngine;

// SÖZLEŞME (FAZ36.4): admin MIN/MAX band modu ("ozel"). HER spin KAZANÇ. Band SADECE HAM küme ödemesini sınırlar:
// ham ∈ [minKat,maksKat]×bahis (örn 0.1-0.3 × 1500 = 150-450 TL). ÇARPAN tamamen BAĞIMSIZ — slider düştü dediyse
// aynen uygulanır, nihai = ham × Σçarpan BANT DIŞINA çıkabilir (artık hata değil, tasarım; "bant nihaiyi sınırlar" İPTAL).
// SCATTER/BONUS: panel bonus periyoduna göre (MotorBonusServisi); eşik altı → görsel, eşik kadar → legacy bonus tetik.

/// <summary>
/// FAZ36.2: Ozel (admin MIN/MAX band) motoru, izole. 03-only. HER spin band içi kazanç.
/// HookMotoru kalıbının band'i admin slider'ından (g.minKat/g.maksKat) alınmış versiyonu — band yukarı/aşağı
/// (bahis altı 0.1-0.3 de olabilir) fark etmez, matematik band-agnostik.
/// STATELESS: ritim/epoch sayacı YOK (Tutma'nın _ritimSayac'ı gibi bir şey taşımaz). Tek iç durum: izole
/// anti-streak _sonSecilenSembol (başka motor/legacy göremez — ANAYASA rule 2). MotorGirdi okur, kayıt üretir.
///
/// KÖK NEDEN (carpanUretimiAktif raporu): Ozel mod eskiden legacy mod-konstrukte yolunu kullanıyordu; o yolun
/// FAZ35.93 _modKonstrukteBasarili guard'ı doğal çarpanı her base spinde kapatıyordu (slider %100 etkisiz).
/// Bu motor ozel'i kendi katmanına alır → guard baypas, çarpan BAĞIMSIZ slider'a tabi çalışır (band-fit yok).
/// </summary>
public sealed class OzelMotoru : ISpinMotoru
{
    private const int CARPAN_SEMBOL = -2;

    // Anti-streak: motorun KENDİ iç durumu (İZOLE — başka motor/legacy göremez). Ritim/epoch sayacı DEĞİL.
    private static int _sonSecilenSembol = -1;

    public bool Uygulanabilir(MotorGirdi girdi) => girdi != null && girdi.aktifSenaryo == "ozel";

    public SpinSimulasyonKaydi ReceteUret(MotorGirdi g)
    {
        if (g == null || g.paytable == null || g.paytable.PayTable_8_9 == null) return null;
        int sembolSayisi = g.paytable.PayTable_8_9.Length;
        if (sembolSayisi <= 0 || g.bahis <= 0 || g.sutun <= 0 || g.satir <= 0) return null;

        int minTl = Mathf.RoundToInt(g.bahis * g.minKat);   // admin MIN katı × bahis
        int maxTl = Mathf.RoundToInt(g.bahis * g.maksKat);  // admin MAX katı × bahis
        if (maxTl < minTl) { int t = minTl; minTl = maxTl; maxTl = t; }
        if (minTl <= 0) minTl = 1;   // ozel: her spin kazanç → taban asla 0

        // KÜME SEÇİMİ (FAZ36.4): ham küme ödemesi ∈ [minTl, maxTl]. TEK yol — alt-band/band-fit YOK.
        // Yeni sözleşme: bant SADECE ham kümeyi sınırlar; çarpan AYRI/BAĞIMSIZ (aşağıda), nihai = ham × çarpan
        // bant dışına çıkabilir (artık hata değil, tasarım kararı). "Bant nihaiyi sınırlar" kuralı İPTAL.
        int kSym, kCnt, beklenenTl;
        if (!HedefOdemeMotorBase.TryPaytableUyumluTekKumeRastgeleSec(
                g.paytable, g.bahis, minTl, maxTl, g.scatterIdx, g.sutun, g.satir,
                _sonSecilenSembol, out kSym, out kCnt, out beklenenTl))
        {
            // Bantta küme yoksa → A2 en yakın (ozel her koşulda kazanır); o da yoksa null → legacy.
            if (!EnYakinKumeSec(g, sembolSayisi, minTl, out kSym, out kCnt, out beklenenTl))
                return null;   // imkansız (paytable tamamen boş) → legacy çalışsın (güvenli varsayılan)
        }

        if (!HedefOdemeMotorBase.TryTekKumeliIlkGridOlustur(
                g.sutun, g.satir, kSym, kCnt, g.scatterIdx, sembolSayisi, out int[,] ilkGrid))
            return null;

        _sonSecilenSembol = kSym;

        // ÇARPAN (FAZ36.4 BAĞIMSIZ): MotorCarpanServisi düştü dediyse AYNEN uygula — nihai = ham × Σçarpan.
        // Band-fit/kırpma YOK; çarpan slider'ı tek başına yönetir, bant ham kümeyi zaten sınırladı, çarpan üstüne biner.
        // Küme DIŞI hücrelere bomba (küme TL'si sabit). Yerleşemeyen bomba sayılmaz → finalCarpan yalnız yerleşeni yansıtır.
        var ck = MotorCarpanServisi.Hesapla(g);
        int[,] ilkCarpanGrid = new int[g.sutun, g.satir];
        var ilkCarpanDegerleri = new List<int>();
        int finalCarpan = 1;
        if (ck.dussun && ck.degerler != null)
        {
            int yerlesen = 0;
            foreach (int deger in ck.degerler)
            {
                if (BombaHucresiBul(ilkGrid, kSym, g.scatterIdx, g.sutun, g.satir, out Vector2Int bomba))
                {
                    ilkGrid[bomba.x, bomba.y] = CARPAN_SEMBOL;
                    ilkCarpanGrid[bomba.x, bomba.y] = deger;
                    ilkCarpanDegerleri.Add(deger);
                    yerlesen += deger;
                }
            }
            if (yerlesen > 0) finalCarpan = yerlesen;   // yerlesen = uygulanan toplam çarpan (bant dışı olabilir, bağımsız)
        }

        // SCATTER/BONUS: karar MotorBonusServisi'nde (çarpandan AYRI, panel bonus periyoduna bağlı — Faz 36.1 slider
        // çift besleme ile legacy yol ile tutarlı). Eşik altı → görsel; ScatterEnjekte adet=0 ise no-op (zero-scatter).
        int scatterSayisi = MotorBonusServisi.Hesapla(g).scatterSayisi;
        ScatterEnjekte(ilkGrid, scatterSayisi, kSym, g.scatterIdx, g.sutun, g.satir);

        // Reçete (cascade kapalı tek adım). patlayan = griddeki TÜM kSym hücreleri.
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
        kayit.NihaiCarpanToplam = Mathf.Max(1, finalCarpan);   // nihai ödeme = beklenenTl × Σçarpan (bant dışı olabilir — bağımsız)
        kayit.ZorlaCarpanKullanildi = finalCarpan > 1;
        kayit.SenaryoOdemeBandinaUygun = true;
        // FAZ36.2/36.4 KALICI ÖZET (motorun tek gözü): band ham kümeyi sınırlar; nihai = ham × çarpan bant dışı olabilir (tasarım).
        Debug.Log($"[OzelMotoru] band=[{minTl}-{maxTl}]TL ham={beklenenTl} carpan={finalCarpan} nihai={beklenenTl * Mathf.Max(1, finalCarpan)}");
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

    /// <summary>Bantta küme yoksa: ≥minTl en küçük (yoksa genel en büyük) küme — ozel her koşulda kazanır.</summary>
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
