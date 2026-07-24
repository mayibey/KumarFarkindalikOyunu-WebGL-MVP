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
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no, viewport-fit=cover"">
    <style>
      html, body {
        width: 100%;
        height: 100%;
        margin: 0;
        padding: 0;
        overflow: hidden;
        touch-action: none; /* pinch zoom = iOS bellek cokmesi */
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
    <script>
      /* PINCH_KILIT: iOS'ta TARAYICI pinch'i bellegi patlatip sekmeyi cokertiyor — kapali.
         Yerine OYUN_ZOOM guvenli CSS zoom saglar. */
      (function(){
        ['gesturestart','gesturechange','gestureend'].forEach(function(t){
          document.addEventListener(t, function(e){ e.preventDefault(); }, {passive:false});
        });
        document.addEventListener('touchmove', function(e){
          if(e.touches && e.touches.length>1) e.preventDefault();
        }, {passive:false});
      })();
      /* OYUN_ZOOM: 2 parmak pinch = CSS transform zoom (bellek guvenli); cift dokunus 2x/sifir. */
      (function(){
        if(!/iPhone|iPad|iPod|Android/i.test(navigator.userAgent)) return;
        var zk=1, zx=0, zy=0;
        function uygula(){
          document.body.style.transformOrigin='0 0';
          document.body.style.transform = (zk<=1.01) ? '' : 'translate('+zx+'px,'+zy+'px) scale('+zk+')';
        }
        function sinirla(){
          zk=Math.min(3,Math.max(1,zk));
          var w=window.innerWidth,h=window.innerHeight;
          zx=Math.min(0,Math.max(w-w*zk,zx));
          zy=Math.min(0,Math.max(h-h*zk,zy));
          if(zk<=1.01){zk=1;zx=0;zy=0;}
        }
        var p=null;
        document.addEventListener('touchstart',function(e){
          if(e.touches.length===2){
            var a=e.touches[0],b=e.touches[1];
            p={ d0:Math.hypot(a.clientX-b.clientX,a.clientY-b.clientY)||1, k0:zk,
                mx:(a.clientX+b.clientX)/2, my:(a.clientY+b.clientY)/2, x0:zx, y0:zy };
          }
        },{passive:false});
        document.addEventListener('touchmove',function(e){
          if(p && e.touches.length===2){
            e.preventDefault();
            var a=e.touches[0],b=e.touches[1];
            var d=Math.hypot(a.clientX-b.clientX,a.clientY-b.clientY)||1;
            var mx=(a.clientX+b.clientX)/2, my=(a.clientY+b.clientY)/2;
            var k=Math.min(3,Math.max(1,p.k0*d/p.d0));
            zx = mx - (p.mx-p.x0)*(k/p.k0);
            zy = my - (p.my-p.y0)*(k/p.k0);
            zk = k;
            sinirla(); uygula();
          }
        },{passive:false});
        document.addEventListener('touchend',function(e){ if(e.touches.length<2) p=null; });
        var sonT=0, sonX=0, sonY=0;
        document.addEventListener('touchend',function(e){
          if(e.touches.length===0 && e.changedTouches.length===1){
            var t=e.changedTouches[0], s=Date.now();
            if(s-sonT<350 && Math.abs(t.clientX-sonX)<40 && Math.abs(t.clientY-sonY)<40){
              if(zk>1){ zk=1; } else { zk=2; zx=-t.clientX; zy=-t.clientY; }
              sinirla(); uygula();
            }
            sonT=s; sonX=t.clientX; sonY=t.clientY;
          }
        });
      })();
    </script>
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

        // Mobil: 3x retina framebuffer WebGL belleğini patlatıp iOS sekmesini çökertiyor;
        // Unity config'ine telefonda 1x çizim ayarı eklenir (masaüstü etkilenmez).
        const string dprSatiri = "        devicePixelRatio: (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent) ? 0.75 : window.devicePixelRatio),";
        if (!html.Contains("devicePixelRatio:") && html.Contains("showBanner: unityShowBanner,"))
        {
            html = html.Replace(
                "showBanner: unityShowBanner,",
                "showBanner: unityShowBanner,\r\n" + dprSatiri);
        }

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
