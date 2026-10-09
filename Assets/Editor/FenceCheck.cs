using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChastainRush
{
    // Round 7 fence check: Unity.exe -batchmode -projectPath . -executeMethod ChastainRush.FenceCheck.Run [-fenceOut <dir>]
    public static class FenceCheck
    {
        public static void Run()
        {
            string outDir = Path.GetFullPath("Temp/FenceCheck");
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-fenceOut") outDir = Path.GetFullPath(args[i + 1]);
            Directory.CreateDirectory(outDir);
            EditorSceneManager.OpenScene(MapBuilder.ScenePath);
            new GameObject("FenceCheck").AddComponent<FenceCheckRunner>().outputDir = outDir;
            EditorApplication.EnterPlaymode();
        }
    }
}
