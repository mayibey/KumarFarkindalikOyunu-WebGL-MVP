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
    ///   5. AdimYoneticisi.OnTutorialBitti → KapanisAkisi: UI gizle + T_SON modal + SerbestTestModunaGec (sahne 04'te KALIR, LoadScene yok)
    /// </summary>
    [Preserve]
    public class TutorialOyunYoneticisi : MonoBehaviour
    {
        public const int TUTORIAL_SAHNE_BUILD_INDEX = 3;

        public static TutorialOyunYoneticisi Ornek { get; private set; }

        public TutorialAdimYoneticisi AdimYoneticisi { get; private set; }
        public TutorialAdminEnjeksiyonu Enjeksiyon { get; private set; }

        private const string T1_METIN =
            "Hoş geldiniz. Az önce bir <color=#F24D40>kumar bağımlısının</color> yaşadıklarını gördük. " +
            "Şimdi <color=#4DCC59>sahne arkasını</color> birlikte göreceğiz. Sağ-alttaki <color=#5BA0FF>AYARLAR</color> butonuna tıklayın, " +
            "<color=#F24D40>manipülasyon panelini</color> açalım.";

        // PAKET 8: T1 karşılama sonrası Normal oyun bilgilendirme — T3_NORMAL adımı kaldırıldı,
        // bu kavram tek bir modal ile başta açıklanır.
        private const string T1_NORMAL_INFO =
            "<color=#4DCC59>Normal oyun</color>: <color=#5BA0FF>manipülasyon kapalı</color>, oyun kendi kurallarında akar — adil <color=#FFD933>RTP</color> ile.\n\n" +
            "Sonra <color=#F24D40>4 manipülasyon senaryosunu</color> göreceğiz:\n" +
            "• <color=#F24D40>Taze Kan</color> (Hook)\n" +
            "• <color=#F24D40>Az Az Kayıp</color> (Yontma)\n" +
            "• <color=#F24D40>Kaçış Engelleme</color> (Tutma)\n" +
            "• <color=#F24D40>Bakiye Tüketme</color> (Koruma)\n\n" +
            "Her senaryoda operatörün nasıl müdahale ettiğini <color=#4DCC59>kendi gözlerimizle</color> göreceğiz.";

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void PaneliSolaAl();
        [DllImport("__Internal")] private static extern void DropdownTooltipEkle();
        [DllImport("__Internal")] private static extern void DropdownAutoRevertEkle(); // PAKET 5: Uygula basmadan kaçınca revert
        [DllImport("__Internal")] private static extern void ToggleKapat(string id); // HOTFIX T6YO: yeniOyuncuToggle force kapat
        [DllImport("__Internal")] private static extern void ToggleAc(string id);    // PAKET 14-FAZ7: T6YO ters — toggle force aç
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

        // PAKET 4-FAZ-2: T_SON sonrası serbest test → original değerler restore için cache
        private static float _orijinalScatterChance = 0.005f;
        private static int _orijinalBonusOtomatikPeriyot = 0;
        private static int _orijinalBahis = 10;
        // PAKET 14-FAZ11: T6YO için maxOdeme orijinal değer cache + restore. T6YO girişinde 99999 set edilir
        // (yeniOyuncu_acik pattern 2500/3000 TL hedefli, limit-aware kayıp grid'e çevirmesin). T_SON sonrası restore.
        private static int _orijinalMaxOdeme = 0;
        private static bool _maxOdemeCachelendi = false;

        // PAKET 14 (İş 4): T5 bonus tetik spin'i (bonusTest_100, ToplamHamKazanc=0) PlayKayipHorn
        // çalıyor (DonusAkis "net<=0 → kayıp ses"). T5 başında klibi cache+null, T6_YENI_OYUNCU
        // başında restore. T5'in 2 spin'i sessiz olur — bonusTest_0 (kayıp+bonus yok) pedagoji
        // ses olmadan da net.
        private static AudioClip _orijinalSpinSonucKayipClip;
        private static bool _spinSonucKayipClipCachelendi = false;

        // PAKET 4-HOTFIX (Bug 1): Spin animasyonu bitince sayaç + scripted motor ilerlet
        // (tıklama anında değil — Modal C erken açılma sorunu fix)
        private bool _spinBekliyor = false;
        private bool _oncekiSpinCalisiyor = false;
        private OyunYoneticisi _oyRef;

        // PAKET 9: T4 (Çarpan Olasılığı) 2-aşamalı akış state — TutorialAdminEnjeksiyonu okur
        public static bool T4AraModalGosterildi { get; set; }
        public static bool T4IkinciAsamaBasladi { get; set; }

        // PAKET 9: T5 (Bonus Sembolü) 2-aşamalı akış state
        public static bool T5AraModalGosterildi { get; set; }
        public static bool T5IkinciAsamaBasladi { get; set; }

        // PAKET 6C2: T6_YENI_OYUNCU 2-aşamalı akış state (TutorialAdminEnjeksiyonu okur)
        public static bool T6AraModalGosterildi { get; set; }
        public static bool T6IkinciAsamaBasladi { get; set; }

        // PAKET 14-FAZ8: T6YO — kullanıcı toggle AÇTIĞINDA 1.aşama tek seferlik tetiklensin
        // (her açma-kapama döngüsünde tekrar pattern yenilemesin).
        public static bool T6IlkAsamaPatternBasladi { get; set; }

        // PAKET 14-FAZ14: Aktif adımın spin net listesi (03'teki ilerleme çubuğu pattern'i).
        // Her spin sonu net (bakiye farkı) eklenir, adım değişiminde temizlenir.
        // PAKET 14-FAZ16: OturumKazanc yerine BahisPanelMevcutBakiye fark hesabı (03 AnlaticiSeritKopru:437
        // referansı). OturumKazanc bahis çıkarmayı tam yakalayamıyordu, 2000 brüt kazanç KIRMIZI görünüyordu.
        public static System.Collections.Generic.List<int> AktifAdimSpinNetleri =
            new System.Collections.Generic.List<int>();
        private static long _oncekiBakiye = 0;
        // PAKET 14-FAZ34.8 BUG N FIX: OturumKazanc fark hesabı için snapshot. SonOdeme race condition'ı
        // by-pass eder. OyunYoneticisi.OturumKazanc spin bittiğinde DonusAkisServisi tarafından artırılır
        // (gerçek ödeme), pre-compute simulasyonu OturumKazanc'ı etkilemez → race yok.
        private static long _oncekiOturumKazanc = 0;

        // PAKET 6D: T8 (Ödeme) + T11 (Çarpan Zorla) 2-aşamalı akış state
        public static bool T8AraModalGosterildi { get; set; }
        public static bool T8IkinciAsamaBasladi { get; set; }
        public static bool T11AraModalGosterildi { get; set; }
        public static bool T11IkinciAsamaBasladi { get; set; }

        // PAKET 4-HOTFIX: T3_TUTMA pedagojik ara modaller (Spin 2 sonrası tahmin, Spin 3 sonrası devam)
        private const string TAHMIN_MODAL =
            "<color=#F24D40>DİKKATLE İZLEYELİM!</color>\n\n" +
            "Oyuncu <color=#FFD933>2 kez</color> kaybetti değil mi? Çıkmayı düşünüyor belki.\n\n" +
            "Şimdi bir sonraki spin'de OYUNCUYA <color=#4DCC59>KAZANÇ GELECEK</color>. İzleyelim, görelim, hissedelim.\n\n" +
            "Sistem oyuncuyu nasıl <color=#F24D40>TUTUYOR</color>, kendi gözlerimizle göreceğiz.";

        private const string DEVAM_MODAL =
            "Gördük mü?\n\n" +
            "Oyuncu tam çıkacağı anda <color=#4DCC59>KAZANÇ</color> geldi. 'İyi ki kalmışım' dedirtti.\n\n" +
            "Bir kere daha izleyelim — aynı <color=#F24D40>kayıp/kayıp/KAZANÇ</color> döngüsü tekrar yaşanacak.\n\n" +
            "Bu sefer <color=#4DCC59>farkındayız</color>. Bu sefer <color=#4DCC59>şüpheciyiz</color>. Kanıtlayalım.";

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

        // Space tuşu Tutorial parametre bekleyen adımda spin'i bypass ediyordu (overlay sadece mouse raycast yutuyor).
        // TutorialAdminEnjeksiyonu.Update'ten parametreTamam state'i set edilir; SpinButonImpl tek satır guard ile okur.
        public static bool SpinKilitli { get; set; } = false;

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
                // PAKET 14-FAZ11: Tutorial sahnesi dışına çıkışta maxOdeme restore (T_SON akışına gitmediyse).
                if (_maxOdemeCachelendi)
                {
                    var oyExit = Object.FindObjectOfType<OyunYoneticisi>();
                    if (oyExit != null) oyExit.AdminSetMaxOdeme(_orijinalMaxOdeme);
                    _maxOdemeCachelendi = false;
                    Debug.Log($"[Tutorial] Defansif restore (sahne çıkışı): maxOdeme={_orijinalMaxOdeme}");
                }
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

            // PAKET 14-FAZ8: T6YO 2-aşama bayrakları önceki oturumdan kalıntı reset
            T6AraModalGosterildi = false;
            T6IkinciAsamaBasladi = false;
            T6IlkAsamaPatternBasladi = false;

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
            if (v == null) return "Önce talimatları tamamla";

            // PAKET 14-FAZ3 (İş 1): T4/T5 için aşamaya göre dinamik mesaj
            if (v.id == TutorialAdimYoneticisi.TutorialAdimId.T4)
            {
                return T4AraModalGosterildi
                    ? "Çarpan olasılığını %0 yap ve Uygula bas"
                    : "Çarpan olasılığını %100 yap ve Uygula bas";
            }
            if (v.id == TutorialAdimYoneticisi.TutorialAdimId.T5)
            {
                return T5AraModalGosterildi
                    ? "Bonus olasılığını %0 yap ve Uygula bas"
                    : "Bonus olasılığını %100 yap ve Uygula bas";
            }

            if (v.yapilacaklar == null || v.yapilacaklar.Length == 0)
                return "Önce talimatları tamamla";
            if (v.yapilacaklar.Length >= 2)
                return $"Önce {v.yapilacaklar[0]} + {v.yapilacaklar[1]}";
            return $"Önce {v.yapilacaklar[0]}";
        }

        // PAKET 14-FAZ3 (İş 2): OyunYoneticisi._bonusUIServisi (private field) → BonusUIServisi.SetSpinSonucKayipClip
        // çağrısı reflection ile yapılır. Tutorial T5 bonus tetik spin'inde horn'un BonusUIServisi yolundan
        // çalmasını engelleyip T6'da restore eder.
        private static void BonusUIServisiKayipClipAyarla(OyunYoneticisi oy, AudioClip clip)
        {
            if (oy == null) return;
            try
            {
                var field = typeof(OyunYoneticisi).GetField("_bonusUIServisi",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var servis = field?.GetValue(oy);
                if (servis == null) { Debug.LogWarning("[Tutorial] _bonusUIServisi reflection null"); return; }
                var metod = servis.GetType().GetMethod("SetSpinSonucKayipClip",
                    BindingFlags.Public | BindingFlags.Instance);
                metod?.Invoke(servis, new object[] { clip });
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Tutorial] BonusUIServisi kayıp clip set hata: " + e.Message);
            }
        }

        // Space tuşu parametre bekleyen adımda yutulduğunda overlay click ile aynı uyarıyı tetikler.
        public void SpinKilitliUyariGoster()
        {
            string eksikMsg = ParametreEksikMesaj();
            HatirlatmaServisi.Ornek?.ZorlaGoster(eksikMsg);
            Debug.Log("[TutorialOyunYoneticisi] SpinKilitli (Space yutuldu) → uyarı: " + eksikMsg);
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
                        // PAKET 14-FAZ27: Her spin başlangıcında RNG state'i sabit seed'e reset — Tutorial deterministik.
                        // Unity engine RNG (pre-compute Simulasyon.cs) + Tutorial pattern RNG ikisi de aynı yerden başlar.
                        UnityEngine.Random.InitState(12345);
                        TutorialSenaryoMotoru.RngResetle();
                        // PAKET 4-HOTFIX (Bug 1): Sayaç ve SpinTamamlandi() artık tıklamada değil,
                        // spin animasyonu bittiğinde Update polling ile tetiklenir (Modal C erken açılma fix).
                        _spinBekliyor = true;
                        Debug.Log("[TutorialOyunYoneticisi] Spin bekleniyor — animasyon bitince sayaç ilerleyecek.");
                    });
                    Debug.Log("[TutorialOyunYoneticisi] ButtonCevir tutorial spin listener eklendi.");
                }
            }

            // TutorialAdimGoster İLERİ click subscribe
            yield return new WaitForSeconds(0.1f); // TutorialAdimGoster Awake tamamlansın
            if (TutorialAdimGoster.Ornek != null)
                TutorialAdimGoster.Ornek.OnIleriTiklandi += IleriTiklandi;

            // T1 modal (sadece Karşılama + Ayarlar tıklama davet — T1_NORMAL_INFO T3_HOOK Modal A sonrasına taşındı)
            if (TutorialModalKopru.Ornek != null)
            {
                yield return TutorialModalKopru.Ornek.ModalGoster(T1_METIN);
            }

            // T1 sonrası AyarlarButton glow
            if (ayarlarBtn != null)
                _glowCoroutine = StartCoroutine(AyarlarButtonGlow(ayarlarBtn));
        }

        // === T1 sonrası akış: panel açıldı → T2'ye geç ===

        private IEnumerator PanelAcildiSonrasi()
        {
            // PAKET 14-FAZ12: 1.5sn → 0.3sn. Modal ile panel eş zamanlı açılsın — iframe genelde cache'li,
            // yükleme hızlı. Çok agresif olursa (0sn) ilk açılışta iframe hazır olmadan T2 başlayabilir.
            yield return new WaitForSeconds(0.3f);

            // PAKET 3B-fix-10 (İş 1): Tutorial = "ayar etkisi gösterimi" sahnesi.
            // 50K bakiye + 1000 TL bahis → büyük rakamlarla çalış, ayar değişimi belirgin hissedilsin.
            // 25 spin × 1000 TL = 25K max kayıp; 50K bakiye yeterli pay.
            var oy = Object.FindObjectOfType<OyunYoneticisi>();
            if (oy != null)
            {
                // PAKET 4-FAZ-2: Original değerleri cache (T_SON sonrası restore için)
                _orijinalScatterChance = oy.scatterChanceNormal;
                _orijinalBonusOtomatikPeriyot = oy.bonusOtomatikSpinPeriyodu;
                _orijinalBahis = oy.BotIcinBahis;
                Debug.Log($"[Tutorial] Original cache: scatter={_orijinalScatterChance}, periyot={_orijinalBonusOtomatikPeriyot}, bahis={_orijinalBahis}");

                // PAKET 14-FAZ31: Admin state'i Normal Oyun moduna resetle — Tutorial başlangıcında
                // admin senaryo preset (S2/S3 vb.) aktif kalabiliyordu → TryConsumeOncedenHesaplanan
                // OncedenHesaplanmisNormalSpinOdemeYenidenDogrulansinMi=true → Tutorial kaydı 5000 TL
                // policy bandına uymuyor → cache discard → fresh RNG simülasyonu (ARMUT 8 + 8+2x çarpan = 2000).
                // AdminNormalOyunUygula: _senaryoPresetAktif=false, _aktifAdminSenaryoIndex=-1,
                // _minOdemeTL=0, _maxOdemeTL=0, SpinPolitikasiniYenile (NormalSenaryo), cache temizle.
                // PAKET 14-FAZ32 (diyagnoz): admin state önce/sonra reflection ile inspect
                int oncekiMaxOdeme = oy.GetAdminMaxOdeme();
                bool oncekiPreset = (bool)(typeof(OyunYoneticisi).GetField("_senaryoPresetAktif", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(oy) ?? false);
                bool oncekiCarpan = oy.carpanUretimiAktif;
                Debug.Log($"[PanelAcildi ÖNCE] maxOdeme={oncekiMaxOdeme}, presetAktif={oncekiPreset}, carpanUretimiAktif={oncekiCarpan}");

                oy.AdminNormalOyunUygula();
                int araMaxOdeme = oy.GetAdminMaxOdeme();
                bool araPreset = (bool)(typeof(OyunYoneticisi).GetField("_senaryoPresetAktif", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(oy) ?? false);
                bool araCarpan = oy.carpanUretimiAktif;
                Debug.Log($"[PanelAcildi ARA] AdminNormalOyunUygula sonrası → maxOdeme={araMaxOdeme}, presetAktif={araPreset}, carpanUretimiAktif={araCarpan}");

                oy.AnlaticiBakiyeyiSifirla(50000);
                oy.AdminBahisAyarla(1000);
                // PAKET 3B-fix-13 (Bug 1): Tutorial boyunca otomatik bonus tetiklenmesini engelle.
                // T11 manuel bonus AdminManuelBonusBaslat ile scatter'dan bağımsız çalışır.
                oy.scatterChanceNormal = 0f;
                // PAKET 14-FAZ26: bonusOtomatikSpinPeriyodu Tutorial geneli kapalı (0). T5 pattern motor
                // 4 scatter düşürür → bonus tetik kontrollü. Otomatik periyot (slider %100 → 1) sabote etmesin.
                oy.AdminSetBonusOtomatikSpinPeriyodu(0);
                // PAKET 14-FAZ13: maxOdeme=99999 Tutorial geneline taşındı (T6YO branch'inden). Tüm Tutorial
                // pattern hedefleri (hook 2500/3000, tutma 2000, T6YO 2500/3000) limit-aware bypass.
                if (!_maxOdemeCachelendi)
                {
                    _orijinalMaxOdeme = oy.GetAdminMaxOdeme();
                    _maxOdemeCachelendi = true;
                    Debug.Log($"[Tutorial] Original maxOdeme cache: {_orijinalMaxOdeme}");
                }
                oy.AdminSetMaxOdeme(99999);
                int sonMaxOdeme = oy.GetAdminMaxOdeme();
                bool sonPreset = (bool)(typeof(OyunYoneticisi).GetField("_senaryoPresetAktif", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(oy) ?? false);
                bool sonCarpan = oy.carpanUretimiAktif;
                Debug.Log($"[PanelAcildi SONRA] AdminSetMaxOdeme(99999) sonrası → maxOdeme={sonMaxOdeme}, presetAktif={sonPreset}, carpanUretimiAktif={sonCarpan}");
                Debug.Log("[TutorialOyunYoneticisi] Bakiye=50000 + Bahis=1000 + scatter=0 + maxOdeme=99999 (eğitim modu).");

                // PAKET 4-FAZ-2: Bahis kilit (T3+ boyunca 1000 TL sabit, T_SON sonrası açılır)
                BahisKilitle(true);

                // PAKET 7+HOTFIX: WinFeedbackUI deaktif KALDIRILDI — basket animasyon artık KAZANÇ kutusundan
                // başlıyor (ekran ortası değil) → BÜYÜK KAZANÇ pop-up ile çakışma yok, ikisi paralel görünebilir
                // (kazanç kutusu üstte, BÜYÜK KAZANÇ ekran merkezinde, basket parabolic flight ile bakiyeye iner).
                // SerbestTestModunaGec restore satırı da artık no-op (deaktif yapılmamıştı).
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
            // PAKET 14-FAZ19: Adım geçiş RACE — yeni adımın UygulamaOnaylandi/degisimAnahtarlari state'leri
            // Update polling tarafından hesaplanana kadar (1 frame) önceki adımdan kalma parametreTamam=true
            // kalabilir. Geçişin ANINDA SpinKilitli=true zorla → kullanıcı boş pencerede spin atamaz.
            SpinKilitli = true;

            HatirlatmaServisi.Ornek?.AktiviteHaberVer(); // PAKET 3B-fix-9 (Feature 3) — yeni adım, timer reset

            // Önceki adımın vurgularını temizle
#if UNITY_WEBGL && !UNITY_EDITOR
            TumVurgulariKapat();
#endif
            // AdimGoster'ı gizle (modal süresince görünmesin, modal sonra göster)
            TutorialAdimGoster.Ornek?.Gizle();

            // PAKET 14-FAZ8: T6YO 1.aşama tetik bayrağı — yeni adıma geçişte reset (kalıntı önlemi).
            T6IlkAsamaPatternBasladi = false;
            // PAKET 14-FAZ18: Uygula onayı her adım başında reset — yeni adımda Uygula tekrar gerekli.
            TutorialAdminEnjeksiyonu.UygulamaOnaylandi = false;

            // PAKET 14-FAZ14: Spin geçmişi net listesi her adım başında sıfırlanır + bakiye cache reset.
            // PAKET 14-FAZ16: 03 referansı — net = simdikiBakiye - oncekiBakiye (bahis çıkarma + kazanç ekleme tek farkta).
            AktifAdimSpinNetleri.Clear();
            var oyAdim = Object.FindObjectOfType<OyunYoneticisi>();
            _oncekiBakiye = oyAdim != null ? oyAdim.BahisPanelMevcutBakiye() : 0L;
            // PAKET 14-FAZ34.8 BUG N FIX: OturumKazanc snapshot — bar net hesabı için referans.
            _oncekiOturumKazanc = oyAdim != null ? oyAdim.OturumKazanc : 0L;

            // PAKET 6C1/6C2/6C3/8/9: adım bazlı pattern motor yönetimi
            // PAKET 13: T3 senaryoları için defansif PatternBaslat — panel event-driven (AdminEnjeksiyonu
            // "oyunModu" case) tek başına yetmiyor; kullanıcı dropdown'a basmadan veya geçiş timing
            // sorununda pattern enjekte edilmiyordu (RNG fallback geçiyordu). Adım girişinde de tetikle.
            if (v.id == TutorialAdimYoneticisi.TutorialAdimId.T3_HOOK)
            {
                // PAKET 14-FAZ34 İş 7: ScriptedSpinUygulayici altyapısı T3 hook için.
                TutorialScriptedYoneticisi.Ornek?.AsamaSetHook();
                TutorialSenaryoMotoru.PatternBaslat("hook");
            }
            else if (v.id == TutorialAdimYoneticisi.TutorialAdimId.T3_YONTMA)
            {
                TutorialScriptedYoneticisi.Ornek?.AsamaSetYontma();
                TutorialSenaryoMotoru.PatternBaslat("yontma");
            }
            else if (v.id == TutorialAdimYoneticisi.TutorialAdimId.T3_TUTMA)
            {
                TutorialScriptedYoneticisi.Ornek?.AsamaSetTutma();
                TutorialSenaryoMotoru.PatternBaslat("tutma");
            }
            else if (v.id == TutorialAdimYoneticisi.TutorialAdimId.T3_KORUMA)
            {
                TutorialScriptedYoneticisi.Ornek?.AsamaSetKoruma();
                TutorialSenaryoMotoru.PatternBaslat("koruma");
            }
            else if (v.id == TutorialAdimYoneticisi.TutorialAdimId.T4)
            {
                // PAKET 9: T4 2-aşamalı — ilk aşama %100 (1 spin), ikinci aşama %0 (1 spin daha).
                T4AraModalGosterildi = false;
                T4IkinciAsamaBasladi = false;
                // PAKET 13-FIX: carpanUretimiAktif default false olabilir → DesenToKayit enjeksiyon bloğu
                // atlanır, çarpan hiç düşmez. T4 başlangıcında defansif olarak true set et.
                var oyT4 = Object.FindObjectOfType<OyunYoneticisi>();
                if (oyT4 != null)
                {
                    oyT4.carpanUretimiAktif = true;
                    Debug.Log($"[Tutorial T4 ADIMDEGISTI] carpanUretimiAktif={oyT4.carpanUretimiAktif}, carpanUretimOlasiligi={oyT4.carpanUretimOlasiligi:F2}, maxCarpanAdedi={oyT4.maxCarpanAdedi}");
                }
                // PAKET 14-FAZ33: ScriptedSpinUygulayici altyapısı devralır (03 referans).
                // TutorialScriptedYoneticisi varsa AsamaSet → Aktif=true, OyunYoneticisi.Spin.cs Tutorial branch'i tetiklenir.
                // Yoksa (yeni dosya yüklenmediyse) PatternBaslat eski pattern motor path'i çalıştırır (fallback).
                TutorialScriptedYoneticisi.Ornek?.AsamaSet("carpanTest_100");
                TutorialSenaryoMotoru.PatternBaslat("carpanTest_100");
            }
            else if (v.id == TutorialAdimYoneticisi.TutorialAdimId.T5)
            {
                // PAKET 9: T5 2-aşamalı — ilk aşama %100 (1 spin, bonus garanti), ikinci aşama %0 (1 spin, bonus yok).
                T5AraModalGosterildi = false;
                T5IkinciAsamaBasladi = false;
                // PAKET 14-FAZ21: T4'ten kalma carpanUretimiAktif=true T5 scatter pattern grid'ine müdahale
                // ediyordu. T5 bonus sembol pedagojisinde çarpan istenmiyor — kapat.
                var oyT5Carpan = Object.FindObjectOfType<OyunYoneticisi>();
                if (oyT5Carpan != null) oyT5Carpan.carpanUretimiAktif = false;
                // PAKET 14 (İş 4): Bonus tetik spin'inde ToplamHamKazanc=0 → PlayKayipHorn çalıyor.
                // spinSonucKayipClip'i cache + null → T5 sırasında horn çalmaz. T6_YENI_OYUNCU başında restore.
                var oyT5Sound = Object.FindObjectOfType<OyunYoneticisi>();
                if (oyT5Sound != null && !_spinSonucKayipClipCachelendi)
                {
                    _orijinalSpinSonucKayipClip = oyT5Sound.spinSonucKayipClip;
                    _spinSonucKayipClipCachelendi = true;
                    oyT5Sound.spinSonucKayipClip = null;
                    // PAKET 14-FAZ3 (İş 2): BonusUIServisi'nin AYRI bir kopyası var (Awake'te SetSpinSonucKayipClip
                    // ile set ediliyor). Sadece OyunYoneticisi.spinSonucKayipClip null'lamak yetmiyor —
                    // bonus toplam 0 path'inde BonusUIServisi._spinSonucKayipClip hâlâ canlı kalıyor.
                    BonusUIServisiKayipClipAyarla(oyT5Sound, null);
                    Debug.Log("[Tutorial T5] spinSonucKayipClip cache+null (OyunYoneticisi + BonusUIServisi).");
                }
                // PAKET 14-FAZ33: ScriptedSpinUygulayici altyapısı T5 bonusTest için.
                TutorialScriptedYoneticisi.Ornek?.AsamaSet("bonusTest_100");
                TutorialSenaryoMotoru.PatternBaslat("bonusTest_100");
            }
            else if (v.id == TutorialAdimYoneticisi.TutorialAdimId.T6_YENI_OYUNCU)
            {
                T6AraModalGosterildi = false;
                T6IkinciAsamaBasladi = false;
                // PAKET 14 (İş 4): T5'te cache'lenen kayıp ses klibini restore — T6YO ve sonrası
                // normal kayıp horn'u çalışsın.
                if (_spinSonucKayipClipCachelendi)
                {
                    var oyT6Sound = Object.FindObjectOfType<OyunYoneticisi>();
                    if (oyT6Sound != null)
                    {
                        oyT6Sound.spinSonucKayipClip = _orijinalSpinSonucKayipClip;
                        // PAKET 14-FAZ3 (İş 2): BonusUIServisi'nin de restore et.
                        BonusUIServisiKayipClipAyarla(oyT6Sound, _orijinalSpinSonucKayipClip);
                    }
                    _spinSonucKayipClipCachelendi = false;
                    Debug.Log("[Tutorial T6YO] spinSonucKayipClip restore edildi (OyunYoneticisi + BonusUIServisi).");
                }
                // PAKET 14-FAZ8: T6YO TEMİZ AKIŞ — toggle KAPALI başla, kullanıcı AÇINCA 1.aşama
                // pattern (yeniOyuncu_acik, 2 kazanç + 1 kayıp), ara modal sonrası kullanıcı KAPATINCA
                // 2.aşama pattern (yeniOyuncu_kapali, 3 kayıp). Kilit yok — kullanıcı serbestçe yönetir.
                T6IlkAsamaPatternBasladi = false;
                PanelKopru.yeniOyuncuModu = false;
                var oyT6 = Object.FindObjectOfType<OyunYoneticisi>();
                // PAKET 14-FAZ13: maxOdeme=99999 set'i Tutorial başına (PanelAcildiSonrasi) taşındı.
                // T6YO branch'inde duplicate yapmaya gerek yok — tüm Tutorial boyunca 99999 etkili.
                // PAKET 14-FAZ15: T6YO'da çarpan kapat — T4'ten kalan carpanUretimiAktif=true ise pattern
                // 2500 × çarpan x10+ = 25K+ TL kontrol dışı ödemeler oluyordu. Saf 2500/3000 enjekte için.
                if (oyT6 != null) oyT6.carpanUretimiAktif = false;
#if UNITY_WEBGL && !UNITY_EDITOR
                ToggleKapat("yeniOyuncuToggle");
#endif
                // PAKET 14-FAZ9: Pattern motoru ÖNCEDEN aktive et — kullanıcı toggle açıp HIZLICA spin atarsa
                // pre-compute akışı RNG kullanmasın, motor zaten hazır olsun. SpinKilitli toggle açana kadar
                // true (lambda yeniOyuncuModu==true gerek) → spin engellenir, pattern güvenli bekler.
                // Toggle açtığında AdminEnjeksiyonu PatternBaslat'ı idempotent restart yapar (sorun değil).
                // PAKET 14-FAZ34 İş 8: ScriptedSpinUygulayici altyapısı T6YO için (toggle açılınca AsamaSetYeniOyuncuAcik).
                TutorialScriptedYoneticisi.Ornek?.AsamaSetYeniOyuncuAcik();
                TutorialSenaryoMotoru.PatternBaslat("yeniOyuncu_acik");
                Debug.Log("[Tutorial T6YO] Giriş — toggle KAPALI, pattern yeniOyuncu_acik önceden aktif (toggle açılınca enjekte hazır).");
            }
            else if (v.id == TutorialAdimYoneticisi.TutorialAdimId.T6) // KAZANDIRMA (sira=7)
            {
                // PAKET 6C3: Default N=3 (slider değişene kadar). Slider hareket → AyarDegisti güncellenir.
                // PAKET 14-FAZ34: ScriptedSpinUygulayici altyapısı — N kazanç + (5-N) kayıp shuffle.
                TutorialScriptedYoneticisi.Ornek?.AsamaSetKazanmaSikligi(3);
                TutorialSenaryoMotoru.DinamikPatternBaslat("kazandirma", 3);
            }
            else if (v.id == TutorialAdimYoneticisi.TutorialAdimId.T8) // NEAR MISS (sira=9)
            {
                // PAKET 6C3: Default N=2. Slider değişince DinamikPatternBaslat yeniden çağrılır.
                // PAKET 14-FAZ34 İş 3: ScriptedSpinUygulayici altyapısı — N near miss + (5-N) normal shuffle.
                TutorialScriptedYoneticisi.Ornek?.AsamaSetNearMiss(2);
                TutorialSenaryoMotoru.DinamikPatternBaslat("nearMiss", 2);
            }
            else if (v.id == TutorialAdimYoneticisi.TutorialAdimId.T7) // ÖDEME ARALIĞI (sira=8, UI "T8")
            {
                // PAKET 14-FAZ2: Artık DİNAMİK. Giriş anında motor pasif — kullanıcı slider'dan
                // min/maks set edince TutorialAdminEnjeksiyonu.TryDinamikOdemePatternBaslat tetiklenir.
                // carpanUretimiAktif=false → RNG akışında 5000 × çarpan = 8000+ TL kaçağı önlenir.
                T8AraModalGosterildi = false;
                T8IkinciAsamaBasladi = false;
                TutorialSenaryoMotoru.Durdur();
                var oyT7 = Object.FindObjectOfType<OyunYoneticisi>();
                if (oyT7 != null) oyT7.carpanUretimiAktif = false;
                // Min/maks cache reset (önceki adımdan kalıntı tetik vermesin)
                TutorialAdminEnjeksiyonu.SonMinCarpan = 0f;
                TutorialAdminEnjeksiyonu.SonMaksCarpan = 0f;
                Debug.Log("[Tutorial T7 Ödeme] Giriş → motor pasif + carpan kapalı, slider eventi bekleniyor.");
            }
            else if (v.id == TutorialAdimYoneticisi.TutorialAdimId.T9) // KAÇIŞ FRENLEME (sira=10, UI "T10")
            {
                // PAKET 6D: 3 kayıp + 1 kazanç deterministik
                // PAKET 14-FAZ34 İş 4: ScriptedSpinUygulayici altyapısı — 3 kayıp + 1 başabaş kazanç + 0 doldurma.
                TutorialScriptedYoneticisi.Ornek?.AsamaSetKacis(3);
                TutorialSenaryoMotoru.PatternBaslat("kacisFrenle");
            }
            else if (v.id == TutorialAdimYoneticisi.TutorialAdimId.T10) // ÇARPAN ZORLA (sira=11, UI "T11")
            {
                // PAKET 6D: 2-aşamalı çarpan ödeme kapalı → açık demo
                // PAKET 14-FAZ34 İş 9: ScriptedSpinUygulayici altyapısı T10 Çarpan Zorla için.
                T11AraModalGosterildi = false;
                T11IkinciAsamaBasladi = false;
                TutorialScriptedYoneticisi.Ornek?.AsamaSetCarpanZorlaKapali();
                TutorialSenaryoMotoru.PatternBaslat("carpanZorla_kapaliOdeme");
            }
            else if ((int)v.id >= (int)TutorialAdimYoneticisi.TutorialAdimId.T4)
            {
                TutorialSenaryoMotoru.Durdur();
            }

            StartCoroutine(AdimAkisi(v));
        }

        private IEnumerator AdimAkisi(AdimVerisi v)
        {
            Debug.Log($"[TutorialOyunYoneticisi] AdimAkisi başladı: {v.id}");
            _ileriTiklandi = false; // PAKET 3B-fix-6: yeni adım, atla flag'i reset

            // PAKET 9: Geçiş modali — opsiyonel, Modal A'dan ÖNCE gösterilir. T4 için "Olasılık Ayarları"
            // başlığı; T3 senaryo bloğundan numerik ayarlara köprü. Diğer adımlar mesajGecis null → atlanır.
            if (TutorialModalKopru.Ornek != null && !string.IsNullOrEmpty(v.mesajGecis))
            {
                yield return TutorialModalKopru.Ornek.ModalGoster(v.mesajGecis);
                yield return null; // ardışık ModalGoster yarış güvencesi
            }

            // === Modal A (mesajBaslangic) — her zaman göster ===
            if (TutorialModalKopru.Ornek != null && !string.IsNullOrEmpty(v.mesajBaslangic))
                yield return TutorialModalKopru.Ornek.ModalGoster(v.mesajBaslangic);

            // PAKET 8-EXT: T3_HOOK Modal A sonrası Normal oyun + 4 senaryo bilgilendirme modali.
            // Önce T1 karşılaması sonrası gösteriliyordu; akış pedagojisi için T3_HOOK Modal A
            // ("5 oyun modu var") ile Modal B ("Taze Kan seç + Uygula") arasına taşındı.
            if (v.id == TutorialAdimYoneticisi.TutorialAdimId.T3_HOOK && TutorialModalKopru.Ornek != null)
            {
                yield return null; // ardışık ModalGoster yarış güvencesi
                yield return TutorialModalKopru.Ornek.ModalGoster(T1_NORMAL_INFO);
            }

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

            // PAKET 6A: T11 bonus yarım kesme — Bonus Tetikle basıldı, 3sn "Bonus Oyun Başladı"
            // görseli hissedildi, sonra reflection cleanup ile 10 free spin oynamadan kes.
            if (v.id == TutorialAdimYoneticisi.TutorialAdimId.T11)
            {
                Debug.Log("[Tutorial T11] Bonus tetiklendi, 3sn görsel hissedilecek...");
                yield return new WaitForSecondsRealtime(3f);
                T11BonusYarimKes();
                // HOTFIX: bonusEndPanel fade animasyonu (~1.65sn) + UI refresh için yeterli gecikme
                yield return new WaitForSecondsRealtime(1.5f);
            }

            // PAKET 9: T5 bonus yarım kesme artık SayaciGecikmeliArtir → TutorialT5BonusModalKontrol →
            // T5IlkAsamaSonuAkisi içinde yapılıyor (1. spin sonrası, adım bitmeden ara modal göstermek için).
            // Adım sonu (her iki aşama bitince) ek temizlik gerekmez — bonus state zaten temizlendi.

            // === Vurgu kapat (AdimGoster gizleme KALDIRILDI — modal C sırasında sağ panel görünür kalsın) ===
            // PAKET 14-FAZ22: TutorialAdimGoster.Ornek?.Gizle() kaldırıldı — eski tasarımdan kalma, modal
            // açıldığında sağ panel kaybolmasın. AdimDegisti yeni adıma geçişte zaten Gizle çağırıyor.
#if UNITY_WEBGL && !UNITY_EDITOR
            TumVurgulariKapat();
#endif

            // PAKET 14-FAZ22: Modal C öncesi GERÇEK animasyon bitiş bekle — sabit 1sn yetersizdi.
            // SayaciGecikmeliArtir ile aynı zincir: spinCalisiyor, BIG WIN pop-up, kazanç animasyon.
            var oyKapanis = Object.FindObjectOfType<OyunYoneticisi>();
            if (oyKapanis != null)
            {
                yield return BekleVeyaTimeout(() => !oyKapanis.SpinCalisiyorMu, 3f);
                yield return BekleVeyaTimeout(() =>
                    oyKapanis.winFeedbackUI == null || !oyKapanis.winFeedbackUI.GosterimAktif, 5f);
                yield return BekleVeyaTimeout(() => !TutorialKazancAnimasyon.AnimasyonAktif, 3f);
                yield return new WaitForSecondsRealtime(0.5f); // ek tampon
            }
            else
            {
                yield return new WaitForSecondsRealtime(1f);
            }

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

        // === PAKET 4-HOTFIX (Bug 1): Update polling — spin animasyonu bitince sayaç ilerlet ===

        private void Update()
        {
            if (_oyRef == null)
            {
                _oyRef = Object.FindObjectOfType<OyunYoneticisi>();
                if (_oyRef == null) return;
            }

            bool simdi = _oyRef.SpinCalisiyorMu;

            // Spin bitiş geçişi (true → false)
            if (_oncekiSpinCalisiyor && !simdi)
            {
                _oncekiSpinCalisiyor = false;
                if (_spinBekliyor)
                {
                    _spinBekliyor = false;
                    // PAKET 6A: SpinCalisiyorMu false oluyor AMA counting up / kazanç UI animation
                    // hâlâ devam ediyor olabilir → 0.8sn gecikmeli sayaç++ ile Modal C "zart" açılma fix.
                    StartCoroutine(SayaciGecikmeliArtir());
                }
            }

            // Spin başlama geçişi (false → true). PAKET 14 (İş 1+10): Mouse path
            // ButtonCevir.onClick listener'da _spinBekliyor=true set ediyor; Space tuşu
            // OyunYoneticisi.cs:1894 SpinButon'u doğrudan çağırıp listener'ı bypass ediyordu.
            // → TutorialSpinSayaci artmıyor + TutorialSenaryoMotoru.SpinTamamlandi() çağrılmıyor
            // → _spinIdx hep 0 → her spin pattern[0] kayıp grid → T9 4. spin'de kazanç gelmedi.
            // Burada hangi yoldan spin başladığından bağımsız flag set edilir (idempotent).
            if (!_oncekiSpinCalisiyor && simdi)
            {
                _oncekiSpinCalisiyor = true;
                _spinBekliyor = true;
            }
        }

        /// <summary>
        /// PAKET 4-HOTFIX: T3_TUTMA pedagojik müdahale — spin 2 sonrası TAHMIN, spin 3 sonrası DEVAM.
        /// Spin 6 (final) AdimAkisi.mesajKapanis ile zaten oynatılıyor — burada müdahale yok.
        /// </summary>
        private void TutorialT3TutmaModalKontrol()
        {
            if (AdimYoneticisi == null) return;
            if (AdimYoneticisi.mevcutAdim != TutorialAdimYoneticisi.TutorialAdimId.T3_TUTMA) return;

            int sayac = TutorialSpinSayaci - AdimYoneticisi.AdimBaslangicSpin;

            if (sayac == 2)
            {
                Debug.Log("[Tutorial] T3_TUTMA Spin 2 bitti → TAHMIN modal");
                StartCoroutine(GosterAraModal(TAHMIN_MODAL));
            }
            else if (sayac == 3)
            {
                Debug.Log("[Tutorial] T3_TUTMA Spin 3 bitti → DEVAM modal");
                StartCoroutine(GosterAraModal(DEVAM_MODAL));
            }
            // sayac == 6 final modali → AdimAkisi otomatik (mesajKapanis = T3_TUTMA_C)
        }

        private IEnumerator GosterAraModal(string metin)
        {
            // TutorialModalKopru kendi raycast bloker'i var (önceki paket) → SpinGuardOverlay'a dokunmaya gerek yok
            if (TutorialModalKopru.Ornek != null)
                yield return TutorialModalKopru.Ornek.ModalGoster(metin);
        }

        // PAKET 6A-EXT-2: Modal C anim state-driven bekleme — timeout'lu + GosterimAktif property.
        // Önceki versiyon WaitUntil(winFeedbackUI.gameObject.activeInHierarchy=false) sonsuza takılıyordu;
        // gameObject root'u Tutorial boyunca daimi aktif (child panelCanvasGroup açılır/kapanır).
        // FIX: GosterimAktif property kullan + her bekleme için 3sn timeout güvencesi.
        private IEnumerator SayaciGecikmeliArtir()
        {
            var oy = _oyRef != null ? _oyRef : Object.FindObjectOfType<OyunYoneticisi>();
            if (oy != null)
            {
                yield return BekleVeyaTimeout(() => !oy.SpinCalisiyorMu, 3f);
                yield return BekleVeyaTimeout(() =>
                    oy.winFeedbackUI == null || !oy.winFeedbackUI.GosterimAktif, 3f);
                yield return BekleVeyaTimeout(() => !TutorialKazancAnimasyon.AnimasyonAktif, 3f);
                // PAKET 14-FAZ34.1 BUG 1+2 FIX: Scripted akışta bakiye güncellemesi gecikebiliyor — wait süresi
                // arttırıldı + 2 ek frame WaitForEndOfFrame eklendi. AktifAdimSpinNetleri.Add'in net hesabı
                // BakiyePanelMevcutBakiye()'nin kazanç eklenmiş halini görmesi garanti edilir.
                yield return new WaitForSecondsRealtime(1.0f);
                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();
            }
            else
            {
                // Defansif: OyunYoneticisi bulunamadıysa eski sabit gecikmeyle ilerle
                yield return new WaitForSecondsRealtime(0.8f);
            }
            TutorialSpinSayaci++;
            // PAKET 14-FAZ16: Bakiye farkı (03 AnlaticiSeritKopru:437 referansı). Bahis ZATEN bakiye'den çıkıyor,
            // kazanç ZATEN bakiye'ye ekleniyor → tek fark = spin'in NET kazanç/kaybı (formül 1-1).
            // PAKET 14-FAZ22: BekleVeyaTimeout(bakiye != baslangic) KALDIRILDI — spin başı bahis çıkarma ANINDA
            // snapshot yakalanıyordu → kazanç eklenmeden net=-1000 yerine net=0 hesaplanıyordu (3.spin mavi).
            // Yukarıdaki spinCalisiyor + WinFeedbackUI + TutorialKazancAnimasyon zinciri + 0.5sn tampon
            // bakiye'nin finalize olmasını zaten garanti ediyor.
            if (oy != null)
            {
                // PAKET 14-FAZ34.8 BUG N FIX (YOL B): Net hesabı OyunYoneticisi.OturumKazanc farkı üzerinden.
                // SonOdeme race condition by-pass edildi (DonusAkisServisi:165 pre-compute spin sonu otomatik
                // tetikleyici SonOdeme'yi N+1 değerine yazıyordu → bar N için yanlış değer okuyordu).
                // OturumKazanc DonusAkisServisi tarafından spin animasyonu bittikten sonra artırılır,
                // pre-compute simulasyonu (ApplyNewGridAndSync grid restore) OturumKazanc'ı etkilemez.
                // Tutorial bar artık KAZANÇ display ile AYNI kaynaktan beslenir → tutarsızlık olamaz.
                long simdikiBakiyeDiag = oy.BahisPanelMevcutBakiye();
                long bakiyeFarkDiag = simdikiBakiyeDiag - _oncekiBakiye;
                long simdikiOturumKazanc = oy.OturumKazanc;
                long spinKazanci = simdikiOturumKazanc - _oncekiOturumKazanc;
                long bahis = oy.BotIcinBahis;
                int net = (int)(spinKazanci - bahis);

                Debug.Log($"[Tutorial Bar] Spin {AktifAdimSpinNetleri.Count + 1}: oturumKazancOnceki={_oncekiOturumKazanc}, oturumKazancSimdi={simdikiOturumKazanc}, spinKazanci={spinKazanci}, bahis={bahis}, net={net}, segmentRengi={(net > 0 ? "KAZANC" : net < 0 ? "KAYIP" : "NOTR")}");
                Debug.Log($"[Tutorial Bar BAKIYE DIAG] oncekiBakiye={_oncekiBakiye}, simdikiBakiye={simdikiBakiyeDiag}, gercekFark={bakiyeFarkDiag}, beklenenFark={net}, bahisDustu={(bakiyeFarkDiag == (long)net ? "EVET" : "HAYIR — bahis akışı şüpheli, BUG K")}");

                _oncekiBakiye = simdikiBakiyeDiag;
                _oncekiOturumKazanc = simdikiOturumKazanc;
                AktifAdimSpinNetleri.Add(net);
            }
            Debug.Log($"[TutorialOyunYoneticisi] Spin tamamlandı (anim state-driven), TutorialSpinSayaci={TutorialSpinSayaci}");
            TutorialSenaryoMotoru.SpinTamamlandi();
            // PAKET 14-FAZ33.1: Tutorial scripted pattern idx ilerletmesi gerçek spin tamamlandığında.
            // Pre-compute coroutine yeniden tetiklenirse aynı kayıt döner; sadece burada idx++.
            TutorialScriptedYoneticisi.Ornek?.SpinTamamlandi();
            TutorialT3TutmaModalKontrol();
            TutorialT4CarpanOlasilikModalKontrol();
            TutorialT5BonusModalKontrol();
            TutorialT6YeniOyuncuModalKontrol();
            TutorialT8OdemeModalKontrol();
            TutorialT11CarpanZorlaModalKontrol();
        }

        // PAKET 9: T5 (Bonus Sembolü) 1. spin sonrası — bonus yarım kes + ara modal göster.
        private void TutorialT5BonusModalKontrol()
        {
            if (AdimYoneticisi == null) return;
            if (AdimYoneticisi.mevcutAdim != TutorialAdimYoneticisi.TutorialAdimId.T5) return;
            if (T5AraModalGosterildi) return;

            int sayac = TutorialSpinSayaci - AdimYoneticisi.AdimBaslangicSpin;
            if (sayac == 1)
            {
                T5AraModalGosterildi = true;
                // PAKET 14-FAZ5 (İş 4): Yapılacaklar 1. madde "%0 ayarla"'ya geç (dinamik aşama).
                AdimYoneticisi.YapilacakMaddesiniGuncelle(0, "Bonus %0 ayarla");
                // PAKET 14-FAZ21: İkinci aşama için Uygula yeniden gerekli — flag reset.
                TutorialAdminEnjeksiyonu.UygulamaOnaylandi = false;
                Debug.Log("[Tutorial T5 Bonus] Aşama 1 bitti (1 spin %100), bonus yarım kes + ara modal + UygulamaOnaylandi=false");
                StartCoroutine(T5IlkAsamaSonuAkisi());
            }
        }

        private IEnumerator T5IlkAsamaSonuAkisi()
        {
            // PAKET 14-FAZ8: Sıralama değişti — ÖNCE 3sn bekle (ShowBonusStartMessage doğal 2sn+ pop-up
            // görünür, "BONUS OYUN BAŞLADI" pedagojik mesaj okunur), SONRA T11BonusYarimKes ile
            // panel'ler kapatılır. Önceki sıralama (T11BonusYarimKes önce) pop-up'ı anında kapatıyordu.
            // Update polling Faz 5 fix'i bonus state'i zaten reset etmiş, bonus oyun grid yenilenmesi
            // engellenmiş → bu 3sn zarfında 4 scatter ekranda kalır + pop-up görünür.
            yield return new WaitForSecondsRealtime(3f);
            T11BonusYarimKes();
            TutorialSenaryoMotoru.Durdur();
            yield return GosterAraModal(TutorialAdimYoneticisi.T5_ARA_MODAL);
        }

        // PAKET 9: T4 (Çarpan Olasılığı) 1. spin sonrası ara modal — kullanıcıya slider %0 daveti.
        private void TutorialT4CarpanOlasilikModalKontrol()
        {
            if (AdimYoneticisi == null) return;
            if (AdimYoneticisi.mevcutAdim != TutorialAdimYoneticisi.TutorialAdimId.T4) return;
            if (T4AraModalGosterildi) return;

            int sayac = TutorialSpinSayaci - AdimYoneticisi.AdimBaslangicSpin;
            if (sayac == 1)
            {
                T4AraModalGosterildi = true;
                // PAKET 14-FAZ5 (İş 4): Yapılacaklar 1. madde "%0 ayarla"'ya geç.
                AdimYoneticisi.YapilacakMaddesiniGuncelle(0, "Çarpan %0 ayarla");
                // PAKET 14-FAZ21: İkinci aşama için Uygula yeniden gerekli — flag reset.
                TutorialAdminEnjeksiyonu.UygulamaOnaylandi = false;
                Debug.Log("[Tutorial T4 Çarpan] Aşama 1 bitti (1 spin %100), ara modal + motor pasif + UygulamaOnaylandi=false");
                TutorialSenaryoMotoru.Durdur();
                StartCoroutine(GosterAraModal(TutorialAdimYoneticisi.T4_ARA_MODAL));
            }
        }

        // PAKET 6A-EXT-2 helper: koşul sağlanana kadar veya maxSn dolana kadar bekle (sonsuza takılma güvencesi).
        private IEnumerator BekleVeyaTimeout(System.Func<bool> kosul, float maxSn)
        {
            float t = 0f;
            while (!kosul() && t < maxSn)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            if (t >= maxSn)
                Debug.LogWarning($"[TutorialOyunYoneticisi] BekleVeyaTimeout: {maxSn}sn doldu, koşul sağlanmadı (defansif geçiş).");
        }

        // PAKET 6D: T8 (Ödeme) 3. spin sonrası ara modal — kullanıcıya MIN/MAKS ayarlama daveti
        private void TutorialT8OdemeModalKontrol()
        {
            if (AdimYoneticisi == null) return;
            if (AdimYoneticisi.mevcutAdim != TutorialAdimYoneticisi.TutorialAdimId.T7) return; // T7 enum = "T8 Ödeme"
            if (T8AraModalGosterildi) return;

            int sayac = TutorialSpinSayaci - AdimYoneticisi.AdimBaslangicSpin;
            if (sayac == 3)
            {
                T8AraModalGosterildi = true;
                Debug.Log("[Tutorial T8 Ödeme] Aşama 1 bitti (3 spin), ara modal + motor pasif");
                TutorialSenaryoMotoru.Durdur();
                StartCoroutine(GosterAraModal(TutorialAdimYoneticisi.T7_ARA_MODAL));
            }
        }

        // PAKET 6D: T11 (Çarpan Zorla) 1. spin sonrası ara modal — toggle aç daveti
        private void TutorialT11CarpanZorlaModalKontrol()
        {
            if (AdimYoneticisi == null) return;
            if (AdimYoneticisi.mevcutAdim != TutorialAdimYoneticisi.TutorialAdimId.T10) return; // T10 enum = "T11 Çarpan Zorla"
            if (T11AraModalGosterildi) return;

            int sayac = TutorialSpinSayaci - AdimYoneticisi.AdimBaslangicSpin;
            if (sayac == 1)
            {
                T11AraModalGosterildi = true;
                Debug.Log("[Tutorial T11 Çarpan Zorla] Aşama 1 bitti (1 spin), ara modal + motor pasif");
                TutorialSenaryoMotoru.Durdur();
                StartCoroutine(GosterAraModal(TutorialAdimYoneticisi.T10_ARA_MODAL));
            }
        }

        // PAKET 6C2: T6_YENI_OYUNCU 3. spin sonrası ara modal
        // PAKET 14-FAZ8: Yapılacaklar 1. madde "Yeni Oyuncu Modu'nu aç" → "Yeni Oyuncu Modu'nu kapat" mutate.
        private void TutorialT6YeniOyuncuModalKontrol()
        {
            if (AdimYoneticisi == null) return;
            if (AdimYoneticisi.mevcutAdim != TutorialAdimYoneticisi.TutorialAdimId.T6_YENI_OYUNCU) return;
            if (T6AraModalGosterildi) return;

            int sayac = TutorialSpinSayaci - AdimYoneticisi.AdimBaslangicSpin;
            if (sayac == 3)
            {
                T6AraModalGosterildi = true;
                AdimYoneticisi.YapilacakMaddesiniGuncelle(0, "Yeni Oyuncu Modu'nu kapat");
                Debug.Log("[Tutorial T6_YENI_OYUNCU] Aşama 1 bitti (3 spin kazanç), ara modal + motor pasif + yapilacak[0]='kapat'");
                TutorialSenaryoMotoru.Durdur();
                StartCoroutine(GosterAraModal(TutorialAdimYoneticisi.T6YO_ARA_MODAL));
            }
        }

        // PAKET 6A: T11 bonus oyun yarım kesme — bonus oyun başlatıldı, 3sn görsel hissedildi,
        // sonra reflection ile state cleanup → 10 free spin oynamadan T_SON'a geçilir.
        // Önceki Fix 4 (TutorialAdminEnjeksiyonu) T11 muafiyetli; burada KASITLI cleanup yapılır.
        private static void T11BonusYarimKes()
        {
            var oy = Object.FindObjectOfType<OyunYoneticisi>();
            if (oy == null) return;

            var bonusAktifField = typeof(OyunYoneticisi).GetField("bonusAktif",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var bonusHakKalanField = typeof(OyunYoneticisi).GetField("bonusHakKalan",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var spinCalisiyorField = typeof(OyunYoneticisi).GetField("spinCalisiyor",
                BindingFlags.NonPublic | BindingFlags.Instance);

            bonusAktifField?.SetValue(oy, false);
            bonusHakKalanField?.SetValue(oy, 0);
            spinCalisiyorField?.SetValue(oy, false);

            // HOTFIX (T5+T11): Bonus özet ekranı (BonusEndPanel) yarım kesme sırasında Modal C arkasında
            // kalıyor olabiliyor. Elle kapat. Plus bonusStartPanel (intro paneli) açıksa onu da kapat.
            if (oy.bonusEndPanel != null) oy.bonusEndPanel.SetActive(false);
            if (oy.bonusStartPanel != null) oy.bonusStartPanel.SetActive(false);

            Debug.Log("[Tutorial T11] Bonus yarım kesildi: bonusAktif=false, bonusHakKalan=0, spinCalisiyor=false, bonusEndPanel/StartPanel kapatıldı");
        }

        // === T_SON kapanış akışı ===

        private void TutorialBitti()
        {
            Debug.Log("[TutorialOyunYoneticisi] Tutorial bitti — kapanış akışı");
            StartCoroutine(KapanisAkisi());
        }

        private IEnumerator KapanisAkisi()
        {
            Debug.Log("[Tutorial] T_SON başladı — serbest test moduna geçilecek (sahne 04'te kalınır)");

            // 1. Vurguları kapat (panel.html iframe AÇIK KALSIN — kullanıcı serbest test'te kullanacak)
#if UNITY_WEBGL && !UNITY_EDITOR
            TumVurgulariKapat();
            // TutorialPaneliKapat KALDIRILDI — iframe açık kalır, kullanıcı senaryo değiştirip test eder.
#endif

            // 2. Tutorial UI elementlerini gizle
            TutorialAdimGoster.Ornek?.Gizle();
            if (_spinGuardOverlayGo != null) _spinGuardOverlayGo.SetActive(false);
            var hudGo = GameObject.Find("TutorialBonusHUD");
            if (hudGo != null) hudGo.SetActive(false);

            yield return new WaitForSeconds(0.3f);

            // 3. T_SON serbest test rehber modali
            if (TutorialModalKopru.Ornek != null)
            {
                yield return TutorialModalKopru.Ornek.ModalGoster(
                    "<color=#4DCC59>Tutorial tamamlandı</color>. Artık istediğiniz gibi test edebilirsiniz. " +
                    "<color=#5BA0FF>AYARLAR</color> butonuna basıp panel'i aç, farklı modlar dene, spin at, gör.\n\n" +
                    "Bağımlılıkla mücadelede yalnız değilsiniz: <color=#4DCC59>Yeşilay Danışma Hattı</color> <color=#FFD933>0850 222 0 191</color>");
            }

            // 4. Serbest test moduna geç (restore + loop modu)
            SerbestTestModunaGec();

            // LoadScene KALDIRILDI — sahne 04'te kalır
            Debug.Log("[Tutorial] Serbest test modu aktif.");
        }

        private void SerbestTestModunaGec()
        {
            var oy = Object.FindObjectOfType<OyunYoneticisi>();
            if (oy != null)
            {
                // Restore: scatter + periyot original değerlerine geri
                oy.scatterChanceNormal = _orijinalScatterChance;
                oy.AdminSetBonusOtomatikSpinPeriyodu(_orijinalBonusOtomatikPeriyot);
                // PAKET 14-FAZ11: maxOdeme restore (T6YO girişinde 99999 set edilmişti).
                if (_maxOdemeCachelendi)
                {
                    oy.AdminSetMaxOdeme(_orijinalMaxOdeme);
                    _maxOdemeCachelendi = false;
                    Debug.Log($"[Tutorial] Restore: maxOdeme={_orijinalMaxOdeme}");
                }
                Debug.Log($"[Tutorial] Restore: scatter={_orijinalScatterChance}, periyot={_orijinalBonusOtomatikPeriyot}");
            }

            // Bahis butonlarını AÇ (1000 TL sabit yerine kullanıcı serbest)
            BahisKilitle(false);

            // PAKET 7: WinFeedbackUI restore (serbest test'te BÜYÜK KAZANÇ pop-up tekrar aktif)
            if (oy != null && oy.winFeedbackUI != null)
            {
                oy.winFeedbackUI.gameObject.SetActive(true);
                Debug.Log("[Tutorial] WinFeedbackUI restore (serbest test BÜYÜK KAZANÇ aktif)");
            }

            // Pattern motoru loop moduna geç (kullanıcı senaryo değişip spin attıkça pattern başa sarar)
            TutorialSenaryoMotoru.LoopModaGec();

            // Hatırlatma sistemi devre dışı (tutorial bitti, idle uyarısı yok)
            if (HatirlatmaServisi.Ornek != null)
                HatirlatmaServisi.Ornek.gameObject.SetActive(false);
        }

        /// <summary>
        /// PAKET 4-FAZ-2: Bahis +/- butonlarını kilitle/aç. T3+ tutorial boyunca kilit, T_SON sonrası açık.
        /// GameObject.Find ile çalışır — OyunYoneticisi public field null olabilir; sahne objesi referansı garantili.
        /// </summary>
        private static void BahisKilitle(bool kilitli)
        {
            float alpha = kilitli ? 0.5f : 1f;
            bool interactable = !kilitli;
            void Uygula(string isim)
            {
                var go = GameObject.Find(isim);
                if (go == null) return;
                var btn = go.GetComponent<Button>();
                if (btn != null) btn.interactable = interactable;
                var cg = go.GetComponent<CanvasGroup>();
                if (cg == null) cg = go.AddComponent<CanvasGroup>();
                cg.alpha = alpha;
            }
            Uygula("bahisArttirButon");
            Uygula("bahisAzaltButon");
            Debug.Log($"[Tutorial] Bahis butonları kilit={kilitli}");
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
            DropdownAutoRevertEkle(); // PAKET 5: Uygula basılmadan blur olursa dropdown eski değere döner
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
