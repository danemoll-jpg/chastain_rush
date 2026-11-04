using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Round 91 (0, Dan's photo after The Big One's landing: "Not sure what this is supposed to be and not really obvious where the track goes from here").
//   the dropped roads   the real quarry's service road down into the pit (OSM 1173718687 west of its junction at (-1946, -1160)) and its branch
//                       (1173718688) lay draped over the race's graded landing slopes and benches: a road climbing a steep fill bank, "a huge plain
//                       grey wedge". Data/tracks/viking_quarry.json "drop_roads" (Tools/mapgen/make_quarry_route.py): a road with keep_to is cut
//                       there (the rest of it is not built); one without is not drawn at all (its entry stays: other data refers to roads by index)
//   the run-out posts   reflector posts along both edges of each jump's run-out (the gravel from the landing deck to the bench: MapBuilder.QuarryRace.cs
//                       surfaces "... run-out"), so the way on reads from where you land; not solid (a wide landing comes down among them)
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class R91Drop { public long osm_id; public float[] keep_to; }
        [Serializable] class R91File { public R91Drop[] drop_roads; }
        static readonly HashSet<long> R91Hidden = new HashSet<long>();

        // (the planning's start, before anything samples the roads)
        static void R91DropRoads(MapData map)
        {
            R91Hidden.Clear();
            string path = Path.Combine(ProjectRoot, QuarryRaceFile); if (!File.Exists(path)) return;
            var f = JsonUtility.FromJson<R91File>(File.ReadAllText(path)); if (f?.drop_roads == null) return;
            var log = new List<string>();
            foreach (var d in f.drop_roads)
            {
                var r = map.roads.FirstOrDefault(q => q.osm_id == d.osm_id); if (r == null || r.points == null) { log.Add($"{d.osm_id} not found"); continue; }
                if (d.keep_to == null || d.keep_to.Length < 2) { R91Hidden.Add(d.osm_id); log.Add($"{d.osm_id} not drawn"); continue; }
                var p = ToPoints(r.points); var k = new Vector2(d.keep_to[0], d.keep_to[1]);
                int best = 0; for (int i = 1; i < p.Count; i++) if ((p[i] - k).sqrMagnitude < (p[best] - k).sqrMagnitude) best = i;
                if (best < 1) { R91Hidden.Add(d.osm_id); continue; }
                r.points = p.Take(best + 1).SelectMany(q => new[] { q.x, q.y }).ToArray();
                log.Add($"{d.osm_id} cut at ({p[best].x:0}, {p[best].y:0}): {best + 1} of {p.Count} points kept");
            }
            Debug.Log("[Round91] the quarry's dropped roads: " + string.Join("; ", log));
        }
        static bool R91Drawn(long osmId) => !R91Hidden.Contains(osmId);

        // Round 91 (C): a launch pipe's mouth (an exit with launch_to): the geyser at the cut's mouth that throws a vehicle coming out up onto the rim
        static void BuildPipeLaunches(Transform root, Network n)
        {
            int k = 0;
            foreach (var x in n.F.exits)
            {
                if (x.launch_to == null || x.launch_to.Length < 3 || !n.Built(n.F.tubes.First(t => t.id == x.tube))) continue;
                var a = new Vector2(x.a[0], x.a[1]); var b = new Vector2(x.b[0], x.b[1]); var o = (a - b).normalized;
                var go = new GameObject("Pipe launch " + x.id); go.transform.SetParent(root, false);
                var q = b + o * Mathf.Min(6f, (a - b).magnitude * .4f); go.transform.position = new Vector3(q.x, x.b_floor, q.y);   // just out of the headwall, in the open cut
                var pl = go.AddComponent<PipeLaunch>(); pl.outward = new Vector3(o.x, 0f, o.y); pl.launchName = x.name.ToLowerInvariant();
                pl.target = new Vector3(x.launch_to[0], x.launch_to[1], x.launch_to[2]);
                pl.landDir = x.launch_dir != null && x.launch_dir.Length >= 2 ? new Vector3(x.launch_dir[0], 0f, x.launch_dir[1]).normalized : pl.outward;
                if (x.launch_angle > 0f) pl.angleDeg = x.launch_angle;
                pl.reach = Mathf.Max(6f, x.d + 2f); k++;
            }
            Debug.Log($"[Round91] {k} launch pipes");
        }

        [Serializable] class R91Markers { public QrLine[] markers; }
        static void BuildRunOutPosts(TerrainGrid terrain, Materials mats, Transform parent)
        {
            string path = Path.Combine(ProjectRoot, QuarryRaceFile); if (!File.Exists(path)) return;
            var f = JsonUtility.FromJson<R91Markers>(File.ReadAllText(path)); if (f?.markers == null) return;
            var mb = new MeshBuilder(); int n = 0;
            foreach (var m in f.markers)
            {
                var line = ToPoints(m.points); if (line.Count < 2) continue;
                var cum = new List<float> { 0f }; for (int i = 1; i < line.Count; i++) cum.Add(cum[i - 1] + (line[i] - line[i - 1]).magnitude);
                for (float s = 0f; s <= cum[cum.Count - 1]; s += 12f)
                {
                    int i = 1; while (i < cum.Count - 1 && cum[i] < s) i++;
                    var a = line[i - 1]; var b = line[i]; var c = Vector2.Lerp(a, b, Mathf.InverseLerp(cum[i - 1], cum[i], s)); var t = (b - a).normalized; var nl = new Vector2(-t.y, t.x);
                    foreach (int side in new[] { 1, -1 })
                    {
                        var q = c + nl * side * m.half; float gy = Ground(terrain, q.x, q.y);
                        Post(mb, new Vector3(q.x, gy - .3f, q.y), 1.45f, .14f, new Color(.93f, .93f, .9f));
                        Post(mb, new Vector3(q.x, gy + .85f, q.y), .28f, .16f, side > 0 ? new Color(.85f, .1f, .08f) : new Color(.95f, .62f, .1f));   // (the reflector: red on the left, amber on the right)
                        n++;
                    }
                }
            }
            if (n > 0) mb.ToObject("Viking Quarry run-out posts", mats.House, parent, false, false);
            Debug.Log($"[Round91] the quarry race's run-outs: {n} reflector posts");
        }
    }
}
