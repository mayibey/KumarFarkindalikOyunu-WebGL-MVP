using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tumble (patlatma, düşme, refill) işlemleri için wrapper servis. Asıl mantık OyunYoneticisi'nde kalır; delegasyon ile çağrılır.
/// </summary>
public class TumbleServisi
{
    private const int MIN_CLUSTER_SIZE = 8;

    private Func<Action<int>, IEnumerator> _tumbleLoopImpl;
    private Func<IEnumerator> _collapseRefillAndAnimateImpl;
    private Func<List<Vector2Int>, IEnumerator> _animatePopImpl;

    private Func<int> _getCurrentBet;
    private Func<List<Vector2Int>, int> _calculateWithPayTable;
    private int[,] _grid;
    private Func<bool> _getBonusAktif;
    private Func<int> _getBonusRemainingPayableTL;
    private int _scatterSpriteIndex;

    public void SetTumbleLoopImpl(Func<Action<int>, IEnumerator> impl) => _tumbleLoopImpl = impl;
    public void SetCollapseRefillAndAnimateImpl(Func<IEnumerator> impl) => _collapseRefillAndAnimateImpl = impl;
    public void SetAnimatePopImpl(Func<List<Vector2Int>, IEnumerator> impl) => _animatePopImpl = impl;
    public void SetGetCurrentBet(Func<int> fn) => _getCurrentBet = fn;
    public void SetCalculateWithPayTable(Func<List<Vector2Int>, int> fn) => _calculateWithPayTable = fn;
    public void SetGrid(int[,] grid) => _grid = grid;
    public void SetGetBonusAktif(Func<bool> fn) => _getBonusAktif = fn;
    public void SetGetBonusRemainingPayableTL(Func<int> fn) => _getBonusRemainingPayableTL = fn;
    public void SetScatterSpriteIndex(int index) => _scatterSpriteIndex = index;

    public virtual IEnumerator TumbleLoop(Action<int> onKazanc) => _tumbleLoopImpl != null ? _tumbleLoopImpl(onKazanc) : null;

    public virtual List<Vector2Int> FindClustersToRemove(int minSize)
    {
        if (_grid == null) return new List<Vector2Int>();
        // Bonus limit 0 olsa bile kümeleri bul; patlama/görsel tutarlılık için tumble hep oynatılır, ödeme ayrı kırpılır.

        int scatterIdx = _scatterSpriteIndex;
        int sutun = _grid.GetLength(0);
        int satir = _grid.GetLength(1);
        var bySymbol = new Dictionary<int, List<Vector2Int>>();

        for (int x = 0; x < sutun; x++)
        {
            for (int y = 0; y < satir; y++)
            {
                int sym = _grid[x, y];
                if (sym < 0) continue;
                if (sym == scatterIdx) continue;
                if (!bySymbol.ContainsKey(sym)) bySymbol[sym] = new List<Vector2Int>();
                bySymbol[sym].Add(new Vector2Int(x, y));
            }
        }

#if UNITY_EDITOR
        if (!_findClustersDebugLoggedOnce)
        {
            _findClustersDebugLoggedOnce = true;
            var sb = new System.Text.StringBuilder();
            sb.Append($"[TumbleServisi] scatterSpriteIndex={scatterIdx}, minSize={minSize}. Grid symbols (sym->count): ");
            foreach (var kv in bySymbol)
                sb.Append($"{kv.Key}->{kv.Value.Count} ");
            UnityEngine.Debug.Log(sb.ToString());
        }
#endif

        var toRemove = new List<Vector2Int>();
        foreach (var kv in bySymbol)
        {
            if (kv.Value.Count >= minSize)
                toRemove.AddRange(kv.Value);
        }
        return toRemove;
    }

#if UNITY_EDITOR
    private static bool _findClustersDebugLoggedOnce;
#endif

    public virtual int CalculateWinForRemoved(List<Vector2Int> removed)
    {
        if (removed == null) return 0;
        if (_calculateWithPayTable != null)
        {
            int pay = _calculateWithPayTable(removed);
            if (pay >= 0) return pay;
        }
        if (_grid == null) return 0;
        int bahis = _getCurrentBet != null ? _getCurrentBet() : 0;
        var counts = new Dictionary<int, int>();
        for (int i = 0; i < removed.Count; i++)
        {
            int sym = _grid[removed[i].x, removed[i].y];
            if (sym < 0) continue;
            if (!counts.ContainsKey(sym)) counts[sym] = 0;
            counts[sym]++;
        }
        float total = 0f;
        foreach (var kv in counts)
        {
            int count = kv.Value;
            if (count < MIN_CLUSTER_SIZE) continue;
            total += count * 10f * bahis;
        }
        return Mathf.RoundToInt(total);
    }

    public virtual IEnumerator CollapseRefillAndAnimate() => _collapseRefillAndAnimateImpl != null ? _collapseRefillAndAnimateImpl() : null;
    public virtual IEnumerator AnimatePop(List<Vector2Int> cells) => _animatePopImpl != null ? _animatePopImpl(cells) : null;
}
