using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

/// <summary>
/// Son 5 normal spinin net kazanç/kayıp özetini TMP metinlerine yazar; isteğe bağlı kazanç/kayıp görselleri (Image + Sprite).
/// </summary>
public class HosgeldinizSonSpinMetinleri : MonoBehaviour
{
    const int SlotSayisi = 5;

    static readonly List<(int odenen, int bahis)> OrtakSonSpins = new List<(int, int)>(SlotSayisi);
    static TMP_Text[] OrtakBulunanMetinler;
    static Image[] OrtakBulunanDurumGorselleri;
    static Sprite PaylasilanKazancSprite;
    static Sprite PaylasilanKayipSprite;

    /// <summary>Noto Sans (₺ glifi); <see cref="Resources"/> yolu: Fonts/NotoSansLira/NotoSans-Variable.</summary>
    static TMP_FontAsset _liraIcinTmpFont;
    static bool _liraFontuYuklemeDenendi;
    static Vector2 PaylasilanDurumKutuBoyutu = new Vector2(145f, 110f);
    static bool PaylasilanDurumSabitBoyut = true;
    static bool PaylasilanDurumLayoutElement = true;

    [Tooltip("Durum Image hedef boyutu (UI birimi). Horizontal/Vertical Layout altında küçülmesin diye kod uygular.")]
    [SerializeField] Vector2 durumGorselKutuBoyutu = new Vector2(145f, 110f);
    [Tooltip("Açık: her güncellemede bu boyut yazılır + Preserve Aspect. Layout Group varsa aşağıdaki Layout Element şart.")]
    [SerializeField] bool durumGorseldeSabitBoyut = true;
    [Tooltip("Açık: Image’a Layout Element eklenir (min/preferred = boyut), layout satırı ikonu ezmez.")]
    [SerializeField] bool durumGorselLayoutElementKullan = true;

    [Tooltip("Kazanç satırında gösterilecek sprite (projedeki kazanc görseli).")]
    [SerializeField] Sprite kazancSprite;
    [Tooltip("Kayıp satırında gösterilecek sprite (projedeki kayip görseli).")]
    [SerializeField] Sprite kayipSprite;

    [Tooltip("Boş veya tamamı null ise sahnede \"son oyun 1 txt\" / \"son oyun 1\" vb. aranır.")]
    [SerializeField] TMP_Text[] hosgeldinizMetinleri;

    [Tooltip("Boş veya tamamı null ise \"son oyun N\" altında \"son oyun N durum\" Image aranır.")]
    [SerializeField] Image[] sonOyunDurumGorselleri;

    readonly List<(int odenen, int bahis)> _sonSpins = new List<(int, int)>(SlotSayisi);

    void Awake()
    {
        PaylasimSpriteSenkron();
        PaylasimDurumBoyutSenkron();
        if (DiziAtanmamisVeyaHepsiNull(hosgeldinizMetinleri))
            hosgeldinizMetinleri = MetinleriIsimleBul();
        if (DiziAtanmamisVeyaHepsiNullImages(sonOyunDurumGorselleri))
            sonOyunDurumGorselleri = DurumGorselleriniIsimleBul();
        MetinleriGuncelle(_sonSpins, hosgeldinizMetinleri, sonOyunDurumGorselleri, CozumleKazancSprite(), CozumleKayipSprite());
    }

    void OnEnable()
    {
        MetinleriGuncelle(_sonSpins, hosgeldinizMetinleri, sonOyunDurumGorselleri, CozumleKazancSprite(), CozumleKayipSprite());
    }

    void Start()
    {
        MetinleriGuncelle(_sonSpins, hosgeldinizMetinleri, sonOyunDurumGorselleri, CozumleKazancSprite(), CozumleKayipSprite());
    }

    void PaylasimSpriteSenkron()
    {
        if (kazancSprite != null)
            PaylasilanKazancSprite = kazancSprite;
        if (kayipSprite != null)
            PaylasilanKayipSprite = kayipSprite;
    }

    void PaylasimDurumBoyutSenkron()
    {
        PaylasilanDurumKutuBoyutu = durumGorselKutuBoyutu;
        PaylasilanDurumSabitBoyut = durumGorseldeSabitBoyut;
        PaylasilanDurumLayoutElement = durumGorselLayoutElementKullan;
    }

    Sprite CozumleKazancSprite() => kazancSprite != null ? kazancSprite : PaylasilanKazancSprite;
    Sprite CozumleKayipSprite() => kayipSprite != null ? kayipSprite : PaylasilanKayipSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void SahneYuklendiktenSonraSlotlariBosalt()
    {
        OrtakSonSpins.Clear();
        OrtakBulunanMetinler = MetinleriIsimleBul();
        OrtakBulunanDurumGorselleri = DurumGorselleriniIsimleBul();
        MetinleriGuncelle(OrtakSonSpins, OrtakBulunanMetinler, OrtakBulunanDurumGorselleri, PaylasilanKazancSprite, PaylasilanKayipSprite);

        var ornekler = UnityEngine.Object.FindObjectsByType<HosgeldinizSonSpinMetinleri>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < ornekler.Length; i++)
            ornekler[i]?.OrnekVerisiniSifirlaVeYaz();
    }

    void OrnekVerisiniSifirlaVeYaz()
    {
        PaylasimSpriteSenkron();
        PaylasimDurumBoyutSenkron();
        _sonSpins.Clear();
        if (DiziAtanmamisVeyaHepsiNull(hosgeldinizMetinleri))
            hosgeldinizMetinleri = MetinleriIsimleBul();
        if (DiziAtanmamisVeyaHepsiNullImages(sonOyunDurumGorselleri))
            sonOyunDurumGorselleri = DurumGorselleriniIsimleBul();
        MetinleriGuncelle(_sonSpins, hosgeldinizMetinleri, sonOyunDurumGorselleri, CozumleKazancSprite(), CozumleKayipSprite());
    }

    /// <summary>Bonus oturumu bittiğinde listenin ilk satırına ek ödeme yansıtılır (normal spin + bonus).</summary>
    public static void SonSpineBonusEkleTumunu(int ekOdenenTL)
    {
        if (ekOdenenTL <= 0) return;
        if (OrtakSonSpins.Count > 0)
        {
            var bas = OrtakSonSpins[0];
            OrtakSonSpins[0] = (bas.odenen + ekOdenenTL, bas.bahis);
        }

        var hepsi = UnityEngine.Object.FindObjectsByType<HosgeldinizSonSpinMetinleri>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (hepsi != null && hepsi.Length > 0)
        {
            for (int i = 0; i < hepsi.Length; i++)
                hepsi[i]?.InstanceSonSpineBonusEkle(ekOdenenTL);
        }
        else
        {
            if (OrtakBulunanMetinler == null || DiziAtanmamisVeyaHepsiNull(OrtakBulunanMetinler))
                OrtakBulunanMetinler = MetinleriIsimleBul();
            if (OrtakBulunanDurumGorselleri == null || DiziAtanmamisVeyaHepsiNullImages(OrtakBulunanDurumGorselleri))
                OrtakBulunanDurumGorselleri = DurumGorselleriniIsimleBul();
            MetinleriGuncelle(OrtakSonSpins, OrtakBulunanMetinler, OrtakBulunanDurumGorselleri, PaylasilanKazancSprite, PaylasilanKayipSprite);
        }
    }

    void InstanceSonSpineBonusEkle(int ekOdenenTL)
    {
        if (ekOdenenTL <= 0 || _sonSpins.Count == 0) return;
        PaylasimSpriteSenkron();
        PaylasimDurumBoyutSenkron();
        var bas = _sonSpins[0];
        _sonSpins[0] = (bas.odenen + ekOdenenTL, bas.bahis);
        if (DiziAtanmamisVeyaHepsiNull(hosgeldinizMetinleri))
            hosgeldinizMetinleri = MetinleriIsimleBul();
        if (DiziAtanmamisVeyaHepsiNullImages(sonOyunDurumGorselleri))
            sonOyunDurumGorselleri = DurumGorselleriniIsimleBul();
        MetinleriGuncelle(_sonSpins, hosgeldinizMetinleri, sonOyunDurumGorselleri, CozumleKazancSprite(), CozumleKayipSprite());
    }

    /// <summary>Sahnedeki <see cref="HosgeldinizSonSpinMetinleri"/> yokken spin sonunda çağrılır.</summary>
    public static void BilesenOlmadanGuncelle(int odenenTL, int bahisTL)
    {
        OrtakSonSpins.Insert(0, (odenenTL, bahisTL));
        while (OrtakSonSpins.Count > SlotSayisi)
            OrtakSonSpins.RemoveAt(OrtakSonSpins.Count - 1);
        if (OrtakBulunanMetinler == null || DiziAtanmamisVeyaHepsiNull(OrtakBulunanMetinler))
            OrtakBulunanMetinler = MetinleriIsimleBul();
        if (OrtakBulunanDurumGorselleri == null || DiziAtanmamisVeyaHepsiNullImages(OrtakBulunanDurumGorselleri))
            OrtakBulunanDurumGorselleri = DurumGorselleriniIsimleBul();
        MetinleriGuncelle(OrtakSonSpins, OrtakBulunanMetinler, OrtakBulunanDurumGorselleri, PaylasilanKazancSprite, PaylasilanKayipSprite);
    }

    static bool DiziAtanmamisVeyaHepsiNull(TMP_Text[] dizi)
    {
        if (dizi == null || dizi.Length == 0)
            return true;
        for (int i = 0; i < dizi.Length; i++)
            if (dizi[i] != null)
                return false;
        return true;
    }

    static bool DiziAtanmamisVeyaHepsiNullImages(Image[] dizi)
    {
        if (dizi == null || dizi.Length == 0)
            return true;
        for (int i = 0; i < dizi.Length; i++)
            if (dizi[i] != null)
                return false;
        return true;
    }

    static string[] IsimAdaylari(int indeks)
    {
        return new[]
        {
            $"son oyun {indeks} txt",
            $"son oyun {indeks}",
            $"Son oyun {indeks} txt",
            $"Son oyun {indeks}",
            $"TxtHosgeldiniz ({indeks})",
        };
    }

    static Transform CocukBul(Transform parent, string ad)
    {
        if (parent == null || string.IsNullOrEmpty(ad))
            return null;
        var t = parent.Find(ad);
        if (t != null)
            return t;
        for (int i = 0; i < parent.childCount; i++)
        {
            if (string.Equals(parent.GetChild(i).name, ad, System.StringComparison.OrdinalIgnoreCase))
                return parent.GetChild(i);
        }
        return null;
    }

    /// <summary><c>son oyun N</c> altında <c>son oyun N durum</c> adlı Image (veya adında durum geçen Image, txt hariç).</summary>
    static Image[] DurumGorselleriniIsimleBul()
    {
        var bulunan = new List<Image>(SlotSayisi);
        for (int i = 1; i <= SlotSayisi; i++)
        {
            Image img = null;
            GameObject parentGo = GameObject.Find($"son oyun {i}");
            if (parentGo == null)
                parentGo = GameObject.Find($"Son oyun {i}");
            if (parentGo != null)
            {
                var tr = parentGo.transform;
                var durumTr = CocukBul(tr, $"son oyun {i} durumimg")
                    ?? CocukBul(tr, $"Son oyun {i} durumimg")
                    ?? CocukBul(tr, $"son oyun {i} durum")
                    ?? CocukBul(tr, $"Son oyun {i} durum");
                if (durumTr != null)
                    img = durumTr.GetComponent<Image>();
                if (img == null)
                {
                    for (int c = 0; c < tr.childCount; c++)
                    {
                        var ch = tr.GetChild(c);
                        string ln = (ch.name ?? "").ToLowerInvariant();
                        if (!ln.Contains("durum"))
                            continue;
                        if (ln.Contains("txt") || ln.Contains("text"))
                            continue;
                        img = ch.GetComponent<Image>();
                        if (img != null)
                            break;
                    }
                }
            }
            bulunan.Add(img);
        }
        return bulunan.ToArray();
    }

    static TMP_Text[] MetinleriIsimleBul()
    {
        var bulunan = new List<TMP_Text>(SlotSayisi);
        for (int i = 1; i <= SlotSayisi; i++)
        {
            TMP_Text tmp = null;
            foreach (string ad in IsimAdaylari(i))
            {
                var go = GameObject.Find(ad);
                if (go == null)
                    continue;
                tmp = go.GetComponentInChildren<TMP_Text>(true);
                if (tmp != null)
                    break;
                Debug.LogWarning($"[HosgeldinizSonSpinMetinleri] '{ad}' bulundu ancak TMP_Text (kendisinde veya çocukta) yok.");
            }
            if (tmp == null)
                Debug.LogWarning($"[HosgeldinizSonSpinMetinleri] Slot {i} için metin bulunamadı (denenen adlar: son oyun N txt / son oyun N / TxtHosgeldiniz (N)). Inspector'dan TMP atayın.");
            else
                bulunan.Add(tmp);
        }
        return bulunan.ToArray();
    }

    /// <summary>Spin tamamlandığında <see cref="SonSpinListeOlaylari"/> tarafından çağrılır.</summary>
    public void SpinSonucuIsle(int odenenTL, int bahisTL)
    {
        PaylasimSpriteSenkron();
        PaylasimDurumBoyutSenkron();
        _sonSpins.Insert(0, (odenenTL, bahisTL));
        while (_sonSpins.Count > SlotSayisi)
            _sonSpins.RemoveAt(_sonSpins.Count - 1);
        if (DiziAtanmamisVeyaHepsiNull(hosgeldinizMetinleri))
            hosgeldinizMetinleri = MetinleriIsimleBul();
        if (DiziAtanmamisVeyaHepsiNullImages(sonOyunDurumGorselleri))
            sonOyunDurumGorselleri = DurumGorselleriniIsimleBul();
        MetinleriGuncelle(_sonSpins, hosgeldinizMetinleri, sonOyunDurumGorselleri, CozumleKazancSprite(), CozumleKayipSprite());
    }

    static void MetinleriGuncelle(
        List<(int odenen, int bahis)> kaynak,
        TMP_Text[] metinler,
        Image[] durumGorselleri,
        Sprite kazancSp,
        Sprite kayipSp)
    {
        if (metinler == null)
            return;
        int veriSayisi = kaynak != null ? kaynak.Count : 0;
        int slot = Mathf.Min(SlotSayisi, metinler.Length);
        for (int i = 0; i < slot; i++)
        {
            var tmp = metinler[i];
            if (tmp != null)
            {
                tmp.color = new Color32(255, 214, 64, 255);
                if (i >= veriSayisi)
                {
                    tmp.text = string.Empty;
                    tmp.richText = true;
                }
                else
                {
                    LiraFontunuMetneUygula(tmp);
                    var (odenen, bahis) = kaynak[i];
                    tmp.text = SatirMetniOlustur(odenen, bahis);
                    tmp.richText = true;
                }
            }

            Image durumImg = null;
            if (durumGorselleri != null && i < durumGorselleri.Length)
                durumImg = durumGorselleri[i];
            DurumGorseliniUygula(durumImg, i < veriSayisi, kaynak, i, kazancSp, kayipSp);
        }
    }

    static void DurumGorseliniUygula(
        Image img,
        bool veriVar,
        List<(int odenen, int bahis)> kaynak,
        int indeks,
        Sprite kazancSp,
        Sprite kayipSp)
    {
        if (img == null)
            return;
        if (!veriVar || kaynak == null || indeks < 0 || indeks >= kaynak.Count ||
            kazancSp == null || kayipSp == null)
        {
            img.enabled = false;
            DurumGorselLayoutBosalt(img);
            return;
        }
        var (odenen, bahis) = kaynak[indeks];
        img.enabled = true;
        img.sprite = odenen > bahis ? kazancSp : kayipSp;
        img.preserveAspect = true;
        if (PaylasilanDurumSabitBoyut &&
            PaylasilanDurumKutuBoyutu.x >= 4f &&
            PaylasilanDurumKutuBoyutu.y >= 4f)
        {
            var rt = img.rectTransform;
            rt.sizeDelta = PaylasilanDurumKutuBoyutu;
            if (PaylasilanDurumLayoutElement)
            {
                var le = img.GetComponent<LayoutElement>();
                if (le == null)
                    le = img.gameObject.AddComponent<LayoutElement>();
                le.enabled = true;
                float w = PaylasilanDurumKutuBoyutu.x;
                float h = PaylasilanDurumKutuBoyutu.y;
                le.minWidth = w;
                le.minHeight = h;
                le.preferredWidth = w;
                le.preferredHeight = h;
                le.flexibleWidth = 0f;
                le.flexibleHeight = 0f;
                le.ignoreLayout = false;
            }
            var ust = rt.parent as RectTransform;
            if (ust != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(ust);
        }
    }

    static void DurumGorselLayoutBosalt(Image img)
    {
        if (img == null)
            return;
        var le = img.GetComponent<LayoutElement>();
        if (le != null)
            le.enabled = false;
    }

    static TMP_FontAsset LiraFontunuYukle()
    {
        if (_liraFontuYuklemeDenendi)
            return _liraIcinTmpFont;
        _liraFontuYuklemeDenendi = true;
        var kaynakFont = Resources.Load<Font>("Fonts/NotoSansLira/NotoSans-Variable");
        if (kaynakFont == null)
        {
            Debug.LogWarning(
                "[HosgeldinizSonSpinMetinleri] ₺ için Noto Sans bulunamadı (Resources/Fonts/NotoSansLira/NotoSans-Variable). Tutar ' TL' ile gösterilir.");
            return null;
        }
        _liraIcinTmpFont = TMP_FontAsset.CreateFontAsset(
            kaynakFont,
            90,
            9,
            GlyphRenderMode.SDFAA,
            1024,
            1024,
            AtlasPopulationMode.Dynamic);
        if (_liraIcinTmpFont != null)
            _liraIcinTmpFont.name = "NotoSans-Lira-Dynamic";
        return _liraIcinTmpFont;
    }

    static void LiraFontunuMetneUygula(TMP_Text tmp)
    {
        var fa = LiraFontunuYukle();
        if (fa != null)
            tmp.font = fa;
    }

    static string TutarLiraVeyaTl(int value)
    {
        if (LiraFontunuYukle() != null)
            return value.ToString("N0", new CultureInfo("tr-TR")) + " \u20BA";
        return OyunFormatServisi.FormatTL(value);
    }

    static string SatirMetniOlustur(int odenen, int bahis)
    {
        if (odenen > bahis)
        {
            int net = odenen - bahis;
            return $"Kazanç: {TutarLiraVeyaTl(net)}";
        }
        int kayip = bahis - odenen;
        return $"Kayıp: {TutarLiraVeyaTl(kayip)}";
    }
}
