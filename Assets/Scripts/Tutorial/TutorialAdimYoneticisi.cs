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
            "Az önce yaşadığın <color=#F24D40>manipülasyon</color> işte bu panelde kuruluyor. " +
            "Üç bölüm göreceğiz: <color=#5BA0FF>Olasılık</color>, <color=#5BA0FF>Manipülasyon</color> ve <color=#5BA0FF>Anlık Müdahale</color>. Hadi başlayalım.";

        // === T3 — 5 senaryo ===

        private const string T3_HOOK_A =
            "<color=#FFD933>5 oyun modu</color> var. Sırayla her birini deneyip aralarındaki farkı göreceğiz.\n\n" +
            "İlk senaryo: <color=#F24D40>TAZE KAN (Hook)</color>. Yeni gelen oyuncu için tasarlandı — " +
            "<color=#4DCC59>bol kazandırma</color>, <color=#F24D40>yumuşak kayıplar</color>, <color=#FFD933>'şanslıyım'</color> hissi.";
        private const string T3_HOOK_B =
            "<color=#5BA0FF>Oyun Modu</color>'ndan <color=#5BA0FF>'Taze Kan'</color> seç, <color=#5BA0FF>Uygula</color> bas. <color=#FFD933>5 spin</color> at.";
        private const string T3_HOOK_C =
            "Gördün mü? Yeni oyuncu hemen <color=#4DCC59>kazandı</color>, oyuna bağlandı. <color=#F24D40>Sömürünün başlangıcı</color> — <color=#F24D40>kanca</color>.";

        private const string T3_YONTMA_A =
            "İkinci senaryo: <color=#F24D40>AZ AZ KAYIP (Yontma)</color>. Oyuncu farkına varmadan, küçük küçük <color=#F24D40>kaybettirme</color>. " +
            "Bakiye <color=#F24D40>sessizce erir</color>.";
        private const string T3_YONTMA_B =
            "<color=#5BA0FF>Oyun Modu</color>'ndan <color=#5BA0FF>'Az Az Kayıp'</color> seç, <color=#5BA0FF>Uygula</color> bas. <color=#FFD933>5 spin</color> at.";
        private const string T3_YONTMA_C =
            "<color=#F24D40>Kazanç hiç gelmedi</color> ya da bahsin altında küçük şey. Oyuncu 'fark etmedim' der. <color=#F24D40>Sessiz sömürü</color>.";

        private const string T3_TUTMA_A =
            "Üçüncü senaryo: <color=#F24D40>KAÇIŞ ENGELLEME (Tutma)</color>. Oyuncu çıkmaya niyetlenirse sistem küçük " +
            "<color=#4DCC59>kazanç hediyesi</color> verir.";
        private const string T3_TUTMA_B =
            "<color=#5BA0FF>Oyun Modu</color>'ndan <color=#5BA0FF>'Kaçış Engelleme'</color> seç, <color=#5BA0FF>Uygula</color> bas. <color=#FFD933>6 spin</color> at.";
        private const string T3_TUTMA_C =
            "Gördün mü? <color=#FFD933>2 kez kayıp</color>, sonra <color=#4DCC59>ÖDEME</color>. Yine <color=#FFD933>2 kez kayıp</color>, yine <color=#4DCC59>ÖDEME</color>. " +
            "Sistem seni tam çıkacağın anda küçük kazançla <color=#F24D40>TUTUYOR</color>. " +
            "<color=#F24D40>Asıl manipülasyon budur</color> — <color=#F24D40>kaybetmeni bekletip</color>, küçük hediye vererek bir spin daha attırır.";

        private const string T3_KORUMA_A =
            "Dördüncü senaryo: <color=#F24D40>BAKİYE TÜKETME (Koruma)</color>. <color=#F24D40>Ödeme neredeyse durur</color> — kasa korunur, " +
            "oyuncu <color=#F24D40>son kuruşa kadar kaybeder</color>.";
        private const string T3_KORUMA_B =
            "<color=#5BA0FF>Oyun Modu</color>'ndan <color=#5BA0FF>'Bakiye Tüketme'</color> seç, <color=#5BA0FF>Uygula</color> bas. <color=#FFD933>5 spin</color> at.";
        private const string T3_KORUMA_C =
            "<color=#F24D40>Kazanç yok</color>. Oyuncu <color=#F24D40>son kuruşuna kadar kaybediyor</color>. Senaryonun ilk üç adımı bağladı, " +
            "son adım sömürdü. <color=#F24D40>Tükeniş aşaması</color>.";

        private const string T3_NORMAL_A =
            "Son senaryo: <color=#4DCC59>NORMAL OYUN</color>. <color=#F24D40>Manipülasyon kapalı</color>, oyun kendi kurallarında akıyor. " +
            "Bunu diğer 4 ile kıyaslayacağız.";
        private const string T3_NORMAL_B =
            "<color=#5BA0FF>Oyun Modu</color>'ndan <color=#5BA0FF>'Normal'</color> seç, <color=#5BA0FF>Uygula</color> bas. <color=#FFD933>5 spin</color> at.";
        private const string T3_NORMAL_C =
            "Fark hissettin mi? Diğer 4 senaryo bütün aksiyon kullanıcının <color=#F24D40>dopaminini</color>, parasını, " +
            "beklentilerini <color=#F24D40>kontrol etmek</color> için kurulmuş. <color=#4DCC59>Normal oyun = manipülasyonsuz</color>. " +
            "<color=#4DCC59>Bu farkındalık temel</color>.";

        // === T4-T11 (mevcut) ===

        private const string T4_BASLANGIC =
            "<color=#5BA0FF>Olasılık Ayarları</color> bölümü otomatik açıldı. İlk parametre: <color=#5BA0FF>çarpan</color> ne sıklıkla düşsün.";
        private const string T4_AKSIYON =
            "<color=#5BA0FF>Çarpan olasılığını</color> <color=#FFD933>%15</color> yap (default <color=#FFD933>%2</color>). Sonra <color=#FFD933>3 spin</color> at.";
        private const string T4_KAPANIS =
            "Çarpanlar şimdi çok daha <color=#FFD933>sık</color> görünüyor. <color=#F24D40>Operatör</color> bunu 'oyun eğlenceli' hissi için " +
            "kullanır — ama beyninde <color=#F24D40>'her an büyük kazanç olabilir' yanılgısı</color> yaratır.";

        private const string T5_BASLANGIC =
            "Şimdi <color=#5BA0FF>bonus sembolüne</color> bakalım. <color=#FFD933>Yıldız (scatter)</color> sembolü nadir düşer ama <color=#F24D40>operatör</color> " +
            "sıklığını arttırabilir. Slider'ı <color=#FFD933>maxa</color> kadar arttır, ne olacağını gör.";
        private const string T5_AKSIYON =
            "<color=#5BA0FF>Bonus sembolü olasılığını</color> arttır, <color=#5BA0FF>Uygula</color> bas. <color=#FFD933>1 spin</color> at.";
        private const string T5_KAPANIS =
            "Gördün mü? Slider'ı arttırdın → <color=#FFD933>scatter</color> düştü → <color=#4DCC59>bonus oyun açıldı</color>. " +
            "<color=#F24D40>Operatör</color> bunu sana 'sürpriz' gibi sunar ama aslında <color=#F24D40>'şimdi büyük kazanca yakınsın, " +
            "devam et' tuzağıdır</color>.";

        // PAKET 6C2 — T6_YENI_OYUNCU (Hook Fazı toggle)
        private const string T6YO_BASLANGIC =
            "Şimdi <color=#F24D40>operatörün GİZLİ silahını</color> göreceğiz: <color=#5BA0FF>Yeni Oyuncu Modu</color>.\n\n" +
            "Bu toggle açıkken sistem seni 'yeni gelen' sayar — sana ÖZEL bir rejim uygular: " +
            "<color=#4DCC59>bol kazandırma</color>, <color=#F24D40>yumuşak kayıplar</color>. <color=#FFD933>'Şanslı bir gün'</color> hissi.";
        private const string T6YO_AKSIYON =
            "Önce <color=#FFD933>3 spin</color> at (toggle <color=#F24D40>KAPALI</color> iken — fark için referans). " +
            "Sonra <color=#5BA0FF>Manipülasyon Ayarları</color>'nda <color=#5BA0FF>'Yeni Oyuncu Modu'</color> toggle'ını AÇ ve <color=#FFD933>3 spin</color> daha at.";
        public const string T6YO_ARA_MODAL =
            "<color=#FFD933>3 spin</color> attık (toggle kapalı). Sonuç: <color=#F24D40>NET kayıp</color> — normal RTP davranışı.\n\n" +
            "Şimdi <color=#5BA0FF>Manipülasyon Ayarları</color>'nda <color=#5BA0FF>'Yeni Oyuncu Modu'</color> toggle'ını AÇ. " +
            "Ardından <color=#FFD933>3 spin</color> daha at. <color=#4DCC59>Fark net olacak</color>.";
        private const string T6YO_KAPANIS =
            "Gördün mü? Aynı slot, aynı bahis. <color=#5BA0FF>Toggle KAPALI</color> → <color=#F24D40>NET kayıp</color>, <color=#5BA0FF>AÇIK</color> → <color=#4DCC59>NET kazanç</color>. " +
            "<color=#F24D40>Operatör</color> seni 'yeni' diye işaretler, sistem sana <color=#4DCC59>hediye verir</color>, sen <color=#FFD933>'şanslı bir gün'</color> sanırsın.\n\n" +
            "Bu <color=#F24D40>manipülasyonun</color> adı <color=#F24D40>HOOK FAZI</color> — yeni oyuncu için tasarlanmış <color=#F24D40>kanca</color>.";

        // PAKET 6C3: T6 (Kazandırma) — 5'lik N mantığı, dinamik pattern motor
        private const string T6_BASLANGIC =
            "Şimdi <color=#5BA0FF>'Kazandırma Sıklığı'</color>'na bakalım. Bu slider <color=#FFD933>5 spin</color>'in kaçında kullanıcıya " +
            "<color=#4DCC59>kazanç</color> verileceğini belirler. Slider'ı ayarla — <color=#4DCC59>sen seçeceksin</color>.";
        private const string T6_AKSIYON =
            "Slider'ı kaydır. Slider değeri ÷ <color=#FFD933>2</color> = <color=#FFD933>5'de kaç kazanç</color>. Örneğin slider <color=#FFD933>6</color> → <color=#FFD933>5'de 3 kazanç</color>. " +
            "<color=#5BA0FF>Uygula</color> bas, <color=#FFD933>5 spin</color> at.";
        private const string T6_KAPANIS =
            "Slider <color=#FFD933>5'de N</color> seçtin. <color=#FFD933>5 spin</color>'in <color=#FFD933>N tanesi</color> <color=#4DCC59>kazanç</color> oldu, kalanı <color=#F24D40>kayıp</color>. " +
            "<color=#F24D40>Operatör</color> bunu istediği gibi ayarlar — sen 'şanslı bir gün' veya 'şanssız' sanırsın " +
            "ama <color=#F24D40>her şey ayarlanmıştır</color>.";

        // PAKET 6D: T7 (Ödeme Aralığı) — 2-aşamalı maks 3x vs min 3-maks 5x karşılaştırma
        private const string T7_BASLANGIC =
            "Ödeme aralığı — <color=#F24D40>operatör</color> kazancın <color=#FFD933>TUTAR</color> aralığını sınırlar. " +
            "Bahis × <color=#5BA0FF>min</color> ile bahis × <color=#5BA0FF>maks</color> arasında ödeme yapar. Önce maksimumu görelim.";
        private const string T7_AKSIYON =
            "<color=#5BA0FF>Ödeme MAKS</color>'ı <color=#FFD933>3</color>'e ayarla (bahis × <color=#FFD933>3</color> = <color=#FFD933>3000 TL</color> tavan). " +
            "Uygula bas, <color=#FFD933>3 spin</color> at. Kazançlar <color=#FFD933>0-3000 TL</color> arasında olacak.";
        public const string T7_ARA_MODAL =
            "<color=#FFD933>3 spin</color> attık (maks <color=#FFD933>3x</color>). Şimdi <color=#5BA0FF>MIN</color> ve <color=#5BA0FF>MAKS</color>'ı BİRLİKTE ayarlayalım.\n\n" +
            "<color=#5BA0FF>Ödeme MIN</color>'i <color=#FFD933>3</color>, <color=#5BA0FF>MAKS</color>'ı <color=#FFD933>5</color> yap. <color=#FFD933>3 spin</color> daha at. Bu sefer kazançlar <color=#FFD933>3000-5000 TL</color> arasına <color=#4DCC59>GARANTİ</color>.";
        private const string T7_KAPANIS =
            "İlk <color=#FFD933>3 spin</color>'de kazanç düşüktü (maks <color=#FFD933>3x</color> = dar aralık). " +
            "İkinci <color=#FFD933>3 spin</color>'de kazanç <color=#4DCC59>GARANTİ</color> <color=#FFD933>3-5x</color> oldu (<color=#F24D40>kayıp imkansız</color>). " +
            "<color=#F24D40>Operatör</color> bunu kullanıcıya 'şanslı seri' gibi gösterir — gerçekte <color=#F24D40>algoritma her şeyi kontrol eder</color>.";

        // PAKET 6C3: T8 (Near Miss) — 5'lik N mantığı, dinamik pattern motor (7-sembol layout)
        private const string T8_BASLANGIC =
            "<color=#5BA0FF>Near miss</color> — <color=#F24D40>'neredeyse kazanıyordun' hissi</color>. Slot oyununun en güçlü <color=#F24D40>tuzaklarından</color> biri. " +
            "Slider'ı ayarla, kaç near miss istediğini sen seç.";
        private const string T8_AKSIYON =
            "Slider <color=#FFD933>5'de N</color> near miss demek. <color=#FFD933>5 spin</color>'in N tanesinde <color=#F24D40>'tam azıcık eksik'</color> kümeler düşecek. " +
            "<color=#5BA0FF>Uygula</color> bas, <color=#FFD933>5 spin</color> at.";
        private const string T8_KAPANIS =
            "Gördün mü? <color=#FFD933>7 aynı sembol</color> düştü ama <color=#F24D40>1 EKSİK</color> — cluster <color=#FFD933>8</color>'den başlıyor. " +
            "Beynin <color=#F24D40>'KAZANIYORDUM'</color> der, oysa hiç şansın yoktu. " +
            "Bu <color=#F24D40>manipülasyon dopamin pompalar</color> — <color=#F24D40>bağımlılığın temel mekanizması</color>.";

        // PAKET 6D: T9 (Kaçış Frenleme) — 3 kayıp + 1 kazanç deterministik demo
        private const string T9_BASLANGIC =
            "Kaçış Frenleme — kullanıcı kaybedip kaybedip çıkma noktasına geldiğinde operatör NE YAPAR? " +
            "Onu tutmak için otomatik kazanç verir. Sen limit yaz: kaç kayıp sonra otomatik kazanç gelsin.";
        private const string T9_AKSIYON =
            "Kaçış limiti kutusuna 3 yaz. Yani 3 kayıptan sonra sistem otomatik kazanç verecek. " +
            "Uygula bas, 4 spin at.";
        private const string T9_KAPANIS =
            "İlk <color=#FFD933>3 spin</color> tam <color=#F24D40>kayıp</color>. Tam çıkmak istedin değil mi? Ama <color=#FFD933>4. spin</color> <color=#4DCC59>kazanç</color> geldi — " +
            "sistemin <color=#F24D40>'frenleme' anı</color>. 'İyi ki kalmışım' dedirten o anlar. " +
            "Bu <color=#F24D40>manipülasyon</color> T3_TUTMA'yla kombine, ama daha <color=#F24D40>PROGRAMLI</color>: limit <color=#F24D40>operatörün elinde</color>.";

        // PAKET 6D: T10 (Çarpan Zorla) — 2-aşamalı açık/kapalı toggle demo
        private const string T10_BASLANGIC =
            "<color=#5BA0FF>Çarpan Zorla</color> — <color=#F24D40>operatörün son silahı</color>. İstediği anda çarpan düşürür. " +
            "Ama bir <color=#F24D40>tuzak</color> var: <color=#5BA0FF>'Çarpan Ödeme'</color> toggle <color=#F24D40>KAPALI</color> iken çarpan düşse de <color=#F24D40>ÖDEME YAPILMAZ</color>. " +
            "Sırayla görelim.";
        private const string T10_AKSIYON =
            "Önce <color=#5BA0FF>'Çarpan Ödeme'</color> toggle <color=#F24D40>KAPALI</color> iken çarpan zorla. <color=#FFD933>×500</color> butonuna bas. Spinin sonucunu izle.";
        public const string T10_ARA_MODAL =
            "Gördün mü? <color=#FFD933>×500</color> çarpan düştü AMA meyve dizilimi cluster oluşturmadı — <color=#F24D40>ödeme YAPILMADI</color>.\n\n" +
            "Şimdi <color=#5BA0FF>'Çarpan Ödeme'</color> toggle'ını <color=#4DCC59>AÇ</color> ve <color=#FFD933>×500</color> butonuna tekrar bas.";
        private const string T10_KAPANIS =
            "Aynı işlem, ama bu sefer ödeme yapan meyve dizilimi + çarpan düştü → <color=#4DCC59>MEGA KAZANÇ</color>. " +
            "<color=#F24D40>Operatör</color> bu toggle'ı kullanarak 'şu kullanıcıya bonus vereceğim' der, gerisini ayarlar. " +
            "<color=#F24D40>Manipülasyon %100 kontrol</color>.";

        private const string T11_BASLANGIC =
            "Son silah: bonus oyununu elle tetikleme.";
        private const string T11_AKSIYON =
            "Bonus Tetikle butonuna bas.";
        private const string T11_KAPANIS =
            "<color=#F24D40>Operatör</color>, kullanıcı pes etmek üzereyken <color=#5BA0FF>bonusu tetikler</color>. " +
            "'Tam çıkıyordum şans yüzüme güldü' der oyuncu. Aslında <color=#F24D40>operatör onu içeride tutmak</color> " +
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
