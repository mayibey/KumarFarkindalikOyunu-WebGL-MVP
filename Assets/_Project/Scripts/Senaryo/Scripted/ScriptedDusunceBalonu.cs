using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace Senaryo.Scripted
{
    /// <summary>
    /// A5 sonu eğitmen modalı sonrası açılan klasik çizgi roman tarzı çoklu düşünce balonu sahnesi.
    /// Karakter ekran ortasında siluet halinde belirir; etrafına 4 ayrı bulut balonu sırayla pop-in olur,
    /// her birinde bir yalan typewriter ile yazılır. Tüm balonlar ekranda kalıcıdır (öncekiler silinmez).
    /// 3 sn bekleme veya sağ altta ATLA butonu sonrası tüm balonlar paralel pop-out olur, karakter çıkar,
    /// dim arka plan kapanır. Sahne 2 dışında devre dışı; runtime UI üretilir (prefab gerekmez).
    /// </summary>
    [Preserve]
    public class ScriptedDusunceBalonu : MonoBehaviour
    {
        public const int ANLATICI_SAHNE_BUILD_INDEX = 2;
        public static ScriptedDusunceBalonu Ornek { get; private set; }

        /// <summary>Balon görünür mü? BaloniGoster coroutine süresince true; SpinButonImpl ve
        /// OyunUIGuncellemeServisi bu flag'e bakıp Spin butonunu engeller.</summary>
        public static bool BalonAcik { get; private set; }

        // === Animasyon parametreleri ===
        private const float DIM_FADE_SURE = 0.4f;
        private const float KARAKTER_POPIN_SURE = 0.5f;
        private const float KARAKTER_BASLANGIC_BEKLEME = 0.3f;
        private const float BALON_POPIN_SURE = 0.4f;
        private const float TYPEWRITER_HARF_BASINA = 0.030f;
        private const float BALONLAR_ARASI_BEKLEME = 1.5f;
        private const float SON_BEKLEME = 3.0f;
        private const float BALON_POPOUT_SURE = 0.4f;
        private const float DUSUNCE_DAIRE_BEKLEME = 0.10f;

        // === UI referansları ===
        private GameObject _root;
        private CanvasGroup _dimCanvasGroup;
        private RectTransform _karakterRt;
        private CanvasGroup _karakterCanvasGroup;
        private Sprite _karakterSprite;
        private Sprite _balonSprite;
        private Sprite _daireSprite;

        // === State ===
        private bool _aktifMi;
        private bool _atlandi;

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
            var go = new GameObject(nameof(ScriptedDusunceBalonu));
            go.AddComponent<ScriptedDusunceBalonu>();
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

            // EventSystem güvencesi
            if (UnityEngine.Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGO = new GameObject("EventSystem (Auto)");
                esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            _karakterSprite = KarakterSpritiniAl();
            _balonSprite = BalonSpriteOlustur();
            _daireSprite = DaireSpriteOlustur();
            UIYarat();
            if (_root != null) _root.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Ornek == this) Ornek = null;
            _aktifMi = false;
        }

        // ──────────────────────────────────────────────────────────────────────
        // PUBLIC API
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Düşünce balonu sahnesini oynatır (4 yalan, her biri kalıcı). Çağıran yield return ile bekler.
        /// </summary>
        public IEnumerator BaloniGoster()
        {
            if (_root == null) yield break;
            _aktifMi = true;
            BalonAcik = true; // Spin butonu bu süre boyunca engellenir
            _atlandi = false;
            _root.SetActive(true);

            // 1) Anlatici HTML iframe'i gizle — sol panel Unity Canvas'in üstünde kalmasın.
            //    (z-index 100 olsa bile WebGL Unity canvas DOM stacking'inde kayboluyor.)
            AnlaticiSeritKopru.Ornek?.Gizle();

            // 2) Sol-altta paralel asistan modal başlat (fire-and-forget). Balon animasyonuyla
            //    eş zamanlı gözükür; kullanıcı modaldaki TAMAM'a bağımsız basabilir.
            var asistanModal = UnityEngine.Object.FindObjectOfType<ScriptedModalKopru>();
            if (asistanModal != null)
            {
                StartCoroutine(asistanModal.ModalGoster(
                    "Bu aşamada oyuncu çevresindeki kişilere <color=#EF4444>yalan söyleyerek</color> " +
                    "veya bankalardan <color=#EF4444>kredi çekerek</color> para bulmaya çalışır.\n\n" +
                    "Burada amaç eski <color=#EF4444>kayıpların telafisidir</color>. Ancak bu, " +
                    "<color=#EF4444><b>kumar bağımlılığının en yıkıcı evresidir</b></color>: borç katlanarak " +
                    "büyür, ilişkiler bozulur, hayatlar mahvolur."
                ));
            }

            // 4 yalan tanımı: konum (karakter merkezinden offset) + metin. Geniş yerleşim, balonlar
            // karakteri çevreler ama üst üste gelmez (balon boyutu 480x170).
            var yalanlar = new (Vector2 konum, string yazi)[]
            {
                (new Vector2(-420f, 280f),  "<color=#EF4444><b>Çocuğum hasta</b></color>, acil para lazım..."),
                (new Vector2( 420f, 280f),  "<color=#EF4444><b>Bir 50 bin kredi çekersem</b></color> hepsini telafi ederim..."),
                (new Vector2(-490f,  30f),  "<color=#EF4444><b>Kız kardeşim borca girdi</b></color>, yardım etmem gerek..."),
                (new Vector2( 490f,  30f),  "<color=#EF4444><b>Bu sefer kazanırsam</b></color> hepsini öderim, söz veriyorum..."),
            };

            var aktifBalonlar = new List<RectTransform>();
            try
            {
                yield return DimFadeIn();
                yield return KarakterPopIn();
                yield return BekleVeyaAtla(KARAKTER_BASLANGIC_BEKLEME);

                for (int i = 0; i < yalanlar.Length; i++)
                {
                    if (_atlandi) break;
                    var (konum, yazi) = yalanlar[i];
                    var balonRt = BalonOlustur(konum, out CanvasGroup balonCg, out TextMeshProUGUI metinTmp, out List<RectTransform> daireRtler);
                    aktifBalonlar.Add(balonRt);
                    yield return DusunceDaireleriniGoster(daireRtler);
                    yield return BalonPopIn(balonRt, balonCg);
                    yield return TypewriterEt(metinTmp, yazi);
                    yield return BekleVeyaAtla(BALONLAR_ARASI_BEKLEME);
                }

                // 4 balon ekranda kaldı → kullanıcı sindirsin
                yield return BekleVeyaAtla(SON_BEKLEME);

                // Paralel pop-out
                foreach (var b in aktifBalonlar)
                    StartCoroutine(BalonPopOut(b));
                yield return new WaitForSecondsRealtime(BALON_POPOUT_SURE);

                yield return KarakterSlideOut();
                yield return DimFadeOut();
            }
            finally
            {
                _aktifMi = false;
                BalonAcik = false; // Spin butonu tekrar serbest
                _atlandi = false;
                if (_root != null) _root.SetActive(false);
                // Aktif balonları temizle (sonraki açılışta yeniden oluşacak)
                foreach (var b in aktifBalonlar)
                    if (b != null) Destroy(b.gameObject);
                // Anlatici HTML iframe'i geri aç
                AnlaticiSeritKopru.Ornek?.Goster();
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // ANİMASYONLAR
        // ──────────────────────────────────────────────────────────────────────

        private IEnumerator DimFadeIn()
        {
            float t = 0f;
            while (t < DIM_FADE_SURE && !_atlandi)
            {
                t += Time.unscaledDeltaTime;
                _dimCanvasGroup.alpha = Mathf.Clamp01(t / DIM_FADE_SURE);
                yield return null;
            }
            _dimCanvasGroup.alpha = 1f;
        }

        private IEnumerator DimFadeOut()
        {
            float t = 0f;
            float baslangic = _dimCanvasGroup.alpha;
            while (t < DIM_FADE_SURE)
            {
                t += Time.unscaledDeltaTime;
                _dimCanvasGroup.alpha = Mathf.Lerp(baslangic, 0f, t / DIM_FADE_SURE);
                yield return null;
            }
            _dimCanvasGroup.alpha = 0f;
        }

        private IEnumerator KarakterPopIn()
        {
            _karakterRt.localScale = Vector3.zero;
            _karakterCanvasGroup.alpha = 0f;
            float t = 0f;
            while (t < KARAKTER_POPIN_SURE && !_atlandi)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / KARAKTER_POPIN_SURE);
                float scale = u < 0.6f
                    ? Mathf.Lerp(0f, 1.1f, 1f - Mathf.Pow(1f - (u / 0.6f), 3f))
                    : Mathf.Lerp(1.1f, 1.0f, (u - 0.6f) / 0.4f);
                _karakterRt.localScale = new Vector3(scale, scale, 1f);
                _karakterCanvasGroup.alpha = Mathf.Clamp01(u * 2f);
                yield return null;
            }
            _karakterRt.localScale = Vector3.one;
            _karakterCanvasGroup.alpha = 1f;
        }

        private IEnumerator KarakterSlideOut()
        {
            float t = 0f;
            Vector3 baslangicScale = _karakterRt.localScale;
            float baslangicAlpha = _karakterCanvasGroup.alpha;
            while (t < BALON_POPOUT_SURE)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / BALON_POPOUT_SURE);
                _karakterRt.localScale = Vector3.Lerp(baslangicScale, baslangicScale * 0.92f, u);
                _karakterCanvasGroup.alpha = Mathf.Lerp(baslangicAlpha, 0f, u);
                yield return null;
            }
        }

        private IEnumerator BalonPopIn(RectTransform rt, CanvasGroup cg)
        {
            rt.localScale = Vector3.zero;
            cg.alpha = 0f;
            float t = 0f;
            while (t < BALON_POPIN_SURE && !_atlandi)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / BALON_POPIN_SURE);
                float scale = u < 0.6f
                    ? Mathf.Lerp(0f, 1.1f, 1f - Mathf.Pow(1f - (u / 0.6f), 3f))
                    : Mathf.Lerp(1.1f, 1.0f, (u - 0.6f) / 0.4f);
                rt.localScale = new Vector3(scale, scale, 1f);
                cg.alpha = Mathf.Clamp01(u * 2f);
                yield return null;
            }
            rt.localScale = Vector3.one;
            cg.alpha = 1f;
        }

        private IEnumerator BalonPopOut(RectTransform rt)
        {
            if (rt == null) yield break;
            var cg = rt.GetComponent<CanvasGroup>();
            float t = 0f;
            Vector3 baslangic = rt.localScale;
            float baslangicAlpha = cg != null ? cg.alpha : 1f;
            while (t < BALON_POPOUT_SURE)
            {
                if (rt == null) yield break;
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / BALON_POPOUT_SURE);
                rt.localScale = Vector3.Lerp(baslangic, baslangic * 0.85f, u);
                if (cg != null) cg.alpha = Mathf.Lerp(baslangicAlpha, 0f, u);
                yield return null;
            }
        }

        private IEnumerator DusunceDaireleriniGoster(List<RectTransform> daireRtler)
        {
            // Her daireyi sırayla görünür yap (alpha 0 → 1)
            foreach (var daireRt in daireRtler)
            {
                if (daireRt == null) continue;
                var img = daireRt.GetComponent<Image>();
                if (img != null)
                {
                    var c = img.color; c.a = 1f; img.color = c;
                }
                if (_atlandi) yield break;
                yield return new WaitForSecondsRealtime(DUSUNCE_DAIRE_BEKLEME);
            }
        }

        private IEnumerator TypewriterEt(TextMeshProUGUI tmp, string mesaj)
        {
            if (tmp == null) yield break;

            // Rich-text safe: tüm mesajı bir kerede ata, görünür karakter sayısını 0 → toplamHarf'e
            // doğru artır. TMP <color>, <b>, <i> tag'lerini maxVisibleCharacters saymaz → tag'ler
            // raw HTML olarak görünmez (yalanlar renklendirilirken kritik).
            tmp.text = mesaj;
            tmp.maxVisibleCharacters = 0;
            tmp.ForceMeshUpdate();
            int toplamHarf = tmp.textInfo.characterCount;

            for (int i = 0; i <= toplamHarf; i++)
            {
                if (_atlandi)
                {
                    tmp.maxVisibleCharacters = toplamHarf;
                    yield break;
                }
                tmp.maxVisibleCharacters = i;
                yield return new WaitForSecondsRealtime(TYPEWRITER_HARF_BASINA);
            }
        }

        private IEnumerator BekleVeyaAtla(float sure)
        {
            float t = 0f;
            while (t < sure && !_atlandi)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void OnAtlaTiklandi() => _atlandi = true;

        // ──────────────────────────────────────────────────────────────────────
        // UI YARATMA — Procedural (prefab gerekmez)
        // ──────────────────────────────────────────────────────────────────────

        private void UIYarat()
        {
            _root = new GameObject("ScriptedDusunceBalonuCanvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _root.transform.SetParent(transform, false);
            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // sortingOrder 1450: Modal (1500) ALTINDA. Düşünce balonu sırasında paralel asistan
            // modal sol-altta net görünür (dim onu kapatmaz). Karakter+balonlar ekran ortasında,
            // modal sol-altta — çakışma minimum.
            canvas.sortingOrder = 1450;

            var scaler = _root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Dim arka plan (full-screen, alpha animatik)
            var dim = new GameObject("Dim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            dim.transform.SetParent(_root.transform, false);
            var dimRt = dim.GetComponent<RectTransform>();
            dimRt.anchorMin = Vector2.zero; dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = dimRt.offsetMax = Vector2.zero;
            dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
            _dimCanvasGroup = dim.GetComponent<CanvasGroup>();
            _dimCanvasGroup.alpha = 0f;

            // Orta-üstte aşama başlığı — geçiş evresinin pedagojik etiketi.
            // "BAŞKA YERDEN PARA BULMA ARAYIŞI" tam orta üste konumlanır, simetrik görünür.
            var asamaGo = new GameObject("AsamaBasligi", typeof(RectTransform), typeof(CanvasRenderer));
            asamaGo.transform.SetParent(_root.transform, false);
            var asamaRt = asamaGo.GetComponent<RectTransform>();
            asamaRt.anchorMin = new Vector2(0.5f, 1f);
            asamaRt.anchorMax = new Vector2(0.5f, 1f);
            asamaRt.pivot = new Vector2(0.5f, 1f);
            asamaRt.sizeDelta = new Vector2(800f, 100f);
            asamaRt.anchoredPosition = new Vector2(0f, -40f);
            var asamaTxt = asamaGo.AddComponent<TextMeshProUGUI>();
            asamaTxt.alignment = TextAlignmentOptions.Center;
            asamaTxt.fontSize = 30f;
            asamaTxt.fontStyle = FontStyles.Bold;
            asamaTxt.color = new Color(0.98f, 0.78f, 0.46f, 1f); // sarı
            asamaTxt.enableWordWrapping = true;
            asamaTxt.lineSpacing = 6f;
            asamaTxt.text = "BAŞKA YERDEN\nPARA BULMA ARAYIŞI";
            asamaTxt.raycastTarget = false;

            // Karakter (ekran ortası, büyük dramatik) — Resources/yuzkafa.png veya procedural fallback
            var karakter = new GameObject("Karakter", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            karakter.transform.SetParent(_root.transform, false);
            _karakterRt = karakter.GetComponent<RectTransform>();
            _karakterRt.anchorMin = _karakterRt.anchorMax = _karakterRt.pivot = new Vector2(0.5f, 0.5f);
            _karakterRt.sizeDelta = new Vector2(450f, 550f);
            _karakterRt.anchoredPosition = new Vector2(0f, -40f);
            var kImg = karakter.GetComponent<Image>();
            kImg.sprite = _karakterSprite;
            kImg.preserveAspect = true;
            kImg.raycastTarget = false;
            _karakterCanvasGroup = karakter.GetComponent<CanvasGroup>();

            // ATLA butonu (sağ alt köşe)
            var atla = new GameObject("AtlaButon",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            atla.transform.SetParent(_root.transform, false);
            var aRt = atla.GetComponent<RectTransform>();
            aRt.anchorMin = aRt.anchorMax = new Vector2(1f, 0f);
            aRt.pivot = new Vector2(1f, 0f);
            aRt.sizeDelta = new Vector2(150f, 52f);
            aRt.anchoredPosition = new Vector2(-30f, 30f);
            atla.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f, 0.7f);
            atla.GetComponent<Button>().onClick.AddListener(OnAtlaTiklandi);

            var aTxtGo = new GameObject("AtlaTxt", typeof(RectTransform), typeof(CanvasRenderer));
            aTxtGo.transform.SetParent(atla.transform, false);
            var aTxtRt = aTxtGo.GetComponent<RectTransform>();
            aTxtRt.anchorMin = Vector2.zero; aTxtRt.anchorMax = Vector2.one;
            aTxtRt.offsetMin = aTxtRt.offsetMax = Vector2.zero;
            var aTxt = aTxtGo.AddComponent<TextMeshProUGUI>();
            aTxt.alignment = TextAlignmentOptions.Center;
            aTxt.fontSize = 20f;
            aTxt.fontStyle = FontStyles.Bold;
            aTxt.color = Color.white;
            aTxt.text = "ATLA ▶";
            aTxt.raycastTarget = false;
        }

        /// <summary>
        /// Tek bir bulut balonu (RectTransform + Image + CanvasGroup) + içinde TMP + 3 küçük düşünce dairesi
        /// karakter kafasına doğru. Konum karakter merkezinden offset (anchor middle-center).
        /// </summary>
        private RectTransform BalonOlustur(Vector2 konum, out CanvasGroup cg, out TextMeshProUGUI tmp, out List<RectTransform> daireRtler)
        {
            var balon = new GameObject("Balon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            balon.transform.SetParent(_root.transform, false);
            var rt = balon.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(480f, 170f);
            rt.anchoredPosition = konum;
            var img = balon.GetComponent<Image>();
            img.sprite = _balonSprite;
            img.type = Image.Type.Simple; // procedural texture, slice yok
            img.color = Color.white;
            img.preserveAspect = false;
            img.raycastTarget = false;
            cg = balon.GetComponent<CanvasGroup>();

            // Yazı — büyük, italic, padding 30px
            var txtGo = new GameObject("Yazi", typeof(RectTransform), typeof(CanvasRenderer));
            txtGo.transform.SetParent(balon.transform, false);
            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(30f, 24f);
            txtRt.offsetMax = new Vector2(-30f, -24f);
            tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 24f;
            tmp.fontStyle = FontStyles.Italic;
            tmp.color = new Color(0.10f, 0.10f, 0.10f, 1f);
            tmp.lineSpacing = 8f;
            tmp.enableWordWrapping = true;
            tmp.text = "";
            tmp.raycastTarget = false;

            // Düşünce yolu daireleri: balondan karakter kafasına doğru (8px → 5px → 3px)
            daireRtler = new List<RectTransform>();
            // Karakter kafası ekran koordinatlarında ~(0, +90) civarı (karakterRt y=-40 + sprite y_top ~125 → ~+85)
            Vector2 karakterKafa = new Vector2(0f, 85f);
            Vector2 balonMerkez = konum;
            Vector2 yon = (karakterKafa - balonMerkez).normalized;
            float toplamMesafe = Vector2.Distance(karakterKafa, balonMerkez);
            float[] mesafeOranlari = { 0.55f, 0.72f, 0.88f }; // balondan kafaya doğru
            float[] caplar = { 24f, 18f, 12f };
            for (int i = 0; i < 3; i++)
            {
                var daire = new GameObject($"DusunceDaire_{i}",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                daire.transform.SetParent(_root.transform, false);
                var dRt = daire.GetComponent<RectTransform>();
                dRt.anchorMin = dRt.anchorMax = dRt.pivot = new Vector2(0.5f, 0.5f);
                dRt.sizeDelta = new Vector2(caplar[i], caplar[i]);
                Vector2 dPos = balonMerkez + yon * (toplamMesafe * mesafeOranlari[i]);
                dRt.anchoredPosition = dPos;
                var dImg = daire.GetComponent<Image>();
                dImg.sprite = _daireSprite;
                dImg.color = new Color(1f, 1f, 1f, 0f); // başlangıç görünmez (DusunceDaireleriniGoster fade-in)
                dImg.raycastTarget = false;
                daireRtler.Add(dRt);
            }
            return rt;
        }

        // ──────────────────────────────────────────────────────────────────────
        // PROCEDURAL SPRITE'LAR
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Karakter sprite kaynağını döndürür. Önce Resources/yuzkafa.png (kullanıcının PNG'si),
        /// bulunmazsa procedural siluet (fallback).
        /// </summary>
        private static Sprite KarakterSpritiniAl()
        {
            var sprite = Resources.Load<Sprite>("yuzkafa");
            if (sprite != null)
            {
                Debug.Log("[ScriptedDusunceBalonu] Resources/yuzkafa.png yüklendi.");
                return sprite;
            }
            Debug.LogWarning("[ScriptedDusunceBalonu] Resources/yuzkafa.png bulunamadı (Sprite import edilmemiş olabilir) — procedural siluet kullanılıyor.");
            return SiluetSpriteOlustur();
        }

        /// <summary>Procedural siluet fallback — kullanıcı PNG'si yoksa kullanılır.</summary>
        private static Sprite SiluetSpriteOlustur()
        {
            int w = 450, h = 550;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color koyu = new Color32(10, 10, 15, 255);
            Color mor = new Color32(60, 52, 137, 255);
            Color saydam = new Color32(0, 0, 0, 0);

            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = saydam;
            tex.SetPixels(pixels);

            // Kafa: dolu daire cx=225, cy=460, r=60
            DaireDoldur(tex, 225, 460, 60, koyu);
            // Boyun
            DikdortgenDoldur(tex, 210, 380, 240, 410, koyu);
            // Gövde
            DikdortgenDoldur(tex, 110, 180, 340, 380, koyu);
            // Slot makinesi (göğüs hizasında)
            DikdortgenDoldur(tex, 145, 220, 305, 310, mor);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>4-5 ellipse birleşmesi klasik bulut balonu, beyaz dolgu + 3px siyah border.</summary>
        private static Sprite BalonSpriteOlustur()
        {
            int w = 480, h = 170;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color beyaz = new Color32(255, 255, 255, 255);
            Color border = new Color32(44, 44, 42, 255);
            Color saydam = new Color32(0, 0, 0, 0);

            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = saydam;
            tex.SetPixels(pixels);

            // 5 elipsin birleşmesi (klasik bulut) — merkezde büyük, kenarlarda küçük
            // Yeni 480×170 ölçek için orantılı koordinatlar
            EllipsDoldur(tex, 240,  85, 190, 65, beyaz); // ana
            EllipsDoldur(tex, 100,  95,  90, 52, beyaz); // sol
            EllipsDoldur(tex, 380,  95,  90, 52, beyaz); // sağ
            EllipsDoldur(tex, 170, 130,  78, 38, beyaz); // sol üst
            EllipsDoldur(tex, 310, 130,  78, 38, beyaz); // sağ üst

            // Border: 3px siyah
            BorderEkle(tex, w, h, border, 3);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>Küçük dolu daire (düşünce yolu) — beyaz dolgu + siyah border.</summary>
        private static Sprite DaireSpriteOlustur()
        {
            int w = 32, h = 32;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color beyaz = new Color32(255, 255, 255, 255);
            Color border = new Color32(44, 44, 42, 255);
            Color saydam = new Color32(0, 0, 0, 0);

            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = saydam;
            tex.SetPixels(pixels);

            DaireDoldur(tex, 16, 16, 14, beyaz);
            BorderEkle(tex, w, h, border, 2);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        // ──────────────────────────────────────────────────────────────────────
        // PİKSEL ÇİZİM YARDIMCILARI
        // ──────────────────────────────────────────────────────────────────────

        private static void DaireDoldur(Texture2D tex, int cx, int cy, int r, Color renk)
        {
            int rSq = r * r;
            for (int y = cy - r; y <= cy + r; y++)
            {
                for (int x = cx - r; x <= cx + r; x++)
                {
                    if (x < 0 || y < 0 || x >= tex.width || y >= tex.height) continue;
                    int dx = x - cx, dy = y - cy;
                    if (dx * dx + dy * dy <= rSq) tex.SetPixel(x, y, renk);
                }
            }
        }

        private static void EllipsDoldur(Texture2D tex, int cx, int cy, int rx, int ry, Color renk)
        {
            for (int y = cy - ry; y <= cy + ry; y++)
            {
                for (int x = cx - rx; x <= cx + rx; x++)
                {
                    if (x < 0 || y < 0 || x >= tex.width || y >= tex.height) continue;
                    float dx = (float)(x - cx) / rx;
                    float dy = (float)(y - cy) / ry;
                    if (dx * dx + dy * dy <= 1f) tex.SetPixel(x, y, renk);
                }
            }
        }

        private static void DikdortgenDoldur(Texture2D tex, int x0, int y0, int x1, int y1, Color renk)
        {
            for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                {
                    if (x < 0 || y < 0 || x >= tex.width || y >= tex.height) continue;
                    tex.SetPixel(x, y, renk);
                }
        }

        /// <summary>Şeffaf piksele komşu opak piksel varsa o komşuya border rengi yaz.</summary>
        private static void BorderEkle(Texture2D tex, int w, int h, Color borderRenk, int kalinlik)
        {
            // Mevcut alpha bilgisi snapshot (border yazımı diğer piksel komşuluğunu bozmasın)
            var snapshot = tex.GetPixels();
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color mevcut = snapshot[y * w + x];
                    if (mevcut.a > 0.5f) continue; // dolu piksel: dokunma
                    bool komsuVar = false;
                    for (int dy = -kalinlik; dy <= kalinlik && !komsuVar; dy++)
                    {
                        for (int dx = -kalinlik; dx <= kalinlik && !komsuVar; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                            if (snapshot[ny * w + nx].a > 0.5f) komsuVar = true;
                        }
                    }
                    if (komsuVar) tex.SetPixel(x, y, borderRenk);
                }
            }
        }
    }
}
