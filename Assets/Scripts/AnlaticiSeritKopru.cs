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

    private OyunYoneticisi _oy;
    private int _aktifAsama = 0;
    private int _aktifSpin = 0;
    private int _toplamSpin = 0;
    private long _baslangicBakiye = 0;
    private int _sonUygulananAsama = -1; // YENI: aşama değişimi tespiti için
    private long _sonBakiye = 50000; // bir önceki spin sonu bakiye — spin başına net delta için
    private readonly List<int> _asamaSpinNet = new List<int>(); // mevcut aşamadaki spin başına net (+/-) TL
    private const int BASLANGIC_BAKIYE = 50000;
    private const int ASAMA7_GORSEL_MAX_CUBUK = 10; // Asama 7 dinamik (999 spin) — HTML max 10 çubuk göster
    private static AnlaticiSeritKopru _ornek;

    /// <summary>Aşama bazlı önerilen bahis (yeniAsama geçişinde set edilir, kullanıcı sonra manuel değiştirebilir).
    /// Pedagojik eğri: 50K → 60K → 75K → 70K → 55K → 30K → 10K → 0 (~61 spin).</summary>
    private static readonly int[] _onerilenBahisler = new int[] { 500, 1000, 1500, 2500, 4000, 2500, 1500 };

    /// <summary>Aşama başına spin eşiği. Aşama 7 = 999 (dinamik, bakiye yetince Tukenis guard'i tetikler).</summary>
    private static readonly int[] _asamaSpinSayisi = new int[] { 10, 10, 8, 8, 10, 10, 999 };

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

        // Bakiye 50.000 TL'ye reset
        _oy.AnlaticiBakiyeyiSifirla(BASLANGIC_BAKIYE);
        _baslangicBakiye = BASLANGIC_BAKIYE;
        _sonBakiye = BASLANGIC_BAKIYE;
        _asamaSpinNet.Clear();

        AsamayiUygula(0);

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

        if (_aktifSpin >= hedefSpin)
        {
            if (_aktifAsama < 6)
            {
                // Önce son spin çubuğunu rengiyle göster (HTML render)
                Guncelle();
                _aktifAsama++;
                _aktifSpin = 0;
                _asamaSpinNet.Clear(); // yeni aşama, çubuklar sıfırlansın
                AsamayiUygula(_aktifAsama);
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

        // Bakiye yetersizse Aşama 7 (Tükeniş) zorla atlanır veya doğrudan Tukenis tetiklenir.
        if (_oy != null)
        {
            int simdiBakiye = (int)_oy.BahisPanelMevcutBakiye();
            int sonrakiBahis = _onerilenBahisler[Mathf.Clamp(_aktifAsama, 0, _onerilenBahisler.Length - 1)];
            if (simdiBakiye < sonrakiBahis)
            {
                if (_aktifAsama < 6)
                {
                    Debug.Log($"[Anlatici] Bakiye yetersiz ({simdiBakiye} < {sonrakiBahis}), Aşama 7 (Tükeniş) zorla atlanıyor.");
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
}
