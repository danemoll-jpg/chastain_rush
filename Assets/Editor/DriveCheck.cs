using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChastainRush
{
    // Development check: opens the generated scene, enters Play mode and lets DriveCheckRunner drive the car along
    // the spawn street. Writes drive_log.tsv and screenshots to the folder given by -driveCheckOut (default Temp/DriveCheck).
    // Batch: Unity.exe -batchmode -projectPath . -executeMethod ChastainRush.DriveCheck.Run [-driveCheckOut <dir>]
    public static class DriveCheck
    {
        [Serializable] class Road { public string name; public float[] points; }
        [Serializable] class Spawn { public float x; public float z; public float heading_deg; public string street; }
        [Serializable] class MapData { public Road[] roads; public Spawn spawn; }

        public static void Run()
        {
            string outDir = Path.GetFullPath("Temp/DriveCheck");
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-driveCheckOut") outDir = args[i + 1];
            Directory.CreateDirectory(outDir);

            EditorSceneManager.OpenScene(MapBuilder.ScenePath);
            var map = JsonUtility.FromJson<MapData>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "Data/map.json")));
            var terrain = Terrain.activeTerrain;
            var runner = new GameObject("DriveCheck").AddComponent<DriveCheckRunner>();
            runner.outputDir = outDir;
            foreach (var p in StreetPath(map, 1500f))
                runner.path.Add(new Vector3(p.x, terrain.SampleHeight(new Vector3(p.x, 0, p.y)) + terrain.transform.position.y, p.y));
            Debug.Log($"[DriveCheck] path of {runner.path.Count} waypoints along {map.spawn.street}; output {outDir}");
            EditorApplication.EnterPlaymode();
        }

        // Walks the spawn street's ways from the spawn point in the spawn heading, at each junction taking the
        // continuation of the same street that bends least, until maxLength metres or a dead end.
        static List<Vector2> StreetPath(MapData map, float maxLength)
        {
            var adj = new Dictionary<Vector2Int, List<Vector2Int>>();
            Vector2Int Key(Vector2 p) => new Vector2Int(Mathf.RoundToInt(p.x * 10), Mathf.RoundToInt(p.y * 10));
            foreach (var r in map.roads)
            {
                if (r.name != map.spawn.street) continue;
                for (int i = 0; i + 3 < r.points.Length; i += 2)
                {
                    var a = Key(new Vector2(r.points[i], r.points[i + 1]));
                    var b = Key(new Vector2(r.points[i + 2], r.points[i + 3]));
                    if (!adj.ContainsKey(a)) adj[a] = new List<Vector2Int>();
                    if (!adj.ContainsKey(b)) adj[b] = new List<Vector2Int>();
                    adj[a].Add(b);
                    adj[b].Add(a);
                }
            }
            var spawn = new Vector2(map.spawn.x, map.spawn.z);
            var heading = new Vector2(Mathf.Sin(map.spawn.heading_deg * Mathf.Deg2Rad), Mathf.Cos(map.spawn.heading_deg * Mathf.Deg2Rad));
            Vector2 P(Vector2Int k) => new Vector2(k.x / 10f, k.y / 10f);
            Vector2Int cur = default;
            float best = float.MaxValue;
            foreach (var k in adj.Keys)
            {
                var d = P(k) - spawn;
                if (Vector2.Dot(d, heading) <= 0) continue;
                if (d.magnitude < best) { best = d.magnitude; cur = k; }
            }
            var path = new List<Vector2> { spawn, P(cur) };
            var seen = new HashSet<Vector2Int> { cur };
            Vector2 dir = (P(cur) - spawn).normalized;
            float length = Vector2.Distance(spawn, P(cur));
            while (length < maxLength)
            {
                Vector2Int pick = default;
                float bestDot = -0.2f;
                bool found = false;
                foreach (var n in adj[cur])
                {
                    if (seen.Contains(n)) continue;
                    float dot = Vector2.Dot((P(n) - P(cur)).normalized, dir);
                    if (dot > bestDot) { bestDot = dot; pick = n; found = true; }
                }
                if (!found) break;
                length += Vector2.Distance(P(cur), P(pick));
                dir = (P(pick) - P(cur)).normalized;
                cur = pick;
                seen.Add(cur);
                path.Add(P(cur));
            }
            return path;
        }
    }
}
