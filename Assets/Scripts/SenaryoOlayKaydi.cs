using System;

/// <summary>
/// Senaryolu oyun oturumunda bir olayı (aşama girişi, çıkışı, şart tamamlandı, bakiye yükleme, manuel geçiş vb.) log sahnesinde göstermek için tek kayıt.
/// </summary>
[Serializable]
public class SenaryoOlayKaydi
{
    public string zaman;
    public int spinIndex;
    public int asamaNo;
    public string asamaAdi;
    public string olayTipi;
    public string aciklama;
    public int bakiye;
    public int toplamYatirilan;
    public int netZarar;

    public SenaryoOlayKaydi(int spinIdx, int asama, string asamaAd, string tip, string acik, int bakiy, int toplamYatir, int netZr)
    {
        zaman = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        spinIndex = spinIdx;
        asamaNo = asama;
        asamaAdi = asamaAd ?? "";
        olayTipi = tip ?? "";
        aciklama = acik ?? "";
        bakiye = bakiy;
        toplamYatirilan = toplamYatir;
        netZarar = netZr;
    }

    // Mevcut senaryo olay tipleri
    public static string OlayTipi_AsamaGirisi => "AsamaGirisi";
    public static string OlayTipi_AsamaCikisi => "AsamaCikisi";
    public static string OlayTipi_AsamaOzeti => "AsamaOzeti";
    public static string OlayTipi_BonusGirisi => "BonusGirisi";
    public static string OlayTipi_BonusCikisi => "BonusCikisi";
    public static string OlayTipi_SartTamamlandi => "SartTamamlandi";
    public static string OlayTipi_BakiyeYukleme => "BakiyeYukleme";
    public static string OlayTipi_ManuelGecis => "ManuelGecis";

    // Oturum / profil
    public static string OlayTipi_OturumBasladi => "OturumBasladi";
    public static string OlayTipi_OturumBitti => "OturumBitti";
    public static string OlayTipi_ProfilDegisti => "ProfilDegisti";
    public static string OlayTipi_ProfillerKaydedildi => "ProfillerKaydedildi";

    // Ekonomi / bakiye
    public static string OlayTipi_BakiyeYuklendiIlk => "BakiyeYuklendiIlk";
    public static string OlayTipi_BahisDegisti => "BahisDegisti";
    public static string OlayTipi_BakiyeYuklemeEkraniAcildi => "BakiyeYuklemeEkraniAcildi";
    public static string OlayTipi_BakiyeYuklemeYapildi => "BakiyeYuklemeYapildi";
    public static string OlayTipi_BakiyeYuklemeReddedildi => "BakiyeYuklemeReddedildi";
    public static string OlayTipi_ParaCekEkraniAcildi => "ParaCekEkraniAcildi";
    public static string OlayTipi_ParaCekildi => "ParaCekildi";
    public static string OlayTipi_EkonomiSenkronizeEdildi => "EkonomiSenkronizeEdildi";

    // Normal spin
    public static string OlayTipi_NormalSpinBasladi => "NormalSpinBasladi";
    public static string OlayTipi_SpinGridSimuleEdildi => "SpinGridSimuleEdildi";
    public static string OlayTipi_NormalSpinOdemeHesaplandi => "NormalSpinOdemeHesaplandi";
    public static string OlayTipi_NormalSpinSonucuUygulandi => "NormalSpinSonucuUygulandi";
    public static string OlayTipi_NormalSpinScatterKontrolu => "NormalSpinScatterKontrolu";
    public static string OlayTipi_NormalSpinBitti => "NormalSpinBitti";

    // Bonus oyunu
    public static string OlayTipi_BonusBasladi => "BonusBasladi";
    public static string OlayTipi_BonusSpinBasladi => "BonusSpinBasladi";
    public static string OlayTipi_BonusSpinGridSimuleEdildi => "BonusSpinGridSimuleEdildi";
    public static string OlayTipi_BonusSpinOdemeHesaplandi => "BonusSpinOdemeHesaplandi";
    public static string OlayTipi_BonusSpinSonucuUygulandi => "BonusSpinSonucuUygulandi";
    public static string OlayTipi_BonusSpinIhlal => "BonusSpinIhlal";
    public static string OlayTipi_BonusBitti => "BonusBitti";
    public static string OlayTipi_BonusOturumKazanciBakiyeyeEklendi => "BonusOturumKazanciBakiyeyeEklendi";

    // Senaryo / aşama yönetimi
    public static string OlayTipi_SenaryoBasladi => "SenaryoBasladi";
    public static string OlayTipi_AsamaBasladi => "AsamaBasladi";
    public static string OlayTipi_AsamaAralikOzeti => "AsamaAralikOzeti";
    public static string OlayTipi_AsamaBitti => "AsamaBitti";
    public static string OlayTipi_AsamaGecisi => "AsamaGecisi";
    public static string OlayTipi_SenaryoBakiyeYukleme => "SenaryoBakiyeYukleme";

    // UI / panel / buton
    public static string OlayTipi_UIButtonTiklandi => "UIButtonTiklandi";
    public static string OlayTipi_PanelAcilti => "PanelAcilti";
    public static string OlayTipi_PanelKapandi => "PanelKapandi";
    public static string OlayTipi_LogFiltreDegisti => "LogFiltreDegisti";

    // Hata / ihlal / uyarı
    public static string OlayTipi_Ihlal_NormalSpinLimitAsildi => "Ihlal_NormalSpinLimitAsildi";
    public static string OlayTipi_Ihlal_BonusSpinLimitAsildi => "Ihlal_BonusSpinLimitAsildi";
    public static string OlayTipi_Ihlal_BonusToplamLimitAsildi => "Ihlal_BonusToplamLimitAsildi";
    public static string OlayTipi_Ihlal_NegatifBakiye => "Ihlal_NegatifBakiye";
    public static string OlayTipi_Uyari_KonfigUyusmazligi => "Uyari_KonfigUyusmazligi";
    public static string OlayTipi_Uyari_NullReferansOnlendi => "Uyari_NullReferansOnlendi";

    // Farkındalık odaklı uyarılar (log ekranında özellikle gösterilir)
    public static string OlayTipi_Uyari_NetKayipEsigi => "Uyari_NetKayipEsigi";
    public static string OlayTipi_Uyari_UzunOyun => "Uyari_UzunOyun";
    public static string OlayTipi_Uyari_SikBonusAlimi => "Uyari_SikBonusAlimi";
    public static string OlayTipi_Uyari_TiltBahisArtisi => "Uyari_TiltBahisArtisi";

    // Hız & çarpan (log ekranı sol panel "Hız & Çarpan" bölümü için)
    public static string OlayTipi_HizYavasladi => "HizYavasladi";
    public static string OlayTipi_HizHizlandi => "HizHizlandi";
    public static string OlayTipi_BonusCarpanArtti => "BonusCarpanArtti";

    // Zorluk & tumble (hangi aşamada hangi zorluk/tumble olasılıkları uygulandı)
    public static string OlayTipi_ZorlukTumbleAyar => "ZorlukTumbleAyar";

    // Near-Miss (az daha) ve RTP
    public static string OlayTipi_NearMiss => "NearMiss";
    public static string OlayTipi_RTPOrani => "RTPOrani";
}

/// <summary>PlayerPrefs JSON serileştirmesi için.</summary>
[Serializable]
public class SenaryoOturumLoguWrapper
{
    public SenaryoOlayKaydi[] kayitlar;
}
