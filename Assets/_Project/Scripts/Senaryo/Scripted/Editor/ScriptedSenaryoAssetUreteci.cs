#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Senaryo.Scripted.Editor
{
    /// <summary>
    /// SCRIPTED_SISTEM_PLAN.md Bölüm 3 tablolarını ScriptableObject asset'ine yazan editor utility.
    /// Menü: Tools → Kumar → Scripted Senaryo Asset'ini Yeniden Üret.
    /// Asset yolu: Assets/_Project/Resources/ScriptedSenaryo.asset (Resources.Load ile yüklenir).
    ///
    /// === MİMARİ NOTU (AŞAMA 3.10) ===
    /// Eski model: her tumble için 30 hücreli "GridRefillSonrasi" tutuluyordu, dolgu sembolleri her
    /// tumble'da yeniden cycling ile üretiliyordu → görsel olarak dolguların değişmesi yanılgısı.
    ///
    /// Yeni model: ilk grid SABİT, her tumble sadece "patlayanHucreler + yukaridanDusenSemboller"
    /// (paralel array) tutar. Motor zaten yer çekimi yapmıyor (CokmeAkisServisi.YerindeTumbleRefillGridOlustur:
    /// patlayan hücreye yerinde yeni sembol). Yani T1 sonrası grid = ilk grid + (patlayan hücrelere düşen
    /// yeni sembol). Patlamayan dolgu hücreleri spin boyunca AYNI kalır.
    ///
    /// Kritik kural: dolgu hücrelerinde TÜM tumble'larda patlayacak sembollerden olmamalı, yoksa
    /// cluster patladıktan sonra dolgudaki aynı semboller toplam 8'i aşar (örn 8 elma cluster +
    /// 3 dolgu elma = 11 elma → yanlış paytable). GridIlk helper allClusters parametresiyle bu
    /// sembolleri dolgu pool'dan hariç tutar.
    /// </summary>
    public static class ScriptedSenaryoAssetUreteci
    {
        // === Sembol indeksleri (Bölüm 2.2 raporu) ===
        private const int SYM_ARMUT = 0;
        private const int SYM_CILEK = 1;
        private const int SYM_ERIK = 2;
        private const int SYM_HINDISTAN = 3;
        private const int SYM_KARPUZ = 4;
        private const int SYM_MUZ = 5;
        private const int SYM_ELMA = 6;
        private const int SYM_UZUM = 7;
        private const int SYM_SCATTER = 8; // sahnede ScatterIndex=8 (yıldız)
        private const int CARPAN_SEMBOL = -2;

        private const int SUTUN = 6;
        private const int SATIR = 5;
        private const int HUCRE_SAYISI = SUTUN * SATIR; // 30

        private const string ASSET_KLASOR = "Assets/_Project/Resources";
        private const string ASSET_YOL = ASSET_KLASOR + "/ScriptedSenaryo.asset";

        // === Modal mesajları (3. tekil dış-gözlemci dili — pedagojik distance) ===
        // A1 Spin 1: ilk kazanç sonrası — saatlerce oynamanın hatırası
        // (manipülasyon farkındalığı M_A2_S2'ye taşındı çünkü A1 S1'de gerçek net kazanç var,
        // manipülasyon görünmez. M_A2_S2 = bahisten az ödeme alan kazanç → manipülasyon net.)
        private const string M_A1_S1 =
            "İlk kazanç oyuncu için en tehlikeli başlangıçtır. Oyuncunun beyni bu anı unutmayacak: saatlerce oyun başında kalmasının sebebi bu kısa anın hatırasıdır.";
        // A1 Spin 4: dopamin yakıtı (3. tekil)
        private const string M_A1_S4 = "Oyuncu ilk kazançları yaşıyor. Oyuncunun beyninde dopamin salgılanıyor. Bu his, saatlerce oyun oynamasının yakıtı olacak.";
        // A1 Spin 7 ve Spin 8 SONRA modal'ları KALDIRILDI (sade akış, ÖNCE modal SpinButonImpl hook'unda).

        // A2 Spin 2: bahisten az ödeme alan kazanç — manipülasyon farkındalığı net görünür
        // (A2 bahis 1000, brüt 500 → ekran "KAZANÇ 500 TL" yazar, bakiyeden 500 düşer).
        private const string M_A2_S2 =
            "<b>⚠️ DİKKAT: manipülasyon farkındalığı</b>\n\n" +
            "Oyuncu az önce <b>1.000 TL</b> bahis koydu, ekrana <i>'KAZANÇ 500 TL'</i> yazdı, bakiyesinden <b>500 TL EKSİLDİ</b> ama oyuncunun zihninde <i>'kazandım'</i> hissi yaşanıyor.\n\n" +
            "Bu sistemin temel manipülasyonudur: her spinde bahisten az ödeme yaparken büyük yazıyla <i>'KAZANÇ'</i> yazılır. Oyuncuda <i>'kazanıyorum'</i> algısı yaratılır. Uzun vadede oyuncu daima kayıptadır. Algoritma bunu kasıtlı tasarlar: sürekli artıyormuş gibi göstererek oyuncuyu bağlamak için.";
        // A2 Spin 3: 3 yıldız near-miss — "Az Daha Tutuyordu" yanılsaması + bonus oyun değeri açıklaması
        private const string M_A2_S3 = "Az önce <b>3 yıldız (bonus sembolü)</b> düştü. Bir tane daha gelseydi, bahis miktarının 100 katı değere sahip 10 ücretsiz spin hakkı veren bir BONUS oyun açılacaktı.\n\nBu <b>'Az Daha Tutuyordu'</b> yanılsamasıdır: oyuncunun beyni bu kıl payı kaçırışı kazanmış gibi algılar. Oyuncu <i>'çok yaklaştım'</i> diye düşünüp daha fazla oynar.";
        // A2 Spin 4 SONRA modal — kontrol yanılsaması vurgusu (3. tekil)
        private const string M_A2_S4 = "Oyuncu oyunu yönettiğini düşünürken, oyun onu adım adım içine çekiyor.";
        // A2 Spin 6: kontrol yanılsaması pekişmesi (3. tekil)
        private const string M_A2_S6 = "Hem üzüm hem elma 1 sembol eksikti. İkisi birden kıl payı kaçtı. Oyuncu şu an <i>'çok yakındım, bir daha denesem'</i> hissi yaşıyor. Bu his manipülasyon: algoritma bunu kasıtlı yarattı. Kontrol yanılsaması böyle pekişiyor.";
        // A2 Spin 8 modal KALDIRILDI (sade akış).

        // A3 modal mesajları
        private const string M_A3_S3 = "İlk ciddi kayıplar yaşanıyor. Amaç para kazanmaktan çıktı, kayıpları telafi etmeye dönüştü.";
        // A3 Spin 6: kayıp kovalama + bahis 2500'e otomatik yükseltme uyarısı
        private const string M_A3_S6 = "Oyuncu kayıpları geri kazanmak için daha fazla risk alıyor, mantıklı düşünme yetisini kaybediyor.\n\n⚠️ Şimdi oyuncu bahsini 2.500 TL'ye yükseltecek; <i>'daha yüksek bahis daha hızlı kurtarır'</i> yanılgısıyla. Bu da algoritmanın istediği şey.";
        // A3 Spin 7
        private const string M_A3_S7 = "Bir tur daha = bir kayıp daha.";
        // A3 Spin 8 modali KALDIRILDI

        // A4 modal mesajları
        private const string M_A4_S2 = "Üst üste kayıplar oyuncuyu yıpratıyor. Algoritma birkaç spin sonra büyük bir vuruş hazırlıyor; ama önce pes etme eşiğine kadar getirecek.";
        private const string M_A4_S4 = "Oyuncu pes etmek üzere. Tam bu noktada büyük bir kazanç düşürülecek. Bu kasıtlı manipülasyondur: pes etmeyi engellemek için tasarlanan bir kurtarma.";
        // A4 Spin 5 modali asset'ten kaldırıldı — AnlaticiSeritKopru.A4S5CarpanModalAkisi runtime'da çağırır.

        // A5 modal mesajları
        private const string M_A5_S1 = "Bahis arttı, beklenti arttı. Oyuncuda adrenalin salgılanıyor.";
        // A5 Spin 3: ×500 çarpan kaçtı + sabırsızlık silahı
        private const string M_A5_S3 = "Ekrana ×500 çarpanı düştü ama eşleşme olmadı. Bu kasıtlı bir tasarım: oyuncunun beyni <i>'çok yaklaştım, bir daha denesem belki tutar'</i> diye düşünüyor. Bu hisle oyuncu bir sonraki bahsi atmak için sabırsızlanır. İşte tam bu sabırsızlık, algoritmanın kullandığı silahtır.";
        private const string M_A5_S4_BONUS = "🎰 ŞANSLI SAATİNDESİN! Bonus oyun aktif edildi. Bakiyenin tamamını yatır, x10000 kazanma şansını kaçırma. SINIRLI TEKLİF.";
        // A5 Spin 5 modali asset'ten kaldırıldı — ScriptedBonusOyunUygulayici dinamik yüzde ile runtime'da modal oynatır.

        // === Bahis sabitleri (Bölüm 2 + AnlaticiSeritKopru._onerilenBahisler) ===
        private const int BAHIS_A1 = 500;
        private const int BAHIS_A2 = 1000;
        private const int BAHIS_A3 = 1500;
        private const int BAHIS_A4 = 1000;
        private const int BAHIS_A5 = 2000;
        public const int BAHIS_A6 = 10000; // A6 hızlı yıkım: 5 spin × 10K = 50K borç tükenir.

        [MenuItem("Tools/Kumar/Scripted Senaryo Asset'ini Yeniden Üret")]
        public static void AssetiYenidenUret()
        {
            EnsureKlasor(ASSET_KLASOR);

            var asset = AssetDatabase.LoadAssetAtPath<ScriptedAsamaListesi>(ASSET_YOL);
            bool yeni = asset == null;
            if (yeni)
            {
                asset = ScriptableObject.CreateInstance<ScriptedAsamaListesi>();
                AssetDatabase.CreateAsset(asset, ASSET_YOL);
            }

            asset.asamaSpinleri.Clear();
            for (int i = 0; i < 7; i++)
                asset.asamaSpinleri.Add(new AsamaSpinListesi());

            DoldurAsama1(asset.asamaSpinleri[0].spinler);
            DoldurAsama2(asset.asamaSpinleri[1].spinler);
            DoldurAsama3(asset.asamaSpinleri[2].spinler);
            DoldurAsama4(asset.asamaSpinleri[3].spinler);
            DoldurAsama5(asset.asamaSpinleri[4].spinler);
            // A6 (idx 5) dinamik — boş; A7 (idx 6) cutscene — boş.

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            int toplamSpin = 0;
            for (int i = 0; i < asset.asamaSpinleri.Count; i++)
                toplamSpin += asset.asamaSpinleri[i].spinler.Count;
            Debug.Log($"[ScriptedSenaryoAssetUreteci] Asset {(yeni ? "oluşturuldu" : "güncellendi")}: {ASSET_YOL} | Toplam tanımlı spin: {toplamSpin}");
            EditorUtility.DisplayDialog(
                "Scripted Senaryo Asset",
                $"Asset {(yeni ? "oluşturuldu" : "güncellendi")}.\n\nYol: {ASSET_YOL}\nToplam tanımlı spin: {toplamSpin}\n\nYeni model: ilk grid sabit + her tumble için yukaridanDusenSemboller.\n(A6 dinamik, A7 cutscene — boş listeler)",
                "Tamam");
        }

        [MenuItem("Tools/Kumar/Scripted Senaryo Asset'ini Logla (A1 Spin 1)")]
        public static void AssetiLogla()
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptedAsamaListesi>(ASSET_YOL);
            if (asset == null)
            {
                Debug.LogError($"[AssetiLogla] Asset bulunamadı: {ASSET_YOL}. Önce 'Yeniden Üret' menüsünden üretin.");
                return;
            }
            if (asset.asamaSpinleri == null || asset.asamaSpinleri.Count == 0
                || asset.asamaSpinleri[0]?.spinler == null || asset.asamaSpinleri[0].spinler.Count == 0)
            {
                Debug.LogError("[AssetiLogla] A1 listesi boş.");
                return;
            }

            var s1 = asset.asamaSpinleri[0].spinler[0];
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[AssetiLogla] A1 Spin {s1.spinSiraNo} (asama={s1.asamaIndex}, bahis={s1.bahis}, brut={s1.brutOdeme}, tip={s1.tip})");
            sb.AppendLine($"  ilkGridSemboller len={s1.ilkGridSemboller?.Length ?? 0}, ilkCarpanDegerleri len={s1.ilkCarpanDegerleri?.Length ?? 0}");
            sb.AppendLine($"  Konvansiyon: y=0 ÜST sıra (Unity GridLayoutGroup default).");
            if (s1.ilkGridSemboller != null)
            {
                var sayim = new Dictionary<int, int>();
                for (int y = 0; y < SATIR; y++)
                {
                    var row = new System.Text.StringBuilder($"  y={y}: ");
                    for (int x = 0; x < SUTUN; x++)
                    {
                        int idx = y * SUTUN + x;
                        int sym = idx < s1.ilkGridSemboller.Length ? s1.ilkGridSemboller[idx] : -99;
                        row.Append($"({x},{y})={SembolAdi(sym)}({sym})  ");
                        if (!sayim.ContainsKey(sym)) sayim[sym] = 0;
                        sayim[sym]++;
                    }
                    sb.AppendLine(row.ToString());
                }
                sb.Append("  Sayım: ");
                foreach (var kv in sayim) sb.Append($"{SembolAdi(kv.Key)}({kv.Key})={kv.Value} ");
                sb.AppendLine();

                // Sembol bazlı pozisyon listesi — kullanıcı dağılımı doğrulayabilsin (üst sıra mı, dağınık mı).
                var symPos = new Dictionary<int, List<string>>();
                for (int idx = 0; idx < s1.ilkGridSemboller.Length; idx++)
                {
                    int sym = s1.ilkGridSemboller[idx];
                    int x = idx % SUTUN;
                    int y = idx / SUTUN;
                    if (!symPos.ContainsKey(sym)) symPos[sym] = new List<string>();
                    symPos[sym].Add($"({x},{y})");
                }
                sb.AppendLine("  Sembol pozisyonları:");
                foreach (var kv in symPos)
                    sb.AppendLine($"    {SembolAdi(kv.Key)}({kv.Key}): {string.Join(" ", kv.Value)}");
            }
            sb.AppendLine($"  Tumble adımı: {s1.tumbleler?.Count ?? 0}");
            if (s1.tumbleler != null)
            {
                for (int ti = 0; ti < s1.tumbleler.Count; ti++)
                {
                    var t = s1.tumbleler[ti];
                    sb.Append($"    T{ti + 1}: patlayan={t.patlayanHucreler?.Count ?? 0} hücre, düşen=[");
                    if (t.yukaridanDusenSemboller != null)
                    {
                        for (int i = 0; i < t.yukaridanDusenSemboller.Length; i++)
                        {
                            sb.Append(SembolAdi(t.yukaridanDusenSemboller[i]));
                            if (i < t.yukaridanDusenSemboller.Length - 1) sb.Append(',');
                        }
                    }
                    sb.AppendLine("]");
                }
            }
            Debug.Log(sb.ToString());
        }

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

        // ============================================================
        // AŞAMA 1 — Isındırma ve Umut (bahis 500, 8 spin)
        // ============================================================
        private static void DoldurAsama1(List<ScriptedSpinKaydi> liste)
        {
            // Spin 1: 3 tumble {hindistan→elma→üzüm} = 1500 | modal A1_S1 (ilk kazanç sonrası)
            {
                int[] all = { SYM_HINDISTAN, SYM_ELMA, SYM_UZUM };
                int[] ilk = GridIlk(all, Seed(0, 1), (SYM_HINDISTAN, 8));
                var t1 = TumbleTekDusen(ilk, new[] { SYM_HINDISTAN }, SYM_ELMA);
                int[] g1 = GridSonrasiHesapla(ilk, t1);
                var t2 = TumbleTekDusen(g1, new[] { SYM_ELMA }, SYM_UZUM);
                int[] g2 = GridSonrasiHesapla(g1, t2);
                var t3 = TumbleDolguDusen(g2, new[] { SYM_UZUM }, all);
                liste.Add(SpinTanimi(1, 0, BAHIS_A1, SpinTipi.Kazanc, 1500, ilk, null, new[] { t1, t2, t3 }, M_A1_S1));
            }
            // Spin 2: tek cluster üzüm = 750 (revize: eski sıfır → küçük kazanç)
            liste.Add(TekClusterSpin(2, 0, BAHIS_A1, 750, SYM_UZUM, SpinTipi.Kazanc));
            // Spin 3: 8 elma + 8 üzüm + ilk grid x2 çarpan, tek tumble = 2500
            {
                int[] all = { SYM_ELMA, SYM_UZUM };
                var (g, c) = GridIlkCarpanli(all, Seed(0, 3), 2, (SYM_ELMA, 8), (SYM_UZUM, 8));
                var t1 = TumbleDolguDusen(g, new[] { SYM_ELMA, SYM_UZUM }, all);
                liste.Add(SpinTanimi(3, 0, BAHIS_A1, SpinTipi.Kazanc, 2500, g, c, new[] { t1 }));
            }
            // Spin 4: 8 üzüm tek cluster = 750 | modal A1_S4
            liste.Add(TekClusterSpin(4, 0, BAHIS_A1, 750, SYM_UZUM, SpinTipi.Kazanc, M_A1_S4));
            // Spin 5: 2 tumble {üzüm→elma} = 1250
            {
                int[] all = { SYM_UZUM, SYM_ELMA };
                int[] ilk = GridIlk(all, Seed(0, 5), (SYM_UZUM, 8));
                var t1 = TumbleTekDusen(ilk, new[] { SYM_UZUM }, SYM_ELMA);
                int[] g1 = GridSonrasiHesapla(ilk, t1);
                var t2 = TumbleDolguDusen(g1, new[] { SYM_ELMA }, all);
                liste.Add(SpinTanimi(5, 0, BAHIS_A1, SpinTipi.Kazanc, 1250, ilk, null, new[] { t1, t2 }));
            }
            // Spin 6: 7 üzüm near-miss
            liste.Add(SpinTanimi(6, 0, BAHIS_A1, SpinTipi.NearMiss, 0,
                GridIlk(null, Seed(0, 6), (SYM_UZUM, 7)), null, NoTumble()));
            // Spin 7: 8 üzüm + 8 elma + x5 çarpan (MEGA) = 6250 — SONRA modal YOK (ÖNCE modal SpinButonImpl hook'unda)
            {
                int[] all = { SYM_UZUM, SYM_ELMA };
                var (g, c) = GridIlkCarpanli(all, Seed(0, 7), 5, (SYM_UZUM, 8), (SYM_ELMA, 8));
                var t1 = TumbleDolguDusen(g, new[] { SYM_UZUM, SYM_ELMA }, all);
                liste.Add(SpinTanimi(7, 0, BAHIS_A1, SpinTipi.MegaWin, 6250, g, c, new[] { t1 }));
            }
            // Spin 8: 10 elma + 8 hindistan tek tumble = 1750 — modal kaldırıldı (sade akış)
            {
                int[] all = { SYM_ELMA, SYM_HINDISTAN };
                int[] ilk = GridIlk(all, Seed(0, 8), (SYM_ELMA, 10), (SYM_HINDISTAN, 8));
                var t1 = TumbleDolguDusen(ilk, new[] { SYM_ELMA, SYM_HINDISTAN }, all);
                liste.Add(SpinTanimi(8, 0, BAHIS_A1, SpinTipi.Kazanc, 1750, ilk, null, new[] { t1 }));
            }
        }

        // ============================================================
        // AŞAMA 2 — Kontrol Bende Hissi (bahis 1000, 8 spin)
        // ============================================================
        private static void DoldurAsama2(List<ScriptedSpinKaydi> liste)
        {
            liste.Add(TekClusterSpin(1, 1, BAHIS_A2, 1000, SYM_ELMA, SpinTipi.Kazanc));
            // Spin 2: tek cluster hindistan, brüt 500 (bahis 1000) — manipülasyon farkındalığı modali
            // (1000 bahis - 500 brüt = 500 net kayıp ama ekran "KAZANÇ 500 TL" yazar; sömürü görünür).
            liste.Add(TekClusterSpin(2, 1, BAHIS_A2, 500, SYM_HINDISTAN, SpinTipi.Kazanc, M_A2_S2));
            // Spin 3: normal sıfır brüt kayıp spini (3-yıldız sahnesi A4 S1'e taşındı —
            // A4 girişinde "neredeyse oluyordu" hissi pedagojik olarak daha güçlü).
            liste.Add(SpinTanimi(3, 1, BAHIS_A2, SpinTipi.Sifir, 0,
                GridSifir(Seed(1, 3)), null, NoTumble()));
            // Spin 4: tek cluster üzüm = 1500 (kasıtlı kazanç) | SONRA modal A2_S4 (kontrol yanılsaması pekişmesi)
            liste.Add(TekClusterSpin(4, 1, BAHIS_A2, 1500, SYM_UZUM, SpinTipi.Kazanc, M_A2_S4));
            // Spin 5: 2 tumble hindistan→muz = 750 (revize: eski sıfır → tatlı minik kazanç)
            {
                int[] all = { SYM_HINDISTAN, SYM_MUZ };
                int[] ilk = GridIlk(all, Seed(1, 5), (SYM_HINDISTAN, 8));
                var t1 = TumbleTekDusen(ilk, new[] { SYM_HINDISTAN }, SYM_MUZ);
                int[] g1 = GridSonrasiHesapla(ilk, t1);
                var t2 = TumbleDolguDusen(g1, new[] { SYM_MUZ }, all);
                liste.Add(SpinTanimi(5, 1, BAHIS_A2, SpinTipi.Kazanc, 750, ilk, null, new[] { t1, t2 }));
            }
            // Spin 6: 7 üzüm + 7 elma near-miss (revize: görsel takas, modal "kıl payı kaçtı" pekişme)
            liste.Add(SpinTanimi(6, 1, BAHIS_A2, SpinTipi.NearMiss, 0,
                GridIlk(null, Seed(1, 6), (SYM_UZUM, 7), (SYM_ELMA, 7)), null, NoTumble(), M_A2_S6));
            // Spin 7: 2 tumble (elma → hindistan); paytable hesabı 1500 (plana sadık değil — A2 Spin 7 plan brüt 750 typo)
            {
                int[] all = { SYM_ELMA, SYM_HINDISTAN };
                int[] ilk = GridIlk(all, Seed(1, 7), (SYM_ELMA, 8));
                var t1 = TumbleTekDusen(ilk, new[] { SYM_ELMA }, SYM_HINDISTAN);
                int[] g1 = GridSonrasiHesapla(ilk, t1);
                var t2 = TumbleDolguDusen(g1, new[] { SYM_HINDISTAN }, all);
                liste.Add(SpinTanimi(7, 1, BAHIS_A2, SpinTipi.Kazanc, 750, ilk, null, new[] { t1, t2 }));
            }
            // Spin 8: sıfır cluster, modal kaldırıldı (sade akış)
            liste.Add(SpinTanimi(8, 1, BAHIS_A2, SpinTipi.Sifir, 0, GridSifir(Seed(1, 8)), null, NoTumble()));
        }

        // ============================================================
        // AŞAMA 3 — Geri Kazanabilirim (bahis 1500, 8 spin)
        // ============================================================
        private static void DoldurAsama3(List<ScriptedSpinKaydi> liste)
        {
            liste.Add(SpinTanimi(1, 2, BAHIS_A3, SpinTipi.Sifir, 0, GridSifir(Seed(2, 1)), null, NoTumble()));
            liste.Add(SpinTanimi(2, 2, BAHIS_A3, SpinTipi.NearMiss, 0,
                GridIlk(null, Seed(2, 2), (SYM_UZUM, 7)), null, NoTumble()));
            liste.Add(SpinTanimi(3, 2, BAHIS_A3, SpinTipi.NearMiss, 0,
                GridIlk(null, Seed(2, 3), (SYM_ELMA, 7)), null, NoTumble(), M_A3_S3));
            liste.Add(SpinTanimi(4, 2, BAHIS_A3, SpinTipi.NearMiss, 0,
                GridIlk(null, Seed(2, 4), (SYM_UZUM, 7), (SYM_ELMA, 7)), null, NoTumble()));
            liste.Add(TekClusterSpin(5, 2, BAHIS_A3, 750, SYM_HINDISTAN, SpinTipi.BahisIadesi));
            liste.Add(SpinTanimi(6, 2, BAHIS_A3, SpinTipi.NearMiss, 0,
                GridIlk(null, Seed(2, 6), (SYM_SCATTER, 3)), null, NoTumble(), M_A3_S6));
            // Spin 7: bahis A3 Spin 6 sonu otomatik 2500'e yükseltildi → bu spin 2500 TL ile oynanır
            // (AnlaticiSeritKopru runtime'da AnlaticiSetBahis(2500) çağırıyor; asset bahis sabiti A3=1500 kayıt
            // amaçlı kalır, motor anlatici bahisini kullanır).
            liste.Add(SpinTanimi(7, 2, BAHIS_A3, SpinTipi.Sifir, 0, GridSifir(Seed(2, 7)), null, NoTumble(), M_A3_S7));
            // Spin 8: NearMiss, modal kaldırıldı (sade akış)
            liste.Add(SpinTanimi(8, 2, BAHIS_A3, SpinTipi.NearMiss, 0,
                GridIlk(null, Seed(2, 8), (SYM_UZUM, 7), (SYM_ELMA, 7), (SYM_SCATTER, 3)), null, NoTumble()));
        }

        // ============================================================
        // AŞAMA 4 — Şansım Döndü (bahis 1000, 5 spin)
        // ============================================================
        private static void DoldurAsama4(List<ScriptedSpinKaydi> liste)
        {
            // Spin 1: 3 yıldız (scatter) NearMiss — sabit konum (üst yarı), AnlaticiSeritKopru.A4S1YildizModalAkisi
            // runtime'da yıldızları döndürürken pedagojik modal'ı tetikler. modalMesaji null:
            // DonusAkisServisi otomatik modal hook'u sessiz atlar (çift modal olmasın).
            liste.Add(SpinTanimi(1, 3, BAHIS_A4, SpinTipi.NearMiss, 0,
                GridSabitScatterUstYari(Seed(3, 1)), null, NoTumble()));
            liste.Add(SpinTanimi(2, 3, BAHIS_A4, SpinTipi.NearMiss, 0,
                GridIlk(null, Seed(3, 2), (SYM_UZUM, 7)), null, NoTumble(), M_A4_S2));
            liste.Add(SpinTanimi(3, 3, BAHIS_A4, SpinTipi.NearMiss, 0,
                GridIlk(null, Seed(3, 3), (SYM_UZUM, 7), (SYM_ELMA, 7)), null, NoTumble()));
            liste.Add(SpinTanimi(4, 3, BAHIS_A4, SpinTipi.Sifir, 0, GridSifir(Seed(3, 4)), null, NoTumble(), M_A4_S4));
            // Spin 5: 8 ARMUT + ilk grid x100 çarpan (MEGA WIN) = 20000 — modal asset'ten kaldırıldı,
            // AnlaticiSeritKopru.A4S5CarpanModalAkisi spin sonu 2 sn pause + dinamik modal oynatır.
            {
                int[] all = { SYM_ARMUT };
                var (g, c) = GridIlkCarpanli(all, Seed(3, 5), 100, (SYM_ARMUT, 8));
                var t1 = TumbleDolguDusen(g, new[] { SYM_ARMUT }, all);
                liste.Add(SpinTanimi(5, 3, BAHIS_A4, SpinTipi.MegaWin, 20000, g, c, new[] { t1 }));
            }
        }

        // ============================================================
        // AŞAMA 5 — Sonunu Düşünen (bahis 2000, 5 spin + bonus tuzağı)
        // ============================================================
        private static void DoldurAsama5(List<ScriptedSpinKaydi> liste)
        {
            liste.Add(SpinTanimi(1, 4, BAHIS_A5, SpinTipi.Sifir, 0, GridSifir(Seed(4, 1)), null, NoTumble(), M_A5_S1));
            // Spin 2: 8 üzüm + ilk grid x2 çarpan = 6000
            {
                int[] all = { SYM_UZUM };
                var (g, c) = GridIlkCarpanli(all, Seed(4, 2), 2, (SYM_UZUM, 8));
                var t1 = TumbleDolguDusen(g, new[] { SYM_UZUM }, all);
                liste.Add(SpinTanimi(2, 4, BAHIS_A5, SpinTipi.Kazanc, 6000, g, c, new[] { t1 }));
            }
            // Spin 3: x500 çarpan grid'e düşer ama cluster yok (carpanKactiFlag) | modal A5_S3
            {
                var (g, c) = GridIlkCarpanli(null, Seed(4, 3), 500);
                var spin = SpinTanimi(3, 4, BAHIS_A5, SpinTipi.Sifir, 0, g, c, NoTumble(), M_A5_S3);
                spin.carpanKactiFlag = true;
                liste.Add(spin);
            }
            // Spin 4: BonusTetik — tüm bakiye otomatik bonus oyuna yatırılır, cüzi getiri (0 TL) döner.
            // Modal mesajı (M_A5_S4_BONUS) KALDIRILDI — ScriptedBonusTuzagiPopup zaten "🎰 ŞANSLI ANINDASIN!"
            // başlığıyla cazip pop-up'ı açıyor; ayrı eğitmen modal redundant ve akışı yavaşlatıyordu.
            {
                var spin4 = SpinTanimi(4, 4, BAHIS_A5, SpinTipi.BonusTetik, 0, GridSifir(Seed(4, 4)), null, NoTumble());
                spin4.bonusOyunuTetikle = true;
                spin4.bonusGetirisi = 0; // Yatırılanın tamamı kaybolur — pedagojik vuruş.
                liste.Add(spin4);
            }
            // Spin 5: bonus oyun cüzi ödeme = 800 (Aşama 5 bonus uygulayıcı tüketir).
            // Asset modali kaldırıldı — A5_S5 dinamik modal ScriptedBonusOyunUygulayici.BonusOyunuOynat
            // sonu yatırım/kazanç yüzdesini hesaplayıp gerçek metni oynatır.
            liste.Add(SpinTanimi(5, 4, BAHIS_A5, SpinTipi.Kazanc, 800, GridSifir(Seed(4, 5)), null, NoTumble()));
        }

        // ============================================================
        // === HELPERS (yeni model) ===
        // ============================================================

        /// <summary>
        /// İlk grid: cluster'lar + (varsa) çarpan + dolgu — tümü <paramref name="seed"/>'den deterministic
        /// rastgele konumlara dağıtılır (System.Random). Aynı seed → aynı asset (Tools menüsü idempotent).
        /// <paramref name="allClusters"/> bu spin'in TÜM tumble'larında patlayacak sembolleri içerir;
        /// bunlar dolgu pool'dan hariç tutulur (sonraki tumble'larda dolgudaki aynı sembol toplam 8'i aşmasın).
        /// Cluster pays mantığı konum bağımsız (CalculateWinWithOwnPayTable sadece sembol sayısına bakar),
        /// dolayısıyla rastgele dağılım paytable hesabını etkilemez — sadece görsel çeşitlilik sağlar.
        /// </summary>
        private static (int[] g, int[] c) GridIlkCarpanli(int[] allClusters, int seed, int carpanDeger, params (int sym, int adet)[] kumeler)
        {
            var rng = new System.Random(seed);
            int[] g = new int[HUCRE_SAYISI];
            int[] c = new int[HUCRE_SAYISI];

            // Tüm hücre indekslerini başlangıçta "boş" listesine al; cluster ve çarpan bunlardan rastgele tüketir.
            var kalanIndexler = new List<int>(HUCRE_SAYISI);
            for (int i = 0; i < HUCRE_SAYISI; i++) kalanIndexler.Add(i);

            // Cluster sembollerini rastgele pozisyonlara yerleştir (her sembol için adet kadar hücre).
            foreach (var (sym, adet) in kumeler)
            {
                for (int i = 0; i < adet && kalanIndexler.Count > 0; i++)
                {
                    int pick = rng.Next(kalanIndexler.Count);
                    int hucreIdx = kalanIndexler[pick];
                    kalanIndexler.RemoveAt(pick);
                    g[hucreIdx] = sym;
                }
            }

            // Çarpan/bomba: dolgu hücrelerinden RASTGELE bir tanesine yerleştirilir (cluster bozulmaz).
            if (carpanDeger > 0 && kalanIndexler.Count > 0)
            {
                int pick = rng.Next(kalanIndexler.Count);
                int hucreIdx = kalanIndexler[pick];
                kalanIndexler.RemoveAt(pick);
                g[hucreIdx] = CARPAN_SEMBOL;
                c[hucreIdx] = carpanDeger;
            }

            // Dolgu pool: 0..7 sembol (SCATTER=8 dahil değil → scatter dolguda asla yer almaz).
            // Cluster ve allClusters sembolleri hariç tutulur.
            int[] dolguPool = { SYM_ARMUT, SYM_CILEK, SYM_ERIK, SYM_HINDISTAN, SYM_KARPUZ, SYM_MUZ, SYM_ELMA, SYM_UZUM };
            var hariç = new HashSet<int>();
            if (allClusters != null) foreach (var s in allClusters) hariç.Add(s);
            foreach (var k in kumeler) hariç.Add(k.sym);
            var dolgu = System.Array.FindAll(dolguPool, s => !hariç.Contains(s));
            if (dolgu.Length == 0) dolgu = new[] { SYM_ARMUT };

            // Kalan hücrelere dolgu cycling. Hücre seçim sırası kalanIndexler içindeki kalan sırayla.
            int di = 0;
            foreach (int idx in kalanIndexler)
            {
                g[idx] = dolgu[di++ % dolgu.Length];
            }
            return (g, c);
        }

        /// <summary>İlk grid çarpansız varyantı — <see cref="GridIlkCarpanli"/>'i carpanDeger=0 ile çağırır.</summary>
        private static int[] GridIlk(int[] allClusters, int seed, params (int sym, int adet)[] kumeler)
        {
            var (g, _) = GridIlkCarpanli(allClusters, seed, 0, kumeler);
            return g;
        }

        /// <summary>
        /// A2 Spin 3 NearMiss için özel grid: 3 SCATTER (yıldız) sembolü grid'in ÜST YARISINDA sabit
        /// konumlarda yerleşir. Sol-alt modal açılınca yıldızlar arkada kalmasın diye sağ ve üst kısma
        /// dağıtılmıştır. Geri kalan 27 hücre rastgele dolgu (6 meyve sembolü, hiçbir cluster 8'e ulaşmaz).
        /// Konumlar (idx = sat × SUTUN + sutun):
        ///   - (sat 0, sutun 1) = idx 1   — üst-sol
        ///   - (sat 0, sutun 4) = idx 4   — üst-sağ
        ///   - (sat 1, sutun 5) = idx 11  — orta-sağ üst
        /// </summary>
        private static int[] GridSabitScatterUstYari(int seed)
        {
            int[] g = new int[HUCRE_SAYISI];
            var rng = new System.Random(seed);
            int[] dolguPool = { SYM_ARMUT, SYM_CILEK, SYM_ERIK, SYM_KARPUZ, SYM_MUZ, SYM_HINDISTAN };
            // Tüm hücreleri rastgele dolguyla doldur (cluster 8'e ulaşmaması için 6 sembol döngüsü
            // — paytable konum bağımsız ama yine de yedek olarak dolguPool'dan eşit dağılım).
            for (int i = 0; i < HUCRE_SAYISI; i++)
                g[i] = dolguPool[rng.Next(dolguPool.Length)];
            // 3 scatter sabit konum: üst yarı, modal sol-altta açıldığında hepsi görünür.
            g[1] = SYM_SCATTER;   // sat 0, sutun 1
            g[4] = SYM_SCATTER;   // sat 0, sutun 4
            g[11] = SYM_SCATTER;  // sat 1, sutun 5
            return g;
        }

        /// <summary>
        /// 30 hücreyi 6 farklı sembol × 5'er ile doldurur — tüm cluster'lar 8'den az → kazançsız.
        /// Her sıfır spin için farklı seed → dolgu paterni varyasyonu.
        /// </summary>
        private static int[] GridSifir(int seed)
        {
            return GridIlk(null, seed,
                (SYM_ARMUT, 5), (SYM_CILEK, 5), (SYM_ERIK, 5),
                (SYM_KARPUZ, 5), (SYM_MUZ, 5), (SYM_HINDISTAN, 5));
        }

        /// <summary>Spinin grid seed'i: aşama (0..6) × 1000 + spin sıra no (1..) × 10. Her spin için tutarlı + farklı.</summary>
        private static int Seed(int asama, int spinNo) => asama * 1000 + spinNo * 10;

        /// <summary>Bir grid'de verilen sembollerden olan tüm hücreleri Vector2Int olarak döner (x = i % SUTUN, y = i / SUTUN).</summary>
        private static List<Vector2Int> HucreleriBul(int[] grid, params int[] semboller)
        {
            var liste = new List<Vector2Int>();
            var setim = new HashSet<int>(semboller);
            for (int i = 0; i < grid.Length; i++)
            {
                if (setim.Contains(grid[i]))
                {
                    int x = i % SUTUN;
                    int y = i / SUTUN;
                    liste.Add(new Vector2Int(x, y));
                }
            }
            return liste;
        }

        /// <summary>
        /// Tumble: <paramref name="patlayanSyms"/>'un <paramref name="oncekiGrid"/>'deki tüm hücreleri patlar,
        /// her biri için <paramref name="dusenSym"/> sembolü düşer (TEK tip yeni sembol — sonraki tumble cluster'ı için).
        /// </summary>
        private static TumbleAdimTanimi TumbleTekDusen(int[] oncekiGrid, int[] patlayanSyms, int dusenSym)
        {
            var koord = HucreleriBul(oncekiGrid, patlayanSyms);
            int n = koord.Count;
            int[] dusen = new int[n];
            for (int i = 0; i < n; i++) dusen[i] = dusenSym;
            return new TumbleAdimTanimi
            {
                patlayanHucreler = koord,
                yukaridanDusenSemboller = dusen,
                yukaridanDusenCarpanlar = new int[n]
            };
        }

        /// <summary>
        /// Tumble: cluster patlar, üstten dolgu sembolleri (cycling) düşer — kazançsız son tumble için.
        /// <paramref name="hariçSemboller"/> dolgu pool'dan hariç tutulur (genelde allClusters).
        /// </summary>
        private static TumbleAdimTanimi TumbleDolguDusen(int[] oncekiGrid, int[] patlayanSyms, int[] hariçSemboller)
        {
            var koord = HucreleriBul(oncekiGrid, patlayanSyms);
            int[] dolguPool = { SYM_ARMUT, SYM_CILEK, SYM_ERIK, SYM_HINDISTAN, SYM_KARPUZ, SYM_MUZ, SYM_ELMA, SYM_UZUM };
            var hariç = new HashSet<int>();
            if (hariçSemboller != null) foreach (var s in hariçSemboller) hariç.Add(s);
            foreach (var s in patlayanSyms) hariç.Add(s);
            var dolgu = System.Array.FindAll(dolguPool, s => !hariç.Contains(s));
            if (dolgu.Length == 0) dolgu = new[] { SYM_ARMUT };

            int n = koord.Count;
            int[] dusen = new int[n];
            for (int i = 0; i < n; i++) dusen[i] = dolgu[i % dolgu.Length];
            return new TumbleAdimTanimi
            {
                patlayanHucreler = koord,
                yukaridanDusenSemboller = dusen,
                yukaridanDusenCarpanlar = new int[n]
            };
        }

        /// <summary>Önceki grid + tumble adımı → sonraki grid (motor mantığı: patlayan hücreye düşen sembol gelir, diğerleri yerinde).</summary>
        private static int[] GridSonrasiHesapla(int[] onceki, TumbleAdimTanimi tumble)
        {
            int[] sonra = (int[])onceki.Clone();
            if (tumble.patlayanHucreler == null || tumble.yukaridanDusenSemboller == null) return sonra;
            int n = Mathf.Min(tumble.patlayanHucreler.Count, tumble.yukaridanDusenSemboller.Length);
            for (int i = 0; i < n; i++)
            {
                var p = tumble.patlayanHucreler[i];
                int idx = p.y * SUTUN + p.x;
                if (idx >= 0 && idx < sonra.Length)
                    sonra[idx] = tumble.yukaridanDusenSemboller[i];
            }
            return sonra;
        }

        private static TumbleAdimTanimi[] NoTumble() => System.Array.Empty<TumbleAdimTanimi>();

        /// <summary>Tek cluster'lı basit spin: 8 hücre patlar, dolgu sembolleri düşer (kazançsız son state).</summary>
        private static ScriptedSpinKaydi TekClusterSpin(int spinNo, int asama, int bahis, long brut, int sym, SpinTipi tip, string modal = null)
        {
            int[] all = { sym };
            int[] ilk = GridIlk(all, Seed(asama, spinNo), (sym, 8));
            var t1 = TumbleDolguDusen(ilk, all, all);
            return SpinTanimi(spinNo, asama, bahis, tip, brut, ilk, null, new[] { t1 }, modal);
        }

        private static ScriptedSpinKaydi SpinTanimi(
            int spinNo, int asama, int bahis, SpinTipi tip, long brut,
            int[] ilkGrid, int[] ilkCarpanlar, TumbleAdimTanimi[] tumbleler, string modal = null)
        {
            return new ScriptedSpinKaydi
            {
                spinSiraNo = spinNo,
                asamaIndex = asama,
                bahis = bahis,
                tip = tip,
                brutOdeme = brut,
                ilkGridSemboller = ilkGrid,
                ilkCarpanDegerleri = ilkCarpanlar ?? new int[HUCRE_SAYISI],
                tumbleler = new List<TumbleAdimTanimi>(tumbleler),
                modalMesaji = modal,
                carpanKactiFlag = false
            };
        }

        private static void EnsureKlasor(string yol)
        {
            if (Directory.Exists(yol)) return;
            Directory.CreateDirectory(yol);
            AssetDatabase.Refresh();
        }
    }
}
#endif
