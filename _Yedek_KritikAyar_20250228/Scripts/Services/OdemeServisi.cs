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

    public int GetSpinOdenebilirLimit()
    {
        long havuz = GetHavuzTL();
        long limit = (long)Mathf.Floor(havuz * 0.10f);
        if (limit < 0) limit = 0;
        if (limit > int.MaxValue) return int.MaxValue;
        return (int)limit;
    }
}
