using UnityEngine;

/// <summary>
/// ZorlukServisi için context arayüzü. OyunYoneticisi implement eder.
/// </summary>
public interface IZorlukBaglami
{
    void SetZorlukSliderDegeri(int v);
    void SetMinClusterSize(int value);
    void SetEasyBias01(float value);
    void SetHardBias01(float value);
    void SetScatterChanceNormal(float value);
    void ZorlukUIMetinVeLogGuncelle(int v);
}

/// <summary>
/// Zorluk/bias/scatter uygulama mantığı (SetZorluk). UI metin güncellemesi context üzerinden.
/// </summary>
public class ZorlukServisi
{
    private IZorlukBaglami _ctx;

    public void SetBaglam(IZorlukBaglami ctx)
    {
        _ctx = ctx;
    }

    /// <summary>Zorluk değerini uygula (4–12 clamp, bias, scatter chance). UI metin context'te güncellenir.</summary>
    public void ZorlukUygula(float deger)
    {
        if (_ctx == null) return;

        int v = Mathf.RoundToInt(deger);
        v = OyunKorumaServisi.ClampZorluk(v);

        _ctx.SetMinClusterSize(OyunKorumaServisi.TUMBLE_SABIT_ESIK);
        _ctx.SetZorlukSliderDegeri(v);

        float easyBias = (v < 8) ? Mathf.InverseLerp(8f, 4f, v) : 0f;
        float hardBias = (v > 8) ? Mathf.InverseLerp(8f, 12f, v) : 0f;
        _ctx.SetEasyBias01(easyBias);
        _ctx.SetHardBias01(hardBias);

        // Scatter oranını zorluk burada DEĞİŞTİRMEZ; tek kaynak scatter slider'dır. Böylece %3 seçildiğinde gerçekten %3 kullanılır.
        _ctx.ZorlukUIMetinVeLogGuncelle(v);
    }
}
