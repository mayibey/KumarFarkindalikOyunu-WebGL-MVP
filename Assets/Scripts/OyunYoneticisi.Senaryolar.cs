using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class OyunYoneticisi
{
    private SpinSimulasyonKaydi Senaryo1PaytableKonstrukteHedefSpinDene(
        int limit,
        bool bonusSpin,
        bool adminManuelMod,
        bool adminVideoArdisikKazanc,
        int maxReroll,
        bool ustUsteAktif,
        ISenaryoSpinPolitikasi spinPolitikasi,
        int zorlaCarpanDegeri,
        bool allowIkiTumble = true)
    {
        _ = ustUsteAktif;
        if (bonusSpin || zorlaCarpanDegeri > 0 || tumbleAyarlari == null || grid == null || _ekonomiServisi == null)
        {
            Debug.LogWarning("[KONSTRUKTE] Erken çıkış: bonusSpin=" + bonusSpin + " zorlaCarpan=" + zorlaCarpanDegeri + " ta=" + (tumbleAyarlari == null) + " grid=" + (grid == null) + " eko=" + (_ekonomiServisi == null));
            return null;
        }

        int bahis0 = Mathf.Max(1, _ekonomiServisi.Bahis);
        int sembolSayisi = tumbleAyarlari.PayTable_8_9 != null ? tumbleAyarlari.PayTable_8_9.Length : 0;
        int kSym, kCnt, beklenenTl;
        _senaryo1KonstrukteIkinciKumeSembol = -1;
        _senaryo1KonstrukteIkinciKumeBoy = 0;
        _senaryo1KonstrukteMaxTumbleAdimi = 1;

        if (allowIkiTumble && Senaryo1HedefOdemeMotoru.TryPaytableUyumluIkiTumbleKumesiSec(
                tumbleAyarlari,
                bahis0,
                _minOdemeTL,
                _maxOdemeTL,
                _senaryo1SonZorunluNihaiOdeme,
                _scatterIndexCache,
                sutun,
                satir,
                out int kSym1,
                out int kCnt1,
                out int tl1,
                out int kSym2,
                out int kCnt2,
                out int tl2))
        {
            kSym = kSym1;
            kCnt = kCnt1;
            beklenenTl = tl1 + tl2;
            _senaryo1KonstrukteIkinciKumeSembol = kSym2;
            _senaryo1KonstrukteIkinciKumeBoy = kCnt2;
            _senaryo1KonstrukteMaxTumbleAdimi = 2;
        }
        else if (!Senaryo1HedefOdemeMotoru.TryPaytableUyumluTekKumeSec(
                     tumbleAyarlari,
                     bahis0,
                     _minOdemeTL,
                     _maxOdemeTL,
                     _senaryo1SonZorunluNihaiOdeme,
                     _scatterIndexCache,
                     sutun,
                     satir,
                     out kSym,
                     out kCnt,
                     out beklenenTl))
            return null;

        Debug.Log($"[KONSTRUKTE] allowIkiTumble={allowIkiTumble} | kSym={kSym} kCnt={kCnt} beklenenTl={beklenenTl} | ikinciSym={_senaryo1KonstrukteIkinciKumeSembol} ikinciCnt={_senaryo1KonstrukteIkinciKumeBoy}");
        if (!Senaryo1HedefOdemeMotoru.TryTekKumeliIlkGridOlustur(
                sutun, satir, kSym, kCnt, _scatterIndexCache, sembolSayisi, out int[,] yeniGrid))
        {
            Debug.LogWarning($"[KONSTRUKTE] TryTekKumeliIlkGridOlustur BAŞARISIZ: sym={kSym} cnt={kCnt}");
            return null;
        }

        for (int x = 0; x < sutun; x++)
            for (int y = 0; y < satir; y++)
                grid[x, y] = yeniGrid[x, y];
        _tumbleServisi?.SetGrid(grid);

        zorlaSiradakiCarpan = zorlaCarpanDegeri;
        UI_CarpanSifirla();
        _izgaraServisi?.ResetScatterCountPerSpin();
        spinKazancHam = 0;

        var kayit = new SpinSimulasyonKaydi { Sutun = sutun, Satir = satir };
        SimulasyonKaydinaMevcutIlkGridStateKopyala(kayit);
        if (_carpanServisi != null && kayit.IlkCarpanDegerleri != null && kayit.IlkCarpanDegerleri.Count > 0)
            _carpanServisi.RecordPlacedCarpanlar(kayit.IlkCarpanDegerleri);

        int turSayaci = 0;
        int tumbleEsik = OyunKorumaServisi.TUMBLE_SABIT_ESIK;
        minClusterSize = tumbleEsik;
        bool limitAsildi = false;

        _senaryo1KonstrukteSimAktif = true;
        try
        {
            while (turSayaci < OyunKorumaServisi.MAX_TUMBLE_TUR)
            {
                var toRemove = _tumbleServisi != null ? _tumbleServisi.FindClustersToRemove(tumbleEsik) : new List<Vector2Int>();
                if (toRemove == null || toRemove.Count == 0)
                    break;

                // Konstrukte: çarpan schedule et (toggle kapalıysa TryScheduleCarpanDrop zaten return false).
                // Bant kontrolü nihaiOdeme > maxTl ise null döndürür; nadiren ama çarpanlı spin mümkün olur.
                CarpanUretVeBirik();

                int turHam = tumbleAyarlari != null
                    ? tumbleAyarlari.CalculateWinWithOwnPayTable(toRemove, grid, satir, sutun, _ekonomiServisi.Bahis, tumbleEsik)
                    : 0;
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

                if (_senaryo1KonstrukteIkinciKumeSembol >= 0 && kayit.Adimlar.Count == 1)
                {
                    // Enjeksiyon öncesi hangi hücrelerin hedef sembolü taşıdığını kaydet (değişen hücreleri bulmak için)
                    var oncekiSym2Hucreleri = new System.Collections.Generic.HashSet<Vector2Int>();
                    int injSym = _senaryo1KonstrukteIkinciKumeSembol;
                    for (int xi = 0; xi < sutun; xi++)
                        for (int yi = 0; yi < satir; yi++)
                            if (grid[xi, yi] == injSym) oncekiSym2Hucreleri.Add(new Vector2Int(xi, yi));

                    if (Senaryo1HedefOdemeMotoru.TryIkinciTumbleKumesiRefillSonrasiEnjekteEt(
                            grid, sutun, satir, _senaryo1KonstrukteIkinciKumeSembol, _senaryo1KonstrukteIkinciKumeBoy,
                            _scatterIndexCache, sembolSayisi,
                            kayit.Adimlar[kayit.Adimlar.Count - 1].YeniSpawnEdilenHucreler))
                    {
                        var sonAdim = kayit.Adimlar[kayit.Adimlar.Count - 1];
                        if (sonAdim.GridRefillSonrasi != null)
                        {
                            for (int xi = 0; xi < sutun; xi++)
                                for (int yi = 0; yi < satir; yi++)
                                    sonAdim.GridRefillSonrasi[xi, yi] = grid[xi, yi];
                        }

                        // Yeni eklenen sym2 hücrelerini InjekteEdilenHucreler'e kaydet (sprite güncellemesi için)
                        for (int xi = 0; xi < sutun; xi++)
                            for (int yi = 0; yi < satir; yi++)
                            {
                                var pos = new Vector2Int(xi, yi);
                                if (grid[xi, yi] == injSym && !oncekiSym2Hucreleri.Contains(pos))
                                    sonAdim.InjekteEdilenHucreler.Add(pos);
                            }

                        _tumbleServisi?.SetGrid(grid);
                        _senaryo1KonstrukteIkinciKumeSembol = -1;
                    }
                    else
                    {
                        Debug.LogWarning("[SENARYO1][KONSTRUKTE] İkinci küme enjekte edilemedi; tek tumble ile devam.");
                        _senaryo1KonstrukteIkinciKumeSembol = -1;
                        _senaryo1KonstrukteMaxTumbleAdimi = 1;
                    }
                }

                if (_senaryo1KonstrukteSimAktif && kayit.Adimlar.Count >= _senaryo1KonstrukteMaxTumbleAdimi)
                    break;
            }
        }
        finally
        {
            _senaryo1KonstrukteSimAktif = false;
            _senaryo1KonstrukteIkinciKumeSembol = -1;
            _senaryo1KonstrukteMaxTumbleAdimi = 1;
        }

        if (limitAsildi)
        {
            Debug.LogWarning("[KONSTRUKTE] limitAsildi=true → null");
            return null;
        }

        KonstrukteRefillSonrasiKazancsizYap(kayit);
        int toplamCarpan = _carpanServisi != null ? _carpanServisi.GetTotalMultiplierForSpin() : 1;
        int nihaiOdeme = _carpanServisi != null ? _carpanServisi.MulClampInt(spinKazancHam, toplamCarpan) : spinKazancHam;
        kayit.ToplamHamKazanc = spinKazancHam;
        kayit.NihaiCarpanToplam = toplamCarpan;

        int bahis = _ekonomiServisi.Bahis;
        int deneme = 0;
        Debug.Log($"[KONSTRUKTE] Sim bitti: ham={spinKazancHam} carpan={toplamCarpan} nihai={nihaiOdeme} adimSayisi={kayit.Adimlar.Count}");

        if (!SpinKaydiHamPaytableIleUyumluMu(kayit, bahis, tumbleEsik))
        {
            Debug.LogWarning($"[KONSTRUKTE] SpinKaydiHamPaytableIleUyumluMu BAŞARISIZ: ham={spinKazancHam} nihai={nihaiOdeme}");
            return null;
        }

        bool zorlaCarpanVardi = zorlaCarpanDegeri > 0;
        if (!bonusSpin && !zorlaCarpanVardi && !OdemeModelineUygunMu(nihaiOdeme, bahis, deneme, maxReroll))
        {
            Debug.LogWarning($"[KONSTRUKTE] OdemeModelineUygunMu BAŞARISIZ: nihai={nihaiOdeme} bahis={bahis} min~={_minOdemeTL} max~={_maxOdemeTL}");
            return null;
        }

        if (adminVideoArdisikKazanc && nihaiOdeme <= bahis)
        {
            Debug.LogWarning("[KONSTRUKTE] adminVideoArdisikKazanc reddi");
            return null;
        }

        if (limit != int.MaxValue && nihaiOdeme > limit)
        {
            Debug.LogWarning($"[KONSTRUKTE] limit aşıldı: nihai={nihaiOdeme} limit={limit}");
            return null;
        }

        if (!bonusSpin && !adminManuelMod && !adminVideoArdisikKazanc && SenaryoYoneticisi.I != null
            && SenaryoYoneticisi.I.ShouldForceNoPaySenaryo12() && nihaiOdeme > 0)
        {
            Debug.LogWarning("[KONSTRUKTE] ShouldForceNoPaySenaryo12 reddi");
            return null;
        }

        if (!zorlaCarpanVardi)
        {
            if (spinPolitikasi.KolayZorlukBonusSpindeMinOdemeAltindaReddet(
                    bonusSpin, zorlaCarpanVardi, limit, _easyBias01, nihaiOdeme))
            {
                Debug.LogWarning($"[KONSTRUKTE] KolayZorlukBonusSpin reddi: nihai={nihaiOdeme}");
                return null;
            }
            if (spinPolitikasi.KolayZorlukTumblesizSonuctaYenidenDene(
                    bonusSpin,
                    zorlaCarpanVardi,
                    limit,
                    _easyBias01,
                    kayit,
                    SenaryoYoneticisi.I != null,
                    _otomatikSpinKalan))
            {
                Debug.LogWarning($"[KONSTRUKTE] KolayZorlukTumblesiz reddi: adimSayisi={kayit?.Adimlar?.Count}");
                return null;
            }
        }

        kayit.SenaryoOdemeBandinaUygun = true;
        kayit.ZorlaCarpanKullanildi = zorlaCarpanDegeri > 0;
        if (!bonusSpin && !zorlaCarpanVardi && !IsAdminSenaryo2Aktif() && !IsAdminSenaryo3Aktif())
            UstUsteDonguyuSpinSonucuIleIlerle(nihaiOdeme > bahis);
        if (adminVideoArdisikKazanc && nihaiOdeme > bahis)
        {
            _adminVideoArdisikKazancSpinKalan = Mathf.Max(0, _adminVideoArdisikKazancSpinKalan - 1);
            Debug.Log($"[ADMIN][VIDEO] Arka arkaya kazançlı spin kaldı: {_adminVideoArdisikKazancSpinKalan}");
        }
        if (bakiye50KteTumbleKapamaAktif && !bonusSpin && _bakiye50KUstundeTumbleKapaliKalanSpin > 0)
            _bakiye50KUstundeTumbleKapaliKalanSpin--;

        string konstrukteModStr = _senaryo1KonstrukteIkinciKumeBoy > 0
            ? $"iki tumble: sym1={kSym}×{kCnt} + sym2={_senaryo1KonstrukteIkinciKumeSembol}×{_senaryo1KonstrukteIkinciKumeBoy}"
            : $"tek tumble: sym={kSym}×{kCnt}";
        Debug.Log($"[SENARYO1][KONSTRUKTE] {konstrukteModStr} hedef≈{beklenenTl} nihai={nihaiOdeme}");
        return kayit;
    }

    private SpinSimulasyonKaydi Senaryo2KazancKonstrukteHedefSpinDene(
        int limit,
        bool bonusSpin,
        bool adminManuelMod,
        bool adminVideoArdisikKazanc,
        int maxReroll,
        bool ustUsteAktif,
        ISenaryoSpinPolitikasi spinPolitikasi,
        int zorlaCarpanDegeri,
        bool allowIkiTumble = true)
    {
        _ = ustUsteAktif;
        if (bonusSpin || zorlaCarpanDegeri > 0 || tumbleAyarlari == null || grid == null || _ekonomiServisi == null)
        {
            Debug.LogWarning("[S2_KAZ] Erken çıkış");
            return null;
        }

        int bahis0 = Mathf.Max(1, _ekonomiServisi.Bahis);
        int sembolSayisi = tumbleAyarlari.PayTable_8_9 != null ? tumbleAyarlari.PayTable_8_9.Length : 0;
        int kSym, kCnt, beklenenTl;
        _senaryo2KonstrukteIkinciKumeSembol = -1;
        _senaryo2KonstrukteIkinciKumeBoy = 0;
        _senaryo2KonstrukteMaxTumbleAdimi = 1;

        if (allowIkiTumble && Senaryo2HedefOdemeMotoru.TryPaytableUyumluIkiTumbleKumesiSec(
                tumbleAyarlari, bahis0, _minOdemeTL, _maxOdemeTL, _senaryo2SonZorunluNihaiOdeme,
                _scatterIndexCache, sutun, satir,
                out int kSym1, out int kCnt1, out int tl1,
                out int kSym2, out int kCnt2, out int tl2))
        {
            kSym = kSym1; kCnt = kCnt1; beklenenTl = tl1 + tl2;
            _senaryo2KonstrukteIkinciKumeSembol = kSym2;
            _senaryo2KonstrukteIkinciKumeBoy = kCnt2;
            _senaryo2KonstrukteMaxTumbleAdimi = 2;
        }
        else if (!Senaryo2HedefOdemeMotoru.TryPaytableUyumluTekKumeSec(
                     tumbleAyarlari, bahis0, _minOdemeTL, _maxOdemeTL, _senaryo2SonZorunluNihaiOdeme,
                     _scatterIndexCache, sutun, satir, out kSym, out kCnt, out beklenenTl))
            return null;

        Debug.Log($"[S2_KAZ] allowIkiTumble={allowIkiTumble} | kSym={kSym} kCnt={kCnt} beklenenTl={beklenenTl} | ikinciSym={_senaryo2KonstrukteIkinciKumeSembol} ikinciCnt={_senaryo2KonstrukteIkinciKumeBoy}");
        if (!Senaryo2HedefOdemeMotoru.TryTekKumeliIlkGridOlustur(
                sutun, satir, kSym, kCnt, _scatterIndexCache, sembolSayisi, out int[,] yeniGrid))
        {
            Debug.LogWarning("[S2_KAZ] TryTekKumeliIlkGridOlustur BAŞARISIZ");
            return null;
        }

        for (int x = 0; x < sutun; x++)
            for (int y = 0; y < satir; y++)
                grid[x, y] = yeniGrid[x, y];
        _tumbleServisi?.SetGrid(grid);
        zorlaSiradakiCarpan = zorlaCarpanDegeri;
        UI_CarpanSifirla();
        _izgaraServisi?.ResetScatterCountPerSpin();
        spinKazancHam = 0;

        var kayit = new SpinSimulasyonKaydi { Sutun = sutun, Satir = satir };
        SimulasyonKaydinaMevcutIlkGridStateKopyala(kayit);
        if (_carpanServisi != null && kayit.IlkCarpanDegerleri != null && kayit.IlkCarpanDegerleri.Count > 0)
            _carpanServisi.RecordPlacedCarpanlar(kayit.IlkCarpanDegerleri);

        int turSayaci = 0;
        int tumbleEsik = OyunKorumaServisi.TUMBLE_SABIT_ESIK;
        minClusterSize = tumbleEsik;
        bool limitAsildi = false;

        _senaryo2KonstrukteSimAktif = true;
        try
        {
            while (turSayaci < OyunKorumaServisi.MAX_TUMBLE_TUR)
            {
                var toRemove = _tumbleServisi != null ? _tumbleServisi.FindClustersToRemove(tumbleEsik) : new List<Vector2Int>();
                if (toRemove == null || toRemove.Count == 0) break;

                // Çarpan toggle açıksa olasılıksal schedule; bant kontrolü (nihaiOdeme > maxTl) halleder.
                CarpanUretVeBirik();

                int turHam = tumbleAyarlari != null
                    ? tumbleAyarlari.CalculateWinWithOwnPayTable(toRemove, grid, satir, sutun, _ekonomiServisi.Bahis, tumbleEsik)
                    : 0;
                if (bonusSpin && zorlaCarpanDegeri <= 0 && _senaryoServisi != null)
                {
                    int kalan = _senaryoServisi.GetBonusRemainingPayableTL();
                    int m = _carpanServisi != null ? _carpanServisi.GetCurrentMultiplierInt() : 1;
                    if (m < 1) m = 1;
                    long proj = (long)(spinKazancHam + turHam) * (long)m;
                    if (kalan <= 0 || proj > (long)kalan) { limitAsildi = true; break; }
                }
                spinKazancHam += turHam;

                var adim = new TumbleAdimKaydi { TurKazanci = turHam };
                adim.PatlayanHucreler.AddRange(toRemove);
                GridHucreleriniTemizle(toRemove);
                if (_cokmeAkisServisi != null) _cokmeAkisServisi.CokmeDoldurSadeceMantik(adim);
                kayit.Adimlar.Add(adim);
                turSayaci++;

                if (_senaryo2KonstrukteIkinciKumeSembol >= 0 && kayit.Adimlar.Count == 1)
                {
                    var oncekiSym2 = new System.Collections.Generic.HashSet<Vector2Int>();
                    int injSym = _senaryo2KonstrukteIkinciKumeSembol;
                    for (int xi = 0; xi < sutun; xi++)
                        for (int yi = 0; yi < satir; yi++)
                            if (grid[xi, yi] == injSym) oncekiSym2.Add(new Vector2Int(xi, yi));

                    if (Senaryo2HedefOdemeMotoru.TryIkinciTumbleKumesiRefillSonrasiEnjekteEt(
                            grid, sutun, satir, _senaryo2KonstrukteIkinciKumeSembol, _senaryo2KonstrukteIkinciKumeBoy,
                            _scatterIndexCache, sembolSayisi,
                            kayit.Adimlar[kayit.Adimlar.Count - 1].YeniSpawnEdilenHucreler))
                    {
                        var sonAdim = kayit.Adimlar[kayit.Adimlar.Count - 1];
                        if (sonAdim.GridRefillSonrasi != null)
                            for (int xi = 0; xi < sutun; xi++)
                                for (int yi = 0; yi < satir; yi++)
                                    sonAdim.GridRefillSonrasi[xi, yi] = grid[xi, yi];

                        for (int xi = 0; xi < sutun; xi++)
                            for (int yi = 0; yi < satir; yi++)
                            {
                                var pos = new Vector2Int(xi, yi);
                                if (grid[xi, yi] == injSym && !oncekiSym2.Contains(pos))
                                    sonAdim.InjekteEdilenHucreler.Add(pos);
                            }

                        _tumbleServisi?.SetGrid(grid);
                        _senaryo2KonstrukteIkinciKumeSembol = -1;
                    }
                    else
                    {
                        Debug.LogWarning("[SENARYO2][KONSTRUKTE] İkinci küme enjekte edilemedi; tek tumble ile devam.");
                        _senaryo2KonstrukteIkinciKumeSembol = -1;
                        _senaryo2KonstrukteMaxTumbleAdimi = 1;
                    }
                }

                if (_senaryo2KonstrukteSimAktif && kayit.Adimlar.Count >= _senaryo2KonstrukteMaxTumbleAdimi)
                    break;
            }
        }
        finally
        {
            _senaryo2KonstrukteSimAktif = false;
            _senaryo2KonstrukteIkinciKumeSembol = -1;
            _senaryo2KonstrukteMaxTumbleAdimi = 1;
        }

        if (limitAsildi) { Debug.LogWarning("[S2_KAZ] limitAsildi → null"); return null; }

        KonstrukteRefillSonrasiKazancsizYap(kayit);
        int toplamCarpan = _carpanServisi != null ? _carpanServisi.GetTotalMultiplierForSpin() : 1;
        int nihaiOdeme = _carpanServisi != null ? _carpanServisi.MulClampInt(spinKazancHam, toplamCarpan) : spinKazancHam;
        kayit.ToplamHamKazanc = spinKazancHam;
        kayit.NihaiCarpanToplam = toplamCarpan;
        int bahis = _ekonomiServisi.Bahis;
        int deneme = 0;

        Debug.Log($"[S2_KAZ] Sim bitti: ham={spinKazancHam} carpan={toplamCarpan} nihai={nihaiOdeme} adim={kayit.Adimlar.Count}");

        if (!SpinKaydiHamPaytableIleUyumluMu(kayit, bahis, tumbleEsik))
        {
            Debug.LogWarning("[S2_KAZ] PaytableUyum BAŞARISIZ"); return null;
        }
        bool zorlaCarpanVardi = zorlaCarpanDegeri > 0;
        if (!bonusSpin && !zorlaCarpanVardi && !OdemeModelineUygunMu(nihaiOdeme, bahis, deneme, maxReroll))
        {
            Debug.LogWarning($"[S2_KAZ] OdemeModeli BAŞARISIZ: nihai={nihaiOdeme}"); return null;
        }
        if (limit != int.MaxValue && nihaiOdeme > limit)
        {
            Debug.LogWarning("[S2_KAZ] limit aşıldı"); return null;
        }
        if (!bonusSpin && !adminManuelMod && !adminVideoArdisikKazanc && SenaryoYoneticisi.I != null
            && SenaryoYoneticisi.I.ShouldForceNoPaySenaryo12() && nihaiOdeme > 0)
        {
            Debug.LogWarning("[S2_KAZ] ShouldForceNoPay reddi"); return null;
        }
        if (!zorlaCarpanVardi)
        {
            if (spinPolitikasi.KolayZorlukBonusSpindeMinOdemeAltindaReddet(bonusSpin, zorlaCarpanVardi, limit, _easyBias01, nihaiOdeme))
            {
                Debug.LogWarning("[S2_KAZ] KolayZorluk reddi"); return null;
            }
            if (spinPolitikasi.KolayZorlukTumblesizSonuctaYenidenDene(bonusSpin, zorlaCarpanVardi, limit, _easyBias01, kayit, SenaryoYoneticisi.I != null, _otomatikSpinKalan))
            {
                Debug.LogWarning("[S2_KAZ] KolayZorlukTumblesiz reddi"); return null;
            }
        }

        kayit.SenaryoOdemeBandinaUygun = true;
        kayit.ZorlaCarpanKullanildi = zorlaCarpanDegeri > 0;
        if (!bonusSpin && !zorlaCarpanVardi && !IsAdminSenaryo2Aktif() && !IsAdminSenaryo3Aktif())
            UstUsteDonguyuSpinSonucuIleIlerle(nihaiOdeme > bahis);

        string s2Mod = _senaryo2KonstrukteIkinciKumeBoy > 0
            ? $"iki tumble: sym1={kSym}×{kCnt} + sym2={_senaryo2KonstrukteIkinciKumeSembol}×{_senaryo2KonstrukteIkinciKumeBoy}"
            : $"tek tumble: sym={kSym}×{kCnt}";
        Debug.Log($"[SENARYO2][KAZANÇ][KONSTRUKTE] {s2Mod} hedef≈{beklenenTl} nihai={nihaiOdeme}");
        return kayit;
    }

    private SpinSimulasyonKaydi Senaryo2KayipKonstrukteHedefSpinDene(
        int limit,
        bool bonusSpin,
        bool adminManuelMod,
        bool adminVideoArdisikKazanc,
        int maxReroll,
        bool ustUsteAktif,
        ISenaryoSpinPolitikasi spinPolitikasi,
        int zorlaCarpanDegeri)
    {
        _ = ustUsteAktif; _ = adminManuelMod; _ = adminVideoArdisikKazanc; _ = maxReroll; _ = spinPolitikasi;
        if (bonusSpin || zorlaCarpanDegeri > 0 || tumbleAyarlari == null || grid == null || _ekonomiServisi == null)
        {
            Debug.LogWarning("[S2_KAY] Erken çıkış");
            return null;
        }

        int bahis = Mathf.Max(1, _ekonomiServisi.Bahis);
        int sembolSayisi = tumbleAyarlari.PayTable_8_9 != null ? tumbleAyarlari.PayTable_8_9.Length : 0;

        if (!Senaryo2HedefOdemeMotoru.TryMinimalKayipGridOlustur(
                tumbleAyarlari, bahis, _scatterIndexCache, sutun, satir,
                out int kayipSembol, out int kayipCnt, out int[,] yeniGrid))
        {
            Debug.LogWarning("[S2_KAY] TryMinimalKayipGridOlustur BAŞARISIZ");
            return null;
        }

        for (int x = 0; x < sutun; x++)
            for (int y = 0; y < satir; y++)
                grid[x, y] = yeniGrid[x, y];
        _tumbleServisi?.SetGrid(grid);
        zorlaSiradakiCarpan = zorlaCarpanDegeri;
        UI_CarpanSifirla();
        _izgaraServisi?.ResetScatterCountPerSpin();
        spinKazancHam = 0;

        var kayit = new SpinSimulasyonKaydi { Sutun = sutun, Satir = satir };
        SimulasyonKaydinaMevcutIlkGridStateKopyala(kayit);
        if (_carpanServisi != null && kayit.IlkCarpanDegerleri != null && kayit.IlkCarpanDegerleri.Count > 0)
            _carpanServisi.RecordPlacedCarpanlar(kayit.IlkCarpanDegerleri);

        int turSayaci = 0;
        int tumbleEsik = OyunKorumaServisi.TUMBLE_SABIT_ESIK;
        minClusterSize = tumbleEsik;
        _senaryo2KonstrukteMaxTumbleAdimi = 1;

        _senaryo2KonstrukteSimAktif = true;
        try
        {
            while (turSayaci < OyunKorumaServisi.MAX_TUMBLE_TUR)
            {
                var toRemove = _tumbleServisi != null ? _tumbleServisi.FindClustersToRemove(tumbleEsik) : new List<Vector2Int>();
                if (toRemove == null || toRemove.Count == 0) break;

                _carpanServisi?.ClearPendingDrops();
                _bombaPatlamaSonrasiIlkRefillCarpanEngeli = true;

                int turHam = tumbleAyarlari != null
                    ? tumbleAyarlari.CalculateWinWithOwnPayTable(toRemove, grid, satir, sutun, _ekonomiServisi.Bahis, tumbleEsik)
                    : 0;
                spinKazancHam += turHam;

                var adim = new TumbleAdimKaydi { TurKazanci = turHam };
                adim.PatlayanHucreler.AddRange(toRemove);
                GridHucreleriniTemizle(toRemove);
                if (_cokmeAkisServisi != null) _cokmeAkisServisi.CokmeDoldurSadeceMantik(adim);
                kayit.Adimlar.Add(adim);
                turSayaci++;

                if (_senaryo2KonstrukteSimAktif && kayit.Adimlar.Count >= _senaryo2KonstrukteMaxTumbleAdimi)
                    break;
            }
        }
        finally
        {
            _senaryo2KonstrukteSimAktif = false;
            _senaryo2KonstrukteMaxTumbleAdimi = 1;
        }

        KonstrukteRefillSonrasiKazancsizYap(kayit);
        int toplamCarpan = _carpanServisi != null ? _carpanServisi.GetTotalMultiplierForSpin() : 1;
        int nihaiOdeme = _carpanServisi != null ? _carpanServisi.MulClampInt(spinKazancHam, toplamCarpan) : spinKazancHam;
        kayit.ToplamHamKazanc = spinKazancHam;
        kayit.NihaiCarpanToplam = toplamCarpan;

        if (nihaiOdeme >= bahis)
        {
            Debug.LogWarning($"[S2_KAY] nihai={nihaiOdeme} >= bahis={bahis}, kayıp üretilemedi");
            return null;
        }
        if (limit != int.MaxValue && nihaiOdeme > limit)
        {
            Debug.LogWarning("[S2_KAY] limit aşıldı"); return null;
        }

        kayit.SenaryoOdemeBandinaUygun = true;
        kayit.ZorlaCarpanKullanildi = zorlaCarpanDegeri > 0;
        if (!bonusSpin && zorlaCarpanDegeri <= 0 && !IsAdminSenaryo2Aktif() && !IsAdminSenaryo3Aktif())
            UstUsteDonguyuSpinSonucuIleIlerle(nihaiOdeme > bahis);

        Debug.Log($"[SENARYO2][KAYIP][KONSTRUKTE] sym={kayipSembol}×{kayipCnt} nihai={nihaiOdeme} (bahis={bahis})");
        return kayit;
    }

    private SpinSimulasyonKaydi Senaryo3KazancKonstrukteHedefSpinDene(
        int limit,
        bool bonusSpin,
        bool adminManuelMod,
        bool adminVideoArdisikKazanc,
        int maxReroll,
        bool ustUsteAktif,
        ISenaryoSpinPolitikasi spinPolitikasi,
        int zorlaCarpanDegeri,
        bool allowIkiTumble = true)
    {
        _ = ustUsteAktif;
        if (bonusSpin || zorlaCarpanDegeri > 0 || tumbleAyarlari == null || grid == null || _ekonomiServisi == null)
        {
            Debug.LogWarning("[S3_KAZ] Erken çıkış");
            return null;
        }

        int bahis0 = Mathf.Max(1, _ekonomiServisi.Bahis);
        int sembolSayisi = tumbleAyarlari.PayTable_8_9 != null ? tumbleAyarlari.PayTable_8_9.Length : 0;
        int kSym, kCnt, beklenenTl;
        _senaryo3KonstrukteIkinciKumeSembol = -1;
        _senaryo3KonstrukteIkinciKumeBoy = 0;
        _senaryo3KonstrukteMaxTumbleAdimi = 1;

        // S3 kazanç bant her zaman bahis+100..bahis+600; uniform random hedef → 100-600 TL profit çeşitliliği
        int s3WinMinTl = bahis0 + 100;
        int s3WinMaxTl = bahis0 + 600;
        int s3WinHedef = UnityEngine.Random.Range(s3WinMinTl, s3WinMaxTl + 1);
        Debug.Log($"[S3_KAZ] WinBant={s3WinMinTl}..{s3WinMaxTl} hedef={s3WinHedef}");

        if (allowIkiTumble && Senaryo3HedefOdemeMotoru.TryPaytableUyumluIkiTumbleKumesiSec(
                tumbleAyarlari, bahis0, s3WinMinTl, s3WinMaxTl, s3WinHedef,
                _scatterIndexCache, sutun, satir,
                out int kSym1, out int kCnt1, out int tl1,
                out int kSym2, out int kCnt2, out int tl2))
        {
            kSym = kSym1; kCnt = kCnt1; beklenenTl = tl1 + tl2;
            _senaryo3KonstrukteIkinciKumeSembol = kSym2;
            _senaryo3KonstrukteIkinciKumeBoy = kCnt2;
            _senaryo3KonstrukteMaxTumbleAdimi = 2;
        }
        else if (!Senaryo3HedefOdemeMotoru.TryPaytableUyumluTekKumeSec(
                     tumbleAyarlari, bahis0, s3WinMinTl, s3WinMaxTl, s3WinHedef,
                     _scatterIndexCache, sutun, satir, out kSym, out kCnt, out beklenenTl))
            return null;

        Debug.Log($"[S3_KAZ] allowIkiTumble={allowIkiTumble} | kSym={kSym} kCnt={kCnt} beklenenTl={beklenenTl} | ikinciSym={_senaryo3KonstrukteIkinciKumeSembol} ikinciCnt={_senaryo3KonstrukteIkinciKumeBoy}");
        if (!Senaryo3HedefOdemeMotoru.TryTekKumeliIlkGridOlustur(
                sutun, satir, kSym, kCnt, _scatterIndexCache, sembolSayisi, out int[,] yeniGrid))
        {
            Debug.LogWarning("[S3_KAZ] TryTekKumeliIlkGridOlustur BAŞARISIZ");
            return null;
        }

        for (int x = 0; x < sutun; x++)
            for (int y = 0; y < satir; y++)
                grid[x, y] = yeniGrid[x, y];
        _tumbleServisi?.SetGrid(grid);
        zorlaSiradakiCarpan = zorlaCarpanDegeri;
        UI_CarpanSifirla();
        _izgaraServisi?.ResetScatterCountPerSpin();
        spinKazancHam = 0;

        var kayit = new SpinSimulasyonKaydi { Sutun = sutun, Satir = satir };
        SimulasyonKaydinaMevcutIlkGridStateKopyala(kayit);
        if (_carpanServisi != null && kayit.IlkCarpanDegerleri != null && kayit.IlkCarpanDegerleri.Count > 0)
            _carpanServisi.RecordPlacedCarpanlar(kayit.IlkCarpanDegerleri);

        int turSayaci = 0;
        int tumbleEsik = OyunKorumaServisi.TUMBLE_SABIT_ESIK;
        minClusterSize = tumbleEsik;
        bool limitAsildi = false;

        _senaryo3KonstrukteSimAktif = true;
        try
        {
            while (turSayaci < OyunKorumaServisi.MAX_TUMBLE_TUR)
            {
                var toRemove = _tumbleServisi != null ? _tumbleServisi.FindClustersToRemove(tumbleEsik) : new List<Vector2Int>();
                if (toRemove == null || toRemove.Count == 0) break;

                // S3 kazanç bandı dar (bahis+100..bahis+600) — çarpan patlarsa bant dışına çıkar; engelle.
                _carpanServisi?.ClearPendingDrops();
                _bombaPatlamaSonrasiIlkRefillCarpanEngeli = true;

                int turHam = tumbleAyarlari != null
                    ? tumbleAyarlari.CalculateWinWithOwnPayTable(toRemove, grid, satir, sutun, _ekonomiServisi.Bahis, tumbleEsik)
                    : 0;
                if (bonusSpin && zorlaCarpanDegeri <= 0 && _senaryoServisi != null)
                {
                    int kalan = _senaryoServisi.GetBonusRemainingPayableTL();
                    int m = _carpanServisi != null ? _carpanServisi.GetCurrentMultiplierInt() : 1;
                    if (m < 1) m = 1;
                    long proj = (long)(spinKazancHam + turHam) * (long)m;
                    if (kalan <= 0 || proj > (long)kalan) { limitAsildi = true; break; }
                }
                spinKazancHam += turHam;

                var adim = new TumbleAdimKaydi { TurKazanci = turHam };
                adim.PatlayanHucreler.AddRange(toRemove);
                GridHucreleriniTemizle(toRemove);
                if (_cokmeAkisServisi != null) _cokmeAkisServisi.CokmeDoldurSadeceMantik(adim);
                kayit.Adimlar.Add(adim);
                turSayaci++;

                if (_senaryo3KonstrukteIkinciKumeSembol >= 0 && kayit.Adimlar.Count == 1)
                {
                    var oncekiSym2 = new System.Collections.Generic.HashSet<Vector2Int>();
                    int injSym = _senaryo3KonstrukteIkinciKumeSembol;
                    for (int xi = 0; xi < sutun; xi++)
                        for (int yi = 0; yi < satir; yi++)
                            if (grid[xi, yi] == injSym) oncekiSym2.Add(new Vector2Int(xi, yi));

                    if (Senaryo3HedefOdemeMotoru.TryIkinciTumbleKumesiRefillSonrasiEnjekteEt(
                            grid, sutun, satir, _senaryo3KonstrukteIkinciKumeSembol, _senaryo3KonstrukteIkinciKumeBoy,
                            _scatterIndexCache, sembolSayisi,
                            kayit.Adimlar[kayit.Adimlar.Count - 1].YeniSpawnEdilenHucreler))
                    {
                        var sonAdim = kayit.Adimlar[kayit.Adimlar.Count - 1];
                        if (sonAdim.GridRefillSonrasi != null)
                            for (int xi = 0; xi < sutun; xi++)
                                for (int yi = 0; yi < satir; yi++)
                                    sonAdim.GridRefillSonrasi[xi, yi] = grid[xi, yi];

                        for (int xi = 0; xi < sutun; xi++)
                            for (int yi = 0; yi < satir; yi++)
                            {
                                var pos = new Vector2Int(xi, yi);
                                if (grid[xi, yi] == injSym && !oncekiSym2.Contains(pos))
                                    sonAdim.InjekteEdilenHucreler.Add(pos);
                            }

                        _tumbleServisi?.SetGrid(grid);
                        _senaryo3KonstrukteIkinciKumeSembol = -1;
                    }
                    else
                    {
                        Debug.LogWarning("[SENARYO3][KONSTRUKTE] İkinci küme enjekte edilemedi; tek tumble ile devam.");
                        _senaryo3KonstrukteIkinciKumeSembol = -1;
                        _senaryo3KonstrukteMaxTumbleAdimi = 1;
                    }
                }

                if (_senaryo3KonstrukteSimAktif && kayit.Adimlar.Count >= _senaryo3KonstrukteMaxTumbleAdimi)
                    break;
            }
        }
        finally
        {
            _senaryo3KonstrukteSimAktif = false;
            _senaryo3KonstrukteIkinciKumeSembol = -1;
            _senaryo3KonstrukteMaxTumbleAdimi = 1;
        }

        if (limitAsildi) { Debug.LogWarning("[S3_KAZ] limitAsildi → null"); return null; }

        KonstrukteRefillSonrasiKazancsizYap(kayit);
        int toplamCarpan = _carpanServisi != null ? _carpanServisi.GetTotalMultiplierForSpin() : 1;
        int nihaiOdeme = _carpanServisi != null ? _carpanServisi.MulClampInt(spinKazancHam, toplamCarpan) : spinKazancHam;
        kayit.ToplamHamKazanc = spinKazancHam;
        kayit.NihaiCarpanToplam = toplamCarpan;
        int bahis = _ekonomiServisi.Bahis;
        int deneme = 0;

        Debug.Log($"[S3_KAZ] Sim bitti: ham={spinKazancHam} carpan={toplamCarpan} nihai={nihaiOdeme} adim={kayit.Adimlar.Count}");

        if (!SpinKaydiHamPaytableIleUyumluMu(kayit, bahis, tumbleEsik))
        {
            Debug.LogWarning("[S3_KAZ] PaytableUyum BAŞARISIZ"); return null;
        }
        bool zorlaCarpanVardi = zorlaCarpanDegeri > 0;
        if (!bonusSpin && !zorlaCarpanVardi && !OdemeModelineUygunMu(nihaiOdeme, bahis, deneme, maxReroll))
        {
            Debug.LogWarning($"[S3_KAZ] OdemeModeli BAŞARISIZ: nihai={nihaiOdeme}"); return null;
        }
        if (limit != int.MaxValue && nihaiOdeme > limit)
        {
            Debug.LogWarning("[S3_KAZ] limit aşıldı"); return null;
        }
        if (!bonusSpin && !adminManuelMod && !adminVideoArdisikKazanc && SenaryoYoneticisi.I != null
            && SenaryoYoneticisi.I.ShouldForceNoPaySenaryo12() && nihaiOdeme > 0)
        {
            Debug.LogWarning("[S3_KAZ] ShouldForceNoPay reddi"); return null;
        }
        if (!zorlaCarpanVardi)
        {
            if (spinPolitikasi.KolayZorlukBonusSpindeMinOdemeAltindaReddet(bonusSpin, zorlaCarpanVardi, limit, _easyBias01, nihaiOdeme))
            {
                Debug.LogWarning("[S3_KAZ] KolayZorluk reddi"); return null;
            }
            if (spinPolitikasi.KolayZorlukTumblesizSonuctaYenidenDene(bonusSpin, zorlaCarpanVardi, limit, _easyBias01, kayit, SenaryoYoneticisi.I != null, _otomatikSpinKalan))
            {
                Debug.LogWarning("[S3_KAZ] KolayZorlukTumblesiz reddi"); return null;
            }
        }

        kayit.SenaryoOdemeBandinaUygun = true;
        kayit.ZorlaCarpanKullanildi = zorlaCarpanDegeri > 0;
        if (!bonusSpin && !zorlaCarpanVardi && !IsAdminSenaryo2Aktif() && !IsAdminSenaryo3Aktif())
            UstUsteDonguyuSpinSonucuIleIlerle(nihaiOdeme > bahis);

        string s3Mod = _senaryo3KonstrukteIkinciKumeBoy > 0
            ? $"iki tumble: sym1={kSym}×{kCnt} + sym2={_senaryo3KonstrukteIkinciKumeSembol}×{_senaryo3KonstrukteIkinciKumeBoy}"
            : $"tek tumble: sym={kSym}×{kCnt}";
        Debug.Log($"[SENARYO3][KAZANÇ][KONSTRUKTE] {s3Mod} hedef≈{beklenenTl} nihai={nihaiOdeme}");
        return kayit;
    }

    private SpinSimulasyonKaydi Senaryo3KayipKonstrukteHedefSpinDene(
        int limit,
        bool bonusSpin,
        bool adminManuelMod,
        bool adminVideoArdisikKazanc,
        int maxReroll,
        bool ustUsteAktif,
        ISenaryoSpinPolitikasi spinPolitikasi,
        int zorlaCarpanDegeri)
    {
        _ = ustUsteAktif; _ = adminManuelMod; _ = adminVideoArdisikKazanc; _ = maxReroll; _ = spinPolitikasi;
        if (bonusSpin || zorlaCarpanDegeri > 0 || tumbleAyarlari == null || grid == null || _ekonomiServisi == null)
        {
            Debug.LogWarning("[S3_KAY] Erken çıkış");
            return null;
        }

        int bahis = Mathf.Max(1, _ekonomiServisi.Bahis);
        int sembolSayisi = tumbleAyarlari.PayTable_8_9 != null ? tumbleAyarlari.PayTable_8_9.Length : 0;

        // Son kayıp spini (index 4) kesinlikle sıfır; diğerleri %20 şans
        bool sifirOdemeDeneme = (_senaryo3DonguIndex == 4) || (UnityEngine.Random.value < 0.20f);
        if (sifirOdemeDeneme)
        {
            _izgaraServisi?.FillRandomAll(0);
            Debug.Log("[S3_KAY] Sıfır ödeme: FillRandomAll(0) çağrıldı");
        }
        else
        {
            if (!Senaryo3HedefOdemeMotoru.TryYuksekPayKayipGridOlustur(
                    tumbleAyarlari, bahis, _scatterIndexCache, sutun, satir,
                    out int kayipSembol, out int kayipCnt, out int[,] yeniGrid))
            {
                Debug.LogWarning("[S3_KAY] TryYuksekPayKayipGridOlustur BAŞARISIZ");
                return null;
            }
            for (int x = 0; x < sutun; x++)
                for (int y = 0; y < satir; y++)
                    grid[x, y] = yeniGrid[x, y];
        }
        _tumbleServisi?.SetGrid(grid);
        zorlaSiradakiCarpan = zorlaCarpanDegeri;
        UI_CarpanSifirla();
        _izgaraServisi?.ResetScatterCountPerSpin();
        spinKazancHam = 0;

        var kayit = new SpinSimulasyonKaydi { Sutun = sutun, Satir = satir };
        SimulasyonKaydinaMevcutIlkGridStateKopyala(kayit);
        if (_carpanServisi != null && kayit.IlkCarpanDegerleri != null && kayit.IlkCarpanDegerleri.Count > 0)
            _carpanServisi.RecordPlacedCarpanlar(kayit.IlkCarpanDegerleri);

        int turSayaci = 0;
        int tumbleEsik = OyunKorumaServisi.TUMBLE_SABIT_ESIK;
        minClusterSize = tumbleEsik;
        _senaryo3KonstrukteMaxTumbleAdimi = 1;

        _senaryo3KonstrukteSimAktif = true;
        try
        {
            while (turSayaci < OyunKorumaServisi.MAX_TUMBLE_TUR)
            {
                var toRemove = _tumbleServisi != null ? _tumbleServisi.FindClustersToRemove(tumbleEsik) : new List<Vector2Int>();
                if (toRemove == null || toRemove.Count == 0) break;

                _carpanServisi?.ClearPendingDrops();
                _bombaPatlamaSonrasiIlkRefillCarpanEngeli = true;

                int turHam = tumbleAyarlari != null
                    ? tumbleAyarlari.CalculateWinWithOwnPayTable(toRemove, grid, satir, sutun, _ekonomiServisi.Bahis, tumbleEsik)
                    : 0;
                spinKazancHam += turHam;

                var adim = new TumbleAdimKaydi { TurKazanci = turHam };
                adim.PatlayanHucreler.AddRange(toRemove);
                GridHucreleriniTemizle(toRemove);
                if (_cokmeAkisServisi != null) _cokmeAkisServisi.CokmeDoldurSadeceMantik(adim);
                kayit.Adimlar.Add(adim);
                turSayaci++;

                if (_senaryo3KonstrukteSimAktif && kayit.Adimlar.Count >= _senaryo3KonstrukteMaxTumbleAdimi)
                    break;
            }
        }
        finally
        {
            _senaryo3KonstrukteSimAktif = false;
            _senaryo3KonstrukteMaxTumbleAdimi = 1;
        }

        KonstrukteRefillSonrasiKazancsizYap(kayit);
        int toplamCarpan = _carpanServisi != null ? _carpanServisi.GetTotalMultiplierForSpin() : 1;
        int nihaiOdeme = _carpanServisi != null ? _carpanServisi.MulClampInt(spinKazancHam, toplamCarpan) : spinKazancHam;
        kayit.ToplamHamKazanc = spinKazancHam;
        kayit.NihaiCarpanToplam = toplamCarpan;

        if (nihaiOdeme >= bahis)
        {
            Debug.LogWarning($"[S3_KAY] nihai={nihaiOdeme} >= bahis={bahis}, kayıp üretilemedi");
            return null;
        }
        if (limit != int.MaxValue && nihaiOdeme > limit)
        {
            Debug.LogWarning("[S3_KAY] limit aşıldı"); return null;
        }

        kayit.SenaryoOdemeBandinaUygun = true;
        kayit.ZorlaCarpanKullanildi = zorlaCarpanDegeri > 0;
        if (!bonusSpin && zorlaCarpanDegeri <= 0 && !IsAdminSenaryo2Aktif() && !IsAdminSenaryo3Aktif())
            UstUsteDonguyuSpinSonucuIleIlerle(nihaiOdeme > bahis);

        Debug.Log($"[SENARYO3][KAYIP][KONSTRUKTE] nihai={nihaiOdeme} (bahis={bahis}) sifirOdeme={sifirOdemeDeneme}");
        return kayit;
    }

    private SpinSimulasyonKaydi Senaryo4KazancKonstrukteHedefSpinDene(
        int limit, bool bonusSpin, bool adminManuelMod, bool adminVideoArdisikKazanc,
        int maxReroll, bool ustUsteAktif, ISenaryoSpinPolitikasi spinPolitikasi, int zorlaCarpanDegeri)
    {
        _ = ustUsteAktif; _ = adminManuelMod; _ = adminVideoArdisikKazanc; _ = spinPolitikasi;
        if (bonusSpin || zorlaCarpanDegeri > 0 || tumbleAyarlari == null || grid == null || _ekonomiServisi == null)
            return null;

        int bahis = Mathf.Max(1, _ekonomiServisi.Bahis);
        _senaryo4SonZorunluNihaiOdeme = Senaryo1HedefOdemeMotoru.HedefNihaiOdemeSec(_minOdemeTL, _maxOdemeTL, _odemeDagilimiYuzde);

        if (!Senaryo4HedefOdemeMotoru.TryPaytableUyumluTekKumeSec(
                tumbleAyarlari, bahis, _minOdemeTL, _maxOdemeTL, _senaryo4SonZorunluNihaiOdeme,
                _scatterIndexCache, sutun, satir,
                out int kSym, out int kCnt, out int beklenenTl))
            return null;

        if (!Senaryo4HedefOdemeMotoru.TryTekKumeliIlkGridOlustur(sutun, satir, kSym, kCnt, _scatterIndexCache,
                tumbleAyarlari.PayTable_8_9.Length, out int[,] yeniGrid))
            return null;

        for (int x = 0; x < sutun; x++) for (int y = 0; y < satir; y++) grid[x, y] = yeniGrid[x, y];
        _tumbleServisi?.SetGrid(grid);
        zorlaSiradakiCarpan = zorlaCarpanDegeri;
        UI_CarpanSifirla();
        _izgaraServisi?.ResetScatterCountPerSpin();
        spinKazancHam = 0;

        var kayit = new SpinSimulasyonKaydi { Sutun = sutun, Satir = satir };
        SimulasyonKaydinaMevcutIlkGridStateKopyala(kayit);
        if (_carpanServisi != null && kayit.IlkCarpanDegerleri != null && kayit.IlkCarpanDegerleri.Count > 0)
            _carpanServisi.RecordPlacedCarpanlar(kayit.IlkCarpanDegerleri);

        int turSayaci = 0;
        int tumbleEsik = OyunKorumaServisi.TUMBLE_SABIT_ESIK;
        minClusterSize = tumbleEsik;
        _senaryo4KonstrukteMaxTumbleAdimi = 1;

        _senaryo4KonstrukteSimAktif = true;
        try
        {
            while (turSayaci < OyunKorumaServisi.MAX_TUMBLE_TUR)
            {
                var toRemove = _tumbleServisi != null ? _tumbleServisi.FindClustersToRemove(tumbleEsik) : new List<Vector2Int>();
                if (toRemove == null || toRemove.Count == 0) break;
                _carpanServisi?.ClearPendingDrops();
                _bombaPatlamaSonrasiIlkRefillCarpanEngeli = true;
                int turHam = tumbleAyarlari != null
                    ? tumbleAyarlari.CalculateWinWithOwnPayTable(toRemove, grid, satir, sutun, _ekonomiServisi.Bahis, tumbleEsik) : 0;
                spinKazancHam += turHam;
                var adim = new TumbleAdimKaydi { TurKazanci = turHam };
                adim.PatlayanHucreler.AddRange(toRemove);
                GridHucreleriniTemizle(toRemove);
                if (_cokmeAkisServisi != null) _cokmeAkisServisi.CokmeDoldurSadeceMantik(adim);
                kayit.Adimlar.Add(adim);
                turSayaci++;
                if (_senaryo4KonstrukteSimAktif && kayit.Adimlar.Count >= _senaryo4KonstrukteMaxTumbleAdimi) break;
            }
        }
        finally
        {
            _senaryo4KonstrukteSimAktif = false;
            _senaryo4KonstrukteMaxTumbleAdimi = 1;
        }

        KonstrukteRefillSonrasiKazancsizYap(kayit);
        int toplamCarpan = _carpanServisi != null ? _carpanServisi.GetTotalMultiplierForSpin() : 1;
        int nihaiOdeme = _carpanServisi != null ? _carpanServisi.MulClampInt(spinKazancHam, toplamCarpan) : spinKazancHam;
        kayit.ToplamHamKazanc = spinKazancHam;
        kayit.NihaiCarpanToplam = toplamCarpan;

        if (!SpinKaydiHamPaytableIleUyumluMu(kayit, bahis, tumbleEsik)) return null;
        if (!OdemeModelineUygunMu(nihaiOdeme, bahis, 0, maxReroll)) return null;
        if (limit != int.MaxValue && nihaiOdeme > limit) return null;

        kayit.SenaryoOdemeBandinaUygun = true;
        kayit.ZorlaCarpanKullanildi = false;
        Debug.Log($"[SENARYO4][KAZANÇ][KONSTRUKTE] sym={kSym}×{kCnt} hedef≈{beklenenTl} nihai={nihaiOdeme}");
        return kayit;
    }

    private SpinSimulasyonKaydi Senaryo4KayipKonstrukteHedefSpinDene(
        int limit, bool bonusSpin, bool adminManuelMod, bool adminVideoArdisikKazanc,
        int maxReroll, bool ustUsteAktif, ISenaryoSpinPolitikasi spinPolitikasi, int zorlaCarpanDegeri)
    {
        _ = ustUsteAktif; _ = adminManuelMod; _ = adminVideoArdisikKazanc; _ = maxReroll; _ = spinPolitikasi;
        if (bonusSpin || zorlaCarpanDegeri > 0 || tumbleAyarlari == null || grid == null || _ekonomiServisi == null)
            return null;

        int bahis = Mathf.Max(1, _ekonomiServisi.Bahis);

        if (!Senaryo4HedefOdemeMotoru.TryMinimalKayipGridOlustur(
                tumbleAyarlari, bahis, _scatterIndexCache, sutun, satir,
                out int kayipSembol, out int kayipCnt, out int[,] yeniGrid))
            return null;

        for (int x = 0; x < sutun; x++) for (int y = 0; y < satir; y++) grid[x, y] = yeniGrid[x, y];
        _tumbleServisi?.SetGrid(grid);
        zorlaSiradakiCarpan = zorlaCarpanDegeri;
        UI_CarpanSifirla();
        _izgaraServisi?.ResetScatterCountPerSpin();
        spinKazancHam = 0;

        var kayit = new SpinSimulasyonKaydi { Sutun = sutun, Satir = satir };
        SimulasyonKaydinaMevcutIlkGridStateKopyala(kayit);
        if (_carpanServisi != null && kayit.IlkCarpanDegerleri != null && kayit.IlkCarpanDegerleri.Count > 0)
            _carpanServisi.RecordPlacedCarpanlar(kayit.IlkCarpanDegerleri);

        int turSayaci = 0;
        int tumbleEsik = OyunKorumaServisi.TUMBLE_SABIT_ESIK;
        minClusterSize = tumbleEsik;
        _senaryo4KonstrukteMaxTumbleAdimi = 1;

        _senaryo4KonstrukteSimAktif = true;
        try
        {
            while (turSayaci < OyunKorumaServisi.MAX_TUMBLE_TUR)
            {
                var toRemove = _tumbleServisi != null ? _tumbleServisi.FindClustersToRemove(tumbleEsik) : new List<Vector2Int>();
                if (toRemove == null || toRemove.Count == 0) break;
                _carpanServisi?.ClearPendingDrops();
                _bombaPatlamaSonrasiIlkRefillCarpanEngeli = true;
                int turHam = tumbleAyarlari != null
                    ? tumbleAyarlari.CalculateWinWithOwnPayTable(toRemove, grid, satir, sutun, _ekonomiServisi.Bahis, tumbleEsik) : 0;
                spinKazancHam += turHam;
                var adim = new TumbleAdimKaydi { TurKazanci = turHam };
                adim.PatlayanHucreler.AddRange(toRemove);
                GridHucreleriniTemizle(toRemove);
                if (_cokmeAkisServisi != null) _cokmeAkisServisi.CokmeDoldurSadeceMantik(adim);
                kayit.Adimlar.Add(adim);
                turSayaci++;
                if (_senaryo4KonstrukteSimAktif && kayit.Adimlar.Count >= _senaryo4KonstrukteMaxTumbleAdimi) break;
            }
        }
        finally
        {
            _senaryo4KonstrukteSimAktif = false;
            _senaryo4KonstrukteMaxTumbleAdimi = 1;
        }

        KonstrukteRefillSonrasiKazancsizYap(kayit);
        int toplamCarpan = _carpanServisi != null ? _carpanServisi.GetTotalMultiplierForSpin() : 1;
        int nihaiOdeme = _carpanServisi != null ? _carpanServisi.MulClampInt(spinKazancHam, toplamCarpan) : spinKazancHam;
        kayit.ToplamHamKazanc = spinKazancHam;
        kayit.NihaiCarpanToplam = toplamCarpan;

        if (nihaiOdeme >= bahis) { Debug.LogWarning($"[S4_KAY] nihai={nihaiOdeme} >= bahis → null"); return null; }
        if (limit != int.MaxValue && nihaiOdeme > limit) return null;

        kayit.SenaryoOdemeBandinaUygun = true;
        kayit.ZorlaCarpanKullanildi = false;
        Debug.Log($"[SENARYO4][KAYIP][KONSTRUKTE] sym={kayipSembol}×{kayipCnt} nihai={nihaiOdeme}");
        return kayit;
    }

    private SpinSimulasyonKaydi Senaryo4BombKonstrukteHedefSpinDene(
        int limit, bool bonusSpin, bool adminManuelMod, bool adminVideoArdisikKazanc,
        int maxReroll, bool ustUsteAktif, ISenaryoSpinPolitikasi spinPolitikasi, int zorlaCarpanDegeri)
    {
        _ = ustUsteAktif; _ = adminManuelMod; _ = adminVideoArdisikKazanc; _ = maxReroll; _ = spinPolitikasi;
        if (bonusSpin || tumbleAyarlari == null || grid == null || _ekonomiServisi == null) return null;

        const int bombDegeri = 100;
        const int bombMinNihai = 5000;
        const int bombMaxNihai = 7000;
        int bahisB = Mathf.Max(1, _ekonomiServisi.Bahis);
        if (!Senaryo4HedefOdemeMotoru.TryBombNihaiHedefliGridOlustur(
                tumbleAyarlari, bahisB, bombDegeri, bombMinNihai, bombMaxNihai,
                _scatterIndexCache, sutun, satir,
                out int cheapSym, out int cheapCnt, out int[,] yeniGrid))
            return null;

        for (int x = 0; x < sutun; x++) for (int y = 0; y < satir; y++) grid[x, y] = yeniGrid[x, y];
        _tumbleServisi?.SetGrid(grid);

        // Önce sıfırla (ClearAllCarpanOverlays burada çalışır; henüz bomb yok)
        zorlaSiradakiCarpan = bombDegeri;
        UI_CarpanSifirla();
        _izgaraServisi?.ResetScatterCountPerSpin();
        spinKazancHam = 0;

        // Sonra bomb yerleştir (temizlikten sonra, silinmez)
        ForceCarpaniIlkGriddeGuvenliYerlestir(bombDegeri);
        zorlaSiradakiCarpan = 0;
        _tumbleServisi?.SetGrid(grid);

        var kayit = new SpinSimulasyonKaydi { Sutun = sutun, Satir = satir };
        SimulasyonKaydinaMevcutIlkGridStateKopyala(kayit);
        if (_carpanServisi != null && kayit.IlkCarpanDegerleri != null && kayit.IlkCarpanDegerleri.Count > 0)
            _carpanServisi.RecordPlacedCarpanlar(kayit.IlkCarpanDegerleri);

        int turSayaci = 0;
        int tumbleEsik = OyunKorumaServisi.TUMBLE_SABIT_ESIK;
        minClusterSize = tumbleEsik;
        _senaryo4KonstrukteMaxTumbleAdimi = 1;

        _senaryo4KonstrukteSimAktif = true;
        try
        {
            while (turSayaci < OyunKorumaServisi.MAX_TUMBLE_TUR)
            {
                var toRemove = _tumbleServisi != null ? _tumbleServisi.FindClustersToRemove(tumbleEsik) : new List<Vector2Int>();
                if (toRemove == null || toRemove.Count == 0) break;
                int turHam = tumbleAyarlari != null
                    ? tumbleAyarlari.CalculateWinWithOwnPayTable(toRemove, grid, satir, sutun, _ekonomiServisi.Bahis, tumbleEsik) : 0;
                spinKazancHam += turHam;
                var adim = new TumbleAdimKaydi { TurKazanci = turHam };
                adim.PatlayanHucreler.AddRange(toRemove);
                GridHucreleriniTemizle(toRemove);
                if (_cokmeAkisServisi != null) _cokmeAkisServisi.CokmeDoldurSadeceMantik(adim);
                kayit.Adimlar.Add(adim);
                turSayaci++;
                if (_senaryo4KonstrukteSimAktif && kayit.Adimlar.Count >= _senaryo4KonstrukteMaxTumbleAdimi) break;
            }
        }
        finally
        {
            _senaryo4KonstrukteSimAktif = false;
            _senaryo4KonstrukteMaxTumbleAdimi = 1;
        }

        KonstrukteRefillSonrasiKazancsizYap(kayit);
        int toplamCarpan = _carpanServisi != null ? _carpanServisi.GetTotalMultiplierForSpin() : 1;
        int nihaiOdeme = _carpanServisi != null ? _carpanServisi.MulClampInt(spinKazancHam, toplamCarpan) : spinKazancHam;
        kayit.ToplamHamKazanc = spinKazancHam;
        kayit.NihaiCarpanToplam = toplamCarpan;

        if (limit != int.MaxValue && nihaiOdeme > limit) return null;

        kayit.SenaryoOdemeBandinaUygun = true;
        kayit.ZorlaCarpanKullanildi = true;
        Debug.Log($"[SENARYO4][BOMB][KONSTRUKTE] cheapSym={cheapSym}×{cheapCnt} bomb={bombDegeri}x carpan={toplamCarpan} nihai={nihaiOdeme}");
        return kayit;
    }

    private SpinSimulasyonKaydi Senaryo5KazancKonstrukteHedefSpinDene(
        int limit, bool bonusSpin, bool adminManuelMod, bool adminVideoArdisikKazanc,
        int maxReroll, bool ustUsteAktif, ISenaryoSpinPolitikasi spinPolitikasi, int zorlaCarpanDegeri)
    {
        _ = ustUsteAktif; _ = adminManuelMod; _ = adminVideoArdisikKazanc; _ = spinPolitikasi;
        if (bonusSpin || zorlaCarpanDegeri > 0 || tumbleAyarlari == null || grid == null || _ekonomiServisi == null)
            return null;

        int bahis = Mathf.Max(1, _ekonomiServisi.Bahis);
        // S5 spin 1 kazanç bandı: 2x-2.5x bahis (400-500 TL @ 200 TL bahis)
        int s5WinMin = bahis * 2;
        int s5WinMax = bahis * 5 / 2;
        int s5WinHedef = UnityEngine.Random.Range(s5WinMin, s5WinMax + 1);
        _senaryo5SonZorunluNihaiOdeme = s5WinHedef;
        Debug.Log($"[S5_KAZ] WinBant={s5WinMin}..{s5WinMax} hedef={s5WinHedef}");

        if (!Senaryo5HedefOdemeMotoru.TryPaytableUyumluTekKumeSec(
                tumbleAyarlari, bahis, s5WinMin, s5WinMax, s5WinHedef,
                _scatterIndexCache, sutun, satir,
                out int kSym, out int kCnt, out int beklenenTl))
            return null;

        if (!Senaryo5HedefOdemeMotoru.TryTekKumeliIlkGridOlustur(sutun, satir, kSym, kCnt, _scatterIndexCache,
                tumbleAyarlari.PayTable_8_9.Length, out int[,] yeniGrid))
            return null;

        for (int x = 0; x < sutun; x++) for (int y = 0; y < satir; y++) grid[x, y] = yeniGrid[x, y];
        _tumbleServisi?.SetGrid(grid);
        zorlaSiradakiCarpan = zorlaCarpanDegeri;
        UI_CarpanSifirla();
        _izgaraServisi?.ResetScatterCountPerSpin();
        spinKazancHam = 0;

        var kayit = new SpinSimulasyonKaydi { Sutun = sutun, Satir = satir };
        SimulasyonKaydinaMevcutIlkGridStateKopyala(kayit);
        if (_carpanServisi != null && kayit.IlkCarpanDegerleri != null && kayit.IlkCarpanDegerleri.Count > 0)
            _carpanServisi.RecordPlacedCarpanlar(kayit.IlkCarpanDegerleri);

        int turSayaci = 0;
        int tumbleEsik = OyunKorumaServisi.TUMBLE_SABIT_ESIK;
        minClusterSize = tumbleEsik;
        _senaryo5KonstrukteMaxTumbleAdimi = 1;

        _senaryo5KonstrukteSimAktif = true;
        try
        {
            while (turSayaci < OyunKorumaServisi.MAX_TUMBLE_TUR)
            {
                var toRemove = _tumbleServisi != null ? _tumbleServisi.FindClustersToRemove(tumbleEsik) : new List<Vector2Int>();
                if (toRemove == null || toRemove.Count == 0) break;
                _carpanServisi?.ClearPendingDrops();
                _bombaPatlamaSonrasiIlkRefillCarpanEngeli = true;
                int turHam = tumbleAyarlari != null
                    ? tumbleAyarlari.CalculateWinWithOwnPayTable(toRemove, grid, satir, sutun, _ekonomiServisi.Bahis, tumbleEsik) : 0;
                spinKazancHam += turHam;
                var adim = new TumbleAdimKaydi { TurKazanci = turHam };
                adim.PatlayanHucreler.AddRange(toRemove);
                GridHucreleriniTemizle(toRemove);
                if (_cokmeAkisServisi != null) _cokmeAkisServisi.CokmeDoldurSadeceMantik(adim);
                kayit.Adimlar.Add(adim);
                turSayaci++;
                if (_senaryo5KonstrukteSimAktif && kayit.Adimlar.Count >= _senaryo5KonstrukteMaxTumbleAdimi) break;
            }
        }
        finally
        {
            _senaryo5KonstrukteSimAktif = false;
            _senaryo5KonstrukteMaxTumbleAdimi = 1;
        }

        KonstrukteRefillSonrasiKazancsizYap(kayit);
        int toplamCarpan = _carpanServisi != null ? _carpanServisi.GetTotalMultiplierForSpin() : 1;
        int nihaiOdeme = _carpanServisi != null ? _carpanServisi.MulClampInt(spinKazancHam, toplamCarpan) : spinKazancHam;
        kayit.ToplamHamKazanc = spinKazancHam;
        kayit.NihaiCarpanToplam = toplamCarpan;

        if (!SpinKaydiHamPaytableIleUyumluMu(kayit, bahis, tumbleEsik)) return null;
        if (!OdemeModelineUygunMu(nihaiOdeme, bahis, 0, maxReroll)) return null;
        if (limit != int.MaxValue && nihaiOdeme > limit) return null;

        kayit.SenaryoOdemeBandinaUygun = true;
        kayit.ZorlaCarpanKullanildi = false;
        Debug.Log($"[SENARYO5][KAZANÇ][KONSTRUKTE] sym={kSym}×{kCnt} hedef≈{beklenenTl} nihai={nihaiOdeme}");
        return kayit;
    }

    private SpinSimulasyonKaydi Senaryo5KayipKonstrukteHedefSpinDene(
        int limit, bool bonusSpin, bool adminManuelMod, bool adminVideoArdisikKazanc,
        int maxReroll, bool ustUsteAktif, ISenaryoSpinPolitikasi spinPolitikasi, int zorlaCarpanDegeri)
    {
        _ = ustUsteAktif; _ = adminManuelMod; _ = adminVideoArdisikKazanc; _ = maxReroll; _ = spinPolitikasi;
        if (bonusSpin || zorlaCarpanDegeri > 0 || tumbleAyarlari == null || grid == null || _ekonomiServisi == null)
            return null;

        int bahis = Mathf.Max(1, _ekonomiServisi.Bahis);
        // S5 spin 2 kayıp bandı: 0.25x-0.5x bahis (50-100 TL @ 200 TL bahis)
        int s5KayipMin = bahis / 4;
        int s5KayipMax = bahis / 2;

        if (!Senaryo5HedefOdemeMotoru.TryRangeliKayipGridOlustur(
                tumbleAyarlari, bahis, s5KayipMin, s5KayipMax, _scatterIndexCache, sutun, satir,
                out int kayipSembol, out int kayipCnt, out int[,] yeniGrid))
            return null;

        for (int x = 0; x < sutun; x++) for (int y = 0; y < satir; y++) grid[x, y] = yeniGrid[x, y];
        _tumbleServisi?.SetGrid(grid);
        zorlaSiradakiCarpan = zorlaCarpanDegeri;
        UI_CarpanSifirla();
        _izgaraServisi?.ResetScatterCountPerSpin();
        spinKazancHam = 0;

        var kayit = new SpinSimulasyonKaydi { Sutun = sutun, Satir = satir };
        SimulasyonKaydinaMevcutIlkGridStateKopyala(kayit);
        if (_carpanServisi != null && kayit.IlkCarpanDegerleri != null && kayit.IlkCarpanDegerleri.Count > 0)
            _carpanServisi.RecordPlacedCarpanlar(kayit.IlkCarpanDegerleri);

        int turSayaci = 0;
        int tumbleEsik = OyunKorumaServisi.TUMBLE_SABIT_ESIK;
        minClusterSize = tumbleEsik;
        _senaryo5KonstrukteMaxTumbleAdimi = 1;

        _senaryo5KonstrukteSimAktif = true;
        try
        {
            while (turSayaci < OyunKorumaServisi.MAX_TUMBLE_TUR)
            {
                var toRemove = _tumbleServisi != null ? _tumbleServisi.FindClustersToRemove(tumbleEsik) : new List<Vector2Int>();
                if (toRemove == null || toRemove.Count == 0) break;
                _carpanServisi?.ClearPendingDrops();
                _bombaPatlamaSonrasiIlkRefillCarpanEngeli = true;
                int turHam = tumbleAyarlari != null
                    ? tumbleAyarlari.CalculateWinWithOwnPayTable(toRemove, grid, satir, sutun, _ekonomiServisi.Bahis, tumbleEsik) : 0;
                spinKazancHam += turHam;
                var adim = new TumbleAdimKaydi { TurKazanci = turHam };
                adim.PatlayanHucreler.AddRange(toRemove);
                GridHucreleriniTemizle(toRemove);
                if (_cokmeAkisServisi != null) _cokmeAkisServisi.CokmeDoldurSadeceMantik(adim);
                kayit.Adimlar.Add(adim);
                turSayaci++;
                if (_senaryo5KonstrukteSimAktif && kayit.Adimlar.Count >= _senaryo5KonstrukteMaxTumbleAdimi) break;
            }
        }
        finally
        {
            _senaryo5KonstrukteSimAktif = false;
            _senaryo5KonstrukteMaxTumbleAdimi = 1;
        }

        KonstrukteRefillSonrasiKazancsizYap(kayit);
        int toplamCarpan = _carpanServisi != null ? _carpanServisi.GetTotalMultiplierForSpin() : 1;
        int nihaiOdeme = _carpanServisi != null ? _carpanServisi.MulClampInt(spinKazancHam, toplamCarpan) : spinKazancHam;
        kayit.ToplamHamKazanc = spinKazancHam;
        kayit.NihaiCarpanToplam = toplamCarpan;

        if (nihaiOdeme < s5KayipMin || nihaiOdeme > s5KayipMax)
        { Debug.LogWarning($"[S5_KAY] nihai={nihaiOdeme} bant dışı [{s5KayipMin}..{s5KayipMax}] → null"); return null; }
        if (limit != int.MaxValue && nihaiOdeme > limit) return null;

        kayit.SenaryoOdemeBandinaUygun = true;
        kayit.ZorlaCarpanKullanildi = false;
        Debug.Log($"[SENARYO5][KAYIP][KONSTRUKTE] sym={kayipSembol}×{kayipCnt} nihai={nihaiOdeme} bant=[{s5KayipMin}..{s5KayipMax}]");
        return kayit;
    }

    private SpinSimulasyonKaydi Senaryo5BombKonstrukteHedefSpinDene(
        int limit, bool bonusSpin, bool adminManuelMod, bool adminVideoArdisikKazanc,
        int maxReroll, bool ustUsteAktif, ISenaryoSpinPolitikasi spinPolitikasi, int zorlaCarpanDegeri)
    {
        _ = ustUsteAktif; _ = adminManuelMod; _ = adminVideoArdisikKazanc; _ = maxReroll; _ = spinPolitikasi;
        if (bonusSpin || tumbleAyarlari == null || grid == null || _ekonomiServisi == null) return null;

        const int bombDegeri = 500;
        if (!Senaryo5HedefOdemeMotoru.TryCheapestBombGridOlustur(
                tumbleAyarlari, _scatterIndexCache, sutun, satir,
                out int cheapSym, out int cheapCnt, out int[,] yeniGrid))
            return null;

        for (int x = 0; x < sutun; x++) for (int y = 0; y < satir; y++) grid[x, y] = yeniGrid[x, y];
        _tumbleServisi?.SetGrid(grid);

        // Önce sıfırla (ClearAllCarpanOverlays burada çalışır; henüz bomb yok)
        zorlaSiradakiCarpan = bombDegeri;
        UI_CarpanSifirla();
        _izgaraServisi?.ResetScatterCountPerSpin();
        spinKazancHam = 0;

        // Sonra bomb yerleştir (temizlikten sonra, silinmez)
        ForceCarpaniIlkGriddeGuvenliYerlestir(bombDegeri);
        zorlaSiradakiCarpan = 0;
        _tumbleServisi?.SetGrid(grid);

        var kayit = new SpinSimulasyonKaydi { Sutun = sutun, Satir = satir };
        SimulasyonKaydinaMevcutIlkGridStateKopyala(kayit);
        if (_carpanServisi != null && kayit.IlkCarpanDegerleri != null && kayit.IlkCarpanDegerleri.Count > 0)
            _carpanServisi.RecordPlacedCarpanlar(kayit.IlkCarpanDegerleri);

        int turSayaci = 0;
        int tumbleEsik = OyunKorumaServisi.TUMBLE_SABIT_ESIK;
        minClusterSize = tumbleEsik;
        _senaryo5KonstrukteMaxTumbleAdimi = 1;

        _senaryo5KonstrukteSimAktif = true;
        try
        {
            while (turSayaci < OyunKorumaServisi.MAX_TUMBLE_TUR)
            {
                var toRemove = _tumbleServisi != null ? _tumbleServisi.FindClustersToRemove(tumbleEsik) : new List<Vector2Int>();
                if (toRemove == null || toRemove.Count == 0) break;
                int turHam = tumbleAyarlari != null
                    ? tumbleAyarlari.CalculateWinWithOwnPayTable(toRemove, grid, satir, sutun, _ekonomiServisi.Bahis, tumbleEsik) : 0;
                spinKazancHam += turHam;
                var adim = new TumbleAdimKaydi { TurKazanci = turHam };
                adim.PatlayanHucreler.AddRange(toRemove);
                GridHucreleriniTemizle(toRemove);
                if (_cokmeAkisServisi != null) _cokmeAkisServisi.CokmeDoldurSadeceMantik(adim);
                kayit.Adimlar.Add(adim);
                turSayaci++;
                if (_senaryo5KonstrukteSimAktif && kayit.Adimlar.Count >= _senaryo5KonstrukteMaxTumbleAdimi) break;
            }
        }
        finally
        {
            _senaryo5KonstrukteSimAktif = false;
            _senaryo5KonstrukteMaxTumbleAdimi = 1;
        }

        KonstrukteRefillSonrasiKazancsizYap(kayit);
        int toplamCarpan = _carpanServisi != null ? _carpanServisi.GetTotalMultiplierForSpin() : 1;
        int nihaiOdeme = _carpanServisi != null ? _carpanServisi.MulClampInt(spinKazancHam, toplamCarpan) : spinKazancHam;
        kayit.ToplamHamKazanc = spinKazancHam;
        kayit.NihaiCarpanToplam = toplamCarpan;

        if (limit != int.MaxValue && nihaiOdeme > limit) return null;

        kayit.SenaryoOdemeBandinaUygun = true;
        kayit.ZorlaCarpanKullanildi = true;
        Debug.Log($"[SENARYO5][BOMB][KONSTRUKTE] cheapSym={cheapSym}×{cheapCnt} bomb={bombDegeri}x carpan={toplamCarpan} nihai={nihaiOdeme}");
        return kayit;
    }

    private bool Senaryo1FallbackKazanciniBandIcineZorla(SpinSimulasyonKaydi kayit, int bahis, out int nihaiOdeme)
    {
        nihaiOdeme = 0;
        if (kayit == null || kayit.Adimlar == null || kayit.Adimlar.Count == 0)
            return false;

        int minNihai = Mathf.Max(bahis + 1, _minOdemeTL);
        int maxNihai = Mathf.Max(minNihai, _maxOdemeTL);
        int carpan = Mathf.Max(1, kayit.NihaiCarpanToplam);

        // Paytable değeri değiştirilemez; mevcut ham gerçekten band içindeyse onayla, değilse reddet.
        int mevcutHam = kayit.ToplamHamKazanc;
        nihaiOdeme = _carpanServisi != null
            ? _carpanServisi.MulClampInt(mevcutHam, carpan)
            : (int)Mathf.Clamp((long)mevcutHam * carpan, int.MinValue, int.MaxValue);

        if (nihaiOdeme < minNihai || nihaiOdeme > maxNihai)
            return false;

        _senaryo1SonZorunluNihaiOdeme = nihaiOdeme;
        kayit.SenaryoOdemeBandinaUygun = true;
        return nihaiOdeme > bahis;
    }

    // ── Senaryo aktiflik sorgu metodları ──────────────────────────────────

    private bool IsAdminSenaryo1Aktif()
    {
        return _senaryoPresetAktif && _aktifAdminSenaryoIndex == 0;
    }

    private bool IsAdminSenaryo2Aktif()
    {
        return _senaryoPresetAktif && _aktifAdminSenaryoIndex == 1;
    }

    private bool IsAdminSenaryo3Aktif()
    {
        return _senaryoPresetAktif && _aktifAdminSenaryoIndex == 2;
    }

    private bool IsAdminSenaryo4Aktif()
    {
        return _senaryoPresetAktif && _aktifAdminSenaryoIndex == 3;
    }

    private bool IsAdminSenaryo5Aktif()
    {
        return _senaryoPresetAktif && _aktifAdminSenaryoIndex == 4;
    }

    private bool IsAdminSenaryo1Veya2Aktif()
    {
        return _senaryoPresetAktif && (_aktifAdminSenaryoIndex == 0 || _aktifAdminSenaryoIndex == 1);
    }

    private bool IsAdminSenaryo1Veya2Veya3Aktif()
    {
        return IsAdminSenaryo1Veya2Aktif() || IsAdminSenaryo3Aktif();
    }

    // ── Senaryo döngü spin tipi metodları ────────────────────────────────

    private SenaryoBombSpinTipi Senaryo4DonguSpinTipi()
    {
        // KY(0) → KY(0) → BOMB(2): spin 1-2 maksimum kayıp, spin 3 100x bomb
        int[] dongu = new int[] { 0, 0, 2 };
        int i = Mathf.Abs(_senaryo4DonguIndex) % 3;
        return (SenaryoBombSpinTipi)dongu[i];
    }

    private SenaryoBombSpinTipi Senaryo5DonguSpinTipi()
    {
        // K(1) → KY(0) → BOMB(2)
        int[] dongu = new int[] { 1, 0, 2 };
        int i = Mathf.Abs(_senaryo5DonguIndex) % 3;
        return (SenaryoBombSpinTipi)dongu[i];
    }

    private bool Senaryo2BeklenenKazancMi()
    {
        // KAZANÇ -> KAYIP -> KAZANÇ -> KAYIP -> KAZANÇ
        int[] dongu = new int[] { 1, 0, 1, 0, 1 };
        int i = Mathf.Abs(_senaryo2DonguIndex) % dongu.Length;
        return dongu[i] == 1;
    }

    private bool Senaryo3BeklenenKazancMi()
    {
        // KAYIP -> KAZANÇ -> KAYIP -> KAZANÇ -> KAYIP (3 kayıp, 2 kazanç)
        int[] dongu = new int[] { 0, 1, 0, 1, 0 };
        int i = Mathf.Abs(_senaryo3DonguIndex) % dongu.Length;
        return dongu[i] == 1;
    }
}
