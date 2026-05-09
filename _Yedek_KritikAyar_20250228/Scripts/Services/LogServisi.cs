using System;

/// <summary>
/// Log/Stats servisi. Ekonomi kaydı + spin/bonus kaydı (eski DonusKayitServisi). GameManager tablo ve log tek noktadan.
/// </summary>
public class LogServisi
{
    private Func<int, string> _formatTL;
    private Action _onSpinSettled;
    private Action _onSpinStart;
    private Action<int, int> _onSpinResult; // (odenen, bahis) -> GameManager totalWon/totalLost

    public void SetFormatTL(Func<int, string> fn) => _formatTL = fn;
    public void SetOnSpinSettled(Action fn) => _onSpinSettled = fn;
    public void SetOnSpinStart(Action fn) => _onSpinStart = fn;
    public void SetOnSpinResult(Action<int, int> fn) => _onSpinResult = fn;

    /// <summary>Ekonomi kaydı: tablo (RecordEconomyAction) + opsiyonel detay log.</summary>
    public void KayitEkonomi(
        string islem,
        int onceki,
        int sonraki,
        int bahis,
        int kazanc,
        string logType,
        string logMsg,
        int logAmount)
    {
        if (GameManager.I == null) return;
        GameManager.I.RecordEconomyAction(onceki, sonraki, islem, bahis, kazanc);
        if (!string.IsNullOrEmpty(logType))
            GameManager.I.Log(logType, logMsg, logAmount);
    }

    public void RecordSpinStart(int prevBakiye, int bakiye, int bahis, int odenebilirLimit)
    {
        _onSpinStart?.Invoke();
        KayitEkonomi(
            "Spin Başladı", prevBakiye, bakiye, bahis, 0, "SPIN",
            $"Spin başladı. Bahis: {(_formatTL != null ? _formatTL(bahis) : bahis.ToString())} | Ödenebilir: {(_formatTL != null ? _formatTL(odenebilirLimit) : odenebilirLimit.ToString())}",
            bahis);
    }

    public void RecordSpinResult(int prevBakiye, int bakiye, int bahis, int odenen)
    {
        KayitEkonomi(
            "Spin Sonucu", prevBakiye, bakiye, bahis, odenen, "SPIN_RESULT",
            $"Spin bitti. Bahis: {(_formatTL != null ? _formatTL(bahis) : bahis.ToString())} | Ödeme: {(_formatTL != null ? _formatTL(odenen) : odenen.ToString())}",
            odenen);
        _onSpinResult?.Invoke(odenen, bahis);
        _onSpinSettled?.Invoke();
    }

    public void RecordBonusStart() { }

    public void RecordBonusSpin(int prevBakiye, int bakiye, int bahis, int odenen)
    {
        KayitEkonomi(
            "Spin Sonucu", prevBakiye, bakiye, bahis, odenen, "SPIN_RESULT",
            $"Spin bitti. Bahis: {(_formatTL != null ? _formatTL(bahis) : bahis.ToString())} | Ödeme: {(_formatTL != null ? _formatTL(odenen) : odenen.ToString())}",
            odenen);
        _onSpinResult?.Invoke(odenen, bahis);
    }

    public void RecordBonusEnd(int prevBakiye, int bakiye, int gercekOdeme)
    {
        KayitEkonomi(
            "Bonus Ödemesi", prevBakiye, bakiye, 0, gercekOdeme, "BONUS_PAYOUT",
            $"Bonus ödemesi: {(_formatTL != null ? _formatTL(gercekOdeme) : gercekOdeme.ToString())}",
            gercekOdeme);
    }
}
