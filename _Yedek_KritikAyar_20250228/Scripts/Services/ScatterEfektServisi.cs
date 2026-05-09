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
/// Scatter büyütme efekti (bonus tetiklenmeden önce scatter hücrelerini scale animasyonu).
/// IzgaraServisi/TumbleServisi/scatter index mantığına dokunmaz; context ile veri alır.
/// </summary>
public class ScatterEfektServisi
{
    private IScatterEfektBaglami _ctx;

    public void SetBaglam(IScatterEfektBaglami ctx)
    {
        _ctx = ctx;
    }

    /// <summary>Scatter hücrelerini bulur, scale animasyonu yapar. OyunYoneticisi'nden sadece coroutine çağrısı kalır.</summary>
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

        float t = 0f;
        while (t < scatterAnimDuration)
        {
            t += Time.deltaTime;
            float scale = Mathf.Lerp(1f, scatterScaleUp, t / scatterAnimDuration);
            for (int i = 0; i < scatterImages.Count; i++)
            {
                if (scatterImages[i] != null && scatterImages[i].rectTransform != null)
                    scatterImages[i].rectTransform.localScale = Vector3.one * scale;
            }
            yield return null;
        }

        yield return new WaitForSeconds(0.3f);
    }
}
