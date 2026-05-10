using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace KumarFarkindalik.Tutorial
{
    /// <summary>
    /// 04_AdminOyunScene (build idx 3) için ana koordinatör. Self-spawn pattern.
    /// PAKET 3A-DÜZELTME (5 sorun):
    ///   1) Tam save temizlik (yedek; ScriptedTutorialGecisEkrani da yapıyor)
    ///   2) AyarlarButton listener override — OyunYoneticisi.AdminAyarButonlariniBagla
    ///      (Start'ta çalışır) AyarlarButton.onClick'i Unity panel açıcı AdminAyarPaneliniAc'e
    ///      bağlıyor. Biz 1 saniye bekleyip override edip PanelKopru.AyarlarButonunaBasildi'yi
    ///      geri yüklüyoruz.
    ///   3) panel.html iframe sola alma — TutorialPanelKonum.jslib > PaneliSolaAl()
    ///   4) T1 bilgilendirici asistan modal otomatik
    ///   5) AyarlarButton hariç tüm butonlar interactable=false
    /// </summary>
    [Preserve]
    public class TutorialOyunYoneticisi : MonoBehaviour
    {
        public const int TUTORIAL_SAHNE_BUILD_INDEX = 3; // 04_AdminOyunScene

        public static TutorialOyunYoneticisi Ornek { get; private set; }

        public TutorialAdimYoneticisi AdimYoneticisi { get; private set; }
        public TutorialAdminEnjeksiyonu Enjeksiyon { get; private set; }

        private const string T1_METIN =
            "Hoş geldin. Az önce bir kumar bağımlısının yaşadıklarını gördün. " +
            "Şimdi sahne arkasını birlikte göreceğiz. Sağ-alttaki AYARLAR butonuna tıkla, " +
            "manipülasyon panelini açalım.";

        // === JSLIB extern (panel.html iframe'i sola alır) ===
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void PaneliSolaAl();
#endif

        // === Glow state ===
        private GameObject _glowGo;
        private Coroutine _glowCoroutine;

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

            AdimYoneticisi = gameObject.AddComponent<TutorialAdimYoneticisi>();
            Enjeksiyon = gameObject.AddComponent<TutorialAdminEnjeksiyonu>();

            Debug.Log("[TutorialOyunYoneticisi] Spawn + AdimYoneticisi/Enjeksiyon AddComponent.");
        }

        private void OnDestroy()
        {
            if (Ornek == this) Ornek = null;
        }

        private IEnumerator Start()
        {
            // OyunYoneticisi.Start (line 568) → AdminAyarButonlariniBagla içinde AyarlarButton'a
            // RemoveAllListeners + AddListener(AdminAyarPaneliniAc) çalışır. 1sn bekleyerek bunun
            // tamamlanmasını garantiliyoruz, sonra override ediyoruz.
            yield return null;
            yield return new WaitForSeconds(1.0f);

            // 1) Yedek save temizlik (Sorun 1) — ScriptedTutorialGecisEkrani.HADİ GÖRELİM zaten yaptı.
            YedekSaveTemizlik();

            // 2) AyarlarButton listener override (Sorun 2) — AdminAyarPaneliniAc kaldırılır,
            //    PanelKopru.AyarlarButonunaBasildi geri yüklenir + IframeKonumAyarla.
            var ayarlarBtnGo = GameObject.Find("AyarlarButton");
            Button ayarlarBtn = ayarlarBtnGo != null ? ayarlarBtnGo.GetComponent<Button>() : null;
            var pk = Object.FindObjectOfType<PanelKopru>();
            if (ayarlarBtn != null)
            {
                ayarlarBtn.onClick.RemoveAllListeners();
                ayarlarBtn.onClick.AddListener(() =>
                {
                    GlowDurdur();
                    if (pk != null) pk.AyarlarButonunaBasildi();
                    StartCoroutine(IframeKonumAyarla());
                });
                Debug.Log("[TutorialOyunYoneticisi] AyarlarButton listener override edildi (PanelKopru.AyarlarButonunaBasildi + IframeKonumAyarla).");
            }
            else
            {
                Debug.LogWarning("[TutorialOyunYoneticisi] AyarlarButton bulunamadı.");
            }

            // 3) Diğer butonları pasifleştir (Sorun 5) — AyarlarButton hariç.
            DigerButonlariPasiflestir();

            // 4) T1 bilgilendirici asistan modal otomatik (Sorun 4)
            if (TutorialModalKopru.Ornek != null)
            {
                yield return TutorialModalKopru.Ornek.ModalGoster(T1_METIN);
            }
            else
            {
                Debug.LogWarning("[TutorialOyunYoneticisi] TutorialModalKopru.Ornek null — T1 modal atlandı.");
            }

            // 5) Modal kapanınca AyarlarButton glow başlat (Sorun 5)
            if (ayarlarBtn != null)
                _glowCoroutine = StartCoroutine(AyarlarButtonGlow(ayarlarBtn));
        }

        private static void YedekSaveTemizlik()
        {
            SaveLoadServisi.Sil();
            PlayerPrefs.DeleteKey("KullaniciAdi");
            PlayerPrefs.DeleteKey("KumarRestoreModuActif");
            PlayerPrefs.Save();
            KullaniciVerileri.KullaniciAdi = "Eğitim Modu";
        }

        private static void DigerButonlariPasiflestir()
        {
            // AyarlarButton DOKUNULMAZ. Aşağıdaki butonlar interactable=false.
            string[] pasifIsimler = {
                "ButtonCevir",        // ana spin butonu (PersistentCall: OyunYoneticisi.SpinButon)
                "bahisArttirButon",
                "bahisAzaltButon",
                "BakiyeYukleButon",
                "BonusSatinAlButton",
                "OtomatikSpinButton",
                "paraCekButon",
                "ParaCekButon",
            };
            int pasifSayisi = 0;
            foreach (var ad in pasifIsimler)
            {
                var go = GameObject.Find(ad);
                if (go == null) continue;
                var btn = go.GetComponent<Button>();
                if (btn != null) { btn.interactable = false; pasifSayisi++; }
            }
            Debug.Log($"[TutorialOyunYoneticisi] DigerButonlariPasiflestir: {pasifSayisi}/{pasifIsimler.Length} buton kapatıldı.");
        }

        private IEnumerator IframeKonumAyarla()
        {
            // PaneliAc DOM'a iframe'i ekledikten sonra style override
            yield return new WaitForSeconds(0.1f);
#if UNITY_WEBGL && !UNITY_EDITOR
            PaneliSolaAl();
#endif
        }

        // === AyarlarButton glow (parlak altın halka, ping-pong alpha) ===

        private IEnumerator AyarlarButtonGlow(Button ayarlarBtn)
        {
            if (ayarlarBtn == null) yield break;
            var btnRt = ayarlarBtn.GetComponent<RectTransform>();
            var parent = ayarlarBtn.transform.parent;
            if (btnRt == null || parent == null) yield break;

            _glowGo = new GameObject("AyarlarButtonGlow",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _glowGo.transform.SetParent(parent, false);
            // Sibling index: AyarlarButton'dan ÖNCE (arkada) — buton önde, glow arkada.
            int btnIdx = ayarlarBtn.transform.GetSiblingIndex();
            _glowGo.transform.SetSiblingIndex(Mathf.Max(0, btnIdx));

            var glowRt = _glowGo.GetComponent<RectTransform>();
            glowRt.anchorMin = btnRt.anchorMin;
            glowRt.anchorMax = btnRt.anchorMax;
            glowRt.pivot = btnRt.pivot;
            glowRt.anchoredPosition = btnRt.anchoredPosition;
            glowRt.sizeDelta = btnRt.sizeDelta + new Vector2(20f, 20f); // 10px her tarafa hafif genişlik

            var img = _glowGo.GetComponent<Image>();
            img.color = new Color(0.83f, 0.69f, 0.22f, 0.3f); // altın yarı saydam
            img.raycastTarget = false; // KESİN: buton tıklamasını engellemesin

            while (_glowGo != null)
            {
                float t = Mathf.PingPong(Time.unscaledTime / 0.6f, 1f);
                float alpha = Mathf.Lerp(0.3f, 0.7f, t);
                var c = img.color;
                c.a = alpha;
                img.color = c;
                yield return null;
            }
        }

        private void GlowDurdur()
        {
            if (_glowCoroutine != null) { StopCoroutine(_glowCoroutine); _glowCoroutine = null; }
            if (_glowGo != null) { Destroy(_glowGo); _glowGo = null; }
        }
    }
}
