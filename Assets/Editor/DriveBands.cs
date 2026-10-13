using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ChastainRush
{
    // Round 24 Part A2, item A2: where a vehicle drives, for the pipe mouths (MapBuilder.Pipes.cs) and the report (MouthScan):
    // every OSM road (half its width plus 0.5 m of curb), the sidewalks as the map build lays them (plus 0.3 m), driveways and the
    // Noonday Creek trail's tread (plus 0.5 m). Nothing of a mouth may stand up on these.
    public static class DriveBands
    {
        [Serializable] class Road { public string name, @class; public float width; public float[] points; public bool tunnel; }
        [Serializable] class Drive { public float[] points; }
        [Serializable] class MapJ { public Road[] roads; public Drive[] driveways; }
        [Serializable] class TrackJ { public float[] line; public float tread_half_m; }

        struct Band { public string what; public int rank; public Vector2 p, q; public float half; }
        static List<Band> bands;
        static Dictionary<(int, int), List<int>> grid;   // 20 m cells
        const float Cell = 20f;

        public static void Load(string projectRoot)
        {
            bands = new List<Band>(); grid = new Dictionary<(int, int), List<int>>();
            var map = JsonUtility.FromJson<MapJ>(File.ReadAllText(Path.Combine(projectRoot, "Data/map.json")));
            var skip = new HashSet<string> { "footway", "path", "cycleway", "steps", "pedestrian", "bridleway" };
            foreach (var r in map.roads)
            {
                if (r.points == null || r.tunnel || skip.Contains(r.@class)) continue;
                string name = string.IsNullOrEmpty(r.name) ? r.@class : r.name;
                for (int i = 2; i + 1 < r.points.Length; i += 2)
                {
                    var p = new Vector2(r.points[i - 2], r.points[i - 1]); var q = new Vector2(r.points[i], r.points[i + 1]);
                    Add("road " + name, 0, p, q, r.width * 0.5f + 0.5f);
                }
            }
            if (map.driveways != null)
                foreach (var d in map.driveways)
                    for (int i = 2; i + 1 < d.points.Length; i += 2)
                        Add("driveway", 2, new Vector2(d.points[i - 2], d.points[i - 1]), new Vector2(d.points[i], d.points[i + 1]), 1.9f);
            // the sidewalks as built (AddWalk, during the map build; saved for MouthScan)
            string wp = Path.Combine(projectRoot, WalksFile);
            if (File.Exists(wp)) foreach (var w in JsonUtility.FromJson<Walks>(File.ReadAllText(wp)).segs) Add("sidewalk", 3, new Vector2(w.x0, w.z0), new Vector2(w.x1, w.z1), w.half);
            string tp = Path.Combine(projectRoot, "Data/tracks/noonday_creek.json");
            if (File.Exists(tp))
            {
                var t = JsonUtility.FromJson<TrackJ>(File.ReadAllText(tp));
                for (int i = 2; i + 1 < t.line.Length; i += 2)
                    Add("Noonday Creek trail", 1, new Vector2(t.line[i - 2], t.line[i - 1]), new Vector2(t.line[i], t.line[i + 1]), t.tread_half_m + 0.5f);
            }
        }

        public const string WalksFile = "Assets/Generated/walks.json";   // git-ignored, rebuilt with the map
        [Serializable] class Seg { public float x0, z0, x1, z1, half; }
        [Serializable] class Walks { public List<Seg> segs = new List<Seg>(); }
        static Walks walks;

        // the map build's sidewalks (MapBuilder.WalkBand: edges a and b): from the first one of a build, the file is written anew
        public static void BeginWalks() => walks = new Walks();
        public static void AddWalk(List<Vector2> a, List<Vector2> b)
        {
            if (walks == null || bands == null) return;
            for (int i = 1; i < a.Count && i < b.Count; i++)
            {
                Vector2 p = (a[i - 1] + b[i - 1]) * 0.5f, q = (a[i] + b[i]) * 0.5f; float half = Vector2.Distance(a[i], b[i]) * 0.5f + 0.3f;
                walks.segs.Add(new Seg { x0 = p.x, z0 = p.y, x1 = q.x, z1 = q.y, half = half });
                Add("sidewalk", 3, p, q, half);
            }
        }
        public static void SaveWalks(string projectRoot) { if (walks != null) File.WriteAllText(Path.Combine(projectRoot, WalksFile), JsonUtility.ToJson(walks)); }

        static void Add(string what, int rank, Vector2 p, Vector2 q, float half)
        {
            int k = bands.Count; bands.Add(new Band { what = what, rank = rank, p = p, q = q, half = half });
            int x0 = Mathf.FloorToInt((Mathf.Min(p.x, q.x) - half) / Cell), x1 = Mathf.FloorToInt((Mathf.Max(p.x, q.x) + half) / Cell);
            int z0 = Mathf.FloorToInt((Mathf.Min(p.y, q.y) - half) / Cell), z1 = Mathf.FloorToInt((Mathf.Max(p.y, q.y) + half) / Cell);
            for (int x = x0; x <= x1; x++) for (int z = z0; z <= z1; z++) { if (!grid.TryGetValue((x, z), out var l)) grid[(x, z)] = l = new List<int>(); l.Add(k); }
        }

        // the drivable surface at p (the road first, then the trail, a driveway, a sidewalk band), or null
        public static string At(Vector2 p, float pad = 0f)
        {
            if (bands == null || !grid.TryGetValue((Mathf.FloorToInt(p.x / Cell), Mathf.FloorToInt(p.y / Cell)), out var l)) return null;
            string best = null; int rank = 99;
            foreach (int k in l)
            {
                var b = bands[k]; var ab = b.q - b.p;
                float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - b.p, ab) / ab.sqrMagnitude);
                float h = b.half + pad;
                if ((b.p + ab * t - p).sqrMagnitude > h * h || b.rank >= rank) continue;
                rank = b.rank; best = b.what;
            }
            return best;
        }
    }
}
