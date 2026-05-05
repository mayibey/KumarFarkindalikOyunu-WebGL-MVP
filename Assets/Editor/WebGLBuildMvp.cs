using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class WebGLBuildMvp
{
    private const string CiktiKlasoru = "Builds/WebGL_MVP";

    [MenuItem("Araçlar/WebGL MVP Build Al")]
    public static void Build()
    {
        try
        {
            WebGlAyarlariniUygula();
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
                throw new Exception("WebGL platformuna geçiş başarısız.");

            string[] sahneler = EditorBuildSettings.scenes
                .Where(s => s != null && s.enabled)
                .Select(s => s.path)
                .ToArray();
            if (sahneler.Length == 0)
                throw new Exception("Build için aktif sahne bulunamadı.");

            Directory.CreateDirectory(CiktiKlasoru);
            BuildOptions ops = BuildOptions.Development;
#if UNITY_2021_2_OR_NEWER
            ops |= BuildOptions.CleanBuildCache;
#endif

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = sahneler,
                target = BuildTarget.WebGL,
                locationPathName = CiktiKlasoru,
                options = ops
            };

            BuildReport rapor = BuildPipeline.BuildPlayer(buildPlayerOptions);
            if (rapor.summary.result != BuildResult.Succeeded)
                throw new Exception("WebGL build başarısız: " + rapor.summary.result);

            Debug.Log("[WebGLBuildMvp] Build tamamlandı: " + Path.GetFullPath(CiktiKlasoru));
            AssetDatabase.SaveAssets();
        }
        catch (Exception ex)
        {
            Debug.LogError("[WebGLBuildMvp] Hata: " + ex.Message);
            throw;
        }
    }

    private static void WebGlAyarlariniUygula()
    {
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.decompressionFallback = false;
        // GEÇİCİ: Stripping Disabled — scripted sistem [RuntimeInitializeOnLoadMethod]
        // attribute'lu sınıflar Medium/Low stripper tarafından strip ediliyor.
        // Production için: Low + [Preserve] attribute kombinasyonu (8 scripted
        // sınıfa eklenmeli).
        PlayerSettings.stripEngineCode = false;

        // Stabilite odaklı başlangıç: 512 MB.
        PlayerSettings.WebGL.memorySize = 512;
        PlayerSettings.WebGL.initialMemorySize = 256;
        PlayerSettings.WebGL.maximumMemorySize = 1024;

        // Debug için "Explicitly Thrown" türevi modu sürüm bağımsız seç.
        PlayerSettings.WebGL.exceptionSupport = WebGlExceptionModuSec();

        // Brotli destekliyse tercih et, değilse Gzip.
        try
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        }
        catch
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        }

#if UNITY_2021_2_OR_NEWER
        // GEÇİCİ: Disabled — bkz yukarıdaki stripEngineCode yorumu (scripted sistem koruma).
        PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.WebGL, ManagedStrippingLevel.Disabled);
#endif
        // KRİTİK: PlayerSettings runtime değişikliklerini ProjectSettings.asset'e PERSIST et.
        // Aksi halde asset'te eski değer kalıyor; IL2CPP build base'i asset'i okuyabiliyor →
        // RuntimeInitializeOnLoadMethod metotları strip ediliyor (scripted sistem aktive olmuyor).
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static WebGLExceptionSupport WebGlExceptionModuSec()
    {
        string[] adlar = Enum.GetNames(typeof(WebGLExceptionSupport));
        if (adlar.Contains("ExplicitlyThrownExceptions"))
            return (WebGLExceptionSupport)Enum.Parse(typeof(WebGLExceptionSupport), "ExplicitlyThrownExceptions");
        if (adlar.Contains("ExplicitlyThrownExceptionsOnly"))
            return (WebGLExceptionSupport)Enum.Parse(typeof(WebGLExceptionSupport), "ExplicitlyThrownExceptionsOnly");
        if (adlar.Contains("FullWithStacktrace"))
            return (WebGLExceptionSupport)Enum.Parse(typeof(WebGLExceptionSupport), "FullWithStacktrace");
        return PlayerSettings.WebGL.exceptionSupport;
    }
}
