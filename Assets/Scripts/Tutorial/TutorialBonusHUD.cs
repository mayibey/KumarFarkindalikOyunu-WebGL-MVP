using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace KumarFarkindalik.Tutorial
{
    /// <summary>
    /// T11 "Bonus Tetikle" sonrasında bonus oyun aktifken sağ-orta'da görünen HUD.
    /// ScriptedBonusHUDKopru görsel stil aynası (SOL → SAĞ konum + BUILD_INDEX 3).
    /// Veri akışı POLLING: OyunYoneticisi.{BonusAktifMi, BonusHakKalan, OturumKazanc} public field/property.
    /// Görev paneli sortingOrder 1650 altı (1640) — modal 1500 üstü.
    /// </summary>
    [Preserve]
    public class TutorialBonusHUD : MonoBehaviour
    {
        public const int TUTORIAL_SAHNE_BUILD_INDEX = 3;
        public const int CANVAS_SORTING_ORDER = 1640;

        public static TutorialBonusHUD Ornek { get; private set; }

        private const float PANEL_GENISLIK = 300f;
        private const float PANEL_YUKSEKLIK = 200f;
        private const float PULSE_PERIYOT = 1.0f;
        private const float PULSE_OLCEK = 1.02f;

        // Renkler — ScriptedBonusHUDKopru ile uyumlu
        private static readonly Color PANEL_ZEMIN = new Color(0.06f, 0.10f, 0.14f, 0.92f);
        private static readonly Color ALTIN = new Color(0.95f, 0.80f, 0.30f, 1f);
        private static readonly Color ALTIN_YARI = new Color(0.95f, 0.80f, 0.30f, 0.5f);

        // UI referansları
        private GameObject _root;
        private RectTransform _panelRt;
        private TextMeshProUGUI _kalanSpinText;
        private TextMeshProUGUI _kazancText;

        // State
        private bool _aktif;
        private Coroutine _pulseCoroutine;
        private OyunYoneticisi _oy;

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
            var go = new GameObject(nameof(TutorialBonusHUD));
            go.AddComponent<TutorialBonusHUD>();
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

            UIYarat();
            if (_root != null) _root.SetActive(false);
            Debug.Log("[TutorialBonusHUD] Spawn: Ornek atandı, UIYarat tamamlandı.");
        }

        private void OnDestroy()
        {
            if (Ornek == this) Ornek = null;
            _aktif = false;
        }

        private void Update()
        {
            // OyunYoneticisi referansı (lazy)
            if (_oy == null)
            {
                _oy = Object.FindObjectOfType<OyunYoneticisi>();
                if (_oy == null) return;
            }

            bool bonusAktif = _oy.BonusAktifMi;

            if (bonusAktif && !_aktif)
            {
                _aktif = true;
                if (_root != null) _root.SetActive(true);
                if (_pulseCoroutine == null) _pulseCoroutine = StartCoroutine(PulseLoop());
            }
            else if (!bonusAktif && _aktif)
            {
                _aktif = false;
                if (_root != null) _root.SetActive(false);
                if (_pulseCoroutine != null) { StopCoroutine(_pulseCoroutine); _pulseCoroutine = null; }
                if (_panelRt != null) _panelRt.localScale = Vector3.one;
            }

            if (_aktif)
            {
                if (_kalanSpinText != null)
                    _kalanSpinText.text = $"<size=14><color=#BFBFBF>Kalan Spin Hakkı</color></size>\n<size=30><b>{_oy.BonusHakKalan} / 10</b></size>";
                if (_kazancText != null)
                {
                    int kazanc = _oy.OturumKazanc;
                    string formatli = kazanc >= 1000
                        ? kazanc.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("tr-TR"))
                        : kazanc.ToString();
                    _kazancText.text = $"<size=14><color=#BFBFBF>Oturum Kazancı</color></size>\n<size=28><b>{formatli} TL</b></size>";
                }
            }
        }

        // === Pulse animasyon (ScriptedBonusHUDKopru ile aynı) ===

        private IEnumerator PulseLoop()
        {
            while (_aktif)
            {
                if (_panelRt == null) { yield return null; continue; }
                float t = 0f;
                while (t < PULSE_PERIYOT * 0.5f && _aktif)
                {
                    t += Time.unscaledDeltaTime;
                    float u = Mathf.Clamp01(t / (PULSE_PERIYOT * 0.5f));
                    float s = Mathf.Lerp(1f, PULSE_OLCEK, u);
                    _panelRt.localScale = new Vector3(s, s, 1f);
                    yield return null;
                }
                t = 0f;
                while (t < PULSE_PERIYOT * 0.5f && _aktif)
                {
                    t += Time.unscaledDeltaTime;
                    float u = Mathf.Clamp01(t / (PULSE_PERIYOT * 0.5f));
                    float s = Mathf.Lerp(PULSE_OLCEK, 1f, u);
                    _panelRt.localScale = new Vector3(s, s, 1f);
                    yield return null;
                }
            }
            if (_panelRt != null) _panelRt.localScale = Vector3.one;
        }

        // === UI yaratımı — SAĞ ORTA (ScriptedBonusHUDKopru aynası) ===

        private void UIYarat()
        {
            _root = new GameObject("TutorialBonusHUDCanvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _root.transform.SetParent(transform, false);

            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CANVAS_SORTING_ORDER;

            var scaler = _root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Panel — SAĞ orta dikey ortalı (görev paneli zaten sağ-üst'te 280×320, bu altta görünür)
            var panel = new GameObject("HUDPanel",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            panel.transform.SetParent(_root.transform, false);
            _panelRt = panel.GetComponent<RectTransform>();
            _panelRt.anchorMin = new Vector2(1f, 0.5f);
            _panelRt.anchorMax = new Vector2(1f, 0.5f);
            _panelRt.pivot = new Vector2(1f, 0.5f);
            _panelRt.sizeDelta = new Vector2(PANEL_GENISLIK, PANEL_YUKSEKLIK);
            _panelRt.anchoredPosition = new Vector2(-30f, 0f); // sağdan 30px iç
            var panelImg = panel.GetComponent<Image>();
            panelImg.color = PANEL_ZEMIN;
            panelImg.raycastTarget = false;

            // 3px altın border
            BorderEkle(panel.transform, _panelRt.sizeDelta, 3f, ALTIN);

            // Başlık "BONUS OYUN"
            var basGo = new GameObject("Baslik", typeof(RectTransform), typeof(CanvasRenderer));
            basGo.transform.SetParent(panel.transform, false);
            var basRt = basGo.GetComponent<RectTransform>();
            basRt.anchorMin = new Vector2(0f, 1f);
            basRt.anchorMax = new Vector2(1f, 1f);
            basRt.pivot = new Vector2(0.5f, 1f);
            basRt.sizeDelta = new Vector2(0f, 32f);
            basRt.anchoredPosition = new Vector2(0f, -12f);
            var basTxt = basGo.AddComponent<TextMeshProUGUI>();
            basTxt.alignment = TextAlignmentOptions.Center;
            basTxt.fontSize = 18f;
            basTxt.fontStyle = FontStyles.Bold;
            basTxt.color = ALTIN;
            basTxt.text = "BONUS OYUN";
            basTxt.raycastTarget = false;

            // Ayırıcı çizgi
            var ayirGo = new GameObject("Ayirici", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            ayirGo.transform.SetParent(panel.transform, false);
            var ayirRt = ayirGo.GetComponent<RectTransform>();
            ayirRt.anchorMin = new Vector2(0.1f, 1f);
            ayirRt.anchorMax = new Vector2(0.9f, 1f);
            ayirRt.pivot = new Vector2(0.5f, 1f);
            ayirRt.sizeDelta = new Vector2(0f, 1f);
            ayirRt.anchoredPosition = new Vector2(0f, -46f);
            var ayirImg = ayirGo.GetComponent<Image>();
            ayirImg.color = ALTIN_YARI;
            ayirImg.raycastTarget = false;

            // Kalan spin — orta üst
            var spinGo = new GameObject("KalanSpin", typeof(RectTransform), typeof(CanvasRenderer));
            spinGo.transform.SetParent(panel.transform, false);
            var spinRt = spinGo.GetComponent<RectTransform>();
            spinRt.anchorMin = new Vector2(0f, 0.5f);
            spinRt.anchorMax = new Vector2(1f, 1f);
            spinRt.offsetMin = new Vector2(10f, 0f);
            spinRt.offsetMax = new Vector2(-10f, -50f);
            _kalanSpinText = spinGo.AddComponent<TextMeshProUGUI>();
            _kalanSpinText.alignment = TextAlignmentOptions.Center;
            _kalanSpinText.fontSize = 30f;
            _kalanSpinText.fontStyle = FontStyles.Normal;
            _kalanSpinText.richText = true;
            _kalanSpinText.color = Color.white;
            _kalanSpinText.text = "<size=14><color=#BFBFBF>Kalan Spin Hakkı</color></size>\n<size=30><b>10 / 10</b></size>";
            _kalanSpinText.raycastTarget = false;

            // Oturum kazancı — orta alt
            var kazGo = new GameObject("Kazanc", typeof(RectTransform), typeof(CanvasRenderer));
            kazGo.transform.SetParent(panel.transform, false);
            var kazRt = kazGo.GetComponent<RectTransform>();
            kazRt.anchorMin = new Vector2(0f, 0f);
            kazRt.anchorMax = new Vector2(1f, 0.5f);
            kazRt.offsetMin = new Vector2(10f, 15f);
            kazRt.offsetMax = new Vector2(-10f, 0f);
            _kazancText = kazGo.AddComponent<TextMeshProUGUI>();
            _kazancText.alignment = TextAlignmentOptions.Center;
            _kazancText.fontSize = 28f;
            _kazancText.fontStyle = FontStyles.Normal;
            _kazancText.richText = true;
            _kazancText.color = Color.white;
            _kazancText.text = "<size=14><color=#BFBFBF>Oturum Kazancı</color></size>\n<size=28><b>0 TL</b></size>";
            _kazancText.raycastTarget = false;
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
