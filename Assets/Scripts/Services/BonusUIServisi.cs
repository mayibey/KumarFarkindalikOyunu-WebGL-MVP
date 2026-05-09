using System;
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Bonus UI akışı: başlangıç/bitiş paneli (fade, TMP, ses) + satın alma onay paneli (göster/gizle, Evet/Hayır).
/// Eski BonusUIAkisServisi + BonusSatinAlmaAkisServisi birleşik.
/// </summary>
public class BonusUIServisi
{
    // --- Bonus başlangıç/bitiş ---
    private GameObject _bonusStartPanel;
    private AudioSource _bonusBellAudio;
    private CanvasGroup _bonusStartCanvasGroup;
    private TMP_Text _bonusStartTMP;
    private Func<int> _getBonusHakKalan;
    private float _bonusStartFadeTime;
    private float _bonusStartShowTime;

    private GameObject _bonusEndPanel;
    private Func<bool> _getBonusEndCloseRequested;
    private Action<bool> _setBonusEndCloseRequested;
    private AudioSource _bonusEndSfxSource;
    private AudioClip _bonusEndApplauseClip;
    private AudioClip _spinSonucKayipClip; // Bonus toplam 0 olduğunda alkış yerine bu çalar (The Price is Right Losing Horn)
    private CanvasGroup _bonusEndCanvasGroup;
    private TMP_Text _bonusEndTitleTMP;
    private TMP_Text _bonusEndWinTMP;
    private Func<int, string> _formatTL;
    private AudioSource _bonusEndMusicAudio;
    private Func<float> _getBonusEndAutoCloseSeconds;
    private Action<int> _setBonusEndCloseButtonText; // kalan saniye (5,4,3,2,1); -1 = sadece "TAMAM"

    // --- Bonus satın al onay ---
    private int _pendingCost;
    private Func<int> _getBakiye;
    private Func<int> _getBonusMaliyeti;
    private Func<bool> _getSpinCalisiyor;
    private Func<bool> _getBonusAktif;
    private Action<int> _showConfirmPanel;
    private Action _hideConfirmPanel;
    private Action<int> _onConfirmed;
    private Action<string> _setUyariText;

    // --- Setters: başlangıç/bitiş ---
    public void SetBonusStartPanel(GameObject panel) => _bonusStartPanel = panel;
    public void SetBonusBellAudio(AudioSource audio) => _bonusBellAudio = audio;
    public void SetBonusStartCanvasGroup(CanvasGroup cg) => _bonusStartCanvasGroup = cg;
    public void SetBonusStartTMP(TMP_Text tmp) => _bonusStartTMP = tmp;
    public void SetGetBonusHakKalan(Func<int> getter) => _getBonusHakKalan = getter;
    public void SetBonusStartFadeTime(float t) => _bonusStartFadeTime = t;
    public void SetBonusStartShowTime(float t) => _bonusStartShowTime = t;
    public void SetBonusEndPanel(GameObject panel) => _bonusEndPanel = panel;
    public void SetGetBonusEndCloseRequested(Func<bool> getter) => _getBonusEndCloseRequested = getter;
    public void SetSetBonusEndCloseRequested(Action<bool> setter) => _setBonusEndCloseRequested = setter;
    public void SetBonusEndSfx(AudioSource source, AudioClip clip) { _bonusEndSfxSource = source; _bonusEndApplauseClip = clip; }
    public void SetSpinSonucKayipClip(AudioClip clip) => _spinSonucKayipClip = clip;
    public void SetBonusEndCanvasGroup(CanvasGroup cg) => _bonusEndCanvasGroup = cg;
    public void SetBonusEndTitleTMP(TMP_Text tmp) => _bonusEndTitleTMP = tmp;
    public void SetBonusEndWinTMP(TMP_Text tmp) => _bonusEndWinTMP = tmp;
    public void SetFormatTL(Func<int, string> fn) => _formatTL = fn;
    public void SetBonusEndMusicAudio(AudioSource audio) => _bonusEndMusicAudio = audio;
    /// <summary>Otomatik spin devam edecekse panel X saniye sonra kendiliğinden kapanır (örn. 5f).</summary>
    public void SetGetBonusEndAutoCloseSeconds(Func<float> fn) => _getBonusEndAutoCloseSeconds = fn;
    /// <summary>Bonus bitiş panelindeki kapat butonu metnini günceller: kalan saniye (5,4,3,2,1) veya -1 = "TAMAM".</summary>
    public void SetBonusEndCloseButtonTextUpdater(Action<int> fn) => _setBonusEndCloseButtonText = fn;

    // --- Setters: satın al onay ---
    public void SetGetBakiye(Func<int> fn) => _getBakiye = fn;
    public void SetGetBonusMaliyeti(Func<int> fn) => _getBonusMaliyeti = fn;
    public void SetGetSpinCalisiyor(Func<bool> fn) => _getSpinCalisiyor = fn;
    public void SetGetBonusAktif(Func<bool> fn) => _getBonusAktif = fn;
    public void SetShowConfirmPanel(Action<int> fn) => _showConfirmPanel = fn;
    public void SetHideConfirmPanel(Action fn) => _hideConfirmPanel = fn;
    public void SetOnConfirmed(Action<int> fn) => _onConfirmed = fn;
    public void SetSetUyariText(Action<string> fn) => _setUyariText = fn;

    // --- Bonus başlangıç/bitiş ---
    public IEnumerator ShowBonusStartMessage()
    {
        if (_bonusStartPanel == null) yield break;
        _bonusStartPanel.SetActive(true);
        // Bonus paneli en önde görünsün; parent'ı öne taşımak tüm sayfayı kaplayan katmanları da yukarı çekebilir.
        _bonusStartPanel.transform.SetAsLastSibling();
        BonusStartGorunurlukVeCanvasAyarla(_bonusStartPanel);
        var panelRt = _bonusStartPanel.transform as RectTransform;
        // Sahnede bonusStartCanvasGroup atanmamış olabiliyor; panel üzerindeki CanvasGroup kullan.
        CanvasGroup bonusCg = _bonusStartCanvasGroup != null
            ? _bonusStartCanvasGroup
            : _bonusStartPanel.GetComponent<CanvasGroup>();
        if (_bonusBellAudio != null && _bonusBellAudio.clip != null)
            _bonusBellAudio.PlayOneShot(_bonusBellAudio.clip);
        else
            Debug.LogWarning("🔕 BONUS START: bonusBellAudio veya clip BOŞ!");
        if (bonusCg != null) bonusCg.alpha = 1f;
        int hak = _getBonusHakKalan != null ? _getBonusHakKalan() : 0;
        if (_bonusStartTMP != null) _bonusStartTMP.text = $"TEBRIKLER!\nBONUS HAKKI KAZANDIN\n{hak} FREE SPIN";
        // Sahne yapısı: kök tam ekran dimmer; asıl "kutu" genelde TMP veya ilk çocuk RectTransform.
        var startRt = BonusStartIcerikRtBul(panelRt);
        Vector3 startScale = startRt != null ? startRt.localScale : Vector3.one;
        Vector2 origSizeDelta = startRt != null ? startRt.sizeDelta : Vector2.zero;
        const float cerceveBoyutCarpani = 0.62f;
        bool icerikKutuAyarla = startRt != null && startRt != panelRt;
        if (icerikKutuAyarla)
            startRt.sizeDelta = origSizeDelta * cerceveBoyutCarpani;
        Vector3 hedefScale = startScale * 0.72f;
        if (startRt != null)
            startRt.localScale = hedefScale * 0.72f;
        float girisSuresi = Mathf.Clamp(_bonusStartFadeTime * 1.75f, 0.36f, 0.52f);
        float tg = 0f;
        while (tg < girisSuresi)
        {
            tg += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(tg / girisSuresi);
            float eased = 1f - Mathf.Pow(1f - p, 2.8f);
            if (bonusCg != null) bonusCg.alpha = 1f;
            if (startRt != null)
                startRt.localScale = Vector3.LerpUnclamped(hedefScale * 0.72f, hedefScale * 1.05f, eased);
            yield return null;
        }
        if (bonusCg != null) bonusCg.alpha = 1f;
        if (startRt != null) startRt.localScale = hedefScale * 1.02f;
        // En az 2 sn görünsün; zil uzunsa ikisinin büyüğü (ses bitmeden kapanmasın).
        float toplamGosterim = Mathf.Max(2f, _bonusStartShowTime);
        if (_bonusBellAudio != null && _bonusBellAudio.clip != null)
            toplamGosterim = Mathf.Max(toplamGosterim, _bonusBellAudio.clip.length);
        float nabizSuresi = Mathf.Max(0.15f, toplamGosterim - girisSuresi);
        float bw = 0f;
        while (bw < nabizSuresi)
        {
            bw += Time.unscaledDeltaTime;
            if (startRt != null)
            {
                float pulse = 1f + Mathf.Sin(bw * 5.5f) * 0.045f;
                startRt.localScale = hedefScale * 1.02f * pulse;
            }
            yield return null;
        }
        if (bonusCg != null)
        {
            float t = 0f;
            while (t < _bonusStartFadeTime)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / _bonusStartFadeTime);
                bonusCg.alpha = Mathf.Lerp(1f, 0f, p);
                if (startRt != null) startRt.localScale = Vector3.LerpUnclamped(hedefScale * 1.02f, hedefScale * 0.88f, p);
                yield return null;
            }
            bonusCg.alpha = 0f;
        }
        if (startRt != null)
        {
            startRt.localScale = startScale;
            if (icerikKutuAyarla)
                startRt.sizeDelta = origSizeDelta;
        }
        _bonusStartPanel.SetActive(false);
    }

    /// <summary>Kök dimmer + TMP opaklığı, CanvasGroup, üstte ayrı Canvas (sorting). Sahnede CanvasGroup referansı boş olsa da çalışır.</summary>
    private static void BonusStartGorunurlukVeCanvasAyarla(GameObject panel)
    {
        if (panel == null) return;
        var panelRt = panel.transform as RectTransform;
        if (panelRt != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRt);
        }

        BonusStartMaskeVeIcerikYapistir(panel);

        var kokGorsel = panel.GetComponent<Image>();
        if (kokGorsel != null)
        {
            if (kokGorsel.sprite != null)
                kokGorsel.color = Color.white;
            else if (panel.transform.Find("YuvarlakMaskeKutu") != null)
                kokGorsel.color = new Color(0f, 0f, 0f, 0f);
            else
                kokGorsel.color = new Color(0.08f, 0.09f, 0.12f, 1f);
        }

        var tmps = panel.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < tmps.Length; i++)
        {
            if (tmps[i] == null) continue;
            tmps[i].alpha = 1f;
            var tc = tmps[i].color;
            tc.a = 1f;
            tmps[i].color = tc;
            tmps[i].ForceMeshUpdate();
        }

        var graphics = panel.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            var g = graphics[i];
            if (g == null || g.gameObject == panel || g is TMP_Text) continue;
            var c = g.color;
            if (g is Image img && img.sprite != null && img.gameObject != panel)
            {
                c.r = Mathf.Min(1f, c.r * 1.02f);
                c.g = Mathf.Min(1f, c.g * 1.02f);
                c.b = Mathf.Min(1f, c.b * 1.02f);
            }
            c.a = 1f;
            g.color = c;
        }

        BonusStartOvalKenarligiEkle(panel);

        Canvas yerel = panel.GetComponent<Canvas>();
        if (yerel == null)
        {
            yerel = panel.AddComponent<Canvas>();
            if (panel.GetComponent<GraphicRaycaster>() == null)
                panel.AddComponent<GraphicRaycaster>();
        }
        yerel.overrideSorting = true;
        yerel.sortingOrder = 32600;

        var icCanvases = panel.GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < icCanvases.Length; i++)
        {
            if (icCanvases[i] == null) continue;
            icCanvases[i].overrideSorting = true;
            if (icCanvases[i].sortingOrder < 32600)
                icCanvases[i].sortingOrder = 32600;
        }

        var cg = panel.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }
    }

    /// <summary>Inspector'daki kök Source Image'ı yuvarlak maskenin içine taşır; kök Image şeffaf kalır (çerçeve üstte).</summary>
    private static void BonusStartMaskeVeIcerikYapistir(GameObject panel)
    {
        if (panel == null) return;
        var kok = panel.GetComponent<Image>();
        if (kok == null) return;

        const string maskeAd = "YuvarlakMaskeKutu";
        const string icerikAd = "BonusIcerikGorsel";

        Sprite kaynak = kok.sprite;
        Transform maskeTr = panel.transform.Find(maskeAd);
        if (kaynak == null && maskeTr != null)
        {
            var eskiIcerik = maskeTr.Find(icerikAd)?.GetComponent<Image>();
            if (eskiIcerik != null && eskiIcerik.sprite != null)
                kaynak = eskiIcerik.sprite;
        }
        if (kaynak == null) return;

        if (maskeTr == null)
        {
            var maskeGo = new GameObject(maskeAd, typeof(RectTransform), typeof(Image), typeof(Mask));
            maskeTr = maskeGo.transform;
            maskeTr.SetParent(panel.transform, false);
        }

        var maskeRt = maskeTr as RectTransform;
        maskeRt.anchorMin = Vector2.zero;
        maskeRt.anchorMax = Vector2.one;
        // Küçük inset: resim çerçeve iç ağzına yakın dolsun (geniş bej boşluk kalmasın).
        const float icBosluk = 2.5f;
        maskeRt.offsetMin = new Vector2(icBosluk, icBosluk);
        maskeRt.offsetMax = new Vector2(-icBosluk, -icBosluk);
        maskeRt.localScale = Vector3.one;

        var maskeImg = maskeTr.GetComponent<Image>();
        maskeImg.sprite = YuvarlakUICerceveSprite.AlDoluYuvarlakMaskeSprite();
        maskeImg.type = Image.Type.Sliced;
        maskeImg.color = Color.white;
        maskeImg.raycastTarget = false;

        var maskeBilesen = maskeTr.GetComponent<Mask>();
        maskeBilesen.showMaskGraphic = false;

        Transform icerikTr = maskeTr.Find(icerikAd);
        if (icerikTr == null)
        {
            var icerikGo = new GameObject(icerikAd, typeof(RectTransform), typeof(Image));
            icerikTr = icerikGo.transform;
            icerikTr.SetParent(maskeTr, false);
        }

        var icerikRt = icerikTr as RectTransform;
        icerikRt.anchorMin = Vector2.zero;
        icerikRt.anchorMax = Vector2.one;
        icerikRt.offsetMin = Vector2.zero;
        icerikRt.offsetMax = Vector2.zero;
        icerikRt.localScale = Vector3.one;

        var icerikImg = icerikTr.GetComponent<Image>();
        if (kok.sprite != null)
        {
            icerikImg.sprite = kok.sprite;
            icerikImg.type = kok.type;
            icerikImg.preserveAspect = kok.preserveAspect;
            kok.sprite = null;
        }
        else if (icerikImg.sprite == null && kaynak != null)
        {
            icerikImg.sprite = kaynak;
            icerikImg.type = Image.Type.Simple;
            icerikImg.preserveAspect = false;
        }
        icerikImg.color = Color.white;
        icerikImg.raycastTarget = false;

        kok.color = new Color(0f, 0f, 0f, 0f);
        kok.raycastTarget = true;

        maskeTr.SetSiblingIndex(0);
    }

    /// <summary>Yuvarlatılmış oval/çerçeve kenarlığı (üstte); kök Image ile çakışmaz.</summary>
    private static void BonusStartOvalKenarligiEkle(GameObject panel)
    {
        if (panel == null) return;
        const string ad = "OvalKenarlik";
        Transform eski = panel.transform.Find(ad);
        GameObject go = eski != null ? eski.gameObject : new GameObject(ad, typeof(RectTransform), typeof(Image));
        if (eski == null)
            go.transform.SetParent(panel.transform, false);
        go.transform.SetAsLastSibling();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-3f, -3f);
        rt.offsetMax = new Vector2(3f, 3f);
        rt.localScale = Vector3.one;
        var img = go.GetComponent<Image>();
        img.sprite = YuvarlakUICerceveSprite.AlVeyaOlustur();
        img.type = Image.Type.Sliced;
        img.raycastTarget = false;
        img.preserveAspect = false;
        img.color = Color.white;
    }

    /// <summary>Bonus mesaj kutusu: önce TMP, yoksa ilk çocuk; sahne köküyle aynıysa kökü döndürür.</summary>
    private static RectTransform BonusStartIcerikRtBul(RectTransform panelRt)
    {
        if (panelRt == null) return null;
        if (panelRt.Find("OvalKenarlik") != null || panelRt.Find("YuvarlakMaskeKutu") != null)
            return panelRt;
        var tmp = panelRt.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null)
            return tmp.rectTransform;
        if (panelRt.childCount > 0)
        {
            var c0 = panelRt.GetChild(0) as RectTransform;
            if (c0 != null) return c0;
        }
        return panelRt;
    }

    /// <summary>Bonus bitiş paneli hedef boyutu (sahneden büyükse runtime’da küçültülür).</summary>
    private const float BonusEndPanelGenislik = 760f;
    private const float BonusEndPanelYukseklik = 518f;

    private static void BonusEndPanelBoyutunuUygula(RectTransform panelRt)
    {
        if (panelRt == null) return;
        panelRt.sizeDelta = new Vector2(BonusEndPanelGenislik, BonusEndPanelYukseklik);
    }

    /// <summary>Bonus bitiş: oval/ölçek animasyonu kullanılmaz (oval son sibling olunca metni kapatıyordu). Sadece canvas sırası.</summary>
    private static void BonusEndCanvasUsteAl(GameObject panel)
    {
        if (panel == null) return;
        Canvas yerel = panel.GetComponent<Canvas>();
        if (yerel == null)
        {
            yerel = panel.AddComponent<Canvas>();
            if (panel.GetComponent<GraphicRaycaster>() == null)
                panel.AddComponent<GraphicRaycaster>();
        }
        yerel.overrideSorting = true;
        yerel.sortingOrder = 32600;
        foreach (var ic in panel.GetComponentsInChildren<Canvas>(true))
        {
            if (ic == null) continue;
            ic.overrideSorting = true;
            if (ic.sortingOrder < 32600)
                ic.sortingOrder = 32600;
        }
    }

    private static Sprite BonusEndBuiltin9SliceDene()
    {
        string[] yollar = { "UI/Skin/Background.psd", "UI/Skin/UISprite.psd", "UI/Skin/Knob.psd" };
        for (int i = 0; i < yollar.Length; i++)
        {
            var s = Resources.GetBuiltinResource<Sprite>(yollar[i]);
            if (s != null) return s;
        }
        return null;
    }

    /// <summary>Slot hissi: koyu mor kart, üst/alt ince altın şerit; dekor her zaman en altta (SetAsFirstSibling).</summary>
    private static void BonusEndSlotTemasiArkaPlan(GameObject panel)
    {
        if (panel == null) return;
        var kokImg = panel.GetComponent<Image>();
        if (kokImg != null)
        {
            kokImg.type = Image.Type.Sliced;
            if (kokImg.sprite == null)
                kokImg.sprite = BonusEndBuiltin9SliceDene();
            kokImg.color = new Color(0.06f, 0.14f, 0.22f, 1f);
            kokImg.raycastTarget = true;
        }

        const string ust = "BonusEnd_RuntimeUstSerit";
        const string alt = "BonusEnd_RuntimeAltSerit";
        Sprite dilim = kokImg != null && kokImg.sprite != null ? kokImg.sprite : BonusEndBuiltin9SliceDene();

        void SeritOlustur(string ad, bool ustte)
        {
            if (dilim == null) return;
            if (panel.transform.Find(ad) != null) return;
            var go = new GameObject(ad, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(panel.transform, false);
            go.transform.SetAsFirstSibling();
            var rt = (RectTransform)go.transform;
            rt.anchorMin = ustte ? new Vector2(0f, 1f) : new Vector2(0f, 0f);
            rt.anchorMax = ustte ? new Vector2(1f, 1f) : new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, ustte ? 1f : 0f);
            rt.sizeDelta = new Vector2(0f, 8f);
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            if (dilim != null)
            {
                img.sprite = dilim;
                img.type = Image.Type.Sliced;
            }
            img.raycastTarget = false;
            img.color = ustte
                ? new Color(0.45f, 0.88f, 0.82f, 0.9f)
                : new Color(0.2f, 0.45f, 0.42f, 0.5f);
        }
        SeritOlustur(ust, true);
        SeritOlustur(alt, false);
    }

    /// <summary>Sahnedeki üst üste binen rect'leri her açılışta dikey sıraya sokar; rich text yok (satır kayması olmaz).</summary>
    private void BonusEndMetinVeButonYerlesimi(RectTransform panelRt, int bonusToplamKazanc)
    {
        if (panelRt == null) return;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRt);

        float w = panelRt.rect.width > 2f ? panelRt.rect.width : panelRt.sizeDelta.x;
        float h = panelRt.rect.height > 2f ? panelRt.rect.height : panelRt.sizeDelta.y;
        float yTitle = Mathf.Clamp(h * 0.26f, 95f, 175f);
        float yWin = Mathf.Clamp(-h * 0.05f, -45f, -15f);
        float yBtn = Mathf.Clamp(-h * 0.30f, -220f, -160f);
        float textGenislik = Mathf.Clamp(w - 48f, 300f, BonusEndPanelGenislik - 40f);

        string tlStr = _formatTL != null ? _formatTL(bonusToplamKazanc) : bonusToplamKazanc.ToString("N0");

        if (_bonusEndTitleTMP != null)
        {
            var rt = _bonusEndTitleTMP.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(textGenislik, 100f);
            rt.anchoredPosition = new Vector2(0f, yTitle);
            rt.localScale = Vector3.one;
            _bonusEndTitleTMP.text = "BONUS OYUN BİTTİ";
            _bonusEndTitleTMP.fontSize = 50;
            _bonusEndTitleTMP.fontStyle = FontStyles.Bold;
            _bonusEndTitleTMP.lineSpacing = -4f;
            _bonusEndTitleTMP.enableWordWrapping = true;
            _bonusEndTitleTMP.overflowMode = TextOverflowModes.Overflow;
            _bonusEndTitleTMP.horizontalAlignment = HorizontalAlignmentOptions.Center;
            _bonusEndTitleTMP.verticalAlignment = VerticalAlignmentOptions.Middle;
            _bonusEndTitleTMP.color = new Color(1f, 0.93f, 0.72f, 1f);
            _bonusEndTitleTMP.alpha = 1f;
            _bonusEndTitleTMP.ForceMeshUpdate();
        }

        if (_bonusEndWinTMP != null)
        {
            var rt = _bonusEndWinTMP.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(textGenislik, 150f);
            rt.anchoredPosition = new Vector2(0f, yWin);
            rt.localScale = Vector3.one;
            _bonusEndWinTMP.text = "Bu bonus turunda kazancın\n" + tlStr;
            _bonusEndWinTMP.fontSize = 34;
            _bonusEndWinTMP.fontStyle = FontStyles.Normal;
            _bonusEndWinTMP.lineSpacing = 14f;
            _bonusEndWinTMP.enableWordWrapping = true;
            _bonusEndWinTMP.overflowMode = TextOverflowModes.Overflow;
            _bonusEndWinTMP.horizontalAlignment = HorizontalAlignmentOptions.Center;
            _bonusEndWinTMP.verticalAlignment = VerticalAlignmentOptions.Middle;
            _bonusEndWinTMP.color = new Color(0.78f, 0.9f, 1f, 1f);
            _bonusEndWinTMP.alpha = 1f;
            _bonusEndWinTMP.ForceMeshUpdate();
        }

        Transform btnTr = panelRt.Find("BonusEndCloseButton");
        if (btnTr != null)
        {
            var rt = (RectTransform)btnTr;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, yBtn);
            rt.localScale = Vector3.one;
        }
    }

    public IEnumerator ShowBonusEndMessage(int bonusToplamKazanc)
    {
        // Eski sahne UI panel akışı (BonusEndPanel SetActive + fade + autoClose) DEVRE DIŞI.
        // Yeni: AnlaticiSeritKopru.BonusBitisGoster ile modern DOM popup açılır.
        // Alkış sesi BURADA C# tarafında PlayOneShot ile çalmaya devam eder (DOM popup ile paralel).
        // Sahne paneli (_bonusEndPanel) prefab olarak kalır ama SetActive(true) çağrılmaz.

        // Defansif: eski panel yanlışlıkla aktifse kapat
        if (_bonusEndPanel != null && _bonusEndPanel.activeSelf)
            _bonusEndPanel.SetActive(false);

        // Bonus toplam 0 → "The Price is Right Losing Horn" (alkış ÇALMAZ).
        // Bonus toplam > 0 → alkış (mevcut davranış).
        if (bonusToplamKazanc == 0)
        {
            if (_bonusEndSfxSource != null && _spinSonucKayipClip != null)
            {
                _bonusEndSfxSource.PlayOneShot(_spinSonucKayipClip);
                Debug.Log("[KayipHorn] Bonus toplam 0 — horn çaldı, alkış atlandı");
            }
            else
                Debug.LogWarning("[KayipHorn] Bonus 0: bonusEndSfxSource veya spinSonucKayipClip boş!");
        }
        else if (_bonusEndSfxSource != null && _bonusEndApplauseClip != null)
        {
            _bonusEndSfxSource.PlayOneShot(_bonusEndApplauseClip);
        }
        else
        {
            Debug.LogWarning("👏 Bonus End: bonusEndSfxSource veya bonusEndApplauseClip boş!");
        }

        // Modern DOM popup'ı aç (alkış sesi ile senkron başlar)
        AnlaticiSeritKopru.BonusBitisGoster(bonusToplamKazanc);

        // Kullanıcı TAMAM tıklayana kadar bekle (JS → SendMessage('BonusBitisOnayla') → flag true)
        // Editor'da BonusBitisGoster fallback flag'i true set eder → coroutine anında devam eder.
        yield return new WaitUntil(() => AnlaticiSeritKopru.BonusBitisOnaylandi);

        // Bonus müziğini durdur (mevcut davranış)
        if (_bonusEndMusicAudio != null) _bonusEndMusicAudio.Stop();
    }

    // --- Bonus satın al onay ---
    public void BonusSatinAlRequested()
    {
        if (_getSpinCalisiyor != null && _getSpinCalisiyor()) return;
        if (_getBonusAktif != null && _getBonusAktif()) return;
        int cost = _getBonusMaliyeti != null ? _getBonusMaliyeti() : 0;
        int bakiye = _getBakiye != null ? _getBakiye() : 0;
        if (bakiye < cost) { _setUyariText?.Invoke($"Yetersiz bakiye. Maliyet: {cost} TL"); return; }
        _pendingCost = cost;
        _showConfirmPanel?.Invoke(cost);
    }

    public void ShowBonusBuyConfirmPanel(int cost) { _pendingCost = cost; _showConfirmPanel?.Invoke(cost); }
    public void HideBonusBuyConfirmPanel() { _hideConfirmPanel?.Invoke(); _pendingCost = 0; }

    public void OnYes()
    {
        if (_pendingCost <= 0) return;
        int cost = _pendingCost;
        _hideConfirmPanel?.Invoke();
        _pendingCost = 0;
        if (_getBakiye != null && _getBakiye() < cost) { _setUyariText?.Invoke($"Yetersiz bakiye. Maliyet: {cost} TL"); return; }
        _onConfirmed?.Invoke(cost);
    }

    public void OnNo() { _hideConfirmPanel?.Invoke(); _pendingCost = 0; }
    public int GetPendingCost() => _pendingCost;
    public void ClearPendingCost() => _pendingCost = 0;
}
