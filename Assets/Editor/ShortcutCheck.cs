using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChastainRush
{
    // Round 8, item 4g: drives both official shortcuts with the real car by script, at several approach speeds, and the road
    // between the same two points at the same speeds; then a race lap that takes both shortcuts. Batch mode is enough (no
    // screenshots): Unity.exe -batchmode -nographics -projectPath . -executeMethod ChastainRush.ShortcutCheck.Run -shortcutCheckOut <dir>
    public static class ShortcutCheck
    {
        [Serializable] class ScLite { public string id, name; public float[] path; public float leave_lap_s, rejoin_lap_s; public JumpLite[] jumps; }
        [Serializable] class JumpLite { public string label; public float x, z; }
        [Serializable] class FileLite { public ScLite[] shortcuts; }

        public static void Run()
        {
            string outDir = Path.GetFullPath("Temp/ShortcutCheck");
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-shortcutCheckOut") outDir = args[i + 1];
            Directory.CreateDirectory(outDir);
            EditorSceneManager.OpenScene(MapBuilder.ScenePath);
            var data = JsonUtility.FromJson<FileLite>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "Data/shortcuts/chastain_glen_loop.json")));
            var runner = new GameObject("ShortcutCheck").AddComponent<ShortcutCheckRunner>();
            runner.outputDir = outDir;
            foreach (var s in data.shortcuts)
            {
                var t = new ShortcutCheckRunner.Target { id = s.id, name = s.name, leaveLapS = s.leave_lap_s, rejoinLapS = s.rejoin_lap_s, startBack = s.id.Contains("over_house") ? 14f : 35f };
                for (int i = 0; i + 1 < s.path.Length; i += 2) t.path.Add(new Vector2(s.path[i], s.path[i + 1]));
                if (s.jumps != null) foreach (var j in s.jumps) t.jumps.Add(new Vector2(j.x, j.z));
                runner.targets.Add(t);
            }
            EditorApplication.EnterPlaymode();
        }
    }
}
