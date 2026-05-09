using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Zorla çarpan bombası sahneye indiğinde dramatik şok efekti oynatır:
/// thunder + bass sesi, beyaz flash, kamera sarsıntısı, kısa slow-motion.
/// Yalnızca carpanDeger >= esikDeger (varsayılan 50) için tetiklenir.
/// Tüm coroutine'ler Time.unscaledDeltaTime kullanır; slow-motion içinde de doğru çalışır.
/// </summary>
public class BombaInisEfektServisi
{
    private Coroutine _aktifEfektCoroutine;

    public void EfektBaslat(
        MonoBehaviour calistirici,
        int carpanDeger,
        AudioClip thunderClip,
        AudioClip bassClip,
        float thunderSesSeviyesi,
        float bassSesSeviyesi,
        RectTransform sarsintiHedefi,
        int esikDeger = 50)
    {
        if (calistirici == null || carpanDeger < esikDeger) return;

        if (_aktifEfektCoroutine != null)
            calistirici.StopCoroutine(_aktifEfektCoroutine);

        _aktifEfektCoroutine = calistirici.StartCoroutine(EfektEnum(
            calistirici, thunderClip, bassClip,
            thunderSesSeviyesi, bassSesSeviyesi, sarsintiHedefi));
    }

    private IEnumerator EfektEnum(
        MonoBehaviour calistirici,
        AudioClip thunderClip,
        AudioClip bassClip,
        float thunderSesSeviyesi,
        float bassSesSeviyesi,
        RectTransform sarsintiHedefi)
    {
        // t=0: ses, flash, sarsıntı, slow-motion başlat
        SesCal(thunderClip, Mathf.Clamp01(thunderSesSeviyesi));
        SesCal(bassClip, Mathf.Clamp01(bassSesSeviyesi));
        calistirici.StartCoroutine(BeyazFlashEnum());
        calistirici.StartCoroutine(SarsintiEnum(sarsintiHedefi));

        Time.timeScale = 0.3f;
        Time.fixedDeltaTime = 0.02f * 0.3f;

        // t=0.2s: slow-motion geri al
        yield return new WaitForSecondsRealtime(0.2f);

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        _aktifEfektCoroutine = null;
    }

    // Geçici bir AudioSource GameObject'i oluşturur; klibin süresi bitince yok eder.
    private static void SesCal(AudioClip clip, float sesSeviyesi)
    {
        if (clip == null) return;
        var go = new GameObject("BombaInisAudio");
        go.hideFlags = HideFlags.HideAndDontSave;
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.volume = sesSeviyesi;
        src.clip = clip;
        src.Play();
        Object.Destroy(go, clip.length + 0.5f);
    }

    // Beyaz overlay: alpha 0 → 0.7 → 0, toplam 0.15s realtime
    private static IEnumerator BeyazFlashEnum()
    {
        var canvasGo = new GameObject("BombaInisFlash");
        canvasGo.hideFlags = HideFlags.HideAndDontSave;
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        canvasGo.AddComponent<CanvasScaler>();

        var imgGo = new GameObject("FlashImg");
        imgGo.transform.SetParent(canvasGo.transform, false);
        var img = imgGo.AddComponent<Image>();
        var rt = imgGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        float sure = 0.15f;
        float gecen = 0f;
        while (gecen < sure)
        {
            gecen += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(gecen / sure);
            float alpha = t < 0.5f ? t * 2f * 0.7f : (1f - t) * 2f * 0.7f;
            img.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        Object.Destroy(canvasGo);
    }

    // Sert sarsıntı (amplitude=15, 0.2s) + sönümlü sarsıntı (8→0, 0.6s), tümü unscaled
    private static IEnumerator SarsintiEnum(RectTransform hedef)
    {
        if (hedef == null) yield break;
        Vector3 baslangic = hedef.localPosition;

        float sertSure = 0.2f;
        float gecen = 0f;
        while (gecen < sertSure)
        {
            gecen += Time.unscaledDeltaTime;
            if (hedef == null) yield break;
            Vector2 d = Random.insideUnitCircle * 15f;
            hedef.localPosition = baslangic + new Vector3(d.x, d.y, 0f);
            yield return null;
        }

        float sonumluSure = 0.6f;
        gecen = 0f;
        while (gecen < sonumluSure)
        {
            gecen += Time.unscaledDeltaTime;
            if (hedef == null) yield break;
            float t = Mathf.Clamp01(gecen / sonumluSure);
            Vector2 d = Random.insideUnitCircle * Mathf.Lerp(8f, 0f, t);
            hedef.localPosition = baslangic + new Vector3(d.x, d.y, 0f);
            yield return null;
        }

        if (hedef != null)
            hedef.localPosition = baslangic;
    }
}
