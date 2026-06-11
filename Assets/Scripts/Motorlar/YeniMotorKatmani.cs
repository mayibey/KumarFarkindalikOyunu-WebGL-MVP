/// <summary>
/// FAZ36: Yeni motor dispatch'i. SimuleEtVeKaydetImpl (scripted erken-return sonrası) buradan TEK motor seçer.
/// SADECE 03 (buildIndex==2) + ayar seçili bir motorla eşleşirse reçete döner; aksi halde null → legacy AYNEN.
/// Karışım YOK (ANAYASA rule 3): bir spin = bir motor.
///
/// Yeni motor eklemek: ilgili sınıfı _motorlar dizisine ekle. Dispatch hook (Spin.cs) DEĞİŞMEZ.
/// </summary>
public static class YeniMotorKatmani
{
    // İŞ D2: ZorlaCarpanMotoru EN ÖNDE (öncelik — force her modu ezer, tek atımlık). Sonra mod motorları.
    // ADIM 1: Yontma. ADIM 2: Hook. İŞ B: Koruma. İŞ C: Tutma. FAZ36.2: Ozel (admin MIN/MAX band). Sonraki: NearMiss / Kacis.
    // Ozel, Zorla'dan SONRA / mod motorlarından ÖNCE: tetiği aktifSenaryo=="ozel" (diğer modlarla çakışmaz —
    // TEK MOTOR kuralı). Reçete dönünce legacy mod-konstrukte yolu (Spin.cs:586) 03'te HİÇ çalışmaz (null → legacy aynen).
    private static readonly ISpinMotoru[] _motorlar = new ISpinMotoru[]
    {
        new ZorlaCarpanMotoru(),
        new OzelMotoru(),
        new YontmaMotoru(),
        new HookMotoru(),
        new KorumaMotoru(),
        new TutmaMotoru(),
    };

    /// <summary>Uygun motoru bul, reçete üret. Hiçbiri uymazsa null (legacy çalışır → güvenli varsayılan).</summary>
    public static SpinSimulasyonKaydi Dispatch(MotorGirdi girdi)
    {
        if (girdi == null) return null;
        for (int i = 0; i < _motorlar.Length; i++)
        {
            if (_motorlar[i].Uygulanabilir(girdi))
                return _motorlar[i].ReceteUret(girdi);
        }
        return null;
    }
}
