using System;
using UnityEngine;

/// <summary>
/// Bonus yavaş mod (hız override) ve tumble SFX spam önleme.
/// </summary>
public class HizVeSesServisi
{
    private float _origPop, _origFall, _origBetween, _origBonusWait;
    private bool _cached;

    private Func<bool> _getBonusYavasMod;
    private Func<(float pop, float fall, float between, float bonusWait)> _getDurations;
    private Action<float, float, float, float> _setDurations;
    private Func<(float pop, float fall, float between, float bonusWait)> _getBonusSpeedOverrides;
    private Func<float> _getUnscaledTime;
    private AudioSource _tumbleSfxSource;

    public void SetGetBonusYavasMod(Func<bool> fn) => _getBonusYavasMod = fn;
    public void SetGetDurations(Func<(float pop, float fall, float between, float bonusWait)> fn) => _getDurations = fn;
    public void SetSetDurations(Action<float, float, float, float> fn) => _setDurations = fn;
    public void SetGetBonusSpeedOverrides(Func<(float pop, float fall, float between, float bonusWait)> fn) => _getBonusSpeedOverrides = fn;
    public void SetGetUnscaledTime(Func<float> fn) => _getUnscaledTime = fn;
    public void SetAudioSource(AudioSource source) => _tumbleSfxSource = source;

    public void ApplyBonusSpeedIfNeeded()
    {
        if (_getBonusYavasMod == null || !_getBonusYavasMod()) return;

        if (_getDurations != null && _setDurations != null && _getBonusSpeedOverrides != null)
        {
            if (!_cached)
            {
                var d = _getDurations();
                _origPop = d.pop;
                _origFall = d.fall;
                _origBetween = d.between;
                _origBonusWait = d.bonusWait;
                _cached = true;
            }
            var ov = _getBonusSpeedOverrides();
            _setDurations(ov.pop, ov.fall, ov.between, ov.bonusWait);
        }
    }

    public void RestoreNormalSpeed()
    {
        if (_getBonusYavasMod == null || !_getBonusYavasMod()) return;

        if (_setDurations != null && _cached)
        {
            _setDurations(_origPop, _origFall, _origBetween, _origBonusWait);
        }
    }

    public void PlayTumbleSfx(AudioClip clip, ref float lastTime, float minInterval, float volume = 1f)
    {
        if (_tumbleSfxSource == null || clip == null) return;

        float now = _getUnscaledTime != null ? _getUnscaledTime() : Time.unscaledTime;
        if (now - lastTime < minInterval) return;
        lastTime = now;

        _tumbleSfxSource.PlayOneShot(clip, volume);
    }
}
