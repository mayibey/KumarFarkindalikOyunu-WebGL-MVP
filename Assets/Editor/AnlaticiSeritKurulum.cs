using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class AnlaticiSeritKurulum
{
    [MenuItem("Araçlar/Anlatıcı Şerit Kur")]
    public static void Kur()
    {
        var sahne = EditorSceneManager.GetActiveScene();
        if (!sahne.IsValid())
        {
            EditorUtility.DisplayDialog("Hata", "Aktif sahne bulunamadı.", "Tamam");
            return;
        }

        var mevcut = GameObject.Find("AnlaticiSeritKopru");
        
        if (mevcut != null)
        {
            var componentVar = mevcut.GetComponent<AnlaticiSeritKopru>();
            if (componentVar != null)
            {
                EditorUtility.DisplayDialog(
                    "Bilgi", 
                    "AnlaticiSeritKopru zaten kurulu.\n\nObje: " + mevcut.name + "\nSahne: " + sahne.name, 
                    "Tamam"
                );
                Selection.activeGameObject = mevcut;
                EditorGUIUtility.PingObject(mevcut);
                return;
            }
            else
            {
                Undo.AddComponent<AnlaticiSeritKopru>(mevcut);
                EditorSceneManager.MarkSceneDirty(sahne);
                EditorSceneManager.SaveScene(sahne);
                Selection.activeGameObject = mevcut;
                EditorGUIUtility.PingObject(mevcut);
                Debug.Log("[AnlaticiSeritKurulum] Mevcut objeye component eklendi: " + mevcut.name);
                EditorUtility.DisplayDialog(
                    "Tamam", 
                    "Mevcut '" + mevcut.name + "' objesine AnlaticiSeritKopru componenti eklendi ve sahne kaydedildi.", 
                    "Tamam"
                );
                return;
            }
        }

        var go = new GameObject("AnlaticiSeritKopru");
        Undo.RegisterCreatedObjectUndo(go, "Anlatıcı Şerit Kur");
        go.AddComponent<AnlaticiSeritKopru>();

        EditorSceneManager.MarkSceneDirty(sahne);
        EditorSceneManager.SaveScene(sahne);

        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);

        Debug.Log("[AnlaticiSeritKurulum] OK: " + go.name + " sahnede olusturuldu, sahne: " + sahne.path);

        EditorUtility.DisplayDialog(
            "Tamam", 
            "AnlaticiSeritKopru olusturuldu ve sahne kaydedildi.\n\nSahne: " + sahne.name, 
            "Tamam"
        );
    }
}
