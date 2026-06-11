using UnityEngine;

// MotorBonusServisi: YENİ motor katmanının bonus/scatter karar servisi (stateless). Mevcut Bonus* sınıflarından
// (BonusAyarlari/BonusUIServisi — legacy bonus OYUNU) BAĞIMSIZ ve farklı; onlara dokunulmaz.

/// <summary>
/// FAZ36 İŞ E: Scatter/bonus kararı TEK yerde (çarpandan AYRI servis, karışmaz). Stateless.
/// Panel bonus ayarını (otomatik periyot / manuel) READ-ONLY okur → scatter düşme sıklığına bağlar.
/// Karar = kaç scatter yerleştirilecek. Eşik (g.scatterEsik) kadar → DonusAkisServisi bonus tetikler (bonus OYUNU
/// LEGACY'de çalışır; motor sadece tetik-spinini üretir, dispatch !bonusSpin kapısı). Eşik altı → görsel heyecan.
/// Motor bu kararı alır, ScatterEnjekte ile gride yazar.
/// </summary>
public static class MotorBonusServisi
{
    public struct Karar
    {
        public int scatterSayisi;   // gride yerleştirilecek scatter adedi
        public bool bonusTetikler;  // scatterSayisi >= eşik mi (bonus tetik beklentisi)
    }

    public static Karar Hesapla(MotorGirdi g)
    {
        if (g == null) return new Karar { scatterSayisi = 0, bonusTetikler = false };
        int esik = Mathf.Max(1, g.scatterEsik);
        float p = BonusOlasiligi(g);
        if (Random.value < p)
            return new Karar { scatterSayisi = esik, bonusTetikler = true };       // eşik kadar → bonus tetikler
        return new Karar { scatterSayisi = Random.Range(0, esik - 1), bonusTetikler = false };  // 0..esik-2 görsel, eşik altı
    }

    // FAZ36.1 FIX1: Periyot P>0 → ~1/P sıklık; P=0 → 0 (bonus KAPALI — panel slider %0 = bonus yok, tek kapı).
    // Sıfıra bölme koruması: bölme yalnız P>0 dalında; P=0 dalı doğrudan 0 döner.
    private static float BonusOlasiligi(MotorGirdi g)
        => g.bonusPeriyot > 0 ? 1f / g.bonusPeriyot : 0f;
}
