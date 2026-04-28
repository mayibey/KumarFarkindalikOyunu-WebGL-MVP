# Senaryolu Oyun Sahnesi – Unity Geliştirme Planı

**Kod tarafı (SenaryoOlayKaydi, SenaryoYoneticisi 7 aşama + şart takibi + log + manuel geçiş) tamamlandı.**

**Önce:** Senaryolu oyun sahnesinin hiyerarşisi admin sahnesiyle (03_AdminOyunScene) aynı olmalı — aynı altyapı (OyunYoneticisi, Canvas içeriği, KasaSistemi, TumbleAyarlari vb.). Detay ve adımlar: **Docs/SENARYOLU_OYUN_HIYERARSI_ADMIN_ILE_ESIT.md**.

Bu dokümanda senaryolu oyun sahnesinde (02_SenaryoluOyun) yapılacaklar, **Unity tarafındaki tüm öğe isimleri (Türkçe)** ve log yapısı tek yerde toplanmıştır.

---

## 1. Amaç

- Ekranda **kenarda bir yerde** mevcut senaryo aşaması, **tamamlanan şartlar**, **aşamadan çıkmak için kalan şartlar** tek panelde görünsün.
- **Manuel senaryo geçişi** ayarı olsun (geliştirme/test için).
- Tüm aşama geçişleri ve önemli olaylar **log**lansın; log sahnesinde (04_LogScane) gösterilebilsin.

---

## 2. Unity’de Oluşturacağın Panel ve Öğeler (İsimler Türkçe)

Aşağıdaki hiyerarşiyi **02_SenaryoluOyun** sahnesinde oluştur. Canvas altında veya uygun bir kök objenin altında kullan.

### 2.1 Ana panel (kenar panel)

| Unity GameObject / Bileşen | Türkçe adı / Metin | Açıklama |
|----------------------------|--------------------|----------|
| **Panel** | `SenaryoDurumPaneli` | Kenarda (örn. sol üst veya sağ üst) duran ana panel. RectTransform ile konumla. |
| **Panel (iç)** | (içerik doğrudan SenaryoDurumPaneli altında da olabilir) | — |

### 2.2 Panel içindeki metinler (TextMeshPro - TMP_Text)

| GameObject adı | Varsayılan metin (etiket) | Script’te referans adı | Açıklama |
|----------------|---------------------------|-------------------------|----------|
| `MevcutAsamaMetni` | Mevcut aşama: 1 - Isındırma / Umut | mevcutAsamaMetni | Hangi senaryo aşamasında olduğumuz. |
| `TamamlananSartlarMetni` | Tamamlanan: — | tamamlananSartlarMetni | Bu aşamada sağlanan çıkış şartları (örn. "Spin ≥ 80 ✓"). |
| `KalanSartlarMetni` | Kalan: — | kalanSartlarMetni | Aşamadan çıkmak için henüz sağlanmayan şartlar. |
| `CikisIcinBilgiMetni` | Çıkmak için en az 2 şart gerekli | cikisIcinBilgiMetni | Kural bilgisi (isteğe bağlı sabit metin). |

### 2.3 Manuel geçiş ayarı (geliştirme)

| GameObject adı | Bileşen | Script’te referans | Açıklama |
|----------------|---------|---------------------|----------|
| `GelistirmeModuToggle` | Toggle | gelistirmeModuToggle | "Geliştirme modu" – açıkken otomatik geçişler isteğe bağlı kapatılabilir / manuel geçiş aktif. (Mevcut SenaryoYoneticisi’ndeki gelistirmeToggle ile aynı mantık.) |
| `ManuelAsamaDropdown` | TMP_Dropdown | manuelAsamaDropdown | Seçenekler: "1 - Isındırma / Umut", "2 - Kontrol bende", "3 - Az daha / Kayıp kovalama", "4 - Bakiye tükenişi", "5 - Bonus zirve", "6 - Gerçek kayıp", "7 - Finale". |
| `AsamayaGecButonu` | Button | asamayaGecButonu | Buton metni: **"Bu Aşamaya Geç"**. Tıklanınca seçilen aşamaya manuel geçiş yapar. |
| `SenaryoAktifToggle` | Toggle | senaryoAktifToggle | "Senaryo aktif" – senaryo mantığının çalışıp çalışmaması. (Mevcut senaryoAktifToggle ile aynı.) |

### 2.4 Opsiyonel: paneli gizle/göster

| GameObject adı | Bileşen | Açıklama |
|----------------|---------|----------|
| `PaneliKucultButonu` | Button | Metin: "Gizle" / "Göster". Paneli küçültür veya tekrar açar. |

---

## 3. Script tarafı referansları (SenaryoYoneticisi)

SenaryoYoneticisi içinde kullanılacak alan isimleri (Türkçe, Inspector’da atanacak):

- **Mevcut aşama / şart metinleri:**  
  `mevcutAsamaMetni`, `tamamlananSartlarMetni`, `kalanSartlarMetni`, `cikisIcinBilgiMetni`
- **Manuel geçiş:**  
  `manuelAsamaDropdown`, `asamayaGecButonu`
- **Zaten var:**  
  `senaryoAktifToggle`, `gelistirmeModuToggle`, `mevcutAsamaText`, `gecisSartText`, `manuelGecisButonlari`  
  (İstersen mevcutAsamaText yerine veya ek olarak mevcutAsamaMetni kullanılacak; gecisSartText yerine tamamlanan/kalan şartlar ayrı ayrı doldurulacak.)
- **Hoşgeldiniz metni:**  
  Sol üstte "Hoşgeldiniz [kullanıcı adı]" yazacak TMP_Text. Unity’de adı "HoşgeldinizText" veya "KullaniciAdiText" olan (veya şu an "New Text" yazan) metni **SenaryoYoneticisi** Inspector’da **hosgeldinizText** alanına sürükleyin.
- **Mevcut ayarlar metni:**  
  SenaryoDurumPaneli içine eklediğiniz **MevcutAyarlarMetni** TMP_Text’i **SenaryoYoneticisi** Inspector’da **mevcutAyarlarMetni** alanına sürükleyin. Kod, oyunda **gerçekten kullanılan** değerleri (OyunYoneticisi’nden) otomatik doldurur: zorluk, scatter düşme %, scatter eşik (bonus için), max scatter/spin, tumble min (aynı sembol sayısı), çarpan düşme % (açık/kapalı), bahis (TL).

**Önemli:** Senaryolu sahnede **hiçbir ayar slider’ı ve admin panel yoktur**. Tüm ayar değerleri sahne config’inden (TumbleAyarlari, CarpanAyarlari, OyunYoneticisi vb.) gelir. MevcutAyarlarMetni ve diğer “mevcut durum” metinleri **seçili aşama ve o an geçerli olan bu değerlere göre** güncellenir; slider ile değiştirme yapılmaz.
- **Oturum verisi:**  
  Uygulama kapatılıp açıldığında, giriş ekranından seçilen kullanıcının kayıtlı spin sayısı, bakiye, toplam kazanç/kayıp değerleri SenaryoYoneticisi tarafından GameManager profilden okunup ekranda gösterilir (Start’ta yükleme yapılır).

---

## 4. Log yapısı

### 4.1 Senaryo olay kaydı (SenaryoOlayKaydi)

Her senaryo olayı (aşama girişi, aşama çıkışı, şart sağlandı, bakiye yükleme, vb.) için tek kayıt.

- **Alanlar (Türkçe düşünülebilir, kodda İngilizce de olabilir; API Türkçe tutulacak):**
  - `zaman` (DateTime veya string ISO)
  - `spinIndex` (o andaki toplam spin)
  - `asamaNo` (1–7)
  - `asamaAdi` (Türkçe aşama adı)
  - `olayTipi` (örn. "AsamaGirisi", "AsamaCikisi", "SartTamamlandi", "BakiyeYukleme", "ManuelGecis")
  - `aciklama` (kısa açıklama, Türkçe)
  - `bakiye`, `toplamYatirilan`, `netZarar` (o anki değerler; isteğe bağlı)

### 4.2 Nerede tutulacak?

- **SenaryoYoneticisi** içinde `List<SenaryoOlayKaydi> oturumLogu`.
- Aşama değişince, önemli olaylarda (yükleme, manuel geçiş, vb.) listeye ekle.
- Log sahnesi (04_LogScane) açılırken **SenaryoYoneticisi.I.GetOturumLogu()** ile bu listeyi alıp gösterebilir (bu kısım ileride Log sahnesi geliştirilirken bağlanacak).

---

## 5. Uygulama adımları (kod tarafı)

1. **SenaryoOlayKaydi sınıfı**  
   Yeni dosya: `Assets/Scripts/SenaryoOlayKaydi.cs`. Alanlar: zaman, spinIndex, asamaNo, asamaAdi, olayTipi, aciklama, (isteğe bağlı) bakiye, toplamYatirilan, netZarar.

2. **SenaryoYoneticisi güncellemesi**
   - Aşamaları **7 aşamaya** çıkar (ONERI_SENARYO_AKIS_TABLOSU.md’deki 1–7).
   - Her aşama için **çıkış şartlarını** (en az 2 şart) ve her bir şartın “şu an sağlandı mı?” bilgisini hesaplayan mantık.
   - **Tamamlanan şartlar** / **Kalan şartlar** metinlerini güncelleyen metod (örn. `SartMetinleriniGuncelle()`).
   - **Manuel geçiş:** `ManuelAsamaDropdown` + "Bu Aşamaya Geç" butonu → seçilen aşamaya geçip log’a "ManuelGecis" yazma.
   - Her **AsamaGecir** ve önemli olayda **SenaryoOlayKaydi** ekleme.
   - Yeni UI alanları: `mevcutAsamaMetni`, `tamamlananSartlarMetni`, `kalanSartlarMetni`, `cikisIcinBilgiMetni`, `manuelAsamaDropdown`, `asamayaGecButonu` (ve varsa `paneliKucultButonu`).

3. **Sahne kurulumu (Unity)**  
   - 02_SenaryoluOyun sahnesinde **SenaryoDurumPaneli** ve içindeki tüm öğeleri yukarıdaki isimlerle oluştur.
   - SenaryoYoneticisi bileşenine bu yeni alanları Inspector’dan bağla.

4. **Log sahnesi entegrasyonu** (sonraki aşama)  
   - SenaryoYoneticisi’nden `GetOturumLogu()` ile listeyi alıp 04_LogScane’de göstermek.

---

## 6. Özet – Unity’de yapman gerekenler (isim listesi)

- **Panel:** SenaryoDurumPaneli  
- **Metinler:** MevcutAsamaMetni, TamamlananSartlarMetni, KalanSartlarMetni, CikisIcinBilgiMetni  
- **Toggle:** GelistirmeModuToggle, SenaryoAktifToggle  
- **Dropdown:** ManuelAsamaDropdown (seçenekler: 1–7 aşama adları)  
- **Buton:** AsamayaGecButonu (metin: "Bu Aşamaya Geç")  
- **Opsiyonel:** PaneliKucultButonu ("Gizle" / "Göster")  

Tüm metinler ve etiketler Türkçe; script’teki `[SerializeField]` veya `public` alan isimleri yukarıdaki tablolardaki referans adlarıyla eşleşecek.

---

## 7. Unity’de adım adım panel kurulumu

1. **02_SenaryoluOyun** sahnesini aç.
2. Canvas (veya uygun kök) altında **UI → Panel** ile yeni panel oluştur; adını **SenaryoDurumPaneli** yap. Kenara (örn. sol üst) taşı.
3. Panel içinde **UI → Text - TextMeshPro** ile dört metin oluştur; GameObject adları: **MevcutAsamaMetni**, **TamamlananSartlarMetni**, **KalanSartlarMetni**, **CikisIcinBilgiMetni**. Varsayılan metinler sırayla: "Mevcut aşama: —", "Tamamlanan: —", "Kalan: —", "Çıkmak için en az 2 şart gerekli".
4. **UI → Toggle** ekle; adı **GelistirmeModuToggle** (veya mevcut geliştirme toggle’ı kullan). Label: "Geliştirme modu".
5. **UI → Dropdown - TMP** ekle; adı **ManuelAsamaDropdown**. Seçenekler script Start’ta 1–7 aşama adlarıyla doldurulur.
6. **UI → Button** ekle; adı **AsamayaGecButonu**. Buton içindeki TMP metni: **Bu Aşamaya Geç**.
7. Sahnedeki **SenaryoYoneticisi** bileşenine gidip Inspector’da **UI - Senaryo durum paneli** bölümündeki alanları bu objelere sürükleyip bırak.
8. Çalıştırıp kenar panelinde mevcut aşama, tamamlanan/kalan şartlar ve "Bu Aşamaya Geç" ile manuel geçişi test et.
