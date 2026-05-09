# Önerilen Çözüm — Bug ve Açık Kalan Noktalar Analizi

Bu dokümanda, **Kritik Ayar** için önerilen çözüm (simülasyon + re-roll, limit kontrolü, PayTable ağırlık, zorluk, cluster dağılımı) **uygulanmış varsayılarak** senin çerçeveden (limit aşımı yok, tumble tutarlılığı, güven) tespit edilen açıklar ve olası hatalar listeleniyor. Kendi önerimin üzerinde “kendi bugunu bul” testi yapıldı.

---

## 1. Simülasyon vs. gösterim tutarsızlığı (determinizm) — **kritik**

**Sorun:** Plan “önce simüle et, limiti aşmıyorsa bu sonucu animasyonla oynat” diyor. Simülasyon ve animasyon **aynı kod yolunu** (Fill → TumbleLoop → Refill → Carpan) kullanıyorsa, ikisi de `UnityEngine.Random` ve `Random.Range` tüketiyor. Simülasyon çalıştığında RNG’den sayılar alınır; ardından “aynı spin’i oynat” dediğimizde RNG’nin durumu **farklı** — refill’de gelen semboller, çarpanın düşüp düşmemesi ve değeri **farklı** olur. Yani:

- Simülasyonda: toplam = 400 TL (limit 500), kabul.
- Oynatırken: refill ve çarpan farklı gelir → toplam 600 TL’ye çıkabilir → **limit aşımı** veya en azından **gösterilen sonuç ≠ onaylanan sonuç**.

**Eksik:** Plan determinizmden hiç bahsetmiyor. İki seçenek:

- **A) Kayıt–tekrar:** Simülasyon sırasında her adım **kaydedilir** (ilk grid, her tumble’daki toRemove, her refill’deki hücre değerleri, her çarpan değeri). Oynatma **sadece** bu kaydı adım adım gösterir; hiç RNG çağrılmaz. Sonuç garantili aynı.
- **B) Seed’li RNG:** Spin başında tek bir seed (örn. `Random.InitState(seed)`) atanır; hem simülasyon hem oynatma **bu seed ile** başlar. Simülasyon ve oynatma aynı sırayla aynı Random çağrılarını yapmalı (ekstra çağrı olmamalı). Seed’i spin başına üretirken (zaman, frame, hash) tekrarlanabilirlik dikkate alınmalı.

Aksi halde “onaylanan sonuç” ile “gösterilen sonuç” farklılaşır; limit simülasyonda aşılmaz ama ekranda aşılabilir.

---

## 2. Re-roll sonsuz döngü riski

**Sorun:** “Toplam > ödenebilir limit ise grid’i yeniden üret, tekrar simüle et” deniyor; **maksimum deneme sayısı** yok. Ödenebilir limit çok düşük (örn. 50 TL), grid boyutu ve ödeme katsayıları yüksekse neredeyse her denemede toplam limiti aşabilir → **sonsuz döngü**, oyun donar.

**Eksik:** Re-roll döngüsünde:

- **Maksimum deneme** (örn. 100–500) konmalı.
- **Fallback:** Bu sayı aşılırsa ne olacak net olmalı: (a) “güvenli” tek bir sonuç zorla seç (örn. tumble yok, kazanç 0), (b) limiti aşan sonucu kırp (toplamı limite indir) — ama (b) “limit aşan ödeme yapılmamalı” kuralıyla çelişebilir; tercihen (a) veya “son denemeyi limiti aşsa bile kabul et ve ödemeyi havuzda kalanla sınırla” gibi net bir kural yazılmalı.

---

## 3. Adım 1.2 (normal spin’de limit için break) — tasarım çelişkisi

**Sorun:** Plan “Normal spin tumble döngüsüne limit kontrolü: (spinKazancHam + bu_tur) * çarpan > limit ise **break**” diyor. Break edildiğinde:

- Grid’de hâlâ **tumble yapacak cluster** var (FindClustersToRemove dolu dönmüştü).
- Biz sadece “yeni tumble yapma” diyoruz; cluster’lar ekranda kalıyor.
- Bu, senin “tumble oluşturacak sayıda meyve varken tumble yapmamayı meydana getirmemeli” kuralıyla **doğrudan çelişir** — yine “görünürde tumble varken tumble yok” durumu oluşur.

**Sonuç:** 1.2’yi “normal spin’de limit aşınca break” şeklinde uygulamak, güven hedefini bozar. Tutarlı çözüm: **Sadece 1.3 (simülasyon + re-roll)**. Limit kontrolü simülasyon içinde yapılır; aşıyorsa re-roll. Canlı döngüde “limit aşınca break” **eklenmemeli** (veya 1.3 devreye girdikten sonra 1.2’deki bu break kaldırılmalı). 1.2 “geçici önlem” olarak bile riskli, çünkü aynı güven sorununu tekrar üretir.

---

## 4. Bonus: “Kalan ödenebilir” simülasyonda doğru mu?

**Sorun:** `GetBonusRemainingPayableTL()` genelde `(bonus cap/budget) - (şu ana kadar ödenen veya pending)` şeklinde. Canlı oyunda her spin/tumble’da ödeme yapılınca bu değer **azalıyor**. Simülasyonda ise “ödeme yapmıyoruz”, sadece kazançları topluyoruz. Eğer simülasyon tek bir bonus spin’inin tumble’larını çalıştırıyorsa:

- Doğru kullanım: Simülasyon içinde “bu spin için kalan” = bonus başında kalan; her tumble’da (ve çarpan sonrası) projeksiyon bu kalanı aşmamalı; simülasyon sonunda “bu spin’de ödenecek” miktar hesaplanır; gerçek oyunda bu miktar ödenir ve **bir sonraki** bonus spin’inde kalan buna göre güncellenir.
- Hatalı kullanım: Simülasyon `GetBonusRemainingPayableTL()` çağırıyor ama gerçek ödeme henüz yapılmadığı için “kalan” hiç azalmıyor; simülasyon çok yüksek toplam kabul edebilir; sonra aynı bonus içinde birkaç spin daha olunca toplam ödeme cap’i aşar.

**Eksik:** Bonus için simülasyonun “kalan”ı nasıl kullandığı net yazılmalı: ya (a) bonus spin başında mevcut `GetBonusRemainingPayableTL()` ile simüle et, bu spin’in toplamını bu limiti aşmayacak şekilde re-roll et, ya (b) tüm bonus turunu simüle edip toplam cap’i aşmayacak şekilde re-roll et. (b) maliyetli; genelde (a) yeterli. Ayrıca “bu spin’de ödenecek” değerin gerçek ödeme sonrası bonus kalanına doğru yansıtıldığından emin olunmalı.

---

## 5. Çarpan (multiplier) simülasyonda

**Sorun:** Çarpan hem **rastgele** (TryScheduleCarpanDrop içinde Random, çarpan değeri RastgeleCarpan ile) hem de **bonus kalan limitine** bağlı (projeksiyon > kalan ise çarpan eklenmiyor). Simülasyon bu mantığı aynen çalıştırmalı. Determinizm yoksa (madde 1):

- Simülasyonda çarpan 1 kalır → toplam 400 TL.
- Oynatırken çarpan 2x düşer → toplam 800 TL → limit aşımı.

Determinizm (kayıt–tekrar veya seed) olmadan çarpan farkı tek başına bile limiti bozabilir.

---

## 6. Sembol ağırlığı ↔ PayTable: sıfır ve sınırlar

**Sorun:** “Yüksek ödeme = düşük ağırlık” için genelde `ağırlık ∝ 1 / pay` veya benzeri kullanılır. Bazı sembollerin PayTable’da **0** veya çok küçük katsayısı olabilir:

- Pay = 0 → 1/pay tanımsız veya sonsuz; bölme hatası veya anlamsız ağırlık.
- Pay çok küçük → ağırlık çok büyük; bir sembol neredeyse her zaman seçilir, dağılım bozulur.

**Eksik:** PayTable entegrasyonunda:

- Pay = 0 (veya tanımsız) için net kural: örn. ağırlık = 1 veya pay’ı 0.01 gibi minimumla sınırla.
- Maksimum ağırlık (veya min pay) sınırı konmalı ki tek sembol dominant olmasın.

---

## 7. Cluster boyutu dağılımı (8–9 / 10–11 / 12+) — determinizm ve tanım

**Sorun:** “Çoğunlukla 8–9, az 10–11, çok az 12+” isteniyor. Cluster boyutu doğrudan seçilmiyor; **refill’deki sembol seçimleri** birleşip cluster’ları oluşturuyor. İki yaklaşım:

- **Yaklaşım A:** Refill’de “bu hücreye X sembolü koyarsam cluster 12’ye çıkar” diye X’in ağırlığını düşür. Bu, refill mantığını karmaşıklaştırır ve **simülasyonla oynatma aynı sırada aynı kararları vermeli**; ekstra “cluster boyutuna göre red/tekrar” varsa RNG tüketimi değişir, determinizm bozulabilir.
- **Yaklaşım B:** Simülasyon bittikten sonra “kabul edilen sonuçta” cluster boyutları 8–9 ağır, 10–11 az, 12+ çok az mı diye bir **re-roll kriteri** eklemek. O zaman “limit ok ama cluster dağılımı kötü” diye re-roll ederiz; bu da re-roll sayısını artırır ve madde 2’deki sonsuz döngü riskini artırabilir.

**Eksik:** Cluster dağılımı hedefi net: (a) sadece refill ağırlığıyla mı (nasıl?), (b) re-roll kriteri mi, (c) ikisi birden mi? Determinizm (aynı seed veya kayıt) korunacak şekilde nasıl uygulanacağı yazılmalı.

---

## 8. Normal spin vs. bonus spin: iki farklı limit kaynağı

**Sorun:** Normal spinde limit = `GetSpinOdenebilirLimit()` (havuzun %10’u), spin başına sabit. Bonus’ta limit = `GetBonusRemainingPayableTL()`, spinler arası **azalıyor**. Simülasyon:

- Normal: Tek spin, tek limit, re-roll ta limiti aşmayan sonuca.
- Bonus: Her bonus spin’i için “o anki kalan” ile simüle; re-roll ta o spin’in toplamı kalanı aşmasın. Sonra gerçek ödeme yapılınca kalan güncellenir; bir sonraki bonus spin’i yeni kalan ile simüle edilir.

Bonus’ta “tüm bonusu bir seferde simüle et” daha doğru ama çok maliyetli; spin bazlı simülasyon + kalan güncellemesi makul, ancak **kalan’ın gerçek ödemeyle senkron** olduğu kodda açık olmalı (madde 4).

---

## 9. Maksimum tumble turu (MAX_TUMBLE_TUR)

**Sorun:** Simülasyon da aynı MAX_TUMBLE_TUR ile sınırlı olmalı. Aksi halde simülasyon 20 tur yapıp “toplam 400 TL” der, oynatma 15 turda kesilirse gösterilen toplam farklı olur. Simülasyon ve oynatma **aynı tur sayısı ve aynı adımları** kullanmalı (determinizmle birlikte).

---

## 10. Özet: Açık kalan ve düzeltilmesi gerekenler

| # | Konu | Risk | Önerilen netleştirme |
|---|------|------|----------------------|
| 1 | Simülasyon = oynatma sonucu | Kritik: Limit ekranda aşılabilir | Kayıt–tekrar veya seed’li RNG; plana determinizm maddesi ekle |
| 2 | Re-roll sonsuz döngü | Oyun donması | Max deneme + fallback kuralı (güvenli sonuç veya kırpma) |
| 3 | 1.2 “limit aşınca break” | Güven: tumble varken tumble yok | Normal spin’de break kullanma; sadece 1.3 (simülasyon + re-roll) |
| 4 | Bonus kalan simülasyonda | Cap aşımı | “Kalan”ı spin başında al; bu spin toplamı ≤ kalan; ödeme sonrası kalan güncelle |
| 5 | Çarpan simülasyonda | Limit aşımı / tutarsızlık | Determinizm (1) ile birlikte çarpan da aynı kalmalı |
| 6 | PayTable 0 / küçük değer | Hata / dağılım bozulması | Min pay, max ağırlık, pay=0 kuralı |
| 7 | Cluster 8–9 / 10–11 / 12+ | Belirsiz, determinizm riski | Refill kuralı mı re-roll kriteri mi; nasıl determinist kalacak |
| 8 | Normal vs. bonus limit | Yanlış limit kullanımı | Normal: spin limit; bonus: kalan per spin, güncelle |
| 9 | MAX_TUMBLE_TUR | Sonuç farkı | Simülasyon ve oynatma aynı tur limiti |

---

Bu liste, önerilen çözümün senin çerçevede (limit aşımı yok, tumble tutarlılığı, güven) **kontrollü uygulanabilmesi** için plana eklenecek veya değiştirilecek noktaları topluyor. En kritik madde determinizm (1); olmadan “spin sonucu önceden belli” ve “limit asla aşılmaz” garantisi verilemez.
