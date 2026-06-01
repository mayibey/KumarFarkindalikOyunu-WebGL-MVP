using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Senaryo.Scripted;

public partial class OyunYoneticisi
{

    /// <summary>Bir sonraki spin'i arka planda hesaplar; grid geri yüklenir, sonuç cache'lenir. Butona basınca gecikme olmaz.</summary>
    private IEnumerator PrecomputeNextSpinCoroutine(int odenebilirLimit, bool bonusSpin)
    {
        if (grid == null || carpanDegerGrid == null || sutun <= 0 || satir <= 0) yield break;
        int[,] savedGrid = (int[,])grid.Clone();
        int[,] savedCarpan = (int[,])carpanDegerGrid.Clone();
        SpinSimulasyonKaydi kayit = SimuleEtVeKaydetImpl(odenebilirLimit, bonusSpin);
        _oncedenHesaplananKayit = kayit;
        _oncedenHesaplananBonusMu = bonusSpin;
        _oncedenHesaplananHazir = kayit != null;
        ApplyNewGridAndSync(savedGrid, savedCarpan);
        // Precompute simülasyonu ClearAllCarpanOverlays'i çalıştırır ve text'leri devre dışı bırakır;
        // grid restore edildikten sonra texleri yeniden etkinleştir.
        _izgaraServisi?.ForceRefreshCarpanTextsFromGrid();
        // FAZ35.112 ÇÖZÜM — Precompute leak temizliği (boş çarpan kesin fix).
        // KÖK NEDEN: SimuleEtVeKaydetImpl içinde Faz 35.110 helper (Simulasyon.cs:944-957)
        // sonraki spinin DOGAL/force çarpanını rastgele bir hücreye direkt yerleştirirken
        // img.sprite=carpan + scale=1.5x + text SetActive(true) GÖRSEL set yapıyor.
        // ApplyNewGridAndSync(savedGrid, savedCarpan) LOJIKSEL state'i restore eder ama
        // VISUAL state'e (img.sprite, rectTransform.localScale) dokunmaz; ForceRefreshCarpanTextsFromGrid
        // sadece text'i grid'e göre senkronlar → leak hücresi: sprite=carpan + scale=1.5x + text yok
        // = "boş çarpan" semptomu. Bu spin'in EN SON işi olduğu için Faz 35.110/35.111 re-pin'leri
        // ezilir. FIX: koşulsuz tam-grid RenderAllSprites ile nihai temizlik — CARPAN cell'leri
        // doğru görsel (sprite+1.5x+text "Nx"), NON-CARPAN cell'leri (leak dahil) temiz meyveye döner
        // (sprite=fruit + scale=1.0 + text SetActive(false)). 02 paylaşım: 02 ScriptedSpinYoneticisi.Aktif
        // playback'inde zaten Simulasyon.cs:399-402 + :542-545 defansif rerender alıyor; bu satır
        // 02 için fazladan no-op koruma (her CARPAN cell aynı sonuca set, bit-for-bit aynı davranış).
        _izgaraServisi?.RenderAllSprites(setAlphaOne: true, resetScale: true);
    }


    private void ApplyNewGridAndSync(int[,] newGrid, int[,] newCarpanGrid)
    {
        int[] oncekiCarpanDegerleri = carpanDegerByCellIndex;
        grid = newGrid;
        carpanDegerGrid = newCarpanGrid;
        _tumbleServisi?.SetGrid(grid);
        _izgaraServisi?.SetGrid(grid);
        _izgaraServisi?.SetCarpanDegerGrid(carpanDegerGrid);
        if (carpanDegerByCellIndex == null || carpanDegerByCellIndex.Length != sutun * satir)
            carpanDegerByCellIndex = new int[sutun * satir];
        for (int yy = 0; yy < satir; yy++)
        {
            for (int xx = 0; xx < sutun; xx++)
            {
                int cidx2 = _izgaraServisi != null ? _izgaraServisi.XYToIndex(xx, yy) : 0;
                if (cidx2 < 0 || cidx2 >= carpanDegerByCellIndex.Length) continue;
                if (grid[xx, yy] == CARPAN_SEMBOL)
                {
                    int yeniDeger = carpanDegerGrid != null ? carpanDegerGrid[xx, yy] : 0;
                    if (yeniDeger <= 0 && oncekiCarpanDegerleri != null && cidx2 < oncekiCarpanDegerleri.Length)
                    {
                        // Geçici senkron kaymalarında bomba üstündeki xN değeri kaybolmasın.
                        yeniDeger = oncekiCarpanDegerleri[cidx2];
                        if (yeniDeger > 0 && carpanDegerGrid != null)
                            carpanDegerGrid[xx, yy] = yeniDeger;
                    }
                    carpanDegerByCellIndex[cidx2] = Mathf.Max(0, yeniDeger);
                }
                else
                {
                    carpanDegerByCellIndex[cidx2] = 0;
                }
            }
        }
    }


    private void OncedenHesaplananSpinOnbelleginiTemizle()
    {
        _oncedenHesaplananKayit = null;
        _oncedenHesaplananHazir = false;
    }

    /// <summary>
    /// ScriptedSpinYoneticisi aktif olduğunda çağrılır: önbellek RNG akışıyla doldurulmuş olabilir,
    /// onu temizleyip yeniden precompute tetikler — bu kez scripted hook devreye girip scripted kaydı cache'ler.
    /// Diğer durumlarda (sahne değişimi, manuel reset) da güvenle çağrılabilir.
    /// </summary>
    public void ScriptedSenaryoCacheTazele()
    {
        OncedenHesaplananSpinOnbelleginiTemizle();
        if (grid == null || sutun <= 0 || satir <= 0)
        {
            Debug.Log("[ScriptedSenaryoCacheTazele] Grid hazır değil; precompute atlandı.");
            return;
        }
        int limit = _odemeServisi != null ? _odemeServisi.GetSpinOdenebilirLimit() : int.MaxValue;
        StartCoroutine(PrecomputeNextSpinCoroutine(limit, false));
        Debug.Log("[ScriptedSenaryoCacheTazele] Önbellek temizlendi ve yeniden precompute tetiklendi.");
    }


    /// <summary>Üst üste kayıp 0 + kazanç N: N spinlik video modu sayacını kutu değerlerinden doldurur.</summary>

    /// <summary>
    /// Kayıttaki her tumble adımının TurKazanci değerinin, o adımdaki grid + patlayan hücreler için paytable ile uyumlu olduğunu doğrular.
    /// </summary>
    private bool SpinKaydiHamPaytableIleUyumluMu(SpinSimulasyonKaydi kayit, int bahis, int tumbleEsik)
    {
        if (kayit == null || tumbleAyarlari == null) return true;
        int sat = kayit.Satir;
        int sut = kayit.Sutun;
        if (kayit.IlkGrid == null || sat <= 0 || sut <= 0) return true;
        if (kayit.IlkGrid.GetLength(0) != sut || kayit.IlkGrid.GetLength(1) != sat) return true;

        int adimSayisi = kayit.Adimlar != null ? kayit.Adimlar.Count : 0;
        if (adimSayisi == 0)
            return kayit.ToplamHamKazanc <= 0;

        int[,] g = kayit.IlkGrid;
        int toplamHesap = 0;
        for (int i = 0; i < adimSayisi; i++)
        {
            var adim = kayit.Adimlar[i];
            if (adim == null) continue;
            var pat = adim.PatlayanHucreler;
            if (pat == null || pat.Count == 0)
            {
                if (adim.TurKazanci != 0)
                    return false;
            }
            else
            {
                int beklenen = tumbleAyarlari.CalculateWinWithOwnPayTable(pat, g, sat, sut, bahis, tumbleEsik);
                if (beklenen != adim.TurKazanci)
                    return false;
                toplamHesap += adim.TurKazanci;
            }

            if (adim.GridRefillSonrasi != null
                && adim.GridRefillSonrasi.GetLength(0) == sut
                && adim.GridRefillSonrasi.GetLength(1) == sat)
                g = adim.GridRefillSonrasi;
        }

        return toplamHesap == kayit.ToplamHamKazanc;
    }


    /// <summary>İlk spin'i hemen arka planda hesaplatır; basınca bekleme olmasın diye precompute anında başlar.</summary>
    private IEnumerator IlkSpinPrecomputeGecikmeli()
    {
        // Oyun açılışında yalnızca kayıtlı ayarı baz al: geçmiş spin/faz kaldığı yerden devam etmesin.
        OncedenHesaplananSpinOnbelleginiTemizle();
        int limit = _odemeServisi != null ? _odemeServisi.GetSpinOdenebilirLimit() : int.MaxValue;
        StartCoroutine(PrecomputeNextSpinCoroutine(limit, false));
        yield break;
    }

    
    public void SpinButon()
    {
        _donusServisi?.SpinButon();
    }


    /// <summary>Sentetik oyuncu / bot test katmanının spin ve bonus durumunu okuması için.</summary>
    public bool SpinCalisiyorMu => spinCalisiyor;
    /// <summary>Sentetik oyuncu / bot test katmanının bonus durumunu okuması için.</summary>
    public bool BonusAktifMi => bonusAktif;
    /// <summary>Bonus oyun sırasında kalan free spin sayısı (HUD takibi için).</summary>
    public int BonusHakKalan => bonusHakKalan;
    /// <summary>Oturum boyunca biriken toplam kazanç TL (bonus HUD + istatistik).</summary>
    public int OturumKazanc => oturumKazanc;
    /// <summary>Bot: bakiye (dönüş atılabilir mi kontrolü).</summary>
    public int BotIcinBakiye => _ekonomiServisi != null ? _ekonomiServisi.Bakiye : 0;
    /// <summary>Bot: mevcut bahis.</summary>
    public int BotIcinBahis => _ekonomiServisi != null ? _ekonomiServisi.Bahis : 0;


    // ÖNCE-modal mekanizması: belirli (asama, spin) kombinasyonlarında SPIN tıklamasında ÖNCE
    // pedagojik modal açılır, kullanıcı TAMAM'a basınca asıl spin atılır. Tek seferlik flag'ler
    // (sahne reset → yeni instance, otomatik sıfır).
    private bool _onceModalA1S7Gosterildi = false;
    private bool _onceModalA2S4Gosterildi = false;

    private void SpinButonImpl()
    {
        Debug.Log("[SpinButon-DEBUG] === BUTONA BASILDI ===");

        if (bonusAktif) { Debug.Log("[SpinButon-DEBUG] RETURN: bonusAktif=true"); return; }
        if (spinCalisiyor) { Debug.Log("[SpinButon-DEBUG] RETURN: spinCalisiyor=true"); return; }

        // SCRIPTED MOD — bloke eden paneller açıkken spin atımı engellenir.
        // - ScriptedYuklemePaneli (A6 borç al)
        // - ScriptedFinalEkrani (A7 cutscene; "Yeniden başla" butonuyla sahne resetlenir)
        // - ScriptedBonusTuzagiPopup (A5 Spin 4 — cazip tuzak pop-up, kullanıcı onayı bekler)
        // - ScriptedBonusOyunUygulayici (A5 Spin 4 — bonus oyun panel + animasyon)
        // - ScriptedModalKopru (eğitmen modal — sahne reload sonrası Pre-A1 + ÖNCE modallar)
        // - ScriptedDusunceBalonu (A5 sonu düşünce balonu)
        if (ScriptedYuklemePaneli.IsAcik) { Debug.Log("[SpinButon-DEBUG] RETURN: ScriptedYuklemePaneli.IsAcik=true"); return; }
        if (ScriptedFinalEkrani.IsAcik) { Debug.Log("[SpinButon-DEBUG] RETURN: ScriptedFinalEkrani.IsAcik=true"); return; }
        if (ScriptedBonusTuzagiPopup.IsAcik) { Debug.Log("[SpinButon-DEBUG] RETURN: ScriptedBonusTuzagiPopup.IsAcik=true"); return; }
        if (ScriptedBonusOyunUygulayici.IsAcik) { Debug.Log("[SpinButon-DEBUG] RETURN: ScriptedBonusOyunUygulayici.IsAcik=true"); return; }
        if (ScriptedModalKopru.ModalAcik) { Debug.Log("[SpinButon-DEBUG] RETURN: ScriptedModalKopru.ModalAcik=true"); return; }
        // Tutorial sahnesi (idx=3) modal guard'ı — Space tuşu / spin butonu modal açıkken yutulur.
        if (KumarFarkindalik.Tutorial.TutorialModalKopru.ModalAcik) { Debug.Log("[SpinButon-DEBUG] RETURN: TutorialModalKopru.ModalAcik=true"); return; }
        // Tutorial parametre bekleyen adım — Space tuşu (mouse overlay tarafından yutulmuyor) için global guard + uyarı.
        if (KumarFarkindalik.Tutorial.TutorialOyunYoneticisi.SpinKilitli) { KumarFarkindalik.Tutorial.TutorialOyunYoneticisi.Ornek?.SpinKilitliUyariGoster(); return; }
        // BIG/MEGA/EPIC WIN pop-up sayma animasyonu sürerken Space spin'i arka planda atıyordu — pop-up bitene kadar yut.
        if (winFeedbackUI != null && winFeedbackUI.GosterimAktif) { Debug.Log("[SpinButon-DEBUG] RETURN: WinFeedbackUI.GosterimAktif=true"); return; }
        if (ScriptedDusunceBalonu.BalonAcik) { Debug.Log("[SpinButon-DEBUG] RETURN: ScriptedDusunceBalonu.BalonAcik=true"); return; }
        if (AnlaticiSeritKopru.BonusBitisAcik) { Debug.Log("[SpinButon-DEBUG] RETURN: AnlaticiSeritKopru.BonusBitisAcik=true"); return; }
        // Race condition fix: A* özel akış coroutine'i (modal + WaitForSeconds delay) sürüyorsa engelle.
        if (AnlaticiSeritKopru.AnlaticiOzelAkisAktif) { Debug.Log("[SpinButon-DEBUG] RETURN: AnlaticiOzelAkisAktif=true (özel akış sürüyor)"); return; }

        // ÖNCE-modal kontrolü: A1 Spin 7 (idx 6) ve A2 Spin 4 (idx 3) tıklandığında önce pedagojik
        // modal açılır, bittiğinde SpinButon tekrar çağrılır → bu sefer flag set olduğu için asıl spin atılır.
        var anlatici = AnlaticiSeritKopru.Ornek;
        if (anlatici != null)
        {
            int asama = anlatici.AktifAsama;
            int spinIdx = anlatici.AsamadakiSpinSayaci;
            Debug.Log($"[SpinButon-DEBUG] AktifAsama={asama}, AsamadakiSpinSayaci={spinIdx}");
            if (asama == 0 && spinIdx == 6 && !_onceModalA1S7Gosterildi)
            {
                _onceModalA1S7Gosterildi = true;
                Debug.Log("[SpinButon-DEBUG] RETURN: A1S7 ÖNCE-modal tetiklendi");
                StartCoroutine(OnceModalGosterVeSpin(
                    "Şimdi <color=#4ADE80>büyük bir kazanç</color> gelecek. Bu <color=#EF4444>kasıtlı</color>: algoritma oyuncuyu <color=#60A5FA><i>'şanslıyım'</i></color> hissine kaptırmak istiyor.\n\n" +
                    "Kazanç sonrası oyuncunun zihninde <color=#60A5FA><i>'ben kazanırım'</i></color> duygusu yerleşecek."
                ));
                return;
            }
            if (asama == 1 && spinIdx == 3 && !_onceModalA2S4Gosterildi)
            {
                _onceModalA2S4Gosterildi = true;
                Debug.Log("[SpinButon-DEBUG] RETURN: A2S4 ÖNCE-modal tetiklendi");
                StartCoroutine(OnceModalGosterVeSpin(
                    "Şu an oyuncu <color=#FB923C>bahisini değiştirecek</color> (yükseltecek). Bu bahisin ardından algoritma <color=#EF4444>kasıtlı olarak</color> kazanç yaşatacak.\n\n" +
                    "Amaç: oyuncuya <color=#60A5FA><i>'doğru zamanda doğru bahis'</i></color> duygusu vermek. Böylece oyuncu <color=#60A5FA>kontrolün kendinde olduğuna</color> inanır."
                ));
                return;
            }
        }
        else
        {
            Debug.Log("[SpinButon-DEBUG] AnlaticiSeritKopru.Ornek=null (senaryo dışı sahne)");
        }

        if (_ekonomiServisi.Bakiye < _ekonomiServisi.Bahis)
        {
            Debug.Log($"[SpinButon-DEBUG] RETURN: bakiye yetersiz (Bakiye={_ekonomiServisi.Bakiye}, Bahis={_ekonomiServisi.Bahis})");
            // Bakiye yetersiz: paneli aç, "Bakiye azalıyor, yükleme yapmak ister misin?" uyarısı göster.
            ShowBakiyeYuklePanel(yetersizBakiyeUyarisi: true);
            return;
        }
        Debug.Log("[SpinButon-DEBUG] Tüm kontroller geçti, spin başlatılıyor (BirSpinHazirlaVeAt)");
        BaslatGeciciGlobalTiklamaKilidi(2f);
        // Spin butonuna basıldığı ilk karede eski spin sonucu görünmesin.
        // DonusAkisServisi bunu tekrar başta sıfırlıyor; burada amaç sadece erken UI sıçramasını engellemek.
        spinKazancHam = 0;
        tumbleToplamKazanc = 0;
        sonSpinKazancHamGoster = 0;
        sonSpinCarpanGoster = 1;
        sonSpinKazancToplamGoster = 0;
        sonSpinKazanci = 0;
        _spinKazanciOturumaEklendi = false;
        _carpanKutuUcusFormulKilit = false;
        _carpanKutuUcusBirikimSonDeger = 0;
        _carpanKutuUcusBirikimGosterMax = 0;
        _normalSpinSonucSesiBuSpinCaldi = false;
        _normalSpinSonucPopupCalisiyor = false;
        spinCalisiyor = true;
        _uiServisi?.ButonDurumu(false);
        if (spinIcon != null) spinIcon.SetRotate(true);
        _uiServisi?.UI_Guncelle();
        StartCoroutine(BirSpinHazirlaVeAt());
    }


    /// <summary>Tek bir spin için bahis düşümü + kayıt + NormalSpinAkisi. Otomatik spin döngüsü her turda bunu çağırır.</summary>
    private IEnumerator BirSpinHazirlaVeAt()
    {
        int mevcutBahis = _ekonomiServisi.Bahis;
        // Çift şans bileşeni kaldırıldı (2026-04-30) — spin maliyeti = bahisin kendisi (1.5x mantığı yok)
        int spinMaliyeti = mevcutBahis;
        // "Bahis artırma" sayacı: sadece yeni spin başlarken ve bahis gerçekten bir önceki spin bahisinden yüksekse artar.
        if (_sonSpinBaslangicBahis >= 0 && mevcutBahis > _sonSpinBaslangicBahis)
            SenaryoYoneticisi.I?.BahisArtirimiYapildi();
        _sonSpinBaslangicBahis = mevcutBahis;

        _spinOdenebilirLimit = _odemeServisi != null ? _odemeServisi.GetSpinOdenebilirLimit() : int.MaxValue;
        _spinPrevBakiye = _ekonomiServisi.Bakiye;
        _spinBahisTL = spinMaliyeti;
        _odemeServisi?.AddBahisToKasa(spinMaliyeti);
        _ekonomiServisi.DeductSpinMaliyeti(spinMaliyeti);
        _logServisi?.RecordSpinStart(_spinPrevBakiye, _ekonomiServisi.Bakiye, _spinBahisTL, _spinOdenebilirLimit);
        _uiServisi?.UI_Guncelle();
        yield return _donusServisi.NormalSpinAkisi();

        // Anlatıcı Şerit'e spin sonu bildir (varsa) — 7 aşamalı manipülasyon hikayesi ilerlemesi
        var anlatici = AnlaticiSeritKopru.Ornek;
        if (anlatici != null) anlatici.SpinAtildi();
    }


    private const int SIMULASYON_MAX_REROLL = 28;
    private const int SIMULASYON_MAX_REROLL_ZORLA_CARPAN_TUMBLE = 80;

    /// <summary>Anlatıcı aktifken aşamaya göre reroll bütçesi: yüksek RTP isteyen aşamada (1-2) çok dene,
    /// tükeniş aşamasında (5-7) az dene. RNG'den uygun ödeme bulunamazsa bant zorlanmadan fallback olur.
    /// FAZ35.83: Mod aktif (odemeMinKat>0 && odemeMaksKat>0) iken reroll=500.
    /// FAZ35.85 K4: ModKazancKonstrukte başarılıysa 28 yeter (grid hedefli, ilk deneme kabul edilir),
    /// başarısızsa 500 fallback (RNG umudu) — akıllı bütçe.</summary>
    private int AsamaIcinMaxReroll()
    {
        // FAZ35.83/35.85: Yeni 03 admin (Anlatıcı yok) + mod aktif → konstrukte durumuna göre akıllı reroll.
        var anlaticiCheck83 = AnlaticiSeritKopru.Ornek;
        if (anlaticiCheck83 == null && odemeMinKat > 0f && odemeMaksKat > 0f)
            return _modKonstrukteBasarili ? 28 : 500;

        var anlatici = AnlaticiSeritKopru.Ornek;
        if (anlatici == null) return SIMULASYON_MAX_REROLL;
        int asama = anlatici.AktifAsama;
        if (asama < 0) return SIMULASYON_MAX_REROLL;
        if (asama == 0) return 2000; // 1 Isındırma — cömertlik zirvesi
        if (asama == 1) return 1500; // 2 Kontrol Bende
        if (asama == 2) return 500;  // 3 Geri Kazanabilirim
        if (asama == 3) return 200;  // 4 Şansın Döndü
        if (asama == 4) return 100;  // 5 Sonu Düşünmeyen
        if (asama == 5) return 50;   // 6 Para Bulmalıyım
        return 20;                   // 7 Tükeniş — minimal reroll, kayıp serbest
    }


    /// <summary>Spin politikası her zaman atanmış olmalı; null ise fabrika ile üretilir.</summary>

    private SenaryoSpinPolitikasiBaglami OlusturSpinPolitikasiBaglami(bool ustUsteAktif)
    {
        return new SenaryoSpinPolitikasiBaglami(
            ustUsteAktif,
            0,
            0,
            IsAdminSenaryo1Veya2Veya3Aktif() || IsAdminSenaryo4Aktif() || IsAdminSenaryo5Aktif());
    }

    // ──────────────────────────────────────────────────────────────────

    // ──────────────────────────────────────────────────────────────────
    //  SENARYO 5  (K→KY→BOMB_500x)
    // ──────────────────────────────────────────────────────────────────

    private SpinSimulasyonKaydi SimuleEtVeKaydetImpl(int odenebilirLimit, bool bonusSpin)
    {
        // PAKET 14-FAZ33 — TUTORIAL SCRIPTED MODE (build idx 3): KumarFarkindalik.Tutorial.TutorialScriptedYoneticisi
        // aktifse RNG bypass + cache validation bypass. ScriptedSpinUygulayici altyapısı (03 referans) Tutorial
        // T4/T5 pattern motor saatler süren bug'larını çözmek için. Tutorial namespace içinde Aktif=false ise
        // pattern motor (TutorialSenaryoMotoru) eski path çalışır (fallback). 03 sahnesinde (idx 1) bu branch
        // koşulu FALSE → ATLANIR, 03 davranışı %100 KORUNUR.
        // Faz 35.37: 02 silindi, idx kaydı (3 → 2).
        // FAZ35.77: Yeni 03_AdminOyunScene (Tutorial'sız) idx 2'ye girince eski 04 (Tutorial) idx 2→3.
        if (SceneManager.GetActiveScene().buildIndex == 3
            && KumarFarkindalik.Tutorial.TutorialScriptedYoneticisi.Aktif
            && KumarFarkindalik.Tutorial.TutorialScriptedYoneticisi.Ornek != null)
        {
            var tutorialKayit = KumarFarkindalik.Tutorial.TutorialScriptedYoneticisi.Ornek.SonrakiSpiniAl(bonusSpin);
            if (tutorialKayit != null)
            {
                int gercekBahisT = _ekonomiServisi != null ? _ekonomiServisi.Bahis : tutorialKayit.bahis;
                Debug.Log($"[TutorialScripted] Spin RNG bypass, brüt {tutorialKayit.tutorialOdemeHam} TL (bahis={gercekBahisT})");
                return ScriptedSpinUygulayici.UygulaKaydi(tutorialKayit, this, gercekBahisT);
            }
        }

        // SCRIPTED MODE — anlatıcı sahnesinde (build idx 2) scripted senaryo aktifse RNG bypass.
        // - Normal spin (bonusSpin=false): aşama+spin sırasından kayıt al (asamaSpinleri[asamaIdx].spinler[spinIdx]).
        // - Bonus spin (bonusSpin=true && _scriptedBonusBahisOverride): bonusSpinleri[bonusIdx] al → motor RTP devre dışı,
        //   10 sabit spin × toplam 4000 TL garanti (paytable doğrulanmış).
        if (ScriptedSpinYoneticisi.Aktif && ScriptedSpinYoneticisi.Ornek != null)
        {
            ScriptedSpinKaydi scriptedKayit = null;
            if (bonusSpin && _scriptedBonusBahisOverride)
            {
                // Bonus spin idx hesabı: bonusHakBaslangic 10, BonusHakKalan azalır → 0..9
                int bonusSpinIdx = bonusHakBaslangic - bonusHakKalan;
                scriptedKayit = ScriptedSpinYoneticisi.Ornek.SonrakiBonusSpiniAl(bonusSpinIdx);
                if (scriptedKayit != null)
                    Debug.Log($"[ScriptedBonus] Spin {bonusSpinIdx + 1}/10 — RNG bypass, brüt {scriptedKayit.tutorialOdemeHam} TL");
            }
            else if (!bonusSpin)
            {
                int asamaIdx = AnlaticiSeritKopru.Ornek != null ? AnlaticiSeritKopru.Ornek.AktifAsama : 0;
                int spinIdx = AnlaticiSeritKopru.Ornek != null ? AnlaticiSeritKopru.Ornek.AsamadakiSpinSayaci : 0;
                scriptedKayit = ScriptedSpinYoneticisi.Ornek.SonrakiSpiniAl(asamaIdx, spinIdx);
                if (scriptedKayit != null)
                    Debug.Log($"[Scripted] Aşama {asamaIdx + 1} Spin {spinIdx + 1} — RNG bypass, brüt {scriptedKayit.tutorialOdemeHam}");
            }
            if (scriptedKayit != null)
            {
                int gercekBahis = _ekonomiServisi != null ? _ekonomiServisi.Bahis : scriptedKayit.bahis;
                return ScriptedSpinUygulayici.UygulaKaydi(scriptedKayit, this, gercekBahis);
            }
        }

        _bombaPatlamaSonrasiIlkRefillCarpanEngeli = false;
        // FAZ35.84: Reroll sayacı reset (her spin başı). Scripted/konstrukte erken return path'leri -1 kalır,
        // ana reroll loop return öncesi deneme sayısı set edilir (debug analiz için).
        _sonSpinRerollSayisi = -1;
        bool adminManuelMod = _adminManuelZorlukKilidi || AdminOyunSahnesiMi();

        int limit = bonusSpin ? (_senaryoServisi != null ? _senaryoServisi.GetBonusRemainingPayableTL() : int.MaxValue) : odenebilirLimit;
        if (bonusSpin && _senaryo5BonusCuziLimitAktif)
            limit = Mathf.Min(limit, Mathf.Max(1, (_ekonomiServisi?.Bahis ?? 200) / 5));
        int zorlaCarpanDegeri = zorlaSiradakiCarpan;
        // Force değeri tek atımlık olmalı: spin başında tüket, global state'te taşınmasın.
        if (zorlaCarpanDegeri > 0)
        {
            zorlaSiradakiCarpan = 0;
            if (carpanAyarlari != null)
                carpanAyarlari.ZorlaSiradakiCarpan = 0;
        }
        // FAZ35.82: Tutma mod deterministik döngü (2-kayıp-1-kazanç, sonsuz).
        // _tutmaBuSpinKayipBekleniyor her spin başında reset → OdemeModelineUygunMu reroll'leri içinde sabit kalır.
        // Senaryo 1-5 aktif iken bypass (Senaryo motorları kendi yolundan ilerler).
        _tutmaBuSpinKayipBekleniyor = false;
        if (_tutmaModAktif && !bonusSpin && zorlaCarpanDegeri <= 0
            && !IsAdminSenaryo1Aktif() && !IsAdminSenaryo2Aktif() && !IsAdminSenaryo3Aktif()
            && !IsAdminSenaryo4Aktif() && !IsAdminSenaryo5Aktif())
        {
            int faz = _tutmaModSpinSayac % 3;
            if (faz == 2)
            {
                // Kazanç spin: mevcut cluster zorlama bayrağını kullan (35.81 Min/Maks bant 1x-2x ile reroll'da bantlanır).
                _kacisFrenlemeBuSpinAktif = true;
                OncedenHesaplananSpinOnbelleginiTemizle();
            }
            else
            {
                // Kayıp spin (faz 0/1): OdemeModelineUygunMu içinde nihaiOdeme>0 → reroll zorlar.
                _tutmaBuSpinKayipBekleniyor = true;
            }
            _tutmaModSpinSayac = (_tutmaModSpinSayac + 1) % 3;
            Debug.Log($"[TUTMA SPIN] faz={faz} ({(faz < 2 ? "kayıp" : "kazanç")}), sonraki sayaç={_tutmaModSpinSayac}");
        }

        // FAZ35.85: Mod kazanç AKTİF konstrukte branch (Senaryo 1 motoru HedefOdemeMotorBase reuse).
        // KÖK NEDEN: PASİF reroll RNG'yi sadece kabul/reddet ediyordu, %90 eğilim olsa bile RNG cluster üretmezse kazanç gelmiyordu.
        // ÇÖZÜM: Eğilim'e göre kazanç bekleniyorsa motor hedef TL seçer + paytable'dan sembol+küme bulur + grid'e enjekte eder.
        // 02 anlatıcı (ScriptedSpinYoneticisi.Aktif) ve eski 04 Tutorial (TutorialScriptedYoneticisi.Aktif) sat ~344-385'te ERKEN RETURN → bu branch'a girmez.
        // Senaryo 1-5 guard'ları bypass + Tutma kayıp fazı (_tutmaBuSpinKayipBekleniyor) bypass + zorlaCarpan bypass.
        // FAZ35.107 SORUN A: DOGAL çarpan RNG check SPIN BAŞINDA bir kez (eski Faz 35.105 reroll loop içinde her iterasyonda RNG yapıyordu → deneme==0 guard ile pas geçer → çarpan kaybı).
        // Yeni yapı: spin başında deterministik flag set, reroll loop İÇİNDE flag check + tek seferlik yerleştirme.
        // Detaylı debug log: F12'de hangi guard pas geçiriyor net teşhis.
        bool _dogalCarpanBuSpinAktif = false;
        int _dogalCarpanDeger = 0;
        // FAZ35.115: Random 1..N adet çarpan (panel "Tek spinde max kaç çarpan" ayarı — maxCarpanAdedi field).
        // Eski davranış: helper sabit TEK çarpan yerleştiriyordu, max ayarı kullanılmıyordu (kullanıcı şikayeti).
        // Yeni: flag set sırasında 1..maxCarpanAdedi inclusive random adet seçilir, placement N kez helper çağırır.
        // Çoklu çarpan ödemede toplanır (CarpanServisi.cs:163 _spinCarpanCarpim += c, Sweet Bonanza tarzı, Tutorial T5 ile tutarlı).
        int _dogalCarpanAdet = 0;
        {
            float _dogalOlasilik = carpanUretimOlasiligi;
            float _dogalRng = UnityEngine.Random.value;
            bool _gA = (zorlaCarpanDegeri <= 0);
            bool _gB = (PanelKopru.aktifSenaryo == "normal");
            bool _gC = carpanUretimiAktif;
            bool _gD = !_modKonstrukteBasarili;
            bool _gE = (_dogalOlasilik > 0f && _dogalRng <= _dogalOlasilik);
            Debug.Log($"[FAZ35.107 DOGAL] olasilik={_dogalOlasilik:F2} rng={_dogalRng:F2} | zorlaYok={_gA} senaryoNormal={_gB} carpanAktif={_gC} modKonstrukteOK={_gD} rngGecti={_gE}");
            if (_gA && _gB && _gC && _gD && _gE)
            {
                _dogalCarpanDeger = RastgeleCarpan();
                if (_dogalCarpanDeger > 0)
                {
                    _dogalCarpanBuSpinAktif = true;
                    // FAZ35.115: 1..maxCarpanAdedi inclusive random (Random.Range int üst sınır exclusive → +1).
                    // maxCarpanAdedi=1 ise her zaman 1; =5 ise 1-5 arası random adet.
                    int _maxAdet = Mathf.Max(1, maxCarpanAdedi);
                    _dogalCarpanAdet = UnityEngine.Random.Range(1, _maxAdet + 1);
                    Debug.Log($"[FAZ35.107 DOGAL] FLAG SET — ilk değer örneği=x{_dogalCarpanDeger}, adet={_dogalCarpanAdet} (max={_maxAdet}), reroll loop her iter'da N kez yerleştirilecek.");
                }
                else
                {
                    Debug.Log($"[FAZ35.107 DOGAL] RastgeleCarpan 0 döndü, yerleşim YOK.");
                }
            }
        }

        // FAZ35.104 İŞ1 BUG 2: Bonus girme olasılığı RNG check (sadece flag set — ASIL ENJEKSİYON reroll loop'a taşındı).
        // KÖK NEDEN: Önceki nokta (Faz 35.103 İŞ2) burada Force4ScatterEnjekte çağırıyordu AMA `grid` field bu noktada eski state.
        // Reroll loop sat ~697'de FillRandomAll RNG sembollerle grid'i dolduruyor → benim 4 scatter'ım SİLİNİYORDU.
        // Kullanıcı log kanıtı: "🧪 BonusKontrol: ScatterSay(ilk)=1" — IlkGrid'de sadece 1 RNG scatter, force enjeksiyon kayboldu.
        // ÇÖZÜM: Spin başında SADECE flag set (RNG check deterministic). Enjeksiyon reroll loop içinde FillRandomAll SONRASI.
        bool _bonusGirmeBuSpinAktif = !bonusSpin
            && zorlaCarpanDegeri <= 0
            && PanelKopru.aktifSenaryo == "normal"
            && _bonusGirmeOlasilik > 0f
            && UnityEngine.Random.value <= _bonusGirmeOlasilik;

        _modKonstrukteBasarili = false;
        _modSpinBekleniyorKazanc = false;
        // FAZ35.101 İŞ1: Normal mod'da min/max çarpan kat slider'ları motor konstrukteyi tetiklememeli.
        // Mod konstrukte SADECE Senaryolu modlar için (Hook/Yontma/Tutma/Koruma). Normal mod = baseline (Faz 35.95 RTP korunur).
        // Önceden kullanıcı Normal mod + min=1, max=2 set edince mod konstrukte aktif → _modKonstrukteBasarili=true →
        // Faz 35.93 SetIsCarpanUretimiAktif guard çarpan üretimini KAPATIYORDU (slider %100 etkisiz, BUG A+B tek kök).
        if (!bonusSpin && zorlaCarpanDegeri <= 0
            && odemeMinKat > 0f && odemeMaksKat > 0f
            && !_tutmaBuSpinKayipBekleniyor
            && !IsAdminSenaryo1Aktif() && !IsAdminSenaryo2Aktif()
            && !IsAdminSenaryo3Aktif() && !IsAdminSenaryo4Aktif() && !IsAdminSenaryo5Aktif()
            && PanelKopru.aktifSenaryo != "normal")
        {
            // K5: Spin başında 1 kez eğilim kararı (reroll'larda OdemeModelineUygunMu aynı field'ı kullanır → tutarlılık).
            _modSpinBekleniyorKazanc = UnityEngine.Random.value <= Mathf.Clamp01(_odemeEgilimiYuzde / 100f);
            // K2: Tutma kazanç fazı (faz==2) → ModKazancKonstrukte üzerinden bant garantili kazanç.
            //     _kacisFrenlemeBuSpinAktif fallback olarak korunur (ModKazancKonstrukte başarısızsa cluster zorla devreye girer).
            if (_tutmaModAktif && (_tutmaModSpinSayac % 3) == 0) // SONRAKİ sayaç 0 ise BU spin faz 2'ydi (kazanç fazı sonu)
            {
                // Tutma sayacı zaten ilerletildi (yukarıda); bu spin'in faz tipini geri çıkarmak için sonraki sayaç=0 → bu spin faz==2 idi.
                _modSpinBekleniyorKazanc = true;
            }

            if (_modSpinBekleniyorKazanc)
            {
                int bahisIcinKonstrukte = _ekonomiServisi != null ? _ekonomiServisi.Bahis : 0;
                if (TryModKazancKonstrukteGridKur(bahisIcinKonstrukte))
                {
                    _modKonstrukteBasarili = true;
                    // FAZ35.88 İŞ1: Bonus çarpan ezilme fix (35.87 İŞ3 regression).
                    // KÖK NEDEN: Helper içinde zorlaSiradakiCarpan=bonusCarpan set ediliyor, AMA ana reroll loop sat ~657'de
                    // "zorlaSiradakiCarpan = zorlaCarpanDegeri" (local, başlangıç 0) ile EZILIYOR → bonus iptal oluyordu.
                    // FIX: Local zorlaCarpanDegeri'yi de bonus değerine senkronize et — sat 657'de aynı değer set edilir.
                    if (_modSonBonusCarpan > 1) zorlaCarpanDegeri = _modSonBonusCarpan;
                    OncedenHesaplananSpinOnbelleginiTemizle();
                    // FAZ35.85 K2: ModKazancKonstrukte başarılı → kaçış frenleme cluster zorla fallback'i devre dışı bırak
                    // (Tutma faz==2 zaten yukarıda _kacisFrenlemeBuSpinAktif=true set ediyordu; konstrukte garantili → çakışma önle).
                    // Konstrukte başarısızsa Tutma branch'ın set ettiği _kacisFrenlemeBuSpinAktif aynen kalır (güvenlik ağı).
                    _kacisFrenlemeBuSpinAktif = false;
                }
            }
        }

        // Kaçış Frenleme: ardışık kayıp eşiği aşıldığında bir önceki spin sonu flag set etmiştir.
        // Bu spinde grid cluster oluşacak şekilde zorlanır; flag tek atımlık (spin başında tüketilir).
        // Bonus / zorla çarpan path'lerinde çakışma olmasın diye sadece normal spinde uygulanır.
        bool kacisFrenlemeUygula = _kacisFrenlemeBuSpinAktif && !bonusSpin && zorlaCarpanDegeri <= 0;
        if (_kacisFrenlemeBuSpinAktif)
            _kacisFrenlemeBuSpinAktif = false;
        // Force carpan aktifse toggle durumundan bağımsız çarpan üretimi açık sayılır.
        bool carpanToggleSecili = zorlaCarpanDegeri > 0 ? true : carpanUretimiAktif;
        if (zorlaCarpanDegeri > 0)
            limit = int.MaxValue;
        int maxReroll = (zorlaCarpanDegeri > 0) ? SIMULASYON_MAX_REROLL_ZORLA_CARPAN_TUMBLE : AsamaIcinMaxReroll();
        bool ustUsteAktif = !bonusSpin && zorlaCarpanDegeri <= 0 && false;
        var spinPolitikasi = SpinPolitikasiniAl();
        var spinPolitikaBaglam = OlusturSpinPolitikasiBaglami(ustUsteAktif);
        spinPolitikasi.SimulasyonRerollUstSiniriniUygula(ref maxReroll, spinPolitikaBaglam);
        bool _dbgBeklenenKazanc = false;
        bool senaryo1KazancBandiZorunlu = !bonusSpin
            && zorlaCarpanDegeri <= 0
            && (IsAdminSenaryo1Aktif()
                || (SenaryoYoneticisi.I != null && SenaryoYoneticisi.I.mevcutAsama == SenaryoYoneticisi.SenaryoAsama.Asama1_IsindirmaUmut))
            && _dbgBeklenenKazanc;
        Debug.Log($"[SEN1_DBG] IsAdminSen1={IsAdminSenaryo1Aktif()} | KazancFaziAktif={false} | FazdaKalan={0} | KazancHedef={0} | KayipHedef={0} | BeklenenKazanc={_dbgBeklenenKazanc} | senaryo1KazancBandiZorunlu={senaryo1KazancBandiZorunlu} | maxReroll={maxReroll}");
        if (senaryo1KazancBandiZorunlu)
            maxReroll = Mathf.Max(maxReroll, 2500);
        bool adminVideoArdisikKazanc = !bonusSpin && AdminOyunSahnesiMi() && 0 == 0 && _adminVideoArdisikKazancSpinKalan > 0;
        bool zorunluBosSpin = !bonusSpin && !adminManuelMod && !adminVideoArdisikKazanc && SenaryoYoneticisi.I != null && SenaryoYoneticisi.I.ShouldForceNoPaySenaryo12();
        if (spinPolitikasi.ZorunluBosSpinAdminPresetYuzundenDevreDisiBirakilsin(spinPolitikaBaglam))
            zorunluBosSpin = false;
        if (zorunluBosSpin) maxReroll = Mathf.Max(maxReroll, 40);
        SpinSimulasyonKaydi sonKayit = null;
        SpinSimulasyonKaydi sonDenemeKayit = null;

        // Senaryo 1 kazanç bandı: akış (1) önce hedef nihai ödeme → (2) rastgele dizilim + tumble ile uygun sonuç ara → (3) kayıt oynatılır.
        if (senaryo1KazancBandiZorunlu)
        {
            _senaryo1SonZorunluNihaiOdeme = Senaryo1HedefOdemeMotoru.HedefNihaiOdemeSec(0, 2000, 50);
            Debug.Log($"[SENARYO1][AKIS] (1) Hedef nihai ödeme={_senaryo1SonZorunluNihaiOdeme} TL (bant {0}–{2000}, dağılım %{50}) → (2) paytable-uyumlu dizilim → (3) oynatma");
        }

        if (senaryo1KazancBandiZorunlu && !bonusSpin && zorlaCarpanDegeri <= 0)
        {
            // İlk deneme: iki-tumble + tek-tumble planlama
            SpinSimulasyonKaydi konstrukte = Senaryo1PaytableKonstrukteHedefSpinDene(
                limit, bonusSpin, adminManuelMod, adminVideoArdisikKazanc,
                maxReroll, ustUsteAktif, spinPolitikasi, zorlaCarpanDegeri, allowIkiTumble: true);
            if (konstrukte != null)
                return konstrukte;
            // İki-tumble enjeksiyonu başarısız olmuş olabilir; sadece tek-tumble ile tekrar dene
            Debug.LogWarning("[KONSTRUKTE] İlk deneme null → allowIkiTumble=false ile tek-tumble fallback deneniyor");
            konstrukte = Senaryo1PaytableKonstrukteHedefSpinDene(
                limit, bonusSpin, adminManuelMod, adminVideoArdisikKazanc,
                maxReroll, ustUsteAktif, spinPolitikasi, zorlaCarpanDegeri, allowIkiTumble: false);
            if (konstrukte != null)
                return konstrukte;
        }

        // Senaryo 2: K-KY-K-KY-K döngüsü | kazanç=paytable bandı | kayıp=minimal geri ödeme
        bool senaryo2KonstrukteZorunlu = !bonusSpin && zorlaCarpanDegeri <= 0 && IsAdminSenaryo2Aktif();
        if (senaryo2KonstrukteZorunlu)
        {
            if (Senaryo2BeklenenKazancMi())
            {
                _senaryo2SonZorunluNihaiOdeme = Senaryo1HedefOdemeMotoru.HedefNihaiOdemeSec(0, 2000, 50);
                maxReroll = Mathf.Max(maxReroll, 2500);
                Debug.Log($"[SENARYO2][AKIS] Kazanç spin: hedef={_senaryo2SonZorunluNihaiOdeme} TL (bant {0}–{2000})");
                SpinSimulasyonKaydi k2 = Senaryo2KazancKonstrukteHedefSpinDene(
                    limit, bonusSpin, adminManuelMod, adminVideoArdisikKazanc,
                    maxReroll, ustUsteAktif, spinPolitikasi, zorlaCarpanDegeri, allowIkiTumble: true);
                if (k2 != null) return k2;
                Debug.LogWarning("[SENARYO2] İki-tumble başarısız → tek-tumble deneniyor");
                k2 = Senaryo2KazancKonstrukteHedefSpinDene(
                    limit, bonusSpin, adminManuelMod, adminVideoArdisikKazanc,
                    maxReroll, ustUsteAktif, spinPolitikasi, zorlaCarpanDegeri, allowIkiTumble: false);
                if (k2 != null) return k2;
                Debug.LogWarning("[SENARYO2] Kazanç konstrukte başarısız → normal reroll");
            }
            else
            {
                Debug.Log("[SENARYO2][AKIS] Kayıp spin: minimal ödeme konstrukte");
                SpinSimulasyonKaydi k2 = Senaryo2KayipKonstrukteHedefSpinDene(
                    limit, bonusSpin, adminManuelMod, adminVideoArdisikKazanc,
                    maxReroll, ustUsteAktif, spinPolitikasi, zorlaCarpanDegeri);
                if (k2 != null) return k2;
                Debug.LogWarning("[SENARYO2] Kayıp konstrukte başarısız → normal reroll");
            }
        }

        // Senaryo 3: KY-K-KY-K-KY döngüsü | kazanç=bahis+küçük bandı | kayıp=bahise yakın altı
        bool senaryo3KonstrukteZorunlu = !bonusSpin && zorlaCarpanDegeri <= 0 && IsAdminSenaryo3Aktif();
        if (senaryo3KonstrukteZorunlu)
        {
            bool s3Kazanc = Senaryo3BeklenenKazancMi();
            Debug.Log($"[S3][KARAR] donguIdx={_senaryo3DonguIndex} beklenen={( s3Kazanc ? "KAZANÇ" : "KAYIP")} | minOdeme={0} maxOdeme={2000}");
            if (s3Kazanc)
            {
                _senaryo3SonZorunluNihaiOdeme = Senaryo1HedefOdemeMotoru.HedefNihaiOdemeSec(0, 2000, 50);
                maxReroll = Mathf.Max(maxReroll, 2500);
                Debug.Log($"[S3][KAZANÇ] hedef={_senaryo3SonZorunluNihaiOdeme} TL");
                SpinSimulasyonKaydi k3 = Senaryo3KazancKonstrukteHedefSpinDene(
                    limit, bonusSpin, adminManuelMod, adminVideoArdisikKazanc,
                    maxReroll, ustUsteAktif, spinPolitikasi, zorlaCarpanDegeri, allowIkiTumble: true);
                if (k3 != null) { Debug.Log($"[S3][KAZANÇ] Konstrukte BAŞARILI"); return k3; }
                Debug.LogWarning("[S3][KAZANÇ] İki-tumble başarısız → tek-tumble");
                k3 = Senaryo3KazancKonstrukteHedefSpinDene(
                    limit, bonusSpin, adminManuelMod, adminVideoArdisikKazanc,
                    maxReroll, ustUsteAktif, spinPolitikasi, zorlaCarpanDegeri, allowIkiTumble: false);
                if (k3 != null) { Debug.Log($"[S3][KAZANÇ] Tek-tumble BAŞARILI"); return k3; }
                Debug.LogWarning("[S3][KAZANÇ] Konstrukte BAŞARISIZ → normal reroll");
            }
            else
            {
                Debug.Log("[S3][KAYIP] Kayıp konstrukte başlıyor");
                SpinSimulasyonKaydi k3 = Senaryo3KayipKonstrukteHedefSpinDene(
                    limit, bonusSpin, adminManuelMod, adminVideoArdisikKazanc,
                    maxReroll, ustUsteAktif, spinPolitikasi, zorlaCarpanDegeri);
                if (k3 != null) { Debug.Log($"[S3][KAYIP] Konstrukte BAŞARILI"); return k3; }
                Debug.LogWarning("[S3][KAYIP] Konstrukte BAŞARISIZ → normal reroll");
            }
        }

        // Senaryo 4: KY→K→BOMB_100x (3-spin döngüsü)
        bool senaryo4KonstrukteZorunlu = !bonusSpin && IsAdminSenaryo4Aktif();
        if (senaryo4KonstrukteZorunlu)
        {
            var tip4 = Senaryo4DonguSpinTipi();
            Debug.Log($"[SENARYO4][AKIS] Döngü tipi={tip4} index={_senaryo4DonguIndex}");
            SpinSimulasyonKaydi k4 = null;
            if (tip4 == SenaryoBombSpinTipi.Kazanc)
            {
                k4 = Senaryo4KazancKonstrukteHedefSpinDene(limit, bonusSpin, adminManuelMod, adminVideoArdisikKazanc,
                    maxReroll, ustUsteAktif, spinPolitikasi, zorlaCarpanDegeri);
                if (k4 != null) return k4;
                Debug.LogWarning("[SENARYO4] Kazanç konstrukte başarısız → normal reroll");
            }
            else if (tip4 == SenaryoBombSpinTipi.Kayip)
            {
                k4 = Senaryo4KayipKonstrukteHedefSpinDene(limit, bonusSpin, adminManuelMod, adminVideoArdisikKazanc,
                    maxReroll, ustUsteAktif, spinPolitikasi, zorlaCarpanDegeri);
                if (k4 != null) return k4;
                Debug.LogWarning("[SENARYO4] Kayıp konstrukte başarısız → normal reroll");
            }
            else // Bomb
            {
                k4 = Senaryo4BombKonstrukteHedefSpinDene(limit, bonusSpin, adminManuelMod, adminVideoArdisikKazanc,
                    maxReroll, ustUsteAktif, spinPolitikasi, zorlaCarpanDegeri);
                if (k4 != null) return k4;
                Debug.LogWarning("[SENARYO4] Bomb konstrukte başarısız → normal reroll");
            }
        }

        // Senaryo 5: K→KY→BOMB_500x (3-spin döngüsü)
        bool senaryo5KonstrukteZorunlu = !bonusSpin && IsAdminSenaryo5Aktif();
        if (senaryo5KonstrukteZorunlu)
        {
            var tip5 = Senaryo5DonguSpinTipi();
            Debug.Log($"[SENARYO5][AKIS] Döngü tipi={tip5} index={_senaryo5DonguIndex}");
            SpinSimulasyonKaydi k5 = null;
            if (tip5 == SenaryoBombSpinTipi.Kazanc)
            {
                k5 = Senaryo5KazancKonstrukteHedefSpinDene(limit, bonusSpin, adminManuelMod, adminVideoArdisikKazanc,
                    maxReroll, ustUsteAktif, spinPolitikasi, zorlaCarpanDegeri);
                if (k5 != null) return k5;
                Debug.LogWarning("[SENARYO5] Kazanç konstrukte başarısız → normal reroll");
            }
            else if (tip5 == SenaryoBombSpinTipi.Kayip)
            {
                k5 = Senaryo5KayipKonstrukteHedefSpinDene(limit, bonusSpin, adminManuelMod, adminVideoArdisikKazanc,
                    maxReroll, ustUsteAktif, spinPolitikasi, zorlaCarpanDegeri);
                if (k5 != null) return k5;
                Debug.LogWarning("[SENARYO5] Kayıp konstrukte başarısız → normal reroll");
            }
            else // Bomb
            {
                k5 = Senaryo5BombKonstrukteHedefSpinDene(limit, bonusSpin, adminManuelMod, adminVideoArdisikKazanc,
                    maxReroll, ustUsteAktif, spinPolitikasi, zorlaCarpanDegeri);
                if (k5 != null) return k5;
                Debug.LogWarning("[SENARYO5] Bomb konstrukte başarısız → normal reroll");
            }
        }

        for (int deneme = 0; deneme < maxReroll; deneme++)
        {
            bool ustUsteKazancBekleniyor = ustUsteAktif && false;
            zorlaSiradakiCarpan = zorlaCarpanDegeri;
            UI_CarpanSifirla();
            _izgaraServisi?.ResetScatterCountPerSpin();
            spinKazancHam = 0;

            int fillLimit = adminManuelMod ? int.MaxValue : odenebilirLimit;
            if (zorlaCarpanDegeri > 0 && !carpanToggleSecili)
                fillLimit = 0;
            // FAZ35.85: ModKazancKonstrukte ilk denemede grid'i hazırladı, FillRandomAll atlanır (grid ezilmesin).
            // İkinci+ rerollarda (deneme>0) konstrukte zaten kabul edilmemiş demek → standart RNG akışına dön.
            if (_modKonstrukteBasarili && deneme == 0)
            {
                Debug.Log("[MOD_KONSTRUKTE] İlk denemede hazır grid kullanılıyor (FillRandomAll atlandı)");
            }
            else
            {
                _izgaraServisi?.FillRandomAll(fillLimit);
            }
            var scatterKarari = spinPolitikasi.SimulasyonSenaryoScatterVeGarantiyiDegerlendir(bonusSpin);
            if (scatterKarari.Mudahale == SimulasyonScatterGridMudahalesi.DortScatterGaranti)
                GrideEnAzDortScatterKoy();
            // Çarpan üretimi tumble adımı içinde yapılır (aşağıdaki while döngüsü).
            // Burada erken üretim yapılırsa pending liste sonraki adımda ezilip force etkisi kaybolabiliyor.

            // Admin manuel testte (özellikle zorluk 4-5) patlama görünürlüğünü garanti et.
            // Senaryo akışını bozmasın diye yalnızca admin manuel mod + normal spin için devreye al.
            if (spinPolitikasi.AdminManuelIlkGriddeClusterZorlansin(bonusSpin, adminManuelMod, zorlaCarpanDegeri, zorlukSeviyesi))
            {
                GrideZorlaEnAzBirCluster();
                _tumbleServisi?.SetGrid(grid);
            }

            // FAZ35.104 İŞ1 BUG 2: Bonus girme olasılığı tetiklendiyse 4 scatter zorla yerleştir.
            // FillRandomAll SONRASI çünkü FillRandomAll RNG ile grid'i dolduruyor → scatter'lar silinmesin diye burada enjekte.
            // FAZ35.114: deneme==0 guard kaldırıldı (Faz 35.113 deneme==0 fix'inin birebir ikizi).
            // KÖK NEDEN: Spin başı flag set (line 489-493) deterministik AMA placement deneme==0 ile kısıtlıydı.
            // İlk attempt OdemeModelineUygunMu/limit ile reddedilince → deneme=1,2,...'de guard FAIL → Force4ScatterEnjekte
            // çağrılmaz → UI_CarpanSifirla+FillRandomAll önceki iter scatter'ını wipe ettiği için kabul edilen iter'da
            // scatter eksik → BonusKontrol sc<esik → bonus tetiklenmiyor (kullanıcı %100 slider'da bonus girmiyor gözlemi).
            // FIX: Her iterasyonda re-inject; Force4ScatterEnjekte idempotent (line 1238 mevcut>=5 → no-op, çift sayım yok).
            // Scatter cluster üretmez (5 < 8 cluster eşiği TUMBLE_SABIT_ESIK) → spinKazancHam katkı yok → reroll/limit/eğilim
            // kararı şişmez → çarpandaki gibi DEĞİŞİM 2 GEREKSİZ. 02 paylaşım: ScriptedSpinYoneticisi.Aktif early-return
            // (Spin.cs:383-407) flag set'i 02'de ÖLÜ kılar → 02 davranışı bit-for-bit aynı.
            if (_bonusGirmeBuSpinAktif)
            {
                Force4ScatterEnjekte(grid);
                _tumbleServisi?.SetGrid(grid);
                Debug.Log($"[FAZ35.103 İŞ2] Bonus girme tetiklendi (olasilik={_bonusGirmeOlasilik:F2}) — 4 scatter zorla yerleştirildi (FillRandomAll sonrası, FAZ35.104 İŞ1 BUG 2 fix).");
            }

            // Kaçış Frenleme: ardışık kayıp eşiği aşıldı → bu spin'in grid'i cluster oluşacak şekilde zorlanır.
            // Sahte para yok; gerçek cluster patlar, gerçek tumble olur, gerçek ödeme akışı işler.
            if (kacisFrenlemeUygula)
            {
                GrideZorlaEnAzBirCluster();
                _tumbleServisi?.SetGrid(grid);
            }

            // FAZ35.107 SORUN A: DOGAL çarpan flag check + yerleştirme (RNG check spin BAŞINDA bir kez yapıldı, flag burada tüketilir).
            // ESKI Faz 35.105 burada RNG yapıyordu → reroll loop her iterasyonda yeni RNG → deneme==0 guard ile pas → çarpan kaybı.
            // YENI: Spin başında flag set (deneme bağımsız, tek RNG), burada flag check ile sadece ilk denemede ForceCarpaniIlkGriddeGuvenliYerlestir.
            // FAZ35.113 DEĞİŞİM 1: deneme==0 guard kaldırıldı (Faz 35.105 artığı — RNG o zaman loop içindeydi).
            // KÖK NEDEN: Faz 35.107 RNG'yi loop dışına taşıdı (deterministik flag), AMA deneme==0 guard PLACEMENT'ta kaldı.
            // İlk attempt OdemeModelineUygunMu/limit ile reddedilince → deneme=1,2,...'de guard FAIL → çarpan asla yerleşmiyor.
            // FIX: Her iterasyonda re-place; UI_CarpanSifirla+FillRandomAll önceki iter çarpanını wipe ettiği için son
            // kabul edilen iter'ın grid'inde çarpan garanti. Flag tek RNG kararıyla deterministik kalır.
            // FAZ35.115 DEĞİŞİM 2: N adet yerleştirme (for loop, her iter farklı random değer + helper çağrısı).
            // KÖK NEDEN: Eski blok tek helper çağrısı (TEK çarpan) yapıyordu, max ayarı kullanılmıyordu.
            // FIX: _dogalCarpanAdet (flag set'te 1..maxCarpanAdedi random) kadar for loop; her iter RastgeleCarpan() yeni değer,
            // helper yeni hücreye yerleştirir (Simulasyon.cs:929 "CARPAN_SEMBOL hücre atla" guard'ı sayesinde her çağrı farklı hücre).
            // Her çağrı kendi görsel set'ini yapar (Faz 35.110 helper içinde sprite + 1.5x + text) → N hücre otomatik render.
            // Eski Faz 35.109 ÇÖZÜM A sprite render bloğu KALDIRILDI — Faz 35.110 helper zaten visual sorumluluğu üstlendi,
            // redundant + tek _dogalCarpanDeger varsayımı N adet ile çelişiyordu.
            // CarpanServisi.cs:163 _spinCarpanCarpim += c (toplama, Sweet Bonanza tarzı) → N çarpan değerleri TOPLANIR.
            // Faz 35.108 fix korunur: Spin.cs:861 RecordPlacedCarpanlar(kayit.IlkCarpanDegerleri) N değerin tamamını state'e yazar.
            // Faz 35.113 korunur: helper RecordPlacedCarpanlar çağırmıyor → reroll kararı ham bazlı, çarpan toplamı reroll'a girmez.
            if (_dogalCarpanBuSpinAktif && _dogalCarpanAdet > 0)
            {
                int _yerlesen115 = 0;
                for (int i115 = 0; i115 < _dogalCarpanAdet; i115++)
                {
                    int deger115 = RastgeleCarpan();
                    if (deger115 <= 0) continue;
                    ForceCarpaniIlkGriddeGuvenliYerlestir(deger115);
                    _yerlesen115++;
                }
                _tumbleServisi?.SetGrid(grid);
                Debug.Log($"[FAZ35.115] DOGAL {_yerlesen115}/{_dogalCarpanAdet} adet çarpan yerleştirildi (max={maxCarpanAdedi}, her hücre random RastgeleCarpan değeri).");
            }

            var zorlaSonrasi = spinPolitikasi.SimulasyonZorlaCarpanSonrasiIlkGridIsiBelirle(
                zorlaCarpanDegeri, carpanToggleSecili, carpanUretimiAktif);
            if (zorlaSonrasi != SimulasyonZorlaCarpanSonrasiIlkGridIsi.Yok)
            {
                ForceCarpaniIlkGriddeGuvenliYerlestir(zorlaCarpanDegeri);
                _tumbleServisi?.SetGrid(grid);
                // DÜZELTME 1: Toggle KAPALI'da CarpanUretVeBirik hiç çağrılmaz; field'ı burada tüket.
                if (!_carpanTumbleAktif)
                {
                    zorlaSiradakiCarpan = 0;
                    if (carpanAyarlari != null) carpanAyarlari.ZorlaSiradakiCarpan = 0;
                }
                // DÜZELTME 2: Toggle KAPALI → clustersiz grid; Toggle AÇIK → zorla cluster.
                if (zorlaSonrasi == SimulasyonZorlaCarpanSonrasiIlkGridIsi.BombaYerlestirVeTumbleCluster)
                {
                    if (_carpanTumbleAktif)
                    {
                        GrideZorlaEnAzBirCluster();
                        _tumbleServisi?.SetGrid(grid);
                    }
                    else
                    {
                        GrideKazancsizYap();
                        _tumbleServisi?.SetGrid(grid);
                    }
                }
                else
                {
                    GrideKazancsizYap();
                    _tumbleServisi?.SetGrid(grid);
                }
            }
            // Çakışan iki kuralı uzlaştır:
            // 1) Tumble/patlama adımı olmadan pozitif ödeme yok.
            // 2) Üst üste kazanç fazı mutlaka kazançla başlamalı.
            // Admin sen 2/3 kayıp fazında da en az bir tumble adımı (önceki iki if birleşik mantık → politika).
            if (spinPolitikasi.UstUsteVeyaAdminSenaryo23IcinIlkGriddeClusterZorlansin(
                    bonusSpin, ustUsteKazancBekleniyor,
                    IsAdminSenaryo2Aktif() || IsAdminSenaryo4Aktif(),
                    IsAdminSenaryo3Aktif() || IsAdminSenaryo5Aktif()))
            {
                GrideZorlaEnAzBirCluster();
                _tumbleServisi?.SetGrid(grid);
            }

            var kayit = new SpinSimulasyonKaydi { Sutun = sutun, Satir = satir };
            sonDenemeKayit = kayit;

            // Aşama 2 %20 düşüş: tumble kapatmak yerine zorluk artırıldı (AsamaAyariniUygula); burada ek işlem yok.

            // Tumble başlamadan önceki son grid tek kaynak (50K kapatma sonrası dahil); paytable doğrulaması ve oynatma aynı state'i kullanır.
            SimulasyonKaydinaMevcutIlkGridStateKopyala(kayit);

            // İlk griddeki bombalar sadece grid/carpanDegerGrid'de vardı; RecordPlacedCarpanlar refill'e bağlı kaldığı için
            // tumble hiç dönmeyebiliyordu → NihaiCarpanToplam=1, ödeme katlanmıyordu. Kayıttaki ilk çarpan listesiyle senkronla.
            if (_carpanServisi != null && kayit.IlkCarpanDegerleri != null && kayit.IlkCarpanDegerleri.Count > 0)
                _carpanServisi.RecordPlacedCarpanlar(kayit.IlkCarpanDegerleri);

            int turSayaci = 0;
            // Kuralı stabilize et: tumble eşiği sabit 8 (düşüşleri/animasyonu tekrar görünür kıl).
            int tumbleEsik = OyunKorumaServisi.TUMBLE_SABIT_ESIK;
            minClusterSize = tumbleEsik;
            bool limitAsildi = false;
            while (turSayaci < OyunKorumaServisi.MAX_TUMBLE_TUR)
            {
                var toRemove = _tumbleServisi != null ? _tumbleServisi.FindClustersToRemove(tumbleEsik) : new List<Vector2Int>();
                if (toRemove == null || toRemove.Count == 0) break;

                // Bomba patladıktan sonraki ilk refill'de yeni bomba yerleşmesini engelle.
                if (TryIlkBombaHucreBul(out _))
                {
                    _bombaPatlamaSonrasiIlkRefillCarpanEngeli = true;
                }

                // S3/S4/S5 fallback reroll'da da çarpan üretme — konstrukte bandı dar, çarpan bant dışına iter.
                // FAZ35.109 ÇÖZÜM B: Multi-tumble çarpan biriktirme — Faz 35.105 spin başı yerleşim aktifse BYPASS.
                // FAZ35.115 yorum güncelleme: Bypass tumble İÇİ ek birikmeyi engeller. Spin başı 1..N adet yerleşim (Faz 35.115)
                // bypass'tan ETKİLENMEZ — bypass tumble loop İÇİNDE çalışır, spin başı placement loop DIŞINDA. Yani N adet
                // yerleşim aktif + tumble içi accumulator iptal: pedagojik tutarlılık (spin başı miktarı bypass'tan bağımsız belirleniyor).
                // Sweet Bonanza tarzı çoklu biriktirme bonus oyunda devam eder (bonusSpin durumunda Faz 35.105 flag false → bu blok normal çalışır).
                // Senaryolu modlarda (aktifSenaryo != "normal") flag false → multi-tumble normal çalışır.
                if (!IsAdminSenaryo3Aktif() && !IsAdminSenaryo4Aktif() && !IsAdminSenaryo5Aktif()
                    && !_dogalCarpanBuSpinAktif)
                    CarpanUretVeBirik();
                else if (_dogalCarpanBuSpinAktif)
                    Debug.Log("[FAZ35.109 ÇÖZÜM B] Multi-tumble çarpan biriktirme BYPASS — Faz 35.105 spin başı yerleşim aktif, tek çarpan/spin garantili.");

                int turHam = tumbleAyarlari != null ? tumbleAyarlari.CalculateWinWithOwnPayTable(toRemove, grid, satir, sutun, _ekonomiServisi != null ? _ekonomiServisi.Bahis : 0, tumbleEsik) : 0;
                int turKazanc = turHam;
                if (bonusSpin && zorlaCarpanDegeri <= 0 && _senaryoServisi != null)
                {
                    int kalan = _senaryoServisi.GetBonusRemainingPayableTL();
                    int m = _carpanServisi != null ? _carpanServisi.GetCurrentMultiplierInt() : 1;
                    if (m < 1) m = 1;
                    long proj = (long)(spinKazancHam + turKazanc) * (long)m;
                    if (kalan <= 0 || proj > (long)kalan)
                    {
                        limitAsildi = true;
                        break;
                    }
                }
                spinKazancHam += turKazanc;

                var adim = new TumbleAdimKaydi { TurKazanci = turKazanc };
                adim.PatlayanHucreler.AddRange(toRemove);

                GridHucreleriniTemizle(toRemove);
                if (_cokmeAkisServisi != null)
                    _cokmeAkisServisi.CokmeDoldurSadeceMantik(adim);
                kayit.Adimlar.Add(adim);
                turSayaci++;
                if (_senaryo1KonstrukteSimAktif && kayit.Adimlar.Count >= _senaryo1KonstrukteMaxTumbleAdimi)
                    break;
                if (_senaryo2KonstrukteSimAktif && kayit.Adimlar.Count >= _senaryo2KonstrukteMaxTumbleAdimi)
                    break;
                if (_senaryo3KonstrukteSimAktif && kayit.Adimlar.Count >= _senaryo3KonstrukteMaxTumbleAdimi)
                    break;
                if (_senaryo4KonstrukteSimAktif && kayit.Adimlar.Count >= _senaryo4KonstrukteMaxTumbleAdimi)
                    break;
                if (_senaryo5KonstrukteSimAktif && kayit.Adimlar.Count >= _senaryo5KonstrukteMaxTumbleAdimi)
                    break;
            }

            if (limitAsildi)
                continue;

            KonstrukteRefillSonrasiKazancsizYap(kayit);

            int toplamCarpan = _carpanServisi != null ? _carpanServisi.GetTotalMultiplierForSpin() : 1;
            // DÜZELTME 3: Toggle KAPALI + force carpan: bomba patlamaz ama formül ekranda gösterilsin.
            if (zorlaCarpanDegeri > 0 && !_carpanTumbleAktif && toplamCarpan <= 1)
                toplamCarpan = zorlaCarpanDegeri;
            int nihaiOdeme = _carpanServisi != null ? _carpanServisi.MulClampInt(spinKazancHam, toplamCarpan) : spinKazancHam;
            kayit.ToplamHamKazanc = spinKazancHam;
            kayit.NihaiCarpanToplam = toplamCarpan;

            int bahis = _ekonomiServisi != null ? _ekonomiServisi.Bahis : 0;

            if (!SpinKaydiHamPaytableIleUyumluMu(kayit, bahis, tumbleEsik))
            {
                Debug.LogWarning("[SIM][PAYTABLE] Ham/adım tutarları patlayan kümelere göre paytable ile örtüşmüyor; deneme reddedildi.");
                continue;
            }
            bool zorlaCarpanVardi = zorlaCarpanDegeri > 0;
            // FAZ35.113 DEĞİŞİM 2 (Yaklaşım b): Reroll/limit kararı initial DOGAL çarpandan bağımsızlaştırıldı.
            // KÖK NEDEN: Spin.cs:861 RecordPlacedCarpanlar(kayit.IlkCarpanDegerleri) initial çarpanı _spinCarpanCarpim'a
            // yazıyor (Faz 35.108 NihaiCarpanToplam=1 bug fix için zorunlu) → toplamCarpan (line 931) şişiyor →
            // nihaiOdeme = ham × initial_multiplier → :947 OdemeModelineUygunMu eğilim kararı şişmiş değere göre
            // veriliyor → kullanıcı hedefi "çarpan, ödeme aralığı/eğilim hesabına girmesin" ihlali. FIX: bu iki karar
            // noktasında nihaiOdeme yerine spinKazancHam (HAM ödeme, çarpansız) kullanılır → karar ham bazlı.
            // kayit.NihaiCarpanToplam (line 937) toplamCarpan ile set sürer → playback'te DonusAkisServisi:181
            // toplamX = kayit.NihaiCarpanToplam ile initial çarpan ödemeyi çarpar (Faz 35.108 fix korunur).
            if (!bonusSpin && !zorlaCarpanVardi && !OdemeModelineUygunMu(spinKazancHam, bahis, deneme, maxReroll))
                continue;

            if (adminVideoArdisikKazanc && nihaiOdeme <= bahis)
                continue;

            // FAZ35.113 DEĞİŞİM 2 (Yaklaşım b devam): limit check de ham bazlı — çarpan ödeme aralığı kararına girmesin.
            // 03 NORMAL Free spin: limit=int.MaxValue → check zaten fire etmiyor, no-op.
            // Bonus/Senaryo: limit < MaxValue. spinKazancHam bazlı check pedagojik tutarlı (çarpan vitrini bağımsız).
            if (limit != int.MaxValue && spinKazancHam > limit)
                continue;
            // Senaryo 1: 4 üst üste ödeme sonrası 3 spin zorunlu boş (ödeme yok)
            if (!bonusSpin && !adminManuelMod && !adminVideoArdisikKazanc && SenaryoYoneticisi.I != null && SenaryoYoneticisi.I.ShouldForceNoPaySenaryo12() && nihaiOdeme > 0)
                continue;
            // Zorla çarpan varken diğer "min tumble / min ödeme" kuralları uygulanmaz; sadece toggle kuralı geçerli.
            if (!zorlaCarpanVardi)
            {
                if (spinPolitikasi.KolayZorlukBonusSpindeMinOdemeAltindaReddet(
                        bonusSpin, zorlaCarpanVardi, limit, _easyBias01, nihaiOdeme))
                    continue;
                if (spinPolitikasi.KolayZorlukTumblesizSonuctaYenidenDene(
                        bonusSpin,
                        zorlaCarpanVardi,
                        limit,
                        _easyBias01,
                        kayit,
                        SenaryoYoneticisi.I != null))
                    continue;
            }
            // CarpanAktifToggle: seçiliyse zorla çarpanla mutlaka tumble, seçili değilse mutlaka tumble olmasın.
            kayit.SenaryoOdemeBandinaUygun = true;
            sonKayit = kayit;
            kayit.ZorlaCarpanKullanildi = zorlaCarpanDegeri > 0;
            // Senaryo 2/3 üst üste dizisi NormalSpinAkisi sonunda ilerletilir; önbellek / SimuleEtVeKaydetImpl burada ilerletmez.
            if (!bonusSpin && !zorlaCarpanVardi && !IsAdminSenaryo2Aktif() && !IsAdminSenaryo3Aktif())
                UstUsteDonguyuSpinSonucuIleIlerle(nihaiOdeme > bahis);
            if (adminVideoArdisikKazanc && nihaiOdeme > bahis)
            {
                _adminVideoArdisikKazancSpinKalan = Mathf.Max(0, _adminVideoArdisikKazancSpinKalan - 1);
                Debug.Log($"[ADMIN][VIDEO] Arka arkaya kazançlı spin kaldı: {_adminVideoArdisikKazancSpinKalan}");
            }
            // FAZ35.84: Reroll deneme sayısı yakalama (debug analiz için)
            _sonSpinRerollSayisi = deneme;
            return kayit;
        }

        // Döngü bitti, sonuç bulunamadı
        if (sonKayit == null)
        {
            int dbgSonNihai = sonDenemeKayit != null && _carpanServisi != null
                ? _carpanServisi.MulClampInt(sonDenemeKayit.ToplamHamKazanc, System.Math.Max(1, sonDenemeKayit.NihaiCarpanToplam))
                : (sonDenemeKayit?.ToplamHamKazanc ?? -1);
            Debug.LogWarning($"[SEN1_DBG2] Tüm reroll'lar tükendi. maxReroll={maxReroll} | senaryo1KazancBandiZorunlu={senaryo1KazancBandiZorunlu} | SonDenemeNihai={dbgSonNihai} | efektifMin~={0} efektifMax~={2000} | KazancFaziAktif={false}");
        }

        // Senaryo 1 zorunlu boş spin: 400 denemede 0 gelmediyse gridi kazançsız yapıp garanti 0 ödeme döndür
        if (zorunluBosSpin && sonKayit == null && sonDenemeKayit != null)
        {
            zorlaSiradakiCarpan = zorlaCarpanDegeri;
            UI_CarpanSifirla();
            _izgaraServisi?.ResetScatterCountPerSpin();
            spinKazancHam = 0;
            int fillLimit = adminManuelMod ? int.MaxValue : odenebilirLimit;
            if (zorlaCarpanDegeri > 0 && !carpanToggleSecili) fillLimit = 0;
            SpinSimulasyonKaydi zorlaKayit = ZorunluBosSpinIcinSifirKayitUret(fillLimit);
            return zorlaKayit;
        }

        if (sonKayit == null && sonDenemeKayit != null)
        {
            int bahisFb = _ekonomiServisi != null ? _ekonomiServisi.Bahis : 0;
            bool asama1PedagojikAktif = !bonusSpin
                && SenaryoYoneticisi.I != null
                && SenaryoYoneticisi.I.mevcutAsama == SenaryoYoneticisi.SenaryoAsama.Asama1_IsindirmaUmut;
            bool adminSenaryo1Aktif = !bonusSpin && IsAdminSenaryo1Aktif();
            // Anlatıcı Asama 0 (Isındırma) ve Asama 1 (Kontrol) cömertlik zorlama: fallback'te bant dışı kazanç
            // bant içine çekilir (Senaryo1 ile aynı mekanik, sadece anlaticiAsama<=1 için).
            // Asama 5-6-7 (kayıp serbest) için TETİKLENMEZ → kayıp eğrisi bozulmaz.
            bool anlaticiCumertlikAktif = !bonusSpin
                && AnlaticiSeritKopru.Ornek != null
                && AnlaticiSeritKopru.Ornek.AktifAsama <= 1;
            int fallbackAdimSayisi = sonDenemeKayit.Adimlar != null ? sonDenemeKayit.Adimlar.Count : 0;
            if (spinPolitikasi.SimulasyonSonDenemedeAdimsizPozitifHamIptalEdilsinMi(
                    bonusSpin, fallbackAdimSayisi, sonDenemeKayit.ToplamHamKazanc))
            {
                // Görsel akış kuralı: tumble/patlama adımı yoksa pozitif ödeme gösterilmez.
                // Aksi halde "meyveler düşmeden direkt kazanç yazdı" bug'ı oluşuyor.
                Debug.LogWarning($"[SIM][FALLBACK] Adim yokken pozitif ödeme engellendi. Ham={sonDenemeKayit.ToplamHamKazanc}");
                sonDenemeKayit.ToplamHamKazanc = 0;
                sonDenemeKayit.NihaiCarpanToplam = 1;
            }

            int nihaiFb = _carpanServisi != null
                ? _carpanServisi.MulClampInt(sonDenemeKayit.ToplamHamKazanc, Mathf.Max(1, sonDenemeKayit.NihaiCarpanToplam))
                : sonDenemeKayit.ToplamHamKazanc;
            bool odemeModelUygun = OdemeModelineUygunMu(nihaiFb, bahisFb, maxReroll, maxReroll);
            bool bandUygun = spinPolitikasi.SimulasyonFallbackOdemeBandinaUygunMu(
                bonusSpin, sonDenemeKayit.ZorlaCarpanKullanildi, odemeModelUygun);
            sonDenemeKayit.SenaryoOdemeBandinaUygun = bandUygun;
            if ((asama1PedagojikAktif || adminSenaryo1Aktif || anlaticiCumertlikAktif) && !bandUygun)
            {
                if (Senaryo1FallbackKazanciniBandIcineZorla(sonDenemeKayit, bahisFb, out int zorlananNihai))
                {
                    nihaiFb = zorlananNihai;
                    bandUygun = true;
                    sonDenemeKayit.SenaryoOdemeBandinaUygun = true;
                    string kaynak = adminSenaryo1Aktif ? "AdminPreset1"
                                  : asama1PedagojikAktif ? "PedagojikAsama1"
                                  : ("AnlaticiAsama" + (AnlaticiSeritKopru.Ornek != null ? AnlaticiSeritKopru.Ornek.AktifAsama : -1));
                    Debug.LogWarning($"[FALLBACK_ZORLAMA] Band dışı fallback, band içi kazanca çevrildi. Nihai={nihaiFb}, Bahis={bahisFb}, MinMax={0}-{2000}, Kaynak={kaynak}.");
                }
                else if (asama1PedagojikAktif || adminSenaryo1Aktif)
                {
                    // Senaryo1 yolu: zorlanacak tumble yoksa sıfır ödeme fallback (mevcut davranış korunur).
                    int fillLimit = adminManuelMod ? int.MaxValue : odenebilirLimit;
                    if (zorlaCarpanDegeri > 0 && !carpanToggleSecili)
                        fillLimit = 0;
                    Debug.LogWarning($"[SENARYO1][FALLBACK_BLOKE] Band dışı fallback engellendi ama zorlanacak tumble kaydı yok. Nihai={nihaiFb}, Bahis={bahisFb}, MinMax={0}-{2000}, Kaynak={(adminSenaryo1Aktif ? "AdminPreset1" : "PedagojikAsama1")}. Sıfır ödeme fallback üretildi.");
                    sonDenemeKayit = ZorunluBosSpinIcinSifirKayitUret(fillLimit);
                    nihaiFb = 0;
                    bandUygun = true;
                    sonDenemeKayit.SenaryoOdemeBandinaUygun = true;
                }
                else
                {
                    // Anlatıcı yolu: zorlanacak tumble yoksa "kayıp" olarak kabul et (sıfırlama yapma).
                    // Kullanıcı kazanç bekliyordu ama RNG hiçbir yerde uygun sonuç bulamadı; sonDenemeKayit olduğu gibi gider.
                    Debug.LogWarning($"[ANLATICI][FALLBACK_BLOKE] Band içi tumble zorlanamadı, sonDeneme kabul edildi (sıfırlama yok). Nihai={nihaiFb}, Bahis={bahisFb}, AnlaticiAsama={(AnlaticiSeritKopru.Ornek != null ? AnlaticiSeritKopru.Ornek.AktifAsama : -1)}.");
                    bandUygun = true;
                    sonDenemeKayit.SenaryoOdemeBandinaUygun = true;
                }
            }

            if (ustUsteAktif)
            {
                int bahis = bahisFb;
                int fallbackNihaiOdeme = nihaiFb;
                bool uygun = bandUygun;
                if (!uygun)
                {
                    int tumbleEsikLog = OyunKorumaServisi.TUMBLE_SABIT_ESIK;
                    bool payOk = SpinKaydiHamPaytableIleUyumluMu(sonDenemeKayit, bahis, tumbleEsikLog);
                    Debug.LogWarning($"[USTUSTE][FALLBACK] Ödeme modeli son denemede uyumsuz; ham/adım paytable sonucu korunuyor (uydurma yok). BeklenenFaz={(false ? "KAZANÇ" : "KAYIP")} KazancHedef={0} KayipHedef={0} FallbackOdeme={fallbackNihaiOdeme} Bahis={bahis} Reroll={maxReroll} PaytableUyumlu={payOk}");
                }
            }

            sonKayit = sonDenemeKayit;
            // Fallback kabulünde de faz sayacı mutlaka ilerletilmeli; aksi halde sıra kayar.
            if (ustUsteAktif && sonKayit != null)
            {
                int bahis = _ekonomiServisi != null ? _ekonomiServisi.Bahis : 0;
                int nihaiOdeme = _carpanServisi != null
                    ? _carpanServisi.MulClampInt(sonKayit.ToplamHamKazanc, Mathf.Max(1, sonKayit.NihaiCarpanToplam))
                    : sonKayit.ToplamHamKazanc;
                if (!IsAdminSenaryo2Aktif() && !IsAdminSenaryo3Aktif())
                    UstUsteDonguyuSpinSonucuIleIlerle(nihaiOdeme > bahis);
            }
        }
        return sonKayit;
    }

    /// <summary>ÖNCE-modal helper: pedagojik modal'ı oynatır, modal kapanınca asıl SpinButonImpl
    /// çağrılır → flag artık set olduğu için ÖNCE bloğu atlanır, asıl spin atılır.</summary>
    private System.Collections.IEnumerator OnceModalGosterVeSpin(string mesaj)
    {
        var modal = UnityEngine.Object.FindObjectOfType<Senaryo.Scripted.ScriptedModalKopru>();
        if (modal != null)
            yield return modal.ModalGoster(mesaj);
        else
            Debug.LogWarning("[ÖNCE Modal] ScriptedModalKopru bulunamadı, modal atlanıyor.");

        // A2 Spin 4 ÖNCE modal sonrası: bahis 1000 → 2000 görsel animasyonla yükseltilir.
        // Kullanıcı "+ + + +" tuşlamış gibi görür; modal "bahisini yükseltecek" iddiasını gerçeğe çevirir.
        var anlatici = AnlaticiSeritKopru.Ornek;
        if (anlatici != null && anlatici.AktifAsama == 1 && anlatici.AsamadakiSpinSayaci == 3)
        {
            yield return BahisYukseltAnimasyonu(1000, 2000);
        }

        // Modal kapandı — asıl spin akışını başlat (flag set olduğu için ÖNCE bloğu atlanır)
        SpinButonImpl();
    }

    // FAZ35.85: ModKazancKonstrukte helper — Senaryo 1 motoru (HedefOdemeMotorBase) reuse.
    // Sadece grid kurar (sembol+küme yerleştirir), kayıt oluşturmaz. Ana reroll loop bu hazır grid'i
    // kullanır (FillRandomAll guard'la atlanır). Tumble + adım kaydı + ödeme hesabı ana loop'ta.
    // Senaryo 1 PaytableKonstrukteHedefSpinDene'nin basit versiyonu (tek-tumble, no carpan/bonus limit).
    private bool TryModKazancKonstrukteGridKur(int bahis)
    {
        if (tumbleAyarlari == null || grid == null || _ekonomiServisi == null) return false;
        int sembolSayisi = tumbleAyarlari.PayTable_8_9?.Length ?? 0;
        if (sembolSayisi <= 0 || bahis <= 0) return false;

        int minTl = Mathf.RoundToInt(bahis * odemeMinKat);
        int maxTl = Mathf.RoundToInt(bahis * odemeMaksKat);
        if (maxTl < minTl) { int t = minTl; minTl = maxTl; maxTl = t; }

        // FAZ35.86 İŞ2: Çeşitlilik motoru — yeni rastgele seçim + anti-streak guard.
        // Mevcut TryPaytableUyumluTekKumeSec deterministik (KumeBoyuAgirligi 8-9 dominantlığı + tolerance filter →
        // pratikte aynı sembol/küme tekrar). Yeni metod: tüm bant adayları EŞİT şans + son seçileni atla.
        // hedefTl parametresi YOK (tolerance filter atlandığı için gereksiz). Senaryo 1-5 mevcut metodu kullanmaya devam eder.
        int onceki = _modSonSecilenSembol;
        if (!Senaryo1HedefOdemeMotoru.TryPaytableUyumluTekKumeRastgeleSec(
                tumbleAyarlari, bahis, minTl, maxTl,
                _scatterIndexCache, sutun, satir,
                _modSonSecilenSembol,
                out int kSym, out int kCnt, out int beklenenTl))
        {
            Debug.LogWarning($"[MOD_KONSTRUKTE] Paytable uyumlu küme YOK: bant={minTl}-{maxTl} bahis={bahis}");
            return false;
        }

        if (!Senaryo1HedefOdemeMotoru.TryTekKumeliIlkGridOlustur(
                sutun, satir, kSym, kCnt, _scatterIndexCache, sembolSayisi, out int[,] yeniGrid))
        {
            Debug.LogWarning($"[MOD_KONSTRUKTE] Grid üretimi başarısız: sym={kSym} cnt={kCnt}");
            return false;
        }

        for (int x = 0; x < sutun; x++)
            for (int y = 0; y < satir; y++)
                grid[x, y] = yeniGrid[x, y];
        _tumbleServisi?.SetGrid(grid);
        // FAZ35.86: Anti-streak için son seçileni güncelle (sonraki spin bu sembolü atlar, alternatif varsa).
        _modSonSecilenSembol = kSym;

        // FAZ35.87 İŞ3: Bonus çarpan uygula (1x %70, 2x %20, 3x %10) — tutar çeşitliliği.
        // zorlaSiradakiCarpan motor mekanizması (Fields.cs:507) bonus carpan'ı ödeme sonunda uygular.
        // Bant taşma guard: (beklenenTl × bonusCarpan) <= maxTl × 2 → 3x çarpanla bile maksimum 2 katı aşmasın.
        int bonusCarpan = ModBonusCarpanSec();
        if (bonusCarpan > 1)
        {
            long yeniTL = (long)beklenenTl * bonusCarpan;
            int tasmaSinir = maxTl * 2;
            if (yeniTL <= tasmaSinir)
            {
                zorlaSiradakiCarpan = bonusCarpan;
                _modSonBonusCarpan = bonusCarpan;
            }
            else
            {
                // Taşma riski → bonus çarpan iptal, log için 1
                _modSonBonusCarpan = 1;
            }
        }
        else
        {
            _modSonBonusCarpan = 1;
        }

        Debug.Log($"[MOD_KONSTRUKTE+BONUS] bant={minTl}-{maxTl} sym={kSym} kümeBoy={kCnt} beklenenTL={beklenenTl} bonus={_modSonBonusCarpan}x (önceki sym={onceki})");
        return true;
    }

    // FAZ35.103 İŞ2: 4 rastgele non-scatter hücreye ScatterIndex yerleştir (bonus tetik için, Normal mod bonus girme olasılığı).
    // Mevcut scatter sayısı 4+ varsa enjeksiyon pas (idempotent). Fisher-Yates karıştırma ile rastgele dağılım.
    private void Force4ScatterEnjekte(int[,] g)
    {
        if (g == null) return;
        int scatterIdx = _scatterIndexCache;
        int sut = g.GetLength(0);
        int sat = g.GetLength(1);

        var adaylar = new System.Collections.Generic.List<(int x, int y)>();
        int mevcutScatter = 0;
        for (int x = 0; x < sut; x++)
        {
            for (int y = 0; y < sat; y++)
            {
                if (g[x, y] == scatterIdx) mevcutScatter++;
                else adaylar.Add((x, y));
            }
        }

        // FAZ35.105 İŞ1 GARANTI: Bonus eşiği (scatterEsik) 4 — ama 4 koymak yetmiyor (RNG tumble içinde silinebilir, kayıp scatter olabilir).
        // 5 scatter ZORLA → eşiği geçme garantili. BonusKontrol scIlk >= 4 ile bonus tetik.
        int hedefScatter = 5;
        int gereken = Mathf.Max(0, hedefScatter - mevcutScatter);
        if (gereken <= 0) return;
        if (adaylar.Count < gereken) gereken = adaylar.Count;

        // Fisher-Yates karıştırma (UnityEngine.Random)
        for (int i = adaylar.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            var t = adaylar[i]; adaylar[i] = adaylar[j]; adaylar[j] = t;
        }

        for (int i = 0; i < gereken; i++)
        {
            g[adaylar[i].x, adaylar[i].y] = scatterIdx;
        }
    }

    /// <summary>Bahis animasyon helper: eski → yeni değere kademeli artar (görsel feedback, "+ tuşu").</summary>
    private System.Collections.IEnumerator BahisYukseltAnimasyonu(int eski, int yeni)
    {
        if (_ekonomiServisi == null) yield break;
        const int ADIM = 250;
        const float SURE_PER_ADIM = 0.10f;
        int simdi = Mathf.Max(eski, _ekonomiServisi.Bahis);
        while (simdi < yeni)
        {
            simdi = Mathf.Min(yeni, simdi + ADIM);
            try { AnlaticiSetBahis(simdi); }
            catch (System.Exception e) { Debug.LogWarning("[BahisYukselt] hata: " + e.Message); break; }
            yield return new WaitForSecondsRealtime(SURE_PER_ADIM);
        }
        Debug.Log($"[BahisYukselt] {eski} → {yeni} TL animasyonu tamamlandı.");
    }
}