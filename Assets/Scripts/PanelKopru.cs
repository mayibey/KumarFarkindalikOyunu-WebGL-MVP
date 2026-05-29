using UnityEngine;
using UnityEngine.SceneManagement;
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

    // FAZ35.78: Yeni 03_AdminOyunScene (idx 2) için sol kenar sabit panel + toggle yardımcı.
    [DllImport("__Internal")]
    private static extern void PaneliAcSolKenar(string url);

    [DllImport("__Internal")]
    private static extern int PanelAcikMi();

    [DllImport("__Internal")]
    private static extern void AyarlariPanelleGonder(string json);

    [DllImport("__Internal")]
    private static extern void BahisPaneliAc(string url);

    [DllImport("__Internal")]
    private static extern void BahisPaneliKapat();

    [DllImport("__Internal")]
    private static extern void BahisPaneliBakiyeGonder(int bakiye);

    // ===== OYUN AYARLARI (panel state takibi) =====
    // FAZ35.76: kazanmaOrani + yeniOyuncuModu static field'ları SİLİNDİ — panel kartları kaldırıldı.
    public static float minCarpan = 0f;         // min ödeme bahis katı (0=devre dışı)
    public static float maksCarpan = 0f;        // maks ödeme bahis katı (0=devre dışı)
    public static float yakinKacirma = 40f;     // bu projede karşılığı YOK
    public static int ardisikKayipLimiti = 8;
    public static bool carpanTumbleAktif = true;
    public static string bonusModu = "manuel";
    public static int bonusOtomatikSpinPeriyodu = 200;
    public static string aktifSenaryo = "normal";

    // FAZ35.81 Madde 2: Bonus modu manuel/otomatik geçişlerinde motor periyot cache.
    // Manuel'e geçerken motor periyodu (>0) cache'lenir + motor 0'lanır.
    // Otomatik'e dönerken cache restore edilir → kullanıcı slider değeri korunur.
    private static int _onceki_bonusOtomatikPeriyot = 0;

    // ===== PANELİ AÇMA =====
    public void AyarlarButonunaBasildi()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
            string tamYol = "StreamingAssets/" + panelDosyaYolu;
            int sahneIdx = SceneManager.GetActiveScene().buildIndex;
            // FAZ35.78: Yeni 03_AdminOyunScene (idx 2, Tutorial'sız admin) — sol kenar toggle.
            // 02 anlatıcı (idx 1) AyarlarButton kullanmaz; eski 04 Tutorial (idx 3) mevcut modal merkez akışında kalır.
            if (sahneIdx == 2)
            {
                if (PanelAcikMi() != 0) PaneliKapat();
                else PaneliAcSolKenar(tamYol);
            }
            else
            {
                PaneliAc(tamYol);
            }
        #else
            Debug.Log("[PanelKopru] Panel sadece WebGL build'de açılır. Editor'de test için browser'da panel.html'i aç.");
        #endif
    }

    // FAZ35.78: Yeni 03 admin sahnesi (idx 2) — sahne yüklenince paneli otomatik sol kenar açar.
    // 02 ve eski 04'te bu Start no-op (idx 1/3 != 2 guard ile atlanır).
    private void Start()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
            if (SceneManager.GetActiveScene().buildIndex == 2)
            {
                string tamYol = "StreamingAssets/" + panelDosyaYolu;
                PaneliAcSolKenar(tamYol);
            }
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
                // PAKET 14-FAZ29: Tutorial sahnesinde (build idx 3) SenaryoUygula SKIP.
                // SenaryoUygula "normal" branch'i AdminNormalOyunUygula çağırıyor → kullanıcı T4 %100
                // sliderından gelen carpanUretimOlasiligi=1.00 default 0.15'e EZİLİYORDU. Tutorial
                // pattern motoru zaten ayrı yoldan (TutorialAdminEnjeksiyonu → PatternBaslat) tetikleniyor.
                // FAZ35.77: Yeni 03_AdminOyunScene (Tutorial'sız) idx 2'ye girince eski 04 (Tutorial) idx 2→3.
                if (SceneManager.GetActiveScene().buildIndex == 3)
                {
                    Debug.Log($"[PanelKopru] Tutorial sahnesi → SenaryoUygula BYPASS (kullanıcı slider değerleri korunur). senaryo={deger}");
                    break;
                }
                SenaryoUygula(deger);
                break;

            // FAZ35.76: case "kazanmaOrani" SİLİNDİ — panel slider kaldırıldı. AdminSetOdemeEgilimi 03 ana
            // ödeme metodu olarak KORUNDU; 03 senaryoları SenaryoUygula üzerinden sabit yüzde gönderiyor.

            case "minCarpan":
                // FAZ35.81 Madde 1: LEGACY KALDIRILDI — eski 35.27 YOL Z'de motor okumuyordu, panel değeri saklanıyordu.
                // Şimdi motor odemeMinKat field'ına yansır → OdemeModelineUygunMu reroll bant kontrolü çalışır.
                minCarpan = float.Parse(deger, System.Globalization.CultureInfo.InvariantCulture);
                _oy?.AdminSetMinCarpanDegeri(minCarpan);
                break;

            case "maksCarpan":
                // FAZ35.81 Madde 1: LEGACY KALDIRILDI — şimdi motor odemeMaksKat field'ına yansır.
                maksCarpan = float.Parse(deger, System.Globalization.CultureInfo.InvariantCulture);
                _oy?.AdminSetMaksCarpanDegeri(maksCarpan);
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

            // FAZ35.76: case "yeniOyuncu" SİLİNDİ — panel toggle kaldırıldı + AdminSetYeniOyuncuModu metodu silindi.

            case "bonusModu":
                // FAZ35.81 Madde 2: bonusModu artık motor tarafına yansır (önce sadece cosmetic field idi).
                // Manuel → motor bonusOtomatikSpinPeriyodu = 0 (otomatik tetik DEVRE DIŞI).
                // Otomatik → cache'lenmiş kullanıcı periyodu restore (varsa).
                bonusModu = deger;
                if (_oy != null)
                {
                    if (deger == "manuel")
                    {
                        int simdikiPeriyot = _oy.GetBonusOtomatikSpinPeriyodu();
                        if (simdikiPeriyot > 0) _onceki_bonusOtomatikPeriyot = simdikiPeriyot;
                        _oy.AdminSetBonusOtomatikSpinPeriyodu(0);
                        Debug.Log($"[PanelKopru bonusModu] manuel → periyot 0 (cache={_onceki_bonusOtomatikPeriyot})");
                    }
                    else if (deger == "otomatik" && _onceki_bonusOtomatikPeriyot > 0)
                    {
                        _oy.AdminSetBonusOtomatikSpinPeriyodu(_onceki_bonusOtomatikPeriyot);
                        Debug.Log($"[PanelKopru bonusModu] otomatik → periyot {_onceki_bonusOtomatikPeriyot} restore");
                    }
                }
                break;

            case "bonusYuzde":
                // PAKET 14-FAZ26: TutorialAdminEnjeksiyonu.AyarDegisti event subscriber zaten yakalıyor
                // (SonBonusYuzdesi static field). Burada "Bilinmeyen ayar" warning çıkmasın diye no-op case.
                break;

            case "bonusOtomatikOran":
                if (int.TryParse(deger, out int oran))
                {
                    // PAKET 14-FAZ26: Tutorial sahnesinde (build idx 3) bonusOtomatikSpinPeriyodu kullanıcı
                    // slider hareketinde değişmesin. T5 slider %100 → periyot=1 → otomatik bonus tetiği
                    // pattern motoru ile çakışıyor (4 scatter düşmeden bonus oyun açılıyor).
                    // FAZ35.77: Yeni 03_AdminOyunScene (Tutorial'sız) idx 2'ye girince eski 04 (Tutorial) idx 2→3.
                    if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex == 3)
                    {
                        Debug.Log($"[PanelKopru] Tutorial sahnesi (idx 3) — bonusOtomatikOran={oran} BYPASS (pattern motor yönetiyor)");
                        break;
                    }
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
                    // PAKET 14-FAZ25 (debug): Slider event gerçekten geliyor mu + field set sonrası değer
                    Debug.Log($"[PanelKopru carpanOlasilik] olasilik={olasilik}, carpanUretimOlasiligi={_oy?.carpanUretimOlasiligi:F2}");
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
                // FAZ35.81 Madde 4: Tutorial sahnesinde (eski 04, idx 3) VarsayilanaDon DEVRE DIŞI.
                // Pedagojik akış scripted pattern'ları + carpanUretimi state'ini yönetir; user reset bunu bozardı.
                if (SceneManager.GetActiveScene().buildIndex == 3)
                {
                    Debug.Log("[PanelKopru] Tutorial sahnesi (idx 3) — VarsayilanaDon BYPASS (pedagojik akış korunur)");
                    break;
                }
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
            // FAZ35.76: kazanmaOrani = X satırları SİLİNDİ (static field kaldırıldı). AdminSetOdemeEgilimi
            // çağrıları KORUNDU — 03 senaryolarının ödeme eğilimi yolu sağlam (sabit yüzde 85/25/45/15).
            case "normal":
                minCarpan = 0f;
                maksCarpan = 0f;
                yakinKacirma = 20f;
                _oy?.AdminNormalOyunUygula();
                break;

            case "hook":  // Yeni avlanan modu — yüksek kazanma, düşük tavan
                minCarpan = 0f;
                maksCarpan = 5f;
                yakinKacirma = 60f;
                _oy?.AdminSetOdemeEgilimi(85);
                break;

            case "yontma":  // Oyuncuyu yıprat
                minCarpan = 0f;
                maksCarpan = 3f;
                yakinKacirma = 70f;
                _oy?.AdminSetOdemeEgilimi(25);
                break;

            case "tutma":  // Oyuncuyu tut
                minCarpan = 0.5f;
                maksCarpan = 8f;
                yakinKacirma = 80f;
                _oy?.AdminSetOdemeEgilimi(45);
                break;

            case "koruma":  // Kasa koru
                minCarpan = 0f;
                maksCarpan = 2f;
                yakinKacirma = 90f;
                _oy?.AdminSetOdemeEgilimi(15);
                break;
        }

        // FAZ35.81 Madde 3: Senaryo butonu yakinKacirma motor yansıt bug fix.
        // Önce SenaryoUygula içinde sadece PanelKopru.yakinKacirma static field set ediliyordu;
        // motor field yakinKacirmaDegeri10da değişmiyordu → senaryo "near-miss" pedagojisi etkisiz.
        // Ölçek dönüşümü: panel 0-100 → motor 0-10 (AdminSetYakinKacirma 0-10 ölçek, Admin.cs:582 clamp).
        _oy?.AdminSetYakinKacirma(Mathf.RoundToInt(yakinKacirma / 10f));

        Debug.Log($"[PanelKopru] Senaryo uygulandı: {senaryo}, yakinKacirma motor 10'da {Mathf.RoundToInt(yakinKacirma / 10f)}");
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
        // FAZ35.76: kazanmaOrani + yeniOyuncuModu reset SİLİNDİ — static field'lar kaldırıldı.
        minCarpan = 0f;
        maksCarpan = 0f;
        yakinKacirma = 40f;
        ardisikKayipLimiti = 8;
        bonusModu = "manuel";
        aktifSenaryo = "normal";
        _oy?.AdminNormalOyunUygula();

        // FAZ35.81 Madde 4: FULL RESET — önceki VarsayilanaDon sadece odemeEgilimi (%65) reset ediyordu;
        // carpanUretimOlasiligi, maxCarpanAdedi, yakinKacirmaDegeri10da, _ardisikKayipLimiti, _carpanTumbleAktif,
        // bonusOtomatikSpinPeriyodu motor field'ları kullanıcının manuel değerinde kalıyordu → reset yanıltıcı.
        // Şimdi 6 motor field'ı Fields.cs default'larına çekilir.
        _oy?.AdminSetCarpanOlasilik(15);          // carpanUretimOlasiligi default 0.15 (Fields.cs:475)
        _oy?.AdminSetMaxCarpanTekSpin(3);         // maxCarpanAdedi default 3 (Fields.cs:476)
        _oy?.AdminSetYakinKacirma(0);             // yakinKacirmaDegeri10da default 0 (Admin.cs:581)
        _oy?.AdminSetArdisikKayipLimiti(8);       // _ardisikKayipLimiti default 8 (Fields.cs:140)
        _oy?.AdminSetCarpanTumbleAktif(true);     // _carpanTumbleAktif default true (Fields.cs:51)
        _oy?.AdminSetBonusOtomatikSpinPeriyodu(0); // bonusOtomatikSpinPeriyodu default 0 = devre dışı (Admin.cs:542)
        // FAZ35.81 Madde 1: Min/Maks Çarpan reset (0 = devre dışı, mevcut RNG akışı).
        _oy?.AdminSetMinCarpanDegeri(0f);
        _oy?.AdminSetMaksCarpanDegeri(0f);

        Debug.Log("[PanelKopru] Varsayılan ayarlara dönüldü (FULL RESET: 8 motor field default'a + odemeEgilimi=65 + Min/Maks devre dışı)");
    }

    // ===== MEVCUT AYARLARI PANELE GÖNDER =====
    private void MevcutAyarlariGonder()
    {
        string json = JsonUtility.ToJson(new AyarlarSnapshot
        {
            // FAZ35.76: kazanmaOrani + yeniOyuncuModu snapshot field'ları SİLİNDİ.
            minCarpan          = minCarpan,
            maksCarpan         = maksCarpan,
            bahis              = _oy != null ? _oy.GetMevcutBahis() : 0,
            yakinKacirma       = yakinKacirma,
            ardisikKayipLimiti = ardisikKayipLimiti,
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
        // FAZ35.76: kazanmaOrani + yeniOyuncuModu field'ları SİLİNDİ.
        public float  minCarpan;
        public float  maksCarpan;
        public int    bahis;
        public float  yakinKacirma;
        public int    ardisikKayipLimiti;
        public bool   carpanTumbleAktif;
        public string bonusModu;
        public string aktifSenaryo;
    }
}
