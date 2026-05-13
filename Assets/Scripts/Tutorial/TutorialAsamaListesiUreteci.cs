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
