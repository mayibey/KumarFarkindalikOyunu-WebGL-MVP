using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CarpanYerlestirmeServisi için context arayüzü. OyunYoneticisi implement eder.
/// </summary>
public interface ICarpanYerlestirmeBaglami
{
    int GetSutun();
    int GetSatir();
    int[,] GetGrid();
    int[,] GetCarpanDegerGrid();
    int[] GetCarpanDegerByCellIndex();
    int GetCarpanSembol();
    int GetScatterIndexCache();
    CarpanServisi GetCarpanServisi();
    IzgaraServisi GetIzgaraServisi();
}

/// <summary>
/// Çarpan state ve hesap mantığı. Overlay/spawn/animasyon OyunYoneticisi'nde kalır.
/// </summary>
public class CarpanServisi
{
    private long _spinCarpanCarpim;
    private readonly List<int> _spinCarpanDegerleri = new List<int>();
    private int _carpanKalanBuSpin;
    private readonly List<int> _pendingCarpanDusurecek = new List<int>();
    private int _forceCarpan;

    private Func<bool> _isCarpanUretimiAktif;
    private Func<bool> _isCarpanSadeceBonus;
    private Func<float> _getCarpanUretimOlasiligi;
    private Func<int> _getMaxCarpanAdedi;
    private Func<int> _rollCarpanDegeri;
    private Func<int> _getSpinKazancHam;
    private Func<int> _getBonusRemainingPayableTL;

    public void SetIsCarpanUretimiAktif(Func<bool> fn) => _isCarpanUretimiAktif = fn;
    public void SetIsCarpanSadeceBonus(Func<bool> fn) => _isCarpanSadeceBonus = fn;
    public void SetGetCarpanUretimOlasiligi(Func<float> fn) => _getCarpanUretimOlasiligi = fn;
    public void SetGetMaxCarpanAdedi(Func<int> fn) => _getMaxCarpanAdedi = fn;
    public void SetRollCarpanDegeri(Func<int> fn) => _rollCarpanDegeri = fn;
    public void SetGetSpinKazancHam(Func<int> fn) => _getSpinKazancHam = fn;
    public void SetGetBonusRemainingPayableTL(Func<int> fn) => _getBonusRemainingPayableTL = fn;
    public void SetForceCarpan(int value) => _forceCarpan = Mathf.Max(0, value);

    public void ResetForNewSpin(int maxCarpanAdedi)
    {
        _spinCarpanCarpim = 0;
        _spinCarpanDegerleri.Clear();
        _pendingCarpanDusurecek.Clear();
        _carpanKalanBuSpin = Mathf.Max(0, maxCarpanAdedi);
    }

    public long GetCurrentMultiplier()
    {
        if (_spinCarpanCarpim < 1) return 0;
        return _spinCarpanCarpim;
    }

    public int GetCurrentMultiplierInt()
    {
        long m = GetCurrentMultiplier();
        if (m > int.MaxValue) return int.MaxValue;
        return (int)m;
    }

    public int GetTotalMultiplierForSpin()
    {
        int x = GetCurrentMultiplierInt();
        return x < 1 ? 1 : x;
    }

    public int MulClampInt(int value, long multiplier)
    {
        long v = (long)value * multiplier;
        if (v > int.MaxValue) return int.MaxValue;
        if (v < int.MinValue) return int.MinValue;
        return (int)v;
    }

    public int ApplyMultiplierToWin(int hamWin)
    {
        return MulClampInt(hamWin, GetCurrentMultiplier());
    }

    public bool TryScheduleCarpanDrop(bool bonusAktif)
    {
        if (_carpanKalanBuSpin <= 0) return false;

        _pendingCarpanDusurecek.Clear();
        if (_carpanKalanBuSpin <= 0) return false;

        if (_isCarpanUretimiAktif == null || !_isCarpanUretimiAktif()) return false;
        if (_isCarpanSadeceBonus != null && _isCarpanSadeceBonus() && !bonusAktif) return false;

        int force = _forceCarpan;
        _forceCarpan = 0;

        int adet;
        if (force > 0)
            adet = 1;
        else
        {
            float olasilik = _getCarpanUretimOlasiligi != null ? _getCarpanUretimOlasiligi() : 0f;
            if (olasilik <= 0f) return false;
            if (UnityEngine.Random.value > olasilik) return false;
            int maxAdet = _getMaxCarpanAdedi != null ? _getMaxCarpanAdedi() : 0;
            adet = UnityEngine.Random.Range(1, Mathf.Min(maxAdet, _carpanKalanBuSpin) + 1);
        }

        int spinHam = _getSpinKazancHam != null ? _getSpinKazancHam() : 0;
        int kalanOdenebilir = _getBonusRemainingPayableTL != null ? _getBonusRemainingPayableTL() : int.MaxValue;

        for (int i = 0; i < adet; i++)
        {
            int carpan = (force > 0) ? force : (_rollCarpanDegeri != null ? _rollCarpanDegeri() : 0);
            if (bonusAktif && kalanOdenebilir >= 0)
            {
                if (kalanOdenebilir <= 0) continue;
                long yeniM = (long)GetCurrentMultiplierInt() + (long)carpan;
                if (yeniM < 1) yeniM = 1;
                long proj = (long)spinHam * yeniM;
                if (proj > (long)kalanOdenebilir) continue;
            }
            if (carpan <= 0) continue;
            _pendingCarpanDusurecek.Add(carpan);
        }

        return _pendingCarpanDusurecek.Count > 0;
    }

    public IReadOnlyList<int> GetPendingDrops() => _pendingCarpanDusurecek;

    public void ClearPendingDrops() => _pendingCarpanDusurecek.Clear();

    public void RecordPlacedCarpanlar(IReadOnlyList<int> placed)
    {
        if (placed == null) return;
        for (int i = 0; i < placed.Count; i++)
        {
            int c = placed[i];
            _spinCarpanDegerleri.Add(c);
            _spinCarpanCarpim += c;
            if (_spinCarpanCarpim > long.MaxValue) _spinCarpanCarpim = long.MaxValue;
        }
        _carpanKalanBuSpin -= placed.Count;
        if (_carpanKalanBuSpin < 0) _carpanKalanBuSpin = 0;
        for (int i = 0; i < placed.Count && _pendingCarpanDusurecek.Count > 0; i++)
            _pendingCarpanDusurecek.RemoveAt(0);
    }

    public int GetCarpanKalanBuSpin() => _carpanKalanBuSpin;
}

/// <summary>
/// Çarpanları dolu grid üzerinde meyve hücrelerine yerleştirme (CarpanlariDoluGriddeUygula mantığı).
/// </summary>
public class CarpanYerlestirmeServisi
{
    private ICarpanYerlestirmeBaglami _ctx;

    public void SetBaglam(ICarpanYerlestirmeBaglami ctx)
    {
        _ctx = ctx;
    }

    public void CarpanlariDoluGriddeUygula()
    {
        if (_ctx == null) return;
        var carpanServisi = _ctx.GetCarpanServisi();
        if (carpanServisi == null) return;

        var pending = carpanServisi.GetPendingDrops();
        if (pending == null || pending.Count == 0) return;

        int[,] grid = _ctx.GetGrid();
        int[,] carpanDegerGrid = _ctx.GetCarpanDegerGrid();
        if (grid == null || carpanDegerGrid == null) return;

        int sutun = _ctx.GetSutun();
        int satir = _ctx.GetSatir();
        int CARPAN_SEMBOL = _ctx.GetCarpanSembol();
        int scatterIndex = _ctx.GetScatterIndexCache();
        var izgaraServisi = _ctx.GetIzgaraServisi();
        int[] carpanDegerByCellIndex = _ctx.GetCarpanDegerByCellIndex();

        List<Vector2Int> adaylar = new List<Vector2Int>();
        for (int y = 0; y < satir; y++)
        {
            for (int x = 0; x < sutun; x++)
            {
                if (grid[x, y] == CARPAN_SEMBOL) continue;
                if (grid[x, y] == scatterIndex) continue;
                if (grid[x, y] < 0) continue;
                adaylar.Add(new Vector2Int(x, y));
            }
        }

        if (adaylar.Count == 0) return;

        int carpanKalan = carpanServisi.GetCarpanKalanBuSpin();
        int placeCount = Mathf.Min(pending.Count, adaylar.Count, carpanKalan);
        var placed = new List<int>();

        for (int i = 0; i < placeCount; i++)
        {
            int pick = UnityEngine.Random.Range(0, adaylar.Count);
            Vector2Int p = adaylar[pick];
            adaylar.RemoveAt(pick);

            int carpan = pending[i];
            if (carpan <= 0) continue;

            grid[p.x, p.y] = CARPAN_SEMBOL;
            carpanDegerGrid[p.x, p.y] = carpan;
            int cellIdx = izgaraServisi != null ? izgaraServisi.XYToIndex(p.x, p.y) : -1;
            if (carpanDegerByCellIndex != null && cellIdx >= 0 && cellIdx < carpanDegerByCellIndex.Length)
                carpanDegerByCellIndex[cellIdx] = carpan;
            placed.Add(carpan);
        }

        if (placed.Count > 0)
            carpanServisi.RecordPlacedCarpanlar(placed);

        izgaraServisi?.EnsureCarpanCellTexts();
        izgaraServisi?.RenderAllSprites(setAlphaOne: true, resetScale: false);
    }
}
