using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SenaryoDurumPaneli kökünde: ok ile panel sağa kayarak gizlenir; tekrar tıklanınca sola açılır.
/// Ok, panel ile aynı canvas üstünde kardeş nesne olarak sağ kenarda kalır (panel kayınca ekranda görünür kalır).
/// </summary>
[DisallowMultipleComponent]
public class SenaryoDurumPaneliKaydirici : MonoBehaviour
{
    [Tooltip("Panel genişliğiyle çarpılır; sağa ne kadar kaydırılacağı.")]
    [SerializeField] float genislikCarpani = 0.92f;
    [Tooltip("Animasyon süresi (saniye).")]
    [SerializeField] float animasyonSuresi = 0.32f;
    [SerializeField] float minimumKaydirmaPx = 260f;

    RectTransform _panelRt;
    Vector2 _acikAnchored;
    bool _gizli;
    bool _animasyonCalisiyor;
    Button _okBtn;
    TextMeshProUGUI _okYazi;
    RectTransform _okRt;

    void Awake()
    {
        _panelRt = GetComponent<RectTransform>();
        if (_panelRt != null)
            _acikAnchored = _panelRt.anchoredPosition;
    }

    void Start()
    {
        if (_panelRt == null) return;
        TMP_Text fontOrnegi = GetComponentInChildren<TextMeshProUGUI>(true);
        OlusturOkVeBagla(fontOrnegi);
    }

    void OlusturOkVeBagla(TMP_Text fontOrnegi)
    {
        Transform ust = _panelRt.parent;
        if (ust == null) return;

        var go = new GameObject("SenaryoDurumOkButonu");
        go.layer = gameObject.layer;
        go.transform.SetParent(ust, false);
        go.transform.SetAsLastSibling();
        var okRt = go.AddComponent<RectTransform>();
        _okRt = okRt;
        okRt.anchorMin = new Vector2(1f, 0.5f);
        okRt.anchorMax = new Vector2(1f, 0.5f);
        okRt.pivot = new Vector2(1f, 0.5f);
        okRt.anchoredPosition = new Vector2(-6f, _panelRt.anchoredPosition.y);
        okRt.sizeDelta = new Vector2(56f, 140f);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.08f, 0.12f, 0.18f, 0.72f);
        img.raycastTarget = true;

        _okBtn = go.AddComponent<Button>();
        _okBtn.targetGraphic = img;
        var colors = _okBtn.colors;
        colors.highlightedColor = new Color(0.25f, 0.35f, 0.5f, 0.9f);
        colors.pressedColor = new Color(0.15f, 0.22f, 0.32f, 0.95f);
        _okBtn.colors = colors;

        var yaziGo = new GameObject("OkMetni");
        yaziGo.layer = gameObject.layer;
        yaziGo.transform.SetParent(go.transform, false);
        var yRt = yaziGo.AddComponent<RectTransform>();
        yRt.anchorMin = Vector2.zero;
        yRt.anchorMax = Vector2.one;
        yRt.offsetMin = Vector2.zero;
        yRt.offsetMax = Vector2.zero;

        _okYazi = yaziGo.AddComponent<TextMeshProUGUI>();
        _okYazi.raycastTarget = false;
        _okYazi.text = "▶";
        _okYazi.fontSize = 40;
        _okYazi.alignment = TextAlignmentOptions.Center;
        _okYazi.color = Color.white;
        if (fontOrnegi is TextMeshProUGUI ugui && ugui.font != null)
        {
            _okYazi.font = ugui.font;
            _okYazi.fontSharedMaterial = ugui.fontSharedMaterial;
        }

        _okBtn.onClick.AddListener(OkaTiklandi);
        Canvas.ForceUpdateCanvases();
    }

    void OnRectTransformDimensionsChange()
    {
        if (!Application.isPlaying || _panelRt == null || _okRt == null || _animasyonCalisiyor)
            return;
        // Farklı oranlarda okun Y hizası panelle senkron kalsın.
        _okRt.anchoredPosition = new Vector2(_okRt.anchoredPosition.x, _panelRt.anchoredPosition.y);
    }

    void OkaTiklandi()
    {
        if (_animasyonCalisiyor || _panelRt == null) return;

        Vector2 hedef;
        if (_gizli)
        {
            hedef = _acikAnchored;
            _gizli = false;
        }
        else
        {
            hedef = _acikAnchored + new Vector2(HesaplaKaydirmaX(), 0f);
            _gizli = true;
        }

        GuncelleOkYazisi();
        StartCoroutine(KaydirAnimasyonu(hedef));
    }

    void GuncelleOkYazisi()
    {
        if (_okYazi == null) return;
        _okYazi.text = _gizli ? "◀" : "▶";
    }

    float HesaplaKaydirmaX()
    {
        float w = Mathf.Abs(_panelRt.rect.width);
        if (w < 80f)
            w = minimumKaydirmaPx;
        return Mathf.Max(w * genislikCarpani, minimumKaydirmaPx);
    }

    IEnumerator KaydirAnimasyonu(Vector2 hedefAnchored)
    {
        _animasyonCalisiyor = true;
        if (_okBtn != null)
            _okBtn.interactable = false;
        Vector2 bas = _panelRt.anchoredPosition;
        float t = 0f;
        float T = Mathf.Max(0.05f, animasyonSuresi);
        while (t < T)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / T);
            float e = 1f - Mathf.Pow(1f - u, 2.8f);
            _panelRt.anchoredPosition = Vector2.Lerp(bas, hedefAnchored, e);
            yield return null;
        }
        _panelRt.anchoredPosition = hedefAnchored;
        _animasyonCalisiyor = false;
        if (_okBtn != null)
            _okBtn.interactable = true;
    }
}
