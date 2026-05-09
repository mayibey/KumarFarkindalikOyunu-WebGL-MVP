using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TumbleAkisServisi için context arayüzü. OyunYoneticisi implement eder.
/// </summary>
public interface ITumbleAkisBaglami
{
    int GetMinClusterSize();
    bool GetBonusAktif();
    int GetBonusRemainingPayableTL();
    int GetCurrentMultiplierInt();
    long GetCurrentMultiplier();
    int GetSpinKazancHam();
    void AddSpinKazancHam(int delta);
    int CalculateWinForRemoved(List<Vector2Int> removed);
    List<Vector2Int> FindClustersToRemove(int minSize);
    void CarpanUretVeBirik();
    void AddTumbleToplamKazanc(int delta);
    void SetSonSpinKazancHamGoster(int value);
    void SetSonSpinCarpanGoster(int value);
    void SetSonSpinKazancToplamGoster(int value);
    void SetSonSpinKazanci(int value);
    int MulClampInt(int ham, long multiplier);
    void UI_Guncelle();
    void PlayTumbleSfx();
    void ClearGridCells(List<Vector2Int> toRemove);
    IEnumerator AnimateCarpanSisme();
    IEnumerator AnimatePop(List<Vector2Int> cells);
    IEnumerator CollapseRefillAndAnimate();
    float GetBetweenStepsDelay();
    Coroutine RunCoroutine(IEnumerator enumerator);
}

/// <summary>
/// Tumble döngüsü: guard/limit, tur sayacı, SFX throttle, UI/grid refresh sırası korunur.
/// Grid değişimleri ve animasyon context üzerinden (IzgaraServisi/TumbleServisi/AnimasyonServisi).
/// </summary>
public class TumbleAkisServisi
{
    private ITumbleAkisBaglami _ctx;

    public void SetBaglam(ITumbleAkisBaglami ctx)
    {
        _ctx = ctx;
    }

    public IEnumerator TumbleLoop(Action<int> onKazanc)
    {
        if (_ctx == null) yield break;

        int turSayaci = 0;
        while (true)
        {
            if (turSayaci >= OyunKorumaServisi.MAX_TUMBLE_TUR)
            {
                Debug.Log($"[TUMBLE] Maksimum {OyunKorumaServisi.MAX_TUMBLE_TUR} tur limitine ulaşıldı, döngü sonlandırıldı.");
                break;
            }

            int minClusterSize = _ctx.GetMinClusterSize();
            List<Vector2Int> toRemove = _ctx.FindClustersToRemove(minClusterSize);
            if (toRemove == null || toRemove.Count == 0) break;

            if (_ctx.GetBonusAktif())
            {
                int kalanOdenebilir = _ctx.GetBonusRemainingPayableTL();
                int m = _ctx.GetCurrentMultiplierInt();
                int turHam = _ctx.CalculateWinForRemoved(toRemove);
                long projeksiyon = ((long)_ctx.GetSpinKazancHam() + (long)turHam) * (long)m;
                if (kalanOdenebilir <= 0 || projeksiyon > (long)kalanOdenebilir)
                    break;
            }

            _ctx.CarpanUretVeBirik();
            int turKazanci = _ctx.CalculateWinForRemoved(toRemove);
            if (turKazanci > 0)
            {
                _ctx.AddSpinKazancHam(turKazanci);
                _ctx.AddTumbleToplamKazanc(turKazanci);

                Coroutine carpanCoro = _ctx.RunCoroutine(_ctx.AnimateCarpanSisme());
                if (carpanCoro != null) yield return carpanCoro;

                _ctx.SetSonSpinKazancHamGoster(_ctx.GetSpinKazancHam());
                _ctx.SetSonSpinCarpanGoster(1);
                _ctx.SetSonSpinKazancToplamGoster(_ctx.GetSpinKazancHam());
                _ctx.SetSonSpinKazanci(_ctx.MulClampInt(_ctx.GetSpinKazancHam(), _ctx.GetCurrentMultiplier()));

                onKazanc?.Invoke(turKazanci);
                _ctx.UI_Guncelle();
            }

            _ctx.PlayTumbleSfx();

            Coroutine popCoro = _ctx.RunCoroutine(_ctx.AnimatePop(toRemove));
            if (popCoro != null) yield return popCoro;

            yield return new WaitForSeconds(0.30f);

            _ctx.ClearGridCells(toRemove);

            Coroutine collapseCoro = _ctx.RunCoroutine(_ctx.CollapseRefillAndAnimate());
            if (collapseCoro != null) yield return collapseCoro;

            yield return new WaitForSeconds(0.15f);

            _ctx.UI_Guncelle();
            turSayaci++;
            yield return new WaitForSeconds(_ctx.GetBetweenStepsDelay());
        }
    }
}
