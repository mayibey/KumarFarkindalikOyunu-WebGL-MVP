using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

namespace KumarFarkindalik.Tutorial
{
    /// <summary>
    /// PAKET 4-FAZ-1: 04 Tutorial T3 senaryoları için DETERMİNİSTİK spin pattern motoru.
    ///
    /// Pattern kod-içinde sabit (asset DEĞİL → ScriptedAsamaListesi 03 paylaşımı bozulmaz).
    /// Reflection ile OyunYoneticisi._oncedenHesaplananKayit field'ını override eder.
    /// Sahne unload'da Destroy → tüm static state RESET (03 kontaminasyon güvenliği).
    ///
    /// 03 ile çatışmama kuralları:
    ///   - Unity Random.seed'e ASLA dokunmaz (System.Random instance kullanır).
    ///   - OyunYoneticisi.PayTable_* field'larına ASLA yazmaz (sadece okur).
    ///   - ScriptedAsamaListesi asset'ini değiştirmez.
    ///   - Tutorial sahnesi (build idx 3) dışında SetActive(false).
    /// </summary>
    [Preserve]
    public class TutorialSenaryoMotoru : MonoBehaviour
    {
        public const int BUILD_INDEX = 3;
        public static TutorialSenaryoMotoru Ornek { get; private set; }

        private struct SpinDesen
        {
            public int sembolId;  // -1 = kayıp grid (cluster yok); 0..7 = meyve sembol
            public int adet;      // 8-12 (8-9 → x8-9, 10-11 → x10-11, 12+ → x12+); 7 = near miss
            // PAKET 6D — T11 (Çarpan Zorla) demo: çarpan grid'e CARPAN_SEMBOL yerleştir
            public bool carpanZorla;
            public int carpanDeger; // örn 500
            // PAKET 14-FAZ29: SABİT çarpan pattern — RNG'siz, her oyuncu AYNI pozisyon + değer.
            // null/boş ise eski RNG mantığı (carpanUretimOlasiligi'na göre).
            public Vector2Int[] carpanPozlari;
            public int[] carpanDegerleri;
            // PAKET 14-FAZ29: SABİT scatter pattern — RNG'siz pozisyon. null/boş ise Fisher-Yates RNG.
            public Vector2Int[] scatterPozlari;
        }

        // Pattern dictionary — bahis=1000 TL, tumble YOK (tek paytable hit / tek tumble adımı)
        private static readonly Dictionary<string, SpinDesen[]> _patternlar = new()
        {
            // PAKET 14 — T3_HOOK: 5 spin — 2000/2500/2000/2500/3000 TL (toplam 12.000 TL).
            // SENARYOLAR.hook.yeniOyuncu=false → AdminSetYeniOyuncuModu çağrılmaz → maxOdeme=1000
            // limiti yok → "bol kazanç" pedagojisi: bahsin 2-3× üstü ödemeler kanca hissi yaratır.
            // 5 spin → 12.000 kazanç vs 5.000 bahis = NET +7.000 TL.
            ["hook"] = new[]
            {
                new SpinDesen { sembolId = 5, adet = 10 },  // x10-11[5]=2.0 → 2000 TL
                new SpinDesen { sembolId = 3, adet = 12 },  // x12+[3]=2.5  → 2500 TL
                new SpinDesen { sembolId = 5, adet = 10 },  // 2000 TL
                new SpinDesen { sembolId = 3, adet = 12 },  // 2500 TL
                new SpinDesen { sembolId = 4, adet = 12 },  // x12+[4]=3.0  → 3000 TL
            },
            // T3_YONTMA: 5 spin — 500/300/500/200/400 (toplam 1.900 TL, bahis toplamı 5000 → net kayıp)
            ["yontma"] = new[]
            {
                new SpinDesen { sembolId = 3, adet = 8 },   // x8-9 [3] = 0.5 → 500 TL
                new SpinDesen { sembolId = 1, adet = 8 },   // x8-9 [1] = 0.3 → 300 TL
                new SpinDesen { sembolId = 3, adet = 8 },   // 500 TL
                new SpinDesen { sembolId = 0, adet = 8 },   // x8-9 [0] = 0.2 → 200 TL
                new SpinDesen { sembolId = 2, adet = 8 },   // x8-9 [2] = 0.4 → 400 TL
            },
            // T3_TUTMA: 6 spin — 0/0/2000/0/0/2000 (kaçış engelleme — 2 kez tutucu kazanç)
            ["tutma"] = new[]
            {
                new SpinDesen { sembolId = -1, adet = 0 },
                new SpinDesen { sembolId = -1, adet = 0 },
                new SpinDesen { sembolId = 5, adet = 10 },  // 2000 TL tutucu
                new SpinDesen { sembolId = -1, adet = 0 },
                new SpinDesen { sembolId = -1, adet = 0 },
                new SpinDesen { sembolId = 5, adet = 10 },  // 2000 TL tutucu
            },
            // PAKET 14-FAZ2: T3_KORUMA — 5/5 kayıp (bakiye tüketme pedagojisi).
            // Net: 5 spin × 1000 bahis = -5000 TL. Hiç "kurtarıcı kazanç" yok → kullanıcı korumanın
            // gerçekte ne kadar agresif olabileceğini hissetsin.
            ["koruma"] = new[]
            {
                new SpinDesen { sembolId = -1, adet = 0 },
                new SpinDesen { sembolId = -1, adet = 0 },
                new SpinDesen { sembolId = -1, adet = 0 },
                new SpinDesen { sembolId = -1, adet = 0 },
                new SpinDesen { sembolId = -1, adet = 0 },
            },
            // PAKET 9 — T5 BONUS SEMBOLÜ 2-aşamalı: %100 → 1 spin (bonus garanti), %0 → 1 spin (bonus yok).
            // bonusTest_100: 4 scatter düşer → bonus tetiklenir; bonusTest_0: normal kayıp grid → bonus tetiklenmez.
            // PAKET 14-FAZ29: scatterPozlari SABİT 4 pozisyon — her oyuncu AYNI yıldız dağılımı.
            ["bonusTest_100"] = new[]
            {
                new SpinDesen
                {
                    sembolId = SCATTER_PATTERN_FLAG, adet = 4,
                    scatterPozlari = new[]
                    {
                        new Vector2Int(1, 1),
                        new Vector2Int(4, 2),
                        new Vector2Int(2, 3),
                        new Vector2Int(5, 0),
                    }
                },
            },
            ["bonusTest_0"] = new[]
            {
                new SpinDesen { sembolId = -1, adet = 0 },                     // %0 → cluster yok, scatter yok, bonus yok
            },
            // PAKET 9 — T4 ÇARPAN 2-aşamalı: %100 → 1 spin (çarpan kesin), %0 → 1 spin (çarpan yok).
            // PAKET 14-FAZ29: carpanPozlari/Degerleri SABİT — RNG'siz, deterministik 2 çarpan
            //   pozisyon (2,3) → 5x, (4,1) → 10x. Her oyuncu AYNI çarpan deneyimi yaşar.
            // carpanTest_0: arrayler boş → çarpan yerleştirilmez (carpanUretimOlasiligi=0 path).
            ["carpanTest_100"] = new[]
            {
                new SpinDesen
                {
                    sembolId = 3, adet = 8,   // 500 TL + 2 sabit çarpan
                    carpanPozlari = new[] { new Vector2Int(2, 3), new Vector2Int(4, 1) },
                    carpanDegerleri = new[] { 5, 10 }
                },
            },
            ["carpanTest_0"] = new[]
            {
                new SpinDesen { sembolId = 3, adet = 8 },   // 500 TL + çarpan yok (slider %0)
            },
            // PAKET 14-FAZ7 — T6_YENI_OYUNCU TERS senaryo:
            // 1.aşama (toggle AÇIK): yeniOyuncu_acik — 3 spin, 2 kazanç + 1 kayıp (NET kazanç, kanca pedagojisi)
            // 2.aşama (toggle KAPALI): yeniOyuncu_kapali — 3 spin HEPSİ kayıp (gerçek RTP, sömürü ortaya çıkar)
            ["yeniOyuncu_kapali"] = new[]
            {
                new SpinDesen { sembolId = -1, adet = 0 },   // Kayıp
                new SpinDesen { sembolId = -1, adet = 0 },   // Kayıp
                new SpinDesen { sembolId = -1, adet = 0 },   // Kayıp (hepsi → -3000 TL net)
            },
            ["yeniOyuncu_acik"] = new[]
            {
                new SpinDesen { sembolId = 3, adet = 12 },   // 2500 TL hindistan (x12+[3]=2.5)
                new SpinDesen { sembolId = 5, adet = 12 },   // 2000 TL muz (farklı meyve, x12+[5]=2.0)
                new SpinDesen { sembolId = -1, adet = 0 },   // Kayıp — 3.spin kayıp, net +2500 TL
            },
            // PAKET 14-FAZ2: T7 Ödeme aralığı artık DİNAMİK (DinamikOdemePatternBaslat ile paytable
            // taramasından üretilir). Eski sabit "odeme_dusukMaks" ve "odeme_aralik3_5" KALDIRILDI.
            // PAKET 6D — T10 (Kaçış): 3 kayıp + 1 kazanç (limit anında otomatik frenleme)
            ["kacisFrenle"] = new[]
            {
                new SpinDesen { sembolId = -1, adet = 0 },   // Spin 1: Kayıp
                new SpinDesen { sembolId = -1, adet = 0 },   // Spin 2: Kayıp
                new SpinDesen { sembolId = -1, adet = 0 },   // Spin 3: Kayıp (limit'e ulaşıldı)
                new SpinDesen { sembolId = 5, adet = 10 },   // Spin 4: 2000 TL KAZANÇ (frenleme)
            },
            // PAKET 6D — T11 (Çarpan Zorla) aşama 1: çarpan düşer AMA ödeme yapacak cluster yok
            ["carpanZorla_kapaliOdeme"] = new[]
            {
                new SpinDesen { sembolId = -1, adet = 0, carpanZorla = true, carpanDeger = 500 },
            },
            // PAKET 6D — T11 aşama 2: cluster + çarpan → büyük kazanç
            ["carpanZorla_acikOdeme"] = new[]
            {
                new SpinDesen { sembolId = 5, adet = 12, carpanZorla = true, carpanDeger = 500 },
            },
            // "normal" → bu dict'te yok → PatternBaslat motor pasif → RNG akışı
        };

        // PAKET 6C1: Scatter pattern özel flag (sembolId yerine kullanılır, scatterIdx'e dönüştürülür DesenToKayit'ta)
        private const int SCATTER_PATTERN_FLAG = -10;

        // === Static state — OnDestroy'da reset (03 kontaminasyon güvenliği) ===
        private static string _aktifPattern = "";
        private static int _spinIdx = 0;
        private static bool _motorAktif = false;
        private static bool _loopAktif = false;
        // HOTFIX (Bug 2-4): RNG kaydı ToplamHamKazanc tesadüfen Tutorial hedefiyle eşleşince
        // idempotent check skip ediyordu → motor inject olmuyordu. Çözüm: reference tracking.
        private static SpinSimulasyonKaydi _sonInjekteEttigimKayit;

        // PAKET 14-FAZ28: Race condition fix — DesenToKayit enjekte edilene kadar SpinKilitli=true.
        // PatternBaslat'ta false reset, Update polling _kayitField.SetValue sonrası true set.
        // TutorialAdminEnjeksiyonu Update polling parametreTamam hesabına bu flag eklenir;
        // pattern motor aktif AMA enjekte henüz tamamlanmadıysa kullanıcı spin tıklayamaz.
        public static bool KayitEnjekteEdildi { get; private set; } = false;
        public static bool MotorAktif => _motorAktif;

        // PAKET 6C3: Dinamik pattern state (T7 Kazandırma + T9 Near Miss — 5'de N mantığı)
        private static int _dinamikN = 3;
        private static string _dinamikMod = "";          // "kazandirma" / "nearMiss" / "odeme"
        private static SpinDesen[] _dinamikPattern;      // cache (her PatternBaslat'ta yeniden üretilir)
        // PAKET 14-FAZ2: T7 Ödeme dinamik aralık (min/maks bahis çarpanı, panel.html minCarpan/maksCarpan)
        private static float _dinamikMin = 0f;
        private static float _dinamikMaks = 0f;

        // System.Random instance (Unity Random.seed'e DOKUNMAZ — 03'e sızmaz)
        // PAKET 14-FAZ27: readonly KALDIRILDI — RngResetle() public method ile reset için.
        private static System.Random _rng = new System.Random(12345);

        // PAKET 14-FAZ27: Her Tutorial spin başında RNG state'i sıfırla (deterministik garanti).
        // Pattern motor _rng tüketimi sıralı → her spin AYNI seed konumundan başlar → AYNI dolgu meyveleri,
        // AYNI cluster pozisyonları, AYNI çarpan değerleri. Tüm oyuncular AYNI Tutorial deneyimi yaşar.
        public static void RngResetle()
        {
            _rng = new System.Random(12345);
        }

        // Reflection cache
        private OyunYoneticisi _oy;
        private FieldInfo _kayitField;
        private FieldInfo _hazirField;

        [Preserve]
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OtomatikInit()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            var aktifSahne = SceneManager.GetActiveScene();
            if (aktifSahne.buildIndex == BUILD_INDEX)
                OnSceneLoaded(aktifSahne, LoadSceneMode.Single);
        }

        [Preserve]
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.buildIndex != BUILD_INDEX)
            {
                if (Ornek != null) Object.Destroy(Ornek.gameObject);
                return;
            }
            if (Ornek != null) return;
            var go = new GameObject(nameof(TutorialSenaryoMotoru));
            go.AddComponent<TutorialSenaryoMotoru>();
        }

        private void Awake()
        {
            if (SceneManager.GetActiveScene().buildIndex != BUILD_INDEX)
            {
                gameObject.SetActive(false);
                return;
            }
            if (Ornek != null && Ornek != this) { Destroy(gameObject); return; }
            Ornek = this;

            _kayitField = typeof(OyunYoneticisi).GetField("_oncedenHesaplananKayit",
                BindingFlags.NonPublic | BindingFlags.Instance);
            _hazirField = typeof(OyunYoneticisi).GetField("_oncedenHesaplananHazir",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (_kayitField == null || _hazirField == null)
                Debug.LogError("[TutorialSenaryoMotoru] Reflection field bulunamadı — motor devre dışı kalacak.");

            // Önceki tutorial oturumundan kalıntı reset
            _aktifPattern = "";
            _spinIdx = 0;
            _motorAktif = false;
            _loopAktif = false;
            KayitEnjekteEdildi = false;

            Debug.Log("[TutorialSenaryoMotoru] Awake — reflection cache + static state reset.");
        }

        private void OnDestroy()
        {
            // ★ 03 KONTAMİNASYON GÜVENLİK: tüm static state RESET
            if (Ornek == this) Ornek = null;
            _aktifPattern = "";
            _spinIdx = 0;
            _motorAktif = false;
            _loopAktif = false;
            _sonInjekteEttigimKayit = null;
            KayitEnjekteEdildi = false;
            Debug.Log("[TutorialSenaryoMotoru] OnDestroy — tüm static state temizlendi.");
        }

        // === Public API — TutorialAdminEnjeksiyonu ve TutorialOyunYoneticisi'den çağrılır ===

        /// <summary>panel.html "oyunModu" event geldiğinde çağır. mod="hook"/"yontma"/"tutma"/"koruma"/"normal".</summary>
        public static void PatternBaslat(string mod)
        {
            if (string.IsNullOrEmpty(mod) || !_patternlar.ContainsKey(mod))
            {
                _aktifPattern = "";
                _motorAktif = false;
                _spinIdx = 0;
                KayitEnjekteEdildi = false;
                Debug.Log($"[TutorialSenaryoMotoru] Pattern '{mod}' tanımsız → motor pasif (RNG akışı).");
                return;
            }
            _aktifPattern = mod;
            _spinIdx = 0;
            _motorAktif = true;
            // HOTFIX: Yarış güvencesi — önceki adımdan kalan referansı temizle
            _sonInjekteEttigimKayit = null;
            // PAKET 14-FAZ28: Enjekte henüz yapılmadı → SpinKilitli=true (Update polling yapana kadar)
            KayitEnjekteEdildi = false;

            // Cache temizle ki sonraki precompute Tutorial pattern ile dolsun (override)
            var oy = Object.FindObjectOfType<OyunYoneticisi>();
            oy?.ScriptedSenaryoCacheTazele();

            Debug.Log($"[TutorialSenaryoMotoru] Pattern '{mod}' başladı — {_patternlar[mod].Length} spin. KayitEnjekteEdildi=false (race fix).");
        }

        /// <summary>ButtonCevir click sonrası çağır (kullanıcı SPIN'i tükettiği anda spinIdx ilerlesin).</summary>
        public static void SpinTamamlandi()
        {
            if (!_motorAktif) return;
            _spinIdx++;
            _sonInjekteEttigimKayit = null; // HOTFIX: tüketildi, sonraki Update yeni desen inject etsin
            // PAKET 14-FAZ28: Sonraki spin için yeni inject bekle → kullanıcı tıklama spin'ler arası kilitli
            KayitEnjekteEdildi = false;
            Debug.Log($"[TutorialSenaryoMotoru] SpinTamamlandi → spinIdx={_spinIdx}, KayitEnjekteEdildi=false (sonraki spin için inject bekle)");
        }

        /// <summary>T_SON sonrası serbest test modu — pattern loop'a girer.</summary>
        public static void LoopModaGec()
        {
            _loopAktif = true;
            _spinIdx = 0;
            Debug.Log("[TutorialSenaryoMotoru] Serbest test loop modu aktif.");
        }

        public static void Durdur()
        {
            _aktifPattern = "";
            _motorAktif = false;
            _loopAktif = false;
            _spinIdx = 0;
            KayitEnjekteEdildi = false;
            Debug.Log("[TutorialSenaryoMotoru] Motor tamamen durduruldu.");
        }

        // PAKET 6C3: Dinamik pattern başlat (T7 Kazandırma + T9 Near Miss)
        // mod: "kazandirma" → N adet küçük kazanç + (5-N) kayıp (KAZANÇLAR BAŞTA, deterministik sıra)
        // mod: "nearMiss"  → N adet 7-sembol near miss + (5-N) kayıp (NEAR MISS'LER BAŞTA)
        public static void DinamikPatternBaslat(string mod, int n)
        {
            _dinamikMod = mod;
            _dinamikN = Mathf.Clamp(n, 0, 5);
            _dinamikPattern = UretDinamikDesenler(_dinamikMod, _dinamikN);
            _aktifPattern = "dinamik";
            _spinIdx = 0;
            _motorAktif = true;
            _sonInjekteEttigimKayit = null;
            // PAKET 14-FAZ28: Enjekte henüz yapılmadı → SpinKilitli=true
            KayitEnjekteEdildi = false;

            var oy = Object.FindObjectOfType<OyunYoneticisi>();
            oy?.ScriptedSenaryoCacheTazele();

            Debug.Log($"[TutorialSenaryoMotoru] Dinamik pattern başladı: mod={mod}, N={_dinamikN}/5, desen sayısı={_dinamikPattern.Length}");
        }

        // PAKET 14-FAZ2: T7 Ödeme aralığı dinamik pattern başlat.
        // 5 spin × paytable taramasından random (sembolId, adet) — payCoef ∈ [minBahisCarpani, maksBahisCarpani].
        // Aralık dışı kombinasyon yoksa o spin için kayıp deseni.
        public static void DinamikOdemePatternBaslat(float minBahisCarpani, float maksBahisCarpani)
        {
            _dinamikMod = "odeme";
            _dinamikMin = minBahisCarpani;
            _dinamikMaks = maksBahisCarpani;
            _dinamikPattern = UretOdemeDesenler(minBahisCarpani, maksBahisCarpani);
            _aktifPattern = "dinamik";
            _spinIdx = 0;
            _motorAktif = true;
            _sonInjekteEttigimKayit = null;
            // PAKET 14-FAZ28: Enjekte henüz yapılmadı → SpinKilitli=true
            KayitEnjekteEdildi = false;

            var oy = Object.FindObjectOfType<OyunYoneticisi>();
            oy?.ScriptedSenaryoCacheTazele();

            Debug.Log($"[TutorialSenaryoMotoru] Ödeme dinamik pattern başladı: aralık=[{minBahisCarpani:F1}-{maksBahisCarpani:F1}]× bahis, {_dinamikPattern.Length} spin");
        }

        private static SpinDesen[] UretDinamikDesenler(string mod, int n)
        {
            // PAKET 14-FAZ2: shuffle KALDIRILDI. Kullanıcı slider'da N seçince N kazanç BAŞTA,
            // (5-N) kayıp SONDA ardışık çıksın → "5'de 3 → K/K/K/x/x" deterministik pedagoji.
            var liste = new List<SpinDesen>();
            int kazancAdedi = Mathf.Clamp(n, 0, 5);
            int kayipAdedi = 5 - kazancAdedi;

            if (mod == "kazandirma")
            {
                // N kazanç desen (500/300/600 TL random tip — paytable uyumlu küçük kazançlar)
                for (int i = 0; i < kazancAdedi; i++)
                {
                    int tip = _rng.Next(3);
                    switch (tip)
                    {
                        case 0: liste.Add(new SpinDesen { sembolId = 3, adet = 8 }); break; // x8-9[3]=0.5 → 500 TL
                        case 1: liste.Add(new SpinDesen { sembolId = 1, adet = 8 }); break; // x8-9[1]=0.3 → 300 TL
                        case 2: liste.Add(new SpinDesen { sembolId = 4, adet = 8 }); break; // x8-9[4]=0.6 → 600 TL
                    }
                }
                for (int i = 0; i < kayipAdedi; i++)
                    liste.Add(new SpinDesen { sembolId = -1, adet = 0 });
            }
            else if (mod == "nearMiss")
            {
                // N near-miss desen (7 sembol — cluster eşik 8'in 1 altı, "neredeyse")
                for (int i = 0; i < kazancAdedi; i++)
                {
                    int sym = _rng.Next(0, 7); // 0-6 normal meyve (scatter 7-8 hariç)
                    liste.Add(new SpinDesen { sembolId = sym, adet = 7 });
                }
                for (int i = 0; i < kayipAdedi; i++)
                    liste.Add(new SpinDesen { sembolId = -1, adet = 0 });
            }

            return liste.ToArray();
        }

        // PAKET 14-FAZ2: T7 Ödeme — paytable taramasıyla [min, maks] aralığında 5 spin üret.
        // Her spin: aralıkta olan tüm (sym 0..6, adet ∈ {8,9,10,11,12}) kombinasyonları → random seç.
        // Aralık dışı (paytable hiç eşleşmiyor) → kayıp deseni.
        private static SpinDesen[] UretOdemeDesenler(float minCarpan, float maksCarpan)
        {
            var liste = new List<SpinDesen>(5);
            var oy = Object.FindObjectOfType<OyunYoneticisi>();
            var ta = oy != null ? oy.tumbleAyarlari : null;
            if (ta == null || ta.PayTable_8_9 == null)
            {
                for (int i = 0; i < 5; i++)
                    liste.Add(new SpinDesen { sembolId = -1, adet = 0 });
                return liste.ToArray();
            }

            int sembolSayisi = ta.PayTable_8_9.Length;
            int scatterIdx = ta.ScatterIndex;
            int[] adetler = { 8, 9, 10, 11, 12 };

            // Aday kombinasyon havuzu (payCoef aralık içinde olanlar)
            var adaylar = new List<SpinDesen>();
            for (int sym = 0; sym < sembolSayisi; sym++)
            {
                if (sym == scatterIdx) continue;
                foreach (int adet in adetler)
                {
                    float payCoef = ta.GetPayForCount(sym, adet);
                    if (payCoef >= minCarpan && payCoef <= maksCarpan)
                        adaylar.Add(new SpinDesen { sembolId = sym, adet = adet });
                }
            }

            if (adaylar.Count == 0)
            {
                Debug.LogWarning($"[TutorialSenaryoMotoru] Ödeme aralığı [{minCarpan}-{maksCarpan}] paytable'da eşleşmedi → 5 kayıp");
                for (int i = 0; i < 5; i++)
                    liste.Add(new SpinDesen { sembolId = -1, adet = 0 });
                return liste.ToArray();
            }

            for (int i = 0; i < 5; i++)
                liste.Add(adaylar[_rng.Next(adaylar.Count)]);

            return liste.ToArray();
        }

        // === Update polling — _hazir=true ise üzerine Tutorial kayıt yaz ===

        private void Update()
        {
            if (!_motorAktif)
            {
                // PAKET 6C2-EXT: T6YO aktifken motor pasifse cache bypass uyarısı (saniyede 1).
                // Beklenen davranış: T6YO branch'i PatternBaslat("yeniOyuncu_kapali") çağırmış olmalı.
                if (Time.frameCount % 60 == 0)
                {
                    var ayDbg = TutorialOyunYoneticisi.Ornek?.AdimYoneticisi;
                    if (ayDbg != null && ayDbg.mevcutAdim == TutorialAdimYoneticisi.TutorialAdimId.T6_YENI_OYUNCU)
                        Debug.LogWarning($"[TutorialSenaryoMotoru] T6YO AKTİF ama motor PASİF — pattern enjekte edilmiyor (_aktifPattern='{_aktifPattern}', _spinIdx={_spinIdx}). Bakiye RNG akışıyla artıyor olabilir.");
                }
                return;
            }
            if (_kayitField == null || _hazirField == null) return;

            if (_oy == null)
            {
                _oy = Object.FindObjectOfType<OyunYoneticisi>();
                if (_oy == null) return;
            }

            // PAKET 6C3: dinamik pattern branch (T7/T9 — runtime üretilen, _dinamikPattern cache)
            SpinDesen[] pattern;
            if (_aktifPattern == "dinamik")
            {
                pattern = _dinamikPattern;
                if (pattern == null || pattern.Length == 0) return;
            }
            else if (!_patternlar.TryGetValue(_aktifPattern, out pattern))
            {
                return;
            }

            int idx;
            if (_loopAktif)
            {
                idx = _spinIdx % pattern.Length;
            }
            else
            {
                if (_spinIdx >= pattern.Length)
                {
                    // PAKET 14-FAZ28: Pattern tükendi → motor pasif (SpinKilitli check geçsin).
                    // Aksi halde MotorAktif=true + KayitEnjekteEdildi=false → SpinKilitli sürekli kalır.
                    if (_motorAktif)
                    {
                        _motorAktif = false;
                        KayitEnjekteEdildi = true; // motor pasif → kontrol atlanır, spin açık
                        Debug.Log($"[TutorialSenaryoMotoru] Pattern '{_aktifPattern}' tükendi → motor pasif, SpinKilitli serbest.");
                    }
                    return;
                }
                idx = _spinIdx;
            }

            var desen = pattern[idx];

            // HOTFIX: Reference tracking — bizim inject ettiğimiz kayıt hâlâ orada mı?
            // RNG cache ToplamHamKazanc'ı Tutorial hedefiyle tesadüfen eşleşse bile artık skip etmez.
            bool hazir = (bool)_hazirField.GetValue(_oy);
            var mevcutKayit = _kayitField.GetValue(_oy) as SpinSimulasyonKaydi;
            if (hazir && mevcutKayit != null && System.Object.ReferenceEquals(mevcutKayit, _sonInjekteEttigimKayit))
            {
                // PAKET 14-FAZ28: Kayıt zaten bizim → flag idempotent true (race koruması)
                if (!KayitEnjekteEdildi) KayitEnjekteEdildi = true;
                return;
            }

            var yeniKayit = DesenToKayit(desen);
            if (yeniKayit == null) return;

            _kayitField.SetValue(_oy, yeniKayit);
            _hazirField.SetValue(_oy, true);
            _sonInjekteEttigimKayit = yeniKayit;
            // PAKET 14-FAZ28: Enjekte tamamlandı → SpinKilitli serbest (TutorialAdminEnjeksiyonu check geçer)
            KayitEnjekteEdildi = true;

            Debug.Log($"[TutorialSenaryoMotoru] Spin enjekte (loop={_loopAktif}): pattern={_aktifPattern}, " +
                      $"idx={idx}/{pattern.Length - 1}, sembol={desen.sembolId}, adet={desen.adet}, " +
                      $"hedefKazanc={yeniKayit.ToplamHamKazanc} TL → KayitEnjekteEdildi=true");
        }

        // === Heuristic: mevcut kayıt bizim ürettiğimiz Tutorial kayıt mı? ===

        private bool KayitDeseneUygunMu(SpinSimulasyonKaydi kayit, SpinDesen desen)
        {
            int hedef = HedefKazancTL(desen);
            return kayit.ToplamHamKazanc == hedef
                && kayit.NihaiCarpanToplam == 1
                && !kayit.ZorlaCarpanKullanildi;
        }

        private int HedefKazancTL(SpinDesen desen)
        {
            if (desen.sembolId < 0) return 0;
            if (_oy == null || _oy.tumbleAyarlari == null) return 0;
            float payCoef = _oy.tumbleAyarlari.GetPayForCount(desen.sembolId, desen.adet);
            int bahis = _oy.BotIcinBahis;
            return Mathf.RoundToInt(payCoef * bahis);
        }

        // === DesenToKayit — SpinSimulasyonKaydi üretim ===

        private SpinSimulasyonKaydi DesenToKayit(SpinDesen desen)
        {
            if (_oy == null || _oy.tumbleAyarlari == null) return null;

            var ta = _oy.tumbleAyarlari;
            const int SUTUN = 6;
            const int SATIR = 5;
            int sembolSayisi = ta.PayTable_8_9 != null ? ta.PayTable_8_9.Length : 9;
            int scatterIdx = ta.ScatterIndex;

            var kayit = new SpinSimulasyonKaydi
            {
                Sutun = SUTUN,
                Satir = SATIR,
                IlkGrid = new int[SUTUN, SATIR],
                IlkCarpanGrid = new int[SUTUN, SATIR],
                NihaiCarpanToplam = 1,
                ZorlaCarpanKullanildi = false,
                CarpanKacti = false,
                SenaryoOdemeBandinaUygun = true
            };

            // PAKET 6C1 — Scatter pattern (T5 bonus testi): 'adet' kadar scatter + dolgu meyve, cluster yok
            // PAKET 14-FAZ29: scatterPozlari varsa RNG'siz sabit yerleştirme
            if (desen.sembolId == SCATTER_PATTERN_FLAG)
            {
                DolduScatterPattern(kayit.IlkGrid, SUTUN, SATIR, sembolSayisi, scatterIdx, desen.adet, desen.scatterPozlari);
                kayit.ToplamHamKazanc = 0;
                Debug.Log($"[TutorialSenaryoMotoru] Scatter pattern: {desen.adet} scatter yerleştirildi → bonus tetiklenmeli");
                return kayit;
            }

            if (desen.sembolId < 0)
            {
                // KAYIP grid — cluster yok, scatter yok, Adimlar boş
                DolduClusterSiz(kayit.IlkGrid, SUTUN, SATIR, sembolSayisi, scatterIdx);
                kayit.ToplamHamKazanc = 0;
                // PAKET 6D — T11 aşama 1: çarpan zorla AMA cluster yok → ödeme 0 ama görsel çarpan düşer
                if (desen.carpanZorla && desen.carpanDeger > 0)
                {
                    EnjekteCarpanZorla(kayit, SUTUN, SATIR, desen.carpanDeger, hedefSym: -1);
                    Debug.Log($"[TutorialSenaryoMotoru] CARPAN ZORLA (kapalı ödeme): {desen.carpanDeger}x düştü, cluster yok, ödeme=0");
                }
                return kayit;
            }

            // PAKET 6C3 — NEAR MISS: 7 aynı sembol (cluster eşik 8'in 1 altı). Hiç patlama yok,
            // pedagojik vurgu "1 sembol daha eksik = neredeyse kazanıyordum". Adimlar boş.
            if (desen.adet == 7)
            {
                var nmPozlari = new List<Vector2Int>(7);
                DolduCluster(kayit.IlkGrid, SUTUN, SATIR, sembolSayisi, scatterIdx,
                             desen.sembolId, 7, nmPozlari);
                kayit.ToplamHamKazanc = 0;
                Debug.Log($"[TutorialSenaryoMotoru] NEAR MISS: 7 adet sym={desen.sembolId} yerleştirildi (cluster yok)");
                return kayit;
            }

            // PAKET 13-FIX-A — Limit-aware kontrol: hedef kazanç admin maxOdeme'sini aşıyorsa cluster
            // OLUŞTURMA, kayıp grid üret. Pedagojik tutarlılık (yeniOyuncu modu vb. düşük limit ile pattern
            // hedefleri çakışırsa görsel "cluster düştü ama ödeme yok" karışıklığı oluşmasın).
            if (desen.adet >= 8 && _oy != null && _oy.tumbleAyarlari != null)
            {
                float onPayCoef = ta.GetPayForCount(desen.sembolId, desen.adet);
                int turKazanciOnHesap = Mathf.RoundToInt(onPayCoef * _oy.BotIcinBahis);
                int maxOdeme = _oy.GetAdminMaxOdeme();
                // PAKET 14-FAZ10 (debug): Tutorial T6YO için pattern hedef vs limit ilişkisini görünür yap.
                Debug.Log($"[TutorialSenaryoMotoru] LIMIT CHECK: pattern={_aktifPattern}, idx={_spinIdx}, sym={desen.sembolId}, adet={desen.adet}, payCoef={onPayCoef:F2}, hedef={turKazanciOnHesap} TL, maxOdeme={maxOdeme} TL, aşar={(maxOdeme > 0 && turKazanciOnHesap > maxOdeme)}");
                if (maxOdeme > 0 && turKazanciOnHesap > maxOdeme)
                {
                    DolduClusterSiz(kayit.IlkGrid, SUTUN, SATIR, sembolSayisi, scatterIdx);
                    kayit.ToplamHamKazanc = 0;
                    Debug.LogWarning($"[TutorialSenaryoMotoru] Pattern hedef {turKazanciOnHesap} TL > maxOdeme {maxOdeme} TL → kayıp grid (limit-aware)");
                    return kayit;
                }
            }

            // KAZANÇ grid — desen.sembolId'den 'adet' kadar + cluster yok dolgu
            var clusterPozlari = new List<Vector2Int>(desen.adet);
            DolduCluster(kayit.IlkGrid, SUTUN, SATIR, sembolSayisi, scatterIdx,
                         desen.sembolId, desen.adet, clusterPozlari);

            // PAKET 6C1 — Çarpan enjeksiyonu (T4+ adımlarda, panel.html'den ayar geçerli)
            // T3_*'de DEVRE DIŞI — T3 senaryo davranışı bozulmasın (oyunModu == hook/yontma/tutma/koruma)
            // HOTFIX: TutorialAdimYoneticisi singleton yok → TutorialOyunYoneticisi.Ornek üzerinden erişim
            //         field adı carpanUretimiAktif (extra "i")
            var ay = TutorialOyunYoneticisi.Ornek?.AdimYoneticisi;
            bool carpanIzinli = ay != null
                                && (int)ay.mevcutAdim >= (int)TutorialAdimYoneticisi.TutorialAdimId.T4;
            // PAKET 14-FAZ25 (debug): Çarpan enjekte branch koşulları her spin için görünür
            Debug.Log($"[T4 DEBUG] mevcutAdim={ay?.mevcutAdim}, carpanIzinli={carpanIzinli}, carpanUretimiAktif={_oy.carpanUretimiAktif}, carpanUretimOlasiligi={_oy.carpanUretimOlasiligi:F2}, maxCarpanAdedi={_oy.maxCarpanAdedi}");
            if (carpanIzinli && _oy.carpanUretimiAktif)
            {
                const int CARPAN_SEMBOL_LOCAL = -2; // OyunYoneticisi.CARPAN_SEMBOL ile aynı
                var clusterSet = new HashSet<Vector2Int>(clusterPozlari);

                // PAKET 14-FAZ29: SABİT çarpan path — pattern'de carpanPozlari/Degerleri varsa RNG'siz yerleştir.
                bool sabitCarpanVar = desen.carpanPozlari != null && desen.carpanPozlari.Length > 0
                                       && desen.carpanDegerleri != null && desen.carpanDegerleri.Length > 0;
                if (sabitCarpanVar)
                {
                    int yerlesenSabit = 0;
                    int n = Mathf.Min(desen.carpanPozlari.Length, desen.carpanDegerleri.Length);
                    for (int i = 0; i < n; i++)
                    {
                        var p = desen.carpanPozlari[i];
                        if (p.x < 0 || p.x >= SUTUN || p.y < 0 || p.y >= SATIR) continue;
                        if (clusterSet.Contains(p)) continue; // cluster üstüne yazma
                        int v = desen.carpanDegerleri[i];
                        if (v <= 0) continue;
                        kayit.IlkGrid[p.x, p.y] = CARPAN_SEMBOL_LOCAL;
                        kayit.IlkCarpanGrid[p.x, p.y] = v;
                        kayit.IlkCarpanDegerleri.Add(v);
                        yerlesenSabit++;
                    }
                    Debug.Log($"[TutorialSenaryoMotoru] SABİT çarpan yerleştirildi (RNG'siz): {yerlesenSabit} adet");
                }
                else
                {
                    float carpanOlasilik = _oy.carpanUretimOlasiligi;
                    int maxCarpan = Mathf.Max(1, _oy.maxCarpanAdedi);
                    if (carpanOlasilik > 0.001f)
                    {
                        int[] dogalHavuz = { 2, 3, 5, 8, 10 }; // Fields.cs:557 doğal havuz {2,3,5,8,10}

                        // Dolgu hücrelerini shuffle ile gez
                        var adaylar = new List<Vector2Int>();
                        for (int y = 0; y < SATIR; y++)
                            for (int x = 0; x < SUTUN; x++)
                                if (!clusterSet.Contains(new Vector2Int(x, y)))
                                    adaylar.Add(new Vector2Int(x, y));
                        for (int i = adaylar.Count - 1; i > 0; i--)
                        {
                            int j = _rng.Next(0, i + 1);
                            var tmp = adaylar[i]; adaylar[i] = adaylar[j]; adaylar[j] = tmp;
                        }

                        int yerlesen = 0;
                        foreach (var p in adaylar)
                        {
                            if (yerlesen >= maxCarpan) break;
                            if (_rng.NextDouble() < carpanOlasilik)
                            {
                                kayit.IlkGrid[p.x, p.y] = CARPAN_SEMBOL_LOCAL;
                                int carpanDegeri = dogalHavuz[_rng.Next(dogalHavuz.Length)];
                                kayit.IlkCarpanGrid[p.x, p.y] = carpanDegeri;
                                kayit.IlkCarpanDegerleri.Add(carpanDegeri);
                                yerlesen++;
                            }
                        }
                        if (yerlesen > 0)
                            Debug.Log($"[TutorialSenaryoMotoru] Çarpan enjekte (RNG): olasilik={carpanOlasilik:F2}, max={maxCarpan}, yerlesen={yerlesen}");
                    }
                }

                // PAKET 9-FIX-A: NihaiCarpanToplam'ı IlkCarpanDegerleri toplamı ile güncelle.
                // Yoksa kayıt default 1 → ödeme katmanı çarpanı 1 alır, "500x13=500" yanlış hesap.
                if (kayit.IlkCarpanDegerleri.Count > 0)
                {
                    int toplamCarpan = 0;
                    foreach (var v in kayit.IlkCarpanDegerleri) toplamCarpan += v;
                    kayit.NihaiCarpanToplam = toplamCarpan;
                    Debug.Log($"[TutorialSenaryoMotoru] NihaiCarpanToplam={toplamCarpan} ({kayit.IlkCarpanDegerleri.Count} çarpan toplam)");
                }
            }

            // PAKET 11 — 03 REFERANS PIPELINE: Clone + patlayanlara yeni sembol yaz.
            // ScriptedSpinUygulayici.cs:87-118 stratejisinin birebir uygulaması:
            //   1. refillGrid = IlkGrid.Clone() → tüm 30 hücre kopyalanır, survivor'lar pozisyonda kalır
            //   2. Patlayan hücrelere SecDolguSembol ile yeni meyve yaz (cluster oluşturmasın)
            //   3. yeniSpawn = patlayan pozisyonlar → CokmeAkisServisi üstten düşme animasyonu uygular
            //   4. DusenHucreFrom/To boş kalır → gravity yok, survivor'lar yerinde sabit
            // Önceki sütun-bazlı survivor kayma mantığı survivor'ları yanlış y'lere taşıyordu (bug).
            int[,] refillGrid = (int[,])kayit.IlkGrid.Clone();
            var yeniSpawn = new List<Vector2Int>(clusterPozlari.Count);
            foreach (var p in clusterPozlari)
            {
                int newSym = SecDolguSembol(refillGrid, SUTUN, SATIR, sembolSayisi, scatterIdx,
                                             desen.sembolId, p.x, p.y);
                refillGrid[p.x, p.y] = newSym;
                yeniSpawn.Add(p);
            }

            // Paytable kazanç hesabı (READ ONLY — paytable'a YAZILMIYOR)
            float payCoef = ta.GetPayForCount(desen.sembolId, desen.adet);
            int turKazanci = Mathf.RoundToInt(payCoef * _oy.BotIcinBahis);

            // PAKET 11 — CarpanGridRefillSonrasi de Clone + patlayanları sıfırla. Survivor pozisyonlardaki
            // çarpanlar IlkCarpanGrid'den otomatik korunur. Patlayan hücrelerde zaten 0 (cluster dışı
            // enjeksiyon) ama defansif olarak sıfır set ediyoruz.
            int[,] carpanGridSonrasi = kayit.IlkCarpanGrid != null
                ? (int[,])kayit.IlkCarpanGrid.Clone()
                : new int[SUTUN, SATIR];
            foreach (var p in clusterPozlari)
                carpanGridSonrasi[p.x, p.y] = 0;

            var adim = new TumbleAdimKaydi
            {
                TurKazanci = turKazanci,
                GridRefillSonrasi = refillGrid,
                CarpanGridRefillSonrasi = carpanGridSonrasi
            };
            adim.PatlayanHucreler.AddRange(clusterPozlari);
            // 03 REFERANS: adim.DusenHucreFrom / DusenHucreTo bırakıldı boş (gravity kapalı).
            adim.YeniSpawnEdilenHucreler.AddRange(yeniSpawn);

            // PAKET 6D — T11 aşama 2: cluster KAZANÇ + büyük çarpan kombosu (carpanOdemeToggle açık)
            if (desen.carpanZorla && desen.carpanDeger > 0)
            {
                EnjekteCarpanZorla(kayit, SUTUN, SATIR, desen.carpanDeger, hedefSym: desen.sembolId);
                Debug.Log($"[TutorialSenaryoMotoru] CARPAN ZORLA (açık ödeme): {desen.carpanDeger}x + cluster sym={desen.sembolId} = mega kazanç");
            }

            kayit.Adimlar.Add(adim);
            kayit.ToplamHamKazanc = turKazanci;

            return kayit;
        }

        // PAKET 6D: T11 çarpan zorla — IlkCarpanGrid'e CARPAN_SEMBOL + değer yerleştir.
        // hedefSym=-1 (kayıp path): herhangi bir hücre (scatter hariç).
        // hedefSym>=0 (kazanç path): cluster hücrelerine yerleşmesin (cluster bozulmasın).
        private void EnjekteCarpanZorla(SpinSimulasyonKaydi kayit, int sutun, int satir, int carpanDeger, int hedefSym)
        {
            if (_oy == null || _oy.tumbleAyarlari == null) return;
            int scatterIdx = _oy.tumbleAyarlari.ScatterIndex;
            const int CARPAN_SEMBOL_LOCAL = -2;

            var adaylar = new List<Vector2Int>();
            for (int x = 0; x < sutun; x++)
                for (int y = 0; y < satir; y++)
                {
                    int s = kayit.IlkGrid[x, y];
                    if (s == scatterIdx) continue;
                    if (hedefSym >= 0 && s == hedefSym) continue;
                    adaylar.Add(new Vector2Int(x, y));
                }

            if (adaylar.Count == 0) return;
            var p = adaylar[_rng.Next(adaylar.Count)];
            kayit.IlkGrid[p.x, p.y] = CARPAN_SEMBOL_LOCAL;
            kayit.IlkCarpanGrid[p.x, p.y] = carpanDeger;
            kayit.IlkCarpanDegerleri.Add(carpanDeger);
        }

        // 30 hücreyi rastgele meyve ile doldur — hiçbir sembolden 8+ olmasın, scatter ASLA.
        private void DolduClusterSiz(int[,] grid, int sutun, int satir, int sembolSayisi, int scatterIdx)
        {
            int hucre = sutun * satir;
            const int MAX_PER_SEMBOL = 5; // 8'lik cluster oluşmaz (5 < 8)
            int[] sembolAdet = new int[sembolSayisi];

            for (int idx = 0; idx < hucre; idx++)
            {
                int x = idx % sutun;
                int y = idx / sutun;
                int sec = -1;
                for (int t = 0; t < sembolSayisi * 3; t++)
                {
                    int s = _rng.Next(0, sembolSayisi);
                    if (s == scatterIdx) continue;
                    if (sembolAdet[s] >= MAX_PER_SEMBOL) continue;
                    sec = s;
                    break;
                }
                if (sec < 0)
                {
                    for (int s = 0; s < sembolSayisi; s++)
                    {
                        if (s == scatterIdx) continue;
                        if (sembolAdet[s] < MAX_PER_SEMBOL + 2) { sec = s; break; }
                    }
                }
                if (sec < 0) sec = 0;
                grid[x, y] = sec;
                sembolAdet[sec]++;
            }
        }

        // 'adet' hücreye sembolId, kalanlara farklı semboller (cluster yok, scatter yok).
        private void DolduCluster(int[,] grid, int sutun, int satir, int sembolSayisi, int scatterIdx,
                                   int sembolId, int adet, List<Vector2Int> clusterPozlariOut)
        {
            int hucre = sutun * satir;

            var pozlar = new List<Vector2Int>(hucre);
            for (int y = 0; y < satir; y++)
                for (int x = 0; x < sutun; x++)
                    pozlar.Add(new Vector2Int(x, y));
            // Fisher-Yates shuffle
            for (int i = pozlar.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(0, i + 1);
                var tmp = pozlar[i]; pozlar[i] = pozlar[j]; pozlar[j] = tmp;
            }

            // İlk 'adet' poz → kazan sembol
            for (int i = 0; i < adet && i < pozlar.Count; i++)
            {
                grid[pozlar[i].x, pozlar[i].y] = sembolId;
                clusterPozlariOut.Add(pozlar[i]);
            }

            // Kalan → farklı semboller, max 5 per sembol (cluster yok)
            const int MAX_PER_DOLGU = 5;
            int[] sembolAdet = new int[sembolSayisi];
            sembolAdet[sembolId] = adet;
            for (int i = adet; i < pozlar.Count; i++)
            {
                int sec = -1;
                for (int t = 0; t < sembolSayisi * 3; t++)
                {
                    int s = _rng.Next(0, sembolSayisi);
                    if (s == scatterIdx || s == sembolId) continue;
                    if (sembolAdet[s] >= MAX_PER_DOLGU) continue;
                    sec = s;
                    break;
                }
                if (sec < 0)
                {
                    for (int s = 0; s < sembolSayisi; s++)
                    {
                        if (s == scatterIdx || s == sembolId) continue;
                        if (sembolAdet[s] < MAX_PER_DOLGU + 2) { sec = s; break; }
                    }
                }
                if (sec < 0) sec = (sembolId == 0) ? 1 : 0;
                grid[pozlar[i].x, pozlar[i].y] = sec;
                sembolAdet[sec]++;
            }
        }

        // PAKET 6C1: T5 scatter pattern — 'scatterAdet' kadar scatter yerleştir + kalanı meyve (cluster yok)
        // PAKET 14-FAZ29: sabitPozlar dolu ise RNG'siz yerleştir; null/boş ise eski Fisher-Yates yolu.
        // Dolgu meyveler her iki yolda da _rng ile (seed 12345, RngResetle ile deterministik).
        private void DolduScatterPattern(int[,] grid, int sutun, int satir, int sembolSayisi,
                                          int scatterIdx, int scatterAdet, Vector2Int[] sabitPozlar = null)
        {
            int hucre = sutun * satir;
            int adet = Mathf.Clamp(scatterAdet, 0, hucre);
            var scatterSet = new HashSet<Vector2Int>();

            if (sabitPozlar != null && sabitPozlar.Length > 0)
            {
                // SABİT yerleştirme — RNG'siz
                int yerlesen = 0;
                for (int i = 0; i < sabitPozlar.Length && yerlesen < adet; i++)
                {
                    var p = sabitPozlar[i];
                    if (p.x < 0 || p.x >= sutun || p.y < 0 || p.y >= satir) continue;
                    if (scatterSet.Contains(p)) continue; // duplicate koruma
                    grid[p.x, p.y] = scatterIdx;
                    scatterSet.Add(p);
                    yerlesen++;
                }
                Debug.Log($"[TutorialSenaryoMotoru] SABİT scatter yerleştirildi (RNG'siz): {yerlesen} pozisyon");
            }
            else
            {
                // RNG yolu (geriye dönük) — Fisher-Yates shuffle
                var pozlar = new List<Vector2Int>(hucre);
                for (int y = 0; y < satir; y++)
                    for (int x = 0; x < sutun; x++)
                        pozlar.Add(new Vector2Int(x, y));
                for (int i = pozlar.Count - 1; i > 0; i--)
                {
                    int j = _rng.Next(0, i + 1);
                    var tmp = pozlar[i]; pozlar[i] = pozlar[j]; pozlar[j] = tmp;
                }
                for (int i = 0; i < adet; i++)
                {
                    grid[pozlar[i].x, pozlar[i].y] = scatterIdx;
                    scatterSet.Add(pozlar[i]);
                }
            }

            // Dolgu meyveler — kalan hücreler _rng ile (cluster yok kuralı: MAX_PER_DOLGU=5)
            const int MAX_PER_DOLGU = 5;
            int[] sembolAdet = new int[sembolSayisi];
            sembolAdet[scatterIdx] = scatterSet.Count;
            for (int y = 0; y < satir; y++)
            {
                for (int x = 0; x < sutun; x++)
                {
                    if (scatterSet.Contains(new Vector2Int(x, y))) continue;
                    int sec = -1;
                    for (int t = 0; t < sembolSayisi * 3; t++)
                    {
                        int s = _rng.Next(0, sembolSayisi);
                        if (s == scatterIdx) continue;
                        if (sembolAdet[s] >= MAX_PER_DOLGU) continue;
                        sec = s; break;
                    }
                    if (sec < 0)
                    {
                        for (int s = 0; s < sembolSayisi; s++)
                        {
                            if (s == scatterIdx) continue;
                            if (sembolAdet[s] < MAX_PER_DOLGU + 2) { sec = s; break; }
                        }
                    }
                    if (sec < 0) sec = 0;
                    grid[x, y] = sec;
                    sembolAdet[sec]++;
                }
            }
        }

        // Refill için tek hücre seç: scatter+kazanSembol hariç, komşu cluster engelle (4-yön)
        private int SecDolguSembol(int[,] grid, int sutun, int satir, int sembolSayisi, int scatterIdx,
                                    int kazanSembol, int xx, int yy)
        {
            int[] dx = { -1, 1, 0, 0 };
            int[] dy = { 0, 0, -1, 1 };
            for (int t = 0; t < sembolSayisi * 3; t++)
            {
                int s = _rng.Next(0, sembolSayisi);
                if (s == scatterIdx || s == kazanSembol) continue;
                bool clash = false;
                for (int k = 0; k < 4; k++)
                {
                    int nx = xx + dx[k], ny = yy + dy[k];
                    if (nx < 0 || nx >= sutun || ny < 0 || ny >= satir) continue;
                    if (grid[nx, ny] == s) { clash = true; break; }
                }
                if (!clash) return s;
            }
            for (int s = 0; s < sembolSayisi; s++)
                if (s != scatterIdx && s != kazanSembol) return s;
            return 0;
        }
    }
}
