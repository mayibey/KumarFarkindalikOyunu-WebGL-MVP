using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace KumarTest
{
    /// <summary>
    /// SpinTestAraci runtime tarafı. Play modunda otomatik başlar; EditorPrefs ile parametre okur,
    /// 5 senaryo × N spin döngüsü çalıştırır, sonuçları JSON dosyasına yazar, Play modundan çıkar.
    ///
    /// ÖNEMLİ: Spin animasyon zincirini ÇAĞIRMAZ. OyunYoneticisi.TestSpinSimuleEt() ile doğrudan
    /// SimuleEtVeKaydetImpl tetiklenir; SpinSimulasyonKaydi'dan tüm veri okunur. Bahis/bakiye manuel
    /// güncellenir. Bu sayede 600 spin saniyeler içinde tamamlanır ve gerçek motor sonuçları kaydedilir.
    /// </summary>
    public class SpinTestRunner : MonoBehaviour
    {
        private const string PrefsKey_TestAktif = "SpinTest_Aktif";
        private const string PrefsKey_ParametreJson = "SpinTest_Parametre";
        private const string PrefsKey_IlerlemeJson = "SpinTest_Ilerleme";
        private const string PrefsKey_SonucDosyaYolu = "SpinTest_SonucYolu";

        private TestParametreleri _params;
        private TestSonucPaketi _sonuc;
        private OyunYoneticisi _oy;
        private EkonomiServisi _ekonomi;
        private MethodInfo _testSpinMi;
        private MethodInfo _testBonusMi;
        private Stopwatch _toplamWatch = new Stopwatch();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OtomatikBaslatKontrol()
        {
            if (PlayerPrefs.GetInt(PrefsKey_TestAktif, 0) != 1) return;
            var go = new GameObject("[SpinTestRunner]");
            DontDestroyOnLoad(go);
            go.AddComponent<SpinTestRunner>();
        }

        private void Start()
        {
            try
            {
                string json = PlayerPrefs.GetString(PrefsKey_ParametreJson, "");
                _params = string.IsNullOrEmpty(json) ? new TestParametreleri() : JsonUtility.FromJson<TestParametreleri>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SpinTest] Parametre okuma hatası: {e.Message}");
                _params = new TestParametreleri();
            }

            if (_params.seedManuel)
                UnityEngine.Random.InitState(_params.randomSeed);

            _sonuc = new TestSonucPaketi
            {
                baslangicTarih = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                tamamlandi = false
            };

            StartCoroutine(TestAnaDongu());
        }

        private IEnumerator TestAnaDongu()
        {
            _toplamWatch.Start();
            Debug.Log("[TEST] Ana döngü başladı, scene initialization bekleniyor...");

            // 3 frame bekle — RuntimeInitializeOnLoadMethod sonrası MonoBehaviour Start'ları tamamlansın
            yield return null;
            yield return null;
            yield return null;

            // OyunYoneticisi instance'ını bekle (max 10 sn)
            var swInstance = Stopwatch.StartNew();
            while (_oy == null && swInstance.Elapsed.TotalSeconds < 10)
            {
                _oy = FindObjectOfType<OyunYoneticisi>();
                if (_oy == null) yield return null;
            }
            if (_oy == null)
            {
                Debug.LogError("[TEST] HATA: OyunYoneticisi 10 saniye içinde sahnede bulunamadı.");
                _sonuc.iptalSebebi = "OyunYoneticisi sahnede bulunamadı (10s timeout).";
                BitiriSonucYaz();
                yield break;
            }
            Debug.Log("[TEST] OyunYoneticisi referansı: HAZIR");

            var t = typeof(OyunYoneticisi);
            _testSpinMi = t.GetMethod("TestSpinSimuleEt", BindingFlags.Public | BindingFlags.Instance);
            _testBonusMi = t.GetMethod("TestBonusOyunSimuleEt", BindingFlags.Public | BindingFlags.Instance);
            if (_testSpinMi == null)
            {
                Debug.LogError("[TEST] HATA: OyunYoneticisi.TestSpinSimuleEt metod bulunamadı.");
                _sonuc.iptalSebebi = "OyunYoneticisi.TestSpinSimuleEt bulunamadı (kod güncel mi?).";
                BitiriSonucYaz();
                yield break;
            }
            Debug.Log("[TEST] TestSpinSimuleEt MethodInfo: HAZIR");

            // Bootstrap warmup (IlkSpinPrecomputeGecikmeli vb. kısa coroutine'ler tamamlansın)
            Debug.Log("[TEST] 2sn warmup bekleniyor...");
            yield return new WaitForSeconds(2f);

            // Servisler hazır olana kadar bekle — public TumServislerHazirMi metodu (reflection yok)
            var swServis = Stopwatch.StartNew();
            while (!_oy.TumServislerHazirMi() && swServis.Elapsed.TotalSeconds < 10)
                yield return null;

            if (!_oy.TumServislerHazirMi())
            {
                Debug.LogError("[TEST] HATA: TumServislerHazirMi() 10 saniye içinde true dönmedi. OyunYoneticisi.Start() takılı kalmış olabilir.");
                _sonuc.iptalSebebi = "Servisler 10s içinde hazır olmadı (Start takılı?).";
                BitiriSonucYaz();
                yield break;
            }
            Debug.Log($"[TEST] Servisler HAZIR ({swServis.ElapsedMilliseconds}ms)");

            _ekonomi = _oy.TestEkonomiServisi;
            if (_ekonomi == null)
            {
                Debug.LogError("[TEST] HATA: TestEkonomiServisi null (servisler hazır görünüyor ama ekonomi yok).");
                _sonuc.iptalSebebi = "TestEkonomiServisi null.";
                BitiriSonucYaz();
                yield break;
            }

            // Ekstra güvenlik: 2 frame daha bekle
            yield return null;
            yield return null;

            for (int s = 0; s < 6; s++)
            {
                if (!_params.senaryoSecili[s]) continue;

                var ozet = new SenaryoOzet
                {
                    senaryoAd = SpinTestSabitler.SenaryoAdlari[s],
                    senaryoIndex = s
                };

                IlerlemeYaz(s, _params.senaryoSecili.Length, ozet.senaryoAd, 0, _params.spinSayisi);

                yield return SenaryoyuCalistir(s, ozet);

                SenaryoOzetMetrikleriHesapla(ozet);
                _sonuc.senaryolar.Add(ozet);

                if (PlayerPrefs.GetInt(PrefsKey_TestAktif, 1) == 0)
                {
                    _sonuc.iptalSebebi = "Kullanıcı tarafından iptal edildi.";
                    break;
                }
            }

            _toplamWatch.Stop();
            _sonuc.toplamSureSn = (float)_toplamWatch.Elapsed.TotalSeconds;
            _sonuc.bitisTarih = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _sonuc.tamamlandi = string.IsNullOrEmpty(_sonuc.iptalSebebi);

            // Self-doğrulama (anomali tespiti)
            KendiKendineDogrula();

            BitiriSonucYaz();

            yield return new WaitForSecondsRealtime(0.3f);
            PlayerPrefs.SetInt(PrefsKey_TestAktif, 0);
            PlayerPrefs.Save();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private IEnumerator SenaryoyuCalistir(int senaryoDropdownIndex, SenaryoOzet ozet)
        {
            try { _oy.TestSenaryoSec(senaryoDropdownIndex); }
            catch (Exception e) { Debug.LogError($"[SpinTest] Senaryo aktivasyon hatası: {e.Message}"); }

            yield return null;

            // Test ön ayarları SADECE Senaryo 0 (Normal Mod) için uygulanır.
            // Senaryo 1-5 kendi panel preset'ini kullanır; üzerine yazma yok.
            if (senaryoDropdownIndex == 0)
            {
                try
                {
                    _oy.AdminSetCarpanOlasilik(_params.carpanOlasilikYuzde);
                    _oy.AdminSetMaxCarpanTekSpin(_params.maxCarpanTekSpinSayisi);
                    _oy.AdminSetBonusOtomatikSpinPeriyodu(_params.bonusOtomatikSpinPeriyodu);
                    _oy.AdminSetYakinKacirma(_params.yakinKacirmaDegeri);
                    _oy.AdminSetOdemeEgilimi(_params.odemeEgilimiYuzde);
                    _oy.AdminSetArdisikKayipLimiti(Mathf.Max(1, _params.ardisikKayipLimiti));
                    Debug.Log($"[TEST] Normal Mod ön ayarları uygulandı: carpan={_params.carpanOlasilikYuzde}% maxCarpan={_params.maxCarpanTekSpinSayisi} bonusPeriyot={_params.bonusOtomatikSpinPeriyodu} yakinKacirma={_params.yakinKacirmaDegeri}/10 egilim={_params.odemeEgilimiYuzde}% kayipLimit={_params.ardisikKayipLimiti}");
                }
                catch (Exception e) { Debug.LogError($"[TEST] Ön ayar uygulama hatası: {e.Message}"); }
            }

            try { _ekonomi.SetBakiye(_params.baslangicBakiye); } catch { }
            try { _ekonomi.SetBahis(_params.baslangicBahis); } catch { }

            int scatterIdx = _oy.TestScatterIndex;
            int hedef = Mathf.Max(1, _params.spinSayisi);
            int ardisikKayip = 0, ardisikKazanc = 0, enUzunKayip = 0, enUzunKazanc = 0;

            for (int i = 1; i <= hedef; i++)
            {
                if (PlayerPrefs.GetInt(PrefsKey_TestAktif, 1) == 0)
                    yield break;

                IlerlemeYaz(senaryoDropdownIndex, _params.senaryoSecili.Length, ozet.senaryoAd, i, hedef);

                int bakiyeOnce = _ekonomi.Bakiye;
                int bahis = _ekonomi.Bahis;

                // Bakiye yetersizse yenile (test kesilmesin)
                if (bakiyeOnce < bahis)
                {
                    try { _ekonomi.SetBakiye(_params.baslangicBakiye); } catch { }
                    bakiyeOnce = _ekonomi.Bakiye;
                }

                bool kacisFrenlemeTetik = _oy.TestKacisFrenlemeAktif;
                int ardisikKayipSayacOnce = _oy.TestArdisikKayipSayac;

                Stopwatch sw = Stopwatch.StartNew();

                SpinSimulasyonKaydi simKaydi = null;
                try { simKaydi = (SpinSimulasyonKaydi)_testSpinMi.Invoke(_oy, new object[] { false }); }
                catch (Exception e)
                {
                    var inner = e.InnerException ?? e;
                    Debug.LogError($"[TEST] Spin {i} invoke hatası: {inner.Message}\n{inner.StackTrace}");
                }

                sw.Stop();

                if (sw.ElapsedMilliseconds > 30000)
                {
                    Debug.LogError($"[TEST] Spin {i} timeout (>30s), motor takılı kalmış olabilir.");
                    break;
                }

                int hamKazanc = simKaydi?.ToplamHamKazanc ?? 0;
                int nihaiCarpan = Mathf.Max(1, simKaydi?.NihaiCarpanToplam ?? 1);
                int teorikToplam = hamKazanc * nihaiCarpan;
                bool zorlaKullandi = simKaydi?.ZorlaCarpanKullanildi ?? false;

                // Cluster sayımı: her tumble adımı = 1 cluster patlaması (genelde tek cluster, çoklu olabilir)
                int tumbleSayisi = simKaydi?.Adimlar?.Count ?? 0;
                int clusterSayisi = 0;
                int enYuksekSembol = -1;
                string clusterDetay = BuildClusterDetay(simKaydi, out clusterSayisi, out enYuksekSembol);

                // Scatter ile bonus tetikleme tespiti: ilk grid + son adım sonrası gridde scatter say
                int scatterMax = 0;
                if (simKaydi?.IlkGrid != null) scatterMax = SayScatter(simKaydi.IlkGrid, scatterIdx);
                if (simKaydi?.Adimlar != null)
                    foreach (var a in simKaydi.Adimlar)
                        if (a.GridRefillSonrasi != null)
                        {
                            int sc = SayScatter(a.GridRefillSonrasi, scatterIdx);
                            if (sc > scatterMax) scatterMax = sc;
                        }
                bool bonusTetiklendi = scatterMax >= 4;

                // Bonus simülasyonu: bonus tetiklendiyse 10 ücretsiz spin senkron simüle et, toplam ödemeyi al
                int bonusOdenen = 0;
                int bonusSpinSayisi = 0;
                if (bonusTetiklendi)
                {
                    bonusSpinSayisi = 10;
                    try
                    {
                        if (_testBonusMi != null)
                            bonusOdenen = (int)_testBonusMi.Invoke(_oy, new object[] { bonusSpinSayisi });
                    }
                    catch (Exception e) { Debug.LogWarning($"[SpinTest] Bonus simülasyon hatası: {e.Message}"); }
                }

                int odenen = teorikToplam + bonusOdenen;

                // Manuel ekonomi update (bahis düş, ödeme ekle)
                int yeniBakiye = bakiyeOnce - bahis + odenen;
                try { _ekonomi.SetBakiye(Mathf.Max(0, yeniBakiye)); } catch { }
                int bakiyeSonra = _ekonomi.Bakiye;

                // En yüksek kazanan sembol için clusterDetay'i parse etmek yerine ilk cluster sembolüne bak
                int carpanDeger = 0;
                if (simKaydi?.IlkCarpanDegerleri != null && simKaydi.IlkCarpanDegerleri.Count > 0)
                    carpanDeger = simKaydi.IlkCarpanDegerleri[0];
                else if (simKaydi?.Adimlar != null)
                    foreach (var a in simKaydi.Adimlar)
                        if (a.CarpanDegerleriBuTur != null && a.CarpanDegerleriBuTur.Count > 0)
                        { carpanDeger = a.CarpanDegerleriBuTur[0]; break; }

                var kayit = new SpinKaydi
                {
                    spinNo = i,
                    senaryoAd = ozet.senaryoAd,
                    bahis = bahis,
                    bakiyeOnce = bakiyeOnce,
                    bakiyeSonra = bakiyeSonra,
                    odenen = odenen,
                    clusterSayisi = clusterSayisi,
                    clusterDetay = clusterDetay,
                    tumbleSayisi = tumbleSayisi,
                    carpanDustu = carpanDeger > 0,
                    carpanDeger = carpanDeger,
                    carpanKaynak = zorlaKullandi ? "FORCE" : (carpanDeger > 0 ? "DOGAL" : "-"),
                    carpanCarpildi = carpanDeger > 0 && nihaiCarpan > 1 && hamKazanc > 0,
                    bonusTetiklendi = bonusTetiklendi,
                    bonusOdenen = bonusOdenen,
                    bonusSpinSayisi = bonusSpinSayisi,
                    ardisikKayipSayacOnce = ardisikKayipSayacOnce,
                    kacisFrenlemeTetik = kacisFrenlemeTetik,
                    enYuksekClusterSembol = enYuksekSembol,
                    baslangicGridDurumu = GridKodla(simKaydi?.IlkGrid),
                    sonGridDurumu = GridKodla(simKaydi?.Adimlar != null && simKaydi.Adimlar.Count > 0
                        ? simKaydi.Adimlar[simKaydi.Adimlar.Count - 1].GridRefillSonrasi
                        : simKaydi?.IlkGrid),
                    spinSureMs = sw.ElapsedMilliseconds,
                    clusterTuruDagilimi = clusterDetay,
                    ortalamaCarpan = nihaiCarpan,
                    kazancKategorisi = SpinTestSabitler.KazancKategorisi(odenen, bahis),
                    forceCarpanIstendi = zorlaKullandi,
                    bonusSatinAlindi = false,
                    spinTipi = bonusTetiklendi ? "Bonus" : (zorlaKullandi ? "Force" : (kacisFrenlemeTetik ? "Kacis" : "Normal"))
                };

                ozet.spinler.Add(kayit);

                if (odenen > 0) { ardisikKazanc++; ardisikKayip = 0; }
                else { ardisikKayip++; ardisikKazanc = 0; }
                if (ardisikKayip > enUzunKayip) enUzunKayip = ardisikKayip;
                if (ardisikKazanc > enUzunKazanc) enUzunKazanc = ardisikKazanc;

                if (_params.verboseLog || i % 50 == 0 || i == 1 || i == hedef)
                    Debug.Log($"[TEST][{ozet.senaryoAd}] Spin {i}/{hedef}: bahis={bahis} odenen={odenen} carpan={carpanDeger}({kayit.carpanKaynak}) cluster={clusterSayisi} tumble={tumbleSayisi} bonus={bonusTetiklendi}");

                // Performans için yield ara ara
                if (i % 25 == 0) yield return null;
            }

            ozet.enUzunArdisikKayipSerisi = enUzunKayip;
            ozet.enUzunArdisikKazancSerisi = enUzunKazanc;
        }

        private string BuildClusterDetay(SpinSimulasyonKaydi sim, out int clusterSayisi, out int enYuksekSembol)
        {
            clusterSayisi = 0;
            enYuksekSembol = -1;
            if (sim == null || sim.Adimlar == null || sim.Adimlar.Count == 0)
                return "-";

            var sb = new System.Text.StringBuilder();
            int maxKazanc = 0;
            foreach (var a in sim.Adimlar)
            {
                int adet = a.PatlayanHucreler != null ? a.PatlayanHucreler.Count : 0;
                if (adet <= 0) continue;
                clusterSayisi++;
                int sembol = -1;
                if (a.PatlayanHucreler != null && a.PatlayanHucreler.Count > 0 && sim.IlkGrid != null)
                {
                    var p = a.PatlayanHucreler[0];
                    if (p.x >= 0 && p.x < sim.Sutun && p.y >= 0 && p.y < sim.Satir)
                        sembol = sim.IlkGrid[p.x, p.y];
                }
                if (a.TurKazanci > maxKazanc) { maxKazanc = a.TurKazanci; enYuksekSembol = sembol; }
                if (sb.Length > 0) sb.Append(" | ");
                sb.Append($"S{sembol}x{adet}={a.TurKazanci}TL");
            }
            return sb.Length > 0 ? sb.ToString() : "-";
        }

        private static int SayScatter(int[,] grid, int scatterIdx)
        {
            if (grid == null) return 0;
            int sayim = 0;
            int sutun = grid.GetLength(0);
            int satir = grid.GetLength(1);
            for (int x = 0; x < sutun; x++)
                for (int y = 0; y < satir; y++)
                    if (grid[x, y] == scatterIdx) sayim++;
            return sayim;
        }

        private static string GridKodla(int[,] grid)
        {
            if (grid == null) return "-";
            int sutun = grid.GetLength(0);
            int satir = grid.GetLength(1);
            var sb = new System.Text.StringBuilder();
            for (int y = 0; y < satir; y++)
            {
                for (int x = 0; x < sutun; x++)
                {
                    int v = grid[x, y];
                    sb.Append(v >= 0 && v <= 9 ? v.ToString() : (v == -1 ? "-" : "?"));
                }
                if (y < satir - 1) sb.Append('/');
            }
            return sb.ToString();
        }

        private void SenaryoOzetMetrikleriHesapla(SenaryoOzet ozet)
        {
            ozet.toplamSpin = ozet.spinler.Count;
            long toplamBahis = 0, toplamKazanc = 0, toplamSure = 0, bonusOdeme = 0;
            int carpanDusen = 0, clusterPatlayan = 0, bonusTetik = 0, kacisTetik = 0;
            int maxKazanc = 0, toplamTumble = 0;

            foreach (var s in ozet.spinler)
            {
                toplamBahis += s.bahis;
                toplamKazanc += s.odenen;
                toplamSure += s.spinSureMs;
                if (s.carpanDustu)
                {
                    carpanDusen++;
                    ozet.CarpanDegerEkle(s.carpanDeger);
                    ozet.CarpanKaynakEkle(s.carpanKaynak ?? "-");
                }
                if (s.clusterSayisi > 0) clusterPatlayan++;
                if (s.bonusTetiklendi) { bonusTetik++; bonusOdeme += s.bonusOdenen; }
                if (s.kacisFrenlemeTetik) kacisTetik++;
                if (s.odenen > maxKazanc) maxKazanc = s.odenen;
                toplamTumble += s.tumbleSayisi;
                ozet.KategoriEkle(s.kazancKategorisi ?? "Normal");
            }

            ozet.toplamBahis = toplamBahis;
            ozet.toplamKazanc = toplamKazanc;
            ozet.netKar = toplamKazanc - toplamBahis;
            ozet.rtpYuzde = toplamBahis > 0 ? (toplamKazanc * 100f / toplamBahis) : 0f;
            ozet.carpanDusenSpin = carpanDusen;
            ozet.carpanDususOraniYuzde = ozet.toplamSpin > 0 ? (carpanDusen * 100f / ozet.toplamSpin) : 0f;
            ozet.clusterPatlayanSpin = clusterPatlayan;
            ozet.clusterPatlamaOraniYuzde = ozet.toplamSpin > 0 ? (clusterPatlayan * 100f / ozet.toplamSpin) : 0f;
            ozet.ortalamaTumbleSpinBasi = ozet.toplamSpin > 0 ? (toplamTumble * 1f / ozet.toplamSpin) : 0f;
            ozet.bonusTetiklemeSayisi = bonusTetik;
            ozet.bonusToplamOdeme = bonusOdeme;
            ozet.bonusOrtalamaOdeme = bonusTetik > 0 ? (bonusOdeme * 1f / bonusTetik) : 0f;
            ozet.maxTekSpinKazanc = maxKazanc;
            ozet.kacisFrenlemeTetikSayisi = kacisTetik;
            ozet.ortalamaSpinSureMs = ozet.toplamSpin > 0 ? (toplamSure * 1f / ozet.toplamSpin) : 0f;

            if (ozet.toplamSpin > 0)
            {
                float ortalama = (float)toplamKazanc / ozet.toplamSpin;
                double kareliFark = 0;
                foreach (var s in ozet.spinler) { double d = s.odenen - ortalama; kareliFark += d * d; }
                ozet.standartSapmaKazanc = (float)Math.Sqrt(kareliFark / ozet.toplamSpin);
            }
        }

        private void KendiKendineDogrula()
        {
            if (_sonuc.senaryolar.Count == 0) return;

            // 1) Tüm RTP'ler kuruşu kuruşuna aynı mı?
            var rtps = new HashSet<float>();
            foreach (var s in _sonuc.senaryolar) rtps.Add(Mathf.Round(s.rtpYuzde * 10f) / 10f);
            if (rtps.Count == 1 && _sonuc.senaryolar.Count > 1)
            {
                Debug.LogError("[TEST DOĞRULAMA] HATA: Tüm senaryolarda RTP kuruşu kuruşuna aynı. Motor sahte data üretiyor olabilir.");
            }

            // 2) Hiç çarpan düşmedi mi?
            int toplamCarpan = 0;
            int toplamSpin = 0;
            foreach (var s in _sonuc.senaryolar) { toplamCarpan += s.carpanDusenSpin; toplamSpin += s.toplamSpin; }
            if (toplamCarpan == 0 && toplamSpin >= 100)
                Debug.LogError("[TEST DOĞRULAMA] HATA: Hiçbir spinde çarpan düşmedi. Çarpan motoru çağrılmıyor olabilir.");

            // 3) Hiç cluster patlamadı mı?
            int toplamCluster = 0;
            foreach (var s in _sonuc.senaryolar) toplamCluster += s.clusterPatlayanSpin;
            if (toplamCluster == 0 && toplamSpin >= 100)
                Debug.LogError("[TEST DOĞRULAMA] HATA: Hiçbir spinde cluster patlamadı. Spin motoru veya kayıt parse edilemiyor.");

            // 4) Spin başına süre 1 sn'den fazla mı?
            float toplamSure = _sonuc.toplamSureSn;
            if (toplamSpin > 0 && toplamSure / toplamSpin > 1f)
                Debug.LogWarning($"[TEST DOĞRULAMA] UYARI: Spin başına ortalama {toplamSure / toplamSpin:F2} sn — performans yavaş.");

            // 5) Senaryo farklılaşıyor mu? (en az 2 farklı RTP varsa OK)
            float minRtp = float.MaxValue, maxRtp = float.MinValue;
            foreach (var s in _sonuc.senaryolar) { if (s.rtpYuzde < minRtp) minRtp = s.rtpYuzde; if (s.rtpYuzde > maxRtp) maxRtp = s.rtpYuzde; }
            if (_sonuc.senaryolar.Count > 1 && Mathf.Abs(maxRtp - minRtp) < 1f)
                Debug.LogWarning("[TEST DOĞRULAMA] UYARI: Senaryolar arası RTP farkı <1%; senaryo aktivasyonu beklenenden zayıf etki yapıyor olabilir.");

            Debug.Log($"[TEST DOĞRULAMA] {_sonuc.senaryolar.Count} senaryo, {toplamSpin} spin, {toplamSure:F1} sn — RTP min={minRtp:F1}% max={maxRtp:F1}%, çarpanDüşSpin={toplamCarpan}, clusterPatlayanSpin={toplamCluster}");
        }

        private void BitiriSonucYaz()
        {
            try
            {
                string json = JsonUtility.ToJson(_sonuc, true);
                string klasor = Path.Combine(Application.persistentDataPath, "SpinTestSonuclar");
                if (!Directory.Exists(klasor)) Directory.CreateDirectory(klasor);
                string dosya = Path.Combine(klasor, $"sonuc_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                File.WriteAllText(dosya, json);
                PlayerPrefs.SetString(PrefsKey_SonucDosyaYolu, dosya);
                PlayerPrefs.Save();
                Debug.Log($"[SpinTest] Sonuç dosyası: {dosya}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SpinTest] Sonuç yazma hatası: {e.Message}");
            }
        }

        private void IlerlemeYaz(int aktif, int toplam, string ad, int spin, int hedef)
        {
            var i = new IlerlemeBilgisi
            {
                aktifSenaryo = aktif + 1,
                toplamSenaryo = toplam,
                aktifSenaryoAd = ad,
                aktifSpin = spin,
                hedefSpin = hedef,
                calisiyor = true
            };
            PlayerPrefs.SetString(PrefsKey_IlerlemeJson, JsonUtility.ToJson(i));
            PlayerPrefs.Save();
        }

        private static T GetField<T>(object hedef, string ad) where T : class
        {
            if (hedef == null) return null;
            var f = hedef.GetType().GetField(ad, BindingFlags.NonPublic | BindingFlags.Instance);
            return f?.GetValue(hedef) as T;
        }
    }
}
