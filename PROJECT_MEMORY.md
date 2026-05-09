\# PROJECT\_MEMORY — Siirt Bonanza (Unity)



\## 1) Projenin amacı (1 paragraf)

Bu proje, Sweet Bonanza benzeri “slot + tumble” mekaniğini kullanarak kumar bağımlılığı farkındalığı oluşturan senaryolu bir eğitim oyunudur. UI dili Türkçe olacaktır. Oyunda oyuncu davranışına göre senaryo durumları (Isındırma/Umut, Kontrol Bende, Az Daha/Kayıp Kovalama, Bakiye Tükeniş \& Yükleme, Bonus Zirve, Gerçek Kayıp, Finale) arasında geçiş yapılır.



\## 2) Oyun Akışı (en üst seviye)

\- GirisScene: oyuncu senaryolu oyun / admin oyun seçer

\- SenaryoluOyunScene: asıl oyun + senaryo mantığı + tumble

\- AdminOyunScene: test/ayarlama paneli

\- LogScene: loglar ve istatistikler



\## 3) Ana Scriptler ve sorumlulukları

\- OyunYoneticisi.cs: bahis, spin, bakiye, ödül, event yayını (BetChanged, SpinSettled vb.)

\- SenaryoYoneticisi.cs: durum geçişleri, kapılar/sayaçlar, manipülasyon sahneleri

\## 4) Log ve raporlama altyapısı

\- Log sahnesi (04_LogScane): Oturum özeti (profil, spin, yatırım/çekim, net, bonus sayıları) ve senaryo aşamalarına göre kronolojik olay listesi. Amaç: eğitim ve analiz için davranışı belgelemek.

\- LogYoneticisi: Genel özet + senaryo logunu iki panelli veya tek scroll’da gösterir; SenaryoYoneticisi.GetOturumLoguStatik ile veri alır.

\- SenaryoOlayKaydi: Tüm olay tipleri Türkçe sabit (OturumBasladi, AsamaGecisi, BonusBitti, BakiyeYuklemeYapildi, Ihlal_* vb.); ileride log sahnesinde anlamlı olanlar filtrelenerek gösterilecek.







\## 5) Kurallarım (AI için)

\- Plan olmadan kod yazma. Önce plan + etkilenecek dosyalar listesi çıkar. İlgili olmayan yeri değiştirme. Sadece sana söyleneni yap.

\- Her değişiklikte sadece gerekli dosyalara dokun (minimum diff).

\- İsimlendirme ve UI metinleri Türkçe.

\- Unity UI: Canvas sorting order, panel order, raycastTarget, GraphicRaycaster, EventSystem kontrolleri kritik.

\- Compile hatası bırakma: değişiklikten sonra “en azından derleme mantığı” kontrol edilmeli.

\- Bir  değişiklik yapar yapmaz hemen yedek al. Son yaptığımız şey bozuk olurssa dönmesi kolay olsun. Her yaptığın işlemden sonra aldığın yedeğin geri dönüş kodunu ver.



## 6) Yapılacaklar / Hatırlatma (kullanıcı isteği)

- **Otomatik spin:** Eklendi. OyunYoneticisi: otomatikSpinDropdown (Tek/10/50/100), otomatikSpinDurdurButon, otomatikSpinKalanText. Çevir’e basınca seçilen adet > 1 ise döngü başlar; Durdur ile _otomatikSpinKalan = 0 yapılır.

---

## 7) Zorla çarpan + animasyon – açık sorunlar (dönüşte devam)

**Yapılan düzeltme (bu oturumda):**  
`SimuleEtVeKaydetImpl` içinde zorla çarpan varken `limit = int.MaxValue` atanıyordu; fakat hemen altında `if (limit == int.MaxValue || nihaiOdeme > limit) continue;` vardı. Bu yüzden limit yokken bile **her spin “continue” ile atlanıyor**, spin hiç kabul edilmiyordu. Bu satır şu şekilde düzeltildi: **`if (limit != int.MaxValue && nihaiOdeme > limit) continue;`** – yani sadece gerçek bir limit varken ve nihai ödeme limiti aşıyorsa atla; zorla çarpan (limit = MaxValue) durumunda artık bu yüzden spin reddedilmemeli.

**Dönüşte kontrol edilecekler:**

1. **Animasyon hâlâ yoksa**  
   - `SimulasyonKaydiniOynatImpl` gerçekten çağrılıyor mu, `kayit != null` mı (yukarıdaki bug fix ile artık kayıt dönmeli).  
   - `CacheCellPositionsThenDisableLayout` sonrası `cellPos` / `_animasyonServisi.SetCellPos` güncel mi.  
   - Layout başka yerde tekrar `enabled = true` yapılıyor olabilir mi (ör. tumble sonrası).  
   - `AnimateGridDropIn` içinde `_runCoroutine` ile başlatılan alt korutinler çalışıyor mu; `_hucreler` / `_cellPos` null mı.

2. **Tumble hâlâ olmuyorsa**  
   - `carpanAktifToggle` Inspector’da atanmış mı, sahnede isimle bulunuyor mu; `carpanToggleSecili` gerçekten true mu.  
   - `GrideZorlaEnAzBirCluster` sonrası `_tumbleServisi.FindClustersToRemove(minClusterSize)` aynı grid referansını görüyor mu; `_tumbleServisi.SetGrid(grid)` her zaman güncel grid ile çağrılıyor mu.  
   - `IzgaraServisi.FillRandomAll(grid)` ile `OyunYoneticisi.grid` aynı referans mı; simülasyon sırasında hangi grid’in doldurulduğunu takip et.

3. **1 TL ödeme / havuz**  
   - DonusAkisServisi’nde zorla çarpan için `AddWinnings(teorikToplam)` ve bonus’ta `BonusPendingOdemeTL` artırılmıyor; akışta başka yerde havuz limiti veya `PayFromHavuz` çağrısı kalmış mı kontrol et.

4. **Kazanç formatı "300 x 2 = 600 TL"**  
   - Zorla çarpan sonrası `SonSpinKazancHamGoster`, `SonSpinCarpanGoster`, `SonSpinKazancToplamGoster` set ediliyor; UI güncellemesi bu değerleri okuyana kadar başka bir kod bu alanları sıfırlıyor veya ezebilir – sıra ve `UI_Guncelle()` çağrı yerlerini kontrol et.

**Özet:** Önce `limit == int.MaxValue` continue bug’ı düzeltildi. Dönüşte yukarıdaki maddelere göre test edip gerekirse debug log / adım adım kontrol ile devam et.



---

## 8) Yedek — toplantı öncesi (7 Nisan 2026)

- **Klasör:** `Yedekler/TumProje_20260407_ToplantiOncesi` (Assets, Packages, ProjectSettings, Docs, PROJECT_MEMORY, README; Library/Temp hariç).
- **Not:** Bu yedek, zorla çarpan sonrası final bomba sonrası tekrar düşme düzeltmesi uygulanmadan önce alındı; düzeltme ana OyunYoneticisi dalında.
