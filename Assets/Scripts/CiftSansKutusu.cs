using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "2 kat şans" paneli: temel bahisin 1.5× spin maliyetini gösterir; kapalı/açık ile knob ve spin düşümü <see cref="HesaplaSpinMaliyeti"/> üzerinden bağlanır.
/// <para><b>Knob için öneri:</b> <c>ToggleZemin</c> üzerine veya altına standart <see cref="Slider"/> koy (min 0, max 1). Sol = kapalı, sağ = açık; handle otomatik kayar.</para>
/// <para>Slider yoksa <c>ToggleButton</c> <see cref="RectTransform.anchoredPosition"/> ile konumlanır (varsayılanlar yatay −72 / +72).</para>
/// <c>TxtBahis</c>: slider kapalı (0) iken seçili bahis; açık (1) iken ceil(1.5×) bahis. İsimde <c>ciftesansbedeli</c> geçen TMP: <c>BET x 1.5 (y TL)</c> (y = ceil(1.5× temel bahis)). Slider adı <c>ciftsansSlideri</c> veya <c>ciftsansSlider</c> olarak aranır.
/// Inspector boş bırakılırsa <c>TxtBahis</c>, <c>ciftsansSlideri</c> veya paneldeki tek / 0–1 aralıklı Slider, <c>ToggleButton</c>, <c>btnkapali</c>/<c>btnacik</c> veya metni <c>KAPALI</c>/<c>AÇIK</c> olan TMP üstündeki <see cref="Button"/> otomatik bulunur (gerekirse TMP’ye Button eklenir; <c>raycastTarget</c> açık olmalı).
/// </summary>
public class CiftSansKutusu : MonoBehaviour
{
    static CiftSansKutusu _ornek;

    [SerializeField] TMP_Text txtBahis;
    [SerializeField] RectTransform toggleButton;
    [SerializeField] Slider toggleSlider;
    [SerializeField] Button btnKapali;
    [SerializeField] Button btnAcik;

    [Tooltip("Slider yokken: knob sol (kapalı) anchoredPosition.")]
    [SerializeField] Vector2 konumKapali = new Vector2(-72f, 0f);
    [Tooltip("Slider yokken: knob sağ (açık) anchoredPosition.")]
    [SerializeField] Vector2 konumAcik = new Vector2(72f, 0f);

    bool _acik;
    bool _sliderDinleyicisiBagli;
    OyunYoneticisi _oyun;

    /// <summary>Sahne hiyerarşisinde <c>ciftsansSlider</c> / <c>ciftsansSlideri</c> gibi adlarla kullanılan slider isimleri.</summary>
    static bool CiftsansSliderIsmiyleEslesirMi(string oyunObjesiAdi)
    {
        if (string.IsNullOrEmpty(oyunObjesiAdi))
            return false;
        return string.Equals(oyunObjesiAdi, "ciftsansSlideri", StringComparison.OrdinalIgnoreCase)
               || string.Equals(oyunObjesiAdi, "ciftsansSlider", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Panel altında (her derinlikte) çift şans slider'ını isimle bulur.</summary>
    static Slider CiftsansSliderRecursiveBul(Transform kok)
    {
        if (kok == null)
            return null;
        var tum = kok.GetComponentsInChildren<Slider>(true);
        for (int i = 0; i < tum.Length; i++)
        {
            var s = tum[i];
            if (s != null && CiftsansSliderIsmiyleEslesirMi(s.gameObject.name))
                return s;
        }
        return null;
    }

    void Awake()
    {
        _ornek = this;
        ReferanslariOtomatikBagla();
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
            if (string.Equals(parent.GetChild(i).name, ad, StringComparison.OrdinalIgnoreCase))
                return parent.GetChild(i);
        }
        return null;
    }

    void ReferanslariOtomatikBagla()
    {
        if (txtBahis == null)
        {
            Transform tr = CocukBul(transform, "TxtBahis");
            if (tr != null)
                txtBahis = tr.GetComponent<TMP_Text>() ?? tr.GetComponentInChildren<TMP_Text>(true);
        }

        if (toggleButton == null)
        {
            Transform zemin = CocukBul(transform, "ToggleZemin");
            Transform knob = zemin != null
                ? (CocukBul(zemin, "ToggleButton") ?? CocukBul(zemin, "ToggleButon"))
                : null;
            if (knob == null)
            {
                knob = transform.Find("ToggleZemin/ToggleButton")
                    ?? transform.Find("ToggleZemin/ToggleButon");
            }
            if (knob != null)
                toggleButton = knob.GetComponent<RectTransform>();
        }

        if (btnKapali == null)
        {
            Transform b = CocukBul(transform, "btnkapali") ?? CocukBul(transform, "btnKapali");
            if (b != null)
                btnKapali = b.GetComponent<Button>();
        }

        if (btnAcik == null)
        {
            Transform b = CocukBul(transform, "btnacik") ?? CocukBul(transform, "btnAcik");
            if (b != null)
                btnAcik = b.GetComponent<Button>();
        }

        if (toggleSlider == null)
            toggleSlider = CiftsansSliderRecursiveBul(transform);

        if (toggleSlider == null)
            toggleSlider = SliderAdiylaBul(transform, "ciftsansSlideri");

        if (toggleSlider == null)
        {
            Transform zemin = CocukBul(transform, "ToggleZemin");
            if (zemin != null)
            {
                toggleSlider = CiftsansSliderRecursiveBul(zemin);
                if (toggleSlider == null)
                    toggleSlider = SliderAdiylaBul(zemin, "ciftsansSlideri");
                if (toggleSlider == null)
                    toggleSlider = zemin.GetComponent<Slider>() ?? zemin.GetComponentInChildren<Slider>(true);
            }
        }

        if (toggleSlider == null)
        {
            var tum = GetComponentsInChildren<Slider>(true);
            for (int i = 0; i < tum.Length; i++)
            {
                if (tum[i] != null && CiftsansSliderIsmiyleEslesirMi(tum[i].gameObject.name))
                {
                    toggleSlider = tum[i];
                    break;
                }
            }
        }

        if (toggleSlider == null)
            toggleSlider = PaneldekiTekVeyaSifirBirSlideriBul();

        if (btnKapali == null)
            btnKapali = ButonBulKapaliIcin(transform);
        if (btnAcik == null)
            btnAcik = ButonBulAcikIcin(transform);

        if (txtBahis == null)
        {
            var metinler = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < metinler.Length; i++)
            {
                if (metinler[i] != null
                    && metinler[i].gameObject.name.IndexOf("TxtBahis", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    txtBahis = metinler[i];
                    break;
                }
            }
        }
    }

    static Slider SliderAdiylaBul(Transform kok, string ad)
    {
        if (kok == null || string.IsNullOrEmpty(ad))
            return null;
        for (int i = 0; i < kok.childCount; i++)
        {
            var ch = kok.GetChild(i);
            if (!string.Equals(ch.name, ad, StringComparison.OrdinalIgnoreCase))
                continue;
            var s = ch.GetComponent<Slider>();
            if (s != null)
                return s;
        }
        return null;
    }

    /// <summary>Adı <c>ciftsansSlideri</c> olmayan Unity varsayılan <c>Slider</c> için: tek alt slider veya min=0 max=1.</summary>
    Slider PaneldekiTekVeyaSifirBirSlideriBul()
    {
        var tum = GetComponentsInChildren<Slider>(true);
        if (tum == null || tum.Length == 0)
            return null;
        for (int i = 0; i < tum.Length; i++)
        {
            var s = tum[i];
            if (s != null && CiftsansSliderIsmiyleEslesirMi(s.gameObject.name))
                return s;
        }
        if (tum.Length == 1)
            return tum[0];
        for (int i = 0; i < tum.Length; i++)
        {
            var s = tum[i];
            if (s == null)
                continue;
            if (Mathf.Approximately(s.minValue, 0f) && Mathf.Approximately(s.maxValue, 1f))
                return s;
        }
        return null;
    }

    static bool MetinAcikGibiMi(string icerik)
    {
        if (string.IsNullOrEmpty(icerik))
            return false;
        icerik = icerik.Trim();
        return icerik.Equals("AÇIK", StringComparison.OrdinalIgnoreCase)
               || icerik.Equals("ACIK", StringComparison.OrdinalIgnoreCase)
               || icerik.Equals("Açik", StringComparison.OrdinalIgnoreCase)
               || string.Equals(icerik, "açık", StringComparison.OrdinalIgnoreCase);
    }

    static Button TmpUzerindekiButonBulVeyaEkle(TMP_Text tmp)
    {
        if (tmp == null)
            return null;
        var p = tmp.GetComponentInParent<Button>();
        if (p != null)
            return p;
        p = tmp.GetComponent<Button>();
        if (p != null)
            return p;
        if (!tmp.raycastTarget)
            return null;
        p = tmp.gameObject.AddComponent<Button>();
        p.targetGraphic = tmp;
        p.transition = Selectable.Transition.None;
        return p;
    }

    static Button ButonBulKapaliIcin(Transform kok)
    {
        if (kok == null)
            return null;
        Transform b = CocukBul(kok, "btnkapali") ?? CocukBul(kok, "btnKapali");
        if (b != null)
        {
            var btn = b.GetComponent<Button>();
            if (btn != null)
                return btn;
        }
        var tmps = kok.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < tmps.Length; i++)
        {
            var tmp = tmps[i];
            if (tmp == null)
                continue;
            string icerik = tmp.text != null ? tmp.text.Trim() : string.Empty;
            if (icerik.Equals("KAPALI", StringComparison.OrdinalIgnoreCase))
                return TmpUzerindekiButonBulVeyaEkle(tmp);
        }
        var butonlar = kok.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < butonlar.Length; i++)
        {
            if (butonlar[i] == null)
                continue;
            string n = butonlar[i].gameObject.name;
            if (n.IndexOf("kapali", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Kapalı", StringComparison.OrdinalIgnoreCase) >= 0)
                return butonlar[i];
        }
        return null;
    }

    static Button ButonBulAcikIcin(Transform kok)
    {
        if (kok == null)
            return null;
        Transform b = CocukBul(kok, "btnacik") ?? CocukBul(kok, "btnAcik");
        if (b != null)
        {
            var btn = b.GetComponent<Button>();
            if (btn != null)
                return btn;
        }
        var tmps = kok.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < tmps.Length; i++)
        {
            var tmp = tmps[i];
            if (tmp == null)
                continue;
            string icerik = tmp.text != null ? tmp.text.Trim() : string.Empty;
            if (MetinAcikGibiMi(icerik))
                return TmpUzerindekiButonBulVeyaEkle(tmp);
        }
        var butonlar = kok.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < butonlar.Length; i++)
        {
            if (butonlar[i] == null)
                continue;
            string n = butonlar[i].gameObject.name;
            if (n.IndexOf("acik", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("açık", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Açık", StringComparison.OrdinalIgnoreCase) >= 0)
                return butonlar[i];
        }
        return null;
    }

    void SliderDinleyicisiniBaglaGerekirse()
    {
        if (toggleSlider == null)
            toggleSlider = PaneldekiTekVeyaSifirBirSlideriBul();
        if (toggleSlider == null || _sliderDinleyicisiBagli)
            return;
        toggleSlider.minValue = 0f;
        toggleSlider.maxValue = 1f;
        toggleSlider.wholeNumbers = false;
        toggleSlider.onValueChanged.RemoveListener(SliderDegeriDegisti);
        toggleSlider.onValueChanged.AddListener(SliderDegeriDegisti);
        _sliderDinleyicisiBagli = true;
    }

    bool AcikDurumunuOku()
    {
        if (toggleSlider == null)
            toggleSlider = PaneldekiTekVeyaSifirBirSlideriBul();
        if (toggleSlider != null)
            return toggleSlider.value >= 0.5f;
        return _acik;
    }

    void OnDestroy()
    {
        if (toggleSlider != null)
            toggleSlider.onValueChanged.RemoveListener(SliderDegeriDegisti);
        if (_ornek == this)
            _ornek = null;
    }

    void Start()
    {
        ReferanslariOtomatikBagla();
        _oyun = FindFirstObjectByType<OyunYoneticisi>();
        SliderDinleyicisiniBaglaGerekirse();
        ButonDinleyicileriniBagla();

        if (toggleSlider != null)
            _acik = toggleSlider.value >= 0.5f;
        else
            _acik = false;

        GorselDurumuUygula();
        BahisMaliyetMetniniYenile();
    }

    void ButonDinleyicileriniBagla()
    {
        ReferanslariOtomatikBagla();
        if (btnKapali != null)
        {
            btnKapali.onClick.RemoveListener(KapaliSecildi);
            btnKapali.onClick.AddListener(KapaliSecildi);
        }
        if (btnAcik != null)
        {
            btnAcik.onClick.RemoveListener(AcikSecildi);
            btnAcik.onClick.AddListener(AcikSecildi);
        }
    }

    void OnEnable()
    {
        if (_oyun == null)
            _oyun = FindFirstObjectByType<OyunYoneticisi>();
        ReferanslariOtomatikBagla();
        SliderDinleyicisiniBaglaGerekirse();
        ButonDinleyicileriniBagla();
        BahisMaliyetMetniniYenile();
    }

    /// <summary>
    /// Çift şans açık mı: önce <see cref="CiftSansKutusu"/> (paneldeki gerçek slider değeri; adı <c>Slider</c> olsa bile),
    /// yoksa sahne genelinde <c>ciftsansSlideri</c> adlı slider.
    /// </summary>
    public static bool CiftSansAcikMi()
    {
        var kutular = UnityEngine.Object.FindObjectsByType<CiftSansKutusu>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        CiftSansKutusu hedef = null;
        if (_ornek != null)
        {
            for (int i = 0; i < kutular.Length; i++)
            {
                if (kutular[i] == _ornek)
                {
                    hedef = _ornek;
                    break;
                }
            }
        }
        if (hedef == null && kutular.Length > 0)
            hedef = kutular[0];

        if (hedef != null)
            return hedef.AcikDurumunuOku();

        var sliders = UnityEngine.Object.FindObjectsByType<Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sliders.Length; i++)
        {
            var s = sliders[i];
            if (s != null && CiftsansSliderIsmiyleEslesirMi(s.gameObject.name))
                return s.value >= 0.5f;
        }

        return false;
    }

    /// <summary>Temel bahise göre bu spinde bakiyeden düşülecek tutar (açıkken ceil(1.5×)).</summary>
    public static int HesaplaSpinMaliyeti(int temelBahisTL)
    {
        int b = Mathf.Max(0, temelBahisTL);
        if (!CiftSansAcikMi())
            return b;
        return Mathf.CeilToInt(b * 1.5f);
    }

    /// <summary>
    /// Tüm sahnedeki isminde <c>TxtBahis</c> geçen TMP metinlerini günceller (script hiçbir objede olmasa bile).
    /// </summary>
    public static void SahnedekiTumTxtBahisMetinleriniAyarla()
    {
        var oy = UnityEngine.Object.FindFirstObjectByType<OyunYoneticisi>(FindObjectsInactive.Include);
        int temel = Mathf.Max(0, oy != null ? oy.GetMevcutBahis() : 0);
        int maliyet = HesaplaSpinMaliyeti(temel);
        string metin = OyunFormatServisi.FormatTL(maliyet);
        string bahisEtiketMetni = "Bahis: " + metin;

        var texts = UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < texts.Length; i++)
        {
            var t = texts[i];
            if (t == null) continue;
            string ad = t.gameObject.name ?? string.Empty;
            if (ad.IndexOf("TxtBahis", StringComparison.OrdinalIgnoreCase) >= 0)
                t.text = metin;
            else if (ad.IndexOf("BahisText", StringComparison.OrdinalIgnoreCase) >= 0)
                t.text = bahisEtiketMetni;
        }

        SahnedekiTumCiftEsansBedeliMetinleriniAyarla(temel);
    }

    /// <summary>
    /// İsimde <c>ciftesansbedeli</c> geçen tüm <see cref="TMP_Text"/> öğelerine
    /// <c>BET x 1.5 (y TL)</c> formatında yazar; y = temel bahisin 1.5 katı (ceil).
    /// </summary>
    public static void SahnedekiTumCiftEsansBedeliMetinleriniAyarla(int? temelBahisTL = null)
    {
        int temel;
        if (temelBahisTL.HasValue)
            temel = Mathf.Max(0, temelBahisTL.Value);
        else
        {
            var oy = UnityEngine.Object.FindFirstObjectByType<OyunYoneticisi>(FindObjectsInactive.Include);
            temel = Mathf.Max(0, oy != null ? oy.GetMevcutBahis() : 0);
        }

        int birBuçukKat = Mathf.CeilToInt(temel * 1.5f);
        string metin = $"BET x 1.5 ({OyunFormatServisi.FormatTL(birBuçukKat)})";

        var texts = UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < texts.Length; i++)
        {
            var t = texts[i];
            if (t != null && t.gameObject.name.IndexOf("ciftesansbedeli", StringComparison.OrdinalIgnoreCase) >= 0)
                t.text = metin;
        }
    }

    /// <summary><see cref="OyunYoneticisi"/> UI yenilemesinden sonra tüm panellerdeki maliyet metnini tazeler.</summary>
    public static void GuncelleTumOrnekler()
    {
        SahnedekiTumTxtBahisMetinleriniAyarla();
        var tumu = UnityEngine.Object.FindObjectsByType<CiftSansKutusu>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < tumu.Length; i++)
            tumu[i]?.BahisMaliyetMetniniYenile();
    }

    void KapaliSecildi()
    {
        SliderDegeriniAyarla(0f);
        _acik = false;
        if (toggleSlider == null && toggleButton != null)
            toggleButton.anchoredPosition = konumKapali;
        SahnedekiTumTxtBahisMetinleriniAyarla();
    }

    void AcikSecildi()
    {
        SliderDegeriniAyarla(1f);
        _acik = true;
        if (toggleSlider == null && toggleButton != null)
            toggleButton.anchoredPosition = konumAcik;
        SahnedekiTumTxtBahisMetinleriniAyarla();
    }

    void SliderDegeriDegisti(float v)
    {
        float snap = v < 0.5f ? 0f : 1f;
        if (Mathf.Abs(v - snap) > 0.001f)
            SliderDegeriniAyarla(snap);
        _acik = snap >= 0.5f;
        SahnedekiTumTxtBahisMetinleriniAyarla();
    }

    void SliderDegeriniAyarla(float v)
    {
        if (toggleSlider == null)
            toggleSlider = CiftsansSliderRecursiveBul(transform) ?? PaneldekiTekVeyaSifirBirSlideriBul();
        SliderDinleyicisiniBaglaGerekirse();
        if (toggleSlider == null)
            return;
        v = Mathf.Clamp01(v);
        toggleSlider.SetValueWithoutNotify(v);
        Canvas.ForceUpdateCanvases();
    }

    void GorselDurumuUygula()
    {
        if (toggleSlider != null)
        {
            SliderDegeriniAyarla(_acik ? 1f : 0f);
            return;
        }

        if (toggleButton != null)
            toggleButton.anchoredPosition = _acik ? konumAcik : konumKapali;
    }

    public void BahisMaliyetMetniniYenile()
    {
        ReferanslariOtomatikBagla();
        if (toggleSlider == null)
            toggleSlider = PaneldekiTekVeyaSifirBirSlideriBul();
        SliderDinleyicisiniBaglaGerekirse();
        if (toggleSlider != null)
            _acik = toggleSlider.value >= 0.5f;
        SahnedekiTumTxtBahisMetinleriniAyarla();
    }
}
