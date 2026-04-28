using UnityEngine;
using System.Runtime.InteropServices;

/// <summary>
/// Panel ile Unity oyunu arasında köprü kuran script.
/// Bu script'i sahnedeki boş bir GameObject'e ekle ve adını "PanelKopru" yap.
/// </summary>
public class PanelKopru : MonoBehaviour
{
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

    // ===== OYUN AYARLARI (panel state takibi) =====
    public static float kazanmaOrani = 65f;
    public static float minCarpan = 0f;         // min ödeme bahis katı (0=devre dışı)
    public static float maksCarpan = 0f;        // maks ödeme bahis katı (0=devre dışı)
    public static float yakinKacirma = 40f;     // bu projede karşılığı YOK
    public static int ardisikKayipLimiti = 8;
    public static bool yeniOyuncuModu = true;
    public static bool carpanTumbleAktif = true;
    public static string bonusModu = "manuel";
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
                // Bu projede near-miss (yakın kaçırma) özelliği bulunmuyor; değer yalnızca panel state'te tutulur.
                yakinKacirma = float.Parse(deger);
                break;

            case "ardisikKayip":
                ardisikKayipLimiti = int.Parse(deger);
                _oy?.AdminSetArdisikKayipLimiti(ardisikKayipLimiti);
                break;

            case "yeniOyuncu":
                yeniOyuncuModu = deger == "True" || deger == "true";
                _oy?.AdminSetYeniOyuncuModu(yeniOyuncuModu);
                break;

            case "bonusModu":
                bonusModu = deger;
                break;

            case "carpanTumble":
            case "carpanOdeme":
                carpanTumbleAktif = deger == "True" || deger == "true";
                _oy?.AdminSetCarpanTumbleAktif(carpanTumbleAktif);
                break;

            case "bonusTetikle":
                BonusOyunuTetikle();
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

            case "paneliKapat":
                #if UNITY_WEBGL && !UNITY_EDITOR
                    PaneliKapat();
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
