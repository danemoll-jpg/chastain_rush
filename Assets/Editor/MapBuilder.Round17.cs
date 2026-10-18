using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Round 17, item 5: Dan's session 2026-10-10_17-47-11-485_46d8cb, BUG-001 and BUG-002 ("unintended jump").
// Both are junction seams in the terrain the car drives on. mapgen levels the ground under each road piece to that piece's
// own profile, and where pieces cross, every sample takes the height of whichever piece pulls hardest: so across a junction
// the surface changes from one piece's height to another's along a line, a crease up to 0.3 m high in 1 m (BUG-001, Chastain
// Glen Lane across New Chastain Road's two carriageways at the entrance) and up to 0.5 m (BUG-002, Chastain Corner and
// Chastain Ridge across New Chastain Road). At these two junctions only (Dan: "this spot only"), the pavement is smoothed:
// a Gaussian (sigma 4 m) of the pavement's own heights (samples on a road or within 1 m of its edge; the banks and lawns
// round it do not count), applied on the pavement and 3 m past its edge, fading out from the spot's radius over 8 m (14 m).
// Grades along the roads stay (the smoothing only rounds steps, creases and crests). It runs before anything samples the ground,
// so the road meshes, curbs, island, signs and trees all sit on the new surface. Other junctions are listed for Dan, not changed.
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        // BUG-002 needs the wider smoothing: there the junction's level plateau sits on a 7 % grade, and with sigma 4 m the step
        // became a crest that still lifted a car at 35 m/s westbound (0.3 s); sigma 8 m spreads it over about 30 m
        static readonly (Vector2 c, float r, float sigma, float fade, string what)[] SeamSpots =
        {
            (new Vector2(2941f, 756f), 24f, 4f, 8f, "BUG-001 Chastain Glen Lane at New Chastain Road (the entrance)"),
            (new Vector2(3152f, 756f), 30f, 8f, 14f, "BUG-002 New Chastain Road at Chastain Corner Northeast"),
            // Round 40 (A3, BUG-003 of session 2026-10-17_12-09-19-449_081d93, "bumpy"): where Hilltop Drive and the service road opposite end on
            // New Chastain Road's northwest-bound carriageway (the reverse Chastain Ridge's line), a 6 cm crest then a 9 cm dip within 4 m
            (new Vector2(3467f, 404f), 16f, 4f, 8f, "Round 40 BUG-003 New Chastain Road at Hilltop Drive"),
        };
        const float SeamStep = 0.5f;

        static void SmoothJunctionSeams(MapData map, TerrainGrid terrain)
        {
            foreach (var spot in SeamSpots) SmoothSpot(map, terrain, spot);
            RoundApproachCrest(terrain);
            FillVergeHollow(map, terrain);
        }

        // Round 18 (2a): Dan's session 2026-10-10_23-08-26-878_0e77b1, BUG-001 ("Cant we have this smoothed out?", "There is an
        // uneven spot in the grass you see as it is darker right there"): the hollow in the grass verge in the corner between New
        // Chastain Road and Chastain Corner Northeast, just north-east of his vehicle at (3145.1, 768.6), left when BUG-002's
        // smoothing above pulled the ground near the curb down toward the lower junction: a 0.8 m rise in 2 m at x 3148 with a
        // trough at its foot and another along the curb. That spot only: within 5 m of VergeHollow, on the grass (from 0.5 m past
        // a road's edge, so the curb and the pavement keep their height), the ground is replaced by the smoothest surface that
        // meets the ground round it and the curb (a membrane: each point the mean of its neighbors), which has no dip and no hump.
        static readonly Vector2 VergeHollow = new Vector2(3149f, 771.5f);
        const float VergeR = 5f;

        static void FillVergeHollow(MapData map, TerrainGrid terrain)
        {
            float reach = VergeR + 2f;
            var segs = RoadSegsNear(map, VergeHollow, reach + 30f, _ => true);
            var g = new HeightGrid(VergeHollow - Vector2.one * reach, Mathf.CeilToInt(2f * reach / SeamStep) + 1, SeamStep);
            var f = g.Map((x, z) => terrain.Height(x, z));
            var free = g.Map((x, z) => Vector2.Distance(new Vector2(x, z), VergeHollow) < VergeR && Clearance(segs, new Vector2(x, z)) > 0.5f ? 1f : 0f);
            for (int it = 0; it < 4000; it++)
                for (int i = 1; i < g.NX - 1; i++) for (int j = 1; j < g.NZ - 1; j++)
                    if (free[i, j] > 0f) f[i, j] = 0.25f * (f[i - 1, j] + f[i + 1, j] + f[i, j - 1] + f[i, j + 1]);
            string Grid(string what)
            {
                var sb = new System.Text.StringBuilder($"[Round18] BUG-001 verge ground {what} (rows z 778 to 764, columns x 3141 to 3157):");
                for (float z = 778f; z >= 764f; z -= 2f) { sb.Append($"\n  z {z:0}:"); for (float x = 3141f; x <= 3157f; x += 2f) sb.Append($" {terrain.Height(x, z):0.00}"); }
                return sb.ToString();
            }
            Debug.Log(Grid("before"));
            float up = 0f, down = 0f;
            EditTerrain(terrain, new Rect(VergeHollow.x - VergeR, VergeHollow.y - VergeR, 2f * VergeR, 2f * VergeR), (x, z, y) =>
            {
                var q = new Vector2(x, z);
                if (Vector2.Distance(q, VergeHollow) >= VergeR || Clearance(segs, q) <= 0.5f) return y;
                float ny = g.Sample(f, x, z);
                up = Mathf.Max(up, ny - y); down = Mathf.Max(down, y - ny);
                return ny;
            });
            Debug.Log(Grid("after"));
            Debug.Log($"[Round18] BUG-001 the uneven verge at New Chastain Road / Chastain Corner smoothed (raised up to {up:0.00} m, lowered up to {down:0.00} m)");
        }

        // Round 18 (A): one spot, also used for the shopping lot's corner; 'paved' counts more ground as pavement (the lot)
        static void SmoothSpot(MapData map, TerrainGrid terrain, (Vector2 c, float r, float sigma, float fade, string what) spot, Func<Vector2, bool> paved = null)
        {
            {
                float reach = spot.r + 3f * spot.sigma + 2f;
                var segs = RoadSegsNear(map, spot.c, reach + 30f, _ => true);
                float Clear(Vector2 q) => paved != null && paved(q) ? Mathf.Min(0f, Clearance(segs, q)) : Clearance(segs, q);
                var g = new HeightGrid(spot.c - Vector2.one * reach, Mathf.CeilToInt(2f * reach / SeamStep) + 1, SeamStep);
                var clear = g.Map((x, z) => Clear(new Vector2(x, z)));
                var h = g.Map((x, z) => terrain.Height(x, z));
                var mask = g.Map((x, z) => clear[g.I(x), g.J(z)] <= 1f ? 1f : 0f);
                var blur = g.NormalizedBlur(h, mask, spot.sigma);
                float most = 0f;
                var area = new Rect(spot.c.x - spot.r - 1f, spot.c.y - spot.r - 1f, 2f * spot.r + 2f, 2f * spot.r + 2f);
                EditTerrain(terrain, area, (x, z, y) =>
                {
                    float wr = Smooth01((spot.r - Vector2.Distance(new Vector2(x, z), spot.c)) / spot.fade);
                    if (wr <= 0f) return y;
                    float w = wr * Smooth01((4f - Clear(new Vector2(x, z))) / 3f);
                    if (w <= 0f) return y;
                    float ny = y + w * (g.Sample(blur, x, z) - y);
                    most = Mathf.Max(most, Mathf.Abs(ny - y));
                    return ny;
                });
                Debug.Log($"[Round17] {spot.what}: pavement smoothed within {spot.r:0} m of ({spot.c.x:0}, {spot.c.y:0}), sigma {spot.sigma:0} m, largest change {most:0.00} m");
            }
        }

        // BUG-002, the approach from the north: Dan's bump log has him LANDED at (3164.3, 767.2), 15 m short of the junction
        // on Chastain Corner, which is where a car coming south off the crest at about (3162, 780) comes down (the check run
        // lifts off there at 35 m/s, before and after the junction smoothing). Along Chastain Corner from the north
        // carriageway to 75 m up, the crest is rounded: no crest sharper than 5 cm against the chord over +-6 m (a crest
        // radius of 360 m, about 0.35 g at 35 m/s). Crests only (dips stay), the first 4 m and everything past 75 m fixed,
        // applied across the road and its edge, fading out 4 to 7 m from the centerline.
        static readonly Vector2[] Bug002Approach =
        {
            new Vector2(3152.0f, 759.8f), new Vector2(3155.0f, 765.5f), new Vector2(3156.2f, 767.6f),
            new Vector2(3167.8f, 790.9f), new Vector2(3179.1f, 813.4f), new Vector2(3200.5f, 855.0f),
        };

        static void RoundApproachCrest(TerrainGrid terrain)
        {
            const int Chord = 6; const float SagMax = 0.05f, From = 4f, To = 75f;
            var pts = new List<Vector2>(); var s = new List<float>();
            float acc = 0f;
            for (int i = 0; i + 1 < Bug002Approach.Length; i++)
            {
                Vector2 a = Bug002Approach[i], b = Bug002Approach[i + 1]; float len = Vector2.Distance(a, b);
                for (float u = 0f; u < len; u += 1f) { pts.Add(Vector2.Lerp(a, b, u / len)); s.Add(acc + u); }
                acc += len;
            }
            int n = pts.Count;
            var h = pts.Select(q => terrain.Height(q.x, q.y)).ToArray(); var p = (float[])h.Clone();
            for (int it = 0; it < 3000; it++)
            {
                var d = new float[n]; float most = 0f;
                for (int k = Chord; k < n - Chord; k++)
                {
                    float ex = Mathf.Max(p[k] - 0.5f * (p[k - Chord] + p[k + Chord]) - SagMax, 0f);
                    most = Mathf.Max(most, ex);
                    d[k] -= ex / 3f; d[k - Chord] += ex / 6f; d[k + Chord] += ex / 6f;
                }
                if (most < 0.002f) break;
                for (int k = 0; k < n; k++) if (s[k] >= From && s[k] <= To) p[k] += d[k];
            }
            var delta = new float[n]; float cut = 0f, fill = 0f;
            for (int k = 0; k < n; k++) { delta[k] = p[k] - h[k]; cut = Mathf.Min(cut, delta[k]); fill = Mathf.Max(fill, delta[k]); }
            float x0 = pts.Min(q => q.x) - 8f, x1 = pts.Max(q => q.x) + 8f, z0 = pts.Min(q => q.y) - 8f, z1 = pts.Max(q => q.y) + 8f;
            EditTerrain(terrain, Rect.MinMaxRect(x0, z0, x1, z1), (x, z, y) =>
            {
                var q = new Vector2(x, z); float best = float.MaxValue, bs = 0f;
                for (int k = 0; k + 1 < n; k++)
                {
                    var ab = pts[k + 1] - pts[k]; float t = Mathf.Clamp01(Vector2.Dot(q - pts[k], ab) / ab.sqrMagnitude);
                    float dd = (pts[k] + ab * t - q).magnitude;
                    if (dd < best) { best = dd; bs = k + t; }
                }
                float w = Smooth01((8f - best) / 3f);
                if (w <= 0f) return y;
                int k0 = Mathf.Clamp((int)bs, 0, n - 1), k1 = Mathf.Min(k0 + 1, n - 1);
                return y + w * Mathf.Lerp(delta[k0], delta[k1], bs - k0);
            });
            Debug.Log($"[Round17] BUG-002 approach: the crest on Chastain Corner Northeast north of the junction rounded (cut up to {-cut:0.00} m, fill up to {fill:0.00} m)");
        }

        static float Smooth01(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

        // every road segment with a point within 'reach' of c (and passing 'keep'), with its half width
        static List<(Vector2 a, Vector2 b, float half)> RoadSegsNear(MapData map, Vector2 c, float reach, Func<int, bool> keep)
        {
            var list = new List<(Vector2, Vector2, float)>();
            for (int ri = 0; ri < map.roads.Length; ri++)
            {
                var r = map.roads[ri];
                if (r.points == null || r.points.Length < 4 || !keep(ri)) continue;
                var p = ToPoints(r.points);
                if (!p.Any(q => (q - c).sqrMagnitude < reach * reach) && !SegsNear(p, c, reach)) continue;
                for (int i = 0; i + 1 < p.Count; i++) list.Add((p[i], p[i + 1], r.width * 0.5f));
            }
            return list;
        }
        static bool SegsNear(List<Vector2> p, Vector2 c, float reach)
        {
            for (int i = 0; i + 1 < p.Count; i++) if (SegDist(c, p[i], p[i + 1]) < reach) return true;
            return false;
        }
        static float SegDist(Vector2 q, Vector2 a, Vector2 b)
        {
            var ab = b - a; float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(q - a, ab) / ab.sqrMagnitude);
            return (a + ab * t - q).magnitude;
        }
        // distance past the nearest road edge (negative on the pavement)
        static float Clearance(List<(Vector2 a, Vector2 b, float half)> segs, Vector2 q)
        {
            float best = float.MaxValue;
            foreach (var s in segs) best = Mathf.Min(best, SegDist(q, s.a, s.b) - s.half);
            return best;
        }

        // a square-celled grid of world samples (x east, z north) for smoothing the ground
        sealed class HeightGrid
        {
            public readonly Vector2 O; public readonly int NX, NZ; public readonly float Step;
            public HeightGrid(Vector2 origin, int n, float step) : this(origin, n, n, step) { }
            public HeightGrid(Vector2 origin, int nx, int nz, float step) { O = origin; NX = nx; NZ = nz; Step = step; }
            public int I(float x) => Mathf.Clamp(Mathf.RoundToInt((x - O.x) / Step), 0, NX - 1);
            public int J(float z) => Mathf.Clamp(Mathf.RoundToInt((z - O.y) / Step), 0, NZ - 1);
            public float[,] Map(Func<float, float, float> f)
            {
                var a = new float[NX, NZ];
                for (int i = 0; i < NX; i++) for (int j = 0; j < NZ; j++) a[i, j] = f(O.x + i * Step, O.y + j * Step);
                return a;
            }
            public float Sample(float[,] a, float x, float z)
            {
                float fx = Mathf.Clamp((x - O.x) / Step, 0f, NX - 1.001f), fz = Mathf.Clamp((z - O.y) / Step, 0f, NZ - 1.001f);
                int i = (int)fx, j = (int)fz; float u = fx - i, v = fz - j;
                return Mathf.Lerp(Mathf.Lerp(a[i, j], a[i + 1, j], u), Mathf.Lerp(a[i, j + 1], a[i + 1, j + 1], u), v);
            }
            // a Gaussian of 'h' that counts only the samples where mask is 1 (normalized convolution); where no masked sample is
            // near, the sample keeps its own height
            public float[,] NormalizedBlur(float[,] h, float[,] mask, float sigma)
            {
                var hm = new float[NX, NZ];
                for (int i = 0; i < NX; i++) for (int j = 0; j < NZ; j++) hm[i, j] = h[i, j] * mask[i, j];
                var num = Blur(hm, sigma / Step); var den = Blur(mask, sigma / Step);
                var o = new float[NX, NZ];
                for (int i = 0; i < NX; i++) for (int j = 0; j < NZ; j++) o[i, j] = den[i, j] > 1e-3f ? num[i, j] / den[i, j] : h[i, j];
                return o;
            }
            float[,] Blur(float[,] a, float s)
            {
                int r = Mathf.CeilToInt(3f * s);
                var k = new float[2 * r + 1]; for (int t = -r; t <= r; t++) k[t + r] = Mathf.Exp(-0.5f * t * t / (s * s));
                var tmp = new float[NX, NZ]; var o = new float[NX, NZ];
                for (int i = 0; i < NX; i++) for (int j = 0; j < NZ; j++)
                    { float acc = 0f; for (int t = -r; t <= r; t++) { int ii = i + t; if (ii >= 0 && ii < NX) acc += a[ii, j] * k[t + r]; } tmp[i, j] = acc; }
                for (int i = 0; i < NX; i++) for (int j = 0; j < NZ; j++)
                    { float acc = 0f; for (int t = -r; t <= r; t++) { int jj = j + t; if (jj >= 0 && jj < NZ) acc += tmp[i, jj] * k[t + r]; } o[i, j] = acc; }
                return o;
            }
        }
    }
}
