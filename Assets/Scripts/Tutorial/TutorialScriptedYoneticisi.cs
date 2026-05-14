using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using Senaryo.Scripted;

namespace KumarFarkindalik.Tutorial
{
    /// <summary>
    /// PAKET 14-FAZ33: Tutorial sahnesi (build idx 3) için ScriptedSpinUygulayici altyapısı kullanan singleton.
    /// 03 ScriptedSpinYoneticisi'nin Tutorial muadili — fakat build idx + state mapping farklı.
    ///
    /// Akış:
    /// 1. Sahne 3 yüklendiğinde GameObject otomatik yaratılır (RuntimeInitializeOnLoadMethod + sceneLoaded).
    /// 2. Awake'te TutorialAsamaListesiUreteci.UretMinimum() çağrılır → 4 pattern × 1 spin (T4+T5).
    /// 3. TutorialOyunYoneticisi.AdimDegisti içinde AsamaSet("carpanTest_100" vb.) çağrılır.
    /// 4. Kullanıcı spin tıkladığında OyunYoneticisi.Spin.cs Tutorial branch'i SonrakiSpiniAl(bonusSpin) çağırır.
    /// 5. ScriptedSpinKaydi → ScriptedSpinUygulayici.UygulaKaydi → SpinSimulasyonKaydi → grid render + ödeme.
    ///
    /// ADIM 1 SCOPE: Sadece T4+T5. Pattern tanımlı değilse Aktif=false döner → pattern motor (Faz 32) devreye girer.
    /// </summary>
    [Preserve]
    public class TutorialScriptedYoneticisi : MonoBehaviour
    {
        public const int BUILD_INDEX = 3;

        /// <summary>Bu pattern için ScriptedSpinKaydi listesi mevcut mu ve sonraki spin var mı? OyunYoneticisi.Spin.cs okur.</summary>
        public static bool Aktif { get; private set; }
        public static TutorialScriptedYoneticisi Ornek { get; private set; }

        /// <summary>PAKET 14-FAZ34.2 BUG D FIX: SonrakiSpiniAl çağrıldığında set edilir. TutorialOyunYoneticisi
        /// SpinTamamlandi handler bunu okuyup net = SonOdeme - bahis hesabı yapar (bakiye snapshot timing'i
        /// yerine deterministik kayıt-bazlı hesap → bar rengi her zaman doğru).</summary>
        public long SonOdeme { get; private set; } = 0;

        private Dictionary<string, List<ScriptedSpinKaydi>> _patternSpinleri;
        private string _aktifPattern = "";
        private int _spinIdx = 0;

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
            var go = new GameObject(nameof(TutorialScriptedYoneticisi));
            go.AddComponent<TutorialScriptedYoneticisi>();
        }

        private void Awake()
        {
            if (SceneManager.GetActiveScene().buildIndex != BUILD_INDEX)
            {
                Aktif = false;
                gameObject.SetActive(false);
                return;
            }
            if (Ornek != null && Ornek != this) { Destroy(gameObject); return; }
            Ornek = this;

            _patternSpinleri = TutorialAsamaListesiUreteci.UretMinimum();
            _aktifPattern = "";
            _spinIdx = 0;
            Aktif = false; // AsamaSet çağrılana kadar pasif → pattern motor (Faz 32) eski path çalışır

            Debug.Log($"[TutorialScriptedYoneticisi] Awake — {_patternSpinleri.Count} pattern hazır. Aktif=false (AsamaSet bekliyor).");
        }

        private void OnDestroy()
        {
            if (Ornek == this) Ornek = null;
            Aktif = false;
            _aktifPattern = "";
            _spinIdx = 0;
            Debug.Log("[TutorialScriptedYoneticisi] OnDestroy — state reset.");
        }

        /// <summary>
        /// TutorialOyunYoneticisi.AdimDegisti veya TutorialAdminEnjeksiyonu.AyarDegisti'den çağrılır.
        /// Pattern adı _patternSpinleri'nde varsa Aktif=true → OyunYoneticisi.Spin.cs Tutorial branch'i tetiklenir.
        /// Yoksa Aktif=false → pattern motor (TutorialSenaryoMotoru) eski path çalışır.
        /// </summary>
        public void AsamaSet(string patternAdi)
        {
            if (_patternSpinleri == null || !_patternSpinleri.ContainsKey(patternAdi))
            {
                Aktif = false;
                _aktifPattern = "";
                _spinIdx = 0;
                Debug.Log($"[TutorialScriptedYoneticisi] AsamaSet('{patternAdi}') — pattern bulunamadı → Aktif=false, pattern motor devreye girer.");
                return;
            }
            _aktifPattern = patternAdi;
            _spinIdx = 0;
            Aktif = true;

            // Pre-compute cache'i temizle ki sonraki spin Tutorial scripted kaydı oynatılsın.
            var oy = Object.FindObjectOfType<OyunYoneticisi>();
            oy?.ScriptedSenaryoCacheTazele();

            Debug.Log($"[TutorialScriptedYoneticisi] AsamaSet('{patternAdi}') — Aktif=true, {_patternSpinleri[patternAdi].Count} spin hazır.");
        }

        /// <summary>OyunYoneticisi.Spin.cs Tutorial branch'inden çağrılır. Sıradaki scripted kaydı döndürür.
        /// PAKET 14-FAZ33.1: idx ilerletmesi BURADA YAPILMAZ — pre-compute yeniden tetiklenirse aynı kayıt
        /// döner. Gerçek kullanıcı spin tamamlandığında <see cref="SpinTamamlandi"/> çağrılır → o zaman idx++.</summary>
        public ScriptedSpinKaydi SonrakiSpiniAl(bool bonusSpin)
        {
            // Bonus spin Tutorial'da T5 bonus pipeline'a giderse OyunYoneticisi BonusDongusu kendi handle eder.
            // Bu yöntemde sadece normal spin için scripted kayıt döndürürüz.
            if (bonusSpin) return null;
            if (!Aktif || string.IsNullOrEmpty(_aktifPattern)) return null;
            if (_patternSpinleri == null || !_patternSpinleri.ContainsKey(_aktifPattern)) return null;

            var liste = _patternSpinleri[_aktifPattern];
            if (_spinIdx >= liste.Count)
            {
                // Pattern tükenmiş (SpinTamamlandi son spinden sonra Aktif'i false yapmadıysa defansif)
                Aktif = false;
                Debug.Log($"[TutorialScriptedYoneticisi] Pattern '{_aktifPattern}' zaten tükenmiş (idx={_spinIdx}/{liste.Count}) → Aktif=false.");
                return null;
            }

            var kayit = liste[_spinIdx];
            // PAKET 14-FAZ34.3 BUG E FIX: SonOdeme = brutOdeme (ham) × NihaiCarpanToplam (çarpan SUM).
            // Önceki: SonOdeme = brutOdeme tek başına (T4'te 1000 ham = NET 0, NOTR mavi bar).
            // ScriptedSpinUygulayici formülü: nihai = ToplamHamKazanc × NihaiCarpanToplam.
            // T4 carpanTest_100: brutOdeme=1000 × çarpan SUM=5 = 5000 → NET +4000 → KAZANC yeşil ✓
            long carpanToplam = 0;
            if (kayit.ilkCarpanDegerleri != null)
                foreach (var v in kayit.ilkCarpanDegerleri) if (v > 0) carpanToplam += v;
            if (carpanToplam == 0) carpanToplam = 1; // çarpan yoksa multiplier=1
            SonOdeme = kayit.brutOdeme * carpanToplam;
            Debug.Log($"[TutorialScriptedYoneticisi] SonrakiSpiniAl: pattern={_aktifPattern}, idx={_spinIdx}/{liste.Count - 1}, brüt={kayit.brutOdeme} TL (idx ilerletilmedi — SpinTamamlandi bekliyor)");
            return kayit;
        }

        /// <summary>PAKET 14-FAZ33.1: Gerçek kullanıcı spin animasyonu bittiğinde TutorialOyunYoneticisi tarafından
        /// çağrılır. Pre-compute coroutine çağrılarından bağımsız olarak pattern idx'i sadece burada ilerler.
        /// Tüm spinler tükendiğinde Aktif=false → pattern motor fallback'i devralır.</summary>
        public void SpinTamamlandi()
        {
            // PAKET 14-FAZ34.6 BUG L FIX: SonOdeme stale değer kalıntısı temizle.
            // TutorialOyunYoneticisi net hesabı SpinTamamlandi öncesinde SonOdeme okudu zaten;
            // sonraki SonrakiSpiniAl yeni değer set edecek. Aktif=false ise fallback path kullanılır.
            SonOdeme = 0;

            if (!Aktif || string.IsNullOrEmpty(_aktifPattern)) return;
            if (_patternSpinleri == null || !_patternSpinleri.ContainsKey(_aktifPattern)) return;

            _spinIdx++;
            var liste = _patternSpinleri[_aktifPattern];
            if (_spinIdx >= liste.Count)
            {
                Aktif = false;
                Debug.Log($"[TutorialScriptedYoneticisi] SpinTamamlandi → idx={_spinIdx}/{liste.Count} TÜKENDI → Aktif=false (pattern motor devralır).");
            }
            else
            {
                Debug.Log($"[TutorialScriptedYoneticisi] SpinTamamlandi → idx={_spinIdx}/{liste.Count} (sonraki spin için scripted kayıt hazır).");
            }
        }

        /// <summary>PAKET 14-FAZ35.0: T6 Kazandırma Sıklığı — havuzdan N kazanç + (5-N) kayıp peş peşe.
        /// Eski tasarım (tek-tip kazanç + final Fisher-Yates) yerine 5'er elemanlı çeşitli havuzlar.
        /// Sıra deterministik: önce N kazanç bloğu (pedagojik mesaj: "kazandıkça umut"), sonra (5-N) kayıp bloğu
        /// ("ardından düşüş"). Her havuzu shuffle ederek oturumlar arası seçim çeşitlenir, sıra korunur.</summary>
        public void AsamaSetKazanmaSikligi(int N)
        {
            N = Mathf.Clamp(N, 0, 5);
            var kazancHavuz = TutorialAsamaListesiUreteci.UretKazancHavuzu(); // 5 farklı
            var kayipHavuz = TutorialAsamaListesiUreteci.UretKayipHavuzu();  // 5 farklı

            // Her havuzu Fisher-Yates karıştır → her oturumda hangi alt seçimlerin geleceği değişir,
            // ama N kazanç + (5-N) kayıp BLOK SIRASI korunur (final shuffle yok).
            for (int i = kazancHavuz.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var tmp = kazancHavuz[i]; kazancHavuz[i] = kazancHavuz[j]; kazancHavuz[j] = tmp;
            }
            for (int i = kayipHavuz.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var tmp = kayipHavuz[i]; kayipHavuz[i] = kayipHavuz[j]; kayipHavuz[j] = tmp;
            }

            var liste = new List<ScriptedSpinKaydi>(5);
            for (int i = 0; i < N; i++) liste.Add(kazancHavuz[i]);
            for (int i = 0; i < 5 - N; i++) liste.Add(kayipHavuz[i]);

            if (_patternSpinleri == null)
                _patternSpinleri = new Dictionary<string, List<ScriptedSpinKaydi>>();
            _patternSpinleri["kazanma"] = liste;
            _aktifPattern = "kazanma";
            _spinIdx = 0;
            Aktif = true;

            var oy = Object.FindObjectOfType<OyunYoneticisi>();
            oy?.ScriptedSenaryoCacheTazele();

            Debug.Log($"[TutorialScriptedYoneticisi] AsamaSetKazanmaSikligi(N={N}) — {N} kazanç + {5 - N} kayıp peş peşe (havuzdan seçim), Aktif=true.");
        }

        /// <summary>PAKET 14-FAZ34 İş 3: T8 Near Miss Sıklığı — N near-miss + (5-N) normal kayıp listesi + shuffle.
        /// Near miss: 7 bitişik Hindistan (cluster eşik 8'in altında, ödeme yok ama "neredeyse" hissi).
        /// Normal: UretKayipKayit dolgu grid (saf kayıp).</summary>
        public void AsamaSetNearMiss(int N)
        {
            N = Mathf.Clamp(N, 0, 5);
            var liste = new List<ScriptedSpinKaydi>(5);
            for (int i = 0; i < N; i++) liste.Add(TutorialAsamaListesiUreteci.UretNearMissKayit());
            for (int i = 0; i < 5 - N; i++) liste.Add(TutorialAsamaListesiUreteci.UretKayipKayit());

            // Fisher-Yates shuffle (UnityEngine.Random — her oturum farklı sıra)
            for (int i = liste.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var tmp = liste[i]; liste[i] = liste[j]; liste[j] = tmp;
            }

            if (_patternSpinleri == null)
                _patternSpinleri = new Dictionary<string, List<ScriptedSpinKaydi>>();
            _patternSpinleri["nearMiss"] = liste;
            _aktifPattern = "nearMiss";
            _spinIdx = 0;
            Aktif = true;

            var oy = Object.FindObjectOfType<OyunYoneticisi>();
            oy?.ScriptedSenaryoCacheTazele();

            Debug.Log($"[TutorialScriptedYoneticisi] AsamaSetNearMiss(N={N}) — {N} near-miss + {5 - N} normal shuffle, Aktif=true.");
        }

        /// <summary>PAKET 14-FAZ34 İş 4: T9 Kaçış Frenleme — N kayıp + 1 kazanç (başabaş) + (4-N) doldurma.
        /// SHUFFLE YOK — sıra önemli: önce kayıplar → kazanç → "tam kaçacaktım ama kazandım" pedagojisi.
        /// N max 3 (kullanıcı 4-5 girse de clamp 1-3).</summary>
        public void AsamaSetKacis(int N)
        {
            N = Mathf.Clamp(N, 1, 3);
            var liste = new List<ScriptedSpinKaydi>(4);
            for (int i = 0; i < N; i++) liste.Add(TutorialAsamaListesiUreteci.UretKayipKayit());
            liste.Add(TutorialAsamaListesiUreteci.UretKacisKazancKayit());
            for (int i = 0; i < 3 - N; i++) liste.Add(TutorialAsamaListesiUreteci.UretKayipKayit());

            if (_patternSpinleri == null)
                _patternSpinleri = new Dictionary<string, List<ScriptedSpinKaydi>>();
            _patternSpinleri["kacis"] = liste;
            _aktifPattern = "kacis";
            _spinIdx = 0;
            Aktif = true;

            var oy = Object.FindObjectOfType<OyunYoneticisi>();
            oy?.ScriptedSenaryoCacheTazele();

            Debug.Log($"[TutorialScriptedYoneticisi] AsamaSetKacis(N={N}) — {N} kayıp + 1 kazanç + {3 - N} doldurma (sıralı), Aktif=true.");
        }

        /// <summary>PAKET 14-FAZ34 İş 5: T7 Ödeme Aralığı — paytable_8_9 taraması ile minCarpan/maxCarpan
        /// aralığındaki (sembol,8-adet) kombinasyonlar pool oluştur → 5 random spin üret. Tumble: 1 cluster patlar.
        /// Aralık dışı kombinasyon yoksa 5 kayıp spin.</summary>
        public void AsamaSetOdemeAraligi(float minCarpan, float maksCarpan)
        {
            var oy = Object.FindObjectOfType<OyunYoneticisi>();
            var ta = oy != null ? oy.tumbleAyarlari : null;
            var liste = new List<ScriptedSpinKaydi>(5);

            if (ta == null || ta.PayTable_8_9 == null)
            {
                for (int i = 0; i < 5; i++) liste.Add(TutorialAsamaListesiUreteci.UretKayipKayit());
                Debug.LogWarning("[TutorialScriptedYoneticisi] AsamaSetOdemeAraligi: tumbleAyarlari NULL → 5 kayıp fallback.");
            }
            else
            {
                int sembolSayisi = ta.PayTable_8_9.Length;
                int scatterIdx = ta.ScatterIndex;
                var aday = new List<(int sembol, long brut)>();
                for (int s = 0; s < sembolSayisi; s++)
                {
                    if (s == scatterIdx) continue;
                    float payCoef = ta.PayTable_8_9[s];
                    if (payCoef >= minCarpan && payCoef <= maksCarpan)
                        aday.Add((s, (long)Mathf.RoundToInt(payCoef * 1000)));
                }

                if (aday.Count == 0)
                {
                    Debug.LogWarning($"[TutorialScriptedYoneticisi] AsamaSetOdemeAraligi [{minCarpan}-{maksCarpan}] paytable_8_9'da eşleşmedi → 5 kayıp.");
                    for (int i = 0; i < 5; i++) liste.Add(TutorialAsamaListesiUreteci.UretKayipKayit());
                }
                else
                {
                    for (int i = 0; i < 5; i++)
                    {
                        var sec = aday[Random.Range(0, aday.Count)];
                        liste.Add(TutorialAsamaListesiUreteci.UretOdemeKayit(sec.sembol, sec.brut));
                    }
                    Debug.Log($"[TutorialScriptedYoneticisi] AsamaSetOdemeAraligi: pool={aday.Count} sembol, 5 random spin üretildi.");
                }
            }

            if (_patternSpinleri == null)
                _patternSpinleri = new Dictionary<string, List<ScriptedSpinKaydi>>();
            _patternSpinleri["odeme"] = liste;
            _aktifPattern = "odeme";
            _spinIdx = 0;
            Aktif = true;

            oy?.ScriptedSenaryoCacheTazele();
            Debug.Log($"[TutorialScriptedYoneticisi] AsamaSetOdemeAraligi(min={minCarpan}, maks={maksCarpan}) — Aktif=true.");
        }

        // ─────────────────────────────────────────────────────────────────────────
        // PAKET 14-FAZ34 İş 7/8/9: T3 senaryolar + T6YO + T10 Çarpan Zorla AsamaSet metodları.
        // Tümünde SHUFFLE YOK — pedagojik sıra önemli.
        // ─────────────────────────────────────────────────────────────────────────

        public void AsamaSetHook()     => AsamaSetHardcoded("hook",    TutorialAsamaListesiUreteci.UretHookSpinleri());
        public void AsamaSetYontma()   => AsamaSetHardcoded("yontma",  TutorialAsamaListesiUreteci.UretYontmaSpinleri());
        public void AsamaSetTutma()    => AsamaSetHardcoded("tutma",   TutorialAsamaListesiUreteci.UretTutmaSpinleri());
        public void AsamaSetKoruma()   => AsamaSetHardcoded("koruma",  TutorialAsamaListesiUreteci.UretKorumaSpinleri());
        public void AsamaSetYeniOyuncuAcik()   => AsamaSetHardcoded("yeniOyuncu_acik",   TutorialAsamaListesiUreteci.UretYeniOyuncuAcikSpinleri());
        public void AsamaSetYeniOyuncuKapali() => AsamaSetHardcoded("yeniOyuncu_kapali", TutorialAsamaListesiUreteci.UretYeniOyuncuKapaliSpinleri());

        public void AsamaSetCarpanZorlaKapali()
            => AsamaSetHardcoded("carpanZorla_kapali", new List<ScriptedSpinKaydi> { TutorialAsamaListesiUreteci.UretCarpanZorlaKapaliKayit() });
        public void AsamaSetCarpanZorlaAcik()
            => AsamaSetHardcoded("carpanZorla_acik", new List<ScriptedSpinKaydi> { TutorialAsamaListesiUreteci.UretCarpanZorlaAcikKayit() });

        private void AsamaSetHardcoded(string ad, List<ScriptedSpinKaydi> liste)
        {
            if (_patternSpinleri == null)
                _patternSpinleri = new Dictionary<string, List<ScriptedSpinKaydi>>();
            _patternSpinleri[ad] = liste;
            _aktifPattern = ad;
            _spinIdx = 0;
            Aktif = true;

            var oy = Object.FindObjectOfType<OyunYoneticisi>();
            oy?.ScriptedSenaryoCacheTazele();

            Debug.Log($"[TutorialScriptedYoneticisi] AsamaSet '{ad}' — {liste.Count} spin (sıralı), Aktif=true.");
        }

        /// <summary>Pattern motoruna geri dönmek için (T_SON veya hata durumu). AsamaSet ile aynı sonuç ama explicit niyet.</summary>
        public void DeaktifEt()
        {
            Aktif = false;
            _aktifPattern = "";
            _spinIdx = 0;
            Debug.Log("[TutorialScriptedYoneticisi] DeaktifEt — pattern motor devralır.");
        }
    }
}
