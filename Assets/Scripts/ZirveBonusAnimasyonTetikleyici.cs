using UnityEngine;

/// <summary>
/// Senaryo 5 zirve bonusu (3. yükleme sonrası ilk bonus, 50 spin, maliyet x2.5) başladığında
/// ve bittiğinde tetiklenir. Inspector'dan animasyon/Animator atayıp bu script ile otomatik oynatırsın.
/// </summary>
public class ZirveBonusAnimasyonTetikleyici : MonoBehaviour
{
    [Header("Oyun Yöneticisi")]
    [Tooltip("Boş bırakırsan sahnede bulunur.")]
    public OyunYoneticisi oyunYoneticisi;

    [Header("Zirve bonusu başladığında (50 spin giriş)")]
    [Tooltip("İsteğe bağlı: bu Animation bileşeni oynatılır.")]
    public Animation zirveBasladiAnimasyon;
    [Tooltip("Animator kullanıyorsan bu trigger tetiklenir.")]
    public Animator zirveBasladiAnimator;
    public string zirveBasladiTrigger = "ZirveBasladi";

    [Header("Zirve bonusu bittiğinde (yüksek kazanç ekranı)")]
    [Tooltip("İsteğe bağlı: bu Animation bileşeni oynatılır.")]
    public Animation zirveBittiAnimasyon;
    [Tooltip("Animator kullanıyorsan bu trigger tetiklenir.")]
    public Animator zirveBittiAnimator;
    public string zirveBittiTrigger = "ZirveBitti";

    private void OnEnable()
    {
        OyunYoneticisi oyun = oyunYoneticisi != null ? oyunYoneticisi : FindFirstObjectByType<OyunYoneticisi>(FindObjectsInactive.Include);
        if (oyun != null)
        {
            oyun.OnZirveBonusBasladi += ZirveBasladi;
            oyun.OnZirveBonusBitti += ZirveBitti;
        }
    }

    private void OnDisable()
    {
        OyunYoneticisi oyun = oyunYoneticisi != null ? oyunYoneticisi : FindFirstObjectByType<OyunYoneticisi>(FindObjectsInactive.Include);
        if (oyun != null)
        {
            oyun.OnZirveBonusBasladi -= ZirveBasladi;
            oyun.OnZirveBonusBitti -= ZirveBitti;
        }
    }

    private void ZirveBasladi()
    {
        if (zirveBasladiAnimasyon != null && zirveBasladiAnimasyon.clip != null)
            zirveBasladiAnimasyon.Play();
        if (zirveBasladiAnimator != null && !string.IsNullOrEmpty(zirveBasladiTrigger))
            zirveBasladiAnimator.SetTrigger(zirveBasladiTrigger);
    }

    private void ZirveBitti(int kazancTL)
    {
        if (zirveBittiAnimasyon != null && zirveBittiAnimasyon.clip != null)
            zirveBittiAnimasyon.Play();
        if (zirveBittiAnimator != null && !string.IsNullOrEmpty(zirveBittiTrigger))
            zirveBittiAnimator.SetTrigger(zirveBittiTrigger);
    }
}
