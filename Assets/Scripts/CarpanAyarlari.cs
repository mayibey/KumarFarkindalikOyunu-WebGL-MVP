using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// CarpanAyarlari - Çarpan sistemi ayarları ve mantığı

public class CarpanAyarlari : MonoBehaviour
{
    [Header("Carpan Uretimi")]
    public bool CarpanUretimiAktif = true;
    [Tooltip("Açıkken çarpan/bomba yalnızca bonus turunda devreye girer. Kapalıyken normal spinde de kullanılır.")]
    public bool CarpanSadeceBonus = false;

    [Range(0f, 1f)]
    public float CarpanUretimOlasiligi = 0.15f;

    [Min(0)]
    public int MaxCarpanAdedi = 2;

    [Min(0)]
    public int CarpanHavuzu = 10;

    [Tooltip("Rastgele çarpan seçilirken 100x/250x/500x gelme olasılığı (0-1). Yüksek = büyük çarpanlar daha sık düşer.")]
    [Range(0f, 1f)]
    public float YuksekCarpanOrani = 0.30f;

    [Tooltip("Test/Debug: Bir sonraki carpani zorla (0 ise kapali).")]
    public int ZorlaSiradakiCarpan = 0;

    [Header("Carpan Gorseli (Sweet Bonanza tarzi)")]
    public Sprite CarpanSembolSprite;

    [Tooltip("Carpan overlay boyutu")]
    public Vector2 CarpanOverlaySize = new Vector2(110f, 110f);

    [Tooltip("Carpan yazisi font boyutu")]
    public int CarpanOverlayFontSize = 54;

    [Header("Carpan Yazi Gorunumu")]
    public Color CarpanYaziRengi = Color.white;
    public Color CarpanYaziDisCizgiRengi = Color.black;

    [Range(0f, 1f)]
    public float CarpanYaziDisCizgiKalinlik = 0.35f;

    public bool CarpanYaziKalin = true;
    public bool CarpanYaziGolge = true;
    public Color CarpanYaziGolgeRengi = Color.black;
    public Vector2 CarpanYaziGolgeOffset = new Vector2(2f, -2f);

    [Header("Carpan Yazi Gradient (Sari-Turuncu)")]
    public bool CarpanGradientAktif = true;
    public Color CarpanGradientUst = new Color(1f, 0.922f, 0.231f, 1f);
    public Color CarpanGradientAlt = new Color(1f, 0.596f, 0f, 1f);
    public float CarpanCharacterSpacing = -15f;

    [Header("Carpan Yazi TMP Underlay (3D Pop)")]
    public bool CarpanUnderlayAktif = true;
    public Color CarpanUnderlayRengi = new Color(0.102f, 0.059f, 0.18f, 1f);
    public float CarpanUnderlayOffsetX = 2f;
    public float CarpanUnderlayOffsetY = -2f;
    [Range(0f, 1f)] public float CarpanUnderlayDilate = 0.2f;
    [Range(0f, 1f)] public float CarpanUnderlaySoftness = 0f;

    [Header("Carpan Yazi TMP Glow (Pariltı)")]
    public bool CarpanGlowAktif = true;
    public Color CarpanGlowRengi = new Color(1f, 0.922f, 0.231f, 0.6f);
    [Range(0f, 1f)] public float CarpanGlowOuter = 0.7f;
    [Range(0f, 1f)] public float CarpanGlowInner = 0f;
    [Range(0f, 1f)] public float CarpanGlowPower = 0.5f;

    [Header("Carpan Text Offset")]
    public Vector2 CarpanOverlayTextOffset = new Vector2(0f, -6f);

    [Header("Carpan Drop Anim")]
    public float CarpanOverlayDropStartYOffset = 250f;

    [Min(0f)]
    public float CarpanOverlayDropDuration = 0.18f;

    void Start()
    {
        // OyunYoneticisi referansını bul
        if (oyunYoneticisi == null)
            oyunYoneticisi = FindObjectOfType<OyunYoneticisi>();
        
        Debug.Log($"[CarpanAyarlari] OyunYoneticisi referansı: {(oyunYoneticisi != null ? "BAĞLANDI" : "BULUNAMADI")}");
        
        // Butonları otomatik bul ve bağla
        OtomatikButonlariBul();
    }
    
    private void OtomatikButonlariBul()
    {
        Button[] butonlar = FindObjectsOfType<Button>(true);
        foreach (Button b in butonlar)
        {
            if (b == null) continue;
            string adi = b.gameObject.name.ToLower(); // küçük harfe çevir
            
            if (adi.Contains("forcex2") && !adi.Contains("forcex100") && !adi.Contains("forcex20"))
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => ZorlaCarpan(2));
                Debug.Log($"[CarpanAyarlari] ForceX2 butonu bulundu: {b.gameObject.name}");
            }
            else if (adi.Contains("forcex5") && !adi.Contains("forcex50"))
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => ZorlaCarpan(5));
                Debug.Log($"[CarpanAyarlari] ForceX5 butonu bulundu: {b.gameObject.name}");
            }
            else if (adi.Contains("forcex10") && !adi.Contains("forcex100"))
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => ZorlaCarpan(10));
                Debug.Log($"[CarpanAyarlari] ForceX10 butonu bulundu: {b.gameObject.name}");
            }
            else if (adi.Contains("forcex50"))
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => ZorlaCarpan(50));
                Debug.Log($"[CarpanAyarlari] ForceX50 butonu bulundu: {b.gameObject.name}");
            }
            else if (adi.Contains("forcex100"))
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => ZorlaCarpan(100));
                Debug.Log($"[CarpanAyarlari] ForceX100 butonu bulundu: {b.gameObject.name}");
            }
            else if (adi.Contains("carpan") && (adi.Contains("sifirla") || adi.Contains("reset")))
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => ZorlaSiradakiCarpan = 0);
                Debug.Log($"[CarpanAyarlari] CarpanSifirla butonu bulundu: {b.gameObject.name}");
            }
        }
    }

    // ========== ÇARPAN MANTIĞI ==========
    
    private int _carpanKalanBuSpin = 0;
    private readonly List<int> _pendingCarpanDusurecek = new List<int>();
    private long _spinCarpanCarpim = 0;
    private readonly List<int> _spinCarpanDegerleri = new List<int>();
    
    // Referans
    public OyunYoneticisi oyunYoneticisi;

    // ========== BUTONLAR İÇİN FONKSİYONLAR ==========
    
    public virtual void ZorlaCarpan(int deger)
    {
        ZorlaSiradakiCarpan = deger;
        
        // OyunYoneticisi ile senkronize et - DİREKT GÜNCELLE!
        if (oyunYoneticisi != null)
        {
            oyunYoneticisi.zorlaSiradakiCarpan = deger;
            // OyunYoneticisi'nin carpanUretimiAktif değerini de güncelle
            oyunYoneticisi.carpanUretimiAktif = CarpanUretimiAktif;
            oyunYoneticisi.carpanSadeceBonus = CarpanSadeceBonus;
            oyunYoneticisi.carpanUretimOlasiligi = Mathf.Clamp01(CarpanUretimOlasiligi);
            oyunYoneticisi.maxCarpanAdedi = Mathf.Max(0, MaxCarpanAdedi);
            oyunYoneticisi.carpanHavuzu = Mathf.Max(0, CarpanHavuzu);
            oyunYoneticisi.yuksekCarpanOrani = Mathf.Clamp01(YuksekCarpanOrani);
            // Force değeri değişince önceden hesaplanmış (ön spin) sonucu geçersiz kıl; her seferinde son seçim geçerli olsun.
            oyunYoneticisi.AdminForceOncedenHesaplananSpinTemizle();
        }
        
        Debug.Log($"[CarpanAyarlari] Zorla çarpan: x{deger} -> OyunYoneticisi.zorlaSiradakiCarpan={oyunYoneticisi.zorlaSiradakiCarpan}");
    }

    public virtual void ZorlaCarpanSifirla()
    {
        ZorlaSiradakiCarpan = 0;
        Debug.Log("[CarpanAyarlari] Zorla çarpan sıfırlandı");
    }

    public virtual void ZorlaCarpan2() => ZorlaCarpan(2);
    public virtual void ZorlaCarpan5() => ZorlaCarpan(5);
    public virtual void ZorlaCarpan10() => ZorlaCarpan(10);
    public virtual void ZorlaCarpan50() => ZorlaCarpan(50);
    public virtual void ZorlaCarpan100() => ZorlaCarpan(100);

    // ========== ÇARPAN MANTIĞI METOTLARI ==========

    public virtual long GetCarpanCarpim() => _spinCarpanCarpim;
    public virtual int GetCarpanKalan() => _carpanKalanBuSpin;

    public virtual int RastgeleCarpan()
    {
        float oran = Mathf.Clamp01(YuksekCarpanOrani);
        if (oran > 0f && Random.value < oran)
        {
            int[] yuksek = new int[] { 100, 250, 500 };
            return yuksek[Random.Range(0, yuksek.Length)];
        }
        int[] pool = new int[] { 2, 3, 5, 10, 20, 50, 100, 200, 500, 1000 };
        int n = Mathf.Clamp(CarpanHavuzu, 1, pool.Length);
        return pool[Random.Range(0, n)];
    }

    public virtual int GetCurrentMultiplierInt()
    {
        long m = _spinCarpanCarpim;
        if (m > int.MaxValue) return int.MaxValue;
        return (int)m;
    }

    public virtual int UygulaSpinCarpani(int spinKazanci)
    {
        if (spinKazanci <= 0) return 0;

        long m = GetCarpanCarpim();
        if (m > 1)
        {
            long sonuc = (long)spinKazanci * m;
            if (sonuc > int.MaxValue) sonuc = int.MaxValue;
            return (int)sonuc;
        }
        return spinKazanci;
    }
}
