using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public partial class OyunYoneticisi
{
    public void SetZorluk(float deger)
    {
        _adminManuelZorlukKilidi = true;
        _zorlukServisi?.ZorlukUygula(deger);
        OncedenHesaplananSpinOnbelleginiTemizle();
    }

    public bool AdminManuelZorlukKilidiAktif() => _adminManuelZorlukKilidi;
    public void AdminManuelZorlukKilidiAyarla(bool aktif) => _adminManuelZorlukKilidi = aktif;

    void IZorlukBaglami.SetZorlukSliderDegeri(int v)
    {
        _zorlukSliderDegeri = v;
        zorlukSeviyesi = v; // Panel / MevcutAyarlarMetni bu değeri okur; gerçekten uygulanan zorluk ile senkron olsun.
    }
    void IZorlukBaglami.SetMinClusterSize(int value) => minClusterSize = value;
    void IZorlukBaglami.SetEasyBias01(float value) => _easyBias01 = value;
    void IZorlukBaglami.SetHardBias01(float value) => _hardBias01 = value;
    void IZorlukBaglami.SetScatterChanceNormal(float value) => scatterChanceNormal = value;
    void IZorlukBaglami.ZorlukUIMetinVeLogGuncelle(int v)
    {
        if (zorlukValueText != null)
            zorlukValueText.text = $"Zorluk: {v}";
        Debug.Log($"[ADMIN] Zorluk={v} | tumbleEsiği(SABİT)={minClusterSize} | easyBias={_easyBias01:0.00} | hardBias={_hardBias01:0.00} | scatterChanceNormal={scatterChanceNormal:0.000}");
    }
    public void OnZorlukSliderChanged(float value)
    {
        _adminManuelZorlukKilidi = true;
        // AdminPanel slider'ı ilk açılış anında çalışıp dinleyiciler henüz hazır değilse
        // zorluk yine de uygulansın diye doğrudan senaryo tarafına da yansıtırız.
        if (_adminAyarUIServisi != null)
            _adminAyarUIServisi.ApplyZorluk(value);
        else
            _senaryoServisi?.SetZorluk(value);
        OncedenHesaplananSpinOnbelleginiTemizle();
    }
    public void OnScatterSliderChanged(float value)
    {
        _adminManuelScatterKilidi = true;
        _adminAyarUIServisi?.ApplyScatter(value);
        OncedenHesaplananSpinOnbelleginiTemizle();
    }
    public void OnCarpanOlasilikSliderChanged(float value)
    {
        _adminAyarUIServisi?.ApplyCarpanOlasilik(value);
        OncedenHesaplananSpinOnbelleginiTemizle();
    }
    public void OnCarpanMaxAdetSliderChanged(float value)
    {
        _adminAyarUIServisi?.ApplyCarpanMaxAdet(value);
        OncedenHesaplananSpinOnbelleginiTemizle();
    }
    public void SetCarpanOlasilikYuzde(float yuzde)
    {
        yuzde = Mathf.Clamp(yuzde, 0f, 100f);
        carpanUretimOlasiligi = yuzde / 100f;

        if (carpanOlasilikValueText != null)
            carpanOlasilikValueText.text = $"{Mathf.RoundToInt(yuzde)}%";

        Debug.Log($"[ADMIN] Çarpan olasılığı set edildi: %{yuzde} (0-1={carpanUretimOlasiligi})");
    }

    public void SetCarpanMaxAdet(float adet)
    {
        int v = Mathf.RoundToInt(adet);
        v = Mathf.Clamp(v, 1, 10);
        maxCarpanAdedi = v;

        if (carpanMaxAdetValueText != null)
            carpanMaxAdetValueText.text = v.ToString();

        Debug.Log($"[ADMIN] Max çarpan adedi set edildi: {maxCarpanAdedi}");
    }
    public void AdminForceOncedenHesaplananSpinTemizle()
    {
        OncedenHesaplananSpinOnbelleginiTemizle();
    }
    public bool IsSenaryo1Aktif() => IsAdminSenaryo1Aktif();

    public void AdminZorlaCarpanSec(int deger, bool popupGoster = true, string ozelMesaj = null)
    {
        if (deger > 0 && IsAdminSenaryo1Aktif())
        {
            Debug.LogWarning("[ADMIN] Senaryo 1 aktifken zorla çarpan engellendi (AdminZorlaCarpanSec).");
            return;
        }
        zorlaSiradakiCarpan = Mathf.Max(0, deger);
        if (carpanAyarlari != null)
            carpanAyarlari.ZorlaSiradakiCarpan = zorlaSiradakiCarpan;
        // UI'daki CarpanAktifToggle'ı ZORLA değiştirmiyoruz.
        // Force sadece bir sonraki spin simülasyonunu etkiler; toggle görseli kullanıcının seçimi olarak kalmalı.
        if (popupGoster)
        {
            string mesaj = string.IsNullOrWhiteSpace(ozelMesaj)
                ? (zorlaSiradakiCarpan > 0 ? $"FORCE x{zorlaSiradakiCarpan} ETKİN" : "FORCE SIFIRLANDI")
                : ozelMesaj;
            AdminForceMesajKutusuGoster(mesaj, 3f);
        }
        Debug.Log($"[ADMIN] Zorla çarpan seçildi: x{zorlaSiradakiCarpan}");
        // Force değişince bir önceki spinde arka planda hesaplanmış (Force'sız) sonuç geçersizdir.
        OncedenHesaplananSpinOnbelleginiTemizle();
    }

    void AdminForceMesajKutusuGoster(string mesaj, float sure)
    {
        const string popupAd = "AdminForceKisaMesajPopup";
        var mevcut = GameObject.Find(popupAd);
        if (mevcut != null)
            Destroy(mevcut);

        var canvasGo = new GameObject(popupAd);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 3600;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var panel = new GameObject("Panel");
        panel.transform.SetParent(canvasGo.transform, false);
        var panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 1f);
        panelRt.anchorMax = new Vector2(0.5f, 1f);
        panelRt.pivot = new Vector2(0.5f, 1f);
        panelRt.anchoredPosition = new Vector2(0f, -28f);
        panelRt.sizeDelta = new Vector2(520f, 80f);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.07f, 0.12f, 0.18f, 0.94f);
        panelImg.raycastTarget = false;

        var yaziGo = new GameObject("Mesaj");
        yaziGo.transform.SetParent(panel.transform, false);
        var yaziRt = yaziGo.AddComponent<RectTransform>();
        yaziRt.anchorMin = Vector2.zero;
        yaziRt.anchorMax = Vector2.one;
        yaziRt.offsetMin = new Vector2(12f, 8f);
        yaziRt.offsetMax = new Vector2(-12f, -8f);
        var yazi = yaziGo.AddComponent<TextMeshProUGUI>();
        yazi.text = mesaj;
        yazi.fontSize = 32;
        yazi.alignment = TMPro.TextAlignmentOptions.Center;
        yazi.color = new Color(0.58f, 0.96f, 0.62f, 1f);
        yazi.raycastTarget = false;

        if (sure > 0f)
            Destroy(canvasGo, sure);
    }
    private bool AdminAyarlariniKaydet()
    {
        try
        {
            PlayerPrefs.SetInt(PP_ADMIN_ODEME_EGILIMI, Mathf.Clamp(_odemeEgilimiYuzde, 0, 100));
            PlayerPrefs.SetInt(PP_ADMIN_ODEME_DAGILIMI, Mathf.Clamp(_odemeDagilimiYuzde, 0, 100));
            PlayerPrefs.SetInt(PP_ADMIN_MIN_ODEME, Mathf.Max(0, _minOdemeTL));
            PlayerPrefs.SetInt(PP_ADMIN_MAX_ODEME, Mathf.Max(0, _maxOdemeTL));
            PlayerPrefs.SetInt(PP_ADMIN_USTUSTE_KAZANC, Mathf.Max(0, _ustUsteKazancHedef));
            PlayerPrefs.SetInt(PP_ADMIN_USTUSTE_KAYIP, Mathf.Max(0, _ustUsteKayipHedef));
            PlayerPrefs.Save();
            Debug.Log($"[ADMIN][KAYIT] Kaydedildi -> Egilim={_odemeEgilimiYuzde} Dagilim={_odemeDagilimiYuzde} Min={_minOdemeTL} Max={_maxOdemeTL} UstUsteKazanc={_ustUsteKazancHedef} UstUsteKayip={_ustUsteKayipHedef}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("[ADMIN][KAYIT] Kaydetme hatası: " + ex.Message);
            return false;
        }
    }

    private void AdminAyarlariniYukle()
    {
        _odemeEgilimiYuzde = Mathf.Clamp(PlayerPrefs.GetInt(PP_ADMIN_ODEME_EGILIMI, _odemeEgilimiYuzde), 0, 100);
        _odemeDagilimiYuzde = Mathf.Clamp(PlayerPrefs.GetInt(PP_ADMIN_ODEME_DAGILIMI, _odemeDagilimiYuzde), 0, 100);
        _minOdemeTL = Mathf.Max(0, PlayerPrefs.GetInt(PP_ADMIN_MIN_ODEME, _minOdemeTL));
        _maxOdemeTL = Mathf.Max(_minOdemeTL, PlayerPrefs.GetInt(PP_ADMIN_MAX_ODEME, _maxOdemeTL));
        _ustUsteKazancHedef = Mathf.Max(0, PlayerPrefs.GetInt(PP_ADMIN_USTUSTE_KAZANC, _ustUsteKazancHedef));
        _ustUsteKayipHedef = Mathf.Max(0, PlayerPrefs.GetInt(PP_ADMIN_USTUSTE_KAYIP, _ustUsteKayipHedef));

        if (odemeEgilimiSliderUI != null) odemeEgilimiSliderUI.SetValueWithoutNotify(_odemeEgilimiYuzde);
        if (odemeDagilimiSliderUI != null) odemeDagilimiSliderUI.SetValueWithoutNotify(_odemeDagilimiYuzde);
        if (minOdemeInput != null) minOdemeInput.SetTextWithoutNotify(_minOdemeTL.ToString());
        if (maxOdemeInput != null) maxOdemeInput.SetTextWithoutNotify(_maxOdemeTL.ToString());
        if (ustUsteKazancInput != null) ustUsteKazancInput.SetTextWithoutNotify(_ustUsteKazancHedef.ToString());
        if (ustUsteKayipInput != null) ustUsteKayipInput.SetTextWithoutNotify(_ustUsteKayipHedef.ToString());

        // Kritik kural: üst üste döngü aktifse her zaman kazanç fazından başlat.
        // Böylece 5/1 gibi ayarlarda ilk spinin kayıp başlaması engellenir.
        UstUsteDonguAyarlariniYenile(true);
        AdminVideoArdisikKazancSayaciniGuncelle();
        OncedenHesaplananSpinOnbelleginiTemizle();
        Debug.Log($"[ADMIN][KAYIT] Yüklendi -> Egilim={_odemeEgilimiYuzde} Dagilim={_odemeDagilimiYuzde} Min={_minOdemeTL} Max={_maxOdemeTL} UstUsteKazanc={_ustUsteKazancHedef} UstUsteKayip={_ustUsteKayipHedef}");
    }

    private void AdminAyarSonucYaz(string mesaj, bool basarili)
    {
        AdminAyarSonucTextiniGarantiEt();
        if (ayarlarSonucText == null) return;
        ayarlarSonucText.text = mesaj;
        ayarlarSonucText.color = basarili ? new Color(0.5f, 0.95f, 0.55f, 1f) : new Color(1f, 0.4f, 0.4f, 1f);
    }

    private void AdminAyarSonucTextiniGarantiEt()
    {
        if (ayarlarSonucText != null) return;

        ayarlarSonucText = GameObject.Find("AyarlarSonuc")?.GetComponent<TMP_Text>();
        if (ayarlarSonucText != null) return;

        if (_adminAyarPanelKok == null)
            _adminAyarPanelKok = AdminSettingsPanelKokunuBul();
        if (_adminAyarPanelKok == null) return;

        var mevcut = _adminAyarPanelKok.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < mevcut.Length; i++)
        {
            var txt = mevcut[i];
            if (txt == null || txt.gameObject == null) continue;
            if (string.Equals(txt.gameObject.name, "AyarlarSonuc", StringComparison.OrdinalIgnoreCase))
            {
                ayarlarSonucText = txt;
                return;
            }
        }

        var sonucGo = new GameObject("AyarlarSonuc");
        sonucGo.transform.SetParent(_adminAyarPanelKok.transform, false);
        var sonucRt = sonucGo.AddComponent<RectTransform>();
        sonucRt.anchorMin = new Vector2(0.5f, 0f);
        sonucRt.anchorMax = new Vector2(0.5f, 0f);
        sonucRt.pivot = new Vector2(0.5f, 0.5f);
        sonucRt.anchoredPosition = new Vector2(0f, 32f);
        sonucRt.sizeDelta = new Vector2(760f, 46f);

        var sonucTxt = sonucGo.AddComponent<TextMeshProUGUI>();
        sonucTxt.fontSize = 28f;
        sonucTxt.enableAutoSizing = false;
        sonucTxt.alignment = TextAlignmentOptions.Center;
        sonucTxt.color = new Color(0.5f, 0.95f, 0.55f, 1f);
        sonucTxt.raycastTarget = false;
        sonucTxt.text = string.Empty;

        ayarlarSonucText = sonucTxt;
    }

    private void AdminOdemeAyarlariOkuVeUygula(bool donguyuSifirla, bool minMaxUstUsteInputlarindanOku = true)
    {
        AdminOdemeUIRefsiniBulGerekirse();
        _odemeEgilimiYuzde = SliderDegeriYuzdeyeCevir(odemeEgilimiSliderUI, _odemeEgilimiYuzde);
        _odemeDagilimiYuzde = SliderDegeriYuzdeyeCevir(odemeDagilimiSliderUI, _odemeDagilimiYuzde);
        if (minMaxUstUsteInputlarindanOku)
        {
            _minOdemeTL = InputDegeriPozitifInt(minOdemeInput, _minOdemeTL);
            _maxOdemeTL = InputDegeriPozitifInt(maxOdemeInput, _maxOdemeTL);
            _ustUsteKazancHedef = InputDegeriPozitifInt(ustUsteKazancInput, _ustUsteKazancHedef);
            _ustUsteKayipHedef = InputDegeriPozitifInt(ustUsteKayipInput, _ustUsteKayipHedef);
        }

        if (_maxOdemeTL < _minOdemeTL)
        {
            int t = _maxOdemeTL;
            _maxOdemeTL = _minOdemeTL;
            _minOdemeTL = t;
        }

        if (odemeEgilimiText != null) odemeEgilimiText.text = $"1) Ödeme Eğilimi %{_odemeEgilimiYuzde}";
        if (odemeDagilimiText != null) odemeDagilimiText.text = $"6) Ödeme Dağılımı %{_odemeDagilimiYuzde}";

        if (donguyuSifirla)
            UstUsteDonguAyarlariniYenile(true);

        AdminVideoArdisikKazancSayaciniGuncelle();

        Debug.Log($"[ADMIN][ODEME_MODEL] Egilim=%{_odemeEgilimiYuzde} Dagilim=%{_odemeDagilimiYuzde} Min={_minOdemeTL} Max={_maxOdemeTL} UstUsteKazanc={_ustUsteKazancHedef} UstUsteKayip={_ustUsteKayipHedef} Faz={(_ustUsteKazancFaziAktif ? "KAZANÇ" : "KAYIP")} Kalan={_ustUsteFazdaKalan}");
    }

    private static bool AdminOyunSahnesiMi()
    {
        string sn = SceneManager.GetActiveScene().name;
        return sn == "03_AdminOyunScene" || sn == "06_AdminOyunKopya";
    }
    private void AdminVideoArdisikKazancSayaciniGuncelle()
    {
        if (!AdminOyunSahnesiMi())
        {
            _adminVideoArdisikKazancSpinKalan = 0;
            return;
        }
        if (_ustUsteKayipHedef > 0)
        {
            _adminVideoArdisikKazancSpinKalan = 0;
            return;
        }
        _adminVideoArdisikKazancSpinKalan = Mathf.Max(0, _ustUsteKazancHedef);
    }

    private void AdminOdemeUIRefsiniBulGerekirse()
    {
        if (odemeEgilimiSliderUI == null) odemeEgilimiSliderUI = GameObject.Find("OdemeEgilimiSlider")?.GetComponent<Slider>();
        if (odemeDagilimiSliderUI == null) odemeDagilimiSliderUI = GameObject.Find("OdemeDagilimiSlider")?.GetComponent<Slider>();
        if (minOdemeInput == null) minOdemeInput = GameObject.Find("minOdemeInput")?.GetComponent<TMP_InputField>();
        if (maxOdemeInput == null)
            maxOdemeInput = GameObject.Find("MaxOdemeInput")?.GetComponent<TMP_InputField>() ?? GameObject.Find("maxOdemeInput")?.GetComponent<TMP_InputField>();
        if (ustUsteKazancInput == null) ustUsteKazancInput = GameObject.Find("ustustekazancinput")?.GetComponent<TMP_InputField>();
        if (ustUsteKayipInput == null) ustUsteKayipInput = GameObject.Find("ustustekayipinput")?.GetComponent<TMP_InputField>();
        var odemeEgilimiTextAday = GameObject.Find("OdemeEgilimiText")?.GetComponent<TMP_Text>();
        if (odemeEgilimiTextAday != null)
            odemeEgilimiText = odemeEgilimiTextAday;
        else if (odemeEgilimiText == null)
            odemeEgilimiText = GameObject.Find("OdemeEgilimiTxt")?.GetComponent<TMP_Text>();
        var odemeDagilimiTextAday = GameObject.Find("OdemeDagilimitxt")?.GetComponent<TMP_Text>()
            ?? GameObject.Find("OdemeDagilimiText")?.GetComponent<TMP_Text>();
        if (odemeDagilimiTextAday != null)
            odemeDagilimiText = odemeDagilimiTextAday;
    }

    private void AdminOdemeUIBindingleriniKur()
    {
        AdminOdemeUIRefsiniBulGerekirse();
        AdminOdemeInputStiliniYukselt();

        if (odemeEgilimiSliderUI != null)
        {
            odemeEgilimiSliderUI.minValue = 0f;
            odemeEgilimiSliderUI.maxValue = 100f;
            odemeEgilimiSliderUI.wholeNumbers = false;
            odemeEgilimiSliderUI.SetValueWithoutNotify(Mathf.Clamp(_odemeEgilimiYuzde, 0, 100));
            odemeEgilimiSliderUI.onValueChanged.RemoveListener(OnOdemeEgilimiSliderDegisti);
            odemeEgilimiSliderUI.onValueChanged.AddListener(OnOdemeEgilimiSliderDegisti);
            OnOdemeEgilimiSliderDegisti(odemeEgilimiSliderUI.value);
        }
        if (odemeDagilimiSliderUI != null)
        {
            odemeDagilimiSliderUI.minValue = 0f;
            odemeDagilimiSliderUI.maxValue = 100f;
            odemeDagilimiSliderUI.wholeNumbers = false;
            odemeDagilimiSliderUI.SetValueWithoutNotify(Mathf.Clamp(_odemeDagilimiYuzde, 0, 100));
            odemeDagilimiSliderUI.onValueChanged.RemoveListener(OnOdemeDagilimiSliderDegisti);
            odemeDagilimiSliderUI.onValueChanged.AddListener(OnOdemeDagilimiSliderDegisti);
            OnOdemeDagilimiSliderDegisti(odemeDagilimiSliderUI.value);
        }
        if (minOdemeInput != null)
        {
            minOdemeInput.onEndEdit.RemoveListener(OnMinOdemeInputDegisti);
            minOdemeInput.onEndEdit.AddListener(OnMinOdemeInputDegisti);
        }
        if (maxOdemeInput != null)
        {
            maxOdemeInput.onEndEdit.RemoveListener(OnMaxOdemeInputDegisti);
            maxOdemeInput.onEndEdit.AddListener(OnMaxOdemeInputDegisti);
        }
        if (ustUsteKazancInput != null)
        {
            ustUsteKazancInput.onEndEdit.RemoveListener(OnUstUsteKazancInputDegisti);
            ustUsteKazancInput.onEndEdit.AddListener(OnUstUsteKazancInputDegisti);
        }
        if (ustUsteKayipInput != null)
        {
            ustUsteKayipInput.onEndEdit.RemoveListener(OnUstUsteKayipInputDegisti);
            ustUsteKayipInput.onEndEdit.AddListener(OnUstUsteKayipInputDegisti);
        }
    }

    private void AdminOdemeInputStiliniYukselt()
    {
        UygulaAdminInputFontStili(minOdemeInput);
        UygulaAdminInputFontStili(maxOdemeInput);
        UygulaAdminInputFontStili(ustUsteKazancInput);
        UygulaAdminInputFontStili(ustUsteKayipInput);
    }

    private static void UygulaAdminInputFontStili(TMP_InputField input)
    {
        if (input == null) return;

        if (input.textComponent != null)
        {
            input.textComponent.fontSize = 30f;
            input.textComponent.enableAutoSizing = false;
            input.textComponent.alignment = TextAlignmentOptions.Midline;
        }

        if (input.placeholder is TMP_Text placeholder)
        {
            placeholder.fontSize = 30f;
            placeholder.enableAutoSizing = false;
            placeholder.alignment = TextAlignmentOptions.Midline;
        }

        if (input.textViewport != null)
        {
            var rt = input.textViewport;
            rt.offsetMin = new Vector2(10f, 6f);
            rt.offsetMax = new Vector2(-10f, -6f);
        }
    }

    private void OnOdemeEgilimiSliderDegisti(float value)
    {
        _odemeEgilimiYuzde = SliderDegeriYuzdeyeCevir(odemeEgilimiSliderUI, _odemeEgilimiYuzde);
        if (odemeEgilimiText != null) odemeEgilimiText.text = $"1) Ödeme Eğilimi %{_odemeEgilimiYuzde}";
    }

    private void OnOdemeDagilimiSliderDegisti(float value)
    {
        _odemeDagilimiYuzde = SliderDegeriYuzdeyeCevir(odemeDagilimiSliderUI, _odemeDagilimiYuzde);
        if (odemeDagilimiText != null) odemeDagilimiText.text = $"6) Ödeme Dağılımı %{_odemeDagilimiYuzde}";
    }

    private void OnMinOdemeInputDegisti(string _)
    {
        _minOdemeTL = InputDegeriPozitifInt(minOdemeInput, _minOdemeTL);
        if (_maxOdemeTL < _minOdemeTL)
        {
            _maxOdemeTL = _minOdemeTL;
            if (maxOdemeInput != null) maxOdemeInput.SetTextWithoutNotify(_maxOdemeTL.ToString());
        }
        if (minOdemeInput != null) minOdemeInput.SetTextWithoutNotify(_minOdemeTL.ToString());
    }

    private void OnMaxOdemeInputDegisti(string _)
    {
        _maxOdemeTL = InputDegeriPozitifInt(maxOdemeInput, _maxOdemeTL);
        if (_maxOdemeTL < _minOdemeTL)
        {
            _maxOdemeTL = _minOdemeTL;
            if (maxOdemeInput != null) maxOdemeInput.SetTextWithoutNotify(_maxOdemeTL.ToString());
        }
    }

    private void OnUstUsteKazancInputDegisti(string _)
    {
        _ustUsteKazancHedef = InputDegeriPozitifInt(ustUsteKazancInput, _ustUsteKazancHedef);
        if (ustUsteKazancInput != null) ustUsteKazancInput.SetTextWithoutNotify(_ustUsteKazancHedef.ToString());
        AdminVideoArdisikKazancSayaciniGuncelle();
    }

    private void OnUstUsteKayipInputDegisti(string _)
    {
        _ustUsteKayipHedef = InputDegeriPozitifInt(ustUsteKayipInput, _ustUsteKayipHedef);
        if (ustUsteKayipInput != null) ustUsteKayipInput.SetTextWithoutNotify(_ustUsteKayipHedef.ToString());
        AdminVideoArdisikKazancSayaciniGuncelle();
    }

    private static int SliderDegeriYuzdeyeCevir(Slider s, int varsayilan)
    {
        if (s == null) return Mathf.Clamp(varsayilan, 0, 100);
        float v = s.value;
        int yuzde = v > 1f ? Mathf.RoundToInt(v) : Mathf.RoundToInt(v * 100f);
        return Mathf.Clamp(yuzde, 0, 100);
    }

    private static int InputDegeriPozitifInt(TMP_InputField input, int varsayilan)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.text))
            return Mathf.Max(0, varsayilan);
        string raw = (input.text ?? string.Empty).Trim().Replace(",", ".");
        if (int.TryParse(raw, out int v)) return Mathf.Max(0, v);
        if (float.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float fv))
            return Mathf.Max(0, Mathf.RoundToInt(fv));
        return Mathf.Max(0, varsayilan);
    }

    private void UstUsteDonguAyarlariniYenile(bool kazancFaziIleBaslat)
    {
        bool aktif = _ustUsteKazancHedef > 0 || _ustUsteKayipHedef > 0;
        if (!aktif)
        {
            _ustUsteFazdaKalan = 0;
            _ustUsteKazancFaziAktif = true;
            return;
        }
        _ustUsteKazancFaziAktif = kazancFaziIleBaslat;
        UstUsteFazSayaciniAktifFazaKur();
    }

    private bool UstUsteDonguAktifMi() => _ustUsteKazancHedef > 0 || _ustUsteKayipHedef > 0;

    private bool UstUsteBeklenenKazancMi()
    {
        if (!UstUsteDonguAktifMi()) return false;
        if (IsAdminSenaryo2Aktif())
            return Senaryo2BeklenenKazancMi();
        if (_ustUsteFazdaKalan <= 0)
            UstUsteDonguAyarlariniYenile(true);
        return _ustUsteKazancFaziAktif;
    }

    private void UstUsteFazSayaciniAktifFazaKur()
    {
        if (!UstUsteDonguAktifMi())
        {
            _ustUsteFazdaKalan = 0;
            return;
        }

        int aktifFazHedef = _ustUsteKazancFaziAktif ? _ustUsteKazancHedef : _ustUsteKayipHedef;
        if (aktifFazHedef <= 0)
        {
            bool digerFazKazanc = !_ustUsteKazancFaziAktif;
            int digerFazHedef = digerFazKazanc ? _ustUsteKazancHedef : _ustUsteKayipHedef;
            if (digerFazHedef <= 0)
            {
                _ustUsteFazdaKalan = 0;
                return;
            }
            _ustUsteKazancFaziAktif = digerFazKazanc;
            aktifFazHedef = digerFazHedef;
        }

        _ustUsteFazdaKalan = aktifFazHedef;
    }

    private void UstUsteDonguyuSpinSonucuIleIlerle(bool kazancGerceklesti)
    {
        if (!UstUsteDonguAktifMi() && !IsAdminSenaryo4Aktif() && !IsAdminSenaryo5Aktif()) return;
        if (IsAdminSenaryo2Aktif())
        {
            _senaryo2DonguIndex = (_senaryo2DonguIndex + 1) % 5;
            return;
        }
        if (IsAdminSenaryo3Aktif())
        {
            _senaryo3DonguIndex = (_senaryo3DonguIndex + 1) % 5;
            return;
        }
        if (IsAdminSenaryo4Aktif())
        {
            _senaryo4DonguIndex = (_senaryo4DonguIndex + 1) % 3;
            return;
        }
        if (IsAdminSenaryo5Aktif())
        {
            int oncekiIdx = _senaryo5DonguIndex;
            _senaryo5DonguIndex = (_senaryo5DonguIndex + 1) % 3;
            bool popupKuruldu = oncekiIdx == 2;
            if (popupKuruldu)
                _senaryo5BombSonrasiPopupBekliyor = true;
            Debug.Log($"[S5][DÖNGÜ] oncekiIdx={oncekiIdx} → yeniIdx={_senaryo5DonguIndex} popupKuruldu={popupKuruldu}");
            return;
        }

        if (_ustUsteFazdaKalan <= 0)
            UstUsteFazSayaciniAktifFazaKur();

        bool buFazBasarili = _ustUsteKazancFaziAktif ? kazancGerceklesti : !kazancGerceklesti;
        if (IsAdminSenaryo3Aktif())
            Debug.Log($"[S3][DÖNGÜ] kazancGerceklesti={kazancGerceklesti} fazAktif={_ustUsteKazancFaziAktif} fazKalan={_ustUsteFazdaKalan} buFazBasarili={buFazBasarili}");
        if (!buFazBasarili) return;

        _ustUsteFazdaKalan = Mathf.Max(0, _ustUsteFazdaKalan - 1);
        if (_ustUsteFazdaKalan > 0) return;

        _ustUsteKazancFaziAktif = !_ustUsteKazancFaziAktif;
        UstUsteFazSayaciniAktifFazaKur();
        if (IsAdminSenaryo3Aktif())
            Debug.Log($"[S3][FAZ DEĞİŞTİ] yeniFaz={(_ustUsteKazancFaziAktif ? "KAZANÇ" : "KAYIP")} fazKalan={_ustUsteFazdaKalan}");
    }

    private bool OdemeModelineUygunMu(int nihaiOdeme, int bahis, int deneme, int maxReroll)
    {
        int min = Mathf.Max(0, _minOdemeTL);
        int max = Mathf.Max(min, _maxOdemeTL);
        bool senaryo2NetBandiAktif = IsAdminSenaryo2Aktif();
        bool senaryo3NetBandiAktif = IsAdminSenaryo3Aktif();
        bool senaryo4NetBandiAktif = IsAdminSenaryo4Aktif();
        bool senaryo5NetBandiAktif = IsAdminSenaryo5Aktif();
        bool ustUsteAktif = UstUsteDonguAktifMi();
        // Admin 2-5: döngü index'e göre belirle; rastgele eğilime düşülmesin.
        bool beklenenKazanc;
        if (senaryo3NetBandiAktif)
            beklenenKazanc = Senaryo3BeklenenKazancMi();
        else if (senaryo2NetBandiAktif)
            beklenenKazanc = Senaryo2BeklenenKazancMi();
        else if (senaryo4NetBandiAktif)
            beklenenKazanc = Senaryo4DonguSpinTipi() == SenaryoBombSpinTipi.Kazanc;
        else if (senaryo5NetBandiAktif)
            beklenenKazanc = Senaryo5DonguSpinTipi() == SenaryoBombSpinTipi.Kazanc;
        else if (ustUsteAktif)
            beklenenKazanc = UstUsteBeklenenKazancMi();
        else
            beklenenKazanc = UnityEngine.Random.value <= Mathf.Clamp01(_odemeEgilimiYuzde / 100f);

        bool kazanc = nihaiOdeme > bahis;

        // max=0 → bant kısıtlaması yok; sadece eğilim (kazanç/kayıp yönü) uygula.
        if (max == 0)
        {
            if (beklenenKazanc && !kazanc) return false;
            if (!beklenenKazanc && kazanc) return false;
            return true;
        }

        int efektifMin = min;
        int efektifMax = max;
        bool toleransAtlandi = SpinPolitikasiniAl().OdemeModelindeHedefToleransAtlanmali();
        SpinPolitikasiniAl().AdminOdemeEfektifBandiniUygula(bahis, beklenenKazanc, ref efektifMin, ref efektifMax);

        // Kesişim sadece tolerans atlanmıyorsa (S1 gibi): S2/3/4/5 için policy bandı tek başına geçerli,
        // _minOdemeTL/_maxOdemeTL (kazanç fazı için ayarlı) kayıp bandını sıfıra düşürmesin.
        if (!toleransAtlandi)
        {
            efektifMin = Mathf.Max(efektifMin, min);
            efektifMax = Mathf.Min(efektifMax, max);
            if (efektifMax < efektifMin)
                return false;
        }

        if (nihaiOdeme < efektifMin || nihaiOdeme > efektifMax)
            return false;

        // Faz aktifken öncelik: faz > eğilim > dağılım.
        // Kazanç fazı: ödeme eğilimi fiilen %100 kabul edilir (kazanç zorunlu).
        // Kayıp fazı: ödeme eğilimi bypass edilir; dağılım kaybın şiddetini belirler.
        if (beklenenKazanc && !kazanc) return false;
        if (!beklenenKazanc && kazanc) return false;

        if (toleransAtlandi) return true;

        int alt;
        int ust;
        if (beklenenKazanc)
        {
            alt = Mathf.Max(efektifMin, bahis + 1);
            ust = efektifMax;
        }
        else
        {
            alt = efektifMin;
            // Öncelik gerçek kayıp (<bahis). Aralık imkansızsa min'e yaslanıp başabaş kaçışını azalt.
            int kayipUst = Mathf.Min(efektifMax, bahis - 1);
            if (kayipUst >= alt)
                ust = kayipUst;
            else
                ust = Mathf.Clamp(efektifMin, efektifMin, efektifMax);
        }

        if (ust < alt) return false;
        int hedef = Mathf.RoundToInt(Mathf.Lerp(alt, ust, Mathf.Clamp01(_odemeDagilimiYuzde / 100f)));
        int tolerans = Mathf.Max(10, Mathf.RoundToInt((ust - alt) * 0.22f));
        if (deneme > Mathf.FloorToInt(maxReroll * 0.70f))
            tolerans = Mathf.Max(tolerans, 45);
        return Mathf.Abs(nihaiOdeme - hedef) <= tolerans;
    }
    private void AdminZorlaButonReferanslariniBulBirKez()
    {
        if (_adminZorlaButonReferanslariBulundu) return;
        _adminForceX5Btn = GameObject.Find("ForceX5")?.GetComponent<Button>();
        _adminForceX10Btn = GameObject.Find("ForceX10")?.GetComponent<Button>();
        _adminForceX50Btn = GameObject.Find("ForceX50")?.GetComponent<Button>();
        _adminForceX100Btn = GameObject.Find("ForceX100")?.GetComponent<Button>();
        _adminCarpanSifirlaBtn = GameObject.Find("CarpanSifirla")?.GetComponent<Button>();
        // Yalnızca en az bir buton bulunduysa hazır say; panel kapalıyken null gelirse sonraki frame'de tekrar dene.
        if (_adminForceX5Btn != null || _adminForceX10Btn != null || _adminForceX50Btn != null || _adminForceX100Btn != null)
            _adminZorlaButonReferanslariBulundu = true;
    }

    private void ZorlaButonlarininEtkilesiminiAyarla(bool etkin)
    {
        AdminZorlaButonReferanslariniBulBirKez();
        if (_adminForceX5Btn != null) _adminForceX5Btn.interactable = etkin;
        if (_adminForceX10Btn != null) _adminForceX10Btn.interactable = etkin;
        if (_adminForceX50Btn != null) _adminForceX50Btn.interactable = etkin;
        if (_adminForceX100Btn != null) _adminForceX100Btn.interactable = etkin;
        if (_adminCarpanSifirlaBtn != null) _adminCarpanSifirlaBtn.interactable = etkin;
    }
    private void Senaryo1IsindirmaPedagojikVarsayilanlariniUygula()
    {
        AdminBahisAyarla(300);
        int b = _ekonomiServisi != null ? Mathf.Max(1, _ekonomiServisi.Bahis) : 300;
        _ustUsteKazancHedef = 5;
        _ustUsteKayipHedef = 0;
        _minOdemeTL = b * 4;
        _maxOdemeTL = b * 5;
        _odemeEgilimiYuzde = 100;
        _senaryo1SonZorunluNihaiOdeme = -1;
        UstUsteDonguAyarlariniYenile(true);
        AdminOdemeUIRefsiniBulGerekirse();
        if (odemeEgilimiSliderUI != null)
            odemeEgilimiSliderUI.SetValueWithoutNotify(Mathf.Clamp(_odemeEgilimiYuzde, 0f, 100f));
        if (odemeDagilimiSliderUI != null)
            odemeDagilimiSliderUI.SetValueWithoutNotify(Mathf.Clamp(_odemeDagilimiYuzde, 0f, 100f));
        if (minOdemeInput != null) minOdemeInput.SetTextWithoutNotify(_minOdemeTL.ToString());
        if (maxOdemeInput != null) maxOdemeInput.SetTextWithoutNotify(_maxOdemeTL.ToString());
        if (ustUsteKazancInput != null) ustUsteKazancInput.SetTextWithoutNotify(_ustUsteKazancHedef.ToString());
        if (ustUsteKayipInput != null) ustUsteKayipInput.SetTextWithoutNotify(_ustUsteKayipHedef.ToString());
        if (odemeEgilimiText != null) odemeEgilimiText.text = $"1) Ödeme Eğilimi %{_odemeEgilimiYuzde}";
        if (odemeDagilimiText != null) odemeDagilimiText.text = $"6) Ödeme Dağılımı %{_odemeDagilimiYuzde}";
        AdminOdemeAyarlariOkuVeUygula(false, false);
        AdminZorlaCarpanSec(0, false, null);
        SpinPolitikasiniYenile();
        // Aşama geçişinden önce üretilmiş sonuç ilk spin'de kullanılmasın; yeni bandı delmesin.
        OncedenHesaplananSpinOnbelleginiTemizle();
        Debug.Log($"[SENARYO] Aşama 1 (Isındırma/Umut) pedagojik varsayılanları: Bahis={b} TL, Min/Max nihai ödeme={_minOdemeTL}–{_maxOdemeTL} TL (net kar ≈ {b * 3}–{b * 4} TL).");
    }

    private void SenaryoPedagojikOdemeVeZorlaKilidiGuncelle()
    {
        OdemeEgilimVeDagilimSliderKilidiniUygula();

        var sy = SenaryoYoneticisi.I;
        if (sy == null)
        {
            _pedagojikAsama1IsindirmaOnceki = false;
            ZorlaButonlarininEtkilesiminiAyarla(!IsAdminSenaryo1Veya2Veya3Aktif() && !IsAdminSenaryo4Aktif() && !IsAdminSenaryo5Aktif());
            return;
        }

        bool asama1Isindirma = sy.mevcutAsama == SenaryoYoneticisi.SenaryoAsama.Asama1_IsindirmaUmut;
        if (asama1Isindirma && !_pedagojikAsama1IsindirmaOnceki)
            Senaryo1IsindirmaPedagojikVarsayilanlariniUygula();

        _pedagojikAsama1IsindirmaOnceki = asama1Isindirma;
        ZorlaButonlarininEtkilesiminiAyarla(!asama1Isindirma && !IsAdminSenaryo1Veya2Veya3Aktif() && !IsAdminSenaryo4Aktif() && !IsAdminSenaryo5Aktif());
    }
    private System.Collections.IEnumerator Senaryo5PopupCoroutine()
    {
        // ── Canvas ──
        var canvasGo = new GameObject("Sen5PopupCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // ── Yarı saydam arka plan (arkası hafif görünsün) ──
        var bgGo = new GameObject("Sen5BG");
        bgGo.transform.SetParent(canvasGo.transform, false);
        var bgImg = bgGo.AddComponent<UnityEngine.UI.Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.52f);
        SetAnchors(bgGo, Vector2.zero, Vector2.one);

        // ── Ortalanmış panel (ekranın %68 genişliği, %42 yüksekliği) ──
        var panelGo = new GameObject("Sen5Panel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        var panelImg = panelGo.AddComponent<UnityEngine.UI.Image>();
        panelImg.color = new Color(0.07f, 0.08f, 0.18f, 0.97f);
        SetAnchors(panelGo, new Vector2(0.16f, 0.29f), new Vector2(0.84f, 0.71f));

        // ── Üst gold aksent çizgisi ──
        var accentGo = new GameObject("Sen5Accent");
        accentGo.transform.SetParent(panelGo.transform, false);
        var accentImg = accentGo.AddComponent<UnityEngine.UI.Image>();
        accentImg.color = new Color(0.88f, 0.72f, 0.08f, 1f);
        SetAnchors(accentGo, new Vector2(0f, 0.93f), new Vector2(1f, 1f));

        // ── Başlık ──
        var titleGo = new GameObject("Sen5Title");
        titleGo.transform.SetParent(panelGo.transform, false);
        var titleTmp = titleGo.AddComponent<TMPro.TextMeshProUGUI>();
        titleTmp.text = "ÖZEL TEKLİF";
        titleTmp.alignment = TMPro.TextAlignmentOptions.Center;
        titleTmp.fontSize = 22;
        titleTmp.fontStyle = TMPro.FontStyles.Bold;
        titleTmp.color = new Color(0.88f, 0.72f, 0.08f, 1f);
        SetAnchors(titleGo, new Vector2(0.05f, 0.73f), new Vector2(0.95f, 0.92f));

        // ── Mesaj metni ──
        var txtGo = new GameObject("Sen5Txt");
        txtGo.transform.SetParent(panelGo.transform, false);
        var tmp = txtGo.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = "Çok şanslı görünüyorsun!\nTüm bakiyen ile bonus oyun satın al,\n1000 katına kadar kazanma şansını yakala!";
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.fontSize = 20;
        tmp.color = new Color(0.92f, 0.92f, 0.92f, 1f);
        SetAnchors(txtGo, new Vector2(0.06f, 0.34f), new Vector2(0.94f, 0.72f));

        // ── Satın Al butonu (sağ) ──
        var btnGo = new GameObject("Sen5SatinAlBtn");
        btnGo.transform.SetParent(panelGo.transform, false);
        var btnImg = btnGo.AddComponent<UnityEngine.UI.Image>();
        btnImg.color = new Color(0.10f, 0.50f, 0.14f, 1f);
        var btn = btnGo.AddComponent<UnityEngine.UI.Button>();
        var btnColors = btn.colors;
        btnColors.highlightedColor = new Color(0.14f, 0.65f, 0.18f, 1f);
        btnColors.pressedColor    = new Color(0.07f, 0.36f, 0.10f, 1f);
        btn.colors = btnColors;
        SetAnchors(btnGo, new Vector2(0.52f, 0.05f), new Vector2(0.94f, 0.29f));
        AddBtnLabel(btnGo, "Satın Al", 19);

        // ── İptal butonu (sol) ──
        var iptalGo = new GameObject("Sen5IptalBtn");
        iptalGo.transform.SetParent(panelGo.transform, false);
        var iptalImg = iptalGo.AddComponent<UnityEngine.UI.Image>();
        iptalImg.color = new Color(0.22f, 0.22f, 0.30f, 1f);
        var iptalBtn = iptalGo.AddComponent<UnityEngine.UI.Button>();
        var iptalColors = iptalBtn.colors;
        iptalColors.highlightedColor = new Color(0.32f, 0.32f, 0.42f, 1f);
        iptalColors.pressedColor     = new Color(0.14f, 0.14f, 0.20f, 1f);
        iptalBtn.colors = iptalColors;
        SetAnchors(iptalGo, new Vector2(0.06f, 0.05f), new Vector2(0.48f, 0.29f));
        AddBtnLabel(iptalGo, "Hayır, teşekkürler", 16);

        _senaryo5PopupGo = canvasGo;
        bool satinAldi = false;
        bool iptalEtti = false;
        btn.onClick.AddListener(() => satinAldi = true);
        iptalBtn.onClick.AddListener(() => iptalEtti = true);

        yield return new WaitUntil(() => satinAldi || iptalEtti);

        UnityEngine.Object.Destroy(canvasGo);
        _senaryo5PopupGo = null;

        if (!satinAldi) yield break;

        // Tüm bakiye bonus satın alımına gitti; bonus cuzi ödeme moduyla başlar
        if (_ekonomiServisi != null)
            _ekonomiServisi.SetBakiye(0);
        _senaryo5BonusCuziLimitAktif = true;
        if (_donusServisi != null)
            _donusServisi.BaslatBonus();
    }
    private void SenaryoPresetUIHazirlaGerekirse()
    {
        if (_senaryoPresetUIHazir) return;

        if (_senaryoPresetDropdown == null)
            _senaryoPresetDropdown = GameObject.Find("SenaryoPresetDropdown")?.GetComponent<TMP_Dropdown>();

        if (_senaryoPresetDropdown != null)
        {
            _senaryoPresetDropdown.onValueChanged.RemoveListener(OnSenaryoPresetDropdownDegisti_Runtime);
            _senaryoPresetDropdown.ClearOptions();
            var ops = new List<string>(_adminSenaryoPresetleri.Length + 1);
            ops.Add("0. NORMAL OYUN");
            for (int i = 0; i < _adminSenaryoPresetleri.Length; i++)
                ops.Add(_adminSenaryoPresetleri[i].Ad);
            _senaryoPresetDropdown.AddOptions(ops);
            SenaryoDropdownYazilariniBuyut();
            _senaryoPresetDropdown.value = 0;
            _senaryoPresetDropdown.RefreshShownValue();
            _senaryoPresetDropdown.onValueChanged.AddListener(OnSenaryoPresetDropdownDegisti_Runtime);
            _senaryoPresetUIHazir = true;
        }

        SenaryoModuDurumLabeliniBulVeYaz();

        if (_senaryoPresetUIHazir)
            AdminNormalOyunUygula();
    }

    private void OnSenaryoPresetDropdownDegisti_Runtime(int index)
    {
        if (index == 0)
        {
            AdminNormalOyunUygula();
            return;
        }
        _senaryoPresetAktif = true;
        AdminSenaryoPresetUygula(index - 1);
    }

    /// <summary>SpinTestAraci için: tüm kritik servislerin (Awake/Start sonrası) başlatıldığını döner.
    /// Reflection'a gerek kalmadan WaitUntil ile bekleyebilmek için public.</summary>
    public bool TumServislerHazirMi()
    {
        return _ekonomiServisi != null
            && _odemeServisi != null
            && _carpanServisi != null
            && _izgaraServisi != null
            && _tumbleServisi != null
            && _donusAkisServisi != null;
    }

    /// <summary>SpinTestAraci için: bakiye/bahis manipülasyonu için public servis erişimi.</summary>
    public EkonomiServisi TestEkonomiServisi => _ekonomiServisi;

    /// <summary>SpinTestAraci için: scatter index'i + state field okuma için public erişim.</summary>
    public int TestScatterIndex => _scatterIndexCache;
    public bool TestKacisFrenlemeAktif => _kacisFrenlemeBuSpinAktif;
    public int TestArdisikKayipSayac => _ardisikKayipSayac;

    /// <summary>SpinTestAraci için: senaryo dropdown index'iyle senaryo aktive eder. 0 = Normal Oyun, 1-5 = Senaryo 1-5.</summary>
    public void TestSenaryoSec(int dropdownIndex0Bazli)
    {
        if (dropdownIndex0Bazli <= 0)
        {
            AdminNormalOyunUygula();
            return;
        }
        _senaryoPresetAktif = true;
        AdminSenaryoPresetUygula(dropdownIndex0Bazli - 1);
    }

    /// <summary>SpinTestAraci için senkron spin simülasyonu. Animasyon ÇAĞRILMAZ — sadece veri akışı.
    /// Dönen kayıttan ham kazanç, çarpan, tumble adımları, force carpan kullanımı okunur.</summary>
    public SpinSimulasyonKaydi TestSpinSimuleEt(bool bonusSpin = false)
    {
        int odenebilir = _odemeServisi != null ? _odemeServisi.GetSpinOdenebilirLimit() : int.MaxValue;
        return SimuleEtVeKaydetImpl(odenebilir, bonusSpin);
    }

    /// <summary>SpinTestAraci için: bonus oyununun bir bonus turunu simüle eder; toplam ödemeyi döner.
    /// Gerçek bonus akışını kısa devre eder; her tur için SimuleEtVeKaydetImpl(bonusSpin=true) çağırır.</summary>
    public int TestBonusOyunSimuleEt(int bonusHak = 10)
    {
        int toplam = 0;
        for (int i = 0; i < bonusHak; i++)
        {
            var kayit = SimuleEtVeKaydetImpl(int.MaxValue, true);
            if (kayit == null) continue;
            int ham = kayit.ToplamHamKazanc;
            int carpan = Mathf.Max(1, kayit.NihaiCarpanToplam);
            toplam += ham * carpan;
        }
        return toplam;
    }

    private void AdminSenaryoPresetUygula(int index)
    {
        if (_adminSenaryoPresetleri == null || _adminSenaryoPresetleri.Length == 0) return;
        index = Mathf.Clamp(index, 0, _adminSenaryoPresetleri.Length - 1);
        _aktifAdminSenaryoIndex = index;
        var p = _adminSenaryoPresetleri[index];

        if (scatterSliderUI != null)
            scatterSliderUI.SetValueWithoutNotify(Mathf.Clamp(p.ScatterYuzde, scatterSliderUI.minValue, scatterSliderUI.maxValue));
        OnScatterSliderChanged(p.ScatterYuzde);

        if (carpanOlasilikSlider != null)
            carpanOlasilikSlider.SetValueWithoutNotify(Mathf.Clamp(p.CarpanYuzde, carpanOlasilikSlider.minValue, carpanOlasilikSlider.maxValue));
        OnCarpanOlasilikSliderChanged(p.CarpanYuzde);

        if (carpanMaxAdetSlider != null)
            carpanMaxAdetSlider.SetValueWithoutNotify(Mathf.Clamp(p.MaxCarpanAdedi, carpanMaxAdetSlider.minValue, carpanMaxAdetSlider.maxValue));
        OnCarpanMaxAdetSliderChanged(p.MaxCarpanAdedi);

        AdminBahisAyarla(p.Bahis);
        AdminMaxScatterPerSpinAyarla(p.MaxScatterPerSpin);

        bool forcePopupGoster = false;
        string forceMesaji = null;
        if (index == 3)
        {
            forcePopupGoster = true;
            forceMesaji = "ZORLA 100X AKTİF";
        }
        else if (index == 4)
        {
            forcePopupGoster = true;
            forceMesaji = "ZORLA 500X AKTİF";
        }
        AdminZorlaCarpanSec(p.ZorlaCarpan, forcePopupGoster, forceMesaji);
        AdminSenaryoPresetOdemeModeliniUygula(p);
        // Senaryo/bahis/ödeme bandı değişti; arka planda eski kuralla hesaplanmış spin kullanılmasın.
        OncedenHesaplananSpinOnbelleginiTemizle();
        SpinPolitikasiniYenile();
    }

    private void AdminSenaryoPresetOdemeModeliniUygula(AdminSenaryoPreset p)
    {
        var girdi = new SenaryoOdemeModelServisi.Girdi
        {
            Bahis = p.Bahis,
            ScatterYuzde = p.ScatterYuzde,
            CarpanYuzde = p.CarpanYuzde,
            MaxCarpanAdedi = p.MaxCarpanAdedi,
            ZorlaCarpan = p.ZorlaCarpan,
            MaxScatterPerSpin = p.MaxScatterPerSpin
        };
        var hedef = _senaryoOdemeModelServisi.Hesapla(girdi);

        int oncekiEgilim = _odemeEgilimiYuzde;
        int oncekiDagilim = _odemeDagilimiYuzde;
        int oncekiMin = _minOdemeTL;
        int oncekiMax = _maxOdemeTL;

        _odemeEgilimiYuzde = Mathf.Clamp(hedef.OdemeEgilimiYuzde, 0, 100);
        _odemeDagilimiYuzde = Mathf.Clamp(hedef.OdemeDagilimiYuzde, 0, 100);
        _minOdemeTL = Mathf.Max(0, hedef.MinOdemeTL);
        _maxOdemeTL = Mathf.Max(_minOdemeTL, hedef.MaxOdemeTL);

        // 1. Senaryo: 5 kazanç / 0 kayıp; net kar bandı bahisin 3–4 katı → nihai ödeme = bahis + net = 4×..5× bahis
        if (_aktifAdminSenaryoIndex == 0)
        {
            _ustUsteKazancHedef = 5;
            _ustUsteKayipHedef = 0;
            int b = Mathf.Max(1, p.Bahis);
            _minOdemeTL = b * 4;
            _maxOdemeTL = b * 5;
            _odemeEgilimiYuzde = 100;
            _senaryo1SonZorunluNihaiOdeme = -1;
            UstUsteDonguAyarlariniYenile(true);
        }
        else if (_aktifAdminSenaryoIndex == 1)
        {
            _ustUsteKazancHedef = 3;
            _ustUsteKayipHedef = 2;
            // Kazanç bant: bahis×3..bahis×8 (net 2x-7x) — geniş tutarak dağılım çeşitliliği
            int b2 = Mathf.Max(1, p.Bahis);
            _minOdemeTL = b2 * 3;
            _maxOdemeTL = b2 * 8;
            _odemeEgilimiYuzde = 60;
            _odemeDagilimiYuzde = 70;
            _senaryo2DonguIndex = 0;
            _senaryo2SonNetKazanc = -1;
            _senaryo2SonNetKayip = -1;
            UstUsteDonguAyarlariniYenile(true);
        }
        else if (_aktifAdminSenaryoIndex == 2)
        {
            _ustUsteKazancHedef = 1;
            _ustUsteKayipHedef = 1;
            // Kayıp fazı: ödeme 0..bahis (net -bahis..0). Kazanç: bahis+100..bahis+200 (net +100..+200). Inspector tavanı kazanç için yeterli olsun.
            _minOdemeTL = p.Bahis + 100;
            _maxOdemeTL = p.Bahis + 600;
            _odemeEgilimiYuzde = 35;
            _odemeDagilimiYuzde = 90;
            _senaryo3DonguIndex = 0;
            _senaryo3SonNetKazanc = -1;
            _senaryo3SonNetKayip = -1;
            // İstisna: bu aşamada döngü kayıp fazından başlar (K-KY-K-KY-K).
            UstUsteDonguAyarlariniYenile(false);
        }
        else if (_aktifAdminSenaryoIndex == 3)
        {
            // S4: KY→K→BOMB (3-spin döngüsü). Band = bahis*2..bahis*5 (kazanç spin için; bomb ayrıca yönetilir).
            int b4 = Mathf.Max(1, p.Bahis);
            _minOdemeTL = b4 * 2;
            _maxOdemeTL = b4 * 5;
            _odemeEgilimiYuzde = 55;
            _odemeDagilimiYuzde = 80;
            _senaryo4DonguIndex = 0;
            _senaryo4SonZorunluNihaiOdeme = -1;
            UstUsteDonguAyarlariniYenile(true);
        }
        else if (_aktifAdminSenaryoIndex == 4)
        {
            // S5: K→KY→BOMB (3-spin döngüsü). Band = bahis*2..bahis*5.
            int b5 = Mathf.Max(1, p.Bahis);
            _minOdemeTL = b5 * 2;
            _maxOdemeTL = b5 * 5;
            _odemeEgilimiYuzde = 55;
            _odemeDagilimiYuzde = 80;
            _senaryo5DonguIndex = 0;
            _senaryo5SonZorunluNihaiOdeme = -1;
            _senaryo5BombSonrasiPopupBekliyor = false;
            _senaryo5BonusCuziLimitAktif = false;
            UstUsteDonguAyarlariniYenile(true);
        }

        if (odemeEgilimiSliderUI != null)
            odemeEgilimiSliderUI.SetValueWithoutNotify(_odemeEgilimiYuzde);
        if (odemeDagilimiSliderUI != null)
            odemeDagilimiSliderUI.SetValueWithoutNotify(_odemeDagilimiYuzde);
        if (minOdemeInput != null)
            minOdemeInput.SetTextWithoutNotify(_minOdemeTL.ToString());
        if (maxOdemeInput != null)
            maxOdemeInput.SetTextWithoutNotify(_maxOdemeTL.ToString());
        if (ustUsteKazancInput != null)
            ustUsteKazancInput.SetTextWithoutNotify(_ustUsteKazancHedef.ToString());
        if (ustUsteKayipInput != null)
            ustUsteKayipInput.SetTextWithoutNotify(_ustUsteKayipHedef.ToString());

        // Input alanlarından tekrar okuma: preset az önce bellek + SetTextWithoutNotify ile set etti; okuma bazen eski UI/bağlantı yüzünden min-max'ı eziyordu.
        AdminOdemeAyarlariOkuVeUygula(false, false);

        Debug.Log($"[ADMIN][SENARYO_ODEME] {p.Ad} -> Egilim=%{_odemeEgilimiYuzde} Dagilim=%{_odemeDagilimiYuzde} Min={_minOdemeTL} Max={_maxOdemeTL}");
        AdminPaytableOzetiLogla(_ekonomiServisi != null ? _ekonomiServisi.Bahis : p.Bahis);
        OdemeEgilimVeDagilimSliderKilidiniUygula();
    }
    private void AdminPaytableOzetiLogla(int bahis)
    {
        if (tumbleAyarlari == null || bahis <= 0) return;
        tumbleAyarlari.EnsurePayTablesInitialized(
            (sembolSpriteListesi != null && sembolSpriteListesi.Count > 0) ? sembolSpriteListesi.Count : 9);
        int sc = Mathf.Clamp(tumbleAyarlari.ScatterIndex, 0, int.MaxValue);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"[ADMIN][PAYTABLE_OZET] Bahis={bahis} TL | TUMBLE_ESIK=8 (tek sembol kümesi, çarpan yok) | ScatterIndex={sc}");
        int n = tumbleAyarlari.PayTable_8_9 != null ? tumbleAyarlari.PayTable_8_9.Length : 0;
        for (int i = 0; i < n; i++)
        {
            float a = tumbleAyarlari.PayTable_8_9[i];
            float b = tumbleAyarlari.PayTable_10_11 != null && i < tumbleAyarlari.PayTable_10_11.Length ? tumbleAyarlari.PayTable_10_11[i] : 0f;
            float c = tumbleAyarlari.PayTable_12Plus != null && i < tumbleAyarlari.PayTable_12Plus.Length ? tumbleAyarlari.PayTable_12Plus[i] : 0f;
            int tl8 = Mathf.RoundToInt(a * bahis);
            int tl10 = Mathf.RoundToInt(b * bahis);
            int tl12 = Mathf.RoundToInt(c * bahis);
            string etik = (i == sc) ? " (SCATTER ödeme 0)" : string.Empty;
            sb.AppendLine($"  İndex {i}{etik}: 8–9 küme → {tl8} TL | 10–11 → {tl10} TL | 12+ → {tl12} TL");
        }
        sb.Append("  Not: Aynı turda birden çok sembol kümesi patlarsa toplam = satırlar toplamı. Düşük zorlukta 6–7 hücre yarım tablo ile ödenir (TumbleAyarlari.CalculateWinWithOwnPayTable).");
        Debug.Log(sb.ToString());
    }

    private void SenaryoModuDurumLabeliniBulVeYaz() { }
    public void AdminNormalOyunUygula()
    {
        _senaryoPresetAktif = false;
        _aktifAdminSenaryoIndex = -1;

        AdminOdemeUIRefsiniBulGerekirse();
        _odemeEgilimiYuzde = 65;
        _odemeDagilimiYuzde = 30;
        _minOdemeTL = 0;
        _maxOdemeTL = 0;
        _ustUsteKazancHedef = 0;
        _ustUsteKayipHedef = 0;
        UstUsteDonguAyarlariniYenile(true);

        if (odemeEgilimiSliderUI != null)
            odemeEgilimiSliderUI.SetValueWithoutNotify(Mathf.Clamp(_odemeEgilimiYuzde, 0f, 100f));
        if (odemeDagilimiSliderUI != null)
            odemeDagilimiSliderUI.SetValueWithoutNotify(Mathf.Clamp(_odemeDagilimiYuzde, 0f, 100f));
        if (minOdemeInput != null) minOdemeInput.SetTextWithoutNotify(_minOdemeTL.ToString());
        if (maxOdemeInput != null) maxOdemeInput.SetTextWithoutNotify(_maxOdemeTL.ToString());
        if (ustUsteKazancInput != null) ustUsteKazancInput.SetTextWithoutNotify(_ustUsteKazancHedef.ToString());
        if (ustUsteKayipInput != null) ustUsteKayipInput.SetTextWithoutNotify(_ustUsteKayipHedef.ToString());
        if (odemeEgilimiText != null) odemeEgilimiText.text = $"1) Ödeme Eğilimi %{_odemeEgilimiYuzde}";
        if (odemeDagilimiText != null) odemeDagilimiText.text = $"6) Ödeme Dağılımı %{_odemeDagilimiYuzde}";

        AdminOdemeAyarlariOkuVeUygula(false, false);
        AdminZorlaCarpanSec(0, false, null);
        SpinPolitikasiniYenile();
        OdemeEgilimVeDagilimSliderKilidiniUygula();
        OncedenHesaplananSpinOnbelleginiTemizle();
        SenaryoModuDurumLabeliniBulVeYaz();

        Debug.Log("[ADMIN] Normal Oyun modu aktif: Senaryo 1-5 kapalı | Eğilim=%65 | Dağılım=%30 | Min/Max=0");
    }

    /// <summary>PanelKopru: kazanma oranını (0-100) doğrudan set eder.</summary>
    public void AdminSetOdemeEgilimi(int yuzde)
    {
        Debug.Log($"[Admin] AdminSetOdemeEgilimi CAGRILDI: yuzde={yuzde} | onceki={_odemeEgilimiYuzde}");
        _odemeEgilimiYuzde = Mathf.Clamp(yuzde, 0, 100);
        if (odemeEgilimiSliderUI != null)
            odemeEgilimiSliderUI.SetValueWithoutNotify(_odemeEgilimiYuzde);
        if (odemeEgilimiText != null)
            odemeEgilimiText.text = $"1) Ödeme Eğilimi %{_odemeEgilimiYuzde}";
        SpinPolitikasiniYenile();
        OncedenHesaplananSpinOnbelleginiTemizle();
        Debug.Log($"[ADMIN][PANEL] OdemeEgilimi = %{_odemeEgilimiYuzde}");
    }

    /// <summary>PanelKopru: zorla çarpan sonrası tumble zinciri devam etsin mi?</summary>
    public void AdminSetCarpanTumbleAktif(bool aktif)
    {
        _carpanTumbleAktif = aktif;
        Debug.Log($"[ADMIN] CarpanTumbleAktif = {aktif}");
    }

    public void AdminSetMinOdeme(int tl)
    {
        _minOdemeTL = Mathf.Max(0, tl);
        if (_maxOdemeTL > 0 && _maxOdemeTL < _minOdemeTL) _maxOdemeTL = _minOdemeTL;
        if (minOdemeInput != null)
            minOdemeInput.SetTextWithoutNotify(_minOdemeTL.ToString());
        OncedenHesaplananSpinOnbelleginiTemizle();
        Debug.Log($"[ADMIN][PANEL] MinOdemeTL = {_minOdemeTL}");
    }

    public void AdminSetMinOdemeCarpan(float carpan)
    {
        _minOdemeCarpan = Mathf.Max(0f, carpan);
        OncedenHesaplananSpinOnbelleginiTemizle();
        Debug.Log($"[ADMIN][PANEL] MinOdemeCarpan = {_minOdemeCarpan}x");
    }

    /// <summary>NO-OP (2026-04-29): Maks ödeme tavanı kaldırıldı; bu setter artık panel state için tutulur ama
    /// ödeme akışında okunmuyor. _maksOdemeCarpan her zaman 0 kalır → DonusAkisServisi clamp koşulu false.</summary>
    public void AdminSetMaksOdemeCarpan(float carpan)
    {
        // Ödeme tavanı kaldırıldı — değer ne gelirse gelsin 0 (etkisiz) tut.
        _maksOdemeCarpan = 0f;
        if (carpan > 0f)
            Debug.LogWarning($"[ADMIN][PANEL] AdminSetMaksOdemeCarpan({carpan}) ETKİSİZ — ödeme tavanı kalkıktan beri okunmuyor.");
        OncedenHesaplananSpinOnbelleginiTemizle();
        Debug.Log($"[ADMIN][PANEL] MaksOdemeCarpan = {_maksOdemeCarpan}x");
    }

    public void AdminSetMaxOdeme(int tl)
    {
        Debug.Log($"[Admin] AdminSetMaxOdeme CAGRILDI: tl={tl} | onceki={_maxOdemeTL}");
        _maxOdemeTL = Mathf.Max(0, tl);
        if (_maxOdemeTL < _minOdemeTL) _maxOdemeTL = _minOdemeTL;
        if (maxOdemeInput != null)
            maxOdemeInput.SetTextWithoutNotify(_maxOdemeTL.ToString());
        OncedenHesaplananSpinOnbelleginiTemizle();
        Debug.Log($"[ADMIN][PANEL] MaxOdemeTL = {_maxOdemeTL}");
    }

    public void AdminSetArdisikKayipLimiti(int limit)
    {
        _ardisikKayipLimiti = Mathf.Max(1, limit);
        _ardisikKayipSayac = 0;
        Debug.Log($"[ADMIN][PANEL] ArdisikKayipLimiti = {_ardisikKayipLimiti}");
    }

    private Coroutine _yeniOyuncuKoroutin;
    private int _yeniOyuncuOncekiEgilim = 65;
    private int _yeniOyuncuOncekiMax = 0;

    [HideInInspector] public int bonusOtomatikSpinPeriyodu = 0; // 0 = devre dışı
    public void AdminSetBonusOtomatikSpinPeriyodu(int oran)
    {
        bonusOtomatikSpinPeriyodu = Mathf.Max(0, oran);
        // Periyot değişince sayaç sıfırlansın (anında yeni rejime geç)
        _bonusOtomatikSpinSayaci = 0;
        Debug.Log("[ADMIN][PANEL] Bonus otomatik spin periyodu = " + bonusOtomatikSpinPeriyodu + " (0 = kapalı)");
    }

    // DonusAkisServisi spin sonu güncellemesinde kullanılan sayaç ve flag.
    [HideInInspector] public int _bonusOtomatikSpinSayaci = 0;
    [HideInInspector] public bool _bonusOtomatikTetikSonrakiSpin = false;

    [HideInInspector] public int carpanSahteOraniYuzde = 0;
    public void AdminSetCarpanSahteOrani(int yuzde)
    {
        carpanSahteOraniYuzde = Mathf.Clamp(yuzde, 0, 100);
        Debug.Log($"[ADMIN][PANEL] Çarpan sahte gösterimi: {carpanSahteOraniYuzde}%");
    }

    [HideInInspector] public int carpanOlasilikYuzde = 2;
    public void AdminSetCarpanOlasilik(int yuzde)
    {
        carpanOlasilikYuzde = Mathf.Clamp(yuzde, 0, 100);
        Debug.Log($"[ADMIN][PANEL] Çarpan düşme olasılığı: {carpanOlasilikYuzde}%");
    }

    [HideInInspector] public int maxCarpanTekSpinSayisi = 3;
    public void AdminSetMaxCarpanTekSpin(int max)
    {
        maxCarpanTekSpinSayisi = Mathf.Clamp(max, 1, 10);
        // CarpanServisi gerçek field 'maxCarpanAdedi' okuyor; değişikliği oraya da yansıt.
        maxCarpanAdedi = maxCarpanTekSpinSayisi;
        Debug.Log($"[ADMIN][PANEL] Tek spinde max çarpan: {maxCarpanTekSpinSayisi} (maxCarpanAdedi={maxCarpanAdedi})");
    }

    // ────────────────────────────────────────────────────────────
    // YAKIN KAÇIRMA (Near-Miss) — 10'da N formatında, 0 = kapalı
    // ────────────────────────────────────────────────────────────
    [HideInInspector] public int yakinKacirmaDegeri10da = 0;
    public void AdminSetYakinKacirma(int deger10da)
    {
        yakinKacirmaDegeri10da = Mathf.Clamp(deger10da, 0, 10);
        Debug.Log($"[ADMIN][PANEL] Yakın Kaçırma = 10'da {yakinKacirmaDegeri10da}");
    }

    // ────────────────────────────────────────────────────────────
    // MANUEL BONUS TETİKLEME (test ve panel butonu için)
    // ────────────────────────────────────────────────────────────
    public void AdminManuelBonusBaslat()
    {
        if (bonusAktif)
        {
            Debug.LogWarning("[ADMIN][PANEL] Bonus zaten aktif; manuel tetikleme atlandı.");
            return;
        }
        Debug.Log("[ADMIN][PANEL] Manuel bonus tetikleme başlatıldı.");
        BaslatBonus();
    }

    public void AdminSetYeniOyuncuModu(bool aktif)
    {
        if (aktif)
        {
            _yeniOyuncuModuAktif = true;
            _yeniOyuncuBaslangicZamani = Time.time;
            _yeniOyuncuOncekiEgilim = _odemeEgilimiYuzde;
            _yeniOyuncuOncekiMax = _maxOdemeTL;
            AdminSetOdemeEgilimi(85);
            AdminSetMaxOdeme(1000);
            if (_yeniOyuncuKoroutin != null) StopCoroutine(_yeniOyuncuKoroutin);
            _yeniOyuncuKoroutin = StartCoroutine(YeniOyuncuModuSureKontrol());
            Debug.Log("[ADMIN][PANEL] Yeni oyuncu modu AKTİF — 30 dk cömert mod başladı.");
        }
        else
        {
            _yeniOyuncuModuAktif = false;
            if (_yeniOyuncuKoroutin != null) { StopCoroutine(_yeniOyuncuKoroutin); _yeniOyuncuKoroutin = null; }
            AdminSetOdemeEgilimi(_yeniOyuncuOncekiEgilim);
            AdminSetMaxOdeme(_yeniOyuncuOncekiMax);
            Debug.Log("[ADMIN][PANEL] Yeni oyuncu modu kapatıldı, önceki ayarlar geri yüklendi.");
        }
    }

    private System.Collections.IEnumerator YeniOyuncuModuSureKontrol()
    {
        yield return new WaitForSeconds(1800f);
        _yeniOyuncuModuAktif = false;
        _yeniOyuncuKoroutin = null;
        AdminSetOdemeEgilimi(_yeniOyuncuOncekiEgilim);
        AdminSetMaxOdeme(_yeniOyuncuOncekiMax);
        Debug.Log("[ADMIN] Yeni oyuncu modu 30 dakika doldu, otomatik sonlandı.");
    }

    public bool AdminBahisAyarla(int hedefBahis)
    {
        if (_ekonomiServisi == null) return false;
        int onceki = _ekonomiServisi.Bahis;
        _ekonomiServisi.SetBahis(hedefBahis);
        int yeni = _ekonomiServisi.Bahis;
        _uiServisi?.UI_Guncelle();
        SenaryoYoneticisi.I?.UI_Guncelle();
        Debug.Log($"[ADMIN-SENARYO] Bahis ayarlandı: {onceki} -> {yeni} (hedef={hedefBahis})");
        return yeni != onceki;
    }
    public bool AdminMaxScatterPerSpinAyarla(int hedef)
    {
        int onceki = maxScatterPerSpin;
        int min = Mathf.Max(1, scatterEsik > 0 ? 1 : 1);
        maxScatterPerSpin = Mathf.Clamp(hedef, min, 5);
        Debug.Log($"[ADMIN-SENARYO] MaxScatterPerSpin ayarlandı: {onceki} -> {maxScatterPerSpin}");
        return onceki != maxScatterPerSpin;
    }
    private void SpinPolitikasiniYenile()
    {
        _spinPolitikasi = SenaryoSpinPolitikasiFabrikasi.Olustur(_senaryoPresetAktif, _aktifAdminSenaryoIndex);
    }
    private ISenaryoSpinPolitikasi SpinPolitikasiniAl()
    {
        if (_spinPolitikasi == null)
            SpinPolitikasiniYenile();
        return _spinPolitikasi;
    }
    private void ZorlaCarpanIlkDususEfektiniBaslat(SpinSimulasyonKaydi kayit)
    {
        if (!zorlaCarpanIlkDususSokEfektiAktif || kayit == null)
            return;
        if (_carpanSokEfektServisi == null)
            return;

        int carpanDegeri = ZorlaCarpanDegeriniBul(kayit);
        if (kayit.ZorlaCarpanKullanildi && carpanDegeri <= 0)
            carpanDegeri = 100; // Kayıtta değer yakalanamazsa bile zorla çarpan spininde efekti garanti et.
        if (carpanDegeri <= 0)
            return;

        RectTransform sarsintiHedefi = null;
        if (kazancText != null && kazancText.canvas != null)
            sarsintiHedefi = kazancText.canvas.rootCanvas.transform as RectTransform;
        if (sarsintiHedefi == null && hucreler != null && hucreler.Length > 0 && hucreler[0] != null && hucreler[0].canvas != null)
            sarsintiHedefi = hucreler[0].canvas.rootCanvas.transform as RectTransform;
        if (sarsintiHedefi == null)
        {
            Debug.LogWarning("[SOK-EFEKT] İlk düşüş sarsıntı hedefi bulunamadı; kazanç text canvas referansını kontrol et.");
            return;
        }

        AudioSource kaynak = tumbleSfxSource != null ? tumbleSfxSource : bonusEndSfxSource;
        if (zorlaCarpanIlkDususSokClip == null)
            Debug.LogWarning("[SOK-EFEKT] Zorla çarpan ilk düşüş ses klibi atanmadı; yalnızca sarsıntı oynatılacak.");

        _carpanSokEfektServisi.BaslatIlkDususSokEfekti(
            this,
            sarsintiHedefi,
            carpanDegeri,
            kaynak,
            zorlaCarpanIlkDususSokClip,
            zorlaCarpanIlkDususSokSesSeviyesi);
        _bombEfektServisi?.BombEfektBaslat(this, carpanDegeri);
        Debug.Log($"[SOK-EFEKT] İlk düşüş sarsıntısı tetiklendi. x{carpanDegeri} | Zorla={kayit.ZorlaCarpanKullanildi}");
    }

    private int ZorlaCarpanDegeriniBul(SpinSimulasyonKaydi kayit)
    {
        if (kayit == null || !kayit.ZorlaCarpanKullanildi)
            return 0;

        int enYuksek = 0;
        if (kayit.IlkCarpanDegerleri != null)
        {
            for (int i = 0; i < kayit.IlkCarpanDegerleri.Count; i++)
                enYuksek = Mathf.Max(enYuksek, kayit.IlkCarpanDegerleri[i]);
        }

        if (enYuksek <= 0 && kayit.IlkCarpanGrid != null)
        {
            int xmax = Mathf.Min(sutun, kayit.Sutun);
            int ymax = Mathf.Min(satir, kayit.Satir);
            for (int x = 0; x < xmax; x++)
                for (int y = 0; y < ymax; y++)
                    enYuksek = Mathf.Max(enYuksek, kayit.IlkCarpanGrid[x, y]);
        }

        if (enYuksek <= 0 && kayit.Adimlar != null)
        {
            for (int a = 0; a < kayit.Adimlar.Count; a++)
            {
                var adim = kayit.Adimlar[a];
                if (adim?.CarpanDegerleriBuTur == null) continue;
                for (int i = 0; i < adim.CarpanDegerleriBuTur.Count; i++)
                    enYuksek = Mathf.Max(enYuksek, adim.CarpanDegerleriBuTur[i]);
            }
        }

        return enYuksek;
    }
}
