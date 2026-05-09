using System.Collections;
using TMPro;
using UnityEngine;

public class KazancSayaciUI : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private TMP_Text kazancText;

    [Header("Ayarlar")]
    [SerializeField] private float varsayilanSure = 1.2f;
    [SerializeField] private string paraBirimi = "₺";

    private Coroutine aktifSayacCoroutine;
    private int gosterilenDeger;

    private void Awake()
    {
        gosterilenDeger = 0;
        UpdateText(gosterilenDeger);
    }

    public void ResetCounter()
    {
        if (aktifSayacCoroutine != null)
        {
            StopCoroutine(aktifSayacCoroutine);
            aktifSayacCoroutine = null;
        }

        gosterilenDeger = 0;
        UpdateText(gosterilenDeger);
    }

    public void StartCount(int hedefDeger)
    {
        StartCount(0, hedefDeger, varsayilanSure);
    }

    public void StartCount(int baslangicDegeri, int hedefDeger, float sure)
    {
        if (kazancText == null)
        {
            Debug.LogWarning("[KazancSayaciUI] TMP_Text referansı atanmadı.");
            return;
        }

        if (aktifSayacCoroutine != null)
        {
            StopCoroutine(aktifSayacCoroutine);
            aktifSayacCoroutine = null;
        }

        aktifSayacCoroutine = StartCoroutine(CountRoutine(baslangicDegeri, hedefDeger, sure));
    }

    private IEnumerator CountRoutine(int baslangicDegeri, int hedefDeger, float sure)
    {
        gosterilenDeger = baslangicDegeri;
        UpdateText(gosterilenDeger);

        if (sure <= 0f)
        {
            gosterilenDeger = hedefDeger;
            UpdateText(gosterilenDeger);
            aktifSayacCoroutine = null;
            yield break;
        }

        float gecenSure = 0f;

        while (gecenSure < sure)
        {
            gecenSure += Time.deltaTime;

            float t = Mathf.Clamp01(gecenSure / sure);

            // Ease-out cubic
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            int yeniDeger = Mathf.RoundToInt(Mathf.Lerp(baslangicDegeri, hedefDeger, easedT));

            if (yeniDeger != gosterilenDeger)
            {
                gosterilenDeger = yeniDeger;
                UpdateText(gosterilenDeger);
            }

            yield return null;
        }

        gosterilenDeger = hedefDeger;
        UpdateText(gosterilenDeger);
        aktifSayacCoroutine = null;
    }

    private void UpdateText(int deger)
    {
        if (kazancText == null)
            return;

        kazancText.text = paraBirimi + deger.ToString("N0");
    }
}

/*
KULLANIM:

1) Canvas altında bir TextMeshPro text oluştur.
2) Bu scripti bir GameObject'e ekle.
3) Inspector'dan kazancText alanına ilgili TMP_Text'i bağla.

ÖRNEK ÇAĞIRIM:
kazancSayaciUI.ResetCounter();
kazancSayaciUI.StartCount(1250);

VEYA:
kazancSayaciUI.StartCount(500, 1200, 0.6f);
*/
