using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChastainRush
{
    // Development check: frame rate in Play mode at the spawn and at the densest part of the map.
    // Batch (needs a graphics device, so no -nographics):
    //   Unity.exe -batchmode -projectPath . -executeMethod ChastainRush.PerfCheck.Run -perfOut <dir>
    // Real Game view (windowed, no -batchmode): add -perfGameView
    public static class PerfCheck
    {
        [Serializable] class Building { public float[] footprint; }
        [Serializable] class Extra { public Building[] buildings; }

        [Serializable] class ViewPt { public string label; public float x, z, fx, fz, qx, qz; }
        [Serializable] class Views { public ViewPt[] points; }

        const float Radius = 300f; // roughly what the chase camera sees of the buildings and roads around the car

        public static void Run()
        {
            string outDir = Path.GetFullPath("Temp/PerfCheck");
            var args = Environment.GetCommandLineArgs();
            bool gameView = Array.IndexOf(args, "-perfGameView") >= 0;
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-perfOut") outDir = args[i + 1];
            Directory.CreateDirectory(outDir);

            EditorSceneManager.OpenScene(MapBuilder.ScenePath);
            string json = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "Data/map.json"));
            var map = JsonUtility.FromJson<DriveCheck.MapData>(json);
            var extra = JsonUtility.FromJson<Extra>(json);
            var runner = new GameObject("PerfCheck").AddComponent<PerfCheckRunner>();
            runner.outputDir = outDir;
            runner.gameView = gameView;

            var centres = new List<Vector2>();
            foreach (var b in extra.buildings)
            {
                Vector2 c = Vector2.zero;
                int n = b.footprint.Length / 2;
                for (int i = 0; i < n; i++) c += new Vector2(b.footprint[2 * i], b.footprint[2 * i + 1]);
                centres.Add(c / Mathf.Max(n, 1));
            }
            var roadPts = new List<(Vector2 p, Vector2 dir, string name)>();
            foreach (var r in map.roads) AddPoints(roadPts, r.points, r.name);
            foreach (var b in map.bridges) AddPoints(roadPts, b.points, b.name);

            var spawn = new Vector2(map.spawn.x, map.spawn.z);
            float h = map.spawn.heading_deg * Mathf.Deg2Rad;
            runner.locations.Add(Make("Round 1 spawn (Chastain Glen Lane)", spawn, new Vector2(Mathf.Sin(h), Mathf.Cos(h)), map.spawn.street, map, centres));

            // densest: the named-road point with the most building footprints within Radius
            int bestCount = -1;
            (Vector2 p, Vector2 dir, string name) bestPt = default;
            for (int i = 0; i < roadPts.Count; i += 3)
            {
                var rp = roadPts[i];
                if (string.IsNullOrEmpty(rp.name)) continue;
                int count = 0;
                foreach (var c in centres) if ((c - rp.p).sqrMagnitude < Radius * Radius) count++;
                if (count > bestCount) { bestCount = count; bestPt = rp; }
            }
            runner.locations.Add(Make("Densest part (most buildings within 300 m of a named road)", bestPt.p, bestPt.dir, bestPt.name, map, centres));
            // Round 7: the three places that matter for the dressed neighbourhood (-perfRound7): the entrance, beside the pond, outside Dan's house
            if (Array.IndexOf(args, "-perfRound7") >= 0)
            {
                var vp = JsonUtility.FromJson<Views>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "Assets/Generated/hood_views.json")));
                ViewPt Find(string l) { foreach (var p in vp.points) if (p.label == l) return p; return null; }
                runner.locations.Clear();
                runner.locations.Add(Make("Entrance island (the map spawn on Chastain Glen Lane)", spawn, new Vector2(Mathf.Sin(h), Mathf.Cos(h)), map.spawn.street, map, centres));
                var gr = Find("geese_road"); var fo = Find("fountain");
                if (gr != null && fo != null)
                {
                    var at = new Vector2(gr.x, gr.z); var tp = (new Vector2(fo.x, fo.z) - at).normalized;
                    var along = new Vector2(-tp.y, tp.x);
                    runner.locations.Add(Make("Beside the pond (road next to the geese, fountain in view)", at - along * 20f, along, "Chastain Glen Lane", map, centres));
                }
                var dn = Find("dan");
                if (dn != null)
                {
                    var q = new Vector2(dn.qx, dn.qz); var c = new Vector2(dn.x, dn.z); var tp = (c - q).normalized; var along = new Vector2(-tp.y, tp.x);
                    runner.locations.Add(Make("Outside Dan's house (street in front of it)", q - along * 25f, along, "Chastain Glen Lane", map, centres));
                }
            }
            EditorApplication.EnterPlaymode();
        }

        static void AddPoints(List<(Vector2, Vector2, string)> list, float[] pts, string name)
        {
            for (int i = 0; i + 3 < pts.Length; i += 2)
            {
                var a = new Vector2(pts[i], pts[i + 1]);
                var b = new Vector2(pts[i + 2], pts[i + 3]);
                if ((b - a).sqrMagnitude > 0.01f) list.Add((a, (b - a).normalized, name));
            }
        }

        static PerfCheckRunner.Location Make(string label, Vector2 at, Vector2 dir, string street, DriveCheck.MapData map, List<Vector2> centres)
        {
            var loc = new PerfCheckRunner.Location { label = label, yaw = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg };
            loc.position = new Vector3(at.x, GroundQuery.Height(at.x, at.y) + 0.8f, at.y);
            foreach (var p in DriveCheck.StreetPath(map, street, at, dir, 400f))
                loc.path.Add(new Vector3(p.x, GroundQuery.Height(p.x, p.y), p.y));
            int buildings = 0;
            foreach (var c in centres) if ((c - at).sqrMagnitude < Radius * Radius) buildings++;
            float roadM = 0f;
            int roadWays = 0;
            foreach (var r in map.roads)
            {
                bool any = false;
                for (int i = 0; i + 3 < r.points.Length; i += 2)
                {
                    var a = new Vector2(r.points[i], r.points[i + 1]);
                    var b = new Vector2(r.points[i + 2], r.points[i + 3]);
                    if (((a + b) * 0.5f - at).sqrMagnitude < Radius * Radius) { roadM += (b - a).magnitude; any = true; }
                }
                if (any) roadWays++;
            }
            loc.counts = $"within 300 m of ({at.x:F0},{at.y:F0}): {buildings} buildings, {roadWays} road ways = {roadM:F0} m of road";
            return loc;
        }
    }
}
