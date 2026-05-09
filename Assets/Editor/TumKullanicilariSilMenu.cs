using UnityEngine;
using UnityEditor;

/// <summary>Editor menüsünden tüm kullanıcıları ve ilgili verileri (profiles.json + PlayerPrefs) silmek için.</summary>
public static class TumKullanicilariSilMenu
{
    [MenuItem("Araçlar / Tüm Kullanıcıları ve Verileri Sil")]
    public static void Calistir()
    {
        if (!EditorUtility.DisplayDialog("Tüm kullanıcıları sil", "Tüm kullanıcılar (profiles.json) ve senaryo/ekonomi PlayerPrefs verileri silinecek. Sıfırdan devam edeceksin. Emin misin?", "Evet, sil", "İptal"))
            return;
        GameManager.TumKullanicilariVeVerileriSil();
        EditorUtility.DisplayDialog("Tamam", "Tüm kullanıcılar ve ilgili veriler silindi. Yeni kullanıcı açarak devam edebilirsin.", "Tamam");
    }
}
