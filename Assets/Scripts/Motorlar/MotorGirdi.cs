using UnityEngine;

/// <summary>
/// FAZ36: Yeni motor katmanı — read-only girdi paketi.
/// Dispatch, OyunYoneticisi instance field'larından (read-only) bunu kurar; motorlar YALNIZ bunu okur,
/// hiçbir instance state'e/flag'e YAZMAZ (ANAYASA rule 2). Motorlar bu girdiyle SpinSimulasyonKaydi (reçete) üretir.
/// </summary>
public sealed class MotorGirdi
{
    /// <summary>PanelKopru.aktifSenaryo (oyunModu) — dispatch motor seçimi.</summary>
    public string aktifSenaryo;
    /// <summary>Mevcut bahis (TL).</summary>
    public int bahis;
    /// <summary>Paytable + grid sembol bilgisi (read-only kullanım).</summary>
    public TumbleAyarlari paytable;
    public int sutun;
    public int satir;
    /// <summary>Scatter sembol index'i (_scatterIndexCache).</summary>
    public int scatterIdx;
    /// <summary>Ödeme bandı min katı (odemeMinKat). band min TL = bahis × minKat.</summary>
    public float minKat;
    /// <summary>Ödeme bandı maks katı (odemeMaksKat). band max TL = bahis × maksKat.</summary>
    public float maksKat;
}
