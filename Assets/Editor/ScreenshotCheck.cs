using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChastainRush
{
    // Round 3 screenshots from fixed viewpoints, saved to Data/screenshots/round3 (or -shotOut <dir>):
    //   01_spawn, 02_hill_past_bend, 03_new_chastain_bridge, 04_canton_road, 05_i575, 06_overhead
    // Batch (needs a graphics device, so no -nographics):
    //   Unity.exe -batchmode -projectPath . -executeMethod ChastainRush.ScreenshotCheck.Run [-shotOut <dir>]
    // Viewpoints come from the map data, so they follow the map when it is regenerated. The chase views use the
    // game's own camera offset (3.6 m up, 7.5 m behind the car, FOV 65) with the car standing on the spot.
    public static class ScreenshotCheck
    {
        [Serializable] class Road { public string name; public string @class; public float width; public float[] points; }
        [Serializable] class Bridge { public string name; public float width; public float[] points; public float[] deck_y; }
        [Serializable] class Spawn { public float x; public float z; public float heading_deg; public string street; }
        [Serializable] class MapData { public Road[] roads; public Bridge[] bridges; public Spawn spawn; public float size_x_m; public float size_z_m; }

        public static void Run()
        {
            string outDir = Path.GetFullPath("Data/screenshots/round3");
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-shotOut") outDir = Path.GetFullPath(args[i + 1]);
            Directory.CreateDirectory(outDir);

            EditorSceneManager.OpenScene(MapBuilder.ScenePath);
            var map = JsonUtility.FromJson<MapData>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "Data/map.json")));
            var runner = new GameObject("ScreenshotRunner").AddComponent<ScreenshotRunner>();
            runner.outputDir = outDir;

            // 1. spawn on Chastain Glen Lane, as the game starts
            float h = map.spawn.heading_deg * Mathf.Deg2Rad;
            var spawn = new Vector2(map.spawn.x, map.spawn.z);
            var heading = new Vector2(Mathf.Sin(h), Mathf.Cos(h));
            runner.views.Add(new ScreenshotRunner.View { file = "01_spawn.png", label = "Spawn on Chastain Glen Lane", at = spawn, heading = heading });

            // 2. the hill past the bend: drive Chastain Glen Lane from the spawn, find the first bend, then the steepest 60 m after it
            var dm = JsonUtility.FromJson<DriveCheck.MapData>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "Data/map.json")));
            var path = DriveCheck.StreetPath(dm, map.spawn.street, spawn, heading, 900f);
            var samples = Resample(path, 5f);
            int bend = -1;
            for (int i = 6; i < samples.Count; i++)
            {
                var a = (samples[i - 3] - samples[i - 6]).normalized; var b = (samples[i] - samples[i - 3]).normalized;
                if (Vector2.Angle(a, b) > 30f) { bend = i; break; }
            }
            if (bend < 0) bend = Mathf.Min(samples.Count - 14, 20);
            int best = bend; float bestGrade = -1f;
            for (int i = bend; i + 12 < samples.Count; i++)
            {
                float g = (GroundQuery.Height(samples[i + 12].x, samples[i + 12].y) - GroundQuery.Height(samples[i].x, samples[i].y)) / 60f;
                if (g > bestGrade) { bestGrade = g; best = i; }
            }
            var start = samples[best];
            var dir = (samples[Mathf.Min(best + 4, samples.Count - 1)] - samples[best]).normalized;
            runner.views.Add(new ScreenshotRunner.View
            {
                file = "02_hill_past_bend.png", label = $"Hill past the bend: Chastain Glen Lane, bend at ({samples[bend].x:F0},{samples[bend].y:F0}), steepest 60 m after it {bestGrade * 100f:F1}% from ({start.x:F0},{start.y:F0})",
                at = start, heading = dir
            });

            // 3. New Chastain Road bridge (first bridge deck of that road): from before the deck, looking along it
            foreach (var b in map.bridges)
            {
                if (b.name != "New Chastain Road") continue;
                var p0 = new Vector2(b.points[0], b.points[1]);
                var p1 = new Vector2(b.points[b.points.Length - 2], b.points[b.points.Length - 1]);
                var d = (p1 - p0).normalized;
                runner.views.Add(new ScreenshotRunner.View
                {
                    file = "03_new_chastain_bridge.png", label = $"New Chastain Road bridge, deck ({p0.x:F0},{p0.y:F0}) to ({p1.x:F0},{p1.y:F0})",
                    at = p0 - d * 14f, heading = d, deckY = b.deck_y[0] + 0.1f
                });
                break;
            }

            // 4. Canton Road: the middle of its widest piece. 5. I-575 (OSM name Phillip Landrum Memorial Highway): middle of its longest piece.
            AddRoadView(runner, map, r => r.name.StartsWith("Canton Road"), r => r.width, "04_canton_road.png", "Canton Road");
            AddRoadView(runner, map, r => r.@class == "motorway" && r.name == "Phillip Landrum Memorial Highway", r => Length(r), "05_i575.png", "I-575 (Phillip Landrum Memorial Highway)");

            runner.overheadSize = new Vector2(map.size_x_m, map.size_z_m);
            EditorApplication.EnterPlaymode();
        }

        static float Length(Road r)
        {
            float l = 0f;
            for (int i = 0; i + 3 < r.points.Length; i += 2) l += Vector2.Distance(new Vector2(r.points[i], r.points[i + 1]), new Vector2(r.points[i + 2], r.points[i + 3]));
            return l;
        }

        static void AddRoadView(ScreenshotRunner runner, MapData map, Func<Road, bool> pick, Func<Road, float> score, string file, string label)
        {
            Road best = null;
            foreach (var r in map.roads) if (pick(r) && (best == null || score(r) > score(best))) best = r;
            if (best == null) { Debug.LogWarning("[ScreenshotCheck] no road for " + label); return; }
            var pts = new List<Vector2>();
            for (int i = 0; i + 1 < best.points.Length; i += 2) pts.Add(new Vector2(best.points[i], best.points[i + 1]));
            var s = Resample(pts, 5f);
            int m = s.Count / 2;
            var d = (s[Mathf.Min(m + 2, s.Count - 1)] - s[Mathf.Max(m - 2, 0)]).normalized;
            runner.views.Add(new ScreenshotRunner.View { file = file, label = $"{label} at ({s[m].x:F0},{s[m].y:F0}), {best.width:F1} m wide", at = s[m], heading = d });
        }

        static List<Vector2> Resample(List<Vector2> pts, float step)
        {
            var o = new List<Vector2> { pts[0] };
            float carry = 0f;
            for (int i = 1; i < pts.Count; i++)
            {
                var a = pts[i - 1]; var b = pts[i];
                float len = Vector2.Distance(a, b);
                float t = step - carry;
                while (t <= len) { o.Add(Vector2.Lerp(a, b, t / len)); t += step; }
                carry = len - (t - step);
            }
            return o;
        }
    }
}
