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
    // Development check (Round 4): where can a car on a road get a bump? Opens the built scene (no Play mode needed),
    // walks every road and bridge deck from map.json and casts rays straight down onto the real colliders, every 0.25 m
    // along the centreline and along both wheel tracks (+-0.85 m, the car's suspension points). It records
    //   STEP     a height jump between neighbouring samples beyond the local grade (the grade is the median step over
    //            +-1.5 m, so a steep but smooth road is not flagged); threshold 3 cm
    //   HUMP     a crest or dip: height against the chord between the points 3 m behind and 3 m ahead; threshold 10 cm
    //   COLLIDER the first collider hit is not the road surface (not a terrain tile on a road, not a bridge deck on a bridge)
    //   OBSTRUCT a non-terrain collider inside the car's lane corridor (+-1.0 m, 0.5-1.5 m above the surface)
    //   MISS     nothing under the sample
    // and for each event whether it sits at a 128 m road-chunk boundary, a junction, a bridge end, or a terrain tile seam.
    // Batch: Unity.exe -batchmode -nographics -projectPath . -executeMethod ChastainRush.BumpScan.Run [-bumpOut Data/bumps.txt]
    public static class BumpScan
    {
        [Serializable] class Road { public string name; public string @class; public float width; public bool oneway; public float[] points; }
        [Serializable] class Bridge { public string name; public string @class; public float width; public float[] points; public float[] deck_y; }
        [Serializable] class MapData { public Road[] roads; public Bridge[] bridges; }
        [Serializable] class TerrainMeta { public float tile_w_m; public float tile_d_m; public int tiles_x; public int tiles_z; public float origin_x_m, origin_z_m; }

        const float RayHeight = 1.5f; // rays start this far above the expected surface: above the car's roof, below a house's eaves
        const float Step = 0.25f, TrackOffset = 0.85f, StepLimit = 0.03f, HumpLimit = 0.10f, HumpHalf = 3f, ChunkCell = 128f;
        const float ClusterGap = 2.0f;

        sealed class Seg { public Vector2 a, b; public float half; public int road; }

        struct Ev
        {
            public string kind, road, collider, track, features;
            public float x, z, s, value;
            public bool bridge;
        }

        public static void Run()
        {
            string root = Path.GetDirectoryName(Application.dataPath);
            string outFile = Path.Combine(root, "Data/bumps.txt");
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-bumpOut") outFile = Path.GetFullPath(args[i + 1]);
            try { Scan(root, outFile); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static void Scan(string root, string outFile)
        {
            EditorSceneManager.OpenScene(MapBuilder.ScenePath);
            Physics.SyncTransforms();
            var map = JsonUtility.FromJson<MapData>(File.ReadAllText(Path.Combine(root, "Data/map.json")));
            var meta = JsonUtility.FromJson<TerrainMeta>(File.ReadAllText(Path.Combine(root, File.Exists(Path.Combine(root, "Assets/Generated/terrain_full.json")) ? "Assets/Generated/terrain_full.json" : "Assets/Generated/terrain.json")));

            // the player car is part of the scene but not part of the road
            foreach (var c in UnityEngine.Object.FindObjectsByType<Collider>()) if (c.attachedRigidbody != null) c.enabled = false;
            Physics.SyncTransforms();

            // collider inventory (what the car can actually touch)
            var inventory = new SortedDictionary<string, int>();
            foreach (var c in UnityEngine.Object.FindObjectsByType<Collider>())
            {
                if (!c.enabled) continue;
                string k = c.name.StartsWith("Terrain_") ? "Terrain tiles" : c.name.StartsWith("Buildings_") ? "Buildings_* chunks" : c.name;
                k += " (" + c.GetType().Name + ")";
                inventory[k] = inventory.TryGetValue(k, out var n) ? n + 1 : 1;
            }

            // other roads for the junction test, and bridge end points
            var segs = new List<Seg>();
            var grid = new Dictionary<Vector2Int, List<int>>();
            const float cell = 32f;
            var names = new List<string>();
            void AddPolyline(float[] pts, float width, string name)
            {
                int ri = names.Count;
                names.Add(name);
                for (int i = 0; i + 3 < pts.Length; i += 2)
                {
                    var s = new Seg { a = new Vector2(pts[i], pts[i + 1]), b = new Vector2(pts[i + 2], pts[i + 3]), half = width * 0.5f, road = ri };
                    int id = segs.Count;
                    segs.Add(s);
                    float pad = s.half + 3f;
                    for (int gx = Mathf.FloorToInt((Mathf.Min(s.a.x, s.b.x) - pad) / cell); gx <= Mathf.FloorToInt((Mathf.Max(s.a.x, s.b.x) + pad) / cell); gx++)
                        for (int gz = Mathf.FloorToInt((Mathf.Min(s.a.y, s.b.y) - pad) / cell); gz <= Mathf.FloorToInt((Mathf.Max(s.a.y, s.b.y) + pad) / cell); gz++)
                        {
                            var key = new Vector2Int(gx, gz);
                            if (!grid.TryGetValue(key, out var l)) grid[key] = l = new List<int>();
                            l.Add(id);
                        }
                }
            }
            foreach (var r in map.roads) AddPolyline(r.points, r.width, r.name);
            var bridgeEnds = new List<Vector2>();
            foreach (var b in map.bridges)
            {
                AddPolyline(b.points, b.width, b.name);
                bridgeEnds.Add(new Vector2(b.points[0], b.points[1]));
                bridgeEnds.Add(new Vector2(b.points[b.points.Length - 2], b.points[b.points.Length - 1]));
            }
            // does the point lie within another road's pavement (plus 3 m)? self = index into 'names'
            bool NearOther(Vector2 p, int self, out string other)
            {
                other = null;
                if (!grid.TryGetValue(new Vector2Int(Mathf.FloorToInt(p.x / cell), Mathf.FloorToInt(p.y / cell)), out var l)) return false;
                foreach (int id in l)
                {
                    var s = segs[id];
                    if (s.road == self || names[s.road] == names[self]) continue;
                    var ab = s.b - s.a;
                    float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - s.a, ab) / ab.sqrMagnitude);
                    if ((s.a + ab * t - p).magnitude < s.half + 3f) { other = names[s.road]; return true; }
                }
                return false;
            }

            // the I-575 interchange (highway-class roads and the Chastain Road deck over them) is out of scope this round; tag it
            var highway = new List<(Vector2 a, Vector2 b)>();
            void AddHighway(float[] pts) { for (int i = 0; i + 3 < pts.Length; i += 2) highway.Add((new Vector2(pts[i], pts[i + 1]), new Vector2(pts[i + 2], pts[i + 3]))); }
            foreach (var r in map.roads) if (r.@class == "motorway" || r.@class == "trunk" || r.@class.EndsWith("_link") && (r.name ?? "").Contains("Highway")) AddHighway(r.points);
            foreach (var b in map.bridges) if (b.name != "New Chastain Road") AddHighway(b.points);
            bool NearHighway(Vector2 p)
            {
                foreach (var (a, b) in highway)
                {
                    var ab = b - a;
                    float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                    if ((a + ab * t - p).sqrMagnitude < 100f * 100f) return true;
                }
                return false;
            }

            var terrainExtent = GroundQuery.Extent();
            var events = new List<Ev>();
            var baseline = new SortedDictionary<string, int>();
            int baselineStations = 0;
            int roadIndex = 0;
            long samples = 0, rays = 0;
            double totalLength = 0, bridgeLength = 0;
            int roadCount = 0;
            var all = new List<(string name, float[] pts, float[] deck, float width)>();
            foreach (var r in map.roads) all.Add((r.name ?? "(unnamed)", r.points, null, r.width));
            foreach (var b in map.bridges) all.Add((b.name ?? "(unnamed)", b.points, b.deck_y, b.width));

            foreach (var item in all)
            {
                int selfIdx = roadIndex++;
                bool bridge = item.deck != null;
                // points of this polyline in 2D, with cumulative length and (bridges) deck height
                var p2 = new List<Vector2>();
                for (int i = 0; i + 1 < item.pts.Length; i += 2) p2.Add(new Vector2(item.pts[i], item.pts[i + 1]));
                if (p2.Count < 2) continue;
                var cum = new float[p2.Count];
                for (int i = 1; i < p2.Count; i++) cum[i] = cum[i - 1] + Vector2.Distance(p2[i - 1], p2[i]);
                float total = cum[cum.Length - 1];
                if (total < 1f) continue;
                roadCount++;
                totalLength += total;
                if (bridge) bridgeLength += total;
                int n = Mathf.Max(2, Mathf.CeilToInt(total / Step) + 1);

                var pos = new Vector2[n];
                var tan = new Vector2[n];
                var h = new float[3][];     // 0 left track, 1 centre, 2 right track
                var firstHit = new string[3][];
                for (int t = 0; t < 3; t++) { h[t] = new float[n]; firstHit[t] = new string[n]; }
                var obstruct = new string[n];
                int seg = 0;
                for (int i = 0; i < n; i++)
                {
                    float s = Mathf.Min(total, i * total / (n - 1));
                    while (seg + 2 < p2.Count && cum[seg + 1] < s) seg++;
                    float f = Mathf.Clamp01((s - cum[seg]) / Mathf.Max(cum[seg + 1] - cum[seg], 1e-4f));
                    pos[i] = Vector2.Lerp(p2[seg], p2[seg + 1], f);
                    tan[i] = (p2[seg + 1] - p2[seg]).normalized;
                }
                // smooth tangents over a few samples so a polyline corner does not swing the wheel tracks sideways
                var tanS = new Vector2[n];
                for (int i = 0; i < n; i++)
                {
                    var sum = Vector2.zero;
                    for (int k = Mathf.Max(0, i - 4); k <= Mathf.Min(n - 1, i + 4); k++) sum += tan[k];
                    tanS[i] = sum.sqrMagnitude > 1e-6f ? sum.normalized : tan[i];
                }

                for (int i = 0; i < n; i++)
                {
                    var nrm = new Vector2(-tanS[i].y, tanS[i].x);
                    // expected surface height, to start the ray above it
                    float s = Mathf.Min(total, i * total / (n - 1));
                    float expect;
                    if (bridge)
                    {
                        int k = Array.BinarySearch(cum, s);
                        if (k < 0) k = ~k;
                        k = Mathf.Clamp(k, 1, cum.Length - 1);
                        float f = Mathf.Clamp01((s - cum[k - 1]) / Mathf.Max(cum[k] - cum[k - 1], 1e-4f));
                        expect = Mathf.Lerp(item.deck[k - 1], item.deck[k], f);
                    }
                    else
                    {
                        expect = GroundQuery.TryHeight(pos[i].x, pos[i].y, out var gy) ? gy : float.NaN;
                    }
                    for (int t = 0; t < 3; t++)
                    {
                        var q = pos[i] + nrm * ((t - 1) * TrackOffset);
                        float top = (float.IsNaN(expect) ? 400f : expect) + RayHeight;
                        rays++;
                        h[t][i] = float.NaN;
                        firstHit[t][i] = "MISS";
                        var o = new Vector3(q.x, top, q.y);
                        if (Physics.Raycast(o, Vector3.down, out var hit, 600f, ~0, QueryTriggerInteraction.Ignore))
                        {
                            h[t][i] = hit.point.y;
                            firstHit[t][i] = hit.collider.name;
                        }
                    }
                    // lane corridor: anything solid that is not the ground or this bridge's own deck, 0.5-1.5 m above the surface
                    if (!float.IsNaN(h[1][i]) && i % 4 == 0)
                    {
                        var centre = new Vector3(pos[i].x, h[1][i] + 1.0f, pos[i].y);
                        var rot = Quaternion.LookRotation(new Vector3(tanS[i].x, 0f, tanS[i].y));
                        var cols = Physics.OverlapBox(centre, new Vector3(1.0f, 0.5f, 0.5f), rot, ~0, QueryTriggerInteraction.Ignore);
                        foreach (var c in cols)
                        {
                            if (c.name.StartsWith("Terrain_")) continue;
                            obstruct[i] = c.name;
                            break;
                        }
                    }
                    samples++;
                    if (i % 4 == 0)
                    {
                        // how common each feature is along all roads, to compare with the features of the bumps
                        baselineStations++;
                        var probe = Features(new Ev { x = pos[i].x, z = pos[i].y, s = s }, selfIdx, meta, bridgeEnds, NearOther, NearHighway, total, bridge);
                        foreach (var f in probe.features.Split(',')) { var key = f.StartsWith("junction") ? "junction" : f; baseline[key] = baseline.TryGetValue(key, out var c0) ? c0 + 1 : 1; }
                    }
                }

                // ---- analyse each track
                var local = new List<Ev>();
                string[] trackName = { "left", "centre", "right" };
                var tmp = new float[13];
                for (int t = 0; t < 3; t++)
                {
                    var ht = h[t];
                    // 1. steps against the local grade
                    for (int i = 6; i < n - 7; i++)
                    {
                        bool ok = true;
                        for (int k = i - 6; k <= i + 7; k++) if (float.IsNaN(ht[k])) { ok = false; break; }
                        if (!ok) continue;
                        float d = ht[i + 1] - ht[i];
                        int c = 0;
                        for (int k = i - 6; k <= i + 6; k++) if (k != i) tmp[c++] = ht[k + 1] - ht[k];
                        Array.Sort(tmp, 0, c);
                        float grade = 0.5f * (tmp[c / 2 - 1] + tmp[c / 2]);
                        float step = d - grade;
                        if (Mathf.Abs(step) > StepLimit)
                            local.Add(new Ev { kind = "STEP", x = pos[i].x, z = pos[i].y, s = i * total / (n - 1), value = step, track = trackName[t] });
                    }
                    // 2. humps and dips against the chord
                    int hh = Mathf.RoundToInt(HumpHalf / Step);
                    for (int i = hh; i < n - hh; i++)
                    {
                        if (float.IsNaN(ht[i - hh]) || float.IsNaN(ht[i]) || float.IsNaN(ht[i + hh])) continue;
                        float sag = ht[i] - 0.5f * (ht[i - hh] + ht[i + hh]);
                        if (Mathf.Abs(sag) > HumpLimit)
                            local.Add(new Ev { kind = "HUMP", x = pos[i].x, z = pos[i].y, s = i * total / (n - 1), value = sag, track = trackName[t] });
                    }
                    // 3. wrong first collider, and holes
                    for (int i = 0; i < n; i++)
                    {
                        string hit = firstHit[t][i];
                        bool good = hit == "MISS" ? false : bridge ? hit == "BridgeDecks" : hit.StartsWith("Terrain_");
                        // a ray that misses exactly on the map's outer edge is a road leaving the map, not a hole
                        if (hit == "MISS" && !InsideExtent(terrainExtent, pos[i], 0.8f)) continue;
                        if (!good)
                            local.Add(new Ev { kind = hit == "MISS" ? "MISS" : "COLLIDER", x = pos[i].x, z = pos[i].y, s = i * total / (n - 1), value = 0f, track = trackName[t], collider = hit });
                    }
                }
                for (int i = 0; i < n; i++)
                    if (obstruct[i] != null && !(bridge && (obstruct[i] == "BridgeDecks")))
                        local.Add(new Ev { kind = "OBSTRUCT", x = pos[i].x, z = pos[i].y, s = i * total / (n - 1), value = 0f, track = "corridor", collider = obstruct[i] });

                // ---- cluster events of the same kind within ClusterGap metres (any track) into one, keep the largest
                foreach (var grp in local.GroupBy(e => e.kind))
                {
                    var list = grp.OrderBy(e => e.s).ToList();
                    int start = 0;
                    for (int i = 1; i <= list.Count; i++)
                    {
                        if (i < list.Count && list[i].s - list[i - 1].s <= ClusterGap) continue;
                        var best = list[start];
                        var colliders = new HashSet<string>();
                        for (int k = start; k < i; k++)
                        {
                            if (Mathf.Abs(list[k].value) > Mathf.Abs(best.value)) best = list[k];
                            if (list[k].collider != null) colliders.Add(list[k].collider);
                        }
                        best.road = item.name;
                        best.bridge = bridge;
                        best.collider = colliders.Count > 0 ? string.Join("+", colliders) : (best.kind == "STEP" || best.kind == "HUMP") ? FirstHitName(best, firstHit, n, total) : null;
                        events.Add(Features(best, selfIdx, meta, bridgeEnds, NearOther, NearHighway, total, bridge));
                        start = i;
                    }
                }
            }

            Write(outFile, events, inventory, roadCount, totalLength, bridgeLength, samples, rays, map, baseline, baselineStations);
        }

        // name of the collider under the centre track at the event (for STEP and HUMP events)
        static string FirstHitName(Ev e, string[][] firstHit, int n, float total)
        {
            int i = Mathf.Clamp(Mathf.RoundToInt(e.s / total * (n - 1)), 0, n - 1);
            return firstHit[1][i];
        }

        static bool InsideExtent(Rect r, Vector2 p, float margin) => p.x > r.xMin + margin && p.x < r.xMax - margin && p.y > r.yMin + margin && p.y < r.yMax - margin;

        delegate bool NearOtherFn(Vector2 p, int self, out string other);

        static Ev Features(Ev e, int selfIdx, TerrainMeta meta, List<Vector2> bridgeEnds, NearOtherFn nearOther, Func<Vector2, bool> nearHighway, float roadLength, bool bridge)
        {
            var tags = new List<string>();
            // road chunks are 128 m cells (visual only)
            float cx = Mathf.Abs(e.x - Mathf.Round(e.x / ChunkCell) * ChunkCell), cz = Mathf.Abs(e.z - Mathf.Round(e.z / ChunkCell) * ChunkCell);
            if (Mathf.Min(cx, cz) < 1.5f) tags.Add("chunk-boundary");
            // terrain tile seams
            // (interior seams only; the map's outer edge is not a seam)
            float kx = Mathf.Round((e.x - meta.origin_x_m) / meta.tile_w_m), kz = Mathf.Round((e.z - meta.origin_z_m) / meta.tile_d_m);   // Round 59: the grid's origin
            float sx = kx >= 1 && kx <= meta.tiles_x - 1 ? Mathf.Abs(e.x - meta.origin_x_m - kx * meta.tile_w_m) : 1e9f, sz = kz >= 1 && kz <= meta.tiles_z - 1 ? Mathf.Abs(e.z - meta.origin_z_m - kz * meta.tile_d_m) : 1e9f;
            if (Mathf.Min(sx, sz) < 1.5f) tags.Add("tile-seam");
            if (nearHighway(new Vector2(e.x, e.z))) tags.Add("i575-zone");
            if (nearOther(new Vector2(e.x, e.z), selfIdx, out var other)) tags.Add("junction:" + other);
            foreach (var be in bridgeEnds) if (Vector2.Distance(be, new Vector2(e.x, e.z)) < 8f) { tags.Add("bridge-end"); break; }
            if (e.s < 3f || roadLength - e.s < 3f) tags.Add("road-end");
            e.features = tags.Count == 0 ? "-" : string.Join(",", tags);
            return e;
        }

        static void Write(string outFile, List<Ev> events, SortedDictionary<string, int> inventory, int roadCount, double totalLength, double bridgeLength, long samples, long rays, MapData map, SortedDictionary<string, int> baseline, int baselineStations)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Road-surface scan (Assets/Editor/BumpScan.cs). Rays straight down onto the scene's real colliders every 0.25 m along the centreline and both wheel tracks (+-0.85 m).");
            sb.AppendLine($"{roadCount} road and bridge pieces, {totalLength / 1000:F1} km ({bridgeLength:F0} m of it bridge deck), {samples} stations, {rays} rays.");
            sb.AppendLine("Colliders in the scene: " + string.Join("; ", inventory.Select(kv => kv.Value + " x " + kv.Key)));
            sb.AppendLine("STEP = jump between neighbouring samples beyond the local grade (>3 cm). HUMP = crest/dip against the chord +-3 m (>10 cm). COLLIDER = first hit is not the road surface. OBSTRUCT = solid object in the lane corridor. MISS = nothing below.");
            sb.AppendLine("value = metres (STEP: jump per 0.25 m; HUMP: positive crest, negative dip). One row per cluster (same kind, within 2 m), largest sample shown.");
            sb.AppendLine();
            sb.AppendLine("SUMMARY");
            foreach (var g in events.GroupBy(e => e.kind).OrderBy(g => g.Key))
                sb.AppendLine($"  {g.Key}: {g.Count()} clusters on {g.Select(e => e.road).Distinct().Count()} road names" +
                              (g.Key == "STEP" || g.Key == "HUMP" ? $", largest {g.Max(e => Mathf.Abs(e.value)):F3} m, median {Median(g.Select(e => Mathf.Abs(e.value)).ToList()):F3} m" : ""));
            var scoped = events.Where(e => !e.features.Contains("i575-zone")).ToList();
            sb.AppendLine($"  outside the I-575 zone (100 m of a highway-class road or of an I-575 deck): " + string.Join(", ", scoped.GroupBy(e => e.kind).OrderBy(g => g.Key).Select(g => g.Key + " " + g.Count())));
            sb.AppendLine($"  serious, outside the I-575 zone (STEP >= 10 cm, HUMP >= 30 cm, any COLLIDER/OBSTRUCT/MISS): STEP {scoped.Count(e => e.kind == "STEP" && Mathf.Abs(e.value) >= 0.10f)}, " +
                          $"HUMP {scoped.Count(e => e.kind == "HUMP" && Mathf.Abs(e.value) >= 0.30f)}, COLLIDER {scoped.Count(e => e.kind == "COLLIDER")}, OBSTRUCT {scoped.Count(e => e.kind == "OBSTRUCT")}, MISS {scoped.Count(e => e.kind == "MISS")}");
            int N(string kind, float min, IEnumerable<Ev> src) => src.Count(e => e.kind == kind && Mathf.Abs(e.value) >= min);
            sb.AppendLine("  outside the I-575 zone, by size: STEP >=3 cm " + N("STEP", 0.03f, scoped) + ", >=5 cm " + N("STEP", 0.05f, scoped) + ", >=10 cm " + N("STEP", 0.10f, scoped) + ", >=20 cm " + N("STEP", 0.20f, scoped) +
                          "; HUMP >=10 cm " + N("HUMP", 0.10f, scoped) + ", >=20 cm " + N("HUMP", 0.20f, scoped) + ", >=30 cm " + N("HUMP", 0.30f, scoped) + ", >=50 cm " + N("HUMP", 0.50f, scoped));
            sb.AppendLine("  features of all clusters (a cluster can have several):");
            var featureCount = new SortedDictionary<string, int>();
            foreach (var e in events)
                foreach (var f in e.features.Split(',')) { var key = f.StartsWith("junction") ? "junction" : f; featureCount[key] = featureCount.TryGetValue(key, out var c) ? c + 1 : 1; }
            foreach (var kv in featureCount) sb.AppendLine($"    {kv.Key}: {kv.Value} of {events.Count} clusters ({100f * kv.Value / events.Count:F0}%); along all roads (every metre): {100f * (baseline.TryGetValue(kv.Key, out var bl) ? bl : 0) / baselineStations:F1}%");
            sb.AppendLine();
            sb.AppendLine("kind\tx\tz\ts(m along piece)\tvalue(m)\ttrack\troad\tbridge\tcollider hit\tfeatures");
            foreach (var e in events.OrderBy(e => e.kind).ThenByDescending(e => Mathf.Abs(e.value)))
                sb.AppendLine($"{e.kind}\t{e.x:F1}\t{e.z:F1}\t{e.s:F1}\t{e.value:F3}\t{e.track}\t{e.road}\t{(e.bridge ? "deck" : "")}\t{e.collider ?? "-"}\t{e.features}");
            Directory.CreateDirectory(Path.GetDirectoryName(outFile));
            File.WriteAllText(outFile, sb.ToString());
            Debug.Log("[BumpScan] wrote " + outFile + "\n" + sb.ToString().Split(new[] { "kind\tx" }, StringSplitOptions.None)[0]);
        }

        // a crest of 'sag' metres over a +-3 m chord has curvature k = 2 sag / 9; a car leaves the road above sqrt(g / k)
        static string LaunchSpeed(Ev e) => e.kind == "HUMP" && e.value > 0f ? Mathf.Sqrt(9.81f * 9f / (2f * e.value)).ToString("F0") : "-";

        static float Median(List<float> v) { if (v.Count == 0) return 0f; v.Sort(); return v[v.Count / 2]; }
    }
}
