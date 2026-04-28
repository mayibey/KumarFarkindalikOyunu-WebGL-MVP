using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SPIN butonuna sürekli pulse + tıklama animasyonu ekler.
/// ButtonCevir GameObject'ine eklenir.
/// </summary>
[RequireComponent(typeof(Button))]
public class SpinButtonAnimator : MonoBehaviour
{
    [Range(0.01f, 0.15f)] public float pulseAmount = 0.05f;
    [Range(0.5f, 4f)]     public float pulseDuration = 2f;

    private Vector3 _baseScale;
    private Coroutine _pulseCoroutine;

    void Awake()
    {
        _baseScale = transform.localScale;
        var btn = GetComponent<Button>();
        btn.onClick.AddListener(OnClick);
        _pulseCoroutine = StartCoroutine(PulseLoop());
    }

    IEnumerator PulseLoop()
    {
        while (true)
        {
            float elapsed = 0f;
            while (elapsed < pulseDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / pulseDuration;
                // ease-in-out PingPong: 0 → peak → 0
                float ping = Mathf.PingPong(t * 2f, 1f);
                float ease = ping * ping * (3f - 2f * ping); // smoothstep
                transform.localScale = _baseScale * (1f + ease * pulseAmount);
                yield return null;
            }
        }
    }

    public void OnClick()
    {
        StartCoroutine(ClickAnim());
    }

    IEnumerator ClickAnim()
    {
        if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
        transform.localScale = _baseScale * 0.95f;
        yield return new WaitForSecondsRealtime(0.1f);
        transform.localScale = _baseScale;
        _pulseCoroutine = StartCoroutine(PulseLoop());
    }
}
