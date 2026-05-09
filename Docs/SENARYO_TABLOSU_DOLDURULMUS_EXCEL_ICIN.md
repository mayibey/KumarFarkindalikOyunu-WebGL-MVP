# Senaryo Aşaması Tablosu – Doldurulmuş (Excel’e Kopyala)

**Amaç:** Excel’deki "boş" ve "BU BÖLÜMÜ GELİŞTİR" hücrelerini projedeki **gerçek değerler** ve spec ile doldurmak. Aşağıdaki metinleri ilgili sütunlara kopyalayabilirsin.

**Projeden alınan gerçek değerler:**  
`ilkBakiye = 20.000 TL`, bakiye 1,5x = 30.000 TL, 2,5x = 50.000 TL, net zarar %30 = 6.000 TL. Yükleme sabit 20.000 TL, oyun içi en fazla 2 yükleme (toplam yükleme sayacı 3’e kadar). `SenaryoYoneticisi.CikisSartlariniDegerlendir` ve `AsamaGecisiKontrol` kodundaki şartlar aynen kullanıldı.

---

## 1. Isındırma / Umut (Zorluk 5)

- **AMAÇ** *(zaten dolu; referans)*  
  Oyuncuya küçük, sık kazançlarla “oyun keyifli, kazanmak mümkün” hissi vermek; bakiye hafif yukarı trendde tutulur, büyük kazanç kapalıdır.

- **AŞAMA ÇIKIŞ ŞARTLARI** *(kodla uyumlu)*  
  **En az 2 şart sağlanır** VEYA bu aşamada **150 spin** geçilirse → Aşama 2’ye geç.  
  Şartlar: (1) Bu aşamada spin ≥ 80, (2) Bakiye ≥ başlangıç bakiyesinin 1,5 katı (20.000 → 30.000 TL), (3) En az 1 bahis değişikliği, (4) En az 1 bonus oyunu görüldü.

---

## 2. Kontrol Bende Hissi (Zorluk 6)

- **AMAÇ** *(zaten dolu; referans)*  
  Oyuncuya “kontrol bende” hissi; bahis artırımına tepki olarak 3–4 spin küçük ödemeler; kazanç sıklığı Aşama 1’den biraz az.

- **AŞAMA ÇIKIŞ ŞARTLARI** *(boştu – dolduruldu)*  
  **En az 2 şart sağlanır** VEYA bu aşamada **100 spin** geçilirse → Aşama 3’e geç.  
  Şartlar: (1) Bu aşamada spin ≥ 50, (2) Bahis değişikliği sayısı ≥ 2.

---

## 3. Az Daha / Kayıp Kovalama (Zorluk 7)

- **AMAÇ** *(zaten dolu; referans)*  
  Near-miss hissi; “az daha kazanacaktım” duygusu; bonus içinde yüksek çarpan görünsün ama tumble/ödeme düşük.

- **AŞAMA ÇIKIŞ ŞARTLARI** *(boştu – dolduruldu)*  
  **En az 2 şart sağlanır** VEYA bu aşamada **120 spin** geçilirse VEYA **toplam yükleme sayısı ≥ 2** → Aşama 4’e geç.  
  Şartlar: (1) Yükleme sayısı ≥ 2, (2) Toplam yatırılan (oturum) ≥ başlangıç bakiyesinin 1,5 katı (30.000 TL), (3) En az 1 bonus satın alma.

---

## 4. Bakiye Tükenişi & Yeniden Yükleme (Zorluk 8)

- **AMAÇ** *(boştu – dolduruldu)*  
  Bakiyeyi kontrollü düşürerek “Bakiye yetersiz / 20.000 TL yükleme yapmak ister misin?” teklifine zemin hazırlamak. Yükleme sonrası ilk 20 spinde küçük kazançlarla oyuncuyu tutmak; toplam yükleme sayacı (oyun başı + oyun içi en fazla 2) ile sınırlı yükleme hakkı sunmak.

- **AŞAMA ÇIKIŞ ŞARTLARI** *(boştu – dolduruldu)*  
  **En az 2 şart sağlanır** VEYA **toplam yükleme sayısı ≥ 3** → Aşama 5’e geç.  
  Şartlar: (1) Yükleme sayısı ≥ 2, (2) Bu aşamada spin ≥ 25, (3) Bonus satın alma sayısı ≥ 2.

- **Yazılımsal ayarlar** *(zaten kısmen dolu)*  
  Kazanç minimal; bakiye kontrollü düşüş; “Bakiye yetersiz” + “20.000 TL yükleme yapmak ister misin?”; yükleme sayacı (max 2 oyun içi); yükleme sonrası ilk 20 spin zorluk 6 (küçük kazançlar). Bonus: scatter max bahisin %20’si; satın al max maliyet −%50 (10.000 → 5.000 TL).

---

## 5. Bonus Zirve Etkisi (Zorluk 9)

- **AMAÇ** *(boştu – dolduruldu)*  
  Oyuncuya “nihayet büyük kazandım” hissini **tek seferlik** yaşatmak: 3. yükleme sonrası girilen ilk bonus oyunda yüksek ödeme (maliyet × 2,5 hedefi). Bu zirveden sonra kazançlar hızla düşer; uzun vadede kaybın kaçınılmaz olduğu mesajına zemin hazırlanır.

- **AŞAMA ÇIKIŞ ŞARTLARI** *(boştu – dolduruldu)*  
  **En az 2 şart sağlanır** VEYA bu aşamada **50 spin** geçilirse → Aşama 6’ya geç.  
  Şartlar: (1) Bu aşamada spin ≥ 35, (2) Toplam bonus görülme sayısı ≥ 2 (bu aşamada en az 1 bonus daha görüldü).

- **Yazılımsal olarak ayarlanacaklar** *(“BU BÖLÜMÜ GELİŞTİR” – dolduruldu)*  
  - Bakiye önce kontrollü şekilde eritilir; 3. yükleme yapılması için gerekli kaybettirme çalışmaları başlar.  
  - 3. yükleme yapıldıktan sonra girilen **ilk** bonus oyunda yüksek kazanç verilir: hedef **maliyet × 2,5** (scatter bonus ise benzer oran).  
  - Bu **tek seferlik** yüksek etkili sahne; sonrasında kazançlar hızla düşer (zorluk 9, ödeme limiti düşük).  
  - Kodda: `yuklemeSayisi == 3` ve bu oturumda ilk bonus açılışı tespit edilerek bu bonusta özel tavan (2,5× maliyet) uygulanacak şekilde geliştirilecek.

---

## 6. Gerçek / Uzun Vadede Kayıp (Zorluk 10)

- **AMAÇ** *(boştu – dolduruldu)*  
  Uzun vadede oyun oynandığında toplam sonucun kayıp olduğunu net göstermek. Büyük kazanç kapalı; küçük kazanç çok nadir; bonus etkisiz. “Kısa süreli zirveler olsa da toplamda kayıp kaçınılmaz” mesajını pekiştirmek.

- **AŞAMA ÇIKIŞ ŞARTLARI** *(boştu – dolduruldu)*  
  **En az 2 şart sağlanır** VEYA **toplam yükleme sayısı ≥ 3** → Aşama 7 (Finale) geç.  
  Şartlar: (1) Toplam yatırılan (oturum) ≥ başlangıç bakiyesinin 2,5 katı (20.000 → 50.000 TL), (2) Net zarar ≥ başlangıç bakiyesinin %30’u (20.000 → 6.000 TL).

- **Yazılımsal olarak ayarlanacaklar** *(“BU BÖLÜMÜ GELİŞTİR” – dolduruldu)*  
  - Büyük kazanç: **kapalı**.  
  - Küçük kazanç: **çok nadir** (tumble oranı düşük; kodda GetKazancOrani Aşama 6 için 0,05).  
  - Bonus: **etkisiz** (düşük ödeme veya pratikte kazanç yok).

---

## 7. Finale Zorunlu Geçiş (Zorluk 11)

- **AMAÇ** *(boştu – dolduruldu)*  
  Oyunu net bir mesajla kapatmak: “Bu oturumun toplam sonucu kayıptır; kısa zirveler yanıltıcıydı.” Kullanıcıya farkındalık / istatistik ekranı sunarak oturumu sonlandırmak; bu aşamadan normal oyuna geri dönüş yok.

- **AŞAMA ÇIKIŞ ŞARTLARI** *(boştu – dolduruldu)*  
  Finale **son aşamadır**; bir sonraki adım oyun dışı: farkındalık ekranı / log sahnesi / girişe dönüş. Yeni oturum yalnızca tamamen baştan başlatılarak açılır.

- **Yazılımsal olarak ayarlanacaklar** *(“BU BÖLÜMÜ GELİŞTİR” – dolduruldu)*  
  - Kazanç **tamamen kapalı** (tumble/ödeme üretilmez veya 0; kodda GetKazancOrani = 0).  
  - Oyun **yavaşlatılır** (bekleme süreleri artırılabilir).  
  - **Analiz ekranı hazırlanır**: Log sahnesine veya farkındalık özet ekranına geçiş; oturum özeti ve senaryo aşama logu gösterilir.

---

## Özet: Kodla eşleşen sayısal değerler

| Parametre | Değer |
|-----------|--------|
| Başlangıç bakiyesi (ilkBakiye) | 20.000 TL |
| Bakiye 1,5x (Aşama 1–3 çıkış şartı) | 30.000 TL |
| Bakiye / toplam yatırılan 2,5x (Aşama 6) | 50.000 TL |
| Net zarar %30 (Aşama 6) | 6.000 TL |
| Yükleme tutarı | 20.000 TL (sabit) |
| Oyun içi yükleme hakkı | 2 (toplam yükleme sayacı 3’e kadar) |
| Aşama 1: min spin çıkış (alternatif) | 150 |
| Aşama 2: min spin çıkış (alternatif) | 100 |
| Aşama 3: min spin çıkış (alternatif) | 120 |
| Aşama 5: min spin çıkış (alternatif) | 50 |
| Yükleme sonrası “ılık” spin sayısı | 20 |
| Bahis artırımı sonrası tepki spin sayısı | 3–4 |

Bu dokümandaki tüm “AMAÇ”, “AŞAMA ÇIKIŞ ŞARTLARI” ve “Yazılımsal olarak ayarlanacaklar” metinleri doğrudan Excel’deki ilgili hücrelere kopyalanabilir. Projedeki `SenaryoYoneticisi.cs` ve `SENARYO_ASAMA_ZORLUK_VE_AYARLAR_SPEC.md` ile uyumludur.
