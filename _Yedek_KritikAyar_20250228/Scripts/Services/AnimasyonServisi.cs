using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Izgara drop, pop ve çarpan şişme animasyonları. Unity bağımlılıkları setter ile alınır.
/// </summary>
public class AnimasyonServisi
{
    public class CarpanOverlayRef
    {
        public RectTransform rt;
        public TextMeshProUGUI tmp;
    }

    private Image[] _hucreler;
    private Vector2[] _cellPos;
    private float _dropDuration;
    private float _dropStagger;
    private float _dropStartYOffset;
    private float _popDuration;
    private ParticleSystem _popParticlePrefab;
    private Transform _particleParent;
    private Func<int, int, int> _xyToIndex;
    private Action _onRefreshCarpanTexts;
    private Func<IReadOnlyDictionary<int, CarpanOverlayRef>> _getCarpanOverlays;
    private Action<IEnumerator> _runCoroutine;

    public void SetHucreler(Image[] hucreler) => _hucreler = hucreler;
    public void SetCellPos(Vector2[] cellPos) => _cellPos = cellPos;
    public void SetDurations(float dropDuration, float dropStagger, float dropStartYOffset, float popDuration)
    {
        _dropDuration = dropDuration;
        _dropStagger = dropStagger;
        _dropStartYOffset = dropStartYOffset;
        _popDuration = popDuration;
    }
    public void SetPopParticlePrefab(ParticleSystem prefab) => _popParticlePrefab = prefab;
    public void SetParticleParent(Transform parent) => _particleParent = parent;
    public void SetXYToIndex(Func<int, int, int> fn) => _xyToIndex = fn;
    public void SetOnRefreshCarpanTexts(Action onRefresh) => _onRefreshCarpanTexts = onRefresh;
    public void SetGetCarpanOverlays(Func<IReadOnlyDictionary<int, CarpanOverlayRef>> getter) => _getCarpanOverlays = getter;
    public void SetRunCoroutine(Action<IEnumerator> run) => _runCoroutine = run;

    public IEnumerator AnimateGridDropIn()
    {
        if (_hucreler == null) yield break;

        int count = _hucreler.Length;
        Vector2[] targets = new Vector2[count];
        Vector2[] starts = new Vector2[count];

        for (int i = 0; i < count; i++)
        {
            var img = _hucreler[i];
            if (img == null) continue;

            RectTransform rt = img.rectTransform;
            Vector2 target = (_cellPos != null && i < _cellPos.Length) ? _cellPos[i] : rt.anchoredPosition;
            Vector2 start = target + Vector2.up * _dropStartYOffset;

            targets[i] = target;
            starts[i] = start;
            rt.anchoredPosition = start;

            Color c = img.color;
            c.a = 0f;
            img.color = c;
        }

        int done = 0;
        for (int i = 0; i < count; i++)
        {
            var img = _hucreler[i];
            if (img == null) { done++; continue; }

            RectTransform rt = img.rectTransform;
            int idx = i;
            _runCoroutine?.Invoke(AnimateOneCellWithFade(img, rt, starts[idx], targets[idx], () => done++));
            if (_dropStagger > 0f)
                yield return new WaitForSeconds(_dropStagger);
        }

        while (done < count)
            yield return null;

        _onRefreshCarpanTexts?.Invoke();
    }

    public IEnumerator AnimateOneCell(RectTransform rt, Vector2 start, Vector2 target, Action onDone)
    {
        Image img = null;
        if (_hucreler != null)
        {
            for (int i = 0; i < _hucreler.Length; i++)
            {
                if (_hucreler[i] != null && _hucreler[i].rectTransform == rt)
                {
                    img = _hucreler[i];
                    break;
                }
            }
        }

        if (img == null)
        {
            float t = 0f;
            while (t < _dropDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / _dropDuration);
                p = p * p * (3f - 2f * p);
                rt.anchoredPosition = Vector2.LerpUnclamped(start, target, p);
                _onRefreshCarpanTexts?.Invoke();
                yield return null;
            }
            rt.anchoredPosition = target;
            _onRefreshCarpanTexts?.Invoke();
            onDone?.Invoke();
            yield break;
        }

        yield return AnimateOneCellWithFade(img, rt, start, target, onDone);
    }

    public IEnumerator AnimateOneCellWithFade(Image img, RectTransform rt, Vector2 start, Vector2 target, Action onDone)
    {
        float t = 0f;

        while (t < _dropDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / _dropDuration);
            float sp = p * p * (3f - 2f * p);

            rt.anchoredPosition = Vector2.LerpUnclamped(start, target, sp);

            float a = Mathf.Clamp01(p / 0.25f);
            Color c = img.color;
            c.a = a;
            img.color = c;

            _onRefreshCarpanTexts?.Invoke();
            yield return null;
        }

        rt.anchoredPosition = target;
        Color cc = img.color;
        cc.a = 1f;
        img.color = cc;

        _onRefreshCarpanTexts?.Invoke();
        onDone?.Invoke();
    }

    public IEnumerator AnimatePop(List<Vector2Int> cells)
    {
        if (cells == null || _hucreler == null || _xyToIndex == null) yield break;

        if (_popParticlePrefab != null && _particleParent != null)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                int idx = _xyToIndex(cells[i].x, cells[i].y);
                if (idx < 0 || idx >= _hucreler.Length) continue;
                var rt = _hucreler[idx].rectTransform;
                var ps = UnityEngine.Object.Instantiate(_popParticlePrefab, rt.position, Quaternion.identity, _particleParent);
                ps.Play();
                UnityEngine.Object.Destroy(ps.gameObject, 1f);
            }
        }

        float t = 0f;
        Vector3[] startScales = new Vector3[cells.Count];
        for (int i = 0; i < cells.Count; i++)
        {
            int idx = _xyToIndex(cells[i].x, cells[i].y);
            if (idx >= 0 && idx < _hucreler.Length)
                startScales[i] = _hucreler[idx].rectTransform.localScale;
        }

        while (t < _popDuration)
        {
            float u = t / _popDuration;
            float a = (u < 0.55f) ? 1f : Mathf.Lerp(1f, 0f, (u - 0.55f) / 0.45f);
            float s = (u < 0.45f)
                ? Mathf.Lerp(1f, 1.55f, u / 0.45f)
                : Mathf.Lerp(1.55f, 0.35f, (u - 0.45f) / 0.55f);

            for (int i = 0; i < cells.Count; i++)
            {
                int idx = _xyToIndex(cells[i].x, cells[i].y);
                if (idx < 0 || idx >= _hucreler.Length) continue;
                var img = _hucreler[idx];

                Color baseCol = Color.white;
                Color flashCol = new Color(1f, 1f, 0.6f, 1f);
                Color mixed = Color.Lerp(baseCol, flashCol, Mathf.Sin(u * Mathf.PI));
                mixed.a = a;
                img.color = mixed;

                img.rectTransform.localScale = startScales[i] * s;
            }

            t += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < cells.Count; i++)
        {
            int idx = _xyToIndex(cells[i].x, cells[i].y);
            if (idx >= 0 && idx < _hucreler.Length)
            {
                var img = _hucreler[idx];
                Color c = img.color;
                c.a = 0f;
                img.color = c;
            }
        }
    }

    public IEnumerator AnimateCarpanSisme(float scale = 1.5f, float duration = 0.18f)
    {
        var overlays = _getCarpanOverlays?.Invoke();
        if (overlays == null || overlays.Count == 0) yield break;

        float t = 0f;
        Dictionary<int, Vector3> startRt = new Dictionary<int, Vector3>();
        Dictionary<int, Vector3> startTmp = new Dictionary<int, Vector3>();
        foreach (var kv in overlays)
        {
            if (kv.Value != null && kv.Value.rt != null)
                startRt[kv.Key] = kv.Value.rt.localScale;
            if (kv.Value != null && kv.Value.tmp != null)
                startTmp[kv.Key] = kv.Value.tmp.rectTransform.localScale;
        }

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float s = Mathf.Lerp(1f, scale, k);
            foreach (var kv in overlays)
            {
                if (kv.Value != null)
                {
                    if (kv.Value.rt != null)
                    {
                        Vector3 baseS = startRt.TryGetValue(kv.Key, out var bs) ? bs : Vector3.one;
                        kv.Value.rt.localScale = baseS * s;
                    }
                    if (kv.Value.tmp != null)
                    {
                        Vector3 baseT = startTmp.TryGetValue(kv.Key, out var bt) ? bt : Vector3.one;
                        kv.Value.tmp.rectTransform.localScale = baseT * s;
                    }
                }
            }
            yield return null;
        }

        t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float s = Mathf.Lerp(scale, 1f, k);
            foreach (var kv in overlays)
            {
                if (kv.Value != null)
                {
                    if (kv.Value.rt != null)
                    {
                        Vector3 baseS = startRt.TryGetValue(kv.Key, out var bs) ? bs : Vector3.one;
                        kv.Value.rt.localScale = baseS * s;
                    }
                    if (kv.Value.tmp != null)
                    {
                        Vector3 baseT = startTmp.TryGetValue(kv.Key, out var bt) ? bt : Vector3.one;
                        kv.Value.tmp.rectTransform.localScale = baseT * s;
                    }
                }
            }
            yield return null;
        }
    }
}
