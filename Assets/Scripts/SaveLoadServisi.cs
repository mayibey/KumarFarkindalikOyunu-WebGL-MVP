using UnityEngine;

/// <summary>
/// PlayerPrefs üzerinden KumarSaveData JSON I/O.
///
/// - Save: spin sonu + borç paneli sonu (AnlaticiSeritKopru.SaveDurumKaydet içinden)
/// - Load: KullaniciAdiModalKontrol "DEVAM ET" → AnlaticiSeritKopru.Start restore yolu
/// - Sil:  A7 final ekranı + "SIFIRDAN BAŞLA" butonu
///
/// PlayerPrefs WebGL'de IndexedDB'ye yazılıyor (PlayerPrefs.Save() force flush).
/// 1 MB key limiti var; bizim JSON ~500 byte civarı.
/// </summary>
public static class SaveLoadServisi
{
    private const string KEY = "KumarSaveData_v1";

    public static void Save(KumarSaveData data)
    {
        if (data == null) return;
        data.saveZamani = System.DateTime.UtcNow.ToString("o");
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(KEY, json);
        PlayerPrefs.Save();
        Debug.Log($"[SaveLoad] Save yazıldı ({json.Length} byte): A{data.aktifAsama + 1} S{data.aktifSpin + 1}, bakiye={data.bakiye}, kullanici='{data.kullaniciAdi}'");
    }

    public static KumarSaveData Load()
    {
        if (!PlayerPrefs.HasKey(KEY)) return null;
        string json = PlayerPrefs.GetString(KEY);
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            var data = JsonUtility.FromJson<KumarSaveData>(json);
            if (data == null)
            {
                Debug.LogWarning("[SaveLoad] FromJson null döndü, save siliniyor.");
                Sil();
                return null;
            }
            Debug.Log($"[SaveLoad] Load başarılı: A{data.aktifAsama + 1} S{data.aktifSpin + 1}, kaydedildi {data.saveZamani}");
            return data;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveLoad] Parse hatası: {e.Message} — save siliniyor.");
            Sil();
            return null;
        }
    }

    public static void Sil()
    {
        if (PlayerPrefs.HasKey(KEY))
        {
            PlayerPrefs.DeleteKey(KEY);
            PlayerPrefs.Save();
            Debug.Log("[SaveLoad] Save silindi.");
        }
    }

    public static bool VarMi() => PlayerPrefs.HasKey(KEY);
}
