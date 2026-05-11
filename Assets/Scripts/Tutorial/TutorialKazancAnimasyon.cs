using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace KumarFarkindalik.Tutorial
{
    /// <summary>
    /// PAKET 5: Spin sonu bakiye delta görsel feedback'i — floating text MVP.
    ///
    /// SpinCalisiyorMu true→false geçişinde BotIcinBakiye delta hesaplar:
    ///   delta > 0 → "+{tutar} TL" sarı (kazanç + bahis fark)
    ///   delta < 0 → "{tutar} TL" kırmızı (sadece bahis düştü, kazanç yok)
    ///   delta == 0 → atla (push spin)
    ///
    /// MVP: parabolic flight + counting up YOK (sonraki paket). Sadece scale-pulse + opacity.
    /// </summary>
    [Preserve]
    public class TutorialKazancAnimasyon : MonoBehaviour
    {
        public const int BUILD_INDEX = 3;
        public const int CANVAS_SORTING_ORDER = 1700; // Tutorial UI'lardan üstte

        public static TutorialKazancAnimasyon Ornek { get; private set; }

        private static readonly Color KAZANC_RENK = new Color(1f, 0.85f, 0.20f, 1f); // sarı
        private static readonly Color KAYIP_RENK  = new Color(0.95f, 0.30f, 0.25f, 1f); // kırmızı

        private OyunYoneticisi _oy;
        private int _oncekiBakiye = -1;
        private bool _oncekiSpinCalisiyor = false;
        private GameObject _canvasGo;

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

            // Raycast yok (animasyon UI tıklamayı engellemesin)
            _canvasGo.GetComponent<GraphicRaycaster>().enabled = false;
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

            // Spin bitiş geçişi (true → false)
            if (_oncekiSpinCalisiyor && !simdi)
            {
                _oncekiSpinCalisiyor = false;
                int yeniBakiye = _oy.BotIcinBakiye;
                int delta = yeniBakiye - _oncekiBakiye;
                _oncekiBakiye = yeniBakiye;

                if (delta > 0)
                {
                    StartCoroutine(FloatingText($"+{delta:N0} TL", KAZANC_RENK));
                    Debug.Log($"[TutorialKazancAnim] Kazanç delta=+{delta} TL");
                }
                else if (delta < 0)
                {
                    StartCoroutine(FloatingText($"{delta:N0} TL", KAYIP_RENK));
                    Debug.Log($"[TutorialKazancAnim] Kayıp delta={delta} TL");
                }
                // delta == 0 → atla
            }

            if (!_oncekiSpinCalisiyor && simdi)
            {
                _oncekiSpinCalisiyor = true;
                // Spin başında oncekiBakiye'yi güncel tut (bahis henüz düşmedi, başlangıç değeri)
                _oncekiBakiye = _oy.BotIcinBakiye;
            }
        }

        private IEnumerator FloatingText(string metin, Color renk)
        {
            // TMP text oluştur — Canvas merkezinde, biraz üstte
            var go = new GameObject("KazancText", typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(_canvasGo.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(400f, 100f);
            rt.anchoredPosition = new Vector2(0f, 100f); // ekran merkezinden 100px yukarı

            var txt = go.AddComponent<TextMeshProUGUI>();
            txt.text = metin;
            txt.fontSize = 64f;
            txt.fontStyle = FontStyles.Bold;
            txt.color = renk;
            txt.alignment = TextAlignmentOptions.Center;
            txt.outlineWidth = 0.25f;
            txt.outlineColor = new Color(0f, 0f, 0f, 0.85f);
            txt.raycastTarget = false;

            // Faz 1: 0.0-0.2sn — scale 0.5→1.2, opacity 0→1
            const float GIRIS = 0.2f;
            float t = 0f;
            while (t < GIRIS)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / GIRIS);
                float scale = Mathf.Lerp(0.5f, 1.2f, u);
                rt.localScale = new Vector3(scale, scale, 1f);
                var c = txt.color; c.a = u; txt.color = c;
                yield return null;
            }

            // Faz 2: 0.2-0.4sn — scale 1.2→1.0 (settle)
            const float SETTLE = 0.2f;
            t = 0f;
            while (t < SETTLE)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / SETTLE);
                float scale = Mathf.Lerp(1.2f, 1.0f, u);
                rt.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            // Faz 3: 0.4-1.0sn — hold (kullanıcı okur)
            yield return new WaitForSecondsRealtime(0.6f);

            // Faz 4: 1.0-1.5sn — opacity 1→0 + yukarı kayma (40px)
            const float CIKIS = 0.5f;
            t = 0f;
            Vector2 baslangicPos = rt.anchoredPosition;
            while (t < CIKIS)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / CIKIS);
                var c = txt.color; c.a = 1f - u; txt.color = c;
                rt.anchoredPosition = baslangicPos + new Vector2(0f, u * 40f);
                yield return null;
            }

            Destroy(go);
        }
    }
}
