using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Full-screen log overlay. LogEkrani.Ac() ile tetiklenir — 55. spin veya "Oturumu Bitir" butonundan.
/// </summary>
public class LogEkrani : MonoBehaviour
{
    private static LogEkrani _instance;

    const string GIRIS_SAHNE = "01_GirisScene";
    const string OYUN_SAHNE  = "03_SenaryoluOyun";

    // ── Statik giriş noktası ────────────────────────────────────────
    public static void Ac()
    {
        if (_instance != null) return;
        var canvas = FindCanvas();
        if (canvas == null) { Debug.LogWarning("[LogEkrani] Canvas bulunamadı."); return; }
        var go = new GameObject("[LogEkrani]", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsLastSibling();
        _instance = go.AddComponent<LogEkrani>();
        _instance.Build(canvas);
    }

    static Canvas FindCanvas()
    {
        foreach (var c in Object.FindObjectsOfType<Canvas>())
            if (c.renderMode == RenderMode.ScreenSpaceOverlay || c.renderMode == RenderMode.ScreenSpaceCamera)
                return c;
        return null;
    }

    // ── Renkler ────────────────────────────────────────────────────
    static Color C(string hex, float a = 1f)
    {
        Color c = Color.white; ColorUtility.TryParseHtmlString(hex, out c); c.a = a; return c;
    }

    // ── UI inşa ────────────────────────────────────────────────────
    void Build(Canvas canvas)
    {
        OturumKayitcisi.BitisBakiyesi = GameManager.I?.ActivePlayer != null
            ? GameManager.I.ActivePlayer.balance : OturumKayitcisi.BitisBakiyesi;

        var rt = GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        // Overlay arka planı
        var ovImg = gameObject.AddComponent<Image>();
        ovImg.color = new Color(10f/255f, 20f/255f, 40f/255f, .95f);
        ovImg.raycastTarget = true;

        // CanvasGroup fade-in için
        var cg = gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        // ── Orta kart ──
        var kartGo = new GameObject("Kart", typeof(RectTransform));
        kartGo.transform.SetParent(transform, false);
        var kartRt = kartGo.GetComponent<RectTransform>();
        kartRt.anchorMin = new Vector2(.5f, .5f); kartRt.anchorMax = new Vector2(.5f, .5f);
        kartRt.pivot = new Vector2(.5f, .5f);
        kartRt.anchoredPosition = Vector2.zero;
        kartRt.sizeDelta = new Vector2(900f, 700f);

        kartGo.AddComponent<Image>().color = C("#1a0f2e");

        // Altın kenarlık
        BuildBorder(kartGo.transform, C("#FAC775", .50f));

        // İçerik GO
        var icGo = new GameObject("Icerik", typeof(RectTransform));
        icGo.transform.SetParent(kartGo.transform, false);
        var icRt = icGo.GetComponent<RectTransform>();
        icRt.anchorMin = Vector2.zero; icRt.anchorMax = Vector2.one;
        icRt.offsetMin = new Vector2(48f, 24f); icRt.offsetMax = new Vector2(-48f, -24f);

        var vlg = icGo.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.spacing = 0f;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        // Başlık
        string ad = KullaniciVerileri.KullaniciAdi;
        string baslikStr = (ad == "Misafir" || string.IsNullOrEmpty(ad))
            ? "Bu oturumda sana neler yapıldı?"
            : $"{ad}, bu oturumda sana neler yapıldı?";

        var baslik = AddTxt(icGo.transform, baslikStr, 28, Color.white, bold: true, prefH: 44);
        baslik.enableVertexGradient = true;
        baslik.colorGradient = new VertexGradient(C("#FFF0C0"), C("#FFF0C0"), C("#CD8B1F"), C("#CD8B1F"));
        Spacer(icGo.transform, 6f);
        AddTxt(icGo.transform, "Her hissettiğin şeyin teknik karşılığı burada.", 14,
               new Color(1f, 1f, 1f, .70f), prefH: 22);
        Spacer(icGo.transform, 14f);

        // Altın ayırıcı çizgi
        var sepGo = new GameObject("Sep", typeof(RectTransform), typeof(Image));
        sepGo.transform.SetParent(icGo.transform, false);
        var sepImg = sepGo.GetComponent<Image>();
        sepImg.color = C("#FAC775", .40f); sepImg.raycastTarget = false;
        var sepLE = sepGo.AddComponent<LayoutElement>();
        sepLE.preferredHeight = 2f;
        Spacer(icGo.transform, 14f);

        // ── Scroll View ──
        var scrollGo = new GameObject("Scroll", typeof(RectTransform));
        scrollGo.transform.SetParent(icGo.transform, false);
        var scrollLE = scrollGo.AddComponent<LayoutElement>();
        scrollLE.preferredHeight = 440f;
        scrollLE.flexibleHeight  = 1f;
        scrollLE.flexibleWidth   = 1f;

        var scrollRect = scrollGo.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;

        var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportGo.transform.SetParent(scrollGo.transform, false);
        var vpRt = viewportGo.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = Vector2.zero; vpRt.offsetMax = Vector2.zero;
        scrollRect.viewport = vpRt;

        var contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(viewportGo.transform, false);
        var contentRt = contentGo.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f); contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot     = new Vector2(.5f, 1f);
        contentRt.offsetMin = Vector2.zero; contentRt.offsetMax = Vector2.zero;
        scrollRect.content = contentRt;

        var contentVlg = contentGo.AddComponent<VerticalLayoutGroup>();
        contentVlg.childAlignment = TextAnchor.UpperLeft;
        contentVlg.spacing = 6f;
        contentVlg.childControlWidth = true; contentVlg.childControlHeight = true;
        contentVlg.childForceExpandWidth = true; contentVlg.childForceExpandHeight = false;
        contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // İçerik verilerini doldur
        DoldurIcerik(contentGo.transform);

        Spacer(icGo.transform, 16f);

        // ── Alt butonlar ──
        var btnRow = new GameObject("Butonlar", typeof(RectTransform));
        btnRow.transform.SetParent(icGo.transform, false);
        var btnLE2 = btnRow.AddComponent<LayoutElement>();
        btnLE2.preferredHeight = 44f;
        var hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20f; hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false; hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;

        BuildBtn(btnRow.transform, "ANA EKRANA DÖN", C("#FAC775"), C("#1a0f2e"), 200f, 44f, AnaEkranaDon);
        BuildBtn(btnRow.transform, "TEKRAR OYNA",    C("#1a0f2e"), C("#FAC775"), 160f, 44f, TekrarOyna,
                 borderCol: C("#FAC775", .55f));

        StartCoroutine(FadeIn(cg));
    }

    void DoldurIcerik(Transform content)
    {
        var ozet = OturumKayitcisi.OlaylariOzetleKategorize();

        // ── SENARYO GEÇİŞLERİ ──
        if (ozet.SenaryoGecisleri.Count > 0)
        {
            Bolum(content, "SENARYO GEÇİŞLERİ");
            foreach (var o in ozet.SenaryoGecisleri)
            {
                string senAd  = ParamAl(o.Detay, "senaryo");
                string egilim = ParamAl(o.Detay, "egilim");
                string max    = ParamAl(o.Detay, "max");
                string bolumAcikl = SenaryoAcikla(senAd, o.SpinNo);
                IcerikSatir(content, $"• {bolumAcikl}", C("#FAC775", .90f));
                if (!string.IsNullOrEmpty(egilim))
                    IcerikSatir(content, $"  (Ödeme eğilimi %{egilim}, tavan {max} TL)",
                                new Color(1f, 1f, 1f, .55f), 12f);
            }
            Spacer(content, 10f);
        }

        // ── ARDIŞIK KAYIP ──
        if (ozet.ArdisikKayiplar.Count > 0)
        {
            Bolum(content, "TUTMA MEKANİZMASI");
            IcerikSatir(content,
                $"• {ozet.ArdisikKayiplar.Count} kez üst üste kaybettiğinde küçük bir kazanç aldın.",
                C("#F7C1C1", .90f));
            IcerikSatir(content,
                "  Bu tesadüf değildi — sistem seni kaçırmamak için kırıntı attı.",
                new Color(1f, 1f, 1f, .55f), 12f);
            Spacer(content, 10f);
        }

        // ── MANUEL MÜDAHALELER (sadece yönetici modunda) ──
        if (KullaniciVerileri.YoneticiModu && ozet.ManuelMudahaleler.Count > 0)
        {
            Bolum(content, "MANUEL MÜDAHALELERİ");
            int carpanSayisi = 0, bonusSayisi = 0;
            foreach (var m in ozet.ManuelMudahaleler)
            {
                if (m.OlayTipi == OturumKayitcisi.OlayTipi_CarpanZorla) carpanSayisi++;
                if (m.OlayTipi == OturumKayitcisi.OlayTipi_BonusManuel)  bonusSayisi++;
            }
            if (carpanSayisi > 0)
                IcerikSatir(content, $"• Operatör {carpanSayisi} kez çarpanı zorla değiştirdi.", C("#FAC775", .85f));
            if (bonusSayisi > 0)
                IcerikSatir(content, $"• Bonus oyunu {bonusSayisi} kez manuel tetiklendi.", C("#FAC775", .85f));
            Spacer(content, 10f);
        }

        // ── ÖZET ──
        Bolum(content, "OTURUM ÖZETI");
        float netKayip = ozet.BaslangicBakiyesi - ozet.BitisBakiyesi;
        IcerikSatir(content, $"• Toplam spin: {ozet.ToplamSpin}", new Color(1f, 1f, 1f, .80f));
        IcerikSatir(content, $"• Başlangıç bakiye: {ozet.BaslangicBakiyesi:F0} TL", new Color(1f, 1f, 1f, .80f));
        IcerikSatir(content, $"• Bitiş bakiye: {ozet.BitisBakiyesi:F0} TL", new Color(1f, 1f, 1f, .80f));

        if (netKayip > 0)
            IcerikSatir(content, $"• Net kaybın: {netKayip:F0} TL", C("#E24B4A", 1f), 16f, bold: true);
        else if (netKayip < 0)
            IcerikSatir(content, $"• Net kazancın: {-netKayip:F0} TL", C("#5DCAA5", 1f), 16f, bold: true);
        else
            IcerikSatir(content, "• Başa baş çıktın.", new Color(1f, 1f, 1f, .80f));
    }

    // ── Senaryo açıklamaları ──
    static string SenaryoAcikla(string ad, int spin)
    {
        switch (ad)
        {
            case "hook":    return $"1–{spin} arası spinlerde YENİ AVLANAN modundaydın. (Seni bağlamak için yüksek kazandırma aktif edildi)";
            case "yontma":  return $"{spin}. spin civarında YONTMA moduna geçildi. (İlk verilen kazancı geri almak için)";
            case "tutma":   return $"{spin}. spin civarında TUTMA moduna geçildi. (Seni oyunda tutmak için)";
            case "koruma":  return $"{spin}. spin civarında KASA KORUMA modundaydın. (Kasayı korumak için ödeme minimuma indirildi)";
            case "normal":  return $"{spin}. spin civarında NORMAL moda geçildi.";
            default:        return $"{spin}. spin: {ad}";
        }
    }

    static string ParamAl(string detay, string anahtar)
    {
        // "senaryo=hook egilim=85 max=1000" formatından değer çeker
        int idx = detay.IndexOf(anahtar + "=");
        if (idx < 0) return "";
        int start = idx + anahtar.Length + 1;
        int end = detay.IndexOf(' ', start);
        return end < 0 ? detay.Substring(start) : detay.Substring(start, end - start);
    }

    // ── Aksiyon butonları ──
    void AnaEkranaDon()
    {
        OturumKayitcisi.SifirlaOturum();
        _instance = null;
        Destroy(gameObject);
        if (GameManager.I != null) GameManager.I.LoadScene(GIRIS_SAHNE);
        else SceneManager.LoadScene(GIRIS_SAHNE);
    }

    void TekrarOyna()
    {
        OturumKayitcisi.SifirlaOturum();
        _instance = null;
        Destroy(gameObject);
        if (GameManager.I != null) GameManager.I.LoadScene(OYUN_SAHNE);
        else SceneManager.LoadScene(OYUN_SAHNE);
    }

    // ── Animasyon ──
    static IEnumerator FadeIn(CanvasGroup cg)
    {
        float t = 0f;
        while (t < 1f) { t += Time.deltaTime / .25f; cg.alpha = Mathf.Clamp01(t); yield return null; }
        cg.alpha = 1f;
    }

    // ── UI yardımcıları ──
    static void Bolum(Transform p, string baslik)
    {
        var go = new GameObject("Bolum_" + baslik, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(p, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = baslik; t.fontSize = 13;
        t.color = C("#FAC775", .70f);
        t.fontStyle = FontStyles.Bold;
        t.characterSpacing = 2.5f;
        t.alignment = TextAlignmentOptions.Left; t.raycastTarget = false;
        go.AddComponent<LayoutElement>().preferredHeight = 22f;
    }

    static TextMeshProUGUI IcerikSatir(Transform p, string text, Color renk, float size = 13.5f,
                                        bool bold = false)
    {
        var go = new GameObject("Satir", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(p, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = renk;
        t.alignment = TextAlignmentOptions.TopLeft;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.lineSpacing = 4f; t.raycastTarget = false;
        if (bold) t.fontStyle = FontStyles.Bold;
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 24f; le.flexibleWidth = 1f;
        return t;
    }

    static TextMeshProUGUI AddTxt(Transform p, string text, float size, Color c,
                                   bool bold = false, float prefH = 24f)
    {
        var go = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(p, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = c;
        t.alignment = TextAlignmentOptions.Center;
        t.enableWordWrapping = false; t.raycastTarget = false;
        if (bold) t.fontStyle = FontStyles.Bold;
        go.AddComponent<LayoutElement>().preferredHeight = prefH;
        return t;
    }

    static void Spacer(Transform p, float h)
    {
        var go = new GameObject("Sp", typeof(RectTransform));
        go.transform.SetParent(p, false);
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = h; le.preferredHeight = h; le.flexibleHeight = 0f;
    }

    static void BuildBorder(Transform parent, Color col)
    {
        var brdGo = new GameObject("Border", typeof(RectTransform), typeof(Image));
        brdGo.transform.SetParent(parent, false);
        var bRt = brdGo.GetComponent<RectTransform>();
        bRt.anchorMin = Vector2.zero; bRt.anchorMax = Vector2.one;
        bRt.offsetMin = Vector2.zero; bRt.offsetMax = Vector2.zero;
        brdGo.GetComponent<Image>().color = col;
        brdGo.GetComponent<Image>().raycastTarget = false;

        var içGo = new GameObject("BorderIc", typeof(RectTransform), typeof(Image));
        içGo.transform.SetParent(parent, false);
        var iRt = içGo.GetComponent<RectTransform>();
        iRt.anchorMin = Vector2.zero; iRt.anchorMax = Vector2.one;
        iRt.offsetMin = new Vector2(1f, 1f); iRt.offsetMax = new Vector2(-1f, -1f);
        içGo.GetComponent<Image>().color = C("#1a0f2e");
        içGo.GetComponent<Image>().raycastTarget = false;
    }

    static void BuildBtn(Transform parent, string label, Color bg, Color txtC, float w, float h,
                          UnityEngine.Events.UnityAction onClick, Color? borderCol = null)
    {
        var go = new GameObject(label, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(w, h);
        var le = go.AddComponent<LayoutElement>();
        le.minWidth = w; le.preferredWidth = w; le.minHeight = h; le.preferredHeight = h;
        var img = go.AddComponent<Image>(); img.color = bg;

        if (borderCol.HasValue)
        {
            var brdGo = new GameObject("BtnBrd", typeof(RectTransform), typeof(Image));
            brdGo.transform.SetParent(go.transform, false);
            var bRt = brdGo.GetComponent<RectTransform>();
            bRt.anchorMin = Vector2.zero; bRt.anchorMax = Vector2.one;
            bRt.offsetMin = Vector2.zero; bRt.offsetMax = Vector2.zero;
            brdGo.GetComponent<Image>().color = borderCol.Value;
            brdGo.GetComponent<Image>().raycastTarget = false;

            var içGo = new GameObject("BtnIc", typeof(RectTransform), typeof(Image));
            içGo.transform.SetParent(go.transform, false);
            var iRt = içGo.GetComponent<RectTransform>();
            iRt.anchorMin = Vector2.zero; iRt.anchorMax = Vector2.one;
            iRt.offsetMin = new Vector2(1f, 1f); iRt.offsetMax = new Vector2(-1f, -1f);
            içGo.GetComponent<Image>().color = C("#1a0f2e");
            içGo.GetComponent<Image>().raycastTarget = false;
        }

        var txtGo = new GameObject("BtnTxt", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGo.transform.SetParent(go.transform, false);
        var tRt = txtGo.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
        tRt.offsetMin = Vector2.zero; tRt.offsetMax = Vector2.zero;
        var t = txtGo.GetComponent<TextMeshProUGUI>();
        t.text = label; t.fontSize = 14; t.color = txtC;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
    }

    void OnDestroy() { if (_instance == this) _instance = null; }
}
