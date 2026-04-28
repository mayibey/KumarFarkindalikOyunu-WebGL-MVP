using System;
using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Bonus UI akışı: başlangıç/bitiş paneli (fade, TMP, ses) + satın alma onay paneli (göster/gizle, Evet/Hayır).
/// Eski BonusUIAkisServisi + BonusSatinAlmaAkisServisi birleşik.
/// </summary>
public class BonusUIServisi
{
    // --- Bonus başlangıç/bitiş ---
    private GameObject _bonusStartPanel;
    private AudioSource _bonusBellAudio;
    private CanvasGroup _bonusStartCanvasGroup;
    private TMP_Text _bonusStartTMP;
    private Func<int> _getBonusHakKalan;
    private float _bonusStartFadeTime;
    private float _bonusStartShowTime;

    private GameObject _bonusEndPanel;
    private Func<bool> _getBonusEndCloseRequested;
    private Action<bool> _setBonusEndCloseRequested;
    private AudioSource _bonusEndSfxSource;
    private AudioClip _bonusEndApplauseClip;
    private CanvasGroup _bonusEndCanvasGroup;
    private TMP_Text _bonusEndTitleTMP;
    private TMP_Text _bonusEndWinTMP;
    private Func<int, string> _formatTL;
    private AudioSource _bonusEndMusicAudio;

    // --- Bonus satın al onay ---
    private int _pendingCost;
    private Func<int> _getBakiye;
    private Func<int> _getBonusMaliyeti;
    private Func<bool> _getSpinCalisiyor;
    private Func<bool> _getBonusAktif;
    private Action<int> _showConfirmPanel;
    private Action _hideConfirmPanel;
    private Action<int> _onConfirmed;
    private Action<string> _setUyariText;

    // --- Setters: başlangıç/bitiş ---
    public void SetBonusStartPanel(GameObject panel) => _bonusStartPanel = panel;
    public void SetBonusBellAudio(AudioSource audio) => _bonusBellAudio = audio;
    public void SetBonusStartCanvasGroup(CanvasGroup cg) => _bonusStartCanvasGroup = cg;
    public void SetBonusStartTMP(TMP_Text tmp) => _bonusStartTMP = tmp;
    public void SetGetBonusHakKalan(Func<int> getter) => _getBonusHakKalan = getter;
    public void SetBonusStartFadeTime(float t) => _bonusStartFadeTime = t;
    public void SetBonusStartShowTime(float t) => _bonusStartShowTime = t;
    public void SetBonusEndPanel(GameObject panel) => _bonusEndPanel = panel;
    public void SetGetBonusEndCloseRequested(Func<bool> getter) => _getBonusEndCloseRequested = getter;
    public void SetSetBonusEndCloseRequested(Action<bool> setter) => _setBonusEndCloseRequested = setter;
    public void SetBonusEndSfx(AudioSource source, AudioClip clip) { _bonusEndSfxSource = source; _bonusEndApplauseClip = clip; }
    public void SetBonusEndCanvasGroup(CanvasGroup cg) => _bonusEndCanvasGroup = cg;
    public void SetBonusEndTitleTMP(TMP_Text tmp) => _bonusEndTitleTMP = tmp;
    public void SetBonusEndWinTMP(TMP_Text tmp) => _bonusEndWinTMP = tmp;
    public void SetFormatTL(Func<int, string> fn) => _formatTL = fn;
    public void SetBonusEndMusicAudio(AudioSource audio) => _bonusEndMusicAudio = audio;

    // --- Setters: satın al onay ---
    public void SetGetBakiye(Func<int> fn) => _getBakiye = fn;
    public void SetGetBonusMaliyeti(Func<int> fn) => _getBonusMaliyeti = fn;
    public void SetGetSpinCalisiyor(Func<bool> fn) => _getSpinCalisiyor = fn;
    public void SetGetBonusAktif(Func<bool> fn) => _getBonusAktif = fn;
    public void SetShowConfirmPanel(Action<int> fn) => _showConfirmPanel = fn;
    public void SetHideConfirmPanel(Action fn) => _hideConfirmPanel = fn;
    public void SetOnConfirmed(Action<int> fn) => _onConfirmed = fn;
    public void SetSetUyariText(Action<string> fn) => _setUyariText = fn;

    // --- Bonus başlangıç/bitiş ---
    public IEnumerator ShowBonusStartMessage()
    {
        if (_bonusStartPanel == null) yield break;
        _bonusStartPanel.SetActive(true);
        if (_bonusBellAudio != null && _bonusBellAudio.clip != null)
            _bonusBellAudio.PlayOneShot(_bonusBellAudio.clip);
        else
            Debug.LogWarning("🔕 BONUS START: bonusBellAudio veya clip BOŞ!");
        if (_bonusStartCanvasGroup != null) _bonusStartCanvasGroup.alpha = 0f;
        int hak = _getBonusHakKalan != null ? _getBonusHakKalan() : 0;
        if (_bonusStartTMP != null) _bonusStartTMP.text = $"BONUS OYUN BAŞLIYOR!\n({hak} Hak)";
        if (_bonusStartCanvasGroup != null)
        {
            float t = 0f;
            while (t < _bonusStartFadeTime) { t += Time.deltaTime; _bonusStartCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t / _bonusStartFadeTime); yield return null; }
            _bonusStartCanvasGroup.alpha = 1f;
        }
        float beklemeSuresi = _bonusStartShowTime;
        if (_bonusBellAudio != null && _bonusBellAudio.clip != null) beklemeSuresi = _bonusBellAudio.clip.length;
        yield return new WaitForSecondsRealtime(beklemeSuresi);
        if (_bonusStartCanvasGroup != null)
        {
            float t = 0f;
            while (t < _bonusStartFadeTime) { t += Time.deltaTime; _bonusStartCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t / _bonusStartFadeTime); yield return null; }
            _bonusStartCanvasGroup.alpha = 0f;
        }
        _bonusStartPanel.SetActive(false);
    }

    public IEnumerator ShowBonusEndMessage(int bonusToplamKazanc)
    {
        if (_bonusEndPanel == null) yield break;
        _setBonusEndCloseRequested?.Invoke(false);
        _bonusEndPanel.SetActive(true);
        if (_bonusEndSfxSource != null && _bonusEndApplauseClip != null)
            _bonusEndSfxSource.PlayOneShot(_bonusEndApplauseClip);
        else
            Debug.LogWarning("👏 Bonus End: bonusEndSfxSource veya bonusEndApplauseClip boş!");
        if (_bonusEndCanvasGroup != null) _bonusEndCanvasGroup.alpha = 1f;
        if (_bonusEndTitleTMP != null) _bonusEndTitleTMP.text = "BONUS OYUN BİTTİ";
        if (_bonusEndWinTMP != null)
            _bonusEndWinTMP.text = $"Toplam Kazanç\n{(_formatTL != null ? _formatTL(bonusToplamKazanc) : bonusToplamKazanc.ToString())}";
        if (_getBonusEndCloseRequested != null)
            yield return new WaitUntil(() => _getBonusEndCloseRequested());
        if (_bonusEndMusicAudio != null) _bonusEndMusicAudio.Stop();
        _bonusEndPanel.SetActive(false);
    }

    // --- Bonus satın al onay ---
    public void BonusSatinAlRequested()
    {
        if (_getSpinCalisiyor != null && _getSpinCalisiyor()) return;
        if (_getBonusAktif != null && _getBonusAktif()) return;
        int cost = _getBonusMaliyeti != null ? _getBonusMaliyeti() : 0;
        int bakiye = _getBakiye != null ? _getBakiye() : 0;
        if (bakiye < cost) { _setUyariText?.Invoke($"Yetersiz bakiye. Maliyet: {cost} TL"); return; }
        _pendingCost = cost;
        _showConfirmPanel?.Invoke(cost);
    }

    public void ShowBonusBuyConfirmPanel(int cost) { _pendingCost = cost; _showConfirmPanel?.Invoke(cost); }
    public void HideBonusBuyConfirmPanel() { _hideConfirmPanel?.Invoke(); _pendingCost = 0; }

    public void OnYes()
    {
        if (_pendingCost <= 0) return;
        int cost = _pendingCost;
        _hideConfirmPanel?.Invoke();
        _pendingCost = 0;
        if (_getBakiye != null && _getBakiye() < cost) { _setUyariText?.Invoke($"Yetersiz bakiye. Maliyet: {cost} TL"); return; }
        _onConfirmed?.Invoke(cost);
    }

    public void OnNo() { _hideConfirmPanel?.Invoke(); _pendingCost = 0; }
    public int GetPendingCost() => _pendingCost;
    public void ClearPendingCost() => _pendingCost = 0;
}
