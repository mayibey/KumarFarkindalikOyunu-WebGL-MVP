using Senaryo.Scripted;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

namespace KumarFarkindalik.Tutorial
{
    /// <summary>
    /// 03_SenaryoluOyun sahnesinde A7 final ekranını izler. ScriptedFinalEkrani.IsAcik
    /// false→true→false geçişini yakaladığında ScriptedTutorialGecisEkrani.Ornek?.Goster() çağırır.
    ///
    /// PAKET 2: Watcher SADECE okuma yapar — ScriptedFinalEkrani'ne event/listener bağlanmaz, hiçbir
    /// public/private alanı yazılmaz. Self-spawn pattern (RuntimeInitializeOnLoadMethod + sceneLoaded)
    /// ScriptedModalKopru:32-77 mimari taklit. Sahne dosyasına yazılmaz, runtime'da oluşur.
    /// </summary>
    [Preserve]
    public class TutorialFinalWatcher : MonoBehaviour
    {
        public const int ANLATICI_SAHNE_BUILD_INDEX = 2; // 03_SenaryoluOyun

        public static TutorialFinalWatcher Ornek { get; private set; }

        private enum WatcherState
        {
            /// <summary>Final henüz açılmadı (initial state veya tetikten sonra reset).</summary>
            Beklemede,
            /// <summary>Final açıldı, kapanmasını bekliyoruz.</summary>
            Acildi,
        }

        private WatcherState _state = WatcherState.Beklemede;
        private bool _sonIsAcik;

        [Preserve]
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OtomatikInit()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            var aktifSahne = SceneManager.GetActiveScene();
            if (aktifSahne.buildIndex == ANLATICI_SAHNE_BUILD_INDEX)
                OnSceneLoaded(aktifSahne, LoadSceneMode.Single);
        }

        [Preserve]
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 03 dışı sahnelerde: mevcut instance varsa Destroy.
            if (scene.buildIndex != ANLATICI_SAHNE_BUILD_INDEX)
            {
                if (Ornek != null) Object.Destroy(Ornek.gameObject);
                return;
            }
            if (Ornek != null) return;
            var go = new GameObject(nameof(TutorialFinalWatcher));
            go.AddComponent<TutorialFinalWatcher>();
        }

        private void Awake()
        {
            if (SceneManager.GetActiveScene().buildIndex != ANLATICI_SAHNE_BUILD_INDEX)
            {
                gameObject.SetActive(false);
                return;
            }
            if (Ornek != null && Ornek != this) { Destroy(gameObject); return; }
            Ornek = this;
            _state = WatcherState.Beklemede;
            _sonIsAcik = false;
        }

        private void OnDestroy()
        {
            if (Ornek == this) Ornek = null;
        }

        private void Update()
        {
            // ScriptedFinalEkrani null-safe okuma. Spawn olmadıysa IsAcik default false (state machine korur).
            bool simdiAcik = ScriptedFinalEkrani.IsAcik;
            if (simdiAcik == _sonIsAcik) return;

            // Geçiş yakalandı:
            if (!_sonIsAcik && simdiAcik)
            {
                // false → true (final ekran açıldı)
                _state = WatcherState.Acildi;
            }
            else if (_sonIsAcik && !simdiAcik)
            {
                // true → false (final ekran kapandı — TAMAM tıklandı veya sahne reload başlamadan)
                if (_state == WatcherState.Acildi)
                {
                    Debug.Log("[TutorialFinalWatcher] A7 final kapandı → ScriptedTutorialGecisEkrani açılıyor.");
                    ScriptedTutorialGecisEkrani.Ornek?.Goster();
                }
                _state = WatcherState.Beklemede; // tekrar A7'ye gelirse yine tetiklensin
            }
            _sonIsAcik = simdiAcik;
        }
    }
}
