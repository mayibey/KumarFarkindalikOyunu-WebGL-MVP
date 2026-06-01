using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace Senaryo.Scripted
{
    /// <summary>
    /// A7 (Tükeniş) cutscene — A6 sonu bakiye 0'a düşünce otomatik tetiklenir.
    /// İstatistikler: toplam yatırım, son bakiye, toplam kayıp, spin sayısı.
    /// Yeşilay yardım hattı + "Yeniden başla" butonu (sahne reset).
    ///
    /// Update'te <see cref="AnlaticiSeritKopru.AktifAsama"/> == 6 (A7) algılanırsa bir kez açılır.
    /// </summary>
    [Preserve]
    public class ScriptedFinalEkrani : MonoBehaviour
    {
        public const int ANLATICI_SAHNE_BUILD_INDEX = 1;
        public const int BASLANGIC_BAKIYE = 50000;
        public const int BORC_MIKTARI = 50000;
        public const string YESILAY_HATTI = "115";

        public static ScriptedFinalEkrani Ornek { get; private set; }
        /// <summary>Final ekranı açıkken true; SpinButonImpl bunu kontrol edip spin atımını bloke eder.</summary>
        public static bool IsAcik { get; private set; }

        /// <summary>FAZ35.82.1 hotfix: Sahne geçişi defansif reset (yeni 03 admin sahnesi Start'ında).</summary>
        public static void ResetState() { IsAcik = false; }

        private GameObject _root;
        private bool _gosterildi;

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
            var go = new GameObject(nameof(ScriptedFinalEkrani));
            go.AddComponent<ScriptedFinalEkrani>();
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

        private void Update()
        {
            if (_gosterildi) return;
            if (!ScriptedSpinYoneticisi.Aktif) return;
            var anlatici = AnlaticiSeritKopru.Ornek;
            if (anlatici == null) return;
            if (anlatici.AktifAsama != 6) return; // A7 = idx 6

            // İlk A7 algısı → final ekran aç
            _gosterildi = true;
            GosterFinalEkrani();
        }

        public void GosterFinalEkrani()
        {
            if (_root == null) return;

            // SAVE/LOAD: A7 final → eğitim tamamlandı, save'i sil. Yeni oyun yeni save oluşturur.
            SaveLoadServisi.Sil();

            // Anlatici HTML iframe'i gizle — final cutscene tam ekran dramatik. Gizli kalır;
            // Yeniden Başla sahne reload yapar, sahne reset Awake'inde anlatici yeniden açılır.
            AnlaticiSeritKopru.Ornek?.Gizle();
            // İstatistikler runtime'da hesaplanır.
            // FAZ35.134: "Geri aldığın" değeri — BorcAlındı=true ise ScriptedYuklemePaneli.BorcOncesiBakiye okur
            // (borç ekleneden önceki bonus-sonu kalan ~4K). Pedagojik mesaj: borç parası "geri alınan" değil,
            // kendi parasından kurtarılan bakiye gözükür. Borç alınmadıysa (teorik, senaryoda imkansız çünkü panel
            // tek "Borç al" butonu) gerçek bakiye okunur.
            var oy = UnityEngine.Object.FindObjectOfType<OyunYoneticisi>();
            int sonBakiye = Senaryo.Scripted.ScriptedYuklemePaneli.BorcAlindi
                ? Senaryo.Scripted.ScriptedYuklemePaneli.BorcOncesiBakiye
                : (oy != null ? oy.BahisPanelMevcutBakiye() : 0);
            // GERÇEK yatırım: borç alındıysa 100K, alınmadıysa 50K. ScriptedYuklemePaneli.BorcAlindi flag'i
            // butona fiilen tıklandığında set edilir.
            int toplamYatirim = BASLANGIC_BAKIYE;
            if (Senaryo.Scripted.ScriptedYuklemePaneli.BorcAlindi)
                toplamYatirim += BORC_MIKTARI;
            // Kayıp formülü otomatik: Yatırım-Bakiye = 100K-4K = 96K (BorcAlındı yolunda).
            int toplamKayip = toplamYatirim - sonBakiye;
            // SenaryoYoneticisi anlatici sahnesinde devre dışı (Asama7_Finale forced) → toplamSpin 0 dönüyordu.
            // Doğru kaynak AnlaticiSeritKopru.ToplamSpin (her SpinTamamlandi'da artırılır).
            int toplamSpin = AnlaticiSeritKopru.Ornek != null ? AnlaticiSeritKopru.Ornek.ToplamSpin : 0;

            if (_istatistikText != null)
            {
                _istatistikText.text =
                    $"Yatırdığın toplam: <color=#FB923C>{OyunFormatServisi.FormatTL(toplamYatirim)}</color>\n" +
                    $"Geri aldığın: <color=#4ADE80>{OyunFormatServisi.FormatTL(sonBakiye)}</color>\n" +
                    $"Net kayıp: <color=#EF4444><b>{OyunFormatServisi.FormatTL(toplamKayip)}</b></color>\n" +
                    $"Toplam spin: {toplamSpin}";
            }
            _root.SetActive(true);
            IsAcik = true;
            Debug.Log($"[ScriptedFinalEkrani] A7 cutscene açıldı | Yatırım={toplamYatirim} | Bakiye={sonBakiye} | Kayıp={toplamKayip} | Spin={toplamSpin}");
        }

        private void OnYenidenBaslaTiklandi()
        {
            Debug.Log("[ScriptedFinalEkrani] TAMAM tıklandı — panel kapatılıyor (sahne reload yok).");
            IsAcik = false;
            if (_root != null) _root.SetActive(false);
            // Anlatıcı iframe'ini geri aç (final açılırken Gizle çağrılmıştı, GosterFinalEkrani:101).
            AnlaticiSeritKopru.Ornek?.Goster();
            // Defansif: BorcAlindi statik flag temizle (kullanıcı yeni oyun başlatmazsa etkisi yok ama tutarlılık).
            ScriptedYuklemePaneli.BorcAlindiSifirla();
            // Save zaten GosterFinalEkrani açılışında silindi (line 97). Sahne reload YOK → kullanıcı
            // arka plandaki oyun ekranını görür, isterse pencereyi kapatabilir veya tarayıcıyı yenileyerek yeni oyun başlar.
        }

        // === UI referansları ===
        private TextMeshProUGUI _istatistikText;

        private void UIYarat()
        {
            _root = new GameObject("ScriptedFinalEkraniCanvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _root.transform.SetParent(transform, false);
            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1800; // En üst (modal/yukleme/bonus üstünde)
            var scaler = _root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Tam ekran karartma
            var bg = new GameObject("Karartma", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bg.transform.SetParent(_root.transform, false);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
            bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.92f);

            // Kutu
            var kutu = new GameObject("FinalKutu", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            kutu.transform.SetParent(_root.transform, false);
            var kutuRt = kutu.GetComponent<RectTransform>();
            kutuRt.anchorMin = kutuRt.anchorMax = kutuRt.pivot = new Vector2(0.5f, 0.5f);
            // FAZ35.134: Kutu yüksekliği 780→850. Sebep: Faz 35.133'te bloklar 25px sıkı boşlukla yerleşmişti
            // (Aile↔Mesaj görsel olarak yapışık). +70px ek alan boşluklara dağıtıldı: 30/30/40/45 (alt bloklarda
            // daha fazla nefes — Mesaj 6-7 satır + Yeşilay zaten yoğun, ekstra 40-45px nefes rahatlatıyor).
            kutuRt.sizeDelta = new Vector2(820f, 850f);
            kutuRt.anchoredPosition = Vector2.zero;
            kutu.GetComponent<Image>().color = new Color(0.05f, 0.07f, 0.12f, 0.99f);
            BorderEkle(kutu.transform, kutuRt.sizeDelta, 3f, new Color(0.85f, 0.18f, 0.18f, 1f));

            // Başlık: "Oyun bitti"
            var basGo = new GameObject("Baslik", typeof(RectTransform), typeof(CanvasRenderer));
            basGo.transform.SetParent(kutu.transform, false);
            var basRt = basGo.GetComponent<RectTransform>();
            basRt.anchorMin = new Vector2(0f, 1f); basRt.anchorMax = new Vector2(1f, 1f);
            basRt.pivot = new Vector2(0.5f, 1f);
            basRt.sizeDelta = new Vector2(0f, 70f);
            basRt.anchoredPosition = new Vector2(0f, -25f);
            var basTxt = basGo.AddComponent<TextMeshProUGUI>();
            basTxt.alignment = TextAlignmentOptions.Center;
            basTxt.fontSize = 44f;
            basTxt.fontStyle = FontStyles.Bold;
            basTxt.color = new Color(1f, 0.45f, 0.45f, 1f);
            basTxt.richText = true;
            basTxt.text = "<color=#EF4444><b>SENARYO TAMAMLANDI</b></color>";
            basTxt.raycastTarget = false;

            // Istatistik metni — FAZ35.134: Y aralığı +140..+325 (kutu 850). Center alignment (X+Y orta) korundu.
            // Baslik bot ~+355 ile 30px boşluk, Istatistik bot +140 ile AileYazisi top +120 arası 20px (Aile mid+40
            // anchoredPos, height 140 → top +110, alt boşluk düzeltildi → AileYazisi'nin top'u +110, 30px boşluk).
            var istGo = new GameObject("Istatistik", typeof(RectTransform), typeof(CanvasRenderer));
            istGo.transform.SetParent(kutu.transform, false);
            var istRt = istGo.GetComponent<RectTransform>();
            istRt.anchorMin = new Vector2(0f, 0.5f); istRt.anchorMax = new Vector2(1f, 1f);
            istRt.offsetMin = new Vector2(40f, 140f); istRt.offsetMax = new Vector2(-40f, -100f);
            _istatistikText = istGo.AddComponent<TextMeshProUGUI>();
            _istatistikText.alignment = TextAlignmentOptions.Center;
            _istatistikText.fontSize = 24f;
            _istatistikText.color = new Color(0.95f, 0.97f, 1f, 1f);
            _istatistikText.enableWordWrapping = true;
            _istatistikText.text = "";
            _istatistikText.raycastTarget = false;

            // AİLE YAZISI (vurgulu, dikkat çekici) — istatistikler ile pedagojik metin arası.
            // Net kayıp rakamının somut karşılığını oyuncuya hissettirir.
            // FAZ35.134: anchoredPosition Y=30→40, sizeDelta height 130→140 (kutu 850, daha rahat nefes).
            // Yeni Y aralığı: mid+40 merkez, height 140 → Y -30..+110. Istatistik bot +140 ile 30px boşluk,
            // Mesaj top -70 ile 40px boşluk (alt blokta daha fazla nefes).
            var aileGo = new GameObject("AileYazisi", typeof(RectTransform), typeof(CanvasRenderer));
            aileGo.transform.SetParent(kutu.transform, false);
            var aileRt = aileGo.GetComponent<RectTransform>();
            aileRt.anchorMin = new Vector2(0f, 0.5f); aileRt.anchorMax = new Vector2(1f, 0.5f);
            aileRt.pivot = new Vector2(0.5f, 0.5f);
            aileRt.sizeDelta = new Vector2(0f, 140f);
            aileRt.anchoredPosition = new Vector2(0f, 40f);
            var aileTxt = aileGo.AddComponent<TextMeshProUGUI>();
            aileTxt.alignment = TextAlignmentOptions.Center;
            aileTxt.fontSize = 30f;
            aileTxt.fontStyle = FontStyles.Bold;
            aileTxt.color = new Color(1f, 0.65f, 0.20f, 1f); // sıcak turuncu — dramatik vurgu
            aileTxt.enableWordWrapping = true;
            aileTxt.lineSpacing = 6f;
            aileTxt.text =
                "Bu rakam ortalama bir aile için <color=#EF4444>2,5 aylık geçim</color> demek.\n" +
                "<size=24><i>Gerçek hayatta oyuncu burada durmaz; bir sonraki maaş, bir sonraki <color=#EF4444>kredi</color>, bir sonraki dönüş umuduyla devam eder.</i></size>";
            aileTxt.raycastTarget = false;

            // Mesaj (alt) — pedagojik metin + Yeşilay. FAZ35.134: offsetMin Y=110→130, offsetMax Y=-60→-70.
            // Yeni Y aralığı kutu 850'de: -295..-70 (225px). AileYazisi bot -30 ile 40px boşluk (yapışık değil),
            // Buton top -340 ile 45px boşluk (alt blokta en geniş nefes — yoğun Mesaj+Yeşilay sonrası rahatlama).
            var mesGo = new GameObject("Mesaj", typeof(RectTransform), typeof(CanvasRenderer));
            mesGo.transform.SetParent(kutu.transform, false);
            var mesRt = mesGo.GetComponent<RectTransform>();
            mesRt.anchorMin = new Vector2(0f, 0f); mesRt.anchorMax = new Vector2(1f, 0.5f);
            mesRt.offsetMin = new Vector2(40f, 130f); mesRt.offsetMax = new Vector2(-40f, -70f);
            var mesTxt = mesGo.AddComponent<TextMeshProUGUI>();
            mesTxt.alignment = TextAlignmentOptions.Center;
            // FAZ35.12: degisiklikler.md #30 — "Unutulmamalıdır ki…" pedagojik metin puntosu +%33 büyütüldü (18→24).
            mesTxt.fontSize = 24f;
            // fontStyle Italic → Normal: üst 2 satırı <i>...</i> tag'leriyle italik tut, yeni cesaret
            // çağrısı paragrafı normal stilde kalsın (renk + bold tag'leri zaten yeterli vurgu).
            mesTxt.fontStyle = FontStyles.Normal;
            mesTxt.richText = true;
            mesTxt.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            mesTxt.enableWordWrapping = true;
            // FAZ35.133: İlk 2 satır ("Türkiye'de binlerce kişi" + "Online kumar bağımlılığı hastalıktır...
            // farkındalıktır") SİLİNDİ. Metin "Unutulmamalıdır ki..." ile doğrudan başlıyor.
            mesTxt.text =
                "Unutulmamalıdır ki <color=#16a34a><b>sanal kumar bağımlılığı çözümsüz değildir</b></color> ve her zaman yeni bir başlangıç yapmak mümkündür. Yaşanan zorluklar ne kadar büyük görünürse görünsün, <color=#16a34a><b>umut her zaman vardır</b></color> ve doğru destekle bu süreç aşılabilir. Bu noktada <color=#ea580c>ailenize, amirlerinize ve güvendiğiniz kişilere</color> durumu açıkça ifade etmek, çözüm yolunda atılacak <color=#16a34a><b>cesur bir adımdır</b></color>. <color=#dc2626><b>Yardım istemek bir zayıflık değil</b></color>, aksine güçlü bir farkındalık ve değişim isteğinin göstergesidir.\n\n" +
                $"<color=#16a34a><b>Yeşilay Yardım Hattı: {YESILAY_HATTI}</b></color>";
            mesTxt.raycastTarget = false;

            // Yeniden başla butonu
            var btnGo = new GameObject("YenidenBaslaButon",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(kutu.transform, false);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = btnRt.anchorMax = new Vector2(0.5f, 0f);
            btnRt.pivot = new Vector2(0.5f, 0f);
            btnRt.sizeDelta = new Vector2(320f, 70f);
            // FAZ35.133: anchoredPosition Y=28→15 (kutu 780'e küçüldü, buton üst sınır Y=-305, Mesaj bot -280 ile 25px boşluk).
            btnRt.anchoredPosition = new Vector2(0f, 15f);
            btnGo.GetComponent<Image>().color = new Color(0.85f, 0.18f, 0.18f, 1f);
            var btn = btnGo.GetComponent<Button>();
            btn.onClick.AddListener(OnYenidenBaslaTiklandi);

            var btnTxtGo = new GameObject("BtnTxt", typeof(RectTransform), typeof(CanvasRenderer));
            btnTxtGo.transform.SetParent(btnGo.transform, false);
            var btnTxtRt = btnTxtGo.GetComponent<RectTransform>();
            btnTxtRt.anchorMin = Vector2.zero; btnTxtRt.anchorMax = Vector2.one;
            btnTxtRt.offsetMin = btnTxtRt.offsetMax = Vector2.zero;
            var btnTxt = btnTxtGo.AddComponent<TextMeshProUGUI>();
            btnTxt.alignment = TextAlignmentOptions.Center;
            btnTxt.fontSize = 24f;
            btnTxt.fontStyle = FontStyles.Bold;
            btnTxt.color = Color.white;
            btnTxt.text = "TAMAM";
            btnTxt.raycastTarget = false;
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
