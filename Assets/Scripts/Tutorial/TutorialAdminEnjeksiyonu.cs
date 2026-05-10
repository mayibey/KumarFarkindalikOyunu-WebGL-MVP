using UnityEngine;

namespace KumarFarkindalik.Tutorial
{
    /// <summary>
    /// Tutorial adımlarına göre admin panel parametrelerini programatik değiştirir
    /// (ödeme eğilimi, max ödeme, manuel asama dropdown vb.) — kullanıcı görsel olarak
    /// hangi parametrenin değiştiğini izler.
    /// TutorialOyunYoneticisi tarafından AddComponent edilir.
    ///
    /// PAKET 3A: Boş iskelet (mantık Paket 3B'de).
    /// </summary>
    public class TutorialAdminEnjeksiyonu : MonoBehaviour
    {
        // PAKET 3B:
        //   - TutorialAdimYoneticisi.OnAdimDegisti subscribe
        //   - Her adım için: hedef admin parametresini OyunYoneticisi public API ile değiştir
        //   - AdminSetOdemeEgilimi / AdminSetMaxOdeme / AdminAsamaDegistir helper'lar
    }
}
