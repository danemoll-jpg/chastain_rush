using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Round 61 (Part B, the map extension stage 2): Cobb County International Airport (McCollum Field) and Kennesaw Mountain's trails, from
// map.json's "aeroways" and "trails" (mapgen_extend.aeroways_and_trails). World, not game layer; nothing here is hand-placed.
//   the airport   "the runway, taxiways, aprons and hangars as generated, drivable (no planes needed this round)": each runway and taxiway
//                 levelled across its width to its own centerline blurred along it (LevelAlong: a runway over 25 m, a taxiway over 8 m),
//                 each apron graded to the plane that fits its ground best; paved (dark asphalt), the runways' white centerline dashes
//                 (36 m on, 24 m off) and edge lines, the taxiways' yellow centerline. The hangars are OSM's buildings, dressed as the rest.
//   the trails    "the trails as paths" (and the standing rule "Paths look like the real ones": at their real width, lying flat on graded
//                 ground, with smooth drivable grass beside it): each trail levelled across its width (blurred over 3 m) with 0.75 m of
//                 smooth ground either side, a packed-dirt tread; never touching a road (the levelling fades out within 8 m of one).
//   no trees on any of them (the new land's trees are kept off by mapgen; this also clears stage 1's on the runway's east end).
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        [Serializable] class Aeroway { public string kind, name; public float width; public float[] points, outline; }
        [Serializable] class TrailLine { public string name, kind; public float width; public float[] points; }

        sealed class AirportPlan
        {
            public readonly List<(string kind, List<Vector2> p, float half)> Lines = new List<(string, List<Vector2>, float)>();
            public readonly List<List<Vector2>> Aprons = new List<List<Vector2>>();
            public readonly List<(List<Vector2> p, float half)> Trails = new List<(List<Vector2>, float)>();
            readonly Dictionary<Vector2Int, List<(Vector2 a, Vector2 b, float half)>> grid = new Dictionary<Vector2Int, List<(Vector2, Vector2, float)>>();
            const float Cell = 32f;
            public void Index(List<Vector2> p, float half)
            {
                for (int i = 0; i + 1 < p.Count; i++)
                {
                    var lo = Vector2.Min(p[i], p[i + 1]) - Vector2.one * half; var hi = Vector2.Max(p[i], p[i + 1]) + Vector2.one * half;
                    for (int cx = Mathf.FloorToInt(lo.x / Cell); cx <= Mathf.FloorToInt(hi.x / Cell); cx++)
                        for (int cz = Mathf.FloorToInt(lo.y / Cell); cz <= Mathf.FloorToInt(hi.y / Cell); cz++)
                        { var k = new Vector2Int(cx, cz); if (!grid.TryGetValue(k, out var l)) grid[k] = l = new List<(Vector2, Vector2, float)>(); l.Add((p[i], p[i + 1], half)); }
                }
            }
            public bool RejectTree(float x, float z)
            {
                var q = new Vector2(x, z);
                if (grid.TryGetValue(new Vector2Int(Mathf.FloorToInt(x / Cell), Mathf.FloorToInt(z / Cell)), out var segs))
                    foreach (var (a, b, half) in segs) { var ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(q - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f)); if ((a + ab * t - q).sqrMagnitude < half * half) return true; }
                foreach (var ap in Aprons) if (InPolygon(q, ap)) return true;
                return false;
            }
        }
        static AirportPlan Airport;

        static AirportPlan PlanAirport(MapData map, TerrainGrid terrain)
        {
            if ((map.aeroways == null || map.aeroways.Length == 0) && (map.trails == null || map.trails.Length == 0)) return null;
            var plan = new AirportPlan(); var pave = new Pavement(map);
            float[] Weights(List<Vector2> p, float from, float to) { var w = new float[p.Count]; for (int i = 0; i < p.Count; i++) w[i] = Smooth01((pave.Clearance(p[i], to + 2f) - from) / (to - from)); return w; }
            float[] Raw(List<Vector2> p) { var r = new float[p.Count]; for (int i = 0; i < p.Count; i++) r[i] = terrain.Height(p[i].x, p[i].y); return r; }
            int runways = 0, taxiways = 0; float most = 0f;
            foreach (var a in map.aeroways ?? new Aeroway[0])
            {
                if (a.kind == "apron") { var ring = ToPoints(a.outline); if (ring.Count >= 3) plan.Aprons.Add(ring); continue; }
                var line = ToPoints(a.points); if (line.Count < 2) continue;
                var p = Resample(line, 1f); if (p.Count < 3) continue; float half = a.width * .5f;
                bool runway = a.kind == "runway";
                LevelAlong(terrain, p, Raw(p), Weights(p, 2f, 8f), half, runway ? 25f : 8f, runway ? 3f : 1.5f, runway ? 12f : 8f);
                plan.Lines.Add((a.kind, p, half)); plan.Index(p, half + 3f);
                if (runway) runways++; else taxiways++;
            }
            // the aprons: each on the plane that fits its ground (least squares), eased back over 8 m
            foreach (var ring in plan.Aprons)
            {
                var r = Rect.MinMaxRect(ring.Min(q => q.x), ring.Min(q => q.y), ring.Max(q => q.x), ring.Max(q => q.y));
                double sx = 0, sz = 0, sy = 0, sxx = 0, szz = 0, sxz = 0, sxy = 0, szy = 0; int n = 0; var c = r.center;
                for (float x = r.xMin; x <= r.xMax; x += 2f) for (float z = r.yMin; z <= r.yMax; z += 2f)
                {
                    var q = new Vector2(x, z); if (!InPolygon(q, ring)) continue; double dx = x - c.x, dz = z - c.y, y = terrain.Height(x, z);
                    sx += dx; sz += dz; sy += y; sxx += dx * dx; szz += dz * dz; sxz += dx * dz; sxy += dx * y; szy += dz * y; n++;
                }
                if (n < 10) continue;
                // [n sx sz; sx sxx sxz; sz sxz szz] [a b c] = [sy sxy szy]
                var m = new double[3, 4] { { n, sx, sz, sy }, { sx, sxx, sxz, sxy }, { sz, sxz, szz, szy } };
                for (int i = 0; i < 3; i++) { int piv = i; for (int k = i + 1; k < 3; k++) if (Math.Abs(m[k, i]) > Math.Abs(m[piv, i])) piv = k; for (int j = 0; j < 4; j++) { var tmp = m[i, j]; m[i, j] = m[piv, j]; m[piv, j] = tmp; } for (int k = 0; k < 3; k++) { if (k == i || Math.Abs(m[i, i]) < 1e-9) continue; double f = m[k, i] / m[i, i]; for (int j = 0; j < 4; j++) m[k, j] -= f * m[i, j]; } }
                double A = m[0, 3] / m[0, 0], B = m[1, 3] / m[1, 1], C = m[2, 3] / m[2, 2];
                const float Ease = 8f;
                EditTerrain(terrain, new Rect(r.xMin - Ease, r.yMin - Ease, r.width + 2f * Ease, r.height + 2f * Ease), (x, z, y) =>
                {
                    var q = new Vector2(x, z); float d = InPolygon(q, ring) ? 0f : RingDist(q, ring); if (d >= Ease) return y;
                    float target = (float)(A + B * (x - c.x) + C * (z - c.y)); float ny = Mathf.Lerp(target, y, Smooth01(d / Ease));
                    most = Mathf.Max(most, Mathf.Abs(ny - y)); return ny;
                });
            }
            float trailLen = 0f;
            foreach (var tl in map.trails ?? new TrailLine[0])
            {
                var line = ToPoints(tl.points); if (line.Count < 2) continue;
                var p = Resample(line, 1f); if (p.Count < 3) continue; float half = Mathf.Max(0.6f, tl.width * .5f);
                LevelAlong(terrain, p, Raw(p), Weights(p, 3f, 8f), half, 3f, 0.75f, 2.5f);
                plan.Trails.Add((p, half)); plan.Index(p, half + 1f); trailLen += p.Count - 1;
            }
            Debug.Log($"[Round61] B: the airport: {runways} runways, {taxiways} taxiways levelled across their width, {plan.Aprons.Count} aprons on their planes (moved up to {most:0.00} m); {plan.Trails.Count} trails, {trailLen:0} m, levelled at their real width");
            return plan;
        }

        static void BuildAirport(TerrainGrid t, Materials mats, Transform world, AirportPlan plan)
        {
            if (plan == null) return;
            var root = new GameObject("Airport and trails").transform; root.SetParent(world, false);
            var pave = new Chunks { Tint = new Color(0.25f, 0.25f, 0.26f) }; var paint = new Chunks(); var dirt = new Chunks { Tint = new Color(0.47f, 0.38f, 0.27f) };
            float lift = 0.05f * vScale;
            foreach (var ring in plan.Aprons) { Vector2 c = Vector2.zero; foreach (var q in ring) c += q; c /= ring.Count; DrapePoly(pave.At(c), ring, 2f, lift, t, 12); }
            void Line(MeshBuilder mb, Vector2 a, Vector2 b, float off, float w, Color col) { mb.Tint = col; var d = (b - a).normalized; var n = new Vector2(-d.y, d.x); Strip(mb, a, b, n, off - w / 2f, off + w / 2f, lift + 0.015f * vScale, t); }
            var white = new Color(0.92f, 0.92f, 0.90f); var yellow = new Color(0.93f, 0.74f, 0.18f);
            foreach (var (kind, p, half) in plan.Lines)
            {
                foreach (var run in Runs(p, 100f)) { var d = Densify(run, 2f); RibbonGridMax(pave.At(d[d.Count / 2]), d, Enumerable.Repeat(half * 2f, d.Count).ToArray(), lift, t, Mathf.Max(2, Mathf.CeilToInt(half * 2f / 3f))); }
                float s = 0f;
                for (int i = 1; i < p.Count; i++, s += 1f)
                {
                    var a = p[i - 1]; var b = p[i]; var mb = paint.At(a);
                    if (kind == "runway")
                    {
                        if (s % 60f < 36f && s > 60f && s < p.Count - 61) Line(mb, a, b, 0f, 0.9f, white);   // the centerline dashes, none over the ends
                        Line(mb, a, b, half - 1.2f, 0.9f, white); Line(mb, a, b, -(half - 1.2f), 0.9f, white);   // the edge lines
                    }
                    else Line(mb, a, b, 0f, 0.3f, yellow);
                }
            }
            foreach (var (p, half) in plan.Trails)
                foreach (var run in Runs(p, 100f)) { var d = Densify(run, 1f); RibbonGridMax(dirt.At(d[d.Count / 2]), d, Enumerable.Repeat(half * 2f, d.Count).ToArray(), 0.04f * vScale, t, 1); }
            pave.ToObjects("Airport pavement", mats.Driveway, root, false);
            paint.ToObjects("Airport paint", mats.Paint, root, false);
            dirt.ToObjects("Trail tread", mats.Driveway, root, false);
        }
    }
}
