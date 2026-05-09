# Tüm Bölümler İçin Olası Sorunlar ve Çözüm Önerileri

Her bölümde: **Olası sorunlar** ve **Çözüm önerileri** madde madde listelenmiştir.

---

## 1. Senaryo yönetimi ve aşama geçişi

### Olası sorunlar
- Aşama geçişi “en az 2 şart” sağlansa bile bazen tetiklenmeyebilir (gelistirmeModu, `this != I` veya sahne değişimi sonrası I’ın yanlış instance’a işaret etmesi).
- `asamaGirisSpinIndex` sadece PlayerPrefs’ten yükleniyor; ilk açılışta veya Reset sonrası 0, ama `toplamSpin` profil ile yüklendiği için “bu aşamada spin” farkı yanlış hesaplanabilir (örn. 2. aşamada 90. spinde girilmiş, toplamSpin 140 → spinFarki 50; doğru).
- Manuel aşama dropdown’dan geçiş yapılınca zorluk/ayarlar güncelleniyor ama `asamaGirisSpinIndex` manuel geçişte güncellenmiyor olabilir; “bu aşamada spin” sayacı yanlış olabilir.
- Aşama 7 (Finale) sonrası geçiş yok; kullanıcı tekrar oynamak isterse sadece Reset veya yeni profil ile başlayabilir.

### Çözüm önerileri
- Aşama geçişini her spin/bonus spin sonunda tek noktadan çağır; `AsamaGecisiKontrol()` içinde sadece `this == I` ve `mevcutAsama != Finale` kontrolü kalsın, gelistirmeModu senaryo sahnesinde geçişi engellemesin (mevcut davranışı koruyun).
- Manuel geçişte `AsamaGecir()` zaten çağrılıyor; `AsamaGecir` içinde `asamaGirisSpinIndex = toplamSpin` atandığından emin olun; böylece “bu aşamada spin” doğru hesaplanır.
- Finale sonrası için: “Yeniden oyna” / “Log ekranına git” butonu veya otomatik log sahnesine yönlendirme eklenebilir.

---

## 2. Ekonomi, kasa ve ödenebilir limit

### Olası sorunlar
- Senaryo ödenebilir bütçesi (`_senaryoOdenebilirKalanTL`) 0.4 sn gecikmeyle yükleniyor; ilk saniyede panel 0 veya eski değer gösterebilir.
- Bonus içinde gerçek üst limit `GetBonusRemainingPayableTL()` (bonus tavanı + senaryo bütçesi min) iken panel sadece `GetSpinOdenebilirLimit()` (senaryo bütçesi) gösteriyor; bonus tavanı daha düşükse kullanıcı “neden daha az ödendi” diye düşünebilir.
- Havuz (KasaYoneticisi) Admin sahnesinde değiştirilip senaryo sahnesine geçilirse, senaryo bütçesi 100k’dan başlıyor ama havuz farklı kalabilir; tutarsızlık.
- Bakiye GameManager + EkonomiServisi + PlayerPrefs üçlüsünde; senkron atlarsa (örn. AddWinnings çağrılmadan sahne değişirse) bakiye yanlış kalabilir.

### Çözüm önerileri
- Senaryo sahnesi açıldığında ödenebilir bütçeyi mümkünse ilk frame’de (veya 1 frame gecikmeyle) yükle; 0.4 sn’yi sadece “oyun hazır” için kullan, bütçe yüklemesini daha erken yap.
- Bonus sırasında MevcutAyarlarMetni’nde iki satır: “Ödenebilir bütçe: X TL” ve “Bonus kalan tavan: Y TL”; gerçek limit = min(X,Y) açıklaması eklenebilir.
- Senaryo sahnesine girerken havuzu senaryo için sabit (örn. 1M) yapmayı zorunlu kılmayın; sadece ödenebilir bütçe 100k ile başlasın ve havuzu ayrı göstermeyin (veya “Senaryo modunda havuz gösterilmez” deyin).
- Tüm bakiye değişimlerini EkonomiServisi.AddWinnings / DeductBet / BakiyeYukle üzerinden yapın; GameManager.ActivePlayer.balance güncellemesi tek yerden (EkonomiServisi veya OyunYoneticisi callback’i) yapılsın.

---

## 3. Spin, bonus ve animasyon

### Olası sorunlar
- Simülasyon 200 denemede uygun sonuç bulamazsa `sonDenemeKayit` döndürülüyor; nadiren çok kolay/zor veya tutarsız bir grid ile animasyon oynatılabilir.
- Bonus satın alındığında Aşama 1 tavanı (maliyet + %30) uygulanıyor; Aşama 2–4’te satın alınan bonus için spec’teki tavanlar (+%20, -%20, -%50) yok.
- Zorla çarpan + toggle kullanılırken animasyon bazen atlanıyormuş gibi hissedilebilir (simülasyon hızlı bitiyorsa).
- Boşluk tuşu ile spin tetiklenirken scatter sayısı ilk grid + mevcut grid max ile alınıyor; edge case’te (çok hızlı art arda spin) bir spin atlanabilir veya çift tetiklenebilir.

### Çözüm önerileri
- Simülasyon max deneme sayısına ulaşırsa log atın; gerekirse “güvenli” varsayılan bir grid (örn. tumble’sız) üretip animasyonu onunla oynatın.
- Bonus tavanlarını aşama bazlı uygulayın: SenaryoYoneticisi.mevcutAsama’ya göre BaslatBonus veya bonus spin ödeme sırasında cap = maliyet * (1 + veya - yüzde) hesaplayın; GetBonusRemainingPayableTL veya RecordBonusPayment öncesi bu cap’i uygulayın.
- Zorla çarpan + toggle için: SimulasyonKaydiniOynat her zaman çağrılsın, kayit null olmasın (mevcut sonDenemeKayit fallback’i bunu azaltıyor).
- Boşluk tuşu spin: NormalSpinAkisi tek seferde çalıştığı ve spinCalisiyor ile kilitlendiği için çift tetikleme riski düşük; scatter sayısı için mevcut Mathf.Max(ilkGrid, simdi) mantığı korunsun.

---

## 4. UI ve paneller

### Olası sorunlar
- SenaryoYoneticisi DontDestroyOnLoad ile kalınca, başka sahneye geçildiğinde asamaText, spinText, mevcutAyarlarMetni vb. eski sahneye ait olduğu için destroy; UI_Guncelle() null reference veya görünmez güncelleme yapabilir.
- Log sahnesinde SenaryoYoneticisi.I hâlâ eski instance ise GetOturumLogu() doğru listeyi verir; ama uygulama kapatılıp açılırsa I yeni instance, oturumLogu boş; geçmiş oturum logu görünmez.
- Bahis butonları başka bir script (örn. SahneBaglama veya Admin) tarafından sonradan tekrar bağlanırsa EkonomiServisi’e dönüp Senaryo paneli güncellenmeyebilir.
- MevcutAyarlarMetni’nde zorluk, scatter %, çarpan % OyunYoneticisi’nden okunuyor; Admin sahnesinde slider’lar bu değerleri değiştirir, senaryo sahnesinde ise sadece aşama ayarı uygulanır—aynı OyunYoneticisi referansı karışıklığa yol açmaz ama “hangi sahnedeyim” bilgisi UI metninde belirtilmez.

### Çözüm önerileri
- SenaryoYoneticisi.UI_Guncelle() başında: Eğer mevcut sahne adı senaryo sahnesi değilse (SceneManager.GetActiveScene) veya asamaText == null ise güncelleme yapma; böylece destroy edilmiş UI’a yazma hatası olmaz.
- Oturum logunu kapatırken veya sahne değişirken (Senaryo sahnesinden çıkarken) GameManager.ActivePlayer’a veya ayrı bir “lastScenarioLog” dosyasına kaydedin; Log sahnesi açıldığında önce bu kayıttan yükleyip sonra I.GetOturumLogu() varsa onu da ekleyin.
- Bahis butonları sadece OyunYoneticisi.Init/Start içinde tek noktadan bağlansın; başka script’ler RemoveAllListeners + AddListener(BahisArttir/BahisAzalt) yapmasın veya yapıyorsa aynı OyunYoneticisi metotlarını kullansın.
- MevcutAyarlarMetni’ne isteğe bağlı “Mod: Senaryo” / “Aşama: X” gibi bir satır eklenebilir; böylece verilerin kaynağı netleşir.

---

## 5. Kalıcılık (kayıt / yükleme)

### Olası sorunlar
- bahisArtirimSayisi ve sonBahisArtirimSpinIndex kaydedilmiyor; oyun kapatılıp açılınca 0 ve -1. Geçiş şartları ve “bahis artırımı sonrası 3–4 spin” kuralı sıfırlanıyor.
- Senaryo ödenebilir bütçesi PlayerPrefs’e yazılıyor; aynı cihazda birden fazla kullanıcı (profil) varsa bütçe paylaşılır (profil bazlı key yok).
- profiles.json ile PlayerPrefs aynı anda kullanılıyor; bakiye hem profile hem PP’de; biri güncellenip diğeri güncellenmezse tutarsızlık.
- Kasa (ana kasa, havuz) PlayerPrefs’te; senaryo sahnesinde “havuz 1M” yapmıyoruz artık ama Admin’de havuz 100k yapılıp senaryo açılırsa havuz 100k kalır; senaryo bütçesi 100k ise örtüşür ama kafa karıştırabilir.

### Çözüm önerileri
- bahisArtirimSayisi ve sonBahisArtirimSpinIndex’i PlayerPrefs’e yazın (örn. PP_SENARYO_BAHIS_DEGISIKLIK_SAYISI, PP_SENARYO_SON_BAHIS_SPIN); SenaryoYoneticisi.Start’ta okuyup ata; Reset’te sil.
- Ödenebilir bütçe ve senaryo aşaması için key’e profil ID’si ekleyin: `PP_SENARYO_ODENEBILIR_{playerId}` gibi; böylece kullanıcı bazlı olur.
- Tek kaynak kuralı: Bakiye sadece GameManager.ActivePlayer.balance; EkonomiServisi bu değeri okuyup yazar, PlayerPrefs sadece ActivePlayer yoksa (giriş yapılmamış) yedek olsun. Save/Load tüm ekonomi için GameManager.SaveProfiles ile yapılsın.
- Senaryo sahnesi açılışında “senaryo modunda ödül havuzu X gösterilmiyor / sadece ödenebilir bütçe kullanılır” denebilir; havuzu zorla 1M yapmayın ki Admin’de yapılan ayar bozulmasın.

---

## 6. Sahne yönetimi ve singleton’lar

### Olası sorunlar
- SenaryoYoneticisi.I, senaryo sahnesinden Log veya Admin’e geçince hâlâ eski instance; yeni sahnede OyunYoneticisi farklı, FindObjectOfType<OyunYoneticisi> yeni sahnenin OY’unu döner—senaryo mantığı yanlış OY üzerinde çalışabilir.
- GameManager.I DontDestroyOnLoad; sahne değişince tek kalır, sorun yok; ama OyunYoneticisi/KasaYoneticisi sahneye özgü; referanslar eski sahneye ait kalırsa null veya yanlış obje.
- 02_SenaryoluOyun tekrar yüklendiğinde yeni bir SenaryoYoneticisi oluşur; Awake’te I != null && I != this ise destroy ediliyor; böylece eski I kalır. Eski I’ın UI referansları eski sahneye ait (destroy); yeni sahnedeki panel güncellenmez.

### Çözüm önerileri
- Senaryo sahnesi yüklendiğinde (scene name 02_ veya “Senaryo” içeriyorsa) Awake’te I’ı **her zaman** this yapın ve DontDestroyOnLoad(this) uygulayın; böylece her zaman senaryo sahnesindeki instance “canlı” olur. Diğer sahnelerde SenaryoYoneticisi yoksa I bir kez set edilmiş kalır; senaryo sahnesine dönünce yeni instance I olur, eski destroy edilir (I = this yapıldığında eski referansı tutan yok).
- Alternatif: I’ı sahne yüklenince “aktif senaryo yöneticisi” olarak güncelle; 02_ yüklendiğinde sahnedeki SenaryoYoneticisi I = this desin, böylece yeni sahnedeki UI bu instance’a bağlı olsun.
- OyunYoneticisi/Kasa referansı için: SenaryoYoneticisi sadece “şu an aktif sahne 02_” ise FindObjectOfType<OyunYoneticisi> kullansın; değilse (farklı sahne) OY’a dokunmasın.

---

## 7. Zorluk ve aşama ayarları

### Olası sorunlar
- Yükleme sonrası 20 spin kuralı artık sadece aşama 1–3’te; spec’te Aşama 4 için de “yükleme sonrası ilk 20 spin küçük kazanç” var—şu an aşama 4’te bu uygulanmıyor (bilinçli tercih: kullanıcı aşama 4’te 8 görsün diye).
- Aşama 2’de “bahis düşürüldüyse 1–2 spin zorluk 7” spec’te var; kodda sadece “bahis artırımı sonrası 3–4 spin zorluk 5” var, bahis düşürme tepkisi yok.
- Zorluk 4–12 aralığında; aşama 5–6–7 için 9–10–11 veriliyor; “Finale’de tumble/ödeme 0” için ek mantık yok, sadece zorluk 11 (çok zor).

### Çözüm önerileri
- Aşama 4’te de “yükleme sonrası 20 spin” istiyorsanız: kuralı tekrar aşama 4’ü de kapsayacak şekilde açın ama **gösterilen** zorluğu “8 (yükleme sonrası geçici: 6)” gibi metinle belirtin; böylece kullanıcı 6 görünce kafası karışmaz.
- Bahis **azaltma** sonrası 1–2 spin zorluk 7: sonBahisAzaltmaSpinIndex ekleyin; BahisAzalt’ta bu indeksi güncelleyin; AsamaAyariniUygula’da aşama 2 için spinFarkiAzaltma <= 2 ise zorluk = 7 uygulayın.
- Finale’de SimuleEtVeKaydet veya ödeme katmanında mevcutAsama == Asama7_Finale ise fillLimit = 0 veya ödeme 0 zorunlu kılın; böylece gerçekten kazanç üretilmez.

---

## 8. Spec uyumu ve eksik özellikler

### Olası sorunlar
- Aşama 2: scatter bonus ~75 spinde bir; kodda aşama 2’ye özel scatter oranı yok, genel scatterChanceNormal kullanılıyor.
- Aşama 3: near-miss (3 scatter sık), bonus içinde yüksek çarpan görünsün tumble olmasın; ayrı mekanizma yok.
- Aşama 5: 3. yükleme sonrası ilk bonus yüksek ödeme (2.5x maliyet); “ilk bonus” ve yuklemeSayisi == 3 kontrolü yok.
- Aşama 6: bonus “etkisiz”; pratikte bonus tavanı çok düşük veya 0 yapılabilir, şu an aşama bazlı bonus tavanı sadece Aşama 1 satın al için var.

### Çözüm önerileri
- Aşama 2’de GetScatterChanceFor (veya SenaryoServisi delegasyonu) mevcutAsama == Asama2 ise daha düşük bir oran dönsün (~1/75 hedef).
- Near-miss: Aşama 3’te scatter üretimini (sadece 3’lü, 4’e tamamlanmasın) ayrı bir “nearMissChance” veya sembol dağılımı ile simüle edin; bonus içinde “yüksek çarpan göster, tumble üretme” için bonus spin simülasyonunda tumble adımı 0 ile sınırlayan bir bayrak eklenebilir.
- 3. yükleme sonrası ilk bonus: BakiyeYukle’da yuklemeSayisi == 3 ise bir “birSonrakiBonusYuksekOdeme” bayrağı set edin; BaslatBonus’ta bu bayrak varsa bonus tavanını maliyet * 2.5 yapın; bonus bittikten sonra bayrağı kaldırın.
- Aşama 6 (ve 7) için GetBonusRemainingPayableTL veya bonus ödeme hesabında mevcutAsama’ya göre cap’i çok düşük (örn. maliyet * 0.1) veya 0 yapın.

---

## 9. Performans ve kod yapısı

### Olası sorunlar
- OyunYoneticisi çok büyük; birçok arayüz ve servis tek sınıfta; değişiklik yaparken yan etki riski yüksek.
- FindObjectOfType<OyunYoneticisi> birçok yerde her UI_Guncelle veya AsamaAyariniUygula’da çağrılıyor; sık çağrıda maliyet artar.
- Coroutine zinciri uzun; spin → bonus → bonus end → otomatik spin devam; bir yerde break olursa (exception, disable) state yarıda kalabilir (spinCalisiyor true, butonlar kapalı).

### Çözüm önerileri
- OyunYoneticisi’nden tumble/simülasyon mantığını Unity’den bağımsız saf sınıflara taşıyın; orkestrasyon OY’da kalsın, hesaplama servislerde olsun (proje kurallarıyla uyumlu).
- SenaryoYoneticisi’ne OyunYoneticisi referansını Start’ta veya GecikmeliMevcutDurumYenile’de bir kez FindObjectOfType ile alıp cache’leyin; UI_Guncelle/AsamaAyariniUygula cache’i kullansın; sahne değişince cache’i null’a çekin veya sahne 02_ ise yeniden ata.
- Kritik state (spinCalisiyor, bonusAktif) için “temizleyici”: bonus bittiğinde veya coroutine exception’da spinCalisiyor = false, ButonDurumu(true) çağrılsın; mümkünse try/finally ile garantileyin.

---

## 10. Log sahnesi ve raporlama

### Olası sorunlar
- Uygulama kapatılıp açılıp doğrudan Log sahnesi açılırsa SenaryoYoneticisi.I yeni instance veya null, oturumLogu boş; senaryo olayları listelenmez.
- Profil logları (profile.logs) ile senaryo olayları (SenaryoOlayKaydi) farklı format; aynı listeye yazılınca “zaman / tip / mesaj” alanları farklı anlamda (biri type= BONUS_BUY, diğeri olayTipi= AsamaGirisi).

### Çözüm önerileri
- Son senaryo oturum logunu (oturumLogu) uygulama kapanırken veya senaryo sahnesinden çıkarken JSON/dosya veya profile’a serileştirip kaydedin; Log sahnesi açıldığında önce bu kaydı yükle, varsa “Senaryo olayları (son oturum)” başlığıyla listele.
- Listede her satırda “Kaynak: Senaryo” / “Kaynak: Genel” etiketi veya ayrı iki bölüm (üstte senaryo olayları, altta genel loglar) kullanın; format farkı kullanıcıyı yanıltmaz.

---

## Özet tablo

| Bölüm | Kritik sorunlar | Öncelikli çözüm |
|-------|------------------|-----------------|
| Senaryo / aşama | I eski instance, manuel geçişte asamaGirisSpinIndex | Sahne adına göre I güncelle; AsamaGecir’de asamaGirisSpinIndex = toplamSpin |
| Ekonomi / ödenebilir | Bonus’ta gösterilen limit ≠ gerçek limit; gecikmeli yükleme | Bonus UI’da “bonus tavan + bütçe” ikisi de yazılsın; bütçe erken yüklensin |
| Spin / bonus | Aşama 2–6 bonus tavanları yok; simülasyon fallback | Aşama bazlı bonus cap; sonDenemeKayit log |
| UI | Destroy edilmiş UI’a yazma; log sahnesinde boş liste | UI_Guncelle’de sahne/null kontrolü; oturum logu kalıcı kayıt |
| Kalıcılık | bahis sayacı kayboluyor; çoklu profil bütçe karışır | PP’ye bahis sayacı; profil bazlı key |
| Singleton / sahne | I eski kalınca yanlış OY | 02_ yüklendiğinde I = sahnedeki instance; cache OY |
| Zorluk | Aşama 4 yükleme kuralı; bahis düşürme; Finale 0 | İsteğe bağlı aşama 4 kuralı; sonBahisAzaltmaSpinIndex; Finale’de ödeme 0 |
| Spec | Scatter 75, near-miss, 3. yükleme bonus, aşama 6 bonus | Aşama bazlı scatter; near-miss/bonus tumble 0; bayrak 2.5x; aşama 6 cap |
| Kod / performans | OY büyük; FindObjectOfType sık | Refaktör; SenaryoYoneticisi OY cache |
| Log sahnesi | Kapat-aç sonrası senaryo logu yok; format karışık | Oturum logu dosyaya/profile; iki bölüm (senaryo / genel) |

Bu belge, **OLASI_SORUNLAR_VE_COZUMLER** başlığıyla tüm bölümler için tek referans olarak güncellenebilir.
