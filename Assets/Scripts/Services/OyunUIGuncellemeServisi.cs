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
    TMP_Text SpinKazancText { get; }
    TMP_Text BonusOyunKazancText { get; }
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
    GameObject BonusBuyConfirmPanel { get; }
    GameObject BonusSatinAlRoot { get; }
    int GetBakiye();
    int GetBahis();
    int GetBahisMin();
    int GetBahisMax();
    bool GetBonusAktif();
    bool GetSpinCalisiyor();
    int GetBonusHakKalan();
    int GetBonusKazanc();
    int GetOturumKazanc();
    int GetSonSpinKazanci();
    bool GetSpinKazanciOturumaEklendi();
    int GetSonSpinKazancHamGoster();
    int GetSonSpinCarpanGoster();
    bool GetCarpanKutuUcusAktif();
    /// <summary>Çarpan kutuya uçuşu sırasında veya ara karede uçuş bayrağı kapalı kalsa bile kazanç metninde kullanılacak toplamlı çarpan (0 = sadece ham TL).</summary>
    int GetCarpanKutuUcusKazancMetniCarpanCarpimi();
    bool GetCarpanKutuUcusFormulKilit();
    int GetCarpanToplamCarpimInt();
    int GetSonSpinKazancCarpanliOnizlemeTL();
    int GetSonSpinKazancToplamGoster();
    int GetBonusMaliyeti();
    /// <summary>BahisText için geçici metin override'ı; null/boş ise normal "Bahis: X TL" formatı.</summary>
    string BahisUIOverrideMetni { get; }
    void RefreshCarpanDisplay();
    void ShowParaCekPanel();
    void HideParaCekPanel();
    void ShowBakiyeYuklePanel();
    void HideBakiyeYuklePanel();
    void OnParaCekOnay();
    void OnBakiyeYukleOnay();
    void SyncOtomatikSpinKalanTextVisibility();
}

/// <summary>
/// Ana oyun UI güncellemesi (bakiye, bahis, kazanç, oturum, buton durumu) ve para çek / bakiye yükle panel baglaması.
/// TL formatı OyunFormatServisi.FormatTL tek kaynak; servis FindGO/FindComp yapmaz.
/// </summary>
public class OyunUIGuncellemeServisi
{
    private IOyunUIGuncellemeBaglami _ctx;
    private TMP_Text _cevirButonText;
    private string _cevirButonOrijinalText;
    private bool _cevirButonTextHazir;
    private bool _kazancTextFitHazir;
    private float _kazancTextFontMax;

    public void SetBaglam(IOyunUIGuncellemeBaglami ctx)
    {
        _ctx = ctx;
    }

    /// <summary>UI_Guncelle karşılığı: bakiye, bahis, hak, oturum kazancı, son kazanç, çarpan, bonus satın al metin/buton.</summary>
    public void RefreshAllUI()
    {
        if (_ctx == null) return;

        if (_ctx.BakiyeText != null)
            _ctx.BakiyeText.text = "Bakiye: " + OyunFormatServisi.FormatTL(_ctx.GetBakiye());

        if (_ctx.BahisText != null)
        {
            // Scripted override (A5 Spin 4 bonus tuzağı): backend bahis 1000 TL, ama UI "TÜM BAKİYE" göstersin.
            string overrideMetin = _ctx.BahisUIOverrideMetni;
            if (!string.IsNullOrEmpty(overrideMetin))
                _ctx.BahisText.text = overrideMetin;
            else
                _ctx.BahisText.text = "Bahis: " + OyunFormatServisi.FormatTL(_ctx.GetBahis());
        }

        if (_ctx.HakText != null)
        {
            _ctx.HakText.gameObject.SetActive(_ctx.GetBonusAktif());
            if (_ctx.GetBonusAktif())
                _ctx.HakText.text = "Kalan Spin Hakkı: " + _ctx.GetBonusHakKalan();
        }

        CevirButonBonusSayaciniGuncelle();

        if (_ctx.SpinKazancText != null)
        {
            bool bonusAktif = _ctx.GetBonusAktif();
            _ctx.SpinKazancText.gameObject.SetActive(bonusAktif);
            if (bonusAktif)
            {
                int goster = _ctx.GetOturumKazanc();
                if (_ctx.GetSpinCalisiyor() && !_ctx.GetSpinKazanciOturumaEklendi())
                {
                    // Spin sırasında yalnızca önizleme toplamı kullanılır; gerçek ödeme ayrı state'te akar.
                    int toplamOnizleme = Mathf.Max(0, _ctx.GetSonSpinKazancToplamGoster());
                    goster += toplamOnizleme;
                }
                string yeniMetin = "OTURUM KAZANCI: " + OyunFormatServisi.FormatTL(goster);
                bool spinBasiGeriYazmaDurumu =
                    _ctx.GetSpinCalisiyor() &&
                    !_ctx.GetSpinKazanciOturumaEklendi() &&
                    _ctx.GetSonSpinKazancToplamGoster() <= 0;

                // Spin başında ara UI refresh, metni eski değere çekmesin.
                if (!spinBasiGeriYazmaDurumu || string.IsNullOrEmpty(_ctx.SpinKazancText.text))
                    _ctx.SpinKazancText.text = yeniMetin;
            }
        }

        if (_ctx.BonusOyunKazancText != null)
        {
            bool bonusAktif = _ctx.GetBonusAktif();
            _ctx.BonusOyunKazancText.gameObject.SetActive(bonusAktif);
            if (bonusAktif)
                _ctx.BonusOyunKazancText.text = "BONUS OYUN KAZANCI: " + OyunFormatServisi.FormatTL(Mathf.Max(0, _ctx.GetBonusKazanc()));
        }

        if (_ctx.KazancText != null)
        {
            KazancTextKutudaKalsin();
            // Tumble sırasında sadece ham TL; çarpan kutuya uçarken önce ham TL, her çarpan değince ham x birikim = toplam; spin bittikten sonra servis çarpanı >1 ise formül.
            bool spinDevamEdiyor = _ctx.GetSpinCalisiyor() && !_ctx.GetSpinKazanciOturumaEklendi();
            bool carpanUcusAktif = _ctx.GetCarpanKutuUcusAktif();
            // Formül kilidi aktifse spin bitmişken de göster (sonraki spin başlayana kadar kalır).
            bool carpanUcusFormulDalinda = carpanUcusAktif || _ctx.GetCarpanKutuUcusFormulKilit();
            int gosterilecekKazanc = spinDevamEdiyor
                ? Mathf.Max(0, _ctx.GetSonSpinKazancToplamGoster())
                : Mathf.Max(0, _ctx.GetSonSpinKazanci());
            int ham = Mathf.Max(0, _ctx.GetSonSpinKazancHamGoster());
            int carpanUcusMetinCarpimi = _ctx.GetCarpanKutuUcusKazancMetniCarpanCarpimi();
            if (carpanUcusFormulDalinda)
            {
                if (carpanUcusMetinCarpimi <= 0)
                    _ctx.KazancText.text = "KAZANÇ: " + OyunFormatServisi.FormatTL(ham);
                else
                {
                    int carpanliToplam = Mathf.Max(0, _ctx.GetSonSpinKazancCarpanliOnizlemeTL());
                    _ctx.KazancText.text = "KAZANÇ: " + OyunFormatServisi.FormatTL(ham) + " x " + carpanUcusMetinCarpimi + " = " + OyunFormatServisi.FormatTL(carpanliToplam);
                }
            }
            else
                _ctx.KazancText.text = "KAZANÇ: " + OyunFormatServisi.FormatTL(gosterilecekKazanc);
        }

        _ctx.RefreshCarpanDisplay();

        int bonusMaliyeti = _ctx.GetBonusMaliyeti();
        if (_ctx.BonusSatinAlText != null)
            _ctx.BonusSatinAlText.text = "BONUS SATIN AL\n" + bonusMaliyeti + " TL";
        if (_ctx.BonusSatinAlRoot != null)
            _ctx.BonusSatinAlRoot.SetActive(!_ctx.GetBonusAktif());
        PanelAcikliginaGoreAnaButonlariGuncelle();
        _ctx.SyncOtomatikSpinKalanTextVisibility();
    }

    private void KazancTextKutudaKalsin()
    {
        if (!(_ctx.KazancText is TextMeshProUGUI tmp)) return;

        if (!_kazancTextFitHazir)
        {
            _kazancTextFontMax = tmp.fontSize > 0.1f ? tmp.fontSize : 42f;
            _kazancTextFitHazir = true;
        }

        tmp.enableAutoSizing = true;
        tmp.fontSizeMax = _kazancTextFontMax;
        tmp.fontSizeMin = Mathf.Max(14f, _kazancTextFontMax * 0.48f);
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Truncate;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    void CevirButonBonusSayaciniGuncelle()
    {
        if (_ctx == null || _ctx.CevirButon == null) return;
        if (!_cevirButonTextHazir)
        {
            _cevirButonText = _ctx.CevirButon.GetComponentInChildren<TMP_Text>(true);
            if (_cevirButonText != null)
            {
                _cevirButonOrijinalText = _cevirButonText.text;
                if (string.IsNullOrWhiteSpace(_cevirButonOrijinalText))
                    _cevirButonOrijinalText = "ÇEVİR";
            }
            _cevirButonTextHazir = true;
        }
        if (_cevirButonText == null) return;

        if (_ctx.GetBonusAktif())
        {
            int kalan = Mathf.Max(0, _ctx.GetBonusHakKalan());
            _cevirButonText.text = $"BONUS {kalan}";
        }
        else
        {
            if (string.IsNullOrWhiteSpace(_cevirButonOrijinalText))
                _cevirButonOrijinalText = "ÇEVİR";
            _cevirButonText.text = _cevirButonOrijinalText;
        }
    }

    /// <summary>ButonDurumu karşılığı: çevir, bahis +/- ve bonus satın al interactable.</summary>
    public void SetButtonsInteractable(bool acik)
    {
        if (_ctx == null) return;

        // İstek: oyun sırasında butonlar kilitlenmesin.
        if (_ctx.CevirButon != null)
            _ctx.CevirButon.interactable = true;

        bool bahisAcik = true;
        int bahis = _ctx.GetBahis();
        int bahisMin = _ctx.GetBahisMin();
        int bahisMax = _ctx.GetBahisMax();

        if (_ctx.BahisAzaltButon != null)
            _ctx.BahisAzaltButon.interactable = bahisAcik && (bahis > bahisMin);
        if (_ctx.BahisArttirButon != null)
            _ctx.BahisArttirButon.interactable = bahisAcik && (bahisMax <= 0 || bahis < bahisMax);

        int bonusMaliyeti = _ctx.GetBonusMaliyeti();
        if (_ctx.BonusSatinAlButon != null)
            _ctx.BonusSatinAlButon.interactable = (_ctx.GetBakiye() >= bonusMaliyeti);
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
        PanelStiliniBagla(_ctx.ParaCekPanel);
        PanelStiliniBagla(_ctx.BakiyeYuklePanel);
        PanelStiliniBagla(_ctx.BonusBuyConfirmPanel);
        PanelAcikliginaGoreAnaButonlariGuncelle();
    }

    static void PanelStiliniBagla(GameObject panel)
    {
        if (panel == null || panel.GetComponent<BakiyeYuklePanelMetinStili>() != null)
            return;
        panel.AddComponent<BakiyeYuklePanelMetinStili>();
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
            openButton.onClick.AddListener(() =>
            {
                onOpen?.Invoke();
                PanelAcikliginaGoreAnaButonlariGuncelle();
            });
        }
        if (onayButton != null)
        {
            onayButton.onClick.RemoveAllListeners();
            onayButton.onClick.AddListener(() =>
            {
                onOnay?.Invoke();
                PanelAcikliginaGoreAnaButonlariGuncelle();
            });
        }
        if (iptalButton != null)
        {
            iptalButton.onClick.RemoveAllListeners();
            iptalButton.onClick.AddListener(() =>
            {
                onClose?.Invoke();
                PanelAcikliginaGoreAnaButonlariGuncelle();
            });
        }
    }

    bool IsPanelAcik(GameObject panel) => panel != null && panel.activeInHierarchy;

    void PanelAcikliginaGoreAnaButonlariGuncelle()
    {
        if (_ctx == null) return;

        bool herhangiPanelAcik =
            IsPanelAcik(_ctx.ParaCekPanel) ||
            IsPanelAcik(_ctx.BakiyeYuklePanel) ||
            IsPanelAcik(_ctx.BonusBuyConfirmPanel);

        bool bonusButonTemelDurumu =
            (_ctx.GetBakiye() >= _ctx.GetBonusMaliyeti());

        if (_ctx.ParaCekButon != null)
            _ctx.ParaCekButon.interactable = !herhangiPanelAcik;
        if (_ctx.BakiyeYukleButon != null)
            _ctx.BakiyeYukleButon.interactable = !herhangiPanelAcik;
        if (_ctx.BonusSatinAlButon != null)
            _ctx.BonusSatinAlButon.interactable = !herhangiPanelAcik && bonusButonTemelDurumu;
    }
}
