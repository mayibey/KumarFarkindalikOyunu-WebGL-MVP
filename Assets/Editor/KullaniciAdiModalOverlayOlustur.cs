using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 01_GirisScene içinde GirisArayuzu altında KullaniciAdiModalRoot overlay'ini sıfırdan kurar.
/// Eski varsa silip yeniden oluşturur, OyunaBaslaButton'un kullaniciAdiModalRoot field'ını bağlar.
/// Menü: "Kumar/Giris/KullaniciAdiModal Overlay Oluştur".
/// </summary>
public static class KullaniciAdiModalOverlayOlustur
{
    [MenuItem("Kumar/Giris/KullaniciAdiModal Overlay Oluştur")]
    public static void Calistir()
    {
        var sahne = SceneManager.GetActiveScene();
        if (!sahne.IsValid() || !sahne.isLoaded)
        {
            EditorUtility.DisplayDialog("Sahne yüklü değil",
                "Önce 01_GirisScene'i aç.", "Tamam");
            return;
        }

        Transform girisArayuzu = null;
        foreach (var k in sahne.GetRootGameObjects())
        {
            foreach (var t in k.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "GirisArayuzu") { girisArayuzu = t; break; }
            }
            if (girisArayuzu != null) break;
        }

        if (girisArayuzu == null)
        {
            EditorUtility.DisplayDialog("GirisArayuzu yok",
                "Sahnede 'GirisArayuzu' Canvas bulunamadı.", "Tamam");
            return;
        }

        // Eski modal varsa kaldır
        bool eskiVardi = false;
        var eski = girisArayuzu.Find("KullaniciAdiModalRoot");
        if (eski != null)
        {
            eskiVardi = true;
            Object.DestroyImmediate(eski.gameObject);
        }

        // Modal root
        var rootGo = new GameObject("KullaniciAdiModalRoot", typeof(RectTransform));
        rootGo.transform.SetParent(girisArayuzu, false);
        var rootRt = (RectTransform)rootGo.transform;
        StretchTumu(rootRt);

        // Karartma
        var karat = OlusturImage("KararatmaOverlay", rootRt);
        StretchTumu((RectTransform)karat.transform);
        var karatImg = karat.GetComponent<Image>();
        karatImg.color = new Color(0f, 0f, 0f, 0.7f);
        karatImg.raycastTarget = true;

        // Modal Panel (altın çerçeve dış)
        var modal = OlusturImage("ModalPanel", rootRt);
        var modalRt = (RectTransform)modal.transform;
        modalRt.anchorMin = modalRt.anchorMax = modalRt.pivot = new Vector2(0.5f, 0.5f);
        modalRt.sizeDelta = new Vector2(600, 400);
        modalRt.anchoredPosition = Vector2.zero;
        var modalImg = modal.GetComponent<Image>();
        modalImg.color = Hex("d4a857");
        modalImg.raycastTarget = true;
        var modalCG = modal.AddComponent<CanvasGroup>();
        modalCG.alpha = 1f; // runtime'da Awake hemen 0 yapacak

        // İç koyu zemin
        var icZemin = OlusturImage("IcZemin", modalRt);
        var icRt = (RectTransform)icZemin.transform;
        icRt.anchorMin = Vector2.zero; icRt.anchorMax = Vector2.one;
        icRt.offsetMin = new Vector2(4, 4); icRt.offsetMax = new Vector2(-4, -4);
        var icImg = icZemin.GetComponent<Image>();
        icImg.color = Hex("1a1410");
        icImg.raycastTarget = false;

        // Başlık
        var baslik = OlusturText("Baslik", modalRt, "Adın", 56, Hex("f4d678"), TextAlignmentOptions.Center, FontStyles.Bold);
        var baslikRt = (RectTransform)baslik.transform;
        baslikRt.anchorMin = baslikRt.anchorMax = new Vector2(0.5f, 1f);
        baslikRt.pivot = new Vector2(0.5f, 1f);
        baslikRt.sizeDelta = new Vector2(560, 80);
        baslikRt.anchoredPosition = new Vector2(0, -25);

        // Alt açıklama
        var aciklama = OlusturText("AltAciklama", modalRt, "Sana bu isimle sesleneceğiz.", 22, Hex("aaa9a5"), TextAlignmentOptions.Center, FontStyles.Normal);
        var aciklamaRt = (RectTransform)aciklama.transform;
        aciklamaRt.anchorMin = aciklamaRt.anchorMax = new Vector2(0.5f, 1f);
        aciklamaRt.pivot = new Vector2(0.5f, 1f);
        aciklamaRt.sizeDelta = new Vector2(560, 30);
        aciklamaRt.anchoredPosition = new Vector2(0, -115);

        // Input
        var input = OlusturIsimInput("IsimInput", modalRt);
        var inputRt = (RectTransform)input.transform;
        inputRt.anchorMin = inputRt.anchorMax = inputRt.pivot = new Vector2(0.5f, 0.5f);
        inputRt.sizeDelta = new Vector2(400, 60);
        inputRt.anchoredPosition = new Vector2(0, 30);
        var tmpInput = input.GetComponent<TMP_InputField>();

        // Başla
        var basla = OlusturButton("BaslaButton", modalRt, "BAŞLA", 32, Color.white, Hex("d4a857"), 280, 70, true);
        var baslaRt = (RectTransform)basla.transform;
        baslaRt.anchorMin = baslaRt.anchorMax = baslaRt.pivot = new Vector2(0.5f, 0.5f);
        baslaRt.anchoredPosition = new Vector2(0, -55);

        // Misafir
        var misafir = OlusturButton("MisafirButton", modalRt, "Misafir", 22, Hex("aaaaaa"), new Color(0.20f, 0.20f, 0.20f, 0.4f), 280, 60, false);
        var misafirRt = (RectTransform)misafir.transform;
        misafirRt.anchorMin = misafirRt.anchorMax = misafirRt.pivot = new Vector2(0.5f, 0.5f);
        misafirRt.anchoredPosition = new Vector2(0, -135);

        // Kontrol script
        var kontrol = modal.AddComponent<KullaniciAdiModalKontrol>();
        var so = new SerializedObject(kontrol);
        so.FindProperty("isimInput").objectReferenceValue = tmpInput;
        so.FindProperty("baslaButton").objectReferenceValue = basla.GetComponent<Button>();
        so.FindProperty("misafirButton").objectReferenceValue = misafir.GetComponent<Button>();
        so.FindProperty("modalCanvasGroup").objectReferenceValue = modalCG;
        so.FindProperty("modalPanel").objectReferenceValue = modalRt;
        so.ApplyModifiedProperties();

        // En sonuncu sibling — render order'da en üstte
        rootGo.transform.SetSiblingIndex(girisArayuzu.childCount - 1);

        // OyunaBaslaButton'un kullaniciAdiModalRoot field'ını bağla
        Transform btnTr = null;
        foreach (var t in girisArayuzu.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "OyunaBaslaButton") { btnTr = t; break; }
        }
        bool buttonBaglandi = false;
        if (btnTr != null)
        {
            var btnComp = btnTr.GetComponent<OyunaBaslaButonu>();
            if (btnComp != null)
            {
                var so2 = new SerializedObject(btnComp);
                var prop = so2.FindProperty("kullaniciAdiModalRoot");
                if (prop != null)
                {
                    prop.objectReferenceValue = rootGo;
                    so2.ApplyModifiedProperties();
                    buttonBaglandi = true;
                }
            }
        }

        // Başlangıçta kapalı
        rootGo.SetActive(false);

        EditorSceneManager.MarkSceneDirty(sahne);
        bool kayit = EditorSceneManager.SaveScene(sahne);

        var rapor = new System.Text.StringBuilder();
        rapor.AppendLine($"KullaniciAdiModalRoot: {(eskiVardi ? "eski silindi, yeniden oluşturuldu" : "yeni oluşturuldu")}");
        rapor.AppendLine($"Sibling index: {rootGo.transform.GetSiblingIndex()} / {girisArayuzu.childCount - 1}");
        rapor.AppendLine($"OyunaBaslaButton.kullaniciAdiModalRoot bağlandı: {(buttonBaglandi ? "EVET" : "HAYIR (button/script yok)")}");
        rapor.AppendLine();
        rapor.AppendLine("Hiyerarşi:");
        rapor.AppendLine("  KullaniciAdiModalRoot (inactive)");
        rapor.AppendLine("    KararatmaOverlay [Image #000 a=0.7, raycastTarget=true]");
        rapor.AppendLine("    ModalPanel [Image #d4a857, CanvasGroup, KullaniciAdiModalKontrol]");
        rapor.AppendLine("      IcZemin [Image #1a1410]");
        rapor.AppendLine("      Baslik [TMP 'Adın' 56pt #f4d678 bold]");
        rapor.AppendLine("      AltAciklama [TMP 22pt #aaa9a5]");
        rapor.AppendLine("      IsimInput [Image #0a0a0a, TMP_InputField]");
        rapor.AppendLine("      BaslaButton [Image #d4a857, Button, TMP 'BAŞLA' 32pt bold]");
        rapor.AppendLine("      MisafirButton [Image #333 a=0.4, Button, TMP 'Misafir' 22pt]");
        rapor.AppendLine();
        rapor.AppendLine(kayit ? "Sahne kaydedildi." : "UYARI: Sahne kaydedilemedi.");
        rapor.AppendLine();
        rapor.AppendLine("Not: TMP yazılar boş görünüyorsa Inspector'dan font asset ata.");

        Debug.Log("[KullaniciAdiModalOverlayOlustur] " + rapor.ToString().Replace("\n", " | "));
        EditorUtility.DisplayDialog("Modal Overlay", rapor.ToString(), "Tamam");
    }

    // --- Helpers ---
    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        return c;
    }

    static GameObject OlusturImage(string ad, Transform parent)
    {
        var go = new GameObject(ad, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        return go;
    }

    static GameObject OlusturText(string ad, Transform parent, string text, float fontSize, Color renk, TextAlignmentOptions align, FontStyles stil)
    {
        var go = new GameObject(ad, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = renk;
        tmp.alignment = align;
        tmp.fontStyle = stil;
        tmp.raycastTarget = false;
        return go;
    }

    static GameObject OlusturIsimInput(string ad, Transform parent)
    {
        var go = new GameObject(ad, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = Hex("0a0a0a");

        var ta = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        ta.transform.SetParent(go.transform, false);
        var taRt = (RectTransform)ta.transform;
        taRt.anchorMin = Vector2.zero; taRt.anchorMax = Vector2.one;
        taRt.offsetMin = new Vector2(10, 6); taRt.offsetMax = new Vector2(-10, -6);

        var ph = new GameObject("Placeholder", typeof(RectTransform));
        ph.transform.SetParent(ta.transform, false);
        var phRt = (RectTransform)ph.transform;
        phRt.anchorMin = Vector2.zero; phRt.anchorMax = Vector2.one;
        phRt.offsetMin = phRt.offsetMax = Vector2.zero;
        var phTmp = ph.AddComponent<TextMeshProUGUI>();
        phTmp.text = "İsmini yaz...";
        phTmp.fontSize = 28;
        phTmp.color = new Color(0.7f, 0.7f, 0.7f, 0.5f);
        phTmp.alignment = TextAlignmentOptions.MidlineLeft;
        phTmp.raycastTarget = false;

        var tx = new GameObject("Text", typeof(RectTransform));
        tx.transform.SetParent(ta.transform, false);
        var txRt = (RectTransform)tx.transform;
        txRt.anchorMin = Vector2.zero; txRt.anchorMax = Vector2.one;
        txRt.offsetMin = txRt.offsetMax = Vector2.zero;
        var txTmp = tx.AddComponent<TextMeshProUGUI>();
        txTmp.text = "";
        txTmp.fontSize = 28;
        txTmp.color = Color.white;
        txTmp.alignment = TextAlignmentOptions.MidlineLeft;
        txTmp.raycastTarget = false;

        var input = go.AddComponent<TMP_InputField>();
        input.targetGraphic = img;
        input.textViewport = taRt;
        input.textComponent = txTmp;
        input.placeholder = phTmp;
        input.pointSize = 28;
        input.characterLimit = 24;
        input.lineType = TMP_InputField.LineType.SingleLine;
        return go;
    }

    static GameObject OlusturButton(string ad, Transform parent, string text, float fontSize, Color textColor, Color bgColor, float w, float h, bool bold)
    {
        var go = new GameObject(ad, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(w, h);
        var img = go.GetComponent<Image>();
        img.color = bgColor;
        var btn = go.GetComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.targetGraphic = img;

        var tmpGo = new GameObject("Text", typeof(RectTransform));
        tmpGo.transform.SetParent(go.transform, false);
        var tmpRt = (RectTransform)tmpGo.transform;
        tmpRt.anchorMin = Vector2.zero; tmpRt.anchorMax = Vector2.one;
        tmpRt.offsetMin = tmpRt.offsetMax = Vector2.zero;
        var tmp = tmpGo.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.raycastTarget = false;
        return go;
    }

    static void StretchTumu(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
