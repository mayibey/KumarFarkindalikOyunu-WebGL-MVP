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

            // PAKET 3B-fix-9 (Feature 3): panel.html aksiyon → hatırlatma timer reset
            HatirlatmaServisi.Ornek?.AktiviteHaberVer();

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

        // PAKET 3B-fix-3: Update YENİDEN aktif — SADECE canlı UI güncellemesi.
        // İLERİ aktif/pasif YAPMAZ (AdimAkisi yield-while ile zaten yönetiyor).
        // Bu method TutorialAdimGoster.IlerlemeGuncelle ile parametre/spin durumunu refresh eder.
        private void Update()
        {
            if (TutorialAdimGoster.Ornek == null) return;
            if (_ay == null)
            {
                _ay = TutorialOyunYoneticisi.Ornek?.AdimYoneticisi;
                if (_ay == null) return;
            }
            var v = _ay.MevcutAdimVerisi;
            if (v == null || !v.aktifMi) return;

            int spin = TutorialAdimYoneticisi.MevcutSpinAl();
            int delta = spin - _ay.AdimBaslangicSpin;

            // Parametre tamamlandı mı (degisimAnahtarlari'nın HEPSİ değişmiş mi)
            bool parametreTamam = true;
            if (v.degisimAnahtarlari != null)
            {
                foreach (var k in v.degisimAnahtarlari)
                    if (!_ay.AdimSirasindaDegistirildi(k)) { parametreTamam = false; break; }
            }

            TutorialAdimGoster.Ornek.IlerlemeGuncelle(delta, v.gerekliSpin, parametreTamam);

            // PAKET 3B-fix-11 (Sorun 2): SPIN butonu interactable=true bırak (listener guard handle ediyor + uyarı).
            // Sadece görsel CanvasGroup alpha grilik (0.5) ile "disabled hissi" ver — click yine işler, guard yanıtlar.
            var spinBtn = TutorialOyunYoneticisi.Ornek?.SpinBtnRef;
            if (spinBtn != null)
            {
                var cg = spinBtn.gameObject.GetComponent<CanvasGroup>();
                if (cg == null) cg = spinBtn.gameObject.AddComponent<CanvasGroup>();
                float hedefAlpha = parametreTamam ? 1f : 0.5f;
                if (Mathf.Abs(cg.alpha - hedefAlpha) > 0.01f)
                    cg.alpha = hedefAlpha;
                if (!spinBtn.interactable) spinBtn.interactable = true; // sürekli aktif
            }
        }
    }
}
