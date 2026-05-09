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
