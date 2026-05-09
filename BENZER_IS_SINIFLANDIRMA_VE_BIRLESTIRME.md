# Benzer İş Yapan Dosyalar — Sınıflandırma ve Birleştirme Önerileri

Tüm script’ler **yaptıkları işe göre** gruplandı. Her grupta birleştirilebilecekler ayrıca işaretlendi.  
*(SesAyarlari şimdilik dokunulmuyor.)*

---

## 1. Veri / model (oyuncu, kayıt, istatistik)

| Dosya | İş | Birleştirme önerisi |
|-------|-----|----------------------|
| **PlayerProfile.cs** | Oyuncu verisi (bakiye, spin, kazanç, logs). | — |
| **StatsEntry.cs** | Tek istatistik satırı (onceki/sonraki bakiye, işlem, spin/bonus sayıları). | **→ PlayerProfile.cs** içine taşı. GameLogEntry zaten orada; aynı dosyada iki veri sınıfı olur. **-1 .cs** |
| **GameLogEntry** | PlayerProfile.cs içinde (log satırı). | Zaten birleşik. |

**Öneri:** StatsEntry sınıfını PlayerProfile.cs dosyasına taşı, StatsEntry.cs sil.

---

## 2. Global / kalıcılık

| Dosya | İş | Birleştirme önerisi |
|-------|-----|----------------------|
| **GameManager.cs** | Singleton: oyuncular, aktif oyuncu, bahis, sahne, RecordEconomyAction. | Ayrı kalsın. |
| **SaveSystem.cs** | Profilleri JSON yazar/okur. | Ayrı kalsın (tek sorumluluk). |

**Öneri:** Birleştirme yok.

---

## 3. Ana orkestrasyon

| Dosya | İş | Birleştirme önerisi |
|-------|-----|----------------------|
| **OyunYoneticisi.cs** | Tüm servisleri kurar, spin/bonus/tumble akışını yönetir. | Tek orkestratör; ayrı kalır. |

---

## 4. Sahne UI (giriş / admin / log)

| Dosya | İş | Birleştirme önerisi |
|-------|-----|----------------------|
| **GirisUI.cs** | Giriş sahnesi: kullanıcı seçimi, sahne geçişi. | Farklı sahne; birleştirme mantıklı değil. |
| **AdminPanel.cs** | Admin sahnesi: şifre, slider’lar, profiller. | Farklı sahne; ayrı kalır. |
| **LogYoneticisi.cs** | Log sahnesi: istatistikler, log listesi. | Farklı sahne; ayrı kalır. |

**Öneri:** Birleştirme yok (sahne bazlı ayrım).

---

## 5. Senaryo

| Dosya | İş | Birleştirme önerisi |
|-------|-----|----------------------|
| **SenaryoYoneticisi.cs** | MonoBehaviour; senaryo aşamaları, takip. | Sahne/state tarafı. |
| **SenaryoServisi.cs** | Zorluk, scatter, çarpan, bonus bütçe delegasyonu. | Servis tarafı. |

**Öneri:** Biri MB biri servis; birleştirme önerilmez.

---

## 6. Bonus (UI + akış)

| Dosya | İş | Birleştirme önerisi |
|-------|-----|----------------------|
| **BonusSatinAlUI.cs** | MonoBehaviour; bonus satın al onay paneli (Goster/Gizle, Evet/Hayır). | Sahne bileşeni; ayrı. |
| **BonusUIAkisServisi.cs** | Bonus başlangıç/bitiş paneli (ShowBonusStartMessage, ShowBonusEndMessage). | Panel akışı. |
| **BonusSatinAlmaAkisServisi.cs** | Bonus satın alma onay akışı (maliyet, panel, OnYes/OnNo). | Satın al akışı. |

**Öneri:** İki servis farklı akış (panel mesaj vs satın al). İstersen uzun vadede tek “BonusAkisServisi” altında toplanabilir; şu an ayrı kalabilir.

---

## 7. Ayar MonoBehaviour’lar (sahneden config)

| Dosya | İş | Birleştirme önerisi |
|-------|-----|----------------------|
| **CarpanAyarlari.cs** | Çarpan config + Force x2/x5 butonları. | Çok alan; EkonomiAyarlari gibi kaldırıp default’a çekmek büyük iş. |
| **TumbleAyarlari.cs** | PayTable, ScatterIndex (merkezi). | Birleştirme önerilmez. |
| **BonusAyarlari.cs** | Bonus hak, scatter, bütçe vb. | SyncFromAyarClasses’ta kullanılıyor. |
| **OdulHavuzuAyarlari.cs** | Havuz/kasa ayarları. | SyncFromAyarClasses’ta kullanılıyor. |
| **SesAyarlari.cs** | Ses kaynakları (Tumble, Bonus, müzik). | Şimdilik dokunulmuyor. |

**Öneri:** İleride hepsi tek bir **OyunAyarlari** (veya ScriptableObject) altında toplanabilir; şu an ayrı kalsın.

---

## 8. UI referans / bağlama / güncelleme

| Dosya | İş | Birleştirme önerisi |
|-------|-----|----------------------|
| **UIReferanslari.cs** | MonoBehaviour; UI referans konteyneri. | Sahne objesi; ayrı. |
| **SahneBaglamaServisi.cs** | Referansları sahneden bulup hedefe yazar (BindIfNeeded). | Ayrı. |
| **UIServisi.cs** | Delegasyon: UIAutoBaglaGerekirse, panel aç/kapa, UI_Guncelle vb. | İnce wrapper. |
| **OyunUIGuncellemeServisi.cs** | Bakiye/bahis/hak/kazanç/çarpan metinlerini günceller. | Gerçek güncelleme mantığı. |
| **AdminAyarUIServisi.cs** | Admin paneli slider/buton bağlama. | Admin’e özel. |

**Öneri:** UIServisi + OyunUIGuncellemeServisi sorumluluk olarak yakın ama biri “çağrı yönlendirme” biri “hesap/güncelleme”; birleştirmek OY’deki delegasyonu değiştirir. Şu an ayrı kalabilir.

---

## 9. Dönüş / spin akışı

| Dosya | İş | Birleştirme önerisi |
|-------|-----|----------------------|
| **DonusAkisServisi.cs** | Normal spin + bonus döngüsü orkestrasyonu. | Ana akış; ayrı. |
| **DonusServisi.cs** | Tek dönüş tetiklemesi (grid doldur, çarpan, TumbleLoop delegasyonu). | İnce; DonusAkisServisi’nin kullandığı yardımcı. |
| **DonusKayitServisi.cs** | Spin sonucu kaydı (totalWon/totalLost/totalNet callback). | Sadece kayıt. |

**Öneri:** DonusKayitServisi çok küçükse **DonusAkisServisi.cs** veya **LogServisi** tarafına “spin kaydı” olarak yaklaştırılabilir; yoksa ayrı kalır.

---

## 10. Izgara

| Dosya | İş | Birleştirme önerisi |
|-------|-----|----------------------|
| **IzgaraServisi.cs** | Grid, semboller, XYToIndex, ScatterSay, render. | Runtime izgara; ayrı. |
| **IzgaraBaslatmaServisi.cs** | İlk kurulum (alloc, hücreler, overlay bağlantısı). | Init; ayrı. |

**Öneri:** Biri runtime biri init; birleştirme gerekmez.

---

## 11. Tumble (patlatma / düşme)

| Dosya | İş | Birleştirme önerisi |
|-------|-----|----------------------|
| **TumbleServisi.cs** | TumbleLoop, CollapseRefillAndAnimate, FindClustersToRemove delegasyonu. | Wrapper. |
| **TumbleAkisServisi.cs** | Tumble döngüsü orkestrasyonu (cluster bul, patlat, refill, çarpan). | Akış. |

**Öneri:** Biri delegasyon biri akış; ayrı kalır.

---

## 12. Çökme

| Dosya | İş | Birleştirme önerisi |
|-------|-----|----------------------|
| **CokmeAkisServisi.cs** | Çökme sonrası refill, çarpan yerleştirme, animasyon. | Tek dosya; birleştirme yok. |

---

## 13. Çarpan

| Dosya | İş | Birleştirme önerisi |
|-------|-----|----------------------|
| **CarpanServisi.cs** | Çarpan state, spin çarpanı, pending listesi, RollCarpanDegeri. | State + hesap. |
| **CarpanYerlestirmeServisi.cs** | Çarpanları grid’e yerleştirme (hücre seçimi, scatter atlama). | Yerleştirme. |
| **CarpanOverlayServisi.cs** | Hücrede “xN” overlay, düşme animasyonu, ClearAll. | Görsel. |

**Öneri:** Üçü farklı sorumluluk; ayrı kalır.

---

## 14. Ekonomi / ödeme / log

| Dosya | İş | Birleştirme önerisi |
|-------|-----|----------------------|
| **EkonomiServisi.cs** | Bakiye, bahis, yatır/çek/ödeme, GameManager senkronu. | Ayrı. |
| **OdemeServisi.cs** | Kasa/havuz: ParaGirisi, OdemeYapOdulHavuzundan. | Ayrı. |
| **LogServisi.cs** | RecordEconomyAction + Log delegasyonu. | Ayrı. |

**Öneri:** Birleştirme yok.

---

## 15. Scatter / animasyon / yardımcı

| Dosya | İş | Birleştirme önerisi |
|-------|-----|----------------------|
| **ScatterEfektServisi.cs** | Scatter hücreleri büyütme efekti. | Tek; ayrı. |
| **AnimasyonServisi.cs** | Pop animasyonu, çarpan overlay ref’leri. | Ayrı. |
| **OyunFormatServisi.cs** | Static: FormatTL vb. | Ayrı. |
| **ZorlukServisi.cs** | Zorluk değeri, bias. | Ayrı. |
| **KorutinServisi.cs** | İsimli coroutine (StartNamed, StopNamed). | Ayrı. |
| **OyunBootstrapServisi.cs** | Başlangıç: ayar sync, EnsurePayTables. | Ayrı. |
| **OyunKorumaServisi.cs** | Oyun koruma wrapper. | Ayrı. |
| **HizVeSesServisi.cs** | Hız/ses delegasyonu. | Ayrı. |

**Öneri:** Birleştirme yok.

---

## 16. Arayüzler (I*Baglami)

| Dosya | İş | Birleştirme önerisi |
|-------|-----|----------------------|
| **IDonusAkisBaglami.cs** | DonusAkisServisi bağlamı. | **→ DonusAkisServisi.cs** (aynı dosyada interface + sınıf). |
| **IIzgaraBaslatmaBaglami.cs** | IzgaraBaslatmaServisi bağlamı. | **→ IzgaraBaslatmaServisi.cs** |
| **ICokmeAkisBaglami.cs** | CokmeAkisServisi bağlamı. | **→ CokmeAkisServisi.cs** |
| **ITumbleAkisBaglami.cs** | TumbleAkisServisi bağlamı. | **→ TumbleAkisServisi.cs** |
| **IScatterEfektBaglami.cs** | ScatterEfektServisi bağlamı. | **→ ScatterEfektServisi.cs** |
| **IOyunUIGuncellemeBaglami.cs** | OyunUIGuncellemeServisi bağlamı. | **→ OyunUIGuncellemeServisi.cs** |
| **IOyunBootstrapBaglami.cs** | OyunBootstrapServisi bağlamı. | **→ OyunBootstrapServisi.cs** |
| **ICarpanYerlestirmeBaglami.cs** | CarpanYerlestirmeServisi bağlamı. | **→ CarpanYerlestirmeServisi.cs** |
| **IZorlukBaglami.cs** | ZorlukServisi bağlamı. | **→ ZorlukServisi.cs** |
| **IOyunKorumaBaglami.cs** | OyunKorumaServisi bağlamı. | **→ OyunKorumaServisi.cs** |

**Öneri:** Her arayüz ilgili servis .cs dosyasının **en üstünde** (aynı dosyada) tanımlanır; ayrı I*.cs silinir. **-10 .cs**

---

## 17. Küçük / tek amaçlı bileşenler

| Dosya | İş | Birleştirme önerisi |
|-------|-----|----------------------|
| **HosgeldinizText.cs** | TMP’ye “Hoş Geldiniz!” yazar. | Çok küçük; ayrı kalabilir veya ileride genel “MetinGuncelleyici” gibi bir şeye dönüşebilir. |
| **SpinIconRotate.cs** | Dönüş ikonunu döndürür. | İstersen mantık OyunYoneticisi’ne alınır, **-1 .cs**; sahne bağlantısı değişir. |

---

## Özet: Birleştirme önerileri (dosya azaltma)

| # | Öneri | Grup | Dosya değişimi |
|---|--------|------|-----------------|
| 1 | **StatsEntry → PlayerProfile.cs** | Veri / model | **-1 .cs** |
| 2 | **10 arayüz → ilgili servis dosyasına** | Arayüzler | **-10 .cs** |
| 3 | (İsteğe bağlı) **SpinIconRotate → OyunYoneticisi** | Küçük bileşen | **-1 .cs** |

**Toplam:** 1+2 uygulanırsa **-11 .cs**. SesAyarlari ve diğer ayar dosyalarına şu an dokunulmuyor.
