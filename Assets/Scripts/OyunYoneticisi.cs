using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.SceneManagement;

public partial class OyunYoneticisi : MonoBehaviour, SahneBaglamaServisi.IBaglamaHedefi, IDonusAkisBaglami, IOyunUIGuncellemeBaglami, IScatterEfektBaglami, ITumbleAkisBaglami, ICokmeAkisBaglami, IIzgaraBaslatmaBaglami, IOyunBootstrapBaglami, ICarpanYerlestirmeBaglami, IZorlukBaglami, IOyunKorumaBaglami
{

    int IOyunKorumaBaglami.GetMaxTumbleTur() => OyunKorumaServisi.MAX_TUMBLE_TUR;
    int IOyunKorumaBaglami.GetTumbleSabitEsik() => OyunKorumaServisi.TUMBLE_SABIT_ESIK;

    // Inspector / AdminPanel bağlantıları için wrapper (asıl binding AdminAyarUIServisi.BindAllAndRefresh)

    private void EnsurePayTablesInitialized()
{
    int n = (sembolSpriteListesi != null) ? sembolSpriteListesi.Count : 0;
    if (n <= 0) return;

    // TumbleAyarlari'ndaki PayTable'ı kullan (ScatterIndex tek kaynak TumbleAyarlari'da)
    if (tumbleAyarlari != null)
        tumbleAyarlari.EnsurePayTablesInitialized(n);
}

    private void UygulaCarpanAyarlari()
    {
        if (carpanAyarlari == null) return;

        // CarpanAyarlari -> OyunYoneticisi (eski alanlara kopyala)
        carpanUretimiAktif = carpanAyarlari.CarpanUretimiAktif;
        carpanSadeceBonus = carpanAyarlari.CarpanSadeceBonus;
        carpanUretimOlasiligi = Mathf.Clamp01(carpanAyarlari.CarpanUretimOlasiligi);
        maxCarpanAdedi = Mathf.Max(0, carpanAyarlari.MaxCarpanAdedi);
        carpanHavuzu = Mathf.Max(0, carpanAyarlari.CarpanHavuzu);
        yuksekCarpanOrani = Mathf.Clamp01(carpanAyarlari.YuksekCarpanOrani);
        zorlaSiradakiCarpan = Mathf.Max(0, carpanAyarlari.ZorlaSiradakiCarpan);

        carpanSembolSprite = carpanAyarlari.CarpanSembolSprite;
        carpanOverlaySize = carpanAyarlari.CarpanOverlaySize;
        carpanOverlayFontSize = Mathf.Max(1, carpanAyarlari.CarpanOverlayFontSize);

        carpanYaziRengi = carpanAyarlari.CarpanYaziRengi;
        carpanYaziDisCizgiRengi = carpanAyarlari.CarpanYaziDisCizgiRengi;
        carpanYaziDisCizgiKalinlik = Mathf.Clamp01(carpanAyarlari.CarpanYaziDisCizgiKalinlik);
        carpanYaziKalin = carpanAyarlari.CarpanYaziKalin;

        carpanYaziGolge = carpanAyarlari.CarpanYaziGolge;
        carpanYaziGolgeRengi = carpanAyarlari.CarpanYaziGolgeRengi;
        carpanYaziGolgeOffset = carpanAyarlari.CarpanYaziGolgeOffset;

        carpanGradientAktif = carpanAyarlari.CarpanGradientAktif;
        carpanGradientUst = carpanAyarlari.CarpanGradientUst;
        carpanGradientAlt = carpanAyarlari.CarpanGradientAlt;
        carpanCharacterSpacing = carpanAyarlari.CarpanCharacterSpacing;
        carpanUnderlayAktif = carpanAyarlari.CarpanUnderlayAktif;
        carpanUnderlayRengi = carpanAyarlari.CarpanUnderlayRengi;
        carpanUnderlayOffsetX = carpanAyarlari.CarpanUnderlayOffsetX;
        carpanUnderlayOffsetY = carpanAyarlari.CarpanUnderlayOffsetY;
        carpanUnderlayDilate = carpanAyarlari.CarpanUnderlayDilate;
        carpanUnderlaySoftness = carpanAyarlari.CarpanUnderlaySoftness;
        carpanGlowAktif = carpanAyarlari.CarpanGlowAktif;
        carpanGlowRengi = carpanAyarlari.CarpanGlowRengi;
        carpanGlowOuter = carpanAyarlari.CarpanGlowOuter;
        carpanGlowInner = carpanAyarlari.CarpanGlowInner;
        carpanGlowPower = carpanAyarlari.CarpanGlowPower;

        carpanOverlayTextOffset = carpanAyarlari.CarpanOverlayTextOffset;
        carpanOverlayDropStartYOffset = carpanAyarlari.CarpanOverlayDropStartYOffset;
        carpanOverlayDropDuration = Mathf.Max(0f, carpanAyarlari.CarpanOverlayDropDuration);

        // Admin slider UI varsa, degerleri senkronla (slider kendi UI textini zaten Start'ta guncelliyor)
        if (carpanOlasilikSlider != null)
        {
            float yuzde = Mathf.Clamp(carpanUretimOlasiligi * 100f, carpanOlasilikSlider.minValue, carpanOlasilikSlider.maxValue);
            carpanOlasilikSlider.value = yuzde;
        }

        if (carpanMaxAdetSlider != null)
        {
            float v = Mathf.Clamp(maxCarpanAdedi, carpanMaxAdetSlider.minValue, carpanMaxAdetSlider.maxValue);
            carpanMaxAdetSlider.value = v;
        }
    }

    // SahneBaglamaServisi.IBaglamaHedefi — Inspector ref'leri korunur; sadece null olanlar servis tarafından doldurulur
    Button SahneBaglamaServisi.IBaglamaHedefi.CevirButon { get => cevirButon; set => cevirButon = value; }
    TMP_Text SahneBaglamaServisi.IBaglamaHedefi.BakiyeText { get => bakiyeText; set => bakiyeText = value; }
    TMP_Text SahneBaglamaServisi.IBaglamaHedefi.BahisText { get => bahisText; set => bahisText = value; }
    TMP_Text SahneBaglamaServisi.IBaglamaHedefi.HakText { get => hakText; set => hakText = value; }
    TMP_Text SahneBaglamaServisi.IBaglamaHedefi.BonusOyunKazancText { get => bonusOyunKazancText; set => bonusOyunKazancText = value; }
    TMP_Text SahneBaglamaServisi.IBaglamaHedefi.KazancText { get => kazancText; set => kazancText = value; }
    TMP_Text SahneBaglamaServisi.IBaglamaHedefi.CarpanText { get => carpanText; set => carpanText = value; }
    TMP_Text SahneBaglamaServisi.IBaglamaHedefi.CarpanOlasilikValueText { get => carpanOlasilikValueText; set => carpanOlasilikValueText = value as TextMeshProUGUI; }
    TMP_Text SahneBaglamaServisi.IBaglamaHedefi.CarpanMaxAdetValueText { get => carpanMaxAdetValueText; set => carpanMaxAdetValueText = value as TextMeshProUGUI; }
    Button SahneBaglamaServisi.IBaglamaHedefi.BakiyeYukleButon { get => bakiyeYukleButon; set => bakiyeYukleButon = value; }
    GameObject SahneBaglamaServisi.IBaglamaHedefi.BakiyeYuklePanel { get => bakiyeYuklePanel; set => bakiyeYuklePanel = value; }
    TMP_InputField SahneBaglamaServisi.IBaglamaHedefi.BakiyeYukleInput { get => bakiyeYukleInput; set => bakiyeYukleInput = value; }
    Button SahneBaglamaServisi.IBaglamaHedefi.BakiyeYukleOnayButon { get => bakiyeYukleOnayButon; set => bakiyeYukleOnayButon = value; }
    Button SahneBaglamaServisi.IBaglamaHedefi.BakiyeYukleIptalButon { get => bakiyeYukleIptalButon; set => bakiyeYukleIptalButon = value; }
    Button SahneBaglamaServisi.IBaglamaHedefi.ParaCekButon { get => paraCekButon; set => paraCekButon = value; }
    GameObject SahneBaglamaServisi.IBaglamaHedefi.ParaCekPanel { get => paraCekPanel; set => paraCekPanel = value; }
    TMP_InputField SahneBaglamaServisi.IBaglamaHedefi.ParaCekInput { get => paraCekInput; set => paraCekInput = value; }
    Button SahneBaglamaServisi.IBaglamaHedefi.ParaCekOnayButon { get => paraCekOnayButon; set => paraCekOnayButon = value; }
    Button SahneBaglamaServisi.IBaglamaHedefi.ParaCekIptalButon { get => paraCekIptalButon; set => paraCekIptalButon = value; }
    Button SahneBaglamaServisi.IBaglamaHedefi.BonusSatinAlButon { get => bonusSatinAlButon; set => bonusSatinAlButon = value; }
    GameObject SahneBaglamaServisi.IBaglamaHedefi.BonusBuyConfirmPanel { get => bonusBuyConfirmPanel; set => bonusBuyConfirmPanel = value; }
    TMP_Text SahneBaglamaServisi.IBaglamaHedefi.BonusBuyConfirmCostText { get => bonusBuyConfirmCostText; set => bonusBuyConfirmCostText = value; }
    CanvasGroup SahneBaglamaServisi.IBaglamaHedefi.BonusBuyConfirmCanvasGroup { get => bonusBuyConfirmCanvasGroup; set => bonusBuyConfirmCanvasGroup = value; }
    Button SahneBaglamaServisi.IBaglamaHedefi.BonusBuyYesButton { get => bonusBuyYesButton; set => bonusBuyYesButton = value; }
    Button SahneBaglamaServisi.IBaglamaHedefi.BonusBuyNoButton { get => bonusBuyNoButton; set => bonusBuyNoButton = value; }
    GameObject SahneBaglamaServisi.IBaglamaHedefi.BonusStartPanel { get => bonusStartPanel; set => bonusStartPanel = value; }
    GameObject SahneBaglamaServisi.IBaglamaHedefi.BonusEndPanel { get => bonusEndPanel; set => bonusEndPanel = value; }
    CanvasGroup SahneBaglamaServisi.IBaglamaHedefi.BonusEndCanvasGroup { get => bonusEndCanvasGroup; set => bonusEndCanvasGroup = value; }
    CanvasGroup SahneBaglamaServisi.IBaglamaHedefi.BonusStartCanvasGroup { get => bonusStartCanvasGroup; set => bonusStartCanvasGroup = value; }
    Button SahneBaglamaServisi.IBaglamaHedefi.OtomatikSpinButton { get => otomatikSpinButton; set => otomatikSpinButton = value; }
    GameObject SahneBaglamaServisi.IBaglamaHedefi.OtomatikSpinPanel { get => otomatikSpinPanel; set => otomatikSpinPanel = value; }
    TMP_Dropdown SahneBaglamaServisi.IBaglamaHedefi.OtomatikSpinDropdown { get => otomatikSpinDropdown; set => otomatikSpinDropdown = value; }
    Button SahneBaglamaServisi.IBaglamaHedefi.OtomatikSpinBaslatButon { get => otomatikSpinBaslatButon; set => otomatikSpinBaslatButon = value; }
    Button SahneBaglamaServisi.IBaglamaHedefi.OtomatikSpinIptalButon { get => otomatikSpinIptalButon; set => otomatikSpinIptalButon = value; }
    TMP_Text SahneBaglamaServisi.IBaglamaHedefi.OtomatikSpinKalanText { get => otomatikSpinKalanText; set => otomatikSpinKalanText = value; }

    // IDonusAkisBaglami — state ve servis erişimi (arayüz DonusAkisServisi.cs içinde)
    UIServisi IDonusAkisBaglami.UIServisi => _uiServisi;
    IzgaraServisi IDonusAkisBaglami.IzgaraServisi => _izgaraServisi;
    OdemeServisi IDonusAkisBaglami.OdemeServisi => _odemeServisi;
    AnimasyonServisi IDonusAkisBaglami.AnimasyonServisi => _animasyonServisi;
    TumbleServisi IDonusAkisBaglami.TumbleServisi => _tumbleServisi;
    CarpanServisi IDonusAkisBaglami.CarpanServisi => _carpanServisi;
    EkonomiServisi IDonusAkisBaglami.EkonomiServisi => _ekonomiServisi;
    LogServisi IDonusAkisBaglami.DonusKayitServisi => _logServisi;
    SenaryoServisi IDonusAkisBaglami.SenaryoServisi => _senaryoServisi;
    HizVeSesServisi IDonusAkisBaglami.HizVeSesServisi => _hizVeSesServisi;
    bool IDonusAkisBaglami.SpinCalisiyor { get => spinCalisiyor; set => spinCalisiyor = value; }
    bool IDonusAkisBaglami.BonusAktif { get => bonusAktif; set => bonusAktif = value; }
    int IDonusAkisBaglami.BonusHakKalan { get => bonusHakKalan; set => bonusHakKalan = value; }
    int IDonusAkisBaglami.BonusKazanc { get => bonusKazanc; set => bonusKazanc = value; }
    int IDonusAkisBaglami.OturumKazanc { get => oturumKazanc; set => oturumKazanc = value; }
    int IDonusAkisBaglami.BonusPendingOdemeTL { get => _bonusPendingOdemeTL; set => _bonusPendingOdemeTL = value; }
    int IDonusAkisBaglami.BonusZorlaCarpanBirikenTL { get => _bonusZorlaCarpanBirikenTL; set => _bonusZorlaCarpanBirikenTL = value; }
    bool IDonusAkisBaglami.SpinKazanciOturumaEklendi { get => _spinKazanciOturumaEklendi; set => _spinKazanciOturumaEklendi = value; }
    int IDonusAkisBaglami.SpinKazancHam { get => spinKazancHam; set => spinKazancHam = value; }
    int IDonusAkisBaglami.TumbleToplamKazanc { get => tumbleToplamKazanc; set => tumbleToplamKazanc = value; }
    int IDonusAkisBaglami.SonSpinKazanci { get => sonSpinKazanci; set => sonSpinKazanci = value; }
    int IDonusAkisBaglami.SpinPrevBakiye => _spinPrevBakiye;
    int IDonusAkisBaglami.SpinBahisTL => _spinBahisTL;
    float IDonusAkisBaglami.BonusSpinBekleme => bonusSpinBekleme;
    int IDonusAkisBaglami.SonSpinKazancHamGoster { set => sonSpinKazancHamGoster = value; }
    int IDonusAkisBaglami.SonSpinCarpanGoster { set => sonSpinCarpanGoster = value; }
    int IDonusAkisBaglami.SonSpinKazancToplamGoster { set => sonSpinKazancToplamGoster = value; }
    int IDonusAkisBaglami.BonusOturumOdenenToplamTL { get => _bonusOturumOdenenToplamTL; set => _bonusOturumOdenenToplamTL = value; }
    int IDonusAkisBaglami.BonusMaxOdemeTL => _bonusMaxOdemeTL;
    int IDonusAkisBaglami.BonusOdenenTL => _bonusOdenenTL;
    bool IDonusAkisBaglami.BonusBudgetAktif => bonusBudgetAktif;
    int IDonusAkisBaglami.BonusBudgetKalanTL => _bonusBudgetKalanTL;
    int[,] IDonusAkisBaglami.Grid => grid;
    int IDonusAkisBaglami.Satir => satir;
    int IDonusAkisBaglami.Sutun => sutun;
    int IDonusAkisBaglami.spinNo { get => _spinNo; set => _spinNo = value; }
    void IDonusAkisBaglami.UI_CarpanSifirla() => UI_CarpanSifirla();
    void IDonusAkisBaglami.CarpanKutuUcusFormulKilidiniKaldir()
    {
        _carpanKutuUcusFormulKilit = false;
        _carpanKutuUcusBirikimSonDeger = 0;
    }
    void IDonusAkisBaglami.CarpanFormulGosterAktivateEt(int birikimSonDeger)
    {
        _carpanKutuUcusFormulKilit = true;
        _carpanKutuUcusBirikimSonDeger = birikimSonDeger;
    }
    void IDonusAkisBaglami.CarpanUretVeBirik() => CarpanUretVeBirik();
    void IDonusAkisBaglami.CarpanlariDoluGriddeUygula() => _carpanYerlestirmeServisi?.CarpanlariDoluGriddeUygula();
    void IDonusAkisBaglami.BaslatBonus() => BaslatBonus();
    IEnumerator IDonusAkisBaglami.ScatterBuyutEfekti() => ScatterBuyutEfekti();
    IEnumerator IDonusAkisBaglami.ShowBonusEndMessage(int bonusToplamKazanc) => ShowBonusEndMessage(bonusToplamKazanc);
    void IDonusAkisBaglami.SetSpinIconRotate(bool rotate) { if (spinIcon != null) spinIcon.SetRotate(rotate); }
    void IDonusAkisBaglami.SetOturumKazancTextActive(bool active) { if (oturumKazancText != null) oturumKazancText.gameObject.SetActive(active); }
    void IDonusAkisBaglami.NormalOyunMusicPlay() { if (normalOyunMusic != null && normalOyunMusic.clip != null && !normalOyunMusic.isPlaying) normalOyunMusic.Play(); }
    void IDonusAkisBaglami.NormalOyunMusicUnPause() { if (normalOyunMusic != null) normalOyunMusic.UnPause(); }
    SpinSimulasyonKaydi IDonusAkisBaglami.SimuleEtVeKaydet(int odenebilirLimit, bool bonusSpin) => SimuleEtVeKaydetImpl(odenebilirLimit, bonusSpin);
    bool IDonusAkisBaglami.TryConsumeOncedenHesaplanan(bool forBonusSpin, out SpinSimulasyonKaydi kayit)
    {
        kayit = null;
        var spinPolitikasi = SpinPolitikasiniAl();
        // Önceden hesaplanan sonuç Force seçilmeden üretilmiş olabilir; zorla çarpan beklenirken önbelleği kullanma.
        if (zorlaSiradakiCarpan > 0)
        {
            OncedenHesaplananSpinOnbelleginiTemizle();
            return false;
        }
        if (!_oncedenHesaplananHazir || _oncedenHesaplananBonusMu != forBonusSpin)
            return false;
        var adayKayit = _oncedenHesaplananKayit;
        // Önbellek, önceki senaryo/bahis/üst üste fazına göre üretilmiş olabilir; preset değişince eski sonuç kullanılmasın.
        if (spinPolitikasi.OncedenHesaplanmisNormalSpinOdemeYenidenDogrulansinMi(adayKayit, forBonusSpin))
        {
            int bahisOnbellek = _ekonomiServisi != null ? _ekonomiServisi.Bahis : 0;
            int nihaiOnbellek = _carpanServisi != null
                ? _carpanServisi.MulClampInt(adayKayit.ToplamHamKazanc, Mathf.Max(1, adayKayit.NihaiCarpanToplam))
                : adayKayit.ToplamHamKazanc;
            if (!OdemeModelineUygunMu(nihaiOnbellek, bahisOnbellek, SIMULASYON_MAX_REROLL, SIMULASYON_MAX_REROLL))
            {
                Debug.LogWarning("[SIM][ÖNBELLEK] Önceden hesaplanmış spin güncel admin senaryosu / ödeme modeli ile uyumsuz; yeniden hesaplanacak.");
                OncedenHesaplananSpinOnbelleginiTemizle();
                return false;
            }
        }
        if (!forBonusSpin && AdminOyunSahnesiMi() && _ustUsteKayipHedef == 0 && _adminVideoArdisikKazancSpinKalan > 0 && adayKayit != null)
        {
            int bahisVideo = _ekonomiServisi != null ? _ekonomiServisi.Bahis : 0;
            int nihaiVideo = _carpanServisi != null
                ? _carpanServisi.MulClampInt(adayKayit.ToplamHamKazanc, Mathf.Max(1, adayKayit.NihaiCarpanToplam))
                : adayKayit.ToplamHamKazanc;
            if (nihaiVideo <= bahisVideo)
            {
                OncedenHesaplananSpinOnbelleginiTemizle();
                return false;
            }
        }
        kayit = _oncedenHesaplananKayit;
        _oncedenHesaplananKayit = null;
        _oncedenHesaplananHazir = false;
        return true;
    }
    void IDonusAkisBaglami.StartPrecomputeNextSpin(int odenebilirLimit, bool bonusSpin) => StartCoroutine(PrecomputeNextSpinCoroutine(odenebilirLimit, bonusSpin));

    void IDonusAkisBaglami.Senaryo2Veya3SpinSonundaUstUsteDonguIlerlet()
    {
        if (!IsAdminSenaryo2Aktif() && !IsAdminSenaryo3Aktif() && !IsAdminSenaryo4Aktif() && !IsAdminSenaryo5Aktif()) return;
        // S4/S5 kendi döngü index'ini kullanır; UstUsteDongu hedefleri sıfır olsa bile ilerlet.
        if (!UstUsteDonguAktifMi() && !IsAdminSenaryo4Aktif() && !IsAdminSenaryo5Aktif()) return;
        Debug.Log($"[DÖNGÜ-İLERLET] S4={IsAdminSenaryo4Aktif()} S5={IsAdminSenaryo5Aktif()} s5idx={_senaryo5DonguIndex} s4idx={_senaryo4DonguIndex}");
        // Simülasyon kabulünde değil, yalnızca NormalSpinAkisi sonunda çağrılır (önbellek spininde index kayması olmasın).
        UstUsteDonguyuSpinSonucuIleIlerle(false);
    }

    bool IDonusAkisBaglami.Senaryo23SpinSonrasiDonguIlerletilmeliMi(SpinSimulasyonKaydi kayit)
    {
        if (bonusAktif) return false;
        if (!IsAdminSenaryo2Aktif() && !IsAdminSenaryo3Aktif() && !IsAdminSenaryo4Aktif() && !IsAdminSenaryo5Aktif()) return false;
        if (kayit == null) return false;
        // Bomb spinlerinde ZorlaCarpanKullanildi=true efekt için set edilir; index ilerlemesini engelleme.
        bool s4BombSpini = IsAdminSenaryo4Aktif() && Senaryo4DonguSpinTipi() == SenaryoBombSpinTipi.Bomb;
        bool s5BombSpini = IsAdminSenaryo5Aktif() && Senaryo5DonguSpinTipi() == SenaryoBombSpinTipi.Bomb;
        if (!s4BombSpini && !s5BombSpini && kayit.ZorlaCarpanKullanildi) return false;
        Debug.Log($"[İLERLET-KARAR] S4Bomb={s4BombSpini} S5Bomb={s5BombSpini} ZorlaKullanildi={kayit.ZorlaCarpanKullanildi} BandaUygun={kayit.SenaryoOdemeBandinaUygun}");
        return kayit.SenaryoOdemeBandinaUygun;
    }

    IEnumerator IDonusAkisBaglami.SimulasyonKaydiniOynat(SpinSimulasyonKaydi kayit) => SimulasyonKaydiniOynatImpl(kayit);
    void IDonusAkisBaglami.TryResumeOtomatikSpin() => TryResumeOtomatikSpin();
    bool IDonusAkisBaglami.OtomatikSpinAktifMi => _otomatikSpinKalan > 0;
    bool IDonusAkisBaglami.CarpanTumbleAktif => _carpanTumbleAktif;
    int IDonusAkisBaglami.MinOdemeTL => _minOdemeTL;
    float IDonusAkisBaglami.MinOdemeCarpan => _minOdemeCarpan;
    float IDonusAkisBaglami.MaksOdemeCarpan => _maksOdemeCarpan;
    int IDonusAkisBaglami.ArdisikKayipLimiti => _ardisikKayipLimiti;
    int IDonusAkisBaglami.ArdisikKayipSayac { get => _ardisikKayipSayac; set => _ardisikKayipSayac = value; }
    void IDonusAkisBaglami.SonrakiSpinKacisFrenlemeAktifEt()
    {
        _kacisFrenlemeBuSpinAktif = true;
        // Önbellekteki spin cluster zorlamasını içermez; geçersizleştir ki yeni spin garanti cluster ile üretilsin.
        OncedenHesaplananSpinOnbelleginiTemizle();
    }
    int IDonusAkisBaglami.BonusOtomatikSpinPeriyodu => bonusOtomatikSpinPeriyodu;
    int IDonusAkisBaglami.BonusOtomatikSpinSayaci { get => _bonusOtomatikSpinSayaci; set => _bonusOtomatikSpinSayaci = value; }
    bool IDonusAkisBaglami.BonusOtomatikTetikSonrakiSpin { get => _bonusOtomatikTetikSonrakiSpin; set => _bonusOtomatikTetikSonrakiSpin = value; }
    int IDonusAkisBaglami.YakinKacirmaDegeri10da => yakinKacirmaDegeri10da;
    void IDonusAkisBaglami.GrideNearMissEnjekteEt() => GrideNearMissEnjekteEt();
    IEnumerator IDonusAkisBaglami.ShowNormalSpinSonucPopup(int odened, int bahis) => ShowNormalSpinSonucPopup(odened, bahis);

    // IOyunUIGuncellemeBaglami
    TMP_Text IOyunUIGuncellemeBaglami.BakiyeText => bakiyeText;
    TMP_Text IOyunUIGuncellemeBaglami.BahisText => bahisText;
    TMP_Text IOyunUIGuncellemeBaglami.HakText => hakText;
    TMP_Text IOyunUIGuncellemeBaglami.SpinKazancText => oturumKazancText;
    TMP_Text IOyunUIGuncellemeBaglami.BonusOyunKazancText => bonusOyunKazancText;
    TMP_Text IOyunUIGuncellemeBaglami.KazancText => kazancText;
    TMP_Text IOyunUIGuncellemeBaglami.CarpanText => carpanText;
    TMP_Text IOyunUIGuncellemeBaglami.BonusSatinAlText => bonusSatinAlText;
    Button IOyunUIGuncellemeBaglami.CevirButon => cevirButon;
    Button IOyunUIGuncellemeBaglami.ParaCekButon => paraCekButon;
    Button IOyunUIGuncellemeBaglami.BakiyeYukleButon => bakiyeYukleButon;
    Button IOyunUIGuncellemeBaglami.BahisArttirButon => bahisArttirButon;
    Button IOyunUIGuncellemeBaglami.BahisAzaltButon => bahisAzaltButon;
    Button IOyunUIGuncellemeBaglami.BonusSatinAlButon => bonusSatinAlButon;
    Button IOyunUIGuncellemeBaglami.ParaCekOnayButon => paraCekOnayButon;
    Button IOyunUIGuncellemeBaglami.ParaCekIptalButon => paraCekIptalButon;
    Button IOyunUIGuncellemeBaglami.BakiyeYukleOnayButon => bakiyeYukleOnayButon;
    Button IOyunUIGuncellemeBaglami.BakiyeYukleIptalButon => bakiyeYukleIptalButon;
    GameObject IOyunUIGuncellemeBaglami.ParaCekPanel => paraCekPanel;
    GameObject IOyunUIGuncellemeBaglami.BakiyeYuklePanel => bakiyeYuklePanel;
    GameObject IOyunUIGuncellemeBaglami.BonusBuyConfirmPanel => bonusBuyConfirmPanel;
    GameObject IOyunUIGuncellemeBaglami.BonusSatinAlRoot => bonusSatinAlRoot;
    int IOyunUIGuncellemeBaglami.GetBakiye() => _ekonomiServisi != null ? _ekonomiServisi.Bakiye : 0;
    int IOyunUIGuncellemeBaglami.GetBahis() => _ekonomiServisi != null ? _ekonomiServisi.Bahis : 0;
    int IOyunUIGuncellemeBaglami.GetBahisMin() => bahisMin;
    int IOyunUIGuncellemeBaglami.GetBahisMax() => bahisMax;
    bool IOyunUIGuncellemeBaglami.GetBonusAktif() => bonusAktif;
    bool IOyunUIGuncellemeBaglami.GetSpinCalisiyor() => spinCalisiyor;
    int IOyunUIGuncellemeBaglami.GetBonusHakKalan() => bonusHakKalan;
    int IOyunUIGuncellemeBaglami.GetBonusKazanc() => bonusKazanc;
    int IOyunUIGuncellemeBaglami.GetOturumKazanc() => oturumKazanc;
    int IOyunUIGuncellemeBaglami.GetSonSpinKazanci() => sonSpinKazanci;
    bool IOyunUIGuncellemeBaglami.GetSpinKazanciOturumaEklendi() => _spinKazanciOturumaEklendi;
    int IOyunUIGuncellemeBaglami.GetSonSpinKazancHamGoster() => sonSpinKazancHamGoster;
    int IOyunUIGuncellemeBaglami.GetSonSpinCarpanGoster() => sonSpinCarpanGoster;
    bool IOyunUIGuncellemeBaglami.GetCarpanKutuUcusAktif() => _carpanKutuUcusAktif;

    int IOyunUIGuncellemeBaglami.GetCarpanKutuUcusKazancMetniCarpanCarpimi()
    {
        if (_carpanKutuUcusAktif)
            return Mathf.Max(_carpanKutuUcusBirikim, _carpanKutuUcusBirikimGosterMax);
        if (_carpanKutuUcusFormulKilit)
            return Mathf.Max(0, _carpanKutuUcusBirikimSonDeger);
        return 0;
    }

    bool IOyunUIGuncellemeBaglami.GetCarpanKutuUcusFormulKilit() => _carpanKutuUcusFormulKilit;

    int IOyunUIGuncellemeBaglami.GetCarpanToplamCarpimInt() =>
        _carpanKutuUcusAktif
            ? Mathf.Max(_carpanKutuUcusBirikim, _carpanKutuUcusBirikimGosterMax)
            : (_carpanServisi != null ? _carpanServisi.GetTotalMultiplierForSpin() : 1);

    int IOyunUIGuncellemeBaglami.GetSonSpinKazancCarpanliOnizlemeTL()
    {
        if (_carpanServisi == null) return Mathf.Max(0, sonSpinKazancHamGoster);
        int ham = Mathf.Max(0, sonSpinKazancHamGoster);
        if (_carpanKutuUcusAktif)
        {
            int birikimGoster = Mathf.Max(_carpanKutuUcusBirikim, _carpanKutuUcusBirikimGosterMax);
            if (birikimGoster <= 0)
                return ham;
            return _carpanServisi.MulClampInt(ham, birikimGoster);
        }
        if (_carpanKutuUcusFormulKilit)
        {
            if (_carpanKutuUcusBirikimSonDeger <= 0)
                return ham;
            return _carpanServisi.MulClampInt(ham, _carpanKutuUcusBirikimSonDeger);
        }
        long ml = _carpanServisi.GetCurrentMultiplier();
        if (ml < 1) ml = 1;
        return _carpanServisi.MulClampInt(ham, ml);
    }
    int IOyunUIGuncellemeBaglami.GetSonSpinKazancToplamGoster() => sonSpinKazancToplamGoster;
    int IOyunUIGuncellemeBaglami.GetBonusMaliyeti() => Mathf.Max(0, _ekonomiServisi != null ? _ekonomiServisi.Bahis : 0) * Mathf.Max(1, bonusSatinAlCarpani);
    void IOyunUIGuncellemeBaglami.RefreshCarpanDisplay() => UI_CarpanGuncelle();
    void IOyunUIGuncellemeBaglami.ShowParaCekPanel() => _uiServisi?.ShowParaCekPanel();
    void IOyunUIGuncellemeBaglami.HideParaCekPanel() => _uiServisi?.HideParaCekPanel();
    void IOyunUIGuncellemeBaglami.ShowBakiyeYuklePanel() => _uiServisi?.ShowBakiyeYuklePanel();
    void IOyunUIGuncellemeBaglami.HideBakiyeYuklePanel() => _uiServisi?.HideBakiyeYuklePanel();
    void IOyunUIGuncellemeBaglami.OnParaCekOnay() => _ekonomiServisi?.OnParaCekOnay();
    void IOyunUIGuncellemeBaglami.OnBakiyeYukleOnay() => BakiyeYukle_OnayButton();
    void IOyunUIGuncellemeBaglami.SyncOtomatikSpinKalanTextVisibility() => OtomatikSpinKalanTextGuncelle();

    // IScatterEfektBaglami
    int[,] IScatterEfektBaglami.Grid => grid;
    int IScatterEfektBaglami.ScatterIndex => _scatterIndexCache;
    int IScatterEfektBaglami.Sutun => sutun;
    int IScatterEfektBaglami.Satir => satir;
    int IScatterEfektBaglami.XYToIndex(int x, int y) => _izgaraServisi != null ? _izgaraServisi.XYToIndex(x, y) : -1;
    Image[] IScatterEfektBaglami.Hucreler => hucreler;
    float IScatterEfektBaglami.ScatterScaleUp => scatterScaleUp;
    float IScatterEfektBaglami.ScatterAnimDuration => scatterAnimDuration;

    // ITumbleAkisBaglami
    int ITumbleAkisBaglami.GetMinClusterSize() => minClusterSize;
    bool ITumbleAkisBaglami.GetBonusAktif() => bonusAktif;
    int ITumbleAkisBaglami.GetBonusRemainingPayableTL() => _senaryoServisi != null ? _senaryoServisi.GetBonusRemainingPayableTL() : int.MaxValue;
    int ITumbleAkisBaglami.GetCurrentMultiplierInt() => _carpanServisi != null ? _carpanServisi.GetCurrentMultiplierInt() : 1;
    long ITumbleAkisBaglami.GetCurrentMultiplier() => _carpanServisi != null ? _carpanServisi.GetCurrentMultiplier() : 1L;
    int ITumbleAkisBaglami.GetSpinKazancHam() => spinKazancHam;
    void ITumbleAkisBaglami.AddSpinKazancHam(int delta) { spinKazancHam += delta; }
    int ITumbleAkisBaglami.CalculateWinForRemoved(List<Vector2Int> removed) => _tumbleServisi != null ? _tumbleServisi.CalculateWinForRemoved(removed) : 0;
    List<Vector2Int> ITumbleAkisBaglami.FindClustersToRemove(int minSize) => _tumbleServisi != null ? _tumbleServisi.FindClustersToRemove(minSize) : new List<Vector2Int>();
    void ITumbleAkisBaglami.CarpanUretVeBirik() => CarpanUretVeBirik();
    void ITumbleAkisBaglami.AddTumbleToplamKazanc(int delta) { tumbleToplamKazanc += delta; }
    void ITumbleAkisBaglami.SetSonSpinKazancHamGoster(int value) { sonSpinKazancHamGoster = value; }
    void ITumbleAkisBaglami.SetSonSpinCarpanGoster(int value) { sonSpinCarpanGoster = value; }
    void ITumbleAkisBaglami.SetSonSpinKazancToplamGoster(int value) { sonSpinKazancToplamGoster = value; }
    void ITumbleAkisBaglami.SetSonSpinKazanci(int value) { sonSpinKazanci = value; }
    int ITumbleAkisBaglami.MulClampInt(int ham, long multiplier) => _carpanServisi != null ? _carpanServisi.MulClampInt(ham, multiplier) : ham;
    void ITumbleAkisBaglami.UI_Guncelle() => _uiServisi?.UI_Guncelle();
    void ITumbleAkisBaglami.PlayTumbleSfx() => _hizVeSesServisi?.PlayTumbleSfx(tumblePopClip, ref _lastTumblePopTime, tumblePopMinInterval, 1f, tumblePopBaslangicOffsetSaniye);
    void ITumbleAkisBaglami.ClearGridCells(List<Vector2Int> toRemove)
    {
        if (toRemove == null || grid == null || carpanDegerGrid == null) return;
        for (int i = 0; i < toRemove.Count; i++)
        {
            int x = toRemove[i].x, y = toRemove[i].y;
            grid[x, y] = -1;
            carpanDegerGrid[x, y] = 0;
            int ridx = _izgaraServisi != null ? _izgaraServisi.XYToIndex(x, y) : -1;
            if (carpanDegerByCellIndex != null && ridx >= 0 && ridx < carpanDegerByCellIndex.Length)
                carpanDegerByCellIndex[ridx] = 0;
        }
    }
    IEnumerator ITumbleAkisBaglami.AnimateCarpanSisme() => _animasyonServisi != null ? _animasyonServisi.AnimateCarpanSisme() : null;
    IEnumerator ITumbleAkisBaglami.AnimatePop(List<Vector2Int> cells) => _tumbleServisi != null ? _tumbleServisi.AnimatePop(cells) : null;
    IEnumerator ITumbleAkisBaglami.CollapseRefillAndAnimate() => _cokmeAkisServisi != null ? _cokmeAkisServisi.CokmeDoldurVeCanlandir() : null;
    float ITumbleAkisBaglami.GetBetweenStepsDelay() => betweenStepsDelay;
    Coroutine ITumbleAkisBaglami.RunCoroutine(IEnumerator enumerator) => enumerator != null ? StartCoroutine(enumerator) : null;

    int ICokmeAkisBaglami.GetSutun() => sutun;
    int ICokmeAkisBaglami.GetSatir() => satir;
    int[,] ICokmeAkisBaglami.GetGrid() => grid;
    int[,] ICokmeAkisBaglami.GetCarpanDegerGrid() => carpanDegerGrid;
    Vector2[] ICokmeAkisBaglami.GetCellPos() => cellPos;
    RectTransform[] ICokmeAkisBaglami.GetCellRT() => cellRT;
    float ICokmeAkisBaglami.GetSpawnFromTopOffset() => spawnFromTopOffset;
    float ICokmeAkisBaglami.GetFallDuration()
    {
        float temel = bonusAktif ? bonusFallDuration : fallDuration;
        // Çok düşük saha değerlerinde tumble dolumu okunaklı kalsın.
        temel = bonusAktif ? Mathf.Max(0.58f, temel) : Mathf.Max(0.62f, temel);
        // Sadece admin sahnesinde otomatik spin sırasında meyve düşüşünü biraz yavaşlat.
        if (_otomatikSpinKalan > 0 && SceneManager.GetActiveScene().name == "03_AdminOyunScene")
            return temel * 1.35f;
        return temel;
    }
    bool ICokmeAkisBaglami.GetBonusAktif() => bonusAktif;
    int ICokmeAkisBaglami.GetCarpanSembol() => CARPAN_SEMBOL;
    IzgaraServisi ICokmeAkisBaglami.GetIzgaraServisi() => _izgaraServisi;
    TumbleServisi ICokmeAkisBaglami.GetTumbleServisi() => _tumbleServisi;
    CarpanServisi ICokmeAkisBaglami.GetCarpanServisi() => _carpanServisi;
    SenaryoServisi ICokmeAkisBaglami.GetSenaryoServisi() => _senaryoServisi;
    bool ICokmeAkisBaglami.ConsumeBombaSonrasiIlkRefillCarpanEngeli()
    {
        bool aktif = _bombaPatlamaSonrasiIlkRefillCarpanEngeli;
        _bombaPatlamaSonrasiIlkRefillCarpanEngeli = false;
        return aktif;
    }
    void ICokmeAkisBaglami.ApplyNewGridAndSync(int[,] newGrid, int[,] newCarpanGrid) => ApplyNewGridAndSync(newGrid, newCarpanGrid);

    IEnumerator ICokmeAkisBaglami.CarpanKazancUcusunuOynat(IReadOnlyList<int> hucreIndeksleri, IReadOnlyList<int> carpanDegerleri)
    {
        if (_carpanOverlayServisi == null || kazancText == null || hucreIndeksleri == null || carpanDegerleri == null)
            yield break;
        if (hucreIndeksleri.Count == 0 || carpanDegerleri.Count == 0)
            yield break;
        _carpanKutuUcusAktif = true;
        _carpanKutuUcusBirikim = 0;
        _carpanKutuUcusBirikimGosterMax = 0;
        _carpanKutuUcusFormulKilit = false;
        _carpanKutuUcusBirikimSonDeger = 0;
        _oyunUIGuncellemeServisi?.RefreshAllUI();
        {
            AudioSource ucusBasKaynak = tumbleSfxSource != null ? tumbleSfxSource : bonusEndSfxSource;
            if (ucusBasKaynak != null && carpanKazancaVurusClip != null)
                ucusBasKaynak.PlayOneShot(carpanKazancaVurusClip, Mathf.Clamp01(carpanKazancaVurusSesSeviyesi * 0.55f));
        }
        yield return _carpanOverlayServisi.KazancaUcuslariSiraliEnum(
            hucreIndeksleri,
            carpanDegerleri,
            kazancText.rectTransform,
            carpan =>
            {
                _carpanKutuUcusBirikim += Mathf.Max(0, carpan);
                _carpanKutuUcusBirikimGosterMax = Mathf.Max(_carpanKutuUcusBirikimGosterMax, _carpanKutuUcusBirikim);
                _carpanKutuUcusBirikimSonDeger = _carpanKutuUcusBirikim;
                if (_carpanKutuUcusBirikim > 0)
                    _carpanKutuUcusFormulKilit = true;
                _oyunUIGuncellemeServisi?.RefreshAllUI();
                if (carpan > 0)
                    StartCoroutine(KazancKutusunaCarpanVurusPlusAnimasyonu(carpan));
                AudioSource kaynak = tumbleSfxSource != null ? tumbleSfxSource : bonusEndSfxSource;
                if (kaynak != null && carpanKazancaVurusClip != null)
                {
                    float clipLen = carpanKazancaVurusClip.length;
                    float bas = Mathf.Clamp(carpanKazancaVurusBaslangicSn, 0f, Mathf.Max(0f, clipLen - 0.02f));
                    float bit = carpanKazancaVurusBitisSn > 0f ? Mathf.Clamp(carpanKazancaVurusBitisSn, 0f, clipLen) : clipLen;
                    if (bit <= bas + 0.01f)
                    {
                        // Geçersiz aralık: tam klibi kısa PlayOneShot ile çal.
                        float eskiPitch = kaynak.pitch;
                        float yeniPitch = UnityEngine.Random.Range(0.96f, 1.06f);
                        kaynak.pitch = yeniPitch;
                        kaynak.PlayOneShot(carpanKazancaVurusClip, Mathf.Clamp01(carpanKazancaVurusSesSeviyesi));
                        kaynak.pitch = eskiPitch;
                    }
                    else
                    {
                        // Klip içinde belirli aralık: geçici AudioSource ile sadece o kesiti çal.
                        GameObject go = new GameObject("TempCarpanKazancSfx");
                        go.transform.SetParent(kaynak.transform, false);
                        var src = go.AddComponent<AudioSource>();
                        src.outputAudioMixerGroup = kaynak.outputAudioMixerGroup;
                        src.spatialBlend = kaynak.spatialBlend;
                        src.priority = kaynak.priority;
                        src.panStereo = kaynak.panStereo;
                        src.pitch = UnityEngine.Random.Range(0.96f, 1.06f);
                        src.volume = Mathf.Clamp01(kaynak.volume * carpanKazancaVurusSesSeviyesi);
                        src.playOnAwake = false;
                        src.loop = false;
                        src.clip = carpanKazancaVurusClip;
                        src.time = bas;
                        src.Play();
                        float sure = Mathf.Max(0.05f, bit - bas);
                        UnityEngine.Object.Destroy(go, sure + 0.15f);
                    }
                }
            });
        _carpanKutuUcusAktif = false;
        _carpanKutuUcusBirikim = 0;
        _carpanKutuUcusBirikimGosterMax = 0;
        // Formül kilidi DonusAkisServisi spin özeti alanlarını yazdıktan sonra kaldırılır; burada erken sıfırlanırsa ham TL ara kare görünür.
        _oyunUIGuncellemeServisi?.RefreshAllUI();
    }

    /// <summary>
    /// Admin / CarpanAyarlari tarafında ForceX değiştiğinde, eski önceden hesaplanmış spini geçersiz kılmak için çağrılır.
    /// Böylece her yeni Force seçiminde bir sonraki spin mutlaka güncel değeri kullanır.
    /// </summary>

    /// <summary>Tek kaynak: Sahnedeki BonusAyarlari / KasaYoneticisi varsa değerleri OY alanlarına kopyala. Bahis limitleri OY default (bahisMin/Max/Adim) ve EkonomiServisi.SetBahisLimits ile verilir.</summary>
    private void SyncFromAyarClassesIfPresent()
    {
        var bonus = FindFirstObjectByType<BonusAyarlari>(FindObjectsInactive.Include);
        _bonusAyarlari = bonus;
        if (bonus != null)
        {
            bonusHakBaslangic = bonus.BonusHakBaslangic;
            bonusSpinBekleme = bonus.BonusSpinBekleme;
            bonusSatinAlCarpani = bonus.BonusSatinAlCarpani;
            bonusBudgetAktif = bonus.BonusBudgetAktif;
            bonusBudgetHavuzOran = bonus.BonusBudgetHavuzOran;
            bonusBudgetMinTL = bonus.BonusBudgetMinTL;
            bonusBudgetMaxTL = bonus.BonusBudgetMaxTL;
            bonusOtoZorlukAktif = bonus.BonusOtoZorlukAktif;
            bonusMinCluster_Easy = bonus.BonusMinCluster_Easy;
            bonusMinCluster_Hard = bonus.BonusMinCluster_Hard;
            // Scatter şansı admin panel slider'dan gelir; config ile üzerine yazma (slider 0 = scatter yok).
            // scatterChanceNormal = bonus.ScatterChanceNormal;
            // scatterChanceBonus = bonus.ScatterChanceBonus;
            scatterEsik = bonus.ScatterEsik;
            scatterScaleUp = bonus.ScatterScaleUp;
            scatterAnimDuration = bonus.ScatterAnimDuration;
        }
        var kasaObj = kasa ?? FindFirstObjectByType<KasaYoneticisi>(FindObjectsInactive.Include);
        if (kasaObj != null)
        {
            bonusBudgetAktif = kasaObj.BonusBudgetAktif;
            bonusBudgetHavuzOran = kasaObj.BonusBudgetHavuzOran;
            bonusBudgetMinTL = kasaObj.BonusBudgetMinTL;
            bonusBudgetMaxTL = kasaObj.BonusBudgetMaxTL;
            bonusMaxOdemeHavuzOrani = kasaObj.BonusMaxOdemeHavuzOrani;
            kasaBazliDengeAktif = kasaObj.KasaBazliDengeAktif;
            minClusterSize_HavuzBos = kasaObj.MinClusterSize_HavuzBos;
            minClusterSize_HavuzDolu = kasaObj.MinClusterSize_HavuzDolu;
            minClusterSize_HavuzAz = kasaObj.MinClusterSize_HavuzAz;
            havuzAzEsik01 = kasaObj.HavuzAzEsik01;
            havuzDoluEsik01 = kasaObj.HavuzDoluEsik01;
            bonusOtoZorlukAktif = kasaObj.BonusOtoZorlukAktif;
            bonusMinCluster_Easy = kasaObj.BonusMinCluster_Easy;
            bonusMinCluster_Hard = kasaObj.BonusMinCluster_Hard;
        }
    }

    void Start()
    {
        SyncFromAyarClassesIfPresent();
        _oyunBootstrapServisi = new OyunBootstrapServisi();
        _oyunBootstrapServisi.SetBaglam(this);
        _oyunBootstrapServisi.Calistir();
        BahisGorselKilidiniHazirla();
    }

    private void OnValidate()
    {
        inspectorBakiyeTL = Mathf.Max(0, inspectorBakiyeTL);
        InspectorBakiyesiniYansit();
    }

    void IOyunBootstrapBaglami.BootstrapMantiginiCalistir()
    {
        _logServisi = new LogServisi();
        _ekonomiServisi = new EkonomiServisi();
        _uiServisi = new UIServisi();
        _zorlukServisi = new ZorlukServisi();
        _zorlukServisi.SetBaglam(this);
        _oyunUIGuncellemeServisi = new OyunUIGuncellemeServisi();
        _oyunUIGuncellemeServisi.SetBaglam(this);
        _carpanSokEfektServisi = new CarpanSokEfektServisi();
        _bombEfektServisi = new BombEfektServisi();
        _scatterEfektServisi = new ScatterEfektServisi();
        _bombaInisEfektServisi = new BombaInisEfektServisi();
        _scatterEfektServisi.SetBaglam(this);
        _uiServisi.SetUIGuncelleImpl(() => _oyunUIGuncellemeServisi?.RefreshAllUI());
        _uiServisi.SetButonDurumuImpl(acik => _oyunUIGuncellemeServisi?.SetButtonsInteractable(acik));
        _uiServisi.SetShowParaCekPanelImpl(ShowParaCekPanel);
        _uiServisi.SetHideParaCekPanelImpl(HideParaCekPanel);
        _uiServisi.SetShowBakiyeYuklePanelImpl(() => ShowBakiyeYuklePanel());
        _uiServisi.SetHideBakiyeYuklePanelImpl(HideBakiyeYuklePanel);
        _uiServisi.SetCloseMoneyPanelsImpl(CloseMoneyPanels);
        _uiServisi.SetShowBonusBuyConfirmPanelImpl(ShowBonusBuyConfirmPanel);
        _uiServisi.SetHideBonusBuyConfirmPanelImpl(HideBonusBuyConfirmPanel);
        _sahneBaglamaServisi = new SahneBaglamaServisi();
        _uiServisi.SetUIAutoBaglaGerekirseImpl(() => _sahneBaglamaServisi.BindIfNeeded(transform, this));
        _uiServisi.SetResolveMoneyUIRefsIfMissingImpl(() => _sahneBaglamaServisi.BindIfNeeded(transform, this));
        _uiServisi.SetWireParaCekUIImpl(() => _oyunUIGuncellemeServisi?.WireMoneyPanelsIfNeeded());
        _uiServisi.SetWireBakiyeYukleUIImpl(() => _oyunUIGuncellemeServisi?.WireMoneyPanelsIfNeeded());

        _odemeServisi = new OdemeServisi();
        _odemeServisi.SetGetHavuzTL(() => kasa != null ? kasa.odulHavuzuTL : 0L);
        _odemeServisi.SetParaGirisiBolVeEkle(tl => { if (kasa != null) kasa.ParaGirisi_BolVeEkle(tl); });
        _odemeServisi.SetOdemeYapOdulHavuzundan(istenen => kasa != null ? kasa.OdemeYap_OdulHavuzundan(istenen) : 0);
        _odemeServisi.SetGetOdenebilirLimitDynamic(() => SenaryoYoneticisi.I != null && _senaryoOdenebilirKalanTL >= 0 ? _senaryoOdenebilirKalanTL : -1);

        _senaryoServisi = new SenaryoServisi();
        _senaryoServisi.SetSetZorlukImpl(SetZorluk);
        _senaryoServisi.SetBiasMultiplierImpl(BiasMultiplier);
        _senaryoServisi.SetGetScatterChanceImpl(GetScatterChanceFor);
        _senaryoServisi.SetGetScatterEsikImpl(() => scatterEsik);
        _senaryoServisi.SetGetMaxScatterPerSpinImpl(() => maxScatterPerSpin);
        _senaryoServisi.SetGetCarpanUretimOlasiligiImpl(() => carpanUretimOlasiligi);
        _senaryoServisi.SetGetMaxCarpanAdediImpl(() => maxCarpanAdedi);
        _senaryoServisi.SetIsCarpanSadeceBonusImpl(() => carpanSadeceBonus);
        _senaryoServisi.SetIsCarpanUretimiAktifImpl(() => carpanUretimiAktif);
        _senaryoServisi.SetInitBonusBudgetFromHavuzImpl(InitBonusBudgetFromHavuz);
        _senaryoServisi.SetGetBonusRemainingPayableTLImpl(GetBonusRemainingPayableTL);
        _senaryoServisi.SetRecordBonusPaymentImpl(RecordBonusPayment);

        _donusAkisServisi = new DonusAkisServisi();
        _donusAkisServisi.SetBaglam(this);
        _donusAkisServisi.SetRunCoroutine(StartCoroutine);

        _donusServisi = new DonusServisi();
        _donusServisi.SetSpinButonImpl(SpinButonImpl);
        _donusServisi.SetNormalSpinAkisiImpl(() => _donusAkisServisi.NormalSpinAkisi());
        _donusServisi.SetBaslatBonusImpl(BaslatBonus);
        _donusServisi.SetBonusBaslangicAkisiImpl(BonusBaslangicAkisi);
        _donusServisi.SetBonusDongusuImpl(() => _donusAkisServisi.BonusDongusu());
        _donusServisi.SetShowBonusStartMessageImpl(ShowBonusStartMessage);
        _donusServisi.SetShowBonusEndMessageImpl(ShowBonusEndMessage);

        _izgaraServisi = new IzgaraServisi();
        _izgaraServisi.SetGridDimensions(satir, sutun);
        _scatterIndexCache = (tumbleAyarlari != null) ? tumbleAyarlari.ScatterIndex : 7;
        // Sembol listesinde 9+ sembol varsa silah genelde index 8'dedir; 7 ile sayarsak bonus tetiklenmez.
        if (_scatterIndexCache == 7 && sembolSpriteListesi != null && sembolSpriteListesi.Count > 8)
            _scatterIndexCache = 8;
        _izgaraServisi.SetScatterSpriteIndex(_scatterIndexCache);
        _izgaraServisi.SetSembolSpriteListesi(sembolSpriteListesi);
        _izgaraServisi.SetCarpanSembolSprite(carpanSembolSprite);
        _izgaraServisi.SetSlotGridRoot(slotGridRoot);
        _izgaraServisi.SetCalculateWinForRemoved(removed => _tumbleServisi != null ? _tumbleServisi.CalculateWinForRemoved(removed) : 0);
        _izgaraServisi.SetGetBonusAktif(() => bonusAktif);
        _izgaraServisi.SetGetEffectiveFillLimit(limit => bonusMaxOdemeHavuzOrani <= 0f || !bonusAktif ? limit : Mathf.Min(limit, Mathf.Max(0, _bonusMaxOdemeTL - _bonusOdenenTL)));
        _izgaraServisi.SetGetScatterChance(_senaryoServisi.GetScatterChance);
        _izgaraServisi.SetGetMaxScatterPerSpin(_senaryoServisi.GetMaxScatterPerSpin);
        _izgaraServisi.SetBiasMultiplier(_senaryoServisi.BiasMultiplier);
        _izgaraServisi.SetGetHardBias01(() => _hardBias01);
        _izgaraServisi.SetGetPayTableBase(() => tumbleAyarlari != null ? tumbleAyarlari.PayTable_8_9 : null);

        _cokmeAkisServisi = new CokmeAkisServisi();
        _cokmeAkisServisi.SetBaglam(this);
        _tumbleAkisServisi = new TumbleAkisServisi();
        _tumbleAkisServisi.SetBaglam(this);
        _tumbleServisi = new TumbleServisi();
        _tumbleServisi.SetTumbleLoopImpl(onKazanc => _tumbleAkisServisi.TumbleLoop(onKazanc));
        _tumbleServisi.SetGetBonusAktif(() => bonusAktif);
        _tumbleServisi.SetGetBonusRemainingPayableTL(() => _senaryoServisi.GetBonusRemainingPayableTL());
        _tumbleServisi.SetScatterSpriteIndex(_scatterIndexCache);
        _tumbleServisi.SetGetCurrentBet(() => _ekonomiServisi != null ? _ekonomiServisi.Bahis : 0);
        _tumbleServisi.SetCalculateWithPayTable(removed =>
        {
            int ham = tumbleAyarlari != null ? tumbleAyarlari.CalculateWinWithOwnPayTable(removed, grid, satir, sutun, _ekonomiServisi != null ? _ekonomiServisi.Bahis : 0, minClusterSize) : -1;
            return ham < 0 ? -1 : ZorlukKazancCarpaniUygula(ham);
        });
        _tumbleServisi.SetCollapseRefillAndAnimateImpl(CollapseRefillAndAnimate);
        _animasyonServisi = new AnimasyonServisi();
        _animasyonServisi.SetHucreler(hucreler);
        _animasyonServisi.SetCellPos(cellPos);
        TumbleAyarlardanAnimasyonHizlariniUygula();
        _animasyonServisi.SetDurations(dropDuration, dropStagger, dropStartYOffset, popDuration);
        _animasyonServisi.SetGetPopDuration(() => popDuration);
        _animasyonServisi.SetPopParticlePrefab(popParticlePrefab);
        Canvas canvas = GetComponentInParent<Canvas>();
        _animasyonServisi.SetParticleParent(canvas != null ? canvas.transform : transform);
        _animasyonServisi.SetXYToIndex((x, y) => _izgaraServisi != null ? _izgaraServisi.XYToIndex(x, y) : -1);
        _animasyonServisi.SetOnRefreshCarpanTexts(() => _izgaraServisi?.ForceRefreshCarpanTextsFromGrid());
        _korutinServisi = new KorutinServisi();
        _korutinServisi.SetRunner(r => StartCoroutine(r), c => StopCoroutine(c));

        _carpanOverlayServisi = new CarpanOverlayServisi();
        _carpanOverlayServisi.SetCellImages(hucreler);
        _carpanOverlayServisi.SetCarpanSembolSprite(carpanSembolSprite);
        _carpanOverlayServisi.SetOverlaySize(carpanOverlaySize);
        _carpanOverlayServisi.SetOverlayFontSize(carpanOverlayFontSize);
        _carpanOverlayServisi.SetOverlayTextOffset(carpanOverlayTextOffset);
        _carpanOverlayServisi.SetDropStartYOffset(carpanOverlayDropStartYOffset);
        _carpanOverlayServisi.SetDropDuration(carpanOverlayDropDuration);
        _carpanOverlayServisi.SetStartNamedCoroutine((key, coro) => _korutinServisi.StartNamed(key, coro));
        _carpanOverlayServisi.SetStopNamedCoroutine(key => _korutinServisi.StopNamed(key));
        _animasyonServisi.SetGetCarpanOverlays(() =>
        {
            var d = new Dictionary<int, AnimasyonServisi.CarpanOverlayRef>();
            foreach (var kv in _carpanOverlayServisi.AnimasyonIcinOverlayleriAl())
                d[kv.Key] = new AnimasyonServisi.CarpanOverlayRef { rt = kv.Value.rt, tmp = kv.Value.tmp };
            return d;
        });
        _animasyonServisi.SetRunCoroutine(coro => StartCoroutine(coro));
        _animasyonServisi.SetCarpanSembolSprite(carpanSembolSprite);
        _animasyonServisi.SetCarpanHucreTextiAl(idx =>
            carpanHücreTextleri != null && idx >= 0 && idx < carpanHücreTextleri.Length ? carpanHücreTextleri[idx] : null);
        _tumbleServisi.SetAnimatePopImpl(cells => _animasyonServisi.AnimatePop(cells));

        _izgaraServisi.SetFindClustersToRemove(_tumbleServisi.FindClustersToRemove);

        _carpanServisi = new CarpanServisi();
        _carpanServisi.SetIsCarpanUretimiAktif(() => carpanUretimiAktif);
        _carpanServisi.SetIsCarpanSadeceBonus(() => carpanSadeceBonus);
        _carpanServisi.SetGetCarpanUretimOlasiligi(() => carpanUretimOlasiligi);
        _carpanServisi.SetGetMaxCarpanAdedi(() => maxCarpanAdedi);
        _carpanServisi.SetRollCarpanDegeri(RastgeleCarpan);
        _carpanServisi.SetGetSpinKazancHam(() => spinKazancHam);
        _carpanServisi.SetGetBonusRemainingPayableTL(() => _senaryoServisi.GetBonusRemainingPayableTL());

        _carpanYerlestirmeServisi = new CarpanYerlestirmeServisi();
        _carpanYerlestirmeServisi.SetBaglam(this);

        _ekonomiServisi.SetLogServisi(_logServisi);
        _ekonomiServisi.SetParaCekInput(paraCekInput);
        _ekonomiServisi.SetBakiyeYukleInput(bakiyeYukleInput);
        _ekonomiServisi.SetParaCekUyariText(paraCekUyariText);
        _ekonomiServisi.SetBakiyeYukleUyariText(bakiyeYukleUyariText);
        _ekonomiServisi.SetBahisLimits(bahisMin, bahisMax, bahisAdim);
        _ekonomiServisi.SetCanChangeBet(() => !spinCalisiyor && !bonusAktif);
        _ekonomiServisi.SetOnEconomyChanged(() => { _uiServisi?.UI_Guncelle(); _uiServisi?.ButonDurumu(true); });
        _ekonomiServisi.SetGetCurrentMultiplier(() => _carpanServisi.GetCurrentMultiplier());
        _ekonomiServisi.SetCarpanSifirla(UI_CarpanSifirla);
        _ekonomiServisi.SetOnParaCekildi(miktar =>
        {
            SenaryoYoneticisi.I?.LogEkle(SenaryoOlayKaydi.OlayTipi_ParaCekildi, $"Para çekildi: {OyunFormatServisi.FormatTL(miktar)}. Güncel bakiye: {(_ekonomiServisi != null ? OyunFormatServisi.FormatTL(_ekonomiServisi.Bakiye) : "—")}.");
        });
        _ekonomiServisi.SetOnBakiyeYuklemeReddedildi(() =>
        {
            SenaryoYoneticisi.I?.LogEkle(SenaryoOlayKaydi.OlayTipi_BakiyeYuklemeReddedildi, "Bakiye yükleme reddedildi. Kalan yükleme hakkı: 0.");
        });

        _bonusUIServisi = new BonusUIServisi();
        _bonusUIServisi.SetBonusStartPanel(bonusStartPanel);
        _bonusUIServisi.SetBonusBellAudio(bonusBellAudio);
        _bonusUIServisi.SetBonusStartCanvasGroup(bonusStartCanvasGroup);
        _bonusUIServisi.SetBonusStartTMP(bonusStartTMP);
        _bonusUIServisi.SetGetBonusHakKalan(() => bonusHakKalan);
        _bonusUIServisi.SetBonusStartFadeTime(bonusStartFadeTime);
        _bonusUIServisi.SetBonusStartShowTime(bonusStartShowTime);
        _bonusUIServisi.SetBonusEndPanel(bonusEndPanel);
        _bonusUIServisi.SetGetBonusEndCloseRequested(() => bonusEndCloseRequested);
        _bonusUIServisi.SetSetBonusEndCloseRequested(v => bonusEndCloseRequested = v);
        // Bonus bitiş paneli her zaman 5 sn sayıp kapansın (scatter veya satın alma fark etmez)
        _bonusUIServisi.SetGetBonusEndAutoCloseSeconds(() => 5f);
        _bonusUIServisi.SetBonusEndCloseButtonTextUpdater(kalan =>
        {
            var tmp = bonusEndCloseButton != null ? bonusEndCloseButton.GetComponentInChildren<TMP_Text>(true) : null;
            if (tmp != null)
                tmp.text = kalan >= 0 ? $"TAMAM (5sn) {kalan}" : "TAMAM";
        });
        SesKaynaklariniHazirla();
        _bonusUIServisi.SetBonusEndSfx(bonusEndSfxSource, bonusEndApplauseClip);
        _bonusUIServisi.SetBonusEndCanvasGroup(bonusEndCanvasGroup);
        _bonusUIServisi.SetBonusEndTitleTMP(bonusEndTitleTMP);
        _bonusUIServisi.SetBonusEndWinTMP(bonusEndWinTMP);
        _bonusUIServisi.SetFormatTL(OyunFormatServisi.FormatTL);
        _bonusUIServisi.SetBonusEndMusicAudio(bonusEndMusicAudio);

        _hizVeSesServisi = new HizVeSesServisi();
        _hizVeSesServisi.SetGetBonusYavasMod(() => bonusYavasMod);
        _hizVeSesServisi.SetGetDurations(() => (popDuration, fallDuration, betweenStepsDelay, bonusSpinBekleme));
        _hizVeSesServisi.SetSetDurations((p, f, b, w) => { popDuration = p; fallDuration = f; betweenStepsDelay = b; bonusSpinBekleme = w; });
        _hizVeSesServisi.SetGetBonusSpeedOverrides(() => (bonusPopDuration, bonusFallDuration, bonusBetweenStepsDelay, bonusSpinBeklemeOverride));
        _hizVeSesServisi.SetGetUnscaledTime(() => Time.unscaledTime);
        _hizVeSesServisi.SetAudioSource(tumbleSfxSource);

        _bonusUIServisi.SetGetBakiye(() => _ekonomiServisi != null ? _ekonomiServisi.Bakiye : 0);
        _bonusUIServisi.SetGetBonusMaliyeti(() => Mathf.Max(0, _ekonomiServisi != null ? _ekonomiServisi.Bahis : 0) * Mathf.Max(1, bonusSatinAlCarpani));
        _bonusUIServisi.SetGetSpinCalisiyor(() => spinCalisiyor);
        _bonusUIServisi.SetGetBonusAktif(() => bonusAktif);
        _bonusUIServisi.SetShowConfirmPanel(cost =>
        {
            int bakiye = _ekonomiServisi != null ? _ekonomiServisi.Bakiye : 0;
            if (_bonusAyarlari == null)
                _bonusAyarlari = FindFirstObjectByType<BonusAyarlari>(FindObjectsInactive.Include);
            if (_bonusAyarlari != null)
            {
                _bonusAyarlari.Goster(cost, bakiye, () => _bonusUIServisi.OnYes(), () => _bonusUIServisi.OnNo());
                BonusMiktariYazisiniGuncelle(cost, bonusBuyConfirmPanel);
                Debug.Log("[BONUS SATIN AL] Panel BonusAyarlari.Goster ile açıldı.");
                return;
            }
            if (bonusBuyConfirmPanel != null)
            {
                BonusMiktariYazisiniGuncelle(cost, bonusBuyConfirmPanel);
                bonusBuyConfirmPanel.SetActive(true);
                if (bonusBuyConfirmCanvasGroup != null) bonusBuyConfirmCanvasGroup.alpha = 1f;
                Debug.Log("[BONUS SATIN AL] Panel bonusBuyConfirmPanel referansı ile açıldı.");
                return;
            }
            var panelGO = GameObject.Find("BonusBuyConfirmPanel");
            if (panelGO == null) panelGO = GameObject.Find("BonusSatinAlOnayPanel");
            if (panelGO != null)
            {
                BonusMiktariYazisiniGuncelle(cost, panelGO);
                panelGO.SetActive(true);
                Debug.Log("[BONUS SATIN AL] Panel isimle bulundu ve açıldı.");
                return;
            }
            Debug.LogWarning("Bonus satın al onay UI bulunamadı. Sahnede BonusAyarlari bileşenli bir panel veya 'BonusBuyConfirmPanel' adlı GameObject ekleyin. Inspector'da OyunYoneticisi.bonusBuyConfirmPanel atayın.");
        });
        _bonusUIServisi.SetHideConfirmPanel(() =>
        {
            _bonusAyarlari?.Kapat();
            if (bonusBuyConfirmPanel != null) bonusBuyConfirmPanel.SetActive(false);
        });
        _bonusUIServisi.SetOnConfirmed(cost =>
        {
            _sonBonusSatinAlindiMaliyet = cost;
            _odemeServisi?.AddBahisToKasa(cost);
            int prevBakiye = _ekonomiServisi.Bakiye;
            _ekonomiServisi.SubtractBakiyeForBonusBuy(cost);
            _uiServisi?.UI_Guncelle();
            _logServisi?.KayitEkonomi("Bonus Satın Alındı", prevBakiye, _ekonomiServisi.Bakiye, cost, 0, "BONUS_BUY", $"Bonus satın alındı. Maliyet: {OyunFormatServisi.FormatTL(cost)}", cost);
            SenaryoYoneticisi.I?.BonusSatinAlindi();
            _donusServisi?.BaslatBonus();
        });

        _logServisi.SetFormatTL(OyunFormatServisi.FormatTL);
        _logServisi.SetOnSpinStart(() =>
        {
            if (GameManager.I != null && GameManager.I.ActivePlayer != null)
                GameManager.I.ActivePlayer.totalSpins += 1;
        });
        _logServisi.SetOnSpinResult((odenen, bahis) =>
        {
            if (GameManager.I != null && GameManager.I.ActivePlayer != null)
            {
                int net = odenen - bahis;
                GameManager.I.ActivePlayer.totalWon += odenen;
                if (net < 0) GameManager.I.ActivePlayer.totalLost += -net;
                GameManager.I.ActivePlayer.totalNet += net;
            }
        });
        _logServisi.SetOnSpinSettled(() => _uiServisi?.UI_Guncelle());

        _adminAyarUIServisi = new AdminAyarUIServisi();
        Slider zorlukSlider = null;
        foreach (var s in GetComponentsInChildren<Slider>(true))
        {
            if (s == null) continue;
            if (s.gameObject.name != null && s.gameObject.name.ToLower().Contains("zorluk"))
            {
                zorlukSlider = s;
                break;
            }
        }
        if (zorlukSlider != null)
        {
            _adminAyarUIServisi.SetZorlukUI(zorlukSlider, zorlukValueText, v => _senaryoServisi?.SetZorluk(v));
            // Slider'ın mevcut değerini, dinleyiciler geç bağlandığında bile anında uygula.
            _adminAyarUIServisi.ApplyZorluk(zorlukSlider.value);
        }
        if (scatterSliderUI == null)
            scatterSliderUI = GameObject.Find("BonusDusmeSlider")?.GetComponent<Slider>() ?? GameObject.Find("ScatterSlider")?.GetComponent<Slider>();
        _adminAyarUIServisi.SetScatterUI(scatterSliderUI, scatterSliderText, v =>
        {
            _adminManuelScatterKilidi = true;
            // Slider 0-100 ise v=56 = %56; 0-1 ise v=0.56 = %56
            int yuzde;
            if (v > 1f)
            {
                yuzde = Mathf.Clamp(Mathf.RoundToInt(v), 0, 100);
                scatterChanceNormal = yuzde / 100f;
            }
            else
            {
                scatterChanceNormal = Mathf.Clamp01(v);
                yuzde = Mathf.RoundToInt(scatterChanceNormal * 100f);
            }
            scatterChanceBonus = 0f;
            if (yuzde >= 100 || scatterChanceNormal >= 0.99f)
                maxScatterPerSpin = 5;
            else if (scatterChanceNormal > 0.0001f)
                maxScatterPerSpin = Mathf.Max(maxScatterPerSpin, scatterEsik);
            UnityEngine.Debug.Log($"[SCATTER] Slider -> %{yuzde} (scatterChanceNormal={scatterChanceNormal:F2}), maxScatterPerSpin={maxScatterPerSpin}, esik={scatterEsik}");
        });
        // CarpanOlasilikValueText / CarpanMaxAdetValueText sabit etiket olarak kalacak; slider değeri yazılmıyor (valueText null).
        _adminAyarUIServisi.SetCarpanOlasilikUI(carpanOlasilikSlider, carpanOlasilikText, null, v =>
        {
            float yuzde = Mathf.Clamp(v, 0f, 100f);
            carpanUretimOlasiligi = yuzde / 100f;
        });
        _adminAyarUIServisi.SetCarpanMaxAdetUI(carpanMaxAdetSlider, carpanMaxAdetText, null, adet =>
        {
            maxCarpanAdedi = Mathf.Clamp(adet, 0, 5);
        });
        AdminOdemeUIRefsiniBulGerekirse();
        AdminAyarlariniYukle();
        AdminOdemeUIBindingleriniKur();
        AdminOdemeAyarlariOkuVeUygula(true);
        // Tek giriş: AdminPanel varsa slider'ları o bağlar; yoksa servis bağlar (çift bağlama yok).
        if (FindObjectOfType<AdminPanel>() == null)
            _adminAyarUIServisi.BindAllAndRefresh();

        // Inspector'u kalabalik yapmadan, bos kalmis UI alanlarini sahneden otomatik bul.
        // (BakiyeYukle / ParaCek / BonusSatinAl tiklanmiyor sorunu genelde referanslar null kaldiginda olur.)
        _uiServisi?.UIAutoBaglaGerekirse();
        UygulaSenaryo2TamEkranArkaPlan();
        Sahne2TumButonlaraHoverBuyutmeEkle();
        SlotGridArkaPlaniniSeffafYap();

        // Çarpan ayarları panelindeki sabit etiket metinleri (slider ile değişmesin)
        if (carpanOlasilikValueText != null) carpanOlasilikValueText.text = "Çarpan Düşme Şansı (%)";
        if (carpanMaxAdetValueText != null) carpanMaxAdetValueText.text = "Max Kaç Çarpan Düşsün? (0-5)";

        UygulaCarpanAyarlari();
        if (carpanAktifToggle == null)
        {
            var toggles = FindObjectsByType<Toggle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in toggles)
            {
                if (t != null && t.gameObject.name.IndexOf("CarpanAktifToggle", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    carpanAktifToggle = t;
                    break;
                }
            }
        }
        if (carpanAktifToggle != null)
        {
            carpanAktifToggle.onValueChanged.RemoveAllListeners();
            carpanAktifToggle.SetIsOnWithoutNotify(carpanUretimiAktif);
            carpanAktifToggle.onValueChanged.AddListener(aktif =>
            {
                carpanUretimiAktif = aktif;
                if (carpanAyarlari != null)
                    carpanAyarlari.CarpanUretimiAktif = aktif;
                _uiServisi?.UI_Guncelle();
                // Toggle değişince precompute edilen bir sonraki spin cache'i geçersiz olsun.
                OncedenHesaplananSpinOnbelleginiTemizle();
            });
        }

        if (carpanSadeceBonusToggle == null)
        {
            var toggles = FindObjectsByType<Toggle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in toggles)
            {
                if (t == null) continue;
                if (t.gameObject.name.IndexOf("CarpanSadeceBonusToggle", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    carpanSadeceBonusToggle = t;
                    break;
                }
            }
        }
        if (carpanSadeceBonusToggle != null)
        {
            carpanSadeceBonusToggle.onValueChanged.RemoveAllListeners();
            carpanSadeceBonusToggle.SetIsOnWithoutNotify(carpanSadeceBonus);
            carpanSadeceBonusToggle.onValueChanged.AddListener(sadeceBonus =>
            {
                carpanSadeceBonus = sadeceBonus;
                if (carpanAyarlari != null)
                    carpanAyarlari.CarpanSadeceBonus = sadeceBonus;
                _uiServisi?.UI_Guncelle();
                // Toggle değişince precompute edilen bir sonraki spin cache'i geçersiz olsun.
                OncedenHesaplananSpinOnbelleginiTemizle();
            });
        }
        _carpanOverlayServisi?.SetCarpanSembolSprite(carpanSembolSprite);
        _carpanOverlayServisi?.SetOverlaySize(carpanOverlaySize);
        _carpanOverlayServisi?.SetOverlayFontSize(carpanOverlayFontSize);
        _carpanOverlayServisi?.SetOverlayTextOffset(carpanOverlayTextOffset);
        _carpanOverlayServisi?.SetDropStartYOffset(carpanOverlayDropStartYOffset);
        _carpanOverlayServisi?.SetDropDuration(carpanOverlayDropDuration);
        _izgaraServisi?.SetCarpanOverlayFontSize(carpanOverlayFontSize);
        _izgaraServisi?.SetCarpanYaziRengi(carpanYaziRengi);
        _izgaraServisi?.SetCarpanYaziKalin(carpanYaziKalin);
        _izgaraServisi?.SetCarpanYaziDisCizgiRengi(carpanYaziDisCizgiRengi);
        _izgaraServisi?.SetCarpanYaziDisCizgiKalinlik(carpanYaziDisCizgiKalinlik);
        _izgaraServisi?.SetCarpanYaziGolge(carpanYaziGolge);
        _izgaraServisi?.SetCarpanYaziGolgeRengi(carpanYaziGolgeRengi);
        _izgaraServisi?.SetCarpanYaziGolgeOffset(carpanYaziGolgeOffset);
        _izgaraServisi?.SetCarpanGradient(carpanGradientAktif, carpanGradientUst, carpanGradientAlt);
        _izgaraServisi?.SetCarpanCharacterSpacing(carpanCharacterSpacing);
        _izgaraServisi?.SetCarpanUnderlay(carpanUnderlayAktif, carpanUnderlayRengi, carpanUnderlayOffsetX, carpanUnderlayOffsetY, carpanUnderlayDilate, carpanUnderlaySoftness);
        _izgaraServisi?.SetCarpanGlow(carpanGlowAktif, carpanGlowRengi, carpanGlowOuter, carpanGlowInner, carpanGlowPower);

        // BONUS SATIN AL ONAY
        if (bonusBuyYesButton != null)
        {
            bonusBuyYesButton.onClick.RemoveListener(OnBonusBuyYes);
            bonusBuyYesButton.onClick.AddListener(OnBonusBuyYes);
        }
        if (bonusBuyNoButton != null)
        {
            bonusBuyNoButton.onClick.RemoveListener(OnBonusBuyNo);
            bonusBuyNoButton.onClick.AddListener(OnBonusBuyNo);
        }

        // Panel ilk kapalı
        if (bonusBuyConfirmPanel != null)
            bonusBuyConfirmPanel.SetActive(false);

        if (bonusSatinAlButon != null)
        {
            bonusSatinAlButon.onClick.RemoveListener(BonusSatinAl);
            bonusSatinAlButon.onClick.AddListener(BonusSatinAl);
        }

        if (normalOyunMusic != null && !normalOyunMusic.isPlaying)
            normalOyunMusic.Play();

        _izgaraBaslatmaServisi = new IzgaraBaslatmaServisi();
        _izgaraBaslatmaServisi.SetBaglam(this);
        StartCoroutine(InitRoutine());
        if (bonusEndCloseButton != null)
        {
            bonusEndCloseButton.onClick.RemoveAllListeners();
            bonusEndCloseButton.onClick.AddListener(() => bonusEndCloseRequested = true);
        }
        // === BAHİS +/- BUTON BAĞLAMA === (OyunYoneticisi metotları: senaryo paneli + ekonomi güncellenir)
        if (bahisArttirButon != null)
        {
            bahisArttirButon.onClick.RemoveAllListeners();
            bahisArttirButon.onClick.AddListener(BahisArttir);
        }

        if (bahisAzaltButon != null)
        {
            bahisAzaltButon.onClick.RemoveAllListeners();
            bahisAzaltButon.onClick.AddListener(BahisAzalt);
        }
        Debug.Log($"[BAHIS HOOK] ArttirButon={(bahisArttirButon != null)} AzaltButon={(bahisAzaltButon != null)}");
        // Otomatik spin: panel kapalı, dropdown 20/50/100/250, butonlar
        if (otomatikSpinPanel != null)
            otomatikSpinPanel.SetActive(false);
        if (otomatikSpinDropdown != null)
        {
            OnOtomatikSpinDropdownChanged(otomatikSpinDropdown.value);
            otomatikSpinDropdown.onValueChanged.RemoveAllListeners();
            otomatikSpinDropdown.onValueChanged.AddListener(OnOtomatikSpinDropdownChanged);
        }
        if (otomatikSpinButton != null)
            otomatikSpinButton.onClick.AddListener(OnOtomatikSpinButtonClick);
        if (otomatikSpinBaslatButon != null)
            otomatikSpinBaslatButon.onClick.AddListener(OnOtomatikSpinBaslatClick);
        if (otomatikSpinIptalButon != null)
            otomatikSpinIptalButon.onClick.AddListener(OnOtomatikSpinIptalClick);
        if (istatistikButon == null)
            istatistikButon = GameObject.Find("BtnLogScene")?.GetComponent<Button>();
        if (istatistikButon != null)
        {
            istatistikButon.onClick.RemoveAllListeners();
            istatistikButon.onClick.AddListener(IstatistikButonTiklandi);
        }
        Button yoneticiBtn = yoneticiButon != null ? yoneticiButon : GameObject.Find("YoneticiButton")?.GetComponent<Button>();
        if (yoneticiBtn != null)
        {
            yoneticiBtn.onClick.RemoveAllListeners();
            yoneticiBtn.onClick.AddListener(YoneticiButonTiklandi);
        }
        OtomatikSpinKalanTextGuncelle();
        _uiServisi?.ResolveMoneyUIRefsIfMissing();
        if (cevirButon != null && cevirButon.GetComponent<ButonBasimHissi>() == null)
            cevirButon.gameObject.AddComponent<ButonBasimHissi>();
        _uiServisi?.WireParaCekUI();
        _uiServisi?.WireBakiyeYukleUI();
        UygulaAdminSahneButonHoverBuyutme();
        AdminHosgeldinizMetniniAyarla();
        AdminSifirlaButonunuBagla();
        AdminAyarButonlariniBagla();
        StartCoroutine(AdminButonBaglamaGecikmeli());
        UygulaOncelikliButonHoverAyari();
        Sahne3GirisDonButonunuOverlayOlustur();
        StartCoroutine(IlkSpinPrecomputeGecikmeli());
        SpinPolitikasiniYenile();
    }

    IEnumerator AdminButonBaglamaGecikmeli()
    {
        if (SceneManager.GetActiveScene().name != "03_AdminOyunScene")
            yield break;
        // Geç çalışan scriptler listenerları ezebiliyor; birkaç kare sonra tekrar bağla.
        yield return null;
        AdminSifirlaButonunuBagla();
        AdminAyarButonlariniBagla();
        Sahne3GirisDonButonunuOverlayOlustur();
        yield return new WaitForSeconds(0.35f);
        AdminSifirlaButonunuBagla();
        AdminAyarButonlariniBagla();
        Sahne3GirisDonButonunuOverlayOlustur();
    }

    void Sahne3GirisDonButonunuOverlayOlustur()
    {
        if (SceneManager.GetActiveScene().name != "03_AdminOyunScene")
            return;

        // Eski overlay geri dön butonunu tamamen kaldır.
        var eskiOverlayButon = GameObject.Find("Sahne3GirisDonButon_Overlay");
        if (eskiOverlayButon != null)
            Destroy(eskiOverlayButon);
        var eskiOverlayCanvas = GameObject.Find("Sahne3GirisDonOverlayCanvas");
        if (eskiOverlayCanvas != null)
            Destroy(eskiOverlayCanvas);

        // Yeni eklenen gerçek butonu (geridonbtn) bağla.
        var tumButonlar = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < tumButonlar.Length; i++)
        {
            var buton = tumButonlar[i];
            if (buton == null || buton.gameObject == null) continue;
            string ad = (buton.gameObject.name ?? "").ToLowerInvariant();
            bool geriDonButonuMu = ad == "geridonbtn" || ad == "geridonbutton" || ad == "geridonbuton" || (ad.Contains("geri") && ad.Contains("don"));
            if (!geriDonButonuMu) continue;

            buton.onClick.RemoveAllListeners();
            buton.onClick.AddListener(() =>
            {
                const string girisSahneAdi = "01_GirisScene";
                if (GameManager.I != null)
                    GameManager.I.LoadScene(girisSahneAdi);
                else
                    SceneManager.LoadScene(girisSahneAdi, LoadSceneMode.Single);
            });
            Debug.Log($"[ADMIN] Geri dön butonu bağlandı: {buton.gameObject.name}");
            return;
        }

        Debug.LogWarning("[ADMIN] Geri dön butonu bulunamadı (beklenen ad: geridonbtn).");
    }

    void UygulaAdminSahneButonHoverBuyutme()
    {
        if (SceneManager.GetActiveScene().name != "03_AdminOyunScene")
            return;

        var tumButonlar = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var buton in tumButonlar)
        {
            if (buton == null) continue;
            if (ModalUyariPanelButonuMu(buton))
            {
                var eskiHover = buton.GetComponent<ButonHoverBuyut>();
                if (eskiHover != null)
                    Destroy(eskiHover);
                continue;
            }
            var hover = buton.GetComponent<ButonHoverBuyut>();
            if (hover == null)
                hover = buton.gameObject.AddComponent<ButonHoverBuyut>();
            hover.hoverScale = 1.1f;   // %10 büyüt
            hover.gecisSuresi = 0.1f;
        }
    }

    void AdminHosgeldinizMetniniAyarla()
    {
        string sahneAdi = SceneManager.GetActiveScene().name;
        bool adminSahnesi = sahneAdi == "03_AdminOyunScene" || sahneAdi == "06_AdminOyunKopya";
        if (!adminSahnesi)
            return;

        var hosgeldinizGo = GameObject.Find("TxtHosgeldiniz");
        if (hosgeldinizGo == null) return;
        var metin = hosgeldinizGo.GetComponent<TMP_Text>();
        if (metin == null) return;

        // BUG FIX (2026-04-29): 4 katmanlı fallback — modal hangi yola yazsa orada bulur.
        string kullaniciAdi = KullaniciVerileri.KullaniciAdi;
        if (string.IsNullOrWhiteSpace(kullaniciAdi) || kullaniciAdi == "Misafir")
        {
            string ppAd = PlayerPrefs.GetString("KullaniciAdi", "");
            if (!string.IsNullOrWhiteSpace(ppAd))
                kullaniciAdi = ppAd;
        }
        if (string.IsNullOrWhiteSpace(kullaniciAdi) || kullaniciAdi == "Misafir")
        {
            string apAd = GameManager.I?.ActivePlayer?.playerName;
            if (!string.IsNullOrWhiteSpace(apAd))
                kullaniciAdi = apAd;
        }
        if (string.IsNullOrWhiteSpace(kullaniciAdi))
            kullaniciAdi = "Misafir";

        // Statik alanı da senkronla (sahne yenilense bile tutarlı kalsın)
        KullaniciVerileri.KullaniciAdi = kullaniciAdi;

        metin.text = $"Hoşgeldin,\n{kullaniciAdi}";
        Debug.Log($"[HOSGELDIN] Kullanıcı adı: '{kullaniciAdi}'");
    }

    void AdminSifirlaButonunuBagla()
    {
        if (SceneManager.GetActiveScene().name != "03_AdminOyunScene")
            return;

        var tumButonlar = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var buton in tumButonlar)
        {
            if (buton == null || buton.gameObject == null) continue;
            string ad = (buton.gameObject.name ?? "").ToLowerInvariant();
            // "CarpanSifirla" ile karışmasın; sadece ana kullanıcı sıfırlama butonunu hedefle.
            bool anaSifirlaButonu = ad == "sifirla" || ad == "sıfırla" || ad == "reset" || ad == "btnsifirla";
            if (!anaSifirlaButonu)
                continue;

            buton.onClick.RemoveAllListeners();
            buton.onClick.AddListener(AdminSifirlamaOnayPopupGoster);
            Debug.Log($"[ADMIN] Sıfırla butonu fallback bağlandı: {buton.gameObject.name}");
            return;
        }
        Debug.LogWarning("[ADMIN] Sıfırla butonu bulunamadı (beklenen ad: Sifirla / Reset / BtnSifirla).");
    }

    void AdminSifirlamaOnayPopupGoster()
    {
        const string popupAd = "AdminSifirlamaOnayPopup";
        var mevcut = GameObject.Find(popupAd);
        if (mevcut != null)
        {
            mevcut.SetActive(true);
            return;
        }

        var canvasGo = new GameObject(popupAd);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 3000;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();

        var arkaPlan = new GameObject("ArkaPlan");
        arkaPlan.transform.SetParent(canvasGo.transform, false);
        var arkaRt = arkaPlan.AddComponent<RectTransform>();
        arkaRt.anchorMin = Vector2.zero;
        arkaRt.anchorMax = Vector2.one;
        arkaRt.offsetMin = Vector2.zero;
        arkaRt.offsetMax = Vector2.zero;
        var arkaImg = arkaPlan.AddComponent<Image>();
        arkaImg.color = new Color(0f, 0f, 0f, 0.62f);
        arkaImg.raycastTarget = true;

        var panel = new GameObject("Panel");
        panel.transform.SetParent(arkaPlan.transform, false);
        var panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(560f, 260f);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.1f, 0.14f, 0.96f);

        var yaziGo = new GameObject("Mesaj");
        yaziGo.transform.SetParent(panel.transform, false);
        var yaziRt = yaziGo.AddComponent<RectTransform>();
        yaziRt.anchorMin = new Vector2(0.1f, 0.45f);
        yaziRt.anchorMax = new Vector2(0.9f, 0.9f);
        yaziRt.offsetMin = Vector2.zero;
        yaziRt.offsetMax = Vector2.zero;
        var yazi = yaziGo.AddComponent<TextMeshProUGUI>();
        yazi.text = "Tüm kullanıcıları sıfırlamak istediğine emin misin?";
        yazi.fontSize = 30;
        yazi.alignment = TMPro.TextAlignmentOptions.Center;
        yazi.color = Color.white;

        Button ButonOlustur(string ad, Vector2 konum, string metin, Color renk, UnityEngine.Events.UnityAction tik)
        {
            var btnGo = new GameObject(ad);
            btnGo.transform.SetParent(panel.transform, false);
            var rt = btnGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.2f);
            rt.anchorMax = new Vector2(0.5f, 0.2f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = konum;
            rt.sizeDelta = new Vector2(180f, 62f);
            var img = btnGo.AddComponent<Image>();
            img.color = renk;
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(tik);

            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(btnGo.transform, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;
            var txt = txtGo.AddComponent<TextMeshProUGUI>();
            txt.text = metin;
            txt.fontSize = 28;
            txt.alignment = TMPro.TextAlignmentOptions.Center;
            txt.color = Color.white;
            txt.raycastTarget = false;
            return btn;
        }

        ButonOlustur("HayirButon", new Vector2(-110f, -70f), "HAYIR", new Color(0.35f, 0.35f, 0.35f, 1f), () =>
        {
            if (canvasGo != null) canvasGo.SetActive(false);
        });
        ButonOlustur("EvetButon", new Vector2(110f, -70f), "EVET", new Color(0.18f, 0.56f, 0.2f, 1f), () =>
        {
            if (canvasGo != null) canvasGo.SetActive(false);
            AdminTumKullanicilariSifirla();
        });
    }

    void AdminTumKullanicilariSifirla()
    {
        // GameManager singleton sahnede yoksa bile dosyadaki profilleri sıfırlayabilsin.
        int sifirlanan = 0;
        List<PlayerProfile> profiles;
        if (GameManager.I != null && GameManager.I.Profiles != null)
            profiles = GameManager.I.Profiles;
        else
            profiles = GameManager.LoadProfiles();

        for (int i = 0; i < profiles.Count; i++)
        {
            var profile = profiles[i];
            if (profile == null) continue;

            profile.balance = 20000;
            profile.totalDeposited = 0;
            profile.totalWithdrawn = 0;
            profile.totalSessions = 0;
            profile.totalNet = 0;
            profile.totalSpins = 0;
            profile.totalBonusEntries = 0;
            profile.totalWagered = 0;
            profile.totalWon = 0;
            profile.totalLost = 0;
            sifirlanan++;
        }

        GameManager.SaveProfiles(profiles);
        if (GameManager.I != null && GameManager.I.Profiles != null)
        {
            // Singleton varsa runtime listedeki referansı koruyarak güncel kalmasını sağla.
            GameManager.I.Profiles.Clear();
            GameManager.I.Profiles.AddRange(profiles);
        }
        var kasaYoneticisi = FindFirstObjectByType<KasaYoneticisi>(FindObjectsInactive.Include);
        if (kasaYoneticisi != null)
        {
            kasaYoneticisi.SetAnaKasa(0);
            kasaYoneticisi.SetOdulHavuzu(0);
            kasaYoneticisi.UI_Guncelle();
        }
        _uiServisi?.UI_Guncelle();
        SenaryoYoneticisi.I?.UI_Guncelle();
        Debug.Log($"[ADMIN] Sıfırla tamamlandı. Kullanıcı sayısı: {sifirlanan}");
        AdminResetSonucPopupGoster($"{sifirlanan} tane kullanıcı bilgisi sıfırlandı.\nAna kasa ve ödül havuzu sıfırlandı.");
    }

    void AdminResetSonucPopupGoster(string mesaj)
    {
        const string popupAd = "AdminResetSonucPopup";
        var mevcut = GameObject.Find(popupAd);
        if (mevcut != null)
            Destroy(mevcut);

        var canvasGo = new GameObject(popupAd);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 3200;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();

        var arkaPlan = new GameObject("ArkaPlan");
        arkaPlan.transform.SetParent(canvasGo.transform, false);
        var arkaRt = arkaPlan.AddComponent<RectTransform>();
        arkaRt.anchorMin = Vector2.zero;
        arkaRt.anchorMax = Vector2.one;
        arkaRt.offsetMin = Vector2.zero;
        arkaRt.offsetMax = Vector2.zero;
        var arkaImg = arkaPlan.AddComponent<Image>();
        arkaImg.color = new Color(0f, 0f, 0f, 0.62f);
        arkaImg.raycastTarget = true;

        var panel = new GameObject("Panel");
        panel.transform.SetParent(arkaPlan.transform, false);
        var panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(620f, 240f);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.1f, 0.14f, 0.96f);

        var yaziGo = new GameObject("Mesaj");
        yaziGo.transform.SetParent(panel.transform, false);
        var yaziRt = yaziGo.AddComponent<RectTransform>();
        yaziRt.anchorMin = new Vector2(0.1f, 0.34f);
        yaziRt.anchorMax = new Vector2(0.9f, 0.86f);
        yaziRt.offsetMin = Vector2.zero;
        yaziRt.offsetMax = Vector2.zero;
        var yazi = yaziGo.AddComponent<TextMeshProUGUI>();
        yazi.text = mesaj;
        yazi.fontSize = 34;
        yazi.alignment = TMPro.TextAlignmentOptions.Center;
        yazi.color = Color.white;
        yazi.raycastTarget = false;

        var btnGo = new GameObject("TamamButon");
        btnGo.transform.SetParent(panel.transform, false);
        var btnRt = btnGo.AddComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0.18f);
        btnRt.anchorMax = new Vector2(0.5f, 0.18f);
        btnRt.pivot = new Vector2(0.5f, 0.5f);
        btnRt.sizeDelta = new Vector2(190f, 62f);
        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0.18f, 0.56f, 0.2f, 1f);
        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(() =>
        {
            if (canvasGo != null)
                Destroy(canvasGo);
        });

        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(btnGo.transform, false);
        var txtRt = txtGo.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;
        var txt = txtGo.AddComponent<TextMeshProUGUI>();
        txt.text = "TAMAM";
        txt.fontSize = 30;
        txt.alignment = TMPro.TextAlignmentOptions.Center;
        txt.color = Color.white;
        txt.raycastTarget = false;
    }

    void AdminAyarButonlariniBagla()
    {
        string aktifSahne = SceneManager.GetActiveScene().name;
        bool adminSahnesi = aktifSahne == "03_AdminOyunScene" || aktifSahne == "06_AdminOyunKopya";
        if (!adminSahnesi)
            return;

        _adminAyarPanelKok = AdminSettingsPanelKokunuBul();
        // 06_AdminOyunKopya'da AyarlarButton, sahnede PanelKopru.AyarlarButonunaBasildi'ye bağlı;
        // RemoveAllListeners() o bağlamı sileceğinden bu sahnede dokunmuyoruz.
        if (aktifSahne != "06_AdminOyunKopya")
        {
            var ayarlarBtn = GameObject.Find("AyarlarButton")?.GetComponent<Button>();
            if (ayarlarBtn != null)
            {
                ayarlarBtn.onClick.RemoveAllListeners();
                ayarlarBtn.onClick.AddListener(AdminAyarPaneliniAc);
                AdminButonTiklamaIyilestir(ayarlarBtn, null);
            }
        }

        if (_adminAyarPanelKok != null)
        {
            var kapatBtn = AdminPanelAltindaButonBul(_adminAyarPanelKok.transform, "CloseButton");
            if (kapatBtn != null)
            {
                kapatBtn.onClick.RemoveAllListeners();
                kapatBtn.onClick.AddListener(AdminAyarPaneliniKapat);
                AdminButonTiklamaIyilestir(kapatBtn, null);
            }
        }

        Button forceX5 = GameObject.Find("ForceX5")?.GetComponent<Button>();
        if (forceX5 != null)
        {
            forceX5.onClick.RemoveAllListeners();
            int deger = AdminButonMetnindenCarpanDegeriCoz(forceX5, 5);
            forceX5.onClick.AddListener(() => AdminZorlaCarpanSec(deger));
            AdminButonTiklamaIyilestir(forceX5, null);
        }

        Button forceX10 = GameObject.Find("ForceX10")?.GetComponent<Button>();
        if (forceX10 != null)
        {
            forceX10.onClick.RemoveAllListeners();
            int deger = AdminButonMetnindenCarpanDegeriCoz(forceX10, 10);
            forceX10.onClick.AddListener(() => AdminZorlaCarpanSec(deger));
            AdminButonTiklamaIyilestir(forceX10, null);
        }

        Button forceX50 = GameObject.Find("ForceX50")?.GetComponent<Button>();
        if (forceX50 != null)
        {
            forceX50.onClick.RemoveAllListeners();
            int deger = AdminButonMetnindenCarpanDegeriCoz(forceX50, 50);
            forceX50.onClick.AddListener(() => AdminZorlaCarpanSec(deger));
            AdminButonTiklamaIyilestir(forceX50, null);
        }

        Button forceX100 = GameObject.Find("ForceX100")?.GetComponent<Button>();
        if (forceX100 != null)
        {
            forceX100.onClick.RemoveAllListeners();
            int deger = AdminButonMetnindenCarpanDegeriCoz(forceX100, 100);
            forceX100.onClick.AddListener(() => AdminZorlaCarpanSec(deger));
            AdminButonTiklamaIyilestir(forceX100, null);
        }

        Button carpanSifirla = GameObject.Find("CarpanSifirla")?.GetComponent<Button>();
        if (carpanSifirla != null)
        {
            carpanSifirla.onClick.RemoveAllListeners();
            carpanSifirla.onClick.AddListener(() => AdminZorlaCarpanSec(0));
            AdminButonTiklamaIyilestir(carpanSifirla, null);
        }

        var tumButonlar = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int kasalarUygulaBaglanan = 0;
        for (int i = 0; i < tumButonlar.Length; i++)
        {
            var b = tumButonlar[i];
            if (b == null || b.gameObject == null) continue;
            string ad = (b.gameObject.name ?? "").ToLowerInvariant();
            if (ad == "kasalaruygulabutton" || ad == "kasalaruygula" || ad.Contains("kasalaruygula")
                || ad == "adminpaneluygulabutton" || ad.Contains("adminpaneluygula"))
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(AdminKasalarAyarlariniUygula);
                // Uygula butonunda sahnede verilen boyutu koru; zorunlu sizeDelta yazma.
                AdminButonTiklamaIyilestir(b, null);
                kasalarUygulaBaglanan++;
            }
        }
        if (kasalarUygulaBaglanan > 0)
            Debug.Log($"[ADMIN] KasalarUygulaButton bağlandı. Toplam: {kasalarUygulaBaglanan}");
        else
            Debug.LogWarning("[ADMIN] KasalarUygulaButton bulunamadı.");
    }

    static Button AdminPanelAltindaButonBul(Transform kok, string hedefAd)
    {
        if (kok == null || string.IsNullOrEmpty(hedefAd)) return null;
        var tumButonlar = kok.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < tumButonlar.Length; i++)
        {
            var b = tumButonlar[i];
            if (b == null || b.gameObject == null) continue;
            if (string.Equals(b.gameObject.name, hedefAd, StringComparison.OrdinalIgnoreCase))
                return b;
        }
        return null;
    }

    GameObject AdminSettingsPanelKokunuBul()
    {
        var kaydiricilar = UnityEngine.Object.FindObjectsByType<AdminSettingsPanelYanKaydirici>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (kaydiricilar != null && kaydiricilar.Length > 0 && kaydiricilar[0] != null)
            return kaydiricilar[0].gameObject;
        return GameObject.Find("AdminSettingsPanel");
    }

    void AdminAyarPaneliniAc()
    {
        if (_adminAyarPanelKok == null)
            _adminAyarPanelKok = AdminSettingsPanelKokunuBul();
        if (_adminAyarPanelKok == null) return;

        _adminAyarPanelKok.SetActive(true);
        AdminOdemeUIRefsiniBulGerekirse();
        AdminAyarlariniYukle();
        AdminOdemeUIBindingleriniKur();
        AdminOdemeAyarlariOkuVeUygula(false);
        AdminAyarSonucTextiniGarantiEt();
        AdminAyarSonucYaz(string.Empty, true);
        var rt = _adminAyarPanelKok.transform as RectTransform;
        if (rt != null)
            rt.SetAsLastSibling();
        _adminAyarPanelKok.GetComponent<AdminSettingsPanelYanKaydirici>()?.ZorlaTamGenisAc();
        Canvas.ForceUpdateCanvases();
    }

    void AdminAyarPaneliniKapat()
    {
        if (_adminAyarPanelKok == null)
            _adminAyarPanelKok = AdminSettingsPanelKokunuBul();
        if (_adminAyarPanelKok == null) return;
        _adminAyarPanelKok.SetActive(false);
    }

    static int AdminButonMetnindenCarpanDegeriCoz(Button buton, int varsayilan)
    {
        if (buton == null) return varsayilan;
        var text = buton.GetComponentInChildren<TMP_Text>(true);
        string yazi = text != null ? (text.text ?? string.Empty) : string.Empty;
        if (string.IsNullOrWhiteSpace(yazi)) return varsayilan;

        var eslesme = System.Text.RegularExpressions.Regex.Match(yazi, @"x\s*(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!eslesme.Success) return varsayilan;

        if (int.TryParse(eslesme.Groups[1].Value, out int deger) && deger > 0)
            return deger;
        return varsayilan;
    }

    static void AdminButonTiklamaIyilestir(Button buton, Vector2? zorunluBoyut)
    {
        if (buton == null || buton.gameObject == null) return;

        var rt = buton.GetComponent<RectTransform>();
        if (rt != null && zorunluBoyut.HasValue)
            rt.sizeDelta = zorunluBoyut.Value;

        // Aynı panelde üstte kalan saydam görseller tıklamayı yemesin.
        buton.transform.SetAsLastSibling();

        var texts = buton.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
            if (texts[i] != null) texts[i].raycastTarget = false;

        var images = buton.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
            if (images[i] != null && images[i].gameObject != buton.gameObject) images[i].raycastTarget = false;
    }

    void AdminKasalarAyarlariniUygula()
    {
        Debug.Log("[ADMIN] KasalarUygulaButton tıklandı.");

        // Kasa değerlerini asıl KasaYoneticisi yönetiyor.
        var kasa = FindObjectOfType<KasaYoneticisi>();
        if (kasa != null)
        {
            string anaInput = kasa.anaKasaInput != null ? (kasa.anaKasaInput.text ?? "") : "";
            string havuzInput = kasa.odulHavuzuInput != null ? (kasa.odulHavuzuInput.text ?? "") : "";
            Debug.Log($"[ADMIN][KASA] Inputlar -> Ana='{anaInput}' Havuz='{havuzInput}' (önce: anaKasaTL={kasa.anaKasaTL}, odulHavuzuTL={kasa.odulHavuzuTL})");

            kasa.ApplyFromInputs();
            Debug.Log($"[ADMIN][KASA] ApplyFromInputs bitti (sonra: anaKasaTL={kasa.anaKasaTL}, odulHavuzuTL={kasa.odulHavuzuTL})");
        }
        else
        {
            Debug.LogWarning("[ADMIN][KASA] KasaYoneticisi bulunamadı; sadece çarpan senkronu yapılacak.");
        }

        // İstersen kasayla birlikte senkronlu çarpan ayarları da otursun.
        SyncFromAyarClassesIfPresent();
        UygulaCarpanAyarlari();
        AdminOdemeAyarlariOkuVeUygula(true);
        // Ayarlar değişince bir önceki state ile üretilmiş ilk spin cache'i geçersiz olmalı.
        OncedenHesaplananSpinOnbelleginiTemizle();
        bool kayitBasarili = AdminAyarlariniKaydet();
        AdminAyarSonucYaz(kayitBasarili ? "ayarlar kaydedildi" : "Ayarlar uygulandi fakat kaydedilemedi.", kayitBasarili);
        _uiServisi?.UI_Guncelle();
        SenaryoYoneticisi.I?.UI_Guncelle();

        Debug.Log("[ADMIN] KasalarUygulaButton: kasa + çarpan ayarları uygulandı, UI yenilendi.");
    }

    /// <summary>
    /// Para çek / bonus onay içindeki butonlarda hover ölçeği metin hizasını bozmasın diye atlanır.
    /// </summary>
    static bool ModalUyariPanelButonuMu(Button buton)
    {
        if (buton == null)
            return false;
        // Bu proje kurgusunda ParaCekPanel / BakiyeYuklePanel / BonusBuyConfirmPanel gibi modallerin
        // butonları da hover'da büyümeli. O yüzden herhangi bir panel butonunu atlamıyoruz.
        return false;
    }

    void UygulaOncelikliButonHoverAyari()
    {
        HoverAyarla(bonusSatinAlButon, 1.2f, 0.22f);
        HoverAyarla(bakiyeYukleButon, 1.2f, 0.22f);
        HoverAyarla(paraCekButon, 1.2f, 0.22f);
    }

    void HoverAyarla(Button buton, float hoverScale, float gecisSuresi)
    {
        if (buton == null) return;
        var hover = buton.GetComponent<ButonHoverBuyut>();
        if (hover == null)
            hover = buton.gameObject.AddComponent<ButonHoverBuyut>();
        hover.hoverScale = hoverScale;
        hover.gecisSuresi = gecisSuresi;
    }

    void UygulaSenaryo2TamEkranArkaPlan()
    {
        string sahneAdi = SceneManager.GetActiveScene().name;
        if (sahneAdi != "02_SenaryoluOyun" && sahneAdi != "03_AdminOyunScene")
            return;

        Sprite bg = Resources.Load<Sprite>("arkaplan");
        if (bg == null)
            bg = DosyadanSpriteYukle("Resources/arkaplan.png");
        if (bg == null)
            bg = DosyadanSpriteYukle("Gorseller/arkaplan.png");
        if (bg == null) return;

        Canvas canvas = AnaCanvasBul();
        if (canvas == null) return;

        Transform mevcut = canvas.transform.Find("RuntimeOrtakArkaPlan");
        GameObject go = mevcut != null ? mevcut.gameObject : new GameObject("RuntimeOrtakArkaPlan");
        if (go.transform.parent != canvas.transform)
            go.transform.SetParent(canvas.transform, false);

        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.SetAsFirstSibling();

        var img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        img.sprite = bg;
        img.type = Image.Type.Simple;
        img.preserveAspect = false;
        img.color = Color.white;
        img.raycastTarget = false;

        OrtaBolumArkaPlaniniSeffaflastir(bg);
    }

    void OrtaBolumArkaPlaniniSeffaflastir(Sprite bg)
    {
        if (bg == null) return;

        Image[] images = FindObjectsOfType<Image>(true);
        if (images == null || images.Length == 0) return;

        for (int i = 0; i < images.Length; i++)
        {
            Image im = images[i];
            if (im == null || im.sprite == null) continue;
            if (im.gameObject.name == "RuntimeOrtakArkaPlan") continue;
            if (im.sprite != bg) continue;

            RectTransform rt = im.rectTransform;
            if (rt == null) continue;

            float alan = Mathf.Abs(rt.rect.width * rt.rect.height);
            // Orta oyun alanı boyutundaki arka planı hedefle (küçük ikonları etkileme).
            if (alan < 300000f || alan > 900000f) continue;

            Color c = im.color;
            c.a = 0.08f;
            im.color = c;
        }
    }

    void Sahne2TumButonlaraHoverBuyutmeEkle()
    {
        if (SceneManager.GetActiveScene().name != "02_SenaryoluOyun")
            return;

        Button[] butonlar = FindObjectsOfType<Button>(true);
        if (butonlar == null || butonlar.Length == 0) return;

        for (int i = 0; i < butonlar.Length; i++)
        {
            Button b = butonlar[i];
            if (b == null) continue;

            var hover = b.GetComponent<ButonHoverBuyut>();
            if (hover == null)
                hover = b.gameObject.AddComponent<ButonHoverBuyut>();

            hover.hoverScale = 1.4f;
            hover.gecisSuresi = 0.08f;
        }
    }

    void SlotGridArkaPlaniniSeffafYap()
    {
        if (slotGridRoot == null) return;

        // SlotGrid kökünde doğrudan image varsa şeffaflaştır.
        Image kokImage = slotGridRoot.GetComponent<Image>();
        if (kokImage != null)
        {
            Color c = kokImage.color;
            c.a = 0f;
            kokImage.color = c;
        }

        // SlotGrid altındaki arka plan isimli image'ları şeffaflaştır.
        Image[] altlar = slotGridRoot.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < altlar.Length; i++)
        {
            Image im = altlar[i];
            if (im == null || im.gameObject == slotGridRoot) continue;

            string n = (im.gameObject.name ?? "").ToLowerInvariant();
            if (!n.Contains("background") && !n.Contains("arka") && !n.Contains("bg") && !n.Contains("item background"))
                continue;

            Color c = im.color;
            c.a = 0f;
            im.color = c;
        }
    }

    Canvas AnaCanvasBul()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        if (canvases == null || canvases.Length == 0) return null;

        Canvas secilen = null;
        float enSkor = float.NegativeInfinity;
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas c = canvases[i];
            if (c == null || !c.gameObject.activeInHierarchy) continue;
            RectTransform rt = c.transform as RectTransform;
            if (rt == null) continue;

            float alan = Mathf.Abs(rt.rect.width * rt.rect.height);
            float skor = alan + (c.sortingOrder * 100000f);
            if (c.renderMode == RenderMode.ScreenSpaceOverlay) skor += 1000000f;

            if (skor > enSkor)
            {
                enSkor = skor;
                secilen = c;
            }
        }

        return secilen;
    }

    Sprite DosyadanSpriteYukle(string relativeFromAssets)
    {
        string path = Path.Combine(Application.dataPath, relativeFromAssets);
        if (!File.Exists(path)) return null;

        byte[] bytes = File.ReadAllBytes(path);
        if (bytes == null || bytes.Length == 0) return null;

        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(bytes)) return null;

        return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }

    /// <summary>Senaryo modu veya pedagojik SenaryoYoneticisi varken ödeme eğilimi/dağılımı slider'ları kilitlenir.</summary>
    private void OdemeEgilimVeDagilimSliderKilidiniUygula()
    {
        AdminOdemeUIRefsiniBulGerekirse();
        bool kilitle = _senaryoPresetAktif || SenaryoYoneticisi.I != null;
        if (odemeEgilimiSliderUI != null)
            odemeEgilimiSliderUI.interactable = !kilitle;
        if (odemeDagilimiSliderUI != null)
            odemeDagilimiSliderUI.interactable = !kilitle;
    }

    /// <summary>Pedagojik Aşama 1: bahis 300, üst üste 5/0, net kar bandı 3–4× bahis (nihai ödeme 4–5× bahis), force sıfır.</summary>

    private void Update()
    {
        SenaryoPresetUIHazirlaGerekirse();
        SenaryoPedagojikOdemeVeZorlaKilidiGuncelle();

        BahisGorselKilidiniUygula();

        // İstek: Oyun sırasında global tıklama kilidi uygulanmasın.
        if (_durumBazliGlobalTiklamaKilidiAktif)
        {
            _durumBazliGlobalTiklamaKilidiAktif = false;
            UygulaGlobalTiklamaKilidiGorunurlugu();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            _boslukTusuBasiliSpin = true;
            SpinButon();
        }
        if (Input.GetKeyUp(KeyCode.Space))
            _boslukTusuBasiliSpin = false;
        if (_boslukTusuBasiliSpin && !spinCalisiyor && !bonusAktif && _ekonomiServisi != null && _ekonomiServisi.Bakiye >= _ekonomiServisi.Bahis)
            SpinButon();

        if (_senaryo5BombSonrasiPopupBekliyor && !spinCalisiyor && _senaryo5PopupGo == null)
        {
            Debug.Log("[S5][POPUP] Tetikleniyor → Senaryo5PopupCoroutine başlatılıyor");
            _senaryo5BombSonrasiPopupBekliyor = false;
            StartCoroutine(Senaryo5PopupCoroutine());
        }
    }

    private IEnumerator InitRoutine()
    {
        return _izgaraBaslatmaServisi != null ? _izgaraBaslatmaServisi.InitRoutine() : null;
    }

    int IIzgaraBaslatmaBaglami.GetSutun() => sutun;
    int IIzgaraBaslatmaBaglami.GetSatir() => satir;
    List<Sprite> IIzgaraBaslatmaBaglami.GetSembolSpriteListesi() => sembolSpriteListesi;
    IzgaraServisi IIzgaraBaslatmaBaglami.GetIzgaraServisi() => _izgaraServisi;
    TumbleServisi IIzgaraBaslatmaBaglami.GetTumbleServisi() => _tumbleServisi;
    UIServisi IIzgaraBaslatmaBaglami.GetUIServisi() => _uiServisi;
    EkonomiServisi IIzgaraBaslatmaBaglami.GetEkonomiServisi() => _ekonomiServisi;
    CarpanOverlayServisi IIzgaraBaslatmaBaglami.GetCarpanOverlayServisi() => _carpanOverlayServisi;
    AnimasyonServisi IIzgaraBaslatmaBaglami.GetAnimasyonServisi() => _animasyonServisi;
    Image[] IIzgaraBaslatmaBaglami.GetHucreler() => hucreler;
    void IIzgaraBaslatmaBaglami.SetHucreler(Image[] arr) => hucreler = arr;
    Transform IIzgaraBaslatmaBaglami.GetSlotGridRoot() => slotGridRoot;
    void IIzgaraBaslatmaBaglami.SetGrid(int[,] g) => grid = g;
    void IIzgaraBaslatmaBaglami.SetCarpanDegerGrid(int[,] g) => carpanDegerGrid = g;
    void IIzgaraBaslatmaBaglami.SetCarpanDegerByCellIndex(int[] a) => carpanDegerByCellIndex = a;
    void IIzgaraBaslatmaBaglami.SetCellPos(Vector2[] p) => cellPos = p;
    void IIzgaraBaslatmaBaglami.SetCellRT(RectTransform[] r) => cellRT = r;
    void IIzgaraBaslatmaBaglami.SetCarpanHücreTextleri(TextMeshProUGUI[] t) => carpanHücreTextleri = t;
    Sprite IIzgaraBaslatmaBaglami.GetMeyveHucreArkaPlanSprite() => null;

    void IIzgaraBaslatmaBaglami.InspectorBakiyesiOyunaGirinceUygula() { }

    /// <summary>Admin senaryo presetinden bahis değerini doğrudan uygular (spin otomatik başlamaz).</summary>

    /// <summary>Admin senaryo presetinden spin başı maksimum scatter adedini ayarlar.</summary>

    /// <summary>Senaryolu oyun paneli gibi dış okumalar için: mevcut bahis (TL). Admin panel yok; değer config/sahneden gelir.</summary>
    public int GetMevcutBahis() => _ekonomiServisi != null ? _ekonomiServisi.Bahis : 0;

    /// <summary>Senaryolu oyun paneli için: bu spin için ödenebilir üst limit (havuzun %10'u). Her spin sonrası güncellenebilir.</summary>
    public int GetSpinOdenebilirLimit() => _odemeServisi != null ? _odemeServisi.GetSpinOdenebilirLimit() : 0;

    /// <summary>Senaryolu oyunda ödenebilir tutarı sabit TL yap (örn. 100000). null = normale dön (havuz %10).</summary>
    public void SetOdenebilirTutarOverride(int? tl) => _odemeServisi?.SetOdenebilirLimitOverride(tl);

    private const string PP_SENARYO_ODENEBILIR_KALAN = "PP_SENARYO_ODENEBILIR_KALAN_TL";
    private string SenaryoOdenebilirKey()
    {
        string id = GameManager.I?.ActivePlayer?.playerId ?? "";
        return string.IsNullOrEmpty(id) ? "" : (PP_SENARYO_ODENEBILIR_KALAN + "_" + id);
    }

    /// <summary>Senaryo sahnesinde: ödenebilir bütçe 100k ile başlar; ödedikçe azalır, ödemedikçe (bahis eve kalınca) artar.</summary>
    public void SenaryoOdenebilirBütceBaslat(int baslangicTL)
    {
        _senaryoOdenebilirKalanTL = Mathf.Max(0, baslangicTL);
    }

    /// <summary>Kaydedilmiş ödenebilir bütçeyi yükler (kullanıcı bazlı); yoksa veya geçersizse varsayilanTL ile başlatır.</summary>
    public void SenaryoOdenebilirBütceYükleVeyaBaslat(int varsayilanTL)
    {
        string key = SenaryoOdenebilirKey();
        if (!string.IsNullOrEmpty(key) && PlayerPrefs.HasKey(key))
        {
            int kayitli = PlayerPrefs.GetInt(key, -1);
            if (kayitli > 0)
            {
                _senaryoOdenebilirKalanTL = kayitli;
                return;
            }
        }
        _senaryoOdenebilirKalanTL = Mathf.Max(0, varsayilanTL);
    }

    /// <summary>Uygulama kapanırken veya sahne değişince senaryo ödenebilir bütçesini kaydet (kullanıcı bazlı).</summary>
    private void OnApplicationQuit() => SenaryoOdenebilirBütceKaydet();
    private void OnDestroy() => SenaryoOdenebilirBütceKaydet();

    private void SenaryoOdenebilirBütceKaydet()
    {
        if (_senaryoOdenebilirKalanTL >= 0)
        {
            string key = SenaryoOdenebilirKey();
            if (!string.IsNullOrEmpty(key))
            {
                PlayerPrefs.SetInt(key, _senaryoOdenebilirKalanTL);
                PlayerPrefs.Save();
            }
        }
    }

    /// <summary>Spin sonrası çağrılır: ödeme yapıldıysa bütçeden düş, yapılmadıysa bahisi bütçeye ekle (sadece senaryo sahnesinde).</summary>
    public void SenaryoOdenebilirGuncelle(int odenen, int bahis)
    {
        if (SenaryoYoneticisi.I == null || _senaryoOdenebilirKalanTL < 0) return;
        if (odenen > 0)
            _senaryoOdenebilirKalanTL -= odenen;
        else if (bahis > 0)
            _senaryoOdenebilirKalanTL += bahis;
        _senaryoOdenebilirKalanTL = Mathf.Max(0, _senaryoOdenebilirKalanTL);
    }

    // ==========================
    // BUTTON / SPIN
    // ==========================
    
    // YENİ: Spin başında ödenebilir tutarı hesapla
    private int _spinOdenebilirLimit = 0;
    private GameObject _geciciTiklamaKilidiPanel;
    private Coroutine _geciciTiklamaKilidiCoroutine;
    private bool _geciciTiklamaKilidiAktif = false;
    private bool _manuelGlobalTiklamaKilidiAktif = false;
    private bool _durumBazliGlobalTiklamaKilidiAktif = false;
    private bool _bombaPatlamaSonrasiIlkRefillCarpanEngeli = false;
}
