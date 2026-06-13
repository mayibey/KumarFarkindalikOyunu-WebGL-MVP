using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public partial class OyunYoneticisi
{
    public void SetZorluk(float deger)
    {
        _adminManuelZorlukKilidi = true;
        _zorlukServisi?.ZorlukUygula(deger);
        OncedenHesaplananSpinOnbelleginiTemizle();
    }

    public bool AdminManuelZorlukKilidiAktif() => _adminManuelZorlukKilidi;
    public void AdminManuelZorlukKilidiAyarla(bool aktif) => _adminManuelZorlukKilidi = aktif;

    void IZorlukBaglami.SetZorlukSliderDegeri(int v)
    {
        _zorlukSliderDegeri = v;
        zorlukSeviyesi = v; // Panel / MevcutAyarlarMetni bu değeri okur; gerçekten uygulanan zorluk ile senkron olsun.
    }
    void IZorlukBaglami.SetMinClusterSize(int value) => minClusterSize = value;
    void IZorlukBaglami.SetEasyBias01(float value) => _easyBias01 = value;
    void IZorlukBaglami.SetHardBias01(float value) => _hardBias01 = value;
    void IZorlukBaglami.SetScatterChanceNormal(float value) => scatterChanceNormal = value;
    void IZorlukBaglami.ZorlukUIMetinVeLogGuncelle(int v)
    {
        if (zorlukValueText != null)
            zorlukValueText.text = $"Zorluk: {v}";
        Debug.Log($"[ADMIN] Zorluk={v} | tumbleEsiği(SABİT)={minClusterSize} | easyBias={_easyBias01:0.00} | hardBias={_hardBias01:0.00} | scatterChanceNormal={scatterChanceNormal:0.000}");
    }
    public void OnZorlukSliderChanged(float value)
    {
        _adminManuelZorlukKilidi = true;
        // AdminPanel slider'ı ilk açılış anında çalışıp dinleyiciler henüz hazır değilse
        // zorluk yine de uygulansın diye doğrudan senaryo tarafına da yansıtırız.
        if (_adminAyarUIServisi != null)
            _adminAyarUIServisi.ApplyZorluk(value);
        else
            _senaryoServisi?.SetZorluk(value);
        OncedenHesaplananSpinOnbelleginiTemizle();
    }
    public void OnScatterSliderChanged(float value)
    {
        _adminManuelScatterKilidi = true;
        // FAZ35.31.1: ApplyScatter çağrısı silindi (scatter UI Faz 35.31'de kaldırıldı)
        OncedenHesaplananSpinOnbelleginiTemizle();
    }
    public void OnCarpanOlasilikSliderChanged(float value)
    {
        _adminAyarUIServisi?.ApplyCarpanOlasilik(value);
        OncedenHesaplananSpinOnbelleginiTemizle();
    }
    public void OnCarpanMaxAdetSliderChanged(float value)
    {
        _adminAyarUIServisi?.ApplyCarpanMaxAdet(value);
        OncedenHesaplananSpinOnbelleginiTemizle();
    }
    public void SetCarpanOlasilikYuzde(float yuzde)
    {
        yuzde = Mathf.Clamp(yuzde, 0f, 100f);
        carpanUretimOlasiligi = yuzde / 100f;

        if (carpanOlasilikValueText != null)
            carpanOlasilikValueText.text = $"{Mathf.RoundToInt(yuzde)}%";

        Debug.Log($"[ADMIN] Çarpan olasılığı set edildi: %{yuzde} (0-1={carpanUretimOlasiligi})");
    }

    // FAZ35.116: SetCarpanMaxAdet ÖLÜ KOD silindi. Hiçbir caller yoktu (Inspector handler OyunYoneticisi.cs:915-918
    // doğrudan lambda kullanıyor, panel AdminSetMaxCarpanTekSpin'i çağırıyor). Tek kaynak prensibi: maxCarpanAdedi
    // tavanı sadece AdminSetMaxCarpanTekSpin + Inspector handler üzerinden set ediliyor, ikisi de MAX_CARPAN_TAVAN
    // (Fields.cs:514+) sabitine referans veriyor.
    public void AdminForceOncedenHesaplananSpinTemizle()
    {
        OncedenHesaplananSpinOnbelleginiTemizle();
    }
    public bool IsSenaryo1Aktif() => IsAdminSenaryo1Aktif();

    public void AdminZorlaCarpanSec(int deger, bool popupGoster = true, string ozelMesaj = null)
    {
        if (deger > 0 && IsAdminSenaryo1Aktif())
        {
            Debug.LogWarning("[ADMIN] Senaryo 1 aktifken zorla çarpan engellendi (AdminZorlaCarpanSec).");
            return;
        }
        // FAZ36.6 İŞ3: Üst tavan 500 (C# backstop; JS doğrulama 36.6.1'de). Preset ×100/250/500 ≤500 → etkilenmez.
        if (deger > 500)
            Debug.LogWarning($"[ADMIN] Zorla çarpan {deger} > tavan 500 → 500'e çekildi.");
        zorlaSiradakiCarpan = Mathf.Clamp(deger, 0, 500);
        if (carpanAyarlari != null)
            carpanAyarlari.ZorlaSiradakiCarpan = zorlaSiradakiCarpan;
        // UI'daki CarpanAktifToggle'ı ZORLA değiştirmiyoruz.
        // Force sadece bir sonraki spin simülasyonunu etkiler; toggle görseli kullanıcının seçimi olarak kalmalı.
        if (popupGoster)
        {
            string mesaj = string.IsNullOrWhiteSpace(ozelMesaj)
                ? (zorlaSiradakiCarpan > 0 ? $"FORCE x{zorlaSiradakiCarpan} ETKİN" : "FORCE SIFIRLANDI")
                : ozelMesaj;
            // GRUP B/MADDE4: sol panel toast kaldırıldı; bilgi üst banner'a alt-tip olarak taşındı.
            // Alt-tip YALNIZCA force x{N} mesajında (zorlaSiradakiCarpan>0); sıfırlandı/özel mesajda gösterilmez.
            string altTip = (string.IsNullOrWhiteSpace(ozelMesaj) && zorlaSiradakiCarpan > 0)
                ? "Bir sonraki turda görünecek"
                : null;
            AdminForceMesajKutusuGoster(mesaj, 3f, altTip);
        }
        Debug.Log($"[ADMIN] Zorla çarpan seçildi: x{zorlaSiradakiCarpan}");
        // Force değişince bir önceki spinde arka planda hesaplanmış (Force'sız) sonuç geçersizdir.
        OncedenHesaplananSpinOnbelleginiTemizle();
    }

    void AdminForceMesajKutusuGoster(string mesaj, float sure, string altTip = null)
    {
        const string popupAd = "AdminForceKisaMesajPopup";
        var mevcut = GameObject.Find(popupAd);
        if (mevcut != null)
            Destroy(mevcut);

        var canvasGo = new GameObject(popupAd);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 3600;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // GRUP B/MADDE4: alt-tip varsa banner iki satır (ana mesaj üst, tip alt) → yükseklik 80→110.
        bool tipVar = !string.IsNullOrWhiteSpace(altTip);

        var panel = new GameObject("Panel");
        panel.transform.SetParent(canvasGo.transform, false);
        var panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 1f);
        panelRt.anchorMax = new Vector2(0.5f, 1f);
        panelRt.pivot = new Vector2(0.5f, 1f);
        panelRt.anchoredPosition = new Vector2(0f, -28f);
        panelRt.sizeDelta = new Vector2(520f, tipVar ? 110f : 80f);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.07f, 0.12f, 0.18f, 0.94f);
        panelImg.raycastTarget = false;

        var yaziGo = new GameObject("Mesaj");
        yaziGo.transform.SetParent(panel.transform, false);
        var yaziRt = yaziGo.AddComponent<RectTransform>();
        // Tip varsa ana mesaj üst ~%62'ye yerleşir; yoksa tüm paneli kaplar (eski davranış).
        yaziRt.anchorMin = tipVar ? new Vector2(0f, 0.38f) : Vector2.zero;
        yaziRt.anchorMax = Vector2.one;
        yaziRt.offsetMin = new Vector2(12f, tipVar ? 0f : 8f);
        yaziRt.offsetMax = new Vector2(-12f, -8f);
        var yazi = yaziGo.AddComponent<TextMeshProUGUI>();
        yazi.text = mesaj;
        yazi.fontSize = 32;
        yazi.alignment = TMPro.TextAlignmentOptions.Center;
        yazi.color = new Color(0.58f, 0.96f, 0.62f, 1f);
        yazi.raycastTarget = false;

        // GRUP B/MADDE4: alt-tip (küçük font, soluk) — yalnız force x{N} mesajında.
        if (tipVar)
        {
            var tipGo = new GameObject("AltTip");
            tipGo.transform.SetParent(panel.transform, false);
            var tipRt = tipGo.AddComponent<RectTransform>();
            tipRt.anchorMin = Vector2.zero;
            tipRt.anchorMax = new Vector2(1f, 0.38f);
            tipRt.offsetMin = new Vector2(12f, 8f);
            tipRt.offsetMax = new Vector2(-12f, 0f);
            var tipYazi = tipGo.AddComponent<TextMeshProUGUI>();
            tipYazi.text = altTip;
            tipYazi.fontSize = 18;
            tipYazi.alignment = TMPro.TextAlignmentOptions.Center;
            tipYazi.color = new Color(0.78f, 0.85f, 0.80f, 0.9f);
            tipYazi.raycastTarget = false;
        }

        if (sure > 0f)
            Destroy(canvasGo, sure);
    }
    private static bool AdminOyunSahnesiMi()
    {
        // FAZ35.77: Yeni 03_AdminOyunScene (Tutorial'sız) + eski 04_AdminOyunScene (Tutorial dahil) ikisi de admin sahnesi.
        string sn = SceneManager.GetActiveScene().name;
        return sn == "03_AdminOyunScene" || sn == "04_AdminOyunScene";
    }
    private void UstUsteDonguyuSpinSonucuIleIlerle(bool kazancGerceklesti)
    {
        _ = kazancGerceklesti;
        if (!IsAdminSenaryo4Aktif() && !IsAdminSenaryo5Aktif() && !IsAdminSenaryo2Aktif() && !IsAdminSenaryo3Aktif()) return;
        if (IsAdminSenaryo2Aktif()) { _senaryo2DonguIndex = (_senaryo2DonguIndex + 1) % 5; return; }
        if (IsAdminSenaryo3Aktif()) { _senaryo3DonguIndex = (_senaryo3DonguIndex + 1) % 5; return; }
        if (IsAdminSenaryo4Aktif()) { _senaryo4DonguIndex = (_senaryo4DonguIndex + 1) % 3; return; }
        if (IsAdminSenaryo5Aktif())
        {
            int oncekiIdx = _senaryo5DonguIndex;
            _senaryo5DonguIndex = (_senaryo5DonguIndex + 1) % 3;
            bool popupKuruldu = oncekiIdx == 2;
            if (popupKuruldu)
                _senaryo5BombSonrasiPopupBekliyor = true;
            Debug.Log($"[S5][DÖNGÜ] oncekiIdx={oncekiIdx} → yeniIdx={_senaryo5DonguIndex} popupKuruldu={popupKuruldu}");
        }
    }

    private bool OdemeModelineUygunMu(int nihaiOdeme, int bahis, int deneme, int maxReroll)
    {
        _ = deneme; _ = maxReroll;
        // Faz 35.27 YOL Z sonrası: min/max/dağılım/üst üste döngü alanları kaldırıldı. Beklenen yön sadece eğilim
        // veya senaryo lokal döngüsünden gelir; bant aralığı yok → sadece eğilim (kazanç/kayıp yönü) zorlanır.
        bool beklenenKazanc;
        if (IsAdminSenaryo3Aktif())
            beklenenKazanc = Senaryo3BeklenenKazancMi();
        else if (IsAdminSenaryo2Aktif())
            beklenenKazanc = Senaryo2BeklenenKazancMi();
        else if (IsAdminSenaryo4Aktif())
            beklenenKazanc = Senaryo4DonguSpinTipi() == SenaryoBombSpinTipi.Kazanc;
        else if (IsAdminSenaryo5Aktif())
            beklenenKazanc = Senaryo5DonguSpinTipi() == SenaryoBombSpinTipi.Kazanc;
        // FAZ35.101 İŞ1: Normal mod'da min/max çarpan kat slider'ları eğilim RNG akışını bozmamalı.
        // Senaryolu modlar (_modSpinBekleniyorKazanc field'ı) için bu branch korunur; Normal mod else'e düşer.
        else if (odemeMinKat > 0f && odemeMaksKat > 0f && PanelKopru.aktifSenaryo != "normal")
            // FAZ35.85 K5: Mod aktifken spin başında 1 kez set edilen _modSpinBekleniyorKazanc kullan (reroll tutarlılık).
            // Spin.cs SimuleEtVeKaydetImpl başında set edilir; ModKazancKonstrukte branch'ı + bu kontrol aynı field'ı paylaşır.
            beklenenKazanc = _modSpinBekleniyorKazanc;
        else
            beklenenKazanc = UnityEngine.Random.value <= Mathf.Clamp01(_odemeEgilimiYuzde / 100f);

        // FAZ35.85 K1: Yontma+Koruma "yontma payı" kategorisi — odemeMinKat<1f iken kazanç anlamı "nihaiOdeme > 0".
        // Hook (1.5-2.5), Tutma (1.2-1.8): odemeMinKat >= 1f → standart kazanç (>bahis).
        // Yontma (0.3-0.7), Koruma (0.1-0.3): odemeMinKat < 1f → ödeme >0 ise "kazanç" sayılır (bahisten az olsa bile).
        // Normal mod (odemeMinKat==0): yontmaPayi=false → standart kazanç mantığı (mevcut davranış).
        bool yontmaPayiKategorisi = (odemeMinKat > 0f && odemeMinKat < 1f);
        bool kazanc = yontmaPayiKategorisi ? (nihaiOdeme > 0) : (nihaiOdeme > bahis);
        if (beklenenKazanc && !kazanc) return false;
        if (!beklenenKazanc && kazanc) return false;

        // FAZ35.81 Madde 1: Serbest oyun Min/Maks Çarpan bant kontrolü (reroll mekaniği — pre-üretim, post-clamp YOK).
        // ALT-PARÇA 7 (Çakışma 8): Çarpan Zorla aktifken (zorlaSiradakiCarpan > 0) bant atlanır — kullanıcı anlık karar verdi, motor itaat.
        // Senaryo 1-5 admin senaryoları aktifken bant atlanır — kendi hedef bantlarını yönetiyorlar.
        // Sadece kazanç spinlerinde uygulanır (kayıp spinler bant kontrolüne tabi değil — kayıp = 0 ödeme).
        // FAZ35.101 İŞ1: Normal mod'da bant filtre kontrolü çalışmamalı (Senaryolu modlar için korunur).
        // aktifSenaryo == "normal" iken kullanıcının slider değerleri (odemeMinKat/odemeMaksKat) sadece
        // UI'da görünür — motor reroll bant'ı yok, Normal mod RNG akışı doğal kazançları ödüllendirir.
        if (beklenenKazanc && kazanc
            && odemeMinKat > 0f && odemeMaksKat > 0f
            && zorlaSiradakiCarpan <= 0
            && !IsAdminSenaryo1Aktif() && !IsAdminSenaryo2Aktif() && !IsAdminSenaryo3Aktif()
            && !IsAdminSenaryo4Aktif() && !IsAdminSenaryo5Aktif()
            && PanelKopru.aktifSenaryo != "normal")
        {
            int minHedef = Mathf.RoundToInt(bahis * odemeMinKat);
            int maksHedef = Mathf.RoundToInt(bahis * odemeMaksKat);
            if (maksHedef < minHedef) { int t = minHedef; minHedef = maksHedef; maksHedef = t; }
            if (nihaiOdeme < minHedef || nihaiOdeme > maksHedef)
                return false; // bant dışı → reroll → motor BAŞTAN yeniden üretir
        }

        // FAZ35.82: Tutma kayıp spin zorlama — sayaç fazı 0/1 iken RNG kazanç çıkardıysa reroll
        // (Spin.cs Tutma branch'ı _tutmaBuSpinKayipBekleniyor=true set eder; spin sonunda temizler.)
        if (_tutmaBuSpinKayipBekleniyor && nihaiOdeme > 0)
            return false;

        return true;
    }
    private void AdminZorlaButonReferanslariniBulBirKez()
    {
        if (_adminZorlaButonReferanslariBulundu) return;
        _adminForceX5Btn = GameObject.Find("ForceX5")?.GetComponent<Button>();
        _adminForceX10Btn = GameObject.Find("ForceX10")?.GetComponent<Button>();
        _adminForceX50Btn = GameObject.Find("ForceX50")?.GetComponent<Button>();
        _adminForceX100Btn = GameObject.Find("ForceX100")?.GetComponent<Button>();
        _adminCarpanSifirlaBtn = GameObject.Find("CarpanSifirla")?.GetComponent<Button>();
        // Yalnızca en az bir buton bulunduysa hazır say; panel kapalıyken null gelirse sonraki frame'de tekrar dene.
        if (_adminForceX5Btn != null || _adminForceX10Btn != null || _adminForceX50Btn != null || _adminForceX100Btn != null)
            _adminZorlaButonReferanslariBulundu = true;
    }

    private void ZorlaButonlarininEtkilesiminiAyarla(bool etkin)
    {
        AdminZorlaButonReferanslariniBulBirKez();
        if (_adminForceX5Btn != null) _adminForceX5Btn.interactable = etkin;
        if (_adminForceX10Btn != null) _adminForceX10Btn.interactable = etkin;
        if (_adminForceX50Btn != null) _adminForceX50Btn.interactable = etkin;
        if (_adminForceX100Btn != null) _adminForceX100Btn.interactable = etkin;
        if (_adminCarpanSifirlaBtn != null) _adminCarpanSifirlaBtn.interactable = etkin;
    }
    private void SenaryoPedagojikOdemeVeZorlaKilidiGuncelle()
    {
        var sy = SenaryoYoneticisi.I;
        bool asama1Isindirma = sy != null && sy.mevcutAsama == SenaryoYoneticisi.SenaryoAsama.Asama1_IsindirmaUmut;
        ZorlaButonlarininEtkilesiminiAyarla(!asama1Isindirma && !IsAdminSenaryo1Veya2Veya3Aktif() && !IsAdminSenaryo4Aktif() && !IsAdminSenaryo5Aktif());
    }
    private System.Collections.IEnumerator Senaryo5PopupCoroutine()
    {
        // ── Canvas ──
        var canvasGo = new GameObject("Sen5PopupCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // ── Yarı saydam arka plan (arkası hafif görünsün) ──
        var bgGo = new GameObject("Sen5BG");
        bgGo.transform.SetParent(canvasGo.transform, false);
        var bgImg = bgGo.AddComponent<UnityEngine.UI.Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.52f);
        SetAnchors(bgGo, Vector2.zero, Vector2.one);

        // ── Ortalanmış panel (ekranın %68 genişliği, %42 yüksekliği) ──
        var panelGo = new GameObject("Sen5Panel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        var panelImg = panelGo.AddComponent<UnityEngine.UI.Image>();
        panelImg.color = new Color(0.07f, 0.08f, 0.18f, 0.97f);
        SetAnchors(panelGo, new Vector2(0.16f, 0.29f), new Vector2(0.84f, 0.71f));

        // ── Üst gold aksent çizgisi ──
        var accentGo = new GameObject("Sen5Accent");
        accentGo.transform.SetParent(panelGo.transform, false);
        var accentImg = accentGo.AddComponent<UnityEngine.UI.Image>();
        accentImg.color = new Color(0.88f, 0.72f, 0.08f, 1f);
        SetAnchors(accentGo, new Vector2(0f, 0.93f), new Vector2(1f, 1f));

        // ── Başlık ──
        var titleGo = new GameObject("Sen5Title");
        titleGo.transform.SetParent(panelGo.transform, false);
        var titleTmp = titleGo.AddComponent<TMPro.TextMeshProUGUI>();
        titleTmp.text = "ÖZEL TEKLİF";
        titleTmp.alignment = TMPro.TextAlignmentOptions.Center;
        titleTmp.fontSize = 22;
        titleTmp.fontStyle = TMPro.FontStyles.Bold;
        titleTmp.color = new Color(0.88f, 0.72f, 0.08f, 1f);
        SetAnchors(titleGo, new Vector2(0.05f, 0.73f), new Vector2(0.95f, 0.92f));

        // ── Mesaj metni ──
        var txtGo = new GameObject("Sen5Txt");
        txtGo.transform.SetParent(panelGo.transform, false);
        var tmp = txtGo.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = "Çok şanslı görünüyorsun!\nTüm bakiyen ile bonus oyun satın al,\n1000 katına kadar kazanma şansını yakala!";
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.fontSize = 20;
        tmp.color = new Color(0.92f, 0.92f, 0.92f, 1f);
        SetAnchors(txtGo, new Vector2(0.06f, 0.34f), new Vector2(0.94f, 0.72f));

        // ── Satın Al butonu (sağ) ──
        var btnGo = new GameObject("Sen5SatinAlBtn");
        btnGo.transform.SetParent(panelGo.transform, false);
        var btnImg = btnGo.AddComponent<UnityEngine.UI.Image>();
        btnImg.color = new Color(0.10f, 0.50f, 0.14f, 1f);
        var btn = btnGo.AddComponent<UnityEngine.UI.Button>();
        var btnColors = btn.colors;
        btnColors.highlightedColor = new Color(0.14f, 0.65f, 0.18f, 1f);
        btnColors.pressedColor    = new Color(0.07f, 0.36f, 0.10f, 1f);
        btn.colors = btnColors;
        SetAnchors(btnGo, new Vector2(0.52f, 0.05f), new Vector2(0.94f, 0.29f));
        AddBtnLabel(btnGo, "Satın Al", 19);

        // ── İptal butonu (sol) ──
        var iptalGo = new GameObject("Sen5IptalBtn");
        iptalGo.transform.SetParent(panelGo.transform, false);
        var iptalImg = iptalGo.AddComponent<UnityEngine.UI.Image>();
        iptalImg.color = new Color(0.22f, 0.22f, 0.30f, 1f);
        var iptalBtn = iptalGo.AddComponent<UnityEngine.UI.Button>();
        var iptalColors = iptalBtn.colors;
        iptalColors.highlightedColor = new Color(0.32f, 0.32f, 0.42f, 1f);
        iptalColors.pressedColor     = new Color(0.14f, 0.14f, 0.20f, 1f);
        iptalBtn.colors = iptalColors;
        SetAnchors(iptalGo, new Vector2(0.06f, 0.05f), new Vector2(0.48f, 0.29f));
        AddBtnLabel(iptalGo, "Hayır, teşekkürler", 16);

        _senaryo5PopupGo = canvasGo;
        bool satinAldi = false;
        bool iptalEtti = false;
        btn.onClick.AddListener(() => satinAldi = true);
        iptalBtn.onClick.AddListener(() => iptalEtti = true);

        yield return new WaitUntil(() => satinAldi || iptalEtti);

        UnityEngine.Object.Destroy(canvasGo);
        _senaryo5PopupGo = null;

        if (!satinAldi) yield break;

        // Tüm bakiye bonus satın alımına gitti; bonus cuzi ödeme moduyla başlar
        if (_ekonomiServisi != null)
            _ekonomiServisi.SetBakiye(0);
        _senaryo5BonusCuziLimitAktif = true;
        if (_donusServisi != null)
            _donusServisi.BaslatBonus();
    }
    private void SenaryoPresetUIHazirlaGerekirse()
    {
        if (_senaryoPresetUIHazir) return;

        if (_senaryoPresetDropdown == null)
            _senaryoPresetDropdown = GameObject.Find("SenaryoPresetDropdown")?.GetComponent<TMP_Dropdown>();

        if (_senaryoPresetDropdown != null)
        {
            _senaryoPresetDropdown.onValueChanged.RemoveListener(OnSenaryoPresetDropdownDegisti_Runtime);
            _senaryoPresetDropdown.ClearOptions();
            var ops = new List<string>(_adminSenaryoPresetleri.Length + 1);
            ops.Add("0. NORMAL OYUN");
            for (int i = 0; i < _adminSenaryoPresetleri.Length; i++)
                ops.Add(_adminSenaryoPresetleri[i].Ad);
            _senaryoPresetDropdown.AddOptions(ops);
            SenaryoDropdownYazilariniBuyut();
            _senaryoPresetDropdown.value = 0;
            _senaryoPresetDropdown.RefreshShownValue();
            _senaryoPresetDropdown.onValueChanged.AddListener(OnSenaryoPresetDropdownDegisti_Runtime);
            _senaryoPresetUIHazir = true;
        }

        SenaryoModuDurumLabeliniBulVeYaz();

        if (_senaryoPresetUIHazir)
            AdminNormalOyunUygula();
    }

    private void OnSenaryoPresetDropdownDegisti_Runtime(int index)
    {
        if (index == 0)
        {
            AdminNormalOyunUygula();
            return;
        }
        _senaryoPresetAktif = true;
        AdminSenaryoPresetUygula(index - 1);
    }

    /// <summary>SpinTestAraci için: tüm kritik servislerin (Awake/Start sonrası) başlatıldığını döner.
    /// Reflection'a gerek kalmadan WaitUntil ile bekleyebilmek için public.</summary>
    public bool TumServislerHazirMi()
    {
        return _ekonomiServisi != null
            && _odemeServisi != null
            && _carpanServisi != null
            && _izgaraServisi != null
            && _tumbleServisi != null
            && _donusAkisServisi != null;
    }

    /// <summary>SpinTestAraci için: bakiye/bahis manipülasyonu için public servis erişimi.</summary>
    public EkonomiServisi TestEkonomiServisi => _ekonomiServisi;

    /// <summary>SpinTestAraci için: scatter index'i + state field okuma için public erişim.</summary>
    public int TestScatterIndex => _scatterIndexCache;
    public bool TestKacisFrenlemeAktif => _kacisFrenlemeBuSpinAktif;
    public int TestArdisikKayipSayac => _ardisikKayipSayac;

    /// <summary>SpinTestAraci için: senaryo dropdown index'iyle senaryo aktive eder. 0 = Normal Oyun, 1-5 = Senaryo 1-5.</summary>
    public void TestSenaryoSec(int dropdownIndex0Bazli)
    {
        if (dropdownIndex0Bazli <= 0)
        {
            AdminNormalOyunUygula();
            return;
        }
        _senaryoPresetAktif = true;
        AdminSenaryoPresetUygula(dropdownIndex0Bazli - 1);
    }

    /// <summary>SpinTestAraci için senkron spin simülasyonu. Animasyon ÇAĞRILMAZ — sadece veri akışı.
    /// Dönen kayıttan ham kazanç, çarpan, tumble adımları, force carpan kullanımı okunur.</summary>
    public SpinSimulasyonKaydi TestSpinSimuleEt(bool bonusSpin = false)
    {
        int odenebilir = _odemeServisi != null ? _odemeServisi.GetSpinOdenebilirLimit() : int.MaxValue;
        return SimuleEtVeKaydetImpl(odenebilir, bonusSpin);
    }

    /// <summary>SpinTestAraci için: bonus oyununun bir bonus turunu simüle eder; toplam ödemeyi döner.
    /// Gerçek bonus akışını kısa devre eder; her tur için SimuleEtVeKaydetImpl(bonusSpin=true) çağırır.</summary>
    public int TestBonusOyunSimuleEt(int bonusHak = 10)
    {
        // FAZ35.140 K4: Bonus toplam cap — bahis × 15. Mevcut int.MaxValue sınırı bonus'ta 28K tek-spin
        // taşmalarına izin veriyordu (Hook 100 spin analizi). Gerçek bonus akışı (Bonus.cs:94) bahis×15
        // sınırı uygular; test API'si bunu yansıtmıyordu — şimdi senkron. bahis 1500 → bonus max 22500.
        int bahis = _ekonomiServisi != null ? _ekonomiServisi.Bahis : 0;
        int bonusCap = Mathf.Max(1, bahis) * 15;
        int toplam = 0;
        for (int i = 0; i < bonusHak; i++)
        {
            var kayit = SimuleEtVeKaydetImpl(int.MaxValue, true);
            if (kayit == null) continue;
            int ham = kayit.ToplamHamKazanc;
            int carpan = Mathf.Max(1, kayit.NihaiCarpanToplam);
            toplam += ham * carpan;
            if (toplam >= bonusCap) { toplam = bonusCap; break; } // cap'e ulaştı → kalan tur boşa harcanmasın
        }
        return Mathf.Min(toplam, bonusCap);
    }

    private void AdminSenaryoPresetUygula(int index)
    {
        if (_adminSenaryoPresetleri == null || _adminSenaryoPresetleri.Length == 0) return;
        index = Mathf.Clamp(index, 0, _adminSenaryoPresetleri.Length - 1);
        _aktifAdminSenaryoIndex = index;
        var p = _adminSenaryoPresetleri[index];

        OnScatterSliderChanged(p.ScatterYuzde);

        if (carpanOlasilikSlider != null)
            carpanOlasilikSlider.SetValueWithoutNotify(Mathf.Clamp(p.CarpanYuzde, carpanOlasilikSlider.minValue, carpanOlasilikSlider.maxValue));
        OnCarpanOlasilikSliderChanged(p.CarpanYuzde);

        if (carpanMaxAdetSlider != null)
            carpanMaxAdetSlider.SetValueWithoutNotify(Mathf.Clamp(p.MaxCarpanAdedi, carpanMaxAdetSlider.minValue, carpanMaxAdetSlider.maxValue));
        OnCarpanMaxAdetSliderChanged(p.MaxCarpanAdedi);

        AdminBahisAyarla(p.Bahis);
        AdminMaxScatterPerSpinAyarla(p.MaxScatterPerSpin);

        bool forcePopupGoster = false;
        string forceMesaji = null;
        if (index == 3)
        {
            forcePopupGoster = true;
            forceMesaji = "ZORLA 100X AKTİF";
        }
        else if (index == 4)
        {
            forcePopupGoster = true;
            forceMesaji = "ZORLA 500X AKTİF";
        }
        AdminZorlaCarpanSec(p.ZorlaCarpan, forcePopupGoster, forceMesaji);
        // Faz 35.27 YOL Z: AdminSenaryoPresetOdemeModeliniUygula kaldırıldı; senaryo motoru kendi lokal döngülerini kullanır.
        AdminPaytableOzetiLogla(_ekonomiServisi != null ? _ekonomiServisi.Bahis : p.Bahis);
        OncedenHesaplananSpinOnbelleginiTemizle();
        SpinPolitikasiniYenile();
    }
    private void AdminPaytableOzetiLogla(int bahis)
    {
        if (tumbleAyarlari == null || bahis <= 0) return;
        tumbleAyarlari.EnsurePayTablesInitialized(
            (sembolSpriteListesi != null && sembolSpriteListesi.Count > 0) ? sembolSpriteListesi.Count : 9);
        int sc = Mathf.Clamp(tumbleAyarlari.ScatterIndex, 0, int.MaxValue);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"[ADMIN][PAYTABLE_OZET] Bahis={bahis} TL | TUMBLE_ESIK=8 (tek sembol kümesi, çarpan yok) | ScatterIndex={sc}");
        int n = tumbleAyarlari.PayTable_8_9 != null ? tumbleAyarlari.PayTable_8_9.Length : 0;
        for (int i = 0; i < n; i++)
        {
            float a = tumbleAyarlari.PayTable_8_9[i];
            float b = tumbleAyarlari.PayTable_10_11 != null && i < tumbleAyarlari.PayTable_10_11.Length ? tumbleAyarlari.PayTable_10_11[i] : 0f;
            float c = tumbleAyarlari.PayTable_12Plus != null && i < tumbleAyarlari.PayTable_12Plus.Length ? tumbleAyarlari.PayTable_12Plus[i] : 0f;
            int tl8 = Mathf.RoundToInt(a * bahis);
            int tl10 = Mathf.RoundToInt(b * bahis);
            int tl12 = Mathf.RoundToInt(c * bahis);
            string etik = (i == sc) ? " (SCATTER ödeme 0)" : string.Empty;
            sb.AppendLine($"  İndex {i}{etik}: 8–9 küme → {tl8} TL | 10–11 → {tl10} TL | 12+ → {tl12} TL");
        }
        sb.Append("  Not: Aynı turda birden çok sembol kümesi patlarsa toplam = satırlar toplamı. Düşük zorlukta 6–7 hücre yarım tablo ile ödenir (TumbleAyarlari.CalculateWinWithOwnPayTable).");
        Debug.Log(sb.ToString());
    }

    private void SenaryoModuDurumLabeliniBulVeYaz() { }
    public void AdminNormalOyunUygula()
    {
        _senaryoPresetAktif = false;
        _aktifAdminSenaryoIndex = -1;

        // FAZ35.95 İŞ1: Normal mod RTP %17 → ~%55-65 hedef (pedagojik denge).
        // Eğilim 65 → 75 → OdemeModelineUygunMu RNG kararı daha sık "kazanç bekliyor" → reroll cluster arama agresif.
        _odemeEgilimiYuzde = 75;

        AdminZorlaCarpanSec(0, false, null);
        SpinPolitikasiniYenile();
        OncedenHesaplananSpinOnbelleginiTemizle();
        SenaryoModuDurumLabeliniBulVeYaz();

        Debug.Log("[ADMIN] Normal Oyun modu aktif: Senaryo 1-5 kapalı | Eğilim=%65");
    }

    /// <summary>PanelKopru: kazanma oranını (0-100) doğrudan set eder.</summary>
    public void AdminSetOdemeEgilimi(int yuzde)
    {
        Debug.Log($"[Admin] AdminSetOdemeEgilimi CAGRILDI: yuzde={yuzde} | onceki={_odemeEgilimiYuzde}");
        _odemeEgilimiYuzde = Mathf.Clamp(yuzde, 0, 100);
        SpinPolitikasiniYenile();
        OncedenHesaplananSpinOnbelleginiTemizle();
        Debug.Log($"[ADMIN][PANEL] OdemeEgilimi = %{_odemeEgilimiYuzde}");
    }

    /// <summary>PanelKopru: zorla çarpan sonrası tumble zinciri devam etsin mi?</summary>
    public void AdminSetCarpanTumbleAktif(bool aktif)
    {
        _carpanTumbleAktif = aktif;
        Debug.Log($"[ADMIN] CarpanTumbleAktif = {aktif}");
    }

    /// <summary>LEGACY (Faz 35.27 YOL Z): _maxOdemeTL alanı kaldırıldı. Anlatıcı/SenaryoOtomatikAkis/Tutorial
    /// hâlâ bu setter'ı çağırıyor; motor okumuyor → no-op (önbellek invalidasyonu hariç).</summary>
    public void AdminSetMaxOdeme(int tl)
    {
        _ = tl;
        OncedenHesaplananSpinOnbelleginiTemizle();
    }

    public void AdminSetArdisikKayipLimiti(int limit)
    {
        _ardisikKayipLimiti = Mathf.Max(1, limit);
        _ardisikKayipSayac = 0;
        Debug.Log($"[ADMIN][PANEL] ArdisikKayipLimiti = {_ardisikKayipLimiti}");
    }

    // FAZ35.76: _yeniOyuncuKoroutin + _yeniOyuncuOncekiEgilim field'ları SİLİNDİ
    // (AdminSetYeniOyuncuModu + YeniOyuncuModuSureKontrol coroutine ile birlikte kaldırıldı).

    [HideInInspector] public int bonusOtomatikSpinPeriyodu = 0; // 0 = devre dışı
    public void AdminSetBonusOtomatikSpinPeriyodu(int oran)
    {
        bonusOtomatikSpinPeriyodu = Mathf.Max(0, oran);
        // Periyot değişince sayaç sıfırlansın (anında yeni rejime geç)
        _bonusOtomatikSpinSayaci = 0;
        Debug.Log("[ADMIN][PANEL] Bonus otomatik spin periyodu = " + bonusOtomatikSpinPeriyodu + " (0 = kapalı)");
    }

    // FAZ35.81 Madde 2: PanelKopru "bonusModu" handler manuel/otomatik geçişlerinde periyodu cache+restore eder.
    public int GetBonusOtomatikSpinPeriyodu() => bonusOtomatikSpinPeriyodu;

    // DonusAkisServisi spin sonu güncellemesinde kullanılan sayaç ve flag.
    [HideInInspector] public int _bonusOtomatikSpinSayaci = 0;
    [HideInInspector] public bool _bonusOtomatikTetikSonrakiSpin = false;

    [HideInInspector] public int carpanSahteOraniYuzde = 0;
    public void AdminSetCarpanSahteOrani(int yuzde)
    {
        carpanSahteOraniYuzde = Mathf.Clamp(yuzde, 0, 100);
        Debug.Log($"[ADMIN][PANEL] Çarpan sahte gösterimi: {carpanSahteOraniYuzde}%");
    }

    // FAZ35.103 İŞ2: Bonus oyuna girme olasılığı delegate.
    // 0 → tamamen doğal scatter akışı (mevcut scatterChanceNormal=0.005f korunur, ~16+ spin gerek).
    // 0-1 arası → her spin başında RNG check (Spin.cs SimuleEtVeKaydetImpl), eşiği geçerse 4 scatter zorla yerleştir → bonus tetik.
    // 1 → her spin garanti 4 scatter → bonus garanti giriş.
    [HideInInspector] public float _bonusGirmeOlasilik = 0f;
    public void AdminSetBonusGirmeOlasilik(float yuzde01)
    {
        _bonusGirmeOlasilik = Mathf.Clamp01(yuzde01);
        Debug.Log($"[ADMIN][PANEL] Bonus oyuna girme olasılığı = {(_bonusGirmeOlasilik * 100f):F0}% (0=doğal akış, 1=her spin garanti)");
    }
    public float GetBonusGirmeOlasilik() => _bonusGirmeOlasilik;

    // FAZ36.1 FIX3: carpanOlasilikYuzde ölü alanı KALDIRILDI — oyun mantığı hiçbir yerde okumuyordu (yalnız bu log
    // self-referans). Gerçek olasılık carpanUretimOlasiligi (float); PanelKopru "carpanOlasilik" case'i + VarsayilanaDon
    // set eder. Bu metod public API olarak korunur (PanelKopru/SpinTestRunner/VarsayilanaDon çağırır) — sadece loglar.
    public void AdminSetCarpanOlasilik(int yuzde)
    {
        int clamped = Mathf.Clamp(yuzde, 0, 100);
        Debug.Log($"[ADMIN][PANEL] Çarpan düşme olasılığı: {clamped}%");
    }

    // FAZ35.81 Madde 1: Serbest oyun Min/Maks Çarpan setter'ları.
    // Kullanıcı 0 set ederse devre dışı (mevcut RNG akışı). >0 set ederse OdemeModelineUygunMu reroll bant'a zorlar.
    public void AdminSetMinCarpanDegeri(float min)
    {
        odemeMinKat = Mathf.Max(0f, min);
        SpinPolitikasiniYenile();
        OncedenHesaplananSpinOnbelleginiTemizle();
        Debug.Log($"[ADMIN][PANEL] Min çarpan kat: {odemeMinKat}x");
    }
    public void AdminSetMaksCarpanDegeri(float maks)
    {
        odemeMaksKat = Mathf.Max(0f, maks);
        SpinPolitikasiniYenile();
        OncedenHesaplananSpinOnbelleginiTemizle();
        Debug.Log($"[ADMIN][PANEL] Maks çarpan kat: {odemeMaksKat}x");
    }

    // FAZ35.82: Tutma modu state yönetimi (mod değişiminde sayaç + flag reset).
    // PanelKopru.SenaryoUygula tarafından mod seçimi/değişiminde çağrılır. Tutma case'inde true,
    // diğer 4 mod case'inde false (kullanıcı modlar arası geçince Tutma otomatik kapanır).
    public void AdminSetTutmaModAktif(bool aktif)
    {
        _tutmaModAktif = aktif;
        _tutmaModSpinSayac = 0;
        _tutmaBuSpinKayipBekleniyor = false;
        OncedenHesaplananSpinOnbelleginiTemizle();
        Debug.Log($"[ADMIN][TUTMA] Mod aktif: {aktif} (sayaç sıfırlandı)");
    }
    public bool IsTutmaModAktif() => _tutmaModAktif;

    [HideInInspector] public int maxCarpanTekSpinSayisi = 3;
    public void AdminSetMaxCarpanTekSpin(int max)
    {
        // FAZ35.116: Strict-reject. MAX_CARPAN_TAVAN (Fields.cs:514+, =5) üstü değer reddedilir, eski değer korunur.
        // Panel UX (panel.html tooltip "1-5" vaad ediyor) backend güvencesiyle hizalandı. Panel UX kontrolü sonraki işe
        // bırakıldı — backend'den 6+ gelse bile motor reddeder, kullanıcı eski değerde kalır.
        if (max > MAX_CARPAN_TAVAN)
        {
            Debug.LogWarning($"[FAZ35.116] AdminSetMaxCarpanTekSpin REDDEDİLDİ: istenen {max} > tavan {MAX_CARPAN_TAVAN}. Eski değer korundu: maxCarpanTekSpinSayisi={maxCarpanTekSpinSayisi}, maxCarpanAdedi={maxCarpanAdedi}.");
            return;
        }
        maxCarpanTekSpinSayisi = Mathf.Clamp(max, 1, MAX_CARPAN_TAVAN);
        // CarpanServisi gerçek field 'maxCarpanAdedi' okuyor; değişikliği oraya da yansıt.
        maxCarpanAdedi = maxCarpanTekSpinSayisi;
        Debug.Log($"[ADMIN][PANEL] Tek spinde max çarpan: {maxCarpanTekSpinSayisi} (maxCarpanAdedi={maxCarpanAdedi}, tavan={MAX_CARPAN_TAVAN})");
    }

    // ────────────────────────────────────────────────────────────
    // YAKIN KAÇIRMA (Near-Miss) — 10'da N formatında, 0 = kapalı
    // ────────────────────────────────────────────────────────────
    [HideInInspector] public int yakinKacirmaDegeri10da = 0;
    public void AdminSetYakinKacirma(int deger10da)
    {
        yakinKacirmaDegeri10da = Mathf.Clamp(deger10da, 0, 10);
        // FAZ35.120: Precompute cache invalidate — toggle değişimi anında etkili olsun.
        // Faz 35.106 carpanOlasilik + bonusGirmeOlasilik için bu pattern eklenmişti, yakinKacirma'da UNUTULMUŞ.
        // Cache stale olunca near-miss flag (Spin.cs:516) precompute zamanında yakinKacirma=0 ile hesaplanıyor →
        // flag FALSE → 35.119 enjeksiyon atlanıyor → [FAZ35.119] log hiç çıkmıyor → spin normal kazanç veriyor.
        // Bu satırla toggle açılır açılmaz cache temizlenir → sonraki spin fresh hesap → flag TRUE → enjeksiyon fire.
        AdminForceOncedenHesaplananSpinTemizle();
        Debug.Log($"[ADMIN][PANEL] Yakın Kaçırma = 10'da {yakinKacirmaDegeri10da} (cache temizlendi)");
    }

    // ────────────────────────────────────────────────────────────
    // MANUEL BONUS TETİKLEME (test ve panel butonu için)
    // ────────────────────────────────────────────────────────────
    public void AdminManuelBonusBaslat()
    {
        if (bonusAktif)
        {
            Debug.LogWarning("[ADMIN][PANEL] Bonus zaten aktif; manuel tetikleme atlandı.");
            return;
        }
        Debug.Log("[ADMIN][PANEL] Manuel bonus tetikleme başlatıldı.");
        BaslatBonus();
    }

    // FAZ35.76: AdminSetYeniOyuncuModu + YeniOyuncuModuSureKontrol coroutine SİLİNDİ. Panel.html'de
    // "Yeni oyuncu modu" toggle kaldırıldığı için bu metoda çağrı kalmadı. AdminSetOdemeEgilimi'ye
    // dolaylı çağrılar (85 set + restore) bu metot içindeydi; metot silindiği için ilgili
    // ödeme eğilimi geçişleri ortadan kalktı. 03 senaryolarının ödeme eğilimi yolu (PanelKopru.
    // SenaryoUygula → AdminSetOdemeEgilimi(sabit) ve AnlaticiSeritKopru/SenaryoOtomatikAkis kendi
    // parametreleriyle) etkilenmedi.

    public bool AdminBahisAyarla(int hedefBahis)
    {
        if (_ekonomiServisi == null) return false;
        int onceki = _ekonomiServisi.Bahis;
        _ekonomiServisi.SetBahis(hedefBahis);
        int yeni = _ekonomiServisi.Bahis;
        _uiServisi?.UI_Guncelle();
        SenaryoYoneticisi.I?.UI_Guncelle();

        // KRİTİK (2026-04-30): Bahis değiştiyse precompute önbelleğini geçersizleştir.
        // Aksi halde sonraki spin ESKİ bahisle hesaplanmış kayıttan oynar — kazançlar düşük çıkar.
        if (yeni != onceki)
        {
            OncedenHesaplananSpinOnbelleginiTemizle();
            Debug.Log($"[BAHIS] Ayarlandı: {onceki} -> {yeni} (hedef={hedefBahis}), precompute önbelleği temizlendi.");
            return true;
        }
        Debug.Log($"[BAHIS] Hedef={hedefBahis}, sonuc={yeni} (değişiklik yok — clamp olabilir, bahisMax={bahisMax}).");
        return false;
    }
    public bool AdminMaxScatterPerSpinAyarla(int hedef)
    {
        int onceki = maxScatterPerSpin;
        int min = Mathf.Max(1, scatterEsik > 0 ? 1 : 1);
        maxScatterPerSpin = Mathf.Clamp(hedef, min, 5);
        Debug.Log($"[ADMIN-SENARYO] MaxScatterPerSpin ayarlandı: {onceki} -> {maxScatterPerSpin}");
        return onceki != maxScatterPerSpin;
    }
    private void SpinPolitikasiniYenile()
    {
        _spinPolitikasi = SenaryoSpinPolitikasiFabrikasi.Olustur(_senaryoPresetAktif, _aktifAdminSenaryoIndex);
    }
    private ISenaryoSpinPolitikasi SpinPolitikasiniAl()
    {
        if (_spinPolitikasi == null)
            SpinPolitikasiniYenile();
        return _spinPolitikasi;
    }
    private void ZorlaCarpanIlkDususEfektiniBaslat(SpinSimulasyonKaydi kayit)
    {
        if (!zorlaCarpanIlkDususSokEfektiAktif || kayit == null)
            return;
        if (_carpanSokEfektServisi == null)
            return;

        int carpanDegeri = ZorlaCarpanDegeriniBul(kayit);
        if (kayit.ZorlaCarpanKullanildi && carpanDegeri <= 0)
            carpanDegeri = 100; // Kayıtta değer yakalanamazsa bile zorla çarpan spininde efekti garanti et.
        if (carpanDegeri <= 0)
            return;

        RectTransform sarsintiHedefi = null;
        if (kazancText != null && kazancText.canvas != null)
            sarsintiHedefi = kazancText.canvas.rootCanvas.transform as RectTransform;
        if (sarsintiHedefi == null && hucreler != null && hucreler.Length > 0 && hucreler[0] != null && hucreler[0].canvas != null)
            sarsintiHedefi = hucreler[0].canvas.rootCanvas.transform as RectTransform;
        if (sarsintiHedefi == null)
        {
            Debug.LogWarning("[SOK-EFEKT] İlk düşüş sarsıntı hedefi bulunamadı; kazanç text canvas referansını kontrol et.");
            return;
        }

        AudioSource kaynak = tumbleSfxSource != null ? tumbleSfxSource : bonusEndSfxSource;
        if (zorlaCarpanIlkDususSokClip == null)
            Debug.LogWarning("[SOK-EFEKT] Zorla çarpan ilk düşüş ses klibi atanmadı; yalnızca sarsıntı oynatılacak.");

        _carpanSokEfektServisi.BaslatIlkDususSokEfekti(
            this,
            sarsintiHedefi,
            carpanDegeri,
            kaynak,
            zorlaCarpanIlkDususSokClip,
            zorlaCarpanIlkDususSokSesSeviyesi);
        _bombEfektServisi?.BombEfektBaslat(this, carpanDegeri);
        Debug.Log($"[SOK-EFEKT] İlk düşüş sarsıntısı tetiklendi. x{carpanDegeri} | Zorla={kayit.ZorlaCarpanKullanildi}");
    }

    private int ZorlaCarpanDegeriniBul(SpinSimulasyonKaydi kayit)
    {
        if (kayit == null || !kayit.ZorlaCarpanKullanildi)
            return 0;

        int enYuksek = 0;
        if (kayit.IlkCarpanDegerleri != null)
        {
            for (int i = 0; i < kayit.IlkCarpanDegerleri.Count; i++)
                enYuksek = Mathf.Max(enYuksek, kayit.IlkCarpanDegerleri[i]);
        }

        if (enYuksek <= 0 && kayit.IlkCarpanGrid != null)
        {
            int xmax = Mathf.Min(sutun, kayit.Sutun);
            int ymax = Mathf.Min(satir, kayit.Satir);
            for (int x = 0; x < xmax; x++)
                for (int y = 0; y < ymax; y++)
                    enYuksek = Mathf.Max(enYuksek, kayit.IlkCarpanGrid[x, y]);
        }

        if (enYuksek <= 0 && kayit.Adimlar != null)
        {
            for (int a = 0; a < kayit.Adimlar.Count; a++)
            {
                var adim = kayit.Adimlar[a];
                if (adim?.CarpanDegerleriBuTur == null) continue;
                for (int i = 0; i < adim.CarpanDegerleriBuTur.Count; i++)
                    enYuksek = Mathf.Max(enYuksek, adim.CarpanDegerleriBuTur[i]);
            }
        }

        return enYuksek;
    }
}
