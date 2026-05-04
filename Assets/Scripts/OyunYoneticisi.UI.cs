using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public partial class OyunYoneticisi
{
    private void CloseMoneyPanels()
    {
        if (bakiyeYuklePanel != null) bakiyeYuklePanel.SetActive(false);
        if (paraCekPanel != null) paraCekPanel.SetActive(false);
    }


    // Inspector OnClick için PUBLIC wrapper'lar (Unity sadece public metotları listeler)
    public void ParaCek_OnayButton()
    {
        _ekonomiServisi?.OnParaCekOnay();
    }


    public void ParaCek_IptalButton()
    {
        _uiServisi?.HideParaCekPanel();
    }


    public void BakiyeYukle_OnayButton()
    {
        bool uygulandi = _ekonomiServisi != null && _ekonomiServisi.OnBakiyeYukleOnay();
        if (uygulandi)
            StartCoroutine(BakiyeYukleButonKilidiCoroutine(2f));
    }


    private IEnumerator BakiyeYukleButonKilidiCoroutine(float sure)
    {
        if (bakiyeYukleOnayButon != null) bakiyeYukleOnayButon.interactable = false;
        if (bakiyeYukleButon != null) bakiyeYukleButon.interactable = false;
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, sure));
        if (bakiyeYukleUyariText != null)
        {
            int kalanHak = _ekonomiServisi != null ? _ekonomiServisi.GetBakiyeYuklemeKalanHak() : 0;
            if (kalanHak <= 0)
                bakiyeYukleUyariText.text = "Bakiye yükleme hakkın kalmadı.";
            else
                bakiyeYukleUyariText.text = $"20.000 TL yükleme yapmak ister misin? (Kalan hak: {kalanHak})";
        }
        if (bakiyeYukleOnayButon != null && bakiyeYuklePanel != null && bakiyeYuklePanel.activeInHierarchy)
            bakiyeYukleOnayButon.interactable = true;
        if (bakiyeYukleButon != null)
            bakiyeYukleButon.interactable = !spinCalisiyor && !bonusAktif;
    }


    public void BakiyeYukle_IptalButton()
    {
        _uiServisi?.HideBakiyeYuklePanel();
    }


    public void ShowParaCekPanel()
    {
        _uiServisi?.ResolveMoneyUIRefsIfMissing();
        _uiServisi?.WireParaCekUI();

        if (paraCekUyariText != null) paraCekUyariText.text = "";

        _uiServisi?.CloseMoneyPanels();
        Debug.Log("[UI] ParaCekButon tıklandı. Panel=" + (paraCekPanel != null ? paraCekPanel.name : "NULL"));

        if (paraCekUyariText != null) paraCekUyariText.text = "";
        if (paraCekInput != null) paraCekInput.text = "";

        if (paraCekPanel != null)
            paraCekPanel.SetActive(true);
        SenaryoYoneticisi.I?.LogEkle(SenaryoOlayKaydi.OlayTipi_ParaCekEkraniAcildi, "Para çek ekranı açıldı. Mevcut bakiye: " + (_ekonomiServisi != null ? _ekonomiServisi.Bakiye.ToString("N0") : "—") + " TL.");
    }


    public void HideParaCekPanel()
    {
        if (paraCekPanel != null)
            paraCekPanel.SetActive(false);
    }

    private void InspectorBakiyesiniYansit()

    {
        if (bakiyeText != null)
            bakiyeText.text = "Bakiye: " + OyunFormatServisi.FormatTL(inspectorBakiyeTL);

        if (Application.isPlaying && _ekonomiServisi != null)
            _ekonomiServisi.SetBakiye(inspectorBakiyeTL);
    }


    private static void SetAnchors(GameObject go, Vector2 min, Vector2 max)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = min; rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static void AddBtnLabel(GameObject btnGo, string label, float fontSize)
    {
        var txtGo = new GameObject("Lbl");
        txtGo.transform.SetParent(btnGo.transform, false);
        var t = txtGo.AddComponent<TMPro.TextMeshProUGUI>();
        t.text = label;
        t.alignment = TMPro.TextAlignmentOptions.Center;
        t.fontSize = fontSize;
        t.color = Color.white;
        var rt = txtGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }


    /// <summary>Tek küme (8–9 / 10–11 / 12+) için TumbleAyarlari katsayıları × bahis TL özetini loglar; sembol sırası Inspector sembolSpriteListesi ile aynı index.</summary>
    private void SenaryoDropdownYazilariniBuyut()

    {
        if (_senaryoPresetDropdown == null) return;

        int secili = Mathf.Max(10, senaryoDropdownSeciliYaziBoyutu);
        int liste = Mathf.Max(10, senaryoDropdownListeYaziBoyutu);

        if (_senaryoPresetDropdown.captionText != null)
            _senaryoPresetDropdown.captionText.fontSize = secili;

        if (_senaryoPresetDropdown.itemText != null)
            _senaryoPresetDropdown.itemText.fontSize = liste;

        RectTransform template = _senaryoPresetDropdown.template;
        if (template == null) return;
        if (senaryoDropdownGenislikIcerigeGore)
        {
            TMP_Text olcumText = _senaryoPresetDropdown.itemText != null
                ? _senaryoPresetDropdown.itemText
                : _senaryoPresetDropdown.captionText;
            float maxMetin = 0f;
            if (olcumText != null)
            {
                bool oncekiAutoSize = olcumText.enableAutoSizing;
                float oncekiFont = olcumText.fontSize;
                float oncekiMin = olcumText.fontSizeMin;
                float oncekiMax = olcumText.fontSizeMax;
                olcumText.enableAutoSizing = false;
                olcumText.fontSize = liste;
                olcumText.fontSizeMin = liste;
                olcumText.fontSizeMax = liste;

                for (int i = 0; i < _adminSenaryoPresetleri.Length; i++)
                {
                    string yazi = _adminSenaryoPresetleri[i].Ad ?? string.Empty;
                    Vector2 pref = olcumText.GetPreferredValues(yazi, 9999f, 0f);
                    if (pref.x > maxMetin) maxMetin = pref.x;
                }

                olcumText.enableAutoSizing = oncekiAutoSize;
                olcumText.fontSize = oncekiFont;
                olcumText.fontSizeMin = oncekiMin;
                olcumText.fontSizeMax = oncekiMax;
            }
            else
            {
                maxMetin = 360f;
            }

            // Sol/sağ boşluk + ok alanı için sıkı pay.
            float hedefW = Mathf.Clamp(maxMetin + 42f, 280f, 620f);
            Vector2 tplW = template.sizeDelta;
            tplW.x = hedefW;
            template.sizeDelta = tplW;
        }
        else if (senaryoDropdownListeGenislik > 1f)
        {
            Vector2 tplW = template.sizeDelta;
            tplW.x = senaryoDropdownListeGenislik;
            template.sizeDelta = tplW;
        }
        Vector2 tplSize = template.sizeDelta;
        tplSize.y = Mathf.Max(120f, senaryoDropdownListePanelYukseklik);
        template.sizeDelta = tplSize;

        var viewport = template.Find("Viewport") as RectTransform;
        if (viewport != null)
        {
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;
        }

        var itemRt = template.Find("Viewport/Content/Item") as RectTransform;
        if (itemRt != null)
        {
            Vector2 itemSize = itemRt.sizeDelta;
            itemSize.y = Mathf.Max(36f, senaryoDropdownSatirYukseklik);
            itemRt.sizeDelta = itemSize;
            var itemLe = itemRt.GetComponent<LayoutElement>();
            if (itemLe != null)
            {
                itemLe.minHeight = itemSize.y;
                itemLe.preferredHeight = itemSize.y;
                itemLe.flexibleHeight = 0f;
            }
        }

        var contentRt = template.Find("Viewport/Content") as RectTransform;
        if (contentRt != null)
        {
            var vlg = contentRt.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.childControlHeight = true;
                vlg.childForceExpandHeight = false;
                vlg.childControlWidth = true;
                // 5 satırın rahat sığması için aradaki boşlukları minimumda tut.
                vlg.spacing = 0f;
                vlg.padding.top = 0;
                vlg.padding.bottom = 0;
            }
        }

        var tumYazilar = template.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < tumYazilar.Length; i++)
        {
            TMP_Text t = tumYazilar[i];
            if (t == null) continue;
            t.fontSize = liste;
            t.enableAutoSizing = true;
            t.fontSizeMax = liste;
            t.fontSizeMin = Mathf.Max(16, liste - 8);
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Ellipsis;
        }
    }

    private void BahisGorselKilidiniHazirla()

    {
        _bahisGorselKilidiHazir = false;
        _bahisGorselRtKilidi = null;
        if (!bahisGorselKilidiAktif)
            return;

        GameObject hedef = null;
        if (!string.IsNullOrWhiteSpace(bahisGorselNesneAdi))
            hedef = GameObject.Find(bahisGorselNesneAdi);

        if (hedef == null && bahisText != null)
            hedef = bahisText.transform.parent != null ? bahisText.transform.parent.gameObject : bahisText.gameObject;
        if (hedef == null)
            return;

        _bahisGorselRtKilidi = hedef.GetComponent<RectTransform>();
        if (_bahisGorselRtKilidi == null)
            return;

        _bahisGorselAnchorMin = _bahisGorselRtKilidi.anchorMin;
        _bahisGorselAnchorMax = _bahisGorselRtKilidi.anchorMax;
        _bahisGorselPivot = _bahisGorselRtKilidi.pivot;
        _bahisGorselAnchoredPos = _bahisGorselRtKilidi.anchoredPosition;
        _bahisGorselSizeDelta = _bahisGorselRtKilidi.sizeDelta;
        _bahisGorselLocalScale = _bahisGorselRtKilidi.localScale;
        _bahisGorselLocalRotation = _bahisGorselRtKilidi.localRotation;
        _bahisGorselKilidiHazir = true;
    }

    private void BahisGorselKilidiniUygula()

    {
        if (!bahisGorselKilidiAktif) return;
        if (!_bahisGorselKilidiHazir || _bahisGorselRtKilidi == null)
        {
            BahisGorselKilidiniHazirla();
            if (!_bahisGorselKilidiHazir || _bahisGorselRtKilidi == null) return;
        }

        _bahisGorselRtKilidi.anchorMin = _bahisGorselAnchorMin;
        _bahisGorselRtKilidi.anchorMax = _bahisGorselAnchorMax;
        _bahisGorselRtKilidi.pivot = _bahisGorselPivot;
        _bahisGorselRtKilidi.anchoredPosition = _bahisGorselAnchoredPos;
        _bahisGorselRtKilidi.sizeDelta = _bahisGorselSizeDelta;
        _bahisGorselRtKilidi.localRotation = _bahisGorselLocalRotation;
        _bahisGorselRtKilidi.localScale = _bahisGorselLocalScale;
    }

    /// <summary>Play modunda, ekonomi hazırken Inspector'daki değeri anında uygular. (Bileşen sağ tık menüsünden de çağrılabilir.)</summary>
    [ContextMenu("Bakiyeyi Inspector (inspectorBakiyeTL) Değerine Uygula")]
    public void InspectorBakiyesiniSimdiUygula()

    {
        if (_ekonomiServisi == null && Application.isPlaying)
        {
            Debug.LogWarning("[OyunYoneticisi] Ekonomi servisi henüz yok; Play modunda oyun başladıktan sonra dene.");
            return;
        }
        InspectorBakiyesiniYansit();
    }

    private void EnsureNormalSpinSonucPopup()

    {
        if (_normalSpinSonucPopup != null) return;

        var canvasGo = new GameObject("NormalSpinSonucPopupCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 4200;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var panel = new GameObject("Panel");
        panel.transform.SetParent(canvasGo.transform, false);
        var panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(760f, 300f);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.03f, 0.06f, 0.12f, 0.86f);

        var baslikGo = new GameObject("Baslik");
        baslikGo.transform.SetParent(panel.transform, false);
        var baslikRt = baslikGo.AddComponent<RectTransform>();
        baslikRt.anchorMin = new Vector2(0.08f, 0.68f);
        baslikRt.anchorMax = new Vector2(0.92f, 0.94f);
        baslikRt.offsetMin = Vector2.zero;
        baslikRt.offsetMax = Vector2.zero;
        _normalSpinSonucBaslikTxt = baslikGo.AddComponent<TextMeshProUGUI>();
        _normalSpinSonucBaslikTxt.alignment = TextAlignmentOptions.Center;
        _normalSpinSonucBaslikTxt.fontSize = 58f;
        _normalSpinSonucBaslikTxt.enableWordWrapping = false;

        var icerikGo = new GameObject("Icerik");
        icerikGo.transform.SetParent(panel.transform, false);
        var icerikRt = icerikGo.AddComponent<RectTransform>();
        icerikRt.anchorMin = new Vector2(0.08f, 0.14f);
        icerikRt.anchorMax = new Vector2(0.92f, 0.66f);
        icerikRt.offsetMin = Vector2.zero;
        icerikRt.offsetMax = Vector2.zero;
        _normalSpinSonucIcerikTxt = icerikGo.AddComponent<TextMeshProUGUI>();
        _normalSpinSonucIcerikTxt.alignment = TextAlignmentOptions.Center;
        _normalSpinSonucIcerikTxt.fontSize = 38f;
        _normalSpinSonucIcerikTxt.lineSpacing = 8f;

        _normalSpinSonucPopup = canvasGo;
        _normalSpinSonucPopup.SetActive(false);
    }

    private IEnumerator ShowNormalSpinSonucPopup(int odenen, int bahis)
    {
        // Normal Oyun modunda (senaryo kapalı): kayıp ve küçük kazanç için popup yok.
        if (!_senaryoPresetAktif)
        {
            // Öncül filtre BigWin'in yeni eşiği (2x) ile uyumlu; içerideki KazancSeviyesiHesapla doğru kategoriye yönlendirir.
            bool buyukKazancNO = bahis > 0 && odenen >= bahis * 2;
            if (buyukKazancNO)
                winFeedbackUI?.ShowWin(odenen, bahis);
            yield break;
        }

        if (_normalSpinSonucPopupCalisiyor)
            yield break;
        _normalSpinSonucPopupCalisiyor = true;

        EnsureNormalSpinSonucPopup();
        if (_normalSpinSonucPopup == null || _normalSpinSonucBaslikTxt == null || _normalSpinSonucIcerikTxt == null)
        {
            _normalSpinSonucPopupCalisiyor = false;
            yield break;
        }

        if (!_normalSpinSonucSesiBuSpinCaldi)
        {
            PlayNormalSpinSonucSesi(odenen, bahis);
            _normalSpinSonucSesiBuSpinCaldi = true;
        }

        int net = odenen - bahis;
        bool kazanc = net > 0;
        bool kayip = net < 0;
        _normalSpinSonucBaslikTxt.text = kazanc ? "Tebrikler" : (kayip ? "X   X   X" : "Tur Özeti");
        _normalSpinSonucBaslikTxt.color = kazanc ? new Color(1f, 0.87f, 0.2f, 1f) : (kayip ? new Color(1f, 0.45f, 0.45f, 1f) : Color.white);
        if (kazanc)
        {
            _normalSpinSonucIcerikTxt.text =
                $"BAHİS: {OyunFormatServisi.FormatTL(bahis)}\n" +
                $"KAZANILAN: {OyunFormatServisi.FormatTL(odenen)}\n" +
                $"<color=#FFD64D>KAZANÇ: {OyunFormatServisi.FormatTL(net)}</color>";
        }
        else if (kayip)
        {
            _normalSpinSonucIcerikTxt.text =
                "<color=#FF5A5A>ÜZGÜNÜM</color>\n" +
                $"BAHİS: {OyunFormatServisi.FormatTL(bahis)}\n" +
                $"<color=#FF5A5A>KAYIP: {OyunFormatServisi.FormatTL(Mathf.Abs(net))}</color>";
        }
        else
        {
            _normalSpinSonucIcerikTxt.text =
                $"BAHİS: {OyunFormatServisi.FormatTL(bahis)}\n" +
                $"KAZANILAN: {OyunFormatServisi.FormatTL(odenen)}\n" +
                "<color=#FFFFFF>KAZANÇ: 0 TL</color>";
        }
        _normalSpinSonucIcerikTxt.color = Color.white;

        _normalSpinSonucPopup.SetActive(true);
        yield return StartCoroutine(AnimateNormalSpinSonucBakiyeAkisi(net));
        yield return new WaitForSecondsRealtime(1.55f);
        _normalSpinSonucPopup.SetActive(false);
        _normalSpinSonucPopupCalisiyor = false;
    }

    private IEnumerator AnimateNormalSpinSonucBakiyeAkisi(int net)

    {
        if (net == 0 || bakiyeText == null || _ekonomiServisi == null || _normalSpinSonucPopup == null)
            yield break;

        int hedefBakiye = _ekonomiServisi.Bakiye;
        int baslangicBakiye = hedefBakiye - net;
        float sure = 1.95f;
        float gecen = 0f;

        RectTransform canvasRt = _normalSpinSonucPopup.GetComponent<RectTransform>();
        if (canvasRt == null)
            yield break;

        var akisYaziGo = new GameObject("BakiyeAkisYazi");
        akisYaziGo.transform.SetParent(_normalSpinSonucPopup.transform, false);
        var akisRt = akisYaziGo.AddComponent<RectTransform>();
        akisRt.sizeDelta = new Vector2(620f, 130f);
        var akisTxt = akisYaziGo.AddComponent<TextMeshProUGUI>();
        akisTxt.alignment = TextAlignmentOptions.Center;
        akisTxt.fontSize = 88f;
        akisTxt.fontStyle = FontStyles.Bold;
        akisTxt.outlineWidth = 0.32f;
        akisTxt.outlineColor = new Color(0f, 0f, 0f, 0.85f);
        akisTxt.enableWordWrapping = false;
        akisTxt.color = net > 0 ? new Color(1f, 0.85f, 0.15f, 1f) : new Color(1f, 0.35f, 0.35f, 1f);
        akisTxt.text = $"{(net > 0 ? "+" : "-")}{OyunFormatServisi.FormatTL(Mathf.Abs(net))}";

        Vector2 baslangicPos = new Vector2(0f, -20f);
        Vector2 hedefPos = baslangicPos;
        RectTransform bakiyeRt = bakiyeText.transform as RectTransform;
        Camera cam = null;
        Canvas bakiyeCanvas = bakiyeText.canvas;
        if (bakiyeCanvas != null && bakiyeCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = bakiyeCanvas.worldCamera;
        if (bakiyeRt != null && RectTransformUtility.WorldToScreenPoint(cam, bakiyeRt.position) is Vector2 bakiyeEkran)
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, bakiyeEkran, null, out hedefPos);

        while (gecen < sure)
        {
            gecen += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(gecen / sure);
            float eased = Mathf.SmoothStep(0f, 1f, t);

            int anlikBakiye = Mathf.RoundToInt(Mathf.Lerp(baslangicBakiye, hedefBakiye, eased));
            bakiyeText.text = "Bakiye: " + OyunFormatServisi.FormatTL(anlikBakiye);

            akisRt.anchoredPosition = Vector2.Lerp(baslangicPos, hedefPos, eased);
            float olcek = Mathf.Lerp(1.35f, 0.90f, eased);
            akisRt.localScale = new Vector3(olcek, olcek, 1f);
            Color c = akisTxt.color;
            c.a = Mathf.Lerp(1f, 0.15f, eased);
            akisTxt.color = c;
            yield return null;
        }

        if (akisYaziGo != null)
            Destroy(akisYaziGo);
        _uiServisi?.UI_Guncelle();
    }


    /// <summary>Çarpan kazanç kutusuna değdiğinde, çarpan metninin üstünde kısa süre +N uçuşu (bakiye akışına benzer).</summary>
    private IEnumerator KazancKutusunaCarpanVurusPlusAnimasyonu(int carpanDeger)
    {
        if (carpanDeger <= 0 || kazancText == null) yield break;
        RectTransform kazancRt = kazancText.rectTransform;
        Canvas canvas = kazancRt.GetComponentInParent<Canvas>();
        RectTransform canvasRt = canvas != null ? canvas.rootCanvas.transform as RectTransform : null;
        if (canvas == null || canvasRt == null) yield break;

        var go = new GameObject("KazancCarpanPlusYazi");
        go.transform.SetParent(canvasRt, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(420f, 140f);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 78f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.outlineWidth = 0.3f;
        tmp.outlineColor = new Color(0f, 0f, 0f, 0.88f);
        tmp.enableWordWrapping = false;
        tmp.color = new Color(1f, 0.9f, 0.22f, 1f);
        tmp.text = "+" + carpanDeger.ToString();
        if (kazancText is TextMeshProUGUI ktmp)
        {
            tmp.font = ktmp.font;
            tmp.fontSharedMaterials = ktmp.fontSharedMaterials;
        }
        rt.SetAsLastSibling();

        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 ekran = RectTransformUtility.WorldToScreenPoint(cam, kazancRt.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, ekran, cam, out Vector2 kutuMerkez);
        // Kutunun altından yukarı doğru (ekran üstüne kaçmasın); hafif overshoot ile kutu üst bandına yaklaşır.
        Vector2 baslangic = kutuMerkez + new Vector2(0f, -128f);
        Vector2 hedef = kutuMerkez + new Vector2(0f, 42f);
        rt.anchoredPosition = baslangic;
        rt.localScale = Vector3.one * 1.28f;

        float sure = 0.82f;
        float gecen = 0f;
        while (gecen < sure)
        {
            gecen += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(gecen / sure);
            float e = Mathf.SmoothStep(0f, 1f, t);
            rt.anchoredPosition = Vector2.Lerp(baslangic, hedef, e);
            float olcek = Mathf.Lerp(1.28f, 0.78f, e);
            rt.localScale = new Vector3(olcek, olcek, 1f);
            Color c = tmp.color;
            c.a = Mathf.Lerp(1f, 0.08f, e);
            tmp.color = c;
            yield return null;
        }

        Destroy(go);
    }

    private void PlayNormalSpinSonucSesi(int odenen, int bahis)

    {
        SesKaynaklariniHazirla();
        int net = odenen - bahis;
        bool kazancVar = net > 0;
        AudioClip clip = kazancVar ? spinSonucKazancClip : spinSonucKayipClip;
        if (clip == null && kazancVar)
            clip = bonusEndApplauseClip != null ? bonusEndApplauseClip : tumblePopClip;
        if (clip == null && !kazancVar)
            clip = tumbleDropClip != null ? tumbleDropClip : tumblePopClip;

        if (clip == null) return;
        AudioSource kaynak = tumbleSfxSource != null ? tumbleSfxSource : bonusEndSfxSource;
        if (kaynak == null)
            kaynak = FindFirstObjectByType<AudioSource>(FindObjectsInactive.Include);
        if (kaynak == null) return;
        kaynak.PlayOneShot(clip, 1f);
    }

    private void SesKaynaklariniHazirla()

    {
        if (tumbleSfxSource == null)
            tumbleSfxSource = GameObject.Find("TumbleSfxSource")?.GetComponent<AudioSource>()
                ?? GameObject.Find("SfxSource")?.GetComponent<AudioSource>()
                ?? FindFirstObjectByType<AudioSource>(FindObjectsInactive.Include);

        if (bonusEndSfxSource == null)
            bonusEndSfxSource = GameObject.Find("BonusEndSfxSource")?.GetComponent<AudioSource>()
                ?? GameObject.Find("BonusSfxSource")?.GetComponent<AudioSource>()
            ?? FindFirstObjectByType<AudioSource>(FindObjectsInactive.Include);
        if (_hizVeSesServisi != null && tumbleSfxSource != null)
            _hizVeSesServisi.SetAudioSource(tumbleSfxSource);
    }


    public void ShowBakiyeYuklePanel(bool yetersizBakiyeUyarisi = false)
    {
        _uiServisi?.ResolveMoneyUIRefsIfMissing();
        _uiServisi?.WireBakiyeYukleUI();

        int kalanHak = _ekonomiServisi != null ? _ekonomiServisi.GetBakiyeYuklemeKalanHak() : 0;
        if (bakiyeYukleUyariText != null)
        {
            if (kalanHak <= 0)
                bakiyeYukleUyariText.text = "Bakiye yükleme hakkın kalmadı.";
            else if (yetersizBakiyeUyarisi)
                bakiyeYukleUyariText.text = $"Bakiye yetersiz. Bakiye azalıyor — 20.000 TL yükleme yapmak ister misin? (Kalan hak: {kalanHak})";
            else
                bakiyeYukleUyariText.text = $"20.000 TL yükleme yapmak ister misin? (Kalan hak: {kalanHak})";
        }
        _uiServisi?.CloseMoneyPanels();

        if (bakiyeYuklePanel != null)
            bakiyeYuklePanel.SetActive(true);
        SenaryoYoneticisi.I?.LogEkle(SenaryoOlayKaydi.OlayTipi_BakiyeYuklemeEkraniAcildi, "Bakiye yükleme ekranı açıldı. Kalan yükleme hakkı: " + kalanHak + "." + (yetersizBakiyeUyarisi ? " (Yetersiz bakiye uyarısı)" : ""));
    }


    public void HideBakiyeYuklePanel()
    {
        if (bakiyeYuklePanel != null)
            bakiyeYuklePanel.SetActive(false);
    }


    public void BonusSatinAl()
    {
        if (_bonusAyarlari == null)
            _bonusAyarlari = FindFirstObjectByType<BonusAyarlari>(FindObjectsInactive.Include);
        Debug.Log("[BONUS SATIN AL] Buton tiklandi.");
        _bonusUIServisi?.BonusSatinAlRequested();
    }


    public void BonusSatinAlOnayla() => _bonusUIServisi?.OnYes();

    public void BonusSatinAlIptal() => _bonusUIServisi?.OnNo();


    private void ShowBonusBuyConfirmPanel(int cost)
    {
        _bonusUIServisi?.ShowBonusBuyConfirmPanel(cost);
        _uiServisi?.UI_Guncelle();
    }


    private void HideBonusBuyConfirmPanel()
    {
        _bonusUIServisi?.HideBonusBuyConfirmPanel();
        _uiServisi?.UI_Guncelle();
    }


    private void OnBonusBuyYes() => _bonusUIServisi?.OnYes();

    private void OnBonusBuyNo() => _bonusUIServisi?.OnNo();


    void BonusMiktariYazisiniGuncelle(int maliyet, GameObject panel)
    {
        long bonusMiktari = Mathf.Max(0, maliyet);
        string formatliMiktar = OyunFormatServisi
            .FormatTL((int)Mathf.Min(int.MaxValue, bonusMiktari))
            .Replace(" TL", " TL");
        string metin = formatliMiktar + " karsiligindan bonus oyun almak istiyor musunuz?";

        if (bonusBuyConfirmCostText != null)
        {
            bonusBuyConfirmCostText.text = metin;
            return;
        }

        if (panel == null) return;
        var tumMetinler = panel.GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in tumMetinler)
        {
            if (tmp == null || tmp.gameObject == null) continue;
            string ad = tmp.gameObject.name.ToLowerInvariant();
            if (ad.Contains("maliyet") || ad.Contains("cost") || ad.Contains("bonusmiktar"))
            {
                tmp.text = metin;
                return;
            }
        }
    }

    /// <summary>PanelKopru için: mevcut ekonomik bakiyeyi public erişimle döner (HTML iframe panele postMessage için).</summary>
    public int BahisPanelMevcutBakiye() => _ekonomiServisi != null ? _ekonomiServisi.Bakiye : 0;

    /// <summary>AnlaticiSeritKopru için: mevcut bahis miktarı (etap profili max ödeme hesabı için).</summary>
    public int AnlaticiMevcutBahis() => _ekonomiServisi != null ? _ekonomiServisi.Bahis : 0;

    /// <summary>AnlaticiSeritKopru için: aşama geçişinde önerilen bahis set eder.</summary>
    public void AnlaticiSetBahis(int tl)
    {
        if (_ekonomiServisi == null) return;
        if (tl < 10) tl = 10;
        _ekonomiServisi.SetBahis(tl);
        _uiServisi?.UI_Guncelle();
        Debug.Log("[AnlaticiSetBahis] Bahis " + tl + " TL");
    }

    /// <summary>AnlaticiSeritKopru için: bakiyeyi 50000 TL'ye reset (her sahne girişinde sıfırdan).</summary>
    public void AnlaticiBakiyeyiSifirla(int yeniBakiye = 50000)
    {
        if (_ekonomiServisi == null) return;
        _ekonomiServisi.SetBakiye(yeniBakiye);
        _uiServisi?.UI_Guncelle();
        Debug.Log("[BAKIYE RESET] Başlangıç bakiye " + yeniBakiye + " TL'ye sıfırlandı");
    }

    /// <summary>AnlaticiSeritKopru için: üst üste kazanç/kayıp fazı state'ini sıfırla
    /// (önceki oturumdan kalan _ustUsteKazancFaziAktif=true gibi durumlar Anlatıcı eğilim ayarını bypass edebiliyor).</summary>
    public void AnlaticiKazancFaziniSifirla()
    {
        _ustUsteKazancFaziAktif = false;
        _ustUsteFazdaKalan = 0;
        _ustUsteKazancHedef = 0;
        _ustUsteKayipHedef = 0;
        SpinPolitikasiniYenile();
        OncedenHesaplananSpinOnbelleginiTemizle();
        Debug.Log("[Admin] UstUsteKazancFazi mekanizması sıfırlandı (Anlatıcı takeover).");
    }

    /// <summary>Bahis +/- butonlarına basıldığında çağrılır (2026-04-30 hibrit).
    /// WebGL: HTML iframe (bahisSec.html). Editor: Unity UI fallback.</summary>
    public void BahisSecimPopupGoster()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL: PanelKopru üzerinden HTML iframe paneli aç
        var pk = FindObjectOfType<PanelKopru>();
        if (pk != null) pk.BahisSecPaneliAc();
        else { Debug.LogError("[BAHIS_POPUP] PanelKopru bulunamadı, Editor fallback'e düşülüyor"); BahisSecimPopupGosterEditorFallback(); }
#else
        BahisSecimPopupGosterEditorFallback();
#endif
    }

    /// <summary>Editor (Unity UI) fallback — eski programatik UI builder. WebGL'de iframe kullanılıyor.</summary>
    public void BahisSecimPopupGosterEditorFallback()
    {
        try
        {
            Debug.Log("[BAHIS_POPUP] Goster çağrıldı (Editor fallback Unity UI)");

            if (spinCalisiyor)
            {
                Debug.LogWarning("[BAHIS_POPUP] spinCalisiyor=true, popup açılmıyor");
                return;
            }
            if (bonusAktif)
            {
                Debug.LogWarning("[BAHIS_POPUP] bonusAktif=true, popup açılmıyor");
                return;
            }
            if (_ekonomiServisi == null)
            {
                Debug.LogError("[BAHIS_POPUP] _ekonomiServisi NULL — popup açılamadı");
                return;
            }

            // ScreenSpaceOverlay canvas önceliği (popup için doğru render mode)
            Canvas canvas = null;
            var canvases = FindObjectsOfType<Canvas>();
            Debug.Log($"[BAHIS_POPUP] Sahne'de {canvases.Length} canvas bulundu");
            foreach (var c in canvases)
            {
                if (c == null) continue;
                if (c.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    canvas = c;
                    break;
                }
            }
            if (canvas == null && canvases.Length > 0)
                canvas = canvases[0]; // fallback: ilk canvas

            if (canvas == null)
            {
                Debug.LogError("[BAHIS_POPUP] Canvas bulunamadı — popup açılamadı");
                return;
            }

            int bakiye = _ekonomiServisi.Bakiye;
            Debug.Log($"[BAHIS_POPUP] Canvas={canvas.name} bakiye={bakiye}");

            BahisSecimPopup.Goster(canvas, bakiye, secilen =>
            {
                try
                {
                    Debug.Log($"[BAHIS_POPUP] Callback: secilen={secilen}");
                    if (_ekonomiServisi != null)
                    {
                        _ekonomiServisi.SetBahis(secilen);
                        Debug.Log($"[BAHIS_POPUP] SetBahis({secilen}) tamam");
                    }
                    if (_uiServisi != null)
                        _uiServisi.UI_Guncelle();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[BAHIS_POPUP] Callback HATA: {ex.Message}\n{ex.StackTrace}");
                }
            });
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[BAHIS_POPUP] HATA: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public void BahisArttir()
    {
        if (_ekonomiServisi == null) return;
        _ekonomiServisi.BahisArttir();
        SenaryoYoneticisi.I?.UI_Guncelle();
    }

    public void BahisAzalt()
    {
        if (_ekonomiServisi == null) return;
        _ekonomiServisi.BahisAzalt();
        SenaryoYoneticisi.I?.UI_Guncelle();
    }


    private void BaslatGeciciGlobalTiklamaKilidi(float sure)
    {
        if (sure <= 0f) return;
        if (_geciciTiklamaKilidiCoroutine != null)
            StopCoroutine(_geciciTiklamaKilidiCoroutine);
        _geciciTiklamaKilidiCoroutine = StartCoroutine(GeciciGlobalTiklamaKilidiCoroutine(sure));
    }


    private IEnumerator GeciciGlobalTiklamaKilidiCoroutine(float sure)
    {
        EnsureGlobalTiklamaKilidiPanel();
        _geciciTiklamaKilidiAktif = true;
        UygulaGlobalTiklamaKilidiGorunurlugu();
        yield return new WaitForSecondsRealtime(sure);
        _geciciTiklamaKilidiAktif = false;
        UygulaGlobalTiklamaKilidiGorunurlugu();
        _geciciTiklamaKilidiCoroutine = null;
    }


    private void EnsureGlobalTiklamaKilidiPanel()
    {
        if (_geciciTiklamaKilidiPanel != null) return;
        _geciciTiklamaKilidiPanel = new GameObject("GeciciTiklamaKilidiPanel");
        var rt = _geciciTiklamaKilidiPanel.AddComponent<RectTransform>();
        var canvas = _geciciTiklamaKilidiPanel.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 32760;
        _geciciTiklamaKilidiPanel.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _geciciTiklamaKilidiPanel.AddComponent<GraphicRaycaster>();
        var img = _geciciTiklamaKilidiPanel.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);
        img.raycastTarget = true;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        DontDestroyOnLoad(_geciciTiklamaKilidiPanel);
    }


    private void SetGlobalTiklamaKilidi(bool aktif)
    {
        _manuelGlobalTiklamaKilidiAktif = aktif;
        UygulaGlobalTiklamaKilidiGorunurlugu();
    }


    private void UygulaGlobalTiklamaKilidiGorunurlugu()
    {
        EnsureGlobalTiklamaKilidiPanel();
        bool aktif = false;
        if (_geciciTiklamaKilidiPanel != null)
            _geciciTiklamaKilidiPanel.SetActive(aktif);
    }

    private void OtomatikSpinKalanTextGuncelle()
    {
        if (otomatikSpinKalanText != null)
        {
            if (_otomatikSpinKalan > 0 && !bonusAktif)
            {
                otomatikSpinKalanText.text = $"Kalan Spin: {_otomatikSpinKalan}";
                otomatikSpinKalanText.gameObject.SetActive(true);
            }
            else
                otomatikSpinKalanText.gameObject.SetActive(false);
        }
        if (otomatikSpinButton != null)
        {
            var tmp = otomatikSpinButton.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
                tmp.text = _otomatikSpinKalan > 0 ? "DURDUR" : (string.IsNullOrEmpty(otomatikSpinButtonNormalText) ? "Otomatik Spin" : otomatikSpinButtonNormalText);
        }
    }


    private void OnOtomatikSpinDropdownChanged(int index)
    {
        int[] secenekler = { 20, 50, 100, 250 };
        _otomatikSpinSecilenAdet = (index >= 0 && index < secenekler.Length) ? secenekler[index] : 20;
    }


    /// <summary>OtomatikSpinButton tıklanınca: spin dönüyorsa durdur, değilse paneli aç.</summary>
    private void OnOtomatikSpinButtonClick()
    {
        if (_otomatikSpinKalan > 0)
        {
            OtomatikSpinDurdur();
            return;
        }
        // Sahne 3’te sadece buton kopyaılanmış olabilir; panel yoksa doğrudan varsayılan adetle başlat.
        if (bonusAktif || spinCalisiyor) return;
        if (otomatikSpinPanel == null)
        {
            if (_ekonomiServisi == null || _ekonomiServisi.Bakiye < _ekonomiServisi.Bahis)
            {
                ShowBakiyeYuklePanel(yetersizBakiyeUyarisi: true);
                return;
            }
            _otomatikSpinKalan = (_otomatikSpinSecilenAdet > 0) ? _otomatikSpinSecilenAdet : 20;
            OtomatikSpinKalanTextGuncelle();
            StartCoroutine(OtomatikSpinDongusu());
            return;
        }
        if (otomatikSpinPanel != null)
        {
            otomatikSpinPanel.SetActive(true);
            // Panel diğer panellerin üstünde görünsün (sıra: en son = en üstte)
            if (otomatikSpinPanel.transform.parent != null)
            {
                otomatikSpinPanel.transform.SetAsLastSibling();
                otomatikSpinPanel.transform.parent.SetAsLastSibling();
            }
        }
    }


    /// <summary>Paneldeki Baslat: seçilen adet ile döngüyü başlat, paneli kapat.</summary>
    private void OnOtomatikSpinBaslatClick()
    {
        if (otomatikSpinPanel != null)
            otomatikSpinPanel.SetActive(false);
        if (bonusAktif || spinCalisiyor) return;
        if (_ekonomiServisi == null || _ekonomiServisi.Bakiye < _ekonomiServisi.Bahis)
        {
            // Bakiye yetersiz: bakiye yükle panelini "bakiye azalıyor, yükleme yapmak ister misin?" ile aç.
            ShowBakiyeYuklePanel(yetersizBakiyeUyarisi: true);
            return;
        }
        if (otomatikSpinDropdown != null)
            OnOtomatikSpinDropdownChanged(otomatikSpinDropdown.value);
        _otomatikSpinKalan = _otomatikSpinSecilenAdet;
        OtomatikSpinKalanTextGuncelle();
        StartCoroutine(OtomatikSpinDongusu());
    }


    /// <summary>Paneldeki İptal: paneli kapat, spin başlatma.</summary>
    private void OnOtomatikSpinIptalClick()
    {
        if (otomatikSpinPanel != null)
            otomatikSpinPanel.SetActive(false);
    }


    private void IstatistikButonTiklandi()
    {
        if (GameManager.I != null)
            GameManager.I.LoadScene("05_LogScane");
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("05_LogScane", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }


    private void YoneticiButonTiklandi()
    {
        AdminGirisDogrulama.Ac(() =>
        {
            if (GameManager.I != null)
                GameManager.I.LoadScene("04_AdminOyunScene");
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("04_AdminOyunScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
        });
    }


    private IEnumerator ShowBonusStartMessage()
    {
        yield return StartCoroutine(_bonusUIServisi.ShowBonusStartMessage());
    }

    private IEnumerator ShowBonusEndMessage(int bonusToplamKazanc)
    {
        if (bonusEndPanel == null) yield break;

        if (_buBonusZirveBonusuMu)
        {
            OnZirveBonusBitti?.Invoke(bonusToplamKazanc);
            _buBonusZirveBonusuMu = false;
        }

        // DonusAkisServisi zaten BonusOturumOdenenToplamTL'yi bakiyeye ekledi. Burada sadece zorla çarpan birikimini ekleyip temizliyoruz; PayFromHavuz ile tekrar ekleme yapılmaz (çift ekleme önlenir).
        int prevBakiye = _ekonomiServisi != null ? _ekonomiServisi.Bakiye : 0;
        if (_ekonomiServisi != null && _bonusZorlaCarpanBirikenTL > 0)
        {
            _ekonomiServisi.AddWinnings(_bonusZorlaCarpanBirikenTL, 0);
            _uiServisi?.UI_Guncelle();
        }
        int toplamEklendi = _bonusOturumOdenenToplamTL + _bonusZorlaCarpanBirikenTL;
        _logServisi?.RecordBonusEnd(prevBakiye, _ekonomiServisi != null ? _ekonomiServisi.Bakiye : 0, toplamEklendi);
        _bonusPendingOdemeTL = 0;
        _bonusZorlaCarpanBirikenTL = 0;

        // Panelde gösterilecek değer: bu bonusta bakiyeye eklenen toplam (bonusToplamKazanc = OturumKazanc zaten bu toplama eşit)
        yield return StartCoroutine(_bonusUIServisi.ShowBonusEndMessage(bonusToplamKazanc));
    }


    private void TrySpawnCarpanOverlay(int carpanDegeri)
    {
        if (carpanSembolSprite == null) return;
        if (hucreler == null || hucreler.Length == 0) return;
        int idx = UnityEngine.Random.Range(0, hucreler.Length);
        _carpanOverlayServisi?.SpawnCarpanOverlayAt(idx, carpanDegeri);
    }


    private void ClearAllCarpanOverlays()
    {
        _carpanOverlayServisi?.ClearAll();
        for (int y = 0; y < satir; y++)
        {
            // Grid içindeki çarpan sembollerini de sıfırla (bir sonraki spin temiz başlasın)
            if (grid != null && carpanDegerGrid != null)
            {
                for (int x = 0; x < sutun; x++)
                {
                    if (grid[x, y] == CARPAN_SEMBOL)
                    {
                        grid[x, y] = -1; // boş yap; FillRandomAll/yerçekimi zaten dolduracak
                        carpanDegerGrid[x, y] = 0;
                    }
                }
            }
            if (carpanHücreTextleri != null)
            {
                for (int i = 0; i < carpanHücreTextleri.Length; i++)
                    if (carpanHücreTextleri[i] != null) carpanHücreTextleri[i].gameObject.SetActive(false);
            }
        }
    }


// ==========================
// ÇARPAN UI (Yeni Sistem)
// ==========================
    private void UI_CarpanSifirla()
    {
        // Spin başında toggle görseli mutlaka current model değeriyle eşleşsin.
        // Başka script'ler Toggle state'ini değiştirse bile kullanıcı seçimini geri zorlayaLım.
        if (carpanAktifToggle != null)
            carpanAktifToggle.SetIsOnWithoutNotify(carpanUretimiAktif);
        if (carpanSadeceBonusToggle != null)
            carpanSadeceBonusToggle.SetIsOnWithoutNotify(carpanSadeceBonus);

        int maxAdet = _senaryoServisi != null ? _senaryoServisi.GetMaxCarpanAdedi() : 0;
        if (zorlaSiradakiCarpan > 0 && maxAdet < 1)
            maxAdet = 1;
        _carpanServisi?.ResetForNewSpin(maxAdet);
        ClearAllCarpanOverlays();
        UI_CarpanGuncelle();
    }


    private void UI_CarpanGuncelle()
    {
        if (carpanText == null) return;

        long mlt = _carpanServisi != null ? _carpanServisi.GetCurrentMultiplier() : 0;
        if (mlt < 1) mlt = 1;

      //  carpanText.text = $"ÇARPAN: x{mlt}";
    }


    private IEnumerator ScatterBuyutEfekti()
    {
        if (_scatterEfektServisi == null) yield break;
        yield return _scatterEfektServisi.ScatterBuyutEfektiCalistir();
    }
}