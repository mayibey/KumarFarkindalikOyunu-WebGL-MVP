using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Globalization;

public class KasaYoneticisi : MonoBehaviour
{
    private const string PP_ANA = "PP_ANA_KASA_TL";
    private const string PP_HAVUZ = "PP_ODUL_HAVUZU_TL";
    private const string PP_ANA_KAP = "PP_ANA_KASA_KAP";
    private const string PP_HAVUZ_KAP = "PP_ODUL_HAVUZU_KAP";
    private void OnApplicationQuit()
    {
        SaveKasalar();
    }

    private void SaveKasalar()
    {
        PlayerPrefs.SetString(PP_ANA, anaKasaTL.ToString());
        PlayerPrefs.SetString(PP_HAVUZ, odulHavuzuTL.ToString());

        PlayerPrefs.SetString(PP_ANA_KAP, anaKasaGorselKapasiteTL.ToString());
        PlayerPrefs.SetString(PP_HAVUZ_KAP, odulHavuzuGorselKapasiteTL.ToString());

        PlayerPrefs.Save();
    }

    private void LoadKasalar()
    {
        if (PlayerPrefs.HasKey(PP_ANA))
        {
            long.TryParse(PlayerPrefs.GetString(PP_ANA), out anaKasaTL);
        }
        if (PlayerPrefs.HasKey(PP_HAVUZ))
        {
            long.TryParse(PlayerPrefs.GetString(PP_HAVUZ), out odulHavuzuTL);
        }

        if (PlayerPrefs.HasKey(PP_ANA_KAP))
        {
            long.TryParse(PlayerPrefs.GetString(PP_ANA_KAP), out anaKasaGorselKapasiteTL);
        }
        if (PlayerPrefs.HasKey(PP_HAVUZ_KAP))
        {
            long.TryParse(PlayerPrefs.GetString(PP_HAVUZ_KAP), out odulHavuzuGorselKapasiteTL);
        }
    }



    [Header("Kasa De�erleri (TL)")]
    public long anaKasaTL = 0;       // hi� eksilmez
    public long odulHavuzuTL = 0;    // �demeler buradan yap�l�r

    [Header("UI Text")]
    public TextMeshProUGUI anaKasaText;
    public TextMeshProUGUI odulHavuzuText;

    [Header("G�rsel Doluluk (Image Type=Filled)")]
    public Image anaKasaFillImage;
    public Image odulHavuzuFillImage;

    [Header("G�rsel Kapasite (FillAmount i�in referans)")]
    public long anaKasaGorselKapasiteTL = 100000;
    public long odulHavuzuGorselKapasiteTL = 50000;

    [Header("Admin: Elle De�i�tirme (Opsiyonel)")]
    public TMP_InputField anaKasaInput;
    public TMP_InputField odulHavuzuInput;
    public Button applyButton;

    [Header("�deme Politikas�")]
    [Tooltip("�d�l havuzu yetmezse �deme s�f�r m� olsun? (true=Hi� �deme yapma)  false=Ne varsa onu �de")]
    public bool havuzYetmezseOdemeSifirla = false;

    [Header("�d�l Havuzu / Bonus B�t�e (OY Sync okur)")]
    public bool BonusBudgetAktif = true;
    [Range(0f, 1f)] public float BonusBudgetHavuzOran = 0.25f;
    public int BonusBudgetMinTL = 0;
    public int BonusBudgetMaxTL = 60000;
    [Range(0f, 1f)] public float BonusMaxOdemeHavuzOrani = 0.10f;

    [Header("Kasa Bazl? Denge")]
    public bool KasaBazliDengeAktif = true;
    public int MinClusterSize_HavuzBos = 999;
    public int MinClusterSize_HavuzDolu = 6;
    public int MinClusterSize_HavuzAz = 12;
    [Range(0f, 1f)] public float HavuzAzEsik01 = 0.15f;
    [Range(0f, 1f)] public float HavuzDoluEsik01 = 0.70f;

    [Header("Bonus Otomatik Zorluk")]
    public bool BonusOtoZorlukAktif = true;
    public int BonusMinCluster_Easy = 6;
    public int BonusMinCluster_Hard = 14;

    private readonly CultureInfo tr = new CultureInfo("tr-TR");

    private void Awake()
    {
        if (applyButton != null)
        {
            applyButton.onClick.RemoveListener(ApplyFromInputs);
            applyButton.onClick.AddListener(ApplyFromInputs);
        }
        LoadKasalar();

        UI_Guncelle();
    }

    // ----------------------------
    // PARA G�R���
    // ----------------------------
    public void ParaGirisi_BolVeEkle(int tutarTL)
    {
        if (tutarTL <= 0) return;

        long anaPay = tutarTL / 2;
        long havuzPay = tutarTL - anaPay;

        anaKasaTL += anaPay;
        odulHavuzuTL += havuzPay;

        UI_Guncelle();
    }

    // ----------------------------
    // �DEME
    // ----------------------------
    /// <returns>Ger�ekten �denen TL</returns>
    public int OdemeYap_OdulHavuzundan(int istenenTL)
    {
        if (istenenTL <= 0) { UI_Guncelle(); return 0; }

        if (odulHavuzuTL <= 0)
        {
            UI_Guncelle();
            return 0;
        }

        // Yetmiyorsa:
        if (odulHavuzuTL < istenenTL)
        {
            if (havuzYetmezseOdemeSifirla)
            {
                UI_Guncelle();
                return 0;
            }

            // �Ne varsa onu �de�
            int odenen = (int)Mathf.Clamp((float)odulHavuzuTL, 0, int.MaxValue);
            odulHavuzuTL = 0;
            UI_Guncelle();
            return odenen;
        }

        // Yetiyorsa tam �de
        odulHavuzuTL -= istenenTL;
        UI_Guncelle();
        return istenenTL;
    }

    // ----------------------------
    // ADMIN: ELLE SET
    // ----------------------------
    public void SetAnaKasa(long yeniTL)
    {
        anaKasaTL = Mathf.Max(0, (float)yeniTL) > 0 ? yeniTL : 0;
        UI_Guncelle();
    }

    public void SetOdulHavuzu(long yeniTL)
    {
        odulHavuzuTL = Mathf.Max(0, (float)yeniTL) > 0 ? yeniTL : 0;
        UI_Guncelle();
    }

    public void ApplyFromInputs()
    {
        if (anaKasaInput != null)
            anaKasaTL = Mathf.Max(0, (float)ParseLongSafe(anaKasaInput.text)) > 0 ? ParseLongSafe(anaKasaInput.text) : 0;

        if (odulHavuzuInput != null)
            odulHavuzuTL = Mathf.Max(0, (float)ParseLongSafe(odulHavuzuInput.text)) > 0 ? ParseLongSafe(odulHavuzuInput.text) : 0;

        UI_Guncelle();
    }

    // ----------------------------
    // ORANLAR (3. ad�mda kullanaca��z)
    // ----------------------------
    /// <summary>�d�l havuzu doluluk oran� (0..1)</summary>
    public float OdulHavuzuOran01()
    {
        if (odulHavuzuGorselKapasiteTL <= 0) return 0f;
        return Mathf.Clamp01((float)odulHavuzuTL / (float)odulHavuzuGorselKapasiteTL);
    }

    /// <summary>Ana kasa doluluk oran� (0..1)</summary>
    public float AnaKasaOran01()
    {
        if (anaKasaGorselKapasiteTL <= 0) return 0f;
        return Mathf.Clamp01((float)anaKasaTL / (float)anaKasaGorselKapasiteTL);
    }

    // ----------------------------
    // UI
    // ----------------------------
    public void UI_Guncelle()
    {
        if (anaKasaText != null)
            anaKasaText.text = $"ANA KASA\n{anaKasaTL.ToString("N0", tr)} TL";

        if (odulHavuzuText != null)
            odulHavuzuText.text = $"�D�L HAVUZU\n{odulHavuzuTL.ToString("N0", tr)} TL";

        if (anaKasaFillImage != null)
            anaKasaFillImage.fillAmount = KapasiteyeGoreFill(anaKasaTL, anaKasaGorselKapasiteTL);

        if (odulHavuzuFillImage != null)
            odulHavuzuFillImage.fillAmount = KapasiteyeGoreFill(odulHavuzuTL, odulHavuzuGorselKapasiteTL);
        SaveKasalar();

    }

    private float KapasiteyeGoreFill(long deger, long kapasite)
    {
        if (kapasite <= 0) return 0f;
        return Mathf.Clamp01((float)deger / (float)kapasite);
    }

    private long ParseLongSafe(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0;
        s = s.Replace(".", "").Replace(",", "").Replace("TL", "").Replace(" ", "");
        long v;
        if (long.TryParse(s, out v)) return v;
        return 0;
    }
}
