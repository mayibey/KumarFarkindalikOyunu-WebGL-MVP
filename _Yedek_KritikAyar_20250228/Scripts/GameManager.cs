using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // ==== PROFIL KAYIT (eski SaveSystem) ====
    private static string ProfilDosyaYolu => Path.Combine(Application.persistentDataPath, "profiles.json");

    [System.Serializable]
    private class ProfilListeSaraci
    {
        public List<PlayerProfile> profiles = new List<PlayerProfile>();
    }

    public static List<PlayerProfile> LoadProfiles()
    {
        try
        {
            if (!File.Exists(ProfilDosyaYolu))
                return new List<PlayerProfile>();
            string json = File.ReadAllText(ProfilDosyaYolu);
            if (string.IsNullOrWhiteSpace(json))
                return new List<PlayerProfile>();
            var wrapper = JsonUtility.FromJson<ProfilListeSaraci>(json);
            return wrapper != null && wrapper.profiles != null ? wrapper.profiles : new List<PlayerProfile>();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("LoadProfiles hata: " + e.Message);
            return new List<PlayerProfile>();
        }
    }

    public static void SaveProfiles(List<PlayerProfile> profiles)
    {
        try
        {
            var wrapper = new ProfilListeSaraci { profiles = profiles };
            string json = JsonUtility.ToJson(wrapper, true);
            File.WriteAllText(ProfilDosyaYolu, json);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("SaveProfiles hata: " + e.Message);
        }
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

    // ==== BAHIS ====
    public int SelectedBet { get; private set; } = 100;

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);

        Profiles = LoadProfiles();
        Debug.Log("GameManager basladi. Yuklenen profil sayisi: " + Profiles.Count);
    }

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
        SceneManager.LoadScene(sceneName);
    }
}