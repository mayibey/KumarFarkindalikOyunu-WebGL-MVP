using UnityEngine;

/// <summary>
/// UI Image için yuvarlatılmış dikdörtgen çerçeve (içi şeffaf) sprite üretir; 9-slice ile esnetilir.
/// </summary>
public static class YuvarlakUICerceveSprite
{
    private static Sprite _onbellek;
    private static Sprite _doluMaskeOnbellek;
    private static int _geometriSurumKayit = -1;
    /// <summary>Kenar kalınlığı / inset değişince artır; önbellekteki eski sprite’lar temizlenir.</summary>
    private const int GeometriSurum = 3;

    private static void GeometriGerekirseYenile()
    {
        if (_geometriSurumKayit == GeometriSurum) return;
        _onbellek = null;
        _doluMaskeOnbellek = null;
        _geometriSurumKayit = GeometriSurum;
    }

    /// <summary>Mask için: yuvarlatılmış dikdörtgen dolgu (içi opak beyaz, dışı şeffaf); içerik bu şekle kırpılır.</summary>
    public static Sprite AlDoluYuvarlakMaskeSprite()
    {
        GeometriGerekirseYenile();
        if (_doluMaskeOnbellek != null) return _doluMaskeOnbellek;
        const int boyut = 128;
        // Çerçeve inceldikçe maske köşeleri halka iç ağzına yakın dursun.
        const int yaricap = 24;
        var tex = new Texture2D(boyut, boyut, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Color32 sifir = new Color32(0, 0, 0, 0);
        Color32 dolgu = new Color32(255, 255, 255, 255);
        for (int y = 0; y < boyut; y++)
        {
            for (int x = 0; x < boyut; x++)
            {
                bool ic = YuvarlakDikdortgenIcinde(x, y, boyut, boyut, yaricap);
                tex.SetPixel(x, y, ic ? dolgu : sifir);
            }
        }
        tex.Apply(false, true);
        float b = Mathf.Clamp(yaricap * 0.9f, 16f, 28f);
        _doluMaskeOnbellek = Sprite.Create(tex, new Rect(0, 0, boyut, boyut), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        _doluMaskeOnbellek.name = "YuvarlakUIDoluMaske_Runtime";
        return _doluMaskeOnbellek;
    }

    public static Sprite AlVeyaOlustur()
    {
        GeometriGerekirseYenile();
        if (_onbellek != null) return _onbellek;
        const int boyut = 128;
        const int yaricap = 26;
        const int kalinlik = 6;
        var tex = new Texture2D(boyut, boyut, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Color32 sifir = new Color32(0, 0, 0, 0);
        Color32 kenar = new Color32(255, 220, 120, 255);
        for (int y = 0; y < boyut; y++)
        {
            for (int x = 0; x < boyut; x++)
            {
                bool dis = YuvarlakDikdortgenIcinde(x, y, boyut, boyut, yaricap);
                bool ic = YuvarlakDikdortgenIcinde(x, y, boyut - 2 * kalinlik, boyut - 2 * kalinlik, Mathf.Max(4, yaricap - kalinlik), kalinlik, kalinlik);
                tex.SetPixel(x, y, dis && !ic ? kenar : sifir);
            }
        }
        tex.Apply(false, true);
        float b = kalinlik;
        _onbellek = Sprite.Create(tex, new Rect(0, 0, boyut, boyut), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        _onbellek.name = "YuvarlakUICerceve_Runtime";
        return _onbellek;
    }

    private static bool YuvarlakDikdortgenIcinde(int px, int py, int w, int h, int r, int ox = 0, int oy = 0)
    {
        float x = px - ox;
        float y = py - oy;
        float rw = w;
        float rh = h;
        if (r <= 0) return x >= 0 && x < rw && y >= 0 && y < rh;
        r = Mathf.Min(r, Mathf.Min((int)(rw * 0.5f), (int)(rh * 0.5f)));
        if (x < 0 || y < 0 || x >= rw || y >= rh) return false;
        if (x >= r && x < rw - r) return true;
        if (y >= r && y < rh - r) return true;
        Vector2 c1 = new Vector2(r, r);
        Vector2 c2 = new Vector2(rw - r, r);
        Vector2 c3 = new Vector2(r, rh - r);
        Vector2 c4 = new Vector2(rw - r, rh - r);
        if (x < r && y < r) return Vector2.Distance(new Vector2(x, y), c1) <= r + 0.5f;
        if (x >= rw - r && y < r) return Vector2.Distance(new Vector2(x, y), c2) <= r + 0.5f;
        if (x < r && y >= rh - r) return Vector2.Distance(new Vector2(x, y), c3) <= r + 0.5f;
        if (x >= rw - r && y >= rh - r) return Vector2.Distance(new Vector2(x, y), c4) <= r + 0.5f;
        return true;
    }
}
