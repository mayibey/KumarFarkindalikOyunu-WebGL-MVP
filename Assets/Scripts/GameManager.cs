using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // ==== PROFIL KAYIT (eski SaveSystem) ====
    private static string ProfilDosyaYolu => Path.Combine(Application.persistentDataPath, "profiles.json");
    private const string WebGlProfilesKey = "PP_WEBGL_PROFILES_JSON_V1";
    public static string WebGlSonProfilDurumu { get; private set; } = "hazir";

    [System.Serializable]
    private class ProfilListeSaraci
    {
        public List<PlayerProfile> profiles = new List<PlayerProfile>();
    }

    public static List<PlayerProfile> LoadProfiles()
    {
        try
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            string json = PlayerPrefs.GetString(WebGlProfilesKey, "");
            if (string.IsNullOrWhiteSpace(json))
            {
                WebGlSonProfilDurumu = "load: bos veri";
                return new List<PlayerProfile>();
            }
#else
            if (!File.Exists(ProfilDosyaYolu))
                return new List<PlayerProfile>();
            string json = File.ReadAllText(ProfilDosyaYolu);
            if (string.IsNullOrWhiteSpace(json))
                return new List<PlayerProfile>();
#endif
            var wrapper = JsonUtility.FromJson<ProfilListeSaraci>(json);
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGlSonProfilDurumu = wrapper != null && wrapper.profiles != null
                ? $"load: ok ({wrapper.profiles.Count})"
                : "load: parse fallback";
#endif
            return wrapper != null && wrapper.profiles != null ? wrapper.profiles : new List<PlayerProfile>();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("LoadProfiles hata: " + e.Message);
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGlSonProfilDurumu = "load hata: " + e.Message;
#endif
            return new List<PlayerProfile>();
        }
    }

    public static void SaveProfiles(List<PlayerProfile> profiles)
    {
        try
        {
            var wrapper = new ProfilListeSaraci { profiles = profiles };
            string json = JsonUtility.ToJson(wrapper, true);
#if UNITY_WEBGL && !UNITY_EDITOR
            PlayerPrefs.SetString(WebGlProfilesKey, json);
            PlayerPrefs.Save();
            int adet = profiles != null ? profiles.Count : 0;
            WebGlSonProfilDurumu = $"save: ok ({adet})";
#else
            File.WriteAllText(ProfilDosyaYolu, json);
#endif
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("SaveProfiles hata: " + e.Message);
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGlSonProfilDurumu = "save hata: " + e.Message;
#endif
        }
    }

    /// <summary>Tüm kullanıcıları ve senaryo/ekonomi PlayerPrefs verilerini siler. Sıfırdan başlamak için kullan.</summary>
    public static void TumKullanicilariVeVerileriSil()
    {
        var current = LoadProfiles();
        foreach (var p in current)
        {
            string id = p?.playerId ?? "";
            if (!string.IsNullOrEmpty(id))
            {
                PlayerPrefs.DeleteKey("PP_SENARYO_MEVCUT_ASAMA_" + id);
                PlayerPrefs.DeleteKey("PP_SENARYO_ASAMA_GIRIS_SPIN_" + id);
                PlayerPrefs.DeleteKey("PP_SENARYO_ODENEBILIR_KALAN_TL_" + id);
                PlayerPrefs.DeleteKey("PP_SENARYO_OTURUM_LOGU_" + id);
            }
        }
        PlayerPrefs.DeleteKey("PP_SENARYO_ODENEBILIR_KALAN_TL");
        PlayerPrefs.DeleteKey("PP_ANA_KASA_TL");
        PlayerPrefs.DeleteKey("PP_ODUL_HAVUZU_TL");
        PlayerPrefs.DeleteKey("PP_ANA_KASA_KAP");
        PlayerPrefs.DeleteKey("PP_ODUL_HAVUZU_KAP");
        PlayerPrefs.DeleteKey("PP_BAKIYE");
        PlayerPrefs.DeleteKey("PP_BAHIS");
        PlayerPrefs.DeleteKey(WebGlProfilesKey);
        PlayerPrefs.Save();

        SaveProfiles(new List<PlayerProfile>());
        if (I != null)
        {
            I.Profiles.Clear();
            I.ActivePlayer = null;
        }
        Debug.Log("[GameManager] Tüm kullanıcılar ve ilgili veriler silindi. Yeni kullanıcı ile sıfırdan devam edebilirsin.");
    }

    // Detaylı ekonomi/oyun aksiyonu kaydı
    public void RecordEconomyAction(double oncekiBakiye, double sonrakiBakiye, string islem, int bahis, int kazanc)
    {
        if (ActivePlayer == null) return;

        int toplamSpin = ActivePlayer.totalSpins;
        int toplamBonus = ActivePlayer.totalBonusEntries;

        ActivePlayer.statsEntries.Add(
    new StatsEntry(
        oncekiBakiye,
        sonrakiBakiye,
        islem,
        toplamSpin,
        toplamBonus,
        bahis,                      // bahisTutari
        "ECONOMY",                  // kategori
        $"Kazanç: {kazanc} TL"      // aciklama
    )
);


        SaveProfiles(Profiles);

    }


    public static GameManager I;

    // ==== OYUNCULAR ====
    public List<PlayerProfile> Profiles { get; private set; } = new List<PlayerProfile>();
    public PlayerProfile ActivePlayer { get; private set; }

    // ==== OTURUM BAZLI LOG (her giriş yapan kullanıcı için ayrı oturum) ====
    private int _oturumBaslangicSpins;
    private int _oturumBaslangicYatirilan;
    private int _oturumBaslangicCekilen;
    private int _oturumBaslangicNet;
    private int _oturumBaslangicBonusGiris;

    // ==== BAHIS ====
    public int SelectedBet { get; private set; } = 100;

    void Awake()
    {
        // Aynı GameObject'te GirisUI var; tüm objeyi silmek GirisUI.Start'ı engeller (sahne 1'e dönüşte butonlar bağlanmaz).
        if (I != null && I != this)
        {
            Destroy(this);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);

        Profiles = LoadProfiles();
        SceneManager.sceneLoaded += SahneYuklendi;
        Debug.Log("GameManager basladi. Yuklenen profil sayisi: " + Profiles.Count);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= SahneYuklendi;
    }

    void SahneYuklendi(Scene sahne, LoadSceneMode mod)
    {
        if (sahne.name == "03_SenaryoluOyun" && ActivePlayer != null)
        {
            // Oturum snapshot'ı sahne yüklendikten sonra yap (SenaryoYoneticisi boş logu yüklesin diye önce temizledik)
            _oturumBaslangicSpins = ActivePlayer.totalSpins;
            _oturumBaslangicYatirilan = ActivePlayer.totalDeposited;
            _oturumBaslangicCekilen = ActivePlayer.totalWithdrawn;
            _oturumBaslangicNet = ActivePlayer.totalNet;
            _oturumBaslangicBonusGiris = ActivePlayer.totalBonusEntries;
        }
    }

    /// <summary>Yeni oyun oturumu başlatır: senaryo logunu bu kullanıcı için temizler. Sahne yüklenmeden önce çağrılmalı.</summary>
    public void YeniOturumBaslat()
    {
        if (ActivePlayer == null) return;
        string key = "PP_SENARYO_OTURUM_LOGU_" + ActivePlayer.playerId;
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
    }

    /// <summary>Bu oturumdaki spin sayısı (bu girişten itibaren).</summary>
    public int OturumSpinSayisi => ActivePlayer != null ? Mathf.Max(0, ActivePlayer.totalSpins - _oturumBaslangicSpins) : 0;
    /// <summary>Bu oturumda yatırılan toplam.</summary>
    public int OturumYatirilan => ActivePlayer != null ? Mathf.Max(0, ActivePlayer.totalDeposited - _oturumBaslangicYatirilan) : 0;
    /// <summary>Bu oturumda çekilen toplam.</summary>
    public int OturumCekilen => ActivePlayer != null ? Mathf.Max(0, ActivePlayer.totalWithdrawn - _oturumBaslangicCekilen) : 0;
    /// <summary>Bu oturumdaki net değişim.</summary>
    public int OturumNet => ActivePlayer != null ? ActivePlayer.totalNet - _oturumBaslangicNet : 0;
    /// <summary>Bu oturumdaki bonus giriş sayısı.</summary>
    public int OturumBonusGiris => ActivePlayer != null ? Mathf.Max(0, ActivePlayer.totalBonusEntries - _oturumBaslangicBonusGiris) : 0;

    // ==== OYUNCU SEC / OLUSTUR ====
    public void SelectOrCreatePlayer(string playerName)
    {
        playerName = (playerName ?? "").Trim();

        if (playerName.Length < 2)
        {
            Debug.LogWarning("Isim en az 2 karakter olmali.");
            return;
        }

        foreach (var p in Profiles)
        {
            if (p.playerName == playerName)
            {
                ActivePlayer = p;
                Log("INFO", "Oyuncu secildi", 0);
                SaveProfiles(Profiles);
                return;
            }
        }

        PlayerProfile yeni = new PlayerProfile(playerName);
        Profiles.Add(yeni);
        ActivePlayer = yeni;

        Log("INFO", "Yeni oyuncu olusturuldu", 0);
        SaveProfiles(Profiles);
    }

    // ==== BAKIYE ====
    public void Deposit(int amount)
    {
        if (ActivePlayer == null || amount <= 0) return;

        ActivePlayer.balance += amount;
        ActivePlayer.totalDeposited += amount;

        Log("DEPOSIT", "Para yatirildi", amount);
        SaveProfiles(Profiles);

        Debug.Log(amount + " TL yatirildi");
    }

    public bool Withdraw(int amount)
    {
        if (ActivePlayer == null || amount <= 0) return false;
        if (ActivePlayer.balance < amount) return false;

        ActivePlayer.balance -= amount;
        ActivePlayer.totalWithdrawn += amount;

        Log("WITHDRAW", "Para cekildi", amount);
        SaveProfiles(Profiles);

        Debug.Log(amount + " TL cekildi");
        return true;
    }

    // ==== BAHIS ====
    public void SetBet(int bet)
    {
        SelectedBet = bet;
        Log("INFO", "Bahis secildi", bet);
        SaveProfiles(Profiles);

        Debug.Log("Bahis set edildi: " + bet);
    }

    // ==== OYUN OTURUMU ====
    public void AddSessionResult(int netChange)
    {
        if (ActivePlayer == null) return;

        ActivePlayer.balance += netChange;
        ActivePlayer.totalSessions += 1;
        ActivePlayer.totalNet += netChange;

        Log("SESSION", "Oturum sonucu", netChange);
        SaveProfiles(Profiles);
    }

    // ==== LOG ====
    public void Log(string type, string message, int amount)
    {
        if (ActivePlayer == null) return;
        if (ActivePlayer.logs == null) ActivePlayer.logs = new List<GameLogEntry>();
        ActivePlayer.logs.Add(new GameLogEntry(type, message, amount));
        SaveProfiles(Profiles);
    }

    // ==== SAHNE ====
    public void LoadScene(string sceneName)
    {
        if (sceneName == "03_SenaryoluOyun" && ActivePlayer != null)
            YeniOturumBaslat();
        SceneManager.LoadScene(sceneName);
    }
}