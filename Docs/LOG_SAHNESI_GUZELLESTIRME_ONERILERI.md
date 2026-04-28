# 04_LogScane güzelleştirme önerileri

Bu dokümanda **04_LogScane** sahnesini daha okunaklı ve profesyonel görünümlü yapmak için yapılabilecekler listeleniyor. Hem Unity Editor’de hem (isteğe bağlı) kod tarafında uygulanabilir.

---

## 1. Görsel (Unity’de)

| Yapılacak | Açıklama |
|-----------|----------|
| **Arka plan** | Canvas veya ana panel için hafif gradient veya düz renk (örn. koyu gri #2C2C2C); metin panelleri beyaza yakın (#F5F5F5) ile kontrast. |
| **Panel başlıkları** | Panel_GenelOzet üstüne "GENEL ÖZET", Panel_SenaryoLog üstüne "SENARYO AKIŞI" yazan TMP_Text ekle; font 18–20, bold. |
| **Kenarlık / gölge** | Her panel için Outline veya Shadow (UI efekt) ile hafif çerçeve; paneller birbirinden net ayrılsın. |
| **Girişe dön butonu** | Butonu daha belirgin yap: renk (örn. mavi/yeşil), hover rengi, font 16–18. İstersen "Dışa aktar" yanına ikinci buton ekle. |
| **Boşluklar** | Panel padding 12–16 px; bölümler arası margin (Layout Group spacing veya boş GameObject). |
| **Font** | Tüm metinlerde aynı font ailesi; başlık 24, özet 20–22, log satırı 18–19. |

---

## 2. İşlevsel (kod + Unity)

| Yapılacak | Açıklama |
|-----------|----------|
| **Dışa aktar butonu** | Sahneye "Dışa Aktar" veya "TXT İndir" butonu ekle. LogYoneticisi’nde **Dışa Aktar Buton** alanına atanırsa Start’ta otomatik bağlanır; yoksa OnClick’e `LogYoneticisi.SenaryoLogunuTxtOlarakDisariAktar()` manuel bağlanır. |
| **Yenile butonu** | "Yenile" butonu ekle; OnClick’te `LogYoneticisi.Yenile()` ile verileri tekrar yükle. |
| **Filtre toggle** | "Sadece anlamlı olaylar" için Toggle; LogYoneticisi’nde **sadeceAnlamliOlaylar** alanına bağla (Inspector’dan veya kodla). |
| **Dışa aktarma mesajı** | Dışa aktarma sonrası kısa süre "Log kaydedildi: [dosya yolu]" yazan uyarı Text; LogYoneticisi’nde opsiyonel **DisAktarBilgiText** alanı. |

---

## 3. Hierarchy önerisi (güzelleştirilmiş)

```
Canvas
├── ArkaPlan (Image, tam ekran, renk/gradient)
├── UstBar
│   ├── Text_Baslik (Kullanici - İstatistikleri)
│   ├── Buton_Yenile
│   ├── Buton_DisariAktar
│   └── Buton_GirisEkrani (GİRİŞE DÖN)
├── Panel_GenelOzet
│   ├── Baslik_GenelOzet (TMP: "GENEL ÖZET")
│   └── GenelOzetText (TMP)
├── Panel_SenaryoLog
│   ├── Baslik_SenaryoLog (TMP: "SENARYO AKIŞI")
│   └── Scroll_SenaryoLog → Viewport → Content → SenaryoLoguText
└── (isteğe bağlı) AltBilgi_DisAktarYolu (TMP, küçük font)
```

---

## 4. Hızlı kontrol listesi

- [ ] Canvas/panel arka plan rengi ayarlandı mı?
- [ ] Her panelin üstünde başlık metni var mı?
- [ ] "GİRİŞE DÖN" butonu görünür ve tıklanabilir mi?
- [ ] "Dışa aktar" butonu eklendi ve LogYoneticisi’ne atandı mı?
- [ ] Senaryo logu Scroll View içinde ve uzun metinde kaydırma çalışıyor mu?
- [ ] Genel özet ve senaryo logu font boyutları okunaklı mı? (öneri: 20–22)

---

## 5. LogYoneticisi ek alanları (kod tarafı)

Script’e aşağıdaki alanlar eklenebilir; böylece Unity’de atama yaparak buton ve mesaj alanları kullanılır:

- **disariAktarButon** (Button): Atanırsa Start’ta OnClick’e `SenaryoLogunuTxtOlarakDisariAktar` bağlanır.
- **yenileButon** (Button): Atanırsa Start’ta OnClick’e `Yenile` bağlanır.
- **disAktarBilgiText** (TMP_Text): Dışa aktarma sonrası "Log kaydedildi: ..." yazılır; 3–5 sn sonra temizlenebilir.

Bu alanlar eklendikten sonra Inspector’dan atanarak ekstra kod yazmadan güzelleştirme tamamlanır.
