using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;

/// <summary>
/// Her WebGL build sonrası index.html içinde Unity'nin yazdığı 960x600 küçük kutu davranışını kaldırır;
/// tam ekran viewport/CSS ekler. Deploy öncesi elle index düzeltmeye gerek kalmaz.
/// </summary>
public static class WebGlIndexTamEkranPostProcess
{
    const string YamaIsareti = "WEBGL_FULLSCREEN_PATCH";

    static readonly string HeadEklenti = @"
    <!-- " + YamaIsareti + @" -->
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0, viewport-fit=cover"">
    <style>
      html, body {
        width: 100%;
        height: 100%;
        margin: 0;
        padding: 0;
        overflow: hidden;
        box-sizing: border-box;
      }
      *, *::before, *::after { box-sizing: inherit; }
      #unity-container.unity-desktop {
        position: fixed !important;
        left: 0 !important;
        top: 0 !important;
        width: 100vw !important;
        height: 100vh !important;
        max-width: none !important;
        max-height: none !important;
        transform: none !important;
        margin: 0 !important;
        display: flex !important;
        flex-direction: column !important;
      }
      #unity-container.unity-desktop #unity-canvas {
        flex: 1 1 0;
        align-self: stretch;
        width: 100% !important;
        height: 100% !important;
        min-height: 0;
        display: block;
      }
      #unity-container.unity-desktop #unity-footer {
        flex: 0 0 auto;
      }
    </style>
";

    [PostProcessBuild(2000)]
    public static void WebGlBuildSonrasi(BuildTarget hedef, string ciktiKlasoru)
    {
        if (hedef != BuildTarget.WebGL)
            return;

        string indexYolu = Path.Combine(ciktiKlasoru, "index.html");
        if (!File.Exists(indexYolu))
            return;

        string html = File.ReadAllText(indexYolu);

        if (!html.Contains(YamaIsareti))
        {
            const string stilBaglanti = "<link rel=\"stylesheet\" href=\"TemplateData/style.css\">";
            if (html.Contains(stilBaglanti))
                html = html.Replace(stilBaglanti, stilBaglanti + HeadEklenti);
            else
                UnityEngine.Debug.LogWarning("[WebGlIndexTamEkranPostProcess] style.css linki bulunamadı; head yaması atlandı.");
        }

        // Canvas: width/height attribute kaldır
        html = Regex.Replace(
            html,
            @"<canvas\s+id=""unity-canvas""[^>]*>",
            m =>
            {
                string ic = m.Value;
                ic = Regex.Replace(ic, @"\s+width\s*=\s*\d+", "", RegexOptions.IgnoreCase);
                ic = Regex.Replace(ic, @"\s+width\s*=\s*""\d+""", "", RegexOptions.IgnoreCase);
                ic = Regex.Replace(ic, @"\s+height\s*=\s*\d+", "", RegexOptions.IgnoreCase);
                ic = Regex.Replace(ic, @"\s+height\s*=\s*""\d+""", "", RegexOptions.IgnoreCase);
                if (ic.IndexOf("tabindex", System.StringComparison.OrdinalIgnoreCase) < 0)
                    ic = ic.Replace(">", " tabindex=\"-1\">");
                return ic;
            },
            RegexOptions.IgnoreCase);

        // Masaüstü: Unity varsayılan 960x600 else bloğu
        html = Regex.Replace(
            html,
            @"\}\s*else\s*\{\s*\r?\n\s*//\s*Desktop style:[^\r\n]*\r?\n\s*canvas\.style\.width\s*=\s*""960px""\s*;\r?\n\s*canvas\.style\.height\s*=\s*""600px""\s*;\r?\n\s*\}",
            "} else { /* tam ekran: canvas CSS */ }",
            RegexOptions.Multiline);

        // Kalan tek satır atamalar (farklı şablon)
        html = Regex.Replace(
            html,
            @"canvas\.style\.width\s*=\s*""960px""\s*;\s*\r?\n\s*canvas\.style\.height\s*=\s*""600px""\s*;",
            "",
            RegexOptions.Multiline);

        // Mobil: ikinci viewport meta oluşturan blok kaldır (viewport head'de)
        html = Regex.Replace(
            html,
            @"var\s+meta\s*=\s*document\.createElement\('meta'\);\s*meta\.name\s*=\s*'viewport';\s*meta\.content\s*=\s*'[^']+';\s*document\.getElementsByTagName\('head'\)\[0\]\.appendChild\(meta\);\s*",
            "",
            RegexOptions.Singleline);

        File.WriteAllText(indexYolu, html);
        UnityEngine.Debug.Log("[WebGlIndexTamEkranPostProcess] index.html tam ekran yaması uygulandı: " + indexYolu);
    }
}
