using UnityEngine;

/// <summary>Mevcut oyun davranışını taşıyan varsayılan spin politikası (Faz 1–2).</summary>
public class VarsayilanSpinPolitikasi : ISenaryoSpinPolitikasi
{
    public void SimulasyonRerollUstSiniriniUygula(ref int maxReroll, in SenaryoSpinPolitikasiBaglami baglam)
    {
        if (!baglam.UstUsteAktif)
            return;
        bool tekFazAktif = (baglam.UstUsteKazancHedef == 0) ^ (baglam.UstUsteKayipHedef == 0);
        maxReroll = Mathf.Max(maxReroll, tekFazAktif ? 260 : 180);
        if (baglam.AdminSenaryo123Aktif)
            maxReroll = Mathf.Max(maxReroll, 520);
    }

    public bool ZorunluBosSpinAdminPresetYuzundenDevreDisiBirakilsin(in SenaryoSpinPolitikasiBaglami baglam)
    {
        return baglam.AdminSenaryo123Aktif;
    }

    /// <summary>Admin ödeme preset kapalıyken bant yok; efektif min/max çağrı öncesi zaten Inspector min/max.</summary>
    public virtual void AdminOdemeEfektifBandiniUygula(int bahis, bool beklenenKazanc, ref int efektifMin, ref int efektifMax)
    {
        _ = bahis;
        _ = beklenenKazanc;
        _ = efektifMin;
        _ = efektifMax;
    }

    /// <summary>Serbest oyunda hedef±tolerans uygulanır (false).</summary>
    public virtual bool OdemeModelindeHedefToleransAtlanmali() => false;

    public virtual bool AdminManuelIlkGriddeClusterZorlansin(bool bonusSpin, bool adminManuelMod, int zorlaCarpanDegeri, int zorlukSeviyesi)
    {
        return !bonusSpin && adminManuelMod && zorlaCarpanDegeri <= 0 && zorlukSeviyesi <= 5;
    }

    public virtual bool UstUsteVeyaAdminSenaryo23IcinIlkGriddeClusterZorlansin(
        bool bonusSpin,
        bool ustUsteKazancBekleniyor,
        bool adminSenaryo2Aktif,
        bool adminSenaryo3Aktif)
    {
        if (bonusSpin)
            return false;
        if (ustUsteKazancBekleniyor)
            return true;
        return adminSenaryo2Aktif || adminSenaryo3Aktif;
    }

    public virtual SimulasyonSenaryoScatterKarari SimulasyonSenaryoScatterVeGarantiyiDegerlendir(bool bonusSpin)
    {
        if (bonusSpin || SenaryoYoneticisi.I == null)
            return SimulasyonSenaryoScatterKarari.Hicbiri();
        var a = SenaryoYoneticisi.I.mevcutAsama;
        int since = SenaryoYoneticisi.I.SpinsSinceLastScatter();
        if ((a == SenaryoYoneticisi.SenaryoAsama.Asama1_IsindirmaUmut && since >= 50) ||
            (a == SenaryoYoneticisi.SenaryoAsama.Asama2_KontrolBende && since >= 75))
            return new SimulasyonSenaryoScatterKarari(SimulasyonScatterGridMudahalesi.DortScatterGaranti);
        return SimulasyonSenaryoScatterKarari.Hicbiri();
    }

    public virtual bool Bakiye50KUstundeSimulasyonIlkGridKazancsizYapilsin(
        bool bonusSpin,
        bool adminManuelMod,
        bool ozellikAktif,
        int kapanmayaKalanSpin)
    {
        return ozellikAktif && !bonusSpin && !adminManuelMod && kapanmayaKalanSpin > 0;
    }

    public virtual bool OncedenHesaplanmisSpinOdemeModeliyleDogrula() => false;

    public virtual bool OncedenHesaplanmisNormalSpinOdemeYenidenDogrulansinMi(
        SpinSimulasyonKaydi adayKayit,
        bool forBonusSpin)
    {
        if (adayKayit == null || forBonusSpin || adayKayit.ZorlaCarpanKullanildi)
            return false;
        return OncedenHesaplanmisSpinOdemeModeliyleDogrula();
    }

    public virtual SimulasyonZorlaCarpanSonrasiIlkGridIsi SimulasyonZorlaCarpanSonrasiIlkGridIsiBelirle(
        int zorlaCarpanDegeri,
        bool carpanToggleSecili,
        bool carpanUretimiAktifmi)
    {
        if (zorlaCarpanDegeri <= 0 || !carpanToggleSecili)
            return SimulasyonZorlaCarpanSonrasiIlkGridIsi.Yok;
        // Force carpan aktifken carpanUretimiAktif toggle'ından bağımsız olarak cluster yerleştir.
        return SimulasyonZorlaCarpanSonrasiIlkGridIsi.BombaYerlestirVeTumbleCluster;
    }

    public virtual bool KolayZorlukBonusSpindeMinOdemeAltindaReddet(
        bool bonusSpin,
        bool zorlaCarpanVardi,
        int limit,
        float easyBias01,
        int nihaiOdeme)
    {
        if (zorlaCarpanVardi || !bonusSpin || limit <= 100 || easyBias01 <= 0.3f)
            return false;
        int minHedef = Mathf.RoundToInt(limit * 0.25f);
        return minHedef > 0 && nihaiOdeme < minHedef;
    }

    public virtual bool KolayZorlukTumblesizSonuctaYenidenDene(
        bool bonusSpin,
        bool zorlaCarpanVardi,
        int limit,
        float easyBias01,
        SpinSimulasyonKaydi kayit,
        bool senaryoYoneticisiVar,
        int otomatikSpinKalan)
    {
        if (bonusSpin || zorlaCarpanVardi || easyBias01 <= 0.3f || limit < 50
            || kayit?.Adimlar == null || kayit.Adimlar.Count != 0)
            return false;
        if (senaryoYoneticisiVar)
        {
            float yenidenDeneOlasilik = otomatikSpinKalan > 0 ? 0.52f : 0.45f;
            return UnityEngine.Random.value < yenidenDeneOlasilik;
        }
        return otomatikSpinKalan <= 0;
    }

    public virtual bool SimulasyonSonDenemedeAdimsizPozitifHamIptalEdilsinMi(
        bool bonusSpin,
        int tumbleAdimSayisi,
        int toplamHamKazanc)
    {
        return !bonusSpin && tumbleAdimSayisi == 0 && toplamHamKazanc > 0;
    }

    public virtual bool SimulasyonFallbackOdemeBandinaUygunMu(
        bool bonusSpin,
        bool zorlaCarpanKullanildi,
        bool odemeModeliSonDenemedeUygun)
    {
        return !bonusSpin && !zorlaCarpanKullanildi && odemeModeliSonDenemedeUygun;
    }
}
