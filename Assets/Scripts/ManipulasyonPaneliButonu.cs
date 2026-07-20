using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 01_GirisScene "MANİPÜLASYON PANELİNE GİT" butonu.
/// Hover + flash davranışı OyunaBaslaButonu ile aynı; tıklanınca yönetici giriş
/// doğrulaması (AdminGirisDogrulama) açılır, başarılıysa 03_AdminOyunScene yüklenir.
/// Girişten vazgeçilirse buton tekrar tıklanabilir hale döner.
/// </summary>
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(RectTransform))]
public class ManipulasyonPaneliButonu : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Hedef")]
    [SerializeField] private string hedefSahne = "03_AdminOyunScene";
    [Tooltip("Sahne yüklenmeden önce yönetici kullanıcı adı/şifre doğrulaması istensin mi?")]
    [SerializeField] private bool girisDogrulamasiIste = false;

    [Header("Hover")]
    [SerializeField] private float hoverSuresi = 0.2f;
    [SerializeField] private float hoverScale = 1.1f;

    [Header("Click Flash")]
    [SerializeField] private int flashSayisi = 4;
    [SerializeField] private float flashSuresi = 0.15f;

    private Coroutine _aktifHoverCo;
    private bool _tiklandi;
    private Image _btnImage;
    private Color _btnOrijinalRenk = Color.white;

    void Awake()
    {
        _btnImage = GetComponent<Image>();
        if (_btnImage != null) _btnOrijinalRenk = _btnImage.color;
    }

    void OnEnable()
    {
        transform.localScale = Vector3.one;
        transform.localRotation = Quaternion.identity;
    }

    public void OnPointerEnter(PointerEventData _)
    {
        if (_tiklandi) return;
        if (_aktifHoverCo != null) StopCoroutine(_aktifHoverCo);
        _aktifHoverCo = StartCoroutine(ScaleAnimasyon(transform.localScale.x, hoverScale, hoverSuresi));
    }

    public void OnPointerExit(PointerEventData _)
    {
        if (_tiklandi) return;
        if (_aktifHoverCo != null) StopCoroutine(_aktifHoverCo);
        _aktifHoverCo = StartCoroutine(ScaleAnimasyon(transform.localScale.x, 1f, hoverSuresi));
    }

    public void OnPointerClick(PointerEventData _)
    {
        if (_tiklandi) return;
        _tiklandi = true;

        var btn = GetComponent<Button>();
        if (btn != null) btn.interactable = false;

        if (_aktifHoverCo != null) { StopCoroutine(_aktifHoverCo); _aktifHoverCo = null; }
        StartCoroutine(FlashEfekti());
    }

    IEnumerator ScaleAnimasyon(float baslangic, float hedef, float sure)
    {
        float t = 0f;
        while (t < sure)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / sure);
            float v = Mathf.Lerp(baslangic, hedef, p);
            transform.localScale = new Vector3(v, v, 1f);
            yield return null;
        }
        transform.localScale = new Vector3(hedef, hedef, 1f);
    }

    IEnumerator FlashEfekti()
    {
        float fazSuresi = Mathf.Max(0.001f, flashSuresi * 0.5f);

        for (int i = 0; i < flashSayisi; i++)
        {
            float t = 0f;
            while (t < fazSuresi)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / fazSuresi);
                if (_btnImage != null)
                    _btnImage.color = Color.Lerp(_btnOrijinalRenk, Color.white, p);
                float s = Mathf.Lerp(1.1f, 1.18f, p);
                transform.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            t = 0f;
            while (t < fazSuresi)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / fazSuresi);
                if (_btnImage != null)
                    _btnImage.color = Color.Lerp(Color.white, _btnOrijinalRenk, p);
                float s = Mathf.Lerp(1.18f, 1.1f, p);
                transform.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
        }

        if (_btnImage != null) _btnImage.color = _btnOrijinalRenk;
        transform.localScale = Vector3.one;

        PaneleGit();
    }

    void PaneleGit()
    {
        if (!girisDogrulamasiIste)
        {
            SahneyiYukle();
            return;
        }

        AdminGirisDogrulama.Ac(SahneyiYukle, ButonuSifirla);
    }

    void SahneyiYukle()
    {
        Debug.Log($"[ManipulasyonPaneliButonu] Manipülasyon paneline gidiliyor → {hedefSahne}");
        if (GameManager.I != null)
            GameManager.I.LoadScene(hedefSahne);
        else
            SceneManager.LoadScene(hedefSahne, LoadSceneMode.Single);
    }

    // Giriş penceresinde İPTAL denirse buton yeniden kullanılabilir olmalı.
    void ButonuSifirla()
    {
        _tiklandi = false;
        var btn = GetComponent<Button>();
        if (btn != null) btn.interactable = true;
        transform.localScale = Vector3.one;
        if (_btnImage != null) _btnImage.color = _btnOrijinalRenk;
    }
}
