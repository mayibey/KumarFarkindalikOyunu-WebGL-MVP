using System.Globalization;

/// <summary>
/// TL ve diğer oyun formatlama işlemleri için tek kaynak.
/// </summary>
public static class OyunFormatServisi
{
    /// <summary>
    /// Tam sayı tutarı Türkçe sayı biçiminde TL metni olarak döndürür (örn. "1.234 TL").
    /// </summary>
    public static string FormatTL(int value)
    {
        return value.ToString("N0", new CultureInfo("tr-TR")) + " TL";
    }
}
