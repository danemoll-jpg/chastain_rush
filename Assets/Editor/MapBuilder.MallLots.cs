using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Round 60 (Part A1, "parking lots and the ring road as they are"): Town Center's parking. OSM maps none round the mall (its surface lots are not
// drawn there; only the three decks to the north), so the mall stood in grass where Dan's photos show the big lots. Here: the ground between the
// mall and its ring road paved as the businesses' lots are (the lot's asphalt draped on the ground, stall rows where the ground is level, Round 34's
// rule; light poles that break and light at night), in the mall's frame:
//   cells of 4 m within 140 m of the mall, not in or within 3 m of its walls or any other building, not on or within 1.5 m of any road (the ring
//   road and the service roads stay as they are, on top), not in water, and only on the mall's side of every road that is not a service road
//   (the straight line from the cell to the mall crosses none: nothing past the ring road).
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        static void PlanMallLots(MallPlan m, MapData map)
        {
            const float Cell = MallPlan.LotCell, Reach = 140f;
            var rings = m.Rings.Values.ToList();
            var uv = rings.SelectMany(r => r).Select(m.UV).ToList();
            float u0 = uv.Min(q => q.x) - Reach, u1 = uv.Max(q => q.x) + Reach, v0 = uv.Min(q => q.y) - Reach, v1 = uv.Max(q => q.y) + Reach;
            int nu = Mathf.CeilToInt((u1 - u0) / Cell), nv = Mathf.CeilToInt((v1 - v0) / Cell);
            var all = new List<(Vector2 a, Vector2 b, float half)>(); var through = new List<(Vector2 a, Vector2 b)>();
            foreach (var r in map.roads)
            {
                var p = ToPoints(r.points);
                for (int i = 0; i + 1 < p.Count; i++) { all.Add((p[i], p[i + 1], r.width * .5f)); if (r.@class != "service") through.Add((p[i], p[i + 1])); }
            }
            var roadGrid = new SegGrid(all, 3f);
            var near = through.Where(s => { var a = m.UV(s.a); var b = m.UV(s.b); return Mathf.Max(a.x, b.x) > u0 && Mathf.Min(a.x, b.x) < u1 && Mathf.Max(a.y, b.y) > v0 && Mathf.Min(a.y, b.y) < v1; }).ToList();
            var others = new List<List<Vector2>>();
            for (int i = 0; i < map.buildings.Length; i++)
            {
                if (m.Owns(i)) continue; var r = ToPoints(map.buildings[i].footprint); if (r.Count < 3) continue;
                var c = m.UV(r[0]); if (c.x < u0 - 20 || c.x > u1 + 20 || c.y < v0 - 20 || c.y > v1 + 20) continue; others.Add(r);
            }
            var water = (map.water ?? new Area[0]).Select(w => ToPoints(w.outline)).Where(w => w.Count > 2).ToList();
            static bool Cross(Vector2 p, Vector2 q, Vector2 a, Vector2 b)
            {
                float d1 = (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x), d2 = (b.x - a.x) * (q.y - a.y) - (b.y - a.y) * (q.x - a.x);
                float d3 = (q.x - p.x) * (a.y - p.y) - (q.y - p.y) * (a.x - p.x), d4 = (q.x - p.x) * (b.y - p.y) - (q.y - p.y) * (b.x - p.x);
                return d1 * d2 < 0f && d3 * d4 < 0f;
            }
            var on = new bool[nu, nv]; int cells = 0;
            for (int i = 0; i < nu; i++)
                for (int j = 0; j < nv; j++)
                {
                    float cu = u0 + (i + .5f) * Cell, cv = v0 + (j + .5f) * Cell; var w = m.W(cu, cv);
                    if (rings.Any(r => InPolygon(w, r))) continue;
                    float dm = rings.Min(r => RingDist(w, r)); if (dm < 3f || dm > Reach) continue;
                    if (roadGrid.Clearance(w) < 1.5f + Cell * .5f) continue;
                    if (others.Any(r => InPolygon(w, r) || RingDist(w, r) < 3f)) continue;
                    if (water.Any(r => InPolygon(w, r) || RingDist(w, r) < 3f)) continue;
                    // the nearest point of the mall, and no through road between
                    Vector2 best = w; float bd = float.MaxValue;
                    foreach (var r in rings) { var q = Closest(r, w); float d = (q - w).sqrMagnitude; if (d < bd) { bd = d; best = q; } }
                    if (near.Any(s => Cross(w, best, s.a, s.b))) continue;
                    on[i, j] = true; cells++;
                }
            m.LotOn = on; m.LotU0 = u0; m.LotV0 = v0; m.LotCells = cells;
        }

        static void MallLots(MallPlan m, MapData map, TerrainGrid t, Materials mats, Transform root)
        {
            const float Cell = MallPlan.LotCell; var on = m.LotOn; if (on == null) return; int nu = on.GetLength(0), nv = on.GetLength(1); float u0 = m.LotU0, v0 = m.LotV0, u1 = u0 + nu * Cell, v1 = v0 + nv * Cell; int cells = m.LotCells;
            var all = new List<(Vector2 a, Vector2 b, float half)>(); foreach (var r in map.roads) { var p = ToPoints(r.points); for (int i = 0; i + 1 < p.Count; i++) all.Add((p[i], p[i + 1], r.width * .5f)); }
            var roadGrid = new SegGrid(all, 3f);
            // merged into rectangles (rows first, then equal rows stacked), each draped as a lot
            var lot = new Chunks { Tint = LotAsphalt }; var paint = new Chunks(); var used = new bool[nu, nv]; int rects = 0;
            for (int j = 0; j < nv; j++)
                for (int i = 0; i < nu; i++)
                {
                    if (!on[i, j] || used[i, j]) continue;
                    int i1 = i; while (i1 + 1 < nu && on[i1 + 1, j] && !used[i1 + 1, j]) i1++;
                    int j1 = j; bool grow = true;
                    while (grow && j1 + 1 < nv) { for (int k = i; k <= i1; k++) if (!on[k, j1 + 1] || used[k, j1 + 1]) { grow = false; break; } if (grow) j1++; }
                    for (int a = i; a <= i1; a++) for (int b = j; b <= j1; b++) used[a, b] = true;
                    float ua = u0 + i * Cell, ub = u0 + (i1 + 1) * Cell, va = v0 + j * Cell, vb = v0 + (j1 + 1) * Cell;
                    var ring = new List<Vector2> { m.W(ua, va), m.W(ub, va), m.W(ub, vb), m.W(ua, vb) };
                    var mid = m.W((ua + ub) * .5f, (va + vb) * .5f); var mb = lot.At(mid);
                    DrapePoly(mb, ring, 2f, .05f * vScale, t, 14); rects++;
                }
            bool On(float u, float v) { int i = Mathf.FloorToInt((u - u0) / Cell), j = Mathf.FloorToInt((v - v0) / Cell); return i >= 0 && j >= 0 && i < nu && j < nv && on[i, j]; }
            // stall rows along the mall (u), double rows 18 m apart (5.2 m stalls either side of the row's line, a 7.6 m aisle between rows)
            int stalls = 0, sloped = 0;
            for (float v = v0 + 9f; v < v1 - 9f; v += 18f)
                for (float u = u0 + 2f; u < u1 - 2f; u += 2.8f)
                    foreach (float side in new[] { -1f, 1f })
                    {
                        Vector2 p0 = m.W(u, v), p2 = m.W(u + 2.8f, v), p1 = m.W(u, v + 5.2f * side), p3 = m.W(u + 2.8f, v + 5.2f * side);
                        if (!On(u, v) || !On(u + 2.8f, v) || !On(u, v + 5.2f * side) || !On(u + 2.8f, v + 5.2f * side)) continue;
                        if (roadGrid.Clearance((p0 + p3) * .5f) < 2f || !StallLevel(p0, p1, p2, p3, t, ref sloped)) continue;
                        PaintLine(paint.At(p0), m.W(u, v + .1f * side), p1, .12f, .065f * vScale, t); stalls++;
                    }
            // light poles on a 36 m grid (breakable; lit at night as the lots' are: Kind.Lamp)
            var lamps = new Breakables(); int poles = 0; var dark = new Color(.20f, .20f, .22f);
            for (float u = u0 + 18f; u < u1; u += 36f)
                for (float v = v0 + 9f; v < v1; v += 36f)
                {
                    if (!On(u, v) || !On(u + 3f, v) || !On(u - 3f, v)) continue;
                    var p = m.W(u, v); if (roadGrid.Clearance(p) < 3f) continue; float gy = Ground(t, p.x, p.y); var n = m.V;
                    var lb = lamps.Begin(p);
                    lamps.Piece(() => Post(lb, new Vector3(p.x, gy - .1f, p.y), 5.6f, .2f, dark), gy + .1f);
                    lamps.Piece(() => Post(lb, new Vector3(p.x, gy + 5.5f, p.y), 5.4f, .16f, dark), gy + .1f);
                    lamps.Piece(() =>
                    {
                        foreach (float s in new[] { -1f, 1f })
                        { var head = new Vector3(p.x, gy + 10.8f, p.y) + new Vector3(n.x, 0f, n.y) * (1f * s); Beam(lb, new Vector3(p.x, gy + 10.75f, p.y), head, .09f, .09f, dark); BoxF(lb, MakeFrame(head, n * s, 1f), -.5f, .5f, -.18f, .1f, -.3f, .3f, dark); }
                    }, gy + .1f);
                    lamps.End(new Vector3(p.x, gy + 1.2f, p.y), m.D3(1, 0), .6f, 2.4f); poles++;
                }
            lot.ToObjects("MallLots", mats.Driveway, root, false);
            paint.ToObjects("MallLotPaint", mats.Paint, root, false);
            if (poles > 0) lamps.ToObjects("MallLotLamps", mats.House, root, 1f, BreakSounds.Kind.Lamp);
            m.Log.Add($"the lots: {cells} cells of {Cell:0} m ({cells * Cell * Cell / 10000f:0.0} ha) in {rects} rectangles, {stalls} stalls ({sloped} left unpainted on a slope), {poles} light poles");
        }
    }
}
