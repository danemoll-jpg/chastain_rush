using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Round 62 (Part C, Dan: BUG-001 x 617.23 z 9.33 Big Shanty Road NW "level all thesse bumps", BUG-002 x 512.91 z 18.66 I-575 "flatten bumps"; Dan keeps
// finding these on the generated roads, so Round 61 D's levelling runs on ALL the map extension's roads of class motorway, trunk, primary, secondary and
// tertiary and their ramps (links): I-75, I-575, Big Shanty, Barrett, Chastain Road, Chastain Meadows, Bells Ferry, US 41, Town Center Drive, Kennesaw
// Mountain Drive (unclassified: by name, as Round 61) and the rest, the new race's roads among them). In the planning, right after the terrain is made,
// in place of Round 61's five roads (MapBuilder.Round61.cs: LevelAlong, the same blur and blend). Dan's two spots lie just inside the old core's south
// edge (the seam with stage 1, which Round 61 left alone): the core's main roads within 120 m of its south edge west of x 1400, and of its west edge, are
// levelled too (no race runs within 250 m of either: checked when this was written), fading out over 30 m into the core. Never within 15 m of the end
// of a bridge the road itself runs onto (fading in over 25 m), as Round 61; a road passing UNDER a bridge is levelled there too (Big Shanty under I-575:
// the decks' ends where Round 59 cut them at the old core's edge lie right over it). Before / after, the sharpest step (second difference over 1 m) and
// the count over 0.10 m per road, and Dan's two spots: in the build log and Assets/Generated/round62_levelling.txt.
// R62SeamOverpasses (after SmoothBridgeEnds): Dan's BUG-002 is I-575's southbound deck over Big Shanty Road. Each of I-575's three carriageways there is a
// deck cut in two at the old core's edge (Round 59) and joined again as a straight deck between the ground at its outer ends (Round 27's JoinChainedDecks):
// 6 % down off a road coming in at 1.3 %, a crest that threw a car at 25 m/s 0.4 s into the air (Round 27's own log: held to 8 m/s). Each is laid as one
// smooth profile: the road 150 m before, the deck, the road 150 m after, blurred along (a Gaussian of 14 m), fading back to the road as it was over the
// outer 40 m; the decks take its heights, the roads are graded to it across their width (easing back over 4 m beside them).
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        static readonly HashSet<string> R62Classes = new HashSet<string> { "motorway", "trunk", "primary", "secondary", "tertiary", "motorway_link", "trunk_link", "primary_link", "secondary_link", "tertiary_link" };
        const float R62SeamBand = 120f, R62SeamFade = 30f, R62SeamSouthToX = 1400f;

        static void R62LevelMains(MapData map, TerrainGrid terrain)
        {
            int first = map.extension != null && map.extension.first != null ? map.extension.first.roads : map.roads.Length;
            float W(Vector2 p)   // the core's part: only near its seams with the extension
            {
                bool inCore = p.x >= 0f && p.y >= 0f && p.x <= map.size_x_m && p.y <= map.size_z_m;
                if (!inCore) return 1f;
                float south = p.x < R62SeamSouthToX ? Smooth01((R62SeamBand - p.y) / R62SeamFade) * Smooth01((R62SeamSouthToX - p.x) / R62SeamFade) : 0f;
                float west = Smooth01((R62SeamBand - p.x) / R62SeamFade);
                return Mathf.Max(south, west);
            }
            bool Main(Road r) => R62Classes.Contains(r.@class) || (!string.IsNullOrEmpty(r.name) && R61Mains.Any(n => r.name.StartsWith(n)));
            var pick = new List<int>();
            for (int i = 0; i < map.roads.Length; i++)
            {
                var r = map.roads[i]; if (r.bridge || r.tunnel || !Main(r) || r.points == null || r.points.Length < 4) continue;
                if (i < first && !ToPoints(r.points).Any(q => W(q) > 0f)) continue;   // a core road away from the seams
                pick.Add(i);
            }
            var deckEnds = new List<Vector2>();
            foreach (var b in map.bridges) { var q = ToPoints(b.points); if (q.Count > 1) { deckEnds.Add(q[0]); deckEnds.Add(q[q.Count - 1]); } }
            foreach (var r in map.roads) if (r.bridge) { var q = ToPoints(r.points); if (q.Count > 1) { deckEnds.Add(q[0]); deckEnds.Add(q[q.Count - 1]); } }
            // chains: pieces of the same road joined where exactly two of them meet (Round 61's rule)
            Vector2Int Key(Vector2 p) => new Vector2Int(Mathf.RoundToInt(p.x * 2f), Mathf.RoundToInt(p.y * 2f));
            var ends = new Dictionary<Vector2Int, List<(int piece, bool start)>>();
            foreach (int i in pick)
            {
                var q = ToPoints(map.roads[i].points);
                foreach (var (p, s) in new[] { (q[0], true), (q[q.Count - 1], false) }) { var k = Key(p); if (!ends.TryGetValue(k, out var l)) ends[k] = l = new List<(int, bool)>(); l.Add((i, s)); }
            }
            var used = new HashSet<int>(); var chains = new List<(string name, List<Vector2> pts, float half, List<Vector2> decks)>();
            foreach (int i in pick)
            {
                if (used.Contains(i)) continue; used.Add(i);
                var line = ToPoints(map.roads[i].points); string name = map.roads[i].name; float half = map.roads[i].width * .5f;
                var mine = new List<Vector2>();   // the deck ends this road runs onto (its pieces' ends within 3 m of one)
                void Ends(List<Vector2> q) { foreach (var e in deckEnds) if ((e - q[0]).sqrMagnitude < 9f || (e - q[q.Count - 1]).sqrMagnitude < 9f) mine.Add(e); }
                Ends(line);
                for (int dir = 0; dir < 2; dir++)
                {
                    for (int guard = 0; guard < 400; guard++)
                    {
                        var tail = line[line.Count - 1];
                        if (!ends.TryGetValue(Key(tail), out var at) || at.Count != 2) break;
                        int k = at.FindIndex(e => !used.Contains(e.piece)); if (k < 0) break;
                        var o = at[k]; if (map.roads[o.piece].name != name || map.roads[o.piece].@class != map.roads[i].@class) break;
                        used.Add(o.piece); var q = ToPoints(map.roads[o.piece].points); if (!o.start) q.Reverse(); Ends(q);
                        line.AddRange(q.Skip(1)); half = Mathf.Max(half, map.roads[o.piece].width * .5f);
                    }
                    line.Reverse();
                }
                chains.Add((string.IsNullOrEmpty(name) ? "(unnamed " + map.roads[i].@class + ")" : name, line, half, mine));
            }
            // Dan's two spots before (the sharpest 1 m step within 25 m)
            var spots = new[] { ("BUG-001 Big Shanty Road", new Vector2(617.23f, 9.33f)), ("BUG-002 I-575", new Vector2(512.91f, 18.66f)) };
            float Worst(Vector2 at)
            {
                float worst = 0f;
                for (float dx = -25f; dx <= 25f; dx += 1f) for (float dz = -25f; dz <= 25f; dz += 1f)
                {
                    float x = at.x + dx, z = at.y + dz, h = terrain.Height(x, z);
                    worst = Mathf.Max(worst, Mathf.Abs(terrain.Height(x - 1f, z) - 2f * h + terrain.Height(x + 1f, z)), Mathf.Abs(terrain.Height(x, z - 1f) - 2f * h + terrain.Height(x, z + 1f)));
                }
                return worst;
            }
            var spotBefore = spots.Select(s => Worst(s.Item2)).ToArray();
            var report = new Dictionary<string, (float before, float after, int b10, int a10, float len)>();
            int levelled = 0;
            foreach (var (name, line0, half, decks) in chains)
            {
                var p = Resample(line0, 1f); int n = p.Count; if (n < 20) continue;
                var raw = new float[n]; for (int i = 0; i < n; i++) raw[i] = terrain.Height(p[i].x, p[i].y);
                var w = new float[n];
                float DeckDist(Vector2 q) { float best = float.MaxValue; foreach (var e in decks) best = Mathf.Min(best, (e - q).sqrMagnitude); return Mathf.Sqrt(best); }
                for (int i = 0; i < n; i++) w[i] = Mathf.Min(Smooth01((DeckDist(p[i]) - 15f) / 25f), W(p[i]));
                if (w.All(v => v <= 0f)) continue;
                float before = SharpestStep(terrain, p, float.MaxValue); int b10 = Steps(terrain, p, .1f);
                // near Dan's two spots (150 m): no crest that lifts a car off the road at 30 m/s (build 2's check: Big Shanty's dip under I-575 still threw it 0.5 s)
                var cap = new bool[n]; bool any = false; for (int i = 0; i < n; i++) { cap[i] = spots.Any(s => (p[i] - s.Item2).magnitude < 150f); any |= cap[i]; }
                LevelAlong(terrain, p, raw, w, half, R61Sigma, R61Shoulder, R61Blend, any ? cap : null, 9.81f / (30f * 30f));
                float after = SharpestStep(terrain, p, float.MaxValue); int a10 = Steps(terrain, p, .1f);
                report.TryGetValue(name, out var o0); levelled++;
                report[name] = (Mathf.Max(o0.before, before), Mathf.Max(o0.after, after), o0.b10 + b10, o0.a10 + a10, o0.len + (n - 1));
            }
            var lines = new List<string> { "Round 62 (C): the main roads levelled across their width (MapBuilder.Round62.cs). Per road name: length levelled, the sharpest 1 m step (second difference) before -> after, steps over 0.10 m before -> after" };
            foreach (var kv in report.OrderByDescending(k => k.Value.before))
                lines.Add($"{kv.Key}\t{kv.Value.len:0} m\t{kv.Value.before:0.000} -> {kv.Value.after:0.000} m\t{kv.Value.b10} -> {kv.Value.a10}");
            for (int i = 0; i < spots.Length; i++) lines.Add($"{spots[i].Item1} ({spots[i].Item2.x:0.00}, {spots[i].Item2.y:0.00}): the sharpest 1 m step within 25 m {spotBefore[i]:0.000} -> {Worst(spots[i].Item2):0.000} m");
            Debug.Log($"[Round62] C: {levelled} chains of {pick.Count} main road pieces levelled ({report.Values.Sum(v => v.len) / 1000f:0.0} km); " +
                      $"sharpest step over all {report.Values.Max(v => v.before):0.000} -> {report.Values.Max(v => v.after):0.000} m; steps over 0.10 m {report.Values.Sum(v => v.b10)} -> {report.Values.Sum(v => v.a10)}; " +
                      string.Join("; ", lines.Skip(lines.Count - spots.Length)));
            try { File.WriteAllLines(Path.Combine(ProjectRoot, GeneratedDir, "round62_levelling.txt"), lines); } catch (System.Exception e) { Debug.LogWarning("[Round62] " + e.Message); }
        }
    
        // ------------------------------------------------------------ I-575's three carriageways over Big Shanty Road, across the old core's edge
        static void R62SeamOverpasses(MapData map, TerrainGrid terrain)
        {
            int firstB = map.extension != null && map.extension.first != null ? map.extension.first.bridges : map.bridges.Length;
            bool Seam(Vector2 e) => (Mathf.Abs(e.y) < .3f && e.x >= 0f && e.x <= map.size_x_m) || (Mathf.Abs(e.x) < .3f && e.y >= 0f && e.y <= map.size_z_m);
            var done = new HashSet<int>(); var chains = new List<(string label, List<(int b, int i, Vector2 p)> at, Bridge b0)>();
            for (int bi = 0; bi < firstB; bi++)
            {
                if (done.Contains(bi)) continue; var b0 = map.bridges[bi]; var p0 = ToPoints(b0.points); if (p0.Count < 2) continue;
                // the piece across the seam: an end of this core piece on the edge, an extension piece of the same road starting there
                int ci = Seam(p0[0]) ? 0 : Seam(p0[p0.Count - 1]) ? p0.Count - 1 : -1; if (ci < 0) continue;
                int bj = -1; bool bjStart = true;
                for (int k = firstB; k < map.bridges.Length && bj < 0; k++)
                {
                    if (map.bridges[k].name != b0.name) continue; var q = ToPoints(map.bridges[k].points);
                    if ((q[0] - p0[ci]).magnitude < 1f) { bj = k; bjStart = true; } else if ((q[q.Count - 1] - p0[ci]).magnitude < 1f) { bj = k; bjStart = false; }
                }
                if (bj < 0) continue; done.Add(bi); done.Add(bj);
                // the deck chain from the core piece's outer end, over the seam, to the extension piece's outer end: (bridge, point index) per point
                var at = new List<(int b, int i, Vector2 p)>();
                for (int k = 0; k < p0.Count; k++) { int i = ci == 0 ? p0.Count - 1 - k : k; at.Add((bi, i, p0[i])); }
                var p1 = ToPoints(map.bridges[bj].points);
                for (int k = 1; k < p1.Count; k++) { int i = bjStart ? k : p1.Count - 1 - k; at.Add((bj, i, p1[i])); }
                chains.Add(($"over the old core's edge (bridges {bi + 1} and {bj + 1})", at, b0));
            }
            // and Bells Ferry Road's bridge over Noonday Creek, by the Noonday Creek Trail race's start (its check 1: off its north end at 25 m/s, 1.3 s in the air,
            // the car came down over its side; Round 27's log: held to 7 m/s): the same, one curve with its roads
            for (int bi = 0; bi < map.bridges.Length; bi++)
            {
                var pb = ToPoints(map.bridges[bi].points); if (map.bridges[bi].name != "Bells Ferry Road Northeast" || pb.Count < 2 || !pb.Any(q => (q - new Vector2(1718f, -643f)).magnitude < 12f) || done.Contains(bi)) continue;
                var at1 = new List<(int b, int i, Vector2 p)>();   // (OSM way 320582950)
                for (int i = 0; i < pb.Count; i++) at1.Add((bi, i, pb[i]));
                chains.Add(($"over Noonday Creek (bridge {bi + 1})", at1, map.bridges[bi])); done.Add(bi);
            }
            foreach (var (label, at, b0) in chains)
            {
                // the roads on the ground beyond both outer ends, 150 m each
                List<Vector2> Beyond(Vector2 E, Vector2 outward)
                {
                    foreach (var r in map.roads)
                    {
                        var q = ToPoints(r.points); if (q.Count < 2 || r.bridge) continue;
                        if (Vector2.Distance(q[0], E) < 2f && Vector2.Dot(q[1] - q[0], outward) > 0f) { q[0] = E; return ExtendPath(map, q, 150f); }
                        if (Vector2.Distance(q[q.Count - 1], E) < 2f && Vector2.Dot(q[q.Count - 2] - q[q.Count - 1], outward) > 0f) { q.Reverse(); q[0] = E; return ExtendPath(map, q, 150f); }
                    }
                    return null;
                }
                var A = Beyond(at[0].p, (at[0].p - at[1].p).normalized); var Z = Beyond(at[at.Count - 1].p, (at[at.Count - 1].p - at[at.Count - 2].p).normalized);
                // 150 m of each (a road's first piece can run on for kilometers: build 2 laid 1.5 km of I-575 with it)
                List<Vector2> Cut150(List<Vector2> l) { if (l == null) return null; var o = new List<Vector2> { l[0] }; float acc = 0f; for (int i = 1; i < l.Count && acc < 150f; i++) { float d = (l[i] - l[i - 1]).magnitude; if (acc + d > 150f) { o.Add(Vector2.Lerp(l[i - 1], l[i], (150f - acc) / d)); acc = 150f; break; } o.Add(l[i]); acc += d; } return o; }
                A = Cut150(A); Z = Cut150(Z);
                if (A == null || Z == null) { Debug.LogWarning($"[Round62] the {b0.name} deck over the old core's edge: no road on the ground at {(A == null ? "its start" : "its end")}, left as it was"); continue; }
                // one line: road A (toward the deck), the deck, road Z, at about 1 m; each point's kind (-1 road A, 0 deck, 1 road Z) and its height now
                var pts = new List<Vector2>(); var kind = new List<int>(); var deckK = new List<int>();
                var ra = Resample(Enumerable.Reverse(A).ToList(), 1f); float La = 0f; for (int i = 1; i < ra.Count; i++) La += (ra[i] - ra[i - 1]).magnitude;
                foreach (var q in ra.Take(ra.Count - 1)) { pts.Add(q); kind.Add(-1); deckK.Add(-1); }
                for (int k = 0; k < at.Count; k++) { pts.Add(at[k].p); kind.Add(0); deckK.Add(k); }
                var rz = Resample(Z, 1f); foreach (var q in rz.Skip(1)) { pts.Add(q); kind.Add(1); deckK.Add(-1); }
                float lift = (RoadLift.TryGetValue(b0.@class, out var lf) ? lf : .06f) * vScale;
                var cum = new float[pts.Count]; for (int i = 1; i < pts.Count; i++) cum[i] = cum[i - 1] + (pts[i] - pts[i - 1]).magnitude;
                var raw = new float[pts.Count];
                for (int i = 0; i < pts.Count; i++) raw[i] = kind[i] == 0 ? map.bridges[at[deckK[i]].b].deck_y[at[deckK[i]].i] + lift : terrain.Height(pts[i].x, pts[i].y);
                float total = cum[cum.Length - 1], sA = cum[ra.Count - 1], sZ = cum[ra.Count - 1 + at.Count - 1];
                const float Sigma = 14f, Fade = 40f; var target = new float[pts.Count];
                for (int i = 0; i < pts.Count; i++)
                {
                    float sw = 0f, sy = 0f;
                    for (int j = 0; j < pts.Count; j++) { float d = cum[j] - cum[i]; if (Mathf.Abs(d) > 3f * Sigma) continue; float k = Mathf.Exp(-.5f * d * d / (Sigma * Sigma)); sw += k; sy += k * raw[j]; }
                    float w = Smooth01(Mathf.Min(cum[i], total - cum[i]) / Fade);
                    target[i] = raw[i] + w * (sy / sw - raw[i]);
                }
                float Kink(float[] y) { float worst = 0f; for (int i = 1; i + 1 < pts.Count; i++) { float d0 = Mathf.Max(.2f, cum[i] - cum[i - 1]), d1 = Mathf.Max(.2f, cum[i + 1] - cum[i]); worst = Mathf.Max(worst, Mathf.Abs((y[i + 1] - y[i]) / d1 - (y[i] - y[i - 1]) / d0)); } return worst; }
                float before = Kink(raw), after = Kink(target);
                // the decks take it
                for (int i = 0; i < pts.Count; i++) if (kind[i] == 0) map.bridges[at[deckK[i]].b].deck_y[at[deckK[i]].i] = target[i] - lift;
                // the roads graded to it across their width (not between the deck's ends), easing back over 4 m beside them
                float half = b0.width * .5f + .5f, bank = 4f; int changed = 0;
                var idx = new CellIndex(16f); for (int i = 0; i + 1 < pts.Count; i++) idx.Add(pts[i], pts[i + 1], half + bank + .01f);   // (every segment: its id is i)
                var near = new List<int>(); var E0 = at[0].p; var o0 = (at[0].p - at[1].p).normalized; var E1 = at[at.Count - 1].p; var o1 = (at[at.Count - 1].p - at[at.Count - 2].p).normalized;
                var lo = pts.Aggregate(new Vector2(float.MaxValue, float.MaxValue), (a, q) => Vector2.Min(a, q)) - Vector2.one * (half + bank); var hi = pts.Aggregate(new Vector2(float.MinValue, float.MinValue), (a, q) => Vector2.Max(a, q)) + Vector2.one * (half + bank);
                EditTerrain(terrain, Rect.MinMaxRect(lo.x, lo.y, hi.x, hi.y), (x, z, y) =>
                {
                    var p = new Vector2(x, z); if (Vector2.Dot(p - E0, o0) < .05f && Vector2.Dot(p - E1, o1) < .05f) return y;   // between the deck's ends: under the deck, left
                    float best = float.MaxValue, tg = y;
                    foreach (int i in idx.Near(p, 0f, near))
                    {
                        if (kind[i] == 0 && kind[i + 1] == 0) continue;
                        var ab = pts[i + 1] - pts[i]; float t = Mathf.Clamp01(Vector2.Dot(p - pts[i], ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f)); float d = (pts[i] + ab * t - p).sqrMagnitude;
                        if (d < best) { best = d; tg = Mathf.Lerp(target[i], target[i + 1], t) - lift; }
                    }
                    float dd = Mathf.Sqrt(best); if (dd > half + bank) return y;
                    float w = dd <= half ? 1f : Smooth01(1f - (dd - half) / bank); float ny = y + w * (tg - y);
                    if (Mathf.Abs(ny - y) > 1e-3f) changed++; return ny;
                });
                // the clearance over the ground under it (Big Shanty Road)
                float clear = float.MaxValue; for (int i = 0; i < pts.Count; i++) if (kind[i] == 0) clear = Mathf.Min(clear, target[i] - lift - .6f - terrain.Height(pts[i].x, pts[i].y));
                Debug.Log($"[Round62] C: the {b0.name} deck {label} ({sZ - sA:0} m) laid with the road {La:0} m before and {total - sZ:0} m after as one curve: " +
                          $"the sharpest change of grade from one meter to the next {before * 100f:0.0} -> {after * 100f:0.0} %, the deck {at.Min(a => map.bridges[a.b].deck_y[a.i]):0.00} to {at.Max(a => map.bridges[a.b].deck_y[a.i]):0.00} m, {changed} road samples; the slab's underside at least {clear:0.0} m over the ground under it");
            }
        }
    }
}
