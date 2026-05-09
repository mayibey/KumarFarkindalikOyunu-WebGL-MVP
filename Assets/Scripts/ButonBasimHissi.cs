using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Butona basıldığında görsel geri bildirim: kısa süre küçülme (basılmış hissi).
/// İstediğin butona bu component'i ekle; Inspector'dan scale/ süre ayarlanabilir.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ButonBasimHissi : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Tooltip("Basılıyken scale (1 = değişmez, 0.92 = hafif küçülme)")]
    [Range(0.7f, 1f)]
    public float basiliScale = 0.94f;

    [Tooltip("Scale geçiş süresi (saniye)")]
    [Range(0.01f, 0.2f)]
    public float gecisSuresi = 0.06f;

    private RectTransform _rt;
    private Vector3 _normalScale;
    private bool _basili;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _normalScale = _rt != null ? _rt.localScale : Vector3.one;
    }

    void OnEnable()
    {
        _basili = false;
        if (_rt != null)
            _rt.localScale = _normalScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_rt == null) return;
        _basili = true;
        _rt.localScale = _normalScale * basiliScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        GeriAl();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_basili)
            GeriAl();
    }

    private void GeriAl()
    {
        _basili = false;
        if (_rt != null && gameObject.activeInHierarchy)
            _rt.localScale = _normalScale;
    }
}
