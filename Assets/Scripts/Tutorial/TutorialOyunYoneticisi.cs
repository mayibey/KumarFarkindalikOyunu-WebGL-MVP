using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

namespace KumarFarkindalik.Tutorial
{
    /// <summary>
    /// 04_AdminOyunScene (build idx 3) için ana koordinatör. Self-spawn pattern
    /// (ScriptedModalKopru ile aynı) — sahne yüklendiğinde otomatik oluşur.
    /// Awake'de TutorialAdimYoneticisi + TutorialAdminEnjeksiyonu component'lerini
    /// kendi GameObject'ine AddComponent eder.
    ///
    /// PAKET 3A (söküm sonrası):
    ///   - Unity Canvas admin paneli ARTIK KULLANILMIYOR (panel.html iframe paneli kullanılacak — Paket 3B)
    ///   - AyarlarButton'a tıklayınca sahne YAML PersistentCall ile PanelKopru.AyarlarButonunaBasildi
    ///     zaten panel.html'i açıyor → runtime onClick.AddListener GEREKSİZ
    ///   - TutorialAdimPaneli + TutorialHighlight + TutorialAdminPanelKilidi SİLİNDİ
    ///     (Tutorial UI'ı Paket 3B'de panel.html JS tarafında implement edilecek)
    /// </summary>
    [Preserve]
    public class TutorialOyunYoneticisi : MonoBehaviour
    {
        public const int TUTORIAL_SAHNE_BUILD_INDEX = 3; // 04_AdminOyunScene

        public static TutorialOyunYoneticisi Ornek { get; private set; }

        public TutorialAdimYoneticisi AdimYoneticisi { get; private set; }
        public TutorialAdminEnjeksiyonu Enjeksiyon { get; private set; }

        [Preserve]
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OtomatikInit()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            var aktifSahne = SceneManager.GetActiveScene();
            if (aktifSahne.buildIndex == TUTORIAL_SAHNE_BUILD_INDEX)
                OnSceneLoaded(aktifSahne, LoadSceneMode.Single);
        }

        [Preserve]
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.buildIndex != TUTORIAL_SAHNE_BUILD_INDEX)
            {
                if (Ornek != null) Object.Destroy(Ornek.gameObject);
                return;
            }
            if (Ornek != null) return;
            var go = new GameObject(nameof(TutorialOyunYoneticisi));
            go.AddComponent<TutorialOyunYoneticisi>();
        }

        private void Awake()
        {
            if (SceneManager.GetActiveScene().buildIndex != TUTORIAL_SAHNE_BUILD_INDEX)
            {
                gameObject.SetActive(false);
                return;
            }
            if (Ornek != null && Ornek != this) { Destroy(gameObject); return; }
            Ornek = this;

            AdimYoneticisi = gameObject.AddComponent<TutorialAdimYoneticisi>();
            Enjeksiyon = gameObject.AddComponent<TutorialAdminEnjeksiyonu>();

            Debug.Log("[TutorialOyunYoneticisi] Spawn + AdimYoneticisi/Enjeksiyon AddComponent edildi.");
        }

        private void OnDestroy()
        {
            if (Ornek == this) Ornek = null;
        }
    }
}
