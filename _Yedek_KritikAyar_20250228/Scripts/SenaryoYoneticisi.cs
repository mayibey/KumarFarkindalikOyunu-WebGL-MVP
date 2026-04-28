using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SenaryoYoneticisi : MonoBehaviour
{
    public static SenaryoYoneticisi I;

    public enum SenaryoAsama
    {
        Asama1_Balayi = 1,
        Asama2_Aliskanlik = 2,
        Asama3_KayipKovalama = 3,
        Asama4_Matematik = 4,
        Asama5_Finale = 5
    }

    [Header("Durum")]
    public bool senaryoAktif = true;
    public bool gelistirmeModu = true;
    public SenaryoAsama mevcutAsama = SenaryoAsama.Asama1_Balayi;

    [Header("Takip")]
    public int toplamSpin;
    public float oyunSuresiDakika;
    public int toplamKazanc;
    public int toplamKayip;
    public int mevcutBakiye;
    public int ilkBakiye = 20000;
    public int yuklemeSayisi = 1;

    [Header("Geçiş Şartları")]
    public int gecis1_kazanc = 40000;
    public int gecis1_spin = 100;
    public float gecis1_sure = 5f;

    public int gecis2_kayip = 10000;
    public int gecis2_bakiye = 10000;
    public int gecis2_spin = 200;

    public int gecis3_kayip = 25000;
    public int gecis3_bakiye = 5000;
    public int gecis3_spin = 300;
    public int gecis3_yukleme = 2;

    public int gecis4_kayip = 45000;
    public int gecis4_bakiye = 2000;
    public int gecis4_spin = 400;
    public int gecis4_yukleme = 3;

    [Header("Kazanç Oranları %")]
    public float asama1_oran = 75f;
    public float asama2_oran = 50f;
    public float asama3_oran = 30f;
    public float asama4_oran = 15f;
    public float asama5_oran = 5f;

    [Header("UI - Oyun")]
    public TextMeshProUGUI asamaText;
    public TextMeshProUGUI spinText;
    public TextMeshProUGUI kazancText;
    public TextMeshProUGUI bakiyeText;

    [Header("UI - Auto Spin")]
    public TMP_Dropdown autoSpinDropdown;
    public Button autoSpinBaslatBtn;
    public Button autoSpinDurdurBtn;
    public TextMeshProUGUI autoSpinKalanText;

    [Header("UI - Admin")]
    public Toggle senaryoAktifToggle;
    public Toggle gelistirmeToggle;
    public TextMeshProUGUI mevcutAsamaText;
    public TextMeshProUGUI gecisSartText;
    public Button[] manuelGecisButonlari;

    private float baslangicZamani;
    private bool autoSpinAktif;
    private int kalanAutoSpin;
    private bool coroutineCalisiyor;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        baslangicZamani = Time.time;
        mevcutBakiye = ilkBakiye;
        SetupUI();
        UI_Guncelle();
    }

    void Update()
    {
        oyunSuresiDakika = (Time.time - baslangicZamani) / 60f;
        
        if (autoSpinAktif && kalanAutoSpin != 0 && !coroutineCalisiyor)
            StartCoroutine(AutoSpinRoutine());
    }

    void SetupUI()
    {
        if (autoSpinDropdown != null)
        {
            autoSpinDropdown.ClearOptions();
            autoSpinDropdown.AddOptions(new List<string> { "10", "25", "50", "100", "200", "Sonsuz" });
            autoSpinDropdown.value = 3;
            autoSpinDropdown.onValueChanged.AddListener(v => { });
        }
        if (autoSpinBaslatBtn != null) autoSpinBaslatBtn.onClick.AddListener(AutoSpinBaslat);
        if (autoSpinDurdurBtn != null) autoSpinDurdurBtn.onClick.AddListener(AutoSpinDurdur);

        if (senaryoAktifToggle != null)
            senaryoAktifToggle.onValueChanged.AddListener(v => { senaryoAktif = v; UI_Guncelle(); });
        if (gelistirmeToggle != null)
            gelistirmeToggle.onValueChanged.AddListener(v => { gelistirmeModu = v; UI_Guncelle(); });

        if (manuelGecisButonlari != null)
            for (int i = 0; i < manuelGecisButonlari.Length; i++)
                if (manuelGecisButonlari[i] != null)
                {
                    int asama = i + 1;
                    manuelGecisButonlari[i].onClick.AddListener(() => ManuelGecis(asama));
                }

        GecisSartGuncelle();
    }

    public void AsamaGecisiKontrol()
    {
        if (!senaryoAktif || gelistirmeModu) return;

        switch (mevcutAsama)
        {
            case SenaryoAsama.Asama1_Balayi:
                if (toplamKazanc >= gecis1_kazanc && toplamSpin >= gecis1_spin && oyunSuresiDakika >= gecis1_sure)
                    AsamaGecir(SenaryoAsama.Asama2_Aliskanlik);
                break;
            case SenaryoAsama.Asama2_Aliskanlik:
                if (toplamKayip >= gecis2_kayip && mevcutBakiye <= gecis2_bakiye && toplamSpin >= gecis2_spin)
                    AsamaGecir(SenaryoAsama.Asama3_KayipKovalama);
                break;
            case SenaryoAsama.Asama3_KayipKovalama:
                if (toplamKayip >= gecis3_kayip && mevcutBakiye <= gecis3_bakiye && toplamSpin >= gecis3_spin && yuklemeSayisi >= gecis3_yukleme)
                    AsamaGecir(SenaryoAsama.Asama4_Matematik);
                break;
            case SenaryoAsama.Asama4_Matematik:
                if (toplamKayip >= gecis4_kayip && mevcutBakiye <= gecis4_bakiye && toplamSpin >= gecis4_spin && yuklemeSayisi >= gecis4_yukleme)
                    AsamaGecir(SenaryoAsama.Asama5_Finale);
                break;
        }
    }

    public void AsamaGecir(SenaryoAsama yeni)
    {
        if (mevcutAsama == yeni) return;
        Debug.Log($"[SENARYO] Aşama: {mevcutAsama} -> {yeni}");
        mevcutAsama = yeni;
        UI_Guncelle();
        GecisSartGuncelle();
    }

    public void ManuelGecis(int asama) => AsamaGecir((SenaryoAsama)asama);

    public float GetKazancOrani()
    {
        switch (mevcutAsama)
        {
            case SenaryoAsama.Asama1_Balayi: return asama1_oran / 100f;
            case SenaryoAsama.Asama2_Aliskanlik: return asama2_oran / 100f;
            case SenaryoAsama.Asama3_KayipKovalama: return asama3_oran / 100f;
            case SenaryoAsama.Asama4_Matematik: return asama4_oran / 100f;
            case SenaryoAsama.Asama5_Finale: return asama5_oran / 100f;
            default: return 0.5f;
        }
    }

    public string GetAsamaAdi()
    {
        switch (mevcutAsama)
        {
            case SenaryoAsama.Asama1_Balayi: return "1 - Balayı";
            case SenaryoAsama.Asama2_Aliskanlik: return "2 - Alışkanlık";
            case SenaryoAsama.Asama3_KayipKovalama: return "3 - Kayıp Kovalama";
            case SenaryoAsama.Asama4_Matematik: return "4 - Matematik";
            case SenaryoAsama.Asama5_Finale: return "5 - Finale";
            default: return "Bilinmiyor";
        }
    }

    public void SpinTamamlandi(int kazanc, int bahis)
    {
        toplamSpin++;
        if (kazanc > bahis) toplamKazanc += (kazanc - bahis);
        else toplamKayip += (bahis - kazanc);

        if (GameManager.I?.ActivePlayer != null)
            mevcutBakiye = GameManager.I.ActivePlayer.balance;

        UI_Guncelle();
        if (senaryoAktif && !gelistirmeModu) AsamaGecisiKontrol();
    }

    public void BakiyeYukle(int tutar)
    {
        if (yuklemeSayisi >= 3) return;
        mevcutBakiye += tutar;
        yuklemeSayisi++;
        UI_Guncelle();
    }

    public void AutoSpinBaslat()
    {
        int secim = autoSpinDropdown != null ? autoSpinDropdown.value : 3;
        if (secim == 5) { autoSpinAktif = true; kalanAutoSpin = -1; }
        else { autoSpinAktif = true; kalanAutoSpin = new int[] { 10, 25, 50, 100, 200 }[secim]; }
        UI_Guncelle();
    }

    public void AutoSpinDurdur()
    {
        autoSpinAktif = false;
        kalanAutoSpin = 0;
        UI_Guncelle();
    }

    System.Collections.IEnumerator AutoSpinRoutine()
    {
        coroutineCalisiyor = true;
        var oyun = FindObjectOfType<OyunYoneticisi>();
        if (oyun != null) oyun.SpinButon();
        yield return new WaitForSeconds(0.5f);
        if (kalanAutoSpin > 0) kalanAutoSpin--;
        if (kalanAutoSpin == 0) AutoSpinDurdur();
        coroutineCalisiyor = false;
    }

    void GecisSartGuncelle()
    {
        if (gecisSartText == null) return;
        string s = "";
        switch (mevcutAsama)
        {
            case SenaryoAsama.Asama1_Balayi: s = $"Kazanc≥{gecis1_kazanc} TL\nSpin≥{gecis1_spin}"; break;
            case SenaryoAsama.Asama2_Aliskanlik: s = $"Kayip≥{gecis2_kayip} TL\nBakiye≤{gecis2_bakiye}"; break;
            case SenaryoAsama.Asama3_KayipKovalama: s = $"Kayip≥{gecis3_kayip} TL\nBakiye≤{gecis3_bakiye}"; break;
            case SenaryoAsama.Asama4_Matematik: s = $"Kayip≥{gecis4_kayip} TL\nBakiye≤{gecis4_bakiye}"; break;
            case SenaryoAsama.Asama5_Finale: s = "SON AŞAMA"; break;
        }
        gecisSartText.text = s;
    }

    public void UI_Guncelle()
    {
        if (asamaText) asamaText.text = $"Aşama: {GetAsamaAdi()}";
        if (spinText) spinText.text = $"Spin: {toplamSpin}";
        if (kazancText) kazancText.text = $"Net: {toplamKazanc - toplamKayip:N0} TL";
        if (bakiyeText) bakiyeText.text = $"Bakiye: {mevcutBakiye:N0} TL";
        if (mevcutAsamaText) mevcutAsamaText.text = GetAsamaAdi();

        if (autoSpinBaslatBtn) autoSpinBaslatBtn.interactable = !autoSpinAktif;
        if (autoSpinDurdurBtn) autoSpinDurdurBtn.interactable = autoSpinAktif;
        if (autoSpinKalanText) autoSpinKalanText.text = autoSpinAktif ? (kalanAutoSpin == -1 ? "Sonsuz" : $"Kalan: {kalanAutoSpin}") : "";

        GecisSartGuncelle();
    }

    public void Reset()
    {
        mevcutAsama = SenaryoAsama.Asama1_Balayi;
        toplamSpin = toplamKazanc = toplamKayip = 0;
        mevcutBakiye = ilkBakiye;
        baslangicZamani = Time.time;
        yuklemeSayisi = 1;
        AutoSpinDurdur();
        UI_Guncelle();
    }
}
