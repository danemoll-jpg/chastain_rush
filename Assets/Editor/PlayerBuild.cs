using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ChastainRush
{
    // Round 8, item 1: a one-step Windows player build. Rebuilds the map scene (Chastain Rush > Build Map) and then builds
    // Builds/ChastainRush/ChastainRush.exe (git-ignored). Ported from Racer's SoloReleaseBuild (development / debugging /
    // profiler off, BuildPipeline.BuildPlayer, a build report and a VERSION file next to the exe); Racer's launcher, signing,
    // version numbering and licence copying are not brought over (Chastain Rush has none of them yet).
    //   Menu:  Chastain Rush > Build Executable
    //   Batch: Unity.exe -batchmode -nographics -quit -projectPath . -executeMethod ChastainRush.PlayerBuild.BuildFromCommandLine
    // Everything the game reads at run time is inside the scene (the route file is embedded by MapBuilder), or written under
    // Application.persistentDataPath in a build: race records, debug reports and the bump log (see the report file).
    public static class PlayerBuild
    {
        public const string OutputDir = "Builds/ChastainRush";
        public const string ExeName = "ChastainRush.exe";

        [MenuItem("Chastain Rush/Build Executable")]
        public static void BuildMenu() => Build();

        public static void BuildFromCommandLine()
        {
            try { Build(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        public static BuildReport Build()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Leave Play mode first.");
            MapBuilder.Build();   // the exe always gets a fresh scene from the current data

            PlayerSettings.companyName = "Chastain Rush";
            PlayerSettings.productName = "Chastain Rush";
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;   // borderless, at the desktop resolution
            PlayerSettings.defaultIsNativeResolution = true;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
            PlayerSettings.usePlayerLog = true;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;
            EditorUserBuildSettings.connectProfiler = false;
            EditorUserBuildSettings.waitForPlayerConnection = false;

            string root = Path.GetDirectoryName(Application.dataPath);
            string dir = Path.Combine(root, OutputDir);
            Directory.CreateDirectory(dir);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { MapBuilder.ScenePath },
                locationPathName = Path.Combine(dir, ExeName),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });
            var s = report.summary;
            string persistent = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", PlayerSettings.companyName, PlayerSettings.productName);
            var lines = new[]
            {
                $"Chastain Rush player build, {DateTime.Now:yyyy-MM-dd HH:mm}",
                $"Result {s.result}; errors {s.totalErrors}; warnings {s.totalWarnings}; output {s.totalSize / 1048576.0:0.0} MB; took {s.totalTime.TotalSeconds:0} s",
                $"Exe: {Path.Combine(dir, ExeName)}",
                $"Unity {Application.unityVersion}; Windows x64; development off",
                $"Run-time files (persistentDataPath): {persistent}",
                "  race_records.json, bump_log.txt, DebugReports/<session>/ (bug reports), Player.log",
                "Keys: Esc twice quits, F2 frame-rate counter, F3 debug panel, F4 bug capture, F6 debug menu, T race, X leave race, R reset car",
            };
            File.WriteAllLines(Path.Combine(dir, "BUILD_INFO.txt"), lines.Concat(report.steps.SelectMany(st => st.messages)
                .Where(m => m.type == LogType.Error || m.type == LogType.Exception).Select(m => "ERROR " + m.content)));
            Debug.Log("[PlayerBuild] " + string.Join("\n", lines));
            if (s.result != BuildResult.Succeeded) throw new InvalidOperationException("Player build failed: " + s.result);
            // A MonoBehaviour in a file of another name is "missing" in a player: the build still succeeds but writes a scene
            // the exe cannot load (Round 8: the fence panels and the geese). Treat it as a failed build.
            int missing = report.steps.SelectMany(st => st.messages).Count(m => m.content.Contains("is missing or no valid script is attached"));
            if (missing > 0) throw new InvalidOperationException($"Player build: {missing} components have a missing script (a MonoBehaviour must live in a file named after it); the exe would not load");
            return report;
        }
    }
}
