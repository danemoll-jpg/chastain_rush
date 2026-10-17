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
            // the exe always gets a fresh scene from the current data (Round 15: -playerBuildKeepScene, for a code-only rebuild
            // during a round, builds from the scene already made; the last build of a round never uses it)
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-playerBuildKeepScene") < 0) MapBuilder.Build();

            PlayerSettings.companyName = "Chastain Rush";
            PlayerSettings.productName = "Chastain Rush";
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;   // borderless, at the desktop resolution
            PlayerSettings.defaultIsNativeResolution = true;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
            PlayerSettings.usePlayerLog = true;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            // Round 24 A2: -playerBuildDev makes a development build (profiler markers on) in Builds/ChastainRushDev instead, for
            // ProfileRunner; Dan's exe is never a development build
            bool dev = Array.IndexOf(Environment.GetCommandLineArgs(), "-playerBuildDev") >= 0;
            EditorUserBuildSettings.development = dev;
            EditorUserBuildSettings.allowDebugging = false;
            EditorUserBuildSettings.connectProfiler = false;
            EditorUserBuildSettings.waitForPlayerConnection = false;

            string root = Path.GetDirectoryName(Application.dataPath);
            string dir = Path.Combine(root, dev ? OutputDir + "Dev" : OutputDir);
            Directory.CreateDirectory(dir);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { MapBuilder.ScenePath },
                locationPathName = Path.Combine(dir, ExeName),
                target = BuildTarget.StandaloneWindows64,
                options = dev ? BuildOptions.Development : BuildOptions.None,
            });
            var s = report.summary;
            string music = s.result == BuildResult.Succeeded ? CopyMusic(root, dir) : "music not copied (build failed)";   // Round 27 (B1)
            if (s.result == BuildResult.Succeeded) CopyPoliceRadio(root, dir);   // Round 39 (Part D)
            string persistent = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", PlayerSettings.companyName, PlayerSettings.productName);
            var lines = new[]
            {
                $"Chastain Rush player build, {DateTime.Now:yyyy-MM-dd HH:mm}",
                $"Result {s.result}; errors {s.totalErrors}; warnings {s.totalWarnings}; output {s.totalSize / 1048576.0:0.0} MB; took {s.totalTime.TotalSeconds:0} s",
                $"Exe: {Path.Combine(dir, ExeName)}",
                $"Unity {Application.unityVersion}; Windows x64; development off",
                $"Run-time files (persistentDataPath): {persistent}",
                "  race_records.json, bump_log.txt, DebugReports/<session>/ (bug reports), Player.log",
                "Keys: Esc twice quits, F2 or P frame-rate counter, F3 or [ debug panel, F4 or ] bug capture, F6 or \\ debug menu, T race, X leave race, R reset car",
                "Radio (Round 27, Racer's): D-pad down or N station / off, D-pad right or . next song, D-pad left or , previous, D-pad up or I what is playing",
                music,
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

        // Round 39 (Part D): Dan's police radio clips (Tools/Audio/police_radio_studio.py writes SourceArt/Audio/PoliceRadio/<id>.wav) into
        // ChastainRush_Data/PoliceRadio/, where the game looks after the drop-in folder; none recorded yet: nothing copied, the radio is text
        static void CopyPoliceRadio(string root, string dir)
        {
            string from = Path.Combine(root, "SourceArt", "Audio", "PoliceRadio"), to = Path.Combine(dir, Path.GetFileNameWithoutExtension(ExeName) + "_Data", "PoliceRadio");
            if (Directory.Exists(to)) Directory.Delete(to, true);
            if (!Directory.Exists(from)) { Debug.Log("[Round39] police radio: no clips recorded yet (SourceArt/Audio/PoliceRadio missing): the calls are text"); return; }
            Directory.CreateDirectory(to); int n = 0;
            foreach (var f in Directory.GetFiles(from)) { var e = Path.GetExtension(f).ToLowerInvariant(); if (e != ".wav") continue; File.Copy(f, Path.Combine(to, Path.GetFileName(f)), true); n++; }
            Debug.Log($"[Round39] police radio: {n} clips copied into {to}");
        }
        // Round 27 (B1): the music as Racer ships it (Racer's Tools/Package-Racer.ps1): the songs staged in BundleMusic/ (committed here)
        // go into Music/ beside the exe, folder by folder (each top folder is a station), with Music/README.txt, and RADIO.md beside
        // the exe. Files already there with the same size and time are left as they are (the music is 771 MB); anything in Music/
        // that is not in BundleMusic/ is removed, so the build ships exactly what is staged.
        static string CopyMusic(string root, string dir)
        {
            string from = Path.Combine(root, "BundleMusic"), to = Path.Combine(dir, "Music");
            if (!Directory.Exists(from)) return "Music: BundleMusic/ missing, no music shipped";
            Directory.CreateDirectory(to);
            int copied = 0, kept = 0, removed = 0; long bytes = 0;
            var wanted = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories))
            {
                string rel = f.Substring(from.Length + 1), dst = Path.Combine(to, rel);
                if (rel.StartsWith(".")) continue;
                wanted.Add(Path.GetFullPath(dst));
                var fi = new FileInfo(f); bytes += fi.Length;
                var di = new FileInfo(dst);
                if (di.Exists && di.Length == fi.Length && di.LastWriteTimeUtc == fi.LastWriteTimeUtc) { kept++; continue; }
                Directory.CreateDirectory(Path.GetDirectoryName(dst));
                File.Copy(f, dst, true); File.SetLastWriteTimeUtc(dst, fi.LastWriteTimeUtc); copied++;
            }
            string readme = Path.Combine(to, "README.txt");
            File.WriteAllText(readme, "The radio's bundled music (Settings > Audio > Music: Source Bundled). Each folder here is one station; songs loose in this folder are \"General\". Add DRM-free MP3, PCM WAV or Ogg Vorbis files, then Rescan. See RADIO.md beside the game.");
            wanted.Add(Path.GetFullPath(readme));
            foreach (var f in Directory.EnumerateFiles(to, "*", SearchOption.AllDirectories).ToList())
                if (!wanted.Contains(Path.GetFullPath(f))) { File.Delete(f); removed++; }
            string doc = Path.Combine(root, "Docs", "RADIO.md");
            if (File.Exists(doc)) File.Copy(doc, Path.Combine(dir, "RADIO.md"), true);
            return $"Music: {wanted.Count - 1} files, {bytes / 1048576.0:0} MB from BundleMusic/ in Music/ ({copied} copied, {kept} already there, {removed} removed)";
        }
    }
}
