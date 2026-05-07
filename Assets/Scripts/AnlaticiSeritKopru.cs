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

    /// <summary>0-6 (Aşama 1-7). OyunYoneticisi.Admin/Spin tarafından reroll/bant override için okunur.</summary>
    public int AktifAsama => _aktifAsama;

    /// <summary>0-indexed; aşamadaki kaçıncı spin. SpinAtildi() içinde artar, aşama değişiminde 0'a sıfırlanır.
    /// ScriptedSpinYoneticisi tarafından "bu aşamadaki spin sırası" olarak okunur (SimuleEtVeKaydetImpl başında).</summary>
    public int AsamadakiSpinSayaci => _aktifSpin;

    void Awake()
    {
        string aktifSahne = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (aktifSahne != "03_SenaryoluOyun")
        {
            Debug.Log("[AnlaticiSeritKopru] Aktif sahne " + aktifSahne + ", anlatici devre disi.");
            gameObject.SetActive(false);
            return;
        }
        _ornek = this;
    }
    void OnDestroy() { if (_ornek == this) _ornek = null; }

    void Start()
    {
        _oy = FindObjectOfType<OyunYoneticisi>();
        if (_oy == null)
        {
            Debug.LogError("[AnlaticiSeritKopru] OyunYoneticisi bulunamadi");
            return;
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

#if UNITY_WEBGL && !UNITY_EDITOR
        AnlaticiPaneliAc("StreamingAssets/anlatici.html");
#else
        Debug.Log("[AnlaticiSeritKopru] Editor: HTML panel sadece WebGL'de açılır.");
#endif
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

    // ──────────────────────────────────────────────────────────────────
    //  PEDAGOJİK GEÇİŞ COROUTINE'LARI — eğitmen modalı + sonraki adım
    // ──────────────────────────────────────────────────────────────────

    /// <summary>Sahne girişinde otomatik: oyuncuyu simülasyona hazırlayan karşılama modalı.</summary>
    private System.Collections.IEnumerator PreA1KarsilamaAkisi()
    {
        // ScriptedModalKopru'nun spawn olmasını bekle (RuntimeInitializeOnLoadMethod ardından bir frame)
        yield return null;
        var modal = UnityEngine.Object.FindObjectOfType<Senaryo.Scripted.ScriptedModalKopru>();
        if (modal == null)
        {
            Debug.LogWarning("[Anlatici] PreA1 — ScriptedModalKopru bulunamadı, karşılama atlanıyor.");
            yield break;
        }
        string mesaj =
            "Hoş geldin. Bu simülasyonda online kumar oyunlarının insanları nasıl etkilediğini birlikte göreceğiz.\n\n" +
            "<b>Önce oyunu tanıyalım:</b>\n" +
            "• Karşında 6×5'lik meyve makinesi var. SPIN tuşuna basınca meyveler döner.\n" +
            "• Aynı meyveden <b>8 veya daha fazlası</b> bir araya gelirse kazanç verir.\n" +
            "• Bazı turlarda <b>ÇARPAN</b> düşer (×2, ×5, ×100 vs.) — kazancı katlar.\n" +
            "• Kazanan meyveler patlar, üstten yenileri düşer (<b>TUMBLE</b>) — zincir kazançlar olur.\n" +
            "• 4 Bonus Sembolü (yıldız) gelirse <b>BONUS</b> oyun açılır.\n\n" +
            "<b>Ekrandaki diğer öğeler:</b>\n" +
            "• <b>Sol panel:</b> hangi aşamadasın, sahne arkası ne oluyor — birlikte görelim diye buradan takip edeceğiz.\n" +
            "• <b>Bakiye:</b> oyuna ayrılan paran (50.000 TL ile başlıyorsun).\n" +
            "• <b>Bahis:</b> her spinde harcayacağın miktar, + ve − tuşlarıyla değişir.\n" +
            "• <b>KAZANÇ:</b> o spin'de kazandığın miktar.\n\n" +
            "Hadi başlayalım — ilk aşama <i>'Isındırma ve Umut'</i>.";
        yield return modal.ModalGoster(mesaj);
    }

    /// <summary>A1 son spini sonrası A2'ye geçiş anında: kontrol yanılsamasının başladığını anlatan modal.</summary>
    private System.Collections.IEnumerator A2GecisAkisi()
    {
        var modal = UnityEngine.Object.FindObjectOfType<Senaryo.Scripted.ScriptedModalKopru>();
        if (modal == null) yield break;
        string mesaj =
            "Birinci aşama tamamlandı. Şu an artıdasın, kendini iyi hissediyorsun.\n\n" +
            "Sırada 'Kontrol Bende Hissi' aşaması var. Bu aşamada algoritma sana üst üste kayıplar yaşatacak. Ama yine de hâlâ bakiyen pozitif olduğu için <i>'kontrol bende, ben isteyince çıkabilirim, bahis değişiklikleriyle kazanırım'</i> düşüneceksin.\n\n" +
            "İşte bu yanılsamayı birlikte göreceğiz.";
        yield return modal.ModalGoster(mesaj);
    }

    /// <summary>A2 son spini sonrası A3'e geçiş: kayıp kovalama tuzağı uyarısı.</summary>
    private System.Collections.IEnumerator A3GecisAkisi()
    {
        var modal = UnityEngine.Object.FindObjectOfType<Senaryo.Scripted.ScriptedModalKopru>();
        if (modal == null) yield break;
        string mesaj =
            "İkinci aşama tamamlandı. Şu an küçük kayıplar yaşadın ama hâlâ artıdasın. <i>'Kontrol bende'</i> hissin yerleşti.\n\n" +
            "Sırada <b>'Geri Kazanabilirim'</b> aşaması var. Bu aşamada algoritma kayıpları katlayacak. Sen <i>'azıcık daha bahis koyarsam telafi ederim'</i> düşüneceksin. Bu <b>'Kayıp Kovalama'</b> denilen psikolojik tuzak — bir kez bu tuzağa girilirse çıkmak çok zor.\n\n" +
            "Birlikte göreceğiz.";
        yield return modal.ModalGoster(mesaj);
    }

    /// <summary>A3 son spini sonrası A4'e geçiş: pes etme eşiği + manipülasyon vuruşu uyarısı.</summary>
    private System.Collections.IEnumerator A4GecisAkisi()
    {
        var modal = UnityEngine.Object.FindObjectOfType<Senaryo.Scripted.ScriptedModalKopru>();
        if (modal == null) yield break;
        string mesaj =
            "Üçüncü aşamayı gördün — kayıp kovalama tuzağı. Oyuncu bahsi yükselterek kurtulmaya çalıştı, daha çok kaybetti.\n\n" +
            "Sırada <b>'Şansım Döndü'</b> aşaması var. Bu aşamada algoritma oyuncuyu pes etme eşiğine getirecek — üst üste sert kayıplar. Tam pes etmek üzereyken büyük bir kazanç düşürecek. Bu büyük kazanç tesadüf değil, <b>kasıtlı bir manipülasyon vuruşu</b> olacak.\n\n" +
            "Amaç: oyuncuyu tekrar oyuna bağlamak.";
        yield return modal.ModalGoster(mesaj);
    }

    /// <summary>A4 son spini sonrası A5'e geçiş: bonus tuzağı uyarısı.</summary>
    private System.Collections.IEnumerator A5GecisAkisi()
    {
        var modal = UnityEngine.Object.FindObjectOfType<Senaryo.Scripted.ScriptedModalKopru>();
        if (modal == null) yield break;
        string mesaj =
            "Büyük kazancı yaşadın. Şu an <i>'şansım döndü, daha kazanırım'</i> hissindesin. İşte tam bu duygu, sıradaki aşamanın yakıtıdır.\n\n" +
            "Sırada <b>'Sonunu Düşünen Kahraman Olamaz'</b> aşaması var. Bu aşamada algoritma sana cazip bir <b>'bonus oyun tuzağı'</b> kuracak — tüm bakiyeni yatırma karşılığında büyük kazanç vaat edilecek. Yatırırsan, çok azını geri alacaksın.\n\n" +
            "Bu, sömürünün doruk noktasıdır. Birlikte göreceğiz.";
        yield return modal.ModalGoster(mesaj);
    }

    /// <summary>
    /// Borç Al onayı sonrası iki aşamalı asistan modal + bahis animasyonu (A6 girişi):
    ///   1) "Borç alındı, döngü başlıyor" pedagojik mesaj
    ///   2) "Bahis 10K'ya çıkacak" bilgilendirmesi + bahis animasyonu (mevcut → 10000)
    /// ScriptedYuklemePaneli.OnBorcAlTiklandi tarafından çağrılır.
    /// </summary>
    public System.Collections.IEnumerator BorcSonrasiModalAkisi()
    {
        yield return new WaitForSecondsRealtime(0.5f); // Yükleme paneli kapanma animasyonu

        var modal = UnityEngine.Object.FindObjectOfType<Senaryo.Scripted.ScriptedModalKopru>();
        if (modal == null) yield break;

        // 1) Döngü başlangıcı pedagojik mesaj
        yield return modal.ModalGoster(
            "İşte oyuncu borç aldı, bakiyesi yenilendi. Şimdi tekrar oynamaya devam edecek.\n\n" +
            "Kumar sitelerinde yeniden bakiye yükleyenlere bilinçli olarak ilk başlarda yine kazandırılır — bu <i>'Isındırma ve Umut'</i> aşamasına benzer.\n\n" +
            "Bu sayede oyuncu tekrar döngüye girer: <i>'şansım yine açıldı, kayıplarımı telafi ederim'</i> düşünür. Ama er ya da geç sistem kazanır, oyuncu kaybeder.\n\n" +
            "Şimdi bu döngüyü hızlıca göreceğiz."
        );

        // 2) A6 bahis bilgilendirme
        yield return modal.ModalGoster(
            "Bu kez oyuncu kayıplarını HIZLI telafi etmek istiyor. Bahsi <b>10.000 TL</b>'ye çıkardı.\n\n" +
            "Sadece 5 spin yetecek — algoritma sömürünün son evresinde tüm bakiyeyi alacak. " +
            "Bu hızlı bitiş, gerçek hayattaki <i>'son kez deneme'</i> bahanesinin sonucudur."
        );

        // 3) Bahis animasyonu: mevcut bahis → 10000 (kademeli artış, "+ tuşu" hissi)
        yield return BahisAnimasyonu(_oy != null ? _oy.AnlaticiMevcutBahis() : 4000, 10000);
    }

    /// <summary>Bahis kademeli artış animasyon helper. Adım otomatik hesaplanır:
    /// fark ≤ 2500 → 250 TL adım, daha büyük → 1000 TL adım. Her adım 0.10 sn (tick hissi).</summary>
    private System.Collections.IEnumerator BahisAnimasyonu(int eski, int yeni)
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

    /// <summary>A4 Spin 5 sonu ×100 çarpan: manipülasyon vuruşunun pedagojik açıklaması.</summary>
    private System.Collections.IEnumerator A4S5CarpanModalAkisi()
    {
        // Kullanıcı çarpanı + büyük kazancı görsün, tumble + ödeme animasyonları otursun
        yield return new WaitForSeconds(2.0f);
        var modal = UnityEngine.Object.FindObjectOfType<Senaryo.Scripted.ScriptedModalKopru>();
        if (modal == null) yield break;
        string mesaj =
            "⚡ Ekrana ×100 çarpan düştü! Az önce pes etmek üzereydin, şimdi büyük kazanç. " +
            "Bu rastlantı değil — algoritma seni tam bu duygusal anda yakaladı. <i>'Şansım döndü'</i> diyeceksin. " +
            "Aslında manipülasyon başarılı oldu.";
        yield return modal.ModalGoster(mesaj);
    }

    /// <summary>
    /// A5 sonu bakiye yetersiz → eğitmen "para arayışı" modalı çalar; modal kapanınca
    /// ScriptedYuklemePaneli "BORÇ AL — 50.000 TL" açılır.
    /// </summary>
    private System.Collections.IEnumerator BasaArayisAkisi()
    {
        // 1) Eğitmen modal — anlatıcı pedagojik açıklama (sol-alt karakter dialog)
        var modal = UnityEngine.Object.FindObjectOfType<Senaryo.Scripted.ScriptedModalKopru>();
        string mesaj =
            "Oyuncu artık paranın bittiğini fark etti.\n\n" +
            "Şimdi başka yerden para bulma arayışında. Yalan söylemeye başlıyor — " +
            "yakınlarına, akrabalarına, arkadaşlarına...\n\n" +
            "Bu, kumar bağımlılığının yıkıcı evresidir. Bir sonraki ekran o anı temsil ediyor.";
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
        }
        else
        {
            Debug.LogError("[Anlatici] BasaArayisAkisi — ScriptedYuklemePaneli bulunamadı!");
        }
    }

    /// <summary>
    /// A6 sonu bakiye yine bitti → eğitmen "döngü başa sardı" modalı çalar; modal kapanınca
    /// klasik A7 final ekranı (Tukenis) tetiklenir. Tek seferlik.
    /// </summary>
    private System.Collections.IEnumerator DonguAkisi()
    {
        if (_donguModalGosterildi) { Tukenis(); yield break; }
        _donguModalGosterildi = true;

        var modal = UnityEngine.Object.FindObjectOfType<Senaryo.Scripted.ScriptedModalKopru>();
        string mesaj =
            "Bakın, para tamamen bitti.\n\n" +
            "<b>5 spin'de 50.000 TL borç eridi.</b> Bu, gerçek hayatta <i>'hızlı kurtulma'</i> bahanesiyle yatırılan paraların kaderidir.\n\n" +
            "Şimdi oyuncu A1'e geri dönmek isteyecek. <i>'Belki bu sefer şanslıyım'</i> diye düşünüyor. <i>'Bir kerelik daha denersem...'</i> diyerek kendini kandırıyor.\n\n" +
            "İşte bağımlılığın özü budur: <b>KAYIP → BORÇ → KAYIP → BORÇ</b>. Sonsuz döngü.\n\n" +
            "Sonraki ekranda yaşanan toplam kayıp gösteriliyor.";
        if (modal != null)
            yield return modal.ModalGoster(mesaj);
        else
            Debug.LogWarning("[Anlatici] DonguAkisi — ScriptedModalKopru bulunamadı, modal atlanıyor.");

        Debug.Log("[Anlatici] DonguAkisi tamamlandı — Tukenis çağrılıyor.");
        Tukenis();
    }
}
