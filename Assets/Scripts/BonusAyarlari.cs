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
    [Tooltip("Açılacak onay paneli. Boş bırakırsan bu bileşenin olduğu GameObject kullanılır.")]
    public GameObject gosterilecekPanel;

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
        GameObject panel = PanelObjeyiAl();
        if (panel != null && panel.activeSelf)
            panel.SetActive(false);
    }

    /// <summary>Açılacak panel: Inspector'da atanmışsa o, yoksa sahnede "BonusBuyConfirmPanel" veya "BonusSatinAlOnayPanel" adlı obje (kapalı olsa da bulunur).</summary>
    private GameObject PanelObjeyiAl()
    {
        if (gosterilecekPanel != null) return gosterilecekPanel;
        var root = transform.root;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.gameObject.name == "BonusBuyConfirmPanel" || t.gameObject.name == "BonusSatinAlOnayPanel")
                return t.gameObject;
        }
        Debug.LogWarning("[BonusAyarlari] Bonus onay paneli bulunamadı. 'gosterilecekPanel' atayın veya panel adını BonusBuyConfirmPanel/BonusSatinAlOnayPanel yapın.");
        return null;
    }

    /// <summary>Bonus satın al onay panelini aç. Üst zincir açılır, alpha=1, panel en öne alınır.</summary>
    public void Goster(int maliyet, int bakiye, Action onEvetCallback, Action onHayirCallback)
    {
        GameObject panel = PanelObjeyiAl();
        if (panel == null) return;
        Transform t = panel.transform;
        while (t != null)
        {
            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
            var cg = t.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;
            t = t.parent;
        }
        panel.SetActive(true);
        var panelCg = panel.GetComponent<CanvasGroup>();
        if (panelCg != null) panelCg.alpha = 1f;
        var panelCanvas = panel.GetComponent<Canvas>();
        if (panelCanvas != null)
        {
            panelCanvas.overrideSorting = true;
            panelCanvas.sortingOrder = 9999;
            panelCanvas.enabled = true;
        }
        panel.transform.SetAsLastSibling();
        if (panel.transform.parent != null) panel.transform.parent.SetAsLastSibling();
    }

    public void Kapat()
    {
        GameObject panel = PanelObjeyiAl();
        if (panel != null) panel.SetActive(false);
    }
}
