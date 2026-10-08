using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChastainRush
{
    // Development check: opens the generated scene, enters Play mode and lets DriveCheckRunner drive the car along a
    // street. Writes drive_log.tsv and screenshots to the folder given by -driveCheckOut (default Temp/DriveCheck).
    // Batch: Unity.exe -batchmode -projectPath . -executeMethod ChastainRush.DriveCheck.Run [-driveCheckOut <dir>] [-driveBridge <n>]
    // Without -driveBridge the car drives the spawn street from the spawn point (the Round 1 check). With
    // -driveBridge n it starts ~120 m before bridge n (index into map.json "bridges") and drives over it and 120 m past it.
    public static partial class DriveCheck
    {
        [Serializable] internal class Road { public string name; public float[] points; }
        [Serializable] internal class Bridge { public string name; public float[] points; public float[] deck_y; }
        [Serializable] internal class Spawn { public float x; public float z; public float heading_deg; public string street; }
        [Serializable] internal class MapData { public Road[] roads; public Bridge[] bridges; public Spawn spawn; }

        public static void Run()
        {
            string outDir = Path.GetFullPath("Temp/DriveCheck");
            int bridgeIndex = -1;
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-driveCheckOut") outDir = args[i + 1];
                if (args[i] == "-driveBridge") bridgeIndex = int.Parse(args[i + 1]);
            }
            Directory.CreateDirectory(outDir);

            EditorSceneManager.OpenScene(MapBuilder.ScenePath);
            var map = JsonUtility.FromJson<MapData>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "Data/map.json")));
            var runner = new GameObject("DriveCheck").AddComponent<DriveCheckRunner>();
            runner.outputDir = outDir;

            string street = map.spawn.street;
            var start = new Vector2(map.spawn.x, map.spawn.z);
            float h = map.spawn.heading_deg * Mathf.Deg2Rad;
            var heading = new Vector2(Mathf.Sin(h), Mathf.Cos(h));
            float length = 1500f;
            if (bridgeIndex >= 0)
            {
                var b = map.bridges[bridgeIndex];
                street = b.name;
                var first = new Vector2(b.points[0], b.points[1]);
                var last = new Vector2(b.points[b.points.Length - 2], b.points[b.points.Length - 1]);
                var dir = (last - first).normalized;
                var back = StreetPath(map, street, first, -dir, 120f);
                start = back[back.Count - 1];
                heading = (back[back.Count - 2] - start).normalized;
                length = 120f + (last - first).magnitude + 120f;
                runner.duration = 60f;
                Debug.Log($"[DriveCheck] bridge {bridgeIndex}: {street}, deck from ({first.x:F0},{first.y:F0}) to ({last.x:F0},{last.y:F0}), start ({start.x:F0},{start.y:F0})");
            }
            var car = GameObject.FindAnyObjectByType<Racer.ArcadeVehicle>();
            float yaw = Mathf.Atan2(heading.x, heading.y) * Mathf.Rad2Deg;
            car.transform.SetPositionAndRotation(new Vector3(start.x, GroundQuery.Height(start.x, start.y) + 0.8f, start.y), Quaternion.Euler(0f, yaw, 0f));
            foreach (var p in StreetPath(map, street, start, heading, length))
                runner.path.Add(new Vector3(p.x, GroundQuery.Height(p.x, p.y), p.y));
            Debug.Log($"[DriveCheck] path of {runner.path.Count} waypoints along {street}; output {outDir}");
            EditorApplication.EnterPlaymode();
        }

        // Walks the named street's ways (ordinary road pieces and bridge decks alike) from 'spawn' in 'heading', at each
        // junction taking the continuation of the same street that bends least, until maxLength metres or a dead end.
        internal static List<Vector2> StreetPath(MapData map, string street, Vector2 spawn, Vector2 heading, float maxLength)
        {
            var adj = new Dictionary<Vector2Int, List<Vector2Int>>();
            Vector2Int Key(Vector2 p) => new Vector2Int(Mathf.RoundToInt(p.x * 10), Mathf.RoundToInt(p.y * 10));
            void AddLine(float[] pts)
            {
                for (int i = 0; i + 3 < pts.Length; i += 2)
                {
                    var a = Key(new Vector2(pts[i], pts[i + 1]));
                    var b = Key(new Vector2(pts[i + 2], pts[i + 3]));
                    if (!adj.ContainsKey(a)) adj[a] = new List<Vector2Int>();
                    if (!adj.ContainsKey(b)) adj[b] = new List<Vector2Int>();
                    adj[a].Add(b);
                    adj[b].Add(a);
                }
            }
            foreach (var r in map.roads) if (r.name == street) AddLine(r.points);
            foreach (var b in map.bridges) if (b.name == street) AddLine(b.points);
            Vector2 P(Vector2Int k) => new Vector2(k.x / 10f, k.y / 10f);
            Vector2Int cur = default;
            float best = float.MaxValue;
            foreach (var k in adj.Keys)
            {
                var d = P(k) - spawn;
                if (Vector2.Dot(d, heading) <= 0 && d.magnitude > 0.2f) continue;
                if (d.magnitude < best) { best = d.magnitude; cur = k; }
            }
            var path = new List<Vector2> { spawn, P(cur) };
            var seen = new HashSet<Vector2Int> { cur };
            Vector2 dir = (P(cur) - spawn).sqrMagnitude > 0.01f ? (P(cur) - spawn).normalized : heading;
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
