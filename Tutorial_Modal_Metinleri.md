# Tutorial Bilgilendirici Modal Metinleri

**Kaynak:** `Assets/Scripts/Tutorial/TutorialAdimYoneticisi.cs:451-654`
**Temizleme:** TMP renk/biçim tag'leri (`<color>`, `<b>`) kaldırıldı. `\n\n` gerçek paragraf'a çevrildi.

---

## Tablo

| # | Adım | Tür | Metin |
|---|------|-----|-------|
| 1 | T2 | başlangıç | Az önce oyuncunun yaşadığı manipülasyon işte bu panelde kuruluyor. Üç bölüm göreceğiz: Olasılık, Manipülasyon ve Anlık Müdahale. Başlayalım. |
| 2 | T3_HOOK | başlangıç | 5 oyun modu var. Sırayla her birini deneyip aralarındaki farkı göreceğiz.<br><br>İlk senaryo: TAZE KAN (Hook). Yeni gelen oyuncu için tasarlandı — bol kazandırma, yumuşak kayıplar, 'şanslıyım' hissi. |
| 3 | T3_HOOK | aksiyon | Oyun Modu'ndan 'Taze Kan' seç, Uygula bas. 5 spin at. |
| 4 | T3_HOOK | kapanış | Gördük mü? Yeni oyuncu hemen kazandı, oyuna bağlandı. Sömürünün başlangıcı — kanca. |
| 5 | T3_YONTMA | başlangıç | İkinci senaryo: AZ AZ KAYIP (Yontma). Oyuncu farkına varmadan, küçük küçük kaybettirme. Bakiye sessizce erir. |
| 6 | T3_YONTMA | aksiyon | Oyun Modu'ndan 'Az Az Kayıp' seç, Uygula bas. 5 spin at. |
| 7 | T3_YONTMA | kapanış | Kazanç miktarları bahis miktarının hep altında olduğu için oyuncu farkında bile olmadan bakiyesi sessizce eridi... |
| 8 | T3_TUTMA | başlangıç | Üçüncü senaryo: KAÇIŞ ENGELLEME (Tutma). Oyuncu çıkmaya niyetlenirse sistem küçük kazanç hediyesi verir. |
| 9 | T3_TUTMA | aksiyon | Oyun Modu'ndan 'Kaçış Engelleme' seç, Uygula bas. 6 spin at. |
| 10 | T3_TUTMA | kapanış | Gördük mü? 2 kez kayıp, sonra ÖDEME. Yine 2 kez kayıp, yine ÖDEME. Sistem oyuncuyu tam çıkacağı anda küçük kazançla TUTUYOR. Asıl manipülasyon budur — oyuncunun kaybetmesini bekletip, küçük hediye vererek bir spin daha attırır. |
| 11 | T3_KORUMA | başlangıç | Dördüncü senaryo: BAKİYE TÜKETME (Koruma). Ödeme neredeyse durur — kasa korunur, oyuncu son kuruşa kadar kaybeder. |
| 12 | T3_KORUMA | aksiyon | Oyun Modu'ndan 'Bakiye Tüketme' seç, Uygula bas. 5 spin at. |
| 13 | T3_KORUMA | kapanış | Kazanç yok. Oyuncu son kuruşuna kadar kaybediyor. Senaryonun ilk üç adımı bağladı, son adım sömürdü. Tükeniş aşaması. |
| 14 | T4 | geçiş | **Olasılık Ayarları**<br><br>Oyun modlarını gördük — kumar sitelerinin oyuncuyu hangi senaryolarla yönlendirdiği öğrenildi.<br><br>Şimdi Olasılık Ayarları bölümüne geçiliyor. Burada sayılarla manipülasyonun nasıl yapıldığı görülecek. |
| 15 | T4 (aşama 1: %100) | başlangıç | **ÇARPAN nedir?**<br><br>Bazı spinlerde grid'e ÇARPAN sembolü düşer (×2, ×3, ×8, ×100 gibi). Kazançlı spinde çarpan varsa kazanç o sayıyla çarpılır. Örneğin 500 TL kazanılır + ×8 çarpan düşer → 4.000 TL.<br><br>Kumar siteleri bu olasılığı istediği gibi ayarlayabilir. Yüksek tutarsa kullanıcı 'vay be, sürekli çarpan geliyor' diye heyecanlanır → daha çok spin atar → daha çok kaybeder.<br><br>**Şimdi deneyelim:** Çarpan olasılığını %100 yap, Uygula bas, 1 spin at. Çarpan kesin düşecek. |
| 16 | T4 (aşama 1) | aksiyon | Çarpan olasılığını %100 yap. Uygula bas, 1 spin at. |
| 17 | T4 (aşama 2: %0) | ara modal | Çarpanları gördük — her spinde garanti düştü.<br><br>Şimdi tersini görelim: olasılık %0 olduğunda hiç çarpan düşmeyecek.<br><br>Çarpan olasılığını %0 yap, Uygula bas, 1 spin daha at. |
| 18 | T4 | kapanış | %100 ayarında çarpan kesin düştü, %0 ayarında ise hiç düşmedi. Operatör bu slider'ı oyuncuya göstermeden ayarlar — 'oyun eğlenceli' hissi için yükseltir, kasayı korumak için sıfırlar. Oyuncunun beyninde 'her an büyük kazanç olabilir' yanılgısı bu mekanizmayla üretilir. |
| 19 | T5 (aşama 1: %100) | başlangıç | **BONUS SEMBOLÜ nedir?**<br><br>Bonus sembolü (yıldız/scatter) grid'e nadir düşer. Bir spinde 4 veya daha fazla scatter olursa BONUS OYUN açılır — 10 free spin + büyük kazanç şansı.<br><br>Kumar siteleri bu olasılığı istediği gibi ayarlar. Yüksek tutarsa kullanıcı 'her spinde büyük bonus gelebilir' diye oyunu bırakamaz. Beklenti = bağımlılık.<br><br>**Şimdi deneyelim:** Bonus olasılığını %100 yap, Uygula bas, 1 spin at. Garantili bonus oyun açılacak. |
| 20 | T5 (aşama 1) | aksiyon | Bonus olasılığını %100 yap, Uygula bas, 1 spin at. |
| 21 | T5 (aşama 2: %0) | ara modal | Bonus oyun açılışını gördük — 4 scatter düştü, BONUS OYUN yazısı çıktı.<br><br>Şimdi tersini görelim: olasılık %0 olduğunda hiç bonus tetiklenmeyecek.<br><br>Bonus olasılığını %0 yap, Uygula bas, 1 spin daha at. |
| 22 | T5 | kapanış | %100 ayarında bonus garanti açıldı, %0 ayarında ise hiç açılmadı. Kumar siteleri bu slider'ı oyuncudan gizler — 'bonus geliyor' hissi yaratmak için yükseltir, kasayı korumak için sıfırlar. Oyuncunun beyninde 'biraz daha oynarsam bonus gelecek' yanılgısı bu mekanizmayla üretilir. |
| 23 | T6_YENI_OYUNCU (aşama 1: aç) | başlangıç | Şimdi operatörün GİZLİ silahını göreceğiz: Yeni Oyuncu Modu.<br><br>Bu toggle açıkken sistem oyuncuyu 'yeni gelen' sayar — ona ÖZEL bir rejim uygular: bol kazandırma, yumuşak kayıplar. 'Şanslı bir gün' hissi. |
| 24 | T6_YENI_OYUNCU | aksiyon | Manipülasyon Ayarları'nda 'Yeni Oyuncu Modu' toggle'ını AÇ ve 3 spin at (sistem oyuncuyu kazandıracak). Sonra toggle'ı KAPAT ve 3 spin daha at — fark netleşecek. |
| 25 | T6_YENI_OYUNCU (aşama 2: kapat) | ara modal | 3 spin attık (toggle açık). Sonuç: BOL KAZANÇ — sistem oyuncuyu çekiyor.<br><br>Şimdi Manipülasyon Ayarları'nda 'Yeni Oyuncu Modu' toggle'ını KAPAT. Ardından 3 spin daha at. Gerçek ortaya çıkacak. |
| 26 | T6_YENI_OYUNCU | kapanış | Gördük mü? Aynı slot, aynı bahis. Toggle AÇIK → BOL KAZANÇ, KAPALI → NET KAYIP.<br><br>Yeni Oyuncu Modu AÇIKKEN sömürü farkedilmez; oyuncu 'şanslı' sanır, oyuna bağlanır. Mod KAPANINCA gerçek RTP ortaya çıkar — kayıplar başlar.<br><br>Bu manipülasyonun adı HOOK FAZI — yeni oyuncuyu sisteme kilitleyen kanca. |
| 27 | T6 | başlangıç | Şimdi 'Kazandırma Sıklığı'na bakalım. Bu slider 5 spin'in kaçında kullanıcıya kazanç verileceğini belirler. Slider'ı ayarla — seçim sizin. |
| 28 | T6 | aksiyon | Slider'ı kaydır. Slider değeri ÷ 2 = 5'de kaç kazanç. Örneğin slider 6 → 5'de 3 kazanç. Uygula bas, 5 spin at. |
| 29 | T6 | kapanış | Slider 5'de N'e ayarlandı. 5 spin'in N tanesi kazanç oldu, kalanı kayıp. Operatör bunu istediği gibi ayarlar — oyuncu 'şanslı bir gün' veya 'şanssız' sanır ama her şey ayarlanmıştır. |
| 30 | T7 (aşama 1: maks 3x) | başlangıç | Ödeme aralığı — operatör kazançların TUTAR aralığını sınırlar. Bahis × min ile bahis × maks arasında ödeme yapar. Önce maksimumu görelim. |
| 31 | T7 (aşama 1) | aksiyon | Ödeme MAKS'ı 3'e ayarla (bahis × 3 = 3000 TL tavan). Uygula bas, 3 spin at. Kazançlar 0-3000 TL arasında olacak. |
| 32 | T7 (aşama 2: min3 maks5) | ara modal | 3 spin attık (maks 3x). Şimdi MIN ve MAKS'ı BİRLİKTE ayarlayalım.<br><br>Ödeme MIN'i 3, MAKS'ı 5 yap. 3 spin daha at. Bu sefer kazançlar 3000-5000 TL arasına GARANTİ. |
| 33 | T7 | kapanış | İlk 3 spin'de kazanç düşüktü (maks 3x = dar aralık). İkinci 3 spin'de kazanç GARANTİ 3-5x oldu (kayıp imkansız). Operatör bunu kullanıcıya 'şanslı seri' gibi gösterir — gerçekte algoritma her şeyi kontrol eder. |
| 34 | T8 | başlangıç | Near miss — 'neredeyse kazanıyordun' hissi. Slot oyununun en güçlü tuzaklarından biri. Slider'ı ayarla — near miss sayısı sizin seçiminiz. |
| 35 | T8 | aksiyon | Slider 5'de N near miss demek. 5 spin'in N tanesinde 'tam azıcık eksik' kümeler düşecek. Uygula bas, 5 spin at. |
| 36 | T8 | kapanış | Gördük mü? 7 aynı sembol düştü ama 1 EKSİK — cluster 8'den başlıyor. Oyuncunun beyni 'KAZANIYORDUM' der, oysa hiç şans yoktu. Bu manipülasyon dopamin pompalar — bağımlılığın temel mekanizması. |
| 37 | T9 | başlangıç | Kaçış Frenleme — kullanıcı kaybedip kaybedip çıkma noktasına geldiğinde operatör NE YAPAR? Onu tutmak için otomatik kazanç verir. Limit yazın: kaç kayıp sonra otomatik kazanç gelsin. |
| 38 | T9 | aksiyon | Kaçış limiti kutusuna 3 yaz. Yani 3 kayıptan sonra sistem otomatik kazanç verecek. Uygula bas, 4 spin at. |
| 39 | T9 | kapanış | İlk 3 spin tam kayıp. Oyuncu tam çıkmak istedi değil mi? Ama 4. spin kazanç geldi — sistemin 'frenleme' anı. 'İyi ki kalmışım' dedirten o anlar. Bu manipülasyon T3_TUTMA'yla kombine, ama daha PROGRAMLI: limit operatörün elinde. |
| 40 | T10 (aşama 1: kapalı ödeme) | başlangıç | Çarpan Zorla — operatörün son silahı. İstediği anda çarpan düşürür. Ama bir tuzak var: 'Çarpan Ödeme' toggle KAPALI iken çarpan düşse de ÖDEME YAPILMAZ. Sırayla görelim. |
| 41 | T10 (aşama 1) | aksiyon | Önce 'Çarpan Ödeme' toggle KAPALI iken çarpan zorla. ×500 butonuna bas. Spinin sonucunu izle. |
| 42 | T10 (aşama 2: açık ödeme) | ara modal | Gördük mü? ×500 çarpan düştü AMA meyve dizilimi cluster oluşturmadı — ödeme YAPILMADI.<br><br>Şimdi 'Çarpan Ödeme' toggle'ını AÇ ve ×500 butonuna tekrar bas. |
| 43 | T10 | kapanış | Aynı işlem, ama bu sefer ödeme yapan meyve dizilimi + çarpan düştü → MEGA KAZANÇ. Operatör bu toggle'ı kullanarak 'şu kullanıcıya bonus vereceğim' der, gerisini ayarlar. Manipülasyon %100 kontrol. |
| 44 | T11 | başlangıç | Son silah: bonus oyununu elle tetikleme. |
| 45 | T11 | aksiyon | Bonus Tetikle butonuna bas. |
| 46 | T11 | kapanış | Operatör, kullanıcı pes etmek üzereyken bonusu tetikler. 'Tam çıkıyordum şans yüzüme güldü' der oyuncu. Aslında operatör onu içeride tutmak için düğmeye bastı. |
| 47 | T_SON | kapanış | Gördün mü? 9 parametre, hepsi kullanıcının zamanını, parasını, dopamin döngüsünü kontrol için. Slot oyunlarında tesadüf yoktur — sadece tasarım vardır. Kumar tesadüf değil, mühendisliktir.<br><br>Bağımlılık yaşadığını düşünüyorsan veya yakınında biri varsa: Yeşilay Danışmanlık Hattı 0850 222 0 191 (ücretsiz, 7/24).<br><br>Bu farkındalık seninle kalsın. |

---

## Notlar

- **`<br><br>`** markdown tablo hücresinde paragraf ayracı (gerçek `\n\n` yerine). Tarayıcıda görsel paragraf boşluğu oluşur.
- **"aksiyon" satırları (3, 6, 9, 12, 16, 20, 24, 28, 31, 35, 38, 41, 45):** BELIRSIZ — bunlar büyük ihtimalle modal değil, sağ panel "yapılacaklar" listesi metni. Kesinleştirmek için `TutorialOyunYoneticisi.AdimAkisi` coroutine çağrı zinciri izlenmeli.
- **T1:** Tutorial girişi modal'ı bu dosyada bulunmadı (`TutorialAdimYoneticisi.cs` içinde T1 const'u yok). Başka bir dosyada (örn `TutorialOyunYoneticisi`) olabilir veya modal değil (sadece UI vurgu).
- **02 sahnesi (Anlatıcı/ScriptedTANI) modal metinleri** bu tabloda yok — onlar `ScriptedAsamaListesi.asset` veya `ScriptedSpinKaydi.modalMesaji` üzerinden gelir, ayrı keşif gerekir.
