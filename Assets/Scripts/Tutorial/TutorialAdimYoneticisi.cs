using System;
using UnityEngine;

namespace KumarFarkindalik.Tutorial
{
    /// <summary>
    /// Tutorial akış adımlarının state machine'i (T1-T_SON).
    /// PAKET 1: İskelet — Ileri/Geri boş, OnAdimDegisti event tanımlı ama henüz tetiklenmiyor.
    /// TODO Paket 3: Her adım için içerik (mesaj + admin panel hedef + spin sayısı).
    /// </summary>
    public class TutorialAdimYoneticisi : MonoBehaviour
    {
        public enum TutorialAdimId
        {
            T1,
            T2,
            T3,
            T4,
            T5,
            T6,
            T_SON
        }

        public TutorialAdimId mevcutAdim = TutorialAdimId.T1;

        /// <summary>Adım değiştiğinde tetiklenir (TutorialOyunYoneticisi + TutorialAdimPaneli + TutorialHighlight dinler).</summary>
        public event Action<TutorialAdimId> OnAdimDegisti;

        public void Ileri()
        {
            // TODO (Paket 3): mevcutAdim'i bir sonraki state'e ilerlet, OnAdimDegisti tetikle.
            // T_SON sonrası: TutorialBitisEkrani açılır.
        }

        public void Geri()
        {
            // TODO (Paket 3): mevcutAdim'i bir önceki state'e dönder, OnAdimDegisti tetikle.
            // T1 için no-op (geri yok).
        }
    }
}
