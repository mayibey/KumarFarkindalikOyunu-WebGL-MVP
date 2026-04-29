using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bahis seçim pop-up'ı: 6 hızlı miktar + opsiyonel manuel input + onay.
/// LEGACY Unity UI (Text, InputField, Font.LegacyRuntime) — WebGL stripping güvenli.
/// Static Goster çağrısıyla Canvas'a build edilir, callback ile sonuç döner.
/// </summary>
public class BahisSecimPopup : MonoBehaviour
{
    private Action<int> _onSec;
    private CanvasGroup _overlayGroup;
    private CanvasGroup _kartGroup;
    private RectTransform _kartRt;
    private InputField _input;
    private int _mevcutBakiye;
    private Font _defaultFont;
    private static readonly int[] HIZLI_MIKTARLAR = { 50, 100, 200, 300, 500, 1000 };

    public static BahisSecimPopup Goster(Canvas canvas, int mevcutBakiye, Action<int> onSec)
    {
        try
        {
            if (canvas == null) { Debug.LogError("[BahisSecimPopup] Goster: canvas NULL"); return null; }
            Debug.Log($"[BahisSecimPopup] Goster: canvas={canvas.name} bakiye={mevcutBakiye}");
            var go = new GameObject("[BahisSecimPopup]", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            go.transform.SetAsLastSibling();
            var m = go.AddComponent<BahisSecimPopup>();
            m._onSec = onSec;
            m._mevcutBakiye = mevcutBakiye;
            m.Build();
            return m;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BahisSecimPopup] Goster HATA: {ex.Message}\n{ex.StackTrace}");
            return null;
        }
    }

    void Build()
    {
        try { BuildIc(); }
        catch (Exception ex)
        {
            Debug.LogError($"[BahisSecimPopup] Build HATA detay: {ex.Message}\n{ex.StackTrace}");
            if (gameObject != null) Destroy(gameObject);
        }
    }

    void BuildIc()
    {
        Debug.Log("[BahisSecimPopup] BuildIc başladı");

        // 1. Default font yükle (legacy UI için zorunlu)
        try
        {
            _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_defaultFont == null) _defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (_defaultFont == null) Debug.LogWarning("[BahisSecimPopup] Default font NULL — Text component font'suz oluşturulacak");
            else Debug.Log($"[BahisSecimPopup] Default font: {_defaultFont.name}");
        }
        catch (Exception ex) { Debug.LogWarning($"[BahisSecimPopup] Font yükleme: {ex.Message}"); }

        var rt = (RectTransform)transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        // 2. Overlay
        try
        {
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
            var oBtn = overlay.AddComponent<Button>();
            oBtn.transition = Selectable.Transition.None;
            oBtn.onClick.AddListener(OnIptal);
            Debug.Log("[BahisSecimPopup] Overlay tamam");
        }
        catch (Exception ex) { Debug.LogError($"[BahisSecimPopup] Overlay HATA: {ex.Message}"); }

        // 3. Kart
        try
        {
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

            // Altın çerçeve
            var border = new GameObject("Border", typeof(RectTransform), typeof(Image));
            border.transform.SetParent(kart.transform, false);
            var bRt = border.GetComponent<RectTransform>();
            bRt.anchorMin = Vector2.zero; bRt.anchorMax = Vector2.one;
            bRt.offsetMin = new Vector2(-3f, -3f); bRt.offsetMax = new Vector2(3f, 3f);
            border.GetComponent<Image>().color = C("#FAC775", 1f);
            border.GetComponent<Image>().raycastTarget = false;
            border.transform.SetSiblingIndex(0);
            Debug.Log("[BahisSecimPopup] Kart tamam");
        }
        catch (Exception ex) { Debug.LogError($"[BahisSecimPopup] Kart HATA: {ex.Message}"); }

        if (_kartRt == null) { Debug.LogError("[BahisSecimPopup] _kartRt NULL — abort"); return; }

        // 4. Başlık + bakiye
        try
        {
            BuildText(_kartRt, "Baslik", "Şanslı Bahis Seç!", 28, FontStyle.Bold,
                C("#FAC775"), new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.97f));
            BuildText(_kartRt, "Bakiye", "Bakiye: " + _mevcutBakiye + " TL",
                17, FontStyle.Normal, C("#cdb78a"),
                new Vector2(0.05f, 0.79f), new Vector2(0.95f, 0.86f));
            Debug.Log("[BahisSecimPopup] Başlık + Bakiye tamam");
        }
        catch (Exception ex) { Debug.LogError($"[BahisSecimPopup] Başlık HATA: {ex.Message}"); }

        // 5. 6 hızlı buton grid (3x2)
        try
        {
            for (int i = 0; i < HIZLI_MIKTARLAR.Length; i++)
            {
                int miktar = HIZLI_MIKTARLAR[i];
                int row = i / 3;
                int col = i % 3;
                float xMin = 0.05f + col * 0.31f;
                float xMax = xMin + 0.28f;
                float yMax = 0.74f - row * 0.13f;
                float yMin = yMax - 0.10f;
                bool yeterli = _mevcutBakiye >= miktar;
                BuildHizliButon(_kartRt, miktar, yeterli,
                    new Vector2(xMin, yMin), new Vector2(xMax, yMax));
            }
            Debug.Log("[BahisSecimPopup] 6 hızlı buton tamam");
        }
        catch (Exception ex) { Debug.LogError($"[BahisSecimPopup] Hızlı butonlar HATA: {ex.Message}"); }

        // 6. Manuel input + onay (FALLBACK: input fail olursa sadece butonlar yeterli)
        try
        {
            BuildText(_kartRt, "ManuelLabel", "Veya özel miktar gir:", 15, FontStyle.Normal,
                C("#cdb78a"), new Vector2(0.05f, 0.40f), new Vector2(0.95f, 0.46f));
            _input = BuildInput(_kartRt, new Vector2(0.05f, 0.28f), new Vector2(0.65f, 0.39f));
            BuildButon(_kartRt, "Onayla", C("#3c8c3c"),
                new Vector2(0.67f, 0.28f), new Vector2(0.95f, 0.39f), OnManuelOnay);
            Debug.Log("[BahisSecimPopup] Manuel input + onay tamam (input null mu: " + (_input == null) + ")");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[BahisSecimPopup] Manuel input HATA (fallback - sadece quick butonlar): {ex.Message}");
            _input = null;
        }

        // 7. İptal X
        try
        {
            BuildButon(_kartRt, "X", C("#a83a3a"),
                new Vector2(0.88f, 0.91f), new Vector2(0.97f, 0.99f), OnIptal);
        }
        catch (Exception ex) { Debug.LogError($"[BahisSecimPopup] İptal X HATA: {ex.Message}"); }

        // 8. Disclaimer
        try
        {
            BuildText(_kartRt, "Uyari",
                "Bahsi yukselten butonlar oyuncuyu hizli tuketime yonlendirir.",
                11, FontStyle.Italic, new Color(1f, 0.85f, 0.85f, 0.7f),
                new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.18f));
        }
        catch (Exception ex) { Debug.LogWarning($"[BahisSecimPopup] Disclaimer HATA: {ex.Message}"); }

        StartCoroutine(GirisAnim());
        Debug.Log("[BahisSecimPopup] BuildIc tamamlandı");
    }

    void BuildText(RectTransform parent, string isim, string yazi, int boyut, FontStyle stil,
        Color renk, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(isim, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var t = go.AddComponent<Text>();
        if (_defaultFont != null) t.font = _defaultFont;
        t.text = yazi;
        t.fontSize = boyut;
        t.fontStyle = stil;
        t.color = renk;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
    }

    void BuildHizliButon(RectTransform parent, int miktar, bool aktif,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject("Hizli_" + miktar, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = aktif ? C("#1a0f2e", 0.85f) : C("#1a0f2e", 0.35f);
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.interactable = aktif;
        if (aktif)
        {
            int captured = miktar;
            btn.onClick.AddListener(() => Sec(captured));
        }

        // Border
        var bGo = new GameObject("Border", typeof(RectTransform), typeof(Image));
        bGo.transform.SetParent(go.transform, false);
        var bRt = bGo.GetComponent<RectTransform>();
        bRt.anchorMin = Vector2.zero; bRt.anchorMax = Vector2.one;
        bRt.offsetMin = new Vector2(-1.5f, -1.5f); bRt.offsetMax = new Vector2(1.5f, 1.5f);
        bGo.GetComponent<Image>().color = aktif ? C("#FAC775", 0.6f) : C("#FAC775", 0.2f);
        bGo.GetComponent<Image>().raycastTarget = false;
        bGo.transform.SetSiblingIndex(0);

        // Text
        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var tRt = textGo.AddComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
        tRt.offsetMin = Vector2.zero; tRt.offsetMax = Vector2.zero;
        var t = textGo.AddComponent<Text>();
        if (_defaultFont != null) t.font = _defaultFont;
        t.text = miktar + " TL";
        t.fontSize = 22;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = aktif ? C("#FAC775") : new Color(0.5f, 0.45f, 0.35f, 0.7f);
        t.raycastTarget = false;
    }

    InputField BuildInput(RectTransform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject("Input", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = C("#0d0920", 0.95f);

        // Border
        var bGo = new GameObject("Border", typeof(RectTransform), typeof(Image));
        bGo.transform.SetParent(go.transform, false);
        var bRt = bGo.GetComponent<RectTransform>();
        bRt.anchorMin = Vector2.zero; bRt.anchorMax = Vector2.one;
        bRt.offsetMin = new Vector2(-1.5f, -1.5f); bRt.offsetMax = new Vector2(1.5f, 1.5f);
        bGo.GetComponent<Image>().color = C("#FAC775", 0.5f);
        bGo.GetComponent<Image>().raycastTarget = false;
        bGo.transform.SetSiblingIndex(0);

        // Placeholder
        var phGo = new GameObject("Placeholder", typeof(RectTransform));
        phGo.transform.SetParent(go.transform, false);
        var phRt = phGo.AddComponent<RectTransform>();
        phRt.anchorMin = Vector2.zero; phRt.anchorMax = Vector2.one;
        phRt.offsetMin = new Vector2(10f, 6f); phRt.offsetMax = new Vector2(-10f, -6f);
        var ph = phGo.AddComponent<Text>();
        if (_defaultFont != null) ph.font = _defaultFont;
        ph.text = "Ozel miktar...";
        ph.fontSize = 17;
        ph.color = new Color(0.6f, 0.55f, 0.4f, 0.6f);
        ph.alignment = TextAnchor.MiddleLeft;
        ph.raycastTarget = false;

        // Text
        var txGo = new GameObject("Text", typeof(RectTransform));
        txGo.transform.SetParent(go.transform, false);
        var txRt = txGo.AddComponent<RectTransform>();
        txRt.anchorMin = Vector2.zero; txRt.anchorMax = Vector2.one;
        txRt.offsetMin = new Vector2(10f, 6f); txRt.offsetMax = new Vector2(-10f, -6f);
        var tx = txGo.AddComponent<Text>();
        if (_defaultFont != null) tx.font = _defaultFont;
        tx.fontSize = 17;
        tx.color = C("#FAC775");
        tx.alignment = TextAnchor.MiddleLeft;
        tx.supportRichText = false;
        tx.raycastTarget = false;

        var input = go.AddComponent<InputField>();
        input.contentType = InputField.ContentType.IntegerNumber;
        input.characterLimit = 9;
        input.textComponent = tx;
        input.placeholder = ph;
        return input;
    }

    void BuildButon(RectTransform parent, string yazi, Color bgRenk,
        Vector2 anchorMin, Vector2 anchorMax, Action onClick)
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
        btn.onClick.AddListener(() => { try { onClick?.Invoke(); } catch (Exception e) { Debug.LogError("[BahisSecimPopup] Btn click HATA: " + e.Message); } });

        var txGo = new GameObject("Text", typeof(RectTransform));
        txGo.transform.SetParent(go.transform, false);
        var txRt = txGo.AddComponent<RectTransform>();
        txRt.anchorMin = Vector2.zero; txRt.anchorMax = Vector2.one;
        txRt.offsetMin = Vector2.zero; txRt.offsetMax = Vector2.zero;
        var tx = txGo.AddComponent<Text>();
        if (_defaultFont != null) tx.font = _defaultFont;
        tx.text = yazi;
        tx.fontSize = 20;
        tx.fontStyle = FontStyle.Bold;
        tx.color = Color.white;
        tx.alignment = TextAnchor.MiddleCenter;
        tx.raycastTarget = false;
    }

    IEnumerator GirisAnim()
    {
        const float sure = 0.25f;
        for (float t = 0f; t < sure; t += Time.unscaledDeltaTime)
        {
            float u = Mathf.Clamp01(t / sure);
            if (_overlayGroup != null) _overlayGroup.alpha = u;
            if (_kartGroup != null) _kartGroup.alpha = u;
            if (_kartRt != null)
            {
                float sc = Mathf.Lerp(0.9f, 1f, Mathf.SmoothStep(0f, 1f, u));
                _kartRt.localScale = new Vector3(sc, sc, 1f);
            }
            yield return null;
        }
        if (_overlayGroup != null) _overlayGroup.alpha = 1f;
        if (_kartGroup != null) _kartGroup.alpha = 1f;
        if (_kartRt != null) _kartRt.localScale = Vector3.one;
    }

    void OnManuelOnay()
    {
        if (_input == null) { Debug.LogWarning("[BahisSecimPopup] _input null, manuel onay atlandı"); return; }
        if (int.TryParse(_input.text, out int m) && m > 0)
        {
            if (m > _mevcutBakiye)
            {
                _input.text = "";
                Debug.LogWarning($"[BahisSecimPopup] {m} > bakiye {_mevcutBakiye}, iptal");
                return;
            }
            Sec(m);
        }
        else { _input.text = ""; }
    }

    void OnIptal() { Kapat(); }

    void Sec(int miktar)
    {
        Debug.Log("[BahisSecimPopup] Seçilen: " + miktar + " TL");
        var cb = _onSec; _onSec = null;
        Kapat();
        try { cb?.Invoke(miktar); }
        catch (Exception ex) { Debug.LogError("[BahisSecimPopup] Sec callback HATA: " + ex.Message); }
    }

    void Kapat()
    {
        _onSec = null;
        if (gameObject != null) Destroy(gameObject);
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
