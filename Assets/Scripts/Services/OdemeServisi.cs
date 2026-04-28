using System;
using UnityEngine;

/// <summary>
/// Kasa / ödül havuzu ödeme ve bahis girişi katmanı. State tutmaz; tüm erişim delegate ile.
/// </summary>
public class OdemeServisi
{
    private Func<long> _getHavuzTL;
    private Action<int> _paraGirisiBolVeEkle;
    private Func<int, int> _odemeYapOdulHavuzundan;

    public void SetGetHavuzTL(Func<long> fn) => _getHavuzTL = fn;
    public void SetParaGirisiBolVeEkle(Action<int> fn) => _paraGirisiBolVeEkle = fn;
    public void SetOdemeYapOdulHavuzundan(Func<int, int> fn) => _odemeYapOdulHavuzundan = fn;

    public long GetHavuzTL() => _getHavuzTL != null ? _getHavuzTL.Invoke() : 0L;

    public void AddBahisToKasa(int tl)
    {
        if (tl > 0) _paraGirisiBolVeEkle?.Invoke(tl);
    }

    public int PayFromHavuz(int istenenTL) => (istenenTL <= 0) ? 0 : (_odemeYapOdulHavuzundan?.Invoke(istenenTL) ?? 0);

    private int? _odenebilirLimitOverride;
    private Func<int> _getOdenebilirLimitDynamic;

    /// <summary>Dinamik ödenebilir limit (senaryo: ödedikçe azalır, ödemedikçe artar). Dönen değer >= 0 ise kullanılır.</summary>
    public void SetGetOdenebilirLimitDynamic(Func<int> fn) => _getOdenebilirLimitDynamic = fn;

    /// <summary>Sabit override (eski davranış). null = havuzun %10'u.</summary>
    public void SetOdenebilirLimitOverride(int? tl) => _odenebilirLimitOverride = tl;

    public int GetSpinOdenebilirLimit()
    {
        if (_getOdenebilirLimitDynamic != null)
        {
            int dyn = _getOdenebilirLimitDynamic();
            if (dyn >= 0) return dyn;
        }
        if (_odenebilirLimitOverride.HasValue)
            return Mathf.Max(0, _odenebilirLimitOverride.Value);
        return int.MaxValue;
    }
}
