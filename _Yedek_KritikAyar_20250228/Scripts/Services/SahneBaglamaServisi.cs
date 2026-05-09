using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI referanslarını sahneden bulup hedefe atar. Sadece null olan alanlar doldurulur.
/// OyunYoneticisi içindeki AutoWire / FindComp / FindTmp zincirlerinin tek kaynağı.
/// </summary>
public class SahneBaglamaServisi
{
    /// <summary>
    /// Wiring hedefi: servis sadece null olan ref'leri set eder.
    /// </summary>
    public interface IBaglamaHedefi
    {
        Button CevirButon { get; set; }
        TMP_Text BakiyeText { get; set; }
        TMP_Text BahisText { get; set; }
        TMP_Text HakText { get; set; }
        TMP_Text KazancText { get; set; }
        TMP_Text CarpanText { get; set; }
        TMP_Text CarpanOlasilikValueText { get; set; }
        TMP_Text CarpanMaxAdetValueText { get; set; }
        Button BakiyeYukleButon { get; set; }
        GameObject BakiyeYuklePanel { get; set; }
        TMP_InputField BakiyeYukleInput { get; set; }
        Button BakiyeYukleOnayButon { get; set; }
        Button BakiyeYukleIptalButon { get; set; }
        Button ParaCekButon { get; set; }
        GameObject ParaCekPanel { get; set; }
        TMP_InputField ParaCekInput { get; set; }
        Button ParaCekOnayButon { get; set; }
        Button ParaCekIptalButon { get; set; }
        Button BonusSatinAlButon { get; set; }
        GameObject BonusBuyConfirmPanel { get; set; }
        TMP_Text BonusBuyConfirmCostText { get; set; }
        CanvasGroup BonusBuyConfirmCanvasGroup { get; set; }
        Button BonusBuyYesButton { get; set; }
        Button BonusBuyNoButton { get; set; }
        GameObject BonusStartPanel { get; set; }
        GameObject BonusEndPanel { get; set; }
        CanvasGroup BonusEndCanvasGroup { get; set; }
        CanvasGroup BonusStartCanvasGroup { get; set; }
    }

    /// <summary>
    /// Eksik (null) UI referanslarını sahneden isimle bulup hedefe yazar. Para/Bakiye panellerindeki input ve butonları da panel altından doldurur.
    /// </summary>
    /// <param name="root">FindTmpByNameContains için arama kökü (örn. OyunYoneticisi'nin transform'u)</param>
    /// <param name="target">Doldurulacak hedef (get/set ile ref'ler yazılır)</param>
    public void BindIfNeeded(Transform root, IBaglamaHedefi target)
    {
        if (target == null) return;

        // --- Sahneden isimle bul ve hedefe yaz ---
        if (target.CevirButon == null)
            target.CevirButon = FindComp<Button>("CevirButon", "SpinButton", "BtnSpin", "SpinButon", "CevirButton", "SpinButonu");

        if (target.BakiyeText == null)
            target.BakiyeText = FindTmpByNameContains(root, "Bakiye");
        if (target.BahisText == null)
            target.BahisText = FindTmpByNameContains(root, "Bahis");
        if (target.HakText == null)
            target.HakText = FindTmpByNameContains(root, "Hak");
        if (target.KazancText == null)
            target.KazancText = FindTmpByNameContains(root, "Kazanc", "KAZAN");
        if (target.CarpanText == null)
            target.CarpanText = FindTmpByNameContains(root, "Carpan", "X");
        if (target.CarpanOlasilikValueText == null)
            target.CarpanOlasilikValueText = FindComp<TextMeshProUGUI>("CarpanOlasilikValueText");
        if (target.CarpanMaxAdetValueText == null)
            target.CarpanMaxAdetValueText = FindComp<TextMeshProUGUI>("CarpanMaxAdetValueText");

        if (target.BakiyeYukleButon == null)
            target.BakiyeYukleButon = FindComp<Button>("BakiyeYukleButon", "BakiyeYukleButton", "BakiyeYukle", "BtnBakiyeYukle");
        if (target.BakiyeYuklePanel == null)
            target.BakiyeYuklePanel = FindGO("BakiyeYuklePanel", "PanelBakiyeYukle", "BakiyeYuklePenceresi");
        if (target.BakiyeYukleInput == null)
            target.BakiyeYukleInput = FindComp<TMP_InputField>("BakiyeYukleInput", "InputBakiyeYukle", "BakiyeYukleInputField");
        if (target.BakiyeYukleOnayButon == null)
            target.BakiyeYukleOnayButon = FindComp<Button>("BakiyeYukleOnayButon", "BakiyeYukleOnayButton", "BtnBakiyeYukleOnay");
        if (target.BakiyeYukleIptalButon == null)
            target.BakiyeYukleIptalButon = FindComp<Button>("BakiyeYukleIptalButon", "BakiyeYukleIptalButton", "BtnBakiyeYukleIptal");

        if (target.ParaCekButon == null)
            target.ParaCekButon = FindComp<Button>("ParaCekButon", "ParaCekButton", "ParaCek", "BtnParaCek");
        if (target.ParaCekPanel == null)
            target.ParaCekPanel = FindGO("ParaCekPanel", "PanelParaCek", "ParaCekPenceresi");
        if (target.ParaCekInput == null)
            target.ParaCekInput = FindComp<TMP_InputField>("ParaCekInput", "InputParaCek", "ParaCekInputField");
        if (target.ParaCekOnayButon == null)
            target.ParaCekOnayButon = FindComp<Button>("ParaCekOnayButon", "ParaCekOnayButton", "BtnParaCekOnay");
        if (target.ParaCekIptalButon == null)
            target.ParaCekIptalButon = FindComp<Button>("ParaCekIptalButon", "ParaCekIptalButton", "BtnParaCekIptal");

        if (target.BonusSatinAlButon == null)
            target.BonusSatinAlButon = FindComp<Button>("BonusSatinAlButon", "BonusSatinAlButton", "BonusSatinAl", "BtnBonusSatinAl");
        if (target.BonusBuyConfirmPanel == null)
            target.BonusBuyConfirmPanel = FindGO("BonusBuyConfirmPanel", "BonusBuyOnayPanel", "BonusSatinAlOnayPanel", "BonusSatinAlConfirmPanel");
        if (target.BonusBuyConfirmCanvasGroup == null && target.BonusBuyConfirmPanel != null)
            target.BonusBuyConfirmCanvasGroup = target.BonusBuyConfirmPanel.GetComponent<CanvasGroup>();
        if (target.BonusBuyConfirmCostText == null)
        {
            target.BonusBuyConfirmCostText = FindComp<TextMeshProUGUI>("MaliyetText", "CostText", "BonusBuyConfirmCostText");
            if (target.BonusBuyConfirmCostText == null && target.BonusBuyConfirmPanel != null)
                target.BonusBuyConfirmCostText = target.BonusBuyConfirmPanel.GetComponentInChildren<TextMeshProUGUI>(true);
        }
        if (target.BonusBuyYesButton == null)
            target.BonusBuyYesButton = FindComp<Button>("YesButton", "BonusBuyYesButon", "BonusBuyYesButton", "BtnBonusBuyYes");
        if (target.BonusBuyNoButton == null)
            target.BonusBuyNoButton = FindComp<Button>("NoButton", "BonusBuyNoButon", "BonusBuyNoButton", "BtnBonusBuyNo");

        if (target.BonusStartPanel == null)
            target.BonusStartPanel = FindGO("BonusStartPanel", "BonusBaslangicPanel");
        if (target.BonusEndPanel == null)
            target.BonusEndPanel = FindGO("BonusEndPanel", "BonusBitisPanel");
        if (target.BonusEndCanvasGroup == null && target.BonusEndPanel != null)
            target.BonusEndCanvasGroup = target.BonusEndPanel.GetComponent<CanvasGroup>();
        if (target.BonusStartCanvasGroup == null && target.BonusStartPanel != null)
            target.BonusStartCanvasGroup = target.BonusStartPanel.GetComponent<CanvasGroup>();

        ResolveMoneyUIRefsOnly(target);

        Debug.Log($"[WIRING] BakiyeText={target.BakiyeText?.gameObject?.name}, KazancText={target.KazancText?.gameObject?.name}, CarpanText={target.CarpanText?.gameObject?.name}");
    }

    private static void ResolveMoneyUIRefsOnly(IBaglamaHedefi target)
    {
        if (target.BakiyeYuklePanel != null)
        {
            if (target.BakiyeYukleInput == null)
                target.BakiyeYukleInput = target.BakiyeYuklePanel.GetComponentInChildren<TMP_InputField>(true);
            if (target.BakiyeYukleOnayButon == null || target.BakiyeYukleIptalButon == null)
            {
                var buttons = target.BakiyeYuklePanel.GetComponentsInChildren<Button>(true);
                foreach (var b in buttons)
                {
                    if (b == null) continue;
                    string n = (b.gameObject != null ? b.gameObject.name : "").ToLowerInvariant();
                    if (target.BakiyeYukleOnayButon == null && (n.Contains("onay") || n.Contains("yükle") || n.Contains("yukle") || n.Contains("confirm") || n.Contains("ok")))
                        target.BakiyeYukleOnayButon = b;
                    if (target.BakiyeYukleIptalButon == null && (n.Contains("kapat") || n.Contains("iptal") || n.Contains("close") || n.Contains("cancel") || n.Contains("x")))
                        target.BakiyeYukleIptalButon = b;
                }
            }
        }
        if (target.ParaCekPanel != null)
        {
            if (target.ParaCekInput == null)
                target.ParaCekInput = target.ParaCekPanel.GetComponentInChildren<TMP_InputField>(true);
            if (target.ParaCekOnayButon == null || target.ParaCekIptalButon == null)
            {
                var buttons = target.ParaCekPanel.GetComponentsInChildren<Button>(true);
                foreach (var b in buttons)
                {
                    if (b == null) continue;
                    string n = (b.gameObject != null ? b.gameObject.name : "").ToLowerInvariant();
                    if (target.ParaCekOnayButon == null && (n.Contains("çek") || n.Contains("cek") || n.Contains("onay") || n.Contains("confirm") || n.Contains("ok")))
                        target.ParaCekOnayButon = b;
                    if (target.ParaCekIptalButon == null && (n.Contains("kapat") || n.Contains("iptal") || n.Contains("close") || n.Contains("cancel") || n.Contains("x")))
                        target.ParaCekIptalButon = b;
                }
            }
        }
    }

    private static GameObject FindGO(params string[] names)
    {
        foreach (var n in names)
        {
            if (string.IsNullOrWhiteSpace(n)) continue;
            var go = GameObject.Find(n);
            if (go != null) return go;
        }
        return null;
    }

    private static T FindComp<T>(params string[] names) where T : Component
    {
        foreach (var n in names)
        {
            if (string.IsNullOrWhiteSpace(n)) continue;
            var go = GameObject.Find(n);
            if (go == null) continue;
            var c = go.GetComponent<T>();
            if (c != null) return c;
            c = go.GetComponentInChildren<T>(true);
            if (c != null) return c;
        }
        return null;
    }

    private static TextMeshProUGUI FindTmpByNameContains(Transform root, string contains)
    {
        if (root == null) return null;
        var all = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        return FindTmpByNameContains(all, contains);
    }

    private static TextMeshProUGUI FindTmpByNameContains(Transform root, string containsA, string containsB)
    {
        if (root == null) return null;
        var all = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        var a = FindTmpByNameContains(all, containsA);
        if (a != null) return a;
        return FindTmpByNameContains(all, containsB);
    }

    private static TextMeshProUGUI FindTmpByNameContains(TextMeshProUGUI[] all, string contains)
    {
        if (all == null) return null;
        string key = contains.ToLowerInvariant();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null) continue;
            if (all[i].gameObject != null && all[i].gameObject.name.ToLowerInvariant().Contains(key))
                return all[i];
        }
        return null;
    }
}
