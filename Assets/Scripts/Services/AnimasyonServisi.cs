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
    private Func<float> _getPopDuration;
    private ParticleSystem _popParticlePrefab;
    private Transform _particleParent;
    private Func<int, int, int> _xyToIndex;
    private Action _onRefreshCarpanTexts;
    private Func<IReadOnlyDictionary<int, CarpanOverlayRef>> _getCarpanOverlays;
    private Action<IEnumerator> _runCoroutine;
    private Func<int, TextMeshProUGUI> _getCarpanHucreTexti;
    private Sprite _carpanSembolSprite;

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
    public void SetCarpanHucreTextiAl(Func<int, TextMeshProUGUI> getter) => _getCarpanHucreTexti = getter;
    public void SetCarpanSembolSprite(Sprite s) => _carpanSembolSprite = s;
    /// <summary>Pop süresini çalışma zamanında dinamik oku (bonus hız override ve inspector değişiklikleri anında yansır).</summary>
    public void SetGetPopDuration(Func<float> getter) => _getPopDuration = getter;

    /// <summary>
    /// Yeni spin başlamadan önce mevcut grid'i aşağı doğru akıtarak temizler.
    /// </summary>
    public IEnumerator AnimateGridOutDown(float outDuration = 0.20f, float outDistance = 180f)
    {
        if (_hucreler == null || _hucreler.Length == 0)
            yield break;

        outDuration = Mathf.Max(0.01f, outDuration);
        float t = 0f;
        int count = _hucreler.Length;
        Vector2[] baslangiclar = new Vector2[count];
        Vector2[] hedefler = new Vector2[count];
        float[] alphaBaslangic = new float[count];

        for (int i = 0; i < count; i++)
        {
            var img = _hucreler[i];
            if (img == null) continue;
            RectTransform rt = img.rectTransform;
            baslangiclar[i] = rt.anchoredPosition;
            hedefler[i] = baslangiclar[i] + Vector2.down * outDistance;
            alphaBaslangic[i] = img.color.a;
        }

        while (t < outDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / outDuration);
            float s = p * p * (3f - 2f * p);

            for (int i = 0; i < count; i++)
            {
                var img = _hucreler[i];
                if (img == null) continue;
                RectTransform rt = img.rectTransform;
                rt.anchoredPosition = Vector2.LerpUnclamped(baslangiclar[i], hedefler[i], s);
                Color c = img.color;
                c.a = Mathf.Lerp(alphaBaslangic[i], 0f, p);
                img.color = c;
            }
            _onRefreshCarpanTexts?.Invoke();
            yield return null;
        }

        for (int i = 0; i < count; i++)
        {
            var img = _hucreler[i];
            if (img == null) continue;
            img.rectTransform.anchoredPosition = hedefler[i];
            Color c = img.color;
            c.a = 0f;
            img.color = c;
        }
        _onRefreshCarpanTexts?.Invoke();
    }

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

        // Normal tumble'da PopDuration düşük (~0.15); bonusda ~0.7 — bonusu gereksiz uzatmamak için çarpan sadece kısa sürelere.
        const float tumPatlamaSureCarpani = 1.95f;
        const float tumPatlamaKisaSureEsigi = 0.4f;
        const float tumPatlamaZirveOlcek = 1.74f;
        // Referans paylar (toplam 1): büyümeyi 2× uzatmak için toplam süreyi uzatıp zirve/küçülme saniyesini koruruz.
        const float refBuyumePay = 0.32f;
        const float refZirvePay = 0.24f;
        const float refKuculmePay = 1f - refBuyumePay - refZirvePay;
        const float buyumeSureCarpani = 2f;
        float payToplam = refBuyumePay * buyumeSureCarpani + refZirvePay + refKuculmePay;

        float curPop = _getPopDuration != null ? _getPopDuration() : _popDuration;
        float popTemel = Mathf.Max(0.08f,
            curPop < tumPatlamaKisaSureEsigi ? curPop * tumPatlamaSureCarpani : curPop);
        float popSure = popTemel * payToplam;

        float buyumeBitisU = (refBuyumePay * buyumeSureCarpani) / payToplam;
        float zirveBitisU = (refBuyumePay * buyumeSureCarpani + refZirvePay) / payToplam;
        if (zirveBitisU <= buyumeBitisU + 0.02f)
            zirveBitisU = Mathf.Min(1f, buyumeBitisU + 0.02f);

        float t = 0f;
        Vector3[] startScales = new Vector3[cells.Count];
        for (int i = 0; i < cells.Count; i++)
        {
            int idx = _xyToIndex(cells[i].x, cells[i].y);
            if (idx >= 0 && idx < _hucreler.Length)
                startScales[i] = _hucreler[idx].rectTransform.localScale;
        }

        while (t < popSure)
        {
            float u = t / popSure;
            float alfaKaybBas = Mathf.Min(0.92f, zirveBitisU + 0.04f);
            float a = (u < alfaKaybBas) ? 1f : Mathf.Lerp(1f, 0f, (u - alfaKaybBas) / Mathf.Max(0.08f, 1f - alfaKaybBas));

            float s;
            if (u < buyumeBitisU)
                s = Mathf.Lerp(1f, tumPatlamaZirveOlcek, u / Mathf.Max(0.001f, buyumeBitisU));
            else if (u < zirveBitisU)
                s = tumPatlamaZirveOlcek;
            else
                s = Mathf.Lerp(tumPatlamaZirveOlcek, 0.35f, (u - zirveBitisU) / Mathf.Max(0.001f, 1f - zirveBitisU));

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

    /// <summary>
    /// Geriye dönük uyumluluk: bomba odaklı pop akışı.
    /// Yeni motorla aynı patlama hissini korumak için mevcut pop animasyonunu kullanır.
    /// </summary>
    public IEnumerator AnimatePopBombali(List<Vector2Int> cells, Vector2Int bombaHucre, int hamKazanc, int carpan, int toplamKazanc)
    {
        if (_hucreler == null || _xyToIndex == null)
            yield break;

        int bombaIdx = _xyToIndex(bombaHucre.x, bombaHucre.y);
        if (bombaIdx < 0 || bombaIdx >= _hucreler.Length)
        {
            int fallbackIdx = (cells != null && cells.Count > 0) ? _xyToIndex(cells[0].x, cells[0].y) : -1;
            bombaIdx = (fallbackIdx >= 0 && fallbackIdx < _hucreler.Length) ? fallbackIdx : -1;
        }

        // Formül metni yalnızca kazanç > 0 ise yaz; toggle KAPALI + force carpan'da "0 TL × N" görünmesin.
        if (hamKazanc > 0 && bombaIdx >= 0 && _getCarpanHucreTexti != null)
        {
            var tmp = _getCarpanHucreTexti(bombaIdx);
            if (tmp != null)
            {
                tmp.enableWordWrapping = false;
                tmp.text = $"{OyunFormatServisi.FormatTL(Mathf.Max(0, hamKazanc))} x{Mathf.Max(1, carpan)}";
            }
        }

        if (cells == null || cells.Count == 0 || bombaIdx < 0)
            yield break;

        var bombaImg = _hucreler[bombaIdx];
        if (bombaImg == null) yield break;
        var bombaRt = bombaImg.rectTransform;
        Vector2 bombaPos = bombaRt.anchoredPosition;
        Vector3 bombaStartScale = bombaRt.localScale;
        Color bombaStartColor = bombaImg.color;

        var idxList = new List<int>(cells.Count);
        var startPos = new Dictionary<int, Vector2>();
        var startScale = new Dictionary<int, Vector3>();
        var startAlpha = new Dictionary<int, float>();
        for (int i = 0; i < cells.Count; i++)
        {
            int idx = _xyToIndex(cells[i].x, cells[i].y);
            if (idx < 0 || idx >= _hucreler.Length) continue;
            var img = _hucreler[idx];
            if (img == null) continue;
            idxList.Add(idx);
            startPos[idx] = img.rectTransform.anchoredPosition;
            startScale[idx] = img.rectTransform.localScale;
            startAlpha[idx] = img.color.a;
        }

        // Eski hissi koru: hedef küçük grid bombası değil, ekranda merkezde "büyük bomba".
        Vector2 merkezHedef = Vector2.zero;
        if (idxList.Count > 0)
        {
            Vector2 toplam = Vector2.zero;
            for (int i = 0; i < idxList.Count; i++)
                toplam += startPos[idxList[i]];
            merkezHedef = toplam / idxList.Count;
        }

        // Merkezde eski hissi veren büyük bomba için geçici bir görsel oluştur.
        // Böylece griddeki küçük çarpan sembolünden bağımsız, belirgin bir "final bomba" görünür.
        RectTransform finalBombaRt = bombaRt;
        Image finalBombaImg = bombaImg;
        GameObject geciciFinalBomba = null;
        if (bombaRt.parent != null)
        {
            geciciFinalBomba = new GameObject("FinalBuyukBomba", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var geciciRt = geciciFinalBomba.GetComponent<RectTransform>();
            var geciciImg = geciciFinalBomba.GetComponent<Image>();
            geciciRt.SetParent(bombaRt.parent, false);
            geciciRt.anchorMin = bombaRt.anchorMin;
            geciciRt.anchorMax = bombaRt.anchorMax;
            geciciRt.pivot = bombaRt.pivot;
            geciciRt.anchoredPosition = bombaPos;
            geciciRt.localScale = bombaStartScale;
            geciciRt.sizeDelta = bombaRt.sizeDelta;
            geciciImg.sprite = _carpanSembolSprite != null ? _carpanSembolSprite : bombaImg.sprite;
            geciciImg.material = bombaImg.material;
            geciciImg.type = bombaImg.type;
            geciciImg.preserveAspect = bombaImg.preserveAspect;
            geciciImg.color = bombaStartColor;
            geciciRt.SetAsLastSibling();

            // Küçük grid bombasını gizleyip animasyonu geçici büyük görselde oynat.
            Color gizli = bombaImg.color;
            gizli.a = 0f;
            bombaImg.color = gizli;

            finalBombaRt = geciciRt;
            finalBombaImg = geciciImg;
        }

        float toplanmaSure = Mathf.Max(0.20f, _popDuration * 0.82f);
        float t = 0f;
        while (t < toplanmaSure)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / toplanmaSure);
            float s = p * p * (3f - 2f * p);

            for (int i = 0; i < idxList.Count; i++)
            {
                int idx = idxList[i];
                var img = _hucreler[idx];
                if (img == null) continue;

                if (idx == bombaIdx)
                {
                    finalBombaRt.anchoredPosition = Vector2.LerpUnclamped(bombaPos, merkezHedef, s);
                    float pulse = 1f + Mathf.Sin(p * Mathf.PI) * 0.22f;
                    finalBombaRt.localScale = bombaStartScale * pulse;
                }
            }
            _onRefreshCarpanTexts?.Invoke();
            yield return null;
        }

        float patlamaSure = Mathf.Max(0.16f, _popDuration * 0.35f);
        t = 0f;
        while (t < patlamaSure)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / patlamaSure);
            float e = p * p * (3f - 2f * p);

            finalBombaRt.anchoredPosition = merkezHedef;
            finalBombaRt.localScale = Vector3.LerpUnclamped(bombaStartScale * 1.25f, bombaStartScale * 2.50f, e);
            Color bc = finalBombaImg.color;
            bc.a = Mathf.Lerp(1f, 0f, e);
            finalBombaImg.color = bc;
            _onRefreshCarpanTexts?.Invoke();
            yield return null;
        }

        if (geciciFinalBomba != null)
        {
            UnityEngine.Object.Destroy(geciciFinalBomba);
            bombaImg.color = bombaStartColor;
        }
        bombaRt.localScale = bombaStartScale;
        bombaRt.anchoredPosition = bombaPos;
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
