using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Round 82 (Dan, 2026-10-29 09:40, session 2026-10-29_09-12-54-968_3aae2d).
// B, BUG-006 / BUG-007 and what Round 81 left (EL13 in the air, the Big Shanty deck, the merge wall): R82ExpressLines lays each Northwest Corridor Express
// Lanes carriageway (I-575's, I-75's: OSM has one reversible way each, so one line each) as ONE height profile end to end, in place of Round 81's deck by
// deck pass: the ground along it smoothed (a Gaussian of 25 m), lifted over every road it crosses (OSM's layers: Data/tracks/crossings.json from
// python Tools/mapgen/crossings.py) to 5.5 m under its slab, kept under every deck crossing over it (5.5 m) but never dug more than 1 m into its own ground
// for one (that deck is raised instead: A). Each bound falls away along a vertical curve of R82R m and then at most R82Grade, and joins the smoothed ground
// by a smooth maximum (no kink), so a car at 35 m/s stays down; the line's two outer ends eased back onto the road as it is. Its decks take the profile,
// its road is graded to it across its width with banks, never onto another road's pavement and never up under another deck (Round 81's banks came up
// through I-75's decks at Bells Ferry Road: BUG-006's torn ground), the ground under its decks cut to under their slabs.
// C (BUG-002 / BUG-003, the Big Shanty Express ramp merging at Noonday Creek): every road that joins a line (a ramp: it shares the line's node) is laid to the
// line: at the line's height while it runs beside it, then eased to its own height over D m (4 % at most); its decks take that too, so the ramp's bridge runs
// beside the Express Lanes' bridge at the same height into the merge (no slot under a lifted deck), the ground between them cut clear of the slabs.
// A (BUG-005, "fix these everywhere"): R82Crossings lists every crossing of crossings.json with its clearance at OSM's heights and as built here; every one
// under 5 m has its upper road's deck chain laid again with its roads (Round 81's chain pass, now capped by decks over its approaches too) and is listed
// again. Report: Assets/Generated/round82_crossings.txt; the lines: round82_express.txt.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        const float R82Clear = 5.5f, R82Slab = .7f, R82R = 300f, R82Grade = .045f, R82Sigma = 25f, R82EndEase = 80f, R82MinClear = 5f;

        [System.Serializable] class R82CrossRef { public string kind; public int index; public long osm_id; public string name; public string @class; public int seg; public float t; }
        [System.Serializable] class R82Cross { public float x, z; public string why; public R82CrossRef over, under; }
        [System.Serializable] class R82CrossFile { public R82Cross[] crossings; }

        static MapData r82Map; static CellIndex r82DeckIdx; static List<(int b, int i)> r82DeckSegs; static readonly List<int> r82Near = new List<int>();
        static R82CrossFile r82Cross;
        static readonly List<string> r82CrossBefore = new List<string>();

        static void R82WidenExpress(MapData map)
        {
            foreach (var r in map.roads) if (r.name == ExpressName) r.width = Mathf.Max(r.width, 6f);
            if (map.bridges != null) foreach (var b in map.bridges) if (b.name == ExpressName) b.width = Mathf.Max(b.width, 6f);
        }

        static float R82Lift(string cls) => (RoadLift.TryGetValue(cls ?? "", out var l) ? l : .06f) * vScale;

        static void R82DeckIndex(MapData map)
        {
            r82Map = map; r82RoadIdx = null; r83ExIdx = null; r83RoadIdx = null; r82DeckIdx = new CellIndex(16f); r82DeckSegs = new List<(int, int)>();
            if (map.bridges == null) return;
            for (int bi = 0; bi < map.bridges.Length; bi++)
            {
                var q = ToPoints(map.bridges[bi].points);
                for (int i = 0; i + 1 < q.Count; i++) { r82DeckSegs.Add((bi, i)); r82DeckIdx.Add(q[i], q[i + 1], map.bridges[bi].width * .5f + 1.5f); }
            }
        }

        // a point under another deck (within its half width + 1.5 m): that deck's slab bottom there (the lowest if several)
        static bool UnderOtherDeck(Vector2 p, HashSet<int> exclude, out float slabBottom)
        {
            slabBottom = float.MaxValue; if (r82DeckIdx == null) return false;
            foreach (int k in r82DeckIdx.Near(p, 0f, r82Near))
            {
                var (bi, i) = r82DeckSegs[k]; if (exclude != null && exclude.Contains(bi)) continue;
                var b = r82Map.bridges[bi]; var a = new Vector2(b.points[2 * i], b.points[2 * i + 1]); var c = new Vector2(b.points[2 * i + 2], b.points[2 * i + 3]);
                var ab = c - a; float t0 = Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f), t = Mathf.Clamp01(t0);
                // Round 83 (the dry run: Canton Road's ground cut 1.2 m just before its deck over the Express Lanes): past a deck's two ends is not under it
                if ((i == 0 && t0 < 0f) || (i + 2 >= b.points.Length / 2 && t0 > 1f)) continue;
                if ((a + ab * t - p).magnitude > b.width * .5f + 1.5f || b.deck_y == null || i + 1 >= b.deck_y.Length) continue;
                slabBottom = Mathf.Min(slabBottom, Mathf.Lerp(b.deck_y[i], b.deck_y[i + 1], t) - R82Slab);   // (deck_y: the top less the road's lift; the slab under its top)
            }
            return slabBottom < float.MaxValue;
        }

        // Round 82 (C): beside a deck's edge (outward), within 4 m: another deck's or a road's pavement at its height (within 0.6 m): how far out it starts
        // (-1: none, the parapet stands)
        static CellIndex r82RoadIdx; static List<(Vector2 a, Vector2 b, float half, string cls)> r82RoadSegs; static readonly List<int> r82RoadNear = new List<int>();
        static float R82SideOpen(int self, Vector2 mid, Vector2 outward, float hw, float topY, TerrainGrid terrain)
        {
            if (r82DeckIdx == null || r82Map == null) return -1f;
            if (r82RoadIdx == null)
            {
                r82RoadIdx = new CellIndex(16f); r82RoadSegs = new List<(Vector2, Vector2, float, string)>();
                foreach (var r in r82Map.roads) { if (r.bridge || r.tunnel) continue; var q = ToPoints(r.points); for (int i = 0; i + 1 < q.Count; i++) { r82RoadSegs.Add((q[i], q[i + 1], r.width * .5f, r.@class)); r82RoadIdx.Add(q[i], q[i + 1], r.width * .5f + .5f); } }
            }
            for (float o = .1f; o <= 4f; o += .3f)
            {
                var p = mid + outward * (hw + o);
                foreach (int k in r82DeckIdx.Near(p, 0f, r82Near))
                {
                    var (bi, i) = r82DeckSegs[k]; if (bi == self) continue; var b = r82Map.bridges[bi];
                    var a = new Vector2(b.points[2 * i], b.points[2 * i + 1]); var c = new Vector2(b.points[2 * i + 2], b.points[2 * i + 3]); var ab = c - a;
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
                    if ((a + ab * t - p).magnitude > b.width * .5f) continue;
                    if (Mathf.Abs(Mathf.Lerp(b.deck_y[i], b.deck_y[i + 1], t) + R82Lift(b.@class) - topY) < .6f) return o - .1f;
                }
                foreach (int k in r82RoadIdx.Near(p, 0f, r82RoadNear))
                {
                    var s = r82RoadSegs[k]; var ab = s.b - s.a; float t = Mathf.Clamp01(Vector2.Dot(p - s.a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
                    if ((s.a + ab * t - p).magnitude > s.half) continue;
                    if (Mathf.Abs(terrain.Height(p.x, p.y) + R82Lift(s.cls) - topY) < .6f) return o - .1f;
                }
            }
            return -1f;
        }

        // Round 83: the top of a deck (not one of 'exclude') under a point, within its half width + 1 m; the nearest to 'near' if several
        static bool R83DeckTopAt(MapData map, Vector2 p, Vector2 dir, HashSet<int> exclude, float near, out float top)
        {
            top = float.NaN; float best = float.MaxValue; if (r82DeckIdx == null) return false;
            foreach (int k in r82DeckIdx.Near(p, 0f, r82Near))
            {
                var (bi, i) = r82DeckSegs[k]; if (exclude != null && exclude.Contains(bi)) continue;
                var b = map.bridges[bi]; if (b.deck_y == null || i + 1 >= b.deck_y.Length) continue;
                var a = new Vector2(b.points[2 * i], b.points[2 * i + 1]); var c = new Vector2(b.points[2 * i + 2], b.points[2 * i + 3]); var ab = c - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
                if ((a + ab * t - p).magnitude > b.width * .5f + 1f) continue;
                float y = Mathf.Lerp(b.deck_y[i], b.deck_y[i + 1], t) + R82Lift(b.@class);
                if (Mathf.Abs(y - near) < best) { best = Mathf.Abs(y - near); top = y; }
            }
            return !float.IsNaN(top);
        }

        // Round 83: a point on an Express Lanes road's pavement (on the ground; within its half width + 0.5 m)
        static CellIndex r83ExIdx; static List<(Vector2 a, Vector2 b, float half)> r83ExSegs; static readonly List<int> r83ExNear = new List<int>();
        static bool R83OnExpress(Vector2 p)
        {
            if (r83ExIdx == null)
            {
                r83ExIdx = new CellIndex(16f); r83ExSegs = new List<(Vector2, Vector2, float)>();
                foreach (var r in r82Map.roads) { if (r.name != ExpressName || r.bridge) continue; var q = ToPoints(r.points); for (int i = 0; i + 1 < q.Count; i++) { r83ExSegs.Add((q[i], q[i + 1], r.width * .5f)); r83ExIdx.Add(q[i], q[i + 1], r.width * .5f + .5f); } }
            }
            foreach (int k in r83ExIdx.Near(p, 0f, r83ExNear))
            {
                var s = r83ExSegs[k]; var ab = s.b - s.a; float t = Mathf.Clamp01(Vector2.Dot(p - s.a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
                if ((s.a + ab * t - p).magnitude < s.half + .5f) return true;
            }
            return false;
        }

        // Round 83: a point on the pavement of a road on the ground (within its half width + 0.3 m) not in 'exclude'
        static bool R83OnOtherRoad(Vector2 p, HashSet<int> exclude)
        {
            if (r83RoadIdx == null)
            {
                r83RoadIdx = new CellIndex(16f); r83RoadSegs = new List<(Vector2, Vector2, float, int)>();
                for (int ri = 0; ri < r82Map.roads.Length; ri++) { var r = r82Map.roads[ri]; if (r.bridge || r.tunnel) continue; var q = ToPoints(r.points); for (int i = 0; i + 1 < q.Count; i++) { r83RoadSegs.Add((q[i], q[i + 1], r.width * .5f, ri)); r83RoadIdx.Add(q[i], q[i + 1], r.width * .5f + .3f); } }
            }
            foreach (int k in r83RoadIdx.Near(p, 0f, r83RoadNear))
            {
                var s = r83RoadSegs[k]; if (exclude != null && exclude.Contains(s.road)) continue;
                var ab = s.b - s.a; float t = Mathf.Clamp01(Vector2.Dot(p - s.a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
                if ((s.a + ab * t - p).magnitude < s.half + .3f) return true;
            }
            return false;
        }
        static CellIndex r83RoadIdx; static List<(Vector2 a, Vector2 b, float half, int road)> r83RoadSegs; static readonly List<int> r83RoadNear = new List<int>();

        static R82CrossFile R82LoadCrossings()
        {
            var path = Path.Combine(ProjectRoot, "Data/tracks/crossings.json");
            if (!File.Exists(path)) { Debug.LogWarning("[Round82] Data/tracks/crossings.json missing: run python Tools/mapgen/crossings.py"); return new R82CrossFile { crossings = new R82Cross[0] }; }
            return JsonUtility.FromJson<R82CrossFile>(File.ReadAllText(path));
        }

        // a crossing piece's surface at a point (a deck's top, or the ground + the road's lift), its half width and direction there
        static float R82TopOf(MapData map, TerrainGrid terrain, R82CrossRef r, Vector2 X, out float half, out Vector2 dir)
        {
            float[] pts = r.kind == "bridge" ? map.bridges[r.index].points : map.roads[r.index].points;
            string cls = r.kind == "bridge" ? map.bridges[r.index].@class : map.roads[r.index].@class;
            half = (r.kind == "bridge" ? map.bridges[r.index].width : map.roads[r.index].width) * .5f;
            int n = pts.Length / 2, best = 0; float bd = float.MaxValue, bt = 0f;
            for (int i = 0; i + 1 < n; i++)
            {
                var a = new Vector2(pts[2 * i], pts[2 * i + 1]); var c = new Vector2(pts[2 * i + 2], pts[2 * i + 3]); var ab = c - a;
                float t = Mathf.Clamp01(Vector2.Dot(X - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f)); float d = (a + ab * t - X).sqrMagnitude;
                if (d < bd) { bd = d; best = i; bt = t; }
            }
            dir = (new Vector2(pts[2 * best + 2], pts[2 * best + 3]) - new Vector2(pts[2 * best], pts[2 * best + 1])).normalized;
            if (r.kind == "bridge") { var dy = map.bridges[r.index].deck_y; return Mathf.Lerp(dy[best], dy[Mathf.Min(best + 1, dy.Length - 1)], bt) + R82Lift(cls); }
            return terrain.Height(X.x, X.y) + R82Lift(cls);
        }

        // the clearance at each crossing now (the upper deck's slab bottom over the lower road's surface)
        static List<(R82Cross c, float clear)> R82Clearances(MapData map, TerrainGrid terrain)
        {
            var o = new List<(R82Cross, float)>();
            foreach (var c in r82Cross.crossings)
            {
                var X = new Vector2(c.x, c.z);
                float top = R82TopOf(map, terrain, c.over, X, out _, out _), under = R82TopOf(map, terrain, c.under, X, out _, out _);
                o.Add((c, top - R82Lift(c.over.@class) - R82Slab - under));
            }
            return o;
        }

        // ------------------------------------------------------------ B / C: the Express Lanes end to end
        sealed class R82P { public Vector2 p; public int bridge = -1, bi; public float raw; public List<(int b, int i)> alias; public bool skip; }   // alias: deck points at the same place (piece joins), given the same height

        static void R82ExpressLines(MapData map, TerrainGrid terrain)
        {
            R82DeckIndex(map); r82Cross = R82LoadCrossings();
            r82CrossBefore.Clear(); foreach (var (c, cl) in R82Clearances(map, terrain)) r82CrossBefore.Add($"{cl:0.0}");
            var log = new List<string> { "Round 82 (B, C): the Express Lanes laid end to end as one profile per carriageway (MapBuilder.Round82.cs). Per line: its length, " +
                                         "the speed its sharpest crest is held at, the steepest grade, the crossings over and under it, how far it moved; then each road joined to it" };
            // the pieces: Express roads on the ground and Express decks
            var pieces = new List<(bool deck, int idx, List<Vector2> q)>();
            for (int i = 0; i < map.roads.Length; i++) if (map.roads[i].name == ExpressName && !map.roads[i].bridge) pieces.Add((false, i, ToPoints(map.roads[i].points)));
            for (int i = 0; i < map.bridges.Length; i++) if (map.bridges[i].name == ExpressName && map.bridges[i].deck_y != null) pieces.Add((true, i, ToPoints(map.bridges[i].points)));
            var used = new bool[pieces.Count]; var lines = new List<List<R82P>>();
            int Touch(Vector2 e, int self) { for (int k = 0; k < pieces.Count; k++) if (k != self && ((pieces[k].q[0] - e).magnitude < 2.5f || (pieces[k].q[pieces[k].q.Count - 1] - e).magnitude < 2.5f)) return k; return -1; }
            for (int guard = 0; guard < 200; guard++)
            {
                // start at a free piece with an end touching no other piece (else any free piece)
                int st = -1; bool fromStart = true;
                for (int k = 0; k < pieces.Count && st < 0; k++) if (!used[k]) { var q = pieces[k].q; if (Touch(q[0], k) < 0 || used[Touch(q[0], k)]) { st = k; fromStart = true; } else if (Touch(q[q.Count - 1], k) < 0 || used[Touch(q[q.Count - 1], k)]) { st = k; fromStart = false; } }
                for (int k = 0; k < pieces.Count && st < 0; k++) if (!used[k]) st = k;
                if (st < 0) break;
                var line = new List<R82P>(); int cur = st; bool fwd = fromStart;
                while (cur >= 0 && !used[cur])
                {
                    used[cur] = true; var pc = pieces[cur]; var q = fwd ? pc.q : Enumerable.Reverse(pc.q).ToList();
                    if (pc.deck)
                    {
                        int n = q.Count;
                        for (int k = 0; k < n; k++) { int ii = fwd ? k : n - 1 - k; line.Add(new R82P { p = q[k], bridge = pc.idx, bi = ii }); }
                    }
                    else foreach (var v in Resample(q, 2f)) line.Add(new R82P { p = v });
                    var tail = q[q.Count - 1]; int nx = -1; bool nfwd = true;
                    for (int k = 0; k < pieces.Count && nx < 0; k++)
                    {
                        if (used[k]) continue; var qk = pieces[k].q;
                        if ((qk[0] - tail).magnitude < 2.5f) { nx = k; nfwd = true; } else if ((qk[qk.Count - 1] - tail).magnitude < 2.5f) { nx = k; nfwd = false; }
                    }
                    cur = nx; fwd = nfwd;
                }
                // drop points within 0.5 m of the one before (piece joins), keeping a deck's point over a road's
                var clean = new List<R82P>();
                foreach (var v in line) { if (clean.Count > 0 && (clean[clean.Count - 1].p - v.p).magnitude < .5f) { var pv = clean[clean.Count - 1]; if (v.bridge >= 0) { if (pv.bridge >= 0) { v.alias = v.alias ?? new List<(int, int)>(); v.alias.Add((pv.bridge, pv.bi)); if (pv.alias != null) v.alias.AddRange(pv.alias); } clean[clean.Count - 1] = v; } continue; } clean.Add(v); }
                if (clean.Count >= 4) lines.Add(clean);
            }
            var lineBridges = new HashSet<int>(pieces.Where(p => p.deck).Select(p => p.idx));
            var lineRoads = new HashSet<int>(pieces.Where(p => !p.deck).Select(p => p.idx));
            float liftL = R82Lift("motorway"), halfL = pieces.Count > 0 ? (pieces[0].deck ? map.bridges[pieces[0].idx].width : map.roads[pieces[0].idx].width) * .5f : 2.25f;
            var graded = new List<(List<Vector2> p, float[] y)>();   // every line, for the joins and the gap fill
            // (A: the lidar's ground under a deck over the lanes is the fill between its abutments, not the road's level): taken across
            var lineSetB = new HashSet<int>(lineBridges);
            foreach (var L in lines) foreach (var v in L) { if (v.bridge >= 0) continue; for (int dx = -1; dx <= 1 && !v.skip; dx++) for (int dz = -1; dz <= 1 && !v.skip; dz++) if (UnderOtherDeck(v.p + new Vector2(dx, dz) * 12f, lineSetB, out float sb) && sb > terrain.Height(v.p.x, v.p.y) - 2f) v.skip = true; }
            R82Lay(map, terrain, lines, lineBridges, lineRoads, liftL, halfL, R82Grade, log, graded, "line");
            BuildClock.Lap("R82ExpressLines: the lines");
            // ---- C: every road joined to a line (it ends on it): laid to the line beside it, then eased to its own height
            R82Joins(map, terrain, graded, lineRoads, lineBridges, halfL, liftL, log);
            Debug.Log("[Round82] B / C: the Express Lanes end to end\n" + string.Join("\n", log));
            try { File.WriteAllLines(Path.Combine(ProjectRoot, GeneratedDir, "round82_express.txt"), log); } catch (System.Exception e) { Debug.LogWarning("[Round82] " + e.Message); }
        }

        // one profile per line (the decks take it, the road graded across its width with banks), as the Express Lanes' (B) and A's raised roads
        static void R82Lay(MapData map, TerrainGrid terrain, List<List<R82P>> lines, HashSet<int> lineBridges, HashSet<int> lineRoads, float liftL, float halfL, float grade,
                           List<string> log, List<(List<Vector2> p, float[] y)> graded, string tag)
        {
            var segIdxAll = new CellIndex(24f); var segAll = new List<(Vector2 a, Vector2 b, float half, int road)>();
            for (int ri = 0; ri < map.roads.Length; ri++) { var q = ToPoints(map.roads[ri].points); for (int i = 0; i + 1 < q.Count; i++) { segAll.Add((q[i], q[i + 1], map.roads[ri].width * .5f, ri)); segIdxAll.Add(q[i], q[i + 1], map.roads[ri].width * .5f + 1f); } }
            var tmpNear = new List<int>();
            int li = 0;
            foreach (var L in lines)
            {
                int N = L.Count; var cum = new float[N]; for (int i = 1; i < N; i++) cum[i] = cum[i - 1] + (L[i].p - L[i - 1].p).magnitude;
                for (int i = 0; i < N; i++) L[i].raw = L[i].bridge >= 0 ? map.bridges[L[i].bridge].deck_y[L[i].bi] + liftL : terrain.Height(L[i].p.x, L[i].p.y);
                // Round 83 (C: ramp 437897716 ends merging onto the Canton Road Connector's bridge beside it, held to its own height 10 m under that deck: 34 %):
                // an end lying on another deck (a merge) is held to that deck's top
                foreach (int e in new[] { 0, N - 1 })
                {
                    var dirE = (e == 0 ? L[1].p - L[0].p : L[N - 1].p - L[N - 2].p).normalized;
                    if (!R83DeckTopAt(map, L[e].p, dirE, lineBridges, L[e].raw, out float top)) continue;
                    log.Add($"  {tag}: its end at ({L[e].p.x:0}, {L[e].p.y:0}) merges onto a deck: held to its top {top:0.0} (was {L[e].raw:0.0})");
                    L[e].raw = top;
                }
                var lineIdx = new CellIndex(16f); for (int i = 0; i + 1 < N; i++) lineIdx.Add(L[i].p, L[i + 1].p, .01f);
                var lnear = new List<int>();
                float LineDist(Vector2 p, float reach, out int seg, out float t)
                {
                    float best = float.MaxValue; seg = 0; t = 0f;
                    foreach (int i in lineIdx.Near(p, reach, lnear)) { var ab = L[i + 1].p - L[i].p; float tt = Mathf.Clamp01(Vector2.Dot(p - L[i].p, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f)); float d = (L[i].p + ab * tt - p).magnitude; if (d < best) { best = d; seg = i; t = tt; } }
                    return best;
                }
                var raw = L.Select(v => (double)v.raw).ToArray();
                // the ground smoothed along
                double[] Gauss(double[] y, float sigma)
                {
                    var o = new double[N];
                    for (int j = 0; j < N; j++)
                    {
                        double sw = 0, sy = 0;
                        for (int k = j; k >= 0 && cum[j] - cum[k] <= 3f * sigma; k--) { double d = cum[j] - cum[k]; double w = System.Math.Exp(-.5 * d * d / (sigma * sigma)); sw += w; sy += w * y[k]; }
                        for (int k = j + 1; k < N && cum[k] - cum[j] <= 3f * sigma; k++) { double d = cum[k] - cum[j]; double w = System.Math.Exp(-.5 * d * d / (sigma * sigma)); sw += w; sy += w * y[k]; }
                        o[j] = sy / sw;
                    }
                    return o;
                }
                // (build 2: the decks' own heights from OSM stood metres off the ground at their ends, and the smoothing made that step a 10 % slope by
                // Greers Chapel Drive): across each run of deck the profile starts from a straight line between the road's heights at its two ends, or the
                // ground under it where that stands higher; the crossings lift it where a road passes under
                var raw2 = (double[])raw.Clone();
                for (int i = 0; i < N;)
                {
                    bool Gap(int q) => L[q].bridge >= 0 || L[q].skip;
                    if (!Gap(i)) { i++; continue; }
                    int j = i; while (j + 1 < N && Gap(j + 1)) j++;
                    int a0 = Mathf.Max(0, i - 1), a1 = Mathf.Min(N - 1, j + 1);
                    double ya = !Gap(a0) ? raw[a0] : terrain.Height(L[a0].p.x, L[a0].p.y), yb = !Gap(a1) ? raw[a1] : terrain.Height(L[a1].p.x, L[a1].p.y);
                    bool skipRun = L[i].skip;
                    for (int k = i; k <= j; k++) { double t = (cum[k] - cum[a0]) / System.Math.Max(cum[a1] - cum[a0], 1e-3); double lin = ya + (yb - ya) * t; raw2[k] = skipRun ? lin : System.Math.Max(lin, terrain.Height(L[k].p.x, L[k].p.y)); }
                    i = j + 1;
                }
                var y0 = Gauss(raw2, R82Sigma);
                // the bounds from the crossings on this line
                var lbs = new List<(float s, float need, float spread, string what)>(); var ubs = new List<(float s, float cap, float spread, string what)>();
                foreach (var c in r82Cross.crossings)
                {
                    bool overMine = (c.over.kind == "bridge" ? lineBridges.Contains(c.over.index) : lineRoads.Contains(c.over.index));
                    bool underMine = (c.under.kind == "bridge" ? lineBridges.Contains(c.under.index) : lineRoads.Contains(c.under.index));
                    if (overMine == underMine) continue;
                    var X = new Vector2(c.x, c.z); float d = LineDist(X, 4f, out int sg, out float st); if (d > 3f) continue;
                    float s = Mathf.Lerp(cum[sg], cum[sg + 1], st); var ldir = (L[sg + 1].p - L[sg].p).normalized;
                    var other = overMine ? c.under : c.over; float top = R82TopOf(map, terrain, other, X, out float oh, out Vector2 odir);
                    float sin = Mathf.Max(.3f, Mathf.Abs(ldir.x * odir.y - ldir.y * odir.x)), spread = oh / sin + 3f;
                    string what = string.IsNullOrEmpty(other.name) ? $"{other.@class} {other.osm_id}" : $"{other.name} ({other.osm_id})";
                    if (overMine) lbs.Add((s, top + R82Clear + R82Slab + liftL + .3f, spread, what));
                    else
                    {
                        float cap = top - R82Lift(other.@class) - R82Slab - R82Clear, own = (float)y0[sg];
                        ubs.Add((s, Mathf.Max(cap, own - 1f), spread, what));
                    }
                }
                // each bound as a vertical curve, then a straight grade: the envelope under / over it
                float Fall(float s, float s0, float spread) { float d = Mathf.Max(0f, Mathf.Abs(s - s0) - spread), d1 = grade * R82R; return d <= d1 ? d * d / (2f * R82R) : d1 * d1 / (2f * R82R) + grade * (d - d1); }
                // (build 1: under Greers Chapel Drive, 130 m past its deck over the Barrett ramps, the line fell 10 %): a deck over the line never holds it lower
                // than the line can come down from a road it goes over (at the same curves and grade); that deck is raised over it instead (A)
                for (int u = 0; u < ubs.Count; u++)
                {
                    var b = ubs[u]; float cap = b.cap;
                    foreach (var l in lbs) cap = Mathf.Max(cap, l.need - Fall(b.s, l.s, l.spread + b.spread));
                    ubs[u] = (b.s, cap, b.spread, b.what);
                }
                var y = new double[N];
                const double C = 3.0;
                for (int j = 0; j < N; j++)
                {
                    double E = y0[j] - 50, U = y0[j] + 50;
                    foreach (var b in lbs) E = System.Math.Max(E, b.need - Fall(cum[j], b.s, b.spread));
                    foreach (var b in ubs) U = System.Math.Min(U, b.cap + Fall(cum[j], b.s, b.spread));
                    double v = y0[j];
                    v = .5 * (v + U - System.Math.Sqrt((v - U) * (v - U) + C * C));   // a smooth minimum (under a deck over it)
                    v = .5 * (v + E + System.Math.Sqrt((v - E) * (v - E) + C * C));   // a smooth maximum (over a road under it): this wins
                    y[j] = v;
                }
                // (build 3: the ground along the road had been graded into a 10 % ramp off the old deck's end before this pass, and the profile followed it):
                // no stretch steeper than grade: each too steep step relaxed from both its ends, never below a road's clearance under it
                var Ehard = new double[N]; var Uhard = new double[N];
                for (int j = 0; j < N; j++) { double E = double.MinValue; foreach (var lb in lbs) E = System.Math.Max(E, lb.need - Fall(cum[j], lb.s, lb.spread)); Ehard[j] = E; Uhard[j] = double.MaxValue; }
                // (build 5: easing the ends back over 80 m made A's short lines steep there): the two end points held to the road as it is, the grade limit
                // spreading the climb from them
                Ehard[0] = Uhard[0] = raw[0]; Ehard[N - 1] = Uhard[N - 1] = raw[N - 1]; y[0] = raw[0]; y[N - 1] = raw[N - 1];
                for (int it = 0; it < 3000; it++)
                {
                    bool any = false;
                    for (int j = 0; j + 1 < N; j++)
                    {
                        double ds = System.Math.Max(cum[j + 1] - cum[j], .1f), lim = grade * ds, dy = y[j + 1] - y[j];
                        if (System.Math.Abs(dy) <= lim + 1e-4) continue;
                        double ex = (System.Math.Abs(dy) - lim) * .5 * System.Math.Sign(dy); any = true;
                        y[j] = System.Math.Min(System.Math.Max(y[j] + ex, Ehard[j]), Uhard[j]); y[j + 1] = System.Math.Min(System.Math.Max(y[j + 1] - ex, Ehard[j + 1]), Uhard[j + 1]);
                    }
                    if (!any) break;
                }
                y = Gauss(y, 5f);
                // the outer ends eased back onto the road as it is
                for (int j = 0; j < N; j++) { double w = Smooth01(Mathf.Min(cum[j], cum[N - 1] - cum[j]) / 15f); y[j] = raw[j] + (y[j] - raw[j]) * w; }   // (the last 15 m onto the road as it is)
                // how it holds: the sharpest crest's speed, the steepest grade over 10 m, the bounds met
                float kMax = 0f, gMax = 0f, moved = 0f; Vector2 gAt = Vector2.zero, kAt = Vector2.zero;
                for (int i = 0; i < N; i++)
                {
                    int i0 = i, i1 = i; while (i0 > 0 && cum[i] - cum[i0] < 5f) i0--; while (i1 < N - 1 && cum[i1] - cum[i] < 5f) i1++;
                    float d0 = Mathf.Max(.5f, cum[i] - cum[i0]), d1 = Mathf.Max(.5f, cum[i1] - cum[i]);
                    float k = (float)(-((y[i1] - y[i]) / d1 - (y[i] - y[i0]) / d0) / ((d0 + d1) * .5f));
                    if (Mathf.Max(k, -k * .5f) > kMax) { kMax = Mathf.Max(k, -k * .5f); kAt = L[i].p; }
                    float gr = (float)System.Math.Abs((y[i1] - y[i0]) / Mathf.Max(cum[i1] - cum[i0], .5f)); if (gr > gMax) { gMax = gr; gAt = L[i].p; }
                    moved = Mathf.Max(moved, (float)System.Math.Abs(y[i] - raw[i]));
                }
                float hold = kMax > 1e-6f ? Mathf.Sqrt(9.81f / kMax) : 999f;
                float YAt(float s) { int j = 0; while (j < N - 1 && cum[j + 1] < s) j++; float t = (s - cum[j]) / Mathf.Max(cum[Mathf.Min(j + 1, N - 1)] - cum[j], 1e-4f); return (float)(y[j] + (y[Mathf.Min(j + 1, N - 1)] - y[j]) * Mathf.Clamp01(t)); }
                var lbTxt = lbs.Select(b => $"{b.what} {YAt(b.s) - b.need + .3f + R82Clear:0.0} m clear").ToList();
                var ubTxt = ubs.Select(b => $"{b.what} {b.cap - YAt(b.s) + R82Clear:0.0} m clear (at most 1 m into its ground)").ToList();
                // the decks take it
                for (int j = 0; j < N; j++) if (L[j].bridge >= 0) { map.bridges[L[j].bridge].deck_y[L[j].bi] = (float)y[j] - liftL; if (L[j].alias != null) foreach (var (ab, ai) in L[j].alias) map.bridges[ab].deck_y[ai] = (float)y[j] - liftL; }
                // the ground graded to it, a piece of about 300 m of line at a time (each sample by its nearest point of the line in that piece)
                var lineSet = new HashSet<int>(lineBridges); int changed = 0, cut = 0;
                const float bankMax = 14f;
                bool shoulder = tag == "line";   // Round 83 (A): the Express Lanes (walled) get a flat shoulder to their wall
                for (int c0 = 0; c0 < N - 1; c0 += 150)
                {
                    int c1 = Mathf.Min(N - 1, c0 + 150);
                    var lo = new Vector2(float.MaxValue, float.MaxValue); var hi = new Vector2(float.MinValue, float.MinValue);
                    for (int j = c0; j <= c1; j++) { lo = Vector2.Min(lo, L[j].p); hi = Vector2.Max(hi, L[j].p); }
                    float reachL = halfL + bankMax + (shoulder ? 3.8f : 0f);   // Round 83 (A): the shoulder widens the grading
                    lo -= Vector2.one * reachL; hi += Vector2.one * reachL;
                    EditTerrain(terrain, Rect.MinMaxRect(lo.x, lo.y, hi.x, hi.y), (x, z, yy) =>
                    {
                        var p = new Vector2(x, z); float dd = LineDist(p, reachL, out int i, out float t);
                        if (dd > reachL || i < c0 || i >= c1) return yy;
                        // Round 83 (the dry run: Dickson Road's line, laid after the Express Lanes, cut their road 0.4 to 1.1 m at both ends of their deck over it):
                        // the Express Lanes' pavement on the ground is theirs alone
                        if (!shoulder && dd > halfL + .5f && R83OnExpress(p)) return yy;
                        // (build 5: a sample just past a deck's end, nearest its last segment, was cut 1 m) a deck only where the sample lies along it
                        bool deck = L[i].bridge >= 0 && L[i + 1].bridge >= 0 && !((i + 2 >= N || L[i + 2].bridge < 0) && t > .98f) && !((i == 0 || L[i - 1].bridge < 0) && t < .02f);
                        float tg = (float)(y[i] + (y[i + 1] - y[i]) * t) - liftL;
                        if (deck)
                        {
                            if (dd > halfL + 1f) return yy;
                            float top = tg - R82Slab - .3f; if (yy > top) { cut++; return top; }
                            return yy;
                        }
                        // never up under another deck (BUG-006): only cut there, to under its slab
                        // a deck overhead caps the ground; a deck at the line's own height (a merging ramp's, a deck beside it) only beyond the line's pavement
                        // (build 5: the I-75 general lanes' bank came up 1.4 m through the Express deck beside them at Dickson Road)
                        bool under = UnderOtherDeck(p, lineSet, out float slabB) && (slabB > tg + 1f || dd > halfL + .5f);
                        float rise = Mathf.Abs(tg - yy), bank = Mathf.Clamp(2f * rise, 4f, bankMax);
                        if (dd > halfL + .5f)
                        {
                            // (build 5, BUG-005's picture: a sawtooth ridge where the bank stopped at the general lanes' edge) another road's pavement is left as it
                            // is; ground between the line and another road within 14 m is one straight slope from the line's edge to that road's
                            float oBest = float.MaxValue; Vector2 oAt = Vector2.zero;
                            foreach (int si in segIdxAll.Near(p, 14f, tmpNear))
                            {
                                var sgm = segAll[si]; if (lineRoads.Contains(sgm.road)) continue;
                                var ab = sgm.b - sgm.a; float tt = Mathf.Clamp01(Vector2.Dot(p - sgm.a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
                                float od = (sgm.a + ab * tt - p).magnitude;
                                if (od < sgm.half + .3f) return under ? Mathf.Min(yy, slabB - .3f) : yy;
                                if (od - sgm.half < oBest) { oBest = od - sgm.half; oAt = sgm.a + ab * tt; }
                            }
                            if (oBest < 14f)
                            {
                                // Round 83 (A: a car drifting off the I-75 Express Lanes by the I-575 flyover climbed the slope up to the flyover's road, which began
                                // at the lane's edge, and was thrown over the lanes): beside the Express Lanes a flat shoulder out past their wall (3.8 m, or half
                                // the gap where the other road is nearer) before the slope
                                float gap = dd - halfL - .5f + oBest, sh = shoulder ? Mathf.Min(3.8f, gap * .5f) : 0f;
                                float a0 = Mathf.Max(0f, dd - halfL - .5f - sh), f = a0 / Mathf.Max(gap - sh, .1f);
                                float oy = terrain.Height(oAt.x, oAt.y), my = Mathf.Lerp(tg, oy, f);
                                if (under) my = Mathf.Min(my, slabB - .3f);
                                if (Mathf.Abs(my - yy) > 1e-3f) changed++; return my;
                            }
                        }
                        float sh2 = shoulder ? 3.8f : 0f;   // Round 83 (A): the Express Lanes' shoulder out past their wall, flat
                        if (dd > halfL + .5f + sh2 + bank) return yy;
                        float w = dd <= halfL + .5f + sh2 ? 1f : Smooth01(1f - (dd - halfL - .5f - sh2) / bank); float ny = yy + w * (tg - yy);
                        if (under) ny = Mathf.Min(ny, slabB - .3f);
                        if (Mathf.Abs(ny - yy) > 1e-3f) changed++; return ny;
                    });
                }
                foreach (var end in BridgeEnds)
                {
                    if (!lineBridges.Contains(end.bridge)) continue; var bb = map.bridges[end.bridge]; int n = bb.deck_y.Length;
                    end.deckTop = bb.deck_y[end.start ? 0 : n - 1] + R82Lift(bb.@class); end.slabBottom = end.deckTop - R82Slab; end.ground = terrain.Height(end.E.x, end.E.y);
                }
                graded.Add((L.Select(v => v.p).ToList(), y.Select(v => (float)v).ToArray()));
                log.Add($"{tag} {li}: from ({L[0].p.x:0}, {L[0].p.y:0}) to ({L[N - 1].p.x:0}, {L[N - 1].p.y:0}), {cum[N - 1]:0} m, {L.Count(v => v.bridge >= 0)} deck points; held to {hold:0} m/s (the sharpest crest at ({kAt.x:0}, {kAt.y:0})), " +
                        $"steepest {gMax * 100f:0.0} % at ({gAt.x:0}, {gAt.y:0}); moved up to {moved:0.0} m; {changed} road samples graded, {cut} cut under its decks");
                log.Add($"  over: {(lbTxt.Count > 0 ? string.Join("; ", lbTxt) : "none")}");
                log.Add($"  under: {(ubTxt.Count > 0 ? string.Join("; ", ubTxt) : "none")}");
                li++;
            }
        }

        static void R82Joins(MapData map, TerrainGrid terrain, List<(List<Vector2> p, float[] y)> lines, HashSet<int> lineRoads, HashSet<int> lineBridges, float halfL, float liftL, List<string> log)
        {
            var idx = new CellIndex(16f); var segs = new List<(int line, int i)>();
            for (int l = 0; l < lines.Count; l++) for (int i = 0; i + 1 < lines[l].p.Count; i++) { segs.Add((l, i)); idx.Add(lines[l].p[i], lines[l].p[i + 1], .01f); }
            var near = new List<int>();
            float Near(Vector2 p, float reach, out float h)
            {
                float best = float.MaxValue; h = 0f;
                foreach (int k in idx.Near(p, reach, near))
                {
                    var (l, i) = segs[k]; var a = lines[l].p[i]; var b = lines[l].p[i + 1]; var ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
                    float d = (a + ab * t - p).magnitude; if (d < best) { best = d; h = Mathf.Lerp(lines[l].y[i], lines[l].y[i + 1], t); }
                }
                return best;
            }
            // the other pieces (roads on the ground, decks): their points
            var pcs = new List<(bool deck, int idx, List<Vector2> q, float half, string cls)>();
            for (int i = 0; i < map.roads.Length; i++) if (!lineRoads.Contains(i) && map.roads[i].name != ExpressName) pcs.Add((false, i, ToPoints(map.roads[i].points), map.roads[i].width * .5f, map.roads[i].@class));
            for (int i = 0; i < map.bridges.Length; i++) if (!lineBridges.Contains(i) && map.bridges[i].deck_y != null && map.bridges[i].name != ExpressName) pcs.Add((true, i, ToPoints(map.bridges[i].points), map.bridges[i].width * .5f, map.bridges[i].@class));
            var endIdx = new CellIndex(8f); var endOf = new List<(int pc, bool start)>();
            for (int k = 0; k < pcs.Count; k++) { endIdx.Add(pcs[k].q[0]); endOf.Add((k, true)); endIdx.Add(pcs[k].q[pcs[k].q.Count - 1]); endOf.Add((k, false)); }
            var enear = new List<int>();
            var doneDeck = new HashSet<int>(); var touchedDecks = new HashSet<int>();
            bool expressLine = lineRoads.Any(i => map.roads[i].name == ExpressName) || lineBridges.Any(i => map.bridges[i].name == ExpressName);
            for (int k = 0; k < pcs.Count; k++)
                {
                    var pc = pcs[k]; if (pc.q.Count < 2) continue;
                    bool atStart = Near(pc.q[0], 2f, out _) < 1.5f, atEnd = Near(pc.q[pc.q.Count - 1], 2f, out _) < 1.5f;
                    if (!atStart && !atEnd) continue;
                    if (atStart && atEnd) continue;   // a piece lying along the line both ends (none expected)
                    // Round 83 (the dry run: Chastain Road's deck, its end lying over I-575's general lanes in plan, was "joined" to them and pulled down 12 m
                    // onto the Express Lanes): an end more than 3 m above or below the line there is a deck crossing it, not a road joining it
                    {
                        var E = atStart ? pc.q[0] : pc.q[pc.q.Count - 1]; Near(E, 2f, out float lhE);
                        float endY = pc.deck ? map.bridges[pc.idx].deck_y[atStart ? 0 : map.bridges[pc.idx].deck_y.Length - 1] + R82Lift(pc.cls) : terrain.Height(E.x, E.y) + R82Lift(pc.cls);
                        if (Mathf.Abs(endY - (lhE - liftL + R82Lift(pc.cls))) > 3f) { log.Add($"  not joined: {pc.cls} {(pc.deck ? map.bridges[pc.idx].osm_id : map.roads[pc.idx].osm_id)} at ({E.x:0}, {E.y:0}), {endY - lhE + liftL - R82Lift(pc.cls):+0.0;-0.0} m off the line (crossing it)"); continue; }
                    }
                    // the path away from the line: this piece, then the pieces that run on from its far end (roads or decks), up to 500 m
                    var path = new List<(Vector2 p, int pc, int i)>();
                    int cur = k; bool fwd = atStart; float len = 0f;
                    var seen = new HashSet<int>();
                    while (cur >= 0 && len < 500f && seen.Add(cur))
                    {
                        var q = pcs[cur].q; int n = q.Count;
                        for (int j = 0; j < n; j++) { int ii = fwd ? j : n - 1 - j; if (path.Count > 0 && (path[path.Count - 1].p - q[ii]).magnitude < .3f) continue; if (path.Count > 0) len += (q[ii] - path[path.Count - 1].p).magnitude; path.Add((q[ii], cur, ii)); }
                        var tail = path[path.Count - 1].p; var dir = (tail - path[Mathf.Max(0, path.Count - 2)].p).normalized; int nx = -1; bool nf = true; float bestDot = .5f;
                        foreach (int e in endIdx.Near(tail, 2f, enear))
                        {
                            var (o, st) = endOf[e]; if (o == cur || seen.Contains(o)) continue; var qo = pcs[o].q; var E = st ? qo[0] : qo[qo.Count - 1];
                            if ((E - tail).magnitude > 2f) continue; var nd = ((st ? qo[1] : qo[qo.Count - 2]) - E).normalized; float dt = Vector2.Dot(nd, dir);
                            if (dt > bestDot) { bestDot = dt; nx = o; nf = st; }
                        }
                        cur = nx; fwd = nf;
                    }
                    // resampled at 1 m with the source of each point (a deck's own points kept)
                    var rp = new List<(Vector2 p, int pc, int i)>();
                    for (int j = 0; j + 1 < path.Count; j++)
                    {
                        rp.Add(path[j]); var a = path[j].p; var b = path[j + 1].p; float d = (b - a).magnitude;
                        if (!pcs[path[j].pc].deck || path[j].pc != path[j + 1].pc) for (float s = 1f; s < d; s += 1f) rp.Add((Vector2.Lerp(a, b, s / d), pcs[path[j].pc].deck && pcs[path[j + 1].pc].deck ? -1 : (pcs[path[j].pc].deck ? path[j + 1].pc : path[j].pc), -1));
                    }
                    rp.Add(path[path.Count - 1]);
                    int M = rp.Count; if (M < 3) continue;
                    var cum = new float[M]; for (int j = 1; j < M; j++) cum[j] = cum[j - 1] + (rp[j].p - rp[j - 1].p).magnitude;
                    float Own(int j) { var e = rp[j]; if (e.pc >= 0 && pcs[e.pc].deck && e.i >= 0) return map.bridges[pcs[e.pc].idx].deck_y[e.i] + R82Lift(pcs[e.pc].cls); return terrain.Height(e.p.x, e.p.y) + R82Lift(e.pc >= 0 ? pcs[e.pc].cls : "motorway_link"); }
                    var own = new float[M]; for (int j = 0; j < M; j++) own[j] = Own(j);
                    var lh = new float[M]; var ld = new float[M]; for (int j = 0; j < M; j++) ld[j] = Near(rp[j].p, 30f, out lh[j]);
                    // beside the line: until the road is clear of it (both halves + 4 m)
                    float sep = halfL + pc.half + 4f; int jp = 0; while (jp + 1 < M && ld[jp + 1] < sep) jp++;
                    float hPar = lh[jp] - liftL + R82Lift(pc.cls), dh = hPar - own[jp];
                    if (Mathf.Abs(dh) < .05f && jp < 3) continue;
                    float D = Mathf.Clamp(Mathf.Abs(dh) / .035f, 40f, 300f);
                    var tgt = new float[M];
                    for (int j = 0; j < M; j++)
                    {
                        if (j <= jp) tgt[j] = lh[j] - liftL + R82Lift(pc.cls);
                        else { float w = Smooth01((cum[j] - cum[jp]) / D); tgt[j] = Mathf.Lerp(own[j] + dh, own[j], w); }   // its own profile, shifted, easing back
                    }
                    if (cum[M - 1] - cum[jp] < D) log.Add($"  join {pc.cls} {(pc.deck ? map.bridges[pc.idx].osm_id : map.roads[pc.idx].osm_id)}: only {cum[M - 1] - cum[jp]:0} m of road to ease {dh:0.0} m back over (wants {D:0})");
                    // the decks on it take it
                    for (int j = 0; j < M; j++) { var e = rp[j]; if (e.pc >= 0 && pcs[e.pc].deck && e.i >= 0) { map.bridges[pcs[e.pc].idx].deck_y[e.i] = tgt[j] - R82Lift(pcs[e.pc].cls); touchedDecks.Add(pcs[e.pc].idx); } }
                    // its ground graded across its width (banks 4 to 10 m), the gap to the line filled level while beside it, never up under a deck
                    var pIdx = new CellIndex(16f); for (int j = 0; j + 1 < M; j++) pIdx.Add(rp[j].p, rp[j + 1].p, .01f);
                    var pn = new List<int>(); int changed = 0;
                    float reach = pc.half + 10f;
                    var pathRoads = new HashSet<int>(lineRoads); foreach (var e in rp) if (e.pc >= 0 && !pcs[e.pc].deck) pathRoads.Add(pcs[e.pc].idx);
                    for (int c0 = 0; c0 < M - 1; c0 += 200)
                    {
                        int c1 = Mathf.Min(M - 1, c0 + 200);
                        var lo = new Vector2(float.MaxValue, float.MaxValue); var hi = new Vector2(float.MinValue, float.MinValue);
                        for (int j = c0; j <= c1; j++) { lo = Vector2.Min(lo, rp[j].p); hi = Vector2.Max(hi, rp[j].p); }
                        lo -= Vector2.one * reach; hi += Vector2.one * reach;
                        EditTerrain(terrain, Rect.MinMaxRect(lo.x, lo.y, hi.x, hi.y), (x, z, yy) =>
                        {
                            var p = new Vector2(x, z); float best = float.MaxValue; int bi = 0; float bt = 0f;
                            foreach (int s in pIdx.Near(p, reach, pn)) { var ab = rp[s + 1].p - rp[s].p; float t = Mathf.Clamp01(Vector2.Dot(p - rp[s].p, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f)); float d = (rp[s].p + ab * t - p).magnitude; if (d < best) { best = d; bi = s; bt = t; } }
                            if (best > reach || bi < c0 || bi >= c1) return yy;
                            float tg = Mathf.Lerp(tgt[bi], tgt[bi + 1], bt) - R82Lift(pc.cls);
                            bool onDeck = rp[bi].pc >= 0 && rp[bi + 1].pc >= 0 && pcs[rp[bi].pc].deck && pcs[rp[bi + 1].pc].deck && !((bi + 2 >= M || rp[bi + 2].pc < 0 || !pcs[rp[bi + 2].pc].deck) && bt > .98f) && !((bi == 0 || rp[bi - 1].pc < 0 || !pcs[rp[bi - 1].pc].deck) && bt < .02f)
                                          ;
                            float dl = Near(p, 30f, out float lhh);
                            bool between = bi <= jp && dl < halfL + 8f && best < pc.half + 8f && best + dl < sep + 6f;
                            onDeck &= best <= pc.half + 1f || between;   // Round 83 (the dry run: the I-75 deck over Frey Road cut a 4 m hole in the Express Lanes' road 8 m beside it): only under its own width, or the gap to the line beside it
                            if (dl < halfL + .5f) return yy;   // the line's own road (graded with it; build 4: the merging ramp's deck cut a 1.7 m hole in it)
                            if (!expressLine && R83OnExpress(p)) return yy;   // Round 83: the Express Lanes' pavement is their own line's (R82ExpressLines), never another road's grading
                            // Round 83 (the dry run: the I-75 deck over Frey Road joined to the general lanes filled the medians between the decks back up to deck
                            // height across Frey Road, laid just before): another road's pavement is never raised by a join (only cut under a deck)
                            bool otherPave = R83OnOtherRoad(p, pathRoads);
                            bool under = UnderOtherDeck(p, null, out float slabB) && (slabB > tg + 1f || best > pc.half + .5f);
                            if (onDeck || under)
                            {
                                // under its own deck, the line's or another's (the gap between two decks side by side too): cut clear of the slabs
                                float cap = under ? slabB - .3f : tg - R82Slab - .3f; if (between) cap = Mathf.Min(cap, Mathf.Min(tg, lhh - liftL) - R82Slab - .3f);
                                return yy > cap ? cap : yy;
                            }
                            if (dl < halfL + .5f) return yy;   // the line's own road (graded with it)
                            if (otherPave) return yy;
                            if (between)
                            {
                                float lineG = lhh - liftL; float f = Mathf.Clamp01((dl - halfL) / Mathf.Max(best - pc.half + dl - halfL, .1f));
                                float ny0 = Mathf.Lerp(lineG, tg, f); if (Mathf.Abs(ny0 - yy) > 1e-3f) changed++; return ny0;
                            }
                            float rise = Mathf.Abs(tg - yy), bank = Mathf.Clamp(2f * rise, 4f, 10f);
                            if (best > pc.half + .5f + bank) return yy;
                            float w = best <= pc.half + .5f ? 1f : Smooth01(1f - (best - pc.half - .5f) / bank); float ny = yy + w * (tg - yy);
                            if (Mathf.Abs(ny - yy) > 1e-3f) changed++; return ny;
                        });
                    }
                    log.Add($"join {pc.cls} {(pc.deck ? map.bridges[pc.idx].osm_id : map.roads[pc.idx].osm_id)} at ({rp[0].p.x:0}, {rp[0].p.y:0}): beside the line {cum[jp]:0} m, then {dh:+0.0;-0.0} m eased back over {D:0} m; {changed} samples");
                }
            foreach (var end in BridgeEnds)
            {
                if (!touchedDecks.Contains(end.bridge)) continue; var bb = map.bridges[end.bridge]; int n = bb.deck_y.Length;
                end.deckTop = bb.deck_y[end.start ? 0 : n - 1] + R82Lift(bb.@class); end.slabBottom = end.deckTop - R82Slab; end.ground = terrain.Height(end.E.x, end.E.y);
            }
        }

        // A (build 4's drives: Round 81's chain pass, pinned to the side streets within 40 m, raised Skip Spann Connector's decks in humps a car flew 2.8 s off):
        // each road whose deck must rise is laid as the Express Lanes are: its decks and 350 m of the road on each side as one profile (at most 6 %, 4.5 % on
        // a highway), the side streets that end on it joined to it
        static void R82RoadLines(MapData map, TerrainGrid terrain, HashSet<int> decks, string report, HashSet<int> laidRoads = null)
        {
            var log = new List<string> { "Round 82 (A): the roads whose decks were too low over another road, each laid with 350 m of road either side as one profile (MapBuilder.Round82.cs)" };
            var pcs = new List<(bool deck, int idx, List<Vector2> q, string name)>();
            for (int i = 0; i < map.roads.Length; i++) if (!map.roads[i].bridge && map.roads[i].name != ExpressName) pcs.Add((false, i, ToPoints(map.roads[i].points), map.roads[i].name));
            for (int i = 0; i < map.bridges.Length; i++) if (map.bridges[i].deck_y != null && map.bridges[i].name != ExpressName) pcs.Add((true, i, ToPoints(map.bridges[i].points), map.bridges[i].name));
            var endIdx = new CellIndex(8f); var endOf = new List<(int pc, bool start)>();
            for (int k = 0; k < pcs.Count; k++) { endIdx.Add(pcs[k].q[0]); endOf.Add((k, true)); endIdx.Add(pcs[k].q[pcs[k].q.Count - 1]); endOf.Add((k, false)); }
            var enear = new List<int>(); var used = new HashSet<int>();
            // the piece that runs on from a path's end (best heading, the same name first)
            int Next(int cur, Vector2 tail, Vector2 dir, string name, HashSet<int> seen, out bool fromStart)
            {
                int nx = -1; fromStart = true; float best = -9f;
                foreach (int e in endIdx.Near(tail, 2.5f, enear))
                {
                    var (o, st) = endOf[e]; if (o == cur || seen.Contains(o)) continue; var qo = pcs[o].q; var E = st ? qo[0] : qo[qo.Count - 1];
                    if ((E - tail).magnitude > 2.5f) continue; var nd = ((st ? qo[1] : qo[qo.Count - 2]) - E).normalized; float dt = Vector2.Dot(nd, dir);
                    if (dt < 0f) continue; float score = dt + (pcs[o].name == name && !string.IsNullOrEmpty(name) ? 1f : 0f);   // (build 5: stopping at a junction 25 m past the deck left no room to climb: on through it)
                    if (score > best) { best = score; nx = o; fromStart = st; }
                }
                return nx;
            }
            foreach (int bi in decks.ToList())
            {
                int k0 = pcs.FindIndex(x => x.deck && x.idx == bi); if (k0 < 0 || used.Contains(k0)) continue;
                var seen = new HashSet<int> { k0 }; string name = pcs[k0].name;
                // walk one way from the piece's end: pieces and their direction
                List<(int pc, bool fwd)> Walk(bool fromEnd)
                {
                    var o = new List<(int, bool)>(); int cur = k0; bool fwd = fromEnd; float beyond = 0f;
                    while (true)
                    {
                        var q = pcs[cur].q; var tail = fwd ? q[q.Count - 1] : q[0]; var prev = fwd ? q[q.Count - 2] : q[1];
                        int nx = Next(cur, tail, (tail - prev).normalized, name, seen, out bool st); if (nx < 0) break;
                        seen.Add(nx); o.Add((nx, st)); cur = nx; fwd = st;
                        if (pcs[nx].deck) beyond = 0f; else { var qn = pcs[nx].q; for (int j = 1; j < qn.Count; j++) beyond += (qn[j] - qn[j - 1]).magnitude; }
                        if (beyond >= 350f || o.Count > 40) break;
                    }
                    return o;
                }
                var ahead = Walk(true); var back = Walk(false);
                var seq = Enumerable.Reverse(back).Select(x => (x.pc, !x.fwd)).ToList(); seq.Add((k0, true)); seq.AddRange(ahead);
                var line = new List<R82P>(); var lb = new HashSet<int>(); var lr = new HashSet<int>();
                foreach (var (pc, fwd) in seq)
                {
                    used.Add(pc); var P = pcs[pc]; var q = fwd ? P.q : Enumerable.Reverse(P.q).ToList(); int n = q.Count;
                    if (P.deck) { lb.Add(P.idx); for (int j = 0; j < n; j++) line.Add(new R82P { p = q[j], bridge = P.idx, bi = fwd ? j : n - 1 - j }); }
                    else { lr.Add(P.idx); foreach (var v in Resample(q, 2f)) line.Add(new R82P { p = v }); }
                }
                var clean = new List<R82P>();
                foreach (var v in line) { if (clean.Count > 0 && (clean[clean.Count - 1].p - v.p).magnitude < .5f) { var pv = clean[clean.Count - 1]; if (v.bridge >= 0) { if (pv.bridge >= 0) { v.alias = v.alias ?? new List<(int, int)>(); v.alias.Add((pv.bridge, pv.bi)); if (pv.alias != null) v.alias.AddRange(pv.alias); } clean[clean.Count - 1] = v; } continue; } clean.Add(v); }
                if (clean.Count < 4) continue;
                // the far ends trimmed to 350 m past the outermost decks
                int f0 = clean.FindIndex(v => v.bridge >= 0), f1 = clean.FindLastIndex(v => v.bridge >= 0);
                float acc = 0f; int a0 = f0; while (a0 > 0 && acc < 350f) { acc += (clean[a0].p - clean[a0 - 1].p).magnitude; a0--; }
                acc = 0f; int a1 = f1; while (a1 < clean.Count - 1 && acc < 350f) { acc += (clean[a1 + 1].p - clean[a1].p).magnitude; a1++; }
                clean = clean.GetRange(a0, a1 - a0 + 1);
                var b0 = map.bridges[bi]; string cls = b0.@class ?? ""; bool fast = cls == "motorway" || cls == "trunk" || cls == "motorway_link";
                var graded = new List<(List<Vector2> p, float[] y)>();
                R82Lay(map, terrain, new List<List<R82P>> { clean }, lb, lr, R82Lift(cls), b0.width * .5f, fast ? R82Grade : .06f, log, graded,
                       $"{(string.IsNullOrEmpty(b0.name) ? cls : b0.name)} ({string.Join(" + ", lb.Select(x => map.bridges[x].osm_id))})");
                R82Joins(map, terrain, graded, lr, lb, b0.width * .5f, R82Lift(cls), log);
                foreach (int x in lb) decks.Add(x);
                if (laidRoads != null) foreach (int x in lr) laidRoads.Add(x);
            }
            Debug.Log("[Round82] A: roads laid for clearance\n" + string.Join("\n", log));
            try { File.WriteAllLines(Path.Combine(ProjectRoot, GeneratedDir, report), log); } catch (System.Exception e) { Debug.LogWarning("[Round82] " + e.Message); }
        }

        // A (build 5's drive and probe: Frey Road humped 3.4 m under I-75's and the Express Lanes' decks, the lidar's ground filled in between the abutments,
        // a car flew 0.9 s): "the one under keeps its own level, the ground cut for it": every road on the ground under a deck (not the Express Lanes, laid
        // already, nor a road laid as an upper one) laid 150 m each side of its crossings, its profile taken across the ground under the deck from the road
        // either side (not the fill), the road graded to it, side streets joined
        static void R82UnderLines(MapData map, TerrainGrid terrain, HashSet<int> laidRoads, List<string> log)
        {
            var pcs = new List<(bool deck, int idx, List<Vector2> q, string name)>();
            for (int i = 0; i < map.roads.Length; i++) if (!map.roads[i].bridge && map.roads[i].name != ExpressName) pcs.Add((false, i, ToPoints(map.roads[i].points), map.roads[i].name));
            var endIdx = new CellIndex(8f); var endOf = new List<(int pc, bool start)>();
            for (int k = 0; k < pcs.Count; k++) { endIdx.Add(pcs[k].q[0]); endOf.Add((k, true)); endIdx.Add(pcs[k].q[pcs[k].q.Count - 1]); endOf.Add((k, false)); }
            var pcOfRoad = new Dictionary<int, int>(); for (int k = 0; k < pcs.Count; k++) pcOfRoad[pcs[k].idx] = k;
            var enear = new List<int>(); var used = new HashSet<int>();
            int Next(int cur, Vector2 tail, Vector2 dir, string name, HashSet<int> seen, out bool fromStart)
            {
                int nx = -1; fromStart = true; float best = -9f;
                foreach (int e in endIdx.Near(tail, 2.5f, enear))
                {
                    var (o, st) = endOf[e]; if (o == cur || seen.Contains(o)) continue; var qo = pcs[o].q; var E = st ? qo[0] : qo[qo.Count - 1];
                    if ((E - tail).magnitude > 2.5f) continue; var nd = ((st ? qo[1] : qo[qo.Count - 2]) - E).normalized; float dt = Vector2.Dot(nd, dir);
                    if (dt < .5f) continue; float score = dt + (pcs[o].name == name && !string.IsNullOrEmpty(name) ? 1f : 0f);
                    if (score > best) { best = score; nx = o; fromStart = st; }
                }
                return nx;
            }
            int n0 = log.Count;
            foreach (var c in r82Cross.crossings)
            {
                if (c.under.kind != "road" || laidRoads.Contains(c.under.index) || !pcOfRoad.TryGetValue(c.under.index, out int k0) || used.Contains(k0)) continue;
                var seen = new HashSet<int> { k0 }; string name = pcs[k0].name;
                List<(int pc, bool fwd)> Walk(bool fromEnd)
                {
                    var o = new List<(int, bool)>(); int cur = k0; bool fwd = fromEnd; float beyond = 0f;
                    while (beyond < 200f && o.Count < 30)
                    {
                        var q = pcs[cur].q; var tail = fwd ? q[q.Count - 1] : q[0]; var prev = fwd ? q[q.Count - 2] : q[1];
                        int nx = Next(cur, tail, (tail - prev).normalized, name, seen, out bool st); if (nx < 0) break;
                        seen.Add(nx); o.Add((nx, st)); cur = nx; fwd = st;
                        var qn = pcs[nx].q; for (int j = 1; j < qn.Count; j++) beyond += (qn[j] - qn[j - 1]).magnitude;
                    }
                    return o;
                }
                var ahead = Walk(true); var back = Walk(false);
                var seq = Enumerable.Reverse(back).Select(x => (x.pc, !x.fwd)).ToList(); seq.Add((k0, true)); seq.AddRange(ahead);
                var line = new List<R82P>(); var lr = new HashSet<int>();
                foreach (var (pc, fwd) in seq) { used.Add(pc); lr.Add(pcs[pc].idx); var q = fwd ? pcs[pc].q : Enumerable.Reverse(pcs[pc].q).ToList(); foreach (var v in Resample(q, 2f)) line.Add(new R82P { p = v }); }
                var clean = new List<R82P>(); foreach (var v in line) if (clean.Count == 0 || (clean[clean.Count - 1].p - v.p).magnitude >= .5f) clean.Add(v);
                if (clean.Count < 10) continue;
                // the ground under every deck over this line (and 12 m either side): taken across, not as it is
                int nSkip = 0;
                foreach (var v in clean) { var p = v.p; for (int dx = -1; dx <= 1 && !v.skip; dx++) for (int dz = -1; dz <= 1 && !v.skip; dz++) { if (UnderOtherDeck(p + new Vector2(dx, dz) * 12f, null, out float sb) && sb > terrain.Height(p.x, p.y) - 2f) v.skip = true; } if (v.skip) nSkip++; }
                if (nSkip == 0) continue;
                // trimmed to 150 m past the outermost skipped point
                int f0 = clean.FindIndex(v => v.skip), f1 = clean.FindLastIndex(v => v.skip);
                float acc = 0f; int a0 = f0; while (a0 > 0 && acc < 150f) { acc += (clean[a0].p - clean[a0 - 1].p).magnitude; a0--; }
                acc = 0f; int a1 = f1; while (a1 < clean.Count - 1 && acc < 150f) { acc += (clean[a1 + 1].p - clean[a1].p).magnitude; a1++; }
                clean = clean.GetRange(a0, a1 - a0 + 1);
                var r0 = map.roads[c.under.index]; string cls = r0.@class ?? ""; bool fast = cls == "motorway" || cls == "trunk" || cls == "motorway_link";
                var graded = new List<(List<Vector2> p, float[] y)>();
                R82Lay(map, terrain, new List<List<R82P>> { clean }, new HashSet<int>(), lr, R82Lift(cls), r0.width * .5f, fast ? R82Grade : .06f, log, graded,
                       $"under: {(string.IsNullOrEmpty(r0.name) ? cls : r0.name)} ({r0.osm_id}) at ({c.x:0}, {c.z:0})");
                R82Joins(map, terrain, graded, lr, new HashSet<int>(), r0.width * .5f, R82Lift(cls), log);
                foreach (int x in lr) laidRoads.Add(x);
            }
            Debug.Log($"[Round82] A: {log.Count - n0} lines of the roads under decks laid at their own level");
        }

        // ------------------------------------------------------------ A: every crossing, its clearance; the short ones laid again
        static void R82Crossings(MapData map, TerrainGrid terrain)
        {
            if (r82Cross == null) { R82DeckIndex(map); r82Cross = R82LoadCrossings(); }
            var mid = R82Clearances(map, terrain);
            // the short ones: their upper deck chains (every deck piece touching end to end), laid again with their roads (not the Express Lanes': their line)
            var fix = new HashSet<int>();
            foreach (var (c, cl) in mid) if (cl < R82MinClear && c.over.kind == "bridge" && map.bridges[c.over.index].name != ExpressName) fix.Add(c.over.index);
            void Close()
            {
                for (bool grew = true; grew;)
                {
                    grew = false;
                    for (int bi = 0; bi < map.bridges.Length; bi++)
                    {
                        if (fix.Contains(bi) || map.bridges[bi].name == ExpressName) continue; var (a, z) = Ends(map.bridges[bi], false);
                        foreach (int f in fix.ToList()) { var (fa, fz) = Ends(map.bridges[f], false); if (Mathf.Min((a - fa).magnitude, (a - fz).magnitude, (z - fa).magnitude, (z - fz).magnitude) < 1.5f) { fix.Add(bi); grew = true; break; } }
                    }
                }
            }
            Close();
            var fixAll = new HashSet<int>(); int passes = 0; var laidRoads = new HashSet<int>();
            for (; passes < 3 && fix.Count > 0; passes++)
            {
                R82RoadLines(map, terrain, fix, passes == 0 ? "round82_crossing_fixes.txt" : $"round82_crossing_fixes_{passes + 1}.txt", laidRoads);
                fixAll.UnionWith(fix); fix.Clear();
                // (a deck raised for the roads under it can come up under a deck over it: that one in the next pass)
                foreach (var (c, cl) in R82Clearances(map, terrain)) if (cl < R82MinClear && c.over.kind == "bridge" && map.bridges[c.over.index].name != ExpressName) fix.Add(c.over.index);
                Close();
            }
            BuildClock.Lap("R82Crossings: the raised roads");
            var ulog = new List<string> { "Round 82 (A): the roads under decks laid at their own level (MapBuilder.Round82.cs R82UnderLines)" };
            R82UnderLines(map, terrain, laidRoads, ulog);
            try { File.WriteAllLines(Path.Combine(ProjectRoot, GeneratedDir, "round82_under_lines.txt"), ulog); } catch (System.Exception e) { Debug.LogWarning("[Round82] " + e.Message); }
            var after = R82Clearances(map, terrain);
            var log = new List<string> { $"Round 82 (A): every road crossing another without a shared node (Data/tracks/crossings.json, python Tools/mapgen/crossings.py): {after.Count}. " +
                                         $"Per crossing: where, the road over (why it is over), the road under, the clearance under the upper slab at OSM's heights -> after the Express Lanes' lines -> as built; " +
                                         $"{fixAll.Count} deck pieces laid again for clearance in {passes} pass(es) (round82_crossing_fixes*.txt)" };
            int shortN = 0;
            for (int k = 0; k < after.Count; k++)
            {
                var (c, cl) = after[k]; string N(R82CrossRef r) => string.IsNullOrEmpty(r.name) ? $"{r.@class} {r.osm_id}" : $"{r.name} ({r.osm_id})";
                string b0 = k < r82CrossBefore.Count ? r82CrossBefore[k] : "?";
                bool ok = cl >= R82MinClear - .05f; if (!ok) shortN++;
                log.Add($"{(ok ? "ok   " : "SHORT")} ({c.x:0}, {c.z:0}) {c.over.kind} {N(c.over)} [{c.why}] over {c.under.kind} {N(c.under)}: {b0} -> {mid[k].clear:0.0} -> {cl:0.0} m");
            }
            log.Insert(1, $"{after.Count - shortN} with 5 m or more under the upper slab, {shortN} short");
            Debug.Log("[Round82] A: crossings\n" + string.Join("\n", log));
            try { File.WriteAllLines(Path.Combine(ProjectRoot, GeneratedDir, "round82_crossings.txt"), log); } catch (System.Exception e) { Debug.LogWarning("[Round82] " + e.Message); }
        }
    }
}
