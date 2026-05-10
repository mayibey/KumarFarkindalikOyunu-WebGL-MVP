using System;
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
/// - Save varsa input pre-fill ile eski ad gösterilir; kullanıcı aynı adı bırakıp BAŞLA'ya basarsa
///   restore moduna girilir, farklı ad yazıp basarsa eski save silinir, sıfırdan başlar
/// - misafirButton sahne prefabında var ama runtime'da gizlenir (tek click handler — BAŞLA)
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

        // Misafir butonu kaldırıldı (UX sadeleştirme) — sahne prefabı dokunulmuyor, runtime'da gizleniyor.
        if (misafirButton != null) misafirButton.gameObject.SetActive(false);

        if (baslaButton != null) baslaButton.onClick.AddListener(BaslaTiklandi);
    }

    void Start()
    {
        StartCoroutine(FadeIn());

        // Save varsa: input'a eski ad pre-fill (kullanıcı görsün, isterse aynısını bırakıp BAŞLA → restore).
        // Save yoksa: input boş başlar, kullanıcı yeni ad yazıp BAŞLA → sıfırdan.
        if (SaveLoadServisi.VarMi())
        {
            var save = SaveLoadServisi.Load();
            if (save != null && !string.IsNullOrEmpty(save.kullaniciAdi))
            {
                if (isimInput != null) isimInput.text = save.kullaniciAdi;
                Debug.Log($"[KullaniciAdiModalKontrol] Save bulundu — input pre-filled='{save.kullaniciAdi}' (aynı kalırsa restore, değişirse sıfırdan).");
            }
        }

        if (isimInput != null) isimInput.ActivateInputField();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            BaslaTiklandi();
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

    /// <summary>
    /// Tek click handler — input'tan girilen adı save ile karşılaştırır:
    ///   - save != null && girilenAd == save.kullaniciAdi → restore (KumarRestoreModuActif=1)
    ///   - save varsa ama girilenAd farklı → save sil + sıfırdan
    ///   - save yok → sıfırdan
    /// 3 yere senkron yazar (KullaniciVerileri statik + PlayerPrefs + GameManager.ActivePlayer.playerName).
    /// </summary>
    void BaslaTiklandi()
    {
        string girilenAd = isimInput != null ? isimInput.text.Trim() : "";
        if (string.IsNullOrEmpty(girilenAd)) girilenAd = "Misafir";

        // Defansif: önceki sahneden kalan restore flag'i temizle (her durumda yeniden değerlendirilecek).
        PlayerPrefs.DeleteKey("KumarRestoreModuActif");

        var save = SaveLoadServisi.VarMi() ? SaveLoadServisi.Load() : null;
        bool ayniAd = save != null
            && !string.IsNullOrEmpty(save.kullaniciAdi)
            && string.Equals(save.kullaniciAdi.Trim(), girilenAd, StringComparison.Ordinal);

        if (ayniAd)
        {
            // Restore: aynı ad → eski oturumdan devam.
            IsmiSenkronYaz(girilenAd);
            PlayerPrefs.SetInt("KumarRestoreModuActif", 1);
            PlayerPrefs.Save();
            Debug.Log($"[KullaniciAdiModalKontrol] BAŞLA — '{girilenAd}' aynı ad, restore mode aktif → {hedefSahne}");
        }
        else
        {
            // Sıfırdan: save varsa farklı ad demek, eski save'i sil.
            if (save != null)
            {
                SaveLoadServisi.Sil();
                Debug.Log($"[KullaniciAdiModalKontrol] BAŞLA — '{girilenAd}' farklı ad (eski='{save.kullaniciAdi}'), save silindi.");
            }
            IsmiSenkronYaz(girilenAd);
            PlayerPrefs.Save();
            Debug.Log($"[KullaniciAdiModalKontrol] BAŞLA — '{girilenAd}' sıfırdan → {hedefSahne}");
        }

        SceneManager.LoadScene(hedefSahne);
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
}
