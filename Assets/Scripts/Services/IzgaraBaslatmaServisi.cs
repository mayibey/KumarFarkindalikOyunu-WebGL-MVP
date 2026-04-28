using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// IzgaraBaslatmaServisi için context arayüzü. OyunYoneticisi implement eder.
/// </summary>
public interface IIzgaraBaslatmaBaglami
{
    int GetSutun();
    int GetSatir();
    List<Sprite> GetSembolSpriteListesi();
    IzgaraServisi GetIzgaraServisi();
    TumbleServisi GetTumbleServisi();
    UIServisi GetUIServisi();
    EkonomiServisi GetEkonomiServisi();
    CarpanOverlayServisi GetCarpanOverlayServisi();
    AnimasyonServisi GetAnimasyonServisi();
    Image[] GetHucreler();
    void SetHucreler(Image[] arr);
    Transform GetSlotGridRoot();
    /// <summary>Oyun yüklenirken (prefs / GameManager sonrası) test bakiyesi uygulanacaksa buradan tetiklenir.</summary>
    void InspectorBakiyesiOyunaGirinceUygula();
    void SetGrid(int[,] g);
    void SetCarpanDegerGrid(int[,] g);
    void SetCarpanDegerByCellIndex(int[] a);
    void SetCellPos(Vector2[] p);
    void SetCellRT(RectTransform[] r);
    void SetCarpanHücreTextleri(TextMeshProUGUI[] t);
    /// <summary>Boş olabilir; o zaman meyve hücre arka planı uygulanmaz.</summary>
    Sprite GetMeyveHucreArkaPlanSprite();
}

/// <summary>
/// Izgara ayrıntı, hücre önbelleği, ilk doldurma, layout kapatma — InitRoutine gövdesi.
/// </summary>
public class IzgaraBaslatmaServisi
{
    private IIzgaraBaslatmaBaglami _ctx;

    public void SetBaglam(IIzgaraBaslatmaBaglami ctx)
    {
        _ctx = ctx;
    }

    public IEnumerator InitRoutine()
    {
        if (_ctx == null) yield break;

        _ctx.GetUIServisi()?.UIAutoBaglaGerekirse();
        _ctx.GetEkonomiServisi()?.EkonomiYukleGameManagerVeyaPrefs();
        _ctx.InspectorBakiyesiOyunaGirinceUygula();

        var sembolList = _ctx.GetSembolSpriteListesi();
        if (sembolList == null || sembolList.Count == 0)
        {
            Debug.LogError("sembolSpriteListesi boş! Inspector'dan sprite ekle.");
            yield break;
        }

        int sutun = _ctx.GetSutun();
        int satir = _ctx.GetSatir();
        var izgaraServisi = _ctx.GetIzgaraServisi();

        int[,] grid = new int[sutun, satir];
        int[,] carpanDegerGrid = new int[sutun, satir];
        int[] carpanDegerByCellIndex = new int[sutun * satir];

        _ctx.SetGrid(grid);
        _ctx.SetCarpanDegerGrid(carpanDegerGrid);
        _ctx.SetCarpanDegerByCellIndex(carpanDegerByCellIndex);
        izgaraServisi?.SetGrid(grid);
        _ctx.GetTumbleServisi()?.SetGrid(grid);
        izgaraServisi?.SetCarpanDegerGrid(carpanDegerGrid);
        izgaraServisi?.SetCarpanDegerByCellIndex(carpanDegerByCellIndex);

        int beklenenHucre = izgaraServisi != null ? izgaraServisi.HucreSayisi() : 0;
        Transform slotKok = _ctx.GetSlotGridRoot();
        Sprite meyveArka = _ctx.GetMeyveHucreArkaPlanSprite();
        if (slotKok != null && meyveArka != null && beklenenHucre > 0 && slotKok.childCount >= beklenenHucre)
            IzgaraServisi.MeyveHucrelerineArkaPlanUygula(slotKok, beklenenHucre, meyveArka);

        Image[] hucreler;
        if (slotKok != null && beklenenHucre > 0 && slotKok.childCount >= beklenenHucre)
            hucreler = IzgaraServisi.SlotGriddenMeyveImgeleriniAl(slotKok, beklenenHucre);
        else
        {
            hucreler = _ctx.GetHucreler();
            if ((hucreler == null || hucreler.Length == 0) && slotKok != null)
                hucreler = slotKok.GetComponentsInChildren<Image>(includeInactive: true);
        }
        _ctx.SetHucreler(hucreler);

        if (hucreler == null || hucreler.Length != (izgaraServisi != null ? izgaraServisi.HucreSayisi() : 0))
        {
            Debug.LogError($"Hucre sayısı hatalı. Beklenen: {izgaraServisi?.HucreSayisi() ?? 0}, Bulunan: {(hucreler == null ? 0 : hucreler.Length)}. Inspector'dan 30 Image'ı bağla veya slotGridRoot ver.");
            yield break;
        }

        izgaraServisi?.SetHucreler(hucreler);
        _ctx.GetCarpanOverlayServisi()?.SetCellImages(hucreler);

        yield return null;
        Canvas.ForceUpdateCanvases();
        izgaraServisi?.CacheCellPositionsThenDisableLayout();
        _ctx.SetCellPos(izgaraServisi?.GetCellPos());
        _ctx.SetCellRT(izgaraServisi?.GetCellRT());
        _ctx.GetAnimasyonServisi()?.SetCellPos(izgaraServisi?.GetCellPos());
        izgaraServisi?.EnsureCarpanCellTexts();
        _ctx.SetCarpanHücreTextleri(izgaraServisi?.GetCarpanHücreTextleri());

        _ctx.GetUIServisi()?.UI_Guncelle();

        izgaraServisi?.FillRandomAll();
        izgaraServisi?.RenderAllSprites(setAlphaOne: true, resetScale: true);
    }
}
