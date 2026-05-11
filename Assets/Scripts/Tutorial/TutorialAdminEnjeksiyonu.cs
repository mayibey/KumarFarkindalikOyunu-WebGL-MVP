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

        // PAKET 3B-fix-14 (Bug A/B/C): OyunYoneticisi cache + private field reflection.
        // Update'te her frame:
        //   Fix 1) scatterChanceNormal > 0 ise 0'a çek (panel.html veya başka path geri yükseltirse)
        //   Fix 3) bonusAktif true → false geçişlerinde log (Bug A root cause izleme)
        //   Fix 4) bonusAktif=true && bonusHakKalan<=0 && T11 değil → reflection cleanup (Bug C)
        private OyunYoneticisi _oy;
        private System.Reflection.FieldInfo _bonusAktifField;
        private System.Reflection.FieldInfo _bonusHakKalanField;
        private bool _oncekiBonusAktif;

        private void Start()
        {
            _ay = TutorialOyunYoneticisi.Ornek?.AdimYoneticisi;
            PanelKopru.OnAyarDegisti += AyarDegisti;
            _eventBagli = true;
            // Reset (önceki tutorial oturumundan kalıntı)
            SonCarpanOlasilik = 0f;
            SonCarpanZorla = 0;
            BonusTetiklendi = false;

            // PAKET 3B-fix-14: OyunYoneticisi private field reflection cache
            _bonusAktifField = typeof(OyunYoneticisi).GetField("bonusAktif",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            _bonusHakKalanField = typeof(OyunYoneticisi).GetField("bonusHakKalan",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (_bonusAktifField == null || _bonusHakKalanField == null)
                Debug.LogWarning("[Tutorial] Bonus state field reflection bulunamadı — Bug C cleanup devre dışı kalacak");
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
                case "oyunModu":
                    // PAKET 4-FAZ-1: Tutorial T3 scripted spin motoru tetikleyici
                    // value = "hook" / "yontma" / "tutma" / "koruma" / "normal"
                    TutorialSenaryoMotoru.PatternBaslat(value);
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

            // === PAKET 3B-fix-14 (Bug A/B/C): Defansif scatter + bonus state izleme/cleanup ===
            // Pasif adımlar (T2, T_SON) dahil HER frame çalışır.
            if (_oy == null) _oy = Object.FindObjectOfType<OyunYoneticisi>();
            if (_oy != null)
            {
                bool t11Aktif = _ay.mevcutAdim == TutorialAdimYoneticisi.TutorialAdimId.T11;

                // Fix 1: scatterChanceNormal kalıcı 0 (panel.html veya başka path 0'dan kaldırsa anında reset)
                if (_oy.scatterChanceNormal > 0.0001f)
                {
                    _oy.scatterChanceNormal = 0f;
                    Debug.Log("[Tutorial] scatterChanceNormal yeniden 0'a çekildi (overhead trigger)");
                }

                // Fix 3: bonus aktif geçiş logu — Bug A root cause izleme
                bool bonusAktif = _oy.BonusAktifMi;
                if (bonusAktif && !_oncekiBonusAktif)
                {
                    string adim = _ay.mevcutAdim.ToString();
                    if (t11Aktif)
                        Debug.Log($"[Tutorial] Bonus tetiklendi (T11 — BEKLENEN). bonusHakKalan={_oy.BonusHakKalan}");
                    else
                        Debug.LogWarning($"[Tutorial] BONUS TETIKLENDI (BEKLENMEYEN)! Adim={adim} scatter={_oy.scatterChanceNormal} periyot={_oy.bonusOtomatikSpinPeriyodu} hakKalan={_oy.BonusHakKalan} oturumKazanc={_oy.OturumKazanc}");
                }
                else if (!bonusAktif && _oncekiBonusAktif)
                {
                    Debug.Log("[Tutorial] Bonus state false oldu (cleanup veya normal akış)");
                }
                _oncekiBonusAktif = bonusAktif;

                // Fix 4: Bug C cleanup — bonus oyun bitti (hakKalan<=0) ama bonusAktif takılı → reflection ile false set
                if (bonusAktif && _oy.BonusHakKalan <= 0 && !t11Aktif
                    && _bonusAktifField != null && _bonusHakKalanField != null)
                {
                    _bonusAktifField.SetValue(_oy, false);
                    _bonusHakKalanField.SetValue(_oy, 0);
                    Debug.Log("[Tutorial] Bonus state reflection ile temizlendi (Bug C — 04'te cleanup kaynagi yok)");
                }
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

            // PAKET 3B-fix-12 (İş 1): SpinGuardOverlay toggle — parametre eksikken click yutucu aktif
            var overlay = TutorialOyunYoneticisi.Ornek?.SpinGuardOverlay;
            if (overlay != null)
            {
                bool olmalı = !parametreTamam;
                if (overlay.activeSelf != olmalı)
                    overlay.SetActive(olmalı);
            }
        }
    }
}
