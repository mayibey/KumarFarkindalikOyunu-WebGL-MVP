using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bir spin'in simülasyon sonucu: ilk grid ve her tumble adımı.
/// Simülasyon arkada çalışır; kayıt ekranda oynatılır (RNG tekrar çalışmaz).
/// </summary>
[Serializable]
public class SpinSimulasyonKaydi
{
    public int Sutun { get; set; }
    public int Satir { get; set; }
    /// <summary>İlk doldurma sonrası grid (drop-in öncesi).</summary>
    public int[,] IlkGrid { get; set; }
    /// <summary>İlk grid için çarpan değerleri (genelde 0).</summary>
    public int[,] IlkCarpanGrid { get; set; }
    /// <summary>İlk gridde yerleştirilen çarpan değerleri (oynatmada state için).</summary>
    public List<int> IlkCarpanDegerleri { get; set; } = new List<int>();
    public List<TumbleAdimKaydi> Adimlar { get; set; } = new List<TumbleAdimKaydi>();
    /// <summary>Nihai ham kazanç (tüm tumble'lar toplamı).</summary>
    public int ToplamHamKazanc { get; set; }
    /// <summary>Nihai çarpan (tüm adımlardaki çarpanların çarpımı/toplamı - servis mantığına göre).</summary>
    public int NihaiCarpanToplam { get; set; }
    /// <summary>Zorla çarpan (5x/10x/50x/100x) ile üretildiyse true; kazanç/oturum gösterimi tavanlanmadan yapılır.</summary>
    public bool ZorlaCarpanKullanildi { get; set; }
    /// <summary>Admin senaryo 2/3: Simülasyon bu spin için ödeme modeli bandına uygun üretildiyse true. Fallback ile band dışı sonuç oynatıldıysa false (K-KY sırası ilerletilmez).</summary>
    public bool SenaryoOdemeBandinaUygun { get; set; } = true;
}

/// <summary>
/// Tek bir tumble adımı: patlayan hücreler, bu tur kazancı, bu tur yerleştirilen çarpanlar, refill sonrası grid.
/// </summary>
[Serializable]
public class TumbleAdimKaydi
{
    public List<Vector2Int> PatlayanHucreler { get; set; } = new List<Vector2Int>();
    public int TurKazanci { get; set; }
    /// <summary>Bu turda yerleştirilen çarpan değerleri (sırayla).</summary>
    public List<int> CarpanDegerleriBuTur { get; set; } = new List<int>();
    /// <summary>Refill + çökme sonrası grid.</summary>
    public int[,] GridRefillSonrasi { get; set; }
    /// <summary>Refill + çökme sonrası çarpan değer grid'i.</summary>
    public int[,] CarpanGridRefillSonrasi { get; set; }
    /// <summary>Bu turda yeni spawn edilen hücreler (animasyon için).</summary>
    public List<Vector2Int> YeniSpawnEdilenHucreler { get; set; } = new List<Vector2Int>();
    /// <summary>Yerçekimi ile düşen hücrelerin eski konumu (animasyon: from -> to).</summary>
    public List<Vector2Int> DusenHucreFrom { get; set; } = new List<Vector2Int>();
    /// <summary>Yerçekimi ile düşen hücrelerin yeni konumu.</summary>
    public List<Vector2Int> DusenHucreTo { get; set; } = new List<Vector2Int>();
    /// <summary>Senaryo konstrukte enjeksiyonuyla sembolü değiştirilen hücreler (sprite güncelleme için).</summary>
    public List<Vector2Int> InjekteEdilenHucreler { get; set; } = new List<Vector2Int>();
}
