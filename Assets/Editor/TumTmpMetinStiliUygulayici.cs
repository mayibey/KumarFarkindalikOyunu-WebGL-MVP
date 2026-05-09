using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Projedeki tum TextMeshProUGUI bilesenlerine toplu stil uygular.
/// Sahne ve prefab dosyalarini tarar, degisiklikleri kaydeder.
/// </summary>
public static class TumTmpMetinStiliUygulayici
{
    static readonly Color32 NormalTop = HexToColor("#FFF3A0");
    static readonly Color32 NormalBottom = HexToColor("#FF8A00");
    static readonly Color32 NormalOutline = HexToColor("#5A2E00");
    static readonly Color32 NormalGlow = HexToColor("#FF9A00");

    static readonly Color32 AcikTop = HexToColor("#A8FF00");
    static readonly Color32 AcikBottom = HexToColor("#00C853");
    static readonly Color32 AcikOutline = HexToColor("#003300");
    static readonly Color32 AcikGlow = HexToColor("#00FF00");

    const float OutlineWidth = 0.25f;
    const float UnderlayDilate = 0.3f;
    const float UnderlaySoftness = 0.5f;
    static readonly Vector2 ShadowOffset = new Vector2(2f, -2f);
    static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.5f);

    [MenuItem("Tools/TMP/Tum TextMeshProUGUI Stil Uygula ve Kaydet")]
    public static void TumProjedeUygula()
    {
        try
        {
            UygulaTumPrefablar();
            UygulaTumSahneler();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[TMP Stil] Tum scene/prefab TextMeshProUGUI stilleri guncellendi ve kaydedildi.");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    static void UygulaTumPrefablar()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            EditorUtility.DisplayProgressBar("TMP Stil", $"Prefab: {path}", (float)i / Mathf.Max(1, prefabGuids.Length));

            GameObject kok = PrefabUtility.LoadPrefabContents(path);
            try
            {
                bool degisti = UygulaObjedekiTumTmp(kok);
                if (degisti)
                    PrefabUtility.SaveAsPrefabAsset(kok, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(kok);
            }
        }
    }

    static void UygulaTumSahneler()
    {
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
        var oncekiKurulum = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            for (int i = 0; i < sceneGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
                EditorUtility.DisplayProgressBar("TMP Stil", $"Scene: {path}", (float)i / Mathf.Max(1, sceneGuids.Length));

                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                bool degisti = false;
                var kokler = scene.GetRootGameObjects();
                for (int r = 0; r < kokler.Length; r++)
                    degisti |= UygulaObjedekiTumTmp(kokler[r]);

                if (degisti)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }
        }
        finally
        {
            if (oncekiKurulum != null && oncekiKurulum.Length > 0)
                EditorSceneManager.RestoreSceneManagerSetup(oncekiKurulum);
        }
    }

    static bool UygulaObjedekiTumTmp(GameObject kok)
    {
        if (kok == null)
            return false;

        bool degisti = false;
        var tumMetinler = kok.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < tumMetinler.Length; i++)
            degisti |= UygulaTekMetinStili(tumMetinler[i]);
        return degisti;
    }

    static bool UygulaTekMetinStili(TextMeshProUGUI tmp)
    {
        if (tmp == null)
            return false;

        Undo.RecordObject(tmp, "TMP Stil Uygula");
        bool degisti = false;

        if (!tmp.fontStyle.HasFlag(FontStyles.Bold))
        {
            tmp.fontStyle |= FontStyles.Bold;
            degisti = true;
        }

        if (tmp.font == null)
        {
            TMP_FontAsset fallback = TMP_Settings.defaultFontAsset;
            if (fallback != null)
            {
                tmp.font = fallback;
                degisti = true;
            }
        }

        if (!tmp.enableVertexGradient)
        {
            tmp.enableVertexGradient = true;
            degisti = true;
        }

        bool acikMetni = AcikMetniMi(tmp.text);
        VertexGradient hedefGradient = acikMetni
            ? new VertexGradient(AcikTop, AcikTop, AcikBottom, AcikBottom)
            : new VertexGradient(NormalTop, NormalTop, NormalBottom, NormalBottom);

        if (!AyniGradient(tmp.colorGradient, hedefGradient))
        {
            tmp.colorGradient = hedefGradient;
            degisti = true;
        }

        if (tmp.alignment != TextAlignmentOptions.Center)
        {
            tmp.alignment = TextAlignmentOptions.Center;
            degisti = true;
        }

        float yeniBoyut = Mathf.Max(60f, tmp.fontSize);
        if (!Mathf.Approximately(tmp.fontSize, yeniBoyut))
        {
            tmp.fontSize = yeniBoyut;
            degisti = true;
        }

        if (!tmp.extraPadding)
        {
            tmp.extraPadding = true;
            degisti = true;
        }

        Material mat = tmp.fontSharedMaterial;
        if (mat != null)
        {
            Undo.RecordObject(mat, "TMP Material Stil Uygula");
            Shader hedefShader = Shader.Find("TextMeshPro/Distance Field");
            if (hedefShader != null && mat.shader != hedefShader)
            {
                mat.shader = hedefShader;
                degisti = true;
            }

            Color outline = acikMetni ? AcikOutline : NormalOutline;
            Color glow = acikMetni ? AcikGlow : NormalGlow;
            degisti |= MatDegerYaz(mat, ShaderUtilities.ID_OutlineWidth, OutlineWidth);
            degisti |= MatDegerYaz(mat, ShaderUtilities.ID_OutlineColor, outline);
            degisti |= MatDegerYaz(mat, ShaderUtilities.ID_UnderlayColor, glow);
            degisti |= MatDegerYaz(mat, ShaderUtilities.ID_UnderlayOffsetX, 0f);
            degisti |= MatDegerYaz(mat, ShaderUtilities.ID_UnderlayOffsetY, 0f);
            degisti |= MatDegerYaz(mat, ShaderUtilities.ID_UnderlayDilate, UnderlayDilate);
            degisti |= MatDegerYaz(mat, ShaderUtilities.ID_UnderlaySoftness, UnderlaySoftness);

            mat.EnableKeyword("UNDERLAY_ON");
            EditorUtility.SetDirty(mat);
        }

        Shadow shadow = tmp.GetComponent<Shadow>();
        if (shadow == null)
            shadow = tmp.gameObject.AddComponent<Shadow>();
        Undo.RecordObject(shadow, "TMP Shadow Uygula");
        if (shadow.effectColor != ShadowColor)
        {
            shadow.effectColor = ShadowColor;
            degisti = true;
        }
        if (shadow.effectDistance != ShadowOffset)
        {
            shadow.effectDistance = ShadowOffset;
            degisti = true;
        }
        if (shadow.useGraphicAlpha)
        {
            shadow.useGraphicAlpha = false;
            degisti = true;
        }

        if (degisti)
        {
            EditorUtility.SetDirty(tmp);
            EditorUtility.SetDirty(tmp.gameObject);
            tmp.SetAllDirty();
        }
        return degisti;
    }

    static bool MatDegerYaz(Material mat, int id, float deger)
    {
        if (mat == null || !mat.HasProperty(id))
            return false;
        float onceki = mat.GetFloat(id);
        if (Mathf.Approximately(onceki, deger))
            return false;
        mat.SetFloat(id, deger);
        return true;
    }

    static bool MatDegerYaz(Material mat, int id, Color deger)
    {
        if (mat == null || !mat.HasProperty(id))
            return false;
        Color onceki = mat.GetColor(id);
        if (onceki == deger)
            return false;
        mat.SetColor(id, deger);
        return true;
    }

    static bool AcikMetniMi(string metin)
    {
        if (string.IsNullOrEmpty(metin))
            return false;
        string t = metin.Trim();
        return string.Equals(t, "AÇIK", StringComparison.OrdinalIgnoreCase)
               || string.Equals(t, "ACIK", StringComparison.OrdinalIgnoreCase);
    }

    static bool AyniGradient(VertexGradient a, VertexGradient b)
    {
        return a.topLeft == b.topLeft
               && a.topRight == b.topRight
               && a.bottomLeft == b.bottomLeft
               && a.bottomRight == b.bottomRight;
    }

    static Color32 HexToColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out var color))
            return color;
        return Color.white;
    }
}
