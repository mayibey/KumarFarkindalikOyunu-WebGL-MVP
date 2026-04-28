# Birleştirme Önerileri (OyunYoneticisi.cs hariç)

OyunYoneticisi dışında, mantıklı birleştirilebilecek dosyaların özeti.

---

## 1. Yüksek mantık — Tek sorumluluk, az dosya artışı

| Hedef | Birleştirilecek dosyalar | Gerekçe |
|------|---------------------------|--------|
| **SaveSystem → GameManager** | `SaveSystem.cs` | SaveSystem sadece `LoadProfiles` / `SaveProfiles`; sadece GameManager, AdminPanel ve EkonomiServisi kullanıyor. Static metodlar GameManager içine taşınabilir (veya GameManager içinde `SaveSystem` nested static sınıf). SaveSystem.cs silinir. |

**Etki:** 1 dosya azalır. Davranış aynı kalır.

---

## 2. Orta mantık — Aynı domain, tek servis

| Hedef | Birleştirilecek dosyalar | Gerekçe |
|------|---------------------------|--------|
| **BonusUIAkisServisi + BonusSatinAlmaAkisServisi** | İkisi tek sınıf | İkisi de “bonus UI akışı”: biri başlangıç/bitiş paneli, diğeri satın alma onay paneli. Tek `BonusUIServisi` veya `BonusAkisServisi` altında iki grup metod (start/end + buy confirm) toplanabilir. OY zaten ikisini de oluşturup bağlıyor. |

**Etki:** 2 servis dosyası → 1. Davranış değişmez.

| Hedef | Birleştirilecek dosyalar | Gerekçe |
|------|---------------------------|--------|
| **LogServisi + DonusKayitServisi** | İkisi tek sınıf | LogServisi: `KayitEkonomi`. DonusKayitServisi: spin/bonus kayıtları ve içeride LogServisi’yi kullanıyor. İkisi “kayıt/log” alanında. DonusKayitServisi’nin metotları LogServisi’ye taşınabilir (veya ortak isim `KayitServisi`). Baglamda artık tek servis verilir (IDonusAkisBaglami.DonusKayitServisi → KayitServisi / LogServisi). |

**Etki:** 2 servis dosyası → 1. EkonomiServisi ve DonusAkisServisi bağlamı güncellenir.

---

## 3. Düşük öncelik — Küçük dosyalar

| Hedef | Birleştirilecek dosyalar | Gerekçe |
|------|---------------------------|--------|
| **HosgeldinizText + SpinIconRotate** | Aynı .cs içinde iki sınıf | İkisi de küçük MonoBehaviour (birkaç satır). Aynı dosyada iki sınıf olarak tutulabilir; farklı GameObject’lere takılı kalır, sadece dosya sayısı 1 azalır. |

**Etki:** 2 dosya → 1 dosya (içinde 2 class).

---

## 4. Birleştirme önerilmez (şimdilik)

| Dosya / Grup | Neden |
|--------------|--------|
| **OyunFormatServisi** | Sadece `FormatTL`; birçok yerde kullanılıyor. Tek dosya, tek sorumluluk; birleştirince dağıtmak gereksiz. |
| **CarpanAyarlari / TumbleAyarlari / OdulHavuzuAyarlari / SesAyarlari** | Hepsi sahne config MonoBehaviour. Tek “OyunAyarlari” yapılabilir ama sahnedeki referanslar ve Inspector düzeni değişir; daha büyük refaktör. |
| **BonusAyarlari** | İncelemede: sadece config kısmı kullanılıyor; runtime kısmı silinebilir. “Başka dosyaya birleştir” yerine önce ölü kod temizliği mantıklı. |
| **UIServisi / SahneBaglamaServisi / OyunUIGuncellemeServisi** | Farklı sorumluluklar (delegasyon, bağlama, metin güncelleme). Birleştirmek OY’yi dağıtmak veya tek dev dosya yaratmak olur. |

---

## Özet sıra önerisi

1. **SaveSystem → GameManager** (tek dosya azalır, risk düşük).
2. **BonusUIAkisServisi + BonusSatinAlmaAkisServisi** (bonus UI tek yerde).
3. **LogServisi + DonusKayitServisi** (kayıt tek servis; baglam güncellemesi gerekir).
4. **HosgeldinizText + SpinIconRotate** (tek .cs, iki sınıf; isteğe bağlı).

İstersen sadece 1’i, ya da 1+2’yi uygulayıp davranışı koruyacak adımları tek tek yazabilirim.
