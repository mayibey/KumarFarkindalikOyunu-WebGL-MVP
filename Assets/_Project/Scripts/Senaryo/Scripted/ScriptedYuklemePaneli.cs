using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace Senaryo.Scripted
{
    /// <summary>
    /// A5 → A6 geçişinde açılan bloke eden "Borç al" paneli.
    /// Tek butonlu: tıklayınca bakiye +50.000 TL yüklenir, panel kapanır, A6 spinleri başlar.
    ///
    /// SpinButonImpl başında <see cref="IsAcik"/> kontrolü ile spin atma bloke edilir → kullanıcı butona
    /// tıklayana kadar A6 ilk spin'i başlamaz.
    ///
    /// Sahne 2 dışında devre dışı; runtime UI üretilir (prefab gerekmez).
    /// </summary>
    [Preserve]
    public class ScriptedYuklemePaneli : MonoBehaviour
    {
        public const int ANLATICI_SAHNE_BUILD_INDEX = 2;
        public const int BORC_MIKTARI = 50000;

        public static ScriptedYuklemePaneli Ornek { get; private set; }
        /// <summary>Panel açıkken true; SpinButonImpl bunu kontrol edip spin atımını bloke eder.</summary>
        public static bool IsAcik { get; private set; }
        /// <summary>BORÇ AL butonuna fiilen tıklandı mı (A7 final ekranı gerçek yatırım hesabı için).</summary>
        public static bool BorcAlindi { get; private set; }

        /// <summary>Sahne reset (Yeniden Başla) sırasında çağrılır — borç alındı flag'ini sıfırlar.</summary>
        public static void BorcAlindiSifirla() => BorcAlindi = false;

        private GameObject _root;
        private Button _borcAlButton;

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
            Debug.Log($"[YuklemePaneliTANI] OnSceneLoaded — idx={scene.buildIndex}, ad={scene.name}, beklenen={ANLATICI_SAHNE_BUILD_INDEX}");
            if (scene.buildIndex != ANLATICI_SAHNE_BUILD_INDEX)
            {
                Debug.Log($"[YuklemePaneliTANI] Sahne uyumsuz, return.");
                return;
            }
            if (Ornek != null)
            {
                Debug.Log("[YuklemePaneliTANI] Ornek zaten var, return.");
                return;
            }
            Debug.Log("[YuklemePaneliTANI] Sahne eşleşti, GameObject yaratılıyor + AddComponent...");
            var go = new GameObject(nameof(ScriptedYuklemePaneli));
            go.AddComponent<ScriptedYuklemePaneli>();
        }

        private void Awake()
        {
            Debug.Log($"[YuklemePaneliTANI] Awake() ÇAĞRILDI — sahne idx={SceneManager.GetActiveScene().buildIndex}, beklenen={ANLATICI_SAHNE_BUILD_INDEX}");
            if (SceneManager.GetActiveScene().buildIndex != ANLATICI_SAHNE_BUILD_INDEX)
            {
                Debug.Log("[YuklemePaneliTANI] Awake — sahne uyumsuz, SetActive(false).");
                gameObject.SetActive(false);
                return;
            }
            if (Ornek != null && Ornek != this) { Debug.Log("[YuklemePaneliTANI] Awake — Ornek başkası, Destroy."); Destroy(gameObject); return; }
            Ornek = this;
            Debug.Log("[YuklemePaneliTANI] Awake — Ornek atandı, UI yaratılacak.");

            // EventSystem güvencesi (idempotent; ScriptedModalKopru daha önce yaratmış olabilir).
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

        /// <summary>Paneli açar (fire-and-forget). SpinButonImpl IsAcik kontrolüyle bloke eder.</summary>
        public void PaneliGoster()
        {
            Debug.Log($"[YuklemePaneliTANI] PaneliGoster ÇAĞRILDI — _root null mu? {(_root == null)}, IsAcik={IsAcik}");
            if (_root == null)
            {
                Debug.LogError("[YuklemePaneliTANI] _root NULL → UIYarat çalışmamış olabilir, panel açılamadı!");
                return;
            }
            // Anlatici HTML iframe'i gizle (yükleme paneli ÜZERİNDE kalmasın)
            AnlaticiSeritKopru.Ornek?.Gizle();
            _root.SetActive(true);
            IsAcik = true;
            Debug.Log("[YuklemePaneliTANI] Panel SetActive(true) — görünür olmalı.");
        }

        private void OnBorcAlTiklandi()
        {
            try
            {
                var oy = UnityEngine.Object.FindObjectOfType<OyunYoneticisi>();
                if (oy != null)
                {
                    int yeniBakiye = oy.BahisPanelMevcutBakiye() + BORC_MIKTARI;
                    oy.AnlaticiBakiyeyiSifirla(yeniBakiye);
                    BorcAlindi = true;
                    Debug.Log($"[ScriptedYuklemePaneli] Borç alındı: +{BORC_MIKTARI} TL → yeni bakiye {yeniBakiye} TL. BorcAlindi=true.");
                }
                else
                {
                    Debug.LogWarning("[ScriptedYuklemePaneli] OyunYoneticisi bulunamadı; bakiye eklenemedi (defansif: panel yine kapatılıyor, senaryo akışı kırılmasın).");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[ScriptedYuklemePaneli] Bakiye yükleme hatası: " + e.Message);
            }
            finally
            {
                if (_root != null) _root.SetActive(false);
                IsAcik = false;
                // Anlatici iframe'i geri aç (referans counter)
                AnlaticiSeritKopru.Ornek?.Goster();
                // Borç sonrası 2 modal + A6 bahis animasyonu (AnlaticiSeritKopru.BorcSonrasiModalAkisi)
                if (AnlaticiSeritKopru.Ornek != null)
                    AnlaticiSeritKopru.Ornek.StartCoroutine(AnlaticiSeritKopru.Ornek.BorcSonrasiModalAkisi());
            }
        }

        private void UIYarat()
        {
            // Root canvas
            _root = new GameObject("ScriptedYuklemePaneliCanvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _root.transform.SetParent(transform, false);
            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1600; // modal kopru'dan biraz daha üstte
            var scaler = _root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Karartma arka plan
            var bg = new GameObject("Karartma", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bg.transform.SetParent(_root.transform, false);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
            var bgImg = bg.GetComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.85f);
            bgImg.raycastTarget = true;

            // Kutu
            var kutu = new GameObject("YuklemeKutu", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            kutu.transform.SetParent(_root.transform, false);
            var kutuRt = kutu.GetComponent<RectTransform>();
            kutuRt.anchorMin = kutuRt.anchorMax = kutuRt.pivot = new Vector2(0.5f, 0.5f);
            kutuRt.sizeDelta = new Vector2(800f, 460f);
            kutuRt.anchoredPosition = Vector2.zero;
            var kutuImg = kutu.GetComponent<Image>();
            kutuImg.color = new Color(0.10f, 0.05f, 0.07f, 0.98f); // koyu kırmızı

            // Border (kırmızı uyarı vurgusu)
            BorderEkle(kutu.transform, kutuRt.sizeDelta, 3f, new Color(0.85f, 0.15f, 0.15f, 0.95f));

            // Başlık
            var basGo = new GameObject("Baslik", typeof(RectTransform), typeof(CanvasRenderer));
            basGo.transform.SetParent(kutu.transform, false);
            // RectTransform constructor'da zaten ekli; AddComponent<RectTransform> Unity'de null döner.
            var basRt = basGo.GetComponent<RectTransform>();
            basRt.anchorMin = new Vector2(0f, 1f);
            basRt.anchorMax = new Vector2(1f, 1f);
            basRt.pivot = new Vector2(0.5f, 1f);
            basRt.sizeDelta = new Vector2(0f, 80f);
            basRt.anchoredPosition = new Vector2(0f, -30f);
            var basTxt = basGo.AddComponent<TextMeshProUGUI>();
            basTxt.alignment = TextAlignmentOptions.Center;
            basTxt.fontSize = 38f;
            basTxt.fontStyle = FontStyles.Bold;
            basTxt.color = new Color(1f, 0.45f, 0.45f, 1f);
            basTxt.text = "Borç alarak devam etmek istiyor musun?";
            basTxt.enableWordWrapping = true;

            // Açıklama
            var aciklamaGo = new GameObject("Aciklama", typeof(RectTransform), typeof(CanvasRenderer));
            aciklamaGo.transform.SetParent(kutu.transform, false);
            var aciklamaRt = aciklamaGo.GetComponent<RectTransform>();
            aciklamaRt.anchorMin = new Vector2(0f, 0f);
            aciklamaRt.anchorMax = new Vector2(1f, 1f);
            aciklamaRt.offsetMin = new Vector2(40f, 130f);
            aciklamaRt.offsetMax = new Vector2(-40f, -130f);
            var aciklamaTxt = aciklamaGo.AddComponent<TextMeshProUGUI>();
            aciklamaTxt.alignment = TextAlignmentOptions.Center;
            aciklamaTxt.fontSize = 22f;
            aciklamaTxt.fontStyle = FontStyles.Italic;
            aciklamaTxt.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            aciklamaTxt.enableWordWrapping = true;
            aciklamaTxt.richText = true;
            aciklamaTxt.text = "Aileden, kredi kartından veya iş arkadaşından <color=#EF4444>borç alarak</color> oyuna devam etmek istiyor musun? <color=#EF4444><b>Borçla kumar oynamak</b></color> bağımlılığın klasik göstergelerinden biridir.";

            // Borç al butonu
            var btnGo = new GameObject("BorcAlButon",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(kutu.transform, false);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = btnRt.anchorMax = new Vector2(0.5f, 0f);
            btnRt.pivot = new Vector2(0.5f, 0f);
            btnRt.sizeDelta = new Vector2(360f, 80f);
            btnRt.anchoredPosition = new Vector2(0f, 28f);
            var btnImg = btnGo.GetComponent<Image>();
            btnImg.color = new Color(0.85f, 0.18f, 0.18f, 1f); // kırmızı
            _borcAlButton = btnGo.GetComponent<Button>();
            _borcAlButton.onClick.AddListener(OnBorcAlTiklandi);

            var btnTxtGo = new GameObject("BtnTxt", typeof(RectTransform), typeof(CanvasRenderer));
            btnTxtGo.transform.SetParent(btnGo.transform, false);
            var btnTxtRt = btnTxtGo.GetComponent<RectTransform>();
            btnTxtRt.anchorMin = Vector2.zero;
            btnTxtRt.anchorMax = Vector2.one;
            btnTxtRt.offsetMin = btnTxtRt.offsetMax = Vector2.zero;
            var btnTxt = btnTxtGo.AddComponent<TextMeshProUGUI>();
            btnTxt.alignment = TextAlignmentOptions.Center;
            btnTxt.fontSize = 26f;
            btnTxt.fontStyle = FontStyles.Bold;
            btnTxt.color = Color.white;
            btnTxt.text = "BORÇ AL — 50.000 TL";
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
                go.GetComponent<Image>().color = renk;
            }
        }
    }
}
