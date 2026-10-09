using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChastainRush
{
    // Round 8 screenshots: the two official shortcuts (turn-offs with their markers, the opened houses, the ramps, the jumps in
    // flight) and Dan's back yard, saved to Data/screenshots/round8 (or -shotOut <dir>). Batch, needs a graphics device:
    //   Unity.exe -batchmode -projectPath . -executeMethod ChastainRush.Round8Check.Run [-shotOut <dir>]
    public static class Round8Check
    {
        public static void Run()
        {
            string outDir = Path.GetFullPath("Data/screenshots/round8");
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-shotOut") outDir = Path.GetFullPath(args[i + 1]);
            Directory.CreateDirectory(outDir);
            EditorSceneManager.OpenScene(MapBuilder.ScenePath);
            var runner = new GameObject("Round8Runner").AddComponent<Round8ShotsRunner>();
            runner.outputDir = outDir;
            runner.shortcutsJson = File.ReadAllText(Path.GetFullPath("Data/shortcuts/chastain_glen_loop.json"));
            runner.viewsJson = File.ReadAllText(Path.GetFullPath("Assets/Generated/hood_views.json"));
            EditorApplication.EnterPlaymode();
        }
    }
}
