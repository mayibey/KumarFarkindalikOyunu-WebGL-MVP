# Log Sahnesi – Profesyonel İki Panelli Kurulum (Unity’de Ne Eklemen Gerekiyor)

Bu dokümanda **04_LogScane** için iki ayrı bölümlü, okunaklı bir log ekranı kurmak üzere Unity’de eklemen gerekenler adım adım yazıyor. Tek ScrollView zorunlu değil; özet ve senaryo logu ayrı panellerde olabilir.

**Amaç:** Kumar farkındalık senaryosunda oyuncunun oturumunu (yatırım, çekim, spin, bonus, aşama geçişleri) sayısal ve kronolojik olarak belgelemek; eğitim ve analiz için rapor sunmak.

---

## 1. Genel Yapı (Hierarchy)

Hedef hiyerarşi aşağıdaki gibi. **Canvas** altında:

```
Canvas (veya LogArayuzu)
├── Text_Baslik                    ← "[Kullanıcı adı] - İstatistikleri" (Kullanici Bilgi Text)
├── Panel_GenelOzet                ← Bölüm 1: Genel özet
│   └── GenelOzetText              ← TextMeshPro – özet metni burada
├── Panel_SenaryoLog               ← Bölüm 2: Senaryo aşamaları (1–7)
│   └── Scroll_SenaryoLog          ← Scroll Rect (metin uzayınca aşağı kaydırma için zorunlu)
│       └── Viewport
│           └── Content_SenaryoLog ← Content + Content Size Fitter (Vertical: Preferred Size)
│               └── SenaryoLoguText ← TextMeshPro – log metni buraya yazılır
├── Buton_GirisEkrani              ← "GİRİŞE DÖN" butonu
└── (isteğe bağlı) Log Scene yazısı vb.
```

---

## 2. Unity’de Yapılacaklar (Adım Adım)

### Adım 1: Başlık (zaten varsa atla)

- Canvas altında **Text - TextMeshPro** oluştur.
- Adını **Text_Baslik** veya bırak; LogYoneticisi’nde **Kullanici Bilgi Text** alanına sürükle.
- Bu alan "[Kullanıcı adı] - İstatistikleri" yazacak.

### Adım 2: Bölüm 1 – Genel Özet paneli

1. Canvas altında **UI → Panel** ekle; adını **Panel_GenelOzet** yap.
2. Panel içinde **UI → Text - TextMeshPro** ekle; adını **GenelOzetText** yap (script bu isimle arar).
3. **GenelOzetText** ayarları (okunaklı olsun diye):
   - Font Size: **22** (veya 20–24)
   - RectTransform: Panel’i kaplasın (stretch), üst/alt/kenar boşlukları istediğin gibi.
   - Overflow: **Overflow** veya **Truncate** yerine metnin taşması için panel yüksekliği yeterli olsun; gerekirse Panel’e Scroll View da ekleyebilirsin (tek başına büyük metin kutusu da yeterli).
4. **LogYoneticisi** Inspector’da **Genel Ozet Text** alanına **GenelOzetText** objesini sürükle. Atanmazsa script sahnede **GenelOzetText** adlı TMP_Text’i arar; iki panelli kurulumda atanması önerilir.

### Adım 3: Bölüm 2 – Senaryo log paneli (her aşamada neler yaşandı)

1. Canvas altında **UI → Panel** ekle; adını **Panel_SenaryoLog** yap.
2. Bu panelin içinde **UI → Scroll View** ekle; adını **Scroll_SenaryoLog** yap.
3. Scroll View’ın varsayılan yapısı: **Viewport** ve **Content** (ve Scrollbar). **Content** objesinin adını **Content_SenaryoLog** yap (script bu isimle arar).
4. **Content_SenaryoLog** üzerinde:
   - **Vertical Layout Group** ekle (Component → Layout → Vertical Layout Group):
     - Child Alignment: Upper Center
     - Control Child Size: Width ✓, Height ✓
     - Child Force Expand: Width ✓, Height ✗
     - Spacing: 6
     - Padding: 8 (veya istediğin değer)
   - **Content Size Fitter** (isteğe bağlı): Vertical Fit → Preferred Size (içerik uzadıkça scroll’un kaydırılabilir olması için).
5. **LogYoneticisi** Inspector’da **Senaryo Log Content** alanına **Content_SenaryoLog** objesini sürükle. Atanmazsa script **Content_SenaryoLog** veya **logContent** ile eşleşen Content’i sahnede arar; iki panelli kullanımda atanması önerilir.

**Senaryo logu için scroll (metin uzayınca aşağı inme):** SenaryoLoguText uzadığında aşağı kaydırabilmek için bu metni bir **Scroll View** içine alın: Panel_SenaryoLog → **Scroll Rect** → Viewport → **Content** → **SenaryoLoguText**. Content objesine **Content Size Fitter** ekleyin; **Vertical Fit = Preferred Size** seçin. LogYoneticisi, metin atandıktan sonra üstündeki ScrollRect varsa içerik boyutunu günceller; böylece scroll çalışır.

### Adım 4: Geri dön butonu

- "GİRİŞE DÖN" butonunun adı **Buton_GirisEkrani** olsun (script isimle bulur).
- LogYoneticisi’nde **Geri Don Buton** alanına sürükle.

### Adım 5: LogYoneticisi component’i

- **LogYoneticisi** script’ini sahneye ekle: Canvas’a veya **Panel_LogRapor** / **LogArayuzu** gibi bir üst objeye **Add Component → Log Yoneticisi**.
- Inspector’da atamalar:
  - **Kullanici Bilgi Text** → Text_Baslik (veya başlık metni)
  - **Genel Ozet Text** → GenelOzetText
  - **Senaryo Logu Text** → SenaryoLoguText (Scroll View → Viewport → Content içinde olmalı)
  - **Log Content** → İki panelli kurulumda kullanılmaz; tek scroll kullanıyorsan o scroll’un Content’ini atayın.
  - **Geri Don Buton** → Buton_GirisEkrani

---

## 3. Ekranda Görünecekler

- **Bölüm 1 (Panel_GenelOzet / GenelOzetText):** Kullanıcı, toplam spin, yatırılan/çekilen, net, bakiye, kazanç/kayıp, bonus sayıları, senaryo oturumu (ulaşılan aşama, oturumda yatırılan, bahis değişikliği). Kod tarafında özet metni **22pt** font ile yazılıyor.
- **Bölüm 2 (Content_SenaryoLog):** "SENARYO AKIŞI – Her aşamada neler yaşandı" başlığı altında **Senaryo 1** … **Senaryo 7** için ayrı ayrı:
  - Her aşamada o aşamada yaşanan olaylar (zaman, olay tipi, açıklama, bakiye, net).
  - O aşamada kayıt yoksa "(Bu aşamada henüz kayıt yok.)" yazılır.
- Başlık ve satır fontları kodda büyütüldü (ana başlık 24, aşama başlıkları 20, log satırları 19).

---

## 4. Tek ScrollView kullanmak istersen

- **Genel Ozet Text** ve **Senaryo Log Content** alanlarını atamayın; script tüm içeriği tek scroll’a yazar.
- **logContent** alanına **Scroll_LogRapor** → **Viewport** → **Content** objesini atayın (veya script **Scroll_LogRapor** ile isimle bulur).
- Sonuç: Aynı scroll içinde önce genel özet bloku, sonra senaryo akışı (1–7) listelenir; fontlar 22/24/20/19 pt.

---

## 5. Özet tablo

| Unity’de oluştur / adlandır     | LogYoneticisi alanı   | Açıklama |
|---------------------------------|------------------------|----------|
| Text (TMP) – başlık             | Kullanici Bilgi Text   | "[Kullanıcı] - İstatistikleri" |
| Panel_GenelOzet → **GenelOzetText** (TMP) | Genel Ozet Text   | Bölüm 1: genel özet metni |
| Panel_SenaryoLog → Scroll → Viewport → **Content** → **SenaryoLoguText** (TMP) | Senaryo Logu Text | Bölüm 2: Log metni; Content'te Content Size Fitter (Vertical: Preferred) olmalı, metin uzayınca scroll ile aşağı inilir |
| Buton "GİRİŞE DÖN" → adı **Buton_GirisEkrani** | Geri Don Buton    | Giriş sahnesine dön |

Bu yapıyı kurduktan sonra log sahnesi iki bölümlü ve her senaryo aşaması için ayrı bölümlerle profesyonel görünüme kavuşur; yazı boyutları da okunaklı olacak şekilde ayarlı.

---

## 6. Geliştirme yönü (log sahnesi)

Oyun akışında senaryo logu artık Türkçe olay tipleriyle dolduruluyor: `SenaryoOlayKaydi.OlayTipi_OturumBasladi`, `OlayTipi_NormalSpinBasladi`, `OlayTipi_NormalSpinBitti`, `OlayTipi_BonusBitti`, `OlayTipi_AsamaGecisi`, `OlayTipi_AsamaAralikOzeti`, `OlayTipi_BakiyeYuklemeYapildi`, `OlayTipi_BakiyeYuklemeEkraniAcildi`, `OlayTipi_BakiyeYuklemeReddedildi`, `OlayTipi_ParaCekEkraniAcildi`, `OlayTipi_ParaCekildi` vb. Log sahnesinde filtreleme bu `olayTipi` değerlerine göre yapılabilir.

- **Filtreleme:** Olay tipine (aşama girişi, bonus, bakiye yükleme, ihlal vb.) veya aşama numarasına göre listeleme; LogYoneticisi veya ayrı bir LogFiltre bileşeni ile.
- **Anlamlı olaylar:** SenaryoOlayKaydi’deki Türkçe olay tiplerinden yalnızca kullanıcıya anlamlı olanları (aşama geçişi, bonus giriş/çıkış, yükleme, para çekme, limit ihlali vb.) panelde göstermek; teknik/spam sayılabilecek olayları gizleme veya “Detay” açılırında sunma.
- **Dışa aktarma:** `LogYoneticisi.SenaryoLogunuTxtOlarakDisariAktar()` metodu hazır; log sahnesine bir buton ekleyip OnClick ile bu metodu çağırarak oturum özeti + senaryo logunu `Application.persistentDataPath` altında TXT olarak kaydedebilirsin (dosya adı: SenaryoLogu_ProfilAdi_yyyyMMdd_HHmmss.txt).
- **Kalıcılık:** Oturum logunu uygulama kapanışında dosyaya veya profile’a yazıp, yeniden başlatma sonrası log sahnesinde “son oturum” olarak gösterme.
