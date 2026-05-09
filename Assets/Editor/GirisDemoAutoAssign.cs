using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 01_GirisScene'deki GirisDemoAnimator component'ine meyve sprite'larını
/// otomatik bağlar. Menü: "Kumar/Giris/Sprite Atama Otomatik".
/// </summary>
public static class GirisDemoAutoAssign
{
    static readonly string[] MeyveDosyalari =
    {
        "Assets/Gorseller/elmalarrrr.png",
        "Assets/Gorseller/muz.png",
        "Assets/Gorseller/karpuz.png",
        "Assets/Gorseller/armut.png",
        "Assets/Gorseller/üzzzümmmm.png",
        "Assets/Gorseller/çilleekkk.png",
        "Assets/Gorseller/errriklerrrr.png",
        "Assets/Gorseller/hindistancevizi.png",
    };

    const string CarpanDosyasi = "Assets/Gorseller/bonanzabomba.png";

    [MenuItem("Kumar/Giris/Sprite Atama Otomatik")]
    public static void Calistir()
    {
        var animator = Object.FindObjectOfType<GirisDemoAnimator>();
        if (animator == null)
        {
            EditorUtility.DisplayDialog(
                "GirisDemoAnimator yok",
                "Sahnede GirisDemoAnimator component'i bulunamadı.\n" +
                "Önce 01_GirisScene'i aç ve SlotGrid'e (veya yeni bir DemoAnimator GO'suna) " +
                "GirisDemoAnimator script'ini ekle, sonra bu menüyü tekrar çalıştır.",
                "Tamam");
            return;
        }

        var meyveler = new List<Sprite>();
        var bulunamayanlar = new List<string>();
        foreach (var yol in MeyveDosyalari)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(yol);
            if (sprite != null) meyveler.Add(sprite);
            else bulunamayanlar.Add(yol);
        }

        if (meyveler.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Meyve sprite'ı bulunamadı",
                "Tanımlı yollarda hiçbir meyve sprite'ı yüklenemedi.\n" +
                "Sprite import tipini 'Sprite (2D and UI)' olarak ayarla.",
                "Tamam");
            return;
        }

        var carpan = AssetDatabase.LoadAssetAtPath<Sprite>(CarpanDosyasi);

        var so = new SerializedObject(animator);
        var meyveProp = so.FindProperty("meyveSprites");
        meyveProp.arraySize = meyveler.Count;
        for (int i = 0; i < meyveler.Count; i++)
            meyveProp.GetArrayElementAtIndex(i).objectReferenceValue = meyveler[i];

        var carpanProp = so.FindProperty("carpanBombaSpr");
        if (carpan != null && carpanProp != null) carpanProp.objectReferenceValue = carpan;

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(animator);
        if (!Application.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(animator.gameObject.scene);
        }

        var rapor = $"Atama tamam: {meyveler.Count} meyve sprite'ı, " +
                    $"çarpan: {(carpan != null ? "VAR" : "YOK")}.";
        if (bulunamayanlar.Count > 0)
            rapor += "\nBulunamayan: " + string.Join(", ", bulunamayanlar);

        Debug.Log("[GirisDemoAutoAssign] " + rapor);
        EditorUtility.DisplayDialog("Sprite Atama", rapor + "\n\nSahneyi kaydet (Ctrl+S).", "Tamam");
    }
}
