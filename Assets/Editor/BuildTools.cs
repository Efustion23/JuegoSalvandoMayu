using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Compilaciones del juego. Desde el menu (Herramientas) o por linea de comandos, con Unity cerrado:
//   Unity.exe -batchmode -quit -projectPath <ruta> -executeMethod BuildTools.BuildWebBatch -logFile build.log
public static class BuildTools
{
    [MenuItem("Herramientas/Compilar para Web (WebGL)")]
    public static void BuildWebMenu() => Build(BuildTarget.WebGL, "Builds/Web", false);

    [MenuItem("Herramientas/Compilar para Windows")]
    public static void BuildWindowsMenu() => Build(BuildTarget.StandaloneWindows64, "Builds/Windows/SalvandoAlMayu.exe", false);

    public static void BuildWebBatch() => Build(BuildTarget.WebGL, "Builds/Web", true);

    // Version para Unity Play: sin compresion (el validador de Unity Play no reconoce los .unityweb del modo con fallback).
    public static void BuildWebPlayBatch()
    {
        var format = PlayerSettings.WebGL.compressionFormat;
        var fallback = PlayerSettings.WebGL.decompressionFallback;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = false;
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = scenes, target = BuildTarget.WebGL, targetGroup = BuildTargetGroup.WebGL, locationPathName = "Builds/WebPlay", options = BuildOptions.None });
        PlayerSettings.WebGL.compressionFormat = format;
        PlayerSettings.WebGL.decompressionFallback = fallback;
        var s = report.summary;
        Debug.Log($"[BuildTools] WebGL (Unity Play): {s.result}, {s.totalErrors} errores, {s.totalSize / (1024 * 1024)} MB -> Builds/WebPlay");
        EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
    }
    public static void BuildWindowsBatch() => Build(BuildTarget.StandaloneWindows64, "Builds/Windows/SalvandoAlMayu.exe", true);

    static void Build(BuildTarget target, string output, bool exitWhenDone)
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (target == BuildTarget.WebGL)
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;   // funciona en cualquier alojamiento, sin configurar cabeceras
        }
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            target = target,
            targetGroup = BuildPipeline.GetBuildTargetGroup(target),
            locationPathName = output,
            options = BuildOptions.None,
        });
        var s = report.summary;
        Debug.Log($"[BuildTools] {target}: {s.result}, {s.totalErrors} errores, {s.totalWarnings} avisos, {s.totalSize / (1024 * 1024)} MB -> {output}");
        if (exitWhenDone) EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
    }
}
