using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChastainRush
{
    // Round 7 screenshots (item 12), saved to Data/screenshots/round7 (or -shotOut <dir>). Batch, needs a graphics device:
    //   Unity.exe -batchmode -projectPath . -executeMethod ChastainRush.Round7Check.Run [-shotOut <dir>]
    // The viewpoints are written by the map builder (Assets/Generated/hood_views.json), so they follow the map.
    public static class Round7Check
    {
        public static void Run()
        {
            string outDir = Path.GetFullPath("Data/screenshots/round7");
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-shotOut") outDir = Path.GetFullPath(args[i + 1]);
            Directory.CreateDirectory(outDir);
            EditorSceneManager.OpenScene(MapBuilder.ScenePath);
            var runner = new GameObject("Round7Runner").AddComponent<Round7Runner>();
            runner.outputDir = outDir;
            runner.viewsJsonPath = Path.GetFullPath("Assets/Generated/hood_views.json");
            EditorApplication.EnterPlaymode();
        }
    }
}
