using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(RectTransform))]
public class OyunaBaslaButonu : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Modal")]
    [Tooltip("Click + flash sonrası SetActive(true) yapılacak modal root GameObject.")]
    [SerializeField] private GameObject kullaniciAdiModalRoot;

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

        // Önceki versiyondan kalma FlashGlow GO'sunu temizle
        if (transform.parent != null)
        {
            var eskiGlow = transform.parent.Find("FlashGlow");
            if (eskiGlow != null) DestroyImmediate(eskiGlow.gameObject);
        }
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
        Debug.Log("[OyunaBaslaButonu] Flash başladı");
        float fazSuresi = Mathf.Max(0.001f, flashSuresi * 0.5f);

        for (int i = 0; i < flashSayisi; i++)
        {
            // Faz A: beyaza + büyü
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
            // Faz B: geri dön
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

        ModalAc();
    }

    void ModalAc()
    {
        if (kullaniciAdiModalRoot == null)
        {
            Debug.LogError("[OyunaBaslaButonu] kullaniciAdiModalRoot atanmamış — modal açılamadı.");
            return;
        }
        Debug.Log("[OyunaBaslaButonu] Flash bitti, modal açılıyor.");
        kullaniciAdiModalRoot.SetActive(true);
    }
}
