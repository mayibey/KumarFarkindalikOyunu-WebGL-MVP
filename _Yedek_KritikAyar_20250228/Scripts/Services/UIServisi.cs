using System;

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
