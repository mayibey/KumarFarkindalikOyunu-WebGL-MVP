using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Admin panel slider eventleri ve TMP text güncellemeleri (zorluk, scatter, çarpan olasılık/adet).
/// </summary>
public class AdminAyarUIServisi
{
    private Slider _zorlukSlider;
    private TMP_Text _zorlukValueText;
    private Action<float> _applyZorluk;

    private Slider _scatterSlider;
    private TMP_Text _scatterSliderText;
    private Action<float> _applyScatterYuzde;

    private Slider _carpanOlasilikSlider;
    private TMP_Text _carpanOlasilikLabelText;
    private TMP_Text _carpanOlasilikValueText;
    private Action<float> _applyCarpanOlasilikYuzde;

    private Slider _carpanMaxAdetSlider;
    private TMP_Text _carpanMaxAdetLabelText;
    private TMP_Text _carpanMaxAdetValueText;
    private Action<int> _applyCarpanMaxAdet;

    public void SetZorlukUI(Slider zorlukSlider, TMP_Text zorlukValueText, Action<float> applyZorluk)
    {
        _zorlukSlider = zorlukSlider;
        _zorlukValueText = zorlukValueText;
        _applyZorluk = applyZorluk;
    }

    public void SetScatterUI(Slider scatterSlider, TMP_Text scatterSliderText, Action<float> applyScatterYuzde)
    {
        _scatterSlider = scatterSlider;
        _scatterSliderText = scatterSliderText;
        _applyScatterYuzde = applyScatterYuzde;
    }

    public void SetCarpanOlasilikUI(Slider slider, TMP_Text labelText, TMP_Text valueText, Action<float> applyYuzde)
    {
        _carpanOlasilikSlider = slider;
        _carpanOlasilikLabelText = labelText;
        _carpanOlasilikValueText = valueText;
        _applyCarpanOlasilikYuzde = applyYuzde;
    }

    public void SetCarpanMaxAdetUI(Slider slider, TMP_Text labelText, TMP_Text valueText, Action<int> applyAdet)
    {
        _carpanMaxAdetSlider = slider;
        _carpanMaxAdetLabelText = labelText;
        _carpanMaxAdetValueText = valueText;
        _applyCarpanMaxAdet = applyAdet;
    }

    public void BindAllAndRefresh()
    {
        if (_zorlukSlider != null)
        {
            _zorlukSlider.onValueChanged.RemoveAllListeners();
            _zorlukSlider.onValueChanged.AddListener(OnZorlukValueChanged);
            OnZorlukValueChanged(_zorlukSlider.value);
        }

        if (_scatterSlider != null)
        {
            _scatterSlider.onValueChanged.RemoveAllListeners();
            _scatterSlider.onValueChanged.AddListener(OnScatterValueChanged);
            OnScatterValueChanged(_scatterSlider.value);
        }

        if (_carpanOlasilikSlider != null)
        {
            _carpanOlasilikSlider.onValueChanged.RemoveAllListeners();
            _carpanOlasilikSlider.onValueChanged.AddListener(OnCarpanOlasilikValueChanged);
            OnCarpanOlasilikValueChanged(_carpanOlasilikSlider.value);
        }

        if (_carpanMaxAdetSlider != null)
        {
            _carpanMaxAdetSlider.onValueChanged.RemoveAllListeners();
            _carpanMaxAdetSlider.onValueChanged.AddListener(OnCarpanMaxAdetValueChanged);
            OnCarpanMaxAdetValueChanged(_carpanMaxAdetSlider.value);
        }
    }

    private void OnZorlukValueChanged(float value)
    {
        int zorluk = Mathf.RoundToInt(value);
        zorluk = Mathf.Clamp(zorluk, 4, 12);
        if (_zorlukValueText != null)
            _zorlukValueText.text = $"Zorluk: {zorluk}";
        _applyZorluk?.Invoke(value);
    }

    private void OnScatterValueChanged(float value)
    {
        // Slider 0-100 veya 0-1 olabilir; metinde her zaman 0-100 yüzde göster
        int yuzde = (value > 1f) ? Mathf.RoundToInt(value) : Mathf.RoundToInt(value * 100f);
        yuzde = Mathf.Clamp(yuzde, 0, 100);
        if (_scatterSliderText != null)
            _scatterSliderText.text = $"Scatter Düşme %{yuzde}";
        _applyScatterYuzde?.Invoke(value);
    }

    private void OnCarpanOlasilikValueChanged(float value)
    {
        float yuzde = Mathf.Clamp(value, 0f, 100f);
        if (_carpanOlasilikLabelText != null)
            _carpanOlasilikLabelText.text = $"%{Mathf.RoundToInt(yuzde)}";
        if (_carpanOlasilikValueText != null)
            _carpanOlasilikValueText.text = $"{Mathf.RoundToInt(yuzde)}%";
        _applyCarpanOlasilikYuzde?.Invoke(value);
    }

    private void OnCarpanMaxAdetValueChanged(float value)
    {
        int adet = Mathf.RoundToInt(value);
        adet = OyunKorumaServisi.ClampCarpanAdet(adet);
        if (_carpanMaxAdetLabelText != null)
            _carpanMaxAdetLabelText.text = adet.ToString();
        if (_carpanMaxAdetValueText != null)
            _carpanMaxAdetValueText.text = adet.ToString();
        _applyCarpanMaxAdet?.Invoke(adet);
    }

    /// <summary>Inspector / dış çağrılar için (slider OnValueChanged OY'da başka yerde bağlıysa).</summary>
    public void ApplyZorluk(float value) => OnZorlukValueChanged(value);
    public void ApplyScatter(float value) => OnScatterValueChanged(value);
    public void ApplyCarpanOlasilik(float value) => OnCarpanOlasilikValueChanged(value);
    public void ApplyCarpanMaxAdet(float value) => OnCarpanMaxAdetValueChanged(value);
}
