using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tüm sahnelerde ortak arka plan görselini uygular.
/// Resources/arkaplan sprite'ını, sahnedeki en olası arka plan Image'ına atar.
/// </summary>
public class OrtakArkaPlanUygulayici : MonoBehaviour
{
    private static Sprite _webGuvenliVarsayilanSprite;
    private static Sprite _ortakSprite;
    private static bool _dinleyiciEklendi;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Baslat()
    {
        if (_dinleyiciEklendi) return;
        _dinleyiciEklendi = true;
        SceneManager.sceneLoaded += SahneYuklendi;
        SahneyeUygula();
    }

    private static void SahneYuklendi(Scene scene, LoadSceneMode mode)
    {
        SahneyeUygula();
    }

    private static void SahneyeUygula()
    {
        // 01_GirisScene manuel olarak tasarlanıyor (Logo + özel arka plan); ortak sprite ezmesin.
        string aktifSahneAdi = SceneManager.GetActiveScene().name;
        if (aktifSahneAdi == "01_GirisScene") return;

        if (_ortakSprite == null)
            _ortakSprite = Resources.Load<Sprite>("arkaplan");
        if (_ortakSprite == null)
            _ortakSprite = DosyadanYukle("Resources/arkaplan.png");
        if (_ortakSprite == null)
            _ortakSprite = DosyadanYukle("Gorseller/arkaplan.png");
        if (_ortakSprite == null) return;

        Image hedef = ArkaPlanImageBul();
        if (hedef == null) return;
        if (OrtakArkaPlanaDokunulmamali(hedef)) return;

        hedef.sprite = _ortakSprite;
        hedef.type = Image.Type.Simple;
        hedef.preserveAspect = false;
        hedef.color = Color.white;
    }

    private static Image ArkaPlanImageBul()
    {
        // Admin sahnesinde doğrudan ana canvas arka planını hedefle.
        string sn = SceneManager.GetActiveScene().name;
        if (sn == "04_AdminOyunScene")
        {
            Image adminArkaPlan = AdminCanvasArkaPlanImageBul();
            if (adminArkaPlan != null) return adminArkaPlan;
        }

        // Sadece aktif objeleri al; kapalı panel/overlay'lere yanlışlıkla uygulama.
        Image[] tumImg = Object.FindObjectsByType<Image>(FindObjectsSortMode.None);
        if (tumImg == null || tumImg.Length == 0) return null;

        Canvas aktifCanvas = null;
        if (tumImg[0] != null)
            aktifCanvas = tumImg[0].GetComponentInParent<Canvas>();
        float canvasAlan = 1f;
        if (aktifCanvas != null)
        {
            var rt = aktifCanvas.transform as RectTransform;
            if (rt != null)
                canvasAlan = Mathf.Max(1f, Mathf.Abs(rt.rect.width * rt.rect.height));
        }

        // Önce isimden yüksek olasılıklı arka planları dene.
        Image adAdayi = null;
        float adAdayiAlan = -1f;
        for (int i = 0; i < tumImg.Length; i++)
        {
            Image img = tumImg[i];
            if (img == null || img.rectTransform == null) continue;
            if (OrtakArkaPlanaDokunulmamali(img)) continue;
            if (PanelBenzeriAtaVarMi(img.transform)) continue;
            string n = (img.gameObject.name ?? "").ToLowerInvariant();
            if (!n.Contains("arka") && !n.Contains("background") && !n.Contains("fon") && !n.Contains("bg"))
                continue;
            float alan = Mathf.Abs(img.rectTransform.rect.width * img.rectTransform.rect.height);
            // Çok küçük "Background" gibi ikonları ele.
            if (alan < canvasAlan * 0.15f) continue;
            if (alan > adAdayiAlan)
            {
                adAdayiAlan = alan;
                adAdayi = img;
            }
        }
        if (adAdayi != null) return adAdayi;

        // İsimden bulunamazsa en büyük Image'ı fallback seç.
        Image enBuyuk = null;
        float enBuyukAlan = -1f;
        for (int i = 0; i < tumImg.Length; i++)
        {
            Image img = tumImg[i];
            if (img == null || img.rectTransform == null) continue;
            if (OrtakArkaPlanaDokunulmamali(img)) continue;
            if (PanelBenzeriAtaVarMi(img.transform)) continue;
            string n = (img.gameObject.name ?? "").ToLowerInvariant();
            if (n.Contains("panel") || n.Contains("popup") || n.Contains("dim") || n.Contains("logo")) continue;
            float alan = Mathf.Abs(img.rectTransform.rect.width * img.rectTransform.rect.height);
            if (alan > enBuyukAlan)
            {
                enBuyukAlan = alan;
                enBuyuk = img;
            }
        }
        return enBuyuk;
    }

    /// <summary>İzgara / meyve alanı için özel atanmış arka plan; ortak Resources/arkaplan ile ezilmemeli.</summary>
    private static bool OrtakArkaPlanaDokunulmamali(Image img)
    {
        if (img == null) return true;
        if (img.GetComponent<SahneArkaPlaniManuel>() != null) return true;
        string ad = img.gameObject.name ?? string.Empty;
        if (ad.IndexOf("meyvelerarkaplan", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        return false;
    }

    private static Image AdminCanvasArkaPlanImageBul()
    {
        Canvas[] canvaslar = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        if (canvaslar == null || canvaslar.Length == 0) return null;

        Canvas anaCanvas = null;
        for (int i = 0; i < canvaslar.Length; i++)
        {
            Canvas c = canvaslar[i];
            if (c == null || !c.gameObject.activeInHierarchy) continue;
            if (c.name == "Canvas")
            {
                anaCanvas = c;
                break;
            }
        }
        if (anaCanvas == null) return null;

        RectTransform canvasRt = anaCanvas.transform as RectTransform;
        float canvasAlan = 1f;
        if (canvasRt != null)
            canvasAlan = Mathf.Max(1f, Mathf.Abs(canvasRt.rect.width * canvasRt.rect.height));

        Image[] tumImg = anaCanvas.GetComponentsInChildren<Image>(true);
        Image enUygun = null;
        float enIyiAlan = -1f;
        for (int i = 0; i < tumImg.Length; i++)
        {
            Image img = tumImg[i];
            if (img == null || img.rectTransform == null) continue;
            if (!img.gameObject.activeInHierarchy) continue;
            if (OrtakArkaPlanaDokunulmamali(img)) continue;
            if (img.transform.parent != anaCanvas.transform) continue; // sadece canvasın birinci seviye çocukları

            string n = (img.gameObject.name ?? "").ToLowerInvariant();
            if (!n.Contains("arka") && !n.Contains("background") && !n.Contains("bg")) continue;

            float alan = Mathf.Abs(img.rectTransform.rect.width * img.rectTransform.rect.height);
            if (alan < canvasAlan * 0.30f) continue;

            if (alan > enIyiAlan)
            {
                enIyiAlan = alan;
                enUygun = img;
            }
        }

        return enUygun;
    }

    private static bool PanelBenzeriAtaVarMi(Transform t)
    {
        Transform cur = t;
        while (cur != null)
        {
            string n = (cur.name ?? "").ToLowerInvariant();
            if (n.Contains("panel") || n.Contains("popup") || n.Contains("dim") || n.Contains("dialog"))
                return true;
            cur = cur.parent;
        }
        return false;
    }

    private static Sprite DosyadanYukle(string relativeFromAssets)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        string ad = string.IsNullOrWhiteSpace(relativeFromAssets) ? "" : Path.GetFileNameWithoutExtension(relativeFromAssets);
        if (!string.IsNullOrWhiteSpace(ad))
        {
            Sprite kaynak = Resources.Load<Sprite>(ad);
            if (kaynak != null) return kaynak;
        }
        return WebIcinGuvenliVarsayilanSprite();
#else
        string path = Path.Combine(Application.dataPath, relativeFromAssets);
        if (!File.Exists(path)) return null;

        byte[] bytes = File.ReadAllBytes(path);
        if (bytes == null || bytes.Length == 0) return null;

        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(bytes)) return null;

        return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
#endif
    }

    private static Sprite WebIcinGuvenliVarsayilanSprite()
    {
        if (_webGuvenliVarsayilanSprite != null) return _webGuvenliVarsayilanSprite;
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.SetPixels(new[] { Color.black, Color.black, Color.black, Color.black });
        tex.Apply();
        _webGuvenliVarsayilanSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        return _webGuvenliVarsayilanSprite;
    }
}
