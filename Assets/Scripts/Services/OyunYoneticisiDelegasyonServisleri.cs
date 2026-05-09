using System;
using System.Collections;

/// <summary>
/// Dönüş ve Bonus akışı için wrapper servis. Asıl mantık OyunYoneticisi'nde kalır; delegasyon ile çağrılır.
/// </summary>
public class DonusServisi
{
    private Action _spinButonImpl;
    private Func<IEnumerator> _normalSpinAkisiImpl;
    private Action _baslatBonusImpl;
    private Func<IEnumerator> _bonusBaslangicAkisiImpl;
    private Func<IEnumerator> _bonusDongusuImpl;
    private Func<IEnumerator> _showBonusStartMessageImpl;
    private Func<int, IEnumerator> _showBonusEndMessageImpl;

    public void SetSpinButonImpl(Action impl) => _spinButonImpl = impl;
    public void SetNormalSpinAkisiImpl(Func<IEnumerator> impl) => _normalSpinAkisiImpl = impl;
    public void SetBaslatBonusImpl(Action impl) => _baslatBonusImpl = impl;
    public void SetBonusBaslangicAkisiImpl(Func<IEnumerator> impl) => _bonusBaslangicAkisiImpl = impl;
    public void SetBonusDongusuImpl(Func<IEnumerator> impl) => _bonusDongusuImpl = impl;
    public void SetShowBonusStartMessageImpl(Func<IEnumerator> impl) => _showBonusStartMessageImpl = impl;
    public void SetShowBonusEndMessageImpl(Func<int, IEnumerator> impl) => _showBonusEndMessageImpl = impl;

    public void SpinButon() => _spinButonImpl?.Invoke();
    public IEnumerator NormalSpinAkisi() => _normalSpinAkisiImpl != null ? _normalSpinAkisiImpl() : null;
    public void BaslatBonus() => _baslatBonusImpl?.Invoke();
    public IEnumerator BonusBaslangicAkisi() => _bonusBaslangicAkisiImpl != null ? _bonusBaslangicAkisiImpl() : null;
    public IEnumerator BonusDongusu() => _bonusDongusuImpl != null ? _bonusDongusuImpl() : null;
    public IEnumerator ShowBonusStartMessage() => _showBonusStartMessageImpl != null ? _showBonusStartMessageImpl() : null;
    public IEnumerator ShowBonusEndMessage(int bonusToplamKazanc) => _showBonusEndMessageImpl != null ? _showBonusEndMessageImpl(bonusToplamKazanc) : null;
}

/// <summary>
/// UI işlemleri için wrapper servis. Asıl mantık OyunYoneticisi'nde kalır; delegasyon ile çağrılır.
/// </summary>
public class UIServisi
{
    private Action _uiGuncelleImpl;
    private Action<bool> _butonDurumuImpl;
    private Action _showParaCekPanelImpl;
    private Action _hideParaCekPanelImpl;
    private Action _showBakiyeYuklePanelImpl;
    private Action _hideBakiyeYuklePanelImpl;
    private Action _closeMoneyPanelsImpl;
    private Action<int> _showBonusBuyConfirmPanelImpl;
    private Action _hideBonusBuyConfirmPanelImpl;
    private Action _uiAutoBaglaGerekirseImpl;
    private Action _resolveMoneyUIRefsIfMissingImpl;
    private Action _wireParaCekUIImpl;
    private Action _wireBakiyeYukleUIImpl;

    public void SetUIGuncelleImpl(Action impl) => _uiGuncelleImpl = impl;
    public void SetButonDurumuImpl(Action<bool> impl) => _butonDurumuImpl = impl;
    public void SetShowParaCekPanelImpl(Action impl) => _showParaCekPanelImpl = impl;
    public void SetHideParaCekPanelImpl(Action impl) => _hideParaCekPanelImpl = impl;
    public void SetShowBakiyeYuklePanelImpl(Action impl) => _showBakiyeYuklePanelImpl = impl;
    public void SetHideBakiyeYuklePanelImpl(Action impl) => _hideBakiyeYuklePanelImpl = impl;
    public void SetCloseMoneyPanelsImpl(Action impl) => _closeMoneyPanelsImpl = impl;
    public void SetShowBonusBuyConfirmPanelImpl(Action<int> impl) => _showBonusBuyConfirmPanelImpl = impl;
    public void SetHideBonusBuyConfirmPanelImpl(Action impl) => _hideBonusBuyConfirmPanelImpl = impl;
    public void SetUIAutoBaglaGerekirseImpl(Action impl) => _uiAutoBaglaGerekirseImpl = impl;
    public void SetResolveMoneyUIRefsIfMissingImpl(Action impl) => _resolveMoneyUIRefsIfMissingImpl = impl;
    public void SetWireParaCekUIImpl(Action impl) => _wireParaCekUIImpl = impl;
    public void SetWireBakiyeYukleUIImpl(Action impl) => _wireBakiyeYukleUIImpl = impl;

    public void UI_Guncelle() => _uiGuncelleImpl?.Invoke();
    public void ButonDurumu(bool acik) => _butonDurumuImpl?.Invoke(acik);
    public void ShowParaCekPanel() => _showParaCekPanelImpl?.Invoke();
    public void HideParaCekPanel() => _hideParaCekPanelImpl?.Invoke();
    public void ShowBakiyeYuklePanel() => _showBakiyeYuklePanelImpl?.Invoke();
    public void HideBakiyeYuklePanel() => _hideBakiyeYuklePanelImpl?.Invoke();
    public void CloseMoneyPanels() => _closeMoneyPanelsImpl?.Invoke();
    public void ShowBonusBuyConfirmPanel(int cost) => _showBonusBuyConfirmPanelImpl?.Invoke(cost);
    public void HideBonusBuyConfirmPanel() => _hideBonusBuyConfirmPanelImpl?.Invoke();
    public void UIAutoBaglaGerekirse() => _uiAutoBaglaGerekirseImpl?.Invoke();
    public void ResolveMoneyUIRefsIfMissing() => _resolveMoneyUIRefsIfMissingImpl?.Invoke();
    public void WireParaCekUI() => _wireParaCekUIImpl?.Invoke();
    public void WireBakiyeYukleUI() => _wireBakiyeYukleUIImpl?.Invoke();
}

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
