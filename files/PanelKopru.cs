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
    public string panelDosyaYolu = "panel.html";

    // ===== JavaScript ile iletişim için import'lar =====
    [DllImport("__Internal")]
    private static extern void PaneliAc(string url);

    [DllImport("__Internal")]
    private static extern void PaneliKapat();

    // ===== OYUN AYARLARI =====
    // Bu değişkenler senin oyununun gerçek ayarlarına bağlanacak.
    // Kendi GameManager'ına göre düzenle.

    public static float kazanmaOrani = 65f;      // 0-100
    public static int maksKazanc = 200;          // TL
    public static float yakinKacirma = 40f;      // 0-100
    public static int ardisikKayipLimiti = 8;    // spin sayısı
    public static bool yeniOyuncuModu = true;
    public static string bonusModu = "manuel";   // "manuel" veya "otomatik"
    public static string aktifSenaryo = "normal";

    // ===== PANELİ AÇMA (Ayarlar butonundan çağırılır) =====
    public void AyarlarButonunaBasildi()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
            PaneliAc(panelDosyaYolu);
        #else
            Debug.Log("[PanelKopru] Panel sadece WebGL build'de açılır. Editor'de test için browser'da panel.html'i aç.");
        #endif
    }

    // ===== PANELDEN GELEN MESAJLAR =====
    // HTML'den postMessage ile gelen mesajları işler.
    // Bu fonksiyonu JS tarafından SendMessage ile çağırabilirsin.
    public void AyarAl(string jsonMesaj)
    {
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

    // Her ayar değişikliğini burada işle
    private void AyariIsle(string anahtar, string deger)
    {
        Debug.Log($"[PanelKopru] {anahtar} = {deger}");

        switch (anahtar)
        {
            case "oyunModu":
                aktifSenaryo = deger;
                SenaryoUygula(deger);
                break;

            case "kazanmaOrani":
                kazanmaOrani = float.Parse(deger);
                // GameManager.Instance.SetKazanmaOrani(kazanmaOrani);
                break;

            case "maksKazanc":
                maksKazanc = int.Parse(deger);
                // GameManager.Instance.SetMaksKazanc(maksKazanc);
                break;

            case "yakinKacirma":
                yakinKacirma = float.Parse(deger);
                // GameManager.Instance.SetYakinKacirma(yakinKacirma);
                break;

            case "ardisikKayip":
                ardisikKayipLimiti = int.Parse(deger);
                // GameManager.Instance.SetArdisikKayip(ardisikKayipLimiti);
                break;

            case "yeniOyuncu":
                yeniOyuncuModu = deger == "True" || deger == "true";
                // GameManager.Instance.SetYeniOyuncuModu(yeniOyuncuModu);
                break;

            case "bonusModu":
                bonusModu = deger;
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
                // JSON içinde tüm ayarlar geliyor
                Debug.Log("[PanelKopru] Tüm ayarlar uygulandı: " + deger);
                break;

            case "paneliKapat":
                #if UNITY_WEBGL && !UNITY_EDITOR
                    PaneliKapat();
                #endif
                break;

            case "panelHazir":
                Debug.Log("[PanelKopru] Panel hazır, mesajlar alınabilir.");
                break;

            default:
                Debug.LogWarning("[PanelKopru] Bilinmeyen ayar: " + anahtar);
                break;
        }
    }

    // ===== SENARYO UYGULAMA =====
    // Senaryo seçildiğinde birden fazla ayarı bir arada değiştir.
    private void SenaryoUygula(string senaryo)
    {
        switch (senaryo)
        {
            case "normal":
                kazanmaOrani = 50f;
                maksKazanc = 500;
                yakinKacirma = 20f;
                break;

            case "hook":  // Yeni avlanan modu
                kazanmaOrani = 85f;
                maksKazanc = 150;
                yakinKacirma = 60f;
                break;

            case "yontma":
                kazanmaOrani = 25f;
                maksKazanc = 100;
                yakinKacirma = 70f;
                break;

            case "tutma":
                kazanmaOrani = 45f;
                maksKazanc = 200;
                yakinKacirma = 80f;
                break;

            case "koruma":  // Kasa koruma
                kazanmaOrani = 15f;
                maksKazanc = 50;
                yakinKacirma = 90f;
                break;
        }

        Debug.Log($"[PanelKopru] Senaryo uygulandı: {senaryo}");
    }

    // ===== BONUS TETİKLEME =====
    private void BonusOyunuTetikle()
    {
        Debug.Log("[PanelKopru] Bonus oyunu manuel tetiklendi!");
        // GameManager.Instance.BonusBaslat();
    }

    // ===== ÇARPAN ZORLAMA =====
    private void CarpanZorla(int carpan)
    {
        Debug.Log($"[PanelKopru] Çarpan zorlandı: x{carpan}");
        // GameManager.Instance.SonrakiCarpaniAyarla(carpan);
    }

    // ===== VARSAYILANA DÖN =====
    private void VarsayilanaDon()
    {
        kazanmaOrani = 65f;
        maksKazanc = 200;
        yakinKacirma = 40f;
        ardisikKayipLimiti = 8;
        yeniOyuncuModu = true;
        bonusModu = "manuel";
        aktifSenaryo = "normal";
        Debug.Log("[PanelKopru] Varsayılan ayarlara dönüldü");
    }

    // ===== VERİ SINIFI =====
    [System.Serializable]
    private class AyarData
    {
        public string source;
        public string key;
        public string value;
    }
}
