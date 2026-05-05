using System.Collections;
using TMPro;
using UnityEngine;

namespace Senaryo.Scripted
{
    /// <summary>
    /// Anlatıcı (03_SenaryoluOyun) sahnesinde spin sonu kazanç miktarının bakiyeye uçuşu + counting up.
    ///
    /// Eski admin sahnesindeki <see cref="OyunYoneticisi"/>.AnimateNormalSpinSonucBakiyeAkisi (UI.cs:422)
    /// portu: 1.95 sn, SmoothStep, Vector2.Lerp, scale 1.35→0.90, alpha 1.0→0.15.
    ///
    /// Conditional kaynak:
    /// - kazanc &gt;= 2 * bahis (BIG WIN+ tier; WinFeedbackUI.KazancSeviyesi):
    ///     uçuş kaynağı = ekran ortasındaki WinFeedback kazanç yazısı (UcusKaynakRect)
    /// - aksi halde:
    ///     uçuş kaynağı = sağ üst spin kazanç text'i (OyunYoneticisi.kazancText)
    /// - kazanc &lt;= 0: hiçbir animasyon tetiklenmez.
    ///
    /// Popup AÇILMAZ — sadece uçuş animasyonu. Bakiye `Mathf.Lerp(eski, yeni, eased)` ile counting up.
    /// </summary>
    public static class ScriptedKazancUcusu
    {
        private const float UCUS_SURE = 1.95f;
        private const int BIGWIN_BAHIS_CARPANI = 2; // WinFeedbackUI.KazancSeviyesiHesapla ile aynı eşik

        private static Coroutine _aktifUcus;
        private static GameObject _aktifAkisYazi;

        /// <summary>
        /// Spin sonu kazanç uçuşu coroutine'i. Çağıran (DonusAkisServisi) <c>yield return</c> ile bekleyebilir;
        /// uçuş tamamlanmadan modal hook'u tetiklenmesin (mega win paneli & uçuş üstüne modal binmesin).
        /// Bakiye <see cref="OyunYoneticisi"/> tarafından zaten güncellendi varsayılır; counting up animasyonu
        /// (yeniBakiye - kazanc) → yeniBakiye lerp'leri ile UI'yi geriye sarıp ileri akıtır.
        /// </summary>
        public static IEnumerator TetikleKazancUcusu(int kazancMiktari, int bahis, OyunYoneticisi mgr)
        {
            if (kazancMiktari <= 0 || mgr == null) yield break;
            if (mgr.bakiyeText == null) yield break;

            // Önceki uçuş hâlâ devam ediyorsa kill + cleanup (hızlı spin senaryosu)
            if (_aktifUcus != null)
            {
                mgr.StopCoroutine(_aktifUcus);
                _aktifUcus = null;
            }
            if (_aktifAkisYazi != null)
            {
                Object.Destroy(_aktifAkisYazi);
                _aktifAkisYazi = null;
            }

            RectTransform kaynakRt = KaynakRectSec(kazancMiktari, bahis, mgr);
            if (kaynakRt == null)
            {
                Debug.LogWarning("[ScriptedKazancUcusu] Kaynak RectTransform null — uçuş atlandı.");
                yield break;
            }

            bool bigWinTier = bahis > 0 && kazancMiktari >= bahis * BIGWIN_BAHIS_CARPANI;
            // UcusCoroutine'i doğrudan yield ile bekle — mgr.StartCoroutine ile fire-and-forget yerine sıralı.
            // _aktifUcus referansı önceki spin'lerden kalan bir coroutine'i kill etmek için tutulur; bu kez
            // yield-bekleme yaptığımız için _aktifUcus'a kayıt yapmaya gerek yok (caller'ın yield'ı bunu eşitler).
            yield return UcusCoroutine(kazancMiktari, kaynakRt, mgr, bigWinTier);
        }

        /// <summary>BIG WIN+ tier'da WinFeedbackUI panelinin kazanç yazısı; aksi halde sağ üst kazancText.</summary>
        private static RectTransform KaynakRectSec(int kazanc, int bahis, OyunYoneticisi mgr)
        {
            bool bigWinTier = bahis > 0 && kazanc >= bahis * BIGWIN_BAHIS_CARPANI;
            if (bigWinTier && mgr.winFeedbackUI != null && mgr.winFeedbackUI.UcusKaynakRect != null)
                return mgr.winFeedbackUI.UcusKaynakRect;
            return mgr.kazancText != null ? mgr.kazancText.rectTransform : null;
        }

        private static IEnumerator UcusCoroutine(int kazancMiktari, RectTransform kaynakRt, OyunYoneticisi mgr, bool bigWinTier)
        {
            // BIG WIN+ tier: WinFeedback paneli kapanma event'ini bekle.
            // - Otomatik akış: sayma + display süresi sonu fade-out tamamlanır, OnPanelKapandi tetiklenir.
            // - Kullanıcı tıklar: AtlaVeKapat hemen OnPanelKapandi tetikler.
            // Her iki yolda da uçuş, panel kapanma anında başlar — sabit gecikme YOK.
            // Timeout güvenliği: event hiç gelmezse (panel referansı yoksa) 5 sn + sayma süresi sonra ilerle.
            if (bigWinTier)
            {
                if (mgr.winFeedbackUI != null)
                {
                    bool panelKapandi = false;
                    System.Action handler = () => panelKapandi = true;
                    mgr.winFeedbackUI.OnPanelKapandi += handler;
                    try
                    {
                        float timeout = WinFeedbackUI.GetCountUpSuresi(kazancMiktari) + 5f;
                        float gecenPanel = 0f;
                        while (!panelKapandi && gecenPanel < timeout)
                        {
                            gecenPanel += Time.unscaledDeltaTime;
                            yield return null;
                        }
                    }
                    finally
                    {
                        mgr.winFeedbackUI.OnPanelKapandi -= handler;
                    }
                    // Doğal his için kısa buffer (panel fade-out kalıntıları görünür kalmasın).
                    yield return new WaitForSecondsRealtime(0.1f);
                }
                else
                {
                    // winFeedbackUI null fallback: eski sabit gecikme davranışı.
                    float saymaSuresi = WinFeedbackUI.GetCountUpSuresi(kazancMiktari);
                    yield return new WaitForSecondsRealtime(saymaSuresi + 0.3f);
                }
            }


            // Spin sonrası ekonomi servisinde zaten yeni bakiye var; counting up için eski değeri (yeni - kazanc) hesapla.
            int hedefBakiye = mgr.BahisPanelMevcutBakiye();
            int baslangicBakiye = hedefBakiye - kazancMiktari;
            if (baslangicBakiye < 0) baslangicBakiye = 0;

            // Hedef = bakiyeText'in canvas-relative pozisyonu
            RectTransform bakiyeRt = mgr.bakiyeText.rectTransform;
            Canvas canvas = bakiyeRt.GetComponentInParent<Canvas>();
            if (canvas == null) yield break;
            Canvas rootCanvas = canvas.rootCanvas;
            RectTransform canvasRt = rootCanvas.transform as RectTransform;
            if (canvasRt == null) yield break;

            // Akış yazı GameObject'i (runtime, panel yok — root canvas'a parent)
            var go = new GameObject("ScriptedBakiyeAkisYazi", typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(rootCanvas.transform, false);
            var akisRt = go.GetComponent<RectTransform>();
            akisRt.anchorMin = akisRt.anchorMax = akisRt.pivot = new Vector2(0.5f, 0.5f);
            akisRt.sizeDelta = new Vector2(620f, 130f);
            var akisTxt = go.AddComponent<TextMeshProUGUI>();
            akisTxt.alignment = TextAlignmentOptions.Center;
            akisTxt.fontSize = 88f;
            akisTxt.fontStyle = FontStyles.Bold;
            akisTxt.outlineWidth = 0.32f;
            akisTxt.outlineColor = new Color(0f, 0f, 0f, 0.85f);
            akisTxt.enableWordWrapping = false;
            akisTxt.color = new Color(1f, 0.85f, 0.15f, 1f);
            akisTxt.text = "+" + OyunFormatServisi.FormatTL(kazancMiktari);
            akisRt.SetAsLastSibling();
            _aktifAkisYazi = go;

            // Başlangıç ve hedef pozisyonları (canvas-relative, ScreenPoint dönüşümü)
            Camera cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
            Vector2 baslangicPos = HesaplaCanvasPos(kaynakRt, canvasRt, cam);
            Vector2 hedefPos = HesaplaCanvasPos(bakiyeRt, canvasRt, cam);
            akisRt.anchoredPosition = baslangicPos;
            akisRt.localScale = Vector3.one * 1.35f;

            // Counting up + uçuş döngüsü (eski animasyonla aynı: SmoothStep, 1.95s, Time.unscaledDeltaTime)
            float gecen = 0f;
            while (gecen < UCUS_SURE)
            {
                gecen += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(gecen / UCUS_SURE);
                float eased = Mathf.SmoothStep(0f, 1f, t);

                int anlikBakiye = Mathf.RoundToInt(Mathf.Lerp(baslangicBakiye, hedefBakiye, eased));
                mgr.bakiyeText.text = "Bakiye: " + OyunFormatServisi.FormatTL(anlikBakiye);

                akisRt.anchoredPosition = Vector2.Lerp(baslangicPos, hedefPos, eased);
                float olcek = Mathf.Lerp(1.35f, 0.90f, eased);
                akisRt.localScale = new Vector3(olcek, olcek, 1f);
                Color c = akisTxt.color;
                c.a = Mathf.Lerp(1f, 0.15f, eased);
                akisTxt.color = c;
                yield return null;
            }

            // Final değere senkronla
            mgr.bakiyeText.text = "Bakiye: " + OyunFormatServisi.FormatTL(hedefBakiye);

            if (_aktifAkisYazi != null)
            {
                Object.Destroy(_aktifAkisYazi);
                _aktifAkisYazi = null;
            }
            _aktifUcus = null;
        }

        /// <summary>RectTransform world pozisyonunu canvas-relative anchored pozisyona çevirir.</summary>
        private static Vector2 HesaplaCanvasPos(RectTransform target, RectTransform canvasRt, Camera cam)
        {
            Vector2 ekran = RectTransformUtility.WorldToScreenPoint(cam, target.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, ekran, cam, out Vector2 yerel);
            return yerel;
        }
    }
}
