using System.Collections;
using UnityEngine;

namespace KumarFarkindalik.Tutorial
{
    /// <summary>
    /// FAZ35.14: "Kendin Oyna" sonrası 04 sahnesi yeniden yüklendiğinde
    /// OyunYoneticisi.Start tamamlandıktan sonra bahis + admin default
    /// reset yapan tek-seferlik helper.
    ///
    /// Neden gerek: OnSceneLoaded sceneLoaded event'i Awake'ten sonra
    /// ama Start'tan önce/aynı frame tetiklenir. _ekonomiServisi init
    /// genelde Start'ta. Race önlemek için 1 frame wait sonra reset,
    /// sonra self-destroy.
    ///
    /// Bakiye reset GameManager.I.ActivePlayer.balance üzerinden
    /// solBtnCallback'te yapıldı (race-free). Bu helper bahis + admin
    /// reset için.
    /// </summary>
    public class TutorialAtlaReset : MonoBehaviour
    {
        public void Baslat() => StartCoroutine(Calistir());

        private IEnumerator Calistir()
        {
            // 1 frame bekle — OyunYoneticisi.Start ve EkonomiServisi.Init tamamlansın
            yield return null;

            var oy = Object.FindObjectOfType<OyunYoneticisi>();
            if (oy != null)
            {
                oy.AnlaticiBakiyeyiSifirla(50000);
                oy.AnlaticiSetBahis(1000);
                oy.AdminNormalOyunUygula();
                Debug.Log("[FAZ35.14] TutorialAtlaReset: bakiye=50000, bahis=1000, admin=normalOyun uygulandı");
            }
            else
            {
                Debug.LogWarning("[FAZ35.14] TutorialAtlaReset: OyunYoneticisi bulunamadı (1 frame sonra hâlâ null)");
            }

            Destroy(gameObject);
        }
    }
}
