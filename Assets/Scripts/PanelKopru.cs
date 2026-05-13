using UnityEngine;
using System.Runtime.InteropServices;

/// <summary>
/// Panel ile Unity oyunu arasında köprü kuran script.
/// Bu script'i sahnedeki boş bir GameObject'e ekle ve adını "PanelKopru" yap.
/// </summary>
public class PanelKopru : MonoBehaviour
{
    /// <summary>
    /// panel.html'den her ayar değişimi (AyariIsle) tetiklendiğinde
    /// (key, value) ile invoke edilir. Tutorial sistemi (04_AdminOyunScene)
    /// subscribe eder; 03_SenaryoluOyun'da subscribe edilmediği için no-op.
    /// </summary>
    public static event System.Action<string, string> OnAyarDegisti;

    [Header("Panel Referansı")]
    [Tooltip("Panel HTML dosyasının yolu. StreamingAssets içindeki dosyayı kullanır.")]
    public string panelDosyaYolu = "panel.html"; // sadece dosya adı, yol Application.streamingAssetsPath ile eklenir

    private OyunYoneticisi _oy;
    private void Awake()
    {
        _oy = FindObjectOfType<OyunYoneticisi>();
        if (_oy == null)
            Debug.LogError("[PanelKopru] OyunYoneticisi bulunamadi! _oy NULL — Admin metod cagrilamiyor.");
        else
            Debug.Log("[PanelKopru] Awake: _oy referansi bulundu -> " + _oy.gameObject.name);
    }

    // ===== JavaScript ile iletişim için import'lar =====
    [DllImport("__Internal")]
    private static extern void PaneliAc(string url);

    [DllImport("__Internal")]
    private static extern void PaneliKapat();

    [DllImport("__Internal")]
    private static extern void AyarlariPanelleGonder(string json);

    [DllImport("__Internal")]
    private static extern void BahisPaneliAc(string url);

    [DllImport("__Internal")]
    private static extern void BahisPaneliKapat();

    [DllImport("__Internal")]
    private static extern void BahisPaneliBakiyeGonder(int bakiye);

    // ===== OYUN AYARLARI (panel state takibi) =====
    public static float kazanmaOrani = 65f;
    public static float minCarpan = 0f;         // min ödeme bahis katı (0=devre dışı)
    public static float maksCarpan = 0f;        // maks ödeme bahis katı (0=devre dışı)
    public static float yakinKacirma = 40f;     // bu projede karşılığı YOK
    public static int ardisikKayipLimiti = 8;
    public static bool yeniOyuncuModu = true;
    public static bool carpanTumbleAktif = true;
    public static string bonusModu = "manuel";
    public static int bonusOtomatikSpinPeriyodu = 200;
    public static string aktifSenaryo = "normal";

    // ===== PANELİ AÇMA =====
    public void AyarlarButonunaBasildi()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
            string tamYol = "StreamingAssets/" + panelDosyaYolu;
            PaneliAc(tamYol);
        #else
            Debug.Log("[PanelKopru] Panel sadece WebGL build'de açılır. Editor'de test için browser'da panel.html'i aç.");
        #endif
    }

    // ===== BAHİS SEÇİM PANELİ (küçük HTML iframe) =====
    /// <summary>Bahis +/- butonlarına basıldığında çağrılır. WebGL'de bahisSec.html iframe açar;
    /// Editor'da OyunYoneticisi'in Unity UI fallback'ine yönlendirir.</summary>
    public void BahisSecPaneliAc()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
            BahisPaneliAc("StreamingAssets/bahisSec.html");
            // Bakiye iframe yüklendikten sonra postMessage ile gönderilir.
            int bakiye = (_oy != null) ? _oy.BahisPanelMevcutBakiye() : 0;
            StartCoroutine(BahisBakiyeGonderGecikmeli(bakiye, 0.3f));
        #else
            Debug.Log("[PanelKopru] Editor: Unity UI fallback BahisSecimPopupGoster çağrılıyor.");
            if (_oy != null) _oy.BahisSecimPopupGosterEditorFallback();
        #endif
    }

    private System.Collections.IEnumerator BahisBakiyeGonderGecikmeli(int bakiye, float gecikme)
    {
        yield return new UnityEngine.WaitForSeconds(gecikme);
        #if UNITY_WEBGL && !UNITY_EDITOR
            BahisPaneliBakiyeGonder(bakiye);
        #endif
    }

    // ===== PANELDEN GELEN MESAJLAR =====
    public void AyarAl(string jsonMesaj)
    {
        Debug.Log("[PanelKopru] AyarAl cagrildi: " + jsonMesaj);
        try
        {
            AyarData data = JsonUtility.FromJson<AyarData>(jsonMesaj);
            AyariIsle(data.key, data.value);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[PanelKopru] Mesaj parse edilemedi: " + e.Message);
        }
    }

    private void AyariIsle(string anahtar, string deger)
    {
        Debug.Log($"[PanelKopru] AyariIsle: anahtar='{anahtar}' deger='{deger}' | _oy null={_oy == null}");

        // Tutorial sistemi için sinyal — 03'te subscribe yok, no-op.
        OnAyarDegisti?.Invoke(anahtar, deger);

        switch (anahtar)
        {
            case "oyunModu":
                aktifSenaryo = deger;
                SenaryoUygula(deger);
                break;

            case "kazanmaOrani":
                kazanmaOrani = float.Parse(deger, System.Globalization.CultureInfo.InvariantCulture);
                _oy?.AdminSetOdemeEgilimi(Mathf.RoundToInt(kazanmaOrani));
                break;

            case "minCarpan":
                minCarpan = float.Parse(deger, System.Globalization.CultureInfo.InvariantCulture);
                _oy?.AdminSetMinOdemeCarpan(minCarpan);
                break;

            case "maksCarpan":
                maksCarpan = float.Parse(deger, System.Globalization.CultureInfo.InvariantCulture);
                _oy?.AdminSetMaksOdemeCarpan(maksCarpan);
                break;

            case "yakinKacirma":
                // UI-5LIK: panel 0-5 ölçek gönderir; backend YakinKacirmaDegeri10da 0-10 ölçek ister → *2 ile çevir.
                // Tutorial tarafı bu değeri TutorialAdminEnjeksiyonu üzerinden ayrıca yakalar (kendi 0-5 mantığı);
                // PanelKopru.yakinKacirma static field'i Tutorial T8 koşul kontrolünde (>0) yalnızca varlık testi yapar.
                yakinKacirma = float.Parse(deger, System.Globalization.CultureInfo.InvariantCulture);
                int yk10da = Mathf.Clamp(Mathf.RoundToInt(yakinKacirma * 2f), 0, 10);
                _oy?.AdminSetYakinKacirma(yk10da);
                break;

            case "ardisikKayip":
                ardisikKayipLimiti = int.Parse(deger);
                _oy?.AdminSetArdisikKayipLimiti(ardisikKayipLimiti);
                break;

            case "yeniOyuncu":
                {
                    yeniOyuncuModu = deger == "True" || deger == "true";
                    // PAKET 14-FAZ10: T6YO sırasında AdminSetYeniOyuncuModu çağrısı BYPASS — bu method
                    // maxOdeme=1000 set ediyor; yeniOyuncu_acik pattern hedefleri 2500/3000 TL limit-aware
                    // kayıp grid'e çeviriliyordu. Tutorial T6YO adımındaysa state'i sadece PanelKopru'da
                    // güncelle (parametreKosulu lambda PanelKopru.yeniOyuncuModu okur), OyunYoneticisi
                    // davranışına dokunma → pattern hedefleri enjekte edilebilir.
                    bool t6yoAktif = KumarFarkindalik.Tutorial.TutorialOyunYoneticisi.Ornek != null
                        && KumarFarkindalik.Tutorial.TutorialOyunYoneticisi.Ornek.AdimYoneticisi != null
                        && KumarFarkindalik.Tutorial.TutorialOyunYoneticisi.Ornek.AdimYoneticisi.mevcutAdim
                            == KumarFarkindalik.Tutorial.TutorialAdimYoneticisi.TutorialAdimId.T6_YENI_OYUNCU;
                    if (!t6yoAktif)
                        _oy?.AdminSetYeniOyuncuModu(yeniOyuncuModu);
                    else
                        Debug.Log("[PanelKopru] T6YO aktif — AdminSetYeniOyuncuModu bypass (maxOdeme limiti uygulanmaz, pattern hedefleri korunur)");
                }
                break;

            case "bonusModu":
                bonusModu = deger;
                break;

            case "bonusOtomatikOran":
                if (int.TryParse(deger, out int oran))
                {
                    bonusOtomatikSpinPeriyodu = oran;
                    _oy?.AdminSetBonusOtomatikSpinPeriyodu(oran);
                }
                break;

            case "carpanSahteOrani":
                if (int.TryParse(deger, out int sahte))
                    _oy?.AdminSetCarpanSahteOrani(sahte);
                break;

            case "carpanOlasilik":
                if (int.TryParse(deger, out int olasilik))
                {
                    _oy?.AdminSetCarpanOlasilik(olasilik);
                    // PAKET 14-FAZ21: AdminSetCarpanOlasilik sadece carpanOlasilikYuzde (UI int field) set
                    // ediyor; DesenToKayit'ın okuduğu float carpanUretimOlasiligi default 0.15f kalıyor.
                    // Slider %100 yapsa bile çarpan düşmüyordu — gerçek mekanik field'ı da set et.
                    if (_oy != null) _oy.carpanUretimOlasiligi = Mathf.Clamp01(olasilik / 100f);
                }
                break;

            case "maxCarpanTekSpin":
                if (int.TryParse(deger, out int maxC))
                    _oy?.AdminSetMaxCarpanTekSpin(maxC);
                break;

            case "carpanTumble":
            case "carpanOdeme":
                carpanTumbleAktif = deger == "True" || deger == "true";
                _oy?.AdminSetCarpanTumbleAktif(carpanTumbleAktif);
                break;

            case "bonusTetikle":
                BonusOyunuTetikle();
                break;

            case "manuelBonusTetikle":
                _oy?.AdminManuelBonusBaslat();
                break;

            case "carpanZorla":
                CarpanZorla(int.Parse(deger));
                break;

            case "varsayilanaDon":
                VarsayilanaDon();
                break;

            case "tumAyarlar":
                Debug.Log("[PanelKopru] Tüm ayarlar uygulandı: " + deger);
                break;

            case "bahisSec":
                Debug.Log($"[PanelKopru] BAHIS HTML panelden geldi: ham_deger='{deger}'");
                if (int.TryParse(deger, out int bahisMiktari) && bahisMiktari > 0)
                {
                    Debug.Log($"[PanelKopru] BAHIS parse_sonuc={bahisMiktari}, AdminBahisAyarla çağrılıyor...");
                    if (_oy != null)
                    {
                        bool ok = _oy.AdminBahisAyarla(bahisMiktari);
                        Debug.Log($"[PanelKopru] AdminBahisAyarla sonuc={ok} (true=değişti+önbellek temizlendi, false=clamp/aynı)");
                    }
                    else
                    {
                        Debug.LogWarning("[PanelKopru] _oy null, bahis ayarlanamadı: " + bahisMiktari);
                    }
                }
                else
                {
                    Debug.LogWarning($"[PanelKopru] BAHIS parse hatası: '{deger}' geçersiz");
                }
                break;

            case "anlaticiAsamaDegis":
                if (int.TryParse(deger, out int yeniAsama))
                {
                    var ask = AnlaticiSeritKopru.Ornek ?? FindObjectOfType<AnlaticiSeritKopru>();
                    if (ask != null) ask.HtmlAsamaDegisti(yeniAsama);
                }
                break;

            case "anlaticiYenidenBaslat":
                var asky = AnlaticiSeritKopru.Ornek ?? FindObjectOfType<AnlaticiSeritKopru>();
                if (asky != null) asky.YenidenBaslat();
                break;

            case "paneliKapat":
                #if UNITY_WEBGL && !UNITY_EDITOR
                    PaneliKapat();
                    BahisPaneliKapat();
                #endif
                break;

            case "panelHazir":
                Debug.Log("[PanelKopru] Panel hazır, mevcut ayarlar gönderiliyor.");
                MevcutAyarlariGonder();
                break;

            default:
                Debug.LogWarning("[PanelKopru] Bilinmeyen ayar: " + anahtar);
                break;
        }
    }

    // ===== SENARYO UYGULAMA =====
    // kazanmaOrani + minCarpan/maksCarpan değerlerini senaryo bazlı ayarlar ve OyunYoneticisi'ne iletir.
    // yakinKacirma bu projede karşılıksız; sadece state tutulur.
    private void SenaryoUygula(string senaryo)
    {
        switch (senaryo)
        {
            case "normal":
                kazanmaOrani = 65f;
                minCarpan = 0f;
                maksCarpan = 0f;
                yakinKacirma = 20f;
                _oy?.AdminNormalOyunUygula();
                _oy?.AdminSetMinOdemeCarpan(0f);
                _oy?.AdminSetMaksOdemeCarpan(0f);
                break;

            case "hook":  // Yeni avlanan modu — yüksek kazanma, düşük tavan
                kazanmaOrani = 85f;
                minCarpan = 0f;
                maksCarpan = 5f;
                yakinKacirma = 60f;
                _oy?.AdminSetOdemeEgilimi(85);
                _oy?.AdminSetMinOdemeCarpan(0f);
                _oy?.AdminSetMaksOdemeCarpan(5f);
                break;

            case "yontma":  // Oyuncuyu yıprat
                kazanmaOrani = 25f;
                minCarpan = 0f;
                maksCarpan = 3f;
                yakinKacirma = 70f;
                _oy?.AdminSetOdemeEgilimi(25);
                _oy?.AdminSetMinOdemeCarpan(0f);
                _oy?.AdminSetMaksOdemeCarpan(3f);
                break;

            case "tutma":  // Oyuncuyu tut
                kazanmaOrani = 45f;
                minCarpan = 0.5f;
                maksCarpan = 8f;
                yakinKacirma = 80f;
                _oy?.AdminSetOdemeEgilimi(45);
                _oy?.AdminSetMinOdemeCarpan(0.5f);
                _oy?.AdminSetMaksOdemeCarpan(8f);
                break;

            case "koruma":  // Kasa koru
                kazanmaOrani = 15f;
                minCarpan = 0f;
                maksCarpan = 2f;
                yakinKacirma = 90f;
                _oy?.AdminSetOdemeEgilimi(15);
                _oy?.AdminSetMinOdemeCarpan(0f);
                _oy?.AdminSetMaksOdemeCarpan(2f);
                break;
        }

        Debug.Log($"[PanelKopru] Senaryo uygulandı: {senaryo}");
    }

    // ===== BONUS TETİKLEME =====
    private void BonusOyunuTetikle()
    {
        Debug.Log("[PanelKopru] Bonus oyunu manuel tetiklendi!");
        _oy?.AdminManuelBonusBaslat();
        OturumKayitcisi.EkleEvent(OturumKayitcisi.OlayTipi_BonusManuel, "panel üzerinden manuel tetikleme");
    }

    // ===== ÇARPAN ZORLAMA =====
    private void CarpanZorla(int carpan)
    {
        Debug.Log($"[PanelKopru] Çarpan zorlandı: x{carpan}");
        _oy?.AdminZorlaCarpanSec(carpan);
        OturumKayitcisi.EkleEvent(OturumKayitcisi.OlayTipi_CarpanZorla, $"carpan=x{carpan}");
    }

    // ===== VARSAYILANA DÖN =====
    private void VarsayilanaDon()
    {
        kazanmaOrani = 65f;
        minCarpan = 0f;
        maksCarpan = 0f;
        yakinKacirma = 40f;
        ardisikKayipLimiti = 8;
        yeniOyuncuModu = true;
        bonusModu = "manuel";
        aktifSenaryo = "normal";
        _oy?.AdminNormalOyunUygula();
        _oy?.AdminSetMinOdemeCarpan(0f);
        _oy?.AdminSetMaksOdemeCarpan(0f);
        Debug.Log("[PanelKopru] Varsayılan ayarlara dönüldü");
    }

    // ===== MEVCUT AYARLARI PANELE GÖNDER =====
    private void MevcutAyarlariGonder()
    {
        string json = JsonUtility.ToJson(new AyarlarSnapshot
        {
            kazanmaOrani       = kazanmaOrani,
            minCarpan          = minCarpan,
            maksCarpan         = maksCarpan,
            bahis              = _oy != null ? _oy.GetMevcutBahis() : 0,
            yakinKacirma       = yakinKacirma,
            ardisikKayipLimiti = ardisikKayipLimiti,
            yeniOyuncuModu     = yeniOyuncuModu,
            carpanTumbleAktif  = PanelKopru.carpanTumbleAktif,
            bonusModu          = bonusModu,
            aktifSenaryo       = aktifSenaryo
        });
        Debug.Log("[PanelKopru] MevcutAyarlariGonder: " + json);
        #if UNITY_WEBGL && !UNITY_EDITOR
            AyarlariPanelleGonder(json);
        #endif
    }

    // ===== VERİ SINIFLARI =====
    [System.Serializable]
    private class AyarData
    {
        public string source;
        public string key;
        public string value;
    }

    [System.Serializable]
    private class AyarlarSnapshot
    {
        public float  kazanmaOrani;
        public float  minCarpan;
        public float  maksCarpan;
        public int    bahis;
        public float  yakinKacirma;
        public int    ardisikKayipLimiti;
        public bool   yeniOyuncuModu;
        public bool   carpanTumbleAktif;
        public string bonusModu;
        public string aktifSenaryo;
    }
}
