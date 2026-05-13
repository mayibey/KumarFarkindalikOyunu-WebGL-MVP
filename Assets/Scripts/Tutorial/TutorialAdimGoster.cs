using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace KumarFarkindalik.Tutorial
{
    /// <summary>
    /// Görev Takip Paneli — 280×320, BAKIYE YÜKLE konumunda (801.18, 222).
    /// PAKET 3B-fix-8 polish: 2 katmanlı altın border + header bandı + sayaç outline + subtle pulse.
    /// </summary>
    [Preserve]
    public class TutorialAdimGoster : MonoBehaviour
    {
        public const int TUTORIAL_SAHNE_BUILD_INDEX = 3;
        public const int CANVAS_SORTING_ORDER = 1650;

        public static TutorialAdimGoster Ornek { get; private set; }

        // === Renk paleti (PAKET 3B-fix-8 polish) ===
        private static readonly Color BALON_RENK   = new Color(0.10f, 0.16f, 0.23f, 0.95f); // body navy
        private static readonly Color BALON_KOYU   = new Color(0.06f, 0.10f, 0.14f, 1f);    // header (daha koyu)
        private static readonly Color ALTIN_KOYU   = new Color(0.65f, 0.52f, 0.15f, 1f);    // dış border + outline
        private static readonly Color ALTIN_RENK   = new Color(0.83f, 0.69f, 0.22f, 1f);    // başlık + İLERİ aktif zemin
        private static readonly Color ALTIN_ACIK   = new Color(0.95f, 0.80f, 0.30f, 1f);    // iç border + aksan
        private static readonly Color BUTON_ARKA   = new Color(0.12f, 0.12f, 0.12f, 0.75f);
        private static readonly Color YESIL        = new Color(0.45f, 0.85f, 0.45f, 1f);
        private static readonly Color GRI          = new Color(0.78f, 0.78f, 0.80f, 1f);
        private static readonly Color BEYAZ        = new Color(0.95f, 0.97f, 1f, 1f);
        private static readonly Color KOYU_YAZI    = new Color(0.06f, 0.08f, 0.12f, 1f);    // altın buton üstü yazı
        // PAKET 3B-fix-15: Yapılacaklar listesi satır renkleri (tamamlandı yeşil, beklemede beyaz)
        private static readonly Color SATIR_BEYAZ  = new Color(1f, 1f, 1f, 1f);
        private static readonly Color SATIR_YESIL  = new Color(0.30f, 0.80f, 0.35f, 1f);    // #4DCC59
        // PAKET 14-FAZ14: Spin geçmişi mini bar segment renkleri (03 anlatici.html ile aynı palet)
        private static readonly Color SEG_BOS    = new Color(1f, 1f, 1f, 0.12f);    // rgba beyaz/12%
        private static readonly Color SEG_KAZANC = new Color(0.365f, 0.835f, 0.365f, 1f);  // #5DD55D
        private static readonly Color SEG_KAYIP  = new Color(1f, 0.333f, 0.333f, 1f);      // #FF5555
        private static readonly Color SEG_NOTR   = new Color(0.376f, 0.647f, 0.980f, 1f);  // #60A5FA

        private const int TOPLAM_ADIM = 12; // PAKET 6C2: T6_YENI_OYUNCU eklendi (11 → 12)
        private const float PULSE_PERIYOT = 2.5f;
        private const float PULSE_OLCEK = 1.012f;

        public event Action OnIleriTiklandi;

        // === UI referansları ===
        private GameObject _root;
        private RectTransform _panelRt;
        private TextMeshProUGUI _sayacText;
        private TextMeshProUGUI _altBaslikText;
        private GameObject _yapilacaklarBlok;
        private TextMeshProUGUI[] _yapilacakSatirlari = new TextMeshProUGUI[3];
        private GameObject _ilerlemeBlok;
        private TextMeshProUGUI _parametreText;
        private TextMeshProUGUI _spinText;
        // PAKET 14-FAZ14: Spin geçmişi mini bar (03 anlatici.html .spin-bar pattern'i)
        private GameObject _spinGecmisiBlok;
        private Image[] _spinSegmentler = new Image[0];
        private int _spinSegmentSayisi = 0;
        private Button _ileriButton;
        private Image _ileriButtonImg;
        private TextMeshProUGUI _ileriButtonTxt;
        private Coroutine _pulseCoroutine;

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
            if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
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

            // PAKET 14-FAZ14: Spin geçmişi mini bar — gerekliSpin>0 ise segment'leri yarat, değilse gizle
            int gerekliSpin = TutorialOyunYoneticisi.Ornek?.AdimYoneticisi?.MevcutAdimVerisi?.gerekliSpin ?? 0;
            SpinGecmisiKur(gerekliSpin);

            if (yapVar)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (_yapilacakSatirlari[i] == null) continue;
                    if (i < yapilacaklar.Length)
                    {
                        // PAKET 3B-fix-15: Yeni adımda renk + prefix RESET (önceki adımın yeşilleri silinir)
                        _yapilacakSatirlari[i].text = "→ " + yapilacaklar[i];
                        _yapilacakSatirlari[i].color = SATIR_BEYAZ;
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

            // Pulse coroutine başlat (idempotent)
            if (_pulseCoroutine == null) _pulseCoroutine = StartCoroutine(PulseLoop());
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

            // Pasif: BUTON_ARKA + beyaz text + 0.45 alpha
            // Aktif: ALTIN_RENK zemin + KOYU_YAZI text + 1.0 alpha
            if (_ileriButtonImg != null)
            {
                _ileriButtonImg.color = aktif ? ALTIN_RENK : BUTON_ARKA;
            }
            if (_ileriButtonTxt != null)
            {
                _ileriButtonTxt.color = aktif ? KOYU_YAZI : new Color(1f, 1f, 1f, 0.6f);
            }
        }

        public void Gizle()
        {
            if (_root != null) _root.SetActive(false);
            if (_pulseCoroutine != null) { StopCoroutine(_pulseCoroutine); _pulseCoroutine = null; }
            if (_panelRt != null) _panelRt.localScale = Vector3.one;
        }

        // PAKET 3B-fix-15: Her frame yapılacaklar listesi renk + prefix polling.
        // Aktif adım değişimleri (parametre değişti, spin sayısı arttı) anında satıra yansır.
        // Overhead: en fazla ~5 anahtar HashSet.Contains O(1) + 3 satır text/color atama.
        private void Update()
        {
            if (_root == null || !_root.activeSelf) return;
            if (_yapilacaklarBlok == null || !_yapilacaklarBlok.activeSelf) return;
            YapilacaklarRenkGuncelle();
            SpinGecmisiRenkGuncelle();
        }

        // PAKET 14-FAZ14: Spin geçmişi mini bar — gerekliSpin sayısına göre dinamik segment Image children.
        // 03 anlatici.html .spin-bar pattern'i (yatay flex, 8px height, 4px gap).
        private void SpinGecmisiKur(int gerekliSpin)
        {
            if (_spinGecmisiBlok == null) return;
            // Önceki segment'leri yok et
            for (int i = 0; i < _spinSegmentler.Length; i++)
                if (_spinSegmentler[i] != null) UnityEngine.Object.Destroy(_spinSegmentler[i].gameObject);

            if (gerekliSpin <= 0)
            {
                _spinSegmentler = new Image[0];
                _spinSegmentSayisi = 0;
                _spinGecmisiBlok.SetActive(false);
                return;
            }
            _spinGecmisiBlok.SetActive(true);
            _spinSegmentSayisi = gerekliSpin;
            _spinSegmentler = new Image[gerekliSpin];

            const float TOPLAM_GENISLIK = 240f;
            const float GAP = 4f;
            float segGenislik = (TOPLAM_GENISLIK - GAP * (gerekliSpin - 1)) / gerekliSpin;
            float baslangicX = -TOPLAM_GENISLIK / 2f;
            for (int i = 0; i < gerekliSpin; i++)
            {
                var sgGo = new GameObject($"Segment_{i}",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                sgGo.transform.SetParent(_spinGecmisiBlok.transform, false);
                var rt = sgGo.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.sizeDelta = new Vector2(segGenislik, 8f);
                rt.anchoredPosition = new Vector2(baslangicX + i * (segGenislik + GAP), -16f);
                var img = sgGo.GetComponent<Image>();
                img.color = SEG_BOS;
                img.raycastTarget = false;
                _spinSegmentler[i] = img;
            }
        }

        private void SpinGecmisiRenkGuncelle()
        {
            if (_spinSegmentler == null || _spinSegmentler.Length == 0) return;
            var netList = TutorialOyunYoneticisi.AktifAdimSpinNetleri;
            if (netList == null) return;
            for (int i = 0; i < _spinSegmentler.Length; i++)
            {
                if (_spinSegmentler[i] == null) continue;
                Color hedef;
                if (i < netList.Count)
                {
                    int net = netList[i];
                    if (net > 0) hedef = SEG_KAZANC;
                    else if (net < 0) hedef = SEG_KAYIP;
                    else hedef = SEG_NOTR;
                }
                else hedef = SEG_BOS;
                if (_spinSegmentler[i].color != hedef)
                    _spinSegmentler[i].color = hedef;
            }
        }

        /// <summary>
        /// Strateji B (TumAnahtarlarTamam fallback):
        ///   - i &lt; paramSayisi → degisimAnahtarlari[i] kontrolü
        ///   - i == son madde + gerekliSpin > 0 + ekstra madde var → spin koşulu
        ///   - aradakiler ("açma/uygula bas" maddeleri) → tüm anahtarlar tamam mı fallback
        /// </summary>
        public void YapilacaklarRenkGuncelle()
        {
            var oy = TutorialOyunYoneticisi.Ornek;
            var ay = oy?.AdimYoneticisi;
            var v = ay?.MevcutAdimVerisi;
            if (v?.yapilacaklar == null || v.yapilacaklar.Length == 0) return;

            int yapilacakSayisi = v.yapilacaklar.Length;
            int paramSayisi = v.degisimAnahtarlari?.Length ?? 0;

            // Tüm anahtarlar tamam mı (fallback için)
            bool tumAnahtarlarTamam = paramSayisi > 0;
            for (int k = 0; k < paramSayisi; k++)
            {
                if (!ay.AdimSirasindaDegistirildi(v.degisimAnahtarlari[k]))
                {
                    tumAnahtarlarTamam = false;
                    break;
                }
            }

            // Spin koşulu (delta = mevcut spin - adım başlangıç spin)
            int delta = oy.TutorialSpinSayaci - ay.AdimBaslangicSpin;
            bool spinTamam = v.gerekliSpin > 0 && delta >= v.gerekliSpin;

            for (int i = 0; i < yapilacakSayisi && i < _yapilacakSatirlari.Length; i++)
            {
                if (_yapilacakSatirlari[i] == null) continue;

                bool tamam;
                bool sonMaddeSpin = (i == yapilacakSayisi - 1)
                                    && v.gerekliSpin > 0
                                    && yapilacakSayisi > paramSayisi;

                if (sonMaddeSpin)
                    tamam = spinTamam;
                else
                {
                    // HOTFIX: AdimSirasindaDegistirildi sadece "key gönderildi mi" — değere bakmıyordu.
                    // parametreKosulu lambda GERÇEK değer kontrolü yapar (örn carpanOlasilik >= %10,
                    // aktifSenaryo == "hook", maksCarpan > 0 vb.). Tüm parametre + ara maddeler için
                    // tek lambda yeşillik kararını verir. parametreKosulu null ise fallback eski mantık.
                    tamam = v.parametreKosulu?.Invoke() ?? tumAnahtarlarTamam;
                }

                string prefix = tamam ? "✓ " : "→ ";
                // PAKET 5: Spin sayacı son maddeye entegre — "5 spin at (2/5)"
                string asilMetin = v.yapilacaklar[i];
                if (sonMaddeSpin)
                {
                    int saymaDelta = oy.TutorialSpinSayaci - ay.AdimBaslangicSpin;
                    int kalan = Mathf.Min(Mathf.Max(0, saymaDelta), v.gerekliSpin);
                    asilMetin += $" ({kalan}/{v.gerekliSpin})";
                }
                string yeniText = prefix + asilMetin;
                if (_yapilacakSatirlari[i].text != yeniText)
                    _yapilacakSatirlari[i].text = yeniText;
                Color hedefRenk = tamam ? SATIR_YESIL : SATIR_BEYAZ;
                if (_yapilacakSatirlari[i].color != hedefRenk)
                    _yapilacakSatirlari[i].color = hedefRenk;
            }
        }

        // === Pulse coroutine — subtle dikkat çekme (SpinButtonAnimator pattern) ===

        private IEnumerator PulseLoop()
        {
            if (_panelRt == null) yield break;
            Vector3 baseScale = Vector3.one;
            float t = 0f;
            while (_panelRt != null)
            {
                t += Time.unscaledDeltaTime;
                float u = (t % PULSE_PERIYOT) / PULSE_PERIYOT;
                float ping = Mathf.PingPong(u * 2f, 1f);
                float ease = ping * ping * (3f - 2f * ping); // smoothstep
                _panelRt.localScale = baseScale * (1f + ease * (PULSE_OLCEK - 1f));
                yield return null;
            }
        }

        // === UI yaratımı — 280×320 panel + premium çerçeve + header bandı ===

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

            // Ana panel — body navy
            var panel = new GameObject("Panel",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(_root.transform, false);
            _panelRt = panel.GetComponent<RectTransform>();
            _panelRt.anchorMin = _panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            _panelRt.pivot = new Vector2(0.5f, 0.5f);
            _panelRt.sizeDelta = new Vector2(280f, 290f); // PAKET 14-FAZ15: Spin geçmişi barı için 260→290
            _panelRt.anchoredPosition = new Vector2(801.18f, 222f);
            panel.GetComponent<Image>().color = BALON_RENK;

            // 2 katmanlı altın border
            BorderEkle(panel.transform, _panelRt.sizeDelta, 3f, ALTIN_KOYU);          // DIŞ 3px koyu altın
            BorderEkleOfsetli(panel.transform, 3f, 2f, ALTIN_ACIK);                    // İÇ 2px açık altın (3px ofset)

            // === HEADER bandı (üst 55px) ===
            var header = new GameObject("Header", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            header.transform.SetParent(panel.transform, false);
            var headerRt = header.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.sizeDelta = new Vector2(-10f, 55f); // panel iç-genişlikten 5px (her tarafta) içeri
            headerRt.anchoredPosition = new Vector2(0f, -5f);
            header.GetComponent<Image>().color = BALON_KOYU;

            // Header alt aksan çizgisi (2px ALTIN_ACIK)
            CizgiEkle(panel.transform, new Vector2(0f, -60f), new Vector2(240f, 2f), ALTIN_ACIK);

            // PAKET 5: Sayaç başlık fontSize 18→24
            _sayacText = MetinYarat(header.transform, "Baslik", new Vector2(0f, -4f),
                new Vector2(260f, 30f), 24f, FontStyles.Bold, ALTIN_ACIK,
                TextAlignmentOptions.Center, "ADIM ?/11");
            _sayacText.outlineWidth = 0.18f;
            _sayacText.outlineColor = ALTIN_KOYU;

            // PAKET 5: Alt başlık fontSize 14→18, pos -32→-34
            _altBaslikText = MetinYarat(header.transform, "AltBaslik", new Vector2(0f, -34f),
                new Vector2(260f, 22f), 18f, FontStyles.Bold, ALTIN_RENK,
                TextAlignmentOptions.Center, "");
            _altBaslikText.characterSpacing = 4f;

            // === YAPILACAKLAR BLOK (PAKET 5: fontlar büyütüldü 13→16/18, satır yüksekliği 25→30) ===
            _yapilacaklarBlok = new GameObject("YapilacaklarBlok", typeof(RectTransform));
            _yapilacaklarBlok.transform.SetParent(panel.transform, false);
            var ybRt = _yapilacaklarBlok.GetComponent<RectTransform>();
            ybRt.anchorMin = ybRt.anchorMax = new Vector2(0.5f, 1f);
            ybRt.pivot = new Vector2(0.5f, 1f);
            ybRt.sizeDelta = new Vector2(280f, 170f); // 110→170 (ilerleme bloğu yok, yapılacaklar tek blok)
            ybRt.anchoredPosition = new Vector2(0f, -70f);

            MetinYarat(_yapilacaklarBlok.transform, "Baslik_NeYapmali", new Vector2(0f, 0f),
                new Vector2(240f, 28f), 18f, FontStyles.Bold, BEYAZ,
                TextAlignmentOptions.Left, "NE YAPMALISIN:");

            for (int i = 0; i < 3; i++)
            {
                _yapilacakSatirlari[i] = MetinYarat(_yapilacaklarBlok.transform, $"Yap{i}",
                    new Vector2(10f, -34f - i * 32f), new Vector2(230f, 28f), 16f,
                    FontStyles.Normal, GRI, TextAlignmentOptions.Left, "");
            }

            // PAKET 5: 1. Ayraç + ILERLEME BLOK + 2. Ayraç KALDIRILDI.
            // _ilerlemeBlok, _parametreText, _spinText field'ları null kalır (IlerlemeGuncelle null check ile safe).
            // Spin sayacı son yapılacaklar maddesine entegre — YapilacaklarRenkGuncelle dinamik günceller.

            // === PAKET 14-FAZ15: SPİN GEÇMİŞİ MİNİ BAR ===
            // Panel direct child. Panel height 290 → top anchor (0.5,1) pivot (0.5,1) pos -250.
            // Yapilacaklar alt sınırı -240, bar üst -250 → 10px margin. İLERİ buton üst (panel alt + 40 = -250
            // panel center'da) sağ köşede, bar dikey alt -280 → İLERİ üstünden aşağıda, çakışma yok.
            _spinGecmisiBlok = new GameObject("SpinGecmisiBlok", typeof(RectTransform));
            _spinGecmisiBlok.transform.SetParent(panel.transform, false);
            var sgRt = _spinGecmisiBlok.GetComponent<RectTransform>();
            sgRt.anchorMin = sgRt.anchorMax = new Vector2(0.5f, 1f);
            sgRt.pivot = new Vector2(0.5f, 1f);
            sgRt.sizeDelta = new Vector2(260f, 30f);
            sgRt.anchoredPosition = new Vector2(0f, -250f);
            // Başlık (sol kenar)
            MetinYarat(_spinGecmisiBlok.transform, "Baslik_SpinGecmisi", new Vector2(-115f, 0f),
                new Vector2(130f, 14f), 11f, FontStyles.Bold, BEYAZ,
                TextAlignmentOptions.Left, "Spin Geçmişi");
            // Segment'ler dinamik AdimGoster() çağrısında SpinGecmisiKur ile yaratılır.

            // === İLERİ butonu ===
            var btnGo = new GameObject("IleriButon",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(panel.transform, false);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = btnRt.anchorMax = new Vector2(1f, 0f);
            btnRt.pivot = new Vector2(1f, 0f);
            btnRt.sizeDelta = new Vector2(110f, 32f);
            btnRt.anchoredPosition = new Vector2(-12f, 8f);
            _ileriButtonImg = btnGo.GetComponent<Image>();
            _ileriButtonImg.color = BUTON_ARKA;
            BorderEkle(btnGo.transform, btnRt.sizeDelta, 1.5f, ALTIN_RENK);
            _ileriButton = btnGo.GetComponent<Button>();
            _ileriButton.transition = Selectable.Transition.None;
            _ileriButton.onClick.AddListener(() => OnIleriTiklandi?.Invoke());

            var btnTxtGo = new GameObject("Txt", typeof(RectTransform), typeof(CanvasRenderer));
            btnTxtGo.transform.SetParent(btnGo.transform, false);
            var btnTxtRt = btnTxtGo.GetComponent<RectTransform>();
            btnTxtRt.anchorMin = Vector2.zero; btnTxtRt.anchorMax = Vector2.one;
            btnTxtRt.offsetMin = btnTxtRt.offsetMax = Vector2.zero;
            _ileriButtonTxt = btnTxtGo.AddComponent<TextMeshProUGUI>();
            _ileriButtonTxt.alignment = TextAlignmentOptions.Center;
            _ileriButtonTxt.fontSize = 16f;
            _ileriButtonTxt.fontStyle = FontStyles.Bold;
            _ileriButtonTxt.color = new Color(1f, 1f, 1f, 0.6f); // pasif başlangıç
            _ileriButtonTxt.text = "İLERİ →";
            _ileriButtonTxt.raycastTarget = false;
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

        private static void CizgiEkle(Transform parent, Vector2 pos, Vector2 size, Color renk)
        {
            var go = new GameObject("Ayrac", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var img = go.GetComponent<Image>();
            img.color = renk;
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

        /// <summary>
        /// İç border (panel kenarından `kenarOfseti` kadar içeride). Anchor stretch + offset.
        /// </summary>
        private static void BorderEkleOfsetli(Transform parent, float kenarOfseti, float kalinlik, Color renk)
        {
            (string ad, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPos)[] kenarlar =
            {
                ("Ust",  new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-2f * kenarOfseti, kalinlik), new Vector2(0f, -kenarOfseti)),
                ("Alt",  new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-2f * kenarOfseti, kalinlik), new Vector2(0f, kenarOfseti)),
                ("Sol",  new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(kalinlik, -2f * kenarOfseti), new Vector2(kenarOfseti, 0f)),
                ("Sag",  new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(kalinlik, -2f * kenarOfseti), new Vector2(-kenarOfseti, 0f)),
            };
            foreach (var k in kenarlar)
            {
                var go = new GameObject("BorderIc_" + k.ad,
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(parent, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = k.anchorMin;
                rt.anchorMax = k.anchorMax;
                rt.sizeDelta = k.sizeDelta;
                rt.anchoredPosition = k.anchoredPos;
                var img = go.GetComponent<Image>();
                img.color = renk;
                img.raycastTarget = false;
            }
        }
    }
}
