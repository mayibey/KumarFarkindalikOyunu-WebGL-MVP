using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Izgara ve sembol işlemleri için wrapper servis. XYToIndex, HucreSayisi, ScatterSay, ForceRefreshCarpanTextsFromGrid, RenderAllSprites burada; diğerleri delegasyon ile OyunYoneticisi'ne yönlendirilir.
/// </summary>
public class IzgaraServisi
{
    private const int CARPAN_SEMBOL = -2;
    private const int MIN_CLUSTER_SIZE = 8;
    // CELL SIZE INSPECTOR’DAN AYARLANIYOR (eski sabit: IzgaraHucreBoyutu = 130x130)
    // SLOTGRID BOYUTU INSPECTOR’DAN AYARLANIYOR (eski sabit: SlotGridBoyutu = 860x645)

    const string MeyveChildAdi = "Meyve";

    /// <summary>SlotGrid'in ilk N çocuğundan sembol <see cref="Image"/>; <see cref="MeyveChildAdi"/> child varsa o, yoksa kök.</summary>
    public static Image[] SlotGriddenMeyveImgeleriniAl(Transform slotGridRoot, int beklenenAdet)
    {
        if (slotGridRoot == null || beklenenAdet <= 0) return null;
        if (slotGridRoot.childCount < beklenenAdet) return null;
        var arr = new Image[beklenenAdet];
        for (int i = 0; i < beklenenAdet; i++)
        {
            Transform cell = slotGridRoot.GetChild(i);
            Transform meyveTr = cell.Find(MeyveChildAdi);
            arr[i] = meyveTr != null ? meyveTr.GetComponent<Image>() : cell.GetComponent<Image>();
        }
        return arr;
    }

    /// <summary>Kökte tek <see cref="Image"/> (meyve) varsa köke arka plan, meyveyi <see cref="MeyveChildAdi"/> child'a taşır. Zaten yapılandıysa sadece kök sprite'ı günceller.</summary>
    public static void MeyveHucrelerineArkaPlanUygula(Transform slotGridRoot, int beklenenAdet, Sprite arkaPlanSprite)
    {
        if (slotGridRoot == null || arkaPlanSprite == null || beklenenAdet <= 0) return;
        int n = Mathf.Min(beklenenAdet, slotGridRoot.childCount);
        for (int i = 0; i < n; i++)
        {
            Transform cell = slotGridRoot.GetChild(i);
            Transform meyveTr = cell.Find(MeyveChildAdi);
            if (meyveTr != null)
            {
                Image kokBg = cell.GetComponent<Image>();
                if (kokBg != null)
                {
                    kokBg.sprite = arkaPlanSprite;
                    kokBg.preserveAspect = false;
                }
                continue;
            }

            Image kokImg = cell.GetComponent<Image>();
            if (kokImg == null) continue;

            Sprite eskiSprite = kokImg.sprite;
            Color eskiRenk = kokImg.color;
            bool eskiPreserve = kokImg.preserveAspect;
            Material eskiMat = kokImg.material;
            bool eskiRaycast = kokImg.raycastTarget;

            kokImg.sprite = arkaPlanSprite;
            kokImg.color = Color.white;
            kokImg.preserveAspect = false;
            kokImg.type = Image.Type.Simple;
            kokImg.raycastTarget = false;

            GameObject meyveGo = new GameObject(MeyveChildAdi, typeof(RectTransform), typeof(Image));
            RectTransform mRt = meyveGo.GetComponent<RectTransform>();
            meyveGo.transform.SetParent(cell, false);
            mRt.anchorMin = Vector2.zero;
            mRt.anchorMax = Vector2.one;
            mRt.offsetMin = Vector2.zero;
            mRt.offsetMax = Vector2.zero;

            Image mImg = meyveGo.GetComponent<Image>();
            mImg.sprite = eskiSprite;
            mImg.color = eskiRenk;
            mImg.preserveAspect = eskiPreserve;
            if (eskiMat != null) mImg.material = eskiMat;
            mImg.raycastTarget = eskiRaycast;
        }
    }
    /// <summary>Normal oyunda bir spin'de en fazla bu kadar scatter yerleştirilir.</summary>
    private const int MAX_SCATTER_PER_SPIN_CAP = 5;

    private int _satir = 5;
    private int _sutun = 6;
    private int[,] _grid;
    private int _scatterSpriteIndex = 0;
    private int[,] _carpanDegerGrid;
    private int[] _carpanDegerByCellIndex;
    private TextMeshProUGUI[] _carpanHücreTextleri;
    private Image[] _hucreler;
    private List<Sprite> _sembolSpriteListesi;
    private Sprite _carpanSembolSprite;
    private int _carpanOverlayFontSize = 36;
    private Color _carpanYaziRengi = Color.white;
    private bool _carpanYaziKalin = true;
    private Color _carpanYaziDisCizgiRengi = new Color(0f, 0f, 0f, 1f);
    private float _carpanYaziDisCizgiKalinlik = 0.35f;
    private bool _carpanYaziGolge = true;
    private Color _carpanYaziGolgeRengi = new Color(0f, 0f, 0f, 0.85f);
    private Vector2 _carpanYaziGolgeOffset = new Vector2(2f, -2f);
    // Gradient
    private bool _carpanGradientAktif = true;
    private Color _carpanGradientUst = new Color(1f, 0.922f, 0.231f, 1f);
    private Color _carpanGradientAlt = new Color(1f, 0.596f, 0f, 1f);
    private float _carpanCharacterSpacing = -15f;
    // TMP Underlay (3D pop shadow)
    private bool _carpanUnderlayAktif = true;
    private Color _carpanUnderlayRengi = new Color(0.102f, 0.059f, 0.18f, 1f);
    private float _carpanUnderlayOffsetX = 2f;
    private float _carpanUnderlayOffsetY = -2f;
    private float _carpanUnderlayDilate = 0.2f;
    private float _carpanUnderlaySoftness = 0f;
    // TMP Glow
    private bool _carpanGlowAktif = true;
    private Color _carpanGlowRengi = new Color(1f, 0.922f, 0.231f, 0.6f);
    private float _carpanGlowOuter = 0.7f;
    private float _carpanGlowInner = 0f;
    private float _carpanGlowPower = 0.5f;
    private Material _carpanSharedMaterial;
    private Transform _slotGridRoot;
    private Vector2[] _cellPos;
    private RectTransform[] _cellRT;

    public virtual void SetSlotGridRoot(Transform t) => _slotGridRoot = t;
    public Vector2[] GetCellPos() => _cellPos;
    public RectTransform[] GetCellRT() => _cellRT;

    public virtual void SetGridDimensions(int satir, int sutun)
    {
        _satir = satir;
        _sutun = sutun;
    }

    public virtual void SetGrid(int[,] grid) => _grid = grid;
    public virtual void SetScatterSpriteIndex(int index) => _scatterSpriteIndex = index;
    public virtual int GetScatterSpriteIndex() => _scatterSpriteIndex;
    public virtual void SetCarpanDegerGrid(int[,] grid) => _carpanDegerGrid = grid;
    public virtual void SetCarpanDegerByCellIndex(int[] arr) => _carpanDegerByCellIndex = arr;
    public virtual void SetCarpanHücreTextleri(TextMeshProUGUI[] arr) => _carpanHücreTextleri = arr;
    public virtual void SetHucreler(Image[] arr) => _hucreler = arr;
    public virtual void SetSembolSpriteListesi(List<Sprite> list) => _sembolSpriteListesi = list;
    public virtual void SetCarpanSembolSprite(Sprite s) => _carpanSembolSprite = s;
    public virtual void SetCarpanOverlayFontSize(int size) => _carpanOverlayFontSize = Mathf.Max(1, size);
    public virtual void SetCarpanYaziRengi(Color c) => _carpanYaziRengi = c;
    public virtual void SetCarpanYaziKalin(bool b) => _carpanYaziKalin = b;
    public virtual void SetCarpanYaziDisCizgiRengi(Color c) => _carpanYaziDisCizgiRengi = c;
    public virtual void SetCarpanYaziDisCizgiKalinlik(float f) => _carpanYaziDisCizgiKalinlik = Mathf.Clamp01(f);
    public virtual void SetCarpanYaziGolge(bool b) => _carpanYaziGolge = b;
    public virtual void SetCarpanYaziGolgeRengi(Color c) => _carpanYaziGolgeRengi = c;
    public virtual void SetCarpanYaziGolgeOffset(Vector2 v) => _carpanYaziGolgeOffset = v;
    public virtual void SetCarpanGradient(bool aktif, Color ust, Color alt) { _carpanGradientAktif = aktif; _carpanGradientUst = ust; _carpanGradientAlt = alt; _carpanSharedMaterial = null; }
    public virtual void SetCarpanCharacterSpacing(float s) => _carpanCharacterSpacing = s;
    public virtual void SetCarpanUnderlay(bool aktif, Color rengi, float ox, float oy, float dilate, float softness) { _carpanUnderlayAktif = aktif; _carpanUnderlayRengi = rengi; _carpanUnderlayOffsetX = ox; _carpanUnderlayOffsetY = oy; _carpanUnderlayDilate = dilate; _carpanUnderlaySoftness = softness; _carpanSharedMaterial = null; }
    public virtual void SetCarpanGlow(bool aktif, Color rengi, float outer, float inner, float power) { _carpanGlowAktif = aktif; _carpanGlowRengi = rengi; _carpanGlowOuter = outer; _carpanGlowInner = inner; _carpanGlowPower = power; _carpanSharedMaterial = null; }
    public TextMeshProUGUI[] GetCarpanHücreTextleri() => _carpanHücreTextleri;

    private Func<float, float, float> _biasMultiplier;
    private Func<float> _getHardBias01;
    public virtual void SetBiasMultiplier(Func<float, float, float> fn) => _biasMultiplier = fn;
    public virtual void SetGetHardBias01(Func<float> fn) => _getHardBias01 = fn;

    private Func<bool, float> _getScatterChance;
    private Func<int> _getMaxScatterPerSpin;
    private int _scatterSayisiBuSpin;
    public virtual void SetGetScatterChance(Func<bool, float> fn) => _getScatterChance = fn;
    public virtual void SetGetMaxScatterPerSpin(Func<int> fn) => _getMaxScatterPerSpin = fn;
    public virtual void ResetScatterCountPerSpin() => _scatterSayisiBuSpin = 0;

    private Func<bool> _getBonusAktif;
    private Func<int, List<Vector2Int>> _findClustersToRemove;
    private Func<List<Vector2Int>, int> _calculateWinForRemoved;
    private Func<int, int> _getEffectiveFillLimit;
    public virtual void SetGetBonusAktif(Func<bool> fn) => _getBonusAktif = fn;
    public virtual void SetFindClustersToRemove(Func<int, List<Vector2Int>> fn) => _findClustersToRemove = fn;
    public virtual void SetCalculateWinForRemoved(Func<List<Vector2Int>, int> fn) => _calculateWinForRemoved = fn;
    public virtual void SetGetEffectiveFillLimit(Func<int, int> fn) => _getEffectiveFillLimit = fn;

    /// <summary>PayTable (8-9) referansı; yüksek ödemeli sembolün düşme ağırlığı azaltılır (ters ağırlık).</summary>
    private Func<float[]> _getPayTableBase;
    public virtual void SetGetPayTableBase(Func<float[]> fn) => _getPayTableBase = fn;

    /// <summary>RenderAllSprites ile aynı sıra: y*sutun + x (y=0 üst satır).</summary>
    public virtual int XYToIndex(int x, int y) => (y * _sutun) + x;

    public virtual int HucreSayisi() => _sutun * _satir;

    public virtual int ScatterSay()
    {
        return ScatterSay(_grid);
    }

    /// <summary>Verilen grid üzerinde scatter (silah) sayısını döner. Bonus kontrolü için mevcut grid açıkça verilebilir.</summary>
    public virtual int ScatterSay(int[,] gridToCount)
    {
        if (gridToCount == null) return 0;
        int sutun = gridToCount.GetLength(0);
        int satir = gridToCount.GetLength(1);
        int c = 0;
        for (int y = 0; y < satir; y++)
            for (int x = 0; x < sutun; x++)
                if (gridToCount[x, y] == _scatterSpriteIndex) c++;
        return c;
    }

    /// <summary>Çarpan hücre yazılarını grid ve carpan değerlerine göre günceller.</summary>
    public virtual void ForceRefreshCarpanTextsFromGrid()
    {
        if (_carpanHücreTextleri == null || _grid == null) return;
        int total = Mathf.Min(_carpanHücreTextleri.Length, HucreSayisi());
        for (int idx = 0; idx < total; idx++)
        {
            int x = idx % _sutun;
            int y = idx / _sutun;
            var tmp = _carpanHücreTextleri[idx];
            if (tmp == null) continue;

            if (_grid[x, y] == CARPAN_SEMBOL)
            {
                int v = (_carpanDegerGrid != null) ? _carpanDegerGrid[x, y] : 0;
                if (v <= 0 && _carpanDegerByCellIndex != null && idx < _carpanDegerByCellIndex.Length)
                {
                    v = _carpanDegerByCellIndex[idx];
                    if (v > 0 && _carpanDegerGrid != null) _carpanDegerGrid[x, y] = v;
                }
                if (v <= 0)
                {
                    int metinden = CarpanTextDegeriCoz(tmp.text);
                    if (metinden > 0)
                    {
                        v = metinden;
                        if (_carpanDegerGrid != null) _carpanDegerGrid[x, y] = v;
                        if (_carpanDegerByCellIndex != null && idx < _carpanDegerByCellIndex.Length)
                            _carpanDegerByCellIndex[idx] = v;
                    }
                }
                if (v > 0)
                {
                    tmp.text = "x" + v.ToString();
                    if (!tmp.gameObject.activeSelf) tmp.gameObject.SetActive(true);
                    var c = tmp.color;
                    c.a = 1f;
                    tmp.color = c;
                }
                else
                {
                    // Geçici senkron kaymalarında (özellikle force ilk drop) mevcut değeri koru.
                    if (!string.IsNullOrWhiteSpace(tmp.text) && tmp.text.StartsWith("x", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!tmp.gameObject.activeSelf) tmp.gameObject.SetActive(true);
                        var c = tmp.color;
                        c.a = 1f;
                        tmp.color = c;
                    }
                    else
                    {
                        tmp.text = "";
                        if (tmp.gameObject.activeSelf) tmp.gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                if (tmp.gameObject.activeSelf) tmp.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>Grid ve çarpan değerlerine göre tüm hücre sprite ve çarpan metinlerini günceller.</summary>
    public virtual void RenderAllSprites(bool setAlphaOne, bool resetScale)
    {
        if (_hucreler == null || _grid == null) return;
        int idx = 0;
        for (int y = 0; y < _satir; y++)
        {
            for (int x = 0; x < _sutun; x++)
            {
                var img = _hucreler[idx];
                int sym = _grid[x, y];

                img.color = Color.white;
                img.material = null;
                img.type = Image.Type.Simple;
                img.preserveAspect = true;

                if (sym == CARPAN_SEMBOL)
                {
                    img.sprite = _carpanSembolSprite != null ? _carpanSembolSprite : (_sembolSpriteListesi != null && _sembolSpriteListesi.Count > 0 ? _sembolSpriteListesi[0] : null);

                    if (_carpanHücreTextleri != null && idx < _carpanHücreTextleri.Length && _carpanHücreTextleri[idx] != null)
                    {
                        int v = (_carpanDegerGrid != null) ? _carpanDegerGrid[x, y] : 0;
                        if (v <= 0 && _carpanDegerByCellIndex != null && idx < _carpanDegerByCellIndex.Length)
                        {
                            v = _carpanDegerByCellIndex[idx];
                            if (v > 0 && _carpanDegerGrid != null) _carpanDegerGrid[x, y] = v;
                        }
                        if (v <= 0)
                        {
                            int metinden = CarpanTextDegeriCoz(_carpanHücreTextleri[idx].text);
                            if (metinden > 0)
                            {
                                v = metinden;
                                if (_carpanDegerGrid != null) _carpanDegerGrid[x, y] = v;
                                if (_carpanDegerByCellIndex != null && idx < _carpanDegerByCellIndex.Length)
                                    _carpanDegerByCellIndex[idx] = v;
                            }
                        }
                        if (v > 0)
                        {
                            _carpanHücreTextleri[idx].text = "x" + v.ToString();
                            _carpanHücreTextleri[idx].gameObject.SetActive(true);
                            var tc = _carpanHücreTextleri[idx].color;
                            tc.a = 1f;
                            _carpanHücreTextleri[idx].color = tc;
                        }
                        else
                        {
                            // Bomba görünüyorsa yazıyı bir karede düşürme; mevcut x değeri varsa koru.
                            var mevcut = _carpanHücreTextleri[idx].text;
                            bool korunacak = !string.IsNullOrWhiteSpace(mevcut) && mevcut.StartsWith("x", StringComparison.OrdinalIgnoreCase);
                            _carpanHücreTextleri[idx].gameObject.SetActive(korunacak);
                            if (!korunacak) _carpanHücreTextleri[idx].text = "";
                        }
                    }
                }
                else
                {
                    if (_sembolSpriteListesi != null && sym >= 0 && sym < _sembolSpriteListesi.Count)
                        img.sprite = _sembolSpriteListesi[sym];

                    if (_carpanHücreTextleri != null && idx < _carpanHücreTextleri.Length && _carpanHücreTextleri[idx] != null)
                    {
                        if (_carpanDegerGrid != null && _carpanDegerGrid[x, y] <= 0)
                            _carpanHücreTextleri[idx].gameObject.SetActive(false);
                    }
                }

                if (setAlphaOne)
                {
                    Color c = img.color;
                    c.a = 1f;
                    img.color = c;
                }

                if (resetScale)
                    img.rectTransform.localScale = Vector3.one;

                idx++;
            }
        }
    }

    private static int CarpanTextDegeriCoz(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        string t = text.Trim();
        if (t.Length < 2 || (t[0] != 'x' && t[0] != 'X')) return 0;
        if (int.TryParse(t.Substring(1), out int v) && v > 0)
            return v;
        return 0;
    }

    /// <summary>Sadece verilen hücrelerin sprite'ını günceller (tumble oynatmasında mevcut meyvelerin değişmemesi için).</summary>
    public virtual void RenderSpritesOnlyForCells(IEnumerable<Vector2Int> hucreler, int[,] grid)
    {
        if (_hucreler == null || grid == null) return;
        foreach (var p in hucreler)
        {
            int x = p.x, y = p.y;
            if (x < 0 || x >= _sutun || y < 0 || y >= _satir) continue;
            int idx = y * _sutun + x;
            if (idx >= _hucreler.Length) continue;
            var img = _hucreler[idx];
            if (img == null) continue;
            int sym = grid[x, y];
            img.color = Color.white;
            img.material = null;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;

            if (sym == CARPAN_SEMBOL)
            {
                img.sprite = _carpanSembolSprite != null
                    ? _carpanSembolSprite
                    : (_sembolSpriteListesi != null && _sembolSpriteListesi.Count > 0 ? _sembolSpriteListesi[0] : null);

                if (_carpanHücreTextleri != null && idx < _carpanHücreTextleri.Length && _carpanHücreTextleri[idx] != null)
                {
                    int v = (_carpanDegerGrid != null) ? _carpanDegerGrid[x, y] : 0;
                    if (v <= 0 && _carpanDegerByCellIndex != null && idx < _carpanDegerByCellIndex.Length)
                        v = _carpanDegerByCellIndex[idx];
                    if (v <= 0)
                        v = CarpanTextDegeriCoz(_carpanHücreTextleri[idx].text);

                    if (v > 0)
                    {
                        if (_carpanDegerGrid != null) _carpanDegerGrid[x, y] = v;
                        if (_carpanDegerByCellIndex != null && idx < _carpanDegerByCellIndex.Length)
                            _carpanDegerByCellIndex[idx] = v;

                        _carpanHücreTextleri[idx].text = "x" + v.ToString();
                        _carpanHücreTextleri[idx].gameObject.SetActive(true);
                        var tc = _carpanHücreTextleri[idx].color;
                        tc.a = 1f;
                        _carpanHücreTextleri[idx].color = tc;
                    }
                    else
                    {
                        _carpanHücreTextleri[idx].text = "";
                        _carpanHücreTextleri[idx].gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                if (_sembolSpriteListesi != null && sym >= 0 && sym < _sembolSpriteListesi.Count)
                    img.sprite = _sembolSpriteListesi[sym];

                if (_carpanHücreTextleri != null && idx < _carpanHücreTextleri.Length && _carpanHücreTextleri[idx] != null)
                    _carpanHücreTextleri[idx].gameObject.SetActive(false);
            }

            Color c = img.color;
            c.a = 1f;
            img.color = c;
            img.rectTransform.localScale = Vector3.one;
        }
    }

    /// <summary>Her hücre için CarpanText (TextMeshProUGUI) oluşturur veya mevcut olanı bulur; carpanHücreTextleri dizisini doldurur.</summary>
    public virtual void EnsureCarpanCellTexts()
    {
        if (_hucreler == null || _hucreler.Length == 0) return;
        if (_carpanHücreTextleri != null && _carpanHücreTextleri.Length == _hucreler.Length) return;

        _carpanHücreTextleri = new TextMeshProUGUI[_hucreler.Length];
        for (int i = 0; i < _hucreler.Length; i++)
        {
            var cell = _hucreler[i];
            if (cell == null) continue;

            var existing = cell.GetComponentInChildren<TextMeshProUGUI>(true);
            if (existing != null && existing.gameObject.name == "CarpanText")
            {
                _carpanHücreTextleri[i] = existing;
                continue;
            }

            GameObject go = new GameObject("CarpanText", typeof(RectTransform));
            go.transform.SetParent(cell.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            tmp.fontSize = _carpanOverlayFontSize;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = Mathf.Max(8, _carpanOverlayFontSize - 12);
            tmp.fontSizeMax = _carpanOverlayFontSize;
            if (_carpanYaziKalin) tmp.fontStyle |= FontStyles.Bold;
            tmp.characterSpacing = _carpanCharacterSpacing;
            tmp.extraPadding = true;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;

            // Vertex gradient (component-level, no material instance needed)
            if (_carpanGradientAktif)
            {
                tmp.color = Color.white;
                tmp.enableVertexGradient = true;
                tmp.colorGradient = new VertexGradient(
                    _carpanGradientUst, _carpanGradientUst,
                    _carpanGradientAlt, _carpanGradientAlt);
            }
            else
            {
                tmp.color = _carpanYaziRengi;
                tmp.enableVertexGradient = false;
            }

            // One shared material for all bomb texts (outline + underlay + glow baked in)
            if (_carpanSharedMaterial == null && tmp.fontSharedMaterial != null)
            {
                _carpanSharedMaterial = new Material(tmp.fontSharedMaterial);
                _carpanSharedMaterial.name = "BombaCarpanMat";
                _carpanSharedMaterial.SetColor("_OutlineColor", _carpanYaziDisCizgiRengi);
                _carpanSharedMaterial.SetFloat("_OutlineWidth", _carpanYaziDisCizgiKalinlik);
                if (_carpanUnderlayAktif)
                {
                    _carpanSharedMaterial.EnableKeyword("UNDERLAY_ON");
                    _carpanSharedMaterial.SetColor("_UnderlayColor", _carpanUnderlayRengi);
                    _carpanSharedMaterial.SetFloat("_UnderlayOffsetX", _carpanUnderlayOffsetX);
                    _carpanSharedMaterial.SetFloat("_UnderlayOffsetY", _carpanUnderlayOffsetY);
                    _carpanSharedMaterial.SetFloat("_UnderlayDilate", _carpanUnderlayDilate);
                    _carpanSharedMaterial.SetFloat("_UnderlaySoftness", _carpanUnderlaySoftness);
                }
                if (_carpanGlowAktif)
                {
                    _carpanSharedMaterial.EnableKeyword("GLOW_ON");
                    _carpanSharedMaterial.SetColor("_GlowColor", _carpanGlowRengi);
                    _carpanSharedMaterial.SetFloat("_GlowOuter", _carpanGlowOuter);
                    _carpanSharedMaterial.SetFloat("_GlowInner", _carpanGlowInner);
                    _carpanSharedMaterial.SetFloat("_GlowPower", _carpanGlowPower);
                }
            }
            if (_carpanSharedMaterial != null)
                tmp.fontSharedMaterial = _carpanSharedMaterial;

            // UI Shadow yalnızca TMP Underlay kapalıyken (çift gölgeden kaçın)
            var shadow = go.GetComponent<Shadow>();
            if (_carpanYaziGolge && !_carpanUnderlayAktif)
            {
                if (shadow == null) shadow = go.AddComponent<Shadow>();
                shadow.effectColor = _carpanYaziGolgeRengi;
                shadow.effectDistance = _carpanYaziGolgeOffset;
                shadow.useGraphicAlpha = true;
            }
            else
            {
                if (shadow != null) UnityEngine.Object.Destroy(shadow);
            }
            tmp.gameObject.SetActive(false);

            _carpanHücreTextleri[i] = tmp;
        }
    }

    /// <summary>Hücre pozisyonlarını önbelleğe alır, layout bileşenini kapatır.</summary>
    public virtual void CacheCellPositionsThenDisableLayout()
    {
        if (_hucreler == null || _hucreler.Length == 0) return;

        Behaviour layoutToDisable = null;
        if (_slotGridRoot != null)
        {
            var kokRt = _slotGridRoot as RectTransform;
            // kokRt.sizeDelta = SlotGridBoyutu; // SLOTGRID BOYUTU INSPECTOR'DAN AYARLANIYOR (eski değer: 860x645)

            var glg = _slotGridRoot.GetComponent<GridLayoutGroup>();
            if (glg != null)
            {
                layoutToDisable = glg;
                // glg.cellSize = IzgaraHucreBoyutu; // CELL SIZE INSPECTOR'DAN AYARLANIYOR (eski değer: 130x130)
                glg.enabled = true;
            }
            else
            {
                var hlg = _slotGridRoot.GetComponent<HorizontalLayoutGroup>();
                if (hlg != null) layoutToDisable = hlg;
                else
                {
                    var vlg = _slotGridRoot.GetComponent<VerticalLayoutGroup>();
                    if (vlg != null) layoutToDisable = vlg;
                }
            }

            if (kokRt != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(kokRt);
        }

        _cellPos = new Vector2[HucreSayisi()];
        _cellRT = new RectTransform[HucreSayisi()];

        for (int i = 0; i < HucreSayisi(); i++)
        {
            _cellRT[i] = _hucreler[i].rectTransform;
            _cellPos[i] = _cellRT[i].anchoredPosition;

            _cellRT[i].localScale = Vector3.one;
            Color c = _hucreler[i].color;
            c.a = 1f;
            _hucreler[i].color = c;
        }

        if (layoutToDisable != null)
            layoutToDisable.enabled = false;
    }

    /// <summary>Grid'de verilen sembol index'inden kaç adet olduğunu döner.</summary>
    public virtual int MevcutSembolAdedi(int[,] countGrid, int sembol)
    {
        if (countGrid == null) return 0;
        int sayac = 0;
        for (int y = 0; y < _satir; y++)
        {
            for (int x = 0; x < _sutun; x++)
            {
                if (countGrid[x, y] == sembol) sayac++;
            }
        }
        return sayac;
    }

    /// <summary>Scatter ve haricSembol hariç rastgele bir sembol indexi döner.</summary>
    public virtual int RandomNonScatterSymbolFarkli(int[,] countGrid, int haricSembol)
    {
        if (_sembolSpriteListesi == null || _sembolSpriteListesi.Count <= 1) return 0;

        int n = _sembolSpriteListesi.Count;
        var adaylar = new List<int>();

        for (int i = 0; i < n; i++)
        {
            if (i == _scatterSpriteIndex) continue;
            if (i == haricSembol) continue;
            adaylar.Add(i);
        }

        if (adaylar.Count == 0) return haricSembol;

        return adaylar[UnityEngine.Random.Range(0, adaylar.Count)];
    }

    /// <summary>Scatter ve çarpan hariç, ağırlıklı (bias) rastgele sembol indexi döner. maxPerSymbol >= 0 ise o sayıya ulaşan semboller seçilmez (limit 0 iken tumble olmasın diye).</summary>
    public virtual int RandomNonScatterSymbol(int[,] countGrid, int maxPerSymbol = -1)
    {
        if (_sembolSpriteListesi == null || _sembolSpriteListesi.Count <= 1) return 0;

        int n = _sembolSpriteListesi.Count;
        int[] counts = new int[n];
        if (countGrid != null)
        {
            for (int x = 0; x < _sutun; x++)
            {
                for (int y = 0; y < _satir; y++)
                {
                    int s = countGrid[x, y];
                    if (s < 0) continue;
                    if (s == _scatterSpriteIndex) continue;
                    if (s >= 0 && s < n) counts[s]++;
                }
            }
        }

        const float MIN_WEIGHT_FLOOR = 0.08f;
        float Bias(float easy, float hard) => _biasMultiplier != null ? _biasMultiplier(easy, hard) : 1f;

        int dominantIndex = -1;
        int dominantCount = -1;
        for (int i = 0; i < n; i++)
        {
            if (i == _scatterSpriteIndex) continue;
            if (counts[i] > dominantCount)
            {
                dominantCount = counts[i];
                dominantIndex = i;
            }
        }

        float totalW = 0f;
        float[] w = new float[n];
        for (int i = 0; i < n; i++)
        {
            if (i == _scatterSpriteIndex) { w[i] = 0f; continue; }
            if (maxPerSymbol >= 0 && counts[i] >= maxPerSymbol) { w[i] = 0f; continue; }

            float wi = 1f;
            int c = counts[i];

            if (i == dominantIndex && dominantCount >= 3)
                wi *= Bias(1.35f, 1.00f);

            if (c == MIN_CLUSTER_SIZE - 4)
                wi *= Bias(1.20f, 1.00f);
            else if (c == MIN_CLUSTER_SIZE - 3)
                wi *= Bias(1.60f, 1.00f);
            else if (c == MIN_CLUSTER_SIZE - 2)
                wi *= Bias(2.20f, 0.70f);
            else if (c >= MIN_CLUSTER_SIZE - 1)
                wi *= Bias(5.00f, 0.15f);
            // Zorluk 4 cluster dağılımı: 8-9 çoğunluk, 10-11 az, 12+ çok az
            if (c == MIN_CLUSTER_SIZE + 1)
                wi *= Bias(0.50f, 1f);
            else if (c >= 10 && c <= 11)
                wi *= Bias(0.35f, 1f);
            else if (c >= 12)
                wi *= Bias(0.15f, 1f);

            // Senaryoda sadece 6–7 adet (8'e tamamlanmak üzere) yumuşat; 5'i kesme (çok zayıf kazanç).
            if (SenaryoYoneticisi.I != null && (c == MIN_CLUSTER_SIZE - 2 || c == MIN_CLUSTER_SIZE - 1))
                wi *= 0.78f;

            // PayTable ters ağırlık: yüksek ödemeli sembol daha az düşsün
            if (_getPayTableBase != null)
            {
                float[] payTable = _getPayTableBase();
                if (payTable != null && i >= 0 && i < payTable.Length)
                {
                    float pay = Mathf.Max(0.01f, payTable[i]);
                    wi *= 1f / (1f + pay);
                }
            }

            wi = Mathf.Max(wi, MIN_WEIGHT_FLOOR);
            w[i] = wi;
            totalW += wi;
        }

        if (totalW <= 0f)
        {
            var adaylar = new List<int>();
            for (int i = 0; i < n; i++)
            {
                if (i == _scatterSpriteIndex) continue;
                if (maxPerSymbol >= 0 && counts[i] >= maxPerSymbol) continue;
                adaylar.Add(i);
            }
            if (adaylar.Count == 0)
            {
                int fallback = UnityEngine.Random.Range(0, n);
                if (fallback == _scatterSpriteIndex) fallback = (fallback + 1) % n;
                return fallback;
            }
            return adaylar[UnityEngine.Random.Range(0, adaylar.Count)];
        }

        float r = UnityEngine.Random.value * totalW;
        int picked = 0;
        for (int i = 0; i < n; i++)
        {
            r -= w[i];
            if (r <= 0f) { picked = i; break; }
        }

        int pickedCount = counts[picked];
        float hardBias01 = _getHardBias01 != null ? _getHardBias01() : 0f;
        if (pickedCount >= MIN_CLUSTER_SIZE - 1)
        {
            float rerollChance = Mathf.Lerp(0.00f, 0.80f, hardBias01);
            if (UnityEngine.Random.value < rerollChance)
            {
                float r2 = UnityEngine.Random.value * totalW;
                for (int i = 0; i < n; i++)
                {
                    r2 -= w[i];
                    if (r2 <= 0f) { picked = i; break; }
                }
            }
        }

        if (picked == _scatterSpriteIndex)
            picked = (picked + 1) % n;

        return picked;
    }

    /// <summary>Scatter şansı ve max scatter sınırına göre scatter veya normal sembol indexi döner. maxPerSymbol >= 0 ise normal sembollerde aynı sembolden en fazla bu kadar (limit 0 iken tumble olmasın).</summary>
    public virtual int RandomSymbolWithScatterChance(int[,] countGrid, bool bonusAktif, int maxPerSymbol = -1)
    {
        int maxScatter = MAX_SCATTER_PER_SPIN_CAP;
        if (_getMaxScatterPerSpin != null)
            maxScatter = Mathf.Min(MAX_SCATTER_PER_SPIN_CAP, _getMaxScatterPerSpin());
        if (_scatterSayisiBuSpin >= maxScatter)
            return RandomNonScatterSymbol(countGrid, maxPerSymbol);

        float chance = _getScatterChance != null ? _getScatterChance(bonusAktif) : 0f;
        if (chance <= 0.0001f)
            return RandomNonScatterSymbol(countGrid, maxPerSymbol);
        if (UnityEngine.Random.value < chance)
        {
            _scatterSayisiBuSpin++;
            return _scatterSpriteIndex;
        }
        return RandomNonScatterSymbol(countGrid, maxPerSymbol);
    }

    public virtual void FillRandomAll() => FillRandomAll(int.MaxValue);

    public virtual void FillRandomAll(int odenebilirLimit)
    {
        if (_grid == null || _carpanDegerGrid == null) return;

        bool bonusAktif = _getBonusAktif != null && _getBonusAktif();
        // Bakiye/havuz yokken (limit 0): 8+ aynı meyve gelmesin, tumble beklentisi oluşmasın.
        int maxPerSymbol = (odenebilirLimit <= 0) ? (MIN_CLUSTER_SIZE - 1) : -1;

        // Her hücrede scatter şansı slider'a göre (0 ise hiç scatter düşmez)
        for (int y = 0; y < _satir; y++)
        {
            for (int x = 0; x < _sutun; x++)
            {
                _grid[x, y] = RandomSymbolWithScatterChance(_grid, bonusAktif, maxPerSymbol);
                _carpanDegerGrid[x, y] = 0;
                int idx = XYToIndex(x, y);
                if (_carpanDegerByCellIndex != null && idx >= 0 && idx < _carpanDegerByCellIndex.Length)
                    _carpanDegerByCellIndex[idx] = 0;
            }
        }
    }
}
