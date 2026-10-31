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
        // the Canton Road Connector's interchange (53, 54, 103), Allgood Road (52), Phillip Landrum by Chastain Road (80), Skip Spann's west deck (30, 39)
        static readonly int[] R88Bridges = { 13, 15, 45, 17, 18, 38, 62, 63, 79, 94, 97, 19, 20, 29, 61, 104, 57, 60, 82, 51, 53, 54, 103, 52, 80, 30, 39 };
        const float R88HoldV = 40f, R88MaxL = 150f, R88MinL = 24f;

        static void R88DeckEnds(MapData map, TerrainGrid terrain)
        {
            var log = new List<string> { "Round 88 (C): the decks' ends at Round 83's leftover places laid onto their roads (MapBuilder.Round88.cs R88DeckEnds). Per end: the step and the sharpest crest / sag before -> after (the speed a car holds wheels down)" };
            const float g = 9.81f, slab = 0.7f;
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
                    if ((a + ab * t - p).magnitude < bb.width * .5f + 1f && Mathf.Lerp(bb.deck_y[i], bb.deck_y[i + 1], t) - slab > y + 2.5f) return true;
                }
                return false;
            }
            var roads = map.roads.Select(r => ToPoints(r.points)).ToList();
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
                        if (best < 0) break;
                        used.Add(best); var w = new List<Vector2>(roads[best]); if (rev) w.Reverse();
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
                    if (n >= 3)
                    {
                        int iq = start ? 2 : n - 3; float dl = Mathf.Max(Vector2.Distance(pts[ie], pts[ip]), .1f);
                        float g1 = (b.deck_y[ie] - b.deck_y[ip]) / dl, g0 = (b.deck_y[ip] - b.deck_y[iq]) / Mathf.Max(Vector2.Distance(pts[ip], pts[iq]), .1f);
                        if (Mathf.Abs(g1 - g0) > .01f && Mathf.Abs(g0) < .15f) { float was = b.deck_y[ie]; b.deck_y[ie] = b.deck_y[ip] + g0 * dl; log.Add($"{tag}: the deck's last segment straightened ({g1 * 100f:0.0} % to {g0 * 100f:0.0} %, its end {was:0.00} to {b.deck_y[ie]:0.00})"); }
                    }
                    // the deck's top and grade over its last 8 m or more
                    int ig = ip; float span = Vector2.Distance(pts[ie], pts[ip]);
                    while (span < 8f && (start ? ig + 1 < n : ig - 1 >= 0)) { int nx = start ? ig + 1 : ig - 1; span += Vector2.Distance(pts[ig], pts[nx]); ig = nx; }
                    float deckTop = b.deck_y[ie] + lift, grade = (b.deck_y[ie] - b.deck_y[ig]) / Mathf.Max(span, .1f);
                    // as far as it may reach: the chained road's length, and short of passing under another deck
                    float Lmax = Mathf.Min(R88MaxL, cum[cum.Length - 1]);
                    for (float s = 2f; s <= Lmax; s += 1f) { var p = At(s); if (UnderOther(p, Mathf.Max(terrain.Height(p.x, p.y), deckTop), bi, chain)) { Lmax = Mathf.Max(0f, s - 6f); break; } }
                    if (Lmax < 12f) { log.Add($"{tag}: the road under another deck within {Lmax + 6f:0} m, left"); continue; }
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
                    if (Mathf.Abs(step0) < .03f && V(before.crest) >= R88HoldV && before.sag < .03f) { log.Add($"{tag}: step {step0:0.00} m, crest held to {V(before.crest):0} m/s: fine, left"); continue; }
                    // the curve: a cubic Hermite from the deck's end (its top, its grade) to the road at L (its height, its grade there over 10 m)
                    float y0 = deckTop - .01f, L = 0f, yL = 0f, gL = 0f, bestK = float.MaxValue;
                    float H(float s, float len2, float yl, float gl)
                    {
                        float t = Mathf.Clamp01(s / len2), t2 = t * t, t3 = t2 * t;
                        return (2f * t3 - 3f * t2 + 1f) * y0 + (t3 - 2f * t2 + t) * len2 * grade + (-2f * t3 + 3f * t2) * yl + (t3 - t2) * len2 * gl;
                    }
                    // the shortest L whose curve a car holds at R88HoldV (its crest) and whose sag is gentle: the curve's own bends, exactly (H'' = 6(1 - 2t)(y1 - y0)/L^2 + ...);
                    // the road's far grade gl over 10 m either side of L (its 1 m grid smoothed out)
                    for (float ll = Mathf.Min(R88MinL, Lmax); ll <= Lmax + .01f; ll += 4f)
                    {
                        float a0 = Mathf.Max(ll - 10f, 0f), a1 = Mathf.Min(ll + 10f, Lmax);
                        float yl = 0f; int cnt = 0; for (float s = a0; s <= a1 + .01f; s += .5f) { yl += Gs(s); cnt++; } yl /= Mathf.Max(cnt, 1);   // its height: the mean about L (the grade line through it)
                        float gl = (Gs(a1) - Gs(a0)) / Mathf.Max(a1 - a0, 1f); yl += gl * (ll - (a0 + a1) * .5f);
                        float cr = 0f, sgm = 0f;
                        for (float s = 0f; s <= ll; s += ll / 40f) { float k2 = (H(s - 1f, ll, yl, gl) - 2f * H(s, ll, yl, gl) + H(s + 1f, ll, yl, gl)); if (s < 1f || s > ll - 1f) continue; cr = Mathf.Max(cr, -k2); sgm = Mathf.Max(sgm, k2); }
                        float k = Mathf.Max(cr, sgm * .4f);
                        if (k < bestK) { bestK = k; L = ll; yL = yl; gL = gl; }
                        if (V(cr) >= R88HoldV && sgm < .02f) { L = ll; yL = yl; gL = gl; break; }
                    }
                    // past L the road eases from the grade line back onto its own ground over 10 m
                    float Target(float s) => s >= L ? Mathf.Lerp(yL + gL * (s - L), Gs(s), Smooth01((s - L) / 10f)) : H(s, L, yL, gL);
                    float Lend = Mathf.Min(L + 10f, Lmax);
                    float half = b.width * .5f, bank = 3.5f, reach = half + bank;
                    float x0 = float.MaxValue, x1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue;
                    for (float s = -4f; s <= Lend; s += 1f) { var p = At(s); x0 = Mathf.Min(x0, p.x); x1 = Mathf.Max(x1, p.x); z0 = Mathf.Min(z0, p.y); z1 = Mathf.Max(z1, p.y); }
                    int changed = 0; float most = 0f;
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
                        if (s > Lend || d > reach || s < -.7f) return y;
                        if (s < 0f) { if (d > half + .35f) return y; float u = deckTop + grade * s - .02f; if (Mathf.Abs(u - y) < 1e-3f) return y; changed++; most = Mathf.Max(most, Mathf.Abs(u - y)); return u; }
                        if (UnderOther(p, Target(s), bi, chain)) return y;
                        // the change along the line at s, faded out across the banks and over the last 6 m before L
                        // (the height itself, level across the road: adding the change at the line to each sample overshot where the ground is steep
                        // right at a deck's end; Frey Road's Express deck kept a 0.7 m spike at its end)
                        float fade = (d <= half + .5f ? 1f : 1f - Mathf.SmoothStep(0f, 1f, (d - half - .5f) / (bank - .5f)));
                        float ny = y + (Target(s) - y) * fade;
                        if (Mathf.Abs(ny - y) > 1e-3f) { changed++; most = Mathf.Max(most, Mathf.Abs(ny - y)); }
                        return ny;
                    });
                    for (int i = 0; i < NM; i++) { var p = At(i * .5f); GM[i] = terrain.Height(p.x, p.y); }
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
