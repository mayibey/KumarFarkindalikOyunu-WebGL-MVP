# Kritik Ayar ve Spin Akışı — Analiz Raporu

Bu dokümanda: ödeme havuzu %10 kuralı, ödenebilir tutar aşımı, spin sonucunun önceden belli olması, zorluk–tumble ilişkisi ve meyve (sembol) olasılıklarının ödeme katsayılarına göre olması konuları incelendi; eksikler ve riskler madde madde çıkarıldı.

---

## 1. Ödenebilir tutar (%10) — Mevcut durum

- **OdemeServisi.GetSpinOdenebilirLimit():** `havuz * 0.10f` ile spin başına limit hesaplanıyor. ✅
- **Bonus:** GetBonusRemainingPayableTL / bonus bütçe/cap ayrı; bonus akışında kalan ödenebilir tutar kullanılıyor. ✅
- **Normal spin:** Spin başında `_spinOdenebilirLimit = GetSpinOdenebilirLimit()` alınıp FillRandomAll(odenebilirNormal) ve log’a veriliyor. ✅

Yani “havuzun %10’u ödenebilir” kuralı **tanım** olarak var; asıl sorun bu limitin **tüm spin (ilk düşüş + tüm tumble’lar + çarpan) boyunca** gerçekten aşılmamasının garanti edilmemesi ve spin sonucunun önceden sabitlenmemesi.

---

## 2. Spin sonucu önceden belli olmuyor (kritik)

**İstediğin:** Spin butonuna basıldığında spinin **tüm sonucu** (ilk grid + tumble’lar + toplam ödeme) belli olsun. Limit aşacaksa **grid yeniden üretilsin** (re-roll); kullanıcı sadece **geçerli** sonucu görsün. Ne “tumble yapabilecek sayıda meyve var ama tumble yok” ne de “limit aşıldığı halde tumble ödendi” olsun.

**Mevcut akış:**

1. **İlk doldurma — FillRandomAll(odenebilirLimit)**  
   - Grid rastgele dolduruluyor.  
   - **Sadece ilk cluster** için: `hamKazanc > kalanLimitTL` ise **tüm grid değiştiriliyor** — tumble oluşmayacak şekilde semboller seçiliyor (RandomNonScatterSymbol ile “güvenli” grid).  
   - Sonuç: Limit aşılmasın diye **bilinçli olarak tumble engelleniyor**; kullanıcı “8’lik görünüm var ama patlamıyor” veya “neden hep tumble yok” algısına kapılabilir. Yani “tumble oluşturacak sayıda meyve gelmesine rağmen tumble yapmamayı meydana getirmemeli” kuralı **ihlal** ediliyor.

2. **Normal spin tumble döngüsü**  
   - TumbleLoop **sadece bonus** için limit kontrolü yapıyor:  
     `if (_ctx.GetBonusAktif()) { ... if (projeksiyon > kalanOdenebilir) break; }`  
   - **Normal spinde** (bonus yokken) döngüde **hiç ödenebilir limit kontrolü yok**.  
   - Yani: İlk grid limiti geçmese bile, tumble’lar ardı ardına gelip **toplam kazanç _spinOdenebilirLimit’i aşabilir**; sonunda yine de PayFromHavuz(spinKazanci) ile ödeme yapılıyor. Limit aşımı **engellenmiyor**, sadece havuz yetmezse kasa tarafında kısıtlama var.

3. **Bonus tumble döngüsü**  
   - Projeksiyon > kalanOdenebilir olunca döngü **break** ediliyor.  
   - Yani: Ekranda hâlâ tumble yapacak cluster varken **tumble durduruluyor**; kullanıcı “patlaması gereken yerler patlamadı” görebilir. “Ödeme tutarının aşıldığı bir tumble gerçekleşmemeli” kısmı sağlansın isteniyor ama “görünürde tumble varken tumble yapmamak” da istenmiyor — mevcut model ikincisine yol açıyor.

**Eksik / risk özeti:**

- Spin sonucu **önceden hesaplanmıyor**; grid + tumble’lar **canlı** oynanıyor, limit ancak “tumble’ı kes” veya “ilk grid’i değiştir” ile korunuyor.
- Normal spinde tumble toplamı **limit ile kısıtlanmıyor**; tek spin ödemesi %10’u aşabilir.
- Limit aşımını engellemek için ya **tumble bastırılıyor** (güven kaybı) ya da **tumble ortada kesiliyor** (tutarsız görüntü).

**Önerilen yön:**  
“Spin butonuna basıldığında sonuç belli olsun” için **tam spin simülasyonu** (deterministic):

- Grid doldur → tumble’ları **simülasyon** modunda (animasyonsuz) çalıştır → toplam ödeme + çarpanı hesapla.
- Toplam > ödenebilir limit ise **baştan grid üret** (re-roll), tekrar simüle et; limiti aşan sonuç **asla** kullanıcıya gösterilmesin.
- Geçerli sonucu bir kez sabitledikten sonra: ya **sadece bu sonucu** göster (tek görüntü) ya da **aynı sonucu** adım adım animasyonla oynat (tumble sayısı ve ödemeler önceden belli).

Bu sayede:  
- Ne “tumble varken tumble yok” ne “limit aşan tumble” olur.  
- Kullanıcı her zaman **tek, tutarlı, limit dahilinde** bir spin görür.

---

## 3. Zorluk (4–12) ve tumble sıklığı

**İstediğin:**  
- Zorluk 4: Ödenebilir tutarı da dikkate alan, **sık ama her spinde zorunlu değil** ödemeler; limit (örn. 500 TL) altında tumble olasılığı **yüksek**.  
- Zorluk 12: **Neredeyse hiç tumble** olmamalı.

**Mevcut durum:**

- **minClusterSize** ZorlukServisi’nde **hep** `OyunKorumaServisi.TUMBLE_SABIT_ESIK` (8) atanıyor; yani zorluk 4 veya 12 olsa da **tumble eşiği hep 8**. Zorluğa göre 6 / 10 / 12 gibi değişen bir eşik **yok**.
- Zorluk **sadece** şunları etkiliyor:  
  - **Bias:** easyBias01 / hardBias01 (4–8 kolay, 8–12 zor).  
  - **Scatter şansı:** 4’te daha yüksek, 12’de daha düşük.  
- IzgaraServisi’nde refill sembol seçimi **bias** ile ağırlıklandırılıyor:  
  - Kolayda “8’e tamamlayacak” semboller daha sık seçiliyor (tumble daha sık).  
  - Zorda “8’e tamamlayacak” semboller baskılanıyor (tumble seyrek).

Bu, “zorluk 4’te sık tumble, 12’de neredeyse yok” beklentisiyle **kısmen** uyumlu; ancak:

- Zorluk 4’te **ödenebilir limit** ile uyum (örn. 500 TL altında tumble olasılığının yüksek olması) **açıkça** kodda yok; limit sadece FillRandomAll’da ilk cluster için kullanılıyor, tumble sıklığı limit ile **ilişkilendirilmiyor**.
- “Her spinde ödeme zorunlu” hissi: Şu an böyle bir zorunluluk yok; sadece bias ile olasılıklar değişiyor. Bu kısım beklentiyle uyumlu.

**Eksik:**  
- Zorluk 4’te “limit (örn. 500 TL) altında tumble olasılığı çok yüksek” için **limit bilgisinin** sembol/refill veya tumble kararına (veya simülasyon re-roll kriterine) girmesi gerekir.  
- İsteğe bağlı: Zorluk 12’de tumble’ı neredeyse sıfıra indirmek için **minClusterSize**’ı zorluğa göre yükseltmek (örn. 10–12) düşünülebilir; şu an sadece bias var.

---

## 4. Meyve (sembol) düşme olasılığı ve ödeme katsayıları

**İstediğin:**  
Yüksek ödeme veren sembolün **düşme olasılığı daha az** olsun (ödeme katsayılarına göre ters ağırlık).

**Mevcut durum:**

- **IzgaraServisi.RandomNonScatterSymbol** (ve refill tarafı): Ağırlıklar **sadece** şunlara göre:
  - Mevcut grid’deki sembol **sayıları** (counts),
  - “8’e yakın tamamlayan” sembollere verilen **bias** (easy/hard),
  - dominant sembol, MIN_CLUSTER_SIZE’a yakınlık vb.
- **PayTable / GetPayForCount / ödeme katsayıları** sembol seçiminde **hiç kullanılmıyor**. Yani “yüksek ödeme veren = daha az düşsün” **yok**.

**Eksik:**  
Sembol seçim ağırlıklarında **PayTable (veya eşdeğer katsayı)** kullanılmalı:  
- Örneğin sembol indeksi `i` için `payTable[i]` yüksekse ağırlık `w[i]` düşük verilmeli (ters oran veya ters karekök vb.).  
- Böylece yüksek ödeme veren semboller daha az sıklıkta gelir; düşük ödeme verenler daha sık.

---

## 5. Zorluk 4’te aynı meyve adet dağılımı (8–9 çoğunluk, 10–11 azınlık, 12+ çok az)

**İstediğin:**  
Zorluk 4’te “aynı meyvenin adet sayısı deli gibi artmasın”; çoğunlukla **8–9**, azınlıkla **10–11**, çok az **12+** sembol (cluster boyutu dağılımı).

**Mevcut durum:**

- Cluster boyutu, grid’deki **bağlı aynı sembol sayısı** ile belirleniyor; refill’deki bias “8’e tamamlayan” sembolleri artırıp azaltıyor ama **cluster boyutunun dağılımı** (8 vs 9 vs 10 vs 12+) açıkça modellenmiyor.
- Yani “çoğunlukla 8–9, az 10–11, çok az 12+” için **açık bir dağılım kuralı** (ör. refill veya cluster kabulünde boyut ağırlığı) yok.

**Eksik:**  
Zorluk 4 (ve gerekirse diğer zorluklar) için:  
- Refill veya cluster oluşumunda **cluster boyutuna** göre ağırlık (8–9 ağır, 10–11 orta, 12+ düşük) eklenebilir;  
- Veya “8’e tamamlayan” seçiminde, 8–9’u tercih edip 10–11–12+’yı baskılayan ek kurallar konabilir.

---

## 6. Özet tablo

| Konu | İstenen | Mevcut | Eksik / Risk |
|------|--------|--------|----------------|
| **Ödenebilir tutar** | Havuzun %10’u, tek spinde aşılmamalı | Limit hesaplanıyor; normal spinde tumble toplamı limitlenmiyor; bonus’ta tumble kesiliyor | Normal spin tumble’da limit yok; bonus’ta “tumble kes” güven sorunu |
| **Spin sonucu önceden belli** | Spin basılınca sonuç sabit; limit aşacaksa re-roll, kullanıcı sadece geçerli sonucu görsün | Sonuç canlı; limit aşımı “ilk grid’i değiştir” veya “tumble’ı kes” ile | Tam spin **simülasyonu + re-roll** yok; deterministik sonuç yok |
| **Tumble bastırılmamalı** | Tumble oluşturacak sayıda meyve varken tumble yapmamak olmasın | Limit aşınca FillRandomAll grid’i “tumble olmasın” diye değiştiriyor | **Ciddi:** Kullanıcıya “sahte” grid; güven zedelenir |
| **Limit aşan tumble olmamalı** | Ödeme tutarı aşıldığı tumble gerçekleşmemeli | Bonus’ta projeksiyon > limit ise döngü break; normalde limit kontrolü yok | Bonus’ta “ortada kes” var; normalde aşım engellenmiyor |
| **Zorluk 4 – sık tumble** | Limit dahilinde, sık ödeme; 500 TL altında tumble olasılığı yüksek | Bias ile tumble sıklığı artıyor; limit ile ilişki yok | Limit bilgisi tumble/sembol kararına girmiyor |
| **Zorluk 12 – neredeyse hiç tumble** | Neredeyse hiç tumble olmamalı | Hard bias ile tumble seyrekleşiyor; eşik hep 8 | İsteğe bağlı: minClusterSize zorluğa göre 10–12 yapılabilir |
| **Meyve olasılığı ↔ ödeme** | Yüksek ödeme veren sembol daha az düşmeli | Sembol ağırlığı PayTable’a göre değil; sadece count + bias | **Eksik:** Ödeme katsayısına göre ters ağırlık yok |
| **Zorluk 4 – cluster boyutu** | Çoğunlukla 8–9, az 10–11, çok az 12+ | Cluster boyutu dağılımı açıkça modellenmiyor | 8–9 / 10–11 / 12+ dağılım kuralı yok |

---

## 7. Öncelikli aksiyon önerileri

1. **Spin sonucunu deterministik yap (simülasyon + re-roll)**  
   - Grid doldur → tumble’ları **simülasyon** (animasyonsuz) ile bitir → toplam ödeme (çarpan dahil) hesapla.  
   - Toplam > ödenebilir limit ise grid’i yeniden üret, tekrar simüle et; limiti aşan sonuç **hiç** gösterilmesin.  
   - Kullanıcıya sadece bu “onaylanmış” spin’i göster (veya aynı sonucu animasyonla oynat).  
   Böylece hem “tumble varken tumble yok” hem “limit aşan tumble” ortadan kalkar.

2. **FillRandomAll’daki “limit aşınca grid’i tumble’sız yap” mantığını kaldır**  
   - Limit aşımı **re-roll** ile çözülsün; grid’i “güvenli” diye değiştirip tumble’ı bastırmayın.

3. **Normal spin tumble döngüsüne limit kontrolü**  
   - En azından simülasyon yoksa: Her tumble turunda (spinKazancHam + bu tur) * çarpan > _spinOdenebilirLimit ise **yeni tumble başlatma** (veya mevcut tumble’ı “limit doldu” diye bitirip ödemeyi kırp). Böylece tek normal spin’de toplam ödeme %10’u aşmaz.

4. **Sembol ağırlığı ↔ PayTable**  
   - RandomNonScatterSymbol (ve gerekirse refill) ağırlıklarında PayTable/ödeme katsayısı kullan: yüksek ödeme = düşük ağırlık.

5. **Zorluk 4 – limit bilgisi**  
   - Simülasyon/re-roll veya tumble kabulünde “ödenebilir limit” (örn. 500 TL) bilgisini kullan; zorluk 4’te “limit altında tumble olasılığı yüksek” davranışını açıkça kodla.

6. **Cluster boyutu dağılımı (8–9 / 10–11 / 12+)**  
   - Zorluk 4 için refill veya cluster oluşumunda “çoğunlukla 8–9, az 10–11, çok az 12+” kuralını ekle (ağırlık veya kabul kuralı ile).

Bu sırayla uygulanırsa, hem güven hem de “kritik ayarlar birbiriyle tutarlı” hedeflerine yaklaşılır.
