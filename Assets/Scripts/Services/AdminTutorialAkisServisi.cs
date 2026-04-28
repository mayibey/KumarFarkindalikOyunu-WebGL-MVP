using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AdminTutorialAkisServisi
{
    private enum Adim
    {
        HosGeldin = 0,
        SpinAt = 1,
        ZorlukDegistir = 2,
        ScatterDegistir = 3,
        CarpanOlasilikDegistir = 4,
        CarpanMaxDegistir = 5,
        ZorlaCarpanTikla = 6,
        LogButonunaTikla = 7,
        Tamamlandi = 8
    }

    private enum SpinOgretimAsamasi
    {
        TumbleMantigiBilgi = 0,
        TumbleOlmayanSpinBekle = 1,
        TumbleOlmayanSonucBilgi = 2,
        TumbleYapanSpinBekle = 3,
        TumbleMeyveSayisiBilgi = 4,
        PatlamaOdemeBilgi = 5,
        YeniMeyveBilgi = 6,
        Tamam = 7
    }

    private Adim _aktifAdim = Adim.HosGeldin;
    private bool _aktif;
    private bool _spinCalisiyorGoruldu;
    private bool _zorlaCarpanTiklandi;
    private bool _logButonuTiklandi;

    private Func<bool> _getSpinCalisiyor;
    private Func<int> _getSonSpinKazancToplam;
    private Func<(int adet, string meyveAdi)> _getEkranMeyveOzeti;
    private Button _cevirButon;
    private Button _istatistikButon;
    private Slider _zorlukSlider;
    private Slider _scatterSlider;
    private Slider _carpanOlasilikSlider;
    private Slider _carpanMaxAdetSlider;
    private readonly List<Button> _zorlaCarpanButonlari = new List<Button>();

    private float _zorlukBaslangic;
    private float _scatterBaslangic;
    private float _carpanOlasilikBaslangic;
    private float _carpanMaxBaslangic;

    private Canvas _overlayCanvas;
    private Image _karartma;
    private RectTransform _panelRt;
    private TMP_Text _baslikText;
    private TMP_Text _aciklamaText;
    private Button _devamButon;
    private Button _deneButon;
    private Button _siradakiButon;
    private RectTransform _aktifHedefRt;
    private Graphic _aktifHedefGraphic;
    private Vector3 _aktifHedefIlkOlcek = Vector3.one;
    private Color _aktifHedefIlkRenk = Color.white;
    private bool _deneModuAktif;
    private Action _siradakiTikAksiyonu;
    private SpinOgretimAsamasi _spinOgretimAsamasi = SpinOgretimAsamasi.TumbleMantigiBilgi;

    public void SetBaglam(
        Func<bool> getSpinCalisiyor,
        Func<int> getSonSpinKazancToplam,
        Func<(int adet, string meyveAdi)> getEkranMeyveOzeti,
        Button cevirButon,
        Button istatistikButon,
        Slider zorlukSlider,
        Slider scatterSlider,
        Slider carpanOlasilikSlider,
        Slider carpanMaxAdetSlider)
    {
        _getSpinCalisiyor = getSpinCalisiyor;
        _getSonSpinKazancToplam = getSonSpinKazancToplam;
        _getEkranMeyveOzeti = getEkranMeyveOzeti;
        _cevirButon = cevirButon;
        _istatistikButon = istatistikButon;
        _zorlukSlider = zorlukSlider;
        _scatterSlider = scatterSlider;
        _carpanOlasilikSlider = carpanOlasilikSlider;
        _carpanMaxAdetSlider = carpanMaxAdetSlider;
    }

    public void Baslat()
    {
        if (_aktif) return;
        _aktif = true;

        SliderlariGerekirseOtomatikBul();

        _zorlukBaslangic = _zorlukSlider != null ? _zorlukSlider.value : 0f;
        _scatterBaslangic = _scatterSlider != null ? _scatterSlider.value : 0f;
        _carpanOlasilikBaslangic = _carpanOlasilikSlider != null ? _carpanOlasilikSlider.value : 0f;
        _carpanMaxBaslangic = _carpanMaxAdetSlider != null ? _carpanMaxAdetSlider.value : 0f;

        _zorlaCarpanButonlari.Clear();
        ButonEkle("ForceX5");
        ButonEkle("ForceX10");
        ButonEkle("ForceX50");
        ButonEkle("ForceX100");

        for (int i = 0; i < _zorlaCarpanButonlari.Count; i++)
            _zorlaCarpanButonlari[i].onClick.AddListener(OnZorlaCarpanTiklandi);
        if (_istatistikButon != null)
            _istatistikButon.onClick.AddListener(OnLogButonuTiklandi);

        OverlayOlustur();
        AdimiUygula();
    }

    public void Tick()
    {
        if (!_aktif) return;
        HedefAnimasyonunuGuncelle();
        if (_aktifAdim == Adim.SpinAt)
            SpinOgretimTick();
    }

    private void SonrakiAdim()
    {
        if (_aktifAdim == Adim.Tamamlandi) return;
        _aktifAdim = (Adim)((int)_aktifAdim + 1);
        AdimiUygula();
    }

    private void AdimiUygula()
    {
        PanelGorunurlugunuAyarla(true);
        TumSelectablelariKilitle();
        HedefVurgusunuTemizle();
        switch (_aktifAdim)
        {
            case Adim.HosGeldin:
                MetinAyarla("Admin Tutorial", "Bu eğitimde admin paneli adım adım ve güvenli şekilde kullanacaksın.");
                DevamButonuAyarla(true, "Başla", SonrakiAdim);
                DeneButonuAyarla(false);
                SiradakiButonuAyarla(false);
                break;
            case Adim.SpinAt:
                SpinAdiminiBaslat();
                break;
            case Adim.ZorlukDegistir:
                MetinAyarla("2/7 - Zorluk", "Zorluğu düşürünce tumble olasılığı artar, yükseltince azalır. Yanıp sönen sliderda dene.");
                InteractableAc(_zorlukSlider);
                HedefiAyarla(_zorlukSlider != null ? _zorlukSlider.transform as RectTransform : null);
                DevamButonuAyarla(false, "Devam", null);
                DeneButonuAyarla(true);
                SiradakiButonuAyarla(true);
                break;
            case Adim.ScatterDegistir:
                MetinAyarla("3/7 - Scatter", "Yanıp sönen Scatter sliderını değiştir.");
                InteractableAc(_scatterSlider);
                HedefiAyarla(_scatterSlider != null ? _scatterSlider.transform as RectTransform : null);
                DevamButonuAyarla(false, "Devam", null);
                DeneButonuAyarla(true);
                SiradakiButonuAyarla(true);
                break;
            case Adim.CarpanOlasilikDegistir:
                MetinAyarla("4/7 - Çarpan Olasılığı", "Yanıp sönen Çarpan Olasılığı sliderını değiştir.");
                InteractableAc(_carpanOlasilikSlider);
                HedefiAyarla(_carpanOlasilikSlider != null ? _carpanOlasilikSlider.transform as RectTransform : null);
                DevamButonuAyarla(false, "Devam", null);
                DeneButonuAyarla(true);
                SiradakiButonuAyarla(true);
                break;
            case Adim.CarpanMaxDegistir:
                MetinAyarla("5/7 - Max Çarpan", "Yanıp sönen Max Çarpan sliderını değiştir.");
                InteractableAc(_carpanMaxAdetSlider);
                HedefiAyarla(_carpanMaxAdetSlider != null ? _carpanMaxAdetSlider.transform as RectTransform : null);
                DevamButonuAyarla(false, "Devam", null);
                DeneButonuAyarla(true);
                SiradakiButonuAyarla(true);
                break;
            case Adim.ZorlaCarpanTikla:
                MetinAyarla("6/7 - Zorla Çarpan", "Yanıp sönen ForceX butonlarından birine bas.");
                for (int i = 0; i < _zorlaCarpanButonlari.Count; i++)
                    InteractableAc(_zorlaCarpanButonlari[i]);
                if (_zorlaCarpanButonlari.Count > 0)
                    HedefiAyarla(_zorlaCarpanButonlari[0].transform as RectTransform);
                DevamButonuAyarla(false, "Devam", null);
                DeneButonuAyarla(true);
                SiradakiButonuAyarla(true);
                break;
            case Adim.LogButonunaTikla:
                MetinAyarla("7/7 - Log Ekranı", "Yanıp sönen İstatistik butonuna bas.");
                InteractableAc(_istatistikButon);
                HedefiAyarla(_istatistikButon != null ? _istatistikButon.transform as RectTransform : null);
                DevamButonuAyarla(false, "Devam", null);
                DeneButonuAyarla(true);
                SiradakiButonuAyarla(true);
                break;
            case Adim.Tamamlandi:
                MetinAyarla("Tutorial Tamamlandı", "Harika. Admin panelini artık kontrollü şekilde kullanabilirsin.");
                TumSelectablelariAc();
                DevamButonuAyarla(true, "Bitir", Kapat);
                DeneButonuAyarla(false);
                SiradakiButonuAyarla(false);
                break;
        }
    }

    private void OverlayOlustur()
    {
        var go = new GameObject("AdminTutorialOverlayCanvas");
        _overlayCanvas = go.AddComponent<Canvas>();
        _overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _overlayCanvas.sortingOrder = 9000;
        go.AddComponent<GraphicRaycaster>();
        go.AddComponent<CanvasScaler>();

        var karartmaGo = new GameObject("Karartma");
        karartmaGo.transform.SetParent(go.transform, false);
        _karartma = karartmaGo.AddComponent<Image>();
        _karartma.color = new Color(0f, 0f, 0f, 0.56f);
        _karartma.raycastTarget = false;
        var karRt = karartmaGo.GetComponent<RectTransform>();
        karRt.anchorMin = Vector2.zero;
        karRt.anchorMax = Vector2.one;
        karRt.offsetMin = Vector2.zero;
        karRt.offsetMax = Vector2.zero;

        var panelGo = new GameObject("Panel");
        panelGo.transform.SetParent(go.transform, false);
        var panelImg = panelGo.AddComponent<Image>();
        panelImg.color = new Color(0.09f, 0.1f, 0.16f, 0.96f);
        panelImg.raycastTarget = false;
        _panelRt = panelGo.GetComponent<RectTransform>();
        _panelRt.anchorMin = new Vector2(0.5f, 0f);
        _panelRt.anchorMax = new Vector2(0.5f, 0f);
        _panelRt.pivot = new Vector2(0.5f, 0f);
        _panelRt.anchoredPosition = new Vector2(0f, 36f);
        _panelRt.sizeDelta = new Vector2(980f, 250f);

        _baslikText = MetinOlustur(panelGo.transform, "Baslik", 46, FontStyles.Bold);
        var baslikRt = _baslikText.rectTransform;
        baslikRt.anchorMin = new Vector2(0f, 1f);
        baslikRt.anchorMax = new Vector2(1f, 1f);
        baslikRt.pivot = new Vector2(0.5f, 1f);
        baslikRt.anchoredPosition = new Vector2(0f, -20f);
        baslikRt.sizeDelta = new Vector2(-44f, 70f);

        _aciklamaText = MetinOlustur(panelGo.transform, "Aciklama", 34, FontStyles.Normal);
        var aciklamaRt = _aciklamaText.rectTransform;
        aciklamaRt.anchorMin = new Vector2(0f, 0f);
        aciklamaRt.anchorMax = new Vector2(1f, 1f);
        aciklamaRt.offsetMin = new Vector2(24f, 76f);
        aciklamaRt.offsetMax = new Vector2(-24f, -88f);

        var devamGo = new GameObject("DevamButon");
        devamGo.transform.SetParent(panelGo.transform, false);
        var devamImg = devamGo.AddComponent<Image>();
        devamImg.color = new Color(0.96f, 0.65f, 0.2f, 1f);
        _devamButon = devamGo.AddComponent<Button>();
        var devamRt = devamGo.GetComponent<RectTransform>();
        devamRt.anchorMin = new Vector2(1f, 0f);
        devamRt.anchorMax = new Vector2(1f, 0f);
        devamRt.pivot = new Vector2(1f, 0f);
        devamRt.anchoredPosition = new Vector2(-24f, 20f);
        devamRt.sizeDelta = new Vector2(240f, 62f);

        var devamText = MetinOlustur(devamGo.transform, "DevamText", 30, FontStyles.Bold);
        devamText.alignment = TextAlignmentOptions.Center;
        devamText.raycastTarget = false;
        devamText.rectTransform.anchorMin = Vector2.zero;
        devamText.rectTransform.anchorMax = Vector2.one;
        devamText.rectTransform.offsetMin = Vector2.zero;
        devamText.rectTransform.offsetMax = Vector2.zero;
        devamText.text = "Devam";

        var deneGo = new GameObject("DeneButon");
        deneGo.transform.SetParent(panelGo.transform, false);
        var deneImg = deneGo.AddComponent<Image>();
        deneImg.color = new Color(0.18f, 0.72f, 0.42f, 1f);
        _deneButon = deneGo.AddComponent<Button>();
        var deneRt = deneGo.GetComponent<RectTransform>();
        deneRt.anchorMin = new Vector2(0f, 0f);
        deneRt.anchorMax = new Vector2(0f, 0f);
        deneRt.pivot = new Vector2(0f, 0f);
        deneRt.anchoredPosition = new Vector2(24f, 20f);
        deneRt.sizeDelta = new Vector2(300f, 62f);
        var deneText = MetinOlustur(deneGo.transform, "DeneText", 28, FontStyles.Bold);
        deneText.alignment = TextAlignmentOptions.Center;
        deneText.rectTransform.anchorMin = Vector2.zero;
        deneText.rectTransform.anchorMax = Vector2.one;
        deneText.rectTransform.offsetMin = Vector2.zero;
        deneText.rectTransform.offsetMax = Vector2.zero;
        deneText.text = "Tamam, deneyeyim";

        var siradakiGo = new GameObject("SiradakiButon");
        siradakiGo.transform.SetParent(go.transform, false);
        var siradakiImg = siradakiGo.AddComponent<Image>();
        siradakiImg.color = new Color(0.95f, 0.5f, 0.18f, 0.95f);
        _siradakiButon = siradakiGo.AddComponent<Button>();
        var siradakiRt = siradakiGo.GetComponent<RectTransform>();
        siradakiRt.anchorMin = new Vector2(1f, 0f);
        siradakiRt.anchorMax = new Vector2(1f, 0f);
        siradakiRt.pivot = new Vector2(1f, 0f);
        siradakiRt.anchoredPosition = new Vector2(-20f, 20f);
        siradakiRt.sizeDelta = new Vector2(170f, 50f);
        var siradakiText = MetinOlustur(siradakiGo.transform, "SiradakiText", 24, FontStyles.Bold);
        siradakiText.alignment = TextAlignmentOptions.Center;
        siradakiText.rectTransform.anchorMin = Vector2.zero;
        siradakiText.rectTransform.anchorMax = Vector2.one;
        siradakiText.rectTransform.offsetMin = Vector2.zero;
        siradakiText.rectTransform.offsetMax = Vector2.zero;
        siradakiText.text = "Sıradaki";
        SiradakiButonuAyarla(false);

        HedefVurgusunuTemizle();
    }

    private static TMP_Text MetinOlustur(Transform parent, string ad, int boyut, FontStyles stil)
    {
        var go = new GameObject(ad);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = boyut;
        t.fontStyle = stil;
        t.color = Color.white;
        t.enableWordWrapping = true;
        t.alignment = TextAlignmentOptions.TopLeft;
        t.raycastTarget = false;
        return t;
    }

    private void DevamButonuAyarla(bool aktif, string metin, Action tik)
    {
        if (_devamButon == null) return;
        _devamButon.onClick.RemoveAllListeners();
        _devamButon.interactable = aktif;
        var txt = _devamButon.GetComponentInChildren<TextMeshProUGUI>(true);
        if (txt != null) txt.text = metin;
        if (aktif && tik != null) _devamButon.onClick.AddListener(() => tik());
    }

    private void SpinAdiminiBaslat()
    {
        _spinOgretimAsamasi = SpinOgretimAsamasi.TumbleMantigiBilgi;
        _spinCalisiyorGoruldu = false;
        SpinAsamaMetniUygula();
    }

    private void SpinOgretimTick()
    {
        if (_getSpinCalisiyor == null) return;
        bool spinCalisiyor = _getSpinCalisiyor();
        if (!_spinCalisiyorGoruldu && spinCalisiyor)
            _spinCalisiyorGoruldu = true;
        if (!spinCalisiyor || !_spinCalisiyorGoruldu) return;

        if (_spinOgretimAsamasi != SpinOgretimAsamasi.TumbleOlmayanSpinBekle &&
            _spinOgretimAsamasi != SpinOgretimAsamasi.TumbleYapanSpinBekle) return;

        int kazanc = _getSonSpinKazancToplam != null ? Mathf.Max(0, _getSonSpinKazancToplam()) : 0;
        if (_spinOgretimAsamasi == SpinOgretimAsamasi.TumbleOlmayanSpinBekle)
        {
            if (kazanc == 0)
            {
                _spinOgretimAsamasi = SpinOgretimAsamasi.TumbleOlmayanSonucBilgi;
                _spinCalisiyorGoruldu = false;
                SpinAsamaMetniUygula();
            }
            else
            {
                _spinCalisiyorGoruldu = false;
                MetinAyarla("Spin ödedi", "Bu spin ödeme yaptı. Şimdi tekrar deneyip tumble olmayan bir spin yakalayalım.");
            }
        }
        else if (_spinOgretimAsamasi == SpinOgretimAsamasi.TumbleYapanSpinBekle)
        {
            if (kazanc > 0)
            {
                _spinOgretimAsamasi = SpinOgretimAsamasi.TumbleMeyveSayisiBilgi;
                _spinCalisiyorGoruldu = false;
                SpinAsamaMetniUygula();
            }
            else
            {
                _spinCalisiyorGoruldu = false;
                MetinAyarla("Bu spin ödeme yapmadı", "Ödeme yapan bir spin yakalayana kadar tekrar deneyelim.");
            }
        }
    }

    private void SpinAsamaMetniUygula()
    {
        InteractableAc(_cevirButon);
        HedefiAyarla(_cevirButon != null ? _cevirButon.transform as RectTransform : null);
        DevamButonuAyarla(false, "Devam", null);
        DeneButonuAyarla(true);

        switch (_spinOgretimAsamasi)
        {
            case SpinOgretimAsamasi.TumbleMantigiBilgi:
                MetinAyarla(
                    "1/7 - Tumble Mantığı",
                    "8+ aynı meyve geldiğinde tumble olur. Meyveler patlar, ödeme hesaplanır, yerlerine yukarıdan yenileri düşer. Bu süreç tumble bitene kadar sürer.");
                DeneButonuAyarla(false);
                SiradakiButonuAyarla(true, () =>
                {
                    _spinOgretimAsamasi = SpinOgretimAsamasi.TumbleOlmayanSpinBekle;
                    SpinAsamaMetniUygula();
                });
                break;
            case SpinOgretimAsamasi.TumbleOlmayanSpinBekle:
                MetinAyarla("Tumble Olmayan Spin", "Şimdi ödeme yapmayan (tumble olmayan) bir spin yap.");
                SiradakiButonuAyarla(false);
                break;
            case SpinOgretimAsamasi.TumbleOlmayanSonucBilgi:
                MetinAyarla("Tumble yok", "Bu spin ödeme yapmadı; çünkü patlayan uygun meyve kümesi oluşmadı.");
                SiradakiButonuAyarla(true, () =>
                {
                    _spinOgretimAsamasi = SpinOgretimAsamasi.TumbleYapanSpinBekle;
                    SpinAsamaMetniUygula();
                });
                break;
            case SpinOgretimAsamasi.TumbleYapanSpinBekle:
                MetinAyarla("Tumble Yapan Spin", "Şimdi ödeme yapan bir spin yapalım.");
                SiradakiButonuAyarla(false);
                break;
            case SpinOgretimAsamasi.TumbleMeyveSayisiBilgi:
                var ozet = _getEkranMeyveOzeti != null ? _getEkranMeyveOzeti() : (0, "meyve");
                MetinAyarla("Küme tespiti", $"Ekranda {ozet.Item1} adet {ozet.Item2} var. Bu yüzden kazanç hesaplanacak ve patlama başlayacak.");
                SiradakiButonuAyarla(true, () =>
                {
                    _spinOgretimAsamasi = SpinOgretimAsamasi.PatlamaOdemeBilgi;
                    SpinAsamaMetniUygula();
                });
                break;
            case SpinOgretimAsamasi.PatlamaOdemeBilgi:
                MetinAyarla("Patlama ve ödeme", "Patlayan meyveler kaybolur ve o kümenin ödemesi hesaplanır.");
                SiradakiButonuAyarla(true, () =>
                {
                    _spinOgretimAsamasi = SpinOgretimAsamasi.YeniMeyveBilgi;
                    SpinAsamaMetniUygula();
                });
                break;
            case SpinOgretimAsamasi.YeniMeyveBilgi:
                MetinAyarla("Yeni meyveler düşer", "Patlayanların yerine yenileri düşer. Yeni kümeler varsa tumble devam eder; yoksa spin biter.");
                SiradakiButonuAyarla(true, SonrakiAdim);
                break;
        }
    }

    private void DeneButonuAyarla(bool aktif)
    {
        if (_deneButon == null) return;
        _deneButon.onClick.RemoveAllListeners();
        _deneButon.gameObject.SetActive(aktif);
        _deneButon.interactable = aktif;
        if (aktif)
            _deneButon.onClick.AddListener(() =>
            {
                _deneModuAktif = true;
                PanelGorunurlugunuAyarla(false);
            });
    }

    private void SiradakiButonuAyarla(bool aktif, Action tik = null)
    {
        if (_siradakiButon == null) return;
        _siradakiTikAksiyonu = tik;
        _siradakiButon.onClick.RemoveAllListeners();
        _siradakiButon.gameObject.SetActive(aktif);
        _siradakiButon.interactable = aktif;
        if (aktif)
            _siradakiButon.onClick.AddListener(() =>
            {
                if (_siradakiTikAksiyonu != null) _siradakiTikAksiyonu();
                else SonrakiAdim();
            });
    }

    private void PanelGorunurlugunuAyarla(bool gorunur)
    {
        if (_panelRt != null) _panelRt.gameObject.SetActive(gorunur);
        if (_karartma != null) _karartma.gameObject.SetActive(gorunur);
        if (gorunur) _deneModuAktif = false;
    }

    private void SliderlariGerekirseOtomatikBul()
    {
        if (_zorlukSlider == null) _zorlukSlider = IsmeGoreSliderBul("zorluk");
        if (_scatterSlider == null) _scatterSlider = IsmeGoreSliderBul("scatter");
        if (_carpanOlasilikSlider == null) _carpanOlasilikSlider = IsmeGoreSliderBul("olasilik", "olasilik", "çarpan");
        if (_carpanMaxAdetSlider == null) _carpanMaxAdetSlider = IsmeGoreSliderBul("max", "carpan");
    }

    private static Slider IsmeGoreSliderBul(params string[] anahtarlar)
    {
        var sliderlar = UnityEngine.Object.FindObjectsByType<Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sliderlar.Length; i++)
        {
            var s = sliderlar[i];
            if (s == null || string.IsNullOrEmpty(s.gameObject.name)) continue;
            string ad = s.gameObject.name.ToLowerInvariant();
            bool tumuVar = true;
            for (int k = 0; k < anahtarlar.Length; k++)
            {
                if (!ad.Contains(anahtarlar[k].ToLowerInvariant()))
                {
                    tumuVar = false;
                    break;
                }
            }
            if (tumuVar) return s;
        }
        return sliderlar.Length > 0 ? sliderlar[0] : null;
    }

    private void HedefiAyarla(RectTransform hedef)
    {
        HedefVurgusunuTemizle();
        if (hedef == null) return;
        _aktifHedefRt = hedef;
        _aktifHedefIlkOlcek = hedef.localScale;
        _aktifHedefGraphic = hedef.GetComponent<Graphic>();
        if (_aktifHedefGraphic == null)
            _aktifHedefGraphic = hedef.GetComponentInChildren<Graphic>(true);
        if (_aktifHedefGraphic != null)
            _aktifHedefIlkRenk = _aktifHedefGraphic.color;
    }

    private void HedefVurgusunuTemizle()
    {
        if (_aktifHedefRt != null)
            _aktifHedefRt.localScale = _aktifHedefIlkOlcek;
        if (_aktifHedefGraphic != null)
            _aktifHedefGraphic.color = _aktifHedefIlkRenk;
        _aktifHedefRt = null;
        _aktifHedefGraphic = null;
    }

    private void HedefAnimasyonunuGuncelle()
    {
        if (!_deneModuAktif || _aktifHedefRt == null) return;
        float p = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5.2f);
        float scale = Mathf.Lerp(1f, 1.08f, p);
        _aktifHedefRt.localScale = _aktifHedefIlkOlcek * scale;
        if (_aktifHedefGraphic != null)
        {
            var c = _aktifHedefIlkRenk;
            c.a = Mathf.Clamp01(Mathf.Lerp(_aktifHedefIlkRenk.a * 0.65f, _aktifHedefIlkRenk.a, p));
            _aktifHedefGraphic.color = c;
        }
    }

    private void MetinAyarla(string baslik, string aciklama)
    {
        if (_baslikText != null) _baslikText.text = baslik;
        if (_aciklamaText != null) _aciklamaText.text = aciklama;
    }

    private void ButonEkle(string ad)
    {
        var b = GameObject.Find(ad)?.GetComponent<Button>();
        if (b != null) _zorlaCarpanButonlari.Add(b);
    }

    private void OnZorlaCarpanTiklandi() => _zorlaCarpanTiklandi = true;
    private void OnLogButonuTiklandi() => _logButonuTiklandi = true;

    private void TumSelectablelariKilitle()
    {
        var butonlar = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < butonlar.Length; i++)
            butonlar[i].interactable = false;
        var sliderlar = UnityEngine.Object.FindObjectsByType<Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sliderlar.Length; i++)
            sliderlar[i].interactable = false;
    }

    private void TumSelectablelariAc()
    {
        var butonlar = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < butonlar.Length; i++)
            butonlar[i].interactable = true;
        var sliderlar = UnityEngine.Object.FindObjectsByType<Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sliderlar.Length; i++)
            sliderlar[i].interactable = true;
    }

    private static void InteractableAc(Selectable s)
    {
        if (s != null) s.interactable = true;
    }

    private void Kapat()
    {
        TumSelectablelariAc();
        if (_overlayCanvas != null)
            UnityEngine.Object.Destroy(_overlayCanvas.gameObject);
        _aktif = false;
    }
}
