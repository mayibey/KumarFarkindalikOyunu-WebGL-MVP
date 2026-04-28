# Tüm Script Dosyaları — Ne İş Yapar?

| Dosya | Tür | Ne iş yapar |
|-------|-----|-------------|
| **Scripts/ (ana katman)** |||
| **OyunYoneticisi.cs** | MonoBehaviour | Ana orkestratör. Tüm servisleri kurar, spin/bonus/tumble akışını yönetir, sahnedeki UI/ayar referanslarını toplar. IDonusAkisBaglami, IIzgaraBaslatmaBaglami vb. arayüzleri uygular. |
| **GameManager.cs** | MonoBehaviour | Global singleton. Oyuncu listesi (Profiles), aktif oyuncu, bahis, sahne yükleme. RecordEconomyAction ile ekonomi kaydı; SaveSystem ile profil kaydetme. |
| **SaveSystem.cs** | Static sınıf | Profilleri JSON (profiles.json) ile diske yazar/okur. LoadProfiles, SaveProfiles. |
| **PlayerProfile.cs** | Veri sınıfı | Tek oyuncu verisi: bakiye, toplam spin/kazanç/kayıp, statsEntries, logs. GameLogEntry nested sınıfı da burada. |
| **StatsEntry.cs** | Veri sınıfı | Tek istatistik kaydı (onceki/sonraki bakiye, işlem, spin/bonus sayıları, kategori, açıklama). |
| **GirisUI.cs** | MonoBehaviour | Giriş sahnesi: kullanıcı seç/oluştur, Senaryolu Oyun / Admin Oyun butonları, sahne geçişi. GameManager ile konuşur. |
| **AdminPanel.cs** | MonoBehaviour | Admin şifre, zorluk/scatter/çarpan slider’ları, profil listesi, kasa/ödül havuzu ayarları. OyunYoneticisi ve GameManager ile bağlanır. |
| **LogYoneticisi.cs** | MonoBehaviour | Log sahnesi (04_LogScane): oyuncu istatistikleri (spin, kazanç, kayıp, bonus), log listesi, geri dön / replay butonları. |
| **SenaryoYoneticisi.cs** | MonoBehaviour | Senaryo modu: aşamalar (Balayı, Alışkanlık, Kayıp Kovalama, Matematik, Finale), takip (toplam spin, kazanç, bakiye). SenaryoServisi ile konuşur. |
| **BonusSatinAlUI.cs** | MonoBehaviour | Bonus satın al onay paneli UI: maliyet göster, Evet/Hayır butonları, Goster/Gizle. OyunYoneticisi tarafından bulunup kullanılır. |
| **UIReferanslari.cs** | MonoBehaviour | UI referans konteyneri: butonlar, TMP metinler, paneller, input alanları. SahneBaglamaServisi bu referanslarla OyunYoneticisi’ni doldurur. |
| **SpinIconRotate.cs** | MonoBehaviour | Dönüş ikonu döndürme (spin sırasında). SetRotate(bool). IDonusAkisBaglami üzerinden tetiklenir (LEGACY). |
| **HosgeldinizText.cs** | MonoBehaviour | Hoş geldin metni güncelleme (sahnede bileşen). |
| **CarpanAyarlari.cs** | MonoBehaviour | Çarpan ayarları (Inspector): olasılık, max adet, “Force x2/x5/…” butonları. OyunYoneticisi’ne referans verir. |
| **TumbleAyarlari.cs** | MonoBehaviour | Tumble ayarları: PayTable, ScatterIndex, sembol sayısı. OyunYoneticisi PayTable’ı buradan alır. |
| **BonusAyarlari.cs** | MonoBehaviour | Bonus ayarları (hak, maliyet vb.). Sahneden FindFirstObjectByType ile okunur. |
| **OdulHavuzuAyarlari.cs** | MonoBehaviour | Ödül havuzu ve kasa ayarları. SyncFromAyarClassesIfPresent ile OY’a kopyalanır. |
| **EkonomiAyarlari.cs** | MonoBehaviour | Bakiye ve bahis ayarları. Sahneden okunup OyunYoneticisi alanlarına yazılır. |
| **SesAyarlari.cs** | MonoBehaviour | Ses/volume ayarları. Sadece sahnede bileşen olarak kullanılabilir. |
| **Scripts/Services/ (servisler)** |||
| **DonusAkisServisi.cs** | Servis | Normal spin ve bonus döngüsü akışı. NormalSpinAkisi, BonusDongusu coroutine’leri; IDonusAkisBaglami ile state/servis erişimi. |
| **EkonomiServisi.cs** | Servis | Bakiye, bahis state; yükleme/yatır/çek/ödeme; GameManager ve PlayerPrefs senkronu. |
| **IzgaraServisi.cs** | Servis | Izgara (grid), sembol listesi, XYToIndex, ScatterSay, çarpan hücre metinleri, RenderAllSprites. |
| **TumbleServisi.cs** | Servis | Tumble (patlatma, düşme, refill) delegasyonu: TumbleLoop, CollapseRefillAndAnimate, FindClustersToRemove, ödeme hesabı. |
| **OdemeServisi.cs** | Servis | Kasa/ödül havuzu: ParaGirisi_BolVeEkle, OdemeYap_OdulHavuzundan, GetHavuzTL, ödenebilir limit. |
| **SenaryoServisi.cs** | Servis | Zorluk, scatter şansı, çarpan ayarları, bonus bütçe/cap; wrapper/delegasyon. |
| **CarpanServisi.cs** | Servis | Çarpan state ve hesap: spin çarpan çarpımı, pending listesi, RollCarpanDegeri, GetBonusRemainingPayableTL. |
| **DonusServisi.cs** | Servis | Tek bir “dönüş” tetiklemesi; grid doldurma, çarpan, TumbleLoop başlatma delegasyonu. |
| **IzgaraBaslatmaServisi.cs** | Servis | Izgara ilk kurulum: grid alloc, hücreler, pozisyonlar, CarpanOverlay ve AnimasyonServisi bağlantısı. InitRoutine. |
| **CarpanYerlestirmeServisi.cs** | Servis | Çarpanları grid’e yerleştirme mantığı (hücre seçimi, scatter atlama). IIzgaraBaslatmaBaglami ile hücreleri alır. |
| **CokmeAkisServisi.cs** | Servis | Çökme (collapse) sonrası akış: refill, çarpan yerleştirme, animasyon, ödeme. TumbleServisi ile koordinasyon. |
| **TumbleAkisServisi.cs** | Servis | Tumble döngüsü orkestrasyonu: cluster bul, patlat, refill, çarpan, tekrar. |
| **ScatterEfektServisi.cs** | Servis | Scatter hücreleri büyütme efekti. IScatterEfektBaglami ile bağlam alır. |
| **BonusUIAkisServisi.cs** | Servis | Bonus başlangıç/bitiş paneli: ShowBonusStartMessage, ShowBonusEndMessage (fade, TMP, ses). |
| **BonusSatinAlmaAkisServisi.cs** | Servis | Bonus satın alma onay akışı: maliyet kontrolü, panel göster/gizle, OnYes/OnNo, onConfirmed callback. |
| **CarpanOverlayServisi.cs** | Servis | Çarpan overlay’leri: hücrede “xN” oluşturma, düşme animasyonu, ClearAll, AnimasyonIcinOverlayleriAl. |
| **AnimasyonServisi.cs** | Servis | Pop animasyonu, çarpan overlay referansları (GetCarpanOverlays), hücre pozisyonları. |
| **OyunUIGuncellemeServisi.cs** | Servis | Bakiye/bahis/hak/kazanç/çarpan metinleri ve buton durumlarını güncelleme. IOyunUIGuncellemeBaglami. |
| **UIServisi.cs** | Servis | UI delegasyonları: UIAutoBaglaGerekirse, panel aç/kapa, UI_Guncelle, buton durumu vb. Tek satırda impl çağrısı. |
| **SahneBaglamaServisi.cs** | Servis | UI referanslarını sahneden veya UIReferanslari’ndan bulup IBaglamaHedefi’ne yazar. BindIfNeeded, FindTmpByNameContains vb. |
| **LogServisi.cs** | Servis | Ekonomi kaydı: GameManager.RecordEconomyAction + opsiyonel Log. Tablo ve log tek noktadan. |
| **DonusKayitServisi.cs** | Servis | Spin sonucu kaydı (ödenen, bahis); GameManager totalWon/totalLost/totalNet güncellemesi için callback. |
| **HizVeSesServisi.cs** | Servis | Hız ve ses efektleri delegasyonu (spin/bonus sesleri vb.). |
| **OyunFormatServisi.cs** | Servis | Static: FormatTL (para formatı) ve benzeri format yardımcıları. |
| **ZorlukServisi.cs** | Servis | Zorluk değeri ve bias çarpanı; IZorlukBaglami ile bağlam. |
| **AdminAyarUIServisi.cs** | Servis | Admin paneli slider/buton bağlama, BindAllAndRefresh. OyunYoneticisi ve AdminPanel ile çalışır. |
| **OyunBootstrapServisi.cs** | Servis | Oyun başlangıç akışı: ayar sınıflarından sync, EnsurePayTablesInitialized vb. IOyunBootstrapBaglami. |
| **OyunKorumaServisi.cs** | Servis | Oyun koruma / güvenlik ile ilgili wrapper (context üzerinden erişim). |
| **KorutinServisi.cs** | Servis | İsimli coroutine: StartNamed, StopNamed, StopAll. Key ile başlat/durdur; bittiğinde sözlükten silinir. |
| **Scripts/Services/ (arayüzler)** |||
| **IDonusAkisBaglami.cs** | Arayüz | DonusAkisServisi için state ve servis erişimi. OyunYoneticisi implement eder. |
| **IIzgaraBaslatmaBaglami.cs** | Arayüz | IzgaraBaslatmaServisi için grid, hücreler, servis getter’ları. |
| **ICokmeAkisBaglami.cs** | Arayüz | CokmeAkisServisi için bağlam. |
| **ITumbleAkisBaglami.cs** | Arayüz | TumbleAkisServisi için bağlam. |
| **IScatterEfektBaglami.cs** | Arayüz | ScatterEfektServisi için bağlam. |
| **IOyunUIGuncellemeBaglami.cs** | Arayüz | OyunUIGuncellemeServisi için veri erişimi. |
| **IOyunBootstrapBaglami.cs** | Arayüz | OyunBootstrapServisi için bağlam. |
| **ICarpanYerlestirmeBaglami.cs** | Arayüz | CarpanYerlestirmeServisi için izgara/çarpan erişimi. |
| **IZorlukBaglami.cs** | Arayüz | ZorlukServisi için bağlam. |
| **IOyunKorumaBaglami.cs** | Arayüz | OyunKorumaServisi için bağlam. |

---

**Kısa özet**

- **Ana katman:** OyunYoneticisi orkestratör; GameManager/SaveSystem/PlayerProfile veri ve kalıcılık; GirisUI/AdminPanel/LogYoneticisi/SenaryoYoneticisi sahneler; BonusSatinAlUI, UIReferanslari, SpinIconRotate, HosgeldinizText UI bileşenleri; *Ayarlari sınıfları Inspector ayarları.
- **Servisler:** Akış (Donus, Cokme, Tumble, Bonus UI, Bonus SatinAlma), veri (Ekonomi, Izgara, Carpan, Odeme, Senaryo), UI (UIServisi, SahneBaglama, OyunUIGuncelleme, AdminAyar), yardımcı (Log, DonusKayit, HizVeSes, OyunFormat, Zorluk, Korutin, OyunBootstrap, OyunKoruma, Animasyon, CarpanOverlay).
- **Arayüzler:** Her servisin “Baglami” arayüzü, OyunYoneticisi’nin servislere veri/servis sağlaması için.
