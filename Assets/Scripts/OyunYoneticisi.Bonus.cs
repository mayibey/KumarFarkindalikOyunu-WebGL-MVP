using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public partial class OyunYoneticisi
{

    private void BaslatBonus()
    {
        _oncedenHesaplananHazir = false;
        // Normal spinden bonusa geçişte input kilidini bırak
        spinCalisiyor = false;

if (spinIcon != null) spinIcon.SetRotate(false);

        if (normalOyunMusic != null && normalOyunMusic.isPlaying)
            normalOyunMusic.Pause();

        if (bonusAktif) return;

        bonusAktif = true;
        SenaryoYoneticisi.I?.BonusGoruldu();
        SenaryoYoneticisi.I?.SetBonusAktif(true);
        int bonusGirisBakiyesi = _ekonomiServisi != null ? _ekonomiServisi.Bakiye : 0;
        SenaryoYoneticisi.I?.LogBonusGirisi(bonusGirisBakiyesi, _sonBonusSatinAlindiMaliyet > 0);
        if (otomatikSpinKalanText != null)
            otomatikSpinKalanText.gameObject.SetActive(false);
        _spinKazanciOturumaEklendi = false;

        _hizVeSesServisi?.ApplyBonusSpeedIfNeeded();
        int spinNo = SenaryoYoneticisi.I != null ? SenaryoYoneticisi.I.toplamSpin : 0;
        SenaryoYoneticisi.I?.LogEkle(SenaryoOlayKaydi.OlayTipi_HizYavasladi, $"Oyun hızı yavaşlatıldı (bonus başladı). Spin: {spinNo}.");
        oturumKazanc = 0;
        _bonusOturumOdenenToplamTL = 0;
        _bonusPendingOdemeTL = 0;
        _bonusZorlaCarpanBirikenTL = 0;

        _senaryoServisi?.InitBonusBudgetFromHavuz(_odemeServisi != null ? _odemeServisi.GetHavuzTL() : 0L);

        _bonusSatınAlindiSenaryo1 = false;
        _senaryo1BonusAktif = false;
        int bahis = _ekonomiServisi != null ? _ekonomiServisi.Bahis : 0;
        int maliyetVarsayilan = Mathf.Max(0, bahis) * Mathf.Max(1, bonusSatinAlCarpani);
        int maliyet = _sonBonusSatinAlindiMaliyet > 0 ? _sonBonusSatinAlindiMaliyet : maliyetVarsayilan;

        if (SenaryoYoneticisi.I != null && maliyet > 0)
        {
            var asama = SenaryoYoneticisi.I.mevcutAsama;
            int yukleme = SenaryoYoneticisi.I.yuklemeSayisi;
            bool satinAlindi = _sonBonusSatinAlindiMaliyet > 0;
            if (satinAlindi) _sonBonusSatinAlindiMaliyet = 0;

            // Genel kural: Bonus satın alındıysa en fazla maliyet + %10; normal spinden (scatter) tetiklenirse maliyetin %10'unu geçmemeli.
            int cap = 0;
            switch (asama)
            {
                case SenaryoYoneticisi.SenaryoAsama.Asama1_IsindirmaUmut:
                    if (satinAlindi) { _bonusSatınAlindiSenaryo1 = true; cap = (int)(maliyet * 1.10f); }
                    else cap = (int)(maliyet * 0.10f);
                    break;
                case SenaryoYoneticisi.SenaryoAsama.Asama2_KontrolBende:
                    if (satinAlindi) cap = (int)(maliyet * 1.10f);
                    else cap = (int)(maliyet * 0.10f);
                    break;
                case SenaryoYoneticisi.SenaryoAsama.Asama3_AzDahaKayipKovalama:
                    if (satinAlindi) cap = (int)(maliyet * 1.10f);
                    else cap = (int)(maliyet * 0.10f);
                    break;
                case SenaryoYoneticisi.SenaryoAsama.Asama4_BakiyeTukenis:
                    if (satinAlindi) cap = (int)(maliyet * 1.10f);
                    else cap = (int)(maliyet * 0.10f);
                    break;
                case SenaryoYoneticisi.SenaryoAsama.Asama5_BonusZirve:
                    if (yukleme >= 3 && !_ucuncuYuklemeSonrasiIlkBonusUygulandi)
                    {
                        cap = (int)(maliyet * 2.5f);
                        _ucuncuYuklemeSonrasiIlkBonusUygulandi = true;
                        _buBonusZirveBonusuMu = true;
                    }
                    else
                    {
                        if (satinAlindi) cap = (int)(maliyet * 1.10f);
                        else cap = (int)(maliyet * 0.10f);
                    }
                    break;
                case SenaryoYoneticisi.SenaryoAsama.Asama6_GercekKayip:
                    if (satinAlindi) cap = (int)(maliyet * 1.10f);
                    else cap = (int)(maliyet * 0.10f);
                    break;
                case SenaryoYoneticisi.SenaryoAsama.Asama7_Finale:
                    cap = 0;
                    break;
                default:
                    break;
            }
            if (cap >= 0)
            {
                if (cap > 0 && cap < 50) cap = 50;
                _bonusBudgetKalanTL = cap;
                _bonusMaxOdemeTL = cap;
                _senaryo1BonusAktif = true;
            }
        }

        bonusHakKalan = bonusHakBaslangic;
        if (_buBonusZirveBonusuMu && senaryo5_zirveBonusSpinSayisi > 0)
        {
            bonusHakKalan = senaryo5_zirveBonusSpinSayisi;
            OnZirveBonusBasladi?.Invoke();
        }
        bonusKazanc = 0;
        _bonusZorlaCarpanBirikenTL = 0;

        _uiServisi?.ButonDurumu(false);
        _uiServisi?.UI_Guncelle();

        StartCoroutine(_donusServisi.BonusBaslangicAkisi());

    }

    private IEnumerator BonusBaslangicAkisi()
    {
        // Bonus mesajını göster
        yield return StartCoroutine(_donusServisi.ShowBonusStartMessage());

        // Sonra free spin döngüsüne gir
        yield return StartCoroutine(_donusServisi.BonusDongusu());
    }



// ==========================
    // TUMBLE
    // ==========================
    private int GetBonusRemainingPayableTL()
    {
        if (!bonusAktif) return int.MaxValue;

        // Cap: bonus başlangıcında havuz snapshot'ının belirli oranı
        int cap = (_bonusMaxOdemeTL > 0) ? _bonusMaxOdemeTL : int.MaxValue;

        long kalan = (long)cap - (long)bonusKazanc; // bonusKazanc = şu ana kadar ÖDENEN toplam (pending dahil sayılır)
        if (kalan < 0) kalan = 0;

        // Budget aktifse onu da dikkate al
        if (bonusBudgetAktif)
        {
            long bk = _bonusBudgetKalanTL;
            if (bk < kalan) kalan = bk;
        }

        // Senaryo 1 bonusunda (scatter/satın al) sadece hesaplanan cap geçerli; havuz ve ödenebilir tavan uygulanmaz
        if (!_senaryo1BonusAktif)
        {
            long havuzSimdi = _odemeServisi != null ? _odemeServisi.GetHavuzTL() : long.MaxValue;
            if (havuzSimdi < long.MaxValue)
            {
                long poolCap = (long)Mathf.Floor((float)havuzSimdi * 0.10f);
                if (poolCap < kalan) kalan = poolCap;
            }
            if (SenaryoYoneticisi.I != null && _senaryoOdenebilirKalanTL >= 0 && _senaryoOdenebilirKalanTL < kalan)
                kalan = _senaryoOdenebilirKalanTL;
        }

        if (kalan > int.MaxValue) return int.MaxValue;
        return (int)kalan;
    }


    /// <summary>SenaryoServisi delegasyonu için: bonus bütçe/cap alanlarını havuz değerine göre başlatır.</summary>
    private void InitBonusBudgetFromHavuz(long odulHavuzuTL)
    {
        if (bonusBudgetAktif)
        {
            long havuz = odulHavuzuTL;
            int hedef = Mathf.RoundToInt((float)havuz * bonusBudgetHavuzOran);
            hedef = Mathf.Clamp(hedef, bonusBudgetMinTL, bonusBudgetMaxTL);
            if (hedef > havuz) hedef = (int)Mathf.Clamp((float)havuz, 0, int.MaxValue);
            _bonusBudgetKalanTL = hedef;
            Debug.Log($"[BONUS BUDGET] Bonus başı bütçe: {_bonusBudgetKalanTL} TL (Havuz={havuz})");
        }
        else
            _bonusBudgetKalanTL = int.MaxValue;

        _bonusBaslangicHavuzTL = odulHavuzuTL;
        if (bonusMaxOdemeHavuzOrani <= 0f)
            _bonusMaxOdemeTL = int.MaxValue;
        else
        {
            float cap = (float)_bonusBaslangicHavuzTL * bonusMaxOdemeHavuzOrani;
            _bonusMaxOdemeTL = cap > int.MaxValue ? int.MaxValue : Mathf.Max(0, Mathf.RoundToInt(cap));
        }
        _bonusOdenenTL = 0;
        Debug.Log($"[BONUS CAP] HavuzSnapshot={_bonusBaslangicHavuzTL} TL | CapOran={bonusMaxOdemeHavuzOrani} | CapTL={_bonusMaxOdemeTL}");
    }


    /// <summary>SenaryoServisi delegasyonu için: ödenen tutarı kaydeder (_bonusOdenenTL ve _bonusBudgetKalanTL günceller).</summary>
    private void RecordBonusPayment(int odenenTL)
    {
        if (odenenTL > 0) _bonusOdenenTL = Mathf.Clamp(_bonusOdenenTL + odenenTL, 0, int.MaxValue);
        if (bonusBudgetAktif && _bonusBudgetKalanTL != int.MaxValue)
        {
            _bonusBudgetKalanTL -= odenenTL;
            if (_bonusBudgetKalanTL < 0) _bonusBudgetKalanTL = 0;
        }

        // Invariant: Bonus toplam ödemesi tanımlı tavanı aşmamalı.
        if (_bonusMaxOdemeTL != int.MaxValue && _bonusOdenenTL > _bonusMaxOdemeTL)
        {
            var senaryo = SenaryoYoneticisi.I;
            int spinNo = senaryo != null ? senaryo.toplamSpin : -1;
            string asamaAdi = senaryo != null ? senaryo.GetAsamaAdi() : "Bilinmiyor";
            Debug.LogError($"[SentetikOyuncu][İHLAL] Bonus toplam ödemesi tavanı aştı. OdenenToplam={_bonusOdenenTL} TL, Cap={_bonusMaxOdemeTL} TL, SpinNo={spinNo}, Asama={asamaAdi}");
        }
    }


    private IEnumerator CollapseRefillAndAnimate()
    {
        return _cokmeAkisServisi != null ? _cokmeAkisServisi.CokmeDoldurVeCanlandir() : null;
    }
}