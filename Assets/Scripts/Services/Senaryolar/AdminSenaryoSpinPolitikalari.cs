using UnityEngine;

/// <summary>
/// Admin ödeme preset 1/2/3 için ayrı politika türleri; her preset kendi ödeme bandı ve tolerans kurallarını taşır.
/// </summary>
public sealed class AdminSenaryo1SpinPolitikasi : VarsayilanSpinPolitikasi
{
    public override bool OncedenHesaplanmisSpinOdemeModeliyleDogrula() => true;

    public override void AdminOdemeEfektifBandiniUygula(int bahis, bool beklenenKazanc, ref int efektifMin, ref int efektifMax)
    {
        _ = beklenenKazanc;
        // Net kar hedefi 3x-4x bahis; nihai ödeme = bahis + net => 4x-5x bahis.
        int b = Mathf.Max(1, bahis);
        efektifMin = b * 4;
        efektifMax = b * 5;
    }

    /// <summary>Senaryo 2/3 ile aynı gerekçe: sabit bant içinde kalan ödeme hedef±tolerans yüzünden reddedilmesin (dağılım kaydırıcısı bandı daraltmasın).</summary>
    public override bool OdemeModelindeHedefToleransAtlanmali() => true;

}

public sealed class AdminSenaryo2SpinPolitikasi : VarsayilanSpinPolitikasi
{
    public override bool OncedenHesaplanmisSpinOdemeModeliyleDogrula() => true;

    public override void AdminOdemeEfektifBandiniUygula(int bahis, bool beklenenKazanc, ref int efektifMin, ref int efektifMax)
    {
        int b = Mathf.Max(1, bahis);
        if (beklenenKazanc)
        {
            // Daha geniş bant → dağılım çeşitliliği: net 2x-7x bahis arası
            efektifMin = b * 3;
            efektifMax = b * 8;
        }
        else
        {
            efektifMin = Mathf.Max(0, b - 100);
            efektifMax = Mathf.Max(efektifMin, b - 10);
        }
    }

    public override bool OdemeModelindeHedefToleransAtlanmali() => true;
}

public sealed class AdminSenaryo3SpinPolitikasi : VarsayilanSpinPolitikasi
{
    public override bool OncedenHesaplanmisSpinOdemeModeliyleDogrula() => true;

    public override void AdminOdemeEfektifBandiniUygula(int bahis, bool beklenenKazanc, ref int efektifMin, ref int efektifMax)
    {
        if (beklenenKazanc)
        {
            // Init bant ile hizala: bahis+100..bahis+600 (konstrukte sonucu reddedilmesin)
            efektifMin = bahis + 100;
            efektifMax = bahis + 600;
        }
        else
        {
            // Yüksek kayıp: 0..bahis (net kayıp)
            efektifMin = 0;
            efektifMax = Mathf.Max(0, bahis);
        }
    }

    public override bool OdemeModelindeHedefToleransAtlanmali() => true;
}

// S4: KY→K→BOMB_100x. Band = bahis*2..bahis*5 (kazanç); kayıp = 0..bahis-1.
public sealed class AdminSenaryo4SpinPolitikasi : VarsayilanSpinPolitikasi
{
    public override bool OncedenHesaplanmisSpinOdemeModeliyleDogrula() => true;

    public override void AdminOdemeEfektifBandiniUygula(int bahis, bool beklenenKazanc, ref int efektifMin, ref int efektifMax)
    {
        int b = Mathf.Max(1, bahis);
        if (beklenenKazanc)
        {
            efektifMin = b * 2;
            efektifMax = b * 5;
        }
        else
        {
            efektifMin = 0;
            efektifMax = Mathf.Max(0, b - 1);
        }
    }

    public override bool OdemeModelindeHedefToleransAtlanmali() => true;
}

// S5: K→KY→BOMB_500x. Band = bahis*2..bahis*5 (kazanç); kayıp = 0..bahis-1.
public sealed class AdminSenaryo5SpinPolitikasi : VarsayilanSpinPolitikasi
{
    public override bool OncedenHesaplanmisSpinOdemeModeliyleDogrula() => true;

    public override void AdminOdemeEfektifBandiniUygula(int bahis, bool beklenenKazanc, ref int efektifMin, ref int efektifMax)
    {
        int b = Mathf.Max(1, bahis);
        if (beklenenKazanc)
        {
            efektifMin = b * 2;
            efektifMax = b * 5;
        }
        else
        {
            efektifMin = 0;
            efektifMax = Mathf.Max(0, b - 1);
        }
    }

    public override bool OdemeModelindeHedefToleransAtlanmali() => true;
}
