using System.Collections.Generic;
using UnityEngine;

namespace Senaryo.Scripted
{
    /// <summary>
    /// <see cref="ScriptedSpinKaydi"/> (asset formatı, 1D grid) → <see cref="SpinSimulasyonKaydi"/>
    /// (oyun motoru formatı, 2D grid) dönüştürücüsü. <see cref="OyunYoneticisi"/> SimuleEtVeKaydetImpl
    /// scripted modda RNG döngüsünü atlar ve bu sınıfın çıktısını döndürür.
    ///
    /// Dönüşüm kuralları:
    /// - 1D → 2D grid: index i = y * sutun + x, x = i % sutun, y = i / sutun (sutun=6, satir=5).
    /// - Çarpanlar SUM ile toplanır (Sweet Bonanza mantığı; CarpanServisi içinde _spinCarpanCarpim ismi yanıltıcı).
    /// - <c>ToplamHamKazanc = brutOdeme / max(1, carpanToplam)</c>; NormalSpinAkisi
    ///   <c>teorikToplam = ham × carpan</c> formülünü uyguladığında bakiyeye eklenen
    ///   tutar planın brüt değerine birebir uyar.
    /// - <see cref="ScriptedSpinKaydi.carpanKactiFlag"/> true ise ham 0, çarpan 1 → ödeme 0
    ///   (çarpan grid'de görünür, cluster yok).
    /// - <see cref="SpinTipi.BonusTetik"/> bu aşamada sıfır spin gibi davranır; bonus akışı AŞAMA 5'te eklenecek.
    /// </summary>
    public static class ScriptedSpinUygulayici
    {
        private const int SUTUN = 6;
        private const int SATIR = 5;
        private const int CARPAN_SEMBOL = -2;

        /// <summary>
        /// Asset kaydını motorun beklediği <see cref="SpinSimulasyonKaydi"/> nesnesine çevirir.
        /// </summary>
        /// <param name="kayit">Asset'ten gelen scripted spin tanımı.</param>
        /// <param name="mgr">Çağıran <see cref="OyunYoneticisi"/> (paytable hesabı için tumbleAyarlari'na erişir).</param>
        /// <param name="bahis">Bu spin için fiili bahis (EkonomiServisi.Bahis). Adım kazancı paytable hesabında kullanılır.</param>
        public static SpinSimulasyonKaydi UygulaKaydi(ScriptedSpinKaydi kayit, OyunYoneticisi mgr, int bahis)
        {
            if (kayit == null) return null;

            var sim = new SpinSimulasyonKaydi
            {
                Sutun = SUTUN,
                Satir = SATIR,
                SenaryoOdemeBandinaUygun = true
            };

            // 1) İlk grid + ilk çarpan grid (1D → 2D)
            sim.IlkGrid = DonustureGrid(kayit.ilkGridSemboller);
            sim.IlkCarpanGrid = DonustureGrid(kayit.ilkCarpanDegerleri);

            // 2) İlk gridde yerleşen çarpan değerleri (List<int>, RecordPlacedCarpanlar için)
            sim.IlkCarpanDegerleri = new List<int>();
            int ilkGridCarpanToplam = 0;
            if (kayit.ilkCarpanDegerleri != null)
            {
                for (int i = 0; i < kayit.ilkCarpanDegerleri.Length; i++)
                {
                    int v = kayit.ilkCarpanDegerleri[i];
                    if (v > 0)
                    {
                        sim.IlkCarpanDegerleri.Add(v);
                        ilkGridCarpanToplam += v;
                    }
                }
            }

            // 3) Tumble adımları (AŞAMA 3.10 yeni model):
            //    Önceki grid'e patlayan hücrelerin yukaridanDusenSemboller'ını yerleştir → GridRefillSonrasi.
            //    Patlamayan dolgu hücreleri spin boyunca DEĞİŞMEZ (motor yer çekimi yapmıyor: CokmeAkisServisi
            //    yerinde refill). InjekteEdilenHucreler'a gerek yok — patlamayan hücrelerin sprite'ı ilk gridten
            //    geliyor, kayıtla zaten tutarlı.
            sim.Adimlar = new List<TumbleAdimKaydi>();
            int adimCarpanToplam = 0;
            int[,] simdikiGrid = sim.IlkGrid;
            int[,] simdikiCarpanGrid = sim.IlkCarpanGrid;

            if (kayit.tumbleler != null)
            {
                for (int ti = 0; ti < kayit.tumbleler.Count; ti++)
                {
                    var tdef = kayit.tumbleler[ti];
                    if (tdef == null) continue;

                    var patlayanlar = tdef.patlayanHucreler != null
                        ? new List<Vector2Int>(tdef.patlayanHucreler)
                        : new List<Vector2Int>();
                    var dusenSym = tdef.yukaridanDusenSemboller ?? new int[0];
                    var dusenCarp = tdef.yukaridanDusenCarpanlar ?? new int[0];

                    // GridRefillSonrasi: önceki grid'i klonla, patlayan hücrelere düşen sembol/çarpanı koy.
                    int[,] sonGrid = (int[,])simdikiGrid.Clone();
                    int[,] sonCarpGrid = (int[,])simdikiCarpanGrid.Clone();
                    int n = Mathf.Min(patlayanlar.Count, dusenSym.Length);
                    var carpanDegerleriBuTur = new List<int>();
                    for (int i = 0; i < n; i++)
                    {
                        var p = patlayanlar[i];
                        if (p.x < 0 || p.x >= SUTUN || p.y < 0 || p.y >= SATIR) continue;
                        int carpan = (i < dusenCarp.Length) ? dusenCarp[i] : 0;
                        if (carpan > 0)
                        {
                            sonGrid[p.x, p.y] = CARPAN_SEMBOL;
                            sonCarpGrid[p.x, p.y] = carpan;
                            carpanDegerleriBuTur.Add(carpan);
                            adimCarpanToplam += carpan;
                        }
                        else
                        {
                            sonGrid[p.x, p.y] = dusenSym[i];
                            sonCarpGrid[p.x, p.y] = 0;
                        }
                    }

                    var adim = new TumbleAdimKaydi
                    {
                        PatlayanHucreler = patlayanlar,
                        GridRefillSonrasi = sonGrid,
                        CarpanGridRefillSonrasi = sonCarpGrid,
                        CarpanDegerleriBuTur = carpanDegerleriBuTur,
                        // Patlayan hücreler yer çekimi/üstten spawn animasyonuyla yenilenir (görsel "yukarıdan düştü").
                        YeniSpawnEdilenHucreler = new List<Vector2Int>(patlayanlar),
                        DusenHucreFrom = new List<Vector2Int>(),
                        DusenHucreTo = new List<Vector2Int>(),
                        // InjekteEdilenHucreler artık BOŞ — patlamayan dolgu hücreleri ilk gridten beri sabit, render güncel.
                        InjekteEdilenHucreler = new List<Vector2Int>()
                    };

                    // Adım kazancı: paytable üzerinden geçici hesap (aşağıda plan brüt'üne göre rescale edilir)
                    int turKazanc = 0;
                    if (mgr != null && mgr.tumbleAyarlari != null && adim.PatlayanHucreler.Count > 0)
                    {
                        turKazanc = mgr.tumbleAyarlari.CalculateWinWithOwnPayTable(
                            adim.PatlayanHucreler, simdikiGrid, SATIR, SUTUN, bahis);
                    }
                    adim.TurKazanci = turKazanc;

                    sim.Adimlar.Add(adim);
                    simdikiGrid = sonGrid;
                    simdikiCarpanGrid = sonCarpGrid;
                }
            }

            // 4) Çarpan toplamı (SUM, Sweet Bonanza). 0 ise efektif 1.
            int carpanToplam = ilkGridCarpanToplam + adimCarpanToplam;
            sim.NihaiCarpanToplam = carpanToplam > 0 ? carpanToplam : 1;

            // 5) AŞAMA 3.11: pro-rata scale KALDIRILDI. Yeni model'de asset her tumble'a doğru sembolü
            //    yerleştiriyor (yukaridanDusenSemboller); paytable hesabı plan brüt'üne birebir uyuyor.
            //    ToplamHamKazanc = sum(adım TurKazanci paytable'dan) — ek dönüşüm yok.
            int paytableToplam = 0;
            for (int i = 0; i < sim.Adimlar.Count; i++) paytableToplam += sim.Adimlar[i].TurKazanci;
            sim.ToplamHamKazanc = paytableToplam;

            // 6) Çarpan kaçtı (A5 Spin 3): grid'de çarpan görünür, cluster yok → ödeme 0.
            // CarpanKacti bayrağı SpinSimulasyonKaydi'a aktarılır → SimulasyonKaydiniOynatImpl ilk grid çarpan
            // toplamasını atlar (kazanç kutusuna uçuş tetiklenmez, "tüh" hissi korunur).
            sim.CarpanKacti = kayit.carpanKactiFlag;
            if (kayit.carpanKactiFlag)
            {
                sim.ToplamHamKazanc = 0;
                sim.NihaiCarpanToplam = 1;
                for (int i = 0; i < sim.Adimlar.Count; i++) sim.Adimlar[i].TurKazanci = 0;
            }

            // 7) BonusTetik (A5 Spin 4): bonus akışı AŞAMA 5'te eklenecek; şu an sıfır spin gibi davran.
            if (kayit.tip == SpinTipi.BonusTetik)
            {
                sim.ToplamHamKazanc = 0;
                sim.NihaiCarpanToplam = 1;
                for (int i = 0; i < sim.Adimlar.Count; i++) sim.Adimlar[i].TurKazanci = 0;
                Debug.Log("[ScriptedUygulayici] BonusTetik tipi (A5 Spin 4) — AŞAMA 5'te bonus akışı bağlanacak; şimdilik sıfır spin.");
            }

            // 8) Force çarpan (A4 x100, A5 x500): havuz/limit clamp'ini bypass et.
            //    Threshold 50: doğal havuz {2,3,5,8,10} max 10×N (max 50 toplam force path haricinde) → 50+ kesin force.
            if (carpanToplam >= 50)
            {
                sim.ZorlaCarpanKullanildi = true;
            }

            long teorikOdeme = (long)sim.ToplamHamKazanc * sim.NihaiCarpanToplam;
            Debug.Log(
                $"[ScriptedUygulayici] Aşama {kayit.asamaIndex + 1} Spin {kayit.spinSiraNo} | " +
                $"Adım={sim.Adimlar.Count} | Ham={sim.ToplamHamKazanc} × Çarpan={sim.NihaiCarpanToplam} = " +
                $"Ödeme {teorikOdeme} TL (plan brüt {kayit.brutOdeme}) | " +
                $"CarpanKacti={kayit.carpanKactiFlag} | ZorlaCarpan={sim.ZorlaCarpanKullanildi}");

            // Her tumble için kısa özet: kaç hücre patladı, hangi semboller düşüyor.
            for (int ti = 0; ti < sim.Adimlar.Count; ti++)
            {
                var adim = sim.Adimlar[ti];
                int patlayan = adim.PatlayanHucreler != null ? adim.PatlayanHucreler.Count : 0;
                Debug.Log($"[ScriptedUygulayici][TUMBLE] Adım {ti + 1}: {patlayan} hücre patladı, TurKazanc={adim.TurKazanci}");
            }

            // DEBUG: IlkGrid'in sembol dağılımını yazdır — kullanıcı ekranda gördüğü cluster ile karşılaştırabilsin.
            // Ekrandaki RNG'den farklı görünüyorsa, log'da scripted dağılım net görülür.
            var sayaclar = new Dictionary<int, int>();
            for (int x = 0; x < SUTUN; x++)
            {
                for (int y = 0; y < SATIR; y++)
                {
                    int s = sim.IlkGrid[x, y];
                    if (!sayaclar.ContainsKey(s)) sayaclar[s] = 0;
                    sayaclar[s]++;
                }
            }
            var sb = new System.Text.StringBuilder("[ScriptedUygulayici][DEBUG] IlkGrid dağılım: ");
            foreach (var kv in sayaclar)
            {
                string ad = SembolAdi(kv.Key);
                sb.Append($"{ad}({kv.Key})={kv.Value} ");
            }
            Debug.Log(sb.ToString());

            // Hücre-by-hücre verbose log: kullanıcı ekran görüntüsü ile karşılaştırabilsin.
            // Konvansiyon: y=0 ekranın ÜST sırası (Unity GridLayoutGroup default; asset üretici ile motor aynı).
            var verbose = new System.Text.StringBuilder("[ScriptedUygulayici][VERBOSE] IlkGrid satırları (y=0 ÜST sıra):\n");
            for (int yy = 0; yy < SATIR; yy++)
            {
                verbose.Append($"  y={yy}: ");
                for (int xx = 0; xx < SUTUN; xx++)
                {
                    int s = sim.IlkGrid[xx, yy];
                    verbose.Append($"({xx},{yy})={SembolAdi(s)}({s})  ");
                }
                verbose.Append('\n');
            }
            Debug.Log(verbose.ToString());

            return sim;
        }

        /// <summary>Sembol indeksinin insan-okur adı (rapora göre 0=Armut..7=Üzüm, 8=Yıldız/Scatter, -2=Çarpan, -1=Boş).</summary>
        private static string SembolAdi(int sym)
        {
            switch (sym)
            {
                case -2: return "Carpan";
                case -1: return "Bos";
                case 0: return "Armut";
                case 1: return "Cilek";
                case 2: return "Erik";
                case 3: return "Hindistan";
                case 4: return "Karpuz";
                case 5: return "Muz";
                case 6: return "Elma";
                case 7: return "Uzum";
                case 8: return "Yildiz";
                default: return "Sym" + sym;
            }
        }

        /// <summary>
        /// 1D 30-element array → 2D [SUTUN, SATIR]. Asset üretici ve motor render aynı konvansiyon kullanır:
        /// y=0 = ekranın ÜST sırası (Unity GridLayoutGroup default top-left start). RenderAllSprites idx sırası
        /// y dış / x iç, hücre array'i sahnede üst-soldan başlar. Bu yüzden flip GEREKMEZ — düz dönüşüm.
        ///
        /// (Tarihçe notu: AŞAMA 3.8'de yanlış varsayımla SATIR-1-yAsset flip uygulanmıştı; ekran asset'in
        /// tam tersini gösterdiği için 3.9'da geri alındı.)
        /// </summary>
        private static int[,] DonustureGrid(int[] flat)
        {
            var grid = new int[SUTUN, SATIR];
            if (flat == null) return grid;
            int n = Mathf.Min(flat.Length, SUTUN * SATIR);
            for (int i = 0; i < n; i++)
            {
                int x = i % SUTUN;
                int y = i / SUTUN;
                grid[x, y] = flat[i];
            }
            return grid;
        }
    }
}
