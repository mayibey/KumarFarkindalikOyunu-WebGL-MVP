using System.Runtime.InteropServices;
using UnityEngine;

namespace KumarFarkindalik.Tutorial
{
    /// <summary>
    /// PAKET 14-FAZ34 İş 6: Tutorial adımına göre yönetici paneli (panel.html) input/select/button'larını
    /// disabled/enabled durumuna geçirir. Sadece aktif adımda ihtiyaç duyulan UI elementleri çalışır,
    /// diğerleri gri (opacity 0.4) + pointer-events:none. Yanlış tıklama → istenmeyen state bozulması önlenir.
    ///
    /// Akış:
    /// 1. TutorialAdimYoneticisi AdimGec → mevcut adımın <c>vurguSelectorlari</c> listesi aktif olur.
    /// 2. <see cref="KilitliAyarlariGonder"/> jslib bridge ile panel iframe'e postMessage atar.
    /// 3. panel.html message listener <c>ayarlariKilitle(aktifSelectorlar)</c> çağırır → DOM toggle.
    /// 4. WebGL dışında (Editor) jslib mevcut değil → no-op (sadece Debug.Log).
    /// </summary>
    public static class TutorialPanelKilit
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void TutorialPanelKilitGonderJslib(string json);
#endif

        /// <summary>
        /// Aktif kalacak (disable EDİLMEYECEK) selector listesi. Diğer tüm input/select/button gri olur.
        /// Liste boş ise tüm UI disable edilir (T1, T2 başlangıç adımları, T_SON kapanış).
        /// </summary>
        public static void KilitliAyarlariGonder(string[] aktifSelectorlar)
        {
            string json = JsonUtility.ToJson(new SelectorListesi { aktifSelectorlar = aktifSelectorlar ?? new string[0] });
            Debug.Log($"[TutorialPanelKilit] KilitliAyarlariGonder: {json}");

#if UNITY_WEBGL && !UNITY_EDITOR
            try { TutorialPanelKilitGonderJslib(json); }
            catch (System.Exception e) { Debug.LogWarning($"[TutorialPanelKilit] jslib hata: {e.Message}"); }
#endif
        }

        [System.Serializable]
        private class SelectorListesi
        {
            public string[] aktifSelectorlar;
        }
    }
}
