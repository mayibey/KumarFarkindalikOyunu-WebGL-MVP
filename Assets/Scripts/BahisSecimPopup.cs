using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Bahis seçim pop-up'ı: 6 hızlı miktar + manuel input + onay.
/// Casino tema (siyah arka plan, altın çerçeve), KullaniciAdiModal paterni.
/// Static Goster çağrısıyla Canvas'a build edilir, callback ile sonuç döner.
/// </summary>
public class BahisSecimPopup : MonoBehaviour
{
    private Action<int> _onSec;
    private CanvasGroup _overlayGroup;
    private CanvasGroup _kartGroup;
    private RectTransform _kartRt;
    private TMP_InputField _input;
    private int _mevcutBakiye;
    private static readonly int[] HIZLI_MIKTARLAR = { 50, 100, 200, 300, 500, 1000 };

    public static BahisSecimPopup Goster(Canvas canvas, int mevcutBakiye, Action<int> onSec)
    {
        if (canvas == null) return null;
        var go = new GameObject("[BahisSecimPopup]", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsLastSibling();
        var m = go.AddComponent<BahisSecimPopup>();
        m._onSec = onSec;
        m._mevcutBakiye = mevcutBakiye;
        m.Build();
        return m;
    }

    void Build()
    {
        var rt = (RectTransform)transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        // Overlay (yarı saydam siyah)
        var overlay = new GameObject("Overlay", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        overlay.transform.SetParent(transform, false);
        var oRt = overlay.GetComponent<RectTransform>();
        oRt.anchorMin = Vector2.zero; oRt.anchorMax = Vector2.one;
        oRt.offsetMin = Vector2.zero; oRt.offsetMax = Vector2.zero;
        var oImg = overlay.GetComponent<Image>();
        oImg.color = new Color(0f, 0f, 0f, 0.78f);
        oImg.raycastTarget = true;
        _overlayGroup = overlay.GetComponent<CanvasGroup>();
        _overlayGroup.alpha = 0f;
        // Overlay'a tıklayınca iptal
        var oBtn = overlay.AddComponent<Button>();
        oBtn.transition = Selectable.Transition.None;
        oBtn.onClick.AddListener(OnIptal);

        // Kart (merkez, altın çerçeve)
        var kart = new GameObject("Kart", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        kart.transform.SetParent(transform, false);
        _kartRt = kart.GetComponent<RectTransform>();
        _kartRt.anchorMin = _kartRt.anchorMax = _kartRt.pivot = new Vector2(0.5f, 0.5f);
        _kartRt.sizeDelta = new Vector2(540f, 560f);
        _kartRt.anchoredPosition = Vector2.zero;
        _kartRt.localScale = Vector3.one * 0.9f;
        var kartImg = kart.GetComponent<Image>();
        kartImg.color = C("#0d0920", 0.96f);
        kartImg.raycastTarget = true;
        _kartGroup = kart.GetComponent<CanvasGroup>();
        _kartGroup.alpha = 0f;
        // Tıklama overlay'e geçmesin (kartın kendisi tıklanırsa iptal etmesin)
        var kartBtn = kart.AddComponent<Button>();
        kartBtn.transition = Selectable.Transition.None;

        // Altın çerçeve (border ekstra GO)
        var border = new GameObject("KartBorder", typeof(RectTransform), typeof(Image));
        border.transform.SetParent(kart.transform, false);
        var bRt = border.GetComponent<RectTransform>();
        bRt.anchorMin = Vector2.zero; bRt.anchorMax = Vector2.one;
        bRt.offsetMin = new Vector2(-3f, -3f); bRt.offsetMax = new Vector2(3f, 3f);
        var bImg = border.GetComponent<Image>();
        bImg.color = C("#FAC775", 1f);
        bImg.raycastTarget = false;
        border.transform.SetSiblingIndex(0);

        // Başlık ("Şanslı Bahis Seç!" — manipülasyon kanıtı)
        BuildText(_kartRt, "Baslik", "Şanslı Bahis Seç!", 30f, FontStyles.Bold,
            C("#FAC775"), new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.97f));

        // Bakiye bilgisi
        BuildText(_kartRt, "Bakiye", $"Bakiye: {OyunFormatServisi.FormatTL(_mevcutBakiye)}",
            18f, FontStyles.Normal, C("#cdb78a"),
            new Vector2(0.05f, 0.79f), new Vector2(0.95f, 0.86f));

        // Hızlı buton grid (3x2)
        for (int i = 0; i < HIZLI_MIKTARLAR.Length; i++)
        {
            int miktar = HIZLI_MIKTARLAR[i];
            int row = i / 3;       // 0 = üst sıra (50/100/200), 1 = alt sıra (300/500/1000)
            int col = i % 3;
            float xMin = 0.05f + col * 0.31f;
            float xMax = xMin + 0.28f;
            float yMax = 0.74f - row * 0.13f;
            float yMin = yMax - 0.10f;
            bool yeterli = _mevcutBakiye >= miktar;
            BuildHizliButon(_kartRt, miktar, yeterli,
                new Vector2(xMin, yMin), new Vector2(xMax, yMax));
        }

        // Manuel input + onay
        BuildText(_kartRt, "ManuelLabel", "Veya özel miktar gir:", 16f, FontStyles.Normal,
            C("#cdb78a"), new Vector2(0.05f, 0.40f), new Vector2(0.95f, 0.46f));
        _input = BuildInput(_kartRt, new Vector2(0.05f, 0.28f), new Vector2(0.65f, 0.39f));
        BuildButon(_kartRt, "Onayla", C("#3c8c3c"), new Vector2(0.67f, 0.28f), new Vector2(0.95f, 0.39f),
            OnManuelOnay);

        // İptal X (üst sağ)
        BuildButon(_kartRt, "✕", C("#a83a3a"), new Vector2(0.88f, 0.91f), new Vector2(0.97f, 0.99f),
            OnIptal);

        // Disclaimer
        BuildText(_kartRt, "Uyari",
            "Bahsi yükselten butonlar oyuncuyu hızlı tüketime yönlendirir.",
            12f, FontStyles.Italic, new Color(1f, 0.85f, 0.85f, 0.7f),
            new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.18f));

        StartCoroutine(GirisAnim());
    }

    void BuildHizliButon(RectTransform parent, int miktar, bool aktif, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject($"Hizli_{miktar}", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = aktif ? C("#1a0f2e", 0.85f) : C("#1a0f2e", 0.35f);
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.interactable = aktif;
        btn.transition = Selectable.Transition.ColorTint;
        var colors = btn.colors;
        colors.highlightedColor = C("#2a1a4a");
        colors.pressedColor = C("#3a2566");
        colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        btn.colors = colors;
        if (aktif) btn.onClick.AddListener(() => OnHizliSec(miktar));

        // Border
        var bGo = new GameObject("Border", typeof(RectTransform), typeof(Image));
        bGo.transform.SetParent(go.transform, false);
        var bRt = bGo.GetComponent<RectTransform>();
        bRt.anchorMin = Vector2.zero; bRt.anchorMax = Vector2.one;
        bRt.offsetMin = new Vector2(-1.5f, -1.5f); bRt.offsetMax = new Vector2(1.5f, 1.5f);
        var bImg = bGo.GetComponent<Image>();
        bImg.color = aktif ? C("#FAC775", 0.6f) : C("#FAC775", 0.2f);
        bImg.raycastTarget = false;
        bGo.transform.SetSiblingIndex(0);

        // Text
        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var tRt = textGo.AddComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
        tRt.offsetMin = Vector2.zero; tRt.offsetMax = Vector2.zero;
        var t = textGo.AddComponent<TextMeshProUGUI>();
        t.text = $"{miktar} TL";
        t.fontSize = 24f;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.color = aktif ? C("#FAC775") : new Color(0.5f, 0.45f, 0.35f, 0.7f);
        t.raycastTarget = false;
    }

    TMP_InputField BuildInput(RectTransform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject("Input", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var bg = go.GetComponent<Image>();
        bg.color = C("#0d0920", 0.95f);

        var border = new GameObject("Border", typeof(RectTransform), typeof(Image));
        border.transform.SetParent(go.transform, false);
        var bRt = border.GetComponent<RectTransform>();
        bRt.anchorMin = Vector2.zero; bRt.anchorMax = Vector2.one;
        bRt.offsetMin = new Vector2(-1.5f, -1.5f); bRt.offsetMax = new Vector2(1.5f, 1.5f);
        border.GetComponent<Image>().color = C("#FAC775", 0.5f);
        border.GetComponent<Image>().raycastTarget = false;
        border.transform.SetSiblingIndex(0);

        var input = go.AddComponent<TMP_InputField>();
        input.contentType = TMP_InputField.ContentType.IntegerNumber;
        input.characterLimit = 9;

        // Text Area
        var textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(go.transform, false);
        var taRt = textArea.GetComponent<RectTransform>();
        taRt.anchorMin = Vector2.zero; taRt.anchorMax = Vector2.one;
        taRt.offsetMin = new Vector2(10f, 6f); taRt.offsetMax = new Vector2(-10f, -6f);

        var phGo = new GameObject("Placeholder", typeof(RectTransform));
        phGo.transform.SetParent(textArea.transform, false);
        var phRt = phGo.AddComponent<RectTransform>();
        phRt.anchorMin = Vector2.zero; phRt.anchorMax = Vector2.one;
        phRt.offsetMin = Vector2.zero; phRt.offsetMax = Vector2.zero;
        var ph = phGo.AddComponent<TextMeshProUGUI>();
        ph.text = "Özel miktar...";
        ph.fontSize = 18f;
        ph.color = new Color(0.6f, 0.55f, 0.4f, 0.6f);
        ph.alignment = TextAlignmentOptions.Left;
        ph.raycastTarget = false;

        var txGo = new GameObject("Text", typeof(RectTransform));
        txGo.transform.SetParent(textArea.transform, false);
        var txRt = txGo.AddComponent<RectTransform>();
        txRt.anchorMin = Vector2.zero; txRt.anchorMax = Vector2.one;
        txRt.offsetMin = Vector2.zero; txRt.offsetMax = Vector2.zero;
        var tx = txGo.AddComponent<TextMeshProUGUI>();
        tx.fontSize = 18f;
        tx.color = C("#FAC775");
        tx.alignment = TextAlignmentOptions.Left;
        tx.raycastTarget = false;

        input.textViewport = (RectTransform)textArea.transform;
        input.textComponent = tx;
        input.placeholder = ph;
        return input;
    }

    void BuildButon(RectTransform parent, string yazi, Color bgRenk, Vector2 anchorMin, Vector2 anchorMax,
        Action onClick)
    {
        var go = new GameObject("Btn_" + yazi, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = bgRenk;
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        var colors = btn.colors;
        Color hi = bgRenk; hi.r = Mathf.Clamp01(hi.r * 1.2f); hi.g = Mathf.Clamp01(hi.g * 1.2f); hi.b = Mathf.Clamp01(hi.b * 1.2f);
        colors.highlightedColor = hi;
        btn.colors = colors;
        btn.onClick.AddListener(() => onClick?.Invoke());

        var txGo = new GameObject("Text", typeof(RectTransform));
        txGo.transform.SetParent(go.transform, false);
        var txRt = txGo.AddComponent<RectTransform>();
        txRt.anchorMin = Vector2.zero; txRt.anchorMax = Vector2.one;
        txRt.offsetMin = Vector2.zero; txRt.offsetMax = Vector2.zero;
        var tx = txGo.AddComponent<TextMeshProUGUI>();
        tx.text = yazi;
        tx.fontSize = 22f;
        tx.fontStyle = FontStyles.Bold;
        tx.color = Color.white;
        tx.alignment = TextAlignmentOptions.Center;
        tx.raycastTarget = false;
    }

    void BuildText(RectTransform parent, string isim, string yazi, float boyut, FontStyles stil, Color renk,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(isim, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = yazi;
        t.fontSize = boyut;
        t.fontStyle = stil;
        t.color = renk;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
    }

    IEnumerator GirisAnim()
    {
        const float sure = 0.25f;
        for (float t = 0f; t < sure; t += Time.unscaledDeltaTime)
        {
            float u = Mathf.Clamp01(t / sure);
            _overlayGroup.alpha = u;
            _kartGroup.alpha = u;
            float sc = Mathf.Lerp(0.9f, 1f, Mathf.SmoothStep(0f, 1f, u));
            _kartRt.localScale = new Vector3(sc, sc, 1f);
            yield return null;
        }
        _overlayGroup.alpha = 1f;
        _kartGroup.alpha = 1f;
        _kartRt.localScale = Vector3.one;
    }

    void OnHizliSec(int miktar) { Sec(miktar); }

    void OnManuelOnay()
    {
        if (_input == null) return;
        if (int.TryParse(_input.text, out int m) && m > 0)
        {
            if (m > _mevcutBakiye)
            {
                _input.text = "";
                Debug.LogWarning($"[BahisSecimPopup] {m} TL bakiyeden büyük ({_mevcutBakiye}). İptal.");
                return;
            }
            Sec(m);
        }
        else { _input.text = ""; }
    }

    void OnIptal() { Kapat(); }

    void Sec(int miktar)
    {
        Debug.Log($"[BahisSecimPopup] Seçilen bahis: {miktar} TL");
        var cb = _onSec; _onSec = null;
        Kapat();
        cb?.Invoke(miktar);
    }

    void Kapat()
    {
        _onSec = null;
        Destroy(gameObject);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) OnIptal();
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            OnManuelOnay();
    }

    static Color C(string hex, float alpha = 1f)
    {
        if (ColorUtility.TryParseHtmlString(hex, out var c)) { c.a = alpha; return c; }
        return new Color(1f, 1f, 1f, alpha);
    }
}
