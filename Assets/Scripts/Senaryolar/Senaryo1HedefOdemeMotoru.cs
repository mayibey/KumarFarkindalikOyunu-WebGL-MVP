using UnityEngine;

/// <summary>
/// Senaryo 1 hedef ödeme: TumbleServisi kuralı toplam sembol sayısı ≥ eşik (komşuluk yok).
/// Ortak paytable seçim ve grid kurulum mantığı HedefOdemeMotorBase'den gelir.
/// </summary>
public class Senaryo1HedefOdemeMotoru : HedefOdemeMotorBase
{
    /// <summary>
    /// Bant [min,max] içinde nihai ödeme hedefi; dağılım %100 üst → max'e yakın; ±%12 jitter ile spin başına çeşitlilik.
    /// (Senaryo1HedefOdemeAkisi'nden taşındı.)
    /// </summary>
    public static int HedefNihaiOdemeSec(int minTl, int maxTl, int odemeDagilimiYuzde)
    {
        int alt = Mathf.Max(0, minTl);
        int ust = Mathf.Max(alt, maxTl);
        if (ust <= alt)
            return alt;
        float tMerkez = Mathf.Clamp01(odemeDagilimiYuzde / 100f);
        float t = Mathf.Clamp01(tMerkez + UnityEngine.Random.Range(-0.12f, 0.12f));
        int hedef = Mathf.RoundToInt(Mathf.Lerp(alt, ust, t));
        return Mathf.Clamp(hedef, alt, ust);
    }
}
