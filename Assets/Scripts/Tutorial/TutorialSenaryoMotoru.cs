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
            public int adet;      // 8-12 (8-9 → x8-9, 10-11 → x10-11, 12+ → x12+)
        }

        // Pattern dictionary — bahis=1000 TL, tumble YOK (tek paytable hit / tek tumble adımı)
        private static readonly Dictionary<string, SpinDesen[]> _patternlar = new()
        {
            // T3_HOOK: 5 spin — 2000/2500/2000/2500/3000 (toplam 12.000 TL — kanca etkisi)
            ["hook"] = new[]
            {
                new SpinDesen { sembolId = 5, adet = 10 },  // x10-11[5] = 2.0 → 2000 TL
                new SpinDesen { sembolId = 3, adet = 12 },  // x12+ [3] = 2.5 → 2500 TL
                new SpinDesen { sembolId = 5, adet = 10 },  // 2000 TL
                new SpinDesen { sembolId = 3, adet = 12 },  // 2500 TL
                new SpinDesen { sembolId = 4, adet = 12 },  // x12+ [4] = 3.0 → 3000 TL
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
            // T3_KORUMA: 5 spin — 0/300/0/200/0 (orijinal 330/250 paytable adımıyla tutturulamaz → revize)
            ["koruma"] = new[]
            {
                new SpinDesen { sembolId = -1, adet = 0 },
                new SpinDesen { sembolId = 1, adet = 8 },   // 300 TL (orijinal 330 → 300; pedagoji aynı)
                new SpinDesen { sembolId = -1, adet = 0 },
                new SpinDesen { sembolId = 0, adet = 8 },   // 200 TL (orijinal 250 → 200)
                new SpinDesen { sembolId = -1, adet = 0 },
            },
            // "normal" → bu dict'te yok → PatternBaslat motor pasif → RNG akışı
        };

        // === Static state — OnDestroy'da reset (03 kontaminasyon güvenliği) ===
        private static string _aktifPattern = "";
        private static int _spinIdx = 0;
        private static bool _motorAktif = false;
        private static bool _loopAktif = false;

        // System.Random instance (Unity Random.seed'e DOKUNMAZ — 03'e sızmaz)
        private static readonly System.Random _rng = new System.Random(12345);

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
                Debug.Log($"[TutorialSenaryoMotoru] Pattern '{mod}' tanımsız → motor pasif (RNG akışı).");
                return;
            }
            _aktifPattern = mod;
            _spinIdx = 0;
            _motorAktif = true;

            // Cache temizle ki sonraki precompute Tutorial pattern ile dolsun (override)
            var oy = Object.FindObjectOfType<OyunYoneticisi>();
            oy?.ScriptedSenaryoCacheTazele();

            Debug.Log($"[TutorialSenaryoMotoru] Pattern '{mod}' başladı — {_patternlar[mod].Length} spin.");
        }

        /// <summary>ButtonCevir click sonrası çağır (kullanıcı SPIN'i tükettiği anda spinIdx ilerlesin).</summary>
        public static void SpinTamamlandi()
        {
            if (!_motorAktif) return;
            _spinIdx++;
            Debug.Log($"[TutorialSenaryoMotoru] SpinTamamlandi → spinIdx={_spinIdx}");
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
            Debug.Log("[TutorialSenaryoMotoru] Motor tamamen durduruldu.");
        }

        // === Update polling — _hazir=true ise üzerine Tutorial kayıt yaz ===

        private void Update()
        {
            if (!_motorAktif) return;
            if (_kayitField == null || _hazirField == null) return;

            if (_oy == null)
            {
                _oy = Object.FindObjectOfType<OyunYoneticisi>();
                if (_oy == null) return;
            }

            if (!_patternlar.TryGetValue(_aktifPattern, out var pattern)) return;

            int idx;
            if (_loopAktif)
            {
                idx = _spinIdx % pattern.Length;
            }
            else
            {
                if (_spinIdx >= pattern.Length) return; // T3 pattern tükendi → motor sessiz
                idx = _spinIdx;
            }

            var desen = pattern[idx];

            // Idempotent: mevcut cache zaten bu desene aitse skip
            bool hazir = (bool)_hazirField.GetValue(_oy);
            var mevcutKayit = _kayitField.GetValue(_oy) as SpinSimulasyonKaydi;
            if (hazir && mevcutKayit != null && KayitDeseneUygunMu(mevcutKayit, desen)) return;

            var yeniKayit = DesenToKayit(desen);
            if (yeniKayit == null) return;

            _kayitField.SetValue(_oy, yeniKayit);
            _hazirField.SetValue(_oy, true);

            Debug.Log($"[TutorialSenaryoMotoru] Spin enjekte (loop={_loopAktif}): pattern={_aktifPattern}, " +
                      $"idx={idx}/{pattern.Length - 1}, sembol={desen.sembolId}, adet={desen.adet}, " +
                      $"hedefKazanc={yeniKayit.ToplamHamKazanc} TL");
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

            if (desen.sembolId < 0)
            {
                // KAYIP grid — cluster yok, scatter yok, Adimlar boş
                DolduClusterSiz(kayit.IlkGrid, SUTUN, SATIR, sembolSayisi, scatterIdx);
                kayit.ToplamHamKazanc = 0;
                return kayit;
            }

            // KAZANÇ grid — desen.sembolId'den 'adet' kadar + cluster yok dolgu
            var clusterPozlari = new List<Vector2Int>(desen.adet);
            DolduCluster(kayit.IlkGrid, SUTUN, SATIR, sembolSayisi, scatterIdx,
                         desen.sembolId, desen.adet, clusterPozlari);

            // Refill grid: kazanan hücreler farklı sembollerle (cluster yok)
            int[,] refillGrid = new int[SUTUN, SATIR];
            for (int x = 0; x < SUTUN; x++)
                for (int y = 0; y < SATIR; y++)
                    refillGrid[x, y] = kayit.IlkGrid[x, y];
            foreach (var p in clusterPozlari)
                refillGrid[p.x, p.y] = SecDolguSembol(refillGrid, SUTUN, SATIR, sembolSayisi, scatterIdx, desen.sembolId, p.x, p.y);

            // Paytable kazanç hesabı (READ ONLY — paytable'a YAZILMIYOR)
            float payCoef = ta.GetPayForCount(desen.sembolId, desen.adet);
            int turKazanci = Mathf.RoundToInt(payCoef * _oy.BotIcinBahis);

            var adim = new TumbleAdimKaydi
            {
                TurKazanci = turKazanci,
                GridRefillSonrasi = refillGrid,
                CarpanGridRefillSonrasi = new int[SUTUN, SATIR]
            };
            adim.PatlayanHucreler.AddRange(clusterPozlari);

            kayit.Adimlar.Add(adim);
            kayit.ToplamHamKazanc = turKazanci;

            return kayit;
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
