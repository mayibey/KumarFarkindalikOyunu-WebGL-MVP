using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// DonusAkisServisi için state ve servis erişimi arayüzü. OyunYoneticisi implement eder.
/// </summary>
public interface IDonusAkisBaglami
{
    UIServisi UIServisi { get; }
    IzgaraServisi IzgaraServisi { get; }
    OdemeServisi OdemeServisi { get; }
    AnimasyonServisi AnimasyonServisi { get; }
    TumbleServisi TumbleServisi { get; }
    CarpanServisi CarpanServisi { get; }
    EkonomiServisi EkonomiServisi { get; }
    LogServisi DonusKayitServisi { get; }
    SenaryoServisi SenaryoServisi { get; }
    HizVeSesServisi HizVeSesServisi { get; }
    bool SpinCalisiyor { get; set; }
    bool BonusAktif { get; set; }
    int BonusHakKalan { get; set; }
    int BonusKazanc { get; set; }
    int OturumKazanc { get; set; }
    int BonusPendingOdemeTL { get; set; }
    bool SpinKazanciOturumaEklendi { get; set; }
    int SpinKazancHam { get; set; }
    int TumbleToplamKazanc { get; set; }
    int SonSpinKazanci { get; set; }
    int SpinPrevBakiye { get; }
    int SpinBahisTL { get; }
    float BonusSpinBekleme { get; }
    int SonSpinKazancHamGoster { set; }
    int SonSpinCarpanGoster { set; }
    int SonSpinKazancToplamGoster { set; }
    int BonusOturumOdenenToplamTL { get; set; }
    int BonusMaxOdemeTL { get; }
    int BonusOdenenTL { get; }
    bool BonusBudgetAktif { get; }
    int BonusBudgetKalanTL { get; }
    int[,] Grid { get; }
    int Satir { get; }
    int Sutun { get; }
    void UI_CarpanSifirla();
    void CarpanUretVeBirik();
    void CarpanlariDoluGriddeUygula();
    void BaslatBonus();
    IEnumerator ScatterBuyutEfekti();
    IEnumerator ShowBonusEndMessage(int bonusToplamKazanc);
    void SetSpinIconRotate(bool rotate);
    void SetOturumKazancTextActive(bool active);
    void NormalOyunMusicPlay();
    void NormalOyunMusicUnPause();
}

/// <summary>
/// Normal dönüş ve bonus döngüsü akışı. State ve servis erişimi IDonusAkisBaglami ile; alt coroutine'ler Func ile çalıştırılır.
/// </summary>
public class DonusAkisServisi
{
    private IDonusAkisBaglami _ctx;
    private Func<IEnumerator, Coroutine> _runCoroutine;

    public void SetBaglam(IDonusAkisBaglami ctx)
    {
        _ctx = ctx;
    }

    public void SetRunCoroutine(Func<IEnumerator, Coroutine> run)
    {
        _runCoroutine = run;
    }

    public IEnumerator NormalSpinAkisi()
    {
        if (_ctx == null) yield break;

        _ctx.SpinCalisiyor = true;
        _ctx.UIServisi?.ButonDurumu(false);
        _ctx.SetSpinIconRotate(true);

        _ctx.SpinKazancHam = 0;
        _ctx.TumbleToplamKazanc = 0;
        _ctx.SonSpinKazanci = 0;
        _ctx.UI_CarpanSifirla();
        _ctx.SpinKazanciOturumaEklendi = false;

        _ctx.IzgaraServisi?.ResetScatterCountPerSpin();
        _ctx.UIServisi?.UI_Guncelle();

        int odenebilirNormal = _ctx.OdemeServisi != null ? _ctx.OdemeServisi.GetSpinOdenebilirLimit() : int.MaxValue;
        _ctx.IzgaraServisi?.FillRandomAll(odenebilirNormal);

        _ctx.CarpanUretVeBirik();
        _ctx.CarpanlariDoluGriddeUygula();

        _ctx.IzgaraServisi?.RenderAllSprites(true, true);
        if (_runCoroutine != null && _ctx.AnimasyonServisi != null)
            yield return _runCoroutine(_ctx.AnimasyonServisi.AnimateGridDropIn());
        _ctx.UIServisi?.UI_Guncelle();

        if (_runCoroutine != null && _ctx.TumbleServisi != null)
            yield return _runCoroutine(_ctx.TumbleServisi.TumbleLoop(null));

        if (_ctx.BonusAktif && _runCoroutine != null && _ctx.AnimasyonServisi != null)
            yield return _runCoroutine(_ctx.AnimasyonServisi.AnimateCarpanSisme());

        int toplamX = _ctx.CarpanServisi.GetTotalMultiplierForSpin();
        int spinKazanci = _ctx.CarpanServisi.MulClampInt(_ctx.SpinKazancHam, toplamX);
        _ctx.SonSpinKazancHamGoster = _ctx.SpinKazancHam;
        _ctx.SonSpinCarpanGoster = toplamX;
        _ctx.SonSpinKazancToplamGoster = spinKazanci;
        _ctx.SonSpinKazanci = 0;

        int odenen = 0;
        if (spinKazanci > 0)
        {
            if (_ctx.BonusAktif)
            {
                odenen = spinKazanci;
                _ctx.BonusPendingOdemeTL += odenen;
            }
            else
            {
                odenen = _ctx.OdemeServisi != null ? _ctx.OdemeServisi.PayFromHavuz(spinKazanci) : spinKazanci;
                _ctx.EkonomiServisi.AddWinnings(odenen, _ctx.SpinBahisTL);
            }
            _ctx.SonSpinKazanci = odenen;
            _ctx.DonusKayitServisi?.RecordSpinResult(_ctx.SpinPrevBakiye, _ctx.EkonomiServisi.Bakiye, _ctx.SpinBahisTL, odenen);
            if (odenen < spinKazanci)
                Debug.LogWarning($"[KASA] Ödül havuzu yetmedi. İstenen={spinKazanci} Ödenen={odenen}");
        }

        Debug.Log($"[NORMAL] SpinKazanci(istenen)={spinKazanci} Odenen={odenen} | Bakiye={_ctx.EkonomiServisi.Bakiye}");

        if (_ctx.SpinBahisTL > 0 && !_ctx.SpinKazanciOturumaEklendi)
        {
            _ctx.OturumKazanc += odenen;
            _ctx.SpinKazanciOturumaEklendi = true;
        }

        int[,] grid = _ctx.Grid;
        int sc = _ctx.IzgaraServisi != null && grid != null ? _ctx.IzgaraServisi.ScatterSay(grid) : 0;
        int esik = _ctx.SenaryoServisi.GetScatterEsik();
        int scatterIdx = _ctx.IzgaraServisi != null ? _ctx.IzgaraServisi.GetScatterSpriteIndex() : -1;
        Debug.Log($"🧪 BonusKontrol: ScatterSay={sc} / Esik={esik} | Kullanılan scatter index={scatterIdx} (silah bu index ise 4+ gelince bonus tetiklenir)");

        if (sc < esik && grid != null && _ctx.Satir > 0 && _ctx.Sutun > 0)
        {
            var counts = new System.Collections.Generic.Dictionary<int, int>();
            for (int y = 0; y < _ctx.Satir; y++)
                for (int x = 0; x < _ctx.Sutun; x++)
                {
                    int v = grid[x, y];
                    if (v >= 0) { if (!counts.ContainsKey(v)) counts[v] = 0; counts[v]++; }
                }
            var sb = new System.Text.StringBuilder("Grid sembol dağılımı (index->adet): ");
            foreach (var kv in counts)
                sb.Append($"{kv.Key}->{kv.Value} ");
            Debug.Log(sb.ToString() + $"\n-> 4-5 silah görüyorsan, o adette olan index scatter olmalı (şu an scatterIdx={scatterIdx}). TumbleAyarlari.ScatterIndex'i Inspector'dan ayarla.");
        }

        if (sc >= esik)
        {
            Debug.Log("🧪 BonusKontrol: EŞİK GEÇİLDİ -> Bonus başlıyor");
            if (_runCoroutine != null)
                yield return _runCoroutine(_ctx.ScatterBuyutEfekti());
            _ctx.SpinCalisiyor = false;
            _ctx.BaslatBonus();
            yield break;
        }

        _ctx.SpinCalisiyor = false;
        _ctx.UIServisi?.ButonDurumu(true);
        _ctx.SetSpinIconRotate(false);
        _ctx.UIServisi?.UI_Guncelle();
    }

    public IEnumerator BonusDongusu()
    {
        if (_ctx == null) yield break;

        _ctx.SpinCalisiyor = true;
        _ctx.UIServisi?.ButonDurumu(false);

        while (_ctx.BonusHakKalan > 0)
        {
            _ctx.UI_CarpanSifirla();
            _ctx.SpinKazancHam = 0;
            _ctx.TumbleToplamKazanc = 0;
            _ctx.SonSpinKazanci = 0;
            _ctx.SpinKazanciOturumaEklendi = false;
            _ctx.UIServisi?.UI_Guncelle();

            int bonusLimit = _ctx.SenaryoServisi.GetBonusRemainingPayableTL();
            _ctx.IzgaraServisi?.FillRandomAll(bonusLimit);

            _ctx.CarpanUretVeBirik();
            _ctx.CarpanlariDoluGriddeUygula();

            _ctx.IzgaraServisi?.RenderAllSprites(true, true);
            if (_runCoroutine != null && _ctx.AnimasyonServisi != null)
                yield return _runCoroutine(_ctx.AnimasyonServisi.AnimateGridDropIn());
            _ctx.UIServisi?.UI_Guncelle();

            if (_runCoroutine != null && _ctx.TumbleServisi != null)
                yield return _runCoroutine(_ctx.TumbleServisi.TumbleLoop(null));

            if (_runCoroutine != null && _ctx.AnimasyonServisi != null)
                yield return _runCoroutine(_ctx.AnimasyonServisi.AnimateCarpanSisme());

            int toplamX = _ctx.CarpanServisi.GetTotalMultiplierForSpin();
            int teorikToplam = _ctx.CarpanServisi.MulClampInt(_ctx.SpinKazancHam, toplamX);
            int maxOdenebilir = _ctx.SenaryoServisi.GetBonusRemainingPayableTL();
            int spinKazanci = teorikToplam;
            if (maxOdenebilir != int.MaxValue && spinKazanci > maxOdenebilir)
                spinKazanci = maxOdenebilir;

            if (spinKazanci < teorikToplam)
            {
                _ctx.SonSpinKazancHamGoster = spinKazanci;
                _ctx.SonSpinCarpanGoster = 1;
                _ctx.SonSpinKazancToplamGoster = spinKazanci;
            }
            else
            {
                _ctx.SonSpinKazancHamGoster = _ctx.SpinKazancHam;
                _ctx.SonSpinCarpanGoster = toplamX;
                _ctx.SonSpinKazancToplamGoster = spinKazanci;
            }
            _ctx.SonSpinKazanci = 0;

            int odenmekIstenen = spinKazanci;
            if (_ctx.BonusMaxOdemeTL != int.MaxValue)
            {
                int capKalan = Mathf.Max(0, _ctx.BonusMaxOdemeTL - _ctx.BonusOdenenTL);
                odenmekIstenen = Mathf.Clamp(odenmekIstenen, 0, capKalan);
            }
            if (_ctx.BonusBudgetAktif && _ctx.BonusBudgetKalanTL != int.MaxValue)
            {
                odenmekIstenen = Mathf.Clamp(odenmekIstenen, 0, _ctx.BonusBudgetKalanTL);
            }

            int odenen = 0;
            if (odenmekIstenen > 0)
            {
                odenen = odenmekIstenen;
                _ctx.BonusPendingOdemeTL += odenen;
            }

            if (odenen > 0) _ctx.SenaryoServisi?.RecordBonusPayment(odenen);
            _ctx.SonSpinKazanci = odenen;

            _ctx.DonusKayitServisi?.RecordBonusSpin(_ctx.SpinPrevBakiye, _ctx.EkonomiServisi.Bakiye, _ctx.SpinBahisTL, odenen);

            if (odenen > 0)
            {
                _ctx.BonusKazanc += odenen;
                _ctx.OturumKazanc += odenen;
                _ctx.BonusOturumOdenenToplamTL += odenen;
                _ctx.SpinKazanciOturumaEklendi = true;
            }

            _ctx.BonusHakKalan = Mathf.Max(0, _ctx.BonusHakKalan - 1);
            _ctx.UIServisi?.UI_Guncelle();

            Debug.Log($"[BONUS] SpinKazanci={spinKazanci} | BonusToplam={_ctx.BonusKazanc}");

            yield return new WaitForSeconds(0.35f);
            yield return new WaitForSeconds(_ctx.BonusSpinBekleme);
        }

        int bonusToplamKazancSnapshot = _ctx.BonusKazanc;
        if (_runCoroutine != null)
            yield return _runCoroutine(_ctx.ShowBonusEndMessage(bonusToplamKazancSnapshot));

        _ctx.NormalOyunMusicPlay();
        _ctx.EkonomiServisi?.EkonomiSenkronizeEt();
        _ctx.HizVeSesServisi?.RestoreNormalSpeed();
        _ctx.NormalOyunMusicUnPause();

        _ctx.BonusAktif = false;
        _ctx.SetOturumKazancTextActive(false);
        _ctx.SpinCalisiyor = false;
        _ctx.UIServisi?.ButonDurumu(true);
        _ctx.UIServisi?.UI_Guncelle();
    }
}
