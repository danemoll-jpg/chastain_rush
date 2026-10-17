using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Round 39 (A4, BUG-004, Dan at x 2678.77 z 1633.55 on the service road off Courson Street: "this looks like it should be a pool
// and tennis courts here"): the swim and tennis amenity off Courson Street Northeast (the Guidhall Trail neighborhood). World only,
// no game layer. Placed from OSM (the clubhouse 1711, the pool house 1545, the two shade canopies 2100 / 2101, OSM's parking lot,
// the service road into it) and NAIP (the pool deck, the pools, the four courts, the path), read off raw/naip_4band.tif; Dan's
// picture is Google imagery and was only looked at for what is there (never traced or measured). Built the way Chastain Lakes'
// amenity is (MapBuilder.ChastainLakes.cs, Round 32), in the style of the Enclave's pool and courts:
//   - the clubhouse (1711): two stories, siding and a gray gable roof like the houses, its gable end and front door toward
//     Courson Street, glass doors onto the deck
//   - the pool deck behind it (east), level, with a 16 x 11 m lap pool (five lanes, lane lines and ropes) and a free-form wading
//     pool joined at its east end, one water (the Glen pool's: ShallowWater, out up the shallow end), loungers and tables round it
//     (breakable), an iron fence round the deck (breakable, the Glen's fences) closed by the buildings' walls, a gate to the path
//   - the pool house (1545) on the deck's south side: one story, hipped roof
//   - the lot (OSM's outline, paved as it lies) with angled stalls in two rows along the drive, two tree islands, light poles, a
//     crosswalk across the drive at Courson Street
//   - a path from the lot past the pool house down to the courts
//   - four tennis courts in one row (blue on a green surround, nets, one 3 m chain-link fence round all four, breakable, two
//     gates), the two shade canopies (2100, 2101: blue fabric on posts) with benches under them, breakable
namespace ChastainRush
{
    public static partial class MapBuilder
    {
        static System.Func<int, bool> CoursonOwns = _ => false;              // buildings this area dresses (the business dressing skips them)
        static System.Func<Vector2, bool> CoursonLotAt = _ => false;         // its lot (the business dressing leaves OSM's lot to it; the drive is paved as the lot)
        static List<Vector2> CoursonLapPool;                                  // the lap pool's rectangle (MapBuilder.Life.cs: people on its deck, as the Enclave's)

        sealed class CoursonSwim
        {
            // the site's frame: the clubhouse's sides (OSM 1711), U toward the pool (east-south-east), V north-north-east
            public static readonly Vector2 O = new Vector2(2735f, 1667f), U = new Vector2(0.919f, -0.393f).normalized, V = new Vector2(0.393f, 0.918f).normalized;
            public static readonly (string name, float x, float z, float yaw)[] Views =
            {
                ("r39_a4_bug004", 2678.77f, 1633.55f, 101.6f), ("r39_a4_lot_to_pool", 2700f, 1640f, 60f), ("r39_a4_courts", 2712f, 1626f, 150f),
            };
            public int Clubhouse = -1, PoolHouse = -1; public readonly List<int> Canopies = new List<int>();
            public List<Vector2> ClubRing, PoolHouseRing; public readonly List<List<Vector2>> CanopyRings = new List<List<Vector2>>();
            // the deck (u0 at the clubhouse's back wall, v0 at the pool house's north wall), the pool on it (frame coordinates)
            public float DeckU0 = -14f, DeckU1 = 19f, DeckV0 = -15f, DeckV1 = 15f, DeckY;
            public List<Vector2> Pool;     // world, CCW: the lap pool with the wading pool's lobe at its east end
            public const float LapU0 = -9f, LapU1 = 7f, LapV0 = -5f, LapV1 = 6f;
            public const float GateU = -4f, GateHalf = 1.6f;   // the deck's gate on its south side, toward the path
            // the courts: one row of four, its own frame (NAIP)
            public static readonly Vector2 CourtC = new Vector2(2722f, 1604.1f), CU = new Vector2(0.888f, 0.459f).normalized, CV = new Vector2(-0.459f, 0.888f).normalized;
            public const float CourtHU = 30.5f, CourtHV = 17.2f; public float CourtY;
            public List<Vector2> Lot; public int LotIndex = -1; public float LotY;
            public static readonly List<Vector2> Aisle = new List<Vector2> { new Vector2(2701.24f, 1651.67f), new Vector2(2697.63f, 1643.32f), new Vector2(2689.43f, 1630.76f), new Vector2(2680.48f, 1620.21f) };   // OSM's drive through the lot
            public const float LotHalf = 9f, LotGrade = 0.04f;
            public readonly List<Vector2> Path = new List<Vector2> { new Vector2(2705.5f, 1642.0f), new Vector2(2710.5f, 1640.0f), new Vector2(2713.0f, 1634.0f), new Vector2(2712.3f, 1626.0f), new Vector2(2713.6f, 1620.0f) };
            public const float PathW = 1.8f;
            public List<Vector2> DriveLeg;   // the service road's first leg, Courson Street to the lot (the crosswalk)
            public readonly List<Vector2> Trees = new List<Vector2>();
            public readonly List<(Vector2 a, Vector2 b, float half)> Roads = new List<(Vector2, Vector2, float)>();
            public readonly List<List<Vector2>> OtherRings = new List<List<Vector2>>();
            public readonly List<string> Log = new List<string>();

            public static Vector2 W(float u, float v) => O + U * u + V * v;
            public static Vector2 F(Vector2 p) { var d = p - O; return new Vector2(Vector2.Dot(d, U), Vector2.Dot(d, V)); }
            public static Vector2 CW(float u, float v) => CourtC + CU * u + CV * v;
            public static Vector2 CF(Vector2 p) { var d = p - CourtC; return new Vector2(Vector2.Dot(d, CU), Vector2.Dot(d, CV)); }
            public bool InDeck(Vector2 p, float m) { var q = F(p); return q.x > DeckU0 - m && q.x < DeckU1 + m && q.y > DeckV0 - m && q.y < DeckV1 + m; }
            public static bool InCourts(Vector2 p, float m) { var q = CF(p); return Mathf.Abs(q.x) < CourtHU + m && Mathf.Abs(q.y) < CourtHV + m; }
            public bool Owns(int i) => i == Clubhouse || i == PoolHouse || Canopies.Contains(i);
            public List<Vector2> DeckRing => new List<Vector2> { W(DeckU0, DeckV0), W(DeckU1, DeckV0), W(DeckU1, DeckV1), W(DeckU0, DeckV1) };
            public List<Vector2> CourtRing => new List<Vector2> { CW(-CourtHU, -CourtHV), CW(CourtHU, -CourtHV), CW(CourtHU, CourtHV), CW(-CourtHU, CourtHV) };

            public bool Blocks(Vector2 p, float m)
            {
                if (p.x < 2650f || p.x > 2775f || p.y < 1570f || p.y > 1700f) return false;
                if (InDeck(p, m + 0.8f) || InCourts(p, m) || (Lot != null && (InPolygon(p, Lot) || RingDist(p, Lot) < m))) return true;
                foreach (var r in new[] { ClubRing, PoolHouseRing }.Concat(CanopyRings)) if (r != null && (InPolygon(p, r) || RingDist(p, r) < m + 0.5f)) return true;
                for (int i = 0; i + 1 < Path.Count; i++) if (SegDist(p, Path[i], Path[i + 1]) < PathW / 2f + m) return true;
                return false;
            }
            public bool RejectTree(float x, float z) => !Trees.Any(q => (q - new Vector2(x, z)).sqrMagnitude < 0.25f) && Blocks(new Vector2(x, z), 1.5f);   // the lot's island trees stand

            // drop any back-yard fence across the area; the pool's iron fence round the deck (0.6 m out), open where a building's wall
            // closes it and at the gate toward the path
            public void PlanFences(List<FenceRun> fences)
            {
                int dropped = fences.RemoveAll(r => r.pts != null && r.pts.Count >= 2 && Densify(r.pts, 1f).Any(p => Blocks(p, 0.5f)));
                float u0 = DeckU0 - 0.6f, u1 = DeckU1 + 0.6f, v0 = DeckV0 - 0.6f, v1 = DeckV1 + 0.6f;
                var loop = Densify(new List<Vector2> { W(GateU + GateHalf, v0), W(u1, v0), W(u1, v1), W(u0, v1), W(u0, v0), W(GateU - GateHalf, v0) }, 0.5f);
                var run = new List<Vector2>(); int runs = 0;
                void Flush() { if (run.Count >= 2) { var fr = new FenceRun { kind = FenceKind.Iron, seed = 39, closed = false }; fr.pts.AddRange(run); fences.Add(fr); runs++; } run = new List<Vector2>(); }
                foreach (var p in loop)
                {
                    bool wall = new[] { ClubRing, PoolHouseRing }.Any(r => r != null && (InPolygon(p, r) || RingDist(p, r) < 0.45f));
                    if (wall) Flush(); else run.Add(p);
                }
                Flush();
                Log.Add($"pool fence in {runs} iron runs (open at the buildings' walls and a {GateHalf * 2f:0.0} m gate toward the path); {dropped} other fence runs kept off the area");
            }
        }

        // ------------------------------------------------------------ planning and grading (before anything samples the ground)

        static CoursonSwim PlanCoursonSwim(MapData map, TerrainGrid t)
        {
            var k = new CoursonSwim();
            List<Vector2> Ring(int i) { var r = ToPoints(map.buildings[i].footprint); if (r.Count > 1 && (r[0] - r[r.Count - 1]).sqrMagnitude < 1e-4f) r.RemoveAt(r.Count - 1); if (PolyArea(r) < 0f) r.Reverse(); return r; }
            // the buildings by where they are (robust to the map's numbering changing)
            for (int i = 0; i < map.buildings.Length; i++)
            {
                var r = Ring(i); if (r.Count < 3) continue; var c = RingCentroid(r);
                if ((c - new Vector2(2711.7f, 1676.4f)).magnitude < 4f && k.Clubhouse < 0) { k.Clubhouse = i; k.ClubRing = r; }
                else if ((c - new Vector2(2722.5f, 1650.8f)).magnitude < 4f && k.PoolHouse < 0) { k.PoolHouse = i; k.PoolHouseRing = r; }
                else if ((c - new Vector2(2718.3f, 1630.2f)).magnitude < 3f || (c - new Vector2(2701.2f, 1621.0f)).magnitude < 3f) { k.Canopies.Add(i); k.CanopyRings.Add(r); }
            }
            if (k.Clubhouse < 0 || k.PoolHouse < 0) { k.Log.Add("the clubhouse or the pool house is not where OSM had it (map changed?): area not built"); Debug.LogWarning("[Round39] Courson Street pool: " + string.Join("; ", k.Log)); return null; }
            CoursonOwns = k.Owns;
            // the deck runs from the clubhouse's back wall to the pool house's north wall
            k.DeckU0 = k.ClubRing.Max(p => CoursonSwim.F(p).x);
            k.DeckV0 = k.PoolHouseRing.Max(p => CoursonSwim.F(p).y);
            // the pool: the lap pool, and the wading pool's lobe off its east end (south of its middle), one CCW ring
            var pool = new List<Vector2> { CoursonSwim.W(CoursonSwim.LapU0, CoursonSwim.LapV0), CoursonSwim.W(CoursonSwim.LapU1, CoursonSwim.LapV0) };
            var lobeC = new Vector2(CoursonSwim.LapU1 + 1.2f, -1.4f); const float lr = 3.4f;
            float a0 = Mathf.PI - Mathf.Acos(1.2f / lr);   // where the circle meets the lap pool's end wall (u = LapU1)
            for (int i = 0; i <= 16; i++)
            {
                float a = Mathf.Lerp(-a0, a0, i / 16f); float rr = lr * (1f + 0.10f * Mathf.Sin(3f * a) * Mathf.Sin(Mathf.PI * i / 16f));   // free-form, a little lumpy
                var q = lobeC + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rr; q.x = Mathf.Max(q.x, CoursonSwim.LapU1);
                pool.Add(CoursonSwim.W(q.x, q.y));
            }
            pool.Add(CoursonSwim.W(CoursonSwim.LapU1, CoursonSwim.LapV1)); pool.Add(CoursonSwim.W(CoursonSwim.LapU0, CoursonSwim.LapV1));
            if (PolyArea(pool) < 0f) pool.Reverse();
            k.Pool = pool;
            CoursonLapPool = new List<Vector2> { CoursonSwim.W(CoursonSwim.LapU0, CoursonSwim.LapV0), CoursonSwim.W(CoursonSwim.LapU1, CoursonSwim.LapV0), CoursonSwim.W(CoursonSwim.LapU1, CoursonSwim.LapV1), CoursonSwim.W(CoursonSwim.LapU0, CoursonSwim.LapV1) };
            // OSM's lot here, and the service road into it
            if (map.parking != null)
                for (int i = 0; i < map.parking.Length; i++)
                {
                    var r = ToPoints(map.parking[i].outline); if (r.Count < 3) continue;
                    if (r.Count > 1 && (r[0] - r[r.Count - 1]).sqrMagnitude < 1e-4f) r.RemoveAt(r.Count - 1);
                    if ((RingCentroid(r) - new Vector2(2692f, 1640f)).magnitude < 15f) { k.LotIndex = i; break; }
                }
            // OSM's outline here is a thin strip; NAIP's paving runs about 9 m either side of the drive's aisle: the lot is that band
            // (its ends square), which covers OSM's outline
            {
                var aisle = CoursonSwim.Aisle; var left = new List<Vector2>(); var right = new List<Vector2>();
                for (int i = 0; i < aisle.Count; i++)
                {
                    var din = i > 0 ? (aisle[i] - aisle[i - 1]).normalized : (aisle[1] - aisle[0]).normalized; var dout = i < aisle.Count - 1 ? (aisle[i + 1] - aisle[i]).normalized : din;
                    var tg = (din + dout).normalized; var n = new Vector2(-tg.y, tg.x); float miter = CoursonSwim.LotHalf / Mathf.Max(0.5f, Vector2.Dot(n, new Vector2(-dout.y, dout.x)));
                    var e = i == 0 ? -din * 1.5f : i == aisle.Count - 1 ? dout * 1.5f : Vector2.zero;
                    left.Add(aisle[i] + n * miter + e); right.Add(aisle[i] - n * miter + e);
                }
                right.Reverse(); var band = new List<Vector2>(left); band.AddRange(right); if (PolyArea(band) < 0f) band.Reverse(); k.Lot = band;
            }
            if (k.Lot != null) { var lot = k.Lot; CoursonLotAt = p => InPolygon(p, lot) || RingDist(p, lot) < 1.5f; }
            foreach (var rd in map.roads)
            {
                var p = ToPoints(rd.points);
                if (rd.@class == "service" && p.Count >= 2 && (p[0] - new Vector2(2674.67f, 1662.29f)).magnitude < 3f && (p[1] - new Vector2(2701.24f, 1651.67f)).magnitude < 3f) k.DriveLeg = new List<Vector2> { p[0], p[1] };
                if (rd.@class == "service") continue;   // the area's own drive is graded with the lot
                for (int i = 0; i + 1 < p.Count; i++) if (BoundsOf(new[] { p[i], p[i + 1] }, 0f).Overlaps(Rect.MinMaxRect(2640f, 1560f, 2785f, 1710f))) k.Roads.Add((p[i], p[i + 1], rd.width * 0.5f));
            }
            for (int i = 0; i < map.buildings.Length; i++)
            {
                if (k.Owns(i)) continue; var r = Ring(i); if (r.Count < 3) continue;
                if (r.Any(p => p.x > 2640f && p.x < 2785f && p.y > 1560f && p.y < 1710f)) k.OtherRings.Add(r);
            }
            // grading: the deck and the courts level (banks of 1 in 2 back to the ground), the lot paved as it lies
            k.DeckY = CoursonMedian(t, (u, v) => CoursonSwim.W(u, v), k.DeckU0, k.DeckU1, k.DeckV0, k.DeckV1);
            // Round 39: level 1.2 m past the deck's edge too (grass poked through its edge where the bank began right at it)
            CoursonLevel(t, k, p => { var q = CoursonSwim.F(p); return (Mathf.Max(0f, k.DeckU0 - q.x - 1.2f, q.x - k.DeckU1 - 1.2f), Mathf.Max(0f, k.DeckV0 - q.y - 1.2f, q.y - k.DeckV1 - 1.2f)); }, k.DeckRing, k.DeckY - 0.04f, 7.2f, "pool deck");
            k.CourtY = CoursonMedian(t, (u, v) => CoursonSwim.CW(u, v), -CoursonSwim.CourtHU, CoursonSwim.CourtHU, -CoursonSwim.CourtHV, CoursonSwim.CourtHV);
            CoursonLevel(t, k, p => { var q = CoursonSwim.CF(p); return (Mathf.Max(0f, Mathf.Abs(q.x) - CoursonSwim.CourtHU), Mathf.Max(0f, Mathf.Abs(q.y) - CoursonSwim.CourtHV)); }, k.CourtRing, k.CourtY, 8f, "courts");
            // the lot: a plane along the aisle falling at most 4 % (stalls are painted only on near-level ground, Round 34), banks 1 in 2
            {
                var e0 = CoursonSwim.Aisle[0]; var e1 = CoursonSwim.Aisle[CoursonSwim.Aisle.Count - 1]; float L = (e1 - e0).magnitude; var dir = (e1 - e0) / L;
                float y0 = Ground(t, e0.x, e0.y), y1 = Ground(t, e1.x, e1.y), g = Mathf.Clamp((y1 - y0) / L, -CoursonSwim.LotGrade, CoursonSwim.LotGrade);
                k.LotY = CoursonMedian(t, (u, v) => (e0 + e1) * 0.5f + dir * u + new Vector2(-dir.y, dir.x) * v, -L / 2f, L / 2f, -CoursonSwim.LotHalf, CoursonSwim.LotHalf);
                var lot = k.Lot; var mid = (e0 + e1) * 0.5f; int changed = 0; float most = 0f;
                EditTerrain(t, BoundsOf(lot, 5f), (x, z, h) =>
                {
                    var p = new Vector2(x, z); float d = InPolygon(p, lot) ? 0f : RingDist(p, lot); if (d > 5f || Clearance(k.Roads, p) < 1f) return h;
                    foreach (var r in k.OtherRings) if (InPolygon(p, r) || RingDist(p, r) < 1.5f) return h;
                    float y = k.LotY + g * Vector2.Dot(p - mid, dir); float nh = d <= 0f ? y : Mathf.Clamp(h, y - d * 0.5f, y + d * 0.5f);
                    if (Mathf.Abs(nh - h) > 1e-3f) { changed++; most = Mathf.Max(most, Mathf.Abs(nh - h)); }
                    return nh;
                });
                k.Log.Add($"lot {L:0} x {CoursonSwim.LotHalf * 2f:0} m along the drive, a plane at {k.LotY:0.00} m falling {Mathf.Abs(g) * 100f:0.0} % (the ground {Mathf.Abs(y1 - y0) / L * 100f:0.0} %; {changed} terrain samples moved, by up to {most:0.00} m)");
            }
            // the lot's two tree islands: where its outline pinches round them (OSM: the notches at about (2692, 1650) and (2698, 1638))
            k.Trees.Add(new Vector2(2692.6f, 1647.6f)); k.Trees.Add(new Vector2(2698.6f, 1636.6f));
            return k;
        }

        static float CoursonMedian(TerrainGrid t, System.Func<float, float, Vector2> w, float u0, float u1, float v0, float v1)
        {
            var s = new List<float>();
            for (float u = u0; u <= u1; u += 1.5f) for (float v = v0; v <= v1; v += 1.5f) { var p = w(u, v); s.Add(Ground(t, p.x, p.y)); }
            s.Sort(); return s[s.Count / 2];
        }

        // level inside a box of a frame ('outside' gives the distance out of the box along each axis), banks back to the ground within
        // reach; never at a road or another building
        static void CoursonLevel(TerrainGrid t, CoursonSwim k, System.Func<Vector2, (float, float)> outside, List<Vector2> ring, float y, float reach, string what, float bank = 0.5f)
        {
            int changed = 0; float most = 0f;
            EditTerrain(t, BoundsOf(ring, reach), (x, z, h) =>
            {
                var p = new Vector2(x, z); var (du, dv) = outside(p); float d = Mathf.Sqrt(du * du + dv * dv);
                if (d > reach || Clearance(k.Roads, p) < 1f) return h;
                foreach (var r in k.OtherRings) if (InPolygon(p, r) || RingDist(p, r) < 1.5f) return h;
                float nh = d <= 0f ? y : Mathf.Clamp(h, y - d * bank, y + d * bank);
                if (Mathf.Abs(nh - h) > 1e-3f) { changed++; most = Mathf.Max(most, Mathf.Abs(nh - h)); }
                return nh;
            });
            k.Log.Add($"{what} level at {y:0.00} m ({changed} terrain samples moved, by up to {most:0.00} m)");
        }

        // ------------------------------------------------------------ the build

        static void BuildCoursonSwim(MapData map, TerrainGrid t, Materials mats, Transform parent, CoursonSwim k)
        {
            if (k == null) return;
            var root = new GameObject("CoursonSwimTennis").transform; root.SetParent(parent, false);
            var mb = new MeshBuilder(); var flat = new MeshBuilder(); var link = new MeshBuilder();
            var lotMb = new Chunks { Tint = LotAsphalt }; var paint = new Chunks(); var walks = new Chunks { Tint = ConcreteLight };
            var furniture = new Breakables(); var fence = new Breakables(); var lamps = new Breakables(); var canopies = new Breakables();
            Color deckC = new Color(0.80f, 0.77f, 0.70f);
            var U3 = new Vector3(CoursonSwim.U.x, 0f, CoursonSwim.U.y); var V3 = new Vector3(CoursonSwim.V.x, 0f, CoursonSwim.V.y);

            // --- the deck and the pool
            float du = (k.DeckU1 - k.DeckU0) / 2f, dv = (k.DeckV1 - k.DeckV0) / 2f; var dc = CoursonSwim.W((k.DeckU0 + k.DeckU1) / 2f, (k.DeckV0 + k.DeckV1) / 2f);
            var f = new Frame { O = new Vector3(dc.x, k.DeckY, dc.y), R = U3, F = V3, M = 1f };
            float lo = float.MaxValue; foreach (var p in Densify(k.DeckRing.Concat(new[] { k.DeckRing[0] }).ToList(), 1.5f)) lo = Mathf.Min(lo, Ground(t, p.x, p.y));
            BoxNoBottom(mb, f, -du, du, Mathf.Min(lo - 0.3f - k.DeckY, -0.3f), 0f, -dv, dv, deckC, deckC, deckC);
            var hc = new HoodCtx { map = map, terrain = t, mats = mats, root = root };
            float surface = BuildSunkPool(hc, mb, f, k.Pool, k.DeckY, du, dv, deckC, root);
            var pr = k.Pool;
            mb.Tint = new Color(0.16f, 0.72f, 0.86f);
            int pv0 = mb.Count; foreach (var p in pr) mb.Vertex(new Vector3(p.x, surface, p.y));
            foreach (var tri in EarClip(pr)) mb.Tri(pv0 + tri.x, pv0 + tri.z, pv0 + tri.y);
            for (int i = 0; i < pr.Count; i++)
            {
                var p0 = pr[i]; var p1 = pr[(i + 1) % pr.Count]; var d = (p1 - p0).normalized; var outN = new Vector2(d.y, -d.x);
                mb.Tint = new Color(0.93f, 0.92f, 0.88f);   // coping
                Face(mb, new Vector3(p0.x, k.DeckY + 0.035f, p0.y), new Vector3(p1.x, k.DeckY + 0.035f, p1.y), new Vector3(p1.x + outN.x * 0.35f, k.DeckY + 0.035f, p1.y + outN.y * 0.35f), new Vector3(p0.x + outN.x * 0.35f, k.DeckY + 0.035f, p0.y + outN.y * 0.35f), Vector3.up);
                mb.Tint = new Color(0.10f, 0.18f, 0.40f);   // the tile line at the waterline
                var inn = -outN * 0.015f;
                Face(mb, new Vector3(p0.x + inn.x, surface - 0.14f, p0.y + inn.y), new Vector3(p1.x + inn.x, surface - 0.14f, p1.y + inn.y), new Vector3(p1.x + inn.x, k.DeckY - 0.01f, p1.y + inn.y), new Vector3(p0.x + inn.x, k.DeckY - 0.01f, p0.y + inn.y), new Vector3(-outN.x, 0f, -outN.y));
            }
            // five lanes along the lap pool's length (U): a dark line down each with its T at the ends, ropes between them
            int lanes = 5; float laneW = (CoursonSwim.LapV1 - CoursonSwim.LapV0) / lanes;
            Vector3 PW(float u, float v, float lift) { var q = CoursonSwim.W(u, v); return new Vector3(q.x, surface + lift, q.y); }
            void WaterQuad(float u0, float u1, float v0, float v1, float lift) => Face(mb, PW(u0, v0, lift), PW(u1, v0, lift), PW(u1, v1, lift), PW(u0, v1, lift), Vector3.up);
            mb.Tint = new Color(0.07f, 0.16f, 0.40f);
            for (int l = 0; l < lanes; l++)
            {
                float cv = CoursonSwim.LapV0 + laneW * (l + 0.5f);
                WaterQuad(CoursonSwim.LapU0 + 2f, CoursonSwim.LapU1 - 2f, cv - 0.12f, cv + 0.12f, 0.004f);
                foreach (float uu in new[] { CoursonSwim.LapU0 + 2f, CoursonSwim.LapU1 - 2f }) WaterQuad(uu - 0.12f, uu + 0.12f, cv - 0.5f, cv + 0.5f, 0.004f);
            }
            for (int l = 1; l < lanes; l++)
            {
                float rv = CoursonSwim.LapV0 + laneW * l;
                for (float u = CoursonSwim.LapU0 + 0.3f; u < CoursonSwim.LapU1 - 0.3f; u += 0.5f)
                    Beam(mb, PW(u, rv, 0.03f), PW(Mathf.Min(u + 0.5f, CoursonSwim.LapU1 - 0.3f), rv, 0.03f), 0.09f, 0.09f, ((int)((u - CoursonSwim.LapU0) / 0.5f) % 4 < 2) ? new Color(0.80f, 0.12f, 0.12f) : LineWhite);
            }
            // loungers all round (striped; facing the water), tables with umbrellas at the corners
            int chairs = 0, tables = 0; Color[] stripe = { new Color(0.15f, 0.40f, 0.62f), new Color(0.88f, 0.86f, 0.80f) };
            var spots = new List<(Vector2 p, Vector2 face, bool table)>();
            for (float u = CoursonSwim.LapU0 + 0.5f; u <= CoursonSwim.LapU1 + 3f; u += 1.4f) { spots.Add((CoursonSwim.W(u, CoursonSwim.LapV1 + 2.3f), -CoursonSwim.V, false)); spots.Add((CoursonSwim.W(u, CoursonSwim.LapV0 - 2.3f), CoursonSwim.V, false)); }
            for (float v = CoursonSwim.LapV0 + 0.5f; v <= CoursonSwim.LapV1 - 0.5f; v += 1.4f) spots.Add((CoursonSwim.W(CoursonSwim.LapU1 + 7.3f, v), -CoursonSwim.U, false));
            foreach (var (u, v) in new[] { (CoursonSwim.LapU0 - 2.5f, CoursonSwim.LapV1 + 2.6f), (CoursonSwim.LapU0 - 2.5f, CoursonSwim.LapV0 - 2.6f), (CoursonSwim.LapU1 + 7.5f, CoursonSwim.LapV1 + 2.8f), (CoursonSwim.LapU1 + 7.5f, CoursonSwim.LapV0 - 2.8f), (k.DeckU1 - 3f, k.DeckV1 - 3f) })
                spots.Add((CoursonSwim.W(u, v), CoursonSwim.U, true));
            foreach (var (p, face, table) in spots)
            {
                if (!k.InDeck(p, -1.0f) || InPolygon(p, k.Pool) || DistToEdges(p, k.Pool) < 1.2f) continue;
                var m = furniture.Begin(p);
                if (table)
                {
                    var g = new Vector3(p.x, k.DeckY, p.y);
                    furniture.Piece(() => { Cyl(m, g + Vector3.up * 0.7f, 0.04f, 0.55f, 0.55f, LineWhite, 10); Cyl(m, g, 0.7f, 0.04f, 0.04f, LineWhite, 5); }, k.DeckY);
                    furniture.Piece(() => { Cyl(m, g, 2.3f, 0.025f, 0.025f, new Color(0.8f, 0.8f, 0.8f), 5); Cyl(m, g + Vector3.up * 2.05f, 0.5f, 1.3f, 0.0f, new Color(0.12f, 0.30f, 0.55f), 8); }, k.DeckY);
                    furniture.End(new Vector3(p.x, k.DeckY + 0.9f, p.y), Vector3.forward, 1.3f, 1.8f, 1.3f);
                    tables++; continue;
                }
                var cf = MakeFrame(new Vector3(p.x, k.DeckY, p.y), face, 1f); var col = stripe[chairs % 2];
                furniture.Piece(() =>
                {
                    for (int s = 0; s < 6; s++) BoxF(m, cf, -0.3f, 0.3f, 0.30f, 0.36f, -0.95f + s * 0.27f, -0.95f + s * 0.27f + 0.25f, s % 2 == 0 ? col : LineWhite);
                    Beam(m, cf.P(-0.3f, 0.36f, -0.95f), cf.P(-0.3f, 0.85f, -1.25f), 0.04f, 0.04f, LineWhite); Beam(m, cf.P(0.3f, 0.36f, -0.95f), cf.P(0.3f, 0.85f, -1.25f), 0.04f, 0.04f, LineWhite);
                    BoxF(m, cf, -0.3f, 0.3f, 0.36f, 0.85f, -1.27f, -1.2f, col);
                    foreach (float uu in new[] { -0.28f, 0.28f }) foreach (float vv in new[] { -0.85f, 0.75f }) BoxF(m, cf, uu - 0.025f, uu + 0.025f, 0f, 0.3f, vv - 0.025f, vv + 0.025f, LineWhite);
                }, k.DeckY);
                furniture.End(new Vector3(p.x, k.DeckY + 0.45f, p.y), new Vector3(face.x, 0f, face.y), 0.7f, 0.9f, 2.2f);
                chairs++;
            }

            // --- the clubhouse (two stories, gray gable roof like the houses) and the pool house (one story, hipped)
            CoursonClubhouse(mb, root, t, k.ClubRing, k.DeckY);
            LakesHouse(mb, root, t, k.PoolHouseRing, CoursonSwim.W(0f, 0f), k.DeckY, 3.1f, 2.0f, new Color(0.82f, 0.78f, 0.68f), new Color(0.38f, 0.39f, 0.41f), true, "Courson pool house");

            // --- the lot (OSM's outline, paved as it lies), the drive paved with it (MapBuilder.cs BuildRoads), angled stalls in two
            // rows along the drive, tree islands, light poles, the crosswalk at Courson Street
            int stalls = 0, sloped = 0;
            if (k.Lot != null)
            {
                DrapePoly(lotMb.At(RingCentroid(k.Lot)), k.Lot, 1f, 0.085f, t, 8);
                var aisle = CoursonSwim.Aisle; float total = 0f; for (int i = 0; i + 1 < aisle.Count; i++) total += (aisle[i + 1] - aisle[i]).magnitude;
                (Vector2 at, Vector2 dir) Along(float s) { for (int i = 0; i + 1 < aisle.Count; i++) { float l = (aisle[i + 1] - aisle[i]).magnitude; if (s <= l || i + 2 == aisle.Count) return (aisle[i] + (aisle[i + 1] - aisle[i]) * Mathf.Clamp01(s / l), (aisle[i + 1] - aisle[i]) / l); s -= l; } return (aisle[0], Vector2.up); }
                {
                    // Round 39: by distance along the whole aisle (not per piece, which lost a stall at every bend)
                    foreach (float side in new[] { -1f, 1f })
                    {
                        for (float s = 1.5f; s <= total - 1.5f; s += 2.9f)
                        {
                            var (a, d) = Along(s); var n = new Vector2(-d.y, d.x); var sn = n * side; var slant = (sn * 0.866f + d * 0.5f).normalized;   // 60 degree stalls, angled with the way in
                            var p0 = a + sn * 2.7f; var p1 = p0 + slant * 5.6f; var p2 = p0 + d * 2.9f; var p3 = p2 + slant * 5.6f;
                            if (!InPolygon(p1, k.Lot) || !InPolygon(p3, k.Lot) || !InPolygon(p0, k.Lot) || k.Trees.Any(q => (q - (p0 + p3) * 0.5f).magnitude < 3f)) continue;
                            if (!StallLevel(p0, p1, p2, p3, t, ref sloped)) continue;
                            PaintLine(paint.At(p0), p0, p1, 0.12f, 0.10f, t); stalls++;
                        }
                    }
                }
                // tree islands: a curbed oval of mulch round each tree
                foreach (var q in k.Trees)
                {
                    var ring = new List<Vector2>(); for (int i = 0; i < 12; i++) { float a = i * Mathf.PI / 6f; ring.Add(q + new Vector2(Mathf.Cos(a) * 1.8f, Mathf.Sin(a) * 1.3f)); }
                    float gy = Ground(t, q.x, q.y);
                    for (int i = 0; i < ring.Count; i++) { var a = ring[i]; var b = ring[(i + 1) % ring.Count]; Beam(mb, new Vector3(a.x, Ground(t, a.x, a.y) + 0.08f, a.y), new Vector3(b.x, Ground(t, b.x, b.y) + 0.08f, b.y), 0.18f, 0.18f, ConcreteLight); }
                    mb.Tint = new Color(0.30f, 0.20f, 0.13f); int v0 = mb.Count; foreach (var p in ring) mb.Vertex(new Vector3(p.x, Ground(t, p.x, p.y) + 0.13f, p.y));
                    foreach (var tri in EarClip(ring)) mb.Tri(v0 + tri.x, v0 + tri.z, v0 + tri.y);
                }
                foreach (var lp in new[] { new Vector2(2704.5f, 1646.5f), new Vector2(2686.0f, 1625.0f) })
                {
                    var p = lp; float gy = Ground(t, p.x, p.y); var dark = new Color(0.20f, 0.20f, 0.22f); var n = new Vector2(-0.835f, 0.55f);
                    var lb = lamps.Begin(p);
                    lamps.Piece(() => Post(lb, new Vector3(p.x, gy - 0.1f, p.y), 4.6f, 0.18f, dark), gy + 0.1f);
                    lamps.Piece(() => Post(lb, new Vector3(p.x, gy + 4.5f, p.y), 3.4f, 0.15f, dark), gy + 0.1f);
                    lamps.Piece(() => { var head = new Vector3(p.x, gy + 7.8f, p.y) + new Vector3(n.x, 0f, n.y) * 0.9f; Beam(lb, new Vector3(p.x, gy + 7.75f, p.y), head, 0.09f, 0.09f, dark); BoxF(lb, MakeFrame(head, n, 1f), -0.5f, 0.5f, -0.18f, 0.1f, -0.3f, 0.3f, dark); }, gy + 0.1f);
                    lamps.End(new Vector3(p.x, gy + 1.2f, p.y), Vector3.right, 0.6f, 2.4f);
                }
            }
            if (k.DriveLeg != null)
            {
                var a = k.DriveLeg[0]; var d = (k.DriveLeg[1] - a).normalized; var n = new Vector2(-d.y, d.x);
                var mid = a + d * 6.5f;   // 2.5 m past Courson Street's edge
                for (float o = -2.1f; o <= 2.11f; o += 0.7f) PaintLine(paint.At(mid), mid + n * o - d * 1.4f, mid + n * o + d * 1.4f, 0.4f, 0.12f, t);
            }

            // --- the path from the lot past the pool house down to the courts
            { var pts = Densify(k.Path, 1f); RibbonGridMax(walks.At(pts[pts.Count / 2]), pts, Enumerable.Repeat(CoursonSwim.PathW, pts.Count).ToArray(), 0.07f, t, 2); }

            // --- the four courts in one row, one fence round all four
            int courts = 0, panels = 0, poles = 0;
            CoursonCourts(flat, link, fence, lamps, t, k.CourtY, ref courts, ref panels, ref poles);

            // --- the shade canopies (blue fabric on four posts) with a bench under each, breakable
            foreach (var ring in k.CanopyRings)
            {
                if (!BestRectangle(ring, out var c, out var cu, out var cv, out float hl, out float hw)) continue;
                float gy = Ground(t, c.x, c.y); var cfm = new Frame { O = new Vector3(c.x, gy, c.y), R = new Vector3(cu.x, 0f, cu.y), F = new Vector3(cv.x, 0f, cv.y), M = 1f };
                var m = canopies.Begin(c); var postC = new Color(0.85f, 0.85f, 0.84f); var blue = new Color(0.12f, 0.36f, 0.66f);
                foreach (float su in new[] { -1f, 1f }) foreach (float sv in new[] { -1f, 1f })
                    { float uu = su * (hl - 0.2f), vv = sv * (hw - 0.2f); canopies.Piece(() => { var q = cfm.P(uu, 0f, vv); Post(m, new Vector3(q.x, Ground(t, q.x, q.z) - 0.1f, q.z), 2.9f, 0.09f, postC); }, gy); }
                canopies.Piece(() =>
                {
                    m.Tint = blue; float y0 = 2.75f, yTop = 3.35f;
                    Face(m, cfm.P(-hl, y0, -hw), cfm.P(hl, y0, -hw), cfm.P(hl, yTop, 0f), cfm.P(-hl, yTop, 0f), Vector3.up);
                    Face(m, cfm.P(-hl, y0, hw), cfm.P(hl, y0, hw), cfm.P(hl, yTop, 0f), cfm.P(-hl, yTop, 0f), Vector3.up);
                    Face(m, cfm.P(-hl, yTop, 0f), cfm.P(hl, yTop, 0f), cfm.P(hl, y0, -hw), cfm.P(-hl, y0, -hw), Vector3.down);
                    Face(m, cfm.P(-hl, yTop, 0f), cfm.P(hl, yTop, 0f), cfm.P(hl, y0, hw), cfm.P(-hl, y0, hw), Vector3.down);
                }, gy);
                canopies.Piece(() => { var wood = new Color(0.50f, 0.36f, 0.24f); BoxF(m, cfm, -hl + 0.8f, hl - 0.8f, 0.42f, 0.48f, -0.25f, 0.25f, wood); foreach (float uu in new[] { -hl + 1.0f, hl - 1.0f }) BoxF(m, cfm, uu - 0.05f, uu + 0.05f, 0f, 0.42f, -0.2f, 0.2f, new Color(0.3f, 0.3f, 0.32f)); }, gy);
                canopies.End(new Vector3(c.x, gy + 1.5f, c.y), new Vector3(cu.x, 0f, cu.y), hw * 2f, 3f, hl * 2f);
            }

            mb.ToObject("CoursonPoolAndBuildings", mats.House, root, false, true);
            flat.ToObject("CoursonCourts", mats.Driveway, root, false, false);
            link.ToObject("CoursonNets", mats.House, root, false, false);
            lotMb.ToObjects("CoursonLot", mats.Driveway, root, false);
            paint.ToObjects("CoursonLotPaint", mats.Paint, root, false);
            walks.ToObjects("CoursonWalks", mats.Sidewalk, root, false);
            furniture.ToObjects("CoursonPoolFurniture", mats.House, root, 0.2f, BreakSounds.Kind.Sign);
            canopies.ToObjects("CoursonShadeCanopies", mats.House, root, 0.6f, BreakSounds.Kind.Sign);
            fence.ToObjects("CoursonCourtFence", mats.House, root, 1.2f, BreakSounds.Kind.Iron);
            lamps.ToObjects("CoursonLamps", mats.House, root, 1f, BreakSounds.Kind.Lamp);
            k.Log.Add($"deck {k.DeckU1 - k.DeckU0:0.0} x {k.DeckV1 - k.DeckV0:0.0} m at {k.DeckY:0.00} m, lap pool {CoursonSwim.LapU1 - CoursonSwim.LapU0:0} x {CoursonSwim.LapV1 - CoursonSwim.LapV0:0} m with the wading pool off its east end (surface {surface:0.00}), {chairs} loungers, {tables} tables; lot {stalls} stalls ({sloped} left unpainted on a slope), {k.Trees.Count} tree islands; {courts} courts at {k.CourtY:0.00} m, {panels} fence panels, {poles} light poles; {k.CanopyRings.Count} shade canopies");
            k.Log.Add(hc.Log.ToString().TrimEnd(' ', ';'));
            Debug.Log("[Round39] Courson Street pool and courts: " + string.Join("; ", k.Log));
        }

        // two stories on the footprint's rectangle, the floor at the deck (block foundation down to the ground), a gray gable roof with
        // its ridge along the long side; the gable end toward Courson Street with the front door under a portico, glass doors onto the deck
        static void CoursonClubhouse(MeshBuilder mb, Transform root, TerrainGrid t, List<Vector2> ring, float floor)
        {
            if (!BestRectangle(ring, out Vector2 c, out Vector2 U, out Vector2 V, out float hl, out float hw)) return;
            if (Vector2.Dot(U, CoursonSwim.U) < 0f) U = -U;   // u toward the pool (the back), -u toward the street (the front)
            V = new Vector2(-U.y, U.x);
            float lo = float.MaxValue; foreach (var p in ring) lo = Mathf.Min(lo, Ground(t, p.x, p.y));
            var f = new Frame { O = new Vector3(c.x, floor, c.y), R = new Vector3(V.x, 0f, V.y), F = new Vector3(-U.x, 0f, -U.y), M = 1f };   // F toward the street; v along the depth
            var siding = new Color(0.80f, 0.76f, 0.66f); var roof = new Color(0.36f, 0.37f, 0.39f); const float H = 5.8f, Rise = 3.2f;
            BoxNoBottom(mb, f, -hw, hw, Mathf.Min(lo - floor - 0.3f, -0.3f), 0f, -hl, hl, FoundationBlock, FoundationBlock, FoundationBlock);
            BoxNoBottom(mb, f, -hw, hw, 0f, H, -hl, hl, siding, siding, siding);
            Gable(mb, f, -hl, hl, -hw, hw, false, H, Rise, 0.5f, 0.4f, roof, siding, true, true, TrimWhite);
            // the front (v = +hl): door, portico, windows on both floors
            FrontQuad(mb, f, -0.9f, 0.9f, 0f, 2.3f, hl + 0.02f, TrimWhite); FrontQuad(mb, f, -0.8f, 0.8f, 0f, 2.2f, hl + 0.04f, new Color(0.25f, 0.32f, 0.42f));
            BoxF(mb, f, -2.0f, 2.0f, 2.6f, 2.8f, hl, hl + 1.8f, roof); foreach (float u in new[] { -1.8f, 1.8f }) BoxF(mb, f, u - 0.1f, u + 0.1f, 0f, 2.6f, hl + 1.6f, hl + 1.8f, TrimWhite);
            foreach (float u in new[] { -hw + 1.6f, hw - 1.6f }) { if (Mathf.Abs(u) < 2.4f) continue; FrontQuad(mb, f, u - 0.6f, u + 0.6f, 0.9f, 2.2f, hl + 0.02f, TrimWhite); FrontQuad(mb, f, u - 0.5f, u + 0.5f, 1.0f, 2.1f, hl + 0.04f, GlassDark); }
            for (float u = -hw + 1.6f; u <= hw - 1.6f; u += Mathf.Max(2.2f, (2f * hw - 3.2f) / 3f)) { FrontQuad(mb, f, u - 0.6f, u + 0.6f, 3.6f, 5.0f, hl + 0.02f, TrimWhite); FrontQuad(mb, f, u - 0.5f, u + 0.5f, 3.7f, 4.9f, hl + 0.04f, GlassDark); }
            // the back (v = -hl, toward the deck): glass doors
            var bf = new Frame { O = f.O, R = -f.R, F = -f.F, M = 1f };
            for (float u = -hw + 2f; u <= hw - 2f; u += 3f) { FrontQuad(mb, bf, u - 0.9f, u + 0.9f, 0f, 2.3f, hl + 0.02f, TrimWhite); FrontQuad(mb, bf, u - 0.8f, u + 0.8f, 0f, 2.2f, hl + 0.04f, GlassDark); FrontQuad(mb, bf, u - 0.6f, u + 0.6f, 3.6f, 5.0f, hl + 0.04f, GlassDark); }
            // the long sides: two rows of windows
            foreach (float s in new[] { -1f, 1f }) for (float vv = -hl + 1.8f; vv <= hl - 1.8f; vv += 3.2f) { SideQuad(mb, f, s * (hw + 0.02f), vv - 0.55f, vv + 0.55f, 1.0f, 2.2f, s, GlassDark); SideQuad(mb, f, s * (hw + 0.02f), vv - 0.55f, vv + 0.55f, 3.7f, 4.9f, s, GlassDark); }
            var col = new GameObject("Courson clubhouse collider"); col.transform.SetParent(root, false);
            col.transform.SetPositionAndRotation(f.P(0f, H / 2f, 0f), Quaternion.LookRotation(f.F, Vector3.up));
            col.AddComponent<BoxCollider>().size = new Vector3(2f * hw, H, 2f * hl);
        }

        // four courts side by side in one row (the courts' length along CV), a green surround, one fence round all four with a gate at
        // each end of the north side (toward the path and the canopies), light poles outside the south side
        static void CoursonCourts(MeshBuilder flat, MeshBuilder link, Breakables fence, Breakables lamps, TerrainGrid t, float y, ref int courts, ref int panels, ref int poles)
        {
            Vector3 P(Vector2 q, float lift) => new Vector3(q.x, y + lift, q.y);
            Vector3 C(float u, float v, float lift) => P(CoursonSwim.CW(u, v), lift);
            void Quad(MeshBuilder m, float u0, float u1, float v0, float v1, float lift) => Face(m, C(u0, v0, lift), C(u1, v0, lift), C(u1, v1, lift), C(u0, v1, lift), Vector3.up);
            float HU = CoursonSwim.CourtHU, HV = CoursonSwim.CourtHV;
            flat.Tint = CourtGreen; Quad(flat, -HU, HU, -HV, HV, 0.03f);
            const float HL = 11.885f, HWd = 5.485f, HWs = 4.115f, SL = 6.40f, LW = 0.05f;
            float pitch = 2f * HU / 4f;
            for (int i = 0; i < 4; i++)
            {
                float cu = -HU + pitch * (i + 0.5f);
                flat.Tint = CourtBlue; Quad(flat, cu - HWd - 0.9f, cu + HWd + 0.9f, -HL - 2.5f, HL + 2.5f, 0.045f);
                flat.Tint = LineWhite;
                foreach (float v in new[] { -HL, HL }) Quad(flat, cu - HWd, cu + HWd, v - LW, v + LW, 0.06f);
                foreach (float u in new[] { -HWd, -HWs, HWs, HWd }) Quad(flat, cu + u - LW, cu + u + LW, -HL, HL, 0.06f);
                foreach (float v in new[] { -SL, SL }) Quad(flat, cu - HWs, cu + HWs, v - LW, v + LW, 0.06f);
                Quad(flat, cu - LW, cu + LW, -SL, SL, 0.06f);
                foreach (float v in new[] { -HL + 0.2f, HL - 0.2f }) Quad(flat, cu - LW, cu + LW, v - LW, v + LW, 0.06f);
                var post = new[] { CoursonSwim.CW(cu - HWd - 0.914f, 0f), CoursonSwim.CW(cu + HWd + 0.914f, 0f) }; var mid = CoursonSwim.CW(cu, 0f);
                foreach (var pp in post) Post(link, P(pp, 0f), 1.07f, 0.08f, new Color(0.12f, 0.24f, 0.16f));
                var nrm = new Vector3(CoursonSwim.CV.x, 0f, CoursonSwim.CV.y);
                for (int s = 0; s < 2; s++)
                {
                    var a0 = s == 0 ? post[0] : mid; var a1 = s == 0 ? mid : post[1];
                    float h0 = s == 0 ? 1.07f : 0.914f, h1 = s == 0 ? 0.914f : 1.07f;
                    link.Tint = new Color(0.08f, 0.08f, 0.09f);
                    Face(link, P(a0, 0.08f), P(a1, 0.08f), P(a1, h1 - 0.06f), P(a0, h0 - 0.06f), nrm); Face(link, P(a0, 0.08f), P(a1, 0.08f), P(a1, h1 - 0.06f), P(a0, h0 - 0.06f), -nrm);
                    link.Tint = LineWhite;
                    Face(link, P(a0, h0 - 0.06f), P(a1, h1 - 0.06f), P(a1, h1), P(a0, h0), nrm); Face(link, P(a0, h0 - 0.06f), P(a1, h1 - 0.06f), P(a1, h1), P(a0, h0), -nrm);
                }
                courts++;
            }
            // the fence: corners SW, SE, NE, NW in the courts' frame; side e from corner e to e + 1 (0 south, 1 east, 2 north, 3 west)
            const float FH = 3.0f;
            var corners = new[] { CoursonSwim.CW(-HU, -HV), CoursonSwim.CW(HU, -HV), CoursonSwim.CW(HU, HV), CoursonSwim.CW(-HU, HV) };
            for (int e = 0; e < 4; e++)
            {
                var p0 = corners[e]; var p1 = corners[(e + 1) % 4]; float L = (p1 - p0).magnitude; var dir = (p1 - p0) / L;
                var o2 = (((p0 + p1) * 0.5f) - CoursonSwim.CourtC).normalized; var outN = new Vector3(o2.x, 0f, o2.y);
                var stretches = new List<(float s0, float s1)>();
                if (e == 2) { float g1 = 2f * HU - 3f * pitch, g2 = 2f * HU - pitch; stretches.Add((0f, g1 - 1.8f)); stretches.Add((g1 + 1.8f, g2 - 1.8f)); stretches.Add((g2 + 1.8f, L)); }   // gates on the north side between courts 1-2 and 3-4
                else stretches.Add((0f, L));
                foreach (var (s0, s1) in stretches)
                {
                    if (s1 - s0 < 0.5f) continue;
                    var q0 = p0 + dir * s0; var q1 = p0 + dir * s1;
                    int n = Mathf.Max(1, Mathf.CeilToInt((s1 - s0) / 3f));
                    for (int j = 0; j < n; j++)
                    {
                        var qa = Vector2.Lerp(q0, q1, j / (float)n); var qb = Vector2.Lerp(q0, q1, (j + 1) / (float)n); var qm = (qa + qb) * 0.5f;
                        var m = fence.Begin(qm);
                        fence.Piece(() => Post(m, P(qa, -0.1f), FH + 0.1f, 0.07f, ChainLink));
                        if (j == n - 1) fence.Piece(() => Post(m, P(qb, -0.1f), FH + 0.1f, 0.07f, ChainLink));
                        fence.Piece(() =>
                        {
                            Beam(m, P(qa, FH - 0.03f), P(qb, FH - 0.03f), 0.05f, 0.05f, ChainLink);
                            m.Tint = ChainLink;
                            float la = Vector2.Dot(qa - p0, dir), lb = Vector2.Dot(qb - p0, dir);
                            for (float sw = la + 0.12f; sw < lb; sw += 0.16f)
                            {
                                var w0 = p0 + dir * sw; var w1 = w0 + dir * 0.012f;
                                Face(m, P(w0, 0f), P(w1, 0f), P(w1, FH), P(w0, FH), outN); Face(m, P(w0, 0f), P(w1, 0f), P(w1, FH), P(w0, FH), -outN);
                            }
                            for (float h = 0.08f; h < FH; h += 0.16f) { Face(m, P(qa, h), P(qb, h), P(qb, h + 0.012f), P(qa, h + 0.012f), outN); Face(m, P(qa, h), P(qb, h), P(qb, h + 0.012f), P(qa, h + 0.012f), -outN); }
                        });
                        if (e == 2)   // the green windscreen on the north side (toward the path)
                            fence.Piece(() =>
                            {
                                m.Tint = new Color(0.11f, 0.29f, 0.18f);
                                Face(m, P(qa + o2 * 0.03f, 0.3f), P(qb + o2 * 0.03f, 0.3f), P(qb + o2 * 0.03f, 2.1f), P(qa + o2 * 0.03f, 2.1f), outN);
                                Face(m, P(qa - o2 * 0.03f, 0.3f), P(qb - o2 * 0.03f, 0.3f), P(qb - o2 * 0.03f, 2.1f), P(qa - o2 * 0.03f, 2.1f), -outN);
                            });
                        fence.End(P(qm, FH / 2f), new Vector3(dir.x, 0f, dir.y), 0.9f, FH, (qb - qa).magnitude);
                        panels++;
                    }
                }
            }
            // light poles outside the south side, between the courts
            for (int i = 1; i < 4; i += 1)
            {
                var p = CoursonSwim.CW(-HU + pitch * i, -HV - 0.6f); float gy = Ground(t, p.x, p.y); var outV = -CoursonSwim.CV;
                var lb = lamps.Begin(p);
                lamps.Piece(() => Post(lb, new Vector3(p.x, gy - 0.1f, p.y), 4.2f, 0.16f, ChainLink), gy + 0.1f);
                lamps.Piece(() => Post(lb, new Vector3(p.x, gy + 4.1f, p.y), 3.9f, 0.13f, ChainLink), gy + 0.1f);
                lamps.Piece(() =>
                {
                    var head = new Vector3(p.x, gy + 8.0f, p.y) - new Vector3(outV.x, 0f, outV.y) * 0.5f;
                    Beam(lb, new Vector3(p.x, gy + 7.9f, p.y), head, 0.08f, 0.08f, ChainLink);
                    BoxF(lb, MakeFrame(head, -outV, 1f), -0.45f, 0.45f, -0.15f, 0.12f, -0.3f, 0.3f, new Color(0.2f, 0.2f, 0.22f));
                }, gy + 0.1f);
                lamps.End(new Vector3(p.x, gy + 1.2f, p.y), new Vector3(outV.x, 0f, outV.y), 0.6f, 2.4f);
                poles++;
            }
        }
    }
}
