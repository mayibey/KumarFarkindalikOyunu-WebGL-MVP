# Services — Birleştirme Analizi

Ciddi kafa yorup mantıklı birleştirme adaylarını çıkardım. Sadece **aynı domain + tek sorumluluk bütünü** oluşanlar önerildi.

---

## 1. Önerilen (yüksek mantık)

### A) LogServisi + DonusKayitServisi → **KayitServisi** (veya LogServisi genişletilmiş)

| | LogServisi | DonusKayitServisi |
|--|------------|-------------------|
| **Ne yapıyor** | `KayitEkonomi`: GameManager.RecordEconomyAction + Log. | `RecordSpinStart`, `RecordSpinResult`, `RecordBonusSpin`, `RecordBonusEnd`: hepsi içeride **LogServisi.KayitEkonomi** çağırıyor + OnSpinStart/OnSpinSettled/OnSpinResult tetikliyor. |
| **Kullanım** | EkonomiServisi, OY (bonus buy), DonusKayitServisi. | DonusAkisServisi (IDonusAkisBaglami.DonusKayitServisi), OY (spin start/result, bonus end). |

**Neden birleşir:** İkisi de “kayıt/log” alanında. DonusKayitServisi zaten tek iş olarak LogServisi’i kullanıyor; kendi metotları sadece parametre toplayıp KayitEkonomi + event çağırıyor. Tek sınıfta toplamak: hem ekonomi kaydı hem spin/bonus kaydı aynı serviste; EkonomiServisi ve OY aynı “KayitServisi” (veya genişletilmiş LogServisi) ile konuşur, DonusAkisServisi de artık DonusKayitServisi yerine bu servisi kullanır (interface’te DonusKayitServisi → KayitServisi/LogServisi olur).

**Etki:** 2 dosya → 1. Davranış aynı kalır; sadece DonusKayitServisi’nin gövdesi LogServisi’e taşınır, DonusKayitServisi silinir ve çağrılar yeni isme göre güncellenir.

---

### B) BonusUIAkisServisi + BonusSatinAlmaAkisServisi → **BonusUIServisi** (veya BonusAkisServisi)

| | BonusUIAkisServisi | BonusSatinAlmaAkisServisi |
|--|-------------------|---------------------------|
| **Ne yapıyor** | Bonus başlangıç/bitiş paneli: ShowBonusStartMessage, ShowBonusEndMessage (fade, TMP, ses). | Bonus satın alma onay: maliyet kontrolü, panel göster/gizle, OnYes/OnNo, onConfirmed callback. |
| **Kullanım** | OY: coroutine’lerde ShowBonusStartMessage / ShowBonusEndMessage. | OY: SetShowConfirmPanel/HideConfirmPanel/OnConfirmed, BonusSatinAlRequested, OnYes/OnNo; UIServisi panel aç/kapa delegasyonu. |

**Neden birleşir:** İkisi de “bonus UI akışı”: biri başlangıç/bitiş mesajı, diğeri satın alma onayı. OY zaten ikisini de oluşturup bağlıyor. Tek **BonusUIServisi** (veya BonusAkisServisi) altında: “bonus start/end” + “bonus buy confirm” metotları toplanır; OY tek servis oluşturur, arayüzler (varsa) buna göre sadeleştirilir.

**Etki:** 2 dosya → 1. Davranış aynı kalır.

---

## 2. İsteğe bağlı (orta mantık)

### C) ScatterEfektServisi + AnimasyonServisi

- **ScatterEfektServisi:** Scatter hücrelerini büyütme (scale) coroutine’i; context ile grid/scatter index alıyor.
- **AnimasyonServisi:** Drop, pop, çarpan şişme animasyonları.

İkisi de “görsel efekt”. Scatter efektini AnimasyonServisi’e taşıyıp (ör. `ScatterBuyutEfektiCalistir` + gerekli context setter’lar) ScatterEfektServisi’i kaldırmak mümkün. Dezavantaj: AnimasyonServisi büyür ve hem grid/cell hem scatter context’i yönetir; bağımlılık sayısı artar. **İstersen yapılabilir**, zorunlu değil.

---

## 3. Birleştirme önerilmez

| Servis / çift | Neden |
|---------------|--------|
| **UIServisi + OyunUIGuncellemeServisi** | Biri ince delegasyon katmanı, diğeri gerçek UI güncelleme mantığı; birleştirince OyunUIGuncellemeServisi hem router hem mantık taşır, tek sorumluluk bozulur. |
| **TumbleServisi + TumbleAkisServisi** | Biri “tumble işlemleri” delegasyonu, diğeri döngü orkestrasyonu; CokmeAkisServisi de TumbleServisi kullanıyor. Birleştirme akışı karıştırır. |
| **IzgaraServisi + IzgaraBaslatmaServisi** | Biri runtime (RenderAllSprites, ScatterSay, sembol seçimi), diğeri tek seferlik init; farklı yaşam döngüsü. |
| **SenaryoServisi** | Saf wrapper, çok sayıda delegate; başka bir servise katmak sadece o servisi şişirir. |
| **OyunFormatServisi** | Static, tek metod; ayrı kalması daha okunabilir. |
| **OyunKorumaServisi** | Zaten static sabit/yardımcı; taşınacak olsa “sabitler” sınıfına gider, servis birleştirmesi değil. |
| **DonusServisi + DonusAkisServisi** | Biri “tek dönüş tetikle”, diğeri “normal/bonus döngüsü”; API ve kullanım yerleri farklı, birleştirme kafa karıştırır. |
| **ZorlukServisi, KorutinServisi, SahneBaglamaServisi, AdminAyarUIServisi, OdemeServisi, EkonomiServisi, CarpanServisi, CarpanOverlayServisi, HizVeSesServisi** | Hepsi tek/net rol; doğal bir “eş” yok, zorla birleştirmek ya god class ya da alakasız sorumluluk karışımı olur. |

---

## Özet

| Öncelik | Birleştirme | Dosya | Sonuç |
|---------|-------------|--------|--------|
| **1** | LogServisi + DonusKayitServisi | 2 → 1 (KayitServisi / LogServisi) | Kayıt/log tek yerde. |
| **2** | BonusUIAkisServisi + BonusSatinAlmaAkisServisi | 2 → 1 (BonusUIServisi) | Bonus UI akışı tek yerde. |
| **İsteğe bağlı** | ScatterEfektServisi → AnimasyonServisi | 2 → 1 | Görsel efekt tek sınıf; AnimasyonServisi büyür. |

En mantıklı ve düşük riskli adımlar **1 ve 2**. İstersen sırayla uygulayalım; önce Log+DonusKayit, sonra Bonus UI ikilisi.
