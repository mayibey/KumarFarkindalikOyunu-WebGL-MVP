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

        /// <summary>
        /// PAKET 3B-fix-4 (Sorun 2): 04 sahnesinde SenaryoYoneticisi GameObject YOK → toplamSpin
        /// çalışmıyor. ButtonCevir.onClick'e runtime listener eklenerek her spin tıklamasında
        /// bu sayaç artırılır. KosulSagla bu sayacı kullanır.
        /// </summary>
        public int TutorialSpinSayaci { get; private set; }

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
                var spinBtn = spinBtnGo.GetComponent<Button>();
                if (spinBtn != null)
                {
                    spinBtn.onClick.AddListener(() =>
                    {
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

            // Bahis koruma: tutorial sırasında min bahis (1 TL) — bakiye sıfırlanmasın (45+ spin × default 200-500)
            var oy = Object.FindObjectOfType<OyunYoneticisi>();
            if (oy != null)
            {
                oy.AdminBahisAyarla(1);
                Debug.Log("[TutorialOyunYoneticisi] AdminBahisAyarla(1) — tutorial bahis koruma");
            }

            if (AdimYoneticisi != null)
                AdimYoneticisi.AdimGec(TutorialAdimYoneticisi.TutorialAdimId.T2);
        }

        // === AdimYoneticisi event handler'ları ===

        private void AdimDegisti(AdimVerisi v)
        {
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
            Debug.Log($"[TutorialOyunYoneticisi] AdimAkisi başladı: {v.id}, ModalKopru.Ornek null={TutorialModalKopru.Ornek == null}, Goster.Ornek null={TutorialAdimGoster.Ornek == null}");

            // === Modal A (mesajBaslangic) — her zaman göster ===
            if (TutorialModalKopru.Ornek != null && !string.IsNullOrEmpty(v.mesajBaslangic))
                yield return TutorialModalKopru.Ornek.ModalGoster(v.mesajBaslangic);

            // PAKET 3B-fix-4 (Sorun 1): pasif adımlarda Modal A kapanışında otomatik geçiş.
            // T_SON ayrı kontrol gereksiz — IleriTiklandi içinde T_SON ise OnTutorialBitti tetiklenir,
            // diğer pasif (T2) ise sıradaki adıma geçer.
            if (!v.aktifMi)
            {
                AdimYoneticisi?.IleriTiklandi();
                yield break;
            }

            // === Modal B (mesajAksiyon) — varsa (aktif adımlar için) ===
            if (TutorialModalKopru.Ornek != null && !string.IsNullOrEmpty(v.mesajAksiyon))
                yield return TutorialModalKopru.Ornek.ModalGoster(v.mesajAksiyon);

            // === Vurgu aç (panel.html) ===
            if (v.vurguSelectorlari != null)
            {
                foreach (var sel in v.vurguSelectorlari)
                {
#if UNITY_WEBGL && !UNITY_EDITOR
                    VurguAc(sel);
#endif
                }
            }

            // === AdimGoster göster (sira + altBaslik + yapılacaklar + altSayac) ===
            int sira = v.sira; // PAKET 3B-fix-5: T3_* için hepsi 3 (override)
            Debug.Log($"[TutorialOyunYoneticisi] AdimGoster.AdimGoster çağrılıyor: sira={sira}, altSayac={v.altSayac ?? "-"}");
            TutorialAdimGoster.Ornek?.AdimGoster(sira, v.altBaslik, v.yapilacaklar, v.altSayac);

            // === Aktif adım: KosulSagla yield-while ile bekle ===
            // (pasif adımlar Modal A sonrası IleriTiklandi ile zaten çıktı — buraya gelmez)
            while (true)
            {
                int spin = TutorialAdimYoneticisi.MevcutSpinAl();
                if (AdimYoneticisi != null && AdimYoneticisi.KosulSagla(spin)) break;
                yield return null;
            }
            Debug.Log($"[TutorialOyunYoneticisi] Koşul sağlandı: {v.id}");

            // === Modal C (mesajKapanis) — pedagojik özet ===
            if (!string.IsNullOrEmpty(v.mesajKapanis))
            {
                TutorialAdimGoster.Ornek?.Gizle();
#if UNITY_WEBGL && !UNITY_EDITOR
                TumVurgulariKapat();
#endif
                if (TutorialModalKopru.Ornek != null)
                    yield return TutorialModalKopru.Ornek.ModalGoster(v.mesajKapanis);
                TutorialAdimGoster.Ornek?.AdimGoster(sira, v.altBaslik, v.yapilacaklar, v.altSayac); // sayaç tekrar göster
            }

            // === İLERİ aktif (kullanıcı tıklayınca sonraki adım) ===
            TutorialAdimGoster.Ornek?.IleriAktif(true);
        }

        private void IleriTiklandi()
        {
            AdimYoneticisi?.IleriTiklandi();
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
