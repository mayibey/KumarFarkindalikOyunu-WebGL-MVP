# Senaryo: Tüm Aşamalar, Şartlar ve Zorluk (Tek Tablo)

**Amaç:** Her aşamanın çıkış şartlarını ve zorluk değerini tek tabloda görmek; zorlukları birlikte belirleyip koda yansıtmak.

**Geçiş kuralı:** Her aşamadan çıkmak için **en az 2 şart** sağlanmalı (veya yedek satırındaki koşul).

---

## Tablo: Aşama | Şartlar | Yedek geçiş | Zorluk (mevcut) | Zorluk (yeni)

| # | Aşama adı | Çıkış şartları (en az 2) | Yedek geçiş | Zorluk (şu an kod) | Zorluk (öneri) |
|---|-----------|---------------------------|-------------|--------------------|----------------|
| **1** | **Isındırma / Umut** | • Bu aşamada spin ≥ 80<br>• Bakiye ≥ başlangıç 1,5x (30.000 TL)<br>• Bahis artırımı ≥ 1<br>• Bonus görüldü ≥ 1 | Bu aşamada 150 spin | **5** | _değiştirilecek_ |
| **2** | **Kontrol bende** | • Bu aşamada spin ≥ 50<br>• Bahis artırımı ≥ 2<br>• Bu aşamada spin ≥ 10 | Bu aşamada 100 spin | **8** | _değiştirilecek_ |
| **3** | **Az daha / Kayıp kovalama** | • Yükleme ≥ 2<br>• Toplam yatırılan ≥ başlangıç 1,5x<br>• Bonus satın al ≥ 1 | Bu aşamada 120 spin veya 2. yükleme | **8** | _değiştirilecek_ |
| **4** | **Bakiye tükenişi** | • Yükleme ≥ 2<br>• Bu aşamada spin ≥ 25<br>• Bonus satın al ≥ 2 | 3. yükleme | **8** | _değiştirilecek_ |
| **5** | **Bonus zirve** | • Bu aşamada spin ≥ 35<br>• Bonus görüldü ≥ 2 (en az 1 bonus daha) | Bu aşamada 50 spin | **8** | _değiştirilecek_ |
| **6** | **Gerçek kayıp** | • Toplam yatırılan ≥ başlangıç 2,5x (50.000 TL)<br>• Net zarar ≥ başlangıç %30 (6.000 TL) | 3. yükleme | **8** | _değiştirilecek_ |
| **7** | **Finale** | — (son aşama) | — | **8** | _değiştirilecek_ |

---

## Zorluk skalası (projede)

- **4** = En kolay (sık tumble, yüksek RTP hissi)
- **5** = Kolay (ısındırma için şu an kullanılan)
- **6–7** = Orta
- **8** = “Gerçek” Sweet Bonanza benzeri (öneri dokümana göre)
- **9–12** = Zor / çok zor (tumble seyrek, kayıp hissi artar)

---

## Öneri (literatür + akış tablosuna göre)

| Aşama | Öneri zorluk | Gerekçe (kısa) |
|-------|--------------|-----------------|
| 1 – Isındırma | **5** | Erken güven, küçük kazançlar sık |
| 2 – Kontrol bende | **6** | Kazançlar azalsın ama tam kesilmesin, “tepki veriyor” hissi |
| 3 – Az daha | **7** | Near-miss ağırlıklı, gerçek kazanç seyrek |
| 4 – Bakiye tükenişi | **8** | Yüksek zorluk, yükleme eşiği |
| 5 – Bonus zirve | **6** (bonus içi kolay), sonrası **8** | Zirve anı ver, sonra sert düş |
| 6 – Gerçek kayıp | **9** | En zor, “şans bitti” hissi |
| 7 – Finale | **8** veya **9** | Oyun bitişi, kazanç engelli |

---

## Sonraki adım

Bu tabloda “Zorluk (öneri)” sütununu seninle doldurup netleştireceğiz. Değerleri onayladığında `SenaryoYoneticisi` içinde aşama bazlı zorluk atamasını (şu an sadece 1=5, diğerleri 8) bu tabloya göre güncelleyeceğim.

**Başlangıç bakiyesi (referans):** 20.000 TL → 1,5x = 30.000 TL, 2,5x = 50.000 TL, %30 = 6.000 TL.
