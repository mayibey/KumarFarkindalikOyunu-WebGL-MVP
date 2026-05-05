using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Senaryo.Scripted
{
    /// <summary>
    /// A5 Spin 4 — bonus tuzağı KÖPRÜSÜ. Eski "dandik runtime UI" iptal edildi; bu sınıf artık
    /// sadece projenin gerçek bonus oyun mekanizmasını (<see cref="OyunYoneticisi.ScriptedBonusTetikle"/>)
    /// programatik olarak başlatıp bitişini bekler.
    ///
    /// Akış:
    ///   1. <see cref="ScriptedBonusTuzagiPopup"/> açılır, kullanıcı [BONUS AL] basar (bakiye 0'a düşer)
    ///   2. <see cref="BonusOyunuOynat"/> çağrılır (DonusAkisServisi hook tarafından, yield return ile)
    ///   3. <see cref="OyunYoneticisi.ScriptedBonusTetikle"/> ile mevcut bonus oyun başlatılır:
    ///      - Cap=0 zorlanır (saf tuzak senaryosu)
    ///      - Sahnedeki BonusStartPanel + 10 free spin + BonusEndPanel akışı çalışır
    ///      - Bonus oyun 0 TL ödeme yapar (cap=0)
    ///   4. <see cref="OyunYoneticisi.BonusAktifMi"/> false olana kadar polling (yield)
    ///   5. Modal "tuzağa düştü..." DonusAkisServisi tarafından sonra çalışır
    /// </summary>
    public class ScriptedBonusOyunUygulayici : MonoBehaviour
    {
        public const int ANLATICI_SAHNE_BUILD_INDEX = 2;
        public static ScriptedBonusOyunUygulayici Ornek { get; private set; }

        // Aktiflik bayrağı: BonusOyunuOynat içindeyken true. SpinButonImpl kontrol eder, spin atımı bloke.
        public static bool IsAcik => Ornek != null && Ornek._aktifMi;
        private bool _aktifMi;

        // Polling güvenlik sınırları
        private const int BONUS_BASLAMA_BEKLEME_MAX_FRAME = 60;       // ~1 sn @60 fps
        private const float BONUS_BITIS_BEKLEME_MAX_SN = 120f;        // 2 dk üst sınır

        // Doğru mimari: bahis backend'de 1000 TL'ye düşürülür (BaslatBonus içinde otomatik), motor
        // doğal RTP ile cluster üretir, cap override veya manuel düzeltme YOK. Bonus toplamı 10 spin
        // × 1000 × paytable RTP ≈ 3-5K civarı çıkar (oyuncunun yatırdığı tüm bakiyenin altında).
        // Pedagojik: "kazandım sandım ama yine kayıptayım" hissi.

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OtomatikInit()
        {
            if (SceneManager.GetActiveScene().buildIndex != ANLATICI_SAHNE_BUILD_INDEX) return;
            if (Ornek != null) return;
            var go = new GameObject(nameof(ScriptedBonusOyunUygulayici));
            go.AddComponent<ScriptedBonusOyunUygulayici>();
        }

        private void Awake()
        {
            if (SceneManager.GetActiveScene().buildIndex != ANLATICI_SAHNE_BUILD_INDEX)
            {
                gameObject.SetActive(false);
                return;
            }
            if (Ornek != null && Ornek != this) { Destroy(gameObject); return; }
            Ornek = this;
            _aktifMi = false;
        }

        private void OnDestroy()
        {
            if (Ornek == this) Ornek = null;
            _aktifMi = false;
        }

        /// <summary>
        /// Mevcut bonus oyun mekanizmasını programatik başlatır + bitişini bekler.
        /// <paramref name="yatirim"/> ve <paramref name="getiri"/> parametreleri eski API uyumluluk için
        /// korundu; bu yeni implementasyonda yatırım/getiri OyunYoneticisi.ScriptedBonusTetikle içinde
        /// (cap=0 zorla) handle edilir.
        /// </summary>
        public IEnumerator BonusOyunuOynat(int yatirim, int getiri, OyunYoneticisi oy)
        {
            if (oy == null)
            {
                Debug.LogError("[ScriptedBonusOyun] OyunYoneticisi null — bonus oyun başlatılamadı.");
                yield break;
            }

            _aktifMi = true;
            try
            {
                Debug.Log($"[ScriptedBonusOyun] Mevcut bonus oyun mekanizmasına bağlanıyor (yatırım={yatirim}, planlanan getiri={getiri}).");

                // 1) Mevcut bonus oyunu programatik başlat — bahis override 1000 TL aktivasyonu
                // (cap override YOK, motor doğal RTP ile hesap yapsın)
                oy.ScriptedBonusTetikle();

                // 2) Bonus aktif olana kadar bekle (BonusBaslangicAkisi coroutine başlamalı)
                int bekleFrame = 0;
                while (!oy.BonusAktifMi && bekleFrame < BONUS_BASLAMA_BEKLEME_MAX_FRAME)
                {
                    yield return null;
                    bekleFrame++;
                }
                if (!oy.BonusAktifMi)
                {
                    Debug.LogError("[ScriptedBonusOyun] Bonus oyun başlatılamadı (timeout — BonusAktifMi false kaldı).");
                    yield break;
                }

                // 3) HUD'u aç — başlangıç: 10 spin / 0 TL kazanç
                int baslangicSpin = oy.BonusHakKalan;
                int baslangicKazanc = oy.OturumKazanc;
                if (ScriptedBonusHUDKopru.Ornek != null)
                    ScriptedBonusHUDKopru.Ornek.Goster(baslangicSpin, baslangicKazanc);

                // 4) Bonus bitene kadar polling + her frame HUD güncelle (BonusHakKalan + OturumKazanc canlı takip)
                float gecen = 0f;
                int sonSpin = -1, sonKazanc = -1;
                while (oy.BonusAktifMi)
                {
                    gecen += Time.unscaledDeltaTime;
                    if (gecen > BONUS_BITIS_BEKLEME_MAX_SN)
                    {
                        Debug.LogError($"[ScriptedBonusOyun] Bonus bitiş timeout ({BONUS_BITIS_BEKLEME_MAX_SN} sn aşıldı). Polling sonlandırılıyor.");
                        break;
                    }
                    int simdiSpin = oy.BonusHakKalan;
                    int simdiKazanc = oy.OturumKazanc;
                    if (simdiSpin != sonSpin || simdiKazanc != sonKazanc)
                    {
                        if (ScriptedBonusHUDKopru.Ornek != null)
                            ScriptedBonusHUDKopru.Ornek.Guncelle(simdiSpin, simdiKazanc);
                        sonSpin = simdiSpin;
                        sonKazanc = simdiKazanc;
                    }
                    yield return null;
                }

                // 5) HUD'u kapat
                if (ScriptedBonusHUDKopru.Ornek != null)
                    ScriptedBonusHUDKopru.Ornek.Gizle();

                // 6) Bahis override'ı kapat — oyuncunun gerçek bahisine geri yükle
                oy.ScriptedBonusBahisOverrideKapat();

                Debug.Log($"[ScriptedBonusOyun] Bonus tamamlandı. Motor doğal RTP ile {oy.OturumKazanc} TL ödedi.");
            }
            finally
            {
                _aktifMi = false;
            }
        }
    }
}
