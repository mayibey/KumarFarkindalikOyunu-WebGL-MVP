using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ScatterEfektServisi için context arayüzü. OyunYoneticisi implement eder.
/// </summary>
public interface IScatterEfektBaglami
{
    int[,] Grid { get; }
    int ScatterIndex { get; }
    int Sutun { get; }
    int Satir { get; }
    int XYToIndex(int x, int y);
    Image[] Hucreler { get; }
    float ScatterScaleUp { get; }
    float ScatterAnimDuration { get; }
}

/// <summary>
/// Scatter bonus tetik efekti:
/// Yıldızlar merkeze toplanır, tek yıldız hissi verip patlayarak kaybolur.
/// </summary>
public class ScatterEfektServisi
{
    private IScatterEfektBaglami _ctx;

    public void SetBaglam(IScatterEfektBaglami ctx)
    {
        _ctx = ctx;
    }

    /// <summary>
    /// Scatter hücrelerini bulur; ortada birleşme + tek yıldız patlama animasyonu oynatır.
    /// </summary>
    public IEnumerator ScatterBuyutEfektiCalistir()
    {
        if (_ctx == null) yield break;

        List<Image> scatterImages = new List<Image>();
        int[,] grid = _ctx.Grid;
        int scatterIdx = _ctx.ScatterIndex;
        int sutun = _ctx.Sutun;
        int satir = _ctx.Satir;
        Image[] hucreler = _ctx.Hucreler;

        if (grid == null || hucreler == null) yield break;

        for (int x = 0; x < sutun; x++)
        {
            for (int y = 0; y < satir; y++)
            {
                if (grid[x, y] == scatterIdx)
                {
                    int idx = _ctx.XYToIndex(x, y);
                    if (idx >= 0 && idx < hucreler.Length)
                        scatterImages.Add(hucreler[idx]);
                }
            }
        }

        if (scatterImages.Count == 0)
            yield break;

        float scatterScaleUp = _ctx.ScatterScaleUp;
        float scatterAnimDuration = _ctx.ScatterAnimDuration;
        if (scatterAnimDuration <= 0f) scatterAnimDuration = 0.01f;
        float toplanmaSure = Mathf.Max(0.18f, scatterAnimDuration * 0.95f);
        float tekYildizSure = Mathf.Max(0.16f, scatterAnimDuration * 0.55f);
        float patlamaSure = Mathf.Max(0.16f, scatterAnimDuration * 0.65f);

        int adet = scatterImages.Count;
        var baslangicPos = new Vector2[adet];
        var baslangicScale = new Vector3[adet];
        var baslangicRenk = new Color[adet];
        Vector2 merkez = Vector2.zero;

        for (int i = 0; i < adet; i++)
        {
            var img = scatterImages[i];
            if (img == null || img.rectTransform == null) continue;
            baslangicPos[i] = img.rectTransform.anchoredPosition;
            baslangicScale[i] = img.rectTransform.localScale;
            baslangicRenk[i] = img.color;
            merkez += baslangicPos[i];
        }
        merkez /= Mathf.Max(1, adet);

        // 1) Scatter yıldızları merkeze toplanır.
        float t = 0f;
        while (t < toplanmaSure)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / toplanmaSure);
            float eased = p * p * (3f - 2f * p);
            for (int i = 0; i < adet; i++)
            {
                var img = scatterImages[i];
                if (img == null || img.rectTransform == null) continue;
                img.rectTransform.anchoredPosition = Vector2.LerpUnclamped(baslangicPos[i], merkez, eased);
                img.rectTransform.localScale = baslangicScale[i] * Mathf.Lerp(1f, scatterScaleUp, eased);
            }
            yield return null;
        }

        // 2) Tek yıldız hissi: diğerleri görünmez, merkezde bir yıldız nabız yapar.
        int merkezIdx = 0;
        for (int i = 0; i < adet; i++)
        {
            if (i == merkezIdx) continue;
            var img = scatterImages[i];
            if (img == null) continue;
            var c = img.color;
            c.a = 0f;
            img.color = c;
        }

        t = 0f;
        while (t < tekYildizSure)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / tekYildizSure);
            float nabiz = 1f + Mathf.Sin(p * Mathf.PI) * 0.22f;
            var merkezImg = scatterImages[merkezIdx];
            if (merkezImg != null && merkezImg.rectTransform != null)
            {
                merkezImg.rectTransform.anchoredPosition = merkez;
                merkezImg.rectTransform.localScale = baslangicScale[merkezIdx] * scatterScaleUp * nabiz;
            }
            yield return null;
        }

        // 3) Tek yıldız patlar ve kaybolur.
        t = 0f;
        while (t < patlamaSure)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / patlamaSure);
            var merkezImg = scatterImages[merkezIdx];
            if (merkezImg != null && merkezImg.rectTransform != null)
            {
                merkezImg.rectTransform.localScale = baslangicScale[merkezIdx] * Mathf.Lerp(scatterScaleUp * 1.2f, 0.2f, p);
                var c = merkezImg.color;
                c.a = Mathf.Lerp(1f, 0f, p);
                merkezImg.color = c;
            }
            yield return null;
        }

        // Sonraki renderlarda konum/ölçek bozulmasın; bonus paneli açılana kadar scatter'lar görünmez kalsın.
        for (int i = 0; i < adet; i++)
        {
            var img = scatterImages[i];
            if (img == null || img.rectTransform == null) continue;
            img.rectTransform.anchoredPosition = baslangicPos[i];
            img.rectTransform.localScale = baslangicScale[i];
            var c = baslangicRenk[i];
            c.a = 0f;
            img.color = c;
        }
    }
}
