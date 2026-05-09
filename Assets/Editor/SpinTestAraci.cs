using System;
using System.IO;
using System.Text;
using KumarTest;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// SpinTestAraci — basit 3-durumlu test penceresi.
/// Idle (form) → Çalışıyor (Play modu + progress) → Tamam (sonuç tablosu).
/// Pencere açılışında her zaman Idle'dan başlar; eski state taşınmaz.
/// Spin motoru: SpinTestRunner Play modunda OyunYoneticisi.TestSpinSimuleEt'i senkron çağırır.
/// </summary>
public class SpinTestAraci : EditorWindow
{
    private const string PrefsKey_TestAktif = "SpinTest_Aktif";
    private const string PrefsKey_ParametreJson = "SpinTest_Parametre";
    private const string PrefsKey_IlerlemeJson = "SpinTest_Ilerleme";
    private const string PrefsKey_SonucDosyaYolu = "SpinTest_SonucYolu";

    private enum Durum { Idle, Calisiyor, Tamam }

    private Durum _durum = Durum.Idle;
    private TestParametreleri _params;
    private TestSonucPaketi _sonuc;
    private string _csvKlasoru = "";
    private Vector2 _scroll;

    [MenuItem("Kumar/Test/Spin Test Aracı")]
    public static void Goster()
    {
        var w = GetWindow<SpinTestAraci>("Spin Test Aracı");
        w.minSize = new Vector2(560, 420);
        w.Show();
    }

    private void OnEnable()
    {
        // Pencere her açıldığında temiz başla — eski sonuç korunmasın
        _params = VarsayilanParametre();
        _sonuc = null;
        _csvKlasoru = "";

        // Eğer Play modu zaten aktifse ve test çalışıyorsa Çalışıyor durumuna geç
        if (EditorApplication.isPlaying && PlayerPrefs.GetInt(PrefsKey_TestAktif, 0) == 1)
            _durum = Durum.Calisiyor;
        else
            _durum = Durum.Idle;

        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void Update()
    {
        // Çalışıyor durumunda progress canlı kalsın
        if (_durum == Durum.Calisiyor)
            Repaint();
    }

    private void OnPlayModeStateChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredEditMode)
        {
            // Test bitti, sonucu oku — iptalSebebi olsa bile Tamam state'ine geç (hata mesajını göster)
            try
            {
                _sonuc = SonucuYukle();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SpinTest] JSON parse hatası: {e.Message}\n{e.StackTrace}");
                _sonuc = new TestSonucPaketi { iptalSebebi = $"JSON parse hatası: {e.Message}", tamamlandi = false };
            }

            if (_sonuc == null)
                _sonuc = new TestSonucPaketi { iptalSebebi = "Sonuç dosyası bulunamadı veya boş.", tamamlandi = false };

            // Senaryolar varsa CSV otomatik yaz
            if (_sonuc.senaryolar != null && _sonuc.senaryolar.Count > 0)
            {
                try { CsvKaydet(); }
                catch (Exception e) { Debug.LogError($"[SpinTest] CSV yazma hatası: {e.Message}"); }
            }

            _durum = Durum.Tamam;

            // Progress key'lerini temizle
            PlayerPrefs.DeleteKey(PrefsKey_TestAktif);
            PlayerPrefs.DeleteKey(PrefsKey_IlerlemeJson);
            PlayerPrefs.Save();
            Repaint();
        }
    }

    void OnGUI()
    {
        EditorGUILayout.Space(8);
        var baslikStil = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
        EditorGUILayout.LabelField("SPIN TEST ARACI", baslikStil);
        EditorGUILayout.Space(8);

        switch (_durum)
        {
            case Durum.Idle: FormCiz(); break;
            case Durum.Calisiyor: CalisiyorCiz(); break;
            case Durum.Tamam: SonucCiz(); break;
        }
    }

    // ─────────────────────────────────────────────
    // FORM
    // ─────────────────────────────────────────────
    private void FormCiz()
    {
        EditorGUILayout.BeginVertical("box");

        _params.spinSayisi = EditorGUILayout.IntSlider("Spin sayısı", _params.spinSayisi, 5, 1000);
        _params.baslangicBahis = EditorGUILayout.IntField("Bahis (TL)", _params.baslangicBahis);
        _params.baslangicBakiye = EditorGUILayout.IntField("Bakiye (TL)", _params.baslangicBakiye);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Senaryolar", EditorStyles.boldLabel);
        for (int i = 0; i < 6; i++)
            _params.senaryoSecili[i] = EditorGUILayout.ToggleLeft(SpinTestSabitler.SenaryoAdlari[i], _params.senaryoSecili[i]);

        EditorGUILayout.Space(6);
        _params.ileriAyarlarAcik = EditorGUILayout.Foldout(_params.ileriAyarlarAcik, "İleri Ayarlar (sadece Senaryo 0 = Normal Mod'a uygulanır)", true);
        if (_params.ileriAyarlarAcik)
        {
            EditorGUI.indentLevel++;
            _params.carpanOlasilikYuzde = EditorGUILayout.IntSlider("Çarpan olasılık %", _params.carpanOlasilikYuzde, 0, 100);
            _params.maxCarpanTekSpinSayisi = EditorGUILayout.IntSlider("Max çarpan/spin", _params.maxCarpanTekSpinSayisi, 1, 6);
            _params.bonusOtomatikSpinPeriyodu = EditorGUILayout.IntField("Bonus periyot (0=kapalı)", _params.bonusOtomatikSpinPeriyodu);
            _params.yakinKacirmaDegeri = EditorGUILayout.IntSlider("Yakın Kaçırma (10'da N)", _params.yakinKacirmaDegeri, 0, 10);
            _params.odemeEgilimiYuzde = EditorGUILayout.IntSlider("Ödeme eğilimi %", _params.odemeEgilimiYuzde, 0, 100);
            _params.ardisikKayipLimiti = EditorGUILayout.IntSlider("Ardışık kayıp limiti", _params.ardisikKayipLimiti, 1, 20);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(8);
        GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
        if (GUILayout.Button("TEST BAŞLAT", GUILayout.Height(36)))
            TestiBaslat();
        GUI.backgroundColor = Color.white;
    }

    // ─────────────────────────────────────────────
    // ÇALIŞIYOR
    // ─────────────────────────────────────────────
    private void CalisiyorCiz()
    {
        EditorGUILayout.BeginVertical("box");

        IlerlemeBilgisi i = null;
        try
        {
            string json = PlayerPrefs.GetString(PrefsKey_IlerlemeJson, "");
            if (!string.IsNullOrEmpty(json)) i = JsonUtility.FromJson<IlerlemeBilgisi>(json);
        }
        catch { }

        EditorGUILayout.LabelField("Test çalışıyor...", EditorStyles.boldLabel);
        if (i != null)
        {
            float oran = i.hedefSpin > 0 ? (float)i.aktifSpin / i.hedefSpin : 0f;
            EditorGUILayout.LabelField($"Senaryo {i.aktifSenaryo}/{i.toplamSenaryo}: {i.aktifSenaryoAd}");
            EditorGUILayout.LabelField($"Spin {i.aktifSpin}/{i.hedefSpin}");
            Rect r = EditorGUILayout.GetControlRect(false, 18);
            EditorGUI.ProgressBar(r, oran, $"{(oran * 100f):F0}%");
        }
        else
        {
            EditorGUILayout.LabelField("Hazırlanıyor...");
        }

        EditorGUILayout.Space(8);
        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("İPTAL", GUILayout.Height(28)))
            TestiIptalEt();
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndVertical();
    }

    // ─────────────────────────────────────────────
    // SONUÇ
    // ─────────────────────────────────────────────
    private void SonucCiz()
    {
        // Sonuç tamamen yoksa → iptal sebebini bas
        if (_sonuc == null)
        {
            EditorGUILayout.HelpBox("Sonuç dosyası bulunamadı.", MessageType.Error);
            if (GUILayout.Button("Yeni Test", GUILayout.Height(28))) YeniTest();
            return;
        }

        // Senaryolar boşsa hata olarak iptalSebebi'ni göster
        if (_sonuc.senaryolar == null || _sonuc.senaryolar.Count == 0)
        {
            string sebep = !string.IsNullOrEmpty(_sonuc.iptalSebebi)
                ? _sonuc.iptalSebebi
                : "Test başlatılamadı (sebep bilinmiyor). Console log'a bakın.";
            EditorGUILayout.HelpBox($"Test başlatılamadı: {sebep}", MessageType.Error);
            EditorGUILayout.LabelField("Olası sebepler:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("• OyunYoneticisi sahnede yok (yanlış sahne mi?)");
            EditorGUILayout.LabelField("• Servisler 10s içinde başlamadı (Start takılı?)");
            EditorGUILayout.LabelField("• Sahne otomatik akışı (SenaryoOtomatikAkis) çakışıyor");
            EditorGUILayout.Space(8);
            if (GUILayout.Button("Yeni Test", GUILayout.Height(28))) YeniTest();
            return;
        }

        EditorGUILayout.LabelField($"Test: {_sonuc.baslangicTarih} ({_sonuc.toplamSureSn:F1} sn)", EditorStyles.miniLabel);
        if (!_sonuc.tamamlandi)
            EditorGUILayout.HelpBox($"Tamamlanmadı: {_sonuc.iptalSebebi}", MessageType.Warning);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        // Karşılaştırma tablosu (kompakt)
        EditorGUILayout.BeginHorizontal();
        Hucre("Senaryo", true, 220);
        Hucre("Spin", true, 50);
        Hucre("RTP", true, 70);
        Hucre("Çarpan%", true, 70);
        Hucre("Bonus", true, 50);
        Hucre("MaxTek", true, 80);
        Hucre("MaxKayıp", true, 80);
        EditorGUILayout.EndHorizontal();

        foreach (var s in _sonuc.senaryolar)
        {
            EditorGUILayout.BeginHorizontal();
            Hucre(KisaAd(s.senaryoAd), false, 220);
            Hucre(s.toplamSpin.ToString(), false, 50);
            HucreRenkli($"{s.rtpYuzde:F1}%", RtpRengi(s.rtpYuzde), 70);
            Hucre($"{s.carpanDususOraniYuzde:F1}%", false, 70);
            Hucre(s.bonusTetiklemeSayisi.ToString(), false, 50);
            Hucre(s.maxTekSpinKazanc.ToString("N0"), false, 80);
            Hucre(s.enUzunArdisikKayipSerisi.ToString(), false, 80);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(8);

        // Anomali tespiti
        var uyarilar = AnomaliTespit();
        if (uyarilar.Count > 0)
        {
            EditorGUILayout.LabelField("Anormal Tespit", EditorStyles.boldLabel);
            foreach (var u in uyarilar)
                EditorGUILayout.HelpBox(u, MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox("Anomali tespit edilmedi.", MessageType.Info);
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(8);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Yeni Test", GUILayout.Height(28)))
            YeniTest();
        if (!string.IsNullOrEmpty(_csvKlasoru) && Directory.Exists(_csvKlasoru))
        {
            if (GUILayout.Button("Klasörü Aç", GUILayout.Height(28)))
                EditorUtility.RevealInFinder(_csvKlasoru);
        }
        EditorGUILayout.EndHorizontal();
    }

    // ─────────────────────────────────────────────
    // CONTROL
    // ─────────────────────────────────────────────
    private void TestiBaslat()
    {
        bool enAzBirSecili = false;
        for (int i = 0; i < _params.senaryoSecili.Length; i++)
            if (_params.senaryoSecili[i]) { enAzBirSecili = true; break; }
        if (!enAzBirSecili)
        {
            EditorUtility.DisplayDialog("Spin Test", "En az bir senaryo seçmelisiniz.", "Tamam");
            return;
        }

        string sahneYolu = "Assets/Scenes/03_SenaryoluOyun.unity";
        if (!File.Exists(sahneYolu))
        {
            EditorUtility.DisplayDialog("Spin Test", $"Test sahnesi bulunamadı: {sahneYolu}", "Tamam");
            return;
        }

        if (EditorSceneManager.GetActiveScene().path != sahneYolu)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(sahneYolu);
        }

        // Eski sonuç dosyasını referansını temizle
        PlayerPrefs.DeleteKey(PrefsKey_SonucDosyaYolu);

        PlayerPrefs.SetInt(PrefsKey_TestAktif, 1);
        PlayerPrefs.SetString(PrefsKey_ParametreJson, JsonUtility.ToJson(_params));
        PlayerPrefs.Save();

        _durum = Durum.Calisiyor;
        Repaint();

        EditorApplication.isPlaying = true;
    }

    private void TestiIptalEt()
    {
        PlayerPrefs.SetInt(PrefsKey_TestAktif, 0);
        PlayerPrefs.Save();
        if (EditorApplication.isPlaying)
            EditorApplication.isPlaying = false;
        // OnPlayModeStateChanged tetiklenecek; durum orada güncellenir.
    }

    private void YeniTest()
    {
        _sonuc = null;
        _csvKlasoru = "";
        _params = VarsayilanParametre();
        _durum = Durum.Idle;
        PlayerPrefs.DeleteKey(PrefsKey_TestAktif);
        PlayerPrefs.DeleteKey(PrefsKey_IlerlemeJson);
        PlayerPrefs.DeleteKey(PrefsKey_SonucDosyaYolu);
        PlayerPrefs.Save();
        Repaint();
    }

    // ─────────────────────────────────────────────
    // ANOMALİ
    // ─────────────────────────────────────────────
    private System.Collections.Generic.List<string> AnomaliTespit()
    {
        var liste = new System.Collections.Generic.List<string>();
        if (_sonuc == null || _sonuc.senaryolar.Count == 0) return liste;

        // 1) Tüm RTP'ler kuruşu kuruşuna aynı?
        if (_sonuc.senaryolar.Count > 1)
        {
            float ilk = Mathf.Round(_sonuc.senaryolar[0].rtpYuzde * 10f) / 10f;
            bool hepsiAyni = true;
            foreach (var s in _sonuc.senaryolar)
            {
                if (Mathf.Abs(Mathf.Round(s.rtpYuzde * 10f) / 10f - ilk) > 0.01f) { hepsiAyni = false; break; }
            }
            if (hepsiAyni)
                liste.Add("Tüm senaryolarda RTP kuruşu kuruşuna aynı — motor sahte data üretiyor olabilir.");
        }

        // 2) Hiç çarpan düşmedi mi?
        int toplamCarpan = 0, toplamSpin = 0, toplamCluster = 0;
        foreach (var s in _sonuc.senaryolar)
        {
            toplamCarpan += s.carpanDusenSpin;
            toplamSpin += s.toplamSpin;
            toplamCluster += s.clusterPatlayanSpin;
        }
        if (toplamCarpan == 0 && toplamSpin >= 100)
            liste.Add("Hiçbir spinde çarpan düşmedi — çarpan motoru çağrılmıyor olabilir.");
        if (toplamCluster == 0 && toplamSpin >= 100)
            liste.Add("Hiçbir spinde cluster patlamadı — spin motoru veya kayıt parse edilmiyor.");

        // 3) Senaryolar arası RTP farkı <1%
        if (_sonuc.senaryolar.Count > 1)
        {
            float minR = float.MaxValue, maxR = float.MinValue;
            foreach (var s in _sonuc.senaryolar)
            {
                if (s.rtpYuzde < minR) minR = s.rtpYuzde;
                if (s.rtpYuzde > maxR) maxR = s.rtpYuzde;
            }
            if (Mathf.Abs(maxR - minR) < 1f)
                liste.Add($"Senaryolar arası RTP farkı %{Mathf.Abs(maxR - minR):F2} — senaryo aktivasyonu zayıf etki yapıyor.");
        }

        // 4) Spin başına süre fazla mı?
        if (toplamSpin > 0 && _sonuc.toplamSureSn / toplamSpin > 1f)
            liste.Add($"Spin başına {_sonuc.toplamSureSn / toplamSpin:F2} sn — performans yavaş.");

        return liste;
    }

    // ─────────────────────────────────────────────
    // CSV
    // ─────────────────────────────────────────────
    private void CsvKaydet()
    {
        try
        {
            string projeKok = Path.GetDirectoryName(Application.dataPath);
            _csvKlasoru = Path.Combine(projeKok, "SpinTestSonuclar", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
            Directory.CreateDirectory(_csvKlasoru);

            // ozet.csv — meta veri başta
            var ozet = new StringBuilder();
            ozet.AppendLine($"# Test Tarihi;{_sonuc.baslangicTarih}");
            ozet.AppendLine($"# Toplam Süre (sn);{_sonuc.toplamSureSn:F1}");
            ozet.AppendLine($"# Spin Sayisi;{_params.spinSayisi}");
            ozet.AppendLine($"# Bahis;{_params.baslangicBahis}");
            ozet.AppendLine($"# Bakiye;{_params.baslangicBakiye}");
            if (_params.ileriAyarlarAcik)
            {
                ozet.AppendLine($"# CarpanOlasilik%;{_params.carpanOlasilikYuzde}");
                ozet.AppendLine($"# MaxCarpan/spin;{_params.maxCarpanTekSpinSayisi}");
                ozet.AppendLine($"# BonusPeriyot;{_params.bonusOtomatikSpinPeriyodu}");
                ozet.AppendLine($"# YakinKacirma10da;{_params.yakinKacirmaDegeri}");
                ozet.AppendLine($"# OdemeEgilimi%;{_params.odemeEgilimiYuzde}");
                ozet.AppendLine($"# ArdisikKayipLimiti;{_params.ardisikKayipLimiti}");
            }
            ozet.AppendLine();
            ozet.AppendLine("Senaryo;Spin;ToplamBahis;ToplamKazanc;NetKar;RTP%;CarpanDus%;ClusterPatla%;BonusTetik;MaxKazanc;MaxArdKayip;MaxArdKazanc;KacisTetik;OrtSureMs");
            foreach (var s in _sonuc.senaryolar)
                ozet.AppendLine($"{s.senaryoAd};{s.toplamSpin};{s.toplamBahis};{s.toplamKazanc};{s.netKar};" +
                    $"{s.rtpYuzde:F1};{s.carpanDususOraniYuzde:F1};{s.clusterPatlamaOraniYuzde:F1};{s.bonusTetiklemeSayisi};" +
                    $"{s.maxTekSpinKazanc};{s.enUzunArdisikKayipSerisi};{s.enUzunArdisikKazancSerisi};{s.kacisFrenlemeTetikSayisi};{s.ortalamaSpinSureMs:F1}");
            File.WriteAllText(Path.Combine(_csvKlasoru, "ozet.csv"), ozet.ToString(), new UTF8Encoding(true));

            // detay.csv (tek dosya, tüm senaryolar)
            var detay = new StringBuilder();
            detay.AppendLine("Senaryo;SpinNo;Bahis;BakiyeOnce;BakiyeSonra;Odenen;Cluster;Tumble;CarpanDeger;CarpanKaynak;Bonus;BonusOdenen;ArdKayipOnce;Kacis;Kategori;SpinTipi;SureMs;ClusterDetay");
            foreach (var oz in _sonuc.senaryolar)
                foreach (var s in oz.spinler)
                    detay.AppendLine($"\"{oz.senaryoAd}\";{s.spinNo};{s.bahis};{s.bakiyeOnce};{s.bakiyeSonra};{s.odenen};" +
                        $"{s.clusterSayisi};{s.tumbleSayisi};{s.carpanDeger};{s.carpanKaynak};" +
                        $"{s.bonusTetiklendi};{s.bonusOdenen};{s.ardisikKayipSayacOnce};{s.kacisFrenlemeTetik};" +
                        $"{s.kazancKategorisi};{s.spinTipi};{s.spinSureMs};\"{s.clusterDetay}\"");
            File.WriteAllText(Path.Combine(_csvKlasoru, "detay.csv"), detay.ToString(), new UTF8Encoding(true));

            // birlesik.csv (özet + detay yan yana, tek tablo)
            var birlesik = new StringBuilder();
            birlesik.AppendLine("Senaryo;SpinNo;Bahis;Odenen;CarpanDeger;Bonus;Kategori;SpinTipi");
            foreach (var oz in _sonuc.senaryolar)
                foreach (var s in oz.spinler)
                    birlesik.AppendLine($"\"{oz.senaryoAd}\";{s.spinNo};{s.bahis};{s.odenen};{s.carpanDeger};{s.bonusTetiklendi};{s.kazancKategorisi};{s.spinTipi}");
            File.WriteAllText(Path.Combine(_csvKlasoru, "birlesik.csv"), birlesik.ToString(), new UTF8Encoding(true));

            Debug.Log($"[SpinTest] CSV: {_csvKlasoru}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SpinTest] CSV hata: {e.Message}");
        }
    }

    // ─────────────────────────────────────────────
    // YARDIMCI
    // ─────────────────────────────────────────────
    private TestSonucPaketi SonucuYukle()
    {
        try
        {
            string yol = PlayerPrefs.GetString(PrefsKey_SonucDosyaYolu, "");
            if (!string.IsNullOrEmpty(yol) && File.Exists(yol))
                return JsonUtility.FromJson<TestSonucPaketi>(File.ReadAllText(yol));
        }
        catch (Exception e) { Debug.LogError($"[SpinTest] Sonuç oku hata: {e.Message}"); }
        return null;
    }

    private static TestParametreleri VarsayilanParametre()
    {
        var p = new TestParametreleri
        {
            spinSayisi = 30,
            baslangicBahis = 100,
            baslangicBakiye = 10000,
            testHizCarpani = 1f,
            verboseLog = false,
            seedManuel = false,
            randomSeed = 0,
            senaryoSecili = new bool[6]
        };
        for (int i = 0; i < p.senaryoSecili.Length; i++) p.senaryoSecili[i] = true;
        return p;
    }

    private static void Hucre(string m, bool basliksiz, float w)
    {
        var stil = basliksiz ? EditorStyles.boldLabel : EditorStyles.label;
        EditorGUILayout.LabelField(m, stil, GUILayout.Width(w));
    }

    private static void HucreRenkli(string m, Color c, float w)
    {
        var orijinal = GUI.color;
        GUI.color = c;
        EditorGUILayout.LabelField(m, EditorStyles.label, GUILayout.Width(w));
        GUI.color = orijinal;
    }

    private static Color RtpRengi(float rtp)
    {
        if (rtp < 60f) return new Color(1f, 0.4f, 0.4f);
        if (rtp > 130f) return new Color(1f, 0.65f, 0f);
        return Color.white;
    }

    private static string KisaAd(string ad)
    {
        if (string.IsNullOrEmpty(ad)) return "-";
        return ad.Length <= 26 ? ad : ad.Substring(0, 26);
    }
}
