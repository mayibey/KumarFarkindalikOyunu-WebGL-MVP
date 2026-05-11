using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using Senaryo.Scripted;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace KumarFarkindalik.Tutorial
{
    /// <summary>
    /// 04_AdminOyunScene (build idx 3) için ana koordinatör. Self-spawn pattern.
    ///
    /// PAKET 3B: Tutorial akış mantığı (T1 - T_SON):
    ///   T1: AyarlarButton glow (mevcut Start coroutine'inde — state machine dışı)
    ///   T2 - T_SON: TutorialAdimYoneticisi state machine
    ///
    /// Akış:
    ///   1. Start coroutine: 1sn bekle → save temizlik → AyarlarButton listener override
    ///      → DigerButonlariPasiflestir → T1 modal → AyarlarButtonGlow
    ///   2. AyarlarButton click: GlowDurdur + DigerButonlariAktiflestir + PanelKopru.AyarlarButonunaBasildi
    ///      + IframeKonumAyarla + PanelAcildiSonrasi (1.5sn bekle → AdminBahisAyarla(1) → AdimGec(T2))
    ///   3. AdimYoneticisi.OnAdimDegisti → AdimAkisi coroutine: modal göster, vurgu aç, AdimGoster göster
    ///   4. TutorialAdimGoster.OnIleriTiklandi → AdimYoneticisi.IleriTiklandi
    ///   5. AdimYoneticisi.OnTutorialBitti → TamSaveTemizlik + TutorialPaneliKapat + LoadScene("01_GirisScene")
    /// </summary>
    [Preserve]
    public class TutorialOyunYoneticisi : MonoBehaviour
    {
        public const int TUTORIAL_SAHNE_BUILD_INDEX = 3;

        public static TutorialOyunYoneticisi Ornek { get; private set; }

        public TutorialAdimYoneticisi AdimYoneticisi { get; private set; }
        public TutorialAdminEnjeksiyonu Enjeksiyon { get; private set; }

        private const string T1_METIN =
            "Hoş geldin. Az önce bir kumar bağımlısının yaşadıklarını gördün. " +
            "Şimdi sahne arkasını birlikte göreceğiz. Sağ-alttaki AYARLAR butonuna tıkla, " +
            "manipülasyon panelini açalım.";

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void PaneliSolaAl();
        [DllImport("__Internal")] private static extern void DropdownTooltipEkle();
        [DllImport("__Internal")] private static extern void VurguAc(string selector);
        [DllImport("__Internal")] private static extern void VurguKapat(string selector);
        [DllImport("__Internal")] private static extern void TumVurgulariKapat();
        [DllImport("__Internal")] private static extern void TutorialPaneliKapat();
#endif

        // === AyarlarButton glow state ===
        private GameObject _glowGo;
        private Coroutine _glowCoroutine;
        private Transform _ayarlarBtnTransform;
        private Vector3 _ayarlarBaseScale = Vector3.one;

        // === Tutorial flow state ===
        private bool _panelAcildi;
        private bool _ileriTiklandi; // AdimAkisi her başlangıçta reset; İLERİ butonu "atla" rolü

        /// <summary>
        /// PAKET 3B-fix-4 (Sorun 2): 04 sahnesinde SenaryoYoneticisi GameObject YOK → toplamSpin
        /// çalışmıyor. ButtonCevir.onClick'e runtime listener eklenerek her spin tıklamasında
        /// bu sayaç artırılır. KosulSagla bu sayacı kullanır.
        /// </summary>
        public int TutorialSpinSayaci { get; private set; }

        // PAKET 3B-fix-9 (Bug 1): ButtonCevir reference field — TutorialAdminEnjeksiyonu.Update'ten
        // interactable toggle için public erişim. Pasif/aktif state'i parametreTamam'a göre yönetilir.
        private Button _spinBtnRef;
        public Button SpinBtnRef => _spinBtnRef;

        // PAKET 3B-fix-12 (İş 1): ButtonCevir üstüne overlay — parametre eksikken click yutar + uyarı.
        // OyunYoneticisi'nin orijinal listener'ı bypass eder (paralel listener sorununu çözer).
        private GameObject _spinGuardOverlayGo;
        public GameObject SpinGuardOverlay => _spinGuardOverlayGo;

        [Preserve]
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OtomatikInit()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            var aktifSahne = SceneManager.GetActiveScene();
            if (aktifSahne.buildIndex == TUTORIAL_SAHNE_BUILD_INDEX)
                OnSceneLoaded(aktifSahne, LoadSceneMode.Single);
        }

        [Preserve]
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.buildIndex != TUTORIAL_SAHNE_BUILD_INDEX)
            {
                if (Ornek != null) Object.Destroy(Ornek.gameObject);
                return;
            }
            if (Ornek != null) return;
            var go = new GameObject(nameof(TutorialOyunYoneticisi));
            go.AddComponent<TutorialOyunYoneticisi>();
        }

        private void Awake()
        {
            if (SceneManager.GetActiveScene().buildIndex != TUTORIAL_SAHNE_BUILD_INDEX)
            {
                gameObject.SetActive(false);
                return;
            }
            if (Ornek != null && Ornek != this) { Destroy(gameObject); return; }
            Ornek = this;

            // Bug 1 düzeltme: ScriptedModalKopru.ModalAcik 03'ten 04'e taşınmış olabilir (singleton Destroy
            // edildi ama static property true kaldı). 04'te SpinButton bu flag'i okuyup kilit kalıyor.
            // Property private setter olduğu için reflection ile reset.
            ScriptedModalKopruModalAcikReset();

            AdimYoneticisi = gameObject.AddComponent<TutorialAdimYoneticisi>();
            Enjeksiyon = gameObject.AddComponent<TutorialAdminEnjeksiyonu>();

            // Event'ler
            AdimYoneticisi.OnAdimDegisti += AdimDegisti;
            AdimYoneticisi.OnTutorialBitti += TutorialBitti;

            Debug.Log("[TutorialOyunYoneticisi] Spawn + AdimYoneticisi/Enjeksiyon AddComponent + event bağlandı.");
        }

        /// <summary>
        /// PAKET 3B-fix-9 (Bug 1): MevcutAdimVerisi.degisimAnahtarlari'nın HEPSİ kullanıcı tarafından
        /// değiştirilmişse true. Pasif adım (degisimAnahtarlari null/boş) için her zaman true.
        /// </summary>
        public bool ParametreSuanTamam()
        {
            var v = AdimYoneticisi?.MevcutAdimVerisi;
            if (v?.degisimAnahtarlari == null || v.degisimAnahtarlari.Length == 0)
                return true;
            foreach (var k in v.degisimAnahtarlari)
                if (!AdimYoneticisi.AdimSirasindaDegistirildi(k)) return false;
            return true;
        }

        /// <summary>
        /// PAKET 3B-fix-12 (İş 1): ButtonCevir parent'ına invisible overlay button — RectTransform birebir
        /// kopya, raycastTarget=true, sibling index ButtonCevir+1 (üstte render). Parametre eksikken
        /// SetActive(true) — SPIN click'i yutar + ZorlaGoster uyarısı. Toggle TutorialAdminEnjeksiyonu.Update'te.
        /// </summary>
        private void SpinGuardOverlayYarat(GameObject spinBtnGo)
        {
            var btnRt = spinBtnGo.GetComponent<RectTransform>();
            var parent = spinBtnGo.transform.parent;
            if (btnRt == null || parent == null) { Debug.LogWarning("[TutorialOyunYoneticisi] SpinGuardOverlay: ButtonCevir RectTransform/parent null"); return; }

            _spinGuardOverlayGo = new GameObject("SpinGuardOverlay",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            _spinGuardOverlayGo.transform.SetParent(parent, false);

            var olRt = _spinGuardOverlayGo.GetComponent<RectTransform>();
            olRt.anchorMin = btnRt.anchorMin;
            olRt.anchorMax = btnRt.anchorMax;
            olRt.pivot = btnRt.pivot;
            olRt.anchoredPosition = btnRt.anchoredPosition;
            olRt.sizeDelta = btnRt.sizeDelta;
            olRt.localScale = btnRt.localScale;

            // Sibling: ButtonCevir'den HEMEN SONRA → üstte render → raycast yutar
            _spinGuardOverlayGo.transform.SetSiblingIndex(spinBtnGo.transform.GetSiblingIndex() + 1);

            var olImg = _spinGuardOverlayGo.GetComponent<Image>();
            olImg.color = new Color(1f, 1f, 1f, 0f); // tamamen transparent
            olImg.raycastTarget = true;

            var olBtn = _spinGuardOverlayGo.GetComponent<Button>();
            olBtn.transition = Selectable.Transition.None;
            olBtn.onClick.AddListener(() =>
            {
                string eksikMsg = ParametreEksikMesaj();
                HatirlatmaServisi.Ornek?.ZorlaGoster(eksikMsg);
                Debug.Log("[TutorialOyunYoneticisi] SpinGuardOverlay click → uyarı: " + eksikMsg);
            });

            _spinGuardOverlayGo.SetActive(false); // başlangıçta gizli; Update'te toggle
            Debug.Log("[TutorialOyunYoneticisi] SpinGuardOverlay yaratıldı.");
        }

        /// <summary>
        /// PAKET 3B-fix-11 (Sorun 2): SPIN'e bastı ama parametre eksik durumunda gösterilecek mesaj.
        /// yapilacaklar listesinden ilk 2 madde (parametre kısmı) — "X seç + Uygula bas".
        /// </summary>
        private string ParametreEksikMesaj()
        {
            var v = AdimYoneticisi?.MevcutAdimVerisi;
            if (v?.yapilacaklar == null || v.yapilacaklar.Length == 0)
                return "Önce talimatları tamamla";
            if (v.yapilacaklar.Length >= 2)
                return $"Önce {v.yapilacaklar[0]} + {v.yapilacaklar[1]}";
            return $"Önce {v.yapilacaklar[0]}";
        }

        private static void ScriptedModalKopruModalAcikReset()
        {
            try
            {
                var prop = typeof(ScriptedModalKopru).GetProperty("ModalAcik",
                    BindingFlags.Static | BindingFlags.Public);
                var setter = prop?.GetSetMethod(true); // private setter dahil
                if (setter != null)
                {
                    setter.Invoke(null, new object[] { false });
                    Debug.Log("[TutorialOyunYoneticisi] ScriptedModalKopru.ModalAcik reset → false");
                }
                else
                {
                    Debug.LogWarning("[TutorialOyunYoneticisi] ScriptedModalKopru.ModalAcik setter bulunamadı");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[TutorialOyunYoneticisi] ModalAcik reset fail: " + e.Message);
            }
        }

        private void OnDestroy()
        {
            if (AdimYoneticisi != null)
            {
                AdimYoneticisi.OnAdimDegisti -= AdimDegisti;
                AdimYoneticisi.OnTutorialBitti -= TutorialBitti;
            }
            if (Ornek == this) Ornek = null;
        }

        private IEnumerator Start()
        {
            yield return null;
            yield return new WaitForSeconds(1.0f); // OyunYoneticisi.Start tamamlansın

            YedekSaveTemizlik();

            // AyarlarButton listener override
            var ayarlarBtnGo = GameObject.Find("AyarlarButton");
            Button ayarlarBtn = ayarlarBtnGo != null ? ayarlarBtnGo.GetComponent<Button>() : null;
            var pk = Object.FindObjectOfType<PanelKopru>();
            if (ayarlarBtn != null)
            {
                ayarlarBtn.onClick.RemoveAllListeners();
                ayarlarBtn.onClick.AddListener(() =>
                {
                    GlowDurdur();
                    DigerButonlariAktiflestir();
                    if (pk != null) pk.AyarlarButonunaBasildi();
                    StartCoroutine(IframeKonumAyarla());
                    if (!_panelAcildi)
                    {
                        _panelAcildi = true;
                        StartCoroutine(PanelAcildiSonrasi());
                    }
                });
            }

            // Diğer butonlar pasif (Tutorial T_SON sonuna kadar ya da panel açılana kadar)
            DigerButonlariPasiflestir();

            // PAKET 3B-fix-3: BAKIYE YÜKLE tamamen gizle (Görev Takip Paneli onun yerine geçer)
            var bakiyeBtn = GameObject.Find("BakiyeYukleButon");
            if (bakiyeBtn != null)
            {
                bakiyeBtn.SetActive(false);
                Debug.Log("[TutorialOyunYoneticisi] BAKIYE YÜKLE gizlendi.");
            }

            // PAKET 3B-fix-3: "Hoş Geldiniz X" TMP_Text'lerini gizle (runtime içerik taraması)
            HosgeldinGizle();

            // PAKET 3B-fix-4 (Sorun 2): ButtonCevir.onClick'e tutorial spin counter listener ekle.
            // PersistentCall (OyunYoneticisi.SpinButon) KORUNUR — bu sadece ek listener.
            var spinBtnGo = GameObject.Find("ButtonCevir");
            if (spinBtnGo != null)
            {
                _spinBtnRef = spinBtnGo.GetComponent<Button>();
                if (_spinBtnRef != null)
                {
                    // PAKET 3B-fix-12 (İş 1): SpinGuardOverlay yarat (ButtonCevir üstüne sibling)
                    SpinGuardOverlayYarat(spinBtnGo);

                    _spinBtnRef.onClick.AddListener(() =>
                    {
                        HatirlatmaServisi.Ornek?.AktiviteHaberVer();
                        if (!ParametreSuanTamam())
                        {
                            // PAKET 3B-fix-11 (Sorun 2): SPIN'e bastı ama parametre eksik → anlık uyarı
                            string eksikMsg = ParametreEksikMesaj();
                            HatirlatmaServisi.Ornek?.ZorlaGoster(eksikMsg);
                            Debug.Log("[TutorialOyunYoneticisi] Spin parametre bekleniyor — uyarı: " + eksikMsg);
                            return;
                        }
                        TutorialSpinSayaci++;
                        Debug.Log($"[TutorialOyunYoneticisi] TutorialSpinSayaci = {TutorialSpinSayaci}");
                    });
                    Debug.Log("[TutorialOyunYoneticisi] ButtonCevir tutorial spin listener eklendi.");
                }
            }

            // TutorialAdimGoster İLERİ click subscribe
            yield return new WaitForSeconds(0.1f); // TutorialAdimGoster Awake tamamlansın
            if (TutorialAdimGoster.Ornek != null)
                TutorialAdimGoster.Ornek.OnIleriTiklandi += IleriTiklandi;

            // T1 modal
            if (TutorialModalKopru.Ornek != null)
                yield return TutorialModalKopru.Ornek.ModalGoster(T1_METIN);

            // T1 sonrası AyarlarButton glow
            if (ayarlarBtn != null)
                _glowCoroutine = StartCoroutine(AyarlarButtonGlow(ayarlarBtn));
        }

        // === T1 sonrası akış: panel açıldı → T2'ye geç ===

        private IEnumerator PanelAcildiSonrasi()
        {
            yield return new WaitForSeconds(1.5f); // panel.html iframe yüklenip hazırlansın

            // PAKET 3B-fix-10 (İş 1): Tutorial = "ayar etkisi gösterimi" sahnesi.
            // 50K bakiye + 1000 TL bahis → büyük rakamlarla çalış, ayar değişimi belirgin hissedilsin.
            // 25 spin × 1000 TL = 25K max kayıp; 50K bakiye yeterli pay.
            var oy = Object.FindObjectOfType<OyunYoneticisi>();
            if (oy != null)
            {
                oy.AnlaticiBakiyeyiSifirla(50000);
                oy.AdminBahisAyarla(1000);
                // PAKET 3B-fix-13 (Bug 1): Tutorial boyunca otomatik bonus tetiklenmesini engelle.
                // T11 manuel bonus AdminManuelBonusBaslat ile scatter'dan bağımsız çalışır.
                oy.scatterChanceNormal = 0f;
                Debug.Log("[TutorialOyunYoneticisi] Bakiye=50000 + Bahis=1000 + scatterChanceNormal=0 (eğitim modu).");
            }

            // PAKET 3B-fix-10 (İş 3): Tutorial sırasında dikkat dağıtıcı butonları gizle
            string[] gizleIsimleri = { "BonusSatinAlButton", "LoginButton", "YoneticiButton" };
            foreach (var ad in gizleIsimleri)
            {
                var go = GameObject.Find(ad);
                if (go != null)
                {
                    go.SetActive(false);
                    Debug.Log($"[TutorialOyunYoneticisi] {ad} gizlendi.");
                }
                else
                {
                    Debug.LogWarning($"[TutorialOyunYoneticisi] {ad} sahnede bulunamadı.");
                }
            }

            if (AdimYoneticisi != null)
                AdimYoneticisi.AdimGec(TutorialAdimYoneticisi.TutorialAdimId.T2);
        }

        // === AdimYoneticisi event handler'ları ===

        private void AdimDegisti(AdimVerisi v)
        {
            HatirlatmaServisi.Ornek?.AktiviteHaberVer(); // PAKET 3B-fix-9 (Feature 3) — yeni adım, timer reset

            // Önceki adımın vurgularını temizle
#if UNITY_WEBGL && !UNITY_EDITOR
            TumVurgulariKapat();
#endif
            // AdimGoster'ı gizle (modal süresince görünmesin, modal sonra göster)
            TutorialAdimGoster.Ornek?.Gizle();

            StartCoroutine(AdimAkisi(v));
        }

        private IEnumerator AdimAkisi(AdimVerisi v)
        {
            Debug.Log($"[TutorialOyunYoneticisi] AdimAkisi başladı: {v.id}");
            _ileriTiklandi = false; // PAKET 3B-fix-6: yeni adım, atla flag'i reset

            // === Modal A (mesajBaslangic) — her zaman göster ===
            if (TutorialModalKopru.Ornek != null && !string.IsNullOrEmpty(v.mesajBaslangic))
                yield return TutorialModalKopru.Ornek.ModalGoster(v.mesajBaslangic);

            // Pasif (T2, T_SON): Modal A sonrası otomatik geçiş
            if (!v.aktifMi)
            {
                AdimYoneticisi?.IleriTiklandi();
                yield break;
            }

            // === Modal B (mesajAksiyon) ===
            if (TutorialModalKopru.Ornek != null && !string.IsNullOrEmpty(v.mesajAksiyon))
                yield return TutorialModalKopru.Ornek.ModalGoster(v.mesajAksiyon);

            // === Vurgu aç ===
            if (v.vurguSelectorlari != null)
            {
                foreach (var sel in v.vurguSelectorlari)
                {
#if UNITY_WEBGL && !UNITY_EDITOR
                    VurguAc(sel);
#endif
                }
            }

            // === AdimGoster göster + İLERİ ANINDA AKTİF (atla butonu) ===
            int sira = v.sira;
            Debug.Log($"[TutorialOyunYoneticisi] AdimGoster.AdimGoster: sira={sira}, altSayac={v.altSayac ?? "-"}");
            TutorialAdimGoster.Ornek?.AdimGoster(sira, v.altBaslik, v.yapilacaklar, v.altSayac);
            TutorialAdimGoster.Ornek?.IleriAktif(true); // PAKET 3B-fix-6: hemen aktif — kullanıcı atlayabilir

            // PAKET 3B-fix-12 (İş 2): Adım girişinde proaktif uyarı — parametre eksikse kullanıcı SPIN'e
            // tıklayıp uyarı beklemesin. Modal kapanış animasyonu için 0.5sn bekle.
            yield return new WaitForSecondsRealtime(0.5f);
            if (!ParametreSuanTamam())
            {
                HatirlatmaServisi.Ornek?.ZorlaGoster(ParametreEksikMesaj());
            }

            // === Koşul VEYA İLERİ tıklamasını bekle ===
            while (!_ileriTiklandi)
            {
                int spin = TutorialAdimYoneticisi.MevcutSpinAl();
                if (AdimYoneticisi != null && AdimYoneticisi.KosulSagla(spin)) break;
                yield return null;
            }
            Debug.Log($"[TutorialOyunYoneticisi] Adım bitti (ileriTiklandi={_ileriTiklandi}): {v.id}");

            // === Vurgu kapat + AdimGoster gizle ===
            TutorialAdimGoster.Ornek?.Gizle();
#if UNITY_WEBGL && !UNITY_EDITOR
            TumVurgulariKapat();
#endif

            // === Modal C (pedagojik özet — atlasa da gösterilsin) ===
            if (TutorialModalKopru.Ornek != null && !string.IsNullOrEmpty(v.mesajKapanis))
                yield return TutorialModalKopru.Ornek.ModalGoster(v.mesajKapanis);

            // PAKET 3B-fix-14 (Fix 2): Aktif adım bittikten sonra bonusOtomatikSpinPeriyodu reset.
            // T5'te kullanıcı periyot ayarlar → o adımın spin'leri bitti → birikim silinir → T6-T11
            // sonraki adımlarda otomatik bonus tetiklenmez. T1-T4 zaten 0 olduğu için no-op.
            var oyRef = Object.FindObjectOfType<OyunYoneticisi>();
            if (oyRef != null) oyRef.AdminSetBonusOtomatikSpinPeriyodu(0);

            // === Modal C kapandı → ANINDA sıradaki adım ===
            AdimYoneticisi?.IleriTiklandi();
        }

        private void IleriTiklandi()
        {
            // PAKET 3B-fix-6: İLERİ "atla" butonu — flag set, AdimAkisi yield-while görür ve Modal C'ye geçer.
            // Doğrudan AdimYoneticisi.IleriTiklandi() çağırmıyoruz; otomatik geçiş Modal C sonrası.
            _ileriTiklandi = true;
            HatirlatmaServisi.Ornek?.AktiviteHaberVer(); // PAKET 3B-fix-9 (Feature 3)
        }

        // === T_SON kapanış akışı ===

        private void TutorialBitti()
        {
            Debug.Log("[TutorialOyunYoneticisi] Tutorial bitti — kapanış akışı");
            StartCoroutine(KapanisAkisi());
        }

        private IEnumerator KapanisAkisi()
        {
            // Tüm vurguları kapat
#if UNITY_WEBGL && !UNITY_EDITOR
            TumVurgulariKapat();
            TutorialPaneliKapat(); // panel.html iframe DOM remove
#endif
            TutorialAdimGoster.Ornek?.Gizle();

            // Save temizlik (yeni başlangıç için)
            SaveLoadServisi.Sil();
            PlayerPrefs.DeleteKey("KullaniciAdi");
            PlayerPrefs.DeleteKey("KumarRestoreModuActif");
            PlayerPrefs.Save();
            KullaniciVerileri.KullaniciAdi = "Misafir";

            yield return new WaitForSeconds(0.3f);
            SceneManager.LoadScene("01_GirisScene");
        }

        // === Yardımcılar ===

        private static void YedekSaveTemizlik()
        {
            SaveLoadServisi.Sil();
            PlayerPrefs.DeleteKey("KullaniciAdi");
            PlayerPrefs.DeleteKey("KumarRestoreModuActif");
            PlayerPrefs.Save();
            KullaniciVerileri.KullaniciAdi = "Eğitim Modu";
        }

        // AyarlarButton hariç pasif/aktif edilecek ortak buton listesi
        private static readonly string[] DIGER_BUTON_ISIMLERI = {
            "ButtonCevir",
            "bahisArttirButon",
            "bahisAzaltButon",
            "BakiyeYukleButon",
            "BonusSatinAlButton",
            "OtomatikSpinButton",
            "paraCekButon",
            "ParaCekButon",
        };

        private static void HosgeldinGizle()
        {
            // "Hoş Geldin..." başlangıçlı TMP_Text'leri runtime'da gizle. HosgeldinizText script'i
            // source kodda yok (broken reference), m_Name de belirsiz — bu yüzden içerik taraması.
            var tumTextler = Object.FindObjectsOfType<TMPro.TextMeshProUGUI>(true);
            int gizliSayisi = 0;
            foreach (var txt in tumTextler)
            {
                if (txt == null || string.IsNullOrEmpty(txt.text)) continue;
                if (txt.text.StartsWith("Hoş Geldin") || txt.text.StartsWith("Hoş geldin"))
                {
                    txt.gameObject.SetActive(false);
                    gizliSayisi++;
                }
            }
            Debug.Log($"[TutorialOyunYoneticisi] HosgeldinGizle: {gizliSayisi} TMP_Text gizlendi.");
        }

        private static void DigerButonlariPasiflestir()
        {
            int sayim = 0;
            foreach (var ad in DIGER_BUTON_ISIMLERI)
            {
                var go = GameObject.Find(ad);
                if (go == null) continue;
                var btn = go.GetComponent<Button>();
                if (btn != null) { btn.interactable = false; sayim++; }
            }
            Debug.Log($"[TutorialOyunYoneticisi] DigerButonlariPasiflestir: {sayim}/{DIGER_BUTON_ISIMLERI.Length} buton kapatıldı.");
        }

        private static void DigerButonlariAktiflestir()
        {
            int sayim = 0;
            foreach (var ad in DIGER_BUTON_ISIMLERI)
            {
                var go = GameObject.Find(ad);
                if (go == null) continue;
                var btn = go.GetComponent<Button>();
                if (btn != null) { btn.interactable = true; sayim++; }
            }
            Debug.Log($"[TutorialOyunYoneticisi] DigerButonlariAktiflestir: {sayim}/{DIGER_BUTON_ISIMLERI.Length} buton aktif edildi.");
        }

        private IEnumerator IframeKonumAyarla()
        {
            yield return new WaitForSeconds(0.1f);
#if UNITY_WEBGL && !UNITY_EDITOR
            PaneliSolaAl();
            DropdownTooltipEkle(); // PAKET 3B-fix-7: oyun modu <option> title attribute (native tooltip)
#endif
        }

        // === AyarlarButton glow (SpinButtonAnimator pattern adapte) ===

        private IEnumerator AyarlarButtonGlow(Button ayarlarBtn)
        {
            if (ayarlarBtn == null) yield break;
            var btnRt = ayarlarBtn.GetComponent<RectTransform>();
            var parent = ayarlarBtn.transform.parent;
            if (btnRt == null || parent == null) yield break;

            _glowGo = new GameObject("AyarlarButtonGlow",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _glowGo.transform.SetParent(parent, false);
            int btnIdx = ayarlarBtn.transform.GetSiblingIndex();
            _glowGo.transform.SetSiblingIndex(Mathf.Max(0, btnIdx));

            var glowRt = _glowGo.GetComponent<RectTransform>();
            glowRt.anchorMin = btnRt.anchorMin;
            glowRt.anchorMax = btnRt.anchorMax;
            glowRt.pivot = btnRt.pivot;
            glowRt.anchoredPosition = btnRt.anchoredPosition;
            glowRt.sizeDelta = btnRt.sizeDelta + new Vector2(20f, 20f);

            var img = _glowGo.GetComponent<Image>();
            img.color = new Color(0.83f, 0.69f, 0.22f, 0.4f);
            img.raycastTarget = false;

            _ayarlarBtnTransform = ayarlarBtn.transform;
            _ayarlarBaseScale = _ayarlarBtnTransform.localScale;

            const float PERIOD = 1.5f;
            const float PULSE_SCALE = 0.06f;
            float elapsed = 0f;

            while (_glowGo != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = (elapsed % PERIOD) / PERIOD;
                float ping = Mathf.PingPong(t * 2f, 1f);
                float ease = ping * ping * (3f - 2f * ping);

                if (_ayarlarBtnTransform != null)
                    _ayarlarBtnTransform.localScale = _ayarlarBaseScale * (1f + ease * PULSE_SCALE);

                if (img != null)
                {
                    var c = img.color;
                    c.a = Mathf.Lerp(0.4f, 0.9f, ease);
                    img.color = c;
                }

                yield return null;
            }
        }

        private void GlowDurdur()
        {
            if (_glowCoroutine != null) { StopCoroutine(_glowCoroutine); _glowCoroutine = null; }
            if (_glowGo != null) { Destroy(_glowGo); _glowGo = null; }
            if (_ayarlarBtnTransform != null)
            {
                _ayarlarBtnTransform.localScale = _ayarlarBaseScale;
                _ayarlarBtnTransform = null;
            }
        }
    }
}
