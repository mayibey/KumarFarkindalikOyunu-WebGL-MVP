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

    // FAZ35.98 İŞ1 B: Detaylı Ayarlar toggle state. AÇIK iken Normal mod delegate guard'ları (carpanUretimOlasiligi %10,
    // maxCarpanAdedi 1) bypass edilir → kullanıcı slider değerleri motora geçer. Toggle kapalı iken (default) Faz 35.95
    // baseline davranışı korunur (pedagojik RTP ~%55-65).
    public static bool detayliAyarlarAcik = false;

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

            case "modAktif":
                // FAZ35.87 İŞ4: Mod toggle KALDIRILDI. Bu case backward-compat için no-op.
                // Yeni akış: panel.html senaryoOtomatikUygula içinde dropdown value tabanlı kontrol; ayrı modAktif komutuna gerek yok.
                // Eski 35.83 mantığı (modAktif true/false → SenaryoUygula çağrı) silindi — dropdown onchange zaten bu işi yapıyor.
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
                // FAZ35.98 İŞ1 A: Toggle bool string ("True"/"False") veya sayısal değer gönderebilir.
                // Eski kod sadece float.Parse yapıyordu → "True" gelince FormatException → motor TEPKI VERMEZ idi.
                // Önce bool dene (toggle case), başarısızsa float dene (slider case).
                // Tutorial tarafı bu değeri TutorialAdminEnjeksiyonu üzerinden ayrıca yakalar (kendi 0-5 mantığı);
                // PanelKopru.yakinKacirma static field'i Tutorial T8 koşul kontrolünde (>0) yalnızca varlık testi yapar.
                bool ykAktif;
                if (bool.TryParse(deger, out var bYk))
                {
                    ykAktif = bYk;
                    yakinKacirma = ykAktif ? 5f : 0f;
                }
                else if (float.TryParse(deger, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var fYk))
                {
                    yakinKacirma = fYk;
                    ykAktif = fYk > 0f;
                }
                else
                {
                    ykAktif = false;
                    yakinKacirma = 0f;
                }
                int yk10da = ykAktif
                    ? Mathf.Clamp(Mathf.RoundToInt(yakinKacirma * 2f), 1, 10)  // slider değeri varsa *2, toggle ise 5*2=10 clamp
                    : 0;
                if (ykAktif && yk10da == 0) yk10da = 5;  // toggle AÇIK + yakinKacirma=5f → yk10da=10 ama emniyet
                _oy?.AdminSetYakinKacirma(yk10da);
                break;

            case "ardisikKayip":
                // FAZ35.82: number input → toggle dönüşümü. JS toggleDegisti("ardisikKayip") true/false bool gönderir.
                // true → 3 (3 üst üste kayıp sonrası kazanç zorla), false → 999 (etkisiz büyük değer = pratikte kapalı).
                bool ardisikAktif = bool.TryParse(deger, out var bAk) ? bAk : (int.TryParse(deger, out var nAk) && nAk > 0);
                ardisikKayipLimiti = ardisikAktif ? 3 : 999;
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

            case "bonusGirmeOlasilik":
                // FAZ35.103 İŞ2: Bonus oyuna girme olasılığı (0-1 float). Slider %X → her spin RNG check → 4 scatter zorla yerleştir → bonus tetik.
                // Yeni mekanik eski periyot tabanlı bonusOtomatikSpinPeriyodu yerine. Motor field Tutorial T5 için korunur (geri uyumluluk).
                if (float.TryParse(deger, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float bgOlasilik))
                {
                    bgOlasilik = Mathf.Clamp01(bgOlasilik);
                    _oy?.AdminSetBonusGirmeOlasilik(bgOlasilik);
                    // FAZ35.106 İŞ1: Precompute cache invalidate — slider değişimi anında etkili olsun.
                    _oy?.AdminForceOncedenHesaplananSpinTemizle();
                    Debug.Log($"[PanelKopru bonusGirmeOlasilik] olasilik={bgOlasilik:F2} ({(bgOlasilik * 100f):F0}%) (cache temizlendi)");
                }
                break;

            case "carpanOlasilik":
                if (int.TryParse(deger, out int olasilik))
                {
                    _oy?.AdminSetCarpanOlasilik(olasilik);
                    // PAKET 14-FAZ21: AdminSetCarpanOlasilik sadece carpanOlasilikYuzde (UI int field) set
                    // ediyor; DesenToKayit'ın okuduğu float carpanUretimOlasiligi default 0.15f kalıyor.
                    // Slider %100 yapsa bile çarpan düşmüyordu — gerçek mekanik field'ı da set et.
                    if (_oy != null) _oy.carpanUretimOlasiligi = Mathf.Clamp01(olasilik / 100f);
                    // FAZ35.106 İŞ1 KESIN FIX: Precompute cache invalidate — slider değişimi anında etkili olsun.
                    // ÖNCESI: PrecomputeNextSpinCoroutine arka planda eski carpanUretimOlasiligi ile spin hesaplıyordu;
                    // kullanıcı slider %100'e çekse bile bir sonraki spin eski state ile koşuyordu (görsel çarpan eksik).
                    _oy?.AdminForceOncedenHesaplananSpinTemizle();
                    // PAKET 14-FAZ25 (debug): Slider event gerçekten geliyor mu + field set sonrası değer
                    Debug.Log($"[PanelKopru carpanOlasilik] olasilik={olasilik}, carpanUretimOlasiligi={_oy?.carpanUretimOlasiligi:F2} (cache temizlendi)");
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

            case "detayliAyarlarAcik":
                // FAZ35.98 İŞ1 B: panel.html detayliAyarlarToggleDegisti AÇIK/KAPALI bildirimini buradan alır.
                // AÇIK iken OyunYoneticisi.cs:745+754 delegate guard'ları (Normal mod carpanUretimOlasiligi %10,
                // maxCarpanAdedi 1) bypass edilir → kullanıcı slider değerleri motora geçer.
                bool.TryParse(deger, out detayliAyarlarAcik);
                Debug.Log($"[FAZ35.98 İŞ1] Detaylı Ayarlar toggle: {detayliAyarlarAcik} (delegate bypass {(detayliAyarlarAcik ? "AKTİF" : "PASİF")})");
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
                // FAZ35.94 İŞ1: paneliKapat artık SADECE yönetici panelini kapatır (panel.html × butonu için).
                // Bahis modal kendi 'bahisPaneliKapat' key'ini kullanır (aşağıdaki yeni case). İki modal bağımsız.
                // Eski davranış: PaneliKapat() + BahisPaneliKapat() — bahis modal "OK" yönetici panelini istemeden kapatıyordu.
                #if UNITY_WEBGL && !UNITY_EDITOR
                    PaneliKapat();
                #endif
                break;

            case "bahisPaneliKapat":
                // FAZ35.94 İŞ1: Yeni izole key — bahisSec.html "OK"/"iptal" sonrası sadece bahis modal'ı kapatır.
                // Yönetici paneli açık kalır, kullanıcı bahis seçince paneli kaybetmez.
                #if UNITY_WEBGL && !UNITY_EDITOR
                    BahisPaneliKapat();
                #endif
                break;

            case "panelHazir":
                Debug.Log("[PanelKopru] Panel hazır, mevcut ayarlar gönderiliyor.");
                MevcutAyarlariGonder();
                break;

            case "uygulamaOnayi":
                // FAZ35.86 İŞ1: Tutorial-only field (TutorialAdminEnjeksiyonu.cs case işliyor idx 3'te).
                // idx 2 (yeni 03 admin) + idx 1 (02 anlatıcı)'de no-op (warning susturma). Akış kesilmez.
                break;

            default:
                Debug.LogWarning("[PanelKopru] Bilinmeyen ayar: " + anahtar);
                break;
        }
    }

    // ===== SENARYO UYGULAMA =====
    // FAZ35.83: Mod karakter revizyonu — saf karakterler, dar bantlar.
    // Hook 1.5x-2.5x + %90; Yontma 0.3x-0.7x + %70; Tutma 1.2x-1.8x + %15 + 2-1 deterministik döngü;
    // Koruma 0.1x-0.3x + %8; Normal 0/0 + %65 (kullanıcı manuel slider'la aktive eder).
    // YENİ 02 GUARD (buildIndex==1): Anlatıcı sahnesinde SenaryoUygula BYPASS — Anlatıcı kendi eğilimini yönetir.
    // Mevcut Tutorial guard (buildIndex==3) korunur. Mod değişimi → Tutma OFF (Tutma case'inde tekrar ON).
    private void SenaryoUygula(string senaryo)
    {
        if (_oy == null) return;

        // FAZ35.83 YENİ: 02 anlatıcı sahnesi (idx 1) BYPASS — AnlaticiSeritKopru kendi eğilimini set ediyor,
        // kullanıcı panelden mod değiştirirse Anlatıcı pedagojik akışı bozulurdu.
        int sahneIdx83 = SceneManager.GetActiveScene().buildIndex;
        if (sahneIdx83 == 1)
        {
            Debug.Log($"[PanelKopru FAZ35.83] 02 anlatıcı sahnesi (idx 1) — SenaryoUygula BYPASS (senaryo={senaryo})");
            return;
        }

        // FAZ35.82: Her mod değişiminde Tutma otomatik off — Tutma case'inde tekrar on.
        _oy.AdminSetTutmaModAktif(false);

        switch (senaryo)
        {
            case "normal":
                _oy.AdminSetOdemeEgilimi(65);
                _oy.AdminSetMinCarpanDegeri(0f);
                _oy.AdminSetMaksCarpanDegeri(0f);
                // FAZ35.140 K2: maxCarpanAdedi=1 — Normal mod havuzu {3,5,8} AMA Faz 35.115 random 1..maxAdet
                // çarpan birikiyor (5+8=13x taşması analiz aracında gözlemlendi). Tek çarpan ile tavan 8x.
                _oy.AdminSetMaxCarpanTekSpin(1);
                minCarpan = 0f; maksCarpan = 0f;
                break;

            case "hook":
                // FAZ35.83: Taze Kan — %85→%90 eğilim, 2/3→1.5/2.5 bant (dar, saf karakter)
                // FAZ35.140 K1: Bant 1.5/2.5 → 0.9/1.6 düşürüldü (RTP). Ama panel.html 1.5/2.5'te kalmıştı (gotcha → efektif 1.5/2.5).
                // FAZ36 ADIM 2: HookMotoru sözleşmesi "bahisin BİRAZ üstü" → band 1.1/1.7 (1650-2550 @1500). PanelKopru + 2 panel.html senkronlandı.
                _oy.AdminSetOdemeEgilimi(90);
                _oy.AdminSetMinCarpanDegeri(1.1f);
                _oy.AdminSetMaksCarpanDegeri(1.7f);
                // FAZ35.140 K2: maxCarpanAdedi=1 — Hook çarpan birikme engellendi (5+8=13x taşması bitti).
                _oy.AdminSetMaxCarpanTekSpin(1);
                minCarpan = 1.1f; maksCarpan = 1.7f;
                break;

            case "yontma":
                // FAZ35.83: %50→%70 eğilim (sürekli kazanç ama bahisten az → sessiz erime)
                _oy.AdminSetOdemeEgilimi(70);
                _oy.AdminSetMinCarpanDegeri(0.3f);
                _oy.AdminSetMaksCarpanDegeri(0.7f);
                minCarpan = 0.3f; maksCarpan = 0.7f;
                break;

            case "tutma":
                // FAZ35.83: bant 1/2→1.2/1.8 (dar bant, hep umut). Deterministik 2-kayıp-1-kazanç korunur.
                _oy.AdminSetOdemeEgilimi(15);
                _oy.AdminSetMinCarpanDegeri(1.2f);
                _oy.AdminSetMaksCarpanDegeri(1.8f);
                minCarpan = 1.2f; maksCarpan = 1.8f;
                _oy.AdminSetTutmaModAktif(true);
                break;

            case "koruma":
                // FAZ35.83: %5→%8 eğilim, bant 0.1/0.5→0.1/0.3 (neredeyse hiç kazanç)
                _oy.AdminSetOdemeEgilimi(8);
                _oy.AdminSetMinCarpanDegeri(0.1f);
                _oy.AdminSetMaksCarpanDegeri(0.3f);
                minCarpan = 0.1f; maksCarpan = 0.3f;
                break;

            // FAZ35.125: "ozel" modu — Detaylı Ayarlar AÇIK iken kullanıcı manuel kontrol.
            // KÖK NEDEN (keşif 35.125): Önceden Detaylı Ayarlar AÇIK iken panel.html:1370 unityeGonder('oyunModu','normal')
            // gönderiyordu → SenaryoUygula("normal") → min/max=0 sıfırlanıyor + aktifSenaryo="normal" guard 3 noktada
            // (Admin.cs:194/217, Spin.cs:528) min/max'ı YOK SAYIYORDU. Kullanıcı min=2 max=5 set etse bile motor görmüyordu.
            // ÇÖZÜM: panel.html:1370 → 'ozel'. Bu case min/max'a DOKUNMAZ (kullanıcı değeri korunur), aktifSenaryo='ozel'
            // set edilir (494'te). 3 guard "!= normal" testi → ozel'de TRUE → min/max aktifleşir → OdemeModelineUygunMu
            // reroll mekanizması nihaiOdeme'yi (çarpan dahil) bant'a sıkıştırır (KARAR 2 doğal davranış: motor bant aşan
            // simulasyonu reject eder, kabul edilen simulasyondaki çarpan zaten bant içi — görsel düşen çarpan = bant uyumlu).
            // Eğilim: default %65 (Faz 35.83 normal eğilim) — kullanıcı KARAR 1 isteği: "spin kazandığında bant içinde,
            // eğilim devrede, kayıp spin olabilir". Tutma OFF (yukarıda 450 zaten set ediyor) — ozel ile çakışmaz.
            // Tutorial T3 (aktifSenaryo == "hook" vb) etkilenmez — "ozel" string'i hook/yontma/tutma/koruma'dan farklı.
            // 02 anlatıcı bypass (line 443-446) — "ozel" 02'ye ulaşmaz. Faz 35.95 baseline ("normal") korunur — dropdown
            // gerçek Normal seçilince case "normal":454-459 min/max=0 sıfırlar, eski davranış.
            case "ozel":
                _oy.AdminSetOdemeEgilimi(65);
                // min/maksCarpan SIFIRLAMA YOK — kullanıcı manuel değeri panel input'lardan korunur.
                break;
        }
        aktifSenaryo = senaryo;

        // FAZ35.121: yakinKacirma artık SADECE case "yakinKacirma" toggle'ından kontrol edilir (line 199-226, *2f doğru ölçek).
        // ESKİ FAZ35.81 satırı KALDIRILDI: `_oy.AdminSetYakinKacirma(Mathf.RoundToInt(yakinKacirma / 10f));`
        // KÖK NEDEN (Faz 35.122 keşif + log kanıtı): yakinKacirma eskiden slider(0-100) idi, /10 ölçek dönüşümü o
        // zaman doğruydu (50 → 5). Faz 35.69'da slider → TOGGLE (5f sabit açık, 0f kapalı) dönüştürüldü AMA bu satır
        // KALINTI olarak kaldı. Toggle AÇIK iken yakinKacirma=5f → 5/10=0.5 → RoundToInt(0.5) Banker's rounding = 0
        // → AdminSetYakinKacirma(0) → motor field SIFIRLANIYORDU. SenaryoUygula her tetiğinde (panel açılışı, dropdown,
        // ayar değişimi) yakinKacirmaDegeri10da 10 → 0'a düşüyor → _yakinKacirmaBuSpinAktif flag FALSE → Faz 35.119
        // near-miss enjeksiyonu atlanıyordu → [FAZ35.119] log hiç çıkmıyordu → spin normal kazanç veriyordu.
        // Faz 35.120 cache invalidate fix doğruydu (log "cache temizlendi" görüldü) AMA bu bug field'ı eziyordu.
        // Hiçbir senaryo case'i (normal/hook/yontma/tutma/koruma) yakinKacirma'yı explicit set etmiyor → satır
        // kaldırılması GÜVENLİ. Doğru set tek noktada: case "yakinKacirma":222 `*2f` (toggle AÇIK 5f→10 ✓).
        // 02 + Tutorial T8: SenaryoUygula buildIndex==1/3'te BYPASS (line 170-173, 439-446) → onlar etkilenmez.

        Debug.Log($"[PanelKopru] FAZ35.83 Senaryo uygulandı: {senaryo}, min={minCarpan}, maks={maksCarpan}, tutmaAktif={(senaryo == "tutma")}");
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
        // FAZ35.98 İŞ1 D: Fields.cs:511 default 0.05f (=%5) ile hizalama. Önceden 15 set ediliyordu (eski default 0.15f).
        _oy?.AdminSetCarpanOlasilik(5);           // carpanUretimOlasiligi default 0.05 (Fields.cs:511)
        _oy?.AdminSetMaxCarpanTekSpin(3);         // maxCarpanAdedi default 3 (Fields.cs:476)
        _oy?.AdminSetYakinKacirma(0);             // yakinKacirmaDegeri10da default 0 (Admin.cs:581)
        // FAZ35.82: ardisikKayip number→toggle dönüşümü; toggle kapalı default → 999 (etkisiz büyük değer).
        _oy?.AdminSetArdisikKayipLimiti(999);
        _oy?.AdminSetCarpanTumbleAktif(true);     // _carpanTumbleAktif default true (Fields.cs:51)
        _oy?.AdminSetBonusOtomatikSpinPeriyodu(0); // bonusOtomatikSpinPeriyodu default 0 = devre dışı (Admin.cs:542)
        // FAZ35.81 Madde 1: Min/Maks Çarpan reset (0 = devre dışı, mevcut RNG akışı).
        _oy?.AdminSetMinCarpanDegeri(0f);
        _oy?.AdminSetMaksCarpanDegeri(0f);
        // FAZ35.82: Tutma modu reset (sayaç sıfırlanır + flag temizlenir).
        _oy?.AdminSetTutmaModAktif(false);

        // FAZ35.83: Mod toggle reset — JS taraf sayfa yüklendiğinde modKilitle(true) çağırır (load handler).
        // VarsayilanaDon C# tarafında ek bir motor state'i yok; yukarıdaki AdminSetMin/MaksCarpanDegeri(0f)
        // + AdminSetTutmaModAktif(false) zaten mod akışını sıfırlıyor. UI senkron için panel tarafı
        // varsayilanaDon JS handler'ında dropdown 'normal'e döner + modAktifToggle ON kalır (default).

        Debug.Log("[PanelKopru] Varsayılan ayarlara dönüldü (FULL RESET: motor field default'a + odemeEgilimi=65 + Min/Maks devre dışı + Tutma off + ardisikKayip kapalı + FAZ35.83 mod akışı sıfırlandı)");
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
