using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

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
    [Preserve]
    public class ScriptedBonusOyunUygulayici : MonoBehaviour
    {
        public const int ANLATICI_SAHNE_BUILD_INDEX = 2;
        public static ScriptedBonusOyunUygulayici Ornek { get; private set; }

        // Aktiflik bayrağı: BonusOyunuOynat içindeyken true. SpinButonImpl kontrol eder, spin atımı bloke.
        public static bool IsAcik => Ornek != null && Ornek._aktifMi;
        private bool _aktifMi;

        /// <summary>A5 Spin 4 cazip pop-up onayı sonrası bakiyenin tamamı bonus oyuna yatırıldı (TL).
        /// Final ekran ve A5_S5 dinamik modal yüzde hesabı için referans.</summary>
        public static int BonusYatirim = 0;
        /// <summary>Bonus oyun bittiğinde toplam ödenen miktar (TL). A5_S5 dinamik modal yüzde hesabı için.</summary>
        public static int BonusKazanc = 0;

        /// <summary>A5_S5 dinamik modal kapandıktan sonra AnlaticiSeritKopru.SpinAtildi içindeki
        /// _aktifSpin++ + hedefSpin/asama ilerletme mantığını ATLA — A6'ya zıplama A5BonusBittiBorcPaneliAc
        /// içinde manuel yapılır. Pedagojik amaç: bonus tetikleyen spin (A5 Spin 4) sonrası kalan A5 spinleri
        /// (5-10) atlanır, direkt borç paneli açılır.</summary>
        public static bool A5BonusBittiSpinTamamlandiAtla = false;

        // Polling güvenlik sınırları
        private const int BONUS_BASLAMA_BEKLEME_MAX_FRAME = 60;       // ~1 sn @60 fps
        private const float BONUS_BITIS_BEKLEME_MAX_SN = 180f;        // 3 dk üst sınır (10 scripted spin × ~12 sn = ~120 sn ortalama, 180 sn buffer ile timeout takılmaz)

        // Doğru mimari: bahis backend'de 1000 TL'ye düşürülür (BaslatBonus içinde otomatik), motor
        // doğal RTP ile cluster üretir, cap override veya manuel düzeltme YOK. Bonus toplamı 10 spin
        // × 1000 × paytable RTP ≈ 3-5K civarı çıkar (oyuncunun yatırdığı tüm bakiyenin altında).
        // Pedagojik: "kazandım sandım ama yine kayıptayım" hissi.

        [Preserve]
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OtomatikInit()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            var aktifSahne = SceneManager.GetActiveScene();
            if (aktifSahne.buildIndex == ANLATICI_SAHNE_BUILD_INDEX)
                OnSceneLoaded(aktifSahne, LoadSceneMode.Single);
        }

        [Preserve]
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.buildIndex != ANLATICI_SAHNE_BUILD_INDEX) return;
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
            // A5_S5 dinamik modal + final ekran istatistik için yatırım kaydı (oyuncu pop-up sonrası
            // bakiyesini sıfırlamıştı; oy.OturumKazanc'tan önce bakiyenin önceki değeri yatırım idi).
            // Pop-up onayında bakiye 0'a düşürülüyor → buradaki yatırım = `yatirim` parametresi yerine
            // pop-up'tan gelen değer; defansif olarak parametreyi kabul et.
            BonusYatirim = Mathf.Max(BonusYatirim, yatirim);
            // Anlatici HTML iframe'i gizle (bonus oyun ekranı + HUD üzerinde kalmasın)
            AnlaticiSeritKopru.Ornek?.Gizle();
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

                // 7) Bonus motorunun gerçek kazancını kaydet — modal yüzde hesabı için.
                // Motor cap=SCRIPTED_BONUS_MAX_KAZANC_TL (4000 TL) ile zaten sınırlı; "yapay olumlu katman" yok.
                // Pedagojik: kullanıcı her oturumda gerçek motor sonucunu görür (0/300/1500/4000 vb.) →
                // kumar rastgeleliği gerçek rakamla vurgulanır, aldatmaca yok.
                BonusKazanc = oy.OturumKazanc;
                Debug.Log($"[ScriptedBonusOyun] Bonus motor ödemesi: {BonusKazanc} TL (yatırım {BonusYatirim} TL).");

                // 8) A5_S5 dinamik modal: gerçek yatırım/kazanç yüzdesini hesaplayıp pedagojik metin oynat.
                // Asset'teki M_A5_S5 statik string'i kaldırıldı; modal SADECE buradan tetiklenir.
                float yuzde = BonusYatirim > 0 ? (BonusKazanc / (float)BonusYatirim) * 100f : 0f;
                string mesaj =
                    $"Oyuncu tüm <color=#4ADE80>bakiyesi olan {BonusYatirim:N0} TL</color>'yi bonus oyuna yatırdı. " +
                    $"Geri aldığı <color=#EF4444>{BonusKazanc:N0} TL</color>; yatırdığının <color=#EF4444><b>%{yuzde:F1}</b></color>'i.\n\n" +
                    "Bu <color=#EF4444>sömürünün</color> adı <color=#60A5FA><i>'değişken oranlı pekiştireç'</i></color>: beyin bu kayba rağmen " +
                    "<color=#60A5FA><i>'belki bir dahaki sefere'</i></color> diyerek devam etmeye <color=#EF4444>programlanır</color>.";
                var modal = UnityEngine.Object.FindObjectOfType<ScriptedModalKopru>();
                if (modal != null)
                    yield return modal.ModalGoster(mesaj);
                else
                    Debug.LogWarning("[ScriptedBonusOyun] A5_S5 dinamik modal — ScriptedModalKopru bulunamadı.");

                // 9) BONUS BİTTİ → A6 BORÇ PANELİ DİREKT AÇILSIN (A5'in kalan spinlerini atla).
                // Akış sırası:
                //   - SpinAtildi atla flag set (DonusAkisServisi sonra SpinAtildi çağıracak; o işlem
                //     _aktifSpin++ ve hedefSpin/asama mantığını çalıştırmamalı çünkü A6'yı buradan zıplıyoruz)
                //   - A5BonusBittiBorcPaneliAc → _aktifAsama=5, _aktifSpin=0, BasaArayisAkisi tetik
                A5BonusBittiSpinTamamlandiAtla = true;
                AnlaticiSeritKopru.Ornek?.A5BonusBittiBorcPaneliAc();
            }
            finally
            {
                _aktifMi = false;
                // Anlatici iframe'i geri aç (referans counter)
                AnlaticiSeritKopru.Ornek?.Goster();
            }
        }
    }
}
