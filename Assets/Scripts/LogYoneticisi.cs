using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LogYoneticisi : MonoBehaviour
{
    public static string WebGlSonDisaAktarDurumu { get; private set; } = "hazir";
    [Header("Başlık ve Butonlar")]
    public TMP_Text kullaniciBilgiText;
    public Button replayButton;
    public Button geriDonButon;
    [Tooltip("Atanırsa tıklanınca senaryo logu TXT olarak dışa aktarılır.")]
    public Button disariAktarButon;
    [Tooltip("Atanırsa tıklanınca ekran yenilenir (VerileriYukle).")]
    public Button yenileButon;
    [Tooltip("Dışa aktarma sonrası kısa süre 'Log kaydedildi' mesajı gösterilir; boş bırakılabilir.")]
    public TMP_Text disAktarBilgiText;

    [Header("Bölüm 1 - Genel Özet (GenelOzetPanel içindeki metin)")]
    public TMP_Text genelOzetText;

    [Header("Bölüm 2 - Senaryo Logu (SenaryoLoguPanel içindeki metin)")]
    [Tooltip("SenaryoLoguPanel'i duplicate edip adını verdiysen; içindeki TMP_Text'in adı SenaryoLoguText olsun, script bulur.")]
    public TMP_Text senaryoLoguText;

    [Header("Eski scroll (kullanılmıyor; Scroll_SenaryoLog silindi)")]
    public Transform logContent;
    public GameObject logSatirPrefab;

    [Header("Filtre")]
    [Tooltip("Açıksa sadece aşama geçişi, bonus giriş/çıkış, bakiye yükleme/para çekme gibi anlamlı olaylar listelenir; spin başladı/bitti gibi yoğun kayıtlar gizlenir.")]
    public bool sadeceAnlamliOlaylar = true;

    [Header("Eski İstatistik UI (isteğe bağlı)")]
    public TMP_Text toplamSpinText;
    public TMP_Text toplamKazancText;
    public TMP_Text toplamKayipText;
    public TMP_Text bonusGirisSayisiText;
    public TMP_Text bonusSatinAlmaText;
    public TMP_Text netBakiyeText;
    public TMP_Text toplamYatirilanText;
    public TMP_Text toplamCekilenText;

    private PlayerProfile _profile;
    private TMP_Text _kartNetSonucDegerText;
    private TMP_Text _kartSpinDegerText;
    private TMP_Text _kartYatirilanDegerText;
    private TMP_Text _kartCekilenDegerText;
    private TMP_Text _kartBonusDegerText;
    private TMP_Text _kartRtpDegerText;
    private TMP_Text _kartToplamKazancDegerText;
    private TMP_Text _kartToplamKayipDegerText;
    private TMP_Text _kartBakiyeDegerText;
    private TMP_Text _kartBonusSatinAlmaDegerText;
    private TMP_Text _kartOturumSureDegerText;
    private TMP_Text _kartOrtalamaSpinSureDegerText;
    private TMP_Text _kartNetUyariDegerText;
    private TMP_Text _kartAsamaDegerText;
    private RectTransform _senaryoKartIcerikRt;

    void Start()
    {
        ReferanslariBul();
        ModernOzetKartlariniKur();
        GiriseDonButonunuBagla();
        GirisDonOverlayCanvasOlustur(); // Her zaman tıklanabilir: en üstte ayrı Canvas + buton
        ScrollViewPanelleriKur();
        LogEkraniLayoutAyarla();
        if (replayButton)
            replayButton.onClick.AddListener(BonusReplayGoster);
        if (disariAktarButon)
            disariAktarButon.onClick.AddListener(DisariAktarVeBilgiGoster);
        if (yenileButon)
            yenileButon.onClick.AddListener(Yenile);
        VerileriYukle();
    }

    void ModernOzetKartlariniKur()
    {
        if (genelOzetText == null) return;
        if (_kartNetSonucDegerText != null) return;

        RectTransform genelPanel = genelOzetText.transform.parent as RectTransform;
        if (genelPanel == null) return;

        GameObject kapsayici = new GameObject("ModernOzetKapsayici");
        kapsayici.transform.SetParent(genelPanel, false);
        var kapsayiciRt = kapsayici.AddComponent<RectTransform>();
        kapsayiciRt.anchorMin = new Vector2(0f, 1f);
        kapsayiciRt.anchorMax = new Vector2(1f, 1f);
        kapsayiciRt.pivot = new Vector2(0.5f, 1f);
        kapsayiciRt.offsetMin = new Vector2(12f, -188f);
        kapsayiciRt.offsetMax = new Vector2(-12f, -12f);

        var dikey = kapsayici.AddComponent<VerticalLayoutGroup>();
        dikey.childControlWidth = true;
        dikey.childControlHeight = true;
        dikey.childForceExpandWidth = true;
        dikey.childForceExpandHeight = false;
        dikey.spacing = 10f;
        dikey.padding = new RectOffset(0, 0, 0, 0);

        var baslik = OzetBaslikSatiriOlustur(kapsayici.transform, "OTURUM ÖZETİ", 30f);
        baslik.alignment = TextAlignmentOptions.Left;

        GameObject satir1 = KartSatiriOlustur(kapsayici.transform, "KartSatiriUst");
        GameObject satir2 = KartSatiriOlustur(kapsayici.transform, "KartSatiriAlt");

        _kartNetSonucDegerText = OzetKartOlustur(satir1.transform, "NET SONUÇ");
        _kartSpinDegerText = OzetKartOlustur(satir1.transform, "SPIN");
        _kartYatirilanDegerText = OzetKartOlustur(satir1.transform, "YATIRILAN");
        _kartCekilenDegerText = OzetKartOlustur(satir2.transform, "ÇEKİLEN");
        _kartBonusDegerText = OzetKartOlustur(satir2.transform, "BONUS");
        _kartRtpDegerText = OzetKartOlustur(satir2.transform, "RTP");

        GameObject satir3 = KartSatiriOlustur(kapsayici.transform, "KartSatiriDetay1");
        GameObject satir4 = KartSatiriOlustur(kapsayici.transform, "KartSatiriDetay2");
        GameObject satir5 = KartSatiriOlustur(kapsayici.transform, "KartSatiriDetay3");

        _kartToplamKazancDegerText = OzetKartOlustur(satir3.transform, "TOP.KAZANÇ");
        _kartToplamKayipDegerText = OzetKartOlustur(satir3.transform, "TOP.KAYIP");
        _kartBakiyeDegerText = OzetKartOlustur(satir3.transform, "BAKİYE");

        _kartBonusSatinAlmaDegerText = OzetKartOlustur(satir4.transform, "BONUS ALIM");
        _kartOturumSureDegerText = OzetKartOlustur(satir4.transform, "OTURUM SÜRE");
        _kartOrtalamaSpinSureDegerText = OzetKartOlustur(satir4.transform, "ORT.SPIN");

        _kartNetUyariDegerText = OzetKartOlustur(satir5.transform, "NET DURUM");
        _kartAsamaDegerText = OzetKartOlustur(satir5.transform, "AŞAMA");
        OzetKartOlustur(satir5.transform, "DURUM");

        // Eski uzun özet metni kartların arkasında görünmesin.
        genelOzetText.gameObject.SetActive(false);
    }

    static TMP_Text OzetBaslikSatiriOlustur(Transform ebeveyn, string metin, float fontBoyutu)
    {
        GameObject go = new GameObject("OzetBaslik");
        go.transform.SetParent(ebeveyn, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 44f);
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 44f;
        le.preferredHeight = 44f;
        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.text = metin;
        txt.fontSize = fontBoyutu;
        txt.fontStyle = FontStyles.Bold;
        txt.color = new Color(0.94f, 0.96f, 1f, 1f);
        txt.raycastTarget = false;
        return txt;
    }

    static GameObject KartSatiriOlustur(Transform ebeveyn, string ad)
    {
        GameObject satir = new GameObject(ad);
        satir.transform.SetParent(ebeveyn, false);
        var rt = satir.AddComponent<RectTransform>();
        const float satirYukseklik = 92f;
        rt.sizeDelta = new Vector2(0f, satirYukseklik);
        var le = satir.AddComponent<LayoutElement>();
        le.minHeight = satirYukseklik;
        le.preferredHeight = satirYukseklik;
        var yatay = satir.AddComponent<HorizontalLayoutGroup>();
        yatay.spacing = 10f;
        yatay.padding = new RectOffset(0, 0, 0, 0);
        yatay.childControlWidth = true;
        yatay.childControlHeight = true;
        yatay.childForceExpandWidth = true;
        yatay.childForceExpandHeight = false;
        return satir;
    }

    static TMP_Text OzetKartOlustur(Transform ebeveyn, string etiket)
    {
        GameObject kart = new GameObject("Kart_" + etiket.Replace(" ", ""));
        kart.transform.SetParent(ebeveyn, false);
        kart.AddComponent<CanvasRenderer>();
        var arka = kart.AddComponent<Image>();
        arka.color = new Color(0.09f, 0.13f, 0.20f, 0.88f);
        var rt = kart.GetComponent<RectTransform>();
        const float kartYukseklik = 88f;
        rt.sizeDelta = new Vector2(0f, kartYukseklik);

        var kartLe = kart.AddComponent<LayoutElement>();
        kartLe.minHeight = kartYukseklik;
        kartLe.preferredHeight = kartYukseklik;
        kartLe.flexibleWidth = 1f;

        var layout = kart.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        GameObject etiketGo = new GameObject("Etiket");
        etiketGo.transform.SetParent(kart.transform, false);
        var etiketText = etiketGo.AddComponent<TextMeshProUGUI>();
        etiketText.text = etiket;
        etiketText.fontSize = 14;
        etiketText.fontStyle = FontStyles.Bold;
        etiketText.color = new Color(0.70f, 0.78f, 0.90f, 1f);
        etiketText.raycastTarget = false;
        etiketText.enableWordWrapping = true;
        etiketText.alignment = TextAlignmentOptions.Top;
        var etiketLe = etiketGo.AddComponent<LayoutElement>();
        etiketLe.minHeight = 18f;
        etiketLe.preferredHeight = 22f;
        etiketLe.flexibleWidth = 1f;

        GameObject degerGo = new GameObject("Deger");
        degerGo.transform.SetParent(kart.transform, false);
        var degerText = degerGo.AddComponent<TextMeshProUGUI>();
        degerText.text = "—";
        degerText.fontSize = 20;
        degerText.fontStyle = FontStyles.Bold;
        degerText.color = Color.white;
        degerText.raycastTarget = false;
        degerText.enableWordWrapping = true;
        degerText.overflowMode = TextOverflowModes.Ellipsis;
        degerText.alignment = TextAlignmentOptions.Top;
        var degerLe = degerGo.AddComponent<LayoutElement>();
        degerLe.minHeight = 26f;
        degerLe.preferredHeight = 32f;
        degerLe.flexibleWidth = 1f;
        return degerText;
    }

    void SenaryoKartGorunumunuKur()
    {
        if (senaryoLoguText == null) return;
        if (_senaryoKartIcerikRt != null) return;

        var scroll = senaryoLoguText.GetComponentInParent<ScrollRect>();
        if (scroll == null || scroll.content == null) return;

        senaryoLoguText.gameObject.SetActive(false);

        var mevcut = scroll.content.Find("SenaryoKartIcerik");
        if (mevcut != null)
        {
            _senaryoKartIcerikRt = mevcut as RectTransform;
            return;
        }

        GameObject icerik = new GameObject("SenaryoKartIcerik");
        icerik.transform.SetParent(scroll.content, false);
        _senaryoKartIcerikRt = icerik.AddComponent<RectTransform>();
        _senaryoKartIcerikRt.anchorMin = new Vector2(0f, 1f);
        _senaryoKartIcerikRt.anchorMax = new Vector2(1f, 1f);
        _senaryoKartIcerikRt.pivot = new Vector2(0.5f, 1f);
        _senaryoKartIcerikRt.offsetMin = new Vector2(0f, 0f);
        _senaryoKartIcerikRt.offsetMax = new Vector2(0f, 0f);

        var vlg = icerik.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.spacing = 8f;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var csf = icerik.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    /// <summary>Genel özet ve senaryo logu metinleri ScrollView içinde değilse runtime'da ScrollView oluşturur.
    /// Sahneye eklediğiniz ScrollView varsa kod onu kullanır, ayarlarını yapar, tekrar oluşturmaz.</summary>
    void ScrollViewPanelleriKur()
    {
        if (genelOzetText != null && genelOzetText.GetComponentInParent<ScrollRect>() == null)
            ScrollViewIcerikEkle(genelOzetText.transform.parent, genelOzetText.transform, "GenelOzetScroll");
        if (senaryoLoguText != null && senaryoLoguText.GetComponentInParent<ScrollRect>() == null)
            ScrollViewIcerikEkle(senaryoLoguText.transform.parent, senaryoLoguText.transform, "SenaryoLogScroll");
        // Unity'de eklediğiniz Scroll View'ları log ekranına uygun ayarla
        ScrollViewAyarlariniUygula(genelOzetText);
        ScrollViewAyarlariniUygula(senaryoLoguText);
    }

    /// <summary>Sahnedeki Scroll View'ı log ekranı için ayarlar: sadece dikey kaydırma, Content Size Fitter, metin anchor.</summary>
    static void ScrollViewAyarlariniUygula(TMP_Text metin)
    {
        if (metin == null) return;
        var scroll = metin.GetComponentInParent<ScrollRect>();
        if (scroll == null) return;

        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.scrollSensitivity = 20f;

        // Scroll View kökü paneli doldursun
        var scrollRt = scroll.transform as RectTransform;
        if (scrollRt != null)
        {
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = Vector2.zero;
        }
        // Viewport Scroll View'ı doldursun
        if (scroll.viewport != null)
        {
            var vpRt = scroll.viewport as RectTransform;
            if (vpRt != null)
            {
                vpRt.anchorMin = Vector2.zero;
                vpRt.anchorMax = Vector2.one;
                vpRt.offsetMin = Vector2.zero;
                vpRt.offsetMax = Vector2.zero;
            }
        }

        if (scroll.content != null)
        {
            var contentRt = scroll.content;
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = new Vector2(8f, 8f);
            contentRt.offsetMax = new Vector2(-8f, -8f);
            var csf = contentRt.GetComponent<ContentSizeFitter>();
            if (csf == null) csf = contentRt.gameObject.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        var textRt = metin.transform as RectTransform;
        if (textRt != null)
        {
            textRt.anchorMin = new Vector2(0f, 1f);
            textRt.anchorMax = new Vector2(1f, 1f);
            textRt.pivot = new Vector2(0.5f, 1f);
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
        }
        var textCsf = metin.GetComponent<ContentSizeFitter>();
        if (textCsf == null) textCsf = metin.gameObject.AddComponent<ContentSizeFitter>();
        textCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        textCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    /// <summary>Panel ile metin arasına ScrollRect → Viewport → Content ekler; metni Content altına taşır.</summary>
    static void ScrollViewIcerikEkle(Transform panel, Transform metinTransform, string scrollAdi)
    {
        if (panel == null || metinTransform == null) return;
        var panelRt = panel as RectTransform;
        if (panelRt == null) return;

        var scrollGo = new GameObject(scrollAdi);
        scrollGo.transform.SetParent(panel, false);
        var scrollRt = scrollGo.AddComponent<RectTransform>();
        scrollRt.anchorMin = Vector2.zero;
        scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = Vector2.zero;
        scrollRt.offsetMax = Vector2.zero;

        var scrollRect = scrollGo.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 20f;

        var viewportGo = new GameObject("Viewport");
        viewportGo.transform.SetParent(scrollGo.transform, false);
        var viewportRt = viewportGo.AddComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = Vector2.zero;
        viewportRt.offsetMax = Vector2.zero;
        viewportGo.AddComponent<RectMask2D>();

        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(viewportGo.transform, false);
        var contentRt = contentGo.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.offsetMin = new Vector2(8f, 8f);
        contentRt.offsetMax = new Vector2(-8f, -8f);
        contentRt.sizeDelta = new Vector2(0f, 0f);

        var csf = contentGo.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        metinTransform.SetParent(contentRt, false);
        var textRt = metinTransform as RectTransform;
        if (textRt != null)
        {
            textRt.anchorMin = new Vector2(0f, 1f);
            textRt.anchorMax = new Vector2(1f, 1f);
            textRt.pivot = new Vector2(0.5f, 1f);
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
        }
        var textCsf = metinTransform.gameObject.GetComponent<ContentSizeFitter>();
        if (textCsf == null) textCsf = metinTransform.gameObject.AddComponent<ContentSizeFitter>();
        textCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        textCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRt;
        scrollRect.viewport = viewportRt;

        UnityEngine.UI.Scrollbar scrollbar = DikeyScrollbarOlustur(scrollGo.transform, scrollRt);
        if (scrollbar != null)
            scrollRect.verticalScrollbar = scrollbar;
    }

    /// <summary>ScrollRect için sağ tarafta dikey scrollbar oluşturur; kullanıcı kaydırma çubuğunu görebilir ve sürükleyebilir.</summary>
    static UnityEngine.UI.Scrollbar DikeyScrollbarOlustur(Transform scrollParent, RectTransform scrollRt)
    {
        float genislik = 14f;
        var barGo = new GameObject("Scrollbar Vertical");
        barGo.transform.SetParent(scrollParent, false);
        var barRt = barGo.AddComponent<RectTransform>();
        barRt.anchorMin = new Vector2(1f, 0f);
        barRt.anchorMax = new Vector2(1f, 1f);
        barRt.pivot = new Vector2(1f, 1f);
        barRt.offsetMin = new Vector2(-genislik - 2f, 2f);
        barRt.offsetMax = new Vector2(-2f, -2f);

        var barImg = barGo.AddComponent<UnityEngine.UI.Image>();
        barImg.color = new Color(0.2f, 0.2f, 0.2f, 0.6f);

        var slideAreaGo = new GameObject("Sliding Area");
        slideAreaGo.transform.SetParent(barGo.transform, false);
        var slideAreaRt = slideAreaGo.AddComponent<RectTransform>();
        slideAreaRt.anchorMin = Vector2.zero;
        slideAreaRt.anchorMax = Vector2.one;
        slideAreaRt.offsetMin = new Vector2(2f, 2f);
        slideAreaRt.offsetMax = new Vector2(-2f, -2f);

        var handleGo = new GameObject("Handle");
        handleGo.transform.SetParent(slideAreaGo.transform, false);
        var handleRt = handleGo.AddComponent<RectTransform>();
        handleRt.anchorMin = new Vector2(0f, 0f);
        handleRt.anchorMax = new Vector2(1f, 0.2f);
        handleRt.offsetMin = Vector2.zero;
        handleRt.offsetMax = Vector2.zero;
        var handleImg = handleGo.AddComponent<UnityEngine.UI.Image>();
        handleImg.color = new Color(0.5f, 0.5f, 0.5f, 0.9f);

        var scrollbar = barGo.AddComponent<UnityEngine.UI.Scrollbar>();
        scrollbar.direction = UnityEngine.UI.Scrollbar.Direction.BottomToTop;
        scrollbar.handleRect = handleRt;
        scrollbar.targetGraphic = handleImg;
        scrollbar.value = 1f;
        return scrollbar;
    }

    /// <summary>İki panelli log: GenelOzetPanel (sol), SenaryoLoguPanel (sağ) RectTransform ayarı. Scroll View varsa paneli ve Scroll View'ı doğru boyutlandırır.</summary>
    void LogEkraniLayoutAyarla()
    {
        // Sol panel: GenelOzetPanel (veya içindeki Scroll View)
        if (genelOzetText != null)
        {
            var scroll = genelOzetText.GetComponentInParent<ScrollRect>();
            Transform panel = scroll != null ? scroll.transform.parent : genelOzetText.transform.parent;
            if (panel != null)
            {
                var panelRt = panel as RectTransform;
                if (panelRt != null)
                {
                    panelRt.anchorMin = new Vector2(0f, 0f);
                    panelRt.anchorMax = new Vector2(0.5f, 1f);
                    panelRt.offsetMin = new Vector2(10f, 10f);
                    panelRt.offsetMax = new Vector2(-5f, -80f);
                }
                if (scroll == null)
                {
                    var textRt = genelOzetText.transform as RectTransform;
                    if (textRt != null)
                    {
                        textRt.anchorMin = Vector2.zero;
                        textRt.anchorMax = Vector2.one;
                        textRt.offsetMin = new Vector2(8f, 8f);
                        textRt.offsetMax = new Vector2(-8f, -8f);
                    }
                }
            }
        }
        // Sağ panel: SenaryoLoguPanel (veya içindeki Scroll View)
        if (senaryoLoguText != null)
        {
            var scroll = senaryoLoguText.GetComponentInParent<ScrollRect>();
            Transform panel = scroll != null ? scroll.transform.parent : senaryoLoguText.transform.parent;
            if (panel != null)
            {
                var panelRt = panel as RectTransform;
                if (panelRt != null)
                {
                    panelRt.anchorMin = new Vector2(0.5f, 0f);
                    panelRt.anchorMax = new Vector2(1f, 1f);
                    panelRt.offsetMin = new Vector2(5f, 10f);
                    panelRt.offsetMax = new Vector2(-10f, -80f);
                }
                if (scroll == null)
                {
                    var textRt = senaryoLoguText.transform as RectTransform;
                    if (textRt != null)
                    {
                        textRt.anchorMin = Vector2.zero;
                        textRt.anchorMax = Vector2.one;
                        textRt.offsetMin = new Vector2(8f, 8f);
                        textRt.offsetMax = new Vector2(-8f, -8f);
                    }
                }
            }
        }
    }

    void ReferanslariBul()
    {
        if (genelOzetText == null)
        {
            var go = GameObject.Find("GenelOzetText");
            if (go != null) genelOzetText = go.GetComponent<TMP_Text>();
        }
        if (senaryoLoguText == null)
        {
            var go = GameObject.Find("SenaryoLoguText");
            if (go != null) senaryoLoguText = go.GetComponent<TMP_Text>();
            if (senaryoLoguText == null)
            {
                var panel = GameObject.Find("SenaryoLoguPanel");
                if (panel != null) senaryoLoguText = panel.GetComponentInChildren<TMP_Text>();
            }
        }
        if (logContent == null)
        {
            var scroll = GameObject.Find("Scroll_LogRapor");
            if (scroll != null)
            {
                var viewport = scroll.transform.Find("Viewport");
                if (viewport != null)
                {
                    var content = viewport.Find("Content");
                    if (content != null) logContent = content;
                }
            }
            if (logContent == null)
            {
                var content = GameObject.Find("Content");
                if (content != null) logContent = content.transform;
            }
        }
        if (geriDonButon == null)
        {
            var go = GameObject.Find("Buton_GirisEkrani");
            if (go == null) go = GameObject.Find("Buton_GirisEkrani "); // olası sondaki boşluk
            if (go == null) go = GameObject.Find("GirisDonBtn");
            if (go != null) geriDonButon = go.GetComponent<Button>();
            if (geriDonButon == null)
            {
                var butonlar = FindObjectsOfType<Button>(true);
                foreach (var b in butonlar)
                {
                    var tmp = b.GetComponentInChildren<TMP_Text>(true);
                    if (tmp != null && (tmp.text.Contains("GİRİŞE DÖN") || tmp.text.Contains("GirisEkrani")))
                    {
                        geriDonButon = b;
                        break;
                    }
                }
            }
        }
    }

    void VerileriYukle()
    {
        if (GameManager.I == null || GameManager.I.ActivePlayer == null)
        {
            Debug.LogWarning("Aktif oyuncu yok!");
            return;
        }

        _profile = GameManager.I.ActivePlayer;

        // Kullanıcı bilgisi: giriş yapan kullanıcıya ait, bu oturum istatistikleri
        if (kullaniciBilgiText)
        {
            kullaniciBilgiText.text = $"{_profile.playerName} – Bu oturum istatistikleri";
            kullaniciBilgiText.fontSize = 24;
        }

        // İstatistikler: bu oturum (giriş yapan kullanıcı bazlı). Para/bakiye için senaryo oturum logu varsa oradan al (daha doğru).
        int oturumSpin = GameManager.I != null ? GameManager.I.OturumSpinSayisi : 0;
        int oturumYatirilan = GameManager.I != null ? GameManager.I.OturumYatirilan : 0;
        int oturumCekilen = GameManager.I != null ? GameManager.I.OturumCekilen : 0;
        int oturumNet = GameManager.I != null ? GameManager.I.OturumNet : 0;
        int oturumBonus = GameManager.I != null ? GameManager.I.OturumBonusGiris : 0;
        int gosterimBakiye = _profile != null ? _profile.balance : 0;
        var sessionLog = SenaryoYoneticisi.GetOturumLoguStatik(_profile?.playerId);
        if (sessionLog != null && sessionLog.Count > 0)
        {
            var yuklemeLoglari = sessionLog.Where(x => x.olayTipi == SenaryoOlayKaydi.OlayTipi_BakiyeYuklemeYapildi).ToList();
            var paraCekLoglari = sessionLog.Where(x => x.olayTipi == SenaryoOlayKaydi.OlayTipi_ParaCekildi).ToList();
            if (yuklemeLoglari.Count > 0) oturumYatirilan = TahminiToplamTutar(yuklemeLoglari);
            if (paraCekLoglari.Count > 0) oturumCekilen = TahminiToplamTutar(paraCekLoglari);
            gosterimBakiye = sessionLog.Last().bakiye;
        }
        float rtpYuzde = 0f;
        if (_profile != null && _profile.totalWagered > 0)
            rtpYuzde = 100f * (float)_profile.totalWon / _profile.totalWagered;

        ModernKartDegerleriniGuncelle(oturumNet, oturumSpin, oturumYatirilan, oturumCekilen, oturumBonus, rtpYuzde);
        SenaryoKartGorunumunuKur();
        SenaryoOlayKartlariniDoldur();

        if (toplamSpinText)
            toplamSpinText.text = $"Bu oturum spin: {oturumSpin}";
        if (toplamKazancText)
            toplamKazancText.text = $"Toplam Kazanç: {_profile.totalWon:N0} TL";
        if (toplamKayipText)
            toplamKayipText.text = $"Toplam Kayıp: {_profile.totalLost:N0} TL";
        if (bonusGirisSayisiText)
            bonusGirisSayisiText.text = $"Bonus Giriş (oturum): {oturumBonus}";
        if (bonusSatinAlmaText)
            bonusSatinAlmaText.text = $"Bonus Satın Alma: {BonusSatinAlmaSayisi()}";
        if (netBakiyeText)
            netBakiyeText.text = $"Net (oturum): {oturumNet:N0} TL";
        if (toplamYatirilanText)
            toplamYatirilanText.text = $"Yatırılan (oturum): {oturumYatirilan:N0} TL";
        if (toplamCekilenText)
            toplamCekilenText.text = $"Çekilen (oturum): {oturumCekilen:N0} TL";

        // Bölüm 1: GenelOzetPanel = genel özet
        string ozet = BuildGenelOzetMetni();
        if (genelOzetText != null)
        {
            genelOzetText.text = ozet;
            genelOzetText.fontSize = 22;
            genelOzetText.enableWordWrapping = true;
            genelOzetText.alignment = TMPro.TextAlignmentOptions.TopLeft;
            genelOzetText.overflowMode = TMPro.TextOverflowModes.Overflow;
        }

        // Bölüm 2: SenaryoLoguPanel = senaryo logu (aşama aşama)
        string senaryoMetin = BuildSenaryoLogTekMetin();
        if (senaryoLoguText != null)
        {
            senaryoLoguText.text = senaryoMetin;
            senaryoLoguText.fontSize = 20;
            senaryoLoguText.enableWordWrapping = true;
            senaryoLoguText.alignment = TMPro.TextAlignmentOptions.TopLeft;
            senaryoLoguText.overflowMode = TMPro.TextOverflowModes.Overflow;
            SenaryoLogScrollIcerikGuncelle();
        }

        // ScrollView içerik yüksekliğinin güncellenmesi için layout'u bir sonraki karede zorla; böylece aşağı kaydırma çalışır.
        StartCoroutine(ScrollViewLayoutGecikmeliGuncelle());
    }

    void ModernKartDegerleriniGuncelle(int oturumNet, int oturumSpin, int oturumYatirilan, int oturumCekilen, int oturumBonus, float rtpYuzde)
    {
        if (_kartNetSonucDegerText == null) return;

        _kartNetSonucDegerText.text = $"{oturumNet:N0} TL";
        _kartNetSonucDegerText.color = oturumNet > 0
            ? new Color(0.34f, 0.97f, 0.58f, 1f)
            : (oturumNet < 0 ? new Color(1f, 0.41f, 0.41f, 1f) : Color.white);

        _kartSpinDegerText.text = $"{oturumSpin:N0}";
        _kartSpinDegerText.color = Color.white;

        _kartYatirilanDegerText.text = $"{oturumYatirilan:N0} TL";
        _kartYatirilanDegerText.color = new Color(1f, 0.82f, 0.41f, 1f);

        _kartCekilenDegerText.text = $"{oturumCekilen:N0} TL";
        _kartCekilenDegerText.color = new Color(0.53f, 0.86f, 1f, 1f);

        _kartBonusDegerText.text = $"{oturumBonus:N0}";
        _kartBonusDegerText.color = new Color(0.86f, 0.70f, 1f, 1f);

        _kartRtpDegerText.text = _profile != null && _profile.totalWagered > 0 ? $"%{rtpYuzde:F1}" : "—";
        _kartRtpDegerText.color = new Color(0.82f, 0.93f, 1f, 1f);

        if (_profile != null)
        {
            _kartToplamKazancDegerText.text = $"{_profile.totalWon:N0} TL";
            _kartToplamKazancDegerText.color = new Color(0.50f, 0.97f, 0.65f, 1f);

            _kartToplamKayipDegerText.text = $"{_profile.totalLost:N0} TL";
            _kartToplamKayipDegerText.color = new Color(1f, 0.57f, 0.57f, 1f);

            _kartBonusSatinAlmaDegerText.text = $"{BonusSatinAlmaSayisi():N0}";
            _kartBonusSatinAlmaDegerText.color = new Color(0.96f, 0.76f, 1f, 1f);
        }

        int gosterimBakiye = _profile != null ? _profile.balance : 0;
        var sessionLog = SenaryoYoneticisi.GetOturumLoguStatik(_profile != null ? _profile.playerId : null);
        if (sessionLog != null && sessionLog.Count > 0)
            gosterimBakiye = sessionLog.Last().bakiye;
        _kartBakiyeDegerText.text = $"{gosterimBakiye:N0} TL";
        _kartBakiyeDegerText.color = Color.white;

        float oturumSuresiDakika = 0f;
        string asamaAdi = "—";
        if (SenaryoYoneticisi.I != null)
        {
            oturumSuresiDakika = SenaryoYoneticisi.I.oyunSuresiDakika;
            asamaAdi = SenaryoYoneticisi.I.GetAsamaAdi();
        }

        _kartOturumSureDegerText.text = $"{oturumSuresiDakika:F1} dk";
        _kartOturumSureDegerText.color = new Color(0.88f, 0.95f, 1f, 1f);

        float ortalamaSpinSure = (oturumSpin > 0 && oturumSuresiDakika > 0f) ? (oturumSuresiDakika * 60f / oturumSpin) : 0f;
        _kartOrtalamaSpinSureDegerText.text = ortalamaSpinSure > 0f ? $"{ortalamaSpinSure:F1} sn" : "—";
        _kartOrtalamaSpinSureDegerText.color = new Color(0.88f, 0.95f, 1f, 1f);

        if (oturumNet < 0)
        {
            _kartNetUyariDegerText.text = $"KAYIP {Mathf.Abs(oturumNet):N0} TL";
            _kartNetUyariDegerText.color = new Color(1f, 0.42f, 0.42f, 1f);
        }
        else if (oturumNet > 0)
        {
            _kartNetUyariDegerText.text = $"KAZANÇ {oturumNet:N0} TL";
            _kartNetUyariDegerText.color = new Color(0.44f, 0.98f, 0.62f, 1f);
        }
        else
        {
            _kartNetUyariDegerText.text = "BAŞABAŞ";
            _kartNetUyariDegerText.color = Color.white;
        }

        _kartAsamaDegerText.text = asamaAdi;
        _kartAsamaDegerText.color = new Color(1f, 0.88f, 0.58f, 1f);
    }

    void SenaryoOlayKartlariniDoldur()
    {
        if (_senaryoKartIcerikRt == null || _profile == null) return;

        for (int i = _senaryoKartIcerikRt.childCount - 1; i >= 0; i--)
            Destroy(_senaryoKartIcerikRt.GetChild(i).gameObject);

        SenaryoBaslikKartiOlustur("SENARYO AKIŞI", "Aşağıdaki kartlar oturumdaki önemli olayları gösterir.");

        var loglar = SenaryoYoneticisi.GetOturumLoguStatik(_profile.playerId);
        if (loglar == null || loglar.Count == 0)
        {
            SenaryoBilgiKartiOlustur("Bu oturumda henüz senaryo kaydı oluşmamış.");
            return;
        }

        if (sadeceAnlamliOlaylar)
            loglar = FiltreliAnlamliOlaylar(loglar);

        var sonKayitlar = loglar.OrderByDescending(x => x.spinIndex).Take(28).OrderBy(x => x.spinIndex).ToList();
        foreach (var kayit in sonKayitlar)
            SenaryoOlayKartiOlustur(kayit);
    }

    void SenaryoBaslikKartiOlustur(string baslik, string altMetin)
    {
        GameObject kart = new GameObject("SenaryoBaslikKart");
        kart.transform.SetParent(_senaryoKartIcerikRt, false);
        kart.AddComponent<CanvasRenderer>();
        var img = kart.AddComponent<Image>();
        img.color = new Color(0.11f, 0.16f, 0.24f, 0.92f);

        var vlg = kart.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(14, 14, 12, 12);
        vlg.spacing = 4f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        SenaryoKartTextOlustur(kart.transform, baslik, 24, FontStyles.Bold, new Color(0.95f, 0.97f, 1f, 1f));
        SenaryoKartTextOlustur(kart.transform, altMetin, 17, FontStyles.Normal, new Color(0.75f, 0.82f, 0.92f, 1f));
    }

    void SenaryoBilgiKartiOlustur(string metin)
    {
        GameObject kart = new GameObject("SenaryoBilgiKart");
        kart.transform.SetParent(_senaryoKartIcerikRt, false);
        kart.AddComponent<CanvasRenderer>();
        var img = kart.AddComponent<Image>();
        img.color = new Color(0.12f, 0.18f, 0.28f, 0.86f);
        var le = kart.AddComponent<LayoutElement>();
        le.minHeight = 60f;
        le.preferredHeight = 60f;
        var hlg = kart.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(14, 14, 8, 8);
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        SenaryoKartTextOlustur(kart.transform, metin, 18, FontStyles.Normal, Color.white);
    }

    void SenaryoOlayKartiOlustur(SenaryoOlayKaydi kayit)
    {
        GameObject kart = new GameObject("OlayKart");
        kart.transform.SetParent(_senaryoKartIcerikRt, false);
        kart.AddComponent<CanvasRenderer>();
        var img = kart.AddComponent<Image>();
        img.color = new Color(0.09f, 0.14f, 0.22f, 0.88f);

        var le = kart.AddComponent<LayoutElement>();
        le.minHeight = 96f;
        le.preferredHeight = 0f;
        le.flexibleHeight = 0f;

        var vlg = kart.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(14, 14, 10, 10);
        vlg.spacing = 4f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var csf = kart.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        string ustSatir = $"{kayit.zaman}  |  Spin {kayit.spinIndex}  |  {kayit.asamaAdi}";
        SenaryoKartTextOlustur(kart.transform, ustSatir, 15, FontStyles.Bold, new Color(0.70f, 0.82f, 0.95f, 1f));

        string olayEtiket = OlayTipiFarkindalikEtiket(kayit.olayTipi);
        SenaryoKartTextOlustur(kart.transform, $"{olayEtiket}: {kayit.aciklama}", 18, FontStyles.Normal, Color.white);

        Color durumRenk = kayit.netZarar > 0 ? new Color(1f, 0.56f, 0.56f, 1f) : new Color(0.58f, 0.92f, 1f, 1f);
        SenaryoKartTextOlustur(kart.transform, $"Bakiye {kayit.bakiye:N0} TL  •  Net {kayit.netZarar:N0} TL", 16, FontStyles.Bold, durumRenk);
    }

    static TMP_Text SenaryoKartTextOlustur(Transform ebeveyn, string metin, float fontBoyutu, FontStyles stil, Color renk)
    {
        GameObject go = new GameObject("Text");
        go.transform.SetParent(ebeveyn, false);
        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.text = metin;
        txt.fontSize = fontBoyutu;
        txt.fontStyle = stil;
        txt.color = renk;
        txt.enableWordWrapping = true;
        txt.alignment = TextAlignmentOptions.TopLeft;
        txt.raycastTarget = false;
        return txt;
    }

    private System.Collections.IEnumerator ScrollViewLayoutGecikmeliGuncelle()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        if (genelOzetText != null)
        {
            genelOzetText.ForceMeshUpdate(true);
            IcerikYuksekliginiMetindenZorla(genelOzetText);
            GenelOzetScrollIcerikGuncelle();
            ScrollPozisyonuUsteAl(genelOzetText);
        }
        if (senaryoLoguText != null)
        {
            senaryoLoguText.ForceMeshUpdate(true);
            IcerikYuksekliginiMetindenZorla(senaryoLoguText);
            SenaryoLogScrollIcerikGuncelle();
            ScrollPozisyonuUsteAl(senaryoLoguText);
        }
        yield return null;
        Canvas.ForceUpdateCanvases();
        if (genelOzetText != null)
        {
            IcerikYuksekliginiMetindenZorla(genelOzetText);
            GenelOzetScrollIcerikGuncelle();
        }
        if (senaryoLoguText != null)
        {
            IcerikYuksekliginiMetindenZorla(senaryoLoguText);
            SenaryoLogScrollIcerikGuncelle();
        }
        yield return null;
        if (genelOzetText != null) IcerikYuksekliginiMetindenZorla(genelOzetText);
        if (senaryoLoguText != null) IcerikYuksekliginiMetindenZorla(senaryoLoguText);
    }

    /// <summary>Scroll Content yüksekliğini TMP metnin gerçek yüksekliğine göre zorlar; kaydırmanın çalışması için gerekli.</summary>
    static void IcerikYuksekliginiMetindenZorla(TMP_Text metin)
    {
        if (metin == null) return;
        var scroll = metin.GetComponentInParent<ScrollRect>();
        if (scroll == null || scroll.content == null || scroll.viewport == null) return;
        var contentRt = scroll.content as RectTransform;
        var viewportRt = scroll.viewport as RectTransform;
        if (contentRt == null || viewportRt == null) return;
        metin.ForceMeshUpdate(true);
        float metinYukseklik = Mathf.Max(metin.preferredHeight, 100f);
        float viewportYukseklik = viewportRt.rect.height;
        float minIcerikYukseklik = Mathf.Max(metinYukseklik + 32f, viewportYukseklik + 20f);
        contentRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, minIcerikYukseklik);
        scroll.vertical = true;
        scroll.enabled = true;
    }

    /// <summary>Scroll View başlangıçta en üstte görünsün; içerik yüksekliği güncellendikten sonra kaydırmayı üste alır.</summary>
    static void ScrollPozisyonuUsteAl(TMP_Text metin)
    {
        if (metin == null) return;
        var scroll = metin.GetComponentInParent<ScrollRect>();
        if (scroll != null) scroll.verticalNormalizedPosition = 1f;
    }

    /// <summary>Genel özet metni ScrollRect içindeyse Content yüksekliğini metne göre ayarlar; kaydırma çalışır.</summary>
    private void GenelOzetScrollIcerikGuncelle()
    {
        if (genelOzetText == null) return;
        var scroll = genelOzetText.GetComponentInParent<ScrollRect>();
        if (scroll == null || scroll.content == null) return;
        IcerikYuksekliginiMetindenZorla(genelOzetText);
        LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content as RectTransform);
    }

    /// <summary>SenaryoLoguText ScrollRect içindeyse Content yüksekliğini metne göre ayarlar; kaydırma çalışır.</summary>
    private void SenaryoLogScrollIcerikGuncelle()
    {
        if (senaryoLoguText == null) return;
        var scroll = senaryoLoguText.GetComponentInParent<ScrollRect>();
        if (scroll == null || scroll.content == null) return;
        IcerikYuksekliginiMetindenZorla(senaryoLoguText);
        LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content as RectTransform);
    }

    /// <summary>Anlamlı olay tipleri: aşama geçişi, bonus, bakiye/para çekme; spin başladı/bitti vb. hariç.</summary>
    static List<SenaryoOlayKaydi> FiltreliAnlamliOlaylar(List<SenaryoOlayKaydi> tumu)
    {
        var anlamli = new HashSet<string>
        {
            SenaryoOlayKaydi.OlayTipi_OturumBasladi,
            SenaryoOlayKaydi.OlayTipi_AsamaGirisi,
            SenaryoOlayKaydi.OlayTipi_AsamaCikisi,
            SenaryoOlayKaydi.OlayTipi_AsamaGecisi,
            SenaryoOlayKaydi.OlayTipi_AsamaOzeti,
            SenaryoOlayKaydi.OlayTipi_AsamaAralikOzeti,
            SenaryoOlayKaydi.OlayTipi_BonusGirisi,
            SenaryoOlayKaydi.OlayTipi_BonusCikisi,
            SenaryoOlayKaydi.OlayTipi_BonusBitti,
            SenaryoOlayKaydi.OlayTipi_BakiyeYuklemeYapildi,
            SenaryoOlayKaydi.OlayTipi_BakiyeYuklemeEkraniAcildi,
            SenaryoOlayKaydi.OlayTipi_BakiyeYuklemeReddedildi,
            SenaryoOlayKaydi.OlayTipi_ParaCekEkraniAcildi,
            SenaryoOlayKaydi.OlayTipi_ParaCekildi,
            SenaryoOlayKaydi.OlayTipi_ManuelGecis,
            SenaryoOlayKaydi.OlayTipi_RTPOrani,
            SenaryoOlayKaydi.OlayTipi_ZorlukTumbleAyar
        };
        return tumu.Where(x => anlamli.Contains(x.olayTipi)).ToList();
    }

    /// <summary>Olay tipini kullanıcıya farkındalık yaratacak şekilde kısa Türkçe etikete çevirir.</summary>
    static string OlayTipiFarkindalikEtiket(string olayTipi)
    {
        if (string.IsNullOrEmpty(olayTipi)) return olayTipi;
        switch (olayTipi)
        {
            case "OturumBasladi": return "Oturum başladı";
            case "AsamaGirisi": return "Aşamaya giriş";
            case "AsamaCikisi": return "Aşamadan çıkış";
            case "AsamaGecisi": return "Aşama geçişi";
            case "AsamaOzeti": return "Aşama özeti";
            case "AsamaAralikOzeti": return "Aşama aralık özeti";
            case "BonusGirisi": return "Bonus oyununa giriş";
            case "BonusCikisi": return "Bonus oyunundan çıkış";
            case "BonusBitti": return "Bonus oyunu bitti";
            case "BakiyeYuklemeYapildi": return "Bakiye yüklendi";
            case "BakiyeYuklemeEkraniAcildi": return "Bakiye yükleme ekranı açıldı";
            case "BakiyeYuklemeReddedildi": return "Bakiye yükleme reddedildi";
            case "ParaCekEkraniAcildi": return "Para çekme ekranı açıldı";
            case "ParaCekildi": return "Para çekildi";
            case "ManuelGecis": return "Manuel aşama geçişi";
            case "BahisDegisti": return "Bahis miktarı değişti";
            case "Uyari_NetKayipEsigi": return "Net kayıp eşiği aşıldı";
            case "Uyari_UzunOyun": return "Uzun süre ara vermeden oyun";
            case "Uyari_SikBonusAlimi": return "Sık bonus alma davranışı";
            case "Uyari_TiltBahisArtisi": return "Kayıp sonrası bahis artışı";
            case "NearMiss": return "Near-Miss (az daha)";
            case "RTPOrani": return "RTP oranı";
            case "ZorlukTumbleAyar": return "Zorluk/tumble ayarı";
            default: return olayTipi;
        }
    }

    /// <summary>Senaryo logunu tek metin olarak üretir; farkındalık odaklı, sıralı ve aşağı kaydırılabilir.</summary>
    string BuildSenaryoLogTekMetin()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("SENARYO AKIŞI – Farkındalık raporu");
        sb.AppendLine("Bu ekran oturumunuzdaki önemli anları aşama aşama gösterir. Aşağı kaydırarak tüm kayıtları inceleyebilirsiniz.");
        sb.AppendLine();

        var senaryoLoglari = SenaryoYoneticisi.GetOturumLoguStatik(_profile != null ? _profile.playerId : null);

        // Hiç kayıt yoksa kullanıcıya sade bir mesaj göster; gereksiz \"kayıt yok\" satırları üretme.
        if (senaryoLoglari == null || senaryoLoglari.Count == 0)
        {
            sb.AppendLine("Bu oturum için henüz senaryo kaydı oluşmamış.");
            return sb.ToString();
        }

        if (sadeceAnlamliOlaylar)
            senaryoLoglari = FiltreliAnlamliOlaylar(senaryoLoglari);

        if (senaryoLoglari == null || senaryoLoglari.Count == 0)
        {
            sb.AppendLine("Bu oturumda sadece ayrıntılı (yoğun) kayıtlar vardı; anlamlı olay filtresi açık olduğu için listelenmedi.");
            return sb.ToString();
        }

        // 1–7 maddesini özetleyen çarpıcı sonuç bloğu
        sb.AppendLine("── Çarpıcı Özet ──");
        sb.AppendLine(BuildSenaryoCarpiciOzet(senaryoLoglari));
        sb.AppendLine();

        // Aşama bazlı detaylı akış
        var gruplu = senaryoLoglari
            .OrderBy(x => x.spinIndex)
            .GroupBy(x => x.asamaNo)
            .OrderBy(g => g.Key);

        foreach (var grup in gruplu)
        {
            int asamaNo = grup.Key;
            string asamaAdi = GetAsamaAdiByNo(asamaNo);
            sb.AppendLine($"── Senaryo {asamaNo}: {asamaAdi} ──");

            foreach (var kayit in grup)
            {
                string etiket = OlayTipiFarkindalikEtiket(kayit.olayTipi);
                sb.AppendLine($"  {kayit.zaman}");
                sb.AppendLine($"  · {etiket}: {kayit.aciklama}");
                sb.AppendLine($"  · Bakiye: {kayit.bakiye:N0} TL  |  Net sonuç: {kayit.netZarar:N0} TL");
                sb.AppendLine();
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>Senaryo loglarına bakarak oturum için çarpıcı sonuç listesi üretir.</summary>
    string BuildSenaryoCarpiciOzet(List<SenaryoOlayKaydi> loglar)
    {
        if (loglar == null || loglar.Count == 0)
            return "Bu oturum için henüz senaryo kaydı oluşmamış.";

        int toplamSpin = loglar.Max(x => x.spinIndex);
        int netZararSon = loglar.Last().netZarar;

        // En büyük net kayıp anı
        var enKotuAn = loglar
            .OrderByDescending(x => x.netZarar)
            .FirstOrDefault();

        // Uyarı loglarının sayıları
        int netKayipEsikSayisi = loglar.Count(x => x.olayTipi == SenaryoOlayKaydi.OlayTipi_Uyari_NetKayipEsigi);
        int uzunOyunUyariSayisi = loglar.Count(x => x.olayTipi == SenaryoOlayKaydi.OlayTipi_Uyari_UzunOyun);
        int sikBonusUyariSayisi = loglar.Count(x => x.olayTipi == SenaryoOlayKaydi.OlayTipi_Uyari_SikBonusAlimi);
        int tiltUyariSayisi = loglar.Count(x => x.olayTipi == SenaryoOlayKaydi.OlayTipi_Uyari_TiltBahisArtisi);

        // Para hareketleri
        var yuklemeLoglari = loglar.Where(x => x.olayTipi == SenaryoOlayKaydi.OlayTipi_BakiyeYuklemeYapildi).ToList();
        var paraCekLoglari = loglar.Where(x => x.olayTipi == SenaryoOlayKaydi.OlayTipi_ParaCekildi).ToList();

        int toplamYuklemeTutar = TahminiToplamTutar(yuklemeLoglari);
        int toplamCekimTutar = TahminiToplamTutar(paraCekLoglari);

        // Bonus davranışı
        int bonusGirisSayisi = loglar.Count(x => x.olayTipi == SenaryoOlayKaydi.OlayTipi_BonusGirisi);
        int bonusCikisSayisi = loglar.Count(x => x.olayTipi == SenaryoOlayKaydi.OlayTipi_BonusCikisi);

        var sb = new System.Text.StringBuilder();

        // 1) En kritik anlar
        sb.AppendLine("• En kritik anlar:");
        if (enKotuAn != null && enKotuAn.netZarar > 0)
        {
            sb.AppendLine(
                $"  - En yüksek net kayıp: {enKotuAn.netZarar:N0} TL (Spin {enKotuAn.spinIndex}, {enKotuAn.asamaAdi}, bakiye {enKotuAn.bakiye:N0} TL).");
        }
        else
        {
            sb.AppendLine("  - Bu oturumda net kayıp oluşmamış.");
        }

        if (yuklemeLoglari.Count > 0)
        {
            sb.AppendLine(
                $"  - En yoğun para hareketi: {yuklemeLoglari.Count} yükleme, tahmini toplam {toplamYuklemeTutar:N0} TL.");
        }
        if (paraCekLoglari.Count > 0)
        {
            sb.AppendLine(
                $"  - Para çekme: {paraCekLoglari.Count} işlem, tahmini toplam {toplamCekimTutar:N0} TL.");
        }

        // 2) Kayıp davranışı özeti
        sb.AppendLine();
        sb.AppendLine("• Kayıp davranışı özeti:");
        string netDurumMetni;
        if (netZararSon > 0)
            netDurumMetni = $"{netZararSon:N0} TL net kayıp";
        else if (netZararSon < 0)
            netDurumMetni = $"{Mathf.Abs(netZararSon):N0} TL net kazanç";
        else
            netDurumMetni = "başabaş";
        sb.AppendLine($"  - Oturumun sonunda net durum: {netDurumMetni}.");
        if (netKayipEsikSayisi > 0)
            sb.AppendLine($"  - Net kayıp eşiği uyarıları: {netKayipEsikSayisi} kez tetiklendi.");
        else
            sb.AppendLine("  - Net kayıp eşiği uyarısı tetiklenmedi.");

        // 3) Bahis & tilt analizi
        sb.AppendLine();
        sb.AppendLine("• Bahis & tilt analizi:");
        if (tiltUyariSayisi > 0)
            sb.AppendLine($"  - Kayıp sonrası hemen bahis artışı (tilt eğilimi) {tiltUyariSayisi} kez gözlendi.");
        else
            sb.AppendLine("  - Kayıp sonrası hemen bahis artışı kaydı yok; bahis değişiklikleri daha kontrollü.");

        // 4) Bonus kullanım alışkanlığı
        sb.AppendLine();
        sb.AppendLine("• Bonus kullanım alışkanlığı:");
        sb.AppendLine($"  - Bonus oyunu giriş sayısı: {bonusGirisSayisi} (çıkış kaydı: {bonusCikisSayisi}).");
        if (sikBonusUyariSayisi > 0)
            sb.AppendLine($"  - Sık bonus alma uyarıları: {sikBonusUyariSayisi} kez tetiklendi.");
        else
            sb.AppendLine("  - Bonus satın alma davranışı eşik uyarısı üretmedi.");

        // 5) Süre & tempo (toplam spin / zaman bilgisi, genel özetten de görülebilir)
        sb.AppendLine();
        sb.AppendLine("• Süre & tempo:");
        sb.AppendLine($"  - Senaryo logunda görülen toplam spin: {toplamSpin:N0}.");
        if (uzunOyunUyariSayisi > 0)
            sb.AppendLine($"  - Uzun süre ara vermeden oyun uyarısı: {uzunOyunUyariSayisi} kez (15 dk, 30 dk vb. eşikler).");
        else
            sb.AppendLine("  - Uzun süre ara vermeden oyun uyarısı tetiklenmedi.");

        // 6) Para hareketi davranışı
        sb.AppendLine();
        sb.AppendLine("• Para hareketi davranışı:");
        sb.AppendLine($"  - Bakiye yükleme işlemi: {yuklemeLoglari.Count} kez, tahmini toplam {toplamYuklemeTutar:N0} TL.");
        sb.AppendLine($"  - Para çekme işlemi: {paraCekLoglari.Count} kez, tahmini toplam {toplamCekimTutar:N0} TL.");

        // 7) Aşama bazlı kısa rapor
        sb.AppendLine();
        sb.AppendLine("• Aşama bazlı kısa rapor:");
        var asamaGruplari = loglar
            .OrderBy(x => x.spinIndex)
            .GroupBy(x => x.asamaNo)
            .OrderBy(g => g.Key);
        foreach (var g in asamaGruplari)
        {
            var sonKayit = g.Last();
            int asamaNet = sonKayit.netZarar;
            int asamaSpinMax = g.Max(x => x.spinIndex) - g.Min(x => x.spinIndex) + 1;
            int asamaYukleme = g.Count(x => x.olayTipi == SenaryoOlayKaydi.OlayTipi_BakiyeYuklemeYapildi);
            int asamaBonus = g.Count(x => x.olayTipi == SenaryoOlayKaydi.OlayTipi_BonusGirisi);
            sb.AppendLine(
                $"  - {sonKayit.asamaAdi}: ~{asamaSpinMax} spin, {asamaYukleme} yükleme, {asamaBonus} bonus girişi, net sonuç ≈ {asamaNet:N0} TL.");
        }

        return sb.ToString();
    }

    /// <summary>\"Bakiye yüklemesi: 1.000 TL\" gibi açıklamalardan tahmini toplam tutarı çeker.</summary>
    int TahminiToplamTutar(List<SenaryoOlayKaydi> loglar)
    {
        if (loglar == null || loglar.Count == 0) return 0;
        int toplam = 0;
        foreach (var k in loglar)
        {
            if (string.IsNullOrEmpty(k.aciklama)) continue;
            // Sayıları çekmek için tüm rakam karakterlerini toplayıp int'e çevirmeye çalış.
            var digits = new System.Text.StringBuilder();
            foreach (char c in k.aciklama)
            {
                if (char.IsDigit(c))
                    digits.Append(c);
                else if (digits.Length > 0 && (c == '.' || c == ' ' || c == ',')) // binlik ayırıcıları atla
                    continue;
                else if (digits.Length > 0)
                    break;
            }
            if (int.TryParse(digits.ToString(), out int tutar))
                toplam += tutar;
        }
        return toplam;
    }

    /// <summary>Sağ scroll'a genel özeti tek blok olarak yazar (çok satır yok, üst üste binme olmaz).</summary>
    void OzetScrollContentDoldur(Transform content, string ozetMetni)
    {
        if (content == null) return;
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
        var contentRt = content as RectTransform;
        if (contentRt != null)
        {
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;
        }
        var blok = new GameObject("OzetBlok");
        blok.transform.SetParent(content, false);
        var txt = blok.AddComponent<TextMeshProUGUI>();
        txt.text = ozetMetni;
        txt.fontSize = 20;
        txt.alignment = TMPro.TextAlignmentOptions.TopLeft;
        txt.raycastTarget = false;
        txt.enableWordWrapping = true;
        var rt = (RectTransform)blok.transform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(12f, 12f);
        rt.offsetMax = new Vector2(-12f, -12f);
    }

        /// <summary>Genel özet metnini üretir: giriş yapan kullanıcıya ait bu oturum istatistikleri ve farkındalık logları.</summary>
        string BuildGenelOzetMetni()
        {
            var p = _profile;
            int bonusSatinAlma = BonusSatinAlmaSayisi();
            string sonAsama = "";
            int toplamYatirilanOturum = 0;
            int bahisArtirim = 0;
            float oturumSuresiDakika = 0f;
            if (SenaryoYoneticisi.I != null)
            {
                sonAsama = SenaryoYoneticisi.I.GetAsamaAdi();
                toplamYatirilanOturum = SenaryoYoneticisi.I.toplamYatirilanOturum;
                bahisArtirim = SenaryoYoneticisi.I.bahisArtirimSayisi;
                oturumSuresiDakika = SenaryoYoneticisi.I.oyunSuresiDakika;
            }

            int oturumSpin = GameManager.I != null ? GameManager.I.OturumSpinSayisi : 0;
            int oturumYatirilan = GameManager.I != null ? GameManager.I.OturumYatirilan : 0;
            int oturumCekilen = GameManager.I != null ? GameManager.I.OturumCekilen : 0;
            int oturumNet = GameManager.I != null ? GameManager.I.OturumNet : 0;
            int oturumBonus = GameManager.I != null ? GameManager.I.OturumBonusGiris : 0;

            int gosterimBakiye = p != null ? p.balance : 0;
            var sessionLog = SenaryoYoneticisi.GetOturumLoguStatik(p?.playerId);
            if (sessionLog != null && sessionLog.Count > 0)
            {
                var yuklemeLoglari = sessionLog.Where(x => x.olayTipi == SenaryoOlayKaydi.OlayTipi_BakiyeYuklemeYapildi).ToList();
                var paraCekLoglari = sessionLog.Where(x => x.olayTipi == SenaryoOlayKaydi.OlayTipi_ParaCekildi).ToList();
                int logYatirilan = TahminiToplamTutar(yuklemeLoglari);
                int logCekilen = TahminiToplamTutar(paraCekLoglari);
                if (yuklemeLoglari.Count > 0) oturumYatirilan = logYatirilan;
                if (paraCekLoglari.Count > 0) oturumCekilen = logCekilen;
                gosterimBakiye = sessionLog.Last().bakiye;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("── BU OTURUM ÖZETİ ──");
            sb.AppendLine("(Bu girişe ait veriler; her kullanıcı kendi oturumunu görür)");
            sb.AppendLine();
            sb.AppendLine($"Profil: {p.playerName}");
            sb.AppendLine();
            sb.AppendLine("── Bonus ──");
            sb.AppendLine($"Kaç kez bonus oyun oynadı: {oturumBonus} (bu oturum)");
            sb.AppendLine($"Kaç kez bonus satın aldı: {bonusSatinAlma} (toplam profil)");
            sb.AppendLine();
            sb.AppendLine("── Para & Bakiye ──");
            sb.AppendLine($"Para çekme miktarı (bu oturum): {oturumCekilen:N0} TL");
            sb.AppendLine($"Para çekme miktarı (toplam): {p.totalWithdrawn:N0} TL");
            sb.AppendLine($"Bu oturum yatırım: {oturumYatirilan:N0} TL");
            sb.AppendLine($"Bu oturum çekim: {oturumCekilen:N0} TL");
            sb.AppendLine($"Bu oturum net sonuç: {oturumNet:N0} TL");
            sb.AppendLine($"Güncel bakiye: {gosterimBakiye:N0} TL");
            sb.AppendLine();
            sb.AppendLine("── Süre & Dönüş ──");
            sb.AppendLine($"Bu oturum süresi: {oturumSuresiDakika:F1} dakika");
            sb.AppendLine($"Bu oturum dönüş sayısı: {oturumSpin:N0}");
            if (oturumSpin > 0 && oturumSuresiDakika > 0f)
                sb.AppendLine($"Ortalama dönüş süresi: {(oturumSuresiDakika * 60f / oturumSpin):F1} saniye/dönüş");
            else
                sb.AppendLine("Ortalama dönüş süresi: —");
            sb.AppendLine();
            sb.AppendLine("── Farkındalık ──");
            if (oturumNet < 0)
                sb.AppendLine($"Net durum uyarısı: Bu oturumda toplam {Math.Abs(oturumNet):N0} TL net kayıp.");
            else if (oturumNet > 0)
                sb.AppendLine($"Net durum uyarısı: Bu oturumda toplam {oturumNet:N0} TL net kazanç.");
            else
                sb.AppendLine("Net durum uyarısı: Bu oturumda net değişim yok.");
            if (oturumYatirilan > 0)
                sb.AppendLine($"Oran bilgisi: Yatırılanın %{(100f * oturumCekilen / oturumYatirilan):F0}'i bu oturumda çekildi.");
            else
                sb.AppendLine("Oran bilgisi: Bu oturumda yatırım yok.");
            if (oturumSpin > 0)
                sb.AppendLine($"Bonus oranı: Dönüşlerin %{(100f * oturumBonus / oturumSpin):F1}'inde bonus oyununa girildi.");
            else
                sb.AppendLine("Bonus oranı: —");
            sb.AppendLine();
            sb.AppendLine("── Hız & Çarpan ──");
            var loglar = SenaryoYoneticisi.GetOturumLoguStatik(p?.playerId);
            var hizlandiSpinler = loglar != null ? loglar.Where(x => x.olayTipi == SenaryoOlayKaydi.OlayTipi_HizHizlandi).Select(x => x.spinIndex).OrderBy(x => x).ToList() : new List<int>();
            var yavasladiSpinler = loglar != null ? loglar.Where(x => x.olayTipi == SenaryoOlayKaydi.OlayTipi_HizYavasladi).Select(x => x.spinIndex).OrderBy(x => x).ToList() : new List<int>();
            var carpanArttiKayitlar = loglar != null ? loglar.Where(x => x.olayTipi == SenaryoOlayKaydi.OlayTipi_BonusCarpanArtti).ToList() : new List<SenaryoOlayKaydi>();
            sb.AppendLine(hizlandiSpinler.Count > 0 ? "Hangi spinlerde hızlandı: Spin " + string.Join(", ", hizlandiSpinler) + " (normal hıza dönüldü)." : "Hangi spinlerde hızlandı: Bu oturumda olmadı.");
            sb.AppendLine(yavasladiSpinler.Count > 0 ? "Ne zaman yavaşladı: Spin " + string.Join(", ", yavasladiSpinler) + " (bonus başladı)." : "Ne zaman yavaşladı: Bu oturumda olmadı.");
            sb.AppendLine("Ne zaman çarpan düşmesi açıldı: Aşama geçişlerinde (sağ panel senaryo logunda)");
            if (carpanArttiKayitlar.Count > 0)
            {
                var spinler = carpanArttiKayitlar.Select(k => k.spinIndex).Distinct().OrderBy(x => x).Take(15).ToList();
                var carpanOzet = "Spin " + string.Join(", ", spinler);
                if (carpanArttiKayitlar.Count > 15) carpanOzet += $" … ({carpanArttiKayitlar.Count} kez)";
                sb.AppendLine("Ne zaman bonus oyunda çarpan oranı arttı: " + carpanOzet);
            }
            else
                sb.AppendLine("Ne zaman bonus oyunda çarpan oranı arttı: Bu oturumda olmadı.");
            sb.AppendLine();
            sb.AppendLine("── Zorluk & Olasılık ──");
            string sonZorlukDegisimi = SonAsamaGecisiMetni();
            sb.AppendLine(sonZorlukDegisimi);
            sb.AppendLine("Ne zaman olasılık düşer: Aşama geçişi ile değişir (senaryo logunda)");
            var zorlukTumbleKayitlari = loglar != null ? loglar.Where(x => x.olayTipi == SenaryoOlayKaydi.OlayTipi_ZorlukTumbleAyar).OrderBy(x => x.spinIndex).ThenBy(x => x.zaman).ToList() : new List<SenaryoOlayKaydi>();
            if (zorlukTumbleKayitlari.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Bu oturumda uygulanan senaryo zorluk/tumble ayarları:");
                foreach (var k in zorlukTumbleKayitlari)
                    sb.AppendLine($"  Spin {k.spinIndex}: {k.aciklama}");
            }
            sb.AppendLine();
            sb.AppendLine("── RTP ──");
            var rtpKayitlari = loglar != null ? loglar.Where(x => x.olayTipi == SenaryoOlayKaydi.OlayTipi_RTPOrani).OrderBy(x => x.spinIndex).ThenBy(x => x.zaman).ToList() : new List<SenaryoOlayKaydi>();
            if (rtpKayitlari.Count > 0)
            {
                var sonRtp = rtpKayitlari.Last();
                sb.AppendLine($"RTP oranı (son log): {sonRtp.aciklama}");
                if (rtpKayitlari.Count > 1)
                    sb.AppendLine($"  (Toplam {rtpKayitlari.Count} RTP kaydı bu oturumda)");
            }
            else if (p != null && p.totalWagered > 0)
            {
                float rtpYuzde = 100f * (float)p.totalWon / (float)p.totalWagered;
                sb.AppendLine($"RTP oranı (profil): %{rtpYuzde:F1} (toplam kazanç: {p.totalWon:N0} TL, toplam bahis: {p.totalWagered:N0} TL)");
            }
            else
                sb.AppendLine("RTP oranı: Henüz yeterli bahis verisi yok.");
            sb.AppendLine();
            sb.AppendLine("── Özet ──");
            sb.AppendLine($"Toplam ödeme (kazanç): {p.totalWon:N0} TL");
            sb.AppendLine($"Toplam kayıp (bahis – kazanç): {p.totalLost:N0} TL");
            if (SenaryoYoneticisi.I != null)
            {
                sb.AppendLine();
                sb.AppendLine("Senaryo oturumu:");
                sb.AppendLine($"  Mevcut aşama: {sonAsama}");
                sb.AppendLine($"  Oturum yatırımı: {toplamYatirilanOturum:N0} TL");
                sb.AppendLine($"  Bahis değişikliği adedi: {bahisArtirim}");
            }
            sb.AppendLine();
            sb.AppendLine("Bu rapor, bu girişteki senaryolu oturum davranışını belgelemek amacıyla oluşturulmuştur.");
            return sb.ToString();
        }

        /// <summary>Senaryo logundan son aşama geçişini bulur; "ne zaman zorluk artar" için metin döner.</summary>
        string SonAsamaGecisiMetni()
        {
            var loglar = SenaryoYoneticisi.GetOturumLoguStatik(_profile != null ? _profile.playerId : null);
            if (loglar == null || loglar.Count == 0) return "Ne zaman zorluk artar: — (henüz aşama geçişi yok)";
            var sonGecis = loglar.Where(x => x.olayTipi == SenaryoOlayKaydi.OlayTipi_AsamaGecisi).OrderBy(x => x.spinIndex).LastOrDefault();
            if (sonGecis == null) return "Ne zaman zorluk artar: — (henüz aşama geçişi yok)";
            return $"Ne zaman zorluk artar: Son geçiş — {sonGecis.zaman}, Spin {sonGecis.spinIndex} ({sonGecis.aciklama})";
        }

        void IlkBlokOzetEkle(Transform parent, string ozetMetni)
        {
            if (parent == null) return;
            ContentLayoutAyarla(parent);
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
            var blok = new GameObject("Blok_GenelOzet");
            blok.transform.SetParent(parent, false);
            var txt = blok.AddComponent<TextMeshProUGUI>();
            txt.text = ozetMetni;
            txt.fontSize = 22;
            txt.raycastTarget = false;
            txt.enableWordWrapping = true;
            var rt = (RectTransform)blok.transform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0, 480);
            var le = blok.AddComponent<LayoutElement>();
            le.minHeight = 480;
            le.preferredHeight = 480;
        }

        void SenaryoLogunuDoldur(Transform content, bool ozetAyriPanelde)
        {
            if (content == null) return;
            ContentLayoutAyarla(content);
            if (ozetAyriPanelde)
            {
                for (int i = content.childCount - 1; i >= 0; i--)
                    Destroy(content.GetChild(i).gameObject);
            }

            var senaryoLoglari = SenaryoYoneticisi.GetOturumLoguStatik(_profile != null ? _profile.playerId : null);
            if (sadeceAnlamliOlaylar && senaryoLoglari != null && senaryoLoglari.Count > 0)
                senaryoLoglari = FiltreliAnlamliOlaylar(senaryoLoglari);
            var gruplu = senaryoLoglari != null && senaryoLoglari.Count > 0
                ? senaryoLoglari.OrderBy(x => x.spinIndex).GroupBy(x => x.asamaNo).ToDictionary(g => g.Key, g => g.ToList())
                : new Dictionary<int, List<SenaryoOlayKaydi>>();

            EkleBaslikSatiri(content, "SENARYO AKIŞ RAPORU — Aşama bazlı olay kayıtları" + (sadeceAnlamliOlaylar ? " (anlamlı olaylar)" : ""), 24, 56);
            EkleBaslikSatiri(content, "Aşağıda 1–7 numaralı senaryo aşamalarına ait kronolojik olay listesi yer almaktadır.", 18, 36);

            bool prefabVar = logSatirPrefab != null;
            for (int asamaNo = 1; asamaNo <= 7; asamaNo++)
            {
                string asamaAdi = GetAsamaAdiByNo(asamaNo);
                EkleBaslikSatiri(content, $"Senaryo {asamaNo} – {asamaAdi}", 20, 44);
                if (gruplu.TryGetValue(asamaNo, out var kayitlar) && kayitlar != null && kayitlar.Count > 0)
                {
                    foreach (var kayit in kayitlar)
                    {
                        string satir = $"{kayit.zaman}  ·  {kayit.olayTipi}: {kayit.aciklama}  ·  Bakiye: {kayit.bakiye:N0} TL  ·  Net sonuç: {kayit.netZarar:N0} TL";
                        EkleLogSatiri(content, satir, prefabVar);
                    }
                }
                else
                    EkleLogSatiri(content, "Bu aşamada kayıt bulunmuyor.", prefabVar);
            }
            SenaryoContentLayoutZorla(content);
        }

        /// <summary>Content genişliğini viewport ile eşitler, layout'u hemen hesaplatır; satırlar üst üste binmesin.</summary>
        void SenaryoContentLayoutZorla(Transform content)
        {
            if (content == null) return;
            var contentRt = content as RectTransform;
            if (contentRt == null) return;
            Transform viewportT = content.parent;
            if (viewportT != null)
            {
                var viewportRt = viewportT as RectTransform;
                if (viewportRt != null)
                {
                    float genislik = viewportRt.rect.width;
                    if (genislik > 50f)
                    {
                        contentRt.anchorMin = new Vector2(0f, 1f);
                        contentRt.anchorMax = new Vector2(1f, 1f);
                        contentRt.pivot = new Vector2(0.5f, 1f);
                        contentRt.offsetMin = new Vector2(0f, 0f);
                        contentRt.offsetMax = new Vector2(0f, 0f);
                        contentRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, genislik);
                    }
                }
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
        }

        /// <summary>Content'te VerticalLayoutGroup + ContentSizeFitter ekler; satırlar alta alta dizilir, scroll yüksekliği içeriğe göre büyür.</summary>
        void ContentLayoutAyarla(Transform content)
        {
            if (content == null) return;
            var le = content.GetComponent<LayoutElement>();
            if (le == null) le = content.gameObject.AddComponent<LayoutElement>();
            le.minWidth = 450;
            le.preferredWidth = 500;
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
            {
                vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
                vlg.childAlignment = TextAnchor.UpperLeft;
                vlg.childControlWidth = true;
                vlg.childControlHeight = true;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;
                vlg.spacing = 10;
                vlg.padding = new RectOffset(12, 12, 12, 12);
            }
            var csf = content.GetComponent<ContentSizeFitter>();
            if (csf == null)
            {
                csf = content.gameObject.AddComponent<ContentSizeFitter>();
                csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        string GetAsamaAdiByNo(int asamaNo)
        {
            if (SenaryoYoneticisi.I != null)
                return SenaryoYoneticisi.I.GetAsamaAdi((SenaryoYoneticisi.SenaryoAsama)Mathf.Clamp(asamaNo, 1, 7));
            return asamaNo switch
            {
                1 => "Isındırma / Umut",
                2 => "Kontrol bende",
                3 => "Az daha / Kayıp kovalama",
                4 => "Bakiye tükenişi",
                5 => "Bonus zirve",
                6 => "Gerçek kayıp",
                7 => "Finale",
                _ => "Bilinmiyor"
            };
        }

        void EkleBaslikSatiri(Transform parent, string baslik, int fontSize = 18, int satirYukseklik = 40)
        {
            var row = new GameObject("Baslik");
            row.transform.SetParent(parent, false);
            var txt = row.AddComponent<TextMeshProUGUI>();
            txt.text = baslik;
            txt.fontSize = fontSize;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TMPro.TextAlignmentOptions.TopLeft;
            txt.raycastTarget = false;
            txt.enableWordWrapping = true;
            var rt = (RectTransform)row.transform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0, satirYukseklik);
            var le = row.AddComponent<LayoutElement>();
            le.minHeight = satirYukseklik;
            le.preferredHeight = satirYukseklik;
            le.minWidth = 380;
        }

    /// <summary>Senaryo logunu TXT olarak persistentDataPath'e yazar. Log sahnesinde bir butona bağlanabilir.</summary>
    public void SenaryoLogunuTxtOlarakDisariAktar()
    {
        string metin = BuildGenelOzetMetni() + "\n\n" + BuildSenaryoLogTekMetin();
        string dosyaAdi = $"SenaryoLogu_{(_profile != null ? _profile.playerName : "Oyuncu")}_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt";
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            GUIUtility.systemCopyBuffer = metin ?? "";
            Debug.Log("[LogYoneticisi] WebGL fallback: log metni panoya kopyalandı.");
            WebGlSonDisaAktarDurumu = "export: pano ok";
            if (disAktarBilgiText != null)
            {
                disAktarBilgiText.text = "WebGL: Log metni panoya kopyalandı.";
                disAktarBilgiText.gameObject.SetActive(true);
                CancelInvoke(nameof(DisAktarBilgiTemizle));
                Invoke(nameof(DisAktarBilgiTemizle), 4f);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[LogYoneticisi] WebGL panoya kopyalama hatası: {ex.Message}");
            WebGlSonDisaAktarDurumu = "export hata: izin/clipboard";
            if (disAktarBilgiText != null)
            {
                disAktarBilgiText.text =
                    "Log panoya kopyalanamadı. Tekrar dene, tarayıcı pano izinlerini kontrol et. " +
                    "Gerekirse log ekranındaki metni manuel seçip kopyala.";
                disAktarBilgiText.gameObject.SetActive(true);
                CancelInvoke(nameof(DisAktarBilgiTemizle));
                Invoke(nameof(DisAktarBilgiTemizle), 6f);
            }
        }
        return;
#else
        string path = Path.Combine(Application.persistentDataPath, dosyaAdi);
        try
        {
            File.WriteAllText(path, metin, System.Text.Encoding.UTF8);
            Debug.Log($"[LogYoneticisi] Senaryo logu dışa aktarıldı: {path}");
            if (disAktarBilgiText != null)
            {
                disAktarBilgiText.text = "Log kaydedildi: " + path;
                disAktarBilgiText.gameObject.SetActive(true);
                CancelInvoke(nameof(DisAktarBilgiTemizle));
                Invoke(nameof(DisAktarBilgiTemizle), 4f);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[LogYoneticisi] Dışa aktarma hatası: {ex.Message}");
            if (disAktarBilgiText != null)
            {
                disAktarBilgiText.text = "Hata: " + ex.Message;
                disAktarBilgiText.gameObject.SetActive(true);
            }
        }
#endif
    }

    void DisAktarBilgiTemizle()
    {
        if (disAktarBilgiText != null)
            disAktarBilgiText.text = "";
    }

    /// <summary>Dışa aktar butonuna tıklanınca çağrılır; logu kaydeder ve opsiyonel bilgi metnini gösterir.</summary>
    void DisariAktarVeBilgiGoster()
    {
        SenaryoLogunuTxtOlarakDisariAktar();
    }

    int BonusSatinAlmaSayisi()
    {
        if (_profile.logs == null) return 0;
        return _profile.logs.Count(x => x.type == "BONUS_BUY");
    }

    void EkleLogSatiri(Transform parent, string satirMetni, bool prefabVar)
    {
        if (parent == null) return;
        const int satirYukseklik = 44;
        const int fontBoyutu = 19;
        if (prefabVar && logSatirPrefab != null)
        {
            var go = Instantiate(logSatirPrefab, parent);
            var texts = go.GetComponentsInChildren<TMP_Text>();
            if (texts.Length >= 1) { texts[0].text = satirMetni; texts[0].fontSize = fontBoyutu; texts[0].alignment = TMPro.TextAlignmentOptions.TopLeft; }
            if (texts.Length >= 2) texts[1].text = "";
            if (texts.Length >= 3) texts[2].text = "";
            var layoutEl = go.GetComponent<LayoutElement>();
            if (layoutEl != null) { layoutEl.minHeight = satirYukseklik; layoutEl.preferredHeight = satirYukseklik; }
            return;
        }
        var row = new GameObject("LogSatir");
        row.transform.SetParent(parent, false);
        var txt = row.AddComponent<TextMeshProUGUI>();
        txt.text = satirMetni;
        txt.fontSize = fontBoyutu;
        txt.alignment = TMPro.TextAlignmentOptions.TopLeft;
        txt.raycastTarget = false;
        txt.enableWordWrapping = true;
        var rt = (RectTransform)row.transform;
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0, satirYukseklik);
        var le = row.AddComponent<LayoutElement>();
        le.minHeight = satirYukseklik;
        le.preferredHeight = satirYukseklik;
        le.minWidth = 380;
    }

    void BonusReplayGoster()
    {
        // Bonus replay özelliği: Bonus oyunların detaylı logunu göster
        Debug.Log("[LOG] Bonus Replay gösteriliyor...");

        // TODO: Detaylı bonus replay UI'sı eklenebilir
        // Şimdilik konsola yazdır
        if (_profile.logs != null)
        {
            var bonusLoglari = _profile.logs.Where(x => x.type.Contains("BONUS")).ToList();
            Debug.Log($"Toplam bonus log sayısı: {bonusLoglari.Count}");
        }
    }

    /// <summary>Girişe dön butonunu bulur, etkinleştirir ve tıklanınca GeriDon çağrılacak şekilde bağlar.</summary>
    void GiriseDonButonunuBagla()
    {
        if (geriDonButon == null)
        {
            Debug.LogWarning("[LogYoneticisi] geriDonButon bulunamadı; Buton_GirisEkrani adını ve text'ini kontrol et.");
            return;
        }

        // Button etkin ve tıklanabilir olsun
        geriDonButon.interactable = true;

        // Üstündeki Image raycast alıyor olsun (aksi halde tıklamayı kaçırır)
        var img = geriDonButon.GetComponent<Image>();
        if (img != null)
        {
            img.raycastTarget = true;
        }

        // Her ihtimale karşı parent CanvasGroup üzerinde raycast ve interactable açık olsun
        var parentCanvasGroup = geriDonButon.GetComponentInParent<CanvasGroup>();
        if (parentCanvasGroup != null)
        {
            parentCanvasGroup.interactable = true;
            parentCanvasGroup.blocksRaycasts = true;
            if (parentCanvasGroup.alpha <= 0f)
                parentCanvasGroup.alpha = 1f;
        }

        // Butonu Canvas'ın doğrudan altına taşı ve en son sıraya al; böylece tüm UI'ın üstünde çizilir ve tıklama engellenmez
        var canvas = geriDonButon.GetComponentInParent<Canvas>();
        if (canvas != null && geriDonButon.transform.parent != canvas.transform)
        {
            geriDonButon.transform.SetParent(canvas.transform, true);
            geriDonButon.transform.SetAsLastSibling();
        }
        else
        {
            geriDonButon.transform.SetAsLastSibling();
        }

        // Her seferinde sadece tek listener kalsın
        geriDonButon.onClick.RemoveAllListeners();
        geriDonButon.onClick.AddListener(GeriDon);

        Debug.Log("[LogYoneticisi] Girişe dön butonu bağlandı ve etkinleştirildi.");
    }

    /// <summary>En üstte çizilen ayrı bir Canvas ve "Girişe dön" butonu oluşturur. Sahne yapısından bağımsız, her zaman tıklanabilir.</summary>
    void GirisDonOverlayCanvasOlustur()
    {
        var canvasGo = new GameObject("GirisDonOverlayCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var panelGo = new GameObject("Panel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        var panelRt = panelGo.AddComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        var btnGo = new GameObject("GirisDonButon_Overlay");
        btnGo.transform.SetParent(panelGo.transform, false);
        var btnRt = btnGo.AddComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0f);
        btnRt.anchorMax = new Vector2(0.5f, 0f);
        btnRt.pivot = new Vector2(0.5f, 0f);
        btnRt.sizeDelta = new Vector2(220f, 56f);
        btnRt.anchoredPosition = new Vector2(0f, 36f);

        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0.25f, 0.45f, 0.85f, 1f);
        btnImg.raycastTarget = true;

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.interactable = true;
        btn.onClick.AddListener(GeriDon);

        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(btnGo.transform, false);
        var txtRt = txtGo.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;
        var txt = txtGo.AddComponent<TextMeshProUGUI>();
        txt.text = "GİRİŞE DÖN";
        txt.fontSize = 22;
        txt.alignment = TMPro.TextAlignmentOptions.Center;
        txt.raycastTarget = false;
    }

    public void GeriDon()
    {
        Debug.Log("[LogYoneticisi] GeriDon BUTONA TIKLANDI");
        // Girişe dön: giriş sahnesine git (seçili kullanıcı değişmez; tekrar giriş yapılabilir).
        const string girisSahneAdi = "01_GirisScene";
        if (GameManager.I != null)
            GameManager.I.LoadScene(girisSahneAdi);
        else
            SceneManager.LoadScene(girisSahneAdi, LoadSceneMode.Single);
    }

    public void Yenile()
    {
        VerileriYukle();
    }
}

