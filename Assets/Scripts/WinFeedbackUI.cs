using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class WinFeedbackUI : MonoBehaviour
{
    public enum KazancSeviyesi { Normal, BigWin, MegaWin, EpicWin }

    [Header("Referanslar")]
    [SerializeField] private CanvasGroup   panelCanvasGroup;
    [SerializeField] private RectTransform olcekKoku;
    [SerializeField] private TMP_Text      baslikText;
    [SerializeField] private TMP_Text      kazancText;

    /// <summary>BIG WIN+ tier'da scripted uçuş animasyonunun kaynak alacağı RectTransform (kazanç miktarı yazısı).</summary>
    public RectTransform UcusKaynakRect => kazancText != null ? kazancText.rectTransform : olcekKoku;

    /// <summary>
    /// Panel kapandığında tetiklenir (otomatik akış sonu fade-out tamamlandığında VEYA kullanıcı tıklayıp atladığında).
    /// ScriptedKazancUcusu bu event'i dinleyerek uçuşu panelin kapanma anına senkronize eder — sabit beklemeden bağımsız.
    /// </summary>
    public event System.Action OnPanelKapandi;

    [Header("Karartma Overlay (boş bırakılırsa otomatik oluşur)")]
    [SerializeField] private Image karartmaOverlay;

    [Header("Animasyon Süreleri")]
    [SerializeField] private float girisAnimasyonSuresi = 0.30f;
    [SerializeField] private float cikisAnimasyonSuresi = 0.25f;
    [SerializeField] private float saymaFazlaSuresi     = 0.5f;

    [Header("Metinler")]
    [SerializeField] private string bigWinBaslik  = "BÜYÜK KAZANÇ";
    [SerializeField] private string megaWinBaslik = "MUHTEŞEM KAZANÇ";
    [SerializeField] private string epicWinBaslik = "EFSANE KAZANÇ";

    [Header("Ses")]
    [Tooltip("Ses/Dıkş Sesi.mp3")]
    [SerializeField] private AudioClip tikSesi;
    [Tooltip("Ses/Alkış sesi.mp3")]
    [SerializeField] private AudioClip kazancFanfareSesi;
    [SerializeField][Range(0f,1f)] private float tikSesVolume     = 0.4f;
    [SerializeField][Range(0f,1f)] private float fanfareSesVolume = 0.8f;

    // ── renkler
    private static readonly Color _overlayRenk = new Color(0f, 0f, 0f, 0.75f);
    private static readonly Color _bigRenk     = new Color(1f, 0.843f, 0f);
    private static readonly Color _megaRenk    = new Color(1f, 0.647f, 0f);
    private static readonly Color _epicRenk    = new Color(1f, 0.843f, 0f);
    private static readonly Color _isinRenk    = new Color(1f, 0.843f, 0f);
    private static readonly Color _glowRenk    = new Color(1f, 0.60f,  0f);

    // ── katman image'ları (hepsi Awake'te programatik oluşturulur)
    private Image _glow;      // arka plan gaussian parlaması
    private Image _isinAlt;   // büyük katman, CW yavaş
    private Image _isinUst;   // küçük katman, CCW hızlı
    private Image _halka;     // pulse halka (WinPanel'den sonra = üstte)

    // ── döndürme durumu
    private float _altDonus;
    private const float _UstDonus = -45f; // sabit CCW
    private bool  _donuyor;

    // ── coroutine takibi
    private Coroutine  _aktifGosterimCoroutine;
    private Coroutine  _aktifParaCoroutine;
    private Coroutine  _halkaCoroutine;
    private GameObject _aktifParaParent;
    private AudioSource _audioSource;
    private Sprite     _coinSprite;

    private static Material _additiveMat;

    // ════════════════════════════════════════════════
    //  LIFECYCLE
    // ════════════════════════════════════════════════

    private void Awake()
    {
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha          = 0f;
            panelCanvasGroup.interactable   = false;
            panelCanvasGroup.blocksRaycasts = false;
        }
        if (olcekKoku != null)
        {
            olcekKoku.localScale = Vector3.zero;

            // DEFANSIF: olcekKoku anchor full-stretch (0,0 → 1,1) ise panel tam ekran kaplar.
            // Sahnede yanlışlıkla bu hâle gelmiş olabilir. Tespit edilirse middle-center + makul boyuta zorla.
            bool tamEkranAnchor = olcekKoku.anchorMin == Vector2.zero && olcekKoku.anchorMax == Vector2.one;
            if (tamEkranAnchor)
            {
                Debug.LogWarning("[WinFeedbackUI] olcekKoku full-stretch anchor tespit edildi (panel tam ekran). Middle-center 640×420'ye override ediliyor.");
                olcekKoku.anchorMin = new Vector2(0.5f, 0.5f);
                olcekKoku.anchorMax = new Vector2(0.5f, 0.5f);
                olcekKoku.pivot = new Vector2(0.5f, 0.5f);
                olcekKoku.sizeDelta = new Vector2(640f, 420f);
                olcekKoku.anchoredPosition = Vector2.zero;
            }
        }

        // Sıra önemli: her katman öncekinin GetSiblingIndex+1'ine yerleşir
        if (karartmaOverlay == null) karartmaOverlay = KarartmaOlustur();
        _glow    = GlowKatmanOlustur(karartmaOverlay);
        _isinAlt = IsinKatmanOlustur("IsinAltKatman", 1200f, _glow,    additive: true);
        _isinUst = IsinKatmanOlustur("IsinUstKatman",  720f, _isinAlt, additive: true);
        _halka   = HalkaOlustur();   // WinPanel'den sonra = en sonda

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f;
        _coinSprite = CoinSpriteOlustur();
    }

    private void Update()
    {
        if (!_donuyor) return;
        float dt = Time.unscaledDeltaTime;
        if (_isinAlt != null) _isinAlt.transform.Rotate(0f, 0f,  _altDonus * dt);
        if (_isinUst != null) _isinUst.transform.Rotate(0f, 0f,  _UstDonus * dt);
    }

    // ════════════════════════════════════════════════
    //  PUBLIC API
    // ════════════════════════════════════════════════

    private bool _atlaTiklamaBagli = false;
    private bool _gosterimAktif = false;
    private int _hedefKazanc;

    /// <summary>BIG WIN+ pop-up gosterimi suanda ekranda mi? Tutorial SayaciGecikmeliArtir
    /// bu property'yi yoklayarak panel kapandiktan SONRA sayaci ilerletir (gameObject.activeInHierarchy
    /// daimi true oldugu icin onun yerine bu kullanilmali).</summary>
    public bool GosterimAktif => _gosterimAktif;

    public void ShowWin(int kazanc, int bahis)
    {
        if (panelCanvasGroup == null || baslikText == null || kazancText == null) return;
        if (olcekKoku == null) olcekKoku = panelCanvasGroup.transform as RectTransform;
        if (olcekKoku == null || bahis <= 0 || kazanc < 0) return;

        var sev = KazancSeviyesiHesapla(kazanc, bahis);
        if (sev == KazancSeviyesi.Normal) return;

        if (_aktifGosterimCoroutine != null) { StopCoroutine(_aktifGosterimCoroutine); _aktifGosterimCoroutine = null; }
        if (_aktifParaCoroutine     != null) { StopCoroutine(_aktifParaCoroutine);     _aktifParaCoroutine     = null; }
        if (_halkaCoroutine         != null) { StopCoroutine(_halkaCoroutine);         _halkaCoroutine         = null; }
        if (_aktifParaParent        != null) { Destroy(_aktifParaParent);              _aktifParaParent        = null; }
        _donuyor = false;

        // Tıkla-atla: karartmaOverlay'a Button ekle (lazy, bir kere)
        TiklamaAtlaBagla();

        _hedefKazanc = kazanc;
        _gosterimAktif = true;
        _aktifGosterimCoroutine = StartCoroutine(GosterimCoroutine(sev, kazanc));
    }

    private void TiklamaAtlaBagla()
    {
        if (_atlaTiklamaBagli || karartmaOverlay == null) return;
        var btn = karartmaOverlay.GetComponent<Button>();
        if (btn == null) btn = karartmaOverlay.gameObject.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(AtlaVeKapat);
        karartmaOverlay.raycastTarget = true;
        _atlaTiklamaBagli = true;
    }

    /// <summary>Pop-up tıklandığında veya dış kaynaktan çağrıldığında: animasyon atlanır,
    /// kazanç metni anında final değere ayarlanır, gösterim kapanır.</summary>
    public void AtlaVeKapat()
    {
        if (!_gosterimAktif) return;
        _gosterimAktif = false;

        StopAllCoroutines();
        _aktifGosterimCoroutine = null;
        _aktifParaCoroutine = null;
        _halkaCoroutine = null;
        _donuyor = false;

        if (kazancText != null)
            kazancText.text = OyunFormatServisi.FormatTL(_hedefKazanc);

        if (_aktifParaParent != null) { Destroy(_aktifParaParent); _aktifParaParent = null; }

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.gameObject.SetActive(false);
        }
        if (olcekKoku != null) olcekKoku.localScale = Vector3.zero;

        DisableKatman(karartmaOverlay);
        DisableKatman(_glow);
        DisableKatman(_isinAlt);
        DisableKatman(_isinUst);
        DisableKatman(_halka);

        Debug.Log("[WinFeedback] Tikla-atla: gosterim hizla kapatildi, kazanc=" + _hedefKazanc);

        // Scripted uçuş bu event'i dinler — panel kapandı sinyali (sabit gecikmeyi tetikle).
        OnPanelKapandi?.Invoke();
    }

    // ════════════════════════════════════════════════
    //  TİER HESAPLAMA
    // ════════════════════════════════════════════════

    private static KazancSeviyesi KazancSeviyesiHesapla(int kazanc, int bahis)
    {
        long b = bahis;
        if (kazanc >= 15 * b) return KazancSeviyesi.EpicWin;
        if (kazanc >=  5 * b) return KazancSeviyesi.MegaWin;
        if (kazanc >=  2 * b) return KazancSeviyesi.BigWin;
        return KazancSeviyesi.Normal;
    }

    private string SeviyeBasligi(KazancSeviyesi s)
    {
        switch (s)
        {
            case KazancSeviyesi.BigWin:  return bigWinBaslik;
            case KazancSeviyesi.MegaWin: return megaWinBaslik;
            case KazancSeviyesi.EpicWin: return epicWinBaslik;
            default: return string.Empty;
        }
    }

    private static Color SeviyeRengi(KazancSeviyesi s)
    {
        switch (s) {
            case KazancSeviyesi.BigWin:  return _bigRenk;
            case KazancSeviyesi.MegaWin: return _megaRenk;
            case KazancSeviyesi.EpicWin: return _epicRenk;
            default: return Color.white;
        }
    }

    private static int ParaSayisi(KazancSeviyesi s)
    {
        switch (s) {
            case KazancSeviyesi.BigWin:  return  30;
            case KazancSeviyesi.MegaWin: return  80;
            case KazancSeviyesi.EpicWin: return 150;
            default: return 0;
        }
    }

    private static float SaymaSuresi(int k)
    {
        if (k <   500) return 0.6f;
        if (k <  2000) return 1.1f;
        if (k <  5000) return 1.9f;
        return 3.0f;
    }

    /// <summary>
    /// Counting up animasyonunun süresi — kazanç miktarına göre threshold'lardan seçilir
    /// (0.8 / 1.5 / 2.5 / 4.0 sn). ScriptedKazancUcusu BIG WIN+ tier'da uçuşu bu süre + buffer kadar geciktirir.
    /// </summary>
    public static float GetCountUpSuresi(int kazanc) => SaymaSuresi(kazanc);

    // tier → görsel parametreler
    private static float GlowA(KazancSeviyesi s)  { switch(s){case KazancSeviyesi.BigWin:return 0.28f;case KazancSeviyesi.MegaWin:return 0.50f;case KazancSeviyesi.EpicWin:return 0.70f;default:return 0f;} }
    private static float AltA(KazancSeviyesi s)   { switch(s){case KazancSeviyesi.BigWin:return 0.55f;case KazancSeviyesi.MegaWin:return 0.72f;case KazancSeviyesi.EpicWin:return 0.90f;default:return 0f;} }
    private static float UstA(KazancSeviyesi s)   { switch(s){case KazancSeviyesi.BigWin:return 0.60f;case KazancSeviyesi.MegaWin:return 0.80f;case KazancSeviyesi.EpicWin:return 1.00f;default:return 0f;} }
    private static float AltHz(KazancSeviyesi s)  { switch(s){case KazancSeviyesi.BigWin:return 15f; case KazancSeviyesi.MegaWin:return 25f; case KazancSeviyesi.EpicWin:return 35f; default:return 0f;} }

    // ════════════════════════════════════════════════
    //  ANA COROUTINE
    // ════════════════════════════════════════════════

    private IEnumerator GosterimCoroutine(KazancSeviyesi sev, int kazanc)
    {
        baslikText.text  = SeviyeBasligi(sev);
        baslikText.color = SeviyeRengi(sev);
        kazancText.text  = OyunFormatServisi.FormatTL(0);

        var tmp = baslikText as TextMeshProUGUI;
        if (tmp != null)
        {
            tmp.fontStyle    = FontStyles.Bold;
            tmp.outlineWidth = (sev == KazancSeviyesi.EpicWin) ? 0.2f : 0f;
            tmp.outlineColor = new Color32(180, 100, 0, 255);
        }

        float ga = GlowA(sev), aa = AltA(sev), ua = UstA(sev);

        // ── Tüm katmanları başlangıç durumuna al
        EnableKatman(karartmaOverlay, Color.clear);
        EnableKatman(_glow,    new Color(_glowRenk.r, _glowRenk.g, _glowRenk.b, 0f));
        EnableKatman(_isinAlt, new Color(_isinRenk.r, _isinRenk.g, _isinRenk.b, 0f), scale: 0.3f, resetRot: true);
        EnableKatman(_isinUst, new Color(_isinRenk.r, _isinRenk.g, _isinRenk.b, 0f), scale: 0.3f, resetRot: true);
        EnableKatman(_halka,   new Color(1f, 0.843f, 0f, 0f), scale: 0f);

        panelCanvasGroup.gameObject.SetActive(true);
        panelCanvasGroup.interactable   = false;
        panelCanvasGroup.blocksRaycasts = false;
        olcekKoku.localScale            = Vector3.zero;
        panelCanvasGroup.alpha          = 0f;

        // ── Giriş animasyonu
        float gsure = Mathf.Max(0.01f, girisAnimasyonSuresi);
        for (float t = 0f; t < gsure; t += Time.unscaledDeltaTime)
        {
            float u     = Mathf.Clamp01(t / gsure);
            float eased = EaseOutBack(u);

            olcekKoku.localScale   = Vector3.one * eased;
            panelCanvasGroup.alpha = u;
            if (karartmaOverlay != null) karartmaOverlay.color = Color.Lerp(Color.clear, _overlayRenk, u);

            float isinSc = Mathf.Lerp(0.3f, 1f, Mathf.Clamp01(t / 0.3f));
            SetAlpha(_glow,    ga * u);
            SetAlpha(_isinAlt, aa * u);
            SetAlpha(_isinUst, ua * u);
            SetScale(_isinAlt, isinSc);
            SetScale(_isinUst, isinSc);

            yield return null;
        }

        // final
        olcekKoku.localScale   = Vector3.one;
        panelCanvasGroup.alpha = 1f;
        if (karartmaOverlay != null) karartmaOverlay.color = _overlayRenk;
        SetAlpha(_glow, ga); SetAlpha(_isinAlt, aa); SetAlpha(_isinUst, ua);
        SetScale(_isinAlt, 1f); SetScale(_isinUst, 1f);

        _altDonus = AltHz(sev);
        _donuyor  = true;

        _halkaCoroutine     = StartCoroutine(HalkaPulseCoroutine());
        _aktifParaCoroutine = StartCoroutine(ParaYagmuruCoroutine(sev));
        if (sev == KazancSeviyesi.EpicWin)
            StartCoroutine(KameraSallamaCoroutine(0.2f, 0.4f, 0.2f));

        yield return StartCoroutine(ParaSaymaCoroutine(kazanc));

        if (kazancFanfareSesi != null && _audioSource != null)
            _audioSource.PlayOneShot(kazancFanfareSesi, fanfareSesVolume);

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, saymaFazlaSuresi));

        // ── Çıkış animasyonu
        _donuyor = false;
        if (_halkaCoroutine != null) { StopCoroutine(_halkaCoroutine); _halkaCoroutine = null; }

        float halkaBaslangicA = (_halka != null) ? _halka.color.a : 0f;
        float csure = Mathf.Max(0.01f, cikisAnimasyonSuresi);
        for (float t = 0f; t < csure; t += Time.unscaledDeltaTime)
        {
            float u     = Mathf.Clamp01(t / csure);
            float eased = 1f - Mathf.Pow(1f - u, 2f);

            panelCanvasGroup.alpha = Mathf.Lerp(1f,  0f,    eased);
            olcekKoku.localScale   = Vector3.one * Mathf.Lerp(1f, 0.85f, eased);
            if (karartmaOverlay != null) karartmaOverlay.color = Color.Lerp(_overlayRenk, Color.clear, eased);
            SetAlpha(_glow,    ga * (1f - eased));
            SetAlpha(_isinAlt, aa * (1f - eased));
            SetAlpha(_isinUst, ua * (1f - eased));
            SetAlpha(_halka,   Mathf.Lerp(halkaBaslangicA, 0f, eased));
            yield return null;
        }

        // cleanup
        panelCanvasGroup.alpha = 0f;
        olcekKoku.localScale   = Vector3.zero;
        panelCanvasGroup.gameObject.SetActive(false);
        DisableKatman(karartmaOverlay);
        DisableKatman(_glow); DisableKatman(_isinAlt);
        DisableKatman(_isinUst); DisableKatman(_halka);
        if (tmp != null) { tmp.fontStyle = FontStyles.Normal; tmp.outlineWidth = 0f; }
        _aktifGosterimCoroutine = null;
        _gosterimAktif = false;

        // Scripted uçuş bu event'i dinler — otomatik akış sonu (fade-out tamam, panel kapalı).
        // AtlaVeKapat StopAllCoroutines ile bu coroutine'i durdurursa burası çağrılmaz; AtlaVeKapat kendi invoke'unu yapar.
        OnPanelKapandi?.Invoke();
    }

    // ════════════════════════════════════════════════
    //  HALKA PULSE
    // ════════════════════════════════════════════════

    private IEnumerator HalkaPulseCoroutine()
    {
        while (true)
        {
            if (_halka == null) yield break;
            _halka.transform.localScale = Vector3.zero;
            SetAlpha(_halka, 0f);

            const float pSure = 1.0f;
            for (float t = 0f; t < pSure; t += Time.unscaledDeltaTime)
            {
                if (_halka == null) yield break;
                float u = t / pSure;
                _halka.transform.localScale = Vector3.one * Mathf.Lerp(0f, 1.5f, EaseOutQuad(u));
                SetAlpha(_halka, Mathf.Lerp(0.5f, 0f, u));
                yield return null;
            }
            // kısa bekleme sonra tekrar
            yield return new WaitForSecondsRealtime(0.05f);
        }
    }

    // ════════════════════════════════════════════════
    //  PARA SAYMA
    // ════════════════════════════════════════════════

    private IEnumerator ParaSaymaCoroutine(int hedef)
    {
        float sure = SaymaSuresi(hedef);
        float sonTik = -999f;

        for (float t = 0f; t < sure; t += Time.unscaledDeltaTime)
        {
            float e = EaseOutQuad(Mathf.Clamp01(t / sure));
            kazancText.text = OyunFormatServisi.FormatTL(Mathf.RoundToInt(e * hedef));

            float aralık = Mathf.Lerp(0.05f, 0.22f, e);
            float simdi  = Time.unscaledTime;
            if (tikSesi != null && _audioSource != null && simdi - sonTik >= aralık)
            {
                _audioSource.PlayOneShot(tikSesi, tikSesVolume);
                sonTik = simdi;
            }
            yield return null;
        }
        kazancText.text = OyunFormatServisi.FormatTL(hedef);
    }

    // ════════════════════════════════════════════════
    //  PARA YAĞMURU
    // ════════════════════════════════════════════════

    private IEnumerator ParaYagmuruCoroutine(KazancSeviyesi sev)
    {
        int sayi = ParaSayisi(sev);
        if (sayi <= 0 || panelCanvasGroup == null) yield break;

        Transform winRoot = panelCanvasGroup.transform.parent ?? panelCanvasGroup.transform;
        Canvas canvas = winRoot.GetComponentInParent<Canvas>();
        if (canvas == null) yield break;

        var crt = canvas.GetComponent<RectTransform>();
        float W = crt != null ? crt.rect.width  : Screen.width;
        float H = crt != null ? crt.rect.height : Screen.height;

        _aktifParaParent = new GameObject("ParaYagmuru");
        var prt = _aktifParaParent.AddComponent<RectTransform>();
        _aktifParaParent.transform.SetParent(winRoot, false);
        prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
        prt.sizeDelta = prt.anchoredPosition = Vector2.zero;
        _aktifParaParent.transform.SetSiblingIndex(panelCanvasGroup.transform.GetSiblingIndex());

        for (int i = 0; i < sayi; i++)
        {
            SpawnPara(_aktifParaParent.transform, W, H, i);
            if (i % 15 == 14) yield return null;
        }

        yield return new WaitForSecondsRealtime(3.5f);
        if (_aktifParaParent != null) { Destroy(_aktifParaParent); _aktifParaParent = null; }
        _aktifParaCoroutine = null;
    }

    private void SpawnPara(Transform parent, float W, float H, int idx)
    {
        var go = new GameObject("P" + idx);
        var rt = go.AddComponent<RectTransform>();
        go.transform.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = Vector2.one * Random.Range(14f, 26f);
        rt.anchoredPosition = new Vector2(Random.Range(-W * 0.48f, W * 0.48f), H * 0.5f + 60f);

        var img = go.AddComponent<Image>();
        img.sprite = _coinSprite;
        Color[] palet = { new Color(1f,0.843f,0f), new Color(1f,0.7f,0f), new Color(0.9f,0.6f,0.1f), new Color(1f,0.9f,0.2f) };
        img.color = palet[Random.Range(0, palet.Length)];
        StartCoroutine(ParaDusCoroutine(rt, img, H, Random.Range(0f, 0.55f)));
    }

    private IEnumerator ParaDusCoroutine(RectTransform rt, Image img, float H, float gecikme)
    {
        if (gecikme > 0f) yield return new WaitForSecondsRealtime(gecikme);
        if (rt == null) yield break;

        float xS  = Random.Range(-90f, 90f);
        float don = Random.Range(-270f, 270f);
        float omur = Random.Range(2f, 3f);
        float el  = 0f;
        Vector2 bas = rt.anchoredPosition;
        float hy  = -H * 0.5f - 100f;

        while (el < omur)
        {
            if (rt == null) yield break;
            el += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(el / omur);
            rt.anchoredPosition = new Vector2(bas.x + xS * Mathf.Sin(t * Mathf.PI * 2.5f), Mathf.Lerp(bas.y, hy, t * t));
            rt.Rotate(0f, 0f, don * Time.unscaledDeltaTime);
            if (t > 0.75f && img != null)
                img.color = new Color(img.color.r, img.color.g, img.color.b, 1f - (t - 0.75f) / 0.25f);
            yield return null;
        }
        if (rt != null) Destroy(rt.gameObject);
    }

    // ════════════════════════════════════════════════
    //  KAMERA SALLAMA
    // ════════════════════════════════════════════════

    private IEnumerator KameraSallamaCoroutine(float gecikme, float sure, float siddet)
    {
        yield return new WaitForSecondsRealtime(gecikme);
        Camera cam = Camera.main;
        if (cam == null) yield break;
        Vector3 op = cam.transform.localPosition;
        for (float t = 0f; t < sure; t += Time.unscaledDeltaTime)
        {
            Vector3 off = Random.insideUnitSphere * (siddet * (1f - t / sure));
            off.z = 0f;
            cam.transform.localPosition = op + off;
            yield return null;
        }
        cam.transform.localPosition = op;
    }

    // ════════════════════════════════════════════════
    //  KATMAN OLUŞTURMA
    // ════════════════════════════════════════════════

    private Image GlowKatmanOlustur(Image onceki)
    {
        var go = KatmanGoOlustur("WinGlow", 800f, onceki);
        if (go == null) return null;
        var img = go.AddComponent<Image>();
        img.raycastTarget = false;
        img.color = new Color(_glowRenk.r, _glowRenk.g, _glowRenk.b, 0f);
        var tex = GlowTex(128);
        img.sprite = Sprite.Create(tex, new Rect(0,0,tex.width,tex.height), new Vector2(0.5f,0.5f));
        go.SetActive(false);
        return img;
    }

    private Image IsinKatmanOlustur(string isim, float boyut, Image onceki, bool additive)
    {
        var go = KatmanGoOlustur(isim, boyut, onceki);
        if (go == null) return null;
        var img = go.AddComponent<Image>();
        img.raycastTarget = false;
        img.color = new Color(_isinRenk.r, _isinRenk.g, _isinRenk.b, 0f);
        if (additive) img.material = AdditiveMat();
        var tex = IsinTex(256, 12);
        img.sprite = Sprite.Create(tex, new Rect(0,0,tex.width,tex.height), new Vector2(0.5f,0.5f));
        go.SetActive(false);
        return img;
    }

    private Image HalkaOlustur()
    {
        if (panelCanvasGroup == null) return null;
        Transform parent = panelCanvasGroup.transform.parent ?? panelCanvasGroup.transform;
        var go = new GameObject("WinHalka");
        go.transform.SetParent(parent, false);
        // WinPanel'den sonra (son sibling)
        go.transform.SetSiblingIndex(parent.childCount - 1);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = Vector2.one * 440f;
        rt.anchoredPosition = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.raycastTarget = false;
        img.color = new Color(1f, 0.843f, 0f, 0f);
        var tex = HalkaTex(128);
        img.sprite = Sprite.Create(tex, new Rect(0,0,tex.width,tex.height), new Vector2(0.5f,0.5f));
        go.SetActive(false);
        return img;
    }

    // Ortak RT+GO oluşturma (sibling: onceki+1)
    private GameObject KatmanGoOlustur(string isim, float boyut, Image onceki)
    {
        if (panelCanvasGroup == null) return null;
        Transform parent = panelCanvasGroup.transform.parent ?? panelCanvasGroup.transform;
        var go = new GameObject(isim);
        go.transform.SetParent(parent, false);
        int idx = (onceki != null)
            ? onceki.transform.GetSiblingIndex() + 1
            : Mathf.Max(0, panelCanvasGroup.transform.GetSiblingIndex());
        go.transform.SetSiblingIndex(idx);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = Vector2.one * boyut;
        rt.anchoredPosition = Vector2.zero;
        return go;
    }

    // ════════════════════════════════════════════════
    //  TEXTURE ÜRETME
    // ════════════════════════════════════════════════

    private static Texture2D IsinTex(int sz, int count)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.ARGB32, false);
        float c = sz * 0.5f, ic = c * 0.07f, div = 360f / count;
        const float edge = 0.08f;
        var px = new Color[sz * sz];
        for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float dx = x-c, dy = y-c, r = Mathf.Sqrt(dx*dx+dy*dy);
                if (r < ic || r >= c) { px[y*sz+x] = Color.clear; continue; }
                float norm = (((Mathf.Atan2(dy,dx)*Mathf.Rad2Deg)+360f)%360f % div) / div;
                float aA;
                if      (norm < edge)        aA = norm/edge;
                else if (norm < 0.5f-edge)   aA = 1f;
                else if (norm < 0.5f)        aA = 1f-(norm-(0.5f-edge))/edge;
                else                         aA = 0f;
                float fd = Mathf.Pow(Mathf.Max(0f, 1f-(r-ic)/(c-ic)), 0.6f);
                px[y*sz+x] = new Color(1f,1f,1f, aA*fd);
            }
        tex.SetPixels(px); tex.Apply(); return tex;
    }

    private static Texture2D GlowTex(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.ARGB32, false);
        float c = sz * 0.5f;
        var px = new Color[sz * sz];
        for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float dx=x-c, dy=y-c, r=Mathf.Sqrt(dx*dx+dy*dy)/c;
                px[y*sz+x] = new Color(1f,1f,1f, Mathf.Clamp01(Mathf.Exp(-r*r*3f)));
            }
        tex.SetPixels(px); tex.Apply(); return tex;
    }

    private static Texture2D HalkaTex(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.ARGB32, false);
        float c = sz*0.5f, dis=c*0.95f, ic=c*0.72f, k=c*0.06f;
        var px = new Color[sz*sz];
        for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float r = Mathf.Sqrt((x-c)*(x-c)+(y-c)*(y-c));
                float a;
                if      (r < ic-k)   a = 0f;
                else if (r < ic)     a = (r-(ic-k))/k;
                else if (r < dis-k)  a = 1f;
                else if (r < dis)    a = 1f-(r-(dis-k))/k;
                else                 a = 0f;
                px[y*sz+x] = new Color(1f,1f,1f,a);
            }
        tex.SetPixels(px); tex.Apply(); return tex;
    }

    private static Sprite CoinSpriteOlustur()
    {
        const int S=32; float c=S*0.5f, r=c-1.5f;
        var tex = new Texture2D(S,S,TextureFormat.ARGB32,false);
        var px  = new Color[S*S];
        for (int y=0;y<S;y++) for (int x=0;x<S;x++) {
            float d=Mathf.Sqrt((x-c+.5f)*(x-c+.5f)+(y-c+.5f)*(y-c+.5f));
            px[y*S+x]=new Color(1f,1f,1f,Mathf.Clamp01((r-d)/1.5f));
        }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex,new Rect(0,0,S,S),new Vector2(0.5f,0.5f));
    }

    // ════════════════════════════════════════════════
    //  MATERİAL (additive blending)
    // ════════════════════════════════════════════════

    private static Material AdditiveMat()
    {
        if (_additiveMat != null) return _additiveMat;
        Shader sh = Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default");
        _additiveMat = new Material(sh);
        _additiveMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _additiveMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        _additiveMat.SetInt("_ZWrite", 0);
        _additiveMat.renderQueue = 3000;
        return _additiveMat;
    }

    // ════════════════════════════════════════════════
    //  OVERLAY
    // ════════════════════════════════════════════════

    private Image KarartmaOlustur()
    {
        if (panelCanvasGroup == null) return null;
        Transform parent = panelCanvasGroup.transform.parent ?? panelCanvasGroup.transform;
        var go = new GameObject("KarartmaOverlay");
        go.transform.SetParent(parent, false);
        go.transform.SetSiblingIndex(Mathf.Max(0, panelCanvasGroup.transform.GetSiblingIndex()));
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.sizeDelta = rt.anchoredPosition = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = Color.clear; img.raycastTarget = false;
        go.SetActive(false);
        return img;
    }

    // ════════════════════════════════════════════════
    //  YARDIMCI
    // ════════════════════════════════════════════════

    private static void EnableKatman(Image img, Color c, float scale = 1f, bool resetRot = false)
    {
        if (img == null) return;
        img.gameObject.SetActive(true);
        img.color = c;
        img.transform.localScale = Vector3.one * scale;
        if (resetRot) img.transform.localRotation = Quaternion.identity;
    }

    private static void SetAlpha(Image img, float a)
    {
        if (img == null) return;
        var col = img.color; col.a = Mathf.Max(0f, a); img.color = col;
    }

    private static void SetScale(Image img, float s)
    {
        if (img == null) return;
        img.transform.localScale = Vector3.one * s;
    }

    private static void DisableKatman(Image img)
    {
        if (img == null) return;
        var col = img.color; col.a = 0f; img.color = col;
        img.gameObject.SetActive(false);
    }

    // ════════════════════════════════════════════════
    //  EASING
    // ════════════════════════════════════════════════

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t-1f,3f) + c1 * Mathf.Pow(t-1f,2f);
    }

    private static float EaseOutQuad(float t) => 1f - (1f-t) * (1f-t);
}
