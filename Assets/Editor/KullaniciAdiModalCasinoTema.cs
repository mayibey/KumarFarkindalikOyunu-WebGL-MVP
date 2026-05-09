using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 01_GirisScene içindeki KullaniciAdiModalRoot modaline casino altın+siyah temasını uygular.
/// "Sana bu isimle sesleneceğiz" yazısını siler, layout boşluklarını yeniden düzenler.
/// Menü: "Kumar/Giris/KullaniciAdiModal Casino Tema".
/// </summary>
public static class KullaniciAdiModalCasinoTema
{
    const string SahneYolu = "Assets/Scenes/01_GirisScene.unity";

    static readonly Color ALTIN          = new Color(0.831f, 0.659f, 0.341f, 1f);   // #d4a857
    static readonly Color ALTIN_PARLAK   = new Color(0.957f, 0.839f, 0.471f, 1f);   // #f4d678
    static readonly Color ALTIN_HOVER    = new Color(0.910f, 0.725f, 0.416f, 1f);   // #e8b96a
    static readonly Color ALTIN_PRESSED  = new Color(0.710f, 0.557f, 0.239f, 1f);   // #b58e3d
    static readonly Color ALTIN_MAT      = new Color(0.722f, 0.659f, 0.459f, 1f);   // #b8a875
    static readonly Color ALTIN_SOLUK    = new Color(0.353f, 0.314f, 0.251f, 1f);   // #5a5040
    static readonly Color SIYAH_DERIN    = new Color(0.039f, 0.039f, 0.039f, 1f);   // #0a0a0a
    static readonly Color KAHVE_KOYU     = new Color(0.102f, 0.078f, 0.063f, 1f);   // #1a1410
    static readonly Color GLOW_ALTIN     = new Color(0.957f, 0.839f, 0.471f, 0.3f); // #f4d678 alpha 0.3
    static readonly Color BORDER_ALTIN_2 = new Color(0.957f, 0.839f, 0.471f, 0.4f); // alpha 0.4
    static readonly Color BORDER_ALTIN_3 = new Color(0.957f, 0.839f, 0.471f, 0.3f); // alpha 0.3
    static readonly Color MISAFIR_BG     = new Color(0.078f, 0.078f, 0.078f, 0.6f); // rgba(20,20,20,0.6)
    static readonly Color KARARATMA      = new Color(0f, 0f, 0f, 0.85f);

    [MenuItem("Kumar/Giris/KullaniciAdiModal Casino Tema")]
    public static void Calistir()
    {
        // Aktif sahne dirty ise sor
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[KullaniciAdiModalCasinoTema] İptal edildi.");
                return;
            }
        }

        var sahne = EditorSceneManager.OpenScene(SahneYolu, OpenSceneMode.Single);
        if (!sahne.IsValid())
        {
            EditorUtility.DisplayDialog("Sahne yok", $"{SahneYolu} açılamadı.", "Tamam");
            return;
        }

        var rapor = new System.Text.StringBuilder();

        // KullaniciAdiModalRoot bul
        Transform root = null;
        foreach (var k in sahne.GetRootGameObjects())
        {
            foreach (var t in k.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "KullaniciAdiModalRoot") { root = t; break; }
            }
            if (root != null) break;
        }
        if (root == null)
        {
            EditorUtility.DisplayDialog("Modal yok",
                "KullaniciAdiModalRoot bulunamadı. Önce 'KullaniciAdiModal Overlay Oluştur' menüsünü çalıştır.",
                "Tamam");
            return;
        }

        var modalPanel = root.Find("ModalPanel");
        if (modalPanel == null)
        {
            EditorUtility.DisplayDialog("ModalPanel yok",
                "KullaniciAdiModalRoot altında ModalPanel bulunamadı.", "Tamam");
            return;
        }

        // 1) AltAciklama sil
        var altAciklama = BulCocuk(modalPanel, "AltAciklama");
        if (altAciklama != null)
        {
            Object.DestroyImmediate(altAciklama.gameObject);
            rapor.AppendLine("AltAciklama silindi.");
        }
        else
        {
            rapor.AppendLine("AltAciklama zaten yoktu.");
        }

        // 2) ModalPanel — altın çerçeve
        var modalImg = modalPanel.GetComponent<Image>();
        if (modalImg != null) { modalImg.color = ALTIN; rapor.AppendLine("ModalPanel arka plan: altın #d4a857."); }
        var modalOutline = modalPanel.GetComponent<Outline>();
        if (modalOutline == null) modalOutline = modalPanel.gameObject.AddComponent<Outline>();
        modalOutline.effectColor = GLOW_ALTIN;
        modalOutline.effectDistance = new Vector2(2f, -2f);
        rapor.AppendLine("ModalPanel outline glow eklendi (#f4d678 alpha 0.3).");

        // 3) IcZemin — koyu siyah
        var icZemin = BulCocuk(modalPanel, "IcZemin");
        if (icZemin != null)
        {
            var icImg = icZemin.GetComponent<Image>();
            if (icImg != null) { icImg.color = SIYAH_DERIN; rapor.AppendLine("IcZemin: #0a0a0a."); }
        }

        // 4) Baslik — parlak altın
        var baslik = BulCocuk(modalPanel, "Baslik");
        if (baslik != null)
        {
            var baslikTmp = baslik.GetComponent<TextMeshProUGUI>();
            if (baslikTmp != null) { baslikTmp.color = ALTIN_PARLAK; baslikTmp.fontStyle = FontStyles.Bold; rapor.AppendLine("Baslik: #f4d678 bold."); }
        }

        // 5) IsimInput — koyu kahve + altın yazı + altın outline
        var isimInput = BulCocuk(modalPanel, "IsimInput");
        if (isimInput != null)
        {
            var inputImg = isimInput.GetComponent<Image>();
            if (inputImg != null) inputImg.color = KAHVE_KOYU;

            var inputOutline = isimInput.GetComponent<Outline>();
            if (inputOutline == null) inputOutline = isimInput.gameObject.AddComponent<Outline>();
            inputOutline.effectColor = BORDER_ALTIN_3;
            inputOutline.effectDistance = new Vector2(1f, -1f);

            var tmpInput = isimInput.GetComponent<TMP_InputField>();
            if (tmpInput != null)
            {
                tmpInput.caretColor = ALTIN;
                if (tmpInput.textComponent != null) tmpInput.textComponent.color = ALTIN_PARLAK;
                if (tmpInput.placeholder is TextMeshProUGUI ph) ph.color = ALTIN_SOLUK;
            }
            rapor.AppendLine("IsimInput: koyu kahve bg + altın çerçeve + altın yazı/caret, placeholder #5a5040.");
        }

        // 6) BaslaButton — altın gradient simulasyonu (Color Tint)
        var basla = BulCocuk(modalPanel, "BaslaButton");
        if (basla != null)
        {
            var baslaImg = basla.GetComponent<Image>();
            if (baslaImg != null) baslaImg.color = ALTIN;
            var baslaBtn = basla.GetComponent<Button>();
            if (baslaBtn != null)
            {
                baslaBtn.transition = Selectable.Transition.ColorTint;
                var cb = baslaBtn.colors;
                cb.normalColor = ALTIN;
                cb.highlightedColor = ALTIN_HOVER;
                cb.pressedColor = ALTIN_PRESSED;
                cb.disabledColor = ALTIN_SOLUK;
                cb.selectedColor = ALTIN_HOVER;
                cb.colorMultiplier = 1f;
                baslaBtn.colors = cb;
            }
            // İçindeki TMP "BAŞLA" → koyu siyah, bold
            foreach (var tmp in basla.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                tmp.color = SIYAH_DERIN;
                tmp.fontStyle = FontStyles.Bold;
            }
            rapor.AppendLine("BaslaButton: altın bg + tint, TMP siyah bold.");
        }

        // 7) MisafirButton — yarı şeffaf koyu + altın kenarlık + altın mat yazı
        var misafir = BulCocuk(modalPanel, "MisafirButton");
        if (misafir != null)
        {
            var mImg = misafir.GetComponent<Image>();
            if (mImg != null) mImg.color = MISAFIR_BG;

            var mOutline = misafir.GetComponent<Outline>();
            if (mOutline == null) mOutline = misafir.gameObject.AddComponent<Outline>();
            mOutline.effectColor = BORDER_ALTIN_2;
            mOutline.effectDistance = new Vector2(1f, -1f);

            var mBtn = misafir.GetComponent<Button>();
            if (mBtn != null)
            {
                mBtn.transition = Selectable.Transition.ColorTint;
                var cb = mBtn.colors;
                cb.normalColor = Color.white;
                cb.highlightedColor = new Color(1f, 0.93f, 0.71f, 1f);  // light gold tint
                cb.pressedColor    = new Color(0.957f, 0.839f, 0.471f, 1f);
                cb.disabledColor   = new Color(0.5f, 0.5f, 0.5f, 1f);
                cb.colorMultiplier = 1f;
                mBtn.colors = cb;
            }
            foreach (var tmp in misafir.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                tmp.color = ALTIN_MAT;
            }
            rapor.AppendLine("MisafirButton: koyu yarı şeffaf bg + altın outline, TMP #b8a875.");
        }

        // 8) KararatmaOverlay — daha koyu (alpha 0.85)
        var kararatma = root.Find("KararatmaOverlay");
        if (kararatma != null)
        {
            var kImg = kararatma.GetComponent<Image>();
            if (kImg != null) { kImg.color = KARARATMA; rapor.AppendLine("KararatmaOverlay: alpha 0.85."); }
        }

        // 9) Layout boşlukları — Baslik 60px alt, Input 30-40, Buton 20-25, Misafir 12
        // ModalPanel 600x400, başlık üst, input merkez, butonlar alt
        // RectTransform anchoredPosition ayarları (mevcut overlay create script'i ile uyumlu pivot)
        if (baslik != null)
        {
            var rt = (RectTransform)baslik.transform;
            rt.anchoredPosition = new Vector2(0f, -45f); // başlık ile üst kenar arası 45 px
        }
        if (isimInput != null)
        {
            var rt = (RectTransform)isimInput.transform;
            rt.anchoredPosition = new Vector2(0f, 30f);  // merkezde, hafif yukarı
        }
        if (basla != null)
        {
            var rt = (RectTransform)basla.transform;
            rt.anchoredPosition = new Vector2(0f, -55f); // input altı 25 px civarı
        }
        if (misafir != null)
        {
            var rt = (RectTransform)misafir.transform;
            rt.anchoredPosition = new Vector2(0f, -130f); // başla altı 12 px civarı (60+12+58 ~ 130)
        }
        rapor.AppendLine("Layout boşlukları yeniden düzenlendi (Başlık-Input-Başla-Misafir).");

        // 10) Sahneyi kaydet
        EditorSceneManager.MarkSceneDirty(sahne);
        bool kayit = EditorSceneManager.SaveScene(sahne);
        rapor.AppendLine();
        rapor.AppendLine(kayit ? "Sahne kaydedildi." : "UYARI: Sahne kaydedilemedi.");

        Debug.Log("[KullaniciAdiModalCasinoTema] " + rapor.ToString().Replace("\n", " | "));
        EditorUtility.DisplayDialog("Casino Tema Uygulandı", rapor.ToString(), "Tamam");
    }

    static Transform BulCocuk(Transform parent, string ad)
    {
        foreach (var t in parent.GetComponentsInChildren<Transform>(true))
        {
            if (t == parent) continue;
            if (t.name == ad) return t;
        }
        return null;
    }
}
