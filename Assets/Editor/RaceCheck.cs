using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChastainRush
{
    // Development check (Round 5, item 8): opens the generated scene, enters Play mode and lets RaceCheckRunner drive the race
    // route by script. Windowed (not -batchmode / -nographics): the screenshots include the race HUD.
    // Unity.exe -projectPath . -executeMethod ChastainRush.RaceCheck.Run [-raceCheckOut <dir>] [-raceCheck lap|full] [-raceDirection forward|reverse]
    public static class RaceCheck
    {
        public static void Run()
        {
            string outDir = Path.GetFullPath("Temp/RaceCheck"), mode = "full";
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-raceCheckOut") outDir = args[i + 1];
                if (args[i] == "-raceCheck") mode = args[i + 1];
            }
            Directory.CreateDirectory(outDir);
            EditorSceneManager.OpenScene(MapBuilder.ScenePath);
            var runner = new GameObject("RaceCheck").AddComponent<RaceCheckRunner>();
            runner.outputDir = outDir; runner.mode = mode;
            // the check writes its own records, not the real local/race_records.json
            runner.recordsPath = Path.Combine(outDir, "race_records_" + mode + ".json");
            if (File.Exists(runner.recordsPath)) File.Delete(runner.recordsPath);
            // Chasefield Way: the street that cuts across the loop between the two Chasefield junctions (forward direction)
            var map = JsonUtility.FromJson<DriveCheck.MapData>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "Data/map.json")));
            var route = JsonUtility.FromJson<RouteJson>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "Data/routes/chastain_glen_loop.json")));
            var cps = route.checkpoints;
            Vector2 a = new Vector2(cps[0].pos[0], cps[0].pos[1]), b = new Vector2(cps[2].pos[0], cps[2].pos[1]);
            foreach (var r in map.roads)
            {
                if (r.name != "Chasefield Way" || r.points.Length < 4) continue;
                var p0 = new Vector2(r.points[0], r.points[1]); var p1 = new Vector2(r.points[r.points.Length - 2], r.points[r.points.Length - 1]);
                bool fwd = (p0 - a).magnitude < 3f && (p1 - b).magnitude < 3f, rev = (p1 - a).magnitude < 3f && (p0 - b).magnitude < 3f;
                if (!fwd && !rev) continue;
                for (int i = 0; i < r.points.Length; i += 2)
                {
                    int k = fwd ? i : r.points.Length - 2 - i;
                    runner.shortcut.Add(new Vector3(r.points[k], GroundQuery.Height(r.points[k], r.points[k + 1]), r.points[k + 1]));
                }
                break;
            }
            Debug.Log($"[RaceCheck] mode {mode}, shortcut path {runner.shortcut.Count} points, output {outDir}");
            EditorApplication.EnterPlaymode();
        }
    }
}
