using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// 55 spinlik otomatik senaryo akışı. YoneticiModu=false iken çalışır.
/// Oyun sahnesine RuntimeInitializeOnLoadMethod ile otomatik eklenir.
/// HUD'a "Oturumu Bitir" butonu ve spin sayacı da bu script tarafından eklenir.
/// </summary>
public class SenaryoOtomatikAkis : MonoBehaviour
{
    public const int TOPLAM_SPIN_LIMITI = 55;

    // Senaryo eşikleri (spin aralığı → [odemeEgilimi, maxOdeme])
    private static readonly int[] EsikSpin = { 1, 11, 26, 36, 51, 56 };
    private static readonly int[] Egilim   = { 85, 25, 45, 15, 50 };
    private static readonly int[] MaxOdeme = { 1000, 100, 200, 50, 300 };
    private static readonly string[] SenaryoAd = { "hook", "yontma", "tutma", "koruma", "normal" };

    private int            _oncekiSegment = -1;
    private OyunYoneticisi _oy;
    private TextMeshProUGUI _sayacText;
    private bool           _logAcildi;

    // ── Oyun sahnesine otomatik eklenir ─────────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoSpawn()
    {
        // SpinTestAraci aktifken otomatik akış spawn edilmez — test runner kontrol etsin.
        if (PlayerPrefs.GetInt("SpinTest_Aktif", 0) == 1)
        {
            Debug.Log("[SenaryoOtomatikAkis] SpinTest_Aktif=1, otomatik akış DEVRE DIŞI.");
            return;
        }
        string sahne = SceneManager.GetActiveScene().name;
        if (sahne != "06_AdminOyunKopya") return;
        var go = new GameObject("[OturumHUD]");
        DontDestroyOnLoad(go);
        go.AddComponent<SenaryoOtomatikAkis>();
    }

    void Start()
    {
        _oy = FindObjectOfType<OyunYoneticisi>();
        if (_oy == null)
            Debug.LogWarning("[SenaryoOtomatikAkis] OyunYoneticisi bulunamadı.");

        SenaryoYoneticisi.OnSpinTamamlandiEvent += OnSpinTamamlandi;

        BakiyeyiBaslat();
        HUDOlustur();
        SenaryoUygula(0);
    }

    void OnDestroy()
    {
        SenaryoYoneticisi.OnSpinTamamlandiEvent -= OnSpinTamamlandi;
    }

    void BakiyeyiBaslat()
    {
        if (OturumKayitcisi.BaslangicBakiyesi > 0f) return;
        int bak = GameManager.I?.ActivePlayer != null ? GameManager.I.ActivePlayer.balance : 0;
        if (bak > 0) OturumKayitcisi.BaslangicBakiyesi = bak;
    }

    // ── Spin tamamlandı hook ────────────────────────────────────────
    void OnSpinTamamlandi(int toplamSpin, int kazanc, int bahis)
    {
        if (_sayacText != null)
            _sayacText.text = $"Spin: {toplamSpin}/{TOPLAM_SPIN_LIMITI}";

        if (!KullaniciVerileri.YoneticiModu)
            SenaryoUygula(toplamSpin);

        if (!_logAcildi && toplamSpin >= TOPLAM_SPIN_LIMITI)
        {
            _logAcildi = true;
            LogEkrani.Ac();
        }
    }

    // ── Segment seçimi ve uygulama ────────────────────────────────
    void SenaryoUygula(int spinNo)
    {
        int seg = SegmentBul(spinNo);
        if (seg == _oncekiSegment) return;
        _oncekiSegment = seg;

        if (_oy != null)
        {
            _oy.AdminSetOdemeEgilimi(Egilim[seg]);
            _oy.AdminSetMaxOdeme(MaxOdeme[seg]);
        }

        OturumKayitcisi.EkleEvent(OturumKayitcisi.OlayTipi_SenaryoGecisi,
            $"senaryo={SenaryoAd[seg]} egilim={Egilim[seg]} max={MaxOdeme[seg]}",
            spinNo);

        Debug.Log($"[OtomatikAkis] Senaryo={SenaryoAd[seg]} | Spin={spinNo} | Egilim={Egilim[seg]}% | Max={MaxOdeme[seg]}TL");
    }

    static int SegmentBul(int spin)
    {
        for (int i = 0; i < EsikSpin.Length - 1; i++)
            if (spin < EsikSpin[i + 1]) return i;
        return EsikSpin.Length - 2;
    }

    // ── HUD buton ve sayaç ────────────────────────────────────────
    void HUDOlustur()
    {
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        // Oturumu Bitir butonu
        var btnGo = new GameObject("[OturumBitirBtn]", typeof(RectTransform));
        btnGo.transform.SetParent(canvas.transform, false);
        btnGo.transform.SetAsLastSibling();
        var btnRt = btnGo.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(1f, 1f); btnRt.anchorMax = new Vector2(1f, 1f);
        btnRt.pivot     = new Vector2(1f, 1f);
        btnRt.anchoredPosition = new Vector2(-12f, -12f);
        btnRt.sizeDelta = new Vector2(120f, 32f);

        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(226f/255f, 75f/255f, 74f/255f, .15f);

        // İnce kırmızı kenarlık
        var brdGo = new GameObject("BtnBrd", typeof(RectTransform), typeof(Image));
        brdGo.transform.SetParent(btnGo.transform, false);
        var bRt2 = brdGo.GetComponent<RectTransform>();
        bRt2.anchorMin = Vector2.zero; bRt2.anchorMax = Vector2.one;
        bRt2.offsetMin = Vector2.zero; bRt2.offsetMax = Vector2.zero;
        brdGo.GetComponent<Image>().color = new Color(226f/255f, 75f/255f, 74f/255f, .40f);
        brdGo.GetComponent<Image>().raycastTarget = false;

        var içGo = new GameObject("BtnIc", typeof(RectTransform), typeof(Image));
        içGo.transform.SetParent(btnGo.transform, false);
        var iRt = içGo.GetComponent<RectTransform>();
        iRt.anchorMin = Vector2.zero; iRt.anchorMax = Vector2.one;
        iRt.offsetMin = new Vector2(1f, 1f); iRt.offsetMax = new Vector2(-1f, -1f);
        içGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, .20f);
        içGo.GetComponent<Image>().raycastTarget = false;

        var btnTxt = new GameObject("BtnTxt", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTxt.transform.SetParent(btnGo.transform, false);
        var tRt = btnTxt.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
        tRt.offsetMin = Vector2.zero; tRt.offsetMax = Vector2.zero;
        var t = btnTxt.GetComponent<TextMeshProUGUI>();
        t.text = "Oturumu Bitir"; t.fontSize = 12;
        t.color = new Color(247f/255f, 193f/255f, 193f/255f, 1f);
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false;

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(LogEkrani.Ac);

        // Spin sayacı
        var sayacGo = new GameObject("[SpinSayac]", typeof(RectTransform), typeof(TextMeshProUGUI));
        sayacGo.transform.SetParent(canvas.transform, false);
        sayacGo.transform.SetAsLastSibling();
        var sRt = sayacGo.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(1f, 1f); sRt.anchorMax = new Vector2(1f, 1f);
        sRt.pivot = new Vector2(1f, 1f);
        sRt.anchoredPosition = new Vector2(-12f, -50f);
        sRt.sizeDelta = new Vector2(120f, 20f);
        _sayacText = sayacGo.GetComponent<TextMeshProUGUI>();
        _sayacText.text = $"Spin: 0/{TOPLAM_SPIN_LIMITI}";
        _sayacText.fontSize = 11;
        _sayacText.color = new Color(1f, 1f, 1f, .50f);
        _sayacText.alignment = TextAlignmentOptions.Right;
        _sayacText.raycastTarget = false;

        Debug.Log("[SenaryoOtomatikAkis] HUD oluşturuldu.");
    }
}
