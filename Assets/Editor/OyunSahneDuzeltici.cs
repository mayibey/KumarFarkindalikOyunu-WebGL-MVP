using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Kumar / Sahne → 04 Duzenle menüsünden çalıştır.
/// Sahne 04_AdminOyunScene açıkken çalıştırılmalı.
/// </summary>
public static class OyunSahneDuzeltici
{
    // ─────────────────────────────────────────────
    // Menü girişleri
    // ─────────────────────────────────────────────

    [MenuItem("Kumar/Sahne 06 – 1) Gereksiz Elementleri Kaldır")]
    static void Adim1_GereksizleriKaldir()
    {
        string[] kaldir = {
            "TxtHosgeldiniz",       // Hoşgeldin yazısı
            "BakiyeYukleButon",     // Bakiye Yükle butonu
            "BonusSatinAlButton",   // Bonus Satın Al butonu
            "CiftSansKutusu",       // 2 kat şans toggle + BET bedeli (tümü içinde)
            "Logo",                 // KUMAR KAZANDIRMAZ logo (sağ büyük)
        };

        int sayac = 0;
        foreach (var ad in kaldir)
        {
            var go = BulGO(ad);
            if (go == null) { Debug.LogWarning($"[Duzeltici] '{ad}' bulunamadı, atlandı."); continue; }
            go.SetActive(false);
            Debug.Log($"[Duzeltici] Deaktive: {ad}");
            sayac++;
        }
        SahneyiKaydet($"Adım 1: {sayac} eleman deaktive edildi.");
    }

    [MenuItem("Kumar/Sahne 06 – 2) SPIN Butonu Büyüt ve Animator Ekle")]
    static void Adim2_SpinButonu()
    {
        var spin = BulGO("ButtonCevir");
        if (spin == null) { Debug.LogError("[Duzeltici] ButtonCevir bulunamadı!"); return; }

        // %25 büyüt: 218x181 → 272x226
        var rt = spin.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(272f, 226f);
            Debug.Log("[Duzeltici] ButtonCevir boyutu → 272x226");
        }

        // SpinButtonAnimator ekle (zaten varsa ekleme)
        if (spin.GetComponent<SpinButtonAnimator>() == null)
        {
            spin.AddComponent<SpinButtonAnimator>();
            Debug.Log("[Duzeltici] SpinButtonAnimator eklendi.");
        }
        else
        {
            Debug.Log("[Duzeltici] SpinButtonAnimator zaten var.");
        }

        SahneyiKaydet("Adım 2: SPIN butonu güncellendi.");
    }

    [MenuItem("Kumar/Sahne 06 – 3) Ayarlar Butonunu Küçült")]
    static void Adim3_AyarlarButonu()
    {
        var ayarlar = BulGO("AyarlarButton");
        if (ayarlar == null) { Debug.LogError("[Duzeltici] AyarlarButton bulunamadı!"); return; }

        var rt = ayarlar.GetComponent<RectTransform>();
        if (rt != null)
        {
            // Sağ alta anchor, 64x64, 20px kenar boşluğu
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.sizeDelta = new Vector2(64f, 64f);
            rt.anchoredPosition = new Vector2(-20f, 20f);
            Debug.Log("[Duzeltici] AyarlarButton → sağ alt 64x64");
        }

        // "AYARLAR" yazısını kaldır; sadece ikon kalsın
        var tmplar = ayarlar.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var t in tmplar)
        {
            t.text = "";
            t.gameObject.SetActive(false);
            Debug.Log($"[Duzeltici] AyarlarButton text temizlendi: {t.gameObject.name}");
        }

        // Buton rengini yarı saydam yap
        var img = ayarlar.GetComponent<Image>();
        if (img != null)
        {
            var c = img.color;
            c.a = 0.5f;
            img.color = c;
        }

        SahneyiKaydet("Adım 3: AyarlarButton küçültüldü.");
    }

    [MenuItem("Kumar/Sahne 06 – 4) Tipografi Düzenle")]
    static void Adim4_Tipografi()
    {
        // KazancText → 48px, altın rengi
        DüzeltTMP("KazancText", 48f, new Color(1f, 0.85f, 0.2f, 1f));

        // SpinIcon (ButtonCevir içindeki yazı) → 32px beyaz
        var spinIcon = BulGO("SpinIcon");
        if (spinIcon != null)
        {
            var t = spinIcon.GetComponent<TextMeshProUGUI>();
            if (t != null) { t.fontSize = 32f; t.color = Color.white; }
        }

        // BakiyeText (1) → 28px beyaz, glow yok
        DüzeltTMP("BakiyeText (1)", 28f, Color.white, disableGlow: true);

        // BahisText (1) → 28px beyaz, glow yok
        DüzeltTMP("BahisText (1)", 28f, Color.white, disableGlow: true);

        // Label'lar (bakiye: bahis: yazıları) → 18px gri
        Color labelRenk = new Color(0.78f, 0.78f, 0.78f, 1f);
        foreach (var labelAdi in new[] { "BakiyeLabel", "BahisLabel", "Text (TMP)" })
        {
            DüzeltTMP(labelAdi, 18f, labelRenk);
        }

        SahneyiKaydet("Adım 4: Tipografi güncellendi.");
    }

    [MenuItem("Kumar/Sahne 06 – 5) Vignette Overlay Ekle")]
    static void Adim5_Vignette()
    {
        // Canvas'ı bul
        var canvas = GameObject.FindObjectOfType<Canvas>();
        if (canvas == null) { Debug.LogError("[Duzeltici] Canvas bulunamadı!"); return; }

        const string vigAd = "_VignetteOverlay";
        if (canvas.transform.Find(vigAd) != null)
        {
            Debug.Log("[Duzeltici] Vignette zaten var, atlandı.");
            return;
        }

        // bg'yi bul (arka plan) ve sibling index'ini al
        var bg = canvas.transform.Find("bg");
        int sibIdx = (bg != null) ? bg.GetSiblingIndex() + 1 : 1;

        GameObject vigGO = new GameObject(vigAd, typeof(RectTransform), typeof(Image));
        vigGO.transform.SetParent(canvas.transform, false);
        vigGO.transform.SetSiblingIndex(sibIdx);

        var vigRT = vigGO.GetComponent<RectTransform>();
        vigRT.anchorMin = Vector2.zero;
        vigRT.anchorMax = Vector2.one;
        vigRT.offsetMin = Vector2.zero;
        vigRT.offsetMax = Vector2.zero;

        // Vignette için koyu yarı saydam renk (radial gradient sprite yoksa düz renk)
        var vigImg = vigGO.GetComponent<Image>();
        vigImg.color = new Color(0f, 0f, 0f, 0f); // başlangıçta şeffaf

        // Sprite olarak Resources'tan vignette yükle; yoksa düz renk
        var vigSprite = Resources.Load<Sprite>("Vignette");
        if (vigSprite != null)
        {
            vigImg.sprite = vigSprite;
            vigImg.color = new Color(0f, 0f, 0f, 0.5f);
            vigImg.type = Image.Type.Simple;
        }
        else
        {
            // Radial vignette sprite yoksa: sadece bg image'ı karart
            var bgImg = (bg != null) ? bg.GetComponent<Image>() : null;
            if (bgImg != null)
            {
                var c = bgImg.color;
                c.r *= 0.6f; c.g *= 0.6f; c.b *= 0.6f;
                bgImg.color = c;
                Debug.Log("[Duzeltici] Vignette sprite yok — bg karartıldı.");
            }
            Object.DestroyImmediate(vigGO);
            vigGO = null;
        }

        if (vigGO != null) vigGO.GetComponent<Image>().raycastTarget = false;

        SahneyiKaydet("Adım 5: Vignette eklendi / bg karartıldı.");
    }

    [MenuItem("Kumar/Sahne 06 – 6) Sol Panel Düzenle (Manipülasyon Grubu)")]
    static void Adim6_SolPanel()
    {
        // Bu 3 element ifşa amaçlı kalmalı — aktif olduklarını doğrula ve konumla
        string[] aktifKalacak = { "BakiyeYukleButon", "BonusSatinAlButton", "CiftSansKutusu" };
        foreach (var ad in aktifKalacak)
        {
            var go = BulGO(ad);
            if (go == null) { Debug.LogWarning($"[Duzeltici] '{ad}' bulunamadı"); continue; }
            go.SetActive(true);
        }

        // BakiyeYukleButon → sol üst, 280x130
        KonumlanDir("BakiyeYukleButon", new Vector2(-710f, 200f), new Vector2(280f, 130f));

        // BonusSatinAlButton → sol orta, 280x130
        KonumlanDir("BonusSatinAlButton", new Vector2(-710f, 50f), new Vector2(280f, 130f));

        // CiftSansKutusu → sol alt, 280x180
        KonumlanDir("CiftSansKutusu", new Vector2(-710f, -120f), new Vector2(280f, 180f));

        // Kaldırılacaklar aktif değilse doğrula
        foreach (var ad in new[] { "TxtHosgeldiniz", "Logo" })
        {
            var go = BulGO(ad);
            if (go != null && go.activeSelf)
            {
                go.SetActive(false);
                Debug.Log($"[Duzeltici] {ad} deaktive edildi");
            }
        }

        SahneyiKaydet("Adım 6: Sol panel manipülasyon grubu konumlandırıldı.");
    }

    [MenuItem("Kumar/Sahne 06 – 7) Logo Sol Üst Köşe")]
    static void Adim7_LogoSolUst()
    {
        var logo = BulGO("Logo");
        if (logo == null) { Debug.LogError("[Duzeltici] Logo bulunamadi!"); return; }

        logo.SetActive(true);

        var rt = logo.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(0f, 1f);
            rt.pivot            = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(40f, -40f);
            rt.sizeDelta        = new Vector2(220f, 140f);
        }

        // PreserveAspect — Image component varsa
        var img = logo.GetComponent<UnityEngine.UI.Image>();
        if (img != null) img.preserveAspect = true;

        Debug.Log("[Duzeltici] Logo sol ust kose: (0,1) anchor, (40,-40) pos, 220x140");
        SahneyiKaydet("Adim 7: Logo sol ust koseye tasindi.");
    }

    [MenuItem("Kumar/Sahne 06 – 9) Hoşgeldin Yazısı Geri")]
    static void Adim9_HosgeldinYazisi()
    {
        var go = BulGO("TxtHosgeldiniz");
        if (go == null) { Debug.LogError("[Duzeltici] TxtHosgeldiniz bulunamadi!"); return; }

        go.SetActive(true);

        // Transform — Logo (220x140 at 40,-40) altına: 40,-200
        var rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(0f, 1f);
            rt.pivot            = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(40f, -200f);
            rt.sizeDelta        = new Vector2(280f, 55f);
        }

        // TMP stil — altın renk, 28px, bold, sol hizalama
        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.fontSize  = 28f;
            tmp.color     = new Color(0.9804f, 0.7804f, 0.4588f, 1f); // #FAC775
            tmp.fontStyle = TMPro.FontStyles.Bold;
            tmp.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
        }

        // Dinamik kullanıcı adı yaz (runtime'da OyunYoneticisi zaten günceller,
        // burada fallback olarak set et)
        if (tmp != null)
        {
            string ad = KullaniciVerileri.KullaniciAdi;
            if (string.IsNullOrWhiteSpace(ad)) ad = "Misafir";
            tmp.text = "Hosgeldin,\n" + ad;
        }

        Debug.Log("[Duzeltici] TxtHosgeldiniz: logo alti (40,-200), 280x55, altin, 28px bold");
        SahneyiKaydet("Adim 9: Hosgeldin yazisi geri getirildi.");
    }

    [MenuItem("Kumar/Sahne 06 – TÜMÜNÜ ÇALIŞTIR")]
    static void TumAdimlar()
    {
        Adim1_GereksizleriKaldir();
        Adim6_SolPanel();           // önce sol panel (1'in üzerine yazar)
        Adim2_SpinButonu();
        Adim3_AyarlarButonu();
        Adim4_Tipografi();
        Adim5_Vignette();
        Adim7_LogoSolUst();
        Adim9_HosgeldinYazisi();
        Debug.Log("[Duzeltici] Tum adimlar tamamlandi.");
    }

    // ─────────────────────────────────────────────
    // Yardımcı metodlar
    // ─────────────────────────────────────────────

    static GameObject BulGO(string ad)
    {
        // Aktif ve inaktif dahil tüm GO'larda ara
        var tüm = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var go in tüm)
        {
            if (go.name == ad && go.scene.IsValid())
                return go;
        }
        return null;
    }

    static void DüzeltTMP(string goAdi, float fontSize, Color renk, bool disableGlow = false)
    {
        var go = BulGO(goAdi);
        if (go == null) { Debug.LogWarning($"[Duzeltici] '{goAdi}' bulunamadı"); return; }

        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp == null) tmp = go.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp == null) { Debug.LogWarning($"[Duzeltici] '{goAdi}' TMP yok"); return; }

        tmp.fontSize = fontSize;
        tmp.color = renk;
        if (disableGlow)
        {
            // Glow/outline sıfırla
            tmp.outlineWidth = 0f;
            tmp.outlineColor = new Color(0, 0, 0, 0);
        }
        Debug.Log($"[Duzeltici] TMP güncellendi: {goAdi} → {fontSize}px");
    }

    static void KonumlanDir(string goAdi, Vector2 pos, Vector2 boyut)
    {
        var go = BulGO(goAdi);
        if (go == null) { Debug.LogWarning($"[Duzeltici] KonumlanDir: '{goAdi}' bulunamadı"); return; }
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchoredPosition = pos;
        rt.sizeDelta = boyut;
        Debug.Log($"[Duzeltici] {goAdi} → pos{pos} size{boyut}");
    }

    static void SahneyiKaydet(string mesaj)
    {
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[Duzeltici] ✓ {mesaj} — Sahne Kaydet (Ctrl+S) unutma!");
    }
}
