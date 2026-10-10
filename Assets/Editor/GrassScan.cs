using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChastainRush
{
    // Round 16, item C (report only): Dan: "some places the grass/sidewalk causes you to either get stuck or bumps you". His
    // reports this round came with no position, so this looks for likely places in the built scene of Chastain Glen. No Play
    // mode: rays straight down onto the real colliders (triggers ignored), at 0.1 m, along profiles across every street of the
    // area (from the centreline out over the curb, the grass strip, the sidewalk and into the lawn, every 1 m along the street)
    // and along it (in the grass strip, on the sidewalk, in the lawn). Per profile:
    //   STEP   a height jump between two samples 0.1 m apart beyond the grade around it: something a wheel hits; >= 6 cm
    //   CREST  the middle of a 2.6 m chord (a car's wheelbase) standing above the chord's ends: the body's floor can touch
    //          (break-over); >= 12 cm (the cars' floors sit about 15 to 20 cm up)
    //   SAG    the middle under the chord: the nose or tail digs in; >= 12 cm
    //   STEEP  a slope over 1 m steeper than 30 degrees
    //   OTHER  the first thing hit is not a terrain tile (a mesh or box under the lawn or the sidewalk)
    // Clustered (2 m) and written with positions to -grassOut (default Data/screenshots/round16/grass_scan.txt).
    // Batch: Unity.exe -batchmode -nographics -projectPath . -executeMethod ChastainRush.GrassScan.Run [-grassOut <file>]
    public static class GrassScan
    {
        [Serializable] class Road { public string name; public float width; public float[] points; }
        [Serializable] class MapData { public Road[] roads; }
        [Serializable] class Area { public int[] roads; public Bounds bounds; }
        [Serializable] class Bounds { public float x0, x1, z0, z1; }

        const float Dx = 0.1f, Chord = 1.3f, StepLimit = 0.06f, ChordLimit = 0.12f, SteepDeg = 30f;

        struct Ev { public string kind, band, profile, what; public float x, z, value; }

        public static void Run()
        {
            string root = Path.GetDirectoryName(Application.dataPath);
            string outFile = Path.Combine(root, "Data/screenshots/round16/grass_scan.txt");
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-grassOut") outFile = Path.GetFullPath(args[i + 1]);
            try { Scan(root, outFile); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static TerrainCollider[] tiles;
        // the ground's height (the terrain collider: what the wheels ride on in the lawns); 'hit' is the first thing a ray from
        // above meets when that is not the terrain (a house, a trunk, a ramp, a box under the lawn)
        static float Height(Vector2 p, float guess, out Collider hit)
        {
            hit = null;
            var ray = new Ray(new Vector3(p.x, guess + 2.5f, p.y), Vector3.down);
            float y = float.NaN;
            foreach (var t in tiles) if (t.Raycast(ray, out var th, 6f)) { y = th.point.y; break; }
            if (Physics.Raycast(ray, out var first, 6f, ~0, QueryTriggerInteraction.Ignore) && !(first.collider is TerrainCollider) && (float.IsNaN(y) || first.point.y > y + 0.02f)) hit = first.collider;
            return y;
        }

        static void Scan(string root, string outFile)
        {
            EditorSceneManager.OpenScene(MapBuilder.ScenePath);
            foreach (var c in UnityEngine.Object.FindObjectsByType<Collider>()) if (c.attachedRigidbody != null) c.enabled = false;
            Physics.SyncTransforms();
            tiles = UnityEngine.Object.FindObjectsByType<TerrainCollider>();
            var map = JsonUtility.FromJson<MapData>(File.ReadAllText(Path.Combine(root, "Data/map.json")));
            var area = JsonUtility.FromJson<Area>(File.ReadAllText(Path.Combine(root, "Data/neighborhood/chastain_glen_area.json")));
            var terrains = Terrain.activeTerrains;
            float TerrainY(Vector2 p) { foreach (var t in terrains) { var o = t.transform.position; var s = t.terrainData.size; if (p.x >= o.x && p.x <= o.x + s.x && p.y >= o.z && p.y <= o.z + s.z) return t.SampleHeight(new Vector3(p.x, 0, p.y)) + o.y; } return 0f; }

            // every road's carriageway (all of map.json near the area): samples on a road are BumpScan's, not this scan's
            var pave = new List<(Vector2 a, Vector2 b, float half)>();
            foreach (var r in map.roads)
                for (int i = 0; i + 3 < r.points.Length; i += 2)
                {
                    var a = new Vector2(r.points[i], r.points[i + 1]); var b = new Vector2(r.points[i + 2], r.points[i + 3]);
                    if (Mathf.Max(a.x, b.x) < area.bounds.x0 - 20 || Mathf.Min(a.x, b.x) > area.bounds.x1 + 20 || Mathf.Max(a.y, b.y) < area.bounds.z0 - 20 || Mathf.Min(a.y, b.y) > area.bounds.z1 + 20) continue;
                    pave.Add((a, b, r.width * 0.5f));
                }
            bool OnRoad(Vector2 p)
            {
                foreach (var (a, b, h) in pave)
                {
                    var ab = b - a; float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                    if ((a + ab * t - p).sqrMagnitude < (h - 0.6f) * (h - 0.6f)) return true;
                }
                return false;
            }
            var evs = new List<Ev>();
            int profiles = 0;
            // one profile: points p0 + dir * (k * Dx); band(k) names where along it a sample is
            void Profile(Vector2 p0, Vector2 dir, int n, Func<int, string> band, string label, bool lengthwise)
            {
                var y = new float[n]; var what = new Collider[n];
                for (int k = 0; k < n; k++) { var p = p0 + dir * (k * Dx); y[k] = Height(p, TerrainY(p), out what[k]); }
                profiles++;
                int ch = Mathf.RoundToInt(Chord / Dx);
                for (int k = 1; k < n; k++)
                {
                    if (float.IsNaN(y[k]) || float.IsNaN(y[k - 1])) continue;
                    var p = p0 + dir * (k * Dx);
                    string b = band(k);
                    if (b == null || OnRoad(p) || OnRoad(p0 + dir * ((k - 1) * Dx))) continue;
                    // a chord's ends on another road (a junction) are not lawn either
                    bool chordOk = k >= ch && k + ch < n && !OnRoad(p0 + dir * ((k - ch) * Dx)) && !OnRoad(p0 + dir * ((k + ch) * Dx));
                    // the grade around: the median step over +-1 m
                    var near = new List<float>();
                    for (int j = Mathf.Max(1, k - 10); j < Mathf.Min(n, k + 10); j++) if (!float.IsNaN(y[j]) && !float.IsNaN(y[j - 1])) near.Add(y[j] - y[j - 1]);
                    near.Sort(); float grade = near.Count > 0 ? near[near.Count / 2] : 0f;
                    float step = (y[k] - y[k - 1]) - grade;
                    if (Mathf.Abs(step) >= StepLimit) evs.Add(new Ev { kind = "STEP", band = b, profile = label, x = p.x, z = p.y, value = step, what = "terrain" });
                    if (chordOk && !float.IsNaN(y[k - ch]) && !float.IsNaN(y[k + ch]))
                    {
                        float mid = y[k] - (y[k - ch] + y[k + ch]) * 0.5f;
                        if (mid >= ChordLimit) evs.Add(new Ev { kind = "CREST", band = b, profile = label, x = p.x, z = p.y, value = mid, what = "terrain" });
                        if (mid <= -ChordLimit) evs.Add(new Ev { kind = "SAG", band = b, profile = label, x = p.x, z = p.y, value = mid, what = "terrain" });
                    }
                    if (k >= 10 && !float.IsNaN(y[k - 10]))
                    {
                        float deg = Mathf.Atan2(Mathf.Abs(y[k] - y[k - 10]), 1f) * Mathf.Rad2Deg;
                        if (deg > SteepDeg) evs.Add(new Ev { kind = "STEEP", band = b, profile = label, x = p.x, z = p.y, value = deg, what = "terrain" });
                    }
                    if (what[k]) evs.Add(new Ev { kind = "OTHER", band = b, profile = label, x = p.x, z = p.y, value = 0f, what = what[k].name });
                }
            }

            foreach (int ri in area.roads)
            {
                var r = map.roads[ri]; float half = r.width * 0.5f;
                var pts = new List<Vector2>(); for (int i = 0; i + 1 < r.points.Length; i += 2) pts.Add(new Vector2(r.points[i], r.points[i + 1]));
                for (int i = 0; i + 1 < pts.Count; i++)
                {
                    var a = pts[i]; var b = pts[i + 1]; float len = Vector2.Distance(a, b); if (len < 0.5f) continue;
                    var t = (b - a) / len; var nrm = new Vector2(-t.y, t.x);
                    // across: centreline to half + 9 m, both sides, every 1 m along
                    for (float s = 0.5f; s < len; s += 1f)
                        foreach (float side in new[] { -1f, 1f })
                        {
                            var c = a + t * s; int n = Mathf.RoundToInt((half + 9f) / Dx);
                            Profile(c, nrm * side, n, k =>
                            {
                                float off = k * Dx - half;   // from the road's edge
                                if (off < -0.6f) return null;           // the road itself (BumpScan's job)
                                if (off < 0.4f) return "curb";
                                if (off < 1.25f) return "grass strip";
                                if (off < 2.65f) return "sidewalk";
                                return "lawn";
                            }, $"across {r.name}", false);
                        }
                    // along: in the grass strip, on the sidewalk, in the lawn
                    foreach (float side in new[] { -1f, 1f })
                        foreach (var (off, nm) in new[] { (0.7f, "grass strip"), (1.95f, "sidewalk"), (4f, "lawn") })
                        {
                            var p0 = a + nrm * side * (half + off); int n = Mathf.RoundToInt(len / Dx);
                            if (n < 30) continue;
                            Profile(p0, t, n, k => nm, $"along {r.name}", true);
                        }
                }
            }

            // keep what lies inside the area, away from other roads' carriageways is not checked (junctions show up: listed)
            evs = evs.Where(e => e.x >= area.bounds.x0 && e.x <= area.bounds.x1 && e.z >= area.bounds.z0 && e.z <= area.bounds.z1).ToList();
            // cluster per kind
            var clusters = new List<(string kind, List<Ev> evs)>();
            foreach (var e in evs.OrderBy(e => e.kind).ThenBy(e => e.x))
            {
                var hit = clusters.FirstOrDefault(c => c.kind == e.kind && c.evs.Any(o => (o.x - e.x) * (o.x - e.x) + (o.z - e.z) * (o.z - e.z) < 4f));
                if (hit.evs != null) hit.evs.Add(e); else clusters.Add((e.kind, new List<Ev> { e }));
            }
            var sb = new StringBuilder();
            sb.AppendLine($"Round 16 item C grass / sidewalk scan, {DateTime.Now:yyyy-MM-dd HH:mm}, scene {MapBuilder.ScenePath}");
            sb.AppendLine($"{profiles} profiles over {area.roads.Length} streets of Chastain Glen; limits: step {StepLimit * 100:0} cm in 10 cm, crest / sag {ChordLimit * 100:0} cm over a {Chord * 2:0.0} m chord, steep {SteepDeg:0} deg over 1 m");
            foreach (var g in clusters.GroupBy(c => c.kind)) sb.AppendLine($"  {g.Key}: {g.Count()} places ({g.Sum(c => c.evs.Count)} samples)");
            foreach (var g in clusters.GroupBy(c => c.kind))
            {
                sb.AppendLine(); sb.AppendLine($"== {g.Key} (largest first)");
                foreach (var c in g.OrderByDescending(c => c.evs.Max(e => Mathf.Abs(e.value))).ThenByDescending(c => c.evs.Count))
                {
                    var m = c.evs.OrderByDescending(e => Mathf.Abs(e.value)).First();
                    var bands = string.Join("/", c.evs.Select(e => e.band).Distinct());
                    var whats = string.Join("/", c.evs.Select(e => (e.what ?? "-").Length > 60 ? e.what.Substring(0, 60) : e.what).Distinct().Take(3));
                    sb.AppendLine($"x {m.x:0.0} z {m.z:0.0}  {g.Key} {m.value:+0.00;-0.00}  samples {c.evs.Count}  {bands}  {m.profile}  hit {whats}");
                }
            }
            Directory.CreateDirectory(Path.GetDirectoryName(outFile));
            File.WriteAllText(outFile, sb.ToString());
            Debug.Log("[GrassScan] " + outFile + "\n" + string.Join("\n", sb.ToString().Split('\n').Take(10)));
        }
    }
}
