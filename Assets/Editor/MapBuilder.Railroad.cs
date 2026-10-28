using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Round 77 (C, Dan's BUG-006 x 1853.58 z -9077.33 Atlanta Street SE: "this should be a rail bridge"; BUG-007 x 1620.14 z -8259.97 downtown Marietta:
// "rail line shouldn't run into the building. The rail should be quite significant ... still shouldn't stop right here"). The railroad through the whole
// map, edge to edge, from Data/tracks/railroad.json (python Tools/mapgen/make_railroad.py: OSM's running lines, the CSX W&A and Atlanta Terminal
// Subdivisions, the Georgia Northeastern, their sidings). World, not game layer.
// Planning (PlanRailroad, after the embankments): each line's bed is a smooth profile along it (its ground smoothed over about RailSmooth m), held at
// the road's height at every level crossing; the ground graded to it (banks easing out by how far the bed stands off the ground). Where the line
// passes over a road it is a bridge: OSM's bridges (North Marietta Parkway) and every crossing of a highway or trunk road (BUG-006: the Marietta loop,
// South Marietta Parkway, OSM has crossing at grade): the bed lifted up to RailLift m and the road dipped under it until the deck clears it by
// RailClear m. The buildings the line runs through give way (BUG-007's); trees kept off the bed.
// Build (step Railroad): a ballast bed, ties every 0.6 m and two rails; bridge decks (a slab with steel through-girders over a road, concrete piers
// over a creek) with abutments; at every level crossing on each side a crossbuck, the red lights and a raised gate arm (they break like the lamps).
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class RailLineJ { public float[] points; public int[] bridge; }
        [Serializable] class RailCrossJ { public int line, i, road; public float x, z, road_heading, width; public string name; public string @class; }
        [Serializable] class RailJ { public RailLineJ[] lines; public RailCrossJ[] crossings, overpasses; public int[] drop; }
        sealed class RailPlan { public List<Vector2> p; public float[] y; public bool[] deck, roadDeck, cross; }
        static List<RailPlan> RailPlans; static readonly HashSet<int> railDrop = new HashSet<int>();
        static List<(Vector2 at, Vector2 road, float half, float y)> RailCrossings;
        static CellIndex railTreeIdx; static List<Vector2> railTreePts;
        const float RailSmooth = 50f, RailLift = 2.5f, RailClear = 5.2f, RailDeck = 1.5f, RailTreeKeep = 5f;
        static readonly HashSet<string> RailOverClasses = new HashSet<string> { "motorway", "motorway_link", "trunk", "trunk_link" };
        static bool RailOwns(int i) => railDrop.Contains(i);
        static bool RailRejectTree(float x, float z)
        {
            if (railTreeIdx == null) return false; var p = new Vector2(x, z); var near = new List<int>();
            foreach (int k in railTreeIdx.Near(p, RailTreeKeep, near)) if ((railTreePts[k] - p).sqrMagnitude < RailTreeKeep * RailTreeKeep) return true;
            return false;
        }

        static void PlanRailroad(MapData map, TerrainGrid t)
        {
            RailPlans = null; railDrop.Clear(); RailCrossings = new List<(Vector2, Vector2, float, float)>(); railTreeIdx = null;
            var path = Path.Combine(ProjectRoot, "Data/tracks/railroad.json"); if (!File.Exists(path)) return;
            var d = JsonUtility.FromJson<RailJ>(File.ReadAllText(path)); if (d?.lines == null) return;
            if (d.drop != null) foreach (int i in d.drop) if (i >= 0 && i < map.buildings.Length && !MariettaOwns(i)) railDrop.Add(i);
            var cross = (d.crossings ?? new RailCrossJ[0]).Where(c => !RailOverClasses.Contains(c.@class)).ToList();
            var over = (d.overpasses ?? new RailCrossJ[0]).Concat((d.crossings ?? new RailCrossJ[0]).Where(c => RailOverClasses.Contains(c.@class))).ToList();
            RailPlans = new List<RailPlan>(); var log = new List<string>();
            var dips = new List<(int road, Vector2 at, float dip)>();
            float steepest = 0f; int graded = 0;
            for (int li = 0; li < d.lines.Length; li++)
            {
                var p = ToPoints(d.lines[li].points); int n = p.Count; if (n < 2) continue;
                var cum = new float[n]; for (int i = 1; i < n; i++) cum[i] = cum[i - 1] + (p[i] - p[i - 1]).magnitude;
                var deck = new bool[n]; var roadDeck = new bool[n]; var isCross = new bool[n];
                var bf = d.lines[li].bridge; for (int i = 0; i < n; i++) deck[i] = bf != null && i < bf.Length && (bf[i] == 1 || (i > 0 && bf[i - 1] == 1));
                // the spans over roads: the road's width and 3 m each side, measured along the line at the crossing's angle
                foreach (var o in over.Where(o => o.line == li))
                {
                    var rd = new Vector2(Mathf.Sin(o.road_heading * Mathf.Deg2Rad), Mathf.Cos(o.road_heading * Mathf.Deg2Rad));
                    var ld = (p[Mathf.Min(o.i + 1, n - 1)] - p[o.i]).normalized; float sin = Mathf.Max(.35f, Mathf.Abs(ld.x * rd.y - ld.y * rd.x));
                    float span = Mathf.Min(40f, (o.width * .5f + 3f) / sin), s0 = cum[o.i] + (new Vector2(o.x, o.z) - p[o.i]).magnitude;
                    for (int i = 0; i < n; i++) if (Mathf.Abs(cum[i] - s0) <= span) { deck[i] = true; roadDeck[i] = true; }
                }
                // the profile: the ground smoothed along (decks barely count), held at each level crossing at the road's own height
                var raw = new float[n]; var w = new float[n];
                for (int i = 0; i < n; i++) { raw[i] = t.Height(p[i].x, p[i].y); w[i] = deck[i] ? .01f : 1f; }
                foreach (var c in cross.Where(c => c.line == li))
                {
                    int k = Mathf.Clamp(c.i + ((new Vector2(c.x, c.z) - p[c.i]).magnitude > 1f ? 1 : 0), 0, n - 1);
                    raw[k] = t.Height(c.x, c.z); w[k] = 1e6f; isCross[k] = true;
                    var rd = new Vector2(Mathf.Sin(c.road_heading * Mathf.Deg2Rad), Mathf.Cos(c.road_heading * Mathf.Deg2Rad));
                    RailCrossings.Add((new Vector2(c.x, c.z), rd, c.width * .5f, raw[k]));
                    for (int i = 0; i < n; i++) if (Mathf.Abs(cum[i] - cum[k]) < c.width * .5f + 1.5f) isCross[i] = true;
                }
                float mu = (RailSmooth / 2f) * (RailSmooth / 2f);   // (points 2 m apart)
                var y = R77Smooth1D(raw, w, mu);
                // over a road: the bed lifted (up to RailLift) and the road dipped for the rest, until the deck clears it
                bool again = false;
                foreach (var o in over.Where(o => o.line == li))
                {
                    int k = Mathf.Clamp(o.i, 0, n - 1); float roadY = t.Height(o.x, o.z), need = roadY + RailClear + RailDeck;
                    if (y[k] >= need) continue;
                    float lifted = Mathf.Min(need, y[k] + RailLift); raw[k] = lifted; w[k] = 1e6f; again = true;
                    if (need - lifted > .05f) dips.Add((o.road, new Vector2(o.x, o.z), need - lifted));
                }
                if (again) y = R77Smooth1D(raw, w, mu);
                for (int i = 1; i < n; i++) steepest = Mathf.Max(steepest, Mathf.Abs(y[i] - y[i - 1]) / Mathf.Max(cum[i] - cum[i - 1], .1f));
                // the bed graded (not under a deck), in pieces of 200 m; the banks ease out over more ground the further the bed stands off it
                int start = -1;
                for (int i = 0; i <= n; i++)
                {
                    bool on = i < n && !deck[i];
                    if (on && start < 0) start = i;
                    if ((!on || i - start >= 100) && start >= 0)
                    {
                        int end = on ? i : i - 1; if (end - start >= 1)
                        {
                            var pts = new List<float>(); var ys = new List<float>(); var bl = new List<float>();
                            for (int k = start; k <= end; k++) { pts.Add(p[k].x); pts.Add(p[k].y); ys.Add(y[k]); bl.Add(Mathf.Clamp(Mathf.Abs(y[k] - raw[k]) * 2f + 3f, 4f, 16f)); }
                            GradeAlong(t, new RidgeGrade { id = $"railroad {li} {start}", points = pts.ToArray(), y = ys.ToArray(), half = 2.4f, blend = 6f, blends = bl.ToArray() });
                            graded += end - start;
                        }
                        start = on ? i : -1;
                    }
                }
                RailPlans.Add(new RailPlan { p = p, y = y, deck = deck, roadDeck = roadDeck, cross = isCross });
            }
            // the roads dipped under their new bridges: every piece of that road within reach, its ground lowered by the dip at the crossing, easing out
            // along it over max(45 m, 20 x the dip) (both carriageways of a divided road: all targets taken before any is graded)
            var grades = new List<RidgeGrade>();
            foreach (var (road, at, dip) in dips)
            {
                string name = map.roads[road].name; float reach = Mathf.Max(45f, dip * 20f);
                for (int ri = 0; ri < map.roads.Length; ri++)
                {
                    var r = map.roads[ri]; if (r.bridge || r.tunnel || r.points == null || (ri != road && (string.IsNullOrEmpty(name) || r.name != name))) continue;
                    var q = Resample(ToPoints(r.points), 1f).Where(v => (v - at).magnitude < reach).ToList(); if (q.Count < 2) continue;
                    var ys = q.Select(v => t.Height(v.x, v.y) - dip * Smooth01(1f - (v - at).magnitude / reach)).ToArray();
                    grades.Add(new RidgeGrade { id = $"railroad underpass {name}", points = q.SelectMany(v => new[] { v.x, v.y }).ToArray(), y = ys, half = r.width * .5f + 1.5f, blend = 7f });
                }
                log.Add($"{name} dipped {dip:0.0} m under the railroad at ({at.x:0}, {at.y:0}) over {reach:0} m each way");
            }
            foreach (var g in grades) GradeAlong(t, g);
            railTreePts = new List<Vector2>(); railTreeIdx = new CellIndex(16f);
            foreach (var rp in RailPlans) foreach (var q in rp.p) { railTreePts.Add(q); railTreeIdx.Add(q); }
            float km = RailPlans.Sum(rp => { float s = 0f; for (int i = 1; i < rp.p.Count; i++) s += (rp.p[i] - rp.p[i - 1]).magnitude; return s; }) / 1000f;
            Debug.Log($"[Round77] C: railroad planned: {RailPlans.Count} lines, {km:0.0} km, {graded * 2f / 1000f:0.0} km of bed graded, the steepest grade {steepest * 100f:0.0} %; " +
                      $"{RailCrossings.Count} level crossings, {over.Count} bridges over roads ({string.Join(", ", over.Select(o => o.name).Distinct())}); {railDrop.Count} buildings give way ({string.Join(", ", railDrop)}); {string.Join("; ", log)}");
        }

        static void BuildRailroad(TerrainGrid t, Materials mats, Transform world)
        {
            if (RailPlans == null || RailPlans.Count == 0) return;
            var root = new GameObject("Railroad").transform; root.SetParent(world, false);
            var track = new Dictionary<Vector2Int, MeshBuilder>(); var decks = new Dictionary<Vector2Int, MeshBuilder>();
            MeshBuilder Of(Dictionary<Vector2Int, MeshBuilder> dd, Vector2 p) { var k = new Vector2Int(Mathf.FloorToInt(p.x / 256f), Mathf.FloorToInt(p.y / 256f)); if (!dd.TryGetValue(k, out var mb)) dd[k] = mb = new MeshBuilder(); return mb; }
            var ballast = new Color(.45f, .43f, .40f); var tie = new Color(.29f, .21f, .15f); var steel = new Color(.40f, .37f, .34f); var concrete = new Color(.62f, .61f, .58f); var girder = new Color(.13f, .14f, .15f);
            float lift = (RoadLift.TryGetValue("tertiary", out var lf) ? lf : .06f) * vScale;
            int nties = 0, nspans = 0, npiers = 0;
            foreach (var rp in RailPlans)
            {
                var p = rp.p; var y = rp.y; int n = p.Count;
                Vector2 Dir(int i) => (p[Mathf.Min(i + 1, n - 1)] - p[Mathf.Max(i - 1, 0)]).normalized;
                for (int i = 0; i + 1 < n; i++)
                {
                    var a = p[i]; var b = p[i + 1]; var mb = Of(track, a); var na = Dir(i); var nb = Dir(i + 1); var ra = new Vector2(-na.y, na.x); var rb = new Vector2(-nb.y, nb.x);
                    float ya = y[i], yb = y[i + 1]; bool crossing = rp.cross[i] && rp.cross[i + 1];
                    Vector3 A(Vector2 q, Vector2 r, float off, float yy) => new Vector3(q.x + r.x * off, yy, q.y + r.y * off);
                    if (crossing)
                    {
                        // a level crossing: the rails flush with the road, nothing else
                        float ra0 = t.Height(a.x, a.y) + lift + .02f, rb0 = t.Height(b.x, b.y) + lift + .02f;
                        foreach (float g in new[] { -.72f, .72f }) Beam(mb, A(a, ra, g, ra0), A(b, rb, g, rb0), .08f, .05f, steel);
                        continue;
                    }
                    if (!(rp.deck[i] && rp.deck[i + 1]))
                    {
                        // the ballast: a flat top 3.0 m wide just over the bed, its shoulders down into the ground
                        mb.Tint = ballast;
                        Face(mb, A(a, ra, -1.5f, ya + .06f), A(b, rb, -1.5f, yb + .06f), A(b, rb, 1.5f, yb + .06f), A(a, ra, 1.5f, ya + .06f), Vector3.up);
                        Face(mb, A(a, ra, 1.5f, ya + .06f), A(b, rb, 1.5f, yb + .06f), A(b, rb, 2.4f, yb - .35f), A(a, ra, 2.4f, ya - .35f), new Vector3(ra.x, 1f, ra.y));
                        Face(mb, A(a, ra, -2.4f, ya - .35f), A(b, rb, -2.4f, yb - .35f), A(b, rb, -1.5f, yb + .06f), A(a, ra, -1.5f, ya + .06f), new Vector3(-ra.x, 1f, -ra.y));
                    }
                    else
                    {
                        // a deck: a concrete slab 4.6 m wide, 1.2 m deep; over a road steel through-girders each side; over a creek a pier every 12 m
                        var dm = Of(decks, a); nspans++;
                        dm.Tint = concrete;
                        Face(dm, A(a, ra, -2.3f, ya + .06f), A(b, rb, -2.3f, yb + .06f), A(b, rb, 2.3f, yb + .06f), A(a, ra, 2.3f, ya + .06f), Vector3.up);
                        Face(dm, A(a, ra, 2.3f, ya - 1.2f), A(b, rb, 2.3f, yb - 1.2f), A(b, rb, -2.3f, yb - 1.2f), A(a, ra, -2.3f, ya - 1.2f), Vector3.down);
                        foreach (float s in new[] { -1f, 1f })
                        {
                            if (rp.roadDeck[i]) { dm.Tint = girder; Beam(dm, A(a, ra, s * 2.45f, ya - .1f), A(b, rb, s * 2.45f, yb - .1f), .3f, 2.6f, girder); }
                            else { dm.Tint = concrete; Beam(dm, A(a, ra, s * 2.35f, ya - .3f), A(b, rb, s * 2.35f, yb - .3f), .2f, 1.8f, concrete); Beam(dm, A(a, ra, s * 2.4f, ya + .9f), A(b, rb, s * 2.4f, yb + .9f), .08f, .08f, girder); }
                        }
                        if (!rp.roadDeck[i] && i % 6 == 0)
                        {
                            float g = t.Height(a.x, a.y); if (ya - 1.2f - g > .3f) { BoxF(dm, MakeFrame(new Vector3(a.x, g - .5f, a.y), na, 1f), -1.8f, 1.8f, 0f, ya - 1.2f - g + .5f, -.5f, .5f, concrete); npiers++; }
                        }
                    }
                    // the ties (every 0.6 m) and the two rails
                    float L = (b - a).magnitude;
                    for (float s = (i * 2f) % .6f; s < L; s += .6f)
                    {
                        float u = s / L; var q = Vector2.Lerp(a, b, u); var r = Vector2.Lerp(ra, rb, u).normalized; float yy = Mathf.Lerp(ya, yb, u);
                        mb.Tint = tie; var f = MakeFrame(new Vector3(q.x, yy + .06f, q.y), new Vector2(-r.y, r.x), 1f);
                        BoxF(mb, f, -1.3f, 1.3f, 0f, .18f, -.11f, .11f, tie); nties++;
                    }
                    foreach (float g in new[] { -.72f, .72f }) Beam(mb, A(a, ra, g, ya + .32f), A(b, rb, g, yb + .32f), .08f, .14f, steel);
                }
                // abutments: a concrete wall under each end of a deck, down into the ground
                for (int i = 0; i < n; i++)
                {
                    bool endA = rp.deck[i] && (i == 0 || !rp.deck[i - 1]), endB = rp.deck[i] && (i == n - 1 || !rp.deck[i + 1]); if (!endA && !endB) continue;
                    var q = p[i]; float g = t.Height(q.x, q.y); if (y[i] - 1.2f - g < -.5f) continue;
                    BoxF(Of(decks, q), MakeFrame(new Vector3(q.x, Mathf.Min(g, y[i]) - 1f, q.y), Dir(i), 1f), -2.6f, 2.6f, 0f, y[i] + .06f - (Mathf.Min(g, y[i]) - 1f), -.6f, .6f, concrete);
                }
            }
            foreach (var kv in track) kv.Value.ToObject($"Railroad track_{kv.Key.x}_{kv.Key.y}", mats.House, root, false, true);
            foreach (var kv in decks) kv.Value.ToObject($"Railroad bridge_{kv.Key.x}_{kv.Key.y}", mats.House, root, true, true);
            // the level crossings: on each side of the road, on the right of the traffic arriving, a crossbuck on a post with the two red lights, the gate's
            // mechanism and its arm raised (striped red and white); they break when hit (BreakSounds Lamp)
            var gear = new Breakables(); var white = new Color(.93f, .93f, .90f); var red = new Color(.78f, .10f, .08f); var dark = new Color(.10f, .10f, .11f); var lamp = new Color(.45f, .05f, .04f);
            foreach (var (at, road, half, gy0) in RailCrossings)
                foreach (float s in new[] { -1f, 1f })
                {
                    var come = road * s;                                     // the way the traffic arriving on this side drives
                    var right = new Vector2(come.y, -come.x);
                    var p = at - come * 4.2f + right * (half + .9f); float gy = t.Height(p.x, p.y);
                    var f = MakeFrame(new Vector3(p.x, gy, p.y), -come, 1f);   // facing the traffic
                    var mb = gear.Begin(p);
                    gear.Piece(() =>
                    {
                        Post(mb, new Vector3(p.x, gy - .1f, p.y), 4.3f, .16f, white);
                        // the crossbuck: two white boards crossed at 45 degrees
                        var c0 = f.P(0f, 3.75f, .12f);
                        foreach (float ang in new[] { 45f, -45f })
                        {
                            var dir = Quaternion.AngleAxis(ang, f.F) * f.R;
                            Beam(mb, c0 - dir * 1.1f, c0 + dir * 1.1f, .26f, .03f, white, f.F);
                        }
                    }, gy + .1f);
                    gear.Piece(() =>
                    {
                        // the lights: a black bar, two round red lamps in black hoods
                        BoxF(mb, f, -.75f, .75f, 2.55f, 2.68f, .05f, .14f, dark);
                        foreach (float u in new[] { -.55f, .55f }) { Cyl(mb, f.P(u, 2.35f, .18f), .02f, .25f, .25f, dark, 10); BoxF(mb, f, u - .19f, u + .19f, 2.17f, 2.53f, .19f, .21f, lamp); BoxF(mb, f, u - .24f, u + .24f, 2.55f, 2.62f, .1f, .45f, dark); }
                    }, gy + .1f);
                    gear.Piece(() =>
                    {
                        // the gate: its mechanism box beside the post (toward the road) and the arm standing up, red and white bands
                        var g = f.P(-.55f, 0f, 0f); BoxF(mb, MakeFrame(g, new Vector2(f.F.x, f.F.z), 1f), -.25f, .25f, .9f, 1.6f, -.2f, .2f, dark);
                        for (int k = 0; k < 6; k++) Beam(mb, g + Vector3.up * (1.6f + k * .9f), g + Vector3.up * (2.5f + k * .9f), .09f, .09f, k % 2 == 0 ? red : white);
                    }, gy + .1f);
                    gear.End(new Vector3(p.x, gy + 1.5f, p.y), new Vector3(come.x, 0f, come.y), 1.2f, 3f, .8f);
                }
            gear.ToObjects("Railroad crossing gear", mats.House, root, 1f, BreakSounds.Kind.Lamp);
            Debug.Log($"[Round77] C: railroad built: {nties} ties, {nspans} deck segments, {npiers} piers, {RailCrossings.Count} level crossings ({gear.Count} crossing posts)");
        }
    }
}
