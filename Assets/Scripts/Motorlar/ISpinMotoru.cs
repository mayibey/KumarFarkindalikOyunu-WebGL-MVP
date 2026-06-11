/// <summary>
/// FAZ36: Yeni motor sözleşmesi. Her mod motoru bunu uygular. SADECE 03 (buildIndex==2) + ilgili ayar
/// seçiliyken YeniMotorKatmani.Dispatch tarafından çağrılır.
///
/// Motor = SADECE KARAR: reçete (grid + ödeme + çarpan) üretir (ANAYASA rule 4).
/// Animasyon/ses/efekt/süre mevcut sunum sisteminde (SimulasyonKaydiniOynat) — motor dokunmaz.
/// İzole: motor başka motorun/legacy'nin state'ini okuyamaz/yazamaz; kendi iç durumu kendi dosyasında.
/// </summary>
public interface ISpinMotoru
{
    /// <summary>Bu motor verilen ayara uygulanabilir mi (dispatch seçim kriteri).</summary>
    bool Uygulanabilir(MotorGirdi girdi);

    /// <summary>
    /// Reçete (SpinSimulasyonKaydi) üret. null dönerse dispatch legacy akışa düşer (güvenli varsayılan).
    /// </summary>
    SpinSimulasyonKaydi ReceteUret(MotorGirdi girdi);
}
