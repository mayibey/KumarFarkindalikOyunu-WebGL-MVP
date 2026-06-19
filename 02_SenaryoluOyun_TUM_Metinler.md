# 02_SenaryoluOyun — TÜM Metinler (Numaralı, Sade)

Bu dosya **02_SenaryoluOyun** (build index 1) sahnesinde ekranda görünen tüm anlatı / eğitim
/ modal / popup / final metinlerini içerir. **Tüm renk/biçim tag'leri (`<color>`, `<b>`, `<i>`,
`<size>`, `<span>`) SİLİNMİŞTİR** — sade metin. C# string'lerindeki `\n` satır başına çevrilmiştir.
Her metnin kaynağı (dosya:satır) belirtilmiştir.

## Modal Numaralandırması
"BİLGİLENDİRİCİ ASİSTAN" kutusunda (eğitmen karakterli, `ScriptedModalKopru`) gösterilen modallar
anlatı sırasına göre **1/29 … 29/29** olarak numaralanmıştır. Bu numara, oyun içindeki modalın sağ
üst köşesine de eklenecektir (kod tarafı).

> **Sayım notu:** Toplam **29** asistan modali. **29/29 (Döngü)** koşulludur — güncel akışta
> (FAZ35.130) A6'nın 5 spini atlanıp doğrudan A7'ye geçildiği için tetiklenmeyebilir.
>
> **Asistan kutusu DIŞINDAki** ekranlar (numara almaz, ayrı UI): Sahne girişi karşılama ekranı,
> Bonus Tuzağı Popup, Borç Al Paneli, Düşünce Balonu (4 yalan), A7 Final Cutscene.

## Kaynaklar
| Kaynak | İçerik |
|--------|--------|
| `Assets/StreamingAssets/anlatici.html` | Sol şerit: 7 aşama başlık + Sistemin Arka Yüzü + Oyuncu Düşüncesi + rozet + bakiye bağlamı + sabit UI |
| `Assets/Scripts/Tutorial/ScriptedTutorialGecisEkrani.cs` | Sahne girişi karşılama (ayrı ekran) |
| `Assets/Scripts/AnlaticiSeritKopru.cs` | PreA1 + geçiş + özel akış asistan modalları |
| `Assets/Scripts/OyunYoneticisi.Spin.cs` | Spin ÖNCESİ asistan modalları (A1S7, A2S4) |
| `Assets/_Project/Scripts/Senaryo/Scripted/Editor/ScriptedSenaryoAssetUreteci.cs` | Asset spin (SONRA) modal const'ları (M_AX_SX) — `DonusAkisServisi.cs:441` ile gösterilir |
| `Assets/_Project/Scripts/Senaryo/Scripted/ScriptedBonusOyunUygulayici.cs` | A5 bonus sonucu dinamik modal |
| `Assets/_Project/Scripts/Senaryo/Scripted/ScriptedBonusTuzagiPopup.cs` | A5 cazip bonus tuzağı popup (ayrı UI) |
| `Assets/_Project/Scripts/Senaryo/Scripted/ScriptedYuklemePaneli.cs` | Borç al paneli (ayrı UI) |
| `Assets/_Project/Scripts/Senaryo/Scripted/ScriptedDusunceBalonu.cs` | 4 yalan balonu + paralel asistan modal |
| `Assets/_Project/Scripts/Senaryo/Scripted/ScriptedFinalEkrani.cs` | A7 final cutscene (ayrı UI) |

---

# BÖLÜM 0 — SAHNE GİRİŞİ KARŞILAMA (ayrı ekran, numara almaz)
**Kaynak:** `ScriptedTutorialGecisEkrani.cs:50-54` · Başlık: BİLGİLENDİRİCİ ASİSTAN · Butonlar: YENİDEN OYNA / HADİ GÖRELİM
```
Az önce bir kumar bağımlısının yaşadıklarını gördün. Peki bu manipülasyonlar nasıl tasarlanıyor? Sahne arkasını birlikte görelim. Sistemin perde arkasındaki mühendisliği öğrenmek ister misin?
```

---

# AŞAMA 1 — ISINDIRMA VE UMUT

## Sol Şerit (`anlatici.html:178-182`)
**Başlık:** Isındırma ve Umut
**Sistemin Arka Yüzü:**
```
Yeni gelen oyuncuya kasıtlı olarak yüksek kazanç verilir. Buradaki amaç para kazandırmak değil, oyuncuyu oyuna bağlamaktır. Yazılım, başlangıç kazancını oyuncuda "şanslıyım" algısı oluşturacak şekilde ayarlar; böylece olumlu ilk izlenimle oyuncuyu içeri çeker.
```
**Oyuncu Düşüncesi:**
```
İlk kazançlar oyuncuya "Ben bu işi yapabiliyorum, bu oyunu çözüyorum." hissini verir. Oyuncu, kazancını kendi becerisine bağlar; oysa bu tamamen bir yönlendirmedir. Zihninde "Oynamaya devam edersem kazanmaya devam ederim." beklentisi yerleşir.
```
**Rozetler:** Geri ödeme: %~250 · Tavan: bahsin 5×'i · Şans: yapay yüksek
**Bakiye bağlamı:** Sahte kazanç ileride büyük kayıp için yem olacak.

## Modallar

### [1/29] PreA1 — Hoş geldiniz / oyun tanıtımı (`AnlaticiSeritKopru.cs:871-884`)
```
Hoş geldiniz. Bu uygulama, kumar bağımlılarının en çok oynadığı slot oyununun bir benzeridir. Burada bu oyunların neden kazandırmadığını ve oyuncuların nasıl manipüle edildiğini birlikte göreceğiz.

Önce oyunu tanıyalım:
• Ekranda 6×5'lik meyve makinesi var. SPIN tuşuna basıldığında meyveler döner.
• Aynı meyveden 8 veya daha fazlası bir araya gelirse kazanç verir.
• Bazı turlarda ÇARPAN düşer (×2, ×5, ×100 vs.) ve kazancı katlar.
• Kazanan meyveler patlar, üstten yenileri düşer (TUMBLE); zincir kazançlar olur.
• 4 Bonus Sembolü (yıldız) gelirse BONUS oyun açılır.

Ekrandaki diğer öğeler:
• Sol panel: Oyuncunun hangi aşamada olduğunu, sahne arkasında ne yaşandığını gösterir; birlikte buradan takip edeceğiz.
• Bakiye: Oyuna ayrılan para (oyuncu 50.000 TL ile başlar).
• Bahis: Her spinde harcanacak miktar, + ve − tuşlarıyla değişir.
• KAZANÇ: O spinde kazanılan miktar.

Hadi başlayalım: ilk aşama 'Isındırma ve Umut'.
```

### [2/29] PreA1 — A1_ANLATIM (`AnlaticiSeritKopru.cs:860-862`)
```
İlk aşama: Isındırma ve Umut

İlk kazanç oyuncu için en tehlikeli başlangıçtır. Beyin başlangıçta yaşanan bu olumlu deneyimi güçlü biçimde hatırlar ve kişinin oyun oynama isteğini arttırır.
```

### [3/29] PreA1 — A1_DAVET (`AnlaticiSeritKopru.cs:864-870`)
```
Şimdi deneyelim

Tam 8 spin at ve neler olduğunu görelim. Bakiyenin nasıl yükseldiğine, kazançların sıklığına dikkat edelim.

Sol panelde SİSTEMİN ARKA YÜZÜ ve OYUNCU DÜŞÜNCESİ bölümlerini takip edelim. Sistemin gerçekte ne yaptığını orada göreceğiz.
```

### [4/29] M_A1_S1 — Spin 1 SONRASI (`ScriptedSenaryoAssetUreteci.cs:59-60`)
```
İlk kazanç oyuncu için en tehlikeli başlangıçtır. Oyuncunun beyni bu anı unutmayacak.
```

### [5/29] M_A1_S4 — Spin 4 SONRASI (`ScriptedSenaryoAssetUreteci.cs:62`)
```
Oyuncu ilk kazançları yaşıyor. Oyuncunun beyninde dopamin salgılanıyor. Bu his, saatlerce oyun oynamasının yakıtı olacak.
```

### [6/29] A1 Spin 7 ÖNCESİ (`OyunYoneticisi.Spin.cs:232-235`)
```
Şimdi büyük bir kazanç gelecek. Bu kasıtlı: algoritma oyuncuyu 'şanslıyım' hissine kaptırmak istiyor.

Kazanç sonrası oyuncunun zihninde 'ben kazanırım' duygusu yerleşecek.
```

### [7/29] A1 → A2 Geçiş (`AnlaticiSeritKopru.cs:910-913`)
```
Birinci aşama tamamlandı. Oyuncu şu an artıda, kendini iyi hissediyor.

Sırada 'Kontrol Bende Hissi' aşaması var. Bu aşamada algoritma oyuncuya üst üste kayıplar yaşatacak. Ama yine de bakiye hâlâ pozitif olduğu için oyuncu 'kontrol bende, istediğim zaman çıkarım, bahis değişiklikleriyle kazanırım' gibi düşünceler yaşar.

Bu yanılsamayı birlikte göreceğiz.
```

---

# AŞAMA 2 — KONTROL BENDE HİSSİ

## Sol Şerit (`anlatici.html:183-187`)
**Başlık:** Kontrol Bende Hissi
**Sistemin Arka Yüzü:**
```
Sistem, küçük tutarlarda kaybettirmeye başlar; ancak ara ara kazanç vererek umudu canlı tutar. Oyuncunun düzen arama eğilimini bildiği için bahis değiştirme ve benzeri totem davranışlarına ufak miktarlarda ödeme verir. Bu davranışların sonuca hiçbir etkisi yoktur; tek işlevi oyuncuyu masada tutmaktır.
```
**Oyuncu Düşüncesi:**
```
Oyuncu, "Hala artıdayım istediğim zaman çıkabilirim, ne zaman basacağımı biliyorum, belli bir düzen var, ben bu oyunu çözdüm." diye düşünür. Rastgele olaylarda bir düzen görme yanılgısına kapılır. Gerçekte hiçbir etkisi olmasa da bahsi değiştirmek ve kendince totem yapmak ona kontrol hissi verir.
```
**Rozetler:** Geri ödeme: %~200 · Tavan: bahsin 3.5×'i · Şans: yüksek
**Bakiye bağlamı:** Hâlâ kazançta gibi ama gerçekte kayıp başladı.

## Modallar

### [8/29] M_A2_S2 — Spin 2 SONRASI (`ScriptedSenaryoAssetUreteci.cs:74-76`)
```
DİKKAT: manipülasyon farkındalığı

Oyuncu az önce 1.500 TL bahis koydu. Ekrana "kazanç 750 TL" yazdı ama bakiyesinden 750 TL eksildi; yine de oyuncunun zihninde kazandım hissi oluştu. Bu sistemin bilerek tasarladığı bir durumdur. Burada amaç oyuncuya kaybettiğini hissettirmeden sürekli kazandığı algısını oluşturmaktır. Her spinde yatırılan bahisten daha az ödeme yapılmasına rağmen ekrana büyük puntolarla "kazanç" yazılır. Uzun vadede oyuncu daima kayıptadır. Algoritma bunu kasıtlı olarak tasarlar: bakiyeyi sürekli artıyormuş gibi göstererek oyuncuyu oyunda tutmak temel amaçtır.
```

### [9/29] A2 Spin 4 ÖNCESİ (`OyunYoneticisi.Spin.cs:242-245`)
```
Şu an oyuncu bahisini değiştirecek (yükseltecek). Bu bahisin ardından algoritma kasıtlı olarak kazanç yaşatacak.

Amaç: oyuncuya 'doğru zamanda doğru bahis' duygusu vermek. Böylece oyuncu kontrolün kendinde olduğuna inanır.
```

### [10/29] M_A2_S4 — Spin 4 SONRASI (`ScriptedSenaryoAssetUreteci.cs:79`)
```
Oyuncu oyunu yönettiğini düşünürken, oyun onu adım adım içine çekiyor.
```

### [11/29] M_A2_S6 — Spin 6 SONRASI (`ScriptedSenaryoAssetUreteci.cs:81`)
```
Ekrana hem üzüm hem de elmadan 7'şer adet düştü. Oysa 8 tane olsaydı ödeme yapılacaktı. İkisi de kıl payı kaçtı. Oyuncu şu an "çok yakındım, bir daha denesem kesin olur" hissini yaşıyor. Bu his bir manipülasyondur. Algoritma bu durumu kasıtlı olarak yaratır. Kontrol yanılsaması bu şekilde pekiştirilir.
```

### [12/29] A2 → A3 Geçiş (`AnlaticiSeritKopru.cs:942-943`)
> Dinamik: {a2SonuBakiye}, {a1SonuBakiye}, {fark} runtime bakiye değerleriyle dolar.
```
İkinci aşama tamamlandı. Oyuncunun bu aşama sonundaki bakiyesi {a2SonuBakiye} TL, birinci aşamadaki bakiyesine {a1SonuBakiye} TL göre {fark} TL azaldı. Oyuncu aslında hâlâ kârda olmasına rağmen kârdan kaybettiği {fark} TL'yi geri kazanabilmek için bir sonraki aşamaya ismini veren "kaybettiklerimi geri kazanabilirim" düşüncesine bürünür. Ancak bu düşünce oyuncunun daha fazla kaybetmesine sebep olur. Oyuncu artık kazanç peşinde değil, "kaybettiklerimi kurtarsam yeter" gibi bir düşünceye girebilir. Bu kayıp kovalama denilen psikolojik bir tuzaktır; bir kez bu döngüye girilirse çıkmak çok zordur.
```

---

# AŞAMA 3 — KAYBETTİKLERİMİ GERİ KAZANABİLİRİM

## Sol Şerit (`anlatici.html:188-192`)
**Başlık:** Kaybettiklerimi Geri Kazanabilirim
**Sistemin Arka Yüzü:**
```
İlk ciddi kayıp dalgası başlamıştır. Sistem oyuncuya verdiklerini geri almaya başlayacaktır. Oyuncu "bahis miktarını arttırırsam daha hızlı telafi edebilirim" yanılgısıyla bahsi arttırır; ancak bu sistemin tam olarak istediği davranıştır çünkü bahis miktarı arttıkça kayıp da artacaktır.
```
**Oyuncu Düşüncesi:**
```
Oyuncu, "Sadece kaybettiğimi geri alayım, sonra çıkarım." der. Bu, kayıp kovalamadır: kaybın acısı, kazancın umudundan çok daha güçlü olduğu için oyuncu durmak yerine riski büyütür.
```
**Rozetler:** Geri ödeme: %~75 · Tavan: bahsin 1×'i · Şans: dengeli
**Bakiye bağlamı:** Telafi etmek için bahis 2 katına çıkacak — kayıp da öyle.

## Modallar

### [13/29] M_A3_S3 — Spin 3 SONRASI (`ScriptedSenaryoAssetUreteci.cs:85`)
```
İlk ciddi kayıplar yaşanmaktadır. Amaç para kazanmaktan ziyade kayıpları telafi etmeye dönüşür.
```

### [14/29] M_A3_S6 — Spin 6 SONRASI (`ScriptedSenaryoAssetUreteci.cs:89`)
```
Oyuncu kayıpları geri kazanmak için daha fazla risk alır. Mantıklı düşünme yetisini kaybeder.

Şimdi oyuncu "yüksek bahis daha hızlı kurtarır" yanılgısıyla bahis miktarını 2500 TL'ye yükseltecek. Bu da algoritmanın istediği davranış biçimidir.
```

### [15/29] M_A3_S7 — Spin 7 SONRASI (`ScriptedSenaryoAssetUreteci.cs:91`)
```
Bir tur daha = bir kayıp daha.
```

### [16/29] A3 → A4 Geçiş (`AnlaticiSeritKopru.cs:960-962`)
```
Üçüncü aşamayı gördük: kayıp kovalama tuzağı. Oyuncu bahsi yükselterek kurtulmaya çalışmıştır, fakat daha çok kaybetmiştir.

Sırada "şansım döndü" aşaması var. Bu aşamada algoritma oyuncuyu pes etme eşiğine getirecektir ve üst üste sert kayıplar yaşatacaktır. Oyuncu tam pes etmek üzereyken büyük bir kazanç sağlatacaktır. Bu büyük kazanç, geçmişteki tüm kayıpları unutturacak kuvvetli bir manipülasyon hamlesidir. Amaç oynamaktan vazgeçmek üzere olan oyuncuyu tekrar oyuna bağlamaktır.
```

---

# AŞAMA 4 — ŞANSIN DÖNDÜ

## Sol Şerit (`anlatici.html:193-197`)
**Başlık:** Şansın Döndü
**Sistemin Arka Yüzü:**
```
Kayıplar derinleşmeye başlar ve sistem, oyuncuya "neredeyse kazanıyordum anları" yaşatır. Önce kazandıracakmış gibi yaparak oyuncuyu heyecanlandırır ve ardından büyük bir kazanç verir. Böylece oyuncu geçmiş tüm kayıplarını unutur ve şansının döndüğüne inanır.
```
**Oyuncu Düşüncesi:**
```
"Neredeyse kazanıyordum." anları beyni kandırır ve oyuncu kendini gerçekten kazanmaya yakın hisseder. Büyük bir kazanç gelince "Oldu bu iş, şansım döndü." algısı pekişir. Bunun ardından gelen kayıpları oyuncu, o tek kazanca sığınarak görmezden gelir.
```
**Rozetler:** Geri ödeme: %~40 · Tavan: bahsin 0.6×'i · Şans: düşük
**Bakiye bağlamı:** Kayıp dakikada hızlandı. Oyuncu fark etmiyor.

## Modallar

### [17/29] A4 Spin 1 — Yıldız (3 bonus sembolü) (`AnlaticiSeritKopru.cs:1099-1100`)
```
Spin çeviren oyuncuların hedefi 4 adet bonus sembolünü aynı anda ekrana düşürerek bonus oyuna girmektir. Sistem burada 3 adet bonus (yıldız) sembolü düşürerek, oyuncunun "neredeyse kazanıyordum" şeklinde düşünmesine sebep olur. Bu durum oyuncuyu kazandırmadan, kazanmış gibi beyninde dopamin salgılanmasına sebep olur. Bu da oyuncunun masada kalma davranışına devam etmesine sebep olur.
```

### [18/29] M_A4_S2 — Spin 2 SONRASI (`ScriptedSenaryoAssetUreteci.cs:100`)
```
Üst üste kayıplar oyuncuyu yıpratmaktadır. Algoritma birkaç spin sonra büyük bir kazanç hazırlamaktadır. Bu, oyuncuda şansım döndü algısı yaratacak. Fakat sistem, önce kurbanı pes etme eşiğine kadar getirecektir.
```

### [19/29] M_A4_S4 — Spin 4 SONRASI (`ScriptedSenaryoAssetUreteci.cs:101`)
```
Oyuncu çok ciddi kayıplar yaşadığı için oyundan çıkmak üzeredir. Tam bu noktada algoritma büyük bir kazanç vererek oyuncuyu şansının döndüğüne inandıracaktır.
```

### [20/29] A4 Spin 5 — ×100 Çarpan, Modal 1 (`AnlaticiSeritKopru.cs:1180-1181`)
```
Ekrana 100x çarpan düştü! Oyuncu pes etmek üzereyken sistem tarafından kurbanın oynamayı bırakmasını engellemek ve geçmiş kayıplarını unutturmak maksadıyla kurbana büyük kazanç verilir. Bu bir rastlantı değildir. Sistem oyuncuyu tam bu duygusal anında yakalar. Oyuncu şansının döndüğünü sanarak oyuna devam eder.
```

### [21/29] A4 Spin 5 — Çekim Şartı Tuzağı, Modal 2 (`AnlaticiSeritKopru.cs:1185-1190`)
```
İşte bu noktada gerçek hayatta oyuncunun aklına şu düşünce gelir: "Şu an kazançtayım, parayı çekip çıkayım." Bu mantıklı bir düşüncedir. Ancak kumar siteleri çoğu zaman bunun kolayca gerçekleşmesine izin vermez.

Çekim şartı tuzağı: Site, oyuncunun kazandığı parayı çekebilmesi için bazı şartlar koyar. Bu şart genellikle iki şekilde uygulanır:

Bahis çevrim şartı: Oyuncu, kazandığı parayı çekebilmek için bu tutarın belirli bir katı kadar bahis yapmak zorunda bırakılır. Bu şart tamamlanmadan para çekimine izin verilmez.

Spin sayısı şartı: Oyuncunun belirli bir spin sayısına ulaşması istenir; örneğin oyuncudan 1000 spin atması beklenebilir. Bu sayıya ulaşmadan çekim yapmasına izin verilmez.

Sonuç değişmez: Oyuncu bu şartları tamamlamaya çalışırken sistem kazandığı parayı zamanla geri alır, hatta oyuncu çoğu zaman anaparasını da kaybeder. Çekim şartını sağlayıp parayı çekmeyi hayal eden oyuncu, şartlar sağlanana kadar zaten masada tüketilmiş olur.
```

### [22/29] A4 → A5 Geçiş (`AnlaticiSeritKopru.cs:979-980`)
```
Büyük kazanç yaşandı. Oyuncu şu anda "şansım döndü, artık daha fazla kazanabilirim" düşüncesine kapıldı. Bu düşünce, sistemin bir sonraki aşamada oyuncuyu oyunda tutmak için kullandığı en güçlü etkendir. Oyuncu bundan sonraki tüm oyun deneyiminde hep bu anın peşinden koşacaktır. Sıradaki aşamada şansının döndüğünü sanan oyuncuya bonus oyun tuzağı kurularak "sonunu düşünen kahraman olamaz" mantığı ile hareket etmesi amaçlanmaktadır. Bu aşamada sistem oyuncuya yüksek vaat içeren bonus oyun teklifi sunar. Oyuncuya tüm bakiyesini yatırması karşılığında çok daha büyük kazançlar elde edebileceği vaat edilir. Oyuncu bu teklifi kabul edip parasını yatırırsa, çok büyük bir kayıp yaşayacaktır.
```

---

# AŞAMA 5 — SONUNU DÜŞÜNEN KAHRAMAN OLAMAZ

## Sol Şerit (`anlatici.html:198-202`)
**Başlık:** Sonunu Düşünen Kahraman Olamaz
**Sistemin Arka Yüzü:**
```
Sistem, oyuncunun artık mantığını bir kenara bıraktığını bilir. Oyuncuda bilinçli karar kalmamıştır; artık yalnızca dürtüler devreye girer. Zafer sarhoşluğu yaşayan oyuncuya sistem şanslı saat, altın zaman gibi tekliflerle tuzak kurarak büyük miktarda bakiyesini sömürmeyi amaçlar.
```
**Oyuncu Düşüncesi:**
```
Oyuncu, bakiyesine göre "Yarısı gitti zaten, gerisini de koyayım" ya da "Şu an çok şanslıyım, kesin büyük vuracağım hepsini yatırayım" diye düşünerek çok büyük riskler alır. Geçmişteki kayıp veya çok şanslı olduğu inancı, geleceğe dair sağlıklı kararı bozar. Geri çekilmek, kaybı kesinleştirmek gibi hissettirdiği için oyuncu duramaz. Şanslı anında olduğunu sanan oyuncu büyük oynamazsa büyük kazancı kaçıracağını zanneder.
```
**Rozetler:** Geri ödeme: %~20 · Tavan: bahsin 0.4×'i · Şans: minimal
**Bakiye bağlamı:** Toplam kayıp birikiyor. Oyuncu hâlâ duramıyor.

## Modallar

### [23/29] M_A5_S1 — Spin 1 SONRASI (`ScriptedSenaryoAssetUreteci.cs:105`)
```
Şansının döndüğünü düşünen oyuncu, bakiyesinin arttığını görünce bahis oranını daha da artırarak daha çok kazanmayı amaçlar. Bahis miktarının artması oyuncuda adrenalin salgılanmasına neden olur.
```

### [24/29] M_A5_S3 — Spin 3 SONRASI (`ScriptedSenaryoAssetUreteci.cs:107`)
```
Oyunda 500x çarpanı düştü ancak ekranda aynı sembolden 8 adet olmadığı için hiç ödeme yapılmadı. Bu, sistem tarafından oyuncuyu bir sonraki tuzağa çekmek için kullanılan yemdir. Oyuncu 500x çarpanı görünce şansının döndüğüne emin olur ve sıradaki tuzağa düşer.
```

### (ayrı UI — numara almaz) A5 Spin 4 — Bonus Tuzağı Popup (`ScriptedBonusTuzagiPopup.cs`)
Başlık (`:269`): ŞANSLI ANINDASIN!
Açıklama (`:285-289`):
```
Tüm bakiyeni bonus oyuna yatır,
10.000 KATI KAZANMA
şansını yakala!

Bu fırsat bir daha karşına çıkmayabilir!
```
Buton (dinamik, `:106`): BONUS AL — TÜM BAKİYE ({bakiye} TL)

### [25/29] A5 Bonus Sonucu — Dinamik Modal (`ScriptedBonusOyunUygulayici.cs:185-189`)
> Dinamik: {BonusYatirim}, {BonusKazanc}, {yuzde} runtime'da hesaplanır.
```
Oyuncu tüm bakiyesi olan {BonusYatirim} TL'yi bonus oyuna yatırdı. Geri aldığı {BonusKazanc} TL; yatırdığının %{yuzde}'i.

Bu sömürünün adı 'değişken oranlı pekiştireç': beyin bu kayba rağmen 'belki bir dahaki sefere' diyerek devam etmeye programlanır.
```

### [26/29] Başa Arayış (`AnlaticiSeritKopru.cs:1252-1255`)
```
Oyuncu artık paranın bittiğini fark etti.

Şimdi başka yerden para bulma arayışında. Yalan söylemeye başlıyor: yakınlarına, akrabalarına, arkadaşlarına...

Bu, kumar bağımlılığının yıkıcı evresidir. Bir sonraki ekran o anı temsil ediyor.
```

### Düşünce Balonu Sahnesi (`ScriptedDusunceBalonu.cs`) — ayrı UI
Üst başlık (`:395`): BAŞKA YERDEN PARA BULMA ARAYIŞI

#### [27/29] Düşünce Balonu — Paralel Asistan Modal (`ScriptedDusunceBalonu.cs:131-135`)
```
Bu aşamada oyuncu çevresindeki kişilere yalan söyleyerek veya bankalardan kredi çekerek para bulmaya çalışır.

Burada amaç eski kayıpların telafisidir. Ancak bu, kumar bağımlılığının en yıkıcı evresidir: borç katlanarak büyür, ilişkiler bozulur, hayatlar mahvolur.
```

#### 4 Yalan Balonu (`ScriptedDusunceBalonu.cs:143-146`) — balon, numara almaz
1. Çocuğum hasta, acil para lazım...
2. Bir 50 bin kredi çekersem hepsini telafi ederim...
3. Kardeşim kaza yaptığı için çok acil para lazım.
4. Bu sefer kazanırsam hepsini öderim, söz veriyorum...

(Atla butonu: ATLA ▶)

### Borç Al Paneli (`ScriptedYuklemePaneli.cs`) — ayrı UI, numara almaz
Başlık (`:212`): Borç alarak devam etmek istiyor musun?
Açıklama (`:230`):
```
Aileden, kredi kartından veya iş arkadaşından borç alarak oyuna devam etmek istiyor musun? Borçla kumar oynamak bağımlılığın klasik göstergelerinden biridir.
```
Buton (`:257`): BORÇ AL — 50.000 TL

---

# AŞAMA 6 — BAŞKA BİR YERDEN PARA BULMALIYIM

## Sol Şerit (`anlatici.html:203-207`)
**Başlık:** Başka Bir Yerden Para Bulmalıyım
**Sistemin Arka Yüzü:**
```
Bakiye biter ve sistem, oyuncuyu kredi kartıyla bakiye satın almaya yönlendirir. Zarar artık oyunun dışına taşar: borçlanma, gizlilik, yalan ve ilişkilerde çatlaklar başlar. Manipülasyon, oyuncunun gerçek hayattaki kaynaklarını da sisteme aktarmasını sağlar.
```
**Oyuncu Düşüncesi:**
```
Oyuncu, "Bir kredi çekersem son bir hamleyle hem borcu kapatır hem de yeniden başlarım." diye düşünür. Bu, çaresizlik tepkisidir: beyin, uzun vadeli sonucu hesaplayamaz hale gelir ve tek çıkış olarak yine oyunu görür.
```
**Rozetler:** Geri ödeme: %~15 · Tavan: bahsin 0.3×'i · Şans: tükenmiş
**Bakiye bağlamı:** Kart limitine bonus eklendi. Borç başladı.
**Spin ilerleme statik uyarısı (`anlatici.html:264`):** Borç aldın, daha büyük kayıp geliyor…

## Modallar

### [28/29] Borç Sonrası (`AnlaticiSeritKopru.cs:1027-1029`)
```
İşte oyuncu borç aldı, bakiyesi yenilendi. Şimdi tekrar oynamaya devam edecek.

Kumar sitelerinde yeniden bakiye yükleyenlere bilinçli olarak ilk başlarda yine kazandırılır — bu 'Isındırma ve Umut' aşamasına benzer.

Bu sayede oyuncu tekrar döngüye girer: 'şansım yine açıldı, kayıplarımı telafi ederim' düşünür. Ama er ya da geç sistem kazanır, oyuncu kaybeder.
```

### [29/29] Döngü (KOŞULLU) (`AnlaticiSeritKopru.cs:1311-1315`)
> FAZ35.130: Güncel akışta A6'nın 5 spini atlanıp doğrudan A7'ye geçildiği için bu modal tetiklenmeyebilir (eski/alternatif akış).
```
Bakın, para tamamen bitti.

5 spin'de 50.000 TL borç eridi. Bu, gerçek hayatta 'hızlı kurtulma' bahanesiyle yatırılan paraların kaderidir.

Şimdi oyuncu A1'e geri dönmek isteyecek. 'Belki bu sefer şanslıyım' diye düşünüyor. 'Bir kerelik daha denersem...' diyerek kendini kandırıyor.

İşte bağımlılığın özü budur: KAYIP → BORÇ → KAYIP → BORÇ. Sonsuz döngü.

Sonraki ekranda yaşanan toplam kayıp gösteriliyor.
```

---

# AŞAMA 7 — TÜKENİŞ

## Sol Şerit (`anlatici.html:208-212`)
**Başlık:** Tükeniş
**Sistemin Arka Yüzü:**
```
Sistem son hamlesini yapar; artık oyuncu ne yaparsa yapsın kazanç vermez. Para, mevki ve ilişkiler tükenmiştir. Oyuncu bağımlı hale gelmiştir ve sistem çoktan kazanmıştır; oyuncu ise "dur" diyemez.
```
**Oyuncu Düşüncesi:**
```
Oyuncu, "Nasıl bu hale geldim? Eve ne diyeceğim? Ama şimdi dursam kayıplar olduğu gibi kalır... Bir spin daha." diye düşünür. Bu, tükeniş aşamasıdır ve bağımlılık literatüründe son dönemin klinik tablosu olarak tanımlanır.
```
**Rozetler:** Geri ödeme: %~5 · Tavan: bahsin 0.1×'i · Şans: yok
**Bakiye bağlamı:** Bu, ortalama bir maaşın katlarına ulaştı.

## A7 Final Cutscene (`ScriptedFinalEkrani.cs`) — ayrı UI, numara almaz
Başlık (`:202`): SENARYO TAMAMLANDI
İstatistik (dinamik, `:128-131`):
```
Yatırdığın toplam: {toplamYatirim}
Geri aldığın: {sonBakiye}
Net kayıp: {toplamKayip}
Toplam spin: {toplamSpin}
```
Aile yazısı (`:240-242`):
```
Bu rakam ortalama bir aile için 2,5 aylık geçim demek.
Gerçek hayatta oyuncu burada durmaz; bir sonraki maaş, bir sonraki kredi, bir sonraki dönüş umuduyla devam eder.
```
Mesaj + Yeşilay (`:265-267`, hat `:22`):
```
Unutulmamalıdır ki sanal kumar bağımlılığı çözümsüz değildir ve her zaman yeni bir başlangıç yapmak mümkündür. Yaşanan zorluklar ne kadar büyük görünürse görünsün, umut her zaman vardır ve doğru destekle bu süreç aşılabilir. Bu noktada ailenize, amirlerinize ve güvendiğiniz kişilere durumu açıkça ifade etmek, çözüm yolunda atılacak cesur bir adımdır. Yardım istemek bir zayıflık değil, aksine güçlü bir farkındalık ve değişim isteğinin göstergesidir.

Yeşilay Yardım Hattı: 115
```
Buton (`:294`): TAMAM

---

# SOL ŞERİT SABİT UI METİNLERİ (`anlatici.html`)
| Eleman | Metin | Satır |
|--------|-------|-------|
| Aşama numarası | AŞAMA 1 / 7 (render'da {N+2}. AŞAMA) | :135 / :236 |
| Bölüm etiketi | SİSTEMİN ARKA YÜZÜ | :141 |
| Bölüm etiketi | OYUNCU DÜŞÜNCESİ | :145 |
| Bölüm etiketi | SPİN İLERLEMESİ | :151 |
| Spin sayaç | 0 / 8 | :152 |
| Spin info (başlangıç) | 8 spin sonra aşama 2 başlayacak | :155 |
| Spin info (genel) | {kalan} spin sonra aşama {N+2} başlayacak | :267 |
| Spin info (A7) | Bakiye tükenince simülasyon biter / {kalan} spin sonra simülasyon biter | :270 |
| Bakiye prefix | {toplamSpin} spin · net durum: | :159 / :285 |
| Bakiye net | 0 TL | :160 |

## Tükeniş Overlay (anlatici.html içi, `:163-169`)
- Başlık: SİMÜLASYON TAMAMLANDI
- Spin/dakika (dinamik): — spin · yaklaşık — dakika
- Rakam (dinamik): −34.000 TL
- Açıklama:
```
Bu rakam ortalama bir aile için 2.5 aylık geçim demek. Gerçek hayatta oyuncu burada durmaz — bir sonraki maaş, bir sonraki kredi, bir sonraki dönüş umudu için devam eder.
```
- Buton: Yeniden Başlat

---

# SAYIM ÖZETİ
| Kategori | Adet |
|---|---|
| Bilgilendirici Asistan modali (numaralı) | **29** (28 kesin + 1 koşullu "Döngü") |
| Sistemin Arka Yüzü (sol şerit) | 7 |
| Oyuncu Düşüncesi (sol şerit) | 7 |
| Ayrı ekran/popup/panel (numarasız) | 5 (sahne girişi, bonus tuzağı popup, borç paneli, düşünce balonu/4 yalan, A7 final) |

## NOTLAR
- **Asistan numarası** = `ScriptedModalKopru` kutusunda gösterilen modallar, anlatı sırasına göre. Oyun
  içinde bu numara modalın sağ üst köşesine eklenecek (kod tarafı, ayrı iş).
- **Spin SONRA modalları** (M_AX_SX) asset'ten gelir, `DonusAkisServisi.cs:441` ile gösterilir.
  **Spin ÖNCE modalları** (#6, #9) `OyunYoneticisi.Spin.cs`'tedir.
- **Sahne girişi karşılama** ayrı bir ekran (`ScriptedTutorialGecisEkrani`); BİLGİLENDİRİCİ ASİSTAN
  başlığı taşır ama asistan kutusu değil → numara almadı.
- **`OyunYoneticisi.cs:682` KARSILAMA_METNI = 03 admin sahnesi** karşılaması, 02'ye ait değil (hariç).
- **Dinamik değerler** ({a2SonuBakiye}, {fark}, {BonusYatirim}, {yuzde}, {toplamKayip} vb.) runtime'da dolar.
- **Renk/biçim tag'leri** kullanıcı isteğiyle bu dosyadan tamamen silindi; orijinal renkli sürüm için kod kaynaklarına bakılmalı.
