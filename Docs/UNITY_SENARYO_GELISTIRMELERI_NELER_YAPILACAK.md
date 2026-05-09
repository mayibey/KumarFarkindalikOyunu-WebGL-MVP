# Senaryo Geliştirmeleri – Unity’de Ne Yapman Gerekiyor?

Bu rehber, senaryo aşama zorlukları (5–11), esnek zorluk kuralları ve takip mekanizmalarının **kod tarafında** yapıldığını varsayar. Aşağıda **sadece Unity Editor’da senin yapman gerekenler** adım adım listelenmiştir.

---

## Özet: Kodda Ne Değişti?

- **Aşama bazlı zorluk:** Aşama 1→5, 2→6, 3→7, 4→8, 5→9, 6→10, 7→11 (spec tablosu).
- **Esnek zorluk:**  
  - Aşama 2’de bahis artırımından sonra **3–4 spin** zorluk 5.  
  - **Yükleme sonrası ilk 20 spin** tüm aşamalarda zorluk en fazla 6.
- **Takip:** `sonBahisArtirimSpinIndex`, `yuklemeSonrasiIlkSpinIndex` (Inspector’da görünür; otomatik doldurulur).
- **Bahis artırımı:** Bahis artır butonuna basıldığında `SenaryoYoneticisi.BahisArtirimiYapildi()` otomatik çağrılıyor (OyunYoneticisi üzerinden).
- **Bakiye yükleme:** `BakiyeYukle(tutar)` çağrıldığında `yuklemeSonrasiIlkSpinIndex` kaydediliyor; sen sadece **bakiye yükleme akışını** SenaryoYoneticisi ile bağlaman gerekiyor (aşağıda).

---

## 1. Sahne: 02_SenaryoluOyun

### 1.1 Hiyerarşi (kök seviye)

Senaryolu oyun sahnesinde **kökte** şunlar olmalı (admin sahnesiyle aynı yapı + SenaryoYoneticisi):

| Kök obje | Gerekli mi? | Not |
|----------|-------------|-----|
| Main Camera | Evet | |
| OyunYoneticisi | Evet | Oyun motoru (slot, ekonomi, tumble, bonus). |
| EventSystem | Evet | |
| Canvas | Evet | Tüm UI burada. |
| Directional Light | Evet | |
| KasaSistemi | Evet | |
| TumbleAyarlari | Evet | |
| CarpanAyarlari | Evet | |
| BonusAyarlari | Evet | |
| OdulHavuzuAyarlari | Evet | |
| **SenaryoYoneticisi** | Evet | Sadece senaryolu sahnede; boş GameObject’e SenaryoYoneticisi script’i ekle. |

**Nasıl yaparsın:**  
- İstersen **03_AdminOyunScene**’i duplicate edip adını **02_SenaryoluOyun** yap.  
- Senaryo sahnesinde **kök seviyede** boş bir GameObject oluştur, adını **SenaryoYoneticisi** koy, **SenaryoYoneticisi** script’ini (Assets/Scripts/SenaryoYoneticisi.cs) bu objeye ekle.  
- Admin’e özel objeleri (YoneticiButton, AdminPasswordPanel, AdminSettingsPanel vb.) **gizleyebilirsin**: Inspector’da **Active** işaretini kapat veya başlangıçta `SetActive(false)` ile kapat.

---

## 2. Canvas Altında Senaryo Paneli

### 2.1 Panel ve içeriği

Canvas altında **SenaryoDurumPaneli** adında bir Panel oluştur (RectTransform ile istediğin yere – örn. sol üst – konumla). Bu panelin **içinde** aşağıdaki öğeleri oluştur ve isimlendir:

| GameObject adı | Bileşen | Kısa açıklama |
|----------------|---------|----------------|
| **HoşgeldinizText** | TMP_Text | Sol üstte "Hoşgeldiniz [kullanıcı adı]". |
| **MevcutAsamaMetni** | TMP_Text | "Mevcut aşama: 1 - Isındırma / Umut" gibi. |
| **TamamlananSartlarMetni** | TMP_Text | Tamamlanan şartlar listesi. |
| **KalanSartlarMetni** | TMP_Text | Kalan şartlar listesi. |
| **CikisIcinBilgiMetni** | TMP_Text | "Çıkmak için en az 2 şart gerekli". |
| **MevcutAyarlarMetni** | TMP_Text | Zorluk, scatter %, çarpan %, ödenebilir tutar (kod doldurur). |
| **ManuelAsamaDropdown** | TMP_Dropdown | 1–7 arası aşama seçimi (kod seçenekleri doldurur). |
| **AsamayaGecButonu** | Button | Metin: "Bu Aşamaya Geç". |
| **SenaryoAktifToggle** | Toggle | "Senaryo aktif" (isteğe bağlı). |
| **GelistirmeModuToggle** | Toggle | "Geliştirme modu" (açıkken otomatik aşama geçişi kapalı). |

**Not:**  
- Toggle’lar ve dropdown **SenaryoYoneticisi** Start’ta doldurulur / dinlenir; sadece objeleri oluşturup Inspector’da bağlaman yeterli.  
- **AsamayaGecButonu** tıklanınca seçilen aşamaya geçer ve **zorluk o aşamaya göre (5–11)** güncellenir.

---

## 3. SenaryoYoneticisi Inspector Bağlamaları

**02_SenaryoluOyun** sahnesinde **SenaryoYoneticisi** objesini seç. Inspector’da **Senaryo Yoneticisi (Script)** bileşeninde aşağıdaki alanları **doğru UI öğelerine sürükleyip bırak** (veya Object Picker ile seç):

### 3.1 Senaryo durum paneli (kenar)

| Inspector alanı | Ne atanacak? |
|-----------------|--------------|
| **Mevcut Asama Metni** | SenaryoDurumPaneli içindeki `MevcutAsamaMetni` (TMP_Text). |
| **Tamamlanan Sartlar Metni** | `TamamlananSartlarMetni` (TMP_Text). |
| **Kalan Sartlar Metni** | `KalanSartlarMetni` (TMP_Text). |
| **Cikis Icin Bilgi Metni** | `CikisIcinBilgiMetni` (TMP_Text). |
| **Manuel Asama Dropdown** | `ManuelAsamaDropdown` (TMP_Dropdown). |
| **Asamaya Gec Butonu** | `AsamayaGecButonu` (Button). |
| **Hosgeldiniz Text** | `HoşgeldinizText` (TMP_Text). |
| **Mevcut Ayarlar Metni** | `MevcutAyarlarMetni` (TMP_Text). |

### 3.2 Toggle’lar (isteğe bağlı ama önerilir)

| Inspector alanı | Ne atanacak? |
|-----------------|--------------|
| **Senaryo Aktif Toggle** | `SenaryoAktifToggle` (Toggle). |
| **Gelistirme Toggle** | `GelistirmeModuToggle` (Toggle). |

### 3.3 Diğer (varsa)

- **Asama Text**, **Spin Text**, **Kazanc Text**, **Bakiye Text**: Senaryolu sahnede ekstra bir istatistik köşesi kullanıyorsan ilgili TMP_Text’leri atayabilirsin; yoksa boş bırakabilirsin.  
- **Gecis Sart Text**: Eski tek metinli şart alanı; istersen aynı paneldeki bir TMP_Text’e atayabilirsin veya boş bırak.  
- **Auto Spin** (dropdown, butonlar, kalan metin): Senaryolu sahnede otomatik spin kullanacaksan ilgili referansları at; kullanmayacaksan boş bırak.

**Kontrol:**  
- Oyun çalışırken **Mevcut Ayarlar Metni** içinde **Zorluk: 5** … **Zorluk: 11** arası değer görünmeli.  
- **Manuel Asama Dropdown**’dan aşama seçip **Bu Aşamaya Geç**’e bastığında hem metinler hem zorluk (Mevcut Ayarlar’da) güncellenmeli.

---

## 4. Bakiye Yükleme → SenaryoYoneticisi Bağlantısı

**Kod tarafında yapıldı:** `EkonomiServisi.OnBakiyeYukleOnay()` içinde, bakiye eklendikten sonra `SenaryoYoneticisi.I?.BakiyeYukle(miktar)` çağrılıyor. **Unity’de ekstra bir bağlantı yapmana gerek yok.**

Böylece:

- `yuklemeSayisi` artar,  
- `yuklemeSonrasiIlkSpinIndex` kaydedilir,  
- **Yükleme sonrası ilk 20 spin** için esnek zorluk (en fazla 6) uygulanır.

**Unity’de yapman gereken:**  
- **BakiyeYuklePanel** ve **BakiyeYukleOnayButon**’un OyunYoneticisi’nde (veya sahne bağlama ile) doğru atanmış olması yeterli; onay butonuna basıldığında zaten EkonomiServisi.OnBakiyeYukleOnay() çalışır ve SenaryoYoneticisi güncellenir.

**Kontrol:**  
- Oyunda bakiye yükle, sonra birkaç spin at.  
- **Mevcut Ayarlar**’da zorluk bir süre 6 veya altı kalıyorsa, yükleme sonrası 20 spin penceresi çalışıyor demektir.

---

## 5. OyunYoneticisi ve Diğer Referanslar (Senaryolu Sahne)

- **OyunYoneticisi**: Sahnedeki **OyunYoneticisi** objesinin **referansları** (BakiyeText, ButtonCevir, bahisArttirButon, bahisAzaltButon, SlotGrid, BonusStartPanel, BonusEndPanel, BakiyeYuklePanel vb.) admin sahnesindeki gibi **dolu** olmalı; çünkü senaryo sahnesi aynı oyun motorunu kullanır.  
- **Bahis artırımı:** Bahis artır butonu zaten **OyunYoneticisi**’ne bağlı; OyunYoneticisi bahis artırınca `SenaryoYoneticisi.I?.BahisArtirimiYapildi()` otomatik çağrılıyor. **Ekstra bir şey yapmana gerek yok.**  
- **KasaSistemi, TumbleAyarlari, CarpanAyarlari, BonusAyarlari, OdulHavuzuAyarlari**: Admin’de nasıl atanmışsa senaryo sahnesinde de aynı şekilde atanmış olmalı (özellikle OyunYoneticisi’nin Inspector’ında).

---

## 6. Kontrol Listesi (Unity’de)

Sahneyi açıp oynarken şunları kontrol et:

- [ ] Kökte **SenaryoYoneticisi** objesi var ve **SenaryoYoneticisi** script’i ekli.  
- [ ] **SenaryoDurumPaneli** Canvas altında; içinde **MevcutAsamaMetni**, **TamamlananSartlarMetni**, **KalanSartlarMetni**, **CikisIcinBilgiMetni**, **MevcutAyarlarMetni**, **ManuelAsamaDropdown**, **AsamayaGecButonu** var.  
- [ ] **SenaryoYoneticisi** Inspector’da bu metinler, dropdown ve buton **atanmış**.  
- [ ] Oyun başlayınca **HoşgeldinizText** “Hoşgeldiniz [kullanıcı adı]” gösteriyor.  
- [ ] **Mevcut Ayarlar Metni** zorluk, scatter %, çarpan %, ödenebilir tutar gösteriyor ve her spin sonrası güncelleniyor.  
- [ ] **Manuel Asama Dropdown**’dan aşama seçip **Bu Aşamaya Geç**’e basınca **Mevcut Asama** ve **Mevcut Ayarlar**’daki zorluk (5–11) değişiyor.  
- [ ] Bakiye yükleme panelinden onay verildiğinde **SenaryoYoneticisi** güncelleniyor (EkonomiServisi içinde zaten çağrı var; BakiyeYuklePanel/Onay butonu referansları atanmış olmalı).  
- [ ] Normal oynanışta aşama geçişi ve şart metinleri güncelleniyor; bonus içinde de panel (ve ödenebilir tutar) güncelleniyor.

---

## 7. Spec Dokümanı

Aşama zorlukları ve yazılımsal ayarların tam tanımı için:  
**Docs/SENARYO_ASAMA_ZORLUK_VE_AYARLAR_SPEC.md**  
**Docs/SENARYO_ASAMALAR_VE_ZORLUK_TABLOSU.md**

Hiyerarşi ve panel isimleri için:  
**Docs/SENARYOLU_OYUN_HIYERARSI_ADMIN_ILE_ESIT.md**  
**Docs/SENARYOLU_OYUN_UNITY_PLANI.md**

---

## 8. Senaryo 5 – Zirve bonusu (50 spin, tek seferlik yüksek etki)

**Kod tarafında yapıldı:**

- 3. yükleme sonrası girilen **ilk** bonus oyunda: maliyet × 2,5 tavan, **50 spin** (varsayılan; Inspector’dan değiştirilebilir).
- Bu bonus **tek seferlik**; sonraki bonuslarda kazançlar hızla düşer (cap ≈ maliyet × 0,20).

**Animasyon / efekt (istersen sen yaparsın):**

- **OyunYoneticisi** Inspector’da **Senaryo 5 - Zirve bonusu** bölümünde:
  - **Senaryo5 Zirve Bonus Spin Sayisi**: 50 (istersen değiştir).
  - **OnZirveBonusBasladi** / **OnZirveBonusBitti**: C# `Action`; kodla dinleyip kendi animasyonunu tetikleyebilirsin.
- **Kolay yol:** Sahnede bir GameObject’e **ZirveBonusAnimasyonTetikleyici** script’ini ekle (Assets/Scripts/ZirveBonusAnimasyonTetikleyici.cs). Inspector’da:
  - **Zirve Basladi Animator** veya **Zirve Basladi Animasyon**: Zirve bonusu **başlarken** (50 spin girişi) oynatılacak Animator/Animation.
  - **Zirve Bitti Animator** veya **Zirve Bitti Animasyon**: Zirve bonusu **bitince** (yüksek kazanç ekranı) oynatılacak Animator/Animation.
- Animator kullanıyorsan **Trigger** adlarını yaz (varsayılan: `ZirveBasladi`, `ZirveBitti`). Animation kullanıyorsan bileşene varsayılan clip atanırsa otomatik oynatılır.

**Manuel yapman gereken:** Sadece istediğin animasyonu/efekti (particle, büyüyen metin, alkış sesi vb.) oluşturup yukarıdaki script’e atamak; tetikleme kodu hazır.

Bu rehberi tamamladıktan sonra senaryo sahnesi, aşama bazlı zorluk (5–11), esnek zorluk (bahis artırımı + yükleme sonrası 20 spin) ve takip değişkenleriyle uyumlu çalışır.
