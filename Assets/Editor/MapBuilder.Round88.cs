using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Round 88 (C, the road work left from Round 83: 64 of 416 crossing runs not clean). Probed on the built scene (R88Probe, Tools/race/probe_ride.py),
// nearly every one is a deck's end that does not meet the road past it: a step of 0.1 to 0.8 m up onto the deck, or the road falling or rising
// away from the deck's end so a car leaves the deck in the air. Two reasons: Round 27's SmoothBridgeEnds follows only the one OSM way that
// starts at the deck's end (Allgood Road's is 6 m long, so a 3.7 m rise went into 6 m), and the passes after it (R81Flyovers, R82ExpressLines /
// Crossings, R60, R62, R77, the railroad) move decks and the ground near their ends again.
// R88DeckEnds runs after all of those, on the decks at C's places only (by bridge number, as the build log numbers them): at each end that meets a
// road on the ground, the road on (OSM's ways chained end to end, so a short piece no longer limits it) is laid as one smooth curve from the deck's
// end at the deck's own grade to the road's own height and grade further on, as long as it needs to be for a car at 40 m/s to stay down (up to
// 150 m; never under another deck), across the road's width with its banks; the ground under the deck's last 0.7 m flush with its top; the
// abutment's record set to the deck as it now stands. Before / after per end in the build log and Assets/Generated/round88_deck_ends.txt.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        // the bridges (the build log's numbers, 1-based) at C's places: Frey Road / I-75 (13, 15, 45), Big Shanty (17, 18, 38), I-75 by Greers Chapel and
        // the Express under it (62, 63, 79, 94, 97), Phillip Landrum (19, 20, 29), the I-575 merge (61, 104), I-75 at Bells Ferry (57, 60, 82), Canton Road (51),
        // the Canton Road Connector's interchange (53, 54, 103), Allgood Road (52), Phillip Landrum by Chastain Road (80), Skip Spann's west deck (30, 39);
        // Round 89: I-75 over Dickson Road (59: a 0.36 m step and a gap at its west end, 2.5 s in the air in Round 88's check)
        static readonly int[] R88Bridges = { 13, 15, 45, 17, 18, 38, 62, 63, 79, 94, 97, 19, 20, 29, 61, 104, 57, 60, 82, 51, 53, 54, 103, 52, 80, 30, 39, 59 };
        const float R88HoldV = 40f, R88MaxL = 150f, R88MinL = 24f, R88Ease = 20f;
        // Round 89: a ramp's deck merging onto another deck (build-log numbers): where their footprints meet, the ramp at the other's top, eased in over the
        // 60 m before. The Canton Road Connector's link 437897716 (103) ran 1.9 m under the eastbound Connector's deck (54) where they meet, into its
        // side wall: Round 88's check cars stopped there 8 times. At one height the bridge builder leaves the wall between them out (R82SideOpen).
        static readonly (int ramp, int onto)[] R89Merges = { (103, 54) };
        // Round 89 (A): ends put back as they were before Round 88 (the pass leaves them, their deck untouched): "51e" = bridge 51's end, "80s" = its start.
        // A dry run can try others with -r89Back "51e;80s" (added to these) or -r88Off (the whole pass off: the build before Round 88 C).
        // 80s: deck 80's start at (577, -3601) sits on ramp 451950527's line (the I-575 Express flyover's way on to the I-75 Express Lanes); Round 88 made
        // it flush, so a car on the ramp drove up onto the deck and fell 15 m off its side where the ramp leaves it (2.2 s in the air, Round 87: 0.76 s)
        static readonly string[] R89Back = { "80s" };

        static HashSet<string> R89BackSet()
        {
            var set = new HashSet<string>(R89Back);
            var a = System.Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < a.Length; i++) if (a[i] == "-r89Back") foreach (var s in a[i + 1].Split(';', ',')) if (s.Trim().Length > 0) set.Add(s.Trim());
            return set;
        }

        static void R89Merge(MapData map, int ri, int oi, List<string> log)
        {
            if (ri < 0 || oi < 0 || ri >= map.bridges.Length || oi >= map.bridges.Length) return;
            var r = map.bridges[ri]; var o = map.bridges[oi]; if (r.deck_y == null || o.deck_y == null) return;
            var P = ToPoints(r.points); var Q = ToPoints(o.points); int n = P.Count; if (n < 2 || r.deck_y.Length < n || Q.Count < 2 || o.deck_y.Length < Q.Count) return;
            float lr = R82Lift(r.@class), lo = R82Lift(o.@class), meet = (r.width + o.width) * .5f + .3f;
            var cum = new float[n]; for (int k = 1; k < n; k++) cum[k] = cum[k - 1] + Vector2.Distance(P[k - 1], P[k]);
            // the other deck's top beside each point (NaN where their footprints do not meet)
            var want = new float[n];
            for (int k = 0; k < n; k++)
            {
                float bd = float.MaxValue, top = float.NaN;
                for (int i = 0; i + 1 < Q.Count; i++)
                {
                    var ab = Q[i + 1] - Q[i]; float t = Mathf.Clamp01(Vector2.Dot(P[k] - Q[i], ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
                    float d = (Q[i] + ab * t - P[k]).magnitude; if (d < bd) { bd = d; top = Mathf.Lerp(o.deck_y[i], o.deck_y[i + 1], t) + lo; }
                }
                want[k] = bd <= meet ? top - lr : float.NaN;
            }
            int kb = -1; for (int k = 0; k < n; k++) if (!float.IsNaN(want[k])) { kb = k; break; }
            if (kb < 0) { log.Add($"merge: bridge {ri + 1} never meets bridge {oi + 1}, left"); return; }
            float d0 = want[kb] - r.deck_y[kb];
            if (Mathf.Abs(d0) > 4f) { log.Add($"merge: bridge {ri + 1} meets bridge {oi + 1} {d0:+0.00;-0.00} m off at ({P[kb].x:0}, {P[kb].y:0}), too far, left"); return; }
            float most = 0f; int moved = 0;
            for (int k = 0; k < n; k++)
            {
                float ny;
                if (k >= kb) ny = float.IsNaN(want[k]) ? r.deck_y[k] + d0 : want[k];
                else { float u = (cum[k] - (cum[kb] - 60f)) / 60f; if (u <= 0f) continue; ny = r.deck_y[k] + d0 * Mathf.SmoothStep(0f, 1f, u); }
                if (Mathf.Abs(ny - r.deck_y[k]) > .01f) { most = Mathf.Max(most, Mathf.Abs(ny - r.deck_y[k])); moved++; r.deck_y[k] = ny; }
            }
            log.Add($"merge: bridge {ri + 1} ({r.name} {r.osm_id}) onto bridge {oi + 1} ({o.name} {o.osm_id}) from ({P[kb].x:0}, {P[kb].y:0}), {cum[n - 1] - cum[kb]:0} m beside it: " +
                    $"{d0:+0.00;-0.00} m there, eased in over 60 m; {moved} point(s) moved by up to {most:0.00} m");
        }

        static void R88DeckEnds(MapData map, TerrainGrid terrain)
        {
            var log = new List<string> { "Round 88 (C): the decks' ends at Round 83's leftover places laid onto their roads (MapBuilder.Round88.cs R88DeckEnds). Per end: the step and the sharpest crest / sag before -> after (the speed a car holds wheels down)" };
            const float g = 9.81f, slab = 0.7f;
            if (System.Environment.GetCommandLineArgs().Contains("-r88Off")) { log.Add("-r88Off: the pass is off (the build before Round 88 C)"); System.IO.File.WriteAllLines(System.IO.Path.Combine(ProjectRoot, GeneratedDir, "round88_deck_ends.txt"), log); return; }
            var back = R89BackSet();
            var pick = new HashSet<int>(R88Bridges.Select(b => b - 1));
            // every deck's footprint (to stop a road's easing short of passing under another deck)
            var idx = new CellIndex(16f); var segs = new List<(int b, int i)>(); var near = new List<int>();
            for (int bi = 0; bi < map.bridges.Length; bi++)
            {
                var q = ToPoints(map.bridges[bi].points);
                for (int i = 0; i + 1 < q.Count; i++) { segs.Add((bi, i)); idx.Add(q[i], q[i + 1], map.bridges[bi].width * .5f + 1f); }
            }
            // another deck over p (its slab more than 2.5 m over 'y'; a deck beside this one at about its height is not over it)
            bool UnderOther(Vector2 p, float y, int self, HashSet<int> chain)
            {
                foreach (int k in idx.Near(p, 0f, near))
                {
                    var (b, i) = segs[k]; if (b == self || chain.Contains(b)) continue; var bb = map.bridges[b]; if (bb.deck_y == null || i + 1 >= bb.deck_y.Length) continue;
                    var a = new Vector2(bb.points[2 * i], bb.points[2 * i + 1]); var e = new Vector2(bb.points[2 * i + 2], bb.points[2 * i + 3]); var ab = e - a;
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
                    // Round 89: only a deck too low over the road to lay it there (under 5 m clear, R82MinClear): Round 88 stopped at any deck over it, so the
                    // Canton Road Connector's ends under the Express Lanes (6 m and more clear) were left with their steps
                    float slabB = Mathf.Lerp(bb.deck_y[i], bb.deck_y[i + 1], t) - slab;
                    if ((a + ab * t - p).magnitude < bb.width * .5f + 1f && slabB > y + 2.5f && slabB < y + R82MinClear) return true;
                }
                return false;
            }
            var roads = map.roads.Select(r => ToPoints(r.points)).ToList();
            foreach (var (ramp, onto) in R89Merges) R89Merge(map, ramp - 1, onto - 1, log);
            foreach (int bi in pick.OrderBy(b => b))
            {
                if (bi < 0 || bi >= map.bridges.Length) continue;
                var b = map.bridges[bi]; var pts = ToPoints(b.points); int n = pts.Count; if (n < 2 || b.deck_y == null || b.deck_y.Length < n) continue;
                float lift = R82Lift(b.@class);
                foreach (bool start in new[] { true, false })
                {
                    int ie = start ? 0 : n - 1, ip = start ? 1 : n - 2;
                    var E = pts[ie]; var outward = (E - pts[ip]).normalized;
                    string tag = $"bridge {bi + 1} ({b.name} {b.osm_id}) {(start ? "start" : "end")} at ({E.x:0}, {E.y:0})";
                    if (EdgeCut(b, map)) { log.Add($"{tag}: cut by the map's edge, left"); continue; }
                    if (back.Contains($"{bi + 1}{(start ? "s" : "e")}")) { log.Add($"{tag}: put back as before Round 88 (R89Back), left"); continue; }
                    // the decks chained to this one (end to end within 3 m): this end runs into another deck, left
                    var chain = new HashSet<int>(); bool chained = false;
                    for (int bj = 0; bj < map.bridges.Length; bj++)
                    {
                        if (bj == bi) continue; var (a, z) = Ends(map.bridges[bj], false);
                        if ((a - E).magnitude < 3f || (z - E).magnitude < 3f) chained = true;
                        var (sa, sz) = Ends(b, false); if ((a - sa).magnitude < 3f || (z - sa).magnitude < 3f || (a - sz).magnitude < 3f || (z - sz).magnitude < 3f) chain.Add(bj);
                    }
                    if (chained) { log.Add($"{tag}: runs into the next deck, left"); continue; }
                    // the road on the ground from this end, OSM's ways chained while they carry straight on (within 40 degrees)
                    var path = new List<Vector2> { E }; var used = new HashSet<int>(); Vector2 head = outward; float len = 0f;
                    for (int guard = 0; guard < 40 && len < R88MaxL + 10f; guard++)
                    {
                        var last = path[path.Count - 1]; int best = -1; bool rev = false; float bestDot = Mathf.Cos(40f * Mathf.Deg2Rad);
                        for (int ri = 0; ri < roads.Count; ri++)
                        {
                            if (used.Contains(ri)) continue; var q = roads[ri]; if (q.Count < 2) continue;
                            float tol = path.Count == 1 ? 2.5f : 1.5f;
                            if ((q[0] - last).magnitude < tol) { float d = Vector2.Dot((q[1] - q[0]).normalized, head); if (d > bestDot) { bestDot = d; best = ri; rev = false; } }
                            if ((q[q.Count - 1] - last).magnitude < tol) { float d = Vector2.Dot((q[q.Count - 2] - q[q.Count - 1]).normalized, head); if (d > bestDot) { bestDot = d; best = ri; rev = true; } }
                        }
                        // Round 89: a short road that stops at a junction without a straight continuation (Greers Chapel's ends east of its decks: 23 and 24 m,
                        // too short to lay 2.3 m into) carries on along the nearest road end within 8 m, turning up to 100 degrees
                        bool loose = false;
                        if (best < 0 && len < 60f)
                        {
                            float bd2 = 8f, cosT = Mathf.Cos(100f * Mathf.Deg2Rad);
                            for (int ri = 0; ri < roads.Count; ri++)
                            {
                                if (used.Contains(ri)) continue; var q = roads[ri]; if (q.Count < 2) continue;
                                float d0 = (q[0] - last).magnitude, d1 = (q[q.Count - 1] - last).magnitude;
                                if (d0 < bd2 && Vector2.Dot((q[1] - q[0]).normalized, head) > cosT) { bd2 = d0; best = ri; rev = false; }
                                if (d1 < bd2 && Vector2.Dot((q[q.Count - 2] - q[q.Count - 1]).normalized, head) > cosT) { bd2 = d1; best = ri; rev = true; }
                            }
                            loose = best >= 0;
                        }
                        if (best < 0) break;
                        used.Add(best); var w = new List<Vector2>(roads[best]); if (rev) w.Reverse();
                        if (loose && (w[0] - path[path.Count - 1]).magnitude > .1f) { len += (w[0] - path[path.Count - 1]).magnitude; path.Add(w[0]); }
                        for (int k = 1; k < w.Count; k++) { len += (w[k] - path[path.Count - 1]).magnitude; path.Add(w[k]); }
                        head = (path[path.Count - 1] - path[path.Count - 2]).normalized;
                    }
                    if (path.Count < 2) { log.Add($"{tag}: no road on the ground carries on from it, left"); continue; }
                    var cum = new float[path.Count]; for (int i = 1; i < path.Count; i++) cum[i] = cum[i - 1] + (path[i] - path[i - 1]).magnitude;
                    Vector2 At(float s)
                    {
                        if (s <= 0f) return E + outward * s;
                        for (int i = 1; i < path.Count; i++) if (s <= cum[i]) return Vector2.Lerp(path[i - 1], path[i], (s - cum[i - 1]) / Mathf.Max(cum[i] - cum[i - 1], 1e-4f));
                        return path[path.Count - 1];
                    }
                    // a kink in the deck's very last segment (Phillip Landrum's north deck rises 0.16 m over its last 3 m where the rest falls 4 %) straightened
                    // to the grade before it, as SmoothBridgeEnds does
                    var keepDeck = (float[])b.deck_y.Clone();   // (put back if this end is left as it is)
                    // Build #1's probe: several decks turn up over their last 2 to 4 m (Allgood Road 0.75 m, I-75 by Frey Road 1.2 m: a kicker), over more
                    // than one segment. The deck's points within 6 m of its end are put on the line through its points 6 to 20 m back (a least-squares fit)
                    {
                        var dEnd = new float[n]; for (int k = 1; k < n; k++) { int a0 = start ? k : n - 1 - k, a1 = start ? k - 1 : n - k; dEnd[a0] = dEnd[a1] + Vector2.Distance(pts[a0], pts[a1]); }
                        // Round 89: first the line through its points 10 to 30 m back, when the deck lies straight there (no point over 0.15 m off it), laid over
                        // the last 10 m: Frey Road's deck (13) dropped 0.5 m over 3 m between 6 and 10 m in, so the 6 to 20 m fit took the kink in (0.6 s in the air)
                        float win = 6f;
                        float sx = 0f, sy = 0f, sxx = 0f, sxy = 0f; int m = 0;
                        {
                            float tx = 0f, ty = 0f, txx = 0f, txy = 0f; int tm = 0;
                            for (int k = 0; k < n; k++) if (dEnd[k] >= 10f && dEnd[k] <= 30f) { tx += dEnd[k]; ty += b.deck_y[k]; txx += dEnd[k] * dEnd[k]; txy += dEnd[k] * b.deck_y[k]; tm++; }
                            if (tm >= 3 && Mathf.Abs(tm * txx - tx * tx) > 1e-3f)
                            {
                                float g2 = (tm * txy - tx * ty) / (tm * txx - tx * tx), y2 = (ty - g2 * tx) / tm; float worst = 0f;
                                for (int k = 0; k < n; k++) if (dEnd[k] >= 10f && dEnd[k] <= 30f) worst = Mathf.Max(worst, Mathf.Abs(b.deck_y[k] - (y2 + g2 * dEnd[k])));
                                if (worst < .15f && Mathf.Abs(g2) < .12f) { sx = tx; sy = ty; sxx = txx; sxy = txy; m = tm; win = 10f; }
                            }
                        }
                        if (m == 0) for (int k = 0; k < n; k++) if (dEnd[k] >= 6f && dEnd[k] <= 20f) { sx += dEnd[k]; sy += b.deck_y[k]; sxx += dEnd[k] * dEnd[k]; sxy += dEnd[k] * b.deck_y[k]; m++; }
                        if (m < 2) { int kf = -1; for (int k = 0; k < n; k++) if (dEnd[k] >= 3f && (kf < 0 || dEnd[k] < dEnd[kf])) kf = k; int kg = -1; for (int k = 0; k < n; k++) if (kf >= 0 && dEnd[k] > dEnd[kf] && (kg < 0 || dEnd[k] < dEnd[kg])) kg = k;
                                     if (kf >= 0 && kg >= 0) { sx = dEnd[kf] + dEnd[kg]; sy = b.deck_y[kf] + b.deck_y[kg]; sxx = dEnd[kf] * dEnd[kf] + dEnd[kg] * dEnd[kg]; sxy = dEnd[kf] * b.deck_y[kf] + dEnd[kg] * b.deck_y[kg]; m = 2; win = 6f; } }
                        if (m >= 2 && Mathf.Abs(m * sxx - sx * sx) > 1e-3f)
                        {
                            float gd = (m * sxy - sx * sy) / (m * sxx - sx * sx), y0d = (sy - gd * sx) / m;   // y = y0d + gd * (distance from the end)
                            if (Mathf.Abs(gd) < .12f)
                            {
                                float dmost = 0f; int moved = 0;
                                for (int k = 0; k < n; k++) if (dEnd[k] < win) { float want = y0d + gd * dEnd[k]; if (Mathf.Abs(b.deck_y[k] - want) > .03f) { dmost = Mathf.Max(dmost, Mathf.Abs(b.deck_y[k] - want)); b.deck_y[k] = want; moved++; } }
                                if (moved > 0) log.Add($"{tag}: the deck's last {win:0} m put back on its own grade ({-gd * 100f:0.0} % toward the end), {moved} point(s) moved by up to {dmost:0.00} m");
                            }
                        }
                    }
                    // the deck's top and grade over its last 8 m or more
                    int ig = ip; float span = Vector2.Distance(pts[ie], pts[ip]);
                    while (span < 8f && (start ? ig + 1 < n : ig - 1 >= 0)) { int nx = start ? ig + 1 : ig - 1; span += Vector2.Distance(pts[ig], pts[nx]); ig = nx; }
                    float deckTop = b.deck_y[ie] + lift, grade = (b.deck_y[ie] - b.deck_y[ig]) / Mathf.Max(span, .1f);
                    // Round 89: the straightening above lowers a deck's last metres by up to 3 m (I-75 at Bells Ferry), so the ground under them stood up
                    // through the deck (Round 88's probe: 0.25 to 0.8 m faces 1 to 6 m in from the end at Frey Road, Big Shanty, Bells Ferry; a car hitting
                    // one at 30 m/s flew 2.5 s). The ground under the deck's last 30 m (its footprint) kept under the slab wherever it stands over the top.
                    int ClampUnder()
                    {
                        float half0 = b.width * .5f + .35f; int lowered = 0;
                        var dE = new float[n]; for (int k = 1; k < n; k++) { int a0 = start ? k : n - 1 - k, a1 = start ? k - 1 : n - k; dE[a0] = dE[a1] + Vector2.Distance(pts[a0], pts[a1]); }
                        float bx0 = float.MaxValue, bx1 = float.MinValue, bz0 = float.MaxValue, bz1 = float.MinValue;
                        for (int k = 0; k < n; k++) if (dE[k] <= 32f) { bx0 = Mathf.Min(bx0, pts[k].x); bx1 = Mathf.Max(bx1, pts[k].x); bz0 = Mathf.Min(bz0, pts[k].y); bz1 = Mathf.Max(bz1, pts[k].y); }
                        EditTerrain(terrain, Rect.MinMaxRect(bx0 - half0, bz0 - half0, bx1 + half0, bz1 + half0), (x, z, y) =>
                        {
                            var p = new Vector2(x, z);
                            if (Vector2.Dot(p - E, outward) > -1.5f) return y;   // the end's own 1.5 m (flush with the top) and the road past it: the laying's
                            float bestD = float.MaxValue, top = 0f;
                            for (int k = 0; k + 1 < n; k++)
                            {
                                if (Mathf.Min(dE[k], dE[k + 1]) > 30f) continue;
                                var ab = pts[k + 1] - pts[k]; float t = Mathf.Clamp01(Vector2.Dot(p - pts[k], ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
                                float dd = (pts[k] + ab * t - p).magnitude; if (dd < bestD) { bestD = dd; top = Mathf.Lerp(b.deck_y[k], b.deck_y[k + 1], t) + lift; }
                            }
                            if (bestD > half0 || y <= top - .05f) return y;
                            lowered++; return top - slab - .05f;
                        });
                        if (lowered > 0) log.Add($"{tag}: the ground under the deck's last 30 m lowered under its slab where it stood over the top ({lowered} samples)");
                        return lowered;
                    }
                    // as far as it may reach: the chained road's length, and short of passing under another deck
                    float Lmax = Mathf.Min(R88MaxL, cum[cum.Length - 1]);
                    for (float s = 2f; s <= Lmax; s += 1f) { var p = At(s); if (UnderOther(p, Mathf.Max(terrain.Height(p.x, p.y), deckTop), bi, chain)) { Lmax = Mathf.Max(0f, s - 6f); break; } }
                    // Round 89 (A): never over a railroad crossing (PlanRailroad graded the road and the bed to each other before this; Canton Road's deck 51
                    // was laid across the tracks 60 m past its end and the rails stood out of the road: a landing at 8.9 m/s there). Stop 8 m short of the
                    // crossing, the curve and its ease inside that. Attempt 2 (attempt 1's 21 m curve and 20 m ease flew cars 1.7 s): the curve as long as
                    // it can be, its ease only 6 m, ending 2 m short of the crossing's pavement
                    float Lcurve = Lmax, ease = R88Ease;
                    if (RailCrossings != null)
                        for (float s = 0f; s <= Lmax + R88Ease; s += 1f)
                        {
                            var p = At(s); bool hit = false;
                            foreach (var rc in RailCrossings) if ((rc.at - p).sqrMagnitude < 100f) { hit = true; break; }
                            if (hit) { ease = 6f; Lmax = Mathf.Min(Lmax, s - 2f); Lcurve = Mathf.Max(0f, Lmax - ease); log.Add($"{tag}: a railroad crossing {s:0} m on: the curve to {Lcurve:0} m at most, eased out by {Lmax:0} m"); break; }
                        }
                    if (Lmax < 12f) { System.Array.Copy(keepDeck, b.deck_y, keepDeck.Length); log.Add($"{tag}: the road under another deck within {Lmax + 6f:0} m, left (its deck as it was)"); continue; }
                    int NM = Mathf.CeilToInt(Lmax / .5f) + 1; var GM = new float[NM];
                    for (int i = 0; i < NM; i++) { var p = At(i * .5f); GM[i] = terrain.Height(p.x, p.y); }
                    float Gs(float s) => GM[Mathf.Clamp(Mathf.RoundToInt(s / .5f), 0, NM - 1)];
                    // the sharpest crest and sag (second difference over 4 m: the terrain's own 1 m grid is noise at 2 m) of a profile from 6 m back on the deck to 'to'
                    (float crest, float sag) Bend(System.Func<float, float> y, float to)
                    {
                        float c = 0f, sg = 0f;
                        for (float s = -2f; s <= to - 4f; s += 1f)
                        {
                            float Y(float u) => u < 0f ? deckTop + grade * u : y(u);
                            float k = (Y(s - 4f) - 2f * Y(s) + Y(s + 4f)) / 16f; c = Mathf.Max(c, -k); sg = Mathf.Max(sg, k);
                        }
                        return (c, sg);
                    }
                    float V(float k) => k > 1e-5f ? Mathf.Sqrt(g / k) : 999f;
                    float step0 = deckTop - GM[0];
                    var before = Bend(Gs, Mathf.Min(Lmax, 60f));
                    if (Mathf.Abs(step0) < .03f && V(before.crest) >= R88HoldV && before.sag < .03f) { ClampUnder(); log.Add($"{tag}: step {step0:0.00} m, crest held to {V(before.crest):0} m/s: fine, left"); continue; }
                    // the curve: a cubic Hermite from the deck's end (its top, its grade) to the road at L (its height, its grade there over 10 m)
                    float y0 = deckTop - .01f, L = 0f, yL = 0f, gL = 0f, bestK = float.MaxValue;
                    float H(float s, float len2, float yl, float gl)
                    {
                        float t = Mathf.Clamp01(s / len2), t2 = t * t, t3 = t2 * t;
                        return (2f * t3 - 3f * t2 + 1f) * y0 + (t3 - 2f * t2 + t) * len2 * grade + (-2f * t3 + 3f * t2) * yl + (t3 - t2) * len2 * gl;
                    }
                    // the shortest L whose whole profile a car holds at R88HoldV (its crest) with a gentle sag: the curve, then the ease from its grade line back onto
                    // the road's own ground over 20 m, and 10 m of that ground on (Round 88 judged the curve alone: Canton Road's east end kept a 28 m/s crest in its ease).
                    // The road's far grade gl over 10 m either side of L (its 1 m grid smoothed out); the bends over 4 m, as Bend measures them
                    float Ease = ease;
                    float Prof(float s, float ll, float yl, float gl) => s >= ll ? Mathf.Lerp(yl + gl * (s - ll), Gs(s), Smooth01((s - ll) / Ease)) : H(s, ll, yl, gl);
                    for (float ll = Mathf.Min(R88MinL, Lcurve); ll <= Lcurve + .01f; ll += 4f)
                    {
                        float a0 = Mathf.Max(ll - 10f, 0f), a1 = Mathf.Min(ll + 10f, Lmax);
                        float yl = 0f; int cnt = 0; for (float s = a0; s <= a1 + .01f; s += .5f) { yl += Gs(s); cnt++; } yl /= Mathf.Max(cnt, 1);   // its height: the mean about L (the grade line through it)
                        float gl = (Gs(a1) - Gs(a0)) / Mathf.Max(a1 - a0, 1f); yl += gl * (ll - (a0 + a1) * .5f);
                        float llc = ll, ylc = yl, glc = gl;
                        var bd = Bend(s => Prof(s, llc, ylc, glc), Mathf.Min(Lmax, ll + Ease + 10f));
                        float k = Mathf.Max(bd.crest, bd.sag * .4f);
                        if (k < bestK) { bestK = k; L = ll; yL = yl; gL = gl; }
                        if (V(bd.crest) >= R88HoldV && bd.sag < .02f) { L = ll; yL = yl; gL = gl; break; }
                    }
                    // past L the road eases from the grade line back onto its own ground over 20 m
                    float Target(float s) => Prof(s, L, yL, gL);
                    float Lend = Mathf.Min(L + Ease, Lmax);
                    float half = b.width * .5f, bank = 3.5f, reach = half + bank;
                    float x0 = float.MaxValue, x1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue;
                    for (float s = -4f; s <= Lend; s += 1f) { var p = At(s); x0 = Mathf.Min(x0, p.x); x1 = Mathf.Max(x1, p.x); z0 = Mathf.Min(z0, p.y); z1 = Mathf.Max(z1, p.y); }
                    int changed = 0, underSkip = 0; float most = 0f;
                    EditTerrain(terrain, Rect.MinMaxRect(x0 - reach, z0 - reach, x1 + reach, z1 + reach), (x, z, y) =>
                    {
                        var p = new Vector2(x, z); float s, d;
                        float sd = Vector2.Dot(p - E, outward);
                        if (sd < 0f) { s = sd; d = Mathf.Abs(Vector2.Dot(p - E, new Vector2(-outward.y, outward.x))); }
                        else
                        {
                            float bestD = float.MaxValue; s = 0f;
                            for (int i = 0; i + 1 < path.Count && cum[i] <= Lend + reach; i++)
                            {
                                var ab = path[i + 1] - path[i]; float t = Mathf.Clamp01(Vector2.Dot(p - path[i], ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
                                float dd = (path[i] + ab * t - p).sqrMagnitude; if (dd < bestD) { bestD = dd; s = cum[i] + t * ab.magnitude; }
                            }
                            d = Mathf.Sqrt(bestD);
                        }
                        if (s > Lend || d > reach || s < -1.5f) return y;   // Round 89: flush under the deck's last 1.5 m (was 0.7: on a diagonal end the 0.65 m sample grid left the point at the end interpolated from a lower sample, Greers Chapel kept a 0.38 m step)
                        if (s < 0f) { if (d > half + .35f) return y; float u = deckTop + grade * s - .02f; if (Mathf.Abs(u - y) < 1e-3f) return y; changed++; most = Mathf.Max(most, Mathf.Abs(u - y)); return u; }
                        if (UnderOther(p, Target(s), bi, chain)) { if (s < 6f && d <= half) underSkip++; return y; }
                        // the change along the line at s, faded out across the banks and over the last 6 m before L
                        // (the height itself, level across the road: adding the change at the line to each sample overshot where the ground is steep
                        // right at a deck's end; Frey Road's Express deck kept a 0.7 m spike at its end)
                        float fade = (d <= half + .5f ? 1f : 1f - Mathf.SmoothStep(0f, 1f, (d - half - .5f) / (bank - .5f)));
                        float ny = y + (Target(s) - y) * fade;
                        if (Mathf.Abs(ny - y) > 1e-3f) { changed++; most = Mathf.Max(most, Mathf.Abs(ny - y)); }
                        return ny;
                    });
                    ClampUnder();
                    for (int i = 0; i < NM; i++) { var p = At(i * .5f); GM[i] = terrain.Height(p.x, p.y); }
                    if (underSkip > 0) log.Add($"{tag}: {underSkip} samples within 6 m of the end left (under another deck)");
                    if (Mathf.Abs(deckTop - GM[0]) > .05f) log.Add($"{tag}: still {deckTop - GM[0]:0.00} m off at the end: deck top {deckTop:0.00}, the curve {Target(0f):0.00}, the ground {GM[0]:0.00} / {Gs(1f):0.00} / {Gs(2f):0.00} at 0 / 1 / 2 m, L {L:0} of {Lmax:0}");
                    var after = Bend(Gs, Mathf.Min(Lmax, L + 20f));
                    // the abutment's record: the deck as it now stands
                    foreach (var end in BridgeEnds) if (end.bridge == bi && end.start == start) { end.deckTop = deckTop; end.slabBottom = deckTop - slab; end.ground = GM[0]; }
                    log.Add($"{tag}: step {step0:0.00} -> {deckTop - GM[0]:0.00} m, crest held to {V(before.crest):0} -> {V(after.crest):0} m/s, sag {before.sag:0.000} -> {after.sag:0.000} per m; " +
                            $"laid over {L:0} m of {Lmax:0} ({used.Count} way(s)), the ground moved by up to {most:0.00} m ({changed} samples)");
                }
            }
            Debug.Log("[Round88] C: " + string.Join("\n", log));
            try { System.IO.File.WriteAllLines(System.IO.Path.Combine(ProjectRoot, GeneratedDir, "round88_deck_ends.txt"), log); } catch (System.Exception e) { Debug.LogWarning("[Round88] " + e.Message); }
        }
    }
}
