# OyunYoneticisi.cs Refaktör Analizi ve PAKET-10 Planı

**Mevcut satır:** ~1912  
**Hedef:** ~1200 satır bandı  
**Yaklaşık taşınacak:** ~700 satır (servislere veya kaldırılacak bloklara)

---

## 1. Kalan Büyük Bloklar (Metot Grupları) – Boyuta Göre

| Blok | Yaklaşık satır | Sorumluluk |
|------|----------------|------------|
| **CollapseRefillAndAnimate** | ~185 (1551–1734) | Kolon collapse, refill, çarpan yerleştirme, grid/carpan sync, animasyon. TumbleService'e delegate ediliyor ama gövde OY'da. |
| **AutoWireUIIfNeeded + FindGO/FindComp/FindTmpByNameContains** | ~255 (2082–2264 + 2236–2263) | Sahne isimleriyle UI bulma, UIReferanslari doldurma, fallback isim listeleri. |
| **BonusDongusu** | ~163 (1255–1418) | Bonus döngüsü: hak azaltma, FillRandomAll, çarpan, TumbleLoop, kazanç/budget clamp, pending ödeme, bonus bitiş, müzik/sync. |
| **NormalSpinAkisi** | ~120 (1059–1179) | Spin başlangıç, grid fill, çarpan, TumbleLoop, ödeme, scatter kontrolü, bonus tetikleme. |
| **UI_Guncelle** | ~67 (1916–1979) | Bakiye/bahis/hak/kazanc/çarpan/bonus satın al metin ve buton güncelleme. |
| **ResolveMoneyUIRefsIfMissing** | ~50 (2007–2056) | Para/bakiye UI referanslarını isimle bulma. |
| **ShowBonusEndMessage** (ödeme kısmı) | ~27 (1229–1248) | Kasa ödemesi, bakiye, bonus pending sıfırlama, fark düzeltme. |
| **CarpanlariDoluGriddeUygula** | ~50 (1798–1845) | Pending çarpanları grid'e yerleştirme (aday hücre, scatter atlama, MultiplierService ile konuşma). |
| **ClearAllCarpanOverlays** (grid sync kısmı) | ~25 (1824–1845) | Grid'de çarpan hücreleri -1 yapma, carpanHücreTextleri gizleme. |
| **InitRoutine** (grid/hücre init) | ~52 (898–949) | Grid alloc, servis setleri, hucreler toplama. |
| **ScatterBuyutEfekti** | ~55 (2265–2310) | Scatter hücreleri büyütme animasyonu. |
| **SpinButonImpl** | ~25 (1028–1056) | Ödenebilir limit, kasa giriş, deduct, RecordSpinStart, NormalSpinAkisi başlatma. |
| **BaslatBonus** | ~55 (1179–1234) | bonusAktif, müzik, BonusBaslangicAkisi. |
| **GetBonusRemainingPayableTL / InitBonusBudgetFromHavuz / RecordBonusPayment** | ~100 toplam (1401–1450) | Bonus bütçe/cap ve ödeme kaydı. |
| **UygulaCarpanAyarlari** | ~45 (410–454) | CarpanAyarlari → OY alanları + slider senkron. |
| **SyncFromAyarClassesIfPresent** | ~60 (554–598) | Bonus/OdulHavuzu/Ekonomi ayar sınıflarından OY'a kopyalama. |
| **EnsurePayTablesInitialized** | ~12 (145–159) | TumbleAyarlari EnsurePayTablesInitialized çağrısı. |
| **FloodFillCluster** | ~38 (1735–1765) | **Kullanılmıyor** (ölü kod). |

**Özet:** En büyük indirimler CollapseRefillAndAnimate, AutoWire/Find blokları, BonusDongusu ve NormalSpinAkisi'nin bir kısmından gelir. UI_Guncelle, Resolve*, ShowBonusEndMessage ödeme kısmı, çarpan grid glue ve bonus bütçe/ödeme de taşınabilir veya sadeleştirilebilir.

---

## 2. Taşınabilir Bloklar – Servis Eşlemesi (Tek Sorumluluk)

| Hedef servis | Taşınacak / sadeleştirilecek | Not |
|--------------|-----------------------------|-----|
| **UIAutoBindService / SceneWiringService** | AutoWireUIIfNeeded, FindGO, FindComp, FindTmpByNameContains (3 overload), ResolveMoneyUIRefsIfMissing içi "isimle bul" mantığı | UI referans toplama ve isimle bulma tek yerde. OY sadece servisi çağırır; referanslar setter veya callback ile OY'a geri verilir. |
| **PayoutService** | kasa.ParaGirisi_BolVeEkle, OdemeYap_OdulHavuzundan, _bonusPendingOdemeTL kapatma (ShowBonusEndMessage içi), ödenebilir limit hesabı (havuz %10) | Kasa erişimi ve havuz/ödeme akışı tek yerde. EconomyService + Kasa interface/delegate ile. |
| **BonusFlowCoreService** | bonusHakKalan başlatma, bonusAktif aç/kapa, _bonusPendingOdemeTL reset, BonusDongusu içi "orchestration" (kim ne çağıracak), GetBonusRemainingPayableTL, InitBonusBudgetFromHavuz, RecordBonusPayment | SpinService yanında; bonus state ve bütçe/cap mantığı. OY sadece state'i tutar veya delegate ile servise sorar. |
| **GridMultiplierBridgeService** | CarpanlariDoluGriddeUygula, CollapseRefillAndAnimate içi "pending çarpan → grid" yerleştirme ve carpanDegerByCellIndex sync, ClearAllCarpanOverlays'ın grid+carpanHücreTextleri kısmı | Grid + Multiplier arası glue. Grid/carpanDegerGrid OY'da kalabilir; "yerleştir/sıfırla" mantığı bridge'de. |
| **Mevcut UIService genişletme** | UI_Guncelle gövdesi (bakiye/bahis/hak/kazanc/çarpan/bonus satın al metin) | Zaten UI sorumluluğu var; UI_Guncelle burada toplanır. |
| **Format / yardımcı** | FormatTL, tekrarlayan TL/format kullanımları | GameFormatService veya static helper; küçük ama dağınık kullanımı toplar. |

**Taşınmayacak / OY'da kalacak:** NormalSpinAkisi / BonusDongusu **coroutine iskeleti** (sıra ve servis çağrıları), SpinButonImpl ince wrapper, BaslatBonus ince wrapper, tumbleAyarlari/carpanAyarlari/kasa referansları, Inspector'a açık public wrapper'lar.

---

## 3. Risk Sırasına Göre 5–8 Adımlık PAKET-10 Planı

**Prensip:** Önce düşük risk (yardımcı/tekil bloklar), sonra orta (UI/payout), en sonda yüksek (akış/glue). Her adım 2–4 dosya; davranış değişmez; public API korunur.

| Aşama | İçerik | Tahmini satır kazancı | Risk | Dosya sayısı |
|-------|--------|------------------------|------|--------------|
| **AŞAMA-1** | **FormatTL + tekrarlayan format** → GameFormatService (veya static helper). OY ve ilgili servisler buradan kullanır. | ~15 | Düşük | 2 (yeni servis + OY) |
| **AŞAMA-2** | **UIAutoBindService:** AutoWireUIIfNeeded, FindGO, FindComp, FindTmpByNameContains taşınır. OY Start'ta servisi kurar; servis referansları setter/callback ile OY'a yazar. Public API aynı kalır. | ~260 | Düşük | 2 (UIAutoBindService + OY) |
| **AŞAMA-3** | **ResolveMoneyUIRefsIfMissing** "isimle bul" kısmı UIAutoBindService'e taşınır veya AŞAMA-2 servisi genişletilir. OY sadece delegasyon. | ~45 | Düşük | 2 |
| **AŞAMA-4** | **PayoutService:** kasa.ParaGirisi_BolVeEkle, OdemeYap_OdulHavuzundan, havuz %10 ödenebilir limit. ShowBonusEndMessage içi "bonus pending öde ve sıfırla" bu servise taşınır; OY sadece çağırır. | ~40 | Orta | 2 (PayoutService + OY) |
| **AŞAMA-5** | **UI_Guncelle gövdesi** UIService'e taşınır. OY'daki UI_Guncelle tek satır delegasyon olur. Gerekli getter'lar delegate veya mevcut UIService genişletmesi ile verilir. | ~65 | Düşük | 2 (UIService + OY) |
| **AŞAMA-6** | **GridMultiplierBridgeService:** CarpanlariDoluGriddeUygula, ClearAllCarpanOverlays'ın grid+carpanText kısmı, CollapseRefillAndAnimate içi "pending → grid + carpanDegerByCellIndex sync" taşınır. CollapseRefillAndAnimate gövdesi TumbleService'e veya bu bridge'e delegate kalır; implementasyon bridge'de olur. | ~120 | Orta | 2–3 (Bridge + OY, gerekirse TumbleService imza) |
| **AŞAMA-7** | **BonusFlowCoreService:** GetBonusRemainingPayableTL, InitBonusBudgetFromHavuz, RecordBonusPayment + bonus state (bonusHakKalan başlatma, bonusAktif set) taşınır. BonusDongusu ve BaslatBonus servisi çağırır; state serviste veya OY'da tutulur (delegate ile okunur). | ~100 | Orta | 2 (BonusFlowCoreService + OY) |
| **AŞAMA-8** | **CollapseRefillAndAnimate** tam gövdesi TumbleService veya GridMultiplierBridgeService'e taşınır; OY sadece `yield return _tumbleService.CollapseRefillAndAnimate()` (veya bridge). FloodFillCluster kullanılmıyorsa **silinir**. Son kontroller ve gerekirse küçük refinetler. | ~185 + ~38 (ölü) | Yüksek | 2–3 |

**Toplam tahmini:** ~768 satır azalma; 1912 − 768 ≈ 1144 → **~1200 bandına** iner.

---

## 4. Commit ve Test Notları

- Her aşama sonunda: `git add ... ; git commit -m "Refactor: PAKET-10 AŞAMA-x ..."`
- Her aşama sonunda kısa kontrol: Unity'de derleme (compile) + Play ile spin/bonus/tumble akışı ve UI (bakiye, bahis, kazanç, bonus satın al) kısa test.
- Public API: Inspector'da bağlı public metotlar (ParaCek_OnayButton, SpinButon, OnZorlukSliderChanged vb.) wrapper olarak kalır; imza değişmez.
- Sahne/prefab: Mümkünse serialize değişiklikleri ayrı commit'te (ör. sadece referans ekleme/çıkarma).

---

## 5. Özet Tablo (Taşınacaklar – (A)–(E) ile)

| Öneri | Aşama | Açıklama |
|-------|--------|----------|
| **(A) AutoWire/Find/UI toplama** | AŞAMA-2, AŞAMA-3 | UIAutoBindService; AutoWireUIIfNeeded, FindComp, FindTmpByNameContains, ResolveMoneyUIRefsIfMissing "isimle bul" kısmı. |
| **(B) Kasa / havuz ödeme** | AŞAMA-4 | PayoutService; ParaGirisi_BolVeEkle, OdemeYap_OdulHavuzundan, bonus pending kapatma, ödenebilir limit. |
| **(C) Bonus state init/close** | AŞAMA-7 | BonusFlowCoreService; bonusHakKalan, bonusAktif, GetBonusRemainingPayableTL, InitBonusBudgetFromHavuz, RecordBonusPayment. |
| **(D) Grid+multiplier glue** | AŞAMA-6, AŞAMA-8 | GridMultiplierBridgeService; CarpanlariDoluGriddeUygula, pending→grid, ClearAllCarpanOverlays grid sync, CollapseRefillAndAnimate. |
| **(E) Format / TL helper** | AŞAMA-1 | GameFormatService veya static; FormatTL ve tekrarlayan format kullanımları. |

Buna ek olarak **UI_Guncelle** AŞAMA-5'te UIService'e taşınarak hem satır hem tek sorumluluk kazanılır.

---

*Dosya: PAKET-10_Refaktor_Plani.md — Word'de açıp "Farklı Kaydet" ile .docx olarak kaydedebilirsin.*
