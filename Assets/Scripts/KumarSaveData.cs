using System.Collections.Generic;

/// <summary>
/// Senaryolu eğitim oyununun "devam et" save state'i. JsonUtility ile PlayerPrefs'e
/// (KumarSaveData_v1 anahtarı) string olarak yazılır. Spin sonu + borç paneli sonu
/// otomatik save tetiklenir; A7 final ekranı açılınca silinir.
///
/// Mimar notları:
/// - SenaryoYoneticisi.mevcutAsama save EDILMEZ — Anlatıcı sahnesi her başlangıçta
///   defansif Asama7_Finale'ye alır (forcedNoPay bypass için). Gerçek aşama aktifAsama'da.
/// - borcAlindi save edilmez — _yuklemePaneliAcildiBuOturum bayrağı zaten yakalıyor.
/// - saveSurumu future migration için. JsonUtility eski JSON'lardaki eksik alanları
///   default değerde bırakır → forward-compat. Sürüm uyuşmazsa Load null dönsün.
/// </summary>
[System.Serializable]
public class KumarSaveData
{
    public string saveSurumu = "1.0";
    public string saveZamani; // UTC ISO ("o" format)
    public string kullaniciAdi;

    // ===== AnlaticiSeritKopru durumu =====
    public int aktifAsama;
    public int aktifSpin;
    public int toplamSpin;
    public int sonUygulananAsama;
    public long sonBakiye;
    public long baslangicBakiye;
    public List<int> asamaSpinNet = new List<int>();
    // PAKET 10: Geçmiş aşamaların spin net'i — kullanıcı manuel aşama değişimi (◀ ▶) ile
    // önceki aşamalara dönerse progress bar'ları görebilsin. JsonUtility Dictionary
    // desteklemez → List<AsamaSpinNetKayit> wrapper.
    public List<AsamaSpinNetKayit> tumAsamaSpinNet = new List<AsamaSpinNetKayit>();

    // ===== Modal flag'leri (9) — restore'dan sonra modal TEKRAR oynamasın =====
    public bool yuklemePaneliAcildiBuOturum;
    public bool donguModalGosterildi;
    public bool preA1ModalGosterildi;
    public bool a2GecisModalGosterildi;
    public bool a3GecisModalGosterildi;
    public bool a4GecisModalGosterildi;
    public bool a5GecisModalGosterildi;
    public bool a3BahisYukseltildi;
    public bool a4S5CarpanModalGosterildi;
    public bool a4S1DonmeGosterildi;

    // ===== Ekonomi =====
    public int bakiye;
    public int oturumKazanc;

    // ===== SenaryoYoneticisi (mevcutAsama save edilmez) =====
    public int yuklemeSayisi;

    // ===== A5 bonus durumu =====
    // BonusYatirim/BonusKazanc public static — final ekran modal yüzde hesabı için kritik.
    public int bonusYatirim;
    public int bonusKazanc;

    // ===== Admin parametreleri (Anlatıcı dinamik set ediyor) =====
    public int adminEgilim;
    public int adminMaxOdeme;
}

/// <summary>PAKET 10: KumarSaveData.tumAsamaSpinNet için JsonUtility uyumlu wrapper —
/// Dictionary&lt;int,List&lt;int&gt;&gt; serileştirilemez, List&lt;AsamaSpinNetKayit&gt; yapılır.</summary>
[System.Serializable]
public class AsamaSpinNetKayit
{
    public int asama;
    public List<int> spinNet = new List<int>();
}
