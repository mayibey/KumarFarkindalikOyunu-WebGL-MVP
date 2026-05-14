using System.Collections.Generic;
using UnityEngine;
using Senaryo.Scripted;

namespace KumarFarkindalik.Tutorial
{
    /// <summary>
    /// PAKET 14-FAZ33: Tutorial ScriptedSpinKaydi runtime üretici.
    /// 03 ScriptedSpinUygulayici altyapısına geçişin minimum scope'u: T4 carpanTest + T5 bonusTest.
    /// Pattern motor (TutorialSenaryoMotoru) diğer adımlarda (T3 senaryolar, T6YO, T6, T7, T8, T9, T10, T11)
    /// çalışmaya DEVAM eder. Bu üretici sadece T4/T5 için ScriptedSpinKaydi döndürür.
    ///
    /// Grid konvansiyon: 6 sütun × 5 satır, index = y * 6 + x (1D row-major).
    /// Sembol ID: 0=Armut, 1=Çilek, 2=Erik, 3=Hindistan, 4=Karpuz, 5=Muz, 6=Elma, 7=Üzüm, 8=Scatter.
    /// CARPAN_SEMBOL = -2 (çarpan hücresi, gerçek değer ilkCarpanDegerleri[i]'den okunur).
    /// </summary>
    public static class TutorialAsamaListesiUreteci
    {
        private const int CARPAN_SEMBOL = -2;
        private const int SCATTER = 8;
        private const int TUTORIAL_BAHIS = 1000;

        /// <summary>
        /// T4 ve T5 için 4 ScriptedSpinKaydi üretir: carpanTest_100, carpanTest_0, bonusTest_100, bonusTest_0.
        /// PAKET 14-FAZ34: T6 Kazandırma için kazanma_1..kazanma_5 dinamik üretim TutorialScriptedYoneticisi
        /// içinde AsamaSetKazanmaSikligi(N) → UretKazancHavuzu / UretKayipHavuzu helper'lar ile yapılır
        /// (PAKET 14-FAZ35.0: tek-tip helper yerine 5'er elemanlı çeşitli havuz).
        /// </summary>
        public static Dictionary<string, List<ScriptedSpinKaydi>> UretMinimum()
        {
            var sonuc = new Dictionary<string, List<ScriptedSpinKaydi>>
            {
                ["carpanTest_100"] = new List<ScriptedSpinKaydi> { UretCarpanTest100() },
                ["carpanTest_0"] = new List<ScriptedSpinKaydi> { UretCarpanTest0() },
                ["bonusTest_100"] = new List<ScriptedSpinKaydi> { UretBonusTest100() },
                ["bonusTest_0"] = new List<ScriptedSpinKaydi> { UretBonusTest0() },
            };
            Debug.Log($"[TutorialAsamaListesiUreteci] {sonuc.Count} pattern × 1 spin üretildi.");
            return sonuc;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // PAKET 14-FAZ35.0: T6 Kazandırma Sıklığı için 5'er elemanlı kazanç + kayıp HAVUZLARI.
        // AsamaSetKazanmaSikligi(N) → havuzlardan N kazanç + (5-N) kayıp peş peşe (final shuffle YOK).
        // Eski tek-tip UretKazancKayit (çarpanlı 1500) çıkarıldı; çarpansız 2000-3000 bandında 5 farklı sembol.
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>T6 kazanç havuzu: 5 farklı sembol × cluster boyutu kombinasyonu, hepsi çarpansız 2000-3000 TL bandı.
        /// Sembol ID: 0=Armut, 1=Çilek, 2=Erik, 3=Hindistan, 4=Karpuz, 5=Muz, 6=Elma, 7=Üzüm (TumbleAyarlari.cs:16-22).
        /// K1: Muz(5)×10 = 2.0×1000 = 2000 TL · K2: Erik(2)×12 = 2.0×1000 = 2000 TL ·
        /// K3: Hindistan(3)×12 = 2.5×1000 = 2500 TL · K4: Karpuz(4)×12 = 3.0×1000 = 3000 TL ·
        /// K5: Elma(6)×10 = 3.0×1000 = 3000 TL.</summary>
        public static List<ScriptedSpinKaydi> UretKazancHavuzu()
        {
            return new List<ScriptedSpinKaydi>
            {
                UretCokAdetKazancKayit(5, 10, 2000), // K1: Muz × 10
                UretCokAdetKazancKayit(2, 12, 2000), // K2: Erik × 12
                UretCokAdetKazancKayit(3, 12, 2500), // K3: Hindistan × 12
                UretCokAdetKazancKayit(4, 12, 3000), // K4: Karpuz × 12
                UretCokAdetKazancKayit(6, 10, 3000), // K5: Elma × 10
            };
        }

        /// <summary>T6 kayıp havuzu: 5 farklı dolgu deseni, hepsinde cluster yok (4-bağlantılı max=1) ve scatter yok.
        /// Formüller: KY1 (x-2y)%8, KY2 (x+1-2y)%8, KY3 (5-x-2y)%8, KY4 (x+3y)%8, KY5 (x+5y)%8.
        /// Hepsinde yatay komşu farkı 1, dikey komşu farkı ∈ {-2,3,5} → asla 0 mod 8 → aynı sembol komşu olamaz.</summary>
        public static List<ScriptedSpinKaydi> UretKayipHavuzu()
        {
            // KY1: (x - 2y) mod 8 — mevcut UretKayipKayit ile aynı pattern
            int[] gridKY1 = new int[]
            {
                0, 1, 2, 3, 4, 5,   // y=0
                6, 7, 0, 1, 2, 3,   // y=1
                4, 5, 6, 7, 0, 1,   // y=2
                2, 3, 4, 5, 6, 7,   // y=3
                0, 1, 2, 3, 4, 5,   // y=4
            };
            // KY2: (x + 1 - 2y) mod 8 — yatay shift +1
            int[] gridKY2 = new int[]
            {
                1, 2, 3, 4, 5, 6,
                7, 0, 1, 2, 3, 4,
                5, 6, 7, 0, 1, 2,
                3, 4, 5, 6, 7, 0,
                1, 2, 3, 4, 5, 6,
            };
            // KY3: (5 - x - 2y) mod 8 — yatay ters
            int[] gridKY3 = new int[]
            {
                5, 4, 3, 2, 1, 0,
                3, 2, 1, 0, 7, 6,
                1, 0, 7, 6, 5, 4,
                7, 6, 5, 4, 3, 2,
                5, 4, 3, 2, 1, 0,
            };
            // KY4: (x + 3y) mod 8 — dikey adım +3
            int[] gridKY4 = new int[]
            {
                0, 1, 2, 3, 4, 5,
                3, 4, 5, 6, 7, 0,
                6, 7, 0, 1, 2, 3,
                1, 2, 3, 4, 5, 6,
                4, 5, 6, 7, 0, 1,
            };
            // KY5: (x + 5y) mod 8 — dikey adım +5
            int[] gridKY5 = new int[]
            {
                0, 1, 2, 3, 4, 5,
                5, 6, 7, 0, 1, 2,
                2, 3, 4, 5, 6, 7,
                7, 0, 1, 2, 3, 4,
                4, 5, 6, 7, 0, 1,
            };

            return new List<ScriptedSpinKaydi>
            {
                KayipKayitOlustur(gridKY1),
                KayipKayitOlustur(gridKY2),
                KayipKayitOlustur(gridKY3),
                KayipKayitOlustur(gridKY4),
                KayipKayitOlustur(gridKY5),
            };
        }

        private static ScriptedSpinKaydi KayipKayitOlustur(int[] grid30)
        {
            return new ScriptedSpinKaydi
            {
                spinSiraNo = 1,
                asamaIndex = 0,
                bahis = TUTORIAL_BAHIS,
                tip = SpinTipi.Sifir,
                brutOdeme = 0,
                ilkGridSemboller = grid30,
                ilkCarpanDegerleri = new int[30],
                tumbleler = new List<TumbleAdimTanimi>(),
                modalMesaji = null,
                carpanKactiFlag = false,
                bonusOyunuTetikle = false,
                bonusGetirisi = 0,
            };
        }

        /// <summary>PAKET 14-FAZ34 İş 3: T8 Near Miss dizilimi — 7 bitişik Hindistan cluster (eşik 8'in BIR ALTINDA).
        /// Pozisyonlar (1,0)(2,0)(3,0)(1,1)(2,1)(3,1)(2,2) → 7 yan yana → cluster oluşmaz, ödeme yok.
        /// Pedagojik etki: "neredeyse 8 oldu, neredeyse kazanıyordum" hissi. Ham 0, NET -1000 (bahis kaybı).
        /// Geri kalan 23 hücre dolgu (Hindistan hariç 6 meyve dengeli, hiçbiri 7+ olmasın).</summary>
        public static ScriptedSpinKaydi UretNearMissKayit()
        {
            int[] grid = new int[30];
            // Near miss cluster: 7 bitişik Hindistan
            grid[0] = 0;  grid[1] = 3;  grid[2] = 3;  grid[3] = 3;  grid[4] = 1;  grid[5] = 2;   // y=0
            grid[6] = 4;  grid[7] = 3;  grid[8] = 3;  grid[9] = 3;  grid[10] = 5; grid[11] = 0;  // y=1
            grid[12] = 6; grid[13] = 1; grid[14] = 3; grid[15] = 6; grid[16] = 2; grid[17] = 4;  // y=2 — (2,2)=14 → 7. Hindistan
            grid[18] = 4; grid[19] = 5; grid[20] = 6; grid[21] = 5; grid[22] = 0; grid[23] = 5;  // y=3
            grid[24] = 1; grid[25] = 2; grid[26] = 4; grid[27] = 2; grid[28] = 0; grid[29] = 1;  // y=4

            int[] carpan = new int[30]; // hepsi 0

            return new ScriptedSpinKaydi
            {
                spinSiraNo = 1,
                asamaIndex = 0,
                bahis = TUTORIAL_BAHIS,
                tip = SpinTipi.NearMiss,
                brutOdeme = 0,
                ilkGridSemboller = grid,
                ilkCarpanDegerleri = carpan,
                tumbleler = new List<TumbleAdimTanimi>(), // cluster yok → tumble yok
                modalMesaji = null,
                carpanKactiFlag = false,
                bonusOyunuTetikle = false,
                bonusGetirisi = 0,
            };
        }

        /// <summary>PAKET 14-FAZ35.2 T7: Çoklu tumble kazanç kaydı (8-li cluster, çarpansız).
        /// İlk grid: ilkClusterSembol 8-li cluster ((1,0)(2,0)(3,0)(1,1)(2,1)(3,1)(2,2)(3,2)) + dolgu.
        /// Her tumble: patlayan 8 hücreye tumbleClusterSemboller[i] düşer.
        ///   • tumbleSembol >= 0 → 8 hücreye aynı sembol yerleşir → YENİ 8-li cluster oluşur → sonraki tumble patlatır.
        ///   • tumbleSembol == -1 → son tumble dolgu sembolleri düşer (cluster oluşmaz, zincir biter).
        /// Gerçek ödeme ScriptedSpinUygulayici tarafından paytable_8_9 × tumble sayısı × bahis ile hesaplanır;
        /// brutOdeme field sadece Debug.Log raporlaması için (paytable taraması ile birlikte).</summary>
        public static ScriptedSpinKaydi UretCokTumbleliKayit(int ilkClusterSembol, int[] tumbleClusterSemboller)
        {
            // İlk grid: 8-li cluster (ilkClusterSembol) + dolgu (max 4 per sembol, scatter hariç)
            int[] grid = new int[30];
            for (int i = 0; i < 30; i++) grid[i] = -1;

            // PAKET 14-FAZ35.2: No-replace random pattern → T7 3 spin'i 3 farklı pozisyon.
            var clusterPozlari = OlusturRastgeleClusterPozlari(8);
            foreach (var p in clusterPozlari) grid[p.y * 6 + p.x] = ilkClusterSembol;

            var dolguSayaclar = new int[8];
            dolguSayaclar[ilkClusterSembol] = 8;
            int adimSembol = 0;
            for (int i = 0; i < 30; i++)
            {
                if (grid[i] != -1) continue;
                int sec = -1;
                for (int t = 0; t < 8; t++)
                {
                    int aday = (adimSembol + t) % 8;
                    if (aday == ilkClusterSembol) continue;
                    if (dolguSayaclar[aday] < 4) { sec = aday; break; }
                }
                if (sec < 0) sec = (ilkClusterSembol == 0) ? 1 : 0;
                grid[i] = sec;
                dolguSayaclar[sec]++;
                adimSembol = (adimSembol + 1) % 8;
            }

            // Tumble adımları: aynı 8 cluster pozisyonu sürekli patlar, içeriği tumble[i]'ye göre değişir
            var tumbleler = new List<TumbleAdimTanimi>();
            int oncekiCluster = ilkClusterSembol;
            foreach (int tumbleSembol in tumbleClusterSemboller)
            {
                int[] dusenSemboller;
                if (tumbleSembol == -1)
                {
                    dusenSemboller = OlusturDolguDizisi(oncekiCluster, 8); // dolgu — yeni cluster oluşmaz
                }
                else
                {
                    dusenSemboller = new int[8];
                    for (int k = 0; k < 8; k++) dusenSemboller[k] = tumbleSembol; // 8 hücreye aynı sembol → yeni cluster
                    oncekiCluster = tumbleSembol;
                }
                tumbleler.Add(new TumbleAdimTanimi
                {
                    patlayanHucreler = new List<Vector2Int>(clusterPozlari),
                    yukaridanDusenSemboller = dusenSemboller,
                    yukaridanDusenCarpanlar = new int[8],
                });
            }

            // brutOdeme raporlama (paytable_8_9 × bahis × cluster sayısı; ScriptedSpinUygulayici göz ardı eder)
            long ramToplam = 0;
            var oy = Object.FindObjectOfType<OyunYoneticisi>();
            var ta = oy != null ? oy.tumbleAyarlari : null;
            if (ta != null && ta.PayTable_8_9 != null && ilkClusterSembol >= 0 && ilkClusterSembol < ta.PayTable_8_9.Length)
            {
                ramToplam = (long)Mathf.RoundToInt(ta.PayTable_8_9[ilkClusterSembol] * TUTORIAL_BAHIS);
                foreach (int t in tumbleClusterSemboller)
                    if (t >= 0 && t < ta.PayTable_8_9.Length)
                        ramToplam += (long)Mathf.RoundToInt(ta.PayTable_8_9[t] * TUTORIAL_BAHIS);
            }

            return new ScriptedSpinKaydi
            {
                spinSiraNo = 1,
                asamaIndex = 0,
                bahis = TUTORIAL_BAHIS,
                tip = SpinTipi.Kazanc,
                brutOdeme = ramToplam,
                ilkGridSemboller = grid,
                ilkCarpanDegerleri = new int[30],
                tumbleler = tumbleler,
                modalMesaji = null,
                carpanKactiFlag = false,
                bonusOyunuTetikle = false,
                bonusGetirisi = 0,
            };
        }

        // ─────────────────────────────────────────────────────────────────────────
        // PAKET 14-FAZ34 İş 7/8/9: 8/10/12-li cluster pozisyon haritası + jenerik kazanç üretici.
        // 4-yönlü komşuluk (bitişik), tek bağlı cluster (flood-fill OK).
        // 8-li: (1,0)(2,0)(3,0)(1,1)(2,1)(3,1)(2,2)(3,2)
        // 10-li: 8-li + (1,2)(4,1)
        // 12-li: 10-li + (4,0)(4,2)
        // ─────────────────────────────────────────────────────────────────────────
        public static List<Vector2Int> OlusturClusterPozlari(int adet)
        {
            var pozlar = new List<Vector2Int>
            {
                new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0),
                new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(3, 1),
                new Vector2Int(2, 2), new Vector2Int(3, 2),
            };
            if (adet >= 10)
            {
                pozlar.Add(new Vector2Int(1, 2));
                pozlar.Add(new Vector2Int(4, 1));
            }
            if (adet >= 12)
            {
                pozlar.Add(new Vector2Int(4, 0));
                pozlar.Add(new Vector2Int(4, 2));
            }
            return pozlar;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // PAKET 14-FAZ35.2: Cluster pozisyon randomize — T6 + T7 yapay hissi fix.
        // Her T-adımı oturumunda no-replace çekim → garantili pattern çeşitliliği.
        // Pattern'ler 4-bağlantılı (manuel doğrulandı) — görsel "cluster" hissi korunur.
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>8-li cluster pattern havuzu — 6 farklı 4-bağlantılı pattern.</summary>
        private static List<Vector2Int> Pattern8li(int idx)
        {
            switch (idx)
            {
                case 0: // A: üst-orta dama (mevcut OlusturClusterPozlari ile aynı)
                    return new List<Vector2Int> {
                        new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0),
                        new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(3, 1),
                        new Vector2Int(2, 2), new Vector2Int(3, 2),
                    };
                case 1: // B: sol 2×4 dikey blok
                    return new List<Vector2Int> {
                        new Vector2Int(0, 0), new Vector2Int(1, 0),
                        new Vector2Int(0, 1), new Vector2Int(1, 1),
                        new Vector2Int(0, 2), new Vector2Int(1, 2),
                        new Vector2Int(0, 3), new Vector2Int(1, 3),
                    };
                case 2: // C: sağ 2×4 dikey blok
                    return new List<Vector2Int> {
                        new Vector2Int(4, 0), new Vector2Int(5, 0),
                        new Vector2Int(4, 1), new Vector2Int(5, 1),
                        new Vector2Int(4, 2), new Vector2Int(5, 2),
                        new Vector2Int(4, 3), new Vector2Int(5, 3),
                    };
                case 3: // D: alt-orta dama (A'nın aşağıya kaydırılmış, sağa genişlemiş hali)
                    return new List<Vector2Int> {
                        new Vector2Int(2, 2), new Vector2Int(3, 2),
                        new Vector2Int(2, 3), new Vector2Int(3, 3), new Vector2Int(4, 3),
                        new Vector2Int(2, 4), new Vector2Int(3, 4), new Vector2Int(4, 4),
                    };
                case 4: // E: orta 4×2 yatay blok
                    return new List<Vector2Int> {
                        new Vector2Int(1, 2), new Vector2Int(2, 2), new Vector2Int(3, 2), new Vector2Int(4, 2),
                        new Vector2Int(1, 3), new Vector2Int(2, 3), new Vector2Int(3, 3), new Vector2Int(4, 3),
                    };
                case 5: // F: üst 4×2 yatay blok
                    return new List<Vector2Int> {
                        new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0),
                        new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(3, 1),
                    };
                default:
                    return Pattern8li(0);
            }
        }

        /// <summary>10-li cluster pattern havuzu — 4 farklı pattern (8-li + 2 ek bitişik hücre).</summary>
        private static List<Vector2Int> Pattern10li(int idx)
        {
            var bas = Pattern8li(idx);
            switch (idx)
            {
                case 0: // A + (1,2)(4,1) — mevcut 10-li
                    bas.Add(new Vector2Int(1, 2)); bas.Add(new Vector2Int(4, 1)); break;
                case 1: // B + (2,0)(2,1) — sol 3×4 yarı
                    bas.Add(new Vector2Int(2, 0)); bas.Add(new Vector2Int(2, 1)); break;
                case 2: // C + (3,0)(3,1) — sağ 3×4 yarı
                    bas.Add(new Vector2Int(3, 0)); bas.Add(new Vector2Int(3, 1)); break;
                case 3: // E + (1,1)(4,1) — orta 4×2 + 2 üst sarkıt
                    bas.Add(new Vector2Int(1, 1)); bas.Add(new Vector2Int(4, 1)); break;
                default:
                    return Pattern10li(0);
            }
            return bas;
        }

        /// <summary>12-li cluster pattern havuzu — 3 farklı pattern (10-li + 2 ek bitişik hücre).</summary>
        private static List<Vector2Int> Pattern12li(int idx)
        {
            var bas = Pattern10li(idx);
            switch (idx)
            {
                case 0: // A10 + (4,0)(4,2) — mevcut 12-li
                    bas.Add(new Vector2Int(4, 0)); bas.Add(new Vector2Int(4, 2)); break;
                case 1: // B10 + (2,2)(2,3) — sol 3×4 tam blok
                    bas.Add(new Vector2Int(2, 2)); bas.Add(new Vector2Int(2, 3)); break;
                case 2: // E10 + (2,1)(3,1) — orta 4×3 blok
                    bas.Add(new Vector2Int(2, 1)); bas.Add(new Vector2Int(3, 1)); break;
                default:
                    return Pattern12li(0);
            }
            return bas;
        }

        private static int ClusterPatternSayisi(int adet)
        {
            if (adet >= 12) return 3;
            if (adet >= 10) return 4;
            return 6; // 8-li
        }

        private static List<Vector2Int> PatternSec(int adet, int patternIdx)
        {
            if (adet >= 12) return Pattern12li(patternIdx);
            if (adet >= 10) return Pattern10li(patternIdx);
            return Pattern8li(patternIdx);
        }

        // No-replace çekim havuzu (per-adet). Havuz tükenince otomatik dolar.
        private static Dictionary<int, List<int>> _patternHavuzu = new Dictionary<int, List<int>>();

        /// <summary>PAKET 14-FAZ35.2: Cluster pozisyon havuzundan no-replace random çekim.
        /// Aynı T-adımı oturumunda art arda çağrılırsa farklı pattern garantilenir, havuz tükenince reset.
        /// 8-li: 6 pattern, 10-li: 4 pattern, 12-li: 3 pattern.</summary>
        public static List<Vector2Int> OlusturRastgeleClusterPozlari(int adet)
        {
            int havuzKey = adet >= 12 ? 12 : (adet >= 10 ? 10 : 8);
            int toplam = ClusterPatternSayisi(havuzKey);

            if (!_patternHavuzu.ContainsKey(havuzKey) || _patternHavuzu[havuzKey].Count == 0)
            {
                var yeniHavuz = new List<int>();
                for (int i = 0; i < toplam; i++) yeniHavuz.Add(i);
                _patternHavuzu[havuzKey] = yeniHavuz;
            }

            var havuz = _patternHavuzu[havuzKey];
            int secIdx = UnityEngine.Random.Range(0, havuz.Count);
            int patternIdx = havuz[secIdx];
            havuz.RemoveAt(secIdx);

            return PatternSec(adet, patternIdx);
        }

        /// <summary>Tutorial yeniden başlatma için no-replace havuzu temizler.</summary>
        public static void PatternHavuzuSifirla()
        {
            _patternHavuzu.Clear();
        }

        /// <summary>PAKET 14-FAZ34 İş 7/8/9: Jenerik kazanç kaydı — verilen sembol için 8/10/12-li cluster.
        /// brutOdeme çağıran tarafından paytable hesabıyla geçirilir (ham, çarpansız).</summary>
        public static ScriptedSpinKaydi UretCokAdetKazancKayit(int sembolId, int adet, long brutOdeme)
        {
            int[] grid = new int[30];
            for (int i = 0; i < 30; i++) grid[i] = -1;

            // PAKET 14-FAZ35.2: No-replace random pattern çekim → her T3/T6/T6YO/T9 spininde farklı pozisyon.
            var clusterPozlari = OlusturRastgeleClusterPozlari(adet);
            foreach (var p in clusterPozlari)
                grid[p.y * 6 + p.x] = sembolId;

            // Dolgu: sembolId ve scatter(8) hariç, max 4 her dolgu sembolünden (cluster yok kuralı)
            var dolguSayaclar = new int[8];
            dolguSayaclar[sembolId] = adet;
            int adimSembol = 0;
            for (int i = 0; i < 30; i++)
            {
                if (grid[i] != -1) continue;
                int sec = -1;
                for (int t = 0; t < 8; t++)
                {
                    int aday = (adimSembol + t) % 8;
                    if (aday == sembolId) continue;
                    if (dolguSayaclar[aday] < 4) { sec = aday; break; }
                }
                if (sec < 0) sec = (sembolId == 0) ? 1 : 0;
                grid[i] = sec;
                dolguSayaclar[sec]++;
                adimSembol = (adimSembol + 1) % 8;
            }

            int[] carpan = new int[30];

            var tumble = new TumbleAdimTanimi
            {
                patlayanHucreler = new List<Vector2Int>(clusterPozlari),
                yukaridanDusenSemboller = OlusturDolguDizisi(sembolId, adet),
                yukaridanDusenCarpanlar = new int[adet],
            };

            return new ScriptedSpinKaydi
            {
                spinSiraNo = 1,
                asamaIndex = 0,
                bahis = TUTORIAL_BAHIS,
                tip = SpinTipi.Kazanc,
                brutOdeme = brutOdeme,
                ilkGridSemboller = grid,
                ilkCarpanDegerleri = carpan,
                tumbleler = new List<TumbleAdimTanimi> { tumble },
                modalMesaji = null,
                carpanKactiFlag = false,
                bonusOyunuTetikle = false,
                bonusGetirisi = 0,
            };
        }

        // ─────────────────────────────────────────────────────────────────────────
        // İŞ 7: T3 senaryolar — hook / yontma / tutma / koruma
        // ─────────────────────────────────────────────────────────────────────────

        public static List<ScriptedSpinKaydi> UretHookSpinleri()
        {
            // PAKET 14-FAZ34.2 BUG C FIX: 5 farklı meyve görsel — Hindistan + Muz tekrarı yerine
            // Muz / Hindistan / Üzüm / Elma / Karpuz (5 ayrı sembol). NET +7000 KORUNUR.
            //   Spin 1: Muz 10  (sym=5, PayTable_10_11[5]=2.0) → ham 2000
            //   Spin 2: Hindistan 12 (sym=3, PayTable_12+[3]=2.5) → ham 2500
            //   Spin 3: Üzüm 8  (sym=7, PayTable_8_9[7]=1.5) → ham 1500
            //   Spin 4: Elma 10 (sym=6, PayTable_10_11[6]=3.0) → ham 3000
            //   Spin 5: Karpuz 12 (sym=4, PayTable_12+[4]=3.0) → ham 3000
            //   Toplam: 12000 - 5000 bahis = NET +7000 (eski tasarımla aynı)
            return new List<ScriptedSpinKaydi>
            {
                UretCokAdetKazancKayit(5, 10, 2000),  // Muz 10
                UretCokAdetKazancKayit(3, 12, 2500),  // Hindistan 12
                UretCokAdetKazancKayit(7, 8,  1500),  // Üzüm 8 (cluster eşik)
                UretCokAdetKazancKayit(6, 10, 3000),  // Elma 10
                UretCokAdetKazancKayit(4, 12, 3000),  // Karpuz 12
            };
        }

        public static List<ScriptedSpinKaydi> UretYontmaSpinleri()
        {
            // 5 spin 8-li: Hin(500) / Çilek(300) / Hin(500) / Armut(200) / Erik(400) = 1900 NET -3100
            return new List<ScriptedSpinKaydi>
            {
                UretCokAdetKazancKayit(3, 8, 500),  // PayTable_8_9[3]=0.5
                UretCokAdetKazancKayit(1, 8, 300),  // PayTable_8_9[1]=0.3
                UretCokAdetKazancKayit(3, 8, 500),
                UretCokAdetKazancKayit(0, 8, 200),  // PayTable_8_9[0]=0.2
                UretCokAdetKazancKayit(2, 8, 400),  // PayTable_8_9[2]=0.4
            };
        }

        public static List<ScriptedSpinKaydi> UretTutmaSpinleri()
        {
            // 6 spin: kayıp/kayıp/Muz10(2000)/kayıp/kayıp/Muz10(2000) = 4000 NET -2000
            return new List<ScriptedSpinKaydi>
            {
                UretKayipKayit(),
                UretKayipKayit(),
                UretCokAdetKazancKayit(5, 10, 2000),
                UretKayipKayit(),
                UretKayipKayit(),
                UretCokAdetKazancKayit(5, 10, 2000),
            };
        }

        public static List<ScriptedSpinKaydi> UretKorumaSpinleri()
        {
            // 5 spin saf kayıp NET -5000
            return new List<ScriptedSpinKaydi>
            {
                UretKayipKayit(), UretKayipKayit(), UretKayipKayit(), UretKayipKayit(), UretKayipKayit(),
            };
        }

        // ─────────────────────────────────────────────────────────────────────────
        // İŞ 8: T6YO YeniOyuncu — açık / kapalı
        // ─────────────────────────────────────────────────────────────────────────

        public static List<ScriptedSpinKaydi> UretYeniOyuncuAcikSpinleri()
        {
            // PAKET 14-FAZ34.1 BUG 4 FIX: Önceki tasarım (Hin12/Muz12/kayıp NET +4500) abartılıydı;
            // ayrıca 2.spinde Muz 12 yine Hindistan görünüyordu (sembol indeks veya cluster pozisyon bug).
            // Yeni tasarım: 3 farklı meyve mütevazı kazanç, kayıp YOK:
            //   Spin 1: Hindistan 12 (sym=3, PayTable_12+[3]=2.5) → ham 2500
            //   Spin 2: Muz 10 (sym=5, PayTable_10_11[5]=2.0) → ham 2000
            //   Spin 3: Karpuz 10 (sym=4, PayTable_10_11[4]=1.5) → ham 1500
            //   Toplam 6000 - 3000 bahis = NET +3000 (abartı yok, 3 farklı meyve)
            return new List<ScriptedSpinKaydi>
            {
                UretCokAdetKazancKayit(3, 12, 2500),  // Hindistan 12
                UretCokAdetKazancKayit(5, 10, 2000),  // Muz 10
                UretCokAdetKazancKayit(4, 10, 1500),  // Karpuz 10
            };
        }

        public static List<ScriptedSpinKaydi> UretYeniOyuncuKapaliSpinleri()
        {
            return new List<ScriptedSpinKaydi> { UretKayipKayit(), UretKayipKayit(), UretKayipKayit() };
        }

        // ─────────────────────────────────────────────────────────────────────────
        // İŞ 9: T10 Çarpan Zorla — kapalı ödeme / açık ödeme
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>Cluster YOK + tek 500x çarpan (2,3) hücresinde. Cluster olmadığı için çarpan ödeme yapmaz, NET -1000.</summary>
        public static ScriptedSpinKaydi UretCarpanZorlaKapaliKayit()
        {
            int[] grid = new int[]
            {
                0, 1, 2, 3, 4, 5,
                6, 7, 0, 1, 2, 3,
                4, 5, 6, 7, 0, 1,
                2, 3, CARPAN_SEMBOL, 5, 6, 7,  // (2,3)=index 20 = çarpan
                0, 1, 2, 3, 4, 5,
            };
            int[] carpan = new int[30];
            carpan[20] = 500;  // 500x

            return new ScriptedSpinKaydi
            {
                spinSiraNo = 1,
                asamaIndex = 0,
                bahis = TUTORIAL_BAHIS,
                tip = SpinTipi.Sifir,
                brutOdeme = 0,
                ilkGridSemboller = grid,
                ilkCarpanDegerleri = carpan,
                tumbleler = new List<TumbleAdimTanimi>(), // cluster yok → tumble yok
                modalMesaji = null,
                carpanKactiFlag = true,  // "çarpan kaçtı" görsel etki (cluster yok → ödeme 0)
                bonusOyunuTetikle = false,
                bonusGetirisi = 0,
            };
        }

        /// <summary>Muz 12 cluster + 500x çarpan (cluster komşusu olmayan hücre). Ham 5000 × 500 = 2.500.000 nihai (mega win).</summary>
        public static ScriptedSpinKaydi UretCarpanZorlaAcikKayit()
        {
            int[] grid = new int[30];
            for (int i = 0; i < 30; i++) grid[i] = -1;

            var clusterPozlari = OlusturClusterPozlari(12);
            foreach (var p in clusterPozlari) grid[p.y * 6 + p.x] = 5; // Muz

            // Cluster pozisyonları (12-li): index 1,2,3,4,7,8,9,10,13,14,15,16
            // Çarpan pozisyonu: (1,4)=index 25 — cluster komşusu olmayan, alt orta
            int carpanIdx = 25;
            grid[carpanIdx] = CARPAN_SEMBOL;

            // Dolgu (kalan hücreler): cluster sembol Muz hariç + scatter hariç
            var dolguSayaclar = new int[8];
            dolguSayaclar[5] = 12; // Muz cluster
            int adimSembol = 0;
            for (int i = 0; i < 30; i++)
            {
                if (grid[i] != -1) continue;
                int sec = -1;
                for (int t = 0; t < 8; t++)
                {
                    int aday = (adimSembol + t) % 8;
                    if (aday == 5) continue;
                    if (dolguSayaclar[aday] < 4) { sec = aday; break; }
                }
                if (sec < 0) sec = 0;
                grid[i] = sec;
                dolguSayaclar[sec]++;
                adimSembol = (adimSembol + 1) % 8;
            }

            int[] carpan = new int[30];
            carpan[carpanIdx] = 500;

            var tumble = new TumbleAdimTanimi
            {
                patlayanHucreler = new List<Vector2Int>(clusterPozlari),
                yukaridanDusenSemboller = OlusturDolguDizisi(5, 12),
                yukaridanDusenCarpanlar = new int[12],
            };

            return new ScriptedSpinKaydi
            {
                spinSiraNo = 1,
                asamaIndex = 0,
                bahis = TUTORIAL_BAHIS,
                tip = SpinTipi.MegaWin,
                brutOdeme = 5000,  // ham (çarpansız) — Muz 12 × 5.0 × bahis 1000
                ilkGridSemboller = grid,
                ilkCarpanDegerleri = carpan,
                tumbleler = new List<TumbleAdimTanimi> { tumble },
                modalMesaji = null,
                carpanKactiFlag = false,
                bonusOyunuTetikle = false,
                bonusGetirisi = 0,
            };
        }

        private static int[] OlusturDolguDizisi(int hariçSembol, int adet)
        {
            var sonuc = new int[adet];
            int gecerli = (hariçSembol == 0) ? 1 : 0;
            for (int i = 0; i < adet; i++)
            {
                int aday = (gecerli + i) % 8;
                if (aday == hariçSembol) aday = (aday + 1) % 8;
                sonuc[i] = aday;
            }
            return sonuc;
        }

        /// <summary>PAKET 14-FAZ34 İş 4: T9 Kaçış Frenleme — kazanç dizilimi (8 Hindistan + (2,3)=2x).
        /// Ham 0.5 × 1000 = 500 × çarpan 2 = 1000 TL nihai → NET 0 (başabaş, "sistem seni tam çekip gitmeden tutuyor" pedagojisi).
        /// Önce N kayıp → bu kazanç → kullanıcı "neredeyse kaçıyordum ama kazandım" hissi.</summary>
        public static ScriptedSpinKaydi UretKacisKazancKayit()
        {
            int[] grid = new int[30];
            grid[0] = 0;  grid[1] = 3;  grid[2] = 3;  grid[3] = 3;  grid[4] = 1;  grid[5] = 2;
            grid[6] = 6;  grid[7] = 3;  grid[8] = 3;  grid[9] = 3;  grid[10] = 4; grid[11] = 0;
            grid[12] = 5; grid[13] = 1; grid[14] = 3; grid[15] = 3; grid[16] = 7; grid[17] = 2;
            grid[18] = 6; grid[19] = 4; grid[20] = CARPAN_SEMBOL; grid[21] = 5; grid[22] = 0; grid[23] = 7;
            grid[24] = 1; grid[25] = 2; grid[26] = 6; grid[27] = 4; grid[28] = 0; grid[29] = 5;

            int[] carpan = new int[30];
            carpan[20] = 2;  // (2,3) → 2x

            var tumble = new TumbleAdimTanimi
            {
                patlayanHucreler = new List<Vector2Int>
                {
                    new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0),
                    new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(3, 1),
                    new Vector2Int(2, 2), new Vector2Int(3, 2),
                },
                yukaridanDusenSemboller = new[] { 0, 1, 2, 4, 5, 6, 7, 1 },
                yukaridanDusenCarpanlar = new[] { 0, 0, 0, 0, 0, 0, 0, 0 },
            };

            return new ScriptedSpinKaydi
            {
                spinSiraNo = 1,
                asamaIndex = 0,
                bahis = TUTORIAL_BAHIS,
                tip = SpinTipi.Kazanc,
                brutOdeme = 500,
                ilkGridSemboller = grid,
                ilkCarpanDegerleri = carpan,
                tumbleler = new List<TumbleAdimTanimi> { tumble },
                modalMesaji = null,
                carpanKactiFlag = false,
                bonusOyunuTetikle = false,
                bonusGetirisi = 0,
            };
        }

        /// <summary>T6 kayıp dizilimi: cluster yok, scatter yok, dolgu meyveler dağıtık → NET -1000 (saf bahis kaybı).
        /// PAKET 14-FAZ34 İş 3: T8 Normal spin (near miss yok) için de yeniden kullanılır.</summary>
        public static ScriptedSpinKaydi UretKayipKayit()
        {
            int[] grid = new int[]
            {
                0, 1, 2, 3, 4, 5,   // y=0
                6, 7, 0, 1, 2, 3,   // y=1
                4, 5, 6, 7, 0, 1,   // y=2
                2, 3, 4, 5, 6, 7,   // y=3
                0, 1, 2, 3, 4, 5,   // y=4
            };
            int[] carpan = new int[30];
            return new ScriptedSpinKaydi
            {
                spinSiraNo = 1,
                asamaIndex = 0,
                bahis = TUTORIAL_BAHIS,
                tip = SpinTipi.Sifir,
                brutOdeme = 0,
                ilkGridSemboller = grid,
                ilkCarpanDegerleri = carpan,
                tumbleler = new List<TumbleAdimTanimi>(),
                modalMesaji = null,
                carpanKactiFlag = false,
                bonusOyunuTetikle = false,
                bonusGetirisi = 0,
            };
        }

        // ─────────────────────────────────────────────────────────────────────────
        // T4 carpanTest_100: 8 Elma cluster + (2,3)=5x çarpan → Nihai 5000, NET +4000
        // ─────────────────────────────────────────────────────────────────────────
        private static ScriptedSpinKaydi UretCarpanTest100()
        {
            // Cluster: Elma(6) → (1,0)(2,0)(3,0)(1,1)(2,1)(3,1)(2,2)(3,2) = 8 hücre, 4-bağlantılı
            // Çarpan: (2,3) = CARPAN_SEMBOL, değer 5
            // Dolgu: 7 sembol × 3 = 21 hücre (Elma hariç, Scatter hariç)
            int[] grid = new int[30];
            grid[0] = 0;  grid[1] = 6;  grid[2] = 6;  grid[3] = 6;  grid[4] = 1;  grid[5] = 2;   // y=0
            grid[6] = 3;  grid[7] = 6;  grid[8] = 6;  grid[9] = 6;  grid[10] = 4; grid[11] = 0;  // y=1
            grid[12] = 5; grid[13] = 1; grid[14] = 6; grid[15] = 6; grid[16] = 7; grid[17] = 2;  // y=2
            grid[18] = 3; grid[19] = 4; grid[20] = CARPAN_SEMBOL; grid[21] = 5; grid[22] = 0; grid[23] = 7; // y=3
            grid[24] = 1; grid[25] = 2; grid[26] = 7; grid[27] = 3; grid[28] = 4; grid[29] = 5;  // y=4

            int[] carpan = new int[30];
            carpan[20] = 5;  // (2,3) → 5x

            // Tumble: 8 Elma patlar, üstten 8 dolgu meyve düşer (cluster yok kuralı)
            var tumble = new TumbleAdimTanimi
            {
                patlayanHucreler = new List<Vector2Int>
                {
                    new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0),
                    new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(3, 1),
                    new Vector2Int(2, 2), new Vector2Int(3, 2),
                },
                yukaridanDusenSemboller = new[] { 0, 1, 2, 3, 4, 5, 7, 0 }, // dağıtık dolgu
                yukaridanDusenCarpanlar = new[] { 0, 0, 0, 0, 0, 0, 0, 0 },
            };

            return new ScriptedSpinKaydi
            {
                spinSiraNo = 1,
                asamaIndex = 0,
                bahis = TUTORIAL_BAHIS,
                tip = SpinTipi.Kazanc,
                brutOdeme = 1000,  // 8 Elma × payCoef 1.0 × bahis 1000 = 1000 ham (çarpan ayrı)
                ilkGridSemboller = grid,
                ilkCarpanDegerleri = carpan,
                tumbleler = new List<TumbleAdimTanimi> { tumble },
                modalMesaji = null,
                carpanKactiFlag = false,
                bonusOyunuTetikle = false,
                bonusGetirisi = 0,
            };
        }

        // ─────────────────────────────────────────────────────────────────────────
        // T4 carpanTest_0: 8 Hindistan cluster (ham 500), çarpan yok
        // ─────────────────────────────────────────────────────────────────────────
        private static ScriptedSpinKaydi UretCarpanTest0()
        {
            // Cluster: Hindistan(3) → aynı 8 pozisyon
            int[] grid = new int[30];
            grid[0] = 0;  grid[1] = 3;  grid[2] = 3;  grid[3] = 3;  grid[4] = 1;  grid[5] = 2;
            grid[6] = 6;  grid[7] = 3;  grid[8] = 3;  grid[9] = 3;  grid[10] = 4; grid[11] = 0;
            grid[12] = 5; grid[13] = 1; grid[14] = 3; grid[15] = 3; grid[16] = 7; grid[17] = 2;
            grid[18] = 6; grid[19] = 4; grid[20] = 7; grid[21] = 5; grid[22] = 0; grid[23] = 7;
            grid[24] = 1; grid[25] = 2; grid[26] = 6; grid[27] = 4; grid[28] = 0; grid[29] = 5;

            int[] carpan = new int[30]; // hepsi 0

            var tumble = new TumbleAdimTanimi
            {
                patlayanHucreler = new List<Vector2Int>
                {
                    new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0),
                    new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(3, 1),
                    new Vector2Int(2, 2), new Vector2Int(3, 2),
                },
                yukaridanDusenSemboller = new[] { 0, 1, 2, 4, 5, 6, 7, 1 },
                yukaridanDusenCarpanlar = new[] { 0, 0, 0, 0, 0, 0, 0, 0 },
            };

            return new ScriptedSpinKaydi
            {
                spinSiraNo = 1,
                asamaIndex = 0,
                bahis = TUTORIAL_BAHIS,
                tip = SpinTipi.Kazanc,
                brutOdeme = 500,  // 8 Hindistan × payCoef 0.5 × bahis 1000 = 500 ham
                ilkGridSemboller = grid,
                ilkCarpanDegerleri = carpan,
                tumbleler = new List<TumbleAdimTanimi> { tumble },
                modalMesaji = null,
                carpanKactiFlag = false,
                bonusOyunuTetikle = false,
                bonusGetirisi = 0,
            };
        }

        // ─────────────────────────────────────────────────────────────────────────
        // T5 bonusTest_100: 4 scatter SABİT (1,1)(4,2)(2,3)(5,0) + dolgu meyveler (cluster yok)
        // ─────────────────────────────────────────────────────────────────────────
        private static ScriptedSpinKaydi UretBonusTest100()
        {
            // Scatter pozisyonları: (1,1)=index 7, (4,2)=16, (2,3)=20, (5,0)=5
            // Dolgu: 26 hücre, 7 meyve sembol (scatter 8 hariç), max 5 per sembol
            int[] grid = new int[30];
            grid[0] = 0;  grid[1] = 1;  grid[2] = 2;  grid[3] = 3;  grid[4] = 4;  grid[5] = SCATTER; // y=0
            grid[6] = 5;  grid[7] = SCATTER; grid[8] = 6;  grid[9] = 6;  grid[10] = 3; grid[11] = 1; // y=1
            grid[12] = 2; grid[13] = 0; grid[14] = 4; grid[15] = 5; grid[16] = SCATTER; grid[17] = 6; // y=2
            grid[18] = 1; grid[19] = 3; grid[20] = SCATTER; grid[21] = 4; grid[22] = 0; grid[23] = 2; // y=3
            grid[24] = 4; grid[25] = 5; grid[26] = 0; grid[27] = 1; grid[28] = 2; grid[29] = 3; // y=4

            int[] carpan = new int[30]; // hepsi 0

            // Tumble yok: scatter cluster oluşturmaz, normal meyve cluster da yok (max 5 per sembol)
            return new ScriptedSpinKaydi
            {
                spinSiraNo = 1,
                asamaIndex = 0,
                bahis = TUTORIAL_BAHIS,
                tip = SpinTipi.Sifir,
                brutOdeme = 0,
                ilkGridSemboller = grid,
                ilkCarpanDegerleri = carpan,
                tumbleler = new List<TumbleAdimTanimi>(), // tumble yok
                modalMesaji = null,
                carpanKactiFlag = false,
                bonusOyunuTetikle = false, // T5 bonus pipeline OyunYoneticisi scatter detection ile çalışır (Faz 5 reset)
                bonusGetirisi = 0,
            };
        }

        // ─────────────────────────────────────────────────────────────────────────
        // T5 bonusTest_0: Saf kayıp grid, scatter yok, cluster yok
        // ─────────────────────────────────────────────────────────────────────────
        private static ScriptedSpinKaydi UretBonusTest0()
        {
            // 30 hücre dağıtık 8 meyve (scatter 8 hariç) — max 5 per sembol, cluster yok
            int[] grid = new int[]
            {
                0, 1, 2, 3, 4, 5,   // y=0
                6, 7, 0, 1, 2, 3,   // y=1
                4, 5, 6, 7, 0, 1,   // y=2
                2, 3, 4, 5, 6, 7,   // y=3
                0, 1, 2, 3, 4, 5,   // y=4
            };

            int[] carpan = new int[30];

            return new ScriptedSpinKaydi
            {
                spinSiraNo = 1,
                asamaIndex = 0,
                bahis = TUTORIAL_BAHIS,
                tip = SpinTipi.Sifir,
                brutOdeme = 0,
                ilkGridSemboller = grid,
                ilkCarpanDegerleri = carpan,
                tumbleler = new List<TumbleAdimTanimi>(),
                modalMesaji = null,
                carpanKactiFlag = false,
                bonusOyunuTetikle = false,
                bonusGetirisi = 0,
            };
        }
    }
}
