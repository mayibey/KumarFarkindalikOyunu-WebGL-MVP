using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Build'de (Editor dışı) Canvas'ı tam ekran yapar ve layout'u birkaç kare yeniler.
/// WebGL'de UI nesnelerinin sahne konumlarını değiştirmez; elle yerleşim korunur.
/// </summary>
public class BuildUILayoutFix : MonoBehaviour
{
    [Tooltip("Build'de Canvas'ı tam ekrana zorla (Editor'da dokunmaz).")]
    public bool builddeTamEkranCanvas = true;

    [Tooltip("Layout rebuild kaç kare boyunca tekrar edilsin (build'de 2–3 önerilir).")]
    public int rebuildKareSayisi = 3;

    [Tooltip("Web/Build için hedef referans çözünürlük (CanvasScaler).")]
    public Vector2 hedefReferansCozunurluk = new Vector2(1920f, 1080f);

    [Tooltip("CanvasScaler Width/Height blend (0=width, 1=height).")]
    [Range(0f, 1f)] public float eslemeKarisimi = 0.5f;

    void Start()
    {
#if !UNITY_EDITOR
        if (builddeTamEkranCanvas)
            CanvasTamEkranYap();
#endif
        StartCoroutine(LayoutRebuildDongusu());
    }

    private void CanvasTamEkranYap()
    {
        var canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        var rect = canvas.GetComponent<RectTransform>();
        if (rect == null) return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.localScale = Vector3.one;

        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = hedefReferansCozunurluk;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = eslemeKarisimi;
        }
    }

    private System.Collections.IEnumerator LayoutRebuildDongusu()
    {
        for (int i = 0; i < rebuildKareSayisi; i++)
        {
            yield return null;
            ForceLayoutRebuild();
        }
    }

    private void ForceLayoutRebuild()
    {
        Canvas.ForceUpdateCanvases();

        var canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            var rect = canvas.GetComponent<RectTransform>();
            if (rect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }

        foreach (var rt in GetComponentsInChildren<RectTransform>(true))
        {
            var lg = rt.GetComponent<LayoutGroup>();
            if (lg != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }
    }

    void OnRectTransformDimensionsChange()
    {
#if !UNITY_EDITOR
        ForceLayoutRebuild();
#endif
    }
}
