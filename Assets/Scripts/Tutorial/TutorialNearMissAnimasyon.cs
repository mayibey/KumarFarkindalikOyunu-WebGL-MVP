using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KumarFarkindalik.Tutorial
{
    /// <summary>
    /// PAKET 14-FAZ35.8: T8 (Near Miss Hissi) — 7 Hindistan rotate animasyonu.
    /// NearMiss spin sonunda 7 Hindistan kendi etrafında döner; sonraki SPIN tıklanana kadar
    /// dönmeye devam eder. SPIN basıldığında durur + rotation reset.
    ///
    /// Pattern kaynağı: AnlaticiSeritKopru.cs:1067-1119 (03 sahnesi A4 Spin 1 yıldız dansı).
    /// Tutorial namespace'e adapt edilirken modal-driven süre yerine "sonraki spin start" tetik.
    ///
    /// Trigger noktaları (TutorialOyunYoneticisi):
    ///   • Awake — bootstrap AddComponent
    ///   • Update spin start (false → true) — DurdurRotate
    ///   • SayaciGecikmeliArtir sonu — TutorialScriptedYoneticisi.SonOynanmisKayit.tip == NearMiss
    ///     && mevcutAdim == T8 ise BaslatRotate.
    /// </summary>
    public class TutorialNearMissAnimasyon : MonoBehaviour
    {
        public static TutorialNearMissAnimasyon Ornek { get; private set; }

        private readonly List<Coroutine> _aktifCoroutineler = new List<Coroutine>();
        private readonly List<GameObject> _aktifSemboller = new List<GameObject>();
        private Coroutine _baslatAkisCoroutine;

        private const float DONME_HIZI = 360f / 1.5f; // 1 tur = 1.5 sn (240 °/s) — AnlaticiSeritKopru emsali
        private const float YERLESME_GECIKMESI = 0.5f; // grid yerleşsin, kullanıcı 7 Hindistan'ı görsün

        private void Awake()
        {
            // PAKET 14-FAZ35.9 DEFANSIF: Sahne yanlışlıkla bu component'i içeriyorsa (Inspector'da
            // manuel eklenmiş, sahne kopyalama hatası, vb.) Tutorial sahnesi dışında self-destruct.
            // TutorialOyunYoneticisi.Awake guard'ının analoğu — orphan instance 03/01/05'te yaşamasın.
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
                != TutorialOyunYoneticisi.TUTORIAL_SAHNE_BUILD_INDEX)
            {
                Destroy(this);
                return;
            }
            if (Ornek != null && Ornek != this) { Destroy(this); return; }
            Ornek = this;
        }

        private void OnDestroy()
        {
            if (Ornek == this) Ornek = null;
        }

        /// <summary>NearMiss spin sonrası çağrılır. 0.5sn bekler (grid otursun), sonra 7 Hindistan
        /// sembolünü bulup sonsuz rotate coroutine'leri başlatır. Sonraki SPIN'e kadar dönmeye devam.</summary>
        public void BaslatRotate()
        {
            // PAKET 14-FAZ35.9 RACE FIX: Önceki BaslatAkis hâlâ 0.5sn delay'inde olabilir;
            // orphan kalmasın diye önce onu durdur, sonra yeni akışı başlat ve takip et.
            if (_baslatAkisCoroutine != null) StopCoroutine(_baslatAkisCoroutine);
            _baslatAkisCoroutine = StartCoroutine(BaslatAkis());
        }

        private IEnumerator BaslatAkis()
        {
            yield return new WaitForSecondsRealtime(YERLESME_GECIKMESI);

            var semboller = HindistanlariBul();
            if (semboller.Count == 0)
            {
                Debug.LogWarning("[T8 Rotate] Hindistan sembolü bulunamadı, animasyon atlanıyor.");
                yield break;
            }

            Debug.Log($"[T8 Rotate] {semboller.Count} Hindistan döndürülüyor.");
            foreach (var sembol in semboller)
            {
                _aktifSemboller.Add(sembol);
                _aktifCoroutineler.Add(StartCoroutine(DondurSonsuz(sembol)));
            }
        }

        /// <summary>Sonraki SPIN tetiklendiğinde çağrılır. Tüm rotate coroutine'leri durdurur,
        /// her sembolün rotation'ını Quaternion.identity'ye sıfırlar (pivot sapması olmasın).</summary>
        public void DurdurRotate()
        {
            // PAKET 14-FAZ35.9 RACE FIX: BaslatAkis hâlâ delay'inde olabilir (HindistanlariBul henüz
            // çalışmadı → _aktifCoroutineler boş). Erken-return ile orphan kalmaması için ÖNCE
            // BaslatAkis'i durdur. Sonra _aktifCoroutineler/_aktifSemboller boşsa erken çık.
            if (_baslatAkisCoroutine != null)
            {
                StopCoroutine(_baslatAkisCoroutine);
                _baslatAkisCoroutine = null;
            }
            if (_aktifCoroutineler.Count == 0 && _aktifSemboller.Count == 0) return;

            Debug.Log($"[T8 Rotate] Dönme durduruluyor ({_aktifSemboller.Count} sembol).");
            foreach (var co in _aktifCoroutineler)
                if (co != null) StopCoroutine(co);
            _aktifCoroutineler.Clear();

            foreach (var sembol in _aktifSemboller)
                if (sembol != null) sembol.transform.localRotation = Quaternion.identity;
            _aktifSemboller.Clear();
        }

        /// <summary>Sprite name fallback (AnlaticiSeritKopru.YildizlariBul emsali) —
        /// Image.sprite.name içinde "hindistan" veya "coconut" geçen GameObject'leri toplar.</summary>
        private List<GameObject> HindistanlariBul()
        {
            var sonuc = new List<GameObject>();
            var images = Object.FindObjectsOfType<Image>();
            foreach (var img in images)
            {
                if (img == null || img.sprite == null) continue;
                string ad = img.sprite.name.ToLower();
                if (ad.Contains("hindistan") || ad.Contains("coconut"))
                    sonuc.Add(img.gameObject);
            }
            return sonuc;
        }

        private IEnumerator DondurSonsuz(GameObject sembol)
        {
            while (sembol != null && _aktifSemboller.Contains(sembol))
            {
                sembol.transform.Rotate(0f, 0f, DONME_HIZI * Time.unscaledDeltaTime);
                yield return null;
            }
        }
    }
}
