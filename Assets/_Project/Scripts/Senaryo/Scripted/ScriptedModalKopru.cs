using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace Senaryo.Scripted
{
    /// <summary>
    /// Karakter Dialog (Visual Novel tarzı) — sahnedeki slot grid görünür kalır, sadece sol-altta
    /// "EĞİTMEN" karakteri silüeti + konuşma balonu slide-in olur. Typewriter ile mesaj yazar.
    ///
    /// Click davranışı (iki kademeli):
    ///   - Yazma sırasında balona/karaktere tıklanırsa: typewriter atlanır, tüm metin anında görünür.
    ///   - Yazma bitmiş + balona tıklanırsa: karakter slide-out, modal kapanır.
    ///
    /// Arka plan dim YOK; oyun görseli oyundan koparılmaz. Sadece sahne 2 (03_SenaryoluOyun)'de aktif.
    /// </summary>
    [Preserve]
    public class ScriptedModalKopru : MonoBehaviour
    {
        public const int ANLATICI_SAHNE_BUILD_INDEX = 2;
        public static ScriptedModalKopru Ornek { get; private set; }

        /// <summary>Modal görünür mü? ModalGoster coroutine süresince true; SpinButonImpl ve
        /// OyunUIGuncellemeServisi bu flag'e bakıp Spin butonunu engeller.</summary>
        public static bool ModalAcik { get; private set; }

        // === UI referansları ===
        private GameObject _root;
        private RectTransform _karakterRt;
        private RectTransform _balonRt;
        private CanvasGroup _balonCanvasGroup;
        private TextMeshProUGUI _mesajText;
        private TextMeshProUGUI _baslikText;
        // TAMAM butonunun fade-in alpha kontrolü (Image+Button GameObject'in CanvasGroup'u).
        private CanvasGroup _tamamCanvasGroup;
        private Button _balonButton;

        // === Animasyon parametreleri ===
        private const float SLIDE_SURE = 0.4f;
        private const float CIKIS_SURE = 0.3f;
        private const float TYPEWRITER_HARF_BASINA = 0.015f; // ~15 ms / harf (yarıya indirildi)
        private const float DEVAM_FADE_SURE = 0.3f;

        // Karakter balon'un CHILD'ı — local pozisyon sabit, ayrı animate edilmiyor.
        // Balon Acik/Kapali pos slide-in/out için kullanılır.
        private Vector2 _balonAcikPos;
        private Vector2 _balonKapaliPos;

        // === Click state ===
        private bool _typewriterCalisiyor;
        private bool _typewriterAtla;
        private bool _yazmaTamamlandi;
        private bool _kullaniciDevamEtti;

        [Preserve]
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OtomatikInit()
        {
            // sceneLoaded event abone: WebGL bootstrap sonrası sahne geçişlerinde de tetiklenir.
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            var aktifSahne = SceneManager.GetActiveScene();
            if (aktifSahne.buildIndex == ANLATICI_SAHNE_BUILD_INDEX)
                OnSceneLoaded(aktifSahne, LoadSceneMode.Single);
        }

        [Preserve]
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.buildIndex != ANLATICI_SAHNE_BUILD_INDEX) return;
            if (Ornek != null) return;
            var go = new GameObject(nameof(ScriptedModalKopru));
            go.AddComponent<ScriptedModalKopru>();
        }

        private void Awake()
        {
            if (SceneManager.GetActiveScene().buildIndex != ANLATICI_SAHNE_BUILD_INDEX)
            {
                gameObject.SetActive(false);
                return;
            }
            if (Ornek != null && Ornek != this) { Destroy(gameObject); return; }
            Ornek = this;

            // EventSystem güvencesi (idempotent)
            if (UnityEngine.Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGO = new GameObject("EventSystem (Auto)");
                esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            UIYarat();
            if (_root != null) _root.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Ornek == this) Ornek = null;
        }

        /// <summary>
        /// Bloke eden karakter dialog: slide-in → typewriter → kullanıcı tıklayana kadar bekle → slide-out.
        /// Boş/null mesaj veya UI hazır değilse anında tamamlanır (defansif).
        /// <paramref name="gizleAnlatici"/>: false geçilirse sol panel iframe AÇIK kalır (Pre-A1
        /// karşılama gibi — modal "sol panel" anlatırken paneli kullanıcının görmesi gerekiyor).
        /// </summary>
        public IEnumerator ModalGoster(string mesaj, bool gizleAnlatici = true)
        {
            if (string.IsNullOrEmpty(mesaj) || _root == null || _mesajText == null)
                yield break;

            ModalAcik = true; // Spin butonu bu süre boyunca engellenir

            // Anlatici HTML iframe yönetimi: TÜM modal akışları için ArkayaAt çağrılır
            // (panel translateX(-100px) + opacity:0.4 + pointer-events:none — display:none yerine).
            // gizleAnlatici parametresi geriye uyumluluk için signature'da kalır ama davranış birleşti:
            // Pre-A1 (false) ve diğer modallar (true) artık aynı pattern'de panel'i sola kaydırır.
            AnlaticiSeritKopru.Ornek?.ArkayaAt();

            try
            {
                // DİNAMİK YÜKSEKLİK: mesajın TMP-rendered yüksekliğini ölçüp balon kutusunu uyarla.
                // Genişlik 680 sabit, mesaj iç padding 40 (sol 20+sağ 20), başlık ~45, TAMAM butonu ~60,
                // alt padding 12 → toplam dikey rezerv ~140 px. Min 200, max 600 clamp.
                const float BALON_GENISLIK = 680f;
                const float MESAJ_GENISLIK = BALON_GENISLIK - 40f; // sol+sağ padding
                const float DIKEY_REZERV = 140f; // başlık + buton + padding
                const float MIN_YUKSEKLIK = 200f;
                const float MAX_YUKSEKLIK = 600f;
                Vector2 prefSize = _mesajText.GetPreferredValues(mesaj, MESAJ_GENISLIK, 0f);
                float balonYukseklik = Mathf.Clamp(prefSize.y + DIKEY_REZERV, MIN_YUKSEKLIK, MAX_YUKSEKLIK);
                _balonRt.sizeDelta = new Vector2(BALON_GENISLIK, balonYukseklik);
                // Tam orta yerleşim (anchor 0.5, 0.5). Açık pos (0,0), kapalı pos ekran altı.
                // Karakter balon CHILD'ı → balon ile birlikte hareket eder, ayrı pos hesabı yok.
                _balonAcikPos = new Vector2(0f, 0f);
                _balonKapaliPos = new Vector2(0f, -700f);

                // State reset
                _typewriterCalisiyor = false;
                _typewriterAtla = false;
                _yazmaTamamlandi = false;
                _kullaniciDevamEtti = false;
                _mesajText.text = "";
                _mesajText.maxVisibleCharacters = 0; // slide-in sırasında metin kazara görünmesin
                if (_tamamCanvasGroup != null)
                {
                    _tamamCanvasGroup.alpha = 0f;
                    _tamamCanvasGroup.interactable = false;
                    _tamamCanvasGroup.blocksRaycasts = false;
                }

                _root.SetActive(true);

                // Slide-in: karakter + balon ekran-altından yukarı kayar
                yield return KarakterGiris();

                // Typewriter
                yield return TypewriterYaz(mesaj);
                _yazmaTamamlandi = true;

                // [→] devam ikonu fade-in
                yield return DevamIkonuFadeIn();

                // Kullanıcı tıklayana kadar bekle
                while (!_kullaniciDevamEtti) yield return null;

                // Slide-out
                yield return KarakterCikis();

                _root.SetActive(false);
            }
            finally
            {
                ModalAcik = false; // Spin butonu tekrar serbest

                // Anlatici iframe state geri yükleme — TÜM modallar için OneAl
                // (transform:none + opacity:1 + pointer-events:auto). gizleAnlatici parametresi
                // davranışı etkilemez (geriye uyumluluk için signature korundu).
                AnlaticiSeritKopru.Ornek?.OneAl();
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // Animasyonlar
        // ──────────────────────────────────────────────────────────────────────

        private IEnumerator KarakterGiris()
        {
            float t = 0f;
            // Karakter balon CHILD'ı — pozisyonu ayrı animate edilmiyor, balon parent'ı ile birlikte hareket eder.
            _balonRt.anchoredPosition = _balonKapaliPos;
            if (_balonCanvasGroup != null) _balonCanvasGroup.alpha = 1f;

            while (t < SLIDE_SURE)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / SLIDE_SURE);
                float eased = 1f - Mathf.Pow(1f - u, 3f); // ease-out cubic
                _balonRt.anchoredPosition = Vector2.Lerp(_balonKapaliPos, _balonAcikPos, eased);
                yield return null;
            }
            _balonRt.anchoredPosition = _balonAcikPos;
        }

        private IEnumerator KarakterCikis()
        {
            float t = 0f;
            float baslangicAlpha = _balonCanvasGroup != null ? _balonCanvasGroup.alpha : 1f;
            // Karakter balon CHILD'ı — start pozisyonu kaydetmiyoruz, balon ile birlikte gider.
            Vector2 balonStart = _balonRt.anchoredPosition;
            Vector3 balonScaleStart = _balonRt.localScale;

            while (t < CIKIS_SURE)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / CIKIS_SURE);
                _balonRt.anchoredPosition = Vector2.Lerp(balonStart, _balonKapaliPos, u);
                _balonRt.localScale = Vector3.Lerp(balonScaleStart, balonScaleStart * 0.95f, u);
                if (_balonCanvasGroup != null)
                    _balonCanvasGroup.alpha = Mathf.Lerp(baslangicAlpha, 0f, u);
                yield return null;
            }
            _balonRt.localScale = balonScaleStart;
        }

        private IEnumerator TypewriterYaz(string mesaj)
        {
            _typewriterCalisiyor = true;
            _typewriterAtla = false;

            // Rich-text safe typewriter: tüm metni bir kerede ata, sonra görünür karakter sayısını
            // 0 → toplamHarf'e doğru artır. TMP <color>, <b>, <i> gibi tag'leri görünür karakter
            // saymaz → tag'ler asla raw HTML olarak ekrana çıkmaz, sadece harfler kademeli görünür.
            _mesajText.text = mesaj;
            _mesajText.maxVisibleCharacters = 0;
            _mesajText.ForceMeshUpdate();
            int toplamHarf = _mesajText.textInfo.characterCount;

            for (int i = 0; i <= toplamHarf; i++)
            {
                if (_typewriterAtla)
                {
                    _mesajText.maxVisibleCharacters = toplamHarf;
                    break;
                }
                _mesajText.maxVisibleCharacters = i;
                float bekle = TYPEWRITER_HARF_BASINA;
                float t = 0f;
                while (t < bekle && !_typewriterAtla)
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            if (_typewriterAtla) _mesajText.maxVisibleCharacters = toplamHarf;
            _typewriterCalisiyor = false;
        }

        private IEnumerator DevamIkonuFadeIn()
        {
            if (_tamamCanvasGroup == null) yield break;
            float t = 0f;
            _tamamCanvasGroup.alpha = 0f;
            while (t < DEVAM_FADE_SURE)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / DEVAM_FADE_SURE);
                _tamamCanvasGroup.alpha = u;
                yield return null;
            }
            _tamamCanvasGroup.alpha = 1f;
            _tamamCanvasGroup.interactable = true;
            _tamamCanvasGroup.blocksRaycasts = true;
        }

        // ──────────────────────────────────────────────────────────────────────
        // Click handler — iki kademeli
        // ──────────────────────────────────────────────────────────────────────

        private void OnBalonTiklandi()
        {
            if (_typewriterCalisiyor)
            {
                _typewriterAtla = true; // typewriter coroutine bunu görür, anında tamamlar
            }
            else if (_yazmaTamamlandi)
            {
                _kullaniciDevamEtti = true; // ModalGoster while loop'u biter
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // UI yarat (runtime, prefab gerekmez)
        // ──────────────────────────────────────────────────────────────────────

        private void UIYarat()
        {
            // Root canvas (sortingOrder 1500 — ödeme uçuşu vs üstüne)
            _root = new GameObject("ScriptedModalKopruCanvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _root.transform.SetParent(transform, false);
            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1500;
            var scaler = _root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // === BALON (ekran tam ortası) ===
            // Karakter aşağıda balon yaratıldıktan SONRA balon child'ı olarak eklenir
            // (Browser zoom + Canvas Scaler durumlarında karakter balon'la birlikte scale/move etsin).
            // Anchor (0.5, 0.5) + pivot (0.5, 0.5) → ekran merkezine yerleşir; reference resolution
            // 1920×1080 üzerinde balon (680×420) tam ortada. Karakter sol kenarında bitişik.
            var balonGo = new GameObject("Balon",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(CanvasGroup), typeof(Button));
            balonGo.transform.SetParent(_root.transform, false);
            _balonRt = balonGo.GetComponent<RectTransform>();
            _balonRt.anchorMin = _balonRt.anchorMax = _balonRt.pivot = new Vector2(0.5f, 0.5f);
            // Balon yüksekliği 170→420: pre-A1 ve uzun pedagojik metinler taşmasın.
            // Mesaj alanı + başlık + TAMAM butonu hepsi sığsın.
            _balonRt.sizeDelta = new Vector2(680f, 420f);
            _balonAcikPos = new Vector2(0f, 0f);     // tam orta
            _balonKapaliPos = new Vector2(0f, -700f); // ekran altında gizli (slide-in için)
            _balonRt.anchoredPosition = _balonKapaliPos;
            Debug.Log("[Modal] Ortaya konumlandı, anchor=(0.5, 0.5)");
            var balonImg = balonGo.GetComponent<Image>();
            balonImg.color = new Color(0.10f, 0.16f, 0.23f, 0.95f); // dark navy
            _balonCanvasGroup = balonGo.GetComponent<CanvasGroup>();
            _balonButton = balonGo.GetComponent<Button>();
            _balonButton.transition = Selectable.Transition.None;
            _balonButton.onClick.AddListener(OnBalonTiklandi);

            // Balon altın border (4 kenar)
            BorderEkle(balonGo.transform, _balonRt.sizeDelta, 2f, new Color(0.83f, 0.69f, 0.22f, 1f));

            // Karakter işaret oku (balonun sol kenarında küçük üçgen — basit kare yerine kare ayağı)
            var okGo = new GameObject("Ok",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            okGo.transform.SetParent(balonGo.transform, false);
            var okRt = okGo.GetComponent<RectTransform>();
            okRt.anchorMin = okRt.anchorMax = new Vector2(0f, 0.5f);
            okRt.pivot = new Vector2(1f, 0.5f);
            okRt.sizeDelta = new Vector2(20f, 30f);
            okRt.anchoredPosition = Vector2.zero;
            var okImg = okGo.GetComponent<Image>();
            okImg.color = new Color(0.10f, 0.16f, 0.23f, 0.95f);
            okImg.raycastTarget = false;

            // === KARAKTER (BALON CHILD'I — sol kenara bitişik) ===
            // Anchor (0, 0.5) sol-orta + pivot (1, 0.5) sağ-orta → karakter SAĞ kenarı balon
            // SOL kenarına dayanır. Balon ne yaparsa (genişlik/scale değişimi, browser zoom)
            // karakter otomatik takip eder — parent transform inheritance ile garantili.
            var karakterGo = new GameObject("Karakter",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            karakterGo.transform.SetParent(balonGo.transform, false);
            _karakterRt = karakterGo.GetComponent<RectTransform>();
            _karakterRt.anchorMin = _karakterRt.anchorMax = new Vector2(0f, 0.5f);
            _karakterRt.pivot = new Vector2(1f, 0.5f);
            _karakterRt.sizeDelta = new Vector2(220f, 280f);
            _karakterRt.anchoredPosition = new Vector2(-20f, 0f); // balon sol kenarından 20 px dışarı
            var karakterImg = karakterGo.GetComponent<Image>();
            karakterImg.sprite = EgitmenGorseliniAl();
            karakterImg.preserveAspect = true;
            karakterImg.raycastTarget = true; // tıklama silüete de düşsün

            var karakterBtn = karakterGo.AddComponent<Button>();
            karakterBtn.transition = Selectable.Transition.None;
            karakterBtn.onClick.AddListener(OnBalonTiklandi);

            Debug.Log("[Modal] Karakter balon child'ı yapıldı, local pos=(-20, 0)");

            // Başlık: "BİLGİLENDİRİCİ ASİSTAN"
            var basGo = new GameObject("Baslik",
                typeof(RectTransform), typeof(CanvasRenderer));
            basGo.transform.SetParent(balonGo.transform, false);
            var basRt = basGo.GetComponent<RectTransform>();
            basRt.anchorMin = new Vector2(0f, 1f);
            basRt.anchorMax = new Vector2(1f, 1f);
            basRt.pivot = new Vector2(0.5f, 1f);
            basRt.sizeDelta = new Vector2(0f, 32f);
            basRt.anchoredPosition = new Vector2(0f, -10f);
            _baslikText = basGo.AddComponent<TextMeshProUGUI>();
            _baslikText.alignment = TextAlignmentOptions.Center;
            _baslikText.fontSize = 18f;
            _baslikText.fontStyle = FontStyles.Bold;
            _baslikText.color = new Color(0.83f, 0.69f, 0.22f, 1f); // altın
            _baslikText.text = "BİLGİLENDİRİCİ ASİSTAN";
            _baslikText.raycastTarget = false;

            // Mesaj text (typewriter target)
            var mesajGo = new GameObject("Mesaj",
                typeof(RectTransform), typeof(CanvasRenderer));
            mesajGo.transform.SetParent(balonGo.transform, false);
            var mesajRt = mesajGo.GetComponent<RectTransform>();
            mesajRt.anchorMin = new Vector2(0f, 0f);
            mesajRt.anchorMax = new Vector2(1f, 1f);
            mesajRt.offsetMin = new Vector2(20f, 12f);   // alt 12, sol 20
            mesajRt.offsetMax = new Vector2(-50f, -45f); // sağ 50 (devam ikonuna yer), üst 45 (başlık)
            _mesajText = mesajGo.AddComponent<TextMeshProUGUI>();
            _mesajText.alignment = TextAlignmentOptions.TopJustified;
            _mesajText.fontSize = 19f;
            _mesajText.fontStyle = FontStyles.Normal;
            _mesajText.color = new Color(0.95f, 0.97f, 1f, 1f);
            _mesajText.enableWordWrapping = true;
            _mesajText.text = "";
            _mesajText.raycastTarget = false;

            // TAMAM butonu — sağ alt köşe; typewriter sonu fade-in olur, click → modal kapanır.
            // Image + Button + CanvasGroup; CanvasGroup ile alpha + interactable kontrolü.
            var tamamGo = new GameObject("TamamButon",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button), typeof(CanvasGroup));
            tamamGo.transform.SetParent(balonGo.transform, false);
            var tamamRt = tamamGo.GetComponent<RectTransform>();
            tamamRt.anchorMin = tamamRt.anchorMax = new Vector2(1f, 0f);
            tamamRt.pivot = new Vector2(1f, 0f);
            tamamRt.sizeDelta = new Vector2(100f, 40f);
            tamamRt.anchoredPosition = new Vector2(-12f, 12f);
            var tamamImg = tamamGo.GetComponent<Image>();
            tamamImg.color = new Color(0.12f, 0.12f, 0.12f, 0.75f); // koyu yarı saydam
            // 1.5px sarı border (#FAC775)
            BorderEkle(tamamGo.transform, tamamRt.sizeDelta, 1.5f, new Color(0.98f, 0.78f, 0.46f, 1f));
            var tamamBtn = tamamGo.GetComponent<Button>();
            tamamBtn.transition = Selectable.Transition.None;
            tamamBtn.onClick.AddListener(OnBalonTiklandi);
            _tamamCanvasGroup = tamamGo.GetComponent<CanvasGroup>();
            _tamamCanvasGroup.alpha = 0f;
            _tamamCanvasGroup.interactable = false;
            _tamamCanvasGroup.blocksRaycasts = false;

            // "TAMAM" yazısı
            var ttxtGo = new GameObject("TamamTxt", typeof(RectTransform), typeof(CanvasRenderer));
            ttxtGo.transform.SetParent(tamamGo.transform, false);
            var ttxtRt = ttxtGo.GetComponent<RectTransform>();
            ttxtRt.anchorMin = Vector2.zero; ttxtRt.anchorMax = Vector2.one;
            ttxtRt.offsetMin = ttxtRt.offsetMax = Vector2.zero;
            var ttxt = ttxtGo.AddComponent<TextMeshProUGUI>();
            ttxt.alignment = TextAlignmentOptions.Center;
            ttxt.fontSize = 18f;
            ttxt.fontStyle = FontStyles.Bold;
            ttxt.color = Color.white;
            ttxt.text = "TAMAM";
            ttxt.raycastTarget = false;
        }

        // ──────────────────────────────────────────────────────────────────────
        // Border helper
        // ──────────────────────────────────────────────────────────────────────

        private static void BorderEkle(Transform parent, Vector2 size, float kalinlik, Color renk)
        {
            (string ad, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta)[] kenarlar =
            {
                ("Ust",  new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, kalinlik)),
                ("Alt",  new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, kalinlik)),
                ("Sol",  new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(kalinlik, 0f)),
                ("Sag",  new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(kalinlik, 0f)),
            };
            foreach (var k in kenarlar)
            {
                var go = new GameObject("Border_" + k.ad, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(parent, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = k.anchorMin;
                rt.anchorMax = k.anchorMax;
                rt.sizeDelta = k.sizeDelta;
                rt.anchoredPosition = Vector2.zero;
                var img = go.GetComponent<Image>();
                img.color = renk;
                img.raycastTarget = false;
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // Karakter görseli — Resources/egitmenyuz.png (kullanıcı PNG'si) veya procedural fallback
        // ──────────────────────────────────────────────────────────────────────

        private static Sprite _cachedSprite;

        /// <summary>
        /// Eğitmen karakter sprite kaynağını döndürür. Önce Resources/egitmenyuz.png yüklenir
        /// (kullanıcının görseli, Sprite olarak import edilmiş olmalı); bulunmazsa procedural
        /// jandarma silueti fallback olarak kullanılır.
        /// </summary>
        private static Sprite EgitmenGorseliniAl()
        {
            if (_cachedSprite != null) return _cachedSprite;

            var sprite = Resources.Load<Sprite>("egitmenyuz");
            if (sprite != null)
            {
                Debug.Log("[ScriptedModalKopru] Resources/egitmenyuz.png yüklendi.");
                _cachedSprite = sprite;
                return sprite;
            }

            Debug.LogWarning("[ScriptedModalKopru] Resources/egitmenyuz.png bulunamadı (Sprite import edilmemiş olabilir) — procedural eğitmen silueti kullanılıyor.");
            return JandarmaSiluetiUret();
        }

        private static Sprite JandarmaSiluetiUret()
        {
            if (_cachedSprite != null) return _cachedSprite;

            const int W = 200, H = 250;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color govde = new Color(0.17f, 0.24f, 0.31f, 1f); // #2c3e50
            Color altın = new Color(0.83f, 0.69f, 0.22f, 1f); // #d4af37
            Color clear = new Color(0f, 0f, 0f, 0f);

            var pixels = new Color[W * H];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            // Y-axis: 0=alt, H-1=üst (Texture2D koordinatı)
            // Layout (alttan üste):
            //   y∈[0,90]    : omuzlar (trapez, alt geniş)
            //   y∈[90,115]  : boyun (dar dikdörtgen)
            //   y∈[115,180] : baş (oval)
            //   y∈[180,210] : kasket gövde (dikdörtgen, başın üstü)
            //   y∈[170,185] : kasket vizoru (önde çıkıntı, kasket altı)

            // Omuzlar (trapez): alt y=0 genişlik 180, üst y=90 genişlik 90
            for (int y = 0; y < 90; y++)
            {
                float t = y / 90f;
                int genislik = Mathf.RoundToInt(Mathf.Lerp(180f, 90f, t));
                int xStart = (W - genislik) / 2;
                for (int x = xStart; x < xStart + genislik; x++)
                    pixels[y * W + x] = govde;
            }

            // Boyun
            for (int y = 90; y < 115; y++)
            {
                int genislik = 50;
                int xStart = (W - genislik) / 2;
                for (int x = xStart; x < xStart + genislik; x++)
                    pixels[y * W + x] = govde;
            }

            // Baş (oval)
            int basCx = W / 2, basCy = 145;
            int basRx = 48, basRy = 35;
            for (int y = basCy - basRy; y <= basCy + basRy; y++)
            {
                for (int x = basCx - basRx; x <= basCx + basRx; x++)
                {
                    if (x < 0 || x >= W || y < 0 || y >= H) continue;
                    float dx = (x - basCx) / (float)basRx;
                    float dy = (y - basCy) / (float)basRy;
                    if (dx * dx + dy * dy <= 1f)
                        pixels[y * W + x] = govde;
                }
            }

            // Kasket gövdesi (üstte düz dikdörtgen)
            for (int y = 175; y < 210; y++)
            {
                int genislik = 100;
                int xStart = (W - genislik) / 2;
                for (int x = xStart; x < xStart + genislik; x++)
                {
                    if (x < 0 || x >= W || y < 0 || y >= H) continue;
                    pixels[y * W + x] = govde;
                }
            }

            // Kasket vizoru (önde uzanan ince çıkıntı)
            for (int y = 168; y < 178; y++)
            {
                int genislik = 116;
                int xStart = (W - genislik) / 2;
                for (int x = xStart; x < xStart + genislik; x++)
                {
                    if (x < 0 || x >= W || y < 0 || y >= H) continue;
                    pixels[y * W + x] = govde;
                }
            }

            // Amblem (kasketin merkezinde altın baklava/yıldız)
            int ambCx = W / 2, ambCy = 192, ambR = 7;
            for (int y = ambCy - ambR; y <= ambCy + ambR; y++)
            {
                for (int x = ambCx - ambR; x <= ambCx + ambR; x++)
                {
                    if (x < 0 || x >= W || y < 0 || y >= H) continue;
                    int dx = Mathf.Abs(x - ambCx);
                    int dy = Mathf.Abs(y - ambCy);
                    if (dx + dy <= ambR) // diamond
                        pixels[y * W + x] = altın;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            _cachedSprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f));
            return _cachedSprite;
        }
    }
}
