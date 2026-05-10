using System.Globalization;
using UnityEngine;

namespace KumarFarkindalik.Tutorial
{
    /// <summary>
    /// PanelKopru.OnAyarDegisti event'ine subscribe olur, panel.html'den gelen ayar değişimlerini
    /// yakalar; PanelKopru'da public static field karşılığı OLMAYAN parametreleri (carpanOlasilik,
    /// carpanZorla, bonusTetikle) lokal static field'larda tutar. TutorialAdimYoneticisi.KosulSagla
    /// bu field'lara erişir.
    ///
    /// Update'te SenaryoYoneticisi.toplamSpin polling ile koşul kontrol → İLERİ aktif.
    /// </summary>
    public class TutorialAdminEnjeksiyonu : MonoBehaviour
    {
        // === PanelKopru'da karşılığı olmayan değerler — lokal cache ===
        public static float SonCarpanOlasilik;  // 0-100 (panel.html %)
        public static int SonCarpanZorla;       // panel.html "carpanZorla" 500 vb.
        public static bool BonusTetiklendi;     // panel.html "bonusTetikle" tek-seferlik flag

        private TutorialAdimYoneticisi _ay;
        private bool _eventBagli;

        private void Start()
        {
            _ay = TutorialOyunYoneticisi.Ornek?.AdimYoneticisi;
            PanelKopru.OnAyarDegisti += AyarDegisti;
            _eventBagli = true;
            // Reset (önceki tutorial oturumundan kalıntı)
            SonCarpanOlasilik = 0f;
            SonCarpanZorla = 0;
            BonusTetiklendi = false;
        }

        private void OnDestroy()
        {
            if (_eventBagli)
            {
                PanelKopru.OnAyarDegisti -= AyarDegisti;
                _eventBagli = false;
            }
        }

        private void AyarDegisti(string key, string value)
        {
            // Bug 2 düzeltme: TutorialAdimYoneticisi'ne "bu adımda key değişti" sinyali ver
            _ay?.AyarDegistiHaber(key);

            switch (key)
            {
                case "carpanOlasilik":
                    if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var co))
                        SonCarpanOlasilik = co;
                    break;
                case "carpanZorla":
                    if (int.TryParse(value, out var cz)) SonCarpanZorla = cz;
                    break;
                case "bonusTetikle":
                    BonusTetiklendi = true;
                    break;
            }
        }

        private void Update()
        {
            if (_ay == null)
            {
                _ay = TutorialOyunYoneticisi.Ornek?.AdimYoneticisi;
                if (_ay == null) return;
            }

            int spin = SenaryoYoneticisi.I != null ? SenaryoYoneticisi.I.toplamSpin : 0;
            bool sagla = _ay.KosulSagla(spin);

            // İLERİ aktif/pasif — TutorialAdimGoster üzerinden
            var goster = TutorialAdimGoster.Ornek;
            if (goster != null && goster.IleriZatenAktif != sagla)
            {
                goster.IleriAktif(sagla);
            }
        }
    }
}
