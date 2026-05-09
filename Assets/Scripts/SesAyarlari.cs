using UnityEngine;

public class SesAyarlari : MonoBehaviour
{
    [Header("Tumble Ses")]
    public AudioSource TumbleSfxSource;
    public AudioClip TumblePopClip;
    public AudioClip TumbleDropClip;
    public float TumblePopMinInterval = 0.06f;
    public float TumbleDropMinInterval = 0.12f;

    [Header("Bonus Bitiş Ses / Müzik")]
    public AudioSource BonusEndMusicAudio;
    public AudioSource BonusEndSfxSource;
    public AudioClip BonusEndApplauseClip;

    [Header("Normal Oyun Müzik")]
    public AudioSource NormalOyunMusic;

    [Header("Scatter / Zil")]
    public AudioSource BonusBellAudio;
}
