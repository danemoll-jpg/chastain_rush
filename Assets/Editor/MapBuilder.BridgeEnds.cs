using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Round 27 (A2, Dan: "the drop at the west end of the New Chastain bridge", x 2421, z 766). A bridge's deck is its own mesh at
// mapgen's deck heights; the road beyond it lies on the terrain. At New Chastain Road's west end the deck stopped 0.2 m over the
// ground and the creek bank fell away under and past it, so a fast vehicle left the deck with its wheels off the ground (3 to 5 m/s
// lost, the Needle 600 at 56 m/s). Now, at every bridge end on the map that meets a road on the ground (not one bridge piece running
// into the next, not the map's edge): the road's ground is graded up to the deck's end, carrying on at the deck's own grade and easing
// back to the road's profile over BridgeEase m (an approach slab of ground: no step, and no crest sharper than a 56 m/s car can hold:
// v^2 * curvature < g), the bank under the deck's end is filled to just under its slab, and an abutment wall stands under the end.
// The road's banks fade back to the natural ground over 3.5 m beside it. Before / after per end in the build log.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        const float BridgeEase = 34f, BridgeHoldV = 60f;   // m of road eased; the speed (m/s) the profile must hold wheels-down
        sealed class BridgeEnd { public int bridge; public bool start; public Vector2 E, outward; public float deckTop, slabBottom, half, ground; }
        static readonly List<BridgeEnd> BridgeEnds = new List<BridgeEnd>();

        static void SmoothBridgeEnds(MapData map, TerrainGrid terrain)
        {
            BridgeEnds.Clear();
            const float slab = 0.7f, g = 9.81f;
            JoinChainedDecks(map, terrain);
            for (int bi = 0; bi < map.bridges.Length; bi++)
            {
                var b = map.bridges[bi]; var pts = ToPoints(b.points); int n = pts.Count;
                if (n < 2) continue;
                float lift = (RoadLift.TryGetValue(b.@class, out var l) ? l : 0.06f) * vScale;
                foreach (bool start in new[] { true, false })
                {
                    int ie = start ? 0 : n - 1, ip = start ? 1 : n - 2;
                    var E = pts[ie]; var outward = (E - pts[ip]).normalized;
                    string tag = $"bridge {bi + 1} ({b.name}) {(start ? "start" : "end")} at x {E.x:0} z {E.y:0}";
                    if (EdgeCut(b, map)) { Debug.Log($"[Round27] {tag}: a bridge cut by the map's edge (the I-575 ramps), left as it was"); continue; }
                    bool chained = false;
                    for (int bj = 0; bj < map.bridges.Length && !chained; bj++)
                    {
                        if (bj == bi) continue; var o = map.bridges[bj].points; int m = o.Length / 2;
                        if (Vector2.Distance(E, new Vector2(o[0], o[1])) < 3f || Vector2.Distance(E, new Vector2(o[2 * m - 2], o[2 * m - 1])) < 3f) chained = true;
                    }
                    if (chained) { Debug.Log($"[Round27] {tag}: runs into the next bridge piece, left"); continue; }
                    // the road on the ground that carries on from this end
                    List<Vector2> path = null;
                    foreach (var r in map.roads)
                    {
                        var q = ToPoints(r.points); if (q.Count < 2) continue;
                        if (Vector2.Distance(q[0], E) < 2f && Vector2.Dot(q[1] - q[0], outward) > 0f) { path = q; break; }
                        if (Vector2.Distance(q[q.Count - 1], E) < 2f && Vector2.Dot(q[q.Count - 2] - q[q.Count - 1], outward) > 0f) { q.Reverse(); path = q; break; }
                    }
                    if (path == null) { Debug.Log($"[Round27] {tag}: no road on the ground carries on from it, left"); continue; }
                    path[0] = E;
                    var cum = new float[path.Count];
                    for (int i = 1; i < path.Count; i++) cum[i] = cum[i - 1] + (path[i] - path[i - 1]).magnitude;
                    Vector2 At(float s)
                    {
                        if (s <= 0f) return E + outward * s;
                        for (int i = 1; i < path.Count; i++) if (s <= cum[i]) return Vector2.Lerp(path[i - 1], path[i], (s - cum[i - 1]) / Mathf.Max(cum[i] - cum[i - 1], 1e-4f));
                        return path[path.Count - 1];
                    }
                    // the deck's grade over its last 8 m or more (its very last point can kink: New Chastain's east end rises 5.6 % over its
                    // last 1.5 m, and carrying that on made a hump)
                    // a kink in the deck's very last segment (New Chastain's south deck rises 8.5 cm over its last 1.5 m, 5.6 %, where the
                    // rest runs at 0.6 %) is straightened to the grade before it
                    if (n >= 3)
                    {
                        int iq = start ? 2 : n - 3;
                        float g1 = (b.deck_y[ie] - b.deck_y[ip]) / Mathf.Max(Vector2.Distance(pts[ie], pts[ip]), 0.1f), g0 = (b.deck_y[ip] - b.deck_y[iq]) / Mathf.Max(Vector2.Distance(pts[ip], pts[iq]), 0.1f);
                        if (Mathf.Abs(g1 - g0) > 0.01f && Mathf.Abs(g0) < 0.15f && Mathf.Abs(g1) < 0.15f) {   // not the I-575 ramps cut by the map's edge (45 to 92 %)
                         float was = b.deck_y[ie]; b.deck_y[ie] = b.deck_y[ip] + g0 * Vector2.Distance(pts[ie], pts[ip]); Debug.Log($"[Round27] {tag}: the deck's last segment straightened ({g1 * 100f:0.0} % to {g0 * 100f:0.0} %, its end {was:0.000} to {b.deck_y[ie]:0.000})"); }
                    }
                    // the deck's own top s m back from the end (s < 0), along its points
                    float DeckAt(float s)
                    {
                        float acc = 0f; int a = ie;
                        while (start ? a + 1 < n : a - 1 >= 0)
                        {
                            int c = start ? a + 1 : a - 1; float seg = Vector2.Distance(pts[a], pts[c]);
                            if (acc + seg >= -s) return Mathf.Lerp(b.deck_y[a], b.deck_y[c], (-s - acc) / Mathf.Max(seg, 1e-4f)) + lift;
                            acc += seg; a = c;
                        }
                        return b.deck_y[a] + lift;
                    }
                    int ig = ip; float span = Vector2.Distance(pts[ie], pts[ip]);
                    while (span < 8f && (start ? ig + 1 < n : ig - 1 >= 0)) { int nx = start ? ig + 1 : ig - 1; span += Vector2.Distance(pts[ig], pts[nx]); ig = nx; }
                    float deckTop = b.deck_y[ie] + lift, grade = (b.deck_y[ie] - b.deck_y[ig]) / Mathf.Max(span, 0.1f);
                    // the ground's profile along the road now, every 0.5 m, as far as the longest easing could reach
                    float Lmax = Mathf.Min(90f, cum[cum.Length - 1]);
                    int NM = Mathf.CeilToInt(Lmax / 0.5f) + 1;
                    var GM = new float[NM]; for (int i = 0; i < NM; i++) { var p = At(i * 0.5f); GM[i] = terrain.Height(p.x, p.y); }
                    // a smooth curve (cubic Hermite) from the deck's end at the deck's grade to the road at L at the road's own grade there:
                    // no step, no kink. L is the shortest from BridgeEase to 90 m whose sharpest crest a car holds at BridgeHoldV (carrying
                    // the deck's grade on and blending, or a fixed 34 m, left a crest where the deck's grade and the road's differ: Chastain
                    // Road's joined decks fall 2.1 to 2.3 % from ends where the road is flat)
                    float y0h = deckTop - 0.01f, L = BridgeEase, yL = 0f, gL = 0f;
                    float H(float s, float len, float yl, float gl)
                    {
                        float t = Mathf.Clamp01(s / len), t2 = t * t, t3 = t2 * t;
                        return (2f * t3 - 3f * t2 + 1f) * y0h + (t3 - 2f * t2 + t) * len * grade + (-2f * t3 + 3f * t2) * yl + (t3 - t2) * len * gl;
                    }
                    for (float ll = BridgeEase; ; ll += 4f)
                    {
                        ll = Mathf.Min(ll, Lmax); int kL = Mathf.Min(NM - 1, Mathf.RoundToInt(ll / 0.5f)), kB = Mathf.Max(0, kL - 20);
                        float yl = GM[kL], gl = (GM[kL] - GM[kB]) / Mathf.Max((kL - kB) * 0.5f, 0.5f), crest = 0f;
                        for (float q = 2f; q <= ll - 2f; q += 1f) crest = Mathf.Max(crest, -(H(q - 2f, ll, yl, gl) - 2f * H(q, ll, yl, gl) + H(q + 2f, ll, yl, gl)) / 4f);
                        L = ll; yL = yl; gL = gl;
                        if (crest <= g / (BridgeHoldV * BridgeHoldV) || ll >= Lmax) break;
                    }
                    int N = Mathf.CeilToInt(L / 0.5f) + 1;
                    var G = GM;
                    float Target(float s) => s >= L ? GM[Mathf.Min(NM - 1, Mathf.RoundToInt(L / 0.5f))] : H(s, L, yL, gL);
                    // how the surface runs off the deck: the step at the end, the sharpest crest (the speed it can be held at)
                    (float step, float vHold) Measure(System.Func<float, float> y)
                    {
                        float kMax = 0f;
                        for (float s = -2f; s <= L - 2f; s += 1f)
                        {
                            float a = s < 0f ? deckTop + grade * s : y(s), m = s + 1f < 0f ? deckTop + grade * (s + 1f) : y(s + 1f), c = y(s + 2f);
                            kMax = Mathf.Max(kMax, -(a - 2f * m + c));   // crest: the grade falls
                        }
                        return (deckTop - y(0f), kMax > 1e-5f ? Mathf.Sqrt(g / kMax) : 999f);
                    }
                    var before = Measure(s => G[Mathf.Clamp(Mathf.RoundToInt(s / 0.5f), 0, N - 1)]);
                    var end = new BridgeEnd { bridge = bi, start = start, E = E, outward = outward, deckTop = deckTop, slabBottom = deckTop - slab, half = b.width * 0.5f };
                    BridgeEnds.Add(end);
                    if (Mathf.Abs(before.step) < 0.05f && before.vHold > BridgeHoldV) { end.ground = G[0]; Debug.Log($"[Round27] {tag}: step {before.step:0.00} m, held to {before.vHold:0} m/s: fine, left"); continue; }
                    float half = b.width * 0.5f, bank = 3.5f, reach = half + bank;
                    float x0 = float.MaxValue, x1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue;
                    for (float s = -6f; s <= L; s += 1f) { var p = At(s); x0 = Mathf.Min(x0, p.x); x1 = Mathf.Max(x1, p.x); z0 = Mathf.Min(z0, p.y); z1 = Mathf.Max(z1, p.y); }
                    int changed = 0;
                    EditTerrain(terrain, Rect.MinMaxRect(x0 - reach, z0 - reach, x1 + reach, z1 + reach), (x, z, y) =>
                    {
                        var p = new Vector2(x, z);
                        // where along the road (s, behind the end negative, under the deck) and how far off its line
                        float s, d;
                        float sd = Vector2.Dot(p - E, outward);
                        if (sd < 0f) { s = sd; d = Mathf.Abs(Vector2.Dot(p - E, new Vector2(-outward.y, outward.x))); }
                        else
                        {
                            float best = float.MaxValue; s = 0f;
                            for (int i = 0; i + 1 < path.Count && cum[i] <= L + reach; i++)
                            {
                                var ab = path[i + 1] - path[i]; float t = Mathf.Clamp01(Vector2.Dot(p - path[i], ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
                                float dd = (path[i] + ab * t - p).sqrMagnitude; if (dd < best) { best = dd; s = cum[i] + t * ab.magnitude; }
                            }
                            d = Mathf.Sqrt(best);
                        }
                        if (s > L || d > reach) return y;
                        if (s < -6f) return y;
                        if (s < 0f && Beds != null && (Beds.Wet(x, z) || (Beds.NearStream(p, out float sdist, out _) && sdist < StreamHalf + 1.5f))) return y;   // the creek's bed stays open
                        if (s < 0f && d > half + 0.35f)
                        {
                            // beside the deck's end: the road's banks carried back 3 m as wing fill (only filled)
                            float d0 = Target(0f) - G[0]; if (d0 <= 0f || s < -3f) return y;
                            float f0 = 1f - Mathf.SmoothStep(0f, 1f, (d - half - 0.5f) / (bank - 0.5f));
                            float wy = y + d0 * f0 * (1f + s / 3f);
                            if (wy <= y + 1e-3f) return y;
                            changed++; return wy;
                        }
                        if (s < 0f)
                        {
                            // under the deck: its last 0.7 m 2 cm under its top, so the ground's surface between that and the road's first
                            // samples runs flush off the end (5 cm there left a 1 to 3 cm dip just past the end, enough to bounce a bike at
                            // 56 m/s); further in, the bank filled to under its slab and falling away at about 40 degrees, never closer than
                            // 5 cm under the deck
                            if (d > half + 0.35f) return y;
                            float deckHere = DeckAt(s), under;
                            if (s > -0.7f) under = deckHere - 0.02f;
                            else under = Mathf.Min(Mathf.Max(y, slabBottom(s) - 0.1f + (s + 0.7f) * 0.85f), deckHere - 0.05f);
                            if (Mathf.Abs(under - y) < 1e-3f) return y;
                            changed++; return under;
                        }
                        int k = Mathf.Clamp(Mathf.RoundToInt(s / 0.5f), 0, N - 1);
                        float delta = Target(s) - G[k];
                        float fade = d <= half + 0.5f ? 1f : 1f - Mathf.SmoothStep(0f, 1f, (d - half - 0.5f) / (bank - 0.5f));
                        float ny = y + delta * fade;
                        if (Mathf.Abs(ny - y) > 1e-3f) changed++;
                        return ny;
                    });
                    float slabBottom(float s) => deckTop + grade * s - slab;
                    var after = Measure(s => { var p = At(s); return terrain.Height(p.x, p.y); });
                    end.ground = terrain.Height(E.x, E.y);
                    Debug.Log($"[Round27] {tag}: before step {before.step:0.00} m, held to {before.vHold:0} m/s; after step {after.step:0.00} m, held to {after.vHold:0} m/s ({changed} terrain samples over {L:0} m)");
                }
            }
        }

        // Round 27 (A2): OSM splits a bridge into pieces (Chastain Road over I-575: two per carriageway) and mapgen gives each piece a
        // straight deck between the ground at its own two ends, so where two pieces meet over the highway the deck came down to the
        // highway's level: a V 12 m deep, at up to 50 % (the Needle 600 flew 2 s off it and crashed). Pieces that run end to end are one
        // deck here: a straight line between the road's heights at the chain's outer ends, lifted only where the ground would come within 0.15 m of it.
        static void JoinChainedDecks(MapData map, TerrainGrid terrain)
        {
            var used = new HashSet<int>();
            for (int bi = 0; bi < map.bridges.Length; bi++)
            {
                if (used.Contains(bi)) continue;
                // the chain through bi: pieces whose ends meet (within 3 m), in order
                var chain = new List<(int b, bool rev)> { (bi, false) }; used.Add(bi);
                for (bool grew = true; grew;)
                {
                    grew = false;
                    var head = Ends(map.bridges[chain[0].b], chain[0].rev).a; var tail = Ends(map.bridges[chain[chain.Count - 1].b], chain[chain.Count - 1].rev).z;
                    for (int bj = 0; bj < map.bridges.Length && !grew; bj++)
                    {
                        if (used.Contains(bj)) continue;
                        var (a, z) = Ends(map.bridges[bj], false);
                        if (Vector2.Distance(a, tail) < 3f) { chain.Add((bj, false)); used.Add(bj); grew = true; }
                        else if (Vector2.Distance(z, tail) < 3f) { chain.Add((bj, true)); used.Add(bj); grew = true; }
                        else if (Vector2.Distance(z, head) < 3f) { chain.Insert(0, (bj, false)); used.Add(bj); grew = true; }
                        else if (Vector2.Distance(a, head) < 3f) { chain.Insert(0, (bj, true)); used.Add(bj); grew = true; }
                    }
                }
                if (chain.Count < 2) continue;
                // every deck point along the chain, its distance along, and where it lives
                var at = new List<(int b, int i, Vector2 p)>();
                foreach (var (b, rev) in chain)
                {
                    var pts = ToPoints(map.bridges[b].points);
                    for (int k = 0; k < pts.Count; k++) { int i = rev ? pts.Count - 1 - k : k; at.Add((b, i, pts[i])); }
                }
                var s = new float[at.Count]; for (int k = 1; k < at.Count; k++) s[k] = s[k - 1] + Vector2.Distance(at[k - 1].p, at[k].p);
                // from the road's own height at each outer end (mapgen lifted the ends 0.2 to 0.4 m over it, and a deck rising to an end
                // that stands over the road is a crest there however the road is eased)
                float y0 = terrain.Height(at[0].p.x, at[0].p.y), y1 = terrain.Height(at[at.Count - 1].p.x, at[at.Count - 1].p.y);
                float deepest = 0f;
                var line = new List<Vector3>();
                // Round 80 (D1): Express Lanes 13's chain keeps its own (smoothed) shape, its ends moved onto the road's heights (ExpressDecks)
                bool kept = ExpressKept.Contains(map.bridges[at[0].b]);
                float k0 = kept ? y0 - map.bridges[at[0].b].deck_y[at[0].i] : 0f, k1 = kept ? y1 - map.bridges[at[at.Count - 1].b].deck_y[at[at.Count - 1].i] : 0f;
                for (int k = 0; k < at.Count; k++)
                {
                    float y = kept ? map.bridges[at[k].b].deck_y[at[k].i] + Mathf.Lerp(k0, k1, s[k] / Mathf.Max(s[at.Count - 1], 1e-3f)) : Mathf.Lerp(y0, y1, s[k] / Mathf.Max(s[at.Count - 1], 1e-3f));
                    var dy = map.bridges[at[k].b].deck_y; deepest = Mathf.Max(deepest, y - dy[at[k].i]); dy[at[k].i] = y;
                    line.Add(new Vector3(at[k].p.x, y, at[k].p.y));
                }
                // ground that would come within 0.1 m of the straight deck is cut down under it (hidden; a lift there made a bump)
                float half = map.bridges[bi].width * 0.5f + 0.4f, cutMost = 0f; int cutN = 0;
                float x0 = line.Min(q => q.x) - half, x1 = line.Max(q => q.x) + half, z0 = line.Min(q => q.z) - half, z1 = line.Max(q => q.z) + half;
                // build speed (after Round 61): the deck's segments by cell (a sample further than 'half' from every one is left as it is)
                var deckIdx = new CellIndex(16f); for (int k = 0; k + 1 < line.Count; k++) deckIdx.Add(new Vector2(line[k].x, line[k].z), new Vector2(line[k + 1].x, line[k + 1].z), half + 0.01f);
                var deckNear = new List<int>();
                EditTerrain(terrain, Rect.MinMaxRect(x0, z0, x1, z1), (x, z, yy) =>
                {
                    var p = new Vector2(x, z); float best = float.MaxValue, deckY = 0f;
                    foreach (int k in deckIdx.Near(p, 0f, deckNear))
                    {
                        Vector2 a = new(line[k].x, line[k].z), c = new(line[k + 1].x, line[k + 1].z); var ab = c - a;
                        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f)); float d = (a + ab * t - p).sqrMagnitude;
                        if (d < best) { best = d; deckY = Mathf.Lerp(line[k].y, line[k + 1].y, t); }
                    }
                    if (best > half * half || yy <= deckY - 0.1f) return yy;
                    // not at the outer ends, where the road meets it (SmoothBridgeEnds grades those)
                    if (Vector2.Distance(p, at[0].p) < 2f || Vector2.Distance(p, at[at.Count - 1].p) < 2f) return yy;
                    cutN++; cutMost = Mathf.Max(cutMost, yy - (deckY - 0.1f)); return deckY - 0.1f;
                });
                float lifted = cutMost;
                Debug.Log($"[Round27] bridges {string.Join(" + ", chain.Select(c => (c.b + 1).ToString()))} ({map.bridges[bi].name}, {s[at.Count - 1]:0} m): {(kept ? "its own shape kept (Round 80), its outer ends onto the road" : "one straight deck between its outer ends")} ({y0:0.0} to {y1:0.0}); the deck raised by up to {deepest:0.0} m where the pieces met; the ground cut under it by up to {lifted:0.00} m ({cutN} samples)");
            }
        }
        // a bridge one of whose ends is the map's edge (the I-575 ramps at the south edge: decks of 45 to 92 % where the crop cuts them)
        static bool EdgeCut(Bridge b, MapData map)
        {
            var (a, z) = Ends(b, false);
            var mext = MapLandOf(map); bool Edge(Vector2 p) => mext.Distance(p.x, p.y) < 2f;   // Round 59: the whole map's edge (Round 76: its land's outline)
            return Edge(a) || Edge(z);
        }
        static (Vector2 a, Vector2 z) Ends(Bridge b, bool rev)
        {
            var p = b.points; int n = p.Length / 2;
            Vector2 first = new Vector2(p[0], p[1]), last = new Vector2(p[2 * n - 2], p[2 * n - 1]);
            return rev ? (last, first) : (first, last);
        }

        // the abutment wall under each graded end: concrete from just under the slab down into the bank
        static void BuildAbutments(MeshBuilder body, TerrainGrid terrain)
        {
            foreach (var e in BridgeEnds)
            {
                var n = new Vector2(-e.outward.y, e.outward.x); float w = e.half + 0.35f;
                float lo = float.MaxValue;
                for (float s = -4f; s <= 0f; s += 0.5f) for (float u = -w; u <= w; u += 1f) { var p = e.E + e.outward * s + n * u; lo = Mathf.Min(lo, terrain.Height(p.x, p.y)); }
                float yTop = e.slabBottom - 0.02f, yBot = lo - 0.6f;
                if (yTop - yBot < 0.3f) continue;
                Vector3 P(float s, float u, float y) { var p = e.E + e.outward * s + n * u; return new Vector3(p.x, y, p.y); }
                float s0 = -2.4f, s1 = -1.2f;
                var o3 = new Vector3(e.outward.x, 0f, e.outward.y); var n3 = new Vector3(n.x, 0f, n.y);
                Face(body, P(s0, -w, yBot), P(s0, w, yBot), P(s0, w, yTop), P(s0, -w, yTop), -o3);
                Face(body, P(s1, -w, yBot), P(s1, w, yBot), P(s1, w, yTop), P(s1, -w, yTop), o3);
                Face(body, P(s0, w, yBot), P(s1, w, yBot), P(s1, w, yTop), P(s0, w, yTop), n3);
                Face(body, P(s0, -w, yBot), P(s1, -w, yBot), P(s1, -w, yTop), P(s0, -w, yTop), -n3);
            }
        }
    }
}
