using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// "Seni nasıl çağıralım?" modal'ı. GirisUI.OyunaBasla() tarafından açılır.
/// Canvas üzerine prosedürel olarak oluşturulur, callback ile sahne geçişi tetiklenir.
/// </summary>
public class KullaniciAdiModal : MonoBehaviour
{
    private Action<string> _onDevam;
    private TMP_InputField _input;
    private CanvasGroup    _overlayGroup;
    private CanvasGroup    _kartGroup;
    private RectTransform  _kartRt;

    // ── Renk yardımcıları ────────────────────────────────────────────
    static Color C(string hex, float a = 1f)
    {
        Color c = Color.white;
        ColorUtility.TryParseHtmlString(hex, out c);
        c.a = a; return c;
    }

    // ── Giriş noktası ────────────────────────────────────────────────
    public static KullaniciAdiModal Goster(Canvas canvas, Action<string> onDevam)
    {
        var go = new GameObject("[KullaniciAdiModal]", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsLastSibling();
        var m = go.AddComponent<KullaniciAdiModal>();
        m._onDevam = onDevam;
        m.Build();
        return m;
    }

    void Build()
    {
        var rt = GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        // ── Overlay ──
        var overlayImg = gameObject.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0f);
        overlayImg.raycastTarget = true;
        _overlayGroup = gameObject.AddComponent<CanvasGroup>();
        _overlayGroup.alpha = 0f;

        // ── Kart ──
        var kartGo = new GameObject("Kart", typeof(RectTransform));
        kartGo.transform.SetParent(transform, false);
        _kartRt = kartGo.GetComponent<RectTransform>();
        _kartRt.anchorMin = new Vector2(.5f, .5f); _kartRt.anchorMax = new Vector2(.5f, .5f);
        _kartRt.pivot     = new Vector2(.5f, .5f);
        _kartRt.anchoredPosition = Vector2.zero;
        _kartRt.sizeDelta = new Vector2(500f, 280f);

        var kartImg = kartGo.AddComponent<Image>();
        kartImg.color = C("#1a0f2e");
        _kartGroup = kartGo.AddComponent<CanvasGroup>();
        _kartGroup.alpha = 0f;
        kartGo.transform.localScale = new Vector3(.9f, .9f, 1f);

        // Altın kenarlık (1px iç çerçeve)
        var brdGo = new GameObject("Border", typeof(RectTransform), typeof(Image));
        brdGo.transform.SetParent(kartGo.transform, false);
        var brdRt = brdGo.GetComponent<RectTransform>();
        brdRt.anchorMin = Vector2.zero; brdRt.anchorMax = Vector2.one;
        brdRt.offsetMin = Vector2.zero; brdRt.offsetMax = Vector2.zero;
        brdGo.GetComponent<Image>().color = C("#FAC775", .40f);
        brdGo.GetComponent<Image>().raycastTarget = false;

        var içGo = new GameObject("Ic", typeof(RectTransform), typeof(Image));
        içGo.transform.SetParent(kartGo.transform, false);
        var içRt = içGo.GetComponent<RectTransform>();
        içRt.anchorMin = Vector2.zero; içRt.anchorMax = Vector2.one;
        içRt.offsetMin = new Vector2(1f, 1f); içRt.offsetMax = new Vector2(-1f, -1f);
        içGo.GetComponent<Image>().color = C("#1a0f2e");
        içGo.GetComponent<Image>().raycastTarget = false;

        // ── İçerik VLG ──
        var icVlgGo = new GameObject("Icerik", typeof(RectTransform));
        icVlgGo.transform.SetParent(kartGo.transform, false);
        var icRt2 = icVlgGo.GetComponent<RectTransform>();
        icRt2.anchorMin = Vector2.zero; icRt2.anchorMax = Vector2.one;
        icRt2.offsetMin = new Vector2(40f, 24f); icRt2.offsetMax = new Vector2(-40f, -24f);
        var vlg = icVlgGo.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.spacing = 0f;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        // Başlık
        AddTxt(icVlgGo.transform, "Adın", 24, C("#FAC775"), bold: true,
               prefH: 36, bottomSpace: 8);

        // Açıklama
        AddTxt(icVlgGo.transform, "Sana bu isimle sesleneceğiz.",
               13, new Color(1f, 1f, 1f, .70f), prefH: 22, bottomSpace: 20);

        // Input field
        _input = BuildInput(icVlgGo.transform);
        var inputLE = _input.gameObject.AddComponent<LayoutElement>();
        inputLE.preferredHeight = 44f;
        inputLE.flexibleWidth = 1f;
        Spacer(icVlgGo.transform, 20f);

        // Butonlar
        var btnRow = new GameObject("Butonlar", typeof(RectTransform));
        btnRow.transform.SetParent(icVlgGo.transform, false);
        var btnLE = btnRow.AddComponent<LayoutElement>();
        btnLE.preferredHeight = 44f;
        var hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16f; hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false; hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;

        BuildBtn(btnRow.transform, "BAŞLA",   C("#FAC775"), C("#1a0f2e"), 140f, 44f, OnBasla);
        BuildBtn(btnRow.transform, "Misafir", C("#1a0f2e", .0f), C("#FAC775", .6f), 140f, 44f,
                 () => OnDevamEt("Misafir"), border: true);

        StartCoroutine(FadeIn());
    }

    // ── Kart açılış animasyonu ──
    IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / .25f;
            float ov = Mathf.Clamp01(t);
            _overlayGroup.alpha = ov * .70f;
            float kf = Mathf.Clamp01(t / (.3f / .25f));
            _kartGroup.alpha = Mathf.SmoothStep(0f, 1f, kf);
            float sc = Mathf.Lerp(.9f, 1f, Mathf.SmoothStep(0f, 1f, kf));
            _kartRt.localScale = new Vector3(sc, sc, 1f);
            yield return null;
        }
        _overlayGroup.alpha = .70f;
        _kartGroup.alpha = 1f;
        _kartRt.localScale = Vector3.one;

        if (EventSystem.current != null && _input != null)
            EventSystem.current.SetSelectedGameObject(_input.gameObject);
        _input?.ActivateInputField();
    }

    // ── Callback'ler ──
    void OnBasla()
    {
        string ad = _input != null ? _input.text.Trim() : "";
        OnDevamEt(string.IsNullOrEmpty(ad) ? "Misafir" : ad);
    }

    void OnDevamEt(string ad)
    {
        // BUG FIX (2026-04-29): Kullanıcı adı 3 yere senkron yazılır — eskiden sadece callback'e geçiyordu,
        // sahne geçişinden sonra "Misafir" gözüküyordu çünkü KullaniciVerileri.KullaniciAdi ve PlayerPrefs boştu.
        if (string.IsNullOrWhiteSpace(ad)) ad = "Misafir";
        KullaniciVerileri.KullaniciAdi = ad;
        UnityEngine.PlayerPrefs.SetString("KullaniciAdi", ad);
        UnityEngine.PlayerPrefs.Save();
        if (GameManager.I != null && GameManager.I.ActivePlayer != null)
            GameManager.I.ActivePlayer.playerName = ad;
        UnityEngine.Debug.Log($"[KullaniciAdiModal] Ad kaydedildi: '{ad}' (statik + PlayerPrefs + ActivePlayer)");

        var cb = _onDevam;
        _onDevam = null;
        Destroy(gameObject);
        cb?.Invoke(ad);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            OnBasla();
        if (Input.GetKeyDown(KeyCode.Escape))
            OnDevamEt("Misafir");
    }

    // ── Yardımcılar ──
    static char ValidateChar(string text, int index, char addedChar)
    {
        if (char.IsLetter(addedChar) || addedChar == ' ') return addedChar;
        return '\0';
    }

    TMP_InputField BuildInput(Transform parent)
    {
        var go = new GameObject("Input", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var bg = go.AddComponent<Image>();
        bg.color = C("#0d0920");

        // Kenarlık rengi
        var brdGo = new GameObject("InputBorder", typeof(RectTransform), typeof(Image));
        brdGo.transform.SetParent(go.transform, false);
        var brdRt = brdGo.GetComponent<RectTransform>();
        brdRt.anchorMin = Vector2.zero; brdRt.anchorMax = Vector2.one;
        brdRt.offsetMin = Vector2.zero; brdRt.offsetMax = Vector2.zero;
        brdGo.GetComponent<Image>().color = C("#FAC775", .50f);
        brdGo.GetComponent<Image>().raycastTarget = false;

        var içGo2 = new GameObject("InputIc", typeof(RectTransform), typeof(Image));
        içGo2.transform.SetParent(go.transform, false);
        var içRt3 = içGo2.GetComponent<RectTransform>();
        içRt3.anchorMin = Vector2.zero; içRt3.anchorMax = Vector2.one;
        içRt3.offsetMin = new Vector2(1f, 1f); içRt3.offsetMax = new Vector2(-1f, -1f);
        içGo2.GetComponent<Image>().color = C("#0d0920");
        içGo2.GetComponent<Image>().raycastTarget = false;

        // Text area
        var taGo = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
        taGo.transform.SetParent(go.transform, false);
        var taRt = taGo.GetComponent<RectTransform>();
        taRt.anchorMin = Vector2.zero; taRt.anchorMax = Vector2.one;
        taRt.offsetMin = new Vector2(14f, 6f); taRt.offsetMax = new Vector2(-14f, -6f);

        // Placeholder
        var phGo = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        phGo.transform.SetParent(taGo.transform, false);
        StretchRT(phGo.GetComponent<RectTransform>());
        var ph = phGo.GetComponent<TextMeshProUGUI>();
        ph.text = "Adını yaz..."; ph.fontSize = 18; ph.color = new Color(1f, 1f, 1f, .35f);
        ph.alignment = TextAlignmentOptions.MidlineLeft; ph.raycastTarget = false;

        // Text
        var txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGo.transform.SetParent(taGo.transform, false);
        StretchRT(txtGo.GetComponent<RectTransform>());
        var txt = txtGo.GetComponent<TextMeshProUGUI>();
        txt.fontSize = 18; txt.color = Color.white;
        txt.alignment = TextAlignmentOptions.MidlineLeft; txt.raycastTarget = false;

        var input = go.AddComponent<TMP_InputField>();
        input.textViewport  = taRt;
        input.textComponent = txt;
        input.placeholder   = ph;
        input.characterLimit       = 20;
        input.onValidateInput      += ValidateChar;
        input.onSubmit.AddListener(_ => OnBasla());

        return input;
    }

    static void BuildBtn(Transform parent, string label, Color bg, Color txtC, float w, float h,
                         UnityEngine.Events.UnityAction onClick, bool border = false)
    {
        var go = new GameObject(label, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(w, h);
        var le = go.AddComponent<LayoutElement>();
        le.minWidth = w; le.preferredWidth = w; le.minHeight = h; le.preferredHeight = h;

        var img = go.AddComponent<Image>();
        img.color = bg;

        if (border)
        {
            var brdGo2 = new GameObject("BtnBorder", typeof(RectTransform), typeof(Image));
            brdGo2.transform.SetParent(go.transform, false);
            var bRt = brdGo2.GetComponent<RectTransform>();
            bRt.anchorMin = Vector2.zero; bRt.anchorMax = Vector2.one;
            bRt.offsetMin = Vector2.zero; bRt.offsetMax = Vector2.zero;
            brdGo2.GetComponent<Image>().color = C("#FAC775", .55f);
            brdGo2.GetComponent<Image>().raycastTarget = false;

            var içGo3 = new GameObject("BtnIc", typeof(RectTransform), typeof(Image));
            içGo3.transform.SetParent(go.transform, false);
            var iRt = içGo3.GetComponent<RectTransform>();
            iRt.anchorMin = Vector2.zero; iRt.anchorMax = Vector2.one;
            iRt.offsetMin = new Vector2(1f, 1f); iRt.offsetMax = new Vector2(-1f, -1f);
            içGo3.GetComponent<Image>().color = C("#1a0f2e");
            içGo3.GetComponent<Image>().raycastTarget = false;
        }

        var txtGo2 = new GameObject("BtnTxt", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGo2.transform.SetParent(go.transform, false);
        StretchRT(txtGo2.GetComponent<RectTransform>());
        var t = txtGo2.GetComponent<TextMeshProUGUI>();
        t.text = label; t.fontSize = 15; t.color = txtC;
        t.fontStyle = FontStyles.Bold; t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        // Hover rengi
        var cols = btn.colors;
        cols.normalColor      = Color.white;
        cols.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
        cols.pressedColor     = new Color(.8f, .8f, .8f, 1f);
        btn.colors = cols;
    }

    static void AddTxt(Transform parent, string text, float size, Color color,
                       bool bold = false, float prefH = 24f, float bottomSpace = 0f)
    {
        var go = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.enableWordWrapping = false; t.raycastTarget = false;
        if (bold) t.fontStyle = FontStyles.Bold;
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = prefH;
        if (bottomSpace > 0f) Spacer(parent, bottomSpace);
    }

    static void Spacer(Transform parent, float h)
    {
        var go = new GameObject("Sp", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = h; le.preferredHeight = h; le.flexibleHeight = 0f;
    }

    static void StretchRT(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
}
