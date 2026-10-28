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
        sealed class RailPlan { public List<Vector2> p; public float[] y; public bool[] deck, roadDeck, cross; public int src; }   // Round 78: src, its index in railroad.json
        static List<RailPlan> RailPlans; static readonly HashSet<int> railDrop = new HashSet<int>();
        static List<(Vector2 at, Vector2 road, float half, float y, int line)> RailCrossings;   // Round 78: line, the line it is on
        static CellIndex railTreeIdx; static List<Vector2> railTreePts;
        const float RailSmooth = 50f, RailLift = 2.5f, RailClear = 5.2f, RailDeck = 1.5f, RailTreeKeep = 5f;
        // Round 78 (D, Dan's BUG-001 x 918.88 z -6512.37 Tower Road NW: "fix the underpass": the road dropped into a narrow trench with sheer walls;
        // BUG-002 x 646.70 z -5541.05 Old 41 Highway NW: "fix the overpass": the road waved up to a deck that barely cleared the rail bed).
        // Under a rail bridge the span reaches past the road and its shoulders far enough for the banks to slope up to the deck's ends (RailBankRun
        // across per metre up), the road dipped on its own untouched profile (the bed's banks no longer counted in it) and its cut graded with banks
        // that slope out by the cut's depth. Under a road bridge (map.json bridges) the line runs at least RailUnder below the deck, its profile capped
        // there and eased back at RailCapGrade so the cut is gentle and the road's approaches stay as they were.
        const float RailBankRun = 2.2f, RailShoulder = 2.5f, RailUnder = 7.7f, RailCapGrade = .03f;
        // Round 78 (C, the air tour: "under a rail bridge"): the South Marietta Parkway bridge (BUG-006's) is the planes' underpass: 11 m clear, its bed lifted up
        // to 6.5 m (the rest the road's dip)
        static readonly Vector2 RailAirAt = new Vector2(1841.6f, -9044.5f);
        static float RailClearAt(Vector2 p) => (p - RailAirAt).magnitude < 20f ? 11f : RailClear;   // (Round 78's check: at 9 m the AI pilots' tails met the deck)
        static float RailLiftAt(Vector2 p) => (p - RailAirAt).magnitude < 20f ? 6.5f : RailLift;
        static float RailUnderSpan(float width, float sin) => Mathf.Min(48f, (width * .5f + RailShoulder + RailBankRun * (RailClear + RailDeck)) / sin);
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
            RailPlans = null; railDrop.Clear(); RailCrossings = new List<(Vector2, Vector2, float, float, int)>(); railTreeIdx = null;
            var path = Path.Combine(ProjectRoot, "Data/tracks/railroad.json"); if (!File.Exists(path)) return;
            var d = JsonUtility.FromJson<RailJ>(File.ReadAllText(path)); if (d?.lines == null) return;
            if (d.drop != null) foreach (int i in d.drop) if (i >= 0 && i < map.buildings.Length && !MariettaOwns(i)) railDrop.Add(i);
            var cross = (d.crossings ?? new RailCrossJ[0]).Where(c => !RailOverClasses.Contains(c.@class)).ToList();
            var over = (d.overpasses ?? new RailCrossJ[0]).Concat((d.crossings ?? new RailCrossJ[0]).Where(c => RailOverClasses.Contains(c.@class))).ToList();
            RailPlans = new List<RailPlan>(); var log = new List<string>();
            if (deckLog.Length > 0) log.Add(deckLog);   // (the decks over the line: RailDecks, before the bridge ends were graded)
            var dips = new List<(int road, Vector2 at, float dip)>();
            var steepList = new List<(Vector2 at, float g)>();   // Round 79: every stretch steeper than 5 % (the log)
            float steepest = 0f; int graded = 0; Vector2 steepAt = Vector2.zero; int nCross = 0, nGradeSep = 0, nForced = 0, nUnder = 0;
            var bedGrades = new List<RidgeGrade>();   // Round 78 (D): every line planned on the untouched ground first, then graded
            // (Round 77 build 1: a second track beside the first got its own profile and sat lower on the first one's bank: a line within 7 m of one
            // already planned takes that one's heights there)
            var doneIdx = new CellIndex(16f); var donePts = new List<(Vector2 p, float y)>(); var nearTmp = new List<int>();
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
                    float span = RailUnderSpan(o.width, sin), s0 = cum[o.i] + (new Vector2(o.x, o.z) - p[o.i]).magnitude;
                    for (int i = 0; i < n; i++) if (Mathf.Abs(cum[i] - s0) <= span) { deck[i] = true; roadDeck[i] = true; }
                }
                // the profile: the ground smoothed along (decks barely count), held at each level crossing at the road's own height
                var raw = new float[n]; var w = new float[n]; var ground = new float[n];
                for (int i = 0; i < n; i++) { raw[i] = ground[i] = t.Height(p[i].x, p[i].y); w[i] = deck[i] ? .01f : 1f; }
                for (int i = 0; i < n; i++)
                {
                    float bd = 49f; float by = 0f; bool got = false;
                    foreach (int k in doneIdx.Near(p[i], 7f, nearTmp)) { float d2 = (donePts[k].p - p[i]).sqrMagnitude; if (d2 < bd) { bd = d2; by = donePts[k].y; got = true; } }
                    if (got) { raw[i] = by; w[i] = 50f; }
                }
                float mu = (RailSmooth / 2f) * (RailSmooth / 2f);   // (points 2 m apart)
                var free = R77Smooth1D(raw, w, mu);   // the profile without the crossings: a crossing whose road is far off it is not at grade
                // Round 78 (D): under each road bridge the line at least RailUnder below its deck, eased back along at RailCapGrade
                foreach (var (k, capY, bname) in RailRoadOver(map, p))
                {
                    if (free[k] <= capY) continue;
                    if (free[k] >= capY + RailUnder - 1f) continue;   // the line at or over the deck's level: it crosses over that road (the Express Lanes' long deck by I-575)
                    for (int i = 0; i < n; i++) { float c = capY + RailCapGrade * Mathf.Abs(cum[i] - cum[k]); if (raw[i] > c) { raw[i] = c; w[i] = Mathf.Max(w[i], i == k ? 1e6f : 30f); } }
                    nUnder++; log.Add($"lowered {free[k] - capY:0.0} m under {bname} at ({p[k].x:0}, {p[k].y:0})");
                }
                free = R77Smooth1D(raw, w, mu);
                foreach (var c in cross.Where(c => c.line == li))
                {
                    int k = Mathf.Clamp(c.i + ((new Vector2(c.x, c.z) - p[c.i]).magnitude > 1f ? 1 : 0), 0, n - 1);
                    float roadY = t.Height(c.x, c.z), off = roadY - free[k];   // (the ground untouched: Round 78 grades after planning)
                    // Round 79 (D: the 27 % grade at Cobb Parkway's rail bridge): a divided road's second carriageway under the deck already made over its
                    // first (14 m apart) was held as a level crossing at its own height beside the lifted bridge; it goes under the same bridge (its road dipped too)
                    if (off < 0f && roadDeck[k]) { over.Add(c); nGradeSep++; log.Add($"{c.name} at ({c.x:0}, {c.z:0}): under the bridge made over its other carriageway"); continue; }
                    if (off < -2.5f && !deck[k])
                    {
                        // the road well below the line: a bridge over it (as OSM's bridges)
                        over.Add(c); nGradeSep++;
                        var rd0 = new Vector2(Mathf.Sin(c.road_heading * Mathf.Deg2Rad), Mathf.Cos(c.road_heading * Mathf.Deg2Rad));
                        var ld0 = (p[Mathf.Min(c.i + 1, n - 1)] - p[c.i]).normalized; float sin0 = Mathf.Max(.35f, Mathf.Abs(ld0.x * rd0.y - ld0.y * rd0.x));
                        float span0 = RailUnderSpan(c.width, sin0);
                        for (int i = 0; i < n; i++) if (Mathf.Abs(cum[i] - cum[k]) <= span0) { deck[i] = true; roadDeck[i] = true; w[i] = .01f; }
                        continue;
                    }
                    if (off > 2.5f) nForced++;
                    raw[k] = roadY; w[k] = 1e6f; isCross[k] = true; nCross++;
                    var rd = new Vector2(Mathf.Sin(c.road_heading * Mathf.Deg2Rad), Mathf.Cos(c.road_heading * Mathf.Deg2Rad));
                    RailCrossings.Add((new Vector2(c.x, c.z), rd, c.width * .5f, raw[k], RailPlans.Count));
                    for (int i = 0; i < n; i++) if (Mathf.Abs(cum[i] - cum[k]) < c.width * .5f + 1.5f) isCross[i] = true;
                }
                var y = R77Smooth1D(raw, w, mu);
                // over a road: the bed lifted (up to RailLift) and the road dipped for the rest, until the deck clears it
                bool again = false;
                foreach (var o in over.Where(o => o.line == li))
                {
                    int k = Mathf.Clamp(o.i, 0, n - 1); var op = new Vector2(o.x, o.z); float roadY = t.Height(o.x, o.z), need = roadY + RailClearAt(op) + RailDeck;
                    if (y[k] >= need) continue;
                    float lifted = Mathf.Min(need, y[k] + RailLiftAt(op)); raw[k] = lifted; w[k] = 1e6f; again = true;
                    if (need - lifted > .05f) dips.Add((o.road, new Vector2(o.x, o.z), need - lifted));
                }
                if (again) y = R77Smooth1D(raw, w, mu);
                for (int i = 1; i < n; i++) { float gr = Mathf.Abs(y[i] - y[i - 1]) / Mathf.Max(cum[i] - cum[i - 1], .1f); if (gr > steepest) { steepest = gr; steepAt = p[i]; } if (gr > .05f && (steepList.Count == 0 || (steepList[steepList.Count - 1].at - p[i]).magnitude > 60f)) steepList.Add((p[i], gr)); }
                for (int i = 0; i < n; i++) { donePts.Add((p[i], y[i])); doneIdx.Add(p[i]); }
                // Round 79 (D): under a road bridge the bed's banks stay within 4 m (an abutment's room): a cut spread 10 m and more along the line took
                // the Canton Road Connector's southbound approach 2 m down in front of its deck (a hole)
                var tight = new bool[n];
                foreach (var (k, _, _) in RailRoadOver(map, p)) for (int i = 0; i < n; i++) if (Mathf.Abs(cum[i] - cum[k]) < 30f) tight[i] = true;
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
                            for (int k = start; k <= end; k++) { pts.Add(p[k].x); pts.Add(p[k].y); ys.Add(y[k]); bl.Add(tight[k] ? 4f : Mathf.Clamp(Mathf.Abs(y[k] - ground[k]) * 2f + 3f, 4f, 22f)); }
                            bedGrades.Add(new RidgeGrade { id = $"railroad {li} {start}", points = pts.ToArray(), y = ys.ToArray(), half = 2.4f, blend = 6f, blends = bl.ToArray(), open_ends = true });   // (open ends: build 1's round ends spilled the bank past a bridge's end onto the road under it)
                            graded += end - start;
                        }
                        start = on ? i : -1;
                    }
                }
                RailPlans.Add(new RailPlan { p = p, y = y, deck = deck, roadDeck = roadDeck, cross = isCross, src = li });
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
                    // (Round 78: the road's own ground, before the bed's banks; the cut's banks slope out by its depth)
                    var dl = q.Select(v => dip * Smooth01(1f - (v - at).magnitude / reach)).ToArray();
                    var ys = q.Select((v, k) => t.Height(v.x, v.y) - dl[k]).ToArray();
                    var bls = dl.Select(dd => Mathf.Clamp(6f + RailBankRun * dd, 7f, 24f)).ToArray();
                    grades.Add(new RidgeGrade { id = $"railroad underpass {name}", points = q.SelectMany(v => new[] { v.x, v.y }).ToArray(), y = ys, half = r.width * .5f + RailShoulder, blend = 7f, blends = bls });
                }
                log.Add($"{name} dipped {dip:0.0} m under the railroad at ({at.x:0}, {at.y:0}) over {reach:0} m each way");
            }
            foreach (var g in bedGrades) GradeAlong(t, g);
            foreach (var g in grades) GradeAlong(t, g);
            railTreePts = new List<Vector2>(); railTreeIdx = new CellIndex(16f);
            foreach (var rp in RailPlans) foreach (var q in rp.p) { railTreePts.Add(q); railTreeIdx.Add(q); }
            float km = RailPlans.Sum(rp => { float s = 0f; for (int i = 1; i < rp.p.Count; i++) s += (rp.p[i] - rp.p[i - 1]).magnitude; return s; }) / 1000f;
            Debug.Log($"[Round77] C: railroad planned: {RailPlans.Count} lines, {km:0.0} km, {graded * 2f / 1000f:0.0} km of bed graded, the steepest grade {steepest * 100f:0.0} % at ({steepAt.x:0}, {steepAt.y:0}); " +
                      $"{nCross} level crossings ({nForced} with the road over 2.5 m above the line's own profile: the line held to it), {nGradeSep} crossings with the road well below made bridges; {nUnder} places lowered under a road bridge; " +
                      $"{RailCrossings.Count} level crossings, {over.Count} bridges over roads ({string.Join(", ", over.Select(o => o.name).Distinct())}); {railDrop.Count} buildings give way ({string.Join(", ", railDrop)}); steeper than 5 %: {(steepList.Count == 0 ? "none" : string.Join(", ", steepList.Select(q => $"{q.g * 100f:0} % at ({q.at.x:0}, {q.at.y:0})")))}; {string.Join("; ", log)}");
        }

        // Round 79 (D): the decks of the roads over the railroad, before the bridge ends and the embankments are graded to them (MapBuilder.cs: before
        // SmoothBridgeEnds). Round 78 changed them in PlanRailroad, after the ground had been shaped to the old decks: the old ground stood through the new
        // deck (the Canton Road Connector's southbound end, a 1.07 m wall in Round 79's first build; Round 78's build 3's "1 m under its road").
        static string deckLog = "";
        static void RailDecks(MapData map)
        {
            deckLog = ""; var path = Path.Combine(ProjectRoot, "Data/tracks/railroad.json"); if (!File.Exists(path) || map.bridges == null) return;
            var d = JsonUtility.FromJson<RailJ>(File.ReadAllText(path)); if (d?.lines == null) return;
            var log = new List<string>();
            // Round 78 (D, Dan's BUG-002 x 646.70 z -5541.05: "fix the overpass": Old 41's deck stepped 1.3 m in 4 m at one end and humped 0.5 m at the other,
            // mapgen lifting it where the bare earth's banks poked through): that bridge gets an even deck, straight between its two ends' heights (the ends
            // stay where Round 27's easing met the approaches)
            foreach (var b in map.bridges)
            {
                if (b.points == null || b.deck_y == null || b.deck_y.Length < 3) continue; var bp = ToPoints(b.points); if (bp.Count != b.deck_y.Length) continue;
                float blen = 0f; for (int i = 1; i < bp.Count; i++) blen += (bp[i] - bp[i - 1]).magnitude; if (blen > 120f) continue;
                bool overRail = d.lines.Any(l => RailRoadOver(map, ToPoints(l.points)).Any(o => o.name == (b.name ?? b.@class)));
                if (!overRail) continue;
                // Round 79 (D, the decks Round 78 listed: the Express Lanes', the Canton Road Connector's (its southbound end 1 m under its road: a
                // wall, the deck's last points stepping 0.9 m) and Cobb Parkway's): every other deck over the line with a step keeps its two ends
                // (where the approaches meet it) and its shape, the steps smoothed out (a membrane over about 6 m, wider until no step of 0.2 m is left)
                if (bp.Min(q => (q - new Vector2(646.7f, -5541.05f)).magnitude) > 60f)
                {
                    float W(float[] a) { float m = 0f; for (int i = 1; i + 1 < a.Length; i++) m = Mathf.Max(m, Mathf.Abs(a[i + 1] - 2f * a[i] + a[i - 1])); return m; }
                    float w0 = W(b.deck_y); if (w0 < .2f) continue;
                    float h = blen / (bp.Count - 1); var wt = new float[bp.Count]; for (int i = 0; i < wt.Length; i++) wt[i] = i == 0 || i == wt.Length - 1 ? 1e6f : 1f;
                    float[] sm = b.deck_y; float reach = 6f;
                    for (int tries = 0; tries < 4; tries++) { sm = R77Smooth1D(b.deck_y, wt, (reach / h) * (reach / h)); if (W(sm) < .2f) break; reach *= 1.6f; }
                    float moved = 0f; for (int i = 0; i < sm.Length; i++) moved = Mathf.Max(moved, Mathf.Abs(sm[i] - b.deck_y[i]));
                    log.Add($"{b.name} over the railroad ({bp[0].x:0}, {bp[0].y:0} to {bp[bp.Count - 1].x:0}, {bp[bp.Count - 1].y:0}): its {w0:0.00} m step smoothed to {W(sm):0.00} m over {reach:0} m (moved at most {moved:0.00} m, the ends kept)");
                    System.Array.Copy(sm, b.deck_y, sm.Length); continue;
                }
                float worst = 0f; for (int i = 1; i + 1 < b.deck_y.Length; i++) worst = Mathf.Max(worst, Mathf.Abs(b.deck_y[i + 1] - 2f * b.deck_y[i] + b.deck_y[i - 1]));
                if (worst < .2f) continue;
                float acc = 0f, y0 = b.deck_y[0], y1 = b.deck_y[b.deck_y.Length - 1];
                for (int i = 1; i < bp.Count - 1; i++) { acc += (bp[i] - bp[i - 1]).magnitude; b.deck_y[i] = Mathf.Lerp(y0, y1, acc / blen); }
                log.Add($"{b.name} over the railroad: an even deck ({blen:0} m, {y0:0.0} to {y1:0.0} m; its worst step was {worst:0.00} m)");
            }
            deckLog = string.Join("; ", log);
        }

        // Round 78 (D): where a road bridge (map.json bridges) passes over this line: the line's point under it and the most the line may stand there
        static List<(int k, float capY, string name)> RailRoadOver(MapData map, List<Vector2> p)
        {
            var res = new List<(int, float, string)>(); if (map.bridges == null) return res;
            foreach (var b in map.bridges)
            {
                if (b.points == null || b.deck_y == null) continue; var bp = ToPoints(b.points); if (bp.Count < 2) continue;
                float x0 = bp.Min(v => v.x) - 4f, x1 = bp.Max(v => v.x) + 4f, z0 = bp.Min(v => v.y) - 4f, z1 = bp.Max(v => v.y) + 4f;
                int bestK = -1, bestJ = -1; float bestD = 3f;
                for (int k = 0; k < p.Count; k++)
                {
                    var q = p[k]; if (q.x < x0 || q.x > x1 || q.y < z0 || q.y > z1) continue;
                    for (int j = 0; j < bp.Count; j++) { float dd = (bp[j] - q).magnitude; if (dd < bestD) { bestD = dd; bestK = k; bestJ = j; } }
                }
                if (bestK < 0) continue;
                res.Add((bestK, b.deck_y[Mathf.Min(bestJ, b.deck_y.Length - 1)] - RailUnder, b.name ?? b.@class));
            }
            return res;
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
            // the level crossings (Round 78 A: grouped per road, the gate arms on hinges and the lamps apart for the running train, MapBuilder.Train.cs)
            int nGear = BuildRailCrossings(t, mats, root);
            WriteRailData(); BuildTrainKit(mats, root);
            Debug.Log($"[Round77] C: railroad built: {nties} ties, {nspans} deck segments, {npiers} piers, {RailCrossings.Count} level crossings ({nGear} crossing posts)");
        }
    }
}
