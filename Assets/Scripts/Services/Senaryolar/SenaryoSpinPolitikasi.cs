/// <summary>
/// Spin simülasyonunda senaryoya özel kuralların toplanacağı politika katmanı.
/// Fabrika: preset kapalı veya indeks ≥3 → <see cref="VarsayilanSpinPolitikasi"/>; indeks 0/1/2 → admin senaryo sınıfları (şimdilik üst sınıfla aynı).
/// </summary>
public readonly struct SenaryoSpinPolitikasiBaglami
{
    public readonly bool UstUsteAktif;
    public readonly int UstUsteKazancHedef;
    public readonly int UstUsteKayipHedef;
    /// <summary>Admin ödeme preset 1/2/3 (öğretmen senaryosu) açık mı.</summary>
    public readonly bool AdminSenaryo123Aktif;

    public SenaryoSpinPolitikasiBaglami(bool ustUsteAktif, int ustUsteKazancHedef, int ustUsteKayipHedef, bool adminSenaryo123Aktif)
    {
        UstUsteAktif = ustUsteAktif;
        UstUsteKazancHedef = ustUsteKazancHedef;
        UstUsteKayipHedef = ustUsteKayipHedef;
        AdminSenaryo123Aktif = adminSenaryo123Aktif;
    }
}

/// <summary>FillRandomAll sonrası pedagojik senaryo grid müdahalesi (scatter garantisi).</summary>
public enum SimulasyonScatterGridMudahalesi
{
    Yok = 0,
    DortScatterGaranti = 1
}

/// <summary>Zorla çarpan + toggle sonrası ilk gridde yapılacak ek adım (tumble cluster vs kazançsız).</summary>
public enum SimulasyonZorlaCarpanSonrasiIlkGridIsi
{
    Yok = 0,
    BombaYerlestirVeTumbleCluster = 1,
    BombaYerlestirVeKazancsiz = 2
}

public readonly struct SimulasyonSenaryoScatterKarari
{
    public readonly SimulasyonScatterGridMudahalesi Mudahale;

    public SimulasyonSenaryoScatterKarari(SimulasyonScatterGridMudahalesi mudahale)
    {
        Mudahale = mudahale;
    }

    public static SimulasyonSenaryoScatterKarari Hicbiri() =>
        new SimulasyonSenaryoScatterKarari(SimulasyonScatterGridMudahalesi.Yok);
}

/// <summary>Simülasyon döngüsündeki senaryo-özel kararlar (reroll, zorunlu boş spin vb.).</summary>
public interface ISenaryoSpinPolitikasi
{
    /// <summary>Üst üste / admin preset kurallarına göre max reroll tavanını yükseltir.</summary>
    void SimulasyonRerollUstSiniriniUygula(ref int maxReroll, in SenaryoSpinPolitikasiBaglami baglam);

    /// <summary>true dönerse zorunlu boş spin (Senaryo 1–2 kuralı) admin preset aktifken iptal edilir.</summary>
    bool ZorunluBosSpinAdminPresetYuzundenDevreDisiBirakilsin(in SenaryoSpinPolitikasiBaglami baglam);

    /// <summary>Aktif politika admin net ödeme bandını ayarlar. Sen 2/3: <paramref name="beklenenKazanc"/> ile kazanç/kayıp bandı seçilir; sen 1 ve varsayılan politikada kullanılmaz.</summary>
    void AdminOdemeEfektifBandiniUygula(int bahis, bool beklenenKazanc, ref int efektifMin, ref int efektifMax);

    /// <summary>Bu politikada hedef±tolerans ödeme kontrolü atlanır mı (admin sen 2/3).</summary>
    bool OdemeModelindeHedefToleransAtlanmali();

    /// <summary>Admin manuel sahnesinde patlama görünürlüğü için ilk gridde küme zorlansın mı.</summary>
    bool AdminManuelIlkGriddeClusterZorlansin(bool bonusSpin, bool adminManuelMod, int zorlaCarpanDegeri, int zorlukSeviyesi);

    /// <summary>Üst üste kazanç fazı veya admin senaryo 2/3 kayıp fazı için ilk gridde en az bir küme zorlansın mı.</summary>
    bool UstUsteVeyaAdminSenaryo23IcinIlkGriddeClusterZorlansin(
        bool bonusSpin,
        bool ustUsteKazancBekleniyor,
        bool adminSenaryo2Aktif,
        bool adminSenaryo3Aktif);

    /// <summary>Aşama 1–2 scatter garantisi: gridde ne uygulanacağı (çağıran Gride* metotlarını çalıştırır).</summary>
    SimulasyonSenaryoScatterKarari SimulasyonSenaryoScatterVeGarantiyiDegerlendir(bool bonusSpin);

    /// <summary>50K+ bakiye sonrası N spin boyunca ilk grid kazançsız yapılsın mı.</summary>
    bool Bakiye50KUstundeSimulasyonIlkGridKazancsizYapilsin(bool bonusSpin, bool adminManuelMod, bool ozellikAktif, int kapanmayaKalanSpin);

    /// <summary>Önbellekten spin alınırken <c>OdemeModelineUygunMu</c> ile yeniden doğrulama yapılsın mı (admin ödeme preset 1–3).</summary>
    bool OncedenHesaplanmisSpinOdemeModeliyleDogrula();

    /// <summary>Önbellekteki normal spin kaydı için <c>OdemeModelineUygunMu</c> yeniden çalıştırılmalı mı (üstteki bayrak + kayıt türü).</summary>
    bool OncedenHesaplanmisNormalSpinOdemeYenidenDogrulansinMi(SpinSimulasyonKaydi adayKayit, bool forBonusSpin);

    /// <summary>Zorla çarpan griddeyken (toggle açık) bomba sonrası cluster mı kazançsız mı.</summary>
    SimulasyonZorlaCarpanSonrasiIlkGridIsi SimulasyonZorlaCarpanSonrasiIlkGridIsiBelirle(
        int zorlaCarpanDegeri,
        bool carpanToggleSecili,
        bool carpanUretimiAktifmi);

    /// <summary>Kolay zorluk + bonus: ödenebilir limite göre minimum altında kalsın reddi.</summary>
    bool KolayZorlukBonusSpindeMinOdemeAltindaReddet(bool bonusSpin, bool zorlaCarpanVardi, int limit, float easyBias01, int nihaiOdeme);

    /// <summary>Kolay zorluk + normal: tumble yoksa senaryo/otomatik kuralına göre yeniden dene.</summary>
    bool KolayZorlukTumblesizSonuctaYenidenDene(
        bool bonusSpin,
        bool zorlaCarpanVardi,
        int limit,
        float easyBias01,
        SpinSimulasyonKaydi kayit,
        bool senaryoYoneticisiVar,
        int otomatikSpinKalan);

    /// <summary>Reroll bitti: son denemede tumble adımı yokken pozitif ham kazanç gösterilmemeli (kayıt sıfırlanır).</summary>
    bool SimulasyonSonDenemedeAdimsizPozitifHamIptalEdilsinMi(bool bonusSpin, int tumbleAdimSayisi, int toplamHamKazanc);

    /// <summary>Reroll bitti: <see cref="SpinSimulasyonKaydi.SenaryoOdemeBandinaUygun"/> için band uygunluğu (ödeme modeli sonucu çağıran hesaplar).</summary>
    bool SimulasyonFallbackOdemeBandinaUygunMu(bool bonusSpin, bool zorlaCarpanKullanildi, bool odemeModeliSonDenemedeUygun);
}

public static class SenaryoSpinPolitikasiFabrikasi
{
    /// <param name="presetAktif">Senaryo modu toggle.</param>
    /// <param name="adminPresetIndex">Dropdown indeksi; 0–2 ödeme preset’i, 3+ zorla çarpan vb. için varsayılan politika.</param>
    public static ISenaryoSpinPolitikasi Olustur(bool presetAktif, int adminPresetIndex)
    {
        if (!presetAktif || adminPresetIndex < 0 || adminPresetIndex > 4)
            return new VarsayilanSpinPolitikasi();
        return adminPresetIndex switch
        {
            0 => new AdminSenaryo1SpinPolitikasi(),
            1 => new AdminSenaryo2SpinPolitikasi(),
            2 => new AdminSenaryo3SpinPolitikasi(),
            3 => new AdminSenaryo4SpinPolitikasi(),
            4 => new AdminSenaryo5SpinPolitikasi(),
            _ => new VarsayilanSpinPolitikasi()
        };
    }
}
