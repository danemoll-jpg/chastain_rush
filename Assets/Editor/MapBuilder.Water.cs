using System.Collections.Generic;
using System.Linq;
using Racer;
using UnityEngine;

// Round 15, item 2: water a vehicle sinks part-way into. The lidar is flat across every pond (the survey flattens water), so
// there was no bed. Before anything samples the ground (right after the Round 9 regrade), this pass:
//   - measures each pond's and lake's level on the untouched lidar (WaterLevel, as before) and each stream's surface (the lowest
//     ground across 3 m at the line, + 5 cm), so every surface stays where it was drawn before;
//   - cuts a bed under them: ponds and lakes a bowl 0.5 m under the surface, reached 14% (1 in 7) in from the shore; streams a
//     channel 0.5 m under the surface for 0.8 m either side of the line, then a 20% bank up to the ground (water about 6 m wide).
//     Only lowers ground, never raises it, and only where the ground was at the water (fading out 0.6 m above it); fades out near roads, bridges, drives (2 to 5 m from the edge) and buildings (1.5 to
//     3.5 m), so none of them moves;
//   - registers every area with ShallowWater (ponds and lakes by their outline, streams as 6 m boxes along the line).
// Depth 0.5 m: Racer's immersion is full at 0.65 m measured from 0.15 m under the ground, so a car on the bed has its wheels
// and lower body in the water, its windows and roof well clear. Same rule as Racer's escapable water: banks gentle all round.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        const float BedDepth = 0.5f, PondSlope = 1f / 7f, StreamSlope = 0.2f, StreamCore = 0.8f, PondLift = 0.15f, StreamLift = 0.05f;
        static float StreamHalf => StreamCore + (BedDepth - StreamLift) / StreamSlope;   // where the bank meets the surface

        sealed class WaterBeds
        {
            public readonly Dictionary<int, float> Level = new Dictionary<int, float>();          // lidar level (surface - PondLift)
            public readonly List<List<Vector2>> Rings = new List<List<Vector2>>();
            public readonly List<Rect> RingBounds = new List<Rect>();
            public readonly List<(List<Vector2> p, List<float> surf)> Streams = new List<(List<Vector2>, List<float>)>();
            readonly Dictionary<Vector2Int, List<(int s, int i)>> grid = new Dictionary<Vector2Int, List<(int, int)>>();
            const float Cell = 16f;
            public int Lowered; public float Deepest; public Vector2 DeepestAt;

            public void Index()
            {
                grid.Clear();   // Round 23: indexed again after a twin culvert trims the streams at its mouths
                for (int s = 0; s < Streams.Count; s++)
                    for (int i = 0; i + 1 < Streams[s].p.Count; i++)
                    {
                        var a = Streams[s].p[i]; var b = Streams[s].p[i + 1]; float pad = StreamHalf + 1f;
                        for (int x = Mathf.FloorToInt((Mathf.Min(a.x, b.x) - pad) / Cell); x <= Mathf.FloorToInt((Mathf.Max(a.x, b.x) + pad) / Cell); x++)
                            for (int z = Mathf.FloorToInt((Mathf.Min(a.y, b.y) - pad) / Cell); z <= Mathf.FloorToInt((Mathf.Max(a.y, b.y) + pad) / Cell); z++)
                            {
                                var k = new Vector2Int(x, z);
                                if (!grid.TryGetValue(k, out var l)) grid[k] = l = new List<(int, int)>();
                                l.Add((s, i));
                            }
                    }
            }

            // the nearest stream point: lateral distance and the surface there
            public bool NearStream(Vector2 p, out float d, out float surf)
            {
                d = float.MaxValue; surf = 0f;
                if (!grid.TryGetValue(new Vector2Int(Mathf.FloorToInt(p.x / Cell), Mathf.FloorToInt(p.y / Cell)), out var l)) return false;
                foreach (var (s, i) in l)
                {
                    var a = Streams[s].p[i]; var ab = Streams[s].p[i + 1] - a;
                    float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                    float dd = (a + ab * t - p).magnitude;
                    if (dd < d) { d = dd; surf = Mathf.Lerp(Streams[s].surf[i], Streams[s].surf[i + 1], t); }
                }
                return d < float.MaxValue;
            }

            // in a pond, lake or stream (for keeping trees out of the water)
            public bool Wet(float x, float z)
            {
                var p = new Vector2(x, z);
                for (int i = 0; i < Rings.Count; i++) if (RingBounds[i].Contains(p) && InPolygon(p, Rings[i])) return true;
                return NearStream(p, out float d, out _) && d < StreamHalf + 0.6f;
            }
        }
        static WaterBeds Beds;

        static float DistToEdges(Vector2 p, List<Vector2> ring)
        {
            float best = float.MaxValue;
            for (int i = 0; i < ring.Count; i++)
            {
                var a = ring[i]; var ab = ring[(i + 1) % ring.Count] - a;
                float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                best = Mathf.Min(best, (a + ab * t - p).magnitude);
            }
            return best;
        }

        static Rect BoundsOf(IEnumerable<Vector2> pts, float pad)
        {
            float x0 = float.MaxValue, z0 = float.MaxValue, x1 = float.MinValue, z1 = float.MinValue;
            foreach (var p in pts) { x0 = Mathf.Min(x0, p.x); z0 = Mathf.Min(z0, p.y); x1 = Mathf.Max(x1, p.x); z1 = Mathf.Max(z1, p.y); }
            return Rect.MinMaxRect(x0 - pad, z0 - pad, x1 + pad, z1 + pad);
        }

        static void CarveWaterBeds(MapData map, TerrainGrid terrain)
        {
            var beds = Beds = new WaterBeds();
            var pave = new Pavement(map);
            if (map.bridges != null) pave.AddLines(map.bridges.Select(b => (ToPoints(b.points), b.width * 0.5f)));
            var blds = new List<(List<Vector2> ring, Rect r)>();
            foreach (var b in map.buildings) { if (b.footprint == null || b.footprint.Length < 6) continue; var r = ToPoints(b.footprint); blds.Add((r, BoundsOf(r, 4f))); }
            // the weight of the cut: 0 on and near pavement and buildings, 1 well clear of them
            float Weight(Vector2 p)
            {
                float w = Mathf.Clamp01((pave.Clearance(p, 6f) - 2f) / 3f);
                if (w <= 0f) return 0f;
                foreach (var (ring, r) in blds)
                    if (r.Contains(p)) { float d = InPolygon(p, ring) ? 0f : DistToEdges(p, ring); w = Mathf.Min(w, Mathf.Clamp01((d - 1.5f) / 2f)); if (w <= 0f) return 0f; }
                return w;
            }
            // 1. levels and surfaces, on the untouched ground
            for (int wi = 0; wi < map.water.Length; wi++)
            {
                var ring = ToPoints(map.water[wi].outline);
                if (ring.Count > 1 && ring[0] == ring[ring.Count - 1]) ring.RemoveAt(ring.Count - 1);
                beds.Rings.Add(ring); beds.RingBounds.Add(BoundsOf(ring, 0f));
                if (ring.Count >= 3) beds.Level[wi] = WaterLevel(ring, terrain);
            }
            foreach (var s in map.streams)
            {
                var pts = Densify(ToPoints(s.points), 2f);
                var surf = new List<float>();
                for (int i = 0; i < pts.Count; i++)
                {
                    var t = (pts[Mathf.Min(i + 1, pts.Count - 1)] - pts[Mathf.Max(i - 1, 0)]).normalized; var n = new Vector2(-t.y, t.x);
                    float lo = float.MaxValue; foreach (float o in new[] { -1.5f, -0.75f, 0f, 0.75f, 1.5f }) lo = Mathf.Min(lo, Ground(terrain, pts[i].x + n.x * o, pts[i].y + n.y * o));
                    surf.Add(lo + StreamLift);
                }
                for (int i = 1; i + 1 < surf.Count; i++) surf[i] = Mathf.Min(surf[i], (surf[i - 1] + surf[i] + surf[i + 1]) / 3f);
                beds.Streams.Add((pts, surf));
            }
            beds.Index();
            // 2. the cut: lower only
            // the cut fades out where the ground stands more than BankFade above the water, so a bank or a hillside beside the
            // water is never trenched (an OSM stream line on a slope): the bed is cut only where the ground was at the water
            const float BankFade = 0.6f;
            float Cut(float y, float target, Vector2 p, float surface)
            {
                if (target >= y) return y;
                float w = Weight(p) * Mathf.Clamp01(1f - (y - surface) / BankFade); if (w <= 0f) return y;
                float ny = y + (target - y) * w;
                beds.Lowered++; if (y - ny > beds.Deepest) { beds.Deepest = y - ny; beds.DeepestAt = p; }
                return ny;
            }
            for (int wi = 0; wi < beds.Rings.Count; wi++)
            {
                if (!beds.Level.TryGetValue(wi, out float level)) continue;
                var ring = beds.Rings[wi];
                EditTerrain(terrain, beds.RingBounds[wi], (x, z, y) =>
                {
                    var p = new Vector2(x, z);
                    if (!InPolygon(p, ring)) return y;   // an island or a bank inside the outline is left alone by the fade in Cut
                    return Cut(y, level - Mathf.Min(BedDepth - PondLift, DistToEdges(p, ring) * PondSlope), p, level + PondLift);
                });
            }
            foreach (var (pts, _) in beds.Streams)
            {
                EditTerrain(terrain, BoundsOf(pts, StreamHalf + 1f), (x, z, y) =>
                {
                    var p = new Vector2(x, z);
                    if (!beds.NearStream(p, out float d, out float surf) || d > StreamHalf + 0.5f) return y;
                    return Cut(y, surf - Mathf.Max(0f, BedDepth - Mathf.Max(0f, d - StreamCore) * StreamSlope), p, surf);
                });
            }
            Debug.Log($"[Round15] water beds: {beds.Level.Count} ponds and lakes, {beds.Streams.Count} streams; {beds.Lowered} terrain samples lowered, by up to {beds.Deepest:0.00} m (at x {beds.DeepestAt.x:0} z {beds.DeepestAt.y:0})");
        }

        // Registers a ShallowWater for every water area: an outline at the surface for ponds and lakes, 6 m boxes along streams
        // (none on pavement or bridges, where nothing was cut).
        static void RegisterWater(MapData map, Transform parent, Pavement pave)
        {
            var root = new GameObject("WaterVolumes").transform; root.SetParent(parent, false);
            int areas = 0, boxes = 0;
            foreach (var kv in Beds.Level)
            {
                var go = new GameObject($"Water area {kv.Key}"); go.transform.SetParent(root, false);
                var w = go.AddComponent<ShallowWater>();
                w.outline = Beds.Rings[kv.Key].ToArray(); w.surfaceY = kv.Value + PondLift;
                areas++;
            }
            foreach (var (pts, surf) in Beds.Streams)
            {
                float acc = 0f; int start = 0;
                for (int i = 1; i < pts.Count; i++)
                {
                    acc += Vector2.Distance(pts[i - 1], pts[i]);
                    if (acc < 6f && i < pts.Count - 1) continue;
                    var a = pts[start]; var b = pts[i]; var mid = (a + b) * 0.5f; float len = (b - a).magnitude;
                    float s = 0f; for (int k = start; k <= i; k++) s += surf[k]; s /= i - start + 1;
                    start = i; acc = 0f;
                    if (len < 0.5f || pave.Clearance(mid, 6f) < 4f) continue;
                    var go = new GameObject("Stream water"); go.transform.SetParent(root, false);
                    go.transform.SetPositionAndRotation(new Vector3(mid.x, s - 0.5f, mid.y), Quaternion.LookRotation(new Vector3(b.x - a.x, 0f, b.y - a.y)));
                    go.transform.localScale = new Vector3(StreamHalf * 2f, 1f, len + 0.6f);
                    go.AddComponent<ShallowWater>();
                    boxes++;
                }
            }
            Debug.Log($"[Round15] ShallowWater: {areas} ponds and lakes by outline, {boxes} stream boxes");
        }
    }
}
