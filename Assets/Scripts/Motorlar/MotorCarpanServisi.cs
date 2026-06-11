using UnityEngine;

// MotorCarpanServisi: YENİ motor katmanının çarpan karar servisi (stateless). Legacy Services/CarpanServisi'nden
// BAĞIMSIZ ve farklı — o legacy spin akışının instance servisi (OyunYoneticisi/Simulasyon/Fields kullanır), buna dokunulmaz.

/// <summary>
/// FAZ36 İŞ A: Çarpan kararı TEK yerde — düşme olasılığı + adet + değerler. Stateless, spin'i YÖNETMEZ (ISpinMotoru DEĞİL).
/// Panel ayarlarını MotorGirdi'den READ-ONLY okur (carpanAktif/carpanOlasilik/maxCarpanAdedi). Motor kararı alır,
/// KENDİ band tavanına uydurur (küme×toplam ≤ kendi tavanı), reçeteye yazar → tek motor kuralı bozulmaz.
///
/// Çarpanlar TOPLANIR (Sweet Bonanza tarzı: ödeme = ham × Σçarpan). Olasılık AYRI (slider) olduğu için, "düştü"
/// kararı verilince değerler ≥2 üretilir (gerçek çarpan; 1x = "çarpan yok" zaten olasılık tarafında elendi).
/// Panel sözleşmesi: slider %100 → her spin çarpan, %0 → hiç. "Panel ne vaat ederse o."
/// </summary>
public static class MotorCarpanServisi
{
    public struct Karar
    {
        public bool dussun;       // bu spin çarpan düşecek mi
        public int[] degerler;    // düşen her çarpanın değeri (≥2), null ise yok
        public int toplam;        // Σdegerler (yok ise 1 — nötr)
    }

    public static Karar Hesapla(MotorGirdi g)
    {
        var yok = new Karar { dussun = false, degerler = null, toplam = 1 };
        if (g == null || !g.carpanAktif || g.carpanOlasilik <= 0f) return yok;
        if (Random.value > Mathf.Clamp01(g.carpanOlasilik)) return yok;   // slider %: düşmedi

        int tavan = Mathf.Clamp(g.maxCarpanAdedi, 1, 8);
        int adet = Random.Range(1, tavan + 1);                            // 1..maxCarpanAdedi
        var d = new int[adet];
        int t = 0;
        for (int i = 0; i < adet; i++) { d[i] = AgirlikliDeger(); t += d[i]; }
        return new Karar { dussun = true, degerler = d, toplam = Mathf.Max(2, t) };
    }

    // Düştüyse değer ≥2 (gerçek çarpan). 2x %70, 3x %30. Olasılık ayrı slider'da yönetildiği için 1x üretilmez.
    private static int AgirlikliDeger() => Random.value < 0.70f ? 2 : 3;
}
