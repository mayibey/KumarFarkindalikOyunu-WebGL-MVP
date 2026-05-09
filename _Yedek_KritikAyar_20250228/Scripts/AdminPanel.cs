using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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

    [Header("Zorla Çarpan Butonları")]
    public Button zorlaCarpan2Button;
    public Button zorlaCarpan5Button;
    public Button zorlaCarpan10Button;
    public Button zorlaCarpan50Button;
    public Button zorlaCarpan100Button;
    public Button zorlaCarpanSifirlaButton;

    // OyunYoneticisi referansı
    private OyunYoneticisi _oyunYoneticisi;
    private Button _sifirlaButonu;

    private bool _adminAcik = false;

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
        Button[] butonlar = FindObjectsOfType<Button>(true);
        foreach (Button b in butonlar)
        {
            if (b == null) continue;
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
        if (_oyunYoneticisi != null)
        {
            _oyunYoneticisi.zorlaSiradakiCarpan = 2;
            Debug.Log("[ADMIN] Zorla çarpan: x2");
        }
    }

    public void ZorlaCarpan5()
    {
        if (_oyunYoneticisi != null)
        {
            _oyunYoneticisi.zorlaSiradakiCarpan = 5;
            Debug.Log("[ADMIN] Zorla çarpan: x5");
        }
    }

    public void ZorlaCarpan10()
    {
        if (_oyunYoneticisi != null)
        {
            _oyunYoneticisi.zorlaSiradakiCarpan = 10;
            Debug.Log("[ADMIN] Zorla çarpan: x10");
        }
    }

    public void ZorlaCarpan50()
    {
        if (_oyunYoneticisi != null)
        {
            _oyunYoneticisi.zorlaSiradakiCarpan = 50;
            Debug.Log("[ADMIN] Zorla çarpan: x50");
        }
    }

    public void ZorlaCarpan100()
    {
        if (_oyunYoneticisi != null)
        {
            _oyunYoneticisi.zorlaSiradakiCarpan = 100;
            Debug.Log("[ADMIN] Zorla çarpan: x100");
        }
    }

    public void ZorlaCarpanSifirla()
    {
        if (_oyunYoneticisi != null)
        {
            _oyunYoneticisi.zorlaSiradakiCarpan = 0;
            Debug.Log("[ADMIN] Zorla çarpan sıfırlandı");
        }
    }

    // Private helper
    private void ZorlaCarpanTıkla(int deger)
    {
        if (_oyunYoneticisi != null)
        {
            _oyunYoneticisi.zorlaSiradakiCarpan = deger;
            Debug.Log($"[ADMIN] Zorla çarpan tıklandı: x{deger}");
        }
    }

    private void OtomatikButonlariBulVeBagla()
    {
        // "Sifirla" veya "Sıfırla" adlı butonu ara
        Button[] butonlar = FindObjectsOfType<Button>(true);
        
        foreach (Button b in butonlar)
        {
            if (b == null) continue;
            string butonAdi = b.gameObject.name.ToLower();
            
            // Sıfırla butonunu bul ve bağla
            if (butonAdi.Contains("sifirla") || butonAdi.Contains("sıfırla") || butonAdi.Contains("reset"))
            {
                _sifirlaButonu = b;
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(TumKullanicilariSifirla);
                Debug.Log($"[ADMIN] Sıfırla butonu bulundu ve bağlandı: {b.gameObject.name}");
                break;
            }
        }
    }

    private void InitializeSliders()
    {
        // Zorluk Slider
        if (zorlukSlider != null)
        {
            zorlukSlider.minValue = 4;
            zorlukSlider.maxValue = 12;
            zorlukSlider.wholeNumbers = true;
            zorlukSlider.onValueChanged.RemoveAllListeners();
            zorlukSlider.onValueChanged.AddListener(OnZorlukSliderChanged);
            
            // Başlangıç değerini ayarla
            if (_oyunYoneticisi != null)
                zorlukSlider.value = _oyunYoneticisi.zorlukSeviyesi;

            //İlk değeri UI'ya yansıt
            OnZorlukSliderChanged(zorlukSlider.value);
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

    // Zorluk Slider Değiştiğinde (tek giriş: OY wrapper üzerinden apply)
    public void OnZorlukSliderChanged(float value)
    {
        if (zorlukDegerText != null)
            zorlukDegerText.text = Mathf.RoundToInt(value).ToString();
        _oyunYoneticisi?.OnZorlukSliderChanged(value);
    }

    // Scatter Slider Değiştiğinde (tek giriş: OY wrapper üzerinden apply)
    public void OnScatterSliderChanged(float value)
    {
        int yuzde = Mathf.RoundToInt(value);
        if (scatterDegerText != null) scatterDegerText.text = $"Scatter Düşme %{yuzde}";
        if (scatterDusmeOraniText != null) scatterDusmeOraniText.text = $"Scatter Düşme %{yuzde}";
        _oyunYoneticisi?.OnScatterSliderChanged(value);
    }

    // Çarpan Olasılık Slider Değiştiğinde (tek giriş: OY wrapper)
    public void OnCarpanOlasilikSliderChanged(float value)
    {
        if (carpanOlasilikText != null) carpanOlasilikText.text = $"%{Mathf.RoundToInt(value)}";
        _oyunYoneticisi?.OnCarpanOlasilikSliderChanged(value);
    }

    // Çarpan Max Adet Slider Değiştiğinde (tek giriş: OY wrapper)
    public void OnCarpanMaxAdetSliderChanged(float value)
    {
        int adet = Mathf.RoundToInt(value);
        if (carpanMaxAdetText != null) carpanMaxAdetText.text = adet.ToString();
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
        Debug.Log("[ADMIN] Tüm kullanıcılar sıfırlanıyor...");
        
        // GameManager üzerinden profilleri al
        if (GameManager.I != null && GameManager.I.Profiles != null)
        {
            var profiles = GameManager.I.Profiles;
            int sifirlananSayisi = 0;
            
            foreach (var profile in profiles)
            {
                if (profile != null)
                {
                    // Bakiyeyi 20000 TL yap
                    profile.balance = 20000;
                    
                    // Tüm istatistikleri sıfırla
                    profile.totalDeposited = 0;
                    profile.totalWithdrawn = 0;
                    profile.totalSessions = 0;
                    profile.totalNet = 0;
                    profile.totalSpins = 0;
                    profile.totalBonusEntries = 0;
                    profile.totalWagered = 0;
                    profile.totalWon = 0;
                    profile.totalLost = 0;
                    
                    sifirlananSayisi++;
                    Debug.Log($"[ADMIN] Kullanıcı sıfırlandı: {profile.playerName} - Yeni bakiye: 20000 TL");
                }
            }
            
            // Değişiklikleri kaydet
            GameManager.SaveProfiles(profiles);
            
            Debug.Log($"[ADMIN] Toplam {sifirlananSayisi} kullanıcı sıfırlandı ve kaydedildi!");
            
            if (uyariText != null)
                uyariText.text = $"✅ {sifirlananSayisi} kullanıcı sıfırlandı!";
        }
        else
        {
            Debug.LogWarning("[ADMIN] GameManager veya Profiller bulunamadı!");
            if (uyariText != null)
                uyariText.text = "❌ Kullanıcı verileri bulunamadı!";
        }
    }
}
