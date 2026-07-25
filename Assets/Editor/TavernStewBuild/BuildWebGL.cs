using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TavernStewBuild
{
    // Snapshot WebGL build for itch.io playtesting (proportions / feel in-browser).
    // Settings are tuned for itch.io hosting, which does NOT set Content-Encoding
    // headers: Gzip + decompressionFallback lets the compressed build load anyway.
    // Output: <project>/Builds/WebGL — the zip below packs its CONTENTS at the root
    // (index.html at top level), which is exactly what itch.io expects.
    public static class BuildWebGL
    {
        private const string OutputDir = "Builds/WebGL";                       // build folder (relative to project root)
        private const string ZipName   = "TavernStew-WebGL-itch.zip";          // ready-to-upload package
        private static readonly string[] Scenes = { "Assets/Scenes/Main.unity" };

        // ONE CLICK: build the WebGL player, then package the itch.io-ready zip.
        [MenuItem("Tavern Stew/Build/WebGL → Zip for itch.io")]
        public static void Perform()
        {
            // --- itch.io-friendly player settings ---
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;          // works without server headers (itch.io)
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.SplashScreen.showUnityLogo = false;          // faster to the game for a snapshot
            // Responsive template scales the game to fill any 16:9 frame (itch.io / fullscreen),
            // so it never crops. WIDTH/HEIGHT here just define the 16:9 design aspect (1920x1080).
            PlayerSettings.WebGL.template = "PROJECT:TavernStewResponsive";
            PlayerSettings.defaultWebScreenWidth = 1920;
            PlayerSettings.defaultWebScreenHeight = 1080;
            if (PlayerSettings.productName == "gmtk2026")
                PlayerSettings.productName = "Tavern Stew";

            var projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            var absOut = Path.Combine(projectRoot, OutputDir);
            Directory.CreateDirectory(absOut);

            var resultFile = Path.Combine(projectRoot, "Builds", "last-webgl-build.txt");
            File.WriteAllText(resultFile, "STARTED\n");

            var opts = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = OutputDir,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(opts);
            BuildSummary s = report.summary;

            if (s.result != BuildResult.Succeeded)
            {
                var fail = $"{s.result}  errors={s.totalErrors}  warnings={s.totalWarnings}";
                Debug.LogError($"[BuildWebGL] {fail}");
                File.WriteAllText(resultFile, fail + "\n" + DateTime.Now + "\n");
                return;
            }

            string zipPath = ZipForItch(absOut, projectRoot, out float zipMb);
            var line = $"SUCCEEDED  {s.totalSize / (1024f * 1024f):F1} MB  in {s.totalTime}  -> {absOut}\n" +
                       $"ZIP  {zipMb:F1} MB  -> {zipPath}";
            Debug.Log($"[BuildWebGL] {line}");
            File.WriteAllText(resultFile, line + "\n" + DateTime.Now + "\n");
            EditorUtility.RevealInFinder(zipPath);   // pop Finder open at the zip, ready to drag onto itch.io
        }

        // Re-zip the LAST build without rebuilding — handy after a manual tweak to the output.
        [MenuItem("Tavern Stew/Build/Zip Last WebGL Build (no rebuild)")]
        public static void ZipOnly()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            var absOut = Path.Combine(projectRoot, OutputDir);
            if (!File.Exists(Path.Combine(absOut, "index.html")))
            {
                EditorUtility.DisplayDialog("No WebGL build found",
                    $"No build at:\n{absOut}\n\nRun 'Tavern Stew ▸ Build ▸ WebGL → Zip for itch.io' first.", "OK");
                return;
            }
            string zipPath = ZipForItch(absOut, projectRoot, out float zipMb);
            Debug.Log($"[BuildWebGL] Zipped existing build: {zipMb:F1} MB -> {zipPath}");
            EditorUtility.RevealInFinder(zipPath);
        }

        // Pack the WebGL output into an itch.io-ready zip (contents at root, no debug symbols).
        private static string ZipForItch(string absOut, string projectRoot, out float zipMb)
        {
            // Strip Unity's debug-symbol folder(s) — explicitly marked "DoNotShip".
            foreach (var dir in Directory.GetDirectories(absOut, "*_DoNotShip", SearchOption.TopDirectoryOnly))
                Directory.Delete(dir, recursive: true);

            // Strip macOS .DS_Store noise so it doesn't end up in the upload.
            foreach (var ds in Directory.GetFiles(absOut, ".DS_Store", SearchOption.AllDirectories))
                File.Delete(ds);

            // WebGL builds sometimes drop a stray Data/ at the project root — clean it up.
            var strayData = Path.Combine(projectRoot, "Data");
            if (Directory.Exists(strayData)) { try { Directory.Delete(strayData, true); } catch { /* ignore */ } }

            var zipPath = Path.Combine(projectRoot, "Builds", ZipName);
            if (File.Exists(zipPath)) File.Delete(zipPath);
            // includeBaseDirectory:false => index.html / Build/ / TemplateData/ sit at the zip root.
            ZipFile.CreateFromDirectory(absOut, zipPath, System.IO.Compression.CompressionLevel.Optimal, includeBaseDirectory: false);

            zipMb = new FileInfo(zipPath).Length / (1024f * 1024f);
            return zipPath;
        }
    }
}
