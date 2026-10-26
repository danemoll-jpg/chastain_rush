using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Racer;
using UnityEditor;
using UnityEngine;

// Round 36 (Part C, Dan: "We should have more fire hydrants around in general"): breakable hydrants (the Glen's, squirting water: HydrantWater) along
// every street of the map that does not have them yet (Chastain Glen and its communities keep theirs): at real spacing, about every 150 m on the
// neighborhood streets and every 100 m on the commercial roads, alternating sides; one at each street corner; one in front of each school,
// church and business. At the curb (1.1 m past the road's edge), never on a road, a drive, a lot or its entrance, a sidewalk, in a yard's house
// line, on a shortcut's or a race's line, a jump or its landing, the stunt park, water or a pipe's cut; none within 20 m of another.
// Breakables per 128 m cell, as the other street furniture (one draw per cell, the shared vertex-lit material).
// Part D (Dan picked these, 10:57): a rigged hydrant beside each street manhole (MH1 to MH13): knocked over, the manhole's cover blows off and the
// way down into the network is open; and geysers: a storm grate in the street 22 m down from a hydrant, one or more per neighborhood with hydrants
// and some along Canton Road; knocked over, the grate shoots a column of water that throws a vehicle high. The links are world (HydrantLinks);
// their effects run in Free Roam only (a race route could switch one on later). Built after the network (its manholes).
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        static readonly string[] HydrantClasses = { "residential", "unclassified", "living_street", "tertiary", "secondary", "primary" };
        static bool Commercial(Road r) => r.@class == "primary" || r.@class == "secondary" || (r.name ?? "").StartsWith("Canton Road");

        static void BuildMoreHydrants(MapData map, TerrainGrid t, Materials mats, Transform parent, Neighborhood hood, RestNeighborhoods rest, Businesses biz, CreekTrack creek, Ridge ridge)
        {
            var root = new GameObject("Hydrants (Round 36)").transform; root.SetParent(parent, false);
            var pave = new Pavement(map);
            // ---------------- what a hydrant must keep off
            var lines = new List<List<Vector2>>();   // race lines and shortcut lines (every route)
            foreach (var f in Directory.GetFiles(Path.Combine(ProjectRoot, "Data/routes"), "*.json"))
            {
                var rj = JsonUtility.FromJson<RouteJson>(File.ReadAllText(f));
                if (rj.loop != null && rj.loop.Length > 3) lines.Add(ToPoints(rj.loop));
                if (rj.loop_rev != null && rj.loop_rev.Length > 3) lines.Add(ToPoints(rj.loop_rev));   // Round 39: a reverse lap of its own
                if (rj.official_shortcuts != null) foreach (var sc in rj.official_shortcuts) if (sc.path != null) lines.Add(ToPoints(sc.path));
            }
            var lineSegs = new Dictionary<Vector2Int, List<(Vector2 a, Vector2 b)>>();
            foreach (var l in lines) for (int i = 0; i + 1 < l.Count; i++) { var k = new Vector2Int(Mathf.FloorToInt(l[i].x / 64f), Mathf.FloorToInt(l[i].y / 64f)); if (!lineSegs.TryGetValue(k, out var ls)) lineSegs[k] = ls = new List<(Vector2, Vector2)>(); ls.Add((l[i], l[i + 1])); }
            bool NearLine(Vector2 p, float d)
            {
                var c = new Vector2Int(Mathf.FloorToInt(p.x / 64f), Mathf.FloorToInt(p.y / 64f));
                for (int dx = -1; dx <= 1; dx++) for (int dz = -1; dz <= 1; dz++) if (lineSegs.TryGetValue(c + new Vector2Int(dx, dz), out var ls)) foreach (var s in ls) if (SegDist(p, s.a, s.b) < d) return true;
                return false;
            }
            var jumps = UnityEngine.Object.FindObjectsByType<BigJump>(FindObjectsSortMode.None).Select(j => new Vector2(j.lip.x, j.lip.z)).ToList();
            var lots = (map.parking ?? new Area[0]).Select(a => ToPoints(a.outline)).Where(r => r.Count > 2).ToList(); lots.Add(LotRing.ToList());
            var lotIdx = new CellIndex(64f); foreach (var l in lots) lotIdx.Add(l, 2.5f);   // build speed: the lots near a point (InPolygon or within 2.5 m: inside its box padded 2.5 m)
            var cand = new List<int>();
            var bgrid = new Dictionary<Vector2Int, List<List<Vector2>>>();
            foreach (var b in map.buildings) { var ring = ToPoints(b.footprint); if (ring.Count < 3) continue; var k = new Vector2Int(Mathf.FloorToInt(ring[0].x / 64f), Mathf.FloorToInt(ring[0].y / 64f)); if (!bgrid.TryGetValue(k, out var l)) bgrid[k] = l = new List<List<Vector2>>(); l.Add(ring); }
            bool InBuilding(Vector2 p)
            {
                var c = new Vector2Int(Mathf.FloorToInt(p.x / 64f), Mathf.FloorToInt(p.y / 64f));
                for (int dx = -1; dx <= 1; dx++) for (int dz = -1; dz <= 1; dz++) if (bgrid.TryGetValue(c + new Vector2Int(dx, dz), out var l)) foreach (var r in l) if (InPolygon(p, r) || RingDist(p, r) < 1.2f) return true;
                return false;
            }
            // Round 70 (F1, Dan's BUG-002 / 003 on Kennesaw Mountain Drive: "You wouldn't have them on a mountian path at alll"): a hydrant only where
            // there are buildings along the street: one within 60 m of it (the mountain drive, the park's roads, roads through woods have none)
            bool NearBuilding(Vector2 p, float d)
            {
                var c = new Vector2Int(Mathf.FloorToInt(p.x / 64f), Mathf.FloorToInt(p.y / 64f));
                for (int dx = -2; dx <= 2; dx++) for (int dz = -2; dz <= 2; dz++) if (bgrid.TryGetValue(c + new Vector2Int(dx, dz), out var l)) foreach (var r in l) if (RingDist(p, r) < d || InPolygon(p, r)) return true;
                return false;
            }
            var mountainBox = new Rect(-1700f, -6600f, 1300f, 1700f); int mountainNo = 0;   // Kennesaw Mountain Drive and the park (the before / after count)
            // and none on the park's own roads (Kennesaw Mountain Drive, its service road), not even by the summit's building or the visitor center
            var parkSegs = new List<(Vector2 a, Vector2 b)>();
            foreach (var r in map.roads) if ((r.name ?? "").StartsWith("Kennesaw Mountain")) { var q = ToPoints(r.points); for (int i = 0; i + 1 < q.Count; i++) parkSegs.Add((q[i], q[i + 1])); }
            bool OnParkRoad(Vector2 p) { foreach (var s in parkSegs) if (SegDist(p, s.a, s.b) < 15f) return true; return false; }
            var placed = new List<Vector2>(); var placedIdx = new CellIndex(32f); var older = new List<(Vector2 p, FenceChunk c, int i)>();
            foreach (var c in InSceneOrder<HydrantChunk>()) { var fc = c.GetComponent<FenceChunk>(); if (fc) for (int i = 0; i < fc.panels.Length; i++) { var w = fc.transform.TransformPoint(fc.panels[i].centre); placed.Add(new Vector2(w.x, w.z)); placedIdx.Add(new Vector2(w.x, w.z)); older.Add((new Vector2(w.x, w.z), fc, i)); } }
            int glen = placed.Count;
            var why = new Dictionary<string, int>();
            bool Ok(Vector2 p, float spacing = 20f)
            {
                string no = null;
                if (pave.On(p, .5f)) no = "pavement";
                else if (lotIdx.Near(p, 0f, cand).Any(i => InPolygon(p, lots[i]) || RingDist(p, lots[i]) < 2.5f)) no = "a lot";
                else if (biz != null && biz.RejectTree(p.x, p.y)) no = "a business lot, walk or drive";
                else if (rest != null && rest.RejectTree(p.x, p.y)) no = "a house's front";
                else if (hood.RejectTree(p.x, p.y)) no = "the Glen's own";
                else if (InBuilding(p)) no = "a building";
                else if (NearLine(p, 3.5f)) no = "a race or shortcut line";
                else if (jumps.Any(j => (j - p).sqrMagnitude < 25f * 25f)) no = "a jump";
                else if (creek != null && creek.RejectTree(p.x, p.y) || ridge != null && ridge.RejectTree(p.x, p.y)) no = "a trail, a jump's landing";
                else if (Park != null && Park.InPark(p)) no = "the stunt park";
                else if (Beds.Wet(p.x, p.y)) no = "water";
                else if (Net != null && Net.RejectTree(p.x, p.y)) no = "a pipe's cut";
                else if (placedIdx.Near(p, spacing, cand).Any(i => (placed[i] - p).sqrMagnitude < spacing * spacing)) no = "spacing";
                else if (mountainBox.Contains(p) && OnParkRoad(p)) { no = "the park's road"; mountainNo++; }
                else if (!NearBuilding(p, 60f)) { no = "no building within 60 m"; if (mountainBox.Contains(p)) mountainNo++; }
                if (no != null) { why[no] = why.TryGetValue(no, out int n) ? n + 1 : 1; return false; }
                return true;
            }
            var hyd = new Breakables(); int streets = 0, corners = 0, fronts = 0, rigs = 0;
            var made = new List<(Vector2 p, FenceChunk c, int panel)>();   // filled after ToObjects (the panels in order)
            var order = new List<Vector2>();
            void Put(Vector2 p, int seed)
            {
                float hy = Ground(t, p.x, p.y); var mb = hyd.Begin(p);
                hyd.Piece(() => Hydrant(mb, new Vector3(p.x, hy, p.y), Hash01(seed, 142)), hy + .12f);
                hyd.End(new Vector3(p.x, hy + .45f, p.y), Vector3.forward, .7f, 1.1f);
                placed.Add(p); placedIdx.Add(p); order.Add(p);
            }
            // the curb point at station s of a road line, on a side (+1 right, -1 left of its direction)
            static Vector2 Curb(List<Vector2> pts, float[] arc, float s, float off, out Vector2 dir)
            {
                int k = Mathf.Clamp(Array.FindIndex(arc, a => a >= s), 1, pts.Count - 1);
                dir = (pts[k] - pts[k - 1]).normalized; var n = new Vector2(dir.y, -dir.x);
                return Vector2.Lerp(pts[k - 1], pts[k], Mathf.InverseLerp(arc[k - 1], arc[k], s)) + n * off;
            }
            // ---------------- along the streets
            var ends = new List<Vector2>(); foreach (var r in map.roads) { var q = ToPoints(r.points); if (q.Count > 1) { ends.Add(q[0]); ends.Add(q[q.Count - 1]); } }
            var endIdx = new CellIndex(32f); foreach (var e in ends) endIdx.Add(e);   // build speed: road ends and road points by cell
            var roadPts = new List<(Vector2 q, int ri)>(); var ptIdx = new CellIndex(32f);
            for (int ri = 0; ri < map.roads.Length; ri++) foreach (var q in ToPoints(map.roads[ri].points)) { roadPts.Add((q, ri)); ptIdx.Add(q); }
            for (int ri = 0; ri < map.roads.Length; ri++)
            {
                var r = map.roads[ri]; if (Array.IndexOf(HydrantClasses, r.@class) < 0 || r.bridge || hood.IsStreetRoad(ri)) continue;
                var pts = Densify(ToPoints(r.points), 2f); if (pts.Count < 2) continue;
                var arc = new float[pts.Count]; for (int i = 1; i < pts.Count; i++) arc[i] = arc[i - 1] + Vector2.Distance(pts[i - 1], pts[i]);
                float L = arc[arc.Length - 1], hw = r.width * .5f, step = Commercial(r) ? 100f : 150f; int side = (ri % 2 == 0) ? 1 : -1;
                for (float s = 30f + Hash01(ri, 77) * 40f; s < L - 20f; s += step, side = -side)
                {
                    foreach (float tryS in new[] { 0f, 8f, -8f, 16f, -16f })
                    {
                        float ss = Mathf.Clamp(s + tryS, 5f, L - 5f); var p = Curb(pts, arc, ss, side * (hw + 1.1f), out _);
                        if (Ok(p)) { Put(p, ri * 31 + (int)ss); streets++; break; }
                        p = Curb(pts, arc, ss, -side * (hw + 1.1f), out _); if (Ok(p)) { Put(p, ri * 37 + (int)ss); streets++; break; }
                    }
                }
                // the corners: 9 m back from an end that meets another road
                foreach (bool atEnd in new[] { false, true })
                {
                    var e = atEnd ? pts[pts.Count - 1] : pts[0]; int meets = endIdx.Near(e, 2f, cand).Count(i => (ends[i] - e).sqrMagnitude < 4f) - 1;
                    bool tee = meets <= 0 && ptIdx.Near(e, 9f, cand).Any(i => map.roads[roadPts[i].ri] != r && (roadPts[i].q - e).sqrMagnitude < 9f * 9f);
                    if (meets <= 0 && !tee || L < 30f) continue;
                    float ss = atEnd ? L - 9f : 9f; var p = Curb(pts, arc, ss, (atEnd ? 1 : -1) * (hw + 1.1f), out _);
                    if (Ok(p, 25f)) { Put(p, ri * 41 + (atEnd ? 1 : 0)); corners++; }
                }
            }
            // ---------------- in front of the schools, churches and businesses: at the curb of the road nearest the building
            var segs = new List<(Vector2 a, Vector2 b, float half, Road r)>();
            foreach (var r in map.roads) { if (Array.IndexOf(HydrantClasses, r.@class) < 0 || r.bridge) continue; var q = ToPoints(r.points); for (int i = 0; i + 1 < q.Count; i++) segs.Add((q[i], q[i + 1], r.width * .5f, r)); }
            var segIdx = new CellIndex(64f); foreach (var s in segs) segIdx.Add(s.a, s.b, 0f);   // build speed: the street segments near a point, in their order (the nearest within 60 / 30 m, ties to the first)
            var kinds = new[] { "school", "church", "commercial", "retail", "office", "civic", "public", "college", "kindergarten", "hotel", "warehouse", "industrial", "supermarket" };
            foreach (var b in map.buildings)
            {
                if (Array.IndexOf(kinds, b.type) < 0) continue;
                var ring = ToPoints(b.footprint); if (ring.Count < 3) continue; Vector2 c = Vector2.zero; foreach (var q in ring) c += q; c /= ring.Count;
                if (hood.Bounds != null && hood.RejectTree(c.x, c.y)) continue;
                float best = 60f; Vector2 foot = default, dir = default; float half = 0f;
                foreach (int si in segIdx.Near(c, 60f, cand)) { var s = segs[si]; var ab = s.b - s.a; float tt = Mathf.Clamp01(Vector2.Dot(c - s.a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-4f)); var f = s.a + ab * tt; float d = Vector2.Distance(c, f) - RingDist(f, ring) * 0f; if (d < best) { best = d; foot = f; dir = ab.normalized; half = s.half; } }
                if (best >= 60f) continue;
                var n = new Vector2(dir.y, -dir.x); if (Vector2.Dot(c - foot, n) < 0) n = -n;   // toward the building
                foreach (float along in new[] { 0f, 6f, -6f, 12f, -12f })
                {
                    var p = foot + dir * along + n * (half + 1.1f);
                    if (Ok(p)) { Put(p, (int)(c.x * 7 + c.y)); fronts++; break; }
                }
            }
            // ---------------- Part D1: a rigged hydrant beside each street manhole
            var lids = InSceneOrder<ManholeLid>();
            var rigAt = new List<(Vector2 p, ManholeLid lid)>(); var rigOld = new List<HydrantLinks.Rig>(); var noRig = new List<string>();
            foreach (var lid in lids)
            {
                var m = new Vector2(lid.lid.position.x, lid.lid.position.z);
                float best = 30f; Vector2 foot = default, dir = default; float half = 0f;
                foreach (int si in segIdx.Near(m, 30f, cand)) { var s = segs[si]; var ab = s.b - s.a; float tt = Mathf.Clamp01(Vector2.Dot(m - s.a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-4f)); var f = s.a + ab * tt; float d = Vector2.Distance(m, f); if (d < best) { best = d; foot = f; dir = ab.normalized; half = s.half; } }
                if (best >= 30f) { noRig.Add($"({m.x:0}, {m.y:0}) no street within 30 m"); continue; }
                bool done = false;
                // one of the Glen's hydrants already standing within 15 m of the manhole is the rigged one
                var near = older.Where(o => (o.p - m).sqrMagnitude < 15f * 15f).OrderBy(o => (o.p - m).sqrMagnitude).ToList();
                if (near.Count > 0) { rigOld.Add(new HydrantLinks.Rig { chunk = near[0].c, panel = near[0].i, lid = lid }); rigs++; continue; }
                foreach (float along in new[] { 6f, -6f, 9f, -9f, 12f, -12f, 4f, -4f })
                    foreach (float side in new[] { 1f, -1f })
                    {
                        if (done) break;
                        var p = foot + dir * along + new Vector2(dir.y, -dir.x) * side * (half + 1.1f);
                        if (Ok(p, 8f)) { Put(p, (int)(m.x * 3 + m.y)); rigAt.Add((p, lid)); rigs++; done = true; }
                    }
                if (!done)
                {
                    // the curb is taken: the new hydrant nearest it (within 15 m of the manhole) is rigged instead
                    var mine = order.Select((q, k) => (q, k)).Where(o => (o.q - m).sqrMagnitude < 15f * 15f).OrderBy(o => (o.q - m).sqrMagnitude).ToList();
                    if (mine.Count > 0) { rigAt.Add((mine[0].q, lid)); rigs++; } else noRig.Add($"({m.x:0}, {m.y:0}) no curb spot");
                }
            }
            // ---------------- Part D2: the geysers: on a straight stretch of a neighborhood's street (and three along Canton Road), a hydrant 22 m up
            // the street from a grate in the gutter; 70 m or more of straight street either way of the grate, so a throw comes down on the street
            var geysersAt = new List<(Vector2 grate, Vector2 dir, Vector2 hydrant, float y)>();
            var hoodsPath = Path.Combine(ProjectRoot, "Data/neighborhood/neighborhoods.json");
            var roadSets = new List<(string name, int[] roads)>();
            if (File.Exists(hoodsPath)) foreach (var nb in JsonUtility.FromJson<HoodsFile>(File.ReadAllText(hoodsPath)).neighborhoods) roadSets.Add((nb.name, nb.roads));
            roadSets.Add(("Chastain Glen", Enumerable.Range(0, map.roads.Length).Where(hood.IsStreetRoad).ToArray()));
            roadSets.Add(("Canton Road", Enumerable.Range(0, map.roads.Length).Where(i => (map.roads[i].name ?? "").StartsWith("Canton Road")).ToArray()));
            foreach (var (name, roads) in roadSets)
            {
                int want = name == "Canton Road" ? 3 : 1, got = 0;
                var cands = new List<(float bend, Vector2 g, Vector2 d, float hw, int ri, float s, List<Vector2> pts, float[] arc)>();
                foreach (int ri in roads ?? new int[0])
                {
                    if (ri < 0 || ri >= map.roads.Length) continue; var r = map.roads[ri]; if (r.bridge || Array.IndexOf(HydrantClasses, r.@class) < 0) continue;
                    var pts = Densify(ToPoints(r.points), 2f); if (pts.Count < 75) continue;
                    var arc = new float[pts.Count]; for (int i = 1; i < pts.Count; i++) arc[i] = arc[i - 1] + Vector2.Distance(pts[i - 1], pts[i]);
                    for (int i = 36; i < pts.Count - 36; i += 4)
                    {
                        var a = pts[i - 35]; var b = pts[i]; var c = pts[i + 35];
                        float bend = Vector2.Angle(b - a, c - b); if (bend > 6f) continue;
                        cands.Add((bend, b, (c - a).normalized, r.width * .5f, ri, arc[i], pts, arc));
                    }
                }
                foreach (var cd in cands.OrderBy(x => x.bend))
                {
                    if (got >= want) break;
                    if (geysersAt.Any(g => (g.grate - cd.g).sqrMagnitude < 150f * 150f)) continue;
                    var n = new Vector2(cd.d.y, -cd.d.x);
                    var grate = cd.g + n * (cd.hw - 1.4f);   // in the gutter on the right-hand side
                    if (NearLine(grate, 0f) && false) continue;
                    var hp = Curb(cd.pts, cd.arc, cd.s - 22f, cd.hw + 1.1f, out _);
                    if (!Ok(hp, 12f)) continue;
                    Put(hp, cd.ri * 53); geysersAt.Add((grate, cd.d, hp, Ground(t, grate.x, grate.y))); got++;
                }
            }
            // ---------------- build them: the hydrants (Breakables, HydrantChunk), the links
            var hroot = hyd.ToObjects("MoreHydrants", mats.House, root, .3f, BreakSounds.Kind.Hydrant);
            foreach (var fc in hroot.GetComponentsInChildren<FenceChunk>()) fc.gameObject.AddComponent<HydrantChunk>();
            (FenceChunk c, int i) Find(Vector2 p)
            {
                foreach (var fc in hroot.GetComponentsInChildren<FenceChunk>()) for (int i = 0; i < fc.panels.Length; i++) { var w = fc.transform.TransformPoint(fc.panels[i].centre); if ((new Vector2(w.x, w.z) - p).sqrMagnitude < .25f) return (fc, i); }
                return (null, -1);
            }
            var grates = new MeshBuilder(); var spray = SpraySoftMaterial != null ? SpraySoftMaterial : MakeSprayMaterial();
            var links = new GameObject("HydrantLinks").AddComponent<HydrantLinks>(); links.transform.SetParent(root, false);
            var rigList = new List<HydrantLinks.Rig>(); var spoutList = new List<HydrantLinks.Spout>();
            foreach (var (p, lid) in rigAt) { var (c, i) = Find(p); if (c) rigList.Add(new HydrantLinks.Rig { chunk = c, panel = i, lid = lid }); }
            rigList.AddRange(rigOld);
            foreach (var g in geysersAt)
            {
                var (c, i) = Find(g.hydrant); if (!c) continue;
                var go = new GameObject("Geyser grate"); go.transform.SetParent(root, false); go.transform.position = new Vector3(g.grate.x, g.y + .04f, g.grate.y);
                var gy = go.AddComponent<Geyser>(); gy.along = new Vector3(g.dir.x, 0, g.dir.y); gy.sprayMaterial = spray;
                spoutList.Add(new HydrantLinks.Spout { chunk = c, panel = i, geyser = gy });
                // the grate: an iron frame with bars, flush in the gutter
                var f = MakeFrame(go.transform.position, new Vector2(-g.dir.y, g.dir.x), 1f);
                BoxF(grates, f, -.55f, .55f, -.02f, .025f, -.35f, .35f, new Color(.16f, .16f, .17f));
                for (int k = -4; k <= 4; k++) BoxF(grates, f, k * .12f - .03f, k * .12f + .03f, .025f, .035f, -.33f, .33f, new Color(.30f, .30f, .32f));
            }
            links.rigs = rigList.ToArray(); links.spouts = spoutList.ToArray();
            grates.ToObject("GeyserGrates", mats.House, root, false, false);
            Debug.Log($"[Round36] hydrants: {order.Count} more ({streets} along the streets, {corners} at corners, {fronts} in front of schools, churches and businesses, {rigs} rigged beside manholes, {geysersAt.Count} by geysers); the Glen's {glen} kept; {lids.Length} manholes, {rigList.Count} rigged{(noRig.Count > 0 ? " (none at " + string.Join(", ", noRig) + ")" : "")}; {spoutList.Count} geysers: " +
                      string.Join(", ", geysersAt.Select(g => $"({g.grate.x:0}, {g.grate.y:0})")) + "; left out: " + string.Join(", ", why.Select(kv => $"{kv.Value} {kv.Key}")) +
                      $"; Kennesaw Mountain (x -1700 to -400, z -6600 to -4900): {order.Count(q => mountainBox.Contains(q))} placed, {mountainNo} tries left out for no building near or the park's road (Round 70)");
        }
        [Serializable] class HoodsFile { public HoodJson[] neighborhoods; }
        [Serializable] class HoodJson { public string name; public int[] roads; }
    }
}
