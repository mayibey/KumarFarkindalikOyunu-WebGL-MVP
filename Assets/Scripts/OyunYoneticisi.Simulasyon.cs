using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public partial class OyunYoneticisi
{
    private void SimulasyonKaydinaMevcutIlkGridStateKopyala(SpinSimulasyonKaydi kayit)
    {
        if (kayit == null) return;
        kayit.IlkCarpanDegerleri.Clear();
        for (int x = 0; x < sutun; x++)
            for (int y = 0; y < satir; y++)
                if (grid[x, y] == CARPAN_SEMBOL && carpanDegerGrid[x, y] != 0)
                    kayit.IlkCarpanDegerleri.Add(carpanDegerGrid[x, y]);
        kayit.IlkGrid = (int[,])grid.Clone();
        kayit.IlkCarpanGrid = (int[,])carpanDegerGrid.Clone();
    }

    /// <summary>Zorunlu boş spin: doldur → çarpan → kazançsız grid → sıfır ödemeli kayıt.</summary>
    private SpinSimulasyonKaydi ZorunluBosSpinIcinSifirKayitUret(int fillLimit)
    {
        _izgaraServisi?.FillRandomAll(fillLimit);
        CarpanUretVeBirik();
        // Çarpanlar sadece refill'de; dolu gridde dönüşüm yok.
        GrideKazancsizYap();
        var kayit = new SpinSimulasyonKaydi { Sutun = sutun, Satir = satir };
        SimulasyonKaydinaMevcutIlkGridStateKopyala(kayit);
        kayit.ToplamHamKazanc = 0;
        kayit.NihaiCarpanToplam = 1;
        kayit.ZorlaCarpanKullanildi = false;
        kayit.SenaryoOdemeBandinaUygun = true;
        return kayit;
    }

    /// <summary>Senaryo 1 fallback: mevcut tumble kaydını, band içi nihai ödemeye zorlar.</summary>

    /// <summary>Senaryo 1: 50 spin sonrası scatter garantisi – gridde en az 4 hücreyi scatter yapar.</summary>
    private void GrideEnAzDortScatterKoy()
    {
        if (grid == null || sutun <= 0 || satir <= 0) return;
        int scatterIdx = _scatterIndexCache;
        var aday = new List<Vector2Int>();
        for (int x = 0; x < sutun; x++)
            for (int y = 0; y < satir; y++)
                if (grid[x, y] != CARPAN_SEMBOL)
                    aday.Add(new Vector2Int(x, y));
        int kac = Mathf.Min(4, aday.Count);
        for (int i = 0; i < kac; i++)
        {
            int r = UnityEngine.Random.Range(i, aday.Count);
            var t = aday[r]; aday[r] = aday[i]; aday[i] = t;
            grid[aday[i].x, aday[i].y] = scatterIdx;
        }
        _tumbleServisi?.SetGrid(grid);
    }

    /// <summary>Gridde minClusterSize ve üstü kümeleri kırar; zorunlu boş spin için kazançsız grid üretir.</summary>
    /// <summary>
    /// Konstrukte simülasyonu max-adım limiti yüzünden durduktan sonra refill grid'inde
    /// oluşabilecek kazanç kümelerini kırar; son adımın GridRefillSonrasi'nı da günceller.
    /// </summary>
    private void KonstrukteRefillSonrasiKazancsizYap(SpinSimulasyonKaydi kayit)
    {
        if (kayit?.Adimlar == null || kayit.Adimlar.Count == 0) return;
        _tumbleServisi?.SetGrid(grid);
        var kalanCluster = _tumbleServisi?.FindClustersToRemove(minClusterSize);
        if (kalanCluster == null || kalanCluster.Count == 0) return;

        Debug.Log($"[KONSTRUKTE-TEMIZLIK] Refill sonrası {kalanCluster.Count} kazanç hücresi bulundu, kırılıyor.");
        GrideKazancsizYap();

        var sonAdim = kayit.Adimlar[kayit.Adimlar.Count - 1];
        if (sonAdim.GridRefillSonrasi != null)
        {
            int xMax = Mathf.Min(sutun, sonAdim.GridRefillSonrasi.GetLength(0));
            int yMax = Mathf.Min(satir, sonAdim.GridRefillSonrasi.GetLength(1));
            for (int x = 0; x < xMax; x++)
                for (int y = 0; y < yMax; y++)
                    sonAdim.GridRefillSonrasi[x, y] = grid[x, y];
        }
    }

    private void GrideKazancsizYap()
    {
        if (grid == null || _tumbleServisi == null || tumbleAyarlari?.PayTable_8_9 == null) return;
        int n = tumbleAyarlari.PayTable_8_9.Length;
        int scatterIdx = _scatterIndexCache;
        for (int iter = 0; iter < 30; iter++)
        {
            _tumbleServisi.SetGrid(grid);
            var toRemove = _tumbleServisi.FindClustersToRemove(minClusterSize);
            if (toRemove == null || toRemove.Count == 0) break;
            var bySym = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<Vector2Int>>();
            for (int i = 0; i < toRemove.Count; i++)
            {
                int x = toRemove[i].x, y = toRemove[i].y;
                if (x < 0 || x >= sutun || y < 0 || y >= satir) continue;
                int s = grid[x, y];
                if (!bySym.ContainsKey(s)) bySym[s] = new System.Collections.Generic.List<Vector2Int>();
                bySym[s].Add(new Vector2Int(x, y));
            }
            bool degisti = false;
            foreach (var kv in bySym)
            {
                if (kv.Value.Count < minClusterSize) continue;
                int degisecek = kv.Value.Count - minClusterSize + 1;
                int baskaSembol = (kv.Key + 1) % n;
                if (baskaSembol == scatterIdx) baskaSembol = (baskaSembol + 1) % n;
                if (baskaSembol == CARPAN_SEMBOL) baskaSembol = (baskaSembol + 1) % n;
                for (int t = 0; t < degisecek && t < kv.Value.Count; t++)
                {
                    var p = kv.Value[t];
                    grid[p.x, p.y] = baskaSembol;
                    degisti = true;
                }
            }
            if (!degisti) break;
        }
        _tumbleServisi?.SetGrid(grid);

        // Görsel hile izlenimi önleme: bağlantısız dağınık 8+ sembol de kırılsın
        if (grid == null) return;
        int eşik = Mathf.Max(2, minClusterSize);
        var adet = new int[n];
        for (int x = 0; x < sutun; x++)
            for (int y = 0; y < satir; y++)
            { int v = grid[x, y]; if (v >= 0 && v < n) adet[v]++; }

        for (int sym = 0; sym < n; sym++)
        {
            if (sym == scatterIdx || sym == CARPAN_SEMBOL) continue;
            int fazla = adet[sym] - (eşik - 1);
            if (fazla <= 0) continue;
            int yedek = (sym + 1) % n;
            if (yedek == scatterIdx || yedek == CARPAN_SEMBOL) yedek = (yedek + 1) % n;
            for (int x = 0; x < sutun && fazla > 0; x++)
                for (int y = 0; y < satir && fazla > 0; y++)
                    if (grid[x, y] == sym && adet[yedek] < eşik - 1)
                    { grid[x, y] = yedek; adet[sym]--; adet[yedek]++; fazla--; }
        }
        _tumbleServisi?.SetGrid(grid);
    }

    /// <summary>Yakın Kaçırma görsel enjeksiyonu: 7 adet aynı sembol komşu hücrelere yerleştirir.
    /// Cluster eşiği 8 olduğu için ÖDEME ÜRETMEZ — sadece "az kalmıştı" görsel hissi.</summary>
    private void GrideNearMissEnjekteEt()
    {
        if (grid == null || sutun <= 0 || satir <= 0) return;
        const int NEAR_MISS_ADET = 7;
        int n = (tumbleAyarlari != null && tumbleAyarlari.PayTable_8_9 != null) ? tumbleAyarlari.PayTable_8_9.Length : 9;
        if (n <= 0) return;
        int scatterIdx = _scatterIndexCache;
        var aday = new List<Vector2Int>();
        for (int x = 0; x < sutun; x++)
            for (int y = 0; y < satir; y++)
                if (grid[x, y] != CARPAN_SEMBOL && grid[x, y] >= 0)
                    aday.Add(new Vector2Int(x, y));
        if (aday.Count < NEAR_MISS_ADET) return;
        // Düşük ödemeli sembol seç (index 0-3 arası)
        int sembol = UnityEngine.Random.Range(0, Mathf.Min(4, n));
        if (sembol == scatterIdx) sembol = (sembol + 1) % n;
        // Karıştır (Fisher-Yates) ki her spinde farklı 7 hücre
        for (int i = aday.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            var t = aday[i]; aday[i] = aday[j]; aday[j] = t;
        }
        for (int i = 0; i < NEAR_MISS_ADET && i < aday.Count; i++)
        {
            var p = aday[i];
            grid[p.x, p.y] = sembol;
        }
        _tumbleServisi?.SetGrid(grid);
        OturumKayitcisi.EkleEvent("yakin_kacirma", $"7 adet sembol {sembol} yerleştirildi (cluster oluşmaz)", _spinNo);
        Debug.Log($"[YAKIN_KACIRMA] 7 adet sembol {sembol} grid'e yerleştirildi (eşik 8, ödeme yok).");
    }

    /// <summary>Zorla çarpan + toggle açıkken tumble garantisi: gridde en az 8 aynı sembol (bir cluster) oluşturur.</summary>
    private void GrideZorlaEnAzBirCluster()
    {
        if (grid == null || sutun <= 0 || satir <= 0) return;
        int n = (tumbleAyarlari != null && tumbleAyarlari.PayTable_8_9 != null) ? tumbleAyarlari.PayTable_8_9.Length : 9;
        if (n <= 0) return;
        int scatterIdx = _scatterIndexCache;
        var adayHucreler = new List<Vector2Int>();
        for (int x = 0; x < sutun; x++)
            for (int y = 0; y < satir; y++)
                if (grid[x, y] != CARPAN_SEMBOL && grid[x, y] >= 0)
                    adayHucreler.Add(new Vector2Int(x, y));
        if (adayHucreler.Count < minClusterSize) return;
        int sembol = UnityEngine.Random.Range(0, n);
        if (sembol == scatterIdx) sembol = (sembol + 1) % n;
        for (int i = 0; i < minClusterSize && i < adayHucreler.Count; i++)
        {
            var p = adayHucreler[i];
            grid[p.x, p.y] = sembol;
        }
        _tumbleServisi?.SetGrid(grid);
    }

    private void GridHucreleriniTemizle(List<Vector2Int> toRemove)
    {
        if (toRemove == null || grid == null || carpanDegerGrid == null) return;
        for (int i = 0; i < toRemove.Count; i++)
        {
            int x = toRemove[i].x, y = toRemove[i].y;
            grid[x, y] = -1;
            carpanDegerGrid[x, y] = 0;
            int ridx = _izgaraServisi != null ? _izgaraServisi.XYToIndex(x, y) : -1;
            if (carpanDegerByCellIndex != null && ridx >= 0 && ridx < carpanDegerByCellIndex.Length)
                carpanDegerByCellIndex[ridx] = 0;
        }
        _tumbleServisi?.SetGrid(grid);
    }

    /// <summary>Meyvelerin drop-in animasyonu öncesi başlangıç konumuna (yukarıda, şeffaf) alır; zorla çarpan dahil tüm spinlerde animasyon görünsün diye.</summary>
    private void HucreleriDropInBaslangicKonumunaAl()
    {
        if (hucreler == null || hucreler.Length == 0) return;
        float offset = dropStartYOffset;
        for (int i = 0; i < hucreler.Length; i++)
        {
            var img = hucreler[i];
            if (img == null) continue;
            Vector2 hedef = (cellPos != null && i < cellPos.Length) ? cellPos[i] : img.rectTransform.anchoredPosition;
            img.rectTransform.anchoredPosition = hedef + Vector2.up * offset;
            Color c = img.color;
            c.a = 0f;
            img.color = c;
        }
    }

    /// <summary>Tumble adımı yokken spin sonrası tam grid düşüşü yerine hücreleri doğrudan ızgara hedefine kilitler.</summary>
    private void HucreleriIzgaraHedefineKilitle()
    {
        if (hucreler == null || hucreler.Length == 0) return;
        for (int i = 0; i < hucreler.Length; i++)
        {
            var img = hucreler[i];
            if (img == null) continue;
            Vector2 hedef = (cellPos != null && i < cellPos.Length) ? cellPos[i] : img.rectTransform.anchoredPosition;
            img.rectTransform.anchoredPosition = hedef;
            Color c = img.color;
            c.a = 1f;
            img.color = c;
        }
    }

    /// <summary>TumbleAyarlari tek kaynak: düşüş / patlama süreleri ve üst spawn ofseti oyun başında buradan okunur.</summary>
    private void TumbleAyarlardanAnimasyonHizlariniUygula()
    {
        if (tumbleAyarlari == null) return;
        const float minNormalFall = 0.62f;
        const float minBonusFall = 0.58f;
        popDuration = Mathf.Max(0.08f, tumbleAyarlari.PopDuration);
        fallDuration = Mathf.Max(minNormalFall, tumbleAyarlari.FallDuration);
        betweenStepsDelay = Mathf.Max(0f, tumbleAyarlari.BetweenStepsDelay);
        spawnFromTopOffset = tumbleAyarlari.SpawnFromTopOffset;
        bonusYavasMod = tumbleAyarlari.BonusYavasMod;
        bonusPopDuration = Mathf.Max(0.08f, tumbleAyarlari.BonusPopDuration);
        bonusFallDuration = Mathf.Max(minBonusFall, tumbleAyarlari.BonusFallDuration);
        bonusBetweenStepsDelay = Mathf.Max(0f, tumbleAyarlari.BonusBetweenStepsDelay);
        bonusSpinBeklemeOverride = Mathf.Max(0f, tumbleAyarlari.BonusSpinBeklemeOverride);
        dropDuration = Mathf.Max(dropDuration, fallDuration * 0.90f);
    }

    private void OdemeLogYaz(SpinSimulasyonKaydi kayit)
    {
        if (kayit == null || kayit.Adimlar == null || kayit.Adimlar.Count == 0) return;

        int bahis = _ekonomiServisi != null ? _ekonomiServisi.Bahis : 0;
        int tumbleEsik = OyunKorumaServisi.TUMBLE_SABIT_ESIK;
        int satir = kayit.Satir > 0 ? kayit.Satir : this.satir;
        int sutun = kayit.Sutun > 0 ? kayit.Sutun : this.sutun;

        var sb = new System.Text.StringBuilder($"[ODEME_LOG] Bahis={bahis}TL");
        int runningCarpan = 0;
        int toplamBeklenen = 0;

        for (int i = 0; i < kayit.Adimlar.Count; i++)
        {
            var adim = kayit.Adimlar[i];
            int[,] oncekiGrid = (i == 0) ? kayit.IlkGrid : kayit.Adimlar[i - 1].GridRefillSonrasi;

            if (adim.CarpanDegerleriBuTur != null)
                foreach (int c in adim.CarpanDegerleriBuTur) runningCarpan += c;
            int gosterimCarpan = runningCarpan > 0 ? runningCarpan : 1;

            // Paytable'dan beklenen değer (simülasyonla aynı fonksiyon)
            int turBeklenen = (tumbleAyarlari != null && oncekiGrid != null && adim.PatlayanHucreler != null)
                ? tumbleAyarlari.CalculateWinWithOwnPayTable(adim.PatlayanHucreler, oncekiGrid, satir, sutun, bahis, tumbleEsik)
                : 0;
            toplamBeklenen += turBeklenen;
            int turFark = adim.TurKazanci - turBeklenen;

            // Per-sembol detay
            var sembolAdet = new System.Collections.Generic.Dictionary<int, int>();
            if (oncekiGrid != null && adim.PatlayanHucreler != null)
            {
                foreach (var hucre in adim.PatlayanHucreler)
                {
                    if (hucre.x < 0 || hucre.y < 0) continue;
                    int sym = oncekiGrid[hucre.x, hucre.y];
                    if (sym < 0) continue;
                    if (!sembolAdet.ContainsKey(sym)) sembolAdet[sym] = 0;
                    sembolAdet[sym]++;
                }
            }

            var parts = new System.Text.StringBuilder();
            bool ilk = true;
            foreach (var kv in sembolAdet)
            {
                if (!ilk) parts.Append("+");
                ilk = false;
                int sym = kv.Key;
                int adet = kv.Value;
                string ad = (sembolSpriteListesi != null && sym >= 0 && sym < sembolSpriteListesi.Count && sembolSpriteListesi[sym] != null)
                    ? sembolSpriteListesi[sym].name
                    : $"Sym{sym}";
                float ptKatsayi = tumbleAyarlari != null ? tumbleAyarlari.GetPayForCount(sym, adet) : 0f;
                int symBek = UnityEngine.Mathf.RoundToInt(ptKatsayi * bahis);
                parts.Append($"{ad}×{adet}(PT={ptKatsayi:F2}x={symBek}TL)");
            }
            string sembolStr = sembolAdet.Count > 0 ? parts.ToString() : "?";

            string farkStr = turFark == 0 ? "OK" : $"FARK={turFark:+#;-#;0}";
            sb.Append($" | Tur{i + 1}: {sembolStr} Bek={turBeklenen}TL Gercek={adim.TurKazanci}TL {farkStr} Carpan={gosterimCarpan}");
        }

        int nihaiCarpan = kayit.NihaiCarpanToplam > 0 ? kayit.NihaiCarpanToplam : 1;
        long nihaiOdeme = (long)kayit.ToplamHamKazanc * nihaiCarpan;
        if (nihaiOdeme > int.MaxValue) nihaiOdeme = int.MaxValue;
        int hamFark = kayit.ToplamHamKazanc - toplamBeklenen;
        string hamFarkStr = hamFark == 0 ? "OK" : $"FARK={hamFark:+#;-#;0}";
        sb.Append($" || ToplamHam: Bek={toplamBeklenen}TL Gercek={kayit.ToplamHamKazanc}TL {hamFarkStr} Carpan={nihaiCarpan} Nihai={(int)nihaiOdeme}TL");

        Debug.Log(sb.ToString());
    }

    /// <summary>Kayıttaki spin sonucunu ekranda oynatır. Normal ve otomatik spin aynı bu yolu kullanır; meyve düşüşü (AnimateGridDropIn + tumble) ikisinde de aynıdır.</summary>
    private IEnumerator SimulasyonKaydiniOynatImpl(SpinSimulasyonKaydi kayit)
    {
        if (kayit == null) yield break;

        OdemeLogYaz(kayit);

        UI_CarpanSifirla();
        _carpanKutuUcusFormulKilit = false;
        _carpanKutuUcusBirikimSonDeger = 0;
        _carpanKutuUcusBirikimGosterMax = 0;
        spinKazancHam = 0;
        sonSpinKazancHamGoster = 0;
        sonSpinCarpanGoster = 1;
        sonSpinKazancToplamGoster = 0;
        sonSpinKazanci = 0;

        // Yeni spin başlamadan önce eski meyveleri aşağı akıtarak çıkar.
        if (_animasyonServisi != null)
            yield return _animasyonServisi.AnimateGridOutDown();

        for (int x = 0; x < kayit.Sutun && x < sutun; x++)
            for (int y = 0; y < kayit.Satir && y < satir; y++)
            {
                grid[x, y] = kayit.IlkGrid[x, y];
                carpanDegerGrid[x, y] = kayit.IlkCarpanGrid[x, y];
            }
        ApplyNewGridAndSync(grid, carpanDegerGrid);
        if (kayit.IlkCarpanDegerleri != null && kayit.IlkCarpanDegerleri.Count > 0 && _carpanServisi != null)
            _carpanServisi.RecordPlacedCarpanlar(kayit.IlkCarpanDegerleri);

        // Force carpan: kilidi spawn anında (RenderAllSprites öncesi) başlat — sayma animasyonunu sıfır kare bırakmadan kes.
        Coroutine _kilitCoroSpin = null;
        if (kayit.ZorlaCarpanKullanildi)
            _kilitCoroSpin = StartCoroutine(BombaHucresiTextKilitle(kayit));

        _izgaraServisi?.RenderAllSprites(true, true);
        _uiServisi?.UI_Guncelle();

        _izgaraServisi?.CacheCellPositionsThenDisableLayout();
        var guncelPos = _izgaraServisi?.GetCellPos();
        if (guncelPos != null)
        {
            cellPos = guncelPos;
            _animasyonServisi?.SetCellPos(guncelPos);
        }
        HucreleriDropInBaslangicKonumunaAl();
        if (kayit.ZorlaCarpanKullanildi)
            _izgaraServisi?.ForceRefreshCarpanTextsFromGrid();
        ZorlaCarpanIlkDususEfektiniBaslat(kayit);
        if (_animasyonServisi != null)
            yield return _animasyonServisi.AnimateGridDropIn();

        // SCRIPTED MOD — DropIn animasyonu sırasında sprite/alpha tutarsız kalan hücreler için defansif rerender:
        // grid kayıttan zaten yazılı; tüm 30 hücrenin sprite'ı + alpha 1 + scale 1 garanti edilir.
        if (Senaryo.Scripted.ScriptedSpinYoneticisi.Aktif)
        {
            _izgaraServisi?.RenderAllSprites(setAlphaOne: true, resetScale: true);
        }

        if (_kilitCoroSpin != null)
        {
            StopCoroutine(_kilitCoroSpin);
            _kilitCoroSpin = null;
        }
        _izgaraServisi?.ForceRefreshCarpanTextsFromGrid();
        _uiServisi?.UI_Guncelle();

        if (kayit.ZorlaCarpanKullanildi && _bombaInisEfektServisi != null)
        {
            int inisCarpi = ZorlaCarpanDegeriniBul(kayit);
            _bombaInisEfektServisi.EfektBaslat(
                this, inisCarpi,
                bombaInisThunderClip, bombaInisBassClip,
                bombaInisThunderSesSeviyesi, bombaInisBassSesSeviyesi,
                slotGridRoot as RectTransform,
                bombaInisEsikDegeri);
        }

        var spinSonuCarpanHucreIdx = new List<int>();
        var spinSonuCarpanDegerleri = new List<int>();

        // Scripted/RNG modlar için ilk grid çarpan toplama (özellikle scripted modda kritik):
        // ScriptedSpinUygulayici çarpanı kayit.IlkCarpanGrid'e koyuyor; mevcut tumble loop sadece YeniSpawn'ı tarıyor.
        // Bu blok ilk grid çarpan koordinatlarını da kazanç-kutusu uçuş listesine ekler.
        // Atlama koşulları:
        //   - kayit.CarpanKacti (A5 Spin 3 senaryosu): "kaçtı" hissi korunsun, uçuş yok
        //   - kayit.ZorlaCarpanKullanildi (A4 Spin 5 x100, A5 x500): AnimatePopBombali zaten oynayacak, çift animasyon olmasın
        bool atlaIlkGridUcus = kayit.CarpanKacti || kayit.ZorlaCarpanKullanildi;
        if (!atlaIlkGridUcus && kayit.IlkCarpanGrid != null && _izgaraServisi != null)
        {
            int hucreSayisiIlk = hucreler != null ? hucreler.Length : 0;
            for (int xx = 0; xx < kayit.Sutun && xx < sutun; xx++)
            {
                for (int yy = 0; yy < kayit.Satir && yy < satir; yy++)
                {
                    int cv = kayit.IlkCarpanGrid[xx, yy];
                    if (cv <= 0) continue;
                    int ix = _izgaraServisi.XYToIndex(xx, yy);
                    if (ix >= 0 && ix < hucreSayisiIlk)
                    {
                        spinSonuCarpanHucreIdx.Add(ix);
                        spinSonuCarpanDegerleri.Add(cv);
                    }
                }
            }
        }

        for (int a = 0; a < kayit.Adimlar.Count; a++)
        {
            var adim = kayit.Adimlar[a];
            spinKazancHam += adim.TurKazanci;
            // Tumble başladığı anda kazanç metni güncellensin.
            // Spin başı sıçramasını ayrı yerde engellediğimiz için burada güvenle anlık akış verebiliriz.
            sonSpinKazancHamGoster = spinKazancHam;
            // Bomba (force) patlayana kadar çarpan uygulanmasın: sadece ham kazanç (OturumKazancText + KazancText).
            sonSpinCarpanGoster = 1;
            sonSpinKazancToplamGoster = spinKazancHam;
            sonSpinKazanci = 0;
            _uiServisi?.UI_Guncelle();

            List<Vector2Int> meyvePopHucreleri = BombaHucreleriniAyikla(adim.PatlayanHucreler);
            List<Vector2Int> temizlenecekHucreler = meyvePopHucreleri;

            // [DBG_POPUP] Pop öncesi: grid verisi == IlkGrid mi, hücre sprite'ı grid sembolüyle uyuşuyor mu?
            if (meyvePopHucreleri != null && kayit.IlkGrid != null)
            {
                int[,] oncekiGridRef = (a == 0) ? kayit.IlkGrid : (kayit.Adimlar[a - 1].GridRefillSonrasi ?? kayit.IlkGrid);
                var dbgSb = new System.Text.StringBuilder($"[DBG_POPUP] Tur{a + 1} pop oncesi ({meyvePopHucreleri.Count} hucre):");
                bool herhangiUyumsuz = false;
                foreach (var p in meyvePopHucreleri)
                {
                    if (p.x < 0 || p.y < 0 || p.x >= sutun || p.y >= satir) continue;
                    int gridSym = grid[p.x, p.y];
                    int ilkSym = oncekiGridRef[p.x, p.y];
                    int hIdx = _izgaraServisi != null ? _izgaraServisi.XYToIndex(p.x, p.y) : -1;
                    string spriteName = (hIdx >= 0 && hucreler != null && hIdx < hucreler.Length && hucreler[hIdx] != null && hucreler[hIdx].sprite != null)
                        ? hucreler[hIdx].sprite.name : "null";
                    string beklenenSprite = (sembolSpriteListesi != null && gridSym >= 0 && gridSym < sembolSpriteListesi.Count && sembolSpriteListesi[gridSym] != null)
                        ? sembolSpriteListesi[gridSym].name : $"Sym{gridSym}";
                    bool dataOk = gridSym == ilkSym;
                    bool spriteOk = spriteName == beklenenSprite;
                    if (!dataOk || !spriteOk)
                    {
                        herhangiUyumsuz = true;
                        dbgSb.Append($" | [{p.x},{p.y}] grid={gridSym} ilk={ilkSym} sprite={spriteName} beklenen={beklenenSprite}{(!dataOk ? " DATA!" : "")}{(!spriteOk ? " SPRITE!" : "")}");
                    }
                }
                if (!herhangiUyumsuz)
                    dbgSb.Append(" TUMU_OK");
                Debug.Log(dbgSb.ToString());
            }

            if (meyvePopHucreleri != null && meyvePopHucreleri.Count > 0)
            {
                _hizVeSesServisi?.PlayTumbleSfx(tumblePopClip, ref _lastTumblePopTime, tumblePopMinInterval, 1f, tumblePopBaslangicOffsetSaniye);
                // Bu adımda sadece meyve pop'u oynatılır; final bomba animasyonu spin sonunda ayrıca tetiklenir.
                if (_tumbleServisi != null)
                {
                    var popCoro = _tumbleServisi.AnimatePop(meyvePopHucreleri);
                    if (popCoro != null) yield return popCoro;
                }
            }
            yield return new WaitForSeconds(0.30f);
            GridHucreleriniTemizle(temizlenecekHucreler);
            if (_cokmeAkisServisi != null)
                yield return _cokmeAkisServisi.CokmeDoldurOynat(adim);
            if (adim.YeniSpawnEdilenHucreler != null && adim.CarpanGridRefillSonrasi != null && _izgaraServisi != null)
            {
                for (int k = 0; k < adim.YeniSpawnEdilenHucreler.Count; k++)
                {
                    var p = adim.YeniSpawnEdilenHucreler[k];
                    int cv = adim.CarpanGridRefillSonrasi[p.x, p.y];
                    if (cv <= 0) continue;
                    int ix = _izgaraServisi.XYToIndex(p.x, p.y);
                    int hucreSayisi = hucreler != null ? hucreler.Length : 0;
                    if (ix >= 0 && ix < hucreSayisi)
                    {
                        spinSonuCarpanHucreIdx.Add(ix);
                        spinSonuCarpanDegerleri.Add(cv);
                    }
                }
            }
            if (adim.CarpanDegerleriBuTur != null && adim.CarpanDegerleriBuTur.Count > 0 && _carpanServisi != null)
            {
                _carpanServisi.RecordPlacedCarpanlar(adim.CarpanDegerleriBuTur);
                if (bonusAktif && SenaryoYoneticisi.I != null)
                {
                    int toplamCarpan = _carpanServisi.GetTotalMultiplierForSpin();
                    SenaryoYoneticisi.I.LogEkle(SenaryoOlayKaydi.OlayTipi_BonusCarpanArtti, $"Bonus oyununda çarpan arttı. Toplam çarpan: {toplamCarpan}x. Spin: {SenaryoYoneticisi.I.toplamSpin}.");
                }
            }
            yield return new WaitForSeconds(0.15f);
            yield return new WaitForSeconds(betweenStepsDelay);

            // SCRIPTED MOD — defansif tumble sonu render: AnimatePop scale'i 0.35'e düşürüyor, sonraki render
            // (RenderSpritesOnlyForCells / fade-in) scale'a dokunmuyor. resetScale=true ile her hücre scale 1 +
            // alpha 1 + sprite mantık grid'inden alınır. Mevcut RNG akışını etkilemez (Aktif=false).
            if (Senaryo.Scripted.ScriptedSpinYoneticisi.Aktif)
            {
                _izgaraServisi?.RenderAllSprites(setAlphaOne: true, resetScale: true);
            }
        }

        // Zorla çarpan kullanıldıysa final bomba animasyonunu oynat (overlay hâlâ grid üzerindeyken, meyveler kaybolmaz).
        if (kayit != null && kayit.ZorlaCarpanKullanildi && _animasyonServisi != null && _carpanServisi != null)
        {
            int finalCarpan = (int)_carpanServisi.GetCurrentMultiplier();
            if (finalCarpan > 1 && spinKazancHam > 0)
            {
                Vector2Int bombaHucre = new Vector2Int(sutun / 2, satir / 2);
                bool bulundu = false;
                if (kayit.IlkCarpanGrid != null)
                    for (int bx = 0; bx < Mathf.Min(kayit.Sutun, sutun) && !bulundu; bx++)
                        for (int by = 0; by < Mathf.Min(kayit.Satir, satir) && !bulundu; by++)
                            if (kayit.IlkCarpanGrid[bx, by] > 0) { bombaHucre = new Vector2Int(bx, by); bulundu = true; }
                yield return _animasyonServisi.AnimatePopBombali(
                    new List<Vector2Int>(), bombaHucre,
                    spinKazancHam, finalCarpan, _carpanServisi.MulClampInt(spinKazancHam, finalCarpan));
            }
        }

        // İstek: çarpanlar tumble zinciri tamamen bittikten sonra (spin sonunda) kutuya gitsin.
        if (spinSonuCarpanHucreIdx.Count > 0 && spinSonuCarpanDegerleri.Count > 0)
            yield return ((ICokmeAkisBaglami)this).CarpanKazancUcusunuOynat(spinSonuCarpanHucreIdx, spinSonuCarpanDegerleri);

        tumbleToplamKazanc = spinKazancHam;
    }

    private List<Vector2Int> FloodFillCluster(int sx, int sy, int sym, bool[,] visited)
    {
        List<Vector2Int> outList = new List<Vector2Int>();
        Queue<Vector2Int> q = new Queue<Vector2Int>();
        q.Enqueue(new Vector2Int(sx, sy));
        visited[sx, sy] = true;

        while (q.Count > 0)
        {
            var p = q.Dequeue();
            outList.Add(p);

            TryEnqueue(p.x + 1, p.y);
            TryEnqueue(p.x - 1, p.y);
            TryEnqueue(p.x, p.y + 1);
            TryEnqueue(p.x, p.y - 1);
        }

        void TryEnqueue(int nx, int ny)
        {
            if (nx < 0 || nx >= sutun || ny < 0 || ny >= satir) return;
            if (visited[nx, ny]) return;
            if (grid[nx, ny] != sym) return;
            visited[nx, ny] = true;
            q.Enqueue(new Vector2Int(nx, ny));
        }

        return outList;
    }

    /// <summary>
    /// İstek: zorla X kullanılan spinlerde, ilk düşüş devam ederken kısa ekran sarsıntısı ve ses oynat.
    /// Efekt ayrı serviste tutulur; beğenilmezse toggle kapatılarak diğer akışlara dokunmadan devre dışı kalır.
    /// </summary>

    /// <summary>
    /// Çarpan aktifse final bomba animasyonuna izin verir.
    /// Önceki stabil davranışla uyumlu olarak meyve kümesi patlamasa bile (çarpan düştüyse) final bomba oynatılır.
    /// </summary>
    private bool FinalBombaAnimasyonuBuSpinIcinGecerli()
    {
        // Bu spinte çarpan görünmüşse final bomba efekti toggle'dan bağımsız oynatılmalı.
        // Aksi durumda kullanıcı "çarpan düştü ama bomba patlamadı" hissi yaşıyor.
        if (carpanSadeceBonus && !bonusAktif)
            return false;
        return true;
    }

    private bool TryIlkBombaHucreBul(out Vector2Int hucre)
    {
        hucre = new Vector2Int(-1, -1);
        if (grid == null) return false;
        for (int y = 0; y < satir; y++)
        {
            for (int x = 0; x < sutun; x++)
            {
                if (grid[x, y] == CARPAN_SEMBOL)
                {
                    hucre = new Vector2Int(x, y);
                    return true;
                }
            }
        }
        return false;
    }

    private bool TryBombaHucreBulOncelikli(SpinSimulasyonKaydi kayit, out Vector2Int hucre)
    {
        if (TryIlkBombaHucreBul(out hucre))
            return true;

        hucre = new Vector2Int(-1, -1);
        if (kayit == null || kayit.IlkGrid == null)
            return false;

        int maxX = Mathf.Min(sutun, kayit.Sutun);
        int maxY = Mathf.Min(satir, kayit.Satir);
        for (int y = 0; y < maxY; y++)
        {
            for (int x = 0; x < maxX; x++)
            {
                if (kayit.IlkGrid[x, y] == CARPAN_SEMBOL)
                {
                    hucre = new Vector2Int(x, y);
                    return true;
                }
            }
        }

        return false;
    }

    private List<Vector2Int> TumHucreleriPatlatmaListesiOlustur()
    {
        var liste = new List<Vector2Int>(sutun * satir);
        if (grid == null) return liste;
        for (int y = 0; y < satir; y++)
        {
            for (int x = 0; x < sutun; x++)
            {
                if (grid[x, y] >= 0 || grid[x, y] == CARPAN_SEMBOL)
                    liste.Add(new Vector2Int(x, y));
            }
        }
        return liste;
    }

    private IEnumerator BombaHucresiTextKilitle(SpinSimulasyonKaydi kayit)
    {
        if (kayit?.IlkCarpanGrid == null || carpanHücreTextleri == null) yield break;
        int bombIdx = -1;
        int carpanDeger = 0;
        for (int bx = 0; bx < sutun && bombIdx < 0; bx++)
            for (int by = 0; by < satir && bombIdx < 0; by++)
                if (kayit.IlkCarpanGrid[bx, by] > 0)
                {
                    bombIdx = _izgaraServisi != null ? _izgaraServisi.XYToIndex(bx, by) : by * sutun + bx;
                    carpanDeger = kayit.IlkCarpanGrid[bx, by];
                }
        if (bombIdx < 0 || bombIdx >= carpanHücreTextleri.Length) yield break;
        var tmp = carpanHücreTextleri[bombIdx];
        if (tmp == null) yield break;
        string hedefText = "x" + carpanDeger.ToString();
        while (true)
        {
            if (tmp.text != hedefText) tmp.text = hedefText;
            if (!tmp.gameObject.activeSelf) tmp.gameObject.SetActive(true);
            var tc = tmp.color;
            if (tc.a < 1f) { tc.a = 1f; tmp.color = tc; }
            yield return null;
        }
    }

    private List<Vector2Int> BombaHucreleriniAyikla(List<Vector2Int> hucreler)
    {
        if (hucreler == null || hucreler.Count == 0)
            return hucreler;

        var sonuc = new List<Vector2Int>(hucreler.Count);
        for (int i = 0; i < hucreler.Count; i++)
        {
            var p = hucreler[i];
            if (p.x < 0 || p.x >= sutun || p.y < 0 || p.y >= satir)
                continue;

            if (grid != null && grid[p.x, p.y] == CARPAN_SEMBOL)
                continue;

            sonuc.Add(p);
        }
        return sonuc;
    }

    private List<Vector2Int> PatlayanHucrelereBombayiEkle(List<Vector2Int> patlayanlar, Vector2Int bombaHucre)
    {
        var sonuc = new List<Vector2Int>();
        var seen = new HashSet<Vector2Int>();

        if (patlayanlar != null)
        {
            for (int i = 0; i < patlayanlar.Count; i++)
            {
                var p = patlayanlar[i];
                if (p.x < 0 || p.x >= sutun || p.y < 0 || p.y >= satir) continue;
                if (seen.Add(p)) sonuc.Add(p);
            }
        }

        if (bombaHucre.x >= 0 && bombaHucre.x < sutun && bombaHucre.y >= 0 && bombaHucre.y < satir)
        {
            if (seen.Add(bombaHucre))
                sonuc.Add(bombaHucre);
        }

        return sonuc;
    }

    // GRID FILL / RENDER
    // ==========================
    private float CurrentScatterChance() => bonusAktif ? scatterChanceBonus : scatterChanceNormal;

    /// <summary>SenaryoServisi delegasyonu: Senaryo 1'de 50, Senaryo 2'de 75 spin sonrası scatter garantisi. Garanti spininde dolumda 0 döner (sonra GrideEnAzDortScatterKoy tam 4 koyar).</summary>
    private float GetScatterChanceFor(bool bonusAktif)
    {
        if (bonusAktif) return scatterChanceBonus;
        if (_adminManuelScatterKilidi) return scatterChanceNormal;
        if (SenaryoYoneticisi.I == null) return scatterChanceNormal;
        var asama = SenaryoYoneticisi.I.mevcutAsama;
        int sinceScatter = SenaryoYoneticisi.I.SpinsSinceLastScatter();
        // Garanti spin: dolumda rastgele scatter yok (sonra GrideEnAzDortScatterKoy).
        if (asama == SenaryoYoneticisi.SenaryoAsama.Asama1_IsindirmaUmut && sinceScatter >= 50)
            return 0f;
        if (asama == SenaryoYoneticisi.SenaryoAsama.Asama2_KontrolBende && sinceScatter >= 75)
            return 0f;

        float senaryoTaban;
        if (asama == SenaryoYoneticisi.SenaryoAsama.Asama1_IsindirmaUmut)
            senaryoTaban = 0.006f;
        else if (asama == SenaryoYoneticisi.SenaryoAsama.Asama2_KontrolBende)
            senaryoTaban = 0.015f;
        else if (asama == SenaryoYoneticisi.SenaryoAsama.Asama3_AzDahaKayipKovalama)
            senaryoTaban = 0.006f;
        else
            return scatterChanceNormal;

        // Slider / Inspector ile yüksek oran verildiyse senaryo tavanını ezmesin (kilitsiz testlerde yıldız düşsün).
        return Mathf.Max(senaryoTaban, scatterChanceNormal);
    }

    // KURAL SABİT: tumble eşiği minClusterSize=8
    // Zorluk arttıkça, 8'e TAMAMLAYACAK sembollerin seçilme ihtimali azalır (anti-8 bias).

// v=8'de nötr; v<8'de easy bias, v>8'de hard bias uygular.
private float BiasMultiplier(float easyMult, float hardMult)
{
    float m = 1f;
    if (_easyBias01 > 0f) m *= Mathf.Lerp(1f, easyMult, _easyBias01);
    if (_hardBias01 > 0f) m *= Mathf.Lerp(1f, hardMult, _hardBias01);
    return m;
}

    /// <summary>Ham kazanç yalnızca paytable × bahis ile belirlenir; zorluk üretim (cluster/dolum) tarafında kalır.</summary>
    private int ZorlukKazancCarpaniUygula(int hamKazanc)
    {
        if (hamKazanc <= 0) return 0;
        return hamKazanc;
    }

    // ==========================
// ÇARPAN (Yeni Sistem: ekrana bomba/jeton düşer, değerler ÇARPILIR)
// ==========================

    private void CarpanUretVeBirik()
    {
        if (_carpanServisi == null) return;
        _carpanServisi.SetForceCarpan(zorlaSiradakiCarpan);
        _carpanServisi.TryScheduleCarpanDrop(bonusAktif);
        zorlaSiradakiCarpan = 0;
        if (carpanAyarlari != null)
            carpanAyarlari.ZorlaSiradakiCarpan = 0;
    }

    private void CarpanlariDoluGriddeUygula()
    {
        _carpanYerlestirmeServisi?.CarpanlariDoluGriddeUygula();
    }

    private void CarpanlariDoluGriddeSessizUygula()
    {
        if (_carpanServisi == null || grid == null || carpanDegerGrid == null) return;

        var pending = _carpanServisi.GetPendingDrops();
        if (pending == null || pending.Count == 0) return;

        // Kazanç kümesini bozma: önce mevcut patlayacak hücreleri işaretle.
        var kazancHucreleri = new HashSet<int>();
        if (_tumbleServisi != null && _izgaraServisi != null)
        {
            _tumbleServisi.SetGrid(grid);
            var clusters = _tumbleServisi.FindClustersToRemove(minClusterSize);
            if (clusters != null)
            {
                for (int i = 0; i < clusters.Count; i++)
                {
                    int idx = _izgaraServisi.XYToIndex(clusters[i].x, clusters[i].y);
                    if (idx >= 0) kazancHucreleri.Add(idx);
                }
            }
        }

        var adaylar = new List<Vector2Int>();
        var guvenliAdaylar = new List<Vector2Int>();
        for (int y = 0; y < satir; y++)
        {
            for (int x = 0; x < sutun; x++)
            {
                if (grid[x, y] == CARPAN_SEMBOL) continue;
                if (grid[x, y] == _scatterIndexCache) continue;
                if (grid[x, y] < 0) continue;
                var p = new Vector2Int(x, y);
                adaylar.Add(p);
                int idx = _izgaraServisi != null ? _izgaraServisi.XYToIndex(x, y) : -1;
                if (idx < 0 || !kazancHucreleri.Contains(idx))
                    guvenliAdaylar.Add(p);
            }
        }
        if (adaylar.Count == 0) return;

        var kullanilacakAdaylar = guvenliAdaylar.Count > 0 ? guvenliAdaylar : adaylar;

        int carpanKalan = _carpanServisi.GetCarpanKalanBuSpin();
        int placeCount = Mathf.Min(pending.Count, kullanilacakAdaylar.Count, carpanKalan);
        if (placeCount <= 0) return;

        var placed = new List<int>();
        for (int i = 0; i < placeCount; i++)
        {
            int pick = UnityEngine.Random.Range(0, kullanilacakAdaylar.Count);
            Vector2Int p = kullanilacakAdaylar[pick];
            kullanilacakAdaylar.RemoveAt(pick);

            int carpan = pending[i];
            if (carpan <= 0) continue;

            grid[p.x, p.y] = CARPAN_SEMBOL;
            carpanDegerGrid[p.x, p.y] = carpan;
            int idx = _izgaraServisi != null ? _izgaraServisi.XYToIndex(p.x, p.y) : -1;
            if (carpanDegerByCellIndex != null && idx >= 0 && idx < carpanDegerByCellIndex.Length)
                carpanDegerByCellIndex[idx] = carpan;
            placed.Add(carpan);
        }

        if (placed.Count > 0)
            _carpanServisi.RecordPlacedCarpanlar(placed);
    }

    private void ForceCarpaniIlkGriddeGuvenliYerlestir(int carpanDegeri)
    {
        if (carpanDegeri <= 0 || grid == null || carpanDegerGrid == null || _izgaraServisi == null) return;

        var clusterIdx = new HashSet<int>();
        if (_tumbleServisi != null)
        {
            _tumbleServisi.SetGrid(grid);
            var clusters = _tumbleServisi.FindClustersToRemove(minClusterSize);
            if (clusters != null)
            {
                for (int i = 0; i < clusters.Count; i++)
                {
                    int idx = _izgaraServisi.XYToIndex(clusters[i].x, clusters[i].y);
                    if (idx >= 0) clusterIdx.Add(idx);
                }
            }
        }

        var guvenli = new List<Vector2Int>();
        var tumAday = new List<Vector2Int>();
        for (int y = 0; y < satir; y++)
        {
            for (int x = 0; x < sutun; x++)
            {
                if (grid[x, y] < 0) continue;
                if (grid[x, y] == CARPAN_SEMBOL) continue;
                if (grid[x, y] == _scatterIndexCache) continue;
                var p = new Vector2Int(x, y);
                tumAday.Add(p);
                int idx = _izgaraServisi.XYToIndex(x, y);
                if (!clusterIdx.Contains(idx))
                    guvenli.Add(p);
            }
        }

        var secimListesi = guvenli.Count > 0 ? guvenli : tumAday;
        if (secimListesi.Count == 0) return;

        var secim = secimListesi[UnityEngine.Random.Range(0, secimListesi.Count)];
        grid[secim.x, secim.y] = CARPAN_SEMBOL;
        carpanDegerGrid[secim.x, secim.y] = carpanDegeri;
        int ridx = _izgaraServisi.XYToIndex(secim.x, secim.y);
        if (carpanDegerByCellIndex != null && ridx >= 0 && ridx < carpanDegerByCellIndex.Length)
            carpanDegerByCellIndex[ridx] = carpanDegeri;
    }

    int ICarpanYerlestirmeBaglami.GetSutun() => sutun;
    int ICarpanYerlestirmeBaglami.GetSatir() => satir;
    int[,] ICarpanYerlestirmeBaglami.GetGrid() => grid;
    int[,] ICarpanYerlestirmeBaglami.GetCarpanDegerGrid() => carpanDegerGrid;
    int[] ICarpanYerlestirmeBaglami.GetCarpanDegerByCellIndex() => carpanDegerByCellIndex;
    int ICarpanYerlestirmeBaglami.GetCarpanSembol() => CARPAN_SEMBOL;
    int ICarpanYerlestirmeBaglami.GetScatterIndexCache() => _scatterIndexCache;
    CarpanServisi ICarpanYerlestirmeBaglami.GetCarpanServisi() => _carpanServisi;
    IzgaraServisi ICarpanYerlestirmeBaglami.GetIzgaraServisi() => _izgaraServisi;

private int RastgeleCarpan()
{
    // Doğal (operatör müdahalesi olmayan) çarpan havuzu yalnızca {2,3,5,8,10}.
    // 100x/250x/500x gibi büyük çarpanlar yalnızca force path üzerinden düşer:
    //   - Operatör admin paneli: AdminZorlaCarpanSec → zorlaSiradakiCarpan → CarpanServisi._forceCarpan
    //   - Senaryo 4/5 BOMB tipi: OyunYoneticisi.Senaryolar.cs içinde zorlaSiradakiCarpan = 100/500 olarak set edilir
    // Force path TryScheduleCarpanDrop'ta _rollCarpanDegeri çağrılmadan kullanılır (CarpanServisi.cs:102-110).
    int secilen;
    string havuzAdi;
    if (IsAdminSenaryo1Veya2Aktif())
    {
        // Senaryo 1/2 manipülasyon davranışı: yalnızca 2/3/5.
        int[] havuz = new int[] { 2, 3, 5 };
        secilen = havuz[UnityEngine.Random.Range(0, havuz.Length)];
        havuzAdi = "SEN12";
    }
    else if (IsAdminSenaryo3Aktif())
    {
        // Senaryo 3 manipülasyon davranışı: 2/3/5/10.
        int[] havuz = new int[] { 2, 3, 5, 10 };
        secilen = havuz[UnityEngine.Random.Range(0, havuz.Length)];
        havuzAdi = "SEN3";
    }
    else
    {
        // Asama 1/2 (senaryolu sahne) ve diğer tüm doğal yollar: ortak temiz havuz.
        int[] havuz = new int[] { 2, 3, 5, 8, 10 };
        secilen = havuz[UnityEngine.Random.Range(0, havuz.Length)];
        havuzAdi = "DOGAL";
    }
    Debug.Log($"[CARPAN] kaynak=DOGAL havuz={havuzAdi} secilen={secilen}x");
    return secilen;
}

    private int UygulaSpinCarpani(int spinKazanci) => _ekonomiServisi != null ? _ekonomiServisi.UygulaSpinCarpani(spinKazanci) : 0;
}