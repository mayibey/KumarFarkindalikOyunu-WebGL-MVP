using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 02_KullaniciAdiModal sahnesinin kontrolü:
/// - Modal CanvasGroup fade-in + scale 0.9→1.0
/// - InputField focus, Enter ile BAŞLA tetikleme
/// - PlayerPrefs'e KullaniciAdi kaydet, sonra hedefSahne'ye geç
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
        if (isimInput != null) isimInput.ActivateInputField();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            BaslaTiklandi();
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

    void BaslaTiklandi()
    {
        string isim = isimInput != null ? isimInput.text.Trim() : "";
        if (string.IsNullOrEmpty(isim)) isim = "Misafir";
        PlayerPrefs.SetString("KullaniciAdi", isim);
        PlayerPrefs.Save();
        Debug.Log($"[KullaniciAdiModalKontrol] KullaniciAdi='{isim}' kaydedildi → {hedefSahne}");
        SceneManager.LoadScene(hedefSahne);
    }

    void MisafirTiklandi()
    {
        PlayerPrefs.SetString("KullaniciAdi", "Misafir");
        PlayerPrefs.Save();
        Debug.Log($"[KullaniciAdiModalKontrol] KullaniciAdi='Misafir' kaydedildi → {hedefSahne}");
        SceneManager.LoadScene(hedefSahne);
    }
}
