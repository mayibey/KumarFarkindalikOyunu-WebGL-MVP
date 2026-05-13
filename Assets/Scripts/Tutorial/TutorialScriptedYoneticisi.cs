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
            Debug.Log($"[TutorialScriptedYoneticisi] SonrakiSpiniAl: pattern={_aktifPattern}, idx={_spinIdx}/{liste.Count - 1}, brüt={kayit.brutOdeme} TL (idx ilerletilmedi — SpinTamamlandi bekliyor)");
            return kayit;
        }

        /// <summary>PAKET 14-FAZ33.1: Gerçek kullanıcı spin animasyonu bittiğinde TutorialOyunYoneticisi tarafından
        /// çağrılır. Pre-compute coroutine çağrılarından bağımsız olarak pattern idx'i sadece burada ilerler.
        /// Tüm spinler tükendiğinde Aktif=false → pattern motor fallback'i devralır.</summary>
        public void SpinTamamlandi()
        {
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
