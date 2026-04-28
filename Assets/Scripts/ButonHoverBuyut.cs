using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Buton üzerine gelince yumuşak şekilde büyütür.
/// </summary>
public class ButonHoverBuyut : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Range(1f, 1.6f)]
    public float hoverScale = 1.4f;

    [Range(0.02f, 0.25f)]
    public float gecisSuresi = 0.08f;

    private RectTransform _rt;
    private Vector3 _normalScale;
    private Vector3 _hedefScale;
    private Vector3 _hizRef;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        if (_rt != null)
            _normalScale = _rt.localScale;
    }

    void OnEnable()
    {
        if (_rt != null)
            _rt.localScale = _normalScale;
        _hedefScale = _normalScale;
        _hizRef = Vector3.zero;
    }

    void Update()
    {
        if (_rt == null) return;
        _rt.localScale = Vector3.SmoothDamp(_rt.localScale, _hedefScale, ref _hizRef, gecisSuresi);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_rt == null) return;
        _hedefScale = _normalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_rt == null) return;
        _hedefScale = _normalScale;
    }
}
