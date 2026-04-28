# Services Klasörü — Servis Özet Tablosu

| Servis | Ne işe yarıyor |
|--------|-----------------|
| **AdminAyarUIServisi** | Admin paneli: zorluk, scatter, çarpan olasılık/adet slider eventleri ve TMP metin güncellemeleri. BindAllAndRefresh ile bağlama. |
| **AnimasyonServisi** | Izgara drop, pop ve çarpan şişme animasyonları. Unity referansları setter ile alınır. |
| **BonusSatinAlmaAkisServisi** | Bonus satın alma onay akışı: maliyet kontrolü, panel göster/gizle, Evet/Hayır; ekonomi/ödeme OY callback ile. |
| **BonusUIAkisServisi** | Bonus başlangıç/bitiş panel UI: panel aç-kapat, TMP set, CanvasGroup fade, ses; ödeme mantığı OY’de. |
| **CarpanOverlayServisi** | Çarpan overlay’leri: hücrede “xN” oluşturma, silme, liste; AnimasyonIcinOverlayleriAl. Grid senkronu OY’de. |
| **CarpanServisi** | Çarpan state ve hesap (spin çarpanı, pending listesi, RollCarpanDegeri); bonus limit delegasyonu. İçinde CarpanYerlestirmeServisi: çarpanları dolu grid’e yerleştirme. |
| **CokmeAkisServisi** | Çökme + doldurma + animasyon akışı. CokmeDoldurVeCanlandir gövdesi; TumbleServisi ile koordinasyon. |
| **DonusAkisServisi** | Normal spin ve bonus döngüsü akışı (coroutine orkestrasyonu). IDonusAkisBaglami ile state/servis erişimi. |
| **DonusKayitServisi** | Dönüş ve bonus sonuç kaydı: GameManager tablo + log, SpinSettled/SpinStart/SpinResult event tetikleme. |
| **DonusServisi** | Tek “dönüş” tetiklemesi: grid doldurma, çarpan, TumbleLoop başlatma; hepsi delegasyon ile OY’ye. |
| **EkonomiServisi** | Bakiye ve bahis state; yükleme, yatır, çek, ödeme; GameManager ve PlayerPrefs senkronu. |
| **HizVeSesServisi** | Bonus yavaş mod (hız override), tumble SFX spam önleme; süre/ses delegasyonu. |
| **IzgaraBaslatmaServisi** | Izgara ilk kurulum: grid alloc, hücreler, pozisyonlar, CarpanOverlay/Animasyon bağlantısı; InitRoutine. |
| **IzgaraServisi** | Izgara ve sembol: XYToIndex, HucreSayisi, ScatterSay, RenderAllSprites, sembol seçimi (bias), scatter sayacı; diğerleri delegasyon. |
| **KorutinServisi** | İsimli coroutine: key ile StartNamed, StopNamed, StopAll; bittiğinde sözlükten silinir. |
| **LogServisi** | Ekonomi kaydı: GameManager.RecordEconomyAction + opsiyonel Log (tablo ve log tek noktadan). |
| **OdemeServisi** | Kasa/ödül havuzu katmanı: GetHavuzTL, AddBahisToKasa, PayFromHavuz; state yok, tüm erişim delegate ile (KasaYoneticisi). |
| **OyunBootstrapServisi** | Oyun başlangıç: servis oluşturma ve delegate bağlama orkestrasyonu. OY.Start → Sync + Bootstrap.Calistir → context.BootstrapMantiginiCalistir. |
| **OyunFormatServisi** | Static: TL formatı (FormatTL) ve benzeri format yardımcıları; tek kaynak. |
| **OyunKorumaServisi** | Oyun sabitleri (MAX_TUMBLE_TUR, TUMBLE_SABIT_ESIK vb.); context (IOyunKorumaBaglami) üzerinden erişim. |
| **OyunUIGuncellemeServisi** | Ana oyun UI: bakiye, bahis, kazanç, oturum, çarpan metinleri; buton durumu; para çek/bakiye yükle panel bağlama. FormatTL tek kaynak. |
| **SahneBaglamaServisi** | UI referanslarını sahneden isimle bulup hedefe (OY) yazar; null olan alanları doldurur. Para/Bakiye panel altı ref’leri. |
| **SenaryoServisi** | Zorluk, scatter şansı/eşik, çarpan ayarları, bonus bütçe/cap wrapper’ı; hepsi OY’den delegasyon. DonusAkisServisi/CokmeAkisServisi bunu kullanır. |
| **ScatterEfektServisi** | Scatter büyütme efekti: bonus tetiklenmeden önce scatter hücrelerinde scale animasyonu; context ile veri alır. |
| **TumbleAkisServisi** | Tumble döngüsü orkestrasyonu: guard/limit, tur sayacı, SFX throttle; grid değişimi ve animasyon context üzerinden. |
| **TumbleServisi** | Tumble (patlatma, düşme, refill) delegasyonu: TumbleLoop, cluster bulma, ödeme hesabı; asıl mantık OY’de. |
| **UIServisi** | UI işlemleri wrapper’ı: UIAutoBaglaGerekirse, panel aç/kapa, UI_Guncelle, buton durumu; hepsi tek satırda impl çağrısı. |
| **ZorlukServisi** | Zorluk değeri ve bias çarpanı; SetZorluk uygulaması; IZorlukBaglami ile context. |

---

## Kısa gruplama

| Grup | Servisler | Ortak rol |
|------|-----------|------------|
| **Akış (flow)** | DonusAkisServisi, CokmeAkisServisi, TumbleAkisServisi, BonusUIAkisServisi, BonusSatinAlmaAkisServisi | Spin/bonus/tumble/UI akışı, coroutine orkestrasyonu |
| **Veri / motor** | EkonomiServisi, OdemeServisi, IzgaraServisi, CarpanServisi, TumbleServisi, SenaryoServisi | Bakiye, kasa, grid, çarpan, tumble mantığı, parametre erişimi |
| **UI** | UIServisi, OyunUIGuncellemeServisi, SahneBaglamaServisi, AdminAyarUIServisi | UI bağlama, güncelleme, admin slider’lar |
| **Yardımcı** | LogServisi, DonusKayitServisi, OyunFormatServisi, KorutinServisi, HizVeSesServisi, AnimasyonServisi, CarpanOverlayServisi, ScatterEfektServisi, ZorlukServisi, OyunKorumaServisi | Log, kayıt, format, coroutine, ses/hız, animasyon, overlay, scatter efekt, zorluk, sabitler |
| **Başlangıç** | OyunBootstrapServisi, DonusServisi, IzgaraBaslatmaServisi | Oyun açılışı, tek dönüş tetiklemesi, izgara init |
