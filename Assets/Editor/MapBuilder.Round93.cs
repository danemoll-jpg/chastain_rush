using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Round 93 (A, Dan's photo after The Big One: "this needs to be fixed. It is an unwanted bump"; C1, Wings & Wheels: "one spot ... also need clean up to be
// more smooth. So please check this course as well for unsmooth roads"): each listed course's own profile along its lap smoothed in the planning, after
// every other pass that grades the ground (Round53Plan), before the roads, gravel and berms are laid on it. Data/tracks/round93_smooth.json (written by
// Tools/mapgen/make_round93_smooth.py from the race routes): per line its points every 2 m, the speed a racer has there, the road's half width and the
// blend beyond it, and the stretches kept as they are (the designed jumps' run-ups, flights and landings; decks).
//   1. the ground along the middle, blurred (a Gaussian of 'sigma' m: lidar ridges, grade joins, the steps where two grades meet)
//   2. no crest a racer at that speed leaves the ground over (the profile's bend over a crest at most crest_g * g / v^2) and no sag sharper than
//      sag_g * g / v^2: crests cut, sags filled (Gauss-Seidel on the profile; the kept stretches hold)
//   3. no point moved more than max_change (m)
//   4. laid across the road's width (level to 'half', easing back to the ground over 'blend'), in short overlapping pieces (SetAlong)
// Report: Assets/Generated/round93_smooth.txt (the most each line moved, and where).
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class R93Line { public string id; public float[] points, speeds; public int[] keep; public float half, blend, sigma, crest_g, sag_g, max_change; public bool closed; }
        [Serializable] class R93File { public R93Line[] lines; }
        const string Round93File = "Data/tracks/round93_smooth.json";

        static void R93SmoothLaps(TerrainGrid terrain)
        {
            string path = Path.Combine(ProjectRoot, Round93File);
            if (!File.Exists(path)) { Debug.Log("[Round93] no " + Round93File); return; }
            var f = JsonUtility.FromJson<R93File>(File.ReadAllText(path)); var rep = new System.Text.StringBuilder();
            foreach (var ln in f.lines ?? new R93Line[0])
            {
                var p = ToPoints(ln.points); int n = p.Count; if (n < 8) continue;
                var keep = new bool[n];
                if (ln.keep != null) for (int k = 0; k + 1 < ln.keep.Length; k += 2) for (int i = Mathf.Max(0, ln.keep[k]); i <= Mathf.Min(n - 1, ln.keep[k + 1]); i++) keep[i] = true;
                var ds = new float[n]; for (int i = 1; i < n; i++) ds[i] = (p[i] - p[i - 1]).magnitude; float step = ds.Skip(1).Average();
                var raw = new float[n]; for (int i = 0; i < n; i++) raw[i] = terrain.Height(p[i].x, p[i].y);
                int Idx(int i) => ln.closed ? (i % n + n) % n : Mathf.Clamp(i, 0, n - 1);
                // 1. the blur
                var h = (float[])raw.Clone();
                if (ln.sigma > 0f)
                {
                    int K = Mathf.CeilToInt(2.5f * ln.sigma / step); var w = new float[2 * K + 1];
                    for (int q = -K; q <= K; q++) w[q + K] = Mathf.Exp(-.5f * (q * step) * (q * step) / (ln.sigma * ln.sigma));
                    for (int i = 0; i < n; i++)
                    {
                        if (keep[i]) continue; float sw = 0f, sy = 0f;
                        for (int q = -K; q <= K; q++) { int j = Idx(i + q); if (!ln.closed && (i + q < 0 || i + q >= n)) continue; sw += w[q + K]; sy += w[q + K] * raw[j]; }
                        h[i] = sy / sw;
                    }
                }
                // 2. crests and sags: h[i] <= mean of its neighbors + c ds^2 / 2 (crest), >= mean - s ds^2 / 2 (sag)
                var cmax = new float[n]; var smax = new float[n];
                for (int i = 0; i < n; i++) { float v = Mathf.Max(8f, ln.speeds != null && i < ln.speeds.Length ? ln.speeds[i] : 30f); cmax[i] = ln.crest_g * 9.81f / (v * v) * step * step * .5f; smax[i] = ln.sag_g * 9.81f / (v * v) * step * step * .5f; }
                int it = 0; float moved = 1f;
                for (; it < 60000 && moved > .0005f; it++)
                {
                    moved = 0f;
                    for (int i = 0; i < n; i++)
                    {
                        if (keep[i] || (!ln.closed && (i == 0 || i == n - 1))) continue;
                        float m = .5f * (h[Idx(i - 1)] + h[Idx(i + 1)]), hi = m + cmax[i], lo = m - smax[i];
                        float nv = Mathf.Clamp(h[i], lo, hi); moved = Mathf.Max(moved, Mathf.Abs(nv - h[i])); h[i] = nv;
                    }
                }
                // 3. the cap
                float most = 0f; int at = 0, changed = 0;
                for (int i = 0; i < n; i++)
                {
                    if (keep[i]) { h[i] = raw[i]; continue; }
                    h[i] = raw[i] + Mathf.Clamp(h[i] - raw[i], -ln.max_change, ln.max_change);
                    float d = Mathf.Abs(h[i] - raw[i]); if (d > most) { most = d; at = i; } if (d > .05f) changed++;
                }
                // 4. laid across the road, piece by piece (50 points, 6 overlapping), each run of points not kept on its own; faded in where a kept stretch ends
                int pieces = 0;
                int s0 = 0;
                while (s0 < n)
                {
                    while (s0 < n && keep[s0]) s0++;
                    if (s0 >= n) break;
                    int e = s0; while (e + 1 < n && !keep[e + 1]) e++;
                    bool fadeA = s0 > 0 || !ln.closed, fadeB = e < n - 1 || !ln.closed;
                    for (int a = s0; a <= e; a += 44)
                    {
                        int b = Mathf.Min(e, a + 50); if (b - a < 2) break;
                        var flat = new float[(b - a + 1) * 2]; var ys = new float[b - a + 1];
                        for (int i = a; i <= b; i++) { flat[(i - a) * 2] = p[i].x; flat[(i - a) * 2 + 1] = p[i].y; ys[i - a] = h[i]; }
                        // (only a run's own ends fade; pieces inside a run overlap at full strength)
                        bool first = a == s0 && fadeA, last = b == e && fadeB;
                        SetAlongEnds(terrain, flat, ys, ln.half, ln.blend, first ? 6f : 0f, last ? 6f : 0f);
                        pieces++;
                        if (b == e) break;
                    }
                    s0 = e + 1;
                }
                string line = $"{ln.id}: {n} points, {changed} moved over 5 cm, the most {most:0.00} m at ({p[at].x:0.0}, {p[at].y:0.0}) (raw {raw[at]:0.00}); {it} passes; {pieces} pieces laid";
                rep.AppendLine(line); Debug.Log("[Round93] " + line);
            }
            try { File.WriteAllText(Path.Combine(ProjectRoot, "Assets/Generated/round93_smooth.txt"), rep.ToString()); } catch (Exception) { }
        }

        // SetAlong with each end faded on its own (0: not faded, the piece meets the next one at full strength)
        static void SetAlongEnds(TerrainGrid terrain, float[] flat, float[] y, float half, float blend, float fadeStart, float fadeEnd)
        {
            var p = ToPoints(flat); int n = p.Count; float reach = half + blend;
            var cs = new float[n]; for (int i = 1; i < n; i++) cs[i] = cs[i - 1] + (p[i] - p[i - 1]).magnitude; float L = cs[n - 1];
            var area = Rect.MinMaxRect(p.Min(q => q.x) - reach, p.Min(q => q.y) - reach, p.Max(q => q.x) + reach, p.Max(q => q.y) + reach);
            EditTerrain(terrain, area, (x, z, gy) =>
            {
                var q = new Vector2(x, z); float best = float.MaxValue, target = gy, along = 0f; bool past = false;
                for (int i = 0; i + 1 < n; i++)
                {
                    var ab = p[i + 1] - p[i]; float tu = Vector2.Dot(q - p[i], ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f), t = Mathf.Clamp01(tu); float dd = (p[i] + ab * t - q).magnitude;
                    if (dd < best) { best = dd; target = Mathf.Lerp(y[i], y[i + 1], t); along = Mathf.Lerp(cs[i], cs[i + 1], t); past = (i == 0 && tu < 0f) || (i == n - 2 && tu > 1f); }
                }
                if (best >= half + blend || past) return gy;
                float w = best <= half ? 1f : Smooth01(1f - (best - half) / blend);
                if (fadeStart > 0f) w *= Smooth01(along / fadeStart);
                if (fadeEnd > 0f) w *= Smooth01((L - along) / fadeEnd);
                return gy + w * (target - gy);
            });
        }
    }
}
