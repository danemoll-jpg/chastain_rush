using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Round 40 (B2, BUG-009 of session 2026-10-17_12-09-19-449_081d93 at x 3057.61 z 760.38, Dan: "This median all down this road shouldn't be sloped like
// it is. The ground between the concrete barriers are relatively flat, though we should at least make it smoother. We can plant more trees through the
// medians as well"): all along New Chastain Road where its two one-way carriageways run side by side, the grass between them is set straight across from
// the one's edge to the other's (the lidar's lumps and the slope down to the lower carriageway's edge gone), a little over the curbs in the middle, so it
// follows the road's grade; where another road crosses the median it is left as it was (blended back over 6 m), as on the bridges. And trees along it,
// solid like the others: every 15 to 19 m down its middle where it is 3 m wide or more, none within 35 m of a crossing (sight lines at the junctions,
// the turn lanes), nor by the manhole plume or the Ridge leap's landing.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        // Round 40 (C, BUG-010 at x 1883 z 645: "unbury this house"; BUG-011 at x 590 z 859: "Raise this house"): house 802 off Chastain Trace and the
        // garage row 594 by the unnamed road off Chastain Road were built with their floors under the ground on their high sides (a house's floor at the
        // foot of its garage doors, a business's within 1.2 m of its highest ground). These two only: the floor just above the ground at its highest
        // corner, the foundation showing on the low side as the others' (MapBuilder.Neighborhoods.cs, MapBuilder.Businesses.cs). 594 stands on flat ground:
        // what looked buried was its walls, 2.6 m under a roof as wide as the building; they are 2 m taller
        static readonly HashSet<int> RaisedBuildings = new HashSet<int> { 802, 594 };
        // Round 40 (A6, the crest cut; "Houses may be moved slightly if needed"): house 1245 on Chastain Ridge Drive 1.6 m west, so the gap between it and
        // 1076 that the cut threads is 5.5 m (it was 3.9 m: at the speed that saves time a car met a wall there more often than not)
        static readonly Dictionary<int, Vector2> HouseMoves = new Dictionary<int, Vector2> { { 1245, new Vector2(-1.6f, 0f) } };
        static List<Vector2> MovedRing(int index, List<Vector2> ring) => HouseMoves.TryGetValue(index, out var d) ? ring.Select(p => p + d).ToList() : ring;

        sealed class MedianSide { public Road road; public List<Vector2> p; public float half; public Dictionary<Vector2Int, List<int>> cells; }
        static List<MedianSide> MedianSides(MapData map) => map.roads.Where(r => r.name == "New Chastain Road" && r.oneway && !r.bridge)
                                                                       .Select(r => new MedianSide { road = r, p = ToPoints(r.points), half = r.width * .5f }).ToList();
        // the nearest point of a side's centerline to q, its distance, and the side's direction there
        static float SideDist(MedianSide s, Vector2 q, out Vector2 at, out Vector2 dir)
        {
            float best = float.MaxValue; at = q; dir = Vector2.up;
            // Round 42 (A): only the segments within 22 m of q's 8 m cell (a side farther than 22 m is never used: InMedian); same result
            if (s.cells == null)
            {
                s.cells = new Dictionary<Vector2Int, List<int>>();
                for (int i = 0; i + 1 < s.p.Count; i++)
                {
                    Vector2 a = s.p[i], b = s.p[i + 1];
                    for (int x = Mathf.FloorToInt((Mathf.Min(a.x, b.x) - 23f) / 8f); x <= Mathf.FloorToInt((Mathf.Max(a.x, b.x) + 23f) / 8f); x++)
                        for (int z = Mathf.FloorToInt((Mathf.Min(a.y, b.y) - 23f) / 8f); z <= Mathf.FloorToInt((Mathf.Max(a.y, b.y) + 23f) / 8f); z++)
                        { var k = new Vector2Int(x, z); if (!s.cells.TryGetValue(k, out var l)) s.cells[k] = l = new List<int>(); l.Add(i); }
                }
            }
            if (!s.cells.TryGetValue(new Vector2Int(Mathf.FloorToInt(q.x / 8f), Mathf.FloorToInt(q.y / 8f)), out var near)) return 99f;
            foreach (int i in near)
            {
                var c = ClosestOnSeg(q, s.p[i], s.p[i + 1]); float d = (c - q).sqrMagnitude;
                if (d < best) { best = d; at = c; dir = (s.p[i + 1] - s.p[i]).normalized; }
            }
            return Mathf.Sqrt(best);
        }
        // q in a median: between two carriageways running opposite ways, 9 to 20 m apart center to center, on neither; t across it (0 at a's edge)
        static bool InMedian(List<MedianSide> sides, Vector2 q, out Vector2 ea, out Vector2 eb, out float t, out float width)
        {
            ea = eb = q; t = 0f; width = 0f;
            MedianSide a = null, b = null; float da = float.MaxValue, db = float.MaxValue; Vector2 pa = q, pb = q, ta = Vector2.up, tb = Vector2.up;
            foreach (var s in sides)
            {
                float d = SideDist(s, q, out var at, out var dir); if (d > 22f) continue;
                if (a == null || d < da) { if (a != null && Vector2.Dot(dir, ta) < -.85f) { b = a; db = da; pb = pa; tb = ta; } a = s; da = d; pa = at; ta = dir; }
                else if (Vector2.Dot(dir, ta) < -.85f && d < db) { b = s; db = d; pb = at; tb = dir; }
            }
            if (a == null || b == null || Vector2.Dot(ta, tb) > -.85f) return false;
            float sep = Vector2.Distance(pa, pb); if (sep < 9f || sep > 20f || Mathf.Abs(da + db - sep) > 1.2f) return false;
            float ga = da - a.half, gb = db - b.half; if (ga <= .05f || gb <= .05f) return false;
            ea = pa + (q - pa).normalized * a.half; eb = pb + (q - pb).normalized * b.half; width = ga + gb; t = ga / width;
            return true;
        }

        // the roads that cross or join the median (every road but New Chastain Road's carriageways). Round 42 (A): only those whose bounds come within
        // 120 m of 'near' (CrossDist starts at 99 m, so the result is the same; every road of the map made the build's median grading take minutes)
        static List<(Road r, List<Vector2> p)> CrossRoads(MapData map, Rect near)
        {
            return map.roads.Where(r => r.name != "New Chastain Road" || !r.oneway).Select(r => (r, p: ToPoints(r.points)))
                .Where(rp => rp.p.Count > 0 && rp.p.Max(q => q.x) > near.xMin - 120f && rp.p.Min(q => q.x) < near.xMax + 120f && rp.p.Max(q => q.y) > near.yMin - 120f && rp.p.Min(q => q.y) < near.yMax + 120f).ToList();
        }

        static void GradeNewChastainMedian(MapData map, TerrainGrid terrain)
        {
            var sides = MedianSides(map); if (sides.Count < 2) return;
            var box = Rect.MinMaxRect(sides.Min(s => s.p.Min(q => q.x)), sides.Min(s => s.p.Min(q => q.y)), sides.Max(s => s.p.Max(q => q.x)), sides.Max(s => s.p.Max(q => q.y)));
            var cross = CrossRoads(map, box);
            float CrossDist(Vector2 q) { float m = 99f; foreach (var (r, p) in cross) for (int i = 0; i + 1 < p.Count; i++) { float d = Vector2.Distance(q, ClosestOnSeg(q, p[i], p[i + 1])) - r.width * .5f; if (d < m) m = d; } return m; }
            // the edges' heights read before anything moves (the edges are road, never in a median)
            var before = new Dictionary<Vector2Int, float>();
            float G(Vector2 q) { var k = new Vector2Int(Mathf.RoundToInt(q.x * 4f), Mathf.RoundToInt(q.y * 4f)); if (!before.TryGetValue(k, out float y)) before[k] = y = Ground(terrain, q.x, q.y); return y; }
            var plan = new List<(float x, float z, float y, float w)>();
            // Round 42 (A): only the 8 m cells within reach of a carriageway (InMedian needs both within 22 m); the rest of the box was most of
            // a minute and a half of the build. Same result
            const float Cell = 8f; var near = new HashSet<Vector2Int>();
            foreach (var sd in sides)
                for (int i = 0; i + 1 < sd.p.Count; i++)
                {
                    Vector2 a = sd.p[i], b = sd.p[i + 1]; float pad = 22f + Cell;
                    for (int cx = Mathf.FloorToInt((Mathf.Min(a.x, b.x) - pad) / Cell); cx <= Mathf.FloorToInt((Mathf.Max(a.x, b.x) + pad) / Cell); cx++)
                        for (int cz = Mathf.FloorToInt((Mathf.Min(a.y, b.y) - pad) / Cell); cz <= Mathf.FloorToInt((Mathf.Max(a.y, b.y) + pad) / Cell); cz++)
                        {
                            var cc = new Vector2((cx + .5f) * Cell, (cz + .5f) * Cell);
                            if (Vector2.Distance(cc, ClosestOnSeg(cc, a, b)) <= 22f + Cell * .71f) near.Add(new Vector2Int(cx, cz));
                        }
                }
            for (float x = box.xMin; x <= box.xMax; x += .5f)
                for (float z = box.yMin; z <= box.yMax; z += .5f)
                {
                    var q = new Vector2(x, z);
                    if (!near.Contains(new Vector2Int(Mathf.FloorToInt(x / Cell), Mathf.FloorToInt(z / Cell)))) continue;
                    if (!InMedian(sides, q, out var ea, out var eb, out float t, out float width)) continue;
                    float w = Smooth01((CrossDist(q) - 2f) / 6f); if (w <= 0f) continue;
                    float edgeIn = Mathf.Min(t, 1f - t) * width;   // meters in from the nearer edge
                    float y = Mathf.Lerp(G(ea), G(eb), t) + .10f * Smooth01((edgeIn - .3f) / .5f);
                    plan.Add((x, z, y, w));
                }
            if (plan.Count == 0) return;
            var lookup = new Dictionary<Vector2Int, (float y, float w)>();
            foreach (var p in plan) lookup[new Vector2Int(Mathf.RoundToInt(p.x * 2f), Mathf.RoundToInt(p.z * 2f))] = (p.y, p.w);
            int moved = 0; float most = 0f;
            EditTerrain(terrain, box, (x, z, y) =>
            {
                // the planned height at this terrain sample: the nearest planned point (0.5 m grid)
                var k = new Vector2Int(Mathf.RoundToInt(x * 2f), Mathf.RoundToInt(z * 2f));
                if (!lookup.TryGetValue(k, out var v)) return y;
                if (!InMedian(sides, new Vector2(x, z), out _, out _, out _, out _)) return y;
                float ny = Mathf.Lerp(y, v.y, v.w); most = Mathf.Max(most, Mathf.Abs(ny - y)); moved++;
                return ny;
            });
            Debug.Log($"[Round40] B2: New Chastain Road's median set straight across between its carriageways' edges: {plan.Count} points planned, {moved} terrain samples moved, by up to {most:0.00} m");
        }

        // trees down the median's middle (as the businesses' extra trees: TreeBuilder plants them, solid, with the map's own rules for pavement and cleared lines)
        static List<Vector2> MedianTrees(MapData map, Ridge ridge)
        {
            var sides = MedianSides(map); var trees = new List<Vector2>(); if (sides.Count < 2) return trees;
            var cross = CrossRoads(map, Rect.MinMaxRect(sides.Min(s => s.p.Min(q => q.x)) - 30f, sides.Min(s => s.p.Min(q => q.y)) - 30f, sides.Max(s => s.p.Max(q => q.x)) + 30f, sides.Max(s => s.p.Max(q => q.y)) + 30f));
            float CrossDist(Vector2 q) { float m = 99f; foreach (var (r, p) in cross) for (int i = 0; i + 1 < p.Count; i++) { float d = Vector2.Distance(q, ClosestOnSeg(q, p[i], p[i + 1])) - r.width * .5f; if (d < m) m = d; } return m; }
            var keepOff = new List<Vector2>();   // the manhole plume and the Ridge leap's landing: open ground there
            if (ridge?.Data?.plumes != null) foreach (var pl in ridge.Data.plumes) keepOff.Add(new Vector2(pl.at[0], pl.at[1]));
            if (ridge?.Data?.landing_zones != null) foreach (var z in ridge.Data.landing_zones.Where(z => z.id == "RL" || z.id == "CHURCH")) { var p = ToPoints(z.poly); keepOff.Add(new Vector2(p.Average(q => q.x), p.Average(q => q.y))); }
            if (ridge?.Data?.landing_zones != null) foreach (var z in ridge.Data.landing_zones.Where(z => z.id == "CRYPT")) keepOff.AddRange(ToPoints(z.poly));   // Round 41: the crypt's jump comes down and turns there (its whole zone and 35 m round it)
            int skipped = 0;
            foreach (var s in sides)
            {
                // walk one side only (the other's trees would be the same median): the side whose direction has a positive x, or z if level
                var dir0 = s.p[s.p.Count - 1] - s.p[0]; if (dir0.x < 0f || (Mathf.Abs(dir0.x) < 1f && dir0.y < 0f)) continue;
                float next = 0f, acc = 0f; int k = 0;
                for (int i = 0; i + 1 < s.p.Count; i++)
                {
                    var a = s.p[i]; var b = s.p[i + 1]; float len = Vector2.Distance(a, b); var d = (b - a) / Mathf.Max(len, 1e-4f);
                    for (float u = 0f; u < len; u += 1f)
                    {
                        if (acc + u < next) continue;
                        var c = a + d * u; var n = new Vector2(-d.y, d.x);
                        // the median's middle: across from this side to the other
                        bool found = false;
                        foreach (float sg in new[] { 1f, -1f })
                            for (float o = s.half + 1f; o < 12f && !found; o += .5f)
                            {
                                var q = c + n * sg * o;
                                if (!InMedian(sides, q, out var ea, out var eb, out float t, out float w)) continue;
                                if (w < 3f) { found = true; break; }
                                var mid = Vector2.Lerp(ea, eb, .5f);
                                if (CrossDist(mid) < 35f || keepOff.Any(p => Vector2.Distance(p, mid) < 60f)) { skipped++; found = true; break; }
                                trees.Add(mid + d * (Hash01(k, 4401) - .5f) * 2f); k++; found = true;
                            }
                        next = acc + u + 15f + 4f * Hash01(k, 4403);
                    }
                    acc += len;
                }
            }
            Debug.Log($"[Round40] B2: {trees.Count} trees planted down New Chastain Road's median ({skipped} places left open: within 35 m of a crossing, or by the plume and the Ridge leap's landing)");
            return trees;
        }
    }
}
