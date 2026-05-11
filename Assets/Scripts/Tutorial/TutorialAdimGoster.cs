using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace KumarFarkindalik.Tutorial
{
    /// <summary>
    /// Görev Takip Paneli — BAKIYE YÜKLE'nin tam yerinde sağ-üst bölgede 280×320 panel.
    /// İçerik (yukarıdan aşağıya):
    ///   1. Başlık "ADIM #/11"
    ///   2. Alt başlık ("HOOK SENARYOSU" vb.)
    ///   3. Ayraç çizgi
    ///   4. "NE YAPMALISIN:" + 3 satır yapılacaklar (aktif adımda)
    ///   5. Ayraç çizgi
    ///   6. "İLERLEME:" + parametre durumu + spin sayacı (aktif adımda)
    ///   7. Ayraç çizgi
    ///   8. İLERİ butonu
    /// Pasif adımlarda (T2, T_SON) yapılacaklar ve ilerleme blokları gizli, sadece başlık+İLERİ.
    /// </summary>
    [Preserve]
    public class TutorialAdimGoster : MonoBehaviour
    {
        public const int TUTORIAL_SAHNE_BUILD_INDEX = 3;
        public const int CANVAS_SORTING_ORDER = 1650;

        public static TutorialAdimGoster Ornek { get; private set; }

        // Renk paleti — ScriptedModalKopru ile uyumlu
        private static readonly Color BALON_RENK = new Color(0.10f, 0.16f, 0.23f, 0.95f);
        private static readonly Color ALTIN_RENK = new Color(0.83f, 0.69f, 0.22f, 1f);
        private static readonly Color BUTON_ARKA = new Color(0.12f, 0.12f, 0.12f, 0.75f);
        private static readonly Color BUTON_BORDER_CTA = new Color(0.98f, 0.78f, 0.46f, 1f);
        private static readonly Color YESIL = new Color(0.45f, 0.85f, 0.45f, 1f);
        private static readonly Color GRI = new Color(0.78f, 0.78f, 0.80f, 1f);
        private static readonly Color BEYAZ = new Color(0.95f, 0.97f, 1f, 1f);

        private const int TOPLAM_ADIM = 11;

        public event Action OnIleriTiklandi;

        // UI referansları
        private GameObject _root;
        private TextMeshProUGUI _sayacText;
        private TextMeshProUGUI _altBaslikText;
        private GameObject _yapilacaklarBlok;
        private TextMeshProUGUI[] _yapilacakSatirlari = new TextMeshProUGUI[3];
        private GameObject _ilerlemeBlok;
        private TextMeshProUGUI _parametreText;
        private TextMeshProUGUI _spinText;
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
            Debug.Log("[TutorialAdimGoster] Spawn: Ornek atandı, UIYarat tamamlandı.");
        }

        private void OnDestroy()
        {
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

        // === UI yaratımı — 280×320 panel ===

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

            // Ana panel — BAKIYE YÜKLE'nin yerinde (sahne YAML'dan: anchor 0.5/0.5, pos 801.18, 222)
            var panel = new GameObject("Panel",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(_root.transform, false);
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(280f, 320f);
            panelRt.anchoredPosition = new Vector2(801.18f, 222f);
            panel.GetComponent<Image>().color = BALON_RENK;
            BorderEkle(panel.transform, panelRt.sizeDelta, 2f, ALTIN_RENK);

            // 1. Başlık "ADIM #/11 · Senaryo X/5" (top -10) — fontSize 22 → 18 (alt sayaç eklendiğinde sığsın)
            _sayacText = MetinYarat(panel.transform, "Baslik", new Vector2(0f, -10f),
                new Vector2(260f, 30f), 18f, FontStyles.Bold, ALTIN_RENK,
                TextAlignmentOptions.Center, "ADIM ?/11");

            // 2. Alt başlık (top -45)
            _altBaslikText = MetinYarat(panel.transform, "AltBaslik", new Vector2(0f, -45f),
                new Vector2(260f, 25f), 16f, FontStyles.Bold, ALTIN_RENK,
                TextAlignmentOptions.Center, "");

            // 3. Ayraç 1 (top -73)
            CizgiEkle(panel.transform, new Vector2(0f, -73f), new Vector2(240f, 1f));

            // === YAPILACAKLAR BLOK (aktif adımlarda görünür) ===
            _yapilacaklarBlok = new GameObject("YapilacaklarBlok", typeof(RectTransform));
            _yapilacaklarBlok.transform.SetParent(panel.transform, false);
            var ybRt = _yapilacaklarBlok.GetComponent<RectTransform>();
            ybRt.anchorMin = ybRt.anchorMax = new Vector2(0.5f, 1f);
            ybRt.pivot = new Vector2(0.5f, 1f);
            ybRt.sizeDelta = new Vector2(280f, 110f);
            ybRt.anchoredPosition = new Vector2(0f, -78f);

            MetinYarat(_yapilacaklarBlok.transform, "Baslik_NeYapmali", new Vector2(0f, 0f),
                new Vector2(240f, 22f), 13f, FontStyles.Bold, BEYAZ,
                TextAlignmentOptions.Left, "NE YAPMALISIN:");

            for (int i = 0; i < 3; i++)
            {
                _yapilacakSatirlari[i] = MetinYarat(_yapilacaklarBlok.transform, $"Yap{i}",
                    new Vector2(10f, -26f - i * 25f), new Vector2(230f, 22f), 13f,
                    FontStyles.Normal, GRI, TextAlignmentOptions.Left, "");
            }

            // === Ayraç 2 (top -195) ===
            CizgiEkle(panel.transform, new Vector2(0f, -195f), new Vector2(240f, 1f));

            // === İLERLEME BLOK ===
            _ilerlemeBlok = new GameObject("IlerlemeBlok", typeof(RectTransform));
            _ilerlemeBlok.transform.SetParent(panel.transform, false);
            var ibRt = _ilerlemeBlok.GetComponent<RectTransform>();
            ibRt.anchorMin = ibRt.anchorMax = new Vector2(0.5f, 1f);
            ibRt.pivot = new Vector2(0.5f, 1f);
            ibRt.sizeDelta = new Vector2(280f, 75f);
            ibRt.anchoredPosition = new Vector2(0f, -200f);

            MetinYarat(_ilerlemeBlok.transform, "Baslik_Ilerleme", new Vector2(0f, 0f),
                new Vector2(240f, 22f), 13f, FontStyles.Bold, BEYAZ,
                TextAlignmentOptions.Left, "İLERLEME:");

            _parametreText = MetinYarat(_ilerlemeBlok.transform, "Parametre",
                new Vector2(10f, -26f), new Vector2(230f, 22f), 13f, FontStyles.Normal, GRI,
                TextAlignmentOptions.Left, "⌛ Parametre: bekleniyor");

            _spinText = MetinYarat(_ilerlemeBlok.transform, "Spin",
                new Vector2(10f, -50f), new Vector2(230f, 22f), 13f, FontStyles.Normal, GRI,
                TextAlignmentOptions.Left, "—");

            // === Ayraç 3 (top -280) ===
            CizgiEkle(panel.transform, new Vector2(0f, -280f), new Vector2(240f, 1f));

            // === İLERİ butonu (sağ-alt 12 px iç) ===
            var btnGo = new GameObject("IleriButon",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(panel.transform, false);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = btnRt.anchorMax = new Vector2(1f, 0f);
            btnRt.pivot = new Vector2(1f, 0f);
            btnRt.sizeDelta = new Vector2(110f, 32f);
            btnRt.anchoredPosition = new Vector2(-12f, 8f);
            var btnImg = btnGo.GetComponent<Image>();
            btnImg.color = BUTON_ARKA;
            BorderEkle(btnGo.transform, btnRt.sizeDelta, 1.5f, BUTON_BORDER_CTA);
            _ileriButton = btnGo.GetComponent<Button>();
            _ileriButton.transition = Selectable.Transition.None;
            _ileriButton.onClick.AddListener(() => OnIleriTiklandi?.Invoke());

            var btnTxtGo = new GameObject("Txt", typeof(RectTransform), typeof(CanvasRenderer));
            btnTxtGo.transform.SetParent(btnGo.transform, false);
            var btnTxtRt = btnTxtGo.GetComponent<RectTransform>();
            btnTxtRt.anchorMin = Vector2.zero; btnTxtRt.anchorMax = Vector2.one;
            btnTxtRt.offsetMin = btnTxtRt.offsetMax = Vector2.zero;
            var btnTxt = btnTxtGo.AddComponent<TextMeshProUGUI>();
            btnTxt.alignment = TextAlignmentOptions.Center;
            btnTxt.fontSize = 16f;
            btnTxt.fontStyle = FontStyles.Bold;
            btnTxt.color = Color.white;
            btnTxt.text = "İLERİ →";
            btnTxt.raycastTarget = false;
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

        private static void CizgiEkle(Transform parent, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("Ayrac", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var img = go.GetComponent<Image>();
            img.color = new Color(0.83f, 0.69f, 0.22f, 0.40f);
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
    }
}
