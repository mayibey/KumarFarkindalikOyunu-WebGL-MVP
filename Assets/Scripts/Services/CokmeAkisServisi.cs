using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CokmeAkisServisi için context arayüzü. OyunYoneticisi implement eder.
/// </summary>
public interface ICokmeAkisBaglami
{
    int GetSutun();
    int GetSatir();
    int[,] GetGrid();
    int[,] GetCarpanDegerGrid();
    Vector2[] GetCellPos();
    RectTransform[] GetCellRT();
    float GetSpawnFromTopOffset();
    float GetFallDuration();
    bool GetBonusAktif();
    int GetCarpanSembol();
    IzgaraServisi GetIzgaraServisi();
    TumbleServisi GetTumbleServisi();
    CarpanServisi GetCarpanServisi();
    SenaryoServisi GetSenaryoServisi();
    bool ConsumeBombaSonrasiIlkRefillCarpanEngeli();
    void ApplyNewGridAndSync(int[,] newGrid, int[,] newCarpanGrid);
    /// <summary>Refill sonrası yerleşen çarpan jetonlarını kazanç yazısına doğru uçurur; bittiğinde UI yenilenir.</summary>
    IEnumerator CarpanKazancUcusunuOynat(IReadOnlyList<int> hucreIndeksleri, IReadOnlyList<int> carpanDegerleri);
}

/// <summary>
/// Çökme + doldurma + animasyon akışı. CokmeDoldurVeCanlandir gövdesi burada.
/// </summary>
public class CokmeAkisServisi
{
    private ICokmeAkisBaglami _ctx;

    public virtual void SetBaglam(ICokmeAkisBaglami ctx)
    {
        _ctx = ctx;
    }

    /// <summary>
    /// Tumble sonrası: sütun çökmez; patlayan hücreler (-1) yerinde yeni sembolle dolar, kalan meyveler aynı (x,y) konumunda kalır.
    /// </summary>
    private static void YerindeTumbleRefillGridOlustur(
        int sutun,
        int satir,
        int[,] oldGrid,
        int[,] carpanDegerGrid,
        int carpanSembol,
        bool bonusAktif,
        IzgaraServisi izgaraServisi,
        out int[,] newGrid,
        out int[,] newCarpanGrid,
        out List<Vector2Int> newlySpawnedCells)
    {
        newGrid = new int[sutun, satir];
        newCarpanGrid = new int[sutun, satir];
        newlySpawnedCells = new List<Vector2Int>();

        for (int x = 0; x < sutun; x++)
        {
            for (int y = 0; y < satir; y++)
            {
                int v = oldGrid[x, y];
                if (v == -1)
                {
                    newGrid[x, y] = -1;
                    newCarpanGrid[x, y] = 0;
                }
                else
                {
                    newGrid[x, y] = v;
                    newCarpanGrid[x, y] = v == carpanSembol ? carpanDegerGrid[x, y] : 0;
                }
            }
        }

        for (int x = 0; x < sutun; x++)
        {
            for (int y = 0; y < satir; y++)
            {
                if (newGrid[x, y] != -1)
                    continue;
                newGrid[x, y] = izgaraServisi != null ? izgaraServisi.RandomSymbolWithScatterChance(newGrid, bonusAktif) : -1;
                newCarpanGrid[x, y] = 0;
                newlySpawnedCells.Add(new Vector2Int(x, y));
            }
        }
    }

    public virtual IEnumerator CokmeDoldurVeCanlandir()
    {
        if (_ctx == null) yield break;

        int sutun = _ctx.GetSutun();
        int satir = _ctx.GetSatir();
        int[,] grid = _ctx.GetGrid();
        int[,] carpanDegerGrid = _ctx.GetCarpanDegerGrid();
        var izgaraServisi = _ctx.GetIzgaraServisi();
        var cellPos = _ctx.GetCellPos();
        var cellRT = _ctx.GetCellRT();
        float spawnFromTopOffset = _ctx.GetSpawnFromTopOffset();
        float etkinUstSpawnY = EtkinUsttenSpawnYOfset(spawnFromTopOffset, cellPos, cellRT, sutun, satir);
        float fallDuration = _ctx.GetFallDuration();
        bool bonusAktif = _ctx.GetBonusAktif();
        int CARPAN_SEMBOL = _ctx.GetCarpanSembol();

        int[,] oldGrid = new int[sutun, satir];
        for (int x = 0; x < sutun; x++)
            for (int y = 0; y < satir; y++)
                oldGrid[x, y] = grid[x, y];

        int hucreSayisi = izgaraServisi != null ? izgaraServisi.HucreSayisi() : 0;
        Vector2[] startPos = new Vector2[hucreSayisi];
        Vector2[] targetPos = new Vector2[hucreSayisi];
        bool[] willMove = new bool[hucreSayisi];
        bool[] isNewSpawn = new bool[hucreSayisi];
        float[] delayByCell = new float[hucreSayisi];
        List<Vector2Int> guncellenecekHucreler = new List<Vector2Int>();

        for (int i = 0; i < hucreSayisi; i++)
        {
            startPos[i] = cellPos != null && i < cellPos.Length ? cellPos[i] : Vector2.zero;
            targetPos[i] = startPos[i];
            willMove[i] = false;
            isNewSpawn[i] = false;
            if (cellRT != null && i < cellRT.Length && cellRT[i] != null)
                cellRT[i].anchoredPosition = cellPos[i];
        }

        YerindeTumbleRefillGridOlustur(
            sutun, satir, oldGrid, carpanDegerGrid, CARPAN_SEMBOL, bonusAktif, izgaraServisi,
            out int[,] newGrid, out int[,] newCarpanGrid, out List<Vector2Int> newlySpawnedCells);

        for (int k = 0; k < newlySpawnedCells.Count; k++)
        {
            var p = newlySpawnedCells[k];
            guncellenecekHucreler.Add(p);
            int idxTarget = izgaraServisi != null ? izgaraServisi.XYToIndex(p.x, p.y) : 0;
            if (idxTarget < 0 || idxTarget >= hucreSayisi || cellPos == null || idxTarget >= cellPos.Length)
                continue;
            startPos[idxTarget] = cellPos[idxTarget] + new Vector2(0f, etkinUstSpawnY);
            targetPos[idxTarget] = cellPos[idxTarget];
            willMove[idxTarget] = true;
            isNewSpawn[idxTarget] = true;
            if (cellRT != null && idxTarget < cellRT.Length && cellRT[idxTarget] != null)
                cellRT[idxTarget].anchoredPosition = startPos[idxTarget];
        }

        var carpanServisi = _ctx.GetCarpanServisi();
        var senaryoServisi = _ctx.GetSenaryoServisi();
        var pendingCarpan = carpanServisi != null ? carpanServisi.GetPendingDrops() : null;
        bool carpanEngeliAktif = _ctx.ConsumeBombaSonrasiIlkRefillCarpanEngeli();
        if (carpanEngeliAktif && carpanServisi != null)
            carpanServisi.ClearPendingDrops();
        if (!carpanEngeliAktif && pendingCarpan != null && pendingCarpan.Count > 0 && newlySpawnedCells.Count > 0)
        {
            for (int i = 0; i < newlySpawnedCells.Count; i++)
            {
                int j = UnityEngine.Random.Range(i, newlySpawnedCells.Count);
                var tmp = newlySpawnedCells[i];
                newlySpawnedCells[i] = newlySpawnedCells[j];
                newlySpawnedCells[j] = tmp;
            }
            int carpanKalan = carpanServisi.GetCarpanKalanBuSpin();
            int placeCount = Mathf.Min(pendingCarpan.Count, newlySpawnedCells.Count, carpanKalan);
            var placedCarpan = new List<int>();
            for (int i = 0; i < placeCount; i++)
            {
                var p = newlySpawnedCells[i];
                int carpan = pendingCarpan[i];
                newGrid[p.x, p.y] = CARPAN_SEMBOL;
                newCarpanGrid[p.x, p.y] = carpan;
                placedCarpan.Add(carpan);
            }
            if (placedCarpan.Count > 0)
                carpanServisi.RecordPlacedCarpanlar(placedCarpan);
        }

        _ctx.ApplyNewGridAndSync(newGrid, newCarpanGrid);
        if (izgaraServisi != null)
        {
            if (guncellenecekHucreler.Count > 0)
                izgaraServisi.RenderSpritesOnlyForCells(guncellenecekHucreler, newGrid);
            else
                izgaraServisi.RenderAllSprites(setAlphaOne: true, resetScale: false);
            izgaraServisi.ForceRefreshCarpanTextsFromGrid();
        }

        float adimGecikme = HesaplaSutunIciAdimGecikmesi(fallDuration);
        AyarlaSutunParalelSiraliGecikme(sutun, satir, hucreSayisi, willMove, delayByCell, adimGecikme);

        float t = 0f;
        bool yeniSpawnVar = newlySpawnedCells != null && newlySpawnedCells.Count > 0;
        float temelSure = carpanEngeliAktif ? fallDuration * 1.22f : fallDuration;
        float anaSure = Mathf.Max(0.0001f, yeniSpawnVar ? temelSure * 1.18f : temelSure);
        var hucreDususSuresi = new float[hucreSayisi];
        float toplamSure = DokulmeEfektiIcinGecikmeVeSureAta(
            sutun, hucreSayisi, willMove, delayByCell, anaSure, fallDuration, hucreDususSuresi);
        while (t < toplamSure)
        {
            for (int i = 0; i < hucreSayisi; i++)
            {
                if (!willMove[i]) continue;
                float sure = hucreDususSuresi[i];
                float u = Mathf.Clamp01((t - delayByCell[i]) / sure);
                float eased = 1f - Mathf.Pow(1f - u, 3.2f);
                if (cellRT != null && i < cellRT.Length && cellRT[i] != null)
                    cellRT[i].anchoredPosition = Vector2.Lerp(startPos[i], targetPos[i], eased);

                if (isNewSpawn[i] && cellRT != null && i < cellRT.Length && cellRT[i] != null)
                {
                    var img = cellRT[i].GetComponent<Image>();
                    if (img != null)
                    {
                        var c = img.color;
                        c.a = Mathf.Lerp(0.25f, 1f, u);
                        img.color = c;
                    }
                }
            }
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        for (int i = 0; i < hucreSayisi; i++)
        {
            if (!willMove[i]) continue;
            if (cellRT != null && i < cellRT.Length && cellRT[i] != null)
                cellRT[i].anchoredPosition = targetPos[i];
        }

    }

    /// <summary>Simülasyon için: çökme + refill + çarpan yerleştirme mantığı, animasyon yok. Grid context'te güncellenir; kayıt doldurulur.</summary>
    public virtual void CokmeDoldurSadeceMantik(TumbleAdimKaydi kayit)
    {
        if (_ctx == null || kayit == null) return;

        int sutun = _ctx.GetSutun();
        int satir = _ctx.GetSatir();
        int[,] grid = _ctx.GetGrid();
        int[,] carpanDegerGrid = _ctx.GetCarpanDegerGrid();
        var izgaraServisi = _ctx.GetIzgaraServisi();
        bool bonusAktif = _ctx.GetBonusAktif();
        int CARPAN_SEMBOL = _ctx.GetCarpanSembol();

        int[,] oldGrid = new int[sutun, satir];
        for (int x = 0; x < sutun; x++)
            for (int y = 0; y < satir; y++)
                oldGrid[x, y] = grid[x, y];

        kayit.DusenHucreFrom.Clear();
        kayit.DusenHucreTo.Clear();

        YerindeTumbleRefillGridOlustur(
            sutun, satir, oldGrid, carpanDegerGrid, CARPAN_SEMBOL, bonusAktif, izgaraServisi,
            out int[,] newGrid, out int[,] newCarpanGrid, out List<Vector2Int> newlySpawnedCells);

        var carpanServisi = _ctx.GetCarpanServisi();
        var senaryoServisi = _ctx.GetSenaryoServisi();
        var pendingCarpan = carpanServisi != null ? carpanServisi.GetPendingDrops() : null;
        bool carpanEngeliAktif = _ctx.ConsumeBombaSonrasiIlkRefillCarpanEngeli();
        if (carpanEngeliAktif && carpanServisi != null)
            carpanServisi.ClearPendingDrops();
        if (!carpanEngeliAktif && pendingCarpan != null && pendingCarpan.Count > 0 && newlySpawnedCells.Count > 0)
        {
            for (int i = 0; i < newlySpawnedCells.Count; i++)
            {
                int j = UnityEngine.Random.Range(i, newlySpawnedCells.Count);
                var tmp = newlySpawnedCells[i];
                newlySpawnedCells[i] = newlySpawnedCells[j];
                newlySpawnedCells[j] = tmp;
            }
            int carpanKalan = carpanServisi.GetCarpanKalanBuSpin();
            int placeCount = Mathf.Min(pendingCarpan.Count, newlySpawnedCells.Count, carpanKalan);
            var placedCarpan = new List<int>();
            for (int i = 0; i < placeCount; i++)
            {
                var p = newlySpawnedCells[i];
                int carpan = pendingCarpan[i];
                newGrid[p.x, p.y] = CARPAN_SEMBOL;
                newCarpanGrid[p.x, p.y] = carpan;
                placedCarpan.Add(carpan);
            }
            kayit.CarpanDegerleriBuTur.Clear();
            kayit.CarpanDegerleriBuTur.AddRange(placedCarpan);
            if (placedCarpan.Count > 0)
                carpanServisi.RecordPlacedCarpanlar(placedCarpan);
        }
        else
            kayit.CarpanDegerleriBuTur.Clear();

        kayit.GridRefillSonrasi = new int[sutun, satir];
        kayit.CarpanGridRefillSonrasi = new int[sutun, satir];
        for (int x = 0; x < sutun; x++)
            for (int y = 0; y < satir; y++)
            {
                kayit.GridRefillSonrasi[x, y] = newGrid[x, y];
                kayit.CarpanGridRefillSonrasi[x, y] = newCarpanGrid[x, y];
            }
        kayit.YeniSpawnEdilenHucreler.Clear();
        kayit.YeniSpawnEdilenHucreler.AddRange(newlySpawnedCells);

        _ctx.ApplyNewGridAndSync(newGrid, newCarpanGrid);
    }

    /// <summary>Kayıttaki bir tumble adımını ekranda oynat: kayıt grid uygulanır, sadece yeni hücreler düşme animasyonu alır.</summary>
    public virtual IEnumerator CokmeDoldurOynat(TumbleAdimKaydi adim)
    {
        if (_ctx == null || adim == null) yield break;

        int sutun = _ctx.GetSutun();
        int satir = _ctx.GetSatir();
        var cellPos = _ctx.GetCellPos();
        var cellRT = _ctx.GetCellRT();
        float spawnFromTopOffset = _ctx.GetSpawnFromTopOffset();
        float etkinUstSpawnY = EtkinUsttenSpawnYOfset(spawnFromTopOffset, cellPos, cellRT, sutun, satir);
        float fallDuration = _ctx.GetFallDuration();
        var izgaraServisi = _ctx.GetIzgaraServisi();
        int hucreSayisi = izgaraServisi != null ? izgaraServisi.HucreSayisi() : 0;

        _ctx.ApplyNewGridAndSync(adim.GridRefillSonrasi, adim.CarpanGridRefillSonrasi);

        var willMove = new bool[hucreSayisi];
        var isNewSpawn = new bool[hucreSayisi];
        var startPos = new Vector2[hucreSayisi];
        var targetPos = new Vector2[hucreSayisi];
        var delayByCell = new float[hucreSayisi];
        for (int i = 0; i < hucreSayisi; i++)
        {
            startPos[i] = cellPos != null && i < cellPos.Length ? cellPos[i] : Vector2.zero;
            targetPos[i] = startPos[i];
            willMove[i] = false;
            isNewSpawn[i] = false;
        }
        foreach (var p in adim.YeniSpawnEdilenHucreler)
        {
            int idx = izgaraServisi != null ? izgaraServisi.XYToIndex(p.x, p.y) : 0;
            if (idx >= 0 && idx < hucreSayisi)
            {
                willMove[idx] = true;
                isNewSpawn[idx] = true;
                startPos[idx] = (cellPos != null && idx < cellPos.Length ? cellPos[idx] : Vector2.zero) + new Vector2(0f, etkinUstSpawnY);
                targetPos[idx] = cellPos != null && idx < cellPos.Length ? cellPos[idx] : Vector2.zero;
            }
        }
        if (adim.DusenHucreFrom != null && adim.DusenHucreTo != null && izgaraServisi != null)
        {
            for (int i = 0; i < adim.DusenHucreFrom.Count && i < adim.DusenHucreTo.Count; i++)
            {
                var from = adim.DusenHucreFrom[i];
                var to = adim.DusenHucreTo[i];
                int idxTo = izgaraServisi.XYToIndex(to.x, to.y);
                int idxFrom = izgaraServisi.XYToIndex(from.x, from.y);
                if (idxTo >= 0 && idxTo < hucreSayisi && idxFrom >= 0 && idxFrom < (cellPos != null ? cellPos.Length : 0))
                {
                    willMove[idxTo] = true;
                    startPos[idxTo] = cellPos[idxFrom];
                    targetPos[idxTo] = cellPos != null && idxTo < cellPos.Length ? cellPos[idxTo] : Vector2.zero;
                }
            }
        }
        float adimGecikmeOynat = HesaplaSutunIciAdimGecikmesi(fallDuration);
        AyarlaSutunParalelSiraliGecikme(sutun, satir, hucreSayisi, willMove, delayByCell, adimGecikmeOynat);

        for (int i = 0; i < hucreSayisi; i++)
        {
            if (cellRT == null || i >= cellRT.Length || cellRT[i] == null) continue;
            if (willMove[i])
                cellRT[i].anchoredPosition = startPos[i];
            else
                cellRT[i].anchoredPosition = cellPos != null && i < cellPos.Length ? cellPos[i] : Vector2.zero;
        }
        // Refill sonrası sadece değişen hücrelerin sprite'ını güncelle; tam grid refresh "sayfa yenileniyor" hissi verir.
        if (izgaraServisi != null)
        {
            List<Vector2Int> degisenHucreler = new List<Vector2Int>();
            if (adim.YeniSpawnEdilenHucreler != null)
            {
                for (int i = 0; i < adim.YeniSpawnEdilenHucreler.Count; i++)
                    degisenHucreler.Add(adim.YeniSpawnEdilenHucreler[i]);
            }
            if (adim.DusenHucreTo != null)
            {
                for (int i = 0; i < adim.DusenHucreTo.Count; i++)
                    degisenHucreler.Add(adim.DusenHucreTo[i]);
            }
            // Konstrukte enjeksiyonuyla sembolü değiştirilen hücreler spawn/düşme listesinde yer almaz; sprite'ları ayrıca güncellenmeli.
            if (adim.InjekteEdilenHucreler != null)
            {
                for (int i = 0; i < adim.InjekteEdilenHucreler.Count; i++)
                    degisenHucreler.Add(adim.InjekteEdilenHucreler[i]);
            }

            if (degisenHucreler.Count > 0)
                izgaraServisi.RenderSpritesOnlyForCells(degisenHucreler, adim.GridRefillSonrasi);
            else
                izgaraServisi.RenderAllSprites(setAlphaOne: true, resetScale: false);
            izgaraServisi.ForceRefreshCarpanTextsFromGrid();
        }

        float t = 0f;
        bool yeniSpawnVar = adim.YeniSpawnEdilenHucreler != null && adim.YeniSpawnEdilenHucreler.Count > 0;
        float anaSure = Mathf.Max(0.0001f, yeniSpawnVar ? fallDuration * 1.18f : fallDuration);
        var hucreDususSuresiOynat = new float[hucreSayisi];
        float toplamSure = DokulmeEfektiIcinGecikmeVeSureAta(
            sutun, hucreSayisi, willMove, delayByCell, anaSure, fallDuration, hucreDususSuresiOynat);
        while (t < toplamSure)
        {
            for (int i = 0; i < hucreSayisi; i++)
            {
                if (!willMove[i]) continue;
                float sure = hucreDususSuresiOynat[i];
                float u = Mathf.Clamp01((t - delayByCell[i]) / sure);
                float eased = 1f - Mathf.Pow(1f - u, 3.2f);
                if (cellRT != null && i < cellRT.Length && cellRT[i] != null)
                    cellRT[i].anchoredPosition = Vector2.Lerp(startPos[i], targetPos[i], eased);

                if (isNewSpawn[i] && cellRT != null && i < cellRT.Length && cellRT[i] != null)
                {
                    var img = cellRT[i].GetComponent<Image>();
                    if (img != null)
                    {
                        var c = img.color;
                        c.a = Mathf.Lerp(0.25f, 1f, u);
                        img.color = c;
                    }
                }
            }
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        for (int i = 0; i < hucreSayisi; i++)
        {
            if (!willMove[i]) continue;
            if (cellRT != null && i < cellRT.Length && cellRT[i] != null)
                cellRT[i].anchoredPosition = targetPos[i];
        }
        // Kayma olmasın: tüm hücreleri tam konuma kilitle.
        if (cellPos != null && cellRT != null)
        {
            for (int i = 0; i < hucreSayisi && i < cellPos.Length && i < cellRT.Length; i++)
            {
                if (cellRT[i] != null)
                    cellRT[i].anchoredPosition = cellPos[i];
            }
        }
        if (izgaraServisi != null)
            izgaraServisi.ForceRefreshCarpanTextsFromGrid();

    }

    /// <summary>
    /// Her sütunda, hedef satıra göre üstten alta sıralı başlangıç gecikmesi (y=0 önce, sonra y=1…).
    /// Tüm sütunlar aynı adım süresini kullanır — birbirleriyle hizalı, aynı hızda dalga.
    /// </summary>
    private static void AyarlaSutunParalelSiraliGecikme(int sutun, int satir, int hucreSayisi, bool[] willMove, float[] delayByCell, float adimGecikme)
    {
        if (sutun <= 0 || satir <= 0 || willMove == null || delayByCell == null || adimGecikme < 0f)
            return;

        var sutundakiler = new List<int>(satir);
        for (int x = 0; x < sutun; x++)
        {
            sutundakiler.Clear();
            for (int y = 0; y < satir; y++)
            {
                int idx = y * sutun + x;
                if (idx >= hucreSayisi || idx >= willMove.Length || idx >= delayByCell.Length)
                    continue;
                if (willMove[idx])
                    sutundakiler.Add(idx);
            }
            sutundakiler.Sort((a, b) =>
            {
                int ya = a / sutun;
                int yb = b / sutun;
                return ya.CompareTo(yb);
            });
            for (int k = 0; k < sutundakiler.Count; k++)
                delayByCell[sutundakiler[k]] = adimGecikme * k;
        }
    }

    /// <summary>
    /// Sütun sırasına ek olarak hafif rastgele gecikme ve hücre başına farklı düşüş süresi — toplu blok yerine dökülme hissi.
    /// </summary>
    private static float DokulmeEfektiIcinGecikmeVeSureAta(
        int sutun,
        int hucreSayisi,
        bool[] willMove,
        float[] delayByCell,
        float anaSure,
        float fallDuration,
        float[] hucreDususSuresi)
    {
        float maxBitis = anaSure;
        float jitterUst = Mathf.Max(0.055f, fallDuration * 0.22f);
        for (int i = 0; i < hucreSayisi; i++)
        {
            hucreDususSuresi[i] = anaSure;
            if (willMove == null || i >= willMove.Length || !willMove[i])
                continue;

            delayByCell[i] += UnityEngine.Random.Range(0f, jitterUst);
            int x = sutun > 0 ? (i % sutun) : 0;
            delayByCell[i] += x * (jitterUst * 0.08f);

            hucreDususSuresi[i] = anaSure * UnityEngine.Random.Range(0.74f, 1.28f);
            float bitis = delayByCell[i] + hucreDususSuresi[i];
            if (bitis > maxBitis)
                maxBitis = bitis;
        }
        return Mathf.Max(maxBitis, anaSure);
    }

    /// <summary>Düşüş süresiyle orantılı sütun içi adım gecikmesi; tüm sütunlarda aynı taban.</summary>
    private static float HesaplaSutunIciAdimGecikmesi(float fallDuration)
    {
        return Mathf.Max(0.11f, fallDuration * 0.38f);
    }

    /// <summary>
    /// Inspector ofseti ile ilk spin (<see cref="AnimasyonServisi.AnimateGridDropIn"/>) aynı yönü kullanır (Vector2.up).
    /// Yüksek çözünürlük / büyük hücrelerde sabit px yetmediği için üst sırada "park edilmiş" görünmeyi önler:
    /// en az ~bir ızgara yüksekliği + pay kadar yukarı başlar.
    /// </summary>
    private static float EtkinUsttenSpawnYOfset(float inspectorSpawnFromTop, Vector2[] cellPos, RectTransform[] cellRT, int sutun, int satir)
    {
        float taban = inspectorSpawnFromTop * 1.35f;
        float hucreYuksekligi = 100f;
        if (cellRT != null && cellRT.Length > 0 && cellRT[0] != null)
            hucreYuksekligi = Mathf.Max(40f, cellRT[0].rect.height);

        if (cellPos != null && sutun > 0 && satir >= 2)
        {
            int iUst = 0 * sutun + 0;
            int iAlt = 1 * sutun + 0;
            if (iAlt < cellPos.Length && iUst < cellPos.Length)
            {
                float dy = Mathf.Abs(cellPos[iAlt].y - cellPos[iUst].y);
                if (dy > 1f)
                    hucreYuksekligi = Mathf.Max(hucreYuksekligi, dy);
            }
        }

        float dinamik = hucreYuksekligi * (satir + 1.35f);
        return Mathf.Max(taban, dinamik);
    }

}
