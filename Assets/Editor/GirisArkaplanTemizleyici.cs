using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 01_GirisScene için arka plan temizleyici:
/// 1) SlotGrid'in kendi Image component'ini siler (GridLayoutGroup vs. korunur).
/// 2) GirisArayuzu altında SlotGrid değil olan arka plan adaylarını (bg/Background/BG/ArkaPlan)
///    SetActive(false) ile kapatır.
/// 3) Sahneyi kaydeder.
/// Menü: "Kumar/Giris/Arka Plan Temizle".
/// </summary>
public static class GirisArkaplanTemizleyici
{
    static readonly string[] ArkaPlanAdAdaylari = { "bg", "background", "arkaplan", "candyland" };

    [MenuItem("Kumar/Giris/Arka Plan Temizle")]
    public static void Calistir()
    {
        var sahne = SceneManager.GetActiveScene();
        if (!sahne.IsValid() || !sahne.isLoaded)
        {
            EditorUtility.DisplayDialog("Sahne yüklü değil",
                "Önce 01_GirisScene'i aç.", "Tamam");
            return;
        }

        // 1) SlotGrid'i bul
        var slotGrid = SlotGridBul(sahne);
        var rapor = new System.Text.StringBuilder();

        bool slotGridImageVarMi = false;
        string slotGridSpriteAdi = "(yok)";
        if (slotGrid == null)
        {
            rapor.AppendLine("UYARI: SlotGrid bulunamadı.");
        }
        else
        {
            var img = slotGrid.GetComponent<Image>();
            if (img != null)
            {
                slotGridImageVarMi = true;
                slotGridSpriteAdi = img.sprite != null ? img.sprite.name : "(boş sprite)";
                UnityEngine.Object.DestroyImmediate(img, true);
                rapor.AppendLine($"SlotGrid Image kaldırıldı (sprite: {slotGridSpriteAdi}).");
            }
            else
            {
                rapor.AppendLine("SlotGrid'de Image component yoktu.");
            }
        }

        // 2) Arka plan adaylarını kapat — sahnedeki tüm GO'lar arasında
        var koklar = sahne.GetRootGameObjects();
        var tumGOlar = new List<GameObject>();
        foreach (var k in koklar) ToplaTumCocuklar(k.transform, tumGOlar);

        var kapatilanlar = new List<string>();
        foreach (var go in tumGOlar)
        {
            if (slotGrid != null && go == slotGrid) continue;
            if (slotGrid != null && go.transform.IsChildOf(slotGrid.transform)) continue;

            string ad = go.name.ToLowerInvariant();
            bool eslesti = false;
            foreach (var aday in ArkaPlanAdAdaylari)
            {
                if (ad.Contains(aday)) { eslesti = true; break; }
            }
            if (!eslesti) continue;

            if (go.GetComponent<Image>() == null && go.GetComponent<RawImage>() == null)
                continue;
            if (!go.activeSelf) continue;

            go.SetActive(false);
            kapatilanlar.Add($"{go.name} (path: {YolBul(go.transform)})");
        }

        if (kapatilanlar.Count == 0)
            rapor.AppendLine("Başka arka plan objesi bulunamadı (kapatılan yok).");
        else
        {
            rapor.AppendLine($"Kapatılan arka plan: {kapatilanlar.Count} adet.");
            foreach (var k in kapatilanlar) rapor.AppendLine("  • " + k);
        }

        // 3) Sahne kaydet
        EditorSceneManager.MarkSceneDirty(sahne);
        bool kaydedildi = EditorSceneManager.SaveScene(sahne);
        rapor.AppendLine(kaydedildi ? "Sahne kaydedildi." : "UYARI: Sahne kaydedilemedi.");

        rapor.AppendLine();
        rapor.AppendLine($"SlotGrid arka planı durumu: {(slotGridImageVarMi ? "TEMİZLENDİ" : "zaten temizdi")}.");

        Debug.Log("[GirisArkaplanTemizleyici] " + rapor.ToString().Replace("\n", " | "));
        EditorUtility.DisplayDialog("Arka Plan Temizleme", rapor.ToString(), "Tamam");
    }

    static GameObject SlotGridBul(Scene sahne)
    {
        foreach (var k in sahne.GetRootGameObjects())
        {
            var t = k.GetComponentsInChildren<Transform>(true);
            foreach (var x in t)
                if (x.name == "SlotGrid") return x.gameObject;
        }
        return null;
    }

    static void ToplaTumCocuklar(Transform t, List<GameObject> liste)
    {
        liste.Add(t.gameObject);
        for (int i = 0; i < t.childCount; i++)
            ToplaTumCocuklar(t.GetChild(i), liste);
    }

    static string YolBul(Transform t)
    {
        var sb = new System.Text.StringBuilder(t.name);
        var p = t.parent;
        while (p != null)
        {
            sb.Insert(0, p.name + "/");
            p = p.parent;
        }
        return sb.ToString();
    }
}
