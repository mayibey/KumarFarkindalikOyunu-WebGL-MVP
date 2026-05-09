using System;

/// <summary>
/// Senaryo (zorluk, scatter, çarpan, bonus bütçe) işlemleri için wrapper servis. Asıl mantık OyunYoneticisi'nde kalır; delegasyon ile çağrılır.
/// </summary>
public class SenaryoServisi
{
    private Action<float> _setZorlukImpl;
    private Func<float, float, float> _biasMultiplierImpl;
    private Func<bool, float> _getScatterChanceImpl;
    private Func<int> _getScatterEsikImpl;
    private Func<int> _getMaxScatterPerSpinImpl;
    private Func<float> _getCarpanUretimOlasiligiImpl;
    private Func<int> _getMaxCarpanAdediImpl;
    private Func<bool> _isCarpanSadeceBonusImpl;
    private Func<bool> _isCarpanUretimiAktifImpl;
    private Action<long> _initBonusBudgetFromHavuzImpl;
    private Func<int> _getBonusRemainingPayableTLImpl;
    private Action<int> _recordBonusPaymentImpl;

    public void SetSetZorlukImpl(Action<float> impl) => _setZorlukImpl = impl;
    public void SetBiasMultiplierImpl(Func<float, float, float> impl) => _biasMultiplierImpl = impl;
    public void SetGetScatterChanceImpl(Func<bool, float> impl) => _getScatterChanceImpl = impl;
    public void SetGetScatterEsikImpl(Func<int> impl) => _getScatterEsikImpl = impl;
    public void SetGetMaxScatterPerSpinImpl(Func<int> impl) => _getMaxScatterPerSpinImpl = impl;
    public void SetGetCarpanUretimOlasiligiImpl(Func<float> impl) => _getCarpanUretimOlasiligiImpl = impl;
    public void SetGetMaxCarpanAdediImpl(Func<int> impl) => _getMaxCarpanAdediImpl = impl;
    public void SetIsCarpanSadeceBonusImpl(Func<bool> impl) => _isCarpanSadeceBonusImpl = impl;
    public void SetIsCarpanUretimiAktifImpl(Func<bool> impl) => _isCarpanUretimiAktifImpl = impl;
    public void SetInitBonusBudgetFromHavuzImpl(Action<long> impl) => _initBonusBudgetFromHavuzImpl = impl;
    public void SetGetBonusRemainingPayableTLImpl(Func<int> impl) => _getBonusRemainingPayableTLImpl = impl;
    public void SetRecordBonusPaymentImpl(Action<int> impl) => _recordBonusPaymentImpl = impl;

    public void SetZorluk(float deger) => _setZorlukImpl?.Invoke(deger);
    public float BiasMultiplier(float easyMult, float hardMult) => _biasMultiplierImpl != null ? _biasMultiplierImpl(easyMult, hardMult) : 1f;
    public float GetScatterChance(bool bonusAktif) => _getScatterChanceImpl != null ? _getScatterChanceImpl(bonusAktif) : 0f;
    public int GetScatterEsik() => _getScatterEsikImpl != null ? _getScatterEsikImpl() : 4;
    public int GetMaxScatterPerSpin() => _getMaxScatterPerSpinImpl != null ? _getMaxScatterPerSpinImpl() : 5;
    public float GetCarpanUretimOlasiligi() => _getCarpanUretimOlasiligiImpl != null ? _getCarpanUretimOlasiligiImpl() : 0f;
    public int GetMaxCarpanAdedi() => _getMaxCarpanAdediImpl != null ? _getMaxCarpanAdediImpl() : 0;
    public bool IsCarpanSadeceBonus() => _isCarpanSadeceBonusImpl != null && _isCarpanSadeceBonusImpl();
    public bool IsCarpanUretimiAktif() => _isCarpanUretimiAktifImpl != null && _isCarpanUretimiAktifImpl();
    public void InitBonusBudgetFromHavuz(long odulHavuzuTL) => _initBonusBudgetFromHavuzImpl?.Invoke(odulHavuzuTL);
    public int GetBonusRemainingPayableTL() => _getBonusRemainingPayableTLImpl != null ? _getBonusRemainingPayableTLImpl() : int.MaxValue;
    public void RecordBonusPayment(int odenenTL) => _recordBonusPaymentImpl?.Invoke(odenenTL);
}
