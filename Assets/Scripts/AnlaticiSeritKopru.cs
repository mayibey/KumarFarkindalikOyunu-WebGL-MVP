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
    private const int SPIN_PER_ASAMA = 10;
    private const int BASLANGIC_BAKIYE = 50000;
    private static AnlaticiSeritKopru _ornek;

    /// <summary>Aşama bazlı önerilen bahis (yeniAsama geçişinde set edilir, kullanıcı sonra manuel değiştirebilir).</summary>
    private static readonly int[] _onerilenBahisler = new int[] { 100, 200, 500, 1000, 2000, 1000, 500 };

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
        new AsamaAyari { egilim = 100, maxCarpani = 2.0f, nearMiss = false }, // 1 Isındırma — çok kazandır
        new AsamaAyari { egilim = 100, maxCarpani = 1.5f, nearMiss = false }, // 2 Kontrol — biraz kazandır
        new AsamaAyari { egilim = 70,  maxCarpani = 1.0f, nearMiss = true  }, // 3 Geri kazanma
        new AsamaAyari { egilim = 30,  maxCarpani = 0.5f, nearMiss = true  }, // 4 Şansın döndü
        new AsamaAyari { egilim = 20,  maxCarpani = 0.3f, nearMiss = true  }, // 5 Sonunu düşünmeyen
        new AsamaAyari { egilim = 15,  maxCarpani = 0.2f, nearMiss = true  }, // 6 Para bulmalıyım
        new AsamaAyari { egilim = 10,  maxCarpani = 0.1f, nearMiss = true  }  // 7 Tükeniş
    };

    public static AnlaticiSeritKopru Ornek => _ornek;

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

        if (_aktifSpin >= SPIN_PER_ASAMA)
        {
            if (_aktifAsama < 6)
            {
                // Önce 10. spin çubuğunu rengiyle göster (HTML render)
                Guncelle();
                _aktifAsama++;
                _aktifSpin = 0;
                _asamaSpinNet.Clear(); // yeni aşama, çubuklar sıfırlansın
                AsamayiUygula(_aktifAsama);
            }
            else
            {
                Guncelle(); // 10. çubuğu son aşamada da göster
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
        string spinNetJson = "[" + string.Join(",", _asamaSpinNet.ConvertAll(n => n.ToString())) + "]";
        string json = "{\"asama\":" + _aktifAsama +
                      ",\"spin\":" + _aktifSpin +
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
