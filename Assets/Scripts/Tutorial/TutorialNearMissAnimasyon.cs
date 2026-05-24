using System.Collections;
using System.Collections.Generic;
using Senaryo.Scripted;
using UnityEngine;
using UnityEngine.UI;

namespace KumarFarkindalik.Tutorial
{
    /// <summary>
    /// PAKET 14-FAZ35.8: T8 (Near Miss Hissi) — Near-miss cluster sembolü rotate animasyonu.
    /// FAZ35.68: Cluster sembolü artık SonOynanmisKayit.ilkGridSemboller frequency analizi ile
    /// dinamik bulunur (≥7 adet eşik); Image'lar OyunYoneticisi.hucreler grid dizisinden grid
    /// değeri üzerinden eşleştirilir (sprite.name kontrolü kaldırıldı → false-positive yok,
    /// sembol-bağımsız). NearMiss spin sonunda cluster sembolünün 7 hücresi kendi etrafında
    /// döner; sonraki SPIN tıklanana kadar devam, SPIN basılınca durur + rotation reset.
    ///
    /// Pattern kaynağı: AnlaticiSeritKopru.cs:1067-1119 (03 sahnesi A4 Spin 1 yıldız dansı).
    /// Tutorial namespace'e adapt edilirken modal-driven süre yerine "sonraki spin start" tetik.
    ///
    /// Trigger noktaları (TutorialOyunYoneticisi):
    ///   • Awake — bootstrap AddComponent
    ///   • Update spin start (false → true) — DurdurRotate
    ///   • SayaciGecikmeliArtir sonu — TutorialScriptedYoneticisi.SonOynanmisKayit.tip == NearMiss
    ///     && mevcutAdim == T8 ise BaslatRotate(kayit).
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

        /// <summary>NearMiss spin sonrası çağrılır. 0.5sn bekler (grid otursun), sonra kayıttan
        /// cluster sembolünü (frequency ≥7) bulup o sembollü grid hücrelerini sonsuz rotate eder.
        /// Sonraki SPIN'e kadar dönmeye devam. FAZ35.68: hardcoded "hindistan/coconut" sprite adı
        /// yerine grid değeri tabanlı eşleme — ekrandaki gerçek cluster döner, sembol-bağımsız.</summary>
        public void BaslatRotate(ScriptedSpinKaydi kayit)
        {
            // PAKET 14-FAZ35.9 RACE FIX: Önceki BaslatAkis hâlâ 0.5sn delay'inde olabilir;
            // orphan kalmasın diye önce onu durdur, sonra yeni akışı başlat ve takip et.
            if (_baslatAkisCoroutine != null) StopCoroutine(_baslatAkisCoroutine);
            _baslatAkisCoroutine = StartCoroutine(BaslatAkis(kayit));
        }

        private IEnumerator BaslatAkis(ScriptedSpinKaydi kayit)
        {
            yield return new WaitForSecondsRealtime(YERLESME_GECIKMESI);

            if (kayit == null || kayit.ilkGridSemboller == null)
            {
                Debug.LogWarning("[T8 Rotate] kayıt veya ilkGridSemboller null, animasyon atlanıyor.");
                yield break;
            }

            // FAZ35.68: Cluster sembolünü frequency ile bul. Near-miss tanımı: 7 bitişik
            // (eşik 8'in altı) → en az 7 adet aynı sembol. Hardcoded "hindistan/coconut" sprite
            // adı kontrolü kaldırıldı, sembol-bağımsız: hangi meyve cluster oluşturursa o döner.
            int[] freq = new int[8]; // 0..7 sembol indeksi (scatter dahil); -1/-2 (boş/çarpan) sayılmaz
            foreach (var s in kayit.ilkGridSemboller)
                if (s >= 0 && s < freq.Length) freq[s]++;
            int clusterSembol = -1;
            for (int i = 0; i < freq.Length; i++)
                if (freq[i] >= 7) { clusterSembol = i; break; }
            if (clusterSembol < 0)
            {
                Debug.LogWarning("[T8 Rotate] Cluster sembolü bulunamadı (≥7 adet sembol yok), animasyon atlanıyor.");
                yield break;
            }

            var semboller = SembolleriBul(clusterSembol, kayit.ilkGridSemboller);
            if (semboller.Count == 0)
            {
                Debug.LogWarning($"[T8 Rotate] Sembol idx={clusterSembol} için grid Image bulunamadı (hucreler null/boş?), animasyon atlanıyor.");
                yield break;
            }

            Debug.Log($"[T8 Rotate] Cluster sembol idx={clusterSembol}, {semboller.Count} Image döndürülüyor.");
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
            // PAKET 14-FAZ35.9 RACE FIX: BaslatAkis hâlâ delay'inde olabilir (SembolleriBul henüz
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

        /// <summary>FAZ35.68: Grid değeri eşleştirmesi (sprite.name DEĞİL) — OyunYoneticisi.hucreler
        /// dizisinde gridSemboller[i] == hedefSembol olan hücrelerin GameObject'lerini toplar.
        /// Sahne-geneli FindObjectsOfType yerine grid-bound: tutorial paneli ikonu gibi yan Image'lar
        /// false-positive yakalanmaz; ayrıca sprite asset rename'ine bağımsız. NearMiss kaydında
        /// tumble olmadığı için ilkGridSemboller == ekrandaki güncel grid durumu (uyuşmazlık yok).</summary>
        private List<GameObject> SembolleriBul(int hedefSembol, int[] gridSemboller)
        {
            var sonuc = new List<GameObject>();
            var oy = Object.FindObjectOfType<OyunYoneticisi>();
            if (oy == null || oy.hucreler == null || gridSemboller == null) return sonuc;
            int n = Mathf.Min(gridSemboller.Length, oy.hucreler.Length);
            for (int i = 0; i < n; i++)
                if (gridSemboller[i] == hedefSembol && oy.hucreler[i] != null)
                    sonuc.Add(oy.hucreler[i].gameObject);
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
