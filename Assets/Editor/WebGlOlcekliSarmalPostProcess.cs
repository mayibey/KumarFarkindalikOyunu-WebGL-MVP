using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;

/// <summary>
/// Her WebGL build sonrası (WebGlIndexTamEkranPostProcess'ten SONRA, order 3000) oyunu sabit
/// 1920x1080 sanal çözünürlükte çalıştıran ölçekleyici sarmalayıcı üretir:
///   1) Yamalı index.html → oyun.html olarak taşınır (oyunun kendisi).
///   2) index.html yerine küçük bir sarmalayıcı yazılır: oyun.html'i 1920x1080 iframe'de açar,
///      pencere hangi boyutta olursa olsun orantılı ölçekler (letterbox, 16:9 korunur).
/// Böylece Unity canvas + DOM enjeksiyonları (anlatıcı şerit, hoşgeldin kutusu, yönetici paneli)
/// tek bir bütün olarak ölçeklenir; dar/geniş ekranlarda taşma-kesilme olmaz.
/// </summary>
public static class WebGlOlcekliSarmalPostProcess
{
    const string SarmalIsareti = "OLCEK_WRAPPER";

    static readonly string SarmalHtml = @"<!DOCTYPE html>
<html lang=""tr"">
<head>
<!-- " + SarmalIsareti + @": oyun.html'i 1920x1080 sanal ekranda orantili olcekler -->
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no, viewport-fit=cover"">
<title>Kumar Kazandırmaz</title>
<link rel=""shortcut icon"" href=""TemplateData/favicon.ico"">
<style>
  html,body{margin:0;padding:0;width:100%;height:100%;overflow:hidden;background:#000;touch-action:none;}
  #sahne{position:fixed;inset:0;overflow:hidden;background:#000;}
  #oyun{position:absolute;left:0;top:0;width:1920px;height:1080px;border:0;background:#000;transform-origin:0 0;}
</style>
</head>
<body>
<div id=""sahne"">
  <iframe id=""oyun"" src=""oyun.html"" allow=""autoplay; fullscreen"" allowfullscreen title=""Kumar Kazandırmaz""></iframe>
</div>
<script>
/* PINCH_KILIT: iOS'ta iki parmak yakinlastirma sayfayi yuksek cozunurlukte yeniden
   cizdirip bellegi patlatiyor (sekme cokmesi). Pinch tamamen engellenir. */
(function(){
  ['gesturestart','gesturechange','gestureend'].forEach(function(t){
    document.addEventListener(t, function(e){ e.preventDefault(); }, {passive:false});
  });
  document.addEventListener('touchmove', function(e){
    if(e.touches && e.touches.length>1) e.preventDefault();
  }, {passive:false});
})();
(function(){
  var GW=1920, GH=1080;
  var sahne=document.getElementById('sahne'), oyun=document.getElementById('oyun');
  if(location.search) oyun.src='oyun.html'+location.search; // ?debug gibi parametreler oyuna aktarilsin
  function olcekle(){
    var w=sahne.clientWidth||window.innerWidth, h=sahne.clientHeight||window.innerHeight;
    if(!w||!h) return;
    var s=Math.min(w/GW, h/GH);
    oyun.style.transform='translate('+((w-GW*s)/2)+'px,'+((h-GH*s)/2)+'px) scale('+s+')';
  }
  window.addEventListener('resize',olcekle);
  window.addEventListener('orientationchange',olcekle);
  if(typeof ResizeObserver!=='undefined') new ResizeObserver(olcekle).observe(sahne);
  olcekle();
})();
</script>
</body>
</html>
";

    [PostProcessBuild(3000)]
    public static void WebGlBuildSonrasi(BuildTarget hedef, string ciktiKlasoru)
    {
        if (hedef != BuildTarget.WebGL)
            return;

        string indexYolu = Path.Combine(ciktiKlasoru, "index.html");
        string oyunYolu  = Path.Combine(ciktiKlasoru, "oyun.html");
        if (!File.Exists(indexYolu))
            return;

        string mevcut = File.ReadAllText(indexYolu);
        if (mevcut.Contains(SarmalIsareti))
        {
            // index.html zaten sarmalayıcı (ör. build üstüne build) — oyun.html'e dokunma.
            UnityEngine.Debug.Log("[WebGlOlcekliSarmalPostProcess] index.html zaten sarmalayıcı, atlandı.");
            return;
        }

        // 1) Oyunun kendisi oyun.html'e
        File.WriteAllText(oyunYolu, mevcut);
        // 2) index.html artık ölçekleyici sarmalayıcı
        File.WriteAllText(indexYolu, SarmalHtml);

        UnityEngine.Debug.Log("[WebGlOlcekliSarmalPostProcess] Ölçekli sarmalayıcı uygulandı: index.html (sarmal) + oyun.html (oyun).");
    }
}
