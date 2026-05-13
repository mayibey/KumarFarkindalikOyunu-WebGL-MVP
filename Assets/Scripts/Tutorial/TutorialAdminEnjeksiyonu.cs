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
        // PAKET 14-FAZ2: T7 Ödeme dinamik min/maks (bahis çarpanı) — slider eventleri ayrı geldiği için cache.
        public static float SonMinCarpan = 0f;
        public static float SonMaksCarpan = 0f;

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
            SonMinCarpan = 0f;
            SonMaksCarpan = 0f;

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

        // PAKET 14-FAZ2: T7 Ödeme dinamik pattern tetikleyici.
        // min ve maks ayrı ayrı geldiği için her event'te çağrılır; min>0 && maks>=min && T7 adımında ise
        // paytable taramasıyla 5 spinlik desen üretir. Ara modal sonrası ikinci aşama bayrağı da set olur.
        private static void TryDinamikOdemePatternBaslat()
        {
            var ay = TutorialOyunYoneticisi.Ornek?.AdimYoneticisi;
            if (ay == null) return;
            if (ay.mevcutAdim != TutorialAdimYoneticisi.TutorialAdimId.T7) return;
            float min = SonMinCarpan;
            float maks = SonMaksCarpan;
            if (min <= 0f || maks <= 0f || maks < min) return;

            // Ara modal sonrası ikinci aşama bayrağını işaretle (UI durumu eski mantıkla uyumlu)
            if (TutorialOyunYoneticisi.T8AraModalGosterildi && !TutorialOyunYoneticisi.T8IkinciAsamaBasladi)
                TutorialOyunYoneticisi.T8IkinciAsamaBasladi = true;

            TutorialSenaryoMotoru.DinamikOdemePatternBaslat(min, maks);
            Debug.Log($"[Tutorial T7 Ödeme] Dinamik pattern tetiklendi: aralık=[{min:F1}-{maks:F1}]× bahis");
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
                    {
                        SonCarpanOlasilik = co;
                        // PAKET 9: T4 ikinci aşama tetik — kullanıcı slider'ı %0'a çekti (≤5 tolerans).
                        var ayT4 = TutorialOyunYoneticisi.Ornek?.AdimYoneticisi;
                        if (ayT4 != null && ayT4.mevcutAdim == TutorialAdimYoneticisi.TutorialAdimId.T4
                            && TutorialOyunYoneticisi.T4AraModalGosterildi
                            && !TutorialOyunYoneticisi.T4IkinciAsamaBasladi
                            && co <= 0.5f) // PAKET 14-FAZ4: TAM %0 sınırı
                        {
                            TutorialOyunYoneticisi.T4IkinciAsamaBasladi = true;
                            // PAKET 14 (İş 3): Motor pattern enjeksiyon race ile RNG path geçerse
                            // (Motor early-out var: carpanOlasilik<=0.001f) RNG yine carpanUretimiAktif=true
                            // ise çarpan üretebilir. Defansif olarak field'ı false çek → tüm path'lerde
                            // çarpan üretimi durur. T4 girişinde true set ediliyor (line ~482), buradaki
                            // false ikinci aşamayı kesin garanti eder.
                            var oyT4Capac = Object.FindObjectOfType<OyunYoneticisi>();
                            if (oyT4Capac != null) oyT4Capac.carpanUretimiAktif = false;
                            TutorialSenaryoMotoru.PatternBaslat("carpanTest_0");
                            Debug.Log("[Tutorial T4 Çarpan] Slider %0 → ikinci pattern başladı (carpanTest_0) + carpanUretimiAktif=false");
                        }
                    }
                    break;
                case "carpanZorla":
                    if (int.TryParse(value, out var cz)) SonCarpanZorla = cz;
                    break;
                case "bonusOtomatikOran":
                    // PAKET 9: T5 ikinci aşama tetik — kullanıcı bonus olasılığını çok düşürdü (periyot >= 50).
                    // panel.html: %0'da 9999 gönderir; %2'de 50; %1'de 100. >=50 hepsini yakalar.
                    if (int.TryParse(value, out int bonusPeriyot))
                    {
                        var ayT5 = TutorialOyunYoneticisi.Ornek?.AdimYoneticisi;
                        if (ayT5 != null && ayT5.mevcutAdim == TutorialAdimYoneticisi.TutorialAdimId.T5
                            && TutorialOyunYoneticisi.T5AraModalGosterildi
                            && !TutorialOyunYoneticisi.T5IkinciAsamaBasladi
                            && bonusPeriyot == 9999) // PAKET 14-FAZ4: TAM %0 sınırı (panel.html %0→9999)
                        {
                            TutorialOyunYoneticisi.T5IkinciAsamaBasladi = true;
                            // PAKET 14-FAZ3 (İş 4): T11BonusYarimKes spinCalisiyor=false set'i Update polling'i
                            // tekrar tetikleyip sayacı 1→2 fantom artırıyor. AdimBaslangicSpin'i şu anki sayaca
                            // alıp gerekliSpin=1 yaparak ikinci aşama için 1 SPIN DAHA garanti et.
                            TutorialOyunYoneticisi.Ornek?.AdimYoneticisi?.IkinciAsamaIcinSayaciResetle(1);
                            TutorialSenaryoMotoru.PatternBaslat("bonusTest_0");
                            Debug.Log($"[Tutorial T5 Bonus] Periyot={bonusPeriyot} (≥50) → ikinci pattern başladı (bonusTest_0) + sayaç reset");
                        }
                    }
                    break;
                case "bonusTetikle":
                    BonusTetiklendi = true;
                    break;
                case "oyunModu":
                    // PAKET 4-FAZ-1: Tutorial T3 scripted spin motoru tetikleyici
                    // value = "hook" / "yontma" / "tutma" / "koruma" / "normal"
                    TutorialSenaryoMotoru.PatternBaslat(value);
                    break;
                case "yeniOyuncu":
                    // PAKET 6C2: T6_YENI_OYUNCU aşama 2 — toggle açıldığında ikinci pattern başlat
                    if (value == "True" || value == "true")
                    {
                        var ay2 = TutorialOyunYoneticisi.Ornek?.AdimYoneticisi;
                        if (ay2 != null && ay2.mevcutAdim == TutorialAdimYoneticisi.TutorialAdimId.T6_YENI_OYUNCU
                            && TutorialOyunYoneticisi.T6AraModalGosterildi
                            && !TutorialOyunYoneticisi.T6IkinciAsamaBasladi)
                        {
                            TutorialOyunYoneticisi.T6IkinciAsamaBasladi = true;
                            // PAKET 6C2-EXT: Kullanıcı bilinçli toggle bastı — defansif kilit AÇIK
                            // (artık yeniOyuncu=true mesajları PanelKopru'da yutulmaz).
                            TutorialOyunYoneticisi.T6YOForceKapaliKilitli = false;
                            TutorialSenaryoMotoru.PatternBaslat("yeniOyuncu_acik");
                            Debug.Log("[Tutorial T6_YENI_OYUNCU] Toggle açıldı → ikinci pattern başladı + kilit kapandı");
                        }
                    }
                    break;
                case "kazanmaOrani":
                    // PAKET 6C3 + UI-5LIK: T7 (Kazandırma) — slider 0-5 doğrudan N (artık scale yok)
                    {
                        var ayK = TutorialOyunYoneticisi.Ornek?.AdimYoneticisi;
                        if (ayK != null && ayK.mevcutAdim == TutorialAdimYoneticisi.TutorialAdimId.T6
                            && int.TryParse(value, out int kazSliderVal))
                        {
                            int n = Mathf.Clamp(kazSliderVal, 0, 5);
                            TutorialSenaryoMotoru.DinamikPatternBaslat("kazandirma", n);
                        }
                    }
                    break;
                case "yakinKacirma":
                    // PAKET 6C3 + UI-5LIK: T9 (Near Miss) — slider 0-5 doğrudan N (artık scale yok)
                    {
                        var ayN = TutorialOyunYoneticisi.Ornek?.AdimYoneticisi;
                        if (ayN != null && ayN.mevcutAdim == TutorialAdimYoneticisi.TutorialAdimId.T8
                            && int.TryParse(value, out int nmSliderVal))
                        {
                            int n = Mathf.Clamp(nmSliderVal, 0, 5);
                            TutorialSenaryoMotoru.DinamikPatternBaslat("nearMiss", n);
                        }
                    }
                    break;
                case "minCarpan":
                    // PAKET 14-FAZ2: T7 Ödeme — min slider değişti, cache + dinamik pattern dene.
                    if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float minCarp))
                    {
                        SonMinCarpan = minCarp;
                        TryDinamikOdemePatternBaslat();
                    }
                    break;
                case "maksCarpan":
                    // PAKET 14-FAZ2: T7 Ödeme — maks slider değişti, cache + dinamik pattern dene.
                    if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float maksCarp))
                    {
                        SonMaksCarpan = maksCarp;
                        TryDinamikOdemePatternBaslat();
                    }
                    break;
                case "carpanOdeme":
                    // PAKET 6D: T11 (Çarpan Zorla) aşama 2 — toggle açıldığında ikinci pattern
                    if (value == "True" || value == "true")
                    {
                        var ayC = TutorialOyunYoneticisi.Ornek?.AdimYoneticisi;
                        if (ayC != null && ayC.mevcutAdim == TutorialAdimYoneticisi.TutorialAdimId.T10
                            && TutorialOyunYoneticisi.T11AraModalGosterildi
                            && !TutorialOyunYoneticisi.T11IkinciAsamaBasladi)
                        {
                            TutorialOyunYoneticisi.T11IkinciAsamaBasladi = true;
                            TutorialSenaryoMotoru.PatternBaslat("carpanZorla_acikOdeme");
                            Debug.Log("[Tutorial T11 Çarpan Zorla] carpanOdemeToggle açıldı → ikinci pattern başladı");
                        }
                    }
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

            // Parametre tamamlandı mı (degisimAnahtarlari'nın HEPSİ değişmiş mi + parametreKosulu lambda doğru)
            bool parametreTamam = true;
            if (v.degisimAnahtarlari != null)
            {
                foreach (var k in v.degisimAnahtarlari)
                    if (!_ay.AdimSirasindaDegistirildi(k)) { parametreTamam = false; break; }
            }
            // PAKET 14-FAZ3 (İş 1): parametreKosulu lambda dahil edilmeli — slider'a sadece dokunmak yetmez,
            // DOĞRU değere (T4 ilk aşama %100, ikinci %0; T5 ilk periyot 1-2, ikinci ≥50) çekilmeli.
            // Aksi halde SpinKilitli false olur, kullanıcı %100 olmadan spin atabilir.
            if (parametreTamam && v.parametreKosulu != null)
                parametreTamam = v.parametreKosulu.Invoke();

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

            // Space tuşu için: overlay sadece mouse raycast yutar, klavye için global flag gerekli.
            TutorialOyunYoneticisi.SpinKilitli = !parametreTamam;
        }
    }
}
