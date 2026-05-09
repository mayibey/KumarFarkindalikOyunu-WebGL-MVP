using System;
using UnityEngine;

// BonusAyarlari - Bonus config (OY Sync okur) + bonus satın al paneli göster/gizle (eski BonusSatinAlUI)

public class BonusAyarlari : MonoBehaviour
{
    [Header("Bonus (Free Spin)")]
    public int BonusHakBaslangic = 10;
    public float BonusSpinBekleme = 0.70f;

    [Header("Bonus Satın Al")]
    public int BonusSatinAlCarpani = 100;

    [Header("Bonus Budget (Ödül Havuzu Koruma)")]
    public bool BonusBudgetAktif = false;
    [Range(0f, 1f)] public float BonusBudgetHavuzOran = 0.25f;
    public int BonusBudgetMinTL = 0;
    public int BonusBudgetMaxTL = 60000;

    [Header("Bonus Otomatik Zorluk")]
    public bool BonusOtoZorlukAktif = true;
    public int BonusMinCluster_Easy = 6;
    public int BonusMinCluster_Hard = 14;

    [Header("Scatter Ayarları (Normal / Bonus)")]
    [Range(0f, 1f)] public float ScatterChanceNormal = 0.005f;
    [Range(0f, 1f)] public float ScatterChanceBonus = 0.001f;
    public int ScatterEsik = 4;

    [Header("Scatter Efekt (opsiyonel)")]
    public float ScatterScaleUp = 1.6f;
    public float ScatterAnimDuration = 0.6f;

    void Awake()
    {
        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    /// <summary>Bonus satın al onay panelini aç (bileşen bu panelin üzerindeyse). Evet/Hayır OyunYoneticisi.BonusSatinAlOnayla / BonusSatinAlIptal ile bağlanır.</summary>
    public void Goster(int maliyet, int bakiye, Action onEvetCallback, Action onHayirCallback)
    {
        gameObject.SetActive(true);
    }

    public void Kapat()
    {
        gameObject.SetActive(false);
    }
}
