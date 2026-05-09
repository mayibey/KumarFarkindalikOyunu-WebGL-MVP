using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class LogYoneticisi : MonoBehaviour
{
    [Header("UI Referansları")]
    public TMP_Text kullaniciBilgiText;
    public Transform logContent;           // ScrollView/Viewport/Content
    public GameObject logSatirPrefab;     // Log satır prefab
    public Button replayButton;            // Bonus replay butonu
    public Button geriDonButon;           // Geri dön butonu
    
    [Header("İstatistik UI")]
    public TMP_Text toplamSpinText;
    public TMP_Text toplamKazancText;
    public TMP_Text toplamKayipText;
    public TMP_Text bonusGirisSayisiText;
    public TMP_Text bonusSatinAlmaText;
    public TMP_Text netBakiyeText;
    public TMP_Text toplamYatirilanText;
    public TMP_Text toplamCekilenText;

    private PlayerProfile _profile;

    void Start()
    {
        // Geri dön butonu
        if (geriDonButon)
            geriDonButon.onClick.AddListener(GeriDon);
        
        // Replay butonu
        if (replayButton)
            replayButton.onClick.AddListener(BonusReplayGoster);
        
        VerileriYukle();
    }

    void VerileriYukle()
    {
        if (GameManager.I == null || GameManager.I.ActivePlayer == null)
        {
            Debug.LogWarning("Aktif oyuncu yok!");
            return;
        }

        _profile = GameManager.I.ActivePlayer;

        // Kullanıcı bilgisi
        if (kullaniciBilgiText)
            kullaniciBilgiText.text = $"{_profile.playerName} - İstatistikleri";

        // İstatistikler
        if (toplamSpinText)
            toplamSpinText.text = $"Toplam Spin: {_profile.totalSpins}";
        
        if (toplamKazancText)
            toplamKazancText.text = $"Toplam Kazanç: {_profile.totalWon:N0} TL";
        
        if (toplamKayipText)
            toplamKayipText.text = $"Toplam Kayıp: {_profile.totalLost:N0} TL";
        
        if (bonusGirisSayisiText)
            bonusGirisSayisiText.text = $"Bonus Giriş: {_profile.totalBonusEntries}";
        
        if (bonusSatinAlmaText)
            bonusSatinAlmaText.text = $"Bonus Satın Alma: {BonusSatinAlmaSayisi()}";
        
        if (netBakiyeText)
            netBakiyeText.text = $"Net: {_profile.totalNet:N0} TL";
        
        if (toplamYatirilanText)
            toplamYatirilanText.text = $"Toplam Yatırılan: {_profile.totalDeposited:N0} TL";
        
        if (toplamCekilenText)
            toplamCekilenText.text = $"Toplam Çekilen: {_profile.totalWithdrawn:N0} TL";

        // Logları listele
        LoglariListele();
    }

    int BonusSatinAlmaSayisi()
    {
        if (_profile.logs == null) return 0;
        return _profile.logs.Count(x => x.type == "BONUS_BUY");
    }

    void LoglariListele()
    {
        if (logContent == null || logSatirPrefab == null) return;

        // Eski logları temizle
        for (int i = logContent.childCount - 1; i >= 0; i--)
            Destroy(logContent.GetChild(i).gameObject);

        if (_profile.logs == null || _profile.logs.Count == 0) return;

        // En son logları göster (en üstte en son)
        var logsSirali = _profile.logs.OrderByDescending(x => x.timeIso).Take(50);

        foreach (var log in logsSirali)
        {
            GameObject go = Instantiate(logSatirPrefab, logContent);
            
            // Log satırı içindeki Text bileşenlerini bul
            var texts = go.GetComponentsInChildren<TMP_Text>();
            
            if (texts.Length >= 3)
            {
                texts[0].text = log.timeIso;           // Saat
                texts[1].text = log.type;              // Tip
                texts[2].text = log.message;           // Mesaj
                
                // Miktar varsa 4. text'e yaz
                if (texts.Length >= 4 && log.amount > 0)
                    texts[3].text = $"{log.amount:N0} TL";
            }
        }
    }

    void BonusReplayGoster()
    {
        // Bonus replay özelliği: Bonus oyunların detaylı logunu göster
        Debug.Log("[LOG] Bonus Replay gösteriliyor...");
        
        // TODO: Detaylı bonus replay UI'sı eklenebilir
        // Şimdilik konsola yazdır
        if (_profile.logs != null)
        {
            var bonusLoglari = _profile.logs.Where(x => x.type.Contains("BONUS")).ToList();
            Debug.Log($"Toplam bonus log sayısı: {bonusLoglari.Count}");
        }
    }

    void GeriDon()
    {
        // Oyuna geri dön
        if (GameManager.I != null)
        {
            // Son oynanan scene'e git
            GameManager.I.LoadScene("02_SenaryoluOyun");
        }
    }

    public void Yenile()
    {
        VerileriYukle();
    }
}
