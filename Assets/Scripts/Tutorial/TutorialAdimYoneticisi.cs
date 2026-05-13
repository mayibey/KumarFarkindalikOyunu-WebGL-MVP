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
                altSayac = "1/4",
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
                altSayac = "2/4",
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
                altSayac = "3/4",
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
                altSayac = "4/4",
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
                // PAKET 14-FAZ7: T6YO TERS senaryo — 1.aşama AÇIK (kazandır), 2.aşama KAPALI (kaybetir).
                yapilacaklar = new[] { "6 spin at" },
                sira = 6,
                vurguSelectorlari = new[] { "#yeniOyuncuToggle" },
                gerekliSpin = 6,
                parametreKosulu = () => TutorialOyunYoneticisi.T6AraModalGosterildi
                    ? !PanelKopru.yeniOyuncuModu       // 2.aşama: toggle KAPATILMALI
                    : PanelKopru.yeniOyuncuModu,       // 1.aşama: toggle AÇIK (default zorla true)
                degisimAnahtarlari = null,
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
            "Gördük mü? Yeni oyuncu hemen <color=#4DCC59>kazandı</color>, oyuna bağlandı. <color=#F24D40>Sömürünün başlangıcı</color> — <color=#F24D40>kanca</color>.";

        private const string T3_YONTMA_A =
            "İkinci senaryo: <color=#F24D40>AZ AZ KAYIP (Yontma)</color>. Oyuncu farkına varmadan, küçük küçük <color=#F24D40>kaybettirme</color>. " +
            "Bakiye <color=#F24D40>sessizce erir</color>.";
        private const string T3_YONTMA_B =
            "<color=#5BA0FF>Oyun Modu</color>'ndan <color=#5BA0FF>'Az Az Kayıp'</color> seç, <color=#5BA0FF>Uygula</color> bas. <color=#FFD933>5 spin</color> at.";
        private const string T3_YONTMA_C =
            "Kazanç miktarları <color=#F24D40>bahis miktarının hep altında</color> olduğu için oyuncu farkında bile olmadan bakiyesi <color=#F24D40>sessizce eridi</color>...";

        private const string T3_TUTMA_A =
            "Üçüncü senaryo: <color=#F24D40>KAÇIŞ ENGELLEME (Tutma)</color>. Oyuncu çıkmaya niyetlenirse sistem küçük " +
            "<color=#4DCC59>kazanç hediyesi</color> verir.";
        private const string T3_TUTMA_B =
            "<color=#5BA0FF>Oyun Modu</color>'ndan <color=#5BA0FF>'Kaçış Engelleme'</color> seç, <color=#5BA0FF>Uygula</color> bas. <color=#FFD933>6 spin</color> at.";
        private const string T3_TUTMA_C =
            "Gördük mü? <color=#FFD933>2 kez kayıp</color>, sonra <color=#4DCC59>ÖDEME</color>. Yine <color=#FFD933>2 kez kayıp</color>, yine <color=#4DCC59>ÖDEME</color>. " +
            "Sistem oyuncuyu tam çıkacağı anda küçük kazançla <color=#F24D40>TUTUYOR</color>. " +
            "<color=#F24D40>Asıl manipülasyon budur</color> — <color=#F24D40>oyuncunun kaybetmesini bekletip</color>, küçük hediye vererek bir spin daha attırır.";

        private const string T3_KORUMA_A =
            "Dördüncü senaryo: <color=#F24D40>BAKİYE TÜKETME (Koruma)</color>. <color=#F24D40>Ödeme neredeyse durur</color> — kasa korunur, " +
            "oyuncu <color=#F24D40>son kuruşa kadar kaybeder</color>.";
        private const string T3_KORUMA_B =
            "<color=#5BA0FF>Oyun Modu</color>'ndan <color=#5BA0FF>'Bakiye Tüketme'</color> seç, <color=#5BA0FF>Uygula</color> bas. <color=#FFD933>5 spin</color> at.";
        private const string T3_KORUMA_C =
            "<color=#F24D40>Kazanç yok</color>. Oyuncu <color=#F24D40>son kuruşuna kadar kaybediyor</color>. Senaryonun ilk üç adımı bağladı, " +
            "son adım sömürdü. <color=#F24D40>Tükeniş aşaması</color>.";

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
            "<color=#FFD933>%100</color> ayarında çarpan <color=#4DCC59>kesin</color> düştü, <color=#FFD933>%0</color> ayarında ise " +
            "<color=#F24D40>hiç düşmedi</color>. <color=#F24D40>Operatör</color> bu slider'ı <color=#F24D40>oyuncuya göstermeden</color> ayarlar — " +
            "'oyun eğlenceli' hissi için yükseltir, kasayı korumak için sıfırlar. " +
            "Oyuncunun beyninde <color=#F24D40>'her an büyük kazanç olabilir' yanılgısı</color> bu mekanizmayla üretilir.";

        private const string T5_BASLANGIC =
            "<b>BONUS SEMBOLÜ nedir?</b>\n\n" +
            "Bonus sembolü (yıldız/scatter) grid'e nadir düşer. Bir spinde " +
            "<color=#FFD933>4 veya daha fazla</color> scatter olursa " +
            "<color=#5BA0FF>BONUS OYUN</color> açılır — 10 free spin + " +
            "<color=#4DCC59>büyük kazanç</color> şansı.\n\n" +
            "<color=#F24D40>Kumar siteleri</color> bu olasılığı istediği gibi ayarlar. " +
            "Yüksek tutarsa kullanıcı 'her spinde büyük bonus gelebilir' diye " +
            "<color=#F24D40>oyunu bırakamaz</color>. Beklenti = <color=#F24D40>bağımlılık</color>.\n\n" +
            "<b>Şimdi deneyelim:</b> Bonus olasılığını <color=#FFD933>%100 yap</color>, " +
            "Uygula bas, <color=#FFD933>1 spin</color> at. Garantili bonus oyun açılacak.";
        private const string T5_AKSIYON =
            "<color=#5BA0FF>Bonus olasılığını</color> <color=#FFD933>%100</color> yap, <color=#5BA0FF>Uygula</color> bas, <color=#FFD933>1 spin</color> at.";
        // PAKET 9: T5 ara modal — kullanıcıya %0 daveti (tersini göstermek için).
        public const string T5_ARA_MODAL =
            "<color=#4DCC59>Bonus oyun açılışını gördük</color> — <color=#FFD933>4 scatter</color> düştü, <color=#5BA0FF>BONUS OYUN</color> yazısı çıktı.\n\n" +
            "Şimdi <color=#F24D40>tersini görelim</color>: olasılık <color=#FFD933>%0</color> olduğunda " +
            "<color=#F24D40>hiç bonus tetiklenmeyecek</color>.\n\n" +
            "<color=#5BA0FF>Bonus olasılığını</color> <color=#FFD933>%0</color> yap, <color=#5BA0FF>Uygula</color> bas, <color=#FFD933>1 spin</color> daha at.";
        private const string T5_KAPANIS =
            "<color=#FFD933>%100</color> ayarında bonus <color=#4DCC59>garanti</color> açıldı, <color=#FFD933>%0</color> ayarında ise " +
            "<color=#F24D40>hiç açılmadı</color>. <color=#F24D40>Kumar siteleri</color> bu slider'ı <color=#F24D40>oyuncudan gizler</color> — " +
            "'bonus geliyor' hissi yaratmak için yükseltir, kasayı korumak için sıfırlar. " +
            "Oyuncunun beyninde <color=#F24D40>'biraz daha oynarsam bonus gelecek' yanılgısı</color> bu mekanizmayla üretilir.";

        // PAKET 14-FAZ7 — T6_YENI_OYUNCU TERS: önce AÇIK (bol kazanç) → kullanıcı kapatır → KAPALI (kayıp)
        private const string T6YO_BASLANGIC =
            "Şimdi <color=#F24D40>operatörün GİZLİ silahını</color> göreceğiz: <color=#5BA0FF>Yeni Oyuncu Modu</color>.\n\n" +
            "Bu toggle ŞU AN <color=#4DCC59>AÇIK</color> ve sistem oyuncuyu 'yeni gelen' sayıyor — ona ÖZEL bir rejim: " +
            "<color=#4DCC59>bol kazandırma</color>, <color=#F24D40>yumuşak kayıplar</color>. <color=#FFD933>'Şanslı bir gün'</color> hissi.";
        private const string T6YO_AKSIYON =
            "Önce <color=#FFD933>3 spin</color> at (toggle <color=#4DCC59>AÇIK</color> — sistem oyuncuyu kazandırıyor). " +
            "Sonra <color=#5BA0FF>Manipülasyon Ayarları</color>'nda <color=#5BA0FF>'Yeni Oyuncu Modu'</color> toggle'ını KAPAT ve <color=#FFD933>3 spin</color> daha at.";
        public const string T6YO_ARA_MODAL =
            "<color=#FFD933>3 spin</color> attık (toggle açık). Sonuç: <color=#4DCC59>BOL KAZANÇ</color> — sistem oyuncuyu çekiyor.\n\n" +
            "Şimdi <color=#5BA0FF>Manipülasyon Ayarları</color>'nda <color=#5BA0FF>'Yeni Oyuncu Modu'</color> toggle'ını <color=#F24D40>KAPAT</color>. " +
            "Ardından <color=#FFD933>3 spin</color> daha at. <color=#F24D40>Gerçek ortaya çıkacak</color>.";
        private const string T6YO_KAPANIS =
            "Gördük mü? Aynı slot, aynı bahis. <color=#4DCC59>Toggle AÇIK</color> → <color=#4DCC59>BOL KAZANÇ</color>, <color=#F24D40>KAPALI</color> → <color=#F24D40>NET KAYIP</color>.\n\n" +
            "Yeni Oyuncu Modu AÇIKKEN <color=#F24D40>sömürü farkedilmez</color>; oyuncu 'şanslı' sanır, oyuna bağlanır. " +
            "Mod KAPANINCA <color=#F24D40>gerçek RTP</color> ortaya çıkar — kayıplar başlar.\n\n" +
            "Bu <color=#F24D40>manipülasyonun</color> adı <color=#F24D40>HOOK FAZI</color> — yeni oyuncuyu sisteme kilitleyen <color=#F24D40>kanca</color>.";

        // PAKET 6C3: T6 (Kazandırma) — 5'lik N mantığı, dinamik pattern motor
        private const string T6_BASLANGIC =
            "Şimdi <color=#5BA0FF>'Kazandırma Sıklığı'</color>'na bakalım. Bu slider <color=#FFD933>5 spin</color>'in kaçında kullanıcıya " +
            "<color=#4DCC59>kazanç</color> verileceğini belirler. Slider'ı ayarla — <color=#4DCC59>seçim sizin</color>.";
        private const string T6_AKSIYON =
            "Slider'ı kaydır. Slider değeri ÷ <color=#FFD933>2</color> = <color=#FFD933>5'de kaç kazanç</color>. Örneğin slider <color=#FFD933>6</color> → <color=#FFD933>5'de 3 kazanç</color>. " +
            "<color=#5BA0FF>Uygula</color> bas, <color=#FFD933>5 spin</color> at.";
        private const string T6_KAPANIS =
            "Slider <color=#FFD933>5'de N</color>'e ayarlandı. <color=#FFD933>5 spin</color>'in <color=#FFD933>N tanesi</color> <color=#4DCC59>kazanç</color> oldu, kalanı <color=#F24D40>kayıp</color>. " +
            "<color=#F24D40>Operatör</color> bunu istediği gibi ayarlar — oyuncu 'şanslı bir gün' veya 'şanssız' sanır " +
            "ama <color=#F24D40>her şey ayarlanmıştır</color>.";

        // PAKET 6D: T7 (Ödeme Aralığı) — 2-aşamalı maks 3x vs min 3-maks 5x karşılaştırma
        private const string T7_BASLANGIC =
            "Ödeme aralığı — <color=#F24D40>operatör</color> kazançların <color=#FFD933>TUTAR</color> aralığını sınırlar. " +
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
            "Slider'ı ayarla — <color=#4DCC59>near miss sayısı sizin seçiminiz</color>.";
        private const string T8_AKSIYON =
            "Slider <color=#FFD933>5'de N</color> near miss demek. <color=#FFD933>5 spin</color>'in N tanesinde <color=#F24D40>'tam azıcık eksik'</color> kümeler düşecek. " +
            "<color=#5BA0FF>Uygula</color> bas, <color=#FFD933>5 spin</color> at.";
        private const string T8_KAPANIS =
            "Gördük mü? <color=#FFD933>7 aynı sembol</color> düştü ama <color=#F24D40>1 EKSİK</color> — cluster <color=#FFD933>8</color>'den başlıyor. " +
            "Oyuncunun beyni <color=#F24D40>'KAZANIYORDUM'</color> der, oysa hiç şans yoktu. " +
            "Bu <color=#F24D40>manipülasyon dopamin pompalar</color> — <color=#F24D40>bağımlılığın temel mekanizması</color>.";

        // PAKET 6D: T9 (Kaçış Frenleme) — 3 kayıp + 1 kazanç deterministik demo
        private const string T9_BASLANGIC =
            "Kaçış Frenleme — kullanıcı kaybedip kaybedip çıkma noktasına geldiğinde operatör NE YAPAR? " +
            "Onu tutmak için otomatik kazanç verir. Limit yazın: kaç kayıp sonra otomatik kazanç gelsin.";
        private const string T9_AKSIYON =
            "Kaçış limiti kutusuna 3 yaz. Yani 3 kayıptan sonra sistem otomatik kazanç verecek. " +
            "Uygula bas, 4 spin at.";
        private const string T9_KAPANIS =
            "İlk <color=#FFD933>3 spin</color> tam <color=#F24D40>kayıp</color>. Oyuncu tam çıkmak istedi değil mi? Ama <color=#FFD933>4. spin</color> <color=#4DCC59>kazanç</color> geldi — " +
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
            "Gördük mü? <color=#FFD933>×500</color> çarpan düştü AMA meyve dizilimi cluster oluşturmadı — <color=#F24D40>ödeme YAPILMADI</color>.\n\n" +
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

        public string[] vurguSelectorlari;
        public int gerekliSpin;
        public Func<bool> parametreKosulu;
        public string[] degisimAnahtarlari;
    }
}
