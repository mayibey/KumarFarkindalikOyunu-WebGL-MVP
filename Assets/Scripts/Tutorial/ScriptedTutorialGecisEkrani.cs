using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace KumarFarkindalik.Tutorial
{
    /// <summary>
    /// A7 final ekranından TAMAM ile çıkışta açılan "BİLGİLENDİRİCİ ASİSTAN" geçiş ekranı.
    /// Görsel olarak ScriptedModalKopru patternini birebir taklit eder (renk, layout, typewriter,
    /// karakter görseli, slide-in animasyonu) — TEK FARK: TAMAM yerine 2 buton (sağ-alt yan yana):
    ///   • [YENİDEN OYNA]   → SaveLoadServisi.Sil() + 03_SenaryoluOyun
    ///   • [HADİ GÖRELİM]   → 04_AdminOyunScene
    ///
    /// PAKET 2: 03_SenaryoluOyun'a runtime'da iliştirilir. ScriptedModalKopru sortingOrder=1500,
    /// final ekran 1800 → bu modal 1900 (en üstte). ScriptedModalKopru.cs DOKUNULMAZ.
    /// </summary>
    [Preserve]
    public class ScriptedTutorialGecisEkrani : MonoBehaviour
    {
        public const int ANLATICI_SAHNE_BUILD_INDEX = 2; // 03_SenaryoluOyun
        public const int CANVAS_SORTING_ORDER = 1900;    // Final ekran (1800) + 100

        public static ScriptedTutorialGecisEkrani Ornek { get; private set; }
        public static bool IsAcik { get; private set; }

        // === Görsel sabitler — ScriptedModalKopru ile birebir ===
        private const float SLIDE_SURE = 0.4f;
        private const float CIKIS_SURE = 0.3f;
        private const float TYPEWRITER_HARF_BASINA = 0.015f;
        private const float DEVAM_FADE_SURE = 0.3f;

        private static readonly Color BALON_RENK = new Color(0.10f, 0.16f, 0.23f, 0.95f); // dark navy
        private static readonly Color ALTIN_RENK = new Color(0.83f, 0.69f, 0.22f, 1f);    // başlık + balon border
        private static readonly Color MESAJ_RENK = new Color(0.95f, 0.97f, 1f, 1f);
        private static readonly Color BUTON_ARKA = new Color(0.12f, 0.12f, 0.12f, 0.75f);
        private static readonly Color BUTON_BORDER_CTA = new Color(0.98f, 0.78f, 0.46f, 1f); // HADİ GÖRELİM
        private static readonly Color BUTON_BORDER_NOTR = new Color(0.50f, 0.50f, 0.55f, 1f); // YENİDEN OYNA

        private const string MESAJ =
            "Az önce bir kumar bağımlısının yaşadıklarını gördün. " +
            "Peki bu manipülasyonlar nasıl tasarlanıyor? " +
            "Sahne arkasını birlikte görelim. " +
            "Sistemin perde arkasındaki mühendisliği öğrenmek ister misin?";

        // === UI referansları ===
        private GameObject _root;
        private RectTransform _balonRt;
        private CanvasGroup _balonCanvasGroup;
        private TextMeshProUGUI _mesajText;
        private Button _balonButton;
        private CanvasGroup _butonlarCanvasGroup;
        private Button _yenidenOynaButton;
        private Button _hadiGorelimButton;

        private Vector2 _balonAcikPos;
        private Vector2 _balonKapaliPos;

        // === State ===
        private bool _typewriterCalisiyor;
        private bool _typewriterAtla;
        private bool _yazmaTamamlandi;
        private bool _gecisYapildi;

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
            if (scene.buildIndex != ANLATICI_SAHNE_BUILD_INDEX)
            {
                if (Ornek != null) Object.Destroy(Ornek.gameObject);
                return;
            }
            if (Ornek != null) return;
            var go = new GameObject(nameof(ScriptedTutorialGecisEkrani));
            go.AddComponent<ScriptedTutorialGecisEkrani>();
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

            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGO = new GameObject("EventSystem (Auto)");
                esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            UIYarat();
            if (_root != null) _root.SetActive(false);
            IsAcik = false;
            _gecisYapildi = false;
        }

        private void OnDestroy()
        {
            if (Ornek == this) Ornek = null;
            IsAcik = false;
        }

        /// <summary>
        /// Modal'ı açar; slide-in → typewriter → kullanıcı buton seçer → sahne yükle.
        /// TutorialFinalWatcher tarafından çağrılır.
        /// </summary>
        public void Goster()
        {
            if (_root == null) return;
            if (IsAcik) return;
            IsAcik = true;
            _gecisYapildi = false;
            _root.SetActive(true);
            StartCoroutine(GosterAkisi());
        }

        public void Gizle()
        {
            if (!IsAcik) return;
            IsAcik = false;
            StartCoroutine(KapatAkisi());
        }

        private IEnumerator GosterAkisi()
        {
            // State reset
            _typewriterCalisiyor = false;
            _typewriterAtla = false;
            _yazmaTamamlandi = false;
            _mesajText.text = "";
            _mesajText.maxVisibleCharacters = 0;
            if (_butonlarCanvasGroup != null)
            {
                _butonlarCanvasGroup.alpha = 0f;
                _butonlarCanvasGroup.interactable = false;
                _butonlarCanvasGroup.blocksRaycasts = false;
            }

            // Dinamik balon yüksekliği — ScriptedModalKopru:136-143 patterni
            const float BALON_GENISLIK = 680f;
            const float MESAJ_GENISLIK = BALON_GENISLIK - 40f;
            const float DIKEY_REZERV = 160f; // başlık 45 + buton şeridi 60 + alt padding 12 + üst pad
            const float MIN_YUKSEKLIK = 240f;
            const float MAX_YUKSEKLIK = 800f;
            Vector2 prefSize = _mesajText.GetPreferredValues(MESAJ, MESAJ_GENISLIK, 0f);
            float balonYukseklik = Mathf.Clamp(prefSize.y + DIKEY_REZERV, MIN_YUKSEKLIK, MAX_YUKSEKLIK);
            _balonRt.sizeDelta = new Vector2(BALON_GENISLIK, balonYukseklik);
            _balonAcikPos = new Vector2(0f, 0f);
            _balonKapaliPos = new Vector2(0f, -700f);

            yield return KarakterGiris();
            yield return TypewriterYaz(MESAJ);
            _yazmaTamamlandi = true;
            yield return ButonlarFadeIn();
            // Akış burada biter — kullanıcı butona basınca OnYenidenOyna/OnHadiGorelim devreye girer.
        }

        private IEnumerator KapatAkisi()
        {
            yield return KarakterCikis();
            if (_root != null) _root.SetActive(false);
        }

        // === Animasyonlar — ScriptedModalKopru:200-287 birebir kopya ===

        private IEnumerator KarakterGiris()
        {
            float t = 0f;
            _balonRt.anchoredPosition = _balonKapaliPos;
            if (_balonCanvasGroup != null) _balonCanvasGroup.alpha = 1f;
            while (t < SLIDE_SURE)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / SLIDE_SURE);
                float eased = 1f - Mathf.Pow(1f - u, 3f);
                _balonRt.anchoredPosition = Vector2.Lerp(_balonKapaliPos, _balonAcikPos, eased);
                yield return null;
            }
            _balonRt.anchoredPosition = _balonAcikPos;
        }

        private IEnumerator KarakterCikis()
        {
            float t = 0f;
            float baslangicAlpha = _balonCanvasGroup != null ? _balonCanvasGroup.alpha : 1f;
            Vector2 balonStart = _balonRt.anchoredPosition;
            Vector3 balonScaleStart = _balonRt.localScale;
            while (t < CIKIS_SURE)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / CIKIS_SURE);
                _balonRt.anchoredPosition = Vector2.Lerp(balonStart, _balonKapaliPos, u);
                _balonRt.localScale = Vector3.Lerp(balonScaleStart, balonScaleStart * 0.95f, u);
                if (_balonCanvasGroup != null)
                    _balonCanvasGroup.alpha = Mathf.Lerp(baslangicAlpha, 0f, u);
                yield return null;
            }
            _balonRt.localScale = balonScaleStart;
        }

        private IEnumerator TypewriterYaz(string mesaj)
        {
            _typewriterCalisiyor = true;
            _typewriterAtla = false;
            _mesajText.text = mesaj;
            _mesajText.maxVisibleCharacters = 0;
            _mesajText.ForceMeshUpdate();
            int toplamHarf = _mesajText.textInfo.characterCount;
            for (int i = 0; i <= toplamHarf; i++)
            {
                if (_typewriterAtla)
                {
                    _mesajText.maxVisibleCharacters = toplamHarf;
                    break;
                }
                _mesajText.maxVisibleCharacters = i;
                float bekle = TYPEWRITER_HARF_BASINA;
                float t = 0f;
                while (t < bekle && !_typewriterAtla)
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            if (_typewriterAtla) _mesajText.maxVisibleCharacters = toplamHarf;
            _typewriterCalisiyor = false;
        }

        private IEnumerator ButonlarFadeIn()
        {
            if (_butonlarCanvasGroup == null) yield break;
            float t = 0f;
            _butonlarCanvasGroup.alpha = 0f;
            while (t < DEVAM_FADE_SURE)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / DEVAM_FADE_SURE);
                _butonlarCanvasGroup.alpha = u;
                yield return null;
            }
            _butonlarCanvasGroup.alpha = 1f;
            _butonlarCanvasGroup.interactable = true;
            _butonlarCanvasGroup.blocksRaycasts = true;
        }

        // === Click handler'lar ===

        private void OnBalonTiklandi()
        {
            // Yazma sırasında balon/karakter tıklanırsa typewriter atlanır.
            if (_typewriterCalisiyor) _typewriterAtla = true;
            // Yazma bitince balon tıklaması KAPATMAZ — kullanıcı 2 butondan birini seçmeli.
        }

        private void OnYenidenOynaTiklandi()
        {
            if (_gecisYapildi) return;
            _gecisYapildi = true;
            if (_yenidenOynaButton != null) _yenidenOynaButton.interactable = false;
            if (_hadiGorelimButton != null) _hadiGorelimButton.interactable = false;
            Debug.Log("[ScriptedTutorialGecisEkrani] YENİDEN OYNA → tam save temizlik + 03_SenaryoluOyun");
            TamSaveTemizlik();
            Gizle();
            SceneManager.LoadScene("03_SenaryoluOyun");
        }

        private void OnHadiGorelimTiklandi()
        {
            if (_gecisYapildi) return;
            _gecisYapildi = true;
            if (_yenidenOynaButton != null) _yenidenOynaButton.interactable = false;
            if (_hadiGorelimButton != null) _hadiGorelimButton.interactable = false;
            Debug.Log("[ScriptedTutorialGecisEkrani] HADİ GÖRELİM → tam save temizlik + 04_AdminOyunScene");
            TamSaveTemizlik();
            Gizle();
            SceneManager.LoadScene("04_AdminOyunScene");
        }

        /// <summary>
        /// 04 sahnesi tutorial başlangıcında / 03 yeniden başlangıcında save state'inin
        /// 4 farklı yerden silinmesini garanti eder.
        ///   - SaveLoadServisi (KumarSaveData_v1 PlayerPrefs key)
        ///   - PlayerPrefs ayrı key'leri: KullaniciAdi + KumarRestoreModuActif
        ///   - KullaniciVerileri.KullaniciAdi static field
        /// "Hoş Geldiniz {eski_ad}" kalıntısını temizler. Yeni sahnede tutorial sabit "Eğitim Modu".
        /// </summary>
        private static void TamSaveTemizlik()
        {
            SaveLoadServisi.Sil();
            PlayerPrefs.DeleteKey("KullaniciAdi");
            PlayerPrefs.DeleteKey("KumarRestoreModuActif");
            PlayerPrefs.Save();
            KullaniciVerileri.KullaniciAdi = "Eğitim Modu";
            Debug.Log("[ScriptedTutorialGecisEkrani] TamSaveTemizlik: SaveLoad sil + PlayerPrefs DeleteKey x2 + KullaniciVerileri reset.");
        }

        // === UI yarat (runtime — ScriptedModalKopru:316-466 stilini birebir taklit) ===

        private void UIYarat()
        {
            // Root canvas
            _root = new GameObject("ScriptedTutorialGecisEkraniCanvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _root.transform.SetParent(transform, false);
            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CANVAS_SORTING_ORDER;
            var scaler = _root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // === BALON ===
            var balonGo = new GameObject("Balon",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(CanvasGroup), typeof(Button));
            balonGo.transform.SetParent(_root.transform, false);
            _balonRt = balonGo.GetComponent<RectTransform>();
            _balonRt.anchorMin = _balonRt.anchorMax = _balonRt.pivot = new Vector2(0.5f, 0.5f);
            _balonRt.sizeDelta = new Vector2(680f, 440f);
            _balonAcikPos = new Vector2(0f, 0f);
            _balonKapaliPos = new Vector2(0f, -700f);
            _balonRt.anchoredPosition = _balonKapaliPos;
            var balonImg = balonGo.GetComponent<Image>();
            balonImg.color = BALON_RENK;
            _balonCanvasGroup = balonGo.GetComponent<CanvasGroup>();
            _balonButton = balonGo.GetComponent<Button>();
            _balonButton.transition = Selectable.Transition.None;
            _balonButton.onClick.AddListener(OnBalonTiklandi);

            // Balon altın border (2px — ScriptedModalKopru ile aynı)
            BorderEkle(balonGo.transform, _balonRt.sizeDelta, 2f, ALTIN_RENK);

            // Karakter işaret oku (sol kenarda küçük çıkıntı)
            var okGo = new GameObject("Ok",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            okGo.transform.SetParent(balonGo.transform, false);
            var okRt = okGo.GetComponent<RectTransform>();
            okRt.anchorMin = okRt.anchorMax = new Vector2(0f, 0.5f);
            okRt.pivot = new Vector2(1f, 0.5f);
            okRt.sizeDelta = new Vector2(20f, 30f);
            okRt.anchoredPosition = Vector2.zero;
            var okImg = okGo.GetComponent<Image>();
            okImg.color = BALON_RENK;
            okImg.raycastTarget = false;

            // === KARAKTER (balon child, sol kenara bitişik) ===
            var karakterGo = new GameObject("Karakter",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            karakterGo.transform.SetParent(balonGo.transform, false);
            var karakterRt = karakterGo.GetComponent<RectTransform>();
            karakterRt.anchorMin = karakterRt.anchorMax = new Vector2(0f, 0.5f);
            karakterRt.pivot = new Vector2(1f, 0.5f);
            karakterRt.sizeDelta = new Vector2(220f, 280f);
            karakterRt.anchoredPosition = new Vector2(-20f, 0f);
            var karakterImg = karakterGo.GetComponent<Image>();
            karakterImg.sprite = EgitmenGorseliniAl();
            karakterImg.preserveAspect = true;
            karakterImg.raycastTarget = true;
            var karakterBtn = karakterGo.AddComponent<Button>();
            karakterBtn.transition = Selectable.Transition.None;
            karakterBtn.onClick.AddListener(OnBalonTiklandi);

            // === BAŞLIK: "BİLGİLENDİRİCİ ASİSTAN" ===
            var basGo = new GameObject("Baslik",
                typeof(RectTransform), typeof(CanvasRenderer));
            basGo.transform.SetParent(balonGo.transform, false);
            var basRt = basGo.GetComponent<RectTransform>();
            basRt.anchorMin = new Vector2(0f, 1f);
            basRt.anchorMax = new Vector2(1f, 1f);
            basRt.pivot = new Vector2(0.5f, 1f);
            basRt.sizeDelta = new Vector2(0f, 32f);
            basRt.anchoredPosition = new Vector2(0f, -10f);
            var baslikText = basGo.AddComponent<TextMeshProUGUI>();
            baslikText.alignment = TextAlignmentOptions.Center;
            baslikText.fontSize = 18f;
            baslikText.fontStyle = FontStyles.Bold;
            baslikText.color = ALTIN_RENK;
            baslikText.text = "BİLGİLENDİRİCİ ASİSTAN";
            baslikText.raycastTarget = false;

            // === MESAJ (typewriter target) — alt 60 (buton şeridi yer açar) ===
            var mesajGo = new GameObject("Mesaj",
                typeof(RectTransform), typeof(CanvasRenderer));
            mesajGo.transform.SetParent(balonGo.transform, false);
            var mesajRt = mesajGo.GetComponent<RectTransform>();
            mesajRt.anchorMin = new Vector2(0f, 0f);
            mesajRt.anchorMax = new Vector2(1f, 1f);
            mesajRt.offsetMin = new Vector2(20f, 62f);   // alt 62 (buton şeridi 46 + alt 12 + 4 boşluk)
            mesajRt.offsetMax = new Vector2(-20f, -45f); // sağ 20, üst 45 (başlık)
            _mesajText = mesajGo.AddComponent<TextMeshProUGUI>();
            _mesajText.alignment = TextAlignmentOptions.TopJustified;
            _mesajText.fontSize = 19f;
            _mesajText.fontStyle = FontStyles.Normal;
            _mesajText.color = MESAJ_RENK;
            _mesajText.enableWordWrapping = true;
            _mesajText.text = "";
            _mesajText.raycastTarget = false;

            // === BUTON KAP (alt-orta simetrik yan yana 2 buton) ===
            // Genişlik 355 = 160 + 35 + 160 (sol buton + boşluk + sağ buton).
            // Pivot (0.5, 0) → balon alt-merkeze sabit; her buton kendi merkezinden büyür.
            var butonKap = new GameObject("ButonKap",
                typeof(RectTransform), typeof(CanvasGroup));
            butonKap.transform.SetParent(balonGo.transform, false);
            var bkRt = butonKap.GetComponent<RectTransform>();
            bkRt.anchorMin = new Vector2(0.5f, 0f);
            bkRt.anchorMax = new Vector2(0.5f, 0f);
            bkRt.pivot = new Vector2(0.5f, 0f);
            bkRt.sizeDelta = new Vector2(355f, 46f);
            bkRt.anchoredPosition = new Vector2(0f, 12f);
            _butonlarCanvasGroup = butonKap.GetComponent<CanvasGroup>();
            _butonlarCanvasGroup.alpha = 0f;
            _butonlarCanvasGroup.interactable = false;
            _butonlarCanvasGroup.blocksRaycasts = false;

            // Buton merkezleri: ±97.5 (kendi merkezleri, 35px aralık).
            // Sol buton: YENİDEN OYNA (nötr — gri border)
            _yenidenOynaButton = ButonYarat(
                butonKap.transform, "YenidenOynaButon", "YENİDEN OYNA",
                anchor: new Vector2(0.5f, 0.5f), anchoredPos: new Vector2(-97.5f, 0f),
                size: new Vector2(160f, 46f),
                borderRenk: BUTON_BORDER_NOTR,
                onClick: OnYenidenOynaTiklandi);

            // Sağ buton: HADİ GÖRELİM (CTA — altın border)
            _hadiGorelimButton = ButonYarat(
                butonKap.transform, "HadiGorelimButon", "HADİ GÖRELİM",
                anchor: new Vector2(0.5f, 0.5f), anchoredPos: new Vector2(97.5f, 0f),
                size: new Vector2(160f, 46f),
                borderRenk: BUTON_BORDER_CTA,
                onClick: OnHadiGorelimTiklandi);
        }

        /// <summary>Buton yaratıcı — ScriptedModalKopru TAMAM butonu stiliyle birebir (sadece border rengi farklı).</summary>
        private Button ButonYarat(
            Transform parent, string adi, string metin,
            Vector2 anchor, Vector2 anchoredPos, Vector2 size,
            Color borderRenk, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(adi,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            var img = go.GetComponent<Image>();
            img.color = BUTON_ARKA;
            BorderEkle(go.transform, size, 1.5f, borderRenk);
            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(onClick);

            var txtGo = new GameObject("Txt", typeof(RectTransform), typeof(CanvasRenderer));
            txtGo.transform.SetParent(go.transform, false);
            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = txtRt.offsetMax = Vector2.zero;
            var txt = txtGo.AddComponent<TextMeshProUGUI>();
            txt.alignment = TextAlignmentOptions.Center;
            txt.fontSize = 18f;
            txt.fontStyle = FontStyles.Bold;
            txt.color = Color.white;
            txt.text = metin;
            txt.raycastTarget = false;
            return btn;
        }

        // === Border helper (ScriptedModalKopru:472-494 birebir kopya) ===
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

        // === Karakter görseli — Resources/egitmenyuz.png (yoksa null Image kalır) ===
        private static Sprite _cachedSprite;

        private static Sprite EgitmenGorseliniAl()
        {
            if (_cachedSprite != null) return _cachedSprite;
            var sprite = Resources.Load<Sprite>("egitmenyuz");
            if (sprite != null)
            {
                _cachedSprite = sprite;
                return sprite;
            }
            Debug.LogWarning("[ScriptedTutorialGecisEkrani] Resources/egitmenyuz.png bulunamadı.");
            return null;
        }
    }
}
