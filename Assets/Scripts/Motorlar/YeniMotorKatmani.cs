/// <summary>
/// FAZ36: Yeni motor dispatch'i. SimuleEtVeKaydetImpl (scripted erken-return sonrası) buradan TEK motor seçer.
/// SADECE 03 (buildIndex==2) + ayar seçili bir motorla eşleşirse reçete döner; aksi halde null → legacy AYNEN.
/// Karışım YOK (ANAYASA rule 3): bir spin = bir motor.
///
/// Yeni motor eklemek: ilgili sınıfı _motorlar dizisine ekle. Dispatch hook (Spin.cs) DEĞİŞMEZ.
/// </summary>
public static class YeniMotorKatmani
{
    // ADIM 1: YontmaMotoru. ADIM 2: HookMotoru. Sonraki fazlar: Tutma / Koruma / Ozel / NearMiss / Kacis eklenir.
    private static readonly ISpinMotoru[] _motorlar = new ISpinMotoru[]
    {
        new YontmaMotoru(),
        new HookMotoru(),
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
