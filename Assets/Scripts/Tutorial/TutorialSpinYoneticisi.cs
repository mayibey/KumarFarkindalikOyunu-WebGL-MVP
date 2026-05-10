using Senaryo.Scripted;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

namespace KumarFarkindalik.Tutorial
{
    /// <summary>
    /// PAKET 1: TutorialSpinYoneticisi'nin TAM KOPYASI — tutorial sahnesi (build idx 3) için.
    /// Sadece namespace, class adı ve build idx sabiti değişti. Resources path aynı kalır
    /// (asset = ScriptedSenaryo) — tutorial özel asset gelecek paketlerde yapılır.
    /// Orijinal dosyaya dokunulmadı.
    /// </summary>
    [Preserve]
    public class TutorialSpinYoneticisi : MonoBehaviour
    {
        /// <summary>04_AdminOyunScene (Tutorial sahnesi) Build Settings index'i.</summary>
        public const int TUTORIAL_SAHNE_BUILD_INDEX = 3;

        /// <summary>true sadece anlatıcı sahnesinde + asset başarıyla yüklendiğinde.</summary>
        public static bool Aktif { get; private set; }

        /// <summary>Sahnedeki tek instance. Aktif değilken null olabilir.</summary>
        public static TutorialSpinYoneticisi Ornek { get; private set; }

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
            if (aktifSahne.buildIndex == TUTORIAL_SAHNE_BUILD_INDEX)
            {
                Debug.Log("[ScriptedTANI] Bootstrap'ta zaten idx=2 → OnSceneLoaded çağrılıyor.");
                OnSceneLoaded(aktifSahne, LoadSceneMode.Single);
            }
        }

        [Preserve]
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[ScriptedTANI] OnSceneLoaded ÇAĞRILDI — idx={scene.buildIndex}, ad={scene.name}, beklenen idx={TUTORIAL_SAHNE_BUILD_INDEX}");
            if (scene.buildIndex != TUTORIAL_SAHNE_BUILD_INDEX)
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
            var go = new GameObject(nameof(TutorialSpinYoneticisi));
            go.AddComponent<TutorialSpinYoneticisi>();
            Debug.Log("[ScriptedTANI] OnSceneLoaded BAŞARILI → Awake çağrılacak.");
        }

        private void Awake()
        {
            Debug.Log("[ScriptedTANI] Awake() ÇAĞRILDI");
            int idx = SceneManager.GetActiveScene().buildIndex;
            if (idx != TUTORIAL_SAHNE_BUILD_INDEX)
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
                Debug.LogWarning("[TutorialSpinYoneticisi] OyunYoneticisi bulunamadı; cache tazeleme atlandı (ilk spin RNG önbelleğinden gelebilir).");
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
            // BUG FIX: A6 yükleme paneli açma sorumluluğu AnlaticiSeritKopru.BasaArayisAkisi'na taşındı.
            // Eskiden buradan da açılıyordu → çift kaynak → A6 ilk spin'de panel TEKRAR açılıyordu.
            // Tek source of truth: AnlaticiSeritKopru. Bu blok kaldırıldı.

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

        /// <summary>
        /// A5 cazip popup → bonus oyun: 10 sabit scripted spin (asset'teki bonusSpinleri'nden okur).
        /// SimuleEtVeKaydetImpl bonusSpin=true durumunda bu metoda başvurur (ScriptedBonusSpinAktif aktifse).
        /// Toplam 4000 TL ödeme garantili (paytable doğrulanmış).
        /// </summary>
        /// <param name="bonusSpinIdx">0-indexed (0..9). DonusAkisServisi.BonusDongusu (10 - bonusHakKalan) ile besler.</param>
        public ScriptedSpinKaydi SonrakiBonusSpiniAl(int bonusSpinIdx)
        {
            if (_asamaListesi == null || _asamaListesi.bonusSpinleri == null) return null;
            if (bonusSpinIdx < 0 || bonusSpinIdx >= _asamaListesi.bonusSpinleri.Count) return null;
            SonKayit = _asamaListesi.bonusSpinleri[bonusSpinIdx];
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
                Debug.LogWarning("[TutorialSpinYoneticisi] Sahne Reset sadece Play modda çalışır.");
                return;
            }
            Debug.Log("[TutorialSpinYoneticisi][CTX] Sahne reset.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>Inspector ContextMenu atlama yardımcısı — bakiye + Anlatıcı state + cache tazele.</summary>
        private void DebugAtla(int asama, int spin, int bakiye)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[TutorialSpinYoneticisi] DebugAtla sadece Play modda çalışır.");
                return;
            }

            var oy = UnityEngine.Object.FindObjectOfType<OyunYoneticisi>();
            if (oy == null)
            {
                Debug.LogError("[TutorialSpinYoneticisi] OyunYoneticisi bulunamadı.");
                return;
            }

            oy.AnlaticiBakiyeyiSifirla(bakiye);
            DebugAsamaSpinSet(asama, spin); // mevcut public method (Anlatıcı state + cache tazele)

            Debug.Log($"[TutorialSpinYoneticisi][CTX] Atla → asama={asama + 1}, spin={spin + 1}, bakiye={bakiye} TL");
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

            Debug.Log($"[TutorialSpinYoneticisi][DEBUG] Atlama: asama={asama + 1}, spin={spin + 1} (0-indexed asama={asama}, spin={spin}).");
        }

        /// <summary>
        /// A6 runtime dinamik spin üretimi. Plan'a göre A6 baştan tanımlı listeye sahip değil:
        /// bakiye 50K → 0'a düşene kadar kayıp spinleri servis edilir (5 spin × 10K = 50K borç).
        /// Grid <see cref="GridRastgeleKayip"/> ile rastgele dağılmış meyve sembolleriyle dolar
        /// (deterministik seed: 6001 + spinSiraNo). Brüt=0, cluster/tumble/modal yok.
        /// </summary>
        private static ScriptedSpinKaydi UretA6DinamikSpin(int spinSiraNo)
        {
            const int BAHIS_A6 = 10000; // Hızlı yıkım: 5 spin × 10K = 50K borç tükenir.
            const int HUCRE = 30;       // 6 sütun × 5 satır

            // Deterministik seed: 6001..6005 (5 spin için), aynı seed → aynı grid.
            int seed = 6001 + spinSiraNo;
            int[] grid = GridRastgeleKayip(seed);

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

        /// <summary>
        /// 6×5 grid'i 0..7 (8=scatter HARİÇ — bonus tetiklenmesin) arası rastgele meyve sembolleriyle
        /// doldurur. Aynı <paramref name="seed"/> → aynı grid (deterministik test için).
        ///
        /// Payline koruması: her satırın ilk 3 reel'inde (sutun 0-1-2) aynı sembol 3 kez peş peşe
        /// gelirse 3. hücre bir sonraki sembolle değiştirilir — slot win pattern'inden kaçınılır.
        /// (Cluster pays mantığı konum bağımsız olduğu için bu kontrol görsel-amaçlıdır;
        /// brutOdeme=0 zaten kayıp spini garanti eder, motor o sayıyı kullanır.)
        /// </summary>
        private static int[] GridRastgeleKayip(int seed)
        {
            const int SUTUN = 6;
            const int SATIR = 5;
            const int HUCRE = SUTUN * SATIR;

            var rng = new System.Random(seed);
            int[] g = new int[HUCRE];
            for (int i = 0; i < HUCRE; i++)
                g[i] = rng.Next(0, 8); // [0,8) → 0..7, scatter (8) hariç

            // Her satırın ilk 3 sütunu için win-pattern temizliği
            for (int y = 0; y < SATIR; y++)
            {
                int i0 = y * SUTUN;
                int i1 = i0 + 1;
                int i2 = i0 + 2;
                if (g[i0] == g[i1] && g[i1] == g[i2])
                    g[i2] = (g[i2] + 1) % 8; // 3. hücreyi farklı sembolle değiştir
            }
            return g;
        }
    }
}
