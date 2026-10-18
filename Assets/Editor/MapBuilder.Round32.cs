using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Round 32 Part A (Dan, 2026-10-15, BUG-005 to BUG-023: "In general not sure why most businesses on Canton road were facing
// away from Canton road without any driveways"). Round 31 picked a business's front as the face looking most toward the
// nearest lot or road, any road, so a service road or side street behind a store won; and lots were paved only where OSM maps
// one. Now (A1):
//   A shop, office, warehouse, church, school or the fire station within 130 m of Canton Road with no other street between it
//   and Canton Road has its front on the face looking most toward Canton Road. In front of each face that looks toward Canton
//   Road: a 1.6 m concrete walk, then a paved lot out to 3 m short of Canton Road's edge (stalls, curbs and light poles as
//   Round 31's lots; never over a house, another street, or where OSM already maps a lot). The lots of one building and of
//   neighbors that touch are paved as one (their convex hull, when that covers no house or other street). Each new lot gets a
//   drive from Canton Road (a curb cut, at the road's own ground), unless an OSM service road already joins the two.
//   Service roads lying in a new lot are paved as the lot (MapBuilder.cs BuildRoads).
// A3 (BUG-019 / BUG-020): building 76 (the long retail block) shows storefronts on every face toward its lot; at the corner by
//   Dan's spot a pizza place with a drive-up window on the side and its lane, then a comic book store, then a hardware store.
// A4 (BUG-014 / BUG-015): building 86 is an ice cream shop (a storefront, a walk-up window), not a house.
// A5 (BUG-021): church 74 is three gabled wings (the spine and its two wings toward Canton Road), 75 three gabled blocks; the
//   one steeple stands over 74's main door at the east end of its north wing, toward Canton Road.
// A6 (BUG-009): the dead-end road 564623219 is joined to the preschool's lot; school 41 is Pine Hollow Elementary, school 0
//   stays Noonday Creek Middle School. A7 (BUG-016): roads 768422193 / 768422194 lead to a gravel lot along Canton Road
//   (NAIP); that lot is paved between them, so neither ends in grass.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        const float CantonReach = 100f, CantonSetback = 3f, LotWalk = 1.6f, LotMaxDepth = 130f;
        const int IceCreamShop = 86, PizzaStrip = 76, ElementarySchool = 41, Preschool = 14;
        static readonly HashSet<int> Round32NotHouses = new HashSet<int> { IceCreamShop };   // dressed here, not by the house kit
        static System.Func<int, bool> LakesOwns = _ => false;                                 // C2: buildings the Chastain Lakes amenity area dresses
        static readonly Vector2 PizzaCorner = new Vector2(3642.3f, 723.0f);                   // 76's corner nearest Dan's BUG-019 spot
        const long PreschoolRoad = 564623219, NowhereRoadA = 768422193, NowhereRoadB = 768422194;
        static readonly Vector2[] NowhereLot = { new Vector2(3510f, 1120f), new Vector2(3519f, 1121f), new Vector2(3516f, 1167f), new Vector2(3500f, 1166f), new Vector2(3504.5f, 1140f) };

        // rows of BusinessSigns.png added in Round 32 (make_business_signs.py KINDS "special")
        const int SignPizza = 49, SignComics = 50, SignHardware = 51, SignIceCream = 52, SignDriveUp = 53, SignElementary = 42, SignMiddle = 43;

        static bool CantonKind(BizKind k) => k == BizKind.Shop || k == BizKind.Office || k == BizKind.Industrial || k == BizKind.Church || k == BizKind.School || k == BizKind.Civic;

        static Vector2 NearestOn(List<(Vector2 a, Vector2 b, float half)> segs, Vector2 p, out float dist, out float half)
        {
            Vector2 best = p; dist = float.MaxValue; half = 0f;
            foreach (var s in segs) { var q = ClosestOnSeg(p, s.a, s.b); float d = (q - p).magnitude; if (d < dist) { dist = d; best = q; half = s.half; } }
            return best;
        }

        static bool SegCross(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
        {
            float Cr(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
            Vector2 r = p2 - p1, s = q2 - q1; float den = Cr(r, s); if (Mathf.Abs(den) < 1e-6f) return false;
            float t = Cr(q1 - p1, s) / den, u = Cr(q1 - p1, r) / den;
            return t >= 0f && t <= 1f && u >= 0f && u <= 1f;
        }

        static List<Vector2> ConvexHull(List<Vector2> pts)
        {
            var p = pts.Distinct().OrderBy(q => q.x).ThenBy(q => q.y).ToList(); if (p.Count < 3) return p;
            float Cr(Vector2 o, Vector2 a, Vector2 b) => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
            var h = new List<Vector2>();
            for (int pass = 0; pass < 2; pass++)
            {
                int start = h.Count;
                foreach (var q in pass == 0 ? p : Enumerable.Reverse(p))
                {
                    while (h.Count >= start + 2 && Cr(h[h.Count - 2], h[h.Count - 1], q) <= 0f) h.RemoveAt(h.Count - 1);
                    h.Add(q);
                }
                h.RemoveAt(h.Count - 1);
            }
            return h;   // counter-clockwise
        }

        // A1: Canton Road's businesses face it. True (and the front set) when the rule applies to z.
        static bool PickCantonFront(Biz z, List<(Vector2 a, Vector2 b, float half)> canton, List<(Vector2 a, Vector2 b)> cross)
        {
            if (!CantonKind(z.kind) || canton.Count == 0) return false;
            // its nearest wall within reach of Canton Road (BUG-019's long block stands 70 m back), no other street in between
            Vector2 near = z.c, q = z.c; float dist = float.MaxValue;
            foreach (var p in Sample(z.ring)) { var qq = NearestOn(canton, p, out float dd, out _); if (dd < dist) { dist = dd; near = p; q = qq; } }
            if (dist > CantonReach) return false;
            foreach (var r in cross) if (SegCross(near, q, r.a, r.b)) return false;   // another street between it and Canton Road
            float best = 0.25f; int front = -1; Vector2 fn = Vector2.zero;
            for (int e = 0; e < z.ring.Count; e++)
            {
                Vector2 a = z.ring[e], b = z.ring[(e + 1) % z.ring.Count]; float len = Vector2.Distance(a, b); if (len < 3f) continue;
                Vector2 m = (a + b) * 0.5f, d = (b - a) / len, n = new Vector2(d.y, -d.x);
                if (InPolygon(m + n * 0.3f, z.ring)) n = -n;
                var to = NearestOn(canton, m, out _, out _) - m; if (to.sqrMagnitude < 1e-4f) continue;
                float score = Vector2.Dot(n, to.normalized) * Mathf.Sqrt(Mathf.Min(len, 40f)) / Mathf.Sqrt(40f);
                if (score > best) { best = score; front = e; fn = n; }
            }
            if (front < 0) return false;
            z.front = front; z.frontN = fn; z.canton = true;
            return true;
        }

        // A1: the walks, the lots and the drives of Canton Road's businesses
        static void PlanCantonLots(Businesses biz, MapData map, List<(Vector2 a, Vector2 b, float half)> canton, List<(Vector2 a, Vector2 b)> cross, HashSet<int> skip)
        {
            var osmLots = biz.Lots.ToList();
            var cantonIdx = new HashSet<int>(biz.Items.Where(q => q.canton).Select(q => q.index));
            var foreign = new List<List<Vector2>>(); var rings = new List<(int index, List<Vector2> ring)>();
            for (int i = 0; i < map.buildings.Length; i++) { var r = ToPoints(map.buildings[i].footprint); if (r.Count < 3) continue; rings.Add((i, r)); if (!cantonIdx.Contains(i)) foreign.Add(r); }
            var service = new List<List<Vector2>>();
            foreach (var r in map.roads) if (r.@class == "service") service.Add(ToPoints(r.points));
            // over a building other than its own ('own': the lot's buildings; a store behind another one gets its walk, no lot)
            bool Bad(List<Vector2> poly, HashSet<int> own)
            {
                Vector2 lo = new Vector2(poly.Min(p => p.x), poly.Min(p => p.y)), hi = new Vector2(poly.Max(p => p.x), poly.Max(p => p.y));
                foreach (var (index, f) in rings)
                {
                    if (own.Contains(index) || f.All(p => p.x < lo.x || p.x > hi.x || p.y < lo.y || p.y > hi.y)) continue;
                    if (f.Any(p => InPolygon(p, poly)) || InPolygon(Centroid(f), poly)) return true;
                }
                foreach (var s in cross)
                {
                    if (InPolygon(s.a, poly) || InPolygon(s.b, poly)) return true;
                    for (int k = 0; k < poly.Count; k++) if (SegCross(s.a, s.b, poly[k], poly[(k + 1) % poly.Count])) return true;
                }
                return false;
            }
            float OsmShare(List<Vector2> poly)
            {
                if (osmLots.Count == 0) return 0f;
                int n = 0, inside = 0; Vector2 c = Centroid(poly);
                foreach (var p in poly) for (int k = 1; k <= 4; k++) { var s = Vector2.Lerp(c, p, k / 5f); n++; if (osmLots.Any(l => InPolygon(s, l))) inside++; }
                return inside / (float)Mathf.Max(1, n);
            }
            // per face toward Canton Road: walk and lot
            var cands = new List<(List<Vector2> lot, Biz z)>();
            int walks = 0, noRoom = 0, blocked = 0, onOsm = 0;
            foreach (var z in biz.Items)
            {
                if (!z.canton || skip.Contains(z.index)) continue;
                for (int e = 0; e < z.ring.Count; e++)
                {
                    Vector2 a = z.ring[e], b = z.ring[(e + 1) % z.ring.Count]; float len = Vector2.Distance(a, b); if (len < 4f) continue;
                    Vector2 d = (b - a) / len, n = new Vector2(d.y, -d.x); if (InPolygon((a + b) * 0.5f + n * 0.3f, z.ring)) n = -n;
                    var to = NearestOn(canton, (a + b) * 0.5f, out _, out _) - (a + b) * 0.5f;
                    if (e != z.front && Vector2.Dot(n, to.normalized) < 0.5f) continue;
                    if (RingDist((a + b) * 0.5f + n * 2f, z.ring) < 1.5f && InPolygon((a + b) * 0.5f + n * 2f, z.ring)) continue;
                    biz.Walks.Add(new List<Vector2> { a, b, b + n * LotWalk, a + n * LotWalk }); walks++;
                    float Reach(Vector2 p0)
                    {
                        for (float s = 0f; s <= LotMaxDepth; s += 1f) { var p = p0 + n * s; var q = NearestOn(canton, p, out float dd, out float hh); if (dd - hh < CantonSetback) return s; }
                        return -1f;
                    }
                    // how far the strip in front of the face runs before another building (a store in front of this one: the lot
                    // stops short of it, BUG-019's corner between buildings 76, 77 and 78)
                    float Clear(Vector2 a0, Vector2 b0)
                    {
                        Vector2 lo = Vector2.Min(a0, b0) - Vector2.one * (LotMaxDepth + 2f), hi = Vector2.Max(a0, b0) + Vector2.one * (LotMaxDepth + 2f);
                        var near = rings.Where(r => r.index != z.index && r.ring.Any(v => v.x > lo.x && v.y > lo.y && v.x < hi.x && v.y < hi.y)).Select(r => r.ring).ToList();
                        for (float s2 = 0f; s2 <= LotMaxDepth; s2 += 1f)
                            for (int k = 0; k <= 8; k++) { var q = Vector2.Lerp(a0, b0, k / 8f) + n * s2; if (near.Any(r => InPolygon(q, r) || RingDist(q, r) < 1.5f)) return s2 - 1f; }
                        return float.MaxValue;
                    }
                    List<Vector2> lot = null;
                    foreach (float ext in new[] { 3f, 0f })
                    {
                        Vector2 a0 = a - d * ext + n * LotWalk, b0 = b + d * ext + n * LotWalk;
                        float da = Reach(a0), db = Reach(b0), clear = Clear(a0, b0);
                        if (da >= 0f && db >= 0f && clear < Mathf.Max(da, db)) { da = Mathf.Min(da, clear); db = Mathf.Min(db, clear); }
                        if (da < 0f || db < 0f || Mathf.Min(da, db) < 6f) { lot = null; noRoom++; break; }
                        var q = new List<Vector2> { a0, b0, b0 + n * db, a0 + n * da };
                        if (PolyArea(q) < 0f) q.Reverse();
                        if (!Bad(q, new HashSet<int> { z.index })) { lot = q; break; }
                        if (ext == 0f) blocked++;
                    }
                    if (lot == null) continue;
                    if (OsmShare(lot) >= 0.3f) { onOsm++; continue; }
                    cands.Add((lot, z));
                }
            }
            // one lot where they touch (a building's own faces and its neighbors'), when the hull covers no house or other street
            int nC = cands.Count; var group = Enumerable.Range(0, nC).ToArray();
            int Find(int i) { while (group[i] != i) i = group[i] = group[group[i]]; return i; }
            bool Touch(List<Vector2> p, List<Vector2> q) => p.Any(v => InPolygon(v, q) || RingDist(v, q) < 3f) || q.Any(v => InPolygon(v, p) || RingDist(v, p) < 3f);
            for (int i = 0; i < nC; i++) for (int j = i + 1; j < nC; j++) if (Touch(cands[i].lot, cands[j].lot)) group[Find(i)] = Find(j);
            var lots = new List<List<Vector2>>(); int merged = 0;
            foreach (var g in Enumerable.Range(0, nC).GroupBy(Find))
            {
                var members = g.Select(i => cands[i].lot).ToList();
                if (members.Count > 1)
                {
                    var hull = ConvexHull(members.SelectMany(l => l).ToList());
                    if (hull.Count >= 3 && !Bad(hull, cantonIdx)) { lots.Add(hull); merged += members.Count; continue; }   // one shared lot may take in other Canton Road stores (BUG-019's corner), never a house
                }
                lots.AddRange(members);
            }
            // the drives: from the lot's point nearest Canton Road straight onto it, unless a service road joins them already
            int drives = 0, linked = 0;
            foreach (var lot in lots)
            {
                biz.Lots.Add(lot); biz.NewLots.Add(lot);
                var c = Centroid(lot); var P = NearestOn(canton, c, out _, out float halfP); var Q = Closest(lot, P);
                bool joined = service.Any(s => s.Any(p => InPolygon(p, lot) || RingDist(p, lot) < 4f) && s.Any(p => { NearestOn(canton, p, out float dd, out float hh); return dd - hh < 4f; }));
                if (joined) { linked++; continue; }
                var dir = (P - Q).normalized; if (dir.sqrMagnitude < 0.5f) continue;
                Vector2 from = Q - dir * 1.0f, to = P - dir * (halfP - 0.6f);
                if (rings.Any(r => InPolygon((from + to) * 0.5f, r.ring) || Enumerable.Range(0, r.ring.Count).Any(k => SegCross(from, to, r.ring[k], r.ring[(k + 1) % r.ring.Count])))) continue;   // never through a building
                biz.Drives.Add((from, to, 3.5f)); drives++;
            }
            // A6 (BUG-009) and A7 (BUG-016)
            JoinPreschoolRoad(biz, map);
            var nowhere = NowhereLot.ToList(); if (PolyArea(nowhere) < 0f) nowhere.Reverse();
            biz.Lots.Add(nowhere); biz.NewLots.Add(nowhere);
            biz.Log.Append($"[Round32] Canton Road: {cantonIdx.Count} businesses face it, {walks} front walks, {lots.Count} new lots ({merged} faces' lots joined into shared ones; " +
                           $"{noRoom} faces too near the road or facing past it, {blocked} kept off a house or a street, {onOsm} on an OSM lot), {drives} drives, {linked} lots already joined by a service road; " +
                           $"BUG-016's gravel lot paved between roads {NowhereRoadA} and {NowhereRoadB}; ");
            biz.Log.Append("faces Canton Road: " + string.Join(" ", biz.Items.Where(q => q.canton).Select(q => q.index)) + "; ");
            // for the round's overhead picture (local/round32): the new lots, walks and drives
            var dump = new System.Text.StringBuilder();
            string Ring(List<Vector2> r) => string.Join(" ", r.Select(v => $"{v.x:0.0},{v.y:0.0}"));
            foreach (var l in biz.NewLots) dump.AppendLine("lot " + Ring(l));
            foreach (var w in biz.Walks) dump.AppendLine("walk " + Ring(w));
            foreach (var d in biz.Drives) dump.AppendLine($"drive {d.a.x:0.0},{d.a.y:0.0} {d.b.x:0.0},{d.b.y:0.0}");
            foreach (var z in biz.Items.Where(q => q.canton)) { var fa = z.ring[z.front]; var fb = z.ring[(z.front + 1) % z.ring.Count]; dump.AppendLine($"front {z.index} {fa.x:0.0},{fa.y:0.0} {fb.x:0.0},{fb.y:0.0}"); }
            System.IO.File.WriteAllText(System.IO.Path.Combine(ProjectRoot, GeneratedDir, "round32_lots.txt"), dump.ToString());
        }

        // A6: the road that ends short of the preschool's lot runs on into it (to the nearest lot, else the preschool's own drive)
        static void JoinPreschoolRoad(Businesses biz, MapData map)
        {
            var r = map.roads.FirstOrDefault(q => q.osm_id == PreschoolRoad); if (r == null) { biz.Log.Append("BUG-009 road not found; "); return; }
            var p = ToPoints(r.points); var end = p[p.Count - 1];
            var pre = biz.Items.FirstOrDefault(q => q.index == Preschool);
            List<Vector2> best = null; float bd = 60f;
            foreach (var lot in biz.Lots) { float dd = InPolygon(end, lot) ? 0f : RingDist(end, lot); if (dd < bd && pre != null && RingDist(pre.c, lot) < 60f) { bd = dd; best = lot; } }
            Vector2 target;
            if (best != null) target = Closest(best, end) + (Centroid(best) - Closest(best, end)).normalized * 2f;
            else { var d = map.roads.FirstOrDefault(q => q.osm_id == 564623221); if (d == null) return; target = ToPoints(d.points)[0]; }
            biz.Drives.Add((end - (end - p[p.Count - 2]).normalized * 1f, target, 2.5f));
            biz.Log.Append($"BUG-009: road {PreschoolRoad} joined to the preschool's lot ({Vector2.Distance(end, target):0} m); ");
        }

        // A5: the church wings, as rectangles (from 74's and 75's OSM outlines): x0, z0, x1, z1, the ridge along x?, eave drop
        static readonly (int b, float x0, float z0, float x1, float z1, bool ridgeX, float drop)[] ChurchWings =
        {
            (74, 3502.6f, 547.6f, 3512.9f, 590.6f, false, 0f),     // the spine, ridge north-south
            (74, 3512.9f, 548.2f, 3536.4f, 562.0f, true, 1.0f),    // the south wing toward Canton Road
            (74, 3512.9f, 568.9f, 3535.0f, 585.0f, true, 1.0f),    // the north wing: the main door and the steeple at its east end
            (75, 3507.4f, 514.9f, 3518.5f, 530.9f, false, 0f),
            (75, 3518.5f, 516.6f, 3529.2f, 528.6f, true, 1.5f),
            (75, 3529.2f, 517.5f, 3536.4f, 527.4f, true, 2.5f),
        };
        static bool HasWings(int index) => ChurchWings.Any(w => w.b == index);

        // the wings of one church: walls, a gable roof per wing (lower wings tuck under the spine's ridge), the gable ends filled
        static void BuildChurchWings(MeshBuilder body, Biz z, Color wall, Color roof, float wallH)
        {
            int wi = -1;
            foreach (var w in ChurchWings.Where(q => q.b == z.index))
            {
                wi++;
                var ring = new List<Vector2> { new Vector2(w.x0, w.z0), new Vector2(w.x1, w.z0), new Vector2(w.x1, w.z1), new Vector2(w.x0, w.z1) };
                float top = z.floorY + wallH - w.drop;
                float floor = z.floorY;
                if (floor - z.baseY > 0.05f) Extrude(body, ring, z.baseY, floor, BizConcrete, BizConcrete, false);
                Vector2 c = new Vector2((w.x0 + w.x1) / 2f, (w.z0 + w.z1) / 2f);
                Vector2 U = w.ridgeX ? Vector2.right : Vector2.up, V = w.ridgeX ? Vector2.up : Vector2.right;
                float hl = (w.ridgeX ? w.x1 - w.x0 : w.z1 - w.z0) / 2f, hw = (w.ridgeX ? w.z1 - w.z0 : w.x1 - w.x0) / 2f;
                float rise = Mathf.Min(Mathf.Tan(z.pitch * Mathf.Deg2Rad) * hw, 0.45f * (top - floor)), eave = top - rise;
                // Round 40 (A7): 74's north wing is hollow for the church leap: its walls are MapBuilder.ChurchClimb.cs' (openings, both faces, colliders)
                if (ChurchHollowWing(z.index, wi)) ChurchHollow = new ChurchShell { x0 = w.x0, z0 = w.z0, x1 = w.x1, z1 = w.z1, floor = floor, eave = eave, top = top, baseY = z.baseY, wall = wall, roof = roof };
                else Extrude(body, ring, floor, eave, wall, roof, false);
                const float oh = 0.35f; float slope = rise / hw, dropY = eave - slope * oh;
                Vector3 W(float a, float b2, float y) { var q = c + U * a + V * b2; return new Vector3(q.x, y, q.y); }
                body.Tint = roof;
                Face(body, W(hl + 0.3f, hw + oh, dropY), W(-hl - 0.3f, hw + oh, dropY), W(-hl - 0.3f, 0f, top), W(hl + 0.3f, 0f, top), new Vector3(V.x * slope, 1f, V.y * slope));
                Face(body, W(hl + 0.3f, -hw - oh, dropY), W(-hl - 0.3f, -hw - oh, dropY), W(-hl - 0.3f, 0f, top), W(hl + 0.3f, 0f, top), new Vector3(-V.x * slope, 1f, -V.y * slope));
                body.Tint = wall;
                TriFace(body, W(hl, hw, eave), W(hl, -hw, eave), W(hl, 0f, top), new Vector3(U.x, 0f, U.y));
                TriFace(body, W(-hl, hw, eave), W(-hl, -hw, eave), W(-hl, 0f, top), new Vector3(-U.x, 0f, -U.y));
                body.Tint = Color.white;
            }
        }

        // A5: 74's main door, sign and steeple at the east gable of its north wing (toward Canton Road)
        static void ChurchEastDoor(MeshBuilder body, MeshBuilder det, Biz z, float wallH, System.Action<MeshBuilder, Frame, float, float, float, int, float> sign, System.Func<BizKind, int, int> next)
        {
            var w = ChurchWings[2]; float top = wallH - w.drop; float hw = (w.z1 - w.z0) / 2f;   // heights from the floor (the frame's)
            float eave = top - Mathf.Min(Mathf.Tan(z.pitch * Mathf.Deg2Rad) * hw, 0.45f * wallH);
            var f = MakeFrame(new Vector3(w.x1, z.floorY, (w.z0 + w.z1) / 2f), Vector2.right, 1f);
            if (ChurchHollow == null)   // Round 40 (A7): the church leap's doors are real (MapBuilder.ChurchClimb.cs); only their frame here
            {
                FrontQuad(det, f, -1.6f, 1.6f, 0f, 3.4f, 0.05f, BizTrim);
                FrontQuad(det, f, -1.45f, 1.45f, 0f, 3.25f, 0.07f, new Color(0.36f, 0.22f, 0.13f));
                FrontQuad(det, f, -0.03f, 0.03f, 0f, 3.25f, 0.08f, new Color(0.25f, 0.15f, 0.09f));
            }
            else { var cdw = RidgeNow.Data.church; float hdw = cdw.door_w * .5f + .2f; FrontQuad(det, f, -hdw - .15f, -hdw, 0f, cdw.door_h + .2f, 0.05f, BizTrim); FrontQuad(det, f, hdw, hdw + .15f, 0f, cdw.door_h + .2f, 0.05f, BizTrim); FrontQuad(det, f, -hdw - .15f, hdw + .15f, cdw.door_h, cdw.door_h + .2f, 0.05f, BizTrim); }
            sign(det, f, 0f, Mathf.Min(eave - 0.6f, 4.3f), Mathf.Min(7f, hw * 1.3f), next(BizKind.Church, z.seed), 0.15f);
            FrontQuad(det, f, -0.7f, 0.7f, eave + 0.6f, eave + 2.6f, 0.05f, BizTrim);   // a round-topped window in the gable (squared)
            FrontQuad(det, f, -0.55f, 0.55f, eave + 0.7f, eave + 2.5f, 0.07f, new Color(0.28f, 0.30f, 0.46f));
            // the steeple: a square white tower on the ridge just behind the gable, its spire above
            float tw = 1.7f, t0 = top + 4.0f, back = 2.2f;
            BoxF(body, f, -tw, tw, eave, t0, -back - 2f * tw, -back, ChurchWhite);
            FrontQuad(body, MakeFrame(f.P(0f, 0f, -back), Vector2.right, 1f), -0.5f, 0.5f, top + 1.2f, top + 3.2f - 0.01f, 0.02f, new Color(0.20f, 0.20f, 0.22f));   // louvre
            var apex = f.P(0f, t0 + 8f, -back - tw);
            body.Tint = new Color(0.30f, 0.30f, 0.32f);
            Vector3 c0 = f.P(-tw, t0, -back), c1 = f.P(tw, t0, -back), c2 = f.P(tw, t0, -back - 2f * tw), c3 = f.P(-tw, t0, -back - 2f * tw);
            TriFace(body, c0, c1, apex, f.F); TriFace(body, c1, c2, apex, f.R); TriFace(body, c2, c3, apex, -f.F); TriFace(body, c3, c0, apex, -f.R);
            body.Tint = Color.white;
        }

        // A3: the sign row for a unit of 76 (by its order from the pizza corner), -1 for an ordinary store
        static int PizzaRow(int order) => order == 0 ? SignPizza : order == 1 ? SignComics : order == 2 ? SignHardware : -1;

        // A3: the pizza place's drive-up window, on the face beside the corner, with its lane painted round the side
        static void DriveUpWindow(MeshBuilder det, Frame f, float u, System.Action<MeshBuilder, Frame, float, float, float, int, float> sign)
        {
            FrontQuad(det, f, u - 1.1f, u + 1.1f, 0.8f, 2.3f, 0.04f, BizFrame);
            FrontQuad(det, f, u - 0.95f, u + 0.95f, 0.95f, 2.15f, 0.06f, BizGlass);
            BoxF(det, f, u - 1.1f, u + 1.1f, 0.85f, 0.95f, 0.04f, 0.45f, BizTrim);                       // the counter
            BoxF(det, f, u - 1.6f, u + 1.6f, 2.6f, 2.75f, 0.02f, 1.4f, new Color(0.62f, 0.12f, 0.10f));    // its little awning
            sign(det, f, u, 3.35f, 4.0f, SignDriveUp, 0.15f);
        }

        // A4: the ice cream shop's walk-up window and counter
        static void WalkUpWindow(MeshBuilder det, Frame f, float u)
        {
            FrontQuad(det, f, u - 0.9f, u + 0.9f, 0.95f, 2.2f, 0.04f, BizTrim);
            FrontQuad(det, f, u - 0.75f, u + 0.75f, 1.05f, 2.1f, 0.06f, BizGlass);
            BoxF(det, f, u - 0.95f, u + 0.95f, 0.95f, 1.05f, 0.04f, 0.4f, BizTrim);
            BoxF(det, f, u - 1.4f, u + 1.4f, 2.45f, 2.55f, 0.02f, 1.2f, new Color(0.92f, 0.55f, 0.65f));
        }
    }
}
