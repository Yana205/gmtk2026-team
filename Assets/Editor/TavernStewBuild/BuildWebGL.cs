using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TavernStewBuild
{
    // Snapshot WebGL build for itch.io playtesting (proportions / feel in-browser).
    // Settings are tuned for itch.io hosting, which does NOT set Content-Encoding
    // headers: Gzip + decompressionFallback lets the compressed build load anyway.
    // Output: <project>/Builds/WebGL — zip the CONTENTS of that folder for itch upload.
    public static class BuildWebGL
    {
        private const string OutputDir = "Builds/WebGL";
        private static readonly string[] Scenes = { "Assets/Scenes/Main.unity" };

        [MenuItem("Tavern Stew/Build/WebGL (itch.io snapshot)")]
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

            string line;
            if (s.result == BuildResult.Succeeded)
            {
                line = $"SUCCEEDED  {s.totalSize / (1024f * 1024f):F1} MB  in {s.totalTime}  -> {absOut}";
                Debug.Log($"[BuildWebGL] {line}");
            }
            else
            {
                line = $"{s.result}  errors={s.totalErrors}  warnings={s.totalWarnings}";
                Debug.LogError($"[BuildWebGL] {line}");
            }
            File.WriteAllText(resultFile, line + "\n" + DateTime.Now + "\n");
        }
    }
}
