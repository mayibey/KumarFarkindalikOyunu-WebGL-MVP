using System.Collections;
using Senaryo.Scripted;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using UnityEngine.UI;

/// <summary>
/// 03_SenaryoluOyun (build idx 2) için idle reminder servisi — HatirlatmaServisi (04 Tutorial) pattern kopyası.
/// Kullanıcı 15sn aksiyon yapmadıysa alt-orta'da mini-modal: "Hadi, SPIN'e bas — izle ve gör".
/// ScriptedModal/Bonus/Yukleme/Final/DusunceBalonu açıkken VEYA AnlaticiOzelAkisAktif iken idle saymaz.
/// SpinCalisiyorMu true iken idle reset (animasyon süresi uyarı olmasın).
/// </summary>
[Preserve]
public class AnlaticiHatirlatmaServisi : MonoBehaviour
{
    public const int ANLATICI_SAHNE_BUILD_INDEX = 2;
    public const int CANVAS_SORTING_ORDER = 1620;

    public static AnlaticiHatirlatmaServisi Ornek { get; private set; }

    private const float IDLE_ESIK = 15f;
    private const float COOLDOWN = 20f;
    private const float GOSTERIM_SURESI = 3f;
    private const float SCALE_SURESI = 0.2f;
    private const float SALLANMA_SURESI = 1.2f;
    private const float SALLANMA_ACI = 4f;

    private static readonly Color BALON_KOYU = new Color(0.06f, 0.10f, 0.14f, 0.95f);
    private static readonly Color ALTIN_ACIK = new Color(0.95f, 0.80f, 0.30f, 1f);
    private static readonly Color BEYAZ = new Color(0.95f, 0.97f, 1f, 1f);

    private float _sonAktivite;
    private float _sonHatirlatma = -999f;
    private bool _hatirlatmaAcik;
    private OyunYoneticisi _oy;

    private GameObject _root;
    private RectTransform _balonRt;
    private TextMeshProUGUI _mesajText;

    [Preserve]
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OtomatikInit()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        var aktifSahne = SceneManager.GetActiveScene();
        if (aktifSahne.buildIndex == ANLATICI_SAHNE_BUILD_INDEX)
            OnSceneLoaded(aktifSahne, LoadSceneMode.Single);
    }

    [Preserve]
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex != ANLATICI_SAHNE_BUILD_INDEX)
        {
            if (Ornek != null) UnityEngine.Object.Destroy(Ornek.gameObject);
            return;
        }
        if (Ornek != null) return;
        var go = new GameObject(nameof(AnlaticiHatirlatmaServisi));
        go.AddComponent<AnlaticiHatirlatmaServisi>();
    }

    private void Awake()
    {
        if (SceneManager.GetActiveScene().buildIndex != ANLATICI_SAHNE_BUILD_INDEX)
        {
            gameObject.SetActive(false);
            return;
        }
        if (Ornek != null && Ornek != this) { Destroy(gameObject); return; }
        Ornek = this;

        if (UnityEngine.Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem (Auto)");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        UIYarat();
        if (_root != null) _root.SetActive(false);
        _sonAktivite = Time.unscaledTime;
        Debug.Log("[AnlaticiHatirlatmaServisi] Spawn: Ornek atandı (03 idle reminder aktif).");
    }

    private void OnDestroy()
    {
        if (Ornek == this) Ornek = null;
    }

    public void AktiviteHaberVer()
    {
        _sonAktivite = Time.unscaledTime;
    }

    private void Update()
    {
        if (_hatirlatmaAcik) return;

        // 03 modal/bonus/yükleme/final/dusunce/anlatıcı özel akışlardan herhangi biri açıksa idle reset
        if (ScriptedModalKopru.ModalAcik
            || ScriptedYuklemePaneli.IsAcik
            || ScriptedFinalEkrani.IsAcik
            || ScriptedBonusTuzagiPopup.IsAcik
            || ScriptedBonusOyunUygulayici.IsAcik
            || ScriptedDusunceBalonu.BalonAcik
            || AnlaticiSeritKopru.BonusBitisAcik
            || AnlaticiSeritKopru.AnlaticiOzelAkisAktif)
        {
            _sonAktivite = Time.unscaledTime;
            return;
        }

        // Spin çalışıyorsa idle reset (animasyon süresi uyarı olmasın)
        if (_oy == null) _oy = UnityEngine.Object.FindObjectOfType<OyunYoneticisi>();
        if (_oy != null && (_oy.SpinCalisiyorMu || _oy.BonusAktifMi))
        {
            _sonAktivite = Time.unscaledTime;
            return;
        }

        float idle = Time.unscaledTime - _sonAktivite;
        float cooldownGecen = Time.unscaledTime - _sonHatirlatma;
        if (idle >= IDLE_ESIK && cooldownGecen >= COOLDOWN)
            StartCoroutine(Goster("Hadi, SPIN'e bas — izle ve gör"));
    }

    private IEnumerator Goster(string mesaj)
    {
        _hatirlatmaAcik = true;
        _sonHatirlatma = Time.unscaledTime;

        if (_mesajText != null) _mesajText.text = mesaj;
        if (_root != null) _root.SetActive(true);
        if (_balonRt != null) _balonRt.localScale = Vector3.zero;

        yield return AnimasyonScale(0f, 1f, SCALE_SURESI);
        yield return AnimasyonSallanma(SALLANMA_SURESI, SALLANMA_ACI);

        float kalan = GOSTERIM_SURESI - SCALE_SURESI - SALLANMA_SURESI;
        if (kalan > 0) yield return new WaitForSecondsRealtime(kalan);

        yield return AnimasyonScale(1f, 0f, SCALE_SURESI);

        if (_root != null) _root.SetActive(false);
        if (_balonRt != null) _balonRt.localEulerAngles = Vector3.zero;
        _hatirlatmaAcik = false;
        _sonAktivite = Time.unscaledTime;
    }

    private IEnumerator AnimasyonScale(float baslangic, float bitis, float sure)
    {
        float t = 0f;
        while (t < sure)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / sure);
            float ease = u * u * (3f - 2f * u);
            float s = Mathf.Lerp(baslangic, bitis, ease);
            if (_balonRt != null) _balonRt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        if (_balonRt != null) _balonRt.localScale = new Vector3(bitis, bitis, 1f);
    }

    private IEnumerator AnimasyonSallanma(float sure, float aci)
    {
        float t = 0f;
        int osc = 4;
        float periodSure = sure / osc;
        while (t < sure)
        {
            t += Time.unscaledDeltaTime;
            float u = (t % periodSure) / periodSure;
            float z = Mathf.Sin(u * Mathf.PI * 2f) * aci;
            if (_balonRt != null) _balonRt.localEulerAngles = new Vector3(0f, 0f, z);
            yield return null;
        }
        if (_balonRt != null) _balonRt.localEulerAngles = Vector3.zero;
    }

    private void UIYarat()
    {
        _root = new GameObject("AnlaticiHatirlatmaCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _root.transform.SetParent(transform, false);

        var canvas = _root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = CANVAS_SORTING_ORDER;
        _root.GetComponent<GraphicRaycaster>().enabled = false;

        var scaler = _root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var balon = new GameObject("Balon",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        balon.transform.SetParent(_root.transform, false);
        _balonRt = balon.GetComponent<RectTransform>();
        _balonRt.anchorMin = _balonRt.anchorMax = new Vector2(0.5f, 0f);
        _balonRt.pivot = new Vector2(0.5f, 0.5f);
        _balonRt.sizeDelta = new Vector2(480f, 64f);
        _balonRt.anchoredPosition = new Vector2(0f, 100f);
        balon.GetComponent<Image>().color = BALON_KOYU;

        BorderEkle(balon.transform, _balonRt.sizeDelta, 2f, ALTIN_ACIK);

        var msg = new GameObject("Mesaj", typeof(RectTransform), typeof(CanvasRenderer));
        msg.transform.SetParent(balon.transform, false);
        var msgRt = msg.GetComponent<RectTransform>();
        msgRt.anchorMin = Vector2.zero; msgRt.anchorMax = Vector2.one;
        msgRt.offsetMin = new Vector2(20f, 0f);
        msgRt.offsetMax = new Vector2(-20f, 0f);
        _mesajText = msg.AddComponent<TextMeshProUGUI>();
        _mesajText.alignment = TextAlignmentOptions.Center;
        _mesajText.fontSize = 18f;
        _mesajText.fontStyle = FontStyles.Bold;
        _mesajText.color = BEYAZ;
        _mesajText.text = "Hadi...";
        _mesajText.raycastTarget = false;
        _mesajText.enableWordWrapping = false;
        _mesajText.overflowMode = TextOverflowModes.Ellipsis;
    }

    private static void BorderEkle(Transform parent, Vector2 size, float kalinlik, Color renk)
    {
        (string ad, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta)[] kenarlar =
        {
            ("Ust",  new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, kalinlik)),
            ("Alt",  new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, kalinlik)),
            ("Sol",  new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(kalinlik, 0f)),
            ("Sag",  new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(kalinlik, 0f)),
        };
        foreach (var k in kenarlar)
        {
            var go = new GameObject("Border_" + k.ad,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = k.anchorMin;
            rt.anchorMax = k.anchorMax;
            rt.sizeDelta = k.sizeDelta;
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = renk;
            img.raycastTarget = false;
        }
    }
}
