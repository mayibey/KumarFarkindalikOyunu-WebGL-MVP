// ⚠️ GEÇİCİ DEBUG PANELİ
// Sadece geliştirme/test sırasında kullanılır. Sağ üst köşede 🛠 toggle butonu;
// tıklayınca aşama atlama listesi açılır. Editor veya Debug build dışında oluşturulmaz.
// Final sürümde bu dosya silinmeli ve OtomatikInit() kaldırılmalı.

using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Senaryo.Scripted
{
    public class ScriptedDebugAtlamaPaneli : MonoBehaviour
    {
        public const int ANLATICI_SAHNE_BUILD_INDEX = 2;
        public static ScriptedDebugAtlamaPaneli Ornek { get; private set; }

        private GameObject _root;
        private GameObject _panelRoot;

        /// <summary>
        /// ⚠️ GEÇİCİ DEVRE DIŞI — sahne UI kaybı bug raporu sonrası izolasyon testi için.
        /// Sebep tespit edilince <see cref="DEBUG_PANEL_AKTIF"/> true yapılarak yeniden açılabilir.
        /// </summary>
        private const bool DEBUG_PANEL_AKTIF = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OtomatikInit()
        {
            // GEÇİCİ — kullanıcı raporu: panel yaratılınca sahne UI (slot grid + butonlar) kayboluyor.
            // Bu flag false iken panel HİÇ oluşturulmaz → kullanıcı sahnede UI'ın dönüp dönmediğini test edebilir.
            // UI dönerse: sebep bu paneldi → izolasyon iyileştirmesi gerekecek.
            // UI dönmezse: sebep başka bir kod yolunda → ayrı araştırma.
            if (!DEBUG_PANEL_AKTIF) return;

            // Sadece Editor veya Debug build'de oluştur
            if (!Application.isEditor && !Debug.isDebugBuild) return;
            if (SceneManager.GetActiveScene().buildIndex != ANLATICI_SAHNE_BUILD_INDEX) return;
            if (Ornek != null) return;
            var go = new GameObject(nameof(ScriptedDebugAtlamaPaneli));
            go.AddComponent<ScriptedDebugAtlamaPaneli>();
        }

        private void Awake()
        {
            if (!Application.isEditor && !Debug.isDebugBuild)
            {
                gameObject.SetActive(false);
                return;
            }
            if (SceneManager.GetActiveScene().buildIndex != ANLATICI_SAHNE_BUILD_INDEX)
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
            if (_panelRoot != null) _panelRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Ornek == this) Ornek = null;
        }

        // ──────────────────────────────────────────────────────────────────────
        // Atlama eylemleri
        // ──────────────────────────────────────────────────────────────────────

        private void Atla(string etiket, int asama, int spin0Indexed, int bakiye)
        {
            try
            {
                var oy = UnityEngine.Object.FindObjectOfType<OyunYoneticisi>();
                if (oy != null)
                {
                    oy.AnlaticiBakiyeyiSifirla(bakiye);
                }

                if (ScriptedSpinYoneticisi.Ornek != null)
                {
                    ScriptedSpinYoneticisi.Ornek.DebugAsamaSpinSet(asama, spin0Indexed);
                }

                Debug.Log($"[Debug] {etiket} → asama={asama + 1}, spin={spin0Indexed + 1}, bakiye={bakiye} TL");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Debug] Atlama hatası ({etiket}): {e.Message}");
            }
        }

        private void SahneReset()
        {
            Debug.Log("[Debug] Sahne reset.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void TogglePanel()
        {
            if (_panelRoot == null) return;
            _panelRoot.SetActive(!_panelRoot.activeSelf);
        }

        // ──────────────────────────────────────────────────────────────────────
        // Runtime UI
        // ──────────────────────────────────────────────────────────────────────

        private void UIYarat()
        {
            // Root canvas (en üst sortingOrder ki final ekran haricinde diğer overlaylerin üstünde gözüksün)
            _root = new GameObject("ScriptedDebugAtlamaCanvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _root.transform.SetParent(transform, false);
            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1900; // Final ekran 1800'den sonra
            var scaler = _root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Toggle button (sağ üst köşe — her zaman görünür)
            var toggleGo = new GameObject("DebugToggle",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            toggleGo.transform.SetParent(_root.transform, false);
            var toggleRt = toggleGo.GetComponent<RectTransform>();
            toggleRt.anchorMin = toggleRt.anchorMax = new Vector2(1f, 1f);
            toggleRt.pivot = new Vector2(1f, 1f);
            toggleRt.sizeDelta = new Vector2(50f, 50f);
            toggleRt.anchoredPosition = new Vector2(-12f, -12f);
            toggleGo.GetComponent<Image>().color = new Color(0.13f, 0.13f, 0.13f, 0.85f);
            var toggleBtn = toggleGo.GetComponent<Button>();
            toggleBtn.onClick.AddListener(TogglePanel);

            var toggleTxtGo = new GameObject("ToggleTxt", typeof(RectTransform), typeof(CanvasRenderer));
            toggleTxtGo.transform.SetParent(toggleGo.transform, false);
            var toggleTxtRt = toggleTxtGo.GetComponent<RectTransform>();
            toggleTxtRt.anchorMin = Vector2.zero; toggleTxtRt.anchorMax = Vector2.one;
            toggleTxtRt.offsetMin = toggleTxtRt.offsetMax = Vector2.zero;
            var toggleTxt = toggleTxtGo.AddComponent<TextMeshProUGUI>();
            toggleTxt.alignment = TextAlignmentOptions.Center;
            toggleTxt.fontSize = 26f;
            toggleTxt.color = Color.white;
            toggleTxt.text = "🛠";
            toggleTxt.raycastTarget = false;

            // Panel (toggle ile aç/kapat)
            _panelRoot = new GameObject("DebugPanel",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            _panelRoot.transform.SetParent(_root.transform, false);
            var panelRt = _panelRoot.GetComponent<RectTransform>();
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(1f, 1f);
            panelRt.pivot = new Vector2(1f, 1f);
            panelRt.sizeDelta = new Vector2(220f, 0f); // genişlik sabit, yükseklik içeriğe göre
            panelRt.anchoredPosition = new Vector2(-12f, -74f);
            _panelRoot.GetComponent<Image>().color = new Color(0.13f, 0.13f, 0.13f, 0.92f);

            var vlg = _panelRoot.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.spacing = 4f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;

            var csf = _panelRoot.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // Başlık
            BasilkEkle("🛠 DEBUG ATLAMA");

            // Atlama butonları (asama 0-indexed, spin 0-indexed)
            ButonEkle("A1 Başla (50K)",        () => Atla("A1 Başla",         0, 0, 50000));
            ButonEkle("A2 Başla (60K)",        () => Atla("A2 Başla",         1, 0, 60000));
            ButonEkle("A3 Başla (56K)",        () => Atla("A3 Başla",         2, 0, 55750));
            ButonEkle("A4 Başla (44.5K)",      () => Atla("A4 Başla",         3, 0, 44500));
            ButonEkle("A5 Başla (59.5K)",      () => Atla("A5 Başla",         4, 0, 59500));
            ButonEkle("A5 Spin 4 (BONUS, 800)", () => Atla("A5 Spin 4 BONUS",  4, 3, 800));
            ButonEkle("A6 Başla (Yükle, 800)", () => Atla("A6 Başla — Yükleme", 5, 0, 800));
            ButonEkle("A6 Bitiş (50.8K→0)",    () => Atla("A6 Bitiş yakını",  5, 19, 2500));
            ButonEkle("A7 Final Direkt",       () => Atla("A7 Final",         6, 0, 0));
            ButonEkle("Sahne Reset",           SahneReset);
        }

        private void BasilkEkle(string metin)
        {
            var go = new GameObject("Baslik", typeof(RectTransform), typeof(CanvasRenderer), typeof(LayoutElement));
            go.transform.SetParent(_panelRoot.transform, false);
            var le = go.GetComponent<LayoutElement>();
            le.minHeight = 30f; le.preferredHeight = 30f;
            var txt = go.AddComponent<TextMeshProUGUI>();
            txt.alignment = TextAlignmentOptions.Center;
            txt.fontSize = 14f;
            txt.fontStyle = FontStyles.Bold;
            txt.color = new Color(1f, 0.85f, 0.2f, 1f);
            txt.text = metin;
            txt.raycastTarget = false;
        }

        private void ButonEkle(string etiket, System.Action onClick)
        {
            var go = new GameObject("Btn_" + etiket,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(_panelRoot.transform, false);
            var le = go.GetComponent<LayoutElement>();
            le.minHeight = 32f; le.preferredHeight = 32f;
            var img = go.GetComponent<Image>();
            img.color = new Color(0.30f, 0.30f, 0.32f, 1f);

            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.normalColor      = new Color(0.30f, 0.30f, 0.32f, 1f);
            colors.highlightedColor = new Color(0.42f, 0.42f, 0.46f, 1f);
            colors.pressedColor     = new Color(0.20f, 0.20f, 0.22f, 1f);
            colors.selectedColor    = colors.highlightedColor;
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var txtGo = new GameObject("Txt", typeof(RectTransform), typeof(CanvasRenderer));
            txtGo.transform.SetParent(go.transform, false);
            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(6f, 0f); txtRt.offsetMax = new Vector2(-6f, 0f);
            var txt = txtGo.AddComponent<TextMeshProUGUI>();
            txt.alignment = TextAlignmentOptions.Center;
            txt.fontSize = 12f;
            txt.color = Color.white;
            txt.text = etiket;
            txt.raycastTarget = false;
        }
    }
}
