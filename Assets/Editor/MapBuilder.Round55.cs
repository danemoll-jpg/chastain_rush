using System;
using UnityEngine;

// Round 55 (Dan, 2026-10-21 12:28; reports BUG-001 to BUG-003 of session 2026-10-21_12-16-40-990_d377b3): Addison Heights' two bumps, from
// Data/tracks/addison_heights.json "smooth_spots" (Tools/mapgen/make_addison_route.py), in the planning after the lap is levelled (Round53Plan).
//   D  BUG-001 (Hayworth Drive at Crawford Circle) and BUG-003 (Kelly Drive at Suholden Circle): the ridge the levelling leaves along each corner's
//      bisector (R54LevelLap sets each point to its nearest piece of the lap's line, and at a corner the nearest piece changes there) blurred away;
//      at BUG-001 first the grade change itself made a vertical curve ("vertical_curves")
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class R55Spot { public string id; public float[] at; public float r, fade, sigma; }
        [Serializable] class R55Curve { public string id; public float[] points; public float length, fit, half, blend; }
        [Serializable] class R55File { public R55Spot[] smooth_spots; public R55Curve[] vertical_curves; }

        // a road's vertical curve: along the line, the ground's grade over its first and last 'fit' m (least squares), the two grades joined by a
        // parabola 'length' m long about where they meet; set across the road ('half', blending back over 'blend'), the ground outside the curve as it is
        static void R55VerticalCurves(TerrainGrid terrain, R55File d)
        {
            if (d?.vertical_curves == null) return;
            foreach (var vc in d.vertical_curves)
            {
                var p = ToPoints(vc.points); int n = p.Count; if (n < 8) continue;
                var cs = new float[n]; for (int i = 1; i < n; i++) cs[i] = cs[i - 1] + (p[i] - p[i - 1]).magnitude;
                var raw = new float[n]; for (int i = 0; i < n; i++) raw[i] = terrain.Height(p[i].x, p[i].y);
                (float a, float g) Fit(Func<int, bool> pick)
                {
                    float sx = 0f, sy = 0f, sxx = 0f, sxy = 0f; int m = 0;
                    for (int i = 0; i < n; i++) if (pick(i)) { sx += cs[i]; sy += raw[i]; sxx += cs[i] * cs[i]; sxy += cs[i] * raw[i]; m++; }
                    float g = (m * sxy - sx * sy) / Mathf.Max(1e-6f, m * sxx - sx * sx); return ((sy - g * sx) / m, g);
                }
                float L = cs[n - 1];
                var (a1, g1) = Fit(i => cs[i] <= vc.fit); var (a2, g2) = Fit(i => cs[i] >= L - vc.fit);
                float sP = Mathf.Abs(g2 - g1) < 1e-4f ? L * 0.5f : (a1 - a2) / (g2 - g1), s0 = sP - vc.length * 0.5f, s1 = sP + vc.length * 0.5f;
                var pts = new System.Collections.Generic.List<float>(); var ys = new System.Collections.Generic.List<float>(); float most = 0f;
                for (int i = 0; i < n; i++)
                {
                    if (cs[i] < s0 - 2f || cs[i] > s1 + 2f) continue;
                    float x = Mathf.Clamp(cs[i] - s0, 0f, vc.length);
                    float y = cs[i] < s0 || cs[i] > s1 ? raw[i] : a1 + g1 * cs[i] + (g2 - g1) / (2f * vc.length) * x * x;
                    pts.Add(p[i].x); pts.Add(p[i].y); ys.Add(y); most = Mathf.Max(most, Mathf.Abs(y - raw[i]));
                }
                if (ys.Count < 3) continue;
                GradeAlong(terrain, new RidgeGrade { id = vc.id, points = pts.ToArray(), y = ys.ToArray(), half = vc.half, blend = vc.blend });
                Debug.Log($"[Round55] D: {vc.id}: grades {g1 * 100f:0.0} % and {g2 * 100f:0.0} % joined by a {vc.length:0} m vertical curve about {sP:0} m along its line (the ground moved by up to {most:0.00} m)");
            }
        }

        // the ground's own heights blurred (a Gaussian of 'sigma', so planes and steady grades stay as they are), full within 'r' of the spot,
        // fading out over 'fade'; two passes
        static void R55SmoothSpots(TerrainGrid terrain)
        {
            string path = System.IO.Path.Combine(ProjectRoot, AddisonTrackFile);
            if (!System.IO.File.Exists(path)) return;
            var d = JsonUtility.FromJson<R55File>(System.IO.File.ReadAllText(path));
            R55VerticalCurves(terrain, d);   // the grade change first, then the blur over it
            if (d?.smooth_spots == null) return;
            const float Step = 0.5f;
            foreach (var sp in d.smooth_spots)
            {
                var c = new Vector2(sp.at[0], sp.at[1]); float reach = sp.r + sp.fade, pad = 3f * sp.sigma;
                int n = Mathf.CeilToInt((reach + pad) * 2f / Step) + 1; float x0 = c.x - reach - pad, z0 = c.y - reach - pad;
                float most = 0f;
                for (int pass = 0; pass < 2; pass++)
                {
                    // the ground as it is, on a 0.5 m grid, then blurred along x and along z
                    var g = new float[n, n]; var t = new float[n, n]; var b = new float[n, n];
                    for (int j = 0; j < n; j++) for (int i = 0; i < n; i++) g[j, i] = terrain.Height(x0 + i * Step, z0 + j * Step);
                    int k = Mathf.CeilToInt(2.5f * sp.sigma / Step); var w = new float[2 * k + 1];
                    for (int q = -k; q <= k; q++) w[q + k] = Mathf.Exp(-0.5f * (q * Step) * (q * Step) / (sp.sigma * sp.sigma));
                    for (int j = 0; j < n; j++) for (int i = 0; i < n; i++) { float sw = 0f, sy = 0f; for (int q = -k; q <= k; q++) { int ii = i + q; if (ii < 0 || ii >= n) continue; sw += w[q + k]; sy += w[q + k] * g[j, ii]; } t[j, i] = sy / sw; }
                    for (int j = 0; j < n; j++) for (int i = 0; i < n; i++) { float sw = 0f, sy = 0f; for (int q = -k; q <= k; q++) { int jj = j + q; if (jj < 0 || jj >= n) continue; sw += w[q + k]; sy += w[q + k] * t[jj, i]; } b[j, i] = sy / sw; }
                    float Blurred(float x, float z)
                    {
                        float fx = Mathf.Clamp((x - x0) / Step, 0f, n - 1.001f), fz = Mathf.Clamp((z - z0) / Step, 0f, n - 1.001f); int i = (int)fx, j = (int)fz; float a = fx - i, e = fz - j;
                        return Mathf.Lerp(Mathf.Lerp(b[j, i], b[j, i + 1], a), Mathf.Lerp(b[j + 1, i], b[j + 1, i + 1], a), e);
                    }
                    EditTerrain(terrain, Rect.MinMaxRect(c.x - reach, c.y - reach, c.x + reach, c.y + reach), (x, z, y) =>
                    {
                        float dist = (new Vector2(x, z) - c).magnitude; if (dist >= reach) return y;
                        float wt = dist <= sp.r ? 1f : Smooth01(1f - (dist - sp.r) / sp.fade), ny = y + wt * (Blurred(x, z) - y);
                        most = Mathf.Max(most, Mathf.Abs(ny - y));
                        return ny;
                    });
                }
                Debug.Log($"[Round55] D: {sp.id}: the ground blurred (sigma {sp.sigma:0.0} m) within {sp.r:0} m of ({c.x:0.0}, {c.y:0.0}), fading over {sp.fade:0} m; moved by up to {most:0.00} m");
            }
        }
    }
}
