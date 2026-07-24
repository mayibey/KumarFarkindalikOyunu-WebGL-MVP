using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Sunum (SunumPaketi/sunum.html) → oyun köprüsü. Sahneden bağımsız, kendi kendine yüklenir
/// (RuntimeInitializeOnLoadMethod + DontDestroyOnLoad; sahne dosyalarına dokunmaz).
///
/// JS çağrısı: unityInstance.SendMessage("SunumKoprusu", "SunumAsamaGit", asamaNo)  (asamaNo 0-6)
///
/// Davranış:
///  - Oyun senaryo sahnesinde DEĞİLSE: isim modalına takılmadan ("Misafir", eski save silinir)
///    02_SenaryoluOyun yüklenir; anlatıcı hazır olunca istenen aşamaya atlanır (0 = doğal başlangıç).
///  - Oyun zaten senaryodaysa: aşama 0 → YenidenBaslat (tam reset), diğerleri → HtmlAsamaDegisti
///    (anlatıcı panelindeki ◀ ▶ okları ile aynı mekanizma).
/// </summary>
public class SunumKoprusu : MonoBehaviour
{
    private const string SENARYO_SAHNE = "02_SenaryoluOyun";
    private const string PANEL_SAHNE = "03_AdminOyunScene";
    private static SunumKoprusu _ornek;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Olustur()
    {
        if (_ornek != null) return;
        var go = new GameObject("SunumKoprusu");
        DontDestroyOnLoad(go);
        _ornek = go.AddComponent<SunumKoprusu>();
        Debug.Log("[SunumKoprusu] Hazır — SendMessage('SunumKoprusu','SunumAsamaGit', n) bekleniyor.");
    }

    public void SunumAsamaGit(int asama)
    {
        asama = Mathf.Clamp(asama, 0, 6);
        Debug.Log($"[SunumKoprusu] SunumAsamaGit({asama}) — aktif sahne: {SceneManager.GetActiveScene().name}");
        StopAllCoroutines();
        StartCoroutine(AsamaGitCo(asama));
    }

    /// <summary>
    /// JS çağrısı: unityInstance.SendMessage("SunumKoprusu", "SunumPanelGit", 0)
    /// Sunumun "Perde Arkası" slaytındaki butondan gelir; manipülasyon paneline geçer
    /// (giriş ekranındaki MANİPÜLASYON PANELİNE GİT butonu ile aynı yol, şifresiz).
    /// </summary>
    public void SunumPanelGit(int _)
    {
        Debug.Log($"[SunumKoprusu] SunumPanelGit — aktif sahne: {SceneManager.GetActiveScene().name}");
        StopAllCoroutines();
        if (SceneManager.GetActiveScene().name == PANEL_SAHNE) return;
        if (GameManager.I != null)
            GameManager.I.LoadScene(PANEL_SAHNE);
        else
            SceneManager.LoadScene(PANEL_SAHNE, LoadSceneMode.Single);
    }

    /// <summary>
    /// JS çağrısı: unityInstance.SendMessage("SunumKoprusu", "SunumSesAyarla", acik)  (1 = ses açık, 0 = kapalı)
    /// Sunumdaki 🔊 düğmesi ve videolu slaytların otomatik susturması kullanır.
    /// AudioListener.volume globaldir; sahne değişse de ayar korunur.
    /// </summary>
    public void SunumSesAyarla(int acik)
    {
        AudioListener.volume = acik == 1 ? 1f : 0f;
        Debug.Log($"[SunumKoprusu] SunumSesAyarla({acik})");
    }

    private IEnumerator AsamaGitCo(int asama)
    {
        if (SceneManager.GetActiveScene().name != SENARYO_SAHNE)
        {
            // İsim modalına takılmadan taze senaryo: KullaniciAdiModalKontrol'ün "sıfırdan" yolunu taklit et.
            PlayerPrefs.DeleteKey("KumarRestoreModuActif");
            if (SaveLoadServisi.VarMi()) SaveLoadServisi.Sil();
            const string isim = "Misafir";
            KullaniciVerileri.KullaniciAdi = isim;
            PlayerPrefs.SetString("KullaniciAdi", isim);
            PlayerPrefs.Save();
            if (GameManager.I != null && GameManager.I.ActivePlayer != null)
                GameManager.I.ActivePlayer.playerName = isim;

            SceneManager.LoadScene(SENARYO_SAHNE, LoadSceneMode.Single);

            // Anlatıcı köprüsü hazır olana dek bekle (üst sınır 12 sn — WebGL ilk yükleme payı).
            float sinir = Time.realtimeSinceStartup + 12f;
            while (AnlaticiSeritKopru.Ornek == null && Time.realtimeSinceStartup < sinir)
                yield return null;
            yield return null; // Start()'lar otursun
            yield return null;

            if (asama > 0 && AnlaticiSeritKopru.Ornek != null)
                AnlaticiSeritKopru.Ornek.HtmlAsamaDegisti(asama);
            else if (AnlaticiSeritKopru.Ornek == null)
                Debug.LogWarning("[SunumKoprusu] AnlaticiSeritKopru hazır olmadı — aşama atlanamadı.");
        }
        else
        {
            var ask = AnlaticiSeritKopru.Ornek;
            if (ask == null)
            {
                Debug.LogWarning("[SunumKoprusu] Senaryo sahnesindeyiz ama AnlaticiSeritKopru yok.");
                yield break;
            }
            if (asama == 0) ask.YenidenBaslat();
            else ask.HtmlAsamaDegisti(asama);
        }
    }
}
