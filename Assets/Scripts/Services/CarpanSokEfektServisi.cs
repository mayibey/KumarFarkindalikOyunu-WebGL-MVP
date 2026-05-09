using System.Collections;
using UnityEngine;

/// <summary>
/// Zorla çarpan ilk düşüş anında kısa ekran sarsıntısı + opsiyonel ses efekti oynatır.
/// İzole servis tasarımı sayesinde efekt kolayca kapatılıp geri alınabilir.
/// </summary>
public class CarpanSokEfektServisi
{
    private Coroutine _aktifSarsintiCoroutine;

    public void BaslatIlkDususSokEfekti(
        MonoBehaviour calistirici,
        RectTransform sarsintiHedefi,
        int carpanDegeri,
        AudioSource sesKaynak,
        AudioClip sesClip,
        float sesSeviyesi)
    {
        if (calistirici == null || sarsintiHedefi == null || carpanDegeri <= 0)
            return;

        if (_aktifSarsintiCoroutine != null)
            calistirici.StopCoroutine(_aktifSarsintiCoroutine);

        _aktifSarsintiCoroutine = calistirici.StartCoroutine(SarsintiEnum(
            sarsintiHedefi,
            carpanDegeri,
            sesKaynak,
            sesClip,
            sesSeviyesi));
    }

    private IEnumerator SarsintiEnum(
        RectTransform sarsintiHedefi,
        int carpanDegeri,
        AudioSource sesKaynak,
        AudioClip sesClip,
        float sesSeviyesi)
    {
        float carpanSkala = HesaplaCarpanSkalasi(carpanDegeri);
        float sure = Mathf.Lerp(0.16f, 0.42f, carpanSkala);
        float genlik = Mathf.Lerp(9f, 34f, carpanSkala);
        float frekans = Mathf.Lerp(22f, 38f, carpanSkala);

        if (sesKaynak != null && sesClip != null)
        {
            float eskiPitch = sesKaynak.pitch;
            sesKaynak.pitch = Mathf.Lerp(1.08f, 0.88f, carpanSkala);
            sesKaynak.PlayOneShot(sesClip, Mathf.Clamp01(sesSeviyesi));
            sesKaynak.pitch = eskiPitch;
        }

        Vector3 baslangic = sarsintiHedefi.localPosition;
        float gecen = 0f;
        while (gecen < sure)
        {
            if (sarsintiHedefi == null) yield break;
            gecen += Time.deltaTime;
            float t = Mathf.Clamp01(gecen / sure);
            float decay = 1f - t;
            float osilasyon = 0.5f + 0.5f * Mathf.Sin(gecen * frekans * Mathf.PI * 2f);
            float anlikGenlik = genlik * decay * osilasyon;
            Vector2 offset = Random.insideUnitCircle * anlikGenlik;
            sarsintiHedefi.localPosition = baslangic + new Vector3(offset.x, offset.y, 0f);
            yield return null;
        }

        if (sarsintiHedefi != null)
            sarsintiHedefi.localPosition = baslangic;

        _aktifSarsintiCoroutine = null;
    }

    private float HesaplaCarpanSkalasi(int carpanDegeri)
    {
        if (carpanDegeri >= 500) return 1f;
        if (carpanDegeri >= 250) return 0.84f;
        if (carpanDegeri >= 150) return 0.72f;
        if (carpanDegeri >= 100) return 0.60f;
        if (carpanDegeri >= 50) return 0.45f;
        if (carpanDegeri >= 10) return 0.28f;
        return 0.18f;
    }
}
