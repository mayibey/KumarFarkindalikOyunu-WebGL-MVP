using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class OyunYoneticisi : MonoBehaviour, SahneBaglamaServisi.IBaglamaHedefi, IDonusAkisBaglami, IOyunUIGuncellemeBaglami, IScatterEfektBaglami, ITumbleAkisBaglami, ICokmeAkisBaglami, IIzgaraBaslatmaBaglami, IOyunBootstrapBaglami, ICarpanYerlestirmeBaglami, IZorlukBaglami, IOyunKorumaBaglami
{
    
    
// === LOG / İSTATİSTİK (otomatik kayıt) ===
    private LogServisi _logServisi;
    private EkonomiServisi _ekonomiServisi;
    private UIServisi _uiServisi;
    private SenaryoServisi _senaryoServisi;
    private DonusServisi _donusServisi;
    private IzgaraServisi _izgaraServisi;
    private TumbleServisi _tumbleServisi;
    private CarpanServisi _carpanServisi;
    private AnimasyonServisi _animasyonServisi;
    private CarpanOverlayServisi _carpanOverlayServisi;
    private KorutinServisi _korutinServisi;
    private BonusUIServisi _bonusUIServisi;
    private HizVeSesServisi _hizVeSesServisi;
    private AdminAyarUIServisi _adminAyarUIServisi;
    private SahneBaglamaServisi _sahneBaglamaServisi;
    private OdemeServisi _odemeServisi;
    private DonusAkisServisi _donusAkisServisi;
    private OyunUIGuncellemeServisi _oyunUIGuncellemeServisi;
    private ScatterEfektServisi _scatterEfektServisi;
    private TumbleAkisServisi _tumbleAkisServisi;
    private CokmeAkisServisi _cokmeAkisServisi;
    private IzgaraBaslatmaServisi _izgaraBaslatmaServisi;
    private OyunBootstrapServisi _oyunBootstrapServisi;
    private CarpanYerlestirmeServisi _carpanYerlestirmeServisi;
    private ZorlukServisi _zorlukServisi;
    private BonusAyarlari _bonusAyarlari;
    private int _spinPrevBakiye = 0;
    private int _spinBahisTL = 0;

    public TextMeshProUGUI bakiyeYukleUyariText; // Input altındaki sonuç yazısı
    public TextMeshProUGUI paraCekUyariText;     // Input altındaki sonuç yazısı

    private void CloseMoneyPanels()
    {
        if (bakiyeYuklePanel != null) bakiyeYuklePanel.SetActive(false);
        if (paraCekPanel != null) paraCekPanel.SetActive(false);
    }

    // Inspector OnClick için PUBLIC wrapper'lar (Unity sadece public metotları listeler)
    public void ParaCek_OnayButton()
    {
        _ekonomiServisi?.OnParaCekOnay();
    }

    public void ParaCek_IptalButton()
    {
        _uiServisi?.HideParaCekPanel();
    }

    public void BakiyeYukle_OnayButton()
    {
        _ekonomiServisi?.OnBakiyeYukleOnay();
    }

    public void BakiyeYukle_IptalButton()
    {
        _uiServisi?.HideBakiyeYuklePanel();
    }

    public void ShowParaCekPanel()
    {
        _uiServisi?.ResolveMoneyUIRefsIfMissing();
        _uiServisi?.WireParaCekUI();

        if (paraCekUyariText != null) paraCekUyariText.text = "";

        _uiServisi?.CloseMoneyPanels();
        Debug.Log("[UI] ParaCekButon tıklandı. Panel=" + (paraCekPanel != null ? paraCekPanel.name : "NULL"));

        if (paraCekUyariText != null) paraCekUyariText.text = "";
        if (paraCekInput != null) paraCekInput.text = "";

        if (paraCekPanel != null)
            paraCekPanel.SetActive(true);
    }

    public void HideParaCekPanel()
    {
        if (paraCekPanel != null)
            paraCekPanel.SetActive(false);
    }



    public void SetZorluk(float deger)
    {
        _zorlukServisi?.ZorlukUygula(deger);
    }

    void IZorlukBaglami.SetZorlukSliderDegeri(int v) => _zorlukSliderDegeri = v;
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

    int IOyunKorumaBaglami.GetMaxTumbleTur() => OyunKorumaServisi.MAX_TUMBLE_TUR;
    int IOyunKorumaBaglami.GetTumbleSabitEsik() => OyunKorumaServisi.TUMBLE_SABIT_ESIK;

public TMPro.TextMeshProUGUI zorlukValueText;

    // Inspector / AdminPanel bağlantıları için wrapper (asıl binding AdminAyarUIServisi.BindAllAndRefresh)
    public void OnZorlukSliderChanged(float value) => _adminAyarUIServisi?.ApplyZorluk(value);
    public void OnScatterSliderChanged(float value) => _adminAyarUIServisi?.ApplyScatter(value);
    public void OnCarpanOlasilikSliderChanged(float value) => _adminAyarUIServisi?.ApplyCarpanOlasilik(value);
    public void OnCarpanMaxAdetSliderChanged(float value) => _adminAyarUIServisi?.ApplyCarpanMaxAdet(value);

    public TMPro.TextMeshProUGUI carpanOlasilikValueText;
    public TMPro.TextMeshProUGUI carpanMaxAdetValueText;

    private void EnsurePayTablesInitialized()
{
    int n = (sembolSpriteListesi != null) ? sembolSpriteListesi.Count : 0;
    if (n <= 0) return;

    // TumbleAyarlari'ndaki PayTable'ı kullan (ScatterIndex tek kaynak TumbleAyarlari'da)
    if (tumbleAyarlari != null)
        tumbleAyarlari.EnsurePayTablesInitialized(n);
}
private float _lastTumblePopTime = -999f;
    private float _lastTumbleDropTime = -999f;
    private int spinKazancHam = 0;   // tumble patlamalarından gelen ham toplam (bu spin)
    private int oturumKazanc = 0;    // oturum boyunca biriken toplam kazanç
    private bool _spinKazanciOturumaEklendi = false; // bonus'ta double sayma önler
    public TextMeshProUGUI oturumKazancText; // (senin OturumKazancText)

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


    


    // ==========================
    // PARA CEK UI
    // ==========================
    [Header("PARA CEK UI")]
	[HideInInspector]
    public Button paraCekButon;               // "ParaCekButon"
	[HideInInspector]
    public GameObject paraCekPanel;           // "ParaCekPanel"
	[HideInInspector]
    public TMP_InputField paraCekInput;       // "ParaCekInput"
	[HideInInspector]
    public Button paraCekOnayButon;           // paneldeki "CEK" butonu
	[HideInInspector]
    public Button paraCekIptalButon;          // paneldeki "KAPAT" butonu
   

    // ==========================
    // BAKIYE YÜKLE UI
    // ==========================
    [Header("BAKIYE YUKLE UI")]
	    [HideInInspector] public Button bakiyeYukleButon;              // "BakiyeYukleButon"
	    [HideInInspector] public GameObject bakiyeYuklePanel;          // "BakiyeYuklePanel"
	    [HideInInspector] public TMP_InputField bakiyeYukleInput;      // "BakiyeYukleInput"
	    [HideInInspector] public Button bakiyeYukleOnayButon;          // "OnayButon"
	    [HideInInspector] public Button bakiyeYukleIptalButon;         // "IptalButon"
   

    [Header("BONUS BUDGET (Ödül Havuzu Koruma)")]
    [HideInInspector] public bool bonusBudgetAktif = true;

    [Range(0f, 1f)]
    [Tooltip("Bonus başında ödül havuzunun ne kadarını bu bonus oturumuna ayıracağız? 0.20 = %20")]
    [HideInInspector] public float bonusBudgetHavuzOran = 0.25f;

    [Tooltip("Bonus için minimum bütçe (TL). Havuz çok azsa bile en az bu kadar ayır.")]
    [HideInInspector] public int bonusBudgetMinTL = 0;

    [Tooltip("Bonus için maksimum bütçe (TL). Havuz çok doluysa bile tavan.")]
    [HideInInspector] public int bonusBudgetMaxTL = 20000;

    private int _bonusBudgetKalanTL = 0;
    private int _bonusOturumOdenenToplamTL = 0;


    [Header("Bonus Ödeme Limiti")]
    [Range(0f, 1f)]
    [Tooltip("Bonus başladığında ödül havuzunun bu oranı kadar (örn 0.10 = %10) maksimum toplam ödeme yapılır. Bu limite ulaşıldıktan sonra bonus devam edebilir ama ek ödeme yapılmaz.")]
    [HideInInspector] public float bonusMaxOdemeHavuzOrani = 0.10f;

    private long _bonusBaslangicHavuzTL = 0;
    private int _bonusMaxOdemeTL = int.MaxValue;
    private int _bonusOdenenTL = 0;

    
    private int _bonusPendingOdemeTL = 0; // Bonus boyunca havuzdan düşülecek tutarı biriktir (bonus bitince tek seferde düş)

    [Header("Kasa Bazlı Kazan/Kaybet")]
    [HideInInspector] public bool kasaBazliDengeAktif = true;

    [Tooltip("Ödül havuzu boşken tumble neredeyse imkansız olsun")]
    [HideInInspector] public int minClusterSize_HavuzBos = 999;

    [Tooltip("Ödül havuzu çok doluyken tumble daha kolay")]
    [HideInInspector] public int minClusterSize_HavuzDolu = 6;

    [Tooltip("Ödül havuzu çok azken tumble daha zor")]
    [HideInInspector] public int minClusterSize_HavuzAz = 12;

    [Range(0f, 1f)]
    [Tooltip("Bu oranın altı 'havuz az' sayılır")]
    [HideInInspector] public float havuzAzEsik01 = 0.15f;

    [Range(0f, 1f)]
    [Tooltip("Bu oranın üstü 'havuz dolu' sayılır")]
    [HideInInspector] public float havuzDoluEsik01 = 0.70f;

    // zorluk slider'ın elle verdiği değer (4-12) halen dursun istiyorsan taban olarak kullanırız
    private int _zorlukSliderDegeri = 8;

    [Header("BONUS Otomatik Zorluk")]
	[HideInInspector] public bool bonusOtoZorlukAktif = true;

    [Tooltip("Bonus başında minClusterSize (kolaylık)")]
	[HideInInspector] public int bonusMinCluster_Easy = 6;

    [Tooltip("Budget biterken minClusterSize (zorluk)")]
	[HideInInspector] public int bonusMinCluster_Hard = 14;


    [Header("Kasa Sistemi")]
    [HideInInspector] public KasaYoneticisi kasa; // (LEGACY) Ayar/bağlantılar taşındı. İnspector kalabalığını azaltmak için gizli.

    [Header("Admin - Max Çarpan Slider UI")]
    [HideInInspector] public Slider carpanMaxAdetSlider;            // (LEGACY)
    [HideInInspector] public TextMeshProUGUI carpanMaxAdetText;     // (LEGACY)

    [Header("Zorluk Ayarı")]
    public int zorlukSeviyesi = 4;

    [Header("BONUS SATIN AL UI OBJESI")]
    [HideInInspector] public GameObject bonusSatinAlRoot; // (LEGACY)

    [Header("Tumble Kazancı UI")]
    [HideInInspector] public TextMeshProUGUI tumbleToplamText; // (LEGACY)
    private int tumbleToplamKazanc = 0;
    [Header("Admin - Çarpan Slider UI")]
    [HideInInspector] public Slider carpanOlasilikSlider;                 // (LEGACY)
    [HideInInspector] public TextMeshProUGUI carpanOlasilikText;          // (LEGACY)


    [Header("BONUS SATIN AL ONAY UI")]
    [HideInInspector] public GameObject bonusBuyConfirmPanel;
    [HideInInspector] public CanvasGroup bonusBuyConfirmCanvasGroup; // (LEGACY)
    [HideInInspector] public TMP_Text bonusBuyConfirmCostText; // (LEGACY)
    [HideInInspector] public Button bonusBuyYesButton;
    [HideInInspector] public Button bonusBuyNoButton;


    [Header("BONUS SATIN AL")]
    [HideInInspector] public Button bonusSatinAlButon;          // (LEGACY)
    [HideInInspector] public TextMeshProUGUI bonusSatinAlText;  // (LEGACY)
    [HideInInspector] public int bonusSatinAlCarpani = 100;     // (LEGACY)


    // === BAHİS +/- KONTROL ===
    [Header("Bahis Kontrol")]
    [HideInInspector] public int bahisMin = 1;
    [HideInInspector] public int bahisMax = 500;
    [HideInInspector] public int bahisAdim = 1;
    [HideInInspector] public SpinIconRotate spinIcon;   // (LEGACY)
    [HideInInspector] public Button bahisArttirButon; // (LEGACY)
    [HideInInspector] public Button bahisAzaltButon;  // (LEGACY)

    [Header("TUMBLE SES")]
    [HideInInspector] public AudioSource tumbleSfxSource;          // (LEGACY)
    [HideInInspector] public AudioClip tumblePopClip;              // (LEGACY)
    [HideInInspector] public AudioClip tumbleDropClip;             // (LEGACY)
    [HideInInspector] public float tumblePopMinInterval = 0.06f;   // (LEGACY)
    [HideInInspector] public float tumbleDropMinInterval = 0.12f;  // (LEGACY)

    [Header("BONUS END MUZIK")]
    [HideInInspector] public AudioSource bonusEndMusicAudio;   // (LEGACY)
    [Header("BONUS END SES")]
    [HideInInspector] public AudioSource bonusEndSfxSource;   // (LEGACY)
    [HideInInspector] public AudioClip bonusEndApplauseClip;  // (LEGACY)
    [Header("NORMAL OYUN MUZIK")]
    [HideInInspector] public AudioSource normalOyunMusic;   // (LEGACY)

    [Header("Grid Ayarları")]
    [HideInInspector] public int sutun = 6;
    [HideInInspector] public int satir = 5;
    [Header("SPIN DROP ANIM")]
    [HideInInspector] public float dropStartYOffset = 700f;   // (LEGACY)
    [HideInInspector] public float dropDuration = 0.25f;      // (LEGACY)
    [HideInInspector] public float dropStagger = 0.005f;      // (LEGACY)



    [Header("Semboller")]
    [HideInInspector] public List<Sprite> sembolSpriteListesi = new List<Sprite>();

    /// <summary>Scatter index tek kaynak: TumbleAyarlari.ScatterIndex. Start'ta oradan okunup _scatterIndexCache'e yazılır.</summary>
    private int _scatterIndexCache = 7;

    [Header("BONUS SCATTER EFEKT")]
    [HideInInspector] public float scatterScaleUp = 1.6f;        // (LEGACY)
    [HideInInspector] public float scatterAnimDuration = 0.6f;   // (LEGACY)
    [HideInInspector] public AudioSource bonusBellAudio;         // (LEGACY)

    [Header("Scatter Ayarları (Normal / Bonus)")]
    [HideInInspector] [Range(0f, 1f)] public float scatterChanceNormal = 0.005f;
    [HideInInspector] [Range(0f, 1f)] public float scatterChanceBonus = 0f; // Bonus oyununda scatter üretilmez
    [HideInInspector] public int scatterEsik = 4;
    public Slider scatterSliderUI;
    public TextMeshProUGUI scatterSliderText;
    public int maxScatterPerSpin = 5;

    [Header("Tumble / Eşleşme")]
    [Tooltip("En az kaç komşu aynı sembol gelirse patlasın (4-yön komşuluk).")]
    [HideInInspector] public int minClusterSize = 8;

    // Zorluk slider'ı (4-12) bunu ayarlar: 0(kolay) .. 1(zor)
    // KURAL DEĞİL; sadece "8'e tamamlayacak sembollerin" gelmesini baskılar.
    private float _easyBias01 = 0f; // zorluk 4..8 (kolaylaştırma)
    private float _hardBias01 = 0f; // zorluk 8..12 (zorlaştırma)

    // PayTable'ler TumbleAyarlari'ndan alınır

    [Header("Animasyon Hızları")]
    [HideInInspector] public float popDuration = 0.75f;
    [HideInInspector] public float fallDuration = 0.45f;
    [HideInInspector] public float betweenStepsDelay = 0.18f;
    [HideInInspector] public float spawnFromTopOffset = 240f;
    [Header("Bonus Hızları (Override)")]
    [HideInInspector] public bool bonusYavasMod = true;
    [HideInInspector] public float bonusPopDuration = 0.70f;
    [HideInInspector] public float bonusFallDuration = 0.80f;
    [HideInInspector] public float bonusBetweenStepsDelay = 0.35f;
    [HideInInspector] public float bonusSpinBeklemeOverride = 1.10f;


    [Header("Efekt (Opsiyonel)")]
    [HideInInspector] public ParticleSystem popParticlePrefab;

    [Header("Ekonomi / Bahis")]

    [Header("Bonus (Free Spin)")]
[HideInInspector] public int bonusHakBaslangic = 10;      // (LEGACY) BonusAyarlari.cs'ye tasindi.
[HideInInspector] public float bonusSpinBekleme = 0.70f;  // (LEGACY)

    [Header("Ayar Sistemleri (Yeni)")]
    [Tooltip("Sahnede Oyun_Sistemleri/TumbleAyarlari objesinde duran ayarlar. Simdilik sadece referans; mantik tasimayi sonra yapacagiz.")]
    public TumbleAyarlari tumbleAyarlari;

    [Tooltip("Sahnede Oyun_Sistemleri/CarpanAyarlari objesinde duran ayarlar. Simdilik sadece referans.")]
    public CarpanAyarlari carpanAyarlari;


    private void UygulaCarpanAyarlari()
    {
        if (carpanAyarlari == null) return;

        // CarpanAyarlari -> OyunYoneticisi (eski alanlara kopyala)
        carpanUretimiAktif = carpanAyarlari.CarpanUretimiAktif;
        carpanSadeceBonus = carpanAyarlari.CarpanSadeceBonus;
        carpanUretimOlasiligi = Mathf.Clamp01(carpanAyarlari.CarpanUretimOlasiligi);
        maxCarpanAdedi = Mathf.Max(0, carpanAyarlari.MaxCarpanAdedi);
        carpanHavuzu = Mathf.Max(0, carpanAyarlari.CarpanHavuzu);
        zorlaSiradakiCarpan = Mathf.Max(0, carpanAyarlari.ZorlaSiradakiCarpan);

        carpanSembolSprite = carpanAyarlari.CarpanSembolSprite;
        carpanOverlaySize = carpanAyarlari.CarpanOverlaySize;
        carpanOverlayFontSize = Mathf.Max(1, carpanAyarlari.CarpanOverlayFontSize);

        carpanYaziRengi = carpanAyarlari.CarpanYaziRengi;
        carpanYaziDisCizgiRengi = carpanAyarlari.CarpanYaziDisCizgiRengi;
        carpanYaziDisCizgiKalinlik = Mathf.Clamp01(carpanAyarlari.CarpanYaziDisCizgiKalinlik);
        carpanYaziKalin = carpanAyarlari.CarpanYaziKalin;

        carpanYaziGolge = carpanAyarlari.CarpanYaziGolge;
        carpanYaziGolgeRengi = carpanAyarlari.CarpanYaziGolgeRengi;
        carpanYaziGolgeOffset = carpanAyarlari.CarpanYaziGolgeOffset;

        carpanOverlayTextOffset = carpanAyarlari.CarpanOverlayTextOffset;
        carpanOverlayDropStartYOffset = carpanAyarlari.CarpanOverlayDropStartYOffset;
        carpanOverlayDropDuration = Mathf.Max(0f, carpanAyarlari.CarpanOverlayDropDuration);

        // Admin slider UI varsa, degerleri senkronla (slider kendi UI textini zaten Start'ta guncelliyor)
        if (carpanOlasilikSlider != null)
        {
            float yuzde = Mathf.Clamp(carpanUretimOlasiligi * 100f, carpanOlasilikSlider.minValue, carpanOlasilikSlider.maxValue);
            carpanOlasilikSlider.value = yuzde;
        }

        if (carpanMaxAdetSlider != null)
        {
            float v = Mathf.Clamp(maxCarpanAdedi, carpanMaxAdetSlider.minValue, carpanMaxAdetSlider.maxValue);
            carpanMaxAdetSlider.value = v;
        }
    }

    [Header("UI Referansları")]
    public Button cevirButon;
    public TMP_Text bakiyeText;
    public TMP_Text bahisText;
    public TMP_Text hakText;
    public TMP_Text kazancText;
    public TMP_Text carpanText;
    [Header("Bonus Uyarı UI")]
    public GameObject bonusStartPanel;
    [Header("Bonus Bitiş UI")]
    public GameObject bonusEndPanel;
    public CanvasGroup bonusEndCanvasGroup;
    public TMP_Text bonusEndTitleTMP;
    public TMP_Text bonusEndWinTMP;
    public Button bonusEndCloseButton;

    private bool bonusEndCloseRequested = false;

    public float bonusEndShowTime = 1.4f;
    public float bonusEndFadeTime = 0.25f;

    public CanvasGroup bonusStartCanvasGroup;
    public TMP_Text bonusStartTMP;
    public float bonusStartShowTime = 1.2f;
    public float bonusStartFadeTime = 0.25f;

    // SahneBaglamaServisi.IBaglamaHedefi — Inspector ref'leri korunur; sadece null olanlar servis tarafından doldurulur
    Button SahneBaglamaServisi.IBaglamaHedefi.CevirButon { get => cevirButon; set => cevirButon = value; }
    TMP_Text SahneBaglamaServisi.IBaglamaHedefi.BakiyeText { get => bakiyeText; set => bakiyeText = value; }
    TMP_Text SahneBaglamaServisi.IBaglamaHedefi.BahisText { get => bahisText; set => bahisText = value; }
    TMP_Text SahneBaglamaServisi.IBaglamaHedefi.HakText { get => hakText; set => hakText = value; }
    TMP_Text SahneBaglamaServisi.IBaglamaHedefi.KazancText { get => kazancText; set => kazancText = value; }
    TMP_Text SahneBaglamaServisi.IBaglamaHedefi.CarpanText { get => carpanText; set => carpanText = value; }
    TMP_Text SahneBaglamaServisi.IBaglamaHedefi.CarpanOlasilikValueText { get => carpanOlasilikValueText; set => carpanOlasilikValueText = value as TextMeshProUGUI; }
    TMP_Text SahneBaglamaServisi.IBaglamaHedefi.CarpanMaxAdetValueText { get => carpanMaxAdetValueText; set => carpanMaxAdetValueText = value as TextMeshProUGUI; }
    Button SahneBaglamaServisi.IBaglamaHedefi.BakiyeYukleButon { get => bakiyeYukleButon; set => bakiyeYukleButon = value; }
    GameObject SahneBaglamaServisi.IBaglamaHedefi.BakiyeYuklePanel { get => bakiyeYuklePanel; set => bakiyeYuklePanel = value; }
    TMP_InputField SahneBaglamaServisi.IBaglamaHedefi.BakiyeYukleInput { get => bakiyeYukleInput; set => bakiyeYukleInput = value; }
    Button SahneBaglamaServisi.IBaglamaHedefi.BakiyeYukleOnayButon { get => bakiyeYukleOnayButon; set => bakiyeYukleOnayButon = value; }
    Button SahneBaglamaServisi.IBaglamaHedefi.BakiyeYukleIptalButon { get => bakiyeYukleIptalButon; set => bakiyeYukleIptalButon = value; }
    Button SahneBaglamaServisi.IBaglamaHedefi.ParaCekButon { get => paraCekButon; set => paraCekButon = value; }
    GameObject SahneBaglamaServisi.IBaglamaHedefi.ParaCekPanel { get => paraCekPanel; set => paraCekPanel = value; }
    TMP_InputField SahneBaglamaServisi.IBaglamaHedefi.ParaCekInput { get => paraCekInput; set => paraCekInput = value; }
    Button SahneBaglamaServisi.IBaglamaHedefi.ParaCekOnayButon { get => paraCekOnayButon; set => paraCekOnayButon = value; }
    Button SahneBaglamaServisi.IBaglamaHedefi.ParaCekIptalButon { get => paraCekIptalButon; set => paraCekIptalButon = value; }
    Button SahneBaglamaServisi.IBaglamaHedefi.BonusSatinAlButon { get => bonusSatinAlButon; set => bonusSatinAlButon = value; }
    GameObject SahneBaglamaServisi.IBaglamaHedefi.BonusBuyConfirmPanel { get => bonusBuyConfirmPanel; set => bonusBuyConfirmPanel = value; }
    TMP_Text SahneBaglamaServisi.IBaglamaHedefi.BonusBuyConfirmCostText { get => bonusBuyConfirmCostText; set => bonusBuyConfirmCostText = value; }
    CanvasGroup SahneBaglamaServisi.IBaglamaHedefi.BonusBuyConfirmCanvasGroup { get => bonusBuyConfirmCanvasGroup; set => bonusBuyConfirmCanvasGroup = value; }
    Button SahneBaglamaServisi.IBaglamaHedefi.BonusBuyYesButton { get => bonusBuyYesButton; set => bonusBuyYesButton = value; }
    Button SahneBaglamaServisi.IBaglamaHedefi.BonusBuyNoButton { get => bonusBuyNoButton; set => bonusBuyNoButton = value; }
    GameObject SahneBaglamaServisi.IBaglamaHedefi.BonusStartPanel { get => bonusStartPanel; set => bonusStartPanel = value; }
    GameObject SahneBaglamaServisi.IBaglamaHedefi.BonusEndPanel { get => bonusEndPanel; set => bonusEndPanel = value; }
    CanvasGroup SahneBaglamaServisi.IBaglamaHedefi.BonusEndCanvasGroup { get => bonusEndCanvasGroup; set => bonusEndCanvasGroup = value; }
    CanvasGroup SahneBaglamaServisi.IBaglamaHedefi.BonusStartCanvasGroup { get => bonusStartCanvasGroup; set => bonusStartCanvasGroup = value; }

    // IDonusAkisBaglami — state ve servis erişimi (arayüz DonusAkisServisi.cs içinde)
    UIServisi IDonusAkisBaglami.UIServisi => _uiServisi;
    IzgaraServisi IDonusAkisBaglami.IzgaraServisi => _izgaraServisi;
    OdemeServisi IDonusAkisBaglami.OdemeServisi => _odemeServisi;
    AnimasyonServisi IDonusAkisBaglami.AnimasyonServisi => _animasyonServisi;
    TumbleServisi IDonusAkisBaglami.TumbleServisi => _tumbleServisi;
    CarpanServisi IDonusAkisBaglami.CarpanServisi => _carpanServisi;
    EkonomiServisi IDonusAkisBaglami.EkonomiServisi => _ekonomiServisi;
    LogServisi IDonusAkisBaglami.DonusKayitServisi => _logServisi;
    SenaryoServisi IDonusAkisBaglami.SenaryoServisi => _senaryoServisi;
    HizVeSesServisi IDonusAkisBaglami.HizVeSesServisi => _hizVeSesServisi;
    bool IDonusAkisBaglami.SpinCalisiyor { get => spinCalisiyor; set => spinCalisiyor = value; }
    bool IDonusAkisBaglami.BonusAktif { get => bonusAktif; set => bonusAktif = value; }
    int IDonusAkisBaglami.BonusHakKalan { get => bonusHakKalan; set => bonusHakKalan = value; }
    int IDonusAkisBaglami.BonusKazanc { get => bonusKazanc; set => bonusKazanc = value; }
    int IDonusAkisBaglami.OturumKazanc { get => oturumKazanc; set => oturumKazanc = value; }
    int IDonusAkisBaglami.BonusPendingOdemeTL { get => _bonusPendingOdemeTL; set => _bonusPendingOdemeTL = value; }
    bool IDonusAkisBaglami.SpinKazanciOturumaEklendi { get => _spinKazanciOturumaEklendi; set => _spinKazanciOturumaEklendi = value; }
    int IDonusAkisBaglami.SpinKazancHam { get => spinKazancHam; set => spinKazancHam = value; }
    int IDonusAkisBaglami.TumbleToplamKazanc { get => tumbleToplamKazanc; set => tumbleToplamKazanc = value; }
    int IDonusAkisBaglami.SonSpinKazanci { get => sonSpinKazanci; set => sonSpinKazanci = value; }
    int IDonusAkisBaglami.SpinPrevBakiye => _spinPrevBakiye;
    int IDonusAkisBaglami.SpinBahisTL => _spinBahisTL;
    float IDonusAkisBaglami.BonusSpinBekleme => bonusSpinBekleme;
    int IDonusAkisBaglami.SonSpinKazancHamGoster { set => sonSpinKazancHamGoster = value; }
    int IDonusAkisBaglami.SonSpinCarpanGoster { set => sonSpinCarpanGoster = value; }
    int IDonusAkisBaglami.SonSpinKazancToplamGoster { set => sonSpinKazancToplamGoster = value; }
    int IDonusAkisBaglami.BonusOturumOdenenToplamTL { get => _bonusOturumOdenenToplamTL; set => _bonusOturumOdenenToplamTL = value; }
    int IDonusAkisBaglami.BonusMaxOdemeTL => _bonusMaxOdemeTL;
    int IDonusAkisBaglami.BonusOdenenTL => _bonusOdenenTL;
    bool IDonusAkisBaglami.BonusBudgetAktif => bonusBudgetAktif;
    int IDonusAkisBaglami.BonusBudgetKalanTL => _bonusBudgetKalanTL;
    int[,] IDonusAkisBaglami.Grid => grid;
    int IDonusAkisBaglami.Satir => satir;
    int IDonusAkisBaglami.Sutun => sutun;
    void IDonusAkisBaglami.UI_CarpanSifirla() => UI_CarpanSifirla();
    void IDonusAkisBaglami.CarpanUretVeBirik() => CarpanUretVeBirik();
    void IDonusAkisBaglami.CarpanlariDoluGriddeUygula() => _carpanYerlestirmeServisi?.CarpanlariDoluGriddeUygula();
    void IDonusAkisBaglami.BaslatBonus() => BaslatBonus();
    IEnumerator IDonusAkisBaglami.ScatterBuyutEfekti() => ScatterBuyutEfekti();
    IEnumerator IDonusAkisBaglami.ShowBonusEndMessage(int bonusToplamKazanc) => ShowBonusEndMessage(bonusToplamKazanc);
    void IDonusAkisBaglami.SetSpinIconRotate(bool rotate) { if (spinIcon != null) spinIcon.SetRotate(rotate); }
    void IDonusAkisBaglami.SetOturumKazancTextActive(bool active) { if (oturumKazancText != null) oturumKazancText.gameObject.SetActive(active); }
    void IDonusAkisBaglami.NormalOyunMusicPlay() { if (normalOyunMusic != null && normalOyunMusic.clip != null && !normalOyunMusic.isPlaying) normalOyunMusic.Play(); }
    void IDonusAkisBaglami.NormalOyunMusicUnPause() { if (normalOyunMusic != null) normalOyunMusic.UnPause(); }

    // IOyunUIGuncellemeBaglami
    TMP_Text IOyunUIGuncellemeBaglami.BakiyeText => bakiyeText;
    TMP_Text IOyunUIGuncellemeBaglami.BahisText => bahisText;
    TMP_Text IOyunUIGuncellemeBaglami.HakText => hakText;
    TMP_Text IOyunUIGuncellemeBaglami.OturumKazancText => oturumKazancText;
    TMP_Text IOyunUIGuncellemeBaglami.KazancText => kazancText;
    TMP_Text IOyunUIGuncellemeBaglami.CarpanText => carpanText;
    TMP_Text IOyunUIGuncellemeBaglami.BonusSatinAlText => bonusSatinAlText;
    Button IOyunUIGuncellemeBaglami.CevirButon => cevirButon;
    Button IOyunUIGuncellemeBaglami.ParaCekButon => paraCekButon;
    Button IOyunUIGuncellemeBaglami.BakiyeYukleButon => bakiyeYukleButon;
    Button IOyunUIGuncellemeBaglami.BahisArttirButon => bahisArttirButon;
    Button IOyunUIGuncellemeBaglami.BahisAzaltButon => bahisAzaltButon;
    Button IOyunUIGuncellemeBaglami.BonusSatinAlButon => bonusSatinAlButon;
    Button IOyunUIGuncellemeBaglami.ParaCekOnayButon => paraCekOnayButon;
    Button IOyunUIGuncellemeBaglami.ParaCekIptalButon => paraCekIptalButon;
    Button IOyunUIGuncellemeBaglami.BakiyeYukleOnayButon => bakiyeYukleOnayButon;
    Button IOyunUIGuncellemeBaglami.BakiyeYukleIptalButon => bakiyeYukleIptalButon;
    GameObject IOyunUIGuncellemeBaglami.ParaCekPanel => paraCekPanel;
    GameObject IOyunUIGuncellemeBaglami.BakiyeYuklePanel => bakiyeYuklePanel;
    GameObject IOyunUIGuncellemeBaglami.BonusSatinAlRoot => bonusSatinAlRoot;
    int IOyunUIGuncellemeBaglami.GetBakiye() => _ekonomiServisi != null ? _ekonomiServisi.Bakiye : 0;
    int IOyunUIGuncellemeBaglami.GetBahis() => _ekonomiServisi != null ? _ekonomiServisi.Bahis : 0;
    int IOyunUIGuncellemeBaglami.GetBahisMin() => bahisMin;
    int IOyunUIGuncellemeBaglami.GetBahisMax() => bahisMax;
    bool IOyunUIGuncellemeBaglami.GetBonusAktif() => bonusAktif;
    bool IOyunUIGuncellemeBaglami.GetSpinCalisiyor() => spinCalisiyor;
    int IOyunUIGuncellemeBaglami.GetBonusHakKalan() => bonusHakKalan;
    int IOyunUIGuncellemeBaglami.GetOturumKazanc() => oturumKazanc;
    int IOyunUIGuncellemeBaglami.GetSonSpinKazanci() => sonSpinKazanci;
    bool IOyunUIGuncellemeBaglami.GetSpinKazanciOturumaEklendi() => _spinKazanciOturumaEklendi;
    int IOyunUIGuncellemeBaglami.GetSonSpinKazancHamGoster() => sonSpinKazancHamGoster;
    int IOyunUIGuncellemeBaglami.GetSonSpinCarpanGoster() => sonSpinCarpanGoster;
    int IOyunUIGuncellemeBaglami.GetSonSpinKazancToplamGoster() => sonSpinKazancToplamGoster;
    int IOyunUIGuncellemeBaglami.GetBonusMaliyeti() => Mathf.Max(0, _ekonomiServisi != null ? _ekonomiServisi.Bahis : 0) * Mathf.Max(1, bonusSatinAlCarpani);
    void IOyunUIGuncellemeBaglami.RefreshCarpanDisplay() => UI_CarpanGuncelle();
    void IOyunUIGuncellemeBaglami.ShowParaCekPanel() => _uiServisi?.ShowParaCekPanel();
    void IOyunUIGuncellemeBaglami.HideParaCekPanel() => _uiServisi?.HideParaCekPanel();
    void IOyunUIGuncellemeBaglami.ShowBakiyeYuklePanel() => _uiServisi?.ShowBakiyeYuklePanel();
    void IOyunUIGuncellemeBaglami.HideBakiyeYuklePanel() => _uiServisi?.HideBakiyeYuklePanel();
    void IOyunUIGuncellemeBaglami.OnParaCekOnay() => _ekonomiServisi?.OnParaCekOnay();
    void IOyunUIGuncellemeBaglami.OnBakiyeYukleOnay() => _ekonomiServisi?.OnBakiyeYukleOnay();

    // IScatterEfektBaglami
    int[,] IScatterEfektBaglami.Grid => grid;
    int IScatterEfektBaglami.ScatterIndex => _scatterIndexCache;
    int IScatterEfektBaglami.Sutun => sutun;
    int IScatterEfektBaglami.Satir => satir;
    int IScatterEfektBaglami.XYToIndex(int x, int y) => _izgaraServisi != null ? _izgaraServisi.XYToIndex(x, y) : -1;
    Image[] IScatterEfektBaglami.Hucreler => hucreler;
    float IScatterEfektBaglami.ScatterScaleUp => scatterScaleUp;
    float IScatterEfektBaglami.ScatterAnimDuration => scatterAnimDuration;

    // ITumbleAkisBaglami
    int ITumbleAkisBaglami.GetMinClusterSize() => minClusterSize;
    bool ITumbleAkisBaglami.GetBonusAktif() => bonusAktif;
    int ITumbleAkisBaglami.GetBonusRemainingPayableTL() => _senaryoServisi != null ? _senaryoServisi.GetBonusRemainingPayableTL() : int.MaxValue;
    int ITumbleAkisBaglami.GetCurrentMultiplierInt() => _carpanServisi != null ? _carpanServisi.GetCurrentMultiplierInt() : 1;
    long ITumbleAkisBaglami.GetCurrentMultiplier() => _carpanServisi != null ? _carpanServisi.GetCurrentMultiplier() : 1L;
    int ITumbleAkisBaglami.GetSpinKazancHam() => spinKazancHam;
    void ITumbleAkisBaglami.AddSpinKazancHam(int delta) { spinKazancHam += delta; }
    int ITumbleAkisBaglami.CalculateWinForRemoved(List<Vector2Int> removed) => _tumbleServisi != null ? _tumbleServisi.CalculateWinForRemoved(removed) : 0;
    List<Vector2Int> ITumbleAkisBaglami.FindClustersToRemove(int minSize) => _tumbleServisi != null ? _tumbleServisi.FindClustersToRemove(minSize) : new List<Vector2Int>();
    void ITumbleAkisBaglami.CarpanUretVeBirik() => CarpanUretVeBirik();
    void ITumbleAkisBaglami.AddTumbleToplamKazanc(int delta) { tumbleToplamKazanc += delta; }
    void ITumbleAkisBaglami.SetSonSpinKazancHamGoster(int value) { sonSpinKazancHamGoster = value; }
    void ITumbleAkisBaglami.SetSonSpinCarpanGoster(int value) { sonSpinCarpanGoster = value; }
    void ITumbleAkisBaglami.SetSonSpinKazancToplamGoster(int value) { sonSpinKazancToplamGoster = value; }
    void ITumbleAkisBaglami.SetSonSpinKazanci(int value) { sonSpinKazanci = value; }
    int ITumbleAkisBaglami.MulClampInt(int ham, long multiplier) => _carpanServisi != null ? _carpanServisi.MulClampInt(ham, multiplier) : ham;
    void ITumbleAkisBaglami.UI_Guncelle() => _uiServisi?.UI_Guncelle();
    void ITumbleAkisBaglami.PlayTumbleSfx() => _hizVeSesServisi?.PlayTumbleSfx(tumblePopClip, ref _lastTumblePopTime, tumblePopMinInterval, 1f);
    void ITumbleAkisBaglami.ClearGridCells(List<Vector2Int> toRemove)
    {
        if (toRemove == null || grid == null || carpanDegerGrid == null) return;
        for (int i = 0; i < toRemove.Count; i++)
        {
            int x = toRemove[i].x, y = toRemove[i].y;
            grid[x, y] = -1;
            carpanDegerGrid[x, y] = 0;
            int ridx = _izgaraServisi != null ? _izgaraServisi.XYToIndex(x, y) : -1;
            if (carpanDegerByCellIndex != null && ridx >= 0 && ridx < carpanDegerByCellIndex.Length)
                carpanDegerByCellIndex[ridx] = 0;
        }
    }
    IEnumerator ITumbleAkisBaglami.AnimateCarpanSisme() => _animasyonServisi != null ? _animasyonServisi.AnimateCarpanSisme() : null;
    IEnumerator ITumbleAkisBaglami.AnimatePop(List<Vector2Int> cells) => _tumbleServisi != null ? _tumbleServisi.AnimatePop(cells) : null;
    IEnumerator ITumbleAkisBaglami.CollapseRefillAndAnimate() => _cokmeAkisServisi != null ? _cokmeAkisServisi.CokmeDoldurVeCanlandir() : null;
    float ITumbleAkisBaglami.GetBetweenStepsDelay() => betweenStepsDelay;
    Coroutine ITumbleAkisBaglami.RunCoroutine(IEnumerator enumerator) => enumerator != null ? StartCoroutine(enumerator) : null;

    int ICokmeAkisBaglami.GetSutun() => sutun;
    int ICokmeAkisBaglami.GetSatir() => satir;
    int[,] ICokmeAkisBaglami.GetGrid() => grid;
    int[,] ICokmeAkisBaglami.GetCarpanDegerGrid() => carpanDegerGrid;
    Vector2[] ICokmeAkisBaglami.GetCellPos() => cellPos;
    RectTransform[] ICokmeAkisBaglami.GetCellRT() => cellRT;
    float ICokmeAkisBaglami.GetSpawnFromTopOffset() => spawnFromTopOffset;
    float ICokmeAkisBaglami.GetFallDuration() => fallDuration;
    bool ICokmeAkisBaglami.GetBonusAktif() => bonusAktif;
    int ICokmeAkisBaglami.GetCarpanSembol() => CARPAN_SEMBOL;
    IzgaraServisi ICokmeAkisBaglami.GetIzgaraServisi() => _izgaraServisi;
    TumbleServisi ICokmeAkisBaglami.GetTumbleServisi() => _tumbleServisi;
    CarpanServisi ICokmeAkisBaglami.GetCarpanServisi() => _carpanServisi;
    SenaryoServisi ICokmeAkisBaglami.GetSenaryoServisi() => _senaryoServisi;
    void ICokmeAkisBaglami.ApplyNewGridAndSync(int[,] newGrid, int[,] newCarpanGrid) => ApplyNewGridAndSync(newGrid, newCarpanGrid);

    private void ApplyNewGridAndSync(int[,] newGrid, int[,] newCarpanGrid)
    {
        grid = newGrid;
        carpanDegerGrid = newCarpanGrid;
        _tumbleServisi?.SetGrid(grid);
        _izgaraServisi?.SetGrid(grid);
        _izgaraServisi?.SetCarpanDegerGrid(carpanDegerGrid);
        if (carpanDegerByCellIndex == null || carpanDegerByCellIndex.Length != sutun * satir)
            carpanDegerByCellIndex = new int[sutun * satir];
        for (int yy = 0; yy < satir; yy++)
        {
            for (int xx = 0; xx < sutun; xx++)
            {
                int cidx2 = _izgaraServisi != null ? _izgaraServisi.XYToIndex(xx, yy) : 0;
                if (cidx2 < 0 || cidx2 >= carpanDegerByCellIndex.Length) continue;
                carpanDegerByCellIndex[cidx2] = (grid[xx, yy] == CARPAN_SEMBOL) ? carpanDegerGrid[xx, yy] : 0;
            }
        }
    }

    [Header("Hücreler (SlotGrid altındaki 30 Image)")]
    [Tooltip("Boş bırakırsan slotGridRoot altından otomatik toplanır.")]
    public Image[] hucreler;

    [Tooltip("SlotGrid objesini buraya ver (GridLayoutGroup bunun üstünde).")]
    public Transform slotGridRoot;

    [Header("Çarpan Ayarları")]
    public bool carpanUretimiAktif = true;
    public bool carpanSadeceBonus = false;
    [Range(0f, 1f)] public float carpanUretimOlasiligi = 0.15f;
    [Range(1, 10)] public int maxCarpanAdedi = 2;
    [Range(1, 10)] public int carpanHavuzu = 10; // havuz büyüklüğü
    public int zorlaSiradakiCarpan = 0;

[Header("Çarpan Görseli (Sweet Bonanza tarzı)")]
[Tooltip("Çarpan jeton/bomba sprite'ını buraya ver (arka plan transparan).")]
public Sprite carpanSembolSprite;

[Tooltip("Çarpan overlay boyutu (px).")]
public Vector2 carpanOverlaySize = new Vector2(110f, 110f);

[Tooltip("Overlay üzerindeki x2/x5 yazı boyutu.")]
public int carpanOverlayFontSize = 36;

    [Header("Çarpan Yazı Görünümü")]
    public Color carpanYaziRengi = Color.white;
    public Color carpanYaziDisCizgiRengi = new Color(0f, 0f, 0f, 1f);
    [Range(0f, 1f)] public float carpanYaziDisCizgiKalinlik = 0.35f;
    public bool carpanYaziKalin = true;
    public bool carpanYaziGolge = true;
    public Color carpanYaziGolgeRengi = new Color(0f, 0f, 0f, 0.85f);
    public Vector2 carpanYaziGolgeOffset = new Vector2(2f, -2f);

[Tooltip("Yazının konum offseti.")]
public Vector2 carpanOverlayTextOffset = new Vector2(0f, -6f);

[Tooltip("Overlay düşme animasyonu başlangıç Y offset.")]
public float carpanOverlayDropStartYOffset = 250f;

[Tooltip("Overlay düşme animasyonu süresi.")]
public float carpanOverlayDropDuration = 0.18f;


    // -------------------------
    // internal state
    // -------------------------
    private int[,] grid;
    // Çarpan sembolü artık grid içinde gerçek bir sembol gibi durur (meyve yerine düşer).
    // grid hücresinde -1 = boş, -2 = çarpan sembolü, 0..N-1 = normal semboller
    private const int CARPAN_SEMBOL = -2;
    private int[,] carpanDegerGrid;
    // Çarpan değerleri bazen (özellikle tumble olmadan) grid yeniden render edilirken sıfırlanabiliyor.
    // Bu yüzden hücre index bazlı yedek tutuyoruz; render sırasında 0 görürsek buradan geri yükleriz.
    private int[] carpanDegerByCellIndex;
 // CARPAN_SEMBOL olan hücrelerin çarpan değeri (x2, x5...)
    private TextMeshProUGUI[] carpanHücreTextleri; // her hücre için x2 yazısı

    private bool spinCalisiyor = false;
    private bool bonusAktif = false;
    private int bonusHakKalan = 0;

    private int normalKazanc = 0;
    private int bonusKazanc = 0;
    private int sonSpinKazanci = 0; // ekranda gösterilecek: son spin kazancı
    private int sonSpinKazancHamGoster = 0;
    private int sonSpinCarpanGoster = 1;
    private int sonSpinKazancToplamGoster = 0;

    // UI pos cache
    private Vector2[] cellPos;
    private RectTransform[] cellRT;
    private Behaviour layoutToDisable;

    /// <summary>Tek kaynak: Sahnedeki BonusAyarlari / KasaYoneticisi varsa değerleri OY alanlarına kopyala. Bahis limitleri OY default (bahisMin/Max/Adim) ve EkonomiServisi.SetBahisLimits ile verilir.</summary>
    private void SyncFromAyarClassesIfPresent()
    {
        var bonus = FindFirstObjectByType<BonusAyarlari>(FindObjectsInactive.Include);
        _bonusAyarlari = bonus;
        if (bonus != null)
        {
            bonusHakBaslangic = bonus.BonusHakBaslangic;
            bonusSpinBekleme = bonus.BonusSpinBekleme;
            bonusSatinAlCarpani = bonus.BonusSatinAlCarpani;
            bonusBudgetAktif = bonus.BonusBudgetAktif;
            bonusBudgetHavuzOran = bonus.BonusBudgetHavuzOran;
            bonusBudgetMinTL = bonus.BonusBudgetMinTL;
            bonusBudgetMaxTL = bonus.BonusBudgetMaxTL;
            bonusOtoZorlukAktif = bonus.BonusOtoZorlukAktif;
            bonusMinCluster_Easy = bonus.BonusMinCluster_Easy;
            bonusMinCluster_Hard = bonus.BonusMinCluster_Hard;
            scatterChanceNormal = bonus.ScatterChanceNormal;
            scatterChanceBonus = bonus.ScatterChanceBonus;
            scatterEsik = bonus.ScatterEsik;
            scatterScaleUp = bonus.ScatterScaleUp;
            scatterAnimDuration = bonus.ScatterAnimDuration;
        }
        var kasaObj = kasa ?? FindFirstObjectByType<KasaYoneticisi>(FindObjectsInactive.Include);
        if (kasaObj != null)
        {
            bonusBudgetAktif = kasaObj.BonusBudgetAktif;
            bonusBudgetHavuzOran = kasaObj.BonusBudgetHavuzOran;
            bonusBudgetMinTL = kasaObj.BonusBudgetMinTL;
            bonusBudgetMaxTL = kasaObj.BonusBudgetMaxTL;
            bonusMaxOdemeHavuzOrani = kasaObj.BonusMaxOdemeHavuzOrani;
            kasaBazliDengeAktif = kasaObj.KasaBazliDengeAktif;
            minClusterSize_HavuzBos = kasaObj.MinClusterSize_HavuzBos;
            minClusterSize_HavuzDolu = kasaObj.MinClusterSize_HavuzDolu;
            minClusterSize_HavuzAz = kasaObj.MinClusterSize_HavuzAz;
            havuzAzEsik01 = kasaObj.HavuzAzEsik01;
            havuzDoluEsik01 = kasaObj.HavuzDoluEsik01;
            bonusOtoZorlukAktif = kasaObj.BonusOtoZorlukAktif;
            bonusMinCluster_Easy = kasaObj.BonusMinCluster_Easy;
            bonusMinCluster_Hard = kasaObj.BonusMinCluster_Hard;
        }
    }

    void Start()
    {
        SyncFromAyarClassesIfPresent();
        _oyunBootstrapServisi = new OyunBootstrapServisi();
        _oyunBootstrapServisi.SetBaglam(this);
        _oyunBootstrapServisi.Calistir();
    }

    void IOyunBootstrapBaglami.BootstrapMantiginiCalistir()
    {
        _logServisi = new LogServisi();
        _ekonomiServisi = new EkonomiServisi();
        _uiServisi = new UIServisi();
        _zorlukServisi = new ZorlukServisi();
        _zorlukServisi.SetBaglam(this);
        _oyunUIGuncellemeServisi = new OyunUIGuncellemeServisi();
        _oyunUIGuncellemeServisi.SetBaglam(this);
        _scatterEfektServisi = new ScatterEfektServisi();
        _scatterEfektServisi.SetBaglam(this);
        _uiServisi.SetUIGuncelleImpl(() => _oyunUIGuncellemeServisi?.RefreshAllUI());
        _uiServisi.SetButonDurumuImpl(acik => _oyunUIGuncellemeServisi?.SetButtonsInteractable(acik));
        _uiServisi.SetShowParaCekPanelImpl(ShowParaCekPanel);
        _uiServisi.SetHideParaCekPanelImpl(HideParaCekPanel);
        _uiServisi.SetShowBakiyeYuklePanelImpl(ShowBakiyeYuklePanel);
        _uiServisi.SetHideBakiyeYuklePanelImpl(HideBakiyeYuklePanel);
        _uiServisi.SetCloseMoneyPanelsImpl(CloseMoneyPanels);
        _uiServisi.SetShowBonusBuyConfirmPanelImpl(ShowBonusBuyConfirmPanel);
        _uiServisi.SetHideBonusBuyConfirmPanelImpl(HideBonusBuyConfirmPanel);
        _sahneBaglamaServisi = new SahneBaglamaServisi();
        _uiServisi.SetUIAutoBaglaGerekirseImpl(() => _sahneBaglamaServisi.BindIfNeeded(transform, this));
        _uiServisi.SetResolveMoneyUIRefsIfMissingImpl(() => _sahneBaglamaServisi.BindIfNeeded(transform, this));
        _uiServisi.SetWireParaCekUIImpl(() => _oyunUIGuncellemeServisi?.WireMoneyPanelsIfNeeded());
        _uiServisi.SetWireBakiyeYukleUIImpl(() => _oyunUIGuncellemeServisi?.WireMoneyPanelsIfNeeded());

        _odemeServisi = new OdemeServisi();
        _odemeServisi.SetGetHavuzTL(() => kasa != null ? kasa.odulHavuzuTL : 0L);
        _odemeServisi.SetParaGirisiBolVeEkle(tl => { if (kasa != null) kasa.ParaGirisi_BolVeEkle(tl); });
        _odemeServisi.SetOdemeYapOdulHavuzundan(istenen => kasa != null ? kasa.OdemeYap_OdulHavuzundan(istenen) : 0);

        _senaryoServisi = new SenaryoServisi();
        _senaryoServisi.SetSetZorlukImpl(SetZorluk);
        _senaryoServisi.SetBiasMultiplierImpl(BiasMultiplier);
        _senaryoServisi.SetGetScatterChanceImpl(GetScatterChanceFor);
        _senaryoServisi.SetGetScatterEsikImpl(() => scatterEsik);
        _senaryoServisi.SetGetMaxScatterPerSpinImpl(() => maxScatterPerSpin);
        _senaryoServisi.SetGetCarpanUretimOlasiligiImpl(() => carpanUretimOlasiligi);
        _senaryoServisi.SetGetMaxCarpanAdediImpl(() => maxCarpanAdedi);
        _senaryoServisi.SetIsCarpanSadeceBonusImpl(() => carpanSadeceBonus);
        _senaryoServisi.SetIsCarpanUretimiAktifImpl(() => carpanUretimiAktif);
        _senaryoServisi.SetInitBonusBudgetFromHavuzImpl(InitBonusBudgetFromHavuz);
        _senaryoServisi.SetGetBonusRemainingPayableTLImpl(GetBonusRemainingPayableTL);
        _senaryoServisi.SetRecordBonusPaymentImpl(RecordBonusPayment);

        _donusAkisServisi = new DonusAkisServisi();
        _donusAkisServisi.SetBaglam(this);
        _donusAkisServisi.SetRunCoroutine(StartCoroutine);

        _donusServisi = new DonusServisi();
        _donusServisi.SetSpinButonImpl(SpinButonImpl);
        _donusServisi.SetNormalSpinAkisiImpl(() => _donusAkisServisi.NormalSpinAkisi());
        _donusServisi.SetBaslatBonusImpl(BaslatBonus);
        _donusServisi.SetBonusBaslangicAkisiImpl(BonusBaslangicAkisi);
        _donusServisi.SetBonusDongusuImpl(() => _donusAkisServisi.BonusDongusu());
        _donusServisi.SetShowBonusStartMessageImpl(ShowBonusStartMessage);
        _donusServisi.SetShowBonusEndMessageImpl(ShowBonusEndMessage);

        _izgaraServisi = new IzgaraServisi();
        _izgaraServisi.SetGridDimensions(satir, sutun);
        _scatterIndexCache = (tumbleAyarlari != null) ? tumbleAyarlari.ScatterIndex : 7;
        // Sembol listesinde 9+ sembol varsa silah genelde index 8'dedir; 7 ile sayarsak bonus tetiklenmez.
        if (_scatterIndexCache == 7 && sembolSpriteListesi != null && sembolSpriteListesi.Count > 8)
            _scatterIndexCache = 8;
        _izgaraServisi.SetScatterSpriteIndex(_scatterIndexCache);
        _izgaraServisi.SetSembolSpriteListesi(sembolSpriteListesi);
        _izgaraServisi.SetCarpanSembolSprite(carpanSembolSprite);
        _izgaraServisi.SetSlotGridRoot(slotGridRoot);
        _izgaraServisi.SetCalculateWinForRemoved(removed => _tumbleServisi != null ? _tumbleServisi.CalculateWinForRemoved(removed) : 0);
        _izgaraServisi.SetGetBonusAktif(() => bonusAktif);
        _izgaraServisi.SetGetEffectiveFillLimit(limit => bonusMaxOdemeHavuzOrani <= 0f || !bonusAktif ? limit : Mathf.Min(limit, Mathf.Max(0, _bonusMaxOdemeTL - _bonusOdenenTL)));
        _izgaraServisi.SetGetScatterChance(_senaryoServisi.GetScatterChance);
        _izgaraServisi.SetGetMaxScatterPerSpin(_senaryoServisi.GetMaxScatterPerSpin);
        _izgaraServisi.SetBiasMultiplier(_senaryoServisi.BiasMultiplier);
        _izgaraServisi.SetGetHardBias01(() => _hardBias01);

        _cokmeAkisServisi = new CokmeAkisServisi();
        _cokmeAkisServisi.SetBaglam(this);
        _tumbleAkisServisi = new TumbleAkisServisi();
        _tumbleAkisServisi.SetBaglam(this);
        _tumbleServisi = new TumbleServisi();
        _tumbleServisi.SetTumbleLoopImpl(onKazanc => _tumbleAkisServisi.TumbleLoop(onKazanc));
        _tumbleServisi.SetGetBonusAktif(() => bonusAktif);
        _tumbleServisi.SetGetBonusRemainingPayableTL(() => _senaryoServisi.GetBonusRemainingPayableTL());
        _tumbleServisi.SetScatterSpriteIndex(_scatterIndexCache);
        _tumbleServisi.SetGetCurrentBet(() => _ekonomiServisi != null ? _ekonomiServisi.Bahis : 0);
        _tumbleServisi.SetCalculateWithPayTable(removed => tumbleAyarlari != null ? tumbleAyarlari.CalculateWinWithOwnPayTable(removed, grid, satir, sutun, _ekonomiServisi != null ? _ekonomiServisi.Bahis : 0) : -1);
        _tumbleServisi.SetCollapseRefillAndAnimateImpl(CollapseRefillAndAnimate);
        _animasyonServisi = new AnimasyonServisi();
        _animasyonServisi.SetHucreler(hucreler);
        _animasyonServisi.SetCellPos(cellPos);
        _animasyonServisi.SetDurations(dropDuration, dropStagger, dropStartYOffset, popDuration);
        _animasyonServisi.SetPopParticlePrefab(popParticlePrefab);
        Canvas canvas = GetComponentInParent<Canvas>();
        _animasyonServisi.SetParticleParent(canvas != null ? canvas.transform : transform);
        _animasyonServisi.SetXYToIndex((x, y) => _izgaraServisi != null ? _izgaraServisi.XYToIndex(x, y) : -1);
        _animasyonServisi.SetOnRefreshCarpanTexts(() => _izgaraServisi?.ForceRefreshCarpanTextsFromGrid());
        _korutinServisi = new KorutinServisi();
        _korutinServisi.SetRunner(r => StartCoroutine(r), c => StopCoroutine(c));

        _carpanOverlayServisi = new CarpanOverlayServisi();
        _carpanOverlayServisi.SetCellImages(hucreler);
        _carpanOverlayServisi.SetCarpanSembolSprite(carpanSembolSprite);
        _carpanOverlayServisi.SetOverlaySize(carpanOverlaySize);
        _carpanOverlayServisi.SetOverlayFontSize(carpanOverlayFontSize);
        _carpanOverlayServisi.SetOverlayTextOffset(carpanOverlayTextOffset);
        _carpanOverlayServisi.SetDropStartYOffset(carpanOverlayDropStartYOffset);
        _carpanOverlayServisi.SetDropDuration(carpanOverlayDropDuration);
        _carpanOverlayServisi.SetStartNamedCoroutine((key, coro) => _korutinServisi.StartNamed(key, coro));
        _carpanOverlayServisi.SetStopNamedCoroutine(key => _korutinServisi.StopNamed(key));
        _animasyonServisi.SetGetCarpanOverlays(() =>
        {
            var d = new Dictionary<int, AnimasyonServisi.CarpanOverlayRef>();
            foreach (var kv in _carpanOverlayServisi.AnimasyonIcinOverlayleriAl())
                d[kv.Key] = new AnimasyonServisi.CarpanOverlayRef { rt = kv.Value.rt, tmp = kv.Value.tmp };
            return d;
        });
        _animasyonServisi.SetRunCoroutine(coro => StartCoroutine(coro));
        _tumbleServisi.SetAnimatePopImpl(cells => _animasyonServisi.AnimatePop(cells));

        _izgaraServisi.SetFindClustersToRemove(_tumbleServisi.FindClustersToRemove);

        _carpanServisi = new CarpanServisi();
        _carpanServisi.SetIsCarpanUretimiAktif(() => carpanUretimiAktif);
        _carpanServisi.SetIsCarpanSadeceBonus(() => carpanSadeceBonus);
        _carpanServisi.SetGetCarpanUretimOlasiligi(() => carpanUretimOlasiligi);
        _carpanServisi.SetGetMaxCarpanAdedi(() => maxCarpanAdedi);
        _carpanServisi.SetRollCarpanDegeri(RastgeleCarpan);
        _carpanServisi.SetGetSpinKazancHam(() => spinKazancHam);
        _carpanServisi.SetGetBonusRemainingPayableTL(() => _senaryoServisi.GetBonusRemainingPayableTL());

        _carpanYerlestirmeServisi = new CarpanYerlestirmeServisi();
        _carpanYerlestirmeServisi.SetBaglam(this);

        _ekonomiServisi.SetLogServisi(_logServisi);
        _ekonomiServisi.SetParaCekInput(paraCekInput);
        _ekonomiServisi.SetBakiyeYukleInput(bakiyeYukleInput);
        _ekonomiServisi.SetParaCekUyariText(paraCekUyariText);
        _ekonomiServisi.SetBakiyeYukleUyariText(bakiyeYukleUyariText);
        _ekonomiServisi.SetBahisLimits(bahisMin, bahisMax, bahisAdim);
        _ekonomiServisi.SetCanChangeBet(() => !spinCalisiyor && !bonusAktif);
        _ekonomiServisi.SetOnEconomyChanged(() => { _uiServisi?.UI_Guncelle(); _uiServisi?.ButonDurumu(true); });
        _ekonomiServisi.SetGetCurrentMultiplier(() => _carpanServisi.GetCurrentMultiplier());
        _ekonomiServisi.SetCarpanSifirla(UI_CarpanSifirla);

        _bonusUIServisi = new BonusUIServisi();
        _bonusUIServisi.SetBonusStartPanel(bonusStartPanel);
        _bonusUIServisi.SetBonusBellAudio(bonusBellAudio);
        _bonusUIServisi.SetBonusStartCanvasGroup(bonusStartCanvasGroup);
        _bonusUIServisi.SetBonusStartTMP(bonusStartTMP);
        _bonusUIServisi.SetGetBonusHakKalan(() => bonusHakKalan);
        _bonusUIServisi.SetBonusStartFadeTime(bonusStartFadeTime);
        _bonusUIServisi.SetBonusStartShowTime(bonusStartShowTime);
        _bonusUIServisi.SetBonusEndPanel(bonusEndPanel);
        _bonusUIServisi.SetGetBonusEndCloseRequested(() => bonusEndCloseRequested);
        _bonusUIServisi.SetSetBonusEndCloseRequested(v => bonusEndCloseRequested = v);
        _bonusUIServisi.SetBonusEndSfx(bonusEndSfxSource, bonusEndApplauseClip);
        _bonusUIServisi.SetBonusEndCanvasGroup(bonusEndCanvasGroup);
        _bonusUIServisi.SetBonusEndTitleTMP(bonusEndTitleTMP);
        _bonusUIServisi.SetBonusEndWinTMP(bonusEndWinTMP);
        _bonusUIServisi.SetFormatTL(OyunFormatServisi.FormatTL);
        _bonusUIServisi.SetBonusEndMusicAudio(bonusEndMusicAudio);

        _hizVeSesServisi = new HizVeSesServisi();
        _hizVeSesServisi.SetGetBonusYavasMod(() => bonusYavasMod);
        _hizVeSesServisi.SetGetDurations(() => (popDuration, fallDuration, betweenStepsDelay, bonusSpinBekleme));
        _hizVeSesServisi.SetSetDurations((p, f, b, w) => { popDuration = p; fallDuration = f; betweenStepsDelay = b; bonusSpinBekleme = w; });
        _hizVeSesServisi.SetGetBonusSpeedOverrides(() => (bonusPopDuration, bonusFallDuration, bonusBetweenStepsDelay, bonusSpinBeklemeOverride));
        _hizVeSesServisi.SetGetUnscaledTime(() => Time.unscaledTime);
        _hizVeSesServisi.SetAudioSource(tumbleSfxSource);

        _bonusUIServisi.SetGetBakiye(() => _ekonomiServisi != null ? _ekonomiServisi.Bakiye : 0);
        _bonusUIServisi.SetGetBonusMaliyeti(() => Mathf.Max(0, _ekonomiServisi != null ? _ekonomiServisi.Bahis : 0) * Mathf.Max(1, bonusSatinAlCarpani));
        _bonusUIServisi.SetGetSpinCalisiyor(() => spinCalisiyor);
        _bonusUIServisi.SetGetBonusAktif(() => bonusAktif);
        _bonusUIServisi.SetShowConfirmPanel(cost =>
        {
            if (_bonusAyarlari != null)
                _bonusAyarlari.Goster(cost, _ekonomiServisi.Bakiye, () => _bonusUIServisi.OnYes(), () => _bonusUIServisi.OnNo());
            else if (bonusBuyConfirmPanel != null)
            {
                if (bonusBuyConfirmCostText != null) bonusBuyConfirmCostText.text = $"Maliyet: {OyunFormatServisi.FormatTL(cost)} TL";
                bonusBuyConfirmPanel.SetActive(true);
                if (bonusBuyConfirmCanvasGroup != null) bonusBuyConfirmCanvasGroup.alpha = 1f;
            }
            else
                Debug.LogWarning("Bonus satın al onay UI bulunamadı. BonusAyarlari bileşenini panele ekle ya da BonusBuyConfirmPanel referansını ver.");
        });
        _bonusUIServisi.SetHideConfirmPanel(() =>
        {
            _bonusAyarlari?.Kapat();
            if (bonusBuyConfirmPanel != null) bonusBuyConfirmPanel.SetActive(false);
        });
        _bonusUIServisi.SetOnConfirmed(cost =>
        {
            _odemeServisi?.AddBahisToKasa(cost);
            int prevBakiye = _ekonomiServisi.Bakiye;
            _ekonomiServisi.SubtractBakiyeForBonusBuy(cost);
            _uiServisi?.UI_Guncelle();
            _logServisi?.KayitEkonomi("Bonus Satın Alındı", prevBakiye, _ekonomiServisi.Bakiye, cost, 0, "BONUS_BUY", $"Bonus satın alındı. Maliyet: {OyunFormatServisi.FormatTL(cost)}", cost);
            _donusServisi?.BaslatBonus();
        });

        _logServisi.SetFormatTL(OyunFormatServisi.FormatTL);
        _logServisi.SetOnSpinStart(() =>
        {
            if (GameManager.I != null && GameManager.I.ActivePlayer != null)
                GameManager.I.ActivePlayer.totalSpins += 1;
        });
        _logServisi.SetOnSpinResult((odenen, bahis) =>
        {
            if (GameManager.I != null && GameManager.I.ActivePlayer != null)
            {
                int net = odenen - bahis;
                GameManager.I.ActivePlayer.totalWon += odenen;
                if (net < 0) GameManager.I.ActivePlayer.totalLost += -net;
                GameManager.I.ActivePlayer.totalNet += net;
            }
        });
        _logServisi.SetOnSpinSettled(() => _uiServisi?.UI_Guncelle());

        _adminAyarUIServisi = new AdminAyarUIServisi();
        var zorlukSlider = GetComponentInChildren<Slider>(true);
        if (zorlukSlider != null && zorlukSlider.gameObject.name.ToLower().Contains("zorluk"))
            _adminAyarUIServisi.SetZorlukUI(zorlukSlider, zorlukValueText, v => _senaryoServisi?.SetZorluk(v));
        _adminAyarUIServisi.SetScatterUI(scatterSliderUI, scatterSliderText, v =>
        {
            int yuzde = Mathf.RoundToInt(v);
            yuzde = Mathf.Clamp(yuzde, 0, 100);
            scatterChanceNormal = yuzde / 100f;
            scatterChanceBonus = 0f;
            if (yuzde >= 100) maxScatterPerSpin = 5;
        });
        // CarpanOlasilikValueText / CarpanMaxAdetValueText sabit etiket olarak kalacak; slider değeri yazılmıyor (valueText null).
        _adminAyarUIServisi.SetCarpanOlasilikUI(carpanOlasilikSlider, carpanOlasilikText, null, v =>
        {
            float yuzde = Mathf.Clamp(v, 0f, 100f);
            carpanUretimOlasiligi = yuzde / 100f;
        });
        _adminAyarUIServisi.SetCarpanMaxAdetUI(carpanMaxAdetSlider, carpanMaxAdetText, null, adet =>
        {
            maxCarpanAdedi = Mathf.Clamp(adet, 0, 5);
        });
        // Tek giriş: AdminPanel varsa slider'ları o bağlar; yoksa servis bağlar (çift bağlama yok).
        if (FindObjectOfType<AdminPanel>() == null)
            _adminAyarUIServisi.BindAllAndRefresh();

        // Inspector'u kalabalik yapmadan, bos kalmis UI alanlarini sahneden otomatik bul.
        // (BakiyeYukle / ParaCek / BonusSatinAl tiklanmiyor sorunu genelde referanslar null kaldiginda olur.)
        _uiServisi?.UIAutoBaglaGerekirse();

        // Çarpan ayarları panelindeki sabit etiket metinleri (slider ile değişmesin)
        if (carpanOlasilikValueText != null) carpanOlasilikValueText.text = "Çarpan Düşme Şansı (%)";
        if (carpanMaxAdetValueText != null) carpanMaxAdetValueText.text = "Max Kaç Çarpan Düşsün? (0-5)";

        UygulaCarpanAyarlari();
        _carpanOverlayServisi?.SetCarpanSembolSprite(carpanSembolSprite);
        _carpanOverlayServisi?.SetOverlaySize(carpanOverlaySize);
        _carpanOverlayServisi?.SetOverlayFontSize(carpanOverlayFontSize);
        _carpanOverlayServisi?.SetOverlayTextOffset(carpanOverlayTextOffset);
        _carpanOverlayServisi?.SetDropStartYOffset(carpanOverlayDropStartYOffset);
        _carpanOverlayServisi?.SetDropDuration(carpanOverlayDropDuration);
        _izgaraServisi?.SetCarpanOverlayFontSize(carpanOverlayFontSize);
        _izgaraServisi?.SetCarpanYaziRengi(carpanYaziRengi);
        _izgaraServisi?.SetCarpanYaziKalin(carpanYaziKalin);
        _izgaraServisi?.SetCarpanYaziDisCizgiRengi(carpanYaziDisCizgiRengi);
        _izgaraServisi?.SetCarpanYaziDisCizgiKalinlik(carpanYaziDisCizgiKalinlik);
        _izgaraServisi?.SetCarpanYaziGolge(carpanYaziGolge);
        _izgaraServisi?.SetCarpanYaziGolgeRengi(carpanYaziGolgeRengi);
        _izgaraServisi?.SetCarpanYaziGolgeOffset(carpanYaziGolgeOffset);

        // BONUS SATIN AL ONAY
        if (bonusBuyYesButton != null)
        {
            bonusBuyYesButton.onClick.RemoveListener(OnBonusBuyYes);
            bonusBuyYesButton.onClick.AddListener(OnBonusBuyYes);
        }
        if (bonusBuyNoButton != null)
        {
            bonusBuyNoButton.onClick.RemoveListener(OnBonusBuyNo);
            bonusBuyNoButton.onClick.AddListener(OnBonusBuyNo);
        }

        // Panel ilk kapalı
        if (bonusBuyConfirmPanel != null)
            bonusBuyConfirmPanel.SetActive(false);

        if (bonusSatinAlButon != null)
        {
            bonusSatinAlButon.onClick.RemoveListener(BonusSatinAl);
            bonusSatinAlButon.onClick.AddListener(BonusSatinAl);
        }


        if (normalOyunMusic != null && !normalOyunMusic.isPlaying)
            normalOyunMusic.Play();

        _izgaraBaslatmaServisi = new IzgaraBaslatmaServisi();
        _izgaraBaslatmaServisi.SetBaglam(this);
        StartCoroutine(InitRoutine());
        if (bonusEndCloseButton != null)
        {
            bonusEndCloseButton.onClick.RemoveAllListeners();
            bonusEndCloseButton.onClick.AddListener(() => bonusEndCloseRequested = true);
        }
        // === BAHİS +/- BUTON BAĞLAMA ===
        if (bahisArttirButon != null)
        {
            bahisArttirButon.onClick.RemoveListener(BahisArttir);
            bahisArttirButon.onClick.AddListener(_ekonomiServisi.BahisArttir);
        }

        if (bahisAzaltButon != null)
        {
            bahisAzaltButon.onClick.RemoveListener(BahisAzalt);
            bahisAzaltButon.onClick.AddListener(_ekonomiServisi.BahisAzalt);
        }
        Debug.Log($"[BAHIS HOOK] ArttirButon={(bahisArttirButon != null)} AzaltButon={(bahisAzaltButon != null)}");
        _uiServisi?.ResolveMoneyUIRefsIfMissing();
        _uiServisi?.WireParaCekUI();
        _uiServisi?.WireBakiyeYukleUI();
    }

    private IEnumerator InitRoutine()
    {
        return _izgaraBaslatmaServisi != null ? _izgaraBaslatmaServisi.InitRoutine() : null;
    }

    int IIzgaraBaslatmaBaglami.GetSutun() => sutun;
    int IIzgaraBaslatmaBaglami.GetSatir() => satir;
    List<Sprite> IIzgaraBaslatmaBaglami.GetSembolSpriteListesi() => sembolSpriteListesi;
    IzgaraServisi IIzgaraBaslatmaBaglami.GetIzgaraServisi() => _izgaraServisi;
    TumbleServisi IIzgaraBaslatmaBaglami.GetTumbleServisi() => _tumbleServisi;
    UIServisi IIzgaraBaslatmaBaglami.GetUIServisi() => _uiServisi;
    EkonomiServisi IIzgaraBaslatmaBaglami.GetEkonomiServisi() => _ekonomiServisi;
    CarpanOverlayServisi IIzgaraBaslatmaBaglami.GetCarpanOverlayServisi() => _carpanOverlayServisi;
    AnimasyonServisi IIzgaraBaslatmaBaglami.GetAnimasyonServisi() => _animasyonServisi;
    Image[] IIzgaraBaslatmaBaglami.GetHucreler() => hucreler;
    void IIzgaraBaslatmaBaglami.SetHucreler(Image[] arr) => hucreler = arr;
    Transform IIzgaraBaslatmaBaglami.GetSlotGridRoot() => slotGridRoot;
    void IIzgaraBaslatmaBaglami.SetGrid(int[,] g) => grid = g;
    void IIzgaraBaslatmaBaglami.SetCarpanDegerGrid(int[,] g) => carpanDegerGrid = g;
    void IIzgaraBaslatmaBaglami.SetCarpanDegerByCellIndex(int[] a) => carpanDegerByCellIndex = a;
    void IIzgaraBaslatmaBaglami.SetCellPos(Vector2[] p) => cellPos = p;
    void IIzgaraBaslatmaBaglami.SetCellRT(RectTransform[] r) => cellRT = r;
    void IIzgaraBaslatmaBaglami.SetCarpanHücreTextleri(TextMeshProUGUI[] t) => carpanHücreTextleri = t;
    public void ShowBakiyeYuklePanel()
    {
        _uiServisi?.ResolveMoneyUIRefsIfMissing();
        _uiServisi?.WireBakiyeYukleUI();

        if (bakiyeYukleUyariText != null) bakiyeYukleUyariText.text = "";
        _uiServisi?.CloseMoneyPanels();
        if (bakiyeYukleInput != null) bakiyeYukleInput.text = "";

        if (bakiyeYuklePanel != null)
            bakiyeYuklePanel.SetActive(true);
    }

    public void HideBakiyeYuklePanel()
    {
        if (bakiyeYuklePanel != null)
            bakiyeYuklePanel.SetActive(false);
    }
    public void BonusSatinAl()
    {
        if (_bonusAyarlari == null)
            _bonusAyarlari = FindFirstObjectByType<BonusAyarlari>(FindObjectsInactive.Include);
        _bonusUIServisi?.BonusSatinAlRequested();
    }

    public void BonusSatinAlOnayla() => _bonusUIServisi?.OnYes();
    public void BonusSatinAlIptal() => _bonusUIServisi?.OnNo();

    private void ShowBonusBuyConfirmPanel(int cost) => _bonusUIServisi?.ShowBonusBuyConfirmPanel(cost);
    private void HideBonusBuyConfirmPanel() => _bonusUIServisi?.HideBonusBuyConfirmPanel();

    private void OnBonusBuyYes() => _bonusUIServisi?.OnYes();
    private void OnBonusBuyNo() => _bonusUIServisi?.OnNo();


   

    public void BahisArttir() => _ekonomiServisi?.BahisArttir();
    public void BahisAzalt() => _ekonomiServisi?.BahisAzalt();

    // ==========================
    // BUTTON / SPIN
    // ==========================
    
    // YENİ: Spin başında ödenebilir tutarı hesapla
    private int _spinOdenebilirLimit = 0;
    
    public void SpinButon()
    {
        _donusServisi?.SpinButon();
    }

    private void SpinButonImpl()
    {
        if (bonusAktif) return;
        if (spinCalisiyor) return;

        if (_ekonomiServisi.Bakiye < _ekonomiServisi.Bahis)
        {
            Debug.LogWarning("Bakiye yetersiz!");
            return;
        }
        
        // Ödenebilir tutar: havuzun %10'u (OdemeServisi)
        _spinOdenebilirLimit = _odemeServisi != null ? _odemeServisi.GetSpinOdenebilirLimit() : int.MaxValue;
        Debug.Log($"[SPIN] Ödenebilir limit: {_spinOdenebilirLimit} (Havuz: {_odemeServisi?.GetHavuzTL() ?? 0} TL, %10)");

        _spinPrevBakiye = _ekonomiServisi.Bakiye;
        _spinBahisTL = _ekonomiServisi.Bahis;
        _odemeServisi?.AddBahisToKasa(_ekonomiServisi.Bahis);
        _ekonomiServisi.DeductBet();
        _logServisi?.RecordSpinStart(_spinPrevBakiye, _ekonomiServisi.Bakiye, _spinBahisTL, _spinOdenebilirLimit);

        _uiServisi?.UI_Guncelle();
        StartCoroutine(_donusServisi.NormalSpinAkisi());
    }

    private void BaslatBonus()
    {
        // Normal spinden bonusa geçişte input kilidini bırak
        spinCalisiyor = false;

if (spinIcon != null) spinIcon.SetRotate(false);

        if (normalOyunMusic != null && normalOyunMusic.isPlaying)
            normalOyunMusic.Pause();

        if (bonusAktif) return;

        bonusAktif = true;
        // ✅ Bonus her başladığında oturum kazancını sıfırla
        oturumKazanc = 0;
        _spinKazanciOturumaEklendi = false;
        if (oturumKazancText != null) oturumKazancText.gameObject.SetActive(true);

        _hizVeSesServisi?.ApplyBonusSpeedIfNeeded();
        _bonusOturumOdenenToplamTL = 0;

        _senaryoServisi?.InitBonusBudgetFromHavuz(_odemeServisi != null ? _odemeServisi.GetHavuzTL() : 0L);

bonusHakKalan = bonusHakBaslangic;
        bonusKazanc = 0;

        _uiServisi?.ButonDurumu(false);
        _uiServisi?.UI_Guncelle();

        StartCoroutine(_donusServisi.BonusBaslangicAkisi());

    }
    private IEnumerator BonusBaslangicAkisi()
    {
        // Bonus mesajını göster
        yield return StartCoroutine(_donusServisi.ShowBonusStartMessage());

        // Sonra free spin döngüsüne gir
        yield return StartCoroutine(_donusServisi.BonusDongusu());
    }

    private IEnumerator ShowBonusStartMessage()
    {
        yield return StartCoroutine(_bonusUIServisi.ShowBonusStartMessage());
    }

    private IEnumerator ShowBonusEndMessage(int bonusToplamKazanc)
    {
        if (bonusEndPanel == null) yield break;

        if (_odemeServisi != null && _bonusPendingOdemeTL > 0)
        {
            int gercekOdeme = _odemeServisi.PayFromHavuz(_bonusPendingOdemeTL);
            int prevBakiye = _ekonomiServisi.Bakiye;
            _ekonomiServisi.AddWinnings(gercekOdeme, 0);
            _logServisi?.RecordBonusEnd(prevBakiye, _ekonomiServisi.Bakiye, gercekOdeme);

            if (gercekOdeme != _bonusPendingOdemeTL)
            {
                int fark = _bonusPendingOdemeTL - gercekOdeme;
                if (fark > 0)
                {
                    _ekonomiServisi.SetBakiye(Mathf.Max(0, _ekonomiServisi.Bakiye - fark));
                    _bonusOdenenTL = Mathf.Max(0, _bonusOdenenTL - fark);
                    bonusToplamKazanc = Mathf.Max(0, bonusToplamKazanc - fark);
                }
            }
            _bonusPendingOdemeTL = 0;
            _uiServisi?.UI_Guncelle();
        }

        yield return StartCoroutine(_bonusUIServisi.ShowBonusEndMessage(bonusToplamKazanc));
    }



// ==========================
    // TUMBLE
    // ==========================
    private int GetBonusRemainingPayableTL()
    {
        if (!bonusAktif) return int.MaxValue;

        // Cap: bonus başlangıcında havuz snapshot'ının belirli oranı
        int cap = (_bonusMaxOdemeTL > 0) ? _bonusMaxOdemeTL : int.MaxValue;

        long kalan = (long)cap - (long)bonusKazanc; // bonusKazanc = şu ana kadar ÖDENEN toplam (pending dahil sayılır)
        if (kalan < 0) kalan = 0;

        // Budget aktifse onu da dikkate al
        if (bonusBudgetAktif)
        {
            long bk = _bonusBudgetKalanTL;
            if (bk < kalan) kalan = bk;
        }

        if (kalan > int.MaxValue) return int.MaxValue;
        return (int)kalan;
    }

    /// <summary>SenaryoServisi delegasyonu için: bonus bütçe/cap alanlarını havuz değerine göre başlatır.</summary>
    private void InitBonusBudgetFromHavuz(long odulHavuzuTL)
    {
        if (bonusBudgetAktif)
        {
            long havuz = odulHavuzuTL;
            int hedef = Mathf.RoundToInt((float)havuz * bonusBudgetHavuzOran);
            hedef = Mathf.Clamp(hedef, bonusBudgetMinTL, bonusBudgetMaxTL);
            if (hedef > havuz) hedef = (int)Mathf.Clamp((float)havuz, 0, int.MaxValue);
            _bonusBudgetKalanTL = hedef;
            Debug.Log($"[BONUS BUDGET] Bonus başı bütçe: {_bonusBudgetKalanTL} TL (Havuz={havuz})");
        }
        else
            _bonusBudgetKalanTL = int.MaxValue;

        _bonusBaslangicHavuzTL = odulHavuzuTL;
        if (bonusMaxOdemeHavuzOrani <= 0f)
            _bonusMaxOdemeTL = int.MaxValue;
        else
        {
            float cap = (float)_bonusBaslangicHavuzTL * bonusMaxOdemeHavuzOrani;
            _bonusMaxOdemeTL = cap > int.MaxValue ? int.MaxValue : Mathf.Max(0, Mathf.RoundToInt(cap));
        }
        _bonusOdenenTL = 0;
        Debug.Log($"[BONUS CAP] HavuzSnapshot={_bonusBaslangicHavuzTL} TL | CapOran={bonusMaxOdemeHavuzOrani} | CapTL={_bonusMaxOdemeTL}");
    }

    /// <summary>SenaryoServisi delegasyonu için: ödenen tutarı kaydeder (_bonusOdenenTL ve _bonusBudgetKalanTL günceller).</summary>
    private void RecordBonusPayment(int odenenTL)
    {
        if (odenenTL > 0) _bonusOdenenTL = Mathf.Clamp(_bonusOdenenTL + odenenTL, 0, int.MaxValue);
        if (bonusBudgetAktif && _bonusBudgetKalanTL != int.MaxValue)
        {
            _bonusBudgetKalanTL -= odenenTL;
            if (_bonusBudgetKalanTL < 0) _bonusBudgetKalanTL = 0;
        }
    }

    private IEnumerator CollapseRefillAndAnimate()
    {
        return _cokmeAkisServisi != null ? _cokmeAkisServisi.CokmeDoldurVeCanlandir() : null;
    }

    private List<Vector2Int> FloodFillCluster(int sx, int sy, int sym, bool[,] visited)
    {
        List<Vector2Int> outList = new List<Vector2Int>();
        Queue<Vector2Int> q = new Queue<Vector2Int>();
        q.Enqueue(new Vector2Int(sx, sy));
        visited[sx, sy] = true;

        while (q.Count > 0)
        {
            var p = q.Dequeue();
            outList.Add(p);

            TryEnqueue(p.x + 1, p.y);
            TryEnqueue(p.x - 1, p.y);
            TryEnqueue(p.x, p.y + 1);
            TryEnqueue(p.x, p.y - 1);
        }

        void TryEnqueue(int nx, int ny)
        {
            if (nx < 0 || nx >= sutun || ny < 0 || ny >= satir) return;
            if (visited[nx, ny]) return;
            if (grid[nx, ny] != sym) return;
            visited[nx, ny] = true;
            q.Enqueue(new Vector2Int(nx, ny));
        }

        return outList;
    }

    // GRID FILL / RENDER
    // ==========================
    private float CurrentScatterChance() => bonusAktif ? scatterChanceBonus : scatterChanceNormal;

    /// <summary>SenaryoServisi delegasyonu için: bonusAktif'e göre scatter şansı döner.</summary>
    private float GetScatterChanceFor(bool bonusAktif) => bonusAktif ? scatterChanceBonus : scatterChanceNormal;

    // KURAL SABİT: tumble eşiği minClusterSize=8
    // Zorluk arttıkça, 8'e TAMAMLAYACAK sembollerin seçilme ihtimali azalır (anti-8 bias).

// v=8'de nötr; v<8'de easy bias, v>8'de hard bias uygular.
private float BiasMultiplier(float easyMult, float hardMult)
{
    float m = 1f;
    if (_easyBias01 > 0f) m *= Mathf.Lerp(1f, easyMult, _easyBias01);
    if (_hardBias01 > 0f) m *= Mathf.Lerp(1f, hardMult, _hardBias01);
    return m;
}

    // ==========================
// ÇARPAN (Yeni Sistem: ekrana bomba/jeton düşer, değerler ÇARPILIR)
// ==========================

    private void CarpanUretVeBirik()
    {
        if (_carpanServisi == null) return;
        _carpanServisi.SetForceCarpan(zorlaSiradakiCarpan);
        _carpanServisi.TryScheduleCarpanDrop(bonusAktif);
        zorlaSiradakiCarpan = 0;
    }

    private void CarpanlariDoluGriddeUygula()
    {
        _carpanYerlestirmeServisi?.CarpanlariDoluGriddeUygula();
    }

    int ICarpanYerlestirmeBaglami.GetSutun() => sutun;
    int ICarpanYerlestirmeBaglami.GetSatir() => satir;
    int[,] ICarpanYerlestirmeBaglami.GetGrid() => grid;
    int[,] ICarpanYerlestirmeBaglami.GetCarpanDegerGrid() => carpanDegerGrid;
    int[] ICarpanYerlestirmeBaglami.GetCarpanDegerByCellIndex() => carpanDegerByCellIndex;
    int ICarpanYerlestirmeBaglami.GetCarpanSembol() => CARPAN_SEMBOL;
    int ICarpanYerlestirmeBaglami.GetScatterIndexCache() => _scatterIndexCache;
    CarpanServisi ICarpanYerlestirmeBaglami.GetCarpanServisi() => _carpanServisi;
    IzgaraServisi ICarpanYerlestirmeBaglami.GetIzgaraServisi() => _izgaraServisi;

private int RastgeleCarpan()
{
    int[] pool = new int[] { 2, 3, 5, 10, 20, 50, 100, 200, 500, 1000 };
    int n = Mathf.Clamp(carpanHavuzu, 1, pool.Length);
    return pool[Random.Range(0, n)];
}
    private int UygulaSpinCarpani(int spinKazanci) => _ekonomiServisi != null ? _ekonomiServisi.UygulaSpinCarpani(spinKazanci) : 0;

private void TrySpawnCarpanOverlay(int carpanDegeri)
{
    if (carpanSembolSprite == null) return;
    if (hucreler == null || hucreler.Length == 0) return;
    int idx = Random.Range(0, hucreler.Length);
    _carpanOverlayServisi?.SpawnCarpanOverlayAt(idx, carpanDegeri);
}

    private void ClearAllCarpanOverlays()
    {
        _carpanOverlayServisi?.ClearAll();

        // Grid içindeki çarpan sembollerini de sıfırla (bir sonraki spin temiz başlasın)
        if (grid != null && carpanDegerGrid != null)
        {
            for (int y = 0; y < satir; y++)
            {
                for (int x = 0; x < sutun; x++)
                {
                    if (grid[x, y] == CARPAN_SEMBOL)
                    {
                        grid[x, y] = -1; // boş yap; FillRandomAll/yerçekimi zaten dolduracak
                        carpanDegerGrid[x, y] = 0;
                    }
                }
            }
        }

        if (carpanHücreTextleri != null)
        {
            for (int i = 0; i < carpanHücreTextleri.Length; i++)
                if (carpanHücreTextleri[i] != null) carpanHücreTextleri[i].gameObject.SetActive(false);
        }
    }



    
// ==========================
// ÇARPAN UI (Yeni Sistem)
// ==========================
private void UI_CarpanSifirla()
{
    _carpanServisi?.ResetForNewSpin(_senaryoServisi != null ? _senaryoServisi.GetMaxCarpanAdedi() : 0);
    ClearAllCarpanOverlays();
    UI_CarpanGuncelle();
}

private void UI_CarpanGuncelle()
{
    if (carpanText == null) return;

    long mlt = _carpanServisi != null ? _carpanServisi.GetCurrentMultiplier() : 0;
    if (mlt < 1) mlt = 1;

  //  carpanText.text = $"ÇARPAN: x{mlt}";
}

    private IEnumerator ScatterBuyutEfekti()
    {
        if (_scatterEfektServisi == null) yield break;
        yield return _scatterEfektServisi.ScatterBuyutEfektiCalistir();
    }
}