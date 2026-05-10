using System;
using System.Collections.Generic;
using UnityEngine;

namespace KumarFarkindalik.Tutorial
{
    /// <summary>
    /// Tutorial akış adımlarının state machine'i (T1 - T11 + T_SON).
    /// PAKET 3B-fix-3: Görev Takip Paneli için altBaslik + yapilacaklar alanları + canlı ilerleme
    /// erişimcileri (AdimBaslangicSpin, AdimSirasindaDegistirildi).
    /// </summary>
    public class TutorialAdimYoneticisi : MonoBehaviour
    {
        public enum TutorialAdimId
        {
            T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T_SON
        }

        public TutorialAdimId mevcutAdim = TutorialAdimId.T1;
        public event Action<AdimVerisi> OnAdimDegisti;
        public event Action OnTutorialBitti;

        private readonly Dictionary<TutorialAdimId, AdimVerisi> _adimlar = new();
        private int _adimBaslangicSpin;
        private readonly HashSet<string> _adimSirasindaDegisenler = new();

        public AdimVerisi MevcutAdimVerisi => _adimlar.TryGetValue(mevcutAdim, out var v) ? v : null;

        // PAKET 3B-fix-3: TutorialAdminEnjeksiyonu.Update bu erişimcileri kullanır (canlı ilerleme).
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

        /// <summary>
        /// PAKET 3B-fix-4 (Sorun 2): 04 sahnesinde SenaryoYoneticisi GameObject YOK; bu yüzden
        /// önce TutorialOyunYoneticisi.TutorialSpinSayaci (ButtonCevir click counter), yoksa
        /// SenaryoYoneticisi.toplamSpin (fallback — 04'te null döner zaten).
        /// </summary>
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
                gerekliSpin = 0,
            };

            _adimlar[TutorialAdimId.T3] = new AdimVerisi
            {
                id = TutorialAdimId.T3,
                aktifMi = true,
                mesajBaslangic = T3_BASLANGIC,
                mesajAksiyon = T3_AKSIYON,
                mesajKapanis = T3_KAPANIS,
                altBaslik = "HOOK SENARYOSU",
                yapilacaklar = new[] { "Oyun modunu 'Hook' seç", "Uygula bas", "3 spin at" },
                vurguSelectorlari = new[] { "#oyunModu", "#senaryoUygulaBtn" },
                gerekliSpin = 3,
                parametreKosulu = () => PanelKopru.aktifSenaryo == "hook",
                degisimAnahtarlari = new[] { "oyunModu" },
            };

            _adimlar[TutorialAdimId.T4] = new AdimVerisi
            {
                id = TutorialAdimId.T4,
                aktifMi = true,
                mesajBaslangic = T4_BASLANGIC,
                mesajAksiyon = T4_AKSIYON,
                mesajKapanis = T4_KAPANIS,
                altBaslik = "ÇARPAN OLASILIĞI",
                yapilacaklar = new[] { "Olasılık Ayarları'nı aç", "Çarpan olasılığını %15 yap", "3 spin at" },
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
                yapilacaklar = new[] { "Bonus olasılığını %5 yap", "5 spin at" },
                vurguSelectorlari = new[] { "#bonusSembolOlasilik" },
                gerekliSpin = 5,
                parametreKosulu = () => PanelKopru.bonusOtomatikSpinPeriyodu > 0
                                        && PanelKopru.bonusOtomatikSpinPeriyodu <= 25,
                degisimAnahtarlari = new[] { "bonusOtomatikOran" },
            };

            _adimlar[TutorialAdimId.T6] = new AdimVerisi
            {
                id = TutorialAdimId.T6,
                aktifMi = true,
                mesajBaslangic = T6_BASLANGIC,
                mesajAksiyon = T6_AKSIYON,
                mesajKapanis = T6_KAPANIS,
                altBaslik = "KAZANDIRMA SIKLIĞI",
                yapilacaklar = new[] { "Manipülasyon Ayarları'nı aç", "Kazandırma sıklığını 7 yap", "5 spin at" },
                vurguSelectorlari = new[] { "#kazanmaOrani" },
                gerekliSpin = 5,
                parametreKosulu = () => PanelKopru.kazanmaOrani >= 60f,
                degisimAnahtarlari = new[] { "kazanmaOrani" },
            };

            _adimlar[TutorialAdimId.T7] = new AdimVerisi
            {
                id = TutorialAdimId.T7,
                aktifMi = true,
                mesajBaslangic = T7_BASLANGIC,
                mesajAksiyon = T7_AKSIYON,
                mesajKapanis = T7_KAPANIS,
                altBaslik = "ÖDEME ARALIĞI",
                yapilacaklar = new[] { "Min'i 0.5 yap", "Maks'ı 2 yap", "5 spin at" },
                vurguSelectorlari = new[] { "#minCarpan", "#maksCarpan" },
                gerekliSpin = 5,
                parametreKosulu = () => PanelKopru.minCarpan > 0f
                                        && PanelKopru.minCarpan <= 0.5f
                                        && PanelKopru.maksCarpan > 0f
                                        && PanelKopru.maksCarpan <= 2f,
                degisimAnahtarlari = new[] { "minCarpan", "maksCarpan" },
            };

            _adimlar[TutorialAdimId.T8] = new AdimVerisi
            {
                id = TutorialAdimId.T8,
                aktifMi = true,
                mesajBaslangic = T8_BASLANGIC,
                mesajAksiyon = T8_AKSIYON,
                mesajKapanis = T8_KAPANIS,
                altBaslik = "NEAR MISS",
                yapilacaklar = new[] { "Near miss'i 8 yap", "5 spin at" },
                vurguSelectorlari = new[] { "#yakinKacirma" },
                gerekliSpin = 5,
                parametreKosulu = () => PanelKopru.yakinKacirma >= 70f,
                degisimAnahtarlari = new[] { "yakinKacirma" },
            };

            _adimlar[TutorialAdimId.T9] = new AdimVerisi
            {
                id = TutorialAdimId.T9,
                aktifMi = true,
                mesajBaslangic = T9_BASLANGIC,
                mesajAksiyon = T9_AKSIYON,
                mesajKapanis = T9_KAPANIS,
                altBaslik = "KAÇIŞ FRENLEME",
                yapilacaklar = new[] { "Üst üste kayıp limitini 3 yap", "8 spin at" },
                vurguSelectorlari = new[] { "#ardisikKayip" },
                gerekliSpin = 8,
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
                altBaslik = "ÇARPAN ZORLA",
                yapilacaklar = new[] { "Anlık Müdahale'yi aç", "×500 butonuna bas", "1 spin at" },
                vurguSelectorlari = new[] { "button[onclick=\"carpanZorla(500)\"]" },
                gerekliSpin = 1,
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
                vurguSelectorlari = new[] { ".trigger-btn" },
                gerekliSpin = 0,
                parametreKosulu = () => TutorialAdminEnjeksiyonu.BonusTetiklendi,
                degisimAnahtarlari = new[] { "bonusTetikle" },
            };

            _adimlar[TutorialAdimId.T_SON] = new AdimVerisi
            {
                id = TutorialAdimId.T_SON,
                aktifMi = false,
                mesajBaslangic = TSON_BASLANGIC,
                altBaslik = "TAMAMLANDI",
                yapilacaklar = null,
                gerekliSpin = 0,
            };
        }

        // === Adım metinleri (A=başlangıç, B=aksiyon, C=kapanış) ===

        private const string T2_BASLANGIC =
            "Hoş geldin. Az önce yaşadığın manipülasyon bu panelde kuruluyor. " +
            "Üç bölüm var — Olasılık, Manipülasyon ve Anlık Müdahale. Birlikte hepsini göreceğiz.";

        private const string T3_BASLANGIC =
            "Şimdi Oyun Modu seçimini öğreneceğiz. Panelde 5 hazır senaryo var. " +
            "Her biri farklı bir manipülasyon stratejisi.";
        private const string T3_AKSIYON =
            "Şimdi Hook seç ve Uygula bas. Sonra 3 spin at, neler hissettiğine dikkat et.";
        private const string T3_KAPANIS =
            "Gördün mü? Hook senaryosunda kazandırma yüksek, kayıplar yumuşak. " +
            "Yeni oyuncuyu 'şanslıyım, devam edeyim' hissine sokmak için. Bağımlılığın ilk adımı.";

        private const string T4_BASLANGIC =
            "Şimdi Olasılık Ayarları'na giriyoruz. İlk parametre: çarpan ne sıklıkla düşsün.";
        private const string T4_AKSIYON =
            "Çarpan olasılığını %15 yap (default %2). Sonra 3 spin at.";
        private const string T4_KAPANIS =
            "Çarpanlar şimdi çok daha sık görünüyor. Operatör bunu 'oyun eğlenceli' hissi için " +
            "kullanır — ama beyninde 'her an büyük kazanç olabilir' yanılgısı yaratır.";

        private const string T5_BASLANGIC =
            "Bonus oyunu — slot oyunlarının en bağımlılık yapan parçası.";
        private const string T5_AKSIYON =
            "Bonus olasılığını %5'e çıkar (default %0.5). Sonra 5 spin at.";
        private const string T5_KAPANIS =
            "Bonus daha sık tetikleniyor şimdi. Oyuncu 'her an büyük bonus gelebilir' diye " +
            "oyunu bırakamaz. Beklenti = bağımlılık.";

        private const string T6_BASLANGIC =
            "Manipülasyon Ayarları başladı. En kritik parametre: kazandırma sıklığı.";
        private const string T6_AKSIYON =
            "Kazandırma sıklığını 7'ye çıkar (default 3). 5 spin at.";
        private const string T6_KAPANIS =
            "10 spinden 7'sinde 'kazandım' hissi. Aslında küçük kazançlar bahsin altında — " +
            "net kayıptasın ama beyne kazanç sinyali gidiyor. Klasik psikolojik tuzak.";

        private const string T7_BASLANGIC =
            "Şimdi ödeme aralığı. Kullanıcı kazandığında ne kadar ödenecek, sen belirleyeceksin.";
        private const string T7_AKSIYON =
            "Min'i 0.5, Maks'ı 2 yap. 5 spin at.";
        private const string T7_KAPANIS =
            "Kullanıcı kazansa bile bahsinin biraz üstünde ödeme alıyor. Görünmez sömürü — " +
            "'kazandım' diyor ama gerçekte bahsi karşılamıyor bile.";

        private const string T8_BASLANGIC =
            "Bağımlılık biliminin en güçlü kavramı: near miss — neredeyse oluyordu hissi.";
        private const string T8_AKSIYON =
            "Near miss'i 8'e çıkar (default 4). 5 spin at.";
        private const string T8_KAPANIS =
            "Beynin 'çok yaklaşmıştım, bir sonrakinde olacak' diyor. Aslında hiç kazanmadın " +
            "ama dopamin salgılandı. Slot tasarımının en sinsi parçası.";

        private const string T9_BASLANGIC =
            "Oyuncu çıkmak istediğinde sistem bunu bilir ve müdahale eder.";
        private const string T9_AKSIYON =
            "Üst üste kayıp limitini 3 yap (default 8). 8 spin at.";
        private const string T9_KAPANIS =
            "3 kayıptan sonra sistem küçük kazanç verdi — kullanıcı 'çıkmayayım, şans dönüyor' " +
            "diye kaldı. Pes etme noktası tasarlanmış. Sömürünün son aşaması.";

        private const string T10_BASLANGIC =
            "Anlık Müdahale — operatörün gerçek zamanlı eli. Şimdi en güçlü silahını göreceksin.";
        private const string T10_AKSIYON =
            "×500 butonuna bas, sonra spin at.";
        private const string T10_KAPANIS =
            "Az önce 'şanslı an' tasarlandı. Operatör kasada otururken istediği anda istediği " +
            "büyüklükte çarpan düşürebilir. 'İnanılmaz kazanç' tesadüf değil — düğmeye basıldı.";

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

    /// <summary>Bir tutorial adımının tüm verileri (3 modal mesajı + altBaslik + yapilacaklar + vurgu + spin + parametre).</summary>
    public class AdimVerisi
    {
        public TutorialAdimYoneticisi.TutorialAdimId id;
        public bool aktifMi;
        public string mesajBaslangic;
        public string mesajAksiyon;
        public string mesajKapanis;

        // PAKET 3B-fix-3: Görev Takip Paneli için
        public string altBaslik;
        public string[] yapilacaklar;

        public string[] vurguSelectorlari;
        public int gerekliSpin;
        public Func<bool> parametreKosulu;
        public string[] degisimAnahtarlari;
    }
}
