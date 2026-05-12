using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace KumarFarkindalik.Tutorial
{
    /// <summary>
    /// PAKET 5 + HOTFIX (Bug 6): Spin sonu bakiye delta görsel feedback'i — basket animasyon.
    ///
    /// KAZANÇ akışı (5 aşama):
    ///   1. Spin tahta merkezinde +X TL belirir (scale 0.5→1.0)
    ///   2. Hold 0.5sn (kullanıcı okur)
    ///   3. Parabolic flight (Bezier curve) bakiye text'e doğru
    ///   4. Bakiye konumunda scale pulse (1.0→1.3→1.0)
    ///   5. Destroy
    ///
    /// KAYIP akışı (3 aşama):
    ///   1. Bakiye yakını -X TL belirir
    ///   2. Hold 0.3sn
    ///   3. Yukarı + yana flight + fade out
    /// </summary>
    [Preserve]
    public class TutorialKazancAnimasyon : MonoBehaviour
    {
        public const int BUILD_INDEX = 3;
        public const int CANVAS_SORTING_ORDER = 1700;

        public static TutorialKazancAnimasyon Ornek { get; private set; }

        private static readonly Color KAZANC_RENK = new Color(1f, 0.85f, 0.20f, 1f);
        private static readonly Color KAYIP_RENK  = new Color(0.95f, 0.30f, 0.25f, 1f);

        private OyunYoneticisi _oy;
        private int _oncekiBakiye = -1;
        private bool _oncekiSpinCalisiyor = false;
        private GameObject _canvasGo;
        private RectTransform _canvasRt;

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
            var go = new GameObject(nameof(TutorialKazancAnimasyon));
            go.AddComponent<TutorialKazancAnimasyon>();
        }

        private void Awake()
        {
            if (SceneManager.GetActiveScene().buildIndex != BUILD_INDEX)
            {
                gameObject.SetActive(false);
                return;
            }
            if (Ornek != null && Ornek != this) { Destroy(gameObject); return; }
            Ornek = this;
            CanvasYarat();
        }

        private void OnDestroy()
        {
            if (Ornek == this) Ornek = null;
        }

        private void CanvasYarat()
        {
            _canvasGo = new GameObject("TutorialKazancAnimCanvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _canvasGo.transform.SetParent(transform, false);

            var canvas = _canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CANVAS_SORTING_ORDER;

            var scaler = _canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            _canvasGo.GetComponent<GraphicRaycaster>().enabled = false;
            _canvasRt = _canvasGo.GetComponent<RectTransform>();
        }

        private void Update()
        {
            if (_oy == null)
            {
                _oy = Object.FindObjectOfType<OyunYoneticisi>();
                if (_oy == null) return;
                _oncekiBakiye = _oy.BotIcinBakiye;
            }

            bool simdi = _oy.SpinCalisiyorMu;

            if (_oncekiSpinCalisiyor && !simdi)
            {
                _oncekiSpinCalisiyor = false;
                int yeniBakiye = _oy.BotIcinBakiye;
                int delta = yeniBakiye - _oncekiBakiye;
                _oncekiBakiye = yeniBakiye;

                if (delta > 0)
                {
                    StartCoroutine(KazancAnimasyon(delta));
                    Debug.Log($"[TutorialKazancAnim] Kazanç delta=+{delta} TL — basket animasyonu");
                }
                else if (delta < 0)
                {
                    StartCoroutine(KayipAnimasyon(delta));
                    Debug.Log($"[TutorialKazancAnim] Kayıp delta={delta} TL — pota dışı animasyonu");
                }
            }

            if (!_oncekiSpinCalisiyor && simdi)
            {
                _oncekiSpinCalisiyor = true;
                _oncekiBakiye = _oy.BotIcinBakiye;
            }
        }

        // === KAZANÇ — basket animasyon (5 aşama) ===

        private IEnumerator KazancAnimasyon(int delta)
        {
            // HOTFIX: Başlangıç KAZANÇ kutusundan (ekran ortası DEĞIL) → parabolic flight bakiyeye
            Vector2 baslangicPos = KazancKutuLocalPos();
            var go = TmpYarat($"+{delta:N0} TL", KAZANC_RENK, baslangicPos);
            var rt = go.GetComponent<RectTransform>();
            var txt = go.GetComponent<TextMeshProUGUI>();

            // Aşama 1: scale 0.5→1.0 + opacity 0→1 (0.3sn)
            yield return ScaleAndFadeIn(rt, txt, 0.5f, 1.0f, 0.3f);

            // Aşama 2: Hold (0.5sn)
            yield return new WaitForSecondsRealtime(0.5f);

            // Aşama 3: Parabolic flight (Bezier curve, 0.6sn)
            Vector2 hedefPos = BakiyeLocalPos();
            Vector2 kontrolPos = (baslangicPos + hedefPos) * 0.5f + new Vector2(0f, 200f);
            yield return BezierFlight(rt, baslangicPos, kontrolPos, hedefPos, 0.6f, 1.0f, 0.5f);

            // Aşama 4: Bakiye'de scale pulse (1.0→1.3→1.0, 0.2sn)
            yield return ScalePulse(rt, 0.5f, 0.8f, 0.5f, 0.1f);

            // Aşama 5: Fade out (0.1sn)
            yield return FadeOut(txt, 0.1f);

            Destroy(go);
        }

        // === KAYIP — pota dışı animasyon (3 aşama) ===

        private IEnumerator KayipAnimasyon(int delta)
        {
            Vector2 baslangicPos = BakiyeLocalPos() + new Vector2(0f, 80f);
            var go = TmpYarat($"{delta:N0} TL", KAYIP_RENK, baslangicPos);
            var rt = go.GetComponent<RectTransform>();
            var txt = go.GetComponent<TextMeshProUGUI>();

            // Aşama 1: scale + opacity (0.2sn)
            yield return ScaleAndFadeIn(rt, txt, 0.5f, 1.0f, 0.2f);

            // Aşama 2: Hold (0.3sn)
            yield return new WaitForSecondsRealtime(0.3f);

            // Aşama 3: Yukarı + yana flight + fade out (0.5sn) — pota dışına
            Vector2 hedefPos = baslangicPos + new Vector2(200f, 300f);
            yield return FlightFade(rt, txt, baslangicPos, hedefPos, 0.5f);

            Destroy(go);
        }

        // === Yardımcılar ===

        private Vector2 SpinTahtaLocalPos()
        {
            if (_oy == null || _oy.slotGridRoot == null) return new Vector2(0f, 100f);
            return WorldToCanvasLocal(_oy.slotGridRoot.position) + new Vector2(0f, 50f); // tahta üstü
        }

        // HOTFIX: Kazanç animasyonu artık KAZANÇ kutusundan çıkar (ekran ortası yerine üst KAZANÇ text'i)
        private Vector2 KazancKutuLocalPos()
        {
            if (_oy == null || _oy.kazancText == null) return SpinTahtaLocalPos(); // fallback
            return WorldToCanvasLocal(_oy.kazancText.transform.position);
        }

        private Vector2 BakiyeLocalPos()
        {
            if (_oy == null || _oy.bakiyeText == null) return new Vector2(-700f, -450f); // fallback
            return WorldToCanvasLocal(_oy.bakiyeText.transform.position);
        }

        private Vector2 WorldToCanvasLocal(Vector3 worldPos)
        {
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRt, screenPos, null, out Vector2 localPos);
            return localPos;
        }

        private GameObject TmpYarat(string metin, Color renk, Vector2 pos)
        {
            var go = new GameObject("KazancText", typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(_canvasGo.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(400f, 100f);
            rt.anchoredPosition = pos;
            rt.localScale = new Vector3(0.5f, 0.5f, 1f);

            var txt = go.AddComponent<TextMeshProUGUI>();
            txt.text = metin;
            txt.fontSize = 64f;
            txt.fontStyle = FontStyles.Bold;
            txt.color = new Color(renk.r, renk.g, renk.b, 0f);
            txt.alignment = TextAlignmentOptions.Center;
            txt.outlineWidth = 0.25f;
            txt.outlineColor = new Color(0f, 0f, 0f, 0.85f);
            txt.raycastTarget = false;
            return go;
        }

        private IEnumerator ScaleAndFadeIn(RectTransform rt, TextMeshProUGUI txt, float scaleStart, float scaleEnd, float sure)
        {
            Color baslangicRenk = txt.color;
            float t = 0f;
            while (t < sure)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / sure);
                float scale = Mathf.Lerp(scaleStart, scaleEnd, u);
                rt.localScale = new Vector3(scale, scale, 1f);
                Color c = baslangicRenk; c.a = u; txt.color = c;
                yield return null;
            }
            rt.localScale = new Vector3(scaleEnd, scaleEnd, 1f);
            Color son = baslangicRenk; son.a = 1f; txt.color = son;
        }

        private IEnumerator BezierFlight(RectTransform rt, Vector2 p0, Vector2 p1, Vector2 p2,
                                         float sure, float scaleStart, float scaleEnd)
        {
            float t = 0f;
            while (t < sure)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / sure);
                Vector2 pos = (1f - u) * (1f - u) * p0 + 2f * (1f - u) * u * p1 + u * u * p2;
                rt.anchoredPosition = pos;
                float scale = Mathf.Lerp(scaleStart, scaleEnd, u);
                rt.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            rt.anchoredPosition = p2;
            rt.localScale = new Vector3(scaleEnd, scaleEnd, 1f);
        }

        private IEnumerator ScalePulse(RectTransform rt, float baz, float pic, float bazSon, float yariDonem)
        {
            float t = 0f;
            while (t < yariDonem)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / yariDonem);
                float s = Mathf.Lerp(baz, pic, u);
                rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            t = 0f;
            while (t < yariDonem)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / yariDonem);
                float s = Mathf.Lerp(pic, bazSon, u);
                rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
        }

        private IEnumerator FadeOut(TextMeshProUGUI txt, float sure)
        {
            Color c0 = txt.color;
            float t = 0f;
            while (t < sure)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / sure);
                Color c = c0; c.a = 1f - u; txt.color = c;
                yield return null;
            }
        }

        private IEnumerator FlightFade(RectTransform rt, TextMeshProUGUI txt, Vector2 p0, Vector2 p1, float sure)
        {
            Color c0 = txt.color;
            float t = 0f;
            while (t < sure)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / sure);
                rt.anchoredPosition = Vector2.Lerp(p0, p1, u);
                Color c = c0; c.a = 1f - u; txt.color = c;
                yield return null;
            }
        }
    }
}
