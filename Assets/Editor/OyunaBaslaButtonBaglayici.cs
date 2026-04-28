using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 01_GirisScene içinde "OyunaBaslaButton" adlı GameObject'i bulur,
/// üzerinde OyunaBaslaButton component yoksa ekler ve sahneyi kaydeder.
/// Menü: "Kumar/Giris/OyunaBasla Butonuna Script Ekle".
/// </summary>
public static class OyunaBaslaButtonBaglayici
{
    [MenuItem("Kumar/Giris/OyunaBasla Butonuna Script Ekle")]
    public static void Calistir()
    {
        var sahne = SceneManager.GetActiveScene();
        if (!sahne.IsValid() || !sahne.isLoaded)
        {
            EditorUtility.DisplayDialog("Sahne yüklü değil",
                "Önce 01_GirisScene'i aç.", "Tamam");
            return;
        }

        GameObject hedef = null;
        foreach (var k in sahne.GetRootGameObjects())
        {
            foreach (var t in k.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "OyunaBaslaButton") { hedef = t.gameObject; break; }
            }
            if (hedef != null) break;
        }

        if (hedef == null)
        {
            EditorUtility.DisplayDialog("Buton bulunamadı",
                "Sahnede 'OyunaBaslaButton' adlı GameObject bulunamadı.", "Tamam");
            return;
        }

        var btn = hedef.GetComponent<Button>();
        if (btn == null)
        {
            EditorUtility.DisplayDialog("Button component yok",
                "OyunaBaslaButton üzerinde UI Button yok. Önce Button ekle.", "Tamam");
            return;
        }

        var mevcut = hedef.GetComponent<OyunaBaslaButton>();
        if (mevcut != null)
        {
            EditorUtility.DisplayDialog("Zaten bağlı",
                "OyunaBaslaButton script'i bu GO'da zaten var. Bir şey yapılmadı.", "Tamam");
            return;
        }

        Undo.AddComponent<OyunaBaslaButton>(hedef);
        EditorSceneManager.MarkSceneDirty(sahne);
        bool kaydedildi = EditorSceneManager.SaveScene(sahne);

        var rapor = $"OyunaBaslaButton script'i '{hedef.name}' GO'suna eklendi.\n" +
                    $"Sahne: {(kaydedildi ? "kaydedildi" : "KAYDEDİLEMEDİ")}.";
        Debug.Log("[OyunaBaslaButtonBaglayici] " + rapor.Replace("\n", " | "));
        EditorUtility.DisplayDialog("Script Bağlama", rapor, "Tamam");
    }
}
