using UnityEngine;

/// <summary>
/// OyunKorumaServisi için context arayüzü. OyunYoneticisi implement eder.
/// </summary>
public interface IOyunKorumaBaglami
{
    int GetMaxTumbleTur();
    int GetTumbleSabitEsik();
}

/// <summary>
/// Oyun koruma/sabit değerleri tek kaynak. MAX_TUMBLE_TUR, TUMBLE_SABIT_ESIK vb.
/// Context üzerinden erişim istenirse IOyunKorumaBaglami kullanılır.
/// </summary>
public static class OyunKorumaServisi
{
    /// <summary>Ekranda aynı sembolden bu kadar olunca cluster patlar (Bonanza kuralı).</summary>
    public const int TUMBLE_SABIT_ESIK = 8;

    /// <summary>Bir tumble döngüsünde en fazla tur sayısı (sonsuz döngü koruması).</summary>
    public const int MAX_TUMBLE_TUR = 20;

    /// <summary>Zorluk slider değeri aralığı (4-12).</summary>
    public static int ClampZorluk(int v) => Mathf.Clamp(v, 4, 12);

    /// <summary>Scatter yüzde 0-100.</summary>
    public static int ClampScatterYuzde(int v) => Mathf.Clamp(v, 0, 100);

    /// <summary>Çarpan max adet 0-5 (admin panel).</summary>
    public static int ClampCarpanAdet(int v) => Mathf.Clamp(v, 0, 5);

    /// <summary>Null ise varsayılan döndür (koruma yardımcısı).</summary>
    public static T GuvenliNull<T>(T value, T varsayilan) where T : class => value ?? varsayilan;
}
