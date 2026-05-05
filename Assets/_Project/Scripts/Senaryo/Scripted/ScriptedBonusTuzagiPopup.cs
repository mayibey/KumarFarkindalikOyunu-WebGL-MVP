using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace Senaryo.Scripted
{
    /// <summary>
    /// A5 Spin 4 — cazip "casino tuzağı" pop-up'ı. Bonus oyun BAŞLAMADAN önce açılır;
    /// kullanıcı [BONUS AL] butonuna basana kadar bekler. Tıklayınca bakiye 0'a düşürülür
    /// (tüm bakiye yatırıldı), pop-up kapanır, ardından <see cref="ScriptedBonusOyunUygulayici"/>
    /// devreye girer (görsel + cüzi getiri).
    ///
    /// Pedagojik amaç: kullanıcının kasıtlı olarak "tüm parasını yatırma" kararı alması. Pop-up'ın
    /// cazibesi (altın renk, "10.000 KAT KAZAN!") hile/tuzak hissini kullanıcıya kazandırır.
    /// </summary>
    [Preserve]
    public class ScriptedBonusTuzagiPopup : MonoBehaviour
    {
        public const int ANLATICI_SAHNE_BUILD_INDEX = 2;
        public static ScriptedBonusTuzagiPopup Ornek { get; private set; }
        public static bool IsAcik { get; private set; }

        // === Animasyon parametreleri ===
        private const float POP_IN_SURE = 0.50f;   // scale 0 → 1.1 → 1.0
        private const float POP_OUT_SURE = 0.30f;  // scale 1.0 → 0.92 + alpha 1 → 0

        private GameObject _root;
        private RectTransform _kutuRt;
        private CanvasGroup _kutuCanvasGroup;
        private TextMeshProUGUI _butonText;
        private Button _bonusAlButton;
        private bool _butonaTiklandi;

        [Preserve]
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OtomatikInit()
        {
            if (SceneManager.GetActiveScene().buildIndex != ANLATICI_SAHNE_BUILD_INDEX) return;
            if (Ornek != null) return;
            var go = new GameObject(nameof(ScriptedBonusTuzagiPopup));
            go.AddComponent<ScriptedBonusTuzagiPopup>();
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

            if (UnityEngine.Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGO = new GameObject("EventSystem (Auto)");
                esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            UIYarat();
            if (_root != null) _root.SetActive(false);
            IsAcik = false;
        }

        private void OnDestroy()
        {
            if (Ornek == this) Ornek = null;
            IsAcik = false;
        }

        /// <summary>
        /// Pop-up'ı açar; kullanıcı [BONUS AL] basana kadar bekler. Bastıktan sonra <paramref name="oy"/>
        /// üzerinden bakiye 0'a düşürülür (tüm bakiye yatırıldı), pop-up kapanma animasyonu oynar.
        /// </summary>
        public IEnumerator PopupGoster(int bakiye, OyunYoneticisi oy)
        {
            if (_root == null || _butonText == null) yield break;

            _butonText.text = "BONUS AL — TÜM BAKİYE (" + OyunFormatServisi.FormatTL(bakiye) + ")";
            _butonaTiklandi = false;
            IsAcik = true;
            _root.SetActive(true);

            // Pop-in: scale 0 → 1.1 → 1.0 (overshoot), alpha 0 → 1
            yield return PopIn();

            // Buton tıklayana kadar bekle
            while (!_butonaTiklandi) yield return null;

            // Bakiyeyi anında 0'a düşür (görsel etki: kullanıcı parasını yatırdı hisseder).
            // ScriptedBonusOyunUygulayici sonradan yatırım=0, getiri=cüzi ile çağrılacak (sadece görsel + getiri ekleme).
            try
            {
                if (oy != null)
                {
                    int eski = oy.BahisPanelMevcutBakiye();
                    int yeni = Mathf.Max(0, eski - bakiye);
                    oy.AnlaticiBakiyeyiSifirla(yeni);
                    Debug.Log($"[ScriptedBonusTuzagiPopup] Onay → bakiye {eski} → {yeni} (yatırım {bakiye} TL).");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[ScriptedBonusTuzagiPopup] Bakiye senkron hatası: " + e.Message);
            }

            // Pop-out
            yield return PopOut();

            _root.SetActive(false);
            IsAcik = false;
        }

        private void OnBonusAlTiklandi()
        {
            _butonaTiklandi = true;
        }

        private IEnumerator PopIn()
        {
            _kutuRt.localScale = Vector3.zero;
            if (_kutuCanvasGroup != null) _kutuCanvasGroup.alpha = 0f;

            float t = 0f;
            while (t < POP_IN_SURE)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / POP_IN_SURE);
                // Overshoot: ilk yarısı 0→1.1, ikinci yarısı 1.1→1.0
                float scale;
                if (u < 0.6f)
                {
                    float u1 = u / 0.6f;
                    scale = Mathf.Lerp(0f, 1.1f, 1f - Mathf.Pow(1f - u1, 3f));
                }
                else
                {
                    float u2 = (u - 0.6f) / 0.4f;
                    scale = Mathf.Lerp(1.1f, 1.0f, u2);
                }
                _kutuRt.localScale = new Vector3(scale, scale, 1f);
                if (_kutuCanvasGroup != null) _kutuCanvasGroup.alpha = Mathf.Clamp01(u * 2f);
                yield return null;
            }
            _kutuRt.localScale = Vector3.one;
            if (_kutuCanvasGroup != null) _kutuCanvasGroup.alpha = 1f;
        }

        private IEnumerator PopOut()
        {
            float t = 0f;
            Vector3 baslangic = _kutuRt.localScale;
            float baslangicAlpha = _kutuCanvasGroup != null ? _kutuCanvasGroup.alpha : 1f;
            while (t < POP_OUT_SURE)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / POP_OUT_SURE);
                float scale = Mathf.Lerp(1f, 0.92f, u);
                _kutuRt.localScale = baslangic * scale;
                if (_kutuCanvasGroup != null)
                    _kutuCanvasGroup.alpha = Mathf.Lerp(baslangicAlpha, 0f, u);
                yield return null;
            }
        }

        private void UIYarat()
        {
            _root = new GameObject("ScriptedBonusTuzagiPopupCanvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _root.transform.SetParent(transform, false);
            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1750; // Bonus oyun (1700) üstünde, modal/yukleme/final altında
            var scaler = _root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Karartma
            var bg = new GameObject("Karartma", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bg.transform.SetParent(_root.transform, false);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
            bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

            // Kutu — cazip altın/kırmızı casino tarzı
            var kutu = new GameObject("TuzakKutu",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            kutu.transform.SetParent(_root.transform, false);
            _kutuRt = kutu.GetComponent<RectTransform>();
            _kutuRt.anchorMin = _kutuRt.anchorMax = _kutuRt.pivot = new Vector2(0.5f, 0.5f);
            _kutuRt.sizeDelta = new Vector2(720f, 460f);
            _kutuRt.anchoredPosition = Vector2.zero;
            _kutuRt.localScale = Vector3.zero;
            // Casino altın arka plan (gradient yerine solid altın + kırmızı border vurgusu)
            kutu.GetComponent<Image>().color = new Color(0.83f, 0.65f, 0.13f, 1f); // altın #d4a01f
            _kutuCanvasGroup = kutu.GetComponent<CanvasGroup>();

            // Kalın parlak border (casino vurgusu)
            BorderEkle(kutu.transform, _kutuRt.sizeDelta, 4f, new Color(0.85f, 0.18f, 0.18f, 1f));

            // Başlık: "🎰 ŞANSLI ANINDASIN!"
            var basGo = new GameObject("Baslik", typeof(RectTransform), typeof(CanvasRenderer));
            basGo.transform.SetParent(kutu.transform, false);
            var basRt = basGo.GetComponent<RectTransform>();
            basRt.anchorMin = new Vector2(0f, 1f); basRt.anchorMax = new Vector2(1f, 1f);
            basRt.pivot = new Vector2(0.5f, 1f);
            basRt.sizeDelta = new Vector2(0f, 80f);
            basRt.anchoredPosition = new Vector2(0f, -25f);
            var basTxt = basGo.AddComponent<TextMeshProUGUI>();
            basTxt.alignment = TextAlignmentOptions.Center;
            basTxt.fontSize = 38f;
            basTxt.fontStyle = FontStyles.Bold;
            basTxt.color = new Color(0.55f, 0.05f, 0.05f, 1f); // koyu kırmızı (altın üstünde okunabilir)
            basTxt.text = "🎰 ŞANSLI ANINDASIN!";
            basTxt.raycastTarget = false;

            // Açıklama
            var aciGo = new GameObject("Aciklama", typeof(RectTransform), typeof(CanvasRenderer));
            aciGo.transform.SetParent(kutu.transform, false);
            var aciRt = aciGo.GetComponent<RectTransform>();
            aciRt.anchorMin = new Vector2(0f, 0f); aciRt.anchorMax = new Vector2(1f, 1f);
            aciRt.offsetMin = new Vector2(40f, 130f); aciRt.offsetMax = new Vector2(-40f, -110f);
            var aciTxt = aciGo.AddComponent<TextMeshProUGUI>();
            aciTxt.alignment = TextAlignmentOptions.Center;
            aciTxt.fontSize = 22f;
            aciTxt.fontStyle = FontStyles.Bold;
            aciTxt.color = new Color(0.10f, 0.05f, 0.05f, 1f);
            aciTxt.lineSpacing = 8f;
            aciTxt.enableWordWrapping = true;
            aciTxt.text =
                "Tüm bakiyeni bonus oyuna yatır,\n" +
                "<color=#9B0000><size=28>10.000 KATI KAZANMA</size></color>\n" +
                "şansını yakala!\n\n" +
                "<i>Bu fırsat bir daha karşına çıkmayabilir!</i>";
            aciTxt.raycastTarget = false;

            // BONUS AL butonu — parlak yeşil
            var btnGo = new GameObject("BonusAlButon",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(kutu.transform, false);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = btnRt.anchorMax = new Vector2(0.5f, 0f);
            btnRt.pivot = new Vector2(0.5f, 0f);
            btnRt.sizeDelta = new Vector2(420f, 80f);
            btnRt.anchoredPosition = new Vector2(0f, 28f);
            btnGo.GetComponent<Image>().color = new Color(0.15f, 0.68f, 0.38f, 1f); // canlı yeşil #27ae60
            _bonusAlButton = btnGo.GetComponent<Button>();
            // Hover efekti: ColorTint ile parlama (Selectable.Transition)
            var colors = _bonusAlButton.colors;
            colors.normalColor = new Color(0.15f, 0.68f, 0.38f, 1f);
            colors.highlightedColor = new Color(0.20f, 0.78f, 0.45f, 1f);
            colors.pressedColor = new Color(0.10f, 0.55f, 0.30f, 1f);
            colors.selectedColor = colors.highlightedColor;
            _bonusAlButton.colors = colors;
            _bonusAlButton.onClick.AddListener(OnBonusAlTiklandi);

            // Buton text (dinamik — bakiye miktarı PopupGoster içinde set edilir)
            var btnTxtGo = new GameObject("BtnTxt", typeof(RectTransform), typeof(CanvasRenderer));
            btnTxtGo.transform.SetParent(btnGo.transform, false);
            var btnTxtRt = btnTxtGo.GetComponent<RectTransform>();
            btnTxtRt.anchorMin = Vector2.zero; btnTxtRt.anchorMax = Vector2.one;
            btnTxtRt.offsetMin = btnTxtRt.offsetMax = Vector2.zero;
            _butonText = btnTxtGo.AddComponent<TextMeshProUGUI>();
            _butonText.alignment = TextAlignmentOptions.Center;
            _butonText.fontSize = 26f;
            _butonText.fontStyle = FontStyles.Bold;
            _butonText.color = Color.white;
            _butonText.text = "BONUS AL";
            _butonText.raycastTarget = false;
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
