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
    private Transform _slotGridRoot;
    private Vector2[] _cellPos;
    private RectTransform[] _cellRT;

    public void SetSlotGridRoot(Transform t) => _slotGridRoot = t;
    public Vector2[] GetCellPos() => _cellPos;
    public RectTransform[] GetCellRT() => _cellRT;

    public void SetGridDimensions(int satir, int sutun)
    {
        _satir = satir;
        _sutun = sutun;
    }

    public void SetGrid(int[,] grid) => _grid = grid;
    public void SetScatterSpriteIndex(int index) => _scatterSpriteIndex = index;
    public int GetScatterSpriteIndex() => _scatterSpriteIndex;
    public void SetCarpanDegerGrid(int[,] grid) => _carpanDegerGrid = grid;
    public void SetCarpanDegerByCellIndex(int[] arr) => _carpanDegerByCellIndex = arr;
    public void SetCarpanHücreTextleri(TextMeshProUGUI[] arr) => _carpanHücreTextleri = arr;
    public void SetHucreler(Image[] arr) => _hucreler = arr;
    public void SetSembolSpriteListesi(List<Sprite> list) => _sembolSpriteListesi = list;
    public void SetCarpanSembolSprite(Sprite s) => _carpanSembolSprite = s;
    public void SetCarpanOverlayFontSize(int size) => _carpanOverlayFontSize = Mathf.Max(1, size);
    public void SetCarpanYaziRengi(Color c) => _carpanYaziRengi = c;
    public void SetCarpanYaziKalin(bool b) => _carpanYaziKalin = b;
    public void SetCarpanYaziDisCizgiRengi(Color c) => _carpanYaziDisCizgiRengi = c;
    public void SetCarpanYaziDisCizgiKalinlik(float f) => _carpanYaziDisCizgiKalinlik = Mathf.Clamp01(f);
    public void SetCarpanYaziGolge(bool b) => _carpanYaziGolge = b;
    public void SetCarpanYaziGolgeRengi(Color c) => _carpanYaziGolgeRengi = c;
    public void SetCarpanYaziGolgeOffset(Vector2 v) => _carpanYaziGolgeOffset = v;
    public TextMeshProUGUI[] GetCarpanHücreTextleri() => _carpanHücreTextleri;

    private Func<float, float, float> _biasMultiplier;
    private Func<float> _getHardBias01;
    public void SetBiasMultiplier(Func<float, float, float> fn) => _biasMultiplier = fn;
    public void SetGetHardBias01(Func<float> fn) => _getHardBias01 = fn;

    private Func<bool, float> _getScatterChance;
    private Func<int> _getMaxScatterPerSpin;
    private int _scatterSayisiBuSpin;
    public void SetGetScatterChance(Func<bool, float> fn) => _getScatterChance = fn;
    public void SetGetMaxScatterPerSpin(Func<int> fn) => _getMaxScatterPerSpin = fn;
    public void ResetScatterCountPerSpin() => _scatterSayisiBuSpin = 0;

    private Func<bool> _getBonusAktif;
    private Func<int, List<Vector2Int>> _findClustersToRemove;
    private Func<List<Vector2Int>, int> _calculateWinForRemoved;
    private Func<int, int> _getEffectiveFillLimit;
    public void SetGetBonusAktif(Func<bool> fn) => _getBonusAktif = fn;
    public void SetFindClustersToRemove(Func<int, List<Vector2Int>> fn) => _findClustersToRemove = fn;
    public void SetCalculateWinForRemoved(Func<List<Vector2Int>, int> fn) => _calculateWinForRemoved = fn;
    public void SetGetEffectiveFillLimit(Func<int, int> fn) => _getEffectiveFillLimit = fn;

    /// <summary>RenderAllSprites ile aynı sıra: y*sutun + x (y=0 üst satır).</summary>
    public int XYToIndex(int x, int y) => (y * _sutun) + x;

    public int HucreSayisi() => _sutun * _satir;

    public int ScatterSay()
    {
        return ScatterSay(_grid);
    }

    /// <summary>Verilen grid üzerinde scatter (silah) sayısını döner. Bonus kontrolü için mevcut grid açıkça verilebilir.</summary>
    public int ScatterSay(int[,] gridToCount)
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
    public void ForceRefreshCarpanTextsFromGrid()
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
                if (v > 0)
                {
                    tmp.text = "x" + v.ToString();
                    if (!tmp.gameObject.activeSelf) tmp.gameObject.SetActive(true);
                }
                else
                {
                    tmp.text = "";
                    if (tmp.gameObject.activeSelf) tmp.gameObject.SetActive(false);
                }
            }
            else
            {
                if (tmp.gameObject.activeSelf) tmp.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>Grid ve çarpan değerlerine göre tüm hücre sprite ve çarpan metinlerini günceller.</summary>
    public void RenderAllSprites(bool setAlphaOne, bool resetScale)
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
                        _carpanHücreTextleri[idx].text = v > 0 ? ("x" + v.ToString()) : "";
                        _carpanHücreTextleri[idx].gameObject.SetActive(v > 0);
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

    /// <summary>Her hücre için CarpanText (TextMeshProUGUI) oluşturur veya mevcut olanı bulur; carpanHücreTextleri dizisini doldurur.</summary>
    public void EnsureCarpanCellTexts()
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
            tmp.color = _carpanYaziRengi;
            if (_carpanYaziKalin) tmp.fontStyle |= FontStyles.Bold;
            tmp.outlineColor = _carpanYaziDisCizgiRengi;
            tmp.outlineWidth = _carpanYaziDisCizgiKalinlik;
            tmp.extraPadding = true;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;

            var shadow = go.GetComponent<Shadow>();
            if (_carpanYaziGolge)
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
    public void CacheCellPositionsThenDisableLayout()
    {
        if (_hucreler == null || _hucreler.Length == 0) return;

        Behaviour layoutToDisable = null;
        if (_slotGridRoot != null)
        {
            var glg = _slotGridRoot.GetComponent<GridLayoutGroup>();
            if (glg != null) layoutToDisable = glg;
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
    public int MevcutSembolAdedi(int[,] countGrid, int sembol)
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
    public int RandomNonScatterSymbolFarkli(int[,] countGrid, int haricSembol)
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

    /// <summary>Scatter ve çarpan hariç, ağırlıklı (bias) rastgele sembol indexi döner.</summary>
    public int RandomNonScatterSymbol(int[,] countGrid)
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
                wi *= Bias(5.00f, 0.25f);

            wi = Mathf.Max(wi, MIN_WEIGHT_FLOOR);
            w[i] = wi;
            totalW += wi;
        }

        if (totalW <= 0f)
        {
            int fallback = UnityEngine.Random.Range(0, n);
            if (fallback == _scatterSpriteIndex) fallback = (fallback + 1) % n;
            return fallback;
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
            float rerollChance = Mathf.Lerp(0.00f, 0.65f, hardBias01);
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

    /// <summary>Scatter şansı ve max scatter sınırına göre scatter veya normal sembol indexi döner; scatter üretilirse sayacı artırır. Normal oyunda en fazla 5 scatter.</summary>
    public int RandomSymbolWithScatterChance(int[,] countGrid, bool bonusAktif)
    {
        int maxScatter = MAX_SCATTER_PER_SPIN_CAP;
        if (_getMaxScatterPerSpin != null)
            maxScatter = Mathf.Min(MAX_SCATTER_PER_SPIN_CAP, _getMaxScatterPerSpin());
        if (_scatterSayisiBuSpin >= maxScatter)
            return RandomNonScatterSymbol(countGrid);

        float chance = _getScatterChance != null ? _getScatterChance(bonusAktif) : 0f;
        if (chance <= 0f)
            return RandomNonScatterSymbol(countGrid);
        if (UnityEngine.Random.value < chance)
        {
            _scatterSayisiBuSpin++;
            return _scatterSpriteIndex;
        }
        return RandomNonScatterSymbol(countGrid);
    }

    public void FillRandomAll() => FillRandomAll(int.MaxValue);

    public void FillRandomAll(int odenebilirLimit)
    {
        if (_grid == null || _carpanDegerGrid == null) return;

        int kalanLimitTL = _getEffectiveFillLimit != null ? _getEffectiveFillLimit(odenebilirLimit) : odenebilirLimit;
        bool bonusAktif = _getBonusAktif != null && _getBonusAktif();

        // Her hücrede scatter şansı slider'a göre (0 ise hiç scatter düşmez)
        for (int y = 0; y < _satir; y++)
        {
            for (int x = 0; x < _sutun; x++)
            {
                _grid[x, y] = RandomSymbolWithScatterChance(_grid, bonusAktif);
                _carpanDegerGrid[x, y] = 0;
                int idx = XYToIndex(x, y);
                if (_carpanDegerByCellIndex != null && idx >= 0 && idx < _carpanDegerByCellIndex.Length)
                    _carpanDegerByCellIndex[idx] = 0;
            }
        }

        if (kalanLimitTL == int.MaxValue) return;
        if (_findClustersToRemove == null || _calculateWinForRemoved == null) return;

        List<Vector2Int> toRemove = _findClustersToRemove(MIN_CLUSTER_SIZE);
        if (toRemove == null || toRemove.Count == 0) return;

        int hamKazanc = _calculateWinForRemoved(toRemove);
        if (hamKazanc > kalanLimitTL)
        {
            UnityEngine.Debug.Log($"[FILL] Ödeme={hamKazanc} > Limit={kalanLimitTL} - Tümleme engelliyorum!");
            for (int y = 0; y < _satir; y++)
            {
                for (int x = 0; x < _sutun; x++)
                {
                    int secilecek = RandomNonScatterSymbol(_grid);
                    int mevcutSayi = MevcutSembolAdedi(_grid, secilecek);
                    if (mevcutSayi >= MIN_CLUSTER_SIZE - 1)
                        secilecek = RandomNonScatterSymbolFarkli(_grid, secilecek);
                    _grid[x, y] = secilecek;
                }
            }
            UnityEngine.Debug.Log("[FILL] Tümleme engellendi - kullanıcı 8 meyve görmeyecek!");
        }
        else
        {
            UnityEngine.Debug.Log($"[FILL] Ödeme={hamKazanc} <= Limit={kalanLimitTL} - Tümleme izin ver!");
        }
    }
}
