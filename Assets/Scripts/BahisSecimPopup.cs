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

    private static Font _cachedFont;
    /// <summary>WebGL stripping güvenli font discovery: 1) sahnedeki Text, 2) builtin, 3) OS dinamik fallback.</summary>
    private static Font GetSafeFont()
    {
        if (_cachedFont != null) return _cachedFont;

        // 1. Sahnede aktif legacy Text varsa font'unu reuse et (en güvenli)
        var existingText = UnityEngine.Object.FindObjectOfType<Text>(true);
        if (existingText != null && existingText.font != null)
        {
            _cachedFont = existingText.font;
            Debug.Log("[BahisSecimPopup] Font kaynak: sahnedeki legacy Text — " + _cachedFont.name);
            return _cachedFont;
        }

        // 2. Resources.GetBuiltinResource fallback (Editor'da çalışır, WebGL'de null dönebilir)
        try
        {
            _cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_cachedFont == null) _cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (_cachedFont != null)
            {
                Debug.Log("[BahisSecimPopup] Font kaynak: builtin — " + _cachedFont.name);
                return _cachedFont;
            }
        }
        catch (Exception ex) { Debug.LogWarning("[BahisSecimPopup] Builtin font hatası: " + ex.Message); }

        // 3. Son çare: OS'tan dinamik font oluştur (her platformda çalışır)
        try
        {
            _cachedFont = Font.CreateDynamicFontFromOSFont("Arial", 16);
            if (_cachedFont != null)
            {
                Debug.LogWarning("[BahisSecimPopup] Font kaynak: OS dinamik fallback (Arial)");
                return _cachedFont;
            }
        }
        catch (Exception ex) { Debug.LogError("[BahisSecimPopup] OS font hatası: " + ex.Message); }

        Debug.LogError("[BahisSecimPopup] HİÇBİR FONT bulunamadı — Text'ler render edilmeyecek");
        return null;
    }

    public static BahisSecimPopup Goster(Canvas canvas, int mevcutBakiye, Action<int> onSec)
    {
        try
        {
            if (canvas == null) { Debug.LogError("[BahisSecimPopup] Goster: canvas NULL"); return null; }
            Debug.Log("[BahisSecimPopup] Goster çağrıldı — panel build başlıyor");
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

        // 1. Default font yükle — GetSafeFont() 3-katmanlı discovery (sahne Text → builtin → OS dinamik)
        _defaultFont = GetSafeFont();

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
            oImg.color = new Color(0f, 0f, 0f, 0.75f);
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
            kartImg.color = C("#15102a", 1f);
            kartImg.raycastTarget = true;
            _kartGroup = kart.GetComponent<CanvasGroup>();
            _kartGroup.alpha = 0f;

            // Parlak altın çerçeve (1.5px, alpha 0.5)
            var border = new GameObject("Border", typeof(RectTransform), typeof(Image));
            border.transform.SetParent(kart.transform, false);
            var bRt = border.GetComponent<RectTransform>();
            bRt.anchorMin = Vector2.zero; bRt.anchorMax = Vector2.one;
            bRt.offsetMin = new Vector2(-1.5f, -1.5f); bRt.offsetMax = new Vector2(1.5f, 1.5f);
            border.GetComponent<Image>().color = C("#E8B547", 0.5f);
            border.GetComponent<Image>().raycastTarget = false;
            border.transform.SetSiblingIndex(0);
            Debug.Log("[BahisSecimPopup] Kart tamam");
        }
        catch (Exception ex) { Debug.LogError($"[BahisSecimPopup] Kart HATA: {ex.Message}"); }

        if (_kartRt == null) { Debug.LogError("[BahisSecimPopup] _kartRt NULL — abort"); return; }

        // 4. Başlık badge (altın yuvarlak ₺) + başlık + alt yardım metni + bakiye (sağ üst)
        try
        {
            // Badge: 42x42 sol üstte parlak altın daire (Image + Text "₺")
            var badge = new GameObject("BaslikBadge", typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(_kartRt, false);
            var bdRt = badge.GetComponent<RectTransform>();
            bdRt.anchorMin = new Vector2(0f, 1f); bdRt.anchorMax = new Vector2(0f, 1f);
            bdRt.pivot = new Vector2(0f, 1f);
            bdRt.anchoredPosition = new Vector2(20f, -20f);
            bdRt.sizeDelta = new Vector2(42f, 42f);
            badge.GetComponent<Image>().color = C("#E8B547", 1f);
            // Badge içi "₺" — koyu mor (altın üzerinde doğal kontrast)
            var badgeText = new GameObject("BadgeText", typeof(RectTransform), typeof(Text));
            badgeText.transform.SetParent(badge.transform, false);
            var bdTxRt = badgeText.GetComponent<RectTransform>();
            bdTxRt.anchorMin = Vector2.zero; bdTxRt.anchorMax = Vector2.one;
            bdTxRt.offsetMin = Vector2.zero; bdTxRt.offsetMax = Vector2.zero;
            var bdTx = badgeText.GetComponent<Text>();
            if (_defaultFont != null) bdTx.font = _defaultFont;
            bdTx.text = "₺";
            bdTx.fontSize = 26; bdTx.fontStyle = FontStyle.Bold;
            bdTx.color = C("#1a0f1a", 1f); bdTx.alignment = TextAnchor.MiddleCenter;
            bdTx.raycastTarget = false;

            // Başlık (badge sağında): "Bahis Seç" parlak altın 28px bold
            BuildText(_kartRt, "Baslik", "Bahis Seç", 28, FontStyle.Bold,
                C("#E8B547"), new Vector2(0.16f, 0.88f), new Vector2(0.70f, 0.97f));

            // Alt yardım metni: yumuşak gri 13px (silik ama okunabilir)
            BuildText(_kartRt, "AltYardim", "Sonraki spinde kullanılacak bahis miktarı",
                13, FontStyle.Normal, C("#b0a090"),
                new Vector2(0.05f, 0.81f), new Vector2(0.95f, 0.87f));

            // Bakiye sağ üste yaslı: parlak altın 14px
            BuildText(_kartRt, "Bakiye", _mevcutBakiye + " TL bakiye",
                14, FontStyle.Normal, C("#E8B547"),
                new Vector2(0.55f, 0.91f), new Vector2(0.95f, 0.97f));

            Debug.Log("[BahisSecimPopup] Başlık badge + bakiye tamam");
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
                // Daha nefesli grid: spacing artırıldı
                float xMin = 0.06f + col * 0.31f;
                float xMax = xMin + 0.27f;
                float yMax = 0.74f - row * 0.155f;
                float yMin = yMax - 0.12f;
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
            BuildText(_kartRt, "ManuelLabel", "Veya özel miktar gir:", 14, FontStyle.Normal,
                C("#9b8a7a"), new Vector2(0.05f, 0.40f), new Vector2(0.95f, 0.46f));
            _input = BuildInput(_kartRt, new Vector2(0.05f, 0.28f), new Vector2(0.65f, 0.39f));
            BuildButon(_kartRt, "Onayla", C("#3a8c4a", 1f),
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
            // İptal X — minimal: transparan arka plan, soluk gri "X" 20px
            BuildButon(_kartRt, "X", new Color(0f, 0f, 0f, 0f),
                new Vector2(0.91f, 0.92f), new Vector2(0.99f, 0.99f), OnIptal, C("#b0a090", 1f), 20);
        }
        catch (Exception ex) { Debug.LogError($"[BahisSecimPopup] İptal X HATA: {ex.Message}"); }

        // 8. Disclaimer
        try
        {
            BuildText(_kartRt, "Uyari",
                "Bahsi yükselten butonlar oyuncuyu daha fazla kayba sürükler",
                11, FontStyle.Italic, C("#5a5050", 1f),
                new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.12f));
        }
        catch (Exception ex) { Debug.LogWarning($"[BahisSecimPopup] Disclaimer HATA: {ex.Message}"); }

        StartCoroutine(GirisAnim());
        var f = GetSafeFont();
        Debug.Log($"[BahisSecimPopup] BuildIc tamamlandı — 6 hızlı buton, font: {(f != null ? f.name : "NULL")}");
    }

    void BuildText(RectTransform parent, string isim, string yazi, int boyut, FontStyle stil,
        Color renk, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(isim, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
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
        // VİSNE TEMA: parlak bordo
        img.color = aktif ? C("#7a1f1f", 1f) : C("#3a1a1a", 0.4f);
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.interactable = aktif;
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.18f, 1.05f, 1.05f, 1f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        btn.colors = colors;
        if (aktif)
        {
            int captured = miktar;
            btn.onClick.AddListener(() => Sec(captured));
        }

        // Belirgin altın-kahve çerçeve (1.5px, alpha 0.7)
        var bGo = new GameObject("Border", typeof(RectTransform), typeof(Image));
        bGo.transform.SetParent(go.transform, false);
        var bRt = bGo.GetComponent<RectTransform>();
        bRt.anchorMin = Vector2.zero; bRt.anchorMax = Vector2.one;
        bRt.offsetMin = new Vector2(-1.5f, -1.5f); bRt.offsetMax = new Vector2(1.5f, 1.5f);
        bGo.GetComponent<Image>().color = aktif ? C("#6b4a2a", 0.7f) : C("#6b4a2a", 0.3f);
        bGo.GetComponent<Image>().raycastTarget = false;
        bGo.transform.SetSiblingIndex(0);

        // Highlight overlay: üst kenarda 2px parlak çizgi (ışık dümeni hissi)
        if (aktif)
        {
            var hlGo = new GameObject("Highlight", typeof(RectTransform), typeof(Image));
            hlGo.transform.SetParent(go.transform, false);
            var hlRt = hlGo.GetComponent<RectTransform>();
            hlRt.anchorMin = new Vector2(0f, 1f); hlRt.anchorMax = new Vector2(1f, 1f);
            hlRt.pivot = new Vector2(0.5f, 1f);
            hlRt.anchoredPosition = new Vector2(0f, 0f);
            hlRt.sizeDelta = new Vector2(0f, 2f);
            hlGo.GetComponent<Image>().color = C("#9a2828", 1f);
            hlGo.GetComponent<Image>().raycastTarget = false;
        }

        // Text + outline (kontrast için)
        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var tRt = textGo.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
        tRt.offsetMin = Vector2.zero; tRt.offsetMax = Vector2.zero;
        var t = textGo.AddComponent<Text>();
        if (_defaultFont != null) t.font = _defaultFont;
        t.text = miktar + " TL";
        t.fontSize = 20;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = aktif ? C("#FFD700", 1f) : new Color(0.5f, 0.45f, 0.35f, 0.55f);
        t.raycastTarget = false;
        var ol = textGo.AddComponent<Outline>();
        ol.effectColor = new Color(0f, 0f, 0f, 1f);
        ol.effectDistance = new Vector2(1.2f, -1.2f);
    }

    InputField BuildInput(RectTransform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject("Input", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = C("#1f1428", 1f);

        // Belirgin kahve çerçeve (1px alpha 0.6)
        var bGo = new GameObject("Border", typeof(RectTransform), typeof(Image));
        bGo.transform.SetParent(go.transform, false);
        var bRt = bGo.GetComponent<RectTransform>();
        bRt.anchorMin = Vector2.zero; bRt.anchorMax = Vector2.one;
        bRt.offsetMin = new Vector2(-1f, -1f); bRt.offsetMax = new Vector2(1f, 1f);
        bGo.GetComponent<Image>().color = C("#3a2a20", 0.6f);
        bGo.GetComponent<Image>().raycastTarget = false;
        bGo.transform.SetSiblingIndex(0);

        // Placeholder — daha okunabilir soluk italic
        var phGo = new GameObject("Placeholder", typeof(RectTransform));
        phGo.transform.SetParent(go.transform, false);
        var phRt = phGo.GetComponent<RectTransform>();
        phRt.anchorMin = Vector2.zero; phRt.anchorMax = Vector2.one;
        phRt.offsetMin = new Vector2(10f, 6f); phRt.offsetMax = new Vector2(-10f, -6f);
        var ph = phGo.AddComponent<Text>();
        if (_defaultFont != null) ph.font = _defaultFont;
        ph.text = "Özel miktar...";
        ph.fontSize = 14;
        ph.fontStyle = FontStyle.Italic;
        ph.color = C("#807060", 1f);
        ph.alignment = TextAnchor.MiddleLeft;
        ph.raycastTarget = false;

        // Yazılan text — parlak altın casino style
        var txGo = new GameObject("Text", typeof(RectTransform));
        txGo.transform.SetParent(go.transform, false);
        var txRt = txGo.GetComponent<RectTransform>();
        txRt.anchorMin = Vector2.zero; txRt.anchorMax = Vector2.one;
        txRt.offsetMin = new Vector2(10f, 6f); txRt.offsetMax = new Vector2(-10f, -6f);
        var tx = txGo.AddComponent<Text>();
        if (_defaultFont != null) tx.font = _defaultFont;
        tx.fontSize = 16;
        tx.color = C("#E8B547", 1f);
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
        Vector2 anchorMin, Vector2 anchorMax, Action onClick, Color? textRenk = null, int fontBoyut = 18)
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
        var txRt = txGo.GetComponent<RectTransform>();
        txRt.anchorMin = Vector2.zero; txRt.anchorMax = Vector2.one;
        txRt.offsetMin = Vector2.zero; txRt.offsetMax = Vector2.zero;
        var tx = txGo.AddComponent<Text>();
        if (_defaultFont != null) tx.font = _defaultFont;
        tx.text = yazi;
        tx.fontSize = fontBoyut;
        tx.fontStyle = FontStyle.Bold;
        tx.color = textRenk ?? Color.white;
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
