# Senaryolu Oyun Sahnesi – Admin ile Aynı Hiyerarşi

**Amaç:** 02_SenaryoluOyun sahnesi, 03_AdminOyunScene ile **aynı alt yapıyı** kullansın. Böylece tek bir oyun motoru (OyunYoneticisi, slot, ekonomi, tumble, bonus) iki sahnede de aynı şekilde çalışır; sadece senaryo sahnesinde SenaryoYoneticisi ve senaryo paneli eklenir.

---

## 1. Hedef: Senaryolu sahnede olması gereken kök hiyerarşi

02_SenaryoluOyun sahnesinin **kök (root)** seviyesinde aşağıdaki yapı olmalı — admin sahnesiyle bire bir aynı, artı senaryoya özel tek kök obje:

```
02_SenaryoluOyun (sahne)
├── Main Camera
├── OyunYoneticisi          ← Oyun motoru (slot, ekonomi, tumble, bonus)
├── EventSystem
├── Canvas                  ← Tüm UI burada (admin’deki gibi)
├── Directional Light
├── KasaSistemi
├── TumbleAyarlari
├── CarpanAyarlari
├── BonusAyarlari
├── OdulHavuzuAyarlari
└── SenaryoYoneticisi       ← Sadece senaryolu sahnede (admin’de yok)
```

**Özet:** Admin sahnesindeki tüm kök objeler (Camera, OyunYoneticisi, EventSystem, Canvas, Light, KasaSistemi, TumbleAyarlari, CarpanAyarlari, BonusAyarlari, OdulHavuzuAyarlari) senaryo sahnesinde de aynı isim ve rol ile bulunmalı. Ek olarak sadece **SenaryoYoneticisi** kök seviyede eklenir.

---

## 2. Canvas altı – Admin ile aynı + senaryo paneli

Canvas’ın altında admin sahnesindeki **aynı** UI öğeleri olmalı; böylece OyunYoneticisi aynı referans isimleriyle (BakiyeText, SlotGrid, ButtonCevir, vb.) çalışır. Ek olarak senaryo paneli eklenir.

**Admin’deki Canvas çocukları (senaryo sahnesinde de olmalı):**

- BakiyeText  
- SlotGrid  
- ButtonCevir  
- DimBg  
- baslik  
- BonusStartPanel  
- BonusEndPanel  
- YoneticiButton  
- AdminPasswordPanel  
- AdminSettingsPanel  
- bahisAzaltButon  
- bahisArttirButon  
- BahisText  
- OturumKazancText  
- BonusSatinAlButton  
- BakiyeYukleButon  
- ParaCekButon  
- HakText  
- BonusBuyConfirmPanel  
- CarpanDrop  
- CarpanTarget  
- Image (arka plan vb.)  
- BakiyeYuklePanel  
- ParaCekPanel  
- BtnLogScene  
- TxtHosgeldiniz  
- OtomatikSpinButton  
- OtomatikSpinPanel  
- KalanSpinText  

**Sadece senaryo sahnesinde Canvas altına eklenecek:**

- **SenaryoDurumPaneli** (içinde: MevcutAsamaMetni, TamamlananSartlarMetni, KalanSartlarMetni, CikisIcinBilgiMetni, GelistirmeModuToggle, ManuelAsamaDropdown, AsamayaGecButonu)

İstersen senaryo oynanırken **YoneticiButton**, **AdminPasswordPanel**, **AdminSettingsPanel** objelerini başlangıçta **SetActive(false)** ile gizleyebilirsin; böylece aynı sahne yapısı kalır ama kullanıcı admin panellerini görmez.

---

## 3. Nasıl eşitleyebilirsin?

### Yöntem A (Önerilen): Admin sahnesini kopyalayıp senaryo sahnesi yapmak

1. **Project** penceresinde **Assets/Scenes/03_AdminOyunScene** sahnesine sağ tıkla → **Duplicate**.  
2. Kopyalanan sahnenin adını **02_SenaryoluOyun** yap.  
3. **02_SenaryoluOyun**’u aç.  
4. **Kök seviyede:**  
   - **Create Empty** → adını **SenaryoYoneticisi** yap.  
   - Bu objeye **SenaryoYoneticisi** script’ini ekle (Assets/Scripts/SenaryoYoneticisi.cs).  
5. **Canvas** altında:  
   - Senaryo durum paneli için **UI → Panel** oluştur, adı **SenaryoDurumPaneli**.  
   - İçine metinleri ve manuel geçiş kontrollerini ekle (MevcutAsamaMetni, TamamlananSartlarMetni, KalanSartlarMetni, CikisIcinBilgiMetni, GelistirmeModuToggle, ManuelAsamaDropdown, AsamayaGecButonu).  
6. **SenaryoYoneticisi** objesindeki **SenaryoYoneticisi** bileşeninde, Inspector’da “UI - Senaryo durum paneli” alanlarını bu paneldeki objelere bağla.  
7. (İsteğe bağlı) Senaryo modunda admin panellerini gizlemek için:  
   - YoneticiButton, AdminPasswordPanel, AdminSettingsPanel’i varsayılan olarak **inactive** bırak veya sahne açılışında bir script ile **SetActive(false)** yap.

Bu yöntemle 02_SenaryoluOyun, 03_AdminOyunScene ile **aynı hiyerarşi ve altyapıyı** kullanır; tek fark SenaryoYoneticisi ve SenaryoDurumPaneli eklenmiş olur.

### Yöntem B: Mevcut 02_SenaryoluOyun’u admin’e benzetmek

Mevcut senaryo sahnesini koruyacaksan:

1. 03_AdminOyunScene’deki **kök objeleri** (OyunYoneticisi, KasaSistemi, TumbleAyarlari, CarpanAyarlari, BonusAyarlari, OdulHavuzuAyarlari) 02_SenaryoluOyun’da yoksa **aynı isimle** oluştur veya admin sahnesinden **Copy Component / Paste** veya prefab kullanarak taşı.  
2. **Canvas** altında admin’de olup senaryo sahnesinde eksik olan her UI öğesini (BakiyeText, SlotGrid, ButtonCevir, paneller, butonlar vb.) admin sahnesinden referans alarak senaryo sahnesine ekle; isimleri admin ile **bire bir aynı** olsun.  
3. Senaryo sahnesine **SenaryoYoneticisi** kök objesini ve **SenaryoDurumPaneli**’ni yukarıdaki gibi ekle ve bağla.

Bu yol daha uzun sürer; **Yöntem A** genelde daha az hata ve aynı altyapıyı garanti eder.

---

## 4. Kontrol listesi

Senaryo sahnesini açtığında Hierarchy’de şunları kontrol et:

- [ ] **OyunYoneticisi** kök seviyede var.  
- [ ] **KasaSistemi**, **TumbleAyarlari**, **CarpanAyarlari**, **BonusAyarlari**, **OdulHavuzuAyarlari** kök seviyede var.  
- [ ] **Canvas** altında admin’deki UI öğeleri (BakiyeText, SlotGrid, ButtonCevir, BahisText, paneller, butonlar vb.) aynı isimlerle var.  
- [ ] **SenaryoYoneticisi** kök seviyede var ve script atanmış.  
- [ ] **Canvas** altında **SenaryoDurumPaneli** var; içinde senaryo metinleri ve manuel geçiş kontrolleri var.  
- [ ] SenaryoYoneticisi bileşeninde panel referansları (MevcutAsamaMetni, TamamlananSartlarMetni, KalanSartlarMetni, CikisIcinBilgiMetni, ManuelAsamaDropdown, AsamayaGecButonu) atanmış.

Bu dokümandaki hiyerarşi ve adımlar uygulandığında, senaryolu oyun sahnesi admin oyun sahnesiyle **aynı alt yapıyı** kullanmış olur; tek fark SenaryoYoneticisi ve SenaryoDurumPaneli’dir.
