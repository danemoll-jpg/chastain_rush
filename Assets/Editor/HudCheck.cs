using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChastainRush
{
    // Development check (Round 4, item 4): does the debug HUD / bug capture / bump log work in Editor Play mode?
    // Needs a graphics device (not -nographics), because the capture reads the rendered frame.
    // Unity.exe -projectPath . -executeMethod ChastainRush.HudCheck.Run [-hudCheckOut <dir>]
    public static class HudCheck
    {
        public static void Run()
        {
            string outDir = Path.GetFullPath("Temp/HudCheck");
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-hudCheckOut") outDir = args[i + 1];
            EditorSceneManager.OpenScene(MapBuilder.ScenePath);
            new GameObject("HudCheck").AddComponent<HudCheckRunner>().outputDir = outDir;
            EditorApplication.EnterPlaymode();
        }
    }
}
