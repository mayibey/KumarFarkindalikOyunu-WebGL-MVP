using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Sol-sabit "Anlatıcı Şerit" HTML iframe paneli için Unity tarafı köprüsü.
/// 7 aşamalı manipülasyon hikayesini yönetir: aşama → 10 spin → bir sonraki aşama.
/// Her aşama OdemeEgilimi + MaxOdeme profilini OyunYoneticisi'ye uygular.
/// HTML panel ⟷ Unity arası postMessage / SendMessage protokolü.
/// </summary>
public class AnlaticiSeritKopru : MonoBehaviour
{
    [DllImport("__Internal")] private static extern void AnlaticiPaneliAc(string url);
    [DllImport("__Internal")] private static extern void AnlaticiPaneliKapat();
    [DllImport("__Internal")] private static extern void AnlaticiPaneliGuncelle(string json);
    [DllImport("__Internal")] private static extern void AnlaticiPaneliGizle();
    [DllImport("__Internal")] private static extern void AnlaticiPaneliGoster();
    [DllImport("__Internal")] private static extern void AnlaticiPaneliArkayaAt();
    [DllImport("__Internal")] private static extern void AnlaticiPaneliOneAl();
    [DllImport("__Internal")] private static extern void HosgeldinKutusunuAc(string metin);
    [DllImport("__Internal")] private static extern void BonusBitisPopupAc(int tutar);
    [DllImport("__Internal")] private static extern void BonusBitisPopupKapat();
    [DllImport("__Internal")] private static extern void HavaiFisekBaslat();

    private static void HosgeldinKutusunuAcGuvenli(string metin)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        HosgeldinKutusunuAc(metin);
#else
        Debug.Log("[HosgeldinKutusu] " + metin);
#endif
    }

    /// <summary>Bonus oyun bitiminde modern DOM popup'ı açar (alkış sesi C# tarafında ayrı çalar).
    /// BonusUIServisi.ShowBonusEndMessage tarafından çağrılır; popup TAMAM tıklanana kadar
    /// <see cref="BonusBitisOnaylandi"/> false kalır.</summary>
    public static void BonusBitisGoster(int tutar)
    {
        // Bonus 0 TL ödedi → "🎉 TEBRİKLER 🎉" popup + havai fişek YAPAY/komik olur (kayıp horn ile çelişir).
        // Atla: kayıp horn zaten BonusUIServisi tarafında çalmış oldu, A5_S5 modal direkt açılır
        // ("Geri aldığı 0 TL, yatırdığının %0'i" → maksimum pedagojik farkındalık).
        if (tutar <= 0)
        {
            Debug.Log("[BonusBitis] Tutar=0, popup ve havai fişek atlandı (gerçek kayıp anı).");
            BonusBitisOnaylandi = true;
            BonusBitisAcik = false;
            return;
        }
        BonusBitisOnaylandi = false;
        BonusBitisAcik = true; // Spin butonu bu süre boyunca engellensin
#if UNITY_WEBGL && !UNITY_EDITOR
        try { BonusBitisPopupAc(tutar); }
        catch (System.Exception e) { Debug.LogWarning("[BonusBitis] hata: " + e.Message); }
        // Tebrikler popup açılışında havai fişek (3sn otomatik dururuyor — ScriptedBonusTuzagiPopup ile aynı görsel dil).
        try { HavaiFisekBaslat(); }
        catch (System.Exception e) { Debug.LogWarning("[BonusBitis-HavaiFisek] hata: " + e.Message); }
#else
        Debug.Log("[BonusBitis] Tutar: " + tutar);
        // Editor fallback: anında onay (test akışı bloklanmasın)
        BonusBitisOnaylandi = true;
        BonusBitisAcik = false;
#endif
    }

    /// <summary>JS popup'ı manuel kapatma (defansif — sahne değişimi vb.).</summary>
    public static void BonusBitisGizle()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try { BonusBitisPopupKapat(); }
        catch (System.Exception e) { Debug.LogWarning("[BonusBitis] kapat hata: " + e.Message); }
#endif
    }

    /// <summary>JS tarafında TAMAM tıklandığında SendMessage ile true olur — coroutine devam eder.</summary>
    public static bool BonusBitisOnaylandi { get; private set; }

    /// <summary>Bonus bitiş popup'ı görünür mü? BonusBitisGoster→true, BonusBitisOnayla→false.
    /// SpinButonImpl ve OyunUIGuncellemeServisi bu flag'e bakıp Spin butonunu engeller.</summary>
    public static bool BonusBitisAcik { get; private set; }

    /// <summary>A* özel akış coroutine'i (modal + animasyon + delay) çalışıyor mu?
    /// Coroutine'in EN BAŞINDA true set edilir (modal henüz açılmadan, WaitForSeconds gibi delay öncesinde),
    /// finally bloğunda false. SpinButonImpl ve HerhangiOverlayAcik bu flag'i kontrol eder →
    /// kullanıcı modal açılma anını beklerken spin atamaz (race condition kapanır).
    /// PreA1, A2-A5 geçişler, A4S1 yıldız modal, A4S5 ×100 modal, BasaArayis, Dongu, BorcSonrasiModal,
    /// BahisAnimasyonu coroutine'leri try/finally ile bu flag'i güvenle yönetir.</summary>
    public static bool AnlaticiOzelAkisAktif = false;

    /// <summary>11 A* coroutine finally'lerinden çağrılır: flag false + UI yenileme.
    /// UI_Guncelle/ButonDurumu çağrısı KRİTİK çünkü BahisAnimasyonu gibi bahis-değişen coroutine'ler
    /// bittikten sonra onEconomyChanged tetiklenmez → spinButon.interactable=false takılı kalır.
    /// Helper bunu garanti eder; A3/A6 spin kilit bug'ının fix'i.</summary>
    private void AnlaticiOzelAkisBitir()
    {
        AnlaticiOzelAkisAktif = false;
        _oy?.UIYenile();
    }

    /// <summary>JS SendMessage handler — GameObject "AnlaticiSeritKopru" üzerinde çağrılır.</summary>
    public void BonusBitisOnayla()
    {
        BonusBitisOnaylandi = true;
        BonusBitisAcik = false;
        Debug.Log("[BonusBitis] Kullanıcı TAMAM tıkladı, coroutine devam.");
    }

    /// <summary>Senaryolu eğitim modunda herhangi bir overlay (modal/balon/popup/yükleme/final) açık mı?
    /// SpinButonImpl ve OyunUIGuncellemeServisi tek satırda kontrol eder. Senaryo dışı sahnelerde
    /// (admin, tutorial) her zaman false → mevcut davranış değişmez.</summary>
    public static bool HerhangiOverlayAcik =>
        SenaryoEgitimiAktif && (
            Senaryo.Scripted.ScriptedYuklemePaneli.IsAcik
            || Senaryo.Scripted.ScriptedFinalEkrani.IsAcik
            || Senaryo.Scripted.ScriptedBonusTuzagiPopup.IsAcik
            || Senaryo.Scripted.ScriptedBonusOyunUygulayici.IsAcik
            || Senaryo.Scripted.ScriptedModalKopru.ModalAcik
            || Senaryo.Scripted.ScriptedDusunceBalonu.BalonAcik
            || BonusBitisAcik
            || AnlaticiOzelAkisAktif
        );

    /// <summary>Sol panel iframe'ini Unity Canvas'ın ARKASINA gönderir (z:50). Modal'ın "sol panel"
    /// anlattığı durumlarda kullanılır — Gizle yerine arkada görünür kalsın.</summary>
    public void ArkayaAt()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try { AnlaticiPaneliArkayaAt(); }
        catch (System.Exception e) { Debug.LogWarning("[Anlatici] ArkayaAt hata: " + e.Message); }
#else
        Debug.Log("[Anlatici] ArkayaAt (Editor fallback — sadece WebGL'de etkili).");
#endif
    }

    /// <summary>ArkayaAt ile arkaya alınan paneli normal z:100'e geri döndürür.</summary>
    public void OneAl()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try { AnlaticiPaneliOneAl(); }
        catch (System.Exception e) { Debug.LogWarning("[Anlatici] OneAl hata: " + e.Message); }
#else
        Debug.Log("[Anlatici] OneAl (Editor fallback — sadece WebGL'de etkili).");
#endif
    }

    private OyunYoneticisi _oy;
    private int _aktifAsama = 0;
    private int _aktifSpin = 0;
    private int _toplamSpin = 0;
    /// <summary>A7 final ekranı toplam spin istatistiği için public erişim.</summary>
    public int ToplamSpin => _toplamSpin;
    /// <summary>A6 girişi sırasında ScriptedYuklemePaneli'i bir defalık otomatik açma flag'i.</summary>
    private bool _yuklemePaneliAcildiBuOturum = false;
    /// <summary>A6 sonu döngü modal'ı bir kez tetiklendi mi (modal sonu Tukenis çağrılır).</summary>
    private bool _donguModalGosterildi = false;
    /// <summary>Pre-A1 karşılama modal'ı bir kez gösterildi mi (sahne girişinde).</summary>
    private bool _preA1ModalGosterildi = false;
    /// <summary>A1 → A2 geçiş modal'ı bir kez gösterildi mi.</summary>
    private bool _a2GecisModalGosterildi = false;
    /// <summary>A2 → A3 geçiş modal'ı bir kez gösterildi mi.</summary>
    private bool _a3GecisModalGosterildi = false;
    /// <summary>A3 → A4 geçiş modal'ı bir kez gösterildi mi.</summary>
    private bool _a4GecisModalGosterildi = false;
    /// <summary>A4 → A5 geçiş modal'ı bir kez gösterildi mi.</summary>
    private bool _a5GecisModalGosterildi = false;
    /// <summary>A3 Spin 6 sonu bahis 2500'e bir kez yükseltildi mi.</summary>
    private bool _a3BahisYukseltildi = false;
    /// <summary>A4 Spin 5 ×100 çarpan modal'ı bir kez gösterildi mi.</summary>
    private bool _a4S5CarpanModalGosterildi = false;
    /// <summary>A4 Spin 1 yıldız döndürme + modal bir kez gösterildi mi (eskiden A2 S3'teydi).</summary>
    private bool _a4S1DonmeGosterildi = false;
    /// <summary>A4 S1 yıldız döndürme: aktif coroutine'ler + GameObject'ler.</summary>
    private readonly System.Collections.Generic.List<Coroutine> _aktifDansCoroutineleri = new System.Collections.Generic.List<Coroutine>();
    private readonly System.Collections.Generic.List<GameObject> _aktifYildizlar = new System.Collections.Generic.List<GameObject>();
    /// <summary>OyunYoneticisi.Spin.cs ÖNCE modal kontrolü için: aktif spin index'i (0-indexed).</summary>
    public int AktifSpin => _aktifSpin;
    private long _baslangicBakiye = 0;
    private int _sonUygulananAsama = -1; // YENI: aşama değişimi tespiti için
    private long _sonBakiye = 50000; // bir önceki spin sonu bakiye — spin başına net delta için
    private readonly List<int> _asamaSpinNet = new List<int>(); // mevcut aşamadaki spin başına net (+/-) TL
    private const int BASLANGIC_BAKIYE = 50000;
    private const int ASAMA7_GORSEL_MAX_CUBUK = 10; // Asama 7 dinamik (999 spin) — HTML max 10 çubuk göster
    private static AnlaticiSeritKopru _ornek;

    /// <summary>Aşama bazlı önerilen bahis (yeniAsama geçişinde set edilir, kullanıcı sonra manuel değiştirebilir).
    /// Pedagojik eğri: 50K → 60K → 75K → 70K → 55K → 30K → 10K → 0 (~61 spin).</summary>
    private static readonly int[] _onerilenBahisler = new int[] { 500, 1000, 1500, 2500, 4000, 10000, 1500 };

    /// <summary>Aşama başına spin eşiği. A6 hızlı yıkım: 5 spin × 10K bahis = 50K borç tükenir. Aşama 7 = 999 (dinamik).</summary>
    private static readonly int[] _asamaSpinSayisi = new int[] { 10, 10, 8, 8, 10, 5, 999 };

    [System.Serializable]
    public class AsamaAyari
    {
        public int egilim;
        public float maxCarpani;
        public bool nearMiss;
    }

    // Egilim 0-100 arası clamp'lenir (motor üst sınırı). Kazandırma maxCarpan ile yapılır.
    private static readonly AsamaAyari[] _asamalar = new AsamaAyari[]
    {
        new AsamaAyari { egilim = 95, maxCarpani = 5.0f, nearMiss = false }, // 1 Isındırma ve Umut — çarpıcı kazanç
        new AsamaAyari { egilim = 90, maxCarpani = 3.5f, nearMiss = false }, // 2 Kontrol Bende Hissi — bol kazanç
        new AsamaAyari { egilim = 50, maxCarpani = 1.0f, nearMiss = true  }, // 3 Geri Kazanabilirim
        new AsamaAyari { egilim = 30, maxCarpani = 0.6f, nearMiss = true  }, // 4 Şansın Döndü
        new AsamaAyari { egilim = 20, maxCarpani = 0.4f, nearMiss = true  }, // 5 Sonu Düşünmeyen Kahraman
        new AsamaAyari { egilim = 15, maxCarpani = 0.3f, nearMiss = true  }, // 6 Başka Yerden Para Bulmalıyım
        new AsamaAyari { egilim = 5,  maxCarpani = 0.1f, nearMiss = true  }  // 7 Tükeniş
    };

    public static AnlaticiSeritKopru Ornek => _ornek;

    /// <summary>Senaryolu eğitim modu aktif mi (03_SenaryoluOyun sahnesinde true). OyunUIGuncellemeServisi
    /// ve OyunYoneticisi.UI bu flag'e bakıp bakiye yükle / bonus satın al butonlarını re-enable etmeyi
    /// atlar — kullanıcı senaryo akışında bu butonlara tıklayamaz.</summary>
    public static bool SenaryoEgitimiAktif { get; private set; }

    /// <summary>0-6 (Aşama 1-7). OyunYoneticisi.Admin/Spin tarafından reroll/bant override için okunur.</summary>
    public int AktifAsama => _aktifAsama;

    /// <summary>0-indexed; aşamadaki kaçıncı spin. SpinAtildi() içinde artar, aşama değişiminde 0'a sıfırlanır.
    /// ScriptedSpinYoneticisi tarafından "bu aşamadaki spin sırası" olarak okunur (SimuleEtVeKaydetImpl başında).</summary>
    public int AsamadakiSpinSayaci => _aktifSpin;

    void Awake()
    {
        // Defansif reset — önceki sahnedeki coroutine StopCoroutine ile zorla durdurulduysa flag takılı kalabilir.
        AnlaticiOzelAkisAktif = false;

        string aktifSahne = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (aktifSahne != "03_SenaryoluOyun")
        {
            Debug.Log("[AnlaticiSeritKopru] Aktif sahne " + aktifSahne + ", anlatici devre disi.");
            SenaryoEgitimiAktif = false;
            gameObject.SetActive(false);
            return;
        }
        SenaryoEgitimiAktif = true;
        _ornek = this;
    }
    void OnDestroy()
    {
        if (_ornek == this) { _ornek = null; SenaryoEgitimiAktif = false; }
        AnlaticiOzelAkisAktif = false; // defansif — sahne unload'ında takılı kalmasın
    }

    void Start()
    {
        // HOTFIX (Bug 1): AnlaticiSeritKopru sadece 03_SenaryoluOyun (build idx 2) için.
        // 04 Tutorial sahnesinde (build idx 3) çalıştığında anlatici.html iframe açılıp sol panel
        // "normal modu uygulandı" toast'ı gösteriyordu → pedagojik kafa karışıklığı. Guard ile atla.
        int sahneIdx = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        if (sahneIdx != 2)
        {
            Debug.Log($"[AnlaticiSeritKopru] Build idx {sahneIdx} → 03 dışı, Start atlanıyor (Tutorial sızıntısı önlendi)");
            gameObject.SetActive(false);
            return;
        }

        _oy = FindObjectOfType<OyunYoneticisi>();
        if (_oy == null)
        {
            Debug.LogError("[AnlaticiSeritKopru] OyunYoneticisi bulunamadi");
            return;
        }

        // ===== SAVE/LOAD RESTORE =====
        // KullaniciAdiModalKontrol "DEVAM ET" tıkladıysa flag set ediliyor (KumarRestoreModuActif=1).
        // Save varsa state'i geri yükle, default reset bloğunu atla.
        if (PlayerPrefs.GetInt("KumarRestoreModuActif", 0) == 1 && SaveLoadServisi.VarMi())
        {
            PlayerPrefs.DeleteKey("KumarRestoreModuActif");
            PlayerPrefs.Save();
            var save = SaveLoadServisi.Load();
            if (save != null)
            {
                RestoreDurumYukle(save);
                FinalSahneKurulumu(save.kullaniciAdi);
                return;
            }
            Debug.LogWarning("[SaveLoad] Restore mode aktif ama save null/bozuk → default reset uygulanacak.");
        }

        // KRİTİK: Eski admin senaryo preset'leri (Senaryo 1-5) Anlatıcı manipülasyonunu BYPASS ediyor.
        // Anlatıcı sahnesinde manipülasyonu Anlatıcı yönetir → Normal Oyun moduna geçir
        // (_senaryoPresetAktif=false, _aktifAdminSenaryoIndex=-1, policy reset, cache temizle).
        // AdminSetOdemeEgilimi/AdminSetMaxOdeme çağrıları sonrasında AdminNormalOyunUygula'nın
        // default değerleri (eğilim 65, max 0) anlatıcı profilinin üstüne yazılır.
        try
        {
            _oy.AdminNormalOyunUygula();
            Debug.Log("[Anlatici] AdminNormalOyunUygula çağrıldı: eski senaryo preset'leri devre dışı.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Anlatici] AdminNormalOyunUygula hatası: " + e.Message);
        }

        // Defansif: önceki oturumdan kalan _ustUsteKazancFaziAktif=true Anlatıcı eğilim hesabını bozabiliyor.
        try { _oy.AnlaticiKazancFaziniSifirla(); }
        catch (System.Exception e) { Debug.LogError("[Anlatici] AnlaticiKazancFaziniSifirla hatası: " + e.Message); }

        // KRİTİK: SenaryoYoneticisi paralel sistem — mevcutAsama Asama1/2'de iken ShouldForceNoPaySenaryo12()
        // Anlatıcı'nın eğilim ayarını bypass edip spinleri zorla 0 ödetir (forcedNoPayKalan random tetiklenir).
        // Anlatıcı sahnesinde manipülasyonu Anlatıcı yönettiği için SenaryoYoneticisi'ni Asama7_Finale'ye al
        // ve forcedNoPay sayacını sıfırla (Asama7'de ShouldForceNoPaySenaryo12 → false).
        if (SenaryoYoneticisi.I != null)
        {
            try
            {
                SenaryoYoneticisi.I.mevcutAsama = SenaryoYoneticisi.SenaryoAsama.Asama7_Finale;
                SenaryoYoneticisi.I.forcedNoPayKalan = 0;
                Debug.Log("[Anlatici] SenaryoYoneticisi devre dışı: mevcutAsama=Asama7_Finale, forcedNoPayKalan=0.");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Anlatici] SenaryoYoneticisi bypass hatası: " + e.Message);
            }
        }

        // Eğitim aracı: her sahne girişinde sıfırdan başla
        _aktifAsama = 0;
        _aktifSpin = 0;
        _toplamSpin = 0;
        _sonUygulananAsama = -1;
        _yuklemePaneliAcildiBuOturum = false;
        _donguModalGosterildi = false;
        _preA1ModalGosterildi = false;
        _a2GecisModalGosterildi = false;
        _a3GecisModalGosterildi = false;
        _a4GecisModalGosterildi = false;
        _a5GecisModalGosterildi = false;
        _a3BahisYukseltildi = false;
        _a4S5CarpanModalGosterildi = false;
        _a4S1DonmeGosterildi = false;
        Senaryo.Scripted.ScriptedYuklemePaneli.BorcAlindiSifirla();
        Senaryo.Scripted.ScriptedBonusOyunUygulayici.BonusYatirim = 0;
        Senaryo.Scripted.ScriptedBonusOyunUygulayici.BonusKazanc = 0;

        // Bakiye 50.000 TL'ye reset
        _oy.AnlaticiBakiyeyiSifirla(BASLANGIC_BAKIYE);
        _baslangicBakiye = BASLANGIC_BAKIYE;
        _sonBakiye = BASLANGIC_BAKIYE;
        _asamaSpinNet.Clear();

        AsamayiUygula(0);

        // Pre-A1 karşılama modal — sahne girişinde otomatik (tek seferlik flag)
        if (!_preA1ModalGosterildi)
        {
            _preA1ModalGosterildi = true;
            StartCoroutine(PreA1KarsilamaAkisi());
        }

        FinalSahneKurulumu(KullaniciVerileri.KullaniciAdi);
    }

    /// <summary>Start (default reset) ve restore akışlarının ortak sonu: HTML panel + hoşgeldin kutusu +
    /// senaryolu kontrol kilidi + IlkGuncelleme coroutine. Restore yolu bu metodu kendisi çağırır.</summary>
    private void FinalSahneKurulumu(string kullaniciAdi)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        AnlaticiPaneliAc("StreamingAssets/anlatici.html");
#else
        Debug.Log("[AnlaticiSeritKopru] Editor: HTML panel sadece WebGL'de açılır.");
#endif

        // Sağ üst hoşgeldin kutusu (runtime DOM, kullanıcı × ile kapatabilir).
        // Kullanıcı adı KullaniciAdiModalKontrol'da KullaniciVerileri.KullaniciAdi'ya yazılır (default "Misafir").
        if (string.IsNullOrWhiteSpace(kullaniciAdi)) kullaniciAdi = "Misafir";
        HosgeldinKutusunuAcGuvenli(kullaniciAdi);

        // Senaryolu eğitim modu: kullanıcı sadece SPIN butonuna basabilir.
        // Bahis +/-, bonus satın al, bakiye yükle, otomatik spin, ayarlar — hepsi devre dışı.
        // Borç Al (A6) ve modal TAMAM butonları runtime canvas'larda → bu devre dışı bırakma onları etkilemez.
        SenaryoluKontrolleriDevreDisiBirak();

        StartCoroutine(IlkGuncelleme());
    }

    private IEnumerator IlkGuncelleme()
    {
        yield return new WaitForSeconds(0.5f);
        Guncelle();
    }

    /// <summary>OyunYoneticisi.Spin spin tamamlandıktan sonra çağırır.</summary>
    public void SpinAtildi()
    {
        // A5 cazip bonus akışı sonrası: _aktifAsama=5 + BasaArayisAkisi A5BonusBittiBorcPaneliAc içinde
        // zaten yapıldı. Bonus tetikleyen spin'in (A5_S4) tamamlanma callback'i bu akışı bozmamalı —
        // _aktifSpin++/hedefSpin/asama ilerleme mantığını ATLA, sadece flag'i tüket.
        if (Senaryo.Scripted.ScriptedBonusOyunUygulayici.A5BonusBittiSpinTamamlandiAtla)
        {
            Senaryo.Scripted.ScriptedBonusOyunUygulayici.A5BonusBittiSpinTamamlandiAtla = false;
            Debug.Log("[Anlatici] SpinAtildi atlandı — A5 bonus bitti, A6 zaten set (A5BonusBittiBorcPaneliAc).");
            return;
        }

        // Bu spin'in net kazanç/kaybı: spin sonrası bakiye - spin öncesi bakiye
        // (NormalSpinAkisi tamamlandıktan sonra çağrılır → bakiye güncel)
        long simdikiBakiye = _oy != null ? _oy.BahisPanelMevcutBakiye() : _sonBakiye;
        int spinNet = (int)(simdikiBakiye - _sonBakiye);
        _sonBakiye = simdikiBakiye;
        _asamaSpinNet.Add(spinNet);

        _aktifSpin++;
        _toplamSpin++;

        // Aşama başına spin eşiği array'den okunur (Asama 3-4 = 8 spin, diğerleri 10, Asama 7 = 999 dinamik).
        int hedefSpin = _asamaSpinSayisi[Mathf.Clamp(_aktifAsama, 0, _asamaSpinSayisi.Length - 1)];

        Debug.Log($"[AnlaticiTANI] SpinTamamlandi sonu — _aktifSpin={_aktifSpin}, hedefSpin={hedefSpin}, _aktifAsama={_aktifAsama}");
        if (_aktifSpin >= hedefSpin)
        {
            Debug.Log($"[AnlaticiTANI] hedefSpin'e ulaşıldı, _aktifAsama < 6 mı? {_aktifAsama < 6}");
            if (_aktifAsama < 6)
            {
                // Önce son spin çubuğunu rengiyle göster (HTML render)
                Guncelle();
                _aktifAsama++;
                Debug.Log($"[AnlaticiTANI] _aktifAsama++ → {_aktifAsama} (yeni asama)");
                _aktifSpin = 0;
                _asamaSpinNet.Clear(); // yeni aşama, çubuklar sıfırlansın
                AsamayiUygula(_aktifAsama);

                // A1 → A2 geçişi: pedagojik modal "Birinci aşama tamamlandı, kontrol yanılsaması başlıyor"
                if (_aktifAsama == 1 && !_a2GecisModalGosterildi)
                {
                    _a2GecisModalGosterildi = true;
                    Debug.Log("[Anlatici] A1→A2 geçişi — A2GecisAkisi tetikleniyor.");
                    StartCoroutine(A2GecisAkisi());
                }
                // A2 → A3 geçişi: kayıp kovalama tuzağı uyarısı
                if (_aktifAsama == 2 && !_a3GecisModalGosterildi)
                {
                    _a3GecisModalGosterildi = true;
                    Debug.Log("[Anlatici] A2→A3 geçişi — A3GecisAkisi tetikleniyor.");
                    StartCoroutine(A3GecisAkisi());
                }
                // A3 → A4 geçişi: pes etme eşiği + manipülasyon vuruşu uyarısı
                if (_aktifAsama == 3 && !_a4GecisModalGosterildi)
                {
                    _a4GecisModalGosterildi = true;
                    Debug.Log("[Anlatici] A3→A4 geçişi — A4GecisAkisi tetikleniyor.");
                    StartCoroutine(A4GecisAkisi());
                }
                // A4 → A5 geçişi: bonus tuzağı uyarısı
                if (_aktifAsama == 4 && !_a5GecisModalGosterildi)
                {
                    _a5GecisModalGosterildi = true;
                    Debug.Log("[Anlatici] A4→A5 geçişi — A5GecisAkisi tetikleniyor.");
                    StartCoroutine(A5GecisAkisi());
                }

                // A5 → A6 geçişi (hedefSpin tamamlandı yolu): eğitmen "para arayışı" modal +
                // ScriptedYuklemePaneli akışını başlat. Bakiye yolu ile aynı pedagojik geçişi paylaşır.
                if (_aktifAsama == 5 && !_yuklemePaneliAcildiBuOturum)
                {
                    Debug.Log("[AnlaticiTANI] A5→A6 hedefSpin yolu — BasaArayisAkisi (modal + panel) tetikleniyor.");
                    StartCoroutine(BasaArayisAkisi());
                }
                else if (_aktifAsama == 5 && _yuklemePaneliAcildiBuOturum)
                {
                    Debug.Log("[AnlaticiTANI] A6'dayız ama panel zaten açılmış (flag true), atla.");
                }
            }
            else
            {
                Guncelle(); // son çubuğu son aşamada da göster
                Tukenis();
                return;
            }
        }
        else
        {
            // Aşama değişmedi ama bahis arada değişmiş olabilir — parametreleri yeniden uygula
            // (maxOdeme = bahis × maxCarpan formülü güncel bahisle hesaplansın)
            AsamayiUygula(_aktifAsama);
        }

        // BAKIYE YETERSİZLİĞİ — aşama bağlamına göre 4 farklı dal:
        //   A1-A4 erken kayıp (asama < 4)            → direkt A7 (uçurum)
        //   A5 sonu (asama == 4)                     → eğitmen "para arayışı" modal → Borç Al paneli
        //   A6 spin sırası (asama == 5 && spin > 0)  → eğitmen "döngü başa sardı" modal → Tukenis
        //   A6 girişi (asama == 5 && spin == 0)      → A7'ye atlama; panel zaten asama geçişinde açıldı
        //   A7 (asama == 6)                          → Tukenis dinamik
        if (_oy != null)
        {
            int simdiBakiye = (int)_oy.BahisPanelMevcutBakiye();
            int sonrakiBahis = _onerilenBahisler[Mathf.Clamp(_aktifAsama, 0, _onerilenBahisler.Length - 1)];
            if (simdiBakiye < sonrakiBahis)
            {
                if (_aktifAsama == 4)
                {
                    // A5 sonu yumuşak geçiş: önce eğitmen modal "para arayışı", ardından yükleme paneli
                    Debug.Log($"[Anlatici] A5 sonu bakiye yetersiz ({simdiBakiye} < {sonrakiBahis}) → A6 yumuşak geçiş + para arayışı modal.");
                    _aktifAsama = 5;
                    _aktifSpin = 0;
                    _asamaSpinNet.Clear();
                    AsamayiUygula(_aktifAsama);
                    StartCoroutine(BasaArayisAkisi());
                    return;
                }
                else if (_aktifAsama == 5 && _aktifSpin == 0)
                {
                    // A6 girişi (henüz spin atılmadı): yükleme paneli zaten asama geçişinde açıldı,
                    // Tukenis'e atlama. Spin başlamadığı için bu daldan zaten geçilmemeli ama defansif log.
                    Debug.Log($"[Anlatici] A6 girişi — bakiye yetersiz ({simdiBakiye} < {sonrakiBahis}) ama Borç Al paneli devreye girecek; A7'ye atlama atlanıyor.");
                }
                else if (_aktifAsama == 5 && _aktifSpin > 0)
                {
                    // A6 spin sırası — borç sonrası bakiye yine bitti → eğitmen "döngü" modal → Tukenis
                    Debug.Log($"[Anlatici] A6 sonu bakiye yine bitti ({simdiBakiye} < {sonrakiBahis}) → döngü modal + final ekran.");
                    StartCoroutine(DonguAkisi());
                    return;
                }
                else if (_aktifAsama < 4)
                {
                    Debug.Log($"[Anlatici] A1-A4 erken bakiye tükendi ({simdiBakiye} < {sonrakiBahis}), Aşama 7 (Tükeniş) zorla atlanıyor.");
                    _aktifAsama = 6;
                    _aktifSpin = 0;
                    _sonUygulananAsama = -1;
                    _asamaSpinNet.Clear();
                    AsamayiUygula(6);
                    Tukenis();
                    return;
                }
                else
                {
                    // Aşama 7'de bakiye yetersiz → dinamik Tukenis
                    Debug.Log($"[Anlatici] Aşama 7 bakiye tükendi ({simdiBakiye} < {sonrakiBahis}), Tukenis tetikleniyor.");
                    Tukenis();
                    return;
                }
            }
        }

        // SPIN-NO ÖZEL HOOK'lar (asama içinde belirli spin sonrası tetiklenen aksiyonlar):
        //   - A3 Spin 6 sonu: bahis otomatik 2500'e yükselt (kayıp kovalama tuzağı pekişir).
        //   - A4 Spin 5 sonu: ×100 çarpan modali (manipülasyon vuruşu pedagojik vurgu).
        if (_aktifAsama == 3 && _aktifSpin == 1 && !_a4S1DonmeGosterildi)
        {
            // A4 Spin 1 sonu: 3 yıldız NearMiss — yıldızları döndür + modal aç
            // (önceden A2 S3'teydi; A4 girişi pedagojik olarak daha güçlü: peş peşe yakın
            // kayıpların ardından gelen büyük kazanç tuzağına önceden uyarı.)
            _a4S1DonmeGosterildi = true;
            Debug.Log("[YildizDans] A4 Spin 1 sonu — yıldız dans + modal akışı başlatılıyor.");
            StartCoroutine(A4S1YildizModalAkisi());
        }
        if (_aktifAsama == 2 && _aktifSpin == 6 && !_a3BahisYukseltildi)
        {
            _a3BahisYukseltildi = true;
            Debug.Log("[BahisAnim] A3 Spin 6 sonu — bahis 1500 → 2500 animasyonla yükseliyor.");
            // ANIMASYONLA: 250 TL/0.10 sn kademeli artış (kullanıcı tick tick tick görür)
            int eski = _oy != null ? _oy.AnlaticiMevcutBahis() : 1500;
            StartCoroutine(BahisAnimasyonu(eski, 2500));
        }
        if (_aktifAsama == 3 && _aktifSpin == 5 && !_a4S5CarpanModalGosterildi)
        {
            _a4S5CarpanModalGosterildi = true;
            Debug.Log("[Anlatici] A4 Spin 5 sonu — ×100 çarpan modali tetikleniyor.");
            StartCoroutine(A4S5CarpanModalAkisi());
        }

        Guncelle();

        // SAVE/LOAD: spin sonu otomatik save (HerhangiOverlayAcik + bonus IsAcik triple guard içeride).
        SaveDurumKaydet();
    }

    private void AsamayiUygula(int idx)
    {
        if (idx < 0 || idx >= _asamalar.Length || _oy == null) return;
        var a = _asamalar[idx];

        // Yeni aşama geçişi mi? (Aynı aşamada her spin sonrası sadece egilim/maxOdeme yeniden hesaplanır,
        // bahis kullanıcının manuel değiştirdiği değerle kalır)
        bool yeniAsama = (idx != _sonUygulananAsama);
        _sonUygulananAsama = idx;

        // Otomatik bahis SADECE yeni aşama geçişinde
        if (yeniAsama && idx >= 0 && idx < _onerilenBahisler.Length)
        {
            int onerilen = _onerilenBahisler[idx];
            try { _oy.AnlaticiSetBahis(onerilen); }
            catch (System.Exception e) { Debug.LogWarning("[AnlaticiSeritKopru] AnlaticiSetBahis hata: " + e.Message); }
        }

        int bahis = _oy.AnlaticiMevcutBahis();
        if (bahis <= 0) bahis = 100;
        int maxOdeme = Mathf.CeilToInt(bahis * a.maxCarpani);
        try
        {
            _oy.AdminSetOdemeEgilimi(a.egilim);
            _oy.AdminSetMaxOdeme(maxOdeme);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[AnlaticiSeritKopru] AsamayiUygula hata: " + e.Message);
        }
        Debug.Log($"[Anlatici] Aşama {idx + 1} uygulandı (yeniAsama={yeniAsama}): egilim=%{a.egilim}, maxCarpan={a.maxCarpani}x, bahis={bahis}, maxOdeme={maxOdeme} TL, nearMiss={a.nearMiss}");
    }

    private void Guncelle()
    {
        if (_oy == null) return;
        long bakiye = _oy.BahisPanelMevcutBakiye();
        long net = bakiye - _baslangicBakiye;
        int hedefSpin = _asamaSpinSayisi[Mathf.Clamp(_aktifAsama, 0, _asamaSpinSayisi.Length - 1)];
        string spinNetJson = "[" + string.Join(",", _asamaSpinNet.ConvertAll(n => n.ToString())) + "]";
        string json = "{\"asama\":" + _aktifAsama +
                      ",\"spin\":" + _aktifSpin +
                      ",\"hedefSpin\":" + hedefSpin +
                      ",\"bakiyeNet\":" + net +
                      ",\"toplamSpin\":" + _toplamSpin +
                      ",\"spinNetleri\":" + spinNetJson + "}";
#if UNITY_WEBGL && !UNITY_EDITOR
        AnlaticiPaneliGuncelle(json);
#endif
    }

    public void Tukenis()
    {
        if (_oy == null) return;
        long bakiye = _oy.BahisPanelMevcutBakiye();
        long net = bakiye - _baslangicBakiye;
        string json = "{\"tukenis\":true,\"bakiyeNet\":" + net + ",\"toplamSpin\":" + _toplamSpin + "}";
#if UNITY_WEBGL && !UNITY_EDITOR
        AnlaticiPaneliGuncelle(json);
#endif
        Debug.Log($"[Anlatici] Tükeniş: net={net} TL, toplam {_toplamSpin} spin");
    }

    /// <summary>HTML panelde ◀ ▶ veya nokta tıklamasıyla manuel aşama değişimi.</summary>
    public void HtmlAsamaDegisti(int yeniAsama)
    {
        if (yeniAsama < 0 || yeniAsama > 6) return;
        _aktifAsama = yeniAsama;
        _aktifSpin = 0;
        _asamaSpinNet.Clear(); // manuel aşama değişiminde de çubuklar sıfır
        AsamayiUygula(yeniAsama);
        Guncelle();
    }

    /// <summary>
    /// ⚠️ GEÇİCİ — Sadece <see cref="ScriptedDebugAtlamaPaneli"/> kullanır. Aşama + spin sayacını
    /// doğrudan set eder; AsamayiUygula çağrılır (bahis/eğilim güncellenir). Final sürümde bu method kaldırılmalı.
    /// </summary>
    public void DebugAsamaSpinSet(int asama, int spin)
    {
        _aktifAsama = Mathf.Clamp(asama, 0, 6);
        _aktifSpin = Mathf.Max(0, spin);
        _sonUygulananAsama = -1; // yeniAsama=true olsun, AsamayiUygula bahisi yeniden set etsin
        _asamaSpinNet.Clear();
        AsamayiUygula(_aktifAsama);
        Guncelle();
    }

    public void YenidenBaslat()
    {
        Debug.Log("[Anlatici] YENİDEN BAŞLAT — full reset");

        // 1. Bakiye reset
        if (_oy != null)
        {
            _oy.AnlaticiBakiyeyiSifirla(BASLANGIC_BAKIYE);
        }
        _baslangicBakiye = BASLANGIC_BAKIYE;

        // 2. Anlatıcı state reset
        _aktifAsama = 0;
        _aktifSpin = 0;
        _toplamSpin = 0;
        _sonUygulananAsama = -1; // KRİTİK: yeniAsama=true olsun, bahis 100'e tekrar set
        _sonBakiye = BASLANGIC_BAKIYE;
        _asamaSpinNet.Clear();

        // 3. Aşama 1 zorla uygula (egilim 100, maxCarpan 2x, otomatik bahis 100)
        AsamayiUygula(0);

        // 4. HTML panele "tukenisKapat" + tüm state'i tek JSON'la gönder
        string json = "{\"asama\":0,\"spin\":0,\"bakiyeNet\":0,\"toplamSpin\":0,\"spinNetleri\":[],\"tukenisKapat\":true}";
#if UNITY_WEBGL && !UNITY_EDITOR
        AnlaticiPaneliGuncelle(json);
#endif

        Debug.Log("[Anlatici] Reset tamam: bakiye=" + BASLANGIC_BAKIYE + ", asama=0, bahis=100");
    }

    // ──────────────────────────────────────────────────────────────────
    //  HTML PANEL TOGGLE — modal/balon/yükleme açılırken anlatici iframe'i gizler
    //  (sol panel WebGL'de DOM iframe; Unity Canvas overlay'lerinin altında kalmaz, gizlenir).
    // ──────────────────────────────────────────────────────────────────

    // Referans counter: birden fazla modal/panel aynı anda açıkken iç içe Gizle/Goster çağrıları
    // tutarlı kalır. Her Gizle sayacı artırır; sayaç 1'e ulaştığında iframe display:none yapılır.
    // Her Goster sayacı azaltır; sayaç 0'a düştüğünde iframe display:block yapılır.
    private static int _gizliSayac = 0;

    /// <summary>Sol anlatici HTML iframe'ini gizler (display:none). Modal/balon/yükleme açılırken çağrılır.
    /// Referans-counted: birden fazla overlay aynı anda Gizle çağırırsa hepsi kapanmadan iframe geri açılmaz.</summary>
    public void Gizle()
    {
        _gizliSayac++;
        if (_gizliSayac != 1) return; // zaten gizli, ek çağrı yalnızca counter artırır
#if UNITY_WEBGL && !UNITY_EDITOR
        try { AnlaticiPaneliGizle(); }
        catch (System.Exception e) { Debug.LogWarning("[Anlatici] Gizle hata: " + e.Message); }
#else
        Debug.Log("[Anlatici] Gizle (Editor fallback — sadece WebGL'de etkili).");
#endif
    }

    /// <summary>Gizlenen anlatici HTML iframe'ini geri açar (referans-counted, sayaç 0'da fiili display:block).</summary>
    public void Goster()
    {
        _gizliSayac = System.Math.Max(0, _gizliSayac - 1);
        if (_gizliSayac != 0) return; // hâlâ başka overlay açık, iframe gizli kalsın
#if UNITY_WEBGL && !UNITY_EDITOR
        try { AnlaticiPaneliGoster(); }
        catch (System.Exception e) { Debug.LogWarning("[Anlatici] Goster hata: " + e.Message); }
#else
        Debug.Log("[Anlatici] Goster (Editor fallback — sadece WebGL'de etkili).");
#endif
    }

    /// <summary>
    /// Senaryolu eğitim modu: kullanıcı sadece SPIN butonuna basabilir.
    /// Bahis +/-, bonus satın al, bakiye yükle, otomatik spin ve ayarlar butonları devre dışı.
    /// Spin butonu (cevirButon) ile borç al ve modal TAMAM butonları (runtime canvas)
    /// dokunulmaz — onlar senaryo akışının parçası.
    /// </summary>
    private void SenaryoluKontrolleriDevreDisiBirak()
    {
        if (_oy == null) { Debug.LogWarning("[Anlatici] _oy null, kontroller devre dışı bırakılamadı."); return; }

        int sayac = 0;
        if (_oy.bahisArttirButon != null)        { _oy.bahisArttirButon.interactable = false;        sayac++; }
        if (_oy.bahisAzaltButon != null)         { _oy.bahisAzaltButon.interactable = false;         sayac++; }
        if (_oy.bonusSatinAlButon != null)       { _oy.bonusSatinAlButon.interactable = false;       sayac++; }
        if (_oy.bakiyeYukleButon != null)        { _oy.bakiyeYukleButon.interactable = false;        sayac++; }
        if (_oy.otomatikSpinButton != null)      { _oy.otomatikSpinButton.interactable = false;      sayac++; }
        if (_oy.otomatikSpinBaslatButon != null) { _oy.otomatikSpinBaslatButon.interactable = false; sayac++; }
        if (_oy.otomatikSpinIptalButon != null)  { _oy.otomatikSpinIptalButon.interactable = false;  sayac++; }

        // Ayarlar butonu: sahnede "AyarlarButton" GameObject'te Button bileşeni var.
        // (AyarlarButtonAdminPanelAcKapa script'i ayrı bir GameObject'te → FindObjectOfType yanlıştı.)
        var ayarGo = GameObject.Find("AyarlarButton");
        if (ayarGo != null)
        {
            var btn = ayarGo.GetComponent<UnityEngine.UI.Button>();
            if (btn != null) { btn.interactable = false; sayac++; }
        }

        Debug.Log($"[Anlatici] Senaryo eğitim modu: {sayac} kontrol butonu devre dışı bırakıldı.");
    }

    // ──────────────────────────────────────────────────────────────────
    //  PEDAGOJİK GEÇİŞ COROUTINE'LARI — eğitmen modalı + sonraki adım
    // ──────────────────────────────────────────────────────────────────

    /// <summary>Sahne girişinde otomatik: oyuncuyu simülasyona hazırlayan karşılama modalı.</summary>
    private System.Collections.IEnumerator PreA1KarsilamaAkisi()
    {
        AnlaticiOzelAkisAktif = true;
        try
        {
            // ScriptedModalKopru'nun spawn olmasını bekle (RuntimeInitializeOnLoadMethod ardından bir frame)
            yield return null;
            var modal = UnityEngine.Object.FindObjectOfType<Senaryo.Scripted.ScriptedModalKopru>();
            if (modal == null)
            {
                Debug.LogWarning("[Anlatici] PreA1 — ScriptedModalKopru bulunamadı, karşılama atlanıyor.");
                yield break;
            }
            // PreA1 ek anlatım — A1 aşamasının pedagojik özeti (Hoş geldin sonrası ikinci modal)
            const string A1_ANLATIM =
                "<b>İlk aşama: <i>Isındırma ve Umut</i></b>\n\n" +
                "İlk kazanç, oyuncu için <color=#F24D40>en tehlikeli başlangıçtır</color>. " +
                "Beyin bu <color=#4DCC59>olumlu deneyimi</color> güçlü biçimde hatırlar ve kişi " +
                "<color=#F24D40>oyunda kalmaya devam eder</color>.\n\n" +
                "<color=#F24D40>Uzun süreli oynama</color> davranışının temelinde, " +
                "<color=#FFD933>ilk kazanmanın yarattığı bu etki</color> bulunur.";
            // PreA1 üçüncü modal — kullanıcıyı spin atmaya davet + sol panel takip yönlendirmesi.
            const string A1_DAVET =
                "<b>Şimdi sen dene</b>\n\n" +
                "Tam <color=#FFD933>10 spin</color> at ve neler olduğunu gör. " +
                "Bakiyenin nasıl yükseldiğine, kazançların sıklığına dikkat et.\n\n" +
                "Sol panelde <color=#5BA0FF>SAHNE ARKASI</color> ve " +
                "<color=#5BA0FF>OYUNCUNUN KAFASI</color> bölümlerini takip et — " +
                "<color=#4DCC59>sistemin gerçekte ne yaptığını</color> orada göreceksin.";
            string mesaj =
                "Hoş geldiniz. Bu simülasyonda online kumar oyunlarının oyuncuları nasıl etkilediğini birlikte göreceğiz.\n\n" +
                "<b>Önce oyunu tanıyalım:</b>\n" +
                "• Ekranda 6×5'lik meyve makinesi var. SPIN tuşuna basıldığında meyveler döner.\n" +
                "• Aynı meyveden <color=#FFD700><b>8 veya daha fazlası</b></color> bir araya gelirse <color=#4ADE80>kazanç verir</color>.\n" +
                "• Bazı turlarda <color=#FFD700><b>ÇARPAN</b></color> düşer (<color=#FFD700>×2, ×5, ×100</color> vs.) ve <color=#4ADE80>kazancı katlar</color>.\n" +
                "• Kazanan meyveler patlar, üstten yenileri düşer (<color=#FFD700><b>TUMBLE</b></color>); zincir kazançlar olur.\n" +
                "• <color=#FFD700>4 Bonus Sembolü</color> (yıldız) gelirse <color=#FFD700><b>BONUS</b> oyun</color> açılır.\n\n" +
                "<b>Ekrandaki diğer öğeler:</b>\n" +
                "• <color=#60A5FA><b>Sol panel:</b></color> Oyuncunun hangi aşamada olduğunu, sahne arkasında ne yaşandığını gösterir; birlikte buradan takip edeceğiz.\n" +
                "• <color=#4ADE80><b>Bakiye:</b></color> Oyuna ayrılan para (oyuncu <color=#4ADE80>50.000 TL</color> ile başlıyor).\n" +
                "• <color=#FB923C><b>Bahis:</b></color> Her spinde harcanacak miktar, <color=#FB923C>+ ve − tuşlarıyla</color> değişir.\n" +
                "• <b>KAZANÇ:</b> O spinde kazanılan miktar.\n\n" +
                "Hadi başlayalım: ilk aşama <i>'Isındırma ve Umut'</i>.";
            // gizleAnlatici: false → modal "Sol panel" anlatırken kullanıcının paneli görmesi gerekiyor.
            yield return modal.ModalGoster(mesaj, gizleAnlatici: false);
            // HOTFIX: Modal state'i temizlensin diye 1 frame bekle (ardışık ModalGoster yarış riski)
            yield return null;
            // PreA1 ek anlatım modal — A1 aşamasının pedagojik özeti
            yield return modal.ModalGoster(A1_ANLATIM, gizleAnlatici: false);
            // HOTFIX: ardışık ModalGoster yarış riski için bir frame ara
            yield return null;
            // PreA1 üçüncü modal — kullanıcıyı 10 spin atmaya davet et
            yield return modal.ModalGoster(A1_DAVET, gizleAnlatici: false);
        }
        finally
        {
            AnlaticiOzelAkisBitir();
        }
    }

    /// <summary>A1 son spini sonrası A2'ye geçiş anında: kontrol yanılsamasının başladığını anlatan modal.</summary>
    private System.Collections.IEnumerator A2GecisAkisi()
    {
        AnlaticiOzelAkisAktif = true;
        try
        {
            var modal = UnityEngine.Object.FindObjectOfType<Senaryo.Scripted.ScriptedModalKopru>();
            if (modal == null) yield break;
            string mesaj =
                "Birinci aşama tamamlandı. Oyuncu şu an artıda, kendini iyi hissediyor.\n\n" +
                "Sırada <color=#FB923C><b>'Kontrol Bende Hissi'</b></color> aşaması var. Bu aşamada algoritma oyuncuya üst üste <color=#EF4444>kayıplar</color> yaşatacak. Ama yine de <color=#4ADE80>bakiye</color> hâlâ pozitif olduğu için oyuncu <i>'kontrol bende, istediğim zaman çıkarım, bahis değişiklikleriyle kazanırım'</i> gibi düşünceler yaşar.\n\n" +
                "Bu <color=#60A5FA>yanılsamayı</color> birlikte göreceğiz.";
            yield return modal.ModalGoster(mesaj);
        }
        finally
        {
            AnlaticiOzelAkisBitir();
        }
    }

    /// <summary>A2 son spini sonrası A3'e geçiş: kayıp kovalama tuzağı uyarısı.</summary>
    private System.Collections.IEnumerator A3GecisAkisi()
    {
        AnlaticiOzelAkisAktif = true;
        try
        {
            var modal = UnityEngine.Object.FindObjectOfType<Senaryo.Scripted.ScriptedModalKopru>();
            if (modal == null) yield break;
            string mesaj =
                "İkinci aşama tamamlandı. Oyuncu şu an küçük <color=#EF4444>kayıplar</color> yaşadı ama hâlâ artıda; <color=#60A5FA><i>'kontrol bende'</i></color> hissi iyice yerleşti.\n\n" +
                "Sırada <color=#FB923C><b>'Kaybettiklerimi Geri Kazanabilirim'</b></color> aşaması var. Sistem bu aşamada oyuncuya <color=#EF4444>bilerek kayıp</color> yaşatacak. Oyuncu artık kazanç peşinde değil; <i>'kaybettiklerimi kurtarayım yeter'</i> gibi düşünmeye başlayacak. Bu <color=#60A5FA><b>'Kayıp Kovalama'</b></color> denilen psikolojik <color=#EF4444>tuzaktır</color> — bir kez girilirse çıkmak çok zor.\n\n" +
                "Birlikte göreceğiz.";
            yield return modal.ModalGoster(mesaj);
        }
        finally
        {
            AnlaticiOzelAkisBitir();
        }
    }

    /// <summary>A3 son spini sonrası A4'e geçiş: pes etme eşiği + manipülasyon vuruşu uyarısı.</summary>
    private System.Collections.IEnumerator A4GecisAkisi()
    {
        AnlaticiOzelAkisAktif = true;
        try
        {
            var modal = UnityEngine.Object.FindObjectOfType<Senaryo.Scripted.ScriptedModalKopru>();
            if (modal == null) yield break;
            string mesaj =
                "Üçüncü aşamayı gördük: <color=#60A5FA>kayıp kovalama tuzağı</color>. Oyuncu <color=#FB923C>bahsi yükselterek</color> kurtulmaya çalıştı, daha çok <color=#EF4444>kaybetti</color>.\n\n" +
                "Sırada <color=#FB923C><b>'Şansım Döndü'</b></color> aşaması var. Bu aşamada algoritma oyuncuyu pes etme eşiğine getirecek; üst üste sert <color=#EF4444>kayıplar</color>. Tam pes etmek üzereyken <color=#4ADE80>büyük bir kazanç</color> düşürecek. Bu büyük kazanç tesadüf değil, <color=#EF4444><b>kasıtlı bir manipülasyon vuruşu</b></color> olacak.\n\n" +
                "Amaç: oyuncuyu tekrar oyuna bağlamak.";
            yield return modal.ModalGoster(mesaj);
        }
        finally
        {
            AnlaticiOzelAkisBitir();
        }
    }

    /// <summary>A4 son spini sonrası A5'e geçiş: bonus tuzağı uyarısı.</summary>
    private System.Collections.IEnumerator A5GecisAkisi()
    {
        AnlaticiOzelAkisAktif = true;
        try
        {
            var modal = UnityEngine.Object.FindObjectOfType<Senaryo.Scripted.ScriptedModalKopru>();
            if (modal == null) yield break;
            string mesaj =
                "<color=#4ADE80>Büyük kazanç</color> yaşandı. Oyuncu şu an <i>'şansım döndü, daha kazanırım'</i> hissinde. İşte tam bu duygu, sıradaki aşamanın yakıtıdır.\n\n" +
                "Sırada <color=#FB923C><b>'Sonunu Düşünen Kahraman Olamaz'</b></color> aşaması var. Bu aşamada algoritma oyuncuya cazip bir <color=#FFD700><b>'bonus oyun tuzağı'</b></color> kuracak: tüm <color=#4ADE80>bakiyesini</color> yatırma karşılığında büyük kazanç vaat edilecek. Yatırırsa, <color=#EF4444>çok azını geri alacak</color>.\n\n" +
                "Bu, <color=#EF4444>sömürünün doruk noktasıdır</color>. Birlikte göreceğiz.";
            yield return modal.ModalGoster(mesaj);
        }
        finally
        {
            AnlaticiOzelAkisBitir();
        }
    }

    /// <summary>
    /// Borç Al onayı sonrası iki aşamalı asistan modal + bahis animasyonu (A6 girişi):
    ///   1) "Borç alındı, döngü başlıyor" pedagojik mesaj
    ///   2) "Bahis 10K'ya çıkacak" bilgilendirmesi + bahis animasyonu (mevcut → 10000)
    /// ScriptedYuklemePaneli.OnBorcAlTiklandi tarafından çağrılır.
    /// </summary>
    public System.Collections.IEnumerator BorcSonrasiModalAkisi()
    {
        AnlaticiOzelAkisAktif = true;
        try
        {
            yield return new WaitForSecondsRealtime(0.5f); // Yükleme paneli kapanma animasyonu

            var modal = UnityEngine.Object.FindObjectOfType<Senaryo.Scripted.ScriptedModalKopru>();
            if (modal == null) yield break;

            // 1) Döngü başlangıcı pedagojik mesaj
            yield return modal.ModalGoster(
                "İşte oyuncu <color=#EF4444>borç aldı</color>, <color=#4ADE80>bakiyesi yenilendi</color>. Şimdi tekrar oynamaya devam edecek.\n\n" +
                "Kumar sitelerinde yeniden bakiye yükleyenlere <color=#EF4444>bilinçli olarak</color> ilk başlarda yine <color=#4ADE80>kazandırılır</color> — bu <i>'Isındırma ve Umut'</i> aşamasına benzer.\n\n" +
                "Bu sayede oyuncu tekrar <color=#EF4444>döngüye girer</color>: <i>'şansım yine açıldı, kayıplarımı telafi ederim'</i> düşünür. Ama er ya da geç sistem kazanır, oyuncu <color=#EF4444>kaybeder</color>.\n\n" +
                "Şimdi bu döngüyü hızlıca göreceğiz."
            );

            // 2) A6 bahis bilgilendirme
            yield return modal.ModalGoster(
                "Bu kez oyuncu <color=#EF4444>kayıplarını HIZLI telafi</color> etmek istiyor. <color=#FB923C><b>Bahsini 10.000 TL'ye</b></color> çıkardı.\n\n" +
                "Sadece <color=#FFD700>5 spin</color> yetecek; algoritma <color=#EF4444>sömürünün son evresinde</color> tüm <color=#4ADE80>bakiyeyi</color> alacak. " +
                "Bu hızlı bitiş, gerçek hayattaki <i>'son kez deneme'</i> bahanesinin sonucudur."
            );

            // 3) Bahis animasyonu: mevcut bahis → 10000 (kademeli artış, "+ tuşu" hissi)
            // BahisAnimasyonu kendi try/finally'sine sahip — flag yönetimi nested ama state korunur (true→true→false→false).
            yield return BahisAnimasyonu(_oy != null ? _oy.AnlaticiMevcutBahis() : 4000, 10000);
        }
        finally
        {
            AnlaticiOzelAkisBitir();
        }
    }

    /// <summary>Bahis kademeli artış animasyon helper. Adım otomatik hesaplanır:
    /// fark ≤ 2500 → 250 TL adım, daha büyük → 1000 TL adım. Her adım 0.10 sn (tick hissi).</summary>
    private System.Collections.IEnumerator BahisAnimasyonu(int eski, int yeni)
    {
        AnlaticiOzelAkisAktif = true;
        try
        {
            if (_oy == null) yield break;
            int fark = yeni - eski;
            int adim = fark <= 2500 ? 250 : 1000; // küçük fark → küçük adım, büyük fark → hızlı tick
            const float SURE_PER_ADIM = 0.10f;
            int simdi = Mathf.Max(eski, _oy.AnlaticiMevcutBahis());
            while (simdi < yeni)
            {
                simdi = Mathf.Min(yeni, simdi + adim);
                try { _oy.AnlaticiSetBahis(simdi); }
                catch (System.Exception e) { Debug.LogWarning("[BahisAnim] hata: " + e.Message); break; }
                yield return new WaitForSecondsRealtime(SURE_PER_ADIM);
            }
            Debug.Log($"[BahisAnim] {eski} → {yeni} TL animasyonu tamamlandı (adım={adim}).");
        }
        finally
        {
            AnlaticiOzelAkisBitir();
        }
    }

    /// <summary>
    /// A4 Spin 1 NearMiss: 3 yıldız sahnede dönerken (sürekli rotate) modal açılır,
    /// modal kapanınca dönme durur. Pedagojik konum: A4 girişinde "neredeyse oluyordu" hissinin
    /// pekişmesi + birazdan gelecek büyük kazanç (A4 S5 ×100) ile tuzağın kapanması bağlantısı.
    /// </summary>
    private System.Collections.IEnumerator A4S1YildizModalAkisi()
    {
        AnlaticiOzelAkisAktif = true;
        try
        {
            yield return new WaitForSecondsRealtime(0.5f); // Spin animasyonu otursun, yıldızlar yerleşsin
            YildizDonmeBaslat();
            yield return new WaitForSecondsRealtime(0.5f); // Kullanıcı dönmeyi fark etsin
            var modal = UnityEngine.Object.FindObjectOfType<Senaryo.Scripted.ScriptedModalKopru>();
            string mesaj =
                "<color=#FFD700><b>Üç yıldız (bonus sembolü)</b></color> yine düştü, dördüncüsü düşmedi. Oyuncu peş peşe bu sahneleri yaşadıkça " +
                "<color=#60A5FA><i>'neredeyse oluyordu, şansım dönmek üzere'</i></color> hissine kapılır ve masada kalmaya devam eder. " +
                "Sistem bu beklentiyi <color=#EF4444>mahsus</color> yaratır — birazdan vereceği <color=#4ADE80>büyük tek kazançla</color> bu hissi pekiştirip " +
                "oyuncuyu <color=#EF4444>kilitleyecek</color>.";
            if (modal != null)
                yield return modal.ModalGoster(mesaj);
            YildizDonmeyiDurdur();
        }
        finally
        {
            AnlaticiOzelAkisBitir();
        }
    }

    /// <summary>Sahnedeki yıldız (scatter) sembol GameObject'lerini bulur — Image sprite name fallback.</summary>
    private System.Collections.Generic.List<GameObject> YildizlariBul()
    {
        var sonuc = new System.Collections.Generic.List<GameObject>();
        var images = UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Image>();
        foreach (var img in images)
        {
            if (img == null || img.sprite == null) continue;
            string ad = img.sprite.name.ToLower();
            if (ad.Contains("scatter") || ad.Contains("yildiz") || ad.Contains("yıldız") || ad.Contains("star"))
                sonuc.Add(img.gameObject);
        }
        Debug.Log($"[YildizBulucu] {sonuc.Count} yıldız bulundu (sprite name).");
        return sonuc;
    }

    private void YildizDonmeBaslat()
    {
        _aktifYildizlar.Clear();
        _aktifDansCoroutineleri.Clear();
        _aktifYildizlar.AddRange(YildizlariBul());
        Debug.Log($"[YildizDans] {_aktifYildizlar.Count} yıldız döndürülüyor.");
        foreach (var y in _aktifYildizlar)
        {
            var c = StartCoroutine(YildizDon(y));
            _aktifDansCoroutineleri.Add(c);
        }
    }

    private System.Collections.IEnumerator YildizDon(GameObject yildiz)
    {
        if (yildiz == null) yield break;
        const float DONME_HIZI = 360f / 1.5f; // 1 tam tur 1.5 sn
        while (yildiz != null)
        {
            yildiz.transform.Rotate(0f, 0f, DONME_HIZI * Time.unscaledDeltaTime);
            yield return null;
        }
    }

    private void YildizDonmeyiDurdur()
    {
        Debug.Log("[YildizDans] Dönme durduruluyor.");
        foreach (var c in _aktifDansCoroutineleri)
        {
            if (c != null) StopCoroutine(c);
        }
        _aktifDansCoroutineleri.Clear();
        foreach (var y in _aktifYildizlar)
        {
            if (y != null) y.transform.localRotation = Quaternion.identity;
        }
        _aktifYildizlar.Clear();
    }

    /// <summary>A4 Spin 5 sonu ×100 çarpan: manipülasyon vuruşunun pedagojik açıklaması +
    /// "çekim şartı tuzağı" ikinci modal'ı (büyük kazanç sonrası "çekip çıkayım" düşüncesini söndürür).</summary>
    private System.Collections.IEnumerator A4S5CarpanModalAkisi()
    {
        AnlaticiOzelAkisAktif = true;
        try
        {
            // NormalSpinAkisi → ScriptedKazancUcusu yield ile beklendi: buraya geldiğimizde
            // win feedback popup'ı + kazanç uçuşu ZATEN tamamlanmış. Modal hemen açılabilir.
            // Küçük buffer: bakiye değişimi netleşsin, kullanıcı "kazandım" hissetsin, sonra modal.
            yield return new WaitForSeconds(0.2f);

            var modal = UnityEngine.Object.FindObjectOfType<Senaryo.Scripted.ScriptedModalKopru>();
            if (modal == null) yield break;
            string mesaj =
                "⚡ Ekrana <color=#FFD700><b>×100 çarpan</b></color> düştü! Oyuncu az önce pes etmek üzereydi, şimdi <color=#4ADE80>büyük kazanç</color>. " +
                "Bu rastlantı değil: algoritma oyuncuyu tam bu duygusal anda yakaladı. <i>'Şansım döndü'</i> diyecek. " +
                "Aslında <color=#EF4444>manipülasyon başarılı oldu</color>.";
            yield return modal.ModalGoster(mesaj);

            // Çekim Şartı Tuzağı — büyük kazanç sonrası "çekip çıkayım" düşüncesini söndüren ikinci modal.
            string cekimSartiMesaji =
            "İşte bu noktada gerçek hayatta oyuncunun aklına şu gelir: <color=#60A5FA><i>'Şu an kazançtayım, parayı çekip çıkayım.'</i></color> Mantıklı düşünce. Ama kumar siteleri bunun olmasına izin vermez.\n\n" +
            "<color=#EF4444><b>Çekim şartı tuzağı:</b></color> Site, oyuncunun kazandığı parayı çekebilmesi için bir <color=#EF4444><b>\"çevrim şartı\"</b></color> koyar. Bu şart genelde iki şekilde olur:\n\n" +
            "- <color=#FB923C><b>Bahis çevrim şartı:</b></color> Oyuncu, kazandığı paranın belirli bir katı kadar tutarda bahis atmadan parasını çekemez.\n\n" +
            "- <color=#FB923C><b>Spin sayısı şartı:</b></color> Oyuncunun belirli bir spin sayısına ulaşması gerekir, <b>örneğin 1000 spin atma şartı gibi</b>. Bu sayıya ulaşmadan çekim yapmasına izin verilmez.\n\n" +
            "Sonuç değişmez: Oyuncu bu şartları tamamlamaya çalışırken sistem <color=#EF4444>kazandığı parayı yavaş yavaş geri alır</color>, üstüne <color=#EF4444>kendi parasını da kaybeder</color>. Çekim şartı <color=#EF4444>sağlanamadan</color> oyuncu zaten masada tüketilmiş olur.\n\n" +
                "Yani <color=#EF4444><b>\"çekip çıkma\" seçeneği aslında yok</b></color> — sadece var gibi görünür. Kumar sitesinin tek gerçek amacı oyuncuyu masada tutmaktır.";
            yield return modal.ModalGoster(cekimSartiMesaji);
        }
        finally
        {
            AnlaticiOzelAkisBitir();
        }
    }

    /// <summary>
    /// A5 cazip bonus bitti (A5_S5 dinamik modal kapandı) → A5'in kalan spinlerini ATLA, direkt A6'ya
    /// zıpla + BasaArayisAkisi (modal + düşünce balonu + borç paneli) tetikle. ScriptedBonusOyunUygulayici
    /// finally öncesinde çağırır. Tek seferlik (panel zaten açıldıysa noop).
    /// </summary>
    public void A5BonusBittiBorcPaneliAc()
    {
        if (_yuklemePaneliAcildiBuOturum)
        {
            Debug.Log("[Anlatici] A5BonusBittiBorcPaneliAc — yükleme paneli zaten açılmış, atlanıyor.");
            return;
        }
        Debug.Log("[Anlatici] A5 cazip bonus bitti → A6 + borç paneli direkt (kalan A5 spinleri atlanıyor).");

        // Bakiye snapshot güncelle (bonus oyun bakiyeyi etkiledi; bir sonraki spin net hesabı doğru olsun)
        if (_oy != null) _sonBakiye = _oy.BahisPanelMevcutBakiye();

        _aktifAsama = 5;  // A6 indeksi (0-tabanlı: 0=A1 ... 5=A6 ... 6=A7)
        _aktifSpin = 0;
        _asamaSpinNet.Clear();
        AsamayiUygula(_aktifAsama);
        Guncelle();

        StartCoroutine(BasaArayisAkisi());
    }

    /// <summary>
    /// A5 sonu bakiye yetersiz → eğitmen "para arayışı" modalı çalar; modal kapanınca
    /// ScriptedYuklemePaneli "BORÇ AL — 50.000 TL" açılır.
    /// </summary>
    private System.Collections.IEnumerator BasaArayisAkisi()
    {
        AnlaticiOzelAkisAktif = true;
        try
        {
            // 1) Eğitmen modal — anlatıcı pedagojik açıklama (sol-alt karakter dialog)
            var modal = UnityEngine.Object.FindObjectOfType<Senaryo.Scripted.ScriptedModalKopru>();
            string mesaj =
                "Oyuncu artık <color=#EF4444>paranın bittiğini</color> fark etti.\n\n" +
                "Şimdi başka yerden para bulma arayışında. <color=#EF4444>Yalan söylemeye</color> başlıyor: " +
                "yakınlarına, akrabalarına, arkadaşlarına...\n\n" +
                "Bu, <color=#EF4444>kumar bağımlılığının yıkıcı evresidir</color>. Bir sonraki ekran o anı temsil ediyor.";
            if (modal != null)
                yield return modal.ModalGoster(mesaj);
            else
                Debug.LogWarning("[Anlatici] BasaArayisAkisi — ScriptedModalKopru bulunamadı, modal atlanıyor.");

            // 2) Düşünce balonu — karakter ortada, 4 yalan ayrı bulutlarda (klasik çizgi roman)
            var balon = UnityEngine.Object.FindObjectOfType<Senaryo.Scripted.ScriptedDusunceBalonu>();
            if (balon != null)
            {
                Debug.Log("[Anlatici] Düşünce balonu açılıyor...");
                yield return balon.BaloniGoster();
            }
            else
            {
                Debug.LogWarning("[Anlatici] BasaArayisAkisi — ScriptedDusunceBalonu bulunamadı, balon atlanıyor.");
            }

            // 3) Yükleme paneli — "BORÇ AL — 50.000 TL"
            var panel = UnityEngine.Object.FindObjectOfType<Senaryo.Scripted.ScriptedYuklemePaneli>();
            if (panel != null)
            {
                panel.PaneliGoster();
                _yuklemePaneliAcildiBuOturum = true;
                Debug.Log("[Anlatici] BasaArayisAkisi tamamlandı — Yükleme paneli açıldı.");

                // SAVE/LOAD: borç paneli açıldı → A6 evresine geçildi, save (kullanıcı bu noktadan devam edebilsin).
                SaveDurumKaydet();
            }
            else
            {
                Debug.LogError("[Anlatici] BasaArayisAkisi — ScriptedYuklemePaneli bulunamadı!");
            }
        }
        finally
        {
            AnlaticiOzelAkisBitir();
        }
    }

    /// <summary>
    /// A6 sonu bakiye yine bitti → eğitmen "döngü başa sardı" modalı çalar; modal kapanınca
    /// klasik A7 final ekranı (Tukenis) tetiklenir. Tek seferlik.
    /// </summary>
    private System.Collections.IEnumerator DonguAkisi()
    {
        AnlaticiOzelAkisAktif = true;
        try
        {
            if (_donguModalGosterildi) { Tukenis(); yield break; }
            _donguModalGosterildi = true;

            var modal = UnityEngine.Object.FindObjectOfType<Senaryo.Scripted.ScriptedModalKopru>();
            string mesaj =
                "Bakın, <color=#EF4444>para tamamen bitti</color>.\n\n" +
                "<color=#EF4444><b>5 spin'de 50.000 TL borç eridi.</b></color> Bu, gerçek hayatta <i>'hızlı kurtulma'</i> bahanesiyle yatırılan paraların kaderidir.\n\n" +
                "Şimdi oyuncu A1'e geri dönmek isteyecek. <i>'Belki bu sefer şanslıyım'</i> diye düşünüyor. <i>'Bir kerelik daha denersem...'</i> diyerek kendini kandırıyor.\n\n" +
                "İşte bağımlılığın özü budur: <color=#EF4444><b>KAYIP → BORÇ → KAYIP → BORÇ</b></color>. Sonsuz döngü.\n\n" +
                "Sonraki ekranda yaşanan toplam <color=#EF4444>kayıp</color> gösteriliyor.";
            if (modal != null)
                yield return modal.ModalGoster(mesaj);
            else
                Debug.LogWarning("[Anlatici] DonguAkisi — ScriptedModalKopru bulunamadı, modal atlanıyor.");

            Debug.Log("[Anlatici] DonguAkisi tamamlandı — Tukenis çağrılıyor.");
            Tukenis();
        }
        finally
        {
            AnlaticiOzelAkisBitir();
        }
    }

    // ═════════════════════════════════════════════════════════════════════════════
    //  SAVE / LOAD
    //  - Save tetik: SpinAtildi sonu + BasaArayisAkisi sonu (panel açıldı)
    //  - Restore: Start başında (KumarRestoreModuActif flag set ise)
    //  - Sil: A7 final (ScriptedFinalEkrani) + KullaniciAdiModalKontrol "SIFIRDAN BAŞLA"
    // ═════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Mevcut state'i KumarSaveData'ya çevirir + PlayerPrefs'e yazar.
    /// Triple guard: HerhangiOverlayAcik + ScriptedBonusOyunUygulayici.IsAcik +
    /// ScriptedBonusTuzagiPopup.IsAcik açıkken save yapma (yarım state korunur).
    /// </summary>
    private void SaveDurumKaydet()
    {
        if (_oy == null) return;

        // STABLE STATE GUARD — popup/modal/bonus akışı açıkken save'lemek state'i bozar.
        if (HerhangiOverlayAcik)
        {
            Debug.Log("[SaveLoad] HerhangiOverlayAcik=true → save atlandı.");
            return;
        }
        if (Senaryo.Scripted.ScriptedBonusOyunUygulayici.IsAcik
            || Senaryo.Scripted.ScriptedBonusTuzagiPopup.IsAcik)
        {
            Debug.Log("[SaveLoad] Bonus akışı / cazip popup aktif → save atlandı.");
            return;
        }

        var data = new KumarSaveData
        {
            kullaniciAdi = KullaniciVerileri.KullaniciAdi,

            aktifAsama = _aktifAsama,
            aktifSpin = _aktifSpin,
            toplamSpin = _toplamSpin,
            sonUygulananAsama = _sonUygulananAsama,
            sonBakiye = _sonBakiye,
            baslangicBakiye = _baslangicBakiye,
            asamaSpinNet = new System.Collections.Generic.List<int>(_asamaSpinNet),

            yuklemePaneliAcildiBuOturum = _yuklemePaneliAcildiBuOturum,
            donguModalGosterildi = _donguModalGosterildi,
            preA1ModalGosterildi = _preA1ModalGosterildi,
            a2GecisModalGosterildi = _a2GecisModalGosterildi,
            a3GecisModalGosterildi = _a3GecisModalGosterildi,
            a4GecisModalGosterildi = _a4GecisModalGosterildi,
            a5GecisModalGosterildi = _a5GecisModalGosterildi,
            a3BahisYukseltildi = _a3BahisYukseltildi,
            a4S5CarpanModalGosterildi = _a4S5CarpanModalGosterildi,
            a4S1DonmeGosterildi = _a4S1DonmeGosterildi,

            bakiye = (int)_oy.BahisPanelMevcutBakiye(),
            oturumKazanc = _oy.OturumKazanc,

            yuklemeSayisi = SenaryoYoneticisi.I != null ? SenaryoYoneticisi.I.yuklemeSayisi : 1,

            bonusYatirim = Senaryo.Scripted.ScriptedBonusOyunUygulayici.BonusYatirim,
            bonusKazanc = Senaryo.Scripted.ScriptedBonusOyunUygulayici.BonusKazanc,

            adminEgilim = _oy.GetAdminOdemeEgilimi(),
            adminMaxOdeme = _oy.GetAdminMaxOdeme(),
        };
        SaveLoadServisi.Save(data);
    }

    /// <summary>
    /// Save'den state'i geri yükle. Start akışındaki default reset bloğuyla aynı setup'ı
    /// kendi içinde yapar (admin reset + SenaryoYoneticisi bypass + kazanç fazı sıfırla),
    /// sonra alanları override eder + AsamayiUygula + AdminSet override (AsamayiUygula adminSet'leri
    /// anlatıcı profilinden ezdiği için save'deki override sonradan uygulanır) + Guncelle.
    /// </summary>
    private void RestoreDurumYukle(KumarSaveData s)
    {
        if (s == null || _oy == null) return;

        // === Setup (default reset bloğunun ortak kısmı) ===
        try
        {
            _oy.AdminNormalOyunUygula();
            Debug.Log("[SaveLoad/Restore] AdminNormalOyunUygula çağrıldı.");
        }
        catch (System.Exception e) { Debug.LogError("[SaveLoad/Restore] AdminNormalOyunUygula hatası: " + e.Message); }

        try { _oy.AnlaticiKazancFaziniSifirla(); }
        catch (System.Exception e) { Debug.LogError("[SaveLoad/Restore] AnlaticiKazancFaziniSifirla hatası: " + e.Message); }

        // SenaryoYoneticisi defansif Asama7_Finale (her zaman) — gerçek aşama _aktifAsama'da.
        if (SenaryoYoneticisi.I != null)
        {
            try
            {
                SenaryoYoneticisi.I.mevcutAsama = SenaryoYoneticisi.SenaryoAsama.Asama7_Finale;
                SenaryoYoneticisi.I.forcedNoPayKalan = 0;
                SenaryoYoneticisi.I.yuklemeSayisi = s.yuklemeSayisi;
            }
            catch (System.Exception e) { Debug.LogError("[SaveLoad/Restore] SenaryoYoneticisi setup hatası: " + e.Message); }
        }

        // === Alan override ===
        _aktifAsama = s.aktifAsama;
        _aktifSpin = s.aktifSpin;
        _toplamSpin = s.toplamSpin;
        _sonUygulananAsama = -1;  // AsamayiUygula içinde "yeniAsama" branch'i çalışsın (idempotency bypass).

        _yuklemePaneliAcildiBuOturum = s.yuklemePaneliAcildiBuOturum;
        _donguModalGosterildi = s.donguModalGosterildi;
        _preA1ModalGosterildi = s.preA1ModalGosterildi;
        _a2GecisModalGosterildi = s.a2GecisModalGosterildi;
        _a3GecisModalGosterildi = s.a3GecisModalGosterildi;
        _a4GecisModalGosterildi = s.a4GecisModalGosterildi;
        _a5GecisModalGosterildi = s.a5GecisModalGosterildi;
        _a3BahisYukseltildi = s.a3BahisYukseltildi;
        _a4S5CarpanModalGosterildi = s.a4S5CarpanModalGosterildi;
        _a4S1DonmeGosterildi = s.a4S1DonmeGosterildi;

        Senaryo.Scripted.ScriptedBonusOyunUygulayici.BonusYatirim = s.bonusYatirim;
        Senaryo.Scripted.ScriptedBonusOyunUygulayici.BonusKazanc = s.bonusKazanc;
        // borcAlindi save edilmez — _yuklemePaneliAcildiBuOturum yeterli (panel tekrar açılmaz).
        // ScriptedYuklemePaneli.BorcAlindiSifirla() çağrılmaz — restore sonrası A6'da iken state korunmalı.

        // Kullanıcı adı — KumarSaveData'dan otoriter (hoşgeldin kutusu, log için).
        if (!string.IsNullOrWhiteSpace(s.kullaniciAdi))
            KullaniciVerileri.KullaniciAdi = s.kullaniciAdi;

        // === Ekonomi ===
        _oy.AnlaticiBakiyeyiSifirla(s.bakiye);
        _oy.OturumKazancSifirla(s.oturumKazanc);
        _baslangicBakiye = s.baslangicBakiye;
        _sonBakiye = s.sonBakiye;
        // _asamaSpinNet readonly (instance referansı sabit, içerik mutate edilir).
        // Mevcut SpinAtildi/Reset pattern'iyle tutarlı: Clear + AddRange.
        _asamaSpinNet.Clear();
        if (s.asamaSpinNet != null) _asamaSpinNet.AddRange(s.asamaSpinNet);

        // === Aşama uygula + admin override ===
        // AsamayiUygula içinde anlatıcı profilinin egilim/maxOdeme'si (a.egilim, bahis × a.maxCarpani) yazılır.
        // Save'deki adminEgilim/adminMaxOdeme bunu sonradan ezer — kullanıcının ayrıldığı andaki tam değerler korunsun.
        AsamayiUygula(_aktifAsama);
        try
        {
            _oy.AdminSetOdemeEgilimi(s.adminEgilim);
            _oy.AdminSetMaxOdeme(s.adminMaxOdeme);
        }
        catch (System.Exception e) { Debug.LogError("[SaveLoad/Restore] AdminSet override hatası: " + e.Message); }

        Guncelle();
        Debug.Log($"[SaveLoad/Restore] OK: A{_aktifAsama + 1} S{_aktifSpin + 1} (toplam={_toplamSpin}), bakiye={s.bakiye}, oturumKazanc={s.oturumKazanc}, kullanici='{s.kullaniciAdi}'");
    }
}
