using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

namespace Senaryo.Scripted
{
    /// <summary>
    /// Bonus oyun sırasında sağ üst köşede HUD: kalan spin + oturum kazancı.
    ///
    /// WebGL build: Plugins/WebGL/ScriptedBonusHUD.jslib içindeki DOM manipülasyon fonksiyonları
    /// ile HTML overlay (Sweet Bonanza tarzı altın çerçeveli kutu).
    ///
    /// Editor: TextMeshPro fallback — sahnedeki "Kalan Spin Hakkı" ve "OTURUM KAZANCI" text'leri
    /// hâlâ güncellenir (mevcut çirkin görünüm; ama bilgi akışı kesintisiz).
    ///
    /// Sadece sahne 2 (03_SenaryoluOyun)'de aktif singleton.
    /// </summary>
    [Preserve]
    public class ScriptedBonusHUDKopru : MonoBehaviour
    {
        public const int ANLATICI_SAHNE_BUILD_INDEX = 2;
        public static ScriptedBonusHUDKopru Ornek { get; private set; }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void BonusHUDGoster();
        [DllImport("__Internal")] private static extern void BonusHUDGizle();
        [DllImport("__Internal")] private static extern void BonusHUDGuncelle(int spin, int kazanc);
#endif

        // Editor TMP fallback — sahnedeki mevcut text'leri runtime'da yakalar (ad bazlı arama).
        private TextMeshProUGUI _kalanSpinTmp;
        private TextMeshProUGUI _oturumKazancTmp;
        private bool _aktif;

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
            if (scene.buildIndex != ANLATICI_SAHNE_BUILD_INDEX) return;
            if (Ornek != null) return;
            var go = new GameObject(nameof(ScriptedBonusHUDKopru));
            go.AddComponent<ScriptedBonusHUDKopru>();
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
            BulTMPReferanslari();
        }

        private void OnDestroy()
        {
            if (Ornek == this) Ornek = null;
        }

        /// <summary>HUD'u açar ve ilk değerleri yazar (typically Goster(10, 0)).</summary>
        public void Goster(int kalanSpin, int oturumKazanci)
        {
            _aktif = true;
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                BonusHUDGoster();
                BonusHUDGuncelle(kalanSpin, oturumKazanci);
            }
            catch (System.Exception e) { Debug.LogError("[ScriptedBonusHUD] WebGL hata: " + e.Message); }
#else
            Debug.Log($"[ScriptedBonusHUD] Editor: HTML panel sadece WebGL'de. Spin={kalanSpin}, Kazanc={oturumKazanci}");
            FallbackTMPGuncelle(kalanSpin, oturumKazanci);
#endif
        }

        /// <summary>HUD'u kapatır.</summary>
        public void Gizle()
        {
            _aktif = false;
#if UNITY_WEBGL && !UNITY_EDITOR
            try { BonusHUDGizle(); }
            catch (System.Exception e) { Debug.LogError("[ScriptedBonusHUD] Gizle hata: " + e.Message); }
#else
            Debug.Log("[ScriptedBonusHUD] Gizle (Editor fallback).");
#endif
        }

        /// <summary>HUD'da kalan spin ve toplam oturum kazancını günceller.</summary>
        public void Guncelle(int kalanSpin, int oturumKazanci)
        {
            if (!_aktif) return;
#if UNITY_WEBGL && !UNITY_EDITOR
            try { BonusHUDGuncelle(kalanSpin, oturumKazanci); }
            catch (System.Exception e) { Debug.LogError("[ScriptedBonusHUD] Guncelle hata: " + e.Message); }
#else
            FallbackTMPGuncelle(kalanSpin, oturumKazanci);
#endif
        }

        // ──────────────────────────────────────────────────────────────────────
        // Editor TMP fallback
        // ──────────────────────────────────────────────────────────────────────

        private void BulTMPReferanslari()
        {
            // Sahnede mevcut bonus text'lerini yakala. İsim/içerik bazlı arama (esnek).
            // Ad: "HakText" / "Kalan Spin Hakkı" benzeri; "OturumKazancText" / "OTURUM KAZANCI"
            var allTmps = UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>(includeInactive: true);
            foreach (var tmp in allTmps)
            {
                string ad = tmp.gameObject.name.ToLower();
                if (_kalanSpinTmp == null && (ad.Contains("hak") || ad.Contains("kalan")))
                    _kalanSpinTmp = tmp;
                if (_oturumKazancTmp == null && (ad.Contains("oturum") && ad.Contains("kazan")))
                    _oturumKazancTmp = tmp;
            }
        }

        private void FallbackTMPGuncelle(int kalanSpin, int oturumKazanci)
        {
            if (_kalanSpinTmp != null)
                _kalanSpinTmp.text = $"Kalan Spin Hakkı: {kalanSpin}";
            if (_oturumKazancTmp != null)
                _oturumKazancTmp.text = $"OTURUM KAZANCI: {oturumKazanci} TL";
        }
    }
}
