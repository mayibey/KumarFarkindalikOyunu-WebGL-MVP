using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

namespace Senaryo.Scripted
{
    /// <summary>
    /// Anlatıcı sahnesinde RNG'yi bypass edip scripted senaryoyu servis eden singleton.
    /// Sadece build index 2 (03_SenaryoluOyun) sahnesinde aktif olur. Diğer sahnelerde Aktif=false → mevcut RNG akışı korunur.
    ///
    /// Kullanım: <see cref="OyunYoneticisi"/>.SimuleEtVeKaydetImpl başında <see cref="Aktif"/> kontrolü yapılır;
    /// true ise <see cref="SonrakiSpiniAl"/> ile o aşama+spinNo için tanımlı kayıt alınır.
    /// </summary>
    [Preserve]
    public class ScriptedSpinYoneticisi : MonoBehaviour
    {
        /// <summary>03_SenaryoluOyun sahnesinin Build Settings index'i (Tools → Build Settings).</summary>
        public const int ANLATICI_SAHNE_BUILD_INDEX = 2;

        /// <summary>true sadece anlatıcı sahnesinde + asset başarıyla yüklendiğinde.</summary>
        public static bool Aktif { get; private set; }

        /// <summary>Sahnedeki tek instance. Aktif değilken null olabilir.</summary>
        public static ScriptedSpinYoneticisi Ornek { get; private set; }

        private ScriptedAsamaListesi _asamaListesi;

        /// <summary>
        /// Sahne yüklendikten sonra otomatik olarak GameObject yaratır (anlatıcı sahnesinde).
        /// Manuel sahne kurulumu gerekmez. Diğer sahnelerde hiçbir şey yapmaz.
        /// </summary>
        [Preserve]
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OtomatikInit()
        {
            Debug.Log("[ScriptedTANI] OtomatikInit ÇAĞRILDI — bootstrap; sceneLoaded event'e abone olunuyor.");
            // RuntimeInitializeOnLoadMethod WebGL'de SADECE bootstrap'ta tetikleniyor (sahne geçişlerinde
            // tekrar çağrılmıyor). Bu nedenle SceneManager.sceneLoaded event'ine abone oluyoruz: her
            // sahne yüklendiğinde OnSceneLoaded çağrılır → idx==2 ise scripted GameObject yaratılır.
            SceneManager.sceneLoaded -= OnSceneLoaded; // idempotent
            SceneManager.sceneLoaded += OnSceneLoaded;

            var aktifSahne = SceneManager.GetActiveScene();
            if (aktifSahne.buildIndex == ANLATICI_SAHNE_BUILD_INDEX)
            {
                Debug.Log("[ScriptedTANI] Bootstrap'ta zaten idx=2 → OnSceneLoaded çağrılıyor.");
                OnSceneLoaded(aktifSahne, LoadSceneMode.Single);
            }
        }

        [Preserve]
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[ScriptedTANI] OnSceneLoaded ÇAĞRILDI — idx={scene.buildIndex}, ad={scene.name}, beklenen idx={ANLATICI_SAHNE_BUILD_INDEX}");
            if (scene.buildIndex != ANLATICI_SAHNE_BUILD_INDEX)
            {
                Debug.Log($"[ScriptedTANI] Sahne uyumsuz → return. idx={scene.buildIndex}");
                return;
            }
            if (Ornek != null)
            {
                Debug.Log("[ScriptedTANI] Ornek zaten var → return.");
                return;
            }
            Debug.Log("[ScriptedTANI] Sahne eşleşti — GameObject yaratılıyor + AddComponent...");
            var go = new GameObject(nameof(ScriptedSpinYoneticisi));
            go.AddComponent<ScriptedSpinYoneticisi>();
            Debug.Log("[ScriptedTANI] OnSceneLoaded BAŞARILI → Awake çağrılacak.");
        }

        private void Awake()
        {
            Debug.Log("[ScriptedTANI] Awake() ÇAĞRILDI");
            int idx = SceneManager.GetActiveScene().buildIndex;
            if (idx != ANLATICI_SAHNE_BUILD_INDEX)
            {
                Debug.Log($"[ScriptedTANI] Awake idx uyumsuz → SetActive(false). idx={idx}");
                Aktif = false;
                gameObject.SetActive(false);
                return;
            }
            if (Ornek != null && Ornek != this)
            {
                Debug.Log("[ScriptedTANI] Awake Ornek başkası → Destroy.");
                Destroy(gameObject);
                return;
            }
            Ornek = this;
            Debug.Log("[ScriptedTANI] Awake Ornek atandı, Resources.Load çağrılıyor...");

            // Asset'i Awake'de yükle: OyunYoneticisi.Start IlkSpinPrecomputeGecikmeli'yi tetikleyene
            // kadar Aktif=true olmalı, yoksa ilk spin önbelleği RNG akışıyla doldurur.
            _asamaListesi = Resources.Load<ScriptedAsamaListesi>(ScriptedAsamaListesi.ResourcePath);
            if (_asamaListesi == null)
            {
                Debug.LogError($"[ScriptedTANI] Resources.Load NULL DÖNDÜ! path='{ScriptedAsamaListesi.ResourcePath}', tip=ScriptedAsamaListesi. Asset bulunamadı veya tip eşleşmedi.");
                Aktif = false;
                return;
            }
            Debug.Log($"[ScriptedTANI] Resources.Load OK. asamaSpinleri null mu? {(_asamaListesi.asamaSpinleri == null)}, count={_asamaListesi.asamaSpinleri?.Count ?? -1}");
            if (_asamaListesi.asamaSpinleri == null || _asamaListesi.asamaSpinleri.Count == 0)
            {
                Debug.LogError("[ScriptedTANI] Asset boş (asamaSpinleri null/0) — deserialize fail veya tip metadata bozuk olabilir.");
                Aktif = false;
                return;
            }

            Aktif = true;
            int toplam = 0;
            for (int i = 0; i < _asamaListesi.asamaSpinleri.Count; i++)
                toplam += _asamaListesi.asamaSpinleri[i]?.spinler?.Count ?? 0;
            Debug.Log($"[ScriptedTANI] BAŞARILI! Aşama={_asamaListesi.asamaSpinleri.Count}, toplam spin={toplam}. Aktif=true.");
        }

        private void Start()
        {
            if (!Aktif) return;
            // RNG önbelleği OyunYoneticisi.Start sırasında doldurulmuş olabilir; scripted aktif olunca temizleyip
            // yeniden precompute tetikle ki ilk spin scripted kaydı oynatılsın (görsel grid + ödeme uyumlu).
            var oy = FindObjectOfType<OyunYoneticisi>();
            if (oy != null)
            {
                oy.ScriptedSenaryoCacheTazele();
            }
            else
            {
                Debug.LogWarning("[ScriptedSpinYoneticisi] OyunYoneticisi bulunamadı; cache tazeleme atlandı (ilk spin RNG önbelleğinden gelebilir).");
            }
        }

        private void OnDestroy()
        {
            if (Ornek == this) Ornek = null;
            Aktif = false;
        }

        /// <summary>
        /// Verilen aşama (0..6) ve aşamadaki spin sırası (0-indexed) için tanımlı scripted kaydı döndürür.
        /// Tanımsızsa (boş aşama, sınır dışı index) <c>null</c> döner → çağıran taraf RNG akışına düşmelidir.
        /// </summary>
        /// <param name="asamaIndex">0..6 aralığında. 0 = Isındırma, 6 = Tükeniş.</param>
        /// <param name="spinSiraNo">O aşamadaki kaçıncı spin (0-indexed). AnlaticiSeritKopru.AsamadakiSpinSayaci ile beslenir.</param>
        /// <summary>En son <see cref="SonrakiSpiniAl"/> çağrısının döndürdüğü kayıt; modal mesajı için DonusAkisServisi tarafından okunur.</summary>
        public ScriptedSpinKaydi SonKayit { get; private set; }

        /// <summary>A6 başında yükleme paneli bir kez gösterilir; bayrak set edilince tekrar açılmaz.</summary>
        private bool _yuklemePaneliGosterildi = false;

        public ScriptedSpinKaydi SonrakiSpiniAl(int asamaIndex, int spinSiraNo)
        {
            // A6 (idx 5) ilk spin'inde yükleme paneli aç (bir kez); SpinButonImpl bu spin'i ScriptedYuklemePaneli.IsAcik
            // koşuluyla bloke eder, kullanıcı butona tıklayınca bakiye +50.000 ve panel kapanır.
            if (asamaIndex == 5 && spinSiraNo == 0 && !_yuklemePaneliGosterildi)
            {
                _yuklemePaneliGosterildi = true;
                var panel = UnityEngine.Object.FindObjectOfType<ScriptedYuklemePaneli>();
                if (panel != null) panel.PaneliGoster();
                else Debug.LogWarning("[ScriptedSpinYoneticisi] A6 yükleme paneli bulunamadı (ScriptedYuklemePaneli null).");
            }

            // A6 (idx 5) — runtime dinamik spin üretimi (asset'te boş liste).
            // Plan: bakiye 50.800 → 0, bahis 2500, çoğunlukla brüt 0 (kayıp odaklı kapanış).
            if (asamaIndex == 5)
            {
                SonKayit = UretA6DinamikSpin(spinSiraNo);
                return SonKayit;
            }

            if (_asamaListesi == null || _asamaListesi.asamaSpinleri == null) return null;
            if (asamaIndex < 0 || asamaIndex >= _asamaListesi.asamaSpinleri.Count) return null;
            var asama = _asamaListesi.asamaSpinleri[asamaIndex];
            if (asama == null || asama.spinler == null) return null;
            if (spinSiraNo < 0 || spinSiraNo >= asama.spinler.Count) return null;
            SonKayit = asama.spinler[spinSiraNo];
            return SonKayit;
        }

#if UNITY_EDITOR
        // ───────────────────────────────────────────────────────────────────────
        // INSPECTOR CONTEXT MENU — Editor'da Play modunda Hierarchy'den bu component
        // üzerine sağ tıklayınca atlama seçenekleri menüde görünür. Sahneye sıfır
        // müdahale (UI yok, runtime Canvas yok). Final sürümde bu blok #if UNITY_EDITOR
        // sayesinde build'e dahil edilmez.
        // ───────────────────────────────────────────────────────────────────────

        [ContextMenu("Atla → A1 Başla (50K TL)")]
        private void DebugAtlaA1() => DebugAtla(0, 0, 50000);

        [ContextMenu("Atla → A2 Başla (60K TL)")]
        private void DebugAtlaA2() => DebugAtla(1, 0, 60000);

        [ContextMenu("Atla → A3 Başla (56K TL)")]
        private void DebugAtlaA3() => DebugAtla(2, 0, 55750);

        [ContextMenu("Atla → A4 Başla (44.5K TL)")]
        private void DebugAtlaA4() => DebugAtla(3, 0, 44500);

        [ContextMenu("Atla → A5 Başla (59.5K TL)")]
        private void DebugAtlaA5() => DebugAtla(4, 0, 59500);

        [ContextMenu("Atla → A5 Spin 4 BONUS TUZAĞI (800 TL)")]
        private void DebugAtlaA5Spin4() => DebugAtla(4, 3, 800);

        [ContextMenu("Atla → A6 Yükleme Paneli (800 TL)")]
        private void DebugAtlaA6() => DebugAtla(5, 0, 800);

        [ContextMenu("Atla → A6 Bitiş (2.5K TL)")]
        private void DebugAtlaA6Bitis() => DebugAtla(5, 19, 2500);

        [ContextMenu("Atla → A7 FİNAL (0 TL)")]
        private void DebugAtlaA7() => DebugAtla(6, 0, 0);

        [ContextMenu("Sahne Reset")]
        private void DebugSahneReset()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[ScriptedSpinYoneticisi] Sahne Reset sadece Play modda çalışır.");
                return;
            }
            Debug.Log("[ScriptedSpinYoneticisi][CTX] Sahne reset.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>Inspector ContextMenu atlama yardımcısı — bakiye + Anlatıcı state + cache tazele.</summary>
        private void DebugAtla(int asama, int spin, int bakiye)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[ScriptedSpinYoneticisi] DebugAtla sadece Play modda çalışır.");
                return;
            }

            var oy = UnityEngine.Object.FindObjectOfType<OyunYoneticisi>();
            if (oy == null)
            {
                Debug.LogError("[ScriptedSpinYoneticisi] OyunYoneticisi bulunamadı.");
                return;
            }

            oy.AnlaticiBakiyeyiSifirla(bakiye);
            DebugAsamaSpinSet(asama, spin); // mevcut public method (Anlatıcı state + cache tazele)

            Debug.Log($"[ScriptedSpinYoneticisi][CTX] Atla → asama={asama + 1}, spin={spin + 1}, bakiye={bakiye} TL");
        }
#endif

        /// <summary>
        /// ⚠️ GEÇİCİ — <see cref="ScriptedDebugAtlamaPaneli"/> ve Inspector ContextMenu tarafından kullanılır.
        /// Anlatıcı state'ini (asama+spin) ve scripted yöneticinin kendi state'ini set eder, cache tazeler.
        /// Final sürümde bu method ve debug helper'ları kaldırılmalı.
        /// </summary>
        public void DebugAsamaSpinSet(int asama, int spin)
        {
            // Yükleme paneli flag'i sıfırla — A6 atlamada yeniden tetiklenebilsin
            _yuklemePaneliGosterildi = false;

            // Anlatıcı state set
            if (AnlaticiSeritKopru.Ornek != null)
            {
                AnlaticiSeritKopru.Ornek.DebugAsamaSpinSet(asama, spin);
            }

            // Önceki spin önbelleğini temizle + yeni precompute (yeni asama+spin'den başlasın)
            var oy = UnityEngine.Object.FindObjectOfType<OyunYoneticisi>();
            if (oy != null) oy.ScriptedSenaryoCacheTazele();

            Debug.Log($"[ScriptedSpinYoneticisi][DEBUG] Atlama: asama={asama + 1}, spin={spin + 1} (0-indexed asama={asama}, spin={spin}).");
        }

        /// <summary>
        /// A6 runtime dinamik spin üretimi. Plan'a göre A6 baştan tanımlı listeye sahip değil:
        /// bakiye 50.800'den 0'a düşene kadar küçük kayıp spinleri servis edilir.
        /// Bahis 2500, brüt çoğunlukla 0 (kayıp); cluster yok, tumble yok, modal yok.
        /// </summary>
        private static ScriptedSpinKaydi UretA6DinamikSpin(int spinSiraNo)
        {
            const int BAHIS_A6 = 2500;
            const int SUTUN = 6;
            const int SATIR = 5;
            const int HUCRE = SUTUN * SATIR;

            // Basit kazançsız grid: 6 sembol × 5'er hücre, hiçbir cluster 8'e ulaşmaz.
            // (Asset üreticideki GridSifir'in runtime mini-versiyonu; üzüm/çarpan dolguda yok.)
            int[] dolguPool = { 0, 1, 2, 3, 4, 5 };
            int[] grid = new int[HUCRE];
            for (int i = 0; i < HUCRE; i++) grid[i] = dolguPool[i % dolguPool.Length];

            return new ScriptedSpinKaydi
            {
                spinSiraNo = spinSiraNo + 1, // 1-indexed gösterim
                asamaIndex = 5,
                bahis = BAHIS_A6,
                tip = SpinTipi.Sifir,
                brutOdeme = 0,
                ilkGridSemboller = grid,
                ilkCarpanDegerleri = new int[HUCRE],
                tumbleler = new System.Collections.Generic.List<TumbleAdimTanimi>(),
                modalMesaji = null,           // A6 modal mesajları AŞAMA 6'da eklenebilir
                carpanKactiFlag = false,
                bonusOyunuTetikle = false,
                bonusGetirisi = 0
            };
        }
    }
}
