using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChastainRush
{
    // Development check (Round 5, item 8; Round 6, item 8): opens the generated scene, enters Play mode and lets a runner drive
    // the race route by script. Windowed (not -batchmode / -nographics): the screenshots include the race HUD.
    // Unity.exe -projectPath . -executeMethod ChastainRush.RaceCheck.Run [-raceCheckOut <dir>] [-raceCheck lap|full|round6] [-raceDirection forward|reverse]
    //   lap / full  RaceCheckRunner: the real car drives the route (full adds a lap by the TEST shortcut on Chasefield Way)
    //   round6      Round6CheckRunner: gates at the edges and at 45 degrees, penalties, the test shortcut, barriers
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
            string records = Path.Combine(outDir, "race_records_" + mode + ".json");   // the check writes its own records, not the real local/race_records.json
            if (File.Exists(records)) File.Delete(records);

            // The TEST shortcut: Chasefield Way, the street that cuts across the loop between the two Chasefield junctions
            // (forward direction). It exists only here; the route file has no official shortcut yet.
            var root = Path.GetDirectoryName(Application.dataPath);
            var map = JsonUtility.FromJson<DriveCheck.MapData>(File.ReadAllText(Path.Combine(root, "Data/map.json")));
            var route = JsonUtility.FromJson<RouteJson>(File.ReadAllText(Path.Combine(root, "Data/routes/chastain_glen_loop.json")));
            var cps = route.checkpoints;
            Vector2 a = new Vector2(cps[0].pos[0], cps[0].pos[1]), b = new Vector2(cps[2].pos[0], cps[2].pos[1]);
            var path = new List<float>();
            var ground = new List<Vector3>();
            foreach (var r in map.roads)
            {
                if (r.name != "Chasefield Way" || r.points.Length < 4) continue;
                var p0 = new Vector2(r.points[0], r.points[1]); var p1 = new Vector2(r.points[r.points.Length - 2], r.points[r.points.Length - 1]);
                bool fwd = (p0 - a).magnitude < 3f && (p1 - b).magnitude < 3f, rev = (p1 - a).magnitude < 3f && (p0 - b).magnitude < 3f;
                if (!fwd && !rev) continue;
                for (int i = 0; i < r.points.Length; i += 2)
                {
                    int k = fwd ? i : r.points.Length - 2 - i;
                    path.Add(r.points[k]); path.Add(r.points[k + 1]);
                    ground.Add(new Vector3(r.points[k], GroundQuery.Height(r.points[k], r.points[k + 1]), r.points[k + 1]));
                }
                break;
            }
            var test = new RouteShortcutJson
            {
                id = "chasefield_test", name = "Chasefield Way (TEST)", direction = "forward", path = path.ToArray(),
                half_width_m = 6f, entry_inset_m = 8f, entry_margin_m = 4f,
                forgives = new[] { cps[0].id, cps[1].id, cps[2].id },   // CP1 and CP3 sit on the two Chasefield junctions, CP2 on the far bend
            };
            Debug.Log($"[RaceCheck] mode {mode}, test shortcut path {ground.Count} points, output {outDir}");

            if (mode == "round6")
            {
                var r6 = new GameObject("Round6Check").AddComponent<Round6CheckRunner>();
                r6.outputDir = outDir; r6.recordsPath = records; r6.testShortcut = test;
            }
            else
            {
                var runner = new GameObject("RaceCheck").AddComponent<RaceCheckRunner>();
                runner.outputDir = outDir; runner.mode = mode; runner.recordsPath = records;
                runner.shortcut.AddRange(ground);
                runner.testShortcut = test;
            }
            EditorApplication.EnterPlaymode();
        }
    }
}
