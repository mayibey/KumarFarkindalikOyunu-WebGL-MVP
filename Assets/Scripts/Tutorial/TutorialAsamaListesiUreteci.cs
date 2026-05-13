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
        /// içinde AsamaSetKazanmaSikligi(N) → UretKazancKayit / UretKayipKayit helper'lar ile yapılır.
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
        // PAKET 14-FAZ34: T6 Kazandırma Sıklığı için kazanç + kayıp helper'ları.
        // AsamaSetKazanmaSikligi(N) içinde N kazanç + (5-N) kayıp → Fisher-Yates shuffle → 5 spinlik liste.
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>T6 kazanç dizilimi: 8 Hindistan cluster + (2,3)=3x çarpan → ham 500 × 3 = 1500 nihai (NET +500).
        /// Pedagojik mesaj: "Bahisten az farklı kazanç hissi", kullanıcı 5 spin sonra net hala düşük olabilir.</summary>
        public static ScriptedSpinKaydi UretKazancKayit()
        {
            // Cluster: Hindistan(3) — T4 carpanTest_0 ile aynı pozisyonlar
            int[] grid = new int[30];
            grid[0] = 0;  grid[1] = 3;  grid[2] = 3;  grid[3] = 3;  grid[4] = 1;  grid[5] = 2;
            grid[6] = 6;  grid[7] = 3;  grid[8] = 3;  grid[9] = 3;  grid[10] = 4; grid[11] = 0;
            grid[12] = 5; grid[13] = 1; grid[14] = 3; grid[15] = 3; grid[16] = 7; grid[17] = 2;
            grid[18] = 6; grid[19] = 4; grid[20] = CARPAN_SEMBOL; grid[21] = 5; grid[22] = 0; grid[23] = 7;
            grid[24] = 1; grid[25] = 2; grid[26] = 6; grid[27] = 4; grid[28] = 0; grid[29] = 5;

            int[] carpan = new int[30];
            carpan[20] = 3;  // (2,3) → 3x

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
                brutOdeme = 500,  // 8 Hindistan × payCoef 0.5 × bahis 1000 = 500 ham (çarpan ayrı)
                ilkGridSemboller = grid,
                ilkCarpanDegerleri = carpan,
                tumbleler = new List<TumbleAdimTanimi> { tumble },
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

        /// <summary>PAKET 14-FAZ34 İş 5: T7 Ödeme Aralığı — verilen sembol için 8-li cluster (paytable_8_9 referans).
        /// brutOdeme = payCoef × bahis (ham, çarpansız). Dolgu meyveler manuel ayarlı (cluster kazançlı sembol hariç).
        /// Tumble: 8 cluster patlar, üstten farklı meyveler düşer (cluster oluşturmaz).</summary>
        public static ScriptedSpinKaydi UretOdemeKayit(int sembolId, long brutOdeme)
        {
            // Cluster (8 hücre, T4/T6 ile aynı pozisyonlar): (1,0)(2,0)(3,0)(1,1)(2,1)(3,1)(2,2)(3,2)
            int[] grid = new int[30];
            for (int i = 0; i < 30; i++) grid[i] = -1;

            grid[1] = sembolId;  grid[2] = sembolId;  grid[3] = sembolId;
            grid[7] = sembolId;  grid[8] = sembolId;  grid[9] = sembolId;
            grid[14] = sembolId; grid[15] = sembolId;

            // Dolgu: sembolId ve scatter(8) hariç, max 4 her dolgu sembolünden
            var dolguSayaclar = new int[8];
            dolguSayaclar[sembolId] = 8;
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
                patlayanHucreler = new List<Vector2Int>
                {
                    new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0),
                    new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(3, 1),
                    new Vector2Int(2, 2), new Vector2Int(3, 2),
                },
                yukaridanDusenSemboller = OlusturDolguDizisi(sembolId, 8),
                yukaridanDusenCarpanlar = new[] { 0, 0, 0, 0, 0, 0, 0, 0 },
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

        /// <summary>PAKET 14-FAZ34 İş 7/8/9: Jenerik kazanç kaydı — verilen sembol için 8/10/12-li cluster.
        /// brutOdeme çağıran tarafından paytable hesabıyla geçirilir (ham, çarpansız).</summary>
        public static ScriptedSpinKaydi UretCokAdetKazancKayit(int sembolId, int adet, long brutOdeme)
        {
            int[] grid = new int[30];
            for (int i = 0; i < 30; i++) grid[i] = -1;

            var clusterPozlari = OlusturClusterPozlari(adet);
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
            // 5 spin: Muz10(2000) / Hin12(2500) / Muz10(2000) / Hin12(2500) / Karpuz12(3000) = 12000 NET +7000
            return new List<ScriptedSpinKaydi>
            {
                UretCokAdetKazancKayit(5, 10, 2000),  // PayTable_10_11[5]=2.0
                UretCokAdetKazancKayit(3, 12, 2500),  // PayTable_12+[3]=2.5
                UretCokAdetKazancKayit(5, 10, 2000),
                UretCokAdetKazancKayit(3, 12, 2500),
                UretCokAdetKazancKayit(4, 12, 3000),  // PayTable_12+[4]=3.0
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
            // 3 spin: Hin12(2500) / Muz12(5000, PayTable_12+[5]=5.0) / kayıp = 7500 NET +4500
            return new List<ScriptedSpinKaydi>
            {
                UretCokAdetKazancKayit(3, 12, 2500),
                UretCokAdetKazancKayit(5, 12, 5000),  // gerçek paytable_12+[5]=5.0
                UretKayipKayit(),
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
