using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Round 81 (A, Dan's BUG-009 x 508 z -3173: "this needs to be smoothed. The bridge obvs should be higher", BUG-010 x 497 z -3143: "needs to be smoothed";
// H, Dan: "the express lanes are usually bridges when they cross 75/575. So they should be that way"): the I-75 / I-575 junction's flyovers (the I-575
// Express flyover 451950526, I-575 southbound's bridges 163463460 and 536793083) and every Northwest Corridor Express Lanes deck, each laid with the road
// 250 m before and after it as one smooth profile a car holds at 35 m/s (a crest's curvature under g / v^2, a sag under twice that), the slab's underside at
// least 6 m over every road it crosses (a deck crossing over this one: under it by as much), never under the ground. The profile: the heights now (the decks',
// the ground along the roads), raised to those clearances, blurred along (a Gaussian, 20 m, then 30 m if a crest is still too sharp: the tuning limit),
// pinned to the road as it was at the line's outer ends and where another road joins it (a merge, a junction), raised again, repeated. The decks take its
// heights; the roads are graded to it across their width, banking back to the ground beside them (never onto another road's pavement); the ground under a
// deck is cut to 0.3 m under its slab. Runs after SmoothBridgeEnds (which it supersedes on these decks: their ends' records for the abutments updated) and
// before R60 / R62 / R77 (the decks R62 lays at the old core's edge are left to it). Report: Assets/Generated/round81_flyovers.txt.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        static readonly long[] R81JunctionIds = { 451950526, 163463460, 536793083 };
        const float R81Clear = 6f, R81V = 35f, R81Reach = 250f, R81Slab = .7f, R81BoundR = 250f;

        // Round 82: 'pick' chooses the decks (default: the junction's flyovers; the Express Lanes are R82ExpressLines' now), 'report' the report's file
        static void R81Flyovers(MapData map, TerrainGrid terrain, System.Func<int, Bridge, bool> pick = null, string report = "round81_flyovers.txt")
        {
            if (map.bridges == null) return;
            var log = new List<string> { "Round 81 (A, H): flyovers laid with their roads as one curve (MapBuilder.Round81.cs). Per deck chain: its length, the road before / after, " +
                                         "the speed its sharpest crest is held at before -> after, the least clearance under its slab over a road it crosses before -> after, the most it moved" };
            var want = new HashSet<int>();
            for (int i = 0; i < map.bridges.Length; i++)
            {
                var b = map.bridges[i]; if (b.points == null || b.points.Length < 4 || b.deck_y == null) continue;
                if (pick != null ? pick(i, b) : R81JunctionIds.Contains(b.osm_id)) want.Add(i);
            }
            bool OnSeam(Vector2 e) => (Mathf.Abs(e.y) < .6f && e.x >= 0f && e.x <= map.size_x_m) || (Mathf.Abs(e.x) < .6f && e.y >= 0f && e.y <= map.size_z_m);
            // the chains: pieces of the set whose ends meet (within 1.5 m), in order
            var left = new List<int>(want); var chains = new List<List<(int b, int i)>>();
            List<(int b, int i)> Pts(int bi, bool fwd) { int n = map.bridges[bi].points.Length / 2; var o = new List<(int, int)>(); for (int k = 0; k < n; k++) o.Add((bi, fwd ? k : n - 1 - k)); return o; }
            Vector2 P(int bi, int i) => new Vector2(map.bridges[bi].points[2 * i], map.bridges[bi].points[2 * i + 1]);
            while (left.Count > 0)
            {
                var c = Pts(left[0], true); left.RemoveAt(0);
                for (bool grew = true; grew;)
                {
                    grew = false;
                    for (int k = 0; k < left.Count && !grew; k++)
                    {
                        int bi = left[k]; int n = map.bridges[bi].points.Length / 2; Vector2 s = P(bi, 0), e = P(bi, n - 1);
                        Vector2 head = P(c[0].b, c[0].i), tail = P(c[c.Count - 1].b, c[c.Count - 1].i);
                        if ((s - tail).magnitude < 1.5f) { c.AddRange(Pts(bi, true).Skip(1)); grew = true; }
                        else if ((e - tail).magnitude < 1.5f) { c.AddRange(Pts(bi, false).Skip(1)); grew = true; }
                        else if ((e - head).magnitude < 1.5f) { c.InsertRange(0, Pts(bi, true).Take(n - 1)); grew = true; }
                        else if ((s - head).magnitude < 1.5f) { c.InsertRange(0, Pts(bi, false).Take(n - 1)); grew = true; }
                        if (grew) left.RemoveAt(k);
                    }
                }
                chains.Add(c);
            }
            // every road and deck as segments (for crossings, joins and the grading's hold)
            var roadSegs = new List<(Vector2 a, Vector2 b, float half, int bridge, int bi, int seg)>();   // bridge: -1 a road on the ground
            foreach (var r in map.roads) { var q = ToPoints(r.points); for (int i = 0; i + 1 < q.Count; i++) roadSegs.Add((q[i], q[i + 1], r.width * .5f, -1, -1, i)); }
            for (int bi = 0; bi < map.bridges.Length; bi++) { var q = ToPoints(map.bridges[bi].points); for (int i = 0; i + 1 < q.Count; i++) roadSegs.Add((q[i], q[i + 1], map.bridges[bi].width * .5f, 1, bi, i)); }
            var segIdx = new CellIndex(24f); foreach (var s in roadSegs) segIdx.Add(s.a, s.b, s.half + 1f);
            var near = new List<int>();
            float g = 9.81f, kCrest = g / (R81V * R81V);
            int done = 0;
            foreach (var chain in chains)
            {
                var ends = new[] { P(chain[0].b, chain[0].i), P(chain[chain.Count - 1].b, chain[chain.Count - 1].i) };
                var b0 = map.bridges[chain[0].b];
                string label = $"{(string.IsNullOrEmpty(b0.name) ? b0.@class : b0.name)} ({string.Join(" + ", chain.Select(c => map.bridges[c.b].osm_id).Distinct())}) from ({ends[0].x:0}, {ends[0].y:0}) to ({ends[1].x:0}, {ends[1].y:0})";
                if (chain.Select(c => c.b).Distinct().Any(bi => EdgeCut(map.bridges[bi], map))) { log.Add($"{label}: cut by the map's edge, left"); continue; }
                if (OnSeam(ends[0]) || OnSeam(ends[1]) || chain.Any(c => OnSeam(P(c.b, c.i)))) { log.Add($"{label}: at the old core's edge, left to Round 62"); continue; }
                // the roads on the ground beyond both ends, up to R81Reach (stopping short of another deck)
                List<Vector2> Beyond(Vector2 E, Vector2 outward)
                {
                    foreach (var r in map.roads)
                    {
                        var q = ToPoints(r.points); if (q.Count < 2 || r.bridge) continue;
                        List<Vector2> l = null;
                        if (Vector2.Distance(q[0], E) < 2f && Vector2.Dot(q[1] - q[0], outward) > 0f) l = q;
                        else if (Vector2.Distance(q[q.Count - 1], E) < 2f && Vector2.Dot(q[q.Count - 2] - q[q.Count - 1], outward) > 0f) { q.Reverse(); l = q; }
                        if (l == null) continue;
                        l[0] = E; l = ExtendPath(map, l, R81Reach);
                        var o = new List<Vector2> { l[0] }; float acc = 0f;
                        for (int i = 1; i < l.Count && acc < R81Reach; i++) { float d = (l[i] - l[i - 1]).magnitude; if (acc + d > R81Reach) { o.Add(Vector2.Lerp(l[i - 1], l[i], (R81Reach - acc) / d)); break; } o.Add(l[i]); acc += d; }
                        return o;
                    }
                    return new List<Vector2> { E };
                }
                var A = Beyond(ends[0], (ends[0] - P(chain[1].b, chain[1].i)).normalized);
                var Z = Beyond(ends[1], (ends[1] - P(chain[chain.Count - 2].b, chain[chain.Count - 2].i)).normalized);
                // one line at about 1 m on the roads (the deck's own points on it): kind -1 road A, 0 deck, 1 road Z
                var pts = new List<Vector2>(); var kind = new List<int>(); var deckK = new List<int>();
                if (A.Count > 1) { var ra = Resample(Enumerable.Reverse(A).ToList(), 1f); foreach (var q in ra.Take(ra.Count - 1)) { pts.Add(q); kind.Add(-1); deckK.Add(-1); } }
                for (int k = 0; k < chain.Count; k++) { pts.Add(P(chain[k].b, chain[k].i)); kind.Add(0); deckK.Add(k); }
                if (Z.Count > 1) { var rz = Resample(Z, 1f); foreach (var q in rz.Skip(1)) { pts.Add(q); kind.Add(1); deckK.Add(-1); } }
                int N = pts.Count; if (N < 4) continue;
                float lift = (RoadLift.TryGetValue(b0.@class, out var lf) ? lf : .06f) * vScale, half = b0.width * .5f;
                var cum = new float[N]; for (int i = 1; i < N; i++) cum[i] = cum[i - 1] + (pts[i] - pts[i - 1]).magnitude;
                float total = cum[N - 1];
                var raw = new float[N];
                for (int i = 0; i < N; i++) raw[i] = kind[i] == 0 ? map.bridges[chain[deckK[i]].b].deck_y[chain[deckK[i]].i] + lift : terrain.Height(pts[i].x, pts[i].y);
                // the line's own pieces: a road or deck segment lying along it (both its ends within 1.5 m of the line)
                var lineIdx = new CellIndex(16f); for (int i = 0; i + 1 < N; i++) lineIdx.Add(pts[i], pts[i + 1], .01f);
                var lnear = new List<int>();
                float LineDist(Vector2 p, out int seg, out float t)
                {
                    float best = float.MaxValue; seg = 0; t = 0f;
                    foreach (int i in lineIdx.Near(p, 3f, lnear)) { var ab = pts[i + 1] - pts[i]; float tt = Mathf.Clamp01(Vector2.Dot(p - pts[i], ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f)); float d = (pts[i] + ab * tt - p).magnitude; if (d < best) { best = d; seg = i; t = tt; } }
                    return best;
                }
                bool Own(int si) => LineDist(roadSegs[si].a, out _, out _) < 1.5f && LineDist(roadSegs[si].b, out _, out _) < 1.5f;
                // bounds: over every road or deck the deck crosses (under: at least R81Clear under the slab; over: this one R81Clear under the other's slab),
                // and the pins (where another road joins the line on the ground)
                var lb = Enumerable.Repeat(float.MinValue, N).ToArray(); var ub = Enumerable.Repeat(float.MaxValue, N).ToArray(); var pin = new float[N];
                var crossings = new List<(float s, float under, bool over, string what)>();
                float lo0 = float.MaxValue;
                for (int i = 0; i + 1 < N; i++)
                {
                    var a = pts[i]; var bb = pts[i + 1];
                    foreach (int si in segIdx.Near(Mathf.Min(a.x, bb.x) - 1f, Mathf.Min(a.y, bb.y) - 1f, Mathf.Max(a.x, bb.x) + 1f, Mathf.Max(a.y, bb.y) + 1f, near).ToList())
                    {
                        var s = roadSegs[si]; if (s.bridge > 0 && chain.Any(c => c.b == s.bi)) continue;
                        if (!SegX(a, bb, s.a, s.b, out float ta, out float tb)) continue;
                        if (Own(si)) continue;
                        var X = Vector2.Lerp(a, bb, ta); float sx = Mathf.Lerp(cum[i], cum[i + 1], ta);
                        bool onDeck = kind[i] == 0 && kind[i + 1] == 0;
                        float other = s.bridge > 0 ? Mathf.Lerp(map.bridges[s.bi].deck_y[s.seg], map.bridges[s.bi].deck_y[s.seg + 1], tb) + lift : terrain.Height(X.x, X.y);
                        float here = Mathf.Lerp(raw[i], raw[i + 1], ta);
                        // Round 82 (A, BUG-005: the Express Lanes' road past its deck was raised into Greers Chapel Drive's decks over it): a deck over the
                        // road part of the line caps it there too
                        if (!onDeck && s.bridge > 0 && other > here + 1f)
                        {
                            float cap = other - lift - R81Slab - R81Clear; crossings.Add((sx, other, true, map.bridges[s.bi].name));
                            for (int j = 0; j < N; j++) { float d = Mathf.Max(0f, Mathf.Abs(cum[j] - sx) - (s.half + 3f)); ub[j] = Mathf.Min(ub[j], cap + d * d / (2f * R81BoundR)); }
                            continue;
                        }
                        if (!onDeck)
                        {
                            // a road meeting the line on the ground (a junction, a merge): the line held to the road as it is there
                            if (s.bridge < 0 && Mathf.Abs(other - here) < 2f) for (int j = 0; j < N; j++) { float d = Mathf.Abs(cum[j] - sx); if (d < 40f) pin[j] = Mathf.Max(pin[j], Smooth01(1f - d / 40f)); }
                            continue;
                        }
                        float spread = s.half + 3f;
                        // Round 81 (second try, the check: the hard edges of these bounds left 1 m steps in the decks, EL13 threw a car off at one): each bound
                        // falls away from its crossing along a vertical curve (a parabola of radius R81BoundR), so following it is itself a smooth curve.
                        // A road on the ground crossing the deck, above it or below (EL13 ran into the Canton Road Connector's embankment): the deck flies over
                        // it (Dan: "the express lanes are usually bridges when they cross"); under another road's deck it keeps its clearance below
                        if (s.bridge < 0 || other < here - 1f)
                        {
                            float need = other + R81Clear + R81Slab + lift; crossings.Add((sx, other, false, s.bridge > 0 ? map.bridges[s.bi].name : "a road"));
                            lo0 = Mathf.Min(lo0, here - lift - R81Slab - other);
                            for (int j = 0; j < N; j++) { float d = Mathf.Max(0f, Mathf.Abs(cum[j] - sx) - spread); lb[j] = Mathf.Max(lb[j], need - d * d / (2f * R81BoundR)); }
                        }
                        else if (other > here + 1f)
                        {
                            float cap = other - lift - R81Slab - R81Clear; crossings.Add((sx, other, true, map.bridges[s.bi].name));
                            for (int j = 0; j < N; j++) { float d = Mathf.Max(0f, Mathf.Abs(cum[j] - sx) - spread); ub[j] = Mathf.Min(ub[j], cap + d * d / (2f * R81BoundR)); }
                        }
                    }
                }
                // a road or another deck ending on the line (a ramp's merge or split, a junction): held there too
                for (int si = 0; si < roadSegs.Count; si++)
                {
                    var sg = roadSegs[si]; if (sg.bridge > 0 && chain.Any(c => c.b == sg.bi)) continue;
                    foreach (var e in new[] { sg.a, sg.b })
                    {
                        if (lineIdx.Near(e, 2f, lnear).Count == 0) continue;
                        float d = LineDist(e, out int i, out float t); if (d > 2f || kind[i] == 0 || Own(si)) continue;
                        float sx = Mathf.Lerp(cum[i], cum[Mathf.Min(i + 1, N - 1)], t);
                        for (int j = 0; j < N; j++) { float dj = Mathf.Abs(cum[j] - sx); if (dj < 40f) pin[j] = Mathf.Max(pin[j], Smooth01(1f - dj / 40f)); }
                    }
                }
                // the line's outer ends held to the road as it is (fading in over 40 m)
                for (int j = 0; j < N; j++) pin[j] = Mathf.Max(pin[j], 1f - Smooth01(Mathf.Min(cum[j], total - cum[j]) / 40f));
                float Hold(float[] y)
                {
                    float kMax = 0f;
                    for (int i = 2; i + 2 < N; i++)
                    {
                        // over about 4 m either side: the change of grade per meter
                        int i0 = i, i1 = i; while (i0 > 0 && cum[i] - cum[i0] < 4f) i0--; while (i1 < N - 1 && cum[i1] - cum[i] < 4f) i1++;
                        float d0 = Mathf.Max(.5f, cum[i] - cum[i0]), d1 = Mathf.Max(.5f, cum[i1] - cum[i]);
                        float k = -((y[i1] - y[i]) / d1 - (y[i] - y[i0]) / d0) / ((d0 + d1) * .5f);
                        kMax = Mathf.Max(kMax, k, -k * .5f);   // a crest at k; a sag at half the weight (twice the room)
                    }
                    return kMax > 1e-6f ? Mathf.Sqrt(g / kMax) : 999f;
                }
                float[] Solve(float sigma)
                {
                    var y = (float[])raw.Clone(); var tmp = new float[N];
                    for (int it = 0; it < 60; it++)
                    {
                        for (int j = 0; j < N; j++) { if (ub[j] > lb[j]) y[j] = Mathf.Min(y[j], ub[j]); y[j] = Mathf.Max(y[j], lb[j]); }
                        for (int j = 0; j < N; j++)
                        {
                            float sw = 0f, sy = 0f;
                            for (int k = j; k >= 0 && cum[j] - cum[k] <= 3f * sigma; k--) { float d = cum[j] - cum[k]; float w = Mathf.Exp(-.5f * d * d / (sigma * sigma)); sw += w; sy += w * y[k]; }
                            for (int k = j + 1; k < N && cum[k] - cum[j] <= 3f * sigma; k++) { float d = cum[k] - cum[j]; float w = Mathf.Exp(-.5f * d * d / (sigma * sigma)); sw += w; sy += w * y[k]; }
                            tmp[j] = sy / sw;
                        }
                        for (int j = 0; j < N; j++) y[j] = raw[j] * pin[j] + tmp[j] * (1f - pin[j]);
                    }
                    for (int j = 0; j < N; j++) { if (ub[j] > lb[j]) y[j] = Mathf.Min(y[j], ub[j]); y[j] = Mathf.Max(y[j], lb[j]); }
                    return y;
                }
                float before = Hold(raw);
                float sigmaUsed = 20f; var target = Solve(sigmaUsed); float after = Hold(target);
                if (after < R81V) { var t2 = Solve(30f); float a2 = Hold(t2); if (a2 > after) { target = t2; after = a2; sigmaUsed = 30f; } }   // the tuning limit: two tries
                float moved = 0f; for (int j = 0; j < N; j++) moved = Mathf.Max(moved, Mathf.Abs(target[j] - raw[j]));
                // the decks take it
                for (int j = 0; j < N; j++) if (kind[j] == 0) map.bridges[chain[deckK[j]].b].deck_y[chain[deckK[j]].i] = target[j] - lift;
                // the roads graded to it across their width, banking back beside them (wider where the change is bigger); never onto another road's pavement;
                // under the deck the ground cut to 0.3 m under its slab
                float bankMax = 14f; int changed = 0, cut = 0;
                var lo = pts.Aggregate(new Vector2(float.MaxValue, float.MaxValue), (a, q) => Vector2.Min(a, q)) - Vector2.one * (half + bankMax);
                var hi = pts.Aggregate(new Vector2(float.MinValue, float.MinValue), (a, q) => Vector2.Max(a, q)) + Vector2.one * (half + bankMax);
                var gnear = new List<int>();
                bool OnOtherRoad(Vector2 p)
                {
                    foreach (int si in segIdx.Near(p, 0f, gnear))
                    {
                        var s = roadSegs[si]; if (s.bridge > 0) continue;
                        var ab = s.b - s.a; float t = Mathf.Clamp01(Vector2.Dot(p - s.a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
                        if ((s.a + ab * t - p).magnitude < s.half + .3f && !Own(si)) return true;
                    }
                    return false;
                }
                var ownSet = new HashSet<int>(chain.Select(c => c.b));
                EditTerrain(terrain, Rect.MinMaxRect(lo.x, lo.y, hi.x, hi.y), (x, z, y) =>
                {
                    var p = new Vector2(x, z); float dd = LineDist(p, out int i, out float t);
                    if (dd > half + bankMax) return y;
                    bool deck = kind[i] == 0 && kind[Mathf.Min(i + 1, N - 1)] == 0;
                    float tg = Mathf.Lerp(target[i], target[Mathf.Min(i + 1, N - 1)], t) - lift;
                    if (deck)
                    {
                        if (dd > half + 1f) return y;
                        float top = tg - R81Slab - .3f; if (y > top) { cut++; return top; }
                        return y;
                    }
                    float rise = Mathf.Abs(tg - y), bank = Mathf.Clamp(2f * rise, 4f, bankMax);
                    if (dd > half + .5f + bank) return y;
                    if (dd > half + .5f && OnOtherRoad(p)) return y;
                    if (UnderOtherDeck(p, ownSet, out float slabB) && slabB > tg + 1f) return Mathf.Min(y, slabB - .3f);   // Round 82: never up under another deck
                    float w = dd <= half + .5f ? 1f : Smooth01(1f - (dd - half - .5f) / bank); float ny = y + w * (tg - y);
                    if (Mathf.Abs(ny - y) > 1e-3f) changed++; return ny;
                });
                // the abutments' records (SmoothBridgeEnds): the decks' ends as they now stand
                foreach (var end in BridgeEnds)
                {
                    if (!chain.Any(c => c.b == end.bridge)) continue; var bb = map.bridges[end.bridge]; int n = bb.deck_y.Length;
                    float bl = (RoadLift.TryGetValue(bb.@class, out var l2) ? l2 : .06f) * vScale;
                    end.deckTop = bb.deck_y[end.start ? 0 : n - 1] + bl; end.slabBottom = end.deckTop - R81Slab; end.ground = terrain.Height(end.E.x, end.E.y);
                }
                float lo1 = float.MaxValue;
                foreach (var cx in crossings.Where(c => !c.over)) { int j = 0; while (j < N - 1 && cum[j] < cx.s) j++; lo1 = Mathf.Min(lo1, target[j] - lift - R81Slab - cx.under); }
                done++;
                log.Add($"{label}: {cum.Where((c, k) => kind[k] == 0).DefaultIfEmpty(0f).Max() - cum.Where((c, k) => kind[k] == 0).DefaultIfEmpty(0f).Min():0} m of deck, the road {cum.Where((c, k) => kind[k] == 0).DefaultIfEmpty(0f).Min():0} m before and " +
                        $"{total - cum.Where((c, k) => kind[k] == 0).DefaultIfEmpty(0f).Max():0} m after; held to {before:0} -> {after:0} m/s (sigma {sigmaUsed:0} m); clearance over the roads under it " +
                        $"{(crossings.Any(c => !c.over) ? $"{lo0:0.0} -> {lo1:0.0} m ({crossings.Count(c => !c.over)} crossings)" : "none crossed")}{(crossings.Any(c => c.over) ? $", under {crossings.Count(c => c.over)} deck(s)" : "")}; " +
                        $"moved up to {moved:0.0} m; {changed} road samples graded, {cut} cut under the deck");
            }
            Debug.Log($"[Round81] A / H: {done} of {chains.Count} deck chains laid with their roads as one curve; report Assets/Generated/round81_flyovers.txt\n" + string.Join("\n", log));
            try { File.WriteAllLines(Path.Combine(ProjectRoot, GeneratedDir, report), log); } catch (System.Exception e) { Debug.LogWarning("[Round81] " + e.Message); }
        }

        // ------------------------------------------------------------ H: the Express Lanes walled off
        // Round 81 (H, Dan, 01:38: "they generally have walls as they are seperated from the main roads"): a concrete barrier wall along both sides of
        // every Express Lanes road on the ground (the decks have their parapets), 0.6 m outside its pavement, solid; none where another road's pavement
        // reaches the wall's line (the ramps on and off, so their gated ramps are the only way in or out, and a road crossing at grade), none under or on a
        // deck at the road's own height. Game-independent world piece (built in the Edges step with the gates).
        static void BuildExpressWalls(MapData map, TerrainGrid terrain, Materials mats, Transform world)
        {
            var segs = new List<(Vector2 a, Vector2 b, float half, bool express, float[] deck, int i, bool link)>();
            foreach (var r in map.roads) { var q = ToPoints(r.points); for (int i = 0; i + 1 < q.Count; i++) segs.Add((q[i], q[i + 1], r.width * .5f, r.name == ExpressName, null, i, (r.@class ?? "").EndsWith("_link"))); }
            if (map.bridges != null) foreach (var b in map.bridges) { var q = ToPoints(b.points); for (int i = 0; i + 1 < q.Count; i++) segs.Add((q[i], q[i + 1], b.width * .5f, b.name == ExpressName, b.deck_y, i, false)); }
            var idx = new CellIndex(24f); foreach (var s in segs) idx.Add(s.a, s.b, s.half + 1f);
            var near = new List<int>();
            var mergeLinks = new HashSet<(Vector2, Vector2)>();   // Round 83: the segments of ramps that merge into an Express Lanes road (an end on it)
            // a wall's point blocked: on another road's pavement (+0.4 m), or on a deck at about the road's height (its parapet is the wall there)
            bool Blocked(Vector2 p, float roadY, Vector2 along, float dEnd = 999f)
            {
                foreach (int si in idx.Near(p, 0f, near))
                {
                    var s = segs[si]; var ab = s.b - s.a; float t = Mathf.Clamp01(Vector2.Dot(p - s.a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
                    float d = (s.a + ab * t - p).magnitude;
                    // (Round 83 tried keeping the wall beside other decks off the road's height in build 2: new scrapes at I-575's seam deck; back to Round 82's rule)
                    if (s.deck != null) { if (d < s.half + 1.2f && s.i + 1 < s.deck.Length && Mathf.Abs(Mathf.Lerp(s.deck[s.i], s.deck[s.i + 1], t) - roadY) < 3f) return true; continue; }
                    // (a road running alongside, the general lanes: the wall stands between them, not left out; a ramp or a road at grade: left out)
                    if (!s.express && d < s.half + .4f && (s.link || Mathf.Abs(Vector2.Dot(ab.normalized, along)) < .9f)) return true;
                    // Round 83 (A, build 1: 49 hits on a wall standing in the narrowing gap where the I-575 flyover's road meets the I-75 Express Lanes): no
                    // wall within 5 m of a ramp's pavement at about the road's height (the merge stays open)
                    if (!s.express && s.link && d < s.half + 5f && (mergeLinks.Contains((s.a, s.b)) || dEnd > 15f)) { var on = s.a + ab * t; if (Mathf.Abs(Ground(terrain, on.x, on.y) - roadY) < 2f) return true; }   // (final build: opened the walls before two I-575 deck starts, cars hit the parapets' ends: not within 30 m of an Express deck's end)
                }
                return false;
            }
            // Round 82 (C, the Big Shanty ramp's check: 251 wall hits where it merges): every point where another road or deck ends on an Express Lanes road
            var merges = new List<Vector2>();
            foreach (var s in segs.Where(x => !x.express))
                foreach (var e in new[] { s.a, s.b })
                    foreach (int si in idx.Near(e, 2f, near).ToList()) { var x = segs[si]; if (!x.express) continue; var ab = x.b - x.a; float t = Mathf.Clamp01(Vector2.Dot(e - x.a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f)); if ((x.a + ab * t - e).magnitude < 2f) { merges.Add(e); break; } }
            foreach (var r in map.roads)
            {
                if (r.name == ExpressName || !(r.@class ?? "").EndsWith("_link")) continue; var q = ToPoints(r.points); if (q.Count < 2) continue;
                bool m = false; foreach (var e in new[] { q[0], q[q.Count - 1] }) foreach (int si in idx.Near(e, 2f, near)) { var x = segs[si]; if (!x.express) continue; var ab = x.b - x.a; float t = Mathf.Clamp01(Vector2.Dot(e - x.a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f)); if ((x.a + ab * t - e).magnitude < 2f) m = true; }
                if (m) for (int i = 0; i + 1 < q.Count; i++) mergeLinks.Add((q[i], q[i + 1]));
            }
            // Round 83 (D, Dan's I-575 seam deck (536, -37), with I-575's general lanes' bridges right beside it at the old map's edge: cars fell off its ends
            // whenever walls stood there, and drove through when they did not): no wall within 40 m of that deck
            var seamDeck = new List<Vector2>();
            if (map.bridges != null) foreach (var bb in map.bridges) if (bb.osm_id == 564154403) seamDeck.AddRange(ToPoints(bb.points));
            var deckEnds = new List<Vector2>();
            if (map.bridges != null) foreach (var bb in map.bridges) if (bb.name == ExpressName) { var (ea, ez) = Ends(bb, false); deckEnds.Add(ea); deckEnds.Add(ez); }
            var root = new GameObject("Express Lanes walls"); root.transform.SetParent(world, false);
            var grey = new Color(.74f, .73f, .70f);
            // (second try, the check: a car that touched the wall's flared foot rode up it and over; the wall stood 0.6 m off a 4.5 m lane): the wall
            // 1.8 m out (a shoulder; less where a road alongside leaves no room), and solid as a plain upright slab 1.5 m high (no foot to climb)
            var mb = new MeshBuilder(); var cmb = new MeshBuilder(); int chunk = 0; float built = 0f, skipped = 0f;
            void Flush()
            {
                if (mb.Count == 0) return;
                mb.ToObject($"Express Lanes wall {chunk}", mats.House, root.transform, false);
                var col = cmb.ToObject($"Express Lanes wall collider {chunk}", mats.House, root.transform, true); var r = col.GetComponent<MeshRenderer>(); if (r) r.enabled = false;
                chunk++; mb = new MeshBuilder(); cmb = new MeshBuilder();
            }
            bool Alongside(Vector2 p, Vector2 along)
            {
                foreach (int si in idx.Near(p, 0f, near))
                {
                    var s = segs[si]; if (s.express || s.deck != null) continue; var ab = s.b - s.a; float t = Mathf.Clamp01(Vector2.Dot(p - s.a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
                    if ((s.a + ab * t - p).magnitude < s.half + .2f) return true;
                }
                return false;
            }
            foreach (var r in map.roads)
            {
                if (r.name != ExpressName || r.bridge) continue;
                var line = Resample(ToPoints(r.points), 3f); if (line.Count < 2) continue;
                float half = r.width * .5f;
                foreach (float side in new[] { -1f, 1f })
                {
                    Vector3? prev = null; Vector3 prevFoot = default;
                    for (int i = 0; i < line.Count; i++)
                    {
                        var dir = (line[Mathf.Min(i + 1, line.Count - 1)] - line[Mathf.Max(i - 1, 0)]).normalized; var right = new Vector2(dir.y, -dir.x);
                        float off = half + .5f; for (float o = half + 3.5f; o >= half + .5f; o -= .3f)   /* Round 82: 2.5 m out (was 1.8); Round 83 (D: 37 scrapes on I-575's at (754, -1385), the car 4 m off the lane's middle): 3.5 m */ if (!Alongside(line[i] + right * (side * o), dir)) { off = o; break; }
                        // Round 82 (B, the drive: cars caught the parapets' ends at the decks' starts): over the 25 m before an Express deck's end the wall closes in
                        // to the parapet's line, leading onto the deck
                        float dEnd = deckEnds.Count > 0 ? deckEnds.Min(e => (e - line[i]).magnitude) : 999f;
                        off = Mathf.Lerp(half + .35f, off, Mathf.Clamp01((dEnd - 2f) / 25f));
                        var p = line[i] + right * (side * off); float roadY = Ground(terrain, line[i].x, line[i].y);
                        if (seamDeck.Any(q => (q - line[i]).magnitude < 40f) || Blocked(p, roadY, dir, dEnd) || merges.Any(m => (m - line[i]).magnitude < 35f)) { prev = null; skipped += 3f; continue; }   // Round 82 (C): open round every merge
                        float footY = Ground(terrain, p.x, p.y);
                        var top = new Vector3(p.x, roadY + .04f, p.y);   // Round 82 (BUG-005: walls climbing over the banks): at the road's height, its foot buried where the bank stands higher
                        if (prev.HasValue)
                        {
                            var a = prev.Value; var b = top; var across = new Vector3(right.x, 0f, right.y) * side;   // toward the outside
                            // the jersey section: a flared foot and an upright, its foot run down to the ground (a skirt where the bank falls away)
                            Beam(mb, a + Vector3.up * .16f, b + Vector3.up * .16f, .6f, .36f, grey);
                            Beam(mb, a + Vector3.up * .55f, b + Vector3.up * .55f, .26f, .52f, grey);
                            Beam(cmb, a + Vector3.up * .45f, b + Vector3.up * .45f, .3f, 1.5f, grey, across);   // the solid slab
                            float lowA = Mathf.Min(prevFoot.y, a.y) - .25f, lowB = Mathf.Min(footY, b.y) - .25f;
                            if (a.y - lowA > .3f || b.y - lowB > .3f)
                            {
                                mb.Tint = grey;
                                Face(mb, new Vector3(a.x, lowA, a.z) + across * .3f, new Vector3(b.x, lowB, b.z) + across * .3f, b + across * .3f, a + across * .3f, across);
                            }
                            built += (b - a).magnitude;
                        }
                        prev = top; prevFoot = new Vector3(p.x, footY, p.y);
                    }
                    if (mb.Count > 60000) Flush();
                }
            }
            Flush();
            Debug.Log($"[Round81] H: the Express Lanes walled off: {built / 1000f:0.0} km of wall in {chunk} pieces, {skipped / 1000f:0.0} km left open (ramps, roads at grade, decks with their parapets)");
        }

        // two segments crossing: the parameters along each
        static bool SegX(Vector2 a, Vector2 b, Vector2 c, Vector2 d, out float ta, out float tb)
        {
            var r = b - a; var s = d - c; float den = r.x * s.y - r.y * s.x; ta = tb = 0f;
            if (Mathf.Abs(den) < 1e-9f) return false;
            var ca = c - a; ta = (ca.x * s.y - ca.y * s.x) / den; tb = (ca.x * r.y - ca.y * r.x) / den;
            return ta > 0f && ta < 1f && tb > 0f && tb < 1f;
        }
    }
}
