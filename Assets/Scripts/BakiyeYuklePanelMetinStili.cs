using System.Collections;
using TMPro;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BakiyeYuklePanel içinde buton altı dışındaki TMP metinlere, paneldeki onay/iptal buton yazılarıyla uyumlu
/// kalın yüz, altın gradient, dış çizgi ve hafif gölge uygular. Panel pasif başladığı için Awake ilk açılışta tetiklenir.
/// </summary>
[DisallowMultipleComponent]
public class BakiyeYuklePanelMetinStili : MonoBehaviour
{
    static Sprite _webGuvenliVarsayilanSprite;
    [SerializeField] float disCizgiKalinlik = 0.22f;
    [SerializeField] Color disCizgiRenk = new Color(0.22f, 0.14f, 0.07f, 1f);
    [SerializeField] Color gradientUst = new Color(1f, 0.93f, 0.55f, 1f);
    [SerializeField] Color gradientAlt = new Color(0.85f, 0.68f, 0.28f, 1f);
    [SerializeField] bool golgeEkle = true;
    [SerializeField] Color golgeRenk = new Color(0f, 0f, 0f, 0.55f);
    [SerializeField] Vector2 golgeOffset = new Vector2(2f, -2f);
    [SerializeField] bool ornekButonYaziFontunuKullan = true;
    [SerializeField] bool metniOrtala = true;
    [SerializeField] string arkaPlanDosyaAdi = "panelarkaplan.png";
    [SerializeField] bool koseYumusatmaAktif = true;
    [SerializeField, Range(0.02f, 0.45f)] float koseYaricapOran = 0.17f;
    [SerializeField, Range(0.002f, 0.08f)] float koseGecisOran = 0.02f;
    [SerializeField] float acilisAnimSure = 0.85f;
    [SerializeField] float acilisBaslangicOlcek = 0.08f;
    [SerializeField] float panelHedefOlcek = 0.98f;
    [SerializeField] float bonusIcerikBoyutCarpani = 1.35f;
    [SerializeField] float bonusHoverTasmasiPayi = 1.15f;

    Coroutine _acilisAnimCoroutine;
    Coroutine _gecHizalamaCoroutine;
    RectTransform _bonusIcerikRect;
    Vector2 _bonusIcerikOrijinalSizeDelta;
    bool _bonusIcerikOrijinalKaydedildi;

    void Awake()
    {
        PanelArkaPlaniniUygula();
        Uygula();
        ButonIcindekiMetinleriHizalaVeGuncelle();
    }

    void OnEnable()
    {
        PanelArkaPlaniniUygula();
        BonusPaneliBakiyeYukleGibiDuzenle();
        Uygula();
        ParaCekPanelGirdiStiliUygula();
        ParaCekOnayButonunuYukleStilineCevir();
        ButonIcindekiMetinleriHizalaVeGuncelle();
        if (_gecHizalamaCoroutine != null)
            StopCoroutine(_gecHizalamaCoroutine);
        _gecHizalamaCoroutine = StartCoroutine(GecHizalamaUygula());
        AcilisAnimasyonunuBaslat();
    }

    void OnDisable()
    {
        if (_acilisAnimCoroutine != null)
        {
            StopCoroutine(_acilisAnimCoroutine);
            _acilisAnimCoroutine = null;
        }
        if (_gecHizalamaCoroutine != null)
        {
            StopCoroutine(_gecHizalamaCoroutine);
            _gecHizalamaCoroutine = null;
        }
    }

    void AcilisAnimasyonunuBaslat()
    {
        var rt = AnimasyonHedefRectiniBul();
        if (rt == null)
            return;

        if (_acilisAnimCoroutine != null)
            StopCoroutine(_acilisAnimCoroutine);

        _acilisAnimCoroutine = StartCoroutine(AcilisAnimasyonuCalistir(rt));
    }

    RectTransform AnimasyonHedefRectiniBul()
    {
        return transform as RectTransform;
    }

    IEnumerator AcilisAnimasyonuCalistir(RectTransform panelRect)
    {
        float sure = Mathf.Max(0.1f, acilisAnimSure);
        float baslangic = Mathf.Clamp(acilisBaslangicOlcek, 0.05f, 1f);
        float t = 0f;
        float hedef = Mathf.Clamp(panelHedefOlcek, 0.2f, 1.2f);
        Vector3 minOlcek = new Vector3(baslangic, baslangic, 1f);
        Vector3 tamOlcek = new Vector3(hedef, hedef, 1f);

        panelRect.localScale = minOlcek;

        while (t < sure)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / sure);
            // En küçük halden daha belirgin ve yumuşak büyüme (slow-ease-out).
            float eased = 1f - Mathf.Pow(1f - p, 3f);
            panelRect.localScale = Vector3.Lerp(minOlcek, tamOlcek, eased);
            yield return null;
        }

        panelRect.localScale = tamOlcek;
        ButonIcindekiMetinleriHizalaVeGuncelle();
        for (int i = 0; i < 2; i++)
        {
            yield return null;
            ButonIcindekiMetinleriHizalaVeGuncelle();
        }
        _acilisAnimCoroutine = null;
    }

    /// <summary>
    /// Uygula() buton altındaki TMP'leri bilerek atlıyor; açılış ölçek animasyonu da mesh'i erken hesaplayabiliyor.
    /// Metin kutusu tam butonu doldurur, ortalanır, satır kırılmaz; sabit punto ile tutulur (otomatik boyut yukarı kaydırabilir).
    /// </summary>
    void ButonIcindekiMetinleriHizalaVeGuncelle()
    {
        foreach (var btn in GetComponentsInChildren<Button>(true))
        {
            if (btn == null)
                continue;

            foreach (var tmp in btn.GetComponentsInChildren<TMP_Text>(true))
            {
                if (tmp == null)
                    continue;

                tmp.horizontalAlignment = HorizontalAlignmentOptions.Center;
                tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.overflowMode = TextOverflowModes.Ellipsis;
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = 18f;
                tmp.fontSizeMax = Mathf.Clamp(tmp.fontSize, 22f, 32f);
                tmp.margin = Vector4.zero;
                tmp.raycastTarget = false;

                var rt = tmp.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.offsetMin = new Vector2(6f, 4f);
                rt.offsetMax = new Vector2(-6f, -4f);
                tmp.ForceMeshUpdate(true);
            }
        }
    }

    IEnumerator GecHizalamaUygula()
    {
        // TMP/layout bazen panel açılışında bir-iki kare geç hesaplanıyor.
        for (int i = 0; i < 3; i++)
        {
            yield return null;
            ButonIcindekiMetinleriHizalaVeGuncelle();
        }
        _gecHizalamaCoroutine = null;
    }

    void PanelArkaPlaniniUygula()
    {
        if (string.IsNullOrWhiteSpace(arkaPlanDosyaAdi))
            return;

        var panelImage = GetComponent<Image>();
        if (panelImage == null)
            return;

        Sprite arkaPlanSprite = GuvenliArkaPlanSpriteYukle();
        if (arkaPlanSprite == null)
            arkaPlanSprite = WebIcinGuvenliVarsayilanSprite();

        Texture2D texture = arkaPlanSprite.texture;
        if (texture == null)
            texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

        if (koseYumusatmaAktif)
            KoseYumusatmaUygula(texture);

        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        var sprite = arkaPlanSprite;
        if (sprite == null)
            sprite = WebIcinGuvenliVarsayilanSprite();

        if (PanelBonusSatinAlOnayMi())
        {
            // Bonus panelde kök tam ekran overlay; görünmez kalmalı.
            var kok = panelImage.color;
            panelImage.color = new Color(kok.r, kok.g, kok.b, 0f);

            var kutu = BonusIcerikImageBul();
            if (kutu != null)
            {
                kutu.sprite = sprite;
                kutu.type = Image.Type.Simple;
                kutu.preserveAspect = false;
                kutu.color = Color.white;
                _bonusIcerikRect = kutu.transform as RectTransform;
                BonusIcerikKutusunuBuyut(_bonusIcerikRect);
            }
        }
        else
        {
            panelImage.sprite = sprite;
            panelImage.type = Image.Type.Simple;
            panelImage.preserveAspect = false;
            panelImage.color = Color.white;
        }
    }

    Sprite GuvenliArkaPlanSpriteYukle()
    {
        if (string.IsNullOrWhiteSpace(arkaPlanDosyaAdi))
            return null;

        string resourcesAdi = Path.GetFileNameWithoutExtension(arkaPlanDosyaAdi);
        if (!string.IsNullOrWhiteSpace(resourcesAdi))
        {
            var res = Resources.Load<Sprite>(resourcesAdi);
            if (res != null) return res;
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        string tamYol = Path.Combine(Application.dataPath, arkaPlanDosyaAdi);
        if (!File.Exists(tamYol))
            return null;
        byte[] bytes = File.ReadAllBytes(tamYol);
        if (bytes == null || bytes.Length == 0)
            return null;
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(bytes))
        {
            Destroy(texture);
            return null;
        }
        return Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f
        );
#else
        return null;
#endif
    }

    static Sprite WebIcinGuvenliVarsayilanSprite()
    {
        if (_webGuvenliVarsayilanSprite != null) return _webGuvenliVarsayilanSprite;
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.SetPixels(new[] { Color.black, Color.black, Color.black, Color.black });
        texture.Apply();
        _webGuvenliVarsayilanSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f
        );
        return _webGuvenliVarsayilanSprite;
    }

    void KoseYumusatmaUygula(Texture2D texture)
    {
        int w = texture.width;
        int h = texture.height;
        if (w <= 4 || h <= 4)
            return;

        float r = Mathf.Clamp01(koseYaricapOran) * Mathf.Min(w, h);
        float feather = Mathf.Max(1f, Mathf.Clamp01(koseGecisOran) * Mathf.Min(w, h));
        float halfW = w * 0.5f;
        float halfH = h * 0.5f;
        float innerX = Mathf.Max(0f, halfW - r);
        float innerY = Mathf.Max(0f, halfH - r);

        Color[] pix = texture.GetPixels();
        for (int y = 0; y < h; y++)
        {
            float py = (y + 0.5f) - halfH;
            float ay = Mathf.Abs(py) - innerY;
            float qy = ay > 0f ? ay : 0f;

            for (int x = 0; x < w; x++)
            {
                float px = (x + 0.5f) - halfW;
                float ax = Mathf.Abs(px) - innerX;
                float qx = ax > 0f ? ax : 0f;

                float dist = Mathf.Sqrt(qx * qx + qy * qy) - r;
                int idx = y * w + x;
                Color c = pix[idx];

                if (dist > feather)
                {
                    c.a = 0f;
                }
                else if (dist > 0f)
                {
                    float edge = 1f - (dist / feather);
                    c.a *= Mathf.Clamp01(edge);
                }

                pix[idx] = c;
            }
        }

        texture.SetPixels(pix);
        texture.Apply(false, false);
    }

    void Uygula()
    {
        TMP_Text ornekButonYazi = null;
        if (ornekButonYaziFontunuKullan)
        {
            foreach (var b in GetComponentsInChildren<Button>(true))
            {
                ornekButonYazi = b.GetComponentInChildren<TMP_Text>(true);
                if (ornekButonYazi != null)
                    break;
            }
        }

        foreach (var tmp in GetComponentsInChildren<TMP_Text>(true))
        {
            if (tmp.GetComponentInParent<Button>(true) != null)
                continue;

            if (ornekButonYazi != null && ornekButonYazi.font != null)
                tmp.font = ornekButonYazi.font;

            tmp.fontStyle = FontStyles.Bold;
            // Sahnedeki eski mavi/özel tint değerlerini sıfırlayıp tüm metinleri aynı tona getir.
            tmp.color = Color.white;
            tmp.enableVertexGradient = true;
            tmp.colorGradient = new VertexGradient(gradientUst, gradientUst, gradientAlt, gradientAlt);
            tmp.outlineWidth = disCizgiKalinlik;
            tmp.outlineColor = disCizgiRenk;

            if (metniOrtala)
                tmp.horizontalAlignment = HorizontalAlignmentOptions.Center;

            if (golgeEkle)
            {
                var shadow = tmp.GetComponent<Shadow>();
                if (shadow == null)
                    shadow = tmp.gameObject.AddComponent<Shadow>();
                shadow.effectColor = golgeRenk;
                shadow.effectDistance = golgeOffset;
                shadow.useGraphicAlpha = true;
            }
        }
    }

    void BonusPaneliBakiyeYukleGibiDuzenle()
    {
        if (!PanelBonusSatinAlOnayMi())
            return;
        // Kök panel overlay görünmez, iç kutu görünür olacak.
        var kokImage = GetComponent<Image>();
        if (kokImage != null)
        {
            var c = kokImage.color;
            kokImage.color = new Color(c.r, c.g, c.b, 0f);
            kokImage.raycastTarget = true;
        }

        BonusIcerikKutusuSinirlariniYenidenHesapla();
    }

    Image BonusIcerikImageBul()
    {
        foreach (var img in GetComponentsInChildren<Image>(true))
        {
            if (img == null || img.gameObject == gameObject)
                continue;
            if (img.transform.parent != transform)
                continue;
            if (img.GetComponentInParent<Button>(true) != null)
                continue;
            return img;
        }
        return null;
    }

    void BonusIcerikKutusunuBuyut(RectTransform kutuRect)
    {
        if (kutuRect == null)
            return;

        if (!_bonusIcerikOrijinalKaydedildi)
        {
            _bonusIcerikOrijinalSizeDelta = kutuRect.sizeDelta;
            _bonusIcerikOrijinalKaydedildi = true;
        }

        float carp = Mathf.Clamp(bonusIcerikBoyutCarpani, 0.8f, 2f);
        kutuRect.sizeDelta = _bonusIcerikOrijinalSizeDelta * carp;
    }

    void BonusIcerikKutusuSinirlariniYenidenHesapla()
    {
        if (!PanelBonusSatinAlOnayMi())
            return;

        var kutuImage = BonusIcerikImageBul();
        if (kutuImage == null)
            return;
        var kutuRect = kutuImage.transform as RectTransform;
        if (kutuRect == null)
            return;

        RectTransform kokRect = transform as RectTransform;
        if (kokRect == null)
            return;

        bool ilk = true;
        float minX = 0f, minY = 0f, maxX = 0f, maxY = 0f;

        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i) as RectTransform;
            if (child == null || child == kutuRect)
                continue;
            if (!child.gameObject.activeInHierarchy)
                continue;

            Bounds b = RectTransformUtility.CalculateRelativeRectTransformBounds(kokRect, child);
            if (ilk)
            {
                minX = b.min.x; minY = b.min.y; maxX = b.max.x; maxY = b.max.y;
                ilk = false;
            }
            else
            {
                if (b.min.x < minX) minX = b.min.x;
                if (b.min.y < minY) minY = b.min.y;
                if (b.max.x > maxX) maxX = b.max.x;
                if (b.max.y > maxY) maxY = b.max.y;
            }
        }

        if (ilk)
            return;

        float genislik = Mathf.Max(10f, maxX - minX);
        float yukseklik = Mathf.Max(10f, maxY - minY);
        float merkezX = (minX + maxX) * 0.5f;
        float merkezY = (minY + maxY) * 0.5f;

        float carp = Mathf.Clamp(bonusIcerikBoyutCarpani * bonusHoverTasmasiPayi, 1f, 3f);
        kutuRect.anchoredPosition = new Vector2(merkezX, merkezY);
        kutuRect.sizeDelta = new Vector2(genislik * carp, yukseklik * carp);
    }

    void ParaCekOnayButonunuYukleStilineCevir()
    {
        if (!PanelParaCekMi())
            return;

        Button kaynakButon = YukleOnayButonunuBul();
        Button hedefButon = ParaCekOnayButonunuBul();
        if (kaynakButon == null || hedefButon == null)
            return;

        hedefButon.transition = kaynakButon.transition;
        hedefButon.colors = kaynakButon.colors;
        hedefButon.spriteState = kaynakButon.spriteState;
        hedefButon.animationTriggers = kaynakButon.animationTriggers;

        var kaynakGorsel = kaynakButon.targetGraphic as Image;
        var hedefGorsel = hedefButon.targetGraphic as Image;
        if (kaynakGorsel != null && hedefGorsel != null)
        {
            // YÜKLE butonundaki yazı sprite içinde gömülü olabileceği için sprite kopyalamıyoruz.
            // Sadece stil tonlarını kopyalayıp hedef butonun kendi görselini koruyoruz.
            hedefGorsel.color = kaynakGorsel.color;
            hedefGorsel.material = kaynakGorsel.material;
        }

        var kaynakYazi = kaynakButon.GetComponentInChildren<TMP_Text>(true);
        var hedefYazi = hedefButon.GetComponentInChildren<TMP_Text>(true);
        if (kaynakYazi != null && hedefYazi != null)
        {
            hedefYazi.font = kaynakYazi.font;
            hedefYazi.fontStyle = kaynakYazi.fontStyle;
            hedefYazi.fontSize = kaynakYazi.fontSize;
            hedefYazi.enableAutoSizing = kaynakYazi.enableAutoSizing;
            hedefYazi.fontSizeMin = kaynakYazi.fontSizeMin;
            hedefYazi.fontSizeMax = kaynakYazi.fontSizeMax;
            hedefYazi.color = kaynakYazi.color;
            hedefYazi.enableVertexGradient = kaynakYazi.enableVertexGradient;
            hedefYazi.colorGradient = kaynakYazi.colorGradient;
            hedefYazi.outlineWidth = kaynakYazi.outlineWidth;
            hedefYazi.outlineColor = kaynakYazi.outlineColor;
            hedefYazi.alignment = kaynakYazi.alignment;
            hedefYazi.text = "ÇEK";
        }
    }

    void ParaCekPanelGirdiStiliUygula()
    {
        if (!PanelParaCekMi())
            return;

        foreach (var input in GetComponentsInChildren<TMP_InputField>(true))
        {
            if (input == null)
                continue;

            var yazi = input.textComponent;
            if (yazi != null)
            {
                yazi.fontStyle = FontStyles.Bold;
                yazi.enableAutoSizing = false;
                yazi.fontSize = 35f;
                yazi.color = Color.black;
                yazi.enableVertexGradient = false;
                yazi.outlineWidth = 0f;
            }

            input.contentType = TMP_InputField.ContentType.IntegerNumber;
            input.characterValidation = TMP_InputField.CharacterValidation.Integer;
            input.text = SadeceRakam(input.text);
            input.onValidateInput = (string _, int __, char eklenen) => (eklenen >= '0' && eklenen <= '9') ? eklenen : '\0';

            var placeholder = input.placeholder as TMP_Text;
            if (placeholder != null)
            {
                placeholder.text = "Miktar girin";
                placeholder.fontStyle = FontStyles.Bold;
                placeholder.enableAutoSizing = false;
                placeholder.fontSize = 40f;
                placeholder.color = Color.black;
                placeholder.enableVertexGradient = false;
                placeholder.outlineWidth = 0.15f;
                placeholder.outlineColor = new Color(0.2f, 0.12f, 0.05f, 0.85f);
            }
        }
    }

    static string SadeceRakam(string deger)
    {
        if (string.IsNullOrEmpty(deger))
            return string.Empty;

        var sb = new System.Text.StringBuilder(deger.Length);
        for (int i = 0; i < deger.Length; i++)
        {
            char ch = deger[i];
            if (ch >= '0' && ch <= '9')
                sb.Append(ch);
        }
        return sb.ToString();
    }

    bool PanelParaCekMi()
    {
        return gameObject.name.ToLowerInvariant().Contains("paracek");
    }

    bool PanelBonusSatinAlOnayMi()
    {
        string ad = gameObject.name.ToLowerInvariant();
        return ad.Contains("bonusbuyconfirmpanel") || ad.Contains("bonussatinalonaypanel");
    }

    Button ParaCekOnayButonunuBul()
    {
        foreach (var b in GetComponentsInChildren<Button>(true))
        {
            if (b == null || b.gameObject == null) continue;
            string ad = b.gameObject.name.ToLowerInvariant();
            if (ad.Contains("iptal") || ad.Contains("kapat") || ad == "x")
                continue;
            if (ad.Contains("onay") || ad.Contains("cek") || ad.Contains("confirm") || ad.Contains("ok"))
                return b;
        }
        return null;
    }

    Button YukleOnayButonunuBul()
    {
        Transform kok = transform.root;
        if (kok == null) return null;

        foreach (var b in kok.GetComponentsInChildren<Button>(true))
        {
            if (b == null || b.gameObject == null) continue;
            Transform p = b.transform.parent;
            while (p != null && !p.name.ToLowerInvariant().Contains("bakiyeyuklepanel"))
                p = p.parent;
            if (p == null) continue;

            string ad = b.gameObject.name.ToLowerInvariant();
            if (ad.Contains("onay") || ad.Contains("yukle") || ad.Contains("confirm") || ad.Contains("ok"))
                return b;
        }
        return null;
    }
}
