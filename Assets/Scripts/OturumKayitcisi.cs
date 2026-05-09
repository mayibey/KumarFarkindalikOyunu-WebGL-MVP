using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class OturumOlayi
{
    public int    SpinNo;
    public string OlayTipi;
    public string Detay;
    public float  Zaman;
}

public class OturumOzeti
{
    public int   ToplamSpin;
    public float BaslangicBakiyesi;
    public float BitisBakiyesi;
    public readonly List<OturumOlayi> SenaryoGecisleri  = new List<OturumOlayi>();
    public readonly List<OturumOlayi> NearMissler        = new List<OturumOlayi>();
    public readonly List<OturumOlayi> ArdisikKayiplar    = new List<OturumOlayi>();
    public readonly List<OturumOlayi> ManuelMudahaleler  = new List<OturumOlayi>();
}

public static class OturumKayitcisi
{
    public const string OlayTipi_Spin          = "spin_sonucu";
    public const string OlayTipi_SenaryoGecisi = "senaryo_gecisi";
    public const string OlayTipi_NearMiss      = "near_miss";
    public const string OlayTipi_ArdisikKayip  = "ardisik_kayip_kirintisi";
    public const string OlayTipi_CarpanZorla   = "carpan_zorla";
    public const string OlayTipi_BonusManuel   = "bonus_manuel";

    public static readonly List<OturumOlayi> Olaylar = new List<OturumOlayi>();
    public static int   ToplamSpin         = 0;
    public static float BaslangicBakiyesi  = 0f;
    public static float BitisBakiyesi      = 0f;

    private static int _ardisakKayipSayaci = 0;

    public static void SifirlaOturum()
    {
        Olaylar.Clear();
        ToplamSpin        = 0;
        BaslangicBakiyesi = 0f;
        BitisBakiyesi     = 0f;
        _ardisakKayipSayaci = 0;
    }

    public static void EkleEvent(string tip, string detay, int spinNo = -1)
    {
        Olaylar.Add(new OturumOlayi
        {
            SpinNo   = spinNo < 0 ? ToplamSpin : spinNo,
            OlayTipi = tip,
            Detay    = detay,
            Zaman    = Time.time
        });
    }

    public static void SpinKaydet(int spinNo, int bahis, int kazanc, float bakiye)
    {
        ToplamSpin    = spinNo;
        BitisBakiyesi = bakiye;

        if (BaslangicBakiyesi <= 0f && bakiye > 0f)
            BaslangicBakiyesi = bakiye + bahis - kazanc;

        EkleEvent(OlayTipi_Spin,
            $"bahis={bahis} kazanc={kazanc} bakiye={bakiye:F0}", spinNo);

        if (kazanc < bahis)
        {
            _ardisakKayipSayaci++;
        }
        else
        {
            if (_ardisakKayipSayaci >= 3)
                EkleEvent(OlayTipi_ArdisikKayip,
                    $"{_ardisakKayipSayaci} kayıp sonrası kırıntı (kazanç={kazanc} bahis={bahis})",
                    spinNo);
            _ardisakKayipSayaci = 0;
        }
    }

    public static OturumOzeti OlaylariOzetleKategorize()
    {
        var oz = new OturumOzeti
        {
            ToplamSpin        = ToplamSpin,
            BaslangicBakiyesi = BaslangicBakiyesi,
            BitisBakiyesi     = BitisBakiyesi
        };
        foreach (var o in Olaylar)
        {
            switch (o.OlayTipi)
            {
                case OlayTipi_SenaryoGecisi: oz.SenaryoGecisleri.Add(o);  break;
                case OlayTipi_NearMiss:      oz.NearMissler.Add(o);       break;
                case OlayTipi_ArdisikKayip:  oz.ArdisikKayiplar.Add(o);  break;
                case OlayTipi_CarpanZorla:   oz.ManuelMudahaleler.Add(o); break;
                case OlayTipi_BonusManuel:   oz.ManuelMudahaleler.Add(o); break;
            }
        }
        return oz;
    }
}
