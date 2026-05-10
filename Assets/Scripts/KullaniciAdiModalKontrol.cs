using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 02_KullaniciAdiModal sahnesinin kontrolü:
/// - Modal CanvasGroup fade-in + scale 0.9→1.0
/// - InputField focus, Enter ile BAŞLA tetikleme
/// - PlayerPrefs + KullaniciVerileri.KullaniciAdi + GameManager.ActivePlayer.playerName senkron yaz
/// - Save varsa input pre-fill + iki buton "DEVAM ET" / "SIFIRDAN BAŞLA"
/// </summary>
public class KullaniciAdiModalKontrol : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private TMP_InputField isimInput;
    [SerializeField] private Button baslaButton;
    [SerializeField] private Button misafirButton;
    [SerializeField] private CanvasGroup modalCanvasGroup;
    [SerializeField] private RectTransform modalPanel;

    [Header("Geçiş")]
    [SerializeField] private string hedefSahne = "03_SenaryoluOyun";

    [Header("Animasyon")]
    [SerializeField] private float fadeSuresi = 0.4f;

    void Awake()
    {
        if (modalCanvasGroup != null) modalCanvasGroup.alpha = 0f;
        if (modalPanel != null) modalPanel.localScale = Vector3.one * 0.9f;

        if (baslaButton != null) baslaButton.onClick.AddListener(BaslaTiklandi);
        if (misafirButton != null) misafirButton.onClick.AddListener(MisafirTiklandi);
    }

    void Start()
    {
        StartCoroutine(FadeIn());

        // Save varsa: input pre-fill + buton metin/listener swap (DEVAM ET / SIFIRDAN BAŞLA).
        if (SaveLoadServisi.VarMi())
        {
            var save = SaveLoadServisi.Load();
            if (save != null && !string.IsNullOrEmpty(save.kullaniciAdi))
            {
                if (isimInput != null) isimInput.text = save.kullaniciAdi;

                ButonMetniDegistir(baslaButton, "DEVAM ET");
                ButonMetniDegistir(misafirButton, "SIFIRDAN BAŞLA");

                if (baslaButton != null)
                {
                    baslaButton.onClick.RemoveAllListeners();
                    baslaButton.onClick.AddListener(DevamEtTiklandi);
                }
                if (misafirButton != null)
                {
                    misafirButton.onClick.RemoveAllListeners();
                    misafirButton.onClick.AddListener(SifirdanBaslaTiklandi);
                }
                Debug.Log($"[KullaniciAdiModalKontrol] Save bulundu — input='{save.kullaniciAdi}', butonlar DEVAM ET / SIFIRDAN BAŞLA olarak ayarlandı.");
                return;  // input.ActivateInputField yok — kullanıcı zaten ismini görüyor
            }
        }

        if (isimInput != null) isimInput.ActivateInputField();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            // Buton listener'ı dinamik: DEVAM ET veya BaslaTiklandi — onClick.Invoke ile aktif handler tetiklenir.
            if (baslaButton != null) baslaButton.onClick.Invoke();
            else BaslaTiklandi();
        }
    }

    IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < fadeSuresi)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / fadeSuresi);
            if (modalCanvasGroup != null) modalCanvasGroup.alpha = p;
            if (modalPanel != null)
            {
                float s = Mathf.Lerp(0.9f, 1.0f, p);
                modalPanel.localScale = new Vector3(s, s, 1f);
            }
            yield return null;
        }
        if (modalCanvasGroup != null) modalCanvasGroup.alpha = 1f;
        if (modalPanel != null) modalPanel.localScale = Vector3.one;
    }

    /// <summary>İlk oyun veya "SIFIRDAN BAŞLA" sonrası — kullanıcı isim yazıp BAŞLA'ya bastı.
    /// 3 yere senkron yazar: KullaniciVerileri.KullaniciAdi (statik), PlayerPrefs, GameManager.ActivePlayer.playerName.</summary>
    void BaslaTiklandi()
    {
        string isim = isimInput != null ? isimInput.text.Trim() : "";
        if (string.IsNullOrEmpty(isim)) isim = "Misafir";
        IsmiSenkronYaz(isim);
        Debug.Log($"[KullaniciAdiModalKontrol] BAŞLA — '{isim}' kaydedildi → {hedefSahne}");
        SceneManager.LoadScene(hedefSahne);
    }

    /// <summary>"Misafir" butonu — eski kullanıcı save'i SİLİNMEZ (dönerse DEVAM ET hâlâ görünür).</summary>
    void MisafirTiklandi()
    {
        IsmiSenkronYaz("Misafir");
        Debug.Log($"[KullaniciAdiModalKontrol] MİSAFİR — kaydedildi (save silinmedi) → {hedefSahne}");
        SceneManager.LoadScene(hedefSahne);
    }

    /// <summary>Save varsa: kullanıcı ismini save'den alıp restore mode flag'i set ediyor;
    /// AnlaticiSeritKopru.Start bu flag'i görüp RestoreDurumYukle çağırıyor.</summary>
    void DevamEtTiklandi()
    {
        var save = SaveLoadServisi.Load();
        if (save == null)
        {
            Debug.LogWarning("[KullaniciAdiModalKontrol] DEVAM ET tıklandı ama save null/bozuk → BAŞLA fallback.");
            BaslaTiklandi();
            return;
        }
        IsmiSenkronYaz(save.kullaniciAdi);
        PlayerPrefs.SetInt("KumarRestoreModuActif", 1);
        PlayerPrefs.Save();
        Debug.Log($"[KullaniciAdiModalKontrol] DEVAM ET — '{save.kullaniciAdi}' restore mode aktif → {hedefSahne}");
        SceneManager.LoadScene(hedefSahne);
    }

    /// <summary>Save varsa: kullanıcı eski oturumu silip yeni oyuna başlıyor.</summary>
    void SifirdanBaslaTiklandi()
    {
        SaveLoadServisi.Sil();
        // Input'taki pre-fill kullanıcının yeni adı olabilir — onu kullan, boşsa "Misafir".
        Debug.Log("[KullaniciAdiModalKontrol] SIFIRDAN BAŞLA — save silindi, BAŞLA akışı.");
        BaslaTiklandi();
    }

    /// <summary>3 yere senkron yaz (Misafir bug fix 2026-04-29 + 2026-05-10).</summary>
    static void IsmiSenkronYaz(string isim)
    {
        if (string.IsNullOrWhiteSpace(isim)) isim = "Misafir";
        KullaniciVerileri.KullaniciAdi = isim;
        PlayerPrefs.SetString("KullaniciAdi", isim);
        PlayerPrefs.Save();
        if (GameManager.I != null && GameManager.I.ActivePlayer != null)
            GameManager.I.ActivePlayer.playerName = isim;
    }

    /// <summary>Buton içindeki tüm TMP_Text/Text bileşenlerinin metnini değiştirir (sahne hierarşisi bilinmediği için tarama).</summary>
    static void ButonMetniDegistir(Button btn, string yeniMetin)
    {
        if (btn == null) return;
        var tmps = btn.GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in tmps) t.text = yeniMetin;
        var legacys = btn.GetComponentsInChildren<Text>(true);
        foreach (var l in legacys) l.text = yeniMetin;
    }
}
