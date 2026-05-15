using System;
using System.Collections.Generic;
using UnityEngine;

namespace KumarFarkindalik.Tutorial
{
    /// <summary>
    /// Tutorial state machine. PAKET 3B-fix-5: T3 → 5 alt-adım (hook → yontma → tutma → koruma → normal).
    /// Enum 16 değer; ana sayaç "ADIM 3/11" (T3 grubu tek adım sayılır) + alt sayaç "Senaryo X/5".
    /// </summary>
    public class TutorialAdimYoneticisi : MonoBehaviour
    {
        public enum TutorialAdimId
        {
            T1, T2,
            T3_HOOK, T3_YONTMA, T3_TUTMA, T3_KORUMA,  // PAKET 8: T3_NORMAL kaldırıldı (4 senaryo)
            T4, T5,
            T6_YENI_OYUNCU,
            T6, T7, T8, T9, T10, T11,
            T_SON
        }

        public TutorialAdimId mevcutAdim = TutorialAdimId.T1;
        public event Action<AdimVerisi> OnAdimDegisti;
        public event Action OnTutorialBitti;

        private readonly Dictionary<TutorialAdimId, AdimVerisi> _adimlar = new();
        private int _adimBaslangicSpin;
        private readonly HashSet<string> _adimSirasindaDegisenler = new();

        public AdimVerisi MevcutAdimVerisi => _adimlar.TryGetValue(mevcutAdim, out var v) ? v : null;
        public int AdimBaslangicSpin => _adimBaslangicSpin;
        public bool AdimSirasindaDegistirildi(string key) => _adimSirasindaDegisenler.Contains(key);

        private void Awake()
        {
            AdimlariDoldur();
        }

        public void AdimGec(TutorialAdimId yeni)
        {
            if (!_adimlar.ContainsKey(yeni))
            {
                Debug.LogError($"[TutorialAdimYoneticisi] Adım bulunamadı: {yeni}");
                return;
            }
            mevcutAdim = yeni;
            _adimBaslangicSpin = MevcutSpinAl();
            _adimSirasindaDegisenler.Clear();
            Debug.Log($"[TutorialAdimYoneticisi] Adım geçti: {yeni} (başlangıç spin={_adimBaslangicSpin})");
            OnAdimDegisti?.Invoke(_adimlar[yeni]);

            // PAKET 14-FAZ34 İş 6: Adım bazlı yönetici paneli disabled — mevcut vurguSelectorlari listesi
            // "aktif kalacak" elementler olarak yeniden kullanılır. Liste boş ise TÜM UI gri (T1, T2, T_SON).
            TutorialPanelKilit.KilitliAyarlariGonder(_adimlar[yeni].vurguSelectorlari);
        }

        public void IleriTiklandi()
        {
            if (mevcutAdim == TutorialAdimId.T_SON)
            {
                Debug.Log("[TutorialAdimYoneticisi] T_SON İLERİ → OnTutorialBitti");
                OnTutorialBitti?.Invoke();
                return;
            }
            int sonraki = (int)mevcutAdim + 1;
            if (sonraki > (int)TutorialAdimId.T_SON) return;
            AdimGec((TutorialAdimId)sonraki);
        }

        public void AyarDegistiHaber(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            _adimSirasindaDegisenler.Add(key);
        }

        /// <summary>PAKET 14-FAZ34.3 BUG G: T4/T5/T6YO/T10 ikinci aşama tetiklendiğinde "1/2" → "2/2".
        /// AdimVerisi.altSayac mutate edilir + TutorialAdimGoster sayaç text'i hemen yenilenir.</summary>
        public void AltSayacGuncelle(string yeniDeger)
        {
            if (!_adimlar.TryGetValue(mevcutAdim, out var v)) return;
            v.altSayac = yeniDeger;
            TutorialAdimGoster.Ornek?.SayacTextGuncelle(v.sira, yeniDeger);
            Debug.Log($"[TutorialAdimYoneticisi] AltSayacGuncelle: adim={mevcutAdim}, altSayac={yeniDeger}");
        }

        // PAKET 14-FAZ3 (İş 4): T5 (ve gelecekte T4) ikinci aşama girişinde çağrılır — AdimBaslangicSpin'i
        // mevcut sayaca alır + gerekliSpin'i ikinci aşama hedefi ile değiştirir. Böylece bonus yarım kes
        // sırasında sayaç fantom artmış olsa bile, ikinci aşama için TAM yeniGerekli spin gerekir.
        public void IkinciAsamaIcinSayaciResetle(int yeniGerekliSpin)
        {
            _adimBaslangicSpin = MevcutSpinAl();
            if (_adimlar.TryGetValue(mevcutAdim, out var v))
                v.gerekliSpin = yeniGerekliSpin;
            Debug.Log($"[TutorialAdimYoneticisi] İkinci aşama sayaç reset: adim={mevcutAdim}, baslangic={_adimBaslangicSpin}, gerekliSpin={yeniGerekliSpin}");
        }

        // PAKET 14-FAZ5 (İş 4): yapilacaklar[idx] dinamik güncelleme. TutorialAdimGoster.IlerlemeGuncelle
        // her frame v.yapilacaklar[i] okuduğu için mutate otomatik UI refresh olur.
        public void YapilacakMaddesiniGuncelle(int idx, string yeniMetin)
        {
            if (!_adimlar.TryGetValue(mevcutAdim, out var v)) return;
            if (v.yapilacaklar == null || idx < 0 || idx >= v.yapilacaklar.Length) return;
            v.yapilacaklar[idx] = yeniMetin;
            Debug.Log($"[TutorialAdimYoneticisi] yapilacaklar[{idx}]='{yeniMetin}' (adim={mevcutAdim})");
        }

        public bool KosulSagla(int mevcutSpin)
        {
            if (!_adimlar.TryGetValue(mevcutAdim, out var v)) return true;
            if (!v.aktifMi) return true;

            if (v.degisimAnahtarlari != null)
            {
                foreach (var k in v.degisimAnahtarlari)
                    if (!_adimSirasindaDegisenler.Contains(k)) return false;
            }

            int delta = mevcutSpin - _adimBaslangicSpin;
            bool spinOK = delta >= v.gerekliSpin;
            bool parametreOK = v.parametreKosulu?.Invoke() ?? true;
            return spinOK && parametreOK;
        }

        public static int MevcutSpinAl()
        {
            if (TutorialOyunYoneticisi.Ornek != null)
                return TutorialOyunYoneticisi.Ornek.TutorialSpinSayaci;
            return SenaryoYoneticisi.I != null ? SenaryoYoneticisi.I.toplamSpin : 0;
        }

        private void AdimlariDoldur()
        {
            _adimlar[TutorialAdimId.T2] = new AdimVerisi
            {
                id = TutorialAdimId.T2,
                aktifMi = false,
                mesajBaslangic = T2_BASLANGIC,
                altBaslik = "GİRİŞ",
                yapilacaklar = null,
                sira = 2,
                altSayac = null,
                gerekliSpin = 0,
            };

            // === T3 GRUP: 5 senaryo (hook → yontma → tutma → koruma → normal) ===

            _adimlar[TutorialAdimId.T3_HOOK] = new AdimVerisi
            {
                id = TutorialAdimId.T3_HOOK,
                aktifMi = true,
                mesajBaslangic = T3_HOOK_A,
                mesajAksiyon = T3_HOOK_B,
                mesajKapanis = T3_HOOK_C,
                altBaslik = "TAZE KAN (HOOK)",
                yapilacaklar = new[] { "Oyun Modu 'Taze Kan' seç", "Uygula bas", "5 spin at" },
                sira = 3,
                // PAKET 14-FAZ35.4: altSayac kaldırıldı — kategoriIciSira/Toplam=1/4 ile DUPLICATE üretiyordu ("1/4 · 1/4 ...").
                vurguSelectorlari = new[] { "#oyunModu", "#senaryoUygulaBtn" },
                gerekliSpin = 5,
                parametreKosulu = () => PanelKopru.aktifSenaryo == "hook",
                degisimAnahtarlari = new[] { "oyunModu" },
            };

            _adimlar[TutorialAdimId.T3_YONTMA] = new AdimVerisi
            {
                id = TutorialAdimId.T3_YONTMA,
                aktifMi = true,
                mesajBaslangic = T3_YONTMA_A,
                mesajAksiyon = T3_YONTMA_B,
                mesajKapanis = T3_YONTMA_C,
                altBaslik = "AZ AZ KAYIP (YONTMA)",
                yapilacaklar = new[] { "Oyun Modu 'Az Az Kayıp' seç", "Uygula bas", "5 spin at" },
                sira = 3,
                // PAKET 14-FAZ35.4: altSayac kaldırıldı (kategoriIciSira/Toplam=2/4 yeterli).
                vurguSelectorlari = new[] { "#oyunModu", "#senaryoUygulaBtn" },
                gerekliSpin = 5,
                parametreKosulu = () => PanelKopru.aktifSenaryo == "yontma",
                degisimAnahtarlari = new[] { "oyunModu" },
            };

            _adimlar[TutorialAdimId.T3_TUTMA] = new AdimVerisi
            {
                id = TutorialAdimId.T3_TUTMA,
                aktifMi = true,
                mesajBaslangic = T3_TUTMA_A,
                mesajAksiyon = T3_TUTMA_B,
                mesajKapanis = T3_TUTMA_C,
                altBaslik = "KAÇIŞ ENGELLEME (TUTMA)",
                // PAKET 4-FAZ-3: TUTMA pattern motoru 6 spin (0/0/2000/0/0/2000) — pedagojik "kayıp → tutucu → kayıp → tutucu" ritmi
                yapilacaklar = new[] { "Oyun Modu 'Kaçış Engelleme' seç", "Uygula bas", "6 spin at" },
                sira = 3,
                // PAKET 14-FAZ35.4: altSayac kaldırıldı (kategoriIciSira/Toplam=3/4 yeterli).
                vurguSelectorlari = new[] { "#oyunModu", "#senaryoUygulaBtn" },
                gerekliSpin = 6,
                parametreKosulu = () => PanelKopru.aktifSenaryo == "tutma",
                degisimAnahtarlari = new[] { "oyunModu" },
            };

            _adimlar[TutorialAdimId.T3_KORUMA] = new AdimVerisi
            {
                id = TutorialAdimId.T3_KORUMA,
                aktifMi = true,
                mesajBaslangic = T3_KORUMA_A,
                mesajAksiyon = T3_KORUMA_B,
                mesajKapanis = T3_KORUMA_C,
                altBaslik = "BAKİYE TÜKETME (KORUMA)",
                yapilacaklar = new[] { "Oyun Modu 'Bakiye Tüketme' seç", "Uygula bas", "5 spin at" },
                sira = 3,
                // PAKET 14-FAZ35.4: altSayac kaldırıldı (kategoriIciSira/Toplam=4/4 yeterli).
                vurguSelectorlari = new[] { "#oyunModu", "#senaryoUygulaBtn" },
                gerekliSpin = 5,
                parametreKosulu = () => PanelKopru.aktifSenaryo == "koruma",
                degisimAnahtarlari = new[] { "oyunModu" },
            };

            // === T4-T11 (mevcut, sira override) ===

            _adimlar[TutorialAdimId.T4] = new AdimVerisi
            {
                id = TutorialAdimId.T4,
                aktifMi = true,
                mesajGecis = T4_GECIS,
                mesajBaslangic = T4_BASLANGIC,
                mesajAksiyon = T4_AKSIYON,
                mesajKapanis = T4_KAPANIS,
                altBaslik = "ÇARPAN OLASILIĞI",
                // PAKET 14-FAZ34.3 BUG G: 2 aşamalı (%100 / %0) — altSayac dinamik. T4IkinciAsamaBasladi=true
                // olunca AltSayacGuncelle("2/2") çağrılır (TutorialAdminEnjeksiyonu case carpanOlasilik).
                altSayac = "1/2",
                // PAKET 14-FAZ5 (İş 4): Aşamaya göre dinamik. Default ilk aşama metni; T4AraModalGosterildi
                // olunca TutorialOyunYoneticisi.YapilacakMaddesiniGuncelle ile "%0 ayarla"'ya geçer.
                yapilacaklar = new[] { "Çarpan %100 ayarla", "2 spin at" },
                sira = 4,
                vurguSelectorlari = new[] { "#carpanOlasilik", "#carpanOlasilikInput" },
                gerekliSpin = 2,
                // PAKET 14-FAZ4 (İş 1): Sınır SIKI — 95/5 toleransı %94/%6'yı kabul ediyordu.
                // %100 ve %0 için 0.5 tolerans (float yuvarlama güvencesi).
                parametreKosulu = () => TutorialOyunYoneticisi.T4AraModalGosterildi
                    ? TutorialAdminEnjeksiyonu.SonCarpanOlasilik <= 0.5f
                    : TutorialAdminEnjeksiyonu.SonCarpanOlasilik >= 99.5f,
                degisimAnahtarlari = new[] { "carpanOlasilik" },
            };

            _adimlar[TutorialAdimId.T5] = new AdimVerisi
            {
                id = TutorialAdimId.T5,
                aktifMi = true,
                mesajBaslangic = T5_BASLANGIC,
                mesajAksiyon = T5_AKSIYON,
                mesajKapanis = T5_KAPANIS,
                altBaslik = "BONUS SEMBOLÜ",
                // PAKET 14-FAZ34.3 BUG G: 2 aşamalı — altSayac dinamik.
                altSayac = "1/2",
                // PAKET 14-FAZ5 (İş 4): Aşamaya göre dinamik (T4 ile aynı pattern).
                yapilacaklar = new[] { "Bonus %100 ayarla", "2 spin at" },
                sira = 5,
                vurguSelectorlari = new[] { "#bonusSembolOlasilik" },
                gerekliSpin = 2,
                // PAKET 14-FAZ5 (İş 1): yuzde DİREKT. Periyot=round(100/yuzde) %67-100 hep 1 dönüyordu.
                // panel.html "bonusYuzde" event'i ile yuzde TutorialAdminEnjeksiyonu.SonBonusYuzdesi'ne yansır.
                parametreKosulu = () => TutorialOyunYoneticisi.T5AraModalGosterildi
                    ? TutorialAdminEnjeksiyonu.SonBonusYuzdesi <= 0.5f
                    : TutorialAdminEnjeksiyonu.SonBonusYuzdesi >= 99.5f,
                degisimAnahtarlari = new[] { "bonusYuzde" },
            };

            _adimlar[TutorialAdimId.T6_YENI_OYUNCU] = new AdimVerisi
            {
                id = TutorialAdimId.T6_YENI_OYUNCU,
                aktifMi = true,
                mesajBaslangic = T6YO_BASLANGIC,
                mesajAksiyon = T6YO_AKSIYON,
                mesajKapanis = T6YO_KAPANIS,
                altBaslik = "YENİ OYUNCU MODU",
                // PAKET 14-FAZ34.3 BUG G: 2 aşamalı (toggle aç / toggle kapat) — altSayac dinamik.
                altSayac = "1/2",
                // PAKET 14-FAZ8: T6YO TEMİZ — 1.aşama kullanıcı AÇAR (kazanç), ara modal sonrası KAPATIR (kayıp).
                // yapilacaklar 2 madde — TutorialT6YeniOyuncuModalKontrol içinde 1.madde dinamik mutate edilir
                // (ara modal sonrası "kapat"'a geçer).
                yapilacaklar = new[] { "Yeni Oyuncu Modu'nu aç", "6 spin at" },
                sira = 6,
                vurguSelectorlari = new[] { "#yeniOyuncuToggle" },
                gerekliSpin = 6,
                parametreKosulu = () => TutorialOyunYoneticisi.T6AraModalGosterildi
                    ? !PanelKopru.yeniOyuncuModu       // 2.aşama: toggle KAPATILMALI
                    : PanelKopru.yeniOyuncuModu,       // 1.aşama: toggle AÇILMALI (giriş kapalı, kullanıcı açacak)
                // PAKET 14-FAZ34.5 BUG H: degisimAnahtarlari ekle → UygulamaOnaylandi guard aktive olur.
                // Önceki: null → toggle aç anında SpinKilitli=false (Uygula bypass). Yeni: toggle aç → Uygula gerek.
                degisimAnahtarlari = new[] { "yeniOyuncu" },
            };

            _adimlar[TutorialAdimId.T6] = new AdimVerisi
            {
                id = TutorialAdimId.T6,
                aktifMi = true,
                mesajBaslangic = T6_BASLANGIC,
                mesajAksiyon = T6_AKSIYON,
                mesajKapanis = T6_KAPANIS,
                altBaslik = "5'DE KAÇ KAZANÇ?",
                // PAKET 6C3: 5'lik N mantığı — slider değeri/2 = 5'de N kazanç
                yapilacaklar = new[] { "Kazandırma sıklığı slider'ını ayarla", "Uygula bas", "5 spin at" },
                sira = 7,
                vurguSelectorlari = new[] { "#kazanmaOrani" },
                gerekliSpin = 5,
                // PAKET 14-FAZ35.7: Slider KESIN 3'e ayarlanmalı (talimat metni "3'e ayarla").
                // Eski `> 0` lambda kullanıcı 1/2/4/5'te de kabul ediyordu → pedagojik mesaj bozuk.
                parametreKosulu = () => Mathf.Abs(PanelKopru.kazanmaOrani - 3f) < 0.01f,
                degisimAnahtarlari = new[] { "kazanmaOrani" },
            };

            _adimlar[TutorialAdimId.T7] = new AdimVerisi
            {
                id = TutorialAdimId.T7,
                aktifMi = true,
                mesajBaslangic = T7_BASLANGIC,
                mesajAksiyon = T7_AKSIYON,
                mesajKapanis = T7_KAPANIS,
                altBaslik = "MIN-MAKS ÖDEME ARALIĞI",
                // PAKET 14-FAZ35.2: Tek aşama, slider varsayılan 3-5, 3 sabit spin (3000/4000/5000).
                // Eski 2 aşamalı (gerekliSpin=6) tasarım kaldırıldı — paytable_8_9 max 1.5 olduğu için
                // 3-5x bandı eşleşmiyordu + ilk 3 spin 0 kazanç regression vardı.
                yapilacaklar = new[] { "Min=3, Maks=5 ayarla (varsayılan)", "Uygula bas", "3 spin at" },
                sira = 8,
                vurguSelectorlari = new[] { "#minCarpan", "#maksCarpan" },
                gerekliSpin = 3,
                // PAKET 14-FAZ35.7: Min=3 ve Maks=5 KESIN değer kontrolü (talimat "Min=3, Maks=5 ayarla").
                // Eski `> 0 && > 0` farklı değerleri kabul ediyordu → SPIN açılıp yanlış aralık spin'i.
                parametreKosulu = () => Mathf.Abs(PanelKopru.minCarpan - 3f) < 0.01f
                                     && Mathf.Abs(PanelKopru.maksCarpan - 5f) < 0.01f,
                degisimAnahtarlari = new[] { "minCarpan", "maksCarpan" },
            };

            _adimlar[TutorialAdimId.T8] = new AdimVerisi
            {
                id = TutorialAdimId.T8,
                aktifMi = true,
                mesajBaslangic = T8_BASLANGIC,
                mesajAksiyon = T8_AKSIYON,
                mesajKapanis = T8_KAPANIS,
                altBaslik = "5'DE KAÇ NEAR MISS?",
                // PAKET 6C3: 5'lik N mantığı — slider değeri/2 = 5'de N near miss
                yapilacaklar = new[] { "Near miss slider'ını ayarla", "Uygula bas", "5 spin at" },
                sira = 9,
                vurguSelectorlari = new[] { "#yakinKacirma" },
                gerekliSpin = 5,
                // PAKET 14-FAZ35.7: Near miss slider'ı KESIN 3 (talimat metni güncellendi: "3'e getir").
                parametreKosulu = () => Mathf.Abs(PanelKopru.yakinKacirma - 3f) < 0.01f,
                degisimAnahtarlari = new[] { "yakinKacirma" },
            };

            _adimlar[TutorialAdimId.T9] = new AdimVerisi
            {
                id = TutorialAdimId.T9,
                aktifMi = true,
                mesajBaslangic = T9_BASLANGIC,
                mesajAksiyon = T9_AKSIYON,
                mesajKapanis = T9_KAPANIS,
                altBaslik = "ÇIKMA ANINDA YAKALAMA",
                // PAKET 6D: 3 kayıp + 1 kazanç (limit'e ulaşıldığında otomatik frenleme)
                yapilacaklar = new[] { "Kaçış limiti kutusuna 3 yaz", "Uygula bas", "4 spin at" },
                sira = 10,
                vurguSelectorlari = new[] { "#ardisikKayip" },
                gerekliSpin = 4,
                // PAKET 14-FAZ35.7: Kaçış limiti KESIN 3 (talimat "3 yaz"). Eski 1-4 aralığı pedagojik
                // tutarlılığı bozuyordu (kullanıcı 1 yazınca da geçiyordu).
                parametreKosulu = () => PanelKopru.ardisikKayipLimiti == 3,
                degisimAnahtarlari = new[] { "ardisikKayip" },
            };

            _adimlar[TutorialAdimId.T10] = new AdimVerisi
            {
                id = TutorialAdimId.T10,
                aktifMi = true,
                mesajBaslangic = T10_BASLANGIC,
                mesajAksiyon = T10_AKSIYON,
                mesajKapanis = T10_KAPANIS,
                altBaslik = "AÇIK MI KAPALI MI?",
                // PAKET 14-FAZ34.3 BUG G: 2 aşamalı (kapalı ödeme / açık ödeme).
                // PAKET 14-FAZ35.4: altSayac kaldırıldı — kategoriIciSira/Toplam=1/2 ile DUPLICATE ("1/2 · 1/2 AÇIK MI KAPALI MI?").
                // 2.aşama state programatik takip edilir (T11AraModalGosterildi/T11IkinciAsamaBasladi), UI'de ek sayaç yok.
                // PAKET 6D: 2-aşamalı — Aşama 1 (toggle KAPALI + ×500 = ödeme yok), ara modal,
                // Aşama 2 (toggle AÇIK + ×500 = mega kazanç)
                yapilacaklar = new[] { "×500 butonuna bas (toggle KAPALI)", "'Çarpan Ödeme' toggle aç", "×500 tekrar bas" },
                sira = 11,
                vurguSelectorlari = new[] { "#carpanOdemeToggle", "#carpanZorla500" },
                gerekliSpin = 2,
                parametreKosulu = () => TutorialAdminEnjeksiyonu.SonCarpanZorla == 500,
                degisimAnahtarlari = new[] { "carpanZorla" },
            };

            _adimlar[TutorialAdimId.T11] = new AdimVerisi
            {
                id = TutorialAdimId.T11,
                aktifMi = true,
                mesajBaslangic = T11_BASLANGIC,
                mesajAksiyon = T11_AKSIYON,
                mesajKapanis = T11_KAPANIS,
                altBaslik = "BONUS TETİKLE",
                yapilacaklar = new[] { "Bonus Tetikle butonuna bas" },
                sira = 12,
                vurguSelectorlari = new[] { "#bonusTetikleBtn" },
                gerekliSpin = 0,
                parametreKosulu = () => TutorialAdminEnjeksiyonu.BonusTetiklendi,
                degisimAnahtarlari = new[] { "bonusTetikle" },
            };

            _adimlar[TutorialAdimId.T_SON] = new AdimVerisi
            {
                id = TutorialAdimId.T_SON,
                aktifMi = false,
                // PAKET 4-FAZ-3: TSON_BASLANGIC kaldırıldı (boş string) → AdimAkisi Modal A'yı skip eder.
                // T_SON'a girince hemen OnTutorialBitti → TutorialBitti → KapanisAkisi → tek modal (Yeşilay + rehber).
                // Eski TSON_BASLANGIC sabiti dosyada kalıyor (kullanılmıyor) — gelecek temizlikte silinebilir.
                mesajBaslangic = "",
                altBaslik = "TAMAMLANDI",
                yapilacaklar = null,
                sira = 12,
                gerekliSpin = 0,
            };

            // PAKET 14-FAZ34.6 İş 1: Tüm adımlara kategori bilgisi ata.
            // Yan panel kategorileri: OLASILIK AYARLARI (4) / MANİPÜLASYON (7) / ANLIK MÜDAHALE (2) / BİTİŞ.
            KategoriBilgileriDoldur();
        }

        private void KategoriBilgileriDoldur()
        {
            Kategori(TutorialAdimId.T2, 0, "BAŞLANGIÇ", 0, 0);

            Kategori(TutorialAdimId.T3_HOOK,    1, "OLASILIK AYARLARI", 1, 4);
            Kategori(TutorialAdimId.T3_YONTMA,  1, "OLASILIK AYARLARI", 2, 4);
            Kategori(TutorialAdimId.T3_TUTMA,   1, "OLASILIK AYARLARI", 3, 4);
            Kategori(TutorialAdimId.T3_KORUMA,  1, "OLASILIK AYARLARI", 4, 4);

            Kategori(TutorialAdimId.T4,                2, "MANİPÜLASYON", 1, 7);
            Kategori(TutorialAdimId.T5,                2, "MANİPÜLASYON", 2, 7);
            Kategori(TutorialAdimId.T6_YENI_OYUNCU,    2, "MANİPÜLASYON", 3, 7);
            Kategori(TutorialAdimId.T6,                2, "MANİPÜLASYON", 4, 7);
            Kategori(TutorialAdimId.T7,                2, "MANİPÜLASYON", 5, 7);
            Kategori(TutorialAdimId.T8,                2, "MANİPÜLASYON", 6, 7);
            Kategori(TutorialAdimId.T9,                2, "MANİPÜLASYON", 7, 7);

            Kategori(TutorialAdimId.T10, 3, "ANLIK MÜDAHALE", 1, 2);
            Kategori(TutorialAdimId.T11, 3, "ANLIK MÜDAHALE", 2, 2);

            Kategori(TutorialAdimId.T_SON, 4, "BİTİŞ", 1, 1);
        }

        private void Kategori(TutorialAdimId adim, int kategoriIndex, string kategoriAdi, int iciSira, int iciToplam)
        {
            if (!_adimlar.TryGetValue(adim, out var v)) return;
            v.kategoriIndex = kategoriIndex;
            v.kategoriAdi = kategoriAdi;
            v.kategoriIciSira = iciSira;
            v.kategoriIciToplam = iciToplam;
        }

        // === Adım metinleri (A=başlangıç, B=aksiyon, C=kapanış) ===

        private const string T2_BASLANGIC =
            "Az önce oyuncunun yaşadığı <color=#F24D40>manipülasyon</color> işte bu panelde kuruluyor. " +
            "Üç bölüm göreceğiz: <color=#5BA0FF>Olasılık</color>, <color=#5BA0FF>Manipülasyon</color> ve <color=#5BA0FF>Anlık Müdahale</color>. Başlayalım.";

        // === T3 — 5 senaryo ===

        private const string T3_HOOK_A =
            "<color=#FFD933>5 oyun modu</color> var. Sırayla her birini deneyip aralarındaki farkı göreceğiz.\n\n" +
            "İlk senaryo: <color=#F24D40>TAZE KAN (Hook)</color>. Yeni gelen oyuncu için tasarlandı — " +
            "<color=#4DCC59>bol kazandırma</color>, <color=#F24D40>yumuşak kayıplar</color>, <color=#FFD933>'şanslıyım'</color> hissi.";
        private const string T3_HOOK_B =
            "<color=#5BA0FF>Oyun Modu</color>'ndan <color=#5BA0FF>'Taze Kan'</color> seç, <color=#5BA0FF>Uygula</color> bas. <color=#FFD933>5 spin</color> at.";
        private const string T3_HOOK_C =
            "Az önce görüldüğü üzere yeni oyuncu hemen kazandı ve oyuna bağlandı. Sömürünün başlangıcı!";

        private const string T3_YONTMA_A =
            "İkinci senaryo: <color=#F24D40>AZ AZ KAYIP (Yontma)</color>. Oyuncu farkına varmadan, küçük küçük <color=#F24D40>kaybettirme</color>. " +
            "Bakiye <color=#F24D40>sessizce erir</color>.";
        private const string T3_YONTMA_B =
            "<color=#5BA0FF>Oyun Modu</color>'ndan <color=#5BA0FF>'Az Az Kayıp'</color> seç, <color=#5BA0FF>Uygula</color> bas. <color=#FFD933>5 spin</color> at.";
        private const string T3_YONTMA_C =
            "Oyuncu farkına varmadan, bahis miktarından daha az kazandırılıp bakiye sessizce eritilir.";

        private const string T3_TUTMA_A =
            "Üçüncü oyun: KAÇIŞ ENGELLEME. Oyuncu çıkmaya niyetlenirse sistem küçük kazanç hediyesi verir. " +
            "Bu aşamada 2 tur üst üste kayıp yaşayan oyuncuya sistem, 3. turda oyundan kaçmasını engellemek için bilinçli bir şekilde ufak kazanç verir.";
        private const string T3_TUTMA_B =
            "<color=#5BA0FF>Oyun Modu</color>'ndan <color=#5BA0FF>'Kaçış Engelleme'</color> seç, <color=#5BA0FF>Uygula</color> bas. <color=#FFD933>6 spin</color> at.";
        private const string T3_TUTMA_C =
            "2 kez kayıp sonra KAZANÇ. Yine 2 kez kayıp, yine KAZANÇ. " +
            "Sistem oyuncuyu tam çıkacağı anda küçük kazançla TUTAR. Bu da manipülasyon tekniklerinden biridir.";

        private const string T3_KORUMA_A =
            "Dördüncü oyun modu: BAKİYE TÜKETME. Kazanç neredeyse durur. Oyuncunun son kuruşuna kadar kaybetmesi amaçlanır.";
        private const string T3_KORUMA_B =
            "<color=#5BA0FF>Oyun Modu</color>'ndan <color=#5BA0FF>'Bakiye Tüketme'</color> seç, <color=#5BA0FF>Uygula</color> bas. <color=#FFD933>5 spin</color> at.";
        private const string T3_KORUMA_C =
            "Kazanç yok. Bu mod, oyuncuyu son kuruşuna kadar kaybettirerek tüketmeyi amaçlıyor.";

        // PAKET 8: T3_NORMAL_A/B/C kaldırıldı (T3_NORMAL adımı sistemden kaldırıldı).
        // Normal oyun bilgilendirmesi karşılama (T1) sonrası tek modal ile veriliyor (T1_NORMAL_INFO).

        // === T4-T11 (mevcut) ===

        // PAKET 9: T4 geçiş modali — T3 senaryo bloğundan Olasılık Ayarları bölümüne pedagojik köprü.
        private const string T4_GECIS =
            "<b>Olasılık Ayarları</b>\n\n" +
            "<color=#4DCC59>Oyun modlarını gördük</color> — kumar sitelerinin " +
            "oyuncuyu hangi senaryolarla yönlendirdiği öğrenildi.\n\n" +
            "Şimdi <color=#5BA0FF>Olasılık Ayarları</color> bölümüne geçiliyor. " +
            "Burada <color=#F24D40>sayılarla</color> manipülasyonun nasıl " +
            "yapıldığı görülecek.";

        private const string T4_BASLANGIC =
            "<b>ÇARPAN nedir?</b>\n\n" +
            "Bazı spinlerde grid'e <color=#5BA0FF>ÇARPAN sembolü</color> düşer (<color=#FFD933>×2, ×3, ×8, ×100</color> gibi). " +
            "Kazançlı spinde çarpan varsa <color=#4DCC59>kazanç o sayıyla çarpılır</color>. " +
            "Örneğin <color=#FFD933>500 TL</color> kazanılır + <color=#FFD933>×8</color> çarpan düşer → <color=#4DCC59>4.000 TL</color>.\n\n" +
            "<color=#F24D40>Kumar siteleri</color> bu olasılığı istediği gibi ayarlayabilir. Yüksek tutarsa kullanıcı " +
            "'vay be, sürekli çarpan geliyor' diye <color=#F24D40>heyecanlanır</color> → daha çok spin atar → daha çok <color=#F24D40>kaybeder</color>.\n\n" +
            "<b>Şimdi deneyelim:</b> Çarpan olasılığını <color=#FFD933>%100 yap</color>, Uygula bas, <color=#FFD933>1 spin</color> at. " +
            "Çarpan kesin düşecek.";
        private const string T4_AKSIYON =
            "<color=#5BA0FF>Çarpan olasılığını</color> <color=#FFD933>%100</color> yap. Uygula bas, <color=#FFD933>1 spin</color> at.";
        // PAKET 9: T4 ara modal — kullanıcıya %0 daveti (tersini göstermek için).
        public const string T4_ARA_MODAL =
            "<color=#4DCC59>Çarpanları gördük</color> — her spinde garanti düştü.\n\n" +
            "Şimdi <color=#F24D40>tersini görelim</color>: olasılık <color=#FFD933>%0</color> olduğunda " +
            "<color=#F24D40>hiç çarpan düşmeyecek</color>.\n\n" +
            "<color=#5BA0FF>Çarpan olasılığını</color> <color=#FFD933>%0</color> yap, <color=#5BA0FF>Uygula</color> bas, <color=#FFD933>1 spin</color> daha at.";
        private const string T4_KAPANIS =
            "Az önce görüldüğü üzere çarpan düşme olasılığını %100 ayarladığımızda çarpan sembolü kesin düştü. " +
            "Ancak bu ayarı %0 yapınca çarpan hiç düşmedi. Kumar siteleri bu ayarı, oyuncuyu istediği gibi manipüle etmek için kullanır.";

        private const string T5_BASLANGIC =
            "BONUS SEMBOLÜ nedir?\n\n" +
            "Bonus sembolü (yıldız) oyun alanına nadir düşer. Bir spinde 4 veya daha fazla yıldız olursa bonus oyun açılır. 10 ücretsiz spin + büyük kazanç şansı. Kumar siteleri bu olasılığı istediği gibi ayarlar.\n\n" +
            "Şimdi deneyelim: Bonus olasılığını %100 yap, uygulaya bas, 1 spin at. Garantili bonus oyun açılacak.";
        private const string T5_AKSIYON =
            "<color=#5BA0FF>Bonus olasılığını</color> <color=#FFD933>%100</color> yap, <color=#5BA0FF>Uygula</color> bas, <color=#FFD933>1 spin</color> at.";
        // PAKET 14-FAZ35.5-B: T5_ARA_MODAL kaldırıldı (gereksiz modal). T5IlkAsamaSonuAkisi
        // doğrudan Aşama 2'ye geçer, kullanıcı bonus %0 ayarlar + 1 spin daha atar.
        private const string T5_KAPANIS =
            "%100 ayarında bonus garanti açıldı. %0 ayarında ise hiç açılmadı. Kumar siteleri bu çarpan ayarlarını oyuncudan gizler.";

        // PAKET 14-FAZ8 — T6_YENI_OYUNCU: kullanıcı toggle AÇAR (bol kazanç) → ara modal → KAPATIR (kayıp)
        private const string T6YO_BASLANGIC =
            "Şimdi kumar sitelerinin gizli silahını göreceğiz: Yeni oyuncu modu. " +
            "Bu ayar açıkken sistem, oyuncuyu yeni gelen olarak algılar ve ona özel bir strateji uygular: bol kazandırma, yumuşak kayıplar.";
        private const string T6YO_AKSIYON =
            "Manipülasyon ayarlarında \"Yeni Oyuncu Modu\" özelliğini aç ve 3 spin at (sistem oyuncuya kazandıracak). " +
            "Sonra özelliği kapat ve 3 spin daha at. Aradaki fark netleşecek.";
        public const string T6YO_ARA_MODAL =
            "<color=#FFD933>3 spin</color> attık (toggle açık). Sonuç: <color=#4DCC59>BOL KAZANÇ</color> — sistem oyuncuyu çekiyor.\n\n" +
            "Şimdi <color=#5BA0FF>Manipülasyon Ayarları</color>'nda <color=#5BA0FF>'Yeni Oyuncu Modu'</color> toggle'ını <color=#F24D40>KAPAT</color>. " +
            "Ardından <color=#FFD933>3 spin</color> daha at. <color=#F24D40>Gerçek ortaya çıkacak</color>.";
        private const string T6YO_KAPANIS =
            "Gördüğümüz üzere aynı slot, aynı bahis. Yeni oyuncu modu özelliği açık halde tutunca bol kazanç; özelliği kapatınca net kayıp.";

        // PAKET 6C3: T6 (Kazandırma) — 5'lik N mantığı, dinamik pattern motor
        private const string T6_BASLANGIC =
            "Şimdi \"Kazandırma Sıklığı'na\" bakalım. Bu ayar, 5 spinin kaçında kullanıcıya kazanç verileceğini belirler.";
        private const string T6_AKSIYON =
            "Slider'ı kaydır. Slider değeri = 5'te kaç kazanç. Örneğin slider 3 → 5'te 3 kazanç. Uygula bas, 5 spin at.";
        private const string T6_KAPANIS =
            "Mod ayarı 5'ten seçilen sayı değeri kadar ayarlanıp kazanç sağlandı. Kalanı ise kumar siteleri tarafından kaybettirilmiştir.";

        // PAKET 6D: T7 (Ödeme Aralığı) — 2-aşamalı maks 3x vs min 3-maks 5x karşılaştırma
        private const string T7_BASLANGIC =
            "Kazanç aralığı, kumar siteleri tarafından kazançların ödeme aralığını sınırlar. Oyun, bahis min ile bahis maks arasındaki bir tutarda ödeme yapar.";
        private const string T7_AKSIYON =
            "Ödeme aralığında min=3, maks=5 ayarla. Uygula bas, 3 spin at.";
        // PAKET 14-FAZ35.5-C: T7_ARA_MODAL kaldırıldı (T7 Faz 35.2'de tek aşamalı oldu, ölü kod).
        private const string T7_KAPANIS =
            "3 spin'de kazançlar 3000, 4000, 5000 TL olarak crescendo arttı. " +
            "Aralık dar (3-5x) ama her spin garanti kazanç. " +
            "Kumar siteleri bunu kullanıcıya 'şanslı seri' gibi gösterir. Gerçekte sistem her şeyi kontrol eder.";

        // PAKET 6C3: T8 (Near Miss) — 5'lik N mantığı, dinamik pattern motor (7-sembol layout)
        private const string T8_BASLANGIC =
            "\"Neredeyse kazanıyordun\" hissi, slot oyununun en güçlü tuzaklarından biridir. 8 sembol ödeme yapacakken ekrana hep 7 sembol düşürerek oyuncuya \"neredeyse oluyordu, bir sonraki turda kesin olacak\" diye düşündürür.";
        private const string T8_AKSIYON =
            "\"Neredeyse Kazanıyordu Hissi\" ayarında, kaç tur üst üste neredeyse kazanıyordun hissinin yaşatılacağı belirlenir. Uygulaya bas ve 5 spin at. Seçilen tur sayısı kadar bu hissin nasıl yaşatıldığını göreceksin.";
        private const string T8_KAPANIS =
            "Gördüğümüz üzere 7 aynı sembol düştü ama 1 EKSİK. Küme 8'den başlıyor. " +
            "Oyuncunun beyni 'KAZANIYORDUM' der oysa hiç şansı yoktur. " +
            "Bu manipülasyon dopamin hormonunu salgılar. Bu hormon, bağımlılığın temel mekanizmasıdır.";

        // PAKET 6D: T9 (Kaçış Frenleme) — 3 kayıp + 1 kazanç deterministik demo
        private const string T9_BASLANGIC =
            "Kaçış frenleme paneli. Kullanıcı sürekli kaybedip çıkma noktasına geldiğinde kumar siteleri ne yapar? " +
            "Onu tutmak için otomatik kazanç verir. Limit yazın: kaç kayıp sonra otomatik kazanç gelsin.";
        private const string T9_AKSIYON =
            "Kaçış limiti kutusuna 3 yaz. Yani 3 kayıptan sonra sistem otomatik kazanç verecek. " +
            "Uygula bas, 4 spin at.";
        private const string T9_KAPANIS =
            "Kullanıcının kaç kez üst üste kayıp yaşadıktan sonra oyundan sıkılmaması için kazanç vereceği ayar.";

        // PAKET 6D: T10 (Çarpan Zorla) — 2-aşamalı açık/kapalı toggle demo
        private const string T10_BASLANGIC =
            "Zorla Çarpan, kumar sitelerinin etkili silahlarından birisidir. Kumar siteleri istediği anda çarpan düşürür, istediği zaman ödeme yaptırır; ödeme yapmak istemediğinde ise çarpan düşürür ancak ekrana yeterli sayıda sembol getirmez. Oyuncu kaçan büyük çarpana üzülürken, \"bu sefer olacak\" diyerek oynamaya devam eder.";
        private const string T10_AKSIYON =
            "Önce Çarpan Ödeme özelliği KAPALI iken çarpan zorla; 500x butonuna bas. Spinin sonucunu izle.";
        public const string T10_ARA_MODAL =
            "Görüldüğü üzere 500x çarpan düştü; ama yeteri kadar herhangi bir sembolden 8 tane olmadığı için kazanç gerçekleşmedi.\n\n" +
            "Şimdi Çarpan Ödeme özelliğini AÇ ve 500x butonuna tekrar bas.";
        private const string T10_KAPANIS =
            "Aynı işlem. Ama bu sefer kazanç sağlayan sembol 8'den fazla ve çarpan düştü. MEGA KAZANÇ sağlandı. " +
            "Kumar siteleri bu özelliği kullanarak \"kullanıcıya bonus vereceğim\" der; gerisini ayarlar.";

        private const string T11_BASLANGIC =
            "Son silah: bonus oyununu elle tetikleme.";
        private const string T11_AKSIYON =
            "Bonus Tetikle butonuna bas.";
        private const string T11_KAPANIS =
            "Kumar siteleri, kullanıcı pes etmek üzereyken bonus oyunu tetikler. Kurban \"sonunda şans yüzüme güldü\" der. Ancak aslında kumar siteleri onu içeride tutmak için düğmeye bastı.";

        private const string TSON_BASLANGIC =
            "Eğitim simülasyonu tamamlandı. Artık istediğiniz gibi test edebilirsiniz. " +
            "AYARLAR butonuna basıp paneli açarak kendiniz tüm durumları deneyimleyebilirsiniz.\n\n" +
            "Bağımlılıkla mücadelede yalnız değilsiniz. Yeşilay Danışma Hattı 115.";
    }

    public class AdimVerisi
    {
        public TutorialAdimYoneticisi.TutorialAdimId id;
        public bool aktifMi;
        /// <summary>Opsiyonel geçiş modali — mesajBaslangic'ten ÖNCE gösterilir (önceki bölümün özeti +
        /// yeni bölümün giriş başlığı için kullanılır). null/boş ise atlanır.</summary>
        public string mesajGecis;
        public string mesajBaslangic;
        public string mesajAksiyon;
        public string mesajKapanis;

        public string altBaslik;
        public string[] yapilacaklar;

        // PAKET 3B-fix-5: T3 grup için ana sayaç + alt sayaç
        public int sira;          // "ADIM #/11" — T3_* için hepsi 3
        public string altSayac;   // "1/5".."5/5" — sadece T3_* için, diğerleri null

        // PAKET 14-FAZ34.6 İş 1: Kategori-bazlı sayaç (yan panel kategorileriyle birebir uyum).
        // Üst satır: "ADIM {kategoriIndex}/4 · {kategoriAdi}", Alt satır: "{kategoriIciSira}/{kategoriIciToplam} ..."
        // T1/T2 → kategoriIndex=0 ("BAŞLANGIÇ"), T3_* → 1 (OLASILIK 4 alt), T4..T9 → 2 (MANİPÜLASYON 7 alt),
        // T10/T11 → 3 (ANLIK MÜDAHALE 2 alt), T_SON → 4 (BİTİŞ 1 alt).
        public int kategoriIndex;
        public string kategoriAdi;
        public int kategoriIciSira;
        public int kategoriIciToplam;

        public string[] vurguSelectorlari;
        public int gerekliSpin;
        public Func<bool> parametreKosulu;
        public string[] degisimAnahtarlari;
    }
}
