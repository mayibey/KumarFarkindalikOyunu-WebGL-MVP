using System.Collections;
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
    private const int SPIN_PER_ASAMA = 10;
    private static AnlaticiSeritKopru _ornek;

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

    void Awake() { _ornek = this; }
    void OnDestroy() { if (_ornek == this) _ornek = null; }

    void Start()
    {
        _oy = FindObjectOfType<OyunYoneticisi>();
        if (_oy == null)
        {
            Debug.LogError("[AnlaticiSeritKopru] OyunYoneticisi bulunamadi");
            return;
        }
        _baslangicBakiye = _oy.BahisPanelMevcutBakiye();
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
        _aktifSpin++;
        _toplamSpin++;

        if (_aktifSpin >= SPIN_PER_ASAMA)
        {
            if (_aktifAsama < 6)
            {
                _aktifAsama++;
                _aktifSpin = 0;
                AsamayiUygula(_aktifAsama);
            }
            else
            {
                Tukenis();
                Guncelle();
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
        int bahis = _oy.AnlaticiMevcutBahis();
        // KORUMA: Bahis henüz set edilmemiş olabilir (Start anında 0 gelir bootstrap öncesi).
        // Bu durumda maxOdeme = 0 olur ve oyun bozulur. Default minimum kullan.
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
        Debug.Log($"[Anlatici] Aşama {idx + 1} uygulandı: egilim=%{a.egilim}, maxCarpan={a.maxCarpani}x, bahis={bahis}, maxOdeme={maxOdeme} TL, nearMiss={a.nearMiss}");
    }

    private void Guncelle()
    {
        if (_oy == null) return;
        long bakiye = _oy.BahisPanelMevcutBakiye();
        long net = bakiye - _baslangicBakiye;
        string json = "{\"asama\":" + _aktifAsama +
                      ",\"spin\":" + _aktifSpin +
                      ",\"bakiyeNet\":" + net +
                      ",\"toplamSpin\":" + _toplamSpin + "}";
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
        AsamayiUygula(yeniAsama);
        Guncelle();
    }

    public void YenidenBaslat()
    {
        _aktifAsama = 0;
        _aktifSpin = 0;
        _toplamSpin = 0;
        if (_oy != null) _baslangicBakiye = _oy.BahisPanelMevcutBakiye();
        AsamayiUygula(0);
        Guncelle();
        Debug.Log("[Anlatici] Yeniden başlatıldı.");
    }
}
