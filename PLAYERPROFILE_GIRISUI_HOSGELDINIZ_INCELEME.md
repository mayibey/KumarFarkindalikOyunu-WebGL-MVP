# PlayerProfile, GirisUI, HosgeldinizText — Değerlendirme

## 1. PlayerProfile.cs

| Ne | Açıklama |
|----|----------|
| **Tür** | Veri sınıfı (Serializable), MonoBehaviour değil |
| **İçerik** | Oyuncu verisi: playerId, playerName, balance, totalDeposited/Withdrawn, totalSessions, totalNet, totalSpins, totalBonusEntries, totalWagered/Won/Lost, statsEntries, logs. İçinde nested **GameLogEntry** ve **StatsEntry** sınıfları. |
| **Kullanım** | GameManager (Profiles listesi, ActivePlayer, yeni profil oluşturma), GirisUI (_profilesCache, liste), LogYoneticisi (_profile), SaveSystem/GameManager.LoadProfiles/SaveProfiles. |

**Sonuç:** Çekirdek veri modeli. Başka bir dosyaya “birleştirilmesi” mantıklı değil; silinmez, bölünmez. **Olduğu gibi kalmalı.**

---

## 2. GirisUI.cs

| Ne | Açıklama |
|----|----------|
| **Tür** | MonoBehaviour (giriş sahnesi) |
| **İçerik** | Senaryolu Oyun / Admin Oyun butonları, kullanıcı giriş paneli, isim input, kullanıcı listesi (prefab + Content), Giriş Yap butonu, uyarı metni, sahne adları. Start’ta buton bulma ve event bağlama; OyunTipiSecildi, GirisYap, ListeyiYenile, KullaniciListedenSec. GameManager.SelectOrCreatePlayer ve LoadScene kullanır. |
| **Kullanım** | Sadece sahnede: 01_Giris veya benzeri sahnede bileşen olarak durur. Başka script’te `FindObjectOfType<GirisUI>` vb. yok. |

**Sonuç:** Tek sahneye özel UI orkestratörü. PlayerProfile ile birleştirilmez (biri veri biri UI). **Olduğu gibi kalabilir**; istenirse “hoş geldiniz” davranışı buraya taşınabilir (aşağıda).

---

## 3. HosgeldinizText.cs

| Ne | Açıklama |
|----|----------|
| **Tür** | MonoBehaviour (çok ince) |
| **İçerik** | `public string mesaj`, Start/OnEnable’da `Yazdir()`: aynı GameObject’teki TextMeshProUGUI’ye mesajı yazar. |
| **Kullanım** | Muhtemelen giriş sahnesinde bir TMP nesnesinde bileşen; başka script’te referans yok. |

**Sonuç:** Tek iş: “bir TMP’ye metin yaz.” Bu davranış **GirisUI** içine alınabilir: giriş sahnesi zaten GirisUI ile yönetiliyorsa, hoş geldiniz metni de orada güncellenebilir.

---

## 4. Birleştirme Önerisi

| Hedef | Ne yapılır? | Risk |
|-------|-------------|------|
| **PlayerProfile** | Dokunma. Veri sınıfı, birleştirme adayı değil. | — |
| **GirisUI + HosgeldinizText** | HosgeldinizText’in işini GirisUI’e taşı: GirisUI’de opsiyonel `TMP_Text hosgeldinizText` ve `string hosgeldinizMesaj` alanları; Start/OnEnable’da `if (hosgeldinizText) hosgeldinizText.text = hosgeldinizMesaj`. HosgeldinizText.cs’i sil. Giriş sahnesinde bu TMP’ye GirisUI üzerinden referans verilir. | Düşük. Sadece 1 dosya azalır, davranış aynı kalır. |

**Özet:**  
- **PlayerProfile** → Değişiklik yok.  
- **GirisUI** → Kalır; istenirse HosgeldinizText mantığı eklenir.  
- **HosgeldinizText** → Silinebilir; işi GirisUI’e taşınır.

İstersen bir sonraki adımda sadece “HosgeldinizText → GirisUI” birleştirmesini uygulayacak değişiklikleri adım adım yazabilirim.
