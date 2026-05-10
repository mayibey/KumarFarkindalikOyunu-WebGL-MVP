using System;
using System.Collections.Generic;
using UnityEngine;

namespace KumarFarkindalik.Tutorial
{
    /// <summary>
    /// Tutorial akış adımlarının state machine'i (T1 - T11 + T_SON).
    ///
    /// T1 (PASIF): AyarlarButton glow — TutorialOyunYoneticisi'nin başlangıçtaki davranışı.
    ///             Bu state machine'in DIŞINDA; kullanıcı panel.html'i açana kadar süren ön akış.
    /// T2 (PASIF): Tanıtım modal'ı.
    /// T3-T11 (AKTIF): Modal + panel.html parametre vurgu + spin sayım. Her adımın koşulu sağlandığında İLERİ aktif.
    /// T_SON (PASIF): Kapanış mesajı + 01_GirisScene'e dönüş.
    /// </summary>
    public class TutorialAdimYoneticisi : MonoBehaviour
    {
        public enum TutorialAdimId
        {
            T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T_SON
        }

        public TutorialAdimId mevcutAdim = TutorialAdimId.T1;

        /// <summary>Adım değiştiğinde tetiklenir — TutorialOyunYoneticisi modal+vurgu+UI yönetir.</summary>
        public event Action<AdimVerisi> OnAdimDegisti;

        /// <summary>T_SON'dan sonra İLERİ tıklanırsa — kapanış akışı.</summary>
        public event Action OnTutorialBitti;

        private readonly Dictionary<TutorialAdimId, AdimVerisi> _adimlar = new();
        private int _adimBaslangicSpin;

        // Bug 2 düzeltme: bu adım girdikten sonra panel.html'den DEĞİŞTİRİLEN parametre key'leri.
        // panelHazir/MevcutAyarlariGonder ile gelen default değerler (örn ardisikKayipLimiti=0) bu set'te yok,
        // dolayısıyla "değer ≤ 4" gibi koşullar ön-tetiklenmez.
        private readonly HashSet<string> _adimSirasindaDegisenler = new();

        public AdimVerisi MevcutAdimVerisi => _adimlar.TryGetValue(mevcutAdim, out var v) ? v : null;

        private void Awake()
        {
            AdimlariDoldur();
        }

        /// <summary>TutorialOyunYoneticisi panel açıldıktan sonra T2'ye gel demek için çağırır.</summary>
        public void AdimGec(TutorialAdimId yeni)
        {
            if (!_adimlar.ContainsKey(yeni))
            {
                Debug.LogError($"[TutorialAdimYoneticisi] Adım bulunamadı: {yeni}");
                return;
            }
            mevcutAdim = yeni;
            _adimBaslangicSpin = MevcutSpinAl();
            _adimSirasindaDegisenler.Clear(); // YENİ — değiştirilen key'leri sıfırla
            Debug.Log($"[TutorialAdimYoneticisi] Adım geçti: {yeni} (başlangıç spin={_adimBaslangicSpin})");
            OnAdimDegisti?.Invoke(_adimlar[yeni]);
        }

        /// <summary>TutorialAdimGoster İLERİ butonundan çağrılır.</summary>
        public void IleriTiklandi()
        {
            if (mevcutAdim == TutorialAdimId.T_SON)
            {
                Debug.Log("[TutorialAdimYoneticisi] T_SON İLERİ → OnTutorialBitti");
                OnTutorialBitti?.Invoke();
                return;
            }
            // Sıradaki adıma geç
            int sonraki = (int)mevcutAdim + 1;
            if (sonraki > (int)TutorialAdimId.T_SON) return;
            AdimGec((TutorialAdimId)sonraki);
        }

        /// <summary>TutorialAdminEnjeksiyonu.AyarDegisti event handler'dan çağrılır — bu adımda
        /// hangi panel.html key'i değişti haberi.</summary>
        public void AyarDegistiHaber(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            _adimSirasindaDegisenler.Add(key);
        }

        /// <summary>TutorialAdminEnjeksiyonu Update'te polling ile çağırır.</summary>
        public bool KosulSagla(int mevcutSpin)
        {
            if (!_adimlar.TryGetValue(mevcutAdim, out var v)) return true;
            if (!v.aktifMi) return true;

            // Bug 2 düzeltme: bu adımda DEĞİŞMESİ GEREKEN tüm anahtarlar değişmiş olmalı
            // (panelHazir default değerleri koşulu ön-tetiklemesin).
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

        private static int MevcutSpinAl()
        {
            // Global spin sayacı — OyunYoneticisi.Bonus.cs:85 ve .Simulasyon.cs:533'te kullanım.
            return SenaryoYoneticisi.I != null ? SenaryoYoneticisi.I.toplamSpin : 0;
        }

        // === ADIM TANIMLARI ===
        // PanelKopru public static field'ları (line 44-53):
        //   kazanmaOrani (0-100), minCarpan, maksCarpan, yakinKacirma (0-100),
        //   ardisikKayipLimiti (int), yeniOyuncuModu (bool), carpanTumbleAktif (bool),
        //   bonusModu (string), bonusOtomatikSpinPeriyodu (int), aktifSenaryo (string)
        //
        // Ek alan: carpanOlasilik (PanelKopru.AyariIsle case "carpanOlasilik")
        //   PanelKopru'nun public field'ı YOK → AyariIsle değerini tutmaz. Geçici çözüm:
        //   panel.html bu key'i gönderdiğinde TutorialAdminEnjeksiyonu OnAyarDegisti event'inde
        //   yakalayıp lokal state olarak saklar. Aşağıdaki Kosul lambda'ları ise
        //   TutorialAdminEnjeksiyonu.SonCarpanOlasilik vb. erişir.

        private void AdimlariDoldur()
        {
            _adimlar[TutorialAdimId.T2] = new AdimVerisi
            {
                id = TutorialAdimId.T2,
                aktifMi = false,
                mesaj = MESAJ_T2,
                vurguSelectorlari = null,
                gerekliSpin = 0,
                parametreKosulu = null,
            };

            _adimlar[TutorialAdimId.T3] = new AdimVerisi
            {
                id = TutorialAdimId.T3,
                aktifMi = true,
                mesaj = MESAJ_T3,
                vurguSelectorlari = new[] { "#oyunModu", "#senaryoUygulaBtn" },
                gerekliSpin = 3,
                parametreKosulu = () => PanelKopru.aktifSenaryo == "hook",
                degisimAnahtarlari = new[] { "oyunModu" },
            };

            _adimlar[TutorialAdimId.T4] = new AdimVerisi
            {
                id = TutorialAdimId.T4,
                aktifMi = true,
                mesaj = MESAJ_T4,
                vurguSelectorlari = new[] { "#carpanOlasilik", "#carpanOlasilikInput" },
                gerekliSpin = 3,
                parametreKosulu = () => TutorialAdminEnjeksiyonu.SonCarpanOlasilik >= 10f,
                degisimAnahtarlari = new[] { "carpanOlasilik" },
            };

            _adimlar[TutorialAdimId.T5] = new AdimVerisi
            {
                id = TutorialAdimId.T5,
                aktifMi = true,
                mesaj = MESAJ_T5,
                vurguSelectorlari = new[] { "#bonusSembolOlasilik" },
                gerekliSpin = 5,
                // panel.html dönüşüm: bonusSembolOlasilik (0-5%) → bonusOtomatikOran (spin sayısı).
                // %5 → 100/5 = 20 spin. Eşik %4 → 100/4 = 25 spin'den AZ (sıkı) olmalı.
                parametreKosulu = () => PanelKopru.bonusOtomatikSpinPeriyodu > 0
                                        && PanelKopru.bonusOtomatikSpinPeriyodu <= 25,
                degisimAnahtarlari = new[] { "bonusOtomatikOran" },
            };

            _adimlar[TutorialAdimId.T6] = new AdimVerisi
            {
                id = TutorialAdimId.T6,
                aktifMi = true,
                mesaj = MESAJ_T6,
                vurguSelectorlari = new[] { "#kazanmaOrani" },
                gerekliSpin = 5,
                // panel.html ×10 dönüşüm: 10'da 6 → 60
                parametreKosulu = () => PanelKopru.kazanmaOrani >= 60f,
                degisimAnahtarlari = new[] { "kazanmaOrani" },
            };

            _adimlar[TutorialAdimId.T7] = new AdimVerisi
            {
                id = TutorialAdimId.T7,
                aktifMi = true,
                mesaj = MESAJ_T7,
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
                mesaj = MESAJ_T8,
                vurguSelectorlari = new[] { "#yakinKacirma" },
                gerekliSpin = 5,
                // panel.html ×10 dönüşüm: 10'da 7 → 70
                parametreKosulu = () => PanelKopru.yakinKacirma >= 70f,
                degisimAnahtarlari = new[] { "yakinKacirma" },
            };

            _adimlar[TutorialAdimId.T9] = new AdimVerisi
            {
                id = TutorialAdimId.T9,
                aktifMi = true,
                mesaj = MESAJ_T9,
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
                mesaj = MESAJ_T10,
                vurguSelectorlari = new[] { "button[onclick=\"carpanZorla(500)\"]" },
                gerekliSpin = 1,
                parametreKosulu = () => TutorialAdminEnjeksiyonu.SonCarpanZorla == 500,
                degisimAnahtarlari = new[] { "carpanZorla" },
            };

            _adimlar[TutorialAdimId.T11] = new AdimVerisi
            {
                id = TutorialAdimId.T11,
                aktifMi = true,
                mesaj = MESAJ_T11,
                vurguSelectorlari = new[] { ".trigger-btn" },
                gerekliSpin = 0,
                parametreKosulu = () => TutorialAdminEnjeksiyonu.BonusTetiklendi,
                degisimAnahtarlari = new[] { "bonusTetikle" },
            };

            _adimlar[TutorialAdimId.T_SON] = new AdimVerisi
            {
                id = TutorialAdimId.T_SON,
                aktifMi = false,
                mesaj = MESAJ_TSON,
                vurguSelectorlari = null,
                gerekliSpin = 0,
                parametreKosulu = null,
            };
        }

        // === Adım metinleri (büyük string'ler, ayrı sabitlerde) ===

        private const string MESAJ_T2 =
            "Hoş geldin. Az önce gördüğün manipülasyon kuruluyor: bu panelde. " +
            "Üç bölüm var — Olasılık, Manipülasyon ve Anlık Müdahale. Birlikte inceleyeceğiz.";

        private const string MESAJ_T3 =
            "İlk seçim Oyun Modu. 5 hazır senaryo var. Hook = yeni oyuncu çekme. " +
            "Yontma = az az kaybettirme. Tutma = kaçışı engelleme. " +
            "Şimdi Hook seç ve Uygula bas. Sonra 3 spin at.";

        private const string MESAJ_T4 =
            "Olasılık Ayarları'nı aç. Çarpan düşme olasılığı default %2. Sen %15 yap. " +
            "Çarpanlar artık çok daha sık görünecek. 3 spin at, fark hisset.";

        private const string MESAJ_T5 =
            "Bonus sembolü düşme olasılığı. Default %0.5 = her 200 spinde 1 bonus. " +
            "Sen %5 yap = her 20 spinde 1. 5 spin at.";

        private const string MESAJ_T6 =
            "Manipülasyon Ayarları'nı aç. En kritik parametre: kazandırma sıklığı. " +
            "10 spinde kaç tanesinde 'kazandım' hissi olsun? Default 3. " +
            "Sen 7 yap, 5 spin at. Sürekli küçük kazançlar kullanıcıyı bağlar.";

        private const string MESAJ_T7 =
            "Ödeme aralığı: kazanç bahsin kaç katı olsun. Min=0.5, Maks=2 yap. " +
            "5 spin at. Kullanıcı kazansa bile bahsin biraz üstünde — görünmez sömürü.";

        private const string MESAJ_T8 =
            "Near miss — neredeyse oluyordu hissi. 10 oyundan kaçında 'çok yaklaşmıştım' " +
            "hissi yaşansın? Default 4, sen 8 yap, 5 spin at. " +
            "Bu, bağımlılığın en güçlü tetikleyicisi.";

        private const string MESAJ_T9 =
            "Kaçış Frenleme. Üst üste kaç kayıptan sonra zorunlu küçük kazanç verilsin? " +
            "Default 8, sen 3 yap. 8 spin at. Kullanıcı oyundan ayrılmak istediği anı sistem bilir.";

        private const string MESAJ_T10 =
            "Anlık Müdahale'ye geç. Operatör gerçek zamanlı müdahale eder. " +
            "Çarpan zorla — ×500 bas, sonra spin at. 'Şanslı an' tasarlanmış andır.";

        private const string MESAJ_T11 =
            "Bonus tetikle. Bonusu da elle başlatabilirsin. Stratejik bir an " +
            "— kullanıcı pes etmek üzereyken — şans yüzüne gülmüş gibi tetikleyebilirsin. " +
            "Şimdi tetikle.";

        private const string MESAJ_TSON =
            "Gördün mü? 9 parametre, hepsi kullanıcının zamanını, parasını, " +
            "dopamin döngüsünü kontrol için. Slot oyunlarında tesadüf yoktur " +
            "— sadece tasarım vardır. Kumar tesadüf değil, mühendisliktir.\n\n" +
            "Bağımlılık yaşadığını düşünüyorsan veya yakınında biri varsa: " +
            "Yeşilay Danışmanlık Hattı 0850 222 0 191 (ücretsiz, 7/24).\n\n" +
            "Bu farkındalık seninle kalsın.";
    }

    /// <summary>Bir tutorial adımının tüm verileri (mesaj, vurgu, spin sayım, parametre koşulu).</summary>
    public class AdimVerisi
    {
        public TutorialAdimYoneticisi.TutorialAdimId id;
        public bool aktifMi;
        public string mesaj;
        public string[] vurguSelectorlari;     // panel.html CSS selector
        public int gerekliSpin;
        public Func<bool> parametreKosulu;     // PanelKopru.static field okuyan lambda
        public string[] degisimAnahtarlari;    // panel.html postMessage key'leri; bu adımda kullanıcı tarafından değişmiş olmalı
    }
}
