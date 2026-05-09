using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class SenaryoYoneticisi : MonoBehaviour
{
    public static SenaryoYoneticisi I;

    /// <summary>Her spin tamamlandığında (toplamSpin, kazanc, bahis) parametreleriyle tetiklenir.</summary>
    public static event System.Action<int, int, int> OnSpinTamamlandiEvent;

    /// <summary>Öneri senaryo akış tablosundaki 7 aşama.</summary>
    public enum SenaryoAsama
    {
        Asama1_IsindirmaUmut = 1,
        Asama2_KontrolBende = 2,
        Asama3_AzDahaKayipKovalama = 3,
        Asama4_BakiyeTukenis = 4,
        Asama5_BonusZirve = 5,
        Asama6_GercekKayip = 6,
        Asama7_Finale = 7
    }

    [Header("Durum")]
    public bool senaryoAktif = true;
    [Tooltip("Açıksa aşama geçişi yapılmaz; senaryolu oyunda varsayılan kapalı.")]
    public bool gelistirmeModu = false;
    public SenaryoAsama mevcutAsama = SenaryoAsama.Asama1_IsindirmaUmut;

    [Header("Takip")]
    public int toplamSpin;
    public float oyunSuresiDakika;
    public int toplamKazanc;
    public int toplamKayip;
    public int mevcutBakiye;
    public int ilkBakiye = 20000;
    public int yuklemeSayisi = 1;
    public int bahisArtirimSayisi;
    public int bonusGorulduSayisi;
    public int bonusSatinAlmaSayisi;
    public int toplamYatirilanOturum;
    public int asamaGirisSpinIndex;

    // Farkındalık logları için ek takip değişkenleri
    // Net kayıp eşiklerini (ör. 500, 1.000, 2.000 TL) bir kez loglamak için hangi eşiğin sırada olduğunu tutar.
    private readonly int[] _netKayipEsikTL = new int[] { 500, 1000, 2000, 5000, 10000 };
    private int _netKayipEsikIndex = 0;
    // Uzun oyun uyarısı için dakika eşikleri (örn. 15, 30, 45...)
    private float _uzunOyunSonrakiDakikaEsik = 15f;
    // Son spin net sonucu; bahis artışı sonrası tilt davranışını yakalamak için kullanılır.
    private int _sonSpinNet = 0;

    [Header("Takip – Esnek zorluk / analiz")]
    [Tooltip("Son bahis artırımının yapıldığı spin indeksi; Aşama 2'de sonraki 3–4 spinde zorluk düşürülür.")]
    public int sonBahisArtirimSpinIndex = -1;
    [Tooltip("Son yüklemeden hemen sonraki ilk spin indeksi; yükleme sonrası ilk 20 spinde zorluk düşürülür.")]
    public int yuklemeSonrasiIlkSpinIndex = -1;
    [Tooltip("Son bonus (scatter) tetiklenen spin indeksi; log vb. için kullanılır.")]
    public int sonBonusTriggerSpinIndex = -1;
    /// <summary>Bu oturumda son scatter'dan bu yana geçen spin (saklanmaz; oyun/sahne açılışında 0, scatter'da sıfırlanır).</summary>
    private int _spinsSinceLastScatterOturum = 0;
    /// <summary>Son spin bonus tetiklediyse true; SpinTamamlandi'da oturum sayacı artırılmaz.</summary>
    private bool _sonSpinBonusTetikledi = false;
    /// <summary>Zorluk/tumble ayar logunu hangi aşama için yazdık; aynı aşama tekrar loglanmasın.</summary>
    private SenaryoAsama? _sonLoglananZorlukAyarAsama = null;
    [Tooltip("Senaryo 1–2: üst üste ödeme yapılan spin sayısı; eşiğe ulaşınca zorunlu boş spin devreye girer.")]
    public int consecutivePayCount = 0;
    [Tooltip("Senaryo 1–2: kalan zorunlu boş spin sayısı (üst üste ödeme sonrası).")]
    public int forcedNoPayKalan = 0;

    /// <summary>Üst üste ödeme eşiği: daha yüksek = daha uzun kazanç serisine izin (bakiye yavaş yükselsin).</summary>
    public const int UST_USTE_ODEME_ESIK_MIN = 3;
    public const int UST_USTE_ODEME_ESIK_MAX = 5;
    /// <summary>Zorunlu boş spin sayısı: düşük = az soğutma, bakiye hafif yükselme eğilimi.</summary>
    public const int ZORUNLU_BOS_MIN = 1;
    public const int ZORUNLU_BOS_MAX = 2;
    /// <summary>Eşik aşıldığında tetikleme olasılığı: düşük = daha seyrek soğutma, bakiye yavaş artabilsin.</summary>
    public const float ZORUNLU_BOS_TETIK_OLASILIK = 0.52f;

    [Header("Geçiş Şartları (tablo: Isındırma→Kontrol→Az Daha→Bakiye Tükenişi→Bonus Zirve→Gerçek Kayıp→Finale)")]
    [Tooltip("1→2: Spin eşiği (200 spin, 3 bonus görülme, 3 bahis değişimi)")]
    public int gecis1_spin = 200;
    [Tooltip("1→2: En az bu kadar bonus oyunu görülmüş olmalı")]
    public int gecis1_bonusSayisi = 2;
    [Tooltip("1→2: En az bu kadar bahis değişikliği")]
    public int gecis1_bahisDegisim = 3;
    [Tooltip("1→2: Bakiye bu tutara (veya üstüne) bir kez ulaşmış olmalı")]
    public int gecis1_bakiyeUstTL = 35000;

    [Tooltip("2→3: Spin eşiği, bahis değişimi ve bakiye üst sınırı şartları")]
    public int gecis2_spin = 300;
    [Tooltip("2→3: En az bu kadar bahis değişikliği")]
    public int gecis2_bahisDegisim = 5;
    [Tooltip("2→3: Bakiye bu tutara (veya üstüne) bir kez ulaşmış olmalı")]
    public int gecis2_bakiyeUstTL = 45000;

    [Tooltip("3→4: Spin eşiği (150 spin, bakiye %40 erime)")]
    public int gecis3_spin = 150;
    [Tooltip("3→4: Bakiye bu oranda eriyince (0–100, 40 = %40) aşama giriş bakiyesine göre")]
    public int gecis3_bakiyeErimeYuzde = 40;

    [Tooltip("4→5: Bakiye bu tutarın altına düşünce + 1 yükleme (Bakiye 10.000 TL altı, 1 yükleme)")]
    public int gecis4_bakiyeAltiTL = 10000;
    [Tooltip("5→6: 3. yükleme tamamlandı; 6→7 fallback için de kullanılır")]
    public int gecis4_yukleme = 3;

    [Tooltip("5→6: Bu aşamada en az bu kadar spin şartı (3. yükleme ile birlikte).")]
    public int gecis5_asamaSpin = 100;

    [Tooltip("6→7: Toplam spin bu değere ulaşınca + net negatif bakiye")]
    public int gecis6_toplamSpinMin = 500;

    [Header("Kazanç Oranları %")]
    public float asama1_oran = 75f;
    public float asama2_oran = 50f;
    public float asama3_oran = 25f;
    public float asama4_oran = 15f;
    public float asama5_oran = 5f;

    [Header("UI - Oyun")]
    public TextMeshProUGUI asamaText;
    public TextMeshProUGUI spinText;
    public TextMeshProUGUI kazancText;
    public TextMeshProUGUI bakiyeText;

    [Header("UI - Auto Spin")]
    public TMP_Dropdown autoSpinDropdown;
    public Button autoSpinBaslatBtn;
    public Button autoSpinDurdurBtn;
    public TextMeshProUGUI autoSpinKalanText;

    [Header("UI - Admin")]
    public Toggle senaryoAktifToggle;
    public Toggle gelistirmeToggle;
    public TextMeshProUGUI mevcutAsamaText;
    public TextMeshProUGUI gecisSartText;
    public Button[] manuelGecisButonlari;

    [Header("UI - Senaryo durum paneli (kenar)")]
    [Tooltip("Mevcut aşama: 1 - Isındırma / Umut")]
    public TMP_Text mevcutAsamaMetni;
    [Tooltip("Tamamlanan çıkış şartları")]
    public TMP_Text tamamlananSartlarMetni;
    [Tooltip("Aşamadan çıkmak için kalan şartlar")]
    public TMP_Text kalanSartlarMetni;
    [Tooltip("Çıkmak için en az 2 şart gerekli")]
    public TMP_Text cikisIcinBilgiMetni;
    [Tooltip("Manuel aşama seçimi (1-7)")]
    public TMP_Dropdown manuelAsamaDropdown;
    [Tooltip("Manuel aşama dropdown'ında seçeneklerin ve seçili değerin font boyutu. 0 ise varsayılan kalır.")]
    [Min(0)] public int manuelAsamaDropdownFontBoyutu = 22;
    [Tooltip("Bu Aşamaya Geç butonu")]
    public Button asamayaGecButonu;
    [Tooltip("Sol üstte 'Hoşgeldiniz [kullanıcı adı]' yazacak metin (Inspector'da 'New Text' olan alanı buraya sürükleyin).")]
    public TMP_Text hosgeldinizText;
    [Tooltip("Mevcut senaryoya göre ayar değerleri: zorluk, scatter, çarpan düşme vb.")]
    public TMP_Text mevcutAyarlarMetni;

    private List<SenaryoOlayKaydi> oturumLogu = new List<SenaryoOlayKaydi>();
    private int _asamaGirisBakiyesi;
    private int _asamaGirisBonusSayisi;
    private int _sonBonusGirisBakiyesi;
    /// <summary>Aşama 1'de bakiye gecis1_bakiyeUstTL'e (örn. 35.000) bir kez ulaştı mı.</summary>
    private bool _asama1BakiyeUstTLUlasti;
    /// <summary>Aşama 2'de bakiye gecis2_bakiyeUstTL'e (örn. 45.000) bir kez ulaştı mı.</summary>
    private bool _asama2BakiyeUstTLUlasti;
    private float baslangicZamani;
    private bool autoSpinAktif;
    private int kalanAutoSpin;
    private bool coroutineCalisiyor;
    private bool _bonusAktif;

    void Awake()
    {
        SceneManager.sceneLoaded += SahneYuklendi_KalitimiTemizle;
        string sahneAdi = SceneManager.GetActiveScene().name ?? "";
        bool senaryoSahnesi = sahneAdi.IndexOf("Senaryo", StringComparison.OrdinalIgnoreCase) >= 0 || sahneAdi.StartsWith("02_", StringComparison.Ordinal);
        if (senaryoSahnesi)
        {
            I = this;
            DontDestroyOnLoad(gameObject);
            return;
        }
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    void SahneYuklendi_KalitimiTemizle(Scene sahne, LoadSceneMode mod)
    {
        if (sahne.name == "01_GirisScene")
        {
            I = null;
            Destroy(gameObject);
        }
    }

    void Start()
    {
        baslangicZamani = Time.time;
        // Uygulama yeniden açıldığında seçili kullanıcının kayıtlı verilerini ekranda göster (spin sayısı, bakiye vb.)
        if (GameManager.I != null && GameManager.I.ActivePlayer != null)
        {
            var p = GameManager.I.ActivePlayer;
            toplamSpin = p.totalSpins;
            mevcutBakiye = p.balance;
            toplamKazanc = p.totalWon;
            toplamKayip = p.totalLost;
            toplamYatirilanOturum = p.totalDeposited > 0 ? p.totalDeposited : ilkBakiye;
        }
        else
        {
            mevcutBakiye = ilkBakiye;
            toplamYatirilanOturum = ilkBakiye;
        }

        // Sadece bu kullanıcıya ait kayıt varsa yükle; yoksa veya yeni kullanıcıysa 1. aşamadan başla
        string asamaKey = SenaryoAsamaKey();
        string spinKey = SenaryoAsamaGirisSpinKey();
        string bonusKey = SenaryoBonusGorulduKey();
        if (!string.IsNullOrEmpty(GameManager.I?.ActivePlayer?.playerId) && PlayerPrefs.HasKey(asamaKey))
        {
            int kayitliAsama = PlayerPrefs.GetInt(asamaKey, 1);
            mevcutAsama = (SenaryoAsama)Mathf.Clamp(kayitliAsama, 1, 7);
            asamaGirisSpinIndex = PlayerPrefs.GetInt(spinKey, 0);
            if (PlayerPrefs.HasKey(bonusKey))
                bonusGorulduSayisi = PlayerPrefs.GetInt(bonusKey, 0);
            if (PlayerPrefs.HasKey(SenaryoBahisArtirimKey()))
                bahisArtirimSayisi = PlayerPrefs.GetInt(SenaryoBahisArtirimKey(), 0);
        }
        else
        {
            mevcutAsama = SenaryoAsama.Asama1_IsindirmaUmut;
            asamaGirisSpinIndex = 0;
        }

        OturumLoguYukle();
        if (oturumLogu.Count == 0)
        {
            LogEkle(SenaryoOlayKaydi.OlayTipi_OturumBasladi, $"Oturum başladı. Başlangıç bakiyesi: {mevcutBakiye:N0} TL. Aşama: {GetAsamaAdi()}.");
            LogEkle(SenaryoOlayKaydi.OlayTipi_AsamaGirisi, $"{GetAsamaAdi()} — Başlangıç bakiyesi: {mevcutBakiye:N0} TL.");
        }
        _asamaGirisBakiyesi = mevcutBakiye;
        _asamaGirisBonusSayisi = bonusGorulduSayisi;
        if (hosgeldinizText != null)
        {
            string _ad = KullaniciVerileri.KullaniciAdi;
            if (string.IsNullOrWhiteSpace(_ad)) _ad = GameManager.I?.ActivePlayer?.playerName ?? "Misafir";
            hosgeldinizText.text = "Hoşgeldiniz " + _ad;
        }
        SetupUI();
        ManuelAsamaDropdownDoldur();
        if (asamayaGecButonu != null)
            asamayaGecButonu.onClick.AddListener(OnAsamayaGecTiklandi);
        SenaryoDurumPanelKaydiriciKur();
        UI_Guncelle();
        // Ödenebilir tutar Kasa/OyunYoneticisi hazır olduktan sonra gelsin diye kısa gecikmeli yenileme (çalıştırınca da doğru değer görünsün).
        StartCoroutine(GecikmeliMevcutDurumYenile());
        // Isındırma aşamasında kullanıcının bilinçli kazanması için zorluğu düşür (5 = daha sık tumble/kazanç).
        AsamaAyariniUygula();
    }

    private const int SENARYO_ODENEBILIR_TUTAR_TL = 100000;
    private const string PP_SENARYO_ASAMA = "PP_SENARYO_MEVCUT_ASAMA";
    private const string PP_SENARYO_ASAMA_GIRIS_SPIN = "PP_SENARYO_ASAMA_GIRIS_SPIN";
    private const string PP_SENARYO_BONUS_GORULDU = "PP_SENARYO_BONUS_GORULDU_TOPLAM";
    private const string PP_SENARYO_BAHIS_ARTIRIM = "PP_SENARYO_BAHIS_ARTIRIM_SAYISI";

    /// <summary>Aşama kaydı kullanıcıya göre tutulur; yeni kullanıcı veya logu olmayan 1. aşamada başlar.</summary>
    private string SenaryoAsamaKey() => PP_SENARYO_ASAMA + "_" + (GameManager.I?.ActivePlayer?.playerId ?? "");
    private string SenaryoAsamaGirisSpinKey() => PP_SENARYO_ASAMA_GIRIS_SPIN + "_" + (GameManager.I?.ActivePlayer?.playerId ?? "");
    private string SenaryoBonusGorulduKey() => PP_SENARYO_BONUS_GORULDU + "_" + (GameManager.I?.ActivePlayer?.playerId ?? "");
    private string SenaryoBahisArtirimKey() => PP_SENARYO_BAHIS_ARTIRIM + "_" + (GameManager.I?.ActivePlayer?.playerId ?? "");

    [Tooltip("Senaryolu sahnede ödenebilir tutar = 100.000 * bu çarpan. 1 = tam 100k, 0,5 = 50k (esnek havuz düşürme).")]
    [Range(0.1f, 1f)]
    public float odenebilirCarpani = 1f;

    /// <summary>Senaryo durum paneline sağ/sol kaydıran ok davranışını ekler (Inspector ataması gerekmez).</summary>
    private void SenaryoDurumPanelKaydiriciKur()
    {
        TMP_Text t = mevcutAsamaMetni != null ? mevcutAsamaMetni : tamamlananSartlarMetni;
        if (t == null) t = kalanSartlarMetni;
        if (t == null) return;
        Transform parent = t.transform.parent;
        if (parent == null) return;
        if (parent.GetComponent<SenaryoDurumPaneliKaydirici>() != null) return;
        parent.gameObject.AddComponent<SenaryoDurumPaneliKaydirici>();
    }

    private System.Collections.IEnumerator GecikmeliMevcutDurumYenile()
    {
        yield return null;
        yield return new UnityEngine.WaitForSeconds(0.4f);
        var oyun = FindObjectOfType<OyunYoneticisi>();
        if (oyun != null)
            oyun.SenaryoOdenebilirBütceYükleVeyaBaslat(SENARYO_ODENEBILIR_TUTAR_TL);
        AsamaAyariniUygula();
        UI_Guncelle();
    }

    /// <summary>Spec tablosuna göre aşama bazlı temel zorluk: 1→5, 2→6, 3→7, 4→8, 5→9, 6→10, 7→11. Esnek kurallar (bahis artırımı / yükleme sonrası) geçici zorluk düşürür. Senaryolu sahnede ödenebilir 100k (veya çarpan) her uygulamada yeniden set edilir.</summary>
    private void AsamaAyariniUygula()
    {
        var oyun = FindObjectOfType<OyunYoneticisi>();
        if (oyun == null) return;
        if (oyun.AdminManuelZorlukKilidiAktif()) return;

        int zorluk = GetAsamaTemelZorluk(mevcutAsama);

        int spinFarkiYukleme = yuklemeSonrasiIlkSpinIndex >= 0 ? (toplamSpin - yuklemeSonrasiIlkSpinIndex) : 999;
        int spinFarkiBahis = sonBahisArtirimSpinIndex >= 0 ? (toplamSpin - sonBahisArtirimSpinIndex) : 999;

        if (mevcutAsama == SenaryoAsama.Asama2_KontrolBende && spinFarkiBahis >= 1 && spinFarkiBahis <= 4)
            zorluk = 5;
        else if (spinFarkiYukleme >= 1 && spinFarkiYukleme <= 20 && (int)mevcutAsama <= 3)
            zorluk = Mathf.Min(zorluk, 6);

        oyun.SetZorluk(zorluk);

        if (_sonLoglananZorlukAyarAsama != mevcutAsama)
        {
            _sonLoglananZorlukAyarAsama = mevcutAsama;
            int v = oyun.zorlukSeviyesi;
            float easyBias = (v < 8) ? Mathf.InverseLerp(8f, 4f, v) : 0f;
            float hardBias = (v > 8) ? Mathf.InverseLerp(8f, 12f, v) : 0f;
            int scatterYuzde = Mathf.RoundToInt(oyun.scatterChanceNormal * 100f);
            int carpanYuzde = Mathf.RoundToInt(oyun.carpanUretimOlasiligi * 100f);
            string aciklama = $"{GetAsamaAdi()}: Zorluk {v}, Tumble eşiği 8, Kolay bias {easyBias:F2}, Zor bias {hardBias:F2}, Scatter %{scatterYuzde}, Çarpan %{carpanYuzde}.";
            LogEkle(SenaryoOlayKaydi.OlayTipi_ZorlukTumbleAyar, aciklama);
        }
    }

    /// <summary>Spec: 1→5, 2→6, 3→7, 4→8, 5→9, 6→10, 7→11.</summary>
    private static int GetAsamaTemelZorluk(SenaryoAsama asama)
    {
        switch (asama)
        {
            case SenaryoAsama.Asama1_IsindirmaUmut: return 5;
            case SenaryoAsama.Asama2_KontrolBende: return 6;
            case SenaryoAsama.Asama3_AzDahaKayipKovalama: return 7;
            case SenaryoAsama.Asama4_BakiyeTukenis: return 8;
            case SenaryoAsama.Asama5_BonusZirve: return 9;
            case SenaryoAsama.Asama6_GercekKayip: return 10;
            case SenaryoAsama.Asama7_Finale: return 11;
            default: return 8;
        }
    }

    void Update()
    {
        oyunSuresiDakika = (Time.time - baslangicZamani) / 60f;

        // Uzun süre ara vermeden oyun oynandığında farkındalık logu üret.
        // Örn: 15 dk, 30 dk, 45 dk... eşikleri aşıldığında birer kez loglanır.
        if (senaryoAktif && toplamSpin > 0 && oyunSuresiDakika >= _uzunOyunSonrakiDakikaEsik)
        {
            int dakika = Mathf.RoundToInt(_uzunOyunSonrakiDakikaEsik);
            LogEkle(SenaryoOlayKaydi.OlayTipi_Uyari_UzunOyun,
                $"Bu oturumda ara vermeden yaklaşık {dakika} dakika oynandı. Toplam spin: {toplamSpin}.");
            _uzunOyunSonrakiDakikaEsik += 15f; // Sonraki uyarı için eşiği artır (her 15 dakikada bir)
        }

        if (autoSpinAktif && kalanAutoSpin != 0 && !coroutineCalisiyor)
            StartCoroutine(AutoSpinRoutine());
    }

    void SetupUI()
    {
        if (autoSpinDropdown != null)
        {
            autoSpinDropdown.ClearOptions();
            autoSpinDropdown.AddOptions(new List<string> { "10", "25", "50", "100", "200", "Sonsuz" });
            autoSpinDropdown.value = 3;
            autoSpinDropdown.onValueChanged.AddListener(v => { });
        }
        if (autoSpinBaslatBtn != null) autoSpinBaslatBtn.onClick.AddListener(AutoSpinBaslat);
        if (autoSpinDurdurBtn != null) autoSpinDurdurBtn.onClick.AddListener(AutoSpinDurdur);

        if (senaryoAktifToggle != null)
        {
            senaryoAktifToggle.onValueChanged.AddListener(v => { senaryoAktif = v; UI_Guncelle(); });
            senaryoAktifToggle.SetIsOnWithoutNotify(senaryoAktif);
        }
        if (gelistirmeToggle != null)
        {
            gelistirmeToggle.onValueChanged.AddListener(v => { gelistirmeModu = v; UI_Guncelle(); });
            gelistirmeToggle.SetIsOnWithoutNotify(gelistirmeModu);
        }

        if (manuelGecisButonlari != null)
            for (int i = 0; i < manuelGecisButonlari.Length; i++)
                if (manuelGecisButonlari[i] != null)
                {
                    int asama = i + 1;
                    manuelGecisButonlari[i].onClick.AddListener(() => ManuelGecis(asama));
                }

        GecisSartGuncelle();
    }

    private void ManuelAsamaDropdownDoldur()
    {
        if (manuelAsamaDropdown == null) return;
        manuelAsamaDropdown.ClearOptions();
        var secenekler = new List<string>
        {
            "1 - Isındırma / Umut",
            "2 - Kontrol bende",
            "3 - Az daha / Kayıp kovalama",
            "4 - Bakiye tükenişi",
            "5 - Bonus zirve",
            "6 - Gerçek kayıp",
            "7 - Finale"
        };
        manuelAsamaDropdown.AddOptions(secenekler);
        if (manuelAsamaDropdownFontBoyutu > 0)
        {
            if (manuelAsamaDropdown.captionText != null)
                manuelAsamaDropdown.captionText.fontSize = manuelAsamaDropdownFontBoyutu;
            if (manuelAsamaDropdown.itemText != null)
            {
                manuelAsamaDropdown.itemText.fontSize = manuelAsamaDropdownFontBoyutu;
                manuelAsamaDropdown.itemText.enableWordWrapping = true;
            }
        }
        // Açılır listenin genişliği ve satır yüksekliği: metin kesilmesin, diğer UI ile iç içe girmesin
        var templateRt = manuelAsamaDropdown.template;
        if (templateRt != null)
        {
            float minListeGenislik = 320f;
            if (templateRt.rect.width < minListeGenislik)
                templateRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, minListeGenislik);
            var viewport = templateRt.Find("Viewport");
            if (viewport != null)
            {
                var content = viewport.Find("Content");
                if (content != null && content.childCount > 0)
                {
                    var item = content.GetChild(0);
                    var le = item.GetComponent<UnityEngine.UI.LayoutElement>();
                    if (le == null) le = item.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
                    le.minHeight = 40f;
                    le.preferredHeight = -1f;
                    le.flexibleWidth = 1f;
                }
            }
        }
    }

    private void OnAsamayaGecTiklandi()
    {
        if (manuelAsamaDropdown == null) return;
        int secim = manuelAsamaDropdown.value;
        int asamaNo = Mathf.Clamp(secim + 1, 1, 7);
        SenaryoAsama hedef = (SenaryoAsama)asamaNo;
        if (mevcutAsama == hedef) return;
        LogEkle(SenaryoOlayKaydi.OlayTipi_ManuelGecis, $"Manuel aşama değişikliği: {GetAsamaAdi()} → {GetAsamaAdi(hedef)}");
        AsamaGecir(hedef);
    }

    public void AsamaGecisiKontrol()
    {
        if (!senaryoAktif || gelistirmeModu) return;
        if (mevcutAsama == SenaryoAsama.Asama7_Finale) return;

        int spinFarki = toplamSpin - asamaGirisSpinIndex;
        bool gecildi = false;
        switch (mevcutAsama)
        {
            case SenaryoAsama.Asama1_IsindirmaUmut:
                // 1→2: Bu aşamanın çıkış şartlarından en az 2 tanesi sağlanmışsa VEYA spinFarki ≥ 150 ise geç
                gecildi = CikisSartlariniDegerlendir(1, out _, out _) >= 2 || spinFarki >= 150;
                if (gecildi) AsamaGecir(SenaryoAsama.Asama2_KontrolBende);
                break;
            case SenaryoAsama.Asama2_KontrolBende:
                // 2→3: Spin, bahis değişimi ve bakiye üst sınırı şartları
                gecildi = CikisSartlariniDegerlendir(2, out _, out _) >= 3;
                if (gecildi) AsamaGecir(SenaryoAsama.Asama3_AzDahaKayipKovalama);
                break;
            case SenaryoAsama.Asama3_AzDahaKayipKovalama:
                // 3→4: Bu aşamanın çıkış şartlarından en az 2 tanesi sağlanmışsa VEYA spinFarki ≥ 120 VEYA yuklemeSayisi ≥ 2 ise geç
                gecildi = CikisSartlariniDegerlendir(3, out _, out _) >= 2 || spinFarki >= 120 || yuklemeSayisi >= 2;
                if (gecildi) AsamaGecir(SenaryoAsama.Asama4_BakiyeTukenis);
                break;
            case SenaryoAsama.Asama4_BakiyeTukenis:
                gecildi = (mevcutBakiye < gecis4_bakiyeAltiTL && yuklemeSayisi >= 2) || yuklemeSayisi >= gecis4_yukleme;
                if (gecildi) AsamaGecir(SenaryoAsama.Asama5_BonusZirve);
                break;
            case SenaryoAsama.Asama5_BonusZirve:
                int spinBuAsamada = toplamSpin - asamaGirisSpinIndex;
                gecildi = yuklemeSayisi >= gecis4_yukleme && spinBuAsamada >= gecis5_asamaSpin;
                if (gecildi) AsamaGecir(SenaryoAsama.Asama6_GercekKayip);
                break;
            case SenaryoAsama.Asama6_GercekKayip:
                int netZarar = toplamKayip - toplamKazanc;
                bool netNegatif = mevcutBakiye < ilkBakiye || netZarar > 0;
                gecildi = (toplamSpin >= gecis6_toplamSpinMin && netNegatif) || yuklemeSayisi >= gecis4_yukleme || mevcutBakiye <= 0;
                if (gecildi) AsamaGecir(SenaryoAsama.Asama7_Finale);
                break;
        }
    }

    /// <summary>Mevcut aşama için çıkış şartları (tabloya göre); tamamlanan/kalan listeleri doldurur.</summary>
    private int CikisSartlariniDegerlendir(int asamaNo, out List<string> tamamlanan, out List<string> kalan)
    {
        tamamlanan = new List<string>();
        kalan = new List<string>();
        int spinFarki = toplamSpin - asamaGirisSpinIndex;
        int netZarar = toplamKayip - toplamKazanc;

        switch (asamaNo)
        {
            case 1:
                if (spinFarki >= gecis1_spin)
                    tamamlanan.Add($"{gecis1_spin} spin (tamamlandı)");
                else
                {
                    int kalanSpin = Mathf.Max(0, gecis1_spin - spinFarki);
                    kalan.Add($"{gecis1_spin} spin (atılan: {spinFarki}, kalan: {kalanSpin})");
                }
                if (bonusGorulduSayisi >= gecis1_bonusSayisi) tamamlanan.Add($"Bonus görülme ≥ {gecis1_bonusSayisi} ({bonusGorulduSayisi})"); else kalan.Add($"Bonus görülme ≥ {gecis1_bonusSayisi} ({bonusGorulduSayisi})");
                if (bahisArtirimSayisi >= gecis1_bahisDegisim) tamamlanan.Add($"Bahis değişimi ≥ {gecis1_bahisDegisim} ({bahisArtirimSayisi})"); else kalan.Add($"Bahis değişimi ≥ {gecis1_bahisDegisim} ({bahisArtirimSayisi})");
                if (_asama1BakiyeUstTLUlasti) tamamlanan.Add($"Bakiye ≥ {gecis1_bakiyeUstTL:N0} TL"); else kalan.Add($"Bakiye ≥ {gecis1_bakiyeUstTL:N0} TL");
                break;
            case 2:
                if (spinFarki >= gecis2_spin)
                    tamamlanan.Add($"{gecis2_spin} spin (tamamlandı)");
                else
                {
                    int kalanSpin2 = Mathf.Max(0, gecis2_spin - spinFarki);
                    kalan.Add($"{gecis2_spin} spin (atılan: {spinFarki}, kalan: {kalanSpin2})");
                }
                if (bahisArtirimSayisi >= gecis2_bahisDegisim) tamamlanan.Add($"Bahis değişimi ≥ {gecis2_bahisDegisim} ({bahisArtirimSayisi})"); else kalan.Add($"Bahis değişimi ≥ {gecis2_bahisDegisim} ({bahisArtirimSayisi})");
                if (_asama2BakiyeUstTLUlasti) tamamlanan.Add($"Bakiye ≥ {gecis2_bakiyeUstTL:N0} TL"); else kalan.Add($"Bakiye ≥ {gecis2_bakiyeUstTL:N0} TL");
                break;
            case 3:
                int bakiyeErime = _asamaGirisBakiyesi * (100 - Mathf.Clamp(gecis3_bakiyeErimeYuzde, 0, 100)) / 100;
                if (spinFarki >= gecis3_spin)
                    tamamlanan.Add($"{gecis3_spin} spin (tamamlandı)");
                else
                {
                    int kalanSpin3 = Mathf.Max(0, gecis3_spin - spinFarki);
                    kalan.Add($"{gecis3_spin} spin (atılan: {spinFarki}, kalan: {kalanSpin3})");
                }
                if (mevcutBakiye <= bakiyeErime) tamamlanan.Add($"Bakiye %{gecis3_bakiyeErimeYuzde} erime ({mevcutBakiye:N0} ≤ {bakiyeErime:N0} TL)"); else kalan.Add($"Bakiye %{gecis3_bakiyeErimeYuzde} erime ({mevcutBakiye:N0} > {bakiyeErime:N0} TL)");
                break;
            case 4:
                if (mevcutBakiye < gecis4_bakiyeAltiTL) tamamlanan.Add($"Bakiye < {gecis4_bakiyeAltiTL:N0} TL ({mevcutBakiye:N0})"); else kalan.Add($"Bakiye < {gecis4_bakiyeAltiTL:N0} TL ({mevcutBakiye:N0})");
                if (yuklemeSayisi >= 2) tamamlanan.Add($"En az 1 yükleme ({yuklemeSayisi})"); else kalan.Add($"En az 1 yükleme ({yuklemeSayisi})");
                break;
            case 5:
                int spinBuAsamada5 = toplamSpin - asamaGirisSpinIndex;
                if (yuklemeSayisi >= gecis4_yukleme)
                    tamamlanan.Add($"{gecis4_yukleme}. yükleme tamamlandı ({yuklemeSayisi})");
                else
                    kalan.Add($"{gecis4_yukleme}. yükleme ({yuklemeSayisi}/{gecis4_yukleme})");
                if (spinBuAsamada5 >= gecis5_asamaSpin)
                    tamamlanan.Add($"{gecis5_asamaSpin} spin (tamamlandı)");
                else
                {
                    int kalanSpin5 = Mathf.Max(0, gecis5_asamaSpin - spinBuAsamada5);
                    kalan.Add($"{gecis5_asamaSpin} spin (atılan: {spinBuAsamada5}, kalan: {kalanSpin5})");
                }
                break;
            case 6:
                bool netNegatif = mevcutBakiye < ilkBakiye || netZarar > 0;
                if (toplamSpin >= gecis6_toplamSpinMin)
                    tamamlanan.Add($"Toplam spin ≥ {gecis6_toplamSpinMin} (tamamlandı)");
                else
                {
                    int kalanToplamSpin = Mathf.Max(0, gecis6_toplamSpinMin - toplamSpin);
                    kalan.Add($"Toplam spin ≥ {gecis6_toplamSpinMin} (atılan: {toplamSpin}, kalan: {kalanToplamSpin})");
                }
                if (netNegatif) tamamlanan.Add("Net negatif bakiye"); else kalan.Add("Net negatif bakiye");
                break;
        }
        return tamamlanan.Count;
    }

    private void SartMetinleriniGuncelle()
    {
        CikisSartlariniDegerlendir((int)mevcutAsama, out var tamamlanan, out var kalan);
        if (tamamlananSartlarMetni != null)
        {
            string metinTamamlanan = tamamlanan.Count > 0 ? string.Join(", ", tamamlanan) : "—";
            tamamlananSartlarMetni.enableWordWrapping = true;
            tamamlananSartlarMetni.overflowMode = TextOverflowModes.Overflow;
            // Sadece tamamlanan şartları göster; global sayaç ayrı alanda yazılacak.
            tamamlananSartlarMetni.text = $"Tamamlanan: {metinTamamlanan}";
        }
        if (kalanSartlarMetni != null)
        {
            string metinKalan = kalan.Count > 0 ? string.Join(", ", kalan) : "—";
            kalanSartlarMetni.enableWordWrapping = true;
            kalanSartlarMetni.overflowMode = TextOverflowModes.Overflow;
            kalanSartlarMetni.text = $"Kalan: {metinKalan}";
        }
        if (cikisIcinBilgiMetni != null)
        {
            string temelMetin;
            if (mevcutAsama == SenaryoAsama.Asama7_Finale)
                temelMetin = "Son aşama.";
            else if (mevcutAsama == SenaryoAsama.Asama5_BonusZirve)
                temelMetin = $"Bu aşamada en az {gecis5_asamaSpin} spin + 3. yükleme tamamlanınca geçilir.";
            else
                temelMetin = "Tüm şartlar sağlanınca geçilir.";

            // Geliştirme modunda toplam spin sayacını burada, ayrı satırda göster; diğer metinlerle üst üste binmez.
            if (gelistirmeModu)
                cikisIcinBilgiMetni.text = temelMetin + $"\nToplam spin (tüm oyun): {toplamSpin}";
            else
                cikisIcinBilgiMetni.text = temelMetin;
        }
        if (mevcutAsamaMetni != null)
            mevcutAsamaMetni.text = "Mevcut aşama: " + GetAsamaAdi();
    }

    public void AsamaGecir(SenaryoAsama yeni)
    {
        if (mevcutAsama == yeni) return;
        SenaryoAsama onceki = mevcutAsama;
        int spinBuAsamada = toplamSpin - asamaGirisSpinIndex;
        int bonusBuAsamada = bonusGorulduSayisi - _asamaGirisBonusSayisi;
        LogEkle(SenaryoOlayKaydi.OlayTipi_AsamaOzeti, $"Aşama kapanış özeti — Toplam dönüş: {spinBuAsamada} | Bonus oyunu: {bonusBuAsamada} | Başlangıç bakiyesi: {_asamaGirisBakiyesi:N0} TL | Kapanış bakiyesi: {mevcutBakiye:N0} TL", (int)onceki);

        mevcutAsama = yeni;
        asamaGirisSpinIndex = toplamSpin;
        _asamaGirisBakiyesi = mevcutBakiye;
        _asamaGirisBonusSayisi = bonusGorulduSayisi;
        consecutivePayCount = 0;
        forcedNoPayKalan = 0;
        if (yeni == SenaryoAsama.Asama1_IsindirmaUmut)
            _asama1BakiyeUstTLUlasti = false;
        if (yeni == SenaryoAsama.Asama2_KontrolBende)
            _asama2BakiyeUstTLUlasti = false;
        if (manuelAsamaDropdown != null)
            manuelAsamaDropdown.SetValueWithoutNotify(Mathf.Clamp((int)mevcutAsama - 1, 0, 6));
        LogEkle(SenaryoOlayKaydi.OlayTipi_AsamaGecisi, $"Aşama geçişi: {GetAsamaAdi(onceki)} → {GetAsamaAdi(yeni)}. Spin: {toplamSpin}. Kapanış bakiyesi önceki aşama: {mevcutBakiye:N0} TL.");
        LogEkle(SenaryoOlayKaydi.OlayTipi_AsamaCikisi, $"Aşama geçişi: {GetAsamaAdi(onceki)} → {GetAsamaAdi(yeni)}");
        LogEkle(SenaryoOlayKaydi.OlayTipi_AsamaGirisi, $"{GetAsamaAdi(yeni)} — Başlangıç bakiyesi: {mevcutBakiye:N0} TL.");
        Debug.Log($"[SENARYO] Aşama: {onceki} -> {yeni}");
        AsamaAyariniUygula();
        UI_Guncelle();
        GecisSartGuncelle();
        SartMetinleriniGuncelle();
    }

    public void ManuelGecis(int asama) => AsamaGecir((SenaryoAsama)Mathf.Clamp(asama, 1, 7));

    public void LogEkle(string olayTipi, string aciklama, int? forAsamaNo = null)
    {
        int asamaNo = forAsamaNo ?? (int)mevcutAsama;
        string asamaAdi = asamaNo >= 1 && asamaNo <= 7 ? GetAsamaAdi((SenaryoAsama)asamaNo) : GetAsamaAdi();
        int netZr = toplamKayip - toplamKazanc;
        oturumLogu.Add(new SenaryoOlayKaydi(toplamSpin, asamaNo, asamaAdi, olayTipi, aciklama, mevcutBakiye, toplamYatirilanOturum, netZr));
        KaydetOturumLogu();
    }

    private const string PP_SENARYO_OTURUM_LOGU = "PP_SENARYO_OTURUM_LOGU";
    private string OturumLoguKey() => PP_SENARYO_OTURUM_LOGU + "_" + (GameManager.I?.ActivePlayer?.playerId ?? "");

    private void KaydetOturumLogu()
    {
        string key = OturumLoguKey();
        if (string.IsNullOrEmpty(key) || key == PP_SENARYO_OTURUM_LOGU + "_") return;
        var wrap = new SenaryoOturumLoguWrapper { kayitlar = oturumLogu.ToArray() };
        string json = JsonUtility.ToJson(wrap);
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();
    }

    private void OturumLoguYukle()
    {
        string key = OturumLoguKey();
        if (string.IsNullOrEmpty(key) || key == PP_SENARYO_OTURUM_LOGU + "_") return;
        string json = PlayerPrefs.GetString(key, "");
        if (string.IsNullOrEmpty(json)) return;
        try
        {
            var wrap = JsonUtility.FromJson<SenaryoOturumLoguWrapper>(json);
            if (wrap != null && wrap.kayitlar != null)
            {
                oturumLogu.Clear();
                oturumLogu.AddRange(wrap.kayitlar);
            }
        }
        catch (System.Exception e) { UnityEngine.Debug.LogWarning("OturumLogu yükleme: " + e.Message); }
    }

    /// <summary>Log sahnesi veya dışarıdan senaryo logunu almak için. Önce I'dan, yoksa PlayerPrefs'ten.</summary>
    public static List<SenaryoOlayKaydi> GetOturumLoguStatik(string playerId)
    {
        if (I != null && GameManager.I?.ActivePlayer?.playerId == playerId)
            return I.GetOturumLogu();
        string key = PP_SENARYO_OTURUM_LOGU + "_" + (playerId ?? "");
        if (string.IsNullOrEmpty(key) || key == PP_SENARYO_OTURUM_LOGU + "_") return new List<SenaryoOlayKaydi>();
        string json = PlayerPrefs.GetString(key, "");
        if (string.IsNullOrEmpty(json)) return new List<SenaryoOlayKaydi>();
        try
        {
            var wrap = JsonUtility.FromJson<SenaryoOturumLoguWrapper>(json);
            if (wrap != null && wrap.kayitlar != null)
                return new List<SenaryoOlayKaydi>(wrap.kayitlar);
        }
        catch { }
        return new List<SenaryoOlayKaydi>();
    }

    public List<SenaryoOlayKaydi> GetOturumLogu() => new List<SenaryoOlayKaydi>(oturumLogu);

    /// <summary>Bonus oyuna girildiğinde logla: giriş bakiyesi ve tür (scatter / satın alındı).</summary>
    public void LogBonusGirisi(int girisBakiyesi, bool satinAlindi)
    {
        _sonBonusGirisBakiyesi = girisBakiyesi;
        string tetikleyici = satinAlindi ? "Satın alma" : "Scatter (dönüş)";
        LogEkle(SenaryoOlayKaydi.OlayTipi_BonusGirisi, $"Bonus oyunu girişi. Bakiye: {girisBakiyesi:N0} TL. Tetikleyici: {tetikleyici}.");
    }

    /// <summary>Bonus oyun bittiğinde logla: çıkış bakiyesi ve bonus kazancı.</summary>
    public void LogBonusCikisi(int cikisBakiyesi)
    {
        int bonusKazanci = cikisBakiyesi - _sonBonusGirisBakiyesi;
        LogEkle(SenaryoOlayKaydi.OlayTipi_BonusCikisi, $"Bonus oyunu sonu. Kapanış bakiyesi: {cikisBakiyesi:N0} TL. Net kazanç: {bonusKazanci:N0} TL.");
    }

    public void BahisArtirimiYapildi()
    {
        bahisArtirimSayisi++;
        sonBahisArtirimSpinIndex = toplamSpin;

        // Bir önceki spin net kayıpla bittiyse ve hemen ardından bahis artırıldıysa, tilt benzeri davranışı logla.
        if (senaryoAktif && _sonSpinNet < 0)
        {
            int netKayipSonSpin = -_sonSpinNet;
            LogEkle(SenaryoOlayKaydi.OlayTipi_Uyari_TiltBahisArtisi,
                $"Bir önceki spin {netKayipSonSpin:N0} TL net kayıpla bitti ve hemen ardından bahis artırıldı. Bu davranış, kaybı hızlıca geri kazanma isteğine işaret edebilir.");
        }
    }
    public void BonusGoruldu()
    {
        bonusGorulduSayisi++;
        if (GameManager.I?.ActivePlayer != null)
        {
            GameManager.I.ActivePlayer.totalBonusEntries++;
            GameManager.SaveProfiles(GameManager.I.Profiles);
            PlayerPrefs.SetInt(SenaryoBonusGorulduKey(), bonusGorulduSayisi);
            PlayerPrefs.Save();
        }
    }
    public void BonusSatinAlindi()
    {
        bonusSatinAlmaSayisi++;

        // Kısa sürede veya toplamda çok sayıda bonus satın alma davranışı için eşik bazlı uyarı.
        if (senaryoAktif && (bonusSatinAlmaSayisi == 3 || bonusSatinAlmaSayisi == 5 || bonusSatinAlmaSayisi == 10))
        {
            LogEkle(SenaryoOlayKaydi.OlayTipi_Uyari_SikBonusAlimi,
                $"Bu oturumda {bonusSatinAlmaSayisi} kez bonus satın alındı. Sık bonus alma davranışı, riski ve kayıp hızını artırabilir.");
        }
    }

    public float GetKazancOrani()
    {
        switch (mevcutAsama)
        {
            case SenaryoAsama.Asama1_IsindirmaUmut: return asama1_oran / 100f;
            case SenaryoAsama.Asama2_KontrolBende: return asama2_oran / 100f;
            case SenaryoAsama.Asama3_AzDahaKayipKovalama: return asama3_oran / 100f;
            case SenaryoAsama.Asama4_BakiyeTukenis: return asama4_oran / 100f;
            case SenaryoAsama.Asama5_BonusZirve: return asama5_oran / 100f;
            case SenaryoAsama.Asama6_GercekKayip: return 0.05f;
            case SenaryoAsama.Asama7_Finale: return 0f;
            default: return 0.5f;
        }
    }

    public string GetAsamaAdi() => GetAsamaAdi(mevcutAsama);
    public string GetAsamaAdi(SenaryoAsama asama)
    {
        switch (asama)
        {
            case SenaryoAsama.Asama1_IsindirmaUmut: return "1 - Isındırma / Umut";
            case SenaryoAsama.Asama2_KontrolBende: return "2 - Kontrol bende";
            case SenaryoAsama.Asama3_AzDahaKayipKovalama: return "3 - Az daha / Kayıp kovalama";
            case SenaryoAsama.Asama4_BakiyeTukenis: return "4 - Bakiye tükenişi";
            case SenaryoAsama.Asama5_BonusZirve: return "5 - Bonus zirve";
            case SenaryoAsama.Asama6_GercekKayip: return "6 - Gerçek kayıp";
            case SenaryoAsama.Asama7_Finale: return "7 - Finale";
            default: return "Bilinmiyor";
        }
    }
    

    public void SpinTamamlandi(int kazanc, int bahis)
    {
        toplamSpin++;
        if (kazanc > bahis) toplamKazanc += (kazanc - bahis);
        else toplamKayip += (bahis - kazanc);

        // Bu spin net sonucu (kazanç eksi bahis); bahis artışı sonrası tilt davranışını tespit etmek için saklanır.
        _sonSpinNet = kazanc - bahis;

        // Net kayıp belirli eşikleri aştığında (500, 1.000, 2.000...) bunu bir kez logla.
        if (senaryoAktif && toplamKayip - toplamKazanc >= 0 && _netKayipEsikIndex < _netKayipEsikTL.Length)
        {
            int netZarar = toplamKayip - toplamKazanc;
            while (_netKayipEsikIndex < _netKayipEsikTL.Length && netZarar >= _netKayipEsikTL[_netKayipEsikIndex])
            {
                int esik = _netKayipEsikTL[_netKayipEsikIndex];
                LogEkle(SenaryoOlayKaydi.OlayTipi_Uyari_NetKayipEsigi,
                    $"Net kayıp eşiği aşıldı: Bu oturumda toplam yaklaşık {esik:N0} TL ve üzeri net kayıp oluştu. Güncel net: {netZarar:N0} TL.");
                _netKayipEsikIndex++;
            }
        }

        // Senaryo 1–2: üst üste ödeme – eşik ve zorunlu boş sayısı rastgele; bazen tetiklenmez (öngörülebilir pattern azalır)
        if (mevcutAsama == SenaryoAsama.Asama1_IsindirmaUmut || mevcutAsama == SenaryoAsama.Asama2_KontrolBende)
        {
            if (kazanc > 0)
            {
                consecutivePayCount++;
                int esik = UnityEngine.Random.Range(UST_USTE_ODEME_ESIK_MIN, UST_USTE_ODEME_ESIK_MAX + 1);
                if (consecutivePayCount >= esik && UnityEngine.Random.value < ZORUNLU_BOS_TETIK_OLASILIK)
                {
                    forcedNoPayKalan = UnityEngine.Random.Range(ZORUNLU_BOS_MIN, ZORUNLU_BOS_MAX + 1);
                    consecutivePayCount = 0;
                }
            }
            else
            {
                consecutivePayCount = 0;
                if (forcedNoPayKalan > 0) forcedNoPayKalan--;
            }
        }

        if (GameManager.I?.ActivePlayer != null)
            mevcutBakiye = GameManager.I.ActivePlayer.balance;

        if (mevcutAsama == SenaryoAsama.Asama1_IsindirmaUmut && mevcutBakiye >= gecis1_bakiyeUstTL)
            _asama1BakiyeUstTLUlasti = true;
        if (mevcutAsama == SenaryoAsama.Asama2_KontrolBende && mevcutBakiye >= gecis2_bakiyeUstTL)
            _asama2BakiyeUstTLUlasti = true;

        // Her 50 spinde veya ilk anlamlı veride RTP oranını logla (toplam kazanç / toplam bahis).
        var p = GameManager.I?.ActivePlayer;
        if (p != null && p.totalWagered > 0 && (toplamSpin % 50 == 0 || toplamSpin == 1))
        {
            float rtpYuzde = 100f * (float)p.totalWon / (float)p.totalWagered;
            LogEkle(SenaryoOlayKaydi.OlayTipi_RTPOrani, $"RTP: %{rtpYuzde:F1} (toplam kazanç: {p.totalWon:N0} TL, toplam bahis: {p.totalWagered:N0} TL). Spin: {toplamSpin}.");
        }

        if (!_sonSpinBonusTetikledi)
            _spinsSinceLastScatterOturum++;
        _sonSpinBonusTetikledi = false;

        // OturumKayitcisi: spin verilerini kaydet + dışarıya event'i ilet
        OturumKayitcisi.SpinKaydet(toplamSpin, bahis, kazanc, mevcutBakiye);
        OnSpinTamamlandiEvent?.Invoke(toplamSpin, kazanc, bahis);

        // Finale aşamasında her spin sonrası oyuncuyu net sonuçla yüzleştirici bir mesaj göster.
        if (mevcutAsama == SenaryoAsama.Asama7_Finale && cikisIcinBilgiMetni != null)
        {
            int netZararToplam = toplamKayip - toplamKazanc;
            cikisIcinBilgiMetni.text = $"Bu spinde {bahis:N0} TL kaybettin. Toplam net zarar: {netZararToplam:N0} TL.";
        }

        AsamaAyariniUygula();
        if (toplamSpin > 0 && toplamSpin % 50 == 0)
            LogEkle(SenaryoOlayKaydi.OlayTipi_AsamaAralikOzeti, $"Aralık özeti — Toplam spin: {toplamSpin}. Aşama: {GetAsamaAdi()}. Bakiye: {mevcutBakiye:N0} TL. Net (toplam kayıp − kazanç): {toplamKayip - toplamKazanc:N0} TL.");
        UI_Guncelle();
        // Senaryolu sahnede (I == this) her spin sonrası aşama geçişi kontrol edilsin; 2 şart sağlanınca otomatik geçiş (gelistirmeModu kapalıysa).
        if (this == I || (!gelistirmeModu && senaryoAktif))
            AsamaGecisiKontrol();

        // Bakiye sıfırlandığında oyunu sonlandır ve doğrudan istatistik (log) sahnesine geç. Önce profildeki bakiyeyi güncelleyip kaydet ki log sayfası doğru görsün.
        if (GameManager.I != null && mevcutBakiye <= 0)
        {
            if (GameManager.I.ActivePlayer != null)
            {
                GameManager.I.ActivePlayer.balance = mevcutBakiye;
                GameManager.SaveProfiles(GameManager.I.Profiles);
            }
            GameManager.I.LoadScene("05_LogScane");
        }
    }

    /// <summary>Bonus (scatter) tetiklendiğinde çağrılır; oturum sayacını sıfırlar (scatter garantisi sadece bu oturumda sayılır).</summary>
    public void OnBonusTriggered()
    {
        sonBonusTriggerSpinIndex = toplamSpin;
        _spinsSinceLastScatterOturum = 0;
        _sonSpinBonusTetikledi = true;
    }

    /// <summary>Bu oturumda son scatter'dan bu yana geçen spin; oyun açılışında 0 (ilk spinde garantiyi tetiklemez).</summary>
    public int SpinsSinceLastScatter()
    {
        return _spinsSinceLastScatterOturum;
    }

    /// <summary>Senaryo 1 veya 2'de zorunlu boş spin (üst üste ödeme sonrası) için true döner.</summary>
    public bool ShouldForceNoPaySenaryo12()
    {
        return (mevcutAsama == SenaryoAsama.Asama1_IsindirmaUmut || mevcutAsama == SenaryoAsama.Asama2_KontrolBende) && forcedNoPayKalan > 0;
    }

    public void BakiyeYukle(int tutar)
    {
        if (yuklemeSayisi >= 3) return;
        mevcutBakiye += tutar;
        toplamYatirilanOturum += tutar;
        yuklemeSayisi++;
        yuklemeSonrasiIlkSpinIndex = toplamSpin;
        LogEkle(SenaryoOlayKaydi.OlayTipi_BakiyeYuklemeYapildi, $"Bakiye yüklemesi: {tutar:N0} TL. Toplam yükleme sayısı: {yuklemeSayisi}. Güncel bakiye: {mevcutBakiye:N0} TL.");
        UI_Guncelle();
    }

    public void AutoSpinBaslat()
    {
        int secim = autoSpinDropdown != null ? autoSpinDropdown.value : 3;
        if (secim == 5) { autoSpinAktif = true; kalanAutoSpin = -1; }
        else { autoSpinAktif = true; kalanAutoSpin = new int[] { 10, 25, 50, 100, 200 }[secim]; }
        UI_Guncelle();
    }

    public void AutoSpinDurdur()
    {
        autoSpinAktif = false;
        kalanAutoSpin = 0;
        UI_Guncelle();
    }

    [Tooltip("Otomatik spin: her spin tetiklemesi sonrası ekstra bekleme (saniye). 0 = spin biter bitmez sonraki tetiklenir.")]
    public float otomatikSpinAralikSaniye = 0f;

    System.Collections.IEnumerator AutoSpinRoutine()
    {
        coroutineCalisiyor = true;
        var oyun = FindObjectOfType<OyunYoneticisi>();
        while (kalanAutoSpin > 0 && oyun != null && !oyun.BonusAktifMi)
        {
            oyun.SpinButon();
            while (oyun.SpinCalisiyorMu)
                yield return null;
            kalanAutoSpin--;
            if (kalanAutoSpin == 0) AutoSpinDurdur();
            float ara = Mathf.Max(0f, otomatikSpinAralikSaniye);
            if (ara > 0f)
                yield return new WaitForSeconds(ara);
        }
        coroutineCalisiyor = false;
    }

    void GecisSartGuncelle()
    {
        if (gecisSartText != null)
        {
            if (mevcutAsama == SenaryoAsama.Asama7_Finale)
                gecisSartText.text = "SON AŞAMA";
            else
            {
                CikisSartlariniDegerlendir((int)mevcutAsama, out var tam, out var kal);
                var sb = new System.Text.StringBuilder();
                if (tam.Count > 0)
                    sb.Append("Tamamlanan: ").Append(string.Join("\n", tam)).Append("\n");
                if (kal.Count > 0)
                    sb.Append("Kalan: ").Append(string.Join("\n", kal));
                else
                    sb.Append("Kalan: —");
                gecisSartText.text = sb.ToString();
                gecisSartText.enableWordWrapping = true;
                gecisSartText.overflowMode = TextOverflowModes.Overflow;
            }
        }
        SartMetinleriniGuncelle();
    }

    /// <summary>Bonus oyun başladığında/ bittiğinde çağrılır; bonus sırasında "Net" metni gizlenir.</summary>
    public void SetBonusAktif(bool aktif)
    {
        if (_bonusAktif == aktif) return;
        _bonusAktif = aktif;
        UI_Guncelle();
    }

    public void UI_Guncelle()
    {
        // UI çizmeden hemen önce bakiyeyi oyundaki gerçek ekonomi durumundan senkronla.
        var oyun = FindObjectOfType<OyunYoneticisi>();
        if (oyun != null)
            mevcutBakiye = oyun.BotIcinBakiye;

        if (asamaText) asamaText.text = $"Aşama: {GetAsamaAdi()}";
        if (spinText) spinText.text = $"Spin: {toplamSpin}";
        // Net / istatistik metni senaryolu oyunda gösterilmez; sadece log sahnesinde yer alır.
        if (bakiyeText) bakiyeText.text = "Bakiye: " + OyunFormatServisi.FormatTL(mevcutBakiye);
        if (mevcutAsamaText) mevcutAsamaText.text = GetAsamaAdi();

        if (autoSpinBaslatBtn) autoSpinBaslatBtn.interactable = !autoSpinAktif;
        if (autoSpinDurdurBtn) autoSpinDurdurBtn.interactable = autoSpinAktif;
        if (autoSpinKalanText) autoSpinKalanText.text = autoSpinAktif ? (kalanAutoSpin == -1 ? "Sonsuz" : $"Kalan: {kalanAutoSpin}") : "";

        // Senaryolu sahnede slider/admin panel yok; tüm değerler seçili aşama ve sahne config'inden gelir. Bonus içinde ödenebilir tutar havuzdan güncel gösterilsin.
        if (mevcutAyarlarMetni != null)
        {
            if (oyun != null)
            {
                int zorluk = oyun.zorlukSeviyesi;
                int scatterYuzde = Mathf.RoundToInt(oyun.scatterChanceNormal * 100f);
                int carpanYuzde = Mathf.RoundToInt(oyun.carpanUretimOlasiligi * 100f);
                int odenebilirTutar = oyun.GetSpinOdenebilirLimit();
                string carpanDurum = oyun.carpanUretimiAktif ? "Açık" : "Kapalı";
                mevcutAyarlarMetni.text = $"Zorluk: {zorluk}\nScatter düşme: %{scatterYuzde}\nÇarpan düşme: %{carpanYuzde} ({carpanDurum})\nÖdenebilir tutar: {odenebilirTutar:N0} TL";
            }
            else
                mevcutAyarlarMetni.text = "Mevcut ayarlar yükleniyor...";
        }

        GecisSartGuncelle();
    }

    private void OnApplicationQuit()
    {
        SenaryoAsamaKaydet();
    }

    private void OnDisable()
    {
        // sahneLoaded aboneliğini temizle + aşama kaydet
        SceneManager.sceneLoaded -= SahneYuklendi_KalitimiTemizle;
        SenaryoAsamaKaydet();
    }

    private void SenaryoAsamaKaydet()
    {
        if (GameManager.I?.ActivePlayer == null) return;
        PlayerPrefs.SetInt(SenaryoAsamaKey(), (int)mevcutAsama);
        PlayerPrefs.SetInt(SenaryoAsamaGirisSpinKey(), asamaGirisSpinIndex);
        PlayerPrefs.SetInt(SenaryoBonusGorulduKey(), bonusGorulduSayisi);
        PlayerPrefs.SetInt(SenaryoBahisArtirimKey(), bahisArtirimSayisi);
        PlayerPrefs.Save();
    }

    public void Reset()
    {
        _bonusAktif = false;
        mevcutAsama = SenaryoAsama.Asama1_IsindirmaUmut;
        toplamSpin = toplamKazanc = toplamKayip = 0;
        mevcutBakiye = ilkBakiye;
        baslangicZamani = Time.time;
        yuklemeSayisi = 1;
        bahisArtirimSayisi = 0;
        bonusGorulduSayisi = 0;
        bonusSatinAlmaSayisi = 0;
        toplamYatirilanOturum = ilkBakiye;
        asamaGirisSpinIndex = 0;
        sonBahisArtirimSpinIndex = -1;
        yuklemeSonrasiIlkSpinIndex = -1;
        sonBonusTriggerSpinIndex = -1;
        _spinsSinceLastScatterOturum = 0;
        _sonSpinBonusTetikledi = false;
        consecutivePayCount = 0;
        forcedNoPayKalan = 0;
        _asama1BakiyeUstTLUlasti = false;
        _asama2BakiyeUstTLUlasti = false;
        oturumLogu.Clear();
        PlayerPrefs.DeleteKey(SenaryoAsamaKey());
        PlayerPrefs.DeleteKey(SenaryoAsamaGirisSpinKey());
        PlayerPrefs.DeleteKey(OturumLoguKey());
        PlayerPrefs.DeleteKey(SenaryoBonusGorulduKey());
        PlayerPrefs.DeleteKey(SenaryoBahisArtirimKey());
        PlayerPrefs.Save();
        AutoSpinDurdur();
        UI_Guncelle();
    }
}
