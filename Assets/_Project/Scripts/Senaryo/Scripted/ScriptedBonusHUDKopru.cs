using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace Senaryo.Scripted
{
    /// <summary>
    /// Bonus oyun sırasında SAĞ ORTA'da (dikey ortalı) açılan HUD overlay.
    /// Önceki HTML iframe (jslib) ve sahne TMP fallback iki paralel implementasyon idi;
    /// artık tek bir runtime Unity Canvas kullanılıyor — hem Editor hem WebGL'de aynı görsel.
    ///
    /// Yapı:
    ///   - Tam ekran ScreenSpaceOverlay Canvas, sortingOrder 1400 (modal 1500'den altta)
    ///   - Sağ orta panel: anchor (1, 0.5), boyut 600×400 (eski 280×120'den 3x büyük)
    ///   - Koyu yarı saydam arka plan + kalın altın border
    ///   - Başlık "BONUS OYUN", kalan spin (büyük), oturum kazancı (yeşil)
    ///   - Subtle pulse animasyonu (her saniye scale 1.0 → 1.02 → 1.0) dikkat çeker
    ///
    /// Sahnedeki eski "HakText" / "BonusOyunKazancText" GameObject'leri silinebilir;
    /// OyunUIGuncellemeServisi null-guard ile sessiz atlar.
    /// </summary>
    [Preserve]
    public class ScriptedBonusHUDKopru : MonoBehaviour
    {
        public const int ANLATICI_SAHNE_BUILD_INDEX = 2;
        public static ScriptedBonusHUDKopru Ornek { get; private set; }

        // === UI referansları ===
        private GameObject _root;
        private RectTransform _panelRt;
        private CanvasGroup _panelCg;
        private TextMeshProUGUI _kalanSpinText;
        private TextMeshProUGUI _kazancText;

        // === State ===
        private bool _aktif;
        private Coroutine _pulseCoroutine;

        // === Görsel sabitler ===
        // Yarıya küçültüldü (sağ orta → sol orta). Bonus oyununda anlatici iframe gizli
        // olduğundan sol orta köşe rahat oturur, dikkat çeker ama dağınık değildir.
        private const float PANEL_GENISLIK = 300f;
        private const float PANEL_YUKSEKLIK = 200f;
        private const float PULSE_PERIYOT = 1.0f;
        private const float PULSE_OLCEK = 1.02f;

        [Preserve]
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OtomatikInit()
        {
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
            var go = new GameObject(nameof(ScriptedBonusHUDKopru));
            go.AddComponent<ScriptedBonusHUDKopru>();
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
            UIYarat();
            if (_root != null) _root.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Ornek == this) Ornek = null;
            _aktif = false;
        }

        // ──────────────────────────────────────────────────────────────────────
        // PUBLIC API
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>HUD'u açar ve ilk değerleri yazar (typically Goster(10, 0)).</summary>
        public void Goster(int kalanSpin, int oturumKazanci)
        {
            _aktif = true;
            if (_root != null) _root.SetActive(true);
            Guncelle(kalanSpin, oturumKazanci);
            if (_pulseCoroutine == null)
                _pulseCoroutine = StartCoroutine(PulseAnimasyon());
            Debug.Log($"[ScriptedBonusHUD] Açıldı — spin {kalanSpin}, kazanç {oturumKazanci} TL.");
        }

        /// <summary>HUD'u kapatır.</summary>
        public void Gizle()
        {
            _aktif = false;
            if (_root != null) _root.SetActive(false);
            if (_pulseCoroutine != null)
            {
                StopCoroutine(_pulseCoroutine);
                _pulseCoroutine = null;
            }
            if (_panelRt != null) _panelRt.localScale = Vector3.one;
            Debug.Log("[ScriptedBonusHUD] Gizlendi.");
        }

        /// <summary>HUD'da kalan spin ve toplam oturum kazancını günceller.</summary>
        public void Guncelle(int kalanSpin, int oturumKazanci)
        {
            if (!_aktif) return;
            if (_kalanSpinText != null)
                _kalanSpinText.text = $"<size=14><color=#BFBFBF>Kalan Spin Hakkı</color></size>\n<size=30><b>{kalanSpin} / 10</b></size>";
            if (_kazancText != null)
            {
                string formatlanmis = oturumKazanci >= 1000
                    ? oturumKazanci.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("tr-TR"))
                    : oturumKazanci.ToString();
                _kazancText.text = $"<size=14><color=#BFBFBF>Oturum Kazancı</color></size>\n<size=28><b>{formatlanmis} TL</b></size>";
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // ANİMASYON
        // ──────────────────────────────────────────────────────────────────────

        private IEnumerator PulseAnimasyon()
        {
            while (_aktif)
            {
                if (_panelRt == null) { yield return null; continue; }
                // 0 → 0.5 saniye: 1.0 → 1.02
                float t = 0f;
                while (t < PULSE_PERIYOT * 0.5f && _aktif)
                {
                    t += Time.unscaledDeltaTime;
                    float u = Mathf.Clamp01(t / (PULSE_PERIYOT * 0.5f));
                    float s = Mathf.Lerp(1f, PULSE_OLCEK, u);
                    _panelRt.localScale = new Vector3(s, s, 1f);
                    yield return null;
                }
                // 0.5 → 1.0 saniye: 1.02 → 1.0
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

        // ──────────────────────────────────────────────────────────────────────
        // UI YARATMA
        // ──────────────────────────────────────────────────────────────────────

        private void UIYarat()
        {
            _root = new GameObject("ScriptedBonusHUDCanvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _root.transform.SetParent(transform, false);
            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // sortingOrder 1400 — ScriptedModalKopru (1500) ve diğer overlay'lerin ALTINDA.
            // Modal açılınca HUD altta kalır; modal kapanınca tekrar görünür kalır.
            canvas.sortingOrder = 1400;
            var scaler = _root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            // GraphicRaycaster aktif kalsın ama panel raycastTarget false (tıklamaları yutmasın).

            // Panel — SOL orta dikey ortalı (anlatici iframe gizli olduğu için sol köşe boşta)
            var panel = new GameObject("HUDPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            panel.transform.SetParent(_root.transform, false);
            _panelRt = panel.GetComponent<RectTransform>();
            _panelRt.anchorMin = new Vector2(0f, 0.5f);
            _panelRt.anchorMax = new Vector2(0f, 0.5f);
            _panelRt.pivot = new Vector2(0f, 0.5f);
            _panelRt.sizeDelta = new Vector2(PANEL_GENISLIK, PANEL_YUKSEKLIK);
            _panelRt.anchoredPosition = new Vector2(30f, 0f); // soldan 30px iç
            var panelImg = panel.GetComponent<Image>();
            panelImg.color = new Color(0.04f, 0.04f, 0.06f, 0.85f); // koyu yarı saydam
            panelImg.raycastTarget = false;
            _panelCg = panel.GetComponent<CanvasGroup>();
            _panelCg.alpha = 1f;

            // Border — orta kalın altın (yarıya küçültülmüş için 3px yeterli)
            BorderEkle(panel.transform, _panelRt.sizeDelta, 3f, new Color(1f, 0.84f, 0f, 1f));

            // Başlık "BONUS OYUN" — üstte, altın renk
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
            basTxt.color = new Color(1f, 0.84f, 0f, 1f); // altın
            basTxt.text = "BONUS OYUN";
            basTxt.raycastTarget = false;

            // Ayırıcı çizgi (başlık altı)
            var ayirGo = new GameObject("Ayirici", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            ayirGo.transform.SetParent(panel.transform, false);
            var ayirRt = ayirGo.GetComponent<RectTransform>();
            ayirRt.anchorMin = new Vector2(0.1f, 1f);
            ayirRt.anchorMax = new Vector2(0.9f, 1f);
            ayirRt.pivot = new Vector2(0.5f, 1f);
            ayirRt.sizeDelta = new Vector2(0f, 1f);
            ayirRt.anchoredPosition = new Vector2(0f, -46f);
            var ayirImg = ayirGo.GetComponent<Image>();
            ayirImg.color = new Color(1f, 0.84f, 0f, 0.5f);
            ayirImg.raycastTarget = false;

            // Kalan spin — orta üst, beyaz
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

            // Oturum kazancı — orta alt, parlayan yeşil
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
            _kazancText.color = new Color(0.0f, 1f, 0.5f, 1f); // ışıltılı yeşil
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
    }
}
