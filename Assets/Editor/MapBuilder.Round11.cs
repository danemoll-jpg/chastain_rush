using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Round 11: Dan's third debug report (2026-10-09_21-01-01-659_c29ec3), the streets of Chastain Glen (TODO items 4, 5, 6).
//   BUG-001, 003, 004, 012  grass in the road and broken sidewalks. Roads were drawn with a vertex at each edge only, so at a
//            junction (where the ground is not level across a road) the lawn came up through the asphalt; junction corners
//            were square, with curbs and sidewalks ending raggedly over each other; sidewalks and curbs (3.5 cm up, a vertex
//            every 2 m at each edge) were half buried by the lawn on bumpy ground. Now: in the area, roads, drives and
//            sidewalks are grids that follow the ground and stand clear of every bump; every corner of a junction gets a curb
//            return (a 6 m arc: asphalt fill, the pale edge band, the curb and, where the streets have one, the sidewalk
//            turning the corner); sidewalks are joined over short gaps and cross drives on top of them.
//   BUG-002, 005  cul-de-sacs: a road that ends (and is a street, 20 m or longer) ends in a bulb (up to 13 m radius, with its
//            curb, edge band and sidewalk round it), so each drive reaches the bulb by itself. Drives that still overlap
//            another drive are re-aimed, one at a time, until none does.
//   BUG-008  "white lines crossing into the road": where the two lanes either side of the entrance island come together, each
//            lane's edge band and curb ran on across the other lane (they were left alone as "the same street"). Paint and
//            curbs are now stopped on any other road's pavement unless that road simply continues this one.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        sealed class StreetCorner { public Vector2 P, X, C, T1, T2, d1, d2, n1, n2; public float R, D, s1, s2, h1, h2; public int r1, r2; }
        sealed class StreetBulb { public Vector2 c, dir; public float R, half; public int road; }

        const float FilletRadius = 6f, BulbRadius = 13f;

        sealed partial class Neighborhood
        {
            public readonly List<StreetCorner> Corners = new List<StreetCorner>();
            public readonly List<StreetBulb> Bulbs = new List<StreetBulb>();
            readonly HashSet<int> streetRoads = new HashSet<int>();
            readonly HashSet<long> continues = new HashSet<long>();
            readonly List<(Vector2 a, Vector2 b, float half, int road)> streetSegs = new List<(Vector2, Vector2, float, int)>();
            public int DrivesReaimed, DriveOverlapsLeft;
            // BUG-012 (Dan, at the entrance: "Make sure grass is not in the road"): the strip of New Chastain Road's median
            // between the entrance's two lanes, where they cross it, is paved (the outline of the two lanes' crossing pieces)
            public List<Vector2> MedianFill;

            public bool IsStreetRoad(int ri) => Present && streetRoads.Contains(ri);
            static long PairKey(int a, int b) => a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;

            // ------------------------------------------------------------ BUG-010: the road into the pool's parking lot

            // Dan: "This is not a sidewalk, this is a road to the parking lot of the pool." The map data has it as a 3 m
            // driveway (pale, like a sidewalk) from the end of Grasmere Court into the lot. It is drawn as a road instead:
            // 6 m of asphalt with the pale edge band, from the court's end into the lot.
            public int PoolRoadIndex = -1;
            public List<Vector2> PoolRoad;
            void PlanPoolRoad()
            {
                if (ParkingRing == null) return;
                var ends = new List<Vector2>();
                foreach (var r in map.roads) { if (r.points == null || r.points.Length < 4) continue; var p = ToPoints(r.points); ends.Add(p[0]); ends.Add(p[p.Count - 1]); }
                for (int i = 0; i < map.driveways.Length; i++)
                {
                    var p = ToPoints(map.driveways[i].points);
                    if (p.Count < 2 || !p.Any(q => InRing(q, ParkingRing))) continue;
                    bool fromRoad = ends.Any(e => (e - p[0]).sqrMagnitude < 2.25f || (e - p[p.Count - 1]).sqrMagnitude < 2.25f);
                    if (!fromRoad) continue;
                    if (ends.Any(e => (e - p[p.Count - 1]).sqrMagnitude < 2.25f)) p.Reverse();   // from the road into the lot
                    PoolRoadIndex = i;
                    PoolRoad = Densify(Smooth(p, 2), 1f);
                    Debug.Log($"[Round11] BUG-010: driveway {i} ({PoolRoad.Count} m) from a road's end into the pool's parking lot is drawn as a {PoolRoadWidth:0} m road");
                    return;
                }
                Debug.LogWarning("[Round11] BUG-010: no driveway from a road's end into the pool's parking lot found");
            }

            // ------------------------------------------------------------ the street graph

            void PlanStreets()
            {
                // the roads of the area, and every road with a point inside it (New Chastain Road at the entrance)
                for (int i = 0; i < map.roads.Length; i++)
                {
                    var r = map.roads[i];
                    if (r.points == null || r.points.Length < 4) continue;
                    bool inside = roadSet.Contains(i);
                    if (!inside) for (int k = 0; k + 1 < r.points.Length && !inside; k += 2) inside = Bounds.Glen.Contains(new Vector2(r.points[k], r.points[k + 1]));
                    if (!inside) inside = TouchesCommunityRoad(r);   // Round 22: in the communities only the roads that meet their streets (New Chastain Road at an entrance)
                    if (!inside) continue;
                    streetRoads.Add(i);
                    var p = ToPoints(r.points);
                    for (int k = 0; k + 1 < p.Count; k++) streetSegs.Add((p[k], p[k + 1], r.width * 0.5f, i));
                }
                // nodes: the ends of the street roads; the arms at each: every street road with a point there
                var nodes = new List<Vector2>();
                foreach (int i in streetRoads)
                {
                    var p = ToPoints(map.roads[i].points);
                    foreach (var e in new[] { p[0], p[p.Count - 1] }) if (!nodes.Any(n => (n - e).sqrMagnitude < 1f)) nodes.Add(e);
                }
                Road r532 = map.roads.FirstOrDefault(r => r.osm_id == 1360406928), r533 = map.roads.FirstOrDefault(r => r.osm_id == 1360406929);
                if (r532 != null && r533 != null)
                {
                    var a = ToPoints(r532.points); var b = ToPoints(r533.points);
                    var quad = new List<Vector2> { a[0], a[a.Count - 1], b[0], b[b.Count - 1] };
                    var cq = (quad[0] + quad[1] + quad[2] + quad[3]) / 4f;
                    quad.Sort((p, q) => Mathf.Atan2(p.y - cq.y, p.x - cq.x).CompareTo(Mathf.Atan2(q.y - cq.y, q.x - cq.x)));
                    MedianFill = quad;
                }
                int corners = 0, bulbs = 0;
                var nodeArms = new List<(Vector2 P, List<(Vector2 d, float h, int road, float len)> arms)>();
                foreach (var P in nodes)
                {
                    var arms = new List<(Vector2 d, float h, int road, float len)>();
                    foreach (int i in streetRoads)
                    {
                        var p = ToPoints(map.roads[i].points);
                        int k = -1; for (int j = 0; j < p.Count; j++) if ((p[j] - P).sqrMagnitude < 0.36f) { k = j; break; }
                        if (k < 0) continue;
                        float h = map.roads[i].width * 0.5f;
                        if (k > 0) arms.Add((ArmDir(p, k, -1, out float l0), h, i, l0));
                        if (k < p.Count - 1) arms.Add((ArmDir(p, k, +1, out float l1), h, i, l1));
                    }
                    if (arms.Count == 0) continue;
                    // pairs of arms that simply continue each other (a street split into two ways): paint and curbs run on
                    for (int a = 0; a < arms.Count; a++)
                        for (int b = a + 1; b < arms.Count; b++)
                            if (arms[a].road != arms[b].road && Vector2.Angle(arms[a].d, arms[b].d) > 150f) continues.Add(PairKey(arms[a].road, arms[b].road));
                    nodeArms.Add((P, arms));
                }
                foreach (var (P, arms) in nodeArms)
                {
                    if (arms.Count == 1)
                    {
                        // a dead end: a cul-de-sac if the street that ends here (with the ways that continue it) is 20 m or more
                        if (roadSet.Contains(arms[0].road) && StreetLength(arms[0].road) >= 20f) { PlanBulb(P, arms[0]); bulbs++; }
                        continue;
                    }
                    arms.Sort((x, y) => Mathf.Atan2(x.d.y, x.d.x).CompareTo(Mathf.Atan2(y.d.y, y.d.x)));
                    for (int a = 0; a < arms.Count; a++)
                    {
                        var A1 = arms[a]; var A2 = arms[(a + 1) % arms.Count];
                        float th = Vector2.SignedAngle(A1.d, A2.d) * Mathf.Deg2Rad; if (th <= 0f) th += Mathf.PI * 2f;
                        if (th > 160f * Mathf.Deg2Rad || th < 40f * Mathf.Deg2Rad) continue;   // straight on, or two lanes either side of an island (its nose is the island's)
                        if (!roadSet.Contains(A1.road) && !roadSet.Contains(A2.road)) continue;
                        // two lanes of one street parting round an island (the entrance): its nose is the island's
                        if (map.roads[A1.road].name == map.roads[A2.road].name && th < 60f * Mathf.Deg2Rad) continue;
                        var n1 = new Vector2(-A1.d.y, A1.d.x); var n2 = new Vector2(A2.d.y, -A2.d.x);
                        var X = Intersect(P + n1 * A1.h, A1.d, P + n2 * A2.h, A2.d);
                        // the arc must fit: its circle (and the curb) clear of any third road's pavement (a median, an island)
                        float R = Mathf.Min(FilletRadius, 9f * Mathf.Tan(th / 2f));
                        var bis = (A1.d + A2.d).normalized;
                        while (R >= 1f && !Fits(X + bis * (R / Mathf.Sin(th / 2f)), R + 0.7f, A1.road, A2.road)) R -= 0.25f;
                        if (R < 1f) continue;
                        if (MedianFill != null && InPolygon(X, MedianFill)) continue;   // paved over (BUG-012)
                        float dist = R / Mathf.Tan(th / 2f);
                        var T1 = X + A1.d * dist; var T2 = X + A2.d * dist;
                        float s1 = Vector2.Dot(T1 - P, A1.d), s2 = Vector2.Dot(T2 - P, A2.d);
                        if (s1 > A1.len - 1f || s2 > A2.len - 1f) continue;   // an arm too short for the arc
                        Corners.Add(new StreetCorner { P = P, X = X, C = T1 + n1 * R, D = dist, T1 = T1, T2 = T2, d1 = A1.d, d2 = A2.d, n1 = n1, n2 = n2, R = R, s1 = s1, s2 = s2, h1 = A1.h, h2 = A2.h, r1 = A1.road, r2 = A2.road });
                        corners++;
                    }
                }
                Debug.Log($"[Round11] streets: {streetRoads.Count} roads, {nodes.Count} nodes, {corners} junction corners with a curb return, {bulbs} cul-de-sac bulbs ({string.Join(", ", Bulbs.Select(b => map.roads[b.road].name + " " + b.R.ToString("0.0") + " m"))}), {continues.Count} road pairs that continue each other");
            }

            // a circle of radius r at c is clear of the pavement of every street road but a and b (and the roads continuing them)
            bool Fits(Vector2 c, float r, int a, int b)
            {
                foreach (var s in streetSegs)
                {
                    if (s.road == a || s.road == b || continues.Contains(PairKey(a, s.road)) || continues.Contains(PairKey(b, s.road))) continue;
                    var ab = s.b - s.a; float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(c - s.a, ab) / ab.sqrMagnitude);
                    if ((s.a + ab * t - c).magnitude - s.half < r) return false;
                }
                return true;
            }

            float RoadLength(int i) { var p = ToPoints(map.roads[i].points); float L = 0f; for (int k = 1; k < p.Count; k++) L += (p[k] - p[k - 1]).magnitude; return L; }
            // the road and every road that continues it, end to end
            float StreetLength(int i)
            {
                var seen = new HashSet<int> { i }; var todo = new Stack<int>(); todo.Push(i); float L = 0f;
                while (todo.Count > 0)
                {
                    int r = todo.Pop(); L += RoadLength(r);
                    foreach (int o in streetRoads) if (!seen.Contains(o) && continues.Contains(PairKey(r, o))) { seen.Add(o); todo.Push(o); }
                }
                return L;
            }

            // the direction of an arm from point k (toward k + step), aimed at the point 5 m along it, and the arm's length
            static Vector2 ArmDir(List<Vector2> p, int k, int step, out float length)
            {
                float acc = 0f; Vector2 target = p[k + step]; length = 0f;
                for (int j = k; j + step >= 0 && j + step < p.Count; j += step)
                {
                    float seg = (p[j + step] - p[j]).magnitude;
                    if (acc < 5f) { target = acc + seg >= 5f ? Vector2.Lerp(p[j], p[j + step], (5f - acc) / seg) : p[j + step]; }
                    acc += seg;
                }
                length = acc;
                return (target - p[k]).normalized;
            }

            static Vector2 Intersect(Vector2 p, Vector2 d, Vector2 q, Vector2 e)
            {
                float den = d.x * e.y - d.y * e.x;
                if (Mathf.Abs(den) < 1e-6f) return (p + q) * 0.5f;
                var w = q - p; float t = (w.x * e.y - w.y * e.x) / den;
                return p + d * t;
            }

            // a cul-de-sac bulb on a street's dead end: as large as BulbRadius allows with its sidewalk 1 m clear of every house
            void PlanBulb(Vector2 P, (Vector2 d, float h, int road, float len) arm)
            {
                float R = BulbRadius;
                for (; R > arm.h + 1f; R -= 0.25f)
                {
                    bool ok = true;
                    for (int k = 0; k < 48 && ok; k++)
                    {
                        float a = k * Mathf.PI * 2f / 48f; var q = P + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (R + 3.3f);
                        if (InsideHouse(q, 1.0f, null)) ok = false;
                    }
                    if (ok) break;
                }
                var b = new StreetBulb { c = P, dir = arm.d, R = R, half = arm.h, road = arm.road };
                Bulbs.Add(b);
                Segs.Add(new RoadSeg { a = P, b = P, half = R, road = arm.road });   // drives, mailboxes, fences and trees see the bulb as road
            }

            // ------------------------------------------------------------ questions the builders ask

            // p (a curb, sidewalk or paint point of road 'road', at least minLateral from its centreline on the corner's side)
            // is in the stretch a curb return replaces
            // (overlap: the stretch ends that much short of the arc's end, so a sidewalk runs on a little into the arc's)
            public bool InCornerZone(int road, Vector2 p, float minLateral, float overlap = 0f)
            {
                foreach (var k in Corners)
                {
                    if (k.r1 == road && Zone(p, k.P, k.d1, k.n1, k.s1 - overlap, minLateral)) return true;
                    if (k.r2 == road && Zone(p, k.P, k.d2, k.n2, k.s2 - overlap, minLateral)) return true;
                }
                return false;
            }
            static bool Zone(Vector2 p, Vector2 P, Vector2 d, Vector2 n, float s, float minLateral)
            {
                var v = p - P; float t = Vector2.Dot(v, d), l = Vector2.Dot(v, n);
                return t > -0.5f && t < s + 0.02f && l > minLateral && l < minLateral + 6f;
            }
            // on the asphalt of a curb return
            public bool OnCornerFill(Vector2 p, float margin)
            {
                foreach (var k in Corners)
                {
                    if ((p - k.X).sqrMagnitude > (k.D + k.R + 2f) * (k.D + k.R + 2f)) continue;
                    if (Vector2.Dot(p - k.T1, k.d1) > 0f || Vector2.Dot(p - k.T2, k.d2) > 0f) continue;   // past the arc's ends
                    if (Vector2.Dot(p - k.X, k.n1) < -margin || Vector2.Dot(p - k.X, k.n2) < -margin) continue;
                    if ((p - k.C).magnitude > k.R - margin) return true;
                }
                return false;
            }
            public bool InMedianFill(Vector2 p) => MedianFill != null && InPolygon(p, MedianFill);
            public bool InBulbZone(Vector2 p, float extra)
            {
                foreach (var b in Bulbs) if ((p - b.c).sqrMagnitude < (b.R + extra) * (b.R + extra)) return true;
                return false;
            }
            // p lies on the pavement of a street road other than 'road' that does not simply continue it
            public bool OtherRoadCovers(int road, Vector2 p, float margin)
            {
                foreach (var s in streetSegs)
                {
                    if (s.road == road || continues.Contains(PairKey(road, s.road))) continue;
                    var ab = s.b - s.a; float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - s.a, ab) / ab.sqrMagnitude);
                    if ((s.a + ab * t - p).sqrMagnitude < (s.half + margin) * (s.half + margin)) return true;
                }
                return false;
            }

            // p is within 'margin' of the pavement of any other street road, even one that continues this one (a sidewalk stops
            // where a lane merges into the street, whose own sidewalk carries on)
            public bool NearOtherRoad(int road, Vector2 p, float margin)
            {
                foreach (var s in streetSegs)
                {
                    if (s.road == road) continue;
                    var ab = s.b - s.a; float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - s.a, ab) / ab.sqrMagnitude);
                    if ((s.a + ab * t - p).sqrMagnitude < (s.half + margin) * (s.half + margin)) return true;
                }
                return false;
            }

            // ------------------------------------------------------------ drives that overlap (BUG-002, BUG-005)

            // Each drive that comes within 0.4 m of another (Dan's excepted) is re-aimed: from the garage straight out along the
            // house's front to the road, or to points along the road's edge either side of the nearest one, the first that
            // clears every other drive. Drives that clear the others are left exactly as they were.
            void ResolveDriveOverlaps()
            {
                var ids = DrivePlans.Keys.ToList();
                var byIndex = Houses.ToDictionary(h => h.index);
                for (int pass = 0; pass < 3; pass++)
                {
                    bool changed = false;
                    foreach (int a in ids)
                    {
                        var h = byIndex[a];
                        if (h.dan || !Conflicts(a, DrivePlans[a], ids)) continue;
                        foreach (var cand in DriveCandidates(h))
                            if (!Conflicts(a, cand, ids)) { DrivePlans[a] = cand; DrivesReaimed++; changed = true; break; }
                    }
                    if (!changed) break;
                }
                var left = ids.Where(a => Conflicts(a, DrivePlans[a], ids)).ToList();
                DriveOverlapsLeft = left.Count;
                foreach (int a in left)
                {
                    var d = DrivePlans[a]; float worst = float.MaxValue; int with = -1;
                    foreach (int o in ids) if (o != a) { var e = DrivePlans[o]; for (int i = 0; i + 1 < d.pts.Count; i++) for (int j = 0; j + 1 < e.pts.Count; j++) { float g = SegSeg(d.pts[i], d.pts[i + 1], e.pts[j], e.pts[j + 1]) - d.widths[i] * 0.5f - e.widths[j] * 0.5f; if (g < worst) { worst = g; with = o; } } }
                    Debug.Log($"[Round11] drive of house {a} still within 0.3 m of house {with}'s ({worst:+0.00;-0.00} m apart); {string.Join(" ", d.pts.Where((q, qi) => qi % 4 == 0).Select(q => $"({q.x:0.0},{q.y:0.0})"))} | {string.Join(" ", DrivePlans[with].pts.Where((q, qi) => qi % 4 == 0).Select(q => $"({q.x:0.0},{q.y:0.0})"))}");
                }
                DriveSegs.Clear();
                foreach (var dp in DrivePlans.Values) for (int i = 0; i + 1 < dp.pts.Count; i++) DriveSegs.Add((dp.pts[i], dp.pts[i + 1], dp.widths[i] * 0.5f));
                Debug.Log($"[Round11] drives: {DrivesReaimed} re-aimed so that no two overlap; overlaps left: {DriveOverlapsLeft}");
            }

            bool Conflicts(int self, DrivePlan d, List<int> ids)
            {
                foreach (int o in ids)
                {
                    if (o == self) continue;
                    var e = DrivePlans[o];
                    if ((e.pts[0] - d.pts[0]).sqrMagnitude > 3600f && (e.pts[e.pts.Count - 1] - d.pts[d.pts.Count - 1]).sqrMagnitude > 3600f) continue;
                    for (int i = 0; i + 1 < d.pts.Count; i += 1)
                        for (int j = 0; j + 1 < e.pts.Count; j += 1)
                            if (SegSeg(d.pts[i], d.pts[i + 1], e.pts[j], e.pts[j + 1]) < d.widths[i] * 0.5f + e.widths[j] * 0.5f + 0.3f) return true;
                }
                return false;
            }

            static float SegSeg(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
            {
                float best = Mathf.Min(Mathf.Min(PointSeg(a, c, d), PointSeg(b, c, d)), Mathf.Min(PointSeg(c, a, b), PointSeg(d, a, b)));
                // crossing
                float d1 = Cross(b - a, c - a), d2 = Cross(b - a, d - a), d3 = Cross(d - c, a - c), d4 = Cross(d - c, b - c);
                if (d1 * d2 < 0f && d3 * d4 < 0f) return 0f;
                return best;
            }
            static float Cross(Vector2 u, Vector2 v) => u.x * v.y - u.y * v.x;
            static float PointSeg(Vector2 p, Vector2 a, Vector2 b) { var ab = b - a; float t = ab.sqrMagnitude < 1e-8f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude); return (a + ab * t - p).magnitude; }

            IEnumerable<DrivePlan> DriveCandidates(HouseSpec h)
            {
                float u = (h.garageU0 + h.garageU1) / 2f;
                Vector3 s3 = h.frame.P(u, 0f, h.D / 2 + 0.05f);
                var S = new Vector2(s3.x, s3.z); var F2 = new Vector2(h.frame.F.x, h.frame.F.z);
                Nearest(S, out Vector2 q, out float half, out Vector2 tan, out _);
                var ends = new List<Vector2>();
                // straight out from the garage to the road's edge
                for (float t = 2f; t < 45f; t += 0.25f) { var p = S + F2 * t; if (RoadClearance(p) < 0.45f) { ends.Add(p); break; } }
                // along the road's edge either side of the nearest point
                for (float off = 1.5f; off <= 9f; off += 1.5f)
                    foreach (float sg in new[] { 1f, -1f })
                    {
                        var g = q + tan * off * sg + (S - q).normalized * (half + 0.45f);
                        Nearest(g, out var q2, out float h2, out _, out _);
                        ends.Add(q2 + (g - q2).normalized * (h2 + 0.45f));
                    }
                foreach (var E in ends)
                {
                    float len = (E - S).magnitude; if (len < 2f) continue;
                    var dp = MakeDrive(h, S, E, F2, len);
                    // not through a house, and only 3 m wide at its mouth more than the plan (no flare): a narrow lot
                    if (dp.pts.Any(p => InsideHouse(p, 0.3f, h))) continue;
                    yield return dp;
                    var narrow = MakeDrive(h, S, E, F2, len); for (int i = 0; i < narrow.widths.Length; i++) narrow.widths[i] = Mathf.Min(narrow.widths[i], h.style == HouseStyle.D ? 3.8f : 5.8f);
                    yield return narrow;
                }
            }

            DrivePlan MakeDrive(HouseSpec h, Vector2 S, Vector2 E, Vector2 F2, float len)
            {
                Vector2 C1 = S + F2 * Mathf.Clamp(len * 0.55f, 2f, 9f);
                var pts = new List<Vector2>();
                int n = Mathf.Max(8, Mathf.CeilToInt(len / 0.75f));
                for (int i = 0; i <= n; i++) { float t = i / (float)n; pts.Add((1 - t) * (1 - t) * S + 2 * (1 - t) * t * C1 + t * t * E); }
                float w0 = h.style == HouseStyle.D ? 3.8f : 5.8f;
                bool bulb = InBulbZone(E, 1f);   // a drive that ends on a cul-de-sac's bulb does not widen at its mouth
                var widths = new float[pts.Count];
                for (int i = 0; i < pts.Count; i++) { float t = i / (float)n; widths[i] = w0 + (t > 0.78f && !bulb ? (t - 0.78f) / 0.22f * 1.5f : 0f); }
                return new DrivePlan { pts = pts, widths = widths, asphalt = h.dan || Hash01(h.seed, 120) < 0.28f };
            }
        }

        const float PoolRoadWidth = 6f;

        // BUG-010: the road into the pool's lot (asphalt as the lot, the pale edge band either side except in the lot)
        static void BuildPoolRoad(HoodCtx c)
        {
            var hood = c.hood; var pts = hood.PoolRoad;
            if (pts == null || pts.Count < 2) return;
            c.Ground.Tint = new Color(0.20f, 0.20f, 0.21f);
            RibbonGridMax(c.Ground.At(pts[pts.Count / 2]), pts, Enumerable.Repeat(PoolRoadWidth, pts.Count).ToArray(), 0.07f, c.terrain, 4, true);
            c.Ground.Tint = EdgeStrip;
            foreach (float side in new[] { 1f, -1f })
            {
                var a = new List<Vector2>(); var b = new List<Vector2>();
                void Flush() { if (a.Count >= 2) Band(c.Ground.At(a[a.Count / 2]), a, b, c.terrain, 0.075f); a.Clear(); b.Clear(); }
                for (int i = 0; i < pts.Count; i++)
                {
                    var t = (pts[Mathf.Min(i + 1, pts.Count - 1)] - pts[Mathf.Max(i - 1, 0)]).normalized; var n = new Vector2(-t.y, t.x) * side;
                    var p = pts[i] + n * (PoolRoadWidth * 0.5f);
                    if (Neighborhood.InRing(p, hood.ParkingRing) || c.roadIndex.Covers(p, NoRoad, 0.2f)) { Flush(); continue; }
                    a.Add(p); b.Add(p + n * 0.45f);
                }
                Flush();
            }
            c.Log.Append($"BUG-010 the way into the pool's lot drawn as a {PoolRoadWidth:0} m road ({pts.Count} m); ");
        }

        // ------------------------------------------------------------ building: surfaces that stay clear of the lawn

        // the highest ground within r of (x, z): a surface vertex set to it stays above the terrain's bumps between vertices
        static float GroundMax(TerrainGrid t, float x, float z, float r)
        {
            float g = t.Height(x, z);
            g = Mathf.Max(g, t.Height(x + r, z)); g = Mathf.Max(g, t.Height(x - r, z)); g = Mathf.Max(g, t.Height(x, z + r)); g = Mathf.Max(g, t.Height(x, z - r));
            float k = r * 0.7f;
            g = Mathf.Max(g, t.Height(x + k, z + k)); g = Mathf.Max(g, t.Height(x - k, z + k)); g = Mathf.Max(g, t.Height(x + k, z - k)); g = Mathf.Max(g, t.Height(x - k, z - k));
            return g;
        }

        // a ribbon with 'across' cells across its width and a row every ~0.75 m (pts as given); the edge vertices lie on the
        // ground, the inner ones on the highest ground near them. miter: keep the width round bends (roads).
        static void RibbonGridMax(MeshBuilder mb, List<Vector2> pts, float[] width, float lift, TerrainGrid t, int across, bool miter = false, Func<Vector2, bool> skipCell = null)
        {
            int start = mb.Count, cols = across + 1;
            for (int i = 0; i < pts.Count; i++)
            {
                Vector2 dirIn = i > 0 ? (pts[i] - pts[i - 1]).normalized : (pts[1] - pts[0]).normalized;
                Vector2 dirOut = i < pts.Count - 1 ? (pts[i + 1] - pts[i]).normalized : dirIn;
                Vector2 tg = (dirIn + dirOut).normalized; if (tg.sqrMagnitude < 1e-6f) tg = dirOut;
                Vector2 nrm = new Vector2(-tg.y, tg.x);
                float w = width[i];
                if (miter) w = Mathf.Min(w / Mathf.Max(0.35f, Vector2.Dot(nrm, new Vector2(-dirOut.y, dirOut.x))), w * 2.5f);
                for (int k = 0; k < cols; k++)
                {
                    var p = pts[i] + nrm * w * (0.5f - k / (float)across);
                    // (roads, miter: the edges too, so no seam of lawn shows where two roads' surfaces meet at a junction)
                    float g = (k == 0 || k == across) && !miter ? Ground(t, p.x, p.y) : GroundMax(t, p.x, p.y, (k == 0 || k == across) ? 0.3f : (miter ? 0.6f : 0.45f));
                    mb.Vertex(new Vector3(p.x, g + lift, p.y));
                }
                if (i > 0)
                    for (int k = 0; k < across; k++)
                    {
                        int a = start + (i - 1) * cols + k, b = a + cols;
                        if (skipCell != null)
                        {
                            var v0 = mb.Verts[a]; var v1 = mb.Verts[b + 1];
                            if (skipCell(new Vector2((v0.x + v1.x) * 0.5f, (v0.z + v1.z) * 0.5f))) continue;
                        }
                        mb.Tri(a, b, a + 1); mb.Tri(a + 1, b, b + 1);
                    }
            }
        }

        // a disc as rings every metre and 'seg' segments, following the ground like RibbonGridMax
        static void DiscGrid(MeshBuilder mb, Vector2 c, float R, float lift, TerrainGrid t, int seg = 40)
        {
            int rings = Mathf.Max(1, Mathf.CeilToInt(R));
            int centre = mb.Vertex(new Vector3(c.x, GroundMax(t, c.x, c.y, 0.45f) + lift, c.y));
            for (int r = 1; r <= rings; r++)
            {
                float rr = R * r / rings;
                for (int s = 0; s < seg; s++)
                {
                    float a = s * Mathf.PI * 2f / seg; float x = c.x + Mathf.Cos(a) * rr, z = c.y + Mathf.Sin(a) * rr;
                    mb.Vertex(new Vector3(x, (r == rings ? Ground(t, x, z) : GroundMax(t, x, z, 0.45f)) + lift, z));
                }
            }
            for (int s = 0; s < seg; s++) mb.Tri(centre, centre + 1 + (s + 1) % seg, centre + 1 + s);
            for (int r = 1; r < rings; r++)
                for (int s = 0; s < seg; s++)
                {
                    int a = centre + 1 + (r - 1) * seg + s, b = centre + 1 + (r - 1) * seg + (s + 1) % seg, d = a + seg, e = b + seg;
                    mb.Tri(a, b, d); mb.Tri(b, e, d);
                }
        }

        // the asphalt of a curb return: from the corner point X out to the arc, rays every 1/12 of the arc, a vertex every metre
        static void CornerFill(MeshBuilder mb, StreetCorner k, float lift, TerrainGrid t)
        {
            var arc = Arc(k, k.R, 12);
            arc[0] -= k.n1 * 0.6f; arc[arc.Count - 1] -= k.n2 * 0.6f;   // a little into the roads, so no seam shows
            var X0 = k.X + (k.P - k.X).normalized * 1.0f;
            int rays = arc.Count, steps = Mathf.Max(2, Mathf.CeilToInt(arc.Max(p => (p - X0).magnitude)));
            int start = mb.Count;
            for (int i = 0; i < rays; i++)
                for (int s = 0; s <= steps; s++)
                {
                    var p = Vector2.Lerp(X0, arc[i], s / (float)steps);
                    mb.Vertex(new Vector3(p.x, (s == steps ? Ground(t, p.x, p.y) : GroundMax(t, p.x, p.y, 0.45f)) + lift, p.y));
                }
            for (int i = 0; i + 1 < rays; i++)
                for (int s = 0; s < steps; s++)
                {
                    int a = start + i * (steps + 1) + s, b = a + steps + 1;
                    TriUp(mb, a, b, a + 1); TriUp(mb, a + 1, b, b + 1);
                }
        }

        // a triangle wound to face up (as BuildPrism does)
        static void TriUp(MeshBuilder mb, int a, int b, int c)
        {
            Vector3 p0 = mb.Verts[a], p1 = mb.Verts[b], p2 = mb.Verts[c];
            if (Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), Vector3.up) >= 0f) mb.Tri(a, b, c); else mb.Tri(a, c, b);
        }

        // the arc of a curb return at radius r from its centre (r = R is the pavement's edge), from T1's side to T2's
        static List<Vector2> Arc(StreetCorner k, float r, int n)
        {
            float a1 = Mathf.Atan2(k.T1.y - k.C.y, k.T1.x - k.C.x), a2 = Mathf.Atan2(k.T2.y - k.C.y, k.T2.x - k.C.x);
            float d = Mathf.DeltaAngle(a1 * Mathf.Rad2Deg, a2 * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            var o = new List<Vector2>();
            for (int i = 0; i <= n; i++) { float a = a1 + d * i / n; o.Add(k.C + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r); }
            return o;
        }

        // a circle round a bulb, as a polyline (closed), radius r
        static List<Vector2> Circle(Vector2 c, float r, int n)
        {
            var o = new List<Vector2>();
            for (int i = 0; i <= n; i++) { float a = i * Mathf.PI * 2f / n; o.Add(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r); }
            return o;
        }

        // In BuildRoads: the curb returns' asphalt and edge bands, and the bulbs' asphalt and edge bands.
        static void BuildStreetSurfaces(Neighborhood hood, TerrainGrid terrain, Chunks main, Chunks paint)
        {
            if (!hood.Present) return;
            float lift = 0.06f * vScale;
            foreach (var k in hood.Corners)
            {
                CornerFill(main.At(k.X), k, lift, terrain);
                paint.Tint = EdgeStrip;
                Band(paint.At(k.X), Arc(k, k.R, 16), Arc(k, k.R - 0.45f, 16), terrain, lift + PaintLift);
            }
            if (hood.MedianFill != null)
            {
                // a grid over the quad (bilinear, about 1 m cells), clear of the lawn like the roads
                var q = hood.MedianFill; int n = 12, start = main.At(q[0]).Count; var mb = main.At(q[0]);
                start = mb.Count;
                for (int i = 0; i <= n; i++)
                    for (int j = 0; j <= n; j++)
                    {
                        float u = i / (float)n, v = j / (float)n;
                        var p = Vector2.Lerp(Vector2.Lerp(q[0], q[1], u), Vector2.Lerp(q[3], q[2], u), v);
                        mb.Vertex(new Vector3(p.x, GroundMax(terrain, p.x, p.y, 0.45f) + lift, p.y));
                    }
                for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) { int a = start + i * (n + 1) + j; TriUp(mb, a, a + n + 1, a + 1); TriUp(mb, a + 1, a + n + 1, a + n + 2); }
            }
            foreach (var b in hood.Bulbs)
            {
                DiscGrid(main.At(b.c), b.c, b.R, lift, terrain);
                paint.Tint = EdgeStrip;
                // the band round the bulb, but not across its own road's mouth
                var inner = Circle(b.c, b.R, 64); var outer = Circle(b.c, b.R + 0.45f, 64);
                var keep = new List<int>();
                for (int i = 0; i < inner.Count; i++) if (Vector2.Dot(inner[i] - b.c, b.dir) < 0f || Mathf.Abs(Cross2(b.dir, inner[i] - b.c)) > b.half + 0.1f) keep.Add(i);
                foreach (var run in IndexRuns(keep, inner.Count)) Band(paint.At(b.c), run.Select(i => outer[i]).ToList(), run.Select(i => inner[i]).ToList(), terrain, lift + PaintLift);
            }
        }

        static float Cross2(Vector2 u, Vector2 v) => u.x * v.y - u.y * v.x;

        // consecutive runs of the kept indices (each at least two long)
        static IEnumerable<List<int>> IndexRuns(List<int> keep, int count)
        {
            var run = new List<int>();
            for (int i = 0; i < keep.Count; i++)
            {
                if (run.Count > 0 && keep[i] != run[run.Count - 1] + 1) { if (run.Count >= 2) yield return run; run = new List<int>(); }
                run.Add(keep[i]);
            }
            if (run.Count >= 2) yield return run;
        }

        // a strip between two matching polylines, on the ground
        static void Band(MeshBuilder mb, List<Vector2> a, List<Vector2> b, TerrainGrid t, float lift)
        {
            int start = mb.Count;
            for (int i = 0; i < a.Count; i++)
            {
                mb.Vertex(new Vector3(a[i].x, Ground(t, a[i].x, a[i].y) + lift, a[i].y));
                mb.Vertex(new Vector3(b[i].x, Ground(t, b[i].x, b[i].y) + lift, b[i].y));
            }
            for (int i = 0; i + 1 < a.Count; i++)
            {
                int p = start + i * 2;
                TriUp(mb, p, p + 2, p + 1); TriUp(mb, p + 1, p + 2, p + 3);
            }
        }

        // In BuildStreets: curbs and sidewalks round the curb returns and the bulbs.
        static void BuildStreetCorners(HoodCtx c)
        {
            var hood = c.hood;
            var centres = hood.Houses.Select(h => h.footprintCentre).ToList();
            bool HouseNear(Vector2 p, Vector2 outward)
            {
                foreach (var hc in centres) { var d = hc - p; if (d.sqrMagnitude < 26f * 26f && Vector2.Dot(d, outward) > 2f) return true; }
                return false;
            }
            int arcs = 0, rings = 0;
            foreach (var k in hood.Corners)
            {
                // the curb: 0.45 to 0.63 m outside the pavement's edge (toward the arc's centre)
                var a = Arc(k, k.R - 0.45f, 16); var b = Arc(k, k.R - 0.63f, 16);
                CurbAlong(c, a, b);
                // the sidewalk round the corner, where the street has houses on this side
                var mid = Arc(k, k.R, 2)[1];
                if (k.R - 3.28f > 0.2f && HouseNear(mid, (k.C - mid).normalized) && hood.DistToPond(mid) > 3f)
                {
                    var wa = LeadIn(k, Arc(k, k.R - 1.88f, 16)); var wb = LeadIn(k, Arc(k, k.R - 3.28f, 16));
                    c.Walks.Tint = ConcreteLight;
                    foreach (var (pa, pb) in OffDrives(c, wa, wb)) WalkBand(c.Walks.At(mid), pa, pb, c.terrain);   // Round 12 (BUG-002): not across drives
                    arcs++;
                }
            }
            foreach (var bl in hood.Bulbs)
            {
                // curb ring (not across the road's mouth or a drive), sidewalk ring (not between the road's own sidewalks)
                var ca = Circle(bl.c, bl.R + 0.45f, 72); var cb = Circle(bl.c, bl.R + 0.63f, 72);
                var keep = new List<int>();
                for (int i = 0; i < ca.Count; i++) if (!c.OnDrive(ca[i], 0.3f) && !MouthOf(bl, ca[i], bl.half + 0.6f)) keep.Add(i);
                foreach (var run in IndexRuns(keep, ca.Count)) CurbAlong(c, run.Select(i => ca[i]).ToList(), run.Select(i => cb[i]).ToList());
                var wa = Circle(bl.c, bl.R + 1.88f, 72); var wb = Circle(bl.c, bl.R + 3.28f, 72);
                keep.Clear();
                for (int i = 0; i < wa.Count; i++) if (!MouthOf(bl, wa[i], bl.half + 1.6f)) keep.Add(i);
                c.Walks.Tint = ConcreteLight;
                foreach (var run in IndexRuns(keep, wa.Count))   // Round 12 (BUG-002): not across the drives that end on the bulb
                    foreach (var (pa, pb) in OffDrives(c, run.Select(i => wa[i]).ToList(), run.Select(i => wb[i]).ToList())) WalkBand(c.Walks.At(bl.c), pa, pb, c.terrain);
                rings++;
            }
            c.Log.Append($"Round 11 streets: {hood.Corners.Count} curb returns ({arcs} with the sidewalk round the corner), {rings} cul-de-sac bulbs with curb and sidewalk rings; drives re-aimed {hood.DrivesReaimed}, overlapping drives left {hood.DriveOverlapsLeft}; ");
        }

        // an arc with a 1.5 m straight added at each end, along the arms (it overlaps the straight sidewalks a little)
        static List<Vector2> LeadIn(StreetCorner k, List<Vector2> arc)
        {
            var o = new List<Vector2> { arc[0] + k.d1 * 1.5f };
            o.AddRange(arc);
            o.Add(arc[arc.Count - 1] + k.d2 * 1.5f);
            return o;
        }

        static bool MouthOf(StreetBulb b, Vector2 p, float half) => Vector2.Dot(p - b.c, b.dir) > 0f && Mathf.Abs(Cross2(b.dir, p - b.c)) < half;

        // a curb along two matching polylines (its road-side edge a, its back b): the top 20 cm up, its face down to the road
        static void CurbAlong(HoodCtx c, List<Vector2> a, List<Vector2> b)
        {
            for (int i = 0; i + 1 < a.Count; i++)
            {
                var mid = (a[i] + a[i + 1]) * 0.5f;
                if (c.OnDrive(mid, 0.3f)) continue;
                var mb = c.Props.At(mid);
                Vector3 P(Vector2 p, float dy) => new Vector3(p.x, Ground(c.terrain, p.x, p.y) + dy, p.y);
                var outN = (b[i] - a[i]).normalized;
                mb.Tint = CurbConcrete;
                Face(mb, P(a[i], 0.20f), P(a[i + 1], 0.20f), P(b[i + 1], 0.20f), P(b[i], 0.20f), Vector3.up);
                mb.Tint = CurbFace;
                Face(mb, P(a[i], 0.08f), P(a[i + 1], 0.08f), P(a[i + 1], 0.20f), P(a[i], 0.20f), new Vector3(-outN.x, 0f, -outN.y));
                Face(mb, P(b[i], -0.1f), P(b[i + 1], -0.1f), P(b[i + 1], 0.20f), P(b[i], 0.20f), new Vector3(outN.x, 0f, outN.y));
                c.CurbMetres += Mathf.RoundToInt((a[i + 1] - a[i]).magnitude);
            }
        }

        // a sidewalk between two matching polylines, two cells across, on the highest ground near each inner vertex
        static void WalkBand(MeshBuilder mb, List<Vector2> a, List<Vector2> b, TerrainGrid t)
        {
            int start = mb.Count;
            for (int i = 0; i < a.Count; i++)
                for (int k = 0; k < 3; k++)
                {
                    var p = Vector2.Lerp(a[i], b[i], k / 2f);
                    mb.Vertex(new Vector3(p.x, (k == 1 ? GroundMax(t, p.x, p.y, 0.45f) : Ground(t, p.x, p.y)) + WalkLift, p.y));
                }
            for (int i = 0; i + 1 < a.Count; i++)
                for (int k = 0; k < 2; k++)
                {
                    int p = start + i * 3 + k, q = p + 3;
                    TriUp(mb, p, q, p + 1); TriUp(mb, p + 1, q, q + 1);
                }
        }

        const float WalkLift = 0.09f;   // above the drives (7.5 cm) (Round 12: it no longer crosses them)
    }
}
