using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public partial class OyunYoneticisi
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
    private CarpanSokEfektServisi _carpanSokEfektServisi;
    private BombEfektServisi _bombEfektServisi;
    private ScatterEfektServisi _scatterEfektServisi;
    private BombaInisEfektServisi _bombaInisEfektServisi;
    private TumbleAkisServisi _tumbleAkisServisi;
    private CokmeAkisServisi _cokmeAkisServisi;
    private readonly SenaryoOdemeModelServisi _senaryoOdemeModelServisi = new SenaryoOdemeModelServisi();
    private IzgaraBaslatmaServisi _izgaraBaslatmaServisi;
    private OyunBootstrapServisi _oyunBootstrapServisi;
    private CarpanYerlestirmeServisi _carpanYerlestirmeServisi;
    private ZorlukServisi _zorlukServisi;
    private BonusAyarlari _bonusAyarlari;
    private int _spinPrevBakiye = 0;
    private int _spinBahisTL = 0;
    // Bahis artırma sayacı, butona basıldığı anda değil spin başında (bahis gerçekten kullanıldığında) işlensin.
    private int _sonSpinBaslangicBahis = -1;
    private int _spinNo = 0;
    private GameObject _adminAyarPanelKok;
    private GameObject _normalSpinSonucPopup;
    private TMP_Text _normalSpinSonucBaslikTxt;
    private TMP_Text _normalSpinSonucIcerikTxt;
    private bool _normalSpinSonucPopupCalisiyor = false;
    private bool _normalSpinSonucSesiBuSpinCaldi = false;
    private RectTransform _bahisGorselRtKilidi;
    private bool _bahisGorselKilidiHazir = false;
    private bool _carpanTumbleAktif = true;
    private int _aktifAdminSenaryoIndex = 0;
    private int _senaryo1SonZorunluNihaiOdeme = -1;
    private int _senaryo2DonguIndex = 0;
    private int _senaryo2SonNetKazanc = -1;
    private int _senaryo2SonNetKayip = -1;
    private int _senaryo3DonguIndex = 0;
    private int _senaryo3SonNetKazanc = -1;
    private int _senaryo3SonNetKayip = -1;
    private Vector2 _bahisGorselAnchorMin;
    private Vector2 _bahisGorselAnchorMax;
    private Vector2 _bahisGorselPivot;
    private Vector2 _bahisGorselAnchoredPos;
    private Vector2 _bahisGorselSizeDelta;
    private Vector3 _bahisGorselLocalScale;
    private Quaternion _bahisGorselLocalRotation;
    private bool _senaryoPresetUIHazir = false;
    private bool _senaryoPresetAktif = false;
    private TMP_Dropdown _senaryoPresetDropdown;
    private ISenaryoSpinPolitikasi _spinPolitikasi;

    /// <summary>Pedagojik akışta Aşama 1 (Isındırma/Umut) için varsayılanların bir kez uygulanması.</summary>
    private bool _pedagojikAsama1IsindirmaOnceki = false;
    private bool _adminZorlaButonReferanslariBulundu;
    private Button _adminForceX5Btn, _adminForceX10Btn, _adminForceX50Btn, _adminForceX100Btn, _adminCarpanSifirlaBtn;

    /// <summary>Senaryo 1 paytable simülasyonu: true iken tumble adımı üst sınırı <see cref="_senaryo1KonstrukteMaxTumbleAdimi"/> ile sınırlanır (çok adımlı konstrükte plan).</summary>
    private bool _senaryo1KonstrukteSimAktif;

    /// <summary>1 = tek patlama; 2 = birinci patlama + refill sonrası ikinci küme enjeksiyonu + ikinci patlama.</summary>
    private int _senaryo1KonstrukteMaxTumbleAdimi = 1;

    /// <summary>Refill sonrası gridde oluşturulacak ikinci ödeme kümesi (-1 = yok).</summary>
    private int _senaryo1KonstrukteIkinciKumeSembol = -1;

    private int _senaryo1KonstrukteIkinciKumeBoy = 8;

    private bool _senaryo2KonstrukteSimAktif;
    private int _senaryo2KonstrukteMaxTumbleAdimi = 1;
    private int _senaryo2KonstrukteIkinciKumeSembol = -1;
    private int _senaryo2KonstrukteIkinciKumeBoy = 8;
    private int _senaryo2SonZorunluNihaiOdeme = -1;

    private bool _senaryo3KonstrukteSimAktif;
    private int _senaryo3KonstrukteMaxTumbleAdimi = 1;
    private int _senaryo3KonstrukteIkinciKumeSembol = -1;
    private int _senaryo3KonstrukteIkinciKumeBoy = 0;
    private int _senaryo3SonZorunluNihaiOdeme = -1;

    private enum SenaryoBombSpinTipi { Kayip = 0, Kazanc = 1, Bomb = 2 }

    // Senaryo 4: KY→K→BOMB_100x döngüsü (3-spin, mod 3)
    private int _senaryo4DonguIndex = 0;
    private bool _senaryo4KonstrukteSimAktif;
    private int _senaryo4KonstrukteMaxTumbleAdimi = 1;
    private int _senaryo4SonZorunluNihaiOdeme = -1;

    // Senaryo 5: K→KY→BOMB_500x döngüsü (3-spin, mod 3)
    private int _senaryo5DonguIndex = 0;
    private bool _senaryo5KonstrukteSimAktif;
    private int _senaryo5KonstrukteMaxTumbleAdimi = 1;
    private int _senaryo5SonZorunluNihaiOdeme = -1;
    private bool _senaryo5BombSonrasiPopupBekliyor = false;
    private GameObject _senaryo5PopupGo = null;
    private bool _senaryo5BonusCuziLimitAktif = false;

    private struct AdminSenaryoPreset
    {
        public string Ad;
        public int Bahis;
        public int ScatterYuzde;
        public int CarpanYuzde;
        public int MaxCarpanAdedi;
        public int ZorlaCarpan;
        public int MaxScatterPerSpin;
    }

    private static readonly AdminSenaryoPreset[] _adminSenaryoPresetleri = new AdminSenaryoPreset[]
    {
        new AdminSenaryoPreset { Ad = "1.ALIŞTIRMA", Bahis = 300, ScatterYuzde = 16, CarpanYuzde = 22, MaxCarpanAdedi = 2, ZorlaCarpan = 0, MaxScatterPerSpin = 2 },
        new AdminSenaryoPreset { Ad = "2.BİRAZ KAZANDIRALIM", Bahis = 300, ScatterYuzde = 14, CarpanYuzde = 20, MaxCarpanAdedi = 2, ZorlaCarpan = 0, MaxScatterPerSpin = 2 },
        new AdminSenaryoPreset { Ad = "3.BİRAZ KAYBETTİRELİM", Bahis = 1000, ScatterYuzde = 10, CarpanYuzde = 24, MaxCarpanAdedi = 3, ZorlaCarpan = 0, MaxScatterPerSpin = 2 },
        new AdminSenaryoPreset { Ad = "4.AZ KAZANDIRALIM ÇOK KAYBETTİRELİM", Bahis = 100, ScatterYuzde = 18, CarpanYuzde = 26, MaxCarpanAdedi = 3, ZorlaCarpan = 0, MaxScatterPerSpin = 2 },
        new AdminSenaryoPreset { Ad = "5.BÜYÜK TEKLİFLERLE PARASINI ALALIM", Bahis = 200, ScatterYuzde = 9, CarpanYuzde = 30, MaxCarpanAdedi = 4, ZorlaCarpan = 0, MaxScatterPerSpin = 5 }
    };

    public TextMeshProUGUI bakiyeYukleUyariText; // Input altındaki sonuç yazısı
    public TextMeshProUGUI paraCekUyariText;     // Input altındaki sonuç yazısı

    public TMPro.TextMeshProUGUI zorlukValueText;
    [Header("Ödeme Modeli (Admin Panel)")]
    public TMP_Text odemeEgilimiText;
    public Slider odemeEgilimiSliderUI;
    public Slider odemeDagilimiSliderUI;
    public TMP_Text odemeDagilimiText;
    public TMP_InputField minOdemeInput;
    public TMP_InputField maxOdemeInput;
    public TMP_InputField ustUsteKazancInput;
    public TMP_InputField ustUsteKayipInput;

    private int _ardisikKayipLimiti = 8;
    private int _ardisikKayipSayac = 0;
    // Kaçış Frenleme: ardışık kayıp eşiği aşıldığında SONRAKİ spin'in grid'i cluster oluşacak şekilde zorlanır.
    // Bayrak SimuleEtVeKaydetImpl tarafından okunur ve bir kez tüketilir.
    private bool _kacisFrenlemeBuSpinAktif = false;
    private bool _yeniOyuncuModuAktif = false;
    private float _yeniOyuncuBaslangicZamani = 0f;
    private float _minOdemeCarpan = 0f;   // panel: min ödeme bahis katı (0=devre dışı)
    private float _maksOdemeCarpan = 0f;  // panel: maks ödeme bahis katı (0=devre dışı)

    [Range(0, 100)] [SerializeField] private int _odemeEgilimiYuzde = 50;
    [Range(0, 100)] [SerializeField] private int _odemeDagilimiYuzde = 50;
    [SerializeField] private int _minOdemeTL = 0;
    [SerializeField] private int _maxOdemeTL = 2000;
    [SerializeField] private int _ustUsteKazancHedef = 0;
    [SerializeField] private int _ustUsteKayipHedef = 0;
    [SerializeField] private bool _ustUsteKazancFaziAktif = true;
    [SerializeField] private int _ustUsteFazdaKalan = 0;
    public TMP_Text ayarlarSonucText;
    private const string PP_ADMIN_ODEME_EGILIMI = "PP_ADMIN_ODEME_EGILIMI";
    private const string PP_ADMIN_ODEME_DAGILIMI = "PP_ADMIN_ODEME_DAGILIMI";
    private const string PP_ADMIN_MIN_ODEME = "PP_ADMIN_MIN_ODEME";
    private const string PP_ADMIN_MAX_ODEME = "PP_ADMIN_MAX_ODEME";
    private const string PP_ADMIN_USTUSTE_KAZANC = "PP_ADMIN_USTUSTE_KAZANC";
    private const string PP_ADMIN_USTUSTE_KAYIP = "PP_ADMIN_USTUSTE_KAYIP";

    public TMPro.TextMeshProUGUI carpanOlasilikValueText;
    public TMPro.TextMeshProUGUI carpanMaxAdetValueText;

    private float _lastTumblePopTime = -999f;
    private float _lastTumbleDropTime = -999f;
    /// <summary>Bakiye ≥ 50.000 TL görüldüğünde 20 spin boyunca tumble kapalı; kalan spin sayısı.</summary>
    private int _bakiye50KUstundeTumbleKapaliKalanSpin = 0;
    [Header("Tumble Koruma (Acil Kurtarma)")]
    [Tooltip("Açık olduğunda bakiye 50K üstünde 20 spin tumble kapatır. Varsayılan kapalı tutulur.")]
    public bool bakiye50KteTumbleKapamaAktif = false;

    [Header("Test — Inspector bakiye")]
    [Tooltip("Bu değer değiştiğinde BakiyeText anında güncellenir. Play modunda ekonomi bakiyesi de buna senkronlanır.")]
    [SerializeField] [Min(0)] private int inspectorBakiyeTL = 100_000;
    private int spinKazancHam = 0;   // tumble patlamalarından gelen ham toplam (bu spin)
    private int oturumKazanc = 0;    // oturum boyunca biriken toplam kazanç
    private bool _spinKazanciOturumaEklendi = false; // bonus'ta double sayma önler
    public TextMeshProUGUI oturumKazancText; // (senin OturumKazancText)

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
    private int _sonBonusSatinAlindiMaliyet = 0;
    /// <summary>Senaryo 1'de satın alınan bonus: ödenebilir tutar tavanı uygulanmasın.</summary>
    private bool _bonusSatınAlindiSenaryo1 = false;
    /// <summary>Senaryo bonusu (scatter veya satın al): havuz/ödenebilir tavanı uygulanmasın, sadece hesaplanan cap.</summary>
    private bool _senaryo1BonusAktif = false;
    /// <summary>3. yükleme sonrası ilk bonusta 2,5x tavan uygulandı mı (Aşama 5, tek seferlik).</summary>
    private bool _ucuncuYuklemeSonrasiIlkBonusUygulandi = false;
    /// <summary>Şu an oynanan bonus, senaryo 5 zirve bonusu mu (50 spin, 2.5x; bitişte animasyon tetiklenir).</summary>
    private bool _buBonusZirveBonusuMu = false;
    private int _senaryoOdenebilirKalanTL = -1;
    private int _bonusOturumOdenenToplamTL = 0;

    [Header("Senaryo 5 - Zirve bonusu (tek seferlik yüksek etki)")]
    [Tooltip("3. yükleme sonrası ilk bonus kaç spin sürsün (50 = yüksek etkili sahne).")]
    public int senaryo5_zirveBonusSpinSayisi = 50;
    [Tooltip("Zirve bonusu başladığında tetiklenir; başka script'te dinleyip animasyon/efekt başlatabilirsin.")]
    public System.Action OnZirveBonusBasladi;
    [Tooltip("Zirve bonusu bittiğinde tetiklenir (kazanç TL); yüksek kazanç ekranı/animasyon için kullan.")]
    public System.Action<int> OnZirveBonusBitti;

    [Header("Bonus Ödeme Limiti")]
    [Range(0f, 1f)]
    [Tooltip("Bonus başladığında ödül havuzunun bu oranı kadar (örn 0.10 = %10) maksimum toplam ödeme yapılır. Bu limite ulaşıldıktan sonra bonus devam edebilir ama ek ödeme yapılmaz.")]
    [HideInInspector] public float bonusMaxOdemeHavuzOrani = 0.10f;

    private long _bonusBaslangicHavuzTL = 0;
    private int _bonusMaxOdemeTL = int.MaxValue;
    private int _bonusOdenenTL = 0;

    private int _bonusPendingOdemeTL = 0; // Bonus boyunca havuzdan düşülecek tutarı biriktir (bonus bitince tek seferde düş)
    private int _bonusZorlaCarpanBirikenTL = 0; // Bonus içinde zorla çarpan kazançları; bakiye sadece 10 hak bitince güncellenir

    private int _otomatikSpinKalan = 0;
    private int _otomatikSpinSecilenAdet = 20; // Varsayılan otomatik spin adedi (panel/dropdown yoksa da çoklu spin sürsün)

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
    private bool _adminManuelZorlukKilidi = false;
    private bool _adminManuelScatterKilidi = false;

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

    [Header("Zorluk Ayarı (8 = Sweet Bonanza referans)")]
    public int zorlukSeviyesi = 8;

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
    [HideInInspector] public int bahisMax = 100000;
    [HideInInspector] public int bahisAdim = 1;
    [HideInInspector] public SpinIconRotate spinIcon;   // (LEGACY)
    [HideInInspector] public Button bahisArttirButon; // (LEGACY)
    [HideInInspector] public Button bahisAzaltButon;  // (LEGACY)

    [Header("TUMBLE SES")]
    public AudioSource tumbleSfxSource;
    public AudioClip tumblePopClip;
    public AudioClip tumbleDropClip;
    [Tooltip("Tumble pop sesini klibin kaçıncı saniyesinden başlatacağımız.")]
    public float tumblePopBaslangicOffsetSaniye = 0f;
    [HideInInspector] public float tumblePopMinInterval = 0.06f;   // (LEGACY)
    [HideInInspector] public float tumbleDropMinInterval = 0.12f;  // (LEGACY)

    [Header("BONUS END MUZIK")]
    [HideInInspector] public AudioSource bonusEndMusicAudio;   // (LEGACY)
    [Header("BONUS END SES")]
    public AudioSource bonusEndSfxSource;
    [HideInInspector] public AudioClip bonusEndApplauseClip;  // (LEGACY)
    [Header("Spin Sonuç Sesleri")]
    public AudioClip spinSonucKazancClip;
    public AudioClip spinSonucKayipClip;
    [Header("Çarpan -> Kazanç Efekti")]
    [Tooltip("Çarpan jetonu kazanç kutusuna vardığında çalınacak ses (opsiyonel).")]
    public AudioClip carpanKazancaVurusClip;
    [Range(0f, 1f)] public float carpanKazancaVurusSesSeviyesi = 1f;
    [Tooltip("Efekt sesinin klip içinde kaçıncı saniyeden başlayacağını belirler.")]
    [Min(0f)] public float carpanKazancaVurusBaslangicSn = 0f;
    [Tooltip("0 veya negatif ise klibin sonuna kadar çalar; > 0 ise bu saniyede bitmeden kesilir.")]
    public float carpanKazancaVurusBitisSn = 0f;
    [Header("Zorla Çarpan İlk Düşüş Efekti")]
    [Tooltip("Zorla çarpan kullanılmış spinde, ilk düşüş sırasında ekran sarsıntısı + ses oynatır.")]
    public bool zorlaCarpanIlkDususSokEfektiAktif = true;
    [Tooltip("Zorla çarpan ilk düşüş şok sesi (opsiyonel).")]
    public AudioClip zorlaCarpanIlkDususSokClip;
    [Range(0f, 1f)] public float zorlaCarpanIlkDususSokSesSeviyesi = 1f;
    [Header("Bomba İniş Şok Efekti")]
    [Tooltip("Bu değerin üstündeki çarpan değerlerinde (>=) bomba iniş şok efekti tetiklenir.")]
    public int bombaInisEsikDegeri = 50;
    [Tooltip("Bomba iniş sırasında çalacak gök gürültüsü sesi.")]
    public AudioClip bombaInisThunderClip;
    [Tooltip("Bomba iniş sırasında çalacak bass impact sesi.")]
    public AudioClip bombaInisBassClip;
    [Range(0f, 1f)] public float bombaInisThunderSesSeviyesi = 0.85f;
    [Range(0f, 1f)] public float bombaInisBassSesSeviyesi = 0.9f;
    [Header("NORMAL OYUN MUZIK")]
    [HideInInspector] public AudioSource normalOyunMusic;   // (LEGACY)

    [Header("Grid Ayarları")]
    [HideInInspector] public int sutun = 6;
    [HideInInspector] public int satir = 5;
    [Header("SPIN DROP ANIM")]
    [HideInInspector] public float dropStartYOffset = 700f;   // (LEGACY)
    [HideInInspector] public float dropDuration = 0.72f;      // İlk düşüş biraz daha yavaş ve okunur
    [HideInInspector] public float dropStagger = 0.025f;      // İleride stagger kullanan akışlar için daha yavaş taban

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

    [Header("Animasyon Hızları")]
    [HideInInspector] public float popDuration = 0.75f;
    [HideInInspector] public float fallDuration = 1.20f;
    [HideInInspector] public float betweenStepsDelay = 0.42f;
    [HideInInspector] public float spawnFromTopOffset = 240f;
    [Header("Bonus Hızları (Override)")]
    [HideInInspector] public bool bonusYavasMod = true;
    [HideInInspector] public float bonusPopDuration = 0.70f;
    [HideInInspector] public float bonusFallDuration = 0.78f;
    [HideInInspector] public float bonusBetweenStepsDelay = 0.18f;
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

    [Tooltip("Çarpan (bomba) üretimi açık/kapalı — oyun mantığı carpanUretimiAktif alanını kullanır.")]
    public Toggle carpanAktifToggle;

    [Tooltip("Çarpan sadece bonus turunda düşsün (normalde düşmesin) — carpanSadeceBonus alanını kullanır.")]
    public Toggle carpanSadeceBonusToggle;

    [Header("Win Feedback UI")]
    [Tooltip("Sahnedeki WinFeedbackUI bileşeni. Normal Oyun modunda büyük kazançlarda BIG/HUGE/MEGA/EPIC WIN gösterir.")]
    public WinFeedbackUI winFeedbackUI;

    [Header("UI Referansları")]
    public Button cevirButon;
    public TMP_Text bakiyeText;
    public TMP_Text bahisText;
    [Header("Bahis Görsel Kilidi")]
    [Tooltip("Açıksa BahisGorsel RectTransform değerleri Play boyunca sabit tutulur; hover/layout gibi etkilerden korunur.")]
    public bool bahisGorselKilidiAktif = true;
    [Tooltip("Sahnedeki bahis görsel root nesne adı.")]
    public string bahisGorselNesneAdi = "BahisGorsel";
    [Header("Senaryo Dropdown Görünüm")]
    [Tooltip("Senaryo dropdown seçili metin font boyutu.")]
    [Min(10)] public int senaryoDropdownSeciliYaziBoyutu = 28;
    [Tooltip("Senaryo dropdown liste öğeleri font boyutu.")]
    [Min(10)] public int senaryoDropdownListeYaziBoyutu = 26;
    [Tooltip("Açılan dropdown listesinin panel yüksekliği.")]
    [Min(120f)] public float senaryoDropdownListePanelYukseklik = 460f;
    [Tooltip("Açılan listede her bir satırın yüksekliği.")]
    [Min(36f)] public float senaryoDropdownSatirYukseklik = 82f;
    [Tooltip("Açılan dropdown listesinin genişliği (uzun senaryo metinleri için). 0 ise dropdown genişliği kullanılır.")]
    [Min(0f)] public float senaryoDropdownListeGenislik = 520f;
    [Tooltip("Liste genişliğini metin içeriğine göre otomatik ayarla.")]
    public bool senaryoDropdownGenislikIcerigeGore = true;
    public TMP_Text hakText;
    public TMP_Text bonusOyunKazancText;
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

    [Header("Otomatik Spin")]
    [Tooltip("Tıklanınca panel açar; spin dönerken 'DURDUR' yazar ve tıklanınca durdurur.")]
    public Button otomatikSpinButton;
    [Tooltip("Panel: içinde Dropdown + Baslat + Iptal. Options: 20, 50, 100, 250.")]
    public GameObject otomatikSpinPanel;
    public TMP_Dropdown otomatikSpinDropdown;
    public Button otomatikSpinBaslatButon;
    public Button otomatikSpinIptalButon;
    [Tooltip("Otomatik spin dönerken 'Kalan Spin: x' gösterilir; bitince gizlenir.")]
    public TMP_Text otomatikSpinKalanText;
    [Tooltip("OtomatikSpinButton'da spin dönmezken görünecek metin.")]
    public string otomatikSpinButtonNormalText = "Otomatik Spin";

    [Header("İstatistik / Log")]
    [Tooltip("İstatistik butonu: tıklanınca Log sahnesine geçer; seçili kullanıcının logları gösterilir. Admin ve senaryo sahnelerinde atanabilir.")]
    public Button istatistikButon;
    [Tooltip("Yönetici butonu (YoneticiButton / YÖNET): tıklanınca Admin sahnesine geçer. Inspector'da atanmazsa sahne adı 'YoneticiButton' ile aranır.")]
    public Button yoneticiButon;

    private bool bonusEndCloseRequested = false;
    private bool _boslukTusuBasiliSpin = false;

    public float bonusEndShowTime = 1.4f;
    public float bonusEndFadeTime = 0.25f;

    public CanvasGroup bonusStartCanvasGroup;
    public TMP_Text bonusStartTMP;
    public float bonusStartShowTime = 1.2f;
    public float bonusStartFadeTime = 0.25f;

    [Header("Hücreler (SlotGrid altındaki 30 Image)")]
    [Tooltip("Boş bırakırsan slotGridRoot altından otomatik toplanır.")]
    public Image[] hucreler;

    [Tooltip("SlotGrid objesini buraya ver (GridLayoutGroup bunun üstünde).")]
    public Transform slotGridRoot;

    [Header("Çarpan Ayarları")]
    public bool carpanUretimiAktif = true;
    public bool carpanSadeceBonus = false;
    [Range(0f, 1f)] public float carpanUretimOlasiligi = 0.15f;
    [Range(1, 10)] public int maxCarpanAdedi = 3;
    [Range(1, 10)] public int carpanHavuzu = 5; // havuz büyüklüğü (doğal havuz {2,3,5,8,10} → 5 öğe)
    [Tooltip("DEPRECATED: doğal havuzda kullanılmıyor. 100x/250x/500x yalnızca force path (admin/senaryo 4-5) üzerinden düşer.")]
    [Range(0f, 1f)] public float yuksekCarpanOrani = 0.0f;
    public int zorlaSiradakiCarpan = 0;

[Header("Çarpan Görseli (Sweet Bonanza tarzı)")]
[Tooltip("Çarpan jeton/bomba sprite'ını buraya ver (arka plan transparan).")]
public Sprite carpanSembolSprite;

[Tooltip("Çarpan overlay boyutu (px).")]
public Vector2 carpanOverlaySize = new Vector2(110f, 110f);

[Tooltip("Overlay üzerindeki x2/x5 yazı boyutu.")]
public int carpanOverlayFontSize = 54;

    [Header("Çarpan Yazı Görünümü")]
    public Color carpanYaziRengi = Color.white;
    public Color carpanYaziDisCizgiRengi = new Color(0f, 0f, 0f, 1f);
    [Range(0f, 1f)] public float carpanYaziDisCizgiKalinlik = 0.35f;
    public bool carpanYaziKalin = true;
    public bool carpanYaziGolge = true;
    public Color carpanYaziGolgeRengi = new Color(0f, 0f, 0f, 0.85f);
    public Vector2 carpanYaziGolgeOffset = new Vector2(2f, -2f);

    [Header("Çarpan Yazı Gradient")]
    public bool carpanGradientAktif = true;
    public Color carpanGradientUst = new Color(1f, 0.922f, 0.231f, 1f);
    public Color carpanGradientAlt = new Color(1f, 0.596f, 0f, 1f);
    public float carpanCharacterSpacing = -15f;

    [Header("Çarpan Yazı TMP Underlay")]
    public bool carpanUnderlayAktif = true;
    public Color carpanUnderlayRengi = new Color(0.102f, 0.059f, 0.18f, 1f);
    public float carpanUnderlayOffsetX = 2f;
    public float carpanUnderlayOffsetY = -2f;
    [Range(0f, 1f)] public float carpanUnderlayDilate = 0.2f;
    [Range(0f, 1f)] public float carpanUnderlaySoftness = 0f;

    [Header("Çarpan Yazı TMP Glow")]
    public bool carpanGlowAktif = true;
    public Color carpanGlowRengi = new Color(1f, 0.922f, 0.231f, 0.6f);
    [Range(0f, 1f)] public float carpanGlowOuter = 0.7f;
    [Range(0f, 1f)] public float carpanGlowInner = 0f;
    [Range(0f, 1f)] public float carpanGlowPower = 0.5f;

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
    private bool _carpanKutuUcusAktif = false;
    private int _carpanKutuUcusBirikim = 0;
    /// <summary>Çarpan uçuşu sırasında birikim anlık 0'a düşse bile formülün ham TL'ye düşmemesi için son pozitif birikim üst sınırı.</summary>
    private int _carpanKutuUcusBirikimGosterMax = 0;
    /// <summary>İlk çarpan kutuya vurduktan sonra spin bitene kadar kazanç metninin tekrar düz TL'ye düşmesini engeller (ara karede uçuş bayrağı kapalı kalsa bile).</summary>
    private bool _carpanKutuUcusFormulKilit = false;
    private int _carpanKutuUcusBirikimSonDeger = 0;

    /// <summary>Butona anında tepki: bir sonraki spin önceden hesaplanıp burada tutulur.</summary>
    private SpinSimulasyonKaydi _oncedenHesaplananKayit;
    private bool _oncedenHesaplananHazir;
    private bool _oncedenHesaplananBonusMu;

    /// <summary>
    /// Admin oyun sahnesinde üst üste kayıp 0 iken: kazanç kutusundaki N = arka arkaya N normal spin boyunca ödeme bahisten büyük olur (video anlatımı).
    /// Kayıp kutusu 0 değilse veya admin sahnesi değilse kullanılmaz.
    /// </summary>
    [SerializeField] private int _adminVideoArdisikKazancSpinKalan;

    // UI pos cache
    private Vector2[] cellPos;
    private RectTransform[] cellRT;
    private Behaviour layoutToDisable;
}
