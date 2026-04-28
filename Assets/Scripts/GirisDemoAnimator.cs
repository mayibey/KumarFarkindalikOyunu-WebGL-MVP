using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 01_GirisScene için round-bazlı tam-tahta tumble demosu.
/// Her round: yeni favori → seed (8 fav + 22 diğer) → üstten düşür → cluster zinciri → bekle → tekrar.
/// </summary>
public class GirisDemoAnimator : MonoBehaviour
{
    const int SUTUN_SAYISI = 6;
    const int SATIR_SAYISI = 5;
    const int MEYVE_TIP_SAYISI = 8;
    const int BOMBA_TIPI = 8;
    const int BOS = -1;
    const int CLUSTER_ESIK = 8;
    const float DUSME_YUKSEKLIK = 400f;

    [Header("Hedef")]
    [SerializeField] private Transform slotGrid;

    [Header("Sprite'lar")]
    [SerializeField] private Sprite[] meyveSprites = new Sprite[MEYVE_TIP_SAYISI];
    [FormerlySerializedAs("carpanBombaSprite")]
    [SerializeField] private Sprite carpanBombaSpr;

    [Header("Zamanlama")]
    [SerializeField] private float patlamaSuresi = 1.2f;
    [FormerlySerializedAs("dogusSuresi")]
    [SerializeField] private float dususSuresi = 1.0f;
    [SerializeField] private float tumbleAraligi = 4.0f;
    [SerializeField] private float sutunStagger = 0.15f;
    [Tooltip("Her cluster patlamasından sonra, gravity başlamadan önce nefes molası.")]
    [SerializeField] private float zincirArasiBekleme = 1.5f;
    [Tooltip("Gravity bittikten sonra yeni cluster kontrolüne geçmeden bekleme.")]
    [SerializeField] private float gravityArasiBekleme = 1.0f;
    [Tooltip("Round açılış düşüşü tamamlandıktan sonra, ilk cluster patlamadan önce dolu tahtayı izleme süresi.")]
    [SerializeField] private float roundBasiBekleme = 2.0f;
    [Tooltip("Yerinde belirme animasyonu süresi (her hücre için).")]
    [SerializeField] private float belirmeAnimasyonSuresi = 0.5f;
    [Tooltip("Her hücrenin rastgele başlangıç gecikmesinin üst sınırı (0..max).")]
    [SerializeField] private float belirmeMaxGecikme = 0.6f;

    [Header("Round")]
    [Tooltip("Her round başında favori meyveden kaç hücre seed edilecek (>=8 cluster garantisi).")]
    [SerializeField] private int favoriAdet = 8;

    [Header("Bomba (şimdilik kullanılmıyor — sonra etkinleşecek)")]
    [Range(0f, 1f)]
    [FormerlySerializedAs("carpanIhtimali")]
    [SerializeField] private float bombaIhtimali = 0.4f;
    [SerializeField] private float bombaPatlamaGecikmesi = 1.0f;

    private int[,] grid;
    private Image[,] hucreImg;
    private Vector3[,] orjPos;
    private int oncekiFavori = -1;
    private bool calisiyor;

    void Start()
    {
        if (slotGrid == null)
        {
            var bulunan = GameObject.Find("SlotGrid");
            if (bulunan != null) slotGrid = bulunan.transform;
        }
        if (slotGrid == null)
        {
            Debug.LogWarning("[GirisDemoAnimator] SlotGrid bulunamadı.");
            enabled = false;
            return;
        }
        if (meyveSprites == null || meyveSprites.Length < MEYVE_TIP_SAYISI)
        {
            Debug.LogWarning("[GirisDemoAnimator] meyveSprites en az 8 adet olmalı.");
            enabled = false;
            return;
        }

        var imgList = new List<Image>();
        foreach (var img in slotGrid.GetComponentsInChildren<Image>(true))
        {
            if (img.transform == slotGrid) continue;
            imgList.Add(img);
        }
        int beklenen = SUTUN_SAYISI * SATIR_SAYISI;
        if (imgList.Count < beklenen)
        {
            Debug.LogWarning($"[GirisDemoAnimator] Beklenen {beklenen} hücre, bulunan {imgList.Count}.");
            enabled = false;
            return;
        }

        // Layout component'lerini bul, önce force build et — sonra cache'ten manuel pozisyon kontrolü
        var glg = slotGrid.GetComponent<GridLayoutGroup>();
        var csf = slotGrid.GetComponent<ContentSizeFitter>();
        Canvas.ForceUpdateCanvases();
        if (slotGrid is RectTransform srt)
            LayoutRebuilder.ForceRebuildLayoutImmediate(srt);

        hucreImg = new Image[SUTUN_SAYISI, SATIR_SAYISI];
        grid = new int[SUTUN_SAYISI, SATIR_SAYISI];
        orjPos = new Vector3[SUTUN_SAYISI, SATIR_SAYISI];

        for (int y = 0; y < SATIR_SAYISI; y++)
        {
            for (int x = 0; x < SUTUN_SAYISI; x++)
            {
                int idx = y * SUTUN_SAYISI + x;
                hucreImg[x, y] = imgList[idx];
                orjPos[x, y] = imgList[idx].transform.localPosition;
            }
        }

        // Layout dondur — pozisyonlar artık manuel kontrol edilecek
        if (glg != null) glg.enabled = false;
        if (csf != null) csf.enabled = false;

        // Başlangıç temiz hali
        for (int y = 0; y < SATIR_SAYISI; y++)
        {
            for (int x = 0; x < SUTUN_SAYISI; x++)
            {
                var img = hucreImg[x, y];
                img.sprite = null;
                img.color = new Color(1f, 1f, 1f, 0f);
                img.transform.localScale = Vector3.one;
                img.transform.localPosition = orjPos[x, y];
                grid[x, y] = BOS;
            }
        }

        calisiyor = true;
        StartCoroutine(AnaRoundDongusu());
    }

    void OnDisable()
    {
        calisiyor = false;
        StopAllCoroutines();
    }

    int YeniFavoriSec()
    {
        int yeni;
        do { yeni = Random.Range(0, MEYVE_TIP_SAYISI); }
        while (yeni == oncekiFavori && MEYVE_TIP_SAYISI > 1);
        oncekiFavori = yeni;
        return yeni;
    }

    int RandomNonFavori(int favori)
    {
        int sec = Random.Range(0, MEYVE_TIP_SAYISI - 1);
        if (sec >= favori) sec++;
        return sec;
    }

    int SayFavori(int favori)
    {
        int n = 0;
        for (int y = 0; y < SATIR_SAYISI; y++)
            for (int x = 0; x < SUTUN_SAYISI; x++)
                if (grid[x, y] == favori) n++;
        return n;
    }

    IEnumerator AnaRoundDongusu()
    {
        while (calisiyor)
        {
            int favori = YeniFavoriSec();
            yield return RoundAcilisi(favori);
            yield return new WaitForSeconds(roundBasiBekleme);

            int zincirNo = 0;
            float roundBaslangic = Time.realtimeSinceStartup;
            while (true)
            {
                var patlayanlar = ClusterBul(out int patlayanTip);
                if (patlayanlar.Count == 0) break;
                zincirNo++;
                int favSay = SayFavori(favori);
                Debug.Log($"[Demo] Tumble #{zincirNo}: {patlayanlar.Count} hücre patlıyor (favori={favori}, fav sayısı={favSay})");
                yield return Patlat(patlayanlar);
                yield return new WaitForSeconds(zincirArasiBekleme);
                yield return GravityVeDoldur();
                yield return new WaitForSeconds(gravityArasiBekleme);
            }
            float roundSure = Time.realtimeSinceStartup - roundBaslangic;
            Debug.Log($"[Demo] Round bitti: {zincirNo} zincir, {roundSure:F2} sn");

            yield return new WaitForSeconds(tumbleAraligi);
        }
    }

    IEnumerator RoundAcilisi(int favori)
    {
        int total = SUTUN_SAYISI * SATIR_SAYISI;

        // 1. 30 hücrelik geçici dizi: önce hepsi non-favori, sonra rastgele favoriAdet konumu favori yap
        int[] dizi = new int[total];
        for (int i = 0; i < total; i++) dizi[i] = RandomNonFavori(favori);

        var indeksler = new List<int>(total);
        for (int i = 0; i < total; i++) indeksler.Add(i);
        for (int i = indeksler.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int tmp = indeksler[i];
            indeksler[i] = indeksler[j];
            indeksler[j] = tmp;
        }
        int favoriYerlestir = Mathf.Clamp(favoriAdet, CLUSTER_ESIK, total);
        for (int i = 0; i < favoriYerlestir; i++)
            dizi[indeksler[i]] = favori;

        Debug.Log($"[Demo] Yeni round: favori meyve {favori}, {favoriYerlestir} hücre seed edildi");

        // 2. Tahtayı boşalt: sprite=null, alpha=0, scale=1, position=orjPos
        for (int y = 0; y < SATIR_SAYISI; y++)
        {
            for (int x = 0; x < SUTUN_SAYISI; x++)
            {
                var img = hucreImg[x, y];
                img.sprite = null;
                img.color = new Color(1f, 1f, 1f, 0f);
                img.transform.localScale = Vector3.one;
                img.transform.localPosition = orjPos[x, y];
                grid[x, y] = BOS;
            }
        }

        // 3. Tüm tahta için sprite ata + belirme başlangıç durumu (alpha=0, scale=0.5, pos=orjPos)
        var tumHucreler = new List<Vector2Int>(total);
        for (int y = 0; y < SATIR_SAYISI; y++)
        {
            for (int x = 0; x < SUTUN_SAYISI; x++)
            {
                int idx = y * SUTUN_SAYISI + x;
                int tip = dizi[idx];
                grid[x, y] = tip;
                var img = hucreImg[x, y];
                img.sprite = meyveSprites[tip];
                img.color = new Color(1f, 1f, 1f, 0f);
                img.transform.localScale = Vector3.one * 0.5f;
                img.transform.localPosition = orjPos[x, y];
                tumHucreler.Add(new Vector2Int(x, y));
            }
        }

        // 4. Yerinde belirme: rastgele gecikmeli fade-in + scale pop
        yield return BelirmeAnimasyonu(tumHucreler);
    }

    List<Vector2Int> ClusterBul(out int patlayanTip)
    {
        patlayanTip = -1;
        var sayilar = new int[MEYVE_TIP_SAYISI];
        for (int y = 0; y < SATIR_SAYISI; y++)
            for (int x = 0; x < SUTUN_SAYISI; x++)
            {
                int t = grid[x, y];
                if (t >= 0 && t < MEYVE_TIP_SAYISI) sayilar[t]++;
            }

        for (int i = 0; i < MEYVE_TIP_SAYISI; i++)
            if (sayilar[i] >= CLUSTER_ESIK) { patlayanTip = i; break; }

        var sonuc = new List<Vector2Int>();
        if (patlayanTip < 0) return sonuc;

        for (int y = 0; y < SATIR_SAYISI; y++)
            for (int x = 0; x < SUTUN_SAYISI; x++)
                if (grid[x, y] == patlayanTip) sonuc.Add(new Vector2Int(x, y));
        return sonuc;
    }

    IEnumerator Patlat(List<Vector2Int> hucreler)
    {
        // Faz 1 (0-40% / 0.2 sn): scale 1 → 1.5, alpha 1 sabit
        // Faz 2 (40-100% / 0.3 sn): scale 1.5 → 0, alpha 1 → 0
        float faz1Bitis = patlamaSuresi * 0.4f;
        float t = 0f;
        while (t < patlamaSuresi)
        {
            t += Time.deltaTime;
            float scale, alfa;
            if (t < faz1Bitis)
            {
                float p = (faz1Bitis > 0f) ? Mathf.Clamp01(t / faz1Bitis) : 1f;
                scale = Mathf.Lerp(1f, 1.5f, p);
                alfa = 1f;
            }
            else
            {
                float faz2Sure = patlamaSuresi - faz1Bitis;
                float p = (faz2Sure > 0f) ? Mathf.Clamp01((t - faz1Bitis) / faz2Sure) : 1f;
                scale = Mathf.Lerp(1.5f, 0f, p);
                alfa = Mathf.Lerp(1f, 0f, p);
            }
            foreach (var h in hucreler)
            {
                var img = hucreImg[h.x, h.y];
                img.color = new Color(1f, 1f, 1f, alfa);
                img.transform.localScale = Vector3.one * scale;
            }
            yield return null;
        }
        foreach (var h in hucreler)
        {
            grid[h.x, h.y] = BOS;
            var img = hucreImg[h.x, h.y];
            img.sprite = null;
            img.color = new Color(1f, 1f, 1f, 0f);
            img.transform.localScale = Vector3.one;
        }
    }

    IEnumerator GravityVeDoldur()
    {
        // Yenileme: gerçek gravity yok. Patlamadan kalan BOS hücreler aynı yerde yeni meyveyle dolar.
        var yenilenen = new List<Vector2Int>();
        for (int y = 0; y < SATIR_SAYISI; y++)
        {
            for (int x = 0; x < SUTUN_SAYISI; x++)
            {
                if (grid[x, y] != BOS) continue;
                int yeniTip = Random.Range(0, MEYVE_TIP_SAYISI);
                grid[x, y] = yeniTip;
                var img = hucreImg[x, y];
                img.sprite = meyveSprites[yeniTip];
                img.color = new Color(1f, 1f, 1f, 0f);
                img.transform.localScale = Vector3.one * 0.5f;
                img.transform.localPosition = orjPos[x, y];
                yenilenen.Add(new Vector2Int(x, y));
            }
        }
        if (yenilenen.Count == 0) yield break;
        yield return BelirmeAnimasyonu(yenilenen);
    }

    IEnumerator BelirmeAnimasyonu(List<Vector2Int> hucreler)
    {
        // Her hücreye 0..belirmeMaxGecikme arası rastgele başlangıç gecikmesi
        var gecikme = new float[hucreler.Count];
        float maxGecikme = 0f;
        for (int i = 0; i < hucreler.Count; i++)
        {
            gecikme[i] = Random.Range(0f, belirmeMaxGecikme);
            if (gecikme[i] > maxGecikme) maxGecikme = gecikme[i];
        }

        // Faz 1 (0-70%): scale 0.5 → 1.05 ease-out, alpha 0 → 1
        // Faz 2 (70-100%): scale 1.05 → 1.0 (pop dönüşü)
        const float FAZ1_ORAN = 0.7f;
        float toplamSure = maxGecikme + belirmeAnimasyonSuresi;
        float t = 0f;
        while (t < toplamSure)
        {
            t += Time.deltaTime;
            for (int i = 0; i < hucreler.Count; i++)
            {
                float localT = t - gecikme[i];
                if (localT < 0f) continue;
                float p = Mathf.Clamp01(localT / belirmeAnimasyonSuresi);

                float scale, alfa;
                if (p < FAZ1_ORAN)
                {
                    float p1 = p / FAZ1_ORAN;
                    float ease = 1f - (1f - p1) * (1f - p1);
                    scale = Mathf.Lerp(0.5f, 1.05f, ease);
                    alfa = p1;
                }
                else
                {
                    float p2 = (p - FAZ1_ORAN) / (1f - FAZ1_ORAN);
                    scale = Mathf.Lerp(1.05f, 1.0f, p2);
                    alfa = 1f;
                }

                var h = hucreler[i];
                var img = hucreImg[h.x, h.y];
                img.color = new Color(1f, 1f, 1f, alfa);
                img.transform.localScale = Vector3.one * scale;
            }
            yield return null;
        }

        // Final: tam ölçek, tam alpha
        foreach (var h in hucreler)
        {
            var img = hucreImg[h.x, h.y];
            img.color = Color.white;
            img.transform.localScale = Vector3.one;
        }
    }

    /*
    // ── BOMBA: şimdilik devre dışı, sonraki adımda yeniden etkinleşecek ──
    IEnumerator BombaDusur()
    {
        int x = Random.Range(0, SUTUN_SAYISI);
        int y = Random.Range(0, SATIR_SAYISI);
        grid[x, y] = BOMBA_TIPI;
        Debug.Log($"[Demo] Bomba düştü: ({x},{y})");

        var img = hucreImg[x, y];
        img.sprite = carpanBombaSpr;
        img.color = new Color(1f, 1f, 1f, 0f);
        img.transform.localScale = Vector3.zero;

        float t = 0f;
        while (t < dususSuresi)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / dususSuresi);
            float ease = 1f - (1f - p) * (1f - p);
            img.color = new Color(1f, 1f, 1f, ease);
            img.transform.localScale = Vector3.one * ease;
            yield return null;
        }
        img.color = Color.white;
        img.transform.localScale = Vector3.one;
    }

    IEnumerator BombaPatlat()
    {
        int bx = -1, by = -1;
        for (int yy = 0; yy < SATIR_SAYISI && bx < 0; yy++)
        {
            for (int xx = 0; xx < SUTUN_SAYISI; xx++)
            {
                if (grid[xx, yy] == BOMBA_TIPI) { bx = xx; by = yy; break; }
            }
        }
        if (bx < 0) yield break;

        var hucreler = new List<Vector2Int> { new Vector2Int(bx, by) };
        if (bx > 0) hucreler.Add(new Vector2Int(bx - 1, by));
        if (bx < SUTUN_SAYISI - 1) hucreler.Add(new Vector2Int(bx + 1, by));
        if (by > 0) hucreler.Add(new Vector2Int(bx, by - 1));
        if (by < SATIR_SAYISI - 1) hucreler.Add(new Vector2Int(bx, by + 1));

        Debug.Log($"[Demo] Bomba patladı: {hucreler.Count} hücre");
        yield return Patlat(hucreler);
    }
    */
}
