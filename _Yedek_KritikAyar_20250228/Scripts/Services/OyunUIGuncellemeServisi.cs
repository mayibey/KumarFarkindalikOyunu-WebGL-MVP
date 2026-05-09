using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// OyunUIGuncellemeServisi için context arayüzü. OyunYoneticisi implement eder.
/// </summary>
public interface IOyunUIGuncellemeBaglami
{
    TMP_Text BakiyeText { get; }
    TMP_Text BahisText { get; }
    TMP_Text HakText { get; }
    TMP_Text OturumKazancText { get; }
    TMP_Text KazancText { get; }
    TMP_Text CarpanText { get; }
    TMP_Text BonusSatinAlText { get; }
    Button CevirButon { get; }
    Button ParaCekButon { get; }
    Button BakiyeYukleButon { get; }
    Button BahisArttirButon { get; }
    Button BahisAzaltButon { get; }
    Button BonusSatinAlButon { get; }
    Button ParaCekOnayButon { get; }
    Button ParaCekIptalButon { get; }
    Button BakiyeYukleOnayButon { get; }
    Button BakiyeYukleIptalButon { get; }
    GameObject ParaCekPanel { get; }
    GameObject BakiyeYuklePanel { get; }
    GameObject BonusSatinAlRoot { get; }
    int GetBakiye();
    int GetBahis();
    int GetBahisMin();
    int GetBahisMax();
    bool GetBonusAktif();
    bool GetSpinCalisiyor();
    int GetBonusHakKalan();
    int GetOturumKazanc();
    int GetSonSpinKazanci();
    bool GetSpinKazanciOturumaEklendi();
    int GetSonSpinKazancHamGoster();
    int GetSonSpinCarpanGoster();
    int GetSonSpinKazancToplamGoster();
    int GetBonusMaliyeti();
    void RefreshCarpanDisplay();
    void ShowParaCekPanel();
    void HideParaCekPanel();
    void ShowBakiyeYuklePanel();
    void HideBakiyeYuklePanel();
    void OnParaCekOnay();
    void OnBakiyeYukleOnay();
}

/// <summary>
/// Ana oyun UI güncellemesi (bakiye, bahis, kazanç, oturum, buton durumu) ve para çek / bakiye yükle panel baglaması.
/// TL formatı OyunFormatServisi.FormatTL tek kaynak; servis FindGO/FindComp yapmaz.
/// </summary>
public class OyunUIGuncellemeServisi
{
    private IOyunUIGuncellemeBaglami _ctx;

    public void SetBaglam(IOyunUIGuncellemeBaglami ctx)
    {
        _ctx = ctx;
    }

    /// <summary>UI_Guncelle karşılığı: bakiye, bahis, hak, oturum kazancı, son kazanç, çarpan, bonus satın al metin/buton.</summary>
    public void RefreshAllUI()
    {
        if (_ctx == null) return;

        if (_ctx.ParaCekButon != null) _ctx.ParaCekButon.interactable = true;
        if (_ctx.BakiyeYukleButon != null) _ctx.BakiyeYukleButon.interactable = true;

        if (_ctx.BakiyeText != null)
            _ctx.BakiyeText.text = "Bakiye: " + OyunFormatServisi.FormatTL(_ctx.GetBakiye());

        if (_ctx.BahisText != null)
            _ctx.BahisText.text = "Bahis: " + _ctx.GetBahis() + " TL";

        if (_ctx.HakText != null)
        {
            _ctx.HakText.gameObject.SetActive(_ctx.GetBonusAktif());
            if (_ctx.GetBonusAktif())
                _ctx.HakText.text = "Kalan Spin Hakkı: " + _ctx.GetBonusHakKalan();
        }

        if (_ctx.OturumKazancText != null)
        {
            _ctx.OturumKazancText.gameObject.SetActive(_ctx.GetBonusAktif());
            int goster = _ctx.GetOturumKazanc();
            if (_ctx.GetSpinCalisiyor() && !_ctx.GetSpinKazanciOturumaEklendi())
                goster += _ctx.GetSonSpinKazanci();
            _ctx.OturumKazancText.text = "OTURUM KAZANCI: " + OyunFormatServisi.FormatTL(goster);
        }

        if (_ctx.KazancText != null)
        {
            int ham = Mathf.Max(0, _ctx.GetSonSpinKazancHamGoster());
            int carpan = Mathf.Max(1, _ctx.GetSonSpinCarpanGoster());
            int toplam = Mathf.Max(0, _ctx.GetSonSpinKazancToplamGoster());
            if (ham <= 0 || toplam <= 0)
                _ctx.KazancText.text = "KAZANÇ: " + OyunFormatServisi.FormatTL(0);
            else if (carpan > 1)
                _ctx.KazancText.text = "KAZANÇ: " + OyunFormatServisi.FormatTL(ham) + " x " + carpan + " = " + OyunFormatServisi.FormatTL(toplam);
            else
                _ctx.KazancText.text = "KAZANÇ: " + OyunFormatServisi.FormatTL(toplam);
        }

        _ctx.RefreshCarpanDisplay();

        int bonusMaliyeti = _ctx.GetBonusMaliyeti();
        if (_ctx.BonusSatinAlText != null)
            _ctx.BonusSatinAlText.text = "BONUS SATIN AL\n" + bonusMaliyeti + " TL";
        if (_ctx.BonusSatinAlButon != null)
            _ctx.BonusSatinAlButon.interactable = !_ctx.GetSpinCalisiyor() && !_ctx.GetBonusAktif() && (_ctx.GetBakiye() >= bonusMaliyeti);
        if (_ctx.BonusSatinAlRoot != null)
            _ctx.BonusSatinAlRoot.SetActive(!_ctx.GetBonusAktif());
        if (_ctx.BakiyeYukleButon != null)
            _ctx.BakiyeYukleButon.interactable = true;
    }

    /// <summary>ButonDurumu karşılığı: çevir, bahis +/- ve bonus satın al interactable.</summary>
    public void SetButtonsInteractable(bool acik)
    {
        if (_ctx == null) return;

        if (_ctx.CevirButon != null)
            _ctx.CevirButon.interactable = acik;

        bool bahisAcik = acik && !_ctx.GetSpinCalisiyor() && !_ctx.GetBonusAktif();
        int bahis = _ctx.GetBahis();
        int bahisMin = _ctx.GetBahisMin();
        int bahisMax = _ctx.GetBahisMax();

        if (_ctx.BahisAzaltButon != null)
            _ctx.BahisAzaltButon.interactable = bahisAcik && (bahis > bahisMin);
        if (_ctx.BahisArttirButon != null)
            _ctx.BahisArttirButon.interactable = bahisAcik && (bahisMax <= 0 || bahis < bahisMax);

        int bonusMaliyeti = _ctx.GetBonusMaliyeti();
        if (_ctx.BonusSatinAlButon != null)
            _ctx.BonusSatinAlButon.interactable = acik && !_ctx.GetSpinCalisiyor() && !_ctx.GetBonusAktif() && (_ctx.GetBakiye() >= bonusMaliyeti);
    }

    /// <summary>Para çek / bakiye yükle panel wiring tek akış. RemoveAllListeners + AddListener ile çift bağlama önlenir; null-safe.</summary>
    public void WireMoneyPanelsIfNeeded()
    {
        if (_ctx == null) return;
        WireOnePanel(
            _ctx.ParaCekPanel,
            _ctx.ParaCekButon,
            _ctx.ParaCekOnayButon,
            _ctx.ParaCekIptalButon,
            _ctx.ShowParaCekPanel,
            _ctx.HideParaCekPanel,
            _ctx.OnParaCekOnay
        );
        WireOnePanel(
            _ctx.BakiyeYuklePanel,
            _ctx.BakiyeYukleButon,
            _ctx.BakiyeYukleOnayButon,
            _ctx.BakiyeYukleIptalButon,
            _ctx.ShowBakiyeYuklePanel,
            _ctx.HideBakiyeYuklePanel,
            _ctx.OnBakiyeYukleOnay
        );
    }

    private void WireOnePanel(
        GameObject panel,
        Button openButton,
        Button onayButton,
        Button iptalButton,
        System.Action onOpen,
        System.Action onClose,
        System.Action onOnay
    )
    {
        if (panel != null)
            panel.SetActive(false);
        if (openButton != null)
        {
            openButton.onClick.RemoveAllListeners();
            openButton.onClick.AddListener(() => onOpen?.Invoke());
        }
        if (onayButton != null)
        {
            onayButton.onClick.RemoveAllListeners();
            onayButton.onClick.AddListener(() => onOnay?.Invoke());
        }
        if (iptalButton != null)
        {
            iptalButton.onClick.RemoveAllListeners();
            iptalButton.onClick.AddListener(() => onClose?.Invoke());
        }
    }
}
