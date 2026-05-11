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
            T3_HOOK, T3_YONTMA, T3_TUTMA, T3_KORUMA, T3_NORMAL,
            T4, T5,
            T6_YENI_OYUNCU,  // PAKET 6C2: Yeni adım — operatörün hook fazı toggle'ı
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
                altSayac = "1/5",
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
                altSayac = "2/5",
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
                altSayac = "3/5",
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
                altSayac = "4/5",
                vurguSelectorlari = new[] { "#oyunModu", "#senaryoUygulaBtn" },
                gerekliSpin = 5,
                parametreKosulu = () => PanelKopru.aktifSenaryo == "koruma",
                degisimAnahtarlari = new[] { "oyunModu" },
            };

            _adimlar[TutorialAdimId.T3_NORMAL] = new AdimVerisi
            {
                id = TutorialAdimId.T3_NORMAL,
                aktifMi = true,
                mesajBaslangic = T3_NORMAL_A,
                mesajAksiyon = T3_NORMAL_B,
                mesajKapanis = T3_NORMAL_C,
                altBaslik = "NORMAL (KIYASLAMA)",
                yapilacaklar = new[] { "Oyun Modu 'Normal' seç", "Uygula bas", "5 spin at" },
                sira = 3,
                altSayac = "5/5",
                vurguSelectorlari = new[] { "#oyunModu", "#senaryoUygulaBtn" },
                gerekliSpin = 5,
                parametreKosulu = () => PanelKopru.aktifSenaryo == "normal",
                degisimAnahtarlari = new[] { "oyunModu" },
            };

            // === T4-T11 (mevcut, sira override) ===

            _adimlar[TutorialAdimId.T4] = new AdimVerisi
            {
                id = TutorialAdimId.T4,
                aktifMi = true,
                mesajBaslangic = T4_BASLANGIC,
                mesajAksiyon = T4_AKSIYON,
                mesajKapanis = T4_KAPANIS,
                altBaslik = "ÇARPAN OLASILIĞI",
                // PAKET 5: Accordion otomatik açılıyor (VurguAc) → "aç" maddesi çıkarıldı
                yapilacaklar = new[] { "Çarpan olasılığını %15 yap", "3 spin at" },
                sira = 4,
                vurguSelectorlari = new[] { "#carpanOlasilik", "#carpanOlasilikInput" },
                gerekliSpin = 3,
                parametreKosulu = () => TutorialAdminEnjeksiyonu.SonCarpanOlasilik >= 10f,
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
                // PAKET 6C1: 1 spin yeterli — scripted scatter pattern bonus tetikler
                yapilacaklar = new[] { "Bonus sembolü olasılığını arttır", "Uygula bas", "1 spin at" },
                sira = 5,
                vurguSelectorlari = new[] { "#bonusSembolOlasilik" },
                gerekliSpin = 1,
                parametreKosulu = () => PanelKopru.bonusOtomatikSpinPeriyodu > 0
                                        && PanelKopru.bonusOtomatikSpinPeriyodu <= 25,
                degisimAnahtarlari = new[] { "bonusOtomatikOran" },
            };

            _adimlar[TutorialAdimId.T6_YENI_OYUNCU] = new AdimVerisi
            {
                id = TutorialAdimId.T6_YENI_OYUNCU,
                aktifMi = true,
                mesajBaslangic = T6YO_BASLANGIC,
                mesajAksiyon = T6YO_AKSIYON,
                mesajKapanis = T6YO_KAPANIS,
                altBaslik = "YENİ OYUNCU MODU",
                // PAKET 6C2: 2-aşamalı adım — 3 spin (kapalı) + toggle + 3 spin (açık) = 6 spin total
                yapilacaklar = new[] { "Yeni Oyuncu Modu'nu aç", "6 spin at" },
                sira = 6,
                vurguSelectorlari = new[] { "#yeniOyuncuToggle" },
                gerekliSpin = 6,
                parametreKosulu = () => PanelKopru.yeniOyuncuModu,
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
                parametreKosulu = () => PanelKopru.kazanmaOrani > 0f,
                degisimAnahtarlari = new[] { "kazanmaOrani" },
            };

            _adimlar[TutorialAdimId.T7] = new AdimVerisi
            {
                id = TutorialAdimId.T7,
                aktifMi = true,
                mesajBaslangic = T7_BASLANGIC,
                mesajAksiyon = T7_AKSIYON,
                mesajKapanis = T7_KAPANIS,
                altBaslik = "MAKS VE MİN MANİPÜLASYON",
                // PAKET 6D: 2-aşamalı — Aşama 1 (maks=3, 3 spin), ara modal, Aşama 2 (min=3 maks=5, 3 spin)
                yapilacaklar = new[] { "Ödeme MAKS'ı 3'e ayarla", "Uygula bas, 3 spin at", "MIN=3 MAKS=5 yap, 3 spin daha at" },
                sira = 8,
                vurguSelectorlari = new[] { "#minCarpan", "#maksCarpan" },
                gerekliSpin = 6,
                parametreKosulu = () => PanelKopru.minCarpan > 0f && PanelKopru.maksCarpan > 0f,
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
                parametreKosulu = () => PanelKopru.yakinKacirma > 0f,
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
                parametreKosulu = () => PanelKopru.ardisikKayipLimiti > 0
                                        && PanelKopru.ardisikKayipLimiti <= 4,
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
                // PAKET 6D: 2-aşamalı — Aşama 1 (toggle KAPALI + ×500 = ödeme yok), ara modal,
                // Aşama 2 (toggle AÇIK + ×500 = mega kazanç)
                yapilacaklar = new[] { "×500 butonuna bas (toggle KAPALI)", "'Çarpan Ödeme' toggle aç", "×500 tekrar bas" },
                sira = 11,
                vurguSelectorlari = new[] { "#carpanOdemeToggle", "button[onclick=\"carpanZorla(500)\"]" },
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
                vurguSelectorlari = new[] { ".trigger-btn" },
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
        }

        // === Adım metinleri (A=başlangıç, B=aksiyon, C=kapanış) ===

        private const string T2_BASLANGIC =
            "Az önce yaşadığın manipülasyon işte bu panelde kuruluyor. " +
            "Üç bölüm göreceğiz: Olasılık, Manipülasyon ve Anlık Müdahale. Hadi başlayalım.";

        // === T3 — 5 senaryo ===

        private const string T3_HOOK_A =
            "5 oyun modu var. Sırayla her birini deneyip aralarındaki farkı göreceğiz.\n\n" +
            "İlk senaryo: TAZE KAN (Hook). Yeni gelen oyuncu için tasarlandı — bol kazandırma, " +
            "yumuşak kayıplar, 'şanslıyım' hissi.";
        private const string T3_HOOK_B =
            "Oyun Modu'ndan 'Taze Kan' seç, Uygula bas. 5 spin at.";
        private const string T3_HOOK_C =
            "Gördün mü? Yeni oyuncu hemen kazandı, oyuna bağlandı. Sömürünün başlangıcı — kanca.";

        private const string T3_YONTMA_A =
            "İkinci senaryo: AZ AZ KAYIP (Yontma). Oyuncu farkına varmadan, küçük küçük kaybettirme. " +
            "Bakiye sessizce erir.";
        private const string T3_YONTMA_B =
            "Oyun Modu'ndan 'Az Az Kayıp' seç, Uygula bas. 5 spin at.";
        private const string T3_YONTMA_C =
            "Kazanç hiç gelmedi ya da bahsin altında küçük şey. Oyuncu 'fark etmedim' der. Sessiz sömürü.";

        private const string T3_TUTMA_A =
            "Üçüncü senaryo: KAÇIŞ ENGELLEME (Tutma). Oyuncu çıkmaya niyetlenirse sistem küçük " +
            "kazanç hediyesi verir.";
        private const string T3_TUTMA_B =
            "Oyun Modu'ndan 'Kaçış Engelleme' seç, Uygula bas. 6 spin at.";
        private const string T3_TUTMA_C =
            "Gördün mü? 2 kez kayıp, sonra ÖDEME. Yine 2 kez kayıp, yine ÖDEME. " +
            "Sistem seni tam çıkacağın anda küçük kazançla TUTUYOR. " +
            "Asıl manipülasyon budur — kaybetmeni bekletip, küçük hediye vererek bir spin daha attırır.";

        private const string T3_KORUMA_A =
            "Dördüncü senaryo: BAKİYE TÜKETME (Koruma). Ödeme neredeyse durur — kasa korunur, " +
            "oyuncu son kuruşa kadar kaybeder.";
        private const string T3_KORUMA_B =
            "Oyun Modu'ndan 'Bakiye Tüketme' seç, Uygula bas. 5 spin at.";
        private const string T3_KORUMA_C =
            "Kazanç yok. Oyuncu son kuruşuna kadar kaybediyor. Senaryonun ilk üç adımı bağladı, " +
            "son adım sömürdü. Tükeniş aşaması.";

        private const string T3_NORMAL_A =
            "Son senaryo: NORMAL OYUN. Manipülasyon kapalı, oyun kendi kurallarında akıyor. " +
            "Bunu diğer 4 ile kıyaslayacağız.";
        private const string T3_NORMAL_B =
            "Oyun Modu'ndan 'Normal' seç, Uygula bas. 5 spin at.";
        private const string T3_NORMAL_C =
            "Fark hissettin mi? Diğer 4 senaryo bütün aksiyon kullanıcının dopaminini, parasını, " +
            "beklentilerini kontrol etmek için kurulmuş. Normal oyun = manipülasyonsuz. " +
            "Bu farkındalık temel.";

        // === T4-T11 (mevcut) ===

        private const string T4_BASLANGIC =
            "Olasılık Ayarları bölümü otomatik açıldı. İlk parametre: çarpan ne sıklıkla düşsün.";
        private const string T4_AKSIYON =
            "Çarpan olasılığını %15 yap (default %2). Sonra 3 spin at.";
        private const string T4_KAPANIS =
            "Çarpanlar şimdi çok daha sık görünüyor. Operatör bunu 'oyun eğlenceli' hissi için " +
            "kullanır — ama beyninde 'her an büyük kazanç olabilir' yanılgısı yaratır.";

        private const string T5_BASLANGIC =
            "Şimdi bonus sembolüne bakalım. Yıldız (scatter) sembolü nadir düşer ama operatör " +
            "sıklığını arttırabilir. Slider'ı maxa kadar arttır, ne olacağını gör.";
        private const string T5_AKSIYON =
            "Bonus sembolü olasılığını arttır, Uygula bas. 1 spin at.";
        private const string T5_KAPANIS =
            "Gördün mü? Slider'ı arttırdın → scatter düştü → bonus oyun açıldı. " +
            "Operatör bunu sana 'sürpriz' gibi sunar ama aslında 'şimdi büyük kazanca yakınsın, " +
            "devam et' tuzağıdır.";

        // PAKET 6C2 — T6_YENI_OYUNCU (Hook Fazı toggle)
        private const string T6YO_BASLANGIC =
            "Şimdi operatörün GİZLİ silahını göreceğiz: Yeni Oyuncu Modu.\n\n" +
            "Bu toggle açıkken sistem seni 'yeni gelen' sayar — sana ÖZEL bir rejim uygular: " +
            "bol kazandırma, yumuşak kayıplar. 'Şanslı bir gün' hissi.";
        private const string T6YO_AKSIYON =
            "Önce 3 spin at (toggle KAPALI iken — fark için referans). " +
            "Sonra Manipülasyon Ayarları'nda 'Yeni Oyuncu Modu' toggle'ını AÇ ve 3 spin daha at.";
        public const string T6YO_ARA_MODAL =
            "3 spin attık (toggle kapalı). Sonuç: NET kayıp — normal RTP davranışı.\n\n" +
            "Şimdi Manipülasyon Ayarları'nda 'Yeni Oyuncu Modu' toggle'ını AÇ. " +
            "Ardından 3 spin daha at. Fark net olacak.";
        private const string T6YO_KAPANIS =
            "Gördün mü? Aynı slot, aynı bahis. Toggle KAPALI → NET kayıp, AÇIK → NET kazanç. " +
            "Operatör seni 'yeni' diye işaretler, sistem sana hediye verir, sen 'şanslı bir gün' sanırsın.\n\n" +
            "Bu manipülasyonun adı HOOK FAZI — yeni oyuncu için tasarlanmış kanca.";

        // PAKET 6C3: T6 (Kazandırma) — 5'lik N mantığı, dinamik pattern motor
        private const string T6_BASLANGIC =
            "Şimdi 'Kazandırma Sıklığı'na bakalım. Bu slider 5 spin'in kaçında kullanıcıya " +
            "kazanç verileceğini belirler. Slider'ı ayarla — sen seçeceksin.";
        private const string T6_AKSIYON =
            "Slider'ı kaydır. Slider değeri ÷ 2 = 5'de kaç kazanç. Örneğin slider 6 → 5'de 3 kazanç. " +
            "Uygula bas, 5 spin at.";
        private const string T6_KAPANIS =
            "Slider 5'de N seçtin. 5 spin'in N tanesi kazanç oldu, kalanı kayıp. " +
            "Operatör bunu istediği gibi ayarlar — sen 'şanslı bir gün' veya 'şanssız' sanırsın " +
            "ama her şey ayarlanmıştır.";

        // PAKET 6D: T7 (Ödeme Aralığı) — 2-aşamalı maks 3x vs min 3-maks 5x karşılaştırma
        private const string T7_BASLANGIC =
            "Ödeme aralığı — operatör kazancın TUTAR aralığını sınırlar. " +
            "Bahis × min ile bahis × maks arasında ödeme yapar. Önce maksimumu görelim.";
        private const string T7_AKSIYON =
            "Ödeme MAKS'ı 3'e ayarla (bahis × 3 = 3000 TL tavan). " +
            "Uygula bas, 3 spin at. Kazançlar 0-3000 TL arasında olacak.";
        public const string T7_ARA_MODAL =
            "3 spin attık (maks 3x). Şimdi MIN ve MAKS'ı BİRLİKTE ayarlayalım.\n\n" +
            "Ödeme MIN'i 3, MAKS'ı 5 yap. 3 spin daha at. Bu sefer kazançlar 3000-5000 TL arasına GARANTİ.";
        private const string T7_KAPANIS =
            "İlk 3 spin'de kazanç düşüktü (maks 3x = dar aralık). " +
            "İkinci 3 spin'de kazanç GARANTİ 3-5x oldu (kayıp imkansız). " +
            "Operatör bunu kullanıcıya 'şanslı seri' gibi gösterir — gerçekte algoritma her şeyi kontrol eder.";

        // PAKET 6C3: T8 (Near Miss) — 5'lik N mantığı, dinamik pattern motor (7-sembol layout)
        private const string T8_BASLANGIC =
            "Near miss — 'neredeyse kazanıyordun' hissi. Slot oyununun en güçlü tuzaklarından biri. " +
            "Slider'ı ayarla, kaç near miss istediğini sen seç.";
        private const string T8_AKSIYON =
            "Slider 5'de N near miss demek. 5 spin'in N tanesinde 'tam azıcık eksik' kümeler düşecek. " +
            "Uygula bas, 5 spin at.";
        private const string T8_KAPANIS =
            "Gördün mü? 7 aynı sembol düştü ama 1 EKSİK — cluster 8'den başlıyor. " +
            "Beynin 'KAZANIYORDUM' der, oysa hiç şansın yoktu. " +
            "Bu manipülasyon dopamin pompalar — bağımlılığın temel mekanizması.";

        // PAKET 6D: T9 (Kaçış Frenleme) — 3 kayıp + 1 kazanç deterministik demo
        private const string T9_BASLANGIC =
            "Kaçış Frenleme — kullanıcı kaybedip kaybedip çıkma noktasına geldiğinde operatör NE YAPAR? " +
            "Onu tutmak için otomatik kazanç verir. Sen limit yaz: kaç kayıp sonra otomatik kazanç gelsin.";
        private const string T9_AKSIYON =
            "Kaçış limiti kutusuna 3 yaz. Yani 3 kayıptan sonra sistem otomatik kazanç verecek. " +
            "Uygula bas, 4 spin at.";
        private const string T9_KAPANIS =
            "İlk 3 spin tam kayıp. Tam çıkmak istedin değil mi? Ama 4. spin kazanç geldi — " +
            "sistemin 'frenleme' anı. 'İyi ki kalmışım' dedirten o anlar. " +
            "Bu manipülasyon T3_TUTMA'yla kombine, ama daha PROGRAMLI: limit operatörün elinde.";

        // PAKET 6D: T10 (Çarpan Zorla) — 2-aşamalı açık/kapalı toggle demo
        private const string T10_BASLANGIC =
            "Çarpan Zorla — operatörün son silahı. İstediği anda çarpan düşürür. " +
            "Ama bir tuzak var: 'Çarpan Ödeme' toggle KAPALI iken çarpan düşse de ÖDEME YAPILMAZ. " +
            "Sırayla görelim.";
        private const string T10_AKSIYON =
            "Önce 'Çarpan Ödeme' toggle KAPALI iken çarpan zorla. ×500 butonuna bas. Spinin sonucunu izle.";
        public const string T10_ARA_MODAL =
            "Gördün mü? ×500 çarpan düştü AMA meyve dizilimi cluster oluşturmadı — ödeme YAPILMADI.\n\n" +
            "Şimdi 'Çarpan Ödeme' toggle'ını AÇ ve ×500 butonuna tekrar bas.";
        private const string T10_KAPANIS =
            "Aynı işlem, ama bu sefer ödeme yapan meyve dizilimi + çarpan düştü → MEGA KAZANÇ. " +
            "Operatör bu toggle'ı kullanarak 'şu kullanıcıya bonus vereceğim' der, gerisini ayarlar. " +
            "Manipülasyon %100 kontrol.";

        private const string T11_BASLANGIC =
            "Son silah: bonus oyununu elle tetikleme.";
        private const string T11_AKSIYON =
            "Bonus Tetikle butonuna bas.";
        private const string T11_KAPANIS =
            "Operatör, kullanıcı pes etmek üzereyken bonusu tetikler. " +
            "'Tam çıkıyordum şans yüzüme güldü' der oyuncu. Aslında operatör onu içeride tutmak " +
            "için düğmeye bastı.";

        private const string TSON_BASLANGIC =
            "Gördün mü? 9 parametre, hepsi kullanıcının zamanını, parasını, dopamin döngüsünü " +
            "kontrol için. Slot oyunlarında tesadüf yoktur — sadece tasarım vardır. " +
            "Kumar tesadüf değil, mühendisliktir.\n\n" +
            "Bağımlılık yaşadığını düşünüyorsan veya yakınında biri varsa: " +
            "Yeşilay Danışmanlık Hattı 0850 222 0 191 (ücretsiz, 7/24).\n\n" +
            "Bu farkındalık seninle kalsın.";
    }

    public class AdimVerisi
    {
        public TutorialAdimYoneticisi.TutorialAdimId id;
        public bool aktifMi;
        public string mesajBaslangic;
        public string mesajAksiyon;
        public string mesajKapanis;

        public string altBaslik;
        public string[] yapilacaklar;

        // PAKET 3B-fix-5: T3 grup için ana sayaç + alt sayaç
        public int sira;          // "ADIM #/11" — T3_* için hepsi 3
        public string altSayac;   // "1/5".."5/5" — sadece T3_* için, diğerleri null

        public string[] vurguSelectorlari;
        public int gerekliSpin;
        public Func<bool> parametreKosulu;
        public string[] degisimAnahtarlari;
    }
}
