using System;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AdminGirisDogrulama : MonoBehaviour
{
    private const string PP_ADMIN_KULLANICI_HASH = "PP_ADMIN_KULLANICI_HASH";
    private const string PP_ADMIN_SIFRE_HASH = "PP_ADMIN_SIFRE_HASH";

    private Action _basariliCallback;
    private Action _iptalCallback;

    private TMP_InputField _kullaniciInput;
    private TMP_InputField _sifreInput;
    private TMP_Text _uyariText;

    private void Update()
    {
        if (EventSystem.current == null) return;

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            var secili = EventSystem.current.currentSelectedGameObject;
            if (_kullaniciInput != null && secili == _kullaniciInput.gameObject)
            {
                OdaklaSifreAlani();
            }
            else if (_sifreInput != null && secili == _sifreInput.gameObject)
            {
                if (_kullaniciInput != null)
                {
                    EventSystem.current.SetSelectedGameObject(_kullaniciInput.gameObject);
                    _kullaniciInput.ActivateInputField();
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            var secili = EventSystem.current.currentSelectedGameObject;
            if (_kullaniciInput != null && secili == _kullaniciInput.gameObject)
                OdaklaSifreAlani();
            else if (_sifreInput != null && secili == _sifreInput.gameObject)
                DogrulaVeDevamEt();
        }
    }

    public static void Ac(Action basarili, Action iptal = null)
    {
        if (basarili == null) return;
        if (FindAnyObjectByType<AdminGirisDogrulama>() != null) return;

        GameObject go = new GameObject("AdminGirisDogrulama_Runtime");
        var pencere = go.AddComponent<AdminGirisDogrulama>();
        pencere._basariliCallback = basarili;
        pencere._iptalCallback = iptal;
        pencere.PencereyiKur();
    }

    private void PencereyiKur()
    {
        Canvas hedefCanvas = CanvasBulVeyaOlustur();
        if (hedefCanvas == null)
        {
            _iptalCallback?.Invoke();
            Destroy(gameObject);
            return;
        }

        transform.SetParent(hedefCanvas.transform, false);
        transform.SetAsLastSibling();

        var kokRt = gameObject.AddComponent<RectTransform>();
        kokRt.anchorMin = Vector2.zero;
        kokRt.anchorMax = Vector2.one;
        kokRt.offsetMin = Vector2.zero;
        kokRt.offsetMax = Vector2.zero;

        var arkaPlan = gameObject.AddComponent<Image>();
        arkaPlan.color = new Color(0f, 0f, 0f, 0.78f);
        arkaPlan.raycastTarget = true;

        GameObject panel = UIElemaniOlustur("Panel", transform, new Vector2(560f, 420f));
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.12f, 0.12f, 0.12f, 0.98f);

        TMP_Text baslik = TMPTextOlustur("Baslik", panel.transform, "YONETICI GIRISI", 34, TextAlignmentOptions.Center);
        RectTransform baslikRt = baslik.rectTransform;
        baslikRt.anchorMin = new Vector2(0.5f, 1f);
        baslikRt.anchorMax = new Vector2(0.5f, 1f);
        baslikRt.pivot = new Vector2(0.5f, 1f);
        baslikRt.anchoredPosition = new Vector2(0f, -24f);
        baslikRt.sizeDelta = new Vector2(500f, 60f);
        baslik.color = Color.white;

        // İlk açılışta varsayılan "admin"/"admin" hash'lerini kaydet.
        if (!PlayerPrefs.HasKey(PP_ADMIN_KULLANICI_HASH))
        {
            PlayerPrefs.SetString(PP_ADMIN_KULLANICI_HASH, SHA256Hash("admin"));
            PlayerPrefs.SetString(PP_ADMIN_SIFRE_HASH, SHA256Hash("admin"));
            PlayerPrefs.Save();
        }

        _kullaniciInput = InputOlustur(panel.transform, "Kullanici Adi", new Vector2(0f, 74f), false);
        _sifreInput = InputOlustur(panel.transform, "Sifre", new Vector2(0f, -8f), true);
        if (_sifreInput != null) _sifreInput.text = "";

        _uyariText = TMPTextOlustur("Uyari", panel.transform, "", 22, TextAlignmentOptions.Center);
        RectTransform uyariRt = _uyariText.rectTransform;
        uyariRt.anchorMin = new Vector2(0.5f, 0.5f);
        uyariRt.anchorMax = new Vector2(0.5f, 0.5f);
        uyariRt.pivot = new Vector2(0.5f, 0.5f);
        uyariRt.anchoredPosition = new Vector2(0f, -88f);
        uyariRt.sizeDelta = new Vector2(500f, 40f);
        _uyariText.color = new Color(1f, 0.35f, 0.35f, 1f);

        Button girisBtn = ButonOlustur(panel.transform, "GIRIS", new Vector2(-130f, -158f), new Color(0.12f, 0.63f, 0.27f, 1f));
        girisBtn.onClick.AddListener(DogrulaVeDevamEt);

        Button iptalBtn = ButonOlustur(panel.transform, "IPTAL", new Vector2(130f, -158f), new Color(0.65f, 0.14f, 0.14f, 1f));
        iptalBtn.onClick.AddListener(IptalEt);

        if (EventSystem.current == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        OdaklaSifreAlani();
    }

    private void OdaklaSifreAlani()
    {
        if (_sifreInput == null || EventSystem.current == null) return;
        EventSystem.current.SetSelectedGameObject(_sifreInput.gameObject);
        _sifreInput.ActivateInputField();
    }

    private void DogrulaVeDevamEt()
    {
        string kullanici = (_kullaniciInput != null ? _kullaniciInput.text : "").Trim();
        string sifre = (_sifreInput != null ? _sifreInput.text : "").Trim();

        string beklenenKullaniciHash = PlayerPrefs.GetString(PP_ADMIN_KULLANICI_HASH, "");
        string beklenenSifreHash = PlayerPrefs.GetString(PP_ADMIN_SIFRE_HASH, "");

        bool kullaniciDogru = string.Equals(SHA256Hash(kullanici), beklenenKullaniciHash, StringComparison.Ordinal);
        bool sifreDogru = string.Equals(SHA256Hash(sifre), beklenenSifreHash, StringComparison.Ordinal);

        if (!kullaniciDogru || !sifreDogru)
        {
            if (_uyariText != null)
                _uyariText.text = "Kullanici adi veya sifre hatali.";
            return;
        }

        _basariliCallback?.Invoke();
        Destroy(gameObject);
    }

    private static string SHA256Hash(string girdi)
    {
        using (var sha = SHA256.Create())
        {
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(girdi));
            var sb = new StringBuilder(64);
            foreach (byte b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }

    private void IptalEt()
    {
        _iptalCallback?.Invoke();
        Destroy(gameObject);
    }

    private static Canvas CanvasBulVeyaOlustur()
    {
        var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] == null) continue;
            if (canvases[i].isActiveAndEnabled) return canvases[i];
        }

        GameObject canvasGo = new GameObject("RuntimeAdminGirisCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 12000;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static GameObject UIElemaniOlustur(string ad, Transform parent, Vector2 boyut)
    {
        GameObject go = new GameObject(ad);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = boyut;
        return go;
    }

    private static TMP_Text TMPTextOlustur(string ad, Transform parent, string metin, float boyut, TextAlignmentOptions hiza)
    {
        GameObject go = new GameObject(ad);
        go.transform.SetParent(parent, false);
        TMP_Text txt = go.AddComponent<TextMeshProUGUI>();
        txt.text = metin;
        txt.fontSize = boyut;
        txt.alignment = hiza;
        txt.enableWordWrapping = false;
        return txt;
    }

    private static TMP_InputField InputOlustur(Transform parent, string placeholder, Vector2 konum, bool sifre)
    {
        GameObject go = UIElemaniOlustur(placeholder + "_Input", parent, new Vector2(460f, 62f));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = konum;

        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.18f, 0.18f, 1f);

        TMP_InputField input = go.AddComponent<TMP_InputField>();
        input.contentType = sifre ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.caretWidth = 2;
        input.customCaretColor = true;
        input.caretColor = Color.white;

        TMP_Text text = TMPTextOlustur("Text", go.transform, "", 24f, TextAlignmentOptions.MidlineLeft);
        RectTransform textRt = text.rectTransform;
        textRt.anchorMin = new Vector2(0f, 0f);
        textRt.anchorMax = new Vector2(1f, 1f);
        textRt.offsetMin = new Vector2(18f, 6f);
        textRt.offsetMax = new Vector2(-18f, -6f);
        text.color = Color.white;

        TMP_Text place = TMPTextOlustur("Placeholder", go.transform, placeholder, 22f, TextAlignmentOptions.MidlineLeft);
        RectTransform placeRt = place.rectTransform;
        placeRt.anchorMin = new Vector2(0f, 0f);
        placeRt.anchorMax = new Vector2(1f, 1f);
        placeRt.offsetMin = new Vector2(18f, 6f);
        placeRt.offsetMax = new Vector2(-18f, -6f);
        place.color = new Color(1f, 1f, 1f, 0.45f);

        input.textComponent = text;
        input.placeholder = place;
        return input;
    }

    private static Button ButonOlustur(Transform parent, string metin, Vector2 konum, Color renk)
    {
        GameObject go = UIElemaniOlustur(metin + "_Buton", parent, new Vector2(200f, 62f));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = konum;

        Image img = go.AddComponent<Image>();
        img.color = renk;

        Button btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = renk;
        colors.highlightedColor = new Color(renk.r * 1.08f, renk.g * 1.08f, renk.b * 1.08f, 1f);
        colors.pressedColor = new Color(renk.r * 0.86f, renk.g * 0.86f, renk.b * 0.86f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        btn.colors = colors;

        TMP_Text txt = TMPTextOlustur("Text", go.transform, metin, 24f, TextAlignmentOptions.Center);
        RectTransform txtRt = txt.rectTransform;
        txtRt.anchorMin = new Vector2(0f, 0f);
        txtRt.anchorMax = new Vector2(1f, 1f);
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;
        txt.color = Color.white;
        txt.fontStyle = FontStyles.Bold;

        return btn;
    }
}
