using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KumarTest
{
    /// <summary>
    /// FAZ35.137: 03 admin sahnesi (buildIndex==2) için panel mod analiz aracı.
    /// Sahne köşesi "📊 Analiz Et" butonu → 5 mod (Normal/Hook/Yontma/Tutma/Koruma) × 100 base spin animasyonsuz
    /// (OyunYoneticisi.TestSpinSimuleEt reuse) + bonus tetik durumunda TestBonusOyunSimuleEt(10) ile bonus kazancı.
    /// Sonuç CSV → Debug.Log (F12 console, kullanıcı kopyalar). jslib + panel.html + anlatici.html + asset DOKUNULMAZ.
    ///
    /// MEVCUT MOTOR/RTP/PAYTABLE/MOD DAVRANIŞI DEĞİŞMEZ — sadece TestSpinSimuleEt çağrılır ve SpinSimulasyonKaydi
    /// okunur. Bakiye değişmez (TestSpinSimuleEt simulasyon, bakiye düşürmez).
    ///
    /// STATE İZOLASYON: Analiz öncesi snapshot (aktifSenaryo, odemeMinKat/MaksKat, _odemeEgilimiYuzde, _tutmaModAktif,
    /// bakiye) → 5 mod × 100 spin → try/finally ile GARANTİ restore. _tutmaModSpinSayac restore: AdminSetTutmaModAktif
    /// çağrısı sayacı 0'a resetler (kabul edilebilir kayıp — analiz sonrası kullanıcı zaten mod değiştirir).
    /// Cache (OncedenHesaplananSpinOnbelleginiTemizle) analiz öncesi + sonrası + her mod geçişinde temizlenir.
    ///
    /// 02 anlatıcı (idx 1) + 04 Tutorial (idx 3) + 03 modal (35.135/136) + diğer 35.* fixler ETKİLENMEZ
    /// (buildIndex==2 guard NET).
    /// </summary>
    public class AnalizAraciRuntime : MonoBehaviour
    {
        private const int ADMIN_03_BUILD_INDEX = 2;
        private const int SPIN_PER_MOD = 100;
        private const int BONUS_HAK_VARSAYILAN = 10;
        private const int BONUS_TETIK_ESIK = 4; // 4 scatter ≥ bonus tetik

        public static AnalizAraciRuntime Ornek { get; private set; }

        // 0..7 meyveler, 8 scatter (ScriptedSenaryoAssetUreteci SYM_* const'larıyla aynı eşleme).
        private static readonly string[] MEYVE_ADLARI =
        {
            "armut", "cilek", "erik", "hindistan", "karpuz", "muz", "elma", "uzum"
        };

        private struct ModAyari
        {
            public string ad;
            public float minKat;
            public float maksKat;
            public int egilim;
            public bool tutmaAktif;
            // FAZ35.140 K2: Hook ve Normal'de tek çarpan (birikme engellendi). Diğer modlar default 3.
            public int maxCarpanAdedi;
        }

        // PanelKopru.SenaryoUygula switch case'leriyle BİREBİR aynı değerler (Faz 35.83 + 35.140 preset'leri).
        // FAZ35.140: Hook bantı 1.5/2.5 → 0.9/1.6 (PanelKopru.cs:461-467 ile senkron). Normal+Hook maxCarpanAdedi=1.
        private static readonly ModAyari[] MODLAR =
        {
            new ModAyari { ad = "normal", minKat = 0f,    maksKat = 0f,    egilim = 65, tutmaAktif = false, maxCarpanAdedi = 1 },
            new ModAyari { ad = "hook",   minKat = 0.9f,  maksKat = 1.6f,  egilim = 90, tutmaAktif = false, maxCarpanAdedi = 1 },
            new ModAyari { ad = "yontma", minKat = 0.3f,  maksKat = 0.7f,  egilim = 70, tutmaAktif = false, maxCarpanAdedi = 3 },
            new ModAyari { ad = "tutma",  minKat = 1.2f,  maksKat = 1.8f,  egilim = 15, tutmaAktif = true,  maxCarpanAdedi = 3 },
            new ModAyari { ad = "koruma", minKat = 0.1f,  maksKat = 0.3f,  egilim = 8,  tutmaAktif = false, maxCarpanAdedi = 3 }
        };

        // === Spawn ===

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OtomatikInit()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            var aktifSahne = SceneManager.GetActiveScene();
            if (aktifSahne.buildIndex == ADMIN_03_BUILD_INDEX)
                OnSceneLoaded(aktifSahne, LoadSceneMode.Single);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.buildIndex != ADMIN_03_BUILD_INDEX) return;
            if (Ornek != null) return;
            var go = new GameObject(nameof(AnalizAraciRuntime));
            go.AddComponent<AnalizAraciRuntime>();
        }

        private void Awake()
        {
            if (SceneManager.GetActiveScene().buildIndex != ADMIN_03_BUILD_INDEX)
            {
                gameObject.SetActive(false);
                return;
            }
            if (Ornek != null && Ornek != this) { Destroy(gameObject); return; }
            Ornek = this;
            UIYarat();
        }

        private void OnDestroy()
        {
            if (Ornek == this) Ornek = null;
        }

        // === UI ===

        private GameObject _root;
        private TMP_Dropdown _modDropdown;
        private Button _analizButon;
        private TextMeshProUGUI _butonText;
        private bool _calisiyor;

        private void UIYarat()
        {
            _root = new GameObject("AnalizAraciCanvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500; // modal/yukleme altinda kalir (modal sortingOrder=1800+)

            var scaler = _root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // FAZ35.139: TMP_Dropdown (5 mod seç) + "Analiz Et" butonu. Yan yana sağ-alt köşe.
            // Dropdown butonun SOLUNDA. TMP_DefaultControls.CreateDropdown helper minimal hierarchy kurar
            // (template + viewport + content + item prefab + scrollbar). Sprite null → procedural Image rengi
            // ile arka plan (görsel basit AMA functional). Caption/item text rengi/fontu set edilir.

            // Dropdown: sağ-alt köşede butonun SOLU, 160×40
            var dropdownGo = TMPro.TMP_DefaultControls.CreateDropdown(new TMPro.TMP_DefaultControls.Resources());
            dropdownGo.name = "AnalizModDropdown";
            dropdownGo.transform.SetParent(_root.transform, false);
            var ddRt = dropdownGo.GetComponent<RectTransform>();
            ddRt.anchorMin = ddRt.anchorMax = new Vector2(1f, 0f);
            ddRt.pivot = new Vector2(1f, 0f);
            ddRt.sizeDelta = new Vector2(160f, 40f);
            ddRt.anchoredPosition = new Vector2(-160f, 25f); // butonun sol yanı (buton -20'de, 130 genişlik → -150'de bitiyor, dropdown -160'ta başlar, ~10px boşluk)

            // Sprite null fallback: arka plan Image rengi
            var ddImg = dropdownGo.GetComponent<Image>();
            if (ddImg != null) ddImg.color = new Color(0.15f, 0.18f, 0.25f, 0.95f);

            _modDropdown = dropdownGo.GetComponent<TMP_Dropdown>();
            _modDropdown.options.Clear();
            for (int i = 0; i < MODLAR.Length; i++)
            {
                string adKucuk = MODLAR[i].ad;
                // Baş harf büyük (Normal/Hook/Yontma/Tutma/Koruma). Türkçe-güvenli ToUpperInvariant.
                string adGoster = adKucuk.Length > 0
                    ? char.ToUpperInvariant(adKucuk[0]) + adKucuk.Substring(1)
                    : adKucuk;
                _modDropdown.options.Add(new TMP_Dropdown.OptionData(adGoster));
            }
            _modDropdown.value = 0; // default: Normal
            _modDropdown.RefreshShownValue();

            // Caption text (seçili göstergesi) — beyaz, 16pt
            if (_modDropdown.captionText != null)
            {
                _modDropdown.captionText.color = Color.white;
                _modDropdown.captionText.fontSize = 16f;
                _modDropdown.captionText.alignment = TextAlignmentOptions.MidlineLeft;
            }
            // Item text (açılır listede her satır) — beyaz, 14pt
            if (_modDropdown.itemText != null)
            {
                _modDropdown.itemText.color = Color.white;
                _modDropdown.itemText.fontSize = 14f;
            }

            // Buton: sağ-alt köşe, küçültüldü (220×50 → 130×40 — dropdown'a yer açıldı)
            var btnGo = new GameObject("AnalizButon",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(_root.transform, false);
            var rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.sizeDelta = new Vector2(130f, 40f);
            rt.anchoredPosition = new Vector2(-20f, 25f);

            var img = btnGo.GetComponent<Image>();
            img.color = new Color(0.2f, 0.4f, 0.7f, 0.92f);

            _analizButon = btnGo.GetComponent<Button>();
            _analizButon.onClick.AddListener(OnAnalizTiklandi);

            var txtGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
            txtGo.transform.SetParent(btnGo.transform, false);
            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = txtRt.offsetMax = Vector2.zero;

            _butonText = txtGo.AddComponent<TextMeshProUGUI>();
            _butonText.text = "Analiz Et";
            _butonText.fontSize = 16f;
            _butonText.fontStyle = FontStyles.Bold;
            _butonText.alignment = TextAlignmentOptions.Center;
            _butonText.color = Color.white;
            _butonText.raycastTarget = false;
        }

        private void OnAnalizTiklandi()
        {
            if (_calisiyor) return;
            // FAZ35.139: Dropdown seçimi modIndex olarak AnalizCalistir'a geçirilir.
            int modIndex = _modDropdown != null ? _modDropdown.value : 0;
            StartCoroutine(AnalizCalistir(modIndex));
        }

        // === Analiz ===

        private IEnumerator AnalizCalistir(int modIndex)
        {
            // FAZ35.139: Tek mod (parametreli). 5 mod döngüsü kaldırıldı — sadece MODLAR[modIndex] çalıştırılır.
            if (modIndex < 0 || modIndex >= MODLAR.Length) modIndex = 0;
            var secilenMod = MODLAR[modIndex];

            _calisiyor = true;
            _butonText.text = "Analiz calisiyor...";
            _analizButon.interactable = false;
            if (_modDropdown != null) _modDropdown.interactable = false;

            var oy = FindObjectOfType<OyunYoneticisi>();
            if (oy == null)
            {
                Debug.LogError("[FAZ35.137 Analiz] OyunYoneticisi bulunamadi, analiz iptal.");
                ButonGeriYukle();
                yield break;
            }

            // === SNAPSHOT ===
            string snapSenaryo = PanelKopru.aktifSenaryo;
            float snapMin = oy.odemeMinKat;
            float snapMax = oy.odemeMaksKat;
            int snapEgilim = oy.GetAdminOdemeEgilimi();
            bool snapTutma = oy.IsTutmaModAktif();
            int snapBakiye = oy.BahisPanelMevcutBakiye();
            int scatterIdx = oy.TestScatterIndex;
            // FAZ35.140: maxCarpanTekSpin snapshot (Hook+Normal'de 1'e zorla, analiz sonra restore).
            int snapMaxCarpan = oy.maxCarpanTekSpinSayisi;
            Debug.Log($"[FAZ35.137 Analiz] SNAPSHOT: aktifSenaryo={snapSenaryo}, min={snapMin}, maks={snapMax}, egilim={snapEgilim}, tutma={snapTutma}, bakiye={snapBakiye}, scatterIdx={scatterIdx}, maxCarpan={snapMaxCarpan}");

            // FAZ35.138: CSV mod bazında ayrı Debug.Log bloklarına bölündü (WebGL console karakter limiti
            // ~10-15K → 500 satır tek log kırpılıyordu, sadece normal + hook başı görünüyordu). Her mod ~100 satır
            // × ~80 char = ~8K → limit altında, kesilmez. Header bir kez baştan, her mod sonu Debug.Log + Clear,
            // sonda CSV BITIS. Format/içerik DEĞİŞMEDİ — sadece çıktı bölünmesi.
            var csv = new StringBuilder(8 * 1024);

            int toplamSpinSayisi = 0;
            int toplamBonusTetik = 0;
            var baslangic = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Analiz öncesi cache temizle (kullanıcı önceki state'i cachelemiş olabilir)
                oy.AdminForceOncedenHesaplananSpinTemizle();

                // FAZ35.138: Header bir kez baştan logla (her mod blokunda tekrar yok, ayrı log).
                Debug.Log("[FAZ35.137 Analiz] CSV BASLANGIC ==============================");
                Debug.Log("[FAZ35.137 Analiz] CSV BASLIK: mod,spin_no,armut,cilek,erik,hindistan,karpuz,muz,elma,uzum,scatter,ham_kazanc,carpan,nihai_odeme,bonus_tetik,bonus_kazanc,toplam_odeme");

                // FAZ35.139: 5 mod döngüsü → tek mod (secilenMod, dropdown.value ile gelir).
                {
                    var mod = secilenMod;

                    // Mod set: aktifSenaryo + setter'lar (PanelKopru.SenaryoUygula switch'leri ile birebir).
                    // PanelKopru.aktifSenaryo public static (PanelKopru.cs:67), doğrudan set güvenli.
                    PanelKopru.aktifSenaryo = mod.ad;
                    oy.AdminSetOdemeEgilimi(mod.egilim);
                    oy.AdminSetMinCarpanDegeri(mod.minKat);
                    oy.AdminSetMaksCarpanDegeri(mod.maksKat);
                    oy.AdminSetTutmaModAktif(mod.tutmaAktif); // sayacı 0'a sıfırlar (taze sayaç)
                    // FAZ35.140 K2: PanelKopru.SenaryoUygula ile birebir (Hook+Normal=1, diğer=3).
                    oy.AdminSetMaxCarpanTekSpin(mod.maxCarpanAdedi);
                    oy.AdminForceOncedenHesaplananSpinTemizle();

                    Debug.Log($"[FAZ35.137 Analiz] Mod aktif (secili): {mod.ad} (min={mod.minKat}x, maks={mod.maksKat}x, egilim={mod.egilim}%, tutma={mod.tutmaAktif}, maxCarpan={mod.maxCarpanAdedi})");

                    for (int spinNo = 1; spinNo <= SPIN_PER_MOD; spinNo++)
                    {
                        SpinSimulasyonKaydi kayit = null;
                        try
                        {
                            kayit = oy.TestSpinSimuleEt(false);
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning($"[FAZ35.137 Analiz] TestSpinSimuleEt hata mod={mod.ad} spin={spinNo}: {e.Message}");
                        }
                        if (kayit == null) continue;

                        // Meyve dağılımı (0..7) + scatter (8) sayımı IlkGrid'den
                        int[] sayilar = new int[9];
                        int sutun = kayit.Sutun;
                        int satir = kayit.Satir;
                        if (kayit.IlkGrid != null && sutun > 0 && satir > 0)
                        {
                            for (int x = 0; x < sutun; x++)
                            {
                                for (int y = 0; y < satir; y++)
                                {
                                    int sym = kayit.IlkGrid[x, y];
                                    if (sym >= 0 && sym < 9) sayilar[sym]++;
                                }
                            }
                        }

                        int ham = kayit.ToplamHamKazanc;
                        int carpan = Mathf.Max(1, kayit.NihaiCarpanToplam);
                        int nihai = ham * carpan;

                        // Bonus tetik: scatter sayısı ≥ eşik
                        int scatterSay = (scatterIdx >= 0 && scatterIdx < 9) ? sayilar[scatterIdx] : sayilar[8];
                        bool bonusTetik = scatterSay >= BONUS_TETIK_ESIK;
                        int bonusKazanc = 0;
                        if (bonusTetik)
                        {
                            try
                            {
                                bonusKazanc = oy.TestBonusOyunSimuleEt(BONUS_HAK_VARSAYILAN);
                            }
                            catch (System.Exception e)
                            {
                                Debug.LogWarning($"[FAZ35.137 Analiz] TestBonusOyunSimuleEt hata mod={mod.ad} spin={spinNo}: {e.Message}");
                            }
                            toplamBonusTetik++;
                        }

                        int toplam = nihai + bonusKazanc;

                        csv.Append(mod.ad).Append(',')
                           .Append(spinNo).Append(',')
                           .Append(sayilar[0]).Append(',').Append(sayilar[1]).Append(',').Append(sayilar[2]).Append(',').Append(sayilar[3]).Append(',')
                           .Append(sayilar[4]).Append(',').Append(sayilar[5]).Append(',').Append(sayilar[6]).Append(',').Append(sayilar[7]).Append(',').Append(sayilar[8]).Append(',')
                           .Append(ham).Append(',').Append(carpan).Append(',').Append(nihai).Append(',')
                           .Append(bonusTetik ? 1 : 0).Append(',').Append(bonusKazanc).Append(',').Append(toplam)
                           .AppendLine();

                        toplamSpinSayisi++;
                        if (spinNo % 25 == 0) yield return null; // frame donmasin
                    }

                    // FAZ35.138: Mod sonunda CSV bloğu Debug.Log + Clear (WebGL console limit altında ~8K).
                    Debug.Log($"[FAZ35.137 Analiz] CSV MOD: {mod.ad}\n{csv}");
                    csv.Clear();
                    yield return null;
                }

                baslangic.Stop();
                Debug.Log($"[FAZ35.137 Analiz] TAMAMLANDI: {toplamSpinSayisi} spin (mod={secilenMod.ad}), {toplamBonusTetik} bonus tetik, sure={baslangic.ElapsedMilliseconds}ms.");
                Debug.Log($"[FAZ35.137 Analiz] CSV BITIS — 1 mod ({secilenMod.ad}) x 100 spin (tek blok yukarida)");
            }
            finally
            {
                // === RESTORE (try/finally GARANTİ — hata olsa bile restore) ===
                PanelKopru.aktifSenaryo = snapSenaryo;
                oy.AdminSetOdemeEgilimi(snapEgilim);
                oy.AdminSetMinCarpanDegeri(snapMin);
                oy.AdminSetMaksCarpanDegeri(snapMax);
                oy.AdminSetTutmaModAktif(snapTutma);
                // FAZ35.140: maxCarpanTekSpin restore (snapMaxCarpan).
                oy.AdminSetMaxCarpanTekSpin(snapMaxCarpan);
                oy.AnlaticiBakiyeyiSifirla(snapBakiye); // bakiye restore (TestSpinSimuleEt dokunmaz ama defansif)
                oy.AdminForceOncedenHesaplananSpinTemizle();
                Debug.Log($"[FAZ35.137 Analiz] RESTORE TAMAM: aktifSenaryo={PanelKopru.aktifSenaryo}, min={oy.odemeMinKat}, maks={oy.odemeMaksKat}, egilim={oy.GetAdminOdemeEgilimi()}, tutma={oy.IsTutmaModAktif()}, bakiye={oy.BahisPanelMevcutBakiye()}, maxCarpan={oy.maxCarpanTekSpinSayisi}");

                ButonGeriYukle();
            }
        }

        private void ButonGeriYukle()
        {
            // FAZ35.139: Buton text "Analiz Et (5 mod x 100)" → "Analiz Et" (dropdown ile mod seçimi).
            if (_butonText != null) _butonText.text = "Analiz Et";
            if (_analizButon != null) _analizButon.interactable = true;
            if (_modDropdown != null) _modDropdown.interactable = true;
            _calisiyor = false;
        }
    }
}
