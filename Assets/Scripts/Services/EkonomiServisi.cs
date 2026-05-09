using System;
using UnityEngine;
using TMPro;

/// <summary>
/// Ekonomi işlemlerinin asıl implementasyonu. Bakiye/bahis state burada tutulur; GameManager ve PlayerPrefs ile senkronize edilir.
/// </summary>
public class EkonomiServisi
{
    private const string PP_BAKIYE = "PP_BAKIYE";
    private const string PP_BAHIS = "PP_BAHIS";

    private int _bakiye = 1000;
    private int _bahis = 10;

    public int Bakiye => _bakiye;
    public int Bahis => _bahis;

    // Bağımlılıklar (OyunYoneticisi tarafından set edilir)
    private LogServisi _logServisi;
    private TMP_InputField _paraCekInput;
    private TMP_InputField _bakiyeYukleInput;
    private TextMeshProUGUI _paraCekUyariText;
    private TextMeshProUGUI _bakiyeYukleUyariText;
    private int _bahisMin = 1;
    private int _bahisMax = 0;
    private int _bahisAdim = 1;
    private Func<bool> _canChangeBet;
    private Action _onEconomyChanged;
    private Func<long> _getCurrentMultiplier;
    private Action _carpanSifirla;
    private Action<int> _onParaCekildi;
    private Action _onBakiyeYuklemeReddedildi;

    public void SetLogServisi(LogServisi logServisi) => _logServisi = logServisi;
    public void SetOnParaCekildi(Action<int> onParaCekildi) => _onParaCekildi = onParaCekildi;
    public void SetOnBakiyeYuklemeReddedildi(Action onReddedildi) => _onBakiyeYuklemeReddedildi = onReddedildi;
    public void SetParaCekInput(TMP_InputField input) => _paraCekInput = input;
    public void SetBakiyeYukleInput(TMP_InputField input) => _bakiyeYukleInput = input;
    public void SetParaCekUyariText(TextMeshProUGUI text) => _paraCekUyariText = text;
    public void SetBakiyeYukleUyariText(TextMeshProUGUI text) => _bakiyeYukleUyariText = text;
    public int GetBakiyeYuklemeKalanHak() => _bakiyeYuklemeKalanHak;
    public void SetBahisLimits(int min, int max, int adim) { _bahisMin = min; _bahisMax = max; _bahisAdim = adim; }
    public void SetCanChangeBet(Func<bool> canChangeBet) => _canChangeBet = canChangeBet;
    public void SetOnEconomyChanged(Action onEconomyChanged) => _onEconomyChanged = onEconomyChanged;
    public void SetGetCurrentMultiplier(Func<long> getCurrentMultiplier) => _getCurrentMultiplier = getCurrentMultiplier;
    public void SetCarpanSifirla(Action carpanSifirla) => _carpanSifirla = carpanSifirla;

    public void EkonomiYukleGameManagerVeyaPrefs()
    {
        if (GameManager.I != null && GameManager.I.ActivePlayer != null)
        {
            _bakiye = GameManager.I.ActivePlayer.balance;
            _bahis = GameManager.I.SelectedBet;
            PlayerPrefs.SetInt(PP_BAKIYE, _bakiye);
            PlayerPrefs.SetInt(PP_BAHIS, _bahis);
            PlayerPrefs.Save();
            return;
        }
        _bakiye = PlayerPrefs.GetInt(PP_BAKIYE, _bakiye);
        _bahis = PlayerPrefs.GetInt(PP_BAHIS, _bahis);
    }

    public void EkonomiSenkronizeEt()
    {
        PlayerPrefs.SetInt(PP_BAKIYE, _bakiye);
        PlayerPrefs.SetInt(PP_BAHIS, _bahis);
        PlayerPrefs.Save();
        if (GameManager.I != null)
        {
            GameManager.I.SetBet(_bahis);
            if (GameManager.I.ActivePlayer != null)
            {
                GameManager.I.ActivePlayer.balance = _bakiye;
                GameManager.SaveProfiles(GameManager.I.Profiles);
            }
        }
        _onEconomyChanged?.Invoke();
    }

    private bool _paraCekIsProcessing = false;
    private int _bakiyeYuklemeKalanHak = 2;

    public void OnParaCekOnay()
    {
        if (_paraCekIsProcessing) return;
        if (_paraCekInput == null)
        {
            if (_paraCekUyariText != null) _paraCekUyariText.text = "❌ Input alanı bulunamadı.";
            return;
        }
        string raw = (_paraCekInput.text ?? "").Trim().Replace(".", "").Replace(",", "");
        // "Geçerli bir tutar gir" sadece sayı girilmediğinde (boş veya sayı değil)
        if (string.IsNullOrEmpty(raw) || !int.TryParse(raw, out int miktar))
        {
            if (_paraCekUyariText != null) _paraCekUyariText.text = "❌ Geçerli bir tutar gir (örn: 500 / 5000).";
            return;
        }
        if (miktar <= 0)
        {
            if (_paraCekUyariText != null) _paraCekUyariText.text = "❌ Tutar 0'dan büyük olmalı.";
            return;
        }
        if (_bakiye < miktar)
        {
            if (_paraCekUyariText != null) _paraCekUyariText.text = "❌ Bakiye yetersiz.";
            return;
        }
        // Çift tetiklenmeyi önle: tutarı aldıktan hemen sonra input'u temizle (onClick + onSubmit aynı anda tetiklenebilir).
        _paraCekInput.text = "";
        _paraCekIsProcessing = true;
        try
        {
            int prevBakiye = _bakiye;
            _bakiye -= miktar;
            EkonomiSenkronizeEt();
            if (GameManager.I != null && GameManager.I.ActivePlayer != null)
            {
                GameManager.I.ActivePlayer.totalWithdrawn += miktar;
                GameManager.I.ActivePlayer.totalNet -= miktar;
            }
            _logServisi?.KayitEkonomi("Para Çekildi", prevBakiye, _bakiye, miktar, 0, "WITHDRAW", $"Para çekildi: {OyunFormatServisi.FormatTL(miktar)}", miktar);
            if (_paraCekUyariText != null) _paraCekUyariText.text = $"✅ {OyunFormatServisi.FormatTL(miktar)} çekildi.";
            _onParaCekildi?.Invoke(miktar);
        }
        finally
        {
            _paraCekIsProcessing = false;
        }
    }

    /// <summary>True = yükleme uygulandı, False = reddedildi (hak yok).</summary>
    public bool OnBakiyeYukleOnay()
    {
        if (_bakiyeYuklemeKalanHak <= 0)
        {
            if (_bakiyeYukleUyariText != null)
                _bakiyeYukleUyariText.text = "❌ Daha fazla bakiye yükleme hakkın kalmadı.";
            _onBakiyeYuklemeReddedildi?.Invoke();
            return false;
        }

        // Senaryolu oyun için: sabit 20.000 TL yükleme akışı.
        int miktar = 20000;
        int prevBakiye = _bakiye;
        _bakiye += miktar;
        EkonomiSenkronizeEt();
        if (GameManager.I != null && GameManager.I.ActivePlayer != null)
        {
            GameManager.I.ActivePlayer.totalDeposited += miktar;
            GameManager.I.ActivePlayer.totalNet += miktar;
        }
        _logServisi?.KayitEkonomi("Bakiye Yüklendi", prevBakiye, _bakiye, miktar, 0, "DEPOSIT", $"Bakiye yüklendi: {OyunFormatServisi.FormatTL(miktar)}", miktar);
        SenaryoYoneticisi.I?.BakiyeYukle(miktar);
        _bakiyeYuklemeKalanHak = Mathf.Max(0, _bakiyeYuklemeKalanHak - 1);
        if (_bakiyeYukleUyariText != null)
            _bakiyeYukleUyariText.text = "✅ 20.000 TL HESABINIZA YUKLENMISTIR";
        return true;
    }

    public void BahisArttir()
    {
        if (_canChangeBet != null && !_canChangeBet()) return;
        int adim = Mathf.Max(1, _bahisAdim);
        int yeni = _bahis + adim;
        if (_bahisMax > 0) yeni = Mathf.Min(yeni, _bahisMax);
        if (_bahisMin > 0) yeni = Mathf.Max(yeni, _bahisMin);
        if (yeni == _bahis) return;
        _bahis = yeni;
        EkonomiSenkronizeEt();
    }

    public void BahisAzalt()
    {
        if (_canChangeBet != null && !_canChangeBet()) return;
        int adim = Mathf.Max(1, _bahisAdim);
        int yeni = _bahis - adim;
        if (_bahisMax > 0) yeni = Mathf.Min(yeni, _bahisMax);
        if (_bahisMin > 0) yeni = Mathf.Max(yeni, _bahisMin);
        if (yeni == _bahis) return;
        _bahis = yeni;
        EkonomiSenkronizeEt();
    }

    /// <summary>
    /// Admin senaryo preseti gibi akışlarda bahis değerini doğrudan ayarlar.
    /// Oyun içi min/max/adım sınırlarına göre güvenli biçimde clamp edilir.
    /// </summary>
    public void SetBahis(int hedefBahis)
    {
        int adim = Mathf.Max(1, _bahisAdim);
        int yeni = Mathf.Max(0, hedefBahis);
        if (_bahisMin > 0) yeni = Mathf.Max(yeni, _bahisMin);
        if (_bahisMax > 0) yeni = Mathf.Min(yeni, _bahisMax);
        if (adim > 1) yeni = Mathf.RoundToInt((float)yeni / adim) * adim;
        if (_bahisMin > 0) yeni = Mathf.Max(yeni, _bahisMin);
        if (_bahisMax > 0) yeni = Mathf.Min(yeni, _bahisMax);
        if (yeni == _bahis) return;
        _bahis = yeni;
        EkonomiSenkronizeEt();
    }

    public int UygulaSpinCarpani(int spinKazanci)
    {
        if (spinKazanci <= 0) { _carpanSifirla?.Invoke(); return 0; }
        long m = _getCurrentMultiplier != null ? _getCurrentMultiplier() : 1;
        if (m > 1)
        {
            long sonuc = (long)spinKazanci * m;
            if (sonuc > int.MaxValue) sonuc = int.MaxValue;
            return (int)sonuc;
        }
        return spinKazanci;
    }

    /// <summary>Spin başında temel bahis kadar düşer (Çift Şans vb. yoksa).</summary>
    public void DeductBet() => DeductSpinMaliyeti(_bahis);

    /// <summary>Spin başında verilen tutar bakiyeden düşer; totalWagered güncellenir (Çift Şans 1.5× vb.).</summary>
    public void DeductSpinMaliyeti(int spinMaliyetiTL)
    {
        int t = Mathf.Max(0, spinMaliyetiTL);
        if (GameManager.I != null && GameManager.I.ActivePlayer != null)
            GameManager.I.ActivePlayer.totalWagered += t;
        _bakiye -= t;
        EkonomiSenkronizeEt();
    }

    /// <summary>Spin/bonus sonunda kazanç eklenir; totalWon, totalNet, totalLost güncellenir.</summary>
    public void AddWinnings(int odenen, int spinBahisTL)
    {
        _bakiye += odenen;
        if (GameManager.I != null && GameManager.I.ActivePlayer != null)
        {
            GameManager.I.ActivePlayer.totalWon += odenen;
            int net = odenen - spinBahisTL;
            if (net < 0) GameManager.I.ActivePlayer.totalLost += -net;
            GameManager.I.ActivePlayer.totalNet += net;
        }
        EkonomiSenkronizeEt();
    }

    /// <summary>Bonus satın alma: bakiye düşer, totalWagered ve totalNet güncellenir.</summary>
    public void SubtractBakiyeForBonusBuy(int maliyet)
    {
        if (GameManager.I != null && GameManager.I.ActivePlayer != null)
        {
            GameManager.I.ActivePlayer.totalWagered += maliyet;
            GameManager.I.ActivePlayer.totalNet -= maliyet;
        }
        _bakiye -= maliyet;
        EkonomiSenkronizeEt();
    }

    /// <summary>Bakiye doğrudan set (örn. bonus havuz düzeltmesi).</summary>
    public void SetBakiye(int value)
    {
        _bakiye = Mathf.Max(0, value);
        EkonomiSenkronizeEt();
    }
}
