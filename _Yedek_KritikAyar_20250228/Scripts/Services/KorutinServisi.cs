using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Named coroutine yönetimi: key ile başlat/durdur, bittiğinde sözlükten otomatik silinir.
/// </summary>
public class KorutinServisi
{
    private readonly Dictionary<string, Coroutine> _named = new Dictionary<string, Coroutine>();
    private Func<IEnumerator, Coroutine> _run;
    private Action<Coroutine> _stop;

    public void SetRunner(Func<IEnumerator, Coroutine> run, Action<Coroutine> stop)
    {
        _run = run;
        _stop = stop;
    }

    /// <summary> Aynı key varsa önce StopNamed, sonra routine'ı wrapper ile başlatıp sözlüğe yazar. Bittiğinde key otomatik silinir. </summary>
    public void StartNamed(string key, IEnumerator routine)
    {
        if (string.IsNullOrEmpty(key) || routine == null) return;
        StopNamed(key);
        Coroutine c = _run != null ? _run(WrapAndRemove(key, routine)) : null;
        if (c != null)
            _named[key] = c;
    }

    public void StopNamed(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        if (_named.TryGetValue(key, out Coroutine c) && c != null)
        {
            _stop?.Invoke(c);
        }
        _named.Remove(key);
    }

    public void StopAll()
    {
        foreach (var kv in _named)
        {
            if (kv.Value != null)
                _stop?.Invoke(kv.Value);
        }
        _named.Clear();
    }

    public bool IsRunning(string key)
    {
        return !string.IsNullOrEmpty(key) && _named.ContainsKey(key);
    }

    private IEnumerator WrapAndRemove(string key, IEnumerator inner)
    {
        yield return inner;
        _named.Remove(key);
    }
}
