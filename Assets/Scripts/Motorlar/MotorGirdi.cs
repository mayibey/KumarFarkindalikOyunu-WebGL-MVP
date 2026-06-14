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

    // FAZ36 İŞ A: Çarpan ayarları (CarpanServisi read-only okur). Panel slider'ları → OyunYoneticisi instance field'ları.
    /// <summary>carpanUretimiAktif — doğal çarpan üretimi açık/kapalı (slider %>0 ile set, FAZ36.1). carpanOdemeAcik AYRI alandır.</summary>
    public bool carpanAktif;
    /// <summary>carpanUretimOlasiligi (0-1) — spin başına çarpan düşme olasılığı (slider).</summary>
    public float carpanOlasilik;
    /// <summary>maxCarpanAdedi — tek spinde max çarpan adedi.</summary>
    public int maxCarpanAdedi;

    // FAZ36 İŞ C: Uygula epoch (PanelKopru.uygulamaEpoch). RitimMotoru (Tutma) değişince içsel sayacı sıfırlar.
    public int uygulamaEpoch;

    // FAZ36 İŞ E: Bonus/scatter ayarları (MotorBonusServisi read-only okur — çarpandan AYRI).
    /// <summary>scatterEsik (OyunYoneticisi.scatterEsik) — bonus tetik için gereken scatter sayısı.</summary>
    public int scatterEsik;
    /// <summary>Bonus otomatik spin periyodu (otomatik modda P>0 → ~1/P sıklık; manuel/0 → varsayılan).</summary>
    public int bonusPeriyot;

    // FAZ36 İŞ D2: Zorla çarpan + ödeme toggle (ZorlaCarpanMotoru read-only okur).
    /// <summary>zorlaSiradakiCarpan — bu spin için zorlanan çarpan (>0 ise ZorlaCarpanMotoru devreye girer, tek atımlık).</summary>
    public int zorlaCarpan;
    /// <summary>carpanTumbleAktif — "Çarpan Düşünce Ödeme Versin" toggle (AÇIK → küme×force; KAPALI → kazançsız+bomba).</summary>
    public bool carpanOdemeAcik;

    // FAZ36.5: Altyapı — read-only snapshot. Bu fazda HİÇBİR motor bu 4 alanı okumaz (davranış bit-for-bit aynı);
    // ileride NearMiss / KaçışEngelleme / eğilim-tabanlı / Bonus-birleştirme motorları için girdi hazır.
    // Legacy okuma noktaları (Spin.cs:538/556/573/620/539) AYNEN yerinde — bu alanlar onları DUBLE ETMEZ, sadece taşır.
    /// <summary>yakinKacirmaDegeri10da — 10'da N near-miss yoğunluğu (0=kapalı). NearMissMotoru için.</summary>
    public int yakinKacirma;
    /// <summary>_odemeEgilimiYuzde (0-100) — kazanç eğilimi yüzdesi. Eğilim tabanlı motor kararı için.</summary>
    public int egilimYuzde;
    /// <summary>_bonusGirmeOlasilik (0-1) — bonus oyuna girme olasılığı (legacy yol). BonusMotoru birleştirmesi için.</summary>
    public float bonusGirmeOlasilik;
}
