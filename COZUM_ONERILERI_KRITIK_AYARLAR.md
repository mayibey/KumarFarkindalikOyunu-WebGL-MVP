# Kritik Ayar Açıkları — Çözüm Önerileri

Bu dokümanda, `ONERILEN_COZUM_BUG_VE_ACIKLIK_ANALIZI.md` içindeki her madde için **net çözüm önerisi** yer alıyor. Senin önerin (simülasyon önce arkada, sonucu ekrana vermek) ana çözüm olarak benimseniyor; diğer maddeler için de çözümsüz kalan yok.

---

## Senin önerin: “Simülasyon önce arkada uygulansın, istenen sonuç ekrana gelsin”

**Öneri:** Simülasyon önce arkada tamamlansın; ardından **o simülasyonun ürettiği sonuç** ekrana gelsin.

Bu tam olarak **kayıt–tekrar (record–replay)** yaklaşımı:

1. **Arkada simülasyon:** Spin butonuna basıldığında, ekranda henüz hiçbir şey oynatılmadan, mevcut tumble mantığı **animasyonsuz** çalıştırılır (grid doldur, cluster bul, kazanç ekle, hücreleri temizle, refill, çarpan üret, tekrarla). Toplam ödeme hesaplanır; limit aşıyorsa grid yeniden üretilir (re-roll), ta ki limiti aşmayan bir sonuç bulunana kadar.
2. **Sonucu kaydet:** Simülasyon her adımda ürettiği veriyi **kaydeder**: ilk grid, her tumble adımında (patlayan hücreler, refill sonrası grid, o turdaki çarpan değeri, o tur kazancı), nihai toplam ham kazanç ve çarpan.
3. **Ekrana sadece bu kayıt oynatılır:** Simülasyon bittikten sonra ekranda **sadece bu kayıt** adım adım gösterilir (düşüş, patlama, refill, çarpan metni). Bu aşamada **hiç RNG çağrılmaz**; tüm veri kayıttan okunur. Böylece “gösterilen” ile “onaylanan” sonuç **birebir aynı** olur; determinizm sorunu kalmaz.

Özet: **Simülasyon önce arkada → çıkan tek sonuç kaydedilir → ekranda sadece o kayıt oynatılır.** İkinci kez aynı mantığı RNG ile çalıştırmıyoruz; bu yüzden “simülasyon = ekran” garantisi sağlanır. Bu yaklaşım plana ana çözüm olarak eklenmeli.

---

## Diğer maddeler için çözüm önerileri

### 1. Determinizm (simülasyon = ekran)
- **Çözüm:** Yukarıdaki “simülasyon arkada, sonucu kaydet, ekranda sadece kaydı oynat” modeli. Ekstra seed’li RNG’ye gerek kalmaz.

### 2. Re-roll sonsuz döngü
- **Çözüm:** Re-roll döngüsüne **maksimum deneme** (örn. 200) konur. Bu sayı aşılırsa **fallback:** Son denemede üretilen grid kullanılır; nihai ödeme **ödenebilir limiti aşıyorsa ödeme havuzda kalanla sınırlanır** (kasa zaten fazlasını vermiyor). Böylece ne sonsuz döngü kalır ne de “hiç sonuç yok” durumu; limit aşımı para tarafında yine yapılmaz.

### 3. Adım 1.2 (normal spin’de limit için break)
- **Çözüm:** Normal spinde “limit aşınca break” **hiç uygulanmaz**. Sadece simülasyon + re-roll (1.3) kullanılır; limit kontrolü sadece simülasyon içinde, re-roll kriteri olarak. Canlı tumble döngüsü artık “kayıt oynatma” olduğu için break mantığına gerek kalmaz.

### 4. Bonus “kalan” simülasyonda
- **Çözüm:** Her bonus spin’i başında **o anki** `GetBonusRemainingPayableTL()` değeri alınır; simülasyon bu spin’i “bu spin için kalan ≤ X” kısıtıyla çalışır; re-roll da bu X’i aşmayan sonuçlara göre yapılır. Simülasyon bittikten sonra “bu spin’de ödenecek” miktar hesaplanır; **gerçek ödeme** bu miktarla yapılır ve bonus kalan (pending/cap) güncellenir. Bir sonraki bonus spin’inde `GetBonusRemainingPayableTL()` zaten güncel kalanı döndürür. Ek bir şey gerekmez; sadece simülasyonun “bu spin için kalan”ı spin başında tek değer alması ve ödemenin spin sonunda yapılıp kalanın güncellenmesi dokümante edilir.

### 5. Çarpan simülasyonda
- **Çözüm:** Kayıt–tekrar ile hallolur. Simülasyon sırasında hangi tumble’da hangi çarpan değeri üretildiyse kayda yazılır; ekranda oynatma sırasında **kayıttaki çarpan değerleri** kullanılır, RNG ile yeniden çarpan üretilmez. Böylece çarpan da simülasyonla ekran arasında aynı kalır.

### 6. PayTable: pay = 0 / çok küçük değer
- **Çözüm:** Ağırlık hesabında pay kullanılırken: (a) Pay ≤ 0 ise **pay = 0.01** (veya sabit min pay) kabul edilir; (b) **maksimum ağırlık** (örn. 5) konur, `weight = Min(1 / pay, maxWeight)` gibi. Böylece ne bölme hatası ne de tek sembolün tamamen dominant olması kalır.

### 7. Cluster boyutu dağılımı (8–9 / 10–11 / 12+)
- **Çözüm:** İki katman: (a) **Refill ağırlığı:** Zorluk 4 (kolay) için, “bu sembolü seçersem mevcut cluster X’e Y adet daha eklenir” bilgisi kullanılıyorsa, cluster 10+ olacak seçimlerin ağırlığı düşürülür (mevcut bias mantığına ek katsayı). (b) **Re-roll kriteri (isteğe bağlı):** Simülasyon sonucu kabul edilirken “tüm cluster boyutları 12’den büyük mü?” gibi bir kontrol; evetse ve deneme sayısı henüz dolmadıysa bir kez daha re-roll. Bu kriter sadece kolay zorlukta ve istenirse uygulanır; max re-roll sayısı (madde 2) ile sınırlı olduğu için sonsuz döngü oluşmaz. Determinizm, “kayıt oynatma” ile korunur; re-roll sadece simülasyon aşamasında.

### 8. Normal vs. bonus limit
- **Çözüm:** Normal: Spin başında `GetSpinOdenebilirLimit()` bir kez alınır; simülasyon ve re-roll bu limite göre. Bonus: Her bonus spin başında `GetBonusRemainingPayableTL()` alınır; simülasyon ve re-roll o spin için bu kalanla; ödeme sonrası kalan güncellenir (madde 4). Plan dokümanında “normal = spin limiti, bonus = spin başı kalan” diye tek cümleyle yazılır.

### 9. MAX_TUMBLE_TUR
- **Çözüm:** Simülasyon, canlı tumble döngüsüyle **aynı** `MAX_TUMBLE_TUR` sabitini kullanır; tur sayısı da kayda yazılır. Oynatma, kayıttaki adımları (en fazla bu tur sayısı) oynatır. Böylece simülasyon ile ekran aynı tur sayısına sahip olur.

---

## Özet: Çözümsüz kalan var mı?

Hayır. Hepsi için uygulanabilir çözüm var:

| Madde | Çözüm |
|-------|--------|
| Determinizm | Simülasyon arkada; sonuç kaydedilir; ekranda sadece kayıt oynatılır (senin önerin). |
| Re-roll sonsuz döngü | Max deneme (örn. 200) + aşılırsa son grid kullanılır, ödeme limit/havuzla kırpılır. |
| 1.2 break | Kullanılmaz; sadece simülasyon + re-roll. |
| Bonus kalan | Spin başında kalan alınır; simülasyon bu spin’i o kalanla; ödeme sonrası kalan güncellenir. |
| Çarpan | Kayıtta yazılır; oynatmada sadece kayıt kullanılır. |
| PayTable 0 / küçük | Min pay (0.01), max ağırlık sınırı. |
| Cluster 8–9 / 10–11 / 12+ | Refill ağırlığı + isteğe bağlı re-roll kriteri; max re-roll ile sınırlı. |
| Normal vs. bonus limit | Normal = spin limiti; bonus = spin başı kalan, ödeme ile güncellenir. |
| MAX_TUMBLE_TUR | Simülasyon ve oynatma aynı sabiti kullanır; tur sayısı kayıtta. |

Bu çözümler `UYGULAMA_PLANI_KRITIK_AYARLAR.md` ve ilgili teknik notlara eklendiğinde, hem “simülasyon önce arkada, sonuç ekrana” akışı hem de diğer açıklar net bir şekilde kapatılmış olur.
