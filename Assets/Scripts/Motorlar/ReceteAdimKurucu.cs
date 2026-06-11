using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// FAZ36: Stateless reçete-adım kurucu. Legacy TumbleAdimKaydi formatına BİREBİR uyumlu adım üretir.
/// Hiçbir instance state'e dokunmaz (ANAYASA rule 2) — yalnız verilen dizilerle çalışır, tüm motorlar paylaşır.
///
/// Legacy düşme geometrisi (CokmeAkisServisi.YerindeTumbleRefillGridOlustur:46): YERİNDE refill, yerçekimi YOK.
/// → Patlayan hücreler -1 olur, AYNI konumda random sembolle dolar (YeniSpawn = iniş animasyonu).
/// → DusenHucreFrom/To legacy'de DAİMA boş → burada da boş bırakılır.
/// </summary>
public static class ReceteAdimKurucu
{
    private const int CARPAN_SEMBOL = -2;           // OyunYoneticisi.Fields.cs:577
    private const int CLUSTER_ESIK = 8;             // OyunKorumaServisi.TUMBLE_SABIT_ESIK

    /// <summary>
    /// Tek-küme tumble adımı kur. patlayanHucreler -1 yapılır, KAZANÇSIZ refill ile yerinde doldurulur
    /// (kazanSembol HARİÇ + hiçbir sembol CLUSTER_ESIK'e ulaşmaz → yeni cluster oluşmaz). CARPAN_SEMBOL KORUNUR.
    /// </summary>
    public static TumbleAdimKaydi TekKumeAdim(
        int[,] ilkGrid, int[,] ilkCarpanGrid,
        List<Vector2Int> patlayanHucreler, int turKazanci,
        int sutun, int satir, int scatterIdx, int sembolSayisi, int kazanSembol)
    {
        var adim = new TumbleAdimKaydi { TurKazanci = turKazanci };
        if (patlayanHucreler != null) adim.PatlayanHucreler.AddRange(patlayanHucreler);

        int[,] yeniGrid = (int[,])ilkGrid.Clone();
        int[,] yeniCarpan = ilkCarpanGrid != null ? (int[,])ilkCarpanGrid.Clone() : new int[sutun, satir];

        // 1. Patlayan hücreleri boşalt (-1).
        if (patlayanHucreler != null)
            foreach (var p in patlayanHucreler)
            {
                if (p.x < 0 || p.x >= sutun || p.y < 0 || p.y >= satir) continue;
                yeniGrid[p.x, p.y] = -1;
                yeniCarpan[p.x, p.y] = 0;
            }

        // 2. Mevcut (kalan) sembol sayımı — kazançsız refill kısıtı için.
        var adet = new int[sembolSayisi];
        for (int x = 0; x < sutun; x++)
            for (int y = 0; y < satir; y++)
            {
                int v = yeniGrid[x, y];
                if (v >= 0 && v < sembolSayisi) adet[v]++;
            }

        // 3. Doldurma adayları: kazanSembol HARİÇ (yeniden cluster olmasın) + scatter HARİÇ.
        var dolguAdaylari = new List<int>();
        for (int s = 0; s < sembolSayisi; s++)
        {
            if (s == scatterIdx || s == kazanSembol) continue;
            dolguAdaylari.Add(s);
        }

        // 4. Patlayan hücreleri yerinde RASTGELE doldur (legacy random refill hissi) — round-robin DEĞİL (görsel iz bırakırdı).
        //    Cluster eşiğini aşmayacak adaylar arasından rastgele seç → kazançsız garanti + legacy ile aynı dağınık görünüm.
        var yeniSpawn = new List<Vector2Int>();
        if (patlayanHucreler != null)
            foreach (var p in patlayanHucreler)
            {
                if (p.x < 0 || p.x >= sutun || p.y < 0 || p.y >= satir) continue;
                int secilen = RastgeleDolguSembol(dolguAdaylari, adet);
                yeniGrid[p.x, p.y] = secilen;
                if (secilen >= 0 && secilen < sembolSayisi) adet[secilen]++;
                yeniSpawn.Add(new Vector2Int(p.x, p.y));
            }

        adim.GridRefillSonrasi = yeniGrid;
        adim.CarpanGridRefillSonrasi = yeniCarpan;
        adim.YeniSpawnEdilenHucreler.AddRange(yeniSpawn);
        // DusenHucreFrom/To: legacy yerçekimi yapmaz → boş (default empty list).
        // CarpanDegerleriBuTur: bu adımda yeni çarpan yerleşmez (cascade kapalı) → boş.
        return adim;
    }

    /// <summary>Cluster eşiğini (8) aşmayacak adaylar arasından RASTGELE sembol seç (legacy random hissi + kazançsız garanti).</summary>
    private static int RastgeleDolguSembol(List<int> dolguAdaylari, int[] adet)
    {
        if (dolguAdaylari == null || dolguAdaylari.Count == 0) return 0;
        var uygun = new List<int>(dolguAdaylari.Count);
        for (int i = 0; i < dolguAdaylari.Count; i++)
        {
            int s = dolguAdaylari[i];
            if (s >= 0 && s < adet.Length && adet[s] < CLUSTER_ESIK - 1) uygun.Add(s);
        }
        var havuz = uygun.Count > 0 ? uygun : dolguAdaylari;   // hepsi doluysa (imkansıza yakın) eşik gevşet
        return havuz[Random.Range(0, havuz.Count)];
    }

    /// <summary>Gridde verilen sembole ait TÜM hücre konumları (pay-anywhere cluster = aynı sembolün hepsi).</summary>
    public static List<Vector2Int> SembolHucreleri(int[,] grid, int sembol, int sutun, int satir)
    {
        var list = new List<Vector2Int>();
        for (int x = 0; x < sutun; x++)
            for (int y = 0; y < satir; y++)
                if (grid[x, y] == sembol) list.Add(new Vector2Int(x, y));
        return list;
    }

    /// <summary>
    /// FAZ36 İŞ B: KAZANÇSIZ random grid (kayıp spini). Hiçbir sembol CLUSTER_ESIK'e (8) ulaşmaz → pay-anywhere kazanç YOK.
    /// Scatter HARİÇ (zero-scatter → bonus tetiklenmez). Reçete bunu IlkGrid yapar, Adimlar=[] boş, ham=0 → legacy kayıp görseli.
    /// Her sembol sayımı &lt; CLUSTER_ESIK-1 kalan adaylardan rastgele seçilir → 30 hücre &lt; 8sembol×7=56 kapasite ile garanti.
    /// </summary>
    public static int[,] KazancsizGridKur(int sutun, int satir, int sembolSayisi, int scatterIdx)
    {
        var grid = new int[sutun, satir];
        var adaylar = new List<int>();
        for (int s = 0; s < sembolSayisi; s++) if (s != scatterIdx) adaylar.Add(s);
        if (adaylar.Count == 0) return grid;

        var adet = new int[sembolSayisi];
        for (int x = 0; x < sutun; x++)
            for (int y = 0; y < satir; y++)
            {
                var uygun = new List<int>(adaylar.Count);
                for (int i = 0; i < adaylar.Count; i++)
                {
                    int s = adaylar[i];
                    if (s >= 0 && s < adet.Length && adet[s] < CLUSTER_ESIK - 1) uygun.Add(s);
                }
                var havuz = uygun.Count > 0 ? uygun : adaylar;   // 30<56 kapasite → uygun asla boşalmaz; defansif fallback
                int secilen = havuz[Random.Range(0, havuz.Count)];
                grid[x, y] = secilen;
                if (secilen >= 0 && secilen < adet.Length) adet[secilen]++;
            }
        return grid;
    }
}
