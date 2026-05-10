using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace KumarFarkindalik.Tutorial
{
    /// <summary>
    /// Sağ-üst köşede "T#/11 + İLERİ" gösterge UI'ı. Modal kapanınca görünür, koşul sağlanınca
    /// İLERİ aktif olur. Self-spawn pattern (build idx 3).
    /// </summary>
    [Preserve]
    public class TutorialAdimGoster : MonoBehaviour
    {
        public const int TUTORIAL_SAHNE_BUILD_INDEX = 3;
        public const int CANVAS_SORTING_ORDER = 1650; // panel.html (DOM) ile aynı seviyede kalır, modal (1500) üstü

        public static TutorialAdimGoster Ornek { get; private set; }

        // ScriptedModalKopru ile birebir paleti
        private static readonly Color BALON_RENK = new Color(0.10f, 0.16f, 0.23f, 0.95f);
        private static readonly Color ALTIN_RENK = new Color(0.83f, 0.69f, 0.22f, 1f);
        private static readonly Color BUTON_ARKA = new Color(0.12f, 0.12f, 0.12f, 0.75f);
        private static readonly Color BUTON_BORDER_CTA = new Color(0.98f, 0.78f, 0.46f, 1f);

        private const int TOPLAM_ADIM = 11;

        public event Action OnIleriTiklandi;

        private GameObject _root;
        private TextMeshProUGUI _sayacText;
        private Button _ileriButton;
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
        }

        private void OnDestroy()
        {
            if (Ornek == this) Ornek = null;
        }

        // === Public API ===

        public void AdimGoster(int sira)
        {
            if (_root == null) return;
            _root.SetActive(true);
            if (_sayacText != null) _sayacText.text = $"T{sira}/{TOPLAM_ADIM}";
            IleriAktif(false);
        }

        public void IleriAktif(bool aktif)
        {
            if (_ileriButton == null) return;
            _ileriButton.interactable = aktif;
            IleriZatenAktif = aktif;
            // İLERİ aktif olunca buton parlat (alpha 1), pasif iken hafif gri
            var img = _ileriButton.GetComponent<Image>();
            if (img != null)
            {
                var c = img.color;
                c.a = aktif ? 1f : 0.45f;
                img.color = c;
            }
        }

        public void Gizle()
        {
            if (_root != null) _root.SetActive(false);
        }

        // === UI yaratımı (sağ-üst 220×80) ===

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

            // Panel arka — sağ-üst
            var panel = new GameObject("Panel",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(_root.transform, false);
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(1f, 1f);
            panelRt.anchorMax = new Vector2(1f, 1f);
            panelRt.pivot = new Vector2(1f, 1f);
            panelRt.sizeDelta = new Vector2(220f, 80f);
            panelRt.anchoredPosition = new Vector2(-30f, -30f);
            panel.GetComponent<Image>().color = BALON_RENK;
            BorderEkle(panel.transform, panelRt.sizeDelta, 2f, ALTIN_RENK);

            // Sayaç metni (sol-orta)
            var sayacGo = new GameObject("Sayac", typeof(RectTransform), typeof(CanvasRenderer));
            sayacGo.transform.SetParent(panel.transform, false);
            var sayacRt = sayacGo.GetComponent<RectTransform>();
            sayacRt.anchorMin = new Vector2(0f, 0f);
            sayacRt.anchorMax = new Vector2(0.45f, 1f);
            sayacRt.offsetMin = new Vector2(12f, 0f);
            sayacRt.offsetMax = Vector2.zero;
            _sayacText = sayacGo.AddComponent<TextMeshProUGUI>();
            _sayacText.alignment = TextAlignmentOptions.Left;
            _sayacText.fontSize = 22f;
            _sayacText.fontStyle = FontStyles.Bold;
            _sayacText.color = ALTIN_RENK;
            _sayacText.text = "T?/11";
            _sayacText.raycastTarget = false;

            // İLERİ butonu (sağ-orta)
            var btnGo = new GameObject("IleriButon",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(panel.transform, false);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(1f, 0.5f);
            btnRt.anchorMax = new Vector2(1f, 0.5f);
            btnRt.pivot = new Vector2(1f, 0.5f);
            btnRt.sizeDelta = new Vector2(100f, 36f);
            btnRt.anchoredPosition = new Vector2(-12f, 0f);
            var btnImg = btnGo.GetComponent<Image>();
            btnImg.color = BUTON_ARKA;
            BorderEkle(btnGo.transform, btnRt.sizeDelta, 1.5f, BUTON_BORDER_CTA);
            _ileriButton = btnGo.GetComponent<Button>();
            _ileriButton.transition = Selectable.Transition.None;
            _ileriButton.onClick.AddListener(() => OnIleriTiklandi?.Invoke());

            // İLERİ → metni
            var txtGo = new GameObject("Txt", typeof(RectTransform), typeof(CanvasRenderer));
            txtGo.transform.SetParent(btnGo.transform, false);
            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = txtRt.offsetMax = Vector2.zero;
            var txt = txtGo.AddComponent<TextMeshProUGUI>();
            txt.alignment = TextAlignmentOptions.Center;
            txt.fontSize = 18f;
            txt.fontStyle = FontStyles.Bold;
            txt.color = Color.white;
            txt.text = "İLERİ →";
            txt.raycastTarget = false;
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
    }
}
