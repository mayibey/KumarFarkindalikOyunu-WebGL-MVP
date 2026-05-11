using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace KumarFarkindalik.Tutorial
{
    /// <summary>
    /// Görev Takip Paneli — 280×320, BAKIYE YÜKLE konumunda (801.18, 222).
    /// PAKET 3B-fix-8 polish: 2 katmanlı altın border + header bandı + sayaç outline + subtle pulse.
    /// </summary>
    [Preserve]
    public class TutorialAdimGoster : MonoBehaviour
    {
        public const int TUTORIAL_SAHNE_BUILD_INDEX = 3;
        public const int CANVAS_SORTING_ORDER = 1650;

        public static TutorialAdimGoster Ornek { get; private set; }

        // === Renk paleti (PAKET 3B-fix-8 polish) ===
        private static readonly Color BALON_RENK   = new Color(0.10f, 0.16f, 0.23f, 0.95f); // body navy
        private static readonly Color BALON_KOYU   = new Color(0.06f, 0.10f, 0.14f, 1f);    // header (daha koyu)
        private static readonly Color ALTIN_KOYU   = new Color(0.65f, 0.52f, 0.15f, 1f);    // dış border + outline
        private static readonly Color ALTIN_RENK   = new Color(0.83f, 0.69f, 0.22f, 1f);    // başlık + İLERİ aktif zemin
        private static readonly Color ALTIN_ACIK   = new Color(0.95f, 0.80f, 0.30f, 1f);    // iç border + aksan
        private static readonly Color BUTON_ARKA   = new Color(0.12f, 0.12f, 0.12f, 0.75f);
        private static readonly Color YESIL        = new Color(0.45f, 0.85f, 0.45f, 1f);
        private static readonly Color GRI          = new Color(0.78f, 0.78f, 0.80f, 1f);
        private static readonly Color BEYAZ        = new Color(0.95f, 0.97f, 1f, 1f);
        private static readonly Color KOYU_YAZI    = new Color(0.06f, 0.08f, 0.12f, 1f);    // altın buton üstü yazı

        private const int TOPLAM_ADIM = 11;
        private const float PULSE_PERIYOT = 2.5f;
        private const float PULSE_OLCEK = 1.012f;

        public event Action OnIleriTiklandi;

        // === UI referansları ===
        private GameObject _root;
        private RectTransform _panelRt;
        private TextMeshProUGUI _sayacText;
        private TextMeshProUGUI _altBaslikText;
        private GameObject _yapilacaklarBlok;
        private TextMeshProUGUI[] _yapilacakSatirlari = new TextMeshProUGUI[3];
        private GameObject _ilerlemeBlok;
        private TextMeshProUGUI _parametreText;
        private TextMeshProUGUI _spinText;
        private Button _ileriButton;
        private Image _ileriButtonImg;
        private TextMeshProUGUI _ileriButtonTxt;
        private Coroutine _pulseCoroutine;

        public bool IleriZatenAktif { get; private set; }

        [Preserve]
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OtomatikInit()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            var aktifSahne = SceneManager.GetActiveScene();
            if (aktifSahne.buildIndex == TUTORIAL_SAHNE_BUILD_INDEX)
                OnSceneLoaded(aktifSahne, LoadSceneMode.Single);
        }

        [Preserve]
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.buildIndex != TUTORIAL_SAHNE_BUILD_INDEX)
            {
                if (Ornek != null) UnityEngine.Object.Destroy(Ornek.gameObject);
                return;
            }
            if (Ornek != null) return;
            var go = new GameObject(nameof(TutorialAdimGoster));
            go.AddComponent<TutorialAdimGoster>();
        }

        private void Awake()
        {
            if (SceneManager.GetActiveScene().buildIndex != TUTORIAL_SAHNE_BUILD_INDEX)
            {
                gameObject.SetActive(false);
                return;
            }
            if (Ornek != null && Ornek != this) { Destroy(gameObject); return; }
            Ornek = this;

            if (UnityEngine.Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGO = new GameObject("EventSystem (Auto)");
                esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            UIYarat();
            Gizle();
            Debug.Log("[TutorialAdimGoster] Spawn: Ornek atandı, UIYarat tamamlandı.");
        }

        private void OnDestroy()
        {
            if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
            if (Ornek == this) Ornek = null;
        }

        // === Public API ===

        public void AdimGoster(int sira, string altBaslik, string[] yapilacaklar, string altSayac = null)
        {
            Debug.Log($"[TutorialAdimGoster] AdimGoster: sira={sira}, altBaslik={altBaslik}, yapilacaklar={yapilacaklar?.Length ?? 0}, altSayac={altSayac ?? "-"}");
            if (_root == null) return;
            _root.SetActive(true);

            if (_sayacText != null)
            {
                _sayacText.text = string.IsNullOrEmpty(altSayac)
                    ? $"ADIM {sira}/{TOPLAM_ADIM}"
                    : $"ADIM {sira}/{TOPLAM_ADIM} · {altSayac}";
            }
            if (_altBaslikText != null) _altBaslikText.text = altBaslik ?? "";

            bool yapVar = yapilacaklar != null && yapilacaklar.Length > 0;
            if (_yapilacaklarBlok != null) _yapilacaklarBlok.SetActive(yapVar);
            if (_ilerlemeBlok != null) _ilerlemeBlok.SetActive(yapVar);

            if (yapVar)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (_yapilacakSatirlari[i] == null) continue;
                    if (i < yapilacaklar.Length)
                    {
                        _yapilacakSatirlari[i].text = "→ " + yapilacaklar[i];
                        _yapilacakSatirlari[i].gameObject.SetActive(true);
                    }
                    else
                    {
                        _yapilacakSatirlari[i].gameObject.SetActive(false);
                    }
                }
                IlerlemeGuncelle(0, 0, false);
            }

            IleriAktif(false);

            // Pulse coroutine başlat (idempotent)
            if (_pulseCoroutine == null) _pulseCoroutine = StartCoroutine(PulseLoop());
        }

        public void IlerlemeGuncelle(int spinAtilan, int hedefSpin, bool parametreTamam)
        {
            if (_parametreText != null)
            {
                _parametreText.text = (parametreTamam ? "✓" : "⌛") + " Parametre: " + (parametreTamam ? "tamam" : "bekleniyor");
                _parametreText.color = parametreTamam ? YESIL : GRI;
            }
            if (_spinText != null)
            {
                if (hedefSpin > 0)
                {
                    bool spinTamam = spinAtilan >= hedefSpin;
                    _spinText.text = (spinTamam ? "✓" : "⌛") + $" Spin: {Mathf.Min(spinAtilan, hedefSpin)}/{hedefSpin}";
                    _spinText.color = spinTamam ? YESIL : GRI;
                }
                else
                {
                    _spinText.text = "—";
                    _spinText.color = GRI;
                }
            }
        }

        public void IleriAktif(bool aktif)
        {
            Debug.Log($"[TutorialAdimGoster] IleriAktif: {aktif}");
            if (_ileriButton == null) return;
            _ileriButton.interactable = aktif;
            IleriZatenAktif = aktif;

            // Pasif: BUTON_ARKA + beyaz text + 0.45 alpha
            // Aktif: ALTIN_RENK zemin + KOYU_YAZI text + 1.0 alpha
            if (_ileriButtonImg != null)
            {
                _ileriButtonImg.color = aktif ? ALTIN_RENK : BUTON_ARKA;
            }
            if (_ileriButtonTxt != null)
            {
                _ileriButtonTxt.color = aktif ? KOYU_YAZI : new Color(1f, 1f, 1f, 0.6f);
            }
        }

        public void Gizle()
        {
            if (_root != null) _root.SetActive(false);
            if (_pulseCoroutine != null) { StopCoroutine(_pulseCoroutine); _pulseCoroutine = null; }
            if (_panelRt != null) _panelRt.localScale = Vector3.one;
        }

        // === Pulse coroutine — subtle dikkat çekme (SpinButtonAnimator pattern) ===

        private IEnumerator PulseLoop()
        {
            if (_panelRt == null) yield break;
            Vector3 baseScale = Vector3.one;
            float t = 0f;
            while (_panelRt != null)
            {
                t += Time.unscaledDeltaTime;
                float u = (t % PULSE_PERIYOT) / PULSE_PERIYOT;
                float ping = Mathf.PingPong(u * 2f, 1f);
                float ease = ping * ping * (3f - 2f * ping); // smoothstep
                _panelRt.localScale = baseScale * (1f + ease * (PULSE_OLCEK - 1f));
                yield return null;
            }
        }

        // === UI yaratımı — 280×320 panel + premium çerçeve + header bandı ===

        private void UIYarat()
        {
            _root = new GameObject("TutorialAdimGosterCanvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _root.transform.SetParent(transform, false);

            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CANVAS_SORTING_ORDER;

            var scaler = _root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Ana panel — body navy
            var panel = new GameObject("Panel",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(_root.transform, false);
            _panelRt = panel.GetComponent<RectTransform>();
            _panelRt.anchorMin = _panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            _panelRt.pivot = new Vector2(0.5f, 0.5f);
            _panelRt.sizeDelta = new Vector2(280f, 320f);
            _panelRt.anchoredPosition = new Vector2(801.18f, 222f);
            panel.GetComponent<Image>().color = BALON_RENK;

            // 2 katmanlı altın border
            BorderEkle(panel.transform, _panelRt.sizeDelta, 3f, ALTIN_KOYU);          // DIŞ 3px koyu altın
            BorderEkleOfsetli(panel.transform, 3f, 2f, ALTIN_ACIK);                    // İÇ 2px açık altın (3px ofset)

            // === HEADER bandı (üst 55px) ===
            var header = new GameObject("Header", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            header.transform.SetParent(panel.transform, false);
            var headerRt = header.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.sizeDelta = new Vector2(-10f, 55f); // panel iç-genişlikten 5px (her tarafta) içeri
            headerRt.anchoredPosition = new Vector2(0f, -5f);
            header.GetComponent<Image>().color = BALON_KOYU;

            // Header alt aksan çizgisi (2px ALTIN_ACIK)
            CizgiEkle(panel.transform, new Vector2(0f, -60f), new Vector2(240f, 2f), ALTIN_ACIK);

            // Sayaç başlık (header içinde, fontSize 22 + outline)
            _sayacText = MetinYarat(header.transform, "Baslik", new Vector2(0f, -6f),
                new Vector2(260f, 28f), 18f, FontStyles.Bold, ALTIN_ACIK,
                TextAlignmentOptions.Center, "ADIM ?/11");
            _sayacText.outlineWidth = 0.18f;
            _sayacText.outlineColor = ALTIN_KOYU;

            // Alt başlık (header alt yarısı)
            _altBaslikText = MetinYarat(header.transform, "AltBaslik", new Vector2(0f, -32f),
                new Vector2(260f, 20f), 14f, FontStyles.Bold, ALTIN_RENK,
                TextAlignmentOptions.Center, "");
            _altBaslikText.characterSpacing = 4f;

            // === YAPILACAKLAR BLOK ===
            _yapilacaklarBlok = new GameObject("YapilacaklarBlok", typeof(RectTransform));
            _yapilacaklarBlok.transform.SetParent(panel.transform, false);
            var ybRt = _yapilacaklarBlok.GetComponent<RectTransform>();
            ybRt.anchorMin = ybRt.anchorMax = new Vector2(0.5f, 1f);
            ybRt.pivot = new Vector2(0.5f, 1f);
            ybRt.sizeDelta = new Vector2(280f, 110f);
            ybRt.anchoredPosition = new Vector2(0f, -68f);

            MetinYarat(_yapilacaklarBlok.transform, "Baslik_NeYapmali", new Vector2(0f, 0f),
                new Vector2(240f, 22f), 13f, FontStyles.Bold, BEYAZ,
                TextAlignmentOptions.Left, "NE YAPMALISIN:");

            for (int i = 0; i < 3; i++)
            {
                _yapilacakSatirlari[i] = MetinYarat(_yapilacaklarBlok.transform, $"Yap{i}",
                    new Vector2(10f, -26f - i * 25f), new Vector2(230f, 22f), 13f,
                    FontStyles.Normal, GRI, TextAlignmentOptions.Left, "");
            }

            // === Ayraç (yapılacaklar - ilerleme arası) ===
            CizgiEkle(panel.transform, new Vector2(0f, -188f), new Vector2(240f, 1f), ALTIN_KOYU);

            // === İLERLEME BLOK ===
            _ilerlemeBlok = new GameObject("IlerlemeBlok", typeof(RectTransform));
            _ilerlemeBlok.transform.SetParent(panel.transform, false);
            var ibRt = _ilerlemeBlok.GetComponent<RectTransform>();
            ibRt.anchorMin = ibRt.anchorMax = new Vector2(0.5f, 1f);
            ibRt.pivot = new Vector2(0.5f, 1f);
            ibRt.sizeDelta = new Vector2(280f, 75f);
            ibRt.anchoredPosition = new Vector2(0f, -193f);

            MetinYarat(_ilerlemeBlok.transform, "Baslik_Ilerleme", new Vector2(0f, 0f),
                new Vector2(240f, 22f), 13f, FontStyles.Bold, BEYAZ,
                TextAlignmentOptions.Left, "İLERLEME:");

            _parametreText = MetinYarat(_ilerlemeBlok.transform, "Parametre",
                new Vector2(10f, -26f), new Vector2(230f, 22f), 13f, FontStyles.Normal, GRI,
                TextAlignmentOptions.Left, "⌛ Parametre: bekleniyor");

            _spinText = MetinYarat(_ilerlemeBlok.transform, "Spin",
                new Vector2(10f, -50f), new Vector2(230f, 22f), 13f, FontStyles.Normal, GRI,
                TextAlignmentOptions.Left, "—");

            // === Ayraç (ilerleme - İLERİ buton arası) ===
            CizgiEkle(panel.transform, new Vector2(0f, -273f), new Vector2(240f, 1f), ALTIN_KOYU);

            // === İLERİ butonu ===
            var btnGo = new GameObject("IleriButon",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(panel.transform, false);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = btnRt.anchorMax = new Vector2(1f, 0f);
            btnRt.pivot = new Vector2(1f, 0f);
            btnRt.sizeDelta = new Vector2(110f, 32f);
            btnRt.anchoredPosition = new Vector2(-12f, 8f);
            _ileriButtonImg = btnGo.GetComponent<Image>();
            _ileriButtonImg.color = BUTON_ARKA;
            BorderEkle(btnGo.transform, btnRt.sizeDelta, 1.5f, ALTIN_RENK);
            _ileriButton = btnGo.GetComponent<Button>();
            _ileriButton.transition = Selectable.Transition.None;
            _ileriButton.onClick.AddListener(() => OnIleriTiklandi?.Invoke());

            var btnTxtGo = new GameObject("Txt", typeof(RectTransform), typeof(CanvasRenderer));
            btnTxtGo.transform.SetParent(btnGo.transform, false);
            var btnTxtRt = btnTxtGo.GetComponent<RectTransform>();
            btnTxtRt.anchorMin = Vector2.zero; btnTxtRt.anchorMax = Vector2.one;
            btnTxtRt.offsetMin = btnTxtRt.offsetMax = Vector2.zero;
            _ileriButtonTxt = btnTxtGo.AddComponent<TextMeshProUGUI>();
            _ileriButtonTxt.alignment = TextAlignmentOptions.Center;
            _ileriButtonTxt.fontSize = 16f;
            _ileriButtonTxt.fontStyle = FontStyles.Bold;
            _ileriButtonTxt.color = new Color(1f, 1f, 1f, 0.6f); // pasif başlangıç
            _ileriButtonTxt.text = "İLERİ →";
            _ileriButtonTxt.raycastTarget = false;
        }

        // === Yardımcılar ===

        private static TextMeshProUGUI MetinYarat(Transform parent, string adi, Vector2 pos,
            Vector2 size, float fontSize, FontStyles style, Color renk,
            TextAlignmentOptions hizalama, string baslangicMetin)
        {
            var go = new GameObject(adi, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var txt = go.AddComponent<TextMeshProUGUI>();
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = renk;
            txt.alignment = hizalama;
            txt.text = baslangicMetin;
            txt.raycastTarget = false;
            txt.enableWordWrapping = false;
            txt.overflowMode = TextOverflowModes.Ellipsis;
            return txt;
        }

        private static void CizgiEkle(Transform parent, Vector2 pos, Vector2 size, Color renk)
        {
            var go = new GameObject("Ayrac", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var img = go.GetComponent<Image>();
            img.color = renk;
            img.raycastTarget = false;
        }

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
                var go = new GameObject("Border_" + k.ad,
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
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

        /// <summary>
        /// İç border (panel kenarından `kenarOfseti` kadar içeride). Anchor stretch + offset.
        /// </summary>
        private static void BorderEkleOfsetli(Transform parent, float kenarOfseti, float kalinlik, Color renk)
        {
            (string ad, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPos)[] kenarlar =
            {
                ("Ust",  new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-2f * kenarOfseti, kalinlik), new Vector2(0f, -kenarOfseti)),
                ("Alt",  new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-2f * kenarOfseti, kalinlik), new Vector2(0f, kenarOfseti)),
                ("Sol",  new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(kalinlik, -2f * kenarOfseti), new Vector2(kenarOfseti, 0f)),
                ("Sag",  new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(kalinlik, -2f * kenarOfseti), new Vector2(-kenarOfseti, 0f)),
            };
            foreach (var k in kenarlar)
            {
                var go = new GameObject("BorderIc_" + k.ad,
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(parent, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = k.anchorMin;
                rt.anchorMax = k.anchorMax;
                rt.sizeDelta = k.sizeDelta;
                rt.anchoredPosition = k.anchoredPos;
                var img = go.GetComponent<Image>();
                img.color = renk;
                img.raycastTarget = false;
            }
        }
    }
}
