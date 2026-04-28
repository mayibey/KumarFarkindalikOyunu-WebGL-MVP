using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class AdminPanel : MonoBehaviour
{
    [Header("Admin Şifresi")]
    public string adminSifre = "1234";
    
    [Header("UI Paneller")]
    public GameObject sifrePanel;
    public GameObject adminAnaPanel;
    public TMP_InputField sifreInput;
    public TMP_Text uyariText;
    public Button adminPanelAcButonu;

    [Header("Zorluk Slider")]
    public Slider zorlukSlider;
    public TMP_Text zorlukDegerText;

    [Header("Scatter Slider")]
    public Slider scatterSlider;
    public TMP_Text scatterDegerText;
    public TMP_Text scatterDusmeOraniText; // Yeni: "Scatter Düşme %15" gibi yazacak

    [Header("Çarpan Ayarları")]
    public Slider carpanOlasilikSlider;
    public TMP_Text carpanOlasilikText;
    public Slider carpanMaxAdetSlider;
    public TMP_Text carpanMaxAdetText;

    [Header("Senaryo Preset")]
    [Tooltip("1-5 senaryo preset seçimi. Seçim değiştiğinde admin ayarları otomatik senkronize edilir.")]
    public TMP_Dropdown senaryoPresetDropdown;
    [Tooltip("Legacy UI Dropdown kullanıyorsanız buraya atanır (TMP yerine).")]
    public Dropdown senaryoPresetDropdownLegacy;

    [Header("Zorla Çarpan Butonları")]
    public Button zorlaCarpan2Button;
    public Button zorlaCarpan5Button;
    public Button zorlaCarpan10Button;
    public Button zorlaCarpan50Button;
    public Button zorlaCarpan100Button;
    public Button zorlaCarpanSifirlaButton;
    [Tooltip("Opsiyonel: ForceX seçimi sonrası 'Sonraki spin x.. hazır' bilgisini gösterir.")]
    public TMP_Text zorlaCarpanDurumText;

    // OyunYoneticisi referansı
    private OyunYoneticisi _oyunYoneticisi;
    private Button _sifirlaButonu;

    private bool _adminAcik = false;
    private bool _adminIslemKilidiAktif = false;
    private CanvasGroup _adminCanvasGroup;
    private Coroutine _senaryoAnimCoroutine;
    private bool _senaryoPresetAktif = true;
    private bool _senaryoPresetHazirlandi = false;

    private struct SenaryoPreset
    {
        public string Ad;
        public int Bahis;
        public int ScatterYuzde;
        public int CarpanYuzde;
        public int MaxCarpanAdedi;
        public int ZorlaCarpan;
        public int MaxScatterPerSpin;
    }

    private static readonly SenaryoPreset[] _senaryoPresetleri = new SenaryoPreset[]
    {
        // Kullanıcı kararı: preset seçimi zorluk değerine dokunmaz.
        // 5. senaryoya kadar 4/5 scatter görünmesin diye MaxScatterPerSpin=2 uygulanır.
        new SenaryoPreset { Ad = "1.ALIŞTIRMA", Bahis = 300, ScatterYuzde = 16, CarpanYuzde = 22, MaxCarpanAdedi = 2, ZorlaCarpan = 0, MaxScatterPerSpin = 2 },
        new SenaryoPreset { Ad = "2.BİRAZ KAZANDIRALIM", Bahis = 300, ScatterYuzde = 14, CarpanYuzde = 20, MaxCarpanAdedi = 2, ZorlaCarpan = 0, MaxScatterPerSpin = 2 },
        new SenaryoPreset { Ad = "3.BİRAZ KAYBETTİRELİM", Bahis = 1000, ScatterYuzde = 10, CarpanYuzde = 24, MaxCarpanAdedi = 3, ZorlaCarpan = 0, MaxScatterPerSpin = 2 },
        new SenaryoPreset { Ad = "4.AZ KAZANDIRALIM ÇOK KAYBETTİRELİM", Bahis = 100, ScatterYuzde = 18, CarpanYuzde = 26, MaxCarpanAdedi = 3, ZorlaCarpan = 0, MaxScatterPerSpin = 2 },
        new SenaryoPreset { Ad = "5.BÜYÜK TEKLİFLERLE PARASINI ALALIM", Bahis = 200, ScatterYuzde = 9, CarpanYuzde = 30, MaxCarpanAdedi = 4, ZorlaCarpan = 0, MaxScatterPerSpin = 5 }
    };

    void Start()
    {
        // OyunYoneticisi referansını bul
        _oyunYoneticisi = FindObjectOfType<OyunYoneticisi>();

        if (sifrePanel) sifrePanel.SetActive(false);
        if (adminAnaPanel) adminAnaPanel.SetActive(false);
        
        if (adminPanelAcButonu)
            adminPanelAcButonu.onClick.AddListener(SifrePaneliGoster);

        if (sifreInput)
            sifreInput.onSubmit.AddListener(SifreKontrolEt);

        // OTOMATİK BUTON BULMA VE BAĞLAMA
        OtomatikButonlariBulVeBagla();

        // Slider'ları başlat
        InitializeSliders();

        // Zorla Çarpan butonlarını bağla
        ZorlaCarpanButonlariniBagla();
        ZorlaCarpanDurumTextHazirla();
        SenaryoPresetDropdownHazirla();

        if (adminAnaPanel != null)
        {
            _adminCanvasGroup = adminAnaPanel.GetComponent<CanvasGroup>();
            if (_adminCanvasGroup == null)
                _adminCanvasGroup = adminAnaPanel.AddComponent<CanvasGroup>();
        }
    }

    private void ZorlaCarpanDurumTextHazirla()
    {
        if (zorlaCarpanDurumText != null) return;
        if (adminAnaPanel == null) return;

        var bulunan = adminAnaPanel.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < bulunan.Length; i++)
        {
            var t = bulunan[i];
            if (t == null || t.gameObject == null) continue;
            string ad = (t.gameObject.name ?? "").ToLowerInvariant();
            if (ad.Contains("force") && ad.Contains("durum"))
            {
                zorlaCarpanDurumText = t;
                return;
            }
        }

        GameObject go = new GameObject("ForceDurumText");
        go.transform.SetParent(adminAnaPanel.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -14f);
        rt.sizeDelta = new Vector2(460f, 34f);

        var text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = 24;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.86f, 0.90f, 0.96f, 1f);
        text.raycastTarget = false;
        text.text = "ForceX beklemiyor";
        zorlaCarpanDurumText = text;
    }

    void Update()
    {
        if (!_senaryoPresetHazirlandi)
        {
            SenaryoPresetDropdownHazirla();
            _senaryoPresetHazirlandi = senaryoPresetDropdown != null || senaryoPresetDropdownLegacy != null;
        }
        AdminIslemKilidiniGuncelle();
        ZorlaCarpanDurumMetniniGuncelle();
    }

    private void ZorlaCarpanDurumMetniniGuncelle()
    {
        if (_oyunYoneticisi != null)
            ZorlaCarpanButonlarinSenaryo1GoreGuncelle(_oyunYoneticisi.IsSenaryo1Aktif());

        if (zorlaCarpanDurumText == null) return;
        if (_oyunYoneticisi == null)
        {
            zorlaCarpanDurumText.text = "";
            return;
        }

        int bekleyen = Mathf.Max(0, _oyunYoneticisi.zorlaSiradakiCarpan);
        if (bekleyen > 0)
        {
            zorlaCarpanDurumText.text = $"Sonraki spin: x{bekleyen} hazır";
            zorlaCarpanDurumText.color = new Color(0.55f, 0.95f, 0.62f, 1f);
        }
        else
        {
            zorlaCarpanDurumText.text = "ForceX beklemiyor";
            zorlaCarpanDurumText.color = new Color(0.86f, 0.90f, 0.96f, 1f);
        }
    }

    private bool OyunSirasindaAdminKilitlenmeli()
    {
        // İstek: oyun sırasında admin paneli de kilitlenmesin.
        return false;
    }

    private void AdminIslemKilidiniGuncelle()
    {
        bool kilit = OyunSirasindaAdminKilitlenmeli();
        if (kilit == _adminIslemKilidiAktif) return;
        _adminIslemKilidiAktif = kilit;

        if (adminAnaPanel == null || !adminAnaPanel.activeInHierarchy) return;
        if (_adminCanvasGroup == null) return;

        _adminCanvasGroup.interactable = !kilit;
        _adminCanvasGroup.blocksRaycasts = !kilit;
        _adminCanvasGroup.alpha = kilit ? 0.7f : 1f;

        // CanvasGroup ataması eksik olsa bile kritik kontroller fiziksel olarak kilitlensin.
        if (zorlukSlider != null) zorlukSlider.interactable = !kilit;
        if (scatterSlider != null) scatterSlider.interactable = !kilit;
        if (carpanOlasilikSlider != null) carpanOlasilikSlider.interactable = !kilit;
        if (carpanMaxAdetSlider != null) carpanMaxAdetSlider.interactable = !kilit;
        if (zorlaCarpan2Button != null) zorlaCarpan2Button.interactable = !kilit;
        if (zorlaCarpan5Button != null) zorlaCarpan5Button.interactable = !kilit;
        if (zorlaCarpan10Button != null) zorlaCarpan10Button.interactable = !kilit;
        if (zorlaCarpan50Button != null) zorlaCarpan50Button.interactable = !kilit;
        if (zorlaCarpan100Button != null) zorlaCarpan100Button.interactable = !kilit;
        if (zorlaCarpanSifirlaButton != null) zorlaCarpanSifirlaButton.interactable = !kilit;

        if (uyariText != null)
            uyariText.text = kilit ? "Spin/bonus sırasında yönetici işlemleri kilitli." : "";
    }

    private void ZorlaCarpanButonlariniBagla()
    {
        // Manuel bağlanan butonlar
        if (zorlaCarpan2Button != null)
        {
            zorlaCarpan2Button.onClick.RemoveAllListeners();
            zorlaCarpan2Button.onClick.AddListener(() => ZorlaCarpanTıkla(2));
        }

        if (zorlaCarpan5Button != null)
        {
            zorlaCarpan5Button.onClick.RemoveAllListeners();
            zorlaCarpan5Button.onClick.AddListener(() => ZorlaCarpanTıkla(5));
        }

        if (zorlaCarpan10Button != null)
        {
            zorlaCarpan10Button.onClick.RemoveAllListeners();
            zorlaCarpan10Button.onClick.AddListener(() => ZorlaCarpanTıkla(10));
        }

        if (zorlaCarpan50Button != null)
        {
            zorlaCarpan50Button.onClick.RemoveAllListeners();
            zorlaCarpan50Button.onClick.AddListener(() => ZorlaCarpanTıkla(50));
        }

        if (zorlaCarpan100Button != null)
        {
            zorlaCarpan100Button.onClick.RemoveAllListeners();
            zorlaCarpan100Button.onClick.AddListener(() => ZorlaCarpanTıkla(100));
        }

        if (zorlaCarpanSifirlaButton != null)
        {
            zorlaCarpanSifirlaButton.onClick.RemoveAllListeners();
            zorlaCarpanSifirlaButton.onClick.AddListener(ZorlaCarpanSifirla);
        }

        // Otomatik buton bulma - isimlerinden
        // NOT: zorlaCarpanXButton alanlarına atanmış butonlar zaten yukarıda bağlandığı için burada tekrar dokunmuyoruz.
        Button[] butonlar = FindObjectsOfType<Button>(true);
        foreach (Button b in butonlar)
        {
            if (b == null) continue;
            if (b == zorlaCarpan2Button || b == zorlaCarpan5Button || b == zorlaCarpan10Button || b == zorlaCarpan50Button || b == zorlaCarpan100Button || b == zorlaCarpanSifirlaButton)
                continue;

            string adi = b.gameObject.name.ToLower();

            if (adi.Contains("carpan2") || adi.Contains("x2") || adi.Contains("carpan_2") || adi.Contains("forcex2") || adi.Contains("force_2"))
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => ZorlaCarpanTıkla(2));
                Debug.Log($"[ADMIN] Carpan2 butonu bulundu: {b.gameObject.name}");
            }
            else if (adi.Contains("carpan5") || adi.Contains("x5") || adi.Contains("carpan_5") || adi.Contains("forcex5") || adi.Contains("force_5"))
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => ZorlaCarpanTıkla(5));
                Debug.Log($"[ADMIN] Carpan5 butonu bulundu: {b.gameObject.name}");
            }
            else if (adi.Contains("carpan10") || adi.Contains("x10") || adi.Contains("carpan_10") || adi.Contains("forcex10") || adi.Contains("force_10"))
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => ZorlaCarpanTıkla(10));
                Debug.Log($"[ADMIN] Carpan10 butonu bulundu: {b.gameObject.name}");
            }
            else if (adi.Contains("carpan50") || adi.Contains("x50") || adi.Contains("carpan_50") || adi.Contains("forcex50") || adi.Contains("force_50"))
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => ZorlaCarpanTıkla(50));
                Debug.Log($"[ADMIN] Carpan50 butonu bulundu: {b.gameObject.name}");
            }
            else if (adi.Contains("carpan100") || adi.Contains("x100") || adi.Contains("carpan_100") || adi.Contains("forcex100") || adi.Contains("force_100"))
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => ZorlaCarpanTıkla(100));
                Debug.Log($"[ADMIN] Carpan100 butonu bulundu: {b.gameObject.name}");
            }
            else if (adi.Contains("carpansifirla") || adi.Contains("carpan_sifirla"))
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(ZorlaCarpanSifirla);
                Debug.Log($"[ADMIN] CarpanSifirla butonu bulundu: {b.gameObject.name}");
            }
        }
    }

    // ========== PUBLIC METOTLAR - Unity OnClick için ==========
    
    // Inspector'daki butonların OnClick'ine bu metotları bağlayabilirsiniz
    public void ZorlaCarpan2()
    {
        if (_adminIslemKilidiAktif) return;
        ZorlaCarpanTıkla(2);
    }

    public void ZorlaCarpan5()
    {
        if (_adminIslemKilidiAktif) return;
        ZorlaCarpanTıkla(5);
    }

    public void ZorlaCarpan10()
    {
        if (_adminIslemKilidiAktif) return;
        ZorlaCarpanTıkla(10);
    }

    public void ZorlaCarpan50()
    {
        if (_adminIslemKilidiAktif) return;
        ZorlaCarpanTıkla(50);
    }

    public void ZorlaCarpan100()
    {
        if (_adminIslemKilidiAktif) return;
        ZorlaCarpanTıkla(100);
    }

    public void ZorlaCarpanSifirla()
    {
        if (_adminIslemKilidiAktif) return;
        ZorlaCarpanTıkla(0);
    }

    // Private helper
    private void ZorlaCarpanTıkla(int deger)
    {
        if (_adminIslemKilidiAktif) return;
        if (deger > 0 && _oyunYoneticisi != null && _oyunYoneticisi.IsSenaryo1Aktif())
        {
            if (uyariText != null)
            {
                uyariText.text = "Senaryo 1'de zorla çarpan kullanılamaz.";
                uyariText.color = new Color(0.95f, 0.38f, 0.38f, 1f);
            }
            Debug.LogWarning("[ADMIN] Senaryo 1 aktifken zorla çarpan engellendi.");
            return;
        }
        if (_oyunYoneticisi != null)
        {
            _oyunYoneticisi.AdminZorlaCarpanSec(deger);
            OturumKayitcisi.EkleEvent(OturumKayitcisi.OlayTipi_CarpanZorla, $"carpan=x{deger} (admin panel)");
            Debug.Log($"[ADMIN] Zorla çarpan tıklandı: x{deger}");
            ZorlaCarpanDurumMetniniGuncelle();
        }
    }

    public void ZorlaCarpanButonlarinSenaryo1GoreGuncelle(bool senaryo1Aktif)
    {
        if (zorlaCarpan2Button != null) zorlaCarpan2Button.interactable = !senaryo1Aktif;
        if (zorlaCarpan5Button != null) zorlaCarpan5Button.interactable = !senaryo1Aktif;
        if (zorlaCarpan10Button != null) zorlaCarpan10Button.interactable = !senaryo1Aktif;
        if (zorlaCarpan50Button != null) zorlaCarpan50Button.interactable = !senaryo1Aktif;
        if (zorlaCarpan100Button != null) zorlaCarpan100Button.interactable = !senaryo1Aktif;
    }

    private void OtomatikButonlariBulVeBagla()
    {
        // CarpanSifirla / zorla çarpan sıfırlama adında da "sifirla" geçer; onları atla, yoksa kullanıcı sıfırlama hiç bağlanmaz.
        Button[] butonlar = FindObjectsOfType<Button>(true);
        Button enIyi = null;
        int enSkor = -1;
        foreach (Button b in butonlar)
        {
            if (b == null) continue;
            if (b == zorlaCarpanSifirlaButton || b == zorlaCarpan2Button || b == zorlaCarpan5Button ||
                b == zorlaCarpan10Button || b == zorlaCarpan50Button || b == zorlaCarpan100Button)
                continue;

            string ad = (b.gameObject.name ?? "").ToLowerInvariant();
            if (ad.Contains("carpan") && (ad.Contains("sifirla") || ad.Contains("sıfırla"))) continue;
            if (ad.Contains("carpansifirla") || ad.Contains("carpan_sifirla") || ad.Contains("zorla")) continue;
            if (!ad.Contains("sifirla") && !ad.Contains("sıfırla") && !ad.Contains("reset")) continue;

            int skor = 0;
            if (ad == "sifirla" || ad == "sıfırla" || ad == "reset" || ad == "btnsifirla") skor = 100;
            else if (ad.Contains("kullanici") || ad.Contains("tum") || ad.Contains("user")) skor = 80;
            else if (!ad.Contains("carpan")) skor = 50;
            if (skor > enSkor)
            {
                enSkor = skor;
                enIyi = b;
            }
        }

        if (enIyi != null)
        {
            _sifirlaButonu = enIyi;
            enIyi.onClick.RemoveAllListeners();
            enIyi.onClick.AddListener(SifirlamaOnayPopupGoster);
            Debug.Log($"[ADMIN] Kullanıcı sıfırlama / silme butonu bağlandı: {enIyi.gameObject.name}");
        }
    }

    private void SifirlamaOnayPopupGoster()
    {
        if (_oyunYoneticisi != null)
        {
            _oyunYoneticisi.SendMessage("AdminSifirlamaOnayPopupGoster", SendMessageOptions.DontRequireReceiver);
            return;
        }

        // OyunYoneticisi yoksa doğrudan sıfırla (fallback).
        TumKullanicilariSifirla();
    }

    private void InitializeSliders()
    {
        if (scatterSlider == null)
            scatterSlider = GameObject.Find("BonusDusmeSlider")?.GetComponent<Slider>() ?? GameObject.Find("ScatterSlider")?.GetComponent<Slider>();

        // Zorluk Slider
        if (zorlukSlider != null)
        {
            zorlukSlider.minValue = 4;
            zorlukSlider.maxValue = 12;
            zorlukSlider.wholeNumbers = true;
            zorlukSlider.onValueChanged.RemoveAllListeners();
            zorlukSlider.onValueChanged.AddListener(OnZorlukSliderChanged);

            // Başlangıç: senaryo zorluğunu kilitlemeden sadece UI senkronu (OnZorlukSliderChanged çağrılmaz).
            if (_oyunYoneticisi != null)
                zorlukSlider.SetValueWithoutNotify(_oyunYoneticisi.zorlukSeviyesi);
            if (zorlukDegerText != null)
                zorlukDegerText.text = Mathf.RoundToInt(zorlukSlider.value).ToString();
        }

        // Scatter Slider - YÜZDESEL olarak çalışır (0-100%)
        if (scatterSlider != null)
        {
            scatterSlider.minValue = 0;
            scatterSlider.maxValue = 100;
            scatterSlider.wholeNumbers = true;
            scatterSlider.onValueChanged.RemoveAllListeners();
            scatterSlider.onValueChanged.AddListener(OnScatterSliderChanged);

            // Başlangıç değerini ayarla (scatterChanceNormal'ı % olarak göster)
            if (_oyunYoneticisi != null)
                scatterSlider.value = _oyunYoneticisi.scatterChanceNormal * 100f;

            // İlk değeri UI'ya yansıt
            OnScatterSliderChanged(scatterSlider.value);
        }

        // Çarpan Olasılık Slider
        if (carpanOlasilikSlider != null)
        {
            carpanOlasilikSlider.minValue = 0;
            carpanOlasilikSlider.maxValue = 100;
            carpanOlasilikSlider.wholeNumbers = true;
            carpanOlasilikSlider.onValueChanged.RemoveAllListeners();
            carpanOlasilikSlider.onValueChanged.AddListener(OnCarpanOlasilikSliderChanged);

            // Başlangıç değerini ayarla
            if (_oyunYoneticisi != null)
                carpanOlasilikSlider.value = _oyunYoneticisi.carpanUretimOlasiligi * 100f;

            //İlk değeri UI'ya yansıt
            OnCarpanOlasilikSliderChanged(carpanOlasilikSlider.value);
        }

        // Çarpan Max Adet Slider
        if (carpanMaxAdetSlider != null)
        {
            carpanMaxAdetSlider.minValue = 0;
            carpanMaxAdetSlider.maxValue = 5;
            carpanMaxAdetSlider.wholeNumbers = true;
            carpanMaxAdetSlider.onValueChanged.RemoveAllListeners();
            carpanMaxAdetSlider.onValueChanged.AddListener(OnCarpanMaxAdetSliderChanged);

            // Başlangıç değerini ayarla
            if (_oyunYoneticisi != null)
                carpanMaxAdetSlider.value = _oyunYoneticisi.maxCarpanAdedi;

            //İlk değeri UI'ya yansıt
            OnCarpanMaxAdetSliderChanged(carpanMaxAdetSlider.value);
        }
    }

    private void SenaryoPresetDropdownHazirla()
    {
        if (senaryoPresetDropdown == null)
            senaryoPresetDropdown = SenaryoPresetDropdownBul();
        if (senaryoPresetDropdownLegacy == null)
            senaryoPresetDropdownLegacy = SenaryoPresetDropdownLegacyBul();

        if ((senaryoPresetDropdown == null && senaryoPresetDropdownLegacy == null) || _senaryoPresetleri == null || _senaryoPresetleri.Length == 0)
        {
            Debug.LogWarning("[ADMIN-SENARYO] Senaryo preset dropdown bulunamadı. TMP_Dropdown veya legacy Dropdown adını 'SenaryoPresetDropdown' yapın.");
            return;
        }

        var ops = new List<string>(_senaryoPresetleri.Length + 1);
        ops.Add("0. NORMAL OYUN");
        for (int i = 0; i < _senaryoPresetleri.Length; i++)
            ops.Add(_senaryoPresetleri[i].Ad);

        if (senaryoPresetDropdown != null)
        {
            senaryoPresetDropdown.onValueChanged.RemoveListener(OnSenaryoPresetDegisti);
            senaryoPresetDropdown.ClearOptions();
            senaryoPresetDropdown.AddOptions(ops);
            senaryoPresetDropdown.value = 0;
            senaryoPresetDropdown.RefreshShownValue();
            senaryoPresetDropdown.onValueChanged.AddListener(OnSenaryoPresetDegisti);
        }

        if (senaryoPresetDropdownLegacy != null)
        {
            senaryoPresetDropdownLegacy.onValueChanged.RemoveListener(OnSenaryoPresetLegacyDegisti);
            senaryoPresetDropdownLegacy.ClearOptions();
            var legacyOps = new List<Dropdown.OptionData>(ops.Count);
            for (int i = 0; i < ops.Count; i++)
                legacyOps.Add(new Dropdown.OptionData(ops[i]));
            senaryoPresetDropdownLegacy.AddOptions(legacyOps);
            senaryoPresetDropdownLegacy.value = 0;
            senaryoPresetDropdownLegacy.RefreshShownValue();
            senaryoPresetDropdownLegacy.onValueChanged.AddListener(OnSenaryoPresetLegacyDegisti);
        }

        // Normal Oyun her zaman varsayılan; dropdown 0 → Normal Oyun modu.
        NormalOyunModunuUygula(false);
    }

    private void OnSenaryoPresetDegisti(int index)
    {
        if (_adminIslemKilidiAktif) return;
        if (index == 0)
        {
            NormalOyunModunuUygula(true);
            return;
        }
        _senaryoPresetAktif = true;
        SenaryoPresetUygula(index - 1, true);
    }

    private void OnSenaryoPresetLegacyDegisti(int index)
    {
        OnSenaryoPresetDegisti(index);
    }

    private void NormalOyunModunuUygula(bool gorselAnim)
    {
        _senaryoPresetAktif = false;

        if (_senaryoAnimCoroutine != null)
            StopCoroutine(_senaryoAnimCoroutine);
        _senaryoAnimCoroutine = StartCoroutine(NormalOyunModunuUygulaEnum(gorselAnim));
    }

    private IEnumerator NormalOyunModunuUygulaEnum(bool gorselAnim)
    {
        float sure = gorselAnim ? 0.35f : 0f;
        yield return SliderDegeriAnimleVeUygula(scatterSlider, 14, sure, OnScatterSliderChanged);
        yield return SliderDegeriAnimleVeUygula(carpanOlasilikSlider, 15, sure, OnCarpanOlasilikSliderChanged);
        yield return SliderDegeriAnimleVeUygula(carpanMaxAdetSlider, 3, sure, OnCarpanMaxAdetSliderChanged);

        _oyunYoneticisi?.AdminNormalOyunUygula();

        if (uyariText != null)
            uyariText.text = "✅ Normal Oyun | Senaryo 1-5 kapalı | Eğilim %65 | Dağılım %30";
    }

    private void SenaryoPresetUygula(int index, bool gorselAnim)
    {
        if (_oyunYoneticisi == null || _senaryoPresetleri == null || _senaryoPresetleri.Length == 0)
            return;
        index = Mathf.Clamp(index, 0, _senaryoPresetleri.Length - 1);
        SenaryoPreset p = _senaryoPresetleri[index];

        if (_senaryoAnimCoroutine != null)
            StopCoroutine(_senaryoAnimCoroutine);
        _senaryoAnimCoroutine = StartCoroutine(SenaryoPresetUygulaAnimliEnum(p, gorselAnim));
    }

    private IEnumerator SenaryoPresetUygulaAnimliEnum(SenaryoPreset p, bool gorselAnim)
    {
        float sure = gorselAnim ? 0.35f : 0f;
        yield return SliderDegeriAnimleVeUygula(scatterSlider, p.ScatterYuzde, sure, OnScatterSliderChanged);
        yield return SliderDegeriAnimleVeUygula(carpanOlasilikSlider, p.CarpanYuzde, sure, OnCarpanOlasilikSliderChanged);
        yield return SliderDegeriAnimleVeUygula(carpanMaxAdetSlider, p.MaxCarpanAdedi, sure, OnCarpanMaxAdetSliderChanged);

        _oyunYoneticisi.AdminBahisAyarla(p.Bahis);
        _oyunYoneticisi.AdminMaxScatterPerSpinAyarla(p.MaxScatterPerSpin);
        _oyunYoneticisi.AdminZorlaCarpanSec(p.ZorlaCarpan);
        ZorlaCarpanDurumMetniniGuncelle();

        if (uyariText != null)
            uyariText.text = $"✅ {p.Ad} yüklendi | Bahis {p.Bahis} TL | Max Scatter {p.MaxScatterPerSpin}";
    }

    private TMP_Dropdown SenaryoPresetDropdownBul()
    {
        TMP_Dropdown dd = GameObject.Find("SenaryoPresetDropdown")?.GetComponent<TMP_Dropdown>();
        if (dd != null) return dd;
        dd = GameObject.Find("SenaryoModuDropdown")?.GetComponent<TMP_Dropdown>();
        if (dd != null) return dd;
        dd = GameObject.Find("SenaryoDropdown")?.GetComponent<TMP_Dropdown>();
        if (dd != null) return dd;

        TMP_Dropdown[] tumDropdownlar = FindObjectsOfType<TMP_Dropdown>(true);
        for (int i = 0; i < tumDropdownlar.Length; i++)
        {
            var d = tumDropdownlar[i];
            if (d == null || d.gameObject == null) continue;
            string ad = d.gameObject.name.ToLowerInvariant();
            if (ad.Contains("senaryo") || ad.Contains("preset") || ad.Contains("scenario"))
                return d;
        }
        return null;
    }

    private Dropdown SenaryoPresetDropdownLegacyBul()
    {
        Dropdown dd = GameObject.Find("SenaryoPresetDropdown")?.GetComponent<Dropdown>();
        if (dd != null) return dd;
        dd = GameObject.Find("SenaryoModuDropdown")?.GetComponent<Dropdown>();
        if (dd != null) return dd;
        dd = GameObject.Find("SenaryoDropdown")?.GetComponent<Dropdown>();
        if (dd != null) return dd;

        Dropdown[] tumDropdownlar = FindObjectsOfType<Dropdown>(true);
        for (int i = 0; i < tumDropdownlar.Length; i++)
        {
            var d = tumDropdownlar[i];
            if (d == null || d.gameObject == null) continue;
            string ad = d.gameObject.name.ToLowerInvariant();
            if (ad.Contains("senaryo") || ad.Contains("preset") || ad.Contains("scenario"))
                return d;
        }
        return null;
    }

    private IEnumerator SliderDegeriAnimleVeUygula(Slider slider, float hedef, float sure, System.Action<float> uygula)
    {
        if (slider == null || uygula == null) yield break;
        float min = slider.minValue;
        float max = slider.maxValue;
        float h = Mathf.Clamp(hedef, min, max);
        if (sure <= 0.01f)
        {
            slider.SetValueWithoutNotify(h);
            uygula(h);
            yield break;
        }

        float bas = slider.value;
        float gecen = 0f;
        while (gecen < sure)
        {
            gecen += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(gecen / sure);
            float e = Mathf.SmoothStep(0f, 1f, t);
            float v = Mathf.Lerp(bas, h, e);
            slider.SetValueWithoutNotify(v);
            uygula(v);
            yield return null;
        }
        slider.SetValueWithoutNotify(h);
        uygula(h);
    }

    // Zorluk Slider Değiştiğinde (tek giriş: OY wrapper üzerinden apply)
    public void OnZorlukSliderChanged(float value)
    {
        if (_adminIslemKilidiAktif) return;
        if (zorlukDegerText != null)
            zorlukDegerText.text = Mathf.RoundToInt(value).ToString();
        _oyunYoneticisi?.OnZorlukSliderChanged(value);
    }

    // Scatter Slider Değiştiğinde (tek giriş: OY wrapper üzerinden apply)
    public void OnScatterSliderChanged(float value)
    {
        if (_adminIslemKilidiAktif) return;
        int yuzde = Mathf.RoundToInt(value);
        if (scatterDegerText != null) scatterDegerText.text = $"Scatter Düşme %{yuzde}";
        if (scatterDusmeOraniText != null) scatterDusmeOraniText.text = $"Scatter Düşme %{yuzde}";
        _oyunYoneticisi?.OnScatterSliderChanged(value);
    }

    // Çarpan Olasılık Slider Değiştiğinde (tek giriş: OY wrapper)
    public void OnCarpanOlasilikSliderChanged(float value)
    {
        if (_adminIslemKilidiAktif) return;
        _oyunYoneticisi?.OnCarpanOlasilikSliderChanged(value);
    }

    // Çarpan Max Adet Slider Değiştiğinde (tek giriş: OY wrapper)
    public void OnCarpanMaxAdetSliderChanged(float value)
    {
        if (_adminIslemKilidiAktif) return;
        _oyunYoneticisi?.OnCarpanMaxAdetSliderChanged(value);
    }

    public void SifrePaneliGoster()
    {
        if (sifrePanel)
        {
            sifrePanel.SetActive(true);
            if (sifreInput) sifreInput.text = "";
            if (uyariText) uyariText.text = "";
            sifreInput?.Select();
        }
    }

    public void SifreKontrolEt(string sifre)
    {
        if (sifre == adminSifre)
        {
            if (sifrePanel) sifrePanel.SetActive(false);
            if (adminAnaPanel) adminAnaPanel.SetActive(true);
            _adminAcik = true;
            _senaryoPresetHazirlandi = false;
            SenaryoPresetDropdownHazirla();
            _senaryoPresetHazirlandi = senaryoPresetDropdown != null || senaryoPresetDropdownLegacy != null;
            Debug.Log("[ADMIN] Panel açıldı");
        }
        else
        {
            if (uyariText) uyariText.text = "❌ Şifre yanlış!";
        }
    }

    public void AdminPaneliKapat()
    {
        if (adminAnaPanel) adminAnaPanel.SetActive(false);
        _adminAcik = false;
    }

    // ============================================
    // TÜM KULLANICILARI SIFIRLA
    // ============================================
    public void TumKullanicilariSifirla()
    {
        Debug.Log("[ADMIN] Kayıtlı kullanıcılar siliniyor (profiles.json + ilgili PlayerPrefs)...");

        int silinenSayi = GameManager.I != null && GameManager.I.Profiles != null
            ? GameManager.I.Profiles.Count
            : GameManager.LoadProfiles().Count;

        if (silinenSayi == 0)
        {
            GameManager.TumKullanicilariVeVerileriSil();
            Debug.LogWarning("[ADMIN] Silinecek kayıtlı kullanıcı yoktu; yine de kasa/havuz tercihleri temizlendi.");
            if (uyariText != null)
                uyariText.text = "Kayıtlı kullanıcı yoktu. Kasa/havuz verileri sıfırlandı.";
        }
        else
            GameManager.TumKullanicilariVeVerileriSil();

        var kasaYoneticisi = FindFirstObjectByType<KasaYoneticisi>(FindObjectsInactive.Include);
        if (kasaYoneticisi != null)
        {
            kasaYoneticisi.SetAnaKasa(0);
            kasaYoneticisi.SetOdulHavuzu(0);
            kasaYoneticisi.UI_Guncelle();
        }

        Debug.Log($"[ADMIN] {silinenSayi} kullanıcı kaydı silindi.");
        if (uyariText != null)
            uyariText.text = silinenSayi > 0
                ? $"✅ {silinenSayi} kullanıcı silindi, kasa/havuz sıfırlandı!"
                : uyariText.text;
        SonucMesajKutusuGoster(silinenSayi > 0
            ? $"{silinenSayi} kayıtlı kullanıcı silindi.\nAna kasa ve ödül havuzu sıfırlandı."
            : "Kayıtlı kullanıcı yoktu.\nAna kasa ve ödül havuzu sıfırlandı.");
    }

    private void SonucMesajKutusuGoster(string mesaj)
    {
        const string popupAd = "AdminResetSonucPopup";
        var mevcut = GameObject.Find(popupAd);
        if (mevcut != null)
            Destroy(mevcut);

        var canvasGo = new GameObject(popupAd);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 3200;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();

        var arkaPlan = new GameObject("ArkaPlan");
        arkaPlan.transform.SetParent(canvasGo.transform, false);
        var arkaRt = arkaPlan.AddComponent<RectTransform>();
        arkaRt.anchorMin = Vector2.zero;
        arkaRt.anchorMax = Vector2.one;
        arkaRt.offsetMin = Vector2.zero;
        arkaRt.offsetMax = Vector2.zero;
        var arkaImg = arkaPlan.AddComponent<Image>();
        arkaImg.color = new Color(0f, 0f, 0f, 0.62f);
        arkaImg.raycastTarget = true;

        var panel = new GameObject("Panel");
        panel.transform.SetParent(arkaPlan.transform, false);
        var panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(620f, 240f);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.1f, 0.14f, 0.96f);

        var yaziGo = new GameObject("Mesaj");
        yaziGo.transform.SetParent(panel.transform, false);
        var yaziRt = yaziGo.AddComponent<RectTransform>();
        yaziRt.anchorMin = new Vector2(0.1f, 0.34f);
        yaziRt.anchorMax = new Vector2(0.9f, 0.86f);
        yaziRt.offsetMin = Vector2.zero;
        yaziRt.offsetMax = Vector2.zero;
        var yazi = yaziGo.AddComponent<TextMeshProUGUI>();
        yazi.text = mesaj;
        yazi.fontSize = 34;
        yazi.alignment = TMPro.TextAlignmentOptions.Center;
        yazi.color = Color.white;
        yazi.raycastTarget = false;

        var btnGo = new GameObject("TamamButon");
        btnGo.transform.SetParent(panel.transform, false);
        var btnRt = btnGo.AddComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0.18f);
        btnRt.anchorMax = new Vector2(0.5f, 0.18f);
        btnRt.pivot = new Vector2(0.5f, 0.5f);
        btnRt.sizeDelta = new Vector2(190f, 62f);
        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0.18f, 0.56f, 0.2f, 1f);
        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(() =>
        {
            if (canvasGo != null)
                Destroy(canvasGo);
        });

        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(btnGo.transform, false);
        var txtRt = txtGo.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;
        var txt = txtGo.AddComponent<TextMeshProUGUI>();
        txt.text = "TAMAM";
        txt.fontSize = 30;
        txt.alignment = TMPro.TextAlignmentOptions.Center;
        txt.color = Color.white;
        txt.raycastTarget = false;
    }
}
