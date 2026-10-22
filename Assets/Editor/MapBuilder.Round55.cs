using System;
using System.Linq;
using UnityEngine;

// Round 55 (Dan, 2026-10-21 12:28; reports BUG-001 to BUG-003 of session 2026-10-21_12-16-40-990_d377b3): Addison Heights' two bumps, from
// Data/tracks/addison_heights.json "smooth_spots" (Tools/mapgen/make_addison_route.py), in the planning after the lap is levelled (Round53Plan).
//   A  the Elaine jump (Dan's option 2): its worn tracks (BuildAddison55, the step Addison); its bench grade, cleared line, landing zone and hedges are data
//   D  BUG-001 (Hayworth Drive at Crawford Circle) and BUG-003 (Kelly Drive at Suholden Circle): the ridge the levelling leaves along each corner's
//      bisector (R54LevelLap sets each point to its nearest piece of the lap's line, and at a corner the nearest piece changes there) blurred away;
//      at BUG-001 first the grade change itself made a vertical curve ("vertical_curves")
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class R55Spot { public string id; public float[] at; public float r, fade, sigma; }
        [Serializable] class R55Curve { public string id; public float[] points, at; public float length, fit, half, blend; }
        [Serializable] class R55Ruts { public string id; public float[] points; }
        [Serializable] class R55File { public R55Spot[] smooth_spots; public R55Curve[] vertical_curves; public R55Ruts[] worn_tracks; }

        // A (Dan picked option 2): the Elaine jump's world pieces that are not plain data for the planning (its bench grade, cleared line, landing zone and
        // hedges are AddisonNow's / AddisonExtra's): worn tire tracks off the bend's edge, down the bank and along the bench (the hint)
        static void BuildAddison55(TerrainGrid terrain, Materials mats, Transform root)
        {
            string path = System.IO.Path.Combine(ProjectRoot, AddisonTrackFile);
            if (!System.IO.File.Exists(path)) return;
            var d = JsonUtility.FromJson<R55File>(System.IO.File.ReadAllText(path));
            if (d?.worn_tracks == null) return;
            foreach (var w in d.worn_tracks)
                if (w.points != null && w.points.Length >= 6) { WornRuts(terrain, mats, root, ToPoints(w.points), w.id); Debug.Log($"[Round55] A: worn tracks: {w.id}"); }
        }

        // a road's vertical curve: along the line, 'length' m about the point nearest 'at' (where the grades meet), the ground replaced by the cubic that
        // meets the ground's own height and grade at both ends (over a kink between two steady grades, the road's parabolic vertical curve); set across
        // the road ('half', blending back over 'blend')
        static void R55VerticalCurves(TerrainGrid terrain, R55File d)
        {
            if (d?.vertical_curves == null) return;
            foreach (var vc in d.vertical_curves)
            {
                var p = ToPoints(vc.points); int n = p.Count; if (n < 8) continue;
                var cs = new float[n]; for (int i = 1; i < n; i++) cs[i] = cs[i - 1] + (p[i] - p[i - 1]).magnitude;
                var raw = new float[n]; for (int i = 0; i < n; i++) raw[i] = terrain.Height(p[i].x, p[i].y);
                var at = new Vector2(vc.at[0], vc.at[1]); int ic = 0; for (int i = 1; i < n; i++) if ((p[i] - at).sqrMagnitude < (p[ic] - at).sqrMagnitude) ic = i;
                float s0 = cs[ic] - vc.length * 0.5f, s1 = cs[ic] + vc.length * 0.5f;
                float H(float s) { int i = 0; while (i + 2 < n && cs[i + 1] < s) i++; return Mathf.Lerp(raw[i], raw[i + 1], Mathf.InverseLerp(cs[i], cs[i + 1], s)); }
                float y0 = H(s0), y1 = H(s1), g0 = (H(s0) - H(s0 - vc.fit)) / vc.fit, g1 = (H(s1 + vc.fit) - H(s1)) / vc.fit, L = s1 - s0;
                var pts = new System.Collections.Generic.List<float>(); var ys = new System.Collections.Generic.List<float>(); float most = 0f;
                for (int i = 0; i < n; i++)
                {
                    if (cs[i] < s0 - 8f || cs[i] > s1 + 8f) continue;
                    float t = Mathf.Clamp01((cs[i] - s0) / L), t2 = t * t, t3 = t2 * t;
                    float y = cs[i] < s0 || cs[i] > s1 ? raw[i] : (2f * t3 - 3f * t2 + 1f) * y0 + (t3 - 2f * t2 + t) * L * g0 + (-2f * t3 + 3f * t2) * y1 + (t3 - t2) * L * g1;
                    pts.Add(p[i].x); pts.Add(p[i].y); ys.Add(y); most = Mathf.Max(most, Mathf.Abs(y - raw[i]));
                }
                if (ys.Count < 3) continue;
                SetAlong(terrain, pts.ToArray(), ys.ToArray(), vc.half, vc.blend, 6f);
                Debug.Log($"[Round55] D: {vc.id}: {g0 * 100f:0.0} % into {g1 * 100f:0.0} % over a {L:0} m vertical curve (the ground moved by up to {most:0.00} m)");
            }
        }

        // as GradeAlong, but nothing past the line's ends (GradeAlong's end caps level a disc round each end), and faded in from each end over 'fadeEnds'
        static void SetAlong(TerrainGrid terrain, float[] flat, float[] y, float half, float blend, float fadeEnds)
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
                if (best >= reach || past) return gy;
                float w = (best <= half ? 1f : Smooth01(1f - (best - half) / blend)) * Smooth01(Mathf.Min(along, L - along) / fadeEnds);
                return gy + w * (target - gy);
            });
        }

        // the ground's own heights blurred (a Gaussian of 'sigma', so planes and steady grades stay as they are), full within 'r' of the spot,
        // fading out over 'fade'; two passes
        static void R55SmoothSpots(TerrainGrid terrain)
        {
            // Round 58 (D): the Chastain Ridge track's vertical curves too (Chastain Ridge Drive's valley, Dan's BUG-003), the same way
            string ridgePath = System.IO.Path.Combine(ProjectRoot, RidgeTrackFile);
            if (System.IO.File.Exists(ridgePath)) R55VerticalCurves(terrain, JsonUtility.FromJson<R55File>(System.IO.File.ReadAllText(ridgePath)));
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
