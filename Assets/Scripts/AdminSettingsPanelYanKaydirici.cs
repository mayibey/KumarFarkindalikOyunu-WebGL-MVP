using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// AdminSettingsPanel solunda ok butonu; basınca içerik sağa kayar ve panel daralır (yalnız ok görünür).
/// Tekrar basınca genişler.
/// </summary>
[DisallowMultipleComponent]
public class AdminSettingsPanelYanKaydirici : MonoBehaviour
{
    /// <summary>
    /// Play modunda sahne yüklendikten sonra (veya additive yüklemede) AdminSettingsPanel açık kalmışsa kapatır.
    /// Enter Play Mode / sahne YAML ile uyuşmayan durumlarda (başka script açıyorsa) garanti davranış.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void SceneLoadedAbonelikSifirla()
    {
        SceneManager.sceneLoaded -= SahneYuklendigindeAktifAdminPaneliniKapat;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void PlayBasindaAdminSettingsPanelKapatVeAboneOl()
    {
        TumAktifAdminSettingsPanelleriniKapat();
        SceneManager.sceneLoaded -= SahneYuklendigindeAktifAdminPaneliniKapat;
        SceneManager.sceneLoaded += SahneYuklendigindeAktifAdminPaneliniKapat;
    }

    static void SahneYuklendigindeAktifAdminPaneliniKapat(Scene scene, LoadSceneMode mode)
    {
        if (!Application.isPlaying)
            return;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var k in root.GetComponentsInChildren<AdminSettingsPanelYanKaydirici>(true))
            {
                if (k != null && k.gameObject.activeSelf)
                    k.gameObject.SetActive(false);
            }
        }
    }

    static void TumAktifAdminSettingsPanelleriniKapat()
    {
        foreach (var k in Object.FindObjectsByType<AdminSettingsPanelYanKaydirici>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (k != null && k.gameObject.activeSelf)
                k.gameObject.SetActive(false);
        }
    }

    [SerializeField] private float animasyonSuresi = 0.28f;
    [SerializeField] private float gizliGenislik = 44f;
    [SerializeField] private float okGenislik = 36f;
    [SerializeField] private Color okArkaPlan = new Color(0.22f, 0.35f, 0.55f, 1f);

    private const string GovdeAdi = "AdminKaydirilanGovde";
    private const string OkAdi = "AdminPanelSolOk";

    private RectTransform _kokRt;
    private RectTransform _kaydirRt;
    private RectTransform _okRt;
    private CanvasGroup _kaydirCg;
    private TextMeshProUGUI _okYazi;
    private Vector2 _acikSizeDelta;
    private Vector2 _acikKaydirPos;
    private float _kaydirMiktar;
    private bool _gizli;
    private Coroutine _anim;

    private void Awake()
    {
        _kokRt = (RectTransform)transform;

        Transform mevcutGovde = transform.Find(GovdeAdi);
        if (mevcutGovde != null)
        {
            _kaydirRt = (RectTransform)mevcutGovde;
            _kaydirCg = _kaydirRt.GetComponent<CanvasGroup>();
            if (_kaydirCg == null)
                _kaydirCg = _kaydirRt.gameObject.AddComponent<CanvasGroup>();
            Transform okT = transform.Find(OkAdi);
            if (okT != null)
            {
                _okRt = okT as RectTransform;
                var btn = okT.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveListener(TogglePanel);
                    btn.onClick.AddListener(TogglePanel);
                }
                _okYazi = okT.GetComponentInChildren<TextMeshProUGUI>(true);
            }
            AcikDurumBaziniYakala();
            OkYazisiniGuncelle();
            return;
        }

        if (GetComponent<RectMask2D>() == null)
            gameObject.AddComponent<RectMask2D>();

        var wrapGo = new GameObject(GovdeAdi, typeof(RectTransform));
        _kaydirRt = wrapGo.GetComponent<RectTransform>();
        _kaydirRt.SetParent(_kokRt, false);
        _kaydirRt.SetAsFirstSibling();
        _kaydirRt.anchorMin = Vector2.zero;
        _kaydirRt.anchorMax = Vector2.one;
        _kaydirRt.offsetMin = Vector2.zero;
        _kaydirRt.offsetMax = Vector2.zero;
        _kaydirCg = wrapGo.AddComponent<CanvasGroup>();

        for (int i = _kokRt.childCount - 1; i >= 0; i--)
        {
            var c = _kokRt.GetChild(i);
            if (c == _kaydirRt.transform)
                continue;
            c.SetParent(_kaydirRt, false);
        }

        var okGo = new GameObject(OkAdi, typeof(RectTransform));
        var img = okGo.AddComponent<Image>();
        img.color = okArkaPlan;
        img.raycastTarget = true;
        img.sprite = null;
        var btnOk = okGo.AddComponent<Button>();
        btnOk.targetGraphic = img;
        btnOk.onClick.AddListener(TogglePanel);

        _okRt = (RectTransform)okGo.transform;
        _okRt.SetParent(_kokRt, false);
        _okRt.anchorMin = new Vector2(0f, 0.5f);
        _okRt.anchorMax = new Vector2(0f, 0.5f);
        _okRt.pivot = new Vector2(0f, 0.5f);
        _okRt.sizeDelta = new Vector2(okGenislik, 120f);
        _okRt.anchoredPosition = new Vector2(4f, 0f);

        var textGo = new GameObject("OkMetin", typeof(RectTransform));
        textGo.transform.SetParent(okGo.transform, false);
        var trt = (RectTransform)textGo.transform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        _okYazi = textGo.AddComponent<TextMeshProUGUI>();
        _okYazi.alignment = TextAlignmentOptions.Center;
        _okYazi.fontSize = 32;
        _okYazi.raycastTarget = false;
        _okYazi.text = "›";
        _okYazi.enableAutoSizing = false;
        var kaynakTmp = _kaydirRt.GetComponentInChildren<TMP_Text>(true);
        if (kaynakTmp != null)
            _okYazi.font = kaynakTmp.font;

        Canvas.ForceUpdateCanvases();
        AcikDurumBaziniYakala();
        OkButonBoyutunuGuncelle();
        OkYazisiniGuncelle();
    }

    /// <summary>
    /// Açık panel genişliği ve içerik konumu — yalnızca gerçekten açıkken güncellenmeli.
    /// Gizleme animasyonu sırasında _gizli hâlâ false olduğu için OnRectTransformDimensionsChange
    /// ile burayı her kare yazmak geri açmayı bozuyordu.
    /// </summary>
    private void AcikDurumBaziniYakala()
    {
        _acikSizeDelta = _kokRt.sizeDelta;
        _acikKaydirPos = _kaydirRt.anchoredPosition;
        KaydirMiktariniHesapla();
    }

    private void KaydirMiktariniHesapla()
    {
        float w = Mathf.Max(_acikSizeDelta.x, _kaydirRt.rect.width);
        _kaydirMiktar = Mathf.Max(w - gizliGenislik + 8f, 200f);
    }

    private void OnRectTransformDimensionsChange()
    {
        if (_kokRt == null || _kaydirRt == null || !Application.isPlaying)
            return;
        if (_anim != null)
            return;
        if (_gizli)
            return;
        if (_kokRt.sizeDelta.x <= gizliGenislik + 2f)
            return;
        AcikDurumBaziniYakala();
        OkButonBoyutunuGuncelle();
    }

    public void TogglePanel()
    {
        if (_anim != null)
            StopCoroutine(_anim);
        _anim = StartCoroutine(AnimEt(!_gizli));
    }

    /// <summary>
    /// Panel tekrar <c>SetActive(true)</c> ile açıldığında Awake yeniden çalışmadığı için dar/gizli durum kalabilir; dışarıdan tam genişliğe sıfırlar.
    /// </summary>
    public void ZorlaTamGenisAc()
    {
        if (_kokRt == null || _kaydirRt == null)
            return;
        if (_anim != null)
        {
            StopCoroutine(_anim);
            _anim = null;
        }
        if (_acikSizeDelta.x <= gizliGenislik + 1f)
            AcikDurumBaziniYakala();
        float hedefX = Mathf.Max(_acikSizeDelta.x, gizliGenislik + 50f);
        _kokRt.sizeDelta = new Vector2(hedefX, _kokRt.sizeDelta.y);
        _kaydirRt.anchoredPosition = new Vector2(_acikKaydirPos.x, _kaydirRt.anchoredPosition.y);
        _gizli = false;
        if (_kaydirCg != null)
        {
            _kaydirCg.blocksRaycasts = true;
            _kaydirCg.alpha = 1f;
        }
        OkYazisiniGuncelle();
        OkButonBoyutunuGuncelle();
    }

    private IEnumerator AnimEt(bool gizlenecek)
    {
        float t0 = 0f;
        float baslangicX = _kokRt.sizeDelta.x;
        float hedefX = gizlenecek ? gizliGenislik : _acikSizeDelta.x;
        float k0 = _kaydirRt.anchoredPosition.x;
        float k1 = gizlenecek ? _acikKaydirPos.x + _kaydirMiktar : _acikKaydirPos.x;

        while (t0 < animasyonSuresi)
        {
            t0 += Time.unscaledDeltaTime;
            float u = animasyonSuresi <= 0f ? 1f : Mathf.Clamp01(t0 / animasyonSuresi);
            float s = u * u * (3f - 2f * u);
            _kokRt.sizeDelta = new Vector2(Mathf.Lerp(baslangicX, hedefX, s), _kokRt.sizeDelta.y);
            _kaydirRt.anchoredPosition = new Vector2(Mathf.Lerp(k0, k1, s), _kaydirRt.anchoredPosition.y);
            yield return null;
        }

        _kokRt.sizeDelta = new Vector2(hedefX, _kokRt.sizeDelta.y);
        _kaydirRt.anchoredPosition = new Vector2(k1, _kaydirRt.anchoredPosition.y);
        _gizli = gizlenecek;
        if (_kaydirCg != null)
            _kaydirCg.blocksRaycasts = !_gizli;
        if (!_gizli)
            AcikDurumBaziniYakala();
        OkYazisiniGuncelle();
        _anim = null;
    }

    private void OkYazisiniGuncelle()
    {
        if (_okYazi == null)
            return;
        // Açıkken: sağa kaydır (gizle). Gizliyken: sola genişlet (aç).
        _okYazi.text = _gizli ? "‹" : "›";
    }

    private void OkButonBoyutunuGuncelle()
    {
        if (_okRt == null || _kokRt == null) return;
        float hedefY = Mathf.Clamp(_kokRt.rect.height * 0.28f, 92f, 180f);
        _okRt.sizeDelta = new Vector2(okGenislik, hedefY);
    }
}
