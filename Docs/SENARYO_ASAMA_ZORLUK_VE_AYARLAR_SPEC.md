# Senaryo Aşamaları – Zorluk ve Yazılımsal Ayarlar (Net Spesifikasyon)

**Kaynak:** Senin belirlediğin aşama mantığı (görsel tablo).  
**Amaç:** Her aşama için zorluk, sabit/esnek kurallar ve takip mekanizmalarını belirsizlik bırakmayacak şekilde tanımlamak.

---

## 1. Zorluk ve esneklik ilkeleri

| İlke | Açıklama |
|------|----------|
| **Temel zorluk** | Her aşamada varsayılan zorluk değeri (4–12). Oyun bu değerle başlar. |
| **Aşama içi esneklik** | Ödeme yapılması gereken durumlarda zorluk **düşürülür** (daha sık tumble/kazanç). Ödeme yapılmaması gereken durumlarda zorluk **yükseltilir**. |
| **Takip/analiz** | Belirli koşullar (spin sayısı, bakiye, yükleme sonrası spin, bonus tetikleme vb.) izlenir; bu koşullara göre zorla çarpan düşürme, zorluk artışı/azalışı tetiklenir. |

---

## 2. Ana tablo: Aşama | Temel zorluk | Yazılımsal ayarlar (net)

| # | Aşama | Temel zorluk | Yazılımsal ayarlar (ölçülebilir / net) |
|---|--------|----------------|----------------------------------------|
| **1** | **Isındırma / Umut** | **5** | • Küçük kazançlar: **sık** (hedef: ortalama en az 1 tumble her 3–4 spinde).<br>• Büyük kazanç: **kapalı** (tek spin ödeme üst limiti örn. bahisin 50x’i).<br>• Bonus: **açık**, ödeme **düşük**; scatter ile tetiklenen bonus ortalama **~50 spinde bir**; bonus satın alındıysa max ödeme = **satın alma bedeli + %30** (örn. 10.000 TL alındıysa max 13.000 TL). |
| **2** | **Kontrol bende** | **6** | • Bahis değişimine **tepki**: bahis artırıldıktan sonra **ardışık 3–4 spinde** küçük ödemeler (tumble veya düşük çarpan) ver.<br>• Kazanç sıklığı: Aşama 1’den **biraz az** (örn. ortalama 1 tumble her 5–6 spinde).<br>• Büyük kazanç: **kapalı**.<br>• Bonus: **açık**, ödeme düşük; ortalama **~75 spinde bir** scatter bonus; satın alındıysa max = **satın alma + %20** (örn. 10.000 → 12.000 TL). |
| **3** | **Az daha / Kayıp kovalama** | **7** | • **Near-miss** aktif: 3 scatter sık görünsün (eşik 4 ise “az daha” hissi).<br>• Bonus **içinde**: 100x, 250x, 500x gibi **yüksek çarpanlar görünsün** ama **tumble olmasın** (kazanç oluşmasın).<br>• Gerçek kazanç: **seyrek** (tumble oranı düşük).<br>• Bonus scatter ile: max ödeme **bahisin %30’u**; satın alındıysa max = **satın alma − %20** (örn. 10.000 → 8.000 TL). |
| **4** | **Bakiye tükenişi** | **8** | • Kazanç senaryoları **minimal** (tumble çok seyrek).<br>• Bakiye **kontrollü düşüş** (negatif trend).<br>• **“Bakiye yetersiz”** uyarısı göster; “Yüklemek ister misin?” benzeri teşvik.<br>• Yükleme sayacı **açık** (kaç kez yüklendi takip).<br>• Bonus: scatter ile max = **bahisin %20’si**; satın alındıysa max = **satın alma − %50** (10.000 → 5.000 TL).<br>• **Yükleme sonrası ilk 20 spin**: küçük kazançlar ver (örn. 200 TL bahis → ortalama ~300 TL civarı ödeme). |
| **5** | **Bonus zirve** | **9** | • İlk bonus (yükleme sonrası, scatter ile): **mümkünse** tetiklenebilir.<br>• Önce bakiye **kontrollü düşsün**, 3. yükleme teşvik edilsin.<br>• **3. yükleme sonrası girilen ilk bonus**: yüksek kazanç (hedef: **satın alma maliyetinin ~2,5 katı**; scatter bonus ise benzer oran).<br>• Bu **tek seferlik** etkili sahne; sonrasında kazançlar **hızla azalsın** (zorluk artışı veya ödeme limiti). |
| **6** | **Gerçek kayıp** | **10** | • Büyük kazanç: **kapalı**.<br>• Küçük kazanç: **çok seyrek** (tumble nadir).<br>• Bonus: **etkisiz** (düşük ödeme veya pratikte kazanç yok). |
| **7** | **Finale** | **11** | • Kazanç **tamamen kapalı** (tumble/ödeme üretilmez veya 0).<br>• Oyun **yavaşlatılır** (bekleme süreleri artırılabilir).<br>• Analiz ekranına geçiş hazırlığı. |

---

## 3. Aşama içi esnek zorluk kuralları

*Aynı aşamada, koşula göre zorluğun geçici artırılıp azaltılması.*

| Aşama | Ödeme **yapılmalı** (zorluk düşür) | Ödeme **yapılmamalı** (zorluk yükselt) |
|--------|-----------------------------------|----------------------------------------|
| 1 – Isındırma | Bakiye çok düştüyse (örn. ilk bakiyenin %70’inin altı): 1–2 spin geçici zorluk 4. | Bakiye hedefin üstündeyse (örn. 1,5x): zorluk 6’ya çekilebilir. |
| 2 – Kontrol bende | Bahis artırımı yapıldıktan sonra 3–4 spin: zorluk 5. | Bahis düşürüldüyse: 1–2 spin zorluk 7 (near-miss verebilir, ödeme az). |
| 3 – Az daha | X spindir kazanç yoksa (örn. 15): bir spin zorluk 6 (küçük tumble). | Normal akışta zorluk 7 sabit; bonus içinde çarpan göster, tumble yok. |
| 4 – Bakiye tükenişi | Yükleme sonrası ilk 20 spin: zorluk 6 (küçük kazançlar). | 20 spin sonrası tekrar 8; bakiye yetersiz uyarısı öncesi ödeme verme. |
| 5 – Bonus zirve | 3. yükleme sonrası **ilk** bonus: zorluk 6 veya özel yüksek ödeme. | Bu bonus dışındaki bonuslarda zorluk 9, ödeme düşük. |
| 6 – Gerçek kayıp | (İsteğe bağlı) Çok uzun kayıp serisinde 1 küçük ödeme: zorluk 9’a geçici düşüm. | Varsayılan 10; büyük kazanç asla. |
| 7 – Finale | — | Her zaman 11; ödeme yok. |

---

## 4. Takip ve analiz mekanizmaları

*Ne zaman zorla çarpan düşülecek, ne zaman zorluk değişecek – karar vericilere girdi sağlayacak sayılar.*

| Mekanizma | Takip edilen | Tetik koşulu | Aksiyon |
|-----------|---------------|--------------|---------|
| **Zorla çarpan zamanlaması** | Aşama, son N spin kazanç/kayıp, bakiye | Örn. Aşama 5, 3. yükleme sonrası ilk bonus; veya “ödeme gerekli” penceresinde | Zorla 100x/250x düşür; tumble açık/kapalı senaryoya göre (Aşama 3’te tumble kapalı). |
| **Bahis artırımı tepkisi** | Son bahis değişim zamanı, artırım mı/düşüm mü | Bahis artırıldı (son 1 spinde) | Sonraki 3–4 spin zorluk düşük (Aşama 2). |
| **Yükleme sonrası pencere** | Son yükleme spin indeksi | `toplamSpin - yüklemeSonrasiIlkSpinIndex <= 20` | Bu 20 spinde zorluk 6, küçük kazanç hedefi. |
| **Bonus türü (scatter / satın al)** | Bonus tetikleme tipi | Scatter vs satın alma | Max ödeme formülü farklı: scatter = bahis %X veya sabit; satın al = maliyet ± %Y. |
| **3. yükleme sonrası ilk bonus** | `yuklemeSayisi == 3` ve bonus bu oturumda ilk kez açılıyor | İki koşul birden | Bu bonusta yüksek ödeme (2,5x maliyet vb.); sonraki bonuslarda normal (düşük). |
| **Near-miss sayacı** | Son spinlerde 3 scatter sayısı | Aşama 3, “az daha” hissi | Scatter olasılığı geçici artırılabilir (sadece 3’lü, 4’e tamamlanmasın). |
| **Bakiye eşikleri** | `mevcutBakiye / ilkBakiye` | &lt; 0,7 → ödeme gerekli; &gt; 1,5 → ödeme azalt | Zorluk geçici artır/azalt. |

---

## 5. Özet sayısal referanslar

| Parametre | Değer |
|-----------|--------|
| Başlangıç bakiyesi | 20.000 TL |
| Bakiye 1,5x hedef (Aşama 1 çıkış) | 30.000 TL |
| Bakiye 2,5x toplam yatırılan (Aşama 6) | 50.000 TL |
| Net zarar %30 (Aşama 6) | 6.000 TL |
| Bonus satın al +%30 (Aşama 1) | 10.000 → max 13.000 TL |
| Bonus satın al +%20 (Aşama 2) | 10.000 → max 12.000 TL |
| Bonus satın al −%20 (Aşama 3) | 10.000 → max 8.000 TL |
| Bonus satın al −%50 (Aşama 4) | 10.000 → max 5.000 TL |
| Yükleme sonrası “ılık” spin sayısı | 20 |
| Bahis artırımı sonrası tepki spin sayısı | 3–4 |
| Scatter bonus ortalama (Aşama 1) | ~50 spinde bir |
| Scatter bonus ortalama (Aşama 2) | ~75 spinde bir |

---

## 6. Belirsizlikleri gideren notlar

- **“Küçük kazanç”:** Tek spinde tumble veya düşük çarpanlı kazanç; üst limit örn. bahisin 20x’i.
- **“Büyük kazanç kapalı”:** Tek spin veya bonus toplam ödeme üst limiti (örn. bahisin 50x’i veya aşama tablosundaki max).
- **“Kontrollü düşüş”:** RTP/limit ile ortalama negatif EV; ani tek seferde bakiye kesilmez.
- **“Bonus satın alındıysa max = maliyet ± %X”:** O bonus oyununun toplam ödemesi bu tavanı aşmasın.
- **Zorluk 4–12:** Projedeki mevcut skalada 4 en kolay, 12 en zor; aşama tablosu 5–11 kullanıyor.

Bu belge, kod tarafında aşama bazlı zorluk ataması, esnek kural motoru ve takip/analiz verileri için tek referans olarak kullanılabilir. İstersen bir sonraki adımda bu spec’e göre `SenaryoYoneticisi` ve ilgili servislerde hangi alanların ekleneceğini madde madde çıkarabiliriz.
