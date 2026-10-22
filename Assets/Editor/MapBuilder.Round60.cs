using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Round 60 (Part B, BUG-001 of session 2026-10-22_05-11-54-864_7498e6, x -1051.61 z 420.74, Dan: "This needs some smoothing"; "The overpass over 75 is very
// bumpy"): Chastain Road over I-75. The decks themselves are straight (one deck per carriageway since Round 27's JoinChainedDecks, 5 % grade); the
// bumps were in the ground where both carriageways come off the decks at the west end (ripples of +0.2 / -0.55 / +0.2 m within 2 m, x -1036; rings
// round each deck's end and a seam along the westbound carriageway's edge, where the two ends' gradings, 20 m apart, overlapped) and, smaller,
// at the east end (x -937). In the planning, after SmoothBridgeEnds, at those two ends only:
//   1. the ground blurred (a Gaussian of 2.5 m: planes and steady grades stay) within 26 m of the ends' middle, fading out over 10 m; never under
//      a deck or within 1 m past its end (the deck is fixed: the ground must meet it);
//   2. each carriageway laid again from its deck's end: one smooth curve (cubic Hermite) from the deck's top at the deck's grade to the blurred
//      ground 45 m on at its own grade, across the road's width (easing back to the ground over 4 m beside it).
// Before / after: the sharpest step (second difference over 1 m) along each carriageway's line, in the build log.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        static void R60SmoothOverpass(MapData map, TerrainGrid terrain)
        {
            foreach (var (label, centre) in new[] { ("west end", new Vector2(-1033f, 424f)), ("east end", new Vector2(-939f, 448f)) })
            {
                // the Chastain Road deck ends here (the chain's outer ends: no other deck end within 3 m)
                var ends = new List<(int b, bool start, Vector2 E, Vector2 outward)>();
                for (int bi = 0; bi < map.bridges.Length; bi++)
                {
                    var b = map.bridges[bi]; if (!(b.name ?? "").StartsWith("Chastain Road")) continue; var pts = ToPoints(b.points); int n = pts.Count; if (n < 3) continue;
                    foreach (bool start in new[] { true, false })
                    {
                        var E = pts[start ? 0 : n - 1]; if ((E - centre).magnitude > 30f) continue;
                        bool chained = false;
                        for (int bj = 0; bj < map.bridges.Length && !chained; bj++) { if (bj == bi) continue; var o = map.bridges[bj].points; int k = o.Length / 2; if (Vector2.Distance(E, new Vector2(o[0], o[1])) < 3f || Vector2.Distance(E, new Vector2(o[2 * k - 2], o[2 * k - 1])) < 3f) chained = true; }
                        if (chained) continue;
                        ends.Add((bi, start, E, (E - pts[start ? 1 : n - 2]).normalized));
                    }
                }
                if (ends.Count == 0) { Debug.LogWarning($"[Round60] B: the overpass's {label}: no deck end found"); continue; }
                // the roads on the ground carrying on from each end (as SmoothBridgeEnds finds them)
                var paths = new List<List<Vector2>>();
                foreach (var e in ends)
                {
                    List<Vector2> path = null;
                    foreach (var r in map.roads)
                    {
                        var q = ToPoints(r.points); if (q.Count < 2) continue;
                        if (Vector2.Distance(q[0], e.E) < 2f && Vector2.Dot(q[1] - q[0], e.outward) > 0f) { path = q; break; }
                        if (Vector2.Distance(q[q.Count - 1], e.E) < 2f && Vector2.Dot(q[q.Count - 2] - q[q.Count - 1], e.outward) > 0f) { q.Reverse(); path = q; break; }
                    }
                    if (path != null) { path[0] = e.E; path = ExtendPath(map, path, 60f); }
                    paths.Add(path);
                }
                var before = string.Join(", ", paths.Where(p => p != null).Select(p => $"{SharpestStep(terrain, p, 50f):0.000} m"));
                // 1. the blur, never on a deck or within 1 m past its end
                bool OnDeck(Vector2 p)
                {
                    foreach (var b in map.bridges)
                    {
                        if (!(b.name ?? "").StartsWith("Chastain Road")) continue; var q = ToPoints(b.points); float half = b.width * .5f + 1f;
                        for (int i = 0; i + 1 < q.Count; i++)
                        {
                            var ab = q[i + 1] - q[i]; float len = ab.magnitude; if (len < 1e-3f) continue; var dir = ab / len;
                            float s = Vector2.Dot(p - q[i], dir), d = Mathf.Abs(dir.x * (p.y - q[i].y) - dir.y * (p.x - q[i].x));
                            if (s > -1f && s < len + 1f && d < half) return true;
                        }
                    }
                    return false;
                }
                const float R = 26f, Fade = 10f, Sigma = 2.5f, Step = .5f; float pad = 3f * Sigma; int N = Mathf.CeilToInt((R + Fade + pad) * 2f / Step) + 1;
                float x0 = centre.x - R - Fade - pad, z0 = centre.y - R - Fade - pad;
                var g = new float[N, N]; var tmp = new float[N, N]; var bl = new float[N, N];
                for (int j = 0; j < N; j++) for (int i = 0; i < N; i++) g[j, i] = terrain.Height(x0 + i * Step, z0 + j * Step);
                int K = Mathf.CeilToInt(2.5f * Sigma / Step); var wk = new float[2 * K + 1]; for (int q = -K; q <= K; q++) wk[q + K] = Mathf.Exp(-.5f * (q * Step) * (q * Step) / (Sigma * Sigma));
                for (int j = 0; j < N; j++) for (int i = 0; i < N; i++) { float sw = 0, sy = 0; for (int q = -K; q <= K; q++) { int ii = i + q; if (ii < 0 || ii >= N) continue; sw += wk[q + K]; sy += wk[q + K] * g[j, ii]; } tmp[j, i] = sy / sw; }
                for (int j = 0; j < N; j++) for (int i = 0; i < N; i++) { float sw = 0, sy = 0; for (int q = -K; q <= K; q++) { int jj = j + q; if (jj < 0 || jj >= N) continue; sw += wk[q + K]; sy += wk[q + K] * tmp[jj, i]; } bl[j, i] = sy / sw; }
                float Blurred(float x, float z) { float fx = Mathf.Clamp((x - x0) / Step, 0f, N - 1.001f), fz = Mathf.Clamp((z - z0) / Step, 0f, N - 1.001f); int i = (int)fx, j = (int)fz; float a = fx - i, e = fz - j; return Mathf.Lerp(Mathf.Lerp(bl[j, i], bl[j, i + 1], a), Mathf.Lerp(bl[j + 1, i], bl[j + 1, i + 1], a), e); }
                float moved = 0f; int samples = 0;
                EditTerrain(terrain, Rect.MinMaxRect(centre.x - R - Fade, centre.y - R - Fade, centre.x + R + Fade, centre.y + R + Fade), (x, z, y) =>
                {
                    var p = new Vector2(x, z); float d = (p - centre).magnitude; if (d >= R + Fade || OnDeck(p)) return y;
                    float w = d <= R ? 1f : Smooth01(1f - (d - R) / Fade); float ny = y + w * (Blurred(x, z) - y);
                    if (Mathf.Abs(ny - y) > 1e-3f) { samples++; moved = Mathf.Max(moved, Mathf.Abs(ny - y)); }
                    return ny;
                });
                // 2. each carriageway again from its deck's end
                for (int k = 0; k < ends.Count; k++)
                {
                    var e = ends[k]; var path = paths[k]; if (path == null) continue; var b = map.bridges[e.b]; var bp = ToPoints(b.points); int n = bp.Count;
                    float lift = (RoadLift.TryGetValue(b.@class, out var l) ? l : .06f) * vScale;
                    int ie = e.start ? 0 : n - 1; int ig = ie; float span = 0f; while (span < 8f && (e.start ? ig + 1 < n : ig - 1 >= 0)) { int nx = e.start ? ig + 1 : ig - 1; span += Vector2.Distance(bp[ig], bp[nx]); ig = nx; }
                    float deckTop = b.deck_y[ie] + lift, grade = (b.deck_y[ie] - b.deck_y[ig]) / Mathf.Max(span, .1f);
                    var cum = new float[path.Count]; for (int i = 1; i < path.Count; i++) cum[i] = cum[i - 1] + (path[i] - path[i - 1]).magnitude;
                    Vector2 At(float s) { for (int i = 1; i < path.Count; i++) if (s <= cum[i]) return Vector2.Lerp(path[i - 1], path[i], (s - cum[i - 1]) / Mathf.Max(cum[i] - cum[i - 1], 1e-4f)); return path[path.Count - 1]; }
                    const float L = 45f; float yL = terrain.Height(At(L).x, At(L).y), gL = (yL - terrain.Height(At(L - 10f).x, At(L - 10f).y)) / 10f, y0 = deckTop - .01f;
                    float Target(float s) { float t = Mathf.Clamp01(s / L), t2 = t * t, t3 = t2 * t; return (2f * t3 - 3f * t2 + 1f) * y0 + (t3 - 2f * t2 + t) * L * grade + (-2f * t3 + 3f * t2) * yL + (t3 - t2) * L * gL; }
                    float half = b.width * .5f + .5f, bank = 4f; int changed = 0;
                    var lo = path.Aggregate(new Vector2(float.MaxValue, float.MaxValue), (a, q) => Vector2.Min(a, q)) - Vector2.one * (half + bank); var hi = path.Aggregate(new Vector2(float.MinValue, float.MinValue), (a, q) => Vector2.Max(a, q)) + Vector2.one * (half + bank);
                    EditTerrain(terrain, Rect.MinMaxRect(lo.x, lo.y, hi.x, hi.y), (x, z, y) =>
                    {
                        var p = new Vector2(x, z); if (Vector2.Dot(p - e.E, e.outward) < .05f) return y;   // never on the deck's side of its end
                        float best = float.MaxValue, s = 0f;
                        for (int i = 0; i + 1 < path.Count && cum[i] <= L + bank; i++) { var ab = path[i + 1] - path[i]; float t = Mathf.Clamp01(Vector2.Dot(p - path[i], ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f)); float dd = (path[i] + ab * t - p).sqrMagnitude; if (dd < best) { best = dd; s = cum[i] + t * ab.magnitude; } }
                        float d = Mathf.Sqrt(best); if (s > L || d > half + bank) return y;
                        float w = d <= half ? 1f : Smooth01(1f - (d - half) / bank); float ny = y + w * (Target(s) - y);
                        if (Mathf.Abs(ny - y) > 1e-3f) changed++; return ny;
                    });
                    Debug.Log($"[Round60] B: the overpass's {label}: bridge {e.b + 1} ({(e.start ? "start" : "end")}) at x {e.E.x:0.0} z {e.E.y:0.0}: laid from the deck's top {deckTop:0.00} m at {grade * 100f:0.0} % to the ground 45 m on ({yL:0.00} m, {gL * 100f:0.0} %), {changed} samples");
                }
                var after = string.Join(", ", paths.Where(p => p != null).Select(p => $"{SharpestStep(terrain, p, 50f):0.000} m"));
                Debug.Log($"[Round60] B: the overpass's {label}: the ground blurred within {R:0} m ({samples} samples, up to {moved:0.00} m); the sharpest step along each carriageway's first 50 m: before {before}, after {after}");
            }
        }
        // a road path carried on through the roads that continue from its far end (to 'length' m)
        static List<Vector2> ExtendPath(MapData map, List<Vector2> path, float length)
        {
            float Len(List<Vector2> p) { float s = 0; for (int i = 1; i < p.Count; i++) s += (p[i] - p[i - 1]).magnitude; return s; }
            for (int guard = 0; guard < 6 && Len(path) < length; guard++)
            {
                var tail = path[path.Count - 1]; var dir = (tail - path[path.Count - 2]).normalized; List<Vector2> next = null; float bestDot = .7f;
                foreach (var r in map.roads)
                {
                    var q = ToPoints(r.points); if (q.Count < 2) continue;
                    if (Vector2.Distance(q[0], tail) < 1.5f) { float dt = Vector2.Dot((q[1] - q[0]).normalized, dir); if (dt > bestDot) { bestDot = dt; next = q; } }
                    else if (Vector2.Distance(q[q.Count - 1], tail) < 1.5f) { var rq = Enumerable.Reverse(q).ToList(); float dt = Vector2.Dot((rq[1] - rq[0]).normalized, dir); if (dt > bestDot) { bestDot = dt; next = rq; } }
                }
                if (next == null) break; path.AddRange(next.Skip(1));
            }
            return path;
        }
        // the sharpest second difference of the ground over 1 m along a path's first 'length' m
        static float SharpestStep(TerrainGrid terrain, List<Vector2> path, float length)
        {
            var cum = new float[path.Count]; for (int i = 1; i < path.Count; i++) cum[i] = cum[i - 1] + (path[i] - path[i - 1]).magnitude;
            Vector2 At(float s) { for (int i = 1; i < path.Count; i++) if (s <= cum[i]) return Vector2.Lerp(path[i - 1], path[i], (s - cum[i - 1]) / Mathf.Max(cum[i] - cum[i - 1], 1e-4f)); return path[path.Count - 1]; }
            float worst = 0f, end = Mathf.Min(length, cum[cum.Length - 1] - 1f);
            for (float s = 1f; s < end; s += .5f) { var a = At(s - 1f); var m = At(s); var b = At(s + 1f); worst = Mathf.Max(worst, Mathf.Abs(terrain.Height(a.x, a.y) - 2f * terrain.Height(m.x, m.y) + terrain.Height(b.x, b.y))); }
            return worst;
        }
    }
}
