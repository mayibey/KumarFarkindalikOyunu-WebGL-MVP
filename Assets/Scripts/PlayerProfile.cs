using System;
using System.Collections.Generic;

[Serializable]
public class PlayerProfile
{
    public List<StatsEntry> statsEntries = new List<StatsEntry>();

    public string playerId;          // benzersiz
    public string playerName;

    public int balance;              // bakiye (TL)
    public int totalDeposited;       // toplam yatrlan
    public int totalWithdrawn;       // toplam ekilen

    public int totalSessions;        // toplam oyun oturumu
    public int totalNet;             // oyunlardan toplam net (kazanlan - kaybedilen)

    // === İSTATİSTİK / KAYIT TOPLAMLARI ===
    public int totalSpins;            // toplam spin sayısı
    public int totalBonusEntries;     // toplam bonus giriş sayısı
    public int totalWagered;          // toplam ödenen bahis (TL)
    public int totalWon;              // toplam ödenen kazanç (TL)
    public int totalLost;             // toplam kaybedilen (bahis - kazanç) yaklaşım (TL)

    public List<GameLogEntry> logs = new List<GameLogEntry>();

    public PlayerProfile(string name)
    {
        playerId = Guid.NewGuid().ToString("N");
        playerName = name;

        balance = 20000;  // Yeni kullanıcılar için başlangıç bakiyesi
        totalDeposited = 0;
        totalWithdrawn = 0;

        totalSessions = 0;
        totalNet = 0;
        totalSpins = 0;
        totalBonusEntries = 0;
        totalWagered = 0;
        totalWon = 0;
        totalLost = 0;
    }
}

[Serializable]
public class GameLogEntry
{
    public string timeIso;
    public string type;      // DEPOSIT, WITHDRAW, SESSION, INFO
    public string message;
    public int amount;       // ilgili miktar (TL)

    public GameLogEntry(string type, string message, int amount)
    {
        this.timeIso = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        this.type = type;
        this.message = message;
        this.amount = amount;
    }
}

[Serializable]
public class StatsEntry
{
    public double oncekiBakiye;
    public double sonrakiBakiye;
    public string yapilanIslem;
    public double kazanc;

    public int toplamSpinSayisi;
    public int toplamBonusGirisSayisi;

    public double bahis;
    public double netDegisim;

    public string kategori;
    public string aciklama;
    public string tarihSaat;

    public StatsEntry(
        double onceki,
        double sonraki,
        string islem,
        int toplamSpin,
        int toplamBonus,
        double bahisTutari,
        string kategoriStr,
        string aciklamaStr
    )
    {
        oncekiBakiye = onceki;
        sonrakiBakiye = sonraki;
        yapilanIslem = islem;
        toplamSpinSayisi = toplamSpin;
        toplamBonusGirisSayisi = toplamBonus;
        bahis = bahisTutari;
        netDegisim = sonraki - onceki;
        kategori = kategoriStr;
        aciklama = aciklamaStr;
        tarihSaat = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
    }
}