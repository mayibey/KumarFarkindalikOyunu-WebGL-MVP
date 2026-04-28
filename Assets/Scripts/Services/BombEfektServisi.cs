using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 100x+ çarpan düşüşünde beyaz ekran flash (0.3s) + kamera sarsıntısı (0.5s) oynatır.
/// </summary>
public class BombEfektServisi
{
    private Coroutine _flashCoroutine;
    private Coroutine _shakeCoroutine;

    public void BombEfektBaslat(MonoBehaviour calistirici, int carpanDegeri)
    {
        if (calistirici == null || carpanDegeri < 100) return;

        if (_shakeCoroutine != null) calistirici.StopCoroutine(_shakeCoroutine);

        _shakeCoroutine = calistirici.StartCoroutine(KameraShakeEnum());
    }

    private IEnumerator BeyzFlashEnum(MonoBehaviour calistirici)
    {
        var go = new GameObject("BombFlashOverlay");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();

        var imgGo = new GameObject("FlashImg");
        imgGo.transform.SetParent(go.transform, false);
        var img = imgGo.AddComponent<Image>();
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        float sure = 0.3f;
        float gecen = 0f;
        while (gecen < sure)
        {
            if (img == null) break;
            gecen += Time.deltaTime;
            float t = Mathf.Clamp01(gecen / sure);
            img.color = new Color(1f, 1f, 1f, 1f - t);
            yield return null;
        }

        if (go != null) Object.Destroy(go);
        _flashCoroutine = null;
    }

    private IEnumerator KameraShakeEnum()
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

        float sure = 0.5f;
        float genlik = 0.12f;
        float gecen = 0f;
        Vector3 baslangic = cam.transform.localPosition;

        while (gecen < sure)
        {
            gecen += Time.deltaTime;
            float decay = 1f - Mathf.Clamp01(gecen / sure);
            Vector2 offset = Random.insideUnitCircle * genlik * decay;
            cam.transform.localPosition = baslangic + new Vector3(offset.x, offset.y, 0f);
            yield return null;
        }

        if (cam != null) cam.transform.localPosition = baslangic;
        _shakeCoroutine = null;
    }
}
