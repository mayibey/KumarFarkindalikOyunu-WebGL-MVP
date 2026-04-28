using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 01_GirisScene için OyunaBasla butonu komple kurulum:
/// - OyunaBaslaButton GO'ya Button + OyunaBaslaButonu component ekler, transition None
/// - Buton Image RaycastTarget=true; arka plan adaylarının RaycastTarget=false
/// - Eski OyunaBaslaButton (i'siz) component varsa kaldırır
/// Menü: "Kumar/Giris/OyunaBasla Butonu Kurulum (Tam)".
/// </summary>
public static class OyunaBaslaButonuKurulum
{
    [MenuItem("Kumar/Giris/OyunaBasla Butonu Kurulum (Tam)")]
    public static void Calistir()
    {
        var sahne = SceneManager.GetActiveScene();
        if (!sahne.IsValid() || !sahne.isLoaded)
        {
            EditorUtility.DisplayDialog("Sahne yüklü değil",
                "Önce 01_GirisScene'i aç.", "Tamam");
            return;
        }

        var tumGOlar = new List<GameObject>();
        foreach (var k in sahne.GetRootGameObjects())
            ToplaTumCocuklar(k.transform, tumGOlar);

        var rapor = new System.Text.StringBuilder();
        GameObject buton = null;
        var raycastKapanan = new List<string>();

        // 1) OyunaBaslaButton GO'sunu bul + arka plan adaylarının raycast'ini kapat
        foreach (var go in tumGOlar)
        {
            if (go.name == "OyunaBaslaButton" && buton == null)
            {
                buton = go;
                continue;
            }

            string adLower = go.name.ToLowerInvariant();
            bool arkaPlanAdayi =
                go.name == "SlotGrid"
                || adLower == "bg"
                || adLower.Contains("background")
                || adLower.Contains("arkaplan")
                || adLower.Contains("logo")
                || adLower.Contains("meyveler");
            if (!arkaPlanAdayi) continue;

            // SlotGrid çocukları (30 meyve hücresi) — onlara dokunma
            if (buton == null) { /* sıralama farklı olabilir, devam */ }

            var img = go.GetComponent<Image>();
            if (img != null && img.raycastTarget)
            {
                Undo.RecordObject(img, "Raycast Target Kapat");
                img.raycastTarget = false;
                EditorUtility.SetDirty(img);
                raycastKapanan.Add($"{go.name} (Image)");
            }
            var raw = go.GetComponent<RawImage>();
            if (raw != null && raw.raycastTarget)
            {
                Undo.RecordObject(raw, "Raycast Target Kapat");
                raw.raycastTarget = false;
                EditorUtility.SetDirty(raw);
                raycastKapanan.Add($"{go.name} (RawImage)");
            }

            // SlotGrid altındaki tüm Image hücrelerinin raycast'ini de kapat (meyveler tıklamayı yutmasın)
            if (go.name == "SlotGrid")
            {
                int hucreSayisi = 0;
                foreach (var cIm in go.GetComponentsInChildren<Image>(true))
                {
                    if (cIm == null || cIm.gameObject == go) continue;
                    if (!cIm.raycastTarget) continue;
                    Undo.RecordObject(cIm, "Raycast Target Kapat (cell)");
                    cIm.raycastTarget = false;
                    EditorUtility.SetDirty(cIm);
                    hucreSayisi++;
                }
                if (hucreSayisi > 0) raycastKapanan.Add($"SlotGrid altı {hucreSayisi} hücre Image");
            }
        }

        if (buton == null)
        {
            EditorUtility.DisplayDialog("Buton bulunamadı",
                "Sahnede 'OyunaBaslaButton' adlı GameObject bulunamadı.", "Tamam");
            return;
        }

        // 2) Button component
        var btn = buton.GetComponent<Button>();
        if (btn == null)
        {
            btn = Undo.AddComponent<Button>(buton);
            rapor.AppendLine("Button component eklendi.");
        }
        else
        {
            rapor.AppendLine("Button component zaten vardı.");
        }
        Undo.RecordObject(btn, "Button Transition None");
        btn.transition = Selectable.Transition.None;
        btn.interactable = true;
        EditorUtility.SetDirty(btn);

        // 3) Buton Image: RaycastTarget=true
        var btnImg = buton.GetComponent<Image>();
        if (btnImg != null)
        {
            Undo.RecordObject(btnImg, "Raycast Target Ac");
            btnImg.raycastTarget = true;
            EditorUtility.SetDirty(btnImg);
            rapor.AppendLine("Buton Image.raycastTarget = true.");
        }
        else
        {
            rapor.AppendLine("UYARI: Butonda Image yok — tıklama için bir Graphic gerekli.");
        }

        // 4) Eski OyunaBaslaButton (i'siz) component varsa kaldır
        var eskiComp = buton.GetComponent<OyunaBaslaButton>();
        if (eskiComp != null)
        {
            Undo.DestroyObjectImmediate(eskiComp);
            rapor.AppendLine("Eski OyunaBaslaButton script'i kaldırıldı.");
        }

        // 5) OyunaBaslaButonu component ekle
        var yeniComp = buton.GetComponent<OyunaBaslaButonu>();
        if (yeniComp == null)
        {
            yeniComp = Undo.AddComponent<OyunaBaslaButonu>(buton);
            rapor.AppendLine("OyunaBaslaButonu script'i eklendi.");
        }
        else
        {
            rapor.AppendLine("OyunaBaslaButonu script'i zaten vardı.");
        }

        // 6) Modal root field bağlama (sahnede KullaniciAdiModalRoot varsa)
        GameObject modalRoot = null;
        foreach (var gx in tumGOlar)
        {
            if (gx.name == "KullaniciAdiModalRoot") { modalRoot = gx; break; }
        }
        if (yeniComp != null)
        {
            var so = new SerializedObject(yeniComp);
            var prop = so.FindProperty("kullaniciAdiModalRoot");
            if (prop != null)
            {
                if (modalRoot != null)
                {
                    prop.objectReferenceValue = modalRoot;
                    so.ApplyModifiedProperties();
                    rapor.AppendLine("kullaniciAdiModalRoot bağlandı.");
                }
                else if (prop.objectReferenceValue == null)
                {
                    rapor.AppendLine("kullaniciAdiModalRoot bağlanamadı (KullaniciAdiModalRoot sahnede yok — önce 'Modal Overlay Oluştur' menüsünü çalıştır).");
                }
            }
        }

        // Raycast raporu
        if (raycastKapanan.Count > 0)
        {
            rapor.AppendLine();
            rapor.AppendLine($"Raycast Target kapatıldı ({raycastKapanan.Count} obje):");
            foreach (var r in raycastKapanan) rapor.AppendLine("  • " + r);
        }
        else
        {
            rapor.AppendLine();
            rapor.AppendLine("Raycast kapatılacak arka plan objesi bulunamadı (zaten kapalı).");
        }

        // Build Settings raporu
        rapor.AppendLine();
        rapor.AppendLine("Build Settings sahneleri:");
        for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
        {
            var s = EditorBuildSettings.scenes[i];
            string ad = System.IO.Path.GetFileNameWithoutExtension(s.path);
            rapor.AppendLine($"  • {ad} ({(s.enabled ? "enabled" : "disabled")})");
        }

        EditorSceneManager.MarkSceneDirty(sahne);
        bool kaydedildi = EditorSceneManager.SaveScene(sahne);
        rapor.AppendLine();
        rapor.AppendLine(kaydedildi ? "Sahne kaydedildi." : "UYARI: Sahne kaydedilemedi.");

        Debug.Log("[OyunaBaslaButonuKurulum] " + rapor.ToString().Replace("\n", " | "));
        EditorUtility.DisplayDialog("Buton Kurulum (Tam)", rapor.ToString(), "Tamam");
    }

    static void ToplaTumCocuklar(Transform t, List<GameObject> liste)
    {
        liste.Add(t.gameObject);
        for (int i = 0; i < t.childCount; i++)
            ToplaTumCocuklar(t.GetChild(i), liste);
    }
}
