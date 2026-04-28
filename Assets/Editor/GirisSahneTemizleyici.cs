using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 01_GirisScene için temizlik aracı:
/// 1) Sahnedeki tüm GirisUI component'lerini kaldırır.
/// 2) Tüm GameObject'lerde missing script referanslarını siler.
/// Menü: "Kumar/Giris/Sahne Temizle".
/// </summary>
public static class GirisSahneTemizleyici
{
    [MenuItem("Kumar/Giris/Sahne Temizle (GirisUI + Missing Scripts)")]
    public static void Calistir()
    {
        var aktifSahne = SceneManager.GetActiveScene();
        if (!aktifSahne.IsValid() || !aktifSahne.isLoaded)
        {
            EditorUtility.DisplayDialog("Sahne yüklü değil",
                "Önce 01_GirisScene'i aç.", "Tamam");
            return;
        }

        int kaldirilanGirisUI = 0;
        int temizlenenMissing = 0;
        int temizlenenGoSayisi = 0;

        var tumGOlar = new List<GameObject>();
        foreach (var kok in aktifSahne.GetRootGameObjects())
            ToplaTumCocuklar(kok.transform, tumGOlar);

        foreach (var go in tumGOlar)
        {
            var girisUIler = go.GetComponents<GirisUI>();
            foreach (var c in girisUIler)
            {
                if (c == null) continue;
                Object.DestroyImmediate(c, true);
                kaldirilanGirisUI++;
            }

            int oncesi = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            if (oncesi > 0)
            {
                int silinen = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                temizlenenMissing += silinen;
                if (silinen > 0) temizlenenGoSayisi++;
            }
        }

        if (kaldirilanGirisUI > 0 || temizlenenMissing > 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(aktifSahne);
        }

        var rapor =
            $"GirisUI kaldırıldı: {kaldirilanGirisUI} adet.\n" +
            $"Missing script silindi: {temizlenenMissing} adet ({temizlenenGoSayisi} GO).\n" +
            $"Taranan GO sayısı: {tumGOlar.Count}.\n\n" +
            $"Sahneyi kaydet (Ctrl+S).";

        Debug.Log("[GirisSahneTemizleyici] " + rapor.Replace("\n", " | "));
        EditorUtility.DisplayDialog("Sahne Temizleme", rapor, "Tamam");
    }

    static void ToplaTumCocuklar(Transform t, List<GameObject> liste)
    {
        liste.Add(t.gameObject);
        for (int i = 0; i < t.childCount; i++)
            ToplaTumCocuklar(t.GetChild(i), liste);
    }
}
