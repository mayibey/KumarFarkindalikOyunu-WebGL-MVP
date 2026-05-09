using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Çarpan overlay oluşturma, silme ve liste yönetimi. Grid senkronu OyunYoneticisi'nde kalır.
/// </summary>
public class CarpanOverlayServisi
{
    /// <summary> AnimasyonServisi.GetCarpanOverlays ile uyumlu referans (sadece rt, tmp). </summary>
    public class CarpanOverlayRef
    {
        public RectTransform rt;
        public TextMeshProUGUI tmp;
    }

    private class CarpanOverlayEntry
    {
        public GameObject root;
        public RectTransform rt;
        public Image img;
        public TextMeshProUGUI tmp;
    }

    private readonly Dictionary<int, CarpanOverlayEntry> _overlays = new Dictionary<int, CarpanOverlayEntry>();

    private Image[] _cellImages;
    private Sprite _carpanSembolSprite;
    private Vector2 _overlaySize;
    private int _overlayFontSize;
    private Vector2 _overlayTextOffset;
    private float _dropStartYOffset;
    private float _dropDuration;
    private Action<string, IEnumerator> _startNamedCoroutine;
    private Action<string> _stopNamedCoroutine;
    private Action<string> _onCoroutineFinished;
    private Action _onOverlayChanged;

    public virtual void SetCellImages(Image[] images) => _cellImages = images;
    public virtual void SetCarpanSembolSprite(Sprite sprite) => _carpanSembolSprite = sprite;
    public virtual void SetOverlaySize(Vector2 size) => _overlaySize = size;
    public virtual void SetOverlayFontSize(int fontSize) => _overlayFontSize = Mathf.Max(1, fontSize);
    public virtual void SetOverlayTextOffset(Vector2 offset) => _overlayTextOffset = offset;
    public virtual void SetDropStartYOffset(float y) => _dropStartYOffset = y;
    public virtual void SetDropDuration(float duration) => _dropDuration = Mathf.Max(0.01f, duration);
    public virtual void SetStartNamedCoroutine(Action<string, IEnumerator> start) => _startNamedCoroutine = start;
    public virtual void SetStopNamedCoroutine(Action<string> stop) => _stopNamedCoroutine = stop;
    public virtual void SetOnCoroutineFinished(Action<string> onFinished) => _onCoroutineFinished = onFinished;
    public virtual void SetOnOverlayChanged(Action onChanged) => _onOverlayChanged = onChanged;

    public virtual void SpawnCarpanOverlayAt(int cellIndex, int carpanDegeri)
    {
        if (cellIndex < 0 || _cellImages == null || cellIndex >= _cellImages.Length) return;
        Image cellImg = _cellImages[cellIndex];
        if (cellImg == null) return;
        if (_carpanSembolSprite == null) return;

        if (!_overlays.TryGetValue(cellIndex, out CarpanOverlayEntry ov) || ov == null || ov.root == null)
        {
            ov = new CarpanOverlayEntry();

            GameObject root = new GameObject("CarpanOverlay", typeof(RectTransform));
            root.transform.SetParent(cellImg.transform, false);

            RectTransform rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = _overlaySize;
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = new Vector3(1.5f, 1.5f, 1f); // bomba 1.5x — yan meyvelerden baskın görünüm
            Debug.Log($"[Carpan] CarpanOverlay yaratıldı, scale=1.5x, cell={cellIndex}");

            Image img = root.AddComponent<Image>();
            img.sprite = _carpanSembolSprite;
            img.preserveAspect = true;
            img.raycastTarget = false;

            GameObject textObj = new GameObject("Text", typeof(RectTransform));
            textObj.transform.SetParent(root.transform, false);

            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.5f, 0.5f);
            trt.anchorMax = new Vector2(0.5f, 0.5f);
            trt.pivot = new Vector2(0.5f, 0.5f);
            trt.sizeDelta = _overlaySize;
            trt.anchoredPosition = _overlayTextOffset;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = _overlayFontSize;
            tmp.raycastTarget = false;

            ov.root = root;
            ov.rt = rt;
            ov.img = img;
            ov.tmp = tmp;

            _overlays[cellIndex] = ov;
        }

        ov.img.sprite = _carpanSembolSprite;
        IzgaraServisi.SetCarpanText(ov.tmp, carpanDegeri);
        ov.root.SetActive(true);

        _stopNamedCoroutine?.Invoke("CarpanDrop_" + cellIndex);
        _startNamedCoroutine?.Invoke("CarpanDrop_" + cellIndex, CarpanDropAnim(ov.rt, cellIndex));

        _onOverlayChanged?.Invoke();
    }

    public virtual void RemoveCarpanOverlayAt(int cellIndex)
    {
        if (!_overlays.TryGetValue(cellIndex, out CarpanOverlayEntry ov)) return;
        if (ov?.root != null)
            UnityEngine.Object.Destroy(ov.root);
        _overlays.Remove(cellIndex);
        _stopNamedCoroutine?.Invoke("CarpanDrop_" + cellIndex);
        _onOverlayChanged?.Invoke();
    }

    /// <summary> Sadece overlay'leri temizler (Destroy + clear). Grid senkronu çağıran tarafında yapılır. </summary>
    public virtual void ClearAll()
    {
        foreach (var kv in _overlays)
        {
            if (kv.Value?.root != null)
                UnityEngine.Object.Destroy(kv.Value.root);
            _stopNamedCoroutine?.Invoke("CarpanDrop_" + kv.Key);
        }
        _overlays.Clear();
        _onOverlayChanged?.Invoke();
    }

    public virtual IReadOnlyDictionary<int, CarpanOverlayRef> AnimasyonIcinOverlayleriAl()
    {
        var result = new Dictionary<int, CarpanOverlayRef>();
        foreach (var kv in _overlays)
        {
            if (kv.Value != null && (kv.Value.rt != null || kv.Value.tmp != null))
                result[kv.Key] = new CarpanOverlayRef { rt = kv.Value.rt, tmp = kv.Value.tmp };
        }
        return result;
    }

    private IEnumerator CarpanDropAnim(RectTransform rt, int cellIndex)
    {
        Vector2 endPos = Vector2.zero;
        Vector2 startPos = new Vector2(0f, _dropStartYOffset);

        rt.anchoredPosition = startPos;

        float dur = _dropDuration;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            rt.anchoredPosition = Vector2.Lerp(startPos, endPos, k);
            yield return null;
        }

        rt.anchoredPosition = endPos;
        _onCoroutineFinished?.Invoke("CarpanDrop_" + cellIndex);
    }

    /// <summary>Refill ile gelen çarpanları sırayla kazanç yazısına doğru uçurur; her varıştan sonra <paramref name="onHerAdimSonrasi"/> çağrılır.</summary>
    public virtual IEnumerator KazancaUcuslariSiraliEnum(IReadOnlyList<int> hucreIndeksleri, IReadOnlyList<int> carpanDegerleri, RectTransform kazancHedef, Action<int> onHerAdimSonrasi)
    {
        if (hucreIndeksleri == null || carpanDegerleri == null || kazancHedef == null) yield break;
        int n = Mathf.Min(hucreIndeksleri.Count, carpanDegerleri.Count);
        for (int i = 0; i < n; i++)
        {
            yield return KazancaTekUcusEnum(hucreIndeksleri[i], carpanDegerleri[i], kazancHedef);
            onHerAdimSonrasi?.Invoke(carpanDegerleri[i]);
        }
    }

    private IEnumerator KazancaTekUcusEnum(int cellIndex, int carpanDegeri, RectTransform kazancHedef)
    {
        if (_cellImages == null || cellIndex < 0 || cellIndex >= _cellImages.Length || kazancHedef == null || _carpanSembolSprite == null)
            yield break;

        var cellImg = _cellImages[cellIndex];
        if (cellImg == null) yield break;
        var cellRt = cellImg.rectTransform;

        // Aynı hücredeki mevcut overlay'i kaldır ki jeton kazanç kutusuna uçarken gridde ikinci bir kopya gezinmesin.
        RemoveCarpanOverlayAt(cellIndex);

        Canvas canvas = kazancHedef.GetComponentInParent<Canvas>();
        if (canvas == null) yield break;

        var go = new GameObject("CarpanKazancUcusu", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var flyRt = go.GetComponent<RectTransform>();
        var img = go.GetComponent<Image>();
        img.sprite = _carpanSembolSprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        flyRt.SetParent(canvas.transform, false);
        flyRt.anchorMin = flyRt.anchorMax = flyRt.pivot = new Vector2(0.5f, 0.5f);
        flyRt.sizeDelta = _overlaySize;
        flyRt.position = cellRt.position;
        flyRt.localScale = Vector3.one * 0.95f;

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(go.transform, false);
        RectTransform trt = textObj.GetComponent<RectTransform>();
        trt.anchorMin = trt.anchorMax = trt.pivot = new Vector2(0.5f, 0.5f);
        trt.sizeDelta = _overlaySize;
        trt.anchoredPosition = _overlayTextOffset;
        var tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = _overlayFontSize;
        IzgaraServisi.SetCarpanText(tmp, carpanDegeri);
        tmp.raycastTarget = false;
        var stilKaynak = kazancHedef.GetComponent<TextMeshProUGUI>();
        if (stilKaynak != null)
        {
            tmp.font = stilKaynak.font;
            tmp.fontSharedMaterials = stilKaynak.fontSharedMaterials;
        }

        flyRt.SetAsLastSibling();

        Vector3 start = flyRt.position;
        Vector3 end = kazancHedef.position;
        const float sure = 0.48f;
        float t = 0f;
        while (t < sure)
        {
            if (kazancHedef == null || flyRt == null) yield break;
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / sure);
            float e = u * u * (3f - 2f * u);
            flyRt.position = Vector3.LerpUnclamped(start, end, e);
            float olcek = Mathf.Lerp(0.95f, 0.55f, e);
            flyRt.localScale = Vector3.one * olcek;
            yield return null;
        }

        // Hedefe değer değmez görünmez yap; metin/layout güncellemesinde başka yere kayıyormuş hissi oluşmasın.
        flyRt.position = end;
        go.SetActive(false);
        UnityEngine.Object.Destroy(go);
    }
}
