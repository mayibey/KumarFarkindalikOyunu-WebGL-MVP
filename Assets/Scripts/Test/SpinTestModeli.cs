using System;
using System.Collections.Generic;

namespace KumarTest
{
    /// <summary>SpinTestAraci için tek spin'in forensic kaydı.</summary>
    [Serializable]
    public class SpinKaydi
    {
        public int spinNo;
        public string senaryoAd;
        public int bahis;
        public int bakiyeOnce;
        public int bakiyeSonra;
        public int odenen;
        public int clusterSayisi;
        public string clusterDetay;
        public int tumbleSayisi;
        public bool carpanDustu;
        public int carpanDeger;
        public string carpanKaynak;
        public bool carpanCarpildi;
        public bool bonusTetiklendi;
        public int bonusOdenen;
        public int bonusSpinSayisi;
        public int ardisikKayipSayacOnce;
        public bool kacisFrenlemeTetik;
        public int enYuksekClusterSembol;
        public string baslangicGridDurumu;
        public string sonGridDurumu;
        public long spinSureMs;
        public string clusterTuruDagilimi;
        public float ortalamaCarpan;
        public string kazancKategorisi;
        public bool forceCarpanIstendi;
        public bool bonusSatinAlindi;
        public string spinTipi;
    }

    [Serializable]
    public class IntIntCifti { public int anahtar; public int adet; }

    [Serializable]
    public class StringIntCifti { public string anahtar; public int adet; }

    [Serializable]
    public class SenaryoOzet
    {
        public string senaryoAd;
        public int senaryoIndex;
        public int toplamSpin;
        public long toplamBahis;
        public long toplamKazanc;
        public long netKar;
        public float rtpYuzde;
        public int carpanDusenSpin;
        public float carpanDususOraniYuzde;
        public List<IntIntCifti> carpanDegerDagilimi = new List<IntIntCifti>();
        public List<StringIntCifti> carpanKaynakDagilimi = new List<StringIntCifti>();
        public int clusterPatlayanSpin;
        public float clusterPatlamaOraniYuzde;
        public float ortalamaTumbleSpinBasi;
        public int bonusTetiklemeSayisi;
        public long bonusToplamOdeme;
        public float bonusOrtalamaOdeme;
        public int maxTekSpinKazanc;
        public int enUzunArdisikKayipSerisi;
        public int enUzunArdisikKazancSerisi;
        public List<StringIntCifti> kazancKategoriDagilimi = new List<StringIntCifti>();
        public int kacisFrenlemeTetikSayisi;
        public float standartSapmaKazanc;
        public float ortalamaSpinSureMs;
        public List<SpinKaydi> spinler = new List<SpinKaydi>();

        public int CarpanDegerAdediBul(int deger)
        {
            for (int i = 0; i < carpanDegerDagilimi.Count; i++)
                if (carpanDegerDagilimi[i].anahtar == deger) return carpanDegerDagilimi[i].adet;
            return 0;
        }
        public int CarpanDegerEkle(int deger)
        {
            for (int i = 0; i < carpanDegerDagilimi.Count; i++)
                if (carpanDegerDagilimi[i].anahtar == deger) return ++carpanDegerDagilimi[i].adet;
            carpanDegerDagilimi.Add(new IntIntCifti { anahtar = deger, adet = 1 });
            return 1;
        }
        public int KategoriAdediBul(string ad)
        {
            for (int i = 0; i < kazancKategoriDagilimi.Count; i++)
                if (kazancKategoriDagilimi[i].anahtar == ad) return kazancKategoriDagilimi[i].adet;
            return 0;
        }
        public void KategoriEkle(string ad)
        {
            for (int i = 0; i < kazancKategoriDagilimi.Count; i++)
                if (kazancKategoriDagilimi[i].anahtar == ad) { kazancKategoriDagilimi[i].adet++; return; }
            kazancKategoriDagilimi.Add(new StringIntCifti { anahtar = ad, adet = 1 });
        }
        public void CarpanKaynakEkle(string ad)
        {
            for (int i = 0; i < carpanKaynakDagilimi.Count; i++)
                if (carpanKaynakDagilimi[i].anahtar == ad) { carpanKaynakDagilimi[i].adet++; return; }
            carpanKaynakDagilimi.Add(new StringIntCifti { anahtar = ad, adet = 1 });
        }
    }

    [Serializable]
    public class TestParametreleri
    {
        public int spinSayisi = 100;
        public int baslangicBahis = 100;
        public int baslangicBakiye = 10000;
        public bool[] senaryoSecili = new bool[6]; // 0=Normal, 1-5=Senaryo 1-5
        public int randomSeed = 0;
        public bool seedManuel = false;
        public bool verboseLog = false;
        public float testHizCarpani = 50f;

        // İleri Ayarlar (Senaryo 0 = Normal modda uygulanır; senaryo 1-5'te yok sayılır)
        public bool ileriAyarlarAcik = false;
        public int carpanOlasilikYuzde = 2;       // 0-100
        public int maxCarpanTekSpinSayisi = 3;    // 1-6
        public int bonusOtomatikSpinPeriyodu = 0; // 0=devre dışı, 1-500
        public int yakinKacirmaDegeri = 0;        // 0-10 (10'da N)
        public int odemeEgilimiYuzde = 65;        // 0-100
        public int ardisikKayipLimiti = 8;        // 0-20
    }

    [Serializable]
    public class TestSonucPaketi
    {
        public string baslangicTarih;
        public string bitisTarih;
        public float toplamSureSn;
        public List<SenaryoOzet> senaryolar = new List<SenaryoOzet>();
        public bool tamamlandi;
        public string iptalSebebi;
    }

    public static class SpinTestSabitler
    {
        public static readonly string[] SenaryoAdlari =
        {
            "0. Normal Oyun",
            "1. Alıştırma",
            "2. Biraz Kazandıralım",
            "3. Biraz Kaybettirelim",
            "4. Az Kazandıralım Çok Kaybettirelim",
            "5. Büyük Tekliflerle Parasını Alalım"
        };

        public const string EditorPrefsKey_TestAktif = "SpinTest_Aktif";
        public const string EditorPrefsKey_Parametre = "SpinTest_Parametre";

        public static string KazancKategorisi(int kazanc, int bahis)
        {
            if (bahis <= 0) return "Normal";
            float oran = (float)kazanc / bahis;
            if (oran >= 15f) return "EpicWin";
            if (oran >= 5f) return "MegaWin";
            if (oran >= 2f) return "BigWin";
            return "Normal";
        }
    }

    [Serializable]
    public class IlerlemeBilgisi
    {
        public int aktifSenaryo;
        public int toplamSenaryo;
        public string aktifSenaryoAd;
        public int aktifSpin;
        public int hedefSpin;
        public bool calisiyor;
    }
}
